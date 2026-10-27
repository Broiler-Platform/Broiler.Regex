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
using Broiler.Unicode.Tables;

var regex = new BroilerRegex(@"\p{Script=Greek}+", "u", UnicodeTables.Instance);
var match = regex.Match("123αβ"); // Success = true, Index = 3, Length = 2
```

The package carries no Unicode data. It depends on the Unicode owner's provider,
`Broiler.Unicode.Algorithms` `0.1.0-preview.5`, alone, and every compilation takes the
caller's tables as an `IUnicodeTables` (requirement R11 of the
[roadmap](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/docs/roadmap.md)):
a JavaScript engine hands in the tables it embeds, and the example above hands in the
owner's `Broiler.Unicode.Tables` `0.1.0-preview.5`, which a caller references itself.
The tables must be of the Unicode version the provider reads, 17.0.0.

## Build and publish

Build and test with:

```bash
dotnet build Broiler.Regex.slnx
dotnet test Broiler.Regex.slnx
```

The solution's tests include `tests/Regex.Architecture.Tests`, the shared JavaScript libraries
programme's architecture rules, which hold the code to the programme's
[conventions](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/docs/conventions.md),
and `tests/Regex.Fuzz`, the fuzz target, whose declared session CI runs beside them
([its README](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/tests/Regex.Fuzz/README.md));
releases are recorded in the [changelog](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/CHANGELOG.md).

See the [preview publishing guide](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/docs/publishing.md)
for CI validation, cumulative preview numbering, package verification, and nuget.org setup.

Broiler.Regex is licensed under the [Apache License 2.0](https://github.com/Broiler-Platform/Broiler.RegEx/blob/main/LICENSE), which provides the
software on an “AS IS” basis without warranties or conditions.
