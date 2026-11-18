# Witness run A-5.2, attempt 1

**The clause.** A-5.2: "the refused set - each name with its letter case changed, an underscore removed, a space or hyphen inserted, an Is prefix, or as the value of the wrong property - refused" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** A property name refused as written is tried again without an Is prefix, so \p{IsLetter} and \p{IsScript=Greek} are accepted. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `cc141dfc3acee1aad7bd600265f955cd8710d5a77b2020f2187a80fa20b7e2bf`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Unicode/RegexUnicode.cs` alone.

**The run.** 2026-11-18T10:37:16Z to 2026-11-18T10:37:39Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.PropertyNameTests." --logger "trx;LogFileName=test.trx"`, exit 0 (`test.log`, `test.trx`).

**The test log's totals.** Failed 0, passed 67, skipped 0, total 67.

**Deciding tests named failing.** none.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** A failed attempt: it does not witness the clause. No test failed. The plant changed `RegexUnicode.ResolveProperty` alone, which the parser reaches only after `RegexUnicode.ClassifyProperty` has accepted the name, so no refused spelling was accepted. This attempt does not witness the clause; attempt 2 plants the same loosening in `ClassifyProperty` too.
