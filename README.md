# Broiler.Regex

An experimental .NET 10 regular-expression engine implementing ECMAScript matching
semantics for Broiler.JS. The implementation and limitations are documented in the
[engine README](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/Broiler.Regex/README.md).

> **Preview status:** This is unstable, substantially AI-assisted software with known
> unsupported ECMAScript features. Human-review approval is revision-scoped; consult
> [HUMAN_REVIEW.md](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/HUMAN_REVIEW.md) for the reviewed revision and conditions before
> describing the current checkout as approved.

Remaining implementation and Broiler.JS adoption work is tracked in the
[current roadmap](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/docs/roadmap.md).

## Install and use

Requires .NET 10. Packages and all dependencies are restored from nuget.org:

```bash
dotnet add package Broiler.Regex --prerelease --source https://api.nuget.org/v3/index.json
```

```csharp
using Broiler.Regex;

var regex = new BroilerRegex(@"\p{Script=Greek}+", "u");
var match = regex.Match("123αβ"); // Success = true, Index = 3, Length = 2
```

Unicode data comes from `Broiler.UniCode.Properties` and
`Broiler.UniCode.Emoji.StringProperties`, version `0.1.0-preview.3`, the version
`Directory.Packages.props` pins and the package depends on at least. Their assembly
namespaces remain `Broiler.Unicode.Properties` and `UnicodeEmoji.StringProperties`.
Requirement R11 of the [roadmap](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/docs/roadmap.md)
replaces both with the Unicode owner's provider, `Broiler.Unicode.Algorithms`, during milestone SJL-11.

## Build and publish

Build and test with:

```bash
dotnet build Broiler.Regex.slnx
dotnet test Broiler.Regex.slnx
```

The solution's tests include `tests/Regex.Architecture.Tests`, the shared JavaScript libraries
programme's architecture rules, which hold the code to the programme's
[conventions](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/docs/conventions.md);
releases are recorded in the [changelog](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/CHANGELOG.md).

See the [preview publishing guide](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/docs/publishing.md)
for CI validation, cumulative preview numbering, package verification, and nuget.org setup.

Broiler.Regex is licensed under the [Apache License 2.0](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/LICENSE), which provides the
software on an “AS IS” basis without warranties or conditions.
