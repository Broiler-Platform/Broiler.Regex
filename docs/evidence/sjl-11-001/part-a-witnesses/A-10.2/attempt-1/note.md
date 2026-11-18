# Witness run A-10.2, attempt 1

**The clause.** A-10.2: "CaseFolding, CaseFoldingData, the generator and the Broiler.UniCode packages are absent" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The library declares a type named CaseFolding. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `85c4b9cfeef4ed8e9cc3a5b7222686c33ec6b0a23687084d25049a4f8cc85c24`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Unicode/CaseFolding.cs` alone.

**The run.** 2026-11-18T10:49:40Z to 2026-11-18T10:49:58Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.ProviderDataTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 1, passed 16, skipped 0, total 17.

**Deciding tests named failing.** `ProviderDataTests.The_library_references_the_provider_alone_and_carries_no_table_or_data_version`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
