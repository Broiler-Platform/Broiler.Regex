using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace Broiler.Regex.Architecture.Tests;

/// <summary>One place in the source where a listed violation is.</summary>
internal sealed record ViolationSite(string Path, int Line);

/// <summary>
/// One row of <c>starting-violations.tsv</c>: the rule or check that finds the violation, the violation exactly as
/// it words it, a token that the source holds where the violation is, and those places.
/// </summary>
internal sealed record StartingViolation(string Rule, string Violation, string Token, IReadOnlyList<ViolationSite> Sites);

/// <summary>
/// The starting violation list: what the rules and checks find in the library on the day they were filed, which
/// SJL-11.2 shrinks to none (bundle sjl-11-001's check E-3). Each rule's test asserts its own rows in both
/// directions through <see cref="AssertExactly"/>: a violation the rule finds that the list does not hold fails
/// the test, and so does a row the rule no longer finds. A change that removes a violation removes its row in
/// the same change, and nothing else may add one.
/// </summary>
internal static class StartingViolations
{
    internal const string FileName = "starting-violations.tsv";

    internal static string FilePath => Path.Combine(SharedTree.TestProjectDirectory, FileName);

    internal static IReadOnlyList<StartingViolation> All { get; } = Load(File.ReadAllLines(FilePath));

    /// <summary>The rows of rule or check <paramref name="rule"/>.</summary>
    internal static IReadOnlyList<string> For(string rule) =>
        All.Where(v => v.Rule == rule).Select(static v => v.Violation).ToList();

    /// <summary>
    /// Asserts that <paramref name="found"/> is exactly the list's rows for <paramref name="rule"/>, and says, when
    /// it is not, which violations are new and which listed ones are gone.
    /// </summary>
    internal static void AssertExactly(string rule, IEnumerable<string> found)
    {
        var actual = found.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var listed = For(rule).Order(StringComparer.Ordinal).ToList();
        var added = actual.Except(listed, StringComparer.Ordinal).ToList();
        var removed = listed.Except(actual, StringComparer.Ordinal).ToList();
        if (added.Count == 0 && removed.Count == 0)
            return;

        var message = rule + " finds a list other than the starting violation list's (" + FileName + "):";
        if (added.Count > 0)
            message += "\n  new, which no change may add:\n    + " + string.Join("\n    + ", added);
        if (removed.Count > 0)
            message += "\n  listed and no longer found, whose rows leave the list in the change that removed them:\n    - " + string.Join("\n    - ", removed);
        Assert.Fail(message);
    }

    internal static IReadOnlyList<StartingViolation> Load(IEnumerable<string> lines)
    {
        var rows = new List<StartingViolation>();
        var number = 0;
        foreach (var line in lines)
        {
            number++;
            if (line.Length == 0 || line[0] == '#')
                continue;
            var fields = line.Split('\t');
            if (fields.Length != 4)
                throw new FormatException(FileName + ":" + number.ToString(CultureInfo.InvariantCulture) + " has " + fields.Length.ToString(CultureInfo.InvariantCulture) + " fields, not 4: rule, violation, token, sites");
            var sites = fields[3].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(site =>
            {
                var colon = site.LastIndexOf(':');
                return new ViolationSite(site[..colon], int.Parse(site[(colon + 1)..], NumberStyles.None, CultureInfo.InvariantCulture));
            }).ToList();
            rows.Add(new StartingViolation(fields[0], fields[1], fields[2], sites));
        }

        return rows;
    }
}
