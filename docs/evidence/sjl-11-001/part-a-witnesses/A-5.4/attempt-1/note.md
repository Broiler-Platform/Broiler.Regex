# Witness run A-5.4, attempt 1

**The clause.** A-5.4: "a four-digit lead-surrogate escape followed by a braced escape accepted under u and v and read as two characters, a braced escape never joining a pair (row S10 of the correspondence)" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** Under u and v a four-digit lead-surrogate escape followed by a braced escape of a trail surrogate is joined into one code point (row S10's defect). Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `9e6aebfde538ee328822bb17984afde184da3fc121d2219a5c545e7f60a60978`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Parsing/RegexParser.cs` alone.

**The run.** 2026-11-18T10:39:22Z to 2026-11-18T10:39:39Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.GroupNameTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 8, passed 52, skipped 0, total 60.

**Deciding tests named failing.** `GroupNameTests.A_braced_escape_never_joins_a_surrogate_pair`.

**Other tests named failing** (they decide nothing for this clause). `GroupNameTests.Every_refused_name_is_a_syntax_error_in_every_mode`.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
