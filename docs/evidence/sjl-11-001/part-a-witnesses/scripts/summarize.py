#!/usr/bin/env python3
"""summarize.py RUNDIR CLAUSE: the failed tests a run's test.log names (Class.Method, counted), and which decide CLAUSE."""
import re, sys, collections, json
sys.path.insert(0, sys.argv[3] if len(sys.argv) > 3 else '.')
from deciding import decides
log = open(sys.argv[1] + '/test.log', encoding='utf-8').read()
c = collections.Counter()
for m in re.finditer(r'^  Failed Broiler\.Regex\.Tests\.([A-Za-z0-9_]+)\.([A-Za-z0-9_]+)', log, re.M):
    c[(m.group(1), m.group(2))] += 1
tot = re.findall(r'^(Passed!|Failed!)\s+- Failed:\s+(\d+), Passed:\s+(\d+), Skipped:\s+(\d+), Total:\s+(\d+)', log, re.M)
dec = sorted(k for k in c if decides(sys.argv[2], *k))
print(json.dumps({'totals': tot, 'failed': [f'{a}.{b} x{n}' for (a, b), n in sorted(c.items())],
                  'deciding_failed': [f'{a}.{b}' for a, b in dec], 'witnessed': bool(dec)}, indent=1))
