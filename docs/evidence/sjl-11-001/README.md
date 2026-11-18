# Bundle SJL-11-001 - the regular-expression area: Broiler.RegEx's rules

**Rules only. Nothing here is a verdict.**

**Milestone:** SJL-11 of the shared JavaScript libraries programme, whose milestone file is
`docs/javascript-shared-libraries/milestones/sjl-11-regexp.md` in the Broiler.VM repository. This bundle judges
its component clauses 1 to 3 and the vectors at library level.

**Rules**, written 2026-10-20 at work package SJL-11.1, before any code of SJL-11.2 exists here, and revised on
2026-10-25, before any run and before this change reached the default branch, to agree with Broiler.VM's JSD-0081 as
filed and, the same day, after review; each file's header says what changed:
- [`decision-rule.md`](decision-rule.md): part A, requirements R1 to R9 and R11 as a contract (clause 1); part B,
  test262 in this repository's CI (clause 2); part C, the adversarial corpus under the JIT and Native AOT
  (clause 3); part D, the vectors at library level (template T3); part E, the process (R10).
- [`reference-arms.md`](reference-arms.md): the library against test-only copies of both engines'
  pre-cut-over regular expressions (template T4).

**Inputs committed with them:** [`tests/Adversarial/corpus.tsv`](../../../tests/Adversarial/corpus.tsv), with its
[README](../../../tests/Adversarial/README.md), part C's population; and
[`eng/adversarial-programs.py`](../../../eng/adversarial-programs.py), which writes both engines' programs from it.
The engines commit those programs in their own bundles `sjl-11-001`; they are not copied here.

**The engines' inputs.** Both engines' programs were written by `eng/adversarial-programs.py` at this change's
`c2aa77167cb3ded40ef50b5c3eeae26f8f7c0b54`, from the corpus whose SHA-256 is
`1a0f8c56bb970fa784fc6e573e9968e939d298d3e912320ed8c1a407fa997dd5`, and both manifests name that commit and that
SHA-256: Broiler.VM's, with `--engine broiler-vm`, and Broiler.JS's, with `--engine broiler-js`, each under its own
bundle's `inputs/adversarial/`. Two runs of each wrote byte-identical files. This change's later commits leave
`tests/Adversarial/corpus.tsv` and `eng/adversarial-programs.py` byte-identical, so the generator at its head writes
the same programs but for the commit their headers name, and that commit stays in the history this change merges.
**So this change merges by a merge commit, not squashed or rebased**: `c2aa771` must stay reachable from the default
branch, or both engines' manifests name a commit that history does not hold.

**Records:**
- This repository's [roadmap](../../roadmap.md), section 6: the record of 2026-10-20 that adopts R1 to R11 as owner
  conditions, with the refusal path and the decisions this repository carries.
- [`HUMAN_REVIEW.md`](../../../HUMAN_REVIEW.md): the dated note of 2026-10-20 on the review state.
- Broiler.VM's JSD-0081, the area verdict, filed in the same set of changes, and its correspondence from code,
  Broiler.VM's `docs/evidence/sjl-11-001/correspondence.md`. Broiler.VM and Broiler.JS commit their own rules in
  their own bundles `sjl-11-001`; neither is read here.

**Owner and reviewer:** the Broiler.RegEx maintainer. Every role the rules name, both engines' owners included, is
held by the same person (Broiler.VM's exception EX-30), and nobody independent has read this bundle.

## What the bundle will hold

| Item | When |
|---|---|
| `manifest.json`: every retained file by its SHA-256, and the commits read | With the first reading |
| `run-library-evidence.sh`: parts A to E at one commit, in a detached worktree, stopping at the first step that does not end as the rule expects | Before the candidate reading |
| `run-reference-arms.sh`: the arms, the population, the comparison and the classification | Before the candidate reading |
| One directory of logs per reading, each step's command the first line of its log | At each reading |

*Added 2026-10-27, at task 2.14 of SJL-11.2; no rule changed.* Part C's runs are CI's at the reading's commit: the
`native-aot-probe` job's `adversarial-aot.txt` and `adversarial-jit.txt` for linux-x64 and win-x64 (each row's child
exit and status line, the two compared byte for byte), and the `adversarial-controls` job's `control-steps.txt`,
`control-nesting.txt` and `revert.txt` (the two controls and their revert), each kept as an artifact of its run. A
reading names those runs by their identifiers and retains the reports by their SHA-256;
[`probes/Regex.Probe/README.md`](../../../probes/Regex.Probe/README.md) gives the commands that repeat them locally.

*Added 2026-11-02, at task 2.12 of SJL-11.2; no rule changed.* **Part D's population is named.** Part D reads "every
vector of conformance/vectors/regexp/*.vectors at the corpus revision bundle sjl-11-001 names", and until this note
the bundle named none. It names **corpus revision 32 at Broiler.JS `b2c2e2243a086285d806866a64b83297791a8ec5`**, the
revision Broiler.JS's bundle `sjl-11-001` names for its own part D (in its `decision-rule.md`, part D's note of
2026-11-01 on the owner's decision D-partd: "The corpus revision this part names is 32"; the decision is recorded in
Broiler.VM's JSD-0081; the status vectors are judged here only), copied into
[`tests/Vectors/regexp/`](../../../tests/Vectors/README.md) with the pin file `tests/Vectors/regexp.pin`. A later
revision is a new copy, a new pin and a dated note here. **Part D's runs** are CI's at the reading's commit: the
`native-aot-probe` job's `vectors-aot.txt` and `vectors-jit.txt` for linux-x64 and win-x64 (each the report of
`--vectors tests/Vectors`, whose header carries the pin and the pin check comes before the run, the two compared byte
for byte), each kept as an artifact of its run, and the test job's run of `VectorsRunTests` and `VectorsProbeTests`,
named by its run identifier. At task
2.12, locally on linux-x64, the two reports were one file (`docs/roadmap.md`, task 2.12's record); the rule's MET is
read at the candidate reading, from CI's runs, win-x64's among them.

**The readings**, named as they come:
- **The candidate reading**, at the commit `0.2.0-preview.1` is packed from. A part NOT MET there blocks both
  cut-overs.
- **The end reading**, at the commit both engines pin after their cut-overs, if it differs.

*Dated note, 2026-11-01 (task 2.9 of SJL-11.2).* The candidate reading's line read "at the commit `0.2.0-preview.1` is
packed from", after decision D12 of Broiler.VM's JSD-0081. The owner wrote on 2026-11-01 "Please keep using version
0.1.0-x scheme!": the SJL-11 surface ships as `0.1.0-preview.6`, and the candidate reading is taken at the commit
`0.1.0-preview.6` is packed from. The filed rule carries the same note ([`decision-rule.md`](decision-rule.md)).

*Dated note, 2026-11-01 (the review of task 2.9), mirroring the rule's extended note.* Two more lines of the filed rule
are read otherwise, not rewritten: check E-4's "package validation against 0.1.0-preview.4" is read as against
`0.1.0-preview.5`, by the owner's decision of 2026-11-01 ("The SJL-11 surface ships as `0.1.0-preview.6`, validated
against `0.1.0-preview.5`", task 2.9's record in `docs/roadmap.md`); and the pins line's `Broiler.Unicode.Algorithms`
and `Broiler.Unicode.Tables` `0.1.0-preview.5`, with check A-10's "at the version above", are read as `0.1.0-preview.6`
of both, since the owner's dependency update `77284f1` of 2026-10-30, which both engines' pins follow.

*Dated note, 2026-11-02 (task 2.13 of SJL-11.2; no rule changed).* The table above read "`run-reference-arms.sh`: the
arms, the population, the comparison and the classification | Before the candidate reading". What that script will
run exists: the arms S, V and J as separate executables under
[`tests/ReferenceArms/`](../../../tests/ReferenceArms/README.md), the population's generator, the comparison and the
classifier under [`scripts/reference-arms/`](../../../scripts/reference-arms/README.md), whose `run.sh` runs every step
in order, and the fast-path differential over population P4. They were run once, **unjudged**, at the task's branch,
with P1 read from the corpus revision of Broiler.JS's branch `sjl-11-3-regexp` as a smoke run and the arbiter
`regexpref.py` taken from that branch: [`reference-arms.md`](reference-arms.md)'s "MET if and only if" was not read,
and nothing of that run is retained here or is a reading (validation principle 5 keeps its figures out of this
bundle; [`docs/roadmap.md`](../../roadmap.md)'s record of task 2.13 has them). **The judged run waits for corpus
revision 32**, which SJL-11.3's task 3.3 produces, to reach Broiler.JS's `main`: then P1 is that revision's, the arbiter
that commit's, and the run is a reading of this bundle at the commit `0.1.0-preview.6` is packed from, by
`run-reference-arms.sh` (still to be written, as the table says) over `scripts/reference-arms/run.sh`.

*Dated note, 2026-11-02 (task 2.13's second review; no rule changed).* The note above read "**The judged run waits for
corpus revision 32**, which SJL-11.3's task 3.3 produces, to reach Broiler.JS's `main`". It has: revision 32 is on
Broiler.JS's `main` since `b2c2e224` (Broiler.JS#1089), whose `regexpref.py` is the blob `d031add5` the unjudged run's
arbiter was, and task 2.12 copied its vectors to `tests/Vectors/` and fixed the one defect of S that run showed (`\k` in
a class, decision D-s5). The comparison was run again, unjudged, twice at this branch's `a113c91`, with P1 and the
arbiter from `b2c2e224`, after the harness's limits were fixed in one place, its stops made one answer, and the cases
a limit of time stops fixed in a stop list a calibration wrote and the repository keeps
(`tests/ReferenceArms/stops.tsv`), so that no limit of time decides an answer: the two runs' listings are byte for
byte the same, as [`reference-arms.md`](reference-arms.md)'s "How it is read" asks
([`docs/roadmap.md`](../../roadmap.md)'s record of task 2.13 has both runs' digests). It is still not a reading
of this bundle: it was not taken by `run-reference-arms.sh` at the commit `0.1.0-preview.6` is packed from, and the
differences it leaves in class (c) wait for the correspondence's rows or for their inputs reduced.

## The commits named so far

- **The rule commit:** the commit that adds this bundle, `tests/Adversarial/` and `eng/adversarial-programs.py`, on
  the branch that lands SJL-11.1 here. Its identifier is recorded in this README when it reaches the default
  branch.

No other commit is named yet.

*Dated note, 2026-11-15 (SJL-11.2's tail: task 2.10's library job, task 4.1; no rule changed).* The item above read
"**The rule commit:** the commit that adds this bundle, `tests/Adversarial/` and `eng/adversarial-programs.py`, on the
branch that lands SJL-11.1 here. Its identifier is recorded in this README when it reaches the default branch.", and "No
other commit is named yet." It has reached it. **The rule commit is `fca7a6d6b010d1e830fd79a8f56ceb0c15064976`**
(2026-10-20, "SJL-11.1: adopt R1 to R11 as owner conditions, and bundle sjl-11-001's rules with the adversarial
corpus"), the one commit that adds this README, [`decision-rule.md`](decision-rule.md),
[`reference-arms.md`](reference-arms.md), `tests/Adversarial/` and `eng/adversarial-programs.py`. The same branch
revised the two rule files three times on 2026-10-25, before any run, each revision stated in the file's header:
`cbf49e8`, `a9738ca` and `edfb2d8`. It reached `main` by the merge commit `e568b88` (Broiler.Regex#6, 2026-10-25), whose
second parent is `edfb2d8`, so the rules as filed are `edfb2d8`'s. Since then the rule files have changed only by the
dated notes of 2026-11-01 (`d22f118`, `53aa534`), which read lines otherwise and rewrite none: every `rule` block of
both files is byte for byte `edfb2d8`'s at this note's commit. The commits of the engines' inputs, `c2aa771`, are named
above. **No reading's commit is named yet**: the candidate reading's is the commit `0.1.0-preview.6` is packed from
(task 4.2).

*Dated note, 2026-11-15 (the same work; no rule changed).* The table above reads "`run-library-evidence.sh`: parts A
to E at one commit, in a detached worktree, stopping at the first step that does not end as the rule expects | Before
the candidate reading" and "`run-reference-arms.sh`: the arms, the population, the comparison and the classification |
Before the candidate reading"; the note of 2026-11-02 above calls the second "still to be written, as the table says".
Both are written (WP4, task 4.1), beside this README:
- [`run-library-evidence.sh`](run-library-evidence.sh) `COMMIT OUT --test262 DIR [--broiler-code DIR]` checks the
  commit out as a detached worktree and runs, in order, part C's controls and revert from the clean tree, the Release
  build (E-1), the tests (A, D's runner, E-3, E-7), the probe's Native AOT publish (E-2) and its four reports under
  Native AOT and the JIT compared byte for byte (the default population, C, D and check B-2), the pack (E-4), the
  fuzz session and its control (E-5) and the assurance check (E-6), each step's command the first line of its log, and
  stops at the first step whose exit is not the one the rule expects. Check B-1 is recorded as not run.
- [`run-reference-arms.sh`](run-reference-arms.sh) `COMMIT OUT --test262 DIR --unicode DIR --broiler-js DIR
  --broiler-vm DIR --correspondence-commit SHA` checks the commit out the same way, checks test262's `test/` against the
  pin's tree, P1 against the commit's `tests/Vectors/regexp.pin` and writes the correspondence out with its blob, runs
  that commit's [`scripts/reference-arms/run.sh`](../../../scripts/reference-arms/README.md), and lists every
  population, answer, comparison and classification file by SHA-256.

**Part B's library job exists** (task 2.10): CI's `test262-library` job and the probe's `--test262` mode
([`probes/Regex.Probe/README.md`](../../../probes/Regex.Probe/README.md)), with
[`test262/library-unreadable.txt`](../../../test262/library-unreadable.txt). **Its host job does not**: B-1's
retained commit is Broiler.JS's SJL-11.5 branch until it merges (decision D35), and that branch does not exist. What
each script and the job were shown to do, locally and unjudged, is in [`docs/roadmap.md`](../../roadmap.md)'s record
of 2026-11-15; nothing of those runs is a reading or retained here (validation principle 5). The note of 2026-11-02
above ends "the differences it leaves in class (c) wait for the correspondence's rows or for their inputs reduced":
the inputs the arbiter cannot reach were put through one declared reduction,
[`scripts/reference-arms/reduce.py`](../../../scripts/reference-arms/README.md), and none became an input the arbiter
reaches with the same difference; that record says what each gave. The rows are Broiler.VM's and wait.

*Dated note, 2026-11-15 (the review of the same work; no rule changed).* Four statements of the two notes above are
corrected here, as [`docs/roadmap.md`](../../roadmap.md)'s note of the same date on that record says in full.
- The first note ends "**No reading's commit is named yet**: the candidate reading's is the commit `0.1.0-preview.6` is
  packed from (task 4.2)". `0.1.0-preview.6` was published to nuget.org on 2026-11-15 from `df5cfc2` (publish run
  43736141111), before any candidate reading, from a tree with neither script and no `test262-library` job. No reading's
  commit is named yet; whether the candidate reading is taken at `df5cfc2` or at a later commit, packed as a later
  version, is the owner's question and is not decided here.
- It also names "**The rule commit is `fca7a6d6b010d1e830fd79a8f56ceb0c15064976`**", the branch commit that adds the
  bundle. Broiler.VM's half of this bundle, and bundles `sjl-12-001` and `sjl-13-001`, name the default-branch merge as
  the rule commit (Broiler.VM's README, its note of 2026-11-15); here that would be
  `e568b88248c7de5dd75210e22cd357197108e644` (Broiler.Regex#6), whose rule files are `edfb2d8`'s. Which this README
  names is left to the owner or this bundle's reviewer. And where it says the branch "revised the two rule files three
  times", the rule files were revised in three commits, `cbf49e8` and `a9738ca` changing both and `edfb2d8` changing
  `decision-rule.md` only.
- The second note's [`run-library-evidence.sh`](run-library-evidence.sh) now runs check E-6 as the commit's CI job does:
  since Broiler.Regex#19 (`df5cfc2`), `dotnet tool restore` and `dotnet hairs assurance check --root .` from the cleaned
  tree; `--broiler-code` is read only for a commit before it. Its reports reproduce byte for byte; its packages do not
  (the pack writes times into them), so their digests are in `packages.sha256`, apart.
  [`run-reference-arms.sh`](run-reference-arms.sh) also checks the Broiler.Unicode checkout against `--unicode-commit`
  (default `0bcb917`, the full run's), since P5 and P6 come from it.
- `run-reference-arms.sh` does not take [`reference-arms.md`](reference-arms.md)'s two negative controls (a copy of arm
  V and one of arm J, each with one result changed, which the comparison must report and not classify); task 2.13 took
  them from scratch worktrees. They are owed at the candidate reading. And of the inputs the arbiter could not reach,
  895 were reduced to an input it reaches where the difference does not survive: they stay in class (c) only under a
  reading of the rule's "until reduced" its owner decides (the roadmap's note).

*Dated note, 2026-11-16 (SJL-11.2; a dated rule file added, no rule edited; nothing here is a reading).* The table
above lists "`run-reference-arms.sh`: the arms, the population, the comparison and the classification", and the
rules' list at the top names [`decision-rule.md`](decision-rule.md) and [`reference-arms.md`](reference-arms.md) only.
- **A revision of the reference-arm rule** is filed beside them:
  [`reference-arms-revised-2026-11-16.md`](reference-arms-revised-2026-11-16.md), option R, which says how class (c)
  reads an input the arbiter cannot reach once the declared reduction (`scripts/reference-arms/reduce.py`) has run, and
  adds a class (b') "beyond the arbiter". It was written after task 2.13's runs, their classification and the
  reduction of 2026-11-15 were seen, so its decision is weaker evidence (`decision-rule.md`, "Revisions"); it was filed
  as the recommended choice under the owner's standing direction of 2026-10-18 and is the owner's to confirm or
  overturn. `reference-arms.md` is unchanged.
- **The classification** is now `scripts/reference-arms/classify.py` `@4`, which reads Broiler.VM's correspondence at
  **`7c65a8febacdcf8bda2d848d21ac2e7748696359`** (the merge of Broiler-Platform/Broiler.VM#211, the file's blob
  `f0f6f0800d7d6336ab1c538bb7afd076a8a1fb2f`): its rows S8a, S12 to S15, M8 and M9 and its widened rows S7, S8 and M4.
  `classification.json` names that commit as `rows-read-from`, beside the blob of the correspondence a run is given.
  With `--reduction`, it reads the revision's option R.
- **What the script does not yet do.** `run-reference-arms.sh` runs `scripts/reference-arms/run.sh`, whose
  classification step reads the rule as filed: it runs neither `reduce.py` nor the classifier with `--reduction`. A
  reading under the revision needs both after the comparison, as [`docs/roadmap.md`](../../roadmap.md)'s record of
  2026-11-16 lists among what the candidate reading (task 4.2) waits on.
- **Class (c) is not empty** when the classifier `@4` and option R are read over task 2.13's retained comparison and
  the reduction of 2026-11-15 (the roadmap's record of 2026-11-16, which has the counts; this README holds none). No
  reading is taken and nothing is declared met.
- *At the review of `@4` the same day*, the classification became `classify.py` `@5`, which reads row M8 of the same
  correspondence only where its listed inputs bear on the difference (the second bullet above reads "`@4`"); the
  correspondence commit it reads is unchanged. Class (c) is still not empty, and is larger than under `@4` (the
  roadmap's note of the same date has the counts). Nothing is declared met.

*Dated note, 2026-11-16, the owner's ruling on population P4 (SJL-11.2; a dated rule file added, no rule edited;
nothing here is a reading).* The note above reads "**The classification** is now `scripts/reference-arms/classify.py`
`@4`" and, at its review, "`@5`".
- **A second revision of the reference-arm rule** is filed beside the others:
  [`reference-arms-revised-2026-11-16b.md`](reference-arms-revised-2026-11-16b.md), which records the owner's ruling of
  2026-11-16, "You can exclude with note regarding the 19 patterns", as the owner's own decision. It excludes from P4
  the 19 patterns the ruling names, whose arbiter cost is in the pattern and whose inputs held class (c), enumerated by
  id in `scripts/reference-arms/p4-excluded.tsv` with the three limbs they were found by, and carries the owner's note
  on them: a known gap of the reference arms, not a verified agreement, their answers kept and reported outside the
  judged population. It was written after task 2.13's runs, their classification and the reduction were seen, so it is
  weaker evidence (`decision-rule.md`, "Revisions"). `reference-arms.md` and option R are unchanged.
- **The classification** is now `classify.py` `@6`: with `--exclude`, the excluded patterns' differences are listed
  apart, in no class and not class (c); without it, `@6` classes as `@5`. The correspondence commit it reads is
  unchanged.
- **What the script does not yet do**: `run-reference-arms.sh` passes neither `--reduction` nor `--exclude`, as the
  note above says of the first.
- **Class (c) is still not empty**, and is smaller than under `@5` (the roadmap's record of the same date has the
  counts). Nothing is declared met.

*Dated note, 2026-11-16, Broiler.VM's rows M10 to M15 (SJL-11.2; no rule file added or edited; nothing here is a
reading).* The note above reads "**The classification** is now `classify.py` `@6`" and "The correspondence commit it
reads is unchanged", that is `7c65a8fe`.
- **The classification** is now `classify.py` `@7`, which reads Broiler.VM's correspondence at
  **`44511066bfb38952c12c81557f56a34bd21ee41e`** (the merge of Broiler-Platform/Broiler.VM#212, the file's blob
  `94b860cc00b71d2e142473a3685fdd92f1cccee4`): its rows M10 to M15, under the owner's decision D-jsexec, each read from
  the row's text and tried after `@6`'s rows in the order the correspondence states. Rows M11 to M14 take an input only
  where the arbiter's answer to the row's reading of the pattern is arm J's, up to row M10's names, and not the text's.
  `classification.json` names that commit as `rows-read-from`; a correspondence without the rows is refused.
- **Row B1's widening** names P3's `O06`, an input the arbiter cannot reach whose reduction shows no difference and
  where neither side stopped at a bound it declares: option R keeps it class (c) whichever row names it, and `@7` gives
  the widening no predicate.
- **What the script does not yet do**: `run-reference-arms.sh` passes neither `--reduction` nor `--exclude`, as the
  notes above say.
- **Class (c) is still not empty**, and is smaller than under `@6` (the roadmap's record of the same date has the
  counts). Nothing is declared met.

*Dated note, 2026-11-16, the owner's ruling on P3's `O06` and Broiler.VM's rows M16 to M26 (SJL-11.2; a dated rule file
added, no rule edited; nothing here is a reading).* The note above reads "**The classification** is now `classify.py`
`@7`, which reads Broiler.VM's correspondence at **`44511066bfb38952c12c81557f56a34bd21ee41e`**" and "**Row B1's
widening** names P3's `O06` ...: option R keeps it class (c) whichever row names it, and `@7` gives the widening no
predicate".
- **A third revision of the reference-arm rule** is filed beside the others:
  [`reference-arms-revised-2026-11-16c.md`](reference-arms-revised-2026-11-16c.md). It records the owner's ruling of
  2026-11-16 on `O06`, "Classify by its row", as the owner's own decision. An input the arbiter cannot reach may be
  classed by the row that names it, where the row gives the input and the mechanism, every other arm answers the input
  as S does, and a predicate the classifier states shows S's answer is the text's. It was written after task 2.13's
  runs, their classification and the reduction were seen, so it is weaker evidence (`decision-rule.md`, "Revisions").
  `reference-arms.md`, option R and the revision of 2026-11-16b are unchanged.
- **The classification** is now `classify.py` `@8`, which reads Broiler.VM's correspondence at
  **`626e4ed88afe8898724bda669e981cf7cf1b98b2`** (the merge of Broiler-Platform/Broiler.VM#215, the file's blob
  `d4605f5cd1461137a3533e816d5bc2275434cce8`).
  - It adds rows M16 to M26, under the owner's decision D-jsrest, each read from the row's text and tried after M15 in
    the order the correspondence states.
  - It reads the dated notes on rows M8, M12, M13 and M15 in place.
  - With `--reduction`, it reads the revision of 2026-11-16c, by which row B1 holds `O06`.
  - `classification.json` names that commit as `rows-read-from`. A correspondence without the rows, or without row
    B1's words naming `O06` and its mechanism, is refused.
- **What the script does not yet do**: `run-reference-arms.sh` passes neither `--reduction` nor `--exclude`, as the
  notes above say.
- **Class (c) is still not empty**, and is smaller than under `@7`: one pattern's inputs, which no row names (the
  roadmap's record of the same date has the counts). Nothing is declared met.

*Dated note, 2026-11-17, Broiler.VM's row M27 (SJL-11.2; no rule edited; nothing here is a reading).* The note above
reads "**The classification** is now `classify.py` `@8`, which reads Broiler.VM's correspondence at
**`626e4ed88afe8898724bda669e981cf7cf1b98b2`**" and "**Class (c) is still not empty**, and is smaller than under `@7`:
one pattern's inputs, which no row names".
- **The classification** is now `classify.py` `@9`, which reads Broiler.VM's correspondence at
  **`5e90bfe485aad60b3637d9f69e54ed71adcd73af`** (the merge of Broiler-Platform/Broiler.VM#216, the file's blob
  `2831290201cca3fb6b524a9be8680755eb566f37`).
  - It adds row M27, under the owner's decision D-jsrest, read from the row's text and tried after M26, as the row
    says. The row names the mechanism of the inputs the note above leaves to no row.
  - `@8`'s predicates, the reduction, the revision of 2026-11-16c and the exclusion are unchanged.
  - `classification.json` names that commit as `rows-read-from`. A correspondence without row M27 is refused.
- **What the script does not yet do**: `run-reference-arms.sh` passes neither `--reduction` nor `--exclude`, as the
  notes above say.
- **Class (c) is empty** on task 2.13's runs, with `--reduction` and `--exclude`, for the first time (the roadmap's
  record of the same date has the counts). That is not a reading: it was taken on the retained runs, after they were
  seen, and it reads option R, which waits on the owner, and the revisions filed under it. Nothing is declared met.

*Dated note, 2026-11-17, the owner's confirmation of option R (SJL-11.2; no rule file added or edited; nothing here is
a reading).* The note of 2026-11-16 above says of
[`reference-arms-revised-2026-11-16.md`](reference-arms-revised-2026-11-16.md), option R, that "it was filed as the
recommended choice under the owner's standing direction of 2026-10-18 and is the owner's to confirm or overturn", and
the note of 2026-11-17 above says the classification "reads option R, which waits on the owner". The owner has ruled.
- **The question put**, verbatim: "With the classifier reading row M27 (Broiler.Regex#25), no unclassified difference
  (class c) remains. One outstanding item for the candidate reading is option R, the reference-arms rule revision I
  filed on 2026-11-16 as the recommended choice. It puts an input the reference checker can't reach into class (b′)
  "beyond the arbiter" when three things hold: cut down to a length the checker can reach, the library and the other
  engine agree; the library's answer there is the checker's; and at full length one side stopped at a limit it
  declares. That covers 2,250 differences, and your O06 ruling builds on it. Do you confirm option R?" The options were
  "Confirm option R" (recommended) and "Overturn option R".
- **The owner chose "Confirm option R"** on 2026-11-17, described as: "Recorded as your own decision. The 2,250 stay in
  class (b′), and the candidate reading no longer waits on this."
- **That is the owner's own decision.** Option R's file says "It is not the owner's own decision: the owner may
  overturn it"; from this date it is read as the owner's own decision, confirmed, and is not rewritten. The file stays
  unedited, as do [`reference-arms.md`](reference-arms.md) and the revisions of 2026-11-16b and 16c, which build on it.
  The confirmation changes no clause, so no dated rule file is added: the bundle's "Revisions" rule
  ([`decision-rule.md`](decision-rule.md)) asks for a new dated file for a rule revised, and the owner's earlier rulings
  have one each (16b and 16c) because each changed a clause.
- **It is still weaker evidence.** Option R was written after task 2.13's runs, their classification and the reduction
  of 2026-11-15 were seen (its header; `decision-rule.md`, "Revisions"); the owner's confirmation does not change when
  it was written. The 2,250 differences it holds in class (b') are those of the classification by `@9` over task
  2.13's runs (the roadmap's record of row M27), which is not a reading.
- Nothing is declared met. [`docs/roadmap.md`](../../roadmap.md)'s record of the same date has the ruling and what the
  candidate reading (task 4.2) still waits on.

*Dated note, 2026-11-17, task 4.2's preparations (SJL-11.2; no rule file added or edited; nothing here is a reading).*
The notes of 2026-11-16 and 2026-11-17 above say "**What the script does not yet do**: `run-reference-arms.sh` passes
neither `--reduction` nor `--exclude`", and the note of 2026-11-15 says "`run-reference-arms.sh` does not take
[`reference-arms.md`](reference-arms.md)'s two negative controls ... They are owed at the candidate reading."
- **The script now does both**, through the commit's `scripts/reference-arms/run.sh`, which it runs. After the
  comparison the run classifies as the rule was filed (`run/comparison`), runs the declared reduction over that
  classification (`run/reduction`), and classifies with `--reduction` and `--exclude
  scripts/reference-arms/p4-excluded.tsv` (`run/classification`, the classification the rule as revised reads). It then
  takes the two negative controls, a copy of arm V over P4 and one of arm J over P1, each run once and removed; a
  control the comparison does not report, or the classifier does not list unclassified, fails the run (`run/controls`).
  So does one whose changed answer, read with no control case named, is not class (c), or whose revert (the run's own
  answers) does not classify as the run does for its class (both added at review, the same day). The digests list the
  new directories, and the script's last line names the classification's log and the controls taken. A commit before this note runs the `run.sh` it has, which does none of it.
- **Shown, not read.** Over task 2.13's retained differences and reduction, from a clean tree, the procedure wrote the
  `@9` classification byte for byte (`classification.json` `d6ca3a14…`, `classified.tsv.gz` `5dd1a960…`), class (c)
  empty. Both controls were reported unclassified, and each one's changed answer, read with no control case named, is
  class (c). [`docs/roadmap.md`](../../roadmap.md)'s record of the same date has the runs and their digests. None is a
  reading or retained here (validation principle 5).
- **The harness's `Win32Exception`**, which ended one run of 2026-11-15 and one of this stage's base runs, is fixed in
  the arms' harness (`tests/ReferenceArms/Common/Harness.cs`). The arms' answers over P1 to P3 are unchanged.
- **The reading's commit.** `0.1.0-preview.6` was published from `df5cfc2`, and the library is unchanged since. The
  roadmap records, under the owner's standing direction of 2026-10-18, the recommended reading at `df5cfc2` (the
  published bits) with a later commit's scripts, jobs and classifier run against it, and the alternative, a later
  commit published as `0.1.0-preview.7`. The note of 2026-11-15 above leaves the rule commit (`fca7a6d` or `e568b88`)
  to the owner or this bundle's reviewer, and so does this one.
- **The candidate reading still waits on** the host job (B-1, which needs Broiler.JS's SJL-11.5 branch, decision D35),
  CI green at the reading's commit (B-3), the reading's commit and the rule commit, and the reading itself. Nothing is
  declared met.

*Dated note, 2026-11-17, task 2.10's host job (SJL-11.2; no rule file added or edited; nothing here is a reading).* The
note of 2026-11-15 above reads "**Its host job does not**: B-1's retained commit is Broiler.JS's SJL-11.5 branch until
it merges (decision D35), and that branch does not exist", and the note above says the candidate reading waits on "the
host job (B-1, which needs Broiler.JS's SJL-11.5 branch, decision D35)".
- **The host job exists.** CI's `test262-host` job checks Broiler.JS out at
  **`f690836dba19ded358aacc4b6edb9c8107a53ccf`**, the head of its branch `sjl-11-5`, which
  [`docs/roadmap.md`](../../roadmap.md)'s record of 2026-11-17 names as D14's retained commit, and test262 at the pin.
  [`scripts/test262-host/run.sh`](../../../scripts/test262-host/run.sh) packs the commit's library as the version that
  Broiler.JS commit pins into a local feed listed first, builds the shell in Release against it, checks the shell holds
  the packed DLL, runs Broiler.JS's `scripts/compliance/run_test262.py` with `--include-negative`, original variant, over
  part B's population and staging's RegExp directory, and holds the report to
  [`test262/host-expected-failures.txt`](../../../test262/host-expected-failures.txt), as B-1 reads.
- **The expected-failures list** names one file, `test/built-ins/RegExp/proto-from-ctor-realm.js`, in the RegExp
  object (its prototype taken from the running realm, not the new target's). Every file listing
  `regexp-duplicate-named-groups` passes in the local run of the job's commands, the four task 2.6 left to this job
  among them; the roadmap's record has the runs, a planted regression among them, and the choices taken with their
  alternatives.
- **The job has not run in CI**, and B-1 is read from CI's run at the reading's commit (B-3); part B's control is a
  reading's. [`run-library-evidence.sh`](run-library-evidence.sh) records B-1 as not run, as before. Its header said
  B-1 "waits for Broiler.JS's SJL-11.5 branch", and the log it writes for B-1 (`logs/test262-host.log`) said that "no
  commit is named, because Broiler.JS's SJL-11.5 branch ... does not exist yet"; both were out of date and are
  corrected at the review of this work. A reading taken at an earlier commit keeps the old text in that log.
- **At review**, the population is fixed by the verdict, not by the job's script alone:
  [`compare.py`](../../../scripts/test262-host/compare.py) holds B-1's population as decision-rule.md words it, plus
  staging's RegExp directory, and is red on a run that leaves out any of those paths or whose files listing
  `regexp-duplicate-named-groups` are not the 19 it declares at the pin; its tests hold that copy to the rule's line
  and the workflow's sparse checkout to it. Broiler.JS's commit must stay reachable until a dated record moves it,
  since `publish.yml` runs the job too.
- **The candidate reading still waits on** CI green at the reading's commit (B-3), the reading's commit (recommended
  `df5cfc2` with this tree's scripts and jobs) and the rule commit, and the reading itself. Nothing is declared met.

*Dated note, 2026-11-18, the candidate reading (task 4.2 of SJL-11.2; no rule file added or edited). This note
supersedes these lines above.* *Corrected at review, the same day, before it reached the default branch. Its first
text read part A on "The manifest names no run of any part A witness failing". Part B's control run turned A-5's `\k`
witnesses red, and that run's Release-job logs are now retained. Part A is read check by check below, and still reads
NOT MET.*
- The note of 2026-11-15 says "**No reading's commit is named yet**: the candidate reading's is the commit
  `0.1.0-preview.6` is packed from (task 4.2)". Its review note says "No reading's commit is named yet; whether the
  candidate reading is taken at `df5cfc2` or at a later commit, packed as a later version, is the owner's question and
  is not decided here".
- The review note also says of the rule commit, `fca7a6d` or `e568b88`: "Which this README names is left to the owner
  or this bundle's reviewer".
- The note of 2026-11-15 says of the reference arms' negative controls: "They are owed at the candidate reading". This
  reading takes both (below).
- The note of 2026-11-17 ends: "**The candidate reading still waits on** CI green at the reading's commit (B-3), the
  reading's commit (recommended `df5cfc2` with this tree's scripts and jobs) and the rule commit, and the reading
  itself." The list at the end of this note replaces it. `49d7be1f`, below, is the "`df5cfc2` with this tree's scripts
  and jobs" that bullet recommends: `df5cfc2`'s library tree, at a later commit that carries the scripts and jobs.

**The reading departs from one line of the rule.** [`decision-rule.md`](decision-rule.md)'s dated note of 2026-11-01
reads: "The candidate reading is read as taken at the commit `0.1.0-preview.6` is packed from." That commit is
`df5cfc2`. This reading is taken at `49d7be1f`, which is not the commit the package was packed from. A README note
cannot rewrite a rule's line, so this is a departure, taken by decision 1 below, and the owner can overturn it:
- the library tree and props are the same git trees at both commits;
- the tests, scripts, probe, workflow and test262 lists are a later commit's (`git diff --name-only df5cfc2 49d7be1f`
  names 4 files under `Broiler.Regex.Tests/`, 4 under `probes/`, 15 under `scripts/`, 2 under `test262/`, 2 under
  `tests/` and `.github/workflows/ci.yml`, besides `docs/`, `CHANGELOG.md` and `.gitignore`);
- the published bytes are not read.

Every per-part reading below is a reading at `49d7be1f`, not at the commit the rule's line names.

**The two commits.** Both decisions are taken under the owner's direction of 2026-10-18 ("Please do all recommended
decisions and skip waiting for review"). They are recommended decisions, not the owner's own words, and the owner can
overturn either. [`docs/roadmap.md`](../../roadmap.md)'s record of 2026-11-18 gives each decision's alternatives.
- **The reading's commit is `49d7be1f3ef148f9c262f0e52b8c360c07fd838d`** (Broiler-Platform/Broiler.Regex#27's merge).
  - Its `Broiler.Regex/`, `Directory.Build.props`, `Directory.Packages.props`, `NuGet.config` and `eng/` are the same
    git trees as at `df5cfc2`, the commit `0.1.0-preview.6` was published from.
  - So the reading reads the published package's sources with this tree's scripts, runner and jobs. It does not read
    the published bytes: no local build reproduces the published DLL.
  - *Not taken*: `df5cfc2`, which has neither script nor the test262 jobs; and a `0.1.0-preview.7`, whose publication
    is the owner's (D13).
- **The rule commit is `e568b88248c7de5dd75210e22cd357197108e644`** (Broiler-Platform/Broiler.Regex#6's merge), whose
  rule files are `edfb2d8`'s. Every `rule` block is the same at the reading's commit. Broiler.VM's half of this bundle
  and bundles `sjl-12-001` and `sjl-13-001` name the merge in the same way.
  - *Not taken*: `fca7a6d`.

**The reading's files.** The table above reads "`manifest.json`: every retained file by its SHA-256, and the commits
read | With the first reading" and "One directory of logs per reading, each step's command the first line of its log |
At each reading". This is the first reading:
- [`candidate-reading/`](candidate-reading/) is its directory:
  - `library-evidence/`: `run-library-evidence.sh 49d7be1f`, linux-x64, exit 0, every step as the rule expects;
  - `reference-arms/`: `run-reference-arms.sh 49d7be1f` over all seven classes, with both negative controls, exit 0;
  - `ci/`: CI's push run at the reading's commit and part B's control runs, with their API answers, job logs and
    reports;
  - `part-b-control/`: the control's record and plant, and the digests of the red run's eight failed jobs' logs
    (`run1-SHA256SUMS`). That file names each log as `run1/<job>.log`, its path in the session's notes, and no
    `run1/` directory is in this bundle. Five of the logs are retained under `ci/job-logs/`: B-1's 134575819645, B-2's
    134575819815 and 134575819759, and the Release jobs 134575819708 and 134575819702. 134575819759 and 134575819702
    come from Windows and are stored with LF, so their digest in `run1-SHA256SUMS` is the manifest's `as-fetched` one.
    The other three logs are not kept. The manifest names all eight under `part-b-control/jobs/` as not-kept entries:
    the five with `same-as` pointing to their `ci/job-logs/` file, the three with why they are not kept.
- [`manifest.json`](manifest.json) names every retained file by its SHA-256 and size, and the commits and CI runs
  read. Each file kept out is named by its digest, with where it was and why: the populations, the arms' answers and
  the listings (by digest and line count, as `reference-arms.md`'s "How it is read" says), the packages, and the
  inputs' copies.

**CI's runs.**
- Run 44206505415 is the push to `main` at `49d7be1f`: 14 of 14 jobs green. B-1 is the host job 134473694801. B-2 is
  the library jobs 134473694923 (linux-x64) and 134473695064 (win-x64).
- Part B's control was taken in CI under the owner's permission of 2026-11-18 ("Allow scratch branch").
  - The plant is `deac674`, on the scratch branch `sjl-11-2-control-b`. Its run, 44237117988, is red: B-1
    134575819645 and B-2 134575819815 and 134575819759 are red on the five
    `named-groups/invalid-dangling-groupname*-u.js` files.
  - The revert is `d0dd222`, whose tree is `49d7be1f`'s. Its run, 44238736222, is green on 14 of 14 jobs.
  - The branch could not be deleted (the session's remote refused the delete). It stays on GitHub at `d0dd222`,
    unmerged and with no pull request, for the owner to delete.

**The reading's results** (counts are results, written here at this reading):
- *Part A*: 2,644 library tests, 49 architecture tests and 11 fuzz-target tests, none failed.
- *Part B*:
  - the host job ran 2,547 files, and only the one listed file fails;
  - all 19 files of duplicate named groups pass;
  - the library job's report is the same file under the JIT and Native AOT on both RIDs, with 0 failed and 43 files
    listed unreadable.
- *Part C*: 65 rows give their declared status in both modes on both RIDs, none ends abnormally, and both controls
  are reported (17 rows and 15 rows).
- *Part D*:
  - 1,420 vectors; no normative or divergence vector differs;
  - 18 of 18 status vectors give their status, and all 85 implementation-defined vectors are reported;
  - 27 vectors of six named ops of the object model are listed as not implemented;
  - the two modes' reports are one file per RID.
- *Part E*: the build and publish report no warning, the 29 suppressions are listed, and the fuzz session found
  nothing in 24,000 iterations while its control found the plant. The assurance check finds no violation, and turned
  red on drift in the control's run.
- *The reference arms*:
  - 1,901,509 differences, byte for byte task 2.13's comparison;
  - as filed: a 1,886,022, a' 45, b 8,983, c 6,459;
  - after the declared reduction (byte for byte that of 2026-11-15) and the exclusion, as revised: a 1,885,368, a' 45,
    b 4,876, b' 2,250, excluded 8,970, **no class (c)**;
  - both negative controls reported unclassified. Each control's changed answer, read with no control case named, is
    class (c), exit 1, and each revert classifies as the run does.

**The reading, per part, at `49d7be1f`**, by [`decision-rule.md`](decision-rule.md) and the reference-arm rule as
revised:
- **Part A: read NOT MET.** The tests pass, but the rule's MET also needs "each witness was watched failing once". Its
  "How it is read" decides each check "by its test's result in the test log of the reading, and by the retained run of
  its witness failing once; a check whose witness was not seen failing is not met". "A file the manifest does not
  name decides nothing". Read check by check in the table below: three checks have a witness for one of their
  clauses, and eight have none.
  - *The tests that decide a check* are the test classes whose headers name it. The test logs
    (`library-evidence/logs/tests.log`, and the Release jobs 134473695300 and 134473694930) carry totals per assembly,
    not a line per test: 2,644 library tests, 49 architecture tests and 11 fuzz-target tests, 0 failed. A test that
    fails is named in the log by `dotnet test`, as the control's Release logs name theirs, and none is named. So the
    totals with 0 failed stand for each test below. No per-test listing is retained.
  - *A witness* is counted by the standard E-3 is read on: a planted defect that the test refuses on every run, in the
    test itself, counts as its witness watched failing. A witness outside the test counts only by a retained run in
    which its test failed.

| Check | The tests that decide it | Result at the reading | Its witness |
|---|---|---|---|
| A-1 | `ChargingTests`, `CeilingTests`; `MeterMetadataTests` (architecture tests, the metadata half) | passed | The metadata half: in the test, run every time. `A1_Budget_StepLimit_the_per_start_reset_and_RegexOverflowException_are_absent_from_the_metadata` refuses each name planted in a copy of the library's sources. The charging halves: none. |
| A-2 | `CompileChargingTests`, `AdversarialCorpusTests` (class V) | passed | none |
| A-3 | `CeilingTests` | passed | none |
| A-4 | `MatcherRecursionTests` (architecture tests, the matcher half), `NestingLimitTests`, `NestingMaximumTests`, `StackDepthGuardTests` | passed | The matcher half: in the test, run every time. `A4_A_recursive_helper_planted_in_a_copy_of_the_library_is_refused` plants each shape the graph once missed. The parser's and compiler's nesting check and the stack clauses: none. |
| A-5 | `PropertyNameTests`, `GroupNameTests`, `NamedReferenceTests`, `ValidateEntryTests` | passed | The clause "`\k<name>` to an undeclared name refused by the parser and by the validate entry" has a retained run of its witness failing: part B's plant `deac674`, CI run 44237117988, Release jobs 134575819708 (ubuntu-24.04) and 134575819702 (windows-latest), each "Failed: 19, Passed: 2625" for `Broiler.Regex.Tests.dll`. Among the failures are `NamedReferenceTests.A_reference_to_an_undeclared_name_is_refused_by_the_parse_alone` (five cases under `u` and `v`), `ValidateEntryTests.The_critic_s_k_case_is_refused_by_the_validate_entry` (`u`, `v`) and `ValidateEntryTests.Every_refusal_is_answered_with_its_code_identifier_argument_and_position` (`\k<b>(?<a>x)`, `u`, `UndefinedGroupName`). The revert's run 44238736222 is green. The other clauses: none. |
| A-6 | `DuplicateNamedGroupTests`, `RegexGroupNamesTests` | passed | none |
| A-7 | `PublicEntryTests`, `EntryTests` | passed | none |
| A-8 | `PublicEntryTests`, `EntryTests`, `DuplicateNamedGroupTests`, `RegexGroupNamesTests` | passed | none |
| A-9 | `FlagsTextTests`, `EscapeStringTests`, `PatternSourceTests` | passed | none |
| A-10 | `ProviderDataTests` | passed | none |
| A-11 | `ConcurrentUseTests`, `FirstUseConcurrencyTests`, `ValidateEntryTests`, `EntryTests`, `CompileChargingTests` | passed | none |

  - *So part A reads NOT MET either way.* Read clause by clause, no check has a witness for every clause. Read as one
    witness per check, A-1, A-4 and A-5 have one, and A-2, A-3 and A-6 to A-11 have none.
  - The control's witness run is at the plant `deac674`, not at the reading's commit. The rule asks that each witness
    was "watched failing once", not that it failed at the reading.
  - Its two Release-job logs were first named in the manifest as not kept, as jobs "that do not decide part B". They
    are now retained under `ci/job-logs/`, because they decide A-5's clause.
- **Part B: read MET on the retained runs**: B-1 to B-3 on run 44206505415, and the control on runs 44237117988 and
  44238736222.
- **Part C: read MET on the retained runs**: the probe's adversarial reports and part C's controls job, run
  44206505415.
- **Part D: read MET on the retained runs**: the probe's vector reports, run 44206505415.
- **Part E: read MET on the retained runs**: E-1 to E-7 on run 44206505415's jobs and the local run's logs. For E-6's
  drift control, the assurance job of the control's run. E-3's "a witness each, watched failing once" is read on the
  same standard as part A's table: each witness of SL1 to SL10, C1, C3 and C5 is a file on disk that its test refuses
  on every run.
- **The reference arms: read MET on the retained runs**, on `reference-arms/run/classification/classification.json`
  and both `controls/*/control.json`. That is under the rule as revised: option R, confirmed by the owner, and the
  owner's rulings filed as 16b and 16c. Each revision is weaker evidence by its own header. Three points bear on it:
  - **Class (b)'s vector clause is the session's reading.** It was made after the runs were seen, under the owner's
    direction of 2026-10-18. The MET line asks that "every class (b) difference the specification admits is in a
    vector at a named corpus revision". Class (b) reads "moved into a vector at a named corpus revision when the
    specification admits both answers". The clause is read as applying only where the specification admits both
    answers. The 4,861 P4 stops at a declared policy (rows D10 and B6) are against an answer, or against another stop,
    so they fall outside it. The 15 P1 differences (rows S3 and M4) are vectors at corpus revision 32. *Not taken*:
    reading every policy stop as admitted. That would need vectors for the 4,861 P4 inputs, of which the corpus holds
    none, and the arms would then read NOT MET. The choice is the owner's.
  - **The excluded patterns are a known gap.** Revision 16b says their differences "are in no admitted class and are
    not class (c), and are reported with every reading, outside the judged population". Its owner's note calls the
    exclusion "A known gap, not a verified agreement". This reading reports them: 8,970 differences, not checked
    against the specification and outside the judged population. They are listed apart, as `excluded`, in
    `classified.tsv.gz` (`5dd1a960…`, kept out by digest).
  - **Class (c)'s "both runs are retained" is not met as written.** The rule reads "every other difference is class
    (c), a defect of S, fixed before the candidate reading, after which the whole comparison is run again and both runs
    are retained". The records name one defect of S, `\k` in a class without `u` (718 differences). Task 2.13's
    unjudged run of 2026-11-01 to 2026-11-02 at `86c1377` found it (`differences.tsv.gz` `23aba8d0…`; the roadmap
    holds only that prefix). Task 2.12's `2e35cbb` fixed it. The comparison was then run again: task 2.13's run, taken
    twice at `a113c91`, `differences.tsv.gz` `0a8345fd…`, which this reading's comparison matches byte for byte. The
    manifest names that listing by its digest and keeps it out, as it does the other listings. The pre-fix run is not
    retained at all: the note above says "nothing of
    that run is retained here", by validation principle 5, and no record names where its files are kept. The MET line
    does not name the clause, so the reading above stands on the MET line. Whether the unretained pre-fix run bears on
    it is the owner's to decide.
- **Part A's NOT MET blocks both cut-overs** (risk D10's stop). Nothing else is declared met or accepted. The gate waits
  on part A's witnesses and on the owner. That covers publication (D13); the decisions above, among them the departure
  from the rule's packed-from line; the session's reading of class (b)'s vector clause; class (c)'s unretained pre-fix
  run; and the scratch branch.
  [`docs/roadmap.md`](../../roadmap.md)'s record of 2026-11-18 has the runs, the readings in full and the choices
  taken.

*Dated note, 2026-11-18, part A read a second time at `49d7be1f` (task 4.2 of SJL-11.2; a dated rule file added, no
rule edited; the candidate reading's retained files unchanged).* The note above reads "**Part A: read NOT MET.** The
tests pass, but the rule's MET also needs "each witness was watched failing once"", and ends "**Part A's NOT MET blocks
both cut-overs** (risk D10's stop). Nothing else is declared met or accepted. The gate waits on part A's witnesses and
on the owner." This note adds part A's second reading; it does not rewrite the first, which stays on record as the
candidate reading's.
- **The owner's choice.** Asked on 2026-11-18 how part A should go forward, the owner chose "Take witnesses, re-read
  A": "A dated revision lets part A alone be read again at 49d7be1f, once every missing witness has a retained run of it
  failing. Nothing else in the rule changes. For each check, I plant its defect in a local scratch worktree (no pushes),
  run its witness test, keep the red log and the revert, then read part A again. Parts B to E and the arms keep their
  readings." The question and the options not taken are quoted in full in the revision.
- **The revision** is [`decision-rule-revised-2026-11-18.md`](decision-rule-revised-2026-11-18.md), the first dated
  revision of `decision-rule.md`, which stays unedited. Its only change: part A alone may be read a second time at the
  candidate reading's commit, from the candidate reading's retained test logs and witness runs taken after it, once
  every clause of every check A-1 to A-11 has a retained witness run. It names the clauses (A-1.1 to A-11.3, split at
  the checks' ";") with the tests that decide each, and defines a witness run, the revert and what is read. It was
  committed before any witness run was taken, and written after the candidate reading was seen, so **its decision is
  weaker evidence**.
- **The witness runs** are retained in [`part-a-witnesses/`](part-a-witnesses/), with their own
  [`manifest.json`](part-a-witnesses/manifest.json) of the bundle manifest's shape, every file by SHA-256 and the
  files over 2 MB by digest only. Each clause the candidate reading left without a witness had a plant made in the
  library's sources alone, in a throwaway copy of `git archive 49d7be1f` on disk, and `Broiler.Regex.Tests` built in
  Release from that copy and run, filtered to the clause's deciding test classes, locally on linux-x64; no plant was
  pushed and each copy was deleted after its run. Each attempt's folder holds the plant (`plant.json`, `plant.diff`),
  the SHA-256 listing of the copy's library sources, the build and test logs, the trx, the exit codes, the gate's
  records, `failures.json` and a note. The revert is the candidate reading's own test logs, all passing; each copy's
  library sources differ from `49d7be1f`'s listing (`part-a-witnesses/base/`) only in the files its plant names.
- **Every attempt is kept.** A-5.1's and A-5.2's first plants changed `RegexUnicode.ResolveProperty`, which the parser
  reaches only after `ClassifyProperty` has decided the name, and refused nothing: failed attempts. A-4.2's first run
  named two deciding tests failing and then ended with xUnit's fatal `TestPipelineException` before its population was
  run, and A-4.4's first test log is over 2 MB, kept by digest: neither is counted. Each of the four clauses has a
  second attempt that witnesses it.
- **Two clauses take the closest plant, not the whole clause**, as the revision says: A-4.4's "in the Native AOT probe"
  is not a test of the assembly, and its plant is a parser descent that needs more stack than the 1 MiB thread holds,
  which the deciding tests under the JIT are written to catch; A-10.1's "at the version above" is a pin outside the
  library's sources, and its plant is a second package dependency, the half the deciding test reads. Each note says so.

**Part A's second reading, clause by clause**, under the revision (the deciding tests are the revision's table's; the
candidate reading's result is its test logs' totals, 0 failed, read for each test as the note above reads them):

| Clause | Deciding test classes | At the candidate reading | Its witness run, and the deciding tests its log names failing |
|---|---|---|---|
| A-1.1 | `ChargingTests` | passed | `A-1.1/attempt-1`: `ChargingTests.A_back_reference_s_length_is_charged_before_any_of_its_characters_is_compared`, `ChargingTests.Before_the_kth_unit_of_work_at_least_k_units_are_charged_and_never_a_block_more_than_the_work`, `ChargingTests.Each_character_a_fast_path_or_the_scan_examines_is_charged_before_it_is_read`, `ChargingTests.The_first_charge_comes_before_the_first_instruction` |
| A-1.2 | `ChargingTests` | passed | `A-1.2/attempt-1`: `ChargingTests.A_non_matching_subject_of_n_characters_charges_at_least_n_units_and_its_end_nothing_under_the_scan`, `ChargingTests.One_call_charges_across_all_its_start_positions_with_no_reset` |
| A-1.3 | `CeilingTests`, `ChargingTests` | passed | `A-1.3/attempt-1`: `CeilingTests.A_catastrophic_case_with_a_step_ceiling_answers_LimitExceeded_naming_steps_and_never_a_match_or_no_match`, `CeilingTests.The_step_ceiling_is_reached_at_the_unit_past_it`, `ChargingTests.A_scan_stopped_by_its_step_ceiling_has_tested_only_the_positions_it_paid_for`, `ChargingTests.A_step_ceiling_of_k_units_stops_the_call_before_the_instruction_that_would_pass_it`, `ChargingTests.An_anchored_call_makes_no_scan_and_a_scan_stops_at_the_step_ceiling` |
| A-1.4 | `ChargingTests` | passed | `A-1.4/attempt-1`: `ChargingTests.A_meter_that_throws_at_its_budget_propagates_and_the_program_answers_a_fresh_call_as_a_new_one_does` |
| A-1.5 | `MeterMetadataTests` | passed | The candidate reading's: in the test, run every time (architecture tests). No new plant. |
| A-2.1 | `CompileChargingTests` | passed | `A-2.1/attempt-1`: `CompileChargingTests.Each_place_charges_at_least_one_unit_per_range_and_per_string_its_expansion_touches`, `CompileChargingTests.Each_range_copied_from_the_provider_is_charged_before_it_is_read` |
| A-2.2 | `AdversarialCorpusTests`, `CompileChargingTests` | passed | `A-2.2/attempt-1`: `AdversarialCorpusTests.A_row_the_new_backend_runs_answers_its_declared_status_in_a_child_process`, `CompileChargingTests.A_compile_budget_admits_the_work_it_covers_and_refuses_one_unit_less`, `CompileChargingTests.A_compile_budget_of_zero_is_a_broken_precondition_and_the_constructors_take_neither_budget_nor_meter`, `CompileChargingTests.The_budget_and_the_nesting_limit_each_answer_their_own_dimension_in_the_order_the_work_meets_them` |
| A-3.1 | `CeilingTests` | passed | `A-3.1/attempt-1`: `CeilingTests.The_frame_ceiling_is_reached_one_frame_past_what_the_match_holds_at_once`, `CeilingTests.The_trail_ceiling_is_reached_one_cell_past_the_writes_the_match_has_not_undone` |
| A-3.2 | `CeilingTests` | passed | `A-3.2/attempt-1`: `CeilingTests.A_ceiling_reached_in_a_look_around_or_a_back_reference_names_itself_and_is_never_taken_for_the_body_s_failure`, `CeilingTests.A_negative_look_around_whose_body_reaches_a_ceiling_answers_LimitExceeded_not_a_match`, `CeilingTests.The_trail_ceiling_is_reached_one_cell_past_the_writes_the_match_has_not_undone`, `CeilingTests.With_the_fast_paths_every_ceiling_below_what_a_call_needs_names_itself` |
| A-4.1 | `MatcherRecursionTests` | passed | The candidate reading's: in the test, run every time (architecture tests). No new plant. |
| A-4.2 | `NestingLimitTests`, `NestingMaximumTests` | passed | `A-4.2/attempt-1`: not counted, as its note says; `A-4.2/attempt-2`: `NestingLimitTests.A_class_adds_a_level_under_v_only`, `NestingLimitTests.A_limit_admits_its_own_depth_and_refuses_one_level_more`, `NestingLimitTests.Every_kind_of_level_counts_toward_one_depth`, `NestingLimitTests.Quantifiers_sequences_and_alternatives_add_no_level`, `NestingLimitTests.The_members_that_take_no_limit_parse_under_the_maximum_and_refuse_past_it_as_a_syntax_error`, `NestingMaximumTests.One_level_past_the_maximum_is_refused_on_a_1_MiB_stack`, `NestingMaximumTests.The_shapes_are_listed_once_and_nest_to_the_depth_they_are_built_for` |
| A-4.3 | `NestingLimitTests` | passed | `A-4.3/attempt-1`: `NestingLimitTests.The_limit_lies_between_1_and_the_documented_maximum` |
| A-4.4 | `NestingMaximumTests`, `StackDepthGuardTests` | passed | `A-4.4/attempt-1`: not counted, as its note says; `A-4.4/attempt-2`: `NestingMaximumTests.A_pattern_at_the_maximum_matches_on_a_1_MiB_stack`, `NestingMaximumTests.A_pattern_at_the_maximum_of_a_shape_the_old_matcher_did_not_prove_matches_on_a_1_MiB_stack`, `NestingMaximumTests.A_pattern_at_the_maximum_parses_and_compiles_on_a_1_MiB_stack` |
| A-5.1 | `PropertyNameTests` | passed | `A-5.1/attempt-1`: a failed attempt, no test failed; `A-5.1/attempt-2`: `PropertyNameTests.Every_name_and_value_the_tables_admit_is_accepted`, `PropertyNameTests.Katakana_Or_Hiragana_is_a_value_no_code_point_has`, `PropertyNameTests.The_probe_cases_get_the_specification_answer` |
| A-5.2 | `PropertyNameTests` | passed | `A-5.2/attempt-1`: a failed attempt, no test failed; `A-5.2/attempt-2`: `PropertyNameTests.A_lone_name_spelled_otherwise_is_refused` |
| A-5.3 | `GroupNameTests` | passed | `A-5.3/attempt-1`: `GroupNameTests.Every_refused_name_is_a_syntax_error_in_every_mode`, `GroupNameTests.The_probe_cases_get_the_specification_answer` |
| A-5.4 | `GroupNameTests` | passed | `A-5.4/attempt-1`: `GroupNameTests.A_braced_escape_never_joins_a_surrogate_pair` |
| A-5.5 | `DecimalEscapeTests` | passed | `A-5.5/attempt-1`: `DecimalEscapeTests.Nul_before_a_digit_that_is_not_ASCII_is_NUL_then_that_digit` |
| A-5.6 | `NamedReferenceTests`, `ValidateEntryTests` | passed | The candidate reading's: part B's control run 44237117988, Release jobs 134575819708 and 134575819702 (`candidate-reading/ci/job-logs/`). No new plant. |
| A-5.7 | `DecimalEscapeTests`, `NamedReferenceTests`, `ValidateEntryTests` | passed | `A-5.7/attempt-1`: `DecimalEscapeTests.Under_u_and_v_a_legacy_octal_escape_is_a_syntax_error`, `ValidateEntryTests.An_Annex_B_extension_refused_under_u_or_v_has_its_own_code_of_row_EG0002` |
| A-6.1 | `DuplicateNamedGroupTests`, `RegexGroupNamesTests` | passed | `A-6.1/attempt-1`: `DuplicateNamedGroupTests.A_name_repeated_where_both_groups_might_participate_is_refused_at_the_later_group`, `DuplicateNamedGroupTests.The_rule_agrees_with_MightBothParticipate_over_a_seeded_population_of_trees` |
| A-6.2 | `DuplicateNamedGroupTests` | passed | `A-6.2/attempt-1`: `DuplicateNamedGroupTests.A_reference_to_a_duplicated_name_is_one_instruction_reading_every_group_of_the_name_at_one_unit`, `DuplicateNamedGroupTests.A_reference_to_a_duplicated_name_matches_the_group_that_participated` |
| A-6.3 | `DuplicateNamedGroupTests`, `RegexGroupNamesTests` | passed | `A-6.3/attempt-1`: `DuplicateNamedGroupTests.The_participating_group_comes_from_the_offset_entry_s_slots`, `RegexGroupNamesTests.The_participating_group_is_read_from_the_offset_entry_s_slots` |
| A-7 | `PublicEntryTests` | passed | `A-7/attempt-1`: `PublicEntryTests.The_anchored_argument_is_independent_of_the_y_flag` |
| A-8 | `DuplicateNamedGroupTests`, `EntryTests`, `PublicEntryTests`, `RegexGroupNamesTests` | passed | `A-8/attempt-1`: `EntryTests.A_call_allocates_nothing_that_grows_with_the_capture_count`, `EntryTests.A_call_through_task_2_4b_s_instructions_allocates_nothing_that_grows_with_the_capture_count`, `PublicEntryTests.A_call_allocates_nothing_that_grows_with_the_capture_count`, `PublicEntryTests.A_call_allocates_nothing_up_to_the_frames_and_trail_cells_the_pool_keeps_and_past_them_its_own_arrays` |
| A-9.1 | `FlagsTextTests` | passed | `A-9.1/attempt-1`: `FlagsTextTests.A_refusal_is_decided_by_the_ninth_code_unit_whatever_the_length`, `FlagsTextTests.A_refusal_is_the_syntax_status_with_the_validate_entry_s_diagnostic`, `FlagsTextTests.A_refusal_names_its_kind_and_the_code_unit`, `FlagsTextTests.Every_string_of_up_to_three_code_units_over_the_flags_and_strangers_reads_as_the_specification_reads_it`, `FlagsTextTests.The_throwing_reader_answers_as_before_through_the_same_grammar` |
| A-9.2 | `EscapeStringTests` | passed | `A-9.2/attempt-1`: `EscapeStringTests.Broiler_VM_s_correspondence_row_T5_answers_as_both_engines_did`, `EscapeStringTests.Each_class_the_specification_names_is_escaped_as_it_says`, `EscapeStringTests.Every_code_point_after_the_first_is_encoded_as_EncodeForRegExpEscape_encodes_it`, `EscapeStringTests.Every_code_point_first_is_escaped_as_the_leading_rule_and_EncodeForRegExpEscape_say`, `EscapeStringTests.Test262_s_RegExp_escape_cases_answer_as_test262_expects` |
| A-9.3 | `PatternSourceTests` | passed | `A-9.3/attempt-1`: `PatternSourceTests.Broiler_VM_s_correspondence_rows_T1_to_T4_answer_as_the_specification_and_the_staging_test_ask`, `PatternSourceTests.Classes_escapes_and_groups_are_read_as_a_literal_reads_them`, `PatternSourceTests.Every_code_unit_after_an_escaped_bracket_is_outside_a_class`, `PatternSourceTests.Every_code_unit_alone_is_itself_but_the_solidus_and_the_line_terminators`, `PatternSourceTests.Every_code_unit_in_five_shapes_holds_the_specification_s_properties_in_every_mode`, `PatternSourceTests.Every_pattern_of_the_test_project_s_string_literals_holds_the_specification_s_properties`, `PatternSourceTests.Test262_s_staging_escape_js_cases_answer_as_test262_expects`, `PatternSourceTests.The_meter_is_paid_in_whole_blocks_before_each_pass_and_a_pattern_that_needs_no_change_costs_its_reading` |
| A-10.1 | `ProviderDataTests` | passed | `A-10.1/attempt-1`: `ProviderDataTests.The_library_references_the_provider_alone_and_carries_no_table_or_data_version` |
| A-10.2 | `ProviderDataTests` | passed | `A-10.2/attempt-1`: `ProviderDataTests.The_library_references_the_provider_alone_and_carries_no_table_or_data_version` |
| A-10.3 | `ProviderDataTests` | passed | `A-10.3/attempt-1`: `ProviderDataTests.A_match_reads_its_case_folding_from_the_callers_tables`, `ProviderDataTests.A_pattern_of_each_kind_reads_the_tables_through_the_callers_provider` |
| A-10.4 | `ProviderDataTests` | passed | `A-10.4/attempt-1`: `ProviderDataTests.The_library_references_the_provider_alone_and_carries_no_table_or_data_version` |
| A-11.1 | `GroupNameTests`, `ValidateEntryTests` | passed | `A-11.1/attempt-1`: `GroupNameTests.An_escaped_surrogate_in_a_name_is_read_or_refused_never_thrown`, `ValidateEntryTests.The_validate_entry_answers_as_Compile_over_the_suite_and_seeded_populations` |
| A-11.2 | `CompileChargingTests`, `ConcurrentUseTests`, `EntryTests`, `FirstUseConcurrencyTests` | passed | `A-11.2/attempt-1`: `ConcurrentUseTests.Concurrent_first_compiles_and_matches_of_a_fresh_library_answer_as_one_thread_does`, `ConcurrentUseTests.Warm_concurrent_compiles_and_matches_answer_as_one_thread_does`, `EntryTests.One_program_shared_by_concurrent_calls_answers_each_as_it_answers_alone` |
| A-11.3 | `ValidateEntryTests` | passed | `A-11.3/attempt-1`: `ValidateEntryTests.A_flags_value_with_u_and_v_is_the_syntax_status_and_an_undefined_bit_a_broken_precondition`, `ValidateEntryTests.No_public_type_is_an_exception_and_the_constructors_refuse_with_an_ArgumentException` (its log also names `ValidateEntryTests.Every_refusal_is_answered_with_its_code_identifier_argument_and_position` failing, a test the revision's A-5.6 row names and its A-11.3 row's exclusion leaves to A-5.6: the dated note on review below) |

- **Part A: read MET on the retained runs**, at its second reading at `49d7be1f`, under the revision of 2026-11-18:
  every check passes at the reading (the candidate reading's logs, unchanged), and every clause of A-1 to A-11 has a
  retained witness run, 32 of them taken after the candidate reading and 3 the candidate reading found. The revision's
  decision is weaker evidence, as its header says. Parts B to E and the reference arms keep their candidate readings,
  unchanged.
- **What it bears on**, in the rule's words and the revision's. The rule reads "A part NOT MET at the candidate reading
  blocks both cut-overs (risk D10's stop)", unchanged. The revision reads: "For part A alone, the reading that line
  reads at the candidate reading's commit is, from this file's date, part A's second reading: if it reads MET, part A
  no longer blocks the cut-overs by that line". So part A no longer blocks the cut-overs by that line; parts B to E read
  MET at the candidate reading. That is all it lifts: the revision also says "Nothing else of either engine's cut-over
  is decided here", and the rule's "What this file does not decide" names "**Either engine's cut-over**". The
  cut-overs still wait on their own holds, H1 and H2, which this bundle does not decide, and on the owner's open items:
  publication (D13); the departure from the rule's packed-from line (the reading's commit); the session's reading of
  class (b)'s vector clause; class (c)'s unretained pre-fix run; and the scratch branch `sjl-11-2-control-b`'s
  deletion. Nothing else is declared met or accepted.
  [`docs/roadmap.md`](../../roadmap.md)'s record of 2026-11-18 on part A's witnesses has the runs and the choices taken.

*Dated note, 2026-11-18, on review of part A's second reading (the revision, every rule file and every retained file
unchanged; the reading unchanged).* Two places in the note above are corrected, each quoted here, and the review's
other findings are recorded for the owner.
- **A-11.3's deciding tests.** The table above read, in A-11.3's last cell, "`A-11.3/attempt-1`:
  `ValidateEntryTests.A_flags_value_with_u_and_v_is_the_syntax_status_and_an_undefined_bit_a_broken_precondition`,
  `ValidateEntryTests.Every_refusal_is_answered_with_its_code_identifier_argument_and_position`,
  `ValidateEntryTests.No_public_type_is_an_exception_and_the_constructors_refuse_with_an_ArgumentException`". The
  revision's A-11.3 row names its deciding tests as "`ValidateEntryTests`, every test not named above for A-5.6, A-5.7
  or A-11.1", and its A-5.6 row names `ValidateEntryTests.Every_refusal_is_answered_with_its_code_identifier_argument_and_position`;
  the same A-11.3 row then lists that test "among them". The row contradicts itself. By its exclusion clause that test
  decides A-5.6, not A-11.3, and it is not counted for A-11.3 here. A-11.3 is witnessed by the two other deciding tests
  its run's log names failing, `ValidateEntryTests.A_flags_value_with_u_and_v_is_the_syntax_status_and_an_undefined_bit_a_broken_precondition`
  and `ValidateEntryTests.No_public_type_is_an_exception_and_the_constructors_refuse_with_an_ArgumentException`, neither
  named for A-5.6, A-5.7 or A-11.1; the cell now says so. The attempt's retained `note.md` and `failures.json`, and
  `part-a-witnesses/scripts/deciding.py`, whose A-11.3 exclusion leaves that test out, count it for A-11.3; they stay
  unedited as retained files, and this note is how they are read. The row's wording is an item for the owner.
- **What it bears on.** The bullet above read "So part A no longer blocks the cut-overs by that line, and no part read
  at the candidate reading does." Part A's candidate reading is NOT MET and stays on record, and the revision points
  the line at part A's second reading only; neither rule file says that no part blocks. It now reads "So part A no
  longer blocks the cut-overs by that line; parts B to E read MET at the candidate reading."
- **The blocking line, for the owner.** The revision lists "A part NOT MET at the candidate reading blocks both
  cut-overs (risk D10's stop)" under "What it does not change", and under "How it is read" says that, for part A
  alone, "the reading that line reads at the candidate reading's commit is, from this file's date, part A's second
  reading". Read literally, the unchanged line still reads the candidate reading's NOT MET; the lift rests on the
  revision's reading of that line, which its "What it revises" does not list. The owner's question framed part A's NOT
  MET as what blocks the cut-overs, and the choice was to read part A again, so this note reads the lift as the
  owner's intent. Whether the owner confirms it, and whether a later dated note names it among the revisions, is the
  owner's.
- **"Not counted", for the owner.** The revision reads "a clause is witnessed if and only if at least one of its
  deciding tests fails in a retained witness run and is named failing in that run's test log", and names no attempt
  "not counted". A-4.2's first attempt names two deciding tests failing before xUnit's fatal error, so by the
  revision's words alone it would witness A-4.2. A-4.4's first test log is kept by its digest only, as the revision's
  "a file over 2 MB by its digest only" allows. Setting both aside, the first by `decision-rule.md`'s "a step that
  stopped before the end of its population is void" and the second because its log is not kept in the bundle, is the
  session's own stricter reading, beyond the revision's text, made after the runs were seen; it is not a category the
  revision defines. Each of the two clauses is witnessed by its second attempt either way.
- **Guarantees with no plant of their own, for the owner.** The unit is the ";"-separated clause, so a clause that
  states more than one guarantee is witnessed by a plant of one of them. A-1.4: the plant stops the meter's exception
  from propagating, not the fresh call's answer. A-4.4: a pattern at the maximum under the JIT; not the flat sequence
  of part C's class F, whose `StackDepthGuardTests` did not fail, nor the Native AOT probe. A-6.1: the "refused
  otherwise" half, not the "accepted" half. A-8: the allocation half;
  `PublicEntryTests.The_entry_s_signature_carries_no_string_and_answers_a_status` did not fail. A-9.1: the duplicate
  flag, not the unknown flag or u with v. A-10.1: the second dependency, not the version pin. By the revision's
  definition the retained runs witness these clauses, and not each guarantee in them; the evidence for the halves
  listed is weaker.
- **A result in the revision, for the owner.** Its header reads "**It holds no figure**", and its section "The
  clauses" reads "Every test named passed at the reading: the candidate reading's logs carry totals per assembly, 0
  failed". That is a result of the candidate reading, which `decision-rule.md`'s "Counts are results, not rules" puts
  in this README. The revision stays unedited, since it was committed before the runs.
- **Files kept by digest only.** A-4.4's first test log and trx and its second trx are named in
  `part-a-witnesses/manifest.json` at the session's scratch directory, which a restart can clear; their digests are
  recorded there. The retained test logs and scripts of `part-a-witnesses/` name that scratch path, as produced. None
  of these decides a clause.

*Dated note, 2026-11-18, the owner's ruling on the blocking line (no rule file and no retained file changed; part A's
second reading unchanged).* The note on review above names, among the owner's wording items, "the blocking line, which
the revision lists as unchanged while it points the line, for part A, at the second reading", read there "as the
owner's intent". The owner was asked: "Part A of the Broiler.Regex reading now reads MET at its second reading (32
witnesses taken, 3 already on record, none missing). But the 2026-11-18 revision kept the rule's line "A part NOT MET
at the candidate reading blocks both cut-overs" unchanged, and the candidate reading's part A NOT MET stays on record.
Read strictly, the block would never lift. Should part A's MET at its second reading lift that block for both
cut-overs (SJL-11.5 in Broiler.JS, SJL-11.4 in Broiler.VM)?" The owner answered "Yes, the second reading lifts it" —
"A dated note records your reading: the block on both cut-overs is lifted as far as part A goes. The cut-overs still
wait on their own holds (H1, H2), their base runs, and your open items." The alternative not taken was "No, keep the
block" — "Both cut-overs stay blocked by the candidate reading's part A NOT MET. Only the end reading or a further
revision could lift the block." This is the owner's own ruling, not one taken under the owner's direction of
2026-10-18. By it, part A's MET at its second reading lifts the block of "A part NOT MET at the candidate reading
blocks both cut-overs (risk D10's stop)" on both cut-overs, as far as part A goes; that wording item is answered.
Nothing else is decided: SJL-11.5 in Broiler.JS and SJL-11.4 in Broiler.VM still wait on their own holds, H1 and H2,
their base runs, and the owner's other open items.

## No figure

This bundle's rules hold no figure: where a count matters it is a parameter of the protocol. Results are written
here at each reading and nowhere else (the programme's validation principle 5).

## Exclusions

| Identifier | What it excludes | Why |
|---|---|---|
| (none) | | |
