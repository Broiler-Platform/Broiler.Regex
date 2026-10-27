# Warning suppressions, and accepted breaks of the published API

[← Conventions](conventions.md)

Filed 2026-10-25, at work package SJL-11.2 of the shared JavaScript libraries programme. Two inventories, which
the programme's conventions 12 and 16 ask for.

## Warning suppressions

Convention 12 suppresses no trim, AOT or single-file warning, and convention 17 makes every warning an error. This
inventory, in the shape of Broiler.VM's rule N36, lists by file every warning suppression in a source or project
file of the library (`Broiler.Regex/`) and the Native AOT probe (`probes/Regex.Probe`), and in every build file of
the repository that MSBuild's evaluation of either imports (today `Directory.Build.props`, `Directory.Packages.props`
and `eng/Broiler.Packaging.props`): a `#pragma warning disable`, a `SuppressMessage` or
`UnconditionalSuppressMessage` attribute, a non-empty `NoWarn`, a `WarningsNotAsErrors` or a
`SuppressTrimAnalysisWarnings` element. Rule SL5 of
[`tests/Regex.Architecture.Tests`](../tests/Regex.Architecture.Tests) scans those files and holds the section below
to what it finds: each suppression is listed there, and when the scan finds none the section says None. The build
files are read from the evaluation's import list, not named in the test, so one a later change adds (a
`Directory.Build.targets`, another `eng/*.props`) is scanned the day it is imported.

The probe's project file sets `NoWarn` to `1701;1702`, the compiler's informational codes the SDK puts in every
project. That **replaces** the XML-documentation codes the packaging file appends; it resets the property rather than
suppressing anything, and is not listed. The test projects, whose sources are the rules' own and name every marker
the scan looks for, and the witnesses of the rules, which exist to break them, are not scanned.

### Inventory

- `eng/Broiler.Packaging.props`: `NoWarn` appends `CS1570`, `CS1572`, `CS1573`, `CS1574`, `CS1587`, `CS1591` and
  `CS1734`, the XML-documentation warnings, for every project of the repository. The file is the vendored copy of the
  platform's packaging props and is not edited here. It reaches the library, whose project file does not reset it,
  so a public member without its documentation compiles there today. Rule SL5 refuses the library's evaluated
  `NoWarn` for each of the seven codes, and the starting violation list holds the seven rows until the library resets
  the property and documents every public member (convention 15).

None of these is a trim, AOT or single-file warning.

## Accepted breaks of the published API

Package validation compares every pack of `Broiler.Regex` with the last published package, `0.1.0-preview.4`
(convention 16). A break that the SJL-11 work makes on purpose (decision D12 of Broiler.VM's JSD-0081) is suppressed
in `Broiler.Regex/CompatibilitySuppressions.xml`, listed here with its R item, and recorded in
[`CHANGELOG.md`](../CHANGELOG.md) under **Changed**.

### R11: Unicode data through the Unicode owner's provider (task 2.2 of SJL-11.2, 2026-10-26)

The package reads its Unicode data through `Broiler.Unicode.Algorithms` alone and carries no table, so whatever
needed a table without being handed one cannot remain (decision D11; decision D12: "a table-less constructor cannot
survive R11", and no shim is kept for a member R11 makes impossible). Each member below needed the old packages'
tables or the deleted case-folding table; each is suppressed by `Broiler.Regex/CompatibilitySuppressions.xml` as
package validation generated it (`ApiCompatGenerateSuppressionFile`), and nothing else is.

| Suppression | Target | What replaces it |
|---|---|---|
| CP0001 | `T:Broiler.Regex.Unicode.CaseFolding` | Deleted with its generated table and generator. Canonicalize is the provider's `UnicodeCaseFolding`, read through the internal `RegexUnicode`. |
| CP0002 | `M:Broiler.Regex.Unicode.UnicodeCharSets.AllCharacters(System.Boolean,System.Boolean)` | Removed: under `v` with `i` it is the code points the provider's simple case folding leaves alone, built by the internal `RegexUnicode.BuildCanonicalCharacters` for the compilation that complements a class. |
| CP0002 | `M:Broiler.Regex.Unicode.UnicodeCharSets.EscapeSet(Broiler.Regex.Ast.ClassEscape,System.Boolean,System.Boolean)` | Removed: `\s` is the provider's Space_Separator and `\w` under `iu` its folding; the internal `RegexUnicode.EscapeSet` answers for the compilation. |
| CP0002 | `M:Broiler.Regex.Unicode.UnicodeCharSets.IsSpace(System.Int32)` | Removed: WhiteSpace's Space_Separator part is Unicode data, the provider's (internal `RegexUnicode`). |
| CP0002 | `M:Broiler.Regex.Unicode.UnicodeCharSets.IsWord(System.Int32,System.Boolean,System.Boolean)` | Removed: under `iu` the word set is widened by the provider's folding (internal `RegexUnicode.IsWord`). |
| CP0002 | `M:Broiler.Regex.Unicode.UnicodeCharSets.ResolveProperty(System.String,System.String)` | Removed: property escapes resolve through the provider's `UnicodeProperties` and `UnicodeStringProperties` (internal `RegexUnicode.ResolveProperty`, which answers the `UnicodeCharSets.PropertyMatch` that stays public). |
| CP0002 | `M:Broiler.Regex.Unicode.UnicodeCharSets.WordSet(System.Boolean,System.Boolean)` | Removed: under `iu` it is built from the provider's folding orbits for the compilation (internal `RegexUnicode`). |
| CP0002 | `M:Broiler.Regex.BroilerRegex.#ctor(System.String,Broiler.Regex.RegexFlags)` | `BroilerRegex(string, RegexFlags, IUnicodeTables)`. |
| CP0002 | `M:Broiler.Regex.BroilerRegex.#ctor(System.String,System.String)` | `BroilerRegex(string, string?, IUnicodeTables)`. |
| CP0002 | `M:Broiler.Regex.Parsing.RegexParser.#ctor(System.String,Broiler.Regex.RegexFlags)` | `RegexParser(string, RegexFlags, IUnicodeTables)`. |
| CP0002 | `M:Broiler.Regex.Parsing.RegexParser.Parse(System.String,Broiler.Regex.RegexFlags,System.Int32@,System.Collections.Generic.IReadOnlyDictionary{System.String,System.Int32}@)` | `RegexParser.Parse(string, RegexFlags, IUnicodeTables, out int)`. The group-name map is not an out parameter of the new member, which convention 1 would refuse in a public signature (check C1); it stays `BroilerRegex.GroupNames` and `RegexParser.GroupNames`. |
| CP0002 | `M:Broiler.Regex.Ast.CharSet.AddEscape(Broiler.Regex.Ast.ClassEscape,System.Boolean,System.Boolean)` | Internal, taking the compilation's provider answers: `\s` is the provider's Space_Separator and `\w` under `iu` its folding. |
| CP0002 | `M:Broiler.Regex.Ast.CharSet.Contains(System.Int32,System.Boolean,System.Boolean)` | Internal, taking the compilation's provider answers: a case-insensitive test folds by the provider. |

Of `UnicodeCharSets`, the members that read no table stay public with their `0.1.0-preview.4` answers: `IsDigit`,
`PositiveEscape`, `IsComplementEscape` and the `PropertyMatch` holder, whose two rows of check C1 stay on the starting
violation list for task 2.9. Decision D12 keeps the 0.1 surface where R11 allows it and keeps no shim for a member R11
makes impossible; the plan's section 1 and the dated record of [`roadmap.md`](roadmap.md) section 6 name the public
`UnicodeCharSets` among R11's removals, which holds for the six members above and not for these four.

A pack ends with two messages from package validation: "Could not resolve reference 'UnicodeEmoji.StringProperties.dll'"
and the same for `Broiler.Unicode.Properties.dll`, "referenced by 'lib/net10.0/Broiler.Regex.dll'" of
`broiler.regex.0.1.0-preview.4`. They are expected after R11: the baseline's own dependencies, the two
`Broiler.UniCode.*` `0.1.0-preview.3` packages, are no longer restored, as this package no longer references them. They
do not weaken the comparison, which reads the public surface of the two `Broiler.Regex.dll` files: no public signature
of `0.1.0-preview.4` names a type of either assembly (its API listing, `PublicAPI.txt` at the start of task 2.2, names
none), so each of its members is still compared, and the pack succeeds only with the suppressions above.

### R10: a `CodePointSet` is immutable after construction (task 2.3 of SJL-11.2, 2026-10-26)

The programme's conventions 7 and 10, which this repository adopted under R10, and decision D31 of Broiler.VM's
JSD-0081 ask that every `CodePointSet` be immutable after construction and that no static field hold mutable state.
`0.1.0-preview.4`'s set was a list that its three public adders appended to and that its readers sorted and merged in
place on their first read, without a lock, so a set shared by a compiled pattern's concurrent matches, or held in a
static field, was rewritten by whichever thread read it first. A set is now normalized when it is built and never
changes, and the three adders, which can only change a set, are removed; no shim is kept for them, since a set they
could change would not be immutable. Each is suppressed by `Broiler.Regex/CompatibilitySuppressions.xml` as package
validation generated it.

| Suppression | Target | What replaces it |
|---|---|---|
| CP0002 | `M:Broiler.Regex.Ast.CodePointSet.AddAll(Broiler.Regex.Ast.CodePointSet)` | `CodePointSet.Union`, which answers a new set; `CharSet.AddSet`, which adds a set to a class. |
| CP0002 | `M:Broiler.Regex.Ast.CodePointSet.AddCodePoint(System.Int32)` | `CodePointSet.Of(codePoint, codePoint)` with `Union`; `CharSet.AddCodePoint`. |
| CP0002 | `M:Broiler.Regex.Ast.CodePointSet.AddRange(System.Int32,System.Int32)` | `CodePointSet.Of(lo, hi)` with `Union`; `CharSet.AddRange`. |

The members that only read a set, or answer a new one, stay with their `0.1.0-preview.4` signatures; `Clone` now
answers the set itself, since a set that cannot change is its own copy.

### R1 and R4: the old backend deleted (task 2.4c of SJL-11.2, 2026-10-26)

Every member matches through the new backend since task 2.4c, and the old one is deleted (decision D20 of Broiler.VM's
JSD-0081): `Matching/`, whose closure compiler recursed per level and per term (requirement R4) and whose per-start
`Budget`, under `StepLimit`, answered no match when it ran out (requirement R1), and `RegexOverflowException`, which
reported that recursion's stack guard and has nothing left to report: a ceiling is a status now (decision D9). The
string-bearing `RegexMatch` and `RegexGroup` stay, as requirement R8 allows and decision D12 keeps them, but not in the
deleted backend's namespace: they move from `Broiler.Regex.Matching` to `Broiler.Regex`, unchanged but for
`RegexMatch.Limit`, and the two members that answer them keep their names and parameters. Each break is suppressed by
`Broiler.Regex/CompatibilitySuppressions.xml` as package validation generated it, and nothing else is. One consumer
names the removed type today: Broiler.JS, pinning `0.1.0-preview.4`, catches `RegexOverflowException` around
`BroilerRegex.Match(string, int)` to fall back to .NET's engine (`JSRegExp.Broiler.cs`); that fallback goes when
Broiler.JS moves its pin, at SJL-11.5 (decision D12), and a ceiling reaches it then as `RegexMatch.Limit`.

| Suppression | Target | What replaces it |
|---|---|---|
| CP0001 | `T:Broiler.Regex.RegexOverflowException` | Deleted (R1, R4). A match that reaches a ceiling answers `RegexStatus.LimitExceeded` naming it through the offset entry, a failed `RegexMatch` whose `Limit` names it through `Match(string, int)` and as the last element of `Matches`, and an `InvalidOperationException` through `IsMatch`, whose `bool` has no room for it (a departure from convention 3 that `conventions.md` tracks for task 2.9); no match recurses on the CLR stack, so none is refused for its depth. |
| CP0001 | `T:Broiler.Regex.Matching.RegexMatch` | `Broiler.Regex.RegexMatch`, the same members and `Limit` (R8 keeps it; R1 and R4 delete its namespace). |
| CP0001 | `T:Broiler.Regex.Matching.RegexGroup` | `Broiler.Regex.RegexGroup`, the same members. |
| CP0002 | `M:Broiler.Regex.BroilerRegex.Match(System.String,System.Int32)` | The same member answering `Broiler.Regex.RegexMatch`. |
| CP0002 | `M:Broiler.Regex.BroilerRegex.Matches(System.String)` | The same member answering `IEnumerable<Broiler.Regex.RegexMatch>`. |

A caller that names the types recompiles with `using Broiler.Regex;` in place of `using Broiler.Regex.Matching;`; one
that reads them through `var`, as the feed check's consumer does, compiles unchanged.
