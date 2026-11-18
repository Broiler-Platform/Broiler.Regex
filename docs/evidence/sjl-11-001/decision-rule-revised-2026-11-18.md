# Bundle SJL-11-001's decision rule, revised on 2026-11-18 by the owner's choice: part A alone read a second time at `49d7be1f`, once every clause of every check has a retained witness run

**Written:** 2026-11-18, at SJL-11.2 (task 4.2), **after** the candidate reading at
`49d7be1f3ef148f9c262f0e52b8c360c07fd838d` was taken and seen: its retained runs, its per-part reading and its part A
NOT MET ([`README.md`](README.md), the dated note of 2026-11-18; [`docs/roadmap.md`](../../roadmap.md), the record of
2026-11-18). The bundle's revision rule is [`decision-rule.md`](decision-rule.md)'s "Revisions": "A rule revised after
any run it judges has been seen is a new dated file in this directory, quoting this one whole and saying it was written
afterwards; its decision is then weaker evidence." **This revision was written after the candidate reading was seen, so
its decision is weaker evidence**: it was drafted for the one part that reading found NOT MET, and that part's results
at the reading were known when it was written. It was written, and committed, **before any witness run it defines was
taken**: no plant had been made or run when this file was committed. It is a new dated file beside `decision-rule.md`
and the reference-arm rule's revisions (`reference-arms-revised-2026-11-16.md`, `16b` and `16c`), the first revision of
`decision-rule.md`, named in this directory's convention. It quotes `decision-rule.md` whole at its end and leaves it,
`reference-arms.md` and every revision of it unedited. **Owner:** the Broiler.RegEx maintainer. **Reviewer:** none
(EX-30). **It holds no figure**: what the witness runs find is written in the bundle's README and nowhere in this file.

## The owner's decision

The question put to the owner on 2026-11-18 was, verbatim: "The Broiler.Regex candidate reading at 49d7be1f reads part A
NOT MET. Every part A test passes, but the rule also needs a retained run of each check's witness failing once. Only
A-5's \k clause has one, from part B's control. A-1 and A-4 have in-test witnesses for half of each check. Seven checks
(A-2, A-3, A-6 to A-10) have none, and A-11 has none either. Parts B to E and the reference arms read MET. Under the
rule, part A's NOT MET blocks both cut-overs: SJL-11.5 in Broiler.JS and SJL-11.4 in Broiler.VM. How should part A go
forward?"

**The owner chose "Take witnesses, re-read A"**, described as: "A dated revision lets part A alone be read again at
49d7be1f, once every missing witness has a retained run of it failing. Nothing else in the rule changes. For each check,
I plant its defect in a local scratch worktree (no pushes), run its witness test, keep the red log and the revert, then
read part A again. Parts B to E and the arms keep their readings."

The options not taken were "Read the prose accounts", described as: "You read the roadmap's accounts of the witnesses
seen failing during development as meeting part A. It is recorded as your reading, and it is weaker evidence, because no
retained file decides it as the rule asks."; and "Leave it NOT MET", described as: "Both cut-overs stay blocked. If part
A's checks still have no witness at the end of SJL-11.2, the area is refused by JSD-0081's refusal path."

**That is the owner's own decision.** It is not a choice taken under the owner's standing direction of 2026-10-18
("Please do all recommended decisions and skip waiting for review."). What this file adds to the choice is its wording:
the unit (a clause), the tests that decide each clause, what a witness run is, the revert, what is read, and what does
not change, each defined below before any run.

## What it revises

- **Part A's MET line**, "MET if and only if every check passes at the reading, and each witness was watched failing
  once.", and **"How it is read"**, its item for part A: "**Part A**: each check by its test's result in the test log of
  the reading, and by the retained run of its witness failing once; a check whose witness was not seen failing is not
  met." Under them, read at the candidate reading, part A read NOT MET: the manifest names no retained run of a witness
  failing for most clauses (the README's dated note of 2026-11-18 and its table).
- **"Each reading is judged on its own"** and the readings' list ("The candidate reading is taken at the commit
  `0.2.0-preview.1` is packed from ... The end reading is taken at the commit both engines pin after their cut-overs, if
  it differs. Every part is judged at both."). They name two readings; this revision adds a **second reading of part A
  alone at the candidate reading's commit**, judged on its own, and no other reading.

## The revision

```rule
evidence class: library contract (not a measurement), part A alone, read a second time at the candidate reading's commit
reading: the commit 49d7be1f3ef148f9c262f0e52b8c360c07fd838d, the candidate reading's; read from the candidate reading's retained test logs (candidate-reading/library-evidence/logs/tests.log, and the Release jobs 134473695300 and 134473694930 of run 44206505415 under candidate-reading/ci/job-logs/), unchanged, together with witness runs taken after the candidate reading
unit: each clause of each check A-1 to A-11, the clauses as the check's text separates them by ";", each named A-n.m in the table "The clauses" below with the tests that decide it; a check with no ";" is one clause
witness run: a plant, a minimal change to the library's sources (the files under Broiler.Regex/) and to nothing else - never a test, the rule or a retained file - in a throwaway copy of 49d7be1f's tree (git archive, on disk), that breaks exactly that clause's guarantee; then Broiler.Regex.Tests built in Release from that copy and run, filtered to the deciding test classes or whole; the run retained: the plant's diff, the SHA-256 listing of the copy's library sources, the build log, the test log (dotnet test's console output, naming each test that fails), the exit codes, and a note of what the plant breaks and which tests failed
witnessed: a clause is witnessed if and only if at least one of its deciding tests fails in a retained witness run and is named failing in that run's test log; a run that fails at build, or that fails only in tests that do not decide the clause, does not witness it; a different plant may then be tried; every attempt is retained, a failed attempt named as such, and none is discarded
a clause no source change can reach: a clause whose guarantee is not a property of the library's sources alone takes the closest faithful plant its deciding test is written to catch, and its note says why; if no plant makes a deciding test fail, the clause is unwitnessed, and recorded as such
already witnessed: A-1.5 and A-4.1, each by the planted copies of the library its test builds and refuses on every run, in the test itself; A-5.6, by part B's control run 44237117988 (its Release jobs 134575819708 and 134575819702, retained under candidate-reading/ci/job-logs/); each keeps that witness and takes no new plant
the revert: the candidate reading's own test logs at 49d7be1f, all passing; each plant's copy differs from 49d7be1f's tree only by its plant's diff, and its library sources' SHA-256 listing is retained beside the diff
retained: under part-a-witnesses/, every file named by its SHA-256 in part-a-witnesses/manifest.json, a manifest of manifest.json's shape; a file over 2 MB by its digest only; LF only; a file neither manifest names decides nothing
MET if and only if every check passes at the reading (the candidate reading's logs, unchanged) and every clause of A-1 to A-11 has a retained witness run.
NOT MET otherwise; a clause with no witness after its attempts is not met, and no further revision follows from this file.
```

## The clauses

Each check's text is the rule block of [`decision-rule.md`](decision-rule.md), split at its ";". **The tests that decide
a clause** are taken from the test classes the README's table of 2026-11-18 names for its check (the classes whose
headers name the check), each narrowed to the tests whose names state that clause; where no class of that table holds a
test of the clause, the test the repository holds for it is named, and the row says so. A test may decide more than one
clause. Every test named passed at the reading: the candidate reading's logs carry totals per assembly, 0 failed, and
the README's note of 2026-11-18 reads those totals for each test. Tests are named `Class.Method` in
`Broiler.Regex.Tests` unless the row says the architecture tests; a class named alone decides by all its tests.

| Clause | Its text | The tests that decide it | Its witness at the candidate reading |
|---|---|---|---|
| A-1.1 | "every operation whose work grows with its input takes a nullable RegexWorkCharge and charges before the work it pays for, in the blocks JSD-0081's decision D8 documents: a recording meter shows, for a declared set of inputs, that before the k-th instruction runs at least k units are charged" | `ChargingTests`, every test not named below for A-1.2, A-1.3 or A-1.4 (among them `Before_the_kth_unit_of_work_at_least_k_units_are_charged_and_never_a_block_more_than_the_work`, `The_first_charge_comes_before_the_first_instruction`, `Each_character_a_fast_path_or_the_scan_examines_is_charged_before_it_is_read`, `The_declared_inputs_reach_every_instruction_in_both_directions`) | none |
| A-1.2 | "one call charges across all its start positions with no reset (a non-matching subject of n positions charges at least n units)" | `ChargingTests.One_call_charges_across_all_its_start_positions_with_no_reset`, `ChargingTests.A_non_matching_subject_of_n_characters_charges_at_least_n_units_and_its_end_nothing_under_the_scan` | none |
| A-1.3 | "with a step ceiling, every catastrophic case of part C returns LimitExceeded naming steps, never NoMatch or Match" | `CeilingTests.A_catastrophic_case_with_a_step_ceiling_answers_LimitExceeded_naming_steps_and_never_a_match_or_no_match`, `CeilingTests.The_step_ceiling_is_reached_at_the_unit_past_it`, `ChargingTests.A_step_ceiling_of_k_units_stops_the_call_before_the_instruction_that_would_pass_it`, `ChargingTests.A_scan_stopped_by_its_step_ceiling_has_tested_only_the_positions_it_paid_for`, `ChargingTests.An_anchored_call_makes_no_scan_and_a_scan_stops_at_the_step_ceiling`, `ChargingTests.The_declared_inputs_run_the_start_position_scan_over_a_character_and_a_class_and_into_a_step_ceiling` | none |
| A-1.4 | "a meter that throws at its budget propagates its exception, and the same compiled instance then answers a fresh call as a new instance does" | `ChargingTests.A_meter_that_throws_at_its_budget_propagates_and_the_program_answers_a_fresh_call_as_a_new_one_does`, `ChargingTests.A_meter_may_match_the_same_program_while_it_is_being_charged` | none |
| A-1.5 | "Budget, StepLimit and the per-start reset are absent from the assembly's metadata" | `MeterMetadataTests` (architecture tests) | in the test, run every time |
| A-2.1 | "compile charges the same meter in proportion to the work, at least one unit per range and per string an expansion touches, for a declared set of v-mode and case-insensitive expansions" | `CompileChargingTests`, every test not named below for A-2.2 or A-11.2 (among them `Each_place_charges_at_least_one_unit_per_range_and_per_string_its_expansion_touches`, `Each_range_copied_from_the_provider_is_charged_before_it_is_read`, `The_meter_holds_whole_blocks_covering_every_unit_charged_and_never_a_block_more`) | none |
| A-2.2 | "a compile budget refuses every case of part C's class V" | `AdversarialCorpusTests` (its class V rows: `A_row_the_new_backend_runs_answers_its_declared_status_in_a_child_process`, `Each_class_V_row_s_expansion_charges_at_least_a_hundred_times_its_budget`), `CompileChargingTests.A_compile_budget_admits_the_work_it_covers_and_refuses_one_unit_less`, `CompileChargingTests.The_budget_and_the_nesting_limit_each_answer_their_own_dimension_in_the_order_the_work_meets_them`, `CompileChargingTests.A_compile_budget_of_zero_is_a_broken_precondition_and_the_constructors_take_neither_budget_nor_meter` | none |
| A-3.1 | "the frame and trail ceilings are the caller's" | `CeilingTests.The_frame_ceiling_is_reached_one_frame_past_what_the_match_holds_at_once`, `CeilingTests.The_trail_ceiling_is_reached_one_cell_past_the_writes_the_match_has_not_undone`, `CeilingTests.A_counted_loop_that_holds_no_choice_still_fills_the_trail_two_cells_an_iteration`, `CeilingTests.A_run_holds_one_frame_and_writes_no_trail_cell_whatever_its_length`, `CeilingTests.A_ceiling_the_call_never_reaches_costs_nothing`, `CeilingTests.A_ceiling_below_one_or_a_frame_or_trail_ceiling_above_its_maximum_is_a_broken_precondition`, `CeilingTests.The_engines_defaults_are_decision_D10_s` | none |
| A-3.2 | "a test reaches each and is answered LimitExceeded naming it" | `CeilingTests.The_frame_ceiling_is_reached_one_frame_past_what_the_match_holds_at_once`, `CeilingTests.The_trail_ceiling_is_reached_one_cell_past_the_writes_the_match_has_not_undone`, `CeilingTests.A_ceiling_reached_in_a_look_around_or_a_back_reference_names_itself_and_is_never_taken_for_the_body_s_failure`, `CeilingTests.With_the_fast_paths_every_ceiling_below_what_a_call_needs_names_itself`, `CeilingTests.A_negative_look_around_whose_body_reaches_a_ceiling_answers_LimitExceeded_not_a_match` | none |
| A-4.1 | "no method of the matcher's namespace reaches itself, directly or through a cycle, in the IL call graph" | `MatcherRecursionTests` (architecture tests) | in the test, run every time |
| A-4.2 | "the parser and the compiler check the caller's nesting limit before each descent and answer the nesting status past it" | `NestingLimitTests`, every test but `The_limit_lies_between_1_and_the_documented_maximum`; `NestingMaximumTests.One_level_past_the_maximum_is_refused_on_a_1_MiB_stack`, `NestingMaximumTests.A_pattern_far_past_the_maximum_is_refused_on_a_1_MiB_stack`, `NestingMaximumTests.The_shapes_are_listed_once_and_nest_to_the_depth_they_are_built_for` | none |
| A-4.3 | "a limit above the documented maximum is refused" | `NestingLimitTests.The_limit_lies_between_1_and_the_documented_maximum` | none |
| A-4.4 | "a pattern at the maximum, and a flat sequence of part C's class F, complete on a thread of the smallest declared stack under the JIT and in the Native AOT probe" | `NestingMaximumTests.A_pattern_at_the_maximum_parses_and_compiles_on_a_1_MiB_stack`, `NestingMaximumTests.A_pattern_at_the_maximum_matches_on_a_1_MiB_stack`, `NestingMaximumTests.A_pattern_at_the_maximum_of_a_shape_the_old_matcher_did_not_prove_matches_on_a_1_MiB_stack`, `StackDepthGuardTests` | none |
| A-5.1 | "every property name and value ECMA-262's three tables and PropertyValueAliases.txt admit, Katakana_Or_Hiragana and Hrkt included, accepted under u and v" | `PropertyNameTests.The_name_tables_are_read`, `PropertyNameTests.Every_lone_name_the_tables_admit_is_accepted`, `PropertyNameTests.Every_name_and_value_the_tables_admit_is_accepted`, `PropertyNameTests.Every_property_of_strings_is_accepted_under_v_alone_and_never_negated`, `PropertyNameTests.Katakana_Or_Hiragana_is_a_value_no_code_point_has`, `PropertyNameTests.The_probe_cases_get_the_specification_answer` | none |
| A-5.2 | "the refused set - each name with its letter case changed, an underscore removed, a space or hyphen inserted, an Is prefix, or as the value of the wrong property - refused" | `PropertyNameTests.A_lone_name_spelled_otherwise_is_refused`, `PropertyNameTests.A_name_or_value_spelled_otherwise_is_refused`, `PropertyNameTests.A_value_of_the_wrong_property_is_refused`, `PropertyNameTests.The_probe_cases_get_the_specification_answer` | none |
| A-5.3 | "group names validated by EcmaScriptLexical at UCD 17.0.0, raw, escaped and as surrogate pairs, in every mode" | `GroupNameTests.Every_accepted_name_reads_the_same_in_every_mode`, `GroupNameTests.Every_refused_name_is_a_syntax_error_in_every_mode`, `GroupNameTests.An_escaped_surrogate_in_a_name_is_read_or_refused_never_thrown`, `GroupNameTests.A_duplicate_name_is_refused_after_its_escapes_are_decoded`, `GroupNameTests.A_reference_names_the_group_as_decoded`, `GroupNameTests.The_probe_cases_get_the_specification_answer` | none |
| A-5.4 | "a four-digit lead-surrogate escape followed by a braced escape accepted under u and v and read as two characters, a braced escape never joining a pair (row S10 of the correspondence)" | `GroupNameTests.A_braced_escape_never_joins_a_surrogate_pair` | none |
| A-5.5 | "\0's lookahead ASCII" | `DecimalEscapeTests.Nul_before_a_digit_that_is_not_ASCII_is_NUL_then_that_digit`, `DecimalEscapeTests.Nul_before_no_digit_is_NUL_in_every_mode`. No class of the README's table holds a test of this clause; `DecimalEscapeTests`, whose header states it ("`\0` before any other digit ... is NUL and then that digit in every mode"), is the test the repository holds for it (requirement R5, task 2.2, `ac26079`) | none |
| A-5.6 | "\k<name> to an undeclared name refused by the parser and by the validate entry" | `NamedReferenceTests.A_reference_to_an_undeclared_name_is_refused_by_the_parse_alone`, `ValidateEntryTests.The_critic_s_k_case_is_refused_by_the_validate_entry`, `ValidateEntryTests.Every_refusal_is_answered_with_its_code_identifier_argument_and_position` | part B's control run 44237117988 |
| A-5.7 | "Annex B syntax only without u and v" | `ValidateEntryTests.An_Annex_B_extension_refused_under_u_or_v_has_its_own_code_of_row_EG0002`, `NamedReferenceTests.Without_a_group_name_k_is_the_letter`, `NamedReferenceTests.In_a_class_without_u_and_v_k_is_the_letter_beside_a_group_name`, and `DecimalEscapeTests.Outside_Unicode_mode_a_legacy_octal_escape_is_its_code_unit`, `DecimalEscapeTests.Under_u_and_v_a_legacy_octal_escape_is_a_syntax_error` (the last two, as A-5.5's row says of the class) | none |
| A-6.1 | "a name repeated only in alternatives that cannot both participate accepted, and refused otherwise, after escapes are decoded" | `DuplicateNamedGroupTests.A_name_repeated_only_across_alternatives_that_cannot_both_participate_is_admitted`, `DuplicateNamedGroupTests.A_name_repeated_where_both_groups_might_participate_is_refused_at_the_later_group`, `DuplicateNamedGroupTests.The_rule_agrees_with_MightBothParticipate_over_a_seeded_population_of_trees`, `DuplicateNamedGroupTests.A_name_is_compared_as_decoded_so_differently_spelled_names_that_decode_alike_are_one_name`, `RegexGroupNamesTests.A_name_is_decoded_and_compared_by_code_unit` | none |
| A-6.2 | "\k<name> matches the participating group" | `DuplicateNamedGroupTests.A_reference_to_a_duplicated_name_matches_the_group_that_participated`, `DuplicateNamedGroupTests.A_reference_to_a_duplicated_name_is_one_instruction_reading_every_group_of_the_name_at_one_unit`, `DuplicateNamedGroupTests.Every_group_of_a_name_in_one_thousand_alternatives_is_read_by_its_reference` | none |
| A-6.3 | "a result maps the name to the participating index" | `DuplicateNamedGroupTests.A_result_maps_each_name_to_the_group_that_participated_and_lists_the_names_in_source_order`, `DuplicateNamedGroupTests.The_participating_group_comes_from_the_offset_entry_s_slots`, `RegexGroupNamesTests`, every test but `A_name_is_decoded_and_compared_by_code_unit` and `Reading_the_participating_group_allocates_nothing` | none |
| A-7 | "an anchored argument, independent of the y flag" | `PublicEntryTests.The_anchored_argument_is_independent_of_the_y_flag` | none |
| A-8 | "an offset-only interface whose signatures carry no string result (the API baseline), and whose call allocates nothing that grows with the capture count" | `PublicEntryTests.The_entry_s_signature_carries_no_string_and_answers_a_status`, `PublicEntryTests.A_call_allocates_nothing_that_grows_with_the_capture_count`, `PublicEntryTests.A_call_allocates_nothing_up_to_the_frames_and_trail_cells_the_pool_keeps_and_past_them_its_own_arrays`, `EntryTests.A_call_allocates_nothing_that_grows_with_the_capture_count`, `EntryTests.A_call_through_task_2_4b_s_instructions_allocates_nothing_that_grows_with_the_capture_count`, `DuplicateNamedGroupTests.A_match_through_a_duplicated_name_and_its_reading_allocate_nothing`, `RegexGroupNamesTests.Reading_the_participating_group_allocates_nothing` | none |
| A-9.1 | "flag parsing returns a status for a duplicate, an unknown flag and u with v" | `FlagsTextTests` | none |
| A-9.2 | "RegExp.escape's text escapes exactly the specification's EncodeForRegExpEscape set over EcmaScriptLexical's WhiteSpace and LineTerminator" | `EscapeStringTests` | none |
| A-9.3 | "source escaping follows EscapeRegExpPattern" | `PatternSourceTests` | none |
| A-10.1 | "the package's dependencies are Broiler.Unicode.Algorithms alone, at the version above" | `ProviderDataTests.The_library_references_the_provider_alone_and_carries_no_table_or_data_version` | none |
| A-10.2 | "CaseFolding, CaseFoldingData, the generator and the Broiler.UniCode packages are absent" | `ProviderDataTests.The_library_references_the_provider_alone_and_carries_no_table_or_data_version` | none |
| A-10.3 | "a recording IUnicodeTables sees every table the library reads" | `ProviderDataTests.A_pattern_of_each_kind_reads_the_tables_through_the_callers_provider`, `ProviderDataTests.A_match_reads_its_case_folding_from_the_callers_tables` | none |
| A-10.4 | "the library declares no data version of its own" | `ProviderDataTests.The_library_references_the_provider_alone_and_carries_no_table_or_data_version`, `ProviderDataTests.A_provider_of_another_Unicode_version_is_refused_when_a_pattern_is_compiled` | none |
| A-11.1 | "no exception for an input its contract admits (the escaped group names that throw ArgumentOutOfRangeException from RegexParser.ReadGroupName in 0.1.0-preview.4's code included)" | `ValidateEntryTests.The_entry_checks_its_arguments_and_answers_no_exception_for_a_refusal`, `ValidateEntryTests.The_validate_entry_answers_as_Compile_over_the_suite_and_seeded_populations`, and `GroupNameTests.An_escaped_surrogate_in_a_name_is_read_or_refused_never_thrown`, the test of the clause's parenthesis (its summary names `RegexParser.ReadGroupName`'s `ArgumentOutOfRangeException` in 0.1.0-preview.4), in a class the README's table lists under A-5 | none |
| A-11.2 | "concurrent compile and match on shared instances answer as single-threaded calls do" | `ConcurrentUseTests`, `FirstUseConcurrencyTests`, `EntryTests.One_program_shared_by_concurrent_calls_answers_each_as_it_answers_alone`, `CompileChargingTests.Concurrent_compilations_with_meters_and_budgets_answer_as_one_alone_does` | none |
| A-11.3 | "a validate entry returning a status, a code, a message identifier and a position" | `ValidateEntryTests`, every test not named above for A-5.6, A-5.7 or A-11.1 (among them `Every_refusal_is_answered_with_its_code_identifier_argument_and_position`, `Every_code_is_refused_by_a_case_of_the_table`, `The_code_table_is_pinned`, `A_condition_the_catalogue_names_has_the_catalogue_s_identifier`) | none |

**A clause's plant** is chosen for that clause: it breaks the guarantee the clause's text states, and the run is read
only on the clause's deciding tests. A plant that also fails tests of other clauses or checks is allowed, as a defect
seldom breaks one guarantee alone; those failures are recorded in the note and decide nothing here.

**Clauses a source change reaches only in part.** A-4.4's "in the Native AOT probe" is a run of the probe under Native
AOT, not a test of `Broiler.Regex.Tests`; its plant is the closest one its deciding tests, the 1 MiB-thread tests under
the JIT, are written to catch (a descent that needs more stack than that thread has), and its note says so. A-10.1's "at
the version above" is a pin in the repository's `Directory.Packages.props`, which is not the library's source; its
plant is a second dependency of the library, the half of the clause the deciding test reads.

## How it is read

- **The second reading of part A is judged on its own.** It reads part A at `49d7be1f` from the candidate reading's
  test logs, unchanged, and from the witness runs under `part-a-witnesses/`, retained after the candidate reading. It is
  not the candidate reading: the candidate reading's part A NOT MET stays on record as that reading's, and is not
  rewritten. It is not the end reading either.
- **The verdict is worded** "read MET on the retained runs" or "read NOT MET", in the bundle's README, with a table
  clause by clause: the clause, its deciding tests, the candidate reading's result, and the witness run with the failing
  deciding tests its log names, or "unwitnessed".
- **The witness runs are local**, on linux-x64, in throwaway copies on disk that are deleted after their runs, through
  `dotnet test` in Release; none is pushed, and none is a CI run. Each copy is `git archive 49d7be1f` with the plant's
  diff applied, so its tree is the reading's but for the diff.
- **What the second reading bears on.** The line "A part NOT MET at the candidate reading blocks both cut-overs" is
  not changed. For part A alone, the reading that line reads at the candidate reading's commit is, from this file's
  date, part A's second reading: if it reads MET, part A no longer blocks the cut-overs by that line; if it reads NOT
  MET, part A blocks them as before. For parts B to E the line reads their candidate readings, unchanged. The first
  reading's NOT MET stays on record either way. Nothing else of either engine's cut-over is decided here
  ("What this file does not decide", quoted below: "**Either engine's cut-over**, test262 parity, fuel parity,
  measurement or engine-level adversarial corpus.").

## What it does not change

- Parts B, C, D and E, the reference arms, their rules and revisions, and their readings at the candidate reading.
- Every check's text, A-1 to A-11 included, the reading's commit (`49d7be1f`), the rule commit, and the pins.
- The manifest's rule that "a file the manifest does not name decides nothing", which reads here of both manifests.
- "A part NOT MET at the candidate reading blocks both cut-overs (risk D10's stop)", JSD-0081's refusal path, and the
  end reading, at which every part, A included, is judged again from its own logs.
- `decision-rule.md`, `reference-arms.md` and the revisions of 2026-11-16, 16b and 16c, which stay unedited, and every
  retained file of the candidate reading.

## The rule it revises, quoted whole

**`decision-rule.md`** at its blob `d83215d6e02f109954184dcf457c486a7e0fab48` on Broiler.Regex's `main` at `662f3499`
(the blob it has at the reading's commit `49d7be1f`), each line prefixed with "> ":

> # Bundle SJL-11-001's decision rule: the component clauses of the regular-expression area - the requirements R1 to R11 as a contract, test262 in this repository's CI, the adversarial corpus under JIT and Native AOT, the vectors at library level, and the process
>
> **Written:** 2026-10-20, at milestone SJL-11 (work package SJL-11.1) of the shared JavaScript libraries programme,
> with the dated record of this repository's `docs/roadmap.md` section 6 that adopts R1 to R11 as owner conditions, and
> before any code of SJL-11.2 exists in this repository: no meter, no new backend, no provider, no validate entry.
> Committed in the same change: `tests/Adversarial/corpus.tsv` and `eng/adversarial-programs.py`. **Milestone judged:**
> SJL-11, `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-11-regexp.md`: its component clauses 1 ("Every
> requirement of SJL-11.2 is met"), 2 ("test262's `RegExp` directories run in the component's CI") and 3 ("The
> adversarial corpus shows no silent exhaustion and no shortened match"), and the vectors at library level (template
> T3). **Owner:** the Broiler.RegEx maintainer. **Reviewer:** none. Every role this rule names, both engines' owners
> included, is held by one person (the programme records this as Broiler.VM's exception EX-30), and this rule does not
> claim that anyone independent has read it.
>
> *Revised 2026-10-25, before any run and before this change reached the default branch, to agree with Broiler.VM's
> JSD-0081 as filed: check A-5 adds row S10's escapes, which that record's requirement table places under R5 and its
> decision D36 among SJL-11.2's fixes; nothing else changed. Revised again the same day, after review and still before
> any run: a section "How it is read" is added, which the programme's validation document asks of every rule (section
> 4); no rule block changed.*
>
> **What this file is.** Five predeclared decisions, one per part. Part A is the contract (R1 to R9 and R11), each
> check with a witness. Part B is clause 2. Part C is clause 3, at library level, in both execution modes. Part D is
> template T3 at library level. Part E is R10. The reference-arm comparison (template T4) is
> [`reference-arms.md`](reference-arms.md) beside this file. **It holds no figure**; counts and seeds below are
> parameters.
>
> **The area's record** is Broiler.VM's JSD-0081, and its input is Broiler.VM's `docs/evidence/sjl-11-001/correspondence.md`.
>
> **The readings.**
> - **The candidate reading** is taken at the commit `0.2.0-preview.1` is packed from. **A part NOT MET there blocks both
>   cut-overs** (the programme's `risks.md` D10: "an engine cut over to Broiler.Regex before every requirement of
>   SJL-11.2 holds" is a stop).
> - **The end reading** is taken at the commit both engines pin after their cut-overs, if it differs.
> Every part is judged at both.
>
> *Dated note, 2026-11-01 (task 2.9 of SJL-11.2); the rule above is not rewritten.* The candidate reading's line reads
> "The candidate reading is taken at the commit `0.2.0-preview.1` is packed from", after decision D12 of Broiler.VM's
> JSD-0081 ("the SJL-11 surface ships as `0.2.0-preview.1`"). The owner wrote on 2026-11-01 "Please keep using version
> 0.1.0-x scheme!", which revises D12: the SJL-11 surface ships as `0.1.0-preview.6`, the next of the `0.1.0-preview.N`
> stream after `0.1.0-preview.5`, which the owner published on 2026-10-29 from `24344b3`. The candidate reading is read
> as taken at the commit `0.1.0-preview.6` is packed from.
>
> *The same dated note, extended on 2026-11-01 (the review of task 2.9).* It ended "nothing else of the rule changes",
> which was not so: two more lines of the rule are read otherwise from 2026-11-01, each by a dated record, and neither is
> rewritten.
> - **Check E-4's baseline.** E-4 reads "package validation against 0.1.0-preview.4". By the owner's decision of
>   2026-11-01, recorded in `docs/roadmap.md` (task 2.9's record: "The SJL-11 surface ships as `0.1.0-preview.6`,
>   validated against `0.1.0-preview.5`"), the baseline is the last published version, `0.1.0-preview.5`, and
>   `Broiler.Regex/CompatibilitySuppressions.xml` holds only the breaks of `0.1.0-preview.5`; the 23 breaks of
>   `0.1.0-preview.4` that `0.1.0-preview.5` shipped left it and stay listed in `docs/suppressions.md` and the CHANGELOG
>   as history. E-4 is read as package validation against `0.1.0-preview.5`; a pack against `0.1.0-preview.4` would fail.
> - **The pins and check A-10's "version above".** The pins line names `Broiler.Unicode.Algorithms` `0.1.0-preview.5`,
>   "the version both engines admit", and `Broiler.Unicode.Tables` `0.1.0-preview.5`. The owner's dependency update
>   `77284f1` ("Update dependencies", 2026-10-30) pins both at `0.1.0-preview.6` in this repository, and both engines pin
>   `Broiler.Unicode.Algorithms` `0.1.0-preview.6` (Broiler.JS at `66973a29`, Broiler.VM at `07def73`). The pins line and
>   A-10's "at the version above" are read as `0.1.0-preview.6` of both packages; the packed `0.1.0-preview.6` depends on
>   `Broiler.Unicode.Algorithms` `0.1.0-preview.6`.
>
> **The pins.** test262 `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`, checked by its tree hash; `Broiler.Unicode.Algorithms`
> `0.1.0-preview.5`, the version both engines admit, and `Broiler.Unicode.Tables` `0.1.0-preview.5` for tests and the
> probe only; the vectors' copy under `tests/Vectors/` by its pin file.
>
> ## Part A - the contract (clause 1: R1 to R9 and R11)
>
> ```rule
> evidence class: library contract (not a measurement)
> population: every public member of the package Broiler.Regex at the reading's commit
> checks, each a test in Broiler.Regex.Tests or tests/Regex.Architecture.Tests, each with a witness that was watched failing once:
>   A-1 R1, the budget: every operation whose work grows with its input takes a nullable RegexWorkCharge and charges before the work it pays for, in the blocks JSD-0081's decision D8 documents: a recording meter shows, for a declared set of inputs, that before the k-th instruction runs at least k units are charged; one call charges across all its start positions with no reset (a non-matching subject of n positions charges at least n units); with a step ceiling, every catastrophic case of part C returns LimitExceeded naming steps, never NoMatch or Match; a meter that throws at its budget propagates its exception, and the same compiled instance then answers a fresh call as a new instance does; Budget, StepLimit and the per-start reset are absent from the assembly's metadata
>   A-2 R2, compilation: compile charges the same meter in proportion to the work, at least one unit per range and per string an expansion touches, for a declared set of v-mode and case-insensitive expansions; a compile budget refuses every case of part C's class V
>   A-3 R3, memory: the frame and trail ceilings are the caller's; a test reaches each and is answered LimitExceeded naming it
>   A-4 R4, nesting: no method of the matcher's namespace reaches itself, directly or through a cycle, in the IL call graph; the parser and the compiler check the caller's nesting limit before each descent and answer the nesting status past it; a limit above the documented maximum is refused; a pattern at the maximum, and a flat sequence of part C's class F, complete on a thread of the smallest declared stack under the JIT and in the Native AOT probe
>   A-5 R5, exact syntax: every property name and value ECMA-262's three tables and PropertyValueAliases.txt admit, Katakana_Or_Hiragana and Hrkt included, accepted under u and v; the refused set - each name with its letter case changed, an underscore removed, a space or hyphen inserted, an Is prefix, or as the value of the wrong property - refused; group names validated by EcmaScriptLexical at UCD 17.0.0, raw, escaped and as surrogate pairs, in every mode; a four-digit lead-surrogate escape followed by a braced escape accepted under u and v and read as two characters, a braced escape never joining a pair (row S10 of the correspondence); \0's lookahead ASCII; \k<name> to an undeclared name refused by the parser and by the validate entry; Annex B syntax only without u and v
>   A-6 R6, duplicate named groups: a name repeated only in alternatives that cannot both participate accepted, and refused otherwise, after escapes are decoded; \k<name> matches the participating group; a result maps the name to the participating index
>   A-7 R7: an anchored argument, independent of the y flag
>   A-8 R8: an offset-only interface whose signatures carry no string result (the API baseline), and whose call allocates nothing that grows with the capture count
>   A-9 R9: flag parsing returns a status for a duplicate, an unknown flag and u with v; RegExp.escape's text escapes exactly the specification's EncodeForRegExpEscape set over EcmaScriptLexical's WhiteSpace and LineTerminator; source escaping follows EscapeRegExpPattern
>   A-10 R11, data: the package's dependencies are Broiler.Unicode.Algorithms alone, at the version above; CaseFolding, CaseFoldingData, the generator and the Broiler.UniCode packages are absent; a recording IUnicodeTables sees every table the library reads; the library declares no data version of its own
>   A-11 the conventions the contract rests on: no exception for an input its contract admits (the escaped group names that throw ArgumentOutOfRangeException from RegexParser.ReadGroupName in 0.1.0-preview.4's code included); concurrent compile and match on shared instances answer as single-threaded calls do; a validate entry returning a status, a code, a message identifier and a position
> MET if and only if every check passes at the reading, and each witness was watched failing once.
> NOT MET otherwise.
> ```
>
> ## Part B - test262 in this repository's CI (clause 2)
>
> ```rule
> evidence class: conformance in CI (not a comparison of runs)
> population: test262 at the pin, the paths test/built-ins/RegExp, test/language/literals/regexp, test/annexB/built-ins/RegExp, test/annexB/language/literals/regexp, test/built-ins/RegExpStringIteratorPrototype and test/built-ins/String/prototype/{match,matchAll,replace,replaceAll,search,split}
> checks:
>   B-1 the host job: .github/workflows/ci.yml's job test262-host checks out Broiler.JS at the forty-digit commit this repository's docs/roadmap.md names by a dated record, packs this commit's Broiler.Regex as the version that commit pins into a local feed listed first, builds BroilerJS in Release against it, and runs scripts/compliance/run_test262.py with --include-negative over the population, original variant; green if and only if no row fails or times out but those in test262/host-expected-failures.txt, each entry naming its path, the host member it fails in and why that member is the host's (the RegExp object, the protocols, species, GetSubstitution's evaluation, lastIndex), and no entry is attributed to pattern syntax, compilation or matching; and every row of a file listing regexp-duplicate-named-groups passes
>   B-2 the library job: test262-library runs this repository's runner over (i) every file under the two literal directories whose regular-expression literals the runner's declared grammar extracts - a negative syntax test's literal refused by the validate entry and by compile, a positive file's literals accepted - (ii) test/built-ins/RegExp/property-escapes/generated through its structured helpers, every listed code point matching and every other code point not, and (iii) test/built-ins/RegExp/unicodeSets/generated likewise; a file it cannot read is listed in test262/library-unreadable.txt with why; under the JIT and through the Native AOT probe, the two reports byte-identical
>   B-3 both jobs run on the default branch at the reading's commit and are green
> controls: a scratch commit of this repository, never merged, that accepts \k<name> to an undeclared name under u; B-1 or B-2 must turn red; reverted
> MET if and only if B-1 to B-3 hold at the reading and the control turned a job red.
> NOT MET otherwise.
> ```
>
> ## Part C - the adversarial corpus under JIT and Native AOT (clause 3)
>
> ```rule
> evidence class: resource accounting at library level (not a measurement)
> population: every row of tests/Adversarial/corpus.tsv as committed with this rule: classes K catastrophic backtracking, A alternation without quantifiers, L look-around and lookbehind, G a greedy run longer than the old allowance, W exhaustion inside one alternative, N nesting (the limit, one past it, and depths far past it, in groups, non-capturing groups and v-mode classes), F a flat sequence of many terms, M frames and trail, V large v-mode and case-insensitive expansions, O ordinary patterns; each row a pattern, flags, a subject built by a declared expression, an operation (compile, validate, or match at an index, anchored or not), a policy (meter budget or none, step, frame and trail ceilings, nesting limit, compile budget) and a declared status (a match with its offsets, no match, LimitExceeded naming its dimension, the meter's exception, the nesting status, a syntax status); a row declared exhausted needs, by regexpref's count of the specification's matcher steps, at least a hundred times its budget, and a row declared to match completes within a hundredth of it
> configuration: probes/Regex.Probe --adversarial, built under the JIT and published with Native AOT under Broiler.VM's root settings, on ubuntu-latest and windows-latest, each row in a child process on a thread of the smallest stack the corpus's header declares; at the reading's commit; each child's exit and status line retained
> controls: (1) a scratch build in which LimitExceeded naming steps is answered NoMatch: every K, A, L, W and G row must be reported; (2) a scratch build without the nesting check: every N row past the limit must end abnormally or differ, and be reported; each reverted and the revert checked
> comparison: per row, mode and RID, the status against the declared one; per RID, the JIT report against the Native AOT report, byte for byte
> admitted difference classes: none
> MET if and only if every row gives its declared status in both modes on both RIDs, no child process ends abnormally, the two modes' reports are byte-identical per RID, and both controls are reported.
> NOT MET otherwise.
> ```
>
> ## Part D - the vectors at library level (template T3)
>
> ```rule
> evidence class: conformance at library level (not a measurement)
> population: every vector of conformance/vectors/regexp/*.vectors at the corpus revision bundle sjl-11-001 names, copied into tests/Vectors/regexp/ with a pin file naming Broiler.JS's commit and each file's SHA-256, checked before the run
> configuration: the library runner in Broiler.Regex.Tests, mapping each op to the library (compile and validate, exec at a lastIndex under the flags, the helpers), under the JIT and through the Native AOT probe
> MET if and only if every normative vector whose op the library implements gives its out, every status vector gives its status, every impl-defined vector is reported with the library's answer, the two modes' reports are identical, and every op the library does not implement (the object model's) is listed by name.
> NOT MET otherwise.
> ```
>
> ## Part E - the process (R10, the conventions, SJL-D-5 (a))
>
> ```rule
> evidence class: process record (not a measurement)
> checks:
>   E-1 IsAotCompatible and IsTrimmable declared, the trim, AOT and single-file analyzers reporting nothing, as errors; no suppression
>   E-2 the Native AOT probe in CI publishes with Broiler.VM's root settings and no warning, leaves no managed assembly in the publish directory, and runs parts C and D's populations with output equal to the JIT's
>   E-3 SL1 to SL10 Active in tests/Regex.Architecture.Tests with a register and a witness each, watched failing once; docs/conventions.md the programme's numbering; the starting violation list empty
>   E-4 the API baseline compared in both directions; package validation against 0.1.0-preview.4 with each suppression listed in docs/suppressions.md and in CHANGELOG.md under Changed with its R item; CHANGELOG.md with Observable changes and Data headings
>   E-5 the fuzz target tests/Regex.Fuzz runs in CI for its declared seed and iteration count with every limit and a meter set: no exception but statuses and the meter's, no process end; a planted throw in a declared parser path is found by the declared seed
>   E-6 the assurance gate: every relevant unit annotated; the artefacts generated by Broiler.Code's command-line tool; CI fails on drift, watched failing once; HUMAN_REVIEW.md names the revision it reviews, or says that none is reviewed
>   E-7 .gitattributes LF, no preprocessor directive, net10.0, nullable enabled, implicit usings disabled
> MET if and only if every check holds at the reading.
> NOT MET otherwise; and a check not holding at the end of SJL-11.2 refuses the area by JSD-0081's refusal path.
> ```
>
> ## How it is read
>
> - **Each reading is judged on its own.** Parts A to E are judged at the candidate reading from the logs retained for
>   it, and again at the end reading, if there is one, from its own logs; a part MET at one reading says nothing of the
>   other. A part NOT MET at the candidate reading blocks both cut-overs (risk D10's stop).
> - **What refuses the area.** JSD-0081's refusal path, as this repository's roadmap section 6 states it: if any of R1
>   to R11 is not met by the end of SJL-11.2, the area is refused. Part A judges R1 to R9 and R11, part B the test262
>   readings of R5, R6 and R10, and part E R10's process and the conventions; a check of any of them not holding at the
>   end of SJL-11.2 is such a requirement unmet, and refuses the area by that path. Part E's block says so of its own
>   checks, and is not the only part that can.
>   *(Revised 2026-10-25, before any reading and before this file reached the default branch: the first item ended "A
>   part NOT MET at the candidate reading blocks both cut-overs (risk D10's stop), and a check of part E not holding at
>   the end of SJL-11.2 refuses the area.", which named part E alone and so read narrower than the roadmap's refusal
>   path and JSD-0081.)*
> - **Each check is decided by a retained file, named in the bundle's manifest by its SHA-256**; a file the manifest
>   does not name decides nothing, and a step that stopped before the end of its population is void and is not judged.
>   `run-library-evidence.sh` writes one log per step, each step's command its first line.
>   - **Part A**: each check by its test's result in the test log of the reading, and by the retained run of its witness
>     failing once; a check whose witness was not seen failing is not met.
>   - **Part B**: the two CI jobs' runs at the reading's commit, named by their run identifiers, read with
>     `test262/host-expected-failures.txt` and `test262/library-unreadable.txt` as that commit holds them; and the
>     control's scratch commit with the run it turned red.
>   - **Part C**: each child's exit and status line per row, mode and RID; the two modes' reports per RID, compared by
>     their digests; and each control's run with its revert.
>   - **Part D**: the runner's report under each mode, with the pin file's check, which comes before the run.
>   - **Part E**: each check by its own record: the build's analyzer output, the probe's publish log, the architecture
>     tests' result with their register, package validation's output with `docs/suppressions.md`, the fuzz job's run,
>     the assurance job's run and its drift control, and the repository's settings files at the reading's commit.
> - **Counts are results, not rules.** Where a check names a count or a seed, it is a parameter; what a reading finds is
>   written in the bundle's README at that reading, and nowhere in this file.
>
> ## Revisions
>
> A rule revised after any run it judges has been seen is a new dated file in this directory, quoting this one whole and
> saying it was written afterwards; its decision is then weaker evidence. Before any code of SJL-11.2 lands, a later
> bundle shows this file unchanged.
>
> ## What this file does not decide
>
> - **Either engine's cut-over**, test262 parity, fuel parity, measurement or engine-level adversarial corpus.
> - **The vectors' expected values**: Broiler.JS's corpus and its reference `regexpref` decide them.
> - **A human review**: SJL-D-5 (a) gates a release, not a development step.
> - **Performance.** Nothing here is timed.
