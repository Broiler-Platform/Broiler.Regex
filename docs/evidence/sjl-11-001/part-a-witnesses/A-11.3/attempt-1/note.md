# Witness run A-11.3, attempt 1

**The clause.** A-11.3: "a validate entry returning a status, a code, a message identifier and a position" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The validate entry answers a refusal's status with the default diagnostic: no code, no message identifier and no position. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `8c54e08efa6097ed42f3461c826dfe0f653a872be5be23e9301f10d414270dc0`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/BroilerRegex.cs` alone.

**The run.** 2026-11-18T10:52:57Z to 2026-11-18T10:53:15Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.ValidateEntryTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 96, passed 57, skipped 0, total 153.

**Deciding tests named failing.** `ValidateEntryTests.A_flags_value_with_u_and_v_is_the_syntax_status_and_an_undefined_bit_a_broken_precondition`; `ValidateEntryTests.Every_refusal_is_answered_with_its_code_identifier_argument_and_position`; `ValidateEntryTests.No_public_type_is_an_exception_and_the_constructors_refuse_with_an_ArgumentException`.

**Other tests named failing** (they decide nothing for this clause). `ValidateEntryTests.An_Annex_B_extension_refused_under_u_or_v_has_its_own_code_of_row_EG0002`; `ValidateEntryTests.The_critic_s_k_case_is_refused_by_the_validate_entry`; `ValidateEntryTests.The_validate_entry_answers_as_Compile_over_the_suite_and_seeded_populations`.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
