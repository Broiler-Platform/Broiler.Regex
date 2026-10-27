using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
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
/// The program compiler of tasks 2.4a and 2.4b (decision D5 of Broiler.VM's JSD-0081): the lowering of each construct of
/// the grammar to instructions, the absence of task 2.4a's transitional answer, the caller's nesting limit, and
/// compilation at the nesting maximum and of a flat sequence of 100,000 terms on a 1 MiB thread.
/// </summary>
public class ProgramCompilerTests
{
    /// <summary>
    /// The program's instructions, one per entry, as "Op A,B,C"; the operands an instruction does not read are left out,
    /// and an instruction that reads right to left ends in " rtl".
    /// </summary>
    internal static string[] Listing(RegexProgram program) => program.Code.ToArray().Select(static i => i.Op switch
    {
        RegexOp.Literal or RegexOp.Class or RegexOp.Goto or RegexOp.SavePosition or RegexOp.SetCounter or RegexOp.IncrementCounter
            or RegexOp.LiteralIgnoringCase or RegexOp.CanonicalClass or RegexOp.LineStart or RegexOp.LineEnd
            or RegexOp.WordEdge or RegexOp.NotWordEdge
            => i.Op + " " + Decimal(i.A),
        RegexOp.Choice or RegexOp.ClearCaptures or RegexOp.ClassIgnoringCase or RegexOp.Backreference or RegexOp.BackreferenceIgnoringCase
            or RegexOp.CloseLookaround or RegexOp.GreedyRun or RegexOp.LazyRun or RegexOp.StringClass
            => i.Op + " " + Decimal(i.A) + "," + Decimal(i.B),
        RegexOp.AtomRun => i.Op + " " + Decimal(i.A),
        RegexOp.ExitAtMaximum or RegexOp.EnterBelowMinimum or RegexOp.RejectEmptyIteration or RegexOp.OpenLookaround
            => i.Op + " " + Decimal(i.A) + "," + Decimal(i.B) + "," + Decimal(i.C),
        _ => i.Op.ToString(),
    } + (i.RightToLeft ? " rtl" : "")).ToArray();

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
        // The general lowering, which the fast paths of task 2.4c replace for three shapes (the theory after this one).
        Assert.Equal(expected, Listing(TestProgram.General(pattern, flags)));
    }

    [Theory]
    [InlineData("a*", "", new[] { "GreedyRun 0,2147483647", "Literal 97", "Succeed" })]
    [InlineData("a*?", "", new[] { "LazyRun 0,2147483647", "Literal 97", "Succeed" })]
    [InlineData("a+", "", new[] { "GreedyRun 1,2147483647", "Literal 97", "Succeed" })]
    [InlineData("a??", "", new[] { "LazyRun 0,1", "Literal 97", "Succeed" })]
    [InlineData("[a-c]{2,3}", "", new[] { "GreedyRun 2,3", "Class 0", "Succeed" })]
    [InlineData(".{2}?", "s", new[] { "LazyRun 2,2", "Any", "Succeed" })]
    [InlineData("\\p{L}*", "iu", new[] { "GreedyRun 0,2147483647", "ClassIgnoringCase 0,0", "Succeed" })]
    [InlineData("a{0}b", "", new[] { "Literal 98", "Succeed" })]
    [InlineData("abc", "", new[] { "AtomRun 3", "Literal 97", "Literal 98", "Literal 99", "Succeed" })]
    [InlineData("ab*c.d", "", new[] { "Literal 97", "GreedyRun 0,2147483647", "Literal 98", "AtomRun 3", "Literal 99", "AnyButLineTerminator", "Literal 100", "Succeed" })]
    [InlineData("a|bc", "", new[] { "Choice 1,3", "Literal 97", "Goto 6", "AtomRun 2", "Literal 98", "Literal 99", "Succeed" })]
    [InlineData("(a)b", "", new[] { "SavePosition 2", "Literal 97", "SavePosition 3", "Literal 98", "Succeed" })]
    [InlineData("(?:a)*", "", new[] { "Choice 1,5", "SavePosition 2", "Literal 97", "RejectEmptyIteration 2,-1,0", "Goto 0", "Succeed" })]
    [InlineData("(a)*", "", new[] { "Choice 1,8", "SavePosition 4", "ClearCaptures 2,3", "SavePosition 2", "Literal 97", "SavePosition 3", "RejectEmptyIteration 4,-1,0", "Goto 0", "Succeed" })]
    [InlineData("(?<=ab)c", "", new[] { "OpenLookaround 2,0,5", "AtomRun 2 rtl", "Literal 98 rtl", "Literal 97 rtl", "CloseLookaround 2,0", "Literal 99", "Succeed" })]
    [InlineData("(?<=a*)", "", new[] { "OpenLookaround 2,0,4", "GreedyRun 0,2147483647 rtl", "Literal 97 rtl", "CloseLookaround 2,0", "Succeed" })]
    [InlineData("(?<=[a-c].?)", "", new[] { "OpenLookaround 2,0,5", "GreedyRun 0,1 rtl", "AnyButLineTerminator rtl", "Class 0 rtl", "CloseLookaround 2,0", "Succeed" })]
    [InlineData("[\\q{abc|ab}]", "v", new[] { "StringClass 0,0", "Class 0", "Succeed" })]
    [InlineData("[\\q{ab|}c]", "v", new[] { "StringClass 0,1", "Class 0", "Succeed" })]
    [InlineData("[\\q{AB}]", "vi", new[] { "StringClass 0,0", "CanonicalClass 0", "Succeed" })]
    [InlineData("(?<=[\\q{ab}])", "v", new[] { "OpenLookaround 2,0,4", "StringClass 0,0 rtl", "Class 0 rtl", "CloseLookaround 2,0", "Succeed" })]
    public void Each_fast_path_lowers_to_its_instruction_and_the_ones_it_owns(string pattern, string flags, string[] expected)
    {
        // Task 2.4c: a quantifier over one character or class is a run followed by its atom, two or more atoms in a row of
        // one sequence an atom run followed by them in the order they are matched (right to left the last first), a class
        // of strings its table's instruction followed by the class of its single characters. A group, a character alone and
        // a quantified group keep the general lowering.
        Assert.Equal(expected, Listing(TestProgram.Compile(pattern, flags)));
    }

    [Theory]
    [InlineData("a", "", 0)]
    [InlineData("(a)b", "", 1)]
    [InlineData("((a))", "", 2)]
    [InlineData("abc", "", 1)]
    [InlineData("[a-c]+x", "", 1)]
    [InlineData("a{2}b", "", 1)]
    [InlineData("a+?b", "", 1)]
    [InlineData(".x", "", 1)]
    [InlineData("\\p{L}", "u", 0)]
    [InlineData("k", "iu", 0)]
    [InlineData("(?:(a)b)*?", "", -1)]
    [InlineData("a*b", "", -1)]
    [InlineData("a?b", "", -1)]
    [InlineData("a|b", "", -1)]
    [InlineData("(?:a|b)c", "", -1)]
    [InlineData("^a", "", -1)]
    [InlineData("\\ba", "", -1)]
    [InlineData("(?=a)a", "", -1)]
    [InlineData("(?<=a)b", "", -1)]
    [InlineData("\\1(a)", "", -1)]
    [InlineData("(?:ab){2}", "", -1)]
    [InlineData("[\\q{ab}c]", "v", -1)]
    [InlineData("", "", -1)]
    public void The_first_test_is_the_one_character_test_every_attempt_makes_before_any_choice(string pattern, string flags, int address)
    {
        // Task 2.4c's literal-prefix prefilter: past the group starts at the front, a character or class instruction, the
        // first atom of an atom run, or the atom of a run that requires a character. A choice, an assertion, a look-around,
        // a reference, a counted loop's bound, a class of strings, a run that may take nothing or nothing at all leaves it
        // -1, and so does every program of the general lowering.
        var program = TestProgram.Compile(pattern, flags);
        Assert.Equal(address, program.FirstTest);
        if (address >= 0)
            Assert.Contains(program.Code[address].Op, (RegexOp[])[RegexOp.Literal, RegexOp.LiteralIgnoringCase, RegexOp.Class, RegexOp.ClassIgnoringCase, RegexOp.CanonicalClass, RegexOp.Any, RegexOp.AnyButLineTerminator]);
        Assert.Equal(-1, TestProgram.General(pattern, flags).FirstTest);
    }

    [Fact]
    public void A_run_and_an_atom_run_cost_their_dispatch_and_the_atom_its_own()
    {
        // A run's dispatch is one unit, its characters the atom's cost each; an atom run costs its first atom's cost, so
        // that folding moves no unit.
        var run = TestProgram.Compile("\\p{L}*", "u");
        Assert.Equal((1, 10), (run.Code[0].Cost, run.Code[1].Cost));
        var folded = TestProgram.Compile("\\p{L}ab", "u");
        Assert.Equal((RegexOp.AtomRun, 10, 10, 1, 1), (folded.Code[0].Op, folded.Code[0].Cost, folded.Code[1].Cost, folded.Code[2].Cost, folded.Code[3].Cost));
        Assert.Equal(TestProgram.General("\\p{L}ab", "u").Code.ToArray().Sum(static i => i.Cost), folded.Code.ToArray().Skip(1).Sum(static i => i.Cost));
    }

    [Theory]
    [InlineData("a", "i", new[] { "LiteralIgnoringCase 65", "Succeed" })]
    [InlineData("a", "iu", new[] { "LiteralIgnoringCase 97", "Succeed" })]
    [InlineData("\\u017F", "i", new[] { "LiteralIgnoringCase 383", "Succeed" })]
    [InlineData("\\u017F", "iu", new[] { "LiteralIgnoringCase 115", "Succeed" })]
    [InlineData("[a-c]", "i", new[] { "ClassIgnoringCase 0,0", "Succeed" })]
    [InlineData("[^a-c]", "i", new[] { "ClassIgnoringCase 0,1", "Succeed" })]
    [InlineData("[a-c]", "iv", new[] { "CanonicalClass 0", "Succeed" })]
    [InlineData("[^a-c]", "iv", new[] { "CanonicalClass 0", "Succeed" })]
    public void Under_i_a_character_is_its_Canonicalize_and_a_class_its_members_tested_by_case_orbit(string pattern, string flags, string[] expected)
    {
        // A character is compared by Canonicalize, the non-u branch an upper case (A, 65) that keeps U+017F outside ASCII,
        // and the u branch simple case folding (s, 115). A class keeps its members as written and its inversion apart from
        // them, as CharacterSetMatcher inverts after it compares; a v-mode class is already folded and complemented.
        var program = TestProgram.Compile(pattern, flags);
        Assert.Equal(expected, Listing(program));
        Assert.NotNull(program.Folding);
    }

    [Theory]
    [InlineData("^a$", "", new[] { "LineStart 0", "Literal 97", "LineEnd 0", "Succeed" })]
    [InlineData("^a$", "m", new[] { "LineStart 1", "Literal 97", "LineEnd 1", "Succeed" })]
    [InlineData("\\ba\\B", "", new[] { "WordEdge 0", "Literal 97", "NotWordEdge 0", "Succeed" })]
    [InlineData("\\b[a]\\b", "iu", new[] { "WordEdge 0", "ClassIgnoringCase 1,0", "WordEdge 0", "Succeed" })]
    public void An_assertion_lowers_to_one_instruction_reading_the_flags_in_force(string pattern, string flags, string[] expected)
    {
        Assert.Equal(expected, Listing(TestProgram.Compile(pattern, flags)));
    }

    [Theory]
    [InlineData("(?i:a)a", "", new[] { "LiteralIgnoringCase 65", "Literal 97", "Succeed" })]
    [InlineData("(?-i:a)a", "i", new[] { "Literal 97", "LiteralIgnoringCase 65", "Succeed" })]
    [InlineData("(?m:^)^", "", new[] { "LineStart 1", "LineStart 0", "Succeed" })]
    [InlineData("(?s-m:.$).$", "m", new[] { "Any", "LineEnd 0", "AnyButLineTerminator", "LineEnd 1", "Succeed" })]
    [InlineData("(?i:\\b)\\b", "u", new[] { "WordEdge 0", "WordEdge 1", "Succeed" })]
    public void A_modifier_group_lowers_its_body_under_its_flags_and_nothing_after_it(string pattern, string flags, string[] expected)
    {
        Assert.Equal(expected, Listing(TestProgram.Compile(pattern, flags)));
    }

    [Theory]
    [InlineData("(?=a)", "", new[] { "OpenLookaround 2,0,3", "Literal 97", "CloseLookaround 2,0", "Succeed" })]
    [InlineData("(?!a)b", "", new[] { "OpenLookaround 2,1,3", "Literal 97", "CloseLookaround 2,1", "Literal 98", "Succeed" })]
    [InlineData("(?<=ab)c", "", new[] { "OpenLookaround 2,0,4", "Literal 98 rtl", "Literal 97 rtl", "CloseLookaround 2,0", "Literal 99", "Succeed" })]
    [InlineData("(?<!a|bc)", "", new[] { "OpenLookaround 2,1,7", "Choice 2,4", "Literal 97 rtl", "Goto 6", "Literal 99 rtl", "Literal 98 rtl", "CloseLookaround 2,1", "Succeed" })]
    [InlineData("(?<=(a)\\1)", "i", new[] { "OpenLookaround 4,0,6", "BackreferenceIgnoringCase 0,1 rtl", "SavePosition 3", "LiteralIgnoringCase 65 rtl", "SavePosition 2", "CloseLookaround 4,0", "Succeed" })]
    [InlineData("(?<=[a-c].)", "", new[] { "OpenLookaround 2,0,4", "AnyButLineTerminator rtl", "Class 0 rtl", "CloseLookaround 2,0", "Succeed" })]
    [InlineData("(?<=(?=a).)", "", new[] { "OpenLookaround 2,0,6", "AnyButLineTerminator rtl", "OpenLookaround 3,0,5", "Literal 97", "CloseLookaround 3,0", "CloseLookaround 2,0", "Succeed" })]
    [InlineData("(?=(?<=a)b)", "", new[] { "OpenLookaround 2,0,6", "OpenLookaround 3,0,4", "Literal 97 rtl", "CloseLookaround 3,0", "Literal 98", "CloseLookaround 2,0", "Succeed" })]
    [InlineData("(?<=a*)", "", new[] { "OpenLookaround 2,0,7", "Choice 2,6", "SavePosition 3", "Literal 97 rtl", "RejectEmptyIteration 3,-1,0", "Goto 1", "CloseLookaround 2,0", "Succeed" })]
    [InlineData("(?<=^\\b$)", "m", new[] { "OpenLookaround 2,0,5", "LineEnd 1", "WordEdge 0", "LineStart 1", "CloseLookaround 2,0", "Succeed" })]
    public void A_look_around_lowers_to_a_barrier_around_its_body_and_a_look_behind_s_body_right_to_left(string pattern, string flags, string[] expected)
    {
        // A look-around's register is its own, taken when it is lowered; its start's third operand is the address after
        // its close. Backward, a sequence's terms are emitted last first and a group's end register is written first;
        // assertions, choices and loops read no direction.
        Assert.Equal(expected, Listing(TestProgram.General(pattern, flags)));
    }

    [Fact]
    public void A_back_reference_reads_a_list_of_its_group_from_the_program_s_referenced_groups()
    {
        // Each reference holds a run of entries of the program's list, one group long today; task 2.6's duplicate names
        // make a run of the groups that share a name, with no change to the instruction.
        var program = TestProgram.Compile("(?<a>x)(y)\\2\\k<a>(?i:\\1)");
        Assert.Equal(["Backreference 0,1", "Backreference 1,1", "BackreferenceIgnoringCase 2,1"],
            Listing(program).Where(static l => l.StartsWith("Backreference", StringComparison.Ordinal)));
        Assert.Equal([2], program.ReferencedGroups(0, 1).ToArray());
        Assert.Equal([1], program.ReferencedGroups(1, 1).ToArray());
        Assert.Equal([1], program.ReferencedGroups(2, 1).ToArray());
        Assert.All(program.Code.ToArray().Where(static i => i.Op is RegexOp.Backreference or RegexOp.BackreferenceIgnoringCase), static i => Assert.Equal(1, i.Cost));
    }

    [Fact]
    public void A_word_boundary_reads_the_word_characters_of_its_flags_and_costs_one_plus_their_weight_twice()
    {
        // WordCharacters (§22.2.2.9.3): the basic set's four ranges, and under u or v with i two more, U+017F and the
        // Kelvin sign; a boundary tests the characters on both sides, so it costs 1 + 2 x 2.
        var basic = TestProgram.Compile("\\b", "i");
        Assert.Equal([48, 57, 65, 90, 95, 95, 97, 122], basic.ClassRanges(0).ToArray());
        var folded = TestProgram.Compile("\\b", "iu");
        Assert.Equal([48, 57, 65, 90, 95, 95, 97, 122, 0x17F, 0x17F, 0x212A, 0x212A], folded.ClassRanges(0).ToArray());
        Assert.Equal((5, 5), (basic.Code[0].Cost, folded.Code[0].Cost));
        Assert.Equal(1, TestProgram.Compile("^$", "m").Code[0].Cost);
    }

    [Fact]
    public void A_class_under_i_costs_one_plus_its_weight_for_each_code_point_of_an_orbit()
    {
        // Each orbit holds at most four code points at UCD 17.0.0, in both branches of Canonicalize; \p{L} is 684 ranges,
        // weight nine, so a test under iu searches for up to four code points: 1 + 4 x 9. A v-mode class under i searches
        // for one, the character's Canonicalize, in the folded set's own ranges. A class of one range costs one however it is
        // tested.
        var unicode = TestProgram.Compile("\\p{L}", "iu");
        Assert.Equal((4, 37), (unicode.OrbitLength, unicode.Code[0].Cost));
        var sets = TestProgram.Compile("\\p{L}", "iv");
        Assert.Equal((RegexOp.CanonicalClass, 1 + RegexProgram.ClassWeight(sets.ClassRanges(0).Length / 2)), (sets.Code[0].Op, sets.Code[0].Cost));
        var legacy = TestProgram.Compile("[a-z]", "i");
        Assert.Equal((4, 1), (legacy.OrbitLength, legacy.Code[0].Cost));
        Assert.Equal(1, TestProgram.Compile("a", "i").Code[0].Cost);
        Assert.Null(TestProgram.Compile("a").Folding);
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
        var program = TestProgram.General("[a-c][^a-c]\\d");
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
    public void Every_cost_is_one_but_a_class_test_s_and_a_capture_reset_of_any_width_costs_one()
    {
        // Decision D8: one unit per instruction dispatched, and a class test's weight on top; nothing else is weighed, a
        // look-around's start and close and a backward read included.
        var program = TestProgram.Compile("((a)(b)(c))+[a-cx-z](?<=[a-cx-z](?!b))");
        Assert.All(program.Code.ToArray(), static i => Assert.Equal(i.Op == RegexOp.Class ? 2 : 1, i.Cost));
        var clear = program.Code.ToArray().Single(static i => i.Op == RegexOp.ClearCaptures);
        Assert.Equal((2, 9, 1), (clear.A, clear.B, clear.Cost));
    }

    [Fact]
    public void The_registers_are_the_capture_registers_then_one_or_two_per_quantifier()
    {
        // Two groups: registers 0 to 5. In the general lowering a* takes one (its mark), b+ two (its mark and its count); as
        // runs they take none.
        var program = TestProgram.General("(a*)(b+)");
        Assert.Equal(2, program.CaptureCount);
        Assert.Equal(6, program.SlotCount);
        Assert.Equal(9, program.RegisterCount);
        Assert.False(program.Unicode);
        Assert.True(TestProgram.Compile("a", "v").Unicode);
        Assert.Equal(6, TestProgram.Compile("(a*)(b+)").RegisterCount);
    }

    [Theory]
    [InlineData("[\\q{abc|ab}]", "v", new[] { "Choice 1,5", "Literal 97", "Literal 98", "Literal 99", "Goto 10", "Choice 6,9", "Literal 97", "Literal 98", "Goto 10", "Class 0", "Succeed" })]
    [InlineData("[\\q{ab}c-e]", "v", new[] { "Choice 1,4", "Literal 97", "Literal 98", "Goto 5", "Class 0", "Succeed" })]
    [InlineData("[\\q{ab|}c]", "v", new[] { "Choice 1,4", "Literal 97", "Literal 98", "Goto 7", "Choice 5,7", "Class 0", "Goto 7", "Succeed" })]
    [InlineData("[\\q{}]", "v", new[] { "Choice 1,3", "Class 0", "Goto 3", "Succeed" })]
    [InlineData("[\\q{AB}]", "vi", new[] { "Choice 1,4", "LiteralIgnoringCase 97", "LiteralIgnoringCase 98", "Goto 5", "CanonicalClass 0", "Succeed" })]
    [InlineData("[\\q{\\u{1F600}a}]", "v", new[] { "Choice 1,4", "Literal 128512", "Literal 97", "Goto 5", "Class 0", "Succeed" })]
    [InlineData("(?<=[\\q{ab}])", "v", new[] { "OpenLookaround 2,0,7", "Choice 2,5", "Literal 98 rtl", "Literal 97 rtl", "Goto 6", "Class 0 rtl", "CloseLookaround 2,0", "Succeed" })]
    public void A_class_with_strings_lowers_to_an_alternation_of_its_strings_longest_first_then_its_characters_then_the_empty_string(string pattern, string flags, string[] expected)
    {
        // §22.2.2.7, Atom :: CharacterClass, steps 3 to 8. The class of single characters is always an alternative, even
        // empty, as the specification appends its CharacterSetMatcher; the empty string, when held, is an empty last one.
        Assert.Equal(expected, Listing(TestProgram.General(pattern, flags)));
    }

    [Fact]
    public void The_transitional_not_yet_supported_answer_is_gone_from_the_library_s_metadata()
    {
        // Task 2.4a's seam: the compiler's third answer, for a construct whose instructions task 2.4b had still to add.
        // It is not an answer of the enumeration any more, and no member, type or string of the assembly names it. Task
        // 2.5's compile budget is the enumeration's third answer now.
        Assert.Equal(["Compiled", "NestingLimitExceeded", "CompileBudgetExceeded"], Enum.GetNames<ProgramCompilation>());
        var assembly = typeof(ProgramCompilation).Assembly;
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var names = assembly.GetTypes().SelectMany(static t => t.GetMembers(all).Select(static m => m.Name).Append(t.Name));
        Assert.DoesNotContain(names, static n => n.Contains("NotYetSupported", StringComparison.OrdinalIgnoreCase));

        using var stream = File.OpenRead(assembly.Location);
        using var portable = new PEReader(stream);
        var metadata = portable.GetMetadataReader();
        var strings = new List<string>();
        for (var handle = MetadataTokens.UserStringHandle(1); !handle.IsNil; handle = metadata.GetNextHandle(handle))
            strings.Add(metadata.GetUserString(handle));
        Assert.NotEmpty(strings);
        Assert.DoesNotContain(strings, static text => text.Contains("not yet supported", StringComparison.OrdinalIgnoreCase)
            || text.Contains("NotYetSupported", StringComparison.Ordinal));
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
    [InlineData("x(?:y|(z\\b))*", "")]
    [InlineData("(?=a)(?<!b)[\\q{xy}]\\1(a)", "v")]
    [InlineData("^\\b\\B$(?i:a)(?<n>b)\\k<n>", "iu")]
    [InlineData("\\p{RGI_Emoji}", "v")]
    public void Every_construct_task_2_4a_refused_now_compiles(string pattern, string flags)
    {
        // Task 2.4a's eighteen rows, each construct it answered "not yet supported" alone, and two that combine them.
        // \p{RGI_Emoji} comes last, so that the differential's suite population, which runs a pattern against the
        // literals below it, does not run it against the long patterns as subjects (task 2.4c's fast paths).
        Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out var program));
        Assert.NotNull(program);
    }

    [Fact]
    public void A_class_of_strings_is_lowered_in_full_at_each_of_its_occurrences_in_the_general_lowering()
    {
        // The compile-time expansion the roadmap's record of task 2.4b handed task 2.4c and task 2.5: without the fast
        // paths each occurrence of a class of strings becomes a Choice and a Goto for each string, a Literal for each of its
        // characters, the class of its single characters and nothing shared, so \p{RGI_Emoji}'s 2,760 strings are 16,717
        // instructions, and a second occurrence as many again.
        const string Emoji = "\\p{RGI_Emoji}";
        var strings = ((CharClassNode)RegexParser.Parse(Emoji, RegexFlags.UnicodeSets, UnicodeTables.Instance, out _)).Set.Strings;
        var characters = strings.Sum(static text => text.EnumerateRunes().Count());
        var once = TestProgram.General(Emoji, "v").Code.Length - 1;
        Assert.Equal((2_760, 16_717), (strings.Count, once));
        Assert.Equal((2 * strings.Count) + characters + 1, once);
        Assert.Equal((2 * once) + 1, TestProgram.General(Emoji + Emoji, "v").Code.Length);
    }

    [Fact]
    public void A_class_of_strings_is_two_instructions_at_each_occurrence_and_one_table_its_occurrences_share()
    {
        // Task 2.4c's remedy: the strings are a table of the program's, built once for each distinct class, direction and
        // folding, and each occurrence is its StringClass and its class of single characters. Two occurrences of
        // \p{RGI_Emoji} share one table; read right to left in a look-behind, or under i, they are other tables.
        const string Emoji = "\\p{RGI_Emoji}";
        var once = TestProgram.Compile(Emoji, "v");
        Assert.Equal((3, 1), (once.Code.Length, once.StringTableCount));
        Assert.Equal(2_760, once.Strings(0).Count);
        var twice = TestProgram.Compile(Emoji + Emoji, "v");
        Assert.Equal((5, 1), (twice.Code.Length, twice.StringTableCount));
        Assert.Equal((0, 0), (twice.Code[0].A, twice.Code[2].A));
        Assert.Equal(2, TestProgram.Compile("(?<=" + Emoji + ")" + Emoji, "v").StringTableCount);
        Assert.Equal(2, TestProgram.Compile(Emoji + "(?i:" + Emoji + ")", "v").StringTableCount);
        Assert.Equal(1, TestProgram.Compile("[\\q{ab|cd}x][\\q{ab|cd}y]", "v").StringTableCount);
        Assert.Equal(2, TestProgram.Compile("[\\q{ab|cd}][\\q{ab|ce}]", "v").StringTableCount);

        // A dispatch costs one and the weight of the characters strings begin with; the class's own test follows.
        var table = once.Strings(0);
        Assert.Equal(1 + RegexProgram.ClassWeight(table.KeyCount), once.Code[0].Cost);
        Assert.Equal(RegexOp.Class, once.Code[1].Op);
    }

    [Fact]
    public void A_table_keeps_each_string_under_the_character_a_match_of_it_begins_with_in_the_class_s_order()
    {
        // Longest first, as the parser sorts them: abc, ab and ba begin with a, a and b; under b, ba alone; no string begins
        // with x. Right to left a string begins with its last character: abc under c, ab and ba under b and a.
        static string[] Under(RegexStrings table, int key)
        {
            var found = new System.Collections.Generic.List<string>();
            for (var member = table.FirstMember(key); member < table.Count; member = table.NextMember(member))
                found.Add(string.Concat(table.Text(member).ToArray().Select(char.ConvertFromUtf32)));
            return [.. found];
        }

        var forward = TestProgram.Compile("[\\q{ab|abc|ba}x]", "v").Strings(0);
        Assert.Equal((3, 2), (forward.Count, forward.KeyCount));
        Assert.Equal(["abc", "ab"], Under(forward, 'a'));
        Assert.Equal(["ba"], Under(forward, 'b'));
        Assert.Empty(Under(forward, 'x'));
        var backward = TestProgram.Compile("(?<=[\\q{ab|abc|ba}x])", "v").Strings(0);
        Assert.True(backward.RightToLeft);
        Assert.Equal(["abc"], Under(backward, 'c'));
        Assert.Equal(["ab"], Under(backward, 'b'));
        Assert.Equal(["ba"], Under(backward, 'a'));

        // Folded under i, a table holds each code point's Canonicalize, as the alternation's instructions compare them;
        // a surrogate pair is one code point.
        var folded = TestProgram.Compile("[\\q{AB|\\u{1F600}x}]", "vi").Strings(0);
        Assert.True(folded.Folded);
        Assert.Equal(["ab"], Under(folded, 'a'));
        Assert.Equal(["\U0001F600x"], Under(folded, 0x1F600));
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
        var tree = RegexParser.Parse(Nested(4), RegexFlags.None, data, new RegexNesting(512), out var captures, out var names);
        Assert.Equal(ProgramCompilation.NestingLimitExceeded, ProgramCompiler.Compile(tree, captures, names, RegexFlags.None, data, new RegexNesting(4), out var program));
        Assert.Null(program);
        Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(tree, captures, names, RegexFlags.None, data, new RegexNesting(5), out _));
    }

    [Theory]
    [MemberData(nameof(NestingMaximumTests.AllShapes), MemberType = typeof(NestingMaximumTests))]
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
    /// <summary>The program the library builds of <paramref name="pattern"/> under <paramref name="flags"/>, its fast paths on.</summary>
    internal static RegexProgram Compile(string pattern, string flags = "") => Compile(pattern, flags, fastPaths: true);

    /// <summary>
    /// The program of <paramref name="pattern"/> with the fast paths off (task 2.4c): the general lowering of the program
    /// compiler's remarks, which the tests that read that lowering, its loops' units and its loops' frames and trail cells
    /// read, and which the fast paths' differential compares with.
    /// </summary>
    internal static RegexProgram General(string pattern, string flags = "") => Compile(pattern, flags, fastPaths: false);

    private static RegexProgram Compile(string pattern, string flags, bool fastPaths)
    {
        var answer = ProgramCompiler.Compile(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out var program, fastPaths);
        Assert.True(answer == ProgramCompilation.Compiled, "/" + pattern + "/" + flags + " compiled to " + answer);
        return program!;
    }
}
