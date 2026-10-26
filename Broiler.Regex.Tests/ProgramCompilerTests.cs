using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Broiler.Regex.Ast;
using Broiler.Regex.Compiling;
using Broiler.Regex.Engine;
using Broiler.Regex.Parsing;
using Broiler.Regex.Unicode;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The program compiler of task 2.4a (decision D5 of Broiler.VM's JSD-0081): the lowering of each construct of the subset
/// to instructions, the transitional "not yet supported" answer for task 2.4b's constructs, the caller's nesting limit,
/// and compilation at the nesting maximum and of a flat sequence of 100,000 terms on a 1 MiB thread.
/// </summary>
public class ProgramCompilerTests
{
    /// <summary>The program's instructions, one per entry, as "Op A,B,C"; the operands an instruction does not read are left out.</summary>
    internal static string[] Listing(RegexProgram program) => program.Code.ToArray().Select(static i => i.Op switch
    {
        RegexOp.Literal or RegexOp.Class or RegexOp.Goto or RegexOp.SavePosition or RegexOp.SetCounter or RegexOp.IncrementCounter
            => i.Op + " " + Decimal(i.A),
        RegexOp.Choice or RegexOp.ClearCaptures => i.Op + " " + Decimal(i.A) + "," + Decimal(i.B),
        RegexOp.ExitAtMaximum or RegexOp.EnterBelowMinimum or RegexOp.RejectEmptyIteration
            => i.Op + " " + Decimal(i.A) + "," + Decimal(i.B) + "," + Decimal(i.C),
        _ => i.Op.ToString(),
    }).ToArray();

    private static string Decimal(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("a", "", new[] { "Literal 97", "Succeed" })]
    [InlineData("ab", "", new[] { "Literal 97", "Literal 98", "Succeed" })]
    [InlineData("", "", new[] { "Succeed" })]
    [InlineData(".", "", new[] { "AnyButLineTerminator", "Succeed" })]
    [InlineData(".", "s", new[] { "Any", "Succeed" })]
    [InlineData("\\u{1F600}", "u", new[] { "Literal 128512", "Succeed" })]
    [InlineData("a|b", "", new[] { "Choice 1,3", "Literal 97", "Goto 4", "Literal 98", "Succeed" })]
    [InlineData("a|b|", "", new[] { "Choice 1,3", "Literal 97", "Goto 6", "Choice 4,6", "Literal 98", "Goto 6", "Succeed" })]
    [InlineData("(a)", "", new[] { "SavePosition 2", "Literal 97", "SavePosition 3", "Succeed" })]
    [InlineData("(?:a)", "", new[] { "Literal 97", "Succeed" })]
    [InlineData("(?<n>a)(b)", "", new[] { "SavePosition 2", "Literal 97", "SavePosition 3", "SavePosition 4", "Literal 98", "SavePosition 5", "Succeed" })]
    [InlineData("a?", "", new[] { "Choice 1,4", "SavePosition 2", "Literal 97", "RejectEmptyIteration 2,-1,0", "Succeed" })]
    [InlineData("a??", "", new[] { "Choice 4,1", "SavePosition 2", "Literal 97", "RejectEmptyIteration 2,-1,0", "Succeed" })]
    [InlineData("a*", "", new[] { "Choice 1,5", "SavePosition 2", "Literal 97", "RejectEmptyIteration 2,-1,0", "Goto 0", "Succeed" })]
    [InlineData("a*?", "", new[] { "Choice 5,1", "SavePosition 2", "Literal 97", "RejectEmptyIteration 2,-1,0", "Goto 0", "Succeed" })]
    [InlineData("a+", "", new[] { "SetCounter 3", "EnterBelowMinimum 3,1,3", "Choice 3,8", "SavePosition 2", "Literal 97", "RejectEmptyIteration 2,3,1", "IncrementCounter 3", "Goto 1", "Succeed" })]
    [InlineData("a{2,3}", "", new[] { "SetCounter 3", "ExitAtMaximum 3,3,9", "EnterBelowMinimum 3,2,4", "Choice 4,9", "SavePosition 2", "Literal 97", "RejectEmptyIteration 2,3,2", "IncrementCounter 3", "Goto 1", "Succeed" })]
    [InlineData("a{2,3}?", "", new[] { "SetCounter 3", "ExitAtMaximum 3,3,9", "EnterBelowMinimum 3,2,4", "Choice 9,4", "SavePosition 2", "Literal 97", "RejectEmptyIteration 2,3,2", "IncrementCounter 3", "Goto 1", "Succeed" })]
    [InlineData("a{0,2}", "", new[] { "SetCounter 3", "ExitAtMaximum 3,2,8", "Choice 3,8", "SavePosition 2", "Literal 97", "RejectEmptyIteration 2,3,0", "IncrementCounter 3", "Goto 1", "Succeed" })]
    [InlineData("a{0}", "", new[] { "Succeed" })]
    [InlineData("(a){0}b", "", new[] { "Literal 98", "Succeed" })]
    [InlineData("(a)*", "", new[] { "Choice 1,8", "SavePosition 4", "ClearCaptures 2,3", "SavePosition 2", "Literal 97", "SavePosition 3", "RejectEmptyIteration 4,-1,0", "Goto 0", "Succeed" })]
    public void Each_construct_lowers_to_the_instructions_of_the_remarks(string pattern, string flags, string[] expected)
    {
        Assert.Equal(expected, Listing(TestProgram.Compile(pattern, flags)));
    }

    [Fact]
    public void A_quantifier_resets_the_groups_of_its_own_body_at_each_iteration_and_no_others()
    {
        // Groups 1 to 3 are the star's body; their registers are 2 to 7.
        Assert.Contains("ClearCaptures 2,7", Listing(TestProgram.Compile("((a)|(b))*")));

        // The outer star's body holds groups 2 and 3, the inner plus's group 3 alone; group 1 is outside both.
        var listing = Listing(TestProgram.Compile("(a)(?:(b)(c)+)*"));
        Assert.Equal(["ClearCaptures 4,7", "ClearCaptures 6,7"], listing.Where(static l => l.StartsWith("ClearCaptures", StringComparison.Ordinal)));

        // A quantified body without groups resets nothing.
        Assert.DoesNotContain(Listing(TestProgram.Compile("(a)(?:b)*")), static l => l.StartsWith("ClearCaptures", StringComparison.Ordinal));
    }

    [Fact]
    public void A_class_is_its_code_points_with_its_negation_applied_and_costs_one_plus_its_weight()
    {
        var program = TestProgram.Compile("[a-c][^a-c]\\d");
        Assert.Equal([97, 99], program.ClassRanges(0).ToArray());
        Assert.Equal([0, 96, 100, CodePointSet.MaxCodePoint], program.ClassRanges(1).ToArray());
        Assert.Equal([48, 57], program.ClassRanges(2).ToArray());
        Assert.Equal([1, 2, 1, 1], program.Code.ToArray().Select(static i => i.Cost));

        // \p{L} is 684 ranges at UCD 17.0.0: a test reads at most ten of them.
        var letters = TestProgram.Compile("\\p{L}", "u");
        Assert.Equal(684, letters.ClassRanges(0).Length / 2);
        Assert.Equal(10, letters.Code[0].Cost);

        // A v-mode class of code points is a class; its set operations are done when it is parsed.
        var v = TestProgram.Compile("[[a-z]--[aeiou]]", "v");
        Assert.Equal(RegexOp.Class, v.Code[0].Op);
        Assert.Equal(5, v.ClassRanges(0).Length / 2);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(7, 2)]
    [InlineData(8, 3)]
    [InlineData(684, 9)]
    public void A_class_weight_is_the_comparisons_a_binary_search_makes_beyond_the_first(int ranges, int weight)
    {
        Assert.Equal(weight, RegexProgram.ClassWeight(ranges));
    }

    [Fact]
    public void Every_cost_is_at_least_one_and_a_capture_reset_costs_one_per_group()
    {
        var program = TestProgram.Compile("((a)(b)(c))+");
        Assert.All(program.Code.ToArray(), static i => Assert.True(i.Cost >= 1));
        var clear = program.Code.ToArray().Single(static i => i.Op == RegexOp.ClearCaptures);
        Assert.Equal(4, clear.Cost);
    }

    [Fact]
    public void The_registers_are_the_capture_registers_then_one_or_two_per_quantifier()
    {
        // Two groups: registers 0 to 5. a* takes one (its mark), b+ two (its mark and its count).
        var program = TestProgram.Compile("(a*)(b+)");
        Assert.Equal(2, program.CaptureCount);
        Assert.Equal(6, program.SlotCount);
        Assert.Equal(9, program.RegisterCount);
        Assert.False(program.Unicode);
        Assert.True(TestProgram.Compile("a", "v").Unicode);
    }

    [Theory]
    [InlineData("^a", "")]
    [InlineData("a$", "")]
    [InlineData("\\ba", "")]
    [InlineData("\\Ba", "")]
    [InlineData("(a)\\1", "")]
    [InlineData("(?<n>a)\\k<n>", "")]
    [InlineData("(?=a)", "")]
    [InlineData("(?!a)", "")]
    [InlineData("(?<=a)b", "")]
    [InlineData("(?<!a)b", "")]
    [InlineData("(?=a){0}b", "")]
    [InlineData("(?i:a)", "")]
    [InlineData("(?-m:a)", "")]
    [InlineData("a", "i")]
    [InlineData("[a]", "iv")]
    [InlineData("[\\q{ab}]", "v")]
    [InlineData("\\p{RGI_Emoji}", "v")]
    [InlineData("x(?:y|(z\\b))*", "")]
    public void A_construct_of_task_2_4b_is_answered_not_yet_supported_with_no_program_and_nothing_thrown(string pattern, string flags)
    {
        Assert.Equal(ProgramCompilation.NotYetSupported, ProgramCompiler.Compile(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out var program));
        Assert.Null(program);
    }

    [Fact]
    public void The_flags_that_change_no_instruction_are_accepted()
    {
        foreach (var flags in (string[])["d", "g", "m", "y", "dgmsy"])
            Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile("a", RegexFlagsParser.Parse(flags), UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out _));
    }

    [Fact]
    public void A_syntax_error_is_the_parser_s_and_an_out_of_range_limit_a_broken_precondition()
    {
        Assert.Throws<RegexSyntaxException>(() => ProgramCompiler.Compile("(", RegexFlags.None, UnicodeTables.Instance, 512, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProgramCompiler.Compile("a", RegexFlags.None, UnicodeTables.Instance, 0, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProgramCompiler.Compile("a", RegexFlags.None, UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit + 1, out _));
        Assert.Throws<ArgumentNullException>(() => ProgramCompiler.Compile(null!, RegexFlags.None, UnicodeTables.Instance, 512, out _));
    }

    [Fact]
    public void The_nesting_limit_is_the_parser_s_and_is_checked_again_before_each_group_s_body()
    {
        string Nested(int levels) => string.Concat(Enumerable.Repeat("(", levels)) + "a" + string.Concat(Enumerable.Repeat(")", levels));

        Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(Nested(3), RegexFlags.None, UnicodeTables.Instance, 4, out _));
        Assert.Equal(ProgramCompilation.NestingLimitExceeded, ProgramCompiler.Compile(Nested(4), RegexFlags.None, UnicodeTables.Instance, 4, out var refused));
        Assert.Null(refused);

        // A tree the parser built under a deeper limit is refused by the compiler's own check.
        var data = new RegexUnicode(UnicodeTables.Instance);
        var tree = RegexParser.Parse(Nested(4), RegexFlags.None, data, new RegexNesting(512), out var captures, out _);
        Assert.Equal(ProgramCompilation.NestingLimitExceeded, ProgramCompiler.Compile(tree, captures, RegexFlags.None, new RegexNesting(4), out var program));
        Assert.Null(program);
        Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(tree, captures, RegexFlags.None, new RegexNesting(5), out _));
    }

    /// <summary>The subset's shapes of <see cref="NestingMaximumTests"/>: those built of groups, alternations, sequences, quantifiers and v-mode classes alone.</summary>
    public static TheoryData<string> SubsetShapes => new()
    {
        "capturing", "unicode capturing", "non-capturing", "named", "v class", "v complement", "v intersection",
        "v subtraction", "v union", "repeated capturing group", "repeated non-capturing group", "optional capturing group",
        "alternation", "sequence", "alternation of sequences", "capturing sequence", "capturing alternation of sequences",
        "named alternation of sequences", "capturing alternation of optional sequences", "repeated alternation of sequences",
        "repeated capturing alternation of sequences",
    };

    [Theory]
    [MemberData(nameof(SubsetShapes))]
    public void A_pattern_at_the_maximum_compiles_on_a_1_MiB_stack_and_one_level_past_it_is_refused(string shape)
    {
        var max = BroilerRegex.MaximumNestingLimit;
        var (atMaximum, flags, _) = NestingMaximumTests.Shape(shape, max - 1);
        var (pastMaximum, _, _) = NestingMaximumTests.Shape(shape, max);
        OnASmallStack(() =>
        {
            Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(atMaximum, flags, UnicodeTables.Instance, max, out var program));
            Assert.NotNull(program);
            Assert.Equal(ProgramCompilation.NestingLimitExceeded, ProgramCompiler.Compile(pastMaximum, flags, UnicodeTables.Instance, max, out _));
        });
    }

    [Fact]
    public void A_flat_sequence_of_100000_terms_compiles_on_a_1_MiB_stack()
    {
        var groups = string.Concat(Enumerable.Repeat("(?:a)", 100_000));
        var alternations = string.Concat(Enumerable.Repeat("(?:a|b)", 100_000));
        OnASmallStack(() =>
        {
            Assert.Equal(100_001, TestProgram.Compile(groups).Code.Length);
            Assert.Equal(400_001, TestProgram.Compile(alternations).Code.Length);
        });
    }

    /// <summary>Runs <paramref name="body"/> on a new thread of a 1 MiB stack, the corpus's declared stack, and rethrows what it throws.</summary>
    internal static void OnASmallStack(Action body)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                body();
            }
            catch (Exception e)
            {
                failure = ExceptionDispatchInfo.Capture(e);
            }
        }, 1 << 20);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}

/// <summary>Compiles a pattern to the new backend's program against the Unicode owner's tables, under the library's maximum nesting limit.</summary>
internal static class TestProgram
{
    internal static RegexProgram Compile(string pattern, string flags = "")
    {
        var answer = ProgramCompiler.Compile(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out var program);
        Assert.True(answer == ProgramCompilation.Compiled, "/" + pattern + "/" + flags + " compiled to " + answer);
        return program!;
    }
}
