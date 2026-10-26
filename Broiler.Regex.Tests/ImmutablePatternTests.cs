using System;
using System.Collections.Generic;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// A compiled pattern is immutable after construction (convention 10, decision D31 of Broiler.VM's JSD-0081): nothing
/// its public members hand out can change it, so one instance answers its concurrent matches as it answers one.
/// </summary>
public class ImmutablePatternTests
{
    [Fact]
    public void A_compiled_patterns_group_names_cannot_be_changed_through_their_interface()
    {
        // Until task 2.3's review, GroupNames was the parser's own Dictionary behind IReadOnlyDictionary, and the matcher
        // read the same instance on every match, so a cast cleared the names every later match reports.
        var regex = TestRegex.Compile("(?<y>a)");
        var names = Assert.IsAssignableFrom<IDictionary<string, int>>(regex.GroupNames);
        Assert.True(names.IsReadOnly);
        Assert.Throws<NotSupportedException>(names.Clear);
        Assert.Throws<NotSupportedException>(() => names["z"] = 2);
        Assert.Throws<NotSupportedException>(() => names.Remove("y"));

        Assert.Equal(1, regex.GroupNames["y"]);
        Assert.Equal("a", regex.Match("a").NamedGroups["y"].Value);
    }

    [Fact]
    public void A_parsers_group_names_cannot_be_changed_through_their_interface()
    {
        var parser = new Parsing.RegexParser("(?<y>a)", RegexFlags.None, Broiler.Unicode.Tables.UnicodeTables.Instance);
        var names = Assert.IsAssignableFrom<IDictionary<string, int>>(parser.GroupNames);
        Assert.True(names.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => names["z"] = 2);
    }
}
