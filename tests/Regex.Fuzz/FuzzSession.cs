using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Broiler.Regex.Fuzz;

/// <summary>
/// A run of iterations in this process: each case generated, run through every entry and judged, its record and its
/// findings written as lines the supervisor reads (<see cref="Supervisor"/>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The lines.</b> <c>r i record</c> after iteration <c>i</c> completes, flushed at once, so that a process that ends
/// in iteration <c>i + 1</c> is known to have ended there; <c>f i kind site detail</c> for each finding; <c>c key n</c>
/// for each outcome and construct reached, at the end; <c>e</c> last. Fields are separated by tabs.
/// </para>
/// <para>
/// <b>On a thread of the smallest stack the adversarial corpus declares</b>, 1 MiB (<c>tests/Adversarial/corpus.tsv</c>,
/// <c>stack-bytes</c>), so that a pattern whose compilation or match would overflow an engine's smallest stack ends this
/// process too, and the supervisor reports it (risk S2: "a stack overflow on any fuzz input" is a stop).
/// </para>
/// </remarks>
internal static class FuzzSession
{
    /// <summary>The declared seed: the session CI runs, and the one the planted defect is shown found by.</summary>
    internal const ulong DeclaredSeed = 20261027;

    /// <summary>The declared iterations of a CI session.</summary>
    internal const long DeclaredIterations = 24_000;

    /// <summary>The stack every iteration runs on: the adversarial corpus's smallest, 1 MiB.</summary>
    internal const int StackBytes = 1 << 20;

    /// <summary>Hooks a control uses to show the supervisor finds what it claims to: never set by a session.</summary>
    internal readonly record struct Controls(long EndProcessAt, long HangAt)
    {
        internal static Controls None => new(-1, -1);
    }

    /// <summary>Runs iterations <paramref name="from"/> to <paramref name="to"/> (exclusive) and writes their lines.</summary>
    internal static void Run(ulong seed, long from, long to, TextWriter output, Controls controls)
    {
        var reached = new SortedDictionary<string, long>(StringComparer.Ordinal);
        Exception? failure = null;
        var thread = new Thread(
            () =>
            {
                try
                {
                    for (var i = from; i < to; i++)
                    {
                        if (i == controls.EndProcessAt)
                            Environment.FailFast("Regex.Fuzz control: the process ends at iteration " + i.ToString(CultureInfo.InvariantCulture));
                        if (i == controls.HangAt)
                            Thread.Sleep(Timeout.Infinite);
                        var outcome = Verdicts.Run(FuzzCase.Generate(seed, i));
                        Write(output, i, outcome);
                        foreach (var (key, count) in outcome.Reached)
                            reached[key] = reached.GetValueOrDefault(key) + count;
                    }
                }
                catch (Exception caught)
                {
                    failure = caught;
                }
            },
            StackBytes);
        thread.Start();
        thread.Join();
        if (failure is not null)
            throw new InvalidOperationException("The session's own loop failed.", failure);

        foreach (var (key, count) in reached)
            output.Write("c\t" + key + "\t" + count.ToString(CultureInfo.InvariantCulture) + "\n");
        output.Write("e\n");
        output.Flush();
    }

    private static void Write(TextWriter output, long iteration, Outcome outcome)
    {
        var index = iteration.ToString(CultureInfo.InvariantCulture);
        foreach (var finding in outcome.Findings)
            output.Write("f\t" + index + "\t" + finding.Line + "\n");
        output.Write("r\t" + index + "\t" + outcome.Record + "\n");
        output.Flush();
    }

    /// <summary>The digest of a transcript: SHA-256 over each iteration's line, <c>index TAB record LF</c>, in index order.</summary>
    internal static string Digest(IEnumerable<(long Index, string Record)> records)
    {
        using var sha = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        foreach (var (index, record) in records)
            sha.AppendData(Encoding.UTF8.GetBytes(index.ToString(CultureInfo.InvariantCulture) + "\t" + record + "\n"));
        return Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
    }

    /// <summary>
    /// Runs iterations in this process, on the session's stack, and answers their records, findings and reach: what the
    /// tests use, and what <c>--replay</c> uses for one iteration.
    /// </summary>
    internal static (List<(long Index, string Record)> Records, List<(long Index, Finding Finding)> Findings, SortedDictionary<string, long> Reached) InProcess(ulong seed, long from, long to)
    {
        var writer = new StringWriter(CultureInfo.InvariantCulture);
        Run(seed, from, to, writer, Controls.None);
        var records = new List<(long, string)>();
        var findings = new List<(long, Finding)>();
        var reached = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (var line in writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = line.Split('\t');
            switch (fields[0])
            {
                case "r":
                    records.Add((long.Parse(fields[1], CultureInfo.InvariantCulture), fields[2]));
                    break;
                case "f":
                    findings.Add((long.Parse(fields[1], CultureInfo.InvariantCulture), new Finding(fields[2], fields[3], string.Join('\t', fields.Skip(4)))));
                    break;
                case "c":
                    reached[fields[1]] = long.Parse(fields[2], CultureInfo.InvariantCulture);
                    break;
            }
        }

        return (records, findings, reached);
    }
}
