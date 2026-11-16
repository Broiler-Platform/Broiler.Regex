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
# Row M8's non-ASCII letters, as the arbiter's UCD gives them (Context.letters), for the code points these tests use.
LETTERS = frozenset((0x00E9, 0x0130, 0x0131, 0x03A3, 0x03C2, 0x03C3, 0x1D400))
WITH_LETTERS = C.Context(letters=LETTERS)


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
        # Not row S7's; since @4 the reason is row S12's.
        self.assertEqual(C.classify(d, "refuse:syntax", set(), nothing)[:2], ("a", "S12"))
        # A quantified look-behind in a pattern the arbiter refuses for another reason: not the row's correction.
        d = diff("J", "accept", "(?<=a)*\\k<b>", "", "refuse:syntax", "accept")
        self.assertNotEqual(C.classify(d, "refuse:syntax", set(), with_code("early:undefined-group-name"))[1], "S7")
        # ... and a reason row S13 names, which holds it since @4.
        self.assertEqual(C.classify(d, "refuse:syntax", set(), with_code("early:undefined-group-name"))[:2],
                         ("a", "S13"))
        self.assertEqual(C.classify(d, "refuse:syntax", set(), with_code("early:quantifier-range"))[0], "c")

    def test_row_s7_holds_nothing_of_j_under_u(self) -> None:
        # The row reads Broiler.JS under u "the same, every refusal early". Since @4 row S12 holds the arms' P4
        # inputs of the reason nothing to repeat, "the constructs of rows S7's and S8's subjects" among them.
        for pattern in ("(?<=a)*b", "(?i:a)", "\\u{110000}"):
            d = diff("J", "accept", pattern, "u", "refuse:syntax", "accept")
            self.assertEqual(C.classify(d, "refuse:syntax", set(), with_code("grammar:nothing-to-repeat"))[:2],
                             ("a", "S12"))
            d["class"] = "P1"
            self.assertNotEqual(C.classify(d, "refuse:syntax", set(), with_code("grammar:nothing-to-repeat"))[1],
                                "S7")

    def test_a_j_refusal_is_no_row_s(self) -> None:
        # J refusing what S and the arbiter accept under u or v is not a correction any of these rows records (row
        # S14, added at @4, holds J's refusals without u or v; row S15 a bound past 2^31 - 1 under i).
        for pattern, flags in (("{", "u"), ("a$", "v"), ("[a&&b]", "v"), ("(?<a>x)|(?<a>y)", "u")):
            d = diff("J", "accept", pattern, flags, "accept", "refuse:syntax")
            self.assertEqual(C.classify(d, "accept", set(), KELVIN)[0], "c", pattern)
            d = diff("J", "exec", pattern, flags, "null", "refuse:syntax", "k\n")
            self.assertEqual(C.classify(d, "null", set(), KELVIN)[0], "c", pattern)

    def test_row_s1_is_not_a_correction_of_j(self) -> None:
        d = diff("J", "accept", "(?<a>x)|(?<a>y)", "u", "accept", "refuse:syntax")
        self.assertEqual(C.classify(d, "accept", set())[0], "c")
        # Without u or v, J's refusal of what the text accepts is row S14's (@4), not row S1's.
        d = diff("J", "accept", "(?<a>x)|(?<a>y)", "", "accept", "refuse:syntax")
        self.assertEqual(C.classify(d, "accept", set())[:2], ("a", "S14"))

    def test_row_s2a_is_j_accepting_an_escaped_duplicate(self) -> None:
        duplicate = with_code("early:duplicate-group-name")
        d = diff("J", "accept", "(?<\\u0061>x)(?<a>y)", "", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), duplicate)[:2], ("a", "S2a"))
        d = diff("J", "accept", "(?<a>x)(?<a>y)", "", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), duplicate)[0], "c")
        d = diff("J", "accept", "(?<\\u0061>x)(?<a>y){,2}", "v", "refuse:syntax", "accept")
        self.assertNotEqual(C.classify(d, "refuse:syntax", set(), with_code("grammar:nothing-to-repeat"))[1], "S2a")

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
        # Not row M2's; since @4 row M8's (`\\b` beside a non-ASCII letter).
        kelvin_letters = C.Context(case_pairs=KELVIN.case_pairs, letters=LETTERS)
        self.assertEqual(C.classify(d, "null", set(), kelvin_letters)[:2], ("a", "M8"))
        d = diff("J", "exec", "[a-z]", "iu", "null", "match:0,1", "\u212a")
        self.assertEqual(C.classify(d, "null", set(), KELVIN)[0], "c")

    def test_row_m2a_is_the_culture_changing_the_answer(self) -> None:
        d = diff("J.tr-TR", "exec", "i", "i", "null", "match:0,1", "\u0130")
        self.assertEqual(C.classify(d, "null", set())[:2], ("a", "M2a"))
        d["id"] = "same-under-invariant"
        same = C.Context(invariant={"same-under-invariant": "match:0,1"})
        # Not row M2a's; since @4 row M8's ("case-insensitive matches of U+0130 and U+0131 no row's pairs hold").
        self.assertEqual(C.classify(d, "null", set(), same)[:2], ("a", "M8"))

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

    def test_every_harness_stop_is_one_answer(self) -> None:
        # Task 2.13's second review: a crash, the wall clock and the former .NET timeout are the harness's one stop,
        # so P3-F02 (`\B` 100,000 times under u) has one class whichever came first.
        for raw in ("limit:harness", "crash:exit-134", "limit:wall-clock", "limit:timeout"):
            self.assertEqual(C.harness_read(raw), "limit:harness")
            d = diff("J.tr-TR", "exec", "\\B" * 100000, "u", "match:0,0", raw)
            self.assertEqual(C.classify(d, ("match", ((0, 0),), ()), set())[:2], ("a", "B8"), raw)
            d = diff("J", "exec", "(?:(a+)+b|a*c)", "u", "match:0,25;-", raw, "a" * 24 + "c")
            self.assertEqual(C.classify(d, ("match", ((0, 25), None), ()), set())[:2], ("b", "D10"), raw)
            d = diff("J", "exec", "(a+)+b", "", "limit:steps", raw, "a" * 40)
            self.assertEqual(C.classify(d, "-", set())[:2], ("b", "D10"), raw)
        self.assertEqual(C.harness_read("limit:steps"), "limit:steps")

    def test_row_b8_is_a_long_sequence_under_u_or_v(self) -> None:
        match = ("match", ((0, 0),), ())
        for pattern, flags, expected in (("\\B" * 100000, "v", "a"), ("(?:a)" * 10000, "u", "a"),
                                         ("\\B" * 100000, "", "b"), ("(?:a)" * 9999, "u", "b")):
            d = diff("J", "exec", pattern, flags, "match:0,0", "crash:exit-134")
            self.assertEqual(C.classify(d, match, set())[0], expected, (pattern[:10], flags))
        # A stop on an input the arbiter cannot reach, or against a wrong S, is no correction.
        d = diff("J", "exec", "\\B" * 100000, "u", "match:0,0", "limit:harness")
        self.assertEqual(C.classify(d, "arbiter:exhausted", set())[0], "c")
        self.assertEqual(C.classify(d, "null", set())[0], "c")
        self.assertEqual(C.longest_sequence("ab|cde(f(g))"), 4)
        self.assertEqual(C.longest_sequence("a{2,3}b*[xyz]\\d"), 4)

    def test_a_harness_stop_of_s_or_v_is_c(self) -> None:
        d = diff("V", "exec", "a", "", "match:0,1", "limit:harness", "a")
        self.assertEqual(C.classify(d, MATCH01, set())[:2], ("c", "-"))
        d = diff("V", "exec", "(a+)+b", "", "limit:steps", "crash:exit-134", "a" * 40)
        self.assertEqual(C.classify(d, "-", set())[:2], ("c", "-"))
        d = diff("J", "exec", "a", "", "limit:harness", "match:0,1", "a")
        self.assertEqual(C.classify(d, MATCH01, set())[:2], ("c", "-"))
        self.assertFalse(C.both_stopped({"S": "limit:wall-clock", "arm": "limit:harness"}))

    def test_row_s5_is_j_refusing_k_in_a_class(self) -> None:
        # Decision D-s5: the class case is the specification's answer, which S gives since task 2.12.
        d = diff("J", "accept", "[\\k<a>](?<a>x)", "", "accept", "refuse:syntax")
        self.assertEqual(C.classify(d, "accept", set())[:2], ("a", "S5"))
        d = diff("J", "exec", "[\\k](?<a>x)", "", "match:0,1", "refuse:syntax", "k")
        self.assertEqual(C.classify(d, MATCH01, set())[:2], ("a", "S5"))
        d = diff("V", "accept", "[\\k<a>](?<a>x)", "", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "accept", set())[0], "c")
        d = diff("J", "accept", "[\\k<a>](?<a>x)", "u", "accept", "refuse:syntax")
        self.assertEqual(C.classify(d, "accept", set())[:2], ("a", "S5"))

    def test_row_f3_is_j_accepting_an_invalid_modifier(self) -> None:
        d = diff("J", "accept", "(?x:a)", "", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set())[:2], ("a", "F3"))
        d = diff("J", "accept", "(?i-x:a)", "", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set())[:2], ("a", "F3"))
        d = diff("J", "accept", "(?i-i:a)", "", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set())[0], "c")

    def test_the_data_decision(self) -> None:
        d = diff("J", "exec", "꟎", "i", "match:0,1", "null", "꟏")
        self.assertEqual(C.classify(d, MATCH01, C.data_partners(Path(".")))[:2], ("a'", "U3"))

    def test_the_canonical_form_reads_names_by_their_capture(self) -> None:
        self.assertEqual(C.canonical("match:0,1;-;0,1|a=2"), ("match", ((0, 1), None, (0, 1)), (("a", (0, 1)),)))
        self.assertEqual(C.canonical("match:0,1;-;0,1|a=-"), ("match", ((0, 1), None, (0, 1)), (("a", None),)))


class RowsAddedTests(unittest.TestCase):
    """The predicates of @4 (2026-11-16), each over its row's own inputs at Broiler.VM's 7c65a8fe and inputs just
    outside them."""

    def test_row_s7_reaches_the_quantified_assertion_vectors(self) -> None:
        nothing = with_code("grammar:nothing-to-repeat")
        for pattern in ("^*", "$+", "\\b*", "^{2}"):
            d = diff("J", "accept", pattern, "", "refuse:syntax", "accept")
            d["class"] = "P1"
            self.assertEqual(C.classify(d, "refuse:syntax", set(), nothing)[:2], ("a", "S7"), pattern)
            # The arms' P4 inputs of that reason are row S12's, by its text.
            d["class"] = "P4"
            self.assertEqual(C.classify(d, "refuse:syntax", set(), nothing)[:2], ("a", "S12"), pattern)
        # `\B*` is not in the widening's list, and `[\b]*` is a class.
        for pattern in ("\\B*", "[\\b]*"):
            d = diff("J", "accept", pattern, "", "refuse:syntax", "accept")
            d["class"] = "P1"
            self.assertNotEqual(C.classify(d, "refuse:syntax", set(), nothing)[1], "S7", pattern)

    def test_row_s8_reaches_the_set_operator_vectors(self) -> None:
        operator = with_code("grammar:class-set-operator")
        d = diff("J", "accept", "[a-z&&b]", "v", "refuse:syntax", "accept")
        d["class"] = "P1"
        self.assertEqual(C.classify(d, "refuse:syntax", set(), operator)[:2], ("a", "S8"))
        d = diff("J", "exec", "[a-z&&[^aeiou]]", "v", "refuse:syntax", "match:0,1", "x")
        d["class"] = "P1"
        self.assertEqual(C.classify(d, "refuse:syntax", set(), operator)[:2], ("a", "S8"))
        d["class"] = "P4"
        self.assertEqual(C.classify(d, "refuse:syntax", set(), operator)[:2], ("a", "S12"))

    def test_row_s12_reads_its_reasons_under_their_flags(self) -> None:
        for pattern, flags, code in (("\\-", "v", "grammar:invalid-escape"), ("]", "u", "grammar:lone-syntax-character"),
                                     ("[\\c]", "u", "grammar:invalid-class-escape"), ("a\\", "", "grammar:trailing-backslash"),
                                     ("[a", "m", "grammar:unterminated-class"), ("(?\\u0069:a)", "u", "grammar:invalid-group"),
                                     ("{", "v", "grammar:nothing-to-repeat")):
            d = diff("J", "accept", pattern, flags, "refuse:syntax", "accept")
            self.assertEqual(C.classify(d, "refuse:syntax", set(), with_code(code))[:2], ("a", "S12"), pattern)
        # A reason the row names under u or v, without them; a reason it does not name; J refusing too.
        for pattern, flags, code in (("\\-", "", "grammar:invalid-escape"), ("(?\\u0069:a)", "", "grammar:invalid-group"),
                                     ("a{2,1}", "", "early:quantifier-range")):
            d = diff("J", "accept", pattern, flags, "refuse:syntax", "accept")
            self.assertEqual(C.classify(d, "refuse:syntax", set(), with_code(code))[0], "c", pattern)
        d = diff("V", "accept", "a\\", "", "refuse:syntax", "accept")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), with_code("grammar:trailing-backslash"))[0], "c")

    def test_row_s13_reads_its_early_errors(self) -> None:
        d = diff("J", "exec", "\\2+\\x6b?", "v", "refuse:syntax", "null", "k")
        past = with_code("early:decimal-escape-past-group-count")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), past)[:2], ("a", "S13"))
        d = diff("J", "exec", "\\2+\\x6b?", "", "refuse:syntax", "null", "k")
        self.assertEqual(C.classify(d, "refuse:syntax", set(), past)[0], "c")
        for code in ("early:class-range-out-of-order", "early:undefined-group-name", "early:invalid-braced-quantifier"):
            d = diff("J", "accept", "x", "", "refuse:syntax", "accept")
            self.assertEqual(C.classify(d, "refuse:syntax", set(), with_code(code))[:2], ("a", "S13"), code)

    def test_row_s14_is_j_refusing_without_u_or_v(self) -> None:
        for pattern, flags in (("\\p{L}", ""), ("[\\W-\\u2028]", "s"), ("[a-\\u00E9-0]", "i"), ("[S-^\\d]", "m")):
            d = diff("J", "accept", pattern, flags, "accept", "refuse:syntax")
            self.assertEqual(C.classify(d, "accept", set())[:2], ("a", "S14"), pattern)
            d = diff("J", "exec", pattern, flags, "null", "refuse:syntax", "a")
            self.assertEqual(C.classify(d, "null", set())[:2], ("a", "S14"), pattern)
        # Under y, a kind only when the same run refused the pattern with no flag; under u, no kind of the row.
        d = diff("J", "exec", "\\p{L}", "y", "null", "refuse:syntax", "a")
        self.assertEqual(C.classify(d, "null", set())[0], "c")
        refused = C.Context(j_refused=frozenset({("J", d["pattern"], ".")}))
        self.assertEqual(C.classify(d, "null", set(), refused)[:2], ("a", "S14"))
        d = diff("J", "accept", "\\p{L}", "u", "accept", "refuse:syntax")
        self.assertEqual(C.classify(d, "accept", set(), refused)[0], "c")

    def test_row_s15_is_a_bound_past_int32_under_i(self) -> None:
        for flags in ("i", "iu", "iv"):
            d = diff("J", "exec", "c{4294967296}\\b", flags, "null", "refuse:syntax", "c")
            self.assertEqual(C.classify(d, "null", set())[:2], ("a", "S15"), flags)
            # Refused without i too: not the row's (under i alone, then row S14's kind refused with no flag).
            without = C.Context(j_refused=frozenset({("J", d["pattern"], C.flags_without(d, "i"))}))
            self.assertEqual(C.classify(d, "null", set(), without)[:2], ("a", "S14") if flags == "i" else ("c", "-"))
        d = diff("J", "exec", "c{2147483647}\\b", "iu", "null", "refuse:syntax", "c")
        self.assertEqual(C.classify(d, "null", set())[0], "c")
        d = diff("J", "exec", "[{4294967296}]", "iu", "null", "refuse:syntax", "c")
        self.assertEqual(C.classify(d, "null", set())[0], "c")
        # Outside P4 the comparison does not show what J answers without i.
        d = diff("J", "exec", "c{4294967296}\\b", "iu", "null", "refuse:syntax", "c")
        d["class"] = "P1"
        self.assertEqual(C.classify(d, "null", set())[0], "c")
        # (@5) ... and under i alone row S14 does not take it in S15's place.
        d = diff("J", "exec", "c{4294967296}\\b", "i", "null", "refuse:syntax", "c")
        d["class"] = "P1"
        self.assertEqual(C.classify(d, "null", set())[0], "c")

    def test_row_m8_reads_its_constructs(self) -> None:
        d = diff("J", "exec", "\\B|I\\b", "", "match:2,2", "match:1,1", "\u00e9I")
        self.assertEqual(C.classify(d, ("match", ((2, 2),), ()), set(), WITH_LETTERS)[:2], ("a", "M8"))
        # The letters are the arbiter's UCD's: a context without them reads no letter.
        self.assertEqual(C.classify(d, ("match", ((2, 2),), ()), set())[0], "c")
        d = diff("J", "exec", "\\B|I\\b", "", "match:2,2", "match:1,1", "aI")
        self.assertEqual(C.classify(d, ("match", ((2, 2),), ()), set(), WITH_LETTERS)[0], "c")
        d = diff("J", "exec", "\\BI", "i", "match:1,2", "null", "a\u0131")
        self.assertEqual(C.classify(d, ("match", ((1, 2),), ()), set())[:2], ("a", "M8"))
        d = diff("J", "exec", "k{0,2}[0\\q{}]_*", "v", "match:0,1", "match:0,0", "k")
        self.assertEqual(C.classify(d, MATCH01, set())[:2], ("a", "M8"))
        d = diff("J.tr-TR", "exec", "[I]", "i", "match:0,1", "null", "i")
        d["class"] = "P5"
        self.assertEqual(C.classify(d, MATCH01, set())[:2], ("a", "M8"))
        d["run"] = "J"
        self.assertEqual(C.classify(d, MATCH01, set())[0], "c")
        # A group name J translates is none of the row's constructs.
        d = diff("J", "exec", "(?<$x>$.{0,2}).*", "m", "match:0,0;0,0|$x=1", "match:0,0;0,0|y=1", "a")
        self.assertEqual(C.classify(d, ("match", ((0, 0), (0, 0)), (("$x", (0, 0)),)), set())[0], "c")

    def test_row_m8_reads_its_constructs_where_they_bear_on_the_difference(self) -> None:
        # (@5) The review's probe: the names alone differ, whatever the pattern and the subject hold.
        names = ("match", ((0, 0), (0, 0)), (("$x", (0, 0)),))
        for pattern, subject in (("(?<$x>$.{0,2}).*\\S", "\u00e9"), ("(?<$x>$.{0,2}).*\\d", "\ud83d"),
                                 ("\\b(?<$x>$.{0,2}).*", "\u00e9"), ("(?<$x>$.{0,2}).*", "\u0130")):
            d = diff("J", "exec", pattern, "im", "match:0,0;0,0|$x=1", "match:0,0;0,0|y=1", subject)
            self.assertEqual(C.classify(d, names, set(), WITH_LETTERS)[0], "c", pattern)
        # The complements, and \w, \d and \s without \b or \B, are not the row's.
        for pattern in ("\\S", "\\W", "\\D", "\\w", "\\d", "\\s", "[\\B\\w]", "[\\S-\\x07B]"):
            d = diff("J", "exec", pattern, "", "match:0,1", "null", "a\u00e9")
            self.assertEqual(C.classify(d, MATCH01, set(), WITH_LETTERS)[0], "c", pattern)
        # A code unit past ASCII that is no letter: a lone surrogate, an emoji, U+00A0.
        for subject in ("\ud83da", "\U0001f600a", "\u00a0a"):
            d = diff("J", "exec", "\\Ba", "", "null", "match:2,3", subject)
            self.assertEqual(C.classify(d, "null", set(), WITH_LETTERS)[0], "c", repr(subject))
        # A letter past ASCII, a pair read as one code point, beside the matches.
        d = diff("J", "exec", "\\Ba", "", "null", "match:2,3", "\U0001d400a")
        self.assertEqual(C.classify(d, "null", set(), WITH_LETTERS)[:2], ("a", "M8"))
        # A letter away from both matches is not beside them, unless a look-around reads past the match.
        d = diff("J", "exec", "a\\b", "", "match:0,1", "null", "ab-\u00e9")
        self.assertEqual(C.classify(d, MATCH01, set(), WITH_LETTERS)[0], "c")
        d = diff("J", "exec", "a\\b(?=.*b)", "", "match:0,1", "null", "ab-\u00e9")
        self.assertEqual(C.classify(d, MATCH01, set(), WITH_LETTERS)[:2], ("a", "M8"))
        # U+0130 or U+0131 under i: in the pattern, or beside the matches.
        d = diff("J", "exec", "a", "i", "match:0,1", "null", "a--\u0130")
        self.assertEqual(C.classify(d, MATCH01, set(), WITH_LETTERS)[0], "c")
        d = diff("J", "exec", "\\u0130|a", "i", "match:0,1", "null", "a--")
        self.assertEqual(C.classify(d, MATCH01, set(), WITH_LETTERS)[:2], ("a", "M8"))
        # P5 in tr-TR: each pattern against its own subject.
        for pattern, subject, row in (("I", "i", "M8"), ("[i]", "I", "M8"), ("I", "I", "-"), ("i", "i", "-")):
            d = diff("J.tr-TR", "exec", pattern, "i", "match:0,1", "null", subject)
            d["class"] = "P5"
            self.assertEqual(C.classify(d, MATCH01, set(), WITH_LETTERS)[1], row, (pattern, subject))

    def test_row_m9_is_its_one_input(self) -> None:
        d = diff("J", "exec", "(?<$x>){1,}?\\B|(?!\u212a)", "y", "match:2,2;2,2|$x=1",
                 "throw:ArgumentOutOfRangeException", "X\\", 2)
        arbiter = ("match", ((2, 2), (2, 2)), (("$x", (2, 2)),))
        self.assertEqual(C.classify(d, arbiter, set())[:2], ("a", "M9"))
        d = diff("J", "exec", "(?<$x>){1,}?\\B", "y", "match:2,2;2,2|$x=1", "throw:ArgumentOutOfRangeException",
                 "X\\", 2)
        self.assertEqual(C.classify(d, arbiter, set())[0], "c")

    def test_row_m4_reaches_its_widened_vectors(self) -> None:
        pair = "\U0001d306"
        d = diff("J", "exec", "(.)", "gdu", "match:1,2;0,2", "match:1,2;1,2", pair, 1)
        self.assertEqual(C.classify(d, ("match", ((1, 2), (0, 2)), ()), set())[:2], ("a", "M4"))
        d = diff("J", "exec", "[\\ud834\\udf06]", "gu", "match:1,2", "null", pair, 1)
        self.assertEqual(C.classify(d, ("match", ((1, 2),), ()), set())[:2], ("a", "M4"))
        # From lastIndex 0, no pair is split.
        d = diff("J", "exec", "[\\ud834\\udf06]", "gu", "match:0,1", "null", pair, 0)
        self.assertEqual(C.classify(d, MATCH01, set())[0], "c")
        # (@5) The widening names two vectors: another pattern or other flags of the same shape are not read.
        d = diff("J", "exec", "(.)", "gu", "match:1,2;0,2", "match:1,2;1,2", pair, 1)
        self.assertEqual(C.classify(d, ("match", ((1, 2), (0, 2)), ()), set())[0], "c")
        d = diff("J", "exec", "[\\ud834\\udf06x]", "gu", "match:1,2", "null", pair, 1)
        self.assertEqual(C.classify(d, ("match", ((1, 2),), ()), set())[0], "c")

    def test_row_s8a_is_v_keeping_word_folding(self) -> None:
        d = diff("V", "exec", "[\\w--I]", "iv", "match:1,2", "match:0,1", "IA")
        self.assertEqual(C.classify(d, ("match", ((1, 2),), ()), set())[:2], ("a", "S8a"))
        d = diff("V", "exec", "[\\w--\\ud83d--\\S]?", "iv", "match:0,0", "match:0,1", "s")
        self.assertEqual(C.classify(d, ("match", ((0, 0),), ()), set())[:2], ("a", "S8a"))
        d = diff("V", "exec", "[\\w--I]", "v", "match:1,2", "match:0,1", "IA")
        self.assertEqual(C.classify(d, ("match", ((1, 2),), ()), set())[0], "c")
        d = diff("V", "exec", "[\\w--I]", "iv", "match:1,2", "match:0,1", "_A")
        self.assertEqual(C.classify(d, ("match", ((1, 2),), ()), set())[0], "c")
        d = diff("J", "exec", "[\\w--I]", "iv", "match:1,2", "match:0,1", "IA")
        self.assertEqual(C.classify(d, ("match", ((1, 2),), ()), set())[0], "c")


class ReductionTests(unittest.TestCase):
    """Option R of the rule's revision of 2026-11-16, over reduce.py's output."""

    def reduction(self, prefix, s_answer="null", reduced=None):
        prefixes = {"x": None if prefix is None else (prefix, C.parse_rendered(s_answer))}
        differences = {} if reduced is None else {(f"x~p{prefix}", "J"): reduced}
        return C.Context(reduction=C.Reduction(prefixes, differences, {f"x~p{prefix}": s_answer}))

    def test_a_stop_against_an_answer_the_prefix_confirms_is_b_prime(self) -> None:
        d = diff("J", "exec", "(a+)+b", "", "limit:steps", "null", "a" * 40)
        self.assertEqual(C.classify(d, "arbiter:exhausted", set(), self.reduction(17))[0], "b'")
        d = diff("V", "exec", "(a+)+b", "", "null", "limit:steps", "a" * 40)
        self.assertEqual(C.classify(d, "arbiter:exhausted", set(), self.reduction(17))[0], "b'")
        # Without the reduction, the rule as filed: class (c).
        self.assertEqual(C.classify(d, "arbiter:exhausted", set())[0], "c")
        # A bound option R does not name (arm V's ceiling, row B6) gives no (b').
        d = diff("V", "exec", "(a+)+b", "", "null", "limit:ceiling", "a" * 40)
        self.assertEqual(C.classify(d, "arbiter:exhausted", set(), self.reduction(17))[0], "c")

    def test_no_stop_or_no_prefix_or_no_reduction_stays_c(self) -> None:
        d = diff("J", "exec", "^[a]*$", "u", "match:0,40", "null", "a" * 40)
        self.assertEqual(C.classify(d, "arbiter:exhausted", set(), self.reduction(17, "match:0,17"))[:2], ("c", "-"))
        d = diff("J", "exec", "(?:\\b){4294967296}", "", "limit:steps", "null", "a")
        self.assertEqual(C.classify(d, "arbiter:exhausted", set(), self.reduction(None))[0], "c")
        d["id"] = "not-reduced"
        self.assertEqual(C.classify(d, "arbiter:exhausted", set(), self.reduction(17))[0], "c")
        # S's answer at the prefix not the arbiter's.
        d = diff("J", "exec", "(a+)+b", "", "limit:steps", "null", "a" * 40)
        context = self.reduction(17, "null")
        context.reduction.prefixes["x"] = (17, ("match", ((0, 1),), ()))
        self.assertEqual(C.classify(d, "arbiter:exhausted", set(), context)[0], "c")

    def test_a_reduced_difference_is_classed_as_itself(self) -> None:
        d = diff("J", "exec", "\\p{L}", "s", "limit:steps", "refuse:syntax", "a" * 40)
        reduced = diff("J", "exec", "\\p{L}", "s", "null", "refuse:syntax", "a" * 2)
        reduced["id"] = "x~p2"
        self.assertEqual(C.classify(d, "arbiter:exhausted", set(), self.reduction(2, "null", reduced))[:2],
                         ("a", "S14"))

    def test_the_reduction_is_read_from_its_files(self) -> None:
        import json
        import tempfile
        from population_format import open_text
        with tempfile.TemporaryDirectory() as tmp:
            tmp = Path(tmp)
            for sub in ("comparison", "runs", "population"):
                (tmp / sub).mkdir()
            (tmp / "reductions.tsv").write_text("# id\tclass\tlength\tlastIndex\tprefix\tarbiter\tprobes\n"
                                                "x\tP4\t40\t0\t17\tnull\t6\ny\tP4\t3\t0\tnone\tarbiter:exhausted\t1\n",
                                                encoding="utf-8")
            with open_text(tmp / "comparison" / "differences.tsv.gz", "wt") as out:
                out.write("# id\tclass\top\trun\tpattern\tflags\tsubject\tlastIndex\tpolicy\tS\tarm\n")
            with open_text(tmp / "runs" / "P4.S.tsv.gz", "wt") as out:
                out.write("# id\tanswer\tcharge\nx~p17\tnull\t0\n")
            (tmp / "population" / "manifest.json").write_text(json.dumps({"reducer": "r@1", "cutoff": 5}),
                                                               encoding="utf-8")
            reduction = C.Reduction.read(tmp, 5)
            self.assertEqual(reduction.prefixes, {"x": (17, "null"), "y": None})
            self.assertEqual(reduction.s_answers, {"x~p17": "null"})
            self.assertEqual(reduction.identity["reducer"], "r@1")
            with self.assertRaises(ValueError):
                C.Reduction.read(tmp, 6)
            d = diff("J", "exec", "(a+)+b", "", "limit:steps", "null", "a" * 40)
            self.assertEqual(C.classify(d, "arbiter:exhausted", set(), C.Context(reduction=reduction))[0], "b'")


class ExclusionTests(unittest.TestCase):
    """The owner's ruling of 2026-11-16 on population P4 (reference-arms-revised-2026-11-16b.md): a listed pattern's
    differences are excluded, under every flag set, by acceptance and by exec, a reduced case among them."""

    def exclusion(self, pattern="(?:){4294967296}a"):
        return C.Exclusion({"P4-00001": encode(pattern)})

    def test_a_listed_pattern_is_excluded_everywhere_in_p4(self) -> None:
        exclusion = self.exclusion()
        for case_id, op, flags in (("P4-00001-none", "accept", ""), ("P4-00001-iv-03", "exec", "iv"),
                                   ("P4-00001-y-15~p0", "exec", "y")):
            d = diff("J", op, "(?:){4294967296}a", flags, "limit:steps", "null", "a")
            d["id"] = case_id
            self.assertIn(C.Exclusion.WHY, exclusion.excludes(d))
            self.assertIn("P4-00001", exclusion.excludes(d))
        d = diff("J", "exec", "(?:){4294967296}a", "", "limit:steps", "null", "a")
        d["id"] = "P4-00002-none-00"
        self.assertIsNone(exclusion.excludes(d))
        # The ruling is P4's: the same id in another class is no listed pattern.
        d["id"], d["class"] = "P4-00001-none-00", "P1"
        self.assertIsNone(exclusion.excludes(d))

    def test_a_listed_id_with_another_pattern_refuses(self) -> None:
        d = diff("J", "exec", "(?:){4294967296}b", "", "limit:steps", "null", "a")
        d["id"] = "P4-00001-none-00"
        with self.assertRaises(C.ExclusionMismatch):
            self.exclusion().excludes(d)

    def test_the_listing_is_named_by_its_bytes(self) -> None:
        import hashlib
        import tempfile
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "p4-excluded.tsv"
            path.write_text(f"# id\tpattern\tquantifier\tflags\nP4-00001\t{encode('(?:){4}a')}\t-\t.\n",
                            encoding="utf-8")
            exclusion = C.Exclusion.read(path)
            self.assertEqual(exclusion.patterns, {"P4-00001": encode("(?:){4}a")})
            self.assertEqual(exclusion.identity, {"rule": C.Exclusion.RULE, "name": "p4-excluded.tsv", "patterns": 1,
                                                  "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})


class CorrespondenceTests(unittest.TestCase):
    def test_a_missing_row_refuses(self) -> None:
        self.assertTrue(any("S1" in p for p in C.check_rows({})))

    def test_the_rows_as_their_classes_name_them(self) -> None:
        rows = {"S1": "**Correction of Broiler.VM** and of Broiler.Regex's preview.4", "S2a": "Correction of Broiler.JS",
                "S5": "VM: the specification's answer. Broiler.JS: **correction of Broiler.JS**, by D-s5: ...",
                "S7": "Correction of Broiler.JS", "S8": "Correction of Broiler.JS",
                "S10": "Correction of Broiler.VM. The class case `[\\uD83D\\u{DC38}]`: **correction of Broiler.JS**",
                "M2a": "Correction of Broiler.JS", "M2b": "Correction of Broiler.JS", "M2": "Correction of Broiler.JS",
                "M3": "Correction of Broiler.JS",
                "M4": "correction of Broiler.VM ... The half-pair matches (`/\\udf06/gu`, `/yu`, the look-behind): "
                      "**correction of Broiler.JS**",
                "M7": "Correction of Broiler.VM. The look-behind: also a correction of Broiler.Regex's preview.4, and "
                      "so of Broiler.JS",
                "B2": "Correction of Broiler.JS and of Broiler.Regex's preview.4", "S3": "policy", "B6": "policy",
                "B8": "**Correction of Broiler.JS and of Broiler.Regex's preview.4** (R4)",
                "F3": "**Same in both** ...; and `(?x:a)`, accepted by Broiler.JS's constructor, a **correction of "
                      "Broiler.JS** that SJL-11.5 answers",
                "B7": "policy", "U3": "Correction of Broiler.JS by data decision"}
        rows.update(ADDED_ROWS)
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


class ExitStatusTests(unittest.TestCase):
    """The exit status run.sh reads: 0 no class (c), 1 a class (c) difference, 2 refused, 3 failed."""

    def test_a_refusal_is_2_and_a_failure_is_3(self) -> None:
        import tempfile
        with tempfile.TemporaryDirectory() as tmp:
            tmp = Path(tmp)
            (tmp / "correspondence.md").write_text("| S1 | nothing |\n", encoding="utf-8")
            args = ["--comparison", str(tmp), "--runs", str(tmp), "--broiler-js", str(tmp / "no-checkout"),
                    "--commit", "HEAD", "--correspondence", str(tmp / "correspondence.md"), "--unicode-pins", str(tmp)]
            self.assertEqual(C.main(args), 2)
            filed = os.environ.get("BROILER_VM_CORRESPONDENCE")
            if filed:
                args[args.index("--correspondence") + 1] = filed
                # The arbiter cannot be taken from a directory that is no checkout: an exception, exit 3, no file.
                self.assertEqual(C.main(args), 3)
                self.assertFalse((tmp / "classification.json").exists())
            with self.assertRaises(SystemExit) as raised:
                C.main(["--comparison"])
            self.assertEqual(raised.exception.code, 2)

    def test_an_exception_is_3_not_the_1_of_class_c(self) -> None:
        original = C.run
        try:
            C.run = lambda argv: 1 // 0
            self.assertEqual(C.main([]), 3)
        finally:
            C.run = original


STUB_REGEXPREF = '''
REFERENCE_VERSION = "regexpref@stub"


class PatternSyntaxError(Exception):
    def __init__(self, code):
        super().__init__(code)
        self.code = code


class SpecAssertion(Exception):
    pass


class _Counted:
    exhausted = False
    result = None


def regexp_create(pattern, flags):
    if pattern == [0x28]:
        raise PatternSyntaxError("grammar:stub")


def counted_exec(pattern, flags, subject, last_index, cutoff):
    return _Counted()


class _Data:
    def orbit(self, unit):
        return {unit}

    def general_category(self, cp):
        return "Ll" if cp == 0xE9 else "Cn"


def ucd():
    return _Data()


def _canonicalize_bmp():
    return list(range(0x10000))


def _canonicalize_bmp_preimages():
    return {}
'''

# The rows added at Broiler.VM's 7c65a8fe (@4), as their classes open.
ADDED_ROWS = {"S8a": "**Correction of Broiler.VM** (kind R; ...)", "S12": "**Correction of Broiler.JS** (SJL-11.5's ...)",
              "S13": "**Correction of Broiler.JS**; as row S12", "S14": "**Correction of Broiler.JS**; as row S12",
              "S15": "**Correction of Broiler.JS**; as row S12", "M8": "**Correction of Broiler.JS**; as row S12",
              "M9": "**Correction of Broiler.JS**; no test262 row is known to move for it"}

STUB_ROWS = {"S1": "Correction of Broiler.VM", "S2a": "Correction of Broiler.JS",
             "S5": "Broiler.JS: **correction of Broiler.JS**, by D-s5", "S7": "Correction of Broiler.JS",
             "S8": "Correction of Broiler.JS",
             "S10": "Correction of Broiler.VM. The class case `[\\uD83D\\u{DC38}]`: **correction of Broiler.JS**",
             "M2a": "Correction of Broiler.JS", "M2b": "Correction of Broiler.JS", "M2": "Correction of Broiler.JS",
             "M3": "Correction of Broiler.JS",
             "M4": "correction of Broiler.VM. The half-pair matches (`/\\udf06/gu`, `/yu`, the look-behind): "
                   "**correction of Broiler.JS**",
             "M7": "Correction of Broiler.VM. The look-behind: also a correction of Broiler.Regex's preview.4, and so "
                   "of Broiler.JS",
             "B2": "Correction of Broiler.JS and of Broiler.Regex's preview.4", "S3": "policy", "B6": "policy",
             "B7": "policy", "B8": "Correction of Broiler.JS and of Broiler.Regex's preview.4",
             "F3": "`(?x:a)`, accepted by Broiler.JS's constructor, a **correction of Broiler.JS**",
             "U3": "Correction of Broiler.JS by data decision", **ADDED_ROWS}


class PathIndependenceTests(unittest.TestCase):
    """classification.json names its inputs by their bytes and commits, not by where they are (task 2.13: it held the
    correspondence's path as given, so the same inputs elsewhere, or a relative path from another working directory,
    gave another digest)."""

    def setUp(self) -> None:
        import subprocess
        import tempfile
        self.subprocess = subprocess
        self.scratch = Path(tempfile.mkdtemp(prefix="classify-paths-"))
        self.saved = (list(sys.path), sys.modules.pop("regexpref", None), os.environ.get("BROILER_UNICODE_PINS"),
                      os.getcwd())

    def tearDown(self) -> None:
        import shutil
        path, module, pins, cwd = self.saved
        os.chdir(cwd)
        sys.path[:] = path
        sys.modules.pop("regexpref", None)
        if module is not None:
            sys.modules["regexpref"] = module
        if pins is None:
            os.environ.pop("BROILER_UNICODE_PINS", None)
        else:
            os.environ["BROILER_UNICODE_PINS"] = pins
        shutil.rmtree(self.scratch, ignore_errors=True)

    def git(self, *args) -> None:
        env = dict(os.environ, GIT_AUTHOR_NAME="t", GIT_AUTHOR_EMAIL="t@example.invalid", GIT_COMMITTER_NAME="t",
                   GIT_COMMITTER_EMAIL="t@example.invalid", GIT_AUTHOR_DATE="2026-01-01T00:00:00Z",
                   GIT_COMMITTER_DATE="2026-01-01T00:00:00Z", GIT_CONFIG_GLOBAL=os.devnull, GIT_CONFIG_NOSYSTEM="1")
        self.subprocess.run(["git", *args], check=True, capture_output=True, env=env)

    def inputs(self, where: Path, checkout: Path | None) -> tuple[Path, Path]:
        """A Broiler.JS stand-in checkout with a stub arbiter (or a clone of `checkout`), a correspondence, a
        comparison and a runs directory under `where`."""
        from population_format import open_text
        js = where / "broiler-js"
        if checkout is None:
            (js / "scripts" / "conformance").mkdir(parents=True)
            (js / "conformance" / "vectors" / "sources").mkdir(parents=True)
            (js / "scripts" / "conformance" / "regexpref.py").write_text(STUB_REGEXPREF, encoding="utf-8")
            (js / "scripts" / "conformance" / "unicoderef.py").write_text("# stub\n", encoding="utf-8")
            (js / "conformance" / "vectors" / "sources" / "unicode.pin").write_text("stub\n", encoding="utf-8")
            self.git("init", "-q", str(js))
            self.git("-C", str(js), "add", ".")
            self.git("-C", str(js), "commit", "-q", "-m", "stub")
        else:
            self.git("clone", "-q", str(checkout), str(js))
        docs = where / "vm" / "docs"
        docs.mkdir(parents=True)
        (docs / "correspondence.md").write_text(
            "".join(f"| {row} | construct | {cls} |\n" for row, cls in STUB_ROWS.items()), encoding="utf-8")
        comparison, runs = where / "comparison", where / "runs"
        comparison.mkdir()
        runs.mkdir()
        (runs / "run.V.json").write_text('{"control-case": null}\n', encoding="utf-8")
        with open_text(comparison / "differences.tsv.gz", "wt") as out:
            out.write("# id\tclass\top\trun\tpattern\tflags\tsubject\tlastIndex\tpolicy\tS\tarm\n")
            out.write(f"P4-1\tP4\taccept\tV\t{encode('(?<a>x)|(?<a>y)')}\t.\t-\t-\tdefault\taccept\trefuse:syntax\n")
            out.write(f"P4-2\tP4\texec\tJ\t{encode('a')}\t.\t{encode('b')}\t0\tdefault\tnull\tmatch:0,1\n")
        return js, docs / "correspondence.md"

    def test_two_runs_from_different_places_write_the_same_classification_json(self) -> None:
        written = []
        checkout = None
        for k, relative in enumerate((True, False)):
            where = self.scratch / f"place-{k}" / ("deeper" if k else "")
            where.mkdir(parents=True, exist_ok=True)
            js, correspondence = self.inputs(where, checkout)
            checkout = checkout or js
            out = self.scratch / f"out-{k}"
            if relative:
                # From the inputs' directory, every path relative to it.
                os.chdir(where)
                args = ["--comparison", "comparison", "--runs", "runs", "--broiler-js", "broiler-js",
                        "--correspondence", "vm/docs/correspondence.md"]
            else:
                os.chdir(self.scratch)
                args = ["--comparison", str(where / "comparison"), "--runs", str(where / "runs"), "--broiler-js",
                        str(js), "--correspondence", str(correspondence)]
            args += ["--commit", "HEAD", "--unicode-pins", str(where), "--out", str(out)]
            self.assertEqual(C.main(args), 1)
            written.append((out / "classification.json").read_bytes())
            sys.modules.pop("regexpref", None)
        self.assertEqual(written[0], written[1])
        self.assertNotIn(str(self.scratch).encode(), written[0])
        self.assertNotIn(b"vm/docs", written[0])
        facts = __import__("json").loads(written[0])
        self.assertEqual(facts["rows-read-from"], C.CORRESPONDENCE)
        self.assertIsNone(facts["reduction"])
        self.assertIsNone(facts["exclusion"])
        data = (self.scratch / "place-0" / "vm" / "docs" / "correspondence.md").read_bytes()
        self.assertEqual(facts["correspondence"], {
            "name": "correspondence.md", "sha256": __import__("hashlib").sha256(data).hexdigest(),
            "blob": self.subprocess.run(["git", "hash-object", "--stdin"], input=data, capture_output=True,
                                        check=True).stdout.decode().strip()})

    def test_an_exclusion_lists_its_patterns_apart_and_asks_no_arbiter(self) -> None:
        import json
        from population_format import open_text
        where = self.scratch / "place"
        where.mkdir()
        js, correspondence = self.inputs(where, None)
        with open_text(where / "comparison" / "differences.tsv.gz", "wt") as out:
            out.write("# id\tclass\top\trun\tpattern\tflags\tsubject\tlastIndex\tpolicy\tS\tarm\n")
            out.write(f"P4-00001-none\tP4\taccept\tV\t{encode('(?<a>x)|(?<a>y)')}\t.\t-\t-\tdefault\taccept\t"
                      "refuse:syntax\n")
            out.write(f"P4-00002-none-00\tP4\texec\tJ\t{encode('a')}\t.\t{encode('b')}\t0\tdefault\tnull\t"
                      "match:0,1\n")
        listing = where / "p4-excluded.tsv"
        listing.write_text(f"# id\tpattern\tquantifier\tflags\nP4-00002\t{encode('a')}\t-\t.\n", encoding="utf-8")
        args = ["--comparison", str(where / "comparison"), "--runs", str(where / "runs"), "--broiler-js", str(js),
                "--correspondence", str(correspondence), "--commit", "HEAD", "--unicode-pins", str(where),
                "--cache", str(where / "cache.jsonl"), "--out", str(self.scratch / "out")]
        # Without the exclusion the exec is class (c); with it, it is excluded, and no class (c) remains.
        self.assertEqual(C.main(args), 1)
        sys.modules.pop("regexpref", None)
        (where / "cache.jsonl").unlink()
        self.assertEqual(C.main(args + ["--exclude", str(listing)]), 0)
        facts = json.loads((self.scratch / "out" / "classification.json").read_text(encoding="utf-8"))
        self.assertEqual(facts["counts"], {"a": 1, "excluded": 1})
        self.assertEqual(facts["by-class-row-run-population"]["excluded/-/J/P4"], 1)
        self.assertEqual(facts["exclusion"]["patterns"], 1)
        self.assertEqual(facts["exclusion"]["name"], "p4-excluded.tsv")
        with open_text(self.scratch / "out" / "classified.tsv.gz") as handle:
            line = [row for row in handle if row.startswith("P4-00002")][0].split("\t")
        self.assertEqual(line[6:9], ["-", "excluded", "-"])
        self.assertNotIn(encode("b"), (where / "cache.jsonl").read_text(encoding="utf-8"))
        # A listed id whose pattern is not the comparison's refuses: another population.
        sys.modules.pop("regexpref", None)
        listing.write_text(f"# id\tpattern\tquantifier\tflags\nP4-00002\t{encode('c')}\t-\t.\n", encoding="utf-8")
        self.assertEqual(C.main(args + ["--exclude", str(listing)]), 2)


if __name__ == "__main__":
    unittest.main()
