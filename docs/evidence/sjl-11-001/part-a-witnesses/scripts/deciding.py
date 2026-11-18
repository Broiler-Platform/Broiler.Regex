# The deciding tests of each clause, as decision-rule-revised-2026-11-18.md's table "The clauses" names them.
# An entry is "Class.Method", "Class" (all its tests), or ("Class", [excluded methods]) for "every test but".
CH_A12 = ["One_call_charges_across_all_its_start_positions_with_no_reset", "A_non_matching_subject_of_n_characters_charges_at_least_n_units_and_its_end_nothing_under_the_scan"]
CH_A13 = ["A_step_ceiling_of_k_units_stops_the_call_before_the_instruction_that_would_pass_it", "A_scan_stopped_by_its_step_ceiling_has_tested_only_the_positions_it_paid_for", "An_anchored_call_makes_no_scan_and_a_scan_stops_at_the_step_ceiling", "The_declared_inputs_run_the_start_position_scan_over_a_character_and_a_class_and_into_a_step_ceiling"]
CH_A14 = ["A_meter_that_throws_at_its_budget_propagates_and_the_program_answers_a_fresh_call_as_a_new_one_does", "A_meter_may_match_the_same_program_while_it_is_being_charged"]
CC_A22 = ["A_compile_budget_admits_the_work_it_covers_and_refuses_one_unit_less", "The_budget_and_the_nesting_limit_each_answer_their_own_dimension_in_the_order_the_work_meets_them", "A_compile_budget_of_zero_is_a_broken_precondition_and_the_constructors_take_neither_budget_nor_meter"]
CC_A112 = ["Concurrent_compilations_with_meters_and_budgets_answer_as_one_alone_does"]
VE_OTHER = ["The_critic_s_k_case_is_refused_by_the_validate_entry", "An_Annex_B_extension_refused_under_u_or_v_has_its_own_code_of_row_EG0002", "The_entry_checks_its_arguments_and_answers_no_exception_for_a_refusal", "The_validate_entry_answers_as_Compile_over_the_suite_and_seeded_populations"]
D = {
 "A-1.1": [("ChargingTests", CH_A12 + CH_A13 + CH_A14)],
 "A-1.2": ["ChargingTests." + m for m in CH_A12],
 "A-1.3": ["CeilingTests.A_catastrophic_case_with_a_step_ceiling_answers_LimitExceeded_naming_steps_and_never_a_match_or_no_match", "CeilingTests.The_step_ceiling_is_reached_at_the_unit_past_it"] + ["ChargingTests." + m for m in CH_A13],
 "A-1.4": ["ChargingTests." + m for m in CH_A14],
 "A-2.1": [("CompileChargingTests", CC_A22 + CC_A112)],
 "A-2.2": ["AdversarialCorpusTests.A_row_the_new_backend_runs_answers_its_declared_status_in_a_child_process", "AdversarialCorpusTests.Each_class_V_row_s_expansion_charges_at_least_a_hundred_times_its_budget"] + ["CompileChargingTests." + m for m in CC_A22],
 "A-3.1": ["CeilingTests." + m for m in ["The_frame_ceiling_is_reached_one_frame_past_what_the_match_holds_at_once", "The_trail_ceiling_is_reached_one_cell_past_the_writes_the_match_has_not_undone", "A_counted_loop_that_holds_no_choice_still_fills_the_trail_two_cells_an_iteration", "A_run_holds_one_frame_and_writes_no_trail_cell_whatever_its_length", "A_ceiling_the_call_never_reaches_costs_nothing", "A_ceiling_below_one_or_a_frame_or_trail_ceiling_above_its_maximum_is_a_broken_precondition", "The_engines_defaults_are_decision_D10_s"]],
 "A-3.2": ["CeilingTests." + m for m in ["The_frame_ceiling_is_reached_one_frame_past_what_the_match_holds_at_once", "The_trail_ceiling_is_reached_one_cell_past_the_writes_the_match_has_not_undone", "A_ceiling_reached_in_a_look_around_or_a_back_reference_names_itself_and_is_never_taken_for_the_body_s_failure", "With_the_fast_paths_every_ceiling_below_what_a_call_needs_names_itself", "A_negative_look_around_whose_body_reaches_a_ceiling_answers_LimitExceeded_not_a_match"]],
 "A-4.2": [("NestingLimitTests", ["The_limit_lies_between_1_and_the_documented_maximum"]), "NestingMaximumTests.One_level_past_the_maximum_is_refused_on_a_1_MiB_stack", "NestingMaximumTests.A_pattern_far_past_the_maximum_is_refused_on_a_1_MiB_stack", "NestingMaximumTests.The_shapes_are_listed_once_and_nest_to_the_depth_they_are_built_for"],
 "A-4.3": ["NestingLimitTests.The_limit_lies_between_1_and_the_documented_maximum"],
 "A-4.4": ["NestingMaximumTests.A_pattern_at_the_maximum_parses_and_compiles_on_a_1_MiB_stack", "NestingMaximumTests.A_pattern_at_the_maximum_matches_on_a_1_MiB_stack", "NestingMaximumTests.A_pattern_at_the_maximum_of_a_shape_the_old_matcher_did_not_prove_matches_on_a_1_MiB_stack", "StackDepthGuardTests"],
 "A-5.1": ["PropertyNameTests." + m for m in ["The_name_tables_are_read", "Every_lone_name_the_tables_admit_is_accepted", "Every_name_and_value_the_tables_admit_is_accepted", "Every_property_of_strings_is_accepted_under_v_alone_and_never_negated", "Katakana_Or_Hiragana_is_a_value_no_code_point_has", "The_probe_cases_get_the_specification_answer"]],
 "A-5.2": ["PropertyNameTests." + m for m in ["A_lone_name_spelled_otherwise_is_refused", "A_name_or_value_spelled_otherwise_is_refused", "A_value_of_the_wrong_property_is_refused", "The_probe_cases_get_the_specification_answer"]],
 "A-5.3": ["GroupNameTests." + m for m in ["Every_accepted_name_reads_the_same_in_every_mode", "Every_refused_name_is_a_syntax_error_in_every_mode", "An_escaped_surrogate_in_a_name_is_read_or_refused_never_thrown", "A_duplicate_name_is_refused_after_its_escapes_are_decoded", "A_reference_names_the_group_as_decoded", "The_probe_cases_get_the_specification_answer"]],
 "A-5.4": ["GroupNameTests.A_braced_escape_never_joins_a_surrogate_pair"],
 "A-5.5": ["DecimalEscapeTests.Nul_before_a_digit_that_is_not_ASCII_is_NUL_then_that_digit", "DecimalEscapeTests.Nul_before_no_digit_is_NUL_in_every_mode"],
 "A-5.7": ["ValidateEntryTests.An_Annex_B_extension_refused_under_u_or_v_has_its_own_code_of_row_EG0002", "NamedReferenceTests.Without_a_group_name_k_is_the_letter", "NamedReferenceTests.In_a_class_without_u_and_v_k_is_the_letter_beside_a_group_name", "DecimalEscapeTests.Outside_Unicode_mode_a_legacy_octal_escape_is_its_code_unit", "DecimalEscapeTests.Under_u_and_v_a_legacy_octal_escape_is_a_syntax_error"],
 "A-6.1": ["DuplicateNamedGroupTests." + m for m in ["A_name_repeated_only_across_alternatives_that_cannot_both_participate_is_admitted", "A_name_repeated_where_both_groups_might_participate_is_refused_at_the_later_group", "The_rule_agrees_with_MightBothParticipate_over_a_seeded_population_of_trees", "A_name_is_compared_as_decoded_so_differently_spelled_names_that_decode_alike_are_one_name"]] + ["RegexGroupNamesTests.A_name_is_decoded_and_compared_by_code_unit"],
 "A-6.2": ["DuplicateNamedGroupTests." + m for m in ["A_reference_to_a_duplicated_name_matches_the_group_that_participated", "A_reference_to_a_duplicated_name_is_one_instruction_reading_every_group_of_the_name_at_one_unit", "Every_group_of_a_name_in_one_thousand_alternatives_is_read_by_its_reference"]],
 "A-6.3": ["DuplicateNamedGroupTests.A_result_maps_each_name_to_the_group_that_participated_and_lists_the_names_in_source_order", "DuplicateNamedGroupTests.The_participating_group_comes_from_the_offset_entry_s_slots", ("RegexGroupNamesTests", ["A_name_is_decoded_and_compared_by_code_unit", "Reading_the_participating_group_allocates_nothing"])],
 "A-7": ["PublicEntryTests.The_anchored_argument_is_independent_of_the_y_flag"],
 "A-8": ["PublicEntryTests.The_entry_s_signature_carries_no_string_and_answers_a_status", "PublicEntryTests.A_call_allocates_nothing_that_grows_with_the_capture_count", "PublicEntryTests.A_call_allocates_nothing_up_to_the_frames_and_trail_cells_the_pool_keeps_and_past_them_its_own_arrays", "EntryTests.A_call_allocates_nothing_that_grows_with_the_capture_count", "EntryTests.A_call_through_task_2_4b_s_instructions_allocates_nothing_that_grows_with_the_capture_count", "DuplicateNamedGroupTests.A_match_through_a_duplicated_name_and_its_reading_allocate_nothing", "RegexGroupNamesTests.Reading_the_participating_group_allocates_nothing"],
 "A-9.1": ["FlagsTextTests"], "A-9.2": ["EscapeStringTests"], "A-9.3": ["PatternSourceTests"],
 "A-10.1": ["ProviderDataTests.The_library_references_the_provider_alone_and_carries_no_table_or_data_version"],
 "A-10.2": ["ProviderDataTests.The_library_references_the_provider_alone_and_carries_no_table_or_data_version"],
 "A-10.3": ["ProviderDataTests.A_pattern_of_each_kind_reads_the_tables_through_the_callers_provider", "ProviderDataTests.A_match_reads_its_case_folding_from_the_callers_tables"],
 "A-10.4": ["ProviderDataTests.The_library_references_the_provider_alone_and_carries_no_table_or_data_version", "ProviderDataTests.A_provider_of_another_Unicode_version_is_refused_when_a_pattern_is_compiled"],
 "A-11.1": ["ValidateEntryTests.The_entry_checks_its_arguments_and_answers_no_exception_for_a_refusal", "ValidateEntryTests.The_validate_entry_answers_as_Compile_over_the_suite_and_seeded_populations", "GroupNameTests.An_escaped_surrogate_in_a_name_is_read_or_refused_never_thrown"],
 "A-11.2": ["ConcurrentUseTests", "FirstUseConcurrencyTests", "EntryTests.One_program_shared_by_concurrent_calls_answers_each_as_it_answers_alone", "CompileChargingTests.Concurrent_compilations_with_meters_and_budgets_answer_as_one_alone_does"],
 "A-11.3": [("ValidateEntryTests", VE_OTHER)],
}
def decides(clause, cls, method):
    for e in D[clause]:
        if isinstance(e, tuple):
            if cls == e[0] and method not in e[1]: return True
        elif "." in e:
            if e == cls + "." + method: return True
        elif e == cls: return True
    return False
