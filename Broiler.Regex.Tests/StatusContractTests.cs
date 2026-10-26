using System;
using Broiler.Unicode.Algorithms;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The status contract of decision D9 of Broiler.VM's JSD-0081 (conventions 3 and 5), as far as task 2.3 introduced it:
/// a completed operation and a limit exceeded, named and numbered as the Unicode owner's <see cref="UnicodeStatus"/>
/// names and numbers them, so that an engine reading both libraries reads one vocabulary.
/// </summary>
public class StatusContractTests
{
    [Fact]
    public void Every_status_the_Unicode_owner_defines_has_the_same_name_and_value_here()
    {
        foreach (var status in Enum.GetValues<UnicodeStatus>())
            Assert.Equal(Enum.GetName(status), Enum.GetName((RegexStatus)(int)status));
    }

    [Fact]
    public void A_compiled_pattern_answers_success_and_a_refused_one_names_its_limit()
    {
        Assert.Equal("Success", Enum.GetName(TestRegex.TryCompile("a", RegexFlags.None, Broiler.Unicode.Tables.UnicodeTables.Instance, 1, out var regex, out var limit)));
        Assert.NotNull(regex);
        Assert.Equal(RegexLimit.None, limit);

        Assert.Equal(RegexStatus.LimitExceeded, TestRegex.TryCompile("(a)", RegexFlags.None, Broiler.Unicode.Tables.UnicodeTables.Instance, 1, out regex, out limit));
        Assert.Null(regex);
        Assert.Equal(RegexLimit.Nesting, limit);
    }
}
