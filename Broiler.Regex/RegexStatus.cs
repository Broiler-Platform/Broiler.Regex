// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           9
// Human-reviewed:   0/2
// IP risk:          None
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.Regex;

/// <summary>
/// How an operation that can refuse its input ended: the status of decision D9 of Broiler.VM's JSD-0081, which the
/// programme's convention 3 makes the answer to every expected failure, in place of an exception.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is here today.</b> SJL-11.2 builds the status contract step by step, and this enum holds what its steps
/// have needed so far: <see cref="Success"/> and <see cref="LimitExceeded"/>, which task 2.3 introduced for the caller's
/// nesting limit (<see cref="BroilerRegex.Compile"/>), and <see cref="Match"/> and <see cref="NoMatch"/>, which task 2.4a
/// added for the new backend's match entry. That entry is internal until task 2.4c routes <see cref="BroilerRegex"/>
/// through it; the two members are public now because an enum's members have the enum's accessibility, and the entry
/// answers this enum rather than one of its own. The later steps add members and keep these: the syntax status with
/// its code, message identifier and position (the validate entry of task 2.8). A syntax error from
/// <see cref="BroilerRegex.Compile"/> is still a <see cref="RegexSyntaxException"/>, and the signature of
/// <see cref="BroilerRegex.Compile"/> is provisional: task 2.5 replaces it to take the caller's meter, and a syntax
/// status needs room for its code, message identifier and position (its remarks). The members here are kept whatever
/// replaces it, and their numbers are fixed.
/// </para>
/// <para>
/// <b>A member D9 does not list.</b> Decision D9's statuses are a match, no match, a limit exceeded naming its
/// dimension, and a syntax status: the answers of a match and of a validation. A compilation that completes is none of
/// them, so <see cref="Success"/> is added for it, named and numbered as the Unicode owner's <c>UnicodeStatus.Success</c>
/// is, as <see cref="LimitExceeded"/> is. Compiling answers <see cref="Success"/> or <see cref="LimitExceeded"/>; a match
/// answers <see cref="Match"/>, <see cref="NoMatch"/> or <see cref="LimitExceeded"/>, and never <see cref="Success"/>.
/// </para>
/// <para>
/// <b>Not an exception.</b> A status is how the library reports a failure its contract admits. It throws only when
/// its caller breaks a documented precondition, such as a nesting limit outside the documented range, and a meter of
/// the caller's that throws propagates its own exception (convention 3).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D9; IP=None; Security=Low; Resources=0; Fingerprint=F52169
// Broiler-Human:        PENDING
public enum RegexStatus
{
    /// <summary>The operation completed, and its result is set: a compilation's compiled pattern.</summary>
    Success = 0,

    /// <summary>
    /// A limit of the caller's policy was reached before the operation completed, and no result is set. The limit is
    /// named by a <see cref="RegexLimit"/> beside the status (convention 5).
    /// </summary>
    LimitExceeded = 1,

    /// <summary>
    /// A match was found (ECMA-262 §22.2.2.1, a MatchState): its offsets are in the caller's slots, two per capture group,
    /// group 0 first, -1 for a group that did not participate.
    /// </summary>
    Match = 2,

    /// <summary>
    /// No match was found at any position the call tried: the start index alone when the match is anchored, and every
    /// position from it to the end of the subject otherwise (§22.2.7.2 RegExpBuiltinExec's loop).
    /// </summary>
    NoMatch = 3,
}

/// <summary>
/// The dimension a <see cref="RegexStatus.LimitExceeded"/> names: which of the caller's limits was reached (decision D9
/// of Broiler.VM's JSD-0081; convention 5).
/// </summary>
/// <remarks>
/// Task 2.3 of SJL-11.2 introduced <see cref="Nesting"/>, and task 2.4a the new backend's three ceilings,
/// <see cref="Steps"/>, <see cref="Frames"/> and <see cref="Trail"/> (requirements R1 and R3, decisions D8 to D10), public
/// for the reason <see cref="RegexStatus"/>'s remarks give although the entry that answers them is internal until task
/// 2.4c. The compile budget (task 2.5) joins them as a member of its own; none changes the members here.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D9; IP=None; Security=Low; Resources=0; Fingerprint=094E76
// Broiler-Human:        PENDING
public enum RegexLimit
{
    /// <summary>No limit: the status is not <see cref="RegexStatus.LimitExceeded"/>.</summary>
    None = 0,

    /// <summary>
    /// The caller's nesting limit: the pattern nests deeper than it, counted as <see cref="BroilerRegex.Compile"/>
    /// counts (requirement R4, decision D6).
    /// </summary>
    Nesting = 1,

    /// <summary>
    /// The caller's step ceiling: the match would do more units of work than it admits, counted in the units the meter is
    /// charged in (requirement R1, decision D8).
    /// </summary>
    Steps = 2,

    /// <summary>
    /// The caller's frame ceiling: the match would hold more backtrack points at once than it admits (requirement R3,
    /// decision D10).
    /// </summary>
    Frames = 3,

    /// <summary>
    /// The caller's trail ceiling: the match would hold more undoable register writes at once than it admits
    /// (requirement R3, decision D10).
    /// </summary>
    Trail = 4,
}
