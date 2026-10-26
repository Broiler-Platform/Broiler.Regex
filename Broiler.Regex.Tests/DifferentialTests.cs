using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Broiler.Regex.Compiling;
using Broiler.Regex.Engine;
using Broiler.Unicode.Tables;
using Xunit;
using Xunit.Abstractions;

namespace Broiler.Regex.Tests;

/// <summary>
/// The differential test of task 2.4a: the new backend and the old one answer the same, index, length and every
/// capture, on two populations. The <b>suite population</b> is every string literal of this test project's sources taken
/// as a pattern, under each flag string near it and under five base flag strings, against the literals near it and a
/// fixed set of subjects, from three start indices; the <b>generated population</b> is drawn by a seeded SplitMix64 from
/// the 2.4a subset's grammar. A case the new compiler answers <see cref="ProgramCompilation.NotYetSupported"/> for is
/// outside the subset and counted, not run.
/// </summary>
/// <remarks>
/// <para>
/// <b>The old backend's budget.</b> The old backend runs under its own step budget, ten million steps per start position,
/// which it answers as a failed start, not as a limit (task 2.4c deletes it). The new one runs under a step ceiling of
/// <see cref="StepCeiling"/> units and the engines' frame and trail ceilings, and a case it stops at a ceiling is counted,
/// not compared, so that no comparison rests on the old budget's silent failure. The ceiling was set from the first run's
/// disagreements, under a ceiling of two million units: three were the old budget's (nested counted quantifiers over an
/// empty alternative, <c>((?&lt;n0&gt;[^]?|){2,}){2,}</c>, on subjects of five to nine characters, where the old backend
/// answered no match after five to eleven seconds although a prefix of the subject matched), and each took the new backend
/// about 241,000 units. Under the ceiling of 100,000, 22 generated cases reach it and every case compared agrees; the
/// largest takes 95,844 units.
/// </para>
/// <para>
/// <b>The old backend's defect D1.</b> Under <c>u</c> and <c>v</c>, a start inside a surrogate pair is a position the
/// new entry reads forward from, so the pair's low half is a character of its own and nothing before the start is read
/// (Broiler.VM's matcher reads it so; RegExpBuiltinExec's step back to the pair's first unit is the caller's, and
/// Broiler.VM's realm takes it before calling). The old backend's greedy run of one character gives back across the
/// start, pairing that low half with the high half before it, so <c>/[\s\S]*\u{1F600}/u</c> from index 1 of
/// <c>"\u{1F600}ba"</c> answers a match at 1 of the character at 0 (and a group can be captured before the match), while
/// <c>/(?:[\s\S])*\u{1F600}/u</c>, which takes no run, answers no match. No forward matcher of ECMA-262 §22.2.2 moves
/// before the index it is called at, so the specification's answer is the new backend's; a test of
/// <see cref="MachineTests"/> pins it. A case starting inside a pair is therefore compared with the old backend's answer on
/// a copy of the subject whose unit before the start is no surrogate: what lies before the start cannot change a forward
/// match, and the copy keeps the old backend from reading it. The first run found seven such disagreements, all D1.
/// </para>
/// <para>
/// The sticky flag <c>y</c> is the new entry's anchored argument; the old backend reads it from its flags.
/// </para>
/// </remarks>
public class DifferentialTests(ITestOutputHelper output)
{
    /// <summary>The new backend's step ceiling for one case: below the 241,000 units of the cases the old backend's budget was found cutting short.</summary>
    private const ulong StepCeiling = 100_000;

    private static readonly RegexMatchCeilings Ceilings = new(StepCeiling, RegexMatchCeilings.EngineFrames, RegexMatchCeilings.EngineTrail);

    /// <summary>The generated population's size, in patterns, each against up to eight subjects from two starts.</summary>
    private const int Generated = 20_000;

    /// <summary>The flag strings every suite pattern runs under, besides those written near it.</summary>
    private static readonly string[] BaseFlags = ["", "s", "u", "v", "y"];

    /// <summary>The subjects every suite pattern runs against, besides the literals near it.</summary>
    private static readonly string[] BaseSubjects =
    [
        "", "a", "ab", "aaa", "abcabc", "aab-bba", "a\nb\rc\u2028d\u2029e", "\uD83D\uDE00", "x\uD83Dy\uDE00z",
        "\uD83D\uDE00\uD83D\uDE00a", "123 abc_DEF", "\u0000\u0001", "colour color", "xyzzy",
    ];

    [Fact]
    public void The_suite_s_patterns_in_the_subset_answer_the_same_on_both_backends()
    {
        var tally = new Tally();
        var sources = TestSources();
        foreach (var file in sources)
        {
            var literals = Literals(File.ReadAllText(file)).ToArray();
            foreach (var (line, pattern) in literals)
            {
                var near = literals.Where(other => other.Line >= line && other.Line <= line + 3).Select(static other => other.Text).ToArray();
                var flags = BaseFlags.Concat(near.Where(LooksLikeFlags)).Distinct(StringComparer.Ordinal);
                var subjects = near.Concat(BaseSubjects).Distinct(StringComparer.Ordinal).ToArray();
                foreach (var flag in flags)
                    tally.Pattern(pattern, flag, subjects, static subject => subject.Length == 0 ? [0] : [0, 1, subject.Length], Path.GetFileName(file) + ":" + line);
            }
        }

        output.WriteLine($"suite population: {sources.Length} source files; {tally}");
        Assert.True(tally.Compared > 10_000, "the suite population compared only " + tally.Compared + " cases");
        Assert.True(tally.Disagreements.Count == 0, Report(tally));
    }

    [Fact]
    public void A_seeded_population_over_the_subset_answers_the_same_on_both_backends()
    {
        var tally = new Tally();
        var random = new SplitMix64(0x2_4A_2026_1026);
        string[] flagChoices = ["", "s", "u", "us", "v", "vs", "y", "uy"];
        for (var i = 0; i < Generated; i++)
        {
            var flags = flagChoices[random.Below(flagChoices.Length)];
            var generator = new PatternGenerator(random);
            var pattern = generator.Disjunction(0);
            var subjects = Enumerable.Range(0, 8).Select(_ => Subject(random)).Distinct(StringComparer.Ordinal).ToArray();
            var starts = subjects.ToDictionary(static subject => subject, subject => random.Below(subject.Length + 1), StringComparer.Ordinal);
            var outside = tally.OutsideSubset;
            tally.Pattern(pattern, flags, subjects, subject => [0, starts[subject]], "generated #" + i);
            Assert.True(tally.OutsideSubset == outside, "the generator left the subset: /" + pattern + "/" + flags);
        }

        output.WriteLine($"generated population: seed 0x24A20261026, {Generated} patterns; {tally}");
        Assert.True(tally.Disagreements.Count == 0, Report(tally));
    }

    /// <summary>The old backend's answer in the form <see cref="MachineTests.Render"/> gives the new one's.</summary>
    internal static string Old(BroilerRegex regex, string subject, int start)
    {
        var match = regex.Match(subject, start);
        if (!match.Success)
            return "nomatch";

        var text = new StringBuilder("match ").Append(match.Index).Append(',').Append(match.Index + match.Length);
        foreach (var group in match.Groups.Skip(1))
            text.Append(' ').Append(group.Success ? group.Index + "," + (group.Index + group.Length) : "-");
        return text.ToString();
    }

    private static string Report(Tally tally) =>
        tally.Disagreements.Count + " disagreements:\n" + string.Join('\n', tally.Disagreements.Take(60));

    private static bool LooksLikeFlags(string text) =>
        text.Length <= 8 && text.All(static c => "dgimsuvy".Contains(c)) && text.Distinct().Count() == text.Length
        && !(text.Contains('u') && text.Contains('v'));

    /// <summary>Runs one pattern under one flag string against every subject from every start the chooser names.</summary>
    private sealed class Tally
    {
        internal int Patterns;
        internal int OutsideSubset;
        internal int NotPatterns;
        internal int Compared;
        internal int OverCeiling;
        internal int OldOverflow;
        internal int InsidePair;
        internal ulong MostUnits;
        internal readonly List<string> Disagreements = [];
        private readonly HashSet<(string, string)> _seen = [];

        internal void Pattern(string pattern, string flags, string[] subjects, Func<string, int[]> starts, string origin)
        {
            if (!_seen.Add((pattern, flags)))
                return;

            BroilerRegex old;
            RegexProgram? program;
            try
            {
                old = TestRegex.Compile(pattern, flags);
                if (ProgramCompiler.Compile(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out program) != ProgramCompilation.Compiled)
                {
                    OutsideSubset++;
                    return;
                }
            }
            catch (RegexSyntaxException)
            {
                NotPatterns++;
                return;
            }

            Patterns++;
            var anchored = flags.Contains('y');
            var unicode = flags.Contains('u') || flags.Contains('v');
            var slots = new int[program!.SlotCount];
            foreach (var subject in subjects)
            {
                foreach (var start in starts(subject).Distinct())
                {
                    var status = program.Match(subject, start, anchored, slots, null, Ceilings, out _, out var units);
                    if (status == RegexStatus.LimitExceeded)
                    {
                        OverCeiling++;
                        continue;
                    }

                    // The old backend's defect D1: a start inside a surrogate pair is compared on a copy whose unit before
                    // the start is no surrogate.
                    var oldSubject = subject;
                    if (unicode && start > 0 && start < subject.Length && char.IsLowSurrogate(subject[start]) && char.IsHighSurrogate(subject[start - 1]))
                    {
                        oldSubject = string.Concat(subject.AsSpan(0, start - 1), "#", subject.AsSpan(start));
                        InsidePair++;
                    }

                    string expected;
                    try
                    {
                        expected = Old(old, oldSubject, start);
                    }
                    catch (RegexOverflowException)
                    {
                        OldOverflow++;
                        continue;
                    }

                    var actual = MachineTests.Render(status, slots);
                    Compared++;
                    MostUnits = Math.Max(MostUnits, units);
                    if (expected != actual)
                        Disagreements.Add($"{origin}: /{Escape(pattern)}/{flags} on \"{Escape(subject)}\" from {start}: old {expected}, new {actual}");
                }
            }
        }

        public override string ToString() =>
            $"{Patterns} pattern-flag pairs in the subset, {OutsideSubset} outside it, {NotPatterns} not patterns under their flags; " +
            $"{Compared} cases compared ({InsidePair} starting inside a surrogate pair), the most units {MostUnits}; " +
            $"{OverCeiling} at the new backend's step ceiling, {OldOverflow} the old backend's stack guard refused; " +
            $"{Disagreements.Count} disagreements";
    }

    internal static string Escape(string text)
    {
        var escaped = new StringBuilder();
        foreach (var c in text)
            escaped.Append(c is < ' ' or > '~' ? $"\\u{(int)c:X4}" : c.ToString());
        return escaped.ToString();
    }

    private static string Subject(SplitMix64 random)
    {
        string[] pieces = ["a", "a", "b", "b", "c", "x", "\n", "\uD83D\uDE00", "\uD83D", "\uDE00", "1", " "];
        var subject = new StringBuilder();
        var length = random.Below(9);
        for (var i = 0; i < length; i++)
            subject.Append(pieces[random.Below(pieces.Length)]);
        return subject.ToString();
    }

    /// <summary>A pattern of the 2.4a subset: literals, the dot, classes, groups of the three kinds, alternation and every quantifier form.</summary>
    private sealed class PatternGenerator(SplitMix64 random)
    {
        private int _names;

        private static readonly string[] Atoms =
        [
            "a", "a", "b", "b", "c", "x", ".", "\\d", "\\w", "\\W", "\\s", "\\n", "[ab]", "[^a]", "[a-c]", "[\\d\\n]", "[^\\n]",
            "[\\s\\S]", "[]", "[^]", "\uD83D\uDE00", "\\uD83D\\uDE00", "\\uD83D", "[\uD83D\uDE00a]", "[^\uD83D\uDE00]", "\\-",
        ];

        private static readonly string[] Quantifiers = ["*", "+", "?", "{0}", "{1}", "{2}", "{0,1}", "{1,2}", "{2,3}", "{0,}", "{2,}", "{0,2}"];

        internal string Disjunction(int depth)
        {
            var alternatives = random.Below(4) == 0 ? 2 + random.Below(2) : 1;
            return string.Join('|', Enumerable.Range(0, alternatives).Select(_ => Sequence(depth)));
        }

        private string Sequence(int depth)
        {
            var terms = new StringBuilder();
            var count = random.Below(4);
            for (var i = 0; i < count; i++)
            {
                terms.Append(Atom(depth));
                if (random.Below(2) == 0)
                {
                    terms.Append(Quantifiers[random.Below(Quantifiers.Length)]);
                    if (random.Below(3) == 0)
                        terms.Append('?');
                }
            }
            return terms.ToString();
        }

        private string Atom(int depth)
        {
            if (depth < 3 && random.Below(3) == 0)
            {
                var inner = Disjunction(depth + 1);
                return random.Below(3) switch
                {
                    0 => "(" + inner + ")",
                    1 => "(?:" + inner + ")",
                    _ => "(?<n" + _names++ + ">" + inner + ")",
                };
            }
            return Atoms[random.Below(Atoms.Length)];
        }
    }

    /// <summary>SplitMix64: a seeded generator whose stream is fixed by its seed on every platform.</summary>
    private sealed class SplitMix64(ulong state)
    {
        private ulong _state = state;

        internal int Below(int bound)
        {
            _state += 0x9E3779B97F4A7C15;
            var z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
            z ^= z >> 31;
            return (int)(z % (ulong)bound);
        }
    }

    /// <summary>This test project's C# sources, the differential test's own file left out.</summary>
    private static string[] TestSources()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Broiler.Regex.slnx")))
            directory = directory.Parent;
        Assert.True(directory != null, "no Broiler.Regex.slnx above " + AppContext.BaseDirectory);
        return Directory.GetFiles(Path.Combine(directory!.FullName, "Broiler.Regex.Tests"), "*.cs")
            .Where(static file => Path.GetFileName(file) != "DifferentialTests.cs")
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// The string literals of a C# source, with the line each starts on: regular literals with their escapes and verbatim
    /// literals; comments, character literals and interpolated strings are skipped.
    /// </summary>
    internal static IEnumerable<(int Line, string Text)> Literals(string source)
    {
        var line = 1;
        var i = 0;
        while (i < source.Length)
        {
            var c = source[i];
            var next = i + 1 < source.Length ? source[i + 1] : '\0';
            if (c == '\n')
            {
                line++;
                i++;
            }
            else if (c == '/' && next == '/')
            {
                while (i < source.Length && source[i] != '\n')
                    i++;
            }
            else if (c == '/' && next == '*')
            {
                i += 2;
                while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/'))
                    line += source[i++] == '\n' ? 1 : 0;
                i += 2;
            }
            else if (c == '\'')
            {
                i++;
                while (source[i] != '\'')
                    i += source[i] == '\\' ? 2 : 1;
                i++;
            }
            else if (c == '$' || (c == '@' && next == '$'))
            {
                // An interpolated string: its text is built at run time, so it is no literal.
                while (source[i] != '"')
                    i++;
                var verbatim = source[i - 1] == '@' || (i >= 2 && source[i - 2] == '@');
                i++;
                while (true)
                {
                    if (source[i] == '"' && !(verbatim && i + 1 < source.Length && source[i + 1] == '"'))
                        break;
                    i += !verbatim && source[i] == '\\' || verbatim && source[i] == '"' ? 2 : 1;
                }
                i++;
            }
            else if (c == '@' && next == '"')
            {
                var start = line;
                var text = new StringBuilder();
                i += 2;
                while (true)
                {
                    if (source[i] == '"')
                    {
                        if (i + 1 < source.Length && source[i + 1] == '"')
                        {
                            text.Append('"');
                            i += 2;
                            continue;
                        }
                        i++;
                        break;
                    }
                    line += source[i] == '\n' ? 1 : 0;
                    text.Append(source[i++]);
                }
                yield return (start, text.ToString());
            }
            else if (c == '"')
            {
                var text = new StringBuilder();
                i++;
                while (source[i] != '"')
                {
                    if (source[i] != '\\')
                    {
                        text.Append(source[i++]);
                        continue;
                    }
                    var escape = source[i + 1];
                    i += 2;
                    switch (escape)
                    {
                        case 'u':
                            text.Append((char)Convert.ToInt32(source.Substring(i, 4), 16));
                            i += 4;
                            break;
                        case 'U':
                            text.Append(char.ConvertFromUtf32(Convert.ToInt32(source.Substring(i, 8), 16)));
                            i += 8;
                            break;
                        case 'x':
                            var digits = 0;
                            while (digits < 4 && Uri.IsHexDigit(source[i + digits]))
                                digits++;
                            text.Append((char)Convert.ToInt32(source.Substring(i, digits), 16));
                            i += digits;
                            break;
                        default:
                            text.Append(escape switch
                            {
                                '0' => '\0', 'a' => '\a', 'b' => '\b', 'f' => '\f', 'n' => '\n', 'r' => '\r', 't' => '\t', 'v' => '\v',
                                _ => escape,
                            });
                            break;
                    }
                }
                i++;
                yield return (line, text.ToString());
            }
            else
            {
                i++;
            }
        }
    }
}
