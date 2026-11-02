// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

// The reference arms' common harness (bundle sjl-11-001, docs/evidence/sjl-11-001/reference-arms.md): it reads the
// population scripts/reference-arms/generate_population.py writes, calls one arm per case, and writes the arm's
// answers, one line per case and in the population's order. It is linked into each arm's executable and holds no
// code of either engine; tests/ReferenceArms/README.md states the answers' notation, which the comparison reads.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Broiler.Regex.ReferenceArms;

/// <summary>One case of the population: an acceptance case or an exec at a lastIndex.</summary>
internal sealed record Case(string Id, bool IsExec, string Pattern, string Flags, string Subject, int LastIndex, string Policy)
{
    /// <summary>Whether the policy is P3's ample one, and its nesting limit when it is.</summary>
    internal int? AmpleNesting => Policy.StartsWith("ample:", StringComparison.Ordinal)
        ? int.Parse(Policy.AsSpan(6), NumberStyles.None, CultureInfo.InvariantCulture)
        : null;
}

/// <summary>What an arm answers for one case: the answer the comparison compares, and the charge it records.</summary>
internal readonly record struct Answer(string Text, string Charge)
{
    internal static Answer Of(string text) => new(text, "-");
}

/// <summary>An arm: its compile step (the acceptance answer and what an exec needs) and its exec.</summary>
internal interface IArm
{
    /// <summary>The arm's name in the output files: S, V or J.</summary>
    string Name { get; }

    /// <summary>The package versions and copies the arm calls, recorded in its run file.</summary>
    IReadOnlyDictionary<string, string> Facts { get; }

    /// <summary>Compiles a pattern under its flags; the acceptance answer, and the compiled pattern when accepted.</summary>
    (Answer Answer, object? Compiled) Compile(Case c);

    /// <summary>
    /// RegExpBuiltinExec's matcher call as the arm's engine makes it, from <paramref name="start"/>, which the harness
    /// has read from lastIndex as the specification's steps 2 to 12 do: the arm answers what its engine reports.
    /// </summary>
    Answer Exec(object compiled, Case c, int start, bool sticky);
}

internal static class Harness
{
    /// <summary>The negative control's case, when a control copy of an arm sets it (README.md, "The negative controls").</summary>
    internal static string? ControlCase { get; set; }

    internal static int Run(string[] args, IArm arm)
    {
        if (args.Length == 5 && args[0] == "--worker")
        {
            SetCulture(args[4] == "-" ? null : args[4]);
            return RunWorker(arm, args[1], long.Parse(args[2], CultureInfo.InvariantCulture), long.Parse(args[3], CultureInfo.InvariantCulture));
        }

        var options = Options.Parse(args);
        if (options is null)
        {
            Console.Error.WriteLine("usage: <arm> --population DIR --out DIR [--classes P1,P2] [--culture NAME] [--threads N]");
            return 2;
        }

        SetCulture(options.Culture);

        Directory.CreateDirectory(options.Out);
        var suffix = arm.Name + (options.Culture is { } c ? "." + c : "");
        var counts = new SortedDictionary<string, long>(StringComparer.Ordinal);
        var started = DateTime.UtcNow;
        foreach (var population in options.Classes)
        {
            var input = Path.Combine(options.Population, population + ".tsv.gz");
            if (!File.Exists(input))
                continue;
            var output = Path.Combine(options.Out, population + "." + suffix + ".tsv.gz");
            counts[population] = RunFile(input, output, options.Threads, options.Culture);
        }

        var facts = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["arm"] = arm.Name,
            ["culture"] = options.Culture ?? "invariant",
            ["current-culture"] = CultureInfo.CurrentCulture.Name,
            ["cases"] = counts,
            ["started"] = started.ToString("O", CultureInfo.InvariantCulture),
            ["finished"] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            ["dynamic-code-compiled"] = RuntimeFeature.IsDynamicCodeCompiled,
            ["invariant-globalization"] = AppContext.TryGetSwitch("System.Globalization.Invariant", out var invariant) && invariant,
            ["control-case"] = ControlCase,
            ["calls"] = arm.Facts,
        };
        File.WriteAllText(Path.Combine(options.Out, "run." + suffix + ".json"),
            JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        return 0;
    }

    private static void SetCulture(string? name)
    {
        var culture = name is null ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(name);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    /// <summary>The stack of every worker thread: 16 MiB, the stack Broiler.JS's shell runs a script on (SH/Program.cs:128).</summary>
    internal const int StackBytes = 16 << 20;

    /// <summary>
    /// The longest one case may run before its worker is stopped and the case answered <c>limit:wall-clock</c>
    /// (README.md, "Workers"): a matcher that does not end within it (an arm with no bound on some input) is recorded, as
    /// the rule records a member that throws, and the next case runs in a new worker.
    /// </summary>
    internal static readonly TimeSpan WallClock = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The wall-clock limit of a case whose pattern and flags a stopped case of the same range had: once one exec of a
    /// pattern does not end, the next ones of that pattern are given less time, so that a pattern no arm's own bound
    /// stops costs a range seconds and not hours (README.md, "Workers").
    /// </summary>
    internal static readonly TimeSpan WallClockAfterAStop = TimeSpan.FromSeconds(1);

    /// <summary>
    /// One class's file: the cases in contiguous ranges, one worker process of this arm per range, each answering its
    /// cases in order. A worker that ends while a case runs (a stack overflow in Broiler.Regex 0.1.0-preview.4's recursive
    /// matcher, correspondence row B8) or that runs one case past <see cref="WallClock"/> is stopped; the case is answered
    /// <c>crash:exit-N</c> or <c>limit:wall-clock</c> and a new worker takes the range from the next case.
    /// </summary>
    private static long RunFile(string input, string output, int threads, string? culture)
    {
        long total = 0;
        using (var reader = new StreamReader(new GZipStream(File.OpenRead(input), CompressionMode.Decompress), new UTF8Encoding(false)))
        {
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (line.Length > 0 && line[0] != '#')
                    total++;
            }
        }

        var size = (total + threads - 1) / Math.Max(1, threads);
        var parts = new List<string>();
        var supervisors = new List<Thread>();
        for (var k = 0; k < threads; k++)
        {
            var from = k * size;
            var to = Math.Min(total, (k + 1) * size);
            if (from >= to)
                break;
            var part = output + ".part" + k.ToString(CultureInfo.InvariantCulture);
            parts.Add(part);
            var thread = new Thread(() => SuperviseRange(input, part, from, to, culture));
            supervisors.Add(thread);
            thread.Start();
        }

        foreach (var thread in supervisors)
            thread.Join();

        // Each part is a gzip member; members one after another are one gzip file.
        using (var sink = File.Create(output))
        {
            foreach (var part in parts)
            {
                using (var source = File.OpenRead(part))
                    source.CopyTo(sink);
                File.Delete(part);
            }
        }

        return total;
    }

    private static void SuperviseRange(string input, string part, long from, long to, string? culture)
    {
        using var writer = new StreamWriter(new GZipStream(File.Create(part), CompressionLevel.Fastest), new UTF8Encoding(false)) { NewLine = "\n" };
        var next = from;
        var stoppedKeys = new HashSet<string>(StringComparer.Ordinal);
        while (next < to)
        {
            var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.Ordinal))
                start.ArgumentList.Add(System.Reflection.Assembly.GetEntryAssembly()!.Location);
            foreach (var argument in new[] { "--worker", input, next.ToString(CultureInfo.InvariantCulture), to.ToString(CultureInfo.InvariantCulture), culture ?? "-" })
                start.ArgumentList.Add(argument);
            using var child = System.Diagnostics.Process.Start(start)!;
            child.ErrorDataReceived += static (_, _) => { };
            child.BeginErrorReadLine();
            var inFlight = -1L;
            var inFlightId = "";
            var inFlightKey = "";
            var since = DateTime.UtcNow;
            var gate = new object();
            var reading = new Thread(() =>
            {
                string? reply;
                while ((reply = child.StandardOutput.ReadLine()) is not null)
                {
                    lock (gate)
                    {
                        if (reply.StartsWith("S\t", StringComparison.Ordinal))
                        {
                            var fields = reply.Split('\t');
                            inFlight = long.Parse(fields[1], CultureInfo.InvariantCulture);
                            inFlightId = fields[2];
                            inFlightKey = fields[3];
                            since = DateTime.UtcNow;
                        }
                        else if (reply.StartsWith("A\t", StringComparison.Ordinal))
                        {
                            writer.WriteLine(reply[2..]);
                            next = inFlight + 1;
                            inFlight = -1;
                        }
                    }
                }
            });
            reading.Start();
            var stopped = false;
            while (!child.WaitForExit(100))
            {
                lock (gate)
                {
                    if (inFlight >= 0 && DateTime.UtcNow - since > (stoppedKeys.Contains(inFlightKey) ? WallClockAfterAStop : WallClock))
                    {
                        child.Kill(entireProcessTree: true);
                        stopped = true;
                        break;
                    }
                }
            }

            child.WaitForExit();
            reading.Join();
            lock (gate)
            {
                if (inFlight >= 0)
                {
                    var answer = stopped ? "limit:wall-clock" : "crash:exit-" + child.ExitCode.ToString(CultureInfo.InvariantCulture);
                    if (stopped)
                        stoppedKeys.Add(inFlightKey);
                    writer.WriteLine(inFlightId + "\t" + answer + "\t-");
                    next = inFlight + 1;
                }
                else if (next < to && child.ExitCode != 0)
                {
                    throw new InvalidOperationException("a worker ended with exit " + child.ExitCode + " between cases at " + next);
                }
            }
        }
    }

    /// <summary>The pattern's, flags' and policy's FNV-1a hash, the same in every worker.</summary>
    private static string KeyOf(Case c)
    {
        var hash = 14695981039346656037UL;
        foreach (var ch in c.Pattern + "\u0000" + c.Flags + "\u0000" + c.Policy)
        {
            hash ^= ch;
            hash *= 1099511628211UL;
        }

        return hash.ToString("X16", CultureInfo.InvariantCulture);
    }

    /// <summary>A worker: the cases from index <paramref name="from"/> to <paramref name="to"/>, one at a time on a thread of <see cref="StackBytes"/>.</summary>
    private static int RunWorker(IArm arm, string input, long from, long to)
    {
        var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { NewLine = "\n", AutoFlush = false };
        var thread = new Thread(() =>
        {
            using var reader = new StreamReader(new GZipStream(File.OpenRead(input), CompressionMode.Decompress), new UTF8Encoding(false));
            string? lastKey = null;
            (Answer Answer, object? Compiled) last = default;
            long index = -1;
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (line.Length == 0 || line[0] == '#')
                    continue;
                index++;
                if (index < from)
                    continue;
                if (index >= to)
                    break;
                var c = Parse(line);
                output.WriteLine("S\t" + index.ToString(CultureInfo.InvariantCulture) + "\t" + c.Id + "\t" + KeyOf(c));
                output.Flush();
                var key = c.Pattern + "\u0000" + c.Flags + "\u0000" + c.Policy;
                if (!string.Equals(key, lastKey, StringComparison.Ordinal))
                {
                    last = Guarded(() => arm.Compile(c));
                    lastKey = key;
                }

                var answer = c.IsExec ? ExecCase(arm, c, last) : last.Answer;
                if (ControlCase is not null && string.Equals(c.Id, ControlCase, StringComparison.Ordinal))
                    answer = new Answer(Planted(answer.Text), answer.Charge);
                output.WriteLine("A\t" + c.Id + "\t" + answer.Text + "\t" + answer.Charge);
            }

            output.Flush();
        }, StackBytes);
        thread.Start();
        thread.Join();
        return 0;
    }

    /// <summary>
    /// The negative control's change: the answer itself, not a marker. A match's first capture ends one unit later, a
    /// null is a match of the empty string at 0, an acceptance a refusal, and anything else a null; so the planted
    /// answer differs from the arm's in every notation the comparison and the classifier read.
    /// </summary>
    internal static string Planted(string text)
    {
        if (text.StartsWith("match:", StringComparison.Ordinal))
        {
            var body = text.Substring(6);
            var end = body.IndexOfAny(new[] { ';', '|' });
            var first = end < 0 ? body : body.Substring(0, end);
            var comma = first.IndexOf(',');
            var to = long.Parse(first.Substring(comma + 1), CultureInfo.InvariantCulture);
            return "match:" + first.Substring(0, comma + 1) + (to + 1).ToString(CultureInfo.InvariantCulture) +
                   (end < 0 ? "" : body.Substring(end));
        }

        return text switch
        {
            "null" => "match:0,0",
            "accept" => "refuse:syntax",
            _ => "null",
        };
    }

    private static (Answer Answer, object? Compiled) Guarded(Func<(Answer Answer, object? Compiled)> compile)
    {
        try
        {
            return compile();
        }
        catch (Exception failure)
        {
            // A member that throws where the arm records no answer is recorded by its exception's type (the rule's
            // "a member that throws is copied to throw a recorded exception instead").
            return (Answer.Of("throw:" + failure.GetType().Name), null);
        }
    }

    /// <summary>RegExpBuiltinExec's steps before the matcher (22.2.7.2, steps 2 to 12): lastIndex read under g or y.</summary>
    private static Answer ExecCase(IArm arm, Case c, (Answer Answer, object? Compiled) compiled)
    {
        if (compiled.Compiled is null)
            return compiled.Answer;
        var global = c.Flags.Contains('g', StringComparison.Ordinal);
        var sticky = c.Flags.Contains('y', StringComparison.Ordinal);
        var start = global || sticky ? c.LastIndex : 0;
        if (start > c.Subject.Length)
            return Answer.Of("null");
        try
        {
            return arm.Exec(compiled.Compiled, c, start, sticky);
        }
        catch (Exception failure)
        {
            return Answer.Of("throw:" + failure.GetType().Name);
        }
    }

    internal static Case Parse(string line)
    {
        var f = line.Split('\t');
        if (f.Length != 7)
            throw new InvalidDataException("a population line of " + f.Length + " fields: " + line[..Math.Min(80, line.Length)]);
        var isExec = f[1] == "exec";
        return new Case(f[0], isExec, Decode(f[2]), f[3] == "." ? "" : f[3], isExec ? Decode(f[4]) : "",
            isExec ? int.Parse(f[5], NumberStyles.None, CultureInfo.InvariantCulture) : 0, f[6]);
    }

    /// <summary>The population's string notation (scripts/reference-arms/population_format.py).</summary>
    internal static string Decode(string field)
    {
        if (field == ".")
            return "";
        var builder = new StringBuilder();
        foreach (var segment in field.Split('+'))
        {
            var star = segment.IndexOf('*', StringComparison.Ordinal);
            var body = star < 0 ? segment : segment[..star];
            var repeat = star < 0 ? 1 : int.Parse(segment.AsSpan(star + 1), NumberStyles.None, CultureInfo.InvariantCulture);
            var units = new char[body.Length / 4];
            for (var k = 0; k < units.Length; k++)
                units[k] = (char)int.Parse(body.AsSpan(k * 4, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
            for (var r = 0; r < repeat; r++)
                builder.Append(units);
        }

        return builder.ToString();
    }

    /// <summary>
    /// The answer of a match: <c>match:</c>, then each group's start and end from group 0, <c>-</c> for one that did not
    /// participate, joined by <c>;</c>; then, when the pattern names groups, <c>|</c> and each name in ordinal order with
    /// the number of its participating group, <c>-</c> when none of its groups participated.
    /// </summary>
    internal static string Render(int[] starts, int[] ends, IEnumerable<(string Name, IReadOnlyList<int> Groups)> names)
    {
        var builder = new StringBuilder("match:");
        for (var g = 0; g < starts.Length; g++)
        {
            if (g > 0)
                builder.Append(';');
            if (starts[g] < 0 || ends[g] < 0)
                builder.Append('-');
            else
                builder.Append(starts[g].ToString(CultureInfo.InvariantCulture)).Append(',').Append(ends[g].ToString(CultureInfo.InvariantCulture));
        }

        var ordered = names.OrderBy(static n => n.Name, StringComparer.Ordinal).ToList();
        if (ordered.Count > 0)
        {
            builder.Append('|');
            for (var k = 0; k < ordered.Count; k++)
            {
                if (k > 0)
                    builder.Append(',');
                builder.Append(Escape(ordered[k].Name)).Append('=');
                var participating = ordered[k].Groups.FirstOrDefault(g => g < starts.Length && starts[g] >= 0 && ends[g] >= 0, -1);
                builder.Append(participating < 0 ? "-" : participating.ToString(CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    /// <summary>A group name as the answers write it: every code unit outside printable ASCII, and <c>,=|\</c>, as \uXXXX.</summary>
    internal static string Escape(string text)
    {
        var builder = new StringBuilder();
        foreach (var ch in text)
        {
            if (ch is >= ' ' and <= '~' && ch is not (',' or '=' or '|' or '\\'))
                builder.Append(ch);
            else
                builder.Append("\\u").Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private sealed record Options(string Population, string Out, IReadOnlyList<string> Classes, string? Culture, int Threads)
    {
        internal static Options? Parse(string[] args)
        {
            string? population = null, output = null, culture = null;
            var classes = new[] { "P1", "P2", "P3", "P4", "P5", "P6", "P7" };
            var threads = Math.Max(1, Math.Min(3, Environment.ProcessorCount));
            for (var i = 0; i + 1 < args.Length; i += 2)
            {
                switch (args[i])
                {
                    case "--population": population = args[i + 1]; break;
                    case "--out": output = args[i + 1]; break;
                    case "--classes": classes = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries); break;
                    case "--culture": culture = args[i + 1]; break;
                    case "--threads": threads = int.Parse(args[i + 1], CultureInfo.InvariantCulture); break;
                    default: return null;
                }
            }

            return population is null || output is null || args.Length % 2 != 0 ? null : new Options(population, output, classes, culture, threads);
        }
    }
}
