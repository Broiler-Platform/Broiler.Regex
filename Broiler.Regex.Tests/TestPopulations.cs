using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Broiler.Regex.Ast;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The populations the differential tests run over: tasks 2.4a's and 2.4b's, on which the new backend was compared
/// with the old one until task 2.4c deleted the old, and on which task 2.4c compares the new backend's programs with and
/// without their fast paths (<see cref="FastPathDifferentialTests"/>). The <b>suite population</b> is every string
/// literal of this test project's sources taken as a pattern, under each flag string near it and under nine base flag
/// strings, against the literals near each of its occurrences and a fixed set of subjects, from three start indices;
/// the <b>generated population</b> is drawn by a seeded SplitMix64 from every construct the parser accepts, under
/// thirteen flag strings, task 2.4b's seed.
/// </summary>
/// <remarks>
/// <para>
/// <b>What the comparison with the old backend found</b>, before task 2.4c retired it (the roadmap's records of tasks
/// 2.4a, 2.4b and 2.4c): every disagreement was one of the old backend's defects, each now a test pinned to the
/// specification's answer on the new backend. D1, a start inside a surrogate pair that its greedy run stepped back across
/// and its loop did not (<see cref="SurrogateStartTests"/>); D2, a <c>v</c>-mode class that tried its empty string
/// before its single characters; and JSD-0081's row M7, a back-reference in a look-behind that compared the code units
/// ending at the index rather than the code points under <c>u</c> (<see cref="OldBackendDefectTests"/>). At its last
/// run, with task 2.4c's fast paths, the comparison stopped only <c>(a+)+b</c>'s 26 suite cases at its step ceiling of
/// 100,000 units and attributed 207 suite disagreements and 57 generated ones to D2 and 8 to M7, none unexplained.
/// </para>
/// <para>
/// The test sources the suite population reads leave out the files of the differential tests and this one, whose
/// literals are the generators' own.
/// </para>
/// </remarks>
internal static class TestPopulations
{
    /// <summary>The generated population's size, in patterns, each against up to eight subjects from two starts.</summary>
    internal const int Generated = 20_000;

    /// <summary>The generated population's seed: task 2.4b's, the date it was drawn on.</summary>
    internal const ulong Seed = 0x2_4B_2026_1026;

    /// <summary>The flag strings every suite pattern runs under, besides those written near it.</summary>
    private static readonly string[] BaseFlags = ["", "i", "m", "s", "u", "iu", "v", "iv", "y"];

    /// <summary>The subjects every suite pattern runs against, besides the literals near it.</summary>
    private static readonly string[] BaseSubjects =
    [
        "", "a", "ab", "aaa", "abcabc", "aab-bba", "a\nb\rc\u2028d\u2029e", "\uD83D\uDE00", "x\uD83Dy\uDE00z",
        "\uD83D\uDE00\uD83D\uDE00a", "123 abc_DEF", "\u0000\u0001", "colour color", "xyzzy",
        "AaBb", "K\u212Ak", "\u017FsS", "a\nA\n",
    ];

    /// <summary>
    /// One pattern of a population under one flag string, the subjects it runs against, the starts it runs from in each,
    /// and where it came from.
    /// </summary>
    internal sealed record PopulationCase(string Pattern, string Flags, string[] Subjects, Func<string, int[]> Starts, string Origin);

    /// <summary>
    /// The suite population over <paramref name="sources"/>: each string literal as a pattern, under the base flag strings
    /// and those near it, against the literals near it and the base subjects, from the start, index 1 and the end.
    /// </summary>
    internal static IEnumerable<PopulationCase> SuitePopulation(string[] sources)
    {
        foreach (var file in sources)
        {
            var literals = Literals(File.ReadAllText(file)).ToArray();
            foreach (var (line, pattern) in literals)
            {
                var near = literals.Where(other => other.Line >= line && other.Line <= line + 3).Select(static other => other.Text).ToArray();
                var flags = BaseFlags.Concat(near.Where(LooksLikeFlags)).Distinct(StringComparer.Ordinal);
                var subjects = near.Concat(BaseSubjects).Distinct(StringComparer.Ordinal).ToArray();
                foreach (var flag in flags)
                    yield return new(pattern, flag, subjects, static subject => subject.Length == 0 ? [0] : [0, 1, subject.Length], Path.GetFileName(file) + ":" + line);
            }
        }
    }

    /// <summary>The generated population of task 2.4b's seed: <see cref="Generated"/> patterns, each against up to eight subjects from the start and one other.</summary>
    internal static IEnumerable<PopulationCase> SeededPopulation()
    {
        var random = new SplitMix64(Seed);
        string[] flagChoices = ["", "i", "m", "s", "u", "iu", "us", "v", "iv", "vs", "y", "iy", "imsu"];
        for (var i = 0; i < Generated; i++)
        {
            var flags = flagChoices[random.Below(flagChoices.Length)];
            var generator = new PatternGenerator(random, flags);
            var pattern = generator.Disjunction(0);
            var subjects = Enumerable.Range(0, 8).Select(_ => Subject(random)).Distinct(StringComparer.Ordinal).ToArray();
            var starts = subjects.ToDictionary(static subject => subject, subject => random.Below(subject.Length + 1), StringComparer.Ordinal);
            yield return new(pattern, flags, subjects, subject => [0, starts[subject]], "generated #" + i);
        }
    }

    /// <summary>Whether some node of <paramref name="tree"/> satisfies <paramref name="test"/>.</summary>
    private static bool Any(RegexNode tree, Func<RegexNode, bool> test) => Nodes(tree).Any(test);

    /// <summary>The constructs of the grammar the generated population must each draw, as <see cref="ConstructsOf"/> names them.</summary>
    internal static readonly string[] ConstructNames =
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
    internal static string Escape(string text)
    {
        var escaped = new StringBuilder();
        foreach (var c in text)
            escaped.Append(c is < ' ' or > '~' ? $"\\u{(int)c:X4}" : c.ToString());
        return escaped.ToString();
    }

    internal static string Subject(SplitMix64 random)
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
    internal sealed class PatternGenerator(SplitMix64 random, string flags)
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
    internal sealed class SplitMix64(ulong state)
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

    /// <summary>This test project's C# sources, the differential tests' files and this one left out.</summary>
    internal static string[] TestSources()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Broiler.Regex.slnx")))
            directory = directory.Parent;
        Assert.True(directory != null, "no Broiler.Regex.slnx above " + AppContext.BaseDirectory);
        return Directory.GetFiles(Path.Combine(directory!.FullName, "Broiler.Regex.Tests"), "*.cs")
            .Where(static file => !Path.GetFileName(file).EndsWith("DifferentialTests.cs", StringComparison.Ordinal) && Path.GetFileName(file) != "TestPopulations.cs")
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
