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
well (`A_class_s_strings_are_members_by_the_comparer_they_are_made_with_and_by_no_other`).

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
