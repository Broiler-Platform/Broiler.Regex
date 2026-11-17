#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Hold a report of Broiler.JS's test262 runner to bundle sjl-11-001's check B-1, the host job's verdict.

    python3 -I scripts/test262-host/compare.py --report test262-host.json --exit CODE \\
        --expected test262/host-expected-failures.txt --suite SUITE --suite-ref SHA \\
        --population test/built-ins/RegExp ... [--require PATH ...] [--summary FILE]

Written 2026-11-17, at task 2.10 of SJL-11.2 (the host job, CI's test262-host). The report is the JSON that
Broiler.JS's scripts/compliance/run_test262.py writes with --output; CODE is that runner's exit. B-1
(docs/evidence/sjl-11-001/decision-rule.md, part B) reads: "green if and only if no row fails or times out but those in
test262/host-expected-failures.txt, each entry naming its path, the host member it fails in and why that member is the
host's (the RegExp object, the protocols, species, GetSubstitution's evaluation, lastIndex), and no entry is attributed
to pattern syntax, compilation or matching; and every row of a file listing regexp-duplicate-named-groups passes". This
script holds the report to that, and to the run it was asked for:

- the run's own settings: the suite commit SHA, negative tests included, the original variant alone and no minifier,
  the requested paths exactly the population given here, and the runner's exit 0 when no row fails or times out and 1
  otherwise (any other exit is the runner's crash);
- one row for every test file of the population in the suite checkout (every *.js under a directory, but the
  *_FIXTURE.js modules the runner leaves out, as test262's INTERPRETING.md does), and no row outside it, so a file the
  runner drops is seen;
- a row that fails or times out and is not listed fails the check, and so does a row the runner skipped: B-1 asks the
  population run, and a skip is no verdict;
- a listed path not in the population, or whose row does not fail or time out, fails the check: a listed file that
  passes is a list that is no longer exact;
- each entry of the list: three tab-separated fields, the path, the host member (one of the five B-1 names) and why,
  not empty; no path twice; and no entry for a file listing regexp-duplicate-named-groups, every row of which must pass;
- every file of the population whose front matter lists regexp-duplicate-named-groups passes, and each --require path
  (the four files task 2.6 of SJL-11.2 left to this job) is in the population, lists the feature and passes.

It prints each failure as a line beginning FAIL, then a summary, and exits 0 when the report holds and 1 otherwise
(2 for a usage error or an unreadable input). Standard library only.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

FEATURE = "regexp-duplicate-named-groups"

# B-1's own words for the host's members, in the order it gives them.
HOST_MEMBERS = (
    "the RegExp object",
    "the protocols",
    "species",
    "GetSubstitution's evaluation",
    "lastIndex",
)

FAILING = ("failed", "timedOut")
FIXTURE_SUFFIX = "_FIXTURE.js"
FRONT_MATTER = re.compile(r"/\*---(.*?)---\*/", re.DOTALL)


class InputError(Exception):
    """An input that cannot be read as this script needs it."""


def read_expected(path: Path) -> tuple[dict[str, tuple[str, str]], list[str]]:
    """The list's entries by path, and the failures of its form."""
    entries: dict[str, tuple[str, str]] = {}
    failures: list[str] = []
    try:
        text = path.read_text(encoding="utf-8")
    except OSError as exc:
        raise InputError(f"cannot read the expected-failures list {path}: {exc}") from exc
    for number, line in enumerate(text.split("\n"), start=1):
        if not line or line.startswith("#"):
            continue
        fields = line.split("\t")
        if len(fields) != 3 or not all(field.strip() for field in fields):
            failures.append(f"FAIL {path.name}:{number}: an entry is a path, a host member and why, tab-separated")
            continue
        test, member, why = fields
        if member not in HOST_MEMBERS:
            failures.append(
                f"FAIL {path.name}:{number}: {test}: the member {member!r} is not one of B-1's host members "
                f"({', '.join(HOST_MEMBERS)})")
        if test in entries:
            failures.append(f"FAIL {path.name}:{number}: {test} is listed twice")
            continue
        entries[test] = (member, why)
    return entries, failures


def population_files(suite: Path, population: list[str]) -> list[str]:
    """Every test file the population names in the suite checkout, as the runner expands it."""
    files: set[str] = set()
    for requested in population:
        local = suite / requested.strip("/")
        if local.is_file():
            files.add(requested.strip("/"))
        elif local.is_dir():
            files.update(
                child.relative_to(suite).as_posix()
                for child in local.rglob("*.js")
                if child.is_file() and not child.name.endswith(FIXTURE_SUFFIX))
        else:
            raise InputError(f"the population's path {requested} is not in the suite checkout {suite}")
    return sorted(files)


def lists_feature(source: str, feature: str = FEATURE) -> bool:
    """Whether a test file's front matter lists the feature, in flow ([a, b]) or block (- a) form."""
    match = FRONT_MATTER.search(source)
    if match is None:
        return False
    lines = match.group(1).split("\n")
    for index, line in enumerate(lines):
        stripped = line.strip()
        if not stripped.startswith("features:"):
            continue
        rest = stripped[len("features:"):].strip()
        if rest.startswith("["):
            following = iter(lines[index + 1:])
            while "]" not in rest:
                more = next(following, None)
                if more is None:
                    break
                rest += " " + more.strip()
            names = [name.strip() for name in rest.split("]")[0].strip("[").split(",")]
            return feature in names
        for following in lines[index + 1:]:
            item = following.strip()
            if not item.startswith("-"):
                break
            if item[1:].strip() == feature:
                return True
        return False
    return False


def check(report: dict, exit_code: int, expected: dict[str, tuple[str, str]], suite: Path, suite_ref: str,
          population: list[str], required: list[str]) -> tuple[list[str], dict[str, object]]:
    """The failures of the report against B-1, and the counts the summary gives."""
    failures: list[str] = []

    if report.get("suiteRef") != suite_ref:
        failures.append(f"FAIL the run's suite commit is {report.get('suiteRef')!r}, not {suite_ref}")
    if report.get("includeNegative") is not True:
        failures.append("FAIL the run left negative tests out (B-1 runs it with --include-negative)")
    if report.get("variants") != ["original"] or report.get("minifier") != "none":
        failures.append(
            f"FAIL the run's variants are {report.get('variants')!r} with minifier {report.get('minifier')!r}, "
            "not the original variant alone")
    requested = sorted(path.strip("/") for path in report.get("requestedPaths") or [])
    if requested != sorted(path.strip("/") for path in population):
        failures.append(f"FAIL the run's requested paths {requested} are not the population {sorted(population)}")

    rows: dict[str, list[dict]] = {}
    for row in report.get("results") or []:
        rows.setdefault(str(row.get("path")), []).append(row)

    files = population_files(suite, population)
    file_set = set(files)
    for path in files:
        found = rows.get(path, [])
        if len(found) != 1:
            failures.append(f"FAIL {path}: {len(found)} rows in the report, not one")
    for path in sorted(set(rows) - file_set):
        failures.append(f"FAIL {path}: a row outside the population")

    failing = {path for path, found in rows.items() if any(row.get("status") in FAILING for row in found)}
    any_failing_row = any(row.get("status") in FAILING for found in rows.values() for row in found)
    if exit_code not in (0, 1):
        failures.append(f"FAIL the runner exited {exit_code}, which is neither its pass (0) nor its failure (1)")
    elif exit_code != (1 if any_failing_row else 0):
        failures.append(
            f"FAIL the runner exited {exit_code}, but the report holds "
            f"{'a' if any_failing_row else 'no'} row that fails or times out")

    for path in sorted(rows):
        for row in rows[path]:
            status = row.get("status")
            if status in FAILING and path not in expected:
                reason = str(row.get("reason") or row.get("stderr") or "").strip().split("\n")[0][:300]
                failures.append(f"FAIL {path}: {status}, and the expected-failures list does not name it: {reason}")
            elif status == "skipped":
                failures.append(f"FAIL {path}: skipped by the runner ({row.get('reason', '')}), so not run")
            elif status not in FAILING and status != "passed":
                failures.append(f"FAIL {path}: the runner's status {status!r} is not one this check reads")

    for path in sorted(expected):
        if path not in file_set:
            failures.append(f"FAIL {path}: listed, but not in the population")
        elif path not in failing:
            statuses = ", ".join(str(row.get("status")) for row in rows.get(path, [])) or "no row"
            failures.append(f"FAIL {path}: listed, but it does not fail or time out ({statuses})")

    feature_files = []
    for path in files:
        try:
            source = (suite / path).read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError) as exc:
            raise InputError(f"cannot read {path} in the suite checkout: {exc}") from exc
        if lists_feature(source):
            feature_files.append(path)
    for path in feature_files:
        if path in expected:
            failures.append(f"FAIL {path}: lists {FEATURE}, whose every row must pass, and is listed")
        statuses = [row.get("status") for row in rows.get(path, [])]
        if not statuses or any(status != "passed" for status in statuses):
            failures.append(f"FAIL {path}: lists {FEATURE}, and its rows are {statuses or 'none'}, not passed")
    for path in required:
        path = path.strip("/")
        if path not in file_set:
            failures.append(f"FAIL {path}: required, but not in the population")
        elif path not in feature_files:
            failures.append(f"FAIL {path}: required as a file of {FEATURE}, but its front matter does not list it")

    counts = {
        "files": len(files),
        "rows": sum(len(found) for found in rows.values()),
        "passed": sum(1 for found in rows.values() for row in found if row.get("status") == "passed"),
        "failing": len(failing),
        "listed": len(expected),
        "feature-files": len(feature_files),
        "required": len(required),
    }
    return failures, counts


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--report", required=True, type=Path, help="run_test262.py's --output JSON")
    parser.add_argument("--exit", required=True, type=int, dest="exit_code", help="run_test262.py's exit")
    parser.add_argument("--expected", required=True, type=Path, help="test262/host-expected-failures.txt")
    parser.add_argument("--suite", required=True, type=Path, help="the test262 checkout the run read")
    parser.add_argument("--suite-ref", required=True, help="the test262 commit the run must name")
    parser.add_argument("--population", required=True, nargs="+", help="the paths the run was asked for")
    parser.add_argument("--require", action="append", default=[], help="a file of the feature that must pass")
    parser.add_argument("--summary", type=Path, help="also write the failures and the summary here")
    args = parser.parse_args(argv)

    try:
        try:
            report = json.loads(args.report.read_text(encoding="utf-8"))
        except (OSError, ValueError) as exc:
            raise InputError(f"cannot read the report {args.report}: {exc}") from exc
        expected, failures = read_expected(args.expected)
        more, counts = check(report, args.exit_code, expected, args.suite.resolve(), args.suite_ref,
                             args.population, args.require)
    except InputError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2
    failures += more
    verdict = "green" if not failures else "red"
    lines = failures + [
        f"test262-host: {verdict}; {counts['files']} files, {counts['rows']} rows, {counts['passed']} passed, "
        f"{counts['failing']} failing of {counts['listed']} listed; {counts['feature-files']} files list {FEATURE}, "
        f"{counts['required']} of them required by name; {len(failures)} failures",
    ]
    text = "\n".join(lines) + "\n"
    sys.stdout.write(text)
    if args.summary is not None:
        args.summary.write_text(text, encoding="utf-8")
    return 0 if not failures else 1


if __name__ == "__main__":
    sys.exit(main())
