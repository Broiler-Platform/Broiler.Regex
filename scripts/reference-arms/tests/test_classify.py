# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""The classifier's decisions over hand-made differences, with the arbiter's answer given; and its reading of the
correspondence. Written with the classifier, before any judged run."""

from __future__ import annotations

import os
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
KELVIN = C.Context(case_pairs=frozenset((0x212A, 0x017F)))


def with_code(code):
    return C.Context(refusal_code=lambda d: code)


class ClassifyTests(unittest.TestCase):
    def test_a_v_duplicate_name_refusal_is_row_s1(self) -> None:
        d = diff("V", "accept", "(?<a>x)|(?<a>y)", "", "accept", "refuse:syntax")
        self.assertEqual(C.classify(d, "accept", set())[:2], ("a", "S1"))

    def test_a_duplicate_spelled_with_an_escape_is_row_s1(self) -> None:
        d = diff("V", "accept", "(?<\\u0061>x)|(?<a>y)", "", "accept", "refuse:syntax")
        self.assertEqual(C.classify(d, "accept", set())[:2], ("a", "S1"))
        d = diff("V", "accept", "(?<\\u{62}>x)|(?<a>y)", "", "accept", "refuse:syntax")
        self.assertEqual(C.classify(d, "accept", set())[0], "c")

    def test_a_quantified_look_behind_j_accepts_is_row_s7(self) -> None:
        nothing = with_code("grammar:nothing-to-repeat")
        d = diff("J", "accept", "(?<=a)*b", "", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), nothing)[:2], ("a", "S7"))
        d = diff("J", "exec", "(?<=a)*b", "", "refuse:syntax", "match:0,1", "b")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), nothing)[:2], ("a", "S7"))
        d = diff("J", "accept", "(?<=a)b*", "", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), nothing)[0], "c")
        # A quantified look-behind in a pattern the arbiter refuses for another reason: not the row's correction.
        d = diff("J", "accept", "(?<=a)*\\k<b>", "", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), with_code("early:undefined-group-name"))[0], "c")

    def test_row_s7_holds_nothing_of_j_under_u(self) -> None:
        # The row reads Broiler.JS under u "the same, every refusal early".
        for pattern in ("(?<=a)*b", "(?i:a)", "\\u{110000}"):
            d = diff("J", "accept", pattern, "u", "refuse:syntax", "accept")
            self.assertEqual(C.classify(d, "refuse:syntax", set(), with_code("grammar:nothing-to-repeat"))[0], "c")

    def test_a_j_refusal_is_no_row_s(self) -> None:
        # J refusing what S and the arbiter accept is not a correction any of these rows records.
        for pattern, flags in (("{", "i"), ("a$", ""), ("[a&&b]", "v"), ("(?<a>x)|(?<a>y)", "")):
            d = diff("J", "accept", pattern, flags, "accept", "refuse:syntax")
            self.assertEqual(C.classify(d, "accept", set(), KELVIN)[0], "c", pattern)
            d = diff("J", "exec", pattern, flags, "null", "refuse:syntax", "k\n")
            self.assertEqual(C.classify(d, "null", set(), KELVIN)[0], "c", pattern)

    def test_row_s1_is_not_a_correction_of_j(self) -> None:
        d = diff("J", "accept", "(?<a>x)|(?<a>y)", "", "accept", "refuse:syntax")
        self.assertEqual(C.classify(d, "accept", set())[0], "c")

    def test_row_s2a_is_j_accepting_an_escaped_duplicate(self) -> None:
        duplicate = with_code("early:duplicate-group-name")
        d = diff("J", "accept", "(?<\\u0061>x)(?<a>y)", "", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), duplicate)[:2], ("a", "S2a"))
        d = diff("J", "accept", "(?<a>x)(?<a>y)", "", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), duplicate)[0], "c")
        d = diff("J", "accept", "(?<\\u0061>x)(?<a>y){,2}", "v", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), with_code("grammar:nothing-to-repeat"))[0], "c")

    def test_row_s8_is_v_mode_syntax_j_accepts(self) -> None:
        d = diff("J", "accept", "[(]", "v", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), with_code("grammar:class-set-syntax-character"))[:2],
                         ("a", "S8"))
        d = diff("J", "accept", "[a!!b]", "v", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set(),
                                    with_code("grammar:class-set-reserved-double-punctuator"))[:2], ("a", "S8"))
        d = diff("J", "accept", "a{2,1}", "v", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), with_code("early:quantifier-range"))[0], "c")
        d = diff("J", "exec", "[\\w--k]", "v", "match:0,1", "null", "a")
        self.assertEqual(C.classify(d, MATCH01, set(), with_code(None))[0], "c")

    def test_row_m2_is_a_case_pair_net_holds(self) -> None:
        d = diff("J", "exec", "[a-z]", "i", "null", "match:0,1", "\u212a")
        self.assertEqual(C.classify(d, "null", set(), KELVIN)[:2], ("a", "M2"))
        # The modifier, not only the flag: P1's matching:modifiers:0009.
        d = diff("J", "exec", "(?i:\\u212a)", "", "null", "match:0,1", "k")
        self.assertEqual(C.classify(d, "null", set(), KELVIN)[:2], ("a", "M2"))
        d = diff("J", "exec", "\\b", "i", "null", "match:0,1", "\u00e9")
        self.assertEqual(C.classify(d, "null", set(), KELVIN)[0], "c")
        d = diff("J", "exec", "[a-z]", "iu", "null", "match:0,1", "\u212a")
        self.assertEqual(C.classify(d, "null", set(), KELVIN)[0], "c")

    def test_row_m2a_is_the_culture_changing_the_answer(self) -> None:
        d = diff("J.tr-TR", "exec", "i", "i", "null", "match:0,1", "\u0130")
        self.assertEqual(C.classify(d, "null", set())[:2], ("a", "M2a"))
        d["id"] = "same-under-invariant"
        same = C.Context(invariant={"same-under-invariant": "match:0,1"})
        self.assertEqual(C.classify(d, "null", set(), same)[0], "c")

    def test_row_m3_reads_both_of_its_halves(self) -> None:
        # P1's matching:m3-end-of-input:0001, 0007, 0008: `$` without m before a final LF.
        for pattern, s, arm in (("a$", "null", "match:0,1"), ("[^]$", "match:1,2", "match:0,1"), ("$", "match:2,2",
                                                                                                       "match:1,1")):
            d = diff("J", "exec", pattern, "", s, arm, "a\n")
            self.assertEqual(C.classify(d, C.canonical(s), set())[:2], ("a", "M3"), pattern)
        d = diff("J", "exec", "^b", "m", "match:2,3", "null", "a\u2028b")
        self.assertEqual(C.classify(d, ("match", ((2, 3),), ()), set())[:2], ("a", "M3"))
        d = diff("J", "exec", "a.b", "", "null", "match:0,3", "a\rb")
        self.assertEqual(C.classify(d, "null", set())[0], "c")
        d = diff("J", "exec", "[$]", "", "null", "match:0,1", "$\n")
        self.assertEqual(C.classify(d, "null", set())[0], "c")

    def test_row_b2_is_a_later_start_after_exhaustion(self) -> None:
        d = diff("J", "exec", "(?:(a+)+b|a*c)", "u", "match:0,25;-", "match:4,25;-", "a" * 24 + "c")
        self.assertEqual(C.classify(d, ("match", ((0, 25), None), ()), set())[:2], ("a", "B2"))
        d = diff("J", "exec", "a|b", "u", "match:0,1", "null", "a")
        self.assertEqual(C.classify(d, MATCH01, set())[0], "c")

    def test_rows_m7_and_s10_name_j_too(self) -> None:
        d = diff("J", "exec", "(?<=\\1(.))b", "u", "null", "match:3,4;2,3", "\ud834\udc00\udc00b")
        self.assertEqual(C.classify(d, "null", set())[:2], ("a", "M7"))
        d = diff("J", "exec", "[\\uD83D\\u{DC38}]", "u", "match:0,1", "null", "\udc38")
        self.assertEqual(C.classify(d, MATCH01, set())[:2], ("a", "S10"))
        d = diff("J", "exec", "[\\uD83D\\u{3042}]", "u", "match:0,1", "null", "\udc38")
        self.assertEqual(C.classify(d, MATCH01, set())[0], "c")

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
        # The rule's arbiter clause: past its cut-off the arbiter has checked nothing, so a side's answer against a
        # stop is class (c) until the input is reduced; so is any other difference it cannot reach.
        d = diff("J", "exec", "(a+)+b", "", "limit:steps", "null", "a" * 40)
        self.assertEqual(C.classify(d, "arbiter:exhausted", set())[0], "c")
        d = diff("V", "exec", "(a+)+b", "", "null", "limit:steps", "a" * 40)
        self.assertEqual(C.classify(d, "arbiter:exhausted", set())[0], "c")
        d = diff("J", "exec", "(a+)+b", "", "match:0,1", "null", "a" * 40)
        self.assertEqual(C.classify(d, "arbiter:exhausted", set())[0], "c")
        d = diff("J", "exec", "(a+)+b", "", "limit:steps", "null", "a" * 40)
        self.assertEqual(C.classify(d, "null", set())[:2], ("b", "D10"))

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
                "S5": "VM: **correction of Broiler.VM** (kind R); preview.4",
                "S7": "Correction of Broiler.JS", "S8": "Correction of Broiler.JS",
                "S10": "Correction of Broiler.VM. The class case `[\\uD83D\\u{DC38}]`: **correction of Broiler.JS**",
                "M2a": "Correction of Broiler.JS", "M2b": "Correction of Broiler.JS", "M2": "Correction of Broiler.JS",
                "M3": "Correction of Broiler.JS",
                "M4": "correction of Broiler.VM ... The half-pair matches (`/\\udf06/gu`, `/yu`, the look-behind): "
                      "**correction of Broiler.JS**",
                "M7": "Correction of Broiler.VM. The look-behind: also a correction of Broiler.Regex's preview.4, and "
                      "so of Broiler.JS",
                "B2": "Correction of Broiler.JS and of Broiler.Regex's preview.4", "S3": "policy", "B6": "policy",
                "B7": "policy", "U3": "Correction of Broiler.JS by data decision"}
        self.assertEqual(C.check_rows(rows), [])
        rows["S10"] = "Same in both"
        self.assertTrue(C.check_rows(rows))

    def test_a_class_that_names_only_the_library_refuses(self) -> None:
        # Row S1's class names Broiler.VM and preview.4: it does not make a correction of Broiler.JS of it.
        self.assertFalse(any(row == "S1" and engine == "J" for row, engine, _, _ in C.ROWS))
        rows = {"M7": "Correction of Broiler.VM, and of Broiler.Regex's preview.4"}
        self.assertTrue(any("row M7" in p and "Broiler.JS" in p for p in C.check_rows(rows)))

    def test_the_filed_correspondence(self) -> None:
        # The classifier's words against the correspondence as filed, when a checkout is named.
        path = os.environ.get("BROILER_VM_CORRESPONDENCE")
        if not path:
            self.skipTest("BROILER_VM_CORRESPONDENCE names no correspondence.md")
        self.assertEqual(C.check_rows(C.read_correspondence(Path(path))), [])


if __name__ == "__main__":
    unittest.main()
