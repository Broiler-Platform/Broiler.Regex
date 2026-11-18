# Witness run A-3.2, attempt 1

**The clause.** A-3.2: "a test reaches each and is answered LimitExceeded naming it" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The trail ceiling, when reached, is answered LimitExceeded naming the frames, not the trail. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `b665223c17ebb0d69f7b886946f0f80a73289d6ba08cc5e759613fc214cc3d2a`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Engine/RegexMachine.cs` alone.

**The run.** 2026-11-18T10:32:31Z to 2026-11-18T10:32:49Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.CeilingTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 23, passed 13, skipped 0, total 36.

**Deciding tests named failing.** `CeilingTests.A_ceiling_reached_in_a_look_around_or_a_back_reference_names_itself_and_is_never_taken_for_the_body_s_failure`; `CeilingTests.A_negative_look_around_whose_body_reaches_a_ceiling_answers_LimitExceeded_not_a_match`; `CeilingTests.The_trail_ceiling_is_reached_one_cell_past_the_writes_the_match_has_not_undone`; `CeilingTests.With_the_fast_paths_every_ceiling_below_what_a_call_needs_names_itself`.

**Other tests named failing** (they decide nothing for this clause). `CeilingTests.A_counted_loop_that_holds_no_choice_still_fills_the_trail_two_cells_an_iteration`.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
