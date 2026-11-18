# Witness run A-6.1, attempt 1

**The clause.** A-6.1: "a name repeated only in alternatives that cannot both participate accepted, and refused otherwise, after escapes are decoded" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** A repeated group name is never refused: the check of MightBothParticipate is made unreachable. Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `dcc0134a8ac0da1b6002150f7099a11393e1eda8dc1124fb1721a88f987008cb`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Parsing/RegexParser.cs` alone.

**The run.** 2026-11-18T10:41:49Z to 2026-11-18T10:42:07Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.DuplicateNamedGroupTests.|FullyQualifiedName~Broiler.Regex.Tests.RegexGroupNamesTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 8, passed 36, skipped 0, total 44.

**Deciding tests named failing.** `DuplicateNamedGroupTests.A_name_repeated_where_both_groups_might_participate_is_refused_at_the_later_group`; `DuplicateNamedGroupTests.The_rule_agrees_with_MightBothParticipate_over_a_seeded_population_of_trees`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
