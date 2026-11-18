# Witness run A-9.3, attempt 1

**The clause.** A-9.3: "source escaping follows EscapeRegExpPattern" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The source getter leaves a solidus outside a class as itself, where EscapeRegExpPattern escapes it. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `62089f1cd06b85065fa3814df362b6edb11d098b4694037c46fd5fc0380e45f4`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Text/RegexEscaper.cs` alone.

**The run.** 2026-11-18T10:47:58Z to 2026-11-18T10:48:18Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.PatternSourceTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 16, passed 6, skipped 0, total 22.

**Deciding tests named failing.** `PatternSourceTests.Broiler_VM_s_correspondence_rows_T1_to_T4_answer_as_the_specification_and_the_staging_test_ask`; `PatternSourceTests.Classes_escapes_and_groups_are_read_as_a_literal_reads_them`; `PatternSourceTests.Every_code_unit_after_an_escaped_bracket_is_outside_a_class`; `PatternSourceTests.Every_code_unit_alone_is_itself_but_the_solidus_and_the_line_terminators`; `PatternSourceTests.Every_code_unit_in_five_shapes_holds_the_specification_s_properties_in_every_mode`; `PatternSourceTests.Every_pattern_of_the_test_project_s_string_literals_holds_the_specification_s_properties`; `PatternSourceTests.Test262_s_staging_escape_js_cases_answer_as_test262_expects`; `PatternSourceTests.The_meter_is_paid_in_whole_blocks_before_each_pass_and_a_pattern_that_needs_no_change_costs_its_reading`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
