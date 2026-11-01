using System.Collections.Generic;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// <c>RegexMatch.Empty</c> is every no-match result of the process, so nothing reachable from it may change
/// (convention 7: no static field holding an object whose state can change after construction).
/// </summary>
public class NoMatchResultTests
{
    [Fact]
    public void The_shared_no_match_result_cannot_be_changed_through_its_interfaces()
    {
        // 0.1.0-preview.4 built NamedGroups as a new Dictionary behind IReadOnlyDictionary, which a cast changed for
        // every later no-match of the process. Since task 2.9 of SJL-11.2 the groups are a span of the match's own array,
        // which no caller can write, and a name's group is read through GroupNamed, which answers none for a failed match.
        var regex = TestRegex.Compile("(?<a>a)");
        var empty = regex.Match("b");
        Assert.Same(RegexMatch.Empty, empty);
        Assert.False(empty.Success);
        Assert.True(empty.Groups.IsEmpty);
        Assert.Null(empty.GroupNamed("a"));
        Assert.Same(empty, regex.NextMatch(empty));
        Assert.DoesNotContain(typeof(RegexMatch).GetProperties(), static p => p.CanWrite);
    }
}
