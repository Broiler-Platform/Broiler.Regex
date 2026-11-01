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
/// <see cref="Parse"/> is the reader of <c>0.1.0-preview.4</c>, kept with its answers and its messages; since task 2.7 of
/// SJL-11.2 it reads the grammar of <see cref="Text.RegexFlagsText.Parse"/>, and since task 2.9 it is internal: it throws
/// for a flags string the specification refuses, an expected failure that convention 3 makes a status, which
/// <see cref="Text.RegexFlagsText.Parse"/> answers with the diagnostic. <see cref="IsUnicodeMode"/>, which reads a flags
/// value and can fail for none, stays public.
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=8E3CEF
// Broiler-Falsified-If: a flags string holding a letter outside d, g, i, m, s, u, v and y, such as "x", is accepted instead of throwing RegexSyntaxException
// Broiler-Human:        PENDING
public static class RegexFlagsParser
{
    /// <summary>
    /// The flags <paramref name="flags"/> sets, or a <see cref="RegexSyntaxException"/> for a string ECMA-262 refuses: the
    /// reader the constructor over a flags text calls, internal since task 2.9 of SJL-11.2 (the type's remarks).
    /// </summary>
    /// <param name="flags">The flags string; null or empty sets no flag.</param>
    /// <returns>The flags.</returns>
    /// <exception cref="RegexSyntaxException">
    /// A code unit outside <c>d g i m s u v y</c>, a flag written twice, or <c>u</c> with <c>v</c>, with the message
    /// <c>0.1.0-preview.4</c> gave each and no position in the pattern; since task 2.8 of SJL-11.2 its
    /// <see cref="RegexSyntaxException.Diagnostic"/> names the refusal and its place in the flags text.
    /// </exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.3.3 RegExpInitialize; IP=Low; Security=High; Resources=1; Fingerprint=2217E0
    // Broiler-Falsified-If: the flags string "uv" (or "vu") returns Unicode | UnicodeSets instead of throwing RegexSyntaxException
    // Broiler-Human:        PENDING
    internal static RegexFlags Parse(string? flags)
    {
        return Text.RegexFlagsText.Parse(flags, out var result, out var diagnostic) == RegexStatus.Success ? result : throw new RegexSyntaxException(diagnostic);
    }

    /// <summary>
    /// True when either <c>u</c> or <c>v</c> is set (both imply code-point semantics): the pattern's Unicode mode, in which
    /// AdvanceStringIndex steps over a surrogate pair (ECMA-262 §22.2.7.3) and the pattern is read by code points
    /// (§22.2.3.4, ParsePattern).
    /// </summary>
    /// <param name="flags">The flags.</param>
    /// <returns>Whether <see cref="RegexFlags.Unicode"/> or <see cref="RegexFlags.UnicodeSets"/> is set.</returns>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=B0612A
    // Broiler-Falsified-If: IsUnicodeMode(RegexFlags.UnicodeSets) returns false, so a v-mode pattern is parsed and matched per UTF-16 code unit
    // Broiler-Human:        PENDING
    public static bool IsUnicodeMode(this RegexFlags flags)
        => (flags & (RegexFlags.Unicode | RegexFlags.UnicodeSets)) != 0;
}
