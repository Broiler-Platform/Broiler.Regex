# Witness run A-4.2, attempt 2

**The clause.** A-4.2: "the parser and the compiler check the caller's nesting limit before each descent and answer the nesting status past it" ([`decision-rule-revised-2026-11-18.md`](../../../decision-rule-revised-2026-11-18.md), "The clauses").

**The plant.** The nesting check admits one level past the caller's limit: a descent is refused only when it would go two levels past it (attempt 1 removed the check, and its run ended with xUnit's fatal TestPipelineException after two deciding tests had failed, before the classes' population was run). Its diff is `plant.diff`, its edits `plant.json`. The copy's library sources are listed by SHA-256 in `library-sources.sha256` (its digest `c184325c0ffcf1ee1e782583c6763a33a527ec49c3bbbd61c3bd368d25f4c376`), which differs from 49d7be1f's listing (`../../base/library-sources-49d7be1f.sha256`) in `Broiler.Regex/Parsing/RegexNesting.cs` alone.

**The run.** 2026-11-18T10:55:50Z to 2026-11-18T10:56:09Z, linux-x64, in a copy of `git archive 49d7be1f` on disk, behind the idle gate (`gate.log`, `run.tsv`, `time.txt`):
`dotnet build Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --nologo --disable-build-servers`, exit 0 (`build.log`); then
`dotnet test Broiler.Regex.Tests/Broiler.Regex.Tests.csproj -c Release --no-build --nologo --disable-build-servers --filter "FullyQualifiedName~Broiler.Regex.Tests.NestingLimitTests.|FullyQualifiedName~Broiler.Regex.Tests.NestingMaximumTests." --logger "trx;LogFileName=test.trx"`, exit 1 (`test.log`, `test.trx`).

**The test log's totals.** Failed 59, passed 100, skipped 0, total 159.

**Deciding tests named failing.** `NestingLimitTests.A_class_adds_a_level_under_v_only`; `NestingLimitTests.A_limit_admits_its_own_depth_and_refuses_one_level_more`; `NestingLimitTests.Every_kind_of_level_counts_toward_one_depth`; `NestingLimitTests.Quantifiers_sequences_and_alternatives_add_no_level`; `NestingLimitTests.The_members_that_take_no_limit_parse_under_the_maximum_and_refuse_past_it_as_a_syntax_error`; `NestingMaximumTests.One_level_past_the_maximum_is_refused_on_a_1_MiB_stack`; `NestingMaximumTests.The_shapes_are_listed_once_and_nest_to_the_depth_they_are_built_for`.

**Other tests named failing** (they decide nothing for this clause). none.

**Read.** The clause is witnessed by this run: its deciding tests above fail and the test log names them.
