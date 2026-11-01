using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// A compiled pattern is immutable after construction (convention 10, decision D31 of Broiler.VM's JSD-0081): nothing
/// its public members hand out can change it, so one instance answers its concurrent matches as it answers one.
/// </summary>
public class ImmutablePatternTests
{
    [Fact]
    public void A_compiled_patterns_group_names_cannot_be_changed_through_their_members()
    {
        // Until task 2.3's review, GroupNames was the parser's own Dictionary behind IReadOnlyDictionary, and the matcher
        // read the same instance on every match, so a cast cleared the names every later match reports. Since task 2.6 it
        // is RegexGroupNames, whose members answer values and read-only spans: no member or field of it writes, and no
        // interface of it is a collection a cast could reach.
        var type = typeof(RegexGroupNames);
        Assert.Empty(type.GetInterfaces());
        Assert.DoesNotContain(type.GetProperties(), static p => p.SetMethod is { IsPublic: true });
        Assert.All(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), static f => Assert.True(f.IsInitOnly, f.Name + " is not readonly"));
        Assert.All(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), static m => Assert.NotEqual(typeof(void), m.ReturnType));

        var regex = TestRegex.Compile("(?<y>a)");
        Assert.Equal([1], regex.GroupNames.GroupsNamed("y").ToArray());
        Assert.Equal("a", regex.Match("a").NamedGroups["y"].Value);
        Assert.Equal([1], regex.GroupNames.GroupsNamed("y").ToArray());
    }

    [Fact]
    public void A_parsers_group_names_before_a_parse_hold_no_name()
    {
        // The public constructor does not parse, so the parser's names are empty until the internal parse sets them.
        var parser = new Parsing.RegexParser("(?<y>a)", RegexFlags.None, Broiler.Unicode.Tables.UnicodeTables.Instance);
        Assert.Equal(0, parser.GroupNames.Count);
        Assert.True(parser.GroupNames.GroupsNamed("y").IsEmpty);
    }
}
