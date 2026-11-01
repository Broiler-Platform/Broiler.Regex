// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   3
// Annotated:        3/3
// Exempt:           2
// Human-reviewed:   0/3
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       3
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.Regex;

/// <summary>
/// A refusal of a pattern or a flags text, as the parser and the flags reader throw it inside the library, with its
/// <see cref="Diagnostic"/>. It is internal since task 2.9 of SJL-11.2: the members that answer a status,
/// <see cref="BroilerRegex.Compile"/>, the validate entry and <see cref="Text.RegexFlagsText.Parse"/>, catch it and answer
/// its diagnostic as decision D9's syntax status; the string-bearing constructors, which decision D12 keeps and which have
/// no status to answer, let it reach their caller as what it derives from, an <see cref="ArgumentException"/>: a pattern
/// or flags text the specification refuses breaks the precondition those constructors document (convention 3).
/// </summary>
/// <remarks>
/// Until task 2.9 it was public and derived from <see cref="Exception"/>: the library's own type for a failure its contract
/// admits, which convention 3 refuses (check C3, a row of the starting violation list until then). Its public members,
/// the position and the diagnostic, are the validate entry's answer for the same pattern, which a caller that needs them
/// reads there. A break of <c>0.1.0-preview.5</c>, listed with requirement R10 in <c>docs/suppressions.md</c>.
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=B41B30
// Broiler-Human:        PENDING
internal sealed class RegexSyntaxException(string message, int position = -1) : ArgumentException(message)
{
    /// <summary>
    /// A refusal the library made: its <paramref name="diagnostic"/>'s message and position, the position -1 for an error of
    /// the flags, whose place in the flags text the diagnostic gives.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D9; IP=None; Security=Low; Resources=0; Fingerprint=C7FDD8
    // Broiler-Human:        PENDING
    internal RegexSyntaxException(RegexDiagnostic diagnostic)
        : this(diagnostic.Message, IsOfFlags(diagnostic.Code) ? -1 : diagnostic.Position)
    {
        Diagnostic = diagnostic;
    }

    /// <summary>Zero-based offset into the pattern where the error was detected, or -1.</summary>
    internal int Position { get; } = position;

    /// <summary>
    /// The refusal as data: its code, message identifier, argument and position (decision D9 of Broiler.VM's JSD-0081). Its
    /// code is <see cref="RegexDiagnosticCode.None"/> for an exception made from a message alone.
    /// </summary>
    internal RegexDiagnostic Diagnostic { get; }

    /// <summary>Whether <paramref name="code"/> is one of the flags', whose position is in the flags text.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=0093C7
    // Broiler-Human:        PENDING
    private static bool IsOfFlags(RegexDiagnosticCode code) =>
        code is RegexDiagnosticCode.UnknownFlag or RegexDiagnosticCode.DuplicateFlag or RegexDiagnosticCode.UnicodeWithUnicodeSets;
}
