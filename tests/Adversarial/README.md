# The adversarial resource corpus

**Written:** 2026-10-20, at work package SJL-11.1 of the shared JavaScript libraries programme, with bundle
`sjl-11-001`'s rules ([`docs/evidence/sjl-11-001/`](../../docs/evidence/sjl-11-001/README.md)) and before any code
of SJL-11.2 exists here: no meter, no new backend, no provider and no validate entry. **Owner:** the Broiler.RegEx
maintainer. **Reviewer:** none; every role the programme names is held by the same person (Broiler.VM's exception
EX-30).

*Revised 2026-10-25, before any run and before this change reached the default branch, to agree with Broiler.VM's
JSD-0081 as filed: section 3 states the four presumptions its decision D37 records, where it stated three, and
section 5 cites decision D10, not D7, for the frame and trail ceilings. No row of [`corpus.tsv`](corpus.tsv) changed,
and its SHA-256 is the one both engines' manifests name.*

[`corpus.tsv`](corpus.tsv) is two things at once:

- **The library's population.** Part C of [the rule](../../docs/evidence/sjl-11-001/decision-rule.md) runs every
  row through the Native AOT probe and under the JIT, each in a child process on a thread of the stack the header
  declares, and compares the status each row gives with the status it declares. That is the milestone's component
  clause 3, "The adversarial corpus shows no silent exhaustion and no shortened match".
- **The source of both engines' programs.** [`eng/adversarial-programs.py`](../../eng/adversarial-programs.py)
  writes one program per case for Broiler.VM (`--engine broiler-vm`) and for Broiler.JS (`--engine broiler-js`),
  with a manifest naming this file's SHA-256 and this repository's commit. Broiler.VM's rule judges its programs
  in its part D and Broiler.JS's in its part C; each engine commits its copy in its own bundle `sjl-11-001`, under
  `docs/evidence/sjl-11-001/inputs/adversarial/`. Nothing generated is committed here.

The corpus is judged as committed with the rule, in the set of changes that files them both. A change after that
set of changes merges is a revision under the rule's "Revisions" section: a new dated file beside the rule, and the
engines' manifests then name a different SHA-256.

## 1. Format

UTF-8, LF line endings, tab-separated. Lines starting with `#` come first; three of them are read:

| Header | Meaning |
|---|---|
| `corpus-format: 1` | This format |
| `stack-bytes: 1048576` | The smallest stack a child process runs a row on (part C's configuration, and requirement R4's check of the nesting maximum) |
| `js-wall-clock-limit-seconds: 300` | The wall-clock limit of each Broiler.JS program, copied into its manifest |

Then a column line and one row per case:

| Column | Content |
|---|---|
| `id` | The class letter and two digits |
| `class` | K catastrophic backtracking, A alternation without quantifiers, L look-around and lookbehind, G a greedy run longer than the old allowance, W exhaustion inside one alternative, N nesting, F a flat sequence of many terms, M frames and trail, V large `v`-mode and case-insensitive expansions, O ordinary patterns |
| `op` | `compile`; `validate` (parse and early errors only, the validate entry of SJL-11.2); `match@k` (a search from index *k*); `anchored@k` (a match at index *k* only, R7's anchored argument, independent of the `y` flag) |
| `pattern` | An expression (below) |
| `flags` | Distinct letters of `dimsuv`, or `-` for none. `g` and `y` never appear: the operation says where a match starts |
| `subject` | An expression, or `-` for `compile` and `validate` |
| `policy` | Six keys, always in this order: `meter` (the caller's meter, which throws once more than this many units have been charged, or `none`), `steps` (the step ceiling, or `none`), `frames`, `trail`, `nesting` (the caller's nesting limit), `compile` (the compile budget, or `none`) |
| `status` | The declared status (below) |
| `vm_fuel`, `vm_subject`, `vm_outcome` | The case in Broiler.VM: its fuel allowance, its subject (`=` for the library's), its declared outcome; all three `-` when the row is not in Broiler.VM's population |
| `js_subject`, `js_outcome` | The case in Broiler.JS: its subject (`=` for the library's), its declared outcome; both `-` when the row is not in Broiler.JS's population |

**Expressions.** One or more JSON string literals, each optionally followed by ` * n`, joined by ` + `:
`"b" + "a" * 30` is `b` followed by thirty `a`. A JSON escape writes any UTF-16 code unit, a lone surrogate
included. The programs build the same text with `String.prototype.repeat`.

**Library statuses.** `match s,e c1 c2 ...` is a match from *s* to *e*, then each capture group in order as
`s,e` or `-` when it did not participate; `x^k` stands for *k* copies of `x`. `nomatch`. `limit:steps`,
`limit:frames`, `limit:trail`, `limit:compile` are LimitExceeded naming that dimension, and `limit:nesting` is the
nesting status. `meter-exception` is the caller's meter's own exception, propagated. `syntax` is a syntax status
(its code is SJL-11.2's to define). `ok` is a `compile` or `validate` that succeeded.

**Engine outcomes.** An answer is `null`, or `s,e` followed by each capture group's length or `-`, with the same
`^k` shorthand, which the generator expands in the manifests. Broiler.VM: `exhausted` (an exhausted allowance or
ceiling, whatever its dimension), `uncaught SyntaxError`, or `completed <answer>`, the program's completion value.
Broiler.JS: `threw RangeError`, `threw SyntaxError`, or `completed <answer>`, printed as
`adversarial-outcome <id> <outcome>`. A `compile` row's program also runs `exec("")`, so that an engine that
compiles lazily compiles, and its answer is that call's.

## 2. Nesting depth

Depth is counted as Broiler.VM's matcher counts it (`src/Broiler.VM.Profile.JavaScript.Format/JsRegExpMatcher.cs`,
`ParseDisjunction` and `ParseNestedClass`, at Broiler.VM `aaa39fb`): the pattern's own Disjunction is depth 1;
each group of any kind (capturing, non-capturing, named, look-around, modifier) adds 1 for its Disjunction; under
`v`, each class adds 1, the outermost included. A limit of *L* admits depth *L* and refuses *L* + 1, so 511 nested
groups around `a` are at depth 512 and 512 groups are one past it. SJL-11.2 counts the same way, so that
Broiler.VM, passing its own limit, keeps its verdicts (JSD-0081's decision D7).

## 3. The margin

The rule's part C asks that "a row declared exhausted needs, by regexpref's count of the specification's matcher
steps, at least a hundred times its budget, and a row declared to match completes within a hundredth of it", so
that a change of units alone cannot move a declared status. **The count** is the number of invocations of
ECMA-262 §22.2.2's Matcher closures, as `regexpref` (Broiler.JS's `scripts/conformance/regexpref.py`,
`ref:regexpref@1`, SJL-11.3's task 3.1) will report it. It does not exist yet. The arguments below are bounds
read from the specification's algorithms, chiefly RepeatMatcher (§22.2.2.3.1); when `regexpref` exists, its count
for each row is recorded in the bundle before the candidate reading, and a count that contradicts a bound here is
a defect of this corpus, answered by a revision before that reading.

The step count does not measure frames, the trail or compilation, so the margin of the rows declared exhausted in
those dimensions is stated in their own terms:

- **M rows:** the pending alternatives (frames) or undoable capture writes (trail) the specification's backtracking
  order holds at once are at least a hundred times the ceiling.
- **V rows:** the ranges and strings the expansion touches, the lower bound of R2's charge (part A, check A-2), are
  at least a hundred times the compile budget, and the budget is larger than the pattern's length in code units,
  so that a charge of one unit per pattern character alone would not refuse the row. Range and string counts are
  read from Broiler.Unicode's pinned UCD 17.0.0 and emoji 17.0.0 files: General_Category `L` has 684 ranges;
  `RGI_Emoji` has 2,760 strings longer than one code point.

**The order of the ceilings.** Every row declared `limit:steps` sets the frame and trail ceilings at four and
eight times its step ceiling or more. Each frame pushed and each cell written to the trail is the work of a
dispatched instruction, which is charged (JSD-0081's decision D8), so the step ceiling is reached first. The M
rows set no step ceiling.

**What the statuses presume.** The declared statuses presume four things of SJL-11.2's backend. Decisions D5 and
D8 alone do not state them: D8 charges a run per character it examines, but neither says which quantifier is a run
or what a run holds. JSD-0081's decision D37 records the four, as stated below, as constraints on SJL-11.2's tasks
2.3 to 2.4c.

1. **No memoization.** The backend searches in the specification's order and does not remember that an
   instruction failed at a position. Every K, A, L and W row's margin counts the specification's paths, which a
   memoizing backend would not walk.
2. **No required-literal analysis.** The only prefilter is task 2.4c's literal-prefix one, which charges its
   skips. Rows that end in an unmatched literal put that literal earlier in the subject where they can (K01,
   K03, K04, A01, A02), so that a test for its presence in the subject would not shortcut them; a test for it at
   or after the start position would. K02 cannot put its `b` earlier, because `(a*)*` matches the empty string
   and a `b` before the run would match at once, so a test for presence alone would answer it. K05 requires no
   literal.
3. **A greedy run.** A greedy quantifier over one character class runs as one instruction with one backtrack
   point: it adds no frame and writes no trail cell per character, and charges one unit per character it
   examines (decision D8). This is Broiler.VM's `Op.Run` (`JsRegExpMatcher.cs`, `EmitRepeat` and `TakeRun`, at
   `aaa39fb`), the form this corpus expects task 2.4c's single-code-point quantifier path to take as
   instructions. Broiler.VM uses it for a class body only: its `a*`, a literal body, is a loop that pushes a frame
   and writes a trail cell per character. The G rows, O05 and O06 therefore write their run as the class `[a]`.
   In the loop form, their 10,000,001 characters would pass the frame ceilings: the G rows' engine cases would
   end exhausted in Broiler.VM and with a `RangeError` in Broiler.JS, and O05 and O06 with `limit:frames`, rather
   than complete. The library's G rows, declared `limit:steps`, reach their step ceiling first in either form.
   The M rows' bodies are alternations, which are not runs; their margin is the loop's. **The presumption holds
   for a class only.** A backend may also run a greedy quantifier over a single character as one instruction;
   it then turns Broiler.VM's stop on `a*` past 2²⁰ characters (row B1 of Broiler.VM's correspondence) into an
   answer, which Broiler.VM's rule reads under its part A's class C2.
4. **Nesting depth counted as Broiler.VM counts it** (section 2; JSD-0081's decision D7): the pattern's own
   Disjunction is depth 1, each group adds 1, and under `v` each class adds 1. The N rows at the limit then
   complete and the rows one past it refuse, in the library and in both engines, and Broiler.VM, passing its own
   limit, keeps its verdicts.

A backend that breaks one of these presumptions can answer as the specification does and still fail part C.
That is why they are stated here and handed to tasks 2.3 to 2.4c.

## 4. The rows

"Today" is what the nearest call of the code at `0799342` (the code `0.1.0-preview.4` was built from),
`new BroilerRegex(pattern, flags).Match(subject, index)`, does. No row can run as written today: there is no
policy, meter, status, validate entry or anchored argument. Paths are under `Broiler.Regex/`.

| Row | Declared | Margin | Today |
|---|---|---|---|
| K01 `(a+)+b` on `b`a³⁰ | `limit:steps` at 10⁶ | From index 1, every composition of every prefix of a³⁰ reaches `b`: 2³⁰ − 1 > 1.07 × 10⁹ ≥ 10⁸ | No match after the per-start allowance runs out (`Matching/Matcher.cs:122-126`, `Matching/MatchState.cs:29-48`): silent exhaustion |
| K02 `(a*)*b` on a³⁰`c` | `limit:steps` at 10⁶ | From index 0, the same compositions, empty iterations refused: 2³⁰ − 1 | As K01 |
| K03 `(a\|aa)+b` on `b`a⁴⁰ | `limit:steps` at 10⁶ | From index 1, every tiling of every prefix by `a` and `aa`: F(43) − 2 = 433,494,435 ≥ 10⁸ | As K01 |
| K04 `(?:a+)+b` /u on `b`a³⁰ | `limit:steps` at 10⁶ | As K01 | As K01 |
| K05 `^(\w+\s?)*$` on a³⁰`!` | `limit:steps` at 10⁶ | Only index 0; every composition of every prefix fails at `$`: 2³⁰ − 1 | As K01 |
| A01 `(?:a\|aa)`×30 `b` on `b`a⁶⁰ | `limit:steps` at 10⁶ | From index 1, each of the 2³⁰ choices consumes at most 60 characters and reaches `b`, which fails: 2³⁰ | Runs every path: alternation is not charged (`Matching/Matcher.cs:311-327`), and there is no quantifier to charge |
| A02 as A01, /v | `limit:steps` at 10⁶ | As A01 | As A01 |
| L01 `^(?!(?:(a+)+b\|a*c))` on a³⁰`c` | `limit:steps` at 10⁶ | Inside the look-ahead, the first alternative visits 2³⁰ − 1 compositions before the second is tried | The allowance runs out inside the look-ahead, which reads as its body's failure (`Matching/Matcher.cs:783-788`), so the negative assertion holds and the row matches: the look-around flip |
| L02 as L01, /u | `limit:steps` at 10⁶ | As L01 | As L01 |
| L03 `^(?=(?:(a+)+b\|a*c))` on a³⁰`c` | `limit:steps` at 10⁶ | As L01 | No match, where the specification matches at 0 |
| L04 `$(?<=(?:b(a+)+\|ca*))` on `c`a³⁰ | `limit:steps` at 10⁶ | `$` holds at 31 only; read right to left, `(a+)+` visits the 2³⁰ − 1 compositions of the suffixes of a³⁰ before `b` fails | No match, where the specification matches at 31 |
| L05 `$(?<!(?:b(a+)+\|ca*))` /u on `c`a³⁰ | `limit:steps` at 10⁶ | As L04 | A match at 31, where the specification has none |
| W01 `(?:(a+)+b\|a*c)` on a³⁰`c` | `limit:steps` at 10⁶ | From index 0, the first alternative visits 2³⁰ − 1 compositions before the second matches | The exhausted alternative reads as failure, the spent allowance fails the second too, and a later start position matches (`Matching/Matcher.cs:317-326`): a wrong index |
| W02 as W01, /u | `limit:steps` at 10⁶ | As W01 | As W01 |
| G01 `[a]*` on a¹⁰⁰⁰⁰⁰⁰¹ | `limit:steps` at 5 × 10⁴ | RepeatMatcher invokes the atom once per character: ≥ 10,000,002 ≥ 5 × 10⁶ | A match of length 10,000,000: the greedy loop stops when the allowance is spent and offers the shorter run (`Matching/Matcher.cs:587-597`); a class takes the same single-code-point path a literal does (`Matching/Matcher.cs:541-563`) |
| G02 as G01, /u | `limit:steps` at 5 × 10⁴ | As G01 | As G01 |
| G03 `^[a]*$` /u on a¹⁰⁰⁰⁰⁰⁰¹ | `limit:steps` at 5 × 10⁴ | As G01 | No match |
| M01 `(?:a\|ab)*c` on (`ab`)¹⁰⁰⁰⁰`c` | `limit:frames` at 100 | Each completed iteration leaves RepeatMatcher's "stop here" alternative pending until the match ends: 10,000 ≥ 100 × 100. O14 is its small instance | No ceiling: completes, holding its iterations on the general quantifier's heap stack (`Matching/Matcher.cs:423`) |
| M02 `(?:(a)\|b)*c` on a¹⁰⁰⁰⁰`c` | `limit:trail` at 100 | Each iteration writes capture 1 while its "stop here" alternative is pending, so the write must be undoable: 10,000 ≥ 100 × 100. Frames stay far below their ceiling. O15 is its small instance | No ceiling: completes, cloning the capture array per write (`Matching/MatchState.cs:84-90`) |
| N01, N07, N13: 511 nested `(`, `(?:`, `[` (/v) around `a`, on `a` | A match, every group `0,1` | Depth 512, at the limit; a few invocations per level | Accepted and matched |
| N02, N08, N14: 512 nested, `compile` | `limit:nesting` | Depth 513, one past the limit | Accepted: no limit (`Parsing/RegexParser.cs:155-165`) |
| N03, N09, N15: 512 nested, `validate` | `limit:nesting` | As N02 | No validate entry |
| N04, N10, N16: 20,000 nested, `compile` | `limit:nesting` | Far past the limit | The parser's recursion ends the process with a stack overflow |
| N05, N11, N17: 1,000,000 nested, `compile` | `limit:nesting` | Far past the limit | As N04 |
| N06, N12, N18: 1,000,000 nested, `validate` | `limit:nesting` | Far past the limit | As N04, through `RegexParser.Parse` |
| F01 `\B`×100,000 on the empty string | `match 0,0` at 10⁸ steps | About 10⁵ invocations ≤ 10⁶ | The sequence's continuation chain recurses once per term (`Matching/Matcher.cs:257-267`) and ends the process with a stack overflow |
| F02 as F01, /u | `match 0,0` | As F01 | As F01 |
| F03 `(?:a)`×100,000 on a¹⁰⁰⁰⁰⁰ | `match 0,100000` | About 2 × 10⁵ ≤ 10⁶ | As F01 |
| F04 `(?:a\|b)`×100,000 on a¹⁰⁰⁰⁰⁰ | `match 0,100000` | About 2 × 10⁵ ≤ 10⁶; one pending alternative per term, far below the frame ceiling | As F01 |
| V01 `[` `\p{L}`×50 `]` /v, `compile` | `limit:compile` at 300 | 50 × 684 = 34,200 ranges ≥ 100 × 300; the pattern is 252 code units | Compiles: compilation is not charged |
| V02 as V01, /iv | `limit:compile` at 300 | As V01; folding touches them again | Compiles, folding each operand one code point at a time (`Parsing/RegexParser.cs:1067-1076`) |
| V03 as V01, /iu | `limit:compile` at 300 | As V01 | Compiles |
| V04 `[` `\p{RGI_Emoji}`×50 `]` /v, `compile` | `limit:compile` at 1,000 | 50 × 2,760 = 138,000 strings ≥ 100 × 1,000; the pattern is 652 code units | Compiles, de-duplicating strings by list search (`Parsing/RegexParser.cs:822-826`) |
| V05 as V04, /iv | `limit:compile` at 1,000 | As V04 | As V04 |
| O01 `(?:(a+)+b\|a*c)` on a¹²`c` | `match 0,13 -` at 10⁸ steps | Fewer than 2¹⁵ invocations | The same answer |
| O02 `^(?!(?:(a+)+b\|a*c))` /u on a¹²`c` | `nomatch` | Fewer than 2¹⁵ | The same answer |
| O03 `$(?<=(?:b(a+)+\|ca*))` on `c`a¹² | `match 13,13 -` | Fewer than 2¹⁵ | The same answer |
| O04 `(a+)+b` on a¹²`b` | `match 0,13 0,12` | Fewer than 10² | The same answer |
| O05 `[a]*` on a¹⁰⁰⁰⁰⁰⁰¹ | `match 0,10000001` at 2 × 10⁹ steps | About 10⁷ ≤ 2 × 10⁷; as a run (section 3), one frame and no trail cell per character, against the ordinary ceilings of 4,194,304 frames and 8,388,608 trail cells, which a frame or a cell per character would pass | A shorter match (as G01) |
| O06 `^[a]*$` /u on a¹⁰⁰⁰⁰⁰⁰¹ | `match 0,10000001` | As O05 | No match (as G03) |
| O07 `(?<a>x)\|(?<a>y)` on `y` | `match 0,1 - 0,1` | Fewer than 10² | Refused: a name declared twice (`Parsing/RegexParser.cs:140-141`) |
| O08 `(?:(?<a>x)\|(?<a>y))\k<a>` on `yy` | `match 0,2 - 0,1` | Fewer than 10² | As O07 |
| O09 `b`, `anchored@0`, on `ab` | `nomatch` | Fewer than 10² | No anchored argument; a search finds `b` at 1 |
| O10 `b`, `anchored@1`, on `ab` | `match 1,2` | Fewer than 10² | The same answer, by search |
| O11 `a`, `match@1`, on `aba` | `match 2,3` | Fewer than 10² | The same answer |
| O12 `[0-9]+x` on `x`1¹⁰⁰⁰⁰⁰⁰, meter 10⁴ | `meter-exception` | From index 1 the run reaches 10⁶ characters and gives each back: ≥ 2 × 10⁶ ≥ 100 × 10⁴ | No meter. Each start position's search stays inside its own allowance, so the call does work quadratic in the subject's length before it answers no match |
| O13 `[0-9]+x` on 1¹⁰⁰`x`, meter 10⁶ | `match 0,101` | Fewer than 10³ ≤ 10⁴ | The same answer |
| O14 `(?:a\|ab)*c` on (`ab`)¹⁰⁰`c` | `match 0,201` | About 10³ | The same answer |
| O15 `(?:(a)\|b)*c` on a¹⁰⁰`c` | `match 0,101 99,100` | About 3 × 10² | The same answer |
| O16 `[` `\p{L}`×50 `]` /iv on `A` | `match 0,1` | Fewer than 10²; compilation unbudgeted | The same answer |
| O17 `[` `\p{RGI_Emoji}`×50 `]` /v on U+1F600 | `match 0,2` | Fewer than 10²; compilation unbudgeted | The same answer |
| O18 `\k<b>(?<a>x)` /u, `compile` | `syntax` | (none) | Refused at compile (`Matching/Matcher.cs:712-718`), though `RegexParser.Parse` accepts it |
| O19 as O18, `validate` | `syntax` | (none) | No validate entry |

## 5. The engines' cases

**Broiler.VM** (`vm_*`): every row but the `validate` rows, O05, O06, O12, O16 and O17, whose shapes the G and V
rows already carry to the engine, or which need a per-case meter no engine passes.

- K, A, L and W rows run on the library's subjects with a fuel allowance equal to the library's step ceiling, in
  the same units (decision D8), and are declared `exhausted`: the program's own work only spends the allowance
  sooner. Broiler.VM's matcher charges the same work today, so the outcome is the same at its base.
- F, V and O rows, and N at the limit, complete with ample fuel. The N rows past the limit end with an uncaught
  `SyntaxError`, Broiler.VM's mapping of the nesting status.
- G rows complete as runs (section 3, the third presumption). Broiler.VM's matcher runs `[a]*` as `Op.Run` at its
  base, and the backend does so after the cut-over by that presumption: one frame and no trail cell per character,
  against the 2²⁰ frames and 2²¹ trail cells the engine passes, which the loop form's frame per character would
  pass after 2²⁰ characters. Their fuel allowance is 10¹⁰. `String.prototype.repeat` charges the subject's
  construction one unit per character, and the run charges one unit per character it examines (decision D8):
  about 10⁷ units each, and with what else the call charges, a few times 10⁷, under a hundredth of the allowance.
- M rows run on subjects four times or more past the ceilings the engine passes (frames 2²⁰, trail 2²¹, decision
  D10): at least one pending alternative and one undoable write per iteration.
- O07 and O08 complete after the cut-over; Broiler.VM's matcher refuses a name declared twice before it, which its
  rule's part D admits as a listed correction.

**Broiler.JS** (`js_*`): the classes W, G, L, N, F, M, V and O. K and A are judged at library level only, because
Broiler.JS passes no meter (decision D10).

- W and L run at 24 repetitions of `a` rather than 30, so that the specification's answer, a few times 2²⁴
  invocations, completes inside the wall-clock limit; they are declared to complete with that answer.
- G, F, V, O and N at the limit complete with the specification's answer, the G rows as runs (section 3) inside
  the frame and trail ceilings Broiler.JS passes, which are Broiler.VM's (decision D10); N past the limit throws
  `SyntaxError` (decision D7); M runs on the same engine-scale subjects as Broiler.VM's and throws `RangeError`
  (decision D10).

## 6. What this corpus does not decide

- A statement about speed: nothing here is timed, and the wall-clock limit is a bound on a hung case.
- The vectors (SJL-11.3) and the reference arms ([`reference-arms.md`](../../docs/evidence/sjl-11-001/reference-arms.md)),
  which have their own populations; the arms' class P3 reads the rows declared to complete.
- Each engine's judgement of its own programs, which its own rule makes.
