# Bundle sjl-11-001's reference-arm population, comparison and classification

The scripts of [`docs/evidence/sjl-11-001/reference-arms.md`](../../docs/evidence/sjl-11-001/reference-arms.md)
(template T4), written at task 2.13 of SJL-11.2, 2026-11-01. The arms they feed and read are under
[`tests/ReferenceArms/`](../../tests/ReferenceArms/README.md). Nothing here is packed or referenced by the library.

| File | What it is |
|---|---|
| `generate_population.py` | Classes P1 to P7, one gzip file each, and `manifest.json` |
| `splitmix64.py` | SplitMix64 and the four readings of a draw; Broiler.Unicode's, copied (its docstring names the blob) |
| `population_format.py` | The string notation and the gzip files, shared by the three scripts |
| `compare.py` | S against each arm run, exactly; writes `summary.json` and `differences.tsv.gz`; classifies nothing |
| `classify.py` | The classification, from the differences, the correspondence's rows and the arbiter's answers |
| `stops.py` | The stop list, from a calibration's timing files (`tests/ReferenceArms/README.md`, "The stop list, and the limits") |
| `run.sh` | Every step in order, each logged with its command; it fails when a step fails, the classifier's crash among them; `--calibrate` writes a stop list instead |
| `reduce.py` | The reduction of the inputs the arbiter cannot reach (written 2026-11-15): each such exec's subject cut to the longest prefix the arbiter reaches, written as a population the arms, the comparison and the classifier run over unchanged, and each difference read against its reduced case (its docstring) |
| `exclusion.py` | The exclusion from P4 by the owner's ruling of 2026-11-16 (written that day): the limbs the 19 were found by, the pattern's limb read from the pattern's text, the arbiter's limb asked of every exec input of such a pattern, the run's limb (a stop against an answer there), and `select`, which writes the listing (its docstring) |
| `p4-excluded.tsv` | The listing `exclusion.py select` wrote and `classify.py --exclude` reads: the 19 P4 patterns the ruling excludes, by id, pattern, quantifier and the flag sets where the three limbs hold |
| `controls.py` | The two negative controls of `reference-arms.md` (written 2026-11-17): the control copy of an arm, the runs it is compared through, and the check that the comparison reports its case and the classifier lists it unclassified, and, at review, that unnamed it is class (c) and the arm's own answers classify as the run's (its docstring) |
| `tests/` | SplitMix64 against its published outputs; the format, the readers and P4's draws; the classifier; `run.sh`'s reading of the classifier's exit; the reduction's reading (added 2026-11-15); the exclusion and the filed listing (added 2026-11-16); `run.sh`'s three classifications, the reduction's check and the controls (added 2026-11-17) |

## Commands

```sh
python3 -m unittest discover -s scripts/reference-arms/tests
#   two tests are skipped unless BROILER_UNICODE=<Broiler.Unicode checkout> (P5's) and
#   BROILER_VM_CORRESPONDENCE=<correspondence.md> (the classifier's words against the filed rows) are set; run.sh sets both
python3 -I -B scripts/reference-arms/generate_population.py --out OUT/population \
    --test262 <test262 at ccaac100> --unicode <Broiler.Unicode checkout> [--vectors <conformance/vectors/regexp>]
dotnet tests/ReferenceArms/S/bin/Release/net10.0/Regex.ReferenceArms.S.dll --population OUT/population --out OUT/runs
#   ... V, J, and J again with --culture tr-TR and --culture lt-LT (tests/ReferenceArms/README.md)
python3 -I -B scripts/reference-arms/compare.py --population OUT/population --runs OUT/runs --out OUT/comparison
python3 -I -B scripts/reference-arms/classify.py --comparison OUT/comparison --runs OUT/runs \
    --broiler-js <Broiler.JS checkout> --commit <the commit the bundle names> \
    --correspondence <Broiler.VM>/docs/evidence/sjl-11-001/correspondence.md \
    --unicode-pins <Broiler.Unicode>/data/unicode/pins
#   since 2026-11-17 run.sh then runs the reduction (reduce.py prefixes, the arms over OUT/reduction/population,
#   compare.py, classify.py, reduce.py read), classify.py again with --reduction OUT/reduction
#   --exclude scripts/reference-arms/p4-excluded.tsv --out OUT/classification, and the negative controls (controls.py);
#   its header gives every command
```

## The files

**A string** (a pattern or a subject) is written as segments joined by `+`, each four upper-case hexadecimal digits
per UTF-16 code unit, optionally followed by `*n` (the segment n times); the empty string is `.`. A lone surrogate is
a code unit like any other. **A population file** `<class>.tsv.gz` has a `#` header and one tab-separated line per
case: `id`, `op` (`accept` or `exec`), `pattern`, `flags` (`.` for none), `subject` and `lastIndex` (`-` for
`accept`), `policy` (`default`, or `ample:<nesting>` for P3). Gzip members carry no name and a zero time stamp, so two
runs write the same bytes; `manifest.json` names every input by its SHA-256, commit or tree, and each class's count and
SHA-256. A population written with `--classes` or `--reduced`, or without P1, is **partial**, and the manifest says so.

## The classes

- **P1**: every vector of every `*.vectors` file of the `regexp` directory given, in file and line order: the first
  two `str` arguments of `in` are the pattern and the flags; a third `str` is the subject and an `int` after it the
  lastIndex (0 when none). A vector with a third string is an exec, one without an acceptance case. The rule names the
  corpus revision bundle sjl-11-001 names: **revision 32, at Broiler.JS `b2c2e2243a086285d806866a64b83297791a8ec5`**
  (Broiler.JS#1089 on its `main`; `tests/Vectors/regexp/` is a byte-for-byte copy). The unjudged runs read P1 from that
  commit's `conformance/vectors/regexp/`, and so will the judged run; nothing of an unjudged run is committed as
  evidence. *(Dated note, 2026-11-02, task 2.13: this item read "**the judged run waits for revision 32**, and until
  then P1 is read from the `sjl-11-3-regexp` branch's revision as a smoke run, never committed as evidence". Revision
  32 is on Broiler.JS's `main` since `b2c2e224`, and the two runs of 2026-11-02 at `a113c91` read P1 from it; the
  judged run waits no longer for the revision but for what `docs/roadmap.md`'s record of task 2.13 says, the
  correspondence's rows for the differences class (c) still holds among it.)*
- **P2**: every regular-expression literal and every `RegExp(...)` call whose first one or two arguments are string
  literals, in every `.js` file but `_FIXTURE` files under `test/built-ins/RegExp`, `test/language/literals/regexp`,
  `test/annexB` and `test/built-ins/String/prototype/{match,matchAll,replace,replaceAll,search,split}` of test262 at
  `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e` (the manifest records the git tree of `test/`, which names the copy's
  content whatever its origin). **The declared grammar** (`scan_javascript`): comments, single- and double-quoted
  string literals (their values by 12.9.4's escapes), template literals skipped, and a `/` begins a literal when the
  previous significant token is one of `(,=:[!&|?{};+-*%<>~^`, a keyword of `return typeof instanceof in of new delete
  void throw case do else yield await`, or nothing. Each distinct (pattern, flags) of a file is an acceptance case
  and an exec from lastIndex 0 against every distinct string literal of its own file, in order, until the file's
  **declared cap of 4,096 (pattern, subject) pairs**.
- **P3**: every row of `tests/Adversarial/corpus.tsv` whose declared status is `match ...` or `nomatch`, under the ample
  policy: no meter and no step ceiling, the library's frame and trail maxima, the row's nesting limit. `match@k` is an
  exec under the row's flags with `g` added when k > 0, so that RegExpBuiltinExec reads lastIndex k; `anchored@k` adds
  `y`.
- **P4**: 2^16 patterns drawn with seed `0x534A4C31312D3031` ("SJL11-01"), each under the flag sets `""`, `i`, `m`,
  `s`, `u`, `v`, `iu`, `iv`, `y` and `d`, as an acceptance case and an exec against each of 16 subjects.
  **The draw procedure.** One SplitMix64 stream from the seed is read in this order, pattern after pattern: the
  pattern's draws (`PatternDraw`: a disjunction of 1 to 3 alternatives, weights 8, 2, 1; an alternative of 0 to 4
  terms, weights 1, 5, 4, 2, 1; a term an atom, an assertion or a look-around, weights 10, 2, 1, an atom quantified
  one time in three, a look-around one in six; nesting to depth three; every choice of a production a draw read by
  weight, a draw modulo the weights' sum read against their running total, or a choice among n, the draw modulo n);
  then, for each of the 16 subjects in turn, its length (a draw: 0 to 32 characters), each character (a draw over the
  pattern's subject alphabet: its code points in order of first appearance, each followed by its case variants of
  `CASE_PARTNERS` or its ASCII swap, then U+1D400, U+1F600, U+10400, U+10428, U+1D306 and the lone surrogates U+D83D,
  U+DE00, U+D834, U+DF06), and its lastIndex (a draw: 0 to its length in code units; RegExpBuiltinExec reads it under
  `y` only, as no set holds `g`). The grammar's atoms, classes (with the `v` flag's set operations, nested classes
  and `\q{}`), escapes, property escapes, back-references, groups (named, with modifiers) and quantifiers include
  Annex B's extensions and near-misses the grammar refuses (`ANNEX_B_ESCAPES`, the `syntax` atoms, `{2,1}`, `{,2}`).
  No draw reads the running Python's Unicode data. `tests/test_splitmix64.py` checks the generator against the
  published reference outputs.
- **P5**: every code point c from U+0000 to U+10FFFF as the pattern `c` and as `[c]`, written raw, each under `i`, `iu`
  and `iv`: an acceptance case and an exec from 0 against each member of c's simple-folding orbit (CaseFolding.txt
  status C and S, c included) and c's simple upper case (UnicodeData.txt field 12), without repeats, read through
  Broiler.Unicode's `unicoderef.py` over its archived UCD 17.0.0 (the manifest names the checkout's commit and the
  blobs).
- **P6**: every line of Broiler.Unicode's U2 (`names_population` of its `generate_population.py`, at its seed): a lone
  name as `\p{name}` and `\P{name}`, a pair as `\p{name=value}` and `\P{name=value}`, each under `u` and `v`, an
  acceptance case.
- **P7**: every code point c in a group name, first (`(?<c>.)`) and later (`(?<ac>.)`), written raw and escaped
  (`\uXXXX` in the BMP, surrogates included, `\u{X}` above it), each without flags and under `u`: an acceptance case.

## The comparison and the classification

`compare.py` aligns each class's population with S's answers and each arm run's, line for line (a line out of step
stops it), and compares the answer columns exactly: the charges are summed in `summary.json` and never compared.
`differences.tsv.gz` holds one line per difference: the case's id, class, operation and inputs, the run, S's answer
and the arm's. It classifies nothing.

`classify.py` was written before any judged run. For each difference it asks the arbiter, `regexpref.py`
(`ref:regexpref@1`) taken by `git archive` from the Broiler.JS commit `--commit` names (with `unicoderef.py` and the
corpus's `unicode.pin`), with a cut-off of 1,000,000 Matcher invocations, and classes the difference by the rule:
(a) the arm wrong, S right, and a correspondence row whose class names the arm's engine as corrected (in the words
`ROWS` gives, which the classifier checks against the correspondence) recording that correction. Each predicate reads
the row's own correction, the direction of the difference and its construct, never a feature of the input alone:

| Row | Arm | The difference it holds |
|---|---|---|
| S1 | V | V refuses a duplicate name S accepts |
| S2a | J | J accepts a name repeated only once decoded, `(?<\u0061>x)(?<a>y)`, the arbiter's reason a duplicate name |
| S5 | J | J refuses `\k` in a class in a pattern with a group name, which S accepts (decision D-s5) |
| S7 | J | without `u` or `v`, J accepts a quantified look-behind, the arbiter's reason nothing to repeat or a braced quantifier |
| S8 | J | under `v`, J accepts a class the arbiter refuses for a ClassSetSyntaxCharacter or a reserved double punctuator |
| S10 | V | V refuses a four-digit lead escape before `\u{`, under `u` or `v` |
| S10 | J | an exec of `[\uD83D\u{DC38}]`'s class case: a lead escape then a braced trail in a class, under `u` or `v` |
| M2a | J | under `tr-TR` or `lt-LT`, `i` without `u` or `v`, U+0130 or U+0131 in the input, J's answer not its invariant one |
| M2b, M2 | J | `i` (the flag or a modifier) without `u` or `v`, an exec both answer, a case pair the arbiter's UCD and Canonicalize hold differently in the input (U+1F80 to U+1FFF: M2b) |
| M3 | J | without `u` or `v`, an exec both answer: under `m`, `^` or `$` and U+2028 or U+2029; without `m`, `$` and a final LF |
| M4 | V, J | from a lastIndex inside a pair: V's match (its start steps back); J's match where S has none |
| M7 | V, J | under `u` or `v`, the arm matches where S does not: a back-reference with a surrogate in the subject (V); one inside a look-behind (J) |
| B2 | J | under `u` or `v`, a nested quantifier, S's match against J's null or later start |
| B8 | J | under `u` or `v`, the harness's stop of J against S's answer, the pattern a sequence of 10,000 terms or more |
| F3 | J | J accepts what S refuses, a modifiers group with a letter that is no modifier, `(?x:a)` (decision D-widen) |
| S7 | J | *(@4)* a P1 vector without `u` or `v` J accepts: a quantified `^`, `$` or `\b`, the arbiter's reason nothing to repeat (the row's widening of 2026-11-01) |
| S8 | J | *(@4)* a P1 vector under `v` J accepts, the arbiter's reason `grammar:class-set-operator` (`[a-z&&b]`, which row S12 leaves to S8) |
| M4 | J | *(@4)* from a lastIndex inside a pair: J's capture from the trail where S's holds the pair, or J's null where S matches at lastIndex (the row's widening of 2026-11-16); *(@5)* only for the widening's two vectors, `(.)` under `gdu` and `[\ud834\udf06]` under `gu` |
| S8a | V | *(@4)* under `iv`, `\w` the first operand of a class difference, V's match taking a letter `A`-`Z`, `a`-`z`, U+017F or U+212A S's answer does not |
| S12 | J | *(@4)* J accepts what S refuses, the arbiter's reason one of the row's `grammar:` reasons under the flags the row names for it; not the P1 vectors rows S7 and S8 hold |
| S13 | J | *(@4)* J accepts what S refuses, the arbiter's reason one of the row's `early:` reasons under the flags the row names for it |
| S15 | J | *(@4)* in P4, under `i`, J refuses what S accepts, a quantifier bound past 2^31 - 1, and the comparison holds no refusal of the same pattern by the same run without `i` |
| S14 | J | *(@4)* without `u` or `v`, J refuses what S accepts, under no flag, `s`, `i` or `m` alone, or under other flags where the same run refused the pattern with no flag; *(@5)* not a bound past 2^31 - 1 under `i` that the comparison does not show refused without `i` |
| M8 | J | *(@5)* an exec both answer, not with the group names alone differing: `\b` or `\B` outside a class and a non-ASCII letter (the arbiter's UCD) in the subject beside either side's match, or anywhere in it under a look-around; `i` and U+0130 or U+0131 in the pattern or beside the matches; an empty `\q{}` in a class under `v`; or in `tr-TR` P5's `I` and `[I]` against `i`, `i` and `[i]` against `I` |
| M9 | J | *(@4)* the row's one input, `(?<$x>){1,}?\B\|(?!\u212A)` under `y` from 2 of `X\`, J throwing `ArgumentOutOfRangeException` |
| M10 | J | *(@7)* without `u` or `v`, an exec both match with the same captures and only the names differ, each name S gives that J does not rewritten in J's at the same capture (the same group): `$` as `(?=$\|\r)` under `m` with no inline modifier group, as `\z` with one and without `m`, as `(?=[\n\r\u2028\u2029]\|\z)` with one under `m`; under `i` a character of the name as its Canonicalize class in the arbiter's tables, the character first, where that class holds another member and a non-ASCII one (`[πΠϖ]`), or as `(?-i:…)` over that class, each member escaped as the translator escapes it, where .NET's invariant lowercase folds the character into another class (`k` as `(?-i:[\x4b\x6b])`) |
| M11 | J | *(@7)* without `u` or `v`, an exec both answer, a group (not a look-around) quantified by `?`, `??`, `{0,1}` or `{0,1}?`, and the arbiter's answer to the row's reading (`(G)?` as `(?:(G)\|)`, `(G)??` as `(?:\|(G))`) is J's, up to row M10's names, and not S's |
| M12 | J | *(@7)* as M11, one reading with every such escape read: `\p{…}` or `\P{…}` in a class (a General_Category name) as the BMP code units of that category in the arbiter's UCD or the others, a `-` after it as `\-`, and `\a` in a class as `\x07` |
| M13 | J | *(@7)* as M11, the reading a class with `-[` after its first member as .NET's subtraction, `(?:(?![B])[A])` |
| M14 | J | *(@7)* as M11, the reading `\--` before a member in a class as `\-\-` |
| M15 | J | *(@7)* under `tr-TR` or `lt-LT`, the flag `i` without `u` or `v`, an exec both answer, `i` or `I` in the input and neither U+0130 nor U+0131, and no difference of run J (the invariant culture) on the same input |
| M8 | J | *(@8, its note)* `\b` or `\B` outside a class as the text lexes a class without `v`: a class ends at its first unescaped `]`, a `[` inside it a member |
| M12 | J | *(@8, its note)* the reading also writes a `-` right before `\p{…}` or `\P{…}` that follows `\d`, `\D`, `\w`, `\W`, `\s` or `\S` as `\-` |
| M13 | J | *(@8, its notes)* a nested class ends at its first `]`, even right after its `[^` (`[\w--\D--[^]]`), under `RegexOptions.ECMAScript`: no `s` and no modifier group; a nested `[]`, or a nested `[^]` under `s` or with a modifier group, read as `@7` read it, its `]` a member |
| M15 | J | *(@8, its note)* or a difference of run J on the same input whose answer is S's match with only the names rewritten as row M10 reads them, at the same groups |
| M16 | J | *(@8)* without `u` or `v`, an exec both answer, `\c` that no ASCII letter follows, a `GapScan` shape in the arbiter's tree of the pattern (a quantifier whose maximum is above 1 over a body that can match both the empty string and a character, or a capturing group or a back-reference inside a look-behind), and the arbiter's answer to the reading (each such `\c` as `\\`) is J's, up to row M10's names, and not S's |
| M17 | J | *(@8)* without `u` or `v`, an exec both answer, a group name written with `\u{…}` outside a class, and the nullable shape above in the arbiter's tree |
| M18 | J | *(@8)* without `u` or `v`, an exec both answer, `\k<…>` outside a class whose name, written with `\u{…}`, is the value of a group's name |
| M19 | J | *(@8)* without `u` or `v`, an exec both answer, `\0` and one or two octal digits outside a class, inside the body of a quantifier of the nullable shape in the arbiter's tree |
| M20 | J | *(@8)* without `u` or `v`, an exec both answer, in a class `\0`, one octal digit and `\d` |
| M21 | J | *(@8)* without `u` or `v`, without `s` and with no modifier group, an exec both answer, `\S` in a class, and the arbiter's answer to the reading (`\S` as `\u0000-\u0008\u000E-\u001F\u0021-\uFFFF`, a `-` beside it as `\-`) is J's, up to row M10's names, and not S's |
| M22 | J | *(@8)* without `u` or `v`, an exec both answer, `()` or `(?<name>)` outside a class under `+?` or `{1,}?`, and a `\|` outside every class and group |
| M23 | J | *(@8)* under the flag `m`, without `u` or `v`, an exec both answer, `^` or `$` a member of a class, and the arbiter's answer to the reading (`^` as the characters `(?<=^\|\r)`, `$` as `(?=$\|\r)`) is J's, up to row M10's names, and not S's |
| M24 | J | *(@8)* without `u` or `v`, under `s` or with a modifier group, an exec both answer, a back-reference in the arbiter's tree of the pattern |
| M25 | J | *(@8)* under the flag `i`, without `u` or `v`, an exec both answer, a letter outside a class inside a modifier group that removes `i` whose Canonicalize class holds another member and a non-ASCII one, and the arbiter's answer to the reading (the letter as that class) is J's, up to row M10's names, and not S's |
| M26 | J | *(@8)* under the flag `i`, without `u` or `v`, an exec both answer, `\W`, `\p{` or `\P{` in a class as the text lexes it without `v` |
| M27 | J | *(@9)* without `u` or `v`, under `s` or with a modifier group the text lexes, an exec both answer, in the arbiter's tree of the pattern a quantifier with a minimum of 1 or more and no maximum (`+` or `{n,}`, lazy or not) over `\D` or a negated class whose members are ASCII letters, digits, `_`, `\d` or ranges within `0-9`, `A-Z` or `a-z`, which ends a term (through groups, capturing or not, `(?:…)` included, not a look-around or a modifier group that adds or removes a flag, and quantifiers whose maximum is 1) whose next term begins with `\b` (itself, or a group so read or positive look-ahead of one alternative, or a quantifier with a minimum of 1 or more, that begins with it), and the arbiter's answer to the reading (each such quantifier greedy and followed by `(?!X)`, X its atom: `\D+?` as `\D+(?!\D)`) is J's, up to row M10's names, and not S's; tried after M26, so an input row M8 takes stays row M8's |
| B1 | J | *(@8, with `--reduction`, the revision of 2026-11-16c)* P3's `O06`, the input the row's widening names: the arbiter cannot reach it, the reduced case agrees with the arbiter, neither side stopped at a bound it declares, every other arm's run answers it as S does, and S's answer is the whole subject, as `whole_run_answer` gives it at the input and the reduced case's answer at the prefix: class (a) "beyond the arbiter, by row" |

Row S1 is not a correction of Broiler.JS (its class names Broiler.VM and preview.4), and row S2b's literal path is not
in arm J. (a') arm J, an exec both answer, under `i` without `u` or `v` where a code point Unicode 16.0 or 17.0 gave a
case pair is in the pattern or the subject (row U3). (b) both sides stopped by their own bounds, or a refusal for
nesting or a limit on one side and the other side's answer the arbiter's (rows S3, B6, B7, and decision D10 for J),
and row M4's empty match from a lastIndex inside a surrogate pair, which the arbiter cannot answer (decision D41).
(c) everything else, an unreachable arbiter included: a stop on one side against an answer the arbiter cannot check
in its cut-off is class (c) until reduced to an input it can, as the rule's arbiter clause reads. It refuses to run
(exit 2) when a row it names is missing from the correspondence or its class does not hold the row's words, exits 3
when it fails (an exception), and otherwise 0 when no difference is (c) and 1 when one is; it writes
`classification.json` last. `run.sh` fails on 2, on 3, and on a run that leaves no `classification.json`. The case
an arm's run file names as its negative-control case is listed unclassified. `classification.json` names its inputs
by what decides them and by no path: the arbiter by its commit and blobs, the correspondence by its file name, git
blob and SHA-256, so that the same inputs give the same bytes wherever they are and from any working directory.
*(Revised 2026-11-02, before any judged run: it held the correspondence as `"file"`, the path as given on the command
line, so the digest of one classification changed with where its inputs were.)*

The harness's stop (`limit:harness`, `tests/ReferenceArms/README.md`, "The stop list, and the limits") is one answer
whichever limit fired, whether the worker crashed or whether the stop list held the case, and the older `crash:exit-N`, `limit:wall-clock` and `limit:timeout` are read as it:
arm J's stop is (a) under row B8 on that row's input (above) when S's answer is the arbiter's, (b) under decision D10
on any other input S answers as the arbiter does, and (b) where S stopped too; S's or arm V's stop is (c), as neither
declares a bound it stands for. *(Revised 2026-11-02 at task 2.13's second review, before any judged run,
`CLASSIFIER_VERSION` @3: a crash was "no row names" and a wall-clock stop (b), so one input's class depended on which
came first, and row B8 was not read. The correspondence is read at Broiler.VM's `5d025cc`: this table's row S5 read
"| S5 | V | V accepts `\k` in a class in a pattern with a group name, which S refuses |", which decision D-s5 and task
2.12 retired, and rows B8 and F3 are new.)* *(Revised 2026-11-02 at the review of task
2.13, before any judged run: the predicates read only a flag or a character, rows S1 and S2b were read for arm J, and
class (b) admitted "the arbiter past its cut-off"; `classify.py`'s docstring quotes what changed.)*

*Dated note, 2026-11-16 (`CLASSIFIER_VERSION` @4, written after task 2.13's runs, their classification and the
reduction of 2026-11-15 were seen).* The paragraphs above describe @3. @4 reads the correspondence at Broiler.VM's
`7c65a8febacdcf8bda2d848d21ac2e7748696359` (Broiler-Platform/Broiler.VM#211's merge; `classify.py`'s `CORRESPONDENCE`, which
`classification.json` names as `rows-read-from` beside the blob of the file a run is given), whose rows S8a, S12 to
S15, M8 and M9 are new and M4 is widened; the rows marked *(@4)* in the table are its predicates, each read from the
row's text, after @3's so that every difference @3 classed keeps its class and row. `classify.py --reduction DIR` reads
`reduce.py`'s output for the comparison as the rule's revision of 2026-11-16 (option R,
[`reference-arms-revised-2026-11-16.md`](../../docs/evidence/sjl-11-001/reference-arms-revised-2026-11-16.md)) does: a
difference the arbiter cannot reach is classed as its reduced difference when the reduced case still differs; class
(b') "beyond the arbiter" when S and the arm agree at the prefix, S's answer there is the arbiter's and one side
stopped at the input at a bound it declares; and (c) otherwise, an input no prefix reaches among them. Without
`--reduction` no difference is (b'). `run.sh` does not pass it: the reduction is run after a comparison, as the
roadmap's record of 2026-11-15 says.

*Dated note, 2026-11-16, at the review of @4 (`CLASSIFIER_VERSION` @5, before any judged run).* The table's row M8
read, at @4, "an exec both answer: `\b`, `\B`, `\w`, `\d`, `\s` or a complement in the pattern and a code point past
ASCII in the subject; `i` and U+0130 or U+0131 in the input; ...". That took differences where only the group names
differ, complements the row does not name, and subjects whose only non-ASCII units are surrogates or emoji. @5 reads
the row's list where it bears on the difference (the row above, and `classify.py`'s docstring): the letters are the
General_Category L code points of the arbiter's UCD (`Arbiter.letters`); "beside" is a code unit from one before the
earliest start of either side's match to the latest end, or the whole subject where the pattern has a look-around;
and "`\w`, `\d` and `\s` beside them" is read as beside `\b` and `\B`, which the row's text does not settle. Rows M4
and S14 are narrowed as their rows above say; no other row changed. With `--reduction`, an arm J `limit:harness` is
read as the stop list's stop: that holds only while no case of the run reached a backstop, a calibration's time limit
or a crash, which the run's facts do not distinguish.

*Dated note, 2026-11-16, after the owner's ruling of that day (`CLASSIFIER_VERSION` @6, written after task 2.13's
runs, their classification and the reduction of 2026-11-15 were seen).* The owner excluded from population P4 the 19
patterns the ruling names, whose arbiter cost is in the pattern and whose inputs held class (c), enumerated by id in
`p4-excluded.tsv`
([`reference-arms-revised-2026-11-16b.md`](../../docs/evidence/sjl-11-001/reference-arms-revised-2026-11-16b.md)).
`classify.py --exclude p4-excluded.tsv` classes every difference of a listed pattern, under every flag set, by
acceptance and by exec, a reduced case `~p` among them, as `excluded`: listed apart with both answers, counted apart
in `classification.json` (which names the listing by its file name, SHA-256 and count), neither (c) nor an admitted
class, and the arbiter is not asked for it; a listed id whose pattern is not the comparison's refuses the run (exit
2). Without `--exclude` @6 classes every difference as @5 did. `exclusion.py select --population DIR --broiler-js
CHECKOUT --commit SHA --unicode-pins DIR --comparison DIR --out FILE [--workers N] [--cache FILE]` writes the listing
from a population, the arbiter and the run's comparison, as the 19 were found; the run's limb reads that comparison, so
the listing, not the limbs, is the exclusion. The pattern's and the arbiter's limbs alone select 11 patterns more,
which the ruling does not name (the revision's "How the 19 were found, and what the limbs select"). `run.sh` passes
neither `--reduction` nor `--exclude`: both are read after a comparison, as the roadmap's record of 2026-11-16 says.

*Dated note, 2026-11-16, after Broiler.VM's rows M10 to M15 of that day (`CLASSIFIER_VERSION` @7, written after task
2.13's runs, their classification by @5 and @6 and the reduction of 2026-11-15 were seen).* The note of @4 above reads
"@4 reads the correspondence at Broiler.VM's `7c65a8febacdcf8bda2d848d21ac2e7748696359` (Broiler-Platform/Broiler.VM#211's
merge; `classify.py`'s `CORRESPONDENCE`, which `classification.json` names as `rows-read-from` beside the blob of the
file a run is given)", and the notes of @5 and @6 left it so. @7 reads it at Broiler.VM's `44511066bfb38952c12c81557f56a34bd21ee41e`
(Broiler-Platform/Broiler.VM#212's merge, the file's blob `94b860cc`; `classify.py`'s `CORRESPONDENCE`), whose rows M10 to
M15 are new (decision D-jsexec), whose row M8 carries a dated note, and whose row B1 is widened for P3's `O06`. The rows
marked *(@7)* in the table are its predicates, each read from the row's text and tried after @6's, in the order the
correspondence states, so that every difference @6 classed keeps its class and row and an input two of them take is the
first's. Rows M11 to M14 ask the arbiter for its answer to the row's reading of the pattern under the difference's
flags, subject and lastIndex (`Arbiter.reading`, cached with its other answers): a construct the input holds whose
reading does not give J's answer takes nothing. Row M10 reads the names alone, and row M15 the comparison. Row B1's
widening is read but has no predicate: the arbiter cannot reach `O06`, its reduced case shows no difference and neither
side stopped at a bound it declares, so option R keeps it class (c) before any row is read. A run of @7 needs the
correspondence at `44511066` or later: an earlier one lacks rows M10 to M15, and the classifier refuses it (exit 2).

*Dated note, 2026-11-16, at the review of @7 (the tag unchanged, before any judged run).* The table's row M10 read, as
first written, "`$` as `(?=$\|\r)` under `m`" and "under `i` a letter as a class that holds it, another member and a
non-ASCII one (`[πΠϖ]`), or as `(?-i:…)` over its class"; that took `x` as `[xé]` or `(?-i:x)`, which the row says is
not rewritten, `(?=$\|\r)` where a modifier group stands in the pattern, and names swapped between groups of equal
spans, and left out a character of a name that is not a letter, as U+2160. Row M12 read "as M11, the reading `\p{…}`
or `\P{…}` in a class ... and `\a` in a class as `\x07`", each kind read alone and then together, so a reading with
one kind left as the text has it could take an input. The rows above are now the predicates (`classify.py`'s docstring,
"At the review of @7"): row M10's Canonicalize class is the arbiter's (`Arbiter.canonicalize_class`), and its "one .NET's
invariant lowercase folds into another Canonicalize class" stands as the pinned UCD's simple lowercase with U+0130 kept
as itself (`Arbiter.over_folds`), .NET's table not being read. Over task 2.13's run the classification is the same bytes.

*Dated note, 2026-11-16, after Broiler.VM's rows M16 to M26 of that day and the owner's ruling on P3's `O06`
(`CLASSIFIER_VERSION` @8, written after task 2.13's runs, their classification by @5, @6 and @7 and the reduction of
2026-11-15 were seen).* The note of @7 above reads "@7 reads it at Broiler.VM's
`44511066bfb38952c12c81557f56a34bd21ee41e`" and "Row B1's widening is read but has no predicate: the arbiter cannot
reach `O06`, its reduced case shows no difference and neither side stopped at a bound it declares, so option R keeps it
class (c) before any row is read." @8 reads the correspondence at Broiler.VM's `626e4ed88afe8898724bda669e981cf7cf1b98b2`
(Broiler-Platform/Broiler.VM#215's merge, the file's blob `d4605f5c`), whose rows M16 to M26 are new (decision
D-jsrest) and whose rows M8, M12, M13 and M15 carry dated notes. The rows marked *(@8)* in the table are its
predicates: the noted ones replace @7's in place, and M16 to M26 come after M15 in the correspondence's order. M16,
M21, M23 and M25 ask the arbiter for its answer to the row's reading, as M11 to M14 do; M16, M17, M19 and M24 read the
arbiter's tree of the pattern (`Arbiter.parse`). Where a row says "class", the pattern is lexed as the text lexes it
without `v` (`text_tokens`). Row B1 is read by the rule's revision of 2026-11-16c
([`reference-arms-revised-2026-11-16c.md`](../../docs/evidence/sjl-11-001/reference-arms-revised-2026-11-16c.md), the
owner's ruling), with `--reduction` only: `classify.py` checks that the row's line holds its words naming the input
and the mechanism (`check_beyond_rows`, exit 2 otherwise), and reads the other arms' answers from the runs directory
`--runs` names. A run of @8 needs the correspondence at `626e4ed8` or later.

*Dated note, 2026-11-17, after Broiler.VM's row M27 of 2026-11-16 (`CLASSIFIER_VERSION` @9, written after task 2.13's
runs, their classification by @3 to @8 and the reduction of 2026-11-15 were seen).* The note of @8 above reads "@8
reads the correspondence at Broiler.VM's `626e4ed88afe8898724bda669e981cf7cf1b98b2`" and "A run of @8 needs the
correspondence at `626e4ed8` or later." @9 reads it at Broiler.VM's `5e90bfe485aad60b3637d9f69e54ed71adcd73af`
(Broiler-Platform/Broiler.VM#216's merge, the file's blob `28312902`), whose row M27 is new (decision D-jsrest): a loop
over `\D`, or over a negated class of ASCII word characters, right before `\b` without `RegexOptions.ECMAScript`, which
.NET makes atomic. The row marked *(@9)* in the table is its predicate (`loop_made_atomic_before_boundary`), tried after
M26 as the row says. It reads the loop and the `\b` in the arbiter's tree of the pattern (`Arbiter.parse`) and asks the
arbiter for its answer to the row's reading, as M16, M21, M23 and M25 do; where the tree's loops and the text's do not
pair one for one it gives no reading, and the row takes nothing. Every other predicate, the reduction, the revision of
2026-11-16c and the exclusion are @8's. A run of @9 needs the correspondence at `5e90bfe4` or later.
*Added at review, 2026-11-17.* A group, where the row reads one, is a capturing group or `(?:…)`, which the arbiter's
tree writes as a modifier group that adds and removes no flag (`plain_group`); a modifier group that adds or removes a
flag is not one. As the row's text reads, a loop before `\b` in the alternatives of a look-behind is named and read too,
though .NET, which runs a look-behind right to left, makes no such loop atomic; only the arbiter's answer to the reading
keeps the row from taking such an input, and no input of task 2.13's runs has the shape. A script test pins it.

*Dated note, 2026-11-17, before the candidate reading (task 4.2; no classifier change, `CLASSIFIER_VERSION` stays @9).*
The notes above read "`run.sh` does not pass it: the reduction is run after a comparison, as the roadmap's record of
2026-11-15 says" (@4) and "`run.sh` passes neither `--reduction` nor `--exclude`: both are read after a comparison, as
the roadmap's record of 2026-11-16 says" (@6). It now does both, as the rule's revisions read the classification
(option R, which the owner confirmed on 2026-11-17, and the revisions of 2026-11-16b and 16c), in three steps after the
comparison, each logged with its command: `classify.py` as the rule was filed, into `OUT/comparison` as before; the
reduction into `OUT/reduction` (`reduce.py prefixes` over that classification, the arms over the reduced cases with the
stop list, `compare.py`, `classify.py` with `--exclude`, `reduce.py read`); and `classify.py` with `--reduction
OUT/reduction` and `--exclude scripts/reference-arms/p4-excluded.tsv` into `OUT/classification`, the classification the
rule as revised reads, logged in `OUT/logs/classify-revised.log` (`OUT/logs/classify.log` holds the classification as
filed, kept for comparison). When it finds class (c), `run.sh` says "class (c) differences" on stderr, naming its
`classification.json`, and still exits 0, as it always has for a class (c) difference (this sentence corrected at
review, the same day: it read "whose exit `run.sh` reports"). The three share one arbiter cache. `--classify-only`
runs the three over an earlier run's comparison and reads an earlier run's `OUT/reduction` only where `reduce.py check`
(new, it writes nothing) finds it lists the inputs that comparison leaves unreachable. Then, after the fast-path differential,
`run.sh` takes the two negative controls with `controls.py` (new): a copy of arm V over P4 and one of arm J over P1,
each made as `tests/ReferenceArms/README.md`'s "The negative controls" says, built, run once and removed; the class's
comparison with the copy's answers and with the arm's own (the run's answers, linked, not an arm run again), each
classified as above, and the copy's once more with no control case named; and the check that the comparison reports
the case, the classifier lists it unclassified and nothing else changed, the same change unnamed is class (c), and
the arm's own answers classify as the run's classification does for the class (the last two added at review, the
same day), which fails the run when it does not hold. The last line `run.sh` writes names the controls taken.
`--controls-only` takes them over an earlier run. `reduce.py check` also compares the cut-off its manifest names (at
review); it does not check the reduction's answers or the commit of the arms that wrote them.
`classify.py` is unchanged.
