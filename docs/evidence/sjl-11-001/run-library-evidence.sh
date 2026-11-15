#!/usr/bin/env bash
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
#
# Bundle sjl-11-001's run-library-evidence.sh (README.md's table; WP4, task 4.1 of SJL-11.2): parts A to E of
# decision-rule.md at one commit of this repository, in a detached worktree, stopping at the first step that does not
# end as the rule expects. Each step writes one log whose first line is its command; the reports the rule reads are
# kept beside the logs and listed by SHA-256 in digests.sha256. It repeats locally what CI's jobs run at a reading's
# commit (.github/workflows/ci.yml); the rule reads CI's runs, named by their identifiers, and this script's logs are
# the reading's local repetition, not a substitute for them.
#
# What reproduces (added 2026-11-15, after the review of the tail): the reports under reports/, and so digests.sha256,
# are the same bytes on every run at one commit on one RID. The packages of check E-4 are not: dotnet pack writes the
# files' and the pack's times into the zip's entries, so two runs at one commit give packages of other bytes (the
# entries' names and lengths the same); their digests are in packages.sha256, apart, and say only which files this
# run's pack wrote. The packages are this run's evidence for E-4 and nothing else: they carry the version the commit's
# props give and are never pushed to a feed.
#
#   docs/evidence/sjl-11-001/run-library-evidence.sh COMMIT OUT --test262 DIR [--broiler-code DIR] [--rid RID]
#
# COMMIT          the commit to read (the candidate reading's).
# OUT             an empty or absent directory: logs/, reports/, packages/, digests.sha256, packages.sha256, steps.tsv.
# --test262       a copy of test262 whose test/ is the pin's tree (checked here by its git tree hash), for check B-2.
# --broiler-code  only for a commit before Broiler.Regex#19 (df5cfc2), whose tree has no hairs.config.json: a
#                 Broiler.Code checkout holding src/Broiler.Code.Review.Cli, beside which check E-6's assurance command
#                 runs as that commit's CI job laid them out; without it, E-6 is recorded as not run there, with why. At
#                 a commit with hairs.config.json, E-6 runs as CI's job does since #19, and this option is not read.
# --rid           the runtime identifier of the Native AOT publish (default linux-x64).
#
# The steps, in order (the part or check of the rule each serves in brackets):
#   part C's controls, from the clean tree, as CI's adversarial-controls job [C]: control 1 and control 2, each
#     reported as the rule asks; neither built the library; the revert, every row at its declared status
#   the tree cleaned of every build output, then the solution's Release build, no warning [E-1]
#   the tests, Broiler.Regex.Tests (part A's checks and part D's runner), the architecture tests and the fuzz
#     target's tests [A, D, E-3, E-7]
#   the probe's default report, its publish with Native AOT with no warning and no managed assembly beside the image,
#     and the reports of its four modes under Native AOT and the JIT, compared byte for byte: the default population
#     [E-2], --adversarial [C], --vectors [D] and --test262 [B-2]
#   check B-1, the host job: not run here; it waits for Broiler.JS's SJL-11.5 branch (decision D35)
#   the pack, package validation against the baseline the project names [E-4]
#   the declared fuzz session, and the planted-defect control [E-5]
#   the assurance check, as the commit's CI job runs it: with hairs.config.json, from the cleaned tree, dotnet tool
#     restore then dotnet hairs assurance check --root . (Hairs.Cli as .config/dotnet-tools.json pins it) [E-6]
# Exit: 0 when every step ends as the rule expects; 1 at the first that does not (its log names why); 2 on arguments
# it does not read.
set -euo pipefail
export PYTHONDONTWRITEBYTECODE=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

usage() {
  echo "usage: run-library-evidence.sh COMMIT OUT --test262 DIR [--broiler-code DIR] [--rid RID]" >&2
  exit 2
}
[ $# -ge 2 ] || usage
commit_arg=$1
out=$(realpath -m "$2")
shift 2
test262="" code="" rid=linux-x64
while [ $# -gt 0 ]; do
  [ $# -ge 2 ] || usage
  case "$1" in
    --test262) test262=$(realpath "$2") ;;
    --broiler-code) code=$(realpath "$2") ;;
    --rid) rid=$2 ;;
    *) usage ;;
  esac
  shift 2
done
[ -n "$test262" ] || usage

TEST262_TEST_TREE=c022917030da8fbc09b7db8f9b7d55ccc5f5ebab
repo=$(git -C "$(dirname "$0")" rev-parse --show-toplevel)
commit=$(git -C "$repo" rev-parse --verify "$commit_arg^{commit}")
if [ -e "$out" ] && [ -n "$(ls -A "$out")" ]; then
  echo "OUT is not empty: $out" >&2
  exit 2
fi
mkdir -p "$out/logs" "$out/reports" "$out/packages"
tree="$out/tree"
reports="$out/reports"
probe_dll=probes/Regex.Probe/bin/Release/net10.0/Broiler.Regex.Probe.dll
image=probes/Regex.Probe/bin/Release/net10.0/$rid/publish/Broiler.Regex.Probe
case "$rid" in win-*) image=$image.exe ;; esac
printf 'step\texit\texpected\n' > "$out/steps.tsv"

cleanup() {
  if [ -d "$tree" ]; then
    git -C "$repo" worktree remove --force "$tree" 2>/dev/null || true
  fi
}
trap cleanup EXIT

# expect NAME CODE COMMAND...: runs COMMAND in the tree, its command the first line of logs/NAME.log, and stops the
# script unless it exits CODE.
expect() {
  local name=$1 want=$2 code=0
  shift 2
  echo "\$ $*" > "$out/logs/$name.log"
  (cd "$tree" && "$@") >> "$out/logs/$name.log" 2>&1 || code=$?
  echo "exit $code" >> "$out/logs/$name.log"
  printf '%s\t%s\t%s\n' "$name" "$code" "$want" >> "$out/steps.tsv"
  if [ "$code" -ne "$want" ]; then
    echo "step $name exited $code where the rule expects $want; see $out/logs/$name.log" >&2
    exit 1
  fi
}

# check NAME WHY TEST...: a check over files a step left, logged as a step is.
check() {
  local name=$1 why=$2 code=0
  shift 2
  { echo "\$ $*"; echo "# $why"; } > "$out/logs/$name.log"
  (cd "$tree" && "$@") >> "$out/logs/$name.log" 2>&1 || code=$?
  echo "exit $code" >> "$out/logs/$name.log"
  printf '%s\t%s\t%s\n' "$name" "$code" 0 >> "$out/steps.tsv"
  if [ "$code" -ne 0 ]; then
    echo "check $name failed: $why; see $out/logs/$name.log" >&2
    exit 1
  fi
}

no_warning() { ! grep -E 'warning [A-Z]+[0-9]+' "$out/logs/$1.log"; }
no_managed_assembly() { ! ls "$(dirname "$image")"/*.dll 2>/dev/null; }
no_library_build() { [ ! -e Broiler.Regex/bin ]; }
fuzz_found_plant() { grep -q "FINDING at iteration .* planted defect (D32 control)" "$out/logs/fuzz-control.log"; }
fuzz_no_finding() { grep -q "no finding in" "$out/logs/fuzz-session.log"; }
same() { cmp "$reports/$1" "$reports/$2"; }

tree_hash() {
  local scratch
  scratch=$(mktemp -d)
  GIT_DIR="$scratch/git" git init -q --bare "$scratch/git"
  GIT_DIR="$scratch/git" GIT_INDEX_FILE="$scratch/index" git --work-tree "$1" add -A -f . >/dev/null
  GIT_DIR="$scratch/git" GIT_INDEX_FILE="$scratch/index" git write-tree
  rm -rf "$scratch"
}
test262_pin() {
  local found
  found=$(tree_hash "$test262/test")
  echo "test262 test/ tree $found, the pin's $TEST262_TEST_TREE"
  [ "$found" = "$TEST262_TEST_TREE" ]
}

{
  echo "commit $commit"
  echo "rid $rid"
  echo "test262 $test262"
  echo "broiler-code ${code:-(none)}$( [ -n "$code" ] && echo " $(git -C "$code" rev-parse HEAD 2>/dev/null)")"
  echo "sdk $(dotnet --version)"
  echo "kernel $(uname -srm)"
  echo "taken $(date -u +%Y-%m-%dT%H:%M:%SZ)"
} > "$out/environment.txt"

echo "\$ git -C $repo worktree add --detach $tree $commit" > "$out/logs/worktree.log"
git -C "$repo" worktree add --detach "$tree" "$commit" >> "$out/logs/worktree.log" 2>&1
check test262-pin "test262's test/ is the pin's tree" test262_pin

# Part C's controls, as CI's adversarial-controls job runs them, before anything builds the library.
expect control-steps-build 0 dotnet build probes/Regex.Probe -c Release --nologo --disable-build-servers -p:RegexAdversarialControl=steps
set +e
(cd "$tree" && dotnet "$probe_dll" --adversarial tests/Adversarial/corpus.tsv --report "$reports/control-steps.txt" > /dev/null 2>&1)
steps_code=$?
set -e
expect control-steps 0 python3 -I -B eng/adversarial-controls.py --control steps --corpus tests/Adversarial/corpus.tsv --report "$reports/control-steps.txt" --exit "$steps_code"
expect control-steps-holds 0 python3 -I -B eng/adversarial-controls.py --holds "adversarial control 1 (part C)" "$probe_dll"
expect control-nesting-build 0 dotnet build probes/Regex.Probe -c Release --nologo --disable-build-servers -p:RegexAdversarialControl=nesting
set +e
(cd "$tree" && dotnet "$probe_dll" --adversarial tests/Adversarial/corpus.tsv --report "$reports/control-nesting.txt" > /dev/null 2>&1)
nesting_code=$?
set -e
expect control-nesting 0 python3 -I -B eng/adversarial-controls.py --control nesting --corpus tests/Adversarial/corpus.tsv --report "$reports/control-nesting.txt" --exit "$nesting_code"
expect control-nesting-holds 0 python3 -I -B eng/adversarial-controls.py --holds "adversarial control 2 (part C)" "$probe_dll"
check controls-no-library "neither control built the library project" no_library_build
expect revert-build 0 dotnet build probes/Regex.Probe -c Release --nologo --disable-build-servers
set +e
(cd "$tree" && dotnet "$probe_dll" --adversarial tests/Adversarial/corpus.tsv --report "$reports/revert.txt" > /dev/null 2>&1)
revert_code=$?
set -e
expect revert 0 python3 -I -B eng/adversarial-controls.py --control none --corpus tests/Adversarial/corpus.tsv --report "$reports/revert.txt" --exit "$revert_code"
expect revert-lacks 0 python3 -I -B eng/adversarial-controls.py --lacks "adversarial control" --lacks RegexAdversarialControl Broiler.Regex/bin/Release/net10.0/Broiler.Regex.dll
expect clean 0 git clean -xfdq

# E-1, then A, D's tests, E-3 and E-7.
expect build 0 dotnet build Broiler.Regex.slnx -c Release --nologo --disable-build-servers
check build-no-warning "the Release build reports no warning (E-1)" no_warning build
expect tests 0 dotnet test Broiler.Regex.slnx -c Release --no-build --nologo --disable-build-servers

# E-2 and the four modes under Native AOT and the JIT.
expect probe-jit 0 dotnet "$probe_dll" --report "$reports/probe-jit.txt"
expect publish 0 dotnet publish probes/Regex.Probe -c Release -r "$rid" --nologo --disable-build-servers
check publish-no-warning "the Native AOT publish reports no warning (E-2)" no_warning publish
check publish-no-assembly "no managed assembly beside the native image (E-2)" no_managed_assembly
expect probe-aot 0 "$image" --report "$reports/probe-aot.txt"
check probe-same "the default report under Native AOT is the JIT's (E-2)" same probe-aot.txt probe-jit.txt
expect adversarial-aot 0 "$image" --adversarial tests/Adversarial/corpus.tsv --report "$reports/adversarial-aot.txt"
expect adversarial-jit 0 dotnet "$probe_dll" --adversarial tests/Adversarial/corpus.tsv --report "$reports/adversarial-jit.txt"
check adversarial-same "part C's two reports are one file" same adversarial-aot.txt adversarial-jit.txt
expect vectors-aot 0 "$image" --vectors tests/Vectors --report "$reports/vectors-aot.txt"
expect vectors-jit 0 dotnet "$probe_dll" --vectors tests/Vectors --report "$reports/vectors-jit.txt"
check vectors-same "part D's two reports are one file" same vectors-aot.txt vectors-jit.txt
expect test262-library-aot 0 "$image" --test262 "$test262" --unreadable test262/library-unreadable.txt --report "$reports/test262-library-aot.txt"
expect test262-library-jit 0 dotnet "$probe_dll" --test262 "$test262" --unreadable test262/library-unreadable.txt --report "$reports/test262-library-jit.txt"
check test262-library-same "check B-2's two reports are one file" same test262-library-aot.txt test262-library-jit.txt
{
  echo "# not run: check B-1, the host job test262-host"
  echo "# It builds Broiler.JS's shell, at the commit docs/roadmap.md names by a dated record, against this commit's"
  echo "# package; no commit is named, because Broiler.JS's SJL-11.5 branch, which decision D35 makes the retained"
  echo "# commit, does not exist yet. The rule reads B-1 from CI's run at the reading's commit."
} > "$out/logs/test262-host.log"
printf 'test262-host\tnot run\t-\n' >> "$out/steps.tsv"

# E-4, E-5, E-6.
expect pack 0 dotnet pack Broiler.Regex/Broiler.Regex.csproj -c Release --nologo --disable-build-servers -o "$out/packages"
expect fuzz-session 0 dotnet tests/Regex.Fuzz/bin/Release/net10.0/Regex.Fuzz.dll
check fuzz-no-finding "the declared session reports no finding (E-5)" fuzz_no_finding
expect fuzz-plant-build 0 dotnet build tests/Regex.Fuzz -c Release --nologo --disable-build-servers -p:RegexFuzzPlant=true
expect fuzz-control 1 dotnet tests/Regex.Fuzz/bin/Release/net10.0/Regex.Fuzz.dll --stop-when-found "planted defect (D32 control)"
check fuzz-control-found "the declared seed reports the planted defect (E-5)" fuzz_found_plant
if [ -f "$tree/hairs.config.json" ]; then
  # CI's assurance job since Broiler.Regex#19: a fresh checkout, the local tool restored, the check at the root.
  expect assurance-clean 0 git clean -xfdq
  expect assurance-restore 0 dotnet tool restore
  expect assurance 0 dotnet hairs assurance check --root .
elif [ -n "$code" ]; then
  echo "\$ dotnet run --project $code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root $tree" > "$out/logs/assurance.log"
  status=0
  (cd "$(dirname "$code")" && dotnet run --project "$code/src/Broiler.Code.Review.Cli" -c Release --disable-build-servers -- assurance check --root "$tree") >> "$out/logs/assurance.log" 2>&1 || status=$?
  echo "exit $status" >> "$out/logs/assurance.log"
  printf 'assurance\t%s\t0\n' "$status" >> "$out/steps.tsv"
  [ "$status" -eq 0 ] || { echo "step assurance exited $status; see $out/logs/assurance.log" >&2; exit 1; }
else
  echo "# not run: check E-6, the assurance check: the commit has no hairs.config.json and no --broiler-code was given; the rule reads CI's assurance job" > "$out/logs/assurance.log"
  printf 'assurance\tnot run\t0\n' >> "$out/steps.tsv"
fi

(cd "$reports" && LC_ALL=C ls | xargs sha256sum) > "$out/digests.sha256"
(cd "$out/packages" && LC_ALL=C ls | xargs sha256sum) > "$out/packages.sha256"
echo "done: $out"
