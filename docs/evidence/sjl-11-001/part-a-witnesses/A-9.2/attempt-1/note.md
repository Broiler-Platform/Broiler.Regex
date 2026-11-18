# Witness run A-9.2, attempt 1

**The clause.** A-9.2: "RegExp.escape's text escapes exactly the specification's EncodeForRegExpEscape set over EcmaScriptLexical's WhiteSpace and LineTerminator" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** RegExp.escape leaves @ as itself, though EncodeForRegExpEscape's otherPunctuators list it. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `267c1d4382d94ee26707d0f722108079d91f92d728249438a22d4b4d4efd0d2e`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Text/RegexEscaper.cs` alone.

**The run.** 2026-11-18T10:46:55Z to 2026-11-18T10:47:14Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.EscapeStringTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 5, passed 13, skipped 0, total 18.

**Deciding tests named failing.** `EscapeStringTests.Broiler_VM_s_correspondence_row_T5_answers_as_both_engines_did`; `EscapeStringTests.Each_class_the_specification_names_is_escaped_as_it_says`; `EscapeStringTests.Every_code_point_after_the_first_is_encoded_as_EncodeForRegExpEscape_encodes_it`; `EscapeStringTests.Every_code_point_first_is_escaped_as_the_leading_rule_and_EncodeForRegExpEscape_say`; `EscapeStringTests.Test262_s_RegExp_escape_cases_answer_as_test262_expects`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
