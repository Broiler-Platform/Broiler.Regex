# Witness run A-5.5, attempt 1

**The clause.** A-5.5: "\0's lookahead ASCII" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** \0's lookahead reads char.IsDigit, any Unicode decimal digit, not the ASCII digits alone (0.1.0-preview.4's defect). Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `87cf19286c9e3b6f3cefed380277f77b93dfed60aebd5654e91309cba4d27736`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Parsing/RegexParser.cs` alone.

**The run.** 2026-11-18T10:40:11Z to 2026-11-18T10:40:29Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.DecimalEscapeTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 4, passed 36, skipped 0, total 40.

**Deciding tests named failing.** `DecimalEscapeTests.Nul_before_a_digit_that_is_not_ASCII_is_NUL_then_that_digit`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
