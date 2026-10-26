// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           0
// Human-reviewed:   0/7
// IP risk:          Low
// Security risk:    Medium
// Criteria:         3/0
// Resource impact:  1/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.Regex.Ast;

namespace Broiler.Regex.Unicode;

/// <summary>
/// The parts of the built-in class escapes (<c>\d \D \w \W \s \S</c>, ECMA-262 §22.2.2.9) that are the
/// specification's own rather than Unicode data: ASCII digits, the basic word characters and the code space.
/// Everything a Unicode version decides - property escapes, case folding, white space, the folded word set and
/// <c>AllCharacters</c> under <c>v</c> with <c>i</c> - is <see cref="RegexUnicode"/>'s, from the Unicode owner's
/// provider (requirement R11).
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=Low; Resources=1; Fingerprint=9B8753
// Broiler-Human:        PENDING
internal static class UnicodeCharSets
{
    /// <summary>The positive counterpart of a complementing escape (<c>\D</c> → <c>\d</c>).</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=F98DB8
    // Broiler-Human:        PENDING
    internal static ClassEscape PositiveEscape(ClassEscape escape) => escape switch
    {
        ClassEscape.NonDigit => ClassEscape.Digit,
        ClassEscape.NonWord => ClassEscape.Word,
        ClassEscape.NonSpace => ClassEscape.Space,
        _ => escape,
    };

    /// <summary>True for <c>\D</c>, <c>\W</c> and <c>\S</c>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=3FAC0B
    // Broiler-Human:        PENDING
    internal static bool IsComplementEscape(ClassEscape escape)
        => escape is ClassEscape.NonDigit or ClassEscape.NonWord or ClassEscape.NonSpace;

    // ----- Built-in sets ------------------------------------------------------

    /// <summary>Every code point, U+0000 to U+10FFFF: AllCharacters outside <c>v</c> with <c>i</c>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=B39463
    // Broiler-Falsified-If: FullRange omits a code point between 0 and 0x10FFFF, such as the lone surrogate 0xD800
    // Broiler-Human:        PENDING
    internal static readonly CodePointSet FullRange = CodePointSet.Of(0, CodePointSet.MaxCodePoint);

    /// <summary><c>\d</c>, exactly ASCII <c>0-9</c> (§22.2.2.9).</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=66FCA5
    // Broiler-Falsified-If: DigitSet is still un-normalized after type initialization, so the first concurrent EscapeSet(Digit) or EscapeSet(NonDigit) calls rewrite its shared range list without a lock
    // Broiler-Human:        PENDING
    internal static readonly CodePointSet DigitSet = CodePointSet.Of('0', '9');

    /// <summary><c>\w</c> without <c>i</c> or without <c>u</c> and <c>v</c>: ASCII letters, digits and the low line.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=09B039
    // Broiler-Human:        PENDING
    internal static readonly CodePointSet BasicWordSet = BuildBasicWordSet();

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=1DC1FD
    // Broiler-Falsified-If: the set it builds contains a code point outside A-Z, a-z, 0-9 and the low line
    // Broiler-Human:        PENDING
    private static CodePointSet BuildBasicWordSet()
    {
        var set = new CodePointSet();
        set.AddRange('a', 'z');
        set.AddRange('A', 'Z');
        set.AddRange('0', '9');
        set.AddCodePoint('_');
        return set;
    }
}
