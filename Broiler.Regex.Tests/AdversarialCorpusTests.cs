using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Broiler.Regex.Compiling;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The adversarial corpus's rows the new backend's entry answers (<c>tests/Adversarial/corpus.tsv</c>): task 2.4a's
/// (classes K, A, W, G without anchors, M, F without assertions, N and the ordinary rows) and task 2.4b's (the anchored K05
/// and G03, the look-around class L, F01 and F02's <c>\B</c>, and the ordinary rows of look-arounds, case-insensitive
/// classes and a <c>v</c>-mode class of strings), and task 2.4c's O05 and O06, the greedy run of <c>[a]</c> over
/// 10,000,001 characters that the run instruction matches with one frame (the corpus's presumption 3), where the loop it
/// replaces passed the frame ceiling, and task 2.5's class V, fifty property escapes in one class under <c>v</c>,
/// <c>vi</c> and <c>iu</c>, each refused at its compile budget (requirement R2; check A-2's second half), and task 2.6's
/// O07 and O08, a group name two alternatives share and a reference to it (requirement R6); each run through
/// the new backend's entry with the row's policy in a child process of its own, the probe under its hidden argument
/// <c>--engine-row</c>, on a thread of the corpus's declared stack: a crash is observed as the child's exit, not suffered
/// by the test host, and each row answers its declared status. Every other row is named with the reason it waits. Task
/// 2.14's <c>--adversarial</c> mode later runs the whole corpus with both controls of the rule's part C.
/// </summary>
public class AdversarialCorpusTests
{
    /// <summary>The rows whose declared status the new backend answers: task 2.4a's, then task 2.4b's, 2.4c's, 2.5's and 2.6's.</summary>
    private static readonly string[] RowIds =
    [
        "K01", "K02", "K03", "K04", "A01", "A02", "W01", "W02", "G01", "G02", "M01", "M02",
        "N01", "N02", "N04", "N05", "N07", "N08", "N10", "N11", "N13", "N14", "N16", "N17", "F03", "F04",
        "O01", "O04", "O09", "O10", "O11", "O12", "O13", "O14", "O15", "O18",
        "K05", "G03", "L01", "L02", "L03", "L04", "L05", "F01", "F02", "O02", "O03", "O16", "O17",
        "O05", "O06",
        "V01", "V02", "V03", "V04", "V05",
        "O07", "O08",
    ];

    public static TheoryData<string> Rows
    {
        get
        {
            var rows = new TheoryData<string>();
            foreach (var id in RowIds)
                rows.Add(id);
            return rows;
        }
    }

    /// <summary>The rows that wait for a later task, and why.</summary>
    private static readonly Dictionary<string, string> Waiting = new(StringComparer.Ordinal)
    {
        ["N03"] = "the validate entry: task 2.8", ["N06"] = "the validate entry: task 2.8", ["N09"] = "the validate entry: task 2.8",
        ["N12"] = "the validate entry: task 2.8", ["N15"] = "the validate entry: task 2.8", ["N18"] = "the validate entry: task 2.8",
        ["O19"] = "the validate entry: task 2.8",
    };

    [Theory]
    [MemberData(nameof(Rows))]
    public void A_row_the_new_backend_runs_answers_its_declared_status_in_a_child_process(string id)
    {
        var declared = Corpus().Single(row => row[0] == id)[7];
        var (exit, output, error) = RunProbe("--engine-row", CorpusPath, id);
        Assert.True(exit == 0, $"the probe for {id} exited {exit}: {error}");
        Assert.Equal(Expand(id + " " + declared), Expand(output.TrimEnd('\n')));
    }

    [Fact]
    public void Each_class_V_row_s_expansion_charges_at_least_a_hundred_times_its_budget()
    {
        // The corpus's margin for class V (tests/Adversarial/README.md, section 3): the units R2's charge counts, the ranges
        // and strings the expansion touches at least, are at least a hundred times the row's compile budget, and the budget
        // is larger than the pattern's length, so that a charge of a unit per pattern character alone would not refuse it.
        // Here the whole compilation, with no budget, is charged: at least a hundred times the budget, for every V row.
        var rows = Corpus().Where(static row => row[1] == "V").ToArray();
        Assert.Equal(5, rows.Length);
        foreach (var row in rows)
        {
            var pattern = Expression(row[3]);
            var budget = ulong.Parse(row[6].Split(' ').Single(static key => key.StartsWith("compile=", StringComparison.Ordinal))["compile=".Length..], CultureInfo.InvariantCulture);
            Assert.True(budget > (ulong)pattern.Length, row[0] + ": a budget of " + budget + " for " + pattern.Length + " code units");

            var charge = new CompileCharge(null, null);
            Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(pattern, RegexFlagsParser.Parse(row[4]), UnicodeTables.Instance, 512, out _, charge: charge));
            Assert.True(charge.Spent >= 100 * budget, row[0] + ": " + charge.Spent + " units for a budget of " + budget);
        }
    }

    /// <summary>An expression of the corpus (its README, section 1): JSON string literals, each optionally followed by <c>* n</c>, joined by <c>+</c>.</summary>
    private static string Expression(string text)
    {
        var built = new StringBuilder();
        foreach (var term in text.Split(" + "))
        {
            var star = term.LastIndexOf("\" * ", StringComparison.Ordinal);
            var literal = star < 0 ? term : term[..(star + 1)];
            var count = star < 0 ? 1 : int.Parse(term[(star + 4)..], CultureInfo.InvariantCulture);
            var value = System.Text.Json.JsonSerializer.Deserialize<string>(literal)!;
            for (var i = 0; i < count; i++)
                built.Append(value);
        }

        return built.ToString();
    }

    [Fact]
    public void Every_row_is_run_or_named_with_the_task_it_waits_for()
    {
        var ids = Corpus().Select(static row => row[0]).ToArray();
        var run = RowIds;
        Assert.Empty(ids.Except(run).Except(Waiting.Keys));
        Assert.Empty(run.Intersect(Waiting.Keys));
        Assert.Empty(run.Concat(Waiting.Keys).Except(ids));
    }

    /// <summary>A status with every <c>x^k</c> written out as <c>k</c> copies of <c>x</c>.</summary>
    private static string Expand(string status) =>
        string.Join(' ', status.Split(' ').SelectMany(static token =>
        {
            var caret = token.LastIndexOf('^');
            return caret > 0 && int.TryParse(token.AsSpan(caret + 1), out var count)
                ? Enumerable.Repeat(token[..caret], count)
                : [token];
        }));

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Broiler.Regex.slnx")))
                directory = directory.Parent;
            Assert.True(directory != null, "no Broiler.Regex.slnx above " + AppContext.BaseDirectory);
            return directory!.FullName;
        }
    }

    private static string CorpusPath => Path.Combine(Root, "tests", "Adversarial", "corpus.tsv");

    /// <summary>The corpus's rows, each split at its tabs.</summary>
    private static IEnumerable<string[]> Corpus() =>
        File.ReadAllLines(CorpusPath).Where(static line => !line.StartsWith('#') && !line.StartsWith("id\t", StringComparison.Ordinal) && line.Length > 0)
            .Select(static line => line.Split('\t'));

    /// <summary>
    /// The probe of this build's configuration (built first: the test project references it without its assembly), run
    /// by the host that runs this test.
    /// </summary>
    private static (int Exit, string Output, string Error) RunProbe(params string[] arguments)
    {
        var output = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        var probe = Path.Combine(Root, "probes", "Regex.Probe", "bin", output.Parent!.Name, output.Name, "Broiler.Regex.Probe.dll");
        Assert.True(File.Exists(probe), "the probe is not built at " + probe);

        var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } path ? path : "dotnet";
        var start = new ProcessStartInfo(host)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(probe);
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var text = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(TimeSpan.FromMinutes(5)))
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("the probe ran past five minutes: " + string.Join(' ', arguments));
        }

        return (process.ExitCode, text, error.Result);
    }
}
