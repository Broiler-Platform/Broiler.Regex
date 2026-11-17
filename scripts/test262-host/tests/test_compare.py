# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""The host job's verdict (compare.py) over hand-made suites, lists and reports. Written 2026-11-17, at task 2.10 of
SJL-11.2: each way check B-1 can be broken turns the verdict red, and the run the job expects keeps it green."""

from __future__ import annotations

import io
import json
import sys
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path

HERE = Path(__file__).resolve().parents[1]
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import compare as C  # noqa: E402

REF = "ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e"
DUPLICATE = "/*---\nfeatures: [regexp-duplicate-named-groups]\n---*/\n"
PLAIN = "/*---\nfeatures: [Symbol.replace]\n---*/\n"
HEADER = "# The host's expected failures.\n"
REALM = "test/built-ins/RegExp/proto-from-ctor-realm.js"


class CompareTests(unittest.TestCase):
    def setUp(self) -> None:
        self.scratch = tempfile.TemporaryDirectory()
        self.root = Path(self.scratch.name)
        self.suite = self.root / "suite"
        files = {
            REALM: PLAIN,
            "test/built-ins/RegExp/named-groups/duplicate-names-replace.js": DUPLICATE,
            "test/built-ins/RegExp/named-groups/plain.js": PLAIN,
            "test/built-ins/RegExp/named-groups/module_FIXTURE.js": PLAIN,
            "test/staging/built-ins/RegExp/named-groups/duplicate-named-groups-replace.js":
                "/*---\nfeatures:\n  - Symbol.replace\n  - regexp-duplicate-named-groups\n---*/\n",
        }
        for path, text in files.items():
            (self.suite / path).parent.mkdir(parents=True, exist_ok=True)
            (self.suite / path).write_text(text, encoding="utf-8")
        self.population = ["test/built-ins/RegExp", "test/staging/built-ins/RegExp"]
        self.required = ["test/built-ins/RegExp/named-groups/duplicate-names-replace.js",
                         "test/staging/built-ins/RegExp/named-groups/duplicate-named-groups-replace.js"]
        self.expected = HEADER + f"{REALM}\tthe RegExp object\tthe constructor's prototype from another realm\n"
        self.rows = {
            REALM: "failed",
            "test/built-ins/RegExp/named-groups/duplicate-names-replace.js": "passed",
            "test/built-ins/RegExp/named-groups/plain.js": "passed",
            "test/staging/built-ins/RegExp/named-groups/duplicate-named-groups-replace.js": "passed",
        }

    def tearDown(self) -> None:
        self.scratch.cleanup()

    def run_compare(self, exit_code: int = 1, **report_changes: object) -> tuple[int, str]:
        report = {
            "suiteRef": REF, "includeNegative": True, "variants": ["original"], "minifier": "none",
            "requestedPaths": list(self.population),
            "results": [{"path": path, "status": status, "variant": "original"} for path, status in self.rows.items()],
        }
        report.update(report_changes)
        (self.root / "report.json").write_text(json.dumps(report), encoding="utf-8")
        (self.root / "expected.txt").write_text(self.expected, encoding="utf-8")
        argv = ["--report", str(self.root / "report.json"), "--exit", str(exit_code),
                "--expected", str(self.root / "expected.txt"), "--suite", str(self.suite), "--suite-ref", REF,
                "--population", *self.population, "--summary", str(self.root / "summary.txt")]
        for path in self.required:
            argv += ["--require", path]
        out = io.StringIO()
        with redirect_stdout(out):
            code = C.main(argv)
        return code, out.getvalue()

    def test_the_expected_run_is_green(self) -> None:
        code, out = self.run_compare()
        self.assertEqual(0, code, out)
        self.assertIn("test262-host: green; 4 files, 4 rows, 3 passed, 1 failing of 1 listed; 2 files list", out)
        self.assertEqual(out, (self.root / "summary.txt").read_text(encoding="utf-8"))

    def test_a_failure_the_list_does_not_name_is_red(self) -> None:
        self.rows["test/built-ins/RegExp/named-groups/plain.js"] = "failed"
        code, out = self.run_compare()
        self.assertEqual(1, code)
        self.assertIn("FAIL test/built-ins/RegExp/named-groups/plain.js: failed, and the expected-failures list", out)

    def test_a_timeout_the_list_does_not_name_is_red(self) -> None:
        self.rows["test/built-ins/RegExp/named-groups/plain.js"] = "timedOut"
        code, out = self.run_compare()
        self.assertEqual(1, code)
        self.assertIn("plain.js: timedOut", out)

    def test_a_listed_file_that_passes_is_red(self) -> None:
        self.rows[REALM] = "passed"
        code, out = self.run_compare(exit_code=0)
        self.assertEqual(1, code)
        self.assertIn(f"FAIL {REALM}: listed, but it does not fail or time out (passed)", out)

    def test_a_listed_path_outside_the_population_is_red(self) -> None:
        self.expected += "test/built-ins/Array/x.js\tlastIndex\tnone\n"
        code, out = self.run_compare()
        self.assertEqual(1, code)
        self.assertIn("FAIL test/built-ins/Array/x.js: listed, but not in the population", out)

    def test_a_failing_file_of_duplicate_named_groups_is_red_even_when_listed(self) -> None:
        path = "test/built-ins/RegExp/named-groups/duplicate-names-replace.js"
        self.rows[path] = "failed"
        self.expected += f"{path}\tGetSubstitution's evaluation\tthe replacement's groups\n"
        code, out = self.run_compare()
        self.assertEqual(1, code)
        self.assertIn(f"FAIL {path}: lists regexp-duplicate-named-groups, whose every row must pass, and is listed", out)
        self.assertIn(f"FAIL {path}: lists regexp-duplicate-named-groups, and its rows are ['failed'], not passed", out)

    def test_the_block_form_of_features_is_read(self) -> None:
        path = "test/staging/built-ins/RegExp/named-groups/duplicate-named-groups-replace.js"
        self.rows[path] = "timedOut"
        code, out = self.run_compare()
        self.assertEqual(1, code)
        self.assertIn(f"FAIL {path}: lists regexp-duplicate-named-groups, and its rows are ['timedOut']", out)

    def test_a_required_file_that_does_not_list_the_feature_or_is_missing_is_red(self) -> None:
        self.required += ["test/built-ins/RegExp/named-groups/plain.js", "test/built-ins/RegExp/gone.js"]
        code, out = self.run_compare()
        self.assertEqual(1, code)
        self.assertIn("plain.js: required as a file of regexp-duplicate-named-groups, but its front matter", out)
        self.assertIn("FAIL test/built-ins/RegExp/gone.js: required, but not in the population", out)

    def test_an_entry_must_name_one_of_the_host_members_and_why(self) -> None:
        self.expected = HEADER + f"{REALM}\tthe matcher\tbacktracking\n"
        code, out = self.run_compare()
        self.assertEqual(1, code)
        self.assertIn("the member 'the matcher' is not one of B-1's host members", out)
        self.expected = HEADER + f"{REALM}\tthe RegExp object\n"
        code, out = self.run_compare()
        self.assertEqual(1, code)
        self.assertIn("an entry is a path, a host member and why, tab-separated", out)
        self.expected = HEADER + f"{REALM}\tthe RegExp object\ta\n{REALM}\tthe RegExp object\tb\n"
        code, out = self.run_compare()
        self.assertEqual(1, code)
        self.assertIn("is listed twice", out)

    def test_a_dropped_skipped_or_extra_row_is_red(self) -> None:
        del self.rows["test/built-ins/RegExp/named-groups/plain.js"]
        code, out = self.run_compare()
        self.assertEqual(1, code)
        self.assertIn("FAIL test/built-ins/RegExp/named-groups/plain.js: 0 rows in the report, not one", out)
        self.rows["test/built-ins/RegExp/named-groups/plain.js"] = "skipped"
        code, out = self.run_compare()
        self.assertEqual(1, code)
        self.assertIn("plain.js: skipped by the runner", out)
        self.rows["test/built-ins/RegExp/named-groups/plain.js"] = "passed"
        self.rows["test/built-ins/RegExp/named-groups/module_FIXTURE.js"] = "passed"
        code, out = self.run_compare()
        self.assertEqual(1, code)
        self.assertIn("module_FIXTURE.js: a row outside the population", out)

    def test_the_run_must_be_the_one_asked_for(self) -> None:
        for change, text in (
            ({"suiteRef": "0" * 40}, "the run's suite commit"),
            ({"includeNegative": False}, "left negative tests out"),
            ({"variants": ["original", "terser"]}, "not the original variant alone"),
            ({"requestedPaths": ["test/built-ins/RegExp"]}, "are not the population"),
        ):
            code, out = self.run_compare(**change)
            self.assertEqual(1, code, change)
            self.assertIn(text, out)

    def test_the_runner_exit_must_agree_with_its_rows(self) -> None:
        code, out = self.run_compare(exit_code=0)
        self.assertEqual(1, code)
        self.assertIn("the runner exited 0, but the report holds a row that fails or times out", out)
        code, out = self.run_compare(exit_code=134)
        self.assertEqual(1, code)
        self.assertIn("the runner exited 134", out)

    def test_an_unreadable_input_is_a_usage_error(self) -> None:
        self.population.append("test/built-ins/RegExpStringIteratorPrototype")
        code, _ = self.run_compare()
        self.assertEqual(2, code)


if __name__ == "__main__":
    unittest.main()
