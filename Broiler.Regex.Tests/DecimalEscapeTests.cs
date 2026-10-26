using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The escapes that begin with an ASCII digit. <c>CharacterEscape :: 0 [lookahead ∉ DecimalDigit]</c> looks ahead
/// at the ASCII digits alone (ECMA-262 §22.2.1, where DecimalDigit is 0 to 9), so <c>\0</c> before any other
/// digit, such as U+0661 ARABIC-INDIC DIGIT ONE, is NUL and then that digit in every mode; 0.1.0-preview.4 looked
/// ahead with <c>char.IsDigit</c>, read <c>\0</c> before such a digit as the character 0 outside Unicode mode and
/// refused it under <c>u</c> and <c>v</c>. Outside Unicode mode, Annex B's LegacyOctalEscapeSequence (§B.1.2)
/// reads <c>\0</c> before an ASCII digit and a class's <c>\1</c> to <c>\7</c> as octal; under <c>u</c> and
/// <c>v</c> each is a syntax error.
/// </summary>
public class DecimalEscapeTests
{
    /// <summary>Every mode: without <c>u</c> and <c>v</c>, under each, and with <c>i</c>.</summary>
    public static readonly TheoryData<string> Modes = new() { "", "u", "v", "i", "iu", "iv" };

    /// <summary>Digits that are not ASCII: Arabic-Indic, Devanagari and fullwidth one, and an astral one.</summary>
    private static readonly string[] OtherDigits = ["\u0661", "\u0967", "\uFF11", "\U0001D7CF"];

    [Theory]
    [MemberData(nameof(Modes))]
    public void Nul_before_a_digit_that_is_not_ASCII_is_NUL_then_that_digit(string flags)
    {
        foreach (var digit in OtherDigits)
        {
            var regex = TestRegex.Compile(@"\0" + digit, flags);
            var match = regex.Match("x\u0000" + digit);
            Assert.True(match.Success, "U+" + char.ConvertToUtf32(digit, 0).ToString("X4") + " /" + flags);
            Assert.Equal(1, match.Index);
            Assert.Equal("\u0000" + digit, match.Value);
            Assert.False(regex.IsMatch("0" + digit));

            // In a class, \0 is NUL beside that digit.
            var cls = TestRegex.Compile(@"^[\0" + digit + "]+$", flags);
            Assert.True(cls.IsMatch("\u0000" + digit + "\u0000"));
            Assert.False(cls.IsMatch("0"));
        }
    }

    /// <summary>Outside Unicode mode, Annex B's LegacyOctalEscapeSequence, with what each pattern matches.</summary>
    [Theory]
    [InlineData(@"\00", "\u0000")]
    [InlineData(@"\01", "\u0001")]
    [InlineData(@"\07", "\u0007")]
    [InlineData(@"\012", "\u000A")]
    [InlineData(@"\0377", "\u001F7")]                   // three digits at most: \037, then 7
    [InlineData(@"\08", "\u00008")]                     // 0 [lookahead ∈ {8, 9}]: NUL, then 8
    [InlineData(@"\09", "\u00009")]
    [InlineData(@"[\1]", "\u0001")]
    [InlineData(@"[\7]", "\u0007")]
    [InlineData(@"[\00]", "\u0000")]
    [InlineData(@"[\12]", "\u000A")]
    [InlineData(@"[\101]", "A")]
    [InlineData(@"[\377]", "\u00FF")]
    [InlineData(@"[\400]+", "\u0020\u00200")]           // above 0377: \40, then 0
    [InlineData(@"[\8]", "8")]                          // 8 and 9 are no octal digits: the identity escape
    [InlineData(@"[\9]", "9")]
    [InlineData(@"[\1-\3]+", "\u0001\u0002\u0003")]
    public void Outside_Unicode_mode_a_legacy_octal_escape_is_its_code_unit(string pattern, string expected)
    {
        var match = TestRegex.Compile(pattern).Match("x" + expected + "x");
        Assert.True(match.Success, pattern);
        Assert.Equal(expected, match.Value);
    }

    [Theory]
    [InlineData(@"\00", "u")]
    [InlineData(@"\01", "u")]
    [InlineData(@"\08", "u")]
    [InlineData(@"[\00]", "u")]
    [InlineData(@"[\1]", "u")]
    [InlineData(@"[\7]", "u")]
    [InlineData(@"[\8]", "u")]
    [InlineData(@"\01", "v")]
    [InlineData(@"[\1]", "v")]
    [InlineData(@"[\01]", "v")]
    public void Under_u_and_v_a_legacy_octal_escape_is_a_syntax_error(string pattern, string flags)
        => Assert.Throws<RegexSyntaxException>(() => TestRegex.Compile(pattern, flags));

    /// <summary>A bare <c>\0</c> stays NUL in every mode, at the end of a pattern, before a letter or a class's end.</summary>
    [Theory]
    [MemberData(nameof(Modes))]
    public void Nul_before_no_digit_is_NUL_in_every_mode(string flags)
    {
        Assert.Equal("\u0000", TestRegex.Compile(@"\0", flags).Match("a\u0000").Value);
        Assert.Equal("\u0000a", TestRegex.Compile(@"\0a", flags).Match("\u0000a").Value);
        Assert.Equal("\u0000", TestRegex.Compile(@"[\0]", flags).Match("a\u0000").Value);
    }

    /// <summary>An atom's <c>\1</c> to <c>\9</c> is a back-reference where the group exists, as it was.</summary>
    [Fact]
    public void An_atom_numeric_escape_is_still_a_back_reference_first()
    {
        Assert.Equal("aa", TestRegex.Compile(@"(a)\1").Match("aa").Value);
        Assert.Equal("aa", TestRegex.Compile(@"(a)\1", "u").Match("aa").Value);
        Assert.Equal("\u0001", TestRegex.Compile(@"\1").Match("\u0001").Value);
        Assert.Throws<RegexSyntaxException>(() => TestRegex.Compile(@"\1", "u"));
    }
}
