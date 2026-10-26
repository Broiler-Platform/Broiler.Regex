// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   6
// Annotated:        6/6
// Exempt:           0
// Human-reviewed:   0/6
// IP risk:          Low
// Security risk:    Medium
// Criteria:         3/0
// Resource impact:  1/10 max
// Unverified:       6
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

    // Each set below is built by CodePointSet.FromRanges, which normalizes it before it returns, so the type's
    // initialization publishes it already sorted and merged. Every compilation reads these sets: one left
    // un-normalized would be sorted and compacted in place, without a lock, by the first reads of a process's
    // first compilations on several threads at once (check A-11; FirstUseConcurrencyTests).

    /// <summary>Every code point, U+0000 to U+10FFFF: AllCharacters outside <c>v</c> with <c>i</c>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=BD7EDC
    // Broiler-Falsified-If: FullRange omits a code point between 0 and 0x10FFFF, such as the lone surrogate 0xD800, or is still un-normalized after type initialization, so the first concurrent class complements under v rewrite its shared range list without a lock
    // Broiler-Human:        PENDING
    internal static readonly CodePointSet FullRange = CodePointSet.FromRanges([(0, CodePointSet.MaxCodePoint)]);

    /// <summary><c>\d</c>, exactly ASCII <c>0-9</c> (§22.2.2.9).</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=E59EC0
    // Broiler-Falsified-If: DigitSet is still un-normalized after type initialization, so the first concurrent EscapeSet(Digit) or EscapeSet(NonDigit) calls rewrite its shared range list without a lock
    // Broiler-Human:        PENDING
    internal static readonly CodePointSet DigitSet = CodePointSet.FromRanges([('0', '9')]);

    /// <summary><c>\w</c> without <c>i</c> or without <c>u</c> and <c>v</c>: ASCII letters, digits and the low line.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=699099
    // Broiler-Falsified-If: BasicWordSet holds a code point outside A-Z, a-z, 0-9 and the low line, or is still un-normalized after type initialization, so two threads first compiling at once both sort and compact it while the folded word set is built from it, and \w loses digits or letters for the rest of the process
    // Broiler-Human:        PENDING
    internal static readonly CodePointSet BasicWordSet = CodePointSet.FromRanges([('0', '9'), ('A', 'Z'), ('_', '_'), ('a', 'z')]);
}
