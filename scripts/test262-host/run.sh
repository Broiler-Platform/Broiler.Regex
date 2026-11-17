#!/usr/bin/env bash
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
#
# Bundle sjl-11-001's check B-1, the host job (task 2.10 of SJL-11.2; decisions D14 and D35), run from this
# repository's root, as CI's test262-host job runs it:
#
#   bash scripts/test262-host/run.sh --broiler-js DIR --broiler-js-commit SHA \
#       --test262 DIR --test262-commit SHA --test262-tree SHA --out DIR [--workers N]
#
# DIR for Broiler.JS and for test262 are checkouts the caller made (CI's actions/checkout; locally a git fetch of the
# same commits). In order, it:
#   1. checks both pins: Broiler.JS's HEAD, test262's HEAD and the git tree of its test/;
#   2. packs this tree's Broiler.Regex as the version Broiler.JS pins (its Directory.Packages.props) into OUT/feed;
#   3. writes Broiler.JS's NuGet.config so that the feed is listed first and is the only source of Broiler.Regex,
#      nuget.org every other package, and restores into OUT/packages, empty at the start, so that no copy of the
#      package from nuget.org already on the machine can be used;
#   4. builds Broiler.JS's shell, Broiler.JS/Broiler.JavaScript (BroilerJS.dll), in Release;
#   5. checks the shell was built against the candidate: the restored package came from OUT/feed, and the
#      Broiler.Regex.dll beside BroilerJS.dll is, byte for byte, the one in the packed .nupkg;
#   6. runs Broiler.JS's scripts/compliance/run_test262.py with --include-negative and no minifier (the original
#      variant) over the population below, at the test262 pin, under TZ=UTC;
#   7. holds its report to test262/host-expected-failures.txt with scripts/test262-host/compare.py (B-1's verdict).
# It exits with compare.py's exit (0 green, 1 red, 2 an unreadable input), or 3 when a step before the run fails.
# OUT holds the feed, the packages, run_test262.py's report (test262-host.json), its log, the candidate's record
# (candidate.txt) and the verdict (summary.txt). Written 2026-11-17, at task 2.10 of SJL-11.2.
set -uo pipefail

# B-1's population (docs/evidence/sjl-11-001/decision-rule.md, part B), and staging's directory of RegExp, which holds
# three of test262's nineteen files of regexp-duplicate-named-groups (task 2.6's record in docs/roadmap.md: the host
# job judges all nineteen).
POPULATION=(
  test/built-ins/RegExp
  test/language/literals/regexp
  test/annexB/built-ins/RegExp
  test/annexB/language/literals/regexp
  test/built-ins/RegExpStringIteratorPrototype
  test/built-ins/String/prototype/match
  test/built-ins/String/prototype/matchAll
  test/built-ins/String/prototype/replace
  test/built-ins/String/prototype/replaceAll
  test/built-ins/String/prototype/search
  test/built-ins/String/prototype/split
  test/staging/built-ins/RegExp
)
# The four files task 2.6's test leaves to this job: their assertions read the host's GetSubstitution or split.
REQUIRED=(
  test/built-ins/RegExp/named-groups/duplicate-names-replace.js
  test/built-ins/RegExp/named-groups/duplicate-names-replaceall.js
  test/built-ins/RegExp/named-groups/duplicate-names-split.js
  test/staging/built-ins/RegExp/named-groups/duplicate-named-groups-replace.js
)

js="" js_commit="" suite="" suite_commit="" suite_tree="" out="" workers=4
while [ $# -gt 0 ]; do
  case "$1" in
    --broiler-js) js=$2; shift 2 ;;
    --broiler-js-commit) js_commit=$2; shift 2 ;;
    --test262) suite=$2; shift 2 ;;
    --test262-commit) suite_commit=$2; shift 2 ;;
    --test262-tree) suite_tree=$2; shift 2 ;;
    --out) out=$2; shift 2 ;;
    --workers) workers=$2; shift 2 ;;
    *) echo "unknown argument: $1" >&2; exit 3 ;;
  esac
done
for value in "$js" "$js_commit" "$suite" "$suite_commit" "$suite_tree" "$out"; do
  [ -n "$value" ] || { echo "usage: see the header of $0" >&2; exit 3; }
done
[ -f Broiler.Regex/Broiler.Regex.csproj ] || { echo "run from the repository's root" >&2; exit 3; }
repo=$(pwd)
js=$(cd "$js" && pwd) && suite=$(cd "$suite" && pwd) || exit 3
fail() { echo "::error::test262-host: $*"; exit 3; }

echo "== 1. the pins"
test "$(git -C "$js" rev-parse HEAD)" = "$js_commit" || fail "Broiler.JS is not at $js_commit"
test "$(git -C "$suite" rev-parse HEAD)" = "$suite_commit" || fail "test262 is not at $suite_commit"
test "$(git -C "$suite" rev-parse HEAD:test)" = "$suite_tree" || fail "test262's test/ is not the tree $suite_tree"
echo "Broiler.JS $js_commit; test262 $suite_commit, test/ $suite_tree"

mkdir -p "$out" && out=$(cd "$out" && pwd) || exit 3
feed=$out/feed
packages=$out/packages
[ ! -e "$feed" ] && [ ! -e "$packages" ] || fail "$feed and $packages must not exist yet"
mkdir -p "$feed" "$packages"

echo "== 2. the candidate package"
version=$(python3 -I - "$js/Directory.Packages.props" <<'EOF'
import re, sys
text = open(sys.argv[1], encoding="utf-8").read()
found = re.findall(r'<PackageVersion\s+Include="Broiler\.Regex"\s+Version="([^"]+)"\s*/>', text)
if len(found) != 1:
    sys.exit(f"Directory.Packages.props pins Broiler.Regex {len(found)} times, not once")
print(found[0])
EOF
) || fail "cannot read the version Broiler.JS pins"
echo "Broiler.JS pins Broiler.Regex $version"
dotnet pack Broiler.Regex/Broiler.Regex.csproj -c Release --nologo --disable-build-servers \
  "-p:Version=$version" "-p:PackageVersion=$version" -o "$feed" || fail "the pack failed"
nupkg=$feed/Broiler.Regex.$version.nupkg
[ -f "$nupkg" ] || fail "the pack did not write $nupkg"
[ "$(find "$feed" -name '*.nupkg' | wc -l)" -eq 1 ] || fail "the feed holds more than the one package"

echo "== 3. Broiler.JS's sources: the feed first, and the only source of Broiler.Regex"
cat > "$js/NuGet.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<!-- Written by Broiler.Regex's scripts/test262-host/run.sh (check B-1): the candidate feed first. -->
<configuration>
  <packageSources>
    <clear />
    <add key="candidate" value="$feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="candidate">
      <package pattern="Broiler.Regex" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
EOF
export NUGET_PACKAGES=$packages

echo "== 4. Broiler.JS's shell, Release"
shell_project=Broiler.JS/Broiler.JavaScript/Broiler.JavaScript.csproj
shell=$js/Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll
(cd "$js" && dotnet build "$shell_project" -c Release --nologo --disable-build-servers) || fail "the shell's build failed"
[ -f "$shell" ] || fail "the build wrote no $shell"

echo "== 5. the shell holds the candidate"
python3 -I - "$nupkg" "$packages" "$version" "$feed" "$(dirname "$shell")/Broiler.Regex.dll" > "$out/candidate.txt" <<'EOF' || { cat "$out/candidate.txt"; fail "the shell was not built against the candidate"; }
import hashlib, json, os, sys, zipfile
nupkg, packages, version, feed, beside = sys.argv[1:]
with zipfile.ZipFile(nupkg) as archive:
    dlls = [name for name in archive.namelist() if name.startswith("lib/") and name.endswith("/Broiler.Regex.dll")]
    if len(dlls) != 1:
        sys.exit(f"the package holds {dlls}, not one Broiler.Regex.dll")
    packed = hashlib.sha256(archive.read(dlls[0])).hexdigest()
metadata = json.load(open(os.path.join(packages, "broiler.regex", version.lower(), ".nupkg.metadata"), encoding="utf-8"))
used = hashlib.sha256(open(beside, "rb").read()).hexdigest()
print(f"package: Broiler.Regex {version}, {os.path.basename(nupkg)} sha256 {hashlib.sha256(open(nupkg, 'rb').read()).hexdigest()}")
print(f"packed {dlls[0]}: sha256 {packed}")
print(f"restored from: {metadata.get('source')}")
print(f"beside the shell: sha256 {used}")
if os.path.realpath(str(metadata.get("source", "")).rstrip("/")) != os.path.realpath(feed):
    sys.exit("the restored package did not come from the candidate feed")
if used != packed:
    sys.exit("the Broiler.Regex.dll beside the shell is not the packed one")
print("the shell holds the candidate")
EOF
echo "Broiler.Regex: $(git -C "$repo" rev-parse HEAD 2>/dev/null || echo 'not a git checkout')" >> "$out/candidate.txt"
cat "$out/candidate.txt"

echo "== 6. test262, through Broiler.JS's runner"
(cd "$js" && TZ=UTC python3 -I scripts/compliance/run_test262.py \
  --suite-ref "$suite_commit" --suite-root "$suite" --broiler-dll "$shell" \
  --include-negative --minifier none --max-workers "$workers" --test-timeout-seconds 30 \
  --output "$out/test262-host.json" --no-summary-stdout \
  "${POPULATION[@]}") > "$out/run_test262.log" 2>&1
code=$?
tail -3 "$out/run_test262.log"
echo "run_test262.py exited $code"

echo "== 7. B-1's verdict"
required_args=()
for path in "${REQUIRED[@]}"; do required_args+=(--require "$path"); done
python3 -I "$repo/scripts/test262-host/compare.py" --report "$out/test262-host.json" --exit "$code" \
  --expected "$repo/test262/host-expected-failures.txt" --suite "$suite" --suite-ref "$suite_commit" \
  --population "${POPULATION[@]}" "${required_args[@]}" --summary "$out/summary.txt"
