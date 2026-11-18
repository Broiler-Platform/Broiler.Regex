#!/usr/bin/env bash
# drive.sh ATTEMPT CLAUSE...: runs plant.sh for each clause's spec plants/CLAUSE.json as run CLAUSE-ATTEMPT, in order.
X=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/rx-wit
att=$1; shift
for c in "$@"; do
  classes=$(python3 -I -c "import json,sys;print(json.load(open(sys.argv[1]))['classes'])" $X/plants/$c.json)
  $X/scripts/plant.sh $c-$att $X/plants/$c.json "$classes" >> $X/runs/drive.log 2>&1
  echo "$(date -u +%FT%TZ) $c-$att done: $(cat $X/runs/$c-$att/status.txt 2>/dev/null | head -1) build=$(cat $X/runs/$c-$att/build.exit 2>/dev/null) test=$(cat $X/runs/$c-$att/test.exit 2>/dev/null)" >> $X/runs/drive.log
done
echo "$(date -u +%FT%TZ) DRIVE-DONE $att $*" >> $X/runs/drive.log
