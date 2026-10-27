using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Broiler.Regex.Text;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Requirement R9's <c>source</c> escaping and bundle <c>sjl-11-001</c>'s check A-9 ("source escaping follows
/// EscapeRegExpPattern"): <see cref="RegexEscaper.EscapePattern"/> against ECMA-262 §22.2.6.13.1. The specification
/// fixes what the text must do, not one text: <c>"/" + S + "/" + F</c> must lex as a RegularExpressionLiteral (§12.9.5)
/// whose body is <c>S</c>, <c>S</c> must behave as the pattern does under the flags, the empty pattern may be
/// <c>(?:)</c>, and one pattern and flags give one text. So the tests hold the library to those properties over every
/// code unit in four shapes and over every pattern of this test project's string literals, and to the text it chose
/// where the specification leaves one: test262's <c>staging/sm/RegExp/escape.js</c>, ported as data
/// (<see cref="Test262SourceCases"/>), which asks for a <c>/</c> in a class to be kept and a line terminator after a
/// backslash to become its escape; and Broiler.VM's correspondence rows T1 to T4.
/// </summary>
public class PatternSourceTests
{
    private static string Source(string pattern)
    {
        Assert.Equal(RegexStatus.Success, RegexEscaper.EscapePattern(pattern, int.MaxValue, null, out var source));
        return source!;
    }

    private static bool IsLineTerminator(char unit) => unit is '\n' or '\r' or '\u2028' or '\u2029';

    /// <summary>The escape the library writes for a line terminator.</summary>
    private static string LineTerminatorEscape(char unit) => unit switch
    {
        '\n' => "\\n",
        '\r' => "\\r",
        _ => "\\u" + ((int)unit).ToString("x4", CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// Whether <c>"/" + body + "/"</c> lexes as one RegularExpressionLiteral whose body is <paramref name="body"/>
    /// (ECMA-262 §12.9.5): no line terminator, a backslash sequence never at the end, a class closed by its first
    /// unescaped <c>]</c>, no unescaped <c>/</c> outside a class, and not a first <c>*</c>, which would open a comment.
    /// </summary>
    private static bool LexesAsLiteralBody(string body)
    {
        if (body.Length == 0 || body[0] == '*')
            return false;
        var inClass = false;
        for (var at = 0; at < body.Length; at++)
        {
            var unit = body[at];
            if (IsLineTerminator(unit))
                return false;
            if (unit == '\\')
            {
                if (at + 1 == body.Length || IsLineTerminator(body[at + 1]))
                    return false;
                at++;
            }
            else if (inClass)
            {
                inClass = unit != ']';
            }
            else if (unit == '[')
            {
                inClass = true;
            }
            else if (unit == '/')
            {
                return false;
            }
        }

        return !inClass;
    }

    // ---- Every code unit, in four shapes. ----

    [Fact]
    public void Every_code_unit_alone_is_itself_but_the_solidus_and_the_line_terminators()
    {
        var changed = 0;
        for (var code = 0; code <= 0xFFFF; code++)
        {
            var unit = (char)code;
            var pattern = unit.ToString();
            var expected = unit == '/' ? "\\/" : IsLineTerminator(unit) ? LineTerminatorEscape(unit) : pattern;
            Assert.True(expected == Source(pattern), "U+" + code.ToString("X4", CultureInfo.InvariantCulture));
            changed += expected == pattern ? 0 : 1;
        }

        // The solidus and the four line terminators; U+0085 and the surrogates among the code units kept.
        Assert.Equal(5, changed);
        Assert.Equal("\u0085", Source("\u0085"));
    }

    [Fact]
    public void Every_code_unit_after_a_backslash_is_kept_with_it_but_a_line_terminator_which_becomes_its_escape()
    {
        for (var code = 0; code <= 0xFFFF; code++)
        {
            var unit = (char)code;
            var pattern = "\\" + unit;
            var expected = IsLineTerminator(unit) ? LineTerminatorEscape(unit) : pattern;
            Assert.True(expected == Source(pattern), "\\ U+" + code.ToString("X4", CultureInfo.InvariantCulture));
            Assert.True(expected == Source("a" + pattern).Substring(1), "a\\ U+" + code.ToString("X4", CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public void Every_code_unit_in_a_class_is_kept_the_solidus_among_them_but_the_line_terminators()
    {
        for (var code = 0; code <= 0xFFFF; code++)
        {
            var unit = (char)code;
            var pattern = "[" + unit + "]";
            var expected = IsLineTerminator(unit) ? "[" + LineTerminatorEscape(unit) + "]" : pattern;
            Assert.True(expected == Source(pattern), "[U+" + code.ToString("X4", CultureInfo.InvariantCulture) + "]");
        }
    }

    [Fact]
    public void Every_code_unit_after_an_escaped_bracket_is_outside_a_class()
    {
        for (var code = 0; code <= 0xFFFF; code++)
        {
            var unit = (char)code;
            var pattern = "\\[" + unit;
            var expected = "\\[" + (unit == '/' ? "\\/" : IsLineTerminator(unit) ? LineTerminatorEscape(unit) : unit.ToString());
            Assert.True(expected == Source(pattern), "\\[U+" + code.ToString("X4", CultureInfo.InvariantCulture));
        }
    }

    // ---- The specification's properties over patterns that compile. ----

    private static readonly string[] Subjects = ["", "/", "a/b", "\n", "\r\n", "\u2028", "\u2029", "\\", "[/]", "a", "aa1", "\uD83D\uDE00"];

    /// <summary>Whether <paramref name="pattern"/> compiles under <paramref name="flags"/>, through the public constructor.</summary>
    private static bool Compiles(string pattern, string flags)
    {
        try
        {
            TestRegex.Compile(pattern, flags);
            return true;
        }
        catch (RegexSyntaxException)
        {
            return false;
        }
    }

    /// <summary>
    /// The specification's three properties for <paramref name="pattern"/> under <paramref name="flags"/>, read so that
    /// only what can fail is compiled: a text that is not a literal's body must come from a pattern that does not compile;
    /// a text that differs from a pattern that compiles must compile and answer as the pattern does on every subject from
    /// every start; and calling again gives the same text. A text equal to its pattern behaves as it, trivially.
    /// </summary>
    /// <returns>Whether the text differs from a pattern that compiles, so that the comparison ran.</returns>
    private static bool HoldsTheSpecificationsProperties(string pattern, string flags)
    {
        var source = Source(pattern);
        Assert.Equal(source, Source(pattern));
        if (!LexesAsLiteralBody(source))
        {
            Assert.False(Compiles(pattern, flags), Show(pattern) + " /" + flags + " compiles and gives " + Show(source) + ", which is not a literal's body");
            return false;
        }

        if (source == pattern || !Compiles(pattern, flags))
            return false;

        var original = TestRegex.Compile(pattern, flags);
        var escaped = TestRegex.Compile(source, flags);
        foreach (var subject in pattern.Length <= 64 ? Subjects.Append(pattern) : Subjects)
        {
            for (var start = 0; start <= subject.Length; start++)
                Assert.True(TestRegex.Answer(original, subject, start) == TestRegex.Answer(escaped, subject, start), Show(pattern) + " /" + flags + " on " + Show(subject));
        }

        return true;
    }

    [Fact]
    public void Every_code_unit_in_five_shapes_holds_the_specification_s_properties_in_every_mode()
    {
        var compared = 0;
        foreach (var flags in new[] { "", "u", "v" })
        {
            for (var code = 0; code <= 0xFFFF; code++)
            {
                var unit = (char)code;
                foreach (var pattern in new[] { unit.ToString(), "\\" + unit, "[" + unit + "]", "\\[" + unit, "a" + unit + "b" })
                    compared += HoldsTheSpecificationsProperties(pattern, flags) ? 1 : 0;
            }
        }

        // Not vacuous: the solidus and the line terminators, in the shapes each mode compiles, are compared as compiled.
        Assert.InRange(compared, 40, 100);
    }

    [Fact]
    public void Every_pattern_of_the_test_project_s_string_literals_holds_the_specification_s_properties()
    {
        var patterns = TestPopulations.TestSources()
            .SelectMany(static file => TestPopulations.Literals(System.IO.File.ReadAllText(file)).Select(static l => l.Text))
            .Where(static pattern => pattern.Length <= 2000)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var compared = 0;
        foreach (var flags in new[] { "", "u", "v" })
        {
            foreach (var pattern in patterns)
                compared += HoldsTheSpecificationsProperties(pattern, flags) ? 1 : 0;
        }

        // Not vacuous: thousands of literals, and those of them holding a solidus or a line terminator that compile are
        // compared as compiled.
        Assert.InRange(patterns.Length, 2000, int.MaxValue);
        Assert.InRange(compared, 10, int.MaxValue);
    }

    [Theory]
    [InlineData("[[a]\\/]", "v", "[[a]\\/]")]
    [InlineData("[[a]/]b/", "", "[[a]\\/]b\\/")]
    [InlineData("[\\]/]/", "", "[\\]/]\\/")]
    [InlineData("[^/]/", "u", "[^/]\\/")]
    [InlineData("[a&&[^/]]", "", "[a&&[^/]]")]
    [InlineData("(?<a>/)\\k<a>/", "u", "(?<a>\\/)\\k<a>\\/")]
    [InlineData("(?<=/)x/", "", "(?<=\\/)x\\/")]
    [InlineData("a\\/b/c", "v", "a\\/b\\/c")]
    [InlineData("\\\\/", "", "\\\\\\/")]
    [InlineData("[\\\\]/", "", "[\\\\]\\/")]
    [InlineData("\\u{2F}/", "u", "\\u{2F}\\/")]
    public void Classes_escapes_and_groups_are_read_as_a_literal_reads_them(string pattern, string flags, string source)
    {
        // A class closes at its first unescaped bracket, as a literal's does; an escaped bracket or backslash opens and
        // closes nothing; a solidus in a class is kept and one outside is escaped.
        Assert.True(Compiles(pattern, flags), pattern);
        Assert.Equal(source, Source(pattern));
        Assert.Equal(source != pattern, HoldsTheSpecificationsProperties(pattern, flags));
    }

    // ---- The text the library chose, where the specification leaves one. ----

    [Fact]
    public void Test262_s_staging_escape_js_cases_answer_as_test262_expects()
    {
        var cases = Test262SourceCases.Cases();
        Assert.Equal(49, cases.Length);
        foreach (var (file, pattern, expected) in cases)
            Assert.True(expected == Source(pattern), file + ": " + Show(pattern) + " gives " + Show(Source(pattern)) + ", test262 expects " + Show(expected));
    }

    [Fact]
    public void Broiler_VM_s_correspondence_rows_T1_to_T4_answer_as_the_specification_and_the_staging_test_ask()
    {
        (string Pattern, string Source)[] cases =
        [
            // T1: the pattern as written, literal and constructor alike (a braced escape is not rewritten).
            ("\\u{61}", "\\u{61}"), ("\\u{1F600}", "\\u{1F600}"), ("\\u{D83D}", "\\u{D83D}"), ("[\\u{61}-\\u{7A}]", "[\\u{61}-\\u{7A}]"),
            ("\\u{3}", "\\u{3}"), ("\\\\u{61}", "\\\\u{61}"),
            // T2, implementation-defined: a solidus in a class is kept, Broiler.JS's text and the staging test's.
            ("[/]", "[/]"),
            // T3: a line terminator after a backslash becomes its escape (Broiler.VM copied it raw).
            ("\\\n", "\\n"), ("\\\u2028", "\\u2028"),
            // T4: the solidus, an escaped one, line terminators, the empty pattern, a lone surrogate and U+0085.
            ("/", "\\/"), ("\\/", "\\/"), ("a/b", "a\\/b"), ("\n", "\\n"), ("\r", "\\r"), ("\u2028", "\\u2028"), ("\u2029", "\\u2029"),
            ("", "(?:)"), ("\uD800", "\uD800"), ("\u0085", "\u0085"),
        ];
        foreach (var (pattern, expected) in cases)
            Assert.True(expected == Source(pattern), Show(pattern) + " gives " + Show(Source(pattern)));
    }

    // ---- The meter, the maximum length and the arguments. ----

    private static (RegexStatus Status, string? Source, List<ulong> Charges) Charged(string pattern, int maxLength)
    {
        var charges = new List<ulong>();
        var status = RegexEscaper.EscapePattern(pattern, maxLength, charges.Add, out var source);
        return (status, source, charges);
    }

    [Fact]
    public void The_meter_is_paid_in_whole_blocks_before_each_pass_and_a_pattern_that_needs_no_change_costs_its_reading()
    {
        // 1,000 solidi: 1,000 read in two blocks, then 2,000 written, the 1,976 unpaid in four blocks more.
        var (status, source, charges) = Charged(new string('/', 1000), int.MaxValue);
        Assert.Equal(RegexStatus.Success, status);
        Assert.Equal(2000, source!.Length);
        Assert.Equal(new ulong[] { 1024, 2048 }, charges);

        var pattern = "[/]" + new string('a', 600);
        var kept = Charged(pattern, int.MaxValue);
        Assert.Same(pattern, kept.Source);
        Assert.Equal(new ulong[] { 1024 }, kept.Charges);

        // The empty pattern reads nothing and writes (?:), one block.
        Assert.Equal((RegexStatus.Success, "(?:)"), (Charged("", 4).Status, Charged("", 4).Source));
        Assert.Equal(new ulong[] { 512 }, Charged("", 4).Charges);
    }

    [Fact]
    public void A_text_longer_than_the_caller_s_maximum_is_refused_before_it_is_written()
    {
        var (status, source, charges) = Charged("\u2028\u2028", 11);
        Assert.Equal(RegexStatus.LimitExceeded, status);
        Assert.Null(source);
        Assert.Equal(new ulong[] { 512 }, charges);
        Assert.Equal("\\u2028\\u2028", Charged("\u2028\u2028", 12).Source);
        Assert.Equal(RegexStatus.LimitExceeded, Charged("", 3).Status);

        // A pattern that needs no change is within any maximum its own length is.
        Assert.Equal(RegexStatus.Success, Charged("abc", 3).Status);
    }

    [Fact]
    public void A_meter_that_throws_stops_the_operation_and_a_broken_precondition_is_an_argument_exception()
    {
        var refused = new InvalidOperationException("budget");
        Assert.Same(refused, Assert.Throws<InvalidOperationException>(() => RegexEscaper.EscapePattern("a/b", int.MaxValue, _ => throw refused, out _)));
        Assert.Throws<ArgumentNullException>(() => RegexEscaper.EscapePattern(null!, 10, null, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => RegexEscaper.EscapePattern("a", -1, null, out _));
    }

    private static string Show(string text) =>
        "\"" + string.Concat(text.Select(static c => c is >= ' ' and <= '~' ? c.ToString() : "\\u" + ((int)c).ToString("X4", CultureInfo.InvariantCulture))) + "\"";
}
