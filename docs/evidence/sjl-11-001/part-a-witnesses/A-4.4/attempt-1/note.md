# Witness run A-4.4, attempt 1

**The clause.** A-4.4: "a pattern at the maximum, and a flat sequence of part C's class F, complete on a thread of the smallest declared stack under the JIT and in the Native AOT probe" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The parser's descent needs more stack than the smallest declared stack holds: each Disjunction it reads takes a 4 KiB frame and asks whether the stack has room, so a pattern at the nesting maximum does not complete on a 1 MiB thread. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `8901e8682a88adef36d903873509ef87210ad2d4dcd41939e9053c4e29d32ee2`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Parsing/RegexParser.cs` alone.

**Why this plant.** The clause's "in the Native AOT probe" is a run of the probe published with Native AOT, not a test of `Broiler.Regex.Tests`; the deciding tests run the same proof under the JIT on a thread of 1 MiB, the smallest stack the corpus declares. The library has no CLR recursion in its matcher (A-4.1) and its parser recurses once per level, bounded by the nesting limit, so no change of a constant reaches the guarantee; the closest faithful plant is a descent that needs more stack per level than 1 MiB holds at the maximum. The plant asks `RuntimeHelpers.EnsureSufficientExecutionStack`, which the library is forbidden to call (rule C5 of `tests/Regex.Architecture.Tests`' register, "No library asks how much stack is left"), only so that the shortage is an exception the test names: without it the thread would overflow its stack and end the test host, and no test would be named.

**The run.** 2026-11-18T10:35:41Z to 2026-11-18T10:36:04Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.NestingMaximumTests.|FullyQualifiedName~Broiler.Regex.Tests.StackDepthGuardTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**Kept out by digest** (over 2 MB): `test.log`, `test.trx`; `../../manifest.json` names each.

**The test log's totals.** Failed 109, passed 31, skipped 0, total 140.

**Deciding tests named failing.** `NestingMaximumTests.A_pattern_at_the_maximum_matches_on_a_1_MiB_stack`; `NestingMaximumTests.A_pattern_at_the_maximum_of_a_shape_the_old_matcher_did_not_prove_matches_on_a_1_MiB_stack`; `NestingMaximumTests.A_pattern_at_the_maximum_parses_and_compiles_on_a_1_MiB_stack`.

**Other tests named failing** (they decide nothing for this clause). `NestingMaximumTests.A_pattern_far_past_the_maximum_is_refused_on_a_1_MiB_stack`; `NestingMaximumTests.One_level_past_the_maximum_is_refused_on_a_1_MiB_stack`.

**Read.** Not counted as the clause's witness. The deciding tests failed with `System.InsufficientExecutionStackException` (the summary below is read from the test log), but each failure's stack trace runs some hundreds of parser levels deep, so the test log and the trx are over 2 MB and are named in the manifest by their digests only, not kept. This attempt is not counted, since its log is not retained; attempt 2 takes a larger frame per level, fails at a shallower depth, and its test log is retained.
