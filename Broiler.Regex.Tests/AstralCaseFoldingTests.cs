namespace Broiler.Regex.Tests;

/// <summary>
/// Case-insensitive matching above the BMP. <c>char.ToLowerInvariant</c> /
/// <c>ToUpperInvariant</c> are defined on one UTF-16 code unit and leave every
/// supplementary code point alone, so each was its own fold class and <c>/𐐀/iu</c> matched
/// only the capital — the failure test262
/// <c>staging/sm/RegExp/unicode-ignoreCase.js</c> reports for the bicameral supplementary
/// scripts (Broiler.JS issue #975).
/// </summary>
public class AstralCaseFoldingTests
{
    private const string DeseretCapitalLongI = "\U00010400";
    private const string DeseretSmallLongI = "\U00010428";
    private const string WarangCitiCapitalNgaa = "\U000118A0";
    private const string WarangCitiSmallNgaa = "\U000118C0";
    private const string GarayCapitalCa = "\U00010D50";
    private const string GaraySmallCa = "\U00010D70";

    [Theory]
    [InlineData(DeseretCapitalLongI, DeseretSmallLongI)]
    [InlineData(DeseretSmallLongI, DeseretCapitalLongI)]
    [InlineData(WarangCitiCapitalNgaa, WarangCitiSmallNgaa)]
    [InlineData(WarangCitiSmallNgaa, WarangCitiCapitalNgaa)]
    // Garay was added in Unicode 16.0 and carries no simple case mapping in .NET's tables
    // yet, so CaseFolding seeds its pairs itself.
    [InlineData(GarayCapitalCa, GaraySmallCa)]
    [InlineData(GaraySmallCa, GarayCapitalCa)]
    public void AstralAtom_MatchesItsFoldEquivalent(string pattern, string equivalent)
    {
        var re = new BroilerRegex(pattern, RegexFlags.IgnoreCase | RegexFlags.Unicode);
        var m = re.Match("<" + equivalent + ">");
        Assert.True(m.Success);
        Assert.Equal(equivalent, m.Value);
    }

    [Theory]
    [InlineData(DeseretCapitalLongI, DeseretSmallLongI)]
    [InlineData(WarangCitiCapitalNgaa, WarangCitiSmallNgaa)]
    public void AstralClassMember_MatchesItsFoldEquivalent(string member, string equivalent)
    {
        var re = new BroilerRegex("[" + member + "]+", RegexFlags.IgnoreCase | RegexFlags.Unicode);
        var m = re.Match("<" + member + equivalent + ">");
        Assert.True(m.Success);
        Assert.Equal(member + equivalent, m.Value);
    }

    [Fact(Timeout = 600000)]
    public void AstralAtom_DoesNotFoldWithoutIgnoreCase()
    {
        var re = new BroilerRegex(DeseretCapitalLongI, RegexFlags.Unicode);
        Assert.False(re.Match(DeseretSmallLongI).Success);
        Assert.True(re.Match(DeseretCapitalLongI).Success);
    }

    [Fact(Timeout = 600000)]
    public void AstralAtom_DoesNotFoldAcrossUnrelatedCodePoints()
    {
        var re = new BroilerRegex(DeseretCapitalLongI, RegexFlags.IgnoreCase | RegexFlags.Unicode);
        Assert.False(re.Match(WarangCitiSmallNgaa).Success);
        Assert.False(re.Match("\U00010401").Success);
    }

    [Theory]
    // The BMP folds the same code path already handled are unchanged.
    [InlineData("A", "a")]
    [InlineData("K", "k")]   // KELVIN SIGN folds to k
    [InlineData("ſ", "s")]   // LATIN SMALL LETTER LONG S folds to s
    public void BmpFoldingIsUnchanged(string pattern, string equivalent)
    {
        var re = new BroilerRegex(pattern, RegexFlags.IgnoreCase | RegexFlags.Unicode);
        Assert.True(re.Match(equivalent).Success);
    }
}
