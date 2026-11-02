#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Bundle sjl-11-001's classification of the comparison's differences, written before any judged run.

    python3 -I scripts/reference-arms/classify.py --comparison DIR --runs DIR --broiler-js CHECKOUT --commit SHA
        --correspondence FILE --unicode-pins DIR [--cutoff N] [--out DIR] [--cache FILE]

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

**The harness's stop.** A case the harness stopped, lost or skipped (tests/ReferenceArms/Common/Limits.cs: a
calibration's time limit, a run's stop list or backstop, a crash) is answered `limit:harness`, whichever it was; the older answers `crash:exit-N`,
`limit:wall-clock` and `limit:timeout` are read as it. Arm J's stop is class (a) under row B8 when the input is that
row's (a sequence of at least B8_TERMS terms under u or v, which Broiler.Regex 0.1.0-preview.4's matcher recurses
through once per term) and S's answer is the arbiter's, and class (b) under decision D10 otherwise; arm V's or S's is
class (c), as neither declares a bound the harness's limit stands for.

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
It writes DIR/classification.json (the counts by class, row and run, the arbiter's commit and blob) and
DIR/classified.tsv.gz (each difference with its class, row, the arbiter's answer and why), and exits 0 when no
difference is (c), 1 when one is, 2 when it refuses to run (a row it names is missing from the correspondence, or
its class does not name the engine the predicate corrects, or an argument is wrong), and 3 when it fails (an
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

from population_format import decode_units, open_text  # noqa: E402

CLASSIFIER_VERSION = "sjl-11-001-classification@3"
DEFAULT_CUTOFF = 1_000_000


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


ROWS = [
    ("S1", "V", lambda d, c: arm_refuses_what_s_accepts(d) and duplicate_names(d), "Correction of Broiler.VM"),
    ("S2a", "J", lambda d, c: arm_accepts_what_s_refuses(d) and duplicate_only_when_decoded(d)
     and c.refusal_code(d) == "early:duplicate-group-name", "Correction of Broiler.JS"),
    ("S5", "J", lambda d, c: class_k_refused(d), "Broiler.JS: **correction of Broiler.JS**, by D-s5"),
    ("S7", "J", quantified_lookbehind_accepted, "Correction of Broiler.JS"),
    ("S8", "J", v_mode_syntax, "Correction of Broiler.JS"),
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
]

# The rows that read a stop: they are read where one side stopped and the other answered, before the policy's (b).
STOP_ROWS = frozenset({"B8"})

POLICY_ROWS = {"V": ("S3", "B6", "B7"), "J": ("S3",)}
ENGINE_WORDS = {"V": "Broiler.VM", "J": "Broiler.JS"}


class Context:
    """What a predicate reads beyond the difference: the arbiter's reason for a refusal, row M2's case pairs, and arm
    J's answers under the invariant culture (row M2a)."""

    def __init__(self, refusal_code=None, case_pairs=frozenset(), invariant=None) -> None:
        self._refusal_code = refusal_code
        self.case_pairs = case_pairs
        self.invariant = invariant or {}

    def refusal_code(self, d):
        return self._refusal_code(d) if self._refusal_code else None


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
    """The exit status: 0 no class (c), 1 a class (c) difference, 2 refused (or an argument argparse refuses), 3 failed."""
    try:
        return run(argv)
    except SystemExit:
        raise
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
    parser.add_argument("--cache", type=Path, help="a file of the arbiter's answers, read when it exists and written "
                        "after the run: an answer depends only on the input, the arbiter's commit and the cut-off")
    args = parser.parse_args(argv)
    out = args.out or args.comparison
    out.mkdir(parents=True, exist_ok=True)
    rows = read_correspondence(args.correspondence)
    problems = check_rows(rows)
    if problems:
        print("refused:\n  " + "\n  ".join(problems), file=sys.stderr)
        return 2
    arbiter = Arbiter(args.broiler_js, args.commit, args.unicode_pins, args.cutoff)
    if args.cache and args.cache.exists():
        arbiter.load(args.cache)
    controls = control_cases(args.runs)
    partners = data_partners(args.unicode_pins)
    context = Context(lambda d: arbiter.refusal_code(d["pattern"], d["flags"]), frozenset(arbiter.case_pairs()),
                      invariant_answers(args.comparison / "differences.tsv.gz"))
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
            if controls.get(d["run"]) == d["id"]:
                category, row, why = "unclassified", "-", "the negative control's planted difference"
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
              "correspondence": {"file": str(args.correspondence),
                                 "sha256": hashlib.sha256(args.correspondence.read_bytes()).hexdigest()},
              "controls": controls, "counts": dict(sorted(counts.items())), "by-class-row-run-population": dict(sorted(by_row.items()))}
    (out / "classification.json").write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    if args.cache:
        arbiter.save(args.cache)
    print(json.dumps(result["counts"], sort_keys=True))
    return 1 if counts.get("c") else 0


def render(answer) -> str:
    if isinstance(answer, tuple):
        captures = ";".join("-" if c is None else f"{c[0]},{c[1]}" for c in answer[1])
        names = ",".join(f"{n}={'-' if r is None else f'{r[0]},{r[1]}'}" for n, r in answer[2])
        return "match:" + captures + ("|" + names if names else "")
    return answer


if __name__ == "__main__":
    sys.exit(main())
