#!/usr/bin/env bash
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
#
# Bundle sjl-11-001's reference arms end to end: build the arms, write the population, run every arm (J under the
# invariant culture, tr-TR and lt-LT), compare, classify. README.md beside this file says what each step is.
#
#   scripts/reference-arms/run.sh OUT --test262 DIR --unicode DIR --broiler-js DIR --commit SHA \
#       --correspondence FILE [--vectors DIR] [--classes P1,...] [--classify-only | --calibrate]
#
# OUT receives the population, the arms' answers, the comparison and the classification; nothing is written into the
# repository but the arms' build output (tests/ReferenceArms/*/bin, obj). Each step's command is the first line of its
# log in OUT/logs. The script stops, with exit 1, at the first step that fails. The classifier's exit 1 (a class (c)
# difference) is not a failure of the script and is kept in its log; its exit 2 (it refused) or 3 (it failed), or a
# run that leaves no OUT/comparison/classification.json, is. The fast-path differential's exit 1 (a disagreement) is
# kept the same way; any other failure of it fails the script. --classify-only runs the classification alone, over
# the comparison an earlier run left in OUT.
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

out=$(realpath -m "${1:?usage: run.sh OUT --test262 DIR --unicode DIR --broiler-js DIR --commit SHA --correspondence FILE [--vectors DIR] [--classes LIST]}")
shift
test262="" unicode="" js="" commit="" correspondence="" vectors="" classes="P1,P2,P3,P4,P5,P6,P7" only=""
while [ $# -gt 0 ]; do
  case "$1" in
    --classify-only) only=classify; shift; continue ;;
    --calibrate) only=calibrate; shift; continue ;;
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

classify() {
  # The classifier's exit: 0 no class (c), 1 a class (c) difference, 2 refused, 3 failed (classify.py). It writes
  # classification.json last, so a file left by an earlier run is removed first and its absence after 0 or 1 is a
  # failure too.
  local result="$out/comparison/classification.json"
  rm -f "$result"
  echo "\$ python3 -I -B scripts/reference-arms/classify.py ..." > "$out/logs/classify.log"
  local status=0
  python3 -I -B scripts/reference-arms/classify.py --comparison "$out/comparison" --runs "$out/runs" --broiler-js "$js" \
    --commit "$commit" --correspondence "$correspondence" --unicode-pins "$unicode/data/unicode/pins" \
    >> "$out/logs/classify.log" 2>&1 || status=$?
  echo "exit $status" >> "$out/logs/classify.log"
  case "$status" in
    0 | 1) ;;
    2) echo "the classifier refused (exit 2); see $out/logs/classify.log" >&2; exit 1 ;;
    *) echo "the classifier failed (exit $status); see $out/logs/classify.log" >&2; exit 1 ;;
  esac
  if [ ! -s "$result" ]; then
    echo "the classifier exited $status and wrote no $result; see $out/logs/classify.log" >&2
    exit 1
  fi
  [ "$status" -eq 0 ] || echo "class (c) differences: see $result" >&2
}

if [ "$only" = classify ]; then
  classify
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

bin() { echo "tests/ReferenceArms/$1/bin/Release/net10.0/Regex.ReferenceArms.$1.dll"; }
stops=tests/ReferenceArms/stops.tsv
if [ "$only" = calibrate ]; then
  mode=(--calibrate)
else
  [ -f "$stops" ] || { echo "no stop list $stops: run.sh OUT --calibrate writes one" >&2; exit 1; }
  mode=(--stops "$stops")
fi
step arm-S dotnet "$(bin S)" --population "$out/population" --out "$out/runs" --classes "$classes" "${mode[@]}"
step arm-V dotnet "$(bin V)" --population "$out/population" --out "$out/runs" --classes "$classes" "${mode[@]}"
step arm-J dotnet "$(bin J)" --population "$out/population" --out "$out/runs" --classes "$classes" "${mode[@]}"
step arm-J-tr-TR dotnet "$(bin J)" --population "$out/population" --out "$out/runs" --classes "$classes" --culture tr-TR "${mode[@]}"
step arm-J-lt-LT dotnet "$(bin J)" --population "$out/population" --out "$out/runs" --classes "$classes" --culture lt-LT "${mode[@]}"
if [ "$only" = calibrate ]; then
  step stops python3 -I -B scripts/reference-arms/stops.py --runs "$out/runs" --population "$out/population" --out "$out/stops.tsv"
  echo "calibrated: $out/stops.tsv"
  exit 0
fi
step compare python3 -I -B scripts/reference-arms/compare.py --population "$out/population" --runs "$out/runs" --out "$out/comparison"

classify

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
if grep -l $'\tstop:unlisted' "$out"/runs/timing/*.tsv > "$out/logs/unlisted-stops.log" 2>/dev/null; then
  echo "a case the stop list does not hold reached a backstop (see $out/logs/unlisted-stops.log): this run is not reproducible" >&2
  exit 1
fi
echo "done: $out"
