using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Broiler.Regex.Fuzz;

/// <summary>
/// The fuzz target's entry point (README.md beside this file).
/// </summary>
/// <remarks>
/// <code>
/// Regex.Fuzz [--seed n] [--iterations n] [--from n] [--workers n] [--watchdog-seconds n] [--max-findings n]
///            [--stop-when-found text] [--transcript path] [--culture name] [--require-coverage] [--no-replay]
/// Regex.Fuzz --replay --seed n --iteration i [--signature s]
/// Regex.Fuzz --show --seed n --iteration i
/// </code>
/// The first form runs a session in child processes (<see cref="Supervisor"/>) and exits 0 with no finding, 1 with one
/// or more that <c>known-findings.tsv</c> does not list, 3 with none when the session did not reach every construct and
/// outcome it must, 4 with none when a listed finding is no longer reached (both required by default for the declared
/// seed and iterations), and 2 on arguments it does not read. <c>--replay</c> runs one iteration in this
/// process, prints its case and its findings, and reduces the case of each. <c>--show</c> prints a case without running
/// it. <c>--child</c>, <c>--end-process-at</c> and <c>--hang-at</c> are the supervisor's and its controls'.
/// </remarks>
internal static class Program
{
    internal static int Main(string[] args)
    {
        if (args.Length > 0 && (args[0].StartsWith('@') || args[0] is "-automated" or "-list" or "--server" or "--internal-msbuild-node"))
        {
            if (Array.Exists(args, static arg => arg is "--server" or "--internal-msbuild-node"))
                return Xunit.MicrosoftTestingPlatform.TestPlatformTestFramework.RunAsync(args, SelfRegisteredExtensions.AddSelfRegisteredExtensions).GetAwaiter().GetResult();
            return Xunit.Runner.InProc.SystemConsole.ConsoleRunner.Run(args).GetAwaiter().GetResult();
        }

        try
        {
            return Run(Read(args));
        }
        catch (FormatException problem)
        {
            Console.Error.WriteLine("regex-fuzz: " + problem.Message);
            Console.Error.WriteLine("usage: Regex.Fuzz [--seed n] [--iterations n] [--from n] [--workers n] [--watchdog-seconds n] [--max-findings n] [--stop-when-found text] [--transcript path] [--culture name] [--require-coverage] [--no-replay] | --replay --seed n --iteration i | --show --seed n --iteration i");
            return 2;
        }
    }

    private static int Run(Dictionary<string, string?> options)
    {
        var seed = Number(options, "--seed", FuzzSession.DeclaredSeed);
        if (options.TryGetValue("--culture", out var culture) && culture is not null)
        {
            // The session runs on a thread of its own, which takes the default culture, not this thread's.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.DefaultThreadCurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture;
            CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentCulture;
        }

        if (options.ContainsKey("--child"))
        {
            using var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = false };
            FuzzSession.Run(
                seed,
                (long)Number(options, "--from", 0),
                (long)Number(options, "--to", 0),
                output,
                new FuzzSession.Controls(Signed(options, "--end-process-at"), Signed(options, "--hang-at")));
            return 0;
        }

        if (options.ContainsKey("--show"))
        {
            Console.Out.WriteLine(FuzzCase.Generate(seed, (long)Number(options, "--iteration", 0)).Describe());
            return 0;
        }

        if (options.ContainsKey("--replay"))
            return Replay(seed, (long)Number(options, "--iteration", 0), options.GetValueOrDefault("--signature"));

        var iterations = (long)Number(options, "--iterations", (ulong)FuzzSession.DeclaredIterations);
        var declared = seed == FuzzSession.DeclaredSeed && iterations >= FuzzSession.DeclaredIterations && Number(options, "--from", 0) == 0;
        var session = new SessionOptions(
            seed,
            (long)Number(options, "--from", 0),
            iterations,
            (int)Number(options, "--workers", (ulong)Math.Clamp(Environment.ProcessorCount, 1, 4)),
            (int)Number(options, "--watchdog-seconds", 120),
            (int)Number(options, "--max-findings", 20),
            options.GetValueOrDefault("--stop-when-found"),
            options.GetValueOrDefault("--transcript"),
            culture,
            declared || options.ContainsKey("--require-coverage"),
            !options.ContainsKey("--no-replay"),
            new FuzzSession.Controls(Signed(options, "--end-process-at"), Signed(options, "--hang-at")));
        return Supervisor.Run(session, Console.Out);
    }

    /// <summary>One iteration in this process: its case, its findings, and each finding's reduction.</summary>
    private static int Replay(ulong seed, long iteration, string? signature)
    {
        var found = FuzzCase.Generate(seed, iteration);
        Console.Out.WriteLine("case: " + found.Describe());
        var outcome = Verdicts.Run(found);
        Console.Out.WriteLine("record: " + outcome.Record);
        if (outcome.Findings.Count == 0)
        {
            Console.Out.WriteLine("no finding in this iteration");
            return 0;
        }

        foreach (var finding in outcome.Findings)
            Console.Out.WriteLine("finding: " + finding.Kind + " at " + finding.Site + ": " + finding.Detail);
        foreach (var target in outcome.Findings.Select(static f => f.Signature).Distinct().Where(s => signature is null || s == signature))
        {
            var reduced = Reducer.Reduce(found, target);
            var again = Verdicts.Run(reduced).Findings.First(f => f.Signature == target);
            Console.Out.WriteLine("reduced (" + target + "): " + reduced.Describe());
            Console.Out.WriteLine("reduced finding: " + again.Kind + " at " + again.Site + ": " + again.Detail);
        }

        return 1;
    }

    private static Dictionary<string, string?> Read(string[] args)
    {
        string[] flags = ["--child", "--show", "--replay", "--require-coverage", "--no-replay"];
        string[] valued =
        [
            "--seed", "--iterations", "--from", "--to", "--workers", "--watchdog-seconds", "--max-findings", "--stop-when-found",
            "--transcript", "--culture", "--iteration", "--signature", "--end-process-at", "--hang-at",
        ];
        var options = new Dictionary<string, string?>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            if (flags.Contains(args[i]))
                options[args[i]] = null;
            else if (valued.Contains(args[i]) && i + 1 < args.Length)
                options[args[i]] = args[++i];
            else
                throw new FormatException("an argument it does not read: " + args[i]);
        }

        return options;
    }

    private static ulong Number(Dictionary<string, string?> options, string name, ulong fallback) =>
        options.TryGetValue(name, out var text) && text is not null
            ? ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : throw new FormatException(name + " takes a number")
            : fallback;

    private static long Signed(Dictionary<string, string?> options, string name) =>
        options.TryGetValue(name, out var text) && text is not null
            ? long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : throw new FormatException(name + " takes a number")
            : -1;
}
