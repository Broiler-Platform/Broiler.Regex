# Witness run A-6.3, attempt 1

**The clause.** A-6.3: "a result maps the name to the participating index" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** A name maps to the first group of the name whatever the slots say, not to the group that participated. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `40a11ec8ecbe8f0695c5be731646af1b75f4a680999f057310cd3545d3fd1f71`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/RegexGroupNames.cs` alone.

**The run.** 2026-11-18T10:44:11Z to 2026-11-18T10:44:30Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.DuplicateNamedGroupTests.|FullyQualifiedName~Broiler.Regex.Tests.RegexGroupNamesTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 11, passed 33, skipped 0, total 44.

**Deciding tests named failing.** `DuplicateNamedGroupTests.The_participating_group_comes_from_the_offset_entry_s_slots`; `RegexGroupNamesTests.The_participating_group_is_read_from_the_offset_entry_s_slots`.

**Other tests named failing** (they decide nothing for this clause). `DuplicateNamedGroupTests.A_reference_to_a_duplicated_name_matches_the_group_that_participated`.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
