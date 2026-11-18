# Witness run A-10.3, attempt 1

**The clause.** A-10.3: "a recording IUnicodeTables sees every table the library reads" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The case folding is read from the first provider the process hands the library, kept in a static field, not from each caller's tables. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `889e351aab37d9829b37d5f908e40eee073d3e3d95ffff44556a9bc9ff4d9cbf`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Unicode/RegexUnicode.cs` alone.

**The run.** 2026-11-18T10:50:09Z to 2026-11-18T10:50:27Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.ProviderDataTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 11, passed 6, skipped 0, total 17.

**Deciding tests named failing.** `ProviderDataTests.A_match_reads_its_case_folding_from_the_callers_tables`; `ProviderDataTests.A_pattern_of_each_kind_reads_the_tables_through_the_callers_provider`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
