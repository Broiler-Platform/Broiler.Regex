using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Broiler.Regex.Text;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Requirement R9's flag half and bundle <c>sjl-11-001</c>'s check A-9 ("flag parsing returns a status for a duplicate,
/// an unknown flag and u with v"): <see cref="RegexFlagsText"/> against ECMA-262's reading of a flags string, enumerated
/// rather than sampled. RegExpInitialize (§22.2.3.3) refuses a code unit other than <c>d g i m s u v y</c> and a code unit
/// written twice; ParsePattern (§22.2.3.4) then refuses <c>u</c> with <c>v</c>; the <c>flags</c> getter (§22.2.6.4) writes
/// the letters in the order <c>d g i m s u v y</c>. The oracle below reads those steps as the text states them, from the
/// whole string, and the population is every ordering of every set of the eight flags, every code unit alone, and every
/// string of up to three code units over the flags and a set of strangers. Broiler.VM's correspondence rows F1 and F2
/// (<c>""</c>, <c>g</c>, <c>gg</c>, <c>x</c>, <c>G</c>, <c>uv</c>, <c>vu</c>, every order of <c>dgimsuvy</c>, spaces and
/// NUL) are among them.
/// </summary>
public class FlagsTextTests
{
    private const string Letters = "dgimsuvy";

    private static readonly RegexFlags[] FlagOfLetter =
    [
        RegexFlags.HasIndices, RegexFlags.Global, RegexFlags.IgnoreCase, RegexFlags.Multiline,
        RegexFlags.DotAll, RegexFlags.Unicode, RegexFlags.UnicodeSets, RegexFlags.Sticky,
    ];

    /// <summary>
    /// The specification's answer for <paramref name="text"/>: the first code unit the first step refuses, reading every
    /// code unit (an unknown one, or one that occurred before it), else <c>u</c> with <c>v</c> at the later of the two,
    /// else the flags.
    /// </summary>
    private static (RegexDiagnosticCode Status, RegexFlags Flags, int Position) Specification(string text)
    {
        for (var at = 0; at < text.Length; at++)
        {
            if (Letters.IndexOf(text[at], StringComparison.Ordinal) < 0)
                return (RegexDiagnosticCode.UnknownFlag, RegexFlags.None, at);
            if (text.IndexOf(text[at], StringComparison.Ordinal) < at)
                return (RegexDiagnosticCode.DuplicateFlag, RegexFlags.None, at);
        }

        var u = text.IndexOf('u', StringComparison.Ordinal);
        var v = text.IndexOf('v', StringComparison.Ordinal);
        if (u >= 0 && v >= 0)
            return (RegexDiagnosticCode.UnicodeWithUnicodeSets, RegexFlags.None, Math.Max(u, v));

        var flags = RegexFlags.None;
        foreach (var letter in text)
            flags |= FlagOfLetter[Letters.IndexOf(letter, StringComparison.Ordinal)];
        return (RegexDiagnosticCode.None, flags, -1);
    }

    /// <summary>
    /// The reader's answer as the specification's is written: the refusal's code, <see cref="RegexDiagnosticCode.None"/>
    /// for none, the flags and the position. Since task 2.9 of SJL-11.2 the status is the syntax status and the refusal a
    /// diagnostic, so the status is held to the code here, every time.
    /// </summary>
    private static (RegexDiagnosticCode Status, RegexFlags Flags, int Position) Read(string text)
    {
        var status = RegexFlagsText.Parse(text, out var flags, out var diagnostic);
        if (status != (diagnostic.Code == RegexDiagnosticCode.None ? RegexStatus.Success : RegexStatus.SyntaxError))
            Assert.Fail(Show(text) + " answered " + status + " with the diagnostic " + diagnostic.Code);
        return (diagnostic.Code, flags, diagnostic.Position);
    }

    /// <summary>
    /// A refusal is the validate entry's diagnostic for the same flags text: the code, its message identifier, the code
    /// unit refused as the argument (none for <c>u</c> with <c>v</c>) and the position, and the message the throwing reader
    /// gives (task 2.9 of SJL-11.2 folded the flags' own status, <c>RegexFlagsStatus</c>, into the codes 1 to 3).
    /// </summary>
    [Theory]
    [InlineData("gx", RegexDiagnosticCode.UnknownFlag, 1, "x", "regexp.flags.unknown")]
    [InlineData("gig", RegexDiagnosticCode.DuplicateFlag, 2, "g", "regexp.flags.duplicate")]
    [InlineData("vu", RegexDiagnosticCode.UnicodeWithUnicodeSets, 1, null, "regexp.flags.unicode-with-unicode-sets")]
    [InlineData("uvx", RegexDiagnosticCode.UnknownFlag, 2, "x", "regexp.flags.unknown")]
    public void A_refusal_is_the_syntax_status_with_the_validate_entry_s_diagnostic(string text, RegexDiagnosticCode code, int position, string? argument, string messageId)
    {
        Assert.Equal(RegexStatus.SyntaxError, RegexFlagsText.Parse(text, out var flags, out var diagnostic));
        Assert.Equal(RegexFlags.None, flags);
        Assert.Equal((code, position, argument, messageId), (diagnostic.Code, diagnostic.Position, diagnostic.Argument, diagnostic.MessageId));
        Assert.Equal(Assert.Throws<RegexSyntaxException>(() => RegexFlagsParser.Parse(text)).Message, diagnostic.Message);

        Assert.Equal(RegexStatus.SyntaxError, BroilerRegex.Validate("a", text, Broiler.Unicode.Tables.UnicodeTables.Instance, 512, out var validated, out _));
        Assert.Equal((validated.Code, validated.Position, validated.Argument, validated.Message), (diagnostic.Code, diagnostic.Position, diagnostic.Argument, diagnostic.Message));

        Assert.Equal(RegexStatus.Success, RegexFlagsText.Parse("dgimsuy", out _, out var none));
        Assert.Equal((RegexDiagnosticCode.None, -1), (none.Code, none.Position));
    }

    /// <summary>Every ordering of every subset of <paramref name="letters"/>: 109,601 strings for the eight flags.</summary>
    private static IEnumerable<string> Arrangements(string letters)
    {
        var stack = new Stack<string>();
        stack.Push("");
        while (stack.Count > 0)
        {
            var prefix = stack.Pop();
            yield return prefix;
            foreach (var letter in letters)
            {
                if (prefix.IndexOf(letter, StringComparison.Ordinal) < 0)
                    stack.Push(prefix + letter);
            }
        }
    }

    [Fact]
    public void Every_ordering_of_every_set_of_the_eight_flags_reads_as_the_specification_reads_it()
    {
        var count = 0;
        var refusedForBothModes = 0;
        foreach (var text in Arrangements(Letters))
        {
            var expected = Specification(text);
            Assert.True(expected == Read(text), text + ": " + Read(text) + " where the specification reads " + expected);
            if (expected.Status == RegexDiagnosticCode.None)
                Assert.Equal(new string(Letters.Where(text.Contains).ToArray()), RegexFlagsText.Format(expected.Flags));
            else
                refusedForBothModes++;
            count++;
        }

        // Not vacuous: 109,601 strings, of which the 84,158 holding both u and v (every order of every set with both) are
        // refused, at the later of the two.
        Assert.Equal(109601, count);
        Assert.Equal(84158, refusedForBothModes);
    }

    [Fact]
    public void Every_code_unit_alone_is_its_flag_or_an_unknown_flag()
    {
        for (var unit = 0; unit <= 0xFFFF; unit++)
        {
            var text = ((char)unit).ToString();
            var letter = Letters.IndexOf((char)unit, StringComparison.Ordinal);
            var expected = letter >= 0
                ? (RegexDiagnosticCode.None, FlagOfLetter[letter], -1)
                : (RegexDiagnosticCode.UnknownFlag, RegexFlags.None, 0);
            Assert.True(expected == Read(text), "U+" + unit.ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    /// <summary>The flags and code units a reader could take for one: upper case, a space, NUL, look-alikes, a lone surrogate.</summary>
    private const string Strangers = "xGUY \0\u017F\u0131\uFF47\u212A\uD800\uDC00";

    [Fact]
    public void Every_string_of_up_to_three_code_units_over_the_flags_and_strangers_reads_as_the_specification_reads_it()
    {
        var alphabet = Letters + Strangers;
        var texts = new List<string> { "" };
        var layer = new List<string> { "" };
        for (var length = 1; length <= 3; length++)
        {
            layer = layer.SelectMany(prefix => alphabet.Select(c => prefix + c)).ToList();
            texts.AddRange(layer);
        }

        Assert.Equal(1 + 20 + 400 + 8000, texts.Count);
        foreach (var text in texts)
            Assert.True(Specification(text) == Read(text), Show(text) + ": " + Read(text) + " where the specification reads " + Specification(text));
    }

    [Theory]
    [InlineData("", RegexDiagnosticCode.None, RegexFlags.None, -1)]
    [InlineData("g", RegexDiagnosticCode.None, RegexFlags.Global, -1)]
    [InlineData("yg", RegexDiagnosticCode.None, RegexFlags.Global | RegexFlags.Sticky, -1)]
    [InlineData("v", RegexDiagnosticCode.None, RegexFlags.UnicodeSets, -1)]
    [InlineData("gg", RegexDiagnosticCode.DuplicateFlag, RegexFlags.None, 1)]
    [InlineData("gimg", RegexDiagnosticCode.DuplicateFlag, RegexFlags.None, 3)]
    [InlineData("x", RegexDiagnosticCode.UnknownFlag, RegexFlags.None, 0)]
    [InlineData("gx", RegexDiagnosticCode.UnknownFlag, RegexFlags.None, 1)]
    [InlineData("G", RegexDiagnosticCode.UnknownFlag, RegexFlags.None, 0)]
    [InlineData("g ", RegexDiagnosticCode.UnknownFlag, RegexFlags.None, 1)]
    [InlineData("\0", RegexDiagnosticCode.UnknownFlag, RegexFlags.None, 0)]
    [InlineData("uv", RegexDiagnosticCode.UnicodeWithUnicodeSets, RegexFlags.None, 1)]
    [InlineData("vu", RegexDiagnosticCode.UnicodeWithUnicodeSets, RegexFlags.None, 1)]
    [InlineData("ugv", RegexDiagnosticCode.UnicodeWithUnicodeSets, RegexFlags.None, 2)]
    [InlineData("dgimsuvy", RegexDiagnosticCode.UnicodeWithUnicodeSets, RegexFlags.None, 6)]
    // An unknown or repeated code unit anywhere is refused before u with v, which ParsePattern refuses only after.
    [InlineData("uvx", RegexDiagnosticCode.UnknownFlag, RegexFlags.None, 2)]
    [InlineData("uvu", RegexDiagnosticCode.DuplicateFlag, RegexFlags.None, 2)]
    [InlineData("vuv", RegexDiagnosticCode.DuplicateFlag, RegexFlags.None, 2)]
    public void A_refusal_names_its_kind_and_the_code_unit(string text, RegexDiagnosticCode status, RegexFlags flags, int position) =>
        Assert.Equal((status, flags, position), Read(text));

    [Fact]
    public void A_refusal_is_decided_by_the_ninth_code_unit_whatever_the_length()
    {
        // Eight distinct flags exist, so the ninth code unit is unknown or a repeat: the work does not grow with the text,
        // which is why the reader takes no meter.
        Assert.Equal((RegexDiagnosticCode.DuplicateFlag, RegexFlags.None, 8), Read("dgimsuyv" + new string('g', 1_000_000)));
        Assert.Equal((RegexDiagnosticCode.UnknownFlag, RegexFlags.None, 8), Read("dgimsuyv" + new string('x', 1_000_000)));
        Assert.Equal((RegexDiagnosticCode.UnknownFlag, RegexFlags.None, 0), Read(new string('x', 1_000_000)));
    }

    [Fact]
    public void Format_writes_every_combination_of_the_flags_in_the_getter_s_order()
    {
        for (var bits = 0; bits < 256; bits++)
        {
            var flags = RegexFlags.None;
            var expected = new StringBuilder();
            for (var letter = 0; letter < Letters.Length; letter++)
            {
                // The bit of each flag, in the getter's order: d g i m s u v y.
                if ((bits & (1 << letter)) != 0)
                {
                    flags |= FlagOfLetter[letter];
                    expected.Append(Letters[letter]);
                }
            }

            var text = RegexFlagsText.Format(flags);
            Assert.Equal(expected.ToString(), text);

            // The text reads back as the flags, but u with v, which the reader refuses at the later of the two.
            var both = (flags & (RegexFlags.Unicode | RegexFlags.UnicodeSets)) == (RegexFlags.Unicode | RegexFlags.UnicodeSets);
            Assert.Equal(both ? (RegexDiagnosticCode.UnicodeWithUnicodeSets, RegexFlags.None, text.IndexOf('v', StringComparison.Ordinal)) : (RegexDiagnosticCode.None, flags, -1), Read(text));
        }

        // ECMA-262's own example of the order (§22.2.6.4): new RegExp("x", "yg").flags is "gy".
        Assert.Equal("gy", RegexFlagsText.Format(RegexFlags.Sticky | RegexFlags.Global));
        Assert.Same(string.Empty, RegexFlagsText.Format(RegexFlags.None));
    }

    [Fact]
    public void Format_refuses_a_bit_no_flag_defines_as_a_broken_precondition()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RegexFlagsText.Format((RegexFlags)256));
        Assert.Throws<ArgumentOutOfRangeException>(() => RegexFlagsText.Format(RegexFlags.Global | (RegexFlags)(1 << 30)));
    }

    /// <summary>
    /// <c>RegexFlagsParser.Parse</c>, the throwing reader of <c>0.1.0-preview.4</c>, reads the same grammar and answers as
    /// it did: the flags, or a <see cref="RegexSyntaxException"/> with its message and no position.
    /// </summary>
    [Fact]
    public void The_throwing_reader_answers_as_before_through_the_same_grammar()
    {
        foreach (var text in Arrangements(Letters).Where(static t => !(t.Contains('u') && t.Contains('v'))))
            Assert.Equal(Specification(text).Flags, RegexFlagsParser.Parse(text));
        Assert.Equal(RegexFlags.None, RegexFlagsParser.Parse(null));

        Assert.Equal(("Invalid regular expression flags: 'x'", -1), Refusal("gx"));
        Assert.Equal(("Duplicate regular expression flag: 'g'", -1), Refusal("gig"));
        Assert.Equal(("The 'u' and 'v' regular expression flags cannot be combined", -1), Refusal("vu"));
        Assert.Equal(("Invalid regular expression flags: 'x'", -1), Refusal("uvx"));

        static (string Message, int Position) Refusal(string text)
        {
            var refusal = Assert.Throws<RegexSyntaxException>(() => RegexFlagsParser.Parse(text));
            return (refusal.Message, refusal.Position);
        }
    }

    private static string Show(string text) =>
        string.Concat(text.Select(static c => c is >= ' ' and <= '~' ? c.ToString() : "\\u" + ((int)c).ToString("X4", System.Globalization.CultureInfo.InvariantCulture)));
}
