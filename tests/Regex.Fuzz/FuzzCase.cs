using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Broiler.Regex.Engine;

namespace Broiler.Regex.Fuzz;

/// <summary>The caller's limits for one compilation: a nesting limit, a compile budget and a meter's allowance (null: a meter that counts and never throws).</summary>
internal readonly record struct CompilePolicy(int Nesting, ulong Budget, ulong? Meter);

/// <summary>The caller's ceilings for one match call and a meter's allowance (null: a meter that counts and never throws).</summary>
internal readonly record struct MatchPolicy(ulong? Steps, int Frames, int Trail, ulong? Meter)
{
    internal RegexMatchCeilings Ceilings => new(Steps, Frames, Trail);
}

/// <summary>
/// One iteration's input: a pattern and its flags, a subject, the start positions, and the policies every call is made
/// under. Every field is drawn from the iteration's own <see cref="FuzzRandom"/>, so a case is a function of the seed
/// and the index (<see cref="Generate"/>).
/// </summary>
internal sealed record FuzzCase(
    long Iteration,
    string Shape,
    string Pattern,
    RegexFlags Flags,
    string Subject,
    IReadOnlyList<string> Constructs,
    IReadOnlyList<int> Starts,
    CompilePolicy Full,
    CompilePolicy Tight,
    MatchPolicy Wide,
    MatchPolicy Narrow,
    int NarrowStart,
    bool NarrowAnchored,
    bool Probes,
    bool Escapes,
    RegexFlags EscapeFlags,
    int EscapeMaximum)
{
    /// <summary>The compile budget of every case's full compilation: far above any pattern the generator writes.</summary>
    internal const ulong FullCompileBudget = 1UL << 26;

    /// <summary>The step ceiling of the wide match calls, which every string-bearing call is compared with.</summary>
    internal const ulong WideSteps = 1UL << 18;

    /// <summary>The step ceiling of the wide calls of a case whose subject is long, so that its ceilings are reached.</summary>
    internal const ulong LongWideSteps = 1UL << 23;

    /// <summary>The case of iteration <paramref name="iteration"/> of the session seeded <paramref name="seed"/>.</summary>
    internal static FuzzCase Generate(ulong seed, long iteration)
    {
        var random = FuzzRandom.ForIteration(seed, iteration);
        var constructs = new SortedSet<string>(StringComparer.Ordinal);
        var flags = DrawFlags(random, constructs);

        string shape;
        string pattern;
        string subject;
        var roll = random.Below(100);
        if (roll < 22)
        {
            (shape, pattern, flags, subject) = CorpusShapes.Write(random, flags, constructs);
        }
        else
        {
            shape = "grammar";
            (pattern, var sample) = PatternGrammar.Write(random, flags, constructs);
            subject = Subjects.Around(random, sample, constructs);
            if (roll < 40)
            {
                shape = "near-miss";
                (pattern, flags) = NearMiss.Apply(random, pattern, flags, constructs);
            }
        }

        var longSubject = subject.Length > 4096;
        var starts = DrawStarts(random, subject, longSubject);
        var narrowStart = random.Pick(starts);
        var full = new CompilePolicy(BroilerRegex.MaximumNestingLimit, FullCompileBudget, null);
        var tight = new CompilePolicy(
            random.Pick([1, 2, 3, random.Between(1, 64), random.Between(1, BroilerRegex.MaximumNestingLimit), BroilerRegex.MaximumNestingLimit]),
            random.Chance(50) ? random.Between(1UL, 4096UL) : random.Between(1UL, FullCompileBudget),
            random.Chance(40) ? random.Between(1UL, 1UL << 16) : null);
        var wideSteps = longSubject ? LongWideSteps : WideSteps;
        var wide = new MatchPolicy(wideSteps, RegexMatchCeilings.EngineFrames, RegexMatchCeilings.EngineTrail, null);
        // A narrow call with no step ceiling has a meter that throws, so that no call of the session is unbounded.
        var narrowSteps = random.Chance(80) ? random.Between(1UL, wideSteps) : (ulong?)null;
        var narrow = new MatchPolicy(
            narrowSteps,
            random.Chance(50) ? random.Between(1, 64) : random.Between(1, RegexMatchCeilings.EngineFrames),
            random.Chance(50) ? random.Between(1, 64) : random.Between(1, RegexMatchCeilings.EngineTrail),
            narrowSteps is null || random.Chance(40) ? random.Between(1UL, 1UL << 14) : null);

        var escapeFlags = random.Pick([RegexFlags.None, RegexFlags.Unicode, RegexFlags.UnicodeSets])
            | (random.Chance(50) ? RegexFlags.IgnoreCase : RegexFlags.None)
            | (random.Chance(30) ? RegexFlags.Multiline : RegexFlags.None)
            | (random.Chance(30) ? RegexFlags.DotAll : RegexFlags.None);

        return new FuzzCase(
            iteration, shape, pattern, flags, subject, constructs.ToArray(), starts, full, tight, wide, narrow,
            narrowStart, random.Chance(50), random.Chance(5), random.Chance(50) && !longSubject, escapeFlags,
            random.Chance(80) ? 1 << 20 : random.Between(0, 64));
    }

    /// <summary>
    /// Every flag combination: the mode (none, <c>u</c>, <c>v</c>, and now and then both, which the flags text refuses
    /// and <see cref="BroilerRegex.Compile"/> reads as <c>v</c>), each of <c>d g i m s y</c> independently.
    /// </summary>
    private static RegexFlags DrawFlags(FuzzRandom random, SortedSet<string> constructs)
    {
        var mode = random.Below(100) switch
        {
            < 36 => RegexFlags.None,
            < 66 => RegexFlags.Unicode,
            < 97 => RegexFlags.UnicodeSets,
            _ => RegexFlags.Unicode | RegexFlags.UnicodeSets,
        };
        var flags = mode;
        foreach (var flag in (RegexFlags[])[RegexFlags.HasIndices, RegexFlags.Global, RegexFlags.IgnoreCase, RegexFlags.Multiline, RegexFlags.DotAll, RegexFlags.Sticky])
        {
            if (random.Chance(flag == RegexFlags.IgnoreCase ? 40 : 30))
                flags |= flag;
        }

        constructs.Add("mode-" + (mode switch
        {
            RegexFlags.None => "none",
            RegexFlags.Unicode => "u",
            RegexFlags.UnicodeSets => "v",
            _ => "u-and-v",
        }));
        if ((flags & RegexFlags.IgnoreCase) != 0)
            constructs.Add("flag-i");
        if ((flags & RegexFlags.Multiline) != 0)
            constructs.Add("flag-m");
        if ((flags & RegexFlags.DotAll) != 0)
            constructs.Add("flag-s");
        if ((flags & RegexFlags.Sticky) != 0)
            constructs.Add("flag-y");
        return flags;
    }

    /// <summary>
    /// The start positions: 0, the end, a position at random, and, where the subject holds a surrogate pair, the
    /// position inside it and the one after it. A long subject is tried from 0 and its end alone.
    /// </summary>
    private static int[] DrawStarts(FuzzRandom random, string subject, bool longSubject)
    {
        var starts = new SortedSet<int> { 0, subject.Length };
        if (!longSubject)
        {
            starts.Add(random.Between(0, subject.Length));
            var pairs = new List<int>();
            for (var i = 1; i < subject.Length; i++)
            {
                if (char.IsHighSurrogate(subject[i - 1]) && char.IsLowSurrogate(subject[i]))
                    pairs.Add(i);
            }

            if (pairs.Count > 0)
            {
                var inside = random.Pick(pairs);
                starts.Add(inside);
                starts.Add(inside + 1);
            }
        }

        return starts.ToArray();
    }

    /// <summary>The case as one line of text a reader can paste back: every string as a JSON-style literal.</summary>
    internal string Describe()
    {
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"iteration {Iteration}, shape {Shape}: pattern {Quote(Pattern)}, flags {FlagsText(Flags)}, subject {Quote(Subject)}");
        text.Append(CultureInfo.InvariantCulture, $"; starts [{string.Join(",", Starts)}]");
        text.Append(CultureInfo.InvariantCulture, $"; full compile {Show(Full)}; tight compile {Show(Tight)}");
        text.Append(CultureInfo.InvariantCulture, $"; wide match {Show(Wide)}; narrow match {Show(Narrow)} at {NarrowStart}{(NarrowAnchored ? " anchored" : "")}");
        return text.ToString();
    }

    private static string Show(CompilePolicy policy) =>
        string.Create(CultureInfo.InvariantCulture, $"(nesting {policy.Nesting}, budget {policy.Budget}, meter {(policy.Meter is { } m ? m.ToString(CultureInfo.InvariantCulture) : "counting")})");

    private static string Show(MatchPolicy policy) =>
        string.Create(CultureInfo.InvariantCulture, $"(steps {(policy.Steps is { } s ? s.ToString(CultureInfo.InvariantCulture) : "none")}, frames {policy.Frames}, trail {policy.Trail}, meter {(policy.Meter is { } m ? m.ToString(CultureInfo.InvariantCulture) : "counting")})");

    /// <summary>The flags as letters; a combination <see cref="Text.RegexFlagsText"/> would refuse is written all the same.</summary>
    internal static string FlagsText(RegexFlags flags)
    {
        var text = new StringBuilder();
        foreach (var (flag, letter) in (ReadOnlySpan<(RegexFlags, char)>)[(RegexFlags.HasIndices, 'd'), (RegexFlags.Global, 'g'), (RegexFlags.IgnoreCase, 'i'), (RegexFlags.Multiline, 'm'), (RegexFlags.DotAll, 's'), (RegexFlags.Unicode, 'u'), (RegexFlags.UnicodeSets, 'v'), (RegexFlags.Sticky, 'y')])
        {
            if ((flags & flag) != 0)
                text.Append(letter);
        }

        return text.Length == 0 ? "-" : text.ToString();
    }

    /// <summary>A string as a JSON literal, every code unit outside printable ASCII escaped, a run of 8 or more repeats shortened to <c>x * n</c>.</summary>
    internal static string Quote(string text)
    {
        if (text.Length > 64)
        {
            // A long text is mostly runs: written as concatenated repeats, as the adversarial corpus writes its subjects.
            var parts = new List<string>();
            var plain = new StringBuilder();
            var i = 0;
            while (i < text.Length)
            {
                var best = (Period: 1, Count: 1);
                for (var period = 1; period <= 8 && i + period <= text.Length; period++)
                {
                    var count = 1;
                    while (i + ((count + 1) * period) <= text.Length && string.CompareOrdinal(text, i, text, i + (count * period), period) == 0)
                        count++;
                    if (count >= 8 && count * period > best.Count * best.Period)
                        best = (period, count);
                }

                if (best.Count >= 8)
                {
                    if (plain.Length > 0)
                        parts.Add(Literal(plain.ToString()));
                    plain.Clear();
                    parts.Add(Literal(text.Substring(i, best.Period)) + " * " + best.Count.ToString(CultureInfo.InvariantCulture));
                    i += best.Period * best.Count;
                }
                else
                {
                    plain.Append(text[i]);
                    i++;
                }
            }

            if (plain.Length > 0)
                parts.Add(Literal(plain.ToString()));
            return string.Join(" + ", parts);
        }

        return Literal(text);
    }

    private static string Literal(string text)
    {
        var quoted = new StringBuilder("\"");
        foreach (var c in text)
        {
            if (c is '"' or '\\')
                quoted.Append('\\').Append(c);
            else if (c is >= ' ' and <= '~')
                quoted.Append(c);
            else
                quoted.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
        }

        return quoted.Append('"').ToString();
    }
}
