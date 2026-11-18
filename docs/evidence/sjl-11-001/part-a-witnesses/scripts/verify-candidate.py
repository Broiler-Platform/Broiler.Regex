#!/usr/bin/env python3
"""verify4.py BUNDLE: checks the candidate reading's retained files against manifest.json and every digest file.

- every retained entry's file exists with its SHA-256 and size, and every file under candidate-reading/ is listed;
- no file under candidate-reading/ is over 2 MB; no path is both retained and not kept;
- a not-kept entry with "same-as" names a retained file of the same SHA-256;
- library-evidence/digests.sha256 verifies against library-evidence/reports/, packages.sha256 against the not-kept
  packages;
- reference-arms/digests.sha256: each line's file is retained with that digest, or is a not-kept entry with it;
- part-b-control/run1-SHA256SUMS: each kept log verifies, each other is a not-kept entry with its digest.
Exit 0 when all hold."""
import hashlib
import json
import sys
from pathlib import Path

b = Path(sys.argv[1])
m = json.loads((b / 'manifest.json').read_text())
bad = []


def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


ret = {e['path']: e for e in m['retained']}
nk = {e['path']: e for e in m['not-kept']}
for path, e in ret.items():
    p = b / path
    if not p.is_file() or sha(p) != e['sha256'] or p.stat().st_size != e['bytes']:
        bad.append('retained mismatch ' + path)
for p in (b / 'candidate-reading').rglob('*'):
    if p.is_file():
        rel = p.relative_to(b).as_posix()
        if rel not in ret:
            bad.append('unlisted ' + rel)
        if b'\r' in p.read_bytes():
            bad.append('holds a CR ' + rel)
        if p.stat().st_size > 2 * 1024 * 1024:
            bad.append('over 2 MB ' + rel)
if set(ret) & set(nk):
    bad.append('both kept and not kept')
for path, e in nk.items():
    if 'same-as' in e:
        t = ret.get(e['same-as'])
        if not t or e['sha256'] not in (t['sha256'], t.get('as-fetched', {}).get('sha256')):
            bad.append('same-as mismatch ' + path)

lib = b / 'candidate-reading/library-evidence'
for line in (lib / 'digests.sha256').read_text().splitlines():
    d, name = line.split('  ', 1)
    if sha(lib / 'reports' / name) != d:
        bad.append('library digest ' + name)
for line in (lib / 'packages.sha256').read_text().splitlines():
    d, name = line.split('  ', 1)
    e = nk.get('candidate-reading/library-evidence/packages/' + name)
    if not e or e['sha256'] != d:
        bad.append('package digest ' + name)

n = 0
for line in (b / 'candidate-reading/reference-arms/digests.sha256').read_text().splitlines():
    d, rel = line.split('  ', 1)
    key = 'candidate-reading/reference-arms/run/' + rel
    e = ret.get(key) or nk.get(key)
    if not e or e['sha256'] != d:
        bad.append('arms digest ' + rel)
    n += 1

for line in (b / 'candidate-reading/part-b-control/run1-SHA256SUMS').read_text().splitlines():
    d, rel = line.split('  ', 1)
    key = 'candidate-reading/part-b-control/jobs/' + Path(rel).name
    e = ret.get(key) or nk.get(key)
    if not e or d not in (e['sha256'], e.get('as-fetched', {}).get('sha256')):
        bad.append('control digest ' + rel)

print(f"retained {len(ret)} ({sum(e['bytes'] for e in ret.values())} bytes), not kept {len(nk)}, arms digests {n}")
for x in bad:
    print('BAD', x)
sys.exit(1 if bad else 0)
