# Human review: Broiler.Regex

> **Status: APPROVED FOR PREVIEW.**

> **Dated note, 2026-10-20, at work package SJL-11.1 of the shared JavaScript libraries programme, by the
> Broiler.RegEx maintainer.** The record below is kept as written: the review of Maik Ratzmer (`MaiRat`),
> dated 2026-07-01 and committed in `79b7a33` ("HUMAN_REVIEW.md finalized"). The commit it reviewed,
> `d5b07a00b48a329c75349890214cdb479973352f` (2026-06-27), is an ancestor of `main`; a shallow clone grafted at
> a later commit does not hold it. The source under `Broiler.Regex/` changed after it, in commits up to and
> including `c1422f4` (`c1422f4d824aa28ff6edd4766d56be25ba7c8a15`, 2026-10-11), the commit `0.1.0-preview.4` was
> packed from. By the record's own rule, "Any source change after the reviewed commit invalidates this approval
> until the changed revision is reviewed again", so **no review covers the current code**. `CODE-ASSURANCE.md`
> likewise records no code unit with a human decision.
>
> Under the programme's decision SJL-D-5 (a), as decision D30 of Broiler.VM's JSD-0081 applies it, this package
> may be admitted for development, and a Broiler.VM release that depends on it waits until the revision it
> depends on carries a completed review. This note was added under the owner's decisions of 2026-10-20, taken
> under the owner's direction of 2026-10-18, "Please do all recommended decisions and skip waiting for
> review.", which changes no programme rule. It is not a review and approves nothing. *(Worded on 2026-10-25,
> before this change reached the default branch, as decision D30 of JSD-0081 words its reason: the note had counted
> the commits and files between the two commits, which the programme's validation principle 5 keeps out of its
> records.)*

Broiler.Regex contains substantial AI-assisted implementation. This record documents the
human review decision for the first preview release. The approval is scoped to the
review target and intended preview use below, and does not imply that the component is
free of defects, performance issues, or vulnerabilities.

"Safe" is not an absolute guarantee. Approval means only that the named reviewer found
the specified revision reasonably suitable for the stated preview use, subject to the
recorded limitations and the software license's warranty disclaimer.

## Review target

- **Component:** Broiler.Regex
- **Scope:** The ECMAScript regular-expression parser and matcher, its tests, and its Broiler.JS integration boundary.
- **Release:** First preview
- **Reviewed commit:** `d5b07a00b48a329c75349890214cdb479973352f`
- **Reviewer:** Maik Ratzmer (`MaiRat`)
- **Reviewer contact or profile:** `MaiRat`
- **Review date:** 2026-07-01
- **Intended preview use:** First preview use in Broiler.JS, with the current conservative routing and documented limitations.

Any source change after the reviewed commit invalidates this approval until the changed
revision is reviewed again.

## Required evidence

- [x] Build and automated tests completed; minimum expected command: `dotnet test Broiler.Regex.slnx`.
- [x] Security-sensitive inputs, trust boundaries, file/network access, native interop,
      and code-execution paths were inspected where applicable.
- [x] Dependency and license notices were checked, including inherited upstream code.
- [x] AI-generated or AI-modified code received source-level review; no AI summary was
      accepted as a substitute for reading the relevant code.
- [x] Public APIs, failure behavior, known limitations, and preview compatibility risks
      were assessed.
- [x] Static analysis, dependency/vulnerability scanning, or an explicit reason for
      omitting each was recorded.
- [x] Open findings and residual risks are listed below.

### Evidence and commands

- `git rev-parse HEAD`
  - Result: `d5b07a00b48a329c75349890214cdb479973352f`
- `dotnet test Broiler.Regex.slnx`
  - Result: passed on 2026-07-01.
  - Test result: 33 passed, 0 failed, 0 skipped.
- `dotnet list Broiler.Regex.slnx package --vulnerable --include-transitive`
  - Result: no vulnerable packages were reported for `Broiler.Regex`, `Broiler.Regex.Tests`,
    `UnicodeProperties`, `UnicodeEmoji.StringProperties`, or `UnicodeCldr.LocaleData`
    from the configured NuGet sources.
- Source inspection notes:
  - `Broiler.Regex` is dependency-free in its production project file and does not reference
    `System.Text.RegularExpressions`, the JavaScript runtime, or other external engines.
  - `AllowUnsafeBlocks` is disabled for the production project.
  - The inspected production surface is dominated by string parsing, AST construction,
    character-set checks, and matcher state transitions.
  - No production file, network, process, native interop, or dynamic code-execution surface
    was identified in the reviewed Regex component.
  - The matcher includes a per-match step budget (`StepLimit = 10_000_000`) intended to
    limit catastrophic backtracking.
  - Known feature limitations remain documented in `Broiler.Regex/README.md`, including
    incomplete Unicode property escapes, incomplete `v`-mode set operations, partial
    case-folding, and clarity-first performance.

### Findings and residual risks

- No blocker was identified for the first preview.
- Security-critical findings are considered unlikely in this component because the reviewed
  production code primarily performs string operations and in-memory matcher state updates,
  with no identified file, network, native interop, process, or code-execution boundary.
- Resource-exhaustion risk cannot be ruled out. Regular-expression parsing and matching are
  inherently loop- and backtracking-heavy, and the current implementation prioritizes
  correctness and clarity over optimization. The step budget reduces, but does not fully
  eliminate, the risk of excessive CPU work or allocation pressure from adversarial or very
  large inputs.
- This risk is accepted for the first preview, provided the component remains within the
  current preview scope and is revisited before broader production exposure or use with
  unbounded untrusted input.

## Decision

- [x] **APPROVED FOR PREVIEW** within the intended-use scope above.
- [ ] **APPROVED WITH CONDITIONS** listed below.
- [ ] **NOT APPROVED** for preview use.

**Conditions:** None beyond the intended preview scope and residual risks recorded above.

## Human attestation

I confirm that I am a human developer, that I personally reviewed the revision and
evidence identified above, and that the decision is my own. I understand that this
attestation is a scoped engineering review, not a warranty or a claim that the component
is free of defects or vulnerabilities.

- **Name:** Maik Ratzmer (`MaiRat`)
- **Signature or attributable commit:** Maik Ratzmer (`MaiRat`)
- **Date:** 2026-07-01

AI tooling assisted with assembling this review record and command evidence. The reviewer
identity and approval decision are recorded from the human reviewer's supplied instruction,
not selected independently by the AI tool.
