// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           4
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    Medium
// Criteria:         6/0
// Resource impact:  1/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Collections.Generic;
using Broiler.Regex.Ast;

namespace Broiler.Regex.Unicode;

/// <summary>
/// The parts of the built-in class escapes (<c>\d \D \w \W \s \S</c>, ECMA-262 §22.2.2.9 CompileToCharSet) that are
/// the specification's own rather than Unicode data: ASCII digits, the basic word characters and the code space;
/// and <see cref="PropertyMatch"/>, which holds the resolution of one <c>\p{…}</c> escape.
/// Everything a Unicode version decides - property escapes, case folding, white space, the folded word set and
/// <c>AllCharacters</c> under <c>v</c> with <c>i</c> - is <see cref="RegexUnicode"/>'s, from the Unicode owner's
/// provider (requirement R11).
/// </summary>
/// <remarks>
/// The public members are those of <c>0.1.0-preview.4</c> that read no Unicode table, which R11 does not force
/// out (decision D12: the 0.1 surface is kept where R11 and R8 allow it; task 2.9 decides the rest). The members
/// that needed the old packages' tables - <c>AllCharacters</c>, <c>EscapeSet</c>, <c>IsSpace</c>, <c>IsWord</c>,
/// <c>ResolveProperty</c> and <c>WordSet</c> - are removed, as <c>docs/suppressions.md</c> lists.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=Low; Resources=1; Fingerprint=7E3895
// Broiler-Human:        PENDING
public static class UnicodeCharSets
{
    /// <summary>
    /// Whether <paramref name="cp"/> is a member of <c>\d</c>: exactly the ASCII digits <c>0-9</c>
    /// (ECMA-262 §22.2.2.9, CompileToCharSet of <c>CharacterClassEscape :: d</c>).
    /// </summary>
    /// <param name="cp">A code point.</param>
    /// <returns>True for U+0030 to U+0039 alone.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=None; Security=Low; Resources=0; Fingerprint=0B019E
    // Broiler-Falsified-If: IsDigit(0x0660) returns true, treating ARABIC-INDIC DIGIT ZERO as a \d member
    // Broiler-Human:        PENDING
    public static bool IsDigit(int cp) => cp >= '0' && cp <= '9';

    /// <summary>
    /// The positive counterpart of a complementing escape (<c>\D</c> → <c>\d</c>), whose CharSet a complementing
    /// escape complements (ECMA-262 §22.2.2.9, CompileToCharSet of <c>CharacterClassEscape</c>).
    /// </summary>
    /// <param name="escape">A built-in class escape.</param>
    /// <returns><c>\d</c>, <c>\w</c> or <c>\s</c> for their complements, and any other escape itself.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=Low; Resources=0; Fingerprint=3982FE
    // Broiler-Human:        PENDING
    public static ClassEscape PositiveEscape(ClassEscape escape) => escape switch
    {
        ClassEscape.NonDigit => ClassEscape.Digit,
        ClassEscape.NonWord => ClassEscape.Word,
        ClassEscape.NonSpace => ClassEscape.Space,
        _ => escape,
    };

    /// <summary>
    /// True for <c>\D</c>, <c>\W</c> and <c>\S</c>, the escapes whose CharSet is a CharacterComplement
    /// (ECMA-262 §22.2.2.9 and §22.2.2.9.6).
    /// </summary>
    /// <param name="escape">A built-in class escape.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=None; Security=Low; Resources=0; Fingerprint=646555
    // Broiler-Human:        PENDING
    public static bool IsComplementEscape(ClassEscape escape)
        => escape is ClassEscape.NonDigit or ClassEscape.NonWord or ClassEscape.NonSpace;

    /// <summary>
    /// The resolution of one <c>\p{…}</c> escape (ECMA-262 §22.2.2.9, CompileToCharSet of
    /// <c>UnicodePropertyValueExpression</c>; §22.2.2.9.7 UnicodeMatchProperty and §22.2.2.9.8
    /// UnicodeMatchPropertyValue): its code points, and its strings when it is a property of strings.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=Medium; Resources=0; Fingerprint=D1335C
    // Broiler-Falsified-If: a resolution the library builds for a property of strings reports IsStringProperty false, so \p{RGI_Emoji} is accepted outside v or \P{RGI_Emoji} under v
    // Broiler-Human:        PENDING
    public sealed class PropertyMatch(CodePointSet codePoints, IReadOnlyList<string> strings)
    {
        /// <summary>
        /// A resolution whose kind is stated: <paramref name="isStringProperty"/> is true for one of ECMA-262's
        /// binary properties of strings (Table 71) whatever its sequences hold.
        /// </summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=2FC543
        // Broiler-Falsified-If: a match built with isStringProperty true and no sequence reports IsStringProperty false
        // Broiler-Human:        PENDING
        internal PropertyMatch(CodePointSet codePoints, IReadOnlyList<string> strings, bool isStringProperty)
            : this(codePoints, strings)
            => _isStringProperty = isStringProperty;

        private readonly bool _isStringProperty = strings.Count > 0;

        /// <summary>The code points the property matches, a set no other resolution shares.</summary>
        public readonly CodePointSet CodePoints = codePoints;

        /// <summary>
        /// The sequences of two or more code points a property of strings (UTS #51) matches; empty for an ordinary
        /// property.
        /// </summary>
        public readonly IReadOnlyList<string> Strings = strings;

        /// <summary>
        /// Whether the escape names one of ECMA-262's binary properties of strings, which may be used only in a
        /// <c>v</c>-mode class and never negated (§22.2.1.1, the early errors of UnicodePropertyValueExpression and
        /// <c>CharacterClassEscape :: P{ … }</c>). A match built by the public constructor answers whether it holds
        /// a sequence, as <c>0.1.0-preview.4</c> did.
        /// </summary>
        public bool IsStringProperty => _isStringProperty;
    }

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
