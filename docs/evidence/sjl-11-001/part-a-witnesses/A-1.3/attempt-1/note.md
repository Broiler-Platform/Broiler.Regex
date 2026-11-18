# Witness run A-1.3, attempt 1

**The clause.** A-1.3: "with a step ceiling, every catastrophic case of part C returns LimitExceeded naming steps, never NoMatch or Match" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** LimitExceeded naming steps is answered NoMatch (part C's control 1's line, as probes/Regex.Probe/AdversarialControls.targets declares it, applied to the library's source). Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `f0170e620f88237f5c126c5e9f1f082774a86f3d52ce7dc0035655b227e9eb76`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Engine/RegexProgram.cs` alone.

The plant is the line of part C's control 1 (`probes/Regex.Probe/AdversarialControls.targets`), written into the library's source of the copy.

**The run.** 2026-11-18T10:23:07Z to 2026-11-18T10:23:24Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.CeilingTests.|FullyQualifiedName~Broiler.Regex.Tests.ChargingTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 38, passed 107, skipped 0, total 145.

**Deciding tests named failing.** `CeilingTests.A_catastrophic_case_with_a_step_ceiling_answers_LimitExceeded_naming_steps_and_never_a_match_or_no_match`; `CeilingTests.The_step_ceiling_is_reached_at_the_unit_past_it`; `ChargingTests.A_scan_stopped_by_its_step_ceiling_has_tested_only_the_positions_it_paid_for`; `ChargingTests.A_step_ceiling_of_k_units_stops_the_call_before_the_instruction_that_would_pass_it`; `ChargingTests.An_anchored_call_makes_no_scan_and_a_scan_stops_at_the_step_ceiling`.

**Other tests named failing** (they decide nothing for this clause). `CeilingTests.A_back_reference_whose_length_passes_the_step_ceiling_stops_before_it_compares`; `CeilingTests.A_ceiling_reached_in_a_look_around_or_a_back_reference_names_itself_and_is_never_taken_for_the_body_s_failure`; `CeilingTests.A_negative_look_around_whose_body_reaches_a_ceiling_answers_LimitExceeded_not_a_match`; `CeilingTests.With_the_fast_paths_every_ceiling_below_what_a_call_needs_names_itself`; `ChargingTests.One_call_charges_across_all_its_start_positions_with_no_reset`; `ChargingTests.The_first_charge_comes_before_the_first_instruction`.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
