# Warning suppressions, and accepted breaks of the published API

[← Conventions](conventions.md)

Filed 2026-10-25, at work package SJL-11.2 of the shared JavaScript libraries programme. Two inventories, which
the programme's conventions 12 and 16 ask for.

## Warning suppressions

Convention 12 suppresses no trim, AOT or single-file warning, and convention 17 makes every warning an error. This
inventory, in the shape of Broiler.VM's rule N36, lists by file every warning suppression in a source or project
file of the library (`Broiler.Regex/`) and the Native AOT probe (`probes/Regex.Probe`), and in every build file of
the repository that MSBuild's evaluation of either imports (today `Directory.Build.props`, `Directory.Packages.props`
and `eng/Broiler.Packaging.props`, and for the probe `probes/Regex.Probe/AdversarialControls.targets`; *dated note,
2026-10-27, task 2.14 of SJL-11.2:* the probe imports that file, which builds part C's controls, in every build, so
the scan reads it, and it suppresses nothing): a `#pragma warning disable`, a `SuppressMessage` or
`UnconditionalSuppressMessage` attribute, a non-empty `NoWarn`, a `WarningsNotAsErrors` or a
`SuppressTrimAnalysisWarnings` element. Rule SL5 of
[`tests/Regex.Architecture.Tests`](../tests/Regex.Architecture.Tests) scans those files and holds the section below
to what it finds: each suppression is listed there, and when the scan finds none the section says None. The build
files are read from the evaluation's import list, not named in the test, so one a later change adds (a
`Directory.Build.targets`, another `eng/*.props`) is scanned the day it is imported.

The probe's project file, and since task 2.9 the library's, sets `NoWarn` to `1701;1702`, the compiler's informational
codes the SDK puts in every project. That **replaces** the XML-documentation codes the packaging file appends; it resets the property rather than
suppressing anything, and is not listed. The test projects, whose sources are the rules' own and name every marker
the scan looks for, and the witnesses of the rules, which exist to break them, are not scanned.

### Inventory

None.

*Dated note, 2026-11-01 (task 2.9 of SJL-11.2).* The inventory read: "`eng/Broiler.Packaging.props`: `NoWarn` appends
`CS1570`, `CS1572`, `CS1573`, `CS1574`, `CS1587`, `CS1591` and `CS1734`, the XML-documentation warnings, for every
project of the repository. The file is the vendored copy of the platform's packaging props and is not edited here. It
reaches the library, whose project file does not reset it, so a public member without its documentation compiles
there today. Rule SL5 refuses the library's evaluated `NoWarn` for each of the seven codes, and the starting violation
list holds the seven rows until the library resets the property and documents every public member (convention 15)."
The library's project file now resets `NoWarn` to the SDK's own codes, `1701;1702`, as the probe's does, so the seven
codes no longer reach it, every public member is documented (three were not: `BroilerRegex.Pattern`, `Flags` and
`RegexFlags.None`), and a missing comment or an unresolved reference is a build error; the seven rows have left the
starting violation list. The packaging props still append the codes for the test projects, which SL5 does not scan.


## Accepted breaks of the published API

Package validation compares every pack of `Broiler.Regex` with the last published package, `0.1.0-preview.5` since
task 2.9 of SJL-11.2 (convention 16). A break that the SJL-11 work makes on purpose (decision D12 of Broiler.VM's
JSD-0081) is suppressed in `Broiler.Regex/CompatibilitySuppressions.xml`, listed here with its R item, and recorded in
[`CHANGELOG.md`](../CHANGELOG.md) under **Changed**.

*Dated note, 2026-11-01 (task 2.9 of SJL-11.2): the baseline moved.* This paragraph read "Package validation compares
every pack of `Broiler.Regex` with the last published package, `0.1.0-preview.4` (convention 16)". The owner published
`0.1.0-preview.5` to nuget.org on 2026-10-29, from `24344b3` of `main`, which carries tasks 2.2 to 2.7, 2.6 and 2.11, and
wrote on 2026-11-01 "Please keep using version 0.1.0-x scheme!", so the SJL-11 surface ships as `0.1.0-preview.6`
(decision D12 named `0.2.0-preview.1`), and package validation reads `0.1.0-preview.5`, the version a consumer can have.
Every break the four sections that follow list, those of tasks 2.2, 2.3, 2.4c and 2.6, is a break of
`0.1.0-preview.4` that `0.1.0-preview.5` shipped: a pack compared with `0.1.0-preview.5` no longer reports it, so its
suppression has left `CompatibilitySuppressions.xml` (23 suppressions, which package validation generated against
`0.1.0-preview.4`), and the sections stay, as written, as the history of what `0.1.0-preview.5` broke. The breaks of
`0.1.0-preview.5` are task 2.9's, in the last section.

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

### R6: duplicate named groups (task 2.6 of SJL-11.2, 2026-10-27)

ES2025 lets a group name be declared by several groups when no two of them can both participate (ECMA-262 §22.2.1.1,
MightBothParticipate §22.2.1.4), so a name no longer maps to one group. `0.1.0-preview.4`'s `GroupNames`, on
`BroilerRegex` and on `RegexParser`, was an `IReadOnlyDictionary<string, int>` of each name to its one group, which
cannot hold a name of two groups, and which convention 1 keeps out of a public signature (check C1; both were rows of
the starting violation list). Both now answer the library's own `RegexGroupNames`: each name with its groups, in the
order of its first group, each group with its name, and the group of a name that participated in a match the offset
entry wrote. No shim keeps the dictionary, since a dictionary of one group per name would answer a name of several
groups wrongly. Each break is suppressed by `Broiler.Regex/CompatibilitySuppressions.xml` as package validation
generated it, and nothing else is.

| Suppression | Target | What replaces it |
|---|---|---|
| CP0002 | `M:Broiler.Regex.BroilerRegex.get_GroupNames` | `BroilerRegex.GroupNames`, answering `RegexGroupNames`: `GroupsNamed(name)` for the groups of a name, `NameAt(i)` and `Count` for the names in order, `NameOfGroup(k)` for a group's name, and `ParticipatingGroup(name, slots)` for the group of a name that participated. The string-bearing `RegexMatch.NamedGroups` keeps its type and maps each name to its participating group, whose new `RegexGroup.Number` says which. |
| CP0002 | `M:Broiler.Regex.Parsing.RegexParser.get_GroupNames` | `RegexParser.GroupNames`, answering `RegexGroupNames`, as above. An instance made by the public constructor does not parse, so it held no name in `0.1.0-preview.4` and holds none now. |

The consumer that reads the dictionary today is Broiler.JS, pinning `0.1.0-preview.4`, which inverts
`BroilerRegex.GroupNames` into its capture map (`JSRegExp.Broiler.cs`, `BuildCaptureMapFromBroiler`); it moves its pin
only at SJL-11.5 (decision D12), where its adapter reads `RegexGroupNames` instead, the shape its own capture map
already has: each name, in the order of its first group, with its list of groups.

### R10 and R8: the surface of `0.1.0-preview.6` (task 2.9 of SJL-11.2, 2026-11-01)

Task 2.9 fixes the public surface the SJL-11 work ships, as `0.1.0-preview.6` (the owner's decision of 2026-11-01; the
dated note above). Each break below is one of `0.1.0-preview.5`, the baseline, which a consumer of that version meets;
each is suppressed by `Broiler.Regex/CompatibilitySuppressions.xml` as package validation generated it
(`ApiCompatGenerateSuppressionFile`), and nothing else is. The R item is the requirement that forces it: R10's
conventions (3, failure is data; 1, neutral types; 4, work charged by the caller's meter) and R8 (an offset-only
interface beside the string-bearing one D12 keeps).

| Suppression | Target | R item | What replaces it |
|---|---|---|---|
| CP0002 | `M:Broiler.Regex.BroilerRegex.Compile(System.String,Broiler.Regex.RegexFlags,Broiler.Unicode.Algorithms.IUnicodeTables,System.Int32,System.Nullable{System.UInt64},Broiler.Regex.RegexWorkCharge,Broiler.Regex.BroilerRegex@,Broiler.Regex.RegexLimit@)` | R10 (convention 3; decision D9) | The same member with an `out RegexDiagnostic` last: a syntax error is `RegexStatus.SyntaxError` with the validate entry's diagnostic, where it was a `RegexSyntaxException`, and the nesting limit carries its `NestingLimit` diagnostic. |
| CP0001 | `T:Broiler.Regex.Text.RegexFlagsStatus` | R9 (decision D9) | `RegexDiagnosticCode`'s three codes of the flags, `UnknownFlag` (1), `DuplicateFlag` (2) and `UnicodeWithUnicodeSets` (3), the same names at the same numbers; `Success` (0) is `RegexDiagnosticCode.None` with `RegexStatus.Success`. Two public types named one refusal; now the flags reader and the validate entry answer one value. |
| CP0002 | `M:Broiler.Regex.Text.RegexFlagsText.Parse(System.ReadOnlySpan{System.Char},Broiler.Regex.RegexFlags@,System.Int32@)` | R9 (decision D9) | `RegexFlagsText.Parse(ReadOnlySpan<char>, out RegexFlags, out RegexDiagnostic)`, answering `RegexStatus.Success` or `SyntaxError` with the diagnostic: its code, the code unit refused as its argument, its position in the flags text (the old `out int`) and its message identifier. |
| CP0002 | `M:Broiler.Regex.RegexFlagsParser.Parse(System.String)` | R10 (convention 3) | Internal: it throws for a flags string the specification refuses, an expected failure; `RegexFlagsText.Parse` answers the same refusal as a status. `RegexFlagsParser.IsUnicodeMode` stays public. |
| CP0001 | `T:Broiler.Regex.RegexSyntaxException` | R10 (convention 3; check C3) | Internal, deriving from `ArgumentException`. `Compile`, the validate entry and `RegexFlagsText.Parse` answer a refusal as the syntax status with its `RegexDiagnostic`; the string-bearing constructors, which decision D12 keeps and which answer no status, document a pattern or flags text the specification refuses as their broken precondition and throw an `ArgumentException` with the diagnostic's message. `Position` and `Diagnostic` were its public members: the validate entry answers both for the same pattern. |
| CP0001 (17) | `T:Broiler.Regex.Ast.AnchorKind`, `AnchorNode`, `AnyCharNode`, `BackreferenceNode`, `CharClassNode`, `CharNode`, `CharSet`, `ClassEscape`, `CodePointSet`, `DisjunctionNode`, `EmptyNode`, `GroupNode`, `LookaroundNode`, `ModifierGroupNode`, `QuantifierNode`, `RegexNode`, `SequenceNode` (each `T:Broiler.Regex.Ast.<name>`) | R10 (conventions 1, 3 and 4; check C1) | Internal. The syntax tree and the code-point set algebra named collections and a delegate in their public signatures (seven C1 rows of the starting violation list), their operations, whose work grows with the pattern or the sets, took no meter (convention 4, which check A-1 reads over every public member), and `CharSet.AddRange` threw for a range out of order. The tree survives as the library's internal model (decision D5: "the parser, the public tree, the `CodePointSet` algebra and the flag parser stay"), and no engine's adapter reads it (plan rows 5.2 and 6.1): `Compile` and the offset entry are the compilation and the match, `RegexGroupNames` the names. |
| CP0001 | `T:Broiler.Regex.Parsing.RegexParser` | R10 (conventions 3 and 4) | Internal. Its `Parse` threw for a pattern it refuses and took no meter or nesting limit; `Compile` and the validate entry are the public parses, under the caller's nesting limit (and `Compile`'s budget and meter), each answering a status with the diagnostic. |
| CP0001 | `T:Broiler.Regex.Unicode.UnicodeCharSets` | R10 (convention 1; check C1) | Internal, with the tree it serves. `PropertyMatch` named a collection in its public signature (two C1 rows); `IsDigit`, `PositiveEscape` and `IsComplementEscape` read the specification's sets, which the compiled pattern holds. |
| CP0002 | `M:Broiler.Regex.RegexMatch.get_Groups` | R10 (convention 1; check C1) | `RegexMatch.Groups`, answering `ReadOnlySpan<RegexGroup>` where it answered `IReadOnlyList<RegexGroup>`: `Groups[i]` and `foreach` read as before, and `Count` is `Length`. |
| CP0002 | `M:Broiler.Regex.RegexMatch.get_NamedGroups` | R10 (convention 1; check C1), R6 | `RegexMatch.GroupNamed(name)`: the name's participating group, or its first group unmatched when none participated, or null for a name the pattern does not declare and for a failed match, the answer the dictionary's entry gave; `BroilerRegex.GroupNames` lists the names in order. |
| CP0002 | `M:Broiler.Regex.BroilerRegex.Matches(System.String)` | R10 (convention 1; check C1) | `BroilerRegex.NextMatch(match)`, from `Match(input, 0)`: the same matches in the same order, after an empty match a code point on (a pair whole under `u` and `v`), and a last failed match naming the ceiling a match reached. |
| CP0001 | `T:Broiler.Regex.Engine.RegexMatchCeilings` | R8 (task 2.4c's offset entry) | `Broiler.Regex.RegexMatchCeilings`, the same type in the package's root namespace beside the offset entry that takes it: its members and their meaning are unchanged. |
| CP0002 | `M:Broiler.Regex.BroilerRegex.Match(System.ReadOnlySpan{System.Char},System.Int32,System.Boolean,System.Span{System.Int32},Broiler.Regex.RegexWorkCharge,Broiler.Regex.Engine.RegexMatchCeilings,Broiler.Regex.RegexLimit@)` | R8 (task 2.4c's offset entry) | The same offset entry over `Broiler.Regex.RegexMatchCeilings`, its behaviour unchanged. |
