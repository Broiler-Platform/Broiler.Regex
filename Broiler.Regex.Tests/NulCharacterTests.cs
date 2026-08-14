namespace Broiler.Regex.Tests;

/// <summary>
/// U+0000 is an ordinary pattern character, and the end of the pattern is a
/// separate condition from it.
/// <para>
/// The parser's cursor returns <c>'\0'</c> for "past the last character", and every
/// end-of-pattern test used to compare against that sentinel. A pattern that merely
/// <em>contained</em> a NUL therefore looked as though it had ended there: the
/// "no letters" character class google.com compiles on its start page — whose first
/// range runs from NUL to space — failed with <c>Unterminated character class</c> at
/// the very first atom.
/// </para>
/// <para>
/// It arrives as a decoded U+0000 because the page builds it with
/// <c>new RegExp(<em>string</em>)</c>: the string literal's <c>\0</c> is consumed by
/// the *string* grammar, never reaching the pattern grammar. So the two spellings are
/// different inputs to this parser, and only the decoded one was broken — a regex
/// literal keeps its backslash, and <c>[\0-…]</c> parsed correctly all along. Both are
/// covered below.
/// </para>
/// <para>
/// Every NUL here is written as a <c>\uXXXX</c> escape. Raw control bytes in the source
/// would be invisible to a reviewer, and would make <c>git format-patch</c> treat this
/// file as binary — which matters while the fix travels to the submodule as a patch.
/// </para>
/// </summary>
public class NulCharacterTests
{
    /// <summary>
    /// The reported pattern, decoded exactly as <c>new RegExp(string)</c> hands it over,
    /// with one range per line so the leading NUL is legible.
    /// </summary>
    private const string GoogleNoLetterClass =
        "^[" +
        "\u0000- " + // NUL … space — the range that used to end the parse
        "!-@" +          // U+0021 … U+0040
        "[-`" +          // U+005B … U+0060
        "{-¿" +          // U+007B … U+00BF
        "×÷" +           // U+00D7 and U+00F7
        "ʹ-˿" +          // U+02B9 … U+02FF
        "\u2000-⯿" +   // U+2000 … U+2BFF
        "]*$";

    [Fact]
    public void GoogleNoLetterClass_Compiles_AndMatchesOnlyNonLetters()
    {
        var re = new BroilerRegex(GoogleNoLetterClass);

        Assert.True(re.IsMatch(" ,.!"));
        Assert.True(re.IsMatch("\u0000"));
        Assert.True(re.IsMatch("×÷"));
        Assert.False(re.IsMatch("a"));
        Assert.False(re.IsMatch("abc"));
    }

    /// <summary>
    /// The two spellings of one class — a decoded NUL and the <c>\0</c> escape —
    /// compile and agree. Only the first was broken.
    /// </summary>
    [Theory]
    [InlineData("[\u0000- ]")]
    [InlineData("[\\0- ]")]
    public void NulStartedRange_MatchesControlsAndSpace(string pattern)
    {
        var re = new BroilerRegex(pattern);
        Assert.True(re.IsMatch("\u0000"));
        Assert.True(re.IsMatch(" "));
        Assert.True(re.IsMatch("\n"));
        Assert.False(re.IsMatch("a"));
    }

    [Fact]
    public void NulInsideClass_IsAMember_NotTheEndOfTheClass()
    {
        var re = new BroilerRegex("[a\u0000b]");
        Assert.True(re.IsMatch("\u0000"));
        Assert.True(re.IsMatch("a"));
        Assert.True(re.IsMatch("b"));
        Assert.False(re.IsMatch("c"));
    }

    [Fact]
    public void NulAsRangeEnd_IsARange_NotTheEndOfTheClass()
    {
        // '-' before a NUL is a range operator like any other: [\0-\0] is the
        // one-member class, and the range is still validated for order.
        Assert.True(new BroilerRegex("[\u0000-\u0000]").IsMatch("\u0000"));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex("[a-\u0000]"));
    }

    [Fact]
    public void NulAsAtom_IsMatchedLiterally_AndDoesNotTruncateThePattern()
    {
        var re = new BroilerRegex("a\u0000b");
        Assert.True(re.IsMatch("a\u0000b"));
        Assert.False(re.IsMatch("ab"));

        // Everything after the NUL is still parsed: the quantifier binds to 'b'.
        var quantified = new BroilerRegex("^a\u0000b+$");
        Assert.True(quantified.IsMatch("a\u0000bbb"));
        Assert.False(quantified.IsMatch("a\u0000"));
    }

    [Fact]
    public void NulSurvivesAlternationAndQuantifiers()
    {
        var re = new BroilerRegex("^(x\u0000|y)*$");
        Assert.True(re.IsMatch("x\u0000y"));
        Assert.False(re.IsMatch("x"));

        Assert.True(new BroilerRegex("^\u0000*$").IsMatch("\u0000\u0000"));
    }

    [Fact]
    public void NulInAGroupName_IsPartOfTheName()
    {
        var re = new BroilerRegex("(?<a\u0000b>x)");
        Assert.Equal(1, re.GroupNames["a\u0000b"]);
        Assert.Equal("x", re.Match("x").Groups[1].Value);
    }

    /// <summary>
    /// The other half of telling end-of-pattern apart from the sentinel: a pattern
    /// ending in a lone '\' has no character to escape. Reading it used to run off the
    /// end of the string and raise <see cref="System.IndexOutOfRangeException"/> in
    /// non-Unicode mode — an unhandled runtime fault where callers catch
    /// <see cref="RegexSyntaxException"/>.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("u")]
    public void TrailingBackslash_IsASyntaxError(string? flags)
    {
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex("a\\", flags));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex("[a\\", flags));
    }

    [Fact]
    public void GenuinelyUnterminatedConstructs_AreStillReported()
    {
        // The end-of-pattern rewrite must not soften these: each really does end
        // before its closer.
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex("[a"));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex("[a-"));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex("(a"));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex("(?<name"));
    }
}
