using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Broiler.Regex.Text;
using Broiler.Unicode.Algorithms;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Requirement R9's <c>RegExp.escape</c> and bundle <c>sjl-11-001</c>'s check A-9 ("RegExp.escape's text escapes exactly
/// the specification's EncodeForRegExpEscape set over EcmaScriptLexical's WhiteSpace and LineTerminator"):
/// <see cref="RegexEscaper.EscapeString"/> against ECMA-262 §22.2.5.1 and §22.2.5.1.1 read as an oracle, over every code
/// point of the code space, first and after the first, so that every class the specification names is enumerated, not
/// sampled: SyntaxCharacter and <c>/</c>, the five ControlEscape code points, the sixteen other punctuators, WhiteSpace,
/// LineTerminator, the surrogates, and the ASCII digits and letters of the leading rule; and, explicitly, U+0085, which
/// is not WhiteSpace in ECMAScript and is not escaped. test262's <c>built-ins/RegExp/escape</c> cases are ported as data
/// (<see cref="Test262EscapeCases"/>), and Broiler.VM's correspondence row T5's inputs are run with both engines' answers.
/// </summary>
public class EscapeStringTests
{
    private static readonly RegexEscaper Escaper = new(UnicodeTables.Instance);

    // ---- The oracle: ECMA-262 §22.2.5.1 and §22.2.5.1.1, with the sets the specification names written out. ----

    private const string SyntaxCharacters = "^$\\.*+?()[]{}|";

    private const string OtherPunctuators = ",-=<>#&!%:;@~'`\"";

    /// <summary>General_Category=Space_Separator at UCD 17.0.0 (UnicodeData.txt, field 2 = Zs), checked against the provider below.</summary>
    private static readonly int[] SpaceSeparators =
    [
        0x0020, 0x00A0, 0x1680, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007, 0x2008, 0x2009, 0x200A,
        0x202F, 0x205F, 0x3000,
    ];

    /// <summary>WhiteSpace (§12.2): TAB, VT, FF, ZWNBSP and the Space_Separators.</summary>
    private static bool IsWhiteSpace(int codePoint) =>
        codePoint is 0x0009 or 0x000B or 0x000C or 0xFEFF || Array.IndexOf(SpaceSeparators, codePoint) >= 0;

    /// <summary>LineTerminator (§12.3): LF, CR, LS and PS.</summary>
    private static bool IsLineTerminator(int codePoint) => codePoint is 0x000A or 0x000D or 0x2028 or 0x2029;

    private static string Utf16(int codePoint) =>
        codePoint is >= 0xD800 and <= 0xDFFF ? ((char)codePoint).ToString() : char.ConvertFromUtf32(codePoint);

    private static string Hex(int value, int digits) => value.ToString(digits == 2 ? "x2" : "x4", CultureInfo.InvariantCulture);

    /// <summary>EncodeForRegExpEscape(cp), its steps in order.</summary>
    private static string EncodeForRegExpEscape(int codePoint)
    {
        if (codePoint < 0x80 && (SyntaxCharacters.Contains((char)codePoint) || codePoint == '/'))
            return "\\" + (char)codePoint;
        switch (codePoint)
        {
            case 0x09: return "\\t";
            case 0x0A: return "\\n";
            case 0x0B: return "\\v";
            case 0x0C: return "\\f";
            case 0x0D: return "\\r";
        }

        if ((codePoint < 0x80 && OtherPunctuators.Contains((char)codePoint)) || IsWhiteSpace(codePoint) || IsLineTerminator(codePoint)
            || codePoint is >= 0xD800 and <= 0xDFFF)
        {
            if (codePoint <= 0xFF)
                return "\\x" + Hex(codePoint, 2);
            return string.Concat(Utf16(codePoint).Select(static unit => "\\u" + Hex(unit, 4)));
        }

        return Utf16(codePoint);
    }

    /// <summary>RegExp.escape(S)'s loop: StringToCodePoints, the leading rule while the result is empty, else EncodeForRegExpEscape.</summary>
    private static string Specification(string text)
    {
        var escaped = new StringBuilder();
        for (var at = 0; at < text.Length;)
        {
            var codePoint = char.IsHighSurrogate(text[at]) && at + 1 < text.Length && char.IsLowSurrogate(text[at + 1])
                ? char.ConvertToUtf32(text[at], text[at + 1])
                : text[at];
            at += codePoint > 0xFFFF ? 2 : 1;
            if (escaped.Length == 0 && codePoint is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z')
                escaped.Append("\\x").Append(Hex(codePoint, 2));
            else
                escaped.Append(EncodeForRegExpEscape(codePoint));
        }

        return escaped.ToString();
    }

    private static string Escape(string text)
    {
        Assert.Equal(RegexStatus.Success, Escaper.EscapeString(text, int.MaxValue, null, out var escaped));
        return escaped!;
    }

    // ---- The whole code space. ----

    [Fact]
    public void Every_code_point_after_the_first_is_encoded_as_EncodeForRegExpEscape_encodes_it()
    {
        var escapedCount = 0;
        for (var codePoint = 0; codePoint <= 0x10FFFF; codePoint++)
        {
            var text = "_" + Utf16(codePoint);
            var expected = "_" + EncodeForRegExpEscape(codePoint);
            var actual = Escape(text);
            if (actual != expected)
                Assert.Fail("U+" + codePoint.ToString("X4", CultureInfo.InvariantCulture) + ": " + Show(actual) + " where the specification gives " + Show(expected));
            if (actual != text)
                escapedCount++;
        }

        // Not vacuous: 15 SyntaxCharacters and /, 5 ControlEscapes, 16 punctuators, 18 WhiteSpace and 2 LineTerminators
        // beyond the ControlEscapes, and 2,048 surrogates are escaped; nothing else is.
        Assert.Equal(15 + 5 + 16 + 18 + 2 + 2048, escapedCount);
    }

    [Fact]
    public void Every_code_point_first_is_escaped_as_the_leading_rule_and_EncodeForRegExpEscape_say()
    {
        for (var codePoint = 0; codePoint <= 0x10FFFF; codePoint++)
        {
            var text = Utf16(codePoint);
            var expected = Specification(text);
            var actual = Escape(text);
            if (actual != expected)
                Assert.Fail("U+" + codePoint.ToString("X4", CultureInfo.InvariantCulture) + ": " + Show(actual) + " where the specification gives " + Show(expected));
        }
    }

    [Fact]
    public void The_test_s_white_space_is_the_provider_s_at_the_version_it_pins()
    {
        // The oracle's Space_Separator list is UCD 17.0.0's; the provider must read the same set over the whole code space,
        // so that a change of data shows here rather than as a silent difference of the oracle's.
        Assert.Equal("17.0.0", UnicodeAlgorithms.UnicodeVersion);
        var properties = new UnicodeProperties(UnicodeTables.Instance);
        Assert.True(properties.TryResolve("Space_Separator", out var zs));
        var lexical = new EcmaScriptLexical(properties);
        for (var codePoint = 0; codePoint <= 0x10FFFF; codePoint++)
        {
            Assert.Equal(Array.IndexOf(SpaceSeparators, codePoint) >= 0, zs.Contains(codePoint));
            Assert.Equal(IsWhiteSpace(codePoint), lexical.IsWhiteSpace(codePoint));
        }
    }

    // ---- Each class the specification names, written out. ----

    [Fact]
    public void Each_class_the_specification_names_is_escaped_as_it_says()
    {
        // A fact, not a theory: an attribute's string is UTF-8 in metadata and cannot carry a lone surrogate.
        (string Text, string Escaped)[] cases =
        [
            // SyntaxCharacter and the solidus: a backslash and the character.
            ("_^", "_\\^"),
            ("_$", "_\\$"),
            ("_\\", "_\\\\"),
            ("_.", "_\\."),
            ("_*", "_\\*"),
            ("_+", "_\\+"),
            ("_?", "_\\?"),
            ("_(", "_\\("),
            ("_)", "_\\)"),
            ("_[", "_\\["),
            ("_]", "_\\]"),
            ("_{", "_\\{"),
            ("_}", "_\\}"),
            ("_|", "_\\|"),
            ("_/", "_\\/"),
            // The ControlEscape table: TAB, LF, VT, FF and CR, before the WhiteSpace and LineTerminator rule they also fall under.
            ("_\t", "_\\t"),
            ("_\n", "_\\n"),
            ("_\v", "_\\v"),
            ("_\f", "_\\f"),
            ("_\r", "_\\r"),
            // The other punctuators, in hexadecimal.
            ("_,-=<>#&!%:;@~'`\"", "_\\x2c\\x2d\\x3d\\x3c\\x3e\\x23\\x26\\x21\\x25\\x3a\\x3b\\x40\\x7e\\x27\\x60\\x22"),
            // WhiteSpace: \x up to U+00FF, \u past it, lower case.
            ("_ \u00A0\uFEFF\u1680\u2000\u200A\u202F\u205F\u3000", "_\\x20\\xa0\\ufeff\\u1680\\u2000\\u200a\\u202f\\u205f\\u3000"),
            // LineTerminator beyond the ControlEscapes.
            ("_\u2028\u2029", "_\\u2028\\u2029"),
            // Lone surrogates, per code unit (a lead before a lead or an underscore, a trail after one or after a trail); a
            // pair is one code point and is itself.
            ("_\uD800\uDBFF_\uDC00\uDFFF", "_\\ud800\\udbff_\\udc00\\udfff"),
            ("_\uDC00\uD800", "_\\udc00\\ud800"),
            ("_\uD83D\uDE00", "_\uD83D\uDE00"),
            ("\uD83D\uDE00x", "\uD83D\uDE00x"),
            // Not WhiteSpace in ECMAScript, so not escaped: U+0085 NEXT LINE (Cc), U+180E MONGOLIAN VOWEL SEPARATOR and U+200B
            // ZERO WIDTH SPACE (Cf); and the underscore, letters past ASCII, the last BMP and the last code point.
            ("_\u0085", "_\u0085"),
            ("\u0085", "\u0085"),
            ("_\u180E\u200B", "_\u180E\u200B"),
            ("__\u00E9\u0100\uFFFF\uDBFF\uDFFF", "__\u00E9\u0100\uFFFF\uDBFF\uDFFF"),
            // The leading rule: a first ASCII digit or letter in hexadecimal, and only the first.
            ("0", "\\x30"),
            ("9a", "\\x39a"),
            ("Za9", "\\x5aa9"),
            ("z", "\\x7a"),
            ("_a", "_a"),
            (".a", "\\.a"),
            ("", ""),
        ];
        foreach (var (text, escaped) in cases)
        {
            Assert.Equal(Specification(text), escaped);
            Assert.True(escaped == Escape(text), Show(text) + " gives " + Show(Escape(text)) + " where " + Show(escaped) + " is the specification's");
        }
    }

    /// <summary>test262's <c>built-ins/RegExp/escape</c> files at the programme's pin, every case of each (ported as data).</summary>
    [Fact]
    public void Test262_s_RegExp_escape_cases_answer_as_test262_expects()
    {
        var cases = Test262EscapeCases.Cases();
        Assert.Equal(313, cases.Length);
        Assert.Equal(13, cases.Select(static c => c.File).Distinct().Count());
        foreach (var (file, input, expected) in cases)
            Assert.True(expected == Escape(input), file + ": " + Show(input) + " gives " + Show(Escape(input)) + ", test262 expects " + Show(expected));
    }

    /// <summary>
    /// Broiler.VM's correspondence row T5 (bundle <c>sjl-11-001</c>, probe <c>t2</c>): each input, with the answer both
    /// engines gave at its revisions, which the row records as the specification's ("same in both").
    /// </summary>
    [Fact]
    public void Broiler_VM_s_correspondence_row_T5_answers_as_both_engines_did()
    {
        (string Text, string Escaped)[] cases =
        [
            ("abc", "\\x61bc"),
            ("1a", "\\x31a"),
            ("a1", "\\x611"),
            ("_", "_"),
            ("a b", "\\x61\\x20b"),
            ("\u00E9", "\u00E9"),
            ("\u0085", "\u0085"),
            ("\u00A0", "\\xa0"),
            ("\uFEFF", "\\ufeff"),
            ("\u2028", "\\u2028"),
            ("\u2029", "\\u2029"),
            ("\u3000", "\\u3000"),
            ("\u180E", "\u180E"),
            ("\u1680", "\\u1680"),
            ("\u202F", "\\u202f"),
            ("\uD800", "\\ud800"),
            ("\uDFFF", "\\udfff"),
            ("\uD83D\uDE00", "\uD83D\uDE00"),
            ("x\uD83D", "\\x78\\ud83d"),
            (",-=<>#&!%:;@~'`\"", "\\x2c\\x2d\\x3d\\x3c\\x3e\\x23\\x26\\x21\\x25\\x3a\\x3b\\x40\\x7e\\x27\\x60\\x22"),
            ("^$\\.*+?()[]{}|/", "\\^\\$\\\\\\.\\*\\+\\?\\(\\)\\[\\]\\{\\}\\|\\/"),
            ("\t\n\u000B\f\r", "\\t\\n\\v\\f\\r"),
            (" ", "\\x20"),
            ("", ""),
            ("Z\u0000", "\\x5a\u0000"),
            ("\u0080", "\u0080"),
            ("\u00FF", "\u00FF"),
            ("\u0100", "\u0100"),
        ];
        Assert.Equal(28, cases.Length);
        foreach (var (text, escaped) in cases)
            Assert.True(escaped == Escape(text), Show(text) + " gives " + Show(Escape(text)) + " where both engines gave " + Show(escaped));
    }

    // ---- What the text is for: a pattern that matches the text, and nothing else, wherever it is placed. ----

    /// <summary>Every code point the specification escapes, and some it does not, each alone.</summary>
    private static IEnumerable<int> Escaped()
    {
        foreach (var c in SyntaxCharacters + "/" + OtherPunctuators)
            yield return c;
        for (var codePoint = 0x09; codePoint <= 0x0D; codePoint++)
            yield return codePoint;
        foreach (var codePoint in SpaceSeparators)
            yield return codePoint;
        foreach (var codePoint in new[] { 0xFEFF, 0x2028, 0x2029, 0x0085, 0x180E, 0x200B, '_', 0x00E9, 0x10000, 0x10FFFF })
            yield return codePoint;
        for (var codePoint = 0xD800; codePoint <= 0xDFFF; codePoint++)
            yield return codePoint;
        foreach (var c in "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz")
            yield return c;
    }

    [Fact]
    public void The_escaped_text_matches_the_text_and_not_another_in_every_mode()
    {
        foreach (var flags in new[] { "", "u", "v" })
        {
            foreach (var codePoint in Escaped())
            {
                var text = Utf16(codePoint);
                var regex = TestRegex.Compile("^(?:" + Escape(text) + ")$", flags);
                Assert.True(regex.Match(text, 0).Success, "U+" + codePoint.ToString("X4", CultureInfo.InvariantCulture) + " under /" + flags);
                var other = codePoint == '_' ? "-" : "_";
                Assert.False(regex.Match(other, 0).Success, "U+" + codePoint.ToString("X4", CultureInfo.InvariantCulture) + " under /" + flags);
            }
        }
    }

    [Theory]
    // After a back-reference, \0 and \c, the leading digit or letter's escape cannot extend the escape before it.
    [InlineData("(a)\\1", "1", "", "aa1")]
    [InlineData("(a)\\1", "1", "u", "aa1")]
    [InlineData("\\0", "0", "", "\u00000")]
    [InlineData("\\0", "7", "u", "\u00007")]
    [InlineData("\\c", "J", "", "\\cJ")]
    public void The_leading_rule_keeps_the_text_out_of_the_escape_before_it(string before, string text, string flags, string subject)
    {
        var match = TestRegex.Compile("^" + before + Escape(text) + "$", flags).Match(subject, 0);
        Assert.True(match.Success);
        Assert.Equal(subject.Length, match.Length);
    }

    // ---- The meter, the maximum length and the arguments. ----

    private static (RegexStatus Status, string? Escaped, List<ulong> Charges) Charged(string text, int maxLength)
    {
        var charges = new List<ulong>();
        var status = Escaper.EscapeString(text, maxLength, charges.Add, out var escaped);
        return (status, escaped, charges);
    }

    [Fact]
    public void The_meter_is_paid_in_whole_blocks_before_each_pass_and_never_a_block_more_than_the_work()
    {
        // Reading 1,000 units costs two blocks before the reading; writing the 2,000 of the result costs the 1,976 the
        // first two blocks leave unpaid, four blocks more, before the writing.
        var dots = new string('.', 1000);
        var (status, escaped, charges) = Charged(dots, int.MaxValue);
        Assert.Equal(RegexStatus.Success, status);
        Assert.Equal(2000, escaped!.Length);
        Assert.Equal(new ulong[] { 1024, 2048 }, charges);

        // A short text is one block for both passes: 3 read and 6 written.
        Assert.Equal(new ulong[] { 512 }, Charged("abc", int.MaxValue).Charges);

        // Over the whole code space and its pairs, the meter is paid in whole blocks, at least the work and less than a
        // block more.
        foreach (var text in new[] { "\uD800" + new string('a', 600), new string('\u3000', 700), "x" + new string('_', 1023), "\uD83D\uDE00" })
        {
            var result = Charged(text, int.MaxValue);
            var work = (ulong)text.Length + (result.Escaped == text ? 0UL : (ulong)result.Escaped!.Length);
            Assert.All(result.Charges, static units => Assert.Equal(0UL, units % 512));
            Assert.InRange(result.Charges.Aggregate(0UL, static (a, b) => a + b), work, work + 511);
        }
    }

    [Fact]
    public void A_text_that_needs_no_change_is_answered_as_itself_and_costs_its_reading_alone()
    {
        var text = "_already literal \u00E9";
        text = text.Replace(" ", "_", StringComparison.Ordinal);
        var (status, escaped, charges) = Charged(text, int.MaxValue);
        Assert.Equal(RegexStatus.Success, status);
        Assert.Same(text, escaped);
        Assert.Equal(new ulong[] { 512 }, charges);

        // The empty text is no work: the meter is not called.
        Assert.Equal((RegexStatus.Success, "", 0), (Charged("", 0).Status, Charged("", 0).Escaped, Charged("", 0).Charges.Count));
    }

    [Fact]
    public void A_result_longer_than_the_caller_s_maximum_is_refused_after_the_reading_and_before_the_writing()
    {
        // "\uD800" is six code units escaped.
        var (status, escaped, charges) = Charged("\uD800\uD800", 11);
        Assert.Equal(RegexStatus.LimitExceeded, status);
        Assert.Null(escaped);
        Assert.Equal(new ulong[] { 512 }, charges);

        Assert.Equal((RegexStatus.Success, "\\ud800\\ud800"), (Charged("\uD800\uD800", 12).Status, Charged("\uD800\uD800", 12).Escaped));

        // The writing pass is charged only once it is admitted: 2 read in one block, then nothing more for 12 written.
        Assert.Equal(new ulong[] { 512 }, Charged("\uD800\uD800", 12).Charges);
        Assert.Equal((RegexStatus.LimitExceeded, (string?)null), (Charged("a", 3).Status, Charged("a", 3).Escaped));
    }

    [Fact]
    public void A_meter_that_throws_stops_the_operation_with_its_own_exception()
    {
        var refused = new InvalidOperationException("budget");
        Assert.Same(refused, Assert.Throws<InvalidOperationException>(() => Escaper.EscapeString("abc", int.MaxValue, _ => throw refused, out _)));

        // Thrown at the second payment, before the writing: the reading's 1,000 units are paid, the writing's are not.
        var calls = 0;
        Assert.Same(refused, Assert.Throws<InvalidOperationException>(() => Escaper.EscapeString(new string('.', 1000), int.MaxValue, _ =>
        {
            if (++calls == 2)
                throw refused;
        }, out _)));
        Assert.Equal(2, calls);

        // The escaper holds nothing the exception could have left changed: it answers the next call as a new one would.
        Assert.Equal("\\x61bc", Escape("abc"));
    }

    [Fact]
    public void A_broken_precondition_is_an_argument_exception()
    {
        Assert.Throws<ArgumentNullException>(() => new RegexEscaper(null!));
        Assert.Throws<ArgumentNullException>(() => Escaper.EscapeString(null!, 10, null, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => Escaper.EscapeString("a", -1, null, out _));
    }

    [Fact]
    public async Task One_escaper_answers_threads_at_once_as_it_answers_one()
    {
        var texts = Enumerable.Range(0, 2000).Select(static i => Utf16(i * 547 % 0x110000) + "a" + (char)(i % 128)).ToArray();
        var alone = texts.Select(Escape).ToArray();
        var together = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => texts.Select(Escape).ToArray())));
        Assert.All(together, answers => Assert.Equal(alone, answers));
    }

    private static string Show(string text) =>
        "\"" + string.Concat(text.Select(static c => c is >= ' ' and <= '~' ? c.ToString() : "\\u" + ((int)c).ToString("X4", CultureInfo.InvariantCulture))) + "\"";
}
