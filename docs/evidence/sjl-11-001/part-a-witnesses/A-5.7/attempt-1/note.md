# Witness run A-5.7, attempt 1

**The clause.** A-5.7: "Annex B syntax only without u and v" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** Annex B's legacy octal escape is read under u and v too. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `6e378b475f34b7d3fd868504da2206e4379da8434fbe00ab9a7c8141a782236f`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Parsing/RegexParser.cs` alone.

**The run.** 2026-11-18T10:40:51Z to 2026-11-18T10:41:16Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.ValidateEntryTests.|FullyQualifiedName~Broiler.Regex.Tests.NamedReferenceTests.|FullyQualifiedName~Broiler.Regex.Tests.DecimalEscapeTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 10, passed 211, skipped 0, total 221.

**Deciding tests named failing.** `DecimalEscapeTests.Under_u_and_v_a_legacy_octal_escape_is_a_syntax_error`; `ValidateEntryTests.An_Annex_B_extension_refused_under_u_or_v_has_its_own_code_of_row_EG0002`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
