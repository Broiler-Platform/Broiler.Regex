// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   4
// Annotated:        4/4
// Exempt:           9
// Human-reviewed:   0/4
// IP risk:          Low
// Security risk:    High
// Criteria:         4/2
// Resource impact:  1/10 max
// Unverified:       4
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.Regex;

/// <summary>
/// The ECMAScript regular-expression flags (ECMA-262 §22.2.6 "Flags").
/// Mirrors the flag letters <c>d g i m s u v y</c>.
/// </summary>
// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=FCF95D
// Broiler-Falsified-If: two members share a bit value, so a parsed flags string such as "gy" reads back as a different set of letters
// Broiler-Human:        PENDING
[Flags]
public enum RegexFlags
{
    None = 0,

    /// <summary><c>d</c> — generate match indices (<c>hasIndices</c>).</summary>
    HasIndices = 1 << 0,

    /// <summary><c>g</c> — global; consult/advance <c>lastIndex</c> across calls.</summary>
    Global = 1 << 1,

    /// <summary><c>i</c> — case-insensitive matching.</summary>
    IgnoreCase = 1 << 2,

    /// <summary><c>m</c> — multiline; <c>^</c>/<c>$</c> also match at line terminators.</summary>
    Multiline = 1 << 3,

    /// <summary><c>s</c> — dotAll; <c>.</c> also matches line terminators.</summary>
    DotAll = 1 << 4,

    /// <summary><c>u</c> — Unicode mode (code-point semantics).</summary>
    Unicode = 1 << 5,

    /// <summary><c>y</c> — sticky; anchor the match at <c>lastIndex</c>.</summary>
    Sticky = 1 << 6,

    /// <summary><c>v</c> — Unicode-sets mode (implies code-point semantics + set ops).</summary>
    UnicodeSets = 1 << 7,
}

/// <summary>
/// Parses and validates an ECMAScript flags string (RegExpInitialize, ECMA-262 §22.2.3.3, and ParsePattern's refusal of
/// <c>u</c> with <c>v</c>, §22.2.3.4), throwing for a string they refuse. Each flag may appear at most once; <c>u</c> and
/// <c>v</c> are mutually exclusive; an unknown letter is a syntax error.
/// </summary>
/// <remarks>
/// The reader of <c>0.1.0-preview.4</c>, kept with its answers and its messages. Since task 2.7 of SJL-11.2 it reads the
/// grammar of <see cref="Text.RegexFlagsText.Parse"/>, which answers the same refusals as a status with a position and no
/// exception (convention 3); task 2.9, which fixes the public surface, decides this member's future.
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=8E3CEF
// Broiler-Falsified-If: a flags string holding a letter outside d, g, i, m, s, u, v and y, such as "x", is accepted instead of throwing RegexSyntaxException
// Broiler-Human:        PENDING
public static class RegexFlagsParser
{
    /// <summary>The flags <paramref name="flags"/> sets, or a <see cref="RegexSyntaxException"/> for a string ECMA-262 refuses.</summary>
    /// <param name="flags">The flags string; null or empty sets no flag.</param>
    /// <returns>The flags.</returns>
    /// <exception cref="RegexSyntaxException">
    /// A code unit outside <c>d g i m s u v y</c>, a flag written twice, or <c>u</c> with <c>v</c>, with the message
    /// <c>0.1.0-preview.4</c> gave each and no position.
    /// </exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.3.3 RegExpInitialize; IP=Low; Security=High; Resources=1; Fingerprint=D31393
    // Broiler-Falsified-If: the flags string "uv" (or "vu") returns Unicode | UnicodeSets instead of throwing RegexSyntaxException
    // Broiler-Human:        PENDING
    public static RegexFlags Parse(string? flags)
    {
        var status = Text.RegexFlagsText.Parse(flags, out var result, out var position);
        return status switch
        {
            Text.RegexFlagsStatus.Success => result,
            Text.RegexFlagsStatus.UnknownFlag => throw new RegexSyntaxException("Invalid regular expression flags: '" + flags![position] + "'"),
            Text.RegexFlagsStatus.DuplicateFlag => throw new RegexSyntaxException("Duplicate regular expression flag: '" + flags![position] + "'"),
            _ => throw new RegexSyntaxException("The 'u' and 'v' regular expression flags cannot be combined"),
        };
    }

    /// <summary>True when either <c>u</c> or <c>v</c> is set (both imply code-point semantics).</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=B0612A
    // Broiler-Falsified-If: IsUnicodeMode(RegexFlags.UnicodeSets) returns false, so a v-mode pattern is parsed and matched per UTF-16 code unit
    // Broiler-Human:        PENDING
    public static bool IsUnicodeMode(this RegexFlags flags)
        => (flags & (RegexFlags.Unicode | RegexFlags.UnicodeSets)) != 0;
}
