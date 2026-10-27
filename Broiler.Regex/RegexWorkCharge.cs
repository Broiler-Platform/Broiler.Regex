// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           0
// Human-reviewed:   0/1
// IP risk:          None
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       1
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.Regex;

/// <summary>
/// The caller's meter (decision D9 of Broiler.VM's JSD-0081; the programme's convention 4): the library hands it the
/// units of work it is about to do, in blocks, before it does them, and the caller counts them against its own budget.
/// A meter that throws stops the operation, and its exception reaches the caller unchanged; the library leaves no shared
/// state changed, so the same compiled pattern answers a new call as a new one would.
/// </summary>
/// <param name="units">The units about to be spent: a whole number of blocks (decision D8; the new backend's block is 512 units).</param>
/// <remarks>
/// <para>
/// <b>Public since task 2.4c, provisional.</b> The offset entry,
/// <see cref="BroilerRegex.Match(System.ReadOnlySpan{char}, int, bool, System.Span{int}, RegexWorkCharge?, Engine.RegexMatchCeilings, out RegexLimit)"/>,
/// takes it; task 2.5 gives <see cref="BroilerRegex.Compile"/> the same meter for compilation (requirement R2), and task
/// 2.9 fixes the public surface of <c>0.2.0-preview.1</c>, so its name and shape may still change before that version
/// ships. The meter is not a cancellation token and the library never polls a clock: cancellation reaches an operation
/// through the meter, by throwing.
/// </para>
/// <para>
/// <b>When it is called.</b> Before the work it pays for, in whole blocks, from the thread that calls the operation, so
/// that before the k-th unit of work at least k units have been charged and never a block more than the work: a
/// match's units run on across all its start positions, with no reset at each.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D9; IP=None; Security=Medium; Resources=0; Fingerprint=87B1DC
// Broiler-Human:        PENDING
public delegate void RegexWorkCharge(ulong units);
