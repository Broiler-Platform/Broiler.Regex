# Broiler.Regex roadmap

This file contains only unfinished work. The parser, continuation-based matcher,
gap-feature routing, common `Exec` match data, and their completed issue-specific
milestones are represented by code and tests.

## 1. Complete Unicode semantics

Implemented: `\p{…}`/`\P{…}` resolve from the pinned Broiler.Unicode property tables
(General_Category, binary properties, `Script`, `Script_Extensions`, and the UTS #51
properties of strings); `v`-mode class-set intersection, subtraction, nesting and
`\q{…}` string alternatives are evaluated; and both branches of `Canonicalize` are
table-driven from the UCD revision named by `CaseFoldingData.UnicodeVersion`, including
`AllCharacters` and the fold-then-complement ordering `v` mode needs.

*Dated note, 2026-10-26 (task 2.2 of SJL-11.2, requirement R11 of section 6): "resolve from the pinned
Broiler.Unicode property tables" and "table-driven from the UCD revision named by
`CaseFoldingData.UnicodeVersion`" no longer describe the code. Property escapes, the properties of strings
and both branches of `Canonicalize` are read through `Broiler.Unicode.Algorithms` over the tables the caller
hands in as an `IUnicodeTables`, by the exact names ECMA-262 admits; `CaseFoldingData`, its generator and
`CaseFolding` are deleted, and the package declares no Unicode version of its own. The paragraph above is kept
as written.*

Remaining:

- Run the pinned test262 RegExp and UnicodeSets paths in CI under the expanded routing.
  Local evidence is `Broiler.Regex.Tests` plus 219,252 differential pattern/flag/input
  cases against V8 — two hand-built corpora and one seeded fuzz — compared on match index
  and every capture. The only remaining mismatches are the two deliberate divergences
  recorded in `Broiler.Regex/README.md`.
- Decide whether to keep following §22.2.2.9 under `vi` for a lone binary property's set
  and for a one-character `\q{…}` alternative, or to match V8, which folds neither
  consistently.

Exit gate: the relevant pinned test262 RegExp and UnicodeSets paths pass without routing
to .NET for unsupported Broiler features.

## 2. Close current RegExp failures

Broiler.JS currently tracks failures involving Annex B leading/trailing escapes,
regular-expression literals, and Unicode ignore-case behavior. For every failure:

1. add the smallest Broiler.Regex or Broiler.JS integration regression;
2. identify whether parsing, matcher semantics, built-in metadata, or host integration
   owns it;
3. rerun the focused upstream path and the full affected shard.

The integration repository's `scripts/compliance/test262-failures.txt` is the exact path
source; do not duplicate its changing list here.

## 3. Replace the translator

Routing now covers **every Unicode-mode (`u`/`v`) pattern Broiler can build**, plus the
non-Unicode gap shapes where .NET is wrong. `Exec`, `Split` and `Replace` consume one
match-data abstraction (`JSRegExp.EnumerateMatches` over `RunMatch`), so the legacy
`String.prototype.split`/`replace` fallback and `assert.match` answer through the routed
engine; `IJSRegExp.Value` is retired for an engine-agnostic `IsMatch`; and a routed
pattern no longer has to be translatable to .NET (`CreateRegex` runs with a null `value`
when a transform or `new Regex` cannot represent it). Widening to all of Unicode mode
fixed real match bugs the .NET translation had for non-gap patterns — a standalone
`\p{…}` under `i` threw, and in-class case folding (`[α-ω]/iu` against `µ`) was missed —
validated by 12,792 differential cases against V8 whose only mismatches are the two
documented `vi` divergences. Quantifier repetition is now iterative for every body shape —
a single-code-point body (`.*`, `\d+`, `[^"]*`) through a linear fast path, any other body
(a capturing group, an alternation of sequences) through an explicit-stack RepeatMatcher —
so a repeat over a subject of any length matches natively. Native recursion is bounded by
the pattern's nesting depth (like the parser), never by the subject, so the
`RegexOverflowException` backstop and its .NET fallback are no longer reachable by input
size; they remain only for a pathologically nested pattern. Validated by 3,008
complex-body differential cases against V8 (nested quantifiers, empty-capable bodies,
backreferences, lazy, multi-capture, look-behind bodies) with zero mismatches.

Remaining before the translator can be dropped:

- preserve global/sticky `lastIndex`, captures, named groups, indices, replacement
  substitutions, and species behavior — the shared path leaves `lastIndex` untouched for
  the legacy helpers, exactly as the .NET path did;
- widen routing to non-Unicode patterns (currently kept on the faster .NET engine for the
  common ASCII/hot-loop case) once the matcher is optimized, then remove the translator —
  this is now the only remaining gate, since no Unicode-mode pattern falls back on overflow.

Exit gate: all supported patterns use Broiler.Regex, the pinned RegExp corpus is green,
and no production path constructs a .NET `Regex` for ECMAScript matching.

## 4. Performance after correctness

This is the gate on routing non-Unicode patterns (§3): a hot-pattern microbenchmark against
the compiled .NET engine measured the interpreter at ~10× slower on average, so routing the
common ASCII path today would be a large regression for no correctness gain.

A first allocation pass has been done and is measured, with identical capture/ordering
semantics (differential-verified against V8):

- `MatchState` is a `readonly struct`, so deriving a state on every atom no longer allocates;
- the single-code-point quantifier fast path scans and backtracks with no per-iteration list
  (it steps back one code point to recompute an earlier cursor);
- one budget and one initial capture array are reused across a run's start positions;
- a run of ≥2 single-code-point atoms in a forward sequence folds into one loop rather than a
  continuation closure per term.

*(Superseded in part 2026-10-20, at work package SJL-11.1, by R1 as section 6's record of that date
adopts it. One item above reads "one budget and one initial capture array are reused across a run's
start positions;". The budget it reuses is reset to a fresh allowance at each start position,
which is the per-start reset R1 removes: from that date the work of a call, across every start
position, is charged to the caller's meter. The item is kept as the record of what the first
allocation pass did; its capture-array half is not affected.)*

That brings simple patterns (`\d+`, `[a-z]+`, `\w+`, class scans) to ~2–3× the compiled .NET
engine and the mean over the microbenchmark from ~10× to ~6×. The remaining cost is
structural: a sequence with capturing groups or nested quantifiers still allocates a
continuation-closure chain per invocation, and each capture write clones the capture array.
Removing those needs the compiled/bytecode path with an explicit backtrack stack (defunction-
alised continuations) — the next step, and the one that makes non-Unicode routing viable.
A `v`-mode class under `i` also applies MaybeSimpleCaseFolding by walking every code point of
each operand at compile time, so an operand the size of `\p{L}` is measurable per
`new BroilerRegex`. Any optimization must retain explicit timeout/backtracking limits and
identical capture/ordering semantics.

## 5. Preview gate

- Run unit, Broiler.JS integration, focused test262, fuzz, timeout, and allocation
  suites.
- Verify the nested Broiler.Unicode revision and generated-data provenance.
- Update the ECMAScript mapping when a previously stubbed production is implemented.
- Update `HUMAN_REVIEW.md` for the exact commit and scope.

## 6. Shared JavaScript libraries: the requirements of milestone SJL-11

*Added 2026-10-12, at milestone SJL-0 of the shared JavaScript libraries programme, whose plan,
ledger and decision register live in the Broiler.VM repository
(`docs/javascript-shared-libraries/`, with the area design in `areas/regexp.md` and the milestone in
`milestones/sjl-11-regexp.md`).*

On 2026-10-11 the programme took decision **SJL-D-14 (a)**: Broiler.Regex is fixed until it meets
every requirement below, and then both JavaScript engines - Broiler.JS and Broiler.VM's JavaScript
profile - move onto it. Broiler.VM's own matcher is deleted at that cut-over rather than moved into
this package, and Broiler.JS's .NET translator retires. Under **SJL-D-14 (i)** duplicate named groups
are implemented here before Broiler.JS retires its translator, so that Broiler.JS loses no feature it
has today. The requirements are Broiler.VM's, because its matcher is the one that has them today.

**Owner of every item: the Broiler.RegEx maintainer.** The JavaScript profile owner and the Broiler.JS
maintainer co-own the cut-overs that follow; all three roles are held by one person, and no item here
is independently confirmed by a second reader.

| # | Item | Today | Exit criterion |
|---|---|---|---|
| R1 | **A caller-owned work budget.** Every unit of matching work is charged through the caller's meter, in blocks, before the work. Exhaustion is a distinct result, never "no match" and never a shorter match. Cancellation reaches the matcher through the meter. | `Matching/MatchState.cs`'s `Budget` gives each start position of a run a fresh fixed allowance (`Budget.Reset` in `Matcher`), exhaustion answers "no match", the greedy fast path can answer a shorter match when its allowance runs out, and nothing cancels a match. | `Budget`, the per-start reset and the shortened match are gone; a pattern whose budget runs out reports exhaustion, which an adversarial corpus shows for catastrophic backtracking, deep nesting and large `v`-mode class expansions. |
| R2 | **Compile-time charging** in proportion to the work, set expansions included. | Compilation is not charged; a `v`-mode class under `i` walks every code point of each operand (section 4). | The compile path charges the same meter, and the adversarial corpus's large expansions are refused by the budget. |
| R3 | **Bounded memory.** Ceilings on backtracking frames and the undo trail, from the caller's policy, reported as exhaustion. | No ceiling beyond the step limit. | Each ceiling is a caller policy with a test that reaches it. |
| R4 | **Bounded nesting without CLR recursion**, in the parser, the compiler and the matcher. | The parser and the compiler recurse without a guard, and the continuation chain is bounded by nesting depth; a deeply nested pattern can terminate the process, at a depth that differs between JIT and Native AOT. | A nesting limit checked before the stack is, an explicit stack where recursion remains, and a nesting corpus refused under JIT and Native AOT alike. |
| R5 | **Exact ECMAScript syntax**: property names and values exactly as ECMA-262's tables list them, `Katakana_Or_Hiragana` included; group names validated against the shared identifier classes (milestone SJL-7); Annex B syntax only where the specification allows it. | Property resolution reads the pinned Broiler.Unicode tables (section 1). | test262's `built-ins/RegExp` and literal syntax tests at the pin, run in this repository's CI. |
| R6 | **Duplicate named groups**, as ES2025 defines them (SJL-D-14 (i)). | Not supported; only Broiler.JS's .NET translator supports them. | Implemented here, with test262's duplicate-named-groups tests passing, **before** Broiler.JS retires its translator. |
| R7 | **An anchored match** for sticky matching and protocol use. | `Match(input, start)` searches forward from `start`. | An anchored-match argument with tests. |
| R8 | **An offset-only result interface**: captures are positions, and strings are the caller's choice. | `BroilerRegex.Match` returns a `RegexMatch` whose match and groups carry their `Value` strings. | A result of positions only, with no eager substring; the string-bearing `RegexMatch` may stay for its existing consumers. |
| R9 | **Text helpers**: flag parsing, `RegExp.escape` over the specification's whitespace set, and escaping of `source`. | Flag parsing exists (`RegexFlags.cs`). | Each helper exists with tests over the specification's sets. |
| R10 | **Process**: `IsAotCompatible` and `IsTrimmable` declared with no warning; a Native AOT probe in CI; test262's `RegExp` directories in CI at the pinned revision; a fuzz target; the programme's `SL` conventions; an assurance gate (decision SJL-D-5 (a)). | The package publishes under Native AOT without a warning but declares neither flag; CI runs no test262 and no assurance gate. | Each runs in this repository's CI and is green. |
| R11 | **Data through the Unicode owner's provider** (milestone SJL-7, decision SJL-D-6 (c)): Unicode property, folding and string-property data reach the engine through a provider, so the engine brings no table into a JavaScript engine's closure. | The tables are compiled into the package. | The matcher reads its tables through the provider, and the case-folding table it shares with Broiler.VM's is one generated table in the Unicode owner. |

**What this changes in sections 3 and 4.** Section 3's exit gate stands: no production path constructs
a .NET `Regex` for ECMAScript matching. Section 4's performance work keeps its rule that explicit
backtracking limits are retained, and those limits become R1's caller-owned budget: the "one budget
reused across a run's start positions" of section 4 is the per-start reset R1 removes. A fix to this
engine reaches both JavaScript engines through a release of this package and each engine's own pin
move; neither engine patches it locally.

### Record of 2026-10-20 (SJL-11.1): R1 to R11 adopted as owner conditions

*Added 2026-10-20, at work package SJL-11.1 of the shared JavaScript libraries programme, by the
Broiler.RegEx maintainer. Every decision named here is the owner's, taken as recommended on 2026-10-20
under the owner's direction of 2026-10-18, "Please do all recommended decisions and skip waiting for
review.", which changes no programme rule. The decisions are numbered as the area verdict numbers them.
The table above is not edited; this record reads it.*

**The area's record.** The programme's area verdict for regular expressions is Broiler.VM's JSD-0081,
`src/Broiler.VM.Profile.JavaScript/docs/decisions/0081-regular-expressions-move-to-broiler-regex-under-its-owner-conditions.md`
in the Broiler.VM repository, filed in the same set of changes as this record. It accepts the area in its
sub-areas S (syntax and early errors, with the literal validation of Broiler.VM's compiler), M
(compilation and matching), B (budget and limits), F (flags), T (`RegExp.escape` and `source`) and U
(data use), and places it here under R1 to R11 as owner conditions (decision D1). Its input is the
correspondence from code, Broiler.VM's `docs/evidence/sjl-11-001/correspondence.md`, which reads this
repository's code at the revisions it names.

**R1 to R11 are adopted as owner conditions.** This repository meets them at SJL-11.2. The milestone's
component clauses judge them by bundle [`sjl-11-001`](evidence/sjl-11-001/README.md)'s rule, at the
candidate reading (the commit `0.2.0-preview.1` is packed from) and at the end reading.

**The refusal path.** If any of R1 to R11 is not met by the end of SJL-11.2, the area is refused:
neither engine moves onto this package, and each keeps its own regular-expression engine with a source
pointer to JSD-0081 (the programme's `risks.md`, rows D8 and D10). A part of the bundle's rule that
reads NOT MET at the candidate reading blocks both cut-overs, because an engine cut over before every
requirement holds is risk D10's stop. Nothing here weakens that rule.

**R4, read (decision D6).** "Bounded nesting without CLR recursion" is read as **no CLR recursion in the
matcher**. The parser and the compiler may recurse, bounded by the caller's nesting limit, which is
checked before each descent and answered with the nesting status, under a documented library maximum
shown to complete on the smallest stack the adversarial corpus declares, under the JIT and in the Native
AOT probe. This is R4's exit criterion ("a nesting limit checked before the stack is, an explicit stack
where recursion remains") and the reading of the area design's requirement 4 and of the milestone's
"nesting bounded without CLR recursion". Rewriting a parser that is not the defect into explicit stacks
is not required.

*Dated note, 2026-10-26 (task 2.3 of SJL-11.2, R4): the parser and the closure compiler check the caller's
nesting limit before each descent and answer `RegexStatus.LimitExceeded` with `RegexLimit.Nesting`, under
`BroilerRegex.MaximumNestingLimit` (512). On linux-x64, a pattern of every shape measured at the task's review,
nested to the maximum, parses and compiles on a 1 MiB thread under the JIT and Native AOT; win-x64 was not
measured. The matcher's half of R4 is task 2.4a's: until its matcher replaces the continuations, matching at the
maximum is proven only for the shapes `NestingMaximumTests.MatchedShapes` lists, and the review's mixtures that
exhaust a 1 MiB stack at match time within the maximum are task 2.4a's open items, each listed with the depth it
first failed at in the package README's known limitations and kept as a skipped test in `NestingMaximumTests`,
which task 2.4a runs.*

**R5, R6 and R10's test262, read (decision D14).** A library cannot run test262 alone, so "test262 ...
in this repository's CI" is read as a host job and a host-free library job. The **host job** checks out Broiler.JS at a retained commit
named by a dated record in this file, packs this commit's package into a local feed listed first, builds
Broiler.JS's shell against it and runs test262's `RegExp` paths at the programme's pin with negative
tests; it holds an expected-failures list in which every entry belongs to the host's object model, and
every test of duplicate named groups passes. The **host-free library job** runs the literal syntax tests,
`property-escapes/generated` and `unicodeSets/generated` under the JIT and through the Native AOT probe.
The retained commit is the branch of SJL-11.5 until it merges, then Broiler.JS's default branch; no
commit is named yet. R5's, R6's and R10's exit criteria are read as met when these jobs run green at a
reading's commit.

**The backend (decision D5).** The matcher is replaced by a new backtracking machine written here: a
compiled instruction program run by a loop with an explicit frame stack and an undo trail
(defunctionalised continuations). Broiler.VM's matcher is its specification, for its design and for its
charging units (decision D8), and **no code is ported from it**, as decision SJL-D-14 requires: it took
(a), under which Broiler.VM's matcher is deleted, and not (b), which would have made that matcher this
package's implementation. The parser, the public syntax tree, the `CodePointSet` algebra and the flag
parser stay; the fast paths are re-expressed as instructions that charge the meter. This is the step
section 4 already names, "the compiled/bytecode path with an explicit backtrack stack".

**The meter and the statuses (decision D9).** The meter is this repository's own nullable delegate,
`RegexWorkCharge`, which receives units and is charged in blocks before the work they pay for (decision
D8). A call answers with a status of this repository's own: a match; no match; a limit exceeded, naming
its dimension (steps, frames, trail, nesting or compile); or a syntax status with a code, a message
identifier and a position. No expected failure is an exception. A meter that throws propagates its
exception, and the instance it was charged for then answers a new call as a new instance would.

*Dated record, 2026-10-26 (task 2.4a of SJL-11.2; R1, R3, R4's matcher half, R7 and R8; decisions D5, D8, D9 and
D10).*

**What the new backend runs.** `Compiling/ProgramCompiler.cs` lowers the syntax tree to a `RegexProgram` on work
lists of the heap, entering each group through the caller's nesting limit (`RegexNesting.Descend`), and
`Engine/RegexMachine.cs` runs it in one loop with an explicit frame stack and an undo trail: a frame is three integers
(the address to resume, the position, the trail's height), a trail cell two (the register and the value it held).
No method of `Broiler.Regex.Engine` reaches itself in the IL call graph (`MatcherRecursionTests`, check A-4's matcher
half, with a planted recursion watched failing). The program holds literals without case folding, `.` with and
without `s`, classes as code-point ranges (under `u`, without it, and `v`-mode classes of code points), sequence,
alternation, `* + ? {n} {n,} {n,m}` greedy and lazy with the specification's empty-iteration rule, and capturing,
named and non-capturing groups, a quantified atom's captures cleared at each iteration. The unit is one instruction
dispatched; a class test costs one plus the floor of the base-2 logarithm of its range count, the steps of the binary
search it does, which is how this library reads D8's "a property class's weight per character tested" for classes
flattened to ranges; every other instruction costs one, a capture reset of any width included, as Broiler.VM charges
its own. The meter, `RegexWorkCharge`, is charged in whole blocks of 512 units before the work, so before the k-th unit
at least k have been charged and the over-charge is below one block; a call charges across all its start positions
with no reset. The caller's ceilings are steps
(optional), frames and trail, each reached answered as `LimitExceeded` naming it; a ceiling below 1, and a frame or
trail ceiling above the library's maxima of 2^24 frames and 2^25 cells, is a broken precondition
(`ArgumentOutOfRangeException`), and both engines pass D10's 2^20 and 2^21. The entry takes a subject span, a start, an
anchored argument independent of `y` and the caller's slots, and answers `Match`, `NoMatch` or `LimitExceeded`; its
registers, frames and trail are rented from the shared array pool and returned, so a call allocates nothing that grows
with the capture count (frames and trail past 65,536 integers are the call's own and go to the collector, so a call's
peak does not stay pooled), and the program is immutable and shared across threads. The entry, the program and the meter
are internal until task 2.4c; `RegexStatus.Match` and `NoMatch` and `RegexLimit.Steps`, `Frames` and `Trail` are public,
as an enum's members are, and recorded as provisional in the CHANGELOG.

**The transitional seam.** For a tree holding an assertion (`^ $ \b \B`), a back-reference, a look-around, a
modifier group, the `i` flag or a `v`-mode class of strings, the compiler answers
`ProgramCompilation.NotYetSupported`: no program, nothing thrown. Task 2.4b adds those instructions and removes the
answer. Task 2.4c adds the fast paths (a greedy run over a class as one instruction, the corpus's presumption 3;
atom-run folding; the literal-prefix prefilter), routes `BroilerRegex` through the new backend and deletes
`Matching/`, `Budget`, `StepLimit` and `RegexOverflowException`; the skipped theory of `NestingMaximumTests` runs then,
and the corpus's rows O05 and O06, whose 10,000,001-character run passes the frame ceiling in the loop form, match then.

**The evidence.** Unit tests per instruction, per quantifier form and per ceiling; a test of the charging property of
check A-1 with a recording meter over the corpus's declared inputs; a differential test of the two backends over every
string literal of the test project in the subset (4,320 pattern and flag pairs and 251,884 cases at this record) and a seeded population
of 20,000 generated patterns (173,428 cases), all agreeing; and the corpus's 36 rows within the subset, each run in a
child process through the probe's hidden argument `--engine-row` on the declared 1 MiB stack, each answering its
declared status. The probe's report carries the new backend's cases, the flat sequences of 100,000 terms and the
nesting maximum among them, under Native AOT as under the JIT.

**Found on the way: the old backend's defect D1.** Under `u` or `v`, the old matcher's greedy run of one character
gives back across a start inside a surrogate pair, joining the low half to the high half before the start, so
`Match` from such a start can answer a match of a character before it, and a group captured before the match. The new
backend reads the low half as a character of its own and never reads before the start, the answer of ECMA-262
§22.2.2's forward matchers; a test pins it. The public members keep the defect until task 2.4c.

**R11, read (decision D11).** Unicode data reaches the library through `Broiler.Unicode.Algorithms`
`0.1.0-preview.5` alone, the version both engines admit, and compilation takes an `IUnicodeTables`.
`CaseFolding`, its generated table and its generator are deleted. The tests and the probe read
`Broiler.Unicode.Tables` `0.1.0-preview.5` as test-only data. This package declares no data version of its
own; the provider's is read. The programme's convention 1 is read as admitting the provider contract
`IUnicodeTables` in a public signature (decision SJL-D-6 (c)).

**The version and the break (decision D12).** The SJL-11 work breaks the `0.1` surface: R11 removes the
constructors that take no tables and the public `CaseFolding` and `UnicodeCharSets`; R6 reshapes
`GroupNames`; and the new backend that meets R1 and R4 (decision D5) removes `RegexOverflowException`, the
old matcher's stack guard (decision D20). The SJL-11 surface ships as **`0.2.0-preview.1`**, validated against
`0.1.0-preview.4` with every break suppressed and listed with its R item. The string-bearing `RegexMatch`
stays, as R8 allows; no shim is kept for a member R11 makes impossible. Decision SJL-D-3's "inside a
major version" is read, for a `0.x` preview, as inside a `0.y` minor version. **No release of either
engine depends on `0.2` before both cut-overs have merged**, and Broiler.JS moves its pin only at
SJL-11.5. `0.2.0-preview.1` is published to nuget.org only with the owner's approval (decision D13).

*Dated note, 2026-10-26 (task 2.3 of SJL-11.2, R10: conventions 7 and 10): the starting violation list left
the owner of `RegexMatch.Empty`'s SL2 row unassigned, to be decided at task 2.3. Task 2.3 took it, as its row
asks for no mutable static anywhere in the library: `RegexMatch` sits in `Matching/`, outside the folders that
row names, but the string-bearing `RegexMatch` outlives task 2.4c's replacement of the matcher (row 2.9,
decision D12), and leaving a one-line repair to task 2.9 would have carried a known breach of convention 7
through four more steps. `RegexMatch.Empty` now holds an empty read-only dictionary and an empty array, and
the row has left the list.*

**Also carried here.**
- **Review (decision D30).** `HUMAN_REVIEW.md` keeps its record and carries a dated note of this date: the
  commit it reviewed, `d5b07a00`, is an ancestor of `main`; the source under `Broiler.Regex/` changed after it,
  before `c1422f4`, the commit `0.1.0-preview.4` was packed from; so, by the record's own rule, no review covers
  the current code.
  A Broiler.VM release that depends on this package waits until the revision it depends on carries a
  completed review (decision SJL-D-5 (a)); development use does not.
- **Thread safety (decision D31).** `CodePointSet` is immutable after construction, no static field is
  mutable, and concurrent compile and match on shared instances answer as single-threaded calls do.
- **The fuzz target (decision D32).** A seeded grammar-based fuzzer runs in CI with every limit and a
  meter set, and a planted defect in a declared parser path is found by its declared seed.

**Section 4, superseded in part.** Section 4's item "one budget and one initial capture array are reused
across a run's start positions;" describes the per-start allowance R1 removes. It is superseded by R1
from this date, as the dated note beside it says, and is kept as written.

**The rules.** Bundle `sjl-11-001`'s rules are committed with this record, before any code of SJL-11.2:
[`decision-rule.md`](evidence/sjl-11-001/decision-rule.md) (R1 to R11 as a contract, test262 in this
repository's CI, the adversarial corpus under the JIT and Native AOT, the vectors at library level, and
the process) and [`reference-arms.md`](evidence/sjl-11-001/reference-arms.md) (the library against both
engines' implementations before their cut-overs). With them come the adversarial corpus,
[`tests/Adversarial/corpus.tsv`](../tests/Adversarial/corpus.tsv), and
[`eng/adversarial-programs.py`](../eng/adversarial-programs.py), which writes both engines' programs from it.
