using Broiler.Regex.Parsing;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Requirement R5's named references (bundle sjl-11-001's check A-5; row S5 of Broiler.VM's correspondence): an
/// <c>AtomEscape :: k GroupName</c> naming no GroupSpecifier of the pattern is an early error (ECMA-262 §22.2.1.1),
/// which the parser decides, so a parse alone refuses it as a compilation does; and where the pattern is read with
/// <c>[+NamedCaptureGroups]</c> (under <c>u</c> or <c>v</c>, or when it declares a group name, Annex B §B.1.2),
/// <c>\k</c> is no identity escape, in a class either.
/// </summary>
public class NamedReferenceTests
{
    private static int ParseRefusal(string pattern, string flags)
        => Assert.Throws<RegexSyntaxException>(() => RegexParser.Parse(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance, out _)).Position;

    [Theory]
    [InlineData(@"\k<b>(?<a>x)", "", 0)]
    [InlineData(@"\k<b>(?<a>x)", "u", 0)]
    [InlineData(@"\k<b>(?<a>x)", "v", 0)]
    [InlineData(@"(?<a>x)\k<b>", "", 7)]
    [InlineData(@"(?<a>x)\k<b>", "u", 7)]
    [InlineData(@"\k<a>", "u", 0)]
    [InlineData(@"\k<a>", "v", 0)]
    [InlineData(@"(?<a>x)|\k<A>", "", 8)]
    [InlineData(@"(?<ab>x)\k<a>", "i", 8)]
    public void A_reference_to_an_undeclared_name_is_refused_by_the_parse_alone(string pattern, string flags, int position)
    {
        Assert.Equal(position, ParseRefusal(pattern, flags));
        Assert.Equal(position, Assert.Throws<RegexSyntaxException>(() => TestRegex.Compile(pattern, flags)).Position);
    }

    [Theory]
    [InlineData(@"\k<a>(?<a>x)", "", "x", "x")]
    [InlineData(@"\k<a>(?<a>x)", "u", "x", "x")]
    [InlineData(@"(?<a>x)\k<a>", "v", "xx", "xx")]
    [InlineData(@"(?:\k<a>(?<a>y))+", "", "yyy", "yyy")]
    public void A_reference_to_a_declared_name_is_accepted_before_or_after_its_group(string pattern, string flags, string subject, string expected)
        => Assert.Equal(expected, TestRegex.Compile(pattern, flags).Match(subject).Value);

    /// <summary>Without a GroupName and without <c>u</c> and <c>v</c>, Annex B reads <c>\k</c> as the letter.</summary>
    [Theory]
    [InlineData(@"\k<a>", "k<a>")]
    [InlineData(@"\k", "k")]
    [InlineData(@"[\k<a>]+", "k<a>")]
    public void Without_a_group_name_k_is_the_letter(string pattern, string expected)
        => Assert.Equal(expected, TestRegex.Compile(pattern).Match(expected).Value);

    [Theory]
    [InlineData(@"(?<a>.)\k", "")]
    [InlineData(@"(?<a>.)\k<a", "")]
    [InlineData(@"(?<a>.)\k<>", "")]
    [InlineData(@"(?<a>.)\k<1>", "")]
    [InlineData(@"[\k<a>](?<a>x)", "")]
    [InlineData(@"(?<a>x)[\k]", "")]
    [InlineData(@"[\k<a>](?<a>x)", "u")]
    [InlineData(@"[\k<a>]", "u")]
    [InlineData(@"\k", "u")]
    public void Where_names_are_read_k_is_only_a_reference(string pattern, string flags)
        => Assert.Throws<RegexSyntaxException>(() => TestRegex.Compile(pattern, flags));
}
