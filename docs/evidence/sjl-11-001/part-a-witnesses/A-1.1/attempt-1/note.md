# Witness run A-1.1, attempt 1

**The clause.** A-1.1: "every operation whose work grows with its input takes a nullable RegexWorkCharge and charges before the work it pays for, in the blocks JSD-0081's decision D8 documents: a recording meter shows, for a declared set of inputs, that before the k-th instruction runs at least k units are charged" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The meter is charged after the work it pays for: Pay charges the whole blocks the units already spent fill (rounding down), not the blocks that cover them, so the meter trails the work by up to a block. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `14ef046cea68935a8d49bb0213f0b367575ab22a91b938ed700ed8777f9c9edd`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Engine/RegexMachine.cs` alone.

**The run.** 2026-11-18T10:28:51Z to 2026-11-18T10:29:09Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.ChargingTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 56, passed 53, skipped 0, total 109.

**Deciding tests named failing.** `ChargingTests.A_back_reference_s_length_is_charged_before_any_of_its_characters_is_compared`; `ChargingTests.Before_the_kth_unit_of_work_at_least_k_units_are_charged_and_never_a_block_more_than_the_work`; `ChargingTests.Each_character_a_fast_path_or_the_scan_examines_is_charged_before_it_is_read`; `ChargingTests.The_first_charge_comes_before_the_first_instruction`.

**Other tests named failing** (they decide nothing for this clause). `ChargingTests.A_non_matching_subject_of_n_characters_charges_at_least_n_units_and_its_end_nothing_under_the_scan`; `ChargingTests.A_step_ceiling_of_k_units_stops_the_call_before_the_instruction_that_would_pass_it`; `ChargingTests.One_call_charges_across_all_its_start_positions_with_no_reset`.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
