# Witness run A-2.2, attempt 1

**The clause.** A-2.2: "a compile budget refuses every case of part C's class V" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The compile budget is ignored: an account is made with no budget whatever the caller passes, so no compilation is refused at its budget. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `e67e49043753308477dae9ea572a88c79c43751706daf97fd50a18406f027a98`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/CompileCharge.cs` alone.

**The run.** 2026-11-18T10:30:52Z to 2026-11-18T10:31:17Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.AdversarialCorpusTests.|FullyQualifiedName~Broiler.Regex.Tests.CompileChargingTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 17, passed 156, skipped 0, total 173.

**Deciding tests named failing.** `AdversarialCorpusTests.A_row_the_new_backend_runs_answers_its_declared_status_in_a_child_process`; `CompileChargingTests.A_compile_budget_admits_the_work_it_covers_and_refuses_one_unit_less`; `CompileChargingTests.A_compile_budget_of_zero_is_a_broken_precondition_and_the_constructors_take_neither_budget_nor_meter`; `CompileChargingTests.The_budget_and_the_nesting_limit_each_answer_their_own_dimension_in_the_order_the_work_meets_them`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
