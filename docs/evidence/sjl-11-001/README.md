# Bundle SJL-11-001 - the regular-expression area: Broiler.RegEx's rules

**Rules only. Nothing here is a verdict.**

**Milestone:** SJL-11 of the shared JavaScript libraries programme, whose milestone file is
`docs/javascript-shared-libraries/milestones/sjl-11-regexp.md` in the Broiler.VM repository. This bundle judges
its component clauses 1 to 3 and the vectors at library level.

**Rules**, written 2026-10-20 at work package SJL-11.1, before any code of SJL-11.2 exists here:
- [`decision-rule.md`](decision-rule.md): part A, requirements R1 to R9 and R11 as a contract (clause 1); part B,
  test262 in this repository's CI (clause 2); part C, the adversarial corpus under the JIT and Native AOT
  (clause 3); part D, the vectors at library level (template T3); part E, the process (R10).
- [`reference-arms.md`](reference-arms.md): the library against test-only copies of both engines'
  pre-cut-over regular expressions (template T4).

**Inputs committed with them:** [`tests/Adversarial/corpus.tsv`](../../../tests/Adversarial/corpus.tsv), with its
[README](../../../tests/Adversarial/README.md), part C's population; and
[`eng/adversarial-programs.py`](../../../eng/adversarial-programs.py), which writes both engines' programs from it.
The engines commit those programs in their own bundles `sjl-11-001`; they are not copied here.

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

**The readings**, named as they come:
- **The candidate reading**, at the commit `0.2.0-preview.1` is packed from. A part NOT MET there blocks both
  cut-overs.
- **The end reading**, at the commit both engines pin after their cut-overs, if it differs.

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
