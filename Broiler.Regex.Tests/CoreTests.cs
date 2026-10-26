using System.Collections.Generic;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>Baseline coverage of the common ECMAScript regex grammar.</summary>
public class CoreTests
{
    [Fact(Timeout = 600000)]
    public void Literal_FindsFirstOccurrence()
    {
        var m = TestRegex.Compile("cat").Match("a cat sat");
        Assert.True(m.Success);
        Assert.Equal(2, m.Index);
        Assert.Equal("cat", m.Value);
    }

    [Fact(Timeout = 600000)]
    public void Alternation()
    {
        Assert.Equal("b", TestRegex.Compile("a|b").Match("zb").Value);
    }

    [Fact(Timeout = 600000)]
    public void CharacterClass_RangeAndNegation()
    {
        Assert.Equal("abc", TestRegex.Compile("[a-c]+").Match("xabcy").Value);
        Assert.Equal("x", TestRegex.Compile("[^a-c]").Match("abcx").Value);
    }

    [Fact(Timeout = 600000)]
    public void Quantifiers_Greedy_Lazy_Bounds()
    {
        Assert.Equal("123", TestRegex.Compile("\\d{2,3}").Match("12345").Value);
        Assert.Equal("aaa", TestRegex.Compile("a+").Match("aaab").Value);
        Assert.Equal("a", TestRegex.Compile("a+?").Match("aaab").Value);
        Assert.Equal("color", TestRegex.Compile("colou?r").Match("color").Value);
    }

    [Fact(Timeout = 600000)]
    public void Anchors()
    {
        Assert.True(TestRegex.Compile("^abc$").Match("abc").Success);
        Assert.False(TestRegex.Compile("^abc$").Match("xabc").Success);
    }

    [Fact(Timeout = 600000)]
    public void Multiline_AnchorsMatchAtLineBreaks()
    {
        var m = TestRegex.Compile("^b", "m").Match("a\nb");
        Assert.True(m.Success);
        Assert.Equal(2, m.Index);
    }

    [Fact(Timeout = 600000)]
    public void DotAll_DotMatchesNewline()
    {
        Assert.True(TestRegex.Compile("a.b", "s").Match("a\nb").Success);
        Assert.False(TestRegex.Compile("a.b").Match("a\nb").Success);
    }

    [Fact(Timeout = 600000)]
    public void WordBoundary()
    {
        var m = TestRegex.Compile("\\bword\\b").Match("a word here");
        Assert.True(m.Success);
        Assert.Equal(2, m.Index);
        Assert.False(TestRegex.Compile("\\bword\\b").Match("password").Success);
    }

    [Fact(Timeout = 600000)]
    public void NamedGroups_ExposedByName()
    {
        var re = TestRegex.Compile("(?<year>\\d{4})-(?<month>\\d{2})");
        var m = re.Match("2026-06");
        Assert.True(m.Success);
        Assert.Equal("2026", m.NamedGroups["year"].Value);
        Assert.Equal("06", m.NamedGroups["month"].Value);
        Assert.Equal(2, re.CaptureCount);
    }

    [Fact(Timeout = 600000)]
    public void Lookahead_PositiveAndNegative()
    {
        Assert.True(TestRegex.Compile("a(?=b)").Match("ab").Success);
        Assert.False(TestRegex.Compile("a(?=b)").Match("ac").Success);
        Assert.True(TestRegex.Compile("a(?!b)").Match("ac").Success);
    }

    [Fact(Timeout = 600000)]
    public void Sticky_AnchorsAtStart()
    {
        Assert.False(TestRegex.Compile("a", "y").Match("ba").Success);
        Assert.True(TestRegex.Compile("a", "y").Match("ab").Success);
    }

    [Fact(Timeout = 600000)]
    public void IgnoreCase_Ascii()
    {
        Assert.True(TestRegex.Compile("abc", "i").Match("ABC").Success);
    }

    [Fact(Timeout = 600000)]
    public void Global_EnumeratesAllMatches()
    {
        var matches = new List<string>();
        foreach (var m in TestRegex.Compile("a").Matches("aba"))
            matches.Add(m.Value);
        Assert.Equal(new[] { "a", "a" }, matches);
    }

    [Fact(Timeout = 600000)]
    public void EmptyPattern_MatchesEmpty()
    {
        var m = TestRegex.Compile("").Match("abc");
        Assert.True(m.Success);
        Assert.Equal(0, m.Length);
    }

    [Fact(Timeout = 600000)]
    public void InlineModifier_IgnoreCaseRegion()
    {
        // (?i:b) is case-insensitive; the surrounding 'a' is not.
        var re = TestRegex.Compile("a(?i:b)c");
        Assert.True(re.Match("aBc").Success);
        Assert.False(re.Match("Abc").Success);
    }
}
