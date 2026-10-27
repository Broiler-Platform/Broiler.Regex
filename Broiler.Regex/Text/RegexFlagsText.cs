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
/// How reading a flags string ended (<see cref="RegexFlagsText.Parse"/>): the flags, or which of the three refusals
/// ECMA-262 makes of a flags string. Each refusal is a <c>SyntaxError</c> in both engines, and the member names it, so
/// that an engine renders its own message from it (decision D9 of Broiler.VM's JSD-0081, a status with a code and a
/// position in place of an exception; decision SJL-D-11, each engine's text its own).
/// </summary>
/// <remarks>
/// <b>Provisional</b> (task 2.7 of SJL-11.2): task 2.8 gives the validate entry decision D9's syntax status, and task
/// 2.9, which fixes the public surface of <c>0.2.0-preview.1</c>, may fold these three refusals into it.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.3.3 RegExpInitialize; IP=None; Security=Low; Resources=0; Fingerprint=645656
// Broiler-Human:        PENDING
public enum RegexFlagsStatus
{
    /// <summary>Every code unit is one of <c>d g i m s u v y</c>, none twice, and not <c>u</c> with <c>v</c>: the flags are set.</summary>
    Success = 0,

    /// <summary>
    /// A code unit that is not one of <c>d g i m s u v y</c> (RegExpInitialize, ECMA-262 §22.2.3.3, its first refusal;
    /// for a literal, IsValidRegularExpressionLiteral, §13.2.7.2). The position is that code unit's.
    /// </summary>
    UnknownFlag = 1,

    /// <summary>A flag written a second time (the same step's second refusal). The position is the second occurrence's.</summary>
    DuplicateFlag = 2,

    /// <summary>
    /// <c>u</c> and <c>v</c> together, each once and nothing else wrong (ParsePattern, §22.2.3.4, which refuses the pair
    /// after every other flag is read). The position is the later of the two.
    /// </summary>
    UnicodeWithUnicodeSets = 3,
}

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
/// <b>Provisional</b>, with every member of <c>Broiler.Regex.Text</c>: task 2.9 fixes the public surface.
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
    /// <param name="flags">The flags when the status is <see cref="RegexFlagsStatus.Success"/>; <see cref="RegexFlags.None"/> otherwise.</param>
    /// <param name="position">
    /// The offset of the code unit the refusal names, in code units of <paramref name="text"/>; -1 on success. The code
    /// units are read from the first: an unknown code unit or the second occurrence of a flag, whichever comes first, is
    /// refused there, and <c>u</c> with <c>v</c> only when neither occurs anywhere, at the later of the two.
    /// </param>
    /// <returns><see cref="RegexFlagsStatus.Success"/>, or the refusal; never an exception.</returns>
    /// <remarks>
    /// A literal's flags are code points and a constructor's code units; a code point outside <c>d g i m s u v y</c> is
    /// unknown either way, and no flag is a surrogate, so the answer is the same in both readings, and the position is
    /// in code units. The work is at most nine code units (the type's remarks).
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.3.3 RegExpInitialize; IP=None; Security=High; Resources=0; Fingerprint=E771AD
    // Broiler-Falsified-If: "gg", "x", "G", "uv" or "uvx" answers Success, "uvx" answers UnicodeWithUnicodeSets rather than UnknownFlag at 2, or a refusal's position is not the code unit it names
    // Broiler-Human:        PENDING
    public static RegexFlagsStatus Parse(ReadOnlySpan<char> text, out RegexFlags flags, out int position)
    {
        var seen = RegexFlags.None;
        var lastMode = -1;
        for (var at = 0; at < text.Length; at++)
        {
            var flag = FlagOf(text[at]);
            if (flag == RegexFlags.None)
                return Refuse(RegexFlagsStatus.UnknownFlag, at, out flags, out position);
            if ((seen & flag) != 0)
                return Refuse(RegexFlagsStatus.DuplicateFlag, at, out flags, out position);

            seen |= flag;
            if ((flag & BothModes) != 0)
                lastMode = at;
        }

        if ((seen & BothModes) == BothModes)
            return Refuse(RegexFlagsStatus.UnicodeWithUnicodeSets, lastMode, out flags, out position);

        flags = seen;
        position = -1;
        return RegexFlagsStatus.Success;
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

    /// <summary>A refusal: no flags, and the position it names.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=05A3FD
    // Broiler-Human:        PENDING
    private static RegexFlagsStatus Refuse(RegexFlagsStatus status, int at, out RegexFlags flags, out int position)
    {
        flags = RegexFlags.None;
        position = at;
        return status;
    }
}
