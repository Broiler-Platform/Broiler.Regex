#!/usr/bin/env python3
"""apply_plant.py SPEC TREE: applies a plant spec (JSON list of {file, anchor, replacement}) to TREE.
Each anchor must occur exactly once in its file, which must be under Broiler.Regex/. Prints nothing on success."""
import json, sys
from pathlib import Path
spec = json.loads(Path(sys.argv[1]).read_text())
tree = Path(sys.argv[2])
for e in spec['edits']:
    f = e['file']
    if not f.startswith('Broiler.Regex/'):
        sys.exit(f'refused: {f} is not a library source')
    p = tree / f
    t = p.read_text(encoding='utf-8') if p.exists() else None
    if e.get('new'):
        if t is not None: sys.exit(f'refused: {f} exists')
        p.write_text(e['replacement'], encoding='utf-8'); continue
    n = t.count(e['anchor'])
    if n != 1:
        sys.exit(f'refused: anchor occurs {n} times in {f}: {e["anchor"][:80]!r}')
    p.write_text(t.replace(e['anchor'], e['replacement']), encoding='utf-8')
