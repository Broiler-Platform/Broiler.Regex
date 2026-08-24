namespace Broiler.Regex.Tests;

/// <summary>
/// <c>\p{…}</c> / <c>\P{…}</c> Unicode property escapes, resolved from the pinned
/// Broiler.Unicode range tables. Before this they parsed and then threw
/// <c>NotSupportedException</c>, so every pattern using one had to be handed back to the
/// .NET translator (Broiler.JS gaps roadmap, track 2 action 1).
/// </summary>
public class UnicodePropertyEscapeTests
{
    private static string? Match(string pattern, string flags, string input)
    {
        var m = new BroilerRegex(pattern, flags).Match(input);
        return m.Success ? m.Value : null;
    }

    [Theory]
    // General_Category, as a lone value and through the gc= dimension.
    [InlineData(@"\p{L}", "u", "a", "a")]
    [InlineData(@"\p{L}", "u", "1", null)]
    [InlineData(@"\p{Lu}", "u", "A", "A")]
    [InlineData(@"\p{Lu}", "u", "a", null)]
    [InlineData(@"\p{Lowercase_Letter}", "u", "a", "a")]
    [InlineData(@"\p{General_Category=Letter}", "u", "a", "a")]
    [InlineData(@"\p{gc=Nd}", "u", "7", "7")]
    // Binary properties, canonical name and ECMAScript alias.
    [InlineData(@"\p{Alphabetic}", "u", "a", "a")]
    [InlineData(@"\p{Alpha}", "u", "a", "a")]
    [InlineData(@"\p{ASCII}", "u", "a", "a")]
    [InlineData(@"\p{ASCII}", "u", "é", null)]
    // Script and Script_Extensions.
    [InlineData(@"\p{Script=Greek}", "u", "α", "α")]
    [InlineData(@"\p{sc=Greek}", "u", "a", null)]
    [InlineData(@"\p{Script_Extensions=Greek}", "u", "α", "α")]
    [InlineData(@"\p{scx=Latin}", "u", "a", "a")]
    // A property escape is a set, so it complements and combines like any other.
    [InlineData(@"\P{L}", "u", "1", "1")]
    [InlineData(@"\P{L}", "u", "a", null)]
    [InlineData(@"[\p{L}\p{Nd}]+", "u", "a1", "a1")]
    [InlineData(@"[\P{L}]", "u", "1", "1")]
    [InlineData(@"[\P{L}]", "u", "a", null)]
    [InlineData(@"[^\p{L}]", "u", "1", "1")]
    [InlineData(@"[^\p{L}]", "u", "a", null)]
    public void PropertyEscape_MatchesItsSet(string pattern, string flags, string input, string? expected)
        => Assert.Equal(expected, Match(pattern, flags, input));

    [Theory]
    // A property escape is code-point based, so a supplementary-plane member matches as
    // one unit rather than as two code units.
    [InlineData(@"\p{Script=Deseret}", "\U00010400")]
    [InlineData(@"\p{Script=Osage}", "\U000104B0")]
    [InlineData(@"\p{Script=Adlam}", "\U0001E900")]
    public void PropertyEscape_MatchesAstralMembersAsOneCodePoint(string pattern, string input)
    {
        var m = new BroilerRegex(pattern, "u").Match(input);
        Assert.True(m.Success);
        Assert.Equal(input, m.Value);
    }

    [Fact(Timeout = 600000)]
    public void PropertyEscape_QuantifiesOverCodePoints()
        => Assert.Equal("\U00010400\U00010401", Match(@"\p{Script=Deseret}+", "u", "\U00010400\U00010401x"));

    [Theory]
    // Case closure runs over the property's set, so an `i` match reaches the other case
    // even though the set itself holds only one of them.
    [InlineData(@"\p{Lu}", "ui", "a", "a")]
    [InlineData(@"\p{Ll}", "ui", "A", "A")]
    [InlineData(@"\P{Lu}", "ui", "a", "a")]
    [InlineData(@"[^\p{Lu}]", "ui", "a", null)]
    public void PropertyEscape_UnderIgnoreCase(string pattern, string flags, string input, string? expected)
        => Assert.Equal(expected, Match(pattern, flags, input));

    [Theory]
    [InlineData(@"\p{Bogus_Property}")]
    [InlineData(@"\p{Script=Nonesuch}")]
    [InlineData(@"\p{gc=Nonesuch}")]
    [InlineData(@"\p{L")]
    [InlineData(@"\pL")]
    public void UnknownOrMalformedProperty_IsASyntaxError(string pattern)
        => Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(pattern, "u"));

    [Fact(Timeout = 600000)]
    public void PropertyOfStrings_NeedsTheVFlag()
    {
        // \p{RGI_Emoji} matches multi-code-point sequences, which only a `v`-mode class
        // can hold — under `u` it is a SyntaxError rather than a silent mis-match.
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(@"\p{RGI_Emoji}", "u"));
        Assert.NotNull(Match(@"\p{RGI_Emoji}", "v", "\U0001F600"));
    }

    [Fact(Timeout = 600000)]
    public void WithoutUnicodeFlag_BackslashPIsAnIdentityEscape()
    {
        // Annex B: outside Unicode mode `\p` is the character `p`, so the pattern is the
        // literal text "p{L}" — not a property escape and not an error.
        Assert.Equal("p{L}", Match(@"\p{L}", "", "p{L}"));
        Assert.Null(Match(@"\p{L}", "", "a"));
    }
}
