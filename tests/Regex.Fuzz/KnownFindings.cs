using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Broiler.Regex.Fuzz;

/// <summary>
/// One finding in the library that this target reported and that is not fixed yet (<c>known-findings.tsv</c>): its
/// kind, the exception's type, the innermost library method on its stack, a text its message holds, and a
/// reproduction a test runs.
/// </summary>
internal sealed record KnownFinding(string Kind, string Type, string Method, string Message, string Pattern, RegexFlags Flags, string Reported)
{
    /// <summary>
    /// Whether <paramref name="finding"/> is this one: the same kind and exception type, thrown with this method the
    /// innermost of the library's frames, with a message that holds <see cref="Message"/>. Another exception of the type
    /// from the same method, or from a method it calls, is not this one, and is reported as a finding.
    /// </summary>
    internal bool Matches(Finding finding)
    {
        if (finding.Kind != Kind || !finding.Detail.StartsWith(Type + ": ", StringComparison.Ordinal))
            return false;
        // Verdicts.Describe: "Type: message at Method (File.cs:line) < ...", the frames innermost first.
        var at = finding.Detail.LastIndexOf(" at ", StringComparison.Ordinal);
        if (at < Type.Length + 2)
            return false;
        var message = finding.Detail[(Type.Length + 2)..at];
        var innermost = finding.Detail[(at + 4)..].Split(" < ")[0].Split(" (")[0];
        return innermost == Method && message.Contains(Message, StringComparison.Ordinal);
    }

    internal string Key => Kind + " " + Type + " (\"" + Message + "\") in " + Method;
}

/// <summary>
/// The list of known findings, <c>known-findings.tsv</c> beside this file. A finding the library code owes a fix for is
/// reported by the session that finds it, and fixed by the library's owner; until then it is listed here, so that the
/// session goes on judging everything else. A listed finding is printed as known and does not fail a session; a listed
/// finding the declared session no longer reaches fails it, as stale, so the list is emptied when the fix lands; and
/// <c>FuzzTargetTests</c> runs each entry's reproduction and fails once it no longer reproduces.
/// </summary>
internal static partial class KnownFindings
{
    internal static IReadOnlyList<KnownFinding> All { get; } = Read(Path.Combine(AppContext.BaseDirectory, "known-findings.tsv"));

    internal static KnownFinding? Of(Finding finding) => All.FirstOrDefault(known => known.Matches(finding));

    /// <summary>A stack frame's method with the compiler's names for local functions and lambdas read as their method's.</summary>
    internal static string Method(string frame) => Generated().Replace(frame.Trim(), "$1");

    [System.Text.RegularExpressions.GeneratedRegex(@"<(\w+)>[a-z]__[\w|]+")]
    private static partial System.Text.RegularExpressions.Regex Generated();

    private static List<KnownFinding> Read(string path)
    {
        var list = new List<KnownFinding>();
        if (!File.Exists(path))
            return list;
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            var fields = line.Split('\t');
            if (fields.Length != 7)
                throw new FormatException("known-findings.tsv: a row has " + fields.Length + " fields, not 7: " + line);
            if (fields[3].Length == 0)
                throw new FormatException("known-findings.tsv: a row has no text of the exception's message: " + line);
            list.Add(new KnownFinding(
                fields[0],
                fields[1],
                fields[2],
                fields[3],
                JsonSerializer.Deserialize<string>(fields[4]) ?? throw new FormatException("known-findings.tsv: the pattern is not a JSON string: " + fields[4]),
                Letters(fields[5]),
                fields[6]));
        }

        return list;
    }

    private static RegexFlags Letters(string text)
    {
        var flags = RegexFlags.None;
        foreach (var letter in text == "-" ? "" : text)
        {
            flags |= letter switch
            {
                'd' => RegexFlags.HasIndices,
                'g' => RegexFlags.Global,
                'i' => RegexFlags.IgnoreCase,
                'm' => RegexFlags.Multiline,
                's' => RegexFlags.DotAll,
                'u' => RegexFlags.Unicode,
                'v' => RegexFlags.UnicodeSets,
                'y' => RegexFlags.Sticky,
                _ => throw new FormatException("known-findings.tsv: an unknown flag " + letter),
            };
        }

        return flags;
    }
}
