# Witness run A-11.2, attempt 1

**The clause.** A-11.2: "concurrent compile and match on shared instances answer as single-threaded calls do" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** Every call of every program shares one static register array instead of renting its own, so concurrent calls on shared instances overwrite each other's registers. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `8756a829dfd715b939685406251ded9cae0bebd06eaf5cfdb9957ee81036156c`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Engine/RegexMachine.cs` alone.

**The run.** 2026-11-18T10:52:00Z to 2026-11-18T10:52:25Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.ConcurrentUseTests.|FullyQualifiedName~Broiler.Regex.Tests.FirstUseConcurrencyTests.|FullyQualifiedName~Broiler.Regex.Tests.EntryTests.|FullyQualifiedName~Broiler.Regex.Tests.CompileChargingTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 3, passed 120, skipped 0, total 123.

**Deciding tests named failing.** `ConcurrentUseTests.Concurrent_first_compiles_and_matches_of_a_fresh_library_answer_as_one_thread_does`; `ConcurrentUseTests.Warm_concurrent_compiles_and_matches_answer_as_one_thread_does`; `EntryTests.One_program_shared_by_concurrent_calls_answers_each_as_it_answers_alone`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
