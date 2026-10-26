using System;
using System.Text;
using Broiler.Regex.Parsing;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The caller's nesting limit (requirement R4 as decision D6 of Broiler.VM's JSD-0081 reads it; conventions 3 and 5):
/// the parser checks it before each descent and answers <see cref="RegexStatus.LimitExceeded"/> naming
/// <see cref="RegexLimit.Nesting"/> past it, counting depth as Broiler.VM's matcher counts it (decision D7): the
/// pattern's own Disjunction is depth 1, each group of any kind adds 1, and under <c>v</c> each class adds 1, the
/// outermost included.
/// </summary>
public class NestingLimitTests
{
    private static RegexStatus Compile(string pattern, RegexFlags flags, int limit, out BroilerRegex? regex, out RegexLimit exceeded)
        => BroilerRegex.Compile(pattern, flags, UnicodeTables.Instance, limit, out regex, out exceeded);

    /// <summary>
    /// A pattern of <paramref name="levels"/> nested levels of one kind around <c>a</c>, at depth
    /// <paramref name="levels"/> + 1, with the flags and a subject it matches.
    /// </summary>
    internal static (string Pattern, RegexFlags Flags, string Subject) Nested(string kind, int levels)
    {
        var open = new StringBuilder();
        var close = new StringBuilder();
        for (var i = 0; i < levels; i++)
        {
            open.Append(kind switch
            {
                "named" => "(?<n" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + ">",
                "class" => "[",
                _ => kind,
            });
            close.Append(kind == "class" ? ']' : ')');
        }

        // A look-behind looks back at an a the pattern has read; a negative look-around's answer alternates with
        // the depth, so its subject is the one its parity matches.
        var flags = kind == "class" ? RegexFlags.UnicodeSets : RegexFlags.None;
        var subject = kind is "(?!" or "(?<!" && levels % 2 == 1 ? "b" : "a";
        var prefix = kind is "(?<=" or "(?<!" && levels > 0 ? subject : "";
        return (prefix + open + "a" + close, flags, subject);
    }

    public static TheoryData<string> Kinds => new() { "(", "(?:", "named", "(?=", "(?!", "(?<=", "(?<!", "(?i:", "(?-i:", "(?m-s:", "class" };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void A_limit_admits_its_own_depth_and_refuses_one_level_more(string kind)
    {
        foreach (var limit in (int[])[1, 2, 8, 512])
        {
            var (atLimit, flags, subject) = Nested(kind, limit - 1);
            Assert.Equal(RegexStatus.Ok, Compile(atLimit, flags, limit, out var regex, out var exceeded));
            Assert.Equal(RegexLimit.None, exceeded);
            Assert.True(regex!.IsMatch(subject));

            var (onePast, _, _) = Nested(kind, limit);
            Assert.Equal(RegexStatus.LimitExceeded, Compile(onePast, flags, limit, out regex, out exceeded));
            Assert.Equal(RegexLimit.Nesting, exceeded);
            Assert.Null(regex);
        }
    }

    [Fact]
    public void A_class_adds_a_level_under_v_only()
    {
        // Under v the outermost class is depth 2 and each nested class one more.
        Assert.Equal(RegexStatus.Ok, Compile("[a]", RegexFlags.UnicodeSets, 2, out _, out _));
        Assert.Equal(RegexStatus.LimitExceeded, Compile("[a]", RegexFlags.UnicodeSets, 1, out _, out _));
        Assert.Equal(RegexStatus.LimitExceeded, Compile("[[a]]", RegexFlags.UnicodeSets, 2, out _, out _));
        Assert.Equal(RegexStatus.Ok, Compile("[a&&[a--[b]]]", RegexFlags.UnicodeSets, 4, out _, out _));
        Assert.Equal(RegexStatus.LimitExceeded, Compile("[a&&[a--[b]]]", RegexFlags.UnicodeSets, 3, out _, out _));
        Assert.Equal(RegexStatus.Ok, Compile("[^[^[^a]]]", RegexFlags.UnicodeSets, 4, out _, out _));

        // Without v a class does not nest, and adds nothing.
        Assert.Equal(RegexStatus.Ok, Compile("[a]", RegexFlags.Unicode, 1, out _, out _));
        Assert.Equal(RegexStatus.Ok, Compile("[[a]]", RegexFlags.None, 1, out _, out _));
    }

    [Fact]
    public void Every_kind_of_level_counts_toward_one_depth()
    {
        // The pattern, a capturing group, a non-capturing group, a look-ahead, a modifier and a v class: depth 6.
        const string pattern = "((?:(?=(?i:[a]))))";
        Assert.Equal(RegexStatus.Ok, Compile(pattern, RegexFlags.UnicodeSets, 6, out var regex, out _));
        Assert.True(regex!.IsMatch("A"));
        Assert.Equal(RegexStatus.LimitExceeded, Compile(pattern, RegexFlags.UnicodeSets, 5, out _, out var exceeded));
        Assert.Equal(RegexLimit.Nesting, exceeded);
    }

    [Fact]
    public void Quantifiers_sequences_and_alternatives_add_no_level()
    {
        Assert.Equal(RegexStatus.Ok, Compile("(?:a|b)*(?:c)+d|e{2}|(?=f)*", RegexFlags.None, 2, out var regex, out _));
        Assert.True(regex!.IsMatch("abcd"));
        Assert.Equal(RegexStatus.Ok, Compile("a*b+c?(?:)", RegexFlags.None, 2, out _, out _));
        Assert.Equal(RegexStatus.LimitExceeded, Compile("((a)*)+", RegexFlags.None, 2, out _, out _));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void A_pattern_far_past_the_limit_is_refused_without_descending(string kind)
    {
        // 1,000,000 levels: a parser that recursed past the limit would end the process with a stack overflow.
        var (pattern, flags, _) = Nested(kind, 1_000_000);
        Assert.Equal(RegexStatus.LimitExceeded, Compile(pattern, flags, 512, out var regex, out var exceeded));
        Assert.Equal(RegexLimit.Nesting, exceeded);
        Assert.Null(regex);
    }

    [Fact]
    public void The_limit_lies_between_1_and_the_documented_maximum()
    {
        Assert.Equal(512, BroilerRegex.MaximumNestingLimit);
        Assert.Throws<ArgumentOutOfRangeException>(() => Compile("a", RegexFlags.None, 0, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => Compile("a", RegexFlags.None, -1, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => Compile("a", RegexFlags.None, BroilerRegex.MaximumNestingLimit + 1, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => Compile("a", RegexFlags.None, int.MaxValue, out _, out _));
        Assert.Equal(RegexStatus.Ok, Compile("a", RegexFlags.None, BroilerRegex.MaximumNestingLimit, out _, out _));
        Assert.Equal(RegexStatus.Ok, Compile("a", RegexFlags.None, 1, out _, out _));
        Assert.Throws<ArgumentNullException>(() => BroilerRegex.Compile(null!, RegexFlags.None, UnicodeTables.Instance, 1, out _, out _));
        Assert.Throws<ArgumentNullException>(() => BroilerRegex.Compile("a", RegexFlags.None, null!, 1, out _, out _));
    }

    [Fact]
    public void The_parser_answers_what_it_reads_first()
    {
        // A syntax error before the level past the limit is reported; a level past the limit before a syntax error is.
        var (deep, _, _) = Nested("(?:", 600);
        Assert.Throws<RegexSyntaxException>(() => Compile("a**" + deep, RegexFlags.None, 512, out _, out _));
        Assert.Equal(RegexStatus.LimitExceeded, Compile(deep + "a**", RegexFlags.None, 512, out _, out _));
        Assert.Equal(RegexStatus.LimitExceeded, Compile(new string('(', 600), RegexFlags.None, 512, out _, out _));
        Assert.Throws<RegexSyntaxException>(() => Compile(new string('(', 100), RegexFlags.None, 512, out _, out _));
    }

    [Fact]
    public void The_members_that_take_no_limit_parse_under_the_maximum_and_refuse_past_it_as_a_syntax_error()
    {
        var max = BroilerRegex.MaximumNestingLimit;
        var (atMaximum, _, subject) = Nested("(", max - 1);
        Assert.True(TestRegex.Compile(atMaximum).IsMatch(subject));
        RegexParser.Parse(atMaximum, RegexFlags.None, UnicodeTables.Instance, out var captures);
        Assert.Equal(max - 1, captures);

        // 0.1.0-preview.4 parsed any depth its stack held and ended the process past it.
        var (onePast, _, _) = Nested("(", max);
        var refused = Assert.Throws<RegexSyntaxException>(() => TestRegex.Compile(onePast));
        Assert.Equal(max - 1, refused.Position);
        Assert.Equal(max - 1, Assert.Throws<RegexSyntaxException>(() => RegexParser.Parse(onePast, RegexFlags.None, UnicodeTables.Instance, out _)).Position);

        var (farPast, flags, _) = Nested("class", 1_000_000);
        Assert.Throws<RegexSyntaxException>(() => TestRegex.Compile(farPast, flags));
    }
}
