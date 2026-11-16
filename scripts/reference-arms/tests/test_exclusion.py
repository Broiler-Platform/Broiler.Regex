# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""The exclusion from population P4 by the owner's ruling of 2026-11-16: the pattern's limb read from the pattern's
text, the arbiter's limb as select reads it, and the listing classify.py --exclude reads. Written 2026-11-16, after
task 2.13's runs, their classification and the reduction of 2026-11-15 were seen."""

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

import classify as C  # noqa: E402
import exclusion as X  # noqa: E402
from population_format import decode_units, encode, open_text, text_of  # noqa: E402

# The 19 patterns of p4-excluded.tsv, each with its flags and the quantifier the pattern's limb finds.
NINETEEN = {
    "P4-01347": ("((?<!$-{1,})){4294967296}\u03a3{0,2}|\\u{10400}\\x6b*k", "((?<!$-{1,})){4294967296}"),
    "P4-01872": ("\u0130|(K?\ud83d+]|(?!\\q|\\d){4294967296}\ud83d(?<=(?=\u0131\\k+)))*", "(?!\\q|\\d){4294967296}"),
    "P4-02466": ("(?<\\u{62}>){4294967296}(?<!(?<![^\\w&&[^]&&\\d]|^))B?", "(?<\\u{62}>){4294967296}"),
    "P4-09242": ("(?:\\b){4294967296}S\\b[\\sK\\n]", "(?:\\b){4294967296}"),
    "P4-11265": ("(?<\\u{62}>(?<!\\B\\b)){4294967296}|(?=\\b|)+?1|\\0\\B", "(?<\\u{62}>(?<!\\B\\b)){4294967296}"),
    "P4-11714": ("(?:^){4294967296}?(?<\\u0061>-){2}[^[]{4294967296}", "(?:^){4294967296}?"),
    "P4-14530": ("[\u03c3-\\S\u212a]{0,2}?(?!\u0131{0,2}\u1e9e){4294967296}K", "(?!\u0131{0,2}\u1e9e){4294967296}"),
    "P4-17689": ("(?:){4294967296}\\P{L}{4294967296}", "(?:){4294967296}"),
    "P4-18962": ("\\s(?!\ude00\ude00{0,2}?)|,\\b|()(?<\\u0061>){4294967296}$", "(?<\\u0061>){4294967296}"),
    "P4-29456": ("(?<\U0001d400>\\n$)|(?:0?\n??){4294967296}($\\b){2}|.+s", "(?:0?\n??){4294967296}"),
    "P4-29657": ("1[^]|(?!\\B(?:|)){4294967296}\ud83d", "(?!\\B(?:|)){4294967296}"),
    "P4-30238": ("(?:(?<=^)){4294967296}?a{1,}|\\_", "(?:(?<=^)){4294967296}?"),
    "P4-33179": ("(?=,\xe9|((\\S{0,2}\\S*){4294967296}?){2}[[\xb5]_]\u1e9e{0,2})", "(\\S{0,2}\\S*){4294967296}?"),
    "P4-40469": ("($){4294967296}\\uD83D\\uDE00+.", "($){4294967296}"),
    "P4-42576": ("(?=){4294967296}|(?!\u03c3{0,2}(?<\u03c0>\\Ba)\\cJ+)B\xa0[]*?|\u03a3\\d\u03a3", "(?=){4294967296}"),
    "P4-54314": (".??(?-i:$){4294967296}", "(?-i:$){4294967296}"),
    "P4-58688": ("[--\xa0\\S]|$S{\\B|(?m:[s\\D]{0,2}^){4294967296}", "(?m:[s\\D]{0,2}^){4294967296}"),
    "P4-63268": ("(?=(?=)){4294967296}(|$\\b.i)", "(?=(?=)){4294967296}"),
    "P4-65213": ("((?!$(?!_*\ud83d\\uD83D\\uDE00\xdf){4294967296}?\\uD83D\\uDE00)\ud83d|,\\2|K\\p{ID_Start}"
                 "(?<a>(\\r)))\\s|\u2028", "(?!_*\ud83d\\uD83D\\uDE00\xdf){4294967296}?"),
}


def limb(pattern: str, flags: str = "") -> list[str]:
    return [q.text for q in X.empty_body_minimum(pattern, flags or ".")]


class PatternLimbTests(unittest.TestCase):
    def test_a_huge_minimum_over_an_atom_that_can_match_empty(self) -> None:
        for pattern, quantifier in NINETEEN.values():
            self.assertEqual(limb(pattern), [quantifier], pattern)
        # Lazy, a group of an empty alternative, optional terms, a back-reference, a look-behind.
        self.assertEqual(limb("(a|){4294967296}"), ["(a|){4294967296}"])
        self.assertEqual(limb("(a)(?:\\1){4294967296}"), ["(?:\\1){4294967296}"])
        self.assertEqual(limb("(?<!\\B\\b){4294967296}"), ["(?<!\\B\\b){4294967296}"])
        self.assertEqual(limb("(?:){4294967296,}"), ["(?:){4294967296,}"])

    def test_not_the_limb(self) -> None:
        for pattern in ("a{4294967296}", "[^[]{4294967296}", "\\P{L}{4294967296}", "(?:a|b){4294967296}",
                        "(?:){4294967295}", "(?:){0,4294967296}", "(?:)*", "[(?:){4294967296}]",
                        "\\((?:a){4294967296}", "(?:\\1){4294967296}", "\\u{10400}"):
            self.assertEqual(limb(pattern), [], pattern)

    def test_read_under_the_flags(self) -> None:
        # Without u, `\k` with no group name is a `k`, and `\1` with no group is an octal escape; with u, a reference.
        self.assertEqual(limb("(?:\\k<a>){4294967296}"), [])
        self.assertEqual(limb("(?:\\k<a>){4294967296}(?<a>)", "u"), ["(?:\\k<a>){4294967296}"])
        # Under v a class can hold the empty string; under u `[\q{}]` is no such class.
        self.assertEqual(limb("[\\q{}]{4294967296}", "v"), ["[\\q{}]{4294967296}"])
        self.assertEqual(limb("[\\q{a|}]{4294967296}", "v"), ["[\\q{a|}]{4294967296}"])
        self.assertEqual(limb("[\\q{}]{4294967296}", "u"), [])
        self.assertEqual(limb("[^\\q{}]{4294967296}", "v"), [])
        # Nested classes under v, a literal `[` in a class otherwise.
        self.assertEqual(limb("[[a]](?:){4294967296}", "v"), ["(?:){4294967296}"])

    def test_deep_nesting_is_no_recursion(self) -> None:
        deep = "(" * 20000 + ")" * 20000 + "{4294967296}"
        self.assertEqual(limb(deep), [deep])


class HelperTests(unittest.TestCase):
    def test_pattern_id(self) -> None:
        self.assertEqual(X.pattern_id("P4-01347-d-02"), "P4-01347")
        self.assertEqual(X.pattern_id("P4-01347-none"), "P4-01347")
        self.assertEqual(X.pattern_id("P4-04713-d-02~p8"), "P4-04713")
        self.assertEqual(X.pattern_id("P1-regexp:budget:k-catastrophic:0001"), "P1-regexp:budget:k-catastrophic:0001")

    def test_shortest_prefix(self) -> None:
        self.assertEqual(X.shortest_prefix(encode("abcdef"), "y", "3"), encode("abc"))
        self.assertEqual(X.shortest_prefix(encode("abcdef"), ".", "3"), ".")
        self.assertEqual(X.shortest_prefix(encode("abcdef"), "d", "3"), ".")

    def test_read_listing(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "listing.tsv"
            path.write_text("# id\tpattern\tquantifier\tflags\nP4-00001\t0061\t0061\t.\n", encoding="utf-8")
            self.assertEqual(X.read_listing(path), {"P4-00001": "0061"})
            for bad in ("P1-x\t0061\t0061\t.\n", "P4-00001\t0061\n", "P4-00001\t0061\t0061\t.\n" * 2):
                path.write_text(bad, encoding="utf-8")
                with self.assertRaises(ValueError):
                    X.read_listing(path)


class FiledListingTests(unittest.TestCase):
    """p4-excluded.tsv, as filed with the rule's revision: the 19 patterns, each with the pattern's limb."""

    def test_the_filed_listing(self) -> None:
        listing = HERE / "p4-excluded.tsv"
        listed = X.read_listing(listing)
        self.assertEqual(len(listed), 19)
        for pid, (pattern, quantifier) in NINETEEN.items():
            self.assertEqual(text_of(decode_units(listed[pid])), pattern, pid)
        for line in listing.read_text(encoding="utf-8").splitlines()[1:]:
            pid, pattern, quantifier, flags = line.split("\t")
            texts = [q.text for f in flags.split(",") for q in X.empty_body_minimum(text_of(decode_units(pattern)), f)]
            self.assertIn(text_of(decode_units(quantifier)), texts, pid)


class _Arbiter:
    """classify.Arbiter as select uses it, every answer from its cache."""
    answers: dict = {}

    def __init__(self, *args) -> None:
        self.cache = dict(self.answers)

    def load(self, path) -> None:
        pass

    def save(self, path) -> None:
        pass


class SelectTests(unittest.TestCase):
    def test_the_three_limbs_and_p4_alone(self) -> None:
        cases = {
            "P4": [("P4-00001-none", "accept", "(?:){4294967296}a", ".", "-", "-"),
                   ("P4-00001-none-00", "exec", "(?:){4294967296}a", ".", "ab", "0"),
                   ("P4-00001-y-00", "exec", "(?:){4294967296}a", "y", "ab", "1"),
                   ("P4-00002-none-00", "exec", "(?=b){4294967296}a", ".", "ab", "0"),
                   ("P4-00003-none-00", "exec", "a{4294967296}", ".", "ab", "0"),
                   ("P4-00004-none-00", "exec", "($){4294967296}", ".", "ab", "0")],
            "P1": [("P1-v:1", "exec", "($){4294967296}", ".", "x", "0")],
        }
        differences = [
            # The pattern's and the arbiter's limbs and a stop against an answer: excluded, under "" alone.
            ("P4-00001-none-00", "P4", "J", "(?:){4294967296}a", ".", "ab", "0", "limit:steps", "null"),
            # Both sides stop under y: not the third limb.
            ("P4-00001-y-00", "P4", "J", "(?:){4294967296}a", "y", "ab", "1", "limit:steps", "limit:harness"),
            # A stop against an answer where the arbiter reaches the prefix: not the second limb.
            ("P4-00002-none-00", "P4", "J", "(?=b){4294967296}a", ".", "ab", "0", "limit:steps", "null"),
            # The first two limbs and both sides stopping: not excluded, listed as the first two alone.
            ("P4-00004-none-00", "P4", "V", "($){4294967296}", ".", "ab", "0", "limit:steps", "limit:ceiling"),
            # Another class: printed, never written.
            ("P1-v:1", "P1", "J", "($){4294967296}", ".", "x", "0", "limit:steps", "match:0,0"),
        ]
        exhausted = "arbiter:exhausted"
        _Arbiter.answers = {
            (encode("(?:){4294967296}a"), ".", ".", "0"): exhausted,
            (encode("(?:){4294967296}a"), "y", encode("a"), "1"): exhausted,
            (encode("(?=b){4294967296}a"), ".", ".", "0"): "null",
            (encode("($){4294967296}"), ".", ".", "0"): exhausted,
        }
        original = C.Arbiter
        try:
            C.Arbiter = _Arbiter
            with tempfile.TemporaryDirectory() as tmp:
                tmp = Path(tmp)
                (tmp / "comparison").mkdir()
                for name, rows in cases.items():
                    with open_text(tmp / f"{name}.tsv.gz", "wt") as out:
                        out.write("# id\top\tpattern\tflags\tsubject\tlastIndex\tpolicy\n")
                        for cid, op, pattern, flags, subject, last in rows:
                            out.write("\t".join([cid, op, encode(pattern), flags,
                                                 "-" if subject == "-" else encode(subject), last, "default"]) + "\n")
                with open_text(tmp / "comparison" / "differences.tsv.gz", "wt") as out:
                    out.write("# id\tclass\top\trun\tpattern\tflags\tsubject\tlastIndex\tpolicy\tS\tarm\n")
                    for cid, cls, run, pattern, flags, subject, last, s_answer, arm in differences:
                        out.write("\t".join([cid, cls, "exec", run, encode(pattern), flags, encode(subject), last,
                                             "default", s_answer, arm]) + "\n")
                parser_args = ["select", "--population", str(tmp), "--broiler-js", str(tmp), "--commit", "HEAD",
                               "--unicode-pins", str(tmp), "--comparison", str(tmp / "comparison"),
                               "--out", str(tmp / "out.tsv")]
                printed = io.StringIO()
                with redirect_stdout(printed):
                    self.assertEqual(X.main(parser_args), 0)
                facts = json.loads(printed.getvalue())
                self.assertEqual(facts["first-limb"], {"P1": 1, "P4": 3})
                self.assertEqual(facts["first-two-limbs"], {"P1": 1, "P4": 2})
                self.assertEqual(facts["three-limbs"], {"P1": 1, "P4": 1})
                self.assertEqual(facts["first-two-alone"], ["P4-00004"])
                self.assertEqual(facts["not-P4"], ["P1-v:1"])
                lines = (tmp / "out.tsv").read_text(encoding="utf-8").splitlines()
                self.assertEqual(lines[1:], ["\t".join(["P4-00001", encode("(?:){4294967296}a"),
                                                        encode("(?:){4294967296}"), "."])])
                self.assertEqual(X.read_listing(tmp / "out.tsv"), {"P4-00001": encode("(?:){4294967296}a")})
        finally:
            C.Arbiter = original

    def test_a_stop_against_an_answer(self) -> None:
        def d(s_answer, arm, op="exec"):
            return {"op": op, "S": s_answer, "arm": arm}
        self.assertTrue(X.stop_against_answer(d("limit:trail", "match:0,1")))
        self.assertTrue(X.stop_against_answer(d("null", "limit:harness")))
        self.assertFalse(X.stop_against_answer(d("limit:steps", "limit:ceiling")))
        self.assertFalse(X.stop_against_answer(d("null", "match:0,0")))
        self.assertFalse(X.stop_against_answer(d("limit:steps", "refuse:syntax")))
        self.assertFalse(X.stop_against_answer(d("limit:steps", "accept", "accept")))


if __name__ == "__main__":
    unittest.main()
