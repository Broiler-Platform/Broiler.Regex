using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The adversarial corpus's rows within task 2.4a's subset (<c>tests/Adversarial/corpus.tsv</c>; classes K, A, W, G
/// without anchors, M, F, N and the ordinary rows), each run through the new backend's entry with the row's policy in a
/// child process of its own, the probe under its hidden argument <c>--engine-row</c>, on a thread of the corpus's declared
/// stack: a crash is observed as the child's exit, not suffered by the test host, and each row answers its declared
/// status. Every other row is named with the reason it waits. Task 2.14's <c>--adversarial</c> mode later runs the whole
/// corpus with both controls of the rule's part C.
/// </summary>
public class AdversarialCorpusTests
{
    /// <summary>The rows whose declared status the new backend answers in task 2.4a.</summary>
    private static readonly string[] RowIds =
    [
        "K01", "K02", "K03", "K04", "A01", "A02", "W01", "W02", "G01", "G02", "M01", "M02",
        "N01", "N02", "N04", "N05", "N07", "N08", "N10", "N11", "N13", "N14", "N16", "N17", "F03", "F04",
        "O01", "O04", "O09", "O10", "O11", "O12", "O13", "O14", "O15", "O18",
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
        ["K05"] = "anchors ^ and $: task 2.4b",
        ["L01"] = "look-ahead: task 2.4b", ["L02"] = "look-ahead: task 2.4b", ["L03"] = "look-ahead: task 2.4b",
        ["L04"] = "lookbehind: task 2.4b", ["L05"] = "lookbehind: task 2.4b",
        ["G03"] = "anchors ^ and $: task 2.4b",
        ["N03"] = "the validate entry: task 2.8", ["N06"] = "the validate entry: task 2.8", ["N09"] = "the validate entry: task 2.8",
        ["N12"] = "the validate entry: task 2.8", ["N15"] = "the validate entry: task 2.8", ["N18"] = "the validate entry: task 2.8",
        ["F01"] = "the assertion \\B: task 2.4b", ["F02"] = "the assertion \\B: task 2.4b",
        ["V01"] = "the compile budget: task 2.5", ["V02"] = "the compile budget: task 2.5", ["V03"] = "the compile budget: task 2.5",
        ["V04"] = "the compile budget: task 2.5", ["V05"] = "the compile budget: task 2.5",
        ["O02"] = "look-ahead and anchors: task 2.4b", ["O03"] = "lookbehind and anchors: task 2.4b",
        ["O05"] = "a greedy run of [a] over 10,000,001 characters: the run instruction of task 2.4c (the corpus's presumption 3); the loop form passes the frame ceiling",
        ["O06"] = "anchors ^ and $: task 2.4b; and the run instruction of task 2.4c",
        ["O07"] = "a group name repeated across alternatives: task 2.6",
        ["O08"] = "a back-reference: task 2.4b",
        ["O16"] = "case-insensitive matching: task 2.4b",
        ["O17"] = "a v-mode class of strings: task 2.4b",
        ["O19"] = "the validate entry: task 2.8",
    };

    [Theory]
    [MemberData(nameof(Rows))]
    public void A_row_in_the_subset_answers_its_declared_status_in_a_child_process(string id)
    {
        var declared = Corpus().Single(row => row[0] == id)[7];
        var (exit, output, error) = RunProbe("--engine-row", CorpusPath, id);
        Assert.True(exit == 0, $"the probe for {id} exited {exit}: {error}");
        Assert.Equal(Expand(id + " " + declared), Expand(output.TrimEnd('\n')));
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
