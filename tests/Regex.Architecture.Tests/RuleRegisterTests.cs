using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Broiler.Regex.Architecture.Tests;

/// <summary>
/// The register and the starting violation list are themselves under test, as Broiler.VM's register is: nothing
/// may drift between the rows, the tests that run, the witnesses on disk and the places the list cites.
/// </summary>
public class RuleRegisterTests
{
    private static readonly JsonElement[] Rules = Load("rules");

    private static readonly JsonElement[] Checks = Load("contractChecks");

    private static IEnumerable<JsonElement> Rows => Rules.Concat(Checks);

    private static readonly string[] Statuses = ["Active", "Vacuous", "Deferred"];

    [Fact]
    public void The_register_holds_SL1_to_SL10_and_the_checks_C1_C3_and_C5_each_Active()
    {
        Assert.Equal(Enumerable.Range(1, 10).Select(static n => "SL" + n), Rules.Select(static r => Field(r, "id")));
        Assert.Equal(["C1", "C3", "C5"], Checks.Select(static r => Field(r, "id")));
        Assert.All(Rows, static r => Assert.Contains(Field(r, "status"), Statuses));
        Assert.All(Rows, static r => Assert.True(Field(r, "status") == "Active", Field(r, "id") + " is not Active"));
        Assert.All(Rows, static r =>
        {
            foreach (var field in (string[])["statement", "evidence", "nonVacuousWhen", "test", "witness"])
                Assert.False(string.IsNullOrWhiteSpace(Field(r, field)), Field(r, "id") + " has no " + field);
        });
        Assert.All(Checks, static r => Assert.False(string.IsNullOrWhiteSpace(Field(r, "convention")), Field(r, "id") + " names no convention"));
    }

    [Fact]
    public void Every_row_names_a_test_that_exists_and_every_rule_test_has_a_row()
    {
        var tests = typeof(RuleRegisterTests).Assembly.GetTypes()
            .SelectMany(static t => t.GetMethods())
            .Where(static m => m.GetCustomAttributes<FactAttribute>(inherit: true).Any())
            .Select(static m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var row in Rows)
        {
            Assert.Contains(Field(row, "test"), tests);
            Assert.StartsWith(Field(row, "id") + "_", Field(row, "test"), StringComparison.Ordinal);
        }

        var ids = Rows.Select(static r => Field(r, "id")).ToHashSet(StringComparer.Ordinal);
        foreach (var test in tests.Where(static t => System.Text.RegularExpressions.Regex.IsMatch(t, "^(SL|C)[0-9]+_")))
            Assert.Contains(test[..test.IndexOf('_', StringComparison.Ordinal)], ids);
    }

    [Fact]
    public void Every_witness_a_row_names_exists_and_every_witness_file_is_named()
    {
        var named = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in Rows)
        {
            foreach (var witness in Field(row, "witness").Split(", "))
            {
                if (witness.StartsWith("witnesses/", StringComparison.Ordinal))
                {
                    Assert.True(File.Exists(Path.Combine(SharedTree.TestProjectDirectory, witness)), witness + " does not exist");
                    Assert.StartsWith(Field(row, "id") + "-", Path.GetFileName(witness), StringComparison.Ordinal);
                    named.Add(Path.GetFileName(witness));
                }
                else
                {
                    // A witness compiled into this assembly, as Broiler.VM's DynamicLoadingWitness is.
                    var type = witness.Split(' ')[0];
                    Assert.Contains(typeof(RuleRegisterTests).Assembly.GetTypes(), t => t.Name == type);
                }
            }
        }

        foreach (var file in Directory.EnumerateFiles(SharedTree.Witnesses))
            Assert.Contains(Path.GetFileName(file), named);
    }

    [Fact]
    public void No_row_carries_a_run_figure_beyond_what_its_rule_names()
    {
        // The register states rules; a bundle retains runs. A count of vectors, files or warnings typed into a row
        // would go stale the day a run changes it, and the starting violation list, not the register, says how
        // many violations there are.
        foreach (var row in Rows)
        {
            var text = Field(row, "statement") + " " + Field(row, "nonVacuousWhen");
            Assert.DoesNotMatch(@"\b\d{2,}\s+(vectors|files|units|warnings|types|members|violations|cases)\b", text);
        }
    }

    /// <summary>
    /// Every row of the starting violation list belongs to a rule or check of the register, is listed once, and
    /// cites at least one place in the source, each of which holds the row's token on the line it names. The
    /// rule's own test decides whether the row is still found; this test keeps the citations true as the code
    /// around them moves.
    /// </summary>
    [Fact]
    public void Every_starting_violation_belongs_to_a_row_and_cites_the_lines_that_hold_it()
    {
        var ids = Rows.Select(static r => Field(r, "id")).ToHashSet(StringComparer.Ordinal);
        var all = StartingViolations.All;
        Assert.Equal(all.Count, all.Select(static v => v.Rule + "\t" + v.Violation).Distinct(StringComparer.Ordinal).Count());
        foreach (var violation in all)
        {
            Assert.Contains(violation.Rule, ids);
            Assert.False(string.IsNullOrWhiteSpace(violation.Token), violation.Violation + " names no token");
            Assert.NotEmpty(violation.Sites);
            foreach (var site in violation.Sites)
            {
                var path = Path.Combine(SharedTree.Root, site.Path.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(path), site.Path + ", cited by " + violation.Rule + ", does not exist");
                var lines = File.ReadAllLines(path);
                var holding = lines.Select((text, index) => (text, index)).Where(l => l.text.Contains(violation.Token, StringComparison.Ordinal)).Select(static l => l.index + 1).ToList();
                Assert.True(
                    site.Line >= 1 && site.Line <= lines.Length && lines[site.Line - 1].Contains(violation.Token, StringComparison.Ordinal),
                    site.Path + ":" + site.Line + ", cited by " + violation.Rule + " for \"" + violation.Violation + "\", does not hold \""
                        + violation.Token + "\"; the file holds it at line " + string.Join(", ", holding));
            }
        }
    }

    /// <summary>
    /// The list is compared place by place for a rule that reads places. The witness calls char.IsDigit twice, on two
    /// lines, and names it once more in a comment where IL names nothing: a row that cites both calls agrees; one that
    /// cites one is refused for the other, a new place of a listed violation; one that cites the comment as well is
    /// refused for a stale citation, although that line holds the token; and one that cites a place twice is refused.
    /// The row's wording is the same in every case, which is what a comparison of wording alone could not see. And a
    /// finding with places, but none, is told from one without places: its row's citations are each refused.
    /// </summary>
    [Fact]
    public void The_list_is_compared_place_by_place_and_a_new_or_a_stale_place_is_refused()
    {
        const string File = "SL4-platform-globalization.cs.witness";
        var witness = Witness.Compile(File);
        Assert.True(witness.HasPlaces);
        var path = Witness.RelativePath(File);
        var lines = Witness.Text(File).Split('\n');
        var holding = lines.Select((text, index) => (text, line: index + 1)).Where(static l => l.text.Contains("char.IsDigit(", StringComparison.Ordinal)).Select(static l => l.line).ToList();
        Assert.Equal(3, holding.Count);
        int comment = holding.Single(l => lines[l - 1].TrimStart().StartsWith("//", StringComparison.Ordinal));
        int[] calls = [.. holding.Where(l => l != comment)];

        var findings = SharedRules.SL4Findings(witness).Where(static f => f.Violation.EndsWith(" calls System.Char.IsDigit(System.Char)", StringComparison.Ordinal)).ToList();
        var violation = Assert.Single(findings).Violation;
        Assert.Equal(calls.Select(l => new SourcePlace(path, l, l)), findings[0].Places!);

        IReadOnlyList<string> Compare(params int[] cited) =>
            StartingViolations.Compare([new StartingViolation("SL4", violation, "char.IsDigit(", [.. cited.Select(l => new ViolationSite(path, l))])], findings);

        Assert.Empty(Compare(calls));
        Assert.Equal(["a place of a listed violation that the list does not cite, which no change may add:", "  + " + violation + " @ " + new SourcePlace(path, calls[1], calls[1])], Compare(calls[0]));
        Assert.Contains("  - " + violation + " @ " + path + ":" + comment + ", where the rule finds no place of it", Compare([.. calls, comment]));
        Assert.Contains(Compare(calls[0], calls[0], calls[1]), static d => d.Contains(", a second citation of the place ", StringComparison.Ordinal));

        // A rule that reads places and finds the violation at none of them, as when the instructions that named it are
        // gone and only a signature, a field or local type or a generic argument still names it, is not compared by
        // wording alone: none of the row's citations holds it, and each is refused.
        Assert.Equal(
            [Unplaced, .. calls.Select(l => "  - " + violation + " @ " + path + ":" + l)],
            StartingViolations.Compare([new StartingViolation("SL4", violation, "char.IsDigit(", [.. calls.Select(l => new ViolationSite(path, l))])], [new Finding(violation, [])]));

        // And a violation the rule finds without places is compared by wording alone, in both directions.
        Assert.Empty(StartingViolations.Compare([new StartingViolation("SL2", "w is mutable", "x", [new ViolationSite(path, 1)])], [new Finding("w is mutable")]));
        Assert.Equal(
            ["new, which no change may add:", "  + v is new", "listed and no longer found, whose rows leave the list in the change that removed them:", "  - w is mutable"],
            StartingViolations.Compare([new StartingViolation("SL2", "w is mutable", "x", [new ViolationSite(path, 1)])], [new Finding("v is new")]));
    }

    /// <summary>
    /// The two limits of a place, which docs/conventions.md and the list's preamble state. A place is a statement, the
    /// lines of one sequence point, so a second call of a listed member inside a statement that already holds one is no
    /// new place. And a place is where an instruction names the member, its declaring type or a type operand, so a type
    /// that only a signature names has none there. The SL4 witness calls char.IsDigit twice in one statement, and names
    /// TimeZoneInfo in a call and, once more, in a parameter alone. A change that reads places more finely fails here,
    /// and the limits' text changes with it.
    /// </summary>
    [Fact]
    public void A_place_is_a_statement_in_which_an_instruction_names_the_violation()
    {
        const string File = "SL4-platform-globalization.cs.witness";
        var witness = Witness.Compile(File);
        var path = Witness.RelativePath(File);
        var lines = Witness.Text(File).Split('\n');
        int LineOf(string text) => 1 + Array.FindIndex(lines, l => l.Contains(text, StringComparison.Ordinal));

        var twice = LineOf("char.IsDigit(text[0]) && char.IsDigit(text[1])");
        Assert.Equal(2, lines[twice - 1].Split("char.IsDigit(").Length - 1);
        Assert.Single(witness.PlacesOf("member:System.Char.IsDigit(System.Char)")!, p => p.Contains(path, twice));

        var call = LineOf("TimeZoneInfo.Local");
        Assert.True(LineOf("TimeZoneInfo? zone") > call);
        Assert.Contains("System.TimeZoneInfo", witness.TypeReferences);
        Assert.Equal([new SourcePlace(path, call, call)], witness.PlacesOf("type:System.TimeZoneInfo")!);
    }

    private const string Unplaced =
        "listed and found, but at no place the rule reads (only a signature, a field or local type or a generic argument still names it), so no citation holds it: the change that removed its last place removes the rest of it:";

    private static string Field(JsonElement row, string name) =>
        row.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static JsonElement[] Load(string section)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(SharedTree.TestProjectDirectory, "rules.register.json")));
        return document.RootElement.GetProperty(section).EnumerateArray().Select(static r => r.Clone()).ToArray();
    }
}
