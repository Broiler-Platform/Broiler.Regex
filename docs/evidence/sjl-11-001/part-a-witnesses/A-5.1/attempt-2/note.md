# Witness run A-5.1, attempt 2

**The clause.** A-5.1: "every property name and value ECMA-262's three tables and PropertyValueAliases.txt admit, Katakana_Or_Hiragana and Hrkt included, accepted under u and v" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The Script values Katakana_Or_Hiragana and Hrkt are refused where the parser decides a property escape's name, RegexUnicode.ClassifyProperty (attempt 1 changed ResolveProperty, which the parser calls only after ClassifyProperty has accepted the name, so it refused nothing). Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `75ee13c3430c0ed672c89000d690894eae0adbdaebc04e3d15a3537c607f2250`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Unicode/RegexUnicode.cs` alone.

**The run.** 2026-11-18T10:53:49Z to 2026-11-18T10:54:08Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.PropertyNameTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 9, passed 58, skipped 0, total 67.

**Deciding tests named failing.** `PropertyNameTests.Every_name_and_value_the_tables_admit_is_accepted`; `PropertyNameTests.Katakana_Or_Hiragana_is_a_value_no_code_point_has`; `PropertyNameTests.The_probe_cases_get_the_specification_answer`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
