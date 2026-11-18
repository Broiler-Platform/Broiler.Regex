# Witness run A-8, attempt 1

**The clause.** A-8: "an offset-only interface whose signatures carry no string result (the API baseline), and whose call allocates nothing that grows with the capture count" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** Every call allocates an array of the program's slot count, which grows with the capture count. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `f04174b5fb8644bfd547807ca4cf059f11e7233b04052e62cf2708fd42ebca5c`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Engine/RegexProgram.cs` alone.

**The run.** 2026-11-18T10:45:32Z to 2026-11-18T10:45:53Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.PublicEntryTests.|FullyQualifiedName~Broiler.Regex.Tests.EntryTests.|FullyQualifiedName~Broiler.Regex.Tests.DuplicateNamedGroupTests.|FullyQualifiedName~Broiler.Regex.Tests.RegexGroupNamesTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 12, passed 57, skipped 0, total 69.

**Deciding tests named failing.** `EntryTests.A_call_allocates_nothing_that_grows_with_the_capture_count`; `EntryTests.A_call_through_task_2_4b_s_instructions_allocates_nothing_that_grows_with_the_capture_count`; `PublicEntryTests.A_call_allocates_nothing_that_grows_with_the_capture_count`; `PublicEntryTests.A_call_allocates_nothing_up_to_the_frames_and_trail_cells_the_pool_keeps_and_past_them_its_own_arrays`.

**Other tests named failing** (they decide nothing for this clause). `EntryTests.A_call_that_ends_with_the_meter_s_exception_gives_its_arrays_back`.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
