using System.Linq;
using System.Text;
using Broiler.Regex.Engine;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The new backend's loop (task 2.4a; decision D5 of Broiler.VM's JSD-0081) against ECMA-262's Pattern semantics
/// (§22.2.2): each instruction, each quantifier form, the empty-iteration rule, a quantified atom's captures cleared at
/// each iteration, the order of alternatives and of greedy and lazy choices, counted bounds at their edges, the search and
/// the anchored match, and a match at the nesting maximum and over a flat sequence of 100,000 terms on a 1 MiB thread. The
/// answers are the specification's; the examples marked "§22.2.2.3.1" are its notes' own.
/// </summary>
public class MachineTests
{
    /// <summary>
    /// The answer of the program of <paramref name="pattern"/> over <paramref name="subject"/>: "nomatch", or "match s,e"
    /// and each group's offsets, "-" for a group that did not participate, as the adversarial corpus writes them.
    /// </summary>
    internal static string Run(string pattern, string flags, string subject, int start = 0, bool anchored = false)
    {
        var program = TestProgram.Compile(pattern, flags);
        var slots = new int[program.SlotCount];
        var status = program.Match(subject, start, anchored, slots, null, Ample, out _, out _);
        return Render(status, slots);
    }

    /// <summary>Ceilings no test of the loop's semantics reaches: no step ceiling, and the library's maximum frames and trail.</summary>
    internal static RegexMatchCeilings Ample => new(null, RegexMatchCeilings.MaximumFrames, RegexMatchCeilings.MaximumTrail);

    internal static string Render(RegexStatus status, int[] slots)
    {
        if (status != RegexStatus.Match)
            return status == RegexStatus.NoMatch ? "nomatch" : status.ToString();

        var text = new StringBuilder("match ").Append(slots[0]).Append(',').Append(slots[1]);
        for (var group = 2; group < slots.Length; group += 2)
            text.Append(' ').Append(slots[group] < 0 ? "-" : slots[group] + "," + slots[group + 1]);
        return text.ToString();
    }

    [Theory]
    [InlineData("a", "", "bab", "match 1,2")]
    [InlineData("abc", "", "xxabcxx", "match 2,5")]
    [InlineData("abc", "", "ab", "nomatch")]
    [InlineData("", "", "abc", "match 0,0")]
    [InlineData("\\u{1F600}", "u", "x\U0001F600", "match 1,3")]
    [InlineData("\\uD83D", "", "\U0001F600", "match 0,1")]
    [InlineData("\\uD83D", "u", "\U0001F600", "nomatch")]
    [InlineData("\\uDE00", "", "\U0001F600", "match 1,2")]
    [InlineData("\\uDE00", "u", "\U0001F600", "nomatch")]
    [InlineData("\\uDE00", "v", "\U0001F600", "nomatch")]
    public void A_character_is_a_code_unit_and_under_u_and_v_a_code_point(string pattern, string flags, string subject, string expected)
    {
        Assert.Equal(expected, Run(pattern, flags, subject));
    }

    [Fact]
    public void Under_u_a_lone_surrogate_in_the_subject_is_a_character_of_its_own()
    {
        // Theory data would carry a lone surrogate through xunit's serialization, which replaces it, so the subjects are here.
        Assert.Equal("match 0,1", Run("\\uD83D", "u", "\uD83Dx"));
        Assert.Equal("match 1,2", Run("\\uDE00", "u", "x\uDE00"));
        Assert.Equal("match 0,1", Run(".", "u", "\uDE00\uD83D"));
        Assert.Equal("match 0,2", Run("..", "u", "\uDE00\uD83D"));
        Assert.Equal("match 0,1", Run("[^a]", "v", "\uD83D"));
    }

    [Theory]
    [InlineData(".", "", "\n", "nomatch")]
    [InlineData(".", "", "\r", "nomatch")]
    [InlineData(".", "", "\u2028", "nomatch")]
    [InlineData(".", "", "\u2029", "nomatch")]
    [InlineData(".", "", "\u0085", "match 0,1")]
    [InlineData(".", "s", "\n", "match 0,1")]
    [InlineData(".", "s", "\u2029", "match 0,1")]
    [InlineData(".", "", "\U0001F600", "match 0,1")]
    [InlineData(".", "u", "\U0001F600", "match 0,2")]
    [InlineData("..", "u", "\U0001F600", "nomatch")]
    [InlineData("..", "", "\U0001F600", "match 0,2")]
    [InlineData("a.c", "", "a\nc abc", "match 4,7")]
    public void A_dot_is_any_character_but_a_line_terminator_and_under_s_any_character(string pattern, string flags, string subject, string expected)
    {
        Assert.Equal(expected, Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("[a-c]+", "", "xabcy", "match 1,4")]
    [InlineData("[^a-c]", "", "abcx", "match 3,4")]
    [InlineData("\\d{2,3}", "", "12345", "match 0,3")]
    [InlineData("[^a]", "", "\U0001F600", "match 0,1")]
    [InlineData("[^a]", "u", "\U0001F600", "match 0,2")]
    [InlineData("[\\u{1F600}]", "u", "x\U0001F600", "match 1,3")]
    [InlineData("[]", "", "abc", "nomatch")]
    [InlineData("[^]", "", "\n", "match 0,1")]
    [InlineData("\\W+", "", "ab, cd", "match 2,4")]
    [InlineData("\\s", "", "a\u3000b", "match 1,2")]
    [InlineData("\\p{Lu}+", "u", "abCDe", "match 2,4")]
    [InlineData("[\\p{L}--[a-z]]+", "v", "abcDEF", "match 3,6")]
    [InlineData("[[a-z]&&[^aeiou]]+", "v", "aebcd", "match 2,5")]
    public void A_class_tests_its_code_points_with_its_negation_applied(string pattern, string flags, string subject, string expected)
    {
        Assert.Equal(expected, Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("a|ab", "", "ab", "match 0,1")]
    [InlineData("ab|a", "", "ab", "match 0,2")]
    [InlineData("x|y|z", "", "az", "match 1,2")]
    [InlineData("a|", "", "b", "match 0,0")]
    [InlineData("((a)|(ab))((c)|(bc))", "", "abc", "match 0,3 0,1 0,1 - 1,3 - 1,3")]
    [InlineData("(a|ab)(c|bcd)(d*)", "", "abcd", "match 0,4 0,1 1,4 4,4")]
    public void An_alternation_tries_its_alternatives_left_to_right(string pattern, string flags, string subject, string expected)
    {
        Assert.Equal(expected, Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("a[a-z]{2,4}", "", "abcdefghi", "match 0,5")]
    [InlineData("a[a-z]{2,4}?", "", "abcdefghi", "match 0,3")]
    [InlineData("(aa|aabaac|ba|b|c)*", "", "aabaac", "match 0,4 2,4")]
    [InlineData("(z)((a+)?(b+)?(c))*", "", "zaacbbbcac", "match 0,10 0,1 8,10 8,9 - 9,10")]
    public void The_specification_s_own_examples_of_RepeatMatcher_answer_as_its_notes_say(string pattern, string flags, string subject, string expected)
    {
        // ECMA-262 §22.2.2.3.1, the notes to RepeatMatcher.
        Assert.Equal(expected, Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("a*", "", "aaa", "match 0,3")]
    [InlineData("a*?", "", "aaa", "match 0,0")]
    [InlineData("a+", "", "aaa", "match 0,3")]
    [InlineData("a+?", "", "aaa", "match 0,1")]
    [InlineData("a?", "", "aaa", "match 0,1")]
    [InlineData("a??", "", "aaa", "match 0,0")]
    [InlineData("a??b", "", "ab", "match 0,2")]
    [InlineData("(a+?)(a*)", "", "aaa", "match 0,3 0,1 1,3")]
    [InlineData("(a*?)(a*)", "", "aaa", "match 0,3 0,0 0,3")]
    [InlineData("(a+)(a+)", "", "aaaa", "match 0,4 0,3 3,4")]
    [InlineData("(a+?)(a+)", "", "aaaa", "match 0,4 0,1 1,4")]
    [InlineData("(?:a|b)*?c", "", "abc", "match 0,3")]
    [InlineData("(a|ab)*c", "", "abc", "match 0,3 0,2")]
    public void A_greedy_quantifier_tries_another_iteration_first_and_a_lazy_one_the_continuation(string pattern, string flags, string subject, string expected)
    {
        Assert.Equal(expected, Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("a{3}", "", "aa", "nomatch")]
    [InlineData("a{3}", "", "aaa", "match 0,3")]
    [InlineData("a{3}", "", "aaaa", "match 0,3")]
    [InlineData("a{2,}", "", "a", "nomatch")]
    [InlineData("a{2,}", "", "aaaa", "match 0,4")]
    [InlineData("a{2,3}", "", "aaaa", "match 0,3")]
    [InlineData("a{2,3}?", "", "aaaa", "match 0,2")]
    [InlineData("a{1,1}", "", "a", "match 0,1")]
    [InlineData("a{0}", "", "a", "match 0,0")]
    [InlineData("(a){0}", "", "a", "match 0,0 -")]
    [InlineData("a{0,0}b", "", "ab", "match 1,2")]
    [InlineData("(?:a{2,3}){2}", "", "aaaaa", "match 0,5")]
    [InlineData("(?:a{2}b){2}", "", "aabaab", "match 0,6")]
    [InlineData("a{0,2147483647}", "", "aaa", "match 0,3")]
    [InlineData("a{2147483647,}", "", "aaa", "nomatch")]
    [InlineData("a{99999999999}", "", "aaa", "nomatch")]
    public void A_counted_quantifier_holds_its_bounds_at_their_edges(string pattern, string flags, string subject, string expected)
    {
        Assert.Equal(expected, Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(a*)*", "", "b", "match 0,0 -")]
    [InlineData("(a*)*", "", "aaa", "match 0,3 0,3")]
    [InlineData("(a?)?", "", "", "match 0,0 -")]
    [InlineData("(a|)*b", "", "aab", "match 0,3 1,2")]
    [InlineData("(?:a*)*b", "", "aab", "match 0,3")]
    [InlineData("(?:a*)*b", "", "c", "nomatch")]
    [InlineData("(?:a*)+b", "", "c", "nomatch")]
    [InlineData("(?:)*", "", "a", "match 0,0")]
    [InlineData("(?:){5}", "", "", "match 0,0")]
    [InlineData("(?:a?){3}", "", "a", "match 0,1")]
    [InlineData("(a?){2,}", "", "aa", "match 0,2 1,2")]
    [InlineData("(a?){2,}", "", "a", "match 0,1 1,1")]
    [InlineData("(?:a|){3,}?b", "", "ab", "match 0,2")]
    public void An_optional_iteration_that_consumes_nothing_fails_and_a_required_one_does_not(string pattern, string flags, string subject, string expected)
    {
        // RepeatMatcher's continuation, step a: "If min = 0 and y's endIndex = x's endIndex, return failure." A required
        // iteration (min > 0) may be empty; an optional one may not, and failing it takes back its captures.
        Assert.Equal(expected, Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?:(a)|b)+", "", "ab", "match 0,2 -")]
    [InlineData("(?:(a)|(b))+", "", "ab", "match 0,2 - 1,2")]
    [InlineData("((a)|b)+", "", "ab", "match 0,2 1,2 -")]
    [InlineData("(?:(a)|b){2}", "", "ab", "match 0,2 -")]
    [InlineData("(?:(a)|b){1,2}?c", "", "abc", "match 0,3 -")]
    [InlineData("(?:(a)|(b))*?c", "", "abc", "match 0,3 - 1,2")]
    [InlineData("(a)(?:(b)|c)*", "", "abc", "match 0,3 0,1 -")]
    public void A_quantified_atom_s_captures_are_cleared_at_each_iteration(string pattern, string flags, string subject, string expected)
    {
        // RepeatMatcher, steps 3 and 4: the captures of the atom's groups are cleared before each iteration, and a group
        // outside the atom keeps its capture.
        Assert.Equal(expected, Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(a)|(b)", "", "b", "match 0,1 - 0,1")]
    [InlineData("(?<x>a)(?<y>b)?", "", "a", "match 0,1 0,1 -")]
    [InlineData("((a)(b))", "", "ab", "match 0,2 0,2 0,1 1,2")]
    [InlineData("(?:(a)|(b))(c)", "", "bc", "match 0,2 - 0,1 1,2")]
    public void Each_group_s_offsets_are_written_with_minus_one_for_a_group_that_did_not_participate(string pattern, string flags, string subject, string expected)
    {
        Assert.Equal(expected, Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("b", "ab", 0, true, "nomatch")]
    [InlineData("b", "ab", 1, true, "match 1,2")]
    [InlineData("b", "ab", 0, false, "match 1,2")]
    [InlineData("a", "aba", 1, false, "match 2,3")]
    [InlineData("a", "aba", 1, true, "nomatch")]
    [InlineData("", "abc", 3, false, "match 3,3")]
    [InlineData("", "abc", 3, true, "match 3,3")]
    [InlineData("a", "abc", 3, false, "nomatch")]
    [InlineData("a*", "baa", 0, true, "match 0,0")]
    [InlineData("a+", "baa", 0, true, "nomatch")]
    public void An_anchored_call_tries_its_start_alone_and_a_search_every_position_after_it(string pattern, string subject, int start, bool anchored, string expected)
    {
        Assert.Equal(expected, Run(pattern, "", subject, start, anchored));
    }

    [Fact]
    public void The_anchored_argument_is_independent_of_the_y_flag()
    {
        // y changes nothing a program does: the caller says where a match starts (requirement R7).
        Assert.Equal("match 1,2", Run("b", "y", "ab"));
        Assert.Equal("nomatch", Run("b", "", "ab", anchored: true));
        Assert.Equal("nomatch", Run("b", "y", "ab", anchored: true));
    }

    [Theory]
    [InlineData("x", "u", "\U0001F600x", 0, "match 2,3")]
    [InlineData("\\uDE00", "u", "\U0001F600", 1, "match 1,2")]
    [InlineData("(?:)", "u", "\U0001F600", 1, "match 1,1")]
    [InlineData("\\uDE00", "", "a\U0001F600", 0, "match 2,3")]
    public void Under_u_a_search_steps_over_a_whole_surrogate_pair_and_a_start_inside_one_reads_a_lone_unit(string pattern, string flags, string subject, int start, string expected)
    {
        Assert.Equal(expected, Run(pattern, flags, subject, start));
    }

    [Fact]
    public void A_quantifier_that_gives_back_never_gives_back_past_the_start_inside_a_surrogate_pair()
    {
        // The old backend's defect D1 (DifferentialTests' remarks): its greedy run of one character gave back across a start
        // inside a pair, joining the low half to the high half before it. No forward matcher of ECMA-262 §22.2.2 moves before
        // the index it is called at, so the match and every capture begin at or after the start.
        var smile = "😀";
        Assert.Equal("nomatch", Run("[\\s\\S]*\\u{1F600}", "u", smile + "ba", 1));      // the old backend: match 1,2
        Assert.Equal("nomatch", Run("[\\s\\S]?\\u{1F600}", "v", smile + "ba", 1));      // the old backend: match 1,2
        Assert.Equal("nomatch", Run("(?:[\\s\\S])*\\u{1F600}", "u", smile + "ba", 1));  // the old backend agrees
        Assert.Equal("match 2,3", Run("[\\s\\S]?[^\\u{1F600}]", "u", "\uD83D" + smile, 2)); // the old backend: nomatch
        Assert.Equal("match 1,1 - 1,1", Run("(.{2})?\\u{1F600}|()", "u", smile + "bcx", 1));
        Assert.Equal("match 1,4 3,4", Run("(.)*", "u", smile + "bc", 1));
    }

    [Fact]
    public void A_greedy_star_over_a_long_subject_runs_on_the_heap_on_a_1_MiB_thread()
    {
        var subject = new string('x', 1_000_000);
        ProgramCompilerTests.OnASmallStack(() =>
        {
            Assert.Equal("match 0,1000000", Run("x*", "", subject));
            Assert.Equal("match 0,1000000 999999,1000000", Run("(x)*", "", subject));
            Assert.Equal("match 0,1000001", Run("(?:x|y)*z?.", "s", subject + "!"));
        });
    }

    [Fact]
    public void A_flat_sequence_of_100000_terms_matches_on_a_1_MiB_thread()
    {
        // The adversarial corpus's F03 and F04.
        var subject = new string('a', 100_000);
        var groups = string.Concat(Enumerable.Repeat("(?:a)", 100_000));
        var alternations = string.Concat(Enumerable.Repeat("(?:a|b)", 100_000));
        var captures = string.Concat(Enumerable.Repeat("(a)", 100_000));
        ProgramCompilerTests.OnASmallStack(() =>
        {
            Assert.Equal("match 0,100000", Run(groups, "", subject));
            Assert.Equal("match 0,100000", Run(alternations, "", subject));
            Assert.StartsWith("match 0,100000 0,1 1,2 2,3 ", Run(captures, "", subject));
        });
    }

    [Theory]
    [MemberData(nameof(ProgramCompilerTests.SubsetShapes), MemberType = typeof(ProgramCompilerTests))]
    public void A_pattern_at_the_nesting_maximum_matches_on_a_1_MiB_thread(string shape)
    {
        // Every shape of NestingMaximumTests within task 2.4a's subset, the shapes the closure matcher could not match at
        // the maximum among them ("capturing sequence", "capturing alternation of sequences" and the rest).
        var (pattern, flags, subject) = NestingMaximumTests.Shape(shape, BroilerRegex.MaximumNestingLimit - 1);
        var expected = TestRegex.Compile(NestingMaximumTests.Shape(shape, 3).Pattern, flags).Match(NestingMaximumTests.Shape(shape, 3).Subject).Success;
        Assert.True(expected, shape + " matches at three levels");
        ProgramCompilerTests.OnASmallStack(() => Assert.StartsWith("match ", Run(pattern, FlagsText(flags), subject)));
    }

    private static string FlagsText(RegexFlags flags) => (flags & RegexFlags.UnicodeSets) != 0 ? "v" : (flags & RegexFlags.Unicode) != 0 ? "u" : "";
}
