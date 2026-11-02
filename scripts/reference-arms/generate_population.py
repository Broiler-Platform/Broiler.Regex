#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Bundle sjl-11-001's reference-arm population: classes P1 to P7 of docs/evidence/sjl-11-001/reference-arms.md.

    python3 -I scripts/reference-arms/generate_population.py --out DIR --test262 DIR --unicode DIR
        [--vectors DIR] [--classes P2,P4] [--reduced]

One file per class, DIR/<class>.tsv.gz, and DIR/manifest.json (every input by its SHA-256 or commit, every class's
count and SHA-256, the seed; no input by its path, so that the population, which the comparison's summary.json names
by the manifest's digest, is the same wherever its inputs are). *(Revised 2026-11-02, at task 2.13's second review:
the manifest also held the vectors directory, the test262 checkout and the Broiler.Unicode checkout by their absolute
paths, as "vectors", "test262" and "checkout".)* README.md beside this file states each class, the draw procedure of P4 and the format.
Two runs over the same inputs write the same bytes. `--vectors` names a `conformance/vectors/regexp/` directory (P1);
without it P1 is not written and the manifest says so. `--classes` writes only the classes named and `--reduced`
writes a smaller P4, P5 and P7 for the tests: either makes the population partial, which the manifest records, and
a partial population is never the judged one.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from population_format import encode, encode_segments, open_text, units_of  # noqa: E402
from splitmix64 import between, choice, splitmix64  # noqa: E402

RULE = "docs/evidence/sjl-11-001/reference-arms.md"
GENERATOR = "scripts/reference-arms/generate_population.py"
GENERATOR_VERSION = "sjl-11-001-population@1"
REPOSITORY = HERE.parents[1]

SEED_P4 = 0x534A4C31312D3031  # "SJL11-01", the rule's
P4_PATTERNS = 1 << 16
P4_FLAG_SETS = ("", "i", "m", "s", "u", "v", "iu", "iv", "y", "d")
P4_SUBJECTS = 16
P4_SUBJECT_MAX = 32
P5_FLAG_SETS = ("i", "iu", "iv")
P6_FLAG_SETS = ("u", "v")
P2_CAP = 4096  # (pattern, subject) pairs per test262 file, the generator's declared cap
CODE_POINTS = 0x110000
TEST262_PIN = "ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e"
TEST262_DIRECTORIES = ("test/built-ins/RegExp", "test/language/literals/regexp", "test/annexB",
                       "test/built-ins/String/prototype/match", "test/built-ins/String/prototype/matchAll",
                       "test/built-ins/String/prototype/replace", "test/built-ins/String/prototype/replaceAll",
                       "test/built-ins/String/prototype/search", "test/built-ins/String/prototype/split")
DEFAULT = "default"


class Writer:
    """One class's file: `id op pattern flags subject lastIndex policy`, tab-separated, in the generator's order."""

    def __init__(self, directory: Path, name: str) -> None:
        self.name = name
        self.path = directory / f"{name}.tsv.gz"
        self.handle = open_text(self.path, "wt")
        self.handle.write("# id\top\tpattern\tflags\tsubject\tlastIndex\tpolicy\n")
        self.accept = 0
        self.exec = 0

    def accept_case(self, case_id: str, pattern: str, flags: str, policy: str = DEFAULT, *, encoded: str | None = None) -> None:
        self.handle.write(f"{case_id}\taccept\t{encoded or encode(pattern)}\t{flags or '.'}\t-\t-\t{policy}\n")
        self.accept += 1

    def exec_case(self, case_id: str, pattern: str, flags: str, subject: str, last_index: int, policy: str = DEFAULT, *,
                  encoded_pattern: str | None = None, encoded_subject: str | None = None) -> None:
        self.handle.write(f"{case_id}\texec\t{encoded_pattern or encode(pattern)}\t{flags or '.'}\t"
                          f"{encoded_subject or encode(subject)}\t{last_index}\t{policy}\n")
        self.exec += 1

    def close(self) -> dict:
        self.handle.close()
        return {"file": self.path.name, "accept": self.accept, "exec": self.exec,
                "sha256": hashlib.sha256(self.path.read_bytes()).hexdigest()}


# --------------------------------------------------------------------------------------------------
# P1: the vectors.

STR_TOKEN = re.compile(r'str:"((?:[^"\\]|\\.)*)"|int:(-?\d+)|(\w+):(\S+)')


def vector_string(body: str) -> str:
    """A `str:"..."` of the corpus's schema (SCHEMA.md section 3): printable ASCII as itself, `\\"`, `\\\\`, and every
    other code unit `\\uXXXX`."""
    units: list[int] = []
    i = 0
    while i < len(body):
        ch = body[i]
        if ch == "\\":
            nxt = body[i + 1]
            if nxt == "u":
                units.append(int(body[i + 2:i + 6], 16))
                i += 6
                continue
            units.append(ord(nxt))
            i += 2
            continue
        units.append(ord(ch))
        i += 1
    from population_format import text_of
    return text_of(units)


def p1(writer: Writer, vectors: Path) -> dict:
    """Every vector of every `*.vectors` file, in file then line order: its first `in` string the pattern, its second
    the flags; a third string the subject, and an `int` after it the lastIndex (0 when none). A vector with two strings
    is an acceptance case, one with three an exec. A vector whose `in` holds no two strings first is not a pattern's
    and is counted, not written."""
    files = sorted(vectors.glob("*.vectors"))
    skipped = 0
    inputs = {}
    for path in files:
        inputs[path.name] = hashlib.sha256(path.read_bytes()).hexdigest()
        vector_id = None
        for line in path.read_text(encoding="utf-8").split("\n"):
            if line.startswith("id: "):
                vector_id = line[4:].strip()
            elif line.startswith("in: ") and vector_id is not None:
                tokens = [m for m in STR_TOKEN.finditer(line[4:])]
                values = [("str", vector_string(m.group(1))) if m.group(1) is not None
                          else ("int", int(m.group(2))) if m.group(2) is not None else ("other", m.group(0))
                          for m in tokens]
                if len(values) < 2 or values[0][0] != "str" or values[1][0] != "str":
                    skipped += 1
                    continue
                pattern, flags = values[0][1], values[1][1]
                if not re.fullmatch(r"[a-z]*", flags):
                    skipped += 1
                    continue
                case_id = "P1-" + vector_id.replace("/", ":")
                if len(values) >= 3 and values[2][0] == "str":
                    last = values[3][1] if len(values) >= 4 and values[3][0] == "int" else 0
                    writer.exec_case(case_id, pattern, flags, values[2][1], last)
                else:
                    writer.accept_case(case_id, pattern, flags)
                vector_id = None
    return {"files": inputs, "not-patterns": skipped}


# --------------------------------------------------------------------------------------------------
# P2: test262.

PUNCTUATOR_BEFORE_REGEX = set("(,=:[!&|?{};+-*%<>~^")
KEYWORDS_BEFORE_REGEX = {"return", "typeof", "instanceof", "in", "of", "new", "delete", "void", "throw", "case",
                         "do", "else", "yield", "await"}
IDENTIFIER_PART = re.compile(r"[A-Za-z0-9_$\u0080-\uffff]")


def js_string_value(body: str) -> str:
    """The String value of a JavaScript string literal's body (12.9.4): the single-character escapes, `\\0`, the
    legacy octal escapes, `\\x`, `\\u` and `\\u{}`, a line continuation, and an identity escape."""
    out: list[str] = []
    i = 0
    simple = {"n": "\n", "t": "\t", "r": "\r", "b": "\b", "f": "\f", "v": "\v"}
    while i < len(body):
        ch = body[i]
        if ch != "\\":
            out.append(ch)
            i += 1
            continue
        i += 1
        if i >= len(body):
            break
        ch = body[i]
        if ch in simple:
            out.append(simple[ch])
            i += 1
        elif ch == "x" and re.fullmatch(r"[0-9A-Fa-f]{2}", body[i + 1:i + 3]):
            out.append(chr(int(body[i + 1:i + 3], 16)))
            i += 3
        elif ch == "u" and body[i + 1:i + 2] == "{":
            end = body.index("}", i)
            out.append(chr(int(body[i + 2:end], 16)))
            i = end + 1
        elif ch == "u" and re.fullmatch(r"[0-9A-Fa-f]{4}", body[i + 1:i + 5]):
            out.append(chr(int(body[i + 1:i + 5], 16)))
            i += 5
        elif ch in "01234567":
            m = re.match(r"[0-3][0-7]{0,2}|[4-7][0-7]?", body[i:])
            assert m is not None
            out.append(chr(int(m.group(0), 8)))
            i += len(m.group(0))
        elif ch == "\r":
            i += 2 if body[i + 1:i + 2] == "\n" else 1
        elif ch in "\n\u2028\u2029":
            i += 1
        else:
            out.append(ch)
            i += 1
    return "".join(out)


def surrogate_join(text: str) -> str:
    """A string whose lone surrogates pair up where a lead stands before a trail, as the UTF-16 text would."""
    return text.encode("utf-16-le", "surrogatepass").decode("utf-16-le", "surrogatepass")


def scan_javascript(source: str) -> tuple[list[tuple[str, str]], list[str], list[tuple[str, str]]]:
    """The generator's declared grammar for test262's sources: comments, string literals (single and double quoted),
    template literals (skipped, with their substitutions' text scanned as code), and regular-expression literals,
    where a `/` begins a literal when the previous significant token is a punctuator of PUNCTUATOR_BEFORE_REGEX, a
    keyword of KEYWORDS_BEFORE_REGEX, or nothing, and a division otherwise. Returns the literals (body, flags), the
    string literals' values in order, and the RegExp calls whose first one or two arguments are string literals."""
    literals: list[tuple[str, str]] = []
    strings: list[str] = []
    calls: list[tuple[str, str]] = []
    tokens: list[tuple[str, str]] = []  # (kind, text) of significant tokens
    i = 0
    n = len(source)
    template_depth: list[int] = []
    brace_depth = 0
    while i < n:
        ch = source[i]
        if ch in " \t\r\n\u2028\u2029\ufeff\u00a0\v\f":
            i += 1
            continue
        if source.startswith("//", i):
            j = i
            while j < n and source[j] not in "\n\r\u2028\u2029":
                j += 1
            i = j
            continue
        if source.startswith("/*", i):
            j = source.find("*/", i + 2)
            i = n if j < 0 else j + 2
            continue
        if ch in "'\"":
            j = i + 1
            while j < n and source[j] != ch:
                if source[j] == "\\":
                    j += 1
                elif source[j] in "\n\r":
                    break
                j += 1
            value = surrogate_join(js_string_value(source[i + 1:j]))
            strings.append(value)
            tokens.append(("string", value))
            i = j + 1
            continue
        if ch == "`" or (ch == "}" and template_depth and template_depth[-1] == brace_depth):
            if ch == "}":
                template_depth.pop()
            j = i + 1
            while j < n and source[j] != "`":
                if source[j] == "\\":
                    j += 2
                    continue
                if source.startswith("${", j):
                    template_depth.append(brace_depth)
                    j += 2
                    break
                j += 1
            else:
                j += 1
            tokens.append(("template", ""))
            i = j
            continue
        if ch == "/":
            previous = tokens[-1] if tokens else None
            regex = previous is None or (previous[0] == "punct" and previous[1][-1] in PUNCTUATOR_BEFORE_REGEX) \
                or (previous[0] == "name" and previous[1] in KEYWORDS_BEFORE_REGEX)
            if regex:
                j = i + 1
                in_class = False
                while j < n and source[j] not in "\n\r\u2028\u2029":
                    c = source[j]
                    if c == "\\":
                        j += 2
                        continue
                    if c == "[":
                        in_class = True
                    elif c == "]":
                        in_class = False
                    elif c == "/" and not in_class:
                        break
                    j += 1
                if j < n and source[j] == "/":
                    k = j + 1
                    while k < n and IDENTIFIER_PART.match(source[k]):
                        k += 1
                    literals.append((surrogate_join(source[i + 1:j]), source[j + 1:k]))
                    tokens.append(("regex", ""))
                    i = k
                    continue
            tokens.append(("punct", "/"))
            i += 1
            continue
        if IDENTIFIER_PART.match(ch) or ch == "\\":
            j = i
            while j < n and (IDENTIFIER_PART.match(source[j]) or source[j] == "\\"):
                j += 1
            tokens.append(("name", source[i:j]))
            i = j
            continue
        if ch == "{":
            brace_depth += 1
        elif ch == "}":
            brace_depth -= 1
        if ch in ")]":
            tokens.append(("close", ch))
        else:
            tokens.append(("punct", ch))
        i += 1
    for k, (kind, text) in enumerate(tokens):
        if kind == "name" and text == "RegExp" and k + 2 < len(tokens) and tokens[k + 1] == ("punct", "(") \
                and tokens[k + 2][0] == "string":
            pattern = tokens[k + 2][1]
            flags = ""
            if k + 4 < len(tokens) and tokens[k + 3] == ("punct", ",") and tokens[k + 4][0] == "string":
                flags = tokens[k + 4][1]
            calls.append((pattern, flags))
    return literals, strings, calls


def git_tree(directory: Path) -> str:
    """The git tree hash of a directory's files as they are, computed in a scratch index: it names a test262 copy's
    content whatever its origin."""
    import os
    import tempfile
    with tempfile.TemporaryDirectory() as scratch:
        env = dict(os.environ, GIT_INDEX_FILE=str(Path(scratch) / "index"), GIT_DIR=str(Path(scratch) / "git"))
        subprocess.run(["git", "init", "-q", "--bare", env["GIT_DIR"]], check=True, env=env)
        subprocess.run(["git", "--work-tree", str(directory), "add", "-A", "-f", "."], check=True, env=env,
                       cwd=directory, stdout=subprocess.DEVNULL)
        return subprocess.run(["git", "write-tree"], check=True, env=env, capture_output=True, text=True).stdout.strip()


def p2(writer: Writer, test262: Path) -> dict:
    """Every literal and every RegExp call of every file under the rule's directories, in path order: each an
    acceptance case under its own flags, and each against every string literal of its own file in order, from
    lastIndex 0, until the file's P2_CAP pairs are written. Flags a literal or a call writes that are not ASCII
    lower-case letters are written as they are: the arms refuse them."""
    files = sorted(p for d in TEST262_DIRECTORIES for p in (test262 / d).rglob("*.js") if "_FIXTURE" not in p.name)
    capped = 0
    for path in files:
        source = path.read_text(encoding="utf-8")
        literals, strings, calls = scan_javascript(source)
        rel = path.relative_to(test262).as_posix()
        seen: set[tuple[str, str]] = set()
        patterns = []
        for pattern, flags in literals + calls:
            if (pattern, flags) not in seen and "\t" not in flags:
                seen.add((pattern, flags))
                patterns.append((pattern, flags))
        subjects = list(dict.fromkeys(strings))
        pairs = 0
        for number, (pattern, flags) in enumerate(patterns):
            case = f"P2-{rel}#{number}"
            writer.accept_case(case, pattern, flags)
            for k, subject in enumerate(subjects):
                if pairs >= P2_CAP:
                    capped += 1
                    break
                writer.exec_case(f"{case}:{k}", pattern, flags, subject, 0)
                pairs += 1
    return {"pin": TEST262_PIN, "tree": git_tree(test262 / "test"), "files": len(files),
            "cap": P2_CAP, "patterns-capped": capped, "directories": list(TEST262_DIRECTORIES)}


# --------------------------------------------------------------------------------------------------
# P3: the adversarial corpus.

EXPRESSION_PART = re.compile(r'\s*("(?:[^"\\]|\\.)*")(?:\s*\*\s*(\d+))?\s*(\+|$)')


def corpus_expression(text: str) -> str:
    """An expression of tests/Adversarial/README.md section 1, as the format's segments."""
    segments = []
    at = 0
    while at < len(text):
        m = EXPRESSION_PART.match(text, at)
        if m is None:
            raise ValueError(f"not an expression: {text[at:at + 40]}")
        value = json.loads(m.group(1))
        segments.append((units_of(value), int(m.group(2) or 1)))
        at = m.end()
    return encode_segments(segments)


def p3(writer: Writer, corpus: Path) -> dict:
    """Every row whose declared status is `match ...` or `nomatch` (the rows declared to complete), under the ample
    policy: no meter, no step ceiling, the library's frame and trail maxima, no compile budget, and the row's own
    nesting limit. `match@k` is an exec at lastIndex k under the row's flags and `g` (for k > 0, so that
    RegExpBuiltinExec reads lastIndex); `anchored@k` under `y`."""
    rows = 0
    lines = [line for line in corpus.read_text(encoding="utf-8").split("\n") if line and not line.startswith("#")]
    header = lines[0].split("\t")
    for line in lines[1:]:
        row = dict(zip(header, line.split("\t")))
        status = row["status"]
        if not (status.startswith("match ") or status == "nomatch"):
            continue
        op, _, k = row["op"].partition("@")
        flags = "" if row["flags"] == "-" else row["flags"]
        start = int(k)
        if op == "match" and start > 0:
            flags = "".join(sorted(flags + "g", key="dgimsuvy".index))
        elif op == "anchored":
            flags = "".join(sorted(flags + "y", key="dgimsuvy".index))
        nesting = re.search(r"nesting=(\d+)", row["policy"]).group(1)
        writer.exec_case(f"P3-{row['id']}", "", flags, "", start, f"ample:{nesting}",
                         encoded_pattern=corpus_expression(row["pattern"]), encoded_subject=corpus_expression(row["subject"]))
        rows += 1
    return {"corpus": str(corpus.relative_to(REPOSITORY)) if corpus.is_relative_to(REPOSITORY) else str(corpus),
            "corpus-sha256": hashlib.sha256(corpus.read_bytes()).hexdigest(), "rows": rows}


# --------------------------------------------------------------------------------------------------
# P4: the grammar draws.

class Draws:
    def __init__(self, seed: int) -> None:
        self.stream = splitmix64(seed)
        self.count = 0

    def next(self) -> int:
        self.count += 1
        return next(self.stream)

    def choice(self, n: int) -> int:
        return choice(self.next(), n)

    def pick(self, items):
        return items[self.choice(len(items))]

    def between(self, low: int, high: int) -> int:
        return between(self.next(), low, high)

    def weighted(self, table):
        """A choice by weight: one draw modulo the weights' sum, read against their running total."""
        total = sum(w for w, _ in table)
        at = self.choice(total)
        for weight, item in table:
            if at < weight:
                return item
            at -= weight
        raise AssertionError


# The pattern characters P4 draws, each a code point; the astral ones and the lone surrogates are written raw.
LITERALS = ["a", "b", "c", "A", "B", "0", "1", " ", "-", "_", ",", "k", "K", "\u212a", "s", "S", "\u017f", "\u00df",
            "\u1e9e", "i", "I", "\u0130", "\u0131", "\u03c3", "\u03c2", "\u03a3", "\u00e9", "\u00b5", "\n", "\u2028",
            "\u00a0", "\U0001d400", "\U0001f600", "\U00010400", "\U00010428", "\ud83d", "\ude00"]
SYNTAX = "^$\\.*+?()[]{}|/"
CLASS_ESCAPES = ["\\d", "\\D", "\\w", "\\W", "\\s", "\\S"]
CHARACTER_ESCAPES = ["\\n", "\\t", "\\f", "\\v", "\\r", "\\cJ", "\\ca", "\\x41", "\\x6b", "\\u0041", "\\u212A",
                     "\\u{1F600}", "\\u{10400}", "\\uD83D\\uDE00", "\\uD83D", "\\0", "\\-", "\\/"]
ANNEX_B_ESCAPES = ["\\a", "\\c", "\\c1", "\\8", "\\12", "\\07", "\\k", "\\p", "\\_", "\\q"]
PROPERTIES = ["L", "Lu", "Ll", "N", "Nd", "Script=Greek", "sc=Latn", "Script_Extensions=Latin", "General_Category=Lu",
              "ASCII", "Any", "Assigned", "ID_Start", "Alphabetic", "White_Space", "Emoji", "Lowercase", "RGI_Emoji",
              "Basic_Emoji", "Emoji_Keycap_Sequence", "L&", "lu", "Greek", "Hex_Digit"]
NAMES = ["a", "b", "n1", "\u03c0", "$x", "_", "\U0001d400", "\\u0061", "\\u{62}"]
MODIFIERS = ["i", "m", "s", "-i", "-m", "-s", "i-m", "m-is", "ii", "i-i", ""]
MAX_DEPTH = 3
QUANTIFIERS = [(6, "*"), (6, "+"), (6, "?"), (3, "{2}"), (3, "{1,}"), (3, "{0,2}"), (2, "{2,1}"), (1, "{"), (1, "{1"),
               (1, "{,2}"), (1, "{4294967296}")]


class PatternDraw:
    """One pattern from ECMA-262 22.2.1's grammar with Annex B, drawn from P4's stream (README.md, "P4"): a
    disjunction of up to three alternatives, each of up to four terms, nested to a depth of at most three; every
    production of the grammar's atoms, assertions, quantifiers and classes, with the v flag's class sets, and a share of
    Annex B's extensions and of near-misses the grammar refuses."""

    def __init__(self, draws: Draws) -> None:
        self.d = draws
        self.groups = 0

    def pattern(self) -> str:
        self.groups = 0
        return self.disjunction(0)

    def disjunction(self, depth: int) -> str:
        count = self.d.weighted([(8, 1), (2, 2), (1, 3)])
        return "|".join(self.alternative(depth) for _ in range(count))

    def alternative(self, depth: int) -> str:
        count = self.d.weighted([(1, 0), (5, 1), (4, 2), (2, 3), (1, 4)])
        return "".join(self.term(depth) for _ in range(count))

    def term(self, depth: int) -> str:
        kind = self.d.weighted([(10, "atom"), (2, "assertion"), (1, "lookaround")])
        if kind == "assertion":
            return self.d.pick(["^", "$", "\\b", "\\B"])
        if kind == "lookaround":
            if depth >= MAX_DEPTH:
                return self.d.pick(["^", "$"])
            head = self.d.pick(["(?=", "(?!", "(?<=", "(?<!"])
            body = head + self.disjunction(depth + 1) + ")"
            # Annex B quantifies a look-ahead; the main grammar refuses it, and refuses every quantified look-behind.
            return body + (self.quantifier() if self.d.choice(6) == 0 else "")
        atom = self.atom(depth)
        return atom + (self.quantifier() if self.d.choice(3) == 0 else "")

    def quantifier(self) -> str:
        q = self.d.weighted(QUANTIFIERS)
        return q + ("?" if self.d.choice(4) == 0 else "")

    def literal(self) -> str:
        ch = self.d.pick(LITERALS)
        return ch

    def atom(self, depth: int) -> str:
        kind = self.d.weighted([(14, "char"), (2, "dot"), (3, "class-escape"), (3, "char-escape"), (1, "annex-b"),
                                (1, "property"), (1, "backref"), (4, "class"), (4, "group"), (1, "syntax")])
        if kind == "char":
            return self.literal()
        if kind == "dot":
            return "."
        if kind == "class-escape":
            return self.d.pick(CLASS_ESCAPES)
        if kind == "char-escape":
            return self.d.pick(CHARACTER_ESCAPES)
        if kind == "annex-b":
            return self.d.pick(ANNEX_B_ESCAPES)
        if kind == "property":
            return self.d.pick(["\\p", "\\P"]) + "{" + self.d.pick(PROPERTIES) + "}"
        if kind == "backref":
            if self.d.choice(2) == 0:
                return "\\" + str(self.d.between(1, 3))
            return "\\k<" + self.d.pick(NAMES) + ">"
        if kind == "syntax":
            return self.d.pick(["]", "{", "}", "{1}", "\\", "(", ")", "[", "*"])
        if kind == "class":
            return self.character_class(depth)
        if depth >= MAX_DEPTH:
            return self.literal()
        head = self.d.weighted([(4, "("), (3, "(?:"), (3, "named"), (2, "modifiers")])
        if head == "named":
            self.groups += 1
            head = "(?<" + self.d.pick(NAMES) + ">"
        elif head == "modifiers":
            head = "(?" + self.d.pick(MODIFIERS) + ":"
        elif head == "(":
            self.groups += 1
        return head + self.disjunction(depth + 1) + ")"

    def class_atom(self) -> str:
        kind = self.d.weighted([(8, "char"), (3, "class-escape"), (2, "char-escape"), (1, "annex-b"), (1, "property"),
                                (1, "special")])
        if kind == "char":
            return self.literal()
        if kind == "class-escape":
            return self.d.pick(CLASS_ESCAPES)
        if kind == "char-escape":
            return self.d.pick(CHARACTER_ESCAPES + ["\\b", "\\B"])
        if kind == "annex-b":
            return self.d.pick(ANNEX_B_ESCAPES + ["\\c_", "\\c0"])
        if kind == "property":
            return self.d.pick(["\\p", "\\P"]) + "{" + self.d.pick(PROPERTIES) + "}"
        return self.d.pick(["-", "^", "[", "]", "(", ")", "{", "}", "/", "|", "&", "!!", "&&", "--", "\\q{ab|c}",
                            "\\q{}", "\\q{\U0001f600|x}"])

    def character_class(self, depth: int) -> str:
        negate = "^" if self.d.choice(4) == 0 else ""
        kind = self.d.weighted([(6, "ranges"), (2, "set-operation"), (1, "nested")])
        if kind == "ranges":
            parts = []
            for _ in range(self.d.between(0, 3)):
                atom = self.class_atom()
                if self.d.choice(3) == 0:
                    atom += "-" + self.class_atom()
                parts.append(atom)
            return "[" + negate + "".join(parts) + "]"
        if kind == "set-operation":
            op = self.d.pick(["--", "&&"])
            operands = [self.class_operand(depth) for _ in range(self.d.between(2, 3))]
            return "[" + negate + op.join(operands) + "]"
        inner = self.character_class(depth + 1) if depth < MAX_DEPTH else "[a]"
        return "[" + negate + inner + self.class_atom() + "]"

    def class_operand(self, depth: int) -> str:
        kind = self.d.weighted([(3, "char"), (2, "escape"), (2, "nested")])
        if kind == "char":
            return self.literal()
        if kind == "escape":
            return self.d.pick(CLASS_ESCAPES + ["\\p{L}", "\\p{RGI_Emoji}", "\\q{ab|c}"])
        return self.character_class(depth + 1) if depth < MAX_DEPTH else "[a]"


# Case variants of the characters subjects are drawn from: fixed, so that the population does not read the running
# Python's Unicode data. ASCII letters are swapped; the others are their case and folding partners.
CASE_PARTNERS = {
    "k": "K\u212a", "K": "k\u212a", "\u212a": "kK", "s": "S\u017f", "S": "s\u017f", "\u017f": "sS",
    "\u00df": "\u1e9e", "\u1e9e": "\u00df", "i": "I\u0130\u0131", "I": "i\u0130\u0131", "\u0130": "iI", "\u0131": "iI",
    "\u03c3": "\u03a3\u03c2", "\u03c2": "\u03c3\u03a3", "\u03a3": "\u03c3\u03c2", "\u00e9": "\u00c9", "\u00c9": "\u00e9",
    "\u00b5": "\u03bc\u039c", "\u03bc": "\u00b5\u039c", "\u039c": "\u00b5\u03bc", "\U00010400": "\U00010428",
    "\U00010428": "\U00010400", "\u03a9": "\u03c9\u2126", "\u03c9": "\u03a9\u2126", "\u2126": "\u03a9\u03c9",
}
SUBJECT_ASTRAL = ["\U0001d400", "\U0001f600", "\U00010400", "\U00010428", "\U0001d306"]
SUBJECT_LONE = ["\ud83d", "\ude00", "\ud834", "\udf06"]


def case_variants(ch: str) -> str:
    if ch in CASE_PARTNERS:
        return CASE_PARTNERS[ch]
    if "a" <= ch <= "z" or "A" <= ch <= "Z":
        return ch.swapcase()
    return ""


def subject_alphabet(pattern: str) -> list[str]:
    """The pattern's own characters (its code points in order of first appearance, a lead and a trail surrogate in
    order being one), each one's case variants after it, then SUBJECT_ASTRAL and SUBJECT_LONE, each once."""
    out: dict[str, None] = {}
    for ch in pattern:
        out.setdefault(ch, None)
        for variant in case_variants(ch):
            out.setdefault(variant, None)
    for ch in SUBJECT_ASTRAL + SUBJECT_LONE:
        out.setdefault(ch, None)
    return list(out)


def p4(writer: Writer, patterns: int) -> dict:
    """README.md, "P4": for each of `patterns` patterns in turn, the pattern's draws, then its 16 subjects' draws
    (for each: its length, 0 to 32; each character a choice over the pattern's subject alphabet; its lastIndex, 0 to
    the length in code units); then for each flag set in order an acceptance case and the 16 exec cases."""
    d = Draws(SEED_P4)
    drawer = PatternDraw(d)
    lengths = [0] * (P4_SUBJECT_MAX + 1)
    for number in range(patterns):
        pattern = surrogate_join(drawer.pattern())
        alphabet = subject_alphabet(pattern)
        subjects = []
        for _ in range(P4_SUBJECTS):
            length = d.between(0, P4_SUBJECT_MAX)
            lengths[length] += 1
            text = surrogate_join("".join(d.pick(alphabet) for _ in range(length)))
            last = d.between(0, len(units_of(text)))
            subjects.append((text, last))
        encoded = encode(pattern)
        for flags in P4_FLAG_SETS:
            tag = flags or "none"
            writer.accept_case(f"P4-{number:05d}-{tag}", pattern, flags, encoded=encoded)
            for k, (subject, last) in enumerate(subjects):
                writer.exec_case(f"P4-{number:05d}-{tag}-{k:02d}", pattern, flags, subject, last, encoded_pattern=encoded)
    return {"seed": f"{SEED_P4:#x}", "patterns": patterns, "flag-sets": list(P4_FLAG_SETS), "subjects": P4_SUBJECTS,
            "draws": d.count, "subject-lengths": lengths}


# --------------------------------------------------------------------------------------------------
# P5, P6, P7: from Broiler.Unicode's reference and population.

def load_unicode(checkout: Path):
    """Broiler.Unicode's unicoderef (`ref:unicoderef@1`) over its archived UCD 17.0.0 pins, and its population
    generator's U2, from the checkout named: their code is that repository's, run here and not copied."""
    scripts = checkout / "scripts" / "reference-arms"
    sys.path.insert(0, str(scripts))
    import unicoderef  # noqa: E402
    import generate_population as unicode_population  # noqa: E402
    sys.path.remove(str(scripts))
    pins = unicoderef.Pins(checkout / "data" / "unicode" / "pins" / "unicode.pin")
    ucd = unicoderef.UCD(pins)
    commit = subprocess.run(["git", "-C", str(checkout), "rev-parse", "HEAD"], capture_output=True, text=True).stdout.strip()
    blobs = {name: subprocess.run(["git", "-C", str(checkout), "hash-object", str(scripts / name)], capture_output=True,
                                  text=True).stdout.strip() for name in ("unicoderef.py", "generate_population.py")}
    return ucd, unicode_population, {"commit": commit, "blobs": blobs,
                                     "pin": {"sha256": pins.sha256, "version": pins.version}}


def raw(cp: int) -> str:
    return chr(cp)


def p5(writer: Writer, ucd, step: int) -> dict:
    """Every code point c (every `step`-th with --reduced): the pattern c and the pattern [c], each under i, iu and
    iv, an acceptance case and an exec from lastIndex 0 against each member of c's simple-folding orbit (CaseFolding.txt
    status C and S; c included) and against c's simple upper case (UnicodeData.txt field 12), in code point order
    without repeats. A surrogate is one code unit; an astral code point two."""
    for cp in range(0, CODE_POINTS, step):
        subjects = sorted(set(ucd.orbit(cp)) | {ucd.simple_upper.get(cp, cp)})
        for form, pattern in (("char", raw(cp)), ("class", "[" + raw(cp) + "]")):
            encoded = encode(pattern)
            for flags in P5_FLAG_SETS:
                case = f"P5-{cp:06X}-{form}-{flags}"
                writer.accept_case(case, pattern, flags, encoded=encoded)
                for subject in subjects:
                    writer.exec_case(f"{case}-{subject:06X}", pattern, flags, raw(subject), 0, encoded_pattern=encoded)
    return {"code-points": len(range(0, CODE_POINTS, step)), "step": step, "flag-sets": list(P5_FLAG_SETS)}


def p6(writer: Writer, ucd, unicode_population) -> dict:
    """Every line of Broiler.Unicode's U2 (names.tsv, as its generator writes it from its seed): `lone` as \\p{name}
    and \\P{name}, `pair` as \\p{name=value} and \\P{name=value}, each under u and v, an acceptance case."""
    names, subclasses, _ = unicode_population.names_population(ucd, unicode_population.U2_DRAWS)
    for number, line in enumerate(names):
        kind, prop, value = line.split("\t")
        body = prop if kind == "lone" else f"{prop}={value}"
        for escape in ("p", "P"):
            pattern = "\\" + escape + "{" + body + "}"
            for flags in P6_FLAG_SETS:
                writer.accept_case(f"P6-{number:05d}-{escape}-{flags}", pattern, flags)
    return {"names": len(names), "u2-draws": unicode_population.U2_DRAWS, "u2-seed": f"{unicode_population.SEED_U2:#x}",
            "subclasses": [s["name"] for s in subclasses]}


def escaped(cp: int) -> str:
    return f"\\u{cp:04X}" if cp < 0x10000 else f"\\u{{{cp:X}}}"


def p7(writer: Writer, step: int) -> dict:
    """Every code point c (every `step`-th with --reduced) in a group name, as its first character `(?<c>.)` and as a
    later one `(?<ac>.)`, written raw and escaped (`\\uXXXX` for a code point of the BMP, a surrogate among them, and
    `\\u{X}` above it), each without flags and under u: an acceptance case."""
    for cp in range(0, CODE_POINTS, step):
        for spelling, text in (("raw", raw(cp)), ("esc", escaped(cp))):
            for position, name in (("first", text), ("later", "a" + text)):
                pattern = "(?<" + name + ">.)"
                encoded = encode(pattern)
                for flags in ("", "u"):
                    writer.accept_case(f"P7-{cp:06X}-{position}-{spelling}-{flags or 'none'}", pattern, flags, encoded=encoded)
    return {"code-points": len(range(0, CODE_POINTS, step)), "step": step}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--test262", type=Path)
    parser.add_argument("--unicode", type=Path, help="a Broiler.Unicode checkout (P5, P6)")
    parser.add_argument("--vectors", type=Path, help="a conformance/vectors/regexp/ directory (P1)")
    parser.add_argument("--corpus", type=Path, default=REPOSITORY / "tests" / "Adversarial" / "corpus.tsv")
    parser.add_argument("--classes", default="P1,P2,P3,P4,P5,P6,P7")
    parser.add_argument("--reduced", action="store_true", help="a smaller P4, P5 and P7, for the tests; never judged")
    args = parser.parse_args(argv)
    classes = [c for c in args.classes.split(",") if c]
    args.out.mkdir(parents=True, exist_ok=True)
    manifest: dict = {"rule": RULE, "generator": GENERATOR, "generator_version": GENERATOR_VERSION,
                      "size": "reduced" if args.reduced else "full", "classes": {}, "inputs": {}}
    commit = subprocess.run(["git", "-C", str(REPOSITORY), "rev-parse", "HEAD"], capture_output=True, text=True).stdout.strip()
    manifest["commit"] = commit
    ucd = unicode_population = None
    if ("P5" in classes or "P6" in classes):
        if args.unicode is None:
            parser.error("P5 and P6 read Broiler.Unicode: --unicode")
        ucd, unicode_population, manifest["inputs"]["broiler-unicode"] = load_unicode(args.unicode)
    for name in classes:
        if name == "P1" and args.vectors is None:
            manifest["classes"]["P1"] = {"written": False, "why": "no --vectors directory was given"}
            continue
        if name == "P2" and args.test262 is None:
            parser.error("P2 reads test262: --test262")
        writer = Writer(args.out, name)
        if name == "P1":
            facts = p1(writer, args.vectors)
        elif name == "P2":
            facts = p2(writer, args.test262)
        elif name == "P3":
            facts = p3(writer, args.corpus)
        elif name == "P4":
            facts = p4(writer, 1 << 9 if args.reduced else P4_PATTERNS)
        elif name == "P5":
            facts = p5(writer, ucd, 4099 if args.reduced else 1)
        elif name == "P6":
            facts = p6(writer, ucd, unicode_population)
        elif name == "P7":
            facts = p7(writer, 4099 if args.reduced else 1)
        else:
            parser.error(f"no class {name}")
        manifest["classes"][name] = {**writer.close(), **facts}
    manifest["partial"] = args.reduced or sorted(classes) != ["P1", "P2", "P3", "P4", "P5", "P6", "P7"] or \
        not manifest["classes"].get("P1", {}).get("file")
    (args.out / "manifest.json").write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
