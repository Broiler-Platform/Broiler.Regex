# Changelog

Every release of the `Broiler.Regex` package has an entry here, newest first. The file was opened on 2026-10-25, at
work package SJL-11.2 of the shared JavaScript libraries programme, whose
[convention 16](docs/conventions.md) asks each governed library for a changelog with "Observable changes" and "Data"
headings. Releases before it, `0.1.0-preview.1` to `0.1.0-preview.4`, are recorded only by their packages on
nuget.org. [`eng/preview-version-history.json`](eng/preview-version-history.json) is not a release record: it holds
only the versions the version resolver has to remember beside nuget.org's, which today is `0.1.0-preview.1`
([publishing guide](docs/publishing.md), "Choose the version").

The package follows semantic versioning on its preview stream ([publishing guide](docs/publishing.md)):

- a public API removal or a binary-breaking change is a major version; for a `0.x` preview, a `0.y` minor version
  (decision D12 of Broiler.VM's JSD-0081);
- a new API is a minor version;
- a correction toward the specification is a minor version, with its own entry under **Observable changes**;
- a change of the Unicode data the package reads is a minor version, with its own entry under **Data**.

Each entry has the headings **Added**, **Changed**, **Observable changes** and **Data**, and says "none" under a
heading with nothing to report. A break of the published API that package validation accepts is listed under
**Changed** with the requirement (R1 to R11 of [`docs/roadmap.md`](docs/roadmap.md) section 6) that forces it, and
in [`docs/suppressions.md`](docs/suppressions.md).

## Unreleased

*SJL-11.2's changes accumulate here. Decision D12 of Broiler.VM's JSD-0081 ships the SJL-11 surface as
`0.2.0-preview.1`, published to nuget.org only with the owner's approval (decision D13).*

### Added

- **The programme's process (R10).**
  - [`docs/conventions.md`](docs/conventions.md), the programme's contract conventions, numbered as Broiler.VM's
    `architecture.md` section 4 and Broiler.Unicode's copy number them.
  - `tests/Regex.Architecture.Tests`: the architecture rules SL1 to SL10 and the contract checks C1, C3 and C5, each
    with a register row and a witness, asserting exactly the starting violation list,
    `tests/Regex.Architecture.Tests/starting-violations.tsv`, which SJL-11.2 shrinks to none.
  - `probes/Regex.Probe`, the Native AOT probe, published with Broiler.VM's root settings; CI compares its report
    under Native AOT with its report under the JIT, on ubuntu-latest and windows-latest.
  - `Broiler.Regex/PublicAPI.txt`, the public API baseline rule SL6 compares in both directions, and package
    validation against the published `0.1.0-preview.4`.
  - CI jobs for the architecture tests, the probe and the assurance check (Broiler.Code's
    `assurance check --root Broiler.RegEx`).
  - `.gitattributes`, which stores every text file with LF line endings.
- **The caller's nesting limit, answered as a status (R4; task 2.3, decisions D6, D7 and D9).**
  `BroilerRegex.Compile(string, RegexFlags, IUnicodeTables, int nestingLimit, out BroilerRegex?, out RegexLimit)`
  compiles a pattern under the caller's limit and answers a `RegexStatus`: `Success` with the compiled pattern, or
  `LimitExceeded` with `RegexLimit.Nesting` when the pattern nests deeper, checked before each descent of the parser
  and of the compiler. Depth is counted as Broiler.VM's matcher counts it: the pattern is depth 1, each group of any
  kind adds 1, and under `v` each class adds 1. `BroilerRegex.MaximumNestingLimit` (512) is the largest limit a
  caller may pass, chosen from a measurement of the stack each shape of nesting costs on a 1 MiB thread that its
  documentation records, and proven there on linux-x64 for the parser and the compiler: a pattern of every shape
  measured, at the maximum, parses and compiles on a 1 MiB thread under the JIT (`NestingMaximumTests`) and in the
  Native AOT probe, which now runs its population on a thread of that stack, and one level past it is refused with the
  nesting status. Matching at the maximum is proven only for the shapes `NestingMaximumTests.MatchedShapes` lists; the
  matcher's continuations recurse until task 2.4a, and the shapes found to exhaust the stack at match time are listed,
  with the depths they fail at, in the package README's known limitations. win-x64 was not measured. The constant is
  never lowered below 512 in the `0.x` line (decision D7). A limit below 1 or above it is the caller's broken
  precondition, an `ArgumentOutOfRangeException` (convention 3). `RegexStatus` and `RegexLimit` hold only what
  nesting needs; the new matcher (task 2.4a) and the validate entry's syntax status (task 2.8) add members to them.
  `Success`, for a compilation that completes, is not one of decision D9's statuses, which are the answers of a match
  and of a validation: it is named and numbered as Broiler.Unicode's `UnicodeStatus.Success`, and a match will answer
  `Match` or `NoMatch` instead. A syntax error from `Compile` is still a `RegexSyntaxException`.

### Changed

- **The assembly declares itself trimmable and AOT-compatible.** `IsAotCompatible` and `IsTrimmable` are set, with the
  trim, AOT and single-file analyzers reporting as errors; they report nothing. The assembly now carries the
  `IsTrimmable` and `IsAotCompatible` assembly metadata, so a consumer's trimmer treats it as trimmable. No method's
  IL changes.
- **Unicode data through the Unicode owner's provider (R11; task 2.2, decision D11).** The package depends on
  `Broiler.Unicode.Algorithms` `0.1.0-preview.5` alone, in place of `Broiler.UniCode.Properties` and
  `Broiler.UniCode.Emoji.StringProperties` `0.1.0-preview.3`, and compilation takes the caller's tables as an
  `IUnicodeTables`: property escapes, the properties of strings, both branches of Canonicalize and `\s`'s
  Space_Separator are the provider's answers over them. An engine hands
  in the tables it embeds; the tests and the probe hand in `Broiler.Unicode.Tables` `0.1.0-preview.5`, which the
  package does not reference. `CaseFolding`, its generated table `CaseFoldingData` and the generator under
  `Broiler.Regex/Unicode/tools/` are deleted. The breaks below are each suppressed in
  `Broiler.Regex/CompatibilitySuppressions.xml` and listed in [`docs/suppressions.md`](docs/suppressions.md); every
  one is R11's, since none of these members can work without tables it is not handed (decision D12):
  - `BroilerRegex(string, RegexFlags)` and `BroilerRegex(string, string?)` are removed; their replacements take an
    `IUnicodeTables` as a third argument.
  - `RegexParser(string, RegexFlags)` and `RegexParser.Parse(string, RegexFlags, out int, out IReadOnlyDictionary<string, int>)`
    are removed; their replacements take an `IUnicodeTables`, and the new `Parse` answers the capture count only
    (the group-name map stays on `BroilerRegex.GroupNames`, as convention 1 keeps a collection out of a new public
    signature).
  - The public type `Broiler.Regex.Unicode.CaseFolding` is removed (deleted), and so are the members of
    `Broiler.Regex.Unicode.UnicodeCharSets` that read Unicode data: `AllCharacters`, `EscapeSet`, `IsSpace`, `IsWord`,
    `ResolveProperty` and `WordSet`. Its members that read no table, `IsDigit`, `PositiveEscape`,
    `IsComplementEscape` and the `PropertyMatch` holder, stay public with their `0.1.0-preview.4` answers.
  - `CharSet.AddEscape(ClassEscape, bool, bool)` and `CharSet.Contains(int, bool, bool)` are now internal: each
    needs the compilation's provider answers.
  - A provider of another Unicode version, or one lacking a table the algorithms read, is refused with an
    `ArgumentException` when a pattern is compiled, and a null provider with an `ArgumentNullException`.
- **A `CodePointSet` is immutable after construction (R10: conventions 7 and 10; task 2.3, decision D31).** A set's
  ranges are sorted and merged when it is built, and nothing changes them after; every operation answers a new set,
  and `Union` is one pass over the two sorted range lists. `0.1.0-preview.4`'s set was a list that its readers sorted
  and merged in place on their first read, without a lock, so a set shared by concurrent matches, or held in one of
  the library's static fields (`UnicodeCharSets`' internal `FullRange`, `DigitSet` and `BasicWordSet`), was rewritten
  by whichever thread read it first. The breaks are suppressed in `Broiler.Regex/CompatibilitySuppressions.xml` and
  listed in [`docs/suppressions.md`](docs/suppressions.md):
  - `CodePointSet.AddCodePoint`, `AddRange` and `AddAll` are removed: each could only change a set. `Of` and
    `Union` build the same sets, and `CharSet`'s adders still add to a class, each now making a new set.
  - `CodePointSet.Clone` answers the set itself, which cannot change; `Ranges` is a list that cannot be changed,
    where `0.1.0-preview.4` handed out the set's own list. `Of` with its bounds out of order still throws
    `ArgumentException`.
- **`RegexMatch.Empty` holds nothing that can change (R10: convention 7; task 2.3).** The no-match result every call
  without a match answers kept its `NamedGroups` as a new `Dictionary` behind `IReadOnlyDictionary`, which a cast
  changed for every later no-match of the process. It is now the empty `ReadOnlyDictionary`, so the cast fails and an
  `IDictionary` write throws `NotSupportedException`; `Groups` stays an empty array. No signature changes.
- **A compiled pattern's group names cannot be changed (R10: convention 10; task 2.3).** `BroilerRegex.GroupNames`
  and `RegexParser.GroupNames` were the parser's own `Dictionary` behind `IReadOnlyDictionary`, and the matcher read the
  same instance on every match, so a cast changed the names every later match of that pattern reports, on any thread.
  Both are now a `ReadOnlyDictionary` view of a map nothing else holds: a cast to `IDictionary` reports `IsReadOnly`, and
  a write throws `NotSupportedException`. No signature changes.

### Observable changes

- **A pattern nested deeper than depth 512 is refused (R4; task 2.3, decision D6)**: more than 511 nested groups, or
  under `v` more than 511 nested classes, the pattern itself being depth 1. The constructors of
  `BroilerRegex` and `RegexParser.Parse`, which take no limit, parse under `BroilerRegex.MaximumNestingLimit` and
  refuse a deeper pattern with a `RegexSyntaxException` at the group or class that opens the level past it.
  `0.1.0-preview.4` parsed and compiled whatever its stack held and ended the process with a stack overflow past it:
  on a 1 MiB thread, at a few hundred levels for some shapes, and on any stack for a pattern such as 1,000,000 opening
  parentheses. A pattern within the maximum compiles and matches as before, and its match can still exhaust a small
  stack for the shapes the README lists, until task 2.4a.
- **Property names are matched exactly (R5).** `\p{…}` and `\P{…}` admit exactly the names and values ECMA-262's
  tables and `PropertyValueAliases.txt` list (§22.2.2.9, UnicodeMatchProperty and UnicodeMatchPropertyValue), as the
  provider's `UnicodeProperties.TryResolve` and `UnicodeStringProperties.TryResolve` match them. `0.1.0-preview.4`
  matched names loosely, ignoring letter case, `_`, `-` and spaces, so it accepted `\p{letter}`, `\p{ascii}`,
  `\p{Script=greek}`, `\p{GC=Lu}`, `\p{ScriptExtensions=Greek}`, `\p{Script = Greek}` and, under `v`,
  `\p{rgiemoji}`; each is now a `RegexSyntaxException`, the specification's early error.
- **`Katakana_Or_Hiragana` is a Script value (R5).** `\p{Script=Katakana_Or_Hiragana}`, `\p{sc=Hrkt}` and the
  same under `Script_Extensions` are accepted, matching nothing, as no code point has that value; `0.1.0-preview.4`
  refused them.
- **Group names are RegExpIdentifierNames, read the same way in every mode (R5).** A GroupName's code points are
  held to IdentifierStartChar and IdentifierPartChar by the provider's `EcmaScriptLexical` (ECMA-262 §22.2.1.1), so
  `(?<1>x)`, `(?<a-b>x)`, `(?<a b>x)`, `(?<a.b>x)`, `(?<\u0031>x)` and a name holding U+0000 are refused, which
  `0.1.0-preview.4` accepted. `\u`, `\u{…}` and escaped or raw surrogate pairs are read in a name with and without
  `u` and `v` alike, so `(?<\u{1D400}>x)` and `(?<\uD835\uDC00>x)` are accepted without `u`, where
  `0.1.0-preview.4` refused the first and threw `ArgumentOutOfRangeException` for the second; a lone surrogate, as
  `(?<\uD800>x)` under `u`, is a `RegexSyntaxException` rather than that exception.
- **A duplicate group name is found as decoded (R5).** `(?<\u0061>x)(?<a>y)` repeats the name `a` and is refused,
  as `(?<a>x)(?<a>y)` always was; `0.1.0-preview.4` compared names as written and accepted it. A name repeated only
  across alternatives stays refused until requirement R6.
- **A braced escape never joins a surrogate pair (R5; row S10 of Broiler.VM's correspondence).** Under `u` and `v`
  only two four-digit escapes form a pair, so `\uD83D\u{DC38}` is a lone lead surrogate and a lone trail surrogate,
  two characters, and `\uD83D\u{41}` is accepted; `0.1.0-preview.4` refused every four-digit lead escape followed by
  `\u{` ("Invalid hexadecimal escape").
- **A reference to an undeclared group name is refused by the parser (R5; row S5).** `\k<b>(?<a>x)`, under `u` or
  without it, is a `RegexSyntaxException` at the reference from `RegexParser.Parse` alone, as ECMA-262 §22.2.1.1
  requires; `0.1.0-preview.4` accepted it there and refused it only when the matcher was built, with no position. In a
  pattern that declares a group name, Annex B's identity escapes exclude `k` (§B.1.2), so `[\k<a>](?<a>x)` is refused,
  which `0.1.0-preview.4` read as a class holding `k`; without a group name and without `u` and `v`, `\k` is still the
  letter.
- **`\0` looks ahead at the ASCII digits alone (R5).** `CharacterEscape :: 0 [lookahead ∉ DecimalDigit]`
  (ECMA-262 §22.2.1) makes `\0` NUL before any digit but `0` to `9`, so `\0` followed by U+0661 ARABIC-INDIC DIGIT
  ONE or U+FF11 FULLWIDTH DIGIT ONE matches NUL and then that digit in every mode; `0.1.0-preview.4` looked ahead with
  `char.IsDigit`, read `\0` there as the character `0` without `u` and `v` and refused the pattern under them.
- **Annex B's legacy octal escapes outside Unicode mode (§B.1.2).** Without `u` and `v`, `\0` before an ASCII digit
  and a class's `\1` to `\7` are LegacyOctalEscapeSequences: `\01` is U+0001, `\012` U+000A, `\08` NUL and then `8`,
  `[\1]` U+0001 and `[\101]` `A`. `0.1.0-preview.4` read each as the identity escape of its first digit, so `\01`
  matched `01` and `[\1]` the digit `1`. Under `u` and `v` each stays a syntax error, and an atom's `\1` to `\9` is a
  back-reference first, as it was.
- **A negated `v`-mode class is held to MayContainStrings of its syntax (R5).** ECMA-262 §22.2.1.1 refuses
  `[^ … ]` and a negated nested class when MayContainStrings of the contents is true (§22.2.1.8): a union when any
  operand may contain strings, an intersection when every operand may, a subtraction when its first operand may, a
  `\q{…}` when one of its alternatives is empty or longer than one ClassSetCharacter as written, and a property of
  strings. `0.1.0-preview.4` asked instead whether strings were left after the set operations, so
  `[^\q{ab}--\q{ab}]`, `[^\p{RGI_Emoji}--\p{RGI_Emoji}]`, `[^\p{RGI_Emoji}&&\q{ab}]`, `[^\q{ab}&&\q{cd}]` and
  `[[^\q{ab}--\q{ab}]]` are now each a `RegexSyntaxException`, which it accepted; `[^\q{\uD83D\u{DE00}}]`, two
  ClassSetCharacters, is one too (`0.1.0-preview.4` refused it as an invalid hexadecimal escape, the S10 entry above).
  `[^\p{RGI_Emoji}&&a]` and `[^a--\q{ab}]` stay accepted.
- **A braced quantifier with nothing to repeat is a syntax error without `u` too (R5).** Annex B's
  `ExtendedAtom :: InvalidBracedQuantifier` is an early error (ECMA-262 §B.1.2.1): a well-formed `{n}`, `{n,}` or
  `{n,m}` where an atom should be, as in `/{2}/`, `/{2,}/`, `/a|{2}/`, `/({2,3})/` and `/x{1}{1,}/`, is a
  `RegexSyntaxException` in every mode. `0.1.0-preview.4` read it as literal text outside Unicode mode. A `{` that
  opens no well-formed quantifier, as in `/{a}/`, `/{,5}/` or `/x{o}x/`, is still a character there, and a quantified
  look-ahead such as `/(?=a){2}/` is still accepted.
- **Annex B's control escapes (§B.1.2).** Without `u` and `v`, a `\c` not followed by an ASCII letter is
  `ExtendedAtom :: \ [lookahead = c]`: the backslash alone, with the `c` read next as a character of its own, so
  `/\c1/` matches the text `\c1` and `/a\c/` the text `a\c`; `0.1.0-preview.4` consumed the `c`, so `/\c1/` matched
  `\1`. Inside a class, `ClassEscape :: c ClassControlLetter` also takes a decimal digit or `_`, so `[\c1]` is U+0011
  and `[\c_]` U+001F, and any other `\c` is the backslash followed by `c`, so `[\c*]` holds `\`, `c` and `*`;
  `0.1.0-preview.4` read `[\c1]` as `\` and `1`. Under `u` and `v` each stays a syntax error.

### Data

- **The Unicode data is the caller's, and the package declares no version of its own (R11).** It reads the tables
  of the Unicode version `Broiler.Unicode.Algorithms` `0.1.0-preview.5` reads, 17.0.0, and refuses any other.
  `0.1.0-preview.4` carried its case-folding table (UCD 17.0.0, `CaseFoldingData.UnicodeVersion`) and took its
  property and emoji data from `Broiler.UniCode.Properties` and `Broiler.UniCode.Emoji.StringProperties`
  `0.1.0-preview.3`.
- **No answer moves with the data.** For every name and value the provider admits, the property's code points, a
  property of strings' code points and sequences, both Canonicalize mappings over the whole code space, and `\s`'s
  Space_Separator part are the same from the provider over `Broiler.Unicode.Tables` `0.1.0-preview.5` as from
  `0.1.0-preview.4`'s own data, compared member by member at task 2.2; the only difference is the
  `Katakana_Or_Hiragana` value above, which `0.1.0-preview.4` did not resolve.
