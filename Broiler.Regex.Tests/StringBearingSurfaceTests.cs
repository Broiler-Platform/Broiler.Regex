using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The string-bearing result decision D12 keeps, in the shape task 2.9 of SJL-11.2 gives it so that no public signature
/// names a base class library collection (convention 1, check C1): <see cref="RegexMatch.Groups"/> a span of the groups,
/// <see cref="RegexMatch.GroupNamed"/> a name's group where <c>NamedGroups</c> was a dictionary, and
/// <see cref="BroilerRegex.NextMatch"/> the global iteration where <c>BroilerRegex.Matches</c> was an enumerable.
/// </summary>
public class StringBearingSurfaceTests
{
    [Fact]
    public void Groups_is_a_span_of_the_groups_in_number_order()
    {
        var match = TestRegex.Compile("(a)(?<n>b)?(c)").Match("xac");
        Assert.Equal(4, match.Groups.Length);
        var numbers = new List<int>();
        foreach (var group in match.Groups)
            numbers.Add(group.Number);
        Assert.Equal([0, 1, 2, 3], numbers);
        Assert.Equal(("ac", "a", false, "c"), (match.Groups[0].Value, match.Groups[1].Value, match.Groups[2].Success, match.Groups[3].Value));
        Assert.True(RegexMatch.Empty.Groups.IsEmpty);
    }

    [Fact]
    public void GroupNamed_answers_the_participating_group_of_a_name_its_first_unmatched_or_none()
    {
        // ES2025's duplicate named groups (R6): at most one group of a name participates, and the name answers it.
        var regex = TestRegex.Compile("(?:(?<n>a)|(?<n>b))(?<m>c)?");
        var b = regex.Match("b");
        Assert.Same(b.Groups[2], b.GroupNamed("n"));
        Assert.Equal((2, true, "b"), (b.GroupNamed("n")!.Number, b.GroupNamed("n")!.Success, b.GroupNamed("n")!.Value));

        // A name no group of which participated answers its first group, unmatched: the groups object's undefined.
        Assert.Same(b.Groups[3], b.GroupNamed("m"));
        Assert.False(b.GroupNamed("m")!.Success);

        // A name the pattern does not declare, and every name of a failed match, answer none.
        Assert.Null(b.GroupNamed("x"));
        Assert.Null(b.GroupNamed(""));
        Assert.Null(regex.Match("z").GroupNamed("n"));
        Assert.Throws<ArgumentNullException>(() => b.GroupNamed(null!));
    }

    /// <summary>The iteration through <see cref="BroilerRegex.NextMatch"/> as (index, value) pairs, up to a bound.</summary>
    private static List<(int Index, string Value, RegexLimit Limit)> Walk(BroilerRegex regex, string input)
    {
        var walked = new List<(int, string, RegexLimit)>();
        for (var match = regex.Match(input, 0); walked.Count < 100; match = regex.NextMatch(match))
        {
            if (match.Success || match.Limit != RegexLimit.None)
                walked.Add((match.Index, match.Value, match.Limit));
            if (!match.Success)
                break;
        }

        return walked;
    }

    [Fact]
    public void NextMatch_walks_the_global_iteration_and_a_failed_match_answers_itself()
    {
        Assert.Equal([(0, "a", RegexLimit.None), (2, "a", RegexLimit.None), (3, "a", RegexLimit.None)], Walk(TestRegex.Compile("a", "g"), "abaa"));

        // After an empty match the next starts one code unit on, or past a surrogate pair whole under u and v
        // (AdvanceStringIndex, §22.2.7.3).
        Assert.Equal([0, 1, 2, 3], Walk(TestRegex.Compile("", "g"), "😀a").Select(static m => m.Index));
        Assert.Equal([0, 2, 3], Walk(TestRegex.Compile("", "gu"), "😀a").Select(static m => m.Index));
        Assert.Equal([0, 2, 3], Walk(TestRegex.Compile("", "gv"), "😀a").Select(static m => m.Index));

        var x = TestRegex.Compile("x");
        var none = x.Match("abc");
        Assert.Same(none, x.NextMatch(none));
        var c = TestRegex.Compile("c");
        Assert.Same(RegexMatch.Empty, c.NextMatch(c.Match("abc")));
        Assert.Throws<ArgumentNullException>(() => c.NextMatch(null!));
    }

    [Fact]
    public void NextMatch_ends_with_the_failed_match_that_names_a_ceiling()
    {
        // A greedy loop over a body of two characters, 1,100,000 times, passes one of the engines' ceilings (CeilingTests),
        // which the string-bearing match from 1 answers; the walk ends with that failed match after the one before it.
        var regex = TestRegex.Compile("b|(?:ab)*", "g");
        var subject = "b" + string.Concat(Enumerable.Repeat("ab", 1_100_000));
        var stopped = regex.Match(subject, 1);
        Assert.False(stopped.Success);
        Assert.NotEqual(RegexLimit.None, stopped.Limit);

        var walked = Walk(regex, subject);
        Assert.Equal([(0, "b", RegexLimit.None), (-1, "", stopped.Limit)], walked);
        Assert.Same(stopped, regex.NextMatch(regex.Match(subject, 0)));
    }
}
