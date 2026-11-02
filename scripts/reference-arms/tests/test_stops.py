# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""The stop list a calibration makes (stops.py), from hand-made timing files. Written 2026-11-02, at task 2.13's
second review."""

from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parents[1]
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import stops as S  # noqa: E402


class StopsTests(unittest.TestCase):
    def test_every_stop_and_no_slow_case_is_listed_by_run(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            runs = Path(scratch) / "runs"
            (runs / "timing").mkdir(parents=True)
            header = "# id\tevent\tprocessor-seconds\twall-seconds\n"
            (runs / "timing" / "P4.J.tr-TR.tsv").write_text(
                header + "P4-1\tstop:processor-time\t5.0\t6.0\nP4-2\tslow\t-\t1.5\nP4-0\tstop:exit-134\t1.0\t1.0\n",
                encoding="utf-8")
            (runs / "timing" / "P3.J.tsv").write_text(header + "P3-F02\tstop:processor-time-after-a-stop\t2\t3\n",
                                                      encoding="utf-8")
            (runs / "timing" / "P3.S.tsv").write_text(header, encoding="utf-8")
            self.assertEqual(S.stopped_cases(runs), [("J", "P3-F02"), ("J.tr-TR", "P4-0"), ("J.tr-TR", "P4-1")])
            out = Path(scratch) / "stops.tsv"
            S.write(S.stopped_cases(runs), "abc", out)
            text = out.read_text(encoding="utf-8")
            self.assertIn("# cases per run: J 1, J.tr-TR 2", text)
            self.assertTrue(text.endswith("J\tP3-F02\nJ.tr-TR\tP4-0\nJ.tr-TR\tP4-1\n"))
            self.assertNotIn("\r", text)


if __name__ == "__main__":
    unittest.main()
