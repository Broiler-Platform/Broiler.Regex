# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""The classifier's decisions over hand-made differences, with the arbiter's answer given; and its reading of the
correspondence. Written with the classifier, before any judged run."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parents[1]
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import classify as C  # noqa: E402
from population_format import encode  # noqa: E402


def diff(run, op, pattern, flags, s, arm, subject="", last=0):
    return {"id": "x", "class": "P4", "op": op, "run": run, "pattern": encode(pattern), "flags": flags or ".",
            "subject": encode(subject) if op == "exec" else "-", "lastIndex": str(last) if op == "exec" else "-",
            "policy": "default", "S": s, "arm": arm}


MATCH01 = ("match", ((0, 1),), ())


class ClassifyTests(unittest.TestCase):
    def test_a_v_duplicate_name_refusal_is_row_s1(self) -> None:
        d = diff("V", "accept", "(?<a>x)|(?<a>y)", "", "accept", "refuse:syntax")
        self.assertEqual(C.classify(d, "accept", set())[:2], ("a", "S1"))

    def test_a_duplicate_spelled_with_an_escape_is_row_s1(self) -> None:
        d = diff("V", "accept", "(?<\\u0061>x)|(?<a>y)", "", "accept", "refuse:syntax")
        self.assertEqual(C.classify(d, "accept", set())[:2], ("a", "S1"))
        d = diff("V", "accept", "(?<\\u{62}>x)|(?<a>y)", "", "accept", "refuse:syntax")
        self.assertEqual(C.classify(d, "accept", set())[0], "c")

    def test_s_wrong_is_c(self) -> None:
        d = diff("J", "exec", "a", "", "null", "match:0,1", "a")
        self.assertEqual(C.classify(d, MATCH01, set())[0], "c")

    def test_an_arm_wrong_with_no_row_is_c(self) -> None:
        d = diff("V", "exec", "a", "", "match:0,1", "null", "a")
        self.assertEqual(C.classify(d, MATCH01, set())[:2], ("c", "-"))

    def test_a_nesting_refusal_is_b(self) -> None:
        d = diff("J", "accept", "(" * 600 + ")" * 600, "", "refuse:nesting", "accept")
        self.assertEqual(C.classify(d, "accept", set())[:2], ("b", "S3"))

    def test_a_limit_against_the_arbiter_s_answer_is_b(self) -> None:
        d = diff("V", "exec", "a", "", "match:0,1", "limit:ceiling", "a")
        self.assertEqual(C.classify(d, MATCH01, set())[:2], ("b", "B6"))

    def test_two_limits_are_b(self) -> None:
        d = diff("V", "exec", "(a+)+b", "", "limit:steps", "limit:ceiling", "a" * 40)
        self.assertEqual(C.classify(d, "arbiter:exhausted", set())[:2], ("b", "B6"))

    def test_the_empty_match_inside_a_pair_is_d41(self) -> None:
        d = diff("V", "exec", "(?:)", "gu", "match:0,0", "match:1,1", "\U0001d306", 1)
        self.assertEqual(C.classify(d, "arbiter:no-answer:SpecAssertion", set())[:2], ("b", "M4"))

    def test_an_unreachable_arbiter_is_c(self) -> None:
        d = diff("J", "exec", "(a+)+b", "", "limit:steps", "null", "a" * 40)
        self.assertEqual(C.classify(d, "arbiter:exhausted", set())[0], "b")
        d = diff("J", "exec", "(a+)+b", "", "match:0,1", "null", "a" * 40)
        self.assertEqual(C.classify(d, "arbiter:exhausted", set())[0], "c")

    def test_the_data_decision(self) -> None:
        d = diff("J", "exec", "꟎", "i", "match:0,1", "null", "꟏")
        self.assertEqual(C.classify(d, MATCH01, C.data_partners(Path(".")))[:2], ("a'", "U3"))

    def test_the_canonical_form_reads_names_by_their_capture(self) -> None:
        self.assertEqual(C.canonical("match:0,1;-;0,1|a=2"), ("match", ((0, 1), None, (0, 1)), (("a", (0, 1)),)))
        self.assertEqual(C.canonical("match:0,1;-;0,1|a=-"), ("match", ((0, 1), None, (0, 1)), (("a", None),)))


class CorrespondenceTests(unittest.TestCase):
    def test_a_missing_row_refuses(self) -> None:
        self.assertTrue(any("S1" in p for p in C.check_rows({})))

    def test_the_rows_as_their_classes_name_them(self) -> None:
        rows = {"S1": "**Correction of Broiler.VM** and of Broiler.Regex's preview.4", "S2a": "Correction of Broiler.JS",
                "S2b": "Correction of Broiler.JS", "S5": "correction of Broiler.VM (kind R); preview.4",
                "S7": "Correction of Broiler.JS", "S8": "Correction of Broiler.JS", "S10": "Correction of Broiler.VM",
                "M2a": "Correction of Broiler.JS", "M2": "Correction of Broiler.JS", "M3": "Correction of Broiler.JS",
                "M4": "correction of Broiler.VM ... correction of Broiler.JS", "M7": "Correction of Broiler.VM",
                "B2": "Correction of Broiler.JS and of Broiler.Regex's preview.4", "S3": "policy", "B6": "policy",
                "B7": "policy", "U3": "Correction of Broiler.JS by data decision"}
        self.assertEqual(C.check_rows(rows), [])
        rows["S10"] = "Same in both"
        self.assertTrue(C.check_rows(rows))


if __name__ == "__main__":
    unittest.main()
