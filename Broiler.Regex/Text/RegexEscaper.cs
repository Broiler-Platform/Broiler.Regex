// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   22
// Annotated:        22/22
// Exempt:           5
// Human-reviewed:   0/22
// IP risk:          None
// Security risk:    High
// Criteria:         9/9
// Resource impact:  2/10 max
// Unverified:       22
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.Unicode.Algorithms;

namespace Broiler.Regex.Text;

/// <summary>
/// The text half of requirement R9 (task 2.7 of SJL-11.2): <c>RegExp.escape</c>'s text, a string escaped for
/// representation inside a pattern (<see cref="EscapeString"/>). Both engines' escape helpers move onto it at their
/// cut-overs (decision D20 of Broiler.VM's JSD-0081).
/// </summary>
/// <remarks>
/// <para>
/// <b>Data.</b> White space is the Unicode owner's, read through <see cref="EcmaScriptLexical"/> over the caller's
/// tables (decision D11): TAB, VT, FF, ZWNBSP and General_Category=Space_Separator, which is ECMAScript's WhiteSpace and
/// not a platform's, so U+0085 and U+180E are not white space and are not escaped. Line terminators are
/// <see cref="EcmaScriptLexical.IsLineTerminator"/>'s: LF, CR, U+2028 and U+2029. An instance holds the provider's
/// lexical classes and nothing else; it is immutable and may be shared by threads (convention 10).
/// </para>
/// <para>
/// <b>Work.</b> Each operation reads its input once to measure the result, then writes the result, and charges the
/// caller's meter one unit per code unit read before it reads them and one unit per code unit written before it writes
/// them, in whole blocks of 512 units on one running account, so that the meter is never paid a block more than the work
/// (decision D8's blocks; convention 4). A text that needs no change is answered as itself and costs its reading only. A
/// result longer than the caller's maximum length is refused before it is written (convention 5).
/// </para>
/// <para>
/// <b>Provisional</b>, with every member of <c>Broiler.Regex.Text</c>: task 2.9 fixes the public surface.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.5.1 RegExp.escape; IP=None; Security=High; Resources=2; Fingerprint=A07F5C
// Broiler-Falsified-If: a code point EncodeForRegExpEscape escapes is copied as itself, or U+0085 is escaped, so text placed in a pattern after escaping matches other text than itself
// Broiler-Human:        PENDING
public sealed class RegexEscaper
{
    /// <summary>The units the meter is charged in: decision D8's block, the new backend's (<see cref="RegexWorkCharge"/>).</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Medium; Resources=0; Fingerprint=AA6BF8
    // Broiler-Human:        PENDING
    private const ulong Block = 512;

    /// <summary>The longest string the runtime holds; a result longer than it is refused as one longer than the caller's maximum.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=779C8B
    // Broiler-Human:        PENDING
    private const int LongestString = 0x3FFFFFDF;

    /// <summary>ECMA-262's SyntaxCharacter (§22.2.1) and U+002F SOLIDUS, the code points EncodeForRegExpEscape writes after a backslash.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.5.1.1 EncodeForRegExpEscape; IP=None; Security=High; Resources=0; Fingerprint=7A7608
    // Broiler-Falsified-If: a SyntaxCharacter or the solidus is missing here, or another code point is here
    // Broiler-Human:        PENDING
    private const string SyntaxCharactersAndSolidus = "^$\\.*+?()[]{}|/";

    /// <summary>
    /// EncodeForRegExpEscape's otherPunctuators, <c>,-=&lt;&gt;#&amp;!%:;@~'`</c> and U+0022 QUOTATION MARK, which it writes
    /// in hexadecimal: an identity escape of most of them is a syntax error under <c>u</c>.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.5.1.1 EncodeForRegExpEscape; IP=None; Security=High; Resources=0; Fingerprint=8B6424
    // Broiler-Falsified-If: one of the sixteen punctuators the specification lists is missing here, or another code point is here
    // Broiler-Human:        PENDING
    private const string OtherPunctuators = ",-=<>#&!%:;@~'`\"";

    /// <summary>The ControlEscape letters of U+0009 to U+000D, in that order (ECMA-262's table "ControlEscape Code Point Values").</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7; IP=None; Security=Medium; Resources=0; Fingerprint=8787C8
    // Broiler-Human:        PENDING
    private const string ControlEscapeLetters = "tnvfr";

    /// <summary>The digits of a hexadecimal escape, lower case, as Number::toString and UnicodeEscape write them.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=B94BC5
    // Broiler-Human:        PENDING
    private const string HexDigits = "0123456789abcdef";

    private readonly EcmaScriptLexical _lexical;

    /// <summary>The escaper over <paramref name="tables"/>, whose white space it reads.</summary>
    /// <param name="tables">The Unicode owner's tables, of the version <c>Broiler.Unicode.Algorithms</c> reads.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tables"/> is null.</exception>
    /// <exception cref="ArgumentException">The tables are of another Unicode version or lack a table the algorithms read.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12.2; IP=None; Security=Medium; Resources=1; Fingerprint=7AF691
    // Broiler-Human:        PENDING
    public RegexEscaper(IUnicodeTables tables)
    {
        ArgumentNullException.ThrowIfNull(tables);
        _lexical = new EcmaScriptLexical(new UnicodeProperties(tables));
    }

    /// <summary>
    /// <c>RegExp.escape</c>'s text (ECMA-262 §22.2.5.1): <paramref name="text"/> rewritten, code point by code point, as
    /// pattern text that matches it literally wherever it is placed.
    /// </summary>
    /// <param name="text">The text, read as code points (StringToCodePoints): a surrogate pair is one code point, and a lone surrogate is one of its own.</param>
    /// <param name="maxLength">The longest result the caller accepts, in UTF-16 code units.</param>
    /// <param name="meter">The caller's meter, or null; charged as the type's remarks say.</param>
    /// <param name="escaped">The pattern text when the status is <see cref="RegexStatus.Success"/>; null otherwise.</param>
    /// <returns>
    /// <see cref="RegexStatus.Success"/>, or <see cref="RegexStatus.LimitExceeded"/> when the result would be longer than
    /// <paramref name="maxLength"/> (or than the longest string the runtime holds), its only limit, with no result.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A first code point that is an ASCII digit or letter is written <c>\xHH</c>, so that the text placed after
    /// <c>\0</c>, <c>\1</c> or <c>\c</c> does not extend that escape. Every code point, the first included otherwise, is
    /// EncodeForRegExpEscape's (§22.2.5.1.1): a SyntaxCharacter or <c>/</c> after a backslash; U+0009 to U+000D as their
    /// ControlEscape, <c>\t \n \v \f \r</c>; the punctuators <c>,-=&lt;&gt;#&amp;!%:;@~'`"</c>, WhiteSpace, LineTerminator
    /// and a lone surrogate as <c>\xHH</c> up to U+00FF and <c>\uHHHH</c> for each UTF-16 code unit past it, in lower
    /// case; every other code point as itself.
    /// </para>
    /// <para>
    /// The argument's type check (a TypeError for anything but a String) is the engine's, which converts nothing here
    /// (convention 2).
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is negative.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.5.1 RegExp.escape; IP=None; Security=High; Resources=2; Fingerprint=95B5D0
    // Broiler-Falsified-If: a leading ASCII letter or digit, a SyntaxCharacter, a ControlEscape character, a listed punctuator, a WhiteSpace or LineTerminator code point or a lone surrogate is answered as itself, U+0085 is escaped, or a result longer than maxLength answers Success
    // Broiler-Human:        PENDING
    public RegexStatus EscapeString(string text, int maxLength, RegexWorkCharge? meter, out string? escaped)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        var account = new MeterAccount(meter);
        account.Spend(text.Length);
        var length = EncodeString(text, []);
        if (length > Math.Min(maxLength, LongestString))
        {
            escaped = null;
            return RegexStatus.LimitExceeded;
        }

        // Every escape is longer than the code point it replaces, so a result as long as the text is the text.
        if (length == text.Length)
        {
            escaped = text;
            return RegexStatus.Success;
        }

        account.Spend(length);
        escaped = string.Create((int)length, (Escaper: this, Text: text), static (destination, state) => state.Escaper.EncodeString(state.Text, destination));
        return RegexStatus.Success;
    }


    /// <summary>
    /// RegExp.escape's loop over <paramref name="text"/>'s code points: writes the result into <paramref name="destination"/>
    /// when it is not empty, and answers the result's length either way, so that one walk measures and writes alike.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.5.1 RegExp.escape; IP=None; Security=High; Resources=1; Fingerprint=DF8A4F
    // Broiler-Falsified-If: the length answered for a text differs from the number of code units the same walk writes for it
    // Broiler-Human:        PENDING
    private long EncodeString(string text, Span<char> destination)
    {
        var writer = new TextWriter(destination);
        for (var at = 0; at < text.Length;)
        {
            var codePoint = CodePointAt(text, at, out var units);
            if (at == 0 && IsAsciiAlphanumeric(codePoint))
                writer.Hex(codePoint);
            else
                EncodeCodePoint(ref writer, codePoint);
            at += units;
        }

        return writer.Length;
    }

    /// <summary>EncodeForRegExpEscape (ECMA-262 §22.2.5.1.1): the pattern text matching <paramref name="codePoint"/>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.5.1.1 EncodeForRegExpEscape; IP=None; Security=High; Resources=1; Fingerprint=55161C
    // Broiler-Falsified-If: a code point of the specification's five classes is written as itself, or a code point outside them is escaped
    // Broiler-Human:        PENDING
    private void EncodeCodePoint(ref TextWriter writer, int codePoint)
    {
        if (codePoint < 0x80 && SyntaxCharactersAndSolidus.Contains((char)codePoint, StringComparison.Ordinal))
        {
            writer.Put('\\');
            writer.Put((char)codePoint);
        }
        else if (codePoint is >= 0x09 and <= 0x0D)
        {
            writer.Put('\\');
            writer.Put(ControlEscapeLetters[codePoint - 0x09]);
        }
        else if ((codePoint < 0x80 && OtherPunctuators.Contains((char)codePoint, StringComparison.Ordinal))
            || _lexical.IsWhiteSpace(codePoint) || EcmaScriptLexical.IsLineTerminator(codePoint) || codePoint is >= 0xD800 and <= 0xDFFF)
        {
            writer.Hex(codePoint);
        }
        else
        {
            writer.CodePoint(codePoint);
        }
    }


    /// <summary>The code point at <paramref name="at"/>, a surrogate pair read as one and a lone surrogate as itself, and how many code units it spans.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s11.1.4 CodePointAt; IP=None; Security=Medium; Resources=0; Fingerprint=014522
    // Broiler-Human:        PENDING
    private static int CodePointAt(string text, int at, out int units)
    {
        var first = text[at];
        if (char.IsHighSurrogate(first) && at + 1 < text.Length && char.IsLowSurrogate(text[at + 1]))
        {
            units = 2;
            return 0x10000 + ((first - 0xD800) << 10) + (text[at + 1] - 0xDC00);
        }

        units = 1;
        return first;
    }

    /// <summary>Whether <paramref name="codePoint"/> is matched by DecimalDigit or AsciiLetter (ECMA-262 §12.9.3, §12.7).</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.5.1 RegExp.escape; IP=None; Security=Medium; Resources=0; Fingerprint=086575
    // Broiler-Human:        PENDING
    private static bool IsAsciiAlphanumeric(int codePoint) =>
        codePoint is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    /// <summary>
    /// The writer both walks share: it writes into its destination when the destination is not empty, and counts the code
    /// units it would write either way, so that a walk that measures and the same walk that writes cannot disagree.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=478F03
    // Broiler-Human:        PENDING
    private ref struct TextWriter
    {
        private readonly Span<char> _destination;

        /// <summary>A writer into <paramref name="destination"/>, or a counter when it is empty.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=DA35BF
        // Broiler-Human:        PENDING
        internal TextWriter(Span<char> destination)
        {
            _destination = destination;
            Length = 0;
        }

        /// <summary>The code units written, or counted, so far.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=24BD3D
        // Broiler-Human:        PENDING
        internal long Length { get; private set; }

        /// <summary>One code unit.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=6E9326
        // Broiler-Human:        PENDING
        internal void Put(char unit)
        {
            if (!_destination.IsEmpty)
                _destination[(int)Length] = unit;
            Length++;
        }

        /// <summary>A code point's UTF-16 code units (UTF16EncodeCodePoint).</summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s11.1.1 UTF16EncodeCodePoint; IP=None; Security=Medium; Resources=0; Fingerprint=986E93
        // Broiler-Human:        PENDING
        internal void CodePoint(int codePoint)
        {
            if (codePoint <= 0xFFFF)
            {
                Put((char)codePoint);
                return;
            }

            Put((char)(0xD800 + ((codePoint - 0x10000) >> 10)));
            Put((char)(0xDC00 + ((codePoint - 0x10000) & 0x3FF)));
        }

        /// <summary>
        /// EncodeForRegExpEscape's hexadecimal escape: <c>\xHH</c> for a code point up to U+00FF, and UnicodeEscape,
        /// <c>\uHHHH</c>, for each UTF-16 code unit of one past it, in lower case.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.5.1.1 EncodeForRegExpEscape; IP=None; Security=High; Resources=0; Fingerprint=83F162
        // Broiler-Falsified-If: a code point up to U+00FF is written other than as \x and two lower-case digits, or one past it other than as \u and four lower-case digits for each of its code units
        // Broiler-Human:        PENDING
        internal void Hex(int codePoint)
        {
            if (codePoint <= 0xFF)
            {
                Put('\\');
                Put('x');
                Digits(codePoint, 2);
                return;
            }

            if (codePoint <= 0xFFFF)
            {
                UnicodeEscape(codePoint);
                return;
            }

            UnicodeEscape(0xD800 + ((codePoint - 0x10000) >> 10));
            UnicodeEscape(0xDC00 + ((codePoint - 0x10000) & 0x3FF));
        }


        /// <summary>UnicodeEscape (ECMA-262 §22.1.3.17.3): <c>\u</c> and four lower-case hexadecimal digits.</summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.1.3.17.3 UnicodeEscape; IP=None; Security=Medium; Resources=0; Fingerprint=BA1AE1
        // Broiler-Human:        PENDING
        private void UnicodeEscape(int unit)
        {
            Put('\\');
            Put('u');
            Digits(unit, 4);
        }

        /// <summary><paramref name="count"/> lower-case hexadecimal digits of <paramref name="value"/>, the most significant first.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=1B4598
        // Broiler-Human:        PENDING
        private void Digits(int value, int count)
        {
            for (var shift = (count - 1) * 4; shift >= 0; shift -= 4)
                Put(HexDigits[(value >> shift) & 0xF]);
        }
    }

    /// <summary>
    /// One operation's account with the caller's meter: the units it has spent and those it has paid, paid in whole
    /// blocks before the work they pay for, so that the paid units are never a block more than the spent ones.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=High; Resources=0; Fingerprint=F3A2C8
    // Broiler-Falsified-If: the meter is called with a number of units that is not a whole number of blocks, after the work it pays for, or with more than a block over the units spent
    // Broiler-Human:        PENDING
    private struct MeterAccount(RegexWorkCharge? meter)
    {
        private ulong _spent;
        private ulong _paid;

        /// <summary>Spends <paramref name="units"/>, paying the meter the whole blocks they take past what it has been paid; its exception, if it throws, propagates.</summary>
        // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=High; Resources=0; Fingerprint=E6C410
        // Broiler-Falsified-If: units already paid for are charged again, or units past what was paid are not charged before this returns
        // Broiler-Human:        PENDING
        internal void Spend(long units)
        {
            _spent += (ulong)units;
            if (meter is null || _spent <= _paid)
                return;

            var due = (_spent - _paid + Block - 1) / Block * Block;
            meter(due);
            _paid += due;
        }
    }
}
