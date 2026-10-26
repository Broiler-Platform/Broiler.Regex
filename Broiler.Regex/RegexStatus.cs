// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           4
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
/// nesting limit (<see cref="BroilerRegex.Compile"/>). The later steps add members and keep these: the new matcher's
/// <c>Match</c> and <c>NoMatch</c> (task 2.4a), and the syntax status with its code, message identifier and position
/// (the validate entry of task 2.8). A syntax error from <see cref="BroilerRegex.Compile"/> is still a
/// <see cref="RegexSyntaxException"/>.
/// </para>
/// <para>
/// <b>A member D9 does not list.</b> Decision D9's statuses are a match, no match, a limit exceeded naming its
/// dimension, and a syntax status: the answers of a match and of a validation. A compilation that completes is none of
/// them, so <see cref="Success"/> is added for it, named and numbered as the Unicode owner's <c>UnicodeStatus.Success</c>
/// is, as <see cref="LimitExceeded"/> is. Compiling answers <see cref="Success"/> or <see cref="LimitExceeded"/>; a match
/// will answer <c>Match</c>, <c>NoMatch</c> or <see cref="LimitExceeded"/>, and never <see cref="Success"/>.
/// </para>
/// <para>
/// <b>Not an exception.</b> A status is how the library reports a failure its contract admits. It throws only when
/// its caller breaks a documented precondition, such as a nesting limit outside the documented range, and a meter of
/// the caller's that throws propagates its own exception (convention 3).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D9; IP=None; Security=Low; Resources=0; Fingerprint=5AD0EA
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
}

/// <summary>
/// The dimension a <see cref="RegexStatus.LimitExceeded"/> names: which of the caller's limits was reached (decision D9
/// of Broiler.VM's JSD-0081; convention 5).
/// </summary>
/// <remarks>
/// Task 2.3 of SJL-11.2 introduced <see cref="Nesting"/>. The new matcher's step, frame and trail ceilings (task 2.4a)
/// and the compile budget (task 2.5) join it as members of their own; none changes the members here.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D9; IP=None; Security=Low; Resources=0; Fingerprint=B88E60
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
}
