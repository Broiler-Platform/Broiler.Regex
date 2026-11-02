// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

// The fast-path differential over population P4 (tests/ReferenceArms/README.md, "The fast-path differential"): the
// comparison FastPathDifferentialTests makes over the test suite's populations, made over P4. Each exec case of P4 runs
// in both programs under the same ceilings (100,000 units and the engines' frames and trail); a case either program
// stops at the step ceiling runs again in both under 10,000,000 units and is compared there; the status, the ceiling a
// limit names, every slot are compared. It writes a summary and every disagreement.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Broiler.Regex.Compiling;
using Broiler.Regex.Engine;
using Broiler.Unicode.Tables;

namespace Broiler.Regex.ReferenceArms;

internal static class Program
{
    private const ulong StepCeiling = 100_000;
    private const ulong SecondCeiling = 10_000_000;
    private static readonly RegexMatchCeilings First = new(StepCeiling, RegexMatchCeilings.EngineFrames, RegexMatchCeilings.EngineTrail);
    private static readonly RegexMatchCeilings Second = new(SecondCeiling, RegexMatchCeilings.EngineFrames, RegexMatchCeilings.EngineTrail);

    private static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("usage: Regex.ReferenceArms.FastPaths <population>/P4.tsv.gz <out-directory>");
            return 2;
        }

        Directory.CreateDirectory(args[1]);
        var tally = new SortedDictionary<string, long>(StringComparer.Ordinal);
        void Count(string key) => tally[key] = tally.GetValueOrDefault(key) + 1;
        var disagreements = new List<string>();
        string? lastKey = null;
        (RegexProgram Fast, RegexProgram General)? programs = null;
        using var reader = new StreamReader(new GZipStream(File.OpenRead(args[0]), CompressionMode.Decompress), new UTF8Encoding(false));
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0 || line[0] == '#')
                continue;
            var c = Harness.Parse(line);
            var key = c.Pattern + "\u0000" + c.Flags;
            if (!string.Equals(key, lastKey, StringComparison.Ordinal))
            {
                lastKey = key;
                programs = Compile(c.Pattern, c.Flags);
                Count(programs is null ? "patterns not compiled (refused under their flags)" : "patterns compiled twice");
            }

            if (!c.IsExec || programs is not { } both)
                continue;
            var sticky = c.Flags.Contains('y', StringComparison.Ordinal);
            var start = c.Flags.Contains('g', StringComparison.Ordinal) || sticky ? c.LastIndex : 0;
            if (start > c.Subject.Length)
                continue;
            var fast = Run(both.Fast, c.Subject, start, sticky, First);
            var general = Run(both.General, c.Subject, start, sticky, First);
            if (fast.Limit == RegexLimit.Steps || general.Limit == RegexLimit.Steps)
            {
                Count(fast.Limit == RegexLimit.Steps && general.Limit == RegexLimit.Steps ? "stopped at the first ceiling: both"
                    : fast.Limit == RegexLimit.Steps ? "stopped at the first ceiling: with the fast paths alone" : "stopped at the first ceiling: without them alone");
                fast = Run(both.Fast, c.Subject, start, sticky, Second);
                general = Run(both.General, c.Subject, start, sticky, Second);
                if (fast.Limit == RegexLimit.Steps || general.Limit == RegexLimit.Steps)
                {
                    Count("stopped at the second ceiling too: not compared");
                    continue;
                }

                Count("compared under the second ceiling");
            }

            if (fast.Limit is RegexLimit.Frames or RegexLimit.Trail || general.Limit is RegexLimit.Frames or RegexLimit.Trail)
                Count("reached a frame or trail ceiling");
            Count("exec cases compared");
            tally["units with the fast paths"] = tally.GetValueOrDefault("units with the fast paths") + (long)fast.Units;
            tally["units without them"] = tally.GetValueOrDefault("units without them") + (long)general.Units;
            if (!string.Equals(fast.Answer, general.Answer, StringComparison.Ordinal))
                disagreements.Add(c.Id + "\twith " + fast.Answer + "\twithout " + general.Answer);
        }

        tally["disagreements"] = disagreements.Count;
        File.WriteAllLines(Path.Combine(args[1], "fast-path-disagreements.tsv"), disagreements);
        File.WriteAllText(Path.Combine(args[1], "fast-path-summary.json"), JsonSerializer.Serialize(tally, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        foreach (var (name, value) in tally)
            Console.WriteLine(name + ": " + value.ToString(CultureInfo.InvariantCulture));
        return disagreements.Count == 0 ? 0 : 1;
    }

    private static (RegexProgram, RegexProgram)? Compile(string pattern, string flags)
    {
        try
        {
            if (Text.RegexFlagsText.Parse(flags, out var parsed, out _) != RegexStatus.Success)
                return null;
            if (ProgramCompiler.Compile(pattern, parsed, UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out var fast) != ProgramCompilation.Compiled
                || ProgramCompiler.Compile(pattern, parsed, UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out var general, fastPaths: false) != ProgramCompilation.Compiled)
                return null;
            return (fast!, general!);
        }
        catch (RegexSyntaxException)
        {
            return null;
        }
    }

    private static (string Answer, RegexLimit Limit, ulong Units) Run(RegexProgram program, string subject, int start, bool anchored, RegexMatchCeilings ceilings)
    {
        var slots = new int[program.SlotCount];
        var status = program.Match(subject, start, anchored, slots, null, ceilings, out var limit, out var units);
        var answer = status switch
        {
            RegexStatus.Match => "match " + string.Join(",", slots),
            RegexStatus.LimitExceeded => "LimitExceeded " + limit,
            _ => status.ToString(),
        };
        return (answer, limit, units);
    }
}
