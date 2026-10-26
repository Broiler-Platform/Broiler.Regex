using System.Collections.Generic;
using Broiler.Regex.Matching;
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
        // every later no-match of the process.
        var empty = TestRegex.Compile("a").Match("b");
        Assert.Same(RegexMatch.Empty, empty);
        Assert.False(empty.Success);

        var named = Assert.IsAssignableFrom<IDictionary<string, RegexGroup>>(empty.NamedGroups);
        Assert.True(named.IsReadOnly);
        var groups = Assert.IsAssignableFrom<ICollection<RegexGroup>>(empty.Groups);
        Assert.True(groups.IsReadOnly);
        Assert.Empty(empty.NamedGroups);
        Assert.Empty(empty.Groups);
    }
}
