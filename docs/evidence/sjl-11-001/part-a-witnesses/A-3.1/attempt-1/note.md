# Witness run A-3.1, attempt 1

**The clause.** A-3.1: "the frame and trail ceilings are the caller's" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The frame and trail ceilings are the library's, not the caller's: the machine takes the engines' defaults whatever ceilings the call passes. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `90679d2d08af5327a623469976cbaffa52fc3411f1c01e49d26b4831da797a30`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Engine/RegexMachine.cs` alone.

**The run.** 2026-11-18T10:31:49Z to 2026-11-18T10:32:09Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.CeilingTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 13, passed 23, skipped 0, total 36.

**Deciding tests named failing.** `CeilingTests.The_frame_ceiling_is_reached_one_frame_past_what_the_match_holds_at_once`; `CeilingTests.The_trail_ceiling_is_reached_one_cell_past_the_writes_the_match_has_not_undone`.

**Other tests named failing** (they decide nothing for this clause). `CeilingTests.A_ceiling_reached_in_a_look_around_or_a_back_reference_names_itself_and_is_never_taken_for_the_body_s_failure`; `CeilingTests.A_negative_look_around_whose_body_reaches_a_ceiling_answers_LimitExceeded_not_a_match`.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
