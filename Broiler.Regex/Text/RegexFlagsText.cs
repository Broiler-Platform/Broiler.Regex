// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           4
// Human-reviewed:   0/9
// IP risk:          None
// Security risk:    High
// Criteria:         2/2
// Resource impact:  0/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.Regex.Text;

/// <summary>
/// A flags string read as a status, and the canonical flags text of a flags value: the flag half of requirement R9
/// (task 2.7 of SJL-11.2), which both engines' flag parsers move onto at their cut-overs (decision D20 of Broiler.VM's
/// JSD-0081).
/// </summary>
/// <remarks>
/// <para>
/// <b>No meter.</b> <see cref="Parse"/> decides by the ninth code unit at the latest, whatever the text's length: eight
/// distinct flags exist, so a ninth code unit is unknown or a repeat. Its work does not grow with its input, so it takes
/// no <see cref="RegexWorkCharge"/> (convention 4 asks one of an operation whose work does).
/// </para>
/// <para>
/// <b>The status</b> is decision D9's syntax status, the validate entry's: <see cref="RegexStatus.Success"/>, or
/// <see cref="RegexStatus.SyntaxError"/> with a <see cref="RegexDiagnostic"/> of one of the three codes of the flags,
/// <see cref="RegexDiagnosticCode.UnknownFlag"/> (1), <see cref="RegexDiagnosticCode.DuplicateFlag"/> (2) and
/// <see cref="RegexDiagnosticCode.UnicodeWithUnicodeSets"/> (3), its argument the code unit refused and its position in the
/// flags text. Until task 2.9 of SJL-11.2 it was an enum of its own, <c>RegexFlagsStatus</c>, with the same three members
/// at the same numbers; task 2.9 folded it into the codes, so a refusal of a flags text is one value whichever entry
/// meets it (a break of <c>0.1.0-preview.5</c>, listed with requirement R9 in <c>docs/suppressions.md</c>).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.3.3 RegExpInitialize; IP=None; Security=High; Resources=0; Fingerprint=C217B9
// Broiler-Falsified-If: a flags string that RegExpInitialize refuses, such as "gg", "x" or "uv", answers Success, or one it accepts answers a refusal
// Broiler-Human:        PENDING
public static class RegexFlagsText
{
    /// <summary>The letters of the eight flags in the order the <c>flags</c> getter writes them (ECMA-262 §22.2.6.4).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.6.4; IP=None; Security=Low; Resources=0; Fingerprint=8CCF14
    // Broiler-Human:        PENDING
    private const string CanonicalOrder = "dgimsuvy";

    /// <summary>Every flag <see cref="RegexFlags"/> defines.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=E62FA4
    // Broiler-Human:        PENDING
    private const RegexFlags Defined = RegexFlags.HasIndices | RegexFlags.Global | RegexFlags.IgnoreCase | RegexFlags.Multiline
        | RegexFlags.DotAll | RegexFlags.Unicode | RegexFlags.UnicodeSets | RegexFlags.Sticky;

    /// <summary>The two flags no pattern may carry together.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=224AA0
    // Broiler-Human:        PENDING
    private const RegexFlags BothModes = RegexFlags.Unicode | RegexFlags.UnicodeSets;

    /// <summary>
    /// Reads <paramref name="text"/> as a flags string: RegExpInitialize's checks of its flags (ECMA-262 §22.2.3.3,
    /// "If F contains any code unit other than "d", "g", "i", "m", "s", "u", "v", or "y", or if F contains any code unit
    /// more than once, throw a SyntaxError exception"), then ParsePattern's refusal of <c>u</c> with <c>v</c> (§22.2.3.4).
    /// A literal's flags are checked the same way (IsValidRegularExpressionLiteral, §13.2.7.2).
    /// </summary>
    /// <param name="text">The flags string, read by UTF-16 code units; an empty text sets no flag.</param>
    /// <param name="flags">The flags when the status is <see cref="RegexStatus.Success"/>; <see cref="RegexFlags.None"/> otherwise.</param>
    /// <param name="diagnostic">
    /// The refusal when the status is <see cref="RegexStatus.SyntaxError"/>, the default value otherwise: its code
    /// (<see cref="RegexDiagnosticCode.UnknownFlag"/>, <see cref="RegexDiagnosticCode.DuplicateFlag"/> or
    /// <see cref="RegexDiagnosticCode.UnicodeWithUnicodeSets"/>), its argument (the code unit refused, for the first two)
    /// and its position, the offset of the code unit the refusal names in code units of <paramref name="text"/>. The code
    /// units are read from the first: an unknown code unit or the second occurrence of a flag, whichever comes first, is
    /// refused there, and <c>u</c> with <c>v</c> only when neither occurs anywhere, at the later of the two.
    /// </param>
    /// <returns><see cref="RegexStatus.Success"/>, or <see cref="RegexStatus.SyntaxError"/>; never an exception.</returns>
    /// <remarks>
    /// A literal's flags are code points and a constructor's code units; a code point outside <c>d g i m s u v y</c> is
    /// unknown either way, and no flag is a surrogate, so the answer is the same in both readings, and the position is
    /// in code units. The work is at most nine code units (the type's remarks).
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.3.3 RegExpInitialize; IP=None; Security=High; Resources=0; Fingerprint=E771AD
    // Broiler-Falsified-If: "gg", "x", "G", "uv" or "uvx" answers Success, "uvx" answers UnicodeWithUnicodeSets rather than UnknownFlag at 2, or a refusal's position is not the code unit it names
    // Broiler-Human:        PENDING
    public static RegexStatus Parse(ReadOnlySpan<char> text, out RegexFlags flags, out RegexDiagnostic diagnostic)
    {
        var seen = RegexFlags.None;
        var lastMode = -1;
        for (var at = 0; at < text.Length; at++)
        {
            var flag = FlagOf(text[at]);
            if (flag == RegexFlags.None)
                return Refuse(RegexDiagnosticCode.UnknownFlag, text, at, out flags, out diagnostic);
            if ((seen & flag) != 0)
                return Refuse(RegexDiagnosticCode.DuplicateFlag, text, at, out flags, out diagnostic);

            seen |= flag;
            if ((flag & BothModes) != 0)
                lastMode = at;
        }

        if ((seen & BothModes) == BothModes)
            return Refuse(RegexDiagnosticCode.UnicodeWithUnicodeSets, text, lastMode, out flags, out diagnostic);

        flags = seen;
        diagnostic = default;
        return RegexStatus.Success;
    }

    /// <summary>
    /// The canonical text of <paramref name="flags"/>: the letter of each flag set, in the order the <c>flags</c> getter
    /// appends them, <c>d g i m s u v y</c> (ECMA-262 §22.2.6.4, get RegExp.prototype.flags), so that
    /// <c>new RegExp("x", "yg")</c>'s flags read <c>gy</c>.
    /// </summary>
    /// <param name="flags">Any combination of the eight flags <see cref="RegexFlags"/> defines; <c>u</c> with <c>v</c> is written as given.</param>
    /// <returns>The letters, or the empty string for <see cref="RegexFlags.None"/>.</returns>
    /// <remarks>
    /// The getter itself reads each flag's property from its receiver, which is the engine's object model (convention 2);
    /// this is its answer for a receiver whose properties are the flags of one regular expression.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="flags"/> holds a bit no member of <see cref="RegexFlags"/> defines: a broken precondition.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.6.4; IP=None; Security=Medium; Resources=0; Fingerprint=E5522E
    // Broiler-Human:        PENDING
    public static string Format(RegexFlags flags)
    {
        if ((flags & ~Defined) != 0)
            throw new ArgumentOutOfRangeException(nameof(flags), "A flags value holds only the bits RegexFlags defines.");

        Span<char> letters = stackalloc char[CanonicalOrder.Length];
        var count = 0;
        foreach (var letter in CanonicalOrder)
        {
            if ((flags & FlagOf(letter)) != 0)
                letters[count++] = letter;
        }

        return count == 0 ? string.Empty : new string(letters[..count]);
    }

    /// <summary>The flag <paramref name="letter"/> names, or <see cref="RegexFlags.None"/> for a code unit that names none.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.6.4; IP=None; Security=Medium; Resources=0; Fingerprint=3C0A77
    // Broiler-Human:        PENDING
    private static RegexFlags FlagOf(char letter) => letter switch
    {
        'd' => RegexFlags.HasIndices,
        'g' => RegexFlags.Global,
        'i' => RegexFlags.IgnoreCase,
        'm' => RegexFlags.Multiline,
        's' => RegexFlags.DotAll,
        'u' => RegexFlags.Unicode,
        'v' => RegexFlags.UnicodeSets,
        'y' => RegexFlags.Sticky,
        _ => RegexFlags.None,
    };

    /// <summary>A refusal of kind <paramref name="code"/>: no flags, and the diagnostic of the code unit at <paramref name="at"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=05A3FD
    // Broiler-Human:        PENDING
    private static RegexStatus Refuse(RegexDiagnosticCode code, ReadOnlySpan<char> text, int at, out RegexFlags flags, out RegexDiagnostic diagnostic)
    {
        flags = RegexFlags.None;
        diagnostic = RegexDiagnostic.OfFlags(code, text[at], at);
        return RegexStatus.SyntaxError;
    }
}
