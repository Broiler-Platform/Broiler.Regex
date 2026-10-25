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
/// directions through <see cref="AssertExactly(string, IEnumerable{Finding})"/>: a violation the rule finds that the
/// list does not hold fails the test, and so does a row the rule no longer finds. A change that removes a violation
/// removes its row in the same change, and nothing else may add one.
/// </summary>
/// <remarks>
/// A violation the rule finds with its places (<see cref="Finding"/>) is asserted place by place as well: each place
/// is cited by exactly one of the row's sites, a line the place spans, and each site lies in a place the rule finds.
/// So a second call of a listed member is refused as a new place, and a citation of a place that no longer holds the
/// violation is refused as stale, although the row's wording is the same. A violation the rule finds without places
/// (<see cref="Finding.Places"/> null) names its one place itself. One the rule reads places for and finds at none
/// (an empty list) is not compared by wording alone: each of its citations is refused, because only a signature, a
/// field or local type or a generic argument still names it, which no instruction does, so no citation can hold it,
/// and the change that removed its last place removes the rest of it.
/// </remarks>
internal static class StartingViolations
{
    internal const string FileName = "starting-violations.tsv";

    internal static string FilePath => Path.Combine(SharedTree.TestProjectDirectory, FileName);

    internal static IReadOnlyList<StartingViolation> All { get; } = Load(File.ReadAllLines(FilePath));

    /// <summary>The rows of rule or check <paramref name="rule"/>.</summary>
    internal static IReadOnlyList<StartingViolation> For(string rule) => All.Where(v => v.Rule == rule).ToList();

    /// <summary>As <see cref="AssertExactly(string, IEnumerable{Finding})"/>, for a rule whose violations name their one place.</summary>
    internal static void AssertExactly(string rule, IEnumerable<string> found) =>
        AssertExactly(rule, found.Select(static v => new Finding(v)));

    /// <summary>
    /// Asserts that <paramref name="found"/> is exactly the list's rows for <paramref name="rule"/>, place by place
    /// where the rule reads places, and says, when it is not, what is new and what is gone.
    /// </summary>
    internal static void AssertExactly(string rule, IEnumerable<Finding> found)
    {
        var differences = Compare(For(rule), found);
        if (differences.Count > 0)
            Assert.Fail(rule + " finds a list other than the starting violation list's (" + FileName + "):" + string.Concat(differences.Select(static d => "\n  " + d)));
    }

    /// <summary>
    /// The differences between the rows <paramref name="listed"/> and the findings, as the lines of a failure
    /// message, grouped by kind; empty when they agree.
    /// </summary>
    internal static IReadOnlyList<string> Compare(IReadOnlyList<StartingViolation> listed, IEnumerable<Finding> found)
    {
        // Each violation's places, or null when every finding of it names its one place itself. A finding with places,
        // even none, makes its violation placed, so a rule that reads places and finds none for a violation it still
        // finds is not compared by wording alone.
        var placesOf = new Dictionary<string, HashSet<SourcePlace>?>(StringComparer.Ordinal);
        foreach (var finding in found)
        {
            placesOf.TryGetValue(finding.Violation, out var places);
            if (places is null && finding.Places is not null)
                places = [];
            places?.UnionWith(finding.Places ?? []);
            placesOf[finding.Violation] = places;
        }

        var rows = listed.ToDictionary(static v => v.Violation, StringComparer.Ordinal);
        var added = placesOf.Keys.Where(v => !rows.ContainsKey(v)).Order(StringComparer.Ordinal).ToList();
        var removed = rows.Keys.Where(v => !placesOf.ContainsKey(v)).Order(StringComparer.Ordinal).ToList();
        var uncited = new List<string>();
        var stale = new List<string>();
        var unplaced = new List<string>();
        foreach (var (violation, places) in placesOf.OrderBy(static p => p.Key, StringComparer.Ordinal))
        {
            if (places is null || !rows.TryGetValue(violation, out var row))
                continue;
            if (places.Count == 0)
            {
                unplaced.AddRange(row.Sites.Select(site => violation + " @ " + site.Path + ":" + site.Line.ToString(CultureInfo.InvariantCulture)));
                continue;
            }

            var citations = places.ToDictionary(static p => p, static _ => 0);
            foreach (var site in row.Sites)
            {
                // The narrowest place that spans the cited line: a lambda's statement inside a longer one is its own.
                var place = places.Where(p => p.Contains(site.Path, site.Line))
                    .OrderBy(static p => p.EndLine - p.StartLine)
                    .FirstOrDefault();
                if (place is null)
                    stale.Add(violation + " @ " + site.Path + ":" + site.Line.ToString(CultureInfo.InvariantCulture) + ", where the rule finds no place of it");
                else if (++citations[place] > 1)
                    stale.Add(violation + " @ " + site.Path + ":" + site.Line.ToString(CultureInfo.InvariantCulture) + ", a second citation of the place " + place);
            }

            uncited.AddRange(citations.Where(static c => c.Value == 0).Select(c => violation + " @ " + c.Key).Order(StringComparer.Ordinal));
        }

        var differences = new List<string>();
        Section(differences, "new, which no change may add:", "+", added);
        Section(differences, "listed and no longer found, whose rows leave the list in the change that removed them:", "-", removed);
        Section(differences, "a place of a listed violation that the list does not cite, which no change may add:", "+", uncited);
        Section(differences, "a cited place that does not hold its violation, whose citation leaves the list in the change that removed it:", "-", stale);
        Section(differences, "listed and found, but at no place the rule reads (only a signature, a field or local type or a generic argument still names it), so no citation holds it: the change that removed its last place removes the rest of it:", "-", unplaced);
        return differences;
    }

    private static void Section(List<string> differences, string heading, string sign, List<string> items)
    {
        if (items.Count == 0)
            return;
        differences.Add(heading);
        differences.AddRange(items.Select(i => "  " + sign + " " + i));
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
