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
  - The public types `Broiler.Regex.Unicode.CaseFolding` (deleted) and `Broiler.Regex.Unicode.UnicodeCharSets` (now
    internal) are removed.
  - `CharSet.AddEscape(ClassEscape, bool, bool)` and `CharSet.Contains(int, bool, bool)` are now internal: each
    needs the compilation's provider answers.
  - A provider of another Unicode version, or one lacking a table the algorithms read, is refused with an
    `ArgumentException` when a pattern is compiled, and a null provider with an `ArgumentNullException`.

### Observable changes

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
