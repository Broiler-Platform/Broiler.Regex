# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""run.sh's reading of the classifier's exit, over each path: run.sh is copied into a scratch repository beside a
stand-in classify.py that exits as the test says and writes classification.json or not, and is run with
--classify-only. Written 2026-11-02, at task 2.13's second review: run.sh read an exception's exit 1 as "a class (c)
difference" and ended with 0 having written no classification."""

from __future__ import annotations

import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parents[1]

STAND_IN = '''import os, sys
from pathlib import Path
args = sys.argv[1:]
out = Path(args[args.index("--comparison") + 1])
if os.environ["WRITE"] == "1":
    (out / "classification.json").write_text("{}\\n", encoding="utf-8")
if os.environ["RAISE"] == "1":
    raise RuntimeError("the stand-in fails")
sys.exit(int(os.environ["STATUS"]))
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
        subprocess.run(["git", "init", "-q", str(repo)], check=True)
        self.run_sh = scripts / "run.sh"
        self.out = self.tmp / "out"
        (self.out / "comparison").mkdir(parents=True)

    def run_classification(self, status: int, write: bool, fail: bool = False, stale: bool = False):
        if stale:
            (self.out / "comparison" / "classification.json").write_text("{}\n", encoding="utf-8")
        env = {"PATH": "/usr/bin:/bin", "STATUS": str(status), "WRITE": "1" if write else "0",
               "RAISE": "1" if fail else "0"}
        return subprocess.run(["bash", str(self.run_sh), str(self.out), "--broiler-js", "js", "--commit", "c",
                               "--correspondence", "c.md", "--unicode", "u", "--classify-only"],
                              capture_output=True, text=True, env=env, check=False)

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


if __name__ == "__main__":
    unittest.main()
