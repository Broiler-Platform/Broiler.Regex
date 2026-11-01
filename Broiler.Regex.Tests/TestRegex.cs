using System.Linq;
using System;
using System.Collections.Generic;
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
        BroilerRegex.Compile(pattern, flags, tables, nestingLimit, null, null, out regex, out limit, out _);

    /// <summary><see cref="BroilerRegex.Compile"/> with no compile budget and no meter, with the refusal's diagnostic.</summary>
    internal static RegexStatus TryCompile(string pattern, RegexFlags flags, IUnicodeTables tables, int nestingLimit, out BroilerRegex? regex, out RegexLimit limit,
        out RegexDiagnostic diagnostic) =>
        BroilerRegex.Compile(pattern, flags, tables, nestingLimit, null, null, out regex, out limit, out diagnostic);

    /// <summary>
    /// The string-bearing <see cref="BroilerRegex.Match(string, int)"/>'s answer in the form <see cref="MachineTests.Render"/>
    /// gives the offset entry's: <c>match s,e</c> and each group's <c>s,e</c> or <c>-</c>, <c>nomatch</c>, or the
    /// ceiling a stopped match names.
    /// </summary>
    internal static string Answer(BroilerRegex regex, string subject, int start) => Render(regex.Match(subject, start));

    /// <summary>
    /// Every match of the global iteration, through <see cref="BroilerRegex.NextMatch"/> from <see cref="BroilerRegex.Match(string, int)"/>
    /// at 0, as <c>BroilerRegex.Matches</c> enumerated them until task 2.9 of SJL-11.2: each match, and a last failed one
    /// when a match reached a ceiling, which names it.
    /// </summary>
    internal static List<RegexMatch> Matches(this BroilerRegex regex, string input)
    {
        var matches = new List<RegexMatch>();
        for (var match = regex.Match(input, 0); ; match = regex.NextMatch(match))
        {
            if (match.Success || match.Limit != RegexLimit.None)
                matches.Add(match);
            if (!match.Success)
                return matches;
        }
    }

    /// <summary>
    /// The names a match's groups carry, each once, in the order of its first group, which is the order
    /// <see cref="BroilerRegex.GroupNames"/> lists them and RegExpBuiltinExec creates the <c>groups</c> object's properties
    /// in (§22.2.7.2, step 34.e): what <c>RegexMatch.NamedKeys()</c> answered until task 2.9 of SJL-11.2. None for a
    /// failed match.
    /// </summary>
    internal static string[] NamedKeys(this RegexMatch match) =>
        [.. match.Groups.ToArray().Select(static g => g.Name).OfType<string>().Distinct(StringComparer.Ordinal)];

    /// <summary>A public match in the form <see cref="MachineTests.Render"/> gives the offset entry's answer.</summary>
    internal static string Render(RegexMatch match)
    {
        if (match.Limit != RegexLimit.None)
            return "LimitExceeded " + match.Limit;
        if (!match.Success)
            return "nomatch";

        var text = new StringBuilder("match ").Append(match.Index).Append(',').Append(match.Index + match.Length);
        for (var group = 1; group < match.Groups.Length; group++)
            text.Append(' ').Append(match.Groups[group].Success ? match.Groups[group].Index + "," + (match.Groups[group].Index + match.Groups[group].Length) : "-");
        return text.ToString();
    }
}
