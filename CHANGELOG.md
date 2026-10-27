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
  nesting status. Matching at the maximum through the public members is proven for every shape measured since task
  2.4c routed it through task 2.4a's backend (below); the old matcher's continuations recursed, and the shapes found
  to exhaust the stack at match time were the package README's known limitations until then. win-x64 was not
  measured. The constant is never lowered below 512 in the `0.x` line (decision D7). A limit below 1 or above it is the caller's broken
  precondition, an `ArgumentOutOfRangeException` (convention 3). `RegexStatus` and `RegexLimit` held only what
  nesting needs; the new matcher (task 2.4a) added members to both and the compile budget (task 2.5) added
  `RegexLimit.Compile` (below), and the validate entry's syntax status (task 2.8) adds more.
  `Success`, for a compilation that completes, is not one of decision D9's statuses, which are the answers of a match
  and of a validation: it is named and numbered as Broiler.Unicode's `UnicodeStatus.Success`, and a match answers
  `Match` or `NoMatch` instead (below). A syntax error from `Compile` is still a `RegexSyntaxException`. **The signature
  of `Compile` is provisional**, and no published version carries it: task 2.5 replaced it, adding the caller's compile
  budget and the caller's compile meter, a nullable `RegexWorkCharge` (convention 4; below), and D9's syntax status
  needs room for a code, a message identifier and a position. The plan gives that status to the validate entry of task
  2.8 and names no task that moves `Compile`'s syntax error onto it; task 2.9, which fixes the public surface for
  `0.2.0-preview.1`, holds that open.
- **The new backend, internal (task 2.4a; R1, R3, R4's matcher half, R7, R8; decisions D5, D8, D9 and D10).** A compiler
  from the syntax tree to an instruction program (`Compiling/ProgramCompiler.cs`, with no recursion, under the caller's
  nesting limit) and one backtracking loop with an explicit frame stack and an undo trail (`Engine/`), written new from
  Broiler.VM's matcher as its specification, with no code ported. Its match entry takes a subject span, a start, an
  anchored argument independent of `y`, the caller's slots (two offsets per group, group 0 first, -1 for a group that
  did not participate), a nullable meter and the caller's ceilings; it answers `Match`, `NoMatch` or `LimitExceeded`
  naming the ceiling reached. Every dispatched instruction is charged one unit, a capture reset of any width included,
  except a class test, charged one plus the floor of the base-2 logarithm of its range count; the units are paid in
  blocks of 512 before the work, across every start position with no reset; a meter that throws ends the call with its
  own exception. The step ceiling is optional; the frame and trail ceilings are at least 1 and at most 16,777,216 frames
  and 33,554,432 trail cells, and both engines pass 1,048,576 and 2,097,152 (decision D10). The registers, frames and
  trail come from the shared array pool; frames and trail past 65,536 integers are allocated for the call and left to
  the collector, so a call's peak does not stay pooled. So once the pool holds them a call allocates nothing that grows
  with the capture count while it holds at most 16,384 frames and 32,768 trail cells, about 16,384 participating
  groups (task 2.4c's review stated the bound; before, this entry read as if there were none). The program runs literals, `.`, classes (`v`-mode classes of code points included),
  sequence, alternation, every quantifier form with the specification's empty-iteration rule, and the three kinds of
  group, clearing a quantified atom's captures at each iteration; task 2.4b adds the rest of the grammar (below), and
  the "not yet supported" answer task 2.4a gave it in the meantime is gone. The program and the compiler are internal;
  the entry, the ceilings and the meter `RegexWorkCharge` became public with task 2.4c (below), which routes
  `BroilerRegex` through them. **Public, and provisional like `Compile`:** `RegexStatus.Match` (2),
  `RegexStatus.NoMatch` (3), `RegexLimit.Steps` (2), `RegexLimit.Frames` (3) and `RegexLimit.Trail` (4), extending
  task 2.3's enums. They could not wait: an enum's members take the enum's
  accessibility, and the new entry answers these two public enums, so its answers are public members whatever the
  entry's own accessibility. `PublicAPI.txt` lists them; package validation accepts them as additions.
- **The new backend runs every pattern the parser accepts (task 2.4b; R4's matcher half, R7; decisions D5 and D8),
  still internal.** Characters and classes compare under `i` by both branches of Canonicalize, read at match time from
  the provider's case folding (a class by the case orbit of the character, inverted after); `^ $` with and without `m`
  and `\b \B` with the word characters the flags give; numbered and named back-references, forward and backward, by
  Canonicalize under `i`, by code point under `u` and `v`, a group that did not participate matching the empty string,
  each reading a list of groups, which task 2.6's duplicate names lengthen (below); the four look-arounds, each a barrier
  frame on the machine's one stack, cut back to when its body matches, with a look-behind's body compiled right to
  left and its captures in source order; modifier groups, their flags in force for their body alone; and `v`-mode
  classes of strings, longest first, then the single characters, then the empty string. No method of the machine
  reaches itself (check A-4). Each new instruction costs one unit, a class under `i` one plus its weight for each code
  point of an orbit, a word boundary one plus the word class's weight twice, and a back-reference its capture's length
  on top when it runs. Under `u` and `v` a start inside a surrogate pair now splits the pair for every read of the
  call, a look-behind's included. The compiler's "not yet supported" answer is deleted, and `NotYetSupported` appears
  nowhere in the assembly. The differential comparison over the whole grammar found two defects of the old backend's,
  which task 2.4c's route corrects (Observable changes) and the roadmap's record of task 2.4b lists: D2, a `v`-mode class
  holding the empty string tried it before its single characters (`/[\q{}a]/v` on `a` answered the empty match), and
  JSD-0081's row M7, a back-reference in a look-behind under `u` or `v` compared by code units, which could begin inside
  a surrogate pair.
- **The new backend's fast paths, under the meter (task 2.4c; R1, decision D8).** A quantifier over one character or a
  class without strings is a run, one instruction that takes its required characters, then greedy as many more as hold
  and lazy none, and gives back or takes one at a time from one frame, with no trail cell; two or more one-character
  atoms in a row are one atom run; a `v`-mode class of strings is one lookup in a table of its strings, keyed by the
  character a string begins with (its last right to left) and shared by every occurrence of the same strings; and a
  search tests start positions with the program's first one-character test, the literal-prefix prefilter, entering the
  program only where it holds. Each charges what the work it replaces would: a run's dispatch one unit and each
  character it examines its atom's cost, the one that ends the run included; an atom run its atoms' costs; a class of
  strings one plus the weight of its first characters and a unit for each character of a string tried; a skipped start
  position its first test's cost, one plus the class's weight. Check A-1 reads every one, in both directions where it
  applies. The corpus's rows O05 and O06, `[a]*` and `^[a]*$` over 10,000,001 characters, match. A differential test
  compares every program with and without its fast paths, over the test suite's string literals, task 2.4b's seeded
  population and a population weighted to the shapes the fast paths replace: no disagreement (the roadmap's record of
  task 2.4c).
- **The offset entry is public (task 2.4c; R7, R8, R1, R3; checks A-7 and A-8).**
  `BroilerRegex.Match(ReadOnlySpan<char> subject, int start, bool anchored, Span<int> slots, RegexWorkCharge? meter,
  RegexMatchCeilings ceilings, out RegexLimit limit)` answers `Match`, `NoMatch` or `LimitExceeded` naming the ceiling,
  writing two offsets per group into the caller's slots, group 0 first and -1 for a group that did not participate;
  the anchored argument is independent of `y`, and the start is taken as given (under `u` and `v` a start inside a
  surrogate pair splits it, and an engine that answers as RegExpBuiltinExec steps back before it calls); no string is
  made, and once the shared pool holds what the pattern needs a call allocates nothing, whatever the capture count,
  while it holds at most 16,384 backtrack frames and 32,768 trail cells; past them its frames or trail are its own, so
  a pattern of more than about 16,384 participating groups allocates, in every call, a trail that grows with them
  (check A-8's bound, pinned at 16,384 and 16,385 groups).
  `BroilerRegex.SlotCount` says how many slots a match writes. The meter `RegexWorkCharge` and the ceilings
  `Broiler.Regex.Engine.RegexMatchCeilings` (with `Engines`, `EngineFrames`, `EngineTrail`, `FrameCeilingMaximum` and
  `TrailCeilingMaximum`) become public for it, and `RegexMatch` gains `Limit`. **All provisional**, like `Compile`:
  task 2.5 added the compile meter and the compile budget to `Compile` (below), and task 2.9 fixes the surface of
  `0.2.0-preview.1`. Check C1 leaves the meter's runtime-made members out, as convention 1 admits it, and refuses any
  other public delegate the library declares, its declaration and every signature that names it.
- **The compile meter and the compile budget (task 2.5; R2; decisions D8, D9 and D10; check A-2).**
  `BroilerRegex.Compile(string, RegexFlags, IUnicodeTables, int nestingLimit, ulong? compileBudget, RegexWorkCharge?
  meter, out BroilerRegex?, out RegexLimit)` charges the caller's meter for the work of the compilation, in blocks of
  512 units paid before the work, as a match is charged, and answers `LimitExceeded` with the new
  `RegexLimit.Compile` (5) when the work would pass the caller's compile budget, before the work that would pass it; a
  budget of zero is a broken precondition (`ArgumentOutOfRangeException`), and null is no budget. The units are in
  proportion to the ranges and strings the pattern's expansions touch: one per code unit of the pattern; one per range
  a property escape expands to, and for a property of strings one per sequence and one per code unit of each; one per
  range of each operand of a union, an intersection, a difference or a complement, and a sort's 1 + floor(log2 n) per
  range gathered out of order; for MaybeSimpleCaseFolding one per range and one per code point mapped one at a time;
  for a class's strings their code units each time one is added, looked up or taken, and the class's order; the sets
  a compilation builds from the provider; and the program compiler's copies of class ranges and string tables. A
  meter that throws ends the compilation with its own exception and changes no state of the library. The constructors
  take neither budget nor meter, as an engine that passes no meter compiles (decision D10). **Provisional:** the
  signature and `RegexLimit.Compile`, until task 2.9 fixes the surface of `0.2.0-preview.1`. `PublicAPI.txt` now
  marks each member these entries call provisional with a `# provisional:` line, which the listing's rewrite keeps and
  rule SL6 refuses once its member is gone. The adversarial corpus's class V, fifty `\p{L}` in a class under `v`,
  `vi` and `iu` at a budget of 300 units and fifty `\p{RGI_Emoji}` under `v` and `vi` at 1,000, answers its declared
  `limit:compile` under the JIT and through the probe.
- **The text helpers (R9; task 2.7, check A-9; decisions D9, D11, D20), all provisional**: task 2.9 fixes the public
  surface of `0.2.0-preview.1`, and `PublicAPI.txt`, which rule SL6 generates, marks each with a `# provisional:`
  line. The new namespace `Broiler.Regex.Text` holds what both engines' flag parsers, `RegExp.escape` helpers and
  `source` writers move onto at their cut-overs (decision D20):
  - `RegexFlagsText.Parse(ReadOnlySpan<char>, out RegexFlags, out int position)` reads a flags string as a
    `RegexFlagsStatus`, `Success`, `UnknownFlag`, `DuplicateFlag` or `UnicodeWithUnicodeSets`, never an exception:
    RegExpInitialize's checks over the whole string first (ECMA-262 §22.2.3.3), the refusal at the first unknown or
    repeated code unit, then ParsePattern's `u` with `v` (§22.2.3.4) at the later of the two. It decides by the ninth
    code unit, so it takes no meter. `RegexFlagsText.Format(RegexFlags)` writes the letters in the `flags` getter's
    order, `d g i m s u v y` (§22.2.6.4). `RegexFlagsParser.Parse` reads the same grammar and answers as before, with
    the same messages, so nothing it answers changes.
  - `new RegexEscaper(IUnicodeTables)` and its `EscapeString(string, int maxLength, RegexWorkCharge?, out string?)`:
    `RegExp.escape`'s text (§22.2.5.1, EncodeForRegExpEscape §22.2.5.1.1), a first ASCII digit or letter as `\xHH`,
    a SyntaxCharacter or `/` after a backslash, U+0009 to U+000D as `\t \n \v \f \r`, the punctuators
    `` ,-=<>#&!%:;@~'`" ``, WhiteSpace, LineTerminator and a lone surrogate in lower-case hexadecimal, every other code
    point as itself. WhiteSpace is the provider's `EcmaScriptLexical` over the caller's tables (decision D11), so
    U+0085 and U+180E are not escaped.
  - `RegexEscaper.EscapePattern(string, int maxLength, RegexWorkCharge?, out string?)`: the `source` getter's text
    (EscapeRegExpPattern, §22.2.6.13.1): `(?:)` for the empty pattern, `\/` for a `/` outside a class, `\n`, `\r`,
    `\u2028` and `\u2029` for a line terminator, raw or after a backslash, every other code unit as itself; a class is
    read as a RegularExpressionLiteral reads one, so a `/` in it is kept, the text test262's
    `staging/sm/RegExp/escape.js` asks for. The text holds under every mode, so it takes no flags.
  - Both answer `RegexStatus.Success`, or `RegexStatus.LimitExceeded` when the result would pass the caller's maximum
    length, their only limit; both charge the meter one unit per code unit read before reading and one per code unit
    written before writing, in whole blocks of 512 on one account (decision D8's blocks, convention 4), and answer a
    text that needs no change as itself, at the cost of its reading.
  - Tests over the specification's sets, every code point or code unit enumerated, U+0085 among them; test262's
    `built-ins/RegExp/escape` and `staging/sm/RegExp/escape.js` cases ported as data, test-only, with test262's
    licence beside them ([`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)); and the Native AOT probe's population
    `text-1`, whose report CI holds to the JIT's.
- **Duplicate named groups (R6; task 2.6, check A-6; decisions D9, D12 and D36).** ES2025's rule: a group name may be
  declared by several groups when no two of them can both participate, that is, when some Disjunction holds each two of
  them in different Alternatives (ECMA-262 §22.2.1.1, MightBothParticipate §22.2.1.4), the names compared as decoded.
  `(?<a>x)|(?<a>y)`, `(?:(?<a>x)|(?<a>y))\k<a>`, `((?<a>x)|(?<a>y))*` and `(?<a>x)|(?:y|(?<a>z))` compile;
  `(?<a>x)(?<a>y)`, `(?:(?<a>x)|(?<a>y))(?<a>z)` and `(?<a>x|(?<a>y))` stay a `RegexSyntaxException` at the later
  group. `\k<a>` reads every group of the name and compares the one that participated, at most one (§22.2.2.7.2), forward,
  right to left in a look-behind and under `i`, at the unit a back-reference always cost: one for the instruction and its
  capture's length. The results map a name to its participating group:
  - `RegexGroupNames`, which `BroilerRegex.GroupNames` and `RegexParser.GroupNames` now answer (Changed): `Count` and
    `NameAt(i)`, the names in the order of their first groups, which is the order RegExpBuiltinExec creates the `groups`
    object's properties in (§22.2.7.2, step 34.e); `GroupsNamed(name)`, a name's groups as a read-only span, ascending;
    `NameOfGroup(k)`, a group's name or null; and `ParticipatingGroup(name, slots)`, the group of the name whose start and
    end the offset entry wrote into the caller's slots, or 0 when none did, allocating nothing.
  - `RegexGroup.Number`, a group's number, so `RegexMatch.NamedGroups[name].Number` is the participating group's.
  **All provisional**, marked in `PublicAPI.txt`, until task 2.9 fixes the surface of `0.2.0-preview.1`. test262's files
  whose features list `regexp-duplicate-named-groups` are judged by a host-free test where the library alone answers their
  assertions (15 of the 19 at the pin), the four whose assertions read a host's substitution or splitter waiting for task
  2.10's host job; the adversarial corpus's O07 and O08 answer their declared statuses, in a child process and in the
  Native AOT probe.

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
  Task 2.3 made both a `ReadOnlyDictionary` view of a map nothing else holds, with no signature change: a cast to
  `IDictionary` reported `IsReadOnly`, and a write threw `NotSupportedException`. Since task 2.6 both answer the
  immutable `RegexGroupNames` instead (R6, below), which implements no interface, so no cast reaches the names.
- **The old backend is deleted, and with it `RegexOverflowException` (R1, R4; task 2.4c, decision D20).** Every member
  matches through the new backend; `Matching/` (the continuation-passing matcher, its per-start `Budget` under
  `StepLimit`, and the greedy fast path's early exit) and the public `RegexOverflowException`, which reported that
  matcher's stack guard, are gone. `RegexMatch` and `RegexGroup` stay, as R8 allows (decision D12), moved from
  `Broiler.Regex.Matching` to `Broiler.Regex`, so `Match(string, int)` and `Matches(string)` answer the moved type. Each
  break is suppressed in `Broiler.Regex/CompatibilitySuppressions.xml` and listed in
  [`docs/suppressions.md`](docs/suppressions.md): the removed `RegexOverflowException` (CP0001, R1 and R4), the two
  moved types (CP0001, kept under R8 out of the namespace R1 and R4 delete) and the two members that answer them
  (CP0002). A caller that names the types recompiles with `using Broiler.Regex;`. A caller that catches
  `RegexOverflowException` reads `RegexMatch.Limit` instead: Broiler.JS's fallback to .NET's engine on that exception
  goes at its pin move, SJL-11.5.
- **The string-bearing members run the new backend under decision D10's policy (task 2.4c; R1, R3, D12).**
  `Match(string, int)`, `IsMatch` and `Matches` pass no meter and no step ceiling, as an engine that passes no meter
  does, and the engines' frame and trail ceilings. A match that reaches one is answered as that ceiling, never as no
  match: `Match` answers a failed `RegexMatch` whose new `Limit` names it, and `Matches` yields that failed match after
  the matches before it, as its last element (a consumer that reads every element as a match checks `Success`). `IsMatch`,
  whose `bool` has no room for a third answer, throws an `InvalidOperationException` naming it: **this departs from
  convention 3**, which admits an exception only for a broken precondition, and is tracked in `docs/conventions.md`
  ("Where Broiler.Regex stands") for task 2.9, which decides between the owner's admission of the exception for a
  member decision D12 keeps and a status-returning form. The offset entry is the status-returning form for a caller
  that expects to meet a ceiling.
- **`Compile`'s provisional signature is replaced (task 2.5; R2).** Task 2.3's
  `Compile(string, RegexFlags, IUnicodeTables, int, out BroilerRegex?, out RegexLimit)` is gone, replaced rather than
  overloaded by the signature above, which adds the compile budget and the meter. No published version carries
  either, so package validation against `0.1.0-preview.4` reads an addition and no suppression is added.
- **The strings of a `v`-mode class are an insertion-ordered hash set (task 2.5, plan row 2.5).** The parser searched
  a list before adding each string, so a union of operands of n strings in all made O(n²) comparisons, and an
  intersection or a difference of a and b strings O(a·b): fifty `\p{RGI_Emoji}` in one class, about 138,000 strings,
  made about 187 million comparisons. Each string is now hashed once per addition or lookup, O(its code units). The
  program compiler finds the table an earlier occurrence of the same strings built by a hash of them, where it compared
  every earlier table string by string. Internal; no signature changes.
- **`GroupNames` answers `RegexGroupNames` (R6; task 2.6, decision D12).** `BroilerRegex.GroupNames` and
  `RegexParser.GroupNames` were an `IReadOnlyDictionary<string, int>` of each name to one group, which cannot hold a name
  ES2025 lets several groups share; both now answer the library's own `RegexGroupNames` (Added). The two breaks, CP0002
  for `BroilerRegex.get_GroupNames` and `RegexParser.get_GroupNames`, are suppressed in
  `Broiler.Regex/CompatibilitySuppressions.xml` and listed in [`docs/suppressions.md`](docs/suppressions.md); no shim keeps
  the dictionary, which would answer a shared name wrongly. Convention 1 kept the dictionary out of a public signature, so
  the two C1 rows of the starting violation list close with it. A caller that read `GroupNames[name]` reads
  `GroupNames.GroupsNamed(name)`, and one that enumerated it reads `NameAt` to `Count`; `RegexMatch.NamedGroups` keeps its
  type. An instance made by `RegexParser`'s public constructor, which does not parse, held no name and holds none.

### Observable changes

- **A group name repeated only across alternatives that cannot both participate is admitted (R6; task 2.6).** Every
  repeated name was a `RegexSyntaxException` until this task; `(?<x>a)|(?<x>b)` compiles and matches `bab` at 0 with
  group 1 unmatched and group 2 `b`, and `(?:(?<x>a)|(?<x>b))\k<x>` matches `bb` and not `ab`. `RegexMatch.NamedGroups`
  maps a shared name to its participating group, or, when none of its groups participated, to the first of them,
  unmatched, and lists each name once, in the order of its first group; for a name declared once nothing changes. Every
  other repetition stays a `RegexSyntaxException` with the same message, which names the refused group's name, and its
  position is the `(` of the first group that might both participate with an earlier group of its name. Where an
  earlier repetition of a name is now admitted, that group comes later than the one `0.1.0-preview.4` refused, so the
  position moves, and the message can name another name: `(?:(?<a>x)|(?<a>y))(?<a>z)` is refused at 19, where it was
  refused at 11, and `(?<a>x)|(?<b>y)(?<a>z)(?<b>w)` at 22 for `b`, where it was refused at 15 for `a`. A pattern whose
  first repetition is refused, such as `(?<a>x)(?<a>y)` at 7, keeps its position.
- **Compilation is charged (R2; task 2.5).** `Compile` charges the caller's meter for the compilation and answers the
  compile budget as `LimitExceeded` naming `Compile` (Added). No compiled pattern answers a match differently: the
  sets, the folds and the strings a class holds, and the order it tries them in, are those of task 2.4c, as every
  assertion of a match's answer in task 2.4c's test suite, passing unchanged, and the new tests of the fold, which hold
  it to a fold of every code point, show. The suite's other edits change no match's answer: the fast-path
  differential's tally of cases the general lowering alone stops under its first ceiling grew from 16 to 64 only
  because task 2.5's tests added classes of `\p{RGI_Emoji}` to the literals its population is drawn from (without that
  file it still stops 16); the program compiler's answers gained `CompileBudgetExceeded`; the tests' `Compile` helper
  passes no budget and no meter; the corpus test runs the V rows it named as waiting, and checks their margin; and
  `EngineNamingTests` lists one more name. Compiling costs less where the work was quadratic or per code point:
  MaybeSimpleCaseFolding of an operand of more than 4,096 code points maps only the code points it shares with
  `Cased`, which holds every code point simple case folding moves (4,469 of `\p{L}`'s 145,672), and a class's strings
  are added and found by a hash (Changed). The sets of `\s` and of the folded `\w` are built the first time a
  compilation reads them, where every compilation built both.

- **The string-bearing members answer as the specification does where the old matcher did not (task 2.4c).** The
  three defects the differential comparison found are corrected: a start inside a surrogate pair under `u` and `v` runs
  the matcher from the pair, and a non-empty match found there reports the caller's start as its index and its
  substring from there (row M4, decision D23; the old matcher's greedy run stepped back and its loop did not, defect
  D1). The answers that change from such a start are rows 2 and 4 of `SurrogateStartTests`:
  `/(?:[\s\S])*\u{1F600}/u` from 1 of `"😀ba"`, no match before, now matches at 1, ending at 2; and
  `/(.{2})?\u{1F600}|()/u` from 1 of `"😀bcx"`, before an empty match at 1 with group 2 empty there, now matches the
  pair, reported at 1 and ending at 2, with neither group. A `v`-mode class tries its empty string after its single
  characters (D2: `/[\q{}a]/v` on `a` matches `a`); and under `u` and `v` a back-reference in a look-behind compares
  code points (M7: `/(?<=\1(.))x/u` on `"\uD83D\uDE00\uDE00x"` does not match). An empty match found at the pair,
  decision D41's implementation-defined case, is reported at the pair's start, Broiler.VM's realm's answer, where the
  old matcher reported the caller's start, or, for `/^/u` from 1 of `"😀"`, which it read from the low half as a lone
  unit, where `^` fails, no match.
- **Exhaustion is a ceiling, never no match (R1, R3; task 2.4c).** The old matcher gave each start position 10,000,000
  steps and answered no match, or a shorter match from its greedy fast path, when they ran out, and charged no step for
  an alternative tried. The string-bearing members now run a catastrophic pattern to its answer, which can take time
  exponential in the subject's length (the offset entry's meter and step ceiling bound it), and answer a frame or trail
  ceiling as a ceiling (Changed). The trail bounds a loop over a body of more than one character: `(a)*` over 500,000
  characters, `(?:ab){1048576}` over as many repetitions and `(?:){2147483647}` on the empty subject answer the trail
  ceiling, where the old matcher matched the first two and answered no match for the third (the specification answers
  an empty match). A quantifier over one character is a run with no trail cell, so `a{1048576}` and `[a]*` over
  10,000,001 characters match.
- **No match exhausts the stack (R4; task 2.4c).** Every shape the review of task 2.3 found to end the process or throw
  `RegexOverflowException` when matched at the nesting maximum on a 1 MiB thread matches there, under the JIT and
  Native AOT, and so do 5,000 capturing groups in a row, which ended the process before.
- **A pattern nested deeper than depth 512 is refused (R4; task 2.3, decision D6)**: more than 511 nested groups, or
  under `v` more than 511 nested classes, the pattern itself being depth 1. The constructors of
  `BroilerRegex` and `RegexParser.Parse`, which take no limit, parse under `BroilerRegex.MaximumNestingLimit` and
  refuse a deeper pattern with a `RegexSyntaxException` at the group or class that opens the level past it.
  `0.1.0-preview.4` parsed and compiled whatever its stack held and ended the process with a stack overflow past it:
  on a 1 MiB thread, at a few hundred levels for some shapes, and on any stack for a pattern such as 1,000,000 opening
  parentheses. A pattern within the maximum compiles and matches as before, and since task 2.4c no match exhausts the
  stack (above).
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
  across alternatives that cannot both participate was refused too until requirement R6, which task 2.6 brought (above).
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
