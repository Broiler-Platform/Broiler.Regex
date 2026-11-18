# Witness run A-1.2, attempt 1

**The clause.** A-1.2: "one call charges across all its start positions with no reset (a non-matching subject of n positions charges at least n units)" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The per-start reset: the units a call has spent are set back to zero before each next start position's attempt, so the step ceiling and the meter read one attempt's work, not the call's. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `104f49d72724d70565dda75093119d5caa8aa08cdf276fe227f6085197a8bcb7`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Engine/RegexMachine.cs` alone.

**The run.** 2026-11-18T10:29:11Z to 2026-11-18T10:29:28Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.ChargingTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 47, passed 62, skipped 0, total 109.

**Deciding tests named failing.** `ChargingTests.A_non_matching_subject_of_n_characters_charges_at_least_n_units_and_its_end_nothing_under_the_scan`; `ChargingTests.One_call_charges_across_all_its_start_positions_with_no_reset`.

**Other tests named failing** (they decide nothing for this clause). `ChargingTests.A_call_s_units_are_one_per_instruction_with_a_class_s_weight_on_top`; `ChargingTests.A_fast_path_charges_its_dispatch_and_each_character_it_examines`; `ChargingTests.A_step_ceiling_of_k_units_stops_the_call_before_the_instruction_that_would_pass_it`; `ChargingTests.Before_the_kth_unit_of_work_at_least_k_units_are_charged_and_never_a_block_more_than_the_work`; `ChargingTests.The_start_position_scan_charges_each_position_it_skips_and_nothing_else`.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
