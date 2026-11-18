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

## No figure

This bundle's rules hold no figure: where a count matters it is a parameter of the protocol. Results are written
here at each reading and nowhere else (the programme's validation principle 5).

## Exclusions

| Identifier | What it excludes | Why |
|---|---|---|
| (none) | | |
