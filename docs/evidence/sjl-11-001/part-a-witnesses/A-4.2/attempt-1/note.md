# Witness run A-4.2, attempt 1

**The clause.** A-4.2: "the parser and the compiler check the caller's nesting limit before each descent and answer the nesting status past it" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The nesting check removed: the caller's limit is never reached by a descent (part C's control 2's line, as probes/Regex.Probe/AdversarialControls.targets declares it, applied to the library's source). Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `d9f489eff3d50d94420fd362b27cca183a858255e33c7ad43b1a0955e1a4f0f3`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Parsing/RegexNesting.cs` alone.

Attempt 1's plant is the line of part C's control 2 (`probes/Regex.Probe/AdversarialControls.targets`), written into the library's source of the copy.

**The run.** 2026-11-18T10:33:21Z to 2026-11-18T10:34:39Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.NestingLimitTests.|FullyQualifiedName~Broiler.Regex.Tests.NestingMaximumTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 2, passed 0, skipped 0, total 2.

**Deciding tests named failing.** `NestingLimitTests.Quantifiers_sequences_and_alternatives_add_no_level`; `NestingLimitTests.The_parser_answers_what_it_reads_first`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** Not counted as the clause's witness. Two deciding tests are named failing, but the run then ended with xUnit's `[FATAL ERROR] Xunit.Sdk.TestPipelineException` about a minute later, and the test log's totals count only the two tests reported: the classes' population was not run to its end. With the check removed, the patterns far past the limit are parsed without bound. A step that stopped before the end of its population is void (`decision-rule.md`, "How it is read"), so this attempt is retained and not counted; attempt 2 is the clause's witness.
