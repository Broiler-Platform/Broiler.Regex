using System;
using System.Linq;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Requirement R5's group names (bundle sjl-11-001's check A-5): a GroupName is a <c>RegExpIdentifierName</c>
/// (ECMA-262 §22.2.1), read the same way in every mode, its code points held to IdentifierStartChar and
/// IdentifierPartChar by the provider's <c>EcmaScriptLexical</c> (§22.2.1.1); and two GroupSpecifiers that might both
/// participate may not share a name, compared as decoded (requirement R6's rule is <see cref="DuplicateNamedGroupTests"/>'s).
/// Also row S10 of Broiler.VM's correspondence: only two four-digit escapes form a
/// surrogate pair, never a braced one.
/// </summary>
public class GroupNameTests
{
    /// <summary>Every mode a name is read in: without <c>u</c> and <c>v</c>, under each, and with other flags beside them.</summary>
    public static readonly TheoryData<string> Modes = new() { "", "u", "v", "i", "iu", "iv", "ms" };

    /// <summary>Accepted names: each spelling, and the name it decodes to.</summary>
    public static readonly TheoryData<string, string> Accepted = new()
    {
        { "a", "a" },
        { "$", "$" },
        { "_", "_" },
        { "\u00E9", "\u00E9" },                   // LATIN SMALL LETTER E WITH ACUTE, raw
        { "a1", "a1" },
        { "a\u200Cb", "a\u200Cb" },               // ZWNJ, an IdentifierPartChar
        { "a\u200Db", "a\u200Db" },               // ZWJ
        { "ab$_9", "ab$_9" },
        { @"\u0041", "A" },
        { @"\u{41}", "A" },
        { @"a\u{1D7DA}", "a\U0001D7DA" },         // MATHEMATICAL DOUBLE-STRUCK DIGIT TWO, ID_Continue
        { @"\uD835\uDC00", "\U0001D400" },     // an escaped surrogate pair
        { @"\u{1D400}", "\U0001D400" },           // MATHEMATICAL BOLD CAPITAL A
        { "\U0001D400", "\U0001D400" },           // the raw pair
        { "a\U0001D400", "a\U0001D400" },
        { @"\u0061b\u{63}", "abc" },
    };

    /// <summary>Refused names: a code point no identifier may start or hold there, a lone surrogate, a malformed escape.</summary>
    public static readonly TheoryData<string> Refused = new()
    {
        "1",
        @"\u0031",
        "a b",
        @"a\u0020",
        "a-b",
        "a.b",
        "\u200Ca",                                   // ZWNJ may not start one
        @"\uD800",
        @"\u{D800}",
        @"\uD835",
        @"\uDC00",
        @"\uD835\u{DC00}",                        // a braced escape never joins a pair
        "\uD835x",                                   // a raw lone lead surrogate
        @"\x41",
        @"\u{110000}",
        @"\u004",
        @"\u{}",
        "a\\",
        "a\U0001F98A",                               // FOX FACE, not ID_Continue
        "",
    };

    [Theory]
    [MemberData(nameof(Modes))]
    public void Every_accepted_name_reads_the_same_in_every_mode(string flags)
    {
        foreach (var row in Accepted)
        {
            var spelling = row.Data.Item1;
            var name = row.Data.Item2;
            var regex = TestRegex.Compile("(?<" + spelling + ">x)", flags);
            Assert.Equal([1], regex.GroupNames.GroupsNamed(name).ToArray());
            Assert.Equal((1, name, name), (regex.GroupNames.Count, regex.GroupNames.NameAt(0), regex.GroupNames.NameOfGroup(1)));

            var match = regex.Match("x");
            Assert.True(match.Success);
            Assert.Equal("x", match.NamedGroups[name].Value);
        }
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void Every_refused_name_is_a_syntax_error_in_every_mode(string flags)
    {
        foreach (var row in Refused)
        {
            var spelling = row.Data;
            var exception = Record.Exception(() => TestRegex.Compile("(?<" + spelling + ">x)", flags));
            Assert.True(exception is RegexSyntaxException, spelling + " under /" + flags + ": " + (exception?.GetType().Name ?? "accepted"));
        }
    }

    /// <summary>
    /// The survey's case: an escaped astral pair without <c>u</c> threw <see cref="ArgumentOutOfRangeException"/>
    /// in 0.1.0-preview.4 (<c>RegexParser.ReadGroupName</c>), and a lone surrogate escape did under <c>u</c>.
    /// </summary>
    [Theory]
    [InlineData(@"(?<\uD835\uDC00>x)", "", true)]
    [InlineData(@"(?<\uD835\uDC00>x)", "u", true)]
    [InlineData(@"(?<\u{1D400}>x)", "", true)]
    [InlineData(@"(?<\u{1D400}>x)", "u", true)]
    [InlineData(@"(?<\uD800>x)", "", false)]
    [InlineData(@"(?<\uD800>x)", "u", false)]
    [InlineData(@"(?<\u{D800}>x)", "u", false)]
    [InlineData(@"(?<\u{D800}>x)", "", false)]
    public void An_escaped_surrogate_in_a_name_is_read_or_refused_never_thrown(string pattern, string flags, bool accepted)
    {
        var exception = Record.Exception(() => TestRegex.Compile(pattern, flags));
        if (accepted)
            Assert.Null(exception);
        else
            Assert.IsType<RegexSyntaxException>(exception);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void A_duplicate_name_is_refused_after_its_escapes_are_decoded(string flags)
    {
        foreach (var pattern in (string[])[
            "(?<a>x)(?<a>y)",
            @"(?<\u0061>x)(?<a>y)",
            @"(?<a>x)(?<\u{61}>y)",
            @"(?<\u0061>x)(?<\u{61}>y)",
            "(?<\U0001D400>x)(?<\\u{1D400}>y)",
            "(?<\\uD835\\uDC00>x)(?<\U0001D400>y)",
            @"(?<ab>x)(?<a\u0062>y)",
            @"(?<a>x)((?<\u0061>y))"])
        {
            var refusal = Assert.Throws<RegexSyntaxException>(() => TestRegex.Compile(pattern, flags));
            Assert.Equal(pattern.LastIndexOf("(?<", StringComparison.Ordinal), refusal.Position);
        }

        // Names that differ once decoded are two names.
        Assert.Equal(2, TestRegex.Compile(@"(?<a>x)(?<\u0062>y)", flags).GroupNames.Count);
    }

    /// <summary>
    /// The duplicate in two alternatives that cannot both participate, which ES2025 admits (§22.2.1.1, MightBothParticipate
    /// §22.2.1.4) and requirement R6 brought with task 2.6, which deleted this class's test that it was refused until then.
    /// An escape no longer hides a duplicate either way: 0.1.0-preview.4 accepted the survey's
    /// <c>(?&lt;\u0061&gt;x)|(?&lt;a&gt;y)</c> only because it compared the names as written, and refused the unescaped
    /// <c>(?&lt;a&gt;x)|(?&lt;a&gt;y)</c>; both are now one name of two groups. <see cref="DuplicateNamedGroupTests"/> holds
    /// the rule.
    /// </summary>
    [Theory]
    [InlineData("(?<a>x)|(?<a>y)", "")]
    [InlineData("(?<a>x)|(?<a>y)", "u")]
    [InlineData(@"(?<\u0061>x)|(?<a>y)", "")]
    [InlineData(@"(?:(?<a>x)|(?<a>y))\k<a>", "")]
    public void A_duplicate_across_alternatives_is_one_name_of_two_groups(string pattern, string flags)
        => Assert.Equal([1, 2], TestRegex.Compile(pattern, flags).GroupNames.GroupsNamed("a").ToArray());

    [Theory]
    [MemberData(nameof(Modes))]
    public void A_reference_names_the_group_as_decoded(string flags)
    {
        Assert.Equal("aa", TestRegex.Compile(@"(?<a>a)\k<\u0061>", flags).Match("aa").Value);
        Assert.Equal("aa", TestRegex.Compile(@"(?<\u0061>a)\k<a>", flags).Match("aa").Value);
        Assert.Equal("xx", TestRegex.Compile(@"(?<\u{1D400}>x)\k<" + "\U0001D400" + ">", flags).Match("xx").Value);
        Assert.Equal("xx", TestRegex.Compile(@"(?<A>x)\k<\u0041>", flags).Match("xx").Value);
    }

    /// <summary>
    /// Row S10 of Broiler.VM's correspondence: only <c>u HexLeadSurrogate \u HexTrailSurrogate</c> forms a pair, so
    /// a four-digit lead escape before a braced escape is a lone lead surrogate and a second character, and a braced
    /// escape never joins one. 0.1.0-preview.4 refused the first patterns ("Invalid hexadecimal escape").
    /// </summary>
    /// <remarks>
    /// The cases are a table in the method, not theory data: a lone surrogate does not survive xunit's
    /// serialization of a theory's arguments.
    /// </remarks>
    [Fact]
    public void A_braced_escape_never_joins_a_surrogate_pair()
    {
        foreach (var (pattern, flags, subject, expected) in ((string, string, string, string?)[])[
            (@"\uD83D\u{DC38}+", "u", "\uD83D\uDC38\uDC38", null),
            (@"\uD83D\u{DC38}+", "v", "\uD83D\uDC38\uDC38", null),
            (@"\u{D83D}\u{DC38}+", "u", "\uD83D\uDC38\uDC38", null),
            (@"\u{D83D}\uDC38+", "u", "\uD83D\uDC38\uDC38", null),
            (@"\uD83D\uDC38+", "u", "\uD83D\uDC38\uDC38", "\uD83D\uDC38"),
            (@"\uD83D\u{3042}*", "u", "\uD83D", "\uD83D"),
            (@"\uD83D\u{3042}*", "v", "\uD83D", "\uD83D"),
            (@"\uD83D\u{41}", "u", "\uD83DA", "\uD83DA"),
            (@"[\uD83D\u{3042}]*", "u", "\uD83D", "\uD83D"),
            (@"[\uD83D\u{DC38}]", "u", "\uDC38", "\uDC38"),
            (@"[\uD83D\u{DC38}]", "u", "\uD83D\uDC38", null),
            (@"[\uD83D\u{DC38}]", "v", "\uDC38", "\uDC38")])
        {
            var match = TestRegex.Compile(pattern, flags).Match(subject);
            if (expected is null)
                Assert.False(match.Success, pattern + " /" + flags);
            else
                Assert.Equal(expected, match.Value);
        }
    }

    /// <summary>The survey's and the critic's group-name probe cases (the plan's section 1), with the specification's answers.</summary>
    [Theory]
    [InlineData("(?<a>x)", "", true)]
    [InlineData("(?<1>x)", "", false)]
    [InlineData("(?<a b>x)", "", false)]
    [InlineData("(?<a-b>x)", "", false)]
    [InlineData("(?<$>x)", "", true)]
    [InlineData("(?<_>x)", "", true)]
    [InlineData("(?<\u00E9>x)", "", true)]
    [InlineData("(?<a.b>x)", "", false)]
    [InlineData("(?<a\u200Cb>x)", "", true)]
    [InlineData(@"(?<\u0041>x)", "", true)]
    [InlineData(@"(?<\u0041>x)", "u", true)]
    [InlineData(@"(?<\u{41}>x)", "", true)]
    [InlineData(@"(?<\u{41}>x)", "u", true)]
    [InlineData("(?<\U0001D400>x)", "", true)]
    [InlineData("(?<\U0001D400>x)", "u", true)]
    [InlineData(@"(?<a\u0020>x)", "", false)]
    [InlineData(@"(?<\u0031>x)", "", false)]
    [InlineData(@"(?<\u0061>x)(?<a>y)", "", false)]
    [InlineData(@"(?<\u0061>x)(?<a>y)", "u", false)]
    public void The_probe_cases_get_the_specification_answer(string pattern, string flags, bool accepted)
    {
        var exception = Record.Exception(() => TestRegex.Compile(pattern, flags));
        if (accepted)
            Assert.Null(exception);
        else
            Assert.IsType<RegexSyntaxException>(exception);
    }
}
