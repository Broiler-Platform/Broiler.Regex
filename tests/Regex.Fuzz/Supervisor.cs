using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Broiler.Regex.Fuzz;

/// <summary>The options of a supervised session.</summary>
internal sealed record SessionOptions(
    ulong Seed,
    long From,
    long Iterations,
    int Workers,
    int WatchdogSeconds,
    int MaximumFindings,
    string? StopWhenFound,
    string? TranscriptPath,
    string? Culture,
    bool RequireCoverage,
    bool Replay,
    FuzzSession.Controls Controls);

/// <summary>
/// Runs a session in child processes and reads them: the records into the transcript and its digest, the findings into
/// the report, and the end of a child that did not write its last line, or wrote nothing for the watchdog's time, into
/// a finding of its own at the iteration after its last record. That is how "no process end" is judged: a process that
/// ends, by a stack overflow or a fail-fast of the runtime, cannot report itself.
/// </summary>
internal static class Supervisor
{
    private sealed class Shared
    {
        internal readonly object Gate = new();
        internal readonly SortedDictionary<long, string> Records = [];
        internal readonly List<(long Index, Finding Finding)> Findings = [];
        internal readonly SortedDictionary<string, long> Reached = new(StringComparer.Ordinal);
        internal readonly Dictionary<KnownFinding, long> Known = [];
        internal volatile bool Stop;
    }

    internal static int Run(SessionOptions options, TextWriter log)
    {
        var library = typeof(BroilerRegex).Assembly == typeof(Supervisor).Assembly
            ? "the library's sources compiled into Regex.Fuzz with the declared defect planted (-p:RegexFuzzPlant=true)"
            : "Broiler.Regex.dll " + typeof(BroilerRegex).Assembly.GetName().Version;
        log.WriteLine(Invariant($"regex-fuzz: seed {options.Seed}, iterations {options.From} to {options.From + options.Iterations - 1}, {options.Workers} worker processes, stack {FuzzSession.StackBytes} bytes, watchdog {options.WatchdogSeconds} s"));
        log.WriteLine("regex-fuzz: library under test: " + library);
        var clock = Stopwatch.StartNew();
        var shared = new Shared();
        var chunk = (options.Iterations + options.Workers - 1) / options.Workers;
        var workers = Enumerable.Range(0, options.Workers)
            .Select(w => (From: options.From + (w * chunk), To: Math.Min(options.From + options.Iterations, options.From + ((w + 1) * chunk))))
            .Where(static range => range.From < range.To)
            .Select(range => Task.Run(() => Work(options, range.From, range.To, shared, log)))
            .ToArray();
        Task.WaitAll(workers);
        clock.Stop();

        var findings = shared.Findings.OrderBy(static f => f.Index).ThenBy(static f => f.Finding.Site, StringComparer.Ordinal).ToList();
        var complete = shared.Records.Count == options.Iterations && !shared.Stop;
        var digest = FuzzSession.Digest(shared.Records.Select(static pair => (pair.Key, pair.Value)));
        if (options.TranscriptPath is { } path)
            File.WriteAllLines(path, shared.Records.Select(static pair => pair.Key.ToString(CultureInfo.InvariantCulture) + "\t" + pair.Value));

        log.WriteLine(Invariant($"regex-fuzz: {shared.Records.Count} iterations completed in {clock.Elapsed.TotalSeconds:F1} s"));
        log.WriteLine("regex-fuzz: reached:");
        foreach (var (key, count) in shared.Reached.Where(static pair => !pair.Key.StartsWith("construct:", StringComparison.Ordinal)))
            log.WriteLine(Invariant($"  {count,9}  {key}"));
        var constructs = shared.Reached.Count(static pair => pair.Key.StartsWith("construct:", StringComparison.Ordinal));
        log.WriteLine(Invariant($"regex-fuzz: {constructs} construct classes reached, of {Coverage.Constructs.Length} the generator can write"));
        var unlisted = shared.Reached.Keys.Where(static key => key.StartsWith("construct:", StringComparison.Ordinal) && !Coverage.Constructs.Contains(key["construct:".Length..])).ToList();
        if (unlisted.Count > 0)
            log.WriteLine("regex-fuzz: reached, but not in the coverage list: " + string.Join(", ", unlisted));
        var missing = Coverage.Missing(shared.Reached).ToList();
        if (missing.Count > 0)
            log.WriteLine("regex-fuzz: not reached: " + string.Join(", ", missing));
        log.WriteLine("regex-fuzz: transcript digest " + digest + (complete ? "" : " (of an incomplete session)"));

        foreach (var known in KnownFindings.All)
            log.WriteLine(Invariant($"regex-fuzz: known finding {known.Key}: {shared.Known.GetValueOrDefault(known)} times; listed in known-findings.tsv: {known.Reported}"));
        var stale = KnownFindings.All.Where(known => !shared.Known.ContainsKey(known)).ToList();

        if (findings.Count > 0)
        {
            log.WriteLine(Invariant($"regex-fuzz: {findings.Count} findings"));
            if (options.Replay)
            {
                foreach (var (index, finding) in findings.Where(static f => f.Finding.Kind is not ("process-end" or "hang")).Take(3))
                    Replay(options, index, finding, log);
            }

            return 1;
        }

        if (options.RequireCoverage && complete && stale.Count > 0)
        {
            log.WriteLine("regex-fuzz: no finding, but the declared session no longer reaches a listed known finding, so its row is stale: " + string.Join("; ", stale.Select(static k => k.Key)) + ". Remove it from known-findings.tsv in the change that fixed it.");
            return 4;
        }

        if (options.RequireCoverage && missing.Count > 0)
        {
            log.WriteLine("regex-fuzz: no finding, but the session did not reach every construct and outcome above, so it may not be read as covering the surface");
            return 3;
        }

        log.WriteLine(Invariant($"regex-fuzz: no finding in {shared.Records.Count} iterations"));
        return 0;
    }

    /// <summary>One worker: children over its range, each restarted after the iteration a process end or a hang was found at.</summary>
    private static void Work(SessionOptions options, long from, long to, Shared shared, TextWriter log)
    {
        var next = from;
        while (next < to && !shared.Stop)
        {
            var arguments = new List<string> { "--child", "--seed", Text(options.Seed), "--from", Text(next), "--to", Text(to) };
            if (options.Culture is { } culture)
                arguments.AddRange(["--culture", culture]);
            if (options.Controls.EndProcessAt >= next)
                arguments.AddRange(["--end-process-at", Text(options.Controls.EndProcessAt)]);
            if (options.Controls.HangAt >= next)
                arguments.AddRange(["--hang-at", Text(options.Controls.HangAt)]);

            using var child = Start(arguments);
            var errors = new StringBuilder();
            child.ErrorDataReceived += (_, data) =>
            {
                if (data.Data is null)
                    return;
                lock (errors)
                {
                    if (errors.Length < 8192)
                        errors.AppendLine(data.Data);
                }
            };
            child.BeginErrorReadLine();

            var last = next - 1;
            var ended = false;
            var silent = false;
            while (true)
            {
                var read = child.StandardOutput.ReadLineAsync();
                if (!read.Wait(TimeSpan.FromSeconds(options.WatchdogSeconds)))
                {
                    silent = true;
                    break;
                }

                if (read.Result is not { } line)
                    break;
                var fields = line.Split('\t');
                switch (fields[0])
                {
                    case "r":
                        last = long.Parse(fields[1], CultureInfo.InvariantCulture);
                        lock (shared.Gate)
                            shared.Records[last] = fields[2];
                        break;
                    case "f":
                    {
                        var index = long.Parse(fields[1], CultureInfo.InvariantCulture);
                        Report(options, shared, index, new Finding(fields[2], fields[3], string.Join('\t', fields.Skip(4))), log);
                        break;
                    }

                    case "c":
                        lock (shared.Gate)
                            shared.Reached[fields[1]] = shared.Reached.GetValueOrDefault(fields[1]) + long.Parse(fields[2], CultureInfo.InvariantCulture);
                        break;
                    case "e":
                        ended = true;
                        break;
                }

                if (shared.Stop)
                    break;
            }

            if (ended)
            {
                child.WaitForExit();
                if (child.ExitCode != 0)
                    Report(options, shared, to, new Finding("process-end", "session", Invariant($"the child exited {child.ExitCode} after its last line")), log);
                return;
            }

            if (shared.Stop && !silent)
            {
                Kill(child);
                return;
            }

            Kill(child);
            string tail;
            lock (errors)
                tail = errors.ToString();
            var at = last + 1;
            var kind = silent ? "hang" : "process-end";
            var detail = silent
                ? Invariant($"no line for {options.WatchdogSeconds} s after iteration {last}; the child was stopped")
                : Invariant($"the process ended with exit code {child.ExitCode} during the iteration; stderr: {Tail(tail)}");
            Report(options, shared, at, new Finding(kind, "session", detail + "; the case: " + FuzzCase.Generate(options.Seed, at).Describe()), log);
            next = at + 1;
        }
    }

    private static void Report(SessionOptions options, Shared shared, long index, Finding finding, TextWriter log)
    {
        lock (shared.Gate)
        {
            if (KnownFindings.Of(finding) is { } known)
            {
                var seen = shared.Known.GetValueOrDefault(known) + 1;
                shared.Known[known] = seen;
                if (seen <= 2)
                    log.WriteLine(Invariant($"regex-fuzz: known finding at iteration {index} (seed {options.Seed}), listed in known-findings.tsv: {finding.Kind} at {finding.Site}: {finding.Detail}"));
                return;
            }

            shared.Findings.Add((index, finding));
            log.WriteLine(Invariant($"regex-fuzz: FINDING at iteration {index} (seed {options.Seed}): {finding.Kind} at {finding.Site}: {finding.Detail}"));
            log.WriteLine(Invariant($"regex-fuzz:   replay: dotnet Regex.Fuzz.dll --replay --seed {options.Seed} --iteration {index}"));
            if (shared.Findings.Count >= options.MaximumFindings || options.StopWhenFound is { } text && finding.Detail.Contains(text, StringComparison.Ordinal))
                shared.Stop = true;
        }
    }

    /// <summary>Replays one finding's iteration in a child, which reduces the case and prints the reduction.</summary>
    private static void Replay(SessionOptions options, long index, Finding finding, TextWriter log)
    {
        log.WriteLine(Invariant($"regex-fuzz: replaying iteration {index} to reduce its {finding.Kind} at {finding.Site}"));
        using var child = Start(["--replay", "--seed", Text(options.Seed), "--iteration", Text(index), "--signature", finding.Signature]);
        child.BeginErrorReadLine();
        var output = child.StandardOutput.ReadToEndAsync();
        if (!child.WaitForExit(TimeSpan.FromSeconds(options.WatchdogSeconds * 2)))
        {
            Kill(child);
            log.WriteLine("regex-fuzz:   the replay did not end within the watchdog's time");
            return;
        }

        foreach (var line in output.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            log.WriteLine("regex-fuzz:   " + line);
    }

    /// <summary>This program again, as a child: through its apphost, or through the host that runs it.</summary>
    internal static Process Start(IEnumerable<string> arguments)
    {
        var self = Path.Combine(AppContext.BaseDirectory, "Regex.Fuzz.dll");
        var processPath = Environment.ProcessPath ?? "dotnet";
        var name = Path.GetFileNameWithoutExtension(processPath);
        var start = name == "Regex.Fuzz"
            ? new ProcessStartInfo(processPath)
            : new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : name == "dotnet" ? processPath : "dotnet");
        if (name != "Regex.Fuzz")
            start.ArgumentList.Add(self);
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.UseShellExecute = false;
        start.StandardOutputEncoding = Encoding.UTF8;
        return Process.Start(start) ?? throw new InvalidOperationException("The child process did not start.");
    }

    private static void Kill(Process child)
    {
        try
        {
            if (!child.HasExited)
                child.Kill(entireProcessTree: true);
            child.WaitForExit();
        }
        catch (InvalidOperationException)
        {
            // It ended on its own between the test and the kill.
        }
    }

    private static string Tail(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" | ", lines.Take(6)).Replace('\r', ' ');
    }

    private static string Text(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Text(ulong value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
