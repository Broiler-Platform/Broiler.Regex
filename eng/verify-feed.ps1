# Prove that a consumer can restore and run the release using nuget.org dependencies.
[CmdletBinding()]
param(
    [string] $Packages = 'artifacts'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem
$packagePath = (Resolve-Path $Packages).Path
$archives = @(Get-ChildItem -LiteralPath $packagePath -Filter *.nupkg)
if (!$archives.Count) { throw 'No packages to validate.' }
$scratch = Join-Path ([IO.Path]::GetTempPath()) ("broiler-feed-check-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $scratch | Out-Null
$references = @()
$mappings = @()
foreach ($archive in $archives) {
    $zip = [IO.Compression.ZipFile]::OpenRead($archive.FullName)
    try {
        $entry = @($zip.Entries | Where-Object FullName -like '*.nuspec')[0]
        $reader = [IO.StreamReader]::new($entry.Open())
        try { [xml] $nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $id = [Security.SecurityElement]::Escape($nuspec.package.metadata.id)
        $version = [Security.SecurityElement]::Escape($nuspec.package.metadata.version)
        $references += "    <PackageReference Include=`"$id`" Version=`"[$version]`" />"
        $mappings += "      <package pattern=`"$id`" />"
    } finally { $zip.Dispose() }
}
$escapedPath = [Security.SecurityElement]::Escape($packagePath)
@"
<configuration>
  <packageSources>
    <clear />
    <add key="release" value="$escapedPath" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <disabledPackageSources><clear /></disabledPackageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="release">
$($mappings -join "`n")
    </packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content (Join-Path $scratch 'NuGet.config') -Encoding utf8
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
$($references -join "`n")
  </ItemGroup>
</Project>
"@ | Set-Content (Join-Path $scratch 'Consumer.csproj') -Encoding utf8
@'
using System;
using Broiler.Regex;

var letters = new BroilerRegex(@"\p{Script=Greek}+", "u").Match("123αβ");
if (!letters.Success || letters.Index != 3 || letters.Length != 2)
    throw new Exception("Unicode property dependency failed.");
if (!new BroilerRegex(@"^\p{RGI_Emoji}$", "v").IsMatch("👨‍👩‍👧‍👦"))
    throw new Exception("Emoji string property dependency failed.");
Console.WriteLine("Package consumer smoke test passed.");
'@ | Set-Content (Join-Path $scratch 'Program.cs') -Encoding utf8
# An isolated cache prevents installed developer packages from hiding feed gaps.
& dotnet restore (Join-Path $scratch 'Consumer.csproj') --configfile (Join-Path $scratch 'NuGet.config') --packages (Join-Path $scratch 'packages') --no-http-cache --nologo
if ($LASTEXITCODE -ne 0) { throw "Consumer restore using nuget.org failed. Publish missing dependencies first. Diagnostics: $scratch" }
& dotnet run --project (Join-Path $scratch 'Consumer.csproj') -c Release --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw "Package consumer smoke test failed. Diagnostics: $scratch" }
Write-Host "Consumer restore and execution verified $($archives.Count) packages using nuget.org dependencies."
