# Witness run A-6.2, attempt 1

**The clause.** A-6.2: "\k<name> matches the participating group" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** A reference to a duplicated name reads only the first group of the name, not the one that participated. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `38ec44f02670ee98e0e00924aab78e010b3bfe162c8778707247f2db247c9a2e`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Compiling/ProgramCompiler.cs` alone.

**The run.** 2026-11-18T10:42:39Z to 2026-11-18T10:42:58Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.DuplicateNamedGroupTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 10, passed 28, skipped 0, total 38.

**Deciding tests named failing.** `DuplicateNamedGroupTests.A_reference_to_a_duplicated_name_is_one_instruction_reading_every_group_of_the_name_at_one_unit`; `DuplicateNamedGroupTests.A_reference_to_a_duplicated_name_matches_the_group_that_participated`.

**Other tests named failing** (they decide nothing for this clause). `DuplicateNamedGroupTests.The_participating_group_comes_from_the_offset_entry_s_slots`.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
