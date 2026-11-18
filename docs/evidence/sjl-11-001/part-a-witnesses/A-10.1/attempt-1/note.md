# Witness run A-10.1, attempt 1

**The clause.** A-10.1: "the package's dependencies are Broiler.Unicode.Algorithms alone, at the version above" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The library takes a second package dependency, Broiler.Unicode.Tables, and reads a member of it. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `f5da34d05690034c96d20547e068c867687d49f75c222b5a8b41236912cb68e8`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Broiler.Regex.csproj`, `Broiler.Regex/WitnessPlantA101.cs` alone.

**What it leaves.** The clause's "at the version above" is the pin in the repository's `Directory.Packages.props`, which is not the library's source; the plant breaks the half the deciding test reads, the library's assembly references.

**The run.** 2026-11-18T10:48:50Z to 2026-11-18T10:49:08Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.ProviderDataTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 1, passed 16, skipped 0, total 17.

**Deciding tests named failing.** `ProviderDataTests.The_library_references_the_provider_alone_and_carries_no_table_or_data_version`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
