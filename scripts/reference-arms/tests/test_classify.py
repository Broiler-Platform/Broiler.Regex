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
        # P5 in tr-TR: each pattern against its own subject. (Since @7 the invariant run's difference on the same
        # input is given, so that row M15, which takes `I` against `I` where the invariant culture answers as the text
        # does, takes nothing here: the test reads row M8 alone, as it did at @5.)
        invariant_differs = C.Context(letters=LETTERS, j_differences=frozenset({"x"}))
        for pattern, subject, row in (("I", "i", "M8"), ("[i]", "I", "M8"), ("I", "I", "-"), ("i", "i", "-")):
            d = diff("J.tr-TR", "exec", pattern, "i", "match:0,1", "null", subject)
            d["class"] = "P5"
            self.assertEqual(C.classify(d, MATCH01, set(), invariant_differs)[1], row, (pattern, subject))

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


def arms_names(names) -> str:
    """A match's names as the arms write them: each name escaped (escape_name) with its capture's index."""
    return ",".join(f"{C.escape_name([ord(c) for c in name])}={group}" for name, group in names)


def reading_gives(answers):
    """A Context whose arbiter answers each reading by the table `answers` (reading text -> answer), else null, and
    records the readings it was asked."""
    asked = []

    def reading(d, text):
        asked.append(text)
        return answers.get(text, "null")

    context = C.Context(reading=reading)
    context.asked = asked
    return context


ROWS_M10_TO_M15 = ("M10", "M11", "M12", "M13", "M14", "M15")


class RowsM10ToM15Tests(unittest.TestCase):
    """The predicates of @7 (2026-11-16), each over its row's own examples at Broiler.VM's 44511066 and inputs just
    outside them; the arbiter's answer to a reading is given."""

    def j(self, pattern, flags, s, arm, subject="ab", run="J", last=0):
        return diff(run, "exec", pattern, flags, s, arm, subject, last)

    def test_row_m10_reads_each_rewrite_where_the_row_names_it(self) -> None:
        s = "match:0,1;0,1|" + arms_names([("$x", 1)])
        # `$x` as `(?=$|\r)x` under m.
        d = self.j("(?<$x>a)", "m", s, "match:0,1;0,1|" + arms_names([("(?=$|\\r)x", 1)]))
        self.assertEqual(C.classify(d, C.canonical(s), set()), ("a", "M10", "a correction of Broiler.JS"))
        # The same rewrite without m: no row names it; nor under m where a modifier group stands in the pattern.
        self.assertEqual(C.classify(dict(d, flags="."), C.canonical(s), set())[0], "c")
        self.assertEqual(C.classify(dict(d, pattern=encode("(?<$x>a)(?i:b)?")), C.canonical(s), set())[1], "-")
        # `\z` with an inline modifier group and without m; not without the group.
        z = "match:0,1;0,1|" + arms_names([("\\zx", 1)])
        self.assertEqual(C.classify(self.j("(?<$x>a)(?i:b)?", ".", s, z), C.canonical(s), set())[1], "M10")
        self.assertEqual(C.classify(self.j("(?<$x>a)", ".", s, z), C.canonical(s), set())[1], "-")
        # The third anchor with a modifier group under m; not without the group.
        nl = "match:0,1;0,1|" + arms_names([("(?=[\\n\\r\\u2028\\u2029]|\\z)x", 1)])
        self.assertEqual(C.classify(self.j("(?<$x>a)(?-s:b)?", "m", s, nl), C.canonical(s), set())[1], "M10")
        self.assertEqual(C.classify(self.j("(?<$x>a)", "m", s, nl), C.canonical(s), set())[1], "-")
        # Another capture, another name, or under u: not this row's.
        self.assertEqual(C.classify(dict(d, arm="match:0,1;1,1|" + arms_names([("(?=$|\\r)x", 1)])),
                                    C.canonical(s), set())[1], "-")
        self.assertEqual(C.classify(dict(d, arm="match:0,1;0,1|" + arms_names([("y", 1)])), C.canonical(s),
                                    set())[1], "-")
        self.assertFalse(C.group_name_rewritten(dict(d, flags="mu"), C.Context()))
        # "At the same capture" is the same group: names swapped between two groups of equal spans are not taken.
        two = "match:0,0;0,0;0,0|" + arms_names([("$a", 1), ("$b", 2)])
        swapped = self.j("(?<$a>)(?<$b>)", "m", two, "match:0,0;0,0;0,0|" + arms_names(
            [("(?=$|\\r)b", 1), ("(?=$|\\r)a", 2)]))
        self.assertFalse(C.group_name_rewritten(swapped, C.Context()))
        kept = dict(swapped, arm="match:0,0;0,0;0,0|" + arms_names([("(?=$|\\r)a", 1), ("(?=$|\\r)b", 2)]))
        self.assertTrue(C.group_name_rewritten(kept, C.Context()))

    def test_row_m10_reads_a_case_rewrite_from_the_canonicalize_class(self) -> None:
        classes = {}
        for members in ((0x3A0, 0x3C0, 0x3D6), (0x4B, 0x6B), (0x212A,), (0x58, 0x78), (0x2160, 0x2170)):
            for unit in members:
                classes[unit] = members
        case = C.Context(canonicalize_class=lambda u: classes.get(u, (u,)),
                         over_folds={0x4B, 0x6B, 0x212A}.__contains__)

        def m10(name, rewritten, flags="i"):
            s = "match:0,1;0,1|" + arms_names([(name, 1)])
            d = self.j(f"(?<{name}>a)", flags, s, "match:0,1;0,1|" + arms_names([(rewritten, 1)]))
            return C.classify(d, C.canonical(s), set(), case)[1]

        # `π` as `[πΠϖ]` under i, its Canonicalize class with the letter first; not without i, nor in another order,
        # nor as a class that is not its Canonicalize class.
        self.assertEqual(m10("π", "[πΠϖ]"), "M10")
        self.assertEqual(m10("π", "[πΠϖ]", "."), "-")
        self.assertEqual(m10("π", "[πϖΠ]"), "-")
        self.assertEqual(m10("π", "[πΠ]"), "-")
        # A character of the name that is not a letter, whose class qualifies: U+2160 as `[Ⅰⅰ]`.
        self.assertEqual(m10("\u2160", "[\u2160\u2170]"), "M10")
        # `x`, whose class is ASCII alone, is not rewritten: neither as a class nor as `(?-i:…)`.
        self.assertEqual(m10("x", "[xé]"), "-")
        self.assertEqual(m10("x", "(?-i:x)"), "-")
        self.assertEqual(m10("x", "(?-i:[\\x58\\x78])"), "-")
        # `k`, which .NET's invariant lowercase folds with U+212A into another class: `(?-i:…)` over its class, each
        # member as the translator escapes it; not as a class of ASCII letters, nor over another class.
        self.assertEqual(m10("k", "(?-i:[\\x4b\\x6b])"), "M10")
        self.assertEqual(m10("k", "[kK]"), "-")
        self.assertEqual(m10("k", "(?-i:[kK])"), "-")
        self.assertEqual(m10("k", "(?-i:[\\x4b\\x6b\\u212a])"), "-")
        # Without the tables no letter reads as rewritten.
        s = "match:0,1;0,1|" + arms_names([("π", 1)])
        d = self.j("(?<π>a)", "i", s, "match:0,1;0,1|" + arms_names([("[πΠϖ]", 1)]))
        self.assertFalse(C.group_name_rewritten(d, C.Context()))

    def test_row_m11_reads_the_empty_iteration(self) -> None:
        self.assertEqual(C.optional_group_reading("(a)?b"), "(?:(a)|)b")
        self.assertEqual(C.optional_group_reading("(?<n>a|)??"), "(?:|(?<n>a|))")
        self.assertEqual(C.optional_group_reading("((a)?){0,1}"), "(?:((?:(a)|))|)")
        self.assertEqual(C.optional_group_reading("(?:a){0,1}?c"), "(?:|(?:a))c")
        self.assertIsNone(C.optional_group_reading("(?=a)?b"))
        self.assertIsNone(C.optional_group_reading("(a)*[(b)?]"))
        # `(\D?^)?`: J keeps the empty iteration, the capture participates; the reading gives J's answer.
        d = self.j("(\\D?^)?", ".", "match:0,0;-", "match:0,0;0,0", subject="")
        context = reading_gives({"(?:(\\D?^)|)": C.canonical("match:0,0;0,0")})
        self.assertEqual(C.classify(d, C.canonical("match:0,0;-"), set(), context)[1], "M11")
        # The reading gives the text's answer, or another: the input stays (c).
        same = reading_gives({"(?:(\\D?^)|)": C.canonical("match:0,0;-")})
        self.assertEqual(C.classify(d, C.canonical("match:0,0;-"), set(), same)[0], "c")
        self.assertEqual(C.classify(d, C.canonical("match:0,0;-"), set(), reading_gives({}))[0], "c")
        # Under u the row reads nothing, and asks the arbiter nothing.
        under_u = reading_gives({"(?:(\\D?^)|)": C.canonical("match:0,0;0,0")})
        self.assertFalse(C.empty_iteration_kept(dict(d, flags="u"), under_u))
        self.assertEqual(under_u.asked, [])

    def test_row_m12_reads_property_escapes_and_bell_in_a_class(self) -> None:
        def digits(name):
            return list(range(0x30, 0x3A)) if name == "N" else [0x61]
        self.assertEqual(C.property_reading("[\\p{N}]B", digits), "[\\u0030-\\u0039]B")
        self.assertEqual(C.property_reading("[\\p{N}-a]", digits), "[\\u0030-\\u0039\\-a]")
        self.assertEqual(C.property_reading("[\\p{N}-]", digits), "[\\u0030-\\u0039-]")
        self.assertTrue(C.property_reading("[\\P{Ll}]", digits).startswith("[\\u0000-\\u0060\\u0062-"))
        self.assertIsNone(C.property_reading("\\p{N}[a]", digits))
        self.assertIsNone(C.property_reading("[\\p{Script=Greek}]", digits))
        self.assertIsNone(C.property_reading("[\\p{Foo}]", digits))
        self.assertEqual(C.bell_reading("[\\a]|cb"), "[\\x07]|cb")
        self.assertIsNone(C.bell_reading("\\a[b]"))
        d = self.j("[\\p{N}]B", ".", "null", "match:0,2", subject="1B")
        context = reading_gives({"[\\u0030-\\u0039]B": C.canonical("match:0,2")})
        context._category_units = digits
        self.assertEqual(C.classify(d, "null", set(), context)[1], "M12")
        self.assertEqual(C.classify(dict(d, flags="v"), "null", set(), context)[0], "c")
        # Both kinds in the pattern: one reading, every such escape read; a reading with `\\a` left as the text has it,
        # or the property escape left, takes nothing even where it gives J's answer.
        mixed = self.j("[\\p{N}]|[\\a]", ".", "null", "match:0,1", subject="1")
        partial = reading_gives({"[\\u0030-\\u0039]|[\\a]": C.canonical("match:0,1"),
                                 "[\\p{N}]|[\\x07]": C.canonical("match:0,1")})
        partial._category_units = digits
        self.assertEqual(C.classify(mixed, "null", set(), partial)[0], "c")
        self.assertEqual(partial.asked, ["[\\u0030-\\u0039]|[\\x07]"])
        full = reading_gives({"[\\u0030-\\u0039]|[\\x07]": C.canonical("match:0,1")})
        full._category_units = digits
        self.assertEqual(C.classify(mixed, "null", set(), full)[1], "M12")
        bell = self.j("[\\a]|cb", ".", "null", "match:0,1", subject="\x07")
        self.assertEqual(C.classify(bell, "null", set(), reading_gives({"[\\x07]|cb": C.canonical("match:0,1")}))[1],
                         "M12")

    def test_row_m13_reads_a_subtraction(self) -> None:
        self.assertEqual(C.subtraction_reading("[\\d-[^a]]"), "(?:(?![^a])[\\d])")
        self.assertEqual(C.subtraction_reading("[\\W-\\s-[a]]"), "(?:(?![a])[\\W\\-\\s])")
        self.assertEqual(C.subtraction_reading("[a-[b-[c]]]x"), "(?:(?!(?:(?![c])[b]))[a])x")
        self.assertIsNone(C.subtraction_reading("[-[a]]"))
        self.assertIsNone(C.subtraction_reading("[a-[b]c]"))
        self.assertIsNone(C.subtraction_reading("[a-z]"))
        d = self.j("[\\d-[^a]]", ".", "null", "match:0,1", subject="a")
        self.assertEqual(C.classify(d, "null", set(), reading_gives(
            {"(?:(?![^a])[\\d])": C.canonical("match:0,1")}))[1], "M13")
        self.assertEqual(C.classify(d, "null", set(), reading_gives({}))[0], "c")

    def test_row_m14_reads_an_escaped_dash(self) -> None:
        self.assertEqual(C.dash_reading("[\\--é]"), "[\\-\\-é]")
        self.assertEqual(C.dash_reading("[^\\--ı]x"), "[^\\-\\-ı]x")
        self.assertIsNone(C.dash_reading("[\\--]"))
        self.assertIsNone(C.dash_reading("\\--a"))
        d = self.j("[\\--é]", ".", "match:0,1", "null", subject="b")
        self.assertEqual(C.classify(d, C.canonical("match:0,1"), set(), reading_gives({"[\\-\\-é]": "null"}))[1],
                         "M14")

    def test_row_m15_is_the_culture_on_i_and_capital_i(self) -> None:
        d = diff("J.tr-TR", "exec", "I", "i", "match:0,1", "null", "i")
        d["id"] = "P4-00817-i-09"
        self.assertEqual(C.classify(d, C.canonical("match:0,1"), set(), C.Context())[1], "M15")
        # The invariant culture answers otherwise too: not this row's.
        differs = C.Context(j_differences=frozenset({"P4-00817-i-09"}))
        self.assertEqual(C.classify(d, C.canonical("match:0,1"), set(), differs)[0], "c")
        # Without i, under u, in the invariant run, or with U+0131 (row M2a's): not this row's.
        for other in (dict(d, flags="."), dict(d, flags="iu"), dict(d, run="J"), dict(d, subject=encode("iı"))):
            self.assertFalse(C.culture_casing_of_i(other, C.Context()), other)
        # No i or I in the input.
        self.assertFalse(C.culture_casing_of_i(dict(d, pattern=encode("k"), subject=encode("K")), C.Context()))

    def test_rows_m10_to_m15_come_after_m9_in_the_correspondence_s_order(self) -> None:
        # @8 (2026-11-16): rows M16 to M26 follow M15, so the slice after M9 ends at M15; the version and the
        # correspondence commit, which this test read as @7's and 44511066, are asserted by RowsM16ToM26Tests.
        names = [row for row, _, _, _ in C.ROWS]
        self.assertEqual(names[names.index("M9") + 1:names.index("M15") + 1], list(ROWS_M10_TO_M15))
        self.assertTrue(all(engine == "J" for row, engine, _, _ in C.ROWS if row in ROWS_M10_TO_M15))

    def test_an_input_row_m8_takes_stays_row_m8(self) -> None:
        # `\B` beside U+0131 (a non-ASCII letter) under i: row M8's, which comes first; the arbiter is asked nothing.
        d = diff("J", "exec", "\\BI", "i", "null", "match:1,2", "aı")
        context = reading_gives({})
        context.letters = LETTERS
        self.assertEqual(C.classify(d, "null", set(), context)[1], "M8")
        self.assertEqual(context.asked, [])


class Node:
    """A stand-in for a node of the arbiter's tree: the predicates read a node's kind by its class's name."""

    def __init__(self, **fields) -> None:
        self.__dict__.update(fields)


def node(kind_name, /, **fields):
    return type(kind_name, (Node,), {})(**fields)


def alternative(*terms):
    return node("Disjunction", alternatives=[node("Alternative", terms=list(terms))])


CHAR_A = node("Character", value=0x61)
# `(\B|a){0,2}`'s body can match both the empty string and a character; `(a){0,2}`'s cannot match empty.
NULLABLE = node("Quantified", atom=node("Group", disjunction=node("Disjunction", alternatives=[
    node("Alternative", terms=[node("Assertion", assertion="B")]), node("Alternative", terms=[CHAR_A])])),
    min=0, max=2)
NOT_NULLABLE = node("Quantified", atom=node("Group", disjunction=alternative(CHAR_A)), min=0, max=2)
ROWS_M16_TO_M26 = ("M16", "M17", "M18", "M19", "M20", "M21", "M22", "M23", "M24", "M25", "M26")


class RowsM16ToM26Tests(unittest.TestCase):
    """The predicates of @8 (2026-11-16), each over its row's own examples at Broiler.VM's 626e4ed8 and inputs just
    outside them, and the notes on rows M8, M12, M13 and M15; the arbiter's answer to a reading and its tree are
    given."""

    def j(self, pattern, flags, s="null", arm="match:0,1", subject="ab", run="J"):
        return diff(run, "exec", pattern, flags, s, arm, subject)

    def tree(self, tree, answers=None):
        context = reading_gives(answers or {})
        context._parse = lambda text: tree
        return context

    def test_the_text_s_lexing_of_a_class(self) -> None:
        tokens = [(t, inside) for _, t, inside, _ in C.text_tokens("[[a]\\b]")]
        self.assertEqual(tokens, [("[", False), ("[", True), ("a", True), ("]", True), ("\\b", False), ("]", False)])
        self.assertEqual([t for _, t, _, _ in C.text_tokens("[^^]")], ["[", "^NEG", "^", "]"])
        self.assertEqual([(t, inside) for _, t, inside, _ in C.text_tokens("[]a")],
                         [("[", False), ("]", True), ("a", False)])

    def test_row_m8_s_note_lexes_a_class_as_the_text_does(self) -> None:
        # `\b` after `[[a]` is outside a class as the text lexes it without v; @7's nesting found it inside.
        self.assertTrue(C.boundary_outside_class("[[a]\\b]", False))
        self.assertFalse(C.boundary_assertion("[[a]\\b]"))
        self.assertFalse(C.boundary_outside_class("[[a]\\b]", True))
        self.assertFalse(C.boundary_outside_class("[a\\b]", False))
        d = diff("J", "exec", "[[a]\\bé]", "", "null", "match:0,1", "aé]")
        self.assertEqual(C.classify(d, "null", set(), WITH_LETTERS)[:2], ("a", "M8"))
        # Under v a class nests, and the row's `\b` is in it.
        self.assertNotEqual(C.classify(dict(d, flags="v"), "null", set(), WITH_LETTERS)[1], "M8")

    def test_row_m12_s_note_writes_the_dash_before_a_property_escape(self) -> None:
        def digits(name):
            return list(range(0x30, 0x3A)) if name == "N" else [0x61]
        self.assertEqual(C.property_reading("[\\d-\\p{N}]", digits), "[\\d\\-\\u0030-\\u0039]")
        self.assertEqual(C.property_reading("[\\W-\\P{N}]x", digits)[:6], "[\\W\\-\\")
        # After a character, not a class escape, the dash is as @7 wrote it.
        self.assertEqual(C.property_reading("[a-\\p{N}]", digits), "[a-\\u0030-\\u0039]")

    def test_row_m13_s_note_ends_a_nested_class_at_its_first_bracket(self) -> None:
        # Right after a nested class's `[^`, under `RegexOptions.ECMAScript`, as the row's note at review narrows it.
        self.assertEqual(C.subtraction_reading("[\\w-\\D-[^]]", True), "(?:(?![^])[\\w\\-\\D])")
        # Without that option, and for a nested `[]` with it, the `]` is a member, as @7 read it: no reading.
        self.assertIsNone(C.subtraction_reading("[\\w-\\D-[^]]"))
        self.assertIsNone(C.subtraction_reading("[a-[]]", True))
        self.assertIsNone(C.subtraction_reading("[a-[]]"))
        d = self.j("[\\w-\\D-[^]]|\\S|\\s.", "d", "match:0,2", "match:0,1", subject="a]")
        read = reading_gives({"(?:(?![^])[\\w\\-\\D])|\\S|\\s.": C.canonical("match:0,1")})
        self.assertEqual(C.classify(d, C.canonical("match:0,2"), set(), read)[1], "M13")
        # Under `s`, or with a modifier group in the pattern, the flags drop the option: not row M13's.
        under_s = reading_gives({"(?:(?![^])[\\w\\-\\D])|\\S|\\s.": C.canonical("match:0,1")})
        self.assertNotEqual(C.classify(dict(d, flags="s"), C.canonical("match:0,2"), set(), under_s)[1], "M13")
        self.assertEqual(under_s.asked, [])
        group = dict(d, pattern=encode("(?i:)[\\w-\\D-[^]]|\\S|\\s."))
        with_group = reading_gives({"(?i:)(?:(?![^])[\\w\\-\\D])|\\S|\\s.": C.canonical("match:0,1")})
        self.assertNotEqual(C.classify(group, C.canonical("match:0,2"), set(), with_group)[1], "M13")
        self.assertNotIn("(?i:)(?:(?![^])[\\w\\-\\D])|\\S|\\s.", with_group.asked)
        # A `-[` that is not the class's last element still gives no reading.
        self.assertIsNone(C.subtraction_reading("[^\\W-[a]-İ]"))

    def test_row_m15_s_note_reads_the_invariant_answer_up_to_row_m10_s_names(self) -> None:
        classes = {0x3A0: (0x3A0, 0x3C0, 0x3D6), 0x3C0: (0x3A0, 0x3C0, 0x3D6), 0x3D6: (0x3A0, 0x3C0, 0x3D6)}
        s = "match:0,2;-|" + arms_names([("π", 1)])
        d = diff("J.tr-TR", "exec", "I(?<π>..\\b)*", "i", s, "null", "ii")
        d["id"] = "P4-63252-i-09"
        renamed = "match:0,2;-|" + arms_names([("[πΠϖ]", 1)])

        def context(invariant):
            return C.Context(j_differences=frozenset({d["id"]}), j_answers={d["id"]: invariant},
                             canonicalize_class=lambda u: classes.get(u, (u,)))
        self.assertEqual(C.classify(d, C.canonical(s), set(), context(renamed))[:2], ("a", "M15"))
        # Run J's answer is another match, or no answer is held: not this row's.
        self.assertEqual(C.classify(d, C.canonical(s), set(), context("match:0,1;-|" + arms_names(
            [("[πΠϖ]", 1)])))[0], "c")
        self.assertEqual(C.classify(d, C.canonical(s), set(), C.Context(j_differences=frozenset({d["id"]})))[0], "c")

    def test_row_m16_reads_the_control_escape_in_gap_scan_s_shapes(self) -> None:
        self.assertEqual(C.control_reading("\\c{0,2}[A\\c0]\\cJ"), "\\\\{0,2}[A\\\\0]\\cJ")
        self.assertIsNone(C.control_reading("\\cJ[\\ca]"))
        d = self.j("(\\c{0,2}a){0,2}", ".", "match:0,0", "match:0,1;0,1", subject="\\")
        reading = {"(\\\\{0,2}a){0,2}": C.canonical("match:0,1;0,1")}
        self.assertEqual(C.classify(d, C.canonical("match:0,0"), set(), self.tree(alternative(NULLABLE), reading))[1],
                         "M16")
        # The look-behind shape: a capturing group inside a look-behind.
        behind = node("LookAround", ahead=False, negative=False, disjunction=alternative(
            node("Group", number=1, name=None, disjunction=alternative(CHAR_A))))
        self.assertEqual(C.classify(d, C.canonical("match:0,0"), set(), self.tree(alternative(behind), reading))[1],
                         "M16")
        # Neither shape: the pattern stays on .NET, and the row takes nothing; nor where the reading's answer is not J's.
        self.assertEqual(C.classify(d, C.canonical("match:0,0"), set(), self.tree(alternative(NOT_NULLABLE),
                                                                                  reading))[0], "c")
        self.assertEqual(C.classify(d, C.canonical("match:0,0"), set(), self.tree(alternative(NULLABLE)))[0], "c")
        self.assertFalse(C.control_escape_read_as_backslash(dict(d, flags="u"), self.tree(alternative(NULLABLE),
                                                                                          reading)))

    def test_rows_m17_and_m18_read_a_braced_name(self) -> None:
        d = self.j(".(?<\\u{62}>\\xdf*|[\\D&&\\w])*[]{0,2}", "")
        self.assertTrue(C.braced_name_in_nullable_repeat(d, self.tree(alternative(NULLABLE))))
        self.assertFalse(C.braced_name_in_nullable_repeat(d, self.tree(alternative(NOT_NULLABLE))))
        self.assertFalse(C.braced_name_in_nullable_repeat(self.j("(?<b>a)*", ""), self.tree(alternative(NULLABLE))))
        self.assertFalse(C.braced_name_in_nullable_repeat(self.j("[(?<\\u{62}>]a*", ""),
                                                          self.tree(alternative(NULLABLE))))
        self.assertTrue(C.braced_name_reference(self.j("((?<b>.\\k<\\u{62}>{2}\\u03a3*?)*)$", ""), C.Context()))
        # A reference whose name names no group, one written without `\u{…}`, or one in a class: not row M18's.
        self.assertFalse(C.braced_name_reference(self.j("(?<b>.)\\k<\\u{63}>", ""), C.Context()))
        self.assertFalse(C.braced_name_reference(self.j("(?<b>.)\\k<b>", ""), C.Context()))
        self.assertFalse(C.braced_name_reference(self.j("(?<b>.)[\\k<\\u{62}>]", ""), C.Context()))

    def test_rows_m19_and_m20_read_a_legacy_octal_escape(self) -> None:
        self.assertEqual(C._octal_marked("(\\B\\07{0,2}){0,2}"), "(\\B\\uE000{0,2}){0,2}")
        self.assertEqual(C._octal_marked("\\077a"), "\\uE000a")
        self.assertIsNone(C._octal_marked("[\\07]\\0"))
        marked = node("Quantified", atom=node("Group", disjunction=alternative(
            node("Assertion", assertion="B"), node("Quantified", atom=node("Character", value=0xE000), min=0, max=2))),
            min=0, max=2)
        d = self.j("(\\B\\07{0,2}){0,2}", "d")
        self.assertTrue(C.octal_in_nullable_repeat(d, self.tree(alternative(marked))))
        # The escape outside the quantifier's body: not row M19's.
        self.assertFalse(C.octal_in_nullable_repeat(d, self.tree(alternative(NULLABLE, node("Character",
                                                                                            value=0xE000)))))
        self.assertTrue(C.octal_before_digit_class(self.j("\\xa0|[\\07\\d]?", ""), C.Context()))
        self.assertFalse(C.octal_before_digit_class(self.j("[\\077\\d]", ""), C.Context()))
        self.assertFalse(C.octal_before_digit_class(self.j("\\07\\d", ""), C.Context()))

    def test_row_m21_reads_the_non_white_space_class_escape(self) -> None:
        nws = C.NON_WHITE_SPACE_AS_NET_READS
        self.assertEqual(C.non_white_space_reading("[\\S\\xe9]*"), "[" + nws + "\\xe9]*")
        self.assertEqual(C.non_white_space_reading("[a-\\S]"), "[a\\-" + nws + "]")
        self.assertEqual(C.non_white_space_reading("[\\S-a]"), "[" + nws + "\\-a]")
        self.assertIsNone(C.non_white_space_reading("\\S[a]"))
        d = self.j("[\\S\\xe9]*", "", "match:0,0", "match:0,1", subject=" ")
        reading = {"[" + nws + "\\xe9]*": C.canonical("match:0,1")}
        self.assertEqual(C.classify(d, C.canonical("match:0,0"), set(), reading_gives(reading))[1], "M21")
        # Under s, or with a modifier group, .NET has no RegexOptions.ECMAScript: not row M21's.
        self.assertFalse(C.non_white_space_in_class(dict(d, flags="s"), reading_gives(reading)))
        self.assertFalse(C.non_white_space_in_class(dict(d, pattern=encode("[\\S\\xe9]*(?i:a)?")),
                                                    reading_gives(reading)))

    def test_row_m22_reads_an_empty_group_under_a_lazy_quantifier(self) -> None:
        self.assertTrue(C.empty_group_lazy(self.j("^\\u017f{1,}?|(?<\\u0061>)+?\\U00010428", ""), C.Context()))
        self.assertTrue(C.empty_group_lazy(self.j("\\n(?<=^)\\u2028|(?:()+?){\\B", ""), C.Context()))
        # One alternative, a `|` inside a group, or the group in a class: not row M22's.
        self.assertFalse(C.empty_group_lazy(self.j("()+?a", ""), C.Context()))
        self.assertFalse(C.empty_group_lazy(self.j("(a|()+?)", ""), C.Context()))
        self.assertFalse(C.empty_group_lazy(self.j("[()+?]|a", ""), C.Context()))
        self.assertFalse(C.empty_group_lazy(self.j("()*?|a", ""), C.Context()))

    def test_row_m23_reads_an_anchor_in_a_class_under_m(self) -> None:
        self.assertEqual(C.anchor_member_reading("[^^]"), "[^(?<=^|\\r)]")
        self.assertEqual(C.anchor_member_reading("[a$]"), "[a(?=$|\\r)]")
        self.assertIsNone(C.anchor_member_reading("^[^a]$"))
        d = self.j("[^^]", "m", "match:0,1", "null", subject="(")
        reading = {"[^(?<=^|\\r)]": "null"}
        self.assertEqual(C.classify(d, C.canonical("match:0,1"), set(), reading_gives(reading))[1], "M23")
        self.assertEqual(C.classify(dict(d, flags="."), C.canonical("match:0,1"), set(), reading_gives(reading))[0],
                         "c")

    def test_row_m24_reads_a_back_reference_without_ecmascript(self) -> None:
        reference = alternative(node("Group", number=1, name=None, disjunction=alternative()),
                                node("Backreference", numbers=(1,), name=None))
        d = self.j("()\\1a", "s", "match:0,1;0,0", "null", subject="a")
        self.assertEqual(C.classify(d, C.canonical("match:0,1;0,0"), set(), self.tree(reference))[1], "M24")
        self.assertTrue(C.backreference_without_ecmascript(dict(d, flags=".", pattern=encode("()\\1a(?i:b)?")),
                                                           self.tree(reference)))
        # Without s or a modifier group, or with no back-reference in the tree: not row M24's.
        self.assertFalse(C.backreference_without_ecmascript(dict(d, flags="."), self.tree(reference)))
        self.assertFalse(C.backreference_without_ecmascript(d, self.tree(alternative(CHAR_A))))

    def test_row_m25_reads_a_letter_expanded_inside_a_group_without_i(self) -> None:
        sigma = (0x3A3, 0x3C2, 0x3C3)

        def classes(unit):
            return sigma if unit in sigma else (unit,)
        self.assertEqual(C.expanded_letter_reading("(?m-is:Σ)", classes), "(?m-is:[Σςσ])")
        self.assertIsNone(C.expanded_letter_reading("Σ(?i:Σ)", classes))
        self.assertIsNone(C.expanded_letter_reading("(?-i:x[Σ])", classes))
        d = self.j("(?m-is:Σ)", "i", "null", "match:0,1", subject="σ")
        context = reading_gives({"(?m-is:[Σςσ])": C.canonical("match:0,1")})
        context._canonicalize_class = classes
        self.assertEqual(C.classify(d, "null", set(), context)[1], "M25")
        self.assertEqual(C.classify(dict(d, flags="."), "null", set(), context)[0], "c")

    def test_row_m26_reads_a_class_under_i(self) -> None:
        for pattern in ("[^\\xa0-\\W].", "[\\P{Ll}]", "s[\\W]{1,}K", "[\\B-\\s\\P{Ll}].+"):
            self.assertTrue(C.class_folded_by_net(self.j(pattern, "i"), C.Context()), pattern)
        for pattern, flags in (("[\\W]", "."), ("\\W", "i"), ("[\\p]", "i"), ("[\\W]", "iu")):
            self.assertFalse(C.class_folded_by_net(self.j(pattern, flags), C.Context()), pattern)

    def test_rows_m16_to_m26_come_after_m15_in_the_correspondence_s_order(self) -> None:
        # @9 (2026-11-17): row M27 follows M26, so the slice after M15 ends at M26; the version and the correspondence
        # commit, which this test read as @8's and 626e4ed8, are asserted by RowM27Tests.
        names = [row for row, _, _, _ in C.ROWS]
        self.assertEqual(names[names.index("M15") + 1:names.index("M26") + 1], list(ROWS_M16_TO_M26))
        self.assertTrue(all(engine == "J" for row, engine, _, _ in C.ROWS if row in ROWS_M16_TO_M26))


INFINITY = float("inf")
NON_DIGIT = node("ClassEscape", kind="D")
BOUNDARY = node("Assertion", kind="b")


def loop(atom, minimum=1, maximum=INFINITY, greedy=False):
    return node("Quantified", atom=atom, min=minimum, max=maximum, greedy=greedy)


def negated(*items):
    return node("ClassRanges", items=list(items), invert=True)


def char(c):
    return node("Character", value=ord(c))


def group(*terms):
    return node("Group", number=1, name=None, disjunction=alternative(*terms))


class RowM27Tests(unittest.TestCase):
    """The predicate of @9 (2026-11-17), row M27 at Broiler.VM's 5e90bfe4, over the row's own examples and inputs just
    outside them; the arbiter's tree and its answer to a reading are given."""

    def j(self, pattern, flags, s, arm, subject, run="J"):
        return diff(run, "exec", pattern, flags, s, arm, subject)

    def context(self, tree, answers=None):
        context = reading_gives(answers or {})
        context._parse = lambda text: tree
        return context

    def test_the_row_s_atoms_and_loops(self) -> None:
        # `\D`, or a negated class of ASCII letters, digits, `_`, `\d` or ranges within 0-9, A-Z or a-z.
        for atom in (NON_DIGIT, negated((char("0"), char("9"))), negated(char("_"), node("ClassEscape", kind="d")),
                     negated((char("a"), char("z")), (char("A"), char("Z")), char("5"))):
            self.assertTrue(C.word_set_loop(loop(atom)), atom.__dict__)
        # Not a class escape other than `\D`, a class not negated, a non-word member, a range across runs or a class
        # escape in a range; nor a loop with a minimum of 0 or a maximum.
        for atom in (node("ClassEscape", kind="d"), node("ClassEscape", kind="W"),
                     node("ClassRanges", items=[char("a")], invert=False), negated(char("!")),
                     negated((char("0"), char("z"))), negated((node("ClassEscape", kind="d"), char("z"))), negated()):
            self.assertFalse(C.word_set_loop(loop(atom)), atom.__dict__)
        self.assertFalse(C.word_set_loop(loop(NON_DIGIT, minimum=0)))
        self.assertFalse(C.word_set_loop(loop(NON_DIGIT, maximum=5)))
        self.assertTrue(C.word_set_loop(loop(NON_DIGIT, minimum=2, greedy=True)))

    def test_the_loop_is_reached_through_groups_and_optional_terms(self) -> None:
        lazy = loop(NON_DIGIT)
        # `\ud83d*((?<=s* )|\D+?|...)?\b`: through an optional group's alternatives to the last term of each.
        optional = loop(node("Group", number=1, name=None, disjunction=node("Disjunction", alternatives=[
            node("Alternative", terms=[node("LookAround", ahead=False, negative=False, disjunction=alternative())]),
            node("Alternative", terms=[lazy])])), minimum=0, maximum=1, greedy=True)
        self.assertEqual(C.loops_before_word_boundary(alternative(optional, BOUNDARY)), [lazy])
        self.assertEqual(C.loops_before_word_boundary(alternative(node("Group", number=0, name=None,
                                                                       disjunction=alternative(char("a"), lazy)),
                                                                  BOUNDARY)), [lazy])
        # A group that does not capture, `(?:…)`, which the arbiter's tree writes as a Modifiers with no flag.
        self.assertEqual(C.loops_before_word_boundary(alternative(node("Modifiers", add="", remove="",
                                                                       disjunction=alternative(lazy)), BOUNDARY)),
                         [lazy])
        # Not through a look-around, a modifier group or a repeated group, and not a loop that is not the last term.
        for term in (node("LookAround", ahead=True, negative=False, disjunction=alternative(lazy)),
                     node("Modifiers", add="i", remove="", disjunction=alternative(lazy)),
                     loop(group(lazy), minimum=0, maximum=2), group(lazy, char("a"))):
            self.assertEqual(C.loops_before_word_boundary(alternative(term, BOUNDARY)), [], term.__dict__)

    def test_a_loop_inside_a_look_behind_is_named_as_the_row_reads(self) -> None:
        # Pinned at review, 2026-11-17: the row's "the quantifier is a term of an alternative" makes no exception for
        # the alternatives of a look-behind, so @9 names a loop before `\b` there and reads it, though .NET, which
        # runs a look-behind right to left, makes no such loop atomic (FindAndMakeLoopsAtomic at v10.0.15). Only
        # answered_as_read keeps the row from taking such an input; no input of task 2.13's runs has the shape.
        for negative, text, reading in ((False, "(?<=\\D+?\\b)x", "(?<=\\D+(?!\\D)\\b)x"),
                                        (True, "(?<!\\D+\\b)x", "(?<!\\D+(?!\\D)\\b)x")):
            lazy = loop(NON_DIGIT, greedy=negative)
            tree = alternative(node("LookAround", ahead=False, negative=negative,
                                    disjunction=alternative(lazy, BOUNDARY)), char("x"))
            self.assertEqual(C.loops_before_word_boundary(tree), [lazy], text)
            self.assertEqual(C.atomic_loop_reading(text, tree), reading)

    def test_the_term_after_the_loop_begins_with_a_word_boundary(self) -> None:
        lazy = loop(NON_DIGIT)
        for after in (BOUNDARY, group(BOUNDARY, char("a")),
                      node("LookAround", ahead=True, negative=False, disjunction=alternative(BOUNDARY)),
                      loop(group(BOUNDARY), minimum=1, maximum=3),
                      node("Modifiers", add="", remove="", disjunction=alternative(BOUNDARY))):
            self.assertEqual(C.loops_before_word_boundary(alternative(lazy, after)), [lazy], after.__dict__)
        for after in (node("Assertion", kind="B"), char("a"),
                      node("LookAround", ahead=True, negative=True, disjunction=alternative(BOUNDARY)),
                      node("LookAround", ahead=False, negative=False, disjunction=alternative(BOUNDARY)),
                      node("Group", number=1, name=None, disjunction=node("Disjunction", alternatives=[
                          node("Alternative", terms=[BOUNDARY]), node("Alternative", terms=[char("a")])])),
                      loop(group(BOUNDARY), minimum=0, maximum=1),
                      node("Modifiers", add="i", remove="", disjunction=alternative(BOUNDARY))):
            self.assertEqual(C.loops_before_word_boundary(alternative(lazy, after)), [], after.__dict__)

    def test_the_row_s_reading(self) -> None:
        self.assertEqual(C.atomic_loop_reading("\\D+?\\b", alternative(loop(NON_DIGIT), BOUNDARY)), "\\D+(?!\\D)\\b")
        tree = alternative(loop(negated((char("0"), char("9"))), minimum=2), BOUNDARY)
        self.assertEqual(C.atomic_loop_reading("[^0-9]{2,}?\\b", tree), "[^0-9]{2,}(?![^0-9])\\b")
        # Only the loops before `\b` are read; another loop of the row's atoms is left as written.
        tree = alternative(loop(NON_DIGIT, greedy=True), char("a"), loop(NON_DIGIT, greedy=True), BOUNDARY)
        self.assertEqual(C.atomic_loop_reading("\\D+a\\D+\\b", tree), "\\D+a\\D+(?!\\D)\\b")
        # No loop before `\b`, or the tree's loops and the text's do not pair one for one: no reading.
        self.assertIsNone(C.atomic_loop_reading("\\D+a\\b", alternative(loop(NON_DIGIT), char("a"), BOUNDARY)))
        self.assertIsNone(C.atomic_loop_reading("[^\\x30]+\\b", alternative(loop(negated(char("0"))), BOUNDARY)))
        self.assertEqual(C.text_loops("[^a-z_\\d]{1,}?x\\D{0,}[\\D]+"), [(0, 14, "[^a-z_\\d]", "{1,}")])

    def test_row_m27_takes_an_input_whose_reading_gives_arm_j_s_answer(self) -> None:
        # `\D+?\b` on `a!b`: the text's lazy loop stops before `!`; .NET's atomic loop runs to the end.
        tree = alternative(loop(NON_DIGIT), BOUNDARY)
        d = self.j("\\D+?\\b", "s", "match:0,1", "match:0,3", "a!b")
        answers = {"\\D+(?!\\D)\\b": C.canonical("match:0,3")}
        context = self.context(tree, answers)
        self.assertEqual(C.classify(d, C.canonical("match:0,1"), set(), context)[:2], ("a", "M27"))
        self.assertEqual(context.asked, ["\\D+(?!\\D)\\b"])
        for run in ("J.tr-TR", "J.lt-LT"):
            self.assertEqual(C.classify(dict(d, run=run), C.canonical("match:0,1"), set(),
                                        self.context(tree, answers))[1], "M27")
        # With a modifier group the text lexes, and no `s`: RegexOptions.ECMAScript is dropped too.
        grouped = self.j("\\D+?\\b(?i:x)?", ".", "match:0,1", "match:0,3", "a!b")
        tree_grouped = alternative(loop(NON_DIGIT), BOUNDARY, loop(node("Modifiers", add="i", remove="",
                                                                     disjunction=alternative(char("x"))), 0, 1))
        self.assertEqual(C.classify(grouped, C.canonical("match:0,1"), set(), self.context(
            tree_grouped, {"\\D+(?!\\D)\\b(?i:x)?": C.canonical("match:0,3")}))[1], "M27")

    def test_row_m27_takes_nothing_outside_its_condition(self) -> None:
        tree = alternative(loop(NON_DIGIT), BOUNDARY)
        answers = {"\\D+(?!\\D)\\b": C.canonical("match:0,3")}
        d = self.j("\\D+?\\b", "s", "match:0,1", "match:0,3", "a!b")
        # Without `s` or a modifier group (`(?i:` inside a class is none), under `u` or `v`, or not an exec both answer.
        for other in (dict(d, flags="."), dict(d, flags="m"), dict(d, pattern=encode("\\D+?\\b[(?i:x)]?")),
                      dict(d, flags="su"), dict(d, flags="sv"), dict(d, arm="limit:harness")):
            self.assertFalse(C.loop_made_atomic_before_boundary(other, self.context(tree, answers)), other)
        # The reading's answer is not arm J's, or is the text's.
        self.assertEqual(C.classify(d, C.canonical("match:0,1"), set(), self.context(tree))[0], "c")
        self.assertEqual(C.classify(d, C.canonical("match:0,1"), set(), self.context(
            tree, {"\\D+(?!\\D)\\b": C.canonical("match:0,1")}))[0], "c")
        # A refused pattern (no tree), or a tree with no loop before `\b`.
        self.assertFalse(C.loop_made_atomic_before_boundary(d, self.context(None, answers)))
        self.assertEqual(C.classify(d, C.canonical("match:0,1"), set(), self.context(
            alternative(loop(NON_DIGIT), node("Assertion", kind="B")), answers))[0], "c")
        # Arm V is not the row's.
        self.assertEqual(C.classify(dict(d, run="V"), C.canonical("match:0,1"), set(), self.context(tree, answers))[0],
                         "c")

    def test_an_input_row_m8_takes_stays_row_m8(self) -> None:
        # `\D+\b` beside U+00E9 (a non-ASCII letter) under s: row M8's, which comes first; the arbiter is asked nothing.
        d = self.j("\\D+\\b", "s", "match:0,1", "match:0,3", "é!b")
        context = self.context(alternative(loop(NON_DIGIT, greedy=True), BOUNDARY),
                               {"\\D+(?!\\D)\\b": C.canonical("match:0,3")})
        context.letters = LETTERS
        self.assertEqual(C.classify(d, C.canonical("match:0,1"), set(), context)[1], "M8")
        self.assertEqual(context.asked, [])

    def test_row_m27_comes_after_m26(self) -> None:
        names = [row for row, _, _, _ in C.ROWS]
        self.assertEqual(names[-2:], ["M26", "M27"])
        self.assertEqual([engine for row, engine, _, _ in C.ROWS if row == "M27"], ["J"])
        self.assertEqual(C.CORRESPONDENCE["commit"], "5e90bfe485aad60b3637d9f69e54ed71adcd73af")
        self.assertEqual(C.CORRESPONDENCE["blob"], "2831290201cca3fb6b524a9be8680755eb566f37")
        self.assertEqual(C.CLASSIFIER_VERSION, "sjl-11-001-classification@9")


class BeyondByRowTests(unittest.TestCase):
    """The rule's revision of 2026-11-16c (the owner's ruling on P3's `O06`): an input the arbiter cannot reach, named
    by a row, classed by that row where the revision's three conditions hold."""

    O06 = "a" * 10_000_001

    def o06(self, run="J", s="match:0,10000001", arm="null", subject=None):
        d = diff(run, "exec", "^[a]*$", "u", s, arm, self.O06 if subject is None else subject)
        d["id"], d["class"] = "P3-O06", "P3"
        return d

    def context(self, others, prefix=999992, at_prefix=None):
        answer = at_prefix or f"match:0,{prefix}"
        prefixes = {"P3-O06": (prefix, C.parse_rendered(answer))}
        reduction = C.Reduction(prefixes, {}, {f"P3-O06~p{prefix}": f"match:0,{prefix}"})
        return C.Context(reduction=reduction, other_answers=lambda d: others)

    def test_the_stated_check_of_condition_3(self) -> None:
        self.assertEqual(C.whole_run_answer("^[a]*$", "u", [0x61] * 5, "0"), ("match", ((0, 5),), ()))
        self.assertEqual(C.whole_run_answer("^[ab]*$", "", [0x61, 0x62, 0x62], "0"), ("match", ((0, 3),), ()))
        self.assertEqual(C.whole_run_answer("^a*$", "d", [], "0"), ("match", ((0, 0),), ()))
        for pattern, flags, subject in (("^[a]*$", "iu", [0x61]), ("^[a]*$", "mu", [0x61]), ("^[a]*$", "u", [0x62]),
                                        ("^[^a]*$", "u", [0x62]), ("^[a-c]*$", "u", [0x62]), ("[a]*", "u", [0x61]),
                                        ("^[a]+$", "u", [0x61])):
            self.assertIsNone(C.whole_run_answer(pattern, flags, subject, "0"), (pattern, flags))
        self.assertIsNone(C.whole_run_answer("^[a]*$", "gu", [0x61], "0"))

    def test_o06_is_row_b1_s_where_every_condition_holds(self) -> None:
        category, row, why = C.classify(self.o06(), "arbiter:exhausted", set(), self.context({"V": "match:0,10000001"}))
        self.assertEqual((category, row), ("a", "B1"))
        self.assertIn("beyond the arbiter, by row B1", why)
        self.assertIn("P3-O06~p999992", why)
        for run in ("J.tr-TR", "J.lt-LT"):
            self.assertEqual(C.classify(self.o06(run), "arbiter:exhausted", set(),
                                        self.context({"V": "match:0,10000001"}))[:2], ("a", "B1"))

    def test_o06_stays_c_where_a_condition_fails(self) -> None:
        agree = {"V": "match:0,10000001"}
        # (2): no other arm answers, one answers otherwise, or one does not hold the case.
        for others in ({}, {"V": "null"}, {"V": None}, {"V": "match:0,10000001", "V.x": "match:0,1"}):
            self.assertEqual(C.classify(self.o06(), "arbiter:exhausted", set(), self.context(others))[:2], ("c", "-"))
        # (3): S's answer is not the whole subject, or the prefix's agreed answer is not what the check gives there.
        self.assertEqual(C.classify(self.o06(s="match:0,5"), "arbiter:exhausted", set(), self.context(agree))[0], "c")
        self.assertEqual(C.classify(self.o06(), "arbiter:exhausted", set(), self.context(
            agree, at_prefix="match:0,999991"))[0], "c")
        # (1): an input no row names (a shorter run of `a`), or arm V's difference (B1 is read for arm J here).
        self.assertEqual(C.classify(self.o06(s="match:0,40", subject="a" * 40), "arbiter:exhausted", set(),
                                    self.context({"V": "match:0,40"}, prefix=17))[0], "c")
        self.assertEqual(C.classify(self.o06(run="V"), "arbiter:exhausted", set(), self.context(
            {"J": "match:0,10000001"}))[0], "c")
        # Without the reduction, the rule as filed: class (c).
        self.assertEqual(C.classify(self.o06(), "arbiter:exhausted", set(), C.Context(
            other_answers=lambda d: agree))[0], "c")

    def test_the_row_s_words_and_the_other_arms_answers_are_read(self) -> None:
        import tempfile
        from population_format import open_text
        self.assertEqual(C.check_beyond_rows({"B1": "| B1 | " + B1_WORDS + " |"}), [])
        self.assertTrue(C.check_beyond_rows({"B1": "| B1 | A greedy run | Correction of Broiler.JS |"}))
        self.assertTrue(C.check_beyond_rows({}))
        with tempfile.TemporaryDirectory() as tmp:
            runs = Path(tmp)
            for run, answer in (("S", "match:0,3"), ("V", "match:0,3"), ("J", "null"), ("J.tr-TR", "null")):
                with open_text(runs / f"P3.{run}.tsv.gz", "wt") as out:
                    out.write(f"# id\tanswer\tcharge\nP3-N01\tnull\t1\nP3-O06\t{answer}\t3\n")
            d = self.o06(run="J.tr-TR")
            self.assertEqual(C.other_arm_answers(runs, d), {"V": "match:0,3"})
            d["id"] = "P3-O99"
            self.assertEqual(C.other_arm_answers(runs, d), {"V": None})


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
        # @8: row B1's words naming P3's `O06` and the mechanism (the revision of 2026-11-16c's condition (1)).
        self.assertEqual(C.check_beyond_rows(C.read_row_lines(Path(path))), [])


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
# The rows added at Broiler.VM's 44511066 (@7), as their classes open.
ADDED_ROWS.update({row: "**Correction of Broiler.JS**; no test262 row is known to move for it"
                   for row in ROWS_M10_TO_M15})
# The rows added at Broiler.VM's 626e4ed8 (@8), as their classes open; and row B1, whose widening the revision of
# 2026-11-16c reads (its class, and its words naming P3's `O06` and the mechanism, as check_beyond_rows reads them).
ADDED_ROWS.update({row: "**Correction of Broiler.JS and of Broiler.Regex's preview.4**; no test262 row is known to "
                        "move for it" for row in ("M16", "M17", "M18", "M19")})
ADDED_ROWS.update({row: "**Correction of Broiler.JS**; no test262 row is known to move for it"
                   for row in ("M20", "M21", "M22", "M23", "M24", "M25", "M26")})
# The row added at Broiler.VM's 5e90bfe4 (@9), as its class opens.
ADDED_ROWS["M27"] = "**Correction of Broiler.JS**; no test262 row is known to move for it"
B1_WORDS = ("reached `/^[a]*$/u` over 10,000,001 `a` (`P3-O06`) ...: the class's run ends at the budget's early exit "
            "this row cites (R1). Broiler.JS: **correction of Broiler.JS and of Broiler.Regex's preview.4** (R1)")
ADDED_ROWS["B1"] = B1_WORDS

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
