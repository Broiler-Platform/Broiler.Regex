#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Bundle sjl-11-001's classification of the comparison's differences, written before any judged run.

    python3 -I scripts/reference-arms/classify.py --comparison DIR --runs DIR --broiler-js CHECKOUT --commit SHA
        --correspondence FILE --unicode-pins DIR [--cutoff N] [--out DIR] [--cache FILE] [--reduction DIR]
        [--exclude FILE]

It reads compare.py's differences.tsv.gz, asks the arbiter for each difference's input, and classes each difference
as the rule's admitted classes read (docs/evidence/sjl-11-001/reference-arms.md):

- **the arbiter** is scripts/conformance/regexpref.py (`ref:regexpref@1`) at the Broiler.JS commit `--commit` names,
  taken from the checkout `--broiler-js` by `git archive` with unicoderef.py and the corpus's unicode.pin, and run over
  Broiler.Unicode's archived UCD 17.0.0 (`--unicode-pins`); never the agreement of the arms. An exec it cannot finish
  in `--cutoff` Matcher invocations is unreachable. Its reason for a refusal (PatternSyntaxError's code) and the case
  pairs its UCD holds are read too, by the rows that name them;
- **(a)** the arm's answer is not the arbiter's, S's is, and a row of the correspondence (`--correspondence`, Broiler.VM's
  docs/evidence/sjl-11-001/correspondence.md) whose class names the arm's engine as corrected, in the words ROWS gives,
  records that correction: ROWS' predicate reads the direction of the difference and the row's construct, never a
  feature of the input alone;
- **(a')** as (a), arm J, an exec both sides answer, and row U3's data decision (a code point whose case mapping
  .NET's data and UCD 17.0.0 hold differently is in the pattern or the subject, under i without u or v);
- **(b)** an outcome kind that follows from a policy: both sides stopped by their own bounds, or one side refused for
  nesting or stopped at a limit (steps, a ceiling, or for arm J the harness's stop) and the other's answer is the
  arbiter's, under rows S3, B6 and B7 (V) and decision D10 (J); and row M4's empty match from a lastIndex inside a
  surrogate pair, which the arbiter cannot answer (decision D41);
- **(c)** everything else: S's answer is not the arbiter's, or the arm's is wrong and no row names it, or the arbiter
  cannot reach the input, a stop against an answer past its cut-off included ("class (c) until reduced to an input it
  can", the rule's arbiter clause). A difference with arm V under any row but V's corrections, the policy rows and M4
  is (c).

**The reduction** (`--reduction DIR`, the rule's revision of 2026-11-16, option R,
docs/evidence/sjl-11-001/reference-arms-revised-2026-11-16.md). A difference the arbiter cannot reach is class (c)
"until reduced to an input it can". Given reduce.py's output for the comparison (DIR/reductions.tsv, the arms' runs
over the reduced cases in DIR/runs, their comparison in DIR/comparison), such a difference is classed:
- **as its reduced difference**, by this classifier, when the reduced case still differs in the same run, in the same
  kinds of answer or in others ("an input whose reduced case differs, in the same kinds of answer or in others, is
  classed as that difference", the filed revision's words);
- **(b') "beyond the arbiter"**, listed with its prefix and both answers there, when S and the arm answer the reduced
  case alike, S's answer there is the arbiter's, and at the original input one side stopped at a bound it declares
  (S's step, frame or trail ceiling, arm V's allowance, or arm J's harness's stop, which the stop list holds:
  DECLARED_STOPS; any `limit:harness` of arm J is read as the stop list's, which holds only while no case of the run
  reached a backstop, a calibration's time limit or a crash: the run's facts do not say which stop it was);
- **(c)** otherwise: the arbiter reaches no prefix of the subject (what it cannot reach is in the pattern), or the
  reduced case agrees but neither side stopped at a bound it declares, or the input was not reduced.
Without `--reduction` no difference is (b'), and the classification reads the rule as filed.

**The harness's stop.** A case the harness stopped, lost or skipped (tests/ReferenceArms/Common/Limits.cs: a
calibration's time limit, a run's stop list or backstop, a crash) is answered `limit:harness`, whichever it was; the older answers `crash:exit-N`,
`limit:wall-clock` and `limit:timeout` are read as it. Arm J's stop is class (a) under row B8 when the input is that
row's (a sequence of at least B8_TERMS terms under u or v, which Broiler.Regex 0.1.0-preview.4's matcher recurses
through once per term) and S's answer is the arbiter's, and class (b) under decision D10 otherwise; arm V's or S's is
class (c), as neither declares a bound the harness's limit stands for.

**The exclusion** (`--exclude FILE`, the rule's revision of 2026-11-16 by the owner's ruling,
docs/evidence/sjl-11-001/reference-arms-revised-2026-11-16b.md). FILE is exclusion.py's listing (p4-excluded.tsv): the
19 P4 patterns the ruling names, by id, whose arbiter cost is in the pattern. Every difference of a listed pattern,
under every flag set, by acceptance and by every exec, a reduced case `~p` among them, is classed **excluded**: listed
apart, "excluded from P4 by the owner's ruling of 2026-11-16", neither (c) nor any admitted class, and the arbiter is
not asked for it. A difference whose id names a listed pattern and whose pattern is not the one listed refuses the run
(exit 2). classification.json names the listing by its file name, SHA-256 and count, and counts the excluded differences
apart. Without `--exclude` no difference is excluded, and the classification reads the rule without the ruling.

*Revised 2026-11-16, after Broiler.VM's rows M16 to M26 of that day and the owner's ruling on P3's `O06`
(CLASSIFIER_VERSION @8, written after task 2.13's runs, their classification by @5, @6 and @7 and the reduction of
2026-11-15 were seen).* @7's paragraph below reads "the correspondence is now read at Broiler.VM's `44511066`
(Broiler-Platform/Broiler.VM#212's merge, the file's blob `94b860cc`; CORRESPONDENCE below)" and "No predicate reads row
B1's widening: `O06` is an input the arbiter cannot reach, whose reduced case shows no difference and where neither side
stopped at a bound it declares, so the reduction classes it (c) before any row is read, as the rule's revision (option
R) says ("without such a stop it stays class (c)"); the row names it, and it stays class (c)"; that paragraph is kept as
written. The correspondence is now read at Broiler.VM's `626e4ed8` (Broiler-Platform/Broiler.VM#215's merge, the file's
blob `d4605f5c`; CORRESPONDENCE below), which adds rows M16 to M26 under decision D-jsrest and gives rows M8, M12, M13
and M15 dated notes. The noted rows' predicates read their notes in place, so that a difference they take keeps its
place in the row order: row M8 finds `\\b` and `\\B` outside a class lexed as the text lexes a class without `v`
(text_tokens); row M12's reading writes a `-` right before a property escape that follows `\\d`, `\\D`, `\\w`, `\\W`,
`\\s` or `\\S` as `\\-`; row M13's nested class ends at its first `]`, even right after its `[` or `[^`; row M15 takes
an input whose invariant run answers S's match up to row M10's names. Rows M16 to M26 come after M15, in the
correspondence's order ("Rows M16 to M26 are read after M15, in order, so an input two of them take is the first's"),
each read from its row's own text: M16, M21, M23 and M25 take an input only where the arbiter's answer to the row's
reading is arm J's, up to row M10's names, and not the text's, as M11 to M14 do; M17 to M20, M22, M24 and M26 take it
by its construct and condition, M16, M17 and M19 reading the arbiter's tree of the pattern (Context.parse) for
`GapScan`'s shapes. The rule's revision of 2026-11-16c (docs/evidence/sjl-11-001/reference-arms-revised-2026-11-16c.md,
the owner's ruling of that day on `O06`) is read with `--reduction`: an input the arbiter cannot reach, at whose
reduced case S, the arm and the arbiter answer alike and at which neither side stopped at a bound it declares, is class
(a) "beyond the arbiter, by row", under the row that names it (BEYOND_ROWS: row B1, P3's `O06`), where (1) the row
names the input and the mechanism (BEYOND_ROWS' predicate reads the input the row names, and check_beyond_rows the row's
words), (2) every other arm that implements the operation answers the input as S does (Context.other_answers, the
runs' answer files), and (3) S's answer at the input is the answer the reduced case's agreed answer extends to, as the
pattern requires (whole_run_answer, the predicate the revision's condition 3 asks the classifier to state); otherwise
it stays class (c). @7's other predicates, the reduction's other outcomes and the exclusion are unchanged.

*Revised 2026-11-16, after Broiler.VM's rows M10 to M15 of that day (CLASSIFIER_VERSION @7, written after task 2.13's
runs, their classification by @5 and @6 and the reduction of 2026-11-15 were seen).* @4's paragraph below reads "The
correspondence is read at Broiler.VM's `7c65a8fe` (Broiler-Platform/Broiler.VM#211's merge, the file's blob `f0f6f080`;
CORRESPONDENCE below)"; that paragraph is kept as written, and the correspondence is now read at
Broiler.VM's `44511066` (Broiler-Platform/Broiler.VM#212's merge, the file's blob `94b860cc`; CORRESPONDENCE below),
which adds rows M10 to M15 under decision D-jsexec, gives row M8 a dated note, and widens row B1 for P3's `O06`.
Predicates are added for M10 to M15, each read from its row's own text, after @6's rows and in the order the
correspondence states (M10 first), so that every difference @6 classed keeps its class and row and an input two of them
take is the first's. Rows M11 to M14 ask the arbiter for its answer to the row's reading of the pattern
(Arbiter.reading): the row takes the input only where that answer is arm J's, up to row M10's names, and not the text's.
Row M10 reads the names alone, each rewrite where the row names it; row M15 reads arm J's answer under the invariant
culture from the comparison. No predicate reads row B1's widening: `O06` is an input the arbiter cannot reach, whose
reduced case shows no difference and where neither side stopped at a bound it declares, so the reduction classes it (c)
before any row is read, as the rule's revision (option R) says ("without such a stop it stays class (c)"); the row
names it, and it stays class (c). @6's predicates, the reduction and the exclusion are unchanged.
*At the review of @7, the same day, before any judged run (the tag unchanged).* The paragraph above read "each rewrite
where the row names it" with a letter's rewrite read as "a class that holds it, or `(?-i:…)` over one", and row M12 read
"The property escapes and `\\a` are each read alone, then together; a reading that gives arm J's answer takes the
input". Row M10 now reads each character of a name, not only a letter, against its Canonicalize class in the arbiter's
tables (Context.canonicalize_class): a class rewrite is that class with the character first, as the translator writes
it, only where it holds another member and a non-ASCII one; `(?-i:…)` is that class, each member escaped as the
translator escapes it, only for a character .NET's invariant lowercase folds into another class (Context.over_folds,
whose lowercase is the pinned UCD's, U+0130 kept as itself: a stand-in, .NET's table is not read); `(?=$|\\r)` only
where no inline modifier group stands in the pattern; and "the same capture" as the same group index, not the span
alone (the arbiter's answer to a reading reports no index, so rows M11 to M14 read the span). Row M12 asks one reading,
every such escape read. Over task 2.13's run each takes the same differences as before.

*Revised 2026-11-16, after the owner's ruling of that day on population P4 (CLASSIFIER_VERSION @6).* The option
`--exclude` (above) is added; no row, predicate, class or reading of the reduction changed, so without it @6 classes
every difference as @5 did.

*Revised 2026-11-16, at the review of @4 and before any judged run (CLASSIFIER_VERSION @5).* Row M8's predicate read
"`\\b`, `\\B`, `\\w`, `\\d`, `\\s` or their complements" in the pattern with "a code point past ASCII in the
subject", and any difference of an exec both answer, the group names alone among them: it took 6,849 differences whose
spans and captures agree and only the names differ (Broiler.JS's translator rewriting a name, which the roadmap's
record of 2026-11-16 places outside the row's list), complements the row does not name, and subjects whose only
non-ASCII units are lone surrogates or emoji. It now reads the row's list where it bears on the difference: never a
difference of the group names alone; `\\b` or `\\B` outside a class with a non-ASCII letter (General_Category L in the
arbiter's UCD, a pair read as one code point) in the subject beside what either side's match covers, or anywhere in it
where the pattern has a look-around; U+0130 or U+0131 under `i` in the pattern or there; and P5's `I` and `[I]`
against `i`, `i` and `[i]` against `I`, each with its own subject. "`\\w`, `\\d` and `\\s` beside them" is read as beside
`\\b` and `\\B` (the plural "them"; "a non-ASCII letter" is singular), so the `\\b` and `\\B` limb holds them; the
row's text does not settle it, and the reading "beside non-ASCII letters" is not taken. Row M4's widened predicate
reads the widening's two vectors' patterns and flags alone, and row S14 no longer takes a bound past 2^31 - 1 that
J refuses only under `i` where row S15 does not (outside P4). The reduction's paragraph above quoted the draft's
words, "an input whose reduced difference is another difference is classed as that difference"; it now quotes the
filed revision.

*Revised 2026-11-16, after task 2.13's runs (unjudged), their classification and the reduction of 2026-11-15 were
seen (CLASSIFIER_VERSION @4).* The correspondence is read at Broiler.VM's `7c65a8fe`
(Broiler-Platform/Broiler.VM#211's merge, the file's blob `f0f6f080`; CORRESPONDENCE below), which adds rows S8a, S12
to S15, M8 and M9 and widens M4 (decisions D-wiv and D-jsrows of JSD-0081, 2026-11-16). Predicates are added for those rows, each read from the row's own text;
row S7's reaches the quantified `^`, `$` and `\\b` vectors its widening of 2026-11-01 names, row S8's the set-operator
vectors row S12 leaves to it, and row M4's (J) the two vectors its widening of 2026-11-16 names. The predicates of
@3 are unchanged and come first, so every difference @3 classed (a), (a') or (b) keeps its class and row. The option
`--reduction` reads the rule's revision of 2026-11-16 (above). @3's docstring is kept as written below.

*Revised 2026-11-02, at task 2.13's second review and before any judged run (CLASSIFIER_VERSION @3).* It read crashes
and the wall clock as distinct answers: a crash was "no row names" (c) and a wall-clock stop (b) under D10, so one
input's class depended on which came first, and row B8 was never read. The harness's stop is now one answer, read as
the paragraph above says. The correspondence is read at Broiler.VM's `5d025cc` (the owner's decisions D-s5 and
D-widen of 2026-11-01): row S5 is no longer read for arm V (its class case is the specification's answer, which S
gives since task 2.12) and is read for arm J (its refusal of the class case is a correction of Broiler.JS), and row
F3 is read for arm J (`(?x:a)`, an invalid modifier its constructor accepts).

*Revised 2026-11-02, at the review of task 2.13 and before any judged run (CLASSIFIER_VERSION @2).* It read "(b) ... the
other's answer is the arbiter's or the arbiter is past its cut-off", which the rule's arbiter clause does not admit;
its rows S1 and S2b for arm J (row S1's class names Broiler.VM and preview.4, row S2b's the literal path the arm does
not copy) are gone, rows M2b, S10 and M7 for J are added, and each predicate now reads its row's correction.

A difference in the case an arm's run file names as its negative-control case is listed **unclassified**.
It writes DIR/classification.json (the counts by class, row and run, the arbiter's commit and blob, the
correspondence's file name, blob and SHA-256, and no path, so that it is the same wherever its inputs are) and
DIR/classified.tsv.gz (each difference with its class, row, the arbiter's answer and why), and exits 0 when no
difference is (c), 1 when one is, 2 when it refuses to run (a row it names is missing from the correspondence, or
its class does not name the engine the predicate corrects, or an argument is wrong, or `--exclude` lists a pattern id
with another pattern than the comparison's), and 3 when it fails (an
exception: an arbiter it cannot take from the checkout, a listing it cannot read); it writes classification.json last,
so a run that does not end with 0 or 1 leaves none.
"""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import os
import re
import subprocess
import sys
import tarfile
import tempfile
import traceback
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from population_format import decode_units, encode_units, open_text  # noqa: E402

CLASSIFIER_VERSION = "sjl-11-001-classification@8"
DEFAULT_CUTOFF = 1_000_000

# The Broiler.VM commit whose correspondence.md this classifier's rows and words are read from (the rule's "rows the
# correspondence names", reference-arms.md; the bundle README's run-reference-arms.sh --correspondence-commit).
# classification.json names it beside the blob of the file a run was given, which may be a later one.
CORRESPONDENCE = {"repository": "Broiler.VM", "commit": "626e4ed88afe8898724bda669e981cf7cf1b98b2",
                  "path": "docs/evidence/sjl-11-001/correspondence.md",
                  "blob": "d4605f5cd1461137a3533e816d5bc2275434cce8"}


# --------------------------------------------------------------------------------------------------
# The answers.

def escape_name(units) -> str:
    """A group name as the arms write it (tests/ReferenceArms/Common/Harness.cs, Escape)."""
    out = []
    for u in units:
        if 0x20 <= u <= 0x7E and chr(u) not in ",=|\\":
            out.append(chr(u))
        else:
            out.append(f"\\u{u:04X}")
    return "".join(out)


def canonical(answer: str):
    """An arm's answer as the arbiter's is compared: a match as its captures and each name's participating capture."""
    if not answer.startswith("match:"):
        return answer
    body, _, names = answer[6:].partition("|")
    captures = tuple(None if g == "-" else tuple(int(x) for x in g.split(",")) for g in body.split(";"))
    named = []
    if names:
        for entry in names.split(","):
            name, _, group = entry.rpartition("=")
            named.append((name, None if group == "-" else captures[int(group)]))
    return ("match", captures, tuple(sorted(named)))


def kind(answer: str) -> str:
    return answer.split(":", 1)[0]


HARNESS_STOP = "limit:harness"
RAW_HARNESS_STOP = re.compile(r"crash:exit-\d+|limit:wall-clock|limit:timeout")


def harness_read(answer: str) -> str:
    """An answer with the harness's stops read as one (tests/ReferenceArms/Common/Limits.cs, HarnessStop): a crash, a
    wall clock and arm J's former .NET match timeout are each the answer `limit:harness`, so which came first never
    changes a class."""
    return HARNESS_STOP if RAW_HARNESS_STOP.fullmatch(answer) else answer


# --------------------------------------------------------------------------------------------------
# The arbiter.

class Arbiter:
    def __init__(self, checkout: Path, commit: str, pins: Path, cutoff: int) -> None:
        self.commit = subprocess.run(["git", "-C", str(checkout), "rev-parse", commit], check=True, capture_output=True,
                                     text=True).stdout.strip()
        paths = ["scripts/conformance/regexpref.py", "scripts/conformance/unicoderef.py",
                 "conformance/vectors/sources/unicode.pin"]
        self.blobs = {p: subprocess.run(["git", "-C", str(checkout), "rev-parse", f"{self.commit}:{p}"], check=True,
                                        capture_output=True, text=True).stdout.strip() for p in paths}
        archive = subprocess.run(["git", "-C", str(checkout), "archive", self.commit] + paths, check=True,
                                 capture_output=True).stdout
        self.directory = Path(tempfile.mkdtemp(prefix="regexpref-"))
        with tarfile.open(fileobj=io.BytesIO(archive)) as tar:
            tar.extractall(self.directory, filter="data")
        os.environ["BROILER_UNICODE_PINS"] = str(pins)
        sys.path.insert(0, str(self.directory / "scripts" / "conformance"))
        import regexpref  # noqa: E402
        self.r = regexpref
        self.version = regexpref.REFERENCE_VERSION
        self.cutoff = cutoff
        self.cache: dict = {}
        self.codes: dict = {}
        self._categories: dict = {}

    def _identity(self) -> list:
        return [self.version, self.commit, self.cutoff]

    def load(self, path: Path) -> None:
        """The answers a run saved, when they are this arbiter's at this cut-off; otherwise none."""
        with open_text(path) as handle:
            if json.loads(handle.readline()) != self._identity():
                return
            for line in handle:
                key, answer = json.loads(line)
                if len(key) == 3 and key[0] == "code":
                    self.codes[(key[1], key[2])] = answer
                    continue
                if isinstance(answer, list):
                    answer = ("match", tuple(None if c is None else tuple(c) for c in answer[1]),
                              tuple((n, None if r is None else tuple(r)) for n, r in answer[2]))
                self.cache[tuple(key)] = answer

    def save(self, path: Path) -> None:
        with open_text(path, "wt") as handle:
            handle.write(json.dumps(self._identity()) + "\n")
            for key, answer in self.cache.items():
                handle.write(json.dumps([list(key), answer]) + "\n")
            for (pattern, flags), code in self.codes.items():
                handle.write(json.dumps([["code", pattern, flags], code]) + "\n")

    def refusal_code(self, pattern, flags) -> str | None:
        """The arbiter's reason for refusing the pattern under the flags (PatternSyntaxError's `code`, such as
        `grammar:class-set-syntax-character`), None when it accepts it."""
        key = (pattern, flags)
        if key not in self.codes:
            try:
                self.r.regexp_create(decode_units(pattern), [ord(c) for c in ("" if flags == "." else flags)])
                self.codes[key] = None
            except self.r.PatternSyntaxError as failure:
                self.codes[key] = failure.code
        return self.codes[key]

    def case_pairs(self) -> set[int]:
        """Row M2's case pairs: every code unit at or above U+0080 whose simple case folding orbit in the pinned UCD
        (CaseFolding.txt, status C and S: the pairs Unicode's case data holds, which .NET's case-insensitive matching
        reads) is not the set of code units Canonicalize without u or v maps where it maps the unit
        (22.2.2.7.3: the full upper case, kept only when it is one unit and does not take a non-ASCII unit into
        ASCII). U+212A KELVIN SIGN, whose orbit holds k and K and whose Canonicalize is itself, is one; so are
        U+017F, U+2126, U+212B, U+1E9E and U+1F80 to U+1FFC with a ypogegrammeni (row M2b)."""
        r = self.r
        data = r.ucd()
        table = r._canonicalize_bmp()
        preimages = r._canonicalize_bmp_preimages()
        out = set()
        for unit in range(0x80, 0x10000):
            orbit = {c for c in data.orbit(unit) if c <= 0xFFFF}
            if orbit != set(preimages.get(table[unit], (unit,))):
                out.add(unit)
        return out

    def canonicalize_class(self, unit: int) -> tuple[int, ...]:
        """Row M10's "Canonicalize class" of a code unit: every BMP code unit Canonicalize without u or v (22.2.2.7.3,
        the arbiter's `_canonicalize_bmp`) maps where it maps the unit, ascending."""
        table = self.r._canonicalize_bmp()
        return self.r._canonicalize_bmp_preimages().get(table[unit], (unit,))

    def over_folds(self, unit: int) -> bool:
        """Row M10's "one .NET's invariant lowercase folds into another Canonicalize class": a BMP code unit whose
        lowercase group (the units whose lowercase is its lowercase) holds a unit of another Canonicalize class. .NET's
        invariant lowercase is not read here; it stands as the pinned UCD's simple lowercase (UnicodeData.txt), with
        U+0130 kept as itself, as .NET's invariant casing is known to keep it (inferred, not read in .NET's source)."""
        if not hasattr(self, "_over_folding"):
            data, table = self.r.ucd(), self.r._canonicalize_bmp()

            def lower(u: int) -> int:
                return u if u == 0x130 else data.simple_lower.get(u, u)

            groups: dict[int, set[int]] = {}
            for u in range(0x10000):
                if not 0xD800 <= u <= 0xDFFF:
                    groups.setdefault(lower(u), set()).add(u)
            self._over_folding = frozenset(
                u for u in range(0x10000) if not 0xD800 <= u <= 0xDFFF
                and any(table[v] != table[u] for v in groups[lower(u)]))
        return unit in self._over_folding

    def category_units(self, name: str) -> list[int]:
        """Row M12's reading: the BMP code units whose General_Category in the pinned UCD is `name` (a two-letter
        value) or begins with it (a one-letter group)."""
        if name not in self._categories:
            data = self.r.ucd()
            self._categories[name] = [u for u in range(0x10000) if (
                data.general_category(u) == name if len(name) == 2 else data.general_category(u).startswith(name))]
        return self._categories[name]

    def reading(self, d, text: str):
        """The arbiter's answer to a row's reading of the difference's pattern (one character per code unit), under its
        flags, at its subject and lastIndex: an exec, cached as any other."""
        return self.answer(encode_units([ord(c) for c in text]), d["flags"], d["subject"], d["lastIndex"], "exec")

    def parse(self, text: str):
        """Rows M16, M17, M19 and M24 (@8): the arbiter's tree of a pattern without `u` or `v` (one character per code
        unit), as ParsePattern with Annex B reads it (regexpref.parse_pattern: its Disjunction, with the groups
        counted and every back-reference resolved); None where the arbiter refuses the pattern."""
        if not hasattr(self, "_trees"):
            self._trees = {}
        if text not in self._trees:
            try:
                self._trees[text] = self.r.parse_pattern([ord(c) for c in text], False, False).tree
            except self.r.PatternSyntaxError:
                self._trees[text] = None
        return self._trees[text]

    def letters(self) -> frozenset:
        """Row M8's non-ASCII letters: every code point past U+007F whose General_Category in the pinned UCD
        (UnicodeData.txt, as the arbiter reads it) is Lu, Ll, Lt, Lm or Lo."""
        data = self.r.ucd()
        return frozenset(cp for cp in range(0x80, 0x110000) if data.general_category(cp).startswith("L"))

    def answer(self, pattern, flags, subject, last_index, op) -> str | tuple:
        key = (pattern, flags, subject if op == "exec" else None, last_index if op == "exec" else None)
        if key in self.cache:
            return self.cache[key]
        r = self.r
        p, f = decode_units(pattern), [ord(c) for c in ("" if flags == "." else flags)]
        try:
            if op == "accept":
                r.regexp_create(p, f)
                result = "accept"
            else:
                counted = r.counted_exec(p, f, decode_units(subject), int(last_index), self.cutoff)
                if counted.exhausted:
                    result = "arbiter:exhausted"
                elif counted.result is None:
                    result = "null"
                else:
                    e = counted.result
                    named = tuple(sorted((escape_name(name), rng) for name, rng in (e.groups or [])))
                    result = ("match", tuple(e.captures), named)
        except r.PatternSyntaxError:
            result = "refuse:syntax"
        except getattr(r, "SpecAssertion", ()) as failure:
            result = "arbiter:no-answer:" + type(failure).__name__
        except RecursionError:
            result = "arbiter:unreachable:recursion"
        except MemoryError:
            result = "arbiter:unreachable:memory"
        self.cache[key] = result
        return result


# --------------------------------------------------------------------------------------------------
# The rows. Each: (row, the engine whose correction the row records, predicate over the difference, the words of the
# row's class that name that engine as corrected). A predicate reads the row's own correction: the direction of the
# difference (which side refuses, or that both answer and differ) and the construct the row names, never a feature of
# the input alone. Revised 2026-11-02 at the review of task 2.13, before any judged run; the revision replaced
# predicates that read only a flag or a character ("i without u", "v", a line terminator in the subject, any
# acceptance difference under u), which classed J's other defects (a) whenever the input had that feature.

def units_text(field: str) -> str:
    from population_format import text_of
    return text_of(decode_units(field))


def has_flag(d, letters: str) -> bool:
    flags = "" if d["flags"] == "." else d["flags"]
    return any(ch in flags for ch in letters)


def unicode_mode(d) -> bool:
    return has_flag(d, "uv")


def refuses(answer: str) -> bool:
    return answer == "refuse:syntax"


def answers(answer: str) -> bool:
    """An answer of the operation: acceptance, or an exec's null or match (not a refusal, a stop or a crash)."""
    return kind(answer) in ("accept", "null", "match")


def arm_accepts_what_s_refuses(d) -> bool:
    """The arm accepts a pattern S refuses (an acceptance case, or an exec of that pattern)."""
    return refuses(d["S"]) and answers(d["arm"])


def arm_refuses_what_s_accepts(d) -> bool:
    return answers(d["S"]) and refuses(d["arm"])


def both_answer_exec(d) -> bool:
    """An exec both sides answer, null or a match, and answer differently: a difference of matching."""
    return d["op"] == "exec" and kind(d["S"]) in ("null", "match") and kind(d["arm"]) in ("null", "match")


def scan(text: str):
    """The pattern's characters outside escapes, each with whether it is inside a class: yields (index, character,
    in_class, class_start). Escapes are skipped whole (the backslash and the character after it); nested classes
    (the v flag's) keep in_class true until the outer class closes."""
    depth, start, i = 0, -1, 0
    while i < len(text):
        ch = text[i]
        if ch == "\\":
            i += 2
            continue
        if ch == "[":
            if depth == 0:
                start = i
            depth += 1
            yield i, ch, True, start
        elif ch == "]" and depth > 0:
            depth -= 1
            yield i, ch, True, start
        else:
            yield i, ch, depth > 0, start if depth > 0 else -1
        i += 1


def added_modifiers(text: str) -> set[str]:
    """The flags a modifiers group `(?ims-ims:` adds anywhere in the pattern."""
    out: set[str] = set()
    for i, ch, in_class, _ in scan(text):
        if ch == "(" and not in_class:
            m = re.match(r"\(\?([a-z]*)(?:-[a-z]*)?:", text[i:])
            if m:
                out |= set(m.group(1))
    return out


def mode(d, letter: str) -> bool:
    """Whether the flag is in force somewhere: in the flags, or added by a modifiers group."""
    return has_flag(d, letter) or letter in added_modifiers(units_text(d["pattern"]))


NAME = re.compile(r"\(\?<([^=!>][^>]*)>")


NAME_ESCAPE = re.compile(r"\\u\{([0-9A-Fa-f]+)\}|\\u([0-9A-Fa-f]{4})")


def decoded_name(name: str) -> str:
    """A GroupName's string value: its \\u escapes decoded (22.2.1.6, RegExpIdentifierCodePoints), a pair of escaped
    surrogates joined."""
    text = NAME_ESCAPE.sub(lambda m: chr(int(m.group(1) or m.group(2), 16)), name)
    return text.encode("utf-16-le", "surrogatepass").decode("utf-16-le", "surrogatepass")


def duplicate_names(d) -> bool:
    """A name declared twice, compared by its value, so that `(?<\\u0061>x)|(?<a>y)` repeats `a`."""
    names = [decoded_name(n) for n in NAME.findall(units_text(d["pattern"]))]
    return len(names) != len(set(names))


def duplicate_only_when_decoded(d) -> bool:
    """Row S2a: a name repeated once its escapes are decoded and not as written, `(?<\\u0061>x)(?<a>y)`: the
    duplicate test that compares names as written lets it through."""
    written = NAME.findall(units_text(d["pattern"]))
    decoded = [decoded_name(n) for n in written]
    return len(decoded) != len(set(decoded)) and len(written) == len(set(written))


def k_in_class(d) -> bool:
    """Row S5: `\\k` in a class, in a pattern that declares a group name, `[\\k<a>](?<a>x)`."""
    text = units_text(d["pattern"])
    return bool(NAME.search(text)) and any(m.start() > 0 and _in_class_at(text, m.start())
                                           for m in re.finditer(r"\\k", text))


def _in_class_at(text: str, index: int) -> bool:
    """Whether the escape beginning at `index` is inside a class."""
    depth, i = 0, 0
    while i < index:
        ch = text[i]
        if ch == "\\":
            i += 2
            continue
        if ch == "[":
            depth += 1
        elif ch == "]" and depth > 0:
            depth -= 1
        i += 1
    return i == index and depth > 0


B8_TERMS = 10_000


def longest_sequence(text: str) -> int:
    """The most terms of one alternative, at any depth: each character, escape, class and group outside a class is a
    term, a quantifier is not, and `|` begins a new alternative. `\\B` 100,000 times is one sequence of 100,000."""
    best, counts, depth, i = 0, [0], 0, 0
    while i < len(text):
        ch = text[i]
        if ch == "\\":
            if depth == 0:
                counts[-1] += 1
            i += 2
            continue
        if depth > 0:
            depth += 1 if ch == "[" else -1 if ch == "]" else 0
        elif ch == "[":
            depth = 1
            counts[-1] += 1
        elif ch == "(":
            counts[-1] += 1
            counts.append(0)
        elif ch == ")" and len(counts) > 1:
            best = max(best, counts.pop())
        elif ch == "|":
            best = max(best, counts[-1])
            counts[-1] = 0
        elif ch == "{" and re.match(r"\{\d+(?:,\d*)?\}", text[i:]):
            i = text.index("}", i)
        elif ch not in "*+?":
            counts[-1] += 1
        i += 1
    return max([best] + counts)


def recursion_per_term(d, context) -> bool:
    """Row B8: arm J stopped by the harness (a stack overflow, or the time the recursion takes) on an input S answers,
    under u or v, whose pattern holds a sequence of at least B8_TERMS terms: Broiler.Regex 0.1.0-preview.4's matcher,
    which Broiler.JS hands every u and v pattern, recurses once per term (`\\B` 20,000 times takes over a second in
    the arm, 100,000 times is stopped; row B8's input is `(?:a)` 100,000 times). Which stop it was is not read."""
    return d["arm"] == HARNESS_STOP and answers(d["S"]) and unicode_mode(d) and longest_sequence(
        units_text(d["pattern"])) >= B8_TERMS


def class_k_refused(d) -> bool:
    """Row S5's class case for Broiler.JS (decision D-s5): J refuses a pattern S accepts that holds `\\k` in a class
    beside a group name, `[\\k<a>](?<a>x)`."""
    return arm_refuses_what_s_accepts(d) and k_in_class(d)


def invalid_modifier_accepted(d) -> bool:
    """Row F3's correction of Broiler.JS (decision D-widen): J accepts what S refuses, and the pattern holds a modifiers
    group with a letter that is not a modifier, `(?x:a)`."""
    if not arm_accepts_what_s_refuses(d):
        return False
    text = units_text(d["pattern"])
    for i, ch, in_class, _ in scan(text):
        if ch == "(" and not in_class:
            m = re.match(r"\(\?([A-Za-z]*)(?:-([A-Za-z]*))?:", text[i:])
            if m and set((m.group(1) or "") + (m.group(2) or "")) - set("ims"):
                return True
    return False


def lead_before_braced(d) -> bool:
    """Row S10: under u or v, a four-digit lead-surrogate escape followed by a braced escape."""
    return bool(re.search(r"\\u[dD][89abAB][0-9a-fA-F]{2}\\u\{", units_text(d["pattern"]))) and unicode_mode(d)


def lead_before_braced_trail_in_class(d) -> bool:
    """Row S10's class case, `[\\uD83D\\u{DC38}]` under u: in a class, a four-digit lead-surrogate escape directly
    followed by a braced trail surrogate, which Broiler.JS's translator joins into one code point."""
    if not unicode_mode(d):
        return False
    text = units_text(d["pattern"])
    for m in re.finditer(r"\\u[dD][89abAB][0-9a-fA-F]{2}\\u\{0*[dD][c-fC-F][0-9a-fA-F]{2}\}", text):
        if _in_class_at(text, m.start()):
            return True
    return False


def inside_pair(d) -> bool:
    if d["op"] != "exec" or not has_flag(d, "gy") or not unicode_mode(d):
        return False
    s = decode_units(d["subject"])
    k = int(d["lastIndex"])
    return 0 < k < len(s) and 0xDC00 <= s[k] <= 0xDFFF and 0xD800 <= s[k - 1] <= 0xDBFF


def has_surrogate(d) -> bool:
    return any(0xD800 <= u <= 0xDFFF for u in decode_units(d["subject"]))


def backreference_across_pair(d) -> bool:
    """Row M7: under u or v, a back-reference, and a surrogate in the subject."""
    return unicode_mode(d) and bool(re.search(r"\\[1-9]|\\k<", units_text(d["pattern"]))) and has_surrogate(d)


def look_behinds(text: str):
    """The text of every look-behind group, `(?<=` or `(?<!` to its closing parenthesis, found by counting
    parentheses outside classes and escapes; and whether a quantifier follows it."""
    for m in re.finditer(r"\(\?<[=!]", text):
        if _in_class_at(text, m.start()) or (m.start() > 0 and _escaped(text, m.start())):
            continue
        depth, i, in_class = 0, m.start(), False
        while i < len(text):
            ch = text[i]
            if ch == "\\":
                i += 2
                continue
            if in_class:
                in_class = ch != "]"
            elif ch == "[":
                in_class = True
            elif ch == "(":
                depth += 1
            elif ch == ")":
                depth -= 1
                if depth == 0:
                    yield text[m.start():i + 1], i + 1 < len(text) and text[i + 1] in "*+?{"
                    break
            i += 1


def _escaped(text: str, index: int) -> bool:
    """Whether the character at `index` is the second of an escape."""
    backslashes, j = 0, index - 1
    while j >= 0 and text[j] == "\\":
        backslashes += 1
        j -= 1
    return backslashes % 2 == 1


def quantified_lookbehind(text: str) -> bool:
    """Whether a look-behind group is followed by a quantifier, `(?<=a)*`."""
    return any(quantified for _, quantified in look_behinds(text))


def backreference_in_look_behind(d) -> bool:
    """Row M7's look-behind, `(?<=\\1(.))b` under u: a back-reference inside a look-behind, a surrogate in the
    subject."""
    return unicode_mode(d) and has_surrogate(d) and any(
        re.search(r"\\[1-9]|\\k<", body) for body, _ in look_behinds(units_text(d["pattern"])))


def pattern_code_points(text: str) -> set[int]:
    """The code points the pattern names: each character, and the value of each \\u, \\u{} and \\x escape."""
    out = {ord(c) for c in text}
    for m in re.finditer(r"\\u\{([0-9A-Fa-f]+)\}|\\u([0-9A-Fa-f]{4})|\\x([0-9A-Fa-f]{2})", text):
        out.add(int(m.group(1) or m.group(2) or m.group(3), 16))
    return out


def input_code_points(d) -> set[int]:
    units = pattern_code_points(units_text(d["pattern"]))
    if d["op"] == "exec":
        units |= set(decode_units(d["subject"]))
    return units


def case_insensitive_without_u(d) -> bool:
    return mode(d, "i") and not unicode_mode(d)


M2B = frozenset(range(0x1F80, 0x2000))


def net_case_pair(d, context) -> bool:
    """Row M2: under i without u or v, an exec both sides answer and answer differently, whose input holds a code
    point of a case pair that Unicode's case data and Canonicalize hold differently (Arbiter.case_pairs), U+212A among
    them; the U+1F80 block's are row M2b."""
    return both_answer_exec(d) and case_insensitive_without_u(d) and bool(
        (input_code_points(d) & context.case_pairs) - M2B)


def iota_subscript_pair(d, context) -> bool:
    """Row M2b: as M2, the pair one of U+1F80 to U+1FFC's (U+1F80 against U+1F88)."""
    return both_answer_exec(d) and case_insensitive_without_u(d) and bool(
        input_code_points(d) & context.case_pairs & M2B)


DOTTED_AND_DOTLESS = frozenset((0x0130, 0x0131))


def culture_casing(d, context) -> bool:
    """Row M2a: arm J under tr-TR or lt-LT, i without u or v, an exec both sides answer, U+0130 or U+0131 in the input,
    and the answer is not the one the same arm gives under the invariant culture: the culture changed it."""
    if d["run"] not in ("J.tr-TR", "J.lt-LT") or not both_answer_exec(d) or not case_insensitive_without_u(d):
        return False
    if not input_code_points(d) & DOTTED_AND_DOTLESS:
        return False
    return context.invariant.get(d["id"], d["S"]) != d["arm"]


def line_terminator(d) -> bool:
    """Row M3, its two halves for Broiler.JS: without u or v, an exec both sides answer; under m (the flag or a
    modifier), `^` or `$` outside a class and U+2028 or U+2029 in the subject, which .NET's `^` and `$` ignore; without
    m, `$` outside a class and a subject that ends with LF, before which .NET's `$` matches."""
    if not both_answer_exec(d) or unicode_mode(d):
        return False
    text = units_text(d["pattern"])
    s = decode_units(d["subject"])
    outside = {ch for _, ch, in_class, _ in scan(text) if not in_class}
    if mode(d, "m") and ("^" in outside or "$" in outside) and any(u in (0x2028, 0x2029) for u in s):
        return True
    return not mode(d, "m") and "$" in outside and len(s) > 0 and s[-1] == 0x0A


def v_mode_syntax(d, context) -> bool:
    """Row S8: under v, arm J accepts a pattern S refuses, and the arbiter's reason is a ClassSetSyntaxCharacter or a
    ClassSetReservedDoublePunctuator in a class (the breaking-change patterns `[(]`, `[a!!b]`, ...)."""
    return has_flag(d, "v") and arm_accepts_what_s_refuses(d) and context.refusal_code(d) in (
        "grammar:class-set-syntax-character", "grammar:class-set-reserved-double-punctuator")


def quantified_lookbehind_accepted(d, context) -> bool:
    """Row S7, Broiler.JS's one answer that differs: without u or v, J accepts a quantified look-behind, `(?<=a)*`,
    where the grammar refuses it (the arbiter's reason: nothing to repeat, or a braced quantifier after an
    assertion). Under u the row reads Broiler.JS "the same", so no difference under u is S7's."""
    return not unicode_mode(d) and arm_accepts_what_s_refuses(d) and quantified_lookbehind(
        units_text(d["pattern"])) and context.refusal_code(d) in ("grammar:nothing-to-repeat",
                                                                   "early:invalid-braced-quantifier")


def half_pair_match(d) -> bool:
    """Row M4, Broiler.JS: from a lastIndex inside a surrogate pair, J matches (starting at lastIndex, a lone half)
    where the specification fails at the pair."""
    return inside_pair(d) and both_answer_exec(d) and kind(d["arm"]) == "match" and d["S"] == "null"


def vm_steps_back(d) -> bool:
    """Row M4, Broiler.VM: from a lastIndex inside a pair, V's start steps back to the pair, so its match differs."""
    return inside_pair(d) and both_answer_exec(d) and kind(d["arm"]) == "match"


def first_index(answer: str) -> int:
    return int(answer[6:].split(";", 1)[0].split(",", 1)[0])


def nested_quantifier(text: str) -> bool:
    """Whether a quantified group holds a quantifier, `(?:(a+)+b|a*c)`'s `(a+)+`: the construct whose backtracking
    exhausts a budget."""
    starts = []
    for i, ch, in_class, _ in scan(text):
        if in_class:
            continue
        if ch == "(":
            starts.append(i)
        elif ch == ")" and starts:
            start = starts.pop()
            if i + 1 < len(text) and text[i + 1] in "*+{" and any(
                    c in "*+{" and not inside for _, c, inside, _ in scan(text[start + 1:i])):
                return True
    return False


def budget_reset_per_start(d) -> bool:
    """Row B2 (Broiler.Regex 0.1.0-preview.4, which Broiler.JS routes every u and v pattern to): exhaustion at one
    start read as that start's failure, so J answers null, or a match at a later start, where S has a match, for a
    pattern with a nested quantifier. The arm's answer does not show that the budget was exhausted; the direction and
    the construct are the row's."""
    if not both_answer_exec(d) or not unicode_mode(d) or kind(d["S"]) != "match":
        return False
    if not nested_quantifier(units_text(d["pattern"])):
        return False
    return d["arm"] == "null" or first_index(d["arm"]) > first_index(d["S"])


# --------------------------------------------------------------------------------------------------
# The predicates added 2026-11-16 (@4), each read from its row's text at Broiler.VM's 7c65a8fe; the words quoted in each
# docstring are the row's.

QUANTIFIER_AFTER = re.compile(r"[*+?]|\{\d+(?:,\d*)?\}")


def quantified_assertion(text: str) -> bool:
    """Whether `^`, `$` or `\\b` outside a class is followed by a quantifier, `^*`, `$+`, `\\b*`."""
    depth, i = 0, 0
    while i < len(text):
        ch = text[i]
        if ch == "\\":
            if depth == 0 and text[i:i + 2] == "\\b" and QUANTIFIER_AFTER.match(text, i + 2):
                return True
            i += 2
            continue
        if ch == "[":
            depth += 1
        elif ch == "]" and depth > 0:
            depth -= 1
        elif depth == 0 and ch in "^$" and QUANTIFIER_AFTER.match(text, i + 1):
            return True
        i += 1
    return False


def quantified_assertion_vector(d, context) -> bool:
    """Row S7's widening of 2026-11-01 (decision D-widen): "a quantified `^`, `$` and `\\b` without `u` (`^*`, `$+`,
    `\\b*`), which neither QuantifiableAssertion nor B.1.2 admits; this row holds them", read in "the runs on revision
    31" of the vectors: a P1 vector without u or v that J accepts and S refuses, the arbiter's reason nothing to repeat.
    Row S12 names the vectors `P1-regexp:syntax:s7-grammar-and-annex-b:0024` to `/0026` as S7's, and holds the arms' P4
    inputs of that reason itself, so the widening reads P1 alone."""
    return d["class"] == "P1" and not unicode_mode(d) and arm_accepts_what_s_refuses(d) and quantified_assertion(
        units_text(d["pattern"])) and context.refusal_code(d) == "grammar:nothing-to-repeat"


def set_operator_vector(d, context) -> bool:
    """Row S8's set-operator vectors, which row S12 names as "set-operator inputs of row S8's subject, which that row
    holds": `P1-regexp:syntax:s8-unicode-sets:0024` (`[a-z&&b]`) and `P1-regexp:matching:v-mode:0027`
    (`[a-z&&[^aeiou]]`) under `v`, the arbiter's reason `grammar:class-set-operator` (a range is no operand of `&&` or
    `--`): a P1 vector under v that J accepts and S refuses for that reason. The arms' P4 inputs of the reason are row
    S12's, by its text."""
    return d["class"] == "P1" and has_flag(d, "v") and arm_accepts_what_s_refuses(d) and context.refusal_code(
        d) == "grammar:class-set-operator"


def s7(d, context) -> bool:
    return quantified_lookbehind_accepted(d, context) or quantified_assertion_vector(d, context)


def s8(d, context) -> bool:
    return v_mode_syntax(d, context) or set_operator_vector(d, context)


def s7_or_s8_vector(d, context) -> bool:
    return quantified_assertion_vector(d, context) or set_operator_vector(d, context)


# Row S12's reasons, each with the flags its text names for it ("under `u` or `v`", "under `v`", "under `u`"), or
# None where it names none.
S12_REASONS = {"grammar:nothing-to-repeat": None, "grammar:invalid-escape": "uv", "grammar:lone-syntax-character": "uv",
               "grammar:invalid-class-escape": "uv", "grammar:trailing-backslash": None,
               "grammar:unterminated-class": None, "grammar:class-set-operator": "v", "grammar:invalid-group": "u"}

# Row S13's.
S13_REASONS = {"early:decimal-escape-past-group-count": "uv", "early:class-range-out-of-order": None,
               "early:class-set-range-out-of-order": None, "early:undefined-group-name": None,
               "early:invalid-braced-quantifier": None, "early:negated-class-with-strings": "v"}


def refused_for(d, context, reasons: dict) -> bool:
    """J accepts (or execs) a pattern S refuses, and the arbiter's reason is one of the row's, under the flags the row
    names for it."""
    if not arm_accepts_what_s_refuses(d):
        return False
    code = context.refusal_code(d)
    if code not in reasons:
        return False
    return reasons[code] is None or has_flag(d, reasons[code])


def grammar_refusal_accepted(d, context) -> bool:
    """Row S12: "A pattern the grammar refuses, by the arbiter's reasons" S12_REASONS, "by acceptance and by every
    `exec` of the pattern"; Broiler.JS "Accepted by the constructor, and each `exec` answers `null` or a match where the
    text throws". "Not this row's": the vectors rows S7 and S8 hold (s7_or_s8_vector)."""
    return refused_for(d, context, S12_REASONS) and not s7_or_s8_vector(d, context)


def early_error_accepted(d, context) -> bool:
    """Row S13: "A pattern an early error refuses, by the arbiter's reasons" S13_REASONS, "by acceptance and by every
    `exec`"; Broiler.JS "Accepted by the constructor, and each `exec` answers `null` or a match"."""
    return refused_for(d, context, S13_REASONS)


BOUND_PAST = 2 ** 31 - 1


def bound_past_int32(text: str) -> bool:
    """Whether a braced quantifier outside a class has a bound past 2^31 - 1, `c{4294967296}`."""
    for i, ch, in_class, _ in scan(text):
        if ch == "{" and not in_class:
            m = re.match(r"\{(\d+)(?:,(\d*))?\}", text[i:])
            if m and (int(m.group(1)) > BOUND_PAST or (m.group(2) and int(m.group(2)) > BOUND_PAST)):
                return True
    return False


def flags_without(d, letter: str) -> str:
    flags = "" if d["flags"] == "." else d["flags"]
    rest = flags.replace(letter, "")
    return rest or "."


def j_refuses_under(d, context, flags: str) -> bool:
    """Whether the same run refused the same pattern under `flags` where S accepted it: an acceptance case of the
    comparison (Context.j_refused)."""
    return (d["run"], d["pattern"], flags) in context.j_refused


def bound_refused_under_i(d, context) -> bool:
    """Row S15: "Under `i`, with or without `u` or `v`, a quantifier whose bound is past 2^31 - 1, in a pattern
    Broiler.JS accepts without `i`: Broiler.JS refuses it under `i`, `iu` and `iv` ... and does not refuse it with no
    flag, nor under `u` or `v` alone; by acceptance and by every `exec`". "Accepts without `i`" is read from the
    comparison: no acceptance difference of the same pattern in the same run under the flags less `i`, which is
    J's acceptance only where that case was run, so the predicate reads population P4, whose every pattern runs under
    "", u and v as under i, iu and iv."""
    return (d["class"] == "P4" and has_flag(d, "i") and arm_refuses_what_s_accepts(d)
            and bound_past_int32(units_text(d["pattern"])) and not j_refuses_under(d, context, flags_without(d, "i")))


S14_ONE_FLAG = frozenset({".", "s", "i", "m"})


def annex_b_refused(d, context) -> bool:
    """Row S14: "Without `u` or `v`, a pattern the text accepts, by B.1.2, and Broiler.JS refuses, by acceptance and by
    every `exec`", of four kinds by what triggers the refusal: "Refused with no flag as well as under its flags",
    "Refused only under `s`", "Refused only under `i`", "Refused only under `m`". The flags are read for the kinds: no
    flag, `s`, `i` or `m` alone, or other flags where the same run refused the same pattern with no flag (an acceptance
    difference of the comparison). "A quantifier bound past 2^31 - 1 that Broiler.JS refuses only under `i` is row
    S15's", which comes first; and since @5 such a bound under `i` that the comparison does not show refused without `i`
    is not this row's where row S15 does not take it either (outside P4), so that the order of the rows does not decide
    it."""
    if unicode_mode(d) or not arm_refuses_what_s_accepts(d):
        return False
    if (has_flag(d, "i") and bound_past_int32(units_text(d["pattern"]))
            and not j_refuses_under(d, context, flags_without(d, "i"))):
        return False
    return d["flags"] in S14_ONE_FLAG or j_refuses_under(d, context, ".")


DOTTED_DOTLESS_I = frozenset((0x0130, 0x0131))
LOOK_AROUND = re.compile(r"\(\?(?:=|!|<=|<!)")


def boundary_assertion(text: str) -> bool:
    """Whether `\\b` or `\\B` is in the pattern outside a class (in a class `\\b` is the backspace, and `\\B` is no
    assertion)."""
    i, depth = 0, 0
    while i < len(text):
        ch = text[i]
        if ch == "\\":
            if depth == 0 and text[i + 1:i + 2] in ("b", "B"):
                return True
            i += 2
            continue
        if ch == "[":
            depth += 1
        elif ch == "]" and depth > 0:
            depth -= 1
        i += 1
    return False


def look_around(text: str) -> bool:
    """Whether the pattern has a look-ahead or look-behind outside a class."""
    return any(not _escaped(text, m.start()) and not _in_class_at(text, m.start()) for m in LOOK_AROUND.finditer(text))


def empty_string_class(text: str) -> bool:
    """`\\q{}` in a class, `[A\\q{}]`."""
    return any(_in_class_at(text, m.start()) for m in re.finditer(r"\\q\{\}", text))


def names_only(d) -> bool:
    """Both sides match with the same spans and captures, and only the group names differ: Broiler.JS's translator
    rewriting a name (`$x` under `m` as `(?=$|\\r)x`, `π` under `i` as `[πΠϖ]`), which row M8's list does not name."""
    return (kind(d["S"]) == "match" and kind(d["arm"]) == "match"
            and canonical(d["S"])[1] == canonical(d["arm"])[1])


def code_point_at(units: list[int], i: int) -> int:
    """The code point the code unit at `i` belongs to: a lead or a trail of a pair gives the pair's."""
    u = units[i]
    if 0xD800 <= u <= 0xDBFF and i + 1 < len(units) and 0xDC00 <= units[i + 1] <= 0xDFFF:
        return 0x10000 + ((u - 0xD800) << 10) + (units[i + 1] - 0xDC00)
    if 0xDC00 <= u <= 0xDFFF and i > 0 and 0xD800 <= units[i - 1] <= 0xDBFF:
        return 0x10000 + ((units[i - 1] - 0xD800) << 10) + (u - 0xDC00)
    return u


def compared_positions(d, text: str, units: list[int]) -> range:
    """The code units of the subject an exec's difference can turn on: from one before the earliest start of either
    side's match to the latest end (an assertion at a position reads the unit before it and the unit at it), or the
    whole subject where the pattern has a look-around, which reads past the match. Where both sides answer and differ,
    a test the two engines answer differently lies on one side's matching path, which is inside its match."""
    if look_around(text):
        return range(len(units))
    spans = [match_parts(answer)[0] for answer in (d["S"], d["arm"]) if kind(answer) == "match"]
    start, end = min(s for s, _ in spans), max(e for _, e in spans)
    return range(max(start - 1, 0), min(end + 1, len(units)))


P5_TR_TR = {"I": "i", "[I]": "i", "i": "I", "[i]": "I"}


def exec_answered_otherwise(d, context) -> bool:
    """Row M8: "An `exec` both sides answer, where Broiler.JS answers otherwise", its inputs as the row lists them,
    each where it bears on the difference, and never a difference of the group names alone (names_only):
    - "`\\b` and `\\B` beside a non-ASCII letter ..., `\\w`, `\\d` and `\\s` beside them": `\\b` or `\\B` outside a class,
      and a letter past ASCII (General_Category L in the arbiter's UCD, Context.letters) at a compared position of the
      subject (compared_positions). "Them" is read as `\\b` and `\\B` (plural, where "a non-ASCII letter" is singular),
      so `\\w`, `\\d` and `\\s` add no input of their own; the complements `\\W`, `\\D` and `\\S` are not the row's;
    - "case-insensitive matches of U+0130 and U+0131 no row's pairs hold (`\\BI` under `i`)": `i` in force, and U+0130 or
      U+0131 in the pattern or at a compared position of the subject;
    - "an empty `\\q{}` in a class under `v`";
    - "under `tr-TR` P5's `I` and `[I]` against `i` and `i` and `[i]` against `I`", each pattern with its own subject.
    Rows M2, M2a, M2b, M3, M4, M7 and B2 come first.
    *(@8, the row's note of 2026-11-16, decision D-jsrest: "`\\b` and `\\B`" are read outside a class "as the text
    lexes a class without `v`, ending at its first unescaped `]`, a `[` inside it being a character" (text_tokens),
    with `u` or without; under `v` as before (boundary_assertion), where a class nests. @7 read them with
    boundary_assertion alone.)*"""
    if not both_answer_exec(d) or names_only(d):
        return False
    text = units_text(d["pattern"])
    units = decode_units(d["subject"])
    compared = compared_positions(d, text, units)
    if boundary_outside_class(text, has_flag(d, "v")) and any(
            code_point_at(units, i) > 0x7F and code_point_at(units, i) in context.letters for i in compared):
        return True
    if mode(d, "i") and (pattern_code_points(text) & DOTTED_DOTLESS_I
                         or any(units[i] in DOTTED_DOTLESS_I for i in compared)):
        return True
    if has_flag(d, "v") and empty_string_class(text):
        return True
    return (d["run"] == "J.tr-TR" and d["class"] == "P5" and mode(d, "i")
            and P5_TR_TR.get(text) == units_text(d["subject"]))


M9_INPUT = ("(?<$x>){1,}?\\B|(?!\u212a)", "y", "X\\", "2")


def throws_out_of_range(d) -> bool:
    """Row M9: "`(?<$x>){1,}?\\B|(?!\\u212A)` under `y` from `lastIndex` 2 of `X\\`", where "The `exec` throws .NET's
    `ArgumentOutOfRangeException`" and S answers. The row names that one input, and the predicate reads it alone."""
    flags = "" if d["flags"] == "." else d["flags"]
    return (d["op"] == "exec" and d["arm"] == "throw:ArgumentOutOfRangeException" and answers(d["S"])
            and (units_text(d["pattern"]), flags, units_text(d["subject"]), d["lastIndex"]) == M9_INPUT)


def match_parts(answer: str):
    return canonical(answer)[1] if answer.startswith("match:") else None


M4_CAPTURE_VECTORS = frozenset({("(.)", frozenset("gdu"))})
M4_NULL_VECTORS = frozenset({("[\\ud834\\udf06]", frozenset("gu")), ("[\U0001d306]", frozenset("gu"))})


def half_pair_capture_or_null(d) -> bool:
    """Row M4's widening of 2026-11-16 (decision D-jsrows), Broiler.JS by the same start: "`(.)` under `gdu`, whose
    capture Broiler.JS gives as the trail alone where the text's is the pair" (both match at the same span, and a
    capture S gives from lastIndex - 1 J gives from lastIndex, the same end), and "`[\\ud834\\udf06]` under `gu`, which
    it answers `null` where the text's answer is a match at index 1" (S matches at lastIndex, J answers null). The
    widening names those two vectors, so each shape is read with its vector's pattern and flags alone (@5)."""
    if not inside_pair(d) or not both_answer_exec(d):
        return False
    k = int(d["lastIndex"])
    s, arm = match_parts(d["S"]), match_parts(d["arm"])
    vector = (units_text(d["pattern"]).lower(), frozenset("" if d["flags"] == "." else d["flags"]))
    if s is not None and arm is None:
        return vector in M4_NULL_VECTORS and s[0][0] == k
    if vector not in M4_CAPTURE_VECTORS:
        return False
    if s is None or arm is None or s[0] != arm[0] or len(s) != len(arm):
        return False
    changed = [(a, b) for a, b in zip(s[1:], arm[1:]) if a != b]
    return bool(changed) and all(a is not None and b is not None and a == (k - 1, b[1]) and b[0] == k
                                 for a, b in changed)


WORD_FOLDING = frozenset(range(0x41, 0x5B)) | frozenset(range(0x61, 0x7B)) | {0x017F, 0x212A}


def word_difference_operand(text: str) -> bool:
    """`\\w` as the first operand of a class difference, alone or inside a nested class: `[\\w--I]`, `[[\\w--...]]`."""
    return any(not _escaped(text, m.start()) for m in re.finditer(r"\[\\w--", text))


def word_difference_keeps_folding(d) -> bool:
    """Row S8a (Broiler.VM): "Under `iv`, `\\w` as the first operand of a class difference, alone or inside a nested
    class", each `exec`, where "the class keeps `\\w`'s members that case-fold to another code point, `A` to `Z`,
    U+017F and U+212A, ... and the case-insensitive match then admits their partners" (`[\\w--I]` matches `i` and
    `I`, `[\\w--\\ud83d--\\S]?` matches `s`, `w` and `W` where the text's answer is the empty match, `[[\\w--...]]*`
    takes one more letter): an exec under i and v both answer, V answers a match, and the code units V's match takes
    that S's answer does not hold a letter `A` to `Z`, `a` to `z`, U+017F or U+212A."""
    if not (has_flag(d, "i") and has_flag(d, "v")) or not both_answer_exec(d) or kind(d["arm"]) != "match":
        return False
    if not word_difference_operand(units_text(d["pattern"])):
        return False
    start, end = match_parts(d["arm"])[0]
    taken = set(range(start, end))
    if kind(d["S"]) == "match":
        s_start, s_end = match_parts(d["S"])[0]
        taken -= set(range(s_start, s_end))
    subject = decode_units(d["subject"])
    return any(subject[i] in WORD_FOLDING for i in taken)


# --------------------------------------------------------------------------------------------------
# The predicates added 2026-11-16 (@7), each read from its row's text at Broiler.VM's 44511066 (decision D-jsexec of
# JSD-0081, Broiler-Platform/Broiler.VM#212), whose note says when each row takes an input: rows M11 to M14 "only where
# the arbiter's answer to the pattern, with the row's construct written as .NET reads it (the row's reading, in its
# Broiler.JS cell), is Broiler.JS's, up to row M10's names, and not the text's"; row M10 "where the captures are the
# text's and only the names differ, each by the translator's rewrite the row names"; row M15 "where arm J under the
# invariant culture answers as the text does". A construct in the input that does not move the answer to arm J's takes
# nothing, so no predicate reads a feature of the input alone.

def unit_text(field: str) -> str:
    """A pattern as one character per UTF-16 code unit (a pair as its two halves): the text a reading rewrites, which
    is written back unit for unit (Context.reading), so the arbiter reads the pattern's own units with the reading's."""
    return "".join(chr(u) for u in decode_units(field))


MODIFIER_GROUP = re.compile(r"\(\?([ims]*)(?:-([ims]*))?:")


def modifier_group(text: str) -> bool:
    """Whether an inline modifier group, `(?m:`, `(?-i:`, `(?i-s:`, stands in the pattern outside a class."""
    for i, ch, in_class, _ in scan(text):
        if ch == "(" and not in_class:
            m = MODIFIER_GROUP.match(text, i)
            if m and (m.group(1) or m.group(2)):
                return True
    return False


# Row M10's rewrites of `$`, each with when the row names it: "`$x` as `(?=$|\r)x` under `m`, as `\zx` where an inline
# modifier group stands in the pattern without `m` and as `(?=[\n\r\u2028\u2029]|\z)x` with one under `m`" (the
# backslashes are the translator's text, as the arms' answers spell the name).
M10_ANCHORS = {"(?=$|\\r)": "under m", "\\z": "a modifier group without m",
               "(?=[\\n\\r\\u2028\\u2029]|\\z)": "a modifier group under m"}
ARMS_UNIT_ESCAPE = re.compile(r"\\u([0-9A-Fa-f]{4})")


def arms_name(name: str) -> str:
    """A group name as the arms write it (escape_name) decoded: each `\\uXXXX` the unit it stands for."""
    return ARMS_UNIT_ESCAPE.sub(lambda m: chr(int(m.group(1), 16)), name)


def anchor_rewrite_named(d, rewrite: str) -> bool:
    """Whether the row names that rewrite of `$` for the input's flags and pattern: `(?=$|\\r)` under `m` with no
    inline modifier group, `\\z` with one and without `m`, the third anchor with one under `m`."""
    group = modifier_group(units_text(d["pattern"]))
    if rewrite == "(?=$|\\r)":
        return has_flag(d, "m") and not group
    if rewrite == "\\z":
        return group and not has_flag(d, "m")
    return group and has_flag(d, "m")


def _translator_unit_escape(unit: int) -> str:
    """A code unit as arm J's translator writes it in a neutralized class (AppendUnicodeEscape): `\\xhh` up to U+00FF,
    `\\uhhhh` above, lower-case hex."""
    return f"\\x{unit:02x}" if unit <= 0xFF else f"\\u{unit:04x}"


def case_rewrites(d, ch: str, context) -> list[str]:
    """The rewrites of a character of a name the row names, as arm J's translator writes them: under `i`, "a character
    of the name the translator's case expansion rewrites: one whose Canonicalize class holds another member and a
    non-ASCII one, as `π`", by a class of them, the character first and the others ascending (`[πΠϖ]`,
    GetCaseFoldEquivalents, AppendWithEquivalents), "or one .NET's invariant lowercase folds into another Canonicalize
    class", by `(?-i:…)` over its Canonicalize class, each member escaped (GetNonUnicodeOverFoldClass,
    AppendNeutralizedClass). The class is the arbiter's Canonicalize without u or v (Context.canonicalize_class); a
    character whose class is ASCII alone, as `x`, has none. Any character of the name is read, not only a letter."""
    if not has_flag(d, "i"):
        return []
    unit = ord(ch)
    members = context.canonicalize_class(unit)
    out = []
    if len(members) > 1 and any(m > 0x7F for m in members):
        out.append("[" + ch + "".join(chr(m) for m in members if m != unit) + "]")
    if context.over_folds(unit):
        body = "".join(_translator_unit_escape(m) for m in members)
        out.append("(?-i:" + (body if len(members) == 1 else "[" + body + "]") + ")")
    return out


def rewritten_name(d, s_name: str, arm_name: str, context) -> bool:
    """Row M10: arm J's name is S's with each `$` written as one of the translator's anchors and each other character
    as itself or as one of its rewrites (case_rewrites), each rewrite where the row names it; and the two differ."""
    s, arm = arms_name(s_name), arms_name(arm_name)
    if s == arm:
        return False
    parts = []
    for ch in s:
        if ch == "$":
            forms = [a for a in M10_ANCHORS if anchor_rewrite_named(d, a)]
        else:
            forms = [ch] + case_rewrites(d, ch, context)
        if not forms:
            return False
        parts.append("(?:" + "|".join(re.escape(f) for f in forms) + ")")
    return re.fullmatch("".join(parts), arm) is not None


def name_indices(answer: str) -> dict:
    """An arm's match answer's names, each with the index of its capture as the answer writes it."""
    if not answer.startswith("match:"):
        return {}
    names = answer[6:].partition("|")[2]
    return dict(entry.rpartition("=")[::2] for entry in names.split(",")) if names else {}


def names_rewritten(d, s_canonical, arm_canonical, context, indices=None) -> bool:
    """Both match with the same captures, and every name S gives that arm J does not is one J gives rewritten, at the
    same capture (rewritten_name); at least one differs. With `indices` (S's names and J's, each to its capture's
    index), "the same capture" is the same group, not only the same span; an arbiter's answer to a reading (rows M11 to
    M14) reports no index, so there the span is read."""
    if not (isinstance(s_canonical, tuple) and isinstance(arm_canonical, tuple)):
        return False
    if s_canonical[1] != arm_canonical[1]:
        return False
    s_names, arm_names = dict(s_canonical[2]), dict(arm_canonical[2])
    missing = {k: v for k, v in s_names.items() if k not in arm_names}
    extra = {k: v for k, v in arm_names.items() if k not in s_names}
    if not missing or len(missing) != len(extra):
        return False
    for name, capture in missing.items():
        if not any(at == capture and (indices is None or indices[0].get(name) == indices[1].get(other))
                   and rewritten_name(d, name, other, context) for other, at in extra.items()):
            return False
    return all(arm_names[k] == v for k, v in s_names.items() if k in arm_names)


def group_name_rewritten(d, context) -> bool:
    """Row M10: "Without `u` or `v`, a named group whose name holds `$`, with the `m` flag or an inline modifier group
    in the pattern, or, under `i`, a character of the name the translator's case expansion rewrites ...; an `exec`
    both sides answer with the same match and the same captures, where Broiler.JS reports the group under another
    name".
    No arbiter call: S's answer is the arbiter's, and the row reads the names."""
    return (not unicode_mode(d) and both_answer_exec(d)
            and names_rewritten(d, canonical(d["S"]), canonical(d["arm"]), context,
                                (name_indices(d["S"]), name_indices(d["arm"]))))


def answered_as_read(d, context, reading: str | None) -> bool:
    """The row's condition for rows M11 to M14: the arbiter's answer to the reading is arm J's, up to row M10's names,
    and not the text's (S's, which is the arbiter's answer to the pattern). A reading of None (the pattern holds no
    such construct) takes nothing."""
    if reading is None:
        return False
    answer = context.reading(d, reading)
    arm = canonical(d["arm"])
    return answer != canonical(d["S"]) and (answer == arm or names_rewritten(d, answer, arm, context))


def _class_end(text: str, i: int) -> int:
    """The index after the class without `v` that opens at text[i] == "[": its first unescaped `]` after the
    optional `^` closes it (22.2.1, ClassContents; a `]` first is that close too)."""
    j = i + 1
    if text[j:j + 1] == "^":
        j += 1
    while j < len(text):
        if text[j] == "\\":
            j += 2
            continue
        if text[j] == "]":
            return j + 1
        j += 1
    return len(text)


def _group_end(text: str, i: int) -> int | None:
    """The index after the group that opens at text[i] == "(", lexed without `u` or `v`; None when it never closes."""
    depth, j = 0, i
    while j < len(text):
        ch = text[j]
        if ch == "\\":
            j += 2
            continue
        if ch == "[":
            j = _class_end(text, j)
            continue
        if ch == "(":
            depth += 1
        elif ch == ")":
            depth -= 1
            if depth == 0:
                return j + 1
        j += 1
    return None


OPTIONAL = ("{0,1}?", "??", "{0,1}", "?")
LOOK_AROUND_OPENERS = ("(?=", "(?!", "(?<=", "(?<!")


def optional_group_reading(text: str) -> str | None:
    """Row M11's reading: "each such `(G)?` or `(G){0,1}` written `(?:(G)|)` and each `(G)??` or `(G){0,1}?` written
    `(?:|(G))`, which keep the empty iteration", for every group but a look-around, inner groups too; None when the
    pattern holds no such group."""
    changed = False

    def walk(s: str) -> str:
        nonlocal changed
        out, i = [], 0
        while i < len(s):
            ch = s[i]
            if ch == "\\":
                out.append(s[i:i + 2])
                i += 2
                continue
            if ch == "[":
                j = _class_end(s, i)
                out.append(s[i:j])
                i = j
                continue
            if ch == "(":
                j = _group_end(s, i)
                if j is None:
                    out.append(s[i:])
                    break
                head = 1
                for opener in ("?:", "?=", "?!", "?<=", "?<!"):
                    if s.startswith(opener, i + 1):
                        head = 1 + len(opener)
                        break
                else:
                    if s.startswith("?<", i + 1):
                        head = s.index(">", i) + 1 - i
                    elif s.startswith("?", i + 1):
                        k = s.find(":", i)
                        head = k + 1 - i if k > 0 else 1
                group = s[i:i + head] + walk(s[i + head:j - 1]) + ")"
                quantifier = next((q for q in OPTIONAL if s.startswith(q, j)), None)
                if quantifier and not s.startswith(LOOK_AROUND_OPENERS, i):
                    lazy = quantifier in ("??", "{0,1}?")
                    out.append("(?:|" + group + ")" if lazy else "(?:" + group + "|)")
                    changed = True
                    i = j + len(quantifier)
                    continue
                out.append(group)
                i = j
                continue
            out.append(ch)
            i += 1
        return "".join(out)

    reading = walk(text)
    return reading if changed else None


def empty_iteration_kept(d, context) -> bool:
    """Row M11: "Without `u` or `v`, a group, not a look-around, quantified by `?`, `??`, `{0,1}` or `{0,1}?`, whose
    body can match the empty string where the quantifier is tried ...; an `exec` both sides answer, where Broiler.JS
    keeps the iteration that matched the empty string ...; and the arbiter's answer to the pattern with each such group
    read as .NET reads it ... is Broiler.JS's, up to row M10's names, and not the text's". The body's matching empty is
    not read apart: where it cannot, the reading answers as the text does, and the row takes nothing."""
    return (not unicode_mode(d) and both_answer_exec(d)
            and answered_as_read(d, context, optional_group_reading(unit_text(d["pattern"]))))


# The General_Category values .NET's `\p{…}` knows (row M12: "with a General_Category name .NET knows"): every
# two-letter value of UnicodeData.txt and each one-letter group, which every value it begins makes up.
GENERAL_CATEGORIES = frozenset("L Lu Ll Lt Lm Lo M Mn Mc Me N Nd Nl No P Pc Pd Ps Pe Pi Pf Po S Sm Sc Sk So Z Zs Zl "
                               "Zp C Cc Cf Cs Co Cn".split())
PROPERTY_IN_CLASS = re.compile(r"\\([pP])\{([A-Za-z]+)\}")
# The class escapes a `-` before a property escape follows in row M12's note (@8), as property_reading writes them.
CLASS_ESCAPES = frozenset(("\\d", "\\D", "\\w", "\\W", "\\s", "\\S"))


def _units_as_ranges(units) -> str:
    out, start, previous = [], None, None
    for u in units:
        if start is None:
            start = previous = u
        elif u == previous + 1:
            previous = u
        else:
            out.append((start, previous))
            start = previous = u
    if start is not None:
        out.append((start, previous))
    return "".join(f"\\u{a:04X}" if a == b else f"\\u{a:04X}-\\u{b:04X}" for a, b in out)


def property_reading(text: str, category_units) -> str | None:
    """Row M12's reading of `\\p{…}` and `\\P{…}` in a class: "each such `\\p{…}` written as the BMP code units of
    that General_Category in the arbiter's UCD (a one-letter name, every category it begins), each `\\P{…}` as the
    other code units, a `-` right after either, not before `]`, as `\\-`". `category_units(name)` gives the units.
    *(@8, the row's note of 2026-11-16, decision D-jsrest: "the row's reading also writes a `-` right before `\\p{…}` or
    `\\P{…}` that follows `\\d`, `\\D`, `\\w`, `\\W`, `\\s` or `\\S` as `\\-`, as the translator writes it", so
    `[\\n-\\w\\B\\d-\\P{N}]` reads `0-9`, `-` and the other code units. @7 left that `-` as written.)*"""
    out, i, in_class, changed = [], 0, False, False
    while i < len(text):
        ch = text[i]
        if ch == "\\" and i + 1 < len(text):
            m = PROPERTY_IN_CLASS.match(text, i) if in_class else None
            if m and m.group(2) in GENERAL_CATEGORIES:
                if len(out) >= 2 and out[-1] == "-" and out[-2] in CLASS_ESCAPES:
                    out[-1] = "\\-"
                units = category_units(m.group(2))
                if m.group(1) == "P":
                    inside = set(units)
                    units = [u for u in range(0x10000) if u not in inside]
                out.append(_units_as_ranges(units))
                i = m.end()
                changed = True
                if text[i:i + 1] == "-" and text[i + 1:i + 2] not in ("]", ""):
                    out.append("\\-")
                    i += 1
                continue
            out.append(text[i:i + 2])
            i += 2
            continue
        if ch == "[" and not in_class:
            in_class = True
        elif ch == "]" and in_class:
            in_class = False
        out.append(ch)
        i += 1
    return "".join(out) if changed else None


def bell_reading(text: str) -> str | None:
    """Row M12's reading of `\\a` in a class: "`\\a` as U+0007", written `\\x07`."""
    out, i, in_class, changed = [], 0, False, False
    while i < len(text):
        ch = text[i]
        if ch == "\\" and i + 1 < len(text):
            if in_class and text[i + 1] == "a":
                out.append("\\x07")
                i += 2
                changed = True
                continue
            out.append(text[i:i + 2])
            i += 2
            continue
        if ch == "[" and not in_class:
            in_class = True
        elif ch == "]" and in_class:
            in_class = False
        out.append(ch)
        i += 1
    return "".join(out) if changed else None


def class_escape_read_by_net(d, context) -> bool:
    """Row M12: "Without `u` or `v`, in a class, `\\p{…}` or `\\P{…}` with a General_Category name .NET knows ...,
    or `\\a`, ...; an `exec` both sides answer, where Broiler.JS answers otherwise and the arbiter's answer to the
    pattern with each such escape read as .NET reads it ... is Broiler.JS's, up to row M10's names, and not the
    text's". One reading is asked: every such escape the pattern holds read, the property escapes and `\\a` together
    where it holds both kinds; a reading with one kind still written as the text has it takes nothing."""
    if unicode_mode(d) or not both_answer_exec(d):
        return False
    text = unit_text(d["pattern"])
    properties = property_reading(text, context.category_units)
    reading = bell_reading(properties if properties is not None else text)
    return answered_as_read(d, context, reading if reading is not None else properties)


CLASS_ESCAPE_LETTERS = frozenset("dDwWsS")


def _neutral_dashes(body: str) -> str:
    """The translator's NeutralizeAnnexBClassRangeDashes over a class's members: a `-` beside `\\d`, `\\D`, `\\w`,
    `\\W`, `\\s` or `\\S` written `\\-`."""
    out, i = [], 0
    while i < len(body):
        if body[i] == "\\":
            out.append(body[i:i + 2])
            i += 2
            continue
        if body[i] == "-":
            before = i >= 2 and body[i - 2] == "\\" and body[i - 1] in CLASS_ESCAPE_LETTERS
            after = body[i + 1:i + 2] == "\\" and body[i + 2:i + 3] in CLASS_ESCAPE_LETTERS
            if before or after:
                out.append("\\-")
                i += 1
                continue
        out.append(body[i])
        i += 1
    return "".join(out)


def _net_class(text: str, i: int, nested_class: bool = False):
    """The class that opens at text[i] == "[" as .NET's grammar reads it: (its reading, the index after it, whether it
    holds a subtraction), or None where `-[` is not followed, after the nested class, by the class's `]`.
    *(@8, row M13's note of 2026-11-16, decision D-jsrest: "the nested class, read as .NET reads it, ends at its first
    `]`, even right after its `[` or `[^`, so `[\\w--\\D--[^]]` reads `\\w`, `-` and the non-digits less every code
    unit": a nested class (`nested_class`) ends there. @7 read a `]` right after a nested class's `[` or `[^` as a
    member.)*"""
    j, negated = i + 1, False
    if text[j:j + 1] == "^":
        negated, j = True, j + 1
    start, first = j, True
    while j < len(text):
        ch = text[j]
        if ch == "\\":
            j += 2
            first = False
            continue
        if ch == "]" and (not first or nested_class):
            return "[" + ("^" if negated else "") + _neutral_dashes(text[start:j]) + "]", j + 1, False
        if ch == "-" and not first and text[j + 1:j + 2] == "[":
            nested = _net_class(text, j + 1, True)
            if nested is None or text[nested[1]:nested[1] + 1] != "]":
                return None
            members = "[" + ("^" if negated else "") + _neutral_dashes(text[start:j]) + "]"
            return "(?:(?!" + nested[0] + ")" + members + ")", nested[1] + 1, True
        first = False
        j += 1
    return None


def subtraction_reading(text: str) -> str | None:
    """Row M13's reading: "each such class written `(?:(?![B])[A])`, A the members before `-[` and B the nested class's,
    read the same way where it holds `-[` itself, with a `-` beside `\\d`, `\\D`, `\\w`, `\\W`, `\\s` or `\\S` in A
    written `\\-`, as the translator writes it"; a class with no `-[` after its first member is left as written."""
    out, i, changed = [], 0, False
    while i < len(text):
        ch = text[i]
        if ch == "\\":
            out.append(text[i:i + 2])
            i += 2
            continue
        if ch == "[":
            read = _net_class(text, i)
            if read and read[2]:
                out.append(read[0])
                i = read[1]
                changed = True
                continue
            j = _class_end(text, i)
            out.append(text[i:j])
            i = j
            continue
        out.append(ch)
        i += 1
    return "".join(out) if changed else None


def class_subtraction(d, context) -> bool:
    """Row M13: "Without `u` or `v`, a class that holds `-[` after its first member ...; an `exec` both sides answer,
    where Broiler.JS answers otherwise and the arbiter's answer to the pattern with each such class read as .NET reads
    it ... is Broiler.JS's, up to row M10's names, and not the text's"."""
    return (not unicode_mode(d) and both_answer_exec(d)
            and answered_as_read(d, context, subtraction_reading(unit_text(d["pattern"]))))


def dash_reading(text: str) -> str | None:
    """Row M14's reading: in a class, "each such `\\--` written `\\-\\-`", where `\\-` is "followed by `-` and a
    member" (not `]`)."""
    out, i, in_class, changed = [], 0, False, False
    while i < len(text):
        ch = text[i]
        if ch == "\\" and i + 1 < len(text):
            if in_class and text[i + 1] == "-" and text[i + 2:i + 3] == "-" and text[i + 3:i + 4] not in ("]", ""):
                out.append("\\-\\-")
                i += 3
                changed = True
                continue
            out.append(text[i:i + 2])
            i += 2
            continue
        if ch == "[" and not in_class:
            in_class = True
        elif ch == "]" and in_class:
            in_class = False
        out.append(ch)
        i += 1
    return "".join(out) if changed else None


def escaped_dash_starts_no_range(d, context) -> bool:
    """Row M14: "Without `u` or `v`, in a class, `\\-` followed by `-` and a member ...; an `exec` both sides answer,
    where Broiler.JS answers otherwise and the arbiter's answer to the pattern with each such `\\--` read as .NET reads
    it ... is Broiler.JS's, up to row M10's names, and not the text's"."""
    return (not unicode_mode(d) and both_answer_exec(d)
            and answered_as_read(d, context, dash_reading(unit_text(d["pattern"]))))


ASCII_I = frozenset((0x49, 0x69))


def culture_casing_of_i(d, context) -> bool:
    """Row M15: "Under `i` without `u` or `v`, under `tr-TR` or `lt-LT`, an input that holds `i` or `I` and neither
    U+0130 nor U+0131 (those are row M2a's), on which arm J under the invariant culture answers as the text does; an
    `exec` both sides answer, where Broiler.JS under the culture answers otherwise". The invariant culture's answer is
    the text's where the comparison holds no difference of run J for the same input (Context.invariant_differs). P5's
    inputs row M8 names are that row's, which comes first. No arbiter call.
    *(@8, the row's note of 2026-11-16, decision D-jsrest: "on which arm J under the invariant culture answers as the
    text does" reads "as the text does, up to row M10's names": where run J differs on the input, the input is still
    the row's when run J's answer is S's match with only the names rewritten as row M10 reads them, at the same groups
    (names_rewritten; Context.invariant_answer). @7 read no difference of run J alone.)*"""
    if d["run"] not in ("J.tr-TR", "J.lt-LT") or not both_answer_exec(d) or not has_flag(d, "i") or unicode_mode(d):
        return False
    points = input_code_points(d)
    if not points & ASCII_I or points & DOTTED_AND_DOTLESS:
        return False
    if not context.invariant_differs(d):
        return True
    invariant = context.invariant_answer(d)
    return invariant is not None and kind(invariant) == "match" and names_rewritten(
        d, canonical(d["S"]), canonical(invariant), context, (name_indices(d["S"]), name_indices(invariant)))


# --------------------------------------------------------------------------------------------------
# The predicates added 2026-11-16 (@8), each read from its row's text at Broiler.VM's 626e4ed8 (decision D-jsrest of
# JSD-0081, Broiler-Platform/Broiler.VM#215), whose note says: "Rows M16, M21, M23 and M25 take an input only where the
# arbiter's answer to the row's reading is Broiler.JS's, up to row M10's names, and not the text's; rows M17 to M20,
# M22, M24 and M26 by their construct and condition." Every one reads arm J, without `u` or `v`, an `exec` both sides
# answer ("an `exec` both sides answer, where Broiler.JS answers otherwise"), and S's answer the arbiter's (the
# classifier reaches its rows only then). Where a row says "class", the pattern is lexed as the text lexes it without
# `v` (text_tokens): a class ends at its first unescaped `]`, and a `[` inside it is a character.

def text_tokens(text: str):
    """The pattern's tokens as the text lexes it without `v` (22.2.1 with Annex B): yields (index, token, in_class,
    first), each escape one token of two characters (`\\c` and what follows it are two tokens), a class's negating `^`
    the token `^NEG`, and `first` true for the token right after a class's `[` or `[^`. A class ends at its first
    unescaped `]`, a `]` right after its `[` included (`[]` is the empty class), and a `[` inside it is a member."""
    i, in_class, first = 0, False, False
    while i < len(text):
        ch = text[i]
        if ch == "\\" and i + 1 < len(text):
            yield i, text[i:i + 2], in_class, first
            i += 2
            first = False
            continue
        if not in_class and ch == "[":
            yield i, ch, False, False
            in_class, i = True, i + 1
            if text[i:i + 1] == "^":
                yield i, "^NEG", True, True
                i += 1
            first = True
            continue
        if in_class and ch == "]":
            yield i, ch, True, first
            in_class, first = False, False
            i += 1
            continue
        yield i, ch, in_class, first
        i += 1
        first = False


def boundary_outside_class(text: str, v: bool) -> bool:
    """Row M8's `\\b` or `\\B` outside a class, as its note of 2026-11-16 (D-jsrest) lexes a class without `v`
    (text_tokens); under `v`, where a class nests, as boundary_assertion reads it."""
    if v:
        return boundary_assertion(text)
    return any(not in_class and token in ("\\b", "\\B") for _, token, in_class, _ in text_tokens(text))


def _joined(tokens) -> str:
    return "".join("^" if token == "^NEG" else token for token in tokens)


def _can_match_empty(node) -> bool:
    """Whether a node of the arbiter's tree can match the empty string, read from its kind alone: an assertion, a
    look-around and a back-reference can; a character, `.` and a class cannot."""
    name = type(node).__name__
    if name in ("Character", "Dot", "ClassEscape", "ClassRanges", "SetClass"):
        return False
    if name in ("Group", "Modifiers"):
        return _can_match_empty(node.disjunction)
    if name == "Quantified":
        return node.min == 0 or _can_match_empty(node.atom)
    if name == "Alternative":
        return all(_can_match_empty(term) for term in node.terms)
    if name == "Disjunction":
        return any(_can_match_empty(alternative) for alternative in node.alternatives)
    return True


def _can_match_character(node) -> bool:
    """Whether a node of the arbiter's tree can match a character, read from its kind alone: a character, `.`, a class
    with a member or negated, and a back-reference can; an assertion and a look-around cannot."""
    name = type(node).__name__
    if name in ("Assertion", "LookAround"):
        return False
    if name == "ClassRanges":
        return bool(node.items) or node.invert
    if name in ("Group", "Modifiers"):
        return _can_match_character(node.disjunction)
    if name == "Quantified":
        return node.max != 0 and _can_match_character(node.atom)
    if name == "Alternative":
        return any(_can_match_character(term) for term in node.terms)
    if name == "Disjunction":
        return any(_can_match_character(alternative) for alternative in node.alternatives)
    return True


def _nodes(node):
    yield node
    for attribute in ("disjunction", "atom"):
        child = getattr(node, attribute, None)
        if child is not None:
            yield from _nodes(child)
    for attribute in ("terms", "alternatives"):
        for child in getattr(node, attribute, None) or ():
            yield from _nodes(child)


def nullable_repeats(tree):
    """`GapScan`'s nullable shape, as rows M16, M17 and M19 name it: each quantifier "whose maximum is above 1 over a
    body that can match both the empty string and a character", in the arbiter's tree."""
    return [n for n in _nodes(tree) if type(n).__name__ == "Quantified" and n.max > 1
            and _can_match_empty(n.atom) and _can_match_character(n.atom)]


def capture_in_look_behind(tree) -> bool:
    """`GapScan`'s other shape, as row M16 names it: "a capturing group or a back-reference inside a look-behind"."""
    return any(type(n).__name__ == "LookAround" and not n.ahead and any(
        type(m).__name__ in ("Group", "Backreference") for m in _nodes(n.disjunction)) for n in _nodes(tree))


def control_reading(text: str) -> str | None:
    """Row M16's reading: "each such `\\c` written `\\\\`, the character after it kept", where no ASCII letter follows
    the `\\c`, inside a class and outside one; None where the pattern holds no such `\\c`."""
    out, changed = [], False
    for i, token, _, _ in text_tokens(text):
        if token == "\\c":
            after = text[i + 2:i + 3]
            if not (after.isascii() and after.isalpha()):
                out.append("\\\\")
                changed = True
                continue
        out.append(token)
    return _joined(out) if changed else None


def control_escape_read_as_backslash(d, context) -> bool:
    """Row M16: "Without `u` or `v`, `\\c` that no ASCII letter follows, in a pattern `TryRouteToBroiler` hands to
    Broiler.Regex `0.1.0-preview.4` by one of `GapScan`'s shapes (a capturing group or a back-reference inside a
    look-behind, or a quantifier whose maximum is above 1 over a body that can match both the empty string and a
    character) ...; an `exec` both sides answer, where Broiler.JS answers otherwise and the arbiter's answer to the
    pattern with each such `\\c` read as preview.4 reads it (the reading in the Broiler.JS cell) is Broiler.JS's, up to
    row M10's names, and not the text's". The shapes are read in the arbiter's tree of the pattern (nullable_repeats,
    capture_in_look_behind); whether preview.4's own parse succeeds, which `TryRouteToBroiler` also needs, is not read
    apart: the reading's answer is preview.4's only where it does."""
    if unicode_mode(d) or not both_answer_exec(d):
        return False
    text = unit_text(d["pattern"])
    reading = control_reading(text)
    if reading is None:
        return False
    tree = context.parse(text)
    if tree is None or not (nullable_repeats(tree) or capture_in_look_behind(tree)):
        return False
    return answered_as_read(d, context, reading)


def _group_names(text: str) -> list[str]:
    """Each group name as written, `(?<name>` outside a class (text_tokens)."""
    out = []
    for i, token, in_class, _ in text_tokens(text):
        if token == "(" and not in_class:
            m = NAME.match(text, i)
            if m:
                out.append(m.group(1))
    return out


def braced_name_in_nullable_repeat(d, context) -> bool:
    """Row M17: "Without `u` or `v`, a group name written with `\\u{…}` (`(?<\\u{62}>…)`), which the text accepts, in a
    pattern that holds a quantifier whose maximum is above 1 over a body that can match both the empty string and a
    character (`GapScan`'s nullable shape) ...; an `exec` both sides answer, where Broiler.JS answers otherwise". The
    name is a group's outside a class; the shape is read in the arbiter's tree (nullable_repeats)."""
    if unicode_mode(d) or not both_answer_exec(d):
        return False
    text = unit_text(d["pattern"])
    if not any("\\u{" in name for name in _group_names(text)):
        return False
    tree = context.parse(text)
    return tree is not None and bool(nullable_repeats(tree))


def braced_name_reference(d, context) -> bool:
    """Row M18: "Without `u` or `v`, `\\k<…>` whose name is written with `\\u{…}` (`\\k<\\u{62}>`) and names a group of
    the pattern ...; an `exec` both sides answer, where Broiler.JS answers otherwise". The reference stands outside a
    class, and its name's value (decoded_name) is the value of a group's name."""
    if unicode_mode(d) or not both_answer_exec(d):
        return False
    text = unit_text(d["pattern"])
    names = {decoded_name(name) for name in _group_names(text)}
    for i, token, in_class, _ in text_tokens(text):
        if token == "\\k" and not in_class:
            m = re.match(r"<([^>]*)>", text[i + 2:])
            if m and "\\u{" in m.group(1) and decoded_name(m.group(1)) in names:
                return True
    return False


OCTAL = "01234567"
OCTAL_MARK = 0xE000


def _octal_marked(text: str) -> str | None:
    """The pattern with each legacy octal escape `\\0` followed by an octal digit outside a class (B.1.2's
    LegacyOctalEscapeSequence: `\\0` and one octal digit, or two) written `\\uE000`, one atom for one atom, so that
    the arbiter's tree shows where it stands; None where there is none, or where the pattern holds U+E000 itself."""
    if chr(OCTAL_MARK) in text:
        return None
    out, last, found = [], 0, False
    for i, token, in_class, _ in text_tokens(text):
        if token == "\\0" and not in_class and text[i + 2:i + 3] and text[i + 2] in OCTAL:
            digits = 2 if text[i + 3:i + 4] and text[i + 3] in OCTAL else 1
            out += [text[last:i], "\\uE000"]
            last, found = i + 2 + digits, True
    return "".join(out) + text[last:] if found else None


def octal_in_nullable_repeat(d, context) -> bool:
    """Row M19: "Without `u` or `v`, a legacy octal escape `\\0` followed by an octal digit (`\\07`) in the body of a
    quantifier whose maximum is above 1, over a body that can match both the empty string and a character, as
    `(\\B\\07{0,2}){0,2}`; an `exec` both sides answer, where Broiler.JS answers otherwise". Each such escape outside a
    class is found in that body of the arbiter's tree (_octal_marked, nullable_repeats)."""
    if unicode_mode(d) or not both_answer_exec(d):
        return False
    marked = _octal_marked(unit_text(d["pattern"]))
    if marked is None:
        return False
    tree = context.parse(marked)
    return tree is not None and any(
        type(n).__name__ == "Character" and n.value == OCTAL_MARK for q in nullable_repeats(tree) for n in _nodes(q.atom))


def octal_before_digit_class(d, context) -> bool:
    """Row M20: "Without `u` or `v`, in a class, a legacy octal escape `\\0` and an octal digit right before `\\d`, as
    `\\xa0|[\\07\\d]?`; an `exec` both sides answer, where Broiler.JS answers otherwise": `\\0`, one octal digit and
    `\\d` (`[\\077\\d]`, whose escape .NET reads whole, is not it)."""
    if unicode_mode(d) or not both_answer_exec(d):
        return False
    text = unit_text(d["pattern"])
    return any(in_class and token == "\\0" and re.match(r"[0-7]\\d", text[i + 2:]) is not None
               for i, token, in_class, _ in text_tokens(text))


def modifier_group_lexed(text: str) -> bool:
    """Whether an inline modifier group, adding or removing a flag, stands outside a class (text_tokens)."""
    for i, token, in_class, _ in text_tokens(text):
        if token == "(" and not in_class:
            m = MODIFIER_GROUP.match(text, i)
            if m and (m.group(1) or m.group(2)):
                return True
    return False


NON_WHITE_SPACE_AS_NET_READS = "\\u0000-\\u0008\\u000E-\\u001F\\u0021-\\uFFFF"


def non_white_space_reading(text: str) -> str | None:
    """Row M21's reading: "each such `\\S` written `\\u0000-\\u0008\\u000E-\\u001F\\u0021-\\uFFFF`, a `-` beside it
    written `\\-`", for `\\S` in a class; None where there is none."""
    tokens = list(text_tokens(text))
    out, changed = [], False
    for n, (_, token, in_class, _) in enumerate(tokens):
        if in_class and token == "\\S":
            if out and out[-1] == "-":
                out[-1] = "\\-"
            out.append(NON_WHITE_SPACE_AS_NET_READS)
            changed = True
            if n + 2 < len(tokens) and tokens[n + 1][1] == "-" and tokens[n + 2][1] != "]":
                tokens[n + 1] = (tokens[n + 1][0], "\\-", True, False)
            continue
        out.append(token)
    return _joined(out) if changed else None


def non_white_space_in_class(d, context) -> bool:
    """Row M21: "Without `u` or `v`, without `s` and with no modifier group in the pattern, `\\S` in a class ...; an
    `exec` both sides answer, where Broiler.JS answers otherwise and the arbiter's answer to the pattern with each such
    `\\S` read as .NET reads it (the reading in the Broiler.JS cell) is Broiler.JS's, up to row M10's names, and not
    the text's"."""
    if unicode_mode(d) or not both_answer_exec(d) or has_flag(d, "s"):
        return False
    text = unit_text(d["pattern"])
    if modifier_group_lexed(text):
        return False
    return answered_as_read(d, context, non_white_space_reading(text))


EMPTY_GROUP_LAZY = re.compile(r"\((?:\?<[^>=!]+>)?\)(?:\+\?|\{1,\}\?)")


def empty_group_lazy(d, context) -> bool:
    """Row M22: "Without `u` or `v`, a capturing group whose body is empty, `()` or `(?<name>)`, quantified by `+?` or
    `{1,}?`, in a pattern with more than one alternative ...; an `exec` both sides answer, where Broiler.JS answers
    otherwise": the group outside a class, and a `|` outside a class and outside every group."""
    if unicode_mode(d) or not both_answer_exec(d):
        return False
    text = unit_text(d["pattern"])
    depth, alternatives, group = 0, False, False
    for i, token, in_class, _ in text_tokens(text):
        if in_class:
            continue
        if token == "(":
            group = group or EMPTY_GROUP_LAZY.match(text, i) is not None
            depth += 1
        elif token == ")":
            depth = max(0, depth - 1)
        elif token == "|" and depth == 0:
            alternatives = True
    return group and alternatives


def anchor_member_reading(text: str) -> str | None:
    """Row M23's reading: "in a class, each such `^` written as the characters `(?<=^|\\r)` and each `$` as
    `(?=$|\\r)`", a `^` that is a member (not the class's negation); None where there is none."""
    out, changed = [], False
    for _, token, in_class, _ in text_tokens(text):
        if in_class and token == "^":
            out.append("(?<=^|\\r)")
            changed = True
        elif in_class and token == "$":
            out.append("(?=$|\\r)")
            changed = True
        else:
            out.append(token)
    return _joined(out) if changed else None


def anchor_member_under_m(d, context) -> bool:
    """Row M23: "Under `m`, without `u` or `v`, `^` as a member of a class anywhere but right after the class's opening
    `[` ..., or `$` as a member of a class; an `exec` both sides answer, where Broiler.JS answers otherwise and the
    arbiter's answer to the pattern with each such member read as the translator writes it (the reading in the
    Broiler.JS cell) is Broiler.JS's, up to row M10's names, and not the text's". `m` is the flag."""
    if unicode_mode(d) or not both_answer_exec(d) or not has_flag(d, "m"):
        return False
    return answered_as_read(d, context, anchor_member_reading(unit_text(d["pattern"])))


def backreference_without_ecmascript(d, context) -> bool:
    """Row M24: "Without `u` or `v`, under `s` or with a modifier group in the pattern, a back-reference to a group of
    the pattern, `\\N` with N no more than the pattern's capturing groups or `\\k<name>` in a pattern with a named group
    ...; an `exec` both sides answer, where Broiler.JS answers `null` or another match". The back-reference is one the
    arbiter's tree holds (its parse reads `\\N` as one only where N is no more than the groups, and `\\k<name>` only in
    a pattern with a named group)."""
    if unicode_mode(d) or not both_answer_exec(d):
        return False
    text = unit_text(d["pattern"])
    if not (has_flag(d, "s") or modifier_group_lexed(text)):
        return False
    tree = context.parse(text)
    return tree is not None and any(type(n).__name__ == "Backreference" for n in _nodes(tree))


REMOVES_I = re.compile(r"\(\?[ims]*-([ims]*):")


def expanded_letter_reading(text: str, canonicalize_class) -> str | None:
    """Row M25's reading: "inside such a group, each such letter written as the class of its Canonicalize class", a
    letter outside a class inside a modifier group that removes `i`, whose Canonicalize class (the arbiter's, without
    `u` or `v`) holds another member and a non-ASCII one; None where there is none."""
    out, stack, changed = [], [], False
    for i, token, in_class, _ in text_tokens(text):
        if not in_class and token == "(":
            m = REMOVES_I.match(text, i)
            stack.append(bool(m and "i" in m.group(1)))
        elif not in_class and token == ")" and stack:
            stack.pop()
        elif not in_class and any(stack) and len(token) == 1 and token.isalpha():
            members = tuple(canonicalize_class(ord(token)))
            if len(members) > 1 and any(m > 0x7F for m in members):
                out.append("[" + "".join(chr(m) for m in members) + "]")
                changed = True
                continue
        out.append(token)
    return _joined(out) if changed else None


def letter_expanded_without_i(d, context) -> bool:
    """Row M25: "Under `i`, without `u` or `v`, inside a modifier group that removes `i` (`(?-i:…)`, `(?m-is:…)`), a
    letter the translator expands, one whose Canonicalize class holds another member and a non-ASCII one, as `Σ` in
    `(?m-is:\\u03a3)`; an `exec` both sides answer, where Broiler.JS answers otherwise and the arbiter's answer to the
    pattern with each such letter read as the translator writes it (the reading in the Broiler.JS cell) is Broiler.JS's,
    up to row M10's names, and not the text's". `i` is the flag; the letter is one written as itself."""
    if unicode_mode(d) or not both_answer_exec(d) or not has_flag(d, "i"):
        return False
    return answered_as_read(d, context, expanded_letter_reading(unit_text(d["pattern"]), context.canonicalize_class))


def class_folded_by_net(d, context) -> bool:
    """Row M26: "Under `i`, without `u` or `v`, a class, lexed as the text lexes it without `v`, that holds `\\W`,
    `\\p{…}` or `\\P{…}` ...; an `exec` both sides answer, where Broiler.JS answers otherwise; rows M12 and M13 come
    first where their readings give Broiler.JS's answer". `i` is the flag."""
    if unicode_mode(d) or not both_answer_exec(d) or not has_flag(d, "i"):
        return False
    text = unit_text(d["pattern"])
    return any(in_class and (token == "\\W" or token in ("\\p", "\\P") and text[i + 2:i + 3] == "{")
               for i, token, in_class, _ in text_tokens(text))


# --------------------------------------------------------------------------------------------------
# The rule's revision of 2026-11-16c (docs/evidence/sjl-11-001/reference-arms-revised-2026-11-16c.md, the owner's
# ruling of that day): an input the arbiter cannot reach, named by a row, classed by that row (@8).

def whole_run_answer(pattern: str, flags: str, subject: list[int], last_index: str):
    """Condition (3)'s stated check: the text's answer to `^X*$`, X one ASCII letter or digit or a class of them
    (`[a]`, no negation, range or escape), under flags without `i`, `m`, `g` and `y` (so X is read as written, `^` and
    `$` are the subject's ends and the exec starts at 0), over a subject every code unit of which X holds: a match of
    the whole subject and no capture. `^` holds at 0; the greedy `*` (22.2.2.3, RepeatMatcher) first takes every unit,
    X holding each; `$` (22.2.2.6) holds at the end, so the first path in the backtracking order succeeds. None for any
    other pattern, flags or subject: no answer is claimed."""
    m = re.fullmatch(r"\^(?:([0-9A-Za-z])|\[([0-9A-Za-z]+)\])\*\$", pattern)
    if m is None or set(flags) & set("imgy") or last_index != "0":
        return None
    members = {ord(c) for c in (m.group(1) or m.group(2))}
    if not set(subject) <= members:
        return None
    return ("match", ((0, len(subject)),), ())


B1_INPUT = ("^[a]*$", "u", 0x61, 10_000_001, "0")


def names_b1_input(d) -> bool:
    """Row B1's widening of 2026-11-16 (decision D-jsexec): "Broiler.Regex's reference arms ... reached `/^[a]*$/u` over
    10,000,001 `a` (`P3-O06`), which Broiler.JS answers `null` in every culture where S answers the whole input ...: the
    class's run ends at the budget's early exit this row cites (R1), so `$` fails at every start (an inference from this
    row's `/[a]*/u` and `/^a*$/u` answers, not traced in preview.4)". The row names that one input, by its pattern, flags
    and subject, and the predicate reads it alone; the row's words are checked by check_beyond_rows."""
    pattern, flags, unit, length, last_index = B1_INPUT
    if d["op"] != "exec" or units_text(d["pattern"]) != pattern or d["flags"] != flags or d["lastIndex"] != last_index:
        return False
    subject = decode_units(d["subject"])
    return len(subject) == length and set(subject) == {unit}


# The rows an input the arbiter cannot reach is classed by (condition (1)): (row, the engine whose correction the row
# records, the predicate that reads the input the row names, the words of the row's class that name that engine as
# corrected, and the row's own words that name the input and the mechanism, which check_beyond_rows reads).
BEYOND_ROWS = [
    ("B1", "J", names_b1_input, "Broiler.JS: **correction of Broiler.JS and of Broiler.Regex's preview.4**",
     ("`/^[a]*$/u` over 10,000,001 `a` (`P3-O06`)", "the class's run ends at the budget's early exit this row cites")),
]


def beyond_by_row(d, reduced_id: str, arbiter_answer, s_answer: str, context) -> tuple[str, str, str] | None:
    """The revision of 2026-11-16c's clause, for an input the arbiter cannot reach whose reduced case shows no
    difference (the arm answers it as S does) and at which neither side stopped at a bound it declares: class (a)
    "beyond the arbiter, by row", under the row, where S's answer at the reduced case is the arbiter's and
    (1) a row of BEYOND_ROWS for the arm's engine names the input and the mechanism;
    (2) every other arm that implements the operation answers the input as S does: each other engine's run of the
        population holds the case and answers S's answer (Context.other_answers), and at least one does;
    (3) S's answer at the input is the text's by evidence other than the arbiter: whole_run_answer gives it at the
        input, and gives the reduced case's agreed answer at the reduced subject.
    None otherwise: the input stays class (c)."""
    if canonical(harness_read(s_answer)) != arbiter_answer:
        return None
    engine = d["run"].split(".", 1)[0]
    row = next((r for r, e, names, _, _ in BEYOND_ROWS if e == engine and names(d)), None)
    if row is None:
        return None
    subject, flags = decode_units(d["subject"]), "" if d["flags"] == "." else d["flags"]
    pattern = units_text(d["pattern"])
    prefix = int(reduced_id.rsplit("~p", 1)[1])
    at_input = whole_run_answer(pattern, flags, subject, d["lastIndex"])
    if at_input is None or canonical(d["S"]) != at_input:
        return None
    if whole_run_answer(pattern, flags, subject[:prefix], d["lastIndex"]) != arbiter_answer:
        return None
    others = context.other_answers(d)
    if not others or any(answer is None or harness_read(answer) != d["S"] for answer in others.values()):
        return None
    agreeing = ", ".join(f"run {run} {answer}" for run, answer in sorted(others.items()))
    return ("a", row, f"beyond the arbiter, by row {row} (reference-arms-revised-2026-11-16c.md): at the prefix "
            f"{reduced_id} S, the arm and the arbiter answer {s_answer}; the row names the input and the mechanism; "
            f"every other arm answers the input as S does ({agreeing}); S's answer {d['S']} is the whole subject, as "
            "the pattern requires over a subject its class holds")


def other_arm_answers(runs: Path, d) -> dict[str, str | None]:
    """Condition (2)'s answers: each run of an engine other than S and the difference's arm, in the runs directory's
    answer files for the difference's population (`<population>.<run>.tsv.gz`), with its answer to the case (None
    where the run does not hold it)."""
    engine = d["run"].split(".", 1)[0]
    out: dict[str, str | None] = {}
    for path in sorted(runs.glob(f"{d['class']}.*.tsv.gz")):
        run = path.name[len(d["class"]) + 1:-len(".tsv.gz")]
        if run.split(".", 1)[0] in ("S", engine):
            continue
        out[run] = None
        with open_text(path) as handle:
            for line in handle:
                if line.startswith(d["id"] + "\t"):
                    out[run] = line.rstrip("\n").split("\t")[1]
                    break
    return out


ROWS = [
    ("S1", "V", lambda d, c: arm_refuses_what_s_accepts(d) and duplicate_names(d), "Correction of Broiler.VM"),
    ("S2a", "J", lambda d, c: arm_accepts_what_s_refuses(d) and duplicate_only_when_decoded(d)
     and c.refusal_code(d) == "early:duplicate-group-name", "Correction of Broiler.JS"),
    ("S5", "J", lambda d, c: class_k_refused(d), "Broiler.JS: **correction of Broiler.JS**, by D-s5"),
    ("S7", "J", s7, "Correction of Broiler.JS"),
    ("S8", "J", s8, "Correction of Broiler.JS"),
    ("S10", "V", lambda d, c: arm_refuses_what_s_accepts(d) and lead_before_braced(d), "Correction of Broiler.VM"),
    ("S10", "J", lambda d, c: both_answer_exec(d) and lead_before_braced_trail_in_class(d),
     "The class case `[\\uD83D\\u{DC38}]`: **correction of Broiler.JS**"),
    ("M2a", "J", culture_casing, "Correction of Broiler.JS"),
    ("M2b", "J", iota_subscript_pair, "Correction of Broiler.JS"),
    ("M2", "J", net_case_pair, "Correction of Broiler.JS"),
    ("M3", "J", lambda d, c: line_terminator(d), "Correction of Broiler.JS"),
    ("M4", "V", lambda d, c: vm_steps_back(d), "correction of Broiler.VM"),
    ("M4", "J", lambda d, c: half_pair_match(d),
     "The half-pair matches (`/\\udf06/gu`, `/yu`, the look-behind): **correction of Broiler.JS**"),
    ("M7", "V", lambda d, c: both_answer_exec(d) and kind(d["arm"]) == "match" and backreference_across_pair(d),
     "Correction of Broiler.VM"),
    ("M7", "J", lambda d, c: both_answer_exec(d) and kind(d["arm"]) == "match" and backreference_in_look_behind(d),
     "also a correction of Broiler.Regex's preview.4, and so of Broiler.JS"),
    ("B2", "J", lambda d, c: budget_reset_per_start(d), "Correction of Broiler.JS and of Broiler.Regex's preview.4"),
    ("B8", "J", recursion_per_term, "Correction of Broiler.JS and of Broiler.Regex's preview.4"),
    ("F3", "J", lambda d, c: invalid_modifier_accepted(d),
     "`(?x:a)`, accepted by Broiler.JS's constructor, a **correction of Broiler.JS**"),
    # Added 2026-11-16 (@4), after @3's rows so that every difference @3 classed keeps its row.
    ("M4", "J", lambda d, c: half_pair_capture_or_null(d),
     "The half-pair matches (`/\\udf06/gu`, `/yu`, the look-behind): **correction of Broiler.JS**"),
    ("S8a", "V", lambda d, c: word_difference_keeps_folding(d), "Correction of Broiler.VM"),
    ("S12", "J", grammar_refusal_accepted, "Correction of Broiler.JS"),
    ("S13", "J", early_error_accepted, "Correction of Broiler.JS"),
    ("S15", "J", bound_refused_under_i, "Correction of Broiler.JS"),
    ("S14", "J", annex_b_refused, "Correction of Broiler.JS"),
    ("M8", "J", exec_answered_otherwise, "Correction of Broiler.JS"),
    ("M9", "J", lambda d, c: throws_out_of_range(d), "Correction of Broiler.JS"),
    # Added 2026-11-16 (@7), after @6's rows so that every difference @6 classed keeps its class and row, in the order
    # the correspondence states ("Rows M10 to M15 are read in that order, so an input two of them take is the first's").
    ("M10", "J", group_name_rewritten, "Correction of Broiler.JS"),
    ("M11", "J", empty_iteration_kept, "Correction of Broiler.JS"),
    ("M12", "J", class_escape_read_by_net, "Correction of Broiler.JS"),
    ("M13", "J", class_subtraction, "Correction of Broiler.JS"),
    ("M14", "J", escaped_dash_starts_no_range, "Correction of Broiler.JS"),
    ("M15", "J", culture_casing_of_i, "Correction of Broiler.JS"),
    # Added 2026-11-16 (@8), after M15 in the order the correspondence states ("Rows M16 to M26 are read after M15, in
    # order, so an input two of them take is the first's"); rows M8, M12, M13 and M15 read their notes in place.
    ("M16", "J", control_escape_read_as_backslash, "Correction of Broiler.JS and of Broiler.Regex's preview.4"),
    ("M17", "J", braced_name_in_nullable_repeat, "Correction of Broiler.JS and of Broiler.Regex's preview.4"),
    ("M18", "J", braced_name_reference, "Correction of Broiler.JS and of Broiler.Regex's preview.4"),
    ("M19", "J", octal_in_nullable_repeat, "Correction of Broiler.JS and of Broiler.Regex's preview.4"),
    ("M20", "J", octal_before_digit_class, "Correction of Broiler.JS"),
    ("M21", "J", non_white_space_in_class, "Correction of Broiler.JS"),
    ("M22", "J", empty_group_lazy, "Correction of Broiler.JS"),
    ("M23", "J", anchor_member_under_m, "Correction of Broiler.JS"),
    ("M24", "J", backreference_without_ecmascript, "Correction of Broiler.JS"),
    ("M25", "J", letter_expanded_without_i, "Correction of Broiler.JS"),
    ("M26", "J", class_folded_by_net, "Correction of Broiler.JS"),
]

# The rows that read a stop: they are read where one side stopped and the other answered, before the policy's (b).
STOP_ROWS = frozenset({"B8"})

POLICY_ROWS = {"V": ("S3", "B6", "B7"), "J": ("S3",)}
ENGINE_WORDS = {"V": "Broiler.VM", "J": "Broiler.JS"}


class Context:
    """What a predicate reads beyond the difference: the arbiter's reason for a refusal, row M2's case pairs, arm J's
    answers under the invariant culture (row M2a), J's refusals in the comparison (rows S14 and S15), the reduction, and
    the letters of the arbiter's UCD (row M8); since @7, the arbiter's answer to a reading of the pattern (rows M11 to
    M14), the BMP code units of a General_Category in the arbiter's UCD (row M12), the inputs where run J, under the
    invariant culture, differs from S (row M15), and a code unit's Canonicalize class and whether .NET's invariant
    lowercase folds it into another (row M10); since @8, run J's answer where it differs (row M15's note), the
    arbiter's tree of a pattern (rows M16, M17, M19 and M24), and the other arms' answers to a case (the revision of
    2026-11-16c's condition (2))."""

    def __init__(self, refusal_code=None, case_pairs=frozenset(), invariant=None, j_refused=frozenset(),
                 reduction=None, letters=frozenset(), reading=None, category_units=None,
                 j_differences=frozenset(), canonicalize_class=None, over_folds=None, j_answers=None, parse=None,
                 other_answers=None) -> None:
        # The id of every difference of run J -> arm J's answer under the invariant culture (row M15's note, @8).
        self.j_answers = j_answers or {}
        # A pattern without u or v, one character per code unit -> the arbiter's tree, or None (@8).
        self._parse = parse
        # A difference -> {run: its answer to the case, or None} for each run of another engine (@8).
        self._other_answers = other_answers
        self._refusal_code = refusal_code
        self.case_pairs = case_pairs
        # The code points past ASCII whose General_Category is a letter (L) in the arbiter's UCD (row M8).
        self.letters = letters
        self.invariant = invariant or {}
        # (run, pattern, flags) of every acceptance difference where S accepts and arm J refuses (rows S14 and S15).
        self.j_refused = j_refused
        # The reduction's cases and the arbiter at their prefixes (Reduction), or None: the rule as filed.
        self.reduction = reduction
        # (d, the reading's text, one character per code unit) -> the arbiter's answer to the reading under the
        # difference's flags, subject and lastIndex (rows M11 to M14).
        self._reading = reading
        # A General_Category value -> the BMP code units of that category (row M12).
        self._category_units = category_units
        # The ids of every difference of run J, arm J under the invariant culture (row M15).
        self.j_differences = j_differences
        # A BMP code unit -> its Canonicalize class without u or v, ascending (row M10); and whether .NET's invariant
        # lowercase folds it into another Canonicalize class (row M10's second path). Without them no letter of a name
        # reads as rewritten.
        self._canonicalize_class = canonicalize_class
        self._over_folds = over_folds

    def refusal_code(self, d):
        return self._refusal_code(d) if self._refusal_code else None

    def reading(self, d, text: str):
        """The arbiter's answer to the pattern as a row reads it; no answer (None) where none was given."""
        return self._reading(d, text) if self._reading else None

    def category_units(self, name: str) -> list[int]:
        return self._category_units(name) if self._category_units else []

    def canonicalize_class(self, unit: int) -> tuple[int, ...]:
        return tuple(self._canonicalize_class(unit)) if self._canonicalize_class else (unit,)

    def over_folds(self, unit: int) -> bool:
        return bool(self._over_folds(unit)) if self._over_folds else False

    def invariant_differs(self, d) -> bool:
        """Whether arm J under the invariant culture answers the same input otherwise than S: a difference of run J in
        the comparison, or for a reduced case `~p` in the reduction's comparison."""
        if "~p" in d["id"] and self.reduction is not None:
            return (d["id"], "J") in self.reduction.differences
        return d["id"] in self.j_differences

    def invariant_answer(self, d) -> str | None:
        """Arm J's answer under the invariant culture where it differs from S's (row M15's note, @8), from the same
        comparison as invariant_differs; None where none is held."""
        if "~p" in d["id"] and self.reduction is not None:
            reduced = self.reduction.differences.get((d["id"], "J"))
            return None if reduced is None else harness_read(reduced["arm"])
        answer = self.j_answers.get(d["id"])
        return None if answer is None else harness_read(answer)

    def parse(self, text: str):
        return self._parse(text) if self._parse else None

    def other_answers(self, d) -> dict:
        return self._other_answers(d) if self._other_answers else {}


# The bounds option R names, by the answer each writes: "S's step, frame or trail ceiling, arm V's allowance, or for
# arm J the harness's stop the stop list holds" (a run's stop of arm J is the stop list's: no case reached a backstop).
DECLARED_STOPS = {"S": frozenset({"limit:steps", "limit:frames", "limit:trail"}), "V": frozenset({"limit:steps"}),
                  "J": frozenset({HARNESS_STOP})}


class Reduction:
    """reduce.py's output for a comparison, as the rule's revision of 2026-11-16 (option R) reads it: each input's
    prefix (reductions.tsv; None where the arbiter reaches none), the reduced cases' differences (DIR/comparison), S's
    answers to them (DIR/runs/*.S.tsv.gz), and the arbiter's answer at each prefix as reductions.tsv records it
    (reduce.py asked this arbiter at the same cut-off, which the reduction's manifest names)."""

    RULE = "reference-arms-revised-2026-11-16.md (option R)"

    def __init__(self, prefixes: dict, differences: dict, s_answers: dict, identity=None) -> None:
        self.prefixes = prefixes
        self.differences = differences
        self.s_answers = s_answers
        self.identity = identity or {}

    @classmethod
    def read(cls, directory: Path, cutoff: int) -> "Reduction":
        def digest(path: Path) -> str:
            return hashlib.sha256(path.read_bytes()).hexdigest()

        prefixes, differences, s_answers = {}, {}, {}
        for r in _rows(directory / "reductions.tsv"):
            prefixes[r["id"]] = None if r["prefix"] == "none" else (int(r["prefix"]), parse_rendered(r["arbiter"]))
        comparison = directory / "comparison" / "differences.tsv.gz"
        for r in _rows(comparison):
            differences[(r["id"], r["run"])] = r
        runs = sorted((directory / "runs").glob("*.S.tsv.gz"))
        for path in runs:
            with open_text(path) as handle:
                for line in handle:
                    if not line.startswith("#"):
                        case_id, answer, _ = line.rstrip("\n").split("\t")
                        s_answers[case_id] = answer
        manifest = json.loads((directory / "population" / "manifest.json").read_text(encoding="utf-8"))
        if manifest.get("cutoff") != cutoff:
            raise ValueError(f"the reduction's cut-off {manifest.get('cutoff')} is not the classifier's {cutoff}")
        identity = {"rule": cls.RULE, "reducer": manifest.get("reducer"),
                    "reductions.tsv": digest(directory / "reductions.tsv"),
                    "comparison/differences.tsv.gz": digest(comparison),
                    "runs": {p.name: digest(p) for p in runs}}
        return cls(prefixes, differences, s_answers, identity)

    def classify(self, d, partners, context) -> tuple[str, str, str] | None:
        """The class of a difference the arbiter cannot reach, by its reduced case; None when the input was not
        reduced."""
        if d.get("id") not in self.prefixes:
            return None
        found = self.prefixes[d["id"]]
        if found is None:
            return ("c", "-", "the arbiter reaches no prefix of the subject, not even the shortest (the reduction): "
                    "what it cannot reach is in the pattern")
        prefix, arb = found
        rid = f"{d['id']}~p{prefix}"
        if isinstance(arb, str) and arb.startswith("arbiter:"):
            return "c", "-", f"reduced to {rid}, which the arbiter does not reach ({arb})"
        reduced = self.differences.get((rid, d["run"]))
        if reduced is not None:
            category, row, why = classify(reduced, arb, partners, context)
            return category, row, f"classed as its reduced difference, {rid}: {why}"
        s_answer = self.s_answers.get(rid)
        if s_answer is None:
            return "c", "-", f"reduced to {rid}, whose answers the reduction's runs do not hold"
        engine = d["run"].split(".", 1)[0]
        sides = [side for side, answer in (("S", d["S"]), (f"arm {engine}", d["arm"]))
                 if answer in DECLARED_STOPS["S" if side == "S" else engine]]
        if sides and canonical(harness_read(s_answer)) == arb:
            return ("b'", "-", f"beyond the arbiter: at the prefix {rid} S and the arm both answer {s_answer}, the "
                    f"arbiter's answer; at the input {sides[0]} stopped at a bound it declares")
        if not sides:
            # The revision of 2026-11-16c (@8): such an input is classed by the row that names it, where the
            # revision's three conditions hold (beyond_by_row); otherwise it stays class (c), as option R reads it.
            beyond = beyond_by_row(d, rid, arb, s_answer, context)
            if beyond is not None:
                return beyond
            return ("c", "-", f"reduced to {rid}, where S and the arm agree ({s_answer}), but at the input neither "
                    "side stopped at a bound it declares")
        return "c", "-", f"reduced to {rid}, where S and the arm agree ({s_answer}) and the arbiter does not"


class Exclusion:
    """The owner's ruling of 2026-11-16 on population P4 (reference-arms-revised-2026-11-16b.md), as exclusion.py's
    listing names its patterns: a difference of a listed pattern is excluded, listed apart, and judged in no class."""

    RULE = "reference-arms-revised-2026-11-16b.md (the owner's ruling of 2026-11-16)"
    WHY = "excluded from P4 by the owner's ruling of 2026-11-16"

    def __init__(self, patterns: dict[str, str], identity=None) -> None:
        self.patterns = patterns
        self.identity = identity or {}

    @classmethod
    def read(cls, path: Path) -> "Exclusion":
        from exclusion import read_listing  # noqa: PLC0415
        patterns = read_listing(path)
        data = path.read_bytes()
        return cls(patterns, {"rule": cls.RULE, "name": path.name, "sha256": hashlib.sha256(data).hexdigest(),
                              "patterns": len(patterns)})

    def excludes(self, d) -> str | None:
        """Why the difference is excluded, or None. A listed id with another pattern refuses (ExclusionMismatch)."""
        if d["class"] != "P4":
            return None
        from exclusion import pattern_id  # noqa: PLC0415
        pid = pattern_id(d["id"])
        listed = self.patterns.get(pid)
        if listed is None:
            return None
        if listed != d["pattern"]:
            raise ExclusionMismatch(f"{d['id']}: the exclusion lists {pid} with another pattern")
        return f"{self.WHY}: pattern {pid}, whose arbiter cost is in the pattern"


class ExclusionMismatch(Exception):
    """The exclusion's listing and the comparison name one pattern id for two patterns: another population."""


def _rows(path: Path):
    header = None
    with open_text(path) as handle:
        for line in handle:
            if line.startswith("#"):
                header = line[2:].rstrip("\n").split("\t")
                continue
            yield dict(zip(header, line.rstrip("\n").split("\t")))


def correspondence_identity(path: Path) -> dict[str, str]:
    """The correspondence as classification.json names it: its file name, its git blob and its SHA-256, which the
    bytes alone decide, never the path it was read from, so that the same inputs give the same classification.json
    wherever they are and whatever the working directory. The blob is the one Broiler.VM's history holds the file by
    (`git log --find-object`). *(Revised 2026-11-02, before any judged run: it read `{"file":
    str(args.correspondence), "sha256": ...}`, the path as given, absolute or relative to the working directory.)*"""
    data = path.read_bytes()
    blob = hashlib.sha1(b"blob %d\0" % len(data) + data).hexdigest()
    return {"name": path.name, "blob": blob, "sha256": hashlib.sha256(data).hexdigest()}


def read_correspondence(path: Path) -> dict[str, str]:
    rows = {}
    for line in path.read_text(encoding="utf-8").split("\n"):
        m = re.match(r"\| ([SMBFTU]\d+[a-z]?) \|", line)
        if m:
            rows.setdefault(m.group(1), line.rstrip().rstrip("|").rsplit("|", 1)[-1])
    return rows


def check_rows(rows: dict[str, str]) -> list[str]:
    """Every row the classifier names is in the correspondence, and its class holds the words ROWS gives, which name
    the arm's engine as corrected."""
    problems = []
    for row, engine, _, words in ROWS:
        cls = rows.get(row)
        if cls is None:
            problems.append(f"row {row} is not in the correspondence")
        elif ENGINE_WORDS[engine] not in words or words not in cls:
            problems.append(f"row {row}'s class does not name {ENGINE_WORDS[engine]} as corrected in the words "
                            f"{words!r}: {cls.strip()[:120]}")
    for engine, policy in POLICY_ROWS.items():
        for row in policy:
            if row not in rows:
                problems.append(f"policy row {row} is not in the correspondence")
    if "U3" not in rows or "data decision" not in rows["U3"]:
        problems.append("row U3 is not the correspondence's data decision")
    for row, engine, _, words, _ in BEYOND_ROWS:
        cls = rows.get(row)
        if cls is None:
            problems.append(f"row {row} is not in the correspondence")
        elif ENGINE_WORDS[engine] not in words or words not in cls:
            problems.append(f"row {row}'s class does not name {ENGINE_WORDS[engine]} as corrected in the words "
                            f"{words!r}: {cls.strip()[:120]}")
    return problems


def read_row_lines(path: Path) -> dict[str, str]:
    """Each row of the correspondence with its whole line (@8: check_beyond_rows reads a row's own words)."""
    lines = {}
    for line in path.read_text(encoding="utf-8").split("\n"):
        m = re.match(r"\| ([SMBFTU]\d+[a-z]?) \|", line)
        if m:
            lines.setdefault(m.group(1), line)
    return lines


def check_beyond_rows(lines: dict[str, str]) -> list[str]:
    """The revision of 2026-11-16c's condition (1), read in the correspondence: each row of BEYOND_ROWS holds the
    words that name its input and the mechanism by which the arm's answer at the input differs from its answer at the
    prefix."""
    problems = []
    for row, _, _, _, named in BEYOND_ROWS:
        line = lines.get(row, "")
        for words in named:
            if words not in line:
                problems.append(f"row {row} does not name its input or mechanism in the words {words!r}")
    return problems


# --------------------------------------------------------------------------------------------------

def stopped(answer: str) -> bool:
    return kind(harness_read(answer)) == "limit" or answer == "refuse:nesting"


def both_stopped(d) -> bool:
    return stopped(d["S"]) and stopped(d["arm"]) and harness_read(d["S"]) != HARNESS_STOP


def classify(d, arbiter_answer, data_partners: set[int], context: Context | None = None) -> tuple[str, str, str]:
    context = context or Context()
    d = dict(d, S=harness_read(d["S"]), arm=harness_read(d["arm"]))
    engine = d["run"].split(".", 1)[0]
    s, arm = d["S"], d["arm"]
    # The harness's stop of S or of arm V stands for no bound either declares: no answer, and no policy's.
    if s == HARNESS_STOP:
        return "c", "-", "S stopped by the harness (a time limit or its worker's end), not by a policy it declares"
    if engine == "V" and arm == HARNESS_STOP:
        return "c", "-", "arm V stopped by the harness (a time limit or its worker's end), which no row names"
    s_c, arm_c = canonical(s), canonical(arm)
    arb = arbiter_answer
    unreachable = isinstance(arb, str) and arb.startswith("arbiter:")
    # D41: the empty match from a lastIndex inside a pair, which the text leaves without an answer.
    if inside_pair(d) and isinstance(arb, str) and arb.startswith("arbiter:no-answer"):
        empty = all(isinstance(a, tuple) and a[1][0] is not None and a[1][0][0] >= a[1][0][1] or a == "null"
                    for a in (s_c, arm_c))
        if empty:
            return "b", "M4", "the empty match from a lastIndex inside a surrogate pair (decision D41)"
    # The policies: a refusal for nesting or a stop at a limit on one side.
    s_policy, arm_policy = stopped(s), stopped(arm)
    if s_policy and arm_policy:
        # Both stopped, by different bounds: each side's policy, and neither gives an answer to check.
        row = "S3" if "nesting" in (s + arm) else ("B6" if engine == "V" and "ceiling" in arm else "B7" if engine == "V" else "D10")
        return "b", row, "both sides stopped by their own policies, which the specification leaves to the implementation"
    if s_policy != arm_policy:
        # One side stopped and the other answered: class (b) only when that answer is the arbiter's. An arbiter past
        # its cut-off has checked nothing, so the difference is class (c) until reduced to an input it can reach (the
        # rule's arbiter clause), below.
        other = arm_c if s_policy else s_c
        if other == arb and arm_policy:
            for row, row_engine, predicate, _ in ROWS:
                if row in STOP_ROWS and row_engine == engine and predicate(d, context):
                    return "a", row, f"a correction of {ENGINE_WORDS[engine]}"
        if other == arb:
            row = "S3" if "nesting" in (s + arm) else ("B6" if engine == "V" and "ceiling" in arm else
                                                      "B7" if engine == "V" else "D10")
            if engine == "V" and row not in POLICY_ROWS["V"]:
                return "c", row, "arm V stopped by a policy no row of the correspondence names"
            return "b", row, "an outcome kind that follows from a policy the specification leaves to the implementation"
    if unreachable:
        if context.reduction is not None:
            reduced = context.reduction.classify(d, data_partners, context)
            if reduced is not None:
                return reduced
        return "c", "-", f"the arbiter gives no answer ({arb}): reduce the input"
    if s_c != arb:
        return "c", "-", "S's answer is not the arbiter's"
    # S is right and the arm is wrong: a row must name it.
    if engine == "J" and both_answer_exec(d) and case_insensitive_without_u(d):
        if input_code_points(d) & data_partners:
            return "a'", "U3", "arm J answers from .NET's case data, S from UCD 17.0.0 (SJL-D-6 (c))"
    for row, row_engine, predicate, _ in ROWS:
        if row not in STOP_ROWS and row_engine == engine and predicate(d, context):
            return "a", row, f"a correction of {ENGINE_WORDS[engine]}"
    return "c", "-", f"arm {engine} is wrong by the arbiter and no row of the correspondence names the input"


def data_partners(pins: Path) -> set[int]:
    """The code points row U3's data decision reaches: the cased letters Unicode 16.0 and 17.0 added (U+A7CB to
    U+A7DC among them, row U3's U+A7CE and U+A7CF), whose case pairs the .NET runtime and the ICU 74 arm J reads
    (Unicode 15.1) do not hold and UCD 17.0.0 does. Declared ranges, not read from the pins (`pins` is unused): the
    classifier names them before any run."""
    _ = pins
    ranges = [(0xA7CB, 0xA7DC), (0x1C89, 0x1C8A), (0x10D50, 0x10D85), (0x16EA0, 0x16EDF), (0x10EC2, 0x10EC4)]
    return {c for lo, hi in ranges for c in range(lo, hi + 1)}


def control_cases(runs: Path) -> dict[str, str]:
    out = {}
    for path in sorted(runs.glob("run.*.json")):
        facts = json.loads(path.read_text(encoding="utf-8"))
        if facts.get("control-case"):
            run = path.name[len("run."):-len(".json")]
            out[run] = facts["control-case"]
    return out


def j_refusals(path: Path) -> frozenset:
    """(run, pattern, flags) of every acceptance difference where S accepts and arm J refuses (rows S14 and S15 read
    J's answer to the same pattern under other flags)."""
    out = set()
    for d in _rows(path):
        if d["op"] == "accept" and d["run"].split(".", 1)[0] == "J" and d["S"] == "accept" and refuses(d["arm"]):
            out.add((d["run"], d["pattern"], d["flags"]))
    return frozenset(out)


def j_differences(path: Path) -> frozenset:
    """The ids of every difference of run J, arm J under the invariant culture (row M15 reads an input where that run
    answers as S does: one not listed)."""
    return frozenset(d["id"] for d in _rows(path) if d["run"] == "J")


def j_answers(path: Path) -> dict[str, str]:
    """Each difference of run J (arm J under the invariant culture) under `i` without `u` or `v`, the inputs row M15
    reads, with arm J's answer (row M15's note, @8)."""
    return {d["id"]: d["arm"] for d in _rows(path) if d["run"] == "J" and has_flag(d, "i") and not unicode_mode(d)}


def invariant_answers(path: Path) -> dict[str, str]:
    """Arm J's answers under the invariant culture where they differ from S's, for the cases whose input holds
    U+0130 or U+0131 (row M2a reads them); a case not listed is one where that run answered as S did."""
    out = {}
    header = None
    with open_text(path) as source:
        for line in source:
            if line.startswith("#"):
                header = line[2:].rstrip("\n").split("\t")
                continue
            d = dict(zip(header, line.rstrip("\n").split("\t")))
            if d["run"] == "J" and input_code_points(d) & DOTTED_AND_DOTLESS:
                out[d["id"]] = d["arm"]
    return out


def main(argv: list[str] | None = None) -> int:
    """The exit status: 0 no class (c), 1 a class (c) difference, 2 refused (or an argument argparse refuses, or an
    exclusion that lists a pattern id for another pattern), 3 failed. An excluded difference is no class (c)."""
    try:
        return run(argv)
    except SystemExit:
        raise
    except ExclusionMismatch as mismatch:
        print(f"refused: {mismatch}", file=sys.stderr)
        return 2
    except BaseException:  # noqa: BLE001 - every failure is exit 3, never the 1 of a class (c) difference
        traceback.print_exc()
        print("failed: the classification did not run to its end, and wrote no classification.json", file=sys.stderr)
        return 3


def run(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--comparison", required=True, type=Path)
    parser.add_argument("--runs", required=True, type=Path)
    parser.add_argument("--broiler-js", required=True, type=Path)
    parser.add_argument("--commit", required=True)
    parser.add_argument("--correspondence", required=True, type=Path)
    parser.add_argument("--unicode-pins", required=True, type=Path)
    parser.add_argument("--cutoff", type=int, default=DEFAULT_CUTOFF)
    parser.add_argument("--out", type=Path)
    parser.add_argument("--reduction", type=Path, help="reduce.py's directory for this comparison (reductions.tsv, "
                        "runs, comparison): the rule's revision of 2026-11-16, option R, classes the differences the "
                        "arbiter cannot reach by it")
    parser.add_argument("--cache", type=Path, help="a file of the arbiter's answers, read when it exists and written "
                        "after the run: an answer depends only on the input, the arbiter's commit and the cut-off")
    parser.add_argument("--exclude", type=Path, help="exclusion.py's listing of the P4 patterns the owner's ruling of "
                        "2026-11-16 excludes (scripts/reference-arms/p4-excluded.tsv): their differences are listed "
                        "apart, in no class")
    args = parser.parse_args(argv)
    out = args.out or args.comparison
    out.mkdir(parents=True, exist_ok=True)
    rows = read_correspondence(args.correspondence)
    problems = check_rows(rows) + check_beyond_rows(read_row_lines(args.correspondence))
    if problems:
        print("refused:\n  " + "\n  ".join(problems), file=sys.stderr)
        return 2
    arbiter = Arbiter(args.broiler_js, args.commit, args.unicode_pins, args.cutoff)
    if args.cache and args.cache.exists():
        arbiter.load(args.cache)
    controls = control_cases(args.runs)
    partners = data_partners(args.unicode_pins)
    reduction = None
    if args.reduction:
        reduction = Reduction.read(args.reduction, args.cutoff)
    exclusion = Exclusion.read(args.exclude) if args.exclude else None
    context = Context(lambda d: arbiter.refusal_code(d["pattern"], d["flags"]), frozenset(arbiter.case_pairs()),
                      invariant_answers(args.comparison / "differences.tsv.gz"),
                      j_refusals(args.comparison / "differences.tsv.gz"), reduction, arbiter.letters(),
                      arbiter.reading, arbiter.category_units, j_differences(args.comparison / "differences.tsv.gz"),
                      arbiter.canonicalize_class, arbiter.over_folds, j_answers(args.comparison / "differences.tsv.gz"),
                      arbiter.parse, lambda d: other_arm_answers(args.runs, d))
    counts: Counter = Counter()
    by_row: Counter = Counter()
    header = None
    with open_text(args.comparison / "differences.tsv.gz") as source, open_text(out / "classified.tsv.gz", "wt") as sink:
        sink.write("# id\tclass\top\trun\tS\tarm\tarbiter\tcategory\trow\twhy\n")
        for line in source:
            if line.startswith("#"):
                header = line[2:].rstrip("\n").split("\t")
                continue
            d = dict(zip(header, line.rstrip("\n").split("\t")))
            d["S"], d["arm"] = harness_read(d["S"]), harness_read(d["arm"])
            excluded = exclusion.excludes(d) if exclusion else None
            if controls.get(d["run"]) == d["id"]:
                category, row, why = "unclassified", "-", "the negative control's planted difference"
                arb = "-"
            elif excluded:
                # The owner's ruling: listed apart, judged in no class, and the arbiter, whose cost is in the pattern,
                # is not asked.
                category, row, why = "excluded", "-", excluded
                arb = "-"
            elif both_stopped(d):
                # Both sides stopped by their own bounds: the class does not read the arbiter, so it is not asked.
                arb = "-"
                category, row, why = classify(d, arb, partners, context)
            else:
                arb = arbiter.answer(d["pattern"], d["flags"], d["subject"], d["lastIndex"], d["op"])
                category, row, why = classify(d, arb, partners, context)
            counts[category] += 1
            by_row[f"{category}/{row}/{d['run']}/{d['class']}"] += 1
            sink.write("\t".join([d["id"], d["class"], d["op"], d["run"], d["S"], d["arm"], render(arb), category, row,
                                  why]) + "\n")
    result = {"classifier": CLASSIFIER_VERSION, "arbiter": {"version": arbiter.version, "commit": arbiter.commit,
                                                            "blobs": arbiter.blobs, "cutoff": args.cutoff},
              "correspondence": correspondence_identity(args.correspondence), "rows-read-from": CORRESPONDENCE,
              "reduction": reduction.identity if reduction else None,
              "exclusion": exclusion.identity if exclusion else None,
              "controls": controls, "counts": dict(sorted(counts.items())), "by-class-row-run-population": dict(sorted(by_row.items()))}
    (out / "classification.json").write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    if args.cache:
        arbiter.save(args.cache)
    print(json.dumps(result["counts"], sort_keys=True))
    return 1 if counts.get("c") else 0


def parse_rendered(text: str):
    """The arbiter's answer as render() writes it, read back: a match's captures and its names by their ranges."""
    if not text.startswith("match:"):
        return text
    body, _, names = text[6:].partition("|")
    captures = tuple(None if g == "-" else tuple(int(x) for x in g.split(",")) for g in body.split(";"))
    named = tuple(sorted((m.group(1), None if m.group(2) == "-" else tuple(int(x) for x in m.group(2).split(",")))
                         for m in re.finditer(r"([^=,]+)=(-|\d+,\d+)", names)))
    return "match", captures, named


def render(answer) -> str:
    if isinstance(answer, tuple):
        captures = ";".join("-" if c is None else f"{c[0]},{c[1]}" for c in answer[1])
        names = ",".join(f"{n}={'-' if r is None else f'{r[0]},{r[1]}'}" for n, r in answer[2])
        return "match:" + captures + ("|" + names if names else "")
    return answer


if __name__ == "__main__":
    sys.exit(main())
