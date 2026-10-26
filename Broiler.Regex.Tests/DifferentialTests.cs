using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Broiler.Regex.Ast;
using Broiler.Regex.Compiling;
using Broiler.Regex.Engine;
using Broiler.Regex.Parsing;
using Broiler.Unicode.Tables;
using Xunit;
using Xunit.Abstractions;

namespace Broiler.Regex.Tests;

/// <summary>
/// The differential test of tasks 2.4a and 2.4b: the new backend and the old one answer the same, index, length and every
/// capture, on two populations over the whole grammar. The <b>suite population</b> is every string literal of this test
/// project's sources taken as a pattern, under each flag string near it and under nine base flag strings, against the
/// literals near each of its occurrences and a fixed set of subjects, from three start indices; the <b>generated population</b> is drawn by a
/// seeded SplitMix64 from every construct the parser accepts, under thirteen flag strings. Since task 2.4b every pattern
/// compiles to a program, and a pattern that does not is a failure; a disagreement is either one of the old backend's
/// recorded defects (<see cref="Defects"/>), each pinned to the specification's answer, or a failure.
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
/// about 241,000 units. Under the ceiling of 100,000, task 2.4a's seed stopped 22 generated cases, the largest compared
/// taking 95,843 units. Task 2.4b's population, drawn from the whole grammar with a seed of its own, stops 2 and its
/// largest compared case takes 25,619 units; the suite's stops are \p{RGI_Emoji}'s (<see cref="SuiteOverStepsPinned"/>).
/// The cases left out are pinned: the stops of each population, at least 170,000 generated cases compared, and no case of
/// either population stopped at a frame or trail ceiling, which on subjects of at most nine pieces would be the new
/// backend giving up rather than answering.
/// </para>
/// <para>
/// <b>The old backend's defects.</b> Task 2.4b's comparison over the whole grammar found two defects of the old
/// backend's besides D1 (<see cref="OldBackendDefectTests"/> pins each). D2: a <c>v</c>-mode class holding the empty
/// string tries it before its single characters, where the specification tries it last; a disagreement is attributed to
/// it when the pattern holds such a class with a single character in it. M7, JSD-0081's row: under <c>u</c> and
/// <c>v</c> a back-reference in a look-behind compares the code units that end at the index rather than the code points,
/// so it can begin inside a surrogate pair; a disagreement is attributed to it when the pattern holds a back-reference in
/// a look-behind, the flags are <c>u</c> or <c>v</c> and the subject holds a surrogate. M7 was found once a pattern that
/// the sources repeat met every subject its occurrences bring, not only those beside its first. Every other disagreement
/// fails the comparison, and there is none.
/// </para>
/// <para>
/// <b>A start inside a surrogate pair, and the old backend's defect D1.</b> Under <c>u</c> and <c>v</c> the new entry
/// reads a start inside a pair forward from the pair's low half, as a lone unit, and read nothing before it until task 2.4b. That is the
/// entry's contract, under which RegExpBuiltinExec's step back to the pair (ECMA-262 §22.2.7.2, step 13.b) is the
/// caller's, as Broiler.VM's realm takes it; it is not the specification's answer from such a <c>lastIndex</c>, which
/// starts at the whole pair (<see cref="SurrogateStartTests"/> holds both). The old backend is inconsistent there, and
/// that is defect D1: its greedy run of one character gives back across the start and reads the high half before it, so
/// <c>/[\s\S]*\u{1F600}/u</c> from index 1 of <c>"\u{1F600}ba"</c> answers a match at 1 of the character at 0,
/// RegExpBuiltinExec's answer by accident, while the same pattern as a loop, <c>/(?:[\s\S])*\u{1F600}/u</c>, does not
/// step back and answers no match. The comparison is of the entry's contract: a case starting inside a pair is compared
/// with the old backend's answer on a copy of the subject whose unit before the start is no surrogate, so that the old
/// backend does not read the pair's high half before the start either. The first run found seven disagreements, all D1.
/// Since task 2.4b's look-behinds the new entry does read before the start, and reads the high half there as a lone
/// unit (the pair is split for the whole call); the copy holds <c>#</c> there instead, so a program with an instruction
/// that reads right to left runs on the copy too, and the split itself is <see cref="MachineLookaroundTests"/>'s to pin.
/// Every other read before the start, <c>\b</c>'s and <c>^</c>'s under <c>m</c>, finds a non-word character that is no
/// line terminator in both, the lone high half and <c>#</c>.
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

    /// <summary>
    /// How many of the suite's cases the step ceiling stops: the catastrophic <c>(a+)+b</c> of ChargingTests' declared
    /// inputs under each flag string on the subjects of twelve to fifteen <c>a</c>s its occurrences bring, and
    /// <c>\p{RGI_Emoji}</c> under <c>v</c> and <c>iv</c> on the subjects of 29 and 31 units beside it, as its strings, tried
    /// one by one in the order the specification gives them, cost about two units each at each start position (task 2.4c's
    /// fast paths).
    /// </summary>
    private const int SuiteOverStepsPinned = 38;

    /// <summary>How many of the seed's generated cases the step ceiling stops: the cases left out of the comparison.</summary>
    private const int OverStepsPinned = 2;

    /// <summary>The generated population's seed: task 2.4b's, the date it was drawn on.</summary>
    private const ulong Seed = 0x2_4B_2026_1026;

    /// <summary>The flag strings every suite pattern runs under, besides those written near it.</summary>
    private static readonly string[] BaseFlags = ["", "i", "m", "s", "u", "iu", "v", "iv", "y"];

    /// <summary>The subjects every suite pattern runs against, besides the literals near it.</summary>
    private static readonly string[] BaseSubjects =
    [
        "", "a", "ab", "aaa", "abcabc", "aab-bba", "a\nb\rc\u2028d\u2029e", "\uD83D\uDE00", "x\uD83Dy\uDE00z",
        "\uD83D\uDE00\uD83D\uDE00a", "123 abc_DEF", "\u0000\u0001", "colour color", "xyzzy",
        "AaBb", "K\u212Ak", "\u017FsS", "a\nA\n",
    ];

    [Fact]
    public void The_suite_s_patterns_answer_the_same_on_both_backends()
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
        foreach (var stopped in tally.Stopped)
            output.WriteLine("  stopped: " + stopped);
        Assert.True(tally.Compared > 10_000, "the suite population compared only " + tally.Compared + " cases");
        Assert.True(tally.NoProgram == 0, tally.NoProgram + " patterns the parser accepts were given no program");
        Assert.True(tally.Disagreements.Count == 0, Report(tally));

        // The suite's cases the step ceiling stops are pinned: the catastrophic (a+)+b on twelve to fifteen a's, and
        // \p{RGI_Emoji} on the two longest subjects beside it; none may stop at a frame or trail ceiling: the subjects are
        // a few characters long, so a stop there is the new backend giving up, not an answer.
        Assert.Equal((SuiteOverStepsPinned, 0, 0), (tally.OverSteps, tally.OverFrames, tally.OverTrail));
        Assert.All(tally.Stopped, static stopped => Assert.Matches(
            @"^(MachineConstructTests\.cs:\d+: /\\p\{RGI_Emoji\}/i?v on |ChargingTests\.cs:\d+: /\(a\+\)\+b/[imsuvy]* on ""a{12,15}c?"" from )", stopped));
    }

    [Fact]
    public void A_seeded_population_over_the_whole_grammar_answers_the_same_on_both_backends()
    {
        var tally = new Tally();
        var random = new SplitMix64(Seed);
        string[] flagChoices = ["", "i", "m", "s", "u", "iu", "us", "v", "iv", "vs", "y", "iy", "imsu"];
        for (var i = 0; i < Generated; i++)
        {
            var flags = flagChoices[random.Below(flagChoices.Length)];
            var generator = new PatternGenerator(random, flags);
            var pattern = generator.Disjunction(0);
            var subjects = Enumerable.Range(0, 8).Select(_ => Subject(random)).Distinct(StringComparer.Ordinal).ToArray();
            var starts = subjects.ToDictionary(static subject => subject, subject => random.Below(subject.Length + 1), StringComparer.Ordinal);
            tally.Pattern(pattern, flags, subjects, subject => [0, starts[subject]], "generated #" + i);
            Assert.True(tally.NoProgram == 0, "a pattern the parser accepts was given no program: /" + pattern + "/" + flags);
        }

        output.WriteLine($"generated population: seed 0x{Seed:X}, {Generated} patterns; {tally}");
        foreach (var stopped in tally.Stopped)
            output.WriteLine("  stopped: " + stopped);
        foreach (var construct in tally.Constructs)
            output.WriteLine($"  {construct.Key}: {construct.Value} pattern-flag pairs");
        Assert.True(tally.Disagreements.Count == 0, Report(tally));

        // Every construct of the grammar is drawn: each kind of node, and each kind of class and look-around.
        Assert.All(ConstructNames, name => Assert.True(tally.Constructs.GetValueOrDefault(name) >= 100, name + " was drawn " + tally.Constructs.GetValueOrDefault(name) + " times"));

        // The cases left out of the comparison are pinned, so that a new backend giving up cannot take the place of one
        // answering: on subjects of at most nine pieces no case may stop at a frame or trail ceiling, the cases the step
        // ceiling stops are this seed's (the most units a compared case takes is under 100,000), and the cases compared
        // keep their floor.
        Assert.Equal((OverStepsPinned, 0, 0), (tally.OverSteps, tally.OverFrames, tally.OverTrail));
        Assert.True(tally.Compared >= 170_000, "the generated population compared only " + tally.Compared + " cases");
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

    /// <summary>Whether a program reads right to left anywhere: whether it holds a look-behind whose body reads a character.</summary>
    private static bool ReadsRightToLeft(RegexProgram program)
    {
        foreach (var instruction in program.Code)
        {
            if (instruction.RightToLeft)
                return true;
        }

        return false;
    }

    private static string Report(Tally tally) =>
        tally.Disagreements.Count + " disagreements:\n" + string.Join('\n', tally.Disagreements.Take(60));

    /// <summary>
    /// An old-backend defect a disagreement is attributed to: its name, as the roadmap's record of task 2.4b gives it, the
    /// test that pins the specification's answer, and the case without which it cannot arise, read from the parsed
    /// pattern, the flags and the subject. A disagreement is attributed to the first defect whose case it is; one that
    /// none explains fails the comparison.
    /// </summary>
    internal sealed record Defect(string Name, string Pin, Func<RegexNode, string, string, bool> Holds);

    /// <summary>The old backend's defects the comparison found, each pinned to the specification's answer.</summary>
    internal static readonly Defect[] Defects =
    [
        new("D2", nameof(OldBackendDefectTests.D2_a_class_s_empty_string_is_tried_after_its_single_characters),
            static (tree, _, _) => Any(tree, static node => node is CharClassNode { Set: { MatchesEmptyString: true, CodePoints.IsEmpty: false } })),
        new("M7", nameof(OldBackendDefectTests.M7_a_backward_back_reference_reads_whole_code_points_under_u),
            static (tree, flags, subject) => (flags.Contains('u') || flags.Contains('v')) && subject.Any(char.IsSurrogate)
                && Any(tree, static node => node is LookaroundNode { Behind: true } look && Any(look.Child, static inner => inner is BackreferenceNode))),
    ];

    /// <summary>Whether some node of <paramref name="tree"/> satisfies <paramref name="test"/>.</summary>
    private static bool Any(RegexNode tree, Func<RegexNode, bool> test) => Nodes(tree).Any(test);

    /// <summary>The constructs of the grammar the generated population must each draw, as <see cref="ConstructsOf"/> names them.</summary>
    private static readonly string[] ConstructNames =
    [
        "alternation", "quantifier", "capture group", "named group", "class", "class under i", "class with strings",
        "class with the empty string", "character under i", "^ or $", "^ or $ under m", "\\b or \\B", "back-reference",
        "named back-reference", "look-ahead", "negative look-ahead", "look-behind", "negative look-behind", "modifier group",
        "back-reference in a look-behind", "property escape",
    ];

    /// <summary>The constructs of <see cref="ConstructNames"/> that <paramref name="tree"/>, parsed under <paramref name="flags"/>, holds.</summary>
    internal static IEnumerable<string> ConstructsOf(RegexNode tree, string flags)
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);
        var ignoreCase = flags.Contains('i');
        var multiline = flags.Contains('m');
        foreach (var node in Nodes(tree))
        {
            switch (node)
            {
                case DisjunctionNode:
                    found.Add("alternation");
                    break;
                case QuantifierNode:
                    found.Add("quantifier");
                    break;
                case GroupNode { IsCapturing: true } group:
                    found.Add(group.Name is null ? "capture group" : "named group");
                    break;
                case CharClassNode cls:
                    found.Add(cls.Set.HasStrings ? "class with strings" : "class");
                    if (cls.Set.MatchesEmptyString)
                        found.Add("class with the empty string");
                    if (ignoreCase)
                        found.Add("class under i");
                    if (cls.Set.UsesPropertyEscape)
                        found.Add("property escape");
                    break;
                case CharNode when ignoreCase:
                    found.Add("character under i");
                    break;
                case AnchorNode { Kind: AnchorKind.StartOfInput or AnchorKind.EndOfInput }:
                    found.Add(multiline ? "^ or $ under m" : "^ or $");
                    break;
                case AnchorNode:
                    found.Add("\\b or \\B");
                    break;
                case BackreferenceNode reference:
                    found.Add(reference.Name is null ? "back-reference" : "named back-reference");
                    break;
                case LookaroundNode look:
                    found.Add((look.Negative ? "negative " : "") + (look.Behind ? "look-behind" : "look-ahead"));
                    if (look.Behind && Any(look.Child, static inner => inner is BackreferenceNode))
                        found.Add("back-reference in a look-behind");
                    break;
                case ModifierGroupNode:
                    found.Add("modifier group");
                    break;
            }
        }

        return found;
    }

    /// <summary>Every node of <paramref name="tree"/>, read on a work list.</summary>
    private static IEnumerable<RegexNode> Nodes(RegexNode tree)
    {
        var pending = new Stack<RegexNode>();
        pending.Push(tree);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            yield return node;
            switch (node)
            {
                case SequenceNode sequence:
                    foreach (var term in sequence.Terms)
                        pending.Push(term);
                    break;
                case DisjunctionNode disjunction:
                    foreach (var alternative in disjunction.Alternatives)
                        pending.Push(alternative);
                    break;
                case GroupNode group:
                    pending.Push(group.Child);
                    break;
                case ModifierGroupNode modifier:
                    pending.Push(modifier.Child);
                    break;
                case LookaroundNode look:
                    pending.Push(look.Child);
                    break;
                case QuantifierNode quantifier:
                    pending.Push(quantifier.Child);
                    break;
            }
        }
    }

    private static bool LooksLikeFlags(string text) =>
        text.Length <= 8 && text.All(static c => "dgimsuvy".Contains(c)) && text.Distinct().Count() == text.Length
        && !(text.Contains('u') && text.Contains('v'));

    /// <summary>Runs one pattern under one flag string against every subject from every start the chooser names.</summary>
    private sealed class Tally
    {
        internal int Patterns;
        /// <summary>Patterns the parser accepts that the new compiler gave no program: none since task 2.4b.</summary>
        internal int NoProgram;

        /// <summary>How many pattern-flag pairs hold each construct of <see cref="ConstructNames"/>.</summary>
        internal readonly SortedDictionary<string, int> Constructs = new(StringComparer.Ordinal);
        internal int NotPatterns;
        internal int Compared;
        internal int OverSteps;
        internal int OverFrames;
        internal int OverTrail;
        internal int OldOverflow;
        internal int InsidePair;
        internal ulong MostUnits;
        internal readonly List<string> Disagreements = [];

        /// <summary>The disagreements attributed to each recorded defect of the old backend, by its name.</summary>
        internal readonly SortedDictionary<string, int> Explained = new(StringComparer.Ordinal);

        /// <summary>The first cases a ceiling stopped, for the report.</summary>
        internal readonly List<string> Stopped = [];
        private readonly HashSet<(string, string, string, int)> _seen = [];

        /// <summary>Each pattern-flag pair met, with its two compilations and its tree, or null when it is no pattern.</summary>
        private readonly Dictionary<(string, string), (BroilerRegex Old, RegexProgram Program, RegexNode Tree)?> _compiled = [];

        /// <summary>Compiles a pattern-flag pair met for the first time on both backends, and counts it.</summary>
        private (BroilerRegex Old, RegexProgram Program, RegexNode Tree)? Compile(string pattern, string flags)
        {
            BroilerRegex old;
            RegexProgram? program;
            try
            {
                old = TestRegex.Compile(pattern, flags);
                if (ProgramCompiler.Compile(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out program) != ProgramCompilation.Compiled)
                {
                    NoProgram++;
                    return null;
                }
            }
            catch (RegexSyntaxException)
            {
                NotPatterns++;
                return null;
            }

            Patterns++;
            var tree = RegexParser.Parse(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance, out _);
            foreach (var construct in ConstructsOf(tree, flags))
                Constructs[construct] = Constructs.GetValueOrDefault(construct) + 1;
            return (old, program!, tree);
        }

        /// <summary>
        /// Runs <paramref name="pattern"/> under <paramref name="flags"/> against <paramref name="subjects"/>. A pattern
        /// met again, as the suite's sources repeat one beside other subjects, is compiled once and runs against every
        /// subject any occurrence brings; a case already compared is not compared again.
        /// </summary>
        internal void Pattern(string pattern, string flags, string[] subjects, Func<string, int[]> starts, string origin)
        {
            if (!_compiled.TryGetValue((pattern, flags), out var compiled))
            {
                compiled = Compile(pattern, flags);
                _compiled[(pattern, flags)] = compiled;
            }

            if (compiled is not { } pair)
                return;

            var (old, program, tree) = pair;

            var anchored = flags.Contains('y');
            var unicode = flags.Contains('u') || flags.Contains('v');
            var slots = new int[program.SlotCount];
            var readsBackward = ReadsRightToLeft(program);
            foreach (var subject in subjects)
            {
                foreach (var start in starts(subject).Distinct())
                {
                    if (!_seen.Add((pattern, flags, subject, start)))
                        continue;

                    // A start inside a surrogate pair is compared under the entry's contract, on a copy whose unit before
                    // the start is no surrogate, so that the old backend (defect D1) does not read the pair's high half
                    // either; a look-behind reads the copy on both backends.
                    var oldSubject = subject;
                    var newSubject = subject;
                    if (unicode && start > 0 && start < subject.Length && char.IsLowSurrogate(subject[start]) && char.IsHighSurrogate(subject[start - 1]))
                    {
                        oldSubject = string.Concat(subject.AsSpan(0, start - 1), "#", subject.AsSpan(start));
                        if (readsBackward)
                            newSubject = oldSubject;
                        InsidePair++;
                    }

                    var status = program.Match(newSubject, start, anchored, slots, null, Ceilings, out var limit, out var units);
                    if (status == RegexStatus.LimitExceeded)
                    {
                        if (Stopped.Count < 40)
                            Stopped.Add($"{origin}: /{Escape(pattern)}/{flags} on \"{Escape(subject)}\" from {start}: {limit} after {units} units");
                        _ = limit switch
                        {
                            RegexLimit.Steps => OverSteps++,
                            RegexLimit.Frames => OverFrames++,
                            RegexLimit.Trail => OverTrail++,
                            _ => throw new InvalidOperationException("a limit exceeded naming " + limit),
                        };
                        continue;
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
                    if (expected == actual)
                        continue;

                    var defect = Defects.FirstOrDefault(d => d.Holds(tree, flags, subject));
                    if (defect is null)
                        Disagreements.Add($"{origin}: /{Escape(pattern)}/{flags} on \"{Escape(subject)}\" from {start}: old {expected}, new {actual}");
                    else
                        Explained[defect.Name] = Explained.GetValueOrDefault(defect.Name) + 1;
                }
            }
        }

        public override string ToString() =>
            $"{Patterns} pattern-flag pairs compiled, {NoProgram} given no program, {NotPatterns} not patterns under their flags; " +
            $"{Compared} cases compared ({InsidePair} starting inside a surrogate pair), the most units {MostUnits}; " +
            $"{OverSteps} stopped at the new backend's step ceiling, {OverFrames} at its frame ceiling, {OverTrail} at its trail ceiling, " +
            $"{OldOverflow} the old backend's stack guard refused; " +
            $"{Disagreements.Count} disagreements unexplained" +
            (Explained.Count == 0 ? "" : ", " + string.Join(", ", Explained.Select(static e => e.Value + " the old backend's defect " + e.Key)));
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
        string[] pieces = ["a", "a", "b", "b", "c", "x", "\n", "\uD83D\uDE00", "\uD83D", "\uDE00", "1", " ", "A", "B", "K", "\u212A", "\u017F", "s", "S", "ab"];
        var subject = new StringBuilder();
        var length = random.Below(9);
        for (var i = 0; i < length; i++)
            subject.Append(pieces[random.Below(pieces.Length)]);
        return subject.ToString();
    }

    /// <summary>
    /// A pattern of the whole grammar under <paramref name="flags"/>: literals with and without a case, the dot, classes of
    /// every kind the flags admit (properties under <c>u</c> and <c>v</c>, set operations and strings under <c>v</c>), the
    /// assertions <c>^ $ \b \B</c>, numbered and named back-references, groups of every kind (capturing, non-capturing,
    /// named, the four look-arounds and modifier groups), alternation and every quantifier form. A draw the parser refuses
    /// under the flags, such as a reference to a group the pattern does not hold under <c>u</c>, is counted as no pattern.
    /// </summary>
    private sealed class PatternGenerator(SplitMix64 random, string flags)
    {
        private readonly bool _unicode = flags.Contains('u') || flags.Contains('v');
        private readonly bool _sets = flags.Contains('v');
        private int _names;
        private int _groups;

        private static readonly string[] Atoms =
        [
            "a", "a", "b", "b", "c", "x", "A", "K", "s", ".", "\\d", "\\w", "\\W", "\\s", "\\n", "[ab]", "[^a]", "[a-c]",
            "[\\d\\n]", "[^\\n]", "[\\s\\S]", "[]", "[^]", "\uD83D\uDE00", "\\uD83D\\uDE00", "\\uD83D", "[\uD83D\uDE00a]",
            "[^\uD83D\uDE00]", "\\-", "\u017F", "\u212A", "[a-z]", "[^k]", "[A-Z]", "[\\w-]",
        ];

        /// <summary>Atoms valid under <c>u</c> and <c>v</c> alone.</summary>
        private static readonly string[] UnicodeAtoms = ["\\p{L}", "\\P{Ll}", "[\\p{Lu}a]", "\\u{1F600}", "[^\\p{L}]", "\\p{ASCII}"];

        /// <summary>Atoms valid under <c>v</c> alone: set operations, nested classes and strings, the empty one among them.</summary>
        private static readonly string[] SetAtoms =
        [
            "[\\q{ab|a}]", "[\\q{}a]", "[\\q{abc|b|}]", "[[a-z]--[b]]", "[\\p{L}&&\\p{Ll}]", "[\\q{\\uD83D\\uDE00x|k}]",
            "[\\q{AB}b]", "[[ab][\\q{ba}]]", "[\\q{a|ab|abc}x]",
        ];

        private static readonly string[] Assertions = ["^", "$", "\\b", "\\B"];

        private static readonly string[] Quantifiers = ["*", "+", "?", "{0}", "{1}", "{2}", "{0,1}", "{1,2}", "{2,3}", "{0,}", "{2,}", "{0,2}"];

        private static readonly string[] Modifiers = ["(?i:", "(?-i:", "(?m:", "(?s:", "(?i-s:", "(?-m:", "(?is:"];

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
                var (atom, quantifiable) = Term(depth);
                terms.Append(atom);
                if (quantifiable && random.Below(2) == 0)
                {
                    terms.Append(Quantifiers[random.Below(Quantifiers.Length)]);
                    if (random.Below(3) == 0)
                        terms.Append('?');
                }
            }
            return terms.ToString();
        }

        /// <summary>One term, and whether a quantifier may follow it (a look-behind or an assertion may not).</summary>
        private (string Atom, bool Quantifiable) Term(int depth)
        {
            if (depth < 3 && random.Below(3) == 0)
            {
                var kind = random.Below(12);
                var open = kind switch
                {
                    0 or 1 => "(",
                    2 => "(?:",
                    3 => "(?<n" + _names++ + ">",
                    4 => "(?=",
                    5 => "(?!",
                    6 or 7 => "(?<=",
                    8 => "(?<!",
                    _ => Modifiers[random.Below(Modifiers.Length)],
                };
                if (kind is 0 or 1 or 3)
                    _groups++;
                var inner = Disjunction(depth + 1);

                // A look-ahead is quantifiable outside u and v alone (Annex B), a look-behind never.
                return (open + inner + ")", kind is not (6 or 7 or 8) && (!_unicode || kind is not (4 or 5)));
            }

            switch (random.Below(10))
            {
                case 0:
                    return (Assertions[random.Below(Assertions.Length)], false);
                case 1 when _groups > 0:
                    return ("\\" + (1 + random.Below(_groups)), true);
                case 2 when _names > 0:
                    return ("\\k<n" + random.Below(_names) + ">", true);
                case 3 when _sets:
                    return (SetAtoms[random.Below(SetAtoms.Length)], true);
                case 4 when _unicode:
                    return (UnicodeAtoms[random.Below(UnicodeAtoms.Length)], true);
                default:
                    return (Atoms[random.Below(Atoms.Length)], true);
            }
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
