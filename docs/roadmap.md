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

*(Dated note, 2026-10-27, task 2.5 of SJL-11.2: MaybeSimpleCaseFolding of an operand of more than 4,096 code points
now maps only the code points it shares with `Cased`, which holds every code point simple case folding moves, so its
work is in proportion to the operand's ranges: 4,469 code points of `\p{L}`'s 145,672, and the fold is charged to the
caller's compile meter through `Compile`. Section 6's record of that date has the units.)*

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

**What the new backend runs.** `Compiling/ProgramCompiler.cs` lowers the syntax tree to a `RegexProgram` on work lists
of the heap, entering each group through the caller's nesting limit (`RegexNesting.Descend`), and
`Engine/RegexMachine.cs` runs it in one loop with an explicit frame stack and an undo trail: a frame is three integers
(the address to resume, the position, the trail's height), a trail cell two (the register and the value it held). No
method of `Broiler.Regex.Engine` reaches itself in the IL call graph (`MatcherRecursionTests`, check A-4's matcher half,
with a planted recursion watched failing). The program holds literals without case folding, `.` with and without `s`,
classes as code-point ranges (under `u`, without it, and `v`-mode classes of code points), sequence, alternation,
`* + ? {n} {n,} {n,m}` greedy and lazy with the specification's empty-iteration rule, and capturing, named and
non-capturing groups, a quantified atom's captures cleared at each iteration. The unit is one instruction dispatched; a
class test costs one plus the floor of the base-2 logarithm of its range count, the steps of the binary search it does,
which is how this library reads D8's "a property class's weight per character tested" for classes flattened to ranges;
every other instruction costs one, a capture reset of any width included, as Broiler.VM charges its own. The meter,
`RegexWorkCharge`, is charged in whole blocks of 512 units before the work, so before the k-th unit at least k have been
charged and the over-charge is below one block; a call charges across all its start positions with no reset. The
caller's ceilings are steps (optional), frames and trail, each reached answered as `LimitExceeded` naming it; a ceiling
below 1, and a frame or trail ceiling above the library's maxima of 2^24 frames and 2^25 cells, is a broken precondition
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

**A bound the trail sets, for tasks 2.4b and 2.4c.** The trail keeps every write that changes a register until the
attempt ends or backtracking undoes it, whether or not a choice is open, as Broiler.VM's does: the cells of a loop that
holds no choice serve only the registers' reset when the attempt fails. A counted quantifier writes two an iteration
whose body consumes (its mark and its count), so under D10's engine ceilings it runs at most 1,048,575 of them:
`a{1048576}`, `(?:ab){1100000}` over as many repetitions and `(?:){2147483647}` on an empty subject answer
`LimitExceeded` naming the trail where the old backend matches (or, for the last, answers no match where the
specification answers an empty match). At the library's maximum trail the bound is 16,777,215 iterations. `CeilingTests`
pins it. Task 2.4c, when it routes `BroilerRegex` through the new backend, either lists these answers as changed or
makes a write with no frame pending leave no cell, resetting the registers another way at a failed start; task 2.4c's
run instruction does not lift the bound for a body of more than one character.

**The evidence.** Unit tests per instruction, per quantifier form and per ceiling; a test of check A-1 that reads a
recording meter at every dispatch, through an observer the library never sets, over a declared set of eleven inputs
(watched failing on a meter paid one unit late); a differential test of the two backends over every string literal of
the test project in the subset (4,523 pattern and flag pairs and 264,486 cases at this record) and a seeded population
of 20,000 generated patterns (173,428 cases), all agreeing, with the cases a ceiling stops pinned (none of the suite's,
the seed's 22 at the step ceiling and none of either at a frame or trail ceiling) and a floor on the cases compared; and
the corpus's 36 rows within the subset, each run in a child process through the probe's hidden argument `--engine-row`
on the declared 1 MiB stack, each answering its declared status. The probe's report carries the new backend's cases, the
flat sequences of 100,000 terms and the nesting maximum among them, under Native AOT as under the JIT. Check A-4's IL
test refuses, over every method the namespace reaches, each dispatch its call graph cannot follow (a function pointer, a
delegate other than the meter, a virtual call into the assembly, `calli`), with five planted shapes watched refused; and
the namespace declares none of the names of Broiler.VM's matcher's private members that this record's review found
copied (decision D5).

**Found on the way: a start inside a surrogate pair, and the old backend's defect D1.** Under `u` or `v`, ECMA-262's
matchers read code points, so a code-unit start inside a pair is no index of their input; the only meaning the
specification gives one is RegExpBuiltinExec's `lastIndex` (§22.2.7.2), whose step 13.b starts the matcher at the whole
pair and whose `"index"` is `lastIndex` itself. The new backend's entry reads such a start forward from the low half, as
a lone unit, and reads nothing before it: that is its contract, and the step back to the pair is its caller's, as
Broiler.VM's realm takes it (JSD-0081's X6 keeps the step in `RegExpMatchOne`). With the step the entry answers as
RegExpBuiltinExec does (`SurrogateStartTests`); without it, it does not, and this record's first reading, that its
answer was §22.2.2's, was wrong. The old backend is inconsistent, and that is defect D1: its greedy run of one character
steps back across the start and its general loop does not, so `/[\s\S]*\u{1F600}/u` from 1 of `"😀ba"` matches at 1 and
`/(?:[\s\S])*\u{1F600}/u` does not. **Task 2.4c's obligation:** the route of `BroilerRegex.Match(input, start)` to the
entry steps back to the pair before it calls, and decides the index the public members report from such a start:
`lastIndex`, as the specification's text and JSD-0081's row M4 have it for a non-empty match, or the pair's start, as
Broiler.VM's realm and V8 report today (row M4 lists Broiler.VM's as that family's defect), with decision D41's empty
match apart. Its CHANGELOG lists the answers that change from such a start (rows 2 and 4 of `SurrogateStartTests`,
skipped until then). SJL-11.4's and SJL-11.5's adapters take the step for their engines.

*Dated record, 2026-10-26 (task 2.4b of SJL-11.2; R4's matcher half and R7; decisions D5 and D8).*

**What the new backend runs.** Every pattern the parser accepts: `ProgramCompilation` answers `Compiled` or
`NestingLimitExceeded`, and task 2.4a's `NotYetSupported` is deleted (a test reads the enumeration, every member name of
the assembly and its user-string heap, and finds it in none). The instructions added are `LiteralIgnoringCase`,
`ClassIgnoringCase` and `CanonicalClass` (the `i` flag: Canonicalize's two branches read at match time from the
provider's case folding, a class tested by the character's case orbit and inverted after, a `v`-mode class by the
character's Canonicalize against its folded members); `LineStart` and `LineEnd` (`^ $`, the `m` flag in force as an
operand); `WordEdge` and `NotWordEdge` (`\b \B`, the word class the flags give, U+017F and U+212A under `iu` and
`iv`); `Backreference` and `BackreferenceIgnoringCase` (a reference to a group that did not participate matches the
empty string; by code point under `u` and `v`); and `OpenLookaround` and `CloseLookaround`. An instruction carries
`RightToLeft` when it stands in a look-behind's body. Modifier groups need no instruction: the compiler lowers a body
under the flags it sets. A `v`-mode class holding strings lowers to an alternation of its strings, longest first, then
its single characters, then the empty string (§22.2.2.7, Atom :: CharacterClass).

**A look-around on the frame stack.** `OpenLookaround` pushes a frame like any other but with a negative address field,
a *barrier*, and writes its index into a register of the look-around's own (through the trail). When the body matches,
`CloseLookaround` sets the frame count back to the barrier's index, so the barrier and every frame the body pushed go at
once and nothing after the look-around can backtrack into it (§22.2.2.6, step 2.b: m runs with the continuation that
always succeeds); a positive look-around goes on from the barrier's position with the captures its body made, a negative
one fails, and the next frame popped undoes the body's writes. When every way through the body fails, backtracking
reaches the barrier: a positive one fails in its turn, and a negative one resumes after the look-around, from its
position, its body's writes undone (step 2.d, "Return c(x)"). No second loop, no call and no second stack: A-4 holds.
Under `i` the machine calls the Unicode owner's case folding, which reads the caller's provider during a match, the
caller's code as the meter is and outside the graph A-4 reads: A-4 now pins the types of other assemblies the machine
calls, so that a new one is reviewed, and refuses any type of the library that implements the provider's interface, so
that the provider stays the caller's (its witness plants one that matches again). **The backward direction**: a
look-behind's body is compiled with `RightToLeft` on each character, class and back-reference instruction, which then
reads the character that ends at the index (a whole surrogate pair under `u` and `v`) and moves the index to its start;
a sequence's terms are emitted last first and a group's end register is written first, so that its range reads left to
right; a back-reference compares the capture from its last character. A look-ahead's body is forward wherever it stands.
Under `u` and `v` a start inside a surrogate pair now splits the pair for every read of the call, so that a look-behind
from the start meets the high half alone.

**Charging (D8).** Each new instruction costs one unit; a class under `i` one plus its weight for each code point of an
orbit (four at most at UCD 17.0.0); `\b` and `\B` one plus the word class's weight twice; a back-reference its capture's
length in code units on top, charged after its dispatch and before it compares; popping a barrier nothing (the
instruction that pushed it paid). Check A-1's per-dispatch test gains inputs reaching every new instruction, and a
test that its declared inputs reach every operation, each that reads the subject in both directions. The order inside a
back-reference's dispatch, its length charged before its first character is compared, gives the same units, charges and
answer either way, so the per-dispatch read cannot see it; a test reads it through the caller's tables, which a
comparison under `i` reads through the case folding, forward and in a look-behind.

**Duplicate named groups stay task 2.6's.** The parser still refuses them. A back-reference's operands name a run of the
program's list of referenced groups (`A` its first entry, `B` its length), one group long today; task 2.6 makes the run
the groups that share the name, and the instruction compares the first whose capture is defined, R6's participating one.

**The evidence.** Unit tests per construct and per direction (`MachineConstructTests`, `MachineLookaroundTests`):
test262's `built-ins/RegExp/lookBehind` cases, the specification's look-ahead notes, the issue #923 rows 3, 4, 6, 7 and
8 with their backward forms, a negative look-around's captures reset, a back-reference in a look-behind to a group
captured later in source order, modifiers nested and undone, `\b` under `iu` and `iv`, the non-`u` Canonicalize's guard
and `v` strings longest first. **Ceilings and A-8**: every step, frame and trail ceiling met through look-arounds of the
four kinds and through back-references answers that ceiling or the full answer, a negative look-around whose body
reaches one answering the ceiling and not a match (`CeilingTests`), and check A-8 holds for back-references, named ones
under `u`, references under `i`, look-arounds and classes under `iu` with word boundaries, at 1 to 1,000 groups
(`EntryTests`). **The old suite on the new backend**: `TestRegex` is the switch, and `NewBackendSuiteTests` runs every
case of every behaviour test again on `BroilerRegex`'s internal route through the new backend (265 methods, 1,475 cases
at this record, 722 of them building an instance on the route), the 18 cases xunit skips until task 2.4c among them;
every case passes. **The differential over the whole grammar**: the suite population (14,164 pattern and flag pairs,
1,286,531 cases) and a seeded population drawing every construct under thirteen flag strings (14,028 pairs, 242,607
cases); no disagreement is unexplained: 203 suite cases and 57 generated ones are defect D2's, each confirmed by the old
backend's answer to the pattern with every class of D2's shape written `(?:C)?`, C the class without the empty string,
which equals the new backend's, and 8 suite cases M7's, by its shape; the counts are pinned for each population, as the
stops are. **The corpus**: K05, G03, L01 to L05, F01, F02, O02, O03, O16 and O17 answer their declared statuses in child
processes and in the probe; O05 and O06 wait for task 2.4c, O07 and O08 for task 2.6, the V rows for task 2.5 and the
validate rows for task 2.8.

**The old backend's defects the comparison found**, each pinned to the specification's answer in `OldBackendDefectTests`
and shown as the old backend gives it: **D2**, a `v`-mode class holding the empty string tries it before its single
characters (`/[\q{}a]/v` on `a` answers the empty match; the specification tries the empty string last), pinned by
`OldBackendDefectTests.D2_a_class_s_empty_string_is_tried_after_its_single_characters` and shown by
`OldBackendDefectTests.D2_the_old_backend_answers_with_the_empty_string_first`; and **M7**, Broiler.VM's JSD-0081 row
and decision D36's fix, a back-reference in a look-behind under `u` or `v` that compares the code units ending at the
index and so can begin inside a surrogate pair (`/(?<=\1(.))x/u` on `"\uD83D\uDE00\uDE00x"` matches where the
specification does not), pinned by `OldBackendDefectTests.M7_a_backward_back_reference_reads_whole_code_points_under_u`
and shown by `OldBackendDefectTests.M7_the_old_backend_compares_the_code_units_that_end_at_the_index`. D1 is task 2.4a's
(`SurrogateStartTests`). The public members show all three until task 2.4c routes them through the new backend.

**What task 2.4c adds.** The fast paths: a greedy run over a class as one instruction (rows O05 and O06, whose loop form
passes the frame ceiling), atom-run folding and the literal-prefix prefilter, and a remedy for a class of many strings,
whose alternation costs about two units a string at each position (`\p{RGI_Emoji}`, about 5,500 units a position). The
route: `BroilerRegex`'s internal route of task 2.4b, which steps back from a start inside a pair and reports the
caller's start (an empty match found at the pair, decision D41's case, at the pair's start, where the public members
report the caller's start), runs under the library's maxima and a step ceiling of 100,000,000 units, and throws
`RegexOverflowException` at any ceiling (where the public members answer no match when the old backend's step budget
runs out, and throw it only at its stack guard), becomes the public members' route with the ceilings, meter and statuses
task 2.4c decides; `Matching/` and the old backend's defects D1, D2 and M7 go with it, and the trail bound of task
2.4a's record is listed or lifted.

**What task 2.5 adds**, besides its V rows: a compile charge for the program's lowering of classes of strings. The
compiler lowers each occurrence of a class of strings in full and shares nothing between occurrences: a `Choice` and a
`Goto` for each string, a `Literal` for each of its characters and the class of its single characters
(`ProgramCompilerTests` pins it). `\p{RGI_Emoji}`, 13 characters of pattern, is 2,760 strings and 16,717 instructions at
each occurrence; its parse and compilation for the new backend allocate about 2,920 KiB, where the parse alone allocates
about 960 KiB and the old backend's constructor about 980 KiB; 100 occurrences, 1,300 characters, are 1,671,701
instructions and about 231 MiB (the parse about 93 MiB, the old constructor about 96 MiB). Task 2.5's compile meter
(decision D8's "in proportion to the expansion") must charge the lowered strings, the characters and strings each
occurrence emits, and not only what the parser expands; or the compiler lowers each class of strings once, into a table
its occurrences share. Until then only the internal route and the tests compile such a program; task 2.4c puts it on the
public constructor's path, so task 2.5's charge, or the shared table, comes with it or before.

*Dated record, 2026-10-26 (task 2.4c of SJL-11.2; R1, R3, R4, R7 and R8; decisions D5, D8, D9, D10, D12, D20, D23
and D41).*

**The fast paths, under the meter (D8).** Four instructions, each owning the instructions after it, which it reads as
data: `GreedyRun` and `LazyRun` (a quantifier over one character or a class without strings: the required characters,
then greedy as many more as hold and lazy none, one frame for the whole run that gives back or takes one character at a
time, no trail cell), `AtomRun` (two or more one-character atoms in a row, read by one dispatch in the order they are
matched) and `StringClass` (a `v`-mode class of strings looked up in a table of its strings keyed by the character a
string begins with, its last right to left, shared by every occurrence of the same strings in one direction and
folding). A frame gains a fourth integer, a mark, so a run's or a class of strings' frame stays and moves on. The
literal-prefix prefilter is the program's first test, the one-character test every attempt makes before any choice,
past the group starts: a search tests each position with it and enters the program only where it holds. The units are
Broiler.VM's for the same work, its matcher read as their specification only (decision D5): a run's dispatch one unit
and each character it examines, the one that ends it included, its atom's cost, charged before the read; an atom run
its atoms' costs; a class of strings one plus the weight of its first characters and a unit for each character of a
string tried; each position the scan tests its first test's cost, one plus the class's weight, charged before the test
is made, the attempt at the position admitted taking that test as its own first test, neither made nor charged again
(so a skip costs its test, and the admitted position what the attempt alone would), and the subject's end no attempt
and no unit. Check A-1 reads each through the observer at every unit charged, its declared inputs reaching every new
instruction in both directions and the scan over a character, over a class and into a step ceiling; and it reads the
order inside each, which a charge made just after its read would leave unchanged at every unit: under `i` each
character read reads the caller's tables once, and `ChargingTests` takes the units charged at each read of the tables
for a greedy and a lazy run (both loops and the lazy frame's next character), an atom run, a folded class of strings
and its single characters, and the scan, over a character, a run's atom, an atom run's first atom and after a group's
start, and into a step ceiling. Check A-1's "a non-matching subject of n positions charges at least n units" is read with
n the subject's characters: the search's last position, the end, holds no character, and under the scan it is neither
tested nor charged, as Broiler.VM's scan charges it nothing, so `b` over n characters costs n units, and over the
empty subject none, the meter uncalled even under a step ceiling of one unit; without a first test the end is an
attempt and costs a unit (`ChargingTests`). *Amended at the review of task 2.4c:* the scan first tested each position and charged
it after, and charged the admitted position nothing in the scan; it now charges before the test, as the contract's
"prepaid" reads, with the same units. Each place's mutant that reads before it charges (both loops of a run, the lazy
frame, the atom run's later atoms, a string's character, a class of strings' single characters, the scan), the run
charged once per run, and an admitted test made again each fail that check.
The corpus's rows **O05 and O06** (`[a]*` and `^[a]*$` over 10,000,001 characters) match, in a child process and in the
probe, where the loop form reached the frame ceiling; presumption 3 holds. `\p{RGI_Emoji}` at a position no emoji
begins at costs under 60 units, where its strings tried one by one cost over 5,000, and the remedy task 2.4b's record
handed task 2.4c for its compilation is the shared table: an occurrence is three instructions, and its construction
allocates about 1.3 MiB, of which the parse is about 0.95 MiB; 100 occurrences about 100 MiB, of which the parse about
94 MiB, which is task 2.5's to charge (2.4b's record: 2,920 KiB and 231 MiB).

**The differential of the fast paths.** The program compiler's internal `fastPaths` option turns them off, the general
lowering; `FastPathDifferentialTests` runs every case with both programs under the same ceilings (100,000 units and the
engines' frames and trail) and compares the status, the ceiling named, the index, the length and every capture; a case
either program stops at the step ceiling runs again in both under 10,000,000 units and is compared there. Over the test
suite's string literals (1,379,695 cases at this record), task 2.4b's seeded population (242,609 cases, every construct drawn) and a
population drawn from a seed of its own and weighted to quantified single characters, rows of atoms, literal prefixes,
classes of strings and look-behinds holding them (166,529 cases), no case disagrees; the 51 cases the first ceiling
stopped (26 of `(a+)+b` in both programs, 19 in the general lowering alone, 6 of the weighted population in both) all
agree under the second, and no case reaches a frame or trail ceiling. The counts are pinned. **The plan names the
reference arms' population P4 for this check, and P4 does not exist yet**: task 2.13 builds it, and the check runs over
P4 when it lands, before the candidate reading.

**The public entry (R7, R8; checks A-7 and A-8).** `BroilerRegex.Match(ReadOnlySpan<char> subject, int start, bool
anchored, Span<int> slots, RegexWorkCharge? meter, RegexMatchCeilings ceilings, out RegexLimit limit)` answers
`RegexStatus`, the slots two per group, group 0 first, -1 for a group that did not participate; `SlotCount` says how
many; `RegexWorkCharge` and `RegexMatchCeilings` are public for it (the latter in `Broiler.Regex.Engine`, where it
was). It takes the start as given: under `u` and `v` a start inside a pair splits it, and an engine answering as
RegExpBuiltinExec steps back to the pair before it calls, then reports from the offsets and its own `lastIndex`, which
is what row M4 and decision D41 need of it. `PublicEntryTests` holds A-7 (`/b/` and `/b/y` alike for each value of the
argument) and A-8 (no string in the signature; a call through 1 to 1,000 groups allocating nothing, with no meter and
with one). *Amended at the review of task 2.4c:* A-8 holds up to a bound, which the record of task 2.4a gave as
frames and trail past 65,536 integers allocated for the call alone: a call holding at most 16,384 frames and 32,768
trail cells allocates nothing once the pool is warm, so a pattern of up to 16,384 participating groups, each writing
two cells, does, and one of 16,385 allocates its trail in every call (`PublicEntryTests` pins both sides, in frames and
in cells); the public remarks and the CHANGELOG say so. The readings were loosened during this task to the least of up
to fifty calls, which a call allocating on every other call passed; each reading now holds the collector off
(`GC.TryStartNoGCRegion`), so no collection trims the pool between two calls, runs in a collection of tests that runs
alone, and requires every one of twenty calls to allocate nothing. All of it is provisional, in the CHANGELOG and the remarks: task 2.5 adds the compile meter, task 2.9 fixes
the surface. Check C1 now leaves the meter's runtime-made members out, as convention 1 admits it. *Amended at the
review of task 2.4c:* the exclusion first covered every public delegate the library declares, so a filter that runs
the caller's code, and a member taking it, passed C1; it names `RegexWorkCharge` alone now
(`SharedRules.AdmittedDelegates`), and the witness's second delegate, which it does not admit, is refused: its
declaration, its constructor and `BeginInvoke`, which name `object`, and a member that takes it.

**The string-bearing members (D10, D12; R1, R3; row M4, D23, D41).** `Match(string, int)`, `IsMatch` and `Matches` run
the same program as RegExpBuiltinExec under decision D10's policy for an engine that passes no meter: no meter, no step
ceiling, and the engines' frame and trail ceilings. A ceiling reached is answered as that ceiling, never as no match:
`Match` with a failed `RegexMatch` whose new, provisional `Limit` names it; `Matches` with that failed match as its last
element; `IsMatch`, whose `bool` has no room for a third answer, with an `InvalidOperationException` naming it,
documented. *Amended at the review of task 2.4c:* `Matches` threw too, and an exception for a ceiling, which decision
D9 makes an expected failure, breaches convention 3; no rule finds a base class library exception, so the starting
violation list could not show it. `Matches` now answers it as data; `IsMatch`'s exception is the one breach left,
tracked in `conventions.md` ("Where Broiler.Regex stands") and below for task 2.9. From a start inside a pair the
matcher runs from the pair; a non-empty match found there reports the caller's start as its index and its substring
from there (row M4, D23); an empty one, D41's implementation-defined case, is reported at the pair's start, where it
was found, Broiler.VM's realm's answer (Broiler.JS answers `lastIndex`). Defect D1's skips are gone:
`SurrogateStartTests` pins every row on the public members, and `NestingMaximumTests` runs the shapes the old matcher
did not prove at the maximum, each matching on a 1 MiB thread. **Task 2.4a's trail bound is listed, not lifted**: a run
writes no cell, so `a{1048576}` matches, but a loop over a body of more than one character keeps the bound, so
`(?:ab){1048576}` and `(a)*` over 500,000 characters answer the trail ceiling, and `(?:){2147483647}` on the empty
subject too, where the specification answers an empty match (CHANGELOG, Observable changes; `CeilingTests`).

**The deletion (D20).** `Matching/` (the closure compiler, `Matcher`, `MatchState`, the `RepeatMatcher`, `Budget`,
`StepLimit`, the per-start `Budget.Reset` and the greedy fast path's early exit), `RegexOverflowException` and the
internal route to the old backend are deleted; D20's other items for this library (`CaseFolding` and its data, the
generator, the loose lookup, the static cache) went at tasks 2.2 and 2.3. `RegexMatch` and `RegexGroup` stay (R8,
D12), moved to the `Broiler.Regex` namespace. The breaks, each suppressed and listed in
[`suppressions.md`](suppressions.md) with its R item: `RegexOverflowException` removed (CP0001; R1, R4), the two types
moved (CP0001) and `Match(string, int)` and `Matches(string)`, which answer them (CP0002). Check A-1's metadata half,
`MeterMetadataTests`, finds no type named `Budget`, `StepLimit` or `RegexOverflowException` and no method, field or
property named `Budget`, `StepLimit` or `Reset` in the built assembly, and refuses each planted in a copy. The tests
collapse into the one backend: `TestRegex`'s switch and `NewBackendSuiteTests` go, the old-versus-new differential is
retired (its last run: no disagreement unexplained, 207 suite and 57 generated cases of D2's shape, 8 of M7's, 26
stopped), its populations kept in `TestPopulations`, and D1, D2 and M7 stay pinned to the specification's answers on
the new backend. The starting violation list loses the three rows this deletion closes, SL3's reflection in
`Matching/Matcher.cs:336`, C3's `RegexOverflowException` and C5's `TryEnsureSufficientExecutionStack` in
`Matching/Matcher.cs:555`; the C1 rows of `RegexMatch.Groups`, `RegexMatch.NamedGroups` and `BroilerRegex.Matches`
stay for task 2.9, re-worded for the namespace.

**What the later tasks still owe.**
- **Task 2.5 (R2).** The compile meter through the parser and the set algebra, and `Compile`'s signature replaced to
  take it; the corpus's V rows. The parse of a class of strings is most of a compilation's allocation now (above).
- **Task 2.6 (R6).** Duplicate named groups: the parser still refuses them; a back-reference already reads a run of the
  program's list of referenced groups, one long, which the shared name lengthens; `RegexMatch.NamedGroups` and
  `GroupNames` must answer the participating group; the corpus's O07 and O08.
- **Task 2.8.** The validate entry and decision D9's syntax status (a code, a message identifier and a position);
  `Compile` and the constructors still throw `RegexSyntaxException`.
- **Task 2.9.** The surface of `0.2.0-preview.1`: the provisional members of this record (the offset entry, `SlotCount`,
  `RegexWorkCharge`, `RegexMatchCeilings` and its namespace, `RegexMatch.Limit`), `IsMatch`'s exception at a ceiling,
  which departs from convention 3 (an owner's admission of it for a member D12 keeps, recorded in JSD-0081 or the
  conventions, or a status-returning form with the `bool` member deprecated; the note in `conventions.md` goes when it
  is decided, and task 2.9's empty violation list does not close it alone), the remaining starting violations (the C1 rows, SL5's `NoWarn` rows
  among them), the version and `eng/preview-version-history.json`.
- **Task 2.13.** Population P4, over which the fast-path differential runs before the candidate reading.

*Where the plan, JSD-0081 and this repository differ, read at this record.* Plan row 2.4c reads the differential as
"against 2.4b's slow path on the reference-arm population P4"; P4 does not exist, and the differential ran over the
three populations above. D8 lists "prefilter skips" as a unit; the units are Broiler.VM's (a skip one plus the class's
weight, the admitted position's test paid once, the end free), with the class's weight this library's, the floor of the
base-2 logarithm of its ranges (task 2.4a's record), not Broiler.VM's per-table weight. The order is not: Broiler.VM
tests a position and charges its skip after, and its attempt makes the admitted position's test again and pays for it
then; this library charges each test before it makes it, and the attempt takes the admitted position's test from the
scan, as D8's "prepaid ... before the work" reads, the totals the same. D10's policy gives the string-bearing
members no meter and no step ceiling, so a catastrophic pattern there is bounded by the caller's patience alone, where
the old budget answered no match; the offset entry is the bounded form. *Amended at the review of task 2.4c:* this
record first said no engine calls those members. Broiler.JS calls them today (`JSRegExp.Broiler.cs`,
`broiler.Match(input, start)`, pinning `0.1.0-preview.4`) and catches `RegexOverflowException` to fall back to .NET's
engine; no engine calls them at `0.2` before its cut-over, as Broiler.JS moves its pin only at SJL-11.5 (decision
D12), and D10 already sets Broiler.JS's no-meter policy. At that pin move its fallback no longer compiles and has
nothing to catch, as the exception is gone and a ceiling is answered as `RegexMatch.Limit`: SJL-11.5 removes it. D12 keeps `RegexMatch`, and plan
row 2.9 names it kept; it is kept, in another namespace, which is a break package validation needs suppressed. Plan
section 4.3's A-1 names "the per-start reset" as a metadata fact; it is read as no member named `Reset`, the reset
having been `Budget.Reset`, and an iterator's compiler-made `IEnumerator.Reset` is not it.

*Dated record, 2026-10-27 (task 2.5 of SJL-11.2; R2; decisions D8, D9, D10 and D12; check A-2 of bundle
`sjl-11-001`).*

**The meter's shape on compile (D9, D10).** Both of the shapes the plan offered, each where it fits. `RegexLimit`
gains the compile dimension decision D9 lists, `RegexLimit.Compile` (5), and the caller's budget is a parameter of
`BroilerRegex.Compile(string, RegexFlags, IUnicodeTables, int nestingLimit, ulong? compileBudget, RegexWorkCharge?
meter, out BroilerRegex?, out RegexLimit)`, beside the nesting limit, not a member of `RegexMatchCeilings`: that type is
a match's, its frame and trail ceilings mean nothing to a compilation, and a compile budget there would make every
match caller state one; Broiler.Unicode's operations take their limits and their meter as parameters in the same
order. The budget and the meter are independent: the budget bounds this compilation's units and is answered as a
status before the work that would pass it; the meter is the caller's running count, charged in whole blocks of 512
units before the work (`CompileCharge.Block`, the block of task 2.4a's machine), and may throw, its exception reaching
the caller unchanged with no state of the library changed (`CompileCharge` is made for one compilation and kept by
nothing it answers; a compiled program keeps the provider's case folding alone). A budget of zero is a broken
precondition; null is none. The constructors take neither, decision D10's policy for an engine that passes no meter.
`Compile`'s signature of task 2.3 is replaced, not overloaded; both are provisional and no published version carries
either.

**The units, place by place (D8, check A-2).** Each place whose work the pattern or the provider's data decides
charges it before doing it, named by the internal `CompileSite`:

| Place | Unit | Where |
|---|---|---|
| `Pattern` | one per code unit of the pattern, before the pre-scan; the parse's and the program compiler's walks cost a constant per code unit or node and ride on it | `RegexParser.ParsePattern` |
| `Property` | one per range of a property's set, before it is copied; for a property of strings one per range and per sequence before any is read, and each sequence's code units before its string is built | `RegexUnicode.ToCodePointSet`, `AddStringProperty` |
| `Normalize` | one per range gathered, and ⌊log₂ n⌋ more per range when they arrive out of order | `CodePointSet.FromRanges`, `Builder.ToSet` |
| `Union`, `Intersection` | one per range of each operand | `CodePointSet`'s internal overloads |
| `Difference` | one per range of each operand, and one | the same |
| `Complement` | one per range, and one | the same |
| `CaseFold` | one per range of a set folded, one per code point mapped one at a time, and a string's code units | `RegexUnicode.SimpleCaseFold`, `RegexParser.Fold` |
| `Strings` | a string's code units, at least one, each time a class's strings add it or look it up, and when the class takes it | `ClassStrings.Add`, `Contains`; `RegexParser.ToCharSet` |
| `StringOrder` | the class's code units 1 + ⌊log₂ n⌋ times, for its order longest first | `RegexParser.ToCharSet` |
| `Universe` | `Cased`'s ranges copied and its code points scanned for `AllCharacters` under `vi`; `\s`'s Space_Separator ranges and three; the 63 code points of `\w` whose orbits the folded `\w` reads | `RegexUnicode` |
| `Program` | one per range a class instruction copies; a class of strings' code units and one to read and hash them, again for each earlier table of its key compared, and its code units and 1 + ⌊log₂ n⌋ per string for a new table; with the fast paths off, each string's code units for its lowering | `ProgramCompiler` |

A whole compilation, measured through the probe's `--engine-row` with no budget: `/abc/` 3 units, `/\p{L}/u` 2,057,
`/\p{L}/vi` 26,134, `/[a-z]/vi` 40, `/\w+\b/iu` 941, `/[^a]/vi` 7,478, `/\p{RGI_Emoji}/v` 337,986 (of which the
order of its 2,760 strings, a sort's bound, 213,072) and a hundred `\p{RGI_Emoji}` in a row 30,519,819, the cost task
2.4c's record left this task to charge. Ordering, the sort's 1 + ⌊log₂ n⌋, is charged as a bound, not as the
comparisons the sort makes.

**MaybeSimpleCaseFolding in proportion to ranges.** The parser mapped every code point of a `v`-mode operand under
`i` one at a time, 145,672 calls for `\p{L}`, which a charge per code point would have made 145,672 units.
`RegexUnicode.SimpleCaseFold` maps a set of at most 4,096 code points that way and splits a larger one by `Cased`:
every code point simple case folding moves is `Cased` (`ProviderDataTests` checks it over the whole code space;
`Changes_When_Casefolded` cannot serve, as three code points that fold are not in it), so the part outside `Cased` is
kept as it is and the part inside, at most `Cased`'s 4,632 code points, is mapped. `\p{L}`'s fold is 5,153 units:
its 684 ranges and the 4,469 code points it shares with `Cased`. `CompileChargingTests` holds the result equal to the
fold of every code point for the general categories, `Cased`, `Any`, `ASCII`, `Assigned`, `Alphabetic`,
`Changes_When_Casefolded`, ten cased and uncased scripts and the complement of each, and 200 sets drawn with a fixed
seed either side of the limit. The sets `\s`, the folded `\w`, `Cased` and `AllCharacters` under `vi` are built the
first time a compilation reads them, where `RegexUnicode`'s constructor built the first two for every compilation.
Under `i` without `v` no class is folded at compile time (task 2.4b folds at match time, charged per match); `i`
reaches compilation only through the folded `\w` under `iu`, a `Universe` charge.

**The union structure (plan row 2.5).** The strings of a `v`-mode class operand were a `List<string>` searched by
`List.Contains` before each addition, and intersections and differences filtered one list by `Contains` on the other:
a union of n strings made O(n²) comparisons and a meet of a and b strings O(a·b), fifty `\p{RGI_Emoji}` in one class
about 187 million. `Ast/ClassStrings.cs` replaces it: an insertion-ordered set, a list for the order the strings were
met in and a `HashSet<string>` compared by code unit for membership, so adding or finding a string is one hash of its
code units, O(its length) expected, and a union, an intersection, a difference or a `\q{…}` of n alternatives is
O(the code units of its operands), which is what it charges. The hash set's order is never read: the class still sorts
its strings longest first, by code unit, so no match changes. The program compiler found an earlier occurrence's table
by comparing the strings with every table built before; it now finds candidates by a key of direction, folding, count
and a hash of the strings in order, and compares string by string only those. A non-`v` class's property escapes are
kept whole in `CodePointSet.Builder` and joined two by two, a level at a time, each range read at most
⌈log₂ (k + 1)⌉ times for k sets. `CompileChargingTests` bounds the touches: a union of 4,000 strings touched
24,018,000 units of strings before the replacement and touches at most eight times their code units now; two hundred
classes of 500 strings cost the program compiler 31,468,185 units before the hash and at most 4,819,200 now. Those
bounds read the account, which `ClassStrings` charges once per addition or lookup whatever the lookup costs, so the
review of this task could put the list search back with the charges unchanged and pass every test. Since the review
the work is read too: a `ClassStrings` takes the comparer that alone decides its membership, the parser's ordinal one
or a test's, and with a comparer that counts its calls a union, an intersection, a difference and a disjunction of n
strings make at most 8n comparisons, where the list search makes about n², some 250,000 at n = 500
(`A_union_an_intersection_and_a_difference_of_n_strings_make_comparisons_in_proportion_to_n`); a comparer that
equates strings of different case holds the membership to the comparer, so a search by any other means is refused as
well (`A_class_s_strings_are_members_by_the_comparer_they_are_made_with_and_by_no_other`). *Amended at the integration
of tasks 2.5 and 2.7:* the bound on the program compiler's tables read the account too, so the verifier of this task
could make each occurrence compare its strings with every earlier table, its charges unchanged, and pass every test,
`Each_occurrence_of_a_class_of_strings_finds_its_table_by_a_hash_of_its_strings` among them. The lookup is now read on
the work as well: the program compiler finds an earlier table through a comparer of strings, for the hash and for the
comparison alike, ordinal but for a test's, and that comparer alone decides which table an occurrence shares (one that
equates strings of different case gives `[\q{ab|cd}]` and `[\q{AB|CD}]` one table). With a comparer that counts its
calls, two hundred classes of the same 500 strings and one string of each class's own, which the class's order puts
last, make 100,200 calls, one hash per string, where at most four times that are admitted; the verifier's mutant makes
10,070,100 and fails `An_occurrence_of_a_class_of_strings_compares_its_strings_with_the_tables_of_its_hash_alone`, the
one test of the suite it fails.

**Class V (check A-2's second half).** The corpus's five V rows answer their declared `limit:compile` under the JIT
(`AdversarialCorpusTests`, each in a child process through the probe's `--engine-row`) and in the probe's report
(E43 to E47), and no longer wait for task 2.5:

| Row | Pattern | Flags | Budget | Answer | Units spent | Whole compilation (E48 to E52) |
|---|---|---|---|---|---|---|
| V01 | `[` + `\p{L}` × 50 + `]` | `v` | 300 | `limit:compile` | 252 | 136,368 |
| V02 | the same | `iv` | 300 | `limit:compile` | 252 | 1,350,949 |
| V03 | the same | `iu` | 300 | `limit:compile` | 252 | 136,368 |
| V04 | `[` + `\p{RGI_Emoji}` × 50 + `]` | `v` | 1,000 | `limit:compile` | 999 | 3,099,677 |
| V05 | the same | `iv` | 1,000 | `limit:compile` | 999 | 4,942,927 |

Each budget exceeds the pattern's length (252 and 652 code units), and each whole compilation is charged more than a
hundred times its budget, which `AdversarialCorpusTests` checks; the probe also compiles the five patterns with no
budget, as the constructors do. Before this task's fourth commit the probe answered the five rows `unsupported`.

**The per-unit test and its control.** `CompileChargingTests.Each_place_charges_at_least_one_unit_per_range_and_per_string_its_expansion_touches`
reads, through an observer the library never sets, what each place charged over a declared set of 35 expansions,
the `v`-mode and case-insensitive ones among them, each row's declared units computed from the provider's data or
written out in its comment: at least one unit per range and per string, and where a place's unit says more, that unit. The blocks are
read at every charge (`The_meter_holds_whole_blocks_covering_every_unit_charged_and_never_a_block_more`): before the
k-th unit at least k units have been charged, never a block more. That test reads the account, not the work; the order
of a charge and the work it pays for is read on the work, where a test can see it:
`Each_range_copied_from_the_provider_is_charged_before_it_is_read` watches the caller's tables and holds every read of
a table of ranges, two for each range a copy reads, to a unit charged at `Property` or `Universe` before it, over
eight patterns of property escapes, properties of strings, `\s` and `AllCharacters` under `vi`. The control removed
each of the library's 25 charge statements in turn, its expression still evaluated and its charge not made, rebuilt,
and ran the compile-charging tests: every removal fails at least one row (24 of them a row of the per-unit test, the
general lowering's charge the test of the fast paths off). Its first run found eleven statements no row needed,
because another charge at the same place paid the floor; the rows were tightened to the places' own units before the
fifth commit. The review of this task found two regressions every test passed: the quadratic list search back in
`ClassStrings` with its charges unchanged, and `ToCodePointSet`'s charge moved after its copy. Each now fails a test
that reads the work: the list search by the default comparison fails the membership test of the union structure
(above), the same search through the set's own comparer its count of comparisons, and each charge moved after its copy
(`ToCodePointSet`'s, `\s`'s and a property of strings') the reads of the tables; five mutants, the whole suite run for
each, each failing those tests alone. Each behaviour change
was watched failing first: the first commit's tests against a charge that charges nothing (24 of 35), the set
algebra's rows against the first commit (23 rows and the fold test), the bounds of the union structure against the
second, the V rows against the third.

**Names (decision D5).** The program compiler's account was first `_charge`, the name of the field of Broiler.VM's
runner that holds its meter; it is `_account`, as is the parser's, and `EngineNamingTests` lists `charge`, watched
failing on the first name. `ClassStrings` is the parser's, outside the backend, and its members are the base class
library's verbs.

**The surface.** Public additions, each provisional and marked in `PublicAPI.txt` by a `# provisional:` line: the
new `Compile` signature and `RegexLimit.Compile`. `PublicAPI.txt`'s marks also name every member the CHANGELOG calls
provisional since task 2.3 (the offset entry, `SlotCount`, `RegexWorkCharge`, `RegexMatchCeilings` and its members,
`RegexMatch.Limit`, and the members of `RegexStatus` and `RegexLimit` that task 2.4a added); the listing's rewrite
keeps a mark while its member is listed, and rule SL6 refuses one whose member is gone (a witness test of each). No
break of `0.1.0-preview.4`'s surface: `Compile` and `RegexLimit` are not in it, and no suppression is added. The
starting violation list closes no row; its citations moved with the lines of `RegexParser.cs`, `CodePointSet.cs` and
`BroilerRegex.cs`.

**What the later tasks still owe.**
- **Task 2.8.** The validate entry with no meter and no expansion (decision D18): every expansion this record charges
  is one the validate entry must not make, so it resolves property names through the provider's `TryResolve` without
  copying their sets (`RegexUnicode.ToCodePointSet`), folds nothing and builds no class; the corpus's N rows and O19
  still wait for it, and the probe answers `unsupported` for the operation.
- **Task 2.9.** `Compile`'s provisional signature (the budget's place and the syntax error still an exception) and
  `RegexLimit.Compile`, and the marks of `PublicAPI.txt`, removed as each member is settled; the public `RegexParser`
  constructor and `Parse` and the public `CodePointSet` operations, whose work grows with the pattern or the sets and
  which take no meter (convention 4), as the constructors take none.
- **Task 2.14.** The `--adversarial` mode builds on `EngineRun`, whose policy now reads the compile budget and whose
  one meter is charged by the compilation and then the match. Part C names no control for class V: its two controls
  are a scratch build that answers `LimitExceeded` naming steps as no match, which every K, A, L, W and G row must
  report, and one without the nesting check, which every N row past the limit must. E48 to E52 record each V
  pattern's whole compilation with no budget, the evidence of the rows' margin, not a control.
- **SJL-11.4.** Broiler.VM's adapter replaces its `source.Length + 32` (row B7) with this meter, through `Compile`.

*Where the plan, JSD-0081 and this repository differ, read at this record.* JSD-0081 and the plan agree on D8 and on
D9's compile dimension. Part C asks a row declared exhausted for a hundred times its budget "by regexpref's count of
the specification's matcher steps"; `regexpref` does not exist yet and counts no compilation, so the corpus's README
states the V rows' margin in R2's own terms, the ranges and strings touched, and this library's test reads the library's
own charge for the whole compilation, which charges at least those. D10 sets the engines' frame and trail ceilings and
Broiler.JS's absent meter, and names no compile budget for an engine; the constructors take none. The plan's row 2.5
names `Fold` under `vi`, which is where this task folds and charges, so the plan and the code agree; the task's brief
also asked for `Fold` under `i`, and under `i` alone nothing is folded at compile time (above), so there is no such
fold to charge. Plan row 2.8 gives the validate entry "a nullable meter"; JSD-0081's D18 and the plan's own D18 give it
"no expansion, no meter", and JSD-0081 is read: task 2.8's entry takes no meter (above). Broiler.VM's realm charges a
compilation `source.Length + 32`; this library's units are its own, in proportion to the expansion, as D8 reads.

*Dated record, 2026-10-27 (task 2.7 of SJL-11.2; R9, check A-9; decisions D9, D11 and D20, and D5's rule).*

**What it builds.** The namespace `Broiler.Regex.Text` (`Text/`), every member provisional until task 2.9:
`RegexFlagsText.Parse` reads a flags string as a `RegexFlagsStatus` (`Success`, `UnknownFlag`, `DuplicateFlag`,
`UnicodeWithUnicodeSets`) with the position of the code unit refused, RegExpInitialize's checks over the whole string
first and ParsePattern's `u` with `v` after (ECMA-262 §22.2.3.3, §22.2.3.4), and `RegexFlagsText.Format` writes a
flags value in the `flags` getter's order (§22.2.6.4); `new RegexEscaper(IUnicodeTables)` holds the provider's
`EcmaScriptLexical` (D11), and its `EscapeString` is `RegExp.escape`'s text (§22.2.5.1, EncodeForRegExpEscape) and the
static `EscapePattern` the `source` text (EscapeRegExpPattern, §22.2.6.13.1). `RegexFlagsParser.Parse` reads the same
grammar and throws as before, so the starting violation list's SL4 row loses its two places in `RegexFlags.cs`.

**The shapes, and why.**
- **The flags' status is an enum of its own.** D9's syntax status, with a code, a message identifier and a position,
  is the validate entry's (task 2.8); the three refusals of a flags string are its codes here, the member's name its
  message identifier (SJL-D-11: each engine renders its own text), with the position beside it. Task 2.9 may fold
  them into task 2.8's status. The reader decides by the ninth code unit, so it takes no meter (convention 4).
- **The escapes answer `RegexStatus.Success` or `LimitExceeded`** past the caller's maximum length, their only limit,
  as the Unicode owner's case mapping answers `UnicodeStatus`. D9's dimensions are steps, frames, trail, nesting and
  compile, so no `RegexLimit` member names a length; one was not added beside task 2.5's compile member, and task 2.9
  decides whether the surface needs it.
- **The meter.** One unit per code unit read before the measuring pass and one per code unit written before the
  writing pass, paid in whole blocks of 512 on one account, never a block more than the work: `RegexWorkCharge`'s
  documented blocks (D8). A text that needs no change is answered as itself and costs its reading only. Broiler.VM's
  realm charges `RegExp.escape` its input's length and then its output's today (`StringCharge` at
  `JsRealm.RegExp.cs:1462` and `:1491`), and `source` nothing. For a text that changes, the two totals agree up to the
  block. For a text of n code units that needs no change they do not: Broiler.VM charges 2n, and the helper charges
  its reading only, n rounded up to the block. *Amended at the integration of tasks 2.5 and 2.7:* that is about half
  of Broiler.VM's charge for a long text only (10,000 underscores: 20,000 today, 10,240 here); the two agree at 256
  code units, 512 each, and for an unchanged text of 1 to 255 code units the helper charges more than Broiler.VM, a
  whole block of 512 against 2n (100 underscores: 200 today, 512 here), where this record first said about half for
  every such text. Either way it is a fuel difference SJL-11.4 must read at its cut-over under decision D8, where an
  exhaustion verdict it moves is class C2 of Broiler.VM's rule. The meter is nullable for an engine that charges
  nothing. *Amended at the review of task 2.7:* this record first said the totals agree up to the block for every
  text.
- **`EscapePattern` takes no flags**: each rewrite holds in all three modes (its remarks), so one pattern gives one
  text whatever its flags.

**How the engines' members map (D20; read as the specification of what each needs, nothing copied, and
`TextNamingTests` holds the namespace to none of their names).**
- Broiler.VM: `RegExpNormalizeFlags` onto `Parse` and `Format`, every refusal its "Invalid regular expression flags"
  `SyntaxError`; the binder's `CheckRegExp` flag checks onto `Parse`, `UnicodeWithUnicodeSets` its "u and v may not be
  combined" message; `RegExpHasFlag` onto the `RegexFlags` bits; `RegExpEscapeText`, `RegExpEscapeCodePoint` and
  `RegExpEscapeHex` onto `EscapeString` with `StringLengthCeiling` as the maximum (`LimitExceeded` its `RangeError`)
  and `engine.Charge` as the meter; `RegExpSourceText` onto `EscapePattern`.
- Broiler.JS: `ParseFlags` onto `Parse` and the bits, its messages naming the code unit at the position;
  `BuildFlagsString` onto `Format`; `Escape` and `TryAppendEscape`, with the helpers they call, onto `EscapeString`
  with no meter; `EscapePatternSource` and `TryAppendLineTerminatorEscape` onto `EscapePattern`, whose text is
  Broiler.JS's for every pattern.
- What stays the engines': `RegExp.escape`'s TypeError for a non-String, the `flags` getter's property reads and
  `toString` (rows F2, T4), the prototype's `(?:)` receiver rule, every message's text, a literal's flags position in
  its own source, and Broiler.JS's `RegexOptions`, which retire with its translator. One answer moves in Broiler.JS: a
  flags string with `u`, `v` and a later unknown or repeated flag (`uvx`, `uvu`) is refused for the later code unit,
  in the specification's order, where `ParseFlags` names `u` with `v`; both are a `SyntaxError`.

**The evidence.** `FlagsTextTests` (every ordering of every set of the eight flags, every code unit alone, every
string of up to three code units over the flags and twelve strangers, against the specification's steps as an
oracle); `EscapeStringTests` (every code point first and after the first, against §22.2.5.1 with the sets written
out and UCD 17.0.0's Space_Separators held equal to the provider's; U+0085; test262's `built-ins/RegExp/escape`,
313 cases ported as data; correspondence row T5; the round trip under no flag, `u` and `v`; the meter and the limit);
`PatternSourceTests` (every code unit in five shapes, under three modes: a text that is not a literal's body only
from a pattern that does not compile, and a rewritten pattern answering as its text; every string literal of the
test project the same; `staging/sm/RegExp/escape.js`'s 49 cases; rows T1 to T4); `TextNamingTests`. Each test was
watched failing against a stub. The Native AOT probe's population `text-1` answers as the JIT does, byte for byte.
A-4 and A-8 are unchanged and pass.

**What the later tasks owe.** Task 2.8: the syntax status, which `RegexFlagsStatus` may join. Task 2.9: the surface
of these members, a length dimension or not, and `RegexFlagsParser.Parse`'s exception (convention 3). Task 2.12: the
vectors' flag, escape and `source` operations mapped onto these helpers. SJL-11.4 and SJL-11.5: the adapters above.

*Where the plan, JSD-0081 and this repository differ, read at this record.* JSD-0081 classes row T2, a `/` in a
class, implementation-defined; the library keeps it, Broiler.JS's text, because test262's
`staging/sm/RegExp/escape.js`, the file row T3's correction of Broiler.VM names, asks for `[/]` too, so Broiler.VM's
`source` for such a pattern changes at SJL-11.4 with T3 and is listed there. Plan row 2.7 and the brief ask for every
new member provisional "in `PublicAPI.txt`"; that file is SL6's generated listing and holds no marks, so the
CHANGELOG, the remarks and this record say it. The edition is not a difference: `RegExp.escape` is ES2025's, added by
the 16th edition (as the ES2026 text's own introduction says), so the clause numbers the mapping gives it
(§22.2.5.1, §22.2.5.1.1, §22.2.6.13.1) agree with the mapping's ES2025 header. The text read is Broiler.VM's archived
ES2026 (`tc39/ecma262` `0248456c`), whose numbers for those clauses are the same, with test262 at the pin `ccaac100`
as its executable form; the one clause the helpers cite whose number moved, UnicodeEscape (§25.5.2.4 in ES2025,
§25.5.4.4 in ES2026), is cited with both. *Amended at the review of task 2.7:* this record first said `RegExp.escape`
is ES2026's. *Amended at the integration of tasks 2.5 and 2.7:* task 2.5, written beside this one, gave
`PublicAPI.txt` its `# provisional:` marks, which the listing's rewrite keeps; the integration marks the thirteen
lines of `Broiler.Regex.Text` with them, so the file now says what the brief asked.

*Dated record, 2026-10-27 (task 2.6 of SJL-11.2; R6, check A-6 of bundle `sjl-11-001`; decisions D8, D9, D12, D14
and D36).*

**What it builds.** ES2025's duplicate named groups. A group name may be declared by several groups when no two of
them can both participate (ECMA-262 §22.2.1.1, MightBothParticipate §22.2.1.4); `\k<name>` reads every group of the
name and matches the one that participated (§22.2.2.7.2); and a result maps the name to that group (§22.2.7.2,
RegExpBuiltinExec, step 34.e). Requirement R6 is implemented here, so JSD-0081's condition (i), that duplicate named
groups are implemented in Broiler.Regex before Broiler.JS retires its translator, holds on this branch; the package
that carries them is task 2.9's `0.2.0-preview.1`. R6's exit criterion, as decision D14 reads it, is met when task
2.10's host and library jobs run green at a reading's commit, the host job passing all 19 of test262's files of
duplicate named groups (below). *Amended at the review of task 2.6:* this record first said R6 is met on this branch.

**The parser's rule.** The parser keeps a stack of the Disjunctions it is inside, each with the position it began at
and the position its current Alternative began at, and holds the latest group of each name, the names compared as
decoded, after their escapes are read (decision D36's check of task 2.2). A named group whose name an earlier group
declared is admitted exactly when that earlier group's `(` lies in a closed Alternative of an open Disjunction: the
innermost open Disjunction that began at or before it, found by a binary search over the stack, whose depth the nesting
limit bounds. Otherwise the later group is refused, a `RegexSyntaxException` at its `(` with the message of a
repeated name; where an earlier repetition is now admitted, that is a later group than `0.1.0-preview.4` refused
(below). Reading the latest earlier group alone is enough: if every earlier pair of the name passed and the new group
cannot participate with the latest, it cannot with any earlier one, which the remarks of `MightBothParticipate` prove
from the nesting of Disjunctions. So each named group costs one test, paid for by task 2.5's `Pattern` units.
`(?<a>x)|(?<a>y)`, `((?<a>x)|(?<a>y))*` and `(?<a>x)|(?:y|(?<a>z))` compile; `(?<a>x)(?<a>y)`,
`(?:(?<a>x)|(?<a>y))(?<a>z)` and `(?<a>x|(?<a>y))` are refused at the later group. `DuplicateNamedGroupTests` holds
admitted and refused tables in every mode, each refusal with its position, and a seeded population of 20,000 generated
trees (seed 20261027). For each tree the test reads MightBothParticipate from the tree itself, over every pair of groups
of a name, and compares it with the parser's verdict. Four mutants of the check each fail the tests: the innermost
open Disjunction only, the outermost only, always and never fail 8, 38, 42 and 17 of their 98 cases. The new tests were
watched failing against the parser of the task's first commit, which refused every repeated name: 39 of 40.

**`\k` (check A-6; D8).** A named back-reference compiles to one `Backreference` instruction over the list of the
name's groups, ascending, which task 2.4a's machine already read. The machine compares the first group in that list
whose capture is defined, and at most one is defined: groups in different Alternatives, and a quantifier's body clears
its captures at each iteration. This holds forward, right to left in a look-behind, under `i` by Canonicalize, and by
code point under `u` and `v`. A reference to a name none of whose groups participated matches the empty string. The
instruction costs what a back-reference always cost, one unit and its capture's length (D8). Reading the list is not
charged, as `ClearCaptures` of any width costs one unit; the list is fixed when the pattern compiles, at most its
capture count. A name shared by 1,000 alternatives is read by its reference (`Every_group_of_a_name_in_one_thousand_alternatives_is_read_by_its_reference`).
Check A-8 holds: a match through a name of 1, 10 and 1,000 groups, and the reading of its participating group,
allocate nothing per call.

**The results' shape (D12, R8).** The library's own type, not a collection of the base class library (convention 1):
- **`RegexGroupNames`**, which `BroilerRegex.GroupNames` and `RegexParser.GroupNames` answer, immutable and shared by a
  compiled pattern's matches. `Count` and `NameAt(i)` give the names in the order of their first groups, which is the
  order of the `groups` object's properties. `GroupsNamed(name)` gives a name's groups, ascending, as a `ReadOnlySpan<int>`,
  empty for an undeclared name. `NameOfGroup(k)` gives a group's name or null. `ParticipatingGroup(name, slots)` gives
  the group of the name whose start and end the offset entry wrote into the caller's slots, or 0. It allocates nothing,
  and a span shorter than the pattern's slots is a broken precondition.
- **`RegexGroup.Number`**, so the string-bearing `RegexMatch.NamedGroups[name]` (decision D12 keeps it, R8 allows it) is
  the participating group, whose number says which. When none of a name's groups participated, the entry is the first
  of them, unmatched. Each name is listed once, in the order of its first group.

Every new member is provisional, marked in `PublicAPI.txt`. The two breaks are CP0002 for `BroilerRegex.get_GroupNames`
and `RegexParser.get_GroupNames`, each a dictionary of each name to one group until now. Both are suppressed in
`CompatibilitySuppressions.xml` and listed in `docs/suppressions.md`. No shim keeps the dictionary, which would answer a
shared name wrongly. The two C1 rows of the starting violation list for `GroupNames` close with it.
`EngineNamingTests` gains `names`, the field of Broiler.VM's parser that holds each group's name. Task 2.6's first name
for the list of names took it, and the test was watched failing on it; the field is now `_nameList`, and the test reads
`RegexGroupNames`' members as well as the backend's.

**test262 (decision D14's reading of R6).** `Test262DuplicateNamedGroupsTests` ports as data, at the pin `ccaac100`,
every file whose front matter lists `regexp-duplicate-named-groups`: 19 files, each assertion transcribed to the
library's API where the library alone answers it. **Fifteen are judged here:** the two syntax files (the Annex B one
through `compile`, whose own steps are the host's), `named-groups/duplicate-names-{exec, group-property-enumeration-order,
match-indices, match, matchall, search, test}.js`, `prototype/exec/duplicate-named-{groups, indices-groups}-properties.js`,
`String/prototype/match/duplicate-named-{groups, indices-groups}-properties.js`, and staging's
`duplicate-named-groups.js` and `duplicate-named-groups-search.js`. **Four wait for task 2.10's host job**, because their
assertions read a host's algorithm over the matches, GetSubstitution or split's splitter: `duplicate-names-replace.js`,
`duplicate-names-replaceall.js`, `duplicate-names-split.js` and staging's `duplicate-named-groups-replace.js`. The test
checks each match those four read. The test also covers the syntax tests of `language/literals/regexp/named-groups/`
(the 56 files' literals) and `built-ins/RegExp/named-groups/`' two `property-names-invalid` files. Six of its nine tests
failed against the task's first commit.

**O07 and O08.** The adversarial corpus's two rows that waited for this task answer their declared statuses in a child
process (`AdversarialCorpusTests`) and in the Native AOT probe (E53: `match 0,1 - 0,1 units 7`; E54:
`match 0,2 - 0,1 units 9`). E55 to E57 add a reference under `i`, one read right to left and a name 1,000
alternatives share. The smoke population gains shared names, two refusals and a rendering of a name's groups and the
one that participated. The probe's report under Native AOT is the JIT's, byte for byte.

**What the later tasks still owe.**
- **Task 2.8.** The validate entry reads the same parse: the duplicate check needs no expansion and no meter beyond the
  pattern's, so it moves with the parser. The refusal of a repeated name becomes D9's syntax status, with a code, a
  message identifier and the later group's position; until then it is the `RegexSyntaxException` it was.
- **Task 2.9.** The provisional surface of this task: `RegexGroupNames` and its five members, `RegexGroup.Number`, and
  the two reshaped `GroupNames`. It also owes `RegexMatch.NamedGroups` and `Groups`, whose C1 rows stay, and the
  unmatched entry of a name no group of which participated: the first group, or none.
- **Task 2.10.** The host job judges all 19 files, the plan's check of row 2.6, the four above whole. The library
  job runs the literal syntax tests under the JIT and through the probe, where this task's test runs under the JIT
  alone.
- **SJL-11.4 and SJL-11.5.** Broiler.VM's admission commit (D15), and its `NameOf`, `NumberOf` and groups object onto
  `NameOfGroup` and `ParticipatingGroup`. Broiler.JS's `BuildCaptureMapFromBroiler` reads the old dictionary and moves at
  SJL-11.5 (`docs/suppressions.md`).

*Where the plan, JSD-0081 and this repository differ, read at this record.*
- **Plan row 2.6's check** is that "test262's files tagged `regexp-duplicate-named-groups` pass in 2.10's host job".
  That job does not exist yet. Until it does, this task's host-free test judges 15 of the 19 and names the four that
  wait. D14's library job names only the literal syntax tests, `property-escapes/generated` and
  `unicodeSets/generated`, so this test reads more than that job will: the matching assertions, transcribed.
- **The brief and plan row 2.6 refuse "with the syntax status (D9)".** That status is task 2.8's, and the parser still
  throws, so the refusal keeps its exception and its message, at the `(` of the first group that might both participate
  with an earlier group of its name. Where an earlier repetition of the name is now admitted, that group comes later
  than the one `0.1.0-preview.4` refused, so the position moves, and the message can name another name:
  `(?:(?<a>x)|(?<a>y))(?<a>z)` is refused at 19, where it was refused at 11, and `(?<a>x)|(?<b>y)(?<a>z)(?<b>w)` at 22
  for `b`, where it was refused at 15 for `a`. *Amended at the review of task 2.6:* this record first said the refusal
  keeps its position.
- **D8's units for a back-reference are unchanged,** as the plan asks. The instruction's work also scans the name's
  groups, at most the pattern's capture count, uncharged as `ClearCaptures`' width is.
- **Check A-6's "a result maps the name to the participating index"** holds where a group participated. Where none
  did, ECMA-262 gives `groups` undefined for the name; `NamedGroups` gives the first group of the name, unmatched,
  whose `Success` is false. Its `Number` is that group's, not a participating one.
- **JSD-0081's row R6** describes `0.1.0-preview.4`: every duplicate refused (S1), and an escaped duplicate in one
  alternative accepted (S2a). Task 2.2 corrected S2a and this task S1, so this repository and JSD-0081 agree. The 19
  files that D15 admits at Broiler.VM's admission commit are the 19 this test reads at the same pin.

*Dated record, 2026-10-27 (task 2.11 of SJL-11.2; R10, check E-5; decision D32; the programme's risks S1 and S2).*

**What it builds.** `tests/Regex.Fuzz` ([its README](../tests/Regex.Fuzz/README.md)), a test project and an executable.
Each iteration of a session draws its case from a generator seeded by the session's seed and the iteration's index
(SplitMix64), so an iteration is the same case however a session's range is split: a pattern from ECMA-262's Pattern
grammar with Annex B's additions (every construct the parser accepts: characters astral, lone and case-folding, every
escape, property escapes and properties of strings, classes, `v`-mode class sets with nesting, `&&`, `--`, ranges and
`\q{…}`, every kind of group, named groups spelled raw, escaped and as surrogate pairs, look-arounds, modifiers,
back-references, every quantifier with bounds past `int.MaxValue`; every flag combination, `u` with `v` among them), or
one of the adversarial corpus's classes K, A, L, G, W, N, F, M, V and O with random sizes, or a grammar pattern with one
near-miss edit that reaches a refusal; and a subject built around the pattern's sample, with lone surrogates, pairs and
long runs. Every case is compiled by `Compile` under a full and a tight policy, each with a nesting limit, a compile
budget and a meter, and by the constructor beside them; matched through the offset entry from every start (a start
inside a surrogate pair among them), anchored and not, under wide ceilings (a step ceiling and the engines' frames and
trail) and once under narrow ones; matched through the string-bearing `Match(string, int)`, `IsMatch` and `Matches`;
and passed to `RegexEscaper.EscapePattern` and `EscapeString` with a maximum length and a meter.

**What counts as a finding.** An exception no entry documents (the answers are the statuses, `Compile`'s
`RegexSyntaxException`, the session's own meter exception, the argument exceptions for arguments broken on purpose and
`IsMatch`'s exception at a ceiling); a disagreement between two answers to one question (`Compile`'s tight and full
policies, the constructor and `Compile`, narrow and wide ceilings, the meter's exception and a fresh call on the same
instance, the string-bearing members and the offset entry through the mapping their documentation states, the text
helpers and the matcher); an answer the documentation rules out (slots, limits, `SlotCount`); and a child process that
ends, by a stack overflow or the runtime's fail-fast, or writes nothing for 120 seconds, which the supervisor reports
at the iteration it was in, with its case. Each child runs on a 1 MiB thread, the corpus's smallest stack. The target
does not judge that a match is the specification's: the vectors and the reference arms do (tasks 2.12 and 2.13).

**The declared session.** Seed `20261027`, iterations 0 to 23,999. CI's `fuzz` job runs it on ubuntu-latest and
windows-latest, bounded by the job's 20 minutes, the step's 10 and the watchdog; locally, on four cores (linux-x64),
it took 26 to 28 seconds on a quiet machine and 37 to 40 seconds beside other builds. It reached every one of the 122
construct classes the generator writes and every outcome the session requires (each status of each entry, each limit,
the meter's exception at compile and at match, a string-bearing member at the engines' ceiling, `IsMatch`'s exception),
and no finding but the one below. Its transcript digest, `56985dcac551a7abe7ecb627c415ec9e68e141b9cae2fb565a93bbe0139fe85e`
at this record's commit, was the same over five runs, over three and four worker processes and under `tr-TR`; it is
recorded, not pinned, because the records hold the library's answers and units and move with them.

**The control (D32).** `dotnet build tests/Regex.Fuzz -c Release -p:RegexFuzzPlant=true` compiles the library's
sources into the target, with the declared line of `RegexParser.ParseClassSubtraction`, the `v`-mode class-set
subtraction (`Set = accumulated.Set.Subtract(operand.Set, _account),`), replaced in a copy under `obj/` by a throw of an
`InvalidOperationException` no entry documents; the library project is not built in that mode, and the build fails
unless the anchor occurs exactly once. The declared seed reports the plant at iteration 52 with one worker process
(with four, the worker that reaches a plant first reports it: iteration 6,008 in one run and 12,010 in another, each
within a second), and CI's `fuzz-control` job
requires it reported; without the plant the declared session reports nothing. The library is unchanged by this task,
and the plant never reaches `Broiler.Regex.dll`, which three readings taken in one session, in one directory, show:
the DLL the library project builds alone and the one the whole solution builds, the fuzz target included, are the same
file; the control's build (`-p:RegexFuzzPlant=true`) builds no library project and leaves that file as it was; and
neither it nor the DLL the package carries holds the plant's text (`planted defect`) or the switch's name
(`RegexFuzzPlant`), in UTF-16 or UTF-8, where the control's `Regex.Fuzz.dll` holds the text, so the search finds it
where it is. *Amended at the integration of tasks 2.6 and 2.11, 2026-10-27:* this record first said "`Broiler.Regex.dll`
built at `de0e7cc` and at this record's commit without the commit stamped
(`-p:ContinuousIntegrationBuild=true -p:EnableSourceLink=false -p:IncludeSourceRevisionInInformationalVersion=false`)
is the same file, SHA-256 `13405060980e2b76fc6bbe548b4f231c21bdb216fc7643fdb17868b4d2cc240b`". No other build
reproduces that hash: the DLL holds the full path of its PDB, so its bytes depend on the directory it is built in. The
task's verifier built both commits in one directory with the same flags and got
`1cc16b88bc21623122b18d0c257bae20cf5de19b9a6ed4cb9e68d0631db5196e` for both, the same file at both commits but not
that one, and the integration built `de0e7cc` in two directories and got two other hashes. A hash is compared within
one session, never recorded as the check; the integration's note below gives the readings at its head.

**The finding.** A lone surrogate written alone as one alternative of a `v`-mode `\q{…}`, raw or escaped, as in
`/[\q{\uD800}]/v` or `/[\q{a|\uDE00}]/v`, escapes `Compile` and the constructor as an `ArgumentException` from
`char.ConvertToUtf32`, which `ParseClassStringDisjunction`'s `Flush` calls for a one-unit alternative
(`Parsing/RegexParser.cs`, line 1197 at `de0e7cc`). ECMA-262 compiles a ClassString of one ClassSetCharacter to that
one character of the class (CompileToCharSet, §22.2.2.9), as this parser compiles `/[\uD800]/v` and
`/[\q{\uD800\uDC00}]/v`, so the pattern is valid and the exception is a defect of the parser's. The declared seed first
reaches it at iteration 280, and 83 times in the declared session. It is library code, which this task does not change:
[`tests/Regex.Fuzz/known-findings.tsv`](../tests/Regex.Fuzz/known-findings.tsv) lists it, so the session prints it as
known and goes on judging everything else, and its row leaves the file with the fix
(`FuzzTargetTests.Each_known_finding_still_reproduces` and the declared session's stale check fail until it does).
**Check E-5 does not hold while the list has a row.** *Amended at the integration of tasks 2.6 and 2.11, 2026-10-27:*
the defect is fixed there and its row has left the list, which is empty, so E-5 holds at the declared seed (the
integration's note below).

**The long run.** On 2026-10-27, iterations 24,000 to 1,023,999 of the declared seed, a million iterations past the
declared session's, in four worker processes: 22 minutes on four cores of a machine other builds kept loaded (load
average 25 to 28), transcript digest `ee73e0f1e208102f4050aa84b0633507a6194ab8df543853808983f6c5ae097d`. Every
construct class and outcome was reached; no process ended and none hung; no exception escaped but the known finding
(3,226 times); and two disagreements, at iterations 215,039 and 814,524, were one second defect of the parser's. In a
`v`-mode class whose expression is an intersection or a subtraction, `RejectMixedOperator` lets any character that is
not an operator follow the last operand, and `ParseNestedClass` then consumes that character as the closing `]`
without reading it, so `/[a--bc/v` and `/[a&&ac/v` compile and match `a`, as `/[a--b]/v` and `/[a&&a]/v` would, where
ECMA-262 refuses both: ClassSubtraction and ClassIntersection are followed by `]` (§22.2.1). (`/[a--bc]/v` is refused,
by the stray `]` the swallowed `c` leaves.) The target saw it through `EscapePattern`: the reduced case is the pattern
`[[a]--B` and a raw carriage return under `v`, matched against `a`; its source text writes the carriage return as
`\r`, whose backslash the parser takes for the `]`, so the two patterns match differently. It is not listed in
`known-findings.tsv`, which lists what the declared session reaches, and is reported for the library's owner.

**What the later tasks owe.**
- **The library's owner:** the fixes of both defects above, the first with its row removed from `known-findings.tsv`
  in the same change. *Amended at the integration of tasks 2.6 and 2.11, 2026-10-27:* both are fixed there, the
  first with its row removed in the same commit (the integration's note below).
- **Task 2.8:** the validate entry, which the target then calls beside `Compile` (a pattern the entry accepts compiles,
  under the same limit, or meets a limit; a refusal is the same refusal).
- **Task 2.9:** when `Compile`'s syntax error becomes a status and `IsMatch`'s exception at a ceiling is admitted or
  replaced, the answers the oracle admits narrow with them (`Verdicts.Compile`, `Verdicts.IsMatch`).
- **Task 2.14:** the probe's `--adversarial` mode: the corpus's rows with their declared statuses, each in a child
  process on the 1 MiB stack, under the JIT and through Native AOT, and part C's two controls. The target reaches every
  class of the corpus but judges no declared status, and runs under the JIT only.
- **SJL-11.4:** D32's second half, Broiler.VM's `broiler-js-regexp-fuzz` session re-pointed to the adapter with its
  controls kept (decision D20).

*Where the plan, JSD-0081 and this repository differ, read at this record.* D32 asks for an "in-process" fuzzer: each
case runs in process, in child processes a supervisor reads, because E-5's "no process end" cannot be observed by the
process that ends. E-5 admits "no exception but statuses and the meter's"; at this commit the library also documents
`Compile`'s `RegexSyntaxException` (until tasks 2.8 and 2.9) and `IsMatch`'s exception at a ceiling (the dated note of
2026-10-26 in [`conventions.md`](conventions.md)), which the oracle admits as documented answers. The brief asked for
the plant as "a define or an internal hook compiled only into the fuzz build": convention 17 admits no preprocessor
directive and this task changes no library code, so the plant is a build-time copy of the parser. Plan row 2.11's
"every limit and a meter set" cannot hold for the string-bearing members or the constructor, which take none (decisions
D10 and D12): they are called only where a call with every limit set answered first, so their work is bounded by it.
Neither the plan nor JSD-0081 declares a seed or a count: this record does. The brief named assurance annotations "if
the assurance config covers `tests/`"; `assurance.config.json` lists the library's project alone, so the target carries
none.

*Amended at the review of task 2.11, 2026-10-27.* The review's five findings were the target's and its records'; the
library is unchanged. The string-bearing members' `Value`, the match's, group 0's and every group's, is now read against the
subject at the offsets they report, each `NamedGroups` entry against a participating group of its name, and a failed
match against the shape `RegexMatch.Empty` documents: a review's plant of a wrong substring in `BroilerRegex.Run` had
passed the declared session, which now reports it. `Matches` is now compared at the engines' ceilings: its emulation
always takes the wide calls the case made, so its first element is compared on every subject, and its ceiling element
8 times at the declared seed, which the session now requires (`matches:limit`), where any subject over 4,096 units had
been skipped and only subjects of about 500,000 units reach a ceiling; past its first element `Matches` is compared on
subjects up to 4,096 units within 2^20 units, as before, a cut-short emulation over the elements it has. A row of
`known-findings.tsv` now names a text of the exception's message and matches only with its method the innermost
library frame, so that another exception on the same path is a finding: the review's plant of an `ArgumentException`
for a three-unit `\q{…}` string, absorbed before as the listed row, is reported, and the row still counts 83. The
declared session's digest is now `a380747ca05586a1489ffa6ebe7aabd605ca5b4091345f0df6ef56c4b3447b12`, the same over five
runs, over three and four worker processes and under `tr-TR`; the session took 21 to 33 seconds on four cores beside
other builds, and the control still reports the plant at iteration 52 with one worker process. The comment of
`Directory.Build.props` now names the fuzz target as the test project its `.Tests` rule does not reach, and the
CHANGELOG says that the library, not the package, is unchanged: the packed README names the target.

*Dated note, 2026-10-27 (the integration of tasks 2.6 and 2.11 of SJL-11.2; R5, R6 and R10, check E-5 of bundle
`sjl-11-001`; decision D32).*

**The merge.** Branch `sjl-11-2-integrate-b` starts from the merge of pull request #13 (`15bffa7`, whose tree is
`de0e7cc`'s, the commit both tasks branched from). Task 2.6 (`34c53e4`) merged with no conflict, then task 2.11
(`cab6252`), whose one conflict was this file: both dated records are kept whole, task 2.6's then task 2.11's.
`assurance generate` over the merged tree wrote nothing, as its configuration covers the library's project alone,
which task 2.11 did not change. At the merge 1,879 library tests, 49 architecture tests and the fuzz target's 11 passed,
and the declared session reported only its known finding.

**The fuzz target's two findings, fixed.**
- `/[\q{\uD800}]/v` and `/[\q{a|\uDE00}]/v` threw an `ArgumentException` out of `Compile` and the constructor (the
  declared session, first at iteration 280; the one row of `known-findings.tsv`). ECMA-262 compiles a ClassString to
  the characters its ClassSetCharacters denote (CompileClassSetString, §22.2.2.10), and under `v` a lone surrogate is a
  code point as any other, so the class holds that code point and matches it, as `/[\uD800]/v` does; test262 at
  `ccaac100` has no file with a lone surrogate in a `\q{…}`, and V8 agrees. The cause was
  `ParseClassStringDisjunction`'s reading of an alternative's code points back from its UTF-16 text, where
  `char.ConvertToUtf32` refuses a one-unit lone surrogate. Reading the related paths (a lone surrogate under `vi`,
  beside pairs, in intersections and subtractions, read right to left, complemented) found three more defects of the
  same loop: a lead surrogate written before a trail one, two ClassSetCharacters as in `\q{\uD83D\u{DE00}}`, read as
  the one code point U+1F600, so the class matched U+1F600 and set operations took it for `\q{\u{1F600}}`; and the
  loop read its characters by a grammar of its own, which refused `\b` and read a reserved double punctuator
  (`\q{a&&b}`) as two characters. An alternative now reads its characters through `ReadClassSetCharacter`, the
  class-set operand's own reading, as code points: one character is a member of the code-point set, a lone surrogate
  included, and an alternative that holds a lead followed by a trail is not kept, as a subject read by code point
  (§22.2.7.2) never holds that pair of code points in a row. `ClassStringCodePointTests`' six tests failed at the merge
  and pass (`28835d1`).
- `/[a--bc/v` and `/[a&&ac/v` compiled and matched `a` (task 2.11's long run, iterations 215,039 and 814,524).
  `ParseClassIntersection` and `ParseClassSubtraction` ended with `RejectMixedOperator`, which refuses only an operator,
  and `ParseNestedClass` then took the next character for the `]` without reading it. They now end with
  `RejectAfterLastOperand`, which admits only the `]` or the pattern's end, so any operand there is the refusal of a
  mixed class-set expression, at the expression's first operand, and a class the pattern ends inside stays unterminated
  at its `[`, as V8 also answers. `ClassSetExpressionEndTests`' 347 cases (each operator, each kind of trailing operand,
  nesting depths 0 to 3, closed and unclosed, `^`, `v` and `vi`, both entries) had 333 failing before and all pass
  (`0c468d2`).

**`known-findings.tsv` is empty.** Its one row left with the first fix, in the same commit, and no row remains. Check
E-5 holds at the declared seed: the session's only answers are the statuses, the meter's exception and the answers
task 2.11's record lists as documented (`Compile`'s `RegexSyntaxException` until tasks 2.8 and 2.9, `IsMatch`'s
exception at a ceiling, an argument exception for an argument broken on purpose), with no listed finding to absorb a
library exception; a listed finding the session no longer reached would fail it as stale.

**The oracle reads `RegexGroup.Number`** (`6b74e50`), which task 2.6 added and task 2.11's oracle, written beside it,
never read: each group's number is its own index; a named entry's is the participating group's, whose slots in the
offset entry's answer are its offsets, and `RegexGroupNames.ParticipatingGroup` reads the same group from those slots;
where none of the name's groups participated, the entry is unmatched and numbered as the name's first group. Three
mutants of the library's numbers each passed the declared session under the oracle before and fail it now: a
participating group of a shared name numbered as the name's first (first found at iteration 119), a name with no
participating group given its last group instead of its first (iteration 1,211), and the `NamedGroups` entry alone
numbered as the name's first group (iteration 119).

**The declared session at the integration's head.** Seed 20261027, iterations 0 to 23,999: no finding, exit 0, every one
of the 122 construct classes the generator writes and every outcome the session requires reached. Transcript digest
`2eb41e236a4a5f08e976bd1f7b7bc0e11db1af16e98261772905b2f61296c846`, the same over two runs with four worker processes,
over one and three, and under `tr-TR`; the session took 17 to 24 seconds with four worker processes, 19 with three and
45 with one, on four cores beside other builds (load average 10 to 15). The control (`-p:RegexFuzzPlant=true`) still
reports the plant: with one worker process at iteration 52, as at task 2.11's review, and with four at iteration 6,008
or 18,005, whichever worker reaches it first, within a second, the session exiting 1.

**The long run.** On 2026-10-27, at `6b74e50`, iterations 24,000 to 1,023,999 of the declared seed, the range of task
2.11's long run, with the two iterations of its finding, 215,039 and 814,524, among them, in four worker processes:
818.5 seconds of session, 13 minutes 40 seconds of wall time, on four cores beside other builds (load average 6 at the
start and 22 at the end). Every construct class and outcome was reached, no process ended or hung, and there was no
finding; transcript digest `731c698b6d876679190b2afaa6b5e6d9579d953ee176bffc926e4f56f9122d1b`.

**The plant never reaches the shipped DLL** (the method task 2.11's record now states, read at the integration's head):
built from a `git archive` of `6b74e50`, whose code the records' commit does not change, into one scratch directory with
the flags task 2.11's record names, `Broiler.Regex.dll` from the library project alone and from the whole solution, the
fuzz target included, is one file (SHA-256 `978ad39895c56eba809843af6060fa5b0bc7247a84f1c9d9fee550851b88318d`, a value
of that directory alone); the control's build afterwards built no library project and left the file byte for byte; the
package packed from it carries the same file; and neither DLL holds `planted defect`, in UTF-16 or UTF-8, or
`RegexFuzzPlant`, where the control's `Regex.Fuzz.dll` holds the text once.

*Where ECMA-262, V8 and this repository differ, read at this note.* For `/[\q{\uD83D\u{DE00}}]/v` V8 matches U+1F600,
while it keeps the alternative apart from `\q{\u{1F600}}` in an intersection or a subtraction; this library follows
ECMA-262 and matches nothing. test262 has no file on it, and the fuzz target judges agreement between the library's
entries, not the specification, so the vectors (task 2.12) and the reference arms (task 2.13) are where it is read
next. What task 2.11's record says the later tasks owe stands, except the library owner's fixes, which this integration
made.

*Added at the integration's review, 2026-10-27.* **A range before `&&` or `--`, refused** (`379c6b6`). The review
read 1,482,230 generated `v` and `vi` class sets against V8 (node 22.22.0) at `cd8a464`: 206 compiled here and were
refused there, each with a ClassSetRange before `&&` or `--`, as `/[a-z--b]/v`, which compiled as `a-z` minus `b`.
ECMA-262 §22.2.1 begins a ClassIntersection and a ClassSubtraction with a ClassSetOperand; a ClassSetRange belongs to a
ClassUnion alone, and after one neither `--` nor `&&` can be read. `ParseClassSetExpression` passed a range read first
to `ParseClassIntersection` or `ParseClassSubtraction` as their first operand: the beginning of the expressions whose
end the second fix above closed. `0.1.0-preview.4` and the merge compiled the same forms, test262 at `ccaac100` has no
file with one, and the fuzz target's grammar writes ranges only in unions, so the declared session could not reach it.
`ParseClassSetRangeOrOperand` now says whether it read a range, and after one the expression is a union, whose
`RejectMixedOperator` refuses the operator as a mixed class-set expression at the expression's first operand. All 206
forms are now that refusal, and V8 refuses each as an invalid set operation. `ClassSetExpressionEndTests`' 47 new
cases (five range-first expressions at depths 0 to 2, with and without `^`, under `v` and `vi`, the review's forms, and
controls) had 38 failing before and all pass. The declared session's digest stays
`2eb41e236a4a5f08e976bd1f7b7bc0e11db1af16e98261772905b2f61296c846`, no answer of it changing. The review also read the
digest at each step of the integration: the merge alone made it
`835849920282be8d19042f8be6f22b2328c25f6c032b45586933f8d5d341bb6c`, task 2.6's admission of a repeated group name
answering 228 iterations otherwise, each a pattern that repeats a name and no other changing; the first fix made it
`62a3783a517aab85df291cb733c8aacf91e0d5773cfce6e86d60fa4c306f250d`; and the second `2eb41e…`. The fuzz target's README
now names both causes. And two cases of `ClassStringCodePointTests`, commented as one escape of a lead and a trail,
held the raw pair in their patterns, so no case asserted the answer of an escaped pair in a `\q{…}`; they now write the
escape, with its answers in set operations, complemented and under `vi`, and the raw pairs stay as cases of their own
(`0320c78`).

*Dated record, 2026-10-27 (task 2.8 of SJL-11.2; R10's conventions 3 and 4 for the validate entry, check A-5's "refused
by the parser and by the validate entry"; decisions D9 and D18 of JSD-0081, and the programme's SJL-D-11).*

**What it builds.** The validate entry, `BroilerRegex.Validate(string pattern, RegexFlags flags, IUnicodeTables tables,
int nestingLimit, out RegexDiagnostic diagnostic, out RegexLimit limit)`, and its overload over a flags text,
`Validate(string, string? flags, IUnicodeTables, int, out RegexDiagnostic, out RegexLimit)`, which reads the flags
first, as IsValidRegularExpressionLiteral does (§13.2.7.2). The status is `RegexStatus`: `Success`; `SyntaxError` (4,
new) with the first refusal the parser meets, in its reading order; or `LimitExceeded` with `RegexLimit.Nesting` when
the pattern nests past the caller's limit before any refusal. The diagnostic is `RegexDiagnostic`, a struct of five
members: `Code` (a `RegexDiagnosticCode`), `MessageId` (`RegexDiagnostic.MessageIdOf(Code)`), `Argument` (a string, or
null where the code takes none), `Position` (a code-unit offset into the pattern, into the flags text for the flags'
three codes, -1 for no refusal) and `Message` (a default English rendering, which is no contract; an engine renders its
own from the identifier and the argument, SJL-D-11). A refusal is never an exception: the entry catches the parser's
internal ones at its boundary, and only a broken argument throws (a null pattern or tables, a limit outside 1 to 512,
tables of another Unicode version). Every `RegexSyntaxException` the parser and the flags parser throw, so every one
`Compile`, the constructors, `RegexParser.Parse` and `RegexFlagsParser.Parse` throw, now carries the same diagnostic as
its new `Diagnostic` property, built by the same call in the parser (`RegexParser.Refuse`), so a pattern's refusal is
one value whichever entry meets it. One refusal of the library carries none: the public `CharSet.AddRange`'s of a range
written high to low (`CharSet.cs:118`), which no compilation reaches and task 2.9 owns (below). All of it is
provisional, marked in `PublicAPI.txt` (63 marks).

**The code table.** Numbered by the part of the grammar, each number fixed once published and never reused: the flags 1
to 3 (`UnknownFlag`, `DuplicateFlag`, `UnicodeWithUnicodeSets`); groups and quantifiers 10 to 17
(`UnmatchedParenthesis`, `UnterminatedGroup`, `InvalidGroup`, `NothingToRepeat`, `QuantifiedAssertion`,
`QuantifierOutOfOrder`, `BracedQuantifierWithoutAtom`, `LoneSyntaxCharacter`); escapes 20 to 25 (`TrailingBackslash`,
`InvalidIdentityEscape`, `InvalidControlEscape`, `InvalidHexEscape`, `InvalidBracedEscape`, `CodePointOutOfRange`);
references 30 to 33 (`BackreferenceOutOfRange`, `UndefinedGroupName`, `NamedReferenceWithoutName`,
`NamedReferenceInClass`); group names 40 to 45 (`UnterminatedGroupName`, `GroupNameInvalidEscape`,
`GroupNameInvalidStart`, `GroupNameInvalidPart`, `EmptyGroupName`, `DuplicateGroupName`); modifiers 50 to 52
(`ModifierRepeated`, `ModifiersEmpty`, `ModifierAddedAndRemoved`); classes 60 to 63 (`UnterminatedClass`,
`ClassRangeOutOfOrder`, `ClassRangeEndpointIsClass`, `NegatedClassMayContainStrings`); class sets 70 to 76
(`ClassSetMixedOperators`, `ClassSetAmpersandAfterIntersection`, `ClassSetRangeEndpointNotCharacter`,
`ClassSetUnescapedSyntaxCharacter`, `ClassSetReservedDoublePunctuator`, `ClassStringDisjunctionWithoutBrace`,
`UnterminatedClassStringDisjunction`); property escapes 80 to 86 (`PropertyEscapeWithoutBrace`,
`UnterminatedPropertyEscape`, `UnknownPropertyName`, `UnknownPropertyValue`, `UnknownLonePropertyName`,
`PropertyOfStringsOutsideUnicodeSets`, `NegatedPropertyOfStrings`); and the nesting limit, 90 (`NestingLimit`). 49
conditions and `None` (0). Each code has one identifier, dotted and lower case in the form of the shared frontend's
early-error catalogue (Broiler.JS's `Broiler.ECMAScript/docs/frontend/early-errors.tsv` at `238069ab`), whose rows are
early errors (EE), Annex B's (EG) and limits and grammar (EX): where the catalogue has a row for the condition, the
identifier is the catalogue's (EE0184 to EE0213: `regexp.group-name.duplicate`, `regexp.quantifier.out-of-order`,
`regexp.modifiers.repeated-flag` for EE0186, EE0188 and EE0189, and so on; and EX0026, the limit on a pattern's nesting
of groups and classes, `limit.regexp.too-deep`, for `NestingLimit`; a test pinning each row). The library coins an
identifier, in the catalogue's `regexp` namespace and form, only for a condition the catalogue has no row for, the
grammar's own refusals and the flags', which the catalogue folds into EE0019 (`regexp.group.unterminated`,
`regexp.class-set.mixed-operators`, `regexp.flags.duplicate`); and for the conditions of row EG0002, which it splits
(the differences, below). EE0183, 2^32 - 1 or more capturing groups, has no code: no string the platform holds reaches
it. The arguments: the code unit for an unknown flag, a lone syntax character, an identity escape or an unescaped
class-set syntax character; the flag for a repeated flag or modifier; the quantifier's code unit for nothing to repeat;
the digits of an out-of-range back-reference; the decoded name of an undefined or duplicate group name; the range as
written, out of order; the two code units of a reserved double punctuator; the text between a property escape's braces;
and the limit, in decimal, for the nesting limit. `ValidateEntryTests.The_code_table_is_pinned` pins every number and
identifier.

**What each early error needs, with no expansion (D18).** The entry runs the compilation's parser with expansion off:
no property escape's set is copied, nothing is folded or complemented, no class's code points or strings are gathered,
no set algebra is done and no universe is built. The early errors need none of it. A property escape's name and kind,
code points or strings, come from the provider's `TryResolve`, which answers a view of its tables and copies nothing
(`RegexUnicode.ClassifyProperty`); a group name's code points from the provider's identifier classes; MayContainStrings
of a negated `v`-mode class from its syntax (§22.2.1.8), never from the strings its operations would leave; a range's
order from its two characters, and a `v`-mode range endpoint's from its operand's syntax; task 2.6's rule for repeated
names, `\k<name>` and `\n` from the names and the count of groups the pattern declares. A validation therefore charges
its account the pattern's read alone, which `A_validation_expands_nothing` reads over eleven patterns whose compilation
charges expansions, and its work is in proportion to the pattern's length.

**The meter (D18).** None. JSD-0081 files D18 as "no expansion, no meter", the plan's own D18 agrees, and plan row 2.8's
"a nullable meter" is not taken, as task 2.5's record said it would not be. The entry takes no compile budget either.

**`Compile`'s error shape: unchanged, owned by task 2.9.** `Compile`, the constructors, `RegexParser.Parse` and
`RegexFlagsParser.Parse` still throw `RegexSyntaxException` for a syntax error, which now carries the diagnostic, so a
caller can read the code without the validate entry. The status form would replace `Compile`'s provisional signature a
third time, with the corpus runner of the probe, which task 2.14 changes beside this task, the fuzz target's oracle and
both engines' adapters moving with it, before task 2.9 fixes the surface of `0.2.0-preview.1` in one change; and C3's
starting violation, `RegexSyntaxException` itself, stays until then. Task 2.9 owns the decision.

**The validate entry and `Compile`.** Every syntax error `Compile` and the constructors refuse a pattern with, the entry
answers with the same diagnostic, and a pattern it accepts `Compile` compiles, under the same nesting limit. They part
only where `Compile` meets a limit the entry does not have: its compile budget, which can stop a compilation before a
later syntax error or the end of a valid pattern (class V's patterns are admitted by their read and refused by
`Compile` at their budget), and its meter, whose exception does the same. Past the caller's nesting limit both answer
`LimitExceeded` naming `Nesting`, and the constructors, which parse under 512, throw the same `NestingLimit` diagnostic.

**The evidence.** `ValidateEntryTests` (152): a table of 80 refusals, at least one for each of the 49 codes, each with
its code, identifier, argument and position, checked through the entry, the constructor and `Compile`; the code table
and its catalogue rows (EE0184 to EE0213 and EX0026); the fourteen patterns of EG0002's split; the critic's
`\k<b>(?<a>x)` under `u`, `v`, none and `i`, with its controls; the nesting limit at 511 and 512, the caller's limit
under `v`, a syntax error met before the limit, and a million levels of groups, non-capturing groups and `v`-mode
classes refused on a 1 MiB thread; the arguments; the default diagnostic, which `CharSet.AddRange`'s refusal still
carries; no expansion; class V; a property escape its mode refuses, refused before it is expanded; and the agreement
with `Compile` over the suite's and task 2.4b's seeded populations at the maximum limit and at 2, more than 10,000
compiled, 1,000 refused and 100 past the limit. `EngineLiteralTests` (127) transcribes the engines' literal tests as
data: Broiler.VM at `58b0c84`, `BinderChecks.cs` lines 119 to 121, `regexp-pattern-modifiers.js` line 20,
`the-v-flag.js` lines 11 and 14 and `the-unicode-property-escapes.js` cases 34 to 61 (48 cases); Broiler.JS at
`238069ab`, every case of `RegExpLiteralEarlyErrorTests` and the literals of `ParserTests.cs` lines 420 and 977 (78
cases); 81 refusals, each with the code this library answers, and 45 admissions, and one case where the test and the
specification differ (below). The adversarial corpus's N03, N06, N09, N12, N15, N18 and O19 answer their declared
statuses through the probe's corpus runner in a child process, which gains a `validate` operation
(`probes/Regex.Probe/EngineRun.cs`, the one edit of `probes/` this task makes beside its own cases), and
`A_validate_row_is_answered_by_the_validate_entry_with_no_meter_and_no_budget` runs the seven under a meter of one unit
and a compile budget of one unit, and V05's pattern as a validation, where a compilation answers otherwise: before this
task the runner answered a `validate` row through its compilation, whose statuses for the seven rows are the declared
ones, so the rows alone would not show which entry ran. The probe's population gains E58 to E68: the seven rows, 511
levels admitted, V05's pattern admitted under a budget that refuses its compilation, the `\k` case without flags, and
`(` under the flags `gg`, refused at the flags. Each behaviour's test failed first against a stub of it, the logs kept
with the task's notes: the entry answering `Success` for every pattern (173 of 264 failing), the parser's refusals
without a diagnostic (172 of 264), a range out of order outside `v` at -1 (3), the entry expanding (12 of 12), a
property expanded before its early error (4 of 4), the caller's limit ignored (2 of 2), and the probe's runner without
the operation (the new corpus test). The fuzz target's oracle compares the entry with each compilation (below): a stub
answering `Success` past the nesting limit is reported, first at iteration 18,000 with four worker processes.

**What it changes, observably.** A class range out of order outside `v` is refused at the range's first character, where
its position was -1 (the check was `CharSet.CheckRange`'s, which knows no position; `v` already gave it); and a property
of strings negated or outside `v` is refused before it is expanded, so `Compile` charges the pattern's read alone where it
charged the expansion first. Messages are unchanged. The parser's 46 refusals are built by `RegexParser.Refuse`, their
numbers formatted through `NumberFormatInfo.InvariantInfo`, so the starting violation list loses the parser's seven
places of the SL4 row and keeps `CharSet.cs:118`, the public `CharSet.AddRange`'s check, which no compilation reaches with
a range out of order; two citations moved with their lines. `ParseAtom`'s two refusals of an atom at the end or at `)`
or `|`, which no pattern reaches (`ParseAlternative` ends an alternative there first), are removed.

**The fuzz target.** The oracle (`Verdicts.Validate`) calls the entry under each compilation's nesting limit and
requires the same status and, for a syntax error, the same code, identifier, argument, position and message wherever that
compilation met neither its budget nor its meter; a diagnostic its status rules out is a contract finding. The session
must reach `validate:ok`, `validate:syntax` and `validate:limit:nesting`: at the declared seed 31,506, 10,826 and 5,668
of the 48,000 validations. The declared session (seed 20261027, iterations 0 to 23,999, as CI runs it): no finding, exit
0, every construct class and outcome reached. Its digest moved twice: the library change alone made it
`d2c0d2e09805b037b8abe4cd562a1c888c68f72f61d998fb14698fe33e4e0246` (353 iterations answered otherwise, 294 of them a
range's position and 59 a property's units, 19 of whose tight compilations now answer the syntax error where they met
their budget or meter), and the validate comparison, which adds its answers to each record, made it
`1fedb4b47f2f4cd45c45f03f78473b130f6e0504a4275b823adebd624f2e24c8`, the same over two runs with four worker processes and
over three under `tr-TR`, 34 seconds on four cores beside other builds. The control still reports the plant: at
iteration 52 with one worker process, at 6,008 with four.

**The review (2026-10-27).** Four findings, each fixed in its own commit. `NestingLimit` answered an identifier of
this library's, `regexp.pattern.nesting-limit`, for a condition the catalogue names, EX0026; it answers EX0026's
`limit.regexp.too-deep`, and the records no longer say the catalogue names early errors only (blocking: before the
identifiers count as published). The codes that split EG0002 were not recorded as a split; they are, below. The records
said every `RegexSyntaxException` the library throws carries its diagnostic; `CharSet.AddRange`'s does not, and they now
say so and leave it to task 2.9. RegExpInitialize was cited as §22.2.3.4, ParsePattern's section; it is §22.2.3.3. The
declared fuzz session after them: no finding, its digest unchanged
(`1fedb4b47f2f4cd45c45f03f78473b130f6e0504a4275b823adebd624f2e24c8`; the transcript records codes by number, not
identifiers); the AOT probe's report byte-identical to the JIT's.

**What task 2.9 still owes from this task.** The shape of `Compile`'s, the constructors', `RegexParser.Parse`'s and
`RegexFlagsParser.Parse`'s syntax error, a status or an exception admitted by the owner, with C3's row; the provisional
marks of this task's surface (both `Validate` overloads, `RegexDiagnostic` and its members, `RegexDiagnosticCode` and
its 50 members, `RegexStatus.SyntaxError`, `RegexSyntaxException.Diagnostic`); whether `RegexFlagsStatus` stays beside
the three codes of the flags; the SL4 place left in `CharSet.CheckRange`, with the public `Ast` surface, and the
diagnostic its refusal lacks; and, when `Compile`'s refusal becomes a status, the fuzz oracle's admitted answers
(`Verdicts.Compile`) and its comparison of the two entries, which then reads the status. **SJL-11.4 and SJL-11.5** call
the entry for a literal's early errors (Broiler.VM's binder `CheckRegExp`, Broiler.JS's `RegExpValidator`) and map its
identifiers under JSD-0083's D7. **Task 2.14** runs the corpus's validate rows through the operation added here. **The
shared catalogue** (Broiler.JS's `early-errors.tsv`) owes the adoption of EG0002's split (below).

*Where the plan, JSD-0081 and this repository differ, read at this record.*
- **The meter.** Plan row 2.8 gives the entry "a nullable meter"; D18 as JSD-0081 files it gives it none, which this
  task follows (above).
- **The engines' identifiers.** JSD-0083's D7 has the provider answer "the failing condition's message identifier", and
  otherwise EE0019's `regexp.literal.invalid` with the validator's message as its argument. The catalogue names early
  errors (EE), Annex B's (EG) and limits and grammar (EX), and where it has a row for a condition the code uses its
  identifier, EX0026's `limit.regexp.too-deep` for the nesting limit among them, but for EG0002's (next). It has no row
  of their own for the grammar's refusals and the flags', which it folds into EE0019, so those have identifiers of this
  library's, which an adapter maps to EE0019 with the message, or the catalogue adopts.
- **Catalogue row EG0002** (`annexb.regexp.unicode-mode`, JSE8107) is one condition for every Annex B extension of the
  pattern grammar refused under `u` or `v`; Broiler.VM's correspondence (`58b0c84`, row B25) hands it to the provider with
  EE0019 and EX0026. The validator tells the extensions apart, and D7 asks for the failing condition's identifier where
  the validator can tell it, so the codes split EG0002 and keep their own identifiers: wholly `LoneSyntaxCharacter` (17,
  a lone `]`, `{` or `}`), `InvalidIdentityEscape` (21, a legacy octal escape and `\8` in a class among them),
  `InvalidControlEscape` (22) and `PropertyEscapeWithoutBrace` (80, `\p` or `\P` without `{`); in part
  `QuantifiedAssertion` (14, a quantified look-ahead), `InvalidHexEscape` (23, `\x` or `\u` without its digits) and
  `NamedReferenceWithoutName` (32, `\k` without `<`). The row's other examples, `\8` without an eighth group and a class range ending in a class escape,
  are EE0192, EE0193 and EE0195's and take their identifiers. **The catalogue is to adopt the split** (rows of its own
  for the seven, or EG0002 noted as split), with the identifiers this library publishes; until it does, an adapter that
  knows only the catalogue's identifiers maps a refusal of one of the seven under `u` or `v` to EG0002 where what it
  refuses is an Annex B extension (the pattern parses past that place without `u` and `v`), and to EE0019 where it is
  not (`^*`, or a `\u` escape short of its digits in a group name).
  `ValidateEntryTests.An_Annex_B_extension_refused_under_u_or_v_has_its_own_code_of_row_EG0002` pins each with a pattern
  Annex B admits without `u` and `v`.
  *Dated note, 2026-11-01 (the integration of tasks 2.8 and 2.14 of SJL-11.2): the list read "wholly
  `LoneSyntaxCharacter` (17, …), `InvalidIdentityEscape` (21, …) and `InvalidControlEscape` (22); in part
  `QuantifiedAssertion` (14, …), `InvalidHexEscape` (23, …), `NamedReferenceWithoutName` (32, `\k` without `<`) and
  `PropertyEscapeWithoutBrace` (80, `\p` without `{`)". Code 80 is wholly EG0002's: the parser refuses `\p` or `\P`
  not followed by `{` only under `u` or `v`, and without them the same text is an identity escape wherever it stands,
  in a class or out of one, so every refusal with code 80 is of an Annex B extension. It is now listed with the codes
  wholly EG0002's, and the test's comment says the same.*
- **Broiler.JS's `ParserTests.cs:420`** admits `/[\q{/}--[a]]/v`, which ECMA-262 refuses (under `v` a `/` is a
  ClassSetSyntaxCharacter, which a `\q{…}` may not hold unescaped) and V8 (node 22.22.0) refuses too; this library
  refuses it with `ClassSetUnescapedSyntaxCharacter` at 4, and admits `/[\q{\/}--[a]]/v`. The engine's test owes the
  correction, at SJL-11.5 at the latest; `EngineLiteralTests` keeps the case apart.
- **Broiler.VM's `the-unicode-property-escapes.js`** says in a comment that the `v` flag "stays refused", while its
  case 60, `[\p{L}]` under `v`, is admitted since that realm's phase F2, as its expected output says and this library
  answers.
- **Task 2.5's record** said the probe answers `unsupported` for a `validate` row. It answered one through its
  compilation, which gives the seven rows' declared statuses; `unsupported` was the answer only after a compilation
  that succeeds.
- **The package README** listed two class-set defects of task 2.2's review as open (`\b` in `\q{…}`, and an escaped
  lead and trail surrogate in `\q{…}` read as one code point); the integration of tasks 2.6 and 2.11 fixed both
  (`28835d1`), and this task removes the item.
- **Check A-5** asks for `\k<name>` to an undeclared name "refused by the parser and by the validate entry": both
  refuse it, with `UndefinedGroupName` at the `\k`, under every mode where `\k` is a reference.

*Dated record, 2026-10-27 (task 2.14 of SJL-11.2; bundle `sjl-11-001`'s part C, the milestone's component clause 3;
decisions D6, D8, D9, D10 and D32).*

**What it builds.** The probe's `--adversarial corpus [--report path]` mode (`probes/Regex.Probe/AdversarialMode.cs`;
[the probe's README](../probes/Regex.Probe/README.md), new with this task, describes every mode of the probe). The probe
starts itself once per row of the corpus, one at a time, in ordinal order of the ids, with its hidden argument
`--engine-row`: under the JIT as `dotnet` and the probe's assembly, under Native AOT as the image itself. The child reads
the corpus, builds the row's pattern and subject from their expressions and runs its operation under its policy through
the new backend's entry, all on a thread of the header's `stack-bytes`, 1 MiB, and writes `id status` and its units. The
parent keeps each child's exit code, status line and units, and judges the row: it **gives its declared status** (the
status with the corpus's `x^k` written out equals the declared one), it **differs**, or the child **ended abnormally**
(an exit other than 0, anything but one line naming the row, or no exit within 120 seconds, when it is stopped). A row
whose operation is `validate` **waits for task 2.8** and is not run, while `EngineRun.Answer` answers that operation
`unsupported`; it is named and counted. The report is deterministic: the corpus's SHA-256 and header, one line per row,
the counts, the rows waiting and the rows reported; no time, path or machine, and no child's standard error, which
differs between the JIT and Native AOT when a child crashes. The mode exits 1 when a row differs or a child ends
abnormally, 0 otherwise, and 2 on a corpus it cannot read. The corpus has one reader now, `AdversarialCorpus.cs`: the
child mode reads it, where it scanned the file for its row, and `Broiler.Regex.Tests` links it with `EngineRun.cs`, so
that `AdversarialCorpusTests` reads the rows, expands the statuses and evaluates the expressions as the probe does, where
it had a reader, an expansion and an evaluator (through `System.Text.Json`) of its own. `AdversarialModeTests` runs the
mode over scratch corpora: a row given, one that differs, a child that ends abnormally (a start past the subject's end,
a broken precondition the run does not answer as a status) and a `validate` row, reported in id order with the corpus's
SHA-256 and exit 1; a passing corpus, its report the same twice; corpora it cannot read, exit 2; and the judgement of a
child directly. Four mutants of the mode (a status not compared, the rows in file order, a non-zero exit with a right
line judged given, the exit always 0) each fail one of its tests.

**The reading at this commit**, on linux-x64 locally, the corpus's SHA-256 `1a0f8c56bb970fa784fc6e573e9968e939d298d3e912320ed8c1a407fa997dd5`
(the one both engines' manifests name): of the 65 rows, **58 give their declared status** under the JIT and under Native
AOT, none differs and no child ends abnormally; **7 wait for task 2.8**, the `validate` rows N03, N06, N09, N12, N15,
N18 and O19. The two reports are the same file (SHA-256 `0028536f72e4ef6915a2d5cc940c4f84e36863ef0198ffbb2ac38a035193454e`),
the Native AOT run in about 2 seconds and the JIT's in about 6. Given: K01 to K05, A01, A02, L01 to L05, W01, W02 and G01
to G03 `limit:steps`; M01 `limit:frames` and M02 `limit:trail`; N01, N07 and N13, at the limit, their matches; N02, N04,
N05, N08, N10, N11, N14, N16 and N17 `limit:nesting`; F01 to F04 their matches; V01 to V05 `limit:compile`; O01 to O18
their statuses (O12 the meter's exception, O18 `syntax`). The win-x64 reading is CI's.

**Part C's controls**, each a scratch build of the probe (`probes/Regex.Probe/AdversarialControls.targets`): with
`-p:RegexAdversarialControl=steps` or `=nesting` the probe does not reference the library project but compiles the
library's sources into itself, one declared line changed in a copy under `obj/`, the build failing unless the line is
in its file exactly once; the line keeps a text of its own, `adversarial control 1 (part C)` or `... 2 ...`, by which a
search finds it. `eng/adversarial-controls.py` reads the corpus itself and holds a report to exactly the rows its
control must report, every other row to its declared status or, for a `validate` row, to waiting, and the exit code to
the rows reported.

| Control | The line changed | Reported, under the JIT and under Native AOT |
|---|---|---|
| (1) `LimitExceeded` naming steps answered `NoMatch` | `RegexProgram.Match`, after the machine runs | Exactly the 17 K, A, L, W and G rows, each `nomatch` where it declares `limit:steps`: differs. 41 rows give their declared status, 7 wait |
| (2) The nesting check removed | `RegexNesting.Descend`'s test of the caller's limit | Exactly the 9 N rows past the limit that run: N02, N08 and N14 compile (`ok`) and differ; N04, N05, N10, N11, N16 and N17 overflow the 1 MiB stack in the parser and end abnormally (exit 134 under the JIT, 139 under Native AOT). N01, N07 and N13 give their declared statuses; the six `validate` rows past the limit wait and are not reached |

Each was reverted, the probe built without the switch, and the revert checked: its report gives every row run its
declared status and reports none. **The controls never reach the shipped DLL**, read as task 2.11's record reads its
plant: from one scratch directory holding this record's code, `Broiler.Regex.dll` built from the library project alone
and by the whole solution is one file (SHA-256 `062788e853b92c296c661c8ef7a41a831e752939f0fcf018f8e8f7ee42b49aa4`, a
value of that directory alone); each control's build afterwards built no library project and left that file byte for
byte and its time unchanged; the package packed from it carries the same file; and neither DLL holds `adversarial
control`, `part C` or `RegexAdversarialControl`, in UTF-16 or UTF-8, where each control's probe assembly holds its text.

**CI.** The `native-aot-probe` job runs the mode under Native AOT and under the JIT on ubuntu-latest and windows-latest
and compares the two reports with `cmp`, a step failing on a row that differs or a child that ends abnormally, each
step bounded by 10 minutes. The new `adversarial-controls` job, on ubuntu-latest within 20 minutes, builds and checks
each control, shows that no control built `Broiler.Regex/bin`, then builds the probe as usual, checks the revert and
searches the library's DLL for the controls' text; its steps took 35 and 36 seconds on two fresh copies of the tree at
`57002e0` locally, every build without build servers (11 to 13 seconds for each of the three builds and runs). Two
earlier local measurements of the same steps took 42 seconds, the figure commit `13bda4a`'s message gives, of a copy
taken before two small edits of that commit's code, and 30; so about 30 to 45 seconds. The reports are kept as
artifacts of the runs, which the bundle's README now names as part C's evidence at a reading.

**Rule SL3 and the probe.** Rule SL3 refused `System.Diagnostics.Process` for the probe, which may do file I/O
only, so the mode, which starts processes, failed it (the probe named `Process` and `ProcessStartInfo`). The probe's
exemption now covers a process, as the register's own wording, "file, network or process I/O", already counted it among
the I/O the probe is exempt from; the witness starts a process, refused for a library and admitted for the probe, and
both SL3 tests were watched failing without the admission. `docs/conventions.md`, "Scope", carries a dated note.

**The library is unchanged**: no library file changed, and the probe's default report is the same file at `bdcf39d`
and at this record's commit, under the JIT and Native AOT (SHA-256
`3fb73580c649682cc99e7ec3a43352f309a8c43c2fef85f6123bf6a840c5d798`).

**What remains.**
- **Task 2.8.** The seven `validate` rows run once `EngineRun.Answer` answers the operation, with no change to the mode;
  `AdversarialCorpusTests`' list of rows that wait is that task's. Control (2) reaches the six `validate` rows past the
  limit only if the validate entry checks nesting through `RegexNesting.Descend`, the control's line; an entry that
  checks it elsewhere needs its line declared in the control too.
- **Part C's judgement.** MET needs every row's declared status in both modes on both RIDs, so it cannot be judged
  before task 2.8; it is judged at the candidate reading, from CI's runs at the commit `0.2.0-preview.1` is packed from,
  win-x64's among them.
- **The margin.** The corpus's README (section 3) asks that `regexpref`'s counts be recorded before the candidate
  reading; that is SJL-11.3's reference, not this task's.

*Where the plan, the filed rule and this repository differ, read at this record.*
- Part C reads the same in the plan's section 4.3 and in the filed rule. The rule's "the smallest stack the corpus's
  header declares" is its one `stack-bytes`, 1 MiB.
- Plan row 2.14 names the probe as its files. The mode also needed rule SL3's admission of the probe's processes (above),
  the test project's links, `eng/adversarial-controls.py` and a CI job; no library file.
- The brief asked for each control as "a build-time switch compiled only into a control build of the library or the
  probe". Convention 17 admits no preprocessor directive, so the switch is an MSBuild property of the probe's build, and
  the library's code reaches the control only as a copy compiled into the probe, as task 2.11's plant does.
- Control (1) changes the answer at `RegexProgram.Match`, the entry every public member and the probe call, as the rule
  words it ("LimitExceeded naming steps is answered NoMatch"). The old backend's form of the defect, a path's exhaustion
  read as that path's failure, is a different change, and the corpus's README describes it per row.
- The rule asks "each child's exit and status line retained". The report keeps both, and the units a child wrote alone
  on its standard error; it keeps no other standard error, whose text differs between the modes when a child crashes.
- The corpus's README (section 1) says three header lines are read. The probe reads two, the format and the stack; the
  third, the wall-clock limit, is Broiler.JS's programs'.
- The brief names the probe's README, which did not exist: it is new with this task.

*Dated note, 2026-11-01 (the integration of tasks 2.8 and 2.14 of SJL-11.2, branch `sjl-11-2-integrate-c`, task 2.14
merged first and task 2.8 onto it): task 2.8's validate entry runs the seven `validate` rows, so the reading above,
part C's control (2) and what remains are no longer the head's, and the record is kept as written.*

- *The reading.* The record read "of the 65 rows, **58 give their declared status** under the JIT and under Native
  AOT, none differs and no child ends abnormally; **7 wait for task 2.8**, the `validate` rows N03, N06, N09, N12,
  N15, N18 and O19". At the integration, on linux-x64 locally, all **65 rows give their declared status** under the
  JIT and under Native AOT, none differs, no child ends abnormally and **none waits**; the two reports are the same
  file (SHA-256 `3f7fc5387c9015ebe26436f0e40334755a05a4e214d33d6b68f558e0ea70dc75`), the Native AOT run in about 2
  seconds and the JIT's in about 7. N03, N06, N09, N12, N15 and N18 answer `limit:nesting` and O19 `syntax`.
- *Control (2).* The table read "Exactly the 9 N rows past the limit that run" and "the six `validate` rows past the
  limit wait and are not reached". The control now reports **all 15 N rows past the limit**, under the JIT and under
  Native AOT: beside the nine the table names, N03, N09 and N15, one level past the limit, answer `ok` through the
  validate entry and differ, and N06, N12 and N18 overflow the 1 MiB stack in the parser and end abnormally (exit 134
  under the JIT, 139 under Native AOT); N01, N07 and N13 and the 47 rows of the other classes give their declared
  statuses. The validate entry checks nesting through `RegexNesting.Descend`, the control's line, so the control
  reaches its rows with no line of its own. Control (1) reports the same 17 rows, the other 48 giving their declared
  statuses, where the table's cell for it read "41 rows give their declared status, 7 wait". Each was reverted and
  the revert checked, its report the one above; the control builds left `Broiler.Regex.dll` byte for byte and its
  time unchanged, and the DLL holds neither control's text.
- *The script.* The record above described it as holding "every other row to its declared status or, for a
  `validate` row, to waiting". `eng/adversarial-controls.py` held control (2) to "every N row past the limit (declared
  limit:nesting) that runs" and admitted "a row whose operation is validate, which may wait for task 2.8"; it took the
  rows that wait out of those a control must report. It now holds control (2) to all 15 and refuses a report with a
  row that waits, under any control. Shown on a scratch copy of the tree with the probe's `validate` operation planted
  out (`EngineRun.Answer` answering it `unsupported`, so the seven rows wait): task 2.14's script passed the nesting
  control's report (9 rows reported, 7 waiting) and the revert's (58 given, 7 waiting); this one fails both, naming
  N03, N06, N09, N12, N15 and N18 as not reported and the seven rows as waiting.
- *CI.* The `adversarial-controls` job's steps took 46 and 50 seconds on two fresh copies of the tree at the
  integration, without build servers, on a busier machine than the record's (load average about 8).
- *What remains.* The record's first item, "**Task 2.8.** The seven `validate` rows run once `EngineRun.Answer`
  answers the operation, with no change to the mode", is done, with no change to the mode, and
  `AdversarialCorpusTests` lists no row that waits. Its second read "MET needs every row's declared status in both
  modes on both RIDs, so it cannot be judged before task 2.8"; part C is now judged at the candidate reading alone,
  from CI's runs at the commit `0.2.0-preview.1` is packed from, win-x64's among them. The margin is unchanged.
- *The exit.* The record read "The mode exits 1 when a row differs or a child ends abnormally, 0 otherwise", so a run
  with rows waiting passed: with the probe's `validate` operation planted out of `EngineRun.Answer` (and the plant
  reverted after), the mode reported the seven rows waiting and exited 0. It now exits 1 when a row waits too (`AdversarialMode.Exit`),
  as no row may since task 2.8; under the same plant it exits 1. `AdversarialModeTests.A_row_that_waits_fails_the_run`
  was watched failing before the change (the exit over a waiting verdict was 0) and, under the plant, failing before
  and after it; `eng/adversarial-controls.py` holds the exit code to 1 when a row is reported or waits, where it read
  "1 if reported else 0". The report's format, `adversarial-1`, is unchanged, and `AdversarialModeTests` asserts the
  `validate` rows' lines with no branch for a runner that does not answer the operation.

*Dated record, 2026-11-02 (task 2.12 of SJL-11.2; bundle `sjl-11-001`'s part D, the vectors at library level, template
T3; decisions D25 and D-partd of Broiler.VM's JSD-0081, and D-s5 for the fix below). Placed after task 2.14's record,
its sibling of part C, rather than after task 2.9's, so that it merges beside task 2.13's record without touching it.*

**What it builds.**
- **The pinned copy** ([`tests/Vectors/README.md`](../tests/Vectors/README.md)): `tests/Vectors/regexp/`, the eight files
  of Broiler.JS's `conformance/vectors/regexp/` at `b2c2e2243a086285d806866a64b83297791a8ec5` (`main` after
  Broiler.JS#1089), corpus revision 32, byte for byte (1,420 vectors; the files last changed at `de8d7b54`), and
  `tests/Vectors/regexp.pin`, which names the commit, the revision and each file's length, SHA-256 and git blob; each
  committed blob is the pinned one. The check comes before every run: every named file with its length and SHA-256,
  and no `.vectors` file the pin does not name, or nothing is read. `THIRD_PARTY_NOTICES.md` names the copy.
- **The runner** in `Broiler.Regex.Tests/Vectors/` (the rule's "the library runner in Broiler.Regex.Tests"), in
  Broiler.ECMAScript's `LibraryRun` shape where it applies: each op called on the decoded `in`, the answer rendered in
  SCHEMA.md's notation, verdicts `match`, `difference` and `not-implemented`. **The op mapping**, through the public
  surface alone, under the policy both engines pass (D7 and D10: `Compile` at nesting 512, no budget, no meter; the
  engines' ceilings):

  | Op | The library |
  |---|---|
  | `RegExpInitialize`, and its nested form | `RegexFlagsText.Parse`, then `BroilerRegex.Compile`; `value: bool:true`, `error: SyntaxError`, or the limit's status |
  | `IsValidRegularExpressionLiteral` | `BroilerRegex.Validate` over the flags text |
  | `RegExpBuiltinExec`, and its repeated form | `Compile`, then `Match(string, int)` from `lastIndex` (0 without `g` and `y`; past the end, null); `lastIndex` written under `g` or `y`; the canonical serialisation, `groups` in the order of the names' first groups (`RegexGroupNames.NameAt`, the participating group by `GroupsNamed`), `d`'s indices |
  | `RegExpBuiltinExec, step budget` | `Compile`, then the offset entry with a step ceiling of the vector's budget, the engines' frame and trail ceilings and a meter that counts; `LimitExceeded` naming steps is `status: exhausted` |
  | `RegExp.prototype.test`, `[ %Symbol.search% ]` | `Match(string, 0)`: `Success`, or `Index` and -1 as a Number |
  | `RegExp.prototype [ %Symbol.match% ]` | `Match(string, 0)`'s groups without `g`; with it every match through `NextMatch` |
  | `RegExp.escape`, and its round trip | `RegexEscaper.EscapeString`; the round trip compiles the text and tests the string |
  | `get RegExp.prototype.flags` | `Compile` of `a`, then `RegexFlagsText.Format` |
  | `EscapeRegExpPattern, one P and F` | `Compile`, then `RegexEscaper.EscapePattern` of the compiled `Pattern`: at library level the literal's body and the constructor's pattern are one string by construction (`Pattern` is the caller's text, and the library has no literal scanner), so the vector checks only that the two calls succeed |
  | `EscapeRegExpPattern, a literal of S` | S = `EscapePattern(Pattern)`: no line terminator, a RegularExpressionBody (§12.9.5), valid under F, and matching the subject as the pattern does |

  The six ops of the object model are not implemented and are listed by name, with the reason, in every report:
  `RegExp.prototype [ %Symbol.split% ]` (12 vectors), `String.prototype.search` (6), `RegExp.prototype accessors` (4),
  `RegularExpressionLiteral, eval code` (3), `get RegExp.prototype.flags, ordinary object` (1) and `get
  RegExp.prototype.flags, read order` (1). An op neither mapped nor listed renders a fault, a difference, so a later
  revision's new op cannot pass unread. A limit the specification does not have is the library's status, never an
  engine's rendering: `status: exhausted` for steps, `status: limit-nesting`, `-frames`, `-trail`, `-compile` else.
- **The verdicts.** A `normative` or `divergence` vector is held to its `out` (a divergence field classifies an
  engine's difference and names no library), the 18 status vectors among them; an `impl-defined` vector is reported
  with the library's answer and what it meets (`out`, each engine's `per-engine` field, the nesting status as D7's
  SyntaxError, or none). The report is ordered by id, holds no time or path, and is made on a 1 MiB thread.
- **The probe** ([its README](../probes/Regex.Probe/README.md)): `--vectors dir [--report path]` in its own file,
  `VectorsMode.cs`, compiling the runner's sources in; exit 2 on a refused pin with no report. CI's `native-aot-probe`
  job runs it under Native AOT and the JIT on ubuntu-24.04 and windows-latest and compares the reports with `cmp`,
  each step within 10 minutes; locally about 2 s each.

**The reading at this commit** (linux-x64, the report's SHA-256 `3b35a4757d294b97441548b93b1fbf31d44587eaffb47055a4ad3b2888603e1b`,
the same file from the Native AOT image and under the JIT):

| Class | Vectors | Implemented | Gives its `out` | Differs | Not implemented |
|---|---|---|---|---|---|
| normative (status vectors aside) | 1,086 | 1,066 | 1,066 | 0 | 20 |
| divergence | 231 | 224 | 224 | 0 | 7 |
| status (`status: exhausted`, all normative) | 18 | 18 | 18 | 0 | 0 |
| impl-defined | 85 | 85 | reported | | 0 |

Of the 85 impl-defined vectors, 68 meet `out` and both engines' expectations, and 17 answer `status: limit-nesting`,
the status both engines render as decision D7's SyntaxError, which their `per-engine` fields hold (`out`, the
specification's, sets no bound): `regexp/syntax/s3-nesting/0011` to `0020`, `regexp/syntax/s3b-deep-nesting/0001` to
`0003`, `regexp/literals/s3-nesting/0002` and `regexp/matching/s3-nesting/0004` to `0006`. **Every normative vector
whose op the library implements gives its `out`**, the six budget vectors that complete among them, and every status
vector its status. The 8 vectors of `EscapeRegExpPattern, one P and F`
(`regexp/text/t1-source-literal-and-constructor/0001` to `0008`, 4 normative and 4 divergence, the divergence ones
those where Broiler.JS answers two texts) are among those counts but check only that `Compile` and `EscapePattern`
succeed, since their P is one string by construction at library level (the op's row above); the literal scanner they
are about is the object model's, judged by the engines.

**The finding, fixed.** At the branch's start (`6ceb0df`'s library, the report SHA-256
`f78ee318aaac80b498c75660430ef1bad2cb61c76e8338406cc61cfd52ca458b`) the same counts held but for four impl-defined
vectors that met none: `[\k<a>](?<a>x)` and `[\k](?<a>x)` through RegExpInitialize
(`regexp/syntax/s5-named-references/0012`, `0014`), `[\k<a>](?<a>x)` as a literal (`regexp/literals/s1-s5-names/0001`)
and `[\k](?<b>x)` on `kx` (`regexp/matching/s4-escapes/0031`), each refused with `NamedReferenceInClass`. Decision D-s5
records that `regexpref` refutes the reading the refusal rested on: no production passes [NamedCaptureGroups] into a
CharacterClass, so B.1.2's ClassEscape reads ~NamedCaptureGroups and admits `k`. A library defect within this task's
reach, so fixed (`2e35cbb`): the parser's refusal is gone, `NamedReferenceTests` was watched failing first (five of five
cases), and `RegexDiagnosticCode.NamedReferenceInClass` is retired, its number kept; no public signature changed.
No vector was found defective, and nothing is reported to the corpus's owner.

**What the earlier records owed this task, and what became of each.**
- Task 2.7's record: "the vectors' flag, escape and `source` operations mapped onto these helpers": done, the table above.
- Task 2.11's record ("the vectors and the reference arms do", for whether a match is the specification's): the
  reading above; the integration's note sends `[\q{\uD83D\u{DE00}}]` under `v` to the vectors and the arms next, and no
  vector of revision 32 holds it, so it stays the reference arms' (task 2.13) or a later revision's.
- Convention 12: the probe "runs bundle `sjl-11-001`'s part D, the vectors at library level, from task 2.12 on": done.
- Rule SL9's statement ends "The vectors at library level replace the population when SJL-11.2 adds them". SL9 still
  reads the probe's population; `VectorsRunTests` holds the vectors' report equal over two runs and two concurrent
  threads, which is SL9's check over the vectors. Moving SL9 itself onto them is a change of the architecture tests'
  register, not made here; it is left to the orchestrator (below).

**Checks at this commit**, linux-x64: the build without a warning; `Broiler.Regex.Tests` 2,631 passed,
`Regex.Architecture.Tests` 49 (the violation list empty), `Regex.Fuzz` 11; the declared fuzz session no finding, its
transcript digest `902fbd151db4d37af1cff3a2424e4df0dc8dc768879ef190a22313ff536ee39d` where `6ceb0df`'s is
`c2ed95c98feb225ec1df3eccf378d9f4804aab02f410d48299be02891e4c5d5b`, since 98 more of its generated patterns compile
with `\k` in a class; the probe's default report and `--adversarial`'s byte-identical to `6ceb0df`'s and between the two
modes; part C's control 1 still builds with the runner compiled in and reports its 17 rows; the Native AOT publish
without a warning; package validation against `0.1.0-preview.5` with `CompatibilitySuppressions.xml` unchanged, and
the packed package holding no vector and no runner; the assurance check without a violation after regeneration.

**What remains.** Part D's judgement is the candidate reading's, from CI's runs at the commit `0.1.0-preview.6` is
packed from, win-x64's among them. A later corpus revision is a new copy, a new pin, a dated note in the bundle's
README and a new reading.

*Where the plan, the filed rule, the corpus and the library differ, read at this record.*
- The filed rule's population is the vectors "at the corpus revision bundle sjl-11-001 names", and this bundle named
  none; its README now names revision 32 at `b2c2e224`, Broiler.JS's part D's revision (D-partd). Plan row 2.12 says
  "at the named revision" and its pin "SHA-256 per file"; the pin also gives each file's length and git blob.
- The rule's MET clause names "every normative vector"; the runner holds the `divergence` vectors to their `out` too,
  as SCHEMA.md section 5 says their `out` is the specification's. Status vectors are `normative` in the corpus.
- The corpus's generator lists as the object model's `String.prototype.search`, the flags getter over an ordinary
  object and "the invariants of EscapeRegExpPattern (`source`) that its text fixes"; task 2.7's record asks for the
  `source` operations mapped onto the helpers, so the two EscapeRegExpPattern ops are mapped, the second with the
  literal grammar of §12.9.5 checked in the runner and the pattern's own answer, not the script's embedded reference
  answer, as the comparison. The first's comparison of a literal scanner's P with a constructor's is the object
  model's, as the generator says: mapped, it exercises only `Compile` and `EscapePattern` (the op's row above).
  `[Symbol.split]`, which the generator does not list, is listed as the object model's: no
  member of the library splits.
- Broiler.ECMAScript's `LibraryRun` compares an impl-defined vector with the engines' agreeing `per-engine` field;
  part D asks for impl-defined vectors to be reported, so they are reported with what they meet, and none is judged.
- The library answered B.1.2's class `\k` otherwise than the text, by its own reading of task 2.2 (fixed above), and
  Broiler.JS's `RegExpLiteralEarlyErrorTests` case `/(?<a>.)[\k]/`, transcribed in `EngineLiteralTests`, expects the
  refusal D-s5 makes a correction of Broiler.JS.

*Dated record, 2026-11-01 (task 2.9 of SJL-11.2; R8 and R10's conventions 1, 3, 4, 16, 17 and 18; decisions D9, D10
and D12, D12 as the owner revised it; checks C1, C3 and C5 and rules SL4, SL5 and SL8).*

**What it builds.** The public surface the SJL-11 work ships, as `0.1.0-preview.6`, with nothing provisional, every
break of `0.1.0-preview.5` suppressed and listed with its R item, and the starting violation list empty. In order of
the branch's commits:
- *The version* (`81b89bc`). `Directory.Build.props` sets the stream's floor, `0.1.0` and `preview.6`, over the vendored
  packaging props' `preview.1`, which are not edited; `eng/preview-version-history.json` lists `0.1.0-preview.1` to
  `0.1.0-preview.5`, each of `preview.2` to `preview.5` checked against nuget.org's index of the package
  (`preview.1` is the retired feed's, not on nuget.org); the resolver's test of the committed history and floor answers
  `0.1.0-preview.6` (watched failing first), and so does the resolver against nuget.org (`DRY_RUN=true`). Package
  validation's baseline is `0.1.0-preview.5`; every break `0.1.0-preview.5` already shipped (tasks 2.2, 2.3, 2.4c and
  2.6) left `CompatibilitySuppressions.xml` and stays listed in `docs/suppressions.md` and the CHANGELOG as history.
- *`Compile`'s syntax status* (`2cbd408`). `Compile` gains a last `out RegexDiagnostic`: a pattern the grammar or an
  early error refuses answers `RegexStatus.SyntaxError` with the diagnostic the validate entry answers for it, and the
  nesting limit its `NestingLimit` diagnostic. 460 tests failed against a stub that kept the exception, and pass.
- *The flags* (`8f4765c`). `RegexFlagsText.Parse` answers `RegexStatus` with a `RegexDiagnostic`; `RegexFlagsStatus` is
  removed, its three refusals the codes `UnknownFlag` (1), `DuplicateFlag` (2) and `UnicodeWithUnicodeSets` (3);
  `RegexFlagsParser.Parse` is internal and `IsUnicodeMode` stays public. 29 tests failed against a reader whose refusal
  carried no diagnostic. *Correction:* that commit's message says the flags tests read "117,000 strings"; they read
  183,558 (109,601 orderings of subsets, 65,536 single code units and 8,421 strings of up to three units).
- *No public exception type* (`b6211b4`). `RegexSyntaxException` is internal and derives from `ArgumentException`; the
  string-bearing constructors document a pattern or flags text the specification refuses as their broken precondition.
  C3's row was watched failing on the old build and leaves the list.
- *The tree, the parser and the set algebra internal* (`616b640`): the seventeen types of `Broiler.Regex.Ast`,
  `Parsing.RegexParser` and `Unicode.UnicodeCharSets`; `CharSet.CheckRange` builds its message through
  `NumberFormatInfo.InvariantInfo`. The last SL4 row and nine C1 rows leave the list.
- *The string-bearing result* (`2785f56`). `RegexMatch.Groups` is a `ReadOnlySpan<RegexGroup>`, `RegexMatch.GroupNamed`
  answers a name's group, and `BroilerRegex.NextMatch` the global iteration; `Matches` and `NamedGroups` are removed.
  Three of four new tests failed against a stub; the last three C1 rows leave the list.
- *Nothing provisional* (`04869ac`). Every `# provisional:` mark of `PublicAPI.txt` is resolved and the documentation that
  called members provisional states their contract; `RegexMatchCeilings` moves to `Broiler.Regex`; the library's
  project resets `NoWarn` to `CS1701` and `CS1702`, so a public member without documentation fails the build, and
  `Pattern`, `Flags` and `RegexFlags.None` gain theirs. SL5's seven rows leave the list.
- *The assurance scheme strict* (`c03abd8`). `assurance.config.json` uses the strict exemption predicate and forbids
  preprocessor directives, as Broiler.Unicode's does; three fields the strict predicate counts carry annotations (382
  relevant units). SL8's two rows leave the list, which is now **empty**: row 2.9's check holds.
- *The records*, with this one: the CHANGELOG, `docs/suppressions.md`, `docs/conventions.md`, both READMEs, the fuzz
  target's README, bundle `sjl-11-001`'s README and rule (by dated note) and `docs/publishing.md`.

**The owner's decisions.** Each is the owner's decision of 2026-11-01, taken as recommended under the owner's direction
of 2026-10-18, "Please do all recommended decisions and skip waiting for review", beside the owner's own words of
2026-11-01, "Please keep using version 0.1.0-x scheme!".
- **The version.** The SJL-11 surface ships as `0.1.0-preview.6`, validated against `0.1.0-preview.5`; the dated note on
  "The version and the break (decision D12)" below quotes what it replaces.
- **`IsMatch` at a ceiling: kept, its exception admitted.** Decision D12 keeps the string-bearing members and R8 asks for
  an offset-only interface beside them, not for their removal, and Broiler.JS's adapter and the feed check's consumer
  call `IsMatch`; so the member stays, and its `InvalidOperationException` at a frame or trail ceiling is admitted as the
  string-bearing member's one documented throw. The status forms are `Match(string, int)`, whose `RegexMatch.Limit`
  names the ceiling, and the offset entry, which answers `LimitExceeded` with the `RegexLimit`; no member that answers a
  status throws for an expected failure. No `TryIsMatch` is added: the two status forms already answer it.
  *Dated correction, 2026-11-01 (the review of task 2.9).* This item read "and Broiler.JS's adapter and the feed check's
  consumer call `IsMatch`". Broiler.JS does not call `BroilerRegex.IsMatch`: at `66973a29` its only call into a
  `BroilerRegex` instance is `broiler.Match(input, start)` (`JSRegExp.Broiler.cs`, line 287), and its own `IsMatch`
  (line 347) is `RunMatch(input, 0).Success`, which goes through that `Match`; its SJL-11.5 adapter will call the offset
  entry. Only the feed check's consumer (`eng/verify-feed.ps1`, line 73) calls `IsMatch`. The decision stands on D12,
  which keeps the string-bearing members, and R8, which asks for the offset-only interface beside them and not for their
  removal.
- **`Compile` and syntax errors.** `Compile` answers `SyntaxError` with task 2.8's diagnostic. The recommendation kept
  the constructors throwing `RegexSyntaxException`; check C3 refuses the library's own public exception type and row
  2.9's check is an empty list, so that shape is ruled out, and the constructors throw an `ArgumentException` (the
  internal `RegexSyntaxException`) with the diagnostic's message, documented as their broken precondition (convention 3
  admits an exception there). `RegexParser.Parse` and `RegexFlagsParser.Parse` are **internal**, the choice that leaves
  the list empty: the public status forms are `Compile`, the validate entry and `RegexFlagsText.Parse`.
- **`RegexFlagsStatus`: folded** into `RegexDiagnosticCode`, the same names at the same numbers, which removes a second
  public type for one refusal.
- **The budget's place.** The compile budget stays a parameter after the nesting limit, and the meter after it, the shape
  Broiler.Unicode's operations take; `Compile(string, RegexFlags, IUnicodeTables, int nestingLimit, ulong?
  compileBudget, RegexWorkCharge? meter, out BroilerRegex?, out RegexLimit, out RegexDiagnostic)`.
- Beside them, as the earlier records left them to this task: the syntax tree, the parser and `UnicodeCharSets` are
  internal (conventions 1, 3 and 4; check A-1); the escape helpers take no length dimension of `RegexLimit`, an escape's
  length being bounded by its input's; a name none of whose groups participated answers its first group, unmatched; the
  ceilings live in the root namespace beside the offset entry.

**What the earlier records owed task 2.9, and what became of each.**
- Task 2.4c's record: the offset entry, `SlotCount`, `RegexWorkCharge`, `RegexMatchCeilings` and its namespace and
  `RegexMatch.Limit`: kept, marks removed, the ceilings moved to `Broiler.Regex`; `IsMatch`'s exception: admitted (above;
  `conventions.md`'s note is closed by a dated note); the remaining starting violations: none remain; the version and
  `eng/preview-version-history.json`: `0.1.0-preview.6`, the history through `preview.5`.
- Task 2.5's record: `Compile`'s signature (the budget's place, the syntax error) and `RegexLimit.Compile`: settled as
  above, `RegexLimit.Compile` kept; the marks: removed; the public `RegexParser` and `CodePointSet` operations that take
  no meter: internal.
- Task 2.7's record: the text helpers' surface, a length dimension or not, and `RegexFlagsParser.Parse`'s exception: kept
  with no length dimension, `RegexFlagsText.Parse` reshaped onto the diagnostic, `RegexFlagsParser.Parse` internal.
- Task 2.6's record: `RegexGroupNames` and its five members, `RegexGroup.Number` and the two `GroupNames`: kept, marks
  removed (`RegexParser.GroupNames` internal with its type); `RegexMatch.NamedGroups` and `Groups`: replaced by
  `GroupNamed` and the span; the unmatched entry of a name no group of which participated: its first group.
- Task 2.11's record: the fuzz oracle's admitted answers once `Compile` answers a status: narrowed (`Verdicts.Compile`
  takes any exception from `Compile` as a finding; the constructor's comparison catches `ArgumentException`); and
  `Verdicts.IsMatch`, which already admits `InvalidOperationException` only where the offset entry met the frame or
  trail ceiling, stays as it is, the exception being admitted.
- Task 2.8's record: the shape of `Compile`'s, the constructors', `RegexParser.Parse`'s and `RegexFlagsParser.Parse`'s
  syntax error with C3's row: settled, the row gone; the marks of the validate entry, `RegexDiagnostic`,
  `RegexDiagnosticCode`, `RegexStatus.SyntaxError` and `RegexSyntaxException.Diagnostic`: removed, the last internal
  with its type; `RegexFlagsStatus`: folded; `CharSet.CheckRange`'s SL4 place and its missing diagnostic: the type is
  internal, the message culture-free, and no public member reaches it, so it carries no diagnostic; the oracle's
  comparison of the two entries: it reads `Compile`'s diagnostic.
- Carried, with their owners: SJL-11.4 and SJL-11.5, each engine's adapter onto this surface (plan rows 5.2 and 6.1),
  Broiler.JS's among them (it reads `RegexParser.Parse`, the tree, `RegexFlagsParser.Parse` and `Groups.Count` today,
  below); WP4 (tasks 4.1 and 4.2), the candidate reading at the commit `0.1.0-preview.6` is packed from and the owner's
  publication after it. Nothing of task 2.9's own is carried.

**The breaks.** Twenty-nine suppressions, 22 CP0001 and 7 CP0002, each listed with its R item in
[`suppressions.md`](suppressions.md), section "R10 and R8: the surface of `0.1.0-preview.6`", and in the CHANGELOG
under **Changed**. A pack with the suppression file emptied fails with exactly those 29 errors against
`0.1.0-preview.5`, and one that regenerates the file writes it unchanged.

**The checks, at `c03abd8` with this record's files.** The Release build of the solution: no warning, no error. The
tests: 2,581 library, 49 architecture (the starting violation list empty) and 11 fuzz, all passing. The declared fuzz
session (seed 20261027, 24,000 iterations): no finding, every one of 122 construct classes reached, digest
`f8477deef307ca48c0bea72dcdc9a44a4197ee09665f9a52599ae4310227dad1` (moved once, at `b6211b4`, by the constructor's
refusal recorded as `syntax`). The probe's default report is byte for byte the one at `cfb541e`; `--adversarial`: 65
rows, 65 give their declared status, none differs, ends abnormally or waits, and the report is the same file under the
JIT and Native AOT (SHA-256 `3f7fc5387c9015ebe26436f0e40334755a05a4e214d33d6b68f558e0ea70dc75`, the one task 2.14
recorded), as is the default report. `dotnet pack` gives `Broiler.Regex.0.1.0-preview.6.nupkg`, depending on
`Broiler.Unicode.Algorithms` `0.1.0-preview.6`, with package validation against `0.1.0-preview.5` passing on the listed
suppressions only. The assurance check: no violation. The feed check's consumer (`eng/verify-feed.ps1`, reproduced in
bash) restores the packed package and `Broiler.Unicode.Tables` `0.1.0-preview.6` and passes, unchanged: it calls the
constructor, `Match` and `IsMatch`, which this task keeps. The resolver's tests: 10 of 10.

*Dated note, 2026-11-01 (the review of task 2.9).* The review's findings and their fixes, each in its own commit after
this record's: the constructors refused a pattern past `MaximumNestingLimit` with "The pattern nests deeper than the
library's maximum nesting limit" where `Compile` and the validate entry answer "... than the caller's nesting limit",
so their documentation's promise of the validate entry's message was false; every nesting refusal now carries one
message, "The pattern nests deeper than the nesting limit", a test compares them, and the fuzz oracle compares the
constructor's nesting refusal too (with the old message restored, the declared session reports it at iterations 40,
6034 and others). `Compile`, the validate entry and the constructor over a `RegexFlags` value compiled
`Unicode | UnicodeSets` as `v`; they refuse it as ParsePattern does, `SyntaxError` with `UnicodeWithUnicodeSets` at -1
before a unit is charged, and refuse a bit `RegexFlags` does not define with an `ArgumentOutOfRangeException`, as
`RegexFlagsText.Format` did (CHANGELOG, Observable changes). `RegexFlagsParser`'s summary names what it now offers. The
records: the `IsMatch` item's reason (corrected above), the differs list's reading of row 2.9 (below), the filed rule's
note on E-4 and the pins, the task list of `24344b3`, the CHANGELOG's preamble and the reference of the version note.
The checks at the review's fixes: the Release build, no warning; 2,582 library tests, 49 architecture and 11 fuzz, all
passing; the declared fuzz session, no finding, digest `c2ed95c98feb225ec1df3eccf378d9f4804aab02f410d48299be02891e4c5d5b`
with four worker processes and with three under `tr-TR` (moved by the `u`-with-`v` refusal: each validate policy answers
`syntax` 680 times more); the probe's default report and its `--adversarial` report unchanged and the same under the
JIT and Native AOT (`8f40ae86…` and `3f7fc538…`), 65 rows at their declared status; `dotnet pack` gives
`0.1.0-preview.6`, depending on `Broiler.Unicode.Algorithms` `0.1.0-preview.6`, package validation against
`0.1.0-preview.5` passing with the same 29 suppressions; the assurance check, no violation; the feed check's consumer,
unchanged, passes against the packed package.

**Where the plan, JSD-0081 and this repository differ, read at this record.**
- JSD-0081's D12 and the plan's row 2.9 ship the surface as `0.2.0-preview.1`, validated against `0.1.0-preview.4`; it
  ships as `0.1.0-preview.6`, validated against `0.1.0-preview.5`, by the owner's decision. JSD-0081's D12 and SJL-D-3's
  reading ("inside a `0.y` minor version") are Broiler.VM's records and are not revised here; convention 16 carries a
  dated revision. The filed rule of bundle `sjl-11-001` names `0.2.0-preview.1` for the candidate reading and carries
  a dated note; its check E-4 reads the baseline as `0.1.0-preview.4`, which is now `0.1.0-preview.5`.
- D12 says "Broiler.JS moves its pin only at SJL-11.5". Broiler.JS's `main` pins `Broiler.Regex` `0.1.0-preview.5`
  since the owner's dependency update of 2026-10-29 (`a58d31d1`). It reads `RegexFlagsParser.Parse`,
  `RegexParser.Parse`, the tree (`GapScan`), `Groups.Count` and `GroupNames` (`JSRegExp.Broiler.cs`), so it cannot take
  `0.1.0-preview.6` without SJL-11.5's adapter; no release depends on `0.1.0-preview.6`.
- Both engines' and this repository's pins of `Broiler.Unicode.Algorithms` are `0.1.0-preview.6` since the owner's
  dependency update of 2026-10-30 (`77284f1`); R11's reading in this section, its README and check A-10 name
  `0.1.0-preview.5`. *Added 2026-11-01 (the review of task 2.9):* the filed rule's dated note now reads its pins line,
  A-10's "version above" and E-4's baseline as `0.1.0-preview.6`, `0.1.0-preview.6` and `0.1.0-preview.5`.
- Decision D5 says "the parser, the public tree, the `CodePointSet` algebra and the flag parser stay": they stay as the
  library's internal model; the tree is no longer public.
- *Added 2026-11-01 (the review of task 2.9).* The plan's row 2.9 says the 0.1 surface is "removed where R11 and R8 force
  it, kept where they allow (the string-bearing `RegexMatch`)", and D12 keeps "the string-bearing RegexMatch (R8
  allows)". This task also removes or reshapes members neither R11 nor R8 forces: the public tree, `RegexParser`,
  `UnicodeCharSets`, `BroilerRegex.Matches`, `RegexMatch.NamedGroups` and the type of `RegexMatch.Groups`, each by R10's
  conventions (convention 1 and check C1 above all) and by row 2.9's own check, "the violation list is empty"; 25 of the
  29 suppressions cite R10, two R9 and two R8. Row 2.9's "removed where R11 and R8 force it" is read with R10's
  conventions and that check forcing removals too. D12's string-bearing `RegexMatch` is kept as a type, with `Groups`
  reshaped to a span and `NamedGroups` and `Matches` replaced by `GroupNamed` and `NextMatch`.
- The brief of this task names the breaks "against `0.1.0-preview.4`" in one place and the baseline `0.1.0-preview.5` in
  another; the owner's decision, the binding one, reads `0.1.0-preview.5`. It says `0.1.0-preview.5` carries "tasks 2.4c
  to 2.11"; `24344b3` carries tasks 2.1 to 2.7 and 2.11, and not 2.8, 2.14 or 2.9. It names `eng/Broiler.Packaging.props`
  as where the version is set; the vendored file says not to edit it, and the floor is set in `Directory.Build.props`.
- The recommendation kept the constructors' `RegexSyntaxException`; C3 and the empty list rule it out (above).

*Dated record, 2026-11-02 (task 2.13 of SJL-11.2; bundle `sjl-11-001`'s [`reference-arms.md`](evidence/sjl-11-001/reference-arms.md),
template T4; decisions D24 and D25 of Broiler.VM's JSD-0081; the fast-path differential over P4 that the records of
tasks 2.4c and 2.11 above leave to this task).*

**What it builds.** Everything the filed rule names but the judged run, which **waits for corpus revision 32** (below).
In order of the branch's commits (`sjl-11-2-13-arms`, from `6ceb0df`):
- *The population's generator* (`28d3e36`): [`scripts/reference-arms/`](../scripts/reference-arms/README.md)'s
  `generate_population.py` writes P1 to P7 as the rule states them, P1 from a vectors directory given as an argument,
  P2 from test262 at `ccaac100` by a declared grammar and a declared cap of 4,096 (pattern, subject) pairs per file,
  P3 from the corpus rows declared to complete under an ample policy, P4's 2^16 draws from seed `0x534A4C31312D3031`
  under the rule's ten flag sets with 16 subjects each, P5 and P7 over every code point, P6 from Broiler.Unicode's U2.
  SplitMix64 is Broiler.Unicode's, copied with its blob named; `tests/test_splitmix64.py` checks it against the
  published reference outputs (seed 1234567, the first five), and the draw procedure is written in the README.
- *The arms* (`dc817a9`): [`tests/ReferenceArms/`](../tests/ReferenceArms/README.md), each arm its own executable, none
  in the solution: S through the public API; V the copy of Broiler.VM's `JsRegExpMatcher.cs` at `aaa39fb`, every byte
  after its header the blob's, with `JsFormatUnicode` re-pointed to `Broiler.Unicode.Tables` and the realm's calls
  (`RegExpCompile`, the matcher's start in `RegExpMatchOne`, `RegExpRun`) copied around a stand-in engine that records
  every charge; J the copy of Broiler.JS's `CreateRegex` with its whole translator, `TryRouteToBroiler`, `GapScan`,
  `RunMatch`, the three prechecks and `JSUnicode` at `1f91b8f1`, over `Broiler.Regex` `0.1.0-preview.4` from nuget.org
  and .NET's `Regex`. Each copied file names its source path, its blob, its members and every change, each change also
  marked `ARM CHANGE` where it is made. `THIRD_PARTY_NOTICES.md` names the copies.
- *The comparison and the classifier* (`e698bf9`, with two fixes the first run showed, `64c5a95` and `905f35a`, each
  with a test): `compare.py` writes `summary.json` and `differences.tsv.gz` and classifies nothing; `classify.py`, written
  before any judged run, reads the correspondence's rows (it refuses to run when a row it names is missing or does not
  name the engine it corrects) and asks `regexpref.py` from the Broiler.JS commit given as an argument, by
  `git archive`, with a cut-off of 1,000,000 Matcher invocations; anything no row names is class (c). `run.sh` runs every
  step in order.
- *The fast-path differential* (`86c1377`): `tests/ReferenceArms/FastPaths/`.
- *The arbiter's answers kept between classifications, and the wall clock raised* (`67dc4b5`), after the run (below).
- *The records*, with this one: the bundle's README (a dated note), the CHANGELOG (process only).

**The negative controls**, each a scratch copy of an arm whose `Program.cs` sets the harness's control case and changes
nothing else, each run once and then removed: V's copy over P4, its control case `P4-01234-u-05`, and J's over P1,
`P1-regexp:matching:m1-notes:0001`. The comparison reported each control case as a difference; J's was classified, and
the classifier listed it **unclassified** (`the negative control's planted difference`), the only unclassified one of
P1's 122 differences; V's comparison was not classified, and its classification was not shown (see the dated note below,
which replaces the claim that it was). Against the uncontrolled V run, V's copy differed in two lines: the planted one,
and `P4-14530-d-05`, `limit:steps` in the run and `limit:wall-clock` in the control, a catastrophic case that ran for
10,000,400 units and took the copy, run on one worker of a machine with a load average near 30, past the harness's
2 seconds of wall-clock time. **The wall-clock limit is a limit of the run, not of the arm**, and under load it moves
a case from one limit kind to another (README.md, "Workers"); a limit kind on either side is class (b), so no class
moved, but the listing is not regenerated byte for byte where it reaches the wall clock. The run used 2 seconds, and 0.5
seconds for the later cases of a pattern already stopped once (so that a pattern arm J's .NET engine never ends costs
seconds and not hours); after it the limits were raised to 10 seconds and 1 second (`67dc4b5`), and the judged run is
to be taken on an idle machine.

*Dated note, 2026-11-02 (the review of task 2.13).* This paragraph read "The comparison reported each control case as a
difference and the classifier listed each **unclassified**": for V the run's `controls/cmp-V/` held the comparison
alone (`differences.tsv.gz` and `summary.json`), so only J's listing was shown. And the planted change was not a
changed result: the harness appended "`#negative-control`" to the answer, which the classifier's canonical form cut
off, so `P4-01234-u-05` read `match:0,0` against `match:0,0#negative-control`. The harness now changes the answer
itself (`40940cd`, `Harness.Planted`), the classifier strips nothing (`a05f89d`), and **both controls were taken
again**, once each, at `a05f89d` on 2026-11-02, each a scratch worktree of that commit whose `Program.cs` set the control
case and nothing else, and then removed (`git worktree remove`): the population P1 and P4 written by the generator at
that commit (P4's draws as in the run, V's 138 acceptance and 5,424 exec differences of P4 again), S over both, V and
its copy over P4, J and its copy over P1, each compared with S and classified by the classifier `@2`. V's copy
differed from the uncontrolled V in one line, `P4-01234-u-05`, `match:0,1` for the matcher's `match:0,0` (no case
reached the wall clock this time); the comparison held 5,563 differences (SHA-256 `122f428b…`) against the
uncontrolled 5,562, and the classifier listed the planted one **unclassified** and every other as in the uncontrolled
run (a 1,632, b 3,182, c 748). J's copy differed in one line, `P1-regexp:matching:m1-notes:0001`, `match:0,2` for
`match:0,1`; 122 differences (SHA-256 `17348349…`) against 121, the planted one **unclassified**, the rest as
uncontrolled (a 105, a' 3, b 5, c 8). Both classifications were kept for the record outside the repository
(`classified.tsv.gz` SHA-256 `309e581d…` for V, `4488065a…` for J), as the run's files are.

**The unjudged run** (2026-11-01 to 2026-11-02; the arms built at `86c1377`, the population written by the generator
as `28d3e36` holds it, run just before that commit, so that its manifest names the base `6ceb0df`; a second run, at
`86c1377`, wrote every class's file byte for byte the same). P1 was read from
Broiler.JS's branch `sjl-11-3-regexp`'s working tree, whose vector files are those of `cfe32852` (corpus revision 32 on
that branch, not on `main`), and the arbiter taken from `cfe32852`; test262 is the
copy at `SCRATCH/sjl12/wp7-records/review-vm-judgement/suite/`, whose `test/` tree is `c022917`, the tree of tc39
`ccaac100`. The population: P1 685 acceptance and 638 exec cases, P2 5,928 and 67,514, P3 23 execs, P4 655,360 and
10,485,760, P5 6,684,672 and 6,703,218, P6 306,424 acceptance cases, P7 8,912,896. S and V each answered every case
once, J three times (invariant, `tr-TR`, `lt-LT`). Arm V differs from S on no case of P5, P6 and P7.

The comparison wrote 1,901,120 differences (`differences.tsv.gz`, SHA-256 `23aba8d0…`) over the five arm runs, and the
classifier, with the arbiter at `cfe32852` (`regexpref.py` blob `d031add5`, the same file as at `d8cb8406`) and a
cut-off of 1,000,000 Matcher invocations, classed them (the rule's classes; a row is the correspondence's; D10 is
decision D10's no-meter policy of Broiler.JS; **the classifier `@1`'s classification, superseded by the review's
below**):
- **(a)**, 1,257,780. J under the invariant culture: S8 334,935 (the `v` classes), M2 41,564 (case-insensitive matching
  without `u`), S2a 23,134 (escaped group names), S7 5,959 (the quantified look-behind without `u`, and `u` mode's
  refusals), M3 3,177 (line terminators), S1 2,579, B2 2,124 (Broiler.Regex `0.1.0-preview.4`'s budget and recursion),
  M4 5; under `tr-TR` and `lt-LT` the same rows within a few dozen each, M2's differences read as M2a there (51,009 and
  47,425, the culture's casing). V: S1 1,905 (duplicate named groups, the escaped spelling among them), S10 218, M4 7,
  M7 4, S5 1;
- **(a')**, 45: J under U3 in each culture, case pairs Unicode 16.0 and 17.0 added, which .NET's data lacks;
- **(b)**, 17,152: one side or both stopped by its own bound, the other side the arbiter's answer or the arbiter past
  its cut-off: V under B6 3,182 and B7 13; J under D10 4,313, 4,787 and 4,842 (its .NET timeout and the harness's wall
  clock), under S3 4 in each culture (the nesting limit), and one difference under M4's empty match in each (decision
  D41);
- **(c)**, 626,143, of three kinds, each investigated:
  1. **One defect of S**, 718 differences (P4 714, P1 4), each against arm V, whose answer is the arbiter's: **`\k` in
     a class, without `u`, in a pattern that declares a group name**, `[\k<a>](?<a>x)`, `[\k](?<b>x)`, which S refuses
     with `NamedReferenceInClass` (`Broiler.Regex/Parsing/RegexParser.cs`, the `\k` test of the class escape) and the
     arbiter accepts as the identity escape `k`. B.1.2's `ClassAtomNoDash[UnicodeMode, NamedCaptureGroups]` reads
     `ClassEscape[?UnicodeMode, ?NamedCaptureGroups]`, but no production passes `NamedCaptureGroups` into a class (the
     main grammar's `ClassAtom[UnicodeMode]` reads `ClassAtomNoDash[?UnicodeMode]`), so the parameter is absent there and
     `IdentityEscape[~NamedCaptureGroups]` admits `k`. Seven P4 patterns hold it, each under the six flag sets without
     `u` or `v`; P1's are the vectors `literals/s1-s5-names/0001`, `syntax/s5-named-references/0012` and `/0014` and
     `matching/s4-escapes/0031`. J agrees with S on these (its group-name precheck refuses them), so the difference is
     V's alone. **Not fixed here**: the fix is in the library's parser and its tests, outside the paths this task
     changes, and the arbiter that decides it is on a branch that has not merged (its review commit `3ea693c7` made
     this reading); task 2.8's record chose the other reading. It is to be fixed before the candidate reading, after
     which the whole comparison runs again (the rule's class (c)).
  2. **A defect of Broiler.VM no row of the correspondence names**, 22 differences of arm V, four patterns, every one a
     set difference of `\w` under `iv`: `[\w--I]` (V matches `I`, which the subtraction removes under case folding),
     `[\w--\ud83d--\S]?`, `[\w--[^]--K]\B|😀`, `𝐀+?[[\w--[\x6b\D\s]]\r]*`. The arm copies the matcher byte for byte and
     changes only where its tables come from (the same UCD 17.0.0), so the answer is the matcher's: not a defect of the
     arm. By the rule it is class (c), and **a difference the correspondence does not name falsifies JSD-0081**:
     Broiler.VM's record is to be revised (row S8's Broiler.VM cell reads "the specification's answers") before either
     cut-over.
  3. **Defects of Broiler.JS no row names**, 208,460 differences under the invariant culture (208,482 and 208,461 under
     `tr-TR` and `lt-LT`), nearly all in P4 without `u`: Annex B patterns the arbiter and S accept and J refuses (a lone
     `]`, `{` or `}`, `\p` without `u`, `\c` in a class, octal escapes and `\8`, `--` and `&&` in a class without `v`,
     `\u{` without `u`), 7,002 acceptance and 110,569 exec differences; patterns the grammar refuses and J accepts
     (quantified assertions such as `µ??\b{1}`), 935 and 73,943; 16,010 matches J answers otherwise; and one exec in
     which J's .NET engine throws `ArgumentOutOfRangeException` (`P4-64098-y-12`, `(?<$x>){1,}?\B|(?!K)` under `y` from
     lastIndex 2 of `X\`). Row S7 names Broiler.JS's quantified look-behind only, and no row names the rest: they too
     falsify JSD-0081's correspondence for Broiler.JS, whose rows are to be added (each a correction of Broiler.JS when
     SJL-11.5 moves it onto the library) before its cut-over.
  No difference is a defect of an arm. Two the first runs showed were the classifier's (row S1 read names as written,
  and row S7 under `u` only), fixed in it before this classification and recorded above.

*Dated note, 2026-11-02 (the review of task 2.13; the classification above is the first, by classifier `@1`, and is
superseded here).* The review found that most of arm J's predicates read only a feature of the input, a flag or a
character, so that a defect of Broiler.JS no row names was class (a) whenever the input had that feature: the record
above read "(a), 1,257,780", "S8 334,935 (the `v` classes), M2 41,564 (case-insensitive matching without `u`)", and its
(c) item 3, "208,460 differences under the invariant culture", was too low. It found too that rows M3, M7, S10 and M2
for Broiler.JS missed vectors of their own (P1's `matching/m3-end-of-input/0001`, `/0004`, `/0007`, `/0008`,
`m7-backreference-surrogates/0009`, `s10-lead-surrogate-escapes/0011` and `modifiers/0009`, which item 3 counted as
named by no row), and that class (b) held "the other side the arbiter's answer or the arbiter past its cut-off", which
the rule's arbiter clause does not admit ("a difference the arbiter cannot reach in its step cut-off is class (c) until
reduced to an input it can"). The classifier `@2` (`a05f89d`) reads each row's own correction (the direction of the
difference and the row's construct, the arbiter's reason for a refusal where the row names one; `scripts/reference-arms/`'s
README lists them), reads no row S1 or S2b for arm J, reads rows M2b, S10 and M7 for it, and leaves a stop against an
answer the arbiter cannot check in class (c). The same 1,901,120 differences, classed again with the arbiter at
`cfe32852` and the correspondence of Broiler.VM's `5e6531b8` (`classified.tsv.gz` SHA-256 `2c43fc14…`), are:
- **(a)**, 373,571. J under the invariant culture, 118,705: S7 77,413 (without `u` or `v`, a quantified look-behind J
  accepts, and every exec of that pattern), S8 36,707 (under `v`, a class syntax character or reserved double
  punctuator J accepts), M2 2,289 (a case pair .NET holds and Canonicalize does not, U+212A, U+017F, U+2126 among
  them), S2a 1,620, M3 557, M2b 111, M4 4, S10 3 (`[\uD83D\u{DC38}]`), M7 1 (`(?<=\1(.))b` under `u`); under `tr-TR`,
  127,920, and `lt-LT`, 124,811, the same rows (S8 36,690 in each) with M2 2,365 and 2,255 and M2a 9,156 and 6,157. V,
  2,135: S1 1,905, S10 218, M4 7, M7 5. Row B2 holds none: its `@1` predicate held 2,122 J answers to patterns S
  refuses;
- **(a')**, 45: J under U3, 15 in each culture;
- **(b)**, 11,052: V under B6 3,182; J under D10 2,270, 2,754 and 2,831, under S3 4 in each culture and row M4's empty
  match 1 in each (decision D41). V's 13 under B7 and J's 2,043, 2,033 and 2,011 under D10 that read the arbiter past its
  cut-off are now (c);
- **(c)**, 1,516,452: V 753, J 505,275 under the invariant culture, 505,461 under `tr-TR` and 504,963 under `lt-LT`.
  1. S's defect of item 1 above, unchanged: 718 against V.
  2. Broiler.VM's `\w` under `iv`, item 2 above, unchanged: 22.
  3. **Inputs the arbiter cannot reach in its cut-off** against a stop: V 13 (P4 12 and P1's
     `budget/k-catastrophic/0001`, `(a+)+b`, V at its allowance), J 2,043 under the invariant culture (P4 2,042: S at
     its step or trail ceiling against J's null, 1,661, match, 359, or refusal, 2, and J at the wall clock against S's
     answer, 20; and P3's `O06`, `^[a]*$` under `u` over 10,000,001 `a`, S's match against J's wall clock), and under
     `tr-TR` and `lt-LT` also P1's `budget/k-catastrophic/0002`, which J under the invariant culture answered as S did
     and under load reached the wall clock. Each is class (c) until reduced to an
     input the arbiter can reach, which the judged run's classification must do or record.
  4. **Defects of Broiler.JS no row names**, under the invariant culture: patterns S and the arbiter refuse and J
     accepts, 19,873 acceptance and 317,750 exec differences, by the arbiter's reason nothing to repeat 143,098 (lone
     braces under `v`, `^*`, `$+`), an invalid escape 59,986 (`\-` outside a class under `v`), a decimal escape past
     the group count 55,379, a lone syntax character 25,045, an invalid class escape 23,611, a class range out of order
     12,512 and 5,610 (with `v`), an undefined group name 6,715, a braced quantifier after an assertion 3,774, a trailing
     backslash 1,649, and 244 others (`[a-z&&b]` under `v`, an unterminated class, a negated class with strings,
     `(?x:a)`); patterns S and the arbiter accept and J refuses (item 3 above's Annex B list), 8,620 and 137,918; execs
     both answer and J answers otherwise, 19,070; and the one exec J's .NET engine throws on (`P4-64098-y-12`). By
     population: P1 8, P2 3, P3 1, P4 505,255, P5 8.
  **Each of P1's, P2's, P3's and P5's differences of arm J in (c), investigated** (an id names the vector):
  `last-index/m4-surrogate-pairs/0013`, `(.)` under `gdu` from lastIndex 1 of U+1D306: S and the arbiter match at
  index 1 with the capture over the whole pair, J's capture is the trail alone, an answer row M4 does not record (it
  reads J's non-empty matches there as the specification's); `/0015`, `[𝌆]` under `gu` from the same
  lastIndex: S and the arbiter match the pair, J starts at the lone trail and answers null, which row M4's half-pair
  matches (J matching where the specification fails) are not; `matching/v-mode/0027` and `syntax/s8-unicode-sets/0024`,
  `[a-z&&[^aeiou]]` and `[a-z&&b]` under `v`, which the arbiter refuses (a range is no operand of `&&`) and J accepts,
  where row S8 reads such mixtures refused (S11); `syntax/s7-grammar-and-annex-b/0024` to `/0026`, `^*`, `$+`, `\b*`
  without flags, and `syntax/modifiers/0037`, `(?x:a)`, which J accepts and row S7 (every refusal early but `(?<=a)*`)
  and row F3 (the constructor the same in both) do not; P2's three `language/literals/regexp/early-err-modifiers-should-not-unicode-escape-{i,m,s}.js`,
  `(?i:a)` and its two siblings under `u`, which J accepts; P3's `O06` (item 3); P5's `]`, `[]]`, `{` and `}` under
  `iv` (acceptance and exec), which J accepts; and under `tr-TR` only, P5's `I` and `[I]` on `i` and `i` and `[i]` on
  `I` under `i`, which J answers null (the Turkish casing pairs `I` with U+0131 and `i` with U+0130): row M2a's
  mechanism, but not a pair the row records, so (c). None is a defect of the arm: each is the copied code's answer
  (the arm changes no line of the translator, the routing or the prechecks but those its headers record), and each
  falsifies JSD-0081's correspondence for Broiler.JS, whose rows are to be added before its cut-over, as item 3 above
  said of the rest. S8's 334,935 of `@1` are now 36,707: the rest are J's acceptances under `v` with another reason,
  in item 4.

**The fast-path differential over P4** (the check tasks 2.4c and 2.11 left to this task): every P4 pattern compiled twice,
with the program compiler's fast paths and without them, under `FastPathDifferentialTests`' ceilings. 473,804
pattern-flag pairs compiled (181,556 refused under their flags); 7,578,425 exec cases compared; 4,880 stopped at the
first ceiling (4,415 both programs, 465 the general lowering alone), of which 2,441 compared under the second and 2,439
stopped there too, not compared; **no disagreement**. Unlike the test suite's populations, 1,660 P4 cases reach a frame or
trail ceiling, and both programs answer them alike, the ceiling included. The check is done; the record of task 2.4c's
"the check runs over P4 when it lands" is met.

**The checks, at `86c1377` with this record's files.** The Release build of the solution: no warning, no error; the
tests: 2,582 library, 49 architecture (the starting violation list empty) and 11 fuzz, all passing; the arms build in
Release with no warning and run; the generator's, the comparison's and the classifier's tests pass; the probe's default
report and its `--adversarial` report under the JIT are the ones task 2.9 recorded (`3f7fc538…` for the latter; no
library or probe source changed, so the Native AOT image was not published again); the assurance check: no violation;
`dotnet pack` of the library gives `Broiler.Regex.0.1.0-preview.6.nupkg`, whose files are the library's assembly, its
documentation, the nuspec, the README, the notices and the icon, and no arm. The architecture tests read no arm as a
project (they read the direct children of `tests/`), and rule SL10 reads every file of the tree: the arms' files are
LF, and `run.sh` keeps Python from writing `__pycache__/` files, whose bytes hold a CR.

**What the judged run waits for.** Corpus revision 32 on Broiler.JS's `main`, which SJL-11.3's task 3.3 produces: then P1
is that revision's vectors, the arbiter `regexpref.py` at that commit (the bundle names it), the population is generated
at the commit the bundle names, the run is taken on an idle machine with the wall-clock limit raised, and the rule's
"MET if and only if" is read. Before it: S's `\k` defect is fixed and Broiler.VM's and Broiler.JS's correspondence rows
for the unlisted differences are filed (for Broiler.JS, the review's item 4 above: its acceptances and refusals by the
arbiter's reason, and its matches), and an input the arbiter cannot reach is reduced to one it can or stays class
(c) (the review's item 3); `run-reference-arms.sh` in the bundle wraps `run.sh` (WP4, task 4.1).

*Dated note, 2026-11-02 (task 2.13's second review: the base merged, three problems of the run fixed, the run taken
twice).* The review's verifier found the records above out of date and the run not reproducible. What changed:

- **The base moved, and S's `\k` defect is fixed.** `origin/main` became `306d204` (task 2.12, Broiler.Regex#17), which
  is merged into this branch (`33b468a`) with no conflict. Task 2.12's `2e35cbb` reads `\k` in a class, without `u` and
  `v`, beside a group name, as the letter (Broiler.VM's decision D-s5) and retires `NamedReferenceInClass`. The (c)
  item 1 above read "**Not fixed here**: the fix is in the library's parser and its tests, outside the paths this task
  changes, and the arbiter that decides it is on a branch that has not merged (its review commit `3ea693c7` made this
  reading); task 2.8's record chose the other reading. It is to be fixed before the candidate reading, after which the
  whole comparison runs again", and the review's note "S's defect of item 1 above, unchanged: 718 against V": the
  defect is fixed by task 2.12, not here, and in the run below S accepts and matches those inputs as arm V and the
  arbiter do, so that no difference with V remains on them. Arm J refuses them, which Broiler.VM's correspondence
  records as a correction of Broiler.JS since its `5d025cc` (row S5, D-s5); the classifier `@3` reads it so.
- **The judged run no longer waits for corpus revision 32.** "What the judged run waits for" above read "Corpus revision
  32 on Broiler.JS's `main`, which SJL-11.3's task 3.3 produces: then P1 is that revision's vectors, the arbiter
  `regexpref.py` at that commit (the bundle names it) [...] Before it: S's `\k` defect is fixed and [...]". Revision 32
  is on Broiler.JS's `main` since `b2c2e224` (Broiler.JS#1089), of which `cfe32852` is an ancestor; `regexpref.py` there
  is the same blob, `d031add5`, and `tests/Vectors/regexp/` (task 2.12) is a byte-for-byte copy of its vectors; and S's
  `\k` defect is fixed. The judged run waits now for the rest of that paragraph: the correspondence's rows for the
  differences class (c) still holds (below), each input the arbiter cannot reach reduced or recorded, the bundle naming
  its commits, and `run-reference-arms.sh` (WP4, task 4.1) at the commit `0.1.0-preview.6` is packed from.
- **`run.sh` hid a classifier crash.** Python's exit for an uncaught exception is 1, the classifier's "a class (c)
  difference", and `run.sh` printed "done" and exited 0 with no `classification.json` when the arbiter's checkout was
  gone. `classify.py` now exits 0, 1, 2 (refused, or a bad argument) or 3 (failed) and writes `classification.json`
  last; `run.sh` fails on 2, on 3 and on a run that leaves no file (`6d1757c`), and `test_run_sh.py` runs each path.
- **Row B8 was never read.** P3-F02 (`\B` 100,000 times under `u`) was (c) where arm J crashed (`crash:exit-134`,
  under `tr-TR`) and (b) under D10 where the wall clock stopped it first. The classifier `@3` reads row B8 for arm J
  (`6d1757c`): the harness's stop of J on a sequence of 10,000 terms or more under `u` or `v`, against S's answer when
  it is the arbiter's, is class (a), whichever stop it was.
- **The run was not reproducible.** Every row the verifier's run changed against the record's was an arm J case where
  one side reached the wall clock, J's .NET timeout, or crashed. First (`10b1c59`, `3b65595`, `81bd650`): every bound
  of a run is in `tests/ReferenceArms/Common/Limits.cs` and each run file records them; every stop of the harness, a
  crash among them, is the one answer `limit:harness`; arm J's .NET `Regex` has no match timeout, as Broiler.JS builds
  it; the workers are fixed, not the machine's processor count; the run's times and which stop each case met go to
  `runs/timing/`, which nothing compares; and the population's manifest names its inputs by their bytes and commits,
  not their paths. Two full runs at `81bd650`, the limit 5 seconds of processor time per case, showed that this is not
  enough: arm J ends some cases after about that much processor time, and `P4-08462-v-12` ended in one and was stopped
  at 5.07 seconds in a probe an hour later on the same machine, so any limit of time leaves a case on its edge. The
  two runs were stopped in arm J. Then (`fab57fa`): no limit of time decides an answer of a run. A **calibration**
  (`run.sh OUT --calibrate`) runs the arms under the time limits and writes the cases they stop as a **stop list**,
  which the repository keeps (`tests/ReferenceArms/stops.tsv`, `a113c91`); every other run answers a listed case
  `limit:harness` without running it and runs every other case to its end, under backstops of 60 seconds of processor
  time (twelve times the calibration's limit) and 900 of wall-clock time, and fails as not reproducible if a case the
  list does not hold reaches one.

**The calibration** (2026-11-02, 11:53 to 15:44 UTC, at `fab57fa`, on the population below): S and V stopped no case.
Arm J stopped 1,930 under the invariant culture, 1,928 under `tr-TR` and 1,930 under `lt-LT`: in P4, 168 or 169 at the
5-second limit and the rest, 1,758 to 1,762, after a stop of the same pattern in the range; and P3-F02 under the first
two, whose stack overflowed (`exit-134`) after 9 to 10 seconds of processor time, while under `lt-LT` the arm answered
it. The machine was shared with other work (load averages between 5 and 25 on its four processors). The list is
`tests/ReferenceArms/stops.tsv` (`a113c91`, SHA-256 `d427109d…`, 5,788 cases). The same commit turns the workers'
tiered compilation off (`DOTNET_TieredCompilation=0`): with it on, how deep 0.1.0-preview.4's recursion goes before
the stack overflows depends on when the background compiler replaces a method's unoptimized frames, which is why
P3-F02 overflowed in two cultures and not the third; with it off the arm answers P3-F02 in all three, and the list,
made before, still holds it for two, where the runs answer it `limit:harness` (row B8, class (a)).

**The run, taken twice** at `a113c91`: P1 from Broiler.JS's `b2c2e224` (`conformance/vectors/regexp/`, the same bytes
as `tests/Vectors/regexp/`), test262 at `ccaac100` (`test/` tree `c022917`), the arbiter `regexpref.py` from
`b2c2e224` (blob `d031add5`) with its cut-off of 1,000,000 Matcher invocations, Broiler.VM's correspondence as its
`main` (`292ae6b`) holds it (blob `d7323aec`, last changed in `5d025cc`), and the classifier `@3`. Two worktrees of
`a113c91` each ran `run.sh` whole, from the build to the fast-path differential, at the same time on a machine shared
with other work. Both ended with exit 0, no case reached a backstop, and **the two runs' listings are byte for byte
the same**: `differences.tsv.gz` SHA-256 `0a8345fd6275347eeaaae432d95aacfdc3c946a4271dca72c9be576f6e5ab677` and
`summary.json` SHA-256 `f0147d1b429b19d5705f2482639e4cb3353a111f242bce491cde3b96a3dbc1d5` in each, and so are the
population (manifest `2366b090…`, the same class files the calibration ran), every arm's answer file, the
classification (`classification.json` `4f6ebdb4…`, `classified.tsv.gz` `a03b9973…`) and the fast-path differential's
summary (`3cb4bb79…`). `differences.tsv.gz` holds cases and answers alone; `summary.json` also names the commit, by
the population's manifest and by S's informational version, so a run at another commit with the same code changes it
there and nowhere else. The comparison wrote **1,901,509 differences**: V 5,454; J 626,895 under the invariant
culture, 636,411 under `tr-TR` and 632,749 under `lt-LT`; by class P1 465, P2 556, P3 7, P4 1,899,989, P5 492, P6 and
P7 none.

**The classification** (classifier `@3`), which **supersedes** the review's classification above (`@2`: "(a)
373,571 [...] (a'), 45 [...] (b), 11,052 [...] (c), 1,516,452"):
- **(a)**, 376,172. J under the invariant culture 119,527, under `tr-TR` 128,759, under `lt-LT` 125,649, by row over
  the three: S7 232,239, S8 110,121, M2a 15,313, M2 6,909, S2a 4,860, **S5 2,460** (J's refusal of `\k` in a class,
  decision D-s5: new, since S no longer refuses it), M3 1,671, M2b 333, M4 12, S10 9, **F3 3** (`(?x:a)`), M7 3, **B8 2**
  (P3-F02 under the invariant culture and `tr-TR`). V, 2,237: S1 2,007, S10 218, M4 7, M7 5;
- **(a')**, 45: J under U3, 15 in each culture;
- **(b)**, 8,983: V under B6 3,182; J under D10 5,786 (1,929, 1,927 and 1,930), every one S at its own ceiling (trail
  3,023, steps 1,793, frames 945, a refusal 25) and J at the harness's stop the list holds; S3 4 and row M4's empty
  match 1 in each culture;
- **(c)**, 1,516,309, of which **none is a defect of S**:
  1. Broiler.VM's `\w` under `iv`, 22, unchanged (the review's item 2);
  2. inputs the arbiter cannot reach in its cut-off against a stop, V 13 and J 6,446;
  3. Broiler.JS's differences no row names, 1,509,828 (P1 21, P2 9, P4 1,509,770, P5 28), the review's item 4 and its
     list of P1, P2 and P5 vectors less those rows F3 and S5 now hold.
  The 718 differences of S's `\k` defect against V are gone: S, V and the arbiter agree on those inputs.

**The negative controls, taken again** at `a113c91` with the stop list, once each, from a scratch worktree
(`/home/user/rx-213-ctl`) whose `Program.cs` of V and of J set the control case and changed nothing else, removed after
(`git worktree remove`): V's copy over P4 differed from run A's V in one line, `P4-01234-u-05`, `match:0,1` for
`match:0,0`; with S over P4 the comparison held 4,951 differences (SHA-256 `4228e09a…`) and the classifier listed the
planted one **unclassified** and the rest a 1,734, b 3,182, c 34 (`classified.tsv.gz` `ba6e0da3…`). J's copy over P1
differed in one line, `P1-regexp:matching:m1-notes:0001`, `match:0,2` for `match:0,1`; 126 differences (`4feaef16…`),
the planted one **unclassified**, the rest a 110, a' 3, b 5, c 7 (`7ca8f03b…`). They were also taken at `81bd650`,
before the stop list, with the same digests; this round reports those at `a113c91`.

**The fast-path differential**, in both runs: 473,852 pattern-flag pairs compiled twice (181,508 refused), 7,579,193
exec cases compared, **no disagreement** (the record above's 473,804 and 7,578,425 grow by the `\k` patterns S now
accepts).

**The checks, at `a113c91` with this note's files** (the Release build of the solution, no warning and no error; the
tests, 2,631 library, 49 architecture (the starting violation list still empty) and 11 fuzz, all passing; arms S, V, J
and `FastPaths` in Release, no warning; the script tests, 57, all passing with `BROILER_UNICODE` and
`BROILER_VM_CORRESPONDENCE` set, as `run.sh` sets them, and 2 skipped without them (P5's test and the correspondence's
words; the fix round's record said 1); the probe's default report `8f40ae86…` and its `--adversarial` report
`3f7fc538…` under the JIT, both unchanged (no library or probe source differs from `306d204`'s); the assurance check,
no violation; `dotnet pack` gives `Broiler.Regex.0.1.0-preview.6.nupkg` with the library's assembly and documentation,
the nuspec, the README, the notices and the icon, no arm and no arm type in the assembly; and `git merge-tree
--write-tree origin/claude/busy-lamport-m3yprr HEAD` merges with no conflict).

*Dated note, 2026-11-02 (task 2.13, after its second review; no class changed).* "The run, taken twice" above reads
"the classification (`classification.json` `4f6ebdb4…`, `classified.tsv.gz` `a03b9973…`)". That `classification.json`
held the correspondence as `"file"`, the absolute path the two runs were given (the same in both, so their files
agreed), and so its digest depended on where the inputs were. The classifier now names the correspondence by its file
name, git blob (`d7323aec`, the blob named above) and SHA-256 and by no path (`29c980bf`), and a script test runs it
twice from different directories and compares the two files byte for byte (the script tests are 58 now, all passing
with both variables set). The classifier was run again over run A's retained comparison and arm runs, twice at once:
from one working directory with every path relative to it, and from another with absolute paths through a symbolic
link and a copy of the correspondence elsewhere. Both wrote the same counts (a 376,172, a' 45, b 8,983, c 1,516,309,
exit 1 for class (c)), **`classification.json` `614399f118f1870fa4e951495e70a42c3e051ce60072663396b9ab72f1c7c444`**
in each, byte for byte, which differs from `4f6ebdb4…` only in the correspondence's entry, and `classified.tsv.gz`
`a03b9973…` unchanged. Run A's `differences.tsv.gz` (`0a8345fd…`) and `summary.json` (`f0147d1b…`) were read, not
written, and are unchanged.

*Where the plan, the filed rule and this repository differ, read at this record.*
- The filed rule's class (b) holds row M4's empty match by decision D41 and its class (c) excepts it; the plan's text of
  section 4.4 has neither (the rule's revision of 2026-10-25 added both). The rule wins and the classifier reads it.
- The rule's arm V copies "JsRegExpMatcher (Parser, Emitter, Runner, JsRegExpCase, JsRegExpCharSet)" and arm J "CreateRegex,
  translator, GapScan, TryRouteToBroiler and RunMatch"; an exec at a lastIndex also needs how each engine calls its
  matcher, so V copies the realm's members that do (the step back out of a surrogate pair, row M4's Broiler.VM cell) and J
  `ExecMatch`'s sticky emulation, each recorded, and the reading of lastIndex under `g` and `y` is the harness's for all
  three arms, as the rule leaves lastIndex handling in the engines.
- The rule names no bound for an arm; V's allowance (10,000,000 units, S's default step ceiling in the same units), J's
  .NET match timeout (one second, an `ARM CHANGE`) and the harness's wall clock are this task's, each a policy of
  class (b).
- The rule's P2 names "test/annexB" whole; the generator reads all of it, and its cap and grammar are its own, declared.
- The plan's row 2.13 and the brief put the arms under `tests/ReferenceArms/`; the architecture tests read only the
  direct children of `tests/` as projects, so the arms, one level down, are read by rule SL10 alone, as test projects
  are by every other rule.
- The brief's diff list holds no library path, so S's defect is reported, not fixed (above).

*Dated note, 2026-11-02 (task 2.13's second review).* Two items of this list no longer hold. "The rule names no bound
for an arm; V's allowance (10,000,000 units, S's default step ceiling in the same units), J's .NET match timeout (one
second, an `ARM CHANGE`) and the harness's wall clock are this task's, each a policy of class (b)": arm J's .NET `Regex`
is built with no match timeout, as Broiler.JS builds it; every bound of a run, V's allowance and the harness's
processor-time limits among them, is in `tests/ReferenceArms/Common/Limits.cs`; and the harness's stop is class (b)
under D10 for arm J, class (a) under row B8 on that row's inputs, and class (c) for S and V (the note above). "The
brief's diff list holds no library path, so S's defect is reported, not fixed (above)": task 2.12 fixed it (`2e35cbb`),
and its merge brought the fix here.

*Dated record, 2026-11-15 (SJL-11.2's tail toward `0.1.0-preview.6`: task 2.10's library job, WP4's task 4.1, task
2.13's inputs the arbiter cannot reach, and the rule commit named; bundle `sjl-11-001`'s check B-2 and README; decisions
D13, D14 and D35 of Broiler.VM's JSD-0081). Every choice below that the documents did not settle was taken as
recommended under the owner's standing direction of 2026-10-18, "Please do all recommended decisions and skip waiting
for review.", each with the alternative not taken. Nothing here is a reading of the bundle, and nothing is declared
met.*

**What it builds**, in the order of the branch's commits (`sjl-11-2-tail`, from `ec796fc`):
- *Task 2.10's library job* (`36b6c08`): check B-2's runner, `Broiler.Regex.Tests/Test262Library/`, compiled into the
  probe as part D's runner is, with the probe's `--test262` mode, `Test262LibraryRunnerTests` (13 tests over sources
  they write), `test262/library-unreadable.txt`, and CI's `test262-library` job; the probe's README describes them.
- *Task 4.1's two scripts* (`ebbf303`, `07d7825`): [`run-reference-arms.sh`](evidence/sjl-11-001/run-reference-arms.sh)
  and [`run-library-evidence.sh`](evidence/sjl-11-001/run-library-evidence.sh), in the bundle as its README's table
  says.
- *The reduction of task 2.13's inputs the arbiter cannot reach* (`68caeeb`):
  [`scripts/reference-arms/reduce.py`](../scripts/reference-arms/README.md) and its test.
- *The records*, with this one: the bundle's README (two dated notes, the rule commit among them), the probe's README
  and the CHANGELOG (process only).

**Task 2.10, its host-free half (decision D14; check B-2).** D14 reads "test262's `RegExp` directories in this
repository's CI" as two jobs. The **library job** now exists. CI's `test262-library` job checks out tc39/test262 at the
pin `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`, checks its commit and the git tree of its `test/`
(`c022917030da8fbc09b7db8f9b7d55ccc5f5ebab`, the tree task 2.13's record names), publishes the probe with Native AOT and
runs `--test262` under it and under the JIT, on linux-x64 and win-x64, the two reports compared with `cmp`. The runner:
- **(i) the literal syntax tests**, `test/language/literals/regexp` and `test/annexB/language/literals/regexp`: the
  literals a declared grammar extracts, which is population P2's `scan_javascript` over UTF-16 code units with two
  changes its summary states (a literal's flags run over backslashes, so `/./\u0067` reaches the flag parser and is
  refused; a backslash before a line terminator ends the body with no literal, as RegularExpressionBackslashSequence
  reads). Each literal is read twice, by `BroilerRegex.Validate` over the flags' text and by `RegexFlagsText.Parse` then
  `Compile`, at nesting 512 with no compile budget and no meter; the two must agree. A negative file (`phase: parse`,
  `SyntaxError`) passes when a literal is refused, a positive one when every literal is accepted.
- **(ii) `property-escapes/generated` and (iii) `unicodeSets/generated`**, read by a structured reader of `buildString`,
  `testPropertyEscapes`, `testPropertyOfStrings`, `testExtendedCharacterClass`, `Any.js`'s `assert(!/\P{Any}/u.test(""),
  ...)` and the negative files' `$DONOTEVALUATE()`, and nothing else; each call is run as `harness/regExpUtils.js`
  runs it, `regExp.test` being the offset entry from index 0 under no step ceiling and the frame and trail maxima.
- **The unreadable list**, `test262/library-unreadable.txt`, names the 43 literal files the runner cannot judge, each
  with why: 23 whose literals are the text of strings passed to `eval` (19) or built from every code unit for `eval`
  (4), 2 positive files with no literal (`7.8.5-1gs.js`, whose `//` is a comment, and `7.8.5-2gs.js`), 17 negative
  files whose SyntaxError is a lexical error of the literal token itself (a line terminator in the body, a backslash
  before one, `/*` and `//` as comments, a slash the line ends after), and one negative file whose SyntaxError is not
  its literal's (`S7.8.5_A2.2_T2.js`, `/a//.source`: the literal `/a/` and a `/` with no left operand). A listed file
  is still run and its reading reported beside the reason; the run fails on a file that does not pass, on an unlisted
  file the runner cannot judge, on a listed file that passes and on a listed path the populations do not hold.

*Shown locally* (linux-x64, the probe built from `36b6c08`'s runner, test262 at the pin from a `git archive` whose
`test/` tree is `c022917`; the same reports again from `run-library-evidence.sh` at `ebbf303` and `68caeeb`, below):
**829 files, all passing but the 43 listed**: 203 literal files, 469 property-escape files and 114 set files; the JIT's
report and Native AOT's are one file (SHA-256 `db5788374537904b7e266bae9dbeda64624d38987b45a4abf3eb819c0dbd72a8`), in 15
and 24 seconds; the Native AOT publish has no warning. **It fails on a regression**: the rule's control, a scratch
worktree of `36b6c08` whose parser accepts `\k<name>` to an undeclared name under `u` (`!_unicode` added to the
undeclared-name check of `RegexParser.Parse`, never committed, the worktree removed after), made both modes' runs exit 1
with five literal files failing, `named-groups/invalid-dangling-groupname{,-2,-3,-4,-without-group}-u.js`, the two
reports again one file. The rule's control is a reading's (B-2's "controls"), taken in CI at the reading's commit; this
was not that. The job has not run in CI: this branch opens no pull request.

**Task 2.10's host job waits.** D14's host job builds Broiler.JS's shell, at a commit this roadmap names by a dated
record, against the commit's package; "The retained commit is the branch of SJL-11.5 until it merges, then Broiler.JS's
default branch; no commit is named yet" (the record of SJL-11.1 above). Decision D35 asks for that branch early; on
2026-11-15 Broiler.JS's remote holds none (`git ls-remote`), and the shell cannot take `0.1.0-preview.6` without
SJL-11.5's adapter (task 2.9's record: the 29 breaks). So check B-1, its expected-failures file
`test262/host-expected-failures.txt`, the four duplicate-named-groups files task 2.6's test leaves to it
(`duplicate-names-replace.js`, `duplicate-names-replaceall.js`, `duplicate-names-split.js`, staging's
`duplicate-named-groups-replace.js`), and R5's, R6's and R10's exit criteria as D14 reads them ("met when these jobs
run green at a reading's commit") all wait for Broiler.JS's SJL-11.5 branch.

**Task 4.1, the two scripts, each shown reproducing retained outputs byte for byte** (each run behind the machine's
shared build lock and idle gate, from a clean worktree the script made and removed):
- `run-library-evidence.sh ebbf303 OUT --test262 <copy> --broiler-code <Broiler.Code at 2c1ad06>`: every one of its 37
  steps ended as the rule expects (the controls' reports, the Release build with no warning, 2,644 library, 49
  architecture and 11 fuzz tests passing, the publish with no warning and no managed assembly, the four modes' reports
  each one file under both modes, the pack with package validation, the fuzz session with no finding and its control
  reporting the plant, the assurance check with no violation; check B-1 recorded as not run), in 184 seconds. Its
  reports are the retained ones: the default report `8f40ae86…` (task 2.9's record), `--adversarial`
  `3f7fc5387c9015ebe26436f0e40334755a05a4e214d33d6b68f558e0ea70dc75` (task 2.9's; the revert's report is the same file),
  `--vectors` `3b35a4757d294b97441548b93b1fbf31d44587eaffb47055a4ad3b2888603e1b` (task 2.12's) and the fuzz transcript
  `902fbd151db4d37af1cff3a2424e4df0dc8dc768879ef190a22313ff536ee39d` (task 2.12's), each also the base's, taken at
  `ec796fc` before this work changed anything.
- `run-reference-arms.sh`: at `ec796fc` over P1 to P3, every population, answer, comparison and classification file
  byte for byte the base's, which `scripts/reference-arms/run.sh` wrote at `ec796fc` directly; and **the full run at
  `07d7825`** (2026-11-15, 17:03 to 18:14 UTC, the machine shared with other work through its build lock; P1 from
  `b2c2e224` checked against the pin file, test262's `test/` the pin's tree, Broiler.VM's correspondence at
  `292ae6b`, blob `d7323aec`, the blob its `main` still holds at `1e8f648b`): the comparison wrote **1,901,509
  differences, `differences.tsv.gz` `0a8345fd6275347eeaaae432d95aacfdc3c946a4271dca72c9be576f6e5ab677`, byte for byte
  task 2.13's two runs'**, and the classification is theirs: `classification.json`
  `614399f118f1870fa4e951495e70a42c3e051ce60072663396b9ab72f1c7c444` and `classified.tsv.gz` `a03b9973…` (a 376,172,
  a' 45, b 8,983, c 1,516,309, the classifier's exit 1), as is the fast-path differential's summary (`3cb4bb79…`, no
  disagreement); no case reached a backstop. `summary.json` differs, as task 2.13's record says it must at another
  commit: it names the commit. The population's P1 to P3 are the base's files.

**Task 2.13's inputs the arbiter cannot reach** (the record of task 2.13: "Each is class (c) until reduced to an input
the arbiter can reach, which the judged run's classification must do or record"). It was done, by one declared reduction
(`scripts/reference-arms/reduce.py`, `68caeeb`, whose docstring states it), over the full run's classification above,
changing no class, row, rule or cut-off. The classifier leaves **6,459** differences in class (c) with the arbiter past
its cut-off of 1,000,000 Matcher invocations: **V 13** (P4 12 and P1's `budget/k-catastrophic/0001`, `(a+)+b`) and **J
6,446** (2,148 under the invariant culture, 2,151 under `tr-TR`, 2,147 under `lt-LT`: P4's and P3's `O06`), over **2,166
distinct inputs**, every one an exec. For each, the arbiter was asked for the subject cut to a prefix, from the
lastIndex the exec reads (0 without `g` or `y`), and a bisection found a length it reaches with one more code unit past
it; the reduced cases were written as a population, the arms S, V and J (three cultures) were run over it with the
repository's stop list, and the comparison and the classifier read them unchanged (2026-11-15, 18:28 to 18:47 UTC).
**What the runs give:**
- **No prefix the arbiter reaches, not even the empty one: 1,269 inputs, 3,789 differences of arm J.** All come from 19
  P4 patterns, each with a quantifier whose minimum is 4,294,967,296 (in the eight read, over an atom that matches the
  empty string: an anchor, a look-around or `(?:)`, as in `(?:\b){4294967296}S\b[\sK\n]` and
  `($){4294967296}\uD83D\uDE00+.`): RepeatMatcher runs such a body that many times before its empty check applies, so
  the arbiter's cost is in the pattern, and a cut of the subject cannot reduce it. S
  stops at its step, trail or frame ceiling (2,393, 1,381 and 15), and J answers null (3,270) or a match (519). **They
  stay unreachable, class (c).**
- **The difference does not survive the reduction: 895 inputs, all 13 of V's differences and 2,651 of J's.** At the
  longest prefix the arbiter reaches, S and the arm answer alike, and S's answer is the arbiter's (2,648 textually, and
  16 the same match whose names the arbiter writes by their ranges and the arms by their group numbers). V's: S at its
  step ceiling against V's null (5) or match (3), and S's answer against V at its allowance (5: `(a+)+b` over 19
  units, null at 17; P4-34325's match at 33 units). J's: S at its step or trail ceiling against J's null (1,962) or
  match (686), and P3's `O06`, `^[a]*$` under `u` over 10,000,001 `a`, where S matches and J answers null in every
  culture, both matching at the 999,992 units the arbiter reaches. So the difference exists only at sizes the
  specification's matcher needs more than the cut-off for: **they stay unreachable, class (c)**, each recorded with the
  answers at its prefix.
- **Reduced to an input the arbiter reaches, with another difference: 2 inputs, 6 differences of arm J.**
  `P4-38461-s-03` and `-s-05`, `(?=b\W){4294967296}?[\x6b\qẞ]^A|A\uD83D` under `s`: S stops at its step ceiling
  and J refuses the pattern; at the prefixes of 2 and 11 units S and the arbiter answer null and J still refuses, which
  the classifier reads as class (c), "arm J is wrong by the arbiter and no row of the correspondence names the input":
  a quantified look-ahead and `\q` in a class without `u` or `v`, which Annex B admits and J refuses, the family of
  Broiler.JS's differences no row names (the review's item 4 of task 2.13).

None reduced to an input the arbiter reaches with the same difference, so **class (c) keeps every one of the 6,459**,
as the rule's arbiter clause reads them; the reduction's listing (`reductions.tsv` SHA-256 `b3e05301…`), the arms'
answers and the outcomes (`outcomes.json` `2379e634…`, `outcomes.tsv` `9ed9f99a…`) are kept outside the repository, as
the runs' files are. **What would class them otherwise is not this repository's to decide**: a stop of S at its own
declared ceiling against an answer the arbiter cannot check is class (c) by the rule as filed, and only the rule's owner
can revise that reading or declare a larger cut-off for these inputs (a dated rule file, weaker evidence, as the rule's
"Revisions" section says); the 6 that reduce join Broiler.JS's differences, whose rows are Broiler.VM's
correspondence's.

**The rule commit** is named in the bundle's README by a dated note: `fca7a6d6b010d1e830fd79a8f56ceb0c15064976`, the
commit that adds the bundle, `tests/Adversarial/` and `eng/adversarial-programs.py`; the rule files were revised on
2026-10-25 before any run (`cbf49e8`, `a9738ca`, `edfb2d8`) and reached `main` by `e568b88` (Broiler.Regex#6), and
every `rule` block is byte for byte `edfb2d8`'s at this record's commit.

**The checks, at `68caeeb`** (this branch's code; the record's own commit changes Markdown only, and the same checks
at it are in the pull request's body). From a clean `git archive` of `68caeeb`: the Release build of the solution, no
warning and no error; the tests, 2,644 library (2,631 and the runner's 13), 49 architecture (the starting violation list
empty) and 11 fuzz, all passing; the probe's default, `--adversarial` and `--vectors` reports under the JIT and Native
AOT, each one file and the retained one (`8f40ae86…`, `3f7fc538…`, `3b35a475…`); the declared fuzz session, digest
`902fbd15…`, no finding; the assurance check with Broiler.Code at `2c1ad06` (its default branch when CI last ran green
here, 2026-11-02; see item 3 below), no violation. `run-library-evidence.sh 68caeeb`: every step as the rule expects,
the `--test262` reports `db578837…` in both modes. The script tests, 60 (58 and the reduction's 2), all passing with
`BROILER_UNICODE` and `BROILER_VM_CORRESPONDENCE` set. No `0.1.0-preview.6` was published and no workflow was
dispatched by this work.

**What remains before the candidate reading (task 4.2)**, none of it this repository's alone:
1. **Class (c) of the reference arms is not empty**, and the rule's MET needs it empty: Broiler.VM's 22 differences of
   `\w` in a set difference under `iv` need a correspondence row and a revision of JSD-0081 (Broiler.VM's records);
   Broiler.JS's 1,509,828 differences no row names need rows, each a correction of Broiler.JS at SJL-11.5 (Broiler.VM's
   correspondence, Broiler.JS's corrections); and the inputs the reduction leaves unreachable (above) stay class (c).
   How each is classed turns on decisions that are Broiler.VM's, Broiler.JS's or the owner's, and none is taken here.
2. **The host job**, which waits for Broiler.JS's SJL-11.5 branch, its adapter on the `0.1.0-preview.6` surface, a
   dated record here naming its commit, and `test262/host-expected-failures.txt`.
3. **CI at the reading's commit, every job green on the default branch (B-3)**, the library job and the host job among
   them. The `assurance` job checks out Broiler.Code's default branch, whose `src/Broiler.Code.Review.Cli` its commit
   `1b31684` ("Switch to Hairs", 2026-11-06) removed, so the job as written fails today: the owner's publish run of
   2026-11-15 at `ec796fc` (run 43734601996) stopped at it, and its publish job was skipped. The pull request
   Broiler-Platform/Broiler.RegEx#19, open on 2026-11-15, moves the check to Hairs; it is not this work's, and it
   touches `.github/workflows/ci.yml` too.
4. **The reading itself, at the commit `0.1.0-preview.6` is packed from**: both scripts run there, CI's runs named by
   their identifiers, part B's control (a scratch commit turning a job red) taken, each part judged from its retained
   files, and the bundle's `manifest.json` written with the first reading.
5. **Then the owner's publication of `0.1.0-preview.6` (decision D13)**, after the candidate reading and only if no
   part reads NOT MET there. This work bumps no version: `Directory.Build.props` gives `0.1.0-preview.6` since task 2.9,
   and `eng/preview-version-history.json` is not touched.

*The choices taken, each the recommended one under the owner's direction of 2026-10-18, with the alternative not
taken.*
- **The runner is C#, one source in the tests and the probe**, as part D's is, so the JIT and Native AOT run the same
  code over the same files. Not taken: a Python extractor writing a case file for the probe, which would put the
  grammar outside both modes and a second language in CI's Windows leg.
- **The literal grammar is P2's, declared, with its two changes**; a literal in a string passed to `eval` is listed,
  not read. Not taken: reading `eval`'s strings, which would make the runner a partial host.
- **A listed file is still run, and fails the run when it passes.** Not taken: skipping listed files, which would let
  the list outlive its reason unseen.
- **The job runs on linux-x64 and win-x64**, as the `native-aot-probe` job does for parts C and D. Not taken: linux-x64
  alone, which the rule would allow and which would not show the Windows image.
- **`regExp.test` runs under no step ceiling and the frame and trail maxima, at nesting 512**: test262 asserts answers,
  not limits, and the maxima are the most the library gives. Not taken: the engines' ceilings, under which a limit
  would read as a failure of the file.
- **The scripts take the commit to read and run in a detached worktree** of it, as the README's table asks of
  `run-library-evidence.sh`, and `run-reference-arms.sh` takes Broiler.VM's correspondence by a commit, written out
  with its blob, rather than by a path. Not taken: running in the caller's tree, or a path to the correspondence.
- **E-6 runs only when a Broiler.Code checkout is given**, and is recorded as not run otherwise; locally it ran with
  Broiler.Code at `2c1ad06`. Not taken: requiring it, which would make the script fail today for a reason outside
  this repository (item 3 above); and moving the check to Hairs, which Broiler-Platform/Broiler.RegEx#19 does.
- **The reduction cuts the subject only**, by bisection over its prefixes. Not taken: cutting the pattern (it changes
  which construct differs) and running the arbiter past its cut-off (it changes the classifier's parameter); either
  would decide what the rule leaves to its owner.
- **The rule commit is named as `fca7a6d`**, by the README item's own words, with its revisions and the merge beside
  it. Not taken: naming the merge `e568b88` or the last revision `edfb2d8` alone.

*Where the brief, the map and this repository differ, read at this record.*
- **"Nothing else runs locally now"** (the brief): another stage's work (Broiler.VM's SJL-13 exit reading) ran on the
  same machine through the same build lock while this work ran; every run here waited for the lock and the idle gate,
  so no two builds ran at once.
- **The brief and the map count the unreachable differences as "V 13 and J 6,446"**: the classification gives V 13
  and J 2,148, 2,151 and 2,147 by culture, 6,446 in all. Task 2.13's record (item 3 of the review) gave J "2,043 under
  the invariant culture" for the earlier classifier `@2`; the `@3` classification its later note supersedes it with
  does not break item 2 down by culture.
- **The rule's check B-2 names "test262-library" as the job**; it says nothing of the RIDs. The job runs on both, as
  parts C and D do.
- **D14 reads the host job's population as "test262's `RegExp` paths at the programme's pin with negative tests"**, and
  check B-1 names `scripts/compliance/run_test262.py` of Broiler.JS; neither is touched here.
- **The CI `assurance` job** checks out Broiler.Code's default branch, which no longer holds the command the job runs
  (above): the job fails as written at `ec796fc` itself, so it is not this branch's regression; the check was run
  locally against Broiler.Code at `2c1ad06`.
- **The brief names the rule commit "around RXB:119-125"**; the README's item is there, and the note names
  `fca7a6d` by the item's own definition (the commit that adds the bundle), with the revisions and the merge beside it.

*Dated note, 2026-11-15 (the review of SJL-11.2's tail; the record above is left as written, and this note corrects it
where it says).* Read at the branch's head after this note's commits; nothing here is a reading, and nothing is declared
met.

**`0.1.0-preview.6` is published.** The record's items 4 and 5 below "What remains" read "**The reading itself, at the
commit `0.1.0-preview.6` is packed from**" and "**Then the owner's publication of `0.1.0-preview.6` (decision D13)**,
after the candidate reading and only if no part reads NOT MET there", and the bundle README's note of today reads "**No
reading's commit is named yet**: the candidate reading's is the commit `0.1.0-preview.6` is packed from (task 4.2)".
Neither holds. `0.1.0-preview.6` was published to nuget.org from `df5cfc2cc7d71cf0824aa8963dcdb956ac7fca9e` (main after
Broiler.Regex#19) by the Publish workflow's run 43736141111 (workflow_dispatch on `main`, created 16:33:28 UTC; its
"Publish to nuget.org" job pushed `Broiler.Regex.0.1.0-preview.6.nupkg` and `.snupkg` at 16:37:08 UTC), and nuget.org's
index lists `0.1.0-preview.6`. That was before any candidate reading, and before the record above was written (19:01
UTC), which did not know it. The commit it was packed from holds no `--test262` mode, no `test262-library` job and
neither of task 4.1's scripts, so the reading the record describes cannot be taken as written, and D13's order
("Publication stays the owner's, after the candidate reading", the dated note of 2026-11-01 above) was not followed.
This work dispatched no workflow and published nothing; the record's "No `0.1.0-preview.6` was published and no workflow
was dispatched by this work" stands as a statement about this work only. **The owner's open question, not decided
here:** whether the candidate reading (task 4.2) is taken at `df5cfc2`, the published package's commit, whose tree lacks
check B-2's job and the scripts (they would then run from a later tree against it), or at a later commit that carries
them, packed as a later version (a version bump this work does not owe and does not make). The CHANGELOG's heading
"`0.1.0-preview.6` (2026-11-01; prepared, not published)" is corrected there by a dated note.

**Broiler.Regex#19 is merged, and this branch now carries it.** Item 3 reads "The `assurance` job checks out
Broiler.Code's default branch, whose `src/Broiler.Code.Review.Cli` its commit `1b31684` ("Switch to Hairs", 2026-11-06)
removed, so the job as written fails today ... The pull request Broiler-Platform/Broiler.RegEx#19, open on 2026-11-15,
moves the check to Hairs; it is not this work's, and it touches `.github/workflows/ci.yml` too." The choice "**E-6 runs
only when a Broiler.Code checkout is given** ... Not taken: requiring it, which would make the script fail today for a
reason outside this repository (item 3 above); and moving the check to Hairs, which Broiler-Platform/Broiler.RegEx#19
does" and the disagreement "the job fails as written at `ec796fc` itself" rest on the same premise. Broiler.Regex#19
merged at 16:32:50 UTC as `df5cfc2` (its change `aa219db`: Hairs.Cli `0.1.0-preview.2` pinned as a local tool,
`assurance.config.json` renamed `hairs.config.json`, the job `dotnet tool restore` then `dotnet hairs assurance check
--root .`); CI's run 43736082350 on `main` at `df5cfc2` succeeded, as did the publish run's assurance job. The branch
merged `main` at `df5cfc2` (`371692f`, no conflict), and `run-library-evidence.sh`'s check E-6 now repeats the commit's
CI job: at a commit with `hairs.config.json`, the tree cleaned, `dotnet tool restore` and `dotnet hairs assurance check
--root .`; `--broiler-code` is read only for a commit before #19. *Choice:* that fallback is kept, so the script still
reads the commits before #19 as their CI did. Not taken: dropping it, which would leave E-6 unrun at those commits. The
record's references "Broiler-Platform/Broiler.RegEx#19" are this repository's own pull request, written here, as
elsewhere in this file, Broiler.Regex#19.

**The checks at the scripts' commit** `3760449` (this branch, after the merge and the review's fixes), behind the
machine's build lock and idle gate: `run-library-evidence.sh 3760449 OUT --test262 <copy whose test/ is c022917>`, every
one of its 39 steps as the rule expects in 168 seconds, among them the Release build with no warning, 2,644 library, 49
architecture and 11 fuzz tests passing, the four modes' reports each one file under Native AOT and the JIT (the default
report `8f40ae86…`, `--adversarial` `3f7fc538…`, `--vectors` `3b35a475…`, all three the retained ones) and E-6 under
Hairs ("Broiler.RegEx: no violations."). The `--test262` report is now
`374235078880ad51c2aa0c39131fc3a5d278829ff4d0ec0fabbc00001b5d7b4d`, in both modes: it differs from `db578837…` above in
one line only, the unreadable list's SHA-256, since the list changed (below).

**What reproduces byte for byte.** The record says each script was shown "reproducing retained outputs byte for byte".
For `run-library-evidence.sh` that holds of its reports, not of all it writes: two runs at one commit give packages of
other bytes (the pack writes times into the zip's entries; the review's run at `2c57110` against the implementer's), so
the packages' digests are now written to `packages.sha256`, apart from `digests.sha256`, and the script's header says
only `reports/` reproduces. The packages it leaves are its evidence for check E-4 only, carry the version the commit's
props give, and are never pushed to a feed; those of the runs before this note were deleted from scratch. For
`run-reference-arms.sh`, the P1-to-P3 comparison the record calls the base's byte for byte was taken by an uncommitted
draft of the script, whose run exited 1 (`ebbf303` committed it after); the committed script was shown on the full run
at `07d7825`, which exited 0. The committed script, with its new check, was run at `3760449` over P1 to P3 (19:55 UTC,
76 seconds, exit 0, the classifier's exit 1 kept): of the 28 files it lists, 25 are the base's byte for byte, and
`population/manifest.json`, `runs/run.S.json` and `comparison/summary.json` differ only where they name the commit. Its
first try at `3760449` stopped in arm S: `Harness.ProcessorTime` (`tests/ReferenceArms/Common/Harness.cs`) catches
`InvalidOperationException` only, and reading `TotalProcessorTime` of a worker that had just exited threw
`Win32Exception` in the supervisor's reading thread, which ended the process. The run was taken again unchanged and
passed. That defect of the harness is recorded, not fixed here: it can end any run of the arms, the candidate reading's
among them, and its fix is the arms' owner's to make before the reading.

**Broiler.Unicode's commit.** P5 and P6 come from the Broiler.Unicode checkout, which the script recorded but did not
check. It now takes `--unicode-commit`, by default `0bcb917564d514feadaa27ce7dd1e0b57b585c90`, the commit of the full
run at `07d7825` that gave task 2.13's comparison and classification byte for byte, and checks the checkout's HEAD and
its tracked files against it. Task 2.13's own record names no Broiler.Unicode commit, so P5 and P6 are shown to
reproduce at this commit only.

**The reference arms' two negative controls are owed.** `reference-arms.md` asks for "a copy of arm V with one result
changed for one input of P4, and a copy of arm J with one result changed for one input of P1, each run once; the
comparison reports each as a difference and does not classify it", and its MET needs both reported unclassified. Task
2.13 took them from scratch worktrees (its record above), as `tests/ReferenceArms/README.md`'s "The negative controls"
describes; `run-reference-arms.sh` runs `scripts/reference-arms/run.sh`, which has no control step, and so does not take
them. Item 4 below "What remains" names part B's control but not these: they are owed at the candidate reading, beside
the script's run, the same way task 2.13 took them.

**The library job's unreadable list gives each file's reading.** The record reads "A listed file is still run and its
reading reported beside the reason; the run fails on a file that does not pass, on an unlisted file the runner cannot
judge, on a listed file that passes and on a listed path the populations do not hold". A listed file was so accepted
with any reading but a pass, and one listed file, `S7.8.5_A2.2_T2.js`, reads FAIL, not unreadable (negative, its only
literal `/a/` accepted). Each entry now gives the reading the runner gives the file (42 `unreadable`, that one `FAIL`),
and the run fails when a listed file reads otherwise (`f58b5c9`; the runner's test covers both).

**The Windows leg is unshown.** The record says the job "runs on linux-x64 and win-x64"; every local run was linux-x64,
and the job has not run in CI. The pin check reads git's objects (`rev-parse HEAD:test`), not the working tree, and the
Windows runner's git converts line endings on checkout by default; the job now sets `core.autocrlf` to false on the
runner before checking test262 out, so the bytes read are the objects'. The win-x64 leg stays unshown until CI runs it.
On a red run each step now prints the report's FAIL lines.

**The 895 inputs whose difference does not survive.** The record reads "**The difference does not survive the reduction:
895 inputs** ... So the difference exists only at sizes the specification's matcher needs more than the cut-off for:
**they stay unreachable, class (c)**", and "None reduced to an input the arbiter reaches with the same difference, so
**class (c) keeps every one of the 6,459**, as the rule's arbiter clause reads them". These 895 inputs were **reduced to
an input the arbiter reaches, where the difference does not survive**: they are not unreachable. That their 2,664
differences stay in class (c) is a reading the rule does not settle, not the rule's own: "a difference the arbiter
cannot reach in its step cut-off is class (c) until reduced to an input it can" does not say whether the reduced input
must carry the same difference. *Choice taken*, and taken by `reduce.py`: a reduction discharges class (c) only when the
same difference survives it (the outcome `reduced`). *Not taken*: the reduced input's agreement with the arbiter ends
the "until reduced" condition, which would take these 2,664 differences out of class (c). Which reading applies is the
rule owner's decision, not this repository's; `reduce.py`'s docstring now says so. The 1,269 inputs with no prefix the
arbiter reaches are unreachable under either reading, and the record's "the 6 that reduce join Broiler.JS's differences,
whose rows are Broiler.VM's correspondence's" is better read: the 6 resemble the family of Broiler.JS's differences no
row names; whether a row covers them is Broiler.VM's call.

**The reduction, regenerable.** The record gives its listings by 8-digit prefixes and keeps them in session scratch,
which can be cleared. In full: `reductions.tsv` `b3e053014bbbdd8c2cf48419edc4f0053b520b642166a45d5acdbabfd3b56237`,
`outcomes.tsv` `9ed9f99ac1cf5643b0a7d38e983d49142026d8dd0ad9202494684f6cf95e9efa`, `outcomes.json`
`2379e634b80d0707cd84c47b8adb980abf8aa74cf1615eda1b5493a5d6ecf4b7`. Their inputs: the full run's comparison
(`differences.tsv.gz` `0a8345fd6275347eeaaae432d95aacfdc3c946a4271dca72c9be576f6e5ab677`, `classified.tsv.gz`
`a03b9973191e969c41c107f8bbf1d80c0bdf1950729623a5e4b9968bea69da61`), Broiler.JS at
`b2c2e2243a086285d806866a64b83297791a8ec5` (the arbiter), Broiler.Unicode at `0bcb917564d514feadaa27ce7dd1e0b57b585c90`
and Broiler.VM's correspondence at `292ae6b2834f98be5f9a1f93c409a4e1aa832c66` (blob
`d7323aeccd7a9c70aa7e34cd9667bccdf000eebb`). The commands, from a tree of this repository at the commit read:
1. `python3 -I -B scripts/reference-arms/reduce.py prefixes --comparison <full run>/run/comparison --broiler-js
   <Broiler.JS> --commit b2c2e2243a086285d806866a64b83297791a8ec5 --unicode-pins <Broiler.Unicode>/data/unicode/pins
   --out OUT --workers 4` (the cut-off its default, 1,000,000, the classifier's);
2. the arms S, V and J built Release (`dotnet build tests/ReferenceArms/<arm> -c Release`) and each run as `dotnet
   tests/ReferenceArms/<arm>/bin/Release/net10.0/Regex.ReferenceArms.<arm>.dll --population OUT/population --out
   OUT/runs --classes P1,P3,P4 --stops tests/ReferenceArms/stops.tsv`, J also with `--culture tr-TR` and `--culture
   lt-LT`;
3. `python3 -I -B scripts/reference-arms/compare.py --population OUT/population --runs OUT/runs --out OUT/comparison`;
4. `python3 -I -B scripts/reference-arms/classify.py --comparison OUT/comparison --runs OUT/runs --broiler-js
   <Broiler.JS> --commit b2c2e2243a086285d806866a64b83297791a8ec5 --correspondence <correspondence.md at 292ae6b>
   --unicode-pins <Broiler.Unicode>/data/unicode/pins` (its exit 1, class (c) present, expected);
5. `python3 -I -B scripts/reference-arms/reduce.py read --comparison <full run>/run/comparison --reduced OUT`. Run again
   at `3760449` from a clean `git archive` (19:36 to 19:54 UTC), these commands gave `reductions.tsv`, the reduced
   population, `outcomes.tsv` and `outcomes.json` byte for byte as above, and every comparison and run file the same but
   the two that carry the build's informational version (`runs/run.S.json` and `comparison/summary.json`: an archive's
   build has no commit in it). The listings stay outside the repository, as the runs' files do (validation principle 5);
   the candidate reading's retained files are where a durable copy belongs, if the rule's owner wants one.

**The rule commit, and Broiler.VM's convention.** The bundle README's note names "**The rule commit is
`fca7a6d6b010d1e830fd79a8f56ceb0c15064976`**", the branch commit that adds the bundle, and the choice above reads "Not
taken: naming the merge `e568b88` or the last revision `edfb2d8` alone." Broiler.VM's half of the same bundle names the
default-branch merge: its README's note of 2026-11-15 (`d4a6d1ac`) reads "merged on 2026-10-25 in
Broiler-Platform/Broiler.VM#168 as `cd1928d6...`, the rule commit, the default-branch merge, as bundles `sjl-12-001` and
`sjl-13-001` name theirs". Under that convention this README's rule commit would be the merge
`e568b88248c7de5dd75210e22cd357197108e644`, whose rule files are `edfb2d8`'s, the rules as filed. Which this bundle
names is the owner's or the bundle reviewer's choice; `fca7a6d` is not settled by this record. The README's "revised the
two rule files three times" is also loose: the rule files were revised in three commits, `cbf49e8` and `a9738ca`
changing both and `edfb2d8` changing `decision-rule.md` only.

*Where the brief, the map and this repository differ, added.* The brief's State reads "Broiler.Regex `origin/main` is
`ec796fc`"; `main` was `df5cfc2` from 16:32 UTC, twelve minutes after this branch was made from `ec796fc`, and the local
`origin/main` was fetched to it at 16:56, before the record above. The brief's and the record's order, the candidate
reading before the owner's publication of `0.1.0-preview.6` (D13), was overtaken by that publication (above).

*Dated record, 2026-11-16 (SJL-11.2: the reference arms' classifier reads Broiler.VM's rows of 2026-11-16, the
reference-arm rule's revision on the inputs the arbiter cannot reach, and the arms classified again; bundle
`sjl-11-001`'s [`reference-arms.md`](evidence/sjl-11-001/reference-arms.md); decisions D-wiv and D-jsrows of Broiler.VM's
JSD-0081). Every choice below that the documents did not settle was taken as recommended under the owner's standing
direction of 2026-10-18, "Please do all recommended decisions and skip waiting for review.", each with the alternative
not taken. Nothing here is a reading of the bundle, no arm was run again, and nothing is declared met.*

**What it builds**, in the order of the branch's commits (`sjl-11-2-arms`, from `b6580b8c`,
Broiler-Platform/Broiler.Regex#20's merge):
- *The classifier `@4`* (`083e202`): [`scripts/reference-arms/classify.py`](../scripts/reference-arms/README.md) reads
  Broiler.VM's correspondence at **`7c65a8febacdcf8bda2d848d21ac2e7748696359`**, the merge of
  Broiler-Platform/Broiler.VM#211 (the file's blob `f0f6f0800d7d6336ab1c538bb7afd076a8a1fb2f`, last changed in
  `225b5533`), and names that commit in `classification.json` as `rows-read-from`, beside the blob of the file a run is
  given; predicates for rows S8a, S12, S13, S14, S15, M8 and M9, and rows S7's, S8's and M4's reaching the vectors those
  rows name; the option `--reduction`; the script tests (74, from 60) and the scripts' README.
- *The rule's revision* (`c58c3f8`):
  [`reference-arms-revised-2026-11-16.md`](evidence/sjl-11-001/reference-arms-revised-2026-11-16.md), option R.
- *The records*, with this one: the bundle's README (a dated note) and the CHANGELOG (process only).

**The predicates, each read from its row's text at `7c65a8fe`.** The predicates of `@3` are unchanged and come first,
so a difference `@3` classed keeps its class and row; each new predicate reads the row's direction (which side refuses,
or that both answer and differ) and the construct or reason the row names.

| Row | Arm | What the predicate reads | Where the row's text does not settle it |
|---|---|---|---|
| S7 | J | as `@3`, and a P1 vector without `u` or `v` J accepts and S refuses, a quantified `^`, `$` or `\b`, the arbiter's reason nothing to repeat (the widening of 2026-11-01) | P1 alone: row S12 names the vectors as S7's and holds the arms' P4 inputs of that reason |
| S8 | J | as `@3`, and a P1 vector under `v` J accepts and S refuses for `grammar:class-set-operator` (`[a-z&&b]`, `[a-z&&[^aeiou]]`) | P1 alone, for the same reason |
| M4 | J | as `@3`, and from a lastIndex inside a pair, J's capture from the trail where S's holds the pair (`(.)` under `gdu`), or J's null where S matches at lastIndex (`[𝌆]` under `gu`) | the widening names two vectors; the predicate reads their two shapes |
| S8a | V | under `iv`, `\w` the first operand of a class difference (`[\w--`, alone or nested), an exec both answer, V matching code units S's answer does not take, among them a letter `A`-`Z`, `a`-`z`, U+017F or U+212A | the flags as written (`i` and `v`), not a modifier |
| S12 | J | J accepts what S refuses, the arbiter's reason one of the row's eight, under the flags the row names for it (`invalid-escape`, `lone-syntax-character`, `invalid-class-escape` under `u` or `v`; `class-set-operator` under `v`; `invalid-group` under `u`); not the vectors rows S7 and S8 hold | the reason alone for nothing to repeat: among the distinct pattern-flag pairs it takes under `u` or `v`, 2,449 quantify a look-around, which the row's parenthesis does not list and its sentence "take the constructs of rows S7's and S8's subjects ... this row holds them" does |
| S13 | J | J accepts what S refuses, the arbiter's reason one of the row's six, under the flags it names (`decimal-escape-past-group-count` under `u` or `v`, `negated-class-with-strings` under `v`) | none found |
| S15 | J | in P4, under `i`, J refuses what S accepts, a braced bound past 2^31 - 1 outside a class, and no acceptance difference of the same pattern in the same run under the flags less `i` | "accepts without `i`" is read from the comparison, which holds J's refusals only where S accepts; so P4 alone, whose every pattern runs under `""`, `u` and `v` as under `i`, `iu` and `iv` |
| S14 | J | without `u` or `v`, J refuses what S accepts, under no flag, `s`, `i` or `m` alone, or under other flags where the same run refused the same pattern with no flag | the row's four kinds are read by their flags, not by the constructs it gives as examples; "by B.1.2" is read as the grammar without `u` or `v` |
| M8 | J | an exec both answer, J otherwise, and one of the row's listed inputs: `\b`, `\B`, `\w`, `\d`, `\s` or a complement in the pattern with a code point past ASCII in the subject; `i` with U+0130 or U+0131 in the input; an empty `\q{}` in a class under `v`; P5's `I`, `[I]`, `i`, `[i]` under `i` against `i` or `I` in `tr-TR` | **not settled**: the row's sentence "An `exec` both sides answer, where Broiler.JS answers otherwise" is followed by a list; read with its list (below) |
| M9 | J | the row's one input, J's `ArgumentOutOfRangeException` against S's answer | none |

**The base answer: the read path unchanged.** Before anything changed, the classifier `@3` (`b6580b8c`'s
`classify.py`) was run over task 2.13's retained comparison (`differences.tsv.gz` `0a8345fd…`, the full run of
2026-11-15 at `07d7825`) with the correspondence at `292ae6b` (blob `d7323aec…`), out of place: it wrote
**`classification.json` `614399f118f1870fa4e951495e70a42c3e051ce60072663396b9ab72f1c7c444` and `classified.tsv.gz`
`a03b9973191e969c41c107f8bbf1d80c0bdf1950729623a5e4b9968bea69da61`, the retained files byte for byte** (a 376,172,
a' 45, b 8,983, c 1,516,309, exit 1). A first try wrote another `classification.json` (`5a81ceb3…`) and the same
`classified.tsv.gz`: the correspondence's copy was named `correspondence-292ae6b.md`, and `classification.json` names
the file by its name. Run with a copy named `correspondence.md`, it gave the retained bytes.

**The classification again** (2026-11-16, 08:32 to 08:33 UTC), by `@4` at `083e202` from a `git archive`, over the
same retained comparison and arm runs, with the correspondence at `7c65a8fe` (as `correspondence.md`) and
`--reduction` over the reduction of 2026-11-15 (`reductions.tsv` `b3e05301…`; the arbiter's answers read from the
reproduction's cache, which holds only answers that depend on the input, the arbiter's commit and the cut-off):
**`classification.json` `5308b6fe9cda22c472e0a4515d13de02a55c92cf1c979a31ebec7c64270cb1bd`, `classified.tsv.gz`
`ecae396f4c3a6a4743a3bdcf61909da9d5d5867e5470ab78a2f8e4502f40d8bb`**, exit 1:
- **(a) 1,875,164**: `@3`'s 376,172 by its rows unchanged, and 1,498,992 that `@3` left in class (c): S12 760,775,
  S14 437,325, S13 252,195, M8 46,594, S15 2,057, S8a 22 (arm V), S7 9, S8 6, M4 6, M9 3;
- **(a') 45** and **(b) 8,983**, `@3`'s unchanged;
- **(b') 2,661**, option R's "beyond the arbiter": arm J 2,648 (S at its step ceiling 2,552 or its trail ceiling 96,
  against J's answer, which S and J both give at the prefix and the arbiter confirms there) and arm V 13 (S at its step
  ceiling against V's answer 8, V at its allowance against S's answer 5);
- **(c) 14,656**, below.
The classifier was also run over the reduction's own comparison (its six differences): all six are row S14's
(`classification.json` `c594b12c…`, `classified.tsv.gz` `b71ed577…`). Both outputs are kept beside the old ones,
outside the repository as the runs' files are, with a `hashes.txt` of every file written; no retained output was
written to. A check over every difference with the arbiter's answers `@3` recorded showed each of `@3`'s (a), (a') and
(b) differences in the same class and row under `@4`.

**The reduction, read under the revision.** Option R declares `reduce.py`'s own reduction (a prefix the arbiter reaches
with one more code unit past it, found by bisection from the lastIndex the exec reads, at the same cut-off), so the
reduction of 2026-11-15 is read as it was run and **is not run again**. Its outcomes now read: the 6 differences
reduced to another difference are row S14's (a), as their reduced difference is; of the 2,664 whose difference does not
survive, 2,661 are (b') and 3 stay (c); the 3,789 with no prefix the arbiter reaches stay (c).

**What class (c) still holds: 14,656 differences, every one arm J's, none a defect of S.**
1. **No prefix the arbiter reaches: 3,789** (1,269 inputs of 19 P4 patterns; 1,263 under the invariant culture,
   1,268 under `tr-TR`, 1,258 under `lt-LT`). Each pattern quantifies, with a minimum of 4,294,967,296, a body that
   matches the empty string, so the arbiter's cost is in the pattern and no cut of the subject reduces it; S stops at
   its ceilings and J answers. They stay (c) under option R, and under every alternative but the third.
2. **P3's `O06`, 3** (one per culture): `^[a]*$` under `u` over 10,000,001 `a`, S's match against J's null. At the
   prefix of 999,992 units S, J and the arbiter agree, but at the input neither side stopped at a bound it declares, so
   option R's proviso does not hold: J's null is a wrong answer the arbiter cannot reach, not a stop.
3. **An exec both answer where J answers otherwise, outside row M8's list: 10,864** (3,880 inputs, all P4; 3,492
   under the invariant culture and `lt-LT` each, 3,880 under `tr-TR`). By what differs: the group names alone, 3,815
   (203 pattern-flag pairs: Broiler.JS's translator rewrites a name, `$x` under `m` or a modifier as
   `(?=$|\r)x`, `π` under `i` as `[πΠϖ]`); another match, 3,432 (323: `\12` and
   `\p{Lu}` without `u`, `i+` under `i`, among them); a null against a match, 2,144 (439); the same match with a
   capture's participation differing, 1,473 (91: an optional group that matched empty). Row M8's sentence "An `exec`
   both sides answer, where Broiler.JS answers otherwise" would hold them all; its list ("`\b` and `\B` beside a
   non-ASCII letter ..., `\w`, `\d` and `\s` beside them, case-insensitive matches of U+0130 and U+0131 no row's pairs
   hold ..., an empty `\q{}` in a class under `v` ... and under `tr-TR` P5's `I` and `[I]` ...") does not name them.
   *Choice taken*: the predicate reads the list, as every predicate since `@2` reads its row's construct and not a
   direction alone. *Not taken*: the sentence alone, which would class these 10,864 (a) under M8 (Broiler.VM's stage
   counted 57,458 there; `@4` classes 46,594). Whether row M8 holds them is Broiler.VM's to say, by a dated widening
   of the row or a row of its own.
Where the arbiter answers (items 2's prefix and 3), S's answer is the arbiter's; where it does not (item 1), S stops at
a ceiling it declares.

**The population question, drafted and put to the owner (not taken).** Class (c)'s item 1 empties only if population
P4 is read without the patterns whose arbiter cost is in the pattern itself. *Drafted option P*, as a dated revision of
the rule's P4 line: "P4 ... excluding a pattern drawn with a quantifier whose minimum is at least the arbiter's
cut-off over a body that can match the empty string, read from the pattern's text; the excluded patterns are listed
with the population's manifest". Its extent is not measured here: of P4's 65,536 patterns, 2,105 hold a braced minimum
of 1,000,000 or more, the 19 among them, and how many of those 2,105 have a body that can match the empty string was not
computed. A narrower form lists the 19 patterns by identifier, which chooses them by their outcome. Either changes the
population after its run was seen, so it would be weaker evidence still, and it is the owner's to take or refuse, not a
recommended choice here. **Without it, or another reading the owner takes, class (c) cannot empty, and the reference
arms read NOT MET at the candidate reading (task 4.2).**

**What the candidate reading (task 4.2) now waits on**, none of it decided here:
1. **The owner on the rule**: confirming or overturning option R (filed as recommended, weaker evidence), and taking or
   refusing option P above. Without option P, or alternative 3 of the revision, class (c) keeps item 1.
2. **Broiler.VM on row M8**: whether its row holds item 3's 10,864 differences (a dated widening, or a new row). Until
   then they are class (c).
3. **P3's `O06`** (item 2): no row and no reading of the rule takes it; it needs the owner's reading of option R's
   proviso, or a row of Broiler.VM's correspondence for J's null there, or the arbiter reaching the input.
4. **The run's procedure under the revision**: `run-reference-arms.sh` runs `run.sh`, which neither runs `reduce.py`
   nor passes `--reduction` to the classifier; the reading needs both after its comparison, and the arms run over the
   reduced population with the stop list, as the record of 2026-11-15 lists.
5. Still owed from the records of 2026-11-15: the two negative controls of `reference-arms.md`, taken beside the
   script's run; the host job (check B-1), which waits for Broiler.JS's SJL-11.5 branch; CI green at the reading's
   commit (B-3); the harness's `Win32Exception` in `Harness.ProcessorTime`, the arms' owner's to fix before the
   reading; the owner's choice of the reading's commit (`df5cfc2`, from which `0.1.0-preview.6` was published, or a
   later one) and of the rule commit the README names (`fca7a6d` or `e568b88`).

**The choices taken**, each the recommended one, with the alternative not taken:
1. *Option R filed as the rule's revision* (the brief's direction). Not taken: alternatives 1 to 3 of the file, each
   recorded there with its effect.
2. *The declared reduction worded as `reduce.py`'s*: "a prefix the arbiter reaches with one more code unit past it,
   found by bisection". The draft read "the longest prefix the arbiter reaches", which `reduce.py` finds only where the
   arbiter's cost grows with the prefix. Not taken: the draft's words, under which the reduction would have to be shown
   to find the longest prefix, or run again.
3. *Option R's (b') reads the bounds the draft names*: S's step, frame and trail ceilings, arm V's allowance and arm J's
   harness's stop. Not taken: arm V's ceilings (row B6) and a refusal for nesting, which the draft does not name; no
   input of the reduction stops at them.
4. *Row M8 read with its list* (above). Not taken: its first sentence alone.
5. *Rows S7's and S8's widenings read for P1 alone*, as row S12's text divides them. Not taken: the constructs in any
   population, which would move P4's differences from S12 to S7 and S8, against S12's "this row holds them".
6. *Row S14's kinds read by flags; row S15's "accepts without `i`" read from the comparison, in P4 alone.* Not taken:
   S14 by its first sentence alone, which would also take J's refusals under `y` or `d` alone that the comparison does
   not show refused with no flag (none in this run).
7. *The new classification kept outside the repository*, beside the old one, as the runs' files are (validation
   principle 5). Not taken: committing it, which the bundle's README reserves for a reading.
8. *The CHANGELOG item under `0.1.0-preview.6`*, as the item of 2026-11-15 is, process only and in neither the
   published package nor its API. Not taken: a new heading, which the owner's open question on the reading's version
   would decide.

**Where the brief, the sources and this repository differ, read at this record.**
- The draft's option R names "the longest prefix the arbiter reaches"; `reduce.py`'s docstring finds "a length m* the
  arbiter reaches with m* + 1 past it", the longest only where the cost grows (choice 2). The scripts' README's row for
  `reduce.py` (2026-11-15) also says "the longest prefix"; it is left as written.
- The draft quotes `decision-rule.md`'s "Revisions" as one line; the file wraps it after "quoting this one whole and".
  The words are the same.
- The draft says alternative 3 was refused by "the review of task 2.13 ... because 'an arbiter past its cut-off has
  checked nothing'". Those words are the classifier's comment (`@2`, 2026-11-02), not the review's record here; the
  revision quotes the comment.
- Broiler.VM's records of 2026-11-16 read rows S8a, S12 to S15, M8 and M9, with S7, S8 and M4, as naming every
  difference the arbiter decides. Read with its list, row M8 does not name item 3's 10,864 (above).
- The brief names "Broiler.Regex `origin/main` `b6580b8c`" and the push ref at `4fb36fc2`, an ancestor of it; both
  held when this record was written.

*Dated note, 2026-11-16, at the review of the classifier `@4` (SJL-11.2; before any judged run; no arm was run again,
and nothing is declared met).* The review found row M8's predicate wider than its row; the classifier is now `@5`
(`4e8a917`), and the counts above are replaced as follows.
- **Row M8.** The table above reads "`\b`, `\B`, `\w`, `\d`, `\s` or a complement in the pattern with a code point past
  ASCII in the subject; `i` with U+0130 or U+0131 in the input; ...". It took 6,849 differences (243 pattern-flag pairs)
  where S and J give the same spans and captures and only the group names differ, the translator's renaming that item
  3 below places outside the row's list; the complements `\W`, `\D` and `\S`, which the row does not name (`\S` as a
  range's end in `[\S-\07B]` among them); and subjects whose only units past ASCII are lone surrogates, emoji or
  U+00A0. `@5` reads the row's list where it bears on the difference: never a difference of the group names alone;
  `\b` or `\B` outside a class, with a letter past ASCII (General_Category L in the arbiter's UCD 17.0.0, a pair read as
  one code point) in the subject from one unit before the earliest start of either side's match to the latest end, or
  anywhere in it where the pattern has a look-around; `i` with U+0130 or U+0131 in the pattern or there; an empty
  `\q{}` in a class under `v`; and in `tr-TR` P5's `I` and `[I]` against `i`, `i` and `[i]` against `I`, each with its
  own subject. **Not settled by the row's text**: "`\w`, `\d` and `\s` beside them" is read as beside `\b` and `\B`
  ("them" is plural; "a non-ASCII letter" is singular), so the `\b` and `\B` limb holds those inputs. *Not taken*:
  "beside non-ASCII letters", which would add to row M8 2,183 of the differences below (171 pattern-flag pairs).
- **Rows M4 and S14**, narrowed and moving nothing in this run: M4's widened predicate reads only the widening's two
  vectors (`(.)` under `gdu`, `[𝌆]` under `gu`), and S14 no longer takes, in S15's place, a bound past
  2^31 - 1 under `i` that the comparison does not show refused without `i` (outside P4). The classifier's docstring
  now quotes the filed revision's clause, "an input whose reduced case differs, in the same kinds of answer or in
  others, is classed as that difference", where it quoted the draft's "an input whose reduced difference is another
  difference is classed as that difference"; the filed clause rewords the draft's, and the classifier reads it as
  written. Its (b') reads any `limit:harness` of arm J as the stop list's stop, which holds only while no case reached
  a backstop, a calibration's time limit or a crash; in this run every one of arm J's 2,648 (b') differences is S's
  stop (`limit:steps` 2,552, `limit:trail` 96), none J's.
- **The classification again** (2026-11-16, 09:12 to 09:13 UTC), by `@5` at `4e8a917` from a `git archive`, over the
  same inputs as `@4`'s above, kept beside `@4`'s with a `hashes.txt` (no retained output written to):
  **`classification.json` `eb1d8dd1d698c8bee86da53b40eda14ecd44679702280ab26c8c3d321a1a8f03`, `classified.tsv.gz`
  `671d4b39ca52fb44ac6e4cc6be36a3b265c0a06a9c405949662b29bdb545a635`**, exit 1. Over the reduction's own comparison,
  `classification.json` `bffc379e8aa7e63a1e541e6639db468a15aa88fd871a2f301c304d5214dabdf8` and `classified.tsv.gz`
  `b71ed577…` (the bytes `@4` wrote), its six differences row S14's as before. Every difference changed only from M8
  (a) to (c), 13,647 of them; no other class or row moved.
- **The counts it replaces.** "(a) 1,875,164 ... M8 46,594" is now **(a) 1,861,517**, M8 **32,947**, the other rows
  as listed; (a') 45, (b) 8,983 and (b') 2,661 are unchanged; "(c) 14,656" is now **(c) 28,303**, every one arm J's.
- **What class (c) holds, replacing item 3** ("An exec both answer where J answers otherwise, outside row M8's list:
  10,864 (3,880 inputs ...)"): items 1 (3,789) and 2 (3) are unchanged, and item 3 is now **24,511** (8,506 inputs and
  1,308 pattern-flag pairs, all P4; 8,014 under the invariant culture, 8,488 under `tr-TR`, 8,009 under `lt-LT`). By
  what differs: the group names alone 10,664 (326 pairs; 3,815 as before and 6,849 from M8); another match 6,717 (513;
  3,285 from M8); a null against a match 3,686 (526; 1,542 from M8); the same match with a capture's participation
  differing 3,444 (145; 1,971 from M8). Where item 3 says "the sentence alone, which would class these 10,864 (a) under
  M8 (... `@4` classes 46,594)", read 24,511 and `@5`'s 32,947; the list of what the candidate reading waits on (its
  item 2) and the list of disagreements read 24,511 for 10,864 likewise.
- **The rule's revision** keeps its rule block, MET line and quote; its prose is corrected (`a30019b`): its draft was
  written in the notes of Broiler.VM's stage, not in Broiler-Platform/Broiler.VM#211; alternative 2 asks only the
  arbiter again; alternative 3 is titled as `@1` read it (either side's stop at a policy bound) and says that class (c)
  still cannot empty under it (item 3 and P3's `O06` are no stops); and for the reduction of 2026-11-15 the
  declaration follows its run.
- What the candidate reading (task 4.2) waits on is otherwise as listed above. Class (c) is not empty, and nothing is
  declared met.

*Dated record, 2026-11-16 (SJL-11.2: the owner's ruling on population P4, filed and applied; bundle `sjl-11-001`'s
[`reference-arms.md`](evidence/sjl-11-001/reference-arms.md)). Written after task 2.13's runs, their classification,
the reduction of 2026-11-15 and the classification by `@5` above were seen. No arm was run again, no rule file was
edited, and nothing is declared met.*
*Corrected 2026-11-16, after review and before this record reached the default branch, with the revision it records
(whose "Corrected before merge" quotes what changed there).* It read "**The 19 patterns, and the property that
selects them.**" and "The revision states the exclusion by three limbs under one of a pattern's flag sets"; "*Choice
taken*: limb (3), which reads the run as the ruling did, so that exactly the 19 the ruling names are excluded. *Not
taken*: (1) and (2) alone, which would exclude 11 patterns the ruling does not name, none of whose differences is
class (c); (1) alone (the drafted option P), 278."; "arm J 6,717 (2,339 in each culture)", where 3 × 2,339 is 7,017;
and, of the brief's "are the 1,269 inputs no prefix reaches", "Those are their inputs no prefix reaches;". The arbiter
reaches no prefix of 2,077 of the 19 patterns' 3,040 execs; the 1,269 are those of them that carry class (c)
differences. The exclusion is now stated as the enumeration by id, the limbs being how the 19 were found. The ids,
the listing and the classification are unchanged.

**The ruling.** The record above put "the population question" to the owner as drafted option P, "not taken". The
question put was whether the 19 P4 patterns, each with a quantifier minimum of 4,294,967,296, should be dropped from the
test population, since the arbiter can never decide them however far the subject is reduced and their 1,269 inputs
(3,789 differences) otherwise keep class (c) from emptying. **The owner answered on 2026-11-16: "You can exclude with
note regarding the 19 patterns".** It is the owner's own decision, not a choice taken under the standing direction of
2026-10-18. The record above's item 1 of "What the candidate reading (task 4.2) now waits on" reads "taking or refusing
option P above": the owner took an exclusion of the 19, worded below; confirming or overturning option R is still the
owner's.

**What it builds**, in the order of the branch's commits (`sjl-11-2-p4`, from `f1816df9`,
Broiler-Platform/Broiler.Regex#21's merge):
- *The rule's revision* (`059958f`):
  [`reference-arms-revised-2026-11-16b.md`](evidence/sjl-11-001/reference-arms-revised-2026-11-16b.md), the next name
  in the directory's convention. It quotes the ruling, says it was written after the runs were seen (weaker evidence
  by `decision-rule.md`'s "Revisions"), quotes `reference-arms.md` whole and option R's clause on the inputs no prefix
  reaches, states the exclusion and lists the 19 by id, and carries the owner's note on them.
- *The classifier `@6` and the listing* (`1012735`): `scripts/reference-arms/classify.py --exclude`, which lists every
  difference of a listed P4 pattern apart as `excluded` (neither class (c) nor an admitted class, the arbiter not
  asked); `scripts/reference-arms/exclusion.py`, whose `select` writes the listing from a population, the arbiter and
  the run's comparison; `scripts/reference-arms/p4-excluded.tsv`, the 19; the script tests (89, from 75) and the
  scripts' README.
- *The records*, with this one: the bundle's README (a dated note) and the CHANGELOG (process only).
- *The corrections after review*, before merge: the revision, this record, the bundle's README, the scripts' README,
  the CHANGELOG and `exclusion.py`'s docstrings, as the note under this record's heading says; one blank line is
  removed from the script tests. No result changed.

**The 19 patterns, enumerated, and the limbs they were found by.** By id: P4-01347, P4-01872, P4-02466, P4-09242,
P4-11265, P4-11714, P4-14530, P4-17689, P4-18962, P4-29456, P4-29657, P4-30238, P4-33179, P4-40469, P4-42576, P4-54314,
P4-58688, P4-63268, P4-65213 (each pattern, its flags and its quantifier are in the revision's table). Each quantifies
with the minimum 4,294,967,296 an atom that can match the empty string (an anchor, `\b`, a look-around, an empty or
optional group), which the arbiter's RepeatMatcher runs that many times before its empty check applies. The revision
states the exclusion as that enumeration, and gives three limbs under one of a pattern's flag sets as how the 19 were
found, each checked over task 2.13's population and the arbiter at its cut-off by `exclusion.py select`:
- (1) that quantifier, read from the pattern under its flags: **278 P4 patterns** (2,326 pattern-flag pairs) and none
  in P1, P2, P3, P5, P6 or P7. "At least 2^32" and "at least the arbiter's cut-off" select the same;
- (1) and (2), the arbiter reaching no prefix of one of its exec inputs, not even the shortest (asked of the 37,216
  exec inputs of the 278 under the flags where (1) holds): **30 patterns**, the 19 and 11 more on whose unreachable
  inputs every side stops (class (b), rows B6 and D10) or all answer alike;
- (1), (2) and (3), the comparison holding a stop against an answer there: **exactly the 19**, under 93 pattern-flag
  pairs, all without `u` or `v`.
*Not settled by the brief's wording*, which asked for a property of the pattern "not by the run's results": no property
found of the pattern and the arbiter alone selects exactly the 19; the narrowest, (1) and (2), selects 30. Limb (3)
reads task 2.13's comparison, as the ruling did, so it is not a property of the pattern: with it the limbs select the
19 ids and nothing else, and state nothing more than the ids. *Choice taken*: the exclusion is the enumeration of the
19 by id, which is what the ruling names, with the three limbs as how they were found. *Offered to the owner*: (1) and
(2), the narrowest property found that holds all 19 (the brief's fallback), which would exclude 11 patterns the ruling
does not name, none of whose differences is class (c). *Not taken*: (1) alone (the drafted option P), 278.
*Choice taken*: an excluded pattern is excluded under every flag set, by acceptance and by every exec, as "the 19 P4
patterns" reads (P4's unit is the pattern drawn). *Not taken*: the 1,269 inputs alone, which moves the same class (c)
differences and leaves the patterns' other 4,059 differences in their classes.

**The base answer, before anything changed.** The classifier `@5` (`f1816df9`'s `classify.py`, from a `git archive`)
over task 2.13's retained comparison (`differences.tsv.gz` `0a8345fd…`), with `--reduction` over the reduction of
2026-11-15 (`reductions.tsv` `b3e05301…`) and the correspondence at `7c65a8fe` (blob `f0f6f080…`), wrote
**`classification.json` `eb1d8dd1d698c8bee86da53b40eda14ecd44679702280ab26c8c3d321a1a8f03` and `classified.tsv.gz`
`671d4b39…`, the retained bytes** (and over the reduction's comparison `bffc379e…` and `b71ed577…`), exit 1.

**The classification again** (2026-11-16), by `@6` at `1012735` from a `git archive`, over the same inputs, with
`--reduction` and `--exclude scripts/reference-arms/p4-excluded.tsv` (SHA-256 `3e2e080b…`), kept outside the
repository beside `@5`'s with a `hashes.txt` (no retained output written to): **`classification.json`
`0c28d1f02b64538cee2513a9adaa998cefbe34431d78a7af672425ae44dbded9`, `classified.tsv.gz`
`baeb11fc31890bb0d46dc80401a06293c1f91d0e23359d0768f0834206fb99ee`**, exit 1:
- **excluded 8,970**: arm J 7,017 (2,339 in each culture) and arm V 1,953, the 19 patterns' every difference;
- **(a) 1,860,854** (was 1,861,517: S12 561 and S8 102 excluded), **(a') 45**, **(b) 4,876** (was 8,983: D10 2,154 and
  B6 1,953 excluded), **(b') 2,250** (was 2,661: 411 excluded);
- **(c) 24,514** (was 28,303: the 3,789 no prefix reaches excluded).
Every difference not of the 19 patterns has `@5`'s class, row and reason byte for byte; every one of theirs changed only
to `excluded`. Over the reduction's own comparison `@6` writes `classification.json`
`f58000d716d5de92d0dfb11eca22d6d9bec95e627ff68df65651216d9566f5f2` and `classified.tsv.gz` `b71ed577…` (`@5`'s bytes:
its six differences are P4-38461's, not excluded). Without `--exclude`, `@6` writes `@5`'s `classified.tsv.gz`
`671d4b39…` byte for byte, and `exclusion.py select` at `1012735` writes the filed listing byte for byte.

**What class (c) still holds: 24,514 differences, every one arm J's, none a defect of S.**
1. **An exec both answer where J answers otherwise, outside row M8's list: 24,511** (P4; 8,014 under the invariant
   culture, 8,488 under `tr-TR`, 8,009 under `lt-LT`), item 3 of the note above, unchanged. Whether row M8 or a row of
   their own holds them is Broiler.VM's to say; until one does they are class (c).
2. **P3's `O06`, 3**, unchanged: no row and no reading of the rule takes it.
The record above's item 1 of class (c), "No prefix the arbiter reaches: 3,789", is now excluded by the ruling, listed
apart and reported with every reading: **a known gap of the reference arms, not a verified agreement** (the revision's
note). On its 1,269 inputs S stops at a ceiling it declares and arm V stops too (at its ceiling, or at its allowance
with S's answer); only arm J answers. The exclusion takes every flag set and input of the 19, so it also takes out of
the judged population 663 class (a) corrections the arbiter confirmed (rows S12 561 and S8 102) and 411 class (b')
differences settled at a prefix it reaches: they stay listed apart, but no longer count as verified corrections or
admitted differences.

**What the candidate reading (task 4.2) now waits on**, replacing the record above's list where it differs:
1. **The owner on option R** (filed as recommended, weaker evidence). The population question is answered by the ruling.
2. **Broiler.VM on the 24,511** (row M8 widened, or rows of their own).
3. **P3's `O06`**, as above.
4. **The run's procedure**: `run.sh` passes neither `--reduction` nor `--exclude`; the reading needs both after its
   comparison, with `reduce.py` run over it.
5. Still owed, as listed above: the two negative controls, the host job (B-1), CI green at the reading's commit (B-3),
   the harness's `Win32Exception`, and the owner's choice of the reading's commit and of the rule commit.

**Where the brief, the sources and this repository differ, read at this record.**
- The brief says the 19 patterns "are the 1,269 inputs no prefix reaches". The 1,269 are only their inputs no prefix
  reaches that carry class (c) differences (the 3,789, all arm J's), the inputs of theirs on which `reduce.py` answered
  `none`. The arbiter reaches no prefix of 2,077 of the 19 patterns' 3,040 execs, under every one of the ten flag sets
  (480 of them under `u`, `v`, `iu` or `iv`); on the other 808 every side stops (656) or no arm differs from S (152).
  The 19 patterns have 3,230 inputs (190 acceptance cases and 3,040 execs), with 8,970 differences, all of which the
  exclusion takes.
- The brief asks that the property select "exactly these 19 in P4 and nothing else in P1 to P5", by "a property of the
  pattern, not by the run's results": limb (1) selects 278, limbs (1) and (2) 30, and only with limb (3), which reads
  the comparison, 19. None selects anything in P1 to P7. So the exclusion is the enumeration by id, and limbs (1) and
  (2), the brief's "narrowest property that holds all 19", are put to the owner.
- The brief's note asks that "S and arm V's answers on them are retained and still reported"; arm J's are too, and every
  difference of the 3,789 is arm J's.
- The question put to the owner counted the 3,789 differences of arm J on the 1,269 inputs; on the same inputs arm V
  has 1,104 more differences (V at its ceiling, S at its own), class (b) under row B6 by `@5`, now excluded with them.

*Dated record, 2026-11-16 (SJL-11.2: the reference arms' classifier reads Broiler.VM's rows M10 to M15 and row B1's
widening, and the arms are classified again; bundle `sjl-11-001`'s
[`reference-arms.md`](evidence/sjl-11-001/reference-arms.md)). Written after task 2.13's runs, their classification by
`@5` and `@6` and the reduction of 2026-11-15 were seen. No arm was run again, no rule file was edited, and nothing is
declared met.*

**What the correspondence adds.** The record above's item 2 of "What the candidate reading (task 4.2) now waits on"
reads "**Broiler.VM on the 24,511** (row M8 widened, or rows of their own)". Broiler.VM answered with rows of their own:
its correspondence at **`44511066bfb38952c12c81557f56a34bd21ee41e`** (the merge of Broiler-Platform/Broiler.VM#212, the
file's blob `94b860cc00b71d2e142473a3685fdd92f1cccee4`), under the owner's decision D-jsexec, adds rows M10 to M15, one
row to a mechanism of arm J's copy of Broiler.JS, gives row M8 a dated note (its "`\w`, `\d` and `\s` beside them" adds
no input of its own, as `@5` read it), and widens row B1 for P3's `O06`. Its note says when each row takes an input and
that "Rows M10 to M15 are read in that order, so an input two of them take is the first's".

**What it builds**, in the order of the branch's commits (`sjl-11-2-m10`, from `304d23d`,
Broiler-Platform/Broiler.Regex#22's merge):
- *The classifier `@7`* (`8748d01`): `scripts/reference-arms/classify.py` reads the correspondence at `44511066`
  (`CORRESPONDENCE`, which `classification.json` names as `rows-read-from`), with one predicate per row, each read from
  the row's text and tried after `@6`'s rows in the correspondence's order; the script tests (97, from 89) and a dated
  note in the scripts' README. `@6`'s rows, the reduction and the exclusion are unchanged. A run of `@7` needs the
  correspondence at `44511066` or later: an earlier one lacks the rows, and the classifier refuses it (exit 2).
- *The records*, with this one: the bundle's README (a dated note) and the CHANGELOG (process only).

**The predicates, each from its row's text.** Every one reads arm J, an `exec` both sides answer and S's answer the
arbiter's (the classifier reaches its rows only then):

| Row | When the row takes an input, in its words | `@7` reads | Takes |
|---|---|---|---:|
| M10 | "Without `u` or `v`, a named group whose name holds `$`, with the `m` flag or an inline modifier group in the pattern, or, under `i`, a character of the name the translator's case expansion rewrites ...; an `exec` both sides answer with the same match and the same captures, where Broiler.JS reports the group under another name" | both match with the same captures; each name S gives that J does not is S's name rewritten at the same capture (the same group): `$` as `(?=$\|\r)` under `m` with no modifier group, as `\z` with one and without `m`, as `(?=[\n\r\u2028\u2029]\|\z)` with one under `m`; under `i`, a character of the name as its Canonicalize class in the arbiter's tables, the character first, where that class holds another member and a non-ASCII one, or as `(?-i:…)` over that class, each member escaped as the translator escapes it, where .NET's invariant lowercase folds the character into another class | 10,664 |
| M11 | "the arbiter's answer to the pattern with each such group read as .NET reads it (the reading in the Broiler.JS cell) is Broiler.JS's, up to row M10's names, and not the text's" | a group, not a look-around, quantified by `?`, `??`, `{0,1}` or `{0,1}?`; the arbiter asked for `(G)?` read `(?:(G)\|)` and `(G)??` read `(?:\|(G))` | 3,414 |
| M12 | the same, "with each such escape read as .NET reads it" | `\p{…}` or `\P{…}` in a class with a General_Category name, read as the BMP code units of that category in the arbiter's UCD or the others; `\a` in a class read U+0007; one reading, every such escape read | 7,771 |
| M13 | the same, "with each such class read as .NET reads it" | a class with `-[` after its first member, read `(?:(?![B])[A])` | 528 |
| M14 | the same, "with each such `\--` read as .NET reads it" | `\-` followed by `-` and a member in a class, read `\-\-` | 363 |
| M15 | "Under `i` without `u` or `v`, under `tr-TR` or `lt-LT`, an input that holds `i` or `I` and neither U+0130 nor U+0131 ..., on which arm J under the invariant culture answers as the text does" | the flag `i`; no difference of run J on the same input in the comparison | 495 |

**Checked against Broiler.VM's prototypes.** Broiler.VM's stage that filed the rows prototyped a predicate for each over
`@5`'s class (c), outside every repository, with counts M10 10,664, M11 3,414, M12 7,771, M13 528, M14 363 and M15
495. `@7` takes exactly each prototype's set of differences, row by row (23,235 in all), and no difference is taken by
two of the six rows when each is tried on every difference. The readings `@7` takes where a row's words admit two (`m`
and `i` from the flags alone, a General_Category name only) take nothing more or less than the wider ones over this run.

**Row B1's widening and `O06`.** The row now names `/^[a]*$/u` over 10,000,001 `a` (`P3-O06`) as a correction of
Broiler.JS and of Broiler.Regex's preview.4. The arbiter cannot reach the input; its reduction to the longest prefix it
reaches shows no difference, and at the input neither side stopped at a bound it declares (S matches, J answers
`null`). Option R reads such an input "without such a stop it stays class (c)", before any row is read, and the
revision of 2026-11-16b changes nothing outside P4's 19 patterns. **So `O06` stays class (c) whichever row names it**,
and `@7` gives the widening no predicate.

**The base answer, before anything changed.** The classifier `@6` (`304d23d`'s `classify.py`, from a `git archive`)
over task 2.13's retained comparison (`differences.tsv.gz` `0a8345fd…`), with `--reduction` over the reduction of
2026-11-15 (`reductions.tsv` `b3e05301…`), `--exclude scripts/reference-arms/p4-excluded.tsv` and the correspondence at
`7c65a8fe` (blob `f0f6f080…`), wrote **`classification.json`
`0c28d1f02b64538cee2513a9adaa998cefbe34431d78a7af672425ae44dbded9` and `classified.tsv.gz` `baeb11fc…`, the retained
bytes**, exit 1. Given the correspondence at `44511066` instead, it wrote the same `classified.tsv.gz` and a
`classification.json` (`8dfc6218…`) that differs only in the correspondence it names.

**The classification again** (2026-11-16), by `@7` at `8748d01` from a `git archive`, over the same inputs with the
correspondence at `44511066`, kept outside the repository in this stage's notes, as `@6`'s run is in its stage's, with a
`hashes.txt` (no retained output written to): **`classification.json` `7d0f71db4c8294b25d7ddec33e97878371a668ba8db540a2d56591633fdf1338`, `classified.tsv.gz`
`6f5f0ee58450a0d8bfea3ba1aaa6279780007bdc4434490b89be37b2a357f1fe`**, exit 1:
- **(a) 1,884,089** (was 1,860,854: rows M10 to M15 take 23,235), **(a') 45**, **(b) 4,876**, **(b') 2,250**,
  **excluded 8,970**, all as under `@6`;
- **(c) 1,279** (was 24,514).
Every difference `@6` classed (a), (a'), (b) or (b'), or excluded, has `@6`'s line byte for byte (1,876,995 lines); only
class (c) differences moved, each to (a) under one of rows M10 to M15. Over the reduction's own comparison `@7` writes
`classification.json` `1445c927…` and `@6`'s `classified.tsv.gz` `b71ed577…`. Without `--exclude`, class (c) is 5,068:
these 1,279 and the 3,789 the exclusion takes.

**What class (c) still holds: 1,279 differences, every one arm J's, none a defect of S.**
1. **An `exec` both answer where J answers otherwise and no row names it: 1,276**, all of population P4, on 426 inputs
   (425 differences under the invariant culture, 426 under `tr-TR`, 425 under `lt-LT`): 810 another match, 361 `null`
   against a match, 105 the same match with a capture's participation or span differing. They are 102 pattern-flag pairs
   of 41 patterns, none under `u` or `v` (17 with no flag, 17 under `d`, 20 under `i`, 19 under `m`, 17 under `s`, 12
   under `y`). Four patterns hold 759 of them: `P4-49430` (225, under no flag, `d`, `m`, `s` and `y`), `P4-53359` (222,
   the same flags), `P4-59886` (219, no flag, `d`, `i`, `m` and `y`) and `P4-55142` (93, every one of those six). 96
   hold a construct of rows M11 to M13 whose reading does not give J's answer (an in-class `\p` or `\P` 81, 51 of them
   under `i`; `\a` 3; `-[` 9, all under `i`; a `?` over a group 3), which the rows' text says stay class (c); 240 more
   hold `-[` after a class's first member, row M13's construct, where the nested class is not the class's last element,
   so the row's reading `(?:(?![B])[A])` is not defined and `@7`'s predicate takes nothing (narrower than the row, not
   wider): `P4-49430` (225, `[^\W--[a]--İ]`, the row's own example) and `P4-36132` (15, `[\w--\D--[^]]`). So 336 hold
   a construct of rows M11 to M13, and 940 hold none of the rows' constructs. Which mechanism moves them is not read
   here.
2. **P3's `O06`, 3**, as above.

**What keeps class (c) from emptying, and so what the candidate reading (task 4.2) would read on the arms today.**
Class (c) is not empty, so by the rule, as filed or revised, the reference arms would read **NOT MET** at a reading
taken on these runs today. Two things keep it so: the 1,276 `exec` answers no row of the correspondence names, which no
classifier change can move until a row names them (Broiler.VM's to say); and `O06`, which no row can move under option
R, only another reading of an input the arbiter cannot reach (the owner's) or an arbiter that reaches it. Nothing is
declared met.

**What the candidate reading (task 4.2) now waits on**, replacing the record above's list where it differs:
1. **The owner on option R** (filed as recommended, weaker evidence).
2. **Broiler.VM on the 1,276** (rows for their mechanisms, or none, in which case they stay class (c)).
3. **P3's `O06`**, as above.
4. **The run's procedure**: `run.sh` passes neither `--reduction` nor `--exclude`; the reading needs both after its
   comparison, with `reduce.py` run over it, and the correspondence at `44511066` or later.
5. Still owed, as listed above: the two negative controls, the host job (B-1), CI green at the reading's commit (B-3),
   the harness's `Win32Exception`, and the owner's choice of the reading's commit and of the rule commit.

**Choices taken, each with its alternative.**
- *Row M10's anchors and letters*: "under `m`" and "under `i`" read as the flags; a character's rewrite as its
  Canonicalize class in the arbiter's tables, written as the translator writes it, the class where it holds another
  member and a non-ASCII one and `(?-i:…)` over it where .NET's invariant lowercase folds the character into another
  class (that lowercase stands as the pinned UCD's simple lowercase with U+0130 kept as itself; .NET's table is not
  read); "the same capture" as the same group. *Not taken*: `m` or `i` added by a modifier group too, or any class
  holding the letter (the prototype's), or the span alone; over this run each takes the same differences.
- *Row M12's reading*: one reading, every such escape the pattern holds read ("each such escape"), the name a
  General_Category value. *Not taken*: each kind alone as well (the prototype's), any name; no difference the rows read
  holds both kinds, and the same differences are taken.
- *Row M15's `i`*: the flag. *Not taken*: `i` added by a modifier group, which takes nothing more here.
- *Row B1*: no predicate, as above. *Not taken*: a predicate after M15, which no input could reach under option R.
- *The arbiter's answers to the readings*: asked of the same arbiter at the same cut-off and cached with its other
  answers. *Not taken*: a second arbiter instance or no cache, which give the same answers more slowly.
- *One test of `@5`'s row M8* now gives the invariant run's difference, so that it reads row M8 alone where row M15
  would otherwise take `I` against `I` in `tr-TR`. *Not taken*: changing its expected rows to M15's.

**Where the brief, the sources and this repository differ, read at this record.**
- The brief says rows M11 to M14 take an input "where the arbiter's answer, with the construct read the .NET way, is
  Broiler.JS's"; the rows add "up to row M10's names, and not the text's". `@7` reads the rows' words.
- The brief says "M10 is checked by its names"; the row also names when each rewrite applies (the `m` flag, a modifier
  group, `i`). `@7` reads those too; over this run they move nothing.
- Broiler.VM's prototype notes name the rows at its branch's `cf4acd97` and `85b16220`; the merge `44511066` differs
  from `85b16220` in the correspondence only in one phrase of the D-jsexec note ("by Broiler.JS's mechanism" became
  "whose mechanism they read"), no row.
- The prototype notes count their table "over the `@5` run's class (c)": 28,303 differences, of which the prototype read
  the 24,514 of arm J that are not the 3,789 no prefix reaches, the set `@6`'s class (c) holds.
- Row M11's "whose body can match the empty string where the quantifier is tried" is not read apart: where the body
  cannot, the reading answers as the text does, and the row takes nothing.

*Note of 2026-11-16, at the review of `@7` (before any judged run; the tag unchanged, nothing declared met).* The record
above is corrected where the review found it wrong or `@7` read wider than its rows; what it read before is quoted here.
- *Row M12* asked "the property escapes and `\a` each read alone, then together", and the table's cell and "Checked
  against Broiler.VM's prototypes" read so ("the property escapes and `\a` read together as well as alone"). Where a
  pattern holds both kinds, a reading with one kind still written as the text has it could take the input, while the
  row reads "with each such escape read as .NET reads it". `@7` now asks one reading, every such escape read.
- *Row M10* read "under `i`, a letter as a class holding it, another member and a non-ASCII one, or as `(?-i:…)`". That
  took `x` as `[xé]` or `(?-i:x)` (the row: "An ASCII letter whose Canonicalize class is ASCII alone, as `x`, is not
  rewritten"), accepted a `(?-i:…)` form arm J never writes (its translator escapes each member, `\xhh` or `\uhhhh`),
  `(?=$|\r)` where a modifier group stands in the pattern (the row names `(?=[\n\r\u2028\u2029]|\z)` there), and
  names swapped between groups of equal spans; and it left out a character of a name that is not a letter, as U+2160.
  `@7` now reads each as the table above says.
- The classification was taken again at the fix (`1fdc1cd`, from a `git archive`, over the same inputs and from the same
  arbiter cache): `classification.json` `7d0f71db…` and `classified.tsv.gz` `6f5f0ee5…`, the same bytes as at
  `8748d01`, and so for the reduction's comparison and the run without `--exclude`. Each row takes the same
  differences, the prototype's sets; no difference holds both kinds of row M12's escapes, and every class rewrite in
  row M10's set is `π` as its Canonicalize class. The script tests are 98 at the fix (one added; "What it builds" says
  97).
- The listing of class (c) read "102 pattern-flag pairs of 42 patterns"; they are of **41** patterns (41 distinct
  pattern ids and texts in `residual-pairs.tsv` and in `classified.tsv.gz`'s class (c)). Commit `7d3dc98`'s message
  says 42 too; it is not amended.
- It read "1,180 hold none of the rows' constructs". That counted a construct only where a row's reading is formed: 240
  of the 1,180 hold `-[` after a class's first member, row M13's construct, where the reading is not defined. 336 hold a
  construct of rows M11 to M13 and 940 none.
- It read "kept outside the repository beside `@6`'s"; `@6`'s run is in its stage's notes and `@7`'s in this stage's.
  The overlap and wider-reading checks the paragraph "Checked against Broiler.VM's prototypes" cites had no retained
  output; the run at the fix keeps them, at `8748d01` and at `1fdc1cd`, with its `run.sh` and arbiter cache in its
  `hashes.txt`. 8748d01's `hashes.txt` is not rewritten.
- "What the candidate reading (task 4.2) now waits on" replaces the record above's items 1 and 4 too, which read
  "1. **The owner on option R** (filed as recommended, weaker evidence). The population question is answered by the
  ruling." and "4. **The run's procedure**: `run.sh` passes neither `--reduction` nor `--exclude`; the reading needs
  both after its comparison, with `reduce.py` run over it."

*Dated record, 2026-11-16 (SJL-11.2: the owner's ruling on P3's `O06` filed, the reference arms' classifier reads
Broiler.VM's rows M16 to M26, and the arms are classified again; bundle `sjl-11-001`'s
[`reference-arms.md`](evidence/sjl-11-001/reference-arms.md)). Written after task 2.13's runs, their classification by
`@5` to `@7` and the reduction of 2026-11-15 were seen. No arm was run again, no existing rule file was edited, and
nothing is declared met.*

**The owner's ruling on `O06`.** The record above's item 3 of "What the candidate reading (task 4.2) now waits on"
reads "**P3's `O06`**, as above". The question put to the owner on 2026-11-16 was: "O06 is /^[a]*$/u matched against
10,000,001 'a'. The library and the Broiler.VM regex engine both match the whole input; Broiler.JS answers null. The
reference checker can't reach that length, but cut down to 999,992 characters, the library, Broiler.JS and the checker
all agree on a full match. Broiler.VM's row B1 names Broiler.JS's cause: an early step-budget exit. The current rule
still keeps it unclassified, because neither side stopped at a declared limit. How should the reference-arms rule read
it?" The options were "Classify by its row" (recommended) and "Keep it unclassified". **The owner chose "Classify by
its row"**, described as: "A dated rule revision lets an unreachable input be classed by the row that names it, but
only if the row gives the mechanism, every other engine agrees with the library, and a stated check shows the
library's answer is right. O06 becomes a Broiler.JS correction (class a, row B1)." That is the owner's own decision,
not one taken under the direction of 2026-10-18. It is filed as
[`reference-arms-revised-2026-11-16c.md`](evidence/sjl-11-001/reference-arms-revised-2026-11-16c.md), the next name in
the directory's convention. That file carries the clause drafted in Broiler.VM's stage notes word for word, as a last
limb of option R's arbiter clause, with its conditions (1) to (3) checked against the ruling's words. It quotes whole
option R's rule block and `reference-arms.md`, and says it was written after the runs were seen, so its decision is
weaker evidence.

**What the correspondence adds.** The record above's item 2 reads "**Broiler.VM on the 1,276** (rows for their
mechanisms, or none, in which case they stay class (c))". Broiler.VM answered with rows of their own. Its
correspondence at **`626e4ed88afe8898724bda669e981cf7cf1b98b2`** (the merge of Broiler-Platform/Broiler.VM#215, the
file's blob `d4605f5cd1461137a3533e816d5bc2275434cce8`), under the owner's decision D-jsrest:
- adds rows M16 to M26, one row to a mechanism of arm J's copy of Broiler.JS, read after M15 in order;
- gives rows M8, M12, M13 and M15 dated notes;
- names one pattern's inputs as no row's: `P4-47050-s-05`.

**What it builds**, in the order of the branch's commits (`sjl-11-2-m16`, from `23deeb63`,
Broiler-Platform/Broiler.Regex#23's merge):
- *The revision* (`e845f6a`), above.
- *The classifier `@8`* (`11e4899`): `scripts/reference-arms/classify.py` reads the correspondence at `626e4ed8`.
  - The noted predicates of rows M8, M12, M13 and M15 replace `@7`'s in place.
  - Rows M16 to M26 come after M15, in the correspondence's order.
  - With `--reduction` it reads the revision of 2026-11-16c.
  - The script tests go from 98 to 117, and the scripts' README gains a dated note.
  - `@7`'s other predicates, the reduction's other outcomes and the exclusion are unchanged.
- *The records*, with this one: the bundle's README (a dated note) and the CHANGELOG (process only).

**The predicates, each from its row's text.** Every one reads arm J, without `u` or `v`, an `exec` both sides answer,
and S's answer the arbiter's (the classifier reaches its rows only then). Where a row says "class", the pattern is
lexed as the text lexes it without `v`: a class ends at its first unescaped `]`, and a `[` inside it is a member. The
counts are the differences `@7` left in class (c) that each now takes:

| Row | When the row takes an input, in its words | `@8` reads | Takes |
|---|---|---|---:|
| M8, by its note | "`\b` and `\B`" are read outside a class "as the text lexes a class without `v`" | row M8's predicate, with `\b` or `\B` found outside a class so lexed (under `v`, as `@7`) | 39 |
| M12, by its note | "the row's reading also writes a `-` right before `\p{…}` or `\P{…}` that follows `\d`, `\D`, `\w`, `\W`, `\s` or `\S` as `\-`" | row M12's reading, with that `-` written `\-` | 39 |
| M13, by its notes | "the nested class, read as .NET reads it, ends at its first `]`, even right after its `[` or `[^`", which the row's later note narrows: "\"even right after its `[` or `[^`\" reads \"even right after its `[^`, under `RegexOptions.ECMAScript`\"" | row M13's reading, a nested class that opens with `[^` ended at a `]` right after it where the flags keep that option (no `s` and no modifier group); a nested `[]`, or one without the option, read as `@7` read it; a `-[` not last still gives no reading | 15 |
| M15, by its note | "as the text does, up to row M10's names" | or run J's answer on the input is S's match with only the names rewritten as row M10 reads them, at the same groups | 1 |
| M16 | "`\c` that no ASCII letter follows, in a pattern `TryRouteToBroiler` hands to ... preview.4 by one of `GapScan`'s shapes", where the arbiter's answer to the reading is Broiler.JS's | a shape in the arbiter's tree of the pattern (a quantifier whose maximum is above 1 over a body that can match both the empty string and a character, or a capturing group or a back-reference inside a look-behind); the reading: each such `\c` as `\\` | 261 |
| M17 | "a group name written with `\u{…}` ..., in a pattern that holds a quantifier whose maximum is above 1 over a body that can match both the empty string and a character" | such a name outside a class, and that shape in the arbiter's tree | 222 |
| M18 | "`\k<…>` whose name is written with `\u{…}` ... and names a group of the pattern" | outside a class, the name's value a group's | 219 |
| M19 | "a legacy octal escape `\0` followed by an octal digit ... in the body of a quantifier whose maximum is above 1, over a body that can match both the empty string and a character" | the escape outside a class, found in that body of the arbiter's tree | 93 |
| M20 | "in a class, a legacy octal escape `\0` and an octal digit right before `\d`" | `\0`, one octal digit and `\d` in a class | 18 |
| M21 | "without `s` and with no modifier group in the pattern, `\S` in a class", where the arbiter's answer to the reading is Broiler.JS's | the reading: `\S` as `\u0000-\u0008\u000E-\u001F!-￿`, a `-` beside it as `\-` | 84 |
| M22 | "a capturing group whose body is empty, `()` or `(?<name>)`, quantified by `+?` or `{1,}?`, in a pattern with more than one alternative" | such a group outside a class, and a `\|` outside every class and group | 90 |
| M23 | "Under `m` ... `^` as a member of a class ... or `$` as a member of a class", where the arbiter's answer to the reading is Broiler.JS's | the flag `m`; the reading: `^` as the characters `(?<=^\|\r)`, `$` as `(?=$\|\r)` | 18 |
| M24 | "under `s` or with a modifier group in the pattern, a back-reference to a group of the pattern" | a back-reference in the arbiter's tree | 57 |
| M25 | "Under `i` ... inside a modifier group that removes `i` ..., a letter the translator expands", where the arbiter's answer to the reading is Broiler.JS's | the flag `i`; a letter outside a class; the reading: the letter as its Canonicalize class | 15 |
| M26 | "Under `i` ... a class, lexed as the text lexes it without `v`, that holds `\W`, `\p{…}` or `\P{…}`" | the flag `i` | 102 |
| B1, by the revision of 2026-11-16c | the row's widening: "`/^[a]*$/u` over 10,000,001 `a` (`P3-O06`)", "the class's run ends at the budget's early exit this row cites (R1)" | the row's one input; condition (2) from every other arm's run; condition (3) by the stated predicate `whole_run_answer` | 3 |

**Checked against Broiler.VM's prototypes.** Broiler.VM's stage that filed the rows prototyped each predicate over
`@7`'s class (c), outside every repository, with the counts above for M8 to M26 and 3 left over.
- `@8` takes exactly each prototype's first-row set, difference for difference (1,273), with 0 mismatches.
- With every predicate tried on every one of the 1,276, each difference is taken by exactly the prototype's rows.
  Only row M12 and row M26 share differences: the 9 of `P4-36184` under `i`, which M12 holds by order.
- Trying the rows that read the arbiter's answer to a reading (M16, M21, M23, M25) before those that read a construct
  gives every difference the same first row as the correspondence's order.

**The notes move nothing `@7` classed.** Every one of `@7`'s 1,900,233 lines outside the 1,276 is the same byte for
byte under `@8`. Tried on every difference `@7` put in rows M8 to M15, each noted predicate still takes its own row's
differences. The noted row M8 takes none of rows M9 to M15's. Later rows also take some of the differences of rows M8
to M15, and order keeps those where they were.

**The base answer, before anything changed.** The classifier `@7` (`23deeb63`'s `classify.py`, from a `git archive`)
over task 2.13's retained comparison (`differences.tsv.gz` `0a8345fd…`), with `--reduction` over the reduction of
2026-11-15 (`reductions.tsv` `b3e05301…`), `--exclude scripts/reference-arms/p4-excluded.tsv` and the correspondence at
`44511066`, wrote **`classification.json` `7d0f71db4c8294b25d7ddec33e97878371a668ba8db540a2d56591633fdf1338` and
`classified.tsv.gz` `6f5f0ee5…`, the retained bytes**, exit 1. Given the correspondence at `626e4ed8` instead, it
wrote the same `classified.tsv.gz` and a `classification.json` (`c7d6e387…`) that differs only in the correspondence
it names.

**The classification again** (2026-11-16), by `@8` at `11e4899` from a `git archive`, over the same inputs with the
correspondence at `626e4ed8`, kept outside the repository in this stage's notes with a `hashes.txt` (no retained
output written to): **`classification.json` `dc77d7c97b0805b7a42d5198af2f8a3c095c361fe8a5285188dff4cc3e1df9be`,
`classified.tsv.gz` `1741d5dc5abc62318e61d98cdaf00160382d3b6c2bde284da0c269f5d063e896`**, exit 1:
- **(a) 1,885,365** (was 1,884,089). The new 1,276 are 94 under rows M8, M12, M13 and M15 by their notes, 1,179 under
  rows M16 to M26, and 3 under row B1.
- **(a') 45**, **(b) 4,876**, **(b') 2,250** and **excluded 8,970**, all as under `@7`.
- **(c) 3** (was 1,279).

Only class (c) differences moved, each to (a). Over the reduction's own comparison `@8` writes `classification.json`
`7c6fb3a7…` and the same `classified.tsv.gz` as `@6` and `@7`, `b71ed577…`. Without `--exclude`, class (c) is 3,792
(`classification.json` `19ea58aa…`, `classified.tsv.gz` `c94499e7…`): these 3 and the 3,789 the exclusion takes.

**What class (c) still holds: 3 differences, arm J's, none a defect of S.** They are `P4-47050-s-05`, one input under
each of arm J's three runs (the invariant culture, `tr-TR` and `lt-LT`): `[ſ-\w]|\ud83d*((?<=s* )|\D+?|[\W&&\s&&[-]])?\b|\f{1(?!)`
under `s`. S and the arbiter answer `match:0,2;0,2`, and arm J `match:0,20;0,20`. No row names it.
Broiler.VM's correspondence says why: written with its `\b` as look-arounds over the ASCII word characters, the
pattern gives the text's answer in all three cultures, so `\b` under `s` is a necessary part of the cause, but "how
.NET's `\b` differs on this input is not read".

**What the candidate reading (task 4.2) would read on the arms today.** Class (c) is not empty, so by the rule, as filed
or as revised, the reference arms would read **NOT MET** at a reading taken on these runs today. One thing keeps class
(c) from emptying: `P4-47050-s-05`, which no classifier change can move until a row names it (Broiler.VM's to say), or
the owner reads it otherwise. Nothing is declared met.

**What the candidate reading (task 4.2) now waits on**, replacing the record above's list where it differs:
1. **The owner on option R** (filed as recommended, weaker evidence). The revision of 2026-11-16c is written as a last
   limb of option R's arbiter clause, and the owner's ruling reads `O06` under it.
2. **Broiler.VM on `P4-47050-s-05`**: a row for its mechanism, or none, in which case its 3 differences stay class (c).
3. **The run's procedure**: `run.sh` passes neither `--reduction` nor `--exclude`. The reading needs both after its
   comparison, with `reduce.py` run over it, and the correspondence at `626e4ed8` or later.
4. Still owed, from the roadmap's own list:
   - the two negative controls of `reference-arms.md`;
   - the host job (B-1), which waits for Broiler.JS's SJL-11.5 branch;
   - CI green at the reading's commit (B-3);
   - the reading's commit, and the rule commit the README names, both the owner's to choose;
   - the harness's `Win32Exception` in `Harness.ProcessorTime`, the arms' owner's to fix before the reading.

**Choices taken, each with its alternative.**
- *The revision's place*: a new dated file, the clause as a last limb of option R's arbiter clause, read with
  `--reduction`. *Not taken*: a flag of its own, or a class apart from (a) (the draft's "also possible" class (b'')).
  The ruling says "class a".
- *Condition (1)*: the row's one input, read by its pattern, flags and subject, and the row's own words checked in
  the correspondence (the classifier refuses one without them). *Not taken*: reading the row's words alone, with no
  input.
- *Condition (2)*: every run of every other arm, from its answer file, holds the case and answers S's answer exactly,
  and at least one does. *Not taken*: reading "no difference of another arm in the comparison", which cannot tell an
  agreeing answer from a case not run.
- *Condition (3)*: the stated predicate reads `^X*$` alone, X one ASCII letter or digit or a class of them, under no
  `i`, `m`, `g` or `y`, over a subject X holds. It must give S's answer at the input and the reduced case's agreed
  answer at the prefix. *Not taken*: a general predicate over any pattern, which would claim answers the classifier
  cannot show.
- *Rows M16 to M26 in the correspondence's order*. *Not taken*: the rows that read a construct after every reading row
  (the correspondence's note on row M24 calls them "after every other row"). Over this run it gives the same rows.
- *Row M16's `GapScan` shapes* read in the arbiter's tree of the pattern. Whether preview.4's own parse succeeds is not
  read apart: the reading gives arm J's answer only where it does. *Not taken*: no shape at all (the prototype's), which
  takes the same 261.
- *Constructs outside a class* (rows M17, M18, M19 and M22), *a name's value* (row M18), *`\p{`/`\P{` with its brace*
  (row M26), *a letter written as itself outside a class* (row M25), *the back-reference from the arbiter's tree*
  (row M24) and *`\r` written as an escape* (row M23). *Not taken*: the prototype's wider or other forms, which take
  the same sets here. The 3 left hold none of those constructs under those rows' flags, so no wider reading takes more.
- *Row M15's note "at the same groups"*, as row M10 reads its names. *Not taken*: the span alone (the prototype's);
  same set.
- *One `@7` test* no longer asserts `@7`'s version and correspondence commit, which a new test asserts for `@8`. *Not
  taken*: leaving it failing, or deleting it.
- *The run kept in this stage's notes*, as `@7`'s is in its stage's. *Not taken*: writing it into `@7`'s folder.

**Where the brief, the sources and this repository differ, read at this record.**
- The brief says `@7`'s predicates "stay unchanged and come first, unless a note revises a row". The notes revise rows
  M8, M12, M13 and M15, whose predicates keep their places in the order. Nothing `@7` classed moved.
- The correspondence orders rows M16 to M26 "after M15, in order", and its note on row M24 says rows M17 to M20, M22,
  M24 and M26 take an input "after every other row". Both orders give the same rows here.
- Row M16's condition includes `TryRouteToBroiler` handing the pattern to preview.4, which needs preview.4's own parse
  to succeed. `@8` reads `GapScan`'s shapes, not that parse. Broiler.VM's probe saw every one of the 261 routed.
- Row B1 gives the mechanism "an inference ..., not traced in preview.4". Condition (1) asks that the row give it,
  which it does; the ruling's question names it as "an early step-budget exit".
- The ruling says "every other engine agrees with the library". The clause says "every other arm that implements the
  operation answers the input as S does". An arm copies an engine, so they are read as one.
- The brief asks `@7` to reproduce `7d0f71db…` byte for byte. It does with the correspondence at `44511066`, the file
  that run read. With the file at `626e4ed8`, only `classification.json`'s correspondence differs.

*Note of 2026-11-16, at the review of `@8` (before any judged run; the tag unchanged, nothing declared met).* The record
above is corrected where the review found `@8` reading wider than its row; what it read before is quoted here.
- *Row M13's notes.* The table's cell read "the nested class, read as .NET reads it, ends at its first `]`, even right
  after its `[` or `[^`" and "row M13's reading, a nested class so ended". The row's later note of that day, at that
  round's review, reads "\"even right after its `[` or `[^`\" reads \"even right after its `[^`, under
  `RegexOptions.ECMAScript`\"" and says "a nested `[]`, or a nested class without that option, is not tested". `@8`
  ended every nested class at a `]` right after its `[` or `[^`, with no flag check, so row M13 would take a difference
  of `[\w--[]]` on `a]`, with or without `s`, where `@7` gave no reading (a probe with the arbiter; no such input is in
  the runs). `@8` now ends a nested class there only where it opens with `[^` and
  the flags keep the option: no `u`, `v` or `s`, and no modifier group in the pattern, as rows M21 and M24 read its
  loss. Otherwise the `]` is a member, as `@7` read it. The scripts' README and the classifier's `@8` paragraph quoted
  the first note's words and are corrected the same way.
- The classification was taken again at the fix (`a9aedb6`, from a `git archive`, over the same inputs and from a copy
  of the run at `11e4899`'s arbiter cache), kept beside that run in this stage's notes with its own `hashes.txt`. Its
  three classifications, their logs, the residual listing, and the prototype and overlap checks are the same bytes as at
  `11e4899` (`classification.json` `dc77d7c9…`, `classified.tsv.gz` `1741d5dc…`). The 15 differences row M13's notes
  take are all `P4-36132`'s, under no flag, `d`, `i`, `m` or `y`, which keep the option, and none holds a nested `[]`.
- The record's header reads "their classification by `@5` to `@7`". It reads "by `@3` to `@7`", as the revision of
  2026-11-16c says: `@3` classified task 2.13's runs as well.

*Dated record, 2026-11-17 (SJL-11.2: the reference arms' classifier reads Broiler.VM's row M27, and the arms are
classified again; bundle `sjl-11-001`'s [`reference-arms.md`](evidence/sjl-11-001/reference-arms.md)). Written after
task 2.13's runs, their classification by `@3` to `@8` and the reduction of 2026-11-15 were seen. No arm was run again,
no rule file was edited, and nothing is declared met.*

**What the correspondence adds.** The record of 2026-11-16 above (the ruling on `O06` and rows M16 to M26) lists, as
item 2 of what the candidate reading waits on, "**Broiler.VM on `P4-47050-s-05`**: a row for its mechanism, or none, in
which case its 3 differences stay class (c)". Broiler.VM answered with a row. Its correspondence at
**`5e90bfe485aad60b3637d9f69e54ed71adcd73af`** (the merge of Broiler-Platform/Broiler.VM#216, the file's blob
`2831290201cca3fb6b524a9be8680755eb566f37`), under the owner's decision D-jsrest:
- adds row M27, a correction of Broiler.JS: without `RegexOptions.ECMAScript`, .NET makes a loop over `\D`, or over a
  negated class of ASCII word characters, atomic before `\b`, and greedy where it was lazy;
- says it is "read after row M26, so an input row M8 takes by its list stays row M8's";
- reads the mechanism in arm J's copy and in .NET's source at dotnet/runtime `v10.0.15`, checked by runs outside
  either repository.

**What it builds**, in the order of the branch's commits (`sjl-11-2-m27`, from `9dd15fc4`,
Broiler-Platform/Broiler.Regex#24's merge):
- *The classifier `@9`* (`d1a0e81`): `scripts/reference-arms/classify.py` reads the correspondence at `5e90bfe4`.
  - Row M27's predicate comes after M26.
  - The script tests go from 117 to 125, and the scripts' README gains the row and a dated note.
  - `@8`'s predicates, the reduction, the revision of 2026-11-16c and the exclusion are unchanged.
- *A group that does not capture* (`0ecf6dc`): the arbiter's tree writes `(?:…)` as a modifier group that adds and
  removes no flag, so `d1a0e81` reached the loop only through a capturing group, as the prototype did. The predicate
  now reads both as the row's "groups (capturing or not ...)".
- *At review* (`6e87fd8`): a script test pins a loop inside a look-behind (below), so the script tests are 126; the
  scripts' README row M27 and its dated note say what a group is. `classify.py` is unchanged.
- *The records*, with this one: the bundle's README (a dated note) and the CHANGELOG (process only).

**The predicate, from the row's text.** It started from the predicate Broiler.VM's stage prototyped over `@8`, and is
read no wider than the row:

| The row's words | `@9` reads (`loop_made_atomic_before_boundary`) |
|---|---|
| "Without `u` or `v`, under `s` or with a modifier group in the pattern (one the text lexes, as rows M21 and M24 read it)" | arm J; neither `u` nor `v`; the flag `s`, or `modifier_group_lexed` |
| "a quantifier with a minimum of 1 or more and no maximum (`+` or `{n,}`, lazy or not) over `\D`, or over a negated class, lexed as the text lexes it without `v`, whose members are ASCII letters, digits, `_`, `\d` or ranges within `0-9`, `A-Z` or `a-z`" | in the arbiter's tree (`Context.parse`), a quantifier with that minimum and no maximum over a `\D`, or over a negated class every member of which is such a character, such a range or `\d` |
| "the quantifier is a term of an alternative, or, through groups (capturing or not; not a look-around or a modifier group) and quantifiers whose maximum is 1, the last term of an alternative of such a term" | the loop ends a term, so reached |
| "and the term after it in that alternative is `\b`, or a group or positive look-ahead of one alternative, or a quantifier with a minimum of 1 or more, that begins with `\b`" | the next term begins with `\b`, so read; a group is a group, capturing or not, and not a modifier group that adds or removes a flag |
| "an `exec` both sides answer, where Broiler.JS answers otherwise and the arbiter's answer to the pattern with each such quantifier read as .NET runs it (the reading in the Broiler.JS cell) is Broiler.JS's, up to row M10's names, and not the text's" | `answered_as_read` over the reading: each such loop written greedy and followed by `(?!X)`, X its atom (`\D+?` as `\D+(?!\D)`); the tree's loops are paired with the text's one for one, and where they do not pair there is no reading and the row takes nothing |
| "read after row M26" | the last of `ROWS` |

**Checked against Broiler.VM's prototype.** Over every one of arm J's 1,896,055 differences in task 2.13's comparison,
`@9`'s predicate, at `d1a0e81` and at `0ecf6dc`, fires on the same 51 as the prototype, gives the same reading on each,
and takes the same 48: the 3 `@8` left in class (c), and 45 `@8` puts in row M8. In the classifier's order, row M8 keeps
those 45, so row M27 takes the 3 and moves nothing else. Broiler.VM's notes inferred that in-order take and did not run
it. This run shows it.

**The base answer, before anything changed.** The classifier `@8` (`9dd15fc4`'s `classify.py`, from a `git archive`)
over task 2.13's retained comparison (`differences.tsv.gz` `0a8345fd…`), with `--reduction` over the reduction of
2026-11-15 (`reductions.tsv` `b3e05301…`), `--exclude scripts/reference-arms/p4-excluded.tsv` and the correspondence at
`626e4ed8`, wrote **`classification.json` `dc77d7c97b0805b7a42d5198af2f8a3c095c361fe8a5285188dff4cc3e1df9be` and
`classified.tsv.gz` `1741d5dc5abc62318e61d98cdaf00160382d3b6c2bde284da0c269f5d063e896`, the retained bytes**, and the
same log, exit 1. Given the correspondence at `5e90bfe4` instead, it wrote the same `classified.tsv.gz`, and a
`classification.json` (`28b83f7f…`) that differs only in the correspondence it names.

**The classification again** (2026-11-17), by `@9` at `d1a0e81` from a `git archive`, over the same inputs with the
correspondence at `5e90bfe4`, from a copy of the `@8` run's arbiter cache. It is kept outside the repository, beside the
`@8` run in the stages' notes, with a `hashes.txt`; no retained output was written to. It wrote
**`classification.json` `d6ca3a14537b1413ca39c40d49fa510a21ac9185bb99cd10bbc145d2fe7e5a8d` and `classified.tsv.gz`
`5dd1a9607d58d4b5515ee1721d24a8b7bfddb22b2cc1e11fc66c1870613ab652`**, exit 0:

| Class | `@8` | `@9` |
|---|---:|---:|
| (a) | 1,885,365 | 1,885,368 |
| (a') | 45 | 45 |
| (b) | 4,876 | 4,876 |
| (b') | 2,250 | 2,250 |
| excluded | 8,970 | 8,970 |
| **(c)** | **3** | **0** |

- Row M27 takes `P4-47050-s-05` in J, `J.tr-TR` and `J.lt-LT`, the 3 `@8` left in class (c), and nothing else.
- The other 1,901,506 lines are the same byte for byte as `@8`'s.
- Over the reduction's own comparison, `@9` writes the same `classified.tsv.gz` as `@6` to `@8`, `b71ed577…`; its
  `classification.json` (`e485c93e…`) differs from `@8`'s only in the version and the correspondence it names.
- Taken again at `0ecf6dc`, over the same inputs and from the same copy of the cache, the three classifications,
  their logs and the checks against `@8` and the prototype are the same bytes as at `d1a0e81`.
- Without `--exclude`, class (c) is 3,789 (`classification.json` `366fd308…`, `classified.tsv.gz` `65905687…`). All
  3,789 are among the 8,970 differences the exclusion takes, and no other difference is in class (c). Without
  `--exclude` the 8,970 are 3,789 (c), 4,107 (b), 663 (a) and 411 (b').
- `classification.json`'s `counts` names only the classes that hold a difference, so with (c) empty it has no `"c"`
  key, rather than `"c": 0`. The exit code reads the same: 0 where no (c) is counted.

**Class (c) is empty, for the first time.** On task 2.13's runs, with `--reduction` and `--exclude`, every difference is
in an admitted class or excluded, and the classifier exits 0. This is not a reading, and nothing is declared met:
- The run was taken on the retained runs, after they were seen. It is not the candidate reading at its commit.
- It reads the rule as revised: option R and its revision of 2026-11-16c (the reduction's (b') and row B1's `O06`),
  and the owner's ruling on P4 (the exclusion). Option R waits on the owner's confirmation.

**What the candidate reading (task 4.2) still waits on**, replacing the record of 2026-11-16 above's list where it
differs (its item 2, Broiler.VM on `P4-47050-s-05`, is answered by row M27):
1. **The owner on option R**, filed as recommended, so weaker evidence. The revision of 2026-11-16c is written as a
   last limb of option R's arbiter clause.
2. **The run's procedure**: `run.sh` passes neither `--reduction` nor `--exclude`. The reading needs both after its
   comparison, with `reduce.py` run over it, and the correspondence at `5e90bfe4` or later.
3. **The two negative controls** of `reference-arms.md`.
4. **The host job** (B-1), which waits for Broiler.JS's SJL-11.5 branch.
5. **CI green at the reading's commit** (B-3).
6. **The reading's commit**, and the rule commit the README names, both the owner's to choose.
7. **The harness's `Win32Exception`** in `Harness.ProcessorTime`, the arms' owner's to fix before the reading.

**Choices taken, each with its alternative.**
- *A group that begins with `\b`* is a group, capturing or not, as Broiler.VM's predicate reads it ("a `Group`, or a
  positive look-ahead, of one alternative"). *Not taken*: a modifier group that adds or removes a flag there too, which
  the row's words "a group" do not exclude. No input of these runs turns on it.
- *No reading where the tree's loops and the text's do not pair one for one* (a class member written as an escape, as
  `[^\x30]`), so the row takes nothing there. *Not taken*: reading such members as the characters they name, which
  would need a writer of the reading from the tree. No input of these runs turns on it.
- *A loop before `\b` inside a look-behind is named and read*, as the row's text reads it: "the quantifier is a term
  of an alternative" makes no exception for the alternatives of a look-behind, so `(?<=\D+?\b)x` reads
  `(?<=\D+(?!\D)\b)x`. .NET makes no such loop atomic: at dotnet/runtime `v10.0.15`, `FindAndMakeLoopsAtomic`
  stops at a node under `RegexOptions.RightToLeft`, as a look-behind's contents are, and `CanBeMadeAtomic` refuses where
  the options differ. Only the arbiter's answer to the reading (`answered_as_read`) keeps the row from taking such an
  input. *Not taken*: reading the row narrower than its text. No input of these runs turns on it: the construct fires
  on the 51 inputs of `P4-47050` and `P4-63913`, whose one look-behind, `(?<=s* )`, holds no such loop. A script test
  pins the behaviour. Unlike the two choices above, which read the row no wider than its words, here the row's words
  are wider than .NET; it is to be raised with Broiler.VM, so that the row can say "not inside a look-behind".
- *The run kept in this stage's notes*, beside `@8`'s in its stage's, as each run before it. *Not taken*: writing it
  into `@8`'s folder.
- *One `@8` test* no longer asserts `@8`'s version and correspondence commit, which a new test asserts for `@9`. *Not
  taken*: leaving it failing, or deleting it.

**Where the brief and the sources differ, read at this record.**
- The brief says Broiler.VM's `proto.tsv` and `exp3.tsv` "hold the prototype's take: in the classifier's order, exactly
  the 3 class (c) differences". They hold the 51 differences the construct fires on alone. Broiler.VM's notes infer the
  in-order take of 3 and say it was not run. `@9`'s run gives it.
- Broiler.VM's predicate reads "for a `Group` (capturing or not)", and the row "through groups (capturing or not; not
  a look-around or a modifier group)". The arbiter's tree writes `(?:…)` as a modifier group with no flag, not as a
  `Group`, so the prototype, and `@9` at `d1a0e81`, read only capturing groups. `0ecf6dc` reads both. The take over
  these runs is the same.
- The brief asks `@8` to reproduce `dc77d7c9…` byte for byte. It does with the correspondence at `626e4ed8`, the file
  that run read. With the file at `5e90bfe4`, only `classification.json`'s correspondence differs.
- The brief's list of what task 4.2 waits on names "the reading's commit". The roadmap's list names the rule commit
  beside it, and this record keeps both.

*Dated record, 2026-11-17 (SJL-11.2: the owner confirms option R; bundle `sjl-11-001`'s
[`reference-arms-revised-2026-11-16.md`](evidence/sjl-11-001/reference-arms-revised-2026-11-16.md)). No rule file was
added or edited, no arm was run, and nothing is declared met.*

**The question put to the owner**, verbatim: "With the classifier reading row M27 (Broiler.Regex#25), no unclassified
difference (class c) remains. One outstanding item for the candidate reading is option R, the reference-arms rule
revision I filed on 2026-11-16 as the recommended choice. It puts an input the reference checker can't reach into class
(b′) "beyond the arbiter" when three things hold: cut down to a length the checker can reach, the library and the
other engine agree; the library's answer there is the checker's; and at full length one side stopped at a limit it
declares. That covers 2,250 differences, and your O06 ruling builds on it. Do you confirm option R?" The options were
"Confirm option R" (recommended) and "Overturn option R".

**The owner chose "Confirm option R"** on 2026-11-17, described as: "Recorded as your own decision. The 2,250 stay in
class (b′), and the candidate reading no longer waits on this."

**That is the owner's own decision**, no longer a choice taken under the standing direction of 2026-10-18 ("Please do
all recommended decisions and skip waiting for review."), as option R's file says it was filed. What it settles:
- Item 1 of the list above, "**The owner on option R**, filed as recommended, so weaker evidence", is answered. The
  revision of 2026-11-16c, written as a last limb of option R's arbiter clause, and the owner's ruling on `O06` under it
  stand on a confirmed clause.
- The 2,250 differences `@9` holds in class (b') over task 2.13's runs stay there.
- Option R's file, `reference-arms.md` and the revisions of 2026-11-16b and 16c are not edited. The confirmation changes
  no clause, so no dated rule file is added; the bundle's README records it by a dated note, and option R's sentence "It
  is not the owner's own decision: the owner may overturn it" is read, from this date, as overtaken by the ruling.
- Option R stays weaker evidence: it was written after task 2.13's runs, their classification and the reduction were
  seen, and the confirmation does not change that.

**Not settled by it**: items 2 to 7 of the list above, the run's procedure (`run.sh` passes neither `--reduction` nor
`--exclude`), the two negative controls, the host job, CI at the reading's commit, the reading's commit and the rule
commit, and the harness's `Win32Exception`.

*Dated record, 2026-11-17 (SJL-11.2: task 4.2's preparations, the run's procedure, the two negative controls and the
harness's `Win32Exception`; bundle `sjl-11-001`'s [`reference-arms.md`](evidence/sjl-11-001/reference-arms.md) and its
revisions). Written after task 2.13's runs, their classification by `@3` to `@9` and the reduction of 2026-11-15 were
seen. No rule file was edited, no library source changed, the arms were run over P1 to P3 only (and a copy of arm V,
as its negative control, over the retained P4 population; corrected at review), and nothing is declared met.*

**What it builds**, in the order of the branch's commits (`sjl-11-2-42prep`, from `fdf8c52c`,
Broiler-Platform/Broiler.Regex#25's merge):
- *The owner's confirmation of option R* (`bfdfd57`): the record above, and a dated note in the bundle's README.
- *The harness* (`dbc476d`): `Harness.ProcessorTime` (`tests/ReferenceArms/Common/Harness.cs`) reads
  `Win32Exception` as the exit it is, as it read `InvalidOperationException`: zero processor time. Test code only.
- *The run's procedure and the negative controls* (`cc16180`), in `scripts/reference-arms/run.sh`, which
  `run-reference-arms.sh` runs from the reading commit's tree:
  - after the comparison, three classifications, each logged with its command: as the rule was filed
    (`OUT/comparison`, as before); the declared reduction over it (`OUT/reduction`: `reduce.py prefixes`, the arms over
    the reduced cases with the stop list, `compare.py`, `classify.py --exclude`, `reduce.py read`); and `classify.py
    --reduction OUT/reduction --exclude scripts/reference-arms/p4-excluded.tsv` (`OUT/classification`), the one the
    rule as revised reads, logged in `OUT/logs/classify-revised.log`. When it finds class (c) the script says so on
    stderr and exits 0, as it always has (corrected at review: this item read "whose exit the script reports"). The
    three share one arbiter cache;
  - `--classify-only` runs the three over an earlier run's comparison. It reads an earlier run's reduction only where
    `reduce.py check` (new; it writes nothing) finds it lists the inputs that comparison leaves unreachable;
  - after the fast-path differential, the two negative controls, with `scripts/reference-arms/controls.py` (new): a
    copy of arm V over P4 and one of arm J over P1, each made as `tests/ReferenceArms/README.md` says (the
    `NEGATIVE-CONTROL` line given the case, nothing else), built, run once and removed. The class's runs with the
    copy's answers (*control*) and with the arm's own (*revert*) are compared and classified as above, and the
    control's comparison once more against the revert's run files (*blind*: no control case named). The check holds the
    control reported unclassified when the copy changed the case's answer alone, the comparison reports the case and
    nothing else changes, and the classifier lists it unclassified and every other line as the revert's; since the
    review (`ec8ca90`, below), also when the blind classification gives the case class (c) and the revert's
    classification is the run's for the class, line for line. Otherwise the run fails. The revert is the run's own
    answers, linked, not an arm run again. A control whose class a partial run lacks is not taken, and its log says
    so. `--controls-only` takes them over an earlier run;
  - `run-reference-arms.sh` lists the new directories' files in `digests.sha256`;
  - the script tests go from 126 to 142. `classify.py` is unchanged, `@9`.
- *The records*, with this one: a dated note in the bundle's README and the CHANGELOG (process only).
- *At review* (`ec8ca90`): the review found that the check could pass a control vacuously. The classifier lists a
  case the run file names as unclassified before it reads anything else, so the first four conditions held whatever it
  would make of the changed answer, and the blind classification, which shows the change failing, was recorded and
  not required. `controls.py check` now requires it (condition 5: the blind classification is the revert's with the
  case's line more, in class (c), and its counts the revert's with one more in (c)) and reads the run's
  classification (`--main`; condition 6: the revert's classification is the run's restricted to the class, line for
  line). `run.sh`'s last line names the controls taken; `run-reference-arms.sh`'s header and last line name
  `run/logs/classify-revised.log`, not `classify.log` (which holds the classification as filed, exit 1 with 6,459 in
  class (c) over task 2.13's runs), and the controls taken; `reduce.py check` also compares the cut-off. The script
  tests go to 146: four new, and two that gained assertions (its message says "six cases"). `classify.py` is
  unchanged. This record is corrected in place at the same review, in the commit after it.

**The base answers, before anything changed** (the stage's notes keep them):
- `@9` at `fdf8c52` (a `git archive`) over task 2.13's retained comparison (`differences.tsv.gz` `0a8345fd…`), with
  `--reduction` over the reduction of 2026-11-15 (`reductions.tsv` `b3e05301…`), `--exclude` and the correspondence at
  `5e90bfe4`, wrote **`classification.json` `d6ca3a14…` and `classified.tsv.gz` `5dd1a960…`**, exit 0, the `@9` run's
  bytes.
- `run-reference-arms.sh` at `fdf8c52` over P1, P2 and P3 (the inputs of the record of 2026-11-15, the correspondence
  at `5e90bfe4`). **Its first try stopped in arm J**: an unhandled `Win32Exception` ("Unable to retrieve the specified
  information about the process or thread. It may have exited or may be privileged.") from `Harness.ProcessorTime`
  (`Harness.cs:236`), called from the supervisor's reading thread (`:284`). The second try passed in 88 seconds; its
  classification, as filed, holds 4 differences in class (c): P3's `O06` in J's three cultures and P1's
  `budget/k-catastrophic/0001` in V.

**1. The harness's `Win32Exception`.** A worker reaped between .NET's check that it runs and its read of
`/proc/PID/stat` throws `Win32Exception`, not `InvalidOperationException`.
- *A reproduction* outside the repository links `Common/Harness.cs` and calls `ProcessorTime` in a tight loop on each
  of 3,000 workers (`/bin/true`, 4 at a time) until it exits. With `fdf8c52`'s harness, 1,217 of the 3,000 threw
  `Win32Exception` out of `ProcessorTime`. With `dbc476d`'s, none did. Each was built behind the machine's idle gate,
  with no warning.
- *The arms' outputs are unchanged*: `run-reference-arms.sh` at `cc16180` over P1 to P3 (172 seconds, exit 0) lists
  every file the base's run lists. The base's run holds 63 files; leaving out `logs/` (14), `runs/timing/` (20) and
  `environment.txt`, which hold the commands, the processor and wall-clock times and the machine, and differ from run
  to run, 28 remain. Of those 28, 25 are the same bytes, the 15 answer files and the classification as filed among
  them. `population/manifest.json`, `runs/run.S.json` and `comparison/summary.json` differ only where they name the
  commit. Of the 20 timing files, 8 differ: the five run files and P3's three of arm J, where the times differ and
  the head's `P3.J.tr-TR.tsv` holds one line more, `P3-O06` `slow`, the worker's wall-clock event (`Harness.cs`),
  which no answer reads. (The 28 were not said to leave these out; added at review.)

**2. The run's procedure, reproducing the `@9` classification.** `run.sh --classify-only` at `cc16180`, from a clean
detached worktree, over task 2.13's retained run (its population and runs read through symbolic links, its
`differences.tsv.gz` copied) and a copy of the retained reduction, with the correspondence at `5e90bfe4` and an empty
arbiter cache. No arm was run. It exited 0 after 2,380 seconds:
- *As filed* (`OUT/comparison`): a 1,886,022, a' 45, b 8,983, **c 6,459**: the 6,459 differences over 2,166 inputs the
  record of 2026-11-15 reduced. `reduce.py check` found the retained reduction to be that comparison's. That check
  compares the inputs left unreachable and the manifest's counts (and, since the review, its cut-off), not the
  reduction's answers: the retained reduction's arms were built at `68caeeb` (its run files say so), task 2.13's at
  `07d7825`. So `reduce.py prefixes` and the arms over the reduced cases ran in this stage only over P1 to P3, on 2
  inputs (below); the full-scale path is first exercised at the reading (added at review).
- *The reduction's own comparison* (`--exclude`): `classification.json` `e485c93e…` and `classified.tsv.gz`
  `b71ed577…`, the `@9` run's bytes. Its `outcomes.json` is the retained one; `outcomes.tsv` differs in the reduced
  cases' class and row columns, which the retained one took from `@3`.
- *As revised* (`OUT/classification`): **`classification.json` `d6ca3a14537b1413ca39c40d49fa510a21ac9185bb99cd10bbc145d2fe7e5a8d`
  and `classified.tsv.gz` `5dd1a9607d58d4b5515ee1721d24a8b7bfddb22b2cc1e11fc66c1870613ab652`, the `@9` classification
  byte for byte**: a 1,885,368, a' 45, b 4,876, b' 2,250, excluded 8,970, no (c).
- *The full procedure over P1 to P3*, in `run-reference-arms.sh`'s run at `cc16180` above, which had to run the arms:
  the reduction took the 2 inputs the classification as filed leaves unreachable there and reduced them to the
  prefixes the retained reduction lists (`budget/k-catastrophic/0001` at 17 code units, `O06` at 999,992). The
  classification as revised holds a 1,003, a' 9, b 15, b' 1 and no (c), `O06` in row B1 by the revision of
  2026-11-16c.

**3. The two negative controls.** `run.sh --controls-only` at `cc16180` over the same retained run (540 seconds,
exit 0, behind the idle gate), and arm J's again in the P1-to-P3 run above, with the same bytes:

| | V over P4, `P4-01234-u-05` | J over P1, `P1-regexp:matching:m1-notes:0001` |
|---|---|---|
| The copy's answers against the arm's | one line: `match:0,1` for `match:0,0` | one line: `match:0,2` for `match:0,1` |
| The comparison | the revert's and the case's line | the revert's and the case's line |
| *Control* (the case named) | **unclassified** 1; a 1,883,909, b 4,861, b' 2,249, excluded 8,970; exit 0 | **unclassified** 1; a 440, a' 9, b 15, b' 1; exit 0 |
| *Blind* (no case named) | **c 1**, the case, "arm V is wrong by the arbiter and no row of the correspondence names the input"; exit 1 | **c 1**, the case, the same reason for arm J; exit 1 |
| *Revert* (the arm's own answers) | no difference at the case; no (c); exit 0 | no difference at the case; no (c); exit 0 |
| `control.json` | reported unclassified | reported unclassified |

Each revert's classification is the run's classification as revised restricted to the class, line for line
(1,899,989 lines for P4, 465 for P1). Each copy was removed after its run, and nothing of them is retained in the
repository. A one-result change is caught: named as the control, it is listed unclassified, as the rule asks; unnamed,
it is class (c) and the classification exits 1; reverted, the classification passes.

*At review* (`ec8ca90`): `run.sh --controls-only` at `ec8ca90`, from a clean detached worktree, over the same retained
run and classification (557 seconds, exit 0, behind the idle gate) wrote every file of both controls but
`control.json` byte for byte as at `cc16180`, and the check, now with conditions 5 and 6, held both reported
unclassified (blind: c 1, exit 1; revert: the run's classification for the class, no (c)). Its last line reads
"negative controls taken: V J". The same check fails a copy of the J control whose blind classification was rewritten
to class (a), row M1 (the review's reproduction, which the check at `cc16180` passed), and fails the J control read
against the classification as filed in place of the run's (condition 6).

**4. The reading's commit.** `0.1.0-preview.6` was published from `df5cfc2` (2026-11-15), and the library is unchanged
since: `Broiler.Regex/`, `Directory.Build.props`, `Directory.Packages.props`, `NuGet.config` and `eng/` are the same
at `df5cfc2` and at this record's commit. Under the owner's direction of 2026-10-18 ("Please do all recommended
decisions and skip waiting for review."), recorded as the recommended reading, the owner's to overturn:
- *Recommended*: take the reading at **`df5cfc2`**, the published bits, with the scripts, the CI jobs and the
  classifier of a later commit run against it. `df5cfc2` itself holds neither script, no `test262-library` job and the
  classifier `@3`. As written, `run-reference-arms.sh` and `run-library-evidence.sh` run one commit's tree, so the
  reading would state how the later commit's scripts read `df5cfc2`'s library (for example, at the later commit with
  the library's tree checked equal to `df5cfc2`'s), and CI's B-3 would be read at the later commit for the same tree.
- *Alternative*: read a later commit, whose own scripts and jobs are in its tree, and publish it as
  `0.1.0-preview.7`. That needs the owner's publish run and a version step, and gives a package of the same library
  source under a new version.

The rule commit is still the README's question (its note of 2026-11-15): `fca7a6d` or `e568b88`.

**What the candidate reading (task 4.2) still waits on**, replacing the record of row M27's list:
1. **The host job** (B-1), which needs Broiler.JS's SJL-11.5 branch (decision D35).
2. **CI green at the reading's commit** (B-3).
3. **The reading's commit and the rule commit**: the recommendation above, the owner's to overturn, and the rule commit
   the README names.
4. **The reading itself**: both scripts at the reading's commit, from a clean tree, on an idle machine, with both
   negative controls taken in the run.

Answered since that list: option R (the owner's confirmation above), the run's procedure, the two negative controls
and the harness's `Win32Exception` (items 2, 3 and 1 above; corrected at review, which read "2, 1 and 3").

**Choices taken, each with its alternative.**
- *The confirmation recorded by a dated note* in the README and this roadmap, no rule file added. *Not taken*: a short
  dated rule file recording it, which the bundle's "Revisions" rule asks only of a clause revised.
- *Three classifications, each in its own directory*: the one as filed kept in `OUT/comparison`, as before. *Not
  taken*: writing only the classification as revised, which leaves the reduction's input unrecorded.
- *`--classify-only` reads an earlier reduction, checked*, and runs one where there is none. *Not taken*: running the
  reduction every time, which runs the arms.
- *One arbiter cache for the three classifications and the controls'*. *Not taken*: none, which asks the arbiter for
  every answer twice (about 40 minutes each over task 2.13's run).
- *The reduction's comparison classified with `--exclude`*, as the records of `@6` to `@9` did. *Not taken*: as filed,
  as the retained one was by `@3`. The classification as revised does not read it.
- *The controls in `run.sh`*, after every full run, with task 2.13's cases. *Not taken*: a script apart, or new cases.
- *A control not reported unclassified fails the run.* *Not taken*: recording it only; the rule's MET needs both
  reported unclassified.
- *The blind classification required in class (c)* (at review; until then it was recorded, not a condition). *Not
  taken*: recording it only, which the review showed lets a control pass whatever the classifier makes of the
  change, since a named case is listed unclassified before anything else is read.
- *The revert held to the run's classification for the class* (at review). *Not taken*: requiring no (c) in the
  revert, which would fail the controls whenever the run's classification has (c) in P1 or P4, a finding the
  classification already reports.
- *The copy made of the files the arm builds from*, under the run's `OUT`. *Not taken*: a scratch worktree, as task
  2.13 did, which needs a git checkout of the run's commit.
- *The harness reads `Win32Exception` as the exit*. *Not taken*: checking `HasExited` first, which leaves the race; or
  catching every exception, which would hide a fault of the harness.
- *The fix shown by a reproduction outside the repository*. *Not taken*: a committed test, which the arms, executables
  with no test project, have no place for.
- *The recommended reading at `df5cfc2`*, under the standing direction. *Not taken*: a later commit published as
  `0.1.0-preview.7`.

**Where the brief and the sources differ, read at this record.**
- The brief asks that each control "make the classification fail (class (c) non-empty, or the run's exit non-zero)
  and then pass when reverted". `reference-arms.md` asks that "the comparison reports each as a difference and does
  not classify it", and its MET that both be "reported unclassified": named as the control, the case is listed
  unclassified and the classification exits 0. The scripts keep the rule's reading. The blind classification shows
  the failure the brief names (class (c) 1, exit 1), and the revert the pass; since the review the check requires
  both.
- The brief says `run-reference-arms.sh` "runs `reduce.py` and passes `--reduction` and `--exclude`". It runs the
  commit's `scripts/reference-arms/run.sh`, which does; `run-reference-arms.sh` changes only its header and its digest
  listing.
- The brief says the arms are not re-run unless the script must. The reproduction ran none. The harness's proof needs
  P1 to P3, and the procedure's full path runs the arms over the reduced cases, so the P1-to-P3 run did both. The V
  control is over P4 by the rule: its copy, and nothing else, was run over the retained P4 population.
- The brief's recommended reading runs "the scripts and classifier of a later commit" against `df5cfc2`. Neither script
  can today: each runs one commit's tree, and `df5cfc2` has no `test262-library` job for B-3 (item 4 above).

*Dated record, 2026-11-17 (SJL-11.2: task 2.10's host job, bundle `sjl-11-001`'s check B-1, against Broiler.JS's
SJL-11.5 branch; decisions D14 and D35). No rule file was edited, no library source changed, and nothing is declared
met. The job has not run in CI: this branch opens no pull request.*

**The Broiler.JS commit.** Decision D14 reads: the host job "checks out Broiler.JS at a retained commit named by a dated
record in this file", and "The retained commit is the branch of SJL-11.5 until it merges, then Broiler.JS's default
branch; no commit is named yet". This record names it: **`f690836dba19ded358aacc4b6edb9c8107a53ccf`**, the head of
Broiler.JS's branch `sjl-11-5` on 2026-11-17 (`git ls-remote`), ten commits on the first-parent line from `6fda6ca4`
(Broiler-Platform/Broiler.JS#1119's merge), where it leaves Broiler.JS's `main`, and not merged. It routes every regular
expression through Broiler.Regex and pins `0.1.0-preview.6` (its `Directory.Packages.props`). The record of 2026-11-15
above says the shell "cannot take `0.1.0-preview.6` without SJL-11.5's adapter", and that check B-1 "waits for
Broiler.JS's SJL-11.5 branch"; the branch now exists (decision D35 asked for it early). When it merges, D14's retained
commit becomes Broiler.JS's default branch, and a later dated record names that commit and moves the job's pin.

**What it builds**, in the order of the branch's commits (`sjl-11-2-host`, from `14c1c999`,
Broiler-Platform/Broiler.Regex#26's merge):
- *The job's script and its verdict* (`4d220ec`): [`scripts/test262-host/run.sh`](../scripts/test262-host/run.sh),
  `scripts/test262-host/compare.py` and its tests (13), and
  [`test262/host-expected-failures.txt`](../test262/host-expected-failures.txt). That commit also took two compiled
  Python files by mistake; `1fd10d5` and `3be21f5` remove them, and `.gitignore` names `__pycache__/`.
- *CI's `test262-host` job* (`2f3a0aa`), in `.github/workflows/ci.yml`, beside the library job; the library job's
  comment, which said the host job waits for the branch, points to it.
- *The records*, with this one: a dated note in the bundle's README and the CHANGELOG (process only).

**The job** (`test262-host`, ubuntu-24.04, 60 minutes at most). It checks Broiler.JS out at the commit above, named in
the job's environment as `BROILER_JS_COMMIT` (depth 2, as Broiler.JS's own test262 action fetches it), and test262 at
the library job's pin `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`, the harness and the population's directories only
(a sparse checkout). It runs the verdict's tests, then `scripts/test262-host/run.sh`, which:
1. checks the pins: Broiler.JS's `HEAD`, test262's `HEAD` and the git tree of its `test/`
   (`c022917030da8fbc09b7db8f9b7d55ccc5f5ebab`, the library job's);
2. packs this commit's `Broiler.Regex` as the version that Broiler.JS commit pins, read from its
   `Directory.Packages.props` (`0.1.0-preview.6`), into a local feed;
3. writes Broiler.JS's `NuGet.config` in the job's checkout so that the feed is listed first and is the only source of
   `Broiler.Regex` (package source mapping), nuget.org every other package, and restores into an empty packages folder;
4. builds Broiler.JS's shell, `Broiler.JS/Broiler.JavaScript` (`BroilerJS.dll`), in Release, as Broiler.JS's test262
   action builds it;
5. checks the shell was built against the candidate: the restored package's source is the feed, and the
   `Broiler.Regex.dll` beside `BroilerJS.dll` is byte for byte the one in the packed `.nupkg`;
6. runs Broiler.JS's `scripts/compliance/run_test262.py` with `--include-negative`, `--minifier none` (the original
   variant), four workers, 30 seconds per test (passed explicitly) and `TZ=UTC`, over part B's population
   (`test/built-ins/RegExp`, `test/language/literals/regexp`, `test/annexB/built-ins/RegExp`,
   `test/annexB/language/literals/regexp`, `test/built-ins/RegExpStringIteratorPrototype` and
   `test/built-ins/String/prototype/{match,matchAll,replace,replaceAll,search,split}`) and staging's
   `test/staging/built-ins/RegExp`, the population `compare.py` holds (at review, below);
7. holds the report to `test262/host-expected-failures.txt` with `compare.py`. The job fails on a row that fails or
   times out and is not listed, on a listed file whose row does not fail or time out, on a listed path the population
   does not hold, on a file the runner skips or leaves without a row, on a run that is not the one asked for (the suite
   commit, negative tests, the variant, the requested paths, an exit that disagrees with the rows), on an entry that does
   not name one of B-1's five host members, and on any row of a file listing `regexp-duplicate-named-groups` that does
   not pass; the four files task 2.6 left to it are required by name. Since the review, it also fails on a population
   that leaves out any path of B-1's population or of staging's directory, and on a count of the feature's files other
   than the 19 it declares at the pin.

It keeps the report, the runner's log, the candidate's record and the verdict as an artifact.

**The candidate package: a local pack, by B-1's words.** B-1 reads "packs this commit's Broiler.Regex as the version
that commit pins into a local feed listed first", and D14 "packs this commit's package into a local feed listed first".
- *Taken*: this commit's library packed in the job as `0.1.0-preview.6`, the version `f690836d` pins.
- *Not taken*: the published `0.1.0-preview.6` from nuget.org, `df5cfc2`'s bits. It would judge the published package
  and not the commit, so a library change after `df5cfc2` would never reach the job.
- The two are not the same bytes, from the same library sources (the record above: unchanged since `df5cfc2`): the
  published package's `Broiler.Regex.dll` has SHA-256 `dbc27afef07fe8ed44d8f926fc6937ebaaf59cdbaa0aa384fbeae01de9803947`,
  and no local pack gave it; the local packs below gave a different DLL in each working copy (the build is not
  reproducible across paths). This machine's NuGet cache already held the published package, and restore takes a cached
  package of the same version whatever the sources say; that is why the job restores into an empty folder and checks
  the DLL beside the shell against the one it packed (step 5).

**The expected-failures list** names one file, from a local run of the job's commands:
`test/built-ins/RegExp/proto-from-ctor-realm.js`, host member "the RegExp object". `Reflect.construct(RegExp, [], C)`,
with `C` a function of another realm whose `prototype` is null, must take the prototype from `C`'s realm
(GetPrototypeFromConstructor, step 4); Broiler.JS takes it from the running realm. The object is allocated by the host
before any pattern is read, and the pattern is empty. It is the one file Broiler.JS's own runs of the branch fail. All
19 files listing `regexp-duplicate-named-groups` pass, among them the four task 2.6's test left to this job
(`duplicate-names-replace.js`, `duplicate-names-replaceall.js`, `duplicate-names-split.js` and staging's
`duplicate-named-groups-replace.js`).

*Shown locally* (linux-x64, four cores; every run behind the machine's shared build lock and hard idle gate, the
working copies on `/dev/shm`; Broiler.JS fetched at `f690836d` and test262 at the pin, sparse, as the job checks them
out; in these runs Broiler.JS's copy was a sibling of the tree, where CI nests it at `host/broiler.js` inside this
repository's checkout; the run at review below took the nested layout):
- *The filing run*, from the working tree before the first commit, with the list empty: 2,547 files, 2,546 passing,
  one failing, the file above; the verdict red on it alone, and green when the report was read again with the list
  as filed. The run took 454 seconds, the test262 run most of it.
- *At `2f3a0aa`*, the job's commit, from a clean `git archive`, three runs of `run.sh` in a row:
  - *the commit as it is*: green, exit 0; 2,547 files, the listed file failing and every other passing; the shell held
    the DLL it packed, restored from the feed; 437 seconds;
  - *a planted regression*: a scratch copy of the archive whose parser accepts `\k<name>` to an undeclared name under
    `u` (`!_unicode &&` added to the undeclared-name check of `RegexParser.Parse`, part B's control; never committed):
    **red, exit 1**, on exactly the five files `named-groups/invalid-dangling-groupname{,-2,-3,-4,-without-group}-u.js`
    of `test/language/literals/regexp`, each a negative test whose literal the shell now accepts, so the test's
    `$DONOTEVALUATE()` runs; the other 2,541 as before; 442 seconds;
  - *the revert*: the same scratch copy with the file restored, the copy then byte for byte the archive: green, exit 0,
    as the commit; 443 seconds.

  The copies, Broiler.JS's working copies, the feeds and the packages were removed after each run; nothing of them is
  retained in the repository.

**At review** (2026-11-17), in two commits after the records' first:
- *The population is fixed by the verdict* (`39c62c9`). Before it, B-1's population lived only in `run.sh`, which also
  handed it to `compare.py`: a commit narrowing `run.sh`'s list and the sparse checkout together kept the job green.
  The final run's report, narrowed to `test/built-ins/RegExp` and staging's directory with its requested paths to
  match, was green over 1,883 files and 16 of the feature's. `compare.py` now holds B-1's population (`B1_POPULATION`,
  part B's line with its braces expanded) and its one extension, staging's RegExp directory (`POPULATION`); the four
  files task 2.6 left to the job (`REQUIRED`); and the count of files listing `regexp-duplicate-named-groups` at the
  pin (`FEATURE_FILES`: 19 at `ccaac100`, 13 under `test/built-ins/RegExp`, one under `test/annexB/built-ins/RegExp`,
  two under `String.prototype.match` and three under staging). A run whose population leaves out any of those paths,
  whose feature files are not as many, or whose suite commit has no declared count, is red; the narrowed report is now
  red with eleven failures, and the full one green as before. `run.sh` takes the population from
  `compare.py --print-population` and keeps no copy. The verdict's tests (21, from 13) read decision-rule.md's line and
  hold `B1_POPULATION` to it, and read `ci.yml` to hold the host job's sparse checkout to `POPULATION` and its
  `TEST262_COMMIT` and `TEST262_TREE` to the library job's pin, which the job restates. The job's first checkout keeps
  no credential, and its comment names the publish dependency above.
- *The library evidence script's account of B-1* (`4e0e17a`), corrected as the last item of the list below says.
- *Run again* at `4e0e17a` from a clean `git archive`, behind the lock and the gate, with Broiler.JS's copy nested at
  `host/broiler.js` inside the tree as CI checks it out: green, exit 0; 2,547 files, the listed file failing and every
  other passing, 19 of the feature, the four required among them; the shell held the DLL it packed, restored from the
  feed; 470 seconds. The planted regression's report from `2f3a0aa`, judged again by `compare.py` at `4e0e17a`, is red
  on the same five files, and the head's and the revert's reports green.
- *Not changed*: the actions stay on their major-version tags (`actions/checkout@v7` and the rest), as every job of
  `ci.yml` has them; pinning all of them to commit SHAs is a separate change. The two compiled Python files of
  `4d220ec` stay in the branch's history, since nothing is force-pushed; `3be21f5` removes the second, and a squash
  merge would keep both out of `main`, the owner's choice.

**B-1's state.** The job exists, and its commands give the verdict above locally. B-1 is read from CI's run at the
reading's commit (B-3), and its control ("a scratch commit of this repository, never merged, that accepts \k<name> to
an undeclared name under u; B-1 or B-2 must turn red") is a reading's; neither was taken here. Nothing is declared met.

**What the candidate reading (task 4.2) still waits on**, replacing the list of the record above:
1. **CI green at the reading's commit** (B-3), the library job and the host job among the jobs.
2. **The reading's commit and the rule commit**: recommended, the owner's to overturn, `df5cfc2` (the published bits)
   with this tree's scripts and jobs run against it (the record above, item 4), and the rule commit the README names
   (`fca7a6d` or `e568b88`). The host job packs the tree it runs in, so at a later commit whose library tree is
   `df5cfc2`'s it judges `df5cfc2`'s library, as B-3 at that commit would.
3. **The reading itself**: both scripts at the reading's commit, from a clean tree, on an idle machine, with both
   negative controls taken in the run, and part B's control.

Changed since that list: item 1, the host job (B-1), now exists; B-1 itself is not read.

**Network.** `Broiler-Platform/Broiler.JS` is public (the GitHub API's `visibility`), and `git ls-remote` reaches
`f690836d` with no credential. The job checks it out with `actions/checkout`'s default token, under the workflow's
`permissions: contents: read`, and keeps no credential (`persist-credentials: false`), in this repository's checkout
too since the review. No secret is added.

**The pin must stay reachable.** `f690836d` is reachable only through Broiler.JS's unmerged branch `sjl-11-5`
(`git ls-remote` on 2026-11-17: `sjl-11-5` at `f690836d`, `main` at `dcab5079`). `publish.yml` calls `ci.yml`, so the
host job runs in every publish and gates it. If the branch is squash-merged or rebased and then deleted, the job's
checkout, and every publish with it, fails until a dated record moves the pin. Until the pin moves, `f690836d` must
stay reachable in Broiler.JS: its branch kept, or a tag or other kept ref on it.

**Choices taken, each with its alternative.**
- *The local pack* (above). *Not taken*: the published package.
- *Part B's whole population, and staging's RegExp directory.* *Not taken*: the brief's list (`RegExp`,
  `literals/regexp` and the six `String.prototype` methods), which leaves out three of part B's directories; or part
  B's population alone, which leaves three of the 19 files of duplicate named groups unjudged, one of them among the
  four task 2.6 left to this job.
- *A skipped file, or one the report has no row for, fails the job.* *Not taken*: B-1's words alone ("no row fails or
  times out"), under which a file the runner skips would pass unseen. None is skipped today.
- *The host member held to B-1's five names.* *Not taken*: free text, which no check could hold to "the host's".
- *ubuntu-24.04 alone.* *Not taken*: windows-latest as well. B-1 names no platform, and Broiler.JS runs its own test262
  on ubuntu-24.04.
- *The job's commands in one script*, run by CI and locally alike. *Not taken*: inline steps, which a local run could
  only copy.
- *test262's population directories alone* (a sparse checkout), the commit and the `test/` tree checked. *Not taken*:
  the whole suite, as the library job checks it out.
- *In a publish run* (`publish.yml` calls this workflow), the job packs as Broiler.JS's pinned version, not the version
  being published, as B-1 says. *Not taken*: packing as the publish version, which `f690836d` does not pin, so its
  restore would not find it.

**Where the brief and the sources differ, read at this record.**
- The brief lists the job's population as test262's "`RegExp` and `literals/regexp` directories, plus `String.prototype`
  match, matchAll, replace, replaceAll, search and split". B-1 runs part B's population, which adds
  `test/annexB/built-ins/RegExp`, `test/annexB/language/literals/regexp` and
  `test/built-ins/RegExpStringIteratorPrototype`; neither names staging, which holds three of the 19 files task 2.6's
  record says the host job judges. The job runs all of them.
- The brief asks to pick the package "by B-1's words" between nuget.org's `0.1.0-preview.6` and a local pack. B-1 and
  D14 both name the local pack; their words leave no choice, and the alternative is recorded as not taken.
- The brief says the branch's "first parent is `6fda6ca4`". `f690836d`'s first parent is `7eeba1eb`; `6fda6ca4` is
  where the branch's first-parent line leaves Broiler.JS's `main`, ten commits down.
- The brief asks the job be shown failing "on a regression by a planted defect in a scratch copy". That is part B's
  control, which the rule takes at a reading, in CI; the run above is local, the same plant, and not that control.
- B-1 says the job "builds BroilerJS in Release". The job builds the shell's project, which builds `BroilerJS.dll` and
  the projects it references, as Broiler.JS's own test262 action does, not Broiler.JS's whole solution.
- [`run-library-evidence.sh`](evidence/sjl-11-001/run-library-evidence.sh) recorded check B-1 as "not run here; it
  waits for Broiler.JS's SJL-11.5 branch" (its header, line 39), and wrote into a reading's retained log
  `logs/test262-host.log` that "no commit is named, because Broiler.JS's SJL-11.5 branch, which decision D35 makes the
  retained commit, does not exist yet". It does not run B-1, which the rule reads from CI. Both texts were out of date;
  at review (below) they are corrected, and the script still records B-1 as not run.

*Dated record, 2026-11-18 (SJL-11.2: task 4.2, the candidate reading of bundle `sjl-11-001` at `49d7be1f`; the
reading's commit and the rule commit named). No rule file, script, workflow or library source was changed. The per-part
readings below are the only judgement this record makes, and nothing beyond them is declared met or accepted.*

**The commits.** Two decisions, taken under the owner's direction of 2026-10-18 ("Please do all recommended decisions
and skip waiting for review"). They are this session's recommended decisions, not the owner's own words, and the owner
can overturn either.
1. **The reading's commit is `49d7be1f3ef148f9c262f0e52b8c360c07fd838d`** (the merge of
   Broiler-Platform/Broiler.Regex#27, `main` on 2026-11-18). Its library sources and props are those of `df5cfc2`, the
   commit `0.1.0-preview.6` was published from (publish run 43736141111). The git trees are equal at both commits:
   `Broiler.Regex/` is `b8001a304dbfba43ea5eee34b02563083b5e8e1d`, `Directory.Build.props` `5b60c39e…`,
   `Directory.Packages.props` `5df30d7c…`, `NuGet.config` `dba156c8…` and `eng/` `083d0d65…`. So the reading reads the
   published package's sources with this tree's scripts, runner and jobs, as item 2 of the record of the host job above
   recommends ("at a later commit whose library tree is `df5cfc2`'s it judges `df5cfc2`'s library").
   - *Not taken*: `df5cfc2` itself. Its tree has no `test262-library` job, no runner, no host job and neither script,
     so B-2, B-3 and the scripts cannot run there.
   - *Not taken*: a new `0.1.0-preview.7` packed from the reading's commit. Publication is the owner's (D13).
   - **The reading judges the sources, not the published bytes.** No local build reproduces the published DLL (the
     record of the host job above: the published `Broiler.Regex.dll` is `dbc27afe…`, and every local pack gave
     another). CI's host job at the reading's commit packed a DLL of SHA-256 `8ffe19e5…` from the same sources.
2. **The rule commit is `e568b88248c7de5dd75210e22cd357197108e644`** (the merge of Broiler-Platform/Broiler.Regex#6).
   Its rule files are `edfb2d8`'s, and every `rule` block of both rule files is byte for byte the same at `e568b88`,
   `edfb2d8` and the reading's commit. Broiler.VM's half of this bundle, and bundles `sjl-12-001` and `sjl-13-001`,
   name the default-branch merge in the same way.
   - *Not taken*: `fca7a6d`, the branch commit that first added the bundle (the README's note of 2026-11-15).

**The reading's runs**, both from a clean tree at the reading's commit, each behind the machine's shared build lock
(`flock` on one lock file) and its hard idle gate: a load of 1.0 or less, and no `dotnet`, test host, compiler server,
MSBuild, ILC or `Broiler.*` process other than the run's own. Release, `--disable-build-servers`, the .NET SDK
`10.0.404`. *The machine*: four cores, 15 GB of memory and no swap, Ubuntu 24.04.5 LTS, Linux 6.18.44 (x86_64),
Python 3.13.18.
- **`docs/evidence/sjl-11-001/run-library-evidence.sh 49d7be1f <OUT> --test262 <DIR>`**, linux-x64, test262's `test/`
  checked as the pin's tree `c022917030da8fbc09b7db8f9b7d55ccc5f5ebab` at `ccaac100`. It ran from 04:27:21 to 04:30:59
  UTC (218 seconds) and exited **0**. All 39 steps that run ended as the rule expects, and B-1 is recorded as not
  run, by design. The gate passed at a load of 0.43.
- **`docs/evidence/sjl-11-001/run-reference-arms.sh 49d7be1f <OUT> --test262 <DIR> --unicode <DIR> --broiler-js <DIR>
  --broiler-vm <DIR> --correspondence-commit 5e90bfe485aad60b3637d9f69e54ed71adcd73af`**. It ran over all seven classes,
  with Broiler.Unicode at the script's default `0bcb917`, Broiler.JS at the pin file's `b2c2e224` (P1 and the arbiter)
  and Broiler.VM's correspondence at `5e90bfe4`. It ran from 06:24:48 to 08:48:18 UTC (8,610 seconds, at most 3.7 GB of
  memory), exited **0**, and took both negative controls (its last line: "negative controls taken: J V"). The gate
  passed at a load of 0.27. While it ran, another stage's build waited on the lock and did not start.
- **Two earlier tries of the arms run were lost**, and are not readings. Each wrote its output to `/dev/shm`, and a
  container restart wiped it (04:32 and 05:51 UTC). The machine's tmpfs counts against its memory, and memory
  exhaustion is the likely cause of both restarts. The third try wrote its output and inputs to the disk. The library
  run's output was copied to the disk when it ended, so it survived, and it is the reading's. It was not run again:
  its `digests.sha256` and `packages.sha256` verify, and its logs are whole.

**CI's runs at the reading's commit**, by identifier, from GitHub's API (fetched with `curl`, no token of this stage's):
- **Run 44206505415**, workflow CI (`ci.yml`), the push to `main` at `49d7be1f`. It is the only run at that commit, and
  all 14 of its jobs succeeded. Each job and the checks it serves:
  - B-1: the host job, 134473694801;
  - B-2: the library job, linux-x64 134473694923 and win-x64 134473695064;
  - B-3: both of the above, on the default branch at the reading's commit;
  - C and D (and E-2): the Native AOT probe, linux-x64 134473695043 and win-x64 134473694553;
  - C: part C's controls, 134473695079;
  - A, D's test runs and E-1: the Release jobs, ubuntu-24.04 134473695300 and windows-latest 134473694930 (the latter
    also packs, E-4);
  - E-3 and E-7: the architecture tests, ubuntu-24.04 134473694949 and windows-latest 134473694825;
  - E-5: the fuzz target, ubuntu-24.04 134473695222 and windows-latest 134473694800, and its planted-defect control,
    134473695052;
  - E-6: the assurance check, 134473695048.
- **Part B's control**, taken in CI under the owner's permission of 2026-11-18 ("Allow scratch branch"). A scratch
  branch `sjl-11-2-control-b` was made from `49d7be1f`; it was never merged and has no pull request.
  - The plant is `deac674863f3bf859c8e6564634b0df499c62f14`: `!_unicode &&` added to the undeclared-name check of
    `RegexParser.Parse`.
  - Its run, **44237117988**, is red. B-1's host job 134575819645 is red on exactly the five files
    `test/language/literals/regexp/named-groups/invalid-dangling-groupname{,-2,-3,-4,-without-group}-u.js`. B-2's
    library jobs, linux-x64 134575819815 and win-x64 134575819759, are red on the same five.
  - The same plant also turned red the Release jobs, the probe jobs, part C's controls and the assurance check
    (134575819693, on `RegexParser.cs`'s fingerprint drift).
  - The revert is `d0dd222d3f1a4f9a7dd5ff4449b0803cbe94a5cb`, whose tree equals `49d7be1f`'s. Its run, **44238736222**,
    is green on all 14 jobs.
  - **The scratch branch could not be deleted**: the session's git remote refused the delete push. It stays on GitHub
    at `d0dd222`, unmerged and with no pull request, until the owner deletes it.
- The publish run of `0.1.0-preview.6`, 43736141111 (`df5cfc2`), is named for the record and is not read.

Every artifact of run 44206505415 was downloaded, along with the test262 artifacts of the two control runs and the
job logs the rule reads. Each zip's SHA-256 is the digest GitHub records for it. Their reports are retained, except the
packages, which are named by digest. The local library run's reports are byte for byte CI's linux-x64 reports.

**The retained files**: [`candidate-reading/`](evidence/sjl-11-001/candidate-reading/) beside the bundle's README,
with [`manifest.json`](evidence/sjl-11-001/manifest.json). The manifest names every retained file by its SHA-256 and
size, and the commits and CI runs read. Each file kept out is named by its digest, with where it was and why it is
kept out: the populations, the arms' answers and the listings (by digest and line count, as `reference-arms.md`
says), the packages, the inputs' copies, and the machine records that list every process. No file is over 2 MB, so
none is kept out for size. A file with the same bytes as one already retained is named with that file's path. Eight
logs from Windows runners are stored with LF, as `.gitattributes` stores every text file (convention 17, rule SL10),
and their bytes as fetched are named beside them.

**The reading, per part**, with the check and the file that decides it:
- **Part A: read NOT MET on the retained runs.** Every check's tests pass: 2,644 library tests, 49 architecture tests
  and 11 fuzz-target tests, none failed, in `library-evidence/logs/tests.log` and in both Release jobs' logs. But the
  rule's MET reads "and each witness was watched failing once", and "How it is read" decides that "by the retained run
  of its witness failing once; a check whose witness was not seen failing is not met". It adds that "a file the
  manifest does not name decides nothing". This reading retains no run of any part A witness failing, and the bundle
  held none before it. The roadmap's task records say, in prose, that tests were watched failing when they were
  written. Those records are not runs the manifest names. The architecture tests differ: their witnesses sit on disk
  and are run every time (E-3 below).
- **Part B: read MET on the retained runs.**
  - B-1: `ci/artifact-files/44206505415-test262-host/summary.txt`, green. 2,547 files ran, and only the one listed file
    fails. All 19 files listing `regexp-duplicate-named-groups` pass, the four required among them. The candidate's
    record shows the shell holding the packed DLL.
  - B-2: the library job's two reports per RID, under the JIT and through Native AOT, are one file (`374235…`). The
    report says "failed: 0", and 43 unreadable files are listed with why.
  - B-3: both jobs are green in run 44206505415, a push to `main` at the reading's commit.
  - The control turned B-1 and B-2 red, and its revert is green: the three red jobs' logs are under
    `part-b-control/jobs/` and `ci/job-logs/`, and the record is under `part-b-control/`.
- **Part C: read MET on the retained runs.** Per RID, the probe's `adversarial-aot.txt` and `adversarial-jit.txt` are
  one file (`3f7fc538…`, on both RIDs and locally). In it, 65 rows give their declared status, none differs and none
  ends abnormally. Part C's controls job wrote `control-steps.txt` (the 17 rows of K, A, L, W and G reported),
  `control-nesting.txt` (every N row past the limit reported) and `revert.txt`, which equals the reports. The local run
  wrote the same bytes, and its `-holds`, `-lacks` and `controls-no-library` logs pass.
- **Part D: read MET on the retained runs.** Per RID, `vectors-aot.txt` and `vectors-jit.txt` are one file
  (`3b35a475…`). Its header shows the pin check before the run: corpus revision 32 at `b2c2e224`, the pin file
  `3be30a80…`.
  - Every normative and divergence vector whose op the library implements gives its out.
  - All 18 status vectors give their status.
  - All 85 implementation-defined vectors are reported.
  - The 27 vectors of ops the library does not implement are listed under six named ops of the object model.
  - The Release jobs ran `VectorsRunTests` and `VectorsProbeTests` within the 2,644 tests.
- **Part E: read MET on the retained runs.**
  - E-1: `build-no-warning.log`, and both Release jobs' logs.
  - E-2: `publish-no-warning.log`, `publish-no-assembly.log` and the probe's `probe-publish.log` per RID. The default
    population's two reports per RID are one file (`8f40ae86…`), and parts C and D's are as above.
  - E-3: the architecture tests' 49 pass. Their register holds SL1 to SL10 and C1, C3 and C5, each Active with a
    witness on disk, and the witnesses are run every time.
  - E-4: `pack.log` and the windows-latest Release job's pack. Package validation runs in the pack against
    `0.1.0-preview.5`, the baseline as the rule's dated note reads it. Each of the 29 suppressions is listed in
    `docs/suppressions.md`, and under Changed in the CHANGELOG, whose entry has the Observable changes and Data
    headings.
  - E-5: the fuzz jobs, their `fuzz.log` artifacts and the control job's `fuzz-control.log`. The session reached 122
    construct classes of 122 with no finding in 24,000 iterations, and the declared seed found the planted defect.
  - E-6: the assurance job ("no violations") and its drift control. The plant's run turned the assurance job red on
    drift (job 134575819693: `RegexParser.cs`'s recorded fingerprint against the current one), and the revert's
    assurance job, 134581114155, is green. `HUMAN_REVIEW.md` names the revision it reviewed and says that no review
    covers the current code.
  - E-7: the architecture tests (SL10, `.gitattributes` LF, the project settings).
- **The reference arms: read MET on the retained runs**, under the rule as revised: option R, confirmed by the owner on
  2026-11-17, and the owner's rulings of 2026-11-16 (16b, the exclusion; 16c, `O06`), each weaker evidence by its own
  header.
  - The population is whole (`population/manifest.json`, `"partial": false`, all seven classes). S, V and J under the
    invariant culture, tr-TR and lt-LT ran over it.
  - The comparison's `differences.tsv.gz` holds 1,901,509 differences (`0a8345fd…`), byte for byte task 2.13's
    retained comparison.
  - *As filed* (`comparison/classification.json`, exit 1): a 1,886,022, a' 45, b 8,983, c 6,459.
  - The declared reduction (`reduction/reductions.tsv`) is byte for byte that of 2026-11-15 (`b3e05301…`).
  - *As revised* (`classification/classification.json` `d6ca3a14…`, and `classified.tsv.gz` `5dd1a960…`, kept by
    digest; `run/logs/classify-revised.log`, exit 0): a 1,885,368, a' 45, b 4,876, b' 2,250, excluded 8,970, **no
    class (c)**. These are the `@9` classification's bytes.
  - The excluded patterns' differences are listed apart.
  - Of the 4,876 class (b) differences, 15 are P1 vectors at corpus revision 32: row S3's nesting refusal (12) and row
    M4's empty match under decision D41 (3). The other 4,861 are P4 stops at a declared policy (rows D10 and B6), where
    the specification gives an answer and admits neither side's stop. So every class (b) difference the specification
    admits is in a vector at a named corpus revision (the choice below).
  - **Both negative controls were reported unclassified** (`controls/V/control.json`, `controls/J/control.json`;
    `controls.py check` found no problem in either):

| | V over P4, `P4-01234-u-05` | J over P1, `P1-regexp:matching:m1-notes:0001` |
|---|---|---|
| The copy's change | `match:0,1` for `match:0,0` | `match:0,2` for `match:0,1` |
| *Control* (the case named) | unclassified 1; exit 0 | unclassified 1; exit 0 |
| *Blind* (no case named) | **c 1**, "arm V is wrong by the arbiter and no row of the correspondence names the input"; exit 1 | **c 1**, the same for arm J; exit 1 |
| *Revert* (the arm's own answers) | no (c), the run's classification for P4; exit 0 | no (c), the run's classification for P1; exit 0 |

The fast-path differential ran to the end, with no disagreement. It is not judged.

**So by the rule, part A's NOT MET blocks both cut-overs** (risk D10's stop; `decision-rule.md`: "A part NOT MET at
the candidate reading blocks both cut-overs"). No rule, script or library code was changed to turn it. Parts B to E
and the reference arms read MET on the retained runs. That is not the gate: the gate waits on part A and on the owner.

**Part B's local check, not rerun.** The patch of the plant is in this stage's notes. A first try at a local check of
it, outside CI, was started on 2026-11-18 and left no result when the container restarted. It is superseded by the
control above, taken in CI, which is the rule's control. The host job's local proof (the record of the host job above)
had already shown the same plant red on the same five files.

**What task 4.2 still waits on**, replacing the record of the host job's list (items 1 and 2 of that list are
answered by the runs and decisions above; item 3's reading is this one):
1. **Part A's witnesses**: a retained run of each part A check's witness failing once, named in a later reading's
   manifest, or the owner's reading of how the roadmap's accounts stand for them. Until then part A reads NOT MET at
   the candidate reading, and the cut-overs stay blocked.
2. **The owner on publication (D13).** `0.1.0-preview.6` was published from `df5cfc2` before this reading, so the
   reading judges its sources and not its bytes. Whether to keep it, publish another version after a reading, or
   neither, is the owner's question.
3. **The owner on the decisions above** (the reading's commit and the rule commit), which the owner can overturn.
4. **The scratch branch `sjl-11-2-control-b`**, for the owner to delete.

**Choices taken, each with its alternative.**
- *The library run kept, not rerun*, as its checks verify. *Not taken*: running it again after the restarts.
- *The arms' output on the disk.* *Not taken*: `/dev/shm`, which lost two tries.
- *The CI artifacts downloaded.* The requests carried no token of this stage's, but they went through the session's
  proxy, whose rate limit (15,000 an hour, `ci/api/rate-limit.json`) is an authenticated client's. *Not taken*: naming
  the artifacts by identifier only, which leaves the reports the rule reads unretained.
- *Class (b)'s vector clause read as above*: a stop at a policy bound against an answer, or against another stop, is
  not an answer the specification admits. *Not taken*: reading every policy stop as admitted by the specification,
  which would need vectors for 4,861 P4 inputs, and of which the corpus holds none.
- *Part A read NOT MET* on the words "the retained run of its witness failing once". *Not taken*: reading the roadmap's
  accounts as retained runs, which the rule's "a file the manifest does not name decides nothing" refuses.
- *The drift control read from the control's run*, whose assurance job was red on drift. *Not taken*: a separate
  drift plant, which would need a second scratch commit.
- *A file retained once*, with later copies of the same bytes named in the manifest. *Not taken*: a copy each.
- *Windows logs stored with LF*, their bytes as fetched named by digest. *Not taken*: storing them as fetched, which
  `.gitattributes` and rule SL10 refuse.
- *The packages, populations and listings named by digest.* *Not taken*: committing them.
- *The artifacts' files under `ci/artifact-files/`*, since the repository's `.gitignore` ignores every directory named
  `artifacts/`; the first commit of the files named them under `ci/artifacts/`, which Git left out, and the next one
  moves them. *Not taken*: editing `.gitignore`, a file outside this reading's directory.

**Where the brief and the sources differ, read at this record.**
- The brief says to retain "the five red jobs' logs that decide it (B-1's and B-2's three)". Three jobs decide it, B-1's
  and B-2's two; five is the number of failing files. The three are retained.
- The control's record in this stage's notes opens with "deleted after both runs", and ends with the delete refused.
  `git ls-remote` shows the branch at `d0dd222`. It is retained as written, and this record says the branch stands.
- The brief asks for `curl` "unauthenticated". No token was sent, but the session's proxy authenticates, so whether
  the artifacts are reachable with no credential at all was not tested.
- The brief's lists of what the reading reads, and the record of the host job's list of what task 4.2 waits on, name
  no part A witness run. The rule's "How it is read" asks for one, and this reading finds none retained.
- No CI run at the reading's commit is a separate drift control for E-6. The one read is the assurance job of part
  B's plant run.
- GitHub's combined commit status for `49d7be1f` reads `pending`, because the commit has no legacy status. All 14 of
  its check runs succeeded.
- The library run's output and test262 copy were on `/dev/shm` (the first try, before the rule of the brief's second
  addendum). Its retained logs name those paths.

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

*Dated note, 2026-11-01 (task 2.9 of SJL-11.2): the version, revised by the owner.* The paragraph above reads "The
SJL-11 surface ships as **`0.2.0-preview.1`**, validated against `0.1.0-preview.4` with every break suppressed and
listed with its R item", and "`0.2.0-preview.1` is published to nuget.org only with the owner's approval (decision
D13)". The owner wrote on 2026-11-01 "Please keep using version 0.1.0-x scheme!": the SJL-11 surface ships as
**`0.1.0-preview.6`**, the next of the `0.1.0-preview.N` stream after `0.1.0-preview.5`, which the owner published on
2026-10-29 from `24344b3`, and package validation reads `0.1.0-preview.5`. Where this roadmap names `0.2.0-preview.1` as
the SJL-11 package (the record of SJL-11.1 above, the candidate reading "at the commit `0.2.0-preview.1` is packed
from", and the records of tasks 2.4c, 2.5, 2.6, 2.8 and 2.14), it is `0.1.0-preview.6`; "`0.2`", where an engine's pin
is meant, is the SJL-11 surface. Publication stays the owner's, after the candidate reading (D13). The rest of the
paragraph stands. What the release train's "inside a `0.y` minor version" now reads is in
[`conventions.md`](conventions.md): convention 16's dated revision of 2026-11-01 and "Where Broiler.Regex stands" record
that the SJL-11 surface's breaks ship inside `0.1`, as `0.1.0-preview.6`, by the owner's decision, a recorded departure
from that reading, which stays the programme's. (Corrected by the review of task 2.9: this sentence sent the reader to
task 2.9's record, which does not say it.)

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
