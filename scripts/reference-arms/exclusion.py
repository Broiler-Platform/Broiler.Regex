#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Bundle sjl-11-001's exclusion from population P4, by the owner's ruling of 2026-11-16 (written that day, after task
2.13's runs, their classification and the reduction of 2026-11-15 were seen).

    python3 -I -B scripts/reference-arms/exclusion.py select --population DIR --broiler-js CHECKOUT --commit SHA
        --unicode-pins DIR --comparison DIR --out FILE [--cutoff N] [--workers N] [--cache FILE]

The owner excluded from P4 the 19 patterns whose arbiter cost is in the pattern and whose inputs keep class (c) from
emptying (docs/evidence/sjl-11-001/reference-arms-revised-2026-11-16b.md). The revision's exclusion is their
enumeration by id, in p4-excluded.tsv beside this script, which `classify.py --exclude` reads; the three limbs below,
each held under one of the pattern's flag sets, are how the 19 were found, and this script writes the listing again
from task 2.13's run:

1. **The pattern's limb** (`empty_body_minimum`): read under the flags (u or v: the grammar of 22.2.1; otherwise
   Annex B's), the pattern quantifies with a braced minimum of at least MINIMUM (2^32 = 4,294,967,296) an atom that
   can match the empty string: an assertion (`^`, `$`, `\\b`, `\\B`), a look-around, a back-reference, an empty group,
   a group one of whose alternatives can (every term an atom that can, or quantified with minimum 0), or under v a
   class that holds the empty string (`[\\q{}]`). The arbiter (regexpref.py's RepeatMatcher) runs such a body that many
   times before its empty check applies, whatever the subject.
2. **The arbiter's limb**: on one of the pattern's exec inputs in the population, the arbiter at its cut-off reaches
   no prefix of the subject, not even the shortest: reduce.py's outcome `none`, the subject cut to the lastIndex under
   g or y and to the empty string otherwise. It is asked of every exec input of a pattern with the first limb.
3. **The run's limb**: on one such input (both limbs held) the comparison holds a difference where one side stops,
   at a limit or by a crash (an answer `limit:...` or `crash:...`), and the other answers (null or a match): the stop
   against an answer the arbiter cannot check, which class (c) holds. This limb reads the run, as the ruling did, and
   is not a property of the pattern: with it the limbs select nothing but the ids. Limbs 1 and 2 alone are the
   narrowest property found of the pattern and the arbiter, and they hold for 11 patterns more, on whose unreachable
   inputs every side stops or all answer alike, so that none of their differences is class (c); the ruling does not
   name them, and the revision puts that property to the owner.

**select** reads every class of the population, applies the first limb to each distinct pattern and flags, asks the
arbiter (classify.py's Arbiter, at the commit and cut-off given) the second for every exec input of those, reads the
third from the comparison's differences.tsv.gz, and writes OUT: a `#` header, then one line per P4 pattern with the
three limbs: its id (`P4-` and its five digits, every flag set and input of it), its pattern and the first limb's
quantifier in the population's notation, and the flag sets under which the three hold. It prints, by class, how many
patterns have the first limb, the first two and all three, and the ids of those with the first two alone. A pattern
of a class other than P4 is printed and not written: the ruling excludes from P4 alone.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from collections import Counter
from dataclasses import dataclass
from multiprocessing import Pool
from pathlib import Path

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from population_format import decode_units, encode_units, open_text, text_of, units_of  # noqa: E402

MINIMUM = 2 ** 32
RULE = "reference-arms-revised-2026-11-16b.md"

BRACED = re.compile(r"\{(\d+)(?:(,)(\d*))?\}")


@dataclass(frozen=True)
class Quantified:
    """A quantified atom: where it starts and ends in the pattern, its minimum, and whether its body can match the
    empty string."""
    start: int
    end: int
    minimum: int
    braced: bool
    body_empty: bool
    text: str


class _Parser:
    def __init__(self, text: str, flags: str) -> None:
        self.t = text
        self.u = "u" in flags or "v" in flags
        self.v = "v" in flags
        self.found: list[Quantified] = []
        self.groups = self._count_groups()
        self.named = bool(re.search(r"\(\?<[^=!]", text))

    def _count_groups(self) -> int:
        t, i, depth, n = self.t, 0, 0, 0
        while i < len(t):
            ch = t[i]
            if ch == "\\":
                i += 2
                continue
            if depth:
                if ch == "]":
                    depth -= 1
                elif ch == "[" and self.v:
                    depth += 1
            elif ch == "[":
                depth = 1
            elif ch == "(" and (t[i + 1:i + 2] != "?" or (t[i + 2:i + 3] == "<" and t[i + 3:i + 4] not in ("=", "!"))):
                n += 1
            i += 1
        return n

    def run(self) -> None:
        """One pass over the pattern, a frame per open group (iterative: P1's nesting vectors are deeper than Python's
        recursion), each frame with whether its alternative so far and any earlier alternative can match empty."""
        t = self.t
        top = {"start": -1, "look": False, "alt": True, "any": False}
        stack = [top]
        i = 0
        while i < len(t):
            ch = t[i]
            if ch == "(":
                look = re.match(r"\(\?(?:=|!|<=|<!)", t[i:])
                m = look or re.match(r"\(\?(?::|<[^>]*>|[a-z]*(?:-[a-z]*)?:)", t[i:])
                stack.append({"start": i, "look": bool(look), "alt": True, "any": False})
                i += m.end() if m else 1
                continue
            if ch == "|":
                stack[-1]["any"] |= stack[-1]["alt"]
                stack[-1]["alt"] = True
                i += 1
                continue
            if ch == ")":
                if len(stack) == 1:
                    i += 1
                    continue
                frame = stack.pop()
                empty = frame["look"] or frame["any"] or frame["alt"]
                i = self.close_term(frame["start"], i + 1, empty, stack[-1])
                continue
            start = i
            i, empty = self.atom(i)
            i = self.close_term(start, i, empty, stack[-1])
        while len(stack) > 1:
            frame = stack.pop()
            self.close_term(frame["start"], len(t), frame["look"] or frame["any"] or frame["alt"], stack[-1])

    def close_term(self, start: int, i: int, empty: bool, parent: dict) -> int:
        q = self.quantifier(i)
        if q is not None:
            end, minimum, braced = q
            if end < len(self.t) and self.t[end] == "?":
                end += 1
            self.found.append(Quantified(start, end, minimum, braced, empty, self.t[start:end]))
            empty = empty or minimum == 0
            i = end
        parent["alt"] &= empty
        return i

    def quantifier(self, i: int):
        if i >= len(self.t):
            return None
        ch = self.t[i]
        if ch in "*?":
            return i + 1, 0, False
        if ch == "+":
            return i + 1, 1, False
        if ch == "{":
            m = BRACED.match(self.t, i)
            if m:
                return m.end(), int(m.group(1)), True
        return None

    def atom(self, i: int) -> tuple[int, bool]:
        t = self.t
        ch = t[i]
        if ch in "^$":
            return i + 1, True
        if ch == ".":
            return i + 1, False
        if ch == "\\":
            return self.escape(i)
        if ch == "[":
            return self.char_class(i)
        return i + 1, False

    def escape(self, i: int) -> tuple[int, bool]:
        t = self.t
        c = t[i + 1:i + 2]
        if c in ("b", "B"):
            return i + 2, True
        if c.isdigit() and c != "0":
            m = re.match(r"\d+", t[i + 1:])
            if self.u or int(m.group(0)) <= self.groups:
                return i + 1 + m.end(), True
            return i + 2, False
        if c == "k" and (self.u or self.named):
            close = t.find(">", i)
            return (close + 1 if close > 0 else i + 2), True
        if self.u and c in ("u", "p", "P") and t[i + 2:i + 3] == "{":
            close = t.find("}", i)
            return (close + 1 if close > 0 else i + 2), False
        return i + 2 if c else i + 1, False

    def char_class(self, i: int) -> tuple[int, bool]:
        """A class, to its closing bracket (nested under v). Under v a class can hold the empty string (`\\q{}`)."""
        t = self.t
        j, depth, empty = i + 1, 1, False
        negated = t[j:j + 1] == "^"
        while j < len(t) and depth:
            ch = t[j]
            if ch == "\\":
                if self.v and t[j + 1:j + 3] == "q{":
                    close = t.find("}", j)
                    body = t[j + 3:close] if close > 0 else ""
                    if not negated and "" in body.split("|"):
                        empty = True
                    j = close + 1 if close > 0 else j + 2
                    continue
                j += 2
                continue
            if ch == "]":
                depth -= 1
            elif ch == "[" and self.v:
                depth += 1
            j += 1
        return j, empty


def quantified(text: str, flags: str) -> list[Quantified]:
    """Every quantified atom of the pattern, read under the flags (u or v: the Unicode grammar; otherwise Annex B's)."""
    p = _Parser(text, "" if flags == "." else flags)
    p.run()
    return p.found


def empty_body_minimum(text: str, flags: str) -> list[Quantified]:
    """The first limb: every braced quantifier with a minimum of at least MINIMUM over an atom that can match empty."""
    return [q for q in quantified(text, flags) if q.braced and q.minimum >= MINIMUM and q.body_empty]


def pattern_id(case_id: str) -> str:
    """A case's pattern: for P4 `P4-` and the pattern's five digits (`P4-01347-d-02` is `P4-01347`, under every flag
    set and subject, a reduced case `~p8` among them); for another class the case itself."""
    if case_id.startswith("P4-"):
        return "-".join(case_id.split("~", 1)[0].split("-")[:2])
    return case_id


def shortest_prefix(subject: str, flags: str, last_index: str) -> str:
    """reduce.py's shortest prefix: the subject cut to the lastIndex under g or y (RegExpBuiltinExec reads it under
    those alone), to the empty string otherwise."""
    flags = "" if flags == "." else flags
    k = int(last_index) if ("g" in flags or "y" in flags) else 0
    return encode_units(decode_units(subject)[:k])


_ARBITER = None


def _start(broiler_js: str, commit: str, pins: str, cutoff: int) -> None:
    global _ARBITER
    from classify import Arbiter  # noqa: PLC0415 - each worker takes its own copy of the arbiter
    _ARBITER = Arbiter(Path(broiler_js), commit, Path(pins), cutoff)


def _ask(key):
    from classify import render  # noqa: PLC0415
    pattern, flags, subject, last_index = key
    return key, render(_ARBITER.answer(pattern, flags, subject, last_index, "exec"))


def _cases(population: Path):
    for path in sorted(population.glob("P*.tsv.gz")):
        name = path.name.split(".", 1)[0]
        with open_text(path) as handle:
            for line in handle:
                if line.startswith("#"):
                    continue
                f = line.rstrip("\n").split("\t")
                yield name, {"id": f[0], "op": f[1], "pattern": f[2], "flags": f[3], "subject": f[4],
                             "lastIndex": f[5]}


def select(args) -> int:
    from classify import Arbiter, parse_rendered  # noqa: PLC0415
    first: dict[tuple[str, str], list[Quantified]] = {}
    candidates: dict[str, dict] = {}
    static = Counter()
    execs = []
    for name, case in _cases(args.population):
        key = (case["pattern"], case["flags"])
        if key not in first:
            first[key] = empty_body_minimum(text_of(decode_units(case["pattern"])), case["flags"])
        if not first[key]:
            continue
        pid = pattern_id(case["id"])
        if pid not in candidates:
            candidates[pid] = {"class": name, "pattern": case["pattern"], "two": set(), "flags": set(),
                               "quantifier": first[key][0]}
            static[name] += 1
        if case["op"] == "exec":
            execs.append((pid, case["flags"],
                          (case["pattern"], case["flags"], shortest_prefix(case["subject"], case["flags"],
                                                                         case["lastIndex"]), case["lastIndex"]),
                          case["id"]))
    arbiter = Arbiter(args.broiler_js, args.commit, args.unicode_pins, args.cutoff)
    if args.cache and args.cache.exists():
        arbiter.load(args.cache)
    missing = sorted({key for _, _, key, _ in execs if key not in arbiter.cache})
    if missing:
        with Pool(args.workers, initializer=_start,
                  initargs=(str(args.broiler_js), args.commit, str(args.unicode_pins), args.cutoff)) as pool:
            for key, answer in pool.imap_unordered(_ask, missing, chunksize=1):
                arbiter.cache[key] = parse_rendered(answer)
    if args.cache:
        arbiter.save(args.cache)
    unreachable: dict[str, str] = {}
    for pid, flags, key, case_id in execs:
        answer = arbiter.cache[key]
        if isinstance(answer, str) and answer.startswith("arbiter:"):
            candidates[pid]["two"].add(flags)
            unreachable[case_id] = pid
    for d in _differences(args.comparison / "differences.tsv.gz"):
        pid = unreachable.get(d["id"])
        if pid is not None and stop_against_answer(d):
            candidates[pid]["flags"].add(d["flags"])
    two = Counter(c["class"] for c in candidates.values() if c["two"])
    both = Counter(c["class"] for c in candidates.values() if c["flags"])
    with open(args.out, "w", encoding="utf-8", newline="\n") as out:
        out.write("# id\tpattern\tquantifier\tflags\n")
        for pid in sorted(candidates):
            c = candidates[pid]
            if c["flags"] and c["class"] == "P4":
                q = c["quantifier"]
                out.write("\t".join([pid, c["pattern"], encode_units(units_of(q.text)),
                                     ",".join(sorted(c["flags"]))]) + "\n")
    others = sorted(pid for pid, c in candidates.items() if c["two"] and c["class"] != "P4")
    print(json.dumps({"first-limb": dict(sorted(static.items())), "first-two-limbs": dict(sorted(two.items())),
                      "three-limbs": dict(sorted(both.items())), "exec-inputs-asked": len(execs),
                      "first-two-alone": sorted(pid for pid, c in candidates.items() if c["two"] and not c["flags"]),
                      "not-P4": others}, sort_keys=True))
    return 0


def _differences(path: Path):
    header = None
    with open_text(path) as handle:
        for line in handle:
            if line.startswith("#"):
                header = line[2:].rstrip("\n").split("\t")
                continue
            yield dict(zip(header, line.rstrip("\n").split("\t")))


def stop_against_answer(d) -> bool:
    """The third limb's difference: one side stops, at a limit (its own bound, or the harness's) or by a crash, and the
    other answers the exec, null or a match."""
    def stops(answer: str) -> bool:
        return answer.startswith(("limit:", "crash:"))

    def answers(answer: str) -> bool:
        return answer == "null" or answer.startswith("match:")
    return d["op"] == "exec" and ((stops(d["S"]) and answers(d["arm"])) or (answers(d["S"]) and stops(d["arm"])))


def read_listing(path: Path) -> dict[str, str]:
    """p4-excluded.tsv (or select's OUT): each excluded pattern's id and its pattern in the population's notation."""
    listed: dict[str, str] = {}
    with open(path, encoding="utf-8") as handle:
        for line in handle:
            if line.startswith("#") or not line.strip():
                continue
            fields = line.rstrip("\n").split("\t")
            if len(fields) != 4 or not re.fullmatch(r"P4-\d{5}", fields[0]) or fields[0] in listed:
                raise ValueError(f"{path.name}: not a listing line: {line.rstrip()!r}")
            listed[fields[0]] = fields[1]
    return listed


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest="mode", required=True)
    p = sub.add_parser("select")
    p.add_argument("--population", required=True, type=Path)
    p.add_argument("--broiler-js", required=True, type=Path)
    p.add_argument("--commit", required=True)
    p.add_argument("--unicode-pins", required=True, type=Path)
    p.add_argument("--comparison", required=True, type=Path, help="compare.py's directory for the population's run")
    p.add_argument("--out", required=True, type=Path)
    p.add_argument("--cutoff", type=int, default=1_000_000)
    p.add_argument("--workers", type=int, default=4)
    p.add_argument("--cache", type=Path, help="a file of the arbiter's answers, classify.py's format")
    args = parser.parse_args(argv)
    return select(args)


if __name__ == "__main__":
    sys.exit(main())
