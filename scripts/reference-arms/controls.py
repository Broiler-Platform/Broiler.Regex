#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Bundle sjl-11-001's two negative controls, written 2026-11-17 (SJL-11.2, before the candidate reading, task 4.2).

    python3 -I -B scripts/reference-arms/controls.py copy --root REPO --arm V|J --case ID --out DIR
    python3 -I -B scripts/reference-arms/controls.py assemble --runs DIR --control DIR --arm V|J --class P4|P1 --out DIR
    python3 -I -B scripts/reference-arms/controls.py check --dir DIR --arm V|J --class P4|P1 --case ID --main DIR

reference-arms.md's negative control reads "a copy of arm V with one result changed for one input of P4, and a copy of
arm J with one result changed for one input of P1, each run once; the comparison reports each as a difference and does
not classify it; the copies are then removed", and its MET needs "both negative controls were reported unclassified".
tests/ReferenceArms/README.md ("The negative controls") says what a control copy is: the arm's directory copied to a
scratch directory, its Program.cs's NEGATIVE-CONTROL line given `Harness.ControlCase = "<case id>";` and nothing else;
the harness then changes that case's answer itself (`Harness.Planted`) and names the case in the run file, which the
classifier reads to list the difference **unclassified**. run.sh takes both, after the classification, with this script:

**copy** copies the files the arm builds from (the repository's Directory.Build.props, Directory.Packages.props,
NuGet.config and eng/, and tests/ReferenceArms/Common and tests/ReferenceArms/ARM, without bin and obj) from REPO
into DIR, keeping their places, and adds that one line after the NEGATIVE-CONTROL comment of the copy's Program.cs. It
refuses a Program.cs with no such comment or more than one, and a case id the line cannot hold as written.

**assemble** writes two runs directories of symbolic links into DIR (--out), for the class the control runs over:
`control-runs`, every run of the class from the run's DIR (--runs) but the arm's, which is the control copy's
(--control: its answers and its run file), and `revert-runs`, every run of the class as the run has it, the arm's
own among them: the comparison without the change. The revert is the run's own answers, linked, not an arm run again
without the change; so its classification is the run's for that class, and condition 6 below holds it so. run.sh
compares each (DIR/control, DIR/revert), classifies each as the run's classification is taken (`--reduction`,
`--exclude`), and classifies the control's comparison once more against the revert's run files (DIR/blind): the same
changed answer with no control case named, as a changed result nobody declared would be read.

**check** reads DIR (the directory assemble wrote into, with the three classifications) and the run's classification
(--main, run.sh's OUT/classification), and holds the control **reported unclassified** when every one of these is so,
and exits 1 naming each that is not:
1. the control copy's run file names the case, and the revert's does not;
2. the control copy's answers differ from the arm's in the run in one line, the case's, and in nothing else;
3. the control's comparison is the revert's with one line more, the case's for the arm's run (the case is no
   difference in the revert: S and the arm agree on it there);
4. the control's classification is the revert's with that one line more, listed `unclassified`, and its
   classification.json names the case among its controls;
5. (added 2026-11-17, at review) the blind classification is the revert's with that one line more, in class (c), its
   counts are the revert's with one more in (c), and its classification.json names no control: the changed result,
   undeclared, makes the classification fail, so that conditions 1 to 4 cannot hold only because the run file names
   the case (the classifier lists a named case unclassified before it reads anything else);
6. (added 2026-11-17, at review) the revert's classification is the run's (--main) restricted to the class, line for
   line: reverted, the classification is the run's own, and passes where the run's does (no (c) in that class).
It writes DIR/control.json: each condition, the class the blind classification gives the case, the counts of the
control's, the revert's and the blind classification, and the class (c) differences of the class in the revert and
in the run.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import sys
from itertools import zip_longest
from pathlib import Path

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from population_format import open_text  # noqa: E402

MARK = "// NEGATIVE-CONTROL:"
ROOT_FILES = ("Directory.Build.props", "Directory.Packages.props", "NuGet.config")


def plant(program: str, case: str) -> str:
    """Program.cs with `Harness.ControlCase = "<case>";` on the line after its NEGATIVE-CONTROL comment."""
    if any(ch in case for ch in '"\\\r\n') or not case:
        raise SystemExit(f"a case id the line cannot hold as written: {case!r}")
    lines = program.split("\n")
    marks = [i for i, line in enumerate(lines) if line.strip().startswith(MARK)]
    if len(marks) != 1:
        raise SystemExit(f"Program.cs holds {len(marks)} NEGATIVE-CONTROL comments, not one")
    line = lines[marks[0]]
    indent = line[:len(line) - len(line.lstrip())]
    lines.insert(marks[0] + 1, f'{indent}Harness.ControlCase = "{case}";')
    return "\n".join(lines)


def copy(args) -> int:
    root, out = args.root.resolve(), args.out
    if out.exists():
        raise SystemExit(f"{out} exists")
    ignore = shutil.ignore_patterns("bin", "obj")
    for name in ROOT_FILES:
        (out / name).parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(root / name, out / name)
    for name in ("eng", "tests/ReferenceArms/Common", f"tests/ReferenceArms/{args.arm}"):
        shutil.copytree(root / name, out / name, ignore=ignore)
    program = out / "tests" / "ReferenceArms" / args.arm / "Program.cs"
    program.write_text(plant(program.read_text(encoding="utf-8"), args.case), encoding="utf-8")
    print(f"copied arm {args.arm} to {out}; its Program.cs sets the control case {args.case}")
    return 0


def link(source: Path, target: Path) -> None:
    target.parent.mkdir(parents=True, exist_ok=True)
    os.symlink(source.resolve(), target)


def assemble(args) -> int:
    runs, control, out = args.runs, args.control, args.out
    arm_file = f"{args.klass}.{args.arm}.tsv.gz"
    run_file = f"run.{args.arm}.json"
    for name in ("control-runs", "revert-runs"):
        if (out / name).exists():
            raise SystemExit(f"{out / name} exists")
    if not (control / arm_file).exists() or not (control / run_file).exists():
        raise SystemExit(f"the control copy's run left no {arm_file} or {run_file} in {control}")
    answers = sorted(runs.glob(f"{args.klass}.*.tsv.gz"))
    if not (runs / arm_file).exists() or not (runs / f"{args.klass}.S.tsv.gz").exists():
        raise SystemExit(f"the run holds no {arm_file} or {args.klass}.S.tsv.gz in {runs}")
    for path in answers + sorted(runs.glob("run.*.json")):
        link(path, out / "revert-runs" / path.name)
        source = control / path.name if path.name in (arm_file, run_file) else path
        link(source, out / "control-runs" / path.name)
    print(f"assembled {len(answers)} answer files of {args.klass}: {out / 'control-runs'}, {out / 'revert-runs'}")
    return 0


def lines(path: Path) -> list[str]:
    with open_text(path) as handle:
        return [line.rstrip("\n") for line in handle if not line.startswith("#")]


def fields(line: str) -> list[str]:
    return line.split("\t")


def control_case(path: Path):
    return json.loads(path.read_text(encoding="utf-8")).get("control-case")


def plus_one_c(counts: dict) -> dict:
    """classification.json's counts with one more difference in class (c); counts names no class it holds none of."""
    result = dict(counts)
    result["c"] = result.get("c", 0) + 1
    return result


def of_class(path: Path, klass: str):
    """The lines of a classified.tsv.gz whose class (its second column) is klass, in order, read one at a time."""
    with open_text(path) as handle:
        for line in handle:
            if not line.startswith("#") and fields(line)[1] == klass:
                yield line.rstrip("\n")


def check(args) -> int:
    d, arm, case = args.dir, args.arm, args.case
    problems: list[str] = []
    found: dict = {"arm": arm, "class": args.klass, "case": case}

    # 1. The run files.
    named, unnamed = control_case(d / "control-runs" / f"run.{arm}.json"), control_case(d / "revert-runs" / f"run.{arm}.json")
    found["control-case"] = named
    if named != case:
        problems.append(f"the control copy's run file names {named!r}, not {case}")
    if unnamed is not None:
        problems.append(f"the run's own run file names a control case, {unnamed!r}")

    # 2. One answer changed, the case's.
    mine = lines(d / "control-runs" / f"{args.klass}.{arm}.tsv.gz")
    theirs = lines(d / "revert-runs" / f"{args.klass}.{arm}.tsv.gz")
    changed = [(a, b) for a, b in zip(mine, theirs) if a != b]
    found["answers-changed"] = [{"control": a, "run": b} for a, b in changed]
    if len(mine) != len(theirs):
        problems.append(f"the control copy answered {len(mine)} cases, the arm {len(theirs)}")
    if len(changed) != 1 or fields(changed[0][0])[0] != case or fields(changed[0][1])[0] != case:
        problems.append(f"the control copy's answers differ from the arm's in {len(changed)} lines, not in the case's alone")

    # 3. The comparison reports the case, and nothing else changes.
    control = lines(d / "control" / "differences.tsv.gz")
    revert = lines(d / "revert" / "differences.tsv.gz")
    planted = [line for line in control if fields(line)[0] == case and fields(line)[3] == arm]
    found["reported"] = planted
    if len(planted) != 1:
        problems.append(f"the control's comparison holds {len(planted)} differences of the case in run {arm}, not one")
    if any(fields(line)[0] == case and fields(line)[3] == arm for line in revert):
        problems.append("the case is already a difference of the arm's own run: choose a case S and the arm agree on")
    if [line for line in control if line not in planted] != revert:
        problems.append("the control's comparison differs from the revert's in more than the case's line")

    # 4. The classification lists it unclassified, and every other line as the revert's.
    classified = lines(d / "control" / "classified.tsv.gz")
    reverted = lines(d / "revert" / "classified.tsv.gz")
    listed = [line for line in classified if fields(line)[0] == case and fields(line)[3] == arm]
    found["listed"] = listed
    if len(listed) != 1 or fields(listed[0])[7] != "unclassified":
        problems.append(f"the control's classification lists the case as {[fields(x)[7] for x in listed]}, not unclassified")
    if [line for line in classified if line not in listed] != reverted:
        problems.append("the control's classification differs from the revert's in more than the case's line")
    summary = json.loads((d / "control" / "classification.json").read_text(encoding="utf-8"))
    if summary.get("controls", {}).get(arm) != case:
        problems.append(f"the control's classification.json names the controls {summary.get('controls')}")

    # 5. Unnamed, the same change is class (c): the classification fails.
    unnamed_lines = lines(d / "blind" / "classified.tsv.gz")
    blind = [line for line in unnamed_lines if fields(line)[0] == case and fields(line)[3] == arm]
    found["blind"] = [{"class": fields(x)[7], "row": fields(x)[8], "why": fields(x)[9]} for x in blind]
    summaries = {name: json.loads((d / name / "classification.json").read_text(encoding="utf-8"))
                 for name in ("control", "revert", "blind")}
    for name, value in summaries.items():
        found[f"counts-{name}"] = value["counts"]
    if len(blind) != 1 or fields(blind[0])[7] != "c":
        problems.append(f"the blind classification gives the case {[fields(x)[7] for x in blind]}, not class (c)")
    if [line for line in unnamed_lines if line not in blind] != reverted:
        problems.append("the blind classification differs from the revert's in more than the case's line")
    if summaries["blind"]["counts"] != plus_one_c(summaries["revert"]["counts"]):
        problems.append(f"the blind classification counts {summaries['blind']['counts']}, not the revert's with one "
                        "more in class (c)")
    if summaries["blind"].get("controls"):
        problems.append(f"the blind classification names the controls {summaries['blind'].get('controls')}")
    del unnamed_lines

    # 6. Reverted, the classification is the run's for the class.
    main_c = revert_c = 0
    same = True
    for ours, run in zip_longest(reverted, of_class(args.main / "classified.tsv.gz", args.klass)):
        same = same and ours == run
        revert_c += ours is not None and fields(ours)[7] == "c"
        main_c += run is not None and fields(run)[7] == "c"
    found["c-revert"], found["c-run"] = revert_c, main_c
    if not same:
        problems.append(f"the revert's classification is not the run's restricted to {args.klass}, line for line")

    found["problems"] = problems
    found["reported-unclassified"] = not problems
    (d / "control.json").write_text(json.dumps(found, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps({"arm": arm, "case": case, "reported-unclassified": not problems, "problems": problems,
                      "blind": found["blind"]}, sort_keys=True))
    return 1 if problems else 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest="mode", required=True)
    c = sub.add_parser("copy")
    c.add_argument("--root", required=True, type=Path)
    c.add_argument("--arm", required=True, choices=("V", "J"))
    c.add_argument("--case", required=True)
    c.add_argument("--out", required=True, type=Path)
    a = sub.add_parser("assemble")
    a.add_argument("--runs", required=True, type=Path)
    a.add_argument("--control", required=True, type=Path)
    a.add_argument("--arm", required=True, choices=("V", "J"))
    a.add_argument("--class", dest="klass", required=True)
    a.add_argument("--out", required=True, type=Path)
    k = sub.add_parser("check")
    k.add_argument("--dir", required=True, type=Path)
    k.add_argument("--arm", required=True, choices=("V", "J"))
    k.add_argument("--class", dest="klass", required=True)
    k.add_argument("--case", required=True)
    k.add_argument("--main", required=True, type=Path)
    args = parser.parse_args(argv)
    return {"copy": copy, "assemble": assemble, "check": check}[args.mode](args)


if __name__ == "__main__":
    sys.exit(main())
