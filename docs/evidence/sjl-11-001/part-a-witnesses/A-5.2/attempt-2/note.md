# Witness run A-5.2, attempt 2

**The clause.** A-5.2: "the refused set - each name with its letter case changed, an underscore removed, a space or hyphen inserted, an Is prefix, or as the value of the wrong property - refused" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** A lone property name refused as written is tried again without an Is prefix, so \p{IsLetter} is accepted: in ClassifyProperty, which decides the name, and in ResolveProperty, which expands it (attempt 1 changed ResolveProperty alone, which the parser reaches only for a name ClassifyProperty accepted). Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `54e9ac305d9a19a2ca226e5da89a6ff0a4f71e6070548b346a0f62c5d5a42bb3`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Unicode/RegexUnicode.cs` alone.

**The run.** 2026-11-18T10:54:30Z to 2026-11-18T10:54:50Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.PropertyNameTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 2, passed 65, skipped 0, total 67.

**Deciding tests named failing.** `PropertyNameTests.A_lone_name_spelled_otherwise_is_refused`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
