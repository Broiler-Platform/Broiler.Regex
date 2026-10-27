using System.Text;
using Broiler.Unicode.Algorithms;
using Broiler.Unicode.Tables;

namespace Broiler.Regex.Tests;

/// <summary>
/// Compiles a pattern for a test against the Unicode owner's tables, which the library reads through the provider
/// and never carries itself (requirement R11; <c>Broiler.Unicode.Tables</c> is test-only data, decision D11), through
/// the public constructors and <c>Compile</c>, as a caller's would. Since task 2.4c they build on the one backend, the
/// new one, so the backend switch tasks 2.4a and 2.4b kept here, which ran every behaviour test on both, is gone.
/// </summary>
internal static class TestRegex
{
    internal static BroilerRegex Compile(string pattern, string? flags = null) => new(pattern, flags, UnicodeTables.Instance);

    internal static BroilerRegex Compile(string pattern, RegexFlags flags) => new(pattern, flags, UnicodeTables.Instance);

    /// <summary>The public constructor taking flags.</summary>
    internal static BroilerRegex New(string pattern, RegexFlags flags, IUnicodeTables tables) => new(pattern, flags, tables);

    /// <summary>The public constructor taking a flags string.</summary>
    internal static BroilerRegex New(string pattern, string? flags, IUnicodeTables tables) => new(pattern, flags, tables);

    /// <summary><see cref="BroilerRegex.Compile"/> with no compile budget and no meter.</summary>
    internal static RegexStatus TryCompile(string pattern, RegexFlags flags, IUnicodeTables tables, int nestingLimit, out BroilerRegex? regex, out RegexLimit limit) =>
        BroilerRegex.Compile(pattern, flags, tables, nestingLimit, null, null, out regex, out limit);

    /// <summary>
    /// The string-bearing <see cref="BroilerRegex.Match(string, int)"/>'s answer in the form <see cref="MachineTests.Render"/>
    /// gives the offset entry's: <c>match s,e</c> and each group's <c>s,e</c> or <c>-</c>, <c>nomatch</c>, or the
    /// ceiling a stopped match names.
    /// </summary>
    internal static string Answer(BroilerRegex regex, string subject, int start) => Render(regex.Match(subject, start));

    /// <summary>A public match in the form <see cref="MachineTests.Render"/> gives the offset entry's answer.</summary>
    internal static string Render(RegexMatch match)
    {
        if (match.Limit != RegexLimit.None)
            return "LimitExceeded " + match.Limit;
        if (!match.Success)
            return "nomatch";

        var text = new StringBuilder("match ").Append(match.Index).Append(',').Append(match.Index + match.Length);
        for (var group = 1; group < match.Groups.Count; group++)
            text.Append(' ').Append(match.Groups[group].Success ? match.Groups[group].Index + "," + (match.Groups[group].Index + match.Groups[group].Length) : "-");
        return text.ToString();
    }
}
