#!/usr/bin/env bash
# plant.sh ID SPEC CLASSES: one witness run. A throwaway copy of 49d7be1f's tree (git archive, on disk) under
# /home/user/rx-wit-plants/ID, the plant SPEC applied to its library sources, then, behind the s2 lock and hard idle gate,
# Broiler.Regex.Tests built in Release and run filtered to CLASSES (comma-separated class names). Output under runs/ID.
set -u
E2=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/s2/ev2
X=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/rx-wit
id=$1; spec=$2; classes=$3
O=$X/runs/$id; C=/home/user/rx-wit-plants/$id; T=$C/tree
[ -e $O ] && { echo "REFUSED: $O exists"; exit 3; }
[ -e $C ] && { echo "REFUSED: $C exists"; exit 3; }
avail=$(df -BG --output=avail / | tail -1 | tr -dc 0-9)
[ "$avail" -ge 8 ] || { echo "REFUSED: df ${avail}G below 8G"; exit 4; }
mkdir -p $O $T $C/pristine
git -C /home/user/rx-wit archive 49d7be1f | tar -x -C $T
git -C /home/user/rx-wit archive 49d7be1f Broiler.Regex | tar -x -C $C/pristine
cp $spec $O/plant.json
python3 -I -B $X/scripts/apply_plant.py $spec $T || { echo "plant refused" > $O/plant.refused; exit 5; }
(cd $C && diff -ruN pristine/Broiler.Regex tree/Broiler.Regex | sed -e 's#^--- pristine/#--- a/#' -e 's#^+++ tree/#+++ b/#' -e 's#^diff -ruN pristine/\(\S*\) tree/#diff -ruN a/\1 b/#' > $O/plant.diff)
(cd $T && find Broiler.Regex -type f | LC_ALL=C sort | xargs sha256sum) > $O/library-sources.sha256
filter=$(echo "$classes" | tr ',' '\n' | sed 's/^/FullyQualifiedName~Broiler.Regex.Tests./; s/$/./' | paste -sd'|')
echo "$filter" > $O/filter.txt
cat > $C/run-inner.sh <<INNER
#!/usr/bin/env bash
cd $T
dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers > $O/build.log 2>&1
b=\$?; echo \$b > $O/build.exit
[ \$b -eq 0 ] || exit 90
dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "$filter" --logger "trx;LogFileName=test.trx" --results-directory $O/results > $O/test.log 2>&1
t=\$?; echo \$t > $O/test.exit
exit \$t
INNER
chmod +x $C/run-inner.sh
$E2/scripts/run.sh $id $O/gate $T -- bash $C/run-inner.sh
echo "run rc=$?" > $O/status.txt
find $T -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
rm -rf $C
echo "df $(df -h / | awk 'NR==2{print $4}')" >> $O/status.txt
echo DONE > $O/DONE
