# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""The negative controls' script (controls.py), from hand-made runs and classifications. Written 2026-11-17, with the
controls' step of run.sh; the arms' copies are built and run over a real population only."""

from __future__ import annotations

import json
import shutil
import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parents[1]
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import controls as K  # noqa: E402
from population_format import open_text  # noqa: E402

PROGRAM = """internal static class Program
{
    private static int Main(string[] args)
    {
        // NEGATIVE-CONTROL: a control copy of this arm sets Harness.ControlCase here, and nothing else changes.
        return Harness.Run(args, new ArmV());
    }
}
"""
DIFFERENCES = "# id\tclass\top\trun\tpattern\tflags\tsubject\tlastIndex\tpolicy\tS\tarm\n"
CLASSIFIED = "# id\tclass\top\trun\tS\tarm\tarbiter\tcategory\trow\twhy\n"
REVERT_DIFFERENCE = "P4-2\tP4\texec\tV\t0061\t.\t0061\t0\tdefault\tnull\tmatch:0,1\n"
PLANTED_DIFFERENCE = "P4-1\tP4\texec\tV\t0061\t.\t0061\t0\tdefault\tmatch:0,0\tmatch:0,1\n"
REVERT_CLASSIFIED = "P4-2\tP4\texec\tV\tnull\tmatch:0,1\tnull\ta\tM4\tw\n"


def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with open_text(path, "wt") as handle:
        handle.write(text)


class PlantTests(unittest.TestCase):
    def test_one_line_is_added_after_the_comment(self) -> None:
        planted = K.plant(PROGRAM, "P4-01234-u-05").split("\n")
        original = PROGRAM.split("\n")
        self.assertEqual(len(planted), len(original) + 1)
        self.assertEqual(planted[5], '        Harness.ControlCase = "P4-01234-u-05";')
        self.assertEqual(planted[:5] + planted[6:], original)

    def test_refusals(self) -> None:
        with self.assertRaises(SystemExit):
            K.plant(PROGRAM.replace("// NEGATIVE-CONTROL:", "//"), "x")
        with self.assertRaises(SystemExit):
            K.plant(PROGRAM + "        // NEGATIVE-CONTROL: twice\n", "x")
        for case in ('a"b', "a\\b", ""):
            with self.assertRaises(SystemExit):
                K.plant(PROGRAM, case)

    def test_the_filed_arms_hold_the_comment_once(self) -> None:
        arms = HERE.parents[1] / "tests" / "ReferenceArms"
        for arm in ("V", "J"):
            text = (arms / arm / "Program.cs").read_text(encoding="utf-8")
            self.assertEqual(len(K.plant(text, "P1-x").split("\n")), len(text.split("\n")) + 1, arm)

    def test_copy_takes_the_arm_and_what_it_builds_from_and_leaves_bin_and_obj(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            root, out = Path(scratch) / "repo", Path(scratch) / "copy"
            for name in K.ROOT_FILES + ("eng/a.props", "tests/ReferenceArms/Common/Harness.cs",
                                        "tests/ReferenceArms/V/bin/x.dll", "tests/ReferenceArms/V/obj/y",
                                        "tests/ReferenceArms/J/Program.cs"):
                (root / name).parent.mkdir(parents=True, exist_ok=True)
                (root / name).write_text("x", encoding="utf-8")
            (root / "tests/ReferenceArms/V/Program.cs").write_text(PROGRAM, encoding="utf-8")
            self.assertEqual(0, K.main(["copy", "--root", str(root), "--arm", "V", "--case", "P4-1", "--out", str(out)]))
            self.assertTrue((out / "eng" / "a.props").exists())
            self.assertTrue((out / "tests/ReferenceArms/Common/Harness.cs").exists())
            self.assertFalse((out / "tests/ReferenceArms/V/bin").exists())
            self.assertFalse((out / "tests/ReferenceArms/V/obj").exists())
            self.assertFalse((out / "tests/ReferenceArms/J").exists())
            self.assertIn('Harness.ControlCase = "P4-1";', (out / "tests/ReferenceArms/V/Program.cs").read_text())


class CheckTests(unittest.TestCase):
    def setUp(self) -> None:
        self.root = Path(tempfile.mkdtemp(prefix="controls-"))
        self.addCleanup(shutil.rmtree, self.root, ignore_errors=True)
        runs, control = self.root / "runs", self.root / "control-copy"
        answers = "# id\tanswer\tcharge\nP4-1\tmatch:0,0\t1\nP4-2\tmatch:0,1\t1\n"
        write(runs / "P4.S.tsv.gz", "# id\tanswer\tcharge\nP4-1\tmatch:0,0\t1\nP4-2\tnull\t1\n")
        write(runs / "P4.V.tsv.gz", answers)
        write(runs / "P4.J.tsv.gz", answers)
        write(runs / "P1.V.tsv.gz", answers)
        (runs / "run.V.json").write_text('{"control-case": null}\n', encoding="utf-8")
        (runs / "run.S.json").write_text('{"control-case": null}\n', encoding="utf-8")
        write(control / "P4.V.tsv.gz", answers.replace("P4-1\tmatch:0,0", "P4-1\tmatch:0,1"))
        (control / "run.V.json").write_text('{"control-case": "P4-1"}\n', encoding="utf-8")
        self.dir = self.root / "dir"
        self.assertEqual(0, K.main(["assemble", "--runs", str(runs), "--control", str(control), "--arm", "V",
                                    "--class", "P4", "--out", str(self.dir)]))

    def classify(self, name: str, differences: str, classified: str, controls: dict) -> None:
        write(self.dir / name / "differences.tsv.gz", DIFFERENCES + differences)
        write(self.dir / name / "classified.tsv.gz", CLASSIFIED + classified)
        counts = {"a": 1}
        (self.dir / name / "classification.json").write_text(json.dumps({"controls": controls, "counts": counts}),
                                                            encoding="utf-8")

    def check(self, planted_class: str = "unclassified", extra: str = "") -> tuple[int, dict]:
        planted = f"P4-1\tP4\texec\tV\tmatch:0,0\tmatch:0,1\t-\t{planted_class}\t-\tw\n"
        self.classify("control", REVERT_DIFFERENCE + PLANTED_DIFFERENCE, REVERT_CLASSIFIED + planted + extra,
                      {"V": "P4-1"})
        self.classify("revert", REVERT_DIFFERENCE, REVERT_CLASSIFIED, {})
        self.classify("blind", REVERT_DIFFERENCE + PLANTED_DIFFERENCE,
                      REVERT_CLASSIFIED + "P4-1\tP4\texec\tV\tmatch:0,0\tmatch:0,1\tmatch:0,0\tc\t-\tw\n", {})
        status = K.main(["check", "--dir", str(self.dir), "--arm", "V", "--class", "P4", "--case", "P4-1"])
        return status, json.loads((self.dir / "control.json").read_text(encoding="utf-8"))

    def test_assemble_swaps_the_arm_s_answers_and_run_file_only(self) -> None:
        control, revert = self.dir / "control-runs", self.dir / "revert-runs"
        self.assertEqual(sorted(p.name for p in control.iterdir()), sorted(p.name for p in revert.iterdir()))
        self.assertEqual(sorted(p.name for p in control.iterdir()),
                         ["P4.J.tsv.gz", "P4.S.tsv.gz", "P4.V.tsv.gz", "run.S.json", "run.V.json"])
        for name in ("P4.V.tsv.gz", "run.V.json"):
            self.assertEqual((control / name).resolve(), (self.root / "control-copy" / name).resolve())
            self.assertEqual((revert / name).resolve(), (self.root / "runs" / name).resolve())
        self.assertEqual((control / "P4.J.tsv.gz").resolve(), (self.root / "runs" / "P4.J.tsv.gz").resolve())

    def test_a_control_reported_unclassified_passes(self) -> None:
        status, found = self.check()
        self.assertEqual(0, status, found["problems"])
        self.assertTrue(found["reported-unclassified"])
        self.assertEqual([{"class": "c", "row": "-", "why": "w"}], found["blind"])

    def test_a_control_classified_fails(self) -> None:
        status, found = self.check(planted_class="c")
        self.assertEqual(1, status)
        self.assertIn("not unclassified", " ".join(found["problems"]))

    def test_another_line_changed_fails(self) -> None:
        status, found = self.check(extra="P4-3\tP4\texec\tV\tnull\tmatch:0,1\tnull\tc\t-\tw\n")
        self.assertEqual(1, status)
        self.assertIn("more than the case's line", " ".join(found["problems"]))

    def test_a_control_not_reported_fails(self) -> None:
        planted = "P4-1\tP4\texec\tV\tmatch:0,0\tmatch:0,1\t-\tunclassified\t-\tw\n"
        self.classify("control", REVERT_DIFFERENCE, REVERT_CLASSIFIED + planted, {"V": "P4-1"})
        self.classify("revert", REVERT_DIFFERENCE, REVERT_CLASSIFIED, {})
        self.classify("blind", REVERT_DIFFERENCE, REVERT_CLASSIFIED, {})
        status = K.main(["check", "--dir", str(self.dir), "--arm", "V", "--class", "P4", "--case", "P4-1"])
        found = json.loads((self.dir / "control.json").read_text(encoding="utf-8"))
        self.assertEqual(1, status)
        self.assertIn("holds 0 differences of the case", " ".join(found["problems"]))

    def test_a_copy_that_changed_another_answer_fails(self) -> None:
        write(self.root / "control-copy" / "P4.V.tsv.gz",
              "# id\tanswer\tcharge\nP4-1\tmatch:0,1\t1\nP4-2\tnull\t1\n")
        status, found = self.check()
        self.assertEqual(1, status)
        self.assertIn("not in the case's alone", " ".join(found["problems"]))


if __name__ == "__main__":
    unittest.main()
