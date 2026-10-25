#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Write one JavaScript engine's adversarial programs from tests/Adversarial/corpus.tsv.

    python3 eng/adversarial-programs.py --engine broiler-vm --out <empty directory>
    python3 eng/adversarial-programs.py --engine broiler-js --out <empty directory>

Written 2026-10-20, at work package SJL-11.1 of the shared JavaScript libraries programme, with bundle
sjl-11-001's rules (docs/evidence/sjl-11-001/). The corpus is the library's population (the rule's part
C); this script turns the rows an engine's column admits into one program per case and a manifest, the
inputs of Broiler.VM's and Broiler.JS's own sjl-11-001 rules (their part D and part C). Those engines
commit the output in their own bundles; nothing it writes is committed here.

The output is deterministic: it depends only on the corpus's bytes, this script's bytes and the commit
both are read at, so two runs at one commit write byte-identical files. The manifest's header names the
corpus's SHA-256 and this repository's commit. The script refuses to run when the corpus or the script
differs from the commit's copy, because the commit would then not name what was read.

Each program builds its pattern and subject from the corpus's expressions with String.prototype.repeat,
never as expanded text, so a subject of millions of characters is a short program. Every character
outside printable ASCII is written as a \\uXXXX escape of its UTF-16 code units.

- broiler-vm: the program's completion value is its answer, as Broiler.VM's command-line root reports
  it; an error is left uncaught, and an exhausted allowance ends the process as that root reports it.
  The manifest gives each case its fuel allowance.
- broiler-js: the program prints one line, "adversarial-outcome <id> <outcome>", through the shell's
  print in script-host mode; an error is caught and reported by its constructor's name. The manifest's
  header gives the wall-clock limit the corpus declares.

The format of the corpus and of an answer is tests/Adversarial/README.md's. Standard library only.
"""

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CORPUS_REL = "tests/Adversarial/corpus.tsv"
SCRIPT_REL = "eng/adversarial-programs.py"
GENERATOR_FORMAT = "1"

COLUMNS = [
    "id", "class", "op", "pattern", "flags", "subject", "policy", "status",
    "vm_fuel", "vm_subject", "vm_outcome", "js_subject", "js_outcome",
]
CLASSES = "KALGWNFMVO"
ENGINES = ("broiler-vm", "broiler-js")

ID_RE = re.compile(r"^[KALGWNFMVO][0-9]{2}$")
OP_RE = re.compile(r"^(compile|validate|match@(0|[1-9][0-9]*)|anchored@(0|[1-9][0-9]*))$")
FLAGS_RE = re.compile(r"^[dimsuv]+$")
COUNT = r"(0|[1-9][0-9]*)"
POLICY_RE = re.compile(
    r"^meter=(none|" + COUNT + r") steps=(none|" + COUNT + r") frames=" + COUNT + r" trail=" + COUNT
    + r" nesting=" + COUNT + r" compile=(none|" + COUNT + r")$")
REPEAT = r"(\^[1-9][0-9]*)?"
STATUS_CAPTURE = r"(-|" + COUNT + r"," + COUNT + r")" + REPEAT
STATUS_RE = re.compile(
    r"^(ok|nomatch|meter-exception|syntax|limit:(steps|frames|trail|nesting|compile)|match "
    + COUNT + r"," + COUNT + r"( " + STATUS_CAPTURE + r")*)$")
ANSWER_GROUP = r"(-|" + COUNT + r")" + REPEAT
ANSWER_RE = r"(null|" + COUNT + r"," + COUNT + r"( " + ANSWER_GROUP + r")*)"
VM_OUTCOME_RE = re.compile(r"^(exhausted|uncaught (SyntaxError|RangeError)|completed " + ANSWER_RE + r")$")
JS_OUTCOME_RE = re.compile(r"^(threw (SyntaxError|RangeError)|completed " + ANSWER_RE + r")$")
TERM_COUNT_RE = re.compile(r" \* " + COUNT)


class CorpusError(Exception):
    pass


def fail(message):
    print("adversarial-programs: " + message, file=sys.stderr)
    sys.exit(2)


def git(*args):
    try:
        result = subprocess.run(["git", "-C", ROOT] + list(args), capture_output=True, check=False)
    except OSError as error:
        fail("cannot run git: " + str(error))
    if result.returncode != 0:
        fail("git " + " ".join(args) + " failed: " + result.stderr.decode("utf-8", "replace").strip())
    return result.stdout


def committed_state(paths):
    """The commit both files are read at; refuses a file that differs from the commit's copy."""
    commit = git("rev-parse", "--verify", "HEAD^{commit}").decode("ascii").strip()
    if not re.match(r"^[0-9a-f]{40}$", commit):
        fail("HEAD is not a forty-digit commit: " + commit)
    for rel in paths:
        with open(os.path.join(ROOT, rel), "rb") as handle:
            on_disk = handle.read()
        if git("show", commit + ":" + rel) != on_disk:
            fail(rel + " differs from its copy at " + commit + "; commit it first, so that the "
                 "manifest's commit names what was read")
    return commit


def parse_expression(text, where):
    """A pattern or subject expression: one or more JSON string literals, each optionally followed
    by ' * <count>', joined by ' + '. Returns [(text, count)]."""
    decoder = json.JSONDecoder()
    terms = []
    at = 0
    while True:
        if at >= len(text) or text[at] != '"':
            raise CorpusError(where + ": expected a JSON string at offset " + str(at))
        try:
            value, at = decoder.raw_decode(text, at)
        except ValueError as error:
            raise CorpusError(where + ": " + str(error)) from None
        count = 1
        match = TERM_COUNT_RE.match(text, at)
        if match:
            count = int(match.group(1))
            at = match.end()
        terms.append((value, count))
        if at == len(text):
            return terms
        if text.startswith(" + ", at):
            at += 3
            continue
        raise CorpusError(where + ": expected ' + ' or the end at offset " + str(at))


def utf16_units(text):
    """The UTF-16 code units of a string that may hold lone surrogates."""
    data = text.encode("utf-16-le", "surrogatepass")
    return [data[i] | (data[i + 1] << 8) for i in range(0, len(data), 2)]


def expression_length(terms):
    return sum(len(utf16_units(value)) * count for value, count in terms)


def js_string(value):
    out = ['"']
    for unit in utf16_units(value):
        if 0x20 <= unit <= 0x7E and unit not in (0x22, 0x5C):
            out.append(chr(unit))
        else:
            out.append("\\u%04x" % unit)
    out.append('"')
    return "".join(out)


def js_expression(terms):
    parts = []
    for value, count in terms:
        literal = js_string(value)
        if count == 1:
            parts.append(literal)
        elif count == 0 or value == "":
            parts.append('""')
        else:
            parts.append(literal + ".repeat(" + str(count) + ")")
    return " + ".join(parts)


def expand_repeats(text):
    """Expands each 'token^k' of an answer or a status into k copies of the token."""
    words = []
    for word in text.split(" "):
        if "^" in word:
            token, count = word.split("^")
            words.extend([token] * int(count))
        else:
            words.append(word)
    return " ".join(words)


def read_corpus(data):
    try:
        text = data.decode("utf-8")
    except UnicodeDecodeError as error:
        raise CorpusError("the corpus is not UTF-8: " + str(error)) from None
    if text.startswith("﻿"):
        raise CorpusError("the corpus starts with a byte-order mark")
    if "\r" in text:
        raise CorpusError("the corpus holds a carriage return; it is LF-only")
    if not text.endswith("\n"):
        raise CorpusError("the corpus does not end with a line feed")
    header = {}
    rows = []
    columns_seen = False
    ids = set()
    for number, line in enumerate(text[:-1].split("\n"), start=1):
        where = "line " + str(number)
        if line.startswith("#"):
            if columns_seen:
                raise CorpusError(where + ": a comment after the column line")
            match = re.match(r"^# ([a-z0-9-]+): (\S+)$", line)
            if match:
                header[match.group(1)] = match.group(2)
            continue
        fields = line.split("\t")
        if not columns_seen:
            if fields != COLUMNS:
                raise CorpusError(where + ": the column line is not " + "\\t".join(COLUMNS))
            columns_seen = True
            continue
        if len(fields) != len(COLUMNS):
            raise CorpusError(where + ": " + str(len(fields)) + " fields, not " + str(len(COLUMNS)))
        row = dict(zip(COLUMNS, fields))
        check_row(row, where + " (" + row["id"] + ")")
        if row["id"] in ids:
            raise CorpusError(where + ": a second row " + row["id"])
        ids.add(row["id"])
        rows.append(row)
    if header.get("corpus-format") != "1":
        raise CorpusError("the header does not declare corpus-format: 1")
    for key in ("stack-bytes", "js-wall-clock-limit-seconds"):
        if not re.match(r"^[1-9][0-9]*$", header.get(key, "")):
            raise CorpusError("the header does not declare " + key)
    if not rows:
        raise CorpusError("the corpus has no row")
    return header, rows


def check_row(row, where):
    if not ID_RE.match(row["id"]) or row["id"][0] != row["class"]:
        raise CorpusError(where + ": an id is a class letter and two digits, and its letter is the class")
    if row["class"] not in CLASSES or len(row["class"]) != 1:
        raise CorpusError(where + ": unknown class " + row["class"])
    if not OP_RE.match(row["op"]):
        raise CorpusError(where + ": unknown operation " + row["op"])
    flags = row["flags"]
    if flags != "-":
        if not FLAGS_RE.match(flags) or len(set(flags)) != len(flags) or ("u" in flags and "v" in flags):
            raise CorpusError(where + ": flags are '-' or distinct letters of dimsuv, never u with v")
    parse_expression(row["pattern"], where + " pattern")
    matching = row["op"].startswith(("match@", "anchored@"))
    if matching:
        parse_expression(row["subject"], where + " subject")
    elif row["subject"] != "-":
        raise CorpusError(where + ": a compile or validate row has no subject")
    if not POLICY_RE.match(row["policy"]):
        raise CorpusError(where + ": the policy is not the six keys in their order")
    status = row["status"]
    if not STATUS_RE.match(status):
        raise CorpusError(where + ": unknown status " + status)
    if matching and status == "ok":
        raise CorpusError(where + ": 'ok' is a compile or validate status only")
    if not matching and (status.startswith("match ") or status == "nomatch"):
        raise CorpusError(where + ": a compile or validate row declares no match")
    for engine, subject_key, outcome_key, outcome_re in (
            ("vm", "vm_subject", "vm_outcome", VM_OUTCOME_RE),
            ("js", "js_subject", "js_outcome", JS_OUTCOME_RE)):
        present = row["vm_fuel"] != "-" if engine == "vm" else row["js_subject"] != "-"
        if not present:
            empty = [row[subject_key], row[outcome_key]] + ([row["vm_fuel"]] if engine == "vm" else [])
            if any(value != "-" for value in empty):
                raise CorpusError(where + ": a row outside the " + engine + " population leaves its columns '-'")
            continue
        if row["op"] == "validate":
            raise CorpusError(where + ": a validate row is a library operation, outside both engines")
        if engine == "vm" and not re.match(r"^[1-9][0-9]*$", row["vm_fuel"]):
            raise CorpusError(where + ": vm_fuel is a positive integer or '-'")
        if row[subject_key] != "=":
            if not matching:
                raise CorpusError(where + ": a compile row's engine subject is '='")
            parse_expression(row[subject_key], where + " " + subject_key)
        if not outcome_re.match(row[outcome_key]):
            raise CorpusError(where + ": unknown " + outcome_key + " " + row[outcome_key])


ANSWER_FUNCTION = """function answerOf(m) {
  if (m === null) {
    return "null";
  }
  var text = m.index + "," + (m.index + m[0].length);
  for (var i = 1; i < m.length; i++) {
    text = text + " " + (m[i] === undefined ? "-" : String(m[i].length));
  }
  return text;
}
"""


def program_body(row, subject_terms, indent):
    """The statements that build the RegExp, run the row's operation and leave its answer in
    `answer`. A compile row also runs exec on the empty string, so that an engine that compiles
    lazily compiles."""
    op = row["op"]
    flags = "" if row["flags"] == "-" else row["flags"]
    lines = ["var pattern = " + js_expression(parse_expression(row["pattern"], row["id"])) + ";"]
    if op == "compile":
        lines.append("var re = new RegExp(pattern, " + js_string(flags) + ");")
        lines.append('var answer = answerOf(re.exec(""));')
    else:
        kind, index = op.split("@")
        flags += "g" if kind == "match" else "y"
        lines.append("var subject = " + js_expression(subject_terms) + ";")
        lines.append("var re = new RegExp(pattern, " + js_string(flags) + ");")
        lines.append("re.lastIndex = " + index + ";")
        lines.append("var answer = answerOf(re.exec(subject));")
    return "".join(indent + line + "\n" for line in lines)


def write_programs(engine, out_dir, header, rows, corpus_sha, commit):
    is_vm = engine == "broiler-vm"
    subject_key = "vm_subject" if is_vm else "js_subject"
    outcome_key = "vm_outcome" if is_vm else "js_outcome"
    selected = [row for row in rows if (row["vm_fuel"] if is_vm else row["js_subject"]) != "-"]
    manifest = [
        "# Adversarial programs for " + engine + ", written by Broiler.RegEx's eng/adversarial-programs.py "
        "--engine " + engine + " from that repository's " + CORPUS_REL + ".",
        "# corpus: " + CORPUS_REL,
        "# corpus-sha256: " + corpus_sha,
        "# broiler-regex-commit: " + commit,
        "# generator-format: " + GENERATOR_FORMAT,
        "# engine: " + engine,
    ]
    if is_vm:
        manifest.append("# run: each program alone, one process of Broiler.VM's command-line composition root, "
                        "with the row's fuel allowance; the program's completion value is its answer, an error "
                        "is left uncaught")
        columns = ["id", "class", "op", "program", "program-sha256", "pattern-code-units",
                   "subject-code-units", "fuel", "declared-outcome"]
    else:
        manifest.append("# wall-clock-limit-seconds: " + header["js-wall-clock-limit-seconds"])
        manifest.append("# run: each program alone, one process of the shell BroilerJS with --script-host and "
                        "the invariant culture; it prints one line, 'adversarial-outcome <id> <outcome>'")
        columns = ["id", "class", "op", "program", "program-sha256", "pattern-code-units",
                   "subject-code-units", "declared-outcome"]
    manifest.append("\t".join(columns))

    files = {}
    for row in selected:
        subject_text = row["subject"] if row[subject_key] == "=" else row[subject_key]
        subject_terms = parse_expression(subject_text, row["id"]) if subject_text != "-" else []
        pattern_terms = parse_expression(row["pattern"], row["id"])
        declared = expand_repeats(row[outcome_key])
        comment = [
            "// SPDX-FileCopyrightText: 2026 Broiler Platform contributors",
            "// SPDX-License-Identifier: Apache-2.0",
            "//",
            "// GENERATED by Broiler.RegEx's eng/adversarial-programs.py --engine " + engine + "; do not edit.",
            "// Case " + row["id"] + " (class " + row["class"] + ") of " + CORPUS_REL + ", SHA-256 " + corpus_sha + ",",
            "// at Broiler.RegEx commit " + commit + ". Operation: " + row["op"] + ".",
        ]
        if is_vm:
            comment.append("// Fuel allowance: " + row["vm_fuel"] + ". Declared outcome: " + row[outcome_key] + ".")
        else:
            comment.append("// Declared outcome: " + row[outcome_key] + ".")
        text = "\n".join(comment) + "\n\n" + ANSWER_FUNCTION + "\n"
        if is_vm:
            text += program_body(row, subject_terms, "") + "answer;\n"
        else:
            text += ("(function () {\n"
                     "  var outcome;\n"
                     "  try {\n"
                     + program_body(row, subject_terms, "    ")
                     + '    outcome = "completed " + answer;\n'
                     "  } catch (e) {\n"
                     '    outcome = "threw " + (e !== null && e !== undefined && e.constructor ? e.constructor.name : String(e));\n'
                     "  }\n"
                     '  print("adversarial-outcome ' + row["id"] + ' " + outcome);\n'
                     "})();\n")
        data = text.encode("ascii")
        name = row["id"] + ".js"
        files[name] = data
        cells = [row["id"], row["class"], row["op"], name, hashlib.sha256(data).hexdigest(),
                 str(expression_length(pattern_terms)), str(expression_length(subject_terms))]
        if is_vm:
            cells.append(row["vm_fuel"])
        cells.append(declared)
        manifest.append("\t".join(cells))
    files["manifest.tsv"] = ("\n".join(manifest) + "\n").encode("ascii")

    for name in sorted(files):
        with open(os.path.join(out_dir, name), "wb") as handle:
            handle.write(files[name])
    return len(selected)


def main():
    parser = argparse.ArgumentParser(description="Write one engine's adversarial programs from " + CORPUS_REL + ".")
    parser.add_argument("--engine", required=True, choices=ENGINES)
    parser.add_argument("--out", required=True, help="a directory that does not exist yet, or is empty")
    args = parser.parse_args()

    out_dir = os.path.abspath(args.out)
    if os.path.exists(out_dir):
        if not os.path.isdir(out_dir) or os.listdir(out_dir):
            fail(args.out + " exists and is not an empty directory")
    commit = committed_state([CORPUS_REL, SCRIPT_REL])
    with open(os.path.join(ROOT, CORPUS_REL), "rb") as handle:
        data = handle.read()
    try:
        header, rows = read_corpus(data)
    except CorpusError as error:
        fail(CORPUS_REL + ": " + str(error))
    os.makedirs(out_dir, exist_ok=True)
    count = write_programs(args.engine, out_dir, header, rows, hashlib.sha256(data).hexdigest(), commit)
    print("adversarial-programs: " + str(count) + " programs for " + args.engine + " in " + args.out)


if __name__ == "__main__":
    main()
