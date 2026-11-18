# Witness run A-4.3, attempt 1

**The clause.** A-4.3: "a limit above the documented maximum is refused" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** A nesting limit above the documented maximum is accepted, read as the maximum, rather than refused. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `9414bf818b675afba78af087ced8aab34dff7372247ea786602556d6b6b0a2bf`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/BroilerRegex.cs` alone.

**The run.** 2026-11-18T10:34:51Z to 2026-11-18T10:35:08Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.NestingLimitTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 1, passed 27, skipped 0, total 28.

**Deciding tests named failing.** `NestingLimitTests.The_limit_lies_between_1_and_the_documented_maximum`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
