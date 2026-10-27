#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Check a report of the probe's --adversarial mode against what part C of bundle sjl-11-001's rule asks of it.

    python3 eng/adversarial-controls.py --control steps   --corpus tests/Adversarial/corpus.tsv --report r.txt --exit 1
    python3 eng/adversarial-controls.py --control nesting --corpus tests/Adversarial/corpus.tsv --report r.txt --exit 1
    python3 eng/adversarial-controls.py --control none    --corpus tests/Adversarial/corpus.tsv --report r.txt --exit 0
    python3 eng/adversarial-controls.py --holds "adversarial control 1 (part C)" <a control build's probe assembly>
    python3 eng/adversarial-controls.py --lacks "adversarial control" --lacks RegexAdversarialControl <Broiler.Regex.dll> ...

Written 2026-10-27, at task 2.14 of SJL-11.2. The rule's controls are scratch builds of the probe
(probes/Regex.Probe/AdversarialControls.targets): (1) "a scratch build in which LimitExceeded naming steps
is answered NoMatch: every K, A, L, W and G row must be reported"; (2) "a scratch build without the nesting
check: every N row past the limit must end abnormally or differ, and be reported"; "each reverted and the
revert checked". This script reads the corpus itself, independently of the probe, and holds the report to:

- its header naming the corpus's SHA-256, and one line for every row of the corpus, in ordinal order of
  the ids, each ending in a verdict;
- the rows reported (differs or ends abnormally) being exactly the control's rows: steps, every row of
  the classes K, A, L, W and G; nesting, every N row past the limit (declared limit:nesting) that runs;
  none, the revert, no row;
- every other row giving its declared status, but a row whose operation is validate, which may wait for
  task 2.8 (the validate entry) and is then named on the report's waiting line;
- the counts line and the reported line agreeing with the rows, and the probe's exit code being 1 when a
  row is reported and 0 otherwise.

With --holds or --lacks it reads files instead: each must hold (or lack) each text, in UTF-16, as a string
literal of an assembly holds it, or in UTF-8, as its metadata names do. The control's text is searched for in
the control's probe assembly, where it must be found, and in the library's DLL and the package's, where it must
not: the switch is a scratch build's alone.

It exits 0 when the report or the files hold and 1 with each failure named otherwise. Standard library only.
"""

import argparse
import hashlib
import sys

COLUMNS = [
    "id", "class", "op", "pattern", "flags", "subject", "policy", "status",
    "vm_fuel", "vm_subject", "vm_outcome", "js_subject", "js_outcome",
]
VERDICTS = {
    ": gives its declared status": "given",
    ": differs": "differs",
    ": ends abnormally": "abnormal",
    ": not run: waits for task 2.8 (the validate entry)": "waiting",
}


def read_corpus(path):
    with open(path, "rb") as handle:
        data = handle.read()
    rows = []
    columns = False
    for line in data.decode("utf-8").split("\n"):
        line = line.rstrip("\r")
        if not line or line.startswith("#"):
            continue
        fields = line.split("\t")
        if not columns:
            if fields != COLUMNS:
                raise SystemExit("adversarial-controls: the corpus's column line is not its README's")
            columns = True
            continue
        rows.append(dict(zip(COLUMNS, fields)))
    return hashlib.sha256(data).hexdigest(), rows


def expected_reported(control, rows):
    if control == "steps":
        return {row["id"] for row in rows if row["class"] in "KALWG"}
    if control == "nesting":
        return {row["id"] for row in rows if row["class"] == "N" and row["status"] == "limit:nesting"}
    return set()


def search(holds, lacks, files):
    failures = []
    for path in files:
        with open(path, "rb") as handle:
            data = handle.read()
        for text in holds + lacks:
            found = [encoding for encoding in ("utf-16-le", "utf-8") if text.encode(encoding) in data]
            if text in holds and not found:
                failures.append(path + " does not hold '" + text + "'")
            elif text in lacks and found:
                failures.append(path + " holds '" + text + "' (" + ", ".join(found) + ")")
    for failure in failures:
        print("adversarial-controls: " + failure, file=sys.stderr)
    if not failures:
        print("adversarial-controls: " + ", ".join(files) + ": " + "; ".join(
            ["holds '" + text + "'" for text in holds] + ["lacks '" + text + "'" for text in lacks]))
    return 1 if failures else 0


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--control", choices=["steps", "nesting", "none"])
    parser.add_argument("--corpus")
    parser.add_argument("--report")
    parser.add_argument("--exit", type=int, help="the probe's exit code")
    parser.add_argument("--holds", action="append", default=[], help="a text each file must hold")
    parser.add_argument("--lacks", action="append", default=[], help="a text each file must not hold")
    parser.add_argument("files", nargs="*")
    args = parser.parse_args()

    if args.holds or args.lacks:
        if not args.files or args.control or args.corpus or args.report or args.exit is not None:
            parser.error("--holds and --lacks take files, and nothing else")
        return search(args.holds, args.lacks, args.files)
    if args.files or not args.control or not args.corpus or not args.report or args.exit is None:
        parser.error("a report is checked with --control, --corpus, --report and --exit")

    digest, rows = read_corpus(args.corpus)
    with open(args.report, encoding="utf-8") as handle:
        lines = handle.read().split("\n")

    failures = []
    if len(lines) < 4 or lines[0] != "Broiler.Regex.Probe adversarial corpus adversarial-1":
        raise SystemExit("adversarial-controls: " + args.report + " is not a report of the --adversarial mode")
    if lines[1] != "corpus sha256 " + digest:
        failures.append("the report's corpus is not " + args.corpus + " (" + lines[1] + ", the file's sha256 " + digest + ")")

    ids = sorted(row["id"] for row in rows)
    by_id = {row["id"]: row for row in rows}
    verdicts = {}
    order = []
    for line in lines[3:]:
        if not line or line.startswith(("rows ", "waiting: ", "reported: ")):
            continue
        row_id = line[:3]
        verdict = next((name for suffix, name in VERDICTS.items() if line.endswith(suffix)), None)
        if row_id not in by_id or verdict is None:
            failures.append("a line the report should not hold: " + line)
            continue
        verdicts[row_id] = verdict
        order.append(row_id)
    if order != ids:
        failures.append("the report's rows are not the corpus's, once each, in ordinal order of the ids")

    reported = {row_id for row_id, verdict in verdicts.items() if verdict in ("differs", "abnormal")}
    waiting = {row_id for row_id, verdict in verdicts.items() if verdict == "waiting"}
    wanted = expected_reported(args.control, rows) - waiting
    for row_id in sorted(wanted - reported):
        failures.append(row_id + " is not reported, which control '" + args.control + "' requires")
    for row_id in sorted(reported - wanted):
        failures.append(row_id + " is reported (" + verdicts[row_id] + "), which control '" + args.control + "' does not explain")
    for row_id in sorted(waiting):
        if by_id[row_id]["op"] != "validate":
            failures.append(row_id + " waits, but its operation is " + by_id[row_id]["op"] + ", not validate")
    unreached = sorted(expected_reported(args.control, rows) & waiting)
    if unreached:
        print("adversarial-controls: " + " ".join(unreached) + " wait for task 2.8 and are not run, so control '"
              + args.control + "' cannot reach them yet")

    counts = {name: sum(1 for verdict in verdicts.values() if verdict == name) for name in VERDICTS.values()}
    summary = ("rows " + str(len(verdicts)) + ": " + str(counts["given"]) + " give their declared status, "
               + str(counts["differs"]) + " differ, " + str(counts["abnormal"]) + " end abnormally, "
               + str(counts["waiting"]) + " wait for task 2.8 (the validate entry)")
    if summary not in lines:
        failures.append("the report's counts are not its rows': expected '" + summary + "'")
    reported_line = "reported: " + (" ".join(sorted(reported)) if reported else "none")
    if reported_line not in lines:
        failures.append("the report's reported line is not its rows': expected '" + reported_line + "'")
    if args.exit != (1 if reported else 0):
        failures.append("the probe exited " + str(args.exit) + ", where " + str(1 if reported else 0) + " was due")

    for failure in failures:
        print("adversarial-controls: " + failure, file=sys.stderr)
    if failures:
        return 1
    print("adversarial-controls: control '" + args.control + "' holds: " + str(len(reported)) + " rows reported ("
          + (" ".join(sorted(reported)) or "none") + "), " + str(counts["given"]) + " give their declared status, "
          + str(len(waiting)) + " wait for task 2.8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
