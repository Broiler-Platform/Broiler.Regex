# Witness run A-5.3, attempt 1

**The clause.** A-5.3: "group names validated by EcmaScriptLexical at UCD 17.0.0, raw, escaped and as surrogate pairs, in every mode" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** A group name's later code points are validated by EcmaScriptLexical's IdentifierPart or as a hyphen, so (?<a-b>x) is accepted. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `697fcd03d805114e348c814b71b7a3e0d25491274744300f9a45e40f6ae07f0f`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Parsing/RegexParser.cs` alone.

**The run.** 2026-11-18T10:38:31Z to 2026-11-18T10:38:50Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.GroupNameTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 8, passed 52, skipped 0, total 60.

**Deciding tests named failing.** `GroupNameTests.Every_refused_name_is_a_syntax_error_in_every_mode`; `GroupNameTests.The_probe_cases_get_the_specification_answer`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
