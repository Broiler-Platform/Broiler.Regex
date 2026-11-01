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
/// Thrown when a pattern or flags string is not a valid ECMAScript regular
/// expression. The JavaScript layer maps this to a <c>SyntaxError</c>.
/// </summary>
/// <remarks>
/// Since task 2.8 of SJL-11.2 every refusal the parser and the flags parser throw carries its <see cref="Diagnostic"/>, the
/// code, message identifier, argument and position that the validate entry,
/// <see cref="BroilerRegex.Validate(string, RegexFlags, Broiler.Unicode.Algorithms.IUnicodeTables, int, out RegexDiagnostic, out RegexLimit)"/>,
/// answers as a status for the same pattern. The exception stays the answer of <see cref="BroilerRegex.Compile"/>, the
/// constructors, <see cref="Parsing.RegexParser.Parse(string, RegexFlags, Broiler.Unicode.Algorithms.IUnicodeTables, out int)"/> and <see cref="RegexFlagsParser.Parse"/> until task 2.9 fixes the
/// public surface (convention 3 makes an expected failure a status; the record of task 2.8 in <c>docs/roadmap.md</c>).
/// One refusal carries none: the public <see cref="Ast.CharSet.AddRange"/>'s of a range written high to low, which no
/// compilation reaches (the parser refuses such a range first) and which task 2.9 owns with the public <c>Ast</c> surface.
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=B41B30
// Broiler-Human:        PENDING
public sealed class RegexSyntaxException(string message, int position = -1) : Exception(message)
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
    public int Position { get; } = position;

    /// <summary>
    /// The refusal as data: its code, message identifier, argument and position (decision D9 of Broiler.VM's JSD-0081). Its
    /// code is <see cref="RegexDiagnosticCode.None"/> for an exception made through the public constructor. Provisional, with
    /// the validate entry (task 2.9 fixes the public surface).
    /// </summary>
    public RegexDiagnostic Diagnostic { get; }

    /// <summary>Whether <paramref name="code"/> is one of the flags', whose position is in the flags text.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=0093C7
    // Broiler-Human:        PENDING
    private static bool IsOfFlags(RegexDiagnosticCode code) =>
        code is RegexDiagnosticCode.UnknownFlag or RegexDiagnosticCode.DuplicateFlag or RegexDiagnosticCode.UnicodeWithUnicodeSets;
}
