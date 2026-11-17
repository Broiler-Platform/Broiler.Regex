#!/usr/bin/env bash
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
#
# Bundle sjl-11-001's reference arms end to end: build the arms, write the population, run every arm (J under the
# invariant culture, tr-TR and lt-LT), compare, classify, reduce, classify under the rule's revisions, and take the two
# negative controls. README.md beside this file says what each step is.
#
#   scripts/reference-arms/run.sh OUT --test262 DIR --unicode DIR --broiler-js DIR --commit SHA \
#       --correspondence FILE [--vectors DIR] [--classes P1,...] [--control-v ID] [--control-j ID]
#       [--classify-only | --controls-only | --calibrate]
#
# OUT receives the population, the arms' answers, the comparison and the classification; nothing is written into the
# repository but the arms' build output (tests/ReferenceArms/*/bin, obj). Each step's command is the first line of its
# log in OUT/logs. The script stops, with exit 1, at the first step that fails. The classifier's exit 1 (a class (c)
# difference) is not a failure of the script and is kept in its log; its exit 2 (it refused) or 3 (it failed), or a
# classification that leaves no classification.json, is. The fast-path differential's exit 1 (a disagreement) is
# kept the same way; any other failure of it fails the script. --classify-only runs the classification alone (below),
# over the comparison an earlier run left in OUT.
#
# The classification (revised 2026-11-17, SJL-11.2, before the candidate reading; until then the script stopped at the
# first item below). The rule as revised (reference-arms-revised-2026-11-16.md, option R, which the owner confirmed on
# 2026-11-17; -16b, the owner's exclusion from P4; -16c, the owner's ruling on P3's O06) reads the classification after
# the declared reduction, in this order, each logged with its command:
#   1. classify.py over the comparison as the rule was filed (OUT/comparison: classification.json, classified.tsv.gz),
#      which leaves in class (c) the differences the arbiter cannot reach;
#   2. the reduction (OUT/reduction): reduce.py prefixes over that classification, the arms S, V and J (three cultures)
#      over the reduced population with the same stop list, compare.py over their answers, classify.py over that
#      comparison (with --exclude), and reduce.py read (outcomes.tsv, outcomes.json);
#   3. classify.py with --reduction OUT/reduction and --exclude scripts/reference-arms/p4-excluded.tsv (OUT/
#      classification: classification.json, classified.tsv.gz): the classification the rule as revised reads, and the
#      one whose exit the script reports.
# Every classification shares one arbiter cache, OUT/arbiter-cache.jsonl.gz (an answer depends only on the input, the
# arbiter's commit and the cut-off), and each one's exit is read as the paragraph above says; the script says "class
# (c) differences" after 3 only. --classify-only runs 1 to 3 over the comparison an earlier run left in OUT; it reads the
# reduction an earlier run left in OUT/reduction when reduce.py check finds it is that comparison's (the same inputs
# left unreachable), fails when it is not, and runs it (building S, V and J) when there is none.
#
# The negative controls (reference-arms.md: "a copy of arm V with one result changed for one input of P4, and a copy of
# arm J with one result changed for one input of P1, each run once; the comparison reports each as a difference and
# does not classify it; the copies are then removed"), after the fast-path differential: for each, controls.py copies
# the arm to OUT/controls/ARM/copy and sets its control case (tests/ReferenceArms/README.md, "The negative controls"),
# the copy is built and run once over its class, the copy is removed, and the class's runs with the copy's answers
# (control) and with the arm's own (revert) are compared and classified as in 3, the control's comparison once more
# against the revert's run files (blind: no control case named); controls.py check then holds the control reported
# unclassified, or fails the script (OUT/controls/ARM/control.json). The cases are task 2.13's, P4-01234-u-05 for V and
# P1-regexp:matching:m1-notes:0001 for J, unless --control-v or --control-j names another. A control whose class the
# run does not hold (a partial run) is not taken, and its log says so. --controls-only takes them over an earlier run
# in OUT that holds the classification of 3.
#
# The arms read the stop list tests/ReferenceArms/stops.tsv: a case it holds is answered as the harness's stop without
# running, and every other case runs to its end under backstops (tests/ReferenceArms/Common/Limits.cs), so that no
# limit of time decides an answer and the listings regenerate byte for byte. A run in which a case the list does not
# hold reaches a backstop ends with exit 1 after the classification, as it is not reproducible. --calibrate instead
# runs the arms under the time limits and writes OUT/stops.tsv, the list a calibration makes (scripts/reference-arms/
# stops.py), and stops there: it compares and classifies nothing.
set -euo pipefail
# Rule SL10 reads every file of the tree, and a .pyc holds a CR: -B (which -I does not imply, and -I ignores
# PYTHONDONTWRITEBYTECODE) and the variable, for the unittest run, keep Python from writing them.
export PYTHONDONTWRITEBYTECODE=1

out=$(realpath -m "${1:?usage: run.sh OUT --test262 DIR --unicode DIR --broiler-js DIR --commit SHA --correspondence FILE [--vectors DIR] [--classes LIST] [--control-v ID] [--control-j ID] [--classify-only | --controls-only | --calibrate]}")
shift
test262="" unicode="" js="" commit="" correspondence="" vectors="" classes="P1,P2,P3,P4,P5,P6,P7" only=""
control_v="P4-01234-u-05" control_j="P1-regexp:matching:m1-notes:0001"
while [ $# -gt 0 ]; do
  case "$1" in
    --classify-only) only=classify; shift; continue ;;
    --controls-only) only=controls; shift; continue ;;
    --calibrate) only=calibrate; shift; continue ;;
    --control-v) control_v=$2 ;;
    --control-j) control_j=$2 ;;
    --test262) test262=$2 ;;
    --unicode) unicode=$2 ;;
    --broiler-js) js=$2 ;;
    --commit) commit=$2 ;;
    --correspondence) correspondence=$2 ;;
    --vectors) vectors=$2 ;;
    --classes) classes=$2 ;;
    *) echo "unknown option $1" >&2; exit 2 ;;
  esac
  shift 2
done

root=$(git -C "$(dirname "$0")" rev-parse --show-toplevel)
cd "$root"
mkdir -p "$out/logs"
step() {
  local log=$1
  shift
  echo "\$ $*" > "$out/logs/$log.log"
  if ! "$@" >> "$out/logs/$log.log" 2>&1; then
    echo "step $log failed; see $out/logs/$log.log" >&2
    exit 1
  fi
}

# One classification, logged as NAME with its command: the classifier's exit 0 (no class (c)) or 1 (a class (c)
# difference) is kept in the log, 2 (it refused) or 3 (it failed) fails the script, and so does a run that leaves no
# classification.json in DIR, which is removed first, so that a file an earlier run left is not read as this one's.
classify_into() {
  local name=$1 dir=$2
  shift 2
  local result="$dir/classification.json" status=0
  mkdir -p "$dir"
  rm -f "$result"
  local command=(python3 -I -B scripts/reference-arms/classify.py "$@" --out "$dir" --broiler-js "$js" --commit "$commit"
    --correspondence "$correspondence" --unicode-pins "$unicode/data/unicode/pins" --cache "$out/arbiter-cache.jsonl.gz")
  echo "\$ ${command[*]}" > "$out/logs/$name.log"
  "${command[@]}" >> "$out/logs/$name.log" 2>&1 || status=$?
  echo "exit $status" >> "$out/logs/$name.log"
  case "$status" in
    0 | 1) ;;
    2) echo "the classifier refused (exit 2); see $out/logs/$name.log" >&2; exit 1 ;;
    *) echo "the classifier failed (exit $status); see $out/logs/$name.log" >&2; exit 1 ;;
  esac
  if [ ! -s "$result" ]; then
    echo "the classifier exited $status and wrote no $result; see $out/logs/$name.log" >&2
    exit 1
  fi
  classified_status=$status
}

stops=tests/ReferenceArms/stops.tsv
if [ "$only" = calibrate ]; then
  mode=(--calibrate)
else
  mode=(--stops "$stops")
fi
bin() { echo "tests/ReferenceArms/$1/bin/Release/net10.0/Regex.ReferenceArms.$1.dll"; }
# The rule as revised: the declared reduction and the owner's exclusion from P4.
revised=(--reduction "$out/reduction" --exclude scripts/reference-arms/p4-excluded.tsv)

# Every arm run over one population into one runs directory, J under the three cultures.
run_arms() {
  local prefix=$1 population=$2 runs=$3 list=$4
  step "${prefix}arm-S" dotnet "$(bin S)" --population "$population" --out "$runs" --classes "$list" "${mode[@]}"
  step "${prefix}arm-V" dotnet "$(bin V)" --population "$population" --out "$runs" --classes "$list" "${mode[@]}"
  step "${prefix}arm-J" dotnet "$(bin J)" --population "$population" --out "$runs" --classes "$list" "${mode[@]}"
  step "${prefix}arm-J-tr-TR" dotnet "$(bin J)" --population "$population" --out "$runs" --classes "$list" --culture tr-TR "${mode[@]}"
  step "${prefix}arm-J-lt-LT" dotnet "$(bin J)" --population "$population" --out "$runs" --classes "$list" --culture lt-LT "${mode[@]}"
}

# The reduction (2 above), into OUT/reduction.
reduction() {
  local red="$out/reduction"
  if [ "$only" = classify ] && [ -f "$red/reductions.tsv" ]; then
    step reduction-check python3 -I -B scripts/reference-arms/reduce.py check --comparison "$out/comparison" --reduced "$red"
  else
    rm -rf "$red"
    step reduction-prefixes python3 -I -B scripts/reference-arms/reduce.py prefixes --comparison "$out/comparison" \
      --broiler-js "$js" --commit "$commit" --unicode-pins "$unicode/data/unicode/pins" --out "$red"
    local list
    list=$(python3 -I -B -c 'import json, sys; print(",".join(json.load(open(sys.argv[1]))["classes"]))' "$red/population/manifest.json")
    if [ -n "$list" ]; then
      if [ "$only" = classify ]; then
        for arm in S V J; do
          step "build-$arm" dotnet build "tests/ReferenceArms/$arm" -c Release --disable-build-servers
        done
      fi
      run_arms reduction- "$red/population" "$red/runs" "$list"
    fi
    step reduction-compare python3 -I -B scripts/reference-arms/compare.py --population "$red/population" --runs "$red/runs" \
      --out "$red/comparison"
  fi
  classify_into reduction-classify "$red/comparison" --comparison "$red/comparison" --runs "$red/runs" \
    --exclude scripts/reference-arms/p4-excluded.tsv
  step reduction-read python3 -I -B scripts/reference-arms/reduce.py read --comparison "$out/comparison" --reduced "$red"
}

# 1 to 3 above.
classification() {
  classify_into classify "$out/comparison" --comparison "$out/comparison" --runs "$out/runs"
  reduction
  classify_into classify-revised "$out/classification" --comparison "$out/comparison" --runs "$out/runs" "${revised[@]}"
  [ "$classified_status" -eq 0 ] || echo "class (c) differences: see $out/classification/classification.json" >&2
}

# The two negative controls, each over its class.
controls() {
  local arm klass id dir
  for spec in "V P4 $control_v" "J P1 $control_j"; do
    read -r arm klass id <<< "$spec"
    dir="$out/controls/$arm"
    if [ ! -f "$out/population/$klass.tsv.gz" ] || [ ! -f "$out/runs/$klass.$arm.tsv.gz" ]; then
      echo "control $arm not taken: the run holds no $klass of arm $arm" | tee "$out/logs/control-$arm.log" >&2
      continue
    fi
    rm -rf "$dir"
    mkdir -p "$dir"
    step "control-$arm-copy" python3 -I -B scripts/reference-arms/controls.py copy --root "$root" --arm "$arm" --case "$id" \
      --out "$dir/copy"
    step "control-$arm-build" dotnet build "$dir/copy/tests/ReferenceArms/$arm" -c Release --disable-build-servers
    step "control-$arm-run" dotnet "$dir/copy/$(bin "$arm")" --population "$out/population" --out "$dir/runs" \
      --classes "$klass" "${mode[@]}"
    rm -rf "$dir/copy"
    step "control-$arm-assemble" python3 -I -B scripts/reference-arms/controls.py assemble --runs "$out/runs" \
      --control "$dir/runs" --arm "$arm" --class "$klass" --out "$dir"
    step "control-$arm-compare" python3 -I -B scripts/reference-arms/compare.py --population "$out/population" \
      --runs "$dir/control-runs" --out "$dir/control"
    step "control-$arm-compare-revert" python3 -I -B scripts/reference-arms/compare.py --population "$out/population" \
      --runs "$dir/revert-runs" --out "$dir/revert"
    classify_into "control-$arm-classify" "$dir/control" --comparison "$dir/control" --runs "$dir/control-runs" "${revised[@]}"
    classify_into "control-$arm-classify-revert" "$dir/revert" --comparison "$dir/revert" --runs "$dir/revert-runs" "${revised[@]}"
    classify_into "control-$arm-classify-blind" "$dir/blind" --comparison "$dir/control" --runs "$dir/revert-runs" "${revised[@]}"
    step "control-$arm-check" python3 -I -B scripts/reference-arms/controls.py check --dir "$dir" --arm "$arm" \
      --class "$klass" --case "$id"
  done
}

unlisted_stops() {
  if grep -l $'\tstop:unlisted' "$out"/runs/timing/*.tsv "$out"/reduction/runs/timing/*.tsv \
      "$out"/controls/*/runs/timing/*.tsv > "$out/logs/unlisted-stops.log" 2>/dev/null; then
    echo "a case the stop list does not hold reached a backstop (see $out/logs/unlisted-stops.log): this run is not reproducible" >&2
    exit 1
  fi
}

if [ "$only" = classify ]; then
  classification
  echo "done: $out"
  exit 0
fi
if [ "$only" = controls ]; then
  [ -s "$out/classification/classification.json" ] || { echo "no classification in $out: --controls-only reads a run's" >&2; exit 1; }
  controls
  unlisted_stops
  echo "done: $out"
  exit 0
fi

{
  echo "commit $(git rev-parse HEAD)"
  echo "taken $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "kernel $(uname -srm)"
  echo "sdk $(dotnet --version)"
  echo "python $(python3 -I -B -c 'import sys; print(sys.version.split()[0])')"
} > "$out/environment.txt"

for arm in S V J FastPaths; do
  step "build-$arm" dotnet build "tests/ReferenceArms/$arm" -c Release --disable-build-servers
done
step script-tests env BROILER_UNICODE="$unicode" BROILER_VM_CORRESPONDENCE="$correspondence" \
  python3 -m unittest discover -s scripts/reference-arms/tests
population_args=(--out "$out/population" --test262 "$test262" --unicode "$unicode" --classes "$classes")
[ -n "$vectors" ] && population_args+=(--vectors "$vectors")
step population python3 -I -B scripts/reference-arms/generate_population.py "${population_args[@]}"

if [ "$only" != calibrate ]; then
  [ -f "$stops" ] || { echo "no stop list $stops: run.sh OUT --calibrate writes one" >&2; exit 1; }
fi
run_arms "" "$out/population" "$out/runs" "$classes"
if [ "$only" = calibrate ]; then
  step stops python3 -I -B scripts/reference-arms/stops.py --runs "$out/runs" --population "$out/population" --out "$out/stops.tsv"
  echo "calibrated: $out/stops.tsv"
  exit 0
fi
step compare python3 -I -B scripts/reference-arms/compare.py --population "$out/population" --runs "$out/runs" --out "$out/comparison"

classification

if [ -f "$out/population/P4.tsv.gz" ]; then
  echo "\$ dotnet $(bin FastPaths) $out/population/P4.tsv.gz $out/fast-paths" > "$out/logs/fast-paths.log"
  status=0
  dotnet "$(bin FastPaths)" "$out/population/P4.tsv.gz" "$out/fast-paths" >> "$out/logs/fast-paths.log" 2>&1 || status=$?
  echo "exit $status" >> "$out/logs/fast-paths.log"
  if [ "$status" -gt 1 ] || [ ! -s "$out/fast-paths/fast-path-summary.json" ]; then
    echo "the fast-path differential failed (exit $status); see $out/logs/fast-paths.log" >&2
    exit 1
  fi
fi
controls
unlisted_stops
echo "done: $out"
