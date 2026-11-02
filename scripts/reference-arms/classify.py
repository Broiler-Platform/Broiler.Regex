#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Bundle sjl-11-001's classification of the comparison's differences, written before any judged run.

    python3 -I scripts/reference-arms/classify.py --comparison DIR --runs DIR --broiler-js CHECKOUT --commit SHA
        --correspondence FILE --unicode-pins DIR [--cutoff N] [--out DIR]

It reads compare.py's differences.tsv.gz, asks the arbiter for each difference's input, and classes each difference
as the rule's admitted classes read (docs/evidence/sjl-11-001/reference-arms.md):

- **the arbiter** is scripts/conformance/regexpref.py (`ref:regexpref@1`) at the Broiler.JS commit `--commit` names,
  taken from the checkout `--broiler-js` by `git archive` with unicoderef.py and the corpus's unicode.pin, and run over
  Broiler.Unicode's archived UCD 17.0.0 (`--unicode-pins`); never the agreement of the arms. An exec it cannot finish
  in `--cutoff` Matcher invocations is unreachable;
- **(a)** the arm's answer is not the arbiter's, S's is, and a row of the correspondence (`--correspondence`, Broiler.VM's
  docs/evidence/sjl-11-001/correspondence.md) whose class names the arm's engine as corrected matches the input
  (ROWS below: each row's predicate over the input and the answers);
- **(a')** as (a), arm J, and row U3's data decision (a code point whose case mapping .NET's data and UCD 17.0.0 hold
  differently is in the pattern or the subject, under i without u or v);
- **(b)** an outcome kind that follows from a policy: one side refused for nesting or stopped at a limit (steps, a
  ceiling, a timeout) and the other's answer is the arbiter's or the arbiter is past its cut-off, under rows S3, B6 and
  B7 (V) and decision D10 (J); and row M4's empty match from a lastIndex inside a surrogate pair, which the arbiter
  cannot answer (decision D41);
- **(c)** everything else: S's answer is not the arbiter's, or the arm's is wrong and no row names it, or the arbiter
  cannot reach the input. A difference with arm V under any row but V's corrections, the policy rows and M4 is (c).

A difference in the case an arm's run file names as its negative-control case is listed **unclassified**.
It writes DIR/classification.json (the counts by class, row and run, the arbiter's commit and blob) and
DIR/classified.tsv.gz (each difference with its class, row, the arbiter's answer and why), and exits 0 when no
difference is (c), 1 when one is, 2 when it refuses to run (a row it names is missing from the correspondence, or
its class does not name the engine the predicate corrects).
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
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from population_format import decode_units, open_text  # noqa: E402

CLASSIFIER_VERSION = "sjl-11-001-classification@1"
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
    answer = answer.split("#", 1)[0]
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

    def _identity(self) -> list:
        return [self.version, self.commit, self.cutoff]

    def load(self, path: Path) -> None:
        """The answers a run saved, when they are this arbiter's at this cut-off; otherwise none."""
        with open_text(path) as handle:
            if json.loads(handle.readline()) != self._identity():
                return
            for line in handle:
                key, answer = json.loads(line)
                if isinstance(answer, list):
                    answer = ("match", tuple(None if c is None else tuple(c) for c in answer[1]),
                              tuple((n, None if r is None else tuple(r)) for n, r in answer[2]))
                self.cache[tuple(key)] = answer

    def save(self, path: Path) -> None:
        with open_text(path, "wt") as handle:
            handle.write(json.dumps(self._identity()) + "\n")
            for key, answer in self.cache.items():
                handle.write(json.dumps([list(key), answer]) + "\n")

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
# The rows. Each: (row, the engine whose correction the row records, predicate over the difference).

def units_text(field: str) -> str:
    from population_format import text_of
    return text_of(decode_units(field))


def has_flag(d, letters: str) -> bool:
    flags = "" if d["flags"] == "." else d["flags"]
    return any(ch in flags for ch in letters)


def unicode_mode(d) -> bool:
    return has_flag(d, "uv")


def inside_pair(d) -> bool:
    if d["op"] != "exec" or not has_flag(d, "gy") or not unicode_mode(d):
        return False
    s = decode_units(d["subject"])
    k = int(d["lastIndex"])
    return 0 < k < len(s) and 0xDC00 <= s[k] <= 0xDFFF and 0xD800 <= s[k - 1] <= 0xDBFF


NAME = re.compile(r"\(\?<([^=!>][^>]*)>")


NAME_ESCAPE = re.compile(r"\\u\{([0-9A-Fa-f]+)\}|\\u([0-9A-Fa-f]{4})")


def decoded_name(name: str) -> str:
    """A GroupName's string value: its \\u escapes decoded (22.2.1.6, RegExpIdentifierCodePoints), a pair of escaped
    surrogates joined."""
    text = NAME_ESCAPE.sub(lambda m: chr(int(m.group(1) or m.group(2), 16)), name)
    return text.encode("utf-16-le", "surrogatepass").decode("utf-16-le", "surrogatepass")


def duplicate_names(d) -> bool:
    """Row S1: a name declared twice, compared by its value, so that `(?<\\u0061>x)|(?<a>y)` repeats `a` (S2a)."""
    names = [decoded_name(n) for n in NAME.findall(units_text(d["pattern"]))]
    return len(names) != len(set(names))


def escaped_names(d) -> bool:
    text = units_text(d["pattern"])
    return any("\\" in n for n in NAME.findall(text)) or bool(re.search(r"\\k<[^>]*\\", text))


def k_escape(d) -> bool:
    return "\\k" in units_text(d["pattern"])


def lead_before_braced(d) -> bool:
    return bool(re.search(r"\\u[dD][89abAB][0-9a-fA-F]{2}\\u\{", units_text(d["pattern"]))) and unicode_mode(d)


def backreference_across_pair(d) -> bool:
    if d["op"] != "exec" or not unicode_mode(d):
        return False
    s = decode_units(d["subject"])
    return bool(re.search(r"\\[1-9]|\\k<", units_text(d["pattern"]))) and any(0xD800 <= u <= 0xDFFF for u in s)


def line_terminators(d) -> bool:
    text = units_text(d["pattern"])
    s = decode_units(d["subject"]) if d["op"] == "exec" else []
    return any(ch in text for ch in "^$.") and any(u in (0x0D, 0x2028, 0x2029, 0x85) for u in s)


def case_insensitive_without_u(d) -> bool:
    return has_flag(d, "i") and not unicode_mode(d)


def turkic_or_lithuanian(d) -> bool:
    return case_insensitive_without_u(d) and d["run"] in ("J.tr-TR", "J.lt-LT")


def quantified_lookbehind(text: str) -> bool:
    """Whether a look-behind group is followed by a quantifier, `(?<=a)*`: the group's end found by counting
    parentheses outside classes and escapes."""
    for m in re.finditer(r"\(\?<[=!]", text):
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
                    if i + 1 < len(text) and text[i + 1] in "*+?{":
                        return True
                    break
            i += 1
    return False


def unicode_mode_syntax(d) -> bool:
    """Row S7: an acceptance difference under u; or, without u, a quantified look-behind, which Broiler.JS accepts
    (`(?<=a)*`, the row's one Broiler.JS answer that differs) where an early error refuses it."""
    if d["op"] != "accept":
        return False
    return has_flag(d, "u") or (d["arm"] == "accept" and quantified_lookbehind(units_text(d["pattern"])))


def unicode_sets(d) -> bool:
    return has_flag(d, "v")


def preview4_matcher(d) -> bool:
    """Rows B1, B2, B3, B5 and B8: Broiler.Regex 0.1.0-preview.4's budget and its recursion, which Broiler.JS routes
    every u and v pattern to: J answers no match, or the process ends, where the specification has an answer."""
    return d["op"] == "exec" and (kind(d["arm"]) in ("crash", "null") and unicode_mode(d) or kind(d["arm"]) == "crash")


ROWS = [
    ("S1", "V", lambda d: duplicate_names(d)),
    ("S1", "J", lambda d: duplicate_names(d)),
    ("S2a", "J", lambda d: escaped_names(d)),
    ("S2b", "J", lambda d: escaped_names(d)),
    ("S5", "V", lambda d: k_escape(d)),
    ("S7", "J", unicode_mode_syntax),
    ("S8", "J", unicode_sets),
    ("S10", "V", lead_before_braced),
    ("M2a", "J", turkic_or_lithuanian),
    ("M2", "J", case_insensitive_without_u),
    ("M3", "J", line_terminators),
    ("M4", "V", lambda d: inside_pair(d)),
    ("M4", "J", lambda d: inside_pair(d)),
    ("M7", "V", backreference_across_pair),
    ("B2", "J", preview4_matcher),
]

POLICY_ROWS = {"V": ("S3", "B6", "B7"), "J": ("S3",)}
ENGINE_WORDS = {"V": "Broiler.VM", "J": "Broiler.JS"}


def read_correspondence(path: Path) -> dict[str, str]:
    rows = {}
    for line in path.read_text(encoding="utf-8").split("\n"):
        m = re.match(r"\| ([SMBFTU]\d+[a-z]?) \|", line)
        if m:
            rows.setdefault(m.group(1), line.rstrip().rstrip("|").rsplit("|", 1)[-1])
    return rows


def check_rows(rows: dict[str, str]) -> list[str]:
    problems = []
    for row, engine, _ in ROWS:
        cls = rows.get(row)
        if cls is None:
            problems.append(f"row {row} is not in the correspondence")
        elif f"orrection of {ENGINE_WORDS[engine]}" not in cls and not (row == "M4") and \
                not (engine == "J" and "preview.4" in cls):
            problems.append(f"row {row}'s class does not name {ENGINE_WORDS[engine]} as corrected: {cls.strip()[:120]}")
    for engine, policy in POLICY_ROWS.items():
        for row in policy:
            if row not in rows:
                problems.append(f"policy row {row} is not in the correspondence")
    if "U3" not in rows or "data decision" not in rows["U3"]:
        problems.append("row U3 is not the correspondence's data decision")
    return problems


# --------------------------------------------------------------------------------------------------

LIMITS = ("limit", "crash")


def stopped(answer: str) -> bool:
    return kind(answer) == "limit" or answer == "refuse:nesting"


def both_stopped(d) -> bool:
    return stopped(d["S"]) and stopped(d["arm"])


def classify(d, arbiter_answer, data_partners: set[int]) -> tuple[str, str, str]:
    engine = d["run"].split(".", 1)[0]
    s, arm = d["S"], d["arm"]
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
        # Both stopped, by different bounds: each side's policy, and the arbiter past its cut-off or the same answer.
        row = "S3" if "nesting" in (s + arm) else ("B6" if engine == "V" and "ceiling" in arm else "B7" if engine == "V" else "D10")
        return "b", row, "both sides stopped by their own policies, which the specification leaves to the implementation"
    if s_policy != arm_policy:
        other = arm_c if s_policy else s_c
        if other == arb or unreachable and arb == "arbiter:exhausted" or (other == "accept" and arb == "accept"):
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
    if engine == "J" and case_insensitive_without_u(d):
        units = set(decode_units(d["pattern"])) | (set(decode_units(d["subject"])) if d["op"] == "exec" else set())
        if units & data_partners:
            return "a'", "U3", "arm J answers from .NET's case data, S from UCD 17.0.0 (SJL-D-6 (c))"
    for row, row_engine, predicate in ROWS:
        if row_engine == engine and predicate(d):
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


def main(argv: list[str] | None = None) -> int:
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
            if controls.get(d["run"]) == d["id"]:
                category, row, why = "unclassified", "-", "the negative control's planted difference"
                arb = "-"
            elif both_stopped(d):
                # Both sides stopped by their own bounds: the class does not read the arbiter, so it is not asked.
                arb = "-"
                category, row, why = classify(d, arb, partners)
            else:
                arb = arbiter.answer(d["pattern"], d["flags"], d["subject"], d["lastIndex"], d["op"])
                category, row, why = classify(d, arb, partners)
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
