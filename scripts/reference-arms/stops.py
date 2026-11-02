#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Bundle sjl-11-001's stop list, from a calibration run of the reference arms.

    python3 -I scripts/reference-arms/stops.py --runs DIR --population DIR --out FILE

A calibration (`run.sh OUT --calibrate`, tests/ReferenceArms/README.md, "Limits") runs every arm under the time limits
of tests/ReferenceArms/Common/Limits.cs and writes, in DIR/timing/<class>.<run>.tsv, each case the harness stopped
(a limit, or the worker's end). This script writes them as the stop list: one line per stopped case, the run's name
(S, V, J, J.tr-TR, J.lt-LT) and the case's id, tab-separated, sorted, under a header naming the population (its
manifest's SHA-256) and the count per run. The repository keeps the list as tests/ReferenceArms/stops.tsv; every run
that is not a calibration answers a listed case as the harness's stop without running it, so that no limit of time
decides any answer of the runs whose listings the comparison writes. Written 2026-11-02, at task 2.13's second review.
"""

from __future__ import annotations

import argparse
import hashlib
import sys
from collections import Counter
from pathlib import Path

STOPS_VERSION = "sjl-11-001-stops@1"


def stopped_cases(runs: Path) -> list[tuple[str, str]]:
    """Every (run, case id) the calibration's timing files record as stopped."""
    out = set()
    for path in sorted((runs / "timing").glob("*.tsv")):
        population, _, run = path.name[:-len(".tsv")].partition(".")
        if not population or not run:
            continue
        for line in path.read_text(encoding="utf-8").split("\n"):
            if not line or line.startswith("#"):
                continue
            fields = line.split("\t")
            if len(fields) >= 2 and fields[1].startswith("stop:"):
                out.add((run, fields[0]))
    return sorted(out)


def write(cases: list[tuple[str, str]], manifest_sha256: str, out: Path) -> None:
    counts = Counter(run for run, _ in cases)
    lines = [f"# {STOPS_VERSION}: the cases a calibration of bundle sjl-11-001's reference arms stopped "
             "(tests/ReferenceArms/README.md, \"Limits\"), written by scripts/reference-arms/stops.py.",
             f"# population manifest SHA-256 {manifest_sha256}",
             "# cases per run: " + ", ".join(f"{run} {counts[run]}" for run in sorted(counts)),
             "# run\tid"]
    lines += [f"{run}\t{case}" for run, case in cases]
    out.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--runs", required=True, type=Path)
    parser.add_argument("--population", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args(argv)
    manifest = args.population / "manifest.json"
    digest = hashlib.sha256(manifest.read_bytes()).hexdigest() if manifest.exists() else "-"
    cases = stopped_cases(args.runs)
    write(cases, digest, args.out)
    print(f"{len(cases)} stopped cases")
    return 0


if __name__ == "__main__":
    sys.exit(main())
