using System;
using System.Linq;
using Broiler.Regex.Engine;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The shape of a pattern's group names since requirement R6 reshaped <c>GroupNames</c> (task 2.6 of SJL-11.2):
/// <see cref="RegexGroupNames"/> gives each name with its groups, in the order of the names' first groups, each group
/// with its name, and the group of a name that participated in a match the offset entry wrote; the string-bearing
/// <see cref="RegexMatch.NamedGroups"/> gives each name its participating group, whose <see cref="RegexGroup.Number"/>
/// says which (bundle <c>sjl-11-001</c>'s check A-6, "a result maps the name to the participating index").
/// </summary>
public class RegexGroupNamesTests
{
    [Fact]
    public void The_names_are_in_the_order_of_their_first_groups_each_with_its_groups_and_each_group_with_its_name()
    {
        var names = TestRegex.Compile("(?<y>a)(?<x>b)(c)(?<z>d)").GroupNames;
        Assert.Equal(3, names.Count);
        Assert.Equal(new[] { "y", "x", "z" }, Enumerable.Range(0, names.Count).Select(names.NameAt));
        Assert.Equal([1], names.GroupsNamed("y").ToArray());
        Assert.Equal([2], names.GroupsNamed("x").ToArray());
        Assert.Equal([4], names.GroupsNamed("z").ToArray());
        Assert.True(names.GroupsNamed("w").IsEmpty);
        Assert.Equal(new string?[] { null, "y", "x", null, "z" }, Enumerable.Range(0, 5).Select(names.NameOfGroup));
    }

    [Fact]
    public void A_pattern_without_a_name_has_none()
    {
        var names = TestRegex.Compile("(a)(b)").GroupNames;
        Assert.Equal(0, names.Count);
        Assert.Equal(new string?[] { null, null, null }, Enumerable.Range(0, 3).Select(names.NameOfGroup));
        Assert.Equal(0, names.ParticipatingGroup("a", [0, 2, 0, 1, 1, 2]));
    }

    [Fact]
    public void A_name_is_decoded_and_compared_by_code_unit()
    {
        var names = TestRegex.Compile(@"(?<\u{1D400}>x)(?<A>y)", "u").GroupNames;
        Assert.Equal(new[] { "\U0001D400", "A" }, Enumerable.Range(0, names.Count).Select(names.NameAt));
        Assert.Equal([1], names.GroupsNamed("\U0001D400").ToArray());
        Assert.True(names.GroupsNamed("a").IsEmpty);
    }

    [Fact]
    public void Positions_and_groups_out_of_range_and_a_null_name_are_broken_preconditions()
    {
        var names = TestRegex.Compile("(?<x>a)(b)").GroupNames;
        Assert.Throws<ArgumentOutOfRangeException>(() => names.NameAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => names.NameAt(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => names.NameOfGroup(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => names.NameOfGroup(3));
        Assert.Throws<ArgumentNullException>(() => names.GroupsNamed(null!).Length);
        Assert.Throws<ArgumentNullException>(() => names.ParticipatingGroup(null!, new int[6]));
        Assert.Throws<ArgumentException>(() => names.ParticipatingGroup("x", new int[5]));
    }

    [Fact]
    public void The_participating_group_is_read_from_the_offset_entry_s_slots()
    {
        var regex = TestRegex.Compile("(?<x>a)|(?<y>b)");
        var slots = new int[regex.SlotCount];
        Assert.Equal(RegexStatus.Match, regex.Match("b", 0, false, slots, null, RegexMatchCeilings.Engines, out _));
        Assert.Equal([0, 1, -1, -1, 0, 1], slots);
        Assert.Equal(0, regex.GroupNames.ParticipatingGroup("x", slots));
        Assert.Equal(2, regex.GroupNames.ParticipatingGroup("y", slots));
        Assert.Equal(0, regex.GroupNames.ParticipatingGroup("z", slots));
    }

    [Fact]
    public void A_string_bearing_match_maps_each_name_to_its_group_and_each_group_knows_its_number()
    {
        var match = TestRegex.Compile("(?<y>a)(?<x>b)?(c)").Match("ac");
        Assert.Equal(new[] { 0, 1, 2, 3 }, match.Groups.Select(static g => g.Number));
        Assert.Equal(new[] { "y", "x" }, match.NamedGroups.Keys);
        Assert.Same(match.Groups[1], match.NamedGroups["y"]);
        Assert.Same(match.Groups[2], match.NamedGroups["x"]);
        Assert.Equal((1, true, "a"), (match.NamedGroups["y"].Number, match.NamedGroups["y"].Success, match.NamedGroups["y"].Value));
        Assert.Equal((2, false, -1), (match.NamedGroups["x"].Number, match.NamedGroups["x"].Success, match.NamedGroups["x"].Index));
        Assert.Equal((3, (string?)null), (match.Groups[3].Number, match.Groups[3].Name));
    }
}

/// <summary>
/// <see cref="RegexGroupNames.ParticipatingGroup"/> reads the caller's slots and allocates nothing, whatever the number
/// of groups a name has (check A-8's convention 11 for the reading an engine makes after each match).
/// </summary>
[Collection(AllocationReading.Collection)]
public class RegexGroupNamesAllocationTests
{
    [Fact]
    public void Reading_the_participating_group_allocates_nothing()
    {
        var regex = TestRegex.Compile(string.Concat(Enumerable.Range(0, 200).Select(static k => "(?<n" + k + ">a)")) + "b");
        var slots = new int[regex.SlotCount];
        Assert.Equal(RegexStatus.Match, regex.Match(new string('a', 200) + "b", 0, false, slots, null, RegexMatchCeilings.Engines, out _));
        var found = 0;
        var bytes = AllocationReading.BytesPerCall(() => found = regex.GroupNames.ParticipatingGroup("n199", slots));
        Assert.Equal(200, found);
        Assert.All(bytes, static b => Assert.Equal(0, b));
    }
}
