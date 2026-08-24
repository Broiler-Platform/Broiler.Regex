namespace Broiler.Regex.Tests;

/// <summary>
/// ECMA-262 §22.2.2.9.4 Canonicalize, now table-driven from the pinned Unicode Character
/// Database rather than from <c>char</c>/<c>Rune</c> casing plus five hand-written special
/// cases (Broiler.JS gaps roadmap, track 2 action 3).
/// </summary>
public class CanonicalizeTests
{
    private static string? Match(string pattern, string flags, string input)
    {
        var m = new BroilerRegex(pattern, flags).Match(input);
        return m.Success ? m.Value : null;
    }

    [Theory]
    // Unicode mode canonicalizes with CaseFolding.txt's common+simple mapping…
    [InlineData("ſ", "iu", "s", "s")]
    [InlineData("s", "iu", "ſ", "ſ")]
    [InlineData("K", "iu", "k", "k")]
    [InlineData("Å", "iu", "å", "å")]
    // …while the non-Unicode branch uppercases a single code unit and refuses a fold that
    // would carry a non-ASCII code unit into ASCII, so ſ and s stay distinct.
    [InlineData("ſ", "i", "s", null)]
    [InlineData("s", "i", "ſ", null)]
    [InlineData("K", "i", "k", null)]
    [InlineData("a", "i", "A", "A")]
    [InlineData("Σ", "i", "σ", "σ")]
    public void Canonicalize_DiffersBetweenModes(string pattern, string flags, string input, string? expected)
        => Assert.Equal(expected, Match(pattern, flags, input));

    [Theory]
    // İ (U+0130) and ı (U+0131) have only full and Turkic-conditional foldings, so their
    // simple case folding is the identity and neither is case-equivalent to an ASCII i.
    // Deriving the fold from ToLowerInvariant made both of them fold to `i`.
    [InlineData("İ", "iu", "i")]
    [InlineData("ı", "iu", "i")]
    [InlineData("İ", "iu", "I")]
    [InlineData("ı", "iu", "I")]
    [InlineData("[İ]", "iu", "i")]
    [InlineData("İ", "i", "i")]
    [InlineData("ı", "i", "I")]
    public void DottedAndDotlessI_AreNotEquivalentToAsciiI(string pattern, string flags, string input)
        => Assert.Null(Match(pattern, flags, input));

    [Theory]
    [InlineData("İ", "iu", "İ")]
    [InlineData("ı", "iu", "ı")]
    public void DottedAndDotlessI_StillMatchThemselves(string pattern, string flags, string input)
        => Assert.Equal(input, Match(pattern, flags, input));

    [Theory]
    // §22.2.2.9.2 WordCharacters widens \w with every code point that folds into the basic
    // set when both `i` and `u` are on — so ſ is a word character there, and \W, which is
    // the complement of that widened set, must reject it.
    [InlineData(@"\w", "iu", "ſ", "ſ")]
    [InlineData(@"\w", "iu", "K", "K")]
    [InlineData(@"\W", "iu", "ſ", null)]
    [InlineData(@"\W", "iu", "K", null)]
    [InlineData(@"\S", "iu", "ſ", "ſ")]
    // Without both flags the basic set applies unchanged.
    [InlineData(@"\W", "u", "ſ", "ſ")]
    [InlineData(@"\w", "u", "ſ", null)]
    [InlineData(@"\W", "i", "ſ", "ſ")]
    public void WordCharacters_WidenUnderIgnoreCaseAndUnicode(string pattern, string flags, string input, string? expected)
        => Assert.Equal(expected, Match(pattern, flags, input));

    [Fact(Timeout = 600000)]
    public void WordBoundary_UsesTheSameWidenedSet()
    {
        // \b reads WordCharacters directly, so it has to agree with \w about ſ.
        Assert.Equal("ſ", Match(@"\bſ\b", "iu", "ſ"));
        Assert.Null(Match(@"\Bſ", "iu", "ſ"));
    }

    [Theory]
    // Supplementary-plane bicameral scripts fold through the table, including ones .NET's
    // own casing has no mapping for.
    [InlineData("\U00010400", "\U00010428")] // Deseret
    [InlineData("\U000118A0", "\U000118C0")] // Warang Citi
    [InlineData("\U00010D50", "\U00010D70")] // Garay (Unicode 16.0)
    [InlineData("\U0001E900", "\U0001E922")] // Adlam
    [InlineData("\U00010570", "\U00010597")] // Vithkuqi
    public void AstralScripts_FoldBothDirections(string upper, string lower)
    {
        Assert.Equal(lower, Match(upper, "iu", lower));
        Assert.Equal(upper, Match(lower, "iu", upper));
        Assert.Equal(lower, Match("[" + upper + "]", "iu", lower));
    }

    [Fact(Timeout = 600000)]
    public void AstralScripts_DoNotFoldAcrossUnrelatedCodePoints()
    {
        Assert.Null(Match("\U00010400", "iu", "\U000118C0"));
        Assert.Null(Match("\U00010400", "iu", "\U00010401"));
        Assert.Null(Match("\U00010400", "u", "\U00010428"));
    }

    [Theory]
    // Cherokee is the case where the uppercase and the folding directions disagree: the
    // small letters were added later, so scf maps them UP to the capitals.
    [InlineData("Ꭰ", "ꭰ")]
    [InlineData("ꭰ", "Ꭰ")]
    public void CherokeeFoldsTowardsTheCapitals(string pattern, string input)
        => Assert.Equal(input, Match(pattern, "iu", input));

    [Fact(Timeout = 600000)]
    public void FoldEquivalenceIsSymmetricAcrossAnOrbit()
    {
        // k, K and the Kelvin sign are one orbit, so a class holding any of them matches
        // all three under `iu`.
        foreach (var member in new[] { "k", "K", "K" })
        {
            Assert.Equal(member, Match("[k]", "iu", member));
            Assert.Equal(member, Match("[K]", "iu", member));
            Assert.Null(Match("[^k]", "iu", member));
        }
    }
}
