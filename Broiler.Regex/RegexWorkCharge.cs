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
/// <b>Internal until it has a public operation.</b> Task 2.4a's match entry takes it, and that entry stays internal until
/// task 2.4c routes <see cref="BroilerRegex"/> through it, so nothing public names the meter yet; it becomes public with
/// the first public member that takes it, under this name and shape. The meter is not a cancellation token and the library
/// never polls a clock: cancellation reaches an operation through the meter, by throwing.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D9; IP=None; Security=Medium; Resources=0; Fingerprint=100FDD
// Broiler-Human:        PENDING
internal delegate void RegexWorkCharge(ulong units);
