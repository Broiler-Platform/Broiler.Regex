#!/usr/bin/env python3
"""retain.py: copies the witness runs from SCRATCH runs/ into the bundle's part-a-witnesses/, writes each attempt's
note.md and failures.json, and part-a-witnesses/manifest.json (every file by SHA-256; over 2 MB by digest only)."""
import hashlib, json, os, re, shutil, subprocess, sys
from pathlib import Path
X = Path('/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/rx-wit')
B = Path('/home/user/rx-wit/docs/evidence/sjl-11-001')
W = B / 'part-a-witnesses'
sys.path.insert(0, str(X / 'scripts'))
from deciding import decides
LIMIT = 2 * 1024 * 1024
if W.exists(): shutil.rmtree(W)
W.mkdir()
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()

# Clause texts from the revision's table.
rev = (B / 'decision-rule-revised-2026-11-18.md').read_text()
text = {m.group(1): m.group(2) for m in re.finditer(r'^\| (A-\d+(?:\.\d+)?) \| "(.*?)" \|', rev, re.M)}

base = {l.split('  ', 1)[1].strip(): l.split('  ', 1)[0] for l in open(X / 'base/library-sources-49d7be1f.sha256')}
(W / 'base').mkdir()
shutil.copy(X / 'base/library-sources-49d7be1f.sha256', W / 'base/library-sources-49d7be1f.sha256')
(W / 'scripts').mkdir()
for s in ['plant.sh', 'apply_plant.py', 'drive.sh', 'deciding.py', 'summarize.py', 'retain.py', 'verify.py', 'verify-candidate.py']:
    shutil.copy(X / 'scripts' / s, W / 'scripts' / s)
for c in sorted(p.name for p in (X / 'plants').glob('*.json')):
    pass

SPECIAL = {
 'A-5.1-1': ('failed attempt', 'No test failed. The plant changed `RegexUnicode.ResolveProperty`, which the parser reaches only after `RegexUnicode.ClassifyProperty` has accepted the name (`RegexParser.ResolvePropertyEscape`), so nothing was refused. This attempt does not witness the clause; attempt 2 plants the same refusal in `ClassifyProperty`.'),
 'A-5.2-1': ('failed attempt', 'No test failed. The plant changed `RegexUnicode.ResolveProperty` alone, which the parser reaches only after `RegexUnicode.ClassifyProperty` has accepted the name, so no refused spelling was accepted. This attempt does not witness the clause; attempt 2 plants the same loosening in `ClassifyProperty` too.'),
 'A-4.2-1': ('not counted', 'Two deciding tests are named failing, but the run then ended with xUnit\'s `[FATAL ERROR] Xunit.Sdk.TestPipelineException` about a minute later, and the test log\'s totals count only the two tests reported: the classes\' population was not run to its end. With the check removed, the patterns far past the limit are parsed without bound. A step that stopped before the end of its population is void (`decision-rule.md`, "How it is read"), so this attempt is retained and not counted; attempt 2 is the clause\'s witness.'),
 'A-4.4-1': ('not counted', 'The deciding tests failed with `System.InsufficientExecutionStackException` (the summary below is read from the test log), but each failure\'s stack trace runs some hundreds of parser levels deep, so the test log and the trx are over 2 MB and are named in the manifest by their digests only, not kept. This attempt is not counted, since its log is not retained; attempt 2 takes a larger frame per level, fails at a shallower depth, and its test log is retained.'),
}
A44 = ('**Why this plant.** The clause\'s "in the Native AOT probe" is a run of the probe published with Native AOT, not a test of `Broiler.Regex.Tests`; the deciding tests run the same proof under the JIT on a thread of 1 MiB, the smallest stack the corpus declares. The library has no CLR recursion in its matcher (A-4.1) and its parser recurses once per level, bounded by the nesting limit, so no change of a constant reaches the guarantee; the closest faithful plant is a descent that needs more stack per level than 1 MiB holds at the maximum. The plant asks `RuntimeHelpers.EnsureSufficientExecutionStack`, which the library is forbidden to call (rule C5 of `tests/Regex.Architecture.Tests`\' register, "No library asks how much stack is left"), only so that the shortage is an exception the test names: without it the thread would overflow its stack and end the test host, and no test would be named.')
EXTRA = {
 'A-1.3': 'The plant is the line of part C\'s control 1 (`probes/Regex.Probe/AdversarialControls.targets`), written into the library\'s source of the copy.',
 'A-4.2': 'Attempt 1\'s plant is the line of part C\'s control 2 (`probes/Regex.Probe/AdversarialControls.targets`), written into the library\'s source of the copy.',
 'A-4.4': A44,
 'A-10.1': '**What it leaves.** The clause\'s "at the version above" is the pin in the repository\'s `Directory.Packages.props`, which is not the library\'s source; the plant breaks the half the deciding test reads, the library\'s assembly references.',
}

manifest = {'bundle': 'sjl-11-001', 'reading': "part A's second reading at the candidate reading's commit, under decision-rule-revised-2026-11-18.md (the owner's choice of 2026-11-18)",
            'taken': '2026-11-18',
            'commits': {'reading': '49d7be1f3ef148f9c262f0e52b8c360c07fd838d', 'reading-library-tree': 'b8001a304dbfba43ea5eee34b02563083b5e8e1d',
                        'rule-revision': subprocess.run(['git', '-C', '/home/user/rx-wit', 'log', '-1', '--format=%H', '--', 'docs/evidence/sjl-11-001/decision-rule-revised-2026-11-18.md'], capture_output=True, text=True).stdout.strip()},
            'the-revert': "the candidate reading's test logs at 49d7be1f, named in manifest.json: candidate-reading/library-evidence/logs/tests.log and candidate-reading/ci/job-logs/134473695300.log and 134473694930.log; base/library-sources-49d7be1f.sha256 lists 49d7be1f's library sources, from which each copy's differs only by its plant",
            'how': 'each attempt: git archive 49d7be1f extracted on disk under /home/user/rx-wit-plants/<run>/tree, the plant applied by scripts/apply_plant.py to files under Broiler.Regex/ only, then behind the s2 lock and idle gate dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers and dotnet test of it --no-build filtered to filter.txt with a trx logger (scripts/plant.sh); the copy deleted after its run; linux-x64, .NET SDK as gate.log\'s run records',
            'attempts': [], 'retained': [], 'not-kept': []}

runs = sorted((X / 'runs').glob('A-*'), key=lambda p: [int(x) if x.isdigit() else x for x in re.split(r'[-.]', p.name)])
for r in runs:
    run = r.name
    clause, attempt = run.rsplit('-', 1)
    d = W / clause / f'attempt-{attempt}'
    d.mkdir(parents=True)
    spec = json.loads((r / 'plant.json').read_text())
    files = {'plant.json': r / 'plant.json', 'plant.diff': r / 'plant.diff', 'library-sources.sha256': r / 'library-sources.sha256',
             'filter.txt': r / 'filter.txt', 'build.log': r / 'build.log', 'build.exit': r / 'build.exit', 'test.log': r / 'test.log',
             'test.exit': r / 'test.exit', 'test.trx': r / 'results/test.trx', 'gate.log': r / 'gate/gate.log', 'run.tsv': r / 'gate/runs.tsv',
             'time.txt': r / f'gate/{run}.time.txt'}
    for name, src in files.items():
        rel = f'part-a-witnesses/{clause}/attempt-{attempt}/{name}'
        if src.stat().st_size > LIMIT:
            manifest['not-kept'].append({'path': rel, 'sha256': sha(src), 'bytes': src.stat().st_size,
                'where': f'SCRATCH/sjl-rest/rx-wit/runs/{run}/' + str(src.relative_to(r)) + ' (scratch)', 'why': 'over 2 MB, named by its digest only (the bundle\'s convention)'})
        else:
            shutil.copy(src, d / name)
    log = (r / 'test.log').read_text(encoding='utf-8')
    import collections
    cnt = collections.Counter((m.group(1), m.group(2)) for m in re.finditer(r'^  Failed Broiler\.Regex\.Tests\.([A-Za-z0-9_]+)\.([A-Za-z0-9_]+)', log, re.M))
    tot = re.findall(r'^(?:Passed!|Failed!)\s+- Failed:\s+(\d+), Passed:\s+(\d+), Skipped:\s+(\d+), Total:\s+(\d+)', log, re.M)
    dec = sorted(f'{a}.{b}' for a, b in cnt if decides(clause, a, b))
    oth = sorted(f'{a}.{b}' for a, b in cnt if not decides(clause, a, b))
    cur = {l.split('  ', 1)[1].strip(): l.split('  ', 1)[0] for l in open(r / 'library-sources.sha256')}
    changed = sorted(k for k in set(base) | set(cur) if base.get(k) != cur.get(k))
    status, why = SPECIAL.get(run, ('witness' if dec else 'failed attempt', None))
    fail = {'run': run, 'clause': clause, 'attempt': int(attempt), 'build-exit': int((r / 'build.exit').read_text()), 'test-exit': int((r / 'test.exit').read_text()),
            'totals': {'failed': int(tot[0][0]), 'passed': int(tot[0][1]), 'skipped': int(tot[0][2]), 'total': int(tot[0][3])} if tot else None,
            'deciding-tests-failed': dec, 'other-tests-failed': oth, 'failed-cases': {f'{a}.{b}': n for (a, b), n in sorted(cnt.items())},
            'library-files-changed': changed, 'library-sources-sha256': sha(r / 'library-sources.sha256'), 'reading': status}
    (d / 'failures.json').write_text(json.dumps(fail, indent=1) + '\n')
    rt = (r / 'gate/runs.tsv').read_text().strip().split('\t')
    filt = (r / 'filter.txt').read_text().strip()
    kept_out = [e['path'].rsplit('/', 1)[1] for e in manifest['not-kept'] if e['path'].startswith(f'part-a-witnesses/{clause}/attempt-{attempt}/')]
    L = [f'# Witness run {clause}, attempt {attempt}', '',
         f'**The clause.** {clause}: "{text[clause]}" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").', '',
         f'**The plant.** {spec["what"][0].upper() + spec["what"][1:]}. Its diff is `plant.diff`, its edits `plant.json`. The copy\'s library sources are listed by SHA-256 in `library-sources.sha256` (its digest `{fail["library-sources-sha256"]}`), which differs from 49d7be1f\'s listing (`../../base/library-sources-49d7be1f.sha256`) in ' + ', '.join(f'`{c}`' for c in changed) + ' alone.', '']
    if clause in EXTRA and not (clause == 'A-4.2' and attempt != '1'):
        L += [EXTRA[clause], '']
    L += [f'**The run.** {rt[3].split("=",1)[1]} to {rt[4].split("=",1)[1]}, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):',
          '`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit ' + str(fail['build-exit']) + ' (`build.log`); then',
          '`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "' + filt + '" --logger "trx;LogFileName=test.trx"`, exit ' + str(fail['test-exit']) + ' (`test.log`, `test.trx`).', '']
    if kept_out:
        L += ['**Kept out by digest** (over 2 MB): ' + ', '.join(f'`{k}`' for k in kept_out) + '; `../../manifest.json` names each.', '']
    if fail['totals']:
        t = fail['totals']; L += [f'**The test log\'s totals.** Failed {t["failed"]}, passed {t["passed"]}, skipped {t["skipped"]}, total {t["total"]}.', '']
    L += ['**Deciding tests named failing.** ' + ('; '.join(f'`{x}`' for x in dec) if dec else 'none') + '.', '',
          '**Other tests named failing** (they decide nothing for this clause). ' + ('; '.join(f'`{x}`' for x in oth) if oth else 'none') + '.', '']
    reading = {'witness': f'**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.',
               'failed attempt': '**Read.** A failed attempt: it does not witness the clause.',
               'not counted': '**Read.** Not counted as the clause\'s witness.'}[status]
    L += [reading + (' ' + why if why else ''), '']
    (d / 'note.md').write_text('\n'.join(L))
    manifest['attempts'].append({'run': run, 'clause': clause, 'attempt': int(attempt), 'reading': status, 'deciding-tests-failed': dec})

for p in sorted(W.rglob('*')):
    if p.is_file() and p.name != 'manifest.json':
        b = p.read_bytes()
        assert b'\r' not in b, p
        assert len(b) <= LIMIT, p
        manifest['retained'].append({'path': p.relative_to(B).as_posix(), 'sha256': hashlib.sha256(b).hexdigest(), 'bytes': len(b)})
(W / 'manifest.json').write_text(json.dumps(manifest, indent=1) + '\n')
print(len(manifest['retained']), 'retained', len(manifest['not-kept']), 'not kept')
for a in manifest['attempts']: print(a['run'], a['reading'], len(a['deciding-tests-failed']))
