# Witness run A-5.1, attempt 1

**The clause.** A-5.1: "every property name and value ECMA-262's three tables and PropertyValueAliases.txt admit, Katakana_Or_Hiragana and Hrkt included, accepted under u and v" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The Script values Katakana_Or_Hiragana and Hrkt are refused. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `d140d54697a93ce315adddafb751874d658f707852c3532edc9a8d91d1a21cd3`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Unicode/RegexUnicode.cs` alone.

**The run.** 2026-11-18T10:36:36Z to 2026-11-18T10:36:55Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.PropertyNameTests." --logger "trx;LogFileName=test.trx"`, exit 0 (`test.log`, `test.trx`).

**The test log's totals.** Failed 0, passed 67, skipped 0, total 67.

**Deciding tests named failing.** none.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** A failed attempt: it does not witness the clause. No test failed. The plant changed `RegexUnicode.ResolveProperty`, which the parser reaches only after `RegexUnicode.ClassifyProperty` has accepted the name (`RegexParser.ResolvePropertyEscape`), so nothing was refused. This attempt does not witness the clause; attempt 2 plants the same refusal in `ClassifyProperty`.
