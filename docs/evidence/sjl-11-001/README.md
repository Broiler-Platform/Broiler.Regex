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

## The commits named so far

- **The rule commit:** the commit that adds this bundle, `tests/Adversarial/` and `eng/adversarial-programs.py`, on
  the branch that lands SJL-11.1 here. Its identifier is recorded in this README when it reaches the default
  branch.

No other commit is named yet.

## No figure

This bundle's rules hold no figure: where a count matters it is a parameter of the protocol. Results are written
here at each reading and nowhere else (the programme's validation principle 5).

## Exclusions

| Identifier | What it excludes | Why |
|---|---|---|
| (none) | | |
