# Witness run A-9.1, attempt 1

**The clause.** A-9.1: "flag parsing returns a status for a duplicate, an unknown flag and u with v" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** Flag parsing never refuses a duplicate flag. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `b733da1fe79a55825698e44936838aa1130d951efd832da19c96d8df099790c0`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Text/RegexFlagsText.cs` alone.

**The run.** 2026-11-18T10:46:15Z to 2026-11-18T10:46:33Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.FlagsTextTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 8, passed 21, skipped 0, total 29.

**Deciding tests named failing.** `FlagsTextTests.A_refusal_is_decided_by_the_ninth_code_unit_whatever_the_length`; `FlagsTextTests.A_refusal_is_the_syntax_status_with_the_validate_entry_s_diagnostic`; `FlagsTextTests.A_refusal_names_its_kind_and_the_code_unit`; `FlagsTextTests.Every_string_of_up_to_three_code_units_over_the_flags_and_strangers_reads_as_the_specification_reads_it`; `FlagsTextTests.The_throwing_reader_answers_as_before_through_the_same_grammar`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
