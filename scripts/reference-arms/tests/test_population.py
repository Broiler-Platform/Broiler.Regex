# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""The population's format, its readers of the inputs, P4's draw procedure, and the generator's determinism."""

from __future__ import annotations

import json
import os
import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parents[1]
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import generate_population as G  # noqa: E402
from population_format import decode, decode_units, encode, encode_segments, read_lines, units_of  # noqa: E402


class FormatTests(unittest.TestCase):
    def test_strings_round_trip_with_lone_surrogates_and_runs(self) -> None:
        for text in ["", "a", "\ud800", "\udc00a", "\U0001f600", "a" * 30 + "b", "😀\ud83d"]:
            self.assertEqual(decode(encode(text)), text)
        self.assertEqual(encode("b" + "a" * 30), "0062+0061*30")
        self.assertEqual(encode(""), ".")

    def test_segments_and_counts(self) -> None:
        self.assertEqual(encode_segments([(units_of("ab"), 3), (units_of("c"), 1)]), "00610062*3+0063")
        self.assertEqual(decode_units("00610062*2+0063"), [0x61, 0x62, 0x61, 0x62, 0x63])

    def test_the_corpus_expression(self) -> None:
        self.assertEqual(decode(G.corpus_expression('"b" + "a" * 3')), "baaa")
        self.assertEqual(decode(G.corpus_expression('"\\ud800" * 2')), "\ud800\ud800")


class InputReaderTests(unittest.TestCase):
    def test_the_vector_string(self) -> None:
        self.assertEqual(G.vector_string('a\\"\\\\\\uD834\\uDF06'), 'a"\\\U0001d306')

    def test_javascript_string_values(self) -> None:
        self.assertEqual(G.js_string_value(r"a\nb\x41B\u{1F600}\101\0"), "a\nbAB\U0001f600A\x00")

    def test_the_scanner_tells_a_literal_from_a_division(self) -> None:
        literals, strings, calls = G.scan_javascript(
            "var x = a / b / c; var r = /a[/]b/gi; f(/x/); // /not/\n"
            "/* /no/ */ new RegExp('p+', \"y\"); RegExp(\"q\"); s = 'str';")
        self.assertEqual(literals, [("a[/]b", "gi"), ("x", "")])
        self.assertEqual(calls, [("p+", "y"), ("q", "")])
        self.assertEqual(strings, ["p+", "y", "q", "str"])


class P4Tests(unittest.TestCase):
    def test_the_draw_procedure_is_the_stream_s(self) -> None:
        # The first pattern is drawn from the first outputs of SplitMix64 at the rule's seed, read by weight.
        a, b = G.PatternDraw(G.Draws(G.SEED_P4)), G.PatternDraw(G.Draws(G.SEED_P4))
        self.assertEqual([a.pattern() for _ in range(50)], [b.pattern() for _ in range(50)])

    def test_the_subject_alphabet(self) -> None:
        alphabet = G.subject_alphabet("k\U0001d400")
        self.assertEqual(alphabet[:4], ["k", "K", "K", "\U0001d400"])
        self.assertIn("\ud83d", alphabet)
        self.assertEqual(len(alphabet), len(set(alphabet)))


class GeneratorTests(unittest.TestCase):
    def test_two_runs_write_the_same_bytes_and_the_manifest_counts_the_lines(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            outs = []
            for k in range(2):
                out = Path(scratch) / str(k)
                self.assertEqual(G.main(["--out", str(out), "--classes", "P3,P4", "--reduced"]), 0)
                outs.append(out)
            for name in ("P3.tsv.gz", "P4.tsv.gz"):
                self.assertEqual((outs[0] / name).read_bytes(), (outs[1] / name).read_bytes())
            manifest = json.loads((outs[0] / "manifest.json").read_text(encoding="utf-8"))
            self.assertTrue(manifest["partial"])
            p4 = manifest["classes"]["P4"]
            lines = list(read_lines(outs[0] / "P4.tsv.gz"))
            self.assertEqual(len(lines), p4["accept"] + p4["exec"])
            self.assertEqual(p4["accept"], (1 << 9) * len(G.P4_FLAG_SETS))
            self.assertEqual(p4["exec"], (1 << 9) * len(G.P4_FLAG_SETS) * G.P4_SUBJECTS)
            lengths = [len(decode_units(line.split("\t")[4])) for line in lines if line.split("\t")[1] == "exec"]
            self.assertLessEqual(max(lengths), 2 * G.P4_SUBJECT_MAX)  # an astral draw is two code units
            p3 = manifest["classes"]["P3"]
            self.assertGreater(p3["rows"], 0)


@unittest.skipUnless(os.environ.get("BROILER_UNICODE"), "BROILER_UNICODE names a Broiler.Unicode checkout")
class UnicodeClassTests(unittest.TestCase):
    def test_p5_s_subjects_are_the_orbit_and_the_upper_case(self) -> None:
        ucd, _, _ = G.load_unicode(Path(os.environ["BROILER_UNICODE"]))
        with tempfile.TemporaryDirectory() as scratch:
            writer = G.Writer(Path(scratch), "P5")
            G.p5(writer, ucd, 0x6B)  # 0, 0x6B (k), 0xD6, ...
            writer.close()
            ids = [line.split("\t")[0] for line in read_lines(Path(scratch) / "P5.tsv.gz")]
        self.assertIn("P5-00006B-char-i-00004B", ids)
        self.assertIn("P5-00006B-char-i-00212A", ids)


if __name__ == "__main__":
    unittest.main()
