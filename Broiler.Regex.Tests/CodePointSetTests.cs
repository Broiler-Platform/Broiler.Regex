using System;
using System.Collections.Generic;
using Broiler.Regex.Ast;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// <c>CodePointSet</c> is immutable after construction (conventions 7 and 10, decision D31): it is normalized when it
/// is built, no operation changes a set it is given, and the list <c>Ranges</c> hands out cannot be changed.
/// </summary>
public class CodePointSetTests
{
    private static (int Lo, int Hi)[] RangesOf(CodePointSet set) => [.. set.Ranges];

    [Fact]
    public void The_list_of_ranges_cannot_be_changed_through_its_interfaces()
    {
        // 0.1.0-preview.4 handed out the set's own List, which a cast changed for every reader of the set.
        var set = CodePointSet.Of('a', 'z');
        var list = Assert.IsAssignableFrom<IList<(int Lo, int Hi)>>(set.Ranges);
        Assert.Throws<NotSupportedException>(() => list[0] = (0, 0));
        Assert.Throws<NotSupportedException>(() => list.Add((0, 0)));
        Assert.Equal([('a', 'z')], RangesOf(set));
    }

    [Fact]
    public void A_union_merges_ranges_that_overlap_or_touch_and_changes_neither_operand()
    {
        var left = CodePointSet.Of(0, 2).Union(CodePointSet.Of(8, 9));
        var middle = CodePointSet.Of(3, 7);
        Assert.Equal([(0, 9)], RangesOf(left.Union(middle)));
        Assert.Equal([(0, 9)], RangesOf(middle.Union(left)));
        Assert.Equal([(0, 2), (8, 9)], RangesOf(left));
        Assert.Equal([(3, 7)], RangesOf(middle));

        Assert.Equal([(0, 9)], RangesOf(CodePointSet.Of(0, 4).Union(CodePointSet.Of(5, 9))));
        Assert.Equal([(0, 4), (6, 9)], RangesOf(CodePointSet.Of(6, 9).Union(CodePointSet.Of(0, 4))));
        Assert.Equal([(0, 12)], RangesOf(CodePointSet.Of(5, 9).Union(CodePointSet.Of(0, 4)).Union(CodePointSet.Of(7, 12))));
    }

    [Fact]
    public void A_range_ending_at_the_largest_integer_does_not_wrap_when_it_is_merged()
    {
        var wide = CodePointSet.Of(0, int.MaxValue);
        Assert.Equal([(0, int.MaxValue)], RangesOf(wide.Union(CodePointSet.Of(5, 6))));
        Assert.Equal([(0, int.MaxValue)], RangesOf(CodePointSet.Of(5, 6).Union(wide)));
    }

    [Fact]
    public void The_algebra_answers_new_sets()
    {
        var letters = CodePointSet.Of('A', 'Z');
        var m = CodePointSet.Of('M', 'M');
        Assert.Equal([('A', 'L'), ('N', 'Z')], RangesOf(letters.Subtract(m)));
        Assert.Equal([('M', 'M')], RangesOf(letters.Intersect(m)));
        Assert.Equal(RangesOf(letters), RangesOf(letters.Complement().Complement()));
        Assert.Equal([('a', 'z')], RangesOf(letters.Map(static cp => cp + 0x20)));
        Assert.Equal([('A', 'Z')], RangesOf(letters));
        Assert.Same(letters, letters.Clone());
        Assert.True(new CodePointSet().IsEmpty);
        Assert.True(CodePointSet.Empty().Complement().Contains(CodePointSet.MaxCodePoint));
    }

    [Fact]
    public void A_range_out_of_order_is_refused()
    {
        Assert.Throws<ArgumentException>(() => CodePointSet.Of('z', 'a'));
    }

    [Fact]
    public void A_class_added_to_one_member_at_a_time_holds_its_members_merged()
    {
        var set = new CharSet();
        set.AddCodePoint('c');
        set.AddCodePoint('a');
        set.AddRange('d', 'f');
        set.AddCodePoint('b');
        set.AddSet(CodePointSet.Of('x', 'z'));
        Assert.Equal([('a', 'f'), ('x', 'z')], RangesOf(set.CodePoints));
        Assert.Throws<RegexSyntaxException>(() => set.AddRange('z', 'a'));
    }
}
