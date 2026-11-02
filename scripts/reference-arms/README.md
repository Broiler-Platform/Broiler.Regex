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
| `run.sh` | Every step in order, each logged with its command |
| `tests/` | SplitMix64 against its published outputs; the format, the readers and P4's draws; the classifier |

## Commands

```sh
python3 -m unittest discover -s scripts/reference-arms/tests      # BROILER_UNICODE=<checkout> adds P5's test
python3 -I -B scripts/reference-arms/generate_population.py --out OUT/population \
    --test262 <test262 at ccaac100> --unicode <Broiler.Unicode checkout> [--vectors <conformance/vectors/regexp>]
dotnet tests/ReferenceArms/S/bin/Release/net10.0/Regex.ReferenceArms.S.dll --population OUT/population --out OUT/runs
#   ... V, J, and J again with --culture tr-TR and --culture lt-LT (tests/ReferenceArms/README.md)
python3 -I -B scripts/reference-arms/compare.py --population OUT/population --runs OUT/runs --out OUT/comparison
python3 -I -B scripts/reference-arms/classify.py --comparison OUT/comparison --runs OUT/runs \
    --broiler-js <Broiler.JS checkout> --commit <the commit the bundle names> \
    --correspondence <Broiler.VM>/docs/evidence/sjl-11-001/correspondence.md \
    --unicode-pins <Broiler.Unicode>/data/unicode/pins
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
  corpus revision bundle sjl-11-001 names; **the judged run waits for revision 32**, and until then P1 is read from the
  `sjl-11-3-regexp` branch's revision as a smoke run, never committed as evidence.
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
| S5 | V | V accepts `\k` in a class in a pattern with a group name, which S refuses |
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

Row S1 is not a correction of Broiler.JS (its class names Broiler.VM and preview.4), and row S2b's literal path is not
in arm J. (a') arm J, an exec both answer, under `i` without `u` or `v` where a code point Unicode 16.0 or 17.0 gave a
case pair is in the pattern or the subject (row U3). (b) both sides stopped by their own bounds, or a refusal for
nesting or a limit on one side and the other side's answer the arbiter's (rows S3, B6, B7, and decision D10 for J),
and row M4's empty match from a lastIndex inside a surrogate pair, which the arbiter cannot answer (decision D41).
(c) everything else, an unreachable arbiter included: a stop on one side against an answer the arbiter cannot check
in its cut-off is class (c) until reduced to an input it can, as the rule's arbiter clause reads. It refuses to run
(exit 2) when a row it names is missing from the correspondence or its class does not hold the row's words. The case
an arm's run file names as its negative-control case is listed unclassified. *(Revised 2026-11-02 at the review of task
2.13, before any judged run: the predicates read only a flag or a character, rows S1 and S2b were read for arm J, and
class (b) admitted "the arbiter past its cut-off"; `classify.py`'s docstring quotes what changed.)*
