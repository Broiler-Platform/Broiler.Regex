using System;
using System.Linq;

namespace Broiler.Regex.Fuzz;

/// <summary>
/// Reduces a case to a smaller one with the same finding: characters of the pattern, then of the subject, removed in
/// halving chunks while a finding of the same signature (<see cref="Finding.Signature"/>) remains, then each flag the
/// finding does not need.
/// </summary>
internal static class Reducer
{
    /// <summary>The most cases one reduction runs.</summary>
    private const int Attempts = 20_000;

    /// <summary>The longest span the window pass removes at once.</summary>
    private const int Window = 12;

    internal static FuzzCase Reduce(FuzzCase found, string signature)
    {
        var attempts = 0;
        bool Holds(FuzzCase candidate)
        {
            if (attempts++ >= Attempts)
                return false;
            return Verdicts.Run(candidate).Findings.Any(f => f.Signature == signature);
        }

        var current = found;
        current = current with { Pattern = Shrink(current.Pattern, text => Holds(current with { Pattern = text })) };
        current = WithSubject(current, Shrink(current.Subject, text => Holds(WithSubject(current, text))));
        foreach (var flag in Enum.GetValues<RegexFlags>().Where(static f => f != RegexFlags.None))
        {
            if ((current.Flags & flag) != 0 && Holds(current with { Flags = current.Flags & ~flag }))
                current = current with { Flags = current.Flags & ~flag };
        }

        return current;
    }

    private static FuzzCase WithSubject(FuzzCase c, string subject) => c with
    {
        Subject = subject,
        Starts = c.Starts.Select(s => Math.Min(s, subject.Length)).Distinct().Order().ToArray(),
        NarrowStart = Math.Min(c.NarrowStart, subject.Length),
    };

    private static string Shrink(string text, Func<string, bool> holds)
    {
        var chunk = Math.Max(1, text.Length / 2);
        while (true)
        {
            var progress = false;
            for (var at = 0; at < text.Length;)
            {
                var candidate = text.Remove(at, Math.Min(chunk, text.Length - at));
                if (holds(candidate))
                {
                    text = candidate;
                    progress = true;
                }
                else
                {
                    at += chunk;
                }
            }

            if (!progress)
            {
                if (chunk == 1)
                    break;
                chunk = Math.Max(1, chunk / 2);
            }
        }

        // Halving chunks miss a span that only goes whole, such as a quantifier with its atom: every span up to
        // Window long is tried at every position, until none can go.
        for (var progress = true; progress;)
        {
            progress = false;
            for (var length = Math.Min(Window, text.Length); length >= 1 && !progress; length--)
            {
                for (var at = 0; at + length <= text.Length; at++)
                {
                    var candidate = text.Remove(at, length);
                    if (holds(candidate))
                    {
                        text = candidate;
                        progress = true;
                        break;
                    }
                }
            }
        }

        return text;
    }
}
