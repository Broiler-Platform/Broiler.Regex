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

## No figure

This bundle's rules hold no figure: where a count matters it is a parameter of the protocol. Results are written
here at each reading and nowhere else (the programme's validation principle 5).

## Exclusions

| Identifier | What it excludes | Why |
|---|---|---|
| (none) | | |
