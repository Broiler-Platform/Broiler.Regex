using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace Broiler.Regex.Fuzz;

/// <summary>
/// The fuzz target's own tests, which <c>dotnet test</c> runs with the rest of the solution: a short session at the
/// declared seed, the determinism its digest rests on, the generator's reach, and the controls of the oracle and of the
/// supervisor. The declared session itself is CI's fuzz job (README.md).
/// </summary>
public class FuzzTargetTests
{
    [Fact]
    public void This_build_tests_the_library_as_shipped()
    {
        // The planted-defect build compiles the library's sources into this assembly; every other build references
        // Broiler.Regex.dll, and that is the one these tests and the CI session judge.
        Assert.NotEqual(typeof(Program).Assembly, typeof(BroilerRegex).Assembly);
        Assert.Equal("Broiler.Regex", typeof(BroilerRegex).Assembly.GetName().Name);
    }

    [Fact]
    public void A_short_session_at_the_declared_seed_finds_nothing()
    {
        var (records, findings, reached) = FuzzSession.InProcess(FuzzSession.DeclaredSeed, 0, 200);
        Assert.Equal(200, records.Count);
        Assert.Empty(findings.Where(static f => KnownFindings.Of(f.Finding) is null).Select(static f => f.Index + " " + f.Finding.Line));
        Assert.True(reached.GetValueOrDefault("compile:ok") > 0 && reached.GetValueOrDefault("compile:syntax") > 0 && reached.GetValueOrDefault("offset:match") > 0);
    }

    [Fact]
    public void A_session_is_a_function_of_its_seed_and_its_range()
    {
        var whole = FuzzSession.Digest(FuzzSession.InProcess(7, 0, 80).Records);
        var again = FuzzSession.Digest(FuzzSession.InProcess(7, 0, 80).Records);
        var split = FuzzSession.Digest(FuzzSession.InProcess(7, 0, 30).Records.Concat(FuzzSession.InProcess(7, 30, 80).Records));
        var other = FuzzSession.Digest(FuzzSession.InProcess(8, 0, 80).Records);
        Assert.Equal(whole, again);
        Assert.Equal(whole, split);
        Assert.NotEqual(whole, other);
    }

    [Fact]
    public void A_case_is_regenerated_from_its_seed_and_index_alone()
    {
        var first = FuzzCase.Generate(FuzzSession.DeclaredSeed, 12_345);
        var second = FuzzCase.Generate(FuzzSession.DeclaredSeed, 12_345);
        Assert.Equal(first.Describe(), second.Describe());
        Assert.NotEqual(first.Describe(), FuzzCase.Generate(FuzzSession.DeclaredSeed, 12_346).Describe());
    }

    [Fact]
    public void The_declared_seed_writes_every_construct_class_within_the_declared_iterations()
    {
        // And the list is the generator's: a construct it writes that the list does not name would go uncounted.
        var wanted = new HashSet<string>(Coverage.Constructs, StringComparer.Ordinal);
        var written = new SortedSet<string>(StringComparer.Ordinal);
        for (long iteration = 0; iteration < FuzzSession.DeclaredIterations && (wanted.Count > 0 || iteration < 2000); iteration++)
        {
            var constructs = FuzzCase.Generate(FuzzSession.DeclaredSeed, iteration).Constructs;
            wanted.ExceptWith(constructs);
            written.UnionWith(constructs);
        }

        Assert.Empty(wanted);
        Assert.Empty(written.Except(Coverage.Constructs));
    }

    [Fact]
    public void An_exception_other_than_the_declared_ones_is_a_finding()
    {
        // The control of the oracle: a meter that throws an exception that is not the session's own, which the library
        // propagates as it must, reaches the oracle as an undeclared exception, as a planted throw in the parser does.
        var found = FuzzCase.Generate(FuzzSession.DeclaredSeed, 0) with
        {
            Pattern = "a+b",
            Flags = RegexFlags.None,
            Subject = "xaab",
            Starts = [0],
            NarrowStart = 0,
            NarrowAnchored = false,
            Narrow = new MatchPolicy(1000, 64, 64, FuzzMeter.ForeignFault),
            Probes = false,
            Escapes = false,
        };
        var outcome = Verdicts.Run(found);
        var finding = Assert.Single(outcome.Findings);
        Assert.Equal("exception", finding.Kind);
        Assert.Equal("offset/narrow@0", finding.Site);
        Assert.StartsWith("System.InvalidOperationException", finding.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void A_case_that_runs_clean_has_no_finding_and_a_record()
    {
        var clean = FuzzCase.Generate(FuzzSession.DeclaredSeed, 0) with
        {
            Pattern = "(?<n>a)+b|\\k<n>",
            Flags = RegexFlags.Unicode,
            Subject = "xaab\U0001F600",
            Starts = [0, 5, 6],
            NarrowStart = 0,
            Probes = true,
            Escapes = true,
        };
        var outcome = Verdicts.Run(clean);
        Assert.Empty(outcome.Findings.Select(static f => f.Line));
        Assert.Contains("full=ok:1:4", outcome.Record.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Each_known_finding_still_reproduces()
    {
        // A row of known-findings.tsv names a defect of the library that is not fixed: once its reproduction stops
        // failing, the row is removed in the change that fixed it.
        Assert.All(KnownFindings.All, known =>
        {
            var reproduction = FuzzCase.Generate(FuzzSession.DeclaredSeed, 0) with
            {
                Pattern = known.Pattern,
                Flags = known.Flags,
                Subject = "",
                Starts = [0],
                NarrowStart = 0,
                Probes = false,
                Escapes = false,
            };
            Assert.Contains(Verdicts.Run(reproduction).Findings, known.Matches);
        });
    }

    [Fact]
    public void A_known_finding_absorbs_its_own_exception_alone()
    {
        // The control of the list: another exception of the listed type from the listed method, or from a method it
        // calls, is a finding, so that a row cannot hide a new defect on the same path while the old one still fires.
        var known = new KnownFinding("exception", "System.ArgumentException", "Broiler.Regex.Parsing.RegexParser.ParseClassStringDisjunction", "surrogate char without a", "", RegexFlags.None, "");
        const string Listed = "System.ArgumentException: Found a high surrogate char without a following low surrogate at index: 0. (Parameter 's')";
        const string Frames = " at Broiler.Regex.Parsing.RegexParser.ParseClassStringDisjunction (RegexParser.cs:1197) < Broiler.Regex.Parsing.RegexParser.ParseClassSetOperand (RegexParser.cs:1124)";
        Assert.True(known.Matches(new Finding("exception", "compile/full", Listed + Frames)));
        Assert.False(known.Matches(new Finding("exception", "compile/full", "System.ArgumentException: another defect on the same path" + Frames)));
        Assert.False(known.Matches(new Finding("exception", "compile/full", Listed + " at Broiler.Regex.Parsing.ClassStrings.Add (ClassStrings.cs:40) <" + Frames[3..])));
        Assert.False(known.Matches(new Finding("exception", "compile/full", "System.ArgumentOutOfRangeException: Found a high surrogate char without a following low surrogate" + Frames)));
        Assert.False(known.Matches(new Finding("disagreement", "compile/full", Listed + Frames)));
        Assert.False(known.Matches(new Finding("exception", "compile/full", Listed)));
    }

    [Fact]
    public void A_process_that_ends_is_a_finding_at_the_iteration_it_ended_in()
    {
        var log = new StringWriter(CultureInfo.InvariantCulture);
        var exit = Supervisor.Run(Options(from: 0, iterations: 8, new FuzzSession.Controls(5, -1), watchdog: 120), log);
        var text = log.ToString();
        Assert.Equal(1, exit);
        Assert.Contains("FINDING at iteration 5 (seed 20261027): process-end at session", text, StringComparison.Ordinal);
        Assert.Contains("regex-fuzz: 7 iterations completed", text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_process_that_stops_answering_is_a_finding_at_the_iteration_it_stopped_in()
    {
        var log = new StringWriter(CultureInfo.InvariantCulture);
        var exit = Supervisor.Run(Options(from: 0, iterations: 5, new FuzzSession.Controls(-1, 2), watchdog: 5), log);
        var text = log.ToString();
        Assert.Equal(1, exit);
        Assert.Contains("FINDING at iteration 2 (seed 20261027): hang at session", text, StringComparison.Ordinal);
        Assert.Contains("regex-fuzz: 4 iterations completed", text, StringComparison.Ordinal);
    }

    private static SessionOptions Options(long from, long iterations, FuzzSession.Controls controls, int watchdog) =>
        new(FuzzSession.DeclaredSeed, from, iterations, 1, watchdog, 20, null, null, null, false, false, controls);
}
