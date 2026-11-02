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
summary (`3cb4bb79…`). The comparison wrote **1,901,509 differences**: V 5,454; J 626,895 under the invariant culture,
636,411 under `tr-TR` and 632,749 under `lt-LT`; by class P1 465, P2 556, P3 7, P4 1,899,989, P5 492, P6 and P7 none.

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
