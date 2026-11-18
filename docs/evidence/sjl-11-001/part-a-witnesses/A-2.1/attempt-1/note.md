# Witness run A-2.1, attempt 1

**The clause.** A-2.1: "compile charges the same meter in proportion to the work, at least one unit per range and per string an expansion touches, for a declared set of v-mode and case-insensitive expansions" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** A property escape's expansion is charged one unit whatever the ranges its set holds, not one per range (RegexUnicode.ToCodePointSet). Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `4209efcff79e565363c58d228505c321bca254a48cc20f9eae35a0f89e3de701`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Unicode/RegexUnicode.cs` alone.

**The run.** 2026-11-18T10:30:20Z to 2026-11-18T10:30:41Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.CompileChargingTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 8, passed 97, skipped 0, total 105.

**Deciding tests named failing.** `CompileChargingTests.Each_place_charges_at_least_one_unit_per_range_and_per_string_its_expansion_touches`; `CompileChargingTests.Each_range_copied_from_the_provider_is_charged_before_it_is_read`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
