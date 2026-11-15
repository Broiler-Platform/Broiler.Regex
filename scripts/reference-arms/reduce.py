#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Bundle sjl-11-001's reduction of the inputs the arbiter cannot reach, written 2026-11-15 (SJL-11.2's tail).

    python3 -I -B scripts/reference-arms/reduce.py prefixes --comparison DIR --broiler-js CHECKOUT --commit SHA
        --unicode-pins DIR --out DIR [--cutoff N] [--workers N]
    python3 -I -B scripts/reference-arms/reduce.py read --comparison DIR --reduced DIR

The rule's arbiter clause reads "a difference the arbiter cannot reach in its step cut-off is class (c) until reduced
to an input it can" (reference-arms.md), and docs/roadmap.md's record of task 2.13 asks that each such input be
reduced or recorded. This script does the one reduction it declares and records what the runs give; it changes no
class, no row and no cut-off, and it decides nothing the classifier does not.

**prefixes.** It reads a run's comparison (differences.tsv.gz) and classification (classified.tsv.gz) and takes every
difference in class (c) whose arbiter answer is `arbiter:...` (past the cut-off, or a recursion or memory limit of the
arbiter). For each distinct input, an exec, it asks the arbiter (classify.py's Arbiter: regexpref.py at the commit
given, the same cut-off, 1,000,000 Matcher invocations by default) for the input with the subject cut to a prefix of
m code units, the lastIndex kept: m from the lastIndex up under g or y, from 0 otherwise (RegExpBuiltinExec reads
lastIndex under those flags only), the original length being the one it cannot reach. It finds
by bisection a length m* the arbiter reaches with m* + 1 past it, the arbiter's cost read as growing with the prefix
(where it does not, the bisection still ends at such a pair of lengths, and m* is a length the arbiter reaches). The
reduced input is the subject's first m* code units; it is written as the case `<id>~p<m*>` of its class's file under
OUT/population, in the population's format, so that the arms, compare.py and classify.py run over it unchanged.
OUT/reductions.tsv lists each input: its id, the original length, the lastIndex the exec reads (0 without g or y),
m*, the arbiter's answer there, and
`none` when the arbiter reaches no prefix from there up, not even the shortest.

**read.** After the arms ran over OUT/population and compare.py and classify.py over their answers (into
OUT/comparison), it pairs each original difference (an id and a run) with its reduced case and writes
OUT/outcomes.tsv and OUT/outcomes.json:
- `reduced`: the reduced case differs in the same run with the same kinds of answer on each side (a stop against an
  answer, as before) and the arbiter answers it: its class and row are the classifier's over the reduced case;
- `differs-otherwise`: the reduced case differs, but in other kinds of answer; listed with its class;
- `agrees`: S and the arm answer the reduced case alike: the input is reduced to one the arbiter reaches, where the
  difference does not survive. The original difference is left in class (c), recorded with both answers there: this
  script takes a reduction to discharge class (c) only when the same difference survives it, a reading the rule does
  not settle and its owner decides (the other reading: the reduced input's agreement with the arbiter ends the rule's
  "until reduced" condition). (Reworded 2026-11-15, after the review of the tail: this item read "the difference does
  not survive the reduction, so the input stays one the arbiter cannot reach".)
- `no-prefix`: the arbiter reaches no prefix, not even the shortest: what it cannot reach is in the pattern, which
  this reduction does not change.
"""

from __future__ import annotations

import argparse
import json
import sys
from collections import Counter, defaultdict
from multiprocessing import Pool
from pathlib import Path

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from population_format import decode_units, encode_units, open_text  # noqa: E402

REDUCER_VERSION = "sjl-11-001-reduction@1"


def rows(path: Path):
    header = None
    with open_text(path) as handle:
        for line in handle:
            if line.startswith("#"):
                header = line[2:].rstrip("\n").split("\t")
                continue
            yield dict(zip(header, line.rstrip("\n").split("\t")))


def unreachable_differences(comparison: Path) -> list[dict]:
    """Every difference the classifier left in class (c) because the arbiter gave no answer, with its input."""
    keys = {(r["id"], r["run"]) for r in rows(comparison / "classified.tsv.gz")
            if r["category"] == "c" and r["arbiter"].startswith("arbiter:")}
    return [d for d in rows(comparison / "differences.tsv.gz") if (d["id"], d["run"]) in keys]


_ARBITER = None


def _start(broiler_js: str, commit: str, pins: str, cutoff: int) -> None:
    global _ARBITER
    from classify import Arbiter  # noqa: PLC0415 - each worker takes its own copy of the arbiter
    _ARBITER = Arbiter(Path(broiler_js), commit, Path(pins), cutoff)


def _reaches(case: dict, length: int):
    subject = encode_units(decode_units(case["subject"])[:length])
    answer = _ARBITER.answer(case["pattern"], case["flags"], subject, case["lastIndex"], "exec")
    reached = not (isinstance(answer, str) and answer.startswith("arbiter:"))
    return reached, answer


def _prefix(case: dict) -> dict:
    from classify import render  # noqa: PLC0415
    n = len(decode_units(case["subject"]))
    # RegExpBuiltinExec reads lastIndex under g or y only, and from 0 otherwise (the harness does the same).
    flags = "" if case["flags"] == "." else case["flags"]
    k = int(case["lastIndex"]) if ("g" in flags or "y" in flags) else 0
    probes = 1
    reached, answer = _reaches(case, k)
    if not reached:
        return {"id": case["id"], "length": n, "lastIndex": k, "prefix": None, "answer": render(answer), "probes": probes}
    lo, hi, at_lo = k, n, answer
    while hi - lo > 1:
        mid = (lo + hi) // 2
        probes += 1
        reached, answer = _reaches(case, mid)
        if reached:
            lo, at_lo = mid, answer
        else:
            hi = mid
    return {"id": case["id"], "length": n, "lastIndex": k, "prefix": lo, "answer": render(at_lo), "probes": probes}


def prefixes(args) -> int:
    differences = unreachable_differences(args.comparison)
    cases: dict[str, dict] = {}
    for d in differences:
        if d["op"] != "exec":
            raise SystemExit(f"{d['id']}: an unreachable difference that is not an exec")
        cases.setdefault(d["id"], d)
    ordered = [cases[i] for i in sorted(cases)]
    with Pool(args.workers, initializer=_start,
              initargs=(str(args.broiler_js), args.commit, str(args.unicode_pins), args.cutoff)) as pool:
        found = pool.map(_prefix, ordered, chunksize=1)
    out = args.out
    (out / "population").mkdir(parents=True, exist_ok=True)
    by_class: dict[str, list[str]] = defaultdict(list)
    with open_text(out / "reductions.tsv", "wt") as listing:
        listing.write("# id\tclass\tlength\tlastIndex\tprefix\tarbiter\tprobes\n")
        for case, result in zip(ordered, found, strict=True):
            prefix = "none" if result["prefix"] is None else str(result["prefix"])
            listing.write("\t".join([case["id"], case["class"], str(result["length"]), str(result["lastIndex"]), prefix,
                                     result["answer"], str(result["probes"])]) + "\n")
            if result["prefix"] is not None:
                subject = encode_units(decode_units(case["subject"])[:result["prefix"]])
                by_class[case["class"]].append("\t".join([f"{case['id']}~p{result['prefix']}", "exec", case["pattern"],
                                                          case["flags"], subject, case["lastIndex"], case["policy"]]))
    for name, lines in sorted(by_class.items()):
        with open_text(out / "population" / f"{name}.tsv.gz", "wt") as handle:
            handle.write("# id\top\tpattern\tflags\tsubject\tlastIndex\tpolicy\n")
            for line in lines:
                handle.write(line + "\n")
    manifest = {"reducer": REDUCER_VERSION, "partial": True, "cutoff": args.cutoff, "classes": sorted(by_class),
                "inputs": len(ordered), "differences": len(differences)}
    (out / "population" / "manifest.json").write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n",
                                                      encoding="utf-8")
    print(json.dumps({"inputs": len(ordered), "differences": len(differences),
                      "reduced-inputs": sum(1 for r in found if r["prefix"] is not None)}, sort_keys=True))
    return 0


def kind(answer: str) -> str:
    """An answer's kind: a stop (limit, refusal for nesting) or not, and which side of a match or null."""
    if answer.startswith(("limit:", "crash:", "refuse:nesting")):
        return "stop"
    return answer.split(":", 1)[0]


def read(args) -> int:
    differences = unreachable_differences(args.comparison)
    listing = {r["id"]: r for r in rows(args.reduced / "reductions.tsv")}
    reduced = {(r["id"], r["run"]): r for r in rows(args.reduced / "comparison" / "classified.tsv.gz")}
    s_answers = {}
    for path in sorted((args.reduced / "runs").glob("*.S.tsv.gz")):
        with open_text(path) as handle:
            for line in handle:
                if not line.startswith("#"):
                    case_id, answer, _ = line.rstrip("\n").split("\t")
                    s_answers[case_id] = answer
    counts: Counter = Counter()
    with open_text(args.reduced / "outcomes.tsv", "wt") as out:
        out.write("# id\trun\tS\tarm\treduced\tS'\tarm'\tarbiter'\toutcome\tclass'\trow'\n")
        for d in differences:
            entry = listing[d["id"]]
            if entry["prefix"] == "none":
                outcome, fields = "no-prefix", ["-", "-", "-", entry["arbiter"], "-", "-"]
            else:
                rid = f"{d['id']}~p{entry['prefix']}"
                r = reduced.get((rid, d["run"]))
                if r is None:
                    outcome = "agrees"
                    fields = [rid, s_answers.get(rid, "?"), s_answers.get(rid, "?"), entry["arbiter"], "-", "-"]
                else:
                    same = kind(r["S"]) == kind(d["S"]) and kind(r["arm"]) == kind(d["arm"])
                    outcome = "reduced" if same and not r["arbiter"].startswith("arbiter:") else "differs-otherwise"
                    fields = [rid, r["S"], r["arm"], r["arbiter"], r["category"], r["row"]]
            counts[f"{outcome}/{d['run']}/{d['class']}"] += 1
            out.write("\t".join([d["id"], d["run"], d["S"], d["arm"], *fields[:4], outcome, *fields[4:]]) + "\n")
    result = {"reducer": REDUCER_VERSION, "differences": len(differences), "outcomes": dict(sorted(counts.items()))}
    (args.reduced / "outcomes.json").write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(result["outcomes"], sort_keys=True))
    return 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest="mode", required=True)
    p = sub.add_parser("prefixes")
    p.add_argument("--comparison", required=True, type=Path)
    p.add_argument("--broiler-js", required=True, type=Path)
    p.add_argument("--commit", required=True)
    p.add_argument("--unicode-pins", required=True, type=Path)
    p.add_argument("--out", required=True, type=Path)
    p.add_argument("--cutoff", type=int, default=1_000_000)
    p.add_argument("--workers", type=int, default=4)
    r = sub.add_parser("read")
    r.add_argument("--comparison", required=True, type=Path)
    r.add_argument("--reduced", required=True, type=Path)
    args = parser.parse_args(argv)
    return prefixes(args) if args.mode == "prefixes" else read(args)


if __name__ == "__main__":
    sys.exit(main())
