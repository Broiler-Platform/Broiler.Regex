# Witness run A-11.1, attempt 1

**The clause.** A-11.1: "no exception for an input its contract admits (the escaped group names that throw ArgumentOutOfRangeException from RegexParser.ReadGroupName in 0.1.0-preview.4's code included)" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** An escaped astral group name read without u throws ArgumentOutOfRangeException, as RegexParser.ReadGroupName did in 0.1.0-preview.4. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `44ce6d45e1f733f2a42d5b9794e4a48e9a59bb22ba775ef7b662e46f73c2534c`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Parsing/RegexParser.cs` alone.

**The run.** 2026-11-18T10:51:27Z to 2026-11-18T10:51:48Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.ValidateEntryTests.|FullyQualifiedName~Broiler.Regex.Tests.GroupNameTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 12, passed 201, skipped 0, total 213.

**Deciding tests named failing.** `GroupNameTests.An_escaped_surrogate_in_a_name_is_read_or_refused_never_thrown`; `ValidateEntryTests.The_validate_entry_answers_as_Compile_over_the_suite_and_seeded_populations`.

**Other tests named failing** (they decide nothing for this clause). `GroupNameTests.A_duplicate_name_is_refused_after_its_escapes_are_decoded`; `GroupNameTests.A_reference_names_the_group_as_decoded`; `GroupNameTests.Every_accepted_name_reads_the_same_in_every_mode`.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
