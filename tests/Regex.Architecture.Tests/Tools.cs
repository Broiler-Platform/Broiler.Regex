using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Broiler.Regex.Architecture.Tests;

/// <summary>
/// The external tool rules run: MSBuild, to evaluate a project and to list the files it imports. Broiler.JS's
/// suite also runs Python for its corpus generators; this repository regenerates no data (rule SL7), so MSBuild is
/// the only one.
/// </summary>
internal static class Tools
{
    /// <summary>The project's evaluated properties, as MSBuild computes them with every import applied.</summary>
    internal static IReadOnlyDictionary<string, string> Evaluate(string project, IEnumerable<string> properties)
    {
        var names = properties.Distinct(StringComparer.Ordinal).ToList();
        var arguments = new List<string> { "msbuild", project, "-nologo", "-p:Configuration=Release" };
        arguments.AddRange(names.Select(static n => "-getProperty:" + n));
        var output = Run(Dotnet, arguments, Path.GetDirectoryName(project)!);
        using var json = JsonDocument.Parse(output);
        return json.RootElement.GetProperty("Properties").EnumerateObject()
            .ToDictionary(static p => p.Name, static p => p.Value.GetString() ?? string.Empty, StringComparer.Ordinal);
    }

    /// <summary>
    /// Every file MSBuild's evaluation of the project imports, the project file among them, by full path and in
    /// ordinal order: read from the project's preprocessed form, which closes each import with the path of the file
    /// it brought in. An import whose condition is false is not there, as the evaluation does not import it.
    /// </summary>
    internal static IReadOnlyList<string> ImportedFiles(string project)
    {
        var scratch = Directory.CreateTempSubdirectory("imports-");
        try
        {
            var preprocessed = Path.Combine(scratch.FullName, "preprocessed.xml");
            Run(Dotnet, ["msbuild", project, "-nologo", "-p:Configuration=Release", "-preprocess:" + preprocessed], Path.GetDirectoryName(project)!);
            return ImportsIn(File.ReadAllLines(preprocessed));
        }
        finally
        {
            scratch.Delete(recursive: true);
        }
    }

    /// <summary>
    /// The files a preprocessed project names as imported: each import's comment ends with the imported file's path
    /// on the line before the rule of '=' signs that closes it, and the comment that closes the import names the
    /// importing file the same way.
    /// </summary>
    internal static IReadOnlyList<string> ImportsIn(IReadOnlyList<string> preprocessed)
    {
        var files = new SortedSet<string>(StringComparer.Ordinal);
        for (var i = 1; i < preprocessed.Count; i++)
        {
            if (!preprocessed[i].StartsWith("=====", StringComparison.Ordinal))
                continue;
            var candidate = preprocessed[i - 1].Trim();
            if (candidate.Length > 0 && Path.IsPathFullyQualified(candidate))
                files.Add(Path.GetFullPath(candidate));
        }

        return [.. files];
    }

    private static string Dotnet => Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet";

    private static string Run(string program, IEnumerable<string> arguments, string directory)
    {
        var start = new ProcessStartInfo(program)
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(program + " " + string.Join(' ', arguments) + " exited " + process.ExitCode + ":\n" + output + error.Result);
        return output;
    }
}
