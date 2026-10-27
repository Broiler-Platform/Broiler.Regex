using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Broiler.Regex.Compiling;
using Broiler.Regex.Engine;
using Broiler.Regex.Parsing;
using Broiler.Regex.Unicode;
using Broiler.Unicode.Tables;
using Xunit;
using Xunit.Abstractions;

namespace Broiler.Regex.Tests;

/// <summary>
/// The differential test of task 2.4c's fast paths (plan row 2.4c, item 2): every pattern of a population compiles twice,
/// with the fast paths and without them (the program compiler's internal option, <c>fastPaths</c>), and the two programs
/// answer the same on every case, the status, the ceiling a limit names, the index, the length and every capture. The
/// populations are task 2.4b's two (<see cref="TestPopulations.SuitePopulation"/> and
/// <see cref="TestPopulations.SeededPopulation"/>) and a third drawn from a seed of its own and weighted to the shapes
/// the fast paths replace (<see cref="FastPathPopulation"/>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The ceilings.</b> Both programs run under the same ceilings: <see cref="StepCeiling"/> units and the engines' frame
/// and trail ceilings. The fast paths do different work for the same answer, and the units are not the answer, so a
/// case either program stops at its step ceiling runs again in both under <see cref="SecondCeiling"/> units, and is
/// compared there when neither stops; a case stopped there too is counted, not compared. Each population's counts are
/// pinned: the cases stopped under the first ceiling, those the general lowering alone was stopped at, and those still
/// stopped under the second, so that a fast path answering where it should not, or working where it should not, moves
/// a count. No case may stop at a frame or trail ceiling in either program: the subjects are short, and a stop there
/// would be one program giving up where the other answers.
/// </para>
/// <para>
/// <b>Population P4.</b> The plan names the reference arms' population P4 for this check; it does not exist yet, as task
/// 2.13 builds it. The record of task 2.4c says so, and this comparison runs over P4 when task 2.13 lands.
/// </para>
/// </remarks>
public class FastPathDifferentialTests(ITestOutputHelper output)
{
    /// <summary>Each program's step ceiling for one case, the old-versus-new differential's.</summary>
    private const ulong StepCeiling = 100_000;

    private static readonly RegexMatchCeilings Ceilings = new(StepCeiling, RegexMatchCeilings.EngineFrames, RegexMatchCeilings.EngineTrail);

    /// <summary>The step ceiling a case stopped under <see cref="StepCeiling"/> runs again under, in both programs.</summary>
    private const ulong SecondCeiling = 10_000_000;

    private static readonly RegexMatchCeilings SecondCeilings = new(SecondCeiling, RegexMatchCeilings.EngineFrames, RegexMatchCeilings.EngineTrail);

    /// <summary>The weighted population's seed: task 2.4c's, the date it was drawn on.</summary>
    private const ulong Seed = 0x2_4C_2026_1026;

    /// <summary>The weighted population's size, in patterns, each against up to eight subjects from two starts.</summary>
    private const int Weighted = 12_000;

    [Fact]
    public void The_suite_s_patterns_answer_the_same_with_and_without_the_fast_paths()
    {
        var tally = new Tally();
        foreach (var row in TestPopulations.SuitePopulation(TestPopulations.TestSources()))
            tally.Pattern(row);

        Report("suite population", tally);
        Assert.True(tally.Disagreements.Count == 0, Disagreements(tally));
        Assert.True(tally.Compared >= 1_300_000, "the suite population compared only " + tally.Compared + " cases");

        // Under the first ceiling (a+)+b's 26 cases on twelve to fifteen a's stop in both programs, as in the old-versus-new
        // differential, and the general lowering alone stops 60, \p{RGI_Emoji}'s strings tried one by one; all 86 answer
        // the same under the second. The general lowering stopped 16 until task 2.5's CompileChargingTests added classes of
        // RGI_Emoji to the suite's literals, which stop 44 more (the population without that file still stops 16).
        Assert.Equal((26, 0, 60, 0, 0, 0), Stops(tally));
    }

    [Fact]
    public void Task_2_4b_s_seeded_population_answers_the_same_with_and_without_the_fast_paths()
    {
        var tally = new Tally();
        foreach (var row in TestPopulations.SeededPopulation())
            tally.Pattern(row);

        Report("task 2.4b's generated population", tally);
        Assert.True(tally.Disagreements.Count == 0, Disagreements(tally));
        Assert.True(tally.Compared >= 240_000, "the generated population compared only " + tally.Compared + " cases");
        Assert.Equal((0, 0, 2, 0, 0, 0), Stops(tally));

        // The population draws every construct of the grammar, each kind of node, class and look-around, as task 2.4b's
        // differential asserted of it.
        Assert.All(TestPopulations.ConstructNames, name => Assert.True(tally.Constructs.GetValueOrDefault(name) >= 100, name + " was drawn " + tally.Constructs.GetValueOrDefault(name) + " times"));
    }

    [Fact]
    public void A_population_weighted_to_the_fast_paths_answers_the_same_with_and_without_them()
    {
        var tally = new Tally();
        foreach (var row in FastPathPopulation())
            tally.Pattern(row);

        Report("weighted population: seed 0x" + Seed.ToString("X", System.Globalization.CultureInfo.InvariantCulture) + ", " + Weighted + " patterns", tally);
        Assert.True(tally.Disagreements.Count == 0, Disagreements(tally));
        Assert.True(tally.Compared >= 160_000, "the weighted population compared only " + tally.Compared + " cases");
        Assert.Equal((6, 0, 1, 0, 0, 0), Stops(tally));

        // The population holds each fast path in quantity, each instruction that reads both ways in both directions, and
        // programs with a first test, so the scan is compared too.
        foreach (var shape in (string[])["GreedyRun", "LazyRun", "AtomRun", "StringClass", "GreedyRun rtl", "LazyRun rtl", "AtomRun rtl", "StringClass rtl", "first test"])
            Assert.True(tally.Shapes.GetValueOrDefault(shape) >= 300, shape + " was compiled " + tally.Shapes.GetValueOrDefault(shape) + " times");
    }

    /// <summary>
    /// The pinned counts of a population's cases left to the second ceiling: stopped under the first in both programs,
    /// with the fast paths alone, without them alone; stopped under the second too; and the frame and trail ceilings
    /// reached in either program.
    /// </summary>
    private static (int, int, int, int, int, int) Stops(Tally tally) =>
        (tally.BothOverSteps, tally.FastOverSteps, tally.GeneralOverSteps, tally.StillOverSteps, tally.OverFrames, tally.OverTrail);

    private void Report(string population, Tally tally)
    {
        output.WriteLine(population + ": " + tally);
        foreach (var shape in tally.Shapes)
            output.WriteLine($"  {shape.Key}: {shape.Value} programs");
        foreach (var stopped in tally.Stopped)
            output.WriteLine("  stopped: " + stopped);
    }

    private static string Disagreements(Tally tally) =>
        tally.Disagreements.Count + " disagreements:\n" + string.Join('\n', tally.Disagreements.Take(60));

    /// <summary>
    /// The population weighted to the fast paths, from <see cref="Seed"/>: patterns that open with a literal prefix half the
    /// time (an atom, an atom run, or a run that requires a character, inside a group or not), then terms that are mostly
    /// quantified single characters and classes, greedy and lazy, under every quantifier form, rows of atoms, classes of
    /// strings under <c>v</c>, look-behinds whose bodies hold the same shapes right to left, look-aheads, groups,
    /// alternations, assertions and back-references; under the flag strings of task 2.4b's population, against subjects of
    /// up to sixteen pieces drawn from the atoms' characters, from the start and one other index.
    /// </summary>
    internal static IEnumerable<TestPopulations.PopulationCase> FastPathPopulation()
    {
        var random = new TestPopulations.SplitMix64(Seed);
        string[] flagChoices = ["", "i", "m", "s", "u", "iu", "us", "v", "iv", "vs", "y", "iy", "imsu"];
        for (var i = 0; i < Weighted; i++)
        {
            var flags = flagChoices[random.Below(flagChoices.Length)];
            var pattern = new WeightedGenerator(random, flags).Pattern();
            var subjects = Enumerable.Range(0, 8).Select(_ => Subject(random)).Distinct(StringComparer.Ordinal).ToArray();
            var starts = subjects.ToDictionary(static subject => subject, subject => random.Below(subject.Length + 1), StringComparer.Ordinal);
            yield return new(pattern, flags, subjects, subject => [0, starts[subject]], "weighted #" + i);
        }
    }

    private static string Subject(TestPopulations.SplitMix64 random)
    {
        string[] pieces =
        [
            "a", "a", "a", "a", "b", "b", "b", "c", "x", "x", "ab", "abc", "A", "B", "K", "k", "K", "ſ", "s", "S",
            "1", "2", " ", "\n", "-", "😀", "\uD83D", "\uDE00",
        ];
        var subject = new StringBuilder();
        var length = random.Below(17);
        for (var i = 0; i < length; i++)
            subject.Append(pieces[random.Below(pieces.Length)]);
        return subject.ToString();
    }

    /// <summary>A pattern weighted to the fast paths' shapes under <paramref name="flags"/> (<see cref="FastPathPopulation"/>).</summary>
    private sealed class WeightedGenerator(TestPopulations.SplitMix64 random, string flags)
    {
        private readonly bool _unicode = flags.Contains('u') || flags.Contains('v');
        private readonly bool _sets = flags.Contains('v');
        private int _groups;

        /// <summary>Single characters and classes of single characters, valid under every flag string.</summary>
        private static readonly string[] Atoms =
        [
            "a", "a", "a", "b", "b", "c", "x", "A", "K", "k", "s", ".", "\\d", "\\w", "\\W", "\\s", "[ab]", "[^a]", "[a-c]",
            "[a-cx-z]", "[^\\n]", "[\\s\\S]", "[^]", "😀", "\\uD83D", "\\uDE00", "[😀a]", "ſ", "K",
            "[^k]", "[\\w-]", "-",
        ];

        /// <summary>Atoms valid under <c>u</c> and <c>v</c> alone.</summary>
        private static readonly string[] UnicodeAtoms = ["\\p{L}", "\\P{Ll}", "[\\p{Lu}a]", "\\u{1F600}", "\\p{ASCII}", "[^\\p{L}]"];

        /// <summary>Classes of strings, valid under <c>v</c> alone, the empty string among them.</summary>
        private static readonly string[] StringClasses =
        [
            "[\\q{ab|a}]", "[\\q{abc|ab|b}x]", "[\\q{abc|b|}]", "[\\q{AB}b]", "[\\q{a|ab|abc}x]", "[\\q{\\uD83D\\uDE00x|k}]",
            "[\\q{ba|a}\\d]",
        ];

        private static readonly string[] Assertions = ["^", "$", "\\b", "\\B"];

        private static readonly string[] Quantifiers = ["*", "*", "+", "+", "?", "{0}", "{1}", "{2}", "{3}", "{0,1}", "{1,2}", "{2,3}", "{0,}", "{2,}", "{0,2}"];

        internal string Pattern()
        {
            var pattern = new StringBuilder();
            if (random.Below(2) == 0)
            {
                // A literal prefix: an atom, a row of atoms, or a run that requires a character, in a group a third of the time.
                var prefix = random.Below(3) switch
                {
                    0 => Atom(),
                    1 => Row(),
                    _ => Atom() + (random.Below(2) == 0 ? "+" : "{2}") + (random.Below(3) == 0 ? "?" : ""),
                };
                if (random.Below(3) == 0)
                {
                    _groups++;
                    prefix = "(" + prefix + ")";
                }

                pattern.Append(prefix);
            }

            pattern.Append(Sequence(0));
            if (random.Below(6) == 0)
                pattern.Append('|').Append(Sequence(0));
            return pattern.ToString();
        }

        private string Atom()
        {
            if (_sets && random.Below(4) == 0)
                return StringClasses[random.Below(StringClasses.Length)];
            if (_unicode && random.Below(4) == 0)
                return UnicodeAtoms[random.Below(UnicodeAtoms.Length)];
            return Atoms[random.Below(Atoms.Length)];
        }

        /// <summary>Two to five atoms in a row: an atom run when none is a class of strings.</summary>
        private string Row() => string.Concat(Enumerable.Range(0, 2 + random.Below(4)).Select(_ => Atom()));

        private string Quantified() => Atom() + Quantifiers[random.Below(Quantifiers.Length)] + (random.Below(3) == 0 ? "?" : "");

        private string Sequence(int depth)
        {
            var terms = new StringBuilder();
            var count = 1 + random.Below(4);
            for (var i = 0; i < count; i++)
                terms.Append(Term(depth));
            return terms.ToString();
        }

        private string Term(int depth)
        {
            var kind = random.Below(20);
            if (depth < 2 && kind < 5)
            {
                var inner = Sequence(depth + 1);
                if (random.Below(4) == 0)
                    inner += "|" + Sequence(depth + 1);
                switch (kind)
                {
                    case 0:
                    case 1:
                        return "(?<" + (random.Below(2) == 0 ? "=" : "!") + inner + ")";
                    case 2:
                        return "(?" + (random.Below(2) == 0 ? "=" : "!") + inner + ")";
                    case 3:
                        _groups++;
                        return "(" + inner + ")" + (random.Below(3) == 0 ? Quantifiers[random.Below(Quantifiers.Length)] : "");
                    default:
                        return "(?:" + inner + ")" + (random.Below(3) == 0 ? Quantifiers[random.Below(Quantifiers.Length)] : "");
                }
            }

            return kind switch
            {
                5 => Assertions[random.Below(Assertions.Length)],
                6 when _groups > 0 => "\\" + (1 + random.Below(_groups)),
                < 12 => Quantified(),
                < 16 => Row(),
                _ => Atom(),
            };
        }
    }

    /// <summary>Runs each case of a population on both programs of its pattern and compares the answers.</summary>
    private sealed class Tally
    {
        internal int Patterns;
        internal int NotPatterns;
        internal int Compared;
        internal int BothOverSteps;
        internal int FastOverSteps;
        internal int GeneralOverSteps;

        /// <summary>The cases stopped under the first ceiling that the second let both programs answer, and compared.</summary>
        internal int ComparedSecond;

        /// <summary>The cases one program or both stopped under the second ceiling too: not compared.</summary>
        internal int StillOverSteps;
        internal int OverFrames;
        internal int OverTrail;
        internal ulong FastUnits;
        internal ulong GeneralUnits;
        internal readonly List<string> Disagreements = [];
        internal readonly List<string> Stopped = [];

        /// <summary>How many programs hold each fast path, and each right to left, and how many have a first test.</summary>
        internal readonly SortedDictionary<string, int> Shapes = new(StringComparer.Ordinal);

        /// <summary>How many pattern-flag pairs hold each construct of <see cref="TestPopulations.ConstructNames"/>.</summary>
        internal readonly SortedDictionary<string, int> Constructs = new(StringComparer.Ordinal);

        private readonly Dictionary<(string, string), (RegexProgram Fast, RegexProgram General)?> _compiled = [];
        private readonly HashSet<(string, string, string, int)> _seen = [];

        private (RegexProgram Fast, RegexProgram General)? Compile(string pattern, string flags)
        {
            RegexProgram? fast;
            RegexProgram? general;
            try
            {
                var parsed = RegexFlagsParser.Parse(flags);
                Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(pattern, parsed, UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out fast));
                Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(pattern, parsed, UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out general, fastPaths: false));
            }
            catch (RegexSyntaxException)
            {
                NotPatterns++;
                return null;
            }

            Patterns++;
            var shapes = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var instruction in fast!.Code)
            {
                if (instruction.Op is RegexOp.GreedyRun or RegexOp.LazyRun or RegexOp.AtomRun or RegexOp.StringClass)
                    shapes.Add(instruction.Op + (instruction.RightToLeft ? " rtl" : ""));
            }

            if (fast.FirstTest >= 0)
                shapes.Add("first test");
            var tree = RegexParser.Parse(pattern, RegexFlagsParser.Parse(flags), new RegexUnicode(UnicodeTables.Instance), new RegexNesting(BroilerRegex.MaximumNestingLimit), out _, out _);
            foreach (var construct in TestPopulations.ConstructsOf(tree, flags))
                Constructs[construct] = Constructs.GetValueOrDefault(construct) + 1;
            foreach (var shape in shapes)
                Shapes[shape] = Shapes.GetValueOrDefault(shape) + 1;
            return (fast, general!);
        }

        internal void Pattern(TestPopulations.PopulationCase row)
        {
            if (!_compiled.TryGetValue((row.Pattern, row.Flags), out var compiled))
            {
                compiled = Compile(row.Pattern, row.Flags);
                _compiled[(row.Pattern, row.Flags)] = compiled;
            }

            if (compiled is not { } programs)
                return;

            var anchored = row.Flags.Contains('y');
            foreach (var subject in row.Subjects)
            {
                foreach (var start in row.Starts(subject).Distinct())
                {
                    if (!_seen.Add((row.Pattern, row.Flags, subject, start)))
                        continue;

                    var fast = Run(programs.Fast, subject, start, anchored, Ceilings);
                    var general = Run(programs.General, subject, start, anchored, Ceilings);
                    var where = $"{row.Origin}: /{TestPopulations.Escape(row.Pattern)}/{row.Flags} on \"{TestPopulations.Escape(subject)}\" from {start}";
                    var fastStopped = fast.Limit == RegexLimit.Steps;
                    var generalStopped = general.Limit == RegexLimit.Steps;
                    OverFrames += (fast.Limit == RegexLimit.Frames ? 1 : 0) + (general.Limit == RegexLimit.Frames ? 1 : 0);
                    OverTrail += (fast.Limit == RegexLimit.Trail ? 1 : 0) + (general.Limit == RegexLimit.Trail ? 1 : 0);
                    if (fastStopped || generalStopped)
                    {
                        _ = (fastStopped, generalStopped) switch
                        {
                            (true, true) => BothOverSteps++,
                            (true, false) => FastOverSteps++,
                            _ => GeneralOverSteps++,
                        };
                        fast = Run(programs.Fast, subject, start, anchored, SecondCeilings);
                        general = Run(programs.General, subject, start, anchored, SecondCeilings);
                        OverFrames += (fast.Limit == RegexLimit.Frames ? 1 : 0) + (general.Limit == RegexLimit.Frames ? 1 : 0);
                        OverTrail += (fast.Limit == RegexLimit.Trail ? 1 : 0) + (general.Limit == RegexLimit.Trail ? 1 : 0);
                        if (fast.Limit == RegexLimit.Steps || general.Limit == RegexLimit.Steps)
                        {
                            StillOverSteps++;
                            if (Stopped.Count < 40)
                                Stopped.Add($"{where}: with the fast paths {fast.Answer} after {fast.Units} units, without {general.Answer} after {general.Units}");
                            continue;
                        }

                        ComparedSecond++;
                    }

                    Compared++;
                    FastUnits += fast.Units;
                    GeneralUnits += general.Units;
                    if (fast.Answer != general.Answer)
                        Disagreements.Add($"{where}: with the fast paths {fast.Answer}, without {general.Answer}");
                }
            }
        }

        private static (string Answer, RegexLimit Limit, ulong Units) Run(RegexProgram program, string subject, int start, bool anchored, RegexMatchCeilings ceilings)
        {
            var slots = new int[program.SlotCount];
            var status = program.Match(subject, start, anchored, slots, null, ceilings, out var limit, out var units);
            return (status == RegexStatus.LimitExceeded ? "LimitExceeded " + limit : MachineTests.Render(status, slots), limit, units);
        }

        public override string ToString() =>
            $"{Patterns} pattern-flag pairs compiled, {NotPatterns} not patterns under their flags; {Compared} cases compared, " +
            $"{FastUnits} units with the fast paths and {GeneralUnits} without; stopped at the step ceiling: {BothOverSteps} with and without them, " +
            $"{FastOverSteps} with them alone, {GeneralOverSteps} without them alone; under the second ceiling {ComparedSecond} of those compared " +
            $"and {StillOverSteps} stopped again; {OverFrames} frame and {OverTrail} trail ceilings reached; " +
            $"{Disagreements.Count} disagreements";
    }
}
