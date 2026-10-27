using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Broiler.Regex.Probe;

/// <summary>
/// The probe's <c>--adversarial corpus [--report path]</c> mode (task 2.14 of SJL-11.2): bundle <c>sjl-11-001</c>'s part C,
/// "the adversarial corpus under JIT and Native AOT", run as its configuration says, "each row in a child process on a
/// thread of the smallest stack the corpus's header declares", with "each child's exit and status line retained".
/// </summary>
/// <remarks>
/// <para>
/// <b>The children.</b> The probe starts itself once per row, in id order and one at a time, with its hidden argument
/// <c>--engine-row corpus id</c> (<see cref="EngineRun.Row"/>): under the JIT as <c>dotnet</c> and the probe's assembly,
/// when this process is <c>dotnet</c>'s, and otherwise as this process's own executable, the Native AOT image or the
/// apphost. The child reads the corpus with <see cref="AdversarialCorpus"/>, builds the row's pattern and subject from
/// their expressions, and runs the row's operation under its policy through the new backend's entry, all on a thread of
/// the header's <c>stack-bytes</c>; it writes <c>id status</c> on its standard output and the units the operation did on
/// its standard error, and exits 0 whatever the row answered. A child that exits otherwise, writes no such line, or runs
/// past <see cref="ChildLimit"/> (it is then stopped) has ended abnormally: a stack overflow, the runtime's fail-fast or
/// an exception the row's run did not answer as a status.
/// </para>
/// <para>
/// <b>The comparison.</b> Each child's status, with the corpus's <c>x^k</c> written out, against the row's declared
/// status: equal, the row gives its declared status; otherwise it differs. A row whose operation is <c>validate</c>
/// waits for task 2.8, the validate entry, and is not run, as long as <see cref="EngineRun.Answer"/> answers that
/// operation <c>unsupported</c>; it is named and counted. When task 2.8 gives <see cref="EngineRun"/> the operation, the
/// rows run with no change here.
/// </para>
/// <para>
/// <b>The report</b> is deterministic: the corpus's SHA-256, its header, one line per row in ordinal order of the ids
/// with the child's exit, its status line and units, and the verdict, and the counts; no time, path or machine, and no
/// text of a child's standard error, which differs between the JIT and Native AOT when a child crashes. So the JIT's
/// report and the Native AOT image's compare byte for byte per platform, part C's comparison. The mode exits 0 when no
/// row differs and no child ended abnormally, 1 otherwise, and 2 on arguments or a corpus it cannot read.
/// </para>
/// <para>
/// <b>The controls</b> of part C are scratch builds of the probe that compile the library's sources into it with one
/// declared line changed (<c>AdversarialControls.targets</c>, <c>-p:RegexAdversarialControl=steps</c> or
/// <c>nesting</c>); this mode is the same in them, and <c>eng/adversarial-controls.py</c> reads their reports.
/// </para>
/// </remarks>
internal static class AdversarialMode
{
    /// <summary>The report's format, written into its first line.</summary>
    internal const string Name = "adversarial-1";

    /// <summary>
    /// The longest a child may run: every row of the corpus answers in well under a second on a hosted runner, so a child
    /// still running after this has hung, and is stopped and reported as ending abnormally.
    /// </summary>
    internal static readonly TimeSpan ChildLimit = TimeSpan.FromSeconds(120);

    /// <summary>The task a <c>validate</c> row waits for while the probe's runner does not run the operation.</summary>
    internal const string ValidateTask = "task 2.8 (the validate entry)";

    /// <summary>A row's verdict.</summary>
    internal enum Verdict
    {
        Given,
        Differs,
        Abnormal,
        Waiting,
    }

    /// <summary>The mode, with the arguments after <c>--adversarial</c>.</summary>
    internal static int Run(ReadOnlySpan<string> args)
    {
        string? corpusPath = null;
        string? reportPath = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--report" && i + 1 < args.Length)
                reportPath = args[++i];
            else if (corpusPath is null && !args[i].StartsWith("--", StringComparison.Ordinal))
                corpusPath = args[i];
            else
                return Usage();
        }

        if (corpusPath is null)
            return Usage();

        byte[] bytes;
        AdversarialCorpus corpus;
        try
        {
            corpus = AdversarialCorpus.Read(corpusPath, out bytes);
        }
        catch (Exception refusal) when (refusal is IOException or UnauthorizedAccessException or FormatException)
        {
            Console.Error.WriteLine("Broiler.Regex.Probe --adversarial: " + refusal.Message);
            return 2;
        }

        var (host, prefix) = Self();
        var validateWaits = EngineRun.Answer("validate", "a", "-", "", EngineRun.ParsePolicy("meter=none steps=none frames=1 trail=1 nesting=512 compile=none"), out _) == "unsupported";

        var report = new StringBuilder();
        report.Append("Broiler.Regex.Probe adversarial corpus ").Append(Name).Append('\n');
        report.Append("corpus sha256 ").Append(Convert.ToHexStringLower(SHA256.HashData(bytes))).Append('\n');
        report.Append("corpus-format 1, stack-bytes ").Append(Decimal(corpus.StackBytes)).Append(", rows ").Append(Decimal(corpus.Rows.Count))
            .Append(", each run in a child process (--engine-row) on a thread of stack-bytes, stopped after ")
            .Append(Decimal((int)ChildLimit.TotalSeconds)).Append(" s\n");

        var verdicts = new List<(string Id, Verdict Verdict)>();
        foreach (var row in corpus.Rows.OrderBy(static row => row.Id, StringComparer.Ordinal))
        {
            report.Append(row.Id).Append(' ').Append(row.Class).Append(' ').Append(row.Op).Append(" declared \"").Append(Escape(row.Status)).Append("\": ");
            if (row.Op == "validate" && validateWaits)
            {
                report.Append("not run: waits for ").Append(ValidateTask).Append('\n');
                verdicts.Add((row.Id, Verdict.Waiting));
                continue;
            }

            var child = RunChild(host, prefix, corpusPath, row.Id);
            var verdict = Judge(row, child, out var line);
            report.Append(child.Exit is { } exit ? "exit " + Decimal(exit) : "no exit within the limit, stopped")
                .Append(line is null ? ", no status line" : ", status line \"" + Escape(line) + "\"")
                .Append(child.Units is { } units ? ", units " + units.ToString(CultureInfo.InvariantCulture) : "")
                .Append(": ").Append(verdict switch
                {
                    Verdict.Given => "gives its declared status",
                    Verdict.Differs => "differs",
                    _ => "ends abnormally",
                })
                .Append('\n');
            verdicts.Add((row.Id, verdict));
        }

        string Ids(Verdict verdict) => string.Join(' ', verdicts.Where(v => v.Verdict == verdict).Select(static v => v.Id));
        int Count(Verdict verdict) => verdicts.Count(v => v.Verdict == verdict);
        var reported = verdicts.Where(static v => v.Verdict is Verdict.Differs or Verdict.Abnormal).Select(static v => v.Id).ToArray();
        report.Append("rows ").Append(Decimal(verdicts.Count)).Append(": ").Append(Decimal(Count(Verdict.Given))).Append(" give their declared status, ")
            .Append(Decimal(Count(Verdict.Differs))).Append(" differ, ").Append(Decimal(Count(Verdict.Abnormal))).Append(" end abnormally, ")
            .Append(Decimal(Count(Verdict.Waiting))).Append(" wait for ").Append(ValidateTask).Append('\n');
        report.Append("waiting: ").Append(Count(Verdict.Waiting) == 0 ? "none" : Ids(Verdict.Waiting)).Append('\n');
        report.Append("reported: ").Append(reported.Length == 0 ? "none" : string.Join(' ', reported)).Append('\n');

        if (reportPath is not null)
            File.WriteAllText(reportPath, report.ToString());
        Console.Out.Write(report.ToString());
        return reported.Length == 0 ? 0 : 1;
    }

    private static int Usage()
    {
        Console.Error.WriteLine("usage: Broiler.Regex.Probe --adversarial corpus.tsv [--report path]");
        return 2;
    }

    /// <summary>
    /// The command that starts this probe again: <c>dotnet</c> and the probe's assembly when this process is
    /// <c>dotnet</c>'s, the JIT's usual host; otherwise this process's executable, which is the Native AOT image or the
    /// JIT's apphost.
    /// </summary>
    private static (string Host, string[] Prefix) Self()
    {
        var self = Environment.ProcessPath ?? throw new InvalidOperationException("The probe cannot find its own executable to start a child process.");
        return string.Equals(Path.GetFileNameWithoutExtension(self), "dotnet", StringComparison.OrdinalIgnoreCase)
            ? (self, [Path.Combine(AppContext.BaseDirectory, "Broiler.Regex.Probe.dll")])
            : (self, []);
    }

    /// <summary>What a child left: its exit code (null when it was stopped at the limit), its standard output and its units.</summary>
    internal readonly record struct Child(int? Exit, string Output, ulong? Units);

    /// <summary>Runs one row in a child process of its own and waits for it, at most <see cref="ChildLimit"/>.</summary>
    private static Child RunChild(string host, string[] prefix, string corpusPath, string id)
    {
        var start = new ProcessStartInfo(host)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
        };
        foreach (var argument in prefix)
            start.ArgumentList.Add(argument);
        start.ArgumentList.Add("--engine-row");
        start.ArgumentList.Add(corpusPath);
        start.ArgumentList.Add(id);

        using var process = Process.Start(start) ?? throw new InvalidOperationException("The probe could not start a child process.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(ChildLimit))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // It ended between the wait and the kill; it is still reported as stopped at the limit.
            }

            process.WaitForExit();
            return new Child(null, output.Result, null);
        }

        process.WaitForExit();
        return new Child(process.ExitCode, output.Result, Units(error.Result));
    }

    /// <summary>The units a child wrote on its standard error, as <c>units n</c> alone on its last line, or null.</summary>
    internal static ulong? Units(string error)
    {
        var lines = error.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.Length == 1 && lines[0].StartsWith("units ", StringComparison.Ordinal)
            && ulong.TryParse(lines[0].AsSpan("units ".Length), NumberStyles.None, CultureInfo.InvariantCulture, out var units)
            ? units
            : null;
    }

    /// <summary>
    /// A row's verdict from its child: abnormal unless the child exited 0 with exactly one line, <c>id status</c>; then
    /// the status, its <c>x^k</c> written out, against the declared one.
    /// </summary>
    internal static Verdict Judge(AdversarialRow row, Child child, out string? line)
    {
        line = child.Output.Length > 0 && child.Output.IndexOf('\n') == child.Output.Length - 1 ? child.Output[..^1] : null;
        if (child.Exit != 0 || line is null || !line.StartsWith(row.Id + " ", StringComparison.Ordinal))
            return Verdict.Abnormal;

        return AdversarialCorpus.Expand(line[(row.Id.Length + 1)..]) == AdversarialCorpus.Expand(row.Status) ? Verdict.Given : Verdict.Differs;
    }

    /// <summary>Printable ASCII but the quote and the backslash as itself; any other code unit as <c>\uXXXX</c>.</summary>
    private static string Escape(string text)
    {
        var escaped = new StringBuilder();
        foreach (var unit in text)
        {
            if (unit is >= ' ' and <= '~' && unit is not '\\' and not '"')
                escaped.Append(unit);
            else
                escaped.Append("\\u").Append(((int)unit).ToString("X4", NumberFormatInfo.InvariantInfo));
        }

        return escaped.ToString();
    }

    private static string Decimal(int value) => value.ToString(NumberFormatInfo.InvariantInfo);
}
