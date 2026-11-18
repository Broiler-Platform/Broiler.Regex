# Witness run A-7, attempt 1

**The clause.** A-7: "an anchored argument, independent of the y flag" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The offset entry's anchored argument depends on the y flag: a sticky pattern is matched anchored whatever the argument. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `81c040656dd1aa5574bfebe1c1183e80a02cccb5f65a5f7beb225e29812cc897`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/BroilerRegex.cs` alone.

**The run.** 2026-11-18T10:44:42Z to 2026-11-18T10:45:00Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.PublicEntryTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 1, passed 10, skipped 0, total 11.

**Deciding tests named failing.** `PublicEntryTests.The_anchored_argument_is_independent_of_the_y_flag`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
