namespace Broiler.Regex.Tests;

/// <summary>
/// Where the Annex B (non-Unicode) grammar and the strict Unicode grammar disagree. Each
/// case below came out of a differential run against V8: the same pattern is a literal in
/// one mode and a syntax error in the other, and getting that boundary wrong silently
/// changes what a legacy pattern matches.
/// </summary>
public class AnnexBGrammarTests
{
    private static string? Match(string pattern, string flags, string input)
    {
        var m = new BroilerRegex(pattern, flags).Match(input);
        return m.Success ? m.Value : null;
    }

    [Theory]
    // `\u{…}` is a code-point escape only in Unicode mode. Outside it, `\u` is the identity
    // escape `u` and the braces are an ordinary quantifier or literal text.
    [InlineData(@"\u{2}", "uu")]
    [InlineData(@"\u", "u")]
    [InlineData(@"\uZZ", "uZZ")]
    [InlineData(@"\u12", "u12")]
    [InlineData(@"\x", "x")]
    [InlineData(@"\xZZ", "xZZ")]
    public void MalformedHexEscape_IsAnIdentityEscapeOutsideUnicodeMode(string pattern, string input)
        => Assert.Equal(input, Match(pattern, "", input));

    [Fact(Timeout = 600000)]
    public void BracedUnicodeEscape_IsACodePointOnlyInUnicodeMode()
    {
        Assert.Equal("\U0001F600", Match(@"\u{1F600}", "u", "\U0001F600"));
        Assert.Null(Match(@"\u{1F600}", "", "\U0001F600"));
        // …which is what makes this class a range from '}' to 'u' — out of order, and so a
        // syntax error rather than an emoji range.
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(@"[\u{1F600}-\u{1F64F}]", ""));
    }

    [Theory]
    // Unicode mode drops Annex B's ExtendedPatternCharacter, so these stand for themselves
    // only without `u`/`v`.
    [InlineData("]")]
    [InlineData("}")]
    [InlineData("{")]
    [InlineData("a{")]
    [InlineData("a{1")]
    public void UnescapedBraceOrBracket_IsLiteralOnlyOutsideUnicodeMode(string pattern)
    {
        Assert.NotNull(Match(pattern, "", pattern));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(pattern, "u"));
    }

    [Theory]
    // A range endpoint that is itself a class is an early error in Unicode mode; Annex B
    // rescues it as a literal '-' between the two atoms.
    [InlineData(@"[\d-a]")]
    [InlineData(@"[a-\d]")]
    [InlineData(@"[\w-\d]")]
    public void ClassEscapeAsRangeEndpoint_IsAnErrorOnlyInUnicodeMode(string pattern)
    {
        Assert.Equal("-", Match(pattern, "", "-"));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(pattern, "u"));
    }

    [Fact(Timeout = 600000)]
    public void PropertyEscapeAsRangeEndpoint_FailsForADifferentReasonInEachMode()
    {
        // Unicode mode: `\p{L}` is a class, so it cannot end a range. Without the flag it
        // is the six characters `p{L}` — and the class then asks for the range `}` to `a`,
        // which is out of order.
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(@"[\p{L}-a]", "u"));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(@"[\p{L}-a]", ""));
    }

    [Fact(Timeout = 600000)]
    public void TrailingDashInAClassStaysLiteralInBothModes()
    {
        Assert.Equal("-", Match(@"[\d-]", "u", "-"));
        Assert.Equal("-", Match("[-a]", "u", "-"));
        Assert.Equal("-", Match("[a-]", "u", "-"));
    }

    [Theory]
    // Annex B `ExtendedTerm :: QuantifiableAssertion Quantifier` — a look-AHEAD, and only
    // a look-ahead, may carry a quantifier outside Unicode mode.
    [InlineData(@"(?!a)+", "b")]
    [InlineData(@"(?!a)*", "b")]
    [InlineData(@"(?!a){1,3}", "b")]
    [InlineData(@"(?!a)+?", "b")]
    [InlineData(@"(?=a){0}", "b")]
    public void QuantifiedLookahead_IsAllowedOutsideUnicodeMode(string pattern, string input)
    {
        Assert.Equal("", Match(pattern, "", input));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(pattern, "u"));
    }

    [Theory]
    // Every other assertion is unquantifiable in both modes.
    [InlineData(@"(?<!a){2,}")]
    [InlineData(@"(?<=a)*")]
    [InlineData(@"(?<!a)?")]
    [InlineData("^{2}")]
    [InlineData("^*")]
    [InlineData("^{2,}")]
    [InlineData(@"\b*")]
    [InlineData(@"\B{1}")]
    [InlineData("$?")]
    public void QuantifiedNonLookahead_IsASyntaxError(string pattern)
    {
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(pattern, ""));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(pattern, "u"));
    }

    [Fact(Timeout = 600000)]
    public void ABraceThatIsNotAQuantifierStillFollowsAnAssertion()
    {
        // `{a}` cannot be a quantifier, so it is literal text after the anchor — only a
        // well-formed `{n,m}` makes the assertion a quantified one.
        Assert.Equal("{a}", Match("^{a}", "", "{a}"));
        Assert.Null(Match(@"\b{a}", "", "{a}"));
        Assert.Equal("{", Match("^{", "", "{"));
    }

    [Fact(Timeout = 600000)]
    public void NamedBackreferenceNeedsADeclaredNameSomewhereOutsideUnicodeMode()
    {
        // With no GroupName anywhere, Annex B reads `\k` as the identity escape `k`, so
        // the pattern is the literal text "k<n>" rather than a reference to nothing.
        Assert.Equal("k<n>", Match(@"\k<n>", "", "k<n>"));
        Assert.Equal("k", Match(@"\k", "", "k"));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(@"\k<n>", "u"));

        // Once the pattern declares one, `\k` is a reference again — including a forward
        // one — and naming a group that does not exist is an error.
        Assert.Equal("aa", Match(@"(?<n>a)\k<n>", "", "aa"));
        Assert.Equal("a", Match(@"\k<n>(?<n>a)", "", "a"));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(@"(?<n>a)\k<m>", ""));
    }

    [Fact(Timeout = 600000)]
    public void EscapedDashIsAClassEscapeButNotAnIdentityEscape()
    {
        // `ClassEscape :: -` makes `\-` a literal hyphen inside a class in every mode,
        // while outside a class Unicode mode admits only a SyntaxCharacter or '/'.
        Assert.Equal("-", Match(@"[\-]", "u", "-"));
        Assert.Equal("-", Match(@"[a\-z]", "u", "-"));
        Assert.Equal("-", Match(@"[\-]", "v", "-"));
        Assert.Equal("-", Match(@"\-", "", "-"));
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(@"\-", "u"));
    }
}
