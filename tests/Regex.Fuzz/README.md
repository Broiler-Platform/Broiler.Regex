# The fuzz target

**Written:** 2026-10-27, at task 2.11 of work package SJL-11.2 of the shared JavaScript libraries programme, under the
owner condition R10 of [`docs/roadmap.md`](../../docs/roadmap.md) section 6 ("a fuzz target"), decision D32 of
Broiler.VM's JSD-0081 ("a seeded in-process grammar fuzzer in CI with every limit set and a planted-defect control")
and the check E-5 of bundle `sjl-11-001`'s rule
([`decision-rule.md`](../../docs/evidence/sjl-11-001/decision-rule.md), part E): "the fuzz target tests/Regex.Fuzz runs
in CI for its declared seed and iteration count with every limit and a meter set: no exception but statuses and the
meter's, no process end; a planted throw in a declared parser path is found by the declared seed". The risks it
answers are the programme's S1 (unbounded work on hostile input) and S2 (a stack overflow on any fuzz input).

## Running it

```bash
dotnet build tests/Regex.Fuzz -c Release
dotnet tests/Regex.Fuzz/bin/Release/net10.0/Regex.Fuzz.dll                       # the declared session
dotnet tests/Regex.Fuzz/bin/Release/net10.0/Regex.Fuzz.dll --seed 7 --iterations 1000000 --from 24000
dotnet tests/Regex.Fuzz/bin/Release/net10.0/Regex.Fuzz.dll --replay --seed 20261027 --iteration 280
dotnet tests/Regex.Fuzz/bin/Release/net10.0/Regex.Fuzz.dll --show --seed 20261027 --iteration 280
```

| Option | Meaning |
|---|---|
| `--seed n`, `--iterations n`, `--from n` | The session: iterations `from` to `from + iterations - 1` of seed `n`; the declared seed and iterations by default |
| `--workers n` | Child processes, each running a contiguous part of the range (default: the processor count, at most 4); the transcript does not depend on it |
| `--watchdog-seconds n` | How long a child may write nothing before it is stopped and reported as a hang (default 120) |
| `--max-findings n`, `--stop-when-found text` | When the session stops early: after `n` findings (default 20), or at a finding whose detail holds `text` |
| `--transcript path` | Writes every iteration's record, in index order |
| `--culture name` | Runs the children under that culture; the transcript must not change |
| `--require-coverage` | Requires every construct and outcome of `Coverage` to be reached, as the declared session always does |
| `--no-replay` | Skips the reduction of the first findings |

Exit codes: 0, no finding; 1, a finding `known-findings.tsv` does not list; 2, arguments it does not read; 3, no finding
but a construct or outcome not reached; 4, no finding but a listed finding no longer reached (stale).

`dotnet test` runs the project's xunit tests with the rest of the solution: a short session at the declared seed, the
determinism of the digest over runs and over a split range, the generator's reach, the oracle's control (an exception
of a meter that is not the session's own is a finding), the supervisor's controls (a child that ends, or stops
answering, is a finding at the iteration it was in), and each known finding's reproduction.

## The declared session

- **Seed** `20261027`, **iterations** 0 to 23,999 (`FuzzSession.DeclaredSeed`, `FuzzSession.DeclaredIterations`).
- **CI**: the `fuzz` job of [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml), on ubuntu-latest and
  windows-latest, bounded by the job's 20 minutes and the session step's 10, and inside it by the watchdog's 120 seconds
  per child line. Locally, on four cores (linux-x64), the session took 26 to 28 seconds on a quiet machine and 37 to
  40 seconds beside other builds.
- **Its transcript digest** at the commit that added it was
  `56985dcac551a7abe7ecb627c415ec9e68e141b9cae2fb565a93bbe0139fe85e`, the same over five runs, over three and four
  worker processes and under `tr-TR`. The digest is a record, not a pin: the records hold the units the meter was charged and
  the library's answers, so it moves with any change of the library's behaviour or units, and CI prints it without
  comparing it.

## What one iteration does

Each iteration draws its case from a generator of its own, seeded by the session's seed and the iteration's index
(SplitMix64), so iteration `i` is the same case however the range is split.

**The pattern.** 22 iterations in 100 take one of the adversarial corpus's classes (`tests/Adversarial/README.md`):
K catastrophic backtracking, A alternation without quantifiers, L look-around and look-behind, G a greedy run past the
old allowance over subjects up to 200,000 characters, W exhaustion inside one alternative, N nesting (510, 511, 512
and 513 levels around the atom, 511 being the limit's, then 1,024, up to 2,048, 20,000 and 1,000,000) in capturing,
non-capturing, named, look-ahead, look-behind and modifier groups and in `v`-mode classes, F flat sequences of up to 20,000 terms, M frames and trail (now and then over a
subject long enough to pass the engines' own ceilings), V large `v`-mode and case-insensitive expansions, and O ordinary
patterns. The rest are written from ECMA-262's Pattern grammar (`PatternGrammar`), Annex B's additions outside `u` and
`v`: literal characters (astral, lone surrogates, characters with case foldings, line terminators), `.`, every character
escape (control, `\c`, `\0`, `\x`, `\u`, `\u{…}`, escaped surrogate pairs and lone surrogates, identity, Annex B's
octal and incomplete escapes), the class escapes, property escapes with and without values, negated, and the properties
of strings, classes with ranges, negation and escapes, `v`-mode class sets with nesting, union, intersection,
subtraction, ranges and `\q{…}` (empty strings included), capturing, non-capturing and named groups (names raw,
escaped, astral and as escaped surrogate pairs, repeated), the four look-arounds (look-aheads quantified outside `u`),
modifier groups, numbered and named back-references (forward, backward and missing), `^ $ \b \B`, and every quantifier
form, greedy and lazy, with bounds up to 2^20, `int.MaxValue` and past it. Every flag combination: the mode (none, `u`,
`v`, and both, which `Compile` reads as `v`), each of `d g i m s y` independently.

**Near misses.** 18 iterations in 100 take a grammar pattern and make one edit that most often reaches a refusal:
quantifier bounds out of order, a group or class left open or closed twice, duplicate and undeclared group names,
property names one edit from an admitted one, a property of strings negated or outside `v`, escapes `u` refuses,
quantified assertions, mixed and reserved class-set operators, invalid group names and modifiers, nothing to repeat,
back-references past the groups, `u` with `v`, an edit at random, a truncation.

**The subject** is built from the pattern's sample (text the pattern would match, or nearly): as it is, embedded in
noise, repeated, edited by one character, interleaved with lone surrogates and pairs, case-mapped, empty, or a long
run of up to 65,536 characters; the classes have their own, of up to 1,100,000 characters.

**The calls.** Every call is under a policy with every limit set:
- `BroilerRegex.Compile` twice, with a nesting limit, a compile budget and a meter: the full policy (the maximum
  nesting limit, a budget of 2^26 units, a meter that counts) and a tight one (a random limit, budget and meter
  allowance);
- the constructor, which takes no limit, beside the full compilation;
- the offset entry `BroilerRegex.Match(ReadOnlySpan<char>, ...)` from every start of the case (0, the end, one at
  random, one inside a surrogate pair and one after it), anchored and not, under wide ceilings (a step ceiling of 2^18
  units, 2^23 for a long subject, and the engines' frames and trail) and a meter, and once under narrow ones, which
  reach every ceiling and the meter's allowance;
- the string-bearing `Match(string, int)`, `IsMatch` and `Matches`, from every start and, now and then, -1 and past the
  end;
- the text helpers `RegexEscaper.EscapePattern` and `EscapeString`, with a maximum length and a meter, half the time;
- now and then, each documented argument exception, for an argument broken on purpose.

## What counts as a finding

An answer is a status (`Success`, `Match`, `NoMatch`, `LimitExceeded` naming its dimension), a
`RegexSyntaxException` from compilation, the session's own meter exception, a documented argument exception for an
argument broken on purpose, and `IsMatch`'s `InvalidOperationException` for a match that reached a ceiling. A finding
is anything else:

- **exception**: any other exception escaping an entry, a documented argument exception included when the session's
  arguments were valid;
- **process-end** and **hang**: a child that ends without its last line, or writes nothing for the watchdog's time,
  reported at the iteration after its last record, with that case; the child is restarted after it;
- **disagreement**: two answers to one question that differ. A tight compilation that succeeds, or refuses a syntax
  error, against the full one; the constructor against the full compilation; a narrow match call that completes
  against the wide one; after the meter's exception, the same instance against its own earlier answer; the
  string-bearing members against the offset entry, through the mapping their documentation states
  (RegExpBuiltinExec's step back into a surrogate pair under `u` and `v`, `y` as the anchored argument, the index of
  decision D41, groups from the slots, each `Value` (the match's, group 0's and every group's) the subject's text at
  the offsets reported, `NamedGroups` from a participating group of each name, a failed match with no index, text or
  groups, `Matches`' iteration of §22.2.6.9, a ceiling reached as `RegexMatch.Limit`, as `IsMatch`'s exception and
  as `Matches`' last element), compared wherever the wide call completed or met the frame or trail ceiling the
  string-bearing members also pass. `Matches` is emulated through the wide calls: those the case made already, from
  its starts, are always taken, so its first element is compared on every subject, a ceiling there included (only
  subjects of about 500,000 units and more reach one, the corpus's class M now and then); a new call is made only on a
  subject of at most 4,096 units, within 2^20 units spent and 4,096 elements. An emulation that reaches the
  enumeration's end is compared element for element and in its count, one cut short over the elements it has;
  `EscapePattern`'s text compiling or matching otherwise than its pattern; `EscapeString`'s text, compiled under any
  flags, failing to match its subject whole;
- **contract**: an answer the documentation rules out: a status with the wrong limit, slots written past
  `SlotCount`, slots other than -1 after any answer but a match or after the meter's exception, a match's offsets
  outside the subject, before an unanchored start or off an anchored one, a group's offsets out of order,
  `SlotCount` other than 2 × (`CaptureCount` + 1).

The session does not judge whether a match is the specification's: that is the vectors' and the reference arms' work
(tasks 2.12 and 2.13). It judges that every entry answers, and that the entries agree.

## The planted-defect control (D32)

`dotnet build tests/Regex.Fuzz -c Release -p:RegexFuzzPlant=true` builds the target without referencing the library:
it compiles the library's sources into itself, with one declared line of the parser replaced, in a copy under `obj/`,
by a throw of an `InvalidOperationException` no entry documents. The declared parser path is the `v`-mode class-set
subtraction, `RegexParser.ParseClassSubtraction` (`Set = accumulated.Set.Subtract(operand.Set, _account),`), which only
a grammar-based generator reaches. The library's source and `Broiler.Regex.dll` are untouched: the library project is
not built in that mode, and the build fails if the anchor is not in the parser exactly once. The `fuzz-control` CI job
runs the declared seed over the declared iterations on that build and requires the plant reported as a finding. With
one worker the declared seed reports it at iteration 52, in under a second; without the plant, the declared session
reports nothing.

## Known findings

[`known-findings.tsv`](known-findings.tsv) lists the findings in library code this target reported and that are not
fixed yet: a session prints each as known and does not fail on it, a declared session that no longer reaches one fails
as stale, and `FuzzTargetTests.Each_known_finding_still_reproduces` fails once its reproduction no longer reproduces.
A row leaves the file in the change that fixes its defect. The library code is not this target's to change.

## Reproducing a finding

Every finding names its seed and iteration. `--show` prints the case; `--replay` runs it in one process, prints every
finding and reduces the case of each (characters of the pattern and of the subject removed while a finding of the
same kind, entry and exception remains, then each flag it does not need). A supervised session replays its first three
findings itself.
