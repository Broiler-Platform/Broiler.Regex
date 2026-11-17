# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""run.sh's reading of the classifier's exit, over each path: run.sh is copied into a scratch repository beside a
stand-in classify.py that exits as the test says and writes classification.json or not, and is run with
--classify-only. Written 2026-11-02, at task 2.13's second review: run.sh read an exception's exit 1 as "a class (c)
difference" and ended with 0 having written no classification.

Added 2026-11-17, with run.sh's reduction and its classification under the rule's revisions: the stand-in writes into
--out where it is given (each classification's own directory), a stand-in reduce.py answers `check` as the test says,
and OUT holds an earlier run's reduction, so --classify-only reads it and builds nothing. The new tests read the three
classifications' arguments, a reduction of another comparison, and the controls a partial run does not take."""

from __future__ import annotations

import json
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parents[1]

STAND_IN = '''import json, os, sys
from pathlib import Path
args = sys.argv[1:]
with open(os.environ["CALLS"], "a", encoding="utf-8") as calls:
    calls.write(json.dumps(args) + "\\n")
out = Path(args[args.index("--out" if "--out" in args else "--comparison") + 1])
if os.environ["WRITE"] == "1":
    (out / "classification.json").write_text("{}\\n", encoding="utf-8")
if os.environ["RAISE"] == "1":
    raise RuntimeError("the stand-in fails")
sys.exit(int(os.environ["STATUS"]))
'''

REDUCE_STAND_IN = '''import os, sys
sys.exit(int(os.environ["CHECK"]) if sys.argv[1] == "check" else 0)
'''


@unittest.skipUnless(shutil.which("bash") and shutil.which("git"), "bash and git run run.sh")
class RunShClassifierTests(unittest.TestCase):
    def setUp(self) -> None:
        self.tmp = Path(tempfile.mkdtemp(prefix="run-sh-"))
        self.addCleanup(shutil.rmtree, self.tmp, ignore_errors=True)
        repo = self.tmp / "repo"
        scripts = repo / "scripts" / "reference-arms"
        scripts.mkdir(parents=True)
        shutil.copy(HERE / "run.sh", scripts / "run.sh")
        (scripts / "classify.py").write_text(STAND_IN, encoding="utf-8")
        (scripts / "reduce.py").write_text(REDUCE_STAND_IN, encoding="utf-8")
        subprocess.run(["git", "init", "-q", str(repo)], check=True)
        self.run_sh = scripts / "run.sh"
        self.out = self.tmp / "out"
        (self.out / "comparison").mkdir(parents=True)
        (self.out / "reduction").mkdir(parents=True)
        (self.out / "reduction" / "reductions.tsv").write_text("# id\n", encoding="utf-8")
        self.calls = self.tmp / "calls.jsonl"

    def run_classification(self, status: int, write: bool, fail: bool = False, stale: bool = False, check: int = 0,
                           only: str = "--classify-only"):
        if stale:
            (self.out / "comparison" / "classification.json").write_text("{}\n", encoding="utf-8")
        env = {"PATH": "/usr/bin:/bin", "STATUS": str(status), "WRITE": "1" if write else "0",
               "RAISE": "1" if fail else "0", "CHECK": str(check), "CALLS": str(self.calls)}
        return subprocess.run(["bash", str(self.run_sh), str(self.out), "--broiler-js", "js", "--commit", "c",
                               "--correspondence", "c.md", "--unicode", "u", only],
                              capture_output=True, text=True, env=env, check=False)

    def arguments(self) -> list[list[str]]:
        with open(self.calls, encoding="utf-8") as calls:
            return [json.loads(line) for line in calls]

    def test_no_class_c_passes(self) -> None:
        result = self.run_classification(0, write=True)
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_class_c_passes_and_says_so(self) -> None:
        result = self.run_classification(1, write=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("class (c) differences", result.stderr)

    def test_a_refusal_fails(self) -> None:
        result = self.run_classification(2, write=False)
        self.assertEqual(result.returncode, 1)
        self.assertIn("refused (exit 2)", result.stderr)

    def test_a_failure_fails(self) -> None:
        result = self.run_classification(3, write=False)
        self.assertEqual(result.returncode, 1)
        self.assertIn("failed (exit 3)", result.stderr)

    def test_an_uncaught_exception_fails_even_with_exit_1(self) -> None:
        # Python's exit for an uncaught exception is 1, the classifier's "class (c)": the missing file decides.
        result = self.run_classification(0, write=False, fail=True)
        self.assertEqual(result.returncode, 1)
        self.assertIn("wrote no", result.stderr)

    def test_a_missing_classification_fails_and_a_stale_one_does_not_count(self) -> None:
        for status in (0, 1):
            result = self.run_classification(status, write=False, stale=True)
            self.assertEqual(result.returncode, 1, status)
            self.assertIn("wrote no", result.stderr)
            self.assertFalse((self.out / "comparison" / "classification.json").exists())

    def test_the_classification_as_filed_then_the_reduction_s_then_the_revised_one(self) -> None:
        result = self.run_classification(0, write=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        filed, reduced, revised = self.arguments()
        value = lambda args, name: args[args.index(name) + 1]  # noqa: E731
        self.assertEqual(value(filed, "--out"), str(self.out / "comparison"))
        self.assertNotIn("--reduction", filed)
        self.assertNotIn("--exclude", filed)
        self.assertEqual(value(reduced, "--comparison"), str(self.out / "reduction" / "comparison"))
        self.assertEqual(value(reduced, "--exclude"), "scripts/reference-arms/p4-excluded.tsv")
        self.assertEqual(value(revised, "--comparison"), str(self.out / "comparison"))
        self.assertEqual(value(revised, "--out"), str(self.out / "classification"))
        self.assertEqual(value(revised, "--reduction"), str(self.out / "reduction"))
        self.assertEqual(value(revised, "--exclude"), "scripts/reference-arms/p4-excluded.tsv")
        for args in (filed, reduced, revised):
            self.assertEqual(value(args, "--cache"), str(self.out / "arbiter-cache.jsonl.gz"))
        self.assertTrue((self.out / "classification" / "classification.json").exists())

    def test_class_c_is_said_of_the_revised_classification(self) -> None:
        result = self.run_classification(1, write=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn(str(self.out / "classification" / "classification.json"), result.stderr)

    def test_a_reduction_of_another_comparison_fails(self) -> None:
        result = self.run_classification(0, write=True, check=1)
        self.assertEqual(result.returncode, 1)
        self.assertIn("step reduction-check failed", result.stderr)
        self.assertEqual(len(self.arguments()), 1)

    def test_the_controls_need_a_classification(self) -> None:
        result = self.run_classification(0, write=True, only="--controls-only")
        self.assertEqual(result.returncode, 1)
        self.assertIn("no classification", result.stderr)

    def test_a_control_whose_class_the_run_lacks_is_not_taken(self) -> None:
        (self.out / "classification").mkdir()
        (self.out / "classification" / "classification.json").write_text("{}\n", encoding="utf-8")
        result = self.run_classification(0, write=True, only="--controls-only")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("control V not taken: the run holds no P4 of arm V", result.stderr)
        self.assertIn("control J not taken: the run holds no P1 of arm J", result.stderr)
        self.assertFalse(self.calls.exists())
        # Added 2026-11-17, at review: the last line names the controls taken, so a reading need not look for them.
        self.assertTrue(result.stdout.rstrip("\n").endswith("(negative controls taken: none)"), result.stdout)


if __name__ == "__main__":
    unittest.main()
