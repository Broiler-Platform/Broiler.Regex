# Witness run A-1.4, attempt 1

**The clause.** A-1.4: "a meter that throws at its budget propagates its exception, and the same compiled instance then answers a fresh call as a new instance does" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** A meter that throws at its budget does not propagate: the machine catches the meter's exception and answers LimitExceeded naming steps. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `82b6f00181ffaa4da8df2b1bc11f4d4140ac74c40b95f51cf2d6d682fb921a82`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Engine/RegexMachine.cs` alone.

**The run.** 2026-11-18T10:29:40Z to 2026-11-18T10:29:58Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.ChargingTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 1, passed 108, skipped 0, total 109.

**Deciding tests named failing.** `ChargingTests.A_meter_that_throws_at_its_budget_propagates_and_the_program_answers_a_fresh_call_as_a_new_one_does`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
