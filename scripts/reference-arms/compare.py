#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Bundle sjl-11-001's comparison: arm S's answers against each arm's, exactly, per operation, input and arm.

    python3 -I scripts/reference-arms/compare.py --population DIR --runs DIR --out DIR

It reads the population (generate_population.py) and the arms' answer files (tests/ReferenceArms/, one per class, arm
and culture: <class>.<arm>[.<culture>].tsv.gz), and writes DIR/summary.json (per class, operation and arm run: the
cases compared, those equal and those that differ, and S's answers by kind) and DIR/differences.tsv.gz (one line per
difference: the case, its class and operation, the arm run, the inputs as the population writes them, S's answer and the
arm's). It **classifies nothing**: classify.py does, from these files. Charges are not compared (the rule's
"comparison"): the arms' charge columns are summed into summary.json and nothing else. A difference in a case whose id an
arm's run file names as its control case is still a difference here; the classifier reports it unclassified.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from collections import Counter, defaultdict
from pathlib import Path

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from population_format import open_text  # noqa: E402

CLASSES = ("P1", "P2", "P3", "P4", "P5", "P6", "P7")
COMPARISON_VERSION = "sjl-11-001-comparison@1"


def kind(answer: str) -> str:
    return answer.split(":", 1)[0].split("#", 1)[0]


def arm_runs(runs: Path, name: str) -> list[tuple[str, Path]]:
    """The arm runs of one class other than S's, by run name (J, J.tr-TR, V), in name order."""
    out = []
    for path in sorted(runs.glob(f"{name}.*.tsv.gz")):
        run = path.name[len(name) + 1:-len(".tsv.gz")]
        if run != "S":
            out.append((run, path))
    return out


def lines(path: Path):
    with open_text(path) as handle:
        for line in handle:
            if line.startswith("#"):
                continue
            yield line.rstrip("\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--population", required=True, type=Path)
    parser.add_argument("--runs", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args(argv)
    args.out.mkdir(parents=True, exist_ok=True)
    summary: dict = {"comparison": COMPARISON_VERSION, "population": {}, "runs": {}, "classes": {}}
    manifest = args.population / "manifest.json"
    if manifest.exists():
        summary["population"] = {"manifest-sha256": hashlib.sha256(manifest.read_bytes()).hexdigest(),
                                 "partial": json.loads(manifest.read_text(encoding="utf-8")).get("partial")}
    for run_file in sorted(args.runs.glob("run.*.json")):
        summary["runs"][run_file.name] = json.loads(run_file.read_text(encoding="utf-8"))
    differences_path = args.out / "differences.tsv.gz"
    total = 0
    with open_text(differences_path, "wt") as out:
        out.write("# id\tclass\top\trun\tpattern\tflags\tsubject\tlastIndex\tpolicy\tS\tarm\n")
        for name in CLASSES:
            population = args.population / f"{name}.tsv.gz"
            s_path = args.runs / f"{name}.S.tsv.gz"
            if not population.exists() or not s_path.exists():
                continue
            runs = arm_runs(args.runs, name)
            readers = [(run, lines(path)) for run, path in runs]
            counts: dict = defaultdict(lambda: Counter())
            s_kinds: Counter = Counter()
            charges: Counter = Counter()
            for case_line, s_line in zip(lines(population), lines(s_path), strict=True):
                case = case_line.split("\t")
                s_id, s_answer, s_charge = s_line.split("\t")
                if s_id != case[0]:
                    raise SystemExit(f"S's answers are out of step with the population at {case[0]}")
                op = case[1]
                s_kinds[f"{op}:{kind(s_answer)}"] += 1
                if s_charge != "-":
                    charges["S"] += int(s_charge)
                for run, reader in readers:
                    arm_id, arm_answer, arm_charge = next(reader).split("\t")
                    if arm_id != case[0]:
                        raise SystemExit(f"{run}'s answers are out of step with the population at {case[0]}")
                    if arm_charge != "-":
                        charges[run] += int(arm_charge)
                    tally = counts[f"{op}/{run}"]
                    tally["compared"] += 1
                    if arm_answer == s_answer:
                        tally["equal"] += 1
                    else:
                        tally["different"] += 1
                        total += 1
                        out.write("\t".join([case[0], name, op, run, case[2], case[3], case[4], case[5], case[6],
                                             s_answer, arm_answer]) + "\n")
            for run, reader in readers:
                if next(reader, None) is not None:
                    raise SystemExit(f"{run}'s answers for {name} run past the population")
            summary["classes"][name] = {"by-operation-and-run": {k: dict(sorted(v.items())) for k, v in sorted(counts.items())},
                                        "S-answers": dict(sorted(s_kinds.items())),
                                        "charges-recorded": dict(sorted(charges.items())),
                                        "runs": [run for run, _ in runs]}
    summary["differences"] = total
    summary["differences-sha256"] = hashlib.sha256(differences_path.read_bytes()).hexdigest()
    (args.out / "summary.json").write_text(json.dumps(summary, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"{total} differences")
    return 0


if __name__ == "__main__":
    sys.exit(main())
