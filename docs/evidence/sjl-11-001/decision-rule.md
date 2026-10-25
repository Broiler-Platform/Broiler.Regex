# Bundle SJL-11-001's decision rule: the component clauses of the regular-expression area - the requirements R1 to R11 as a contract, test262 in this repository's CI, the adversarial corpus under JIT and Native AOT, the vectors at library level, and the process

**Written:** 2026-10-20, at milestone SJL-11 (work package SJL-11.1) of the shared JavaScript libraries programme,
with the dated record of this repository's `docs/roadmap.md` section 6 that adopts R1 to R11 as owner conditions, and
before any code of SJL-11.2 exists in this repository: no meter, no new backend, no provider, no validate entry.
Committed in the same change: `tests/Adversarial/corpus.tsv` and `eng/adversarial-programs.py`. **Milestone judged:**
SJL-11, `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-11-regexp.md`: its component clauses 1 ("Every
requirement of SJL-11.2 is met"), 2 ("test262's `RegExp` directories run in the component's CI") and 3 ("The
adversarial corpus shows no silent exhaustion and no shortened match"), and the vectors at library level (template
T3). **Owner:** the Broiler.RegEx maintainer. **Reviewer:** none. Every role this rule names, both engines' owners
included, is held by one person (the programme records this as Broiler.VM's exception EX-30), and this rule does not
claim that anyone independent has read it.

*Revised 2026-10-25, before any run and before this change reached the default branch, to agree with Broiler.VM's
JSD-0081 as filed: check A-5 adds row S10's escapes, which that record's requirement table places under R5 and its
decision D36 among SJL-11.2's fixes; nothing else changed. Revised again the same day, after review and still before
any run: a section "How it is read" is added, which the programme's validation document asks of every rule (section
4); no rule block changed.*

**What this file is.** Five predeclared decisions, one per part. Part A is the contract (R1 to R9 and R11), each
check with a witness. Part B is clause 2. Part C is clause 3, at library level, in both execution modes. Part D is
template T3 at library level. Part E is R10. The reference-arm comparison (template T4) is
[`reference-arms.md`](reference-arms.md) beside this file. **It holds no figure**; counts and seeds below are
parameters.

**The area's record** is Broiler.VM's JSD-0081, and its input is Broiler.VM's `docs/evidence/sjl-11-001/correspondence.md`.

**The readings.**
- **The candidate reading** is taken at the commit `0.2.0-preview.1` is packed from. **A part NOT MET there blocks both
  cut-overs** (the programme's `risks.md` D10: "an engine cut over to Broiler.Regex before every requirement of
  SJL-11.2 holds" is a stop).
- **The end reading** is taken at the commit both engines pin after their cut-overs, if it differs.
Every part is judged at both.

**The pins.** test262 `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`, checked by its tree hash; `Broiler.Unicode.Algorithms`
`0.1.0-preview.5`, the version both engines admit, and `Broiler.Unicode.Tables` `0.1.0-preview.5` for tests and the
probe only; the vectors' copy under `tests/Vectors/` by its pin file.

## Part A - the contract (clause 1: R1 to R9 and R11)

```rule
evidence class: library contract (not a measurement)
population: every public member of the package Broiler.Regex at the reading's commit
checks, each a test in Broiler.Regex.Tests or tests/Regex.Architecture.Tests, each with a witness that was watched failing once:
  A-1 R1, the budget: every operation whose work grows with its input takes a nullable RegexWorkCharge and charges before the work it pays for, in the blocks JSD-0081's decision D8 documents: a recording meter shows, for a declared set of inputs, that before the k-th instruction runs at least k units are charged; one call charges across all its start positions with no reset (a non-matching subject of n positions charges at least n units); with a step ceiling, every catastrophic case of part C returns LimitExceeded naming steps, never NoMatch or Match; a meter that throws at its budget propagates its exception, and the same compiled instance then answers a fresh call as a new instance does; Budget, StepLimit and the per-start reset are absent from the assembly's metadata
  A-2 R2, compilation: compile charges the same meter in proportion to the work, at least one unit per range and per string an expansion touches, for a declared set of v-mode and case-insensitive expansions; a compile budget refuses every case of part C's class V
  A-3 R3, memory: the frame and trail ceilings are the caller's; a test reaches each and is answered LimitExceeded naming it
  A-4 R4, nesting: no method of the matcher's namespace reaches itself, directly or through a cycle, in the IL call graph; the parser and the compiler check the caller's nesting limit before each descent and answer the nesting status past it; a limit above the documented maximum is refused; a pattern at the maximum, and a flat sequence of part C's class F, complete on a thread of the smallest declared stack under the JIT and in the Native AOT probe
  A-5 R5, exact syntax: every property name and value ECMA-262's three tables and PropertyValueAliases.txt admit, Katakana_Or_Hiragana and Hrkt included, accepted under u and v; the refused set - each name with its letter case changed, an underscore removed, a space or hyphen inserted, an Is prefix, or as the value of the wrong property - refused; group names validated by EcmaScriptLexical at UCD 17.0.0, raw, escaped and as surrogate pairs, in every mode; a four-digit lead-surrogate escape followed by a braced escape accepted under u and v and read as two characters, a braced escape never joining a pair (row S10 of the correspondence); \0's lookahead ASCII; \k<name> to an undeclared name refused by the parser and by the validate entry; Annex B syntax only without u and v
  A-6 R6, duplicate named groups: a name repeated only in alternatives that cannot both participate accepted, and refused otherwise, after escapes are decoded; \k<name> matches the participating group; a result maps the name to the participating index
  A-7 R7: an anchored argument, independent of the y flag
  A-8 R8: an offset-only interface whose signatures carry no string result (the API baseline), and whose call allocates nothing that grows with the capture count
  A-9 R9: flag parsing returns a status for a duplicate, an unknown flag and u with v; RegExp.escape's text escapes exactly the specification's EncodeForRegExpEscape set over EcmaScriptLexical's WhiteSpace and LineTerminator; source escaping follows EscapeRegExpPattern
  A-10 R11, data: the package's dependencies are Broiler.Unicode.Algorithms alone, at the version above; CaseFolding, CaseFoldingData, the generator and the Broiler.UniCode packages are absent; a recording IUnicodeTables sees every table the library reads; the library declares no data version of its own
  A-11 the conventions the contract rests on: no exception for an input its contract admits (the escaped group names that throw ArgumentOutOfRangeException from RegexParser.ReadGroupName in 0.1.0-preview.4's code included); concurrent compile and match on shared instances answer as single-threaded calls do; a validate entry returning a status, a code, a message identifier and a position
MET if and only if every check passes at the reading, and each witness was watched failing once.
NOT MET otherwise.
```

## Part B - test262 in this repository's CI (clause 2)

```rule
evidence class: conformance in CI (not a comparison of runs)
population: test262 at the pin, the paths test/built-ins/RegExp, test/language/literals/regexp, test/annexB/built-ins/RegExp, test/annexB/language/literals/regexp, test/built-ins/RegExpStringIteratorPrototype and test/built-ins/String/prototype/{match,matchAll,replace,replaceAll,search,split}
checks:
  B-1 the host job: .github/workflows/ci.yml's job test262-host checks out Broiler.JS at the forty-digit commit this repository's docs/roadmap.md names by a dated record, packs this commit's Broiler.Regex as the version that commit pins into a local feed listed first, builds BroilerJS in Release against it, and runs scripts/compliance/run_test262.py with --include-negative over the population, original variant; green if and only if no row fails or times out but those in test262/host-expected-failures.txt, each entry naming its path, the host member it fails in and why that member is the host's (the RegExp object, the protocols, species, GetSubstitution's evaluation, lastIndex), and no entry is attributed to pattern syntax, compilation or matching; and every row of a file listing regexp-duplicate-named-groups passes
  B-2 the library job: test262-library runs this repository's runner over (i) every file under the two literal directories whose regular-expression literals the runner's declared grammar extracts - a negative syntax test's literal refused by the validate entry and by compile, a positive file's literals accepted - (ii) test/built-ins/RegExp/property-escapes/generated through its structured helpers, every listed code point matching and every other code point not, and (iii) test/built-ins/RegExp/unicodeSets/generated likewise; a file it cannot read is listed in test262/library-unreadable.txt with why; under the JIT and through the Native AOT probe, the two reports byte-identical
  B-3 both jobs run on the default branch at the reading's commit and are green
controls: a scratch commit of this repository, never merged, that accepts \k<name> to an undeclared name under u; B-1 or B-2 must turn red; reverted
MET if and only if B-1 to B-3 hold at the reading and the control turned a job red.
NOT MET otherwise.
```

## Part C - the adversarial corpus under JIT and Native AOT (clause 3)

```rule
evidence class: resource accounting at library level (not a measurement)
population: every row of tests/Adversarial/corpus.tsv as committed with this rule: classes K catastrophic backtracking, A alternation without quantifiers, L look-around and lookbehind, G a greedy run longer than the old allowance, W exhaustion inside one alternative, N nesting (the limit, one past it, and depths far past it, in groups, non-capturing groups and v-mode classes), F a flat sequence of many terms, M frames and trail, V large v-mode and case-insensitive expansions, O ordinary patterns; each row a pattern, flags, a subject built by a declared expression, an operation (compile, validate, or match at an index, anchored or not), a policy (meter budget or none, step, frame and trail ceilings, nesting limit, compile budget) and a declared status (a match with its offsets, no match, LimitExceeded naming its dimension, the meter's exception, the nesting status, a syntax status); a row declared exhausted needs, by regexpref's count of the specification's matcher steps, at least a hundred times its budget, and a row declared to match completes within a hundredth of it
configuration: probes/Regex.Probe --adversarial, built under the JIT and published with Native AOT under Broiler.VM's root settings, on ubuntu-latest and windows-latest, each row in a child process on a thread of the smallest stack the corpus's header declares; at the reading's commit; each child's exit and status line retained
controls: (1) a scratch build in which LimitExceeded naming steps is answered NoMatch: every K, A, L, W and G row must be reported; (2) a scratch build without the nesting check: every N row past the limit must end abnormally or differ, and be reported; each reverted and the revert checked
comparison: per row, mode and RID, the status against the declared one; per RID, the JIT report against the Native AOT report, byte for byte
admitted difference classes: none
MET if and only if every row gives its declared status in both modes on both RIDs, no child process ends abnormally, the two modes' reports are byte-identical per RID, and both controls are reported.
NOT MET otherwise.
```

## Part D - the vectors at library level (template T3)

```rule
evidence class: conformance at library level (not a measurement)
population: every vector of conformance/vectors/regexp/*.vectors at the corpus revision bundle sjl-11-001 names, copied into tests/Vectors/regexp/ with a pin file naming Broiler.JS's commit and each file's SHA-256, checked before the run
configuration: the library runner in Broiler.Regex.Tests, mapping each op to the library (compile and validate, exec at a lastIndex under the flags, the helpers), under the JIT and through the Native AOT probe
MET if and only if every normative vector whose op the library implements gives its out, every status vector gives its status, every impl-defined vector is reported with the library's answer, the two modes' reports are identical, and every op the library does not implement (the object model's) is listed by name.
NOT MET otherwise.
```

## Part E - the process (R10, the conventions, SJL-D-5 (a))

```rule
evidence class: process record (not a measurement)
checks:
  E-1 IsAotCompatible and IsTrimmable declared, the trim, AOT and single-file analyzers reporting nothing, as errors; no suppression
  E-2 the Native AOT probe in CI publishes with Broiler.VM's root settings and no warning, leaves no managed assembly in the publish directory, and runs parts C and D's populations with output equal to the JIT's
  E-3 SL1 to SL10 Active in tests/Regex.Architecture.Tests with a register and a witness each, watched failing once; docs/conventions.md the programme's numbering; the starting violation list empty
  E-4 the API baseline compared in both directions; package validation against 0.1.0-preview.4 with each suppression listed in docs/suppressions.md and in CHANGELOG.md under Changed with its R item; CHANGELOG.md with Observable changes and Data headings
  E-5 the fuzz target tests/Regex.Fuzz runs in CI for its declared seed and iteration count with every limit and a meter set: no exception but statuses and the meter's, no process end; a planted throw in a declared parser path is found by the declared seed
  E-6 the assurance gate: every relevant unit annotated; the artefacts generated by Broiler.Code's command-line tool; CI fails on drift, watched failing once; HUMAN_REVIEW.md names the revision it reviews, or says that none is reviewed
  E-7 .gitattributes LF, no preprocessor directive, net10.0, nullable enabled, implicit usings disabled
MET if and only if every check holds at the reading.
NOT MET otherwise; and a check not holding at the end of SJL-11.2 refuses the area by JSD-0081's refusal path.
```

## How it is read

- **Each reading is judged on its own.** Parts A to E are judged at the candidate reading from the logs retained for
  it, and again at the end reading, if there is one, from its own logs; a part MET at one reading says nothing of the
  other. A part NOT MET at the candidate reading blocks both cut-overs (risk D10's stop), and a check of part E not
  holding at the end of SJL-11.2 refuses the area.
- **Each check is decided by a retained file, named in the bundle's manifest by its SHA-256**; a file the manifest
  does not name decides nothing, and a step that stopped before the end of its population is void and is not judged.
  `run-library-evidence.sh` writes one log per step, each step's command its first line.
  - **Part A**: each check by its test's result in the test log of the reading, and by the retained run of its witness
    failing once; a check whose witness was not seen failing is not met.
  - **Part B**: the two CI jobs' runs at the reading's commit, named by their run identifiers, read with
    `test262/host-expected-failures.txt` and `test262/library-unreadable.txt` as that commit holds them; and the
    control's scratch commit with the run it turned red.
  - **Part C**: each child's exit and status line per row, mode and RID; the two modes' reports per RID, compared by
    their digests; and each control's run with its revert.
  - **Part D**: the runner's report under each mode, with the pin file's check, which comes before the run.
  - **Part E**: each check by its own record: the build's analyzer output, the probe's publish log, the architecture
    tests' result with their register, package validation's output with `docs/suppressions.md`, the fuzz job's run,
    the assurance job's run and its drift control, and the repository's settings files at the reading's commit.
- **Counts are results, not rules.** Where a check names a count or a seed, it is a parameter; what a reading finds is
  written in the bundle's README at that reading, and nowhere in this file.

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory, quoting this one whole and
saying it was written afterwards; its decision is then weaker evidence. Before any code of SJL-11.2 lands, a later
bundle shows this file unchanged.

## What this file does not decide

- **Either engine's cut-over**, test262 parity, fuel parity, measurement or engine-level adversarial corpus.
- **The vectors' expected values**: Broiler.JS's corpus and its reference `regexpref` decide them.
- **A human review**: SJL-D-5 (a) gates a release, not a development step.
- **Performance.** Nothing here is timed.
