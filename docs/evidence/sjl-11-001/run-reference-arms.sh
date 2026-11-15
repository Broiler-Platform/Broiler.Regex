#!/usr/bin/env bash
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
#
# Bundle sjl-11-001's run-reference-arms.sh (README.md's table; WP4, task 4.1 of SJL-11.2): the arms, the population,
# the comparison and the classification of reference-arms.md, at one commit of this repository, from a clean tree. It
# names its inputs, checks each, and runs scripts/reference-arms/run.sh of that commit, which builds the arms, writes the
# population, runs every arm (J under the invariant culture, tr-TR and lt-LT), compares, classifies and runs the
# fast-path differential, each step logged with its command as the first line.
#
#   docs/evidence/sjl-11-001/run-reference-arms.sh COMMIT OUT \
#       --test262 DIR --unicode DIR --broiler-js DIR --broiler-vm DIR --correspondence-commit SHA \
#       [--js-commit SHA] [--unicode-commit SHA] [--classes P1,...]
#
# COMMIT   the commit of this repository to run (a reading's).
# OUT      an empty or absent directory; it receives inputs/, run/ (run.sh's OUT), logs/, digests.sha256 and
#          inputs.txt. Nothing is written into this repository's working tree.
# --test262              a copy of test262 whose test/ is the pin's tree (checked here, by its git tree hash).
# --unicode              a Broiler.Unicode git checkout (P5 and P6, by its unicoderef.py over its archived UCD pins),
#                        at --unicode-commit with no change to a tracked file (checked here).
# --broiler-js           a Broiler.JS git repository holding --js-commit: P1 is that commit's
#                        conformance/vectors/regexp/, checked against COMMIT's tests/Vectors/regexp.pin, and the arbiter
#                        regexpref.py is that commit's (the classifier reads it by git archive).
# --broiler-vm           a Broiler.VM git repository holding --correspondence-commit, whose
#                        docs/evidence/sjl-11-001/correspondence.md the classifier reads (written to OUT/inputs).
# --js-commit            default: the commit COMMIT's tests/Vectors/regexp.pin names (corpus revision 32 at b2c2e224).
# --unicode-commit       default: 0bcb917564d514feadaa27ce7dd1e0b57b585c90, the Broiler.Unicode commit of the full
#                        run at 07d7825 (2026-11-15) that gave task 2.13's comparison and classification byte for byte
#                        (docs/roadmap.md, the record of SJL-11.2's tail). Task 2.13's own record names no Broiler.Unicode
#                        commit, so P5 and P6 are shown to reproduce at this commit only (added 2026-11-15, after the
#                        review of the tail).
# --classes              default: all seven; a run with fewer is partial, and the population's manifest says so.
#
# COMMIT is checked out as a detached worktree under OUT (git worktree add --detach), so the arms are built from a clean
# tree, and removed at the end with its build output. Exit: 0 when run.sh ends with exit 0 (a class (c) difference is
# not a failure: the classifier's exit 1 is kept in OUT/run/logs/classify.log, as run.sh keeps it); 1 when an input
# check or a step fails, or run.sh fails; 2 on arguments it does not read.
set -euo pipefail
export PYTHONDONTWRITEBYTECODE=1

usage() {
  echo "usage: run-reference-arms.sh COMMIT OUT --test262 DIR --unicode DIR --broiler-js DIR --broiler-vm DIR --correspondence-commit SHA [--js-commit SHA] [--unicode-commit SHA] [--classes LIST]" >&2
  exit 2
}
[ $# -ge 2 ] || usage
commit_arg=$1
out=$(realpath -m "$2")
shift 2
test262="" unicode="" js="" vm="" corr_commit="" js_commit="" classes="" unicode_commit=0bcb917564d514feadaa27ce7dd1e0b57b585c90
while [ $# -gt 0 ]; do
  [ $# -ge 2 ] || usage
  case "$1" in
    --test262) test262=$(realpath "$2") ;;
    --unicode) unicode=$(realpath "$2") ;;
    --broiler-js) js=$(realpath "$2") ;;
    --broiler-vm) vm=$(realpath "$2") ;;
    --correspondence-commit) corr_commit=$2 ;;
    --js-commit) js_commit=$2 ;;
    --unicode-commit) unicode_commit=$2 ;;
    --classes) classes=$2 ;;
    *) usage ;;
  esac
  shift 2
done
[ -n "$test262" ] && [ -n "$unicode" ] && [ -n "$js" ] && [ -n "$vm" ] && [ -n "$corr_commit" ] || usage

# The pins this script checks: test262's commit and the git tree of its test/ (docs/evidence/sjl-11-001/decision-rule.md,
# "The pins"; scripts/reference-arms/README.md, P2).
TEST262_PIN=ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
TEST262_TEST_TREE=c022917030da8fbc09b7db8f9b7d55ccc5f5ebab

repo=$(git -C "$(dirname "$0")" rev-parse --show-toplevel)
commit=$(git -C "$repo" rev-parse --verify "$commit_arg^{commit}")
if [ -e "$out" ] && [ -n "$(ls -A "$out")" ]; then
  echo "OUT is not empty: $out" >&2
  exit 2
fi
mkdir -p "$out/logs" "$out/inputs"
tree="$out/tree"

step() {
  local log=$1
  shift
  echo "\$ $*" > "$out/logs/$log.log"
  if ! "$@" >> "$out/logs/$log.log" 2>&1; then
    echo "step $log failed; see $out/logs/$log.log" >&2
    exit 1
  fi
}

# The git tree hash of a directory as it is, in a scratch index: it names a copy's content whatever its origin.
tree_hash() {
  local scratch
  scratch=$(mktemp -d)
  GIT_DIR="$scratch/git" git init -q --bare "$scratch/git"
  GIT_DIR="$scratch/git" GIT_INDEX_FILE="$scratch/index" git --work-tree "$1" add -A -f . >/dev/null
  GIT_DIR="$scratch/git" GIT_INDEX_FILE="$scratch/index" git write-tree
  rm -rf "$scratch"
}

check_test262() {
  local found
  found=$(tree_hash "$test262/test")
  echo "test262 test/ tree $found, the pin's $TEST262_TEST_TREE (commit $TEST262_PIN)"
  [ "$found" = "$TEST262_TEST_TREE" ]
}

check_vectors() {
  # COMMIT's pin file names the Broiler.JS commit and each file's name, length and SHA-256; P1 is that commit's
  # directory, which must give every file the pin names, byte for byte, and no other.
  local pin="$tree/tests/Vectors/regexp.pin" dir="$out/inputs/vectors/conformance/vectors/regexp" listed found f
  mkdir -p "$out/inputs/vectors"
  git -C "$js" archive --format=tar "$js_commit" conformance/vectors/regexp | tar -x -C "$out/inputs/vectors"
  listed=$(sed -n 's/^file: \([^ ]*\) \([0-9]*\) \([0-9a-f]\{64\}\) .*$/\1 \2 \3/p' "$pin" | LC_ALL=C sort)
  found=$(cd "$dir" && for f in *.vectors; do echo "$f $(wc -c < "$f") $(sha256sum < "$f" | cut -d' ' -f1)"; done | LC_ALL=C sort)
  grep -E '^(broiler-js-commit|corpus-revision|source):' "$pin"
  echo "$found"
  [ -n "$listed" ] && [ "$listed" = "$found" ]
}

check_unicode() {
  # The checkout's HEAD is the commit, and no tracked file differs from it: P5 and P6 read its working tree.
  local head changed
  head=$(git -C "$unicode" rev-parse HEAD)
  changed=$(git -C "$unicode" status --porcelain --untracked-files=no)
  echo "Broiler.Unicode HEAD $head, the commit asked $unicode_commit; tracked files changed: ${changed:-none}"
  [ "$head" = "$(git -C "$unicode" rev-parse --verify "$unicode_commit^{commit}")" ] && [ -z "$changed" ]
}

check_correspondence() {
  local dir="$out/inputs/correspondence"
  mkdir -p "$dir"
  git -C "$vm" show "$corr_commit:docs/evidence/sjl-11-001/correspondence.md" > "$dir/correspondence.md"
  echo "Broiler.VM $(git -C "$vm" rev-parse "$corr_commit") correspondence.md blob $(git -C "$vm" rev-parse "$corr_commit:docs/evidence/sjl-11-001/correspondence.md") sha256 $(sha256sum < "$dir/correspondence.md" | cut -d' ' -f1)"
}

cleanup() {
  if [ -d "$tree" ]; then
    git -C "$repo" worktree remove --force "$tree" 2>/dev/null || true
  fi
}
trap cleanup EXIT

step worktree git -C "$repo" worktree add --detach "$tree" "$commit"
if [ -z "$js_commit" ]; then
  js_commit=$(sed -n 's/^broiler-js-commit: \([0-9a-f]\{40\}\)$/\1/p' "$tree/tests/Vectors/regexp.pin")
  [ -n "$js_commit" ] || { echo "the pin file names no commit" >&2; exit 1; }
fi
js_commit=$(git -C "$js" rev-parse --verify "$js_commit^{commit}")
step check-test262 check_test262
step check-vectors check_vectors
step check-unicode check_unicode
step check-correspondence check_correspondence
{
  echo "commit $commit"
  echo "test262 $TEST262_PIN test/ $TEST262_TEST_TREE ($test262)"
  echo "broiler-unicode $(git -C "$unicode" rev-parse HEAD) ($unicode)"
  echo "broiler-js $js_commit ($js)"
  echo "broiler-vm correspondence $(git -C "$vm" rev-parse "$corr_commit") ($vm)"
  echo "classes ${classes:-P1,P2,P3,P4,P5,P6,P7}"
  echo "taken $(date -u +%Y-%m-%dT%H:%M:%SZ)"
} > "$out/inputs.txt"

args=("$out/run" --test262 "$test262" --unicode "$unicode" --broiler-js "$js" --commit "$js_commit"
  --correspondence "$out/inputs/correspondence/correspondence.md" --vectors "$out/inputs/vectors/conformance/vectors/regexp")
[ -n "$classes" ] && args+=(--classes "$classes")
step run-sh bash "$tree/scripts/reference-arms/run.sh" "${args[@]}"

# The listings and every file they come from, by SHA-256; timing/ is the run's and is not listed (nothing compares it).
listed=()
for d in population runs comparison fast-paths; do
  [ -d "$out/run/$d" ] && listed+=("$d")
done
(cd "$out/run" && find "${listed[@]}" -type f ! -path 'runs/timing/*' | LC_ALL=C sort | xargs sha256sum) > "$out/digests.sha256"
echo "done: $out (digests.sha256; the classification's exit in run/logs/classify.log)"
