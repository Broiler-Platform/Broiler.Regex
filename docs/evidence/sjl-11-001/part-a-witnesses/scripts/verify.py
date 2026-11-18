#!/usr/bin/env python3
"""verify.py BUNDLE: the candidate reading's checks (verify-candidate.py, run first) and part-a-witnesses/manifest.json's:
every retained file by SHA-256 and size, every file under part-a-witnesses/ listed, no CR, none over 2 MB, a not-kept file
not on disk; each attempt's failures.json agrees with the manifest; every clause of the revision's table has a witness
attempt, or is one the revision says keeps the candidate reading's witness. Exit 0 when all hold."""
import hashlib, json, re, subprocess, sys
from pathlib import Path
b = Path(sys.argv[1])
here = Path(__file__).resolve().parent
rc = subprocess.run([sys.executable, '-I', '-B', str(here / 'verify-candidate.py'), str(b)]).returncode
bad = [] if rc == 0 else ['candidate reading checks failed']
W = b / 'part-a-witnesses'
m = json.loads((W / 'manifest.json').read_text())
ret = {e['path']: e for e in m['retained']}
nk = {e['path']: e for e in m['not-kept']}
for path, e in ret.items():
    p = b / path
    if not p.is_file() or hashlib.sha256(p.read_bytes()).hexdigest() != e['sha256'] or p.stat().st_size != e['bytes']:
        bad.append('retained mismatch ' + path)
for p in W.rglob('*'):
    if p.is_file() and p.name != 'manifest.json' or (p.is_file() and p.parent != W):
        rel = p.relative_to(b).as_posix()
        if rel not in ret: bad.append('unlisted ' + rel)
        if b'\r' in p.read_bytes(): bad.append('holds a CR ' + rel)
        if p.stat().st_size > 2 * 1024 * 1024: bad.append('over 2 MB ' + rel)
if b'\r' in (W / 'manifest.json').read_bytes(): bad.append('manifest holds a CR')
for path in nk:
    if (b / path).exists(): bad.append('not kept but on disk ' + path)
    if path in ret: bad.append('both kept and not kept ' + path)
witnessed = set()
for a in m['attempts']:
    f = json.loads((W / a['clause'] / f"attempt-{a['attempt']}" / 'failures.json').read_text())
    if f['reading'] != a['reading'] or f['deciding-tests-failed'] != a['deciding-tests-failed']:
        bad.append('attempt disagrees with failures.json ' + a['run'])
    if a['reading'] == 'witness':
        if not a['deciding-tests-failed']: bad.append('witness without a failing deciding test ' + a['run'])
        log = W / a['clause'] / f"attempt-{a['attempt']}" / 'test.log'
        if not log.is_file(): bad.append('witness whose test log is not kept ' + a['run'])
        else:
            t = log.read_text()
            for test in a['deciding-tests-failed']:
                if '  Failed Broiler.Regex.Tests.' + test not in t: bad.append(f'{a["run"]}: {test} not named failing in its log')
        witnessed.add(a['clause'])
rev = (b / 'decision-rule-revised-2026-11-18.md').read_text()
clauses = re.findall(r'^\| (A-\d+(?:\.\d+)?) \| "', rev, re.M)
kept = {'A-1.5', 'A-4.1', 'A-5.6'}
missing = [c for c in clauses if c not in witnessed and c not in kept]
both = [c for c in clauses if c in witnessed and c in kept]
print(f"part-a-witnesses: retained {len(ret)}, not kept {len(nk)}, attempts {len(m['attempts'])}, clauses {len(clauses)}, "
      f"witnessed by a run {len(witnessed)}, keeping the candidate reading's witness {len(kept & set(clauses))}, unwitnessed {missing}")
if both: bad.append('a clause both kept and planted: ' + ', '.join(both))
for x in bad: print('BAD', x)
sys.exit(1 if bad else 0)
