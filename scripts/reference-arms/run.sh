#!/usr/bin/env bash
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
#
# Bundle sjl-11-001's reference arms end to end: build the arms, write the population, run every arm (J under the
# invariant culture, tr-TR and lt-LT), compare, classify. README.md beside this file says what each step is.
#
#   scripts/reference-arms/run.sh OUT --test262 DIR --unicode DIR --broiler-js DIR --commit SHA \
#       --correspondence FILE [--vectors DIR] [--classes P1,...]
#
# OUT receives the population, the arms' answers, the comparison and the classification; nothing is written into the
# repository but the arms' build output (tests/ReferenceArms/*/bin, obj). Each step's command is the first line of its
# log in OUT/logs. The script stops at the first step that fails; the classifier's exit 1 (a class (c) difference)
# is not a failure of the script, and is kept in its log.
set -euo pipefail
export PYTHONDONTWRITEBYTECODE=1  # rule SL10 reads every file of the tree, and a .pyc holds a CR

out=$(realpath -m "${1:?usage: run.sh OUT --test262 DIR --unicode DIR --broiler-js DIR --commit SHA --correspondence FILE [--vectors DIR] [--classes LIST]}")
shift
test262="" unicode="" js="" commit="" correspondence="" vectors="" classes="P1,P2,P3,P4,P5,P6,P7"
while [ $# -gt 0 ]; do
  case "$1" in
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

{
  echo "commit $(git rev-parse HEAD)"
  echo "taken $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "kernel $(uname -srm)"
  echo "sdk $(dotnet --version)"
  echo "python $(python3 -I -c 'import sys; print(sys.version.split()[0])')"
} > "$out/environment.txt"

for arm in S V J FastPaths; do
  step "build-$arm" dotnet build "tests/ReferenceArms/$arm" -c Release --disable-build-servers
done
step script-tests python3 -m unittest discover -s scripts/reference-arms/tests
population_args=(--out "$out/population" --test262 "$test262" --unicode "$unicode" --classes "$classes")
[ -n "$vectors" ] && population_args+=(--vectors "$vectors")
step population python3 -I scripts/reference-arms/generate_population.py "${population_args[@]}"

bin() { echo "tests/ReferenceArms/$1/bin/Release/net10.0/Regex.ReferenceArms.$1.dll"; }
step arm-S dotnet "$(bin S)" --population "$out/population" --out "$out/runs" --classes "$classes"
step arm-V dotnet "$(bin V)" --population "$out/population" --out "$out/runs" --classes "$classes"
step arm-J dotnet "$(bin J)" --population "$out/population" --out "$out/runs" --classes "$classes"
step arm-J-tr-TR dotnet "$(bin J)" --population "$out/population" --out "$out/runs" --classes "$classes" --culture tr-TR
step arm-J-lt-LT dotnet "$(bin J)" --population "$out/population" --out "$out/runs" --classes "$classes" --culture lt-LT
step compare python3 -I scripts/reference-arms/compare.py --population "$out/population" --runs "$out/runs" --out "$out/comparison"

echo "\$ python3 -I scripts/reference-arms/classify.py ..." > "$out/logs/classify.log"
status=0
python3 -I scripts/reference-arms/classify.py --comparison "$out/comparison" --runs "$out/runs" --broiler-js "$js" \
  --commit "$commit" --correspondence "$correspondence" --unicode-pins "$unicode/data/unicode/pins" \
  >> "$out/logs/classify.log" 2>&1 || status=$?
echo "exit $status" >> "$out/logs/classify.log"
[ "$status" -le 1 ] || { echo "the classifier refused (exit $status)" >&2; exit 1; }

if [ -f "$out/population/P4.tsv.gz" ]; then
  echo "\$ dotnet $(bin FastPaths) $out/population/P4.tsv.gz $out/fast-paths" > "$out/logs/fast-paths.log"
  dotnet "$(bin FastPaths)" "$out/population/P4.tsv.gz" "$out/fast-paths" >> "$out/logs/fast-paths.log" 2>&1 || echo "exit $?" >> "$out/logs/fast-paths.log"
fi
echo "done: $out"
