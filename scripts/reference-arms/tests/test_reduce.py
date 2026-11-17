# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""The reduction's reading (reduce.py read), from hand-made comparisons. Written 2026-11-15, at SJL-11.2's tail; the
prefix search asks the arbiter and is run over a real comparison only."""

from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parents[1]
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import reduce as R  # noqa: E402
from population_format import open_text  # noqa: E402

DIFFERENCES = "# id\tclass\top\trun\tpattern\tflags\tsubject\tlastIndex\tpolicy\tS\tarm\n"
CLASSIFIED = "# id\tclass\top\trun\tS\tarm\tarbiter\tcategory\trow\twhy\n"


def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with open_text(path, "wt") as handle:
        handle.write(text)


class ReduceTests(unittest.TestCase):
    def test_a_stop_is_one_kind_and_an_answer_its_own(self) -> None:
        self.assertEqual("stop", R.kind("limit:steps"))
        self.assertEqual("stop", R.kind("limit:harness"))
        self.assertEqual("match", R.kind("match:0,1"))
        self.assertEqual("null", R.kind("null"))
        self.assertEqual("refuse", R.kind("refuse:syntax"))

    def test_each_unreachable_difference_is_paired_with_its_reduced_case(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            root = Path(scratch)
            comparison, reduced = root / "comparison", root / "reduced"
            write(comparison / "differences.tsv.gz", DIFFERENCES
                  + "P4-1\tP4\texec\tJ\t0061\t.\t0061*30\t0\tdefault\tlimit:steps\tnull\n"
                  + "P4-2\tP4\texec\tJ\t0062\t.\t0062*30\t0\tdefault\tmatch:0,30\tlimit:harness\n"
                  + "P4-3\tP4\texec\tJ\t0063\t.\t0063*30\t0\tdefault\tnull\tlimit:harness\n"
                  + "P4-4\tP4\texec\tJ\t0064\t.\t0064\t0\tdefault\tnull\tmatch:0,1\n")
            write(comparison / "classified.tsv.gz", CLASSIFIED
                  + "P4-1\tP4\texec\tJ\tlimit:steps\tnull\tarbiter:exhausted\tc\t-\tw\n"
                  + "P4-2\tP4\texec\tJ\tmatch:0,30\tlimit:harness\tarbiter:exhausted\tc\t-\tw\n"
                  + "P4-3\tP4\texec\tJ\tnull\tlimit:harness\tarbiter:exhausted\tc\t-\tw\n"
                  + "P4-4\tP4\texec\tJ\tnull\tmatch:0,1\tnull\tc\t-\tw\n")
            write(reduced / "reductions.tsv", "# id\tclass\tlength\tlastIndex\tprefix\tarbiter\tprobes\n"
                  + "P4-1\tP4\t30\t0\t20\tnull\t5\nP4-2\tP4\t30\t0\t18\tmatch:0,18\t5\nP4-3\tP4\t30\t0\tnone\tarbiter:exhausted\t1\n")
            write(reduced / "comparison" / "classified.tsv.gz", CLASSIFIED
                  + "P4-1~p20\tP4\texec\tJ\tlimit:steps\tnull\tnull\tb\tD10\tw\n")
            write(reduced / "runs" / "P4.S.tsv.gz", "# id\tanswer\tcharge\nP4-1~p20\tlimit:steps\t10\nP4-2~p18\tmatch:0,18\t9\n")
            self.assertEqual(0, R.main(["read", "--comparison", str(comparison), "--reduced", str(reduced)]))
            with open_text(reduced / "outcomes.tsv") as handle:
                lines = [line.rstrip("\n").split("\t") for line in handle if not line.startswith("#")]
            outcomes = {line[0]: (line[8], line[9]) for line in lines}
            self.assertEqual({"P4-1": ("reduced", "b"), "P4-2": ("agrees", "-"), "P4-3": ("no-prefix", "-")}, outcomes)

    def test_check_finds_a_reduction_of_this_comparison_or_of_another(self) -> None:
        # Added 2026-11-17, with run.sh's reduction step: --classify-only reads an earlier reduction only when it is
        # this comparison's.
        import json  # noqa: PLC0415
        with tempfile.TemporaryDirectory() as scratch:
            root = Path(scratch)
            comparison, reduced = root / "comparison", root / "reduced"
            write(comparison / "differences.tsv.gz", DIFFERENCES
                  + "P4-1\tP4\texec\tJ\t0061\t.\t0061*30\t0\tdefault\tlimit:steps\tnull\n"
                  + "P4-1\tP4\texec\tJ.tr-TR\t0061\t.\t0061*30\t0\tdefault\tlimit:steps\tnull\n"
                  + "P4-4\tP4\texec\tJ\t0064\t.\t0064\t0\tdefault\tnull\tmatch:0,1\n")
            write(comparison / "classified.tsv.gz", CLASSIFIED
                  + "P4-1\tP4\texec\tJ\tlimit:steps\tnull\tarbiter:exhausted\tc\t-\tw\n"
                  + "P4-1\tP4\texec\tJ.tr-TR\tlimit:steps\tnull\tarbiter:exhausted\tc\t-\tw\n"
                  + "P4-4\tP4\texec\tJ\tnull\tmatch:0,1\tnull\tc\t-\tw\n")
            write(reduced / "reductions.tsv", "# id\tclass\tlength\tlastIndex\tprefix\tarbiter\tprobes\n"
                  + "P4-1\tP4\t30\t0\t20\tnull\t5\n")
            manifest = reduced / "population" / "manifest.json"
            manifest.parent.mkdir(parents=True)
            manifest.write_text(json.dumps({"inputs": 1, "differences": 2}), encoding="utf-8")
            args = ["check", "--comparison", str(comparison), "--reduced", str(reduced)]
            self.assertEqual(0, R.main(args))
            manifest.write_text(json.dumps({"inputs": 1, "differences": 3}), encoding="utf-8")
            self.assertEqual(1, R.main(args))
            manifest.write_text(json.dumps({"inputs": 1, "differences": 2}), encoding="utf-8")
            write(reduced / "reductions.tsv", "# id\tclass\tlength\tlastIndex\tprefix\tarbiter\tprobes\n"
                  + "P4-2\tP4\t30\t0\t20\tnull\t5\n")
            self.assertEqual(1, R.main(args))


if __name__ == "__main__":
    unittest.main()
