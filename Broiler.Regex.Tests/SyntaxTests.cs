namespace Broiler.Regex.Tests;

public class SyntaxTests
{
    [Theory]
    [InlineData("(")]            // unterminated group
    [InlineData("[a")]           // unterminated class
    [InlineData("a{2,1}")]       // m < n
    [InlineData("(?<x>a)\\k<y>")] // reference to a name the pattern does not declare
    public void InvalidPatterns_Throw(string pattern)
    {
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(pattern));
    }

    [Fact(Timeout = 600000)]
    public void LoneNamedBackreference_IsLiteralOutsideUnicodeMode()
    {
        // `\k<x>` in a pattern that declares no group name is not a broken reference: the
        // Annex B grammar reads `\k` as the identity escape, so the pattern is literal
        // text. It is an error only under `u`/`v`. See AnnexBGrammarTests.
        Assert.True(new BroilerRegex("\\k<x>").Match("k<x>").Success);
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex("\\k<x>", "u"));
    }

    [Theory]
    [InlineData("gg")]           // duplicate flag
    [InlineData("uv")]           // u and v together
    [InlineData("q")]            // unknown flag
    public void InvalidFlags_Throw(string flags)
    {
        Assert.Throws<RegexSyntaxException>(() => new BroilerRegex("a", flags));
    }

    [Fact(Timeout = 600000)]
    public void ValidFlags_AreParsed()
    {
        var re = new BroilerRegex("a", "gimsuyd");
        Assert.True((re.Flags & RegexFlags.Global) != 0);
        Assert.True((re.Flags & RegexFlags.IgnoreCase) != 0);
        Assert.True((re.Flags & RegexFlags.Unicode) != 0);
    }
}
