using System;
using System.Linq;
using Broiler.Regex.Engine;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Requirement R6, ES2025's duplicate named groups (bundle <c>sjl-11-001</c>'s check A-6): "a name repeated only in
/// alternatives that cannot both participate accepted, and refused otherwise, after escapes are decoded;
/// <c>\k&lt;name&gt;</c> matches the participating group; a result maps the name to the participating index". The
/// early error is ECMA-262 §22.2.1.1's, over MightBothParticipate (§22.2.1.4): two GroupSpecifiers may share a name only
/// when a Disjunction holds them in different Alternatives. <c>\k</c> reads every group of its name (§22.2.2.7, over
/// GroupSpecifiersThatMatch §22.2.1.9) and compares the one whose capture is defined (BackreferenceMatcher, §22.2.2.7.2),
/// and RegExpBuiltinExec gives the name that group's value (§22.2.7.2, step 33).
/// </summary>
public class DuplicateNamedGroupTests
{
    /// <summary>Every mode a pattern is read in, as <see cref="GroupNameTests.Modes"/>.</summary>
    public static readonly TheoryData<string> Modes = GroupNameTests.Modes;

    /// <summary>Names repeated only in Alternatives that cannot both participate, each with the groups of its first name.</summary>
    public static readonly (string Pattern, int[] Groups)[] Admitted =
    [
        ("(?<a>x)|(?<a>y)", [1, 2]),
        (@"(?<\u0061>x)|(?<a>y)", [1, 2]),
        (@"(?<a>x)|(?<\u{61}>y)", [1, 2]),
        ("(?<\U0001D400>x)|(?<\\uD835\\uDC00>y)", [1, 2]),
        ("(?<a>x)|(?<a>y)|(?<a>z)", [1, 2, 3]),
        ("(?:(?<a>x)|(?<a>y))\\k<a>", [1, 2]),
        ("(?:(?<a>x)|(?<a>y))|(?<a>z)", [1, 2, 3]),
        ("(?<a>x)|(?:(?<a>y)|(?<a>z))", [1, 2, 3]),
        ("(?:(?:(?<a>x)|b)|(?<a>y))", [1, 2]),
        ("((?<a>x)|(?<a>y))*", [2, 3]),
        ("(?=(?<a>x))|(?<=(?<a>y))", [1, 2]),
        ("(?<!(?<a>x))|(?!(?<a>y))", [1, 2]),
        ("(?i:(?<a>x))|(?-i:(?<a>y))", [1, 2]),
        ("(?<a>x)|(?<b>y)(?<a>z)", [1, 3]),
        ("(?:(?<x>a)|(?<y>a)(?<x>b))(?:(?<z>c)|(?<z>d))", [1, 3]),
        ("(?:(?:(?<a>x)|(?<a>y)|(a)|(?<b>b)|(?<a>z))\\k<a>){3}", [1, 2, 5]),
        ("^(?:(?<a>x)|(?<a>y)|z){2}\\k<a>$", [1, 2]),
        ("\\k<a>(?:(?<a>x)|(?<a>y))", [1, 2]),
        ("(?<a>x)|(?:zy\\k<a>)", [1]),
    ];

    /// <summary>Names repeated where both groups might participate, each with the position of the group refused.</summary>
    public static readonly (string Pattern, int Refused)[] Refused =
    [
        ("(?<a>x)(?<a>y)", 7),
        (@"(?<a>x)(?<\u0061>y)", 7),
        ("(?:(?<a>x)|(?<a>y))(?<a>z)", 19),
        ("(?<a>x|(?<a>y))", 7),
        ("(?<a>(?<a>x))", 5),
        ("(?:(?<a>x)|b)(?:(?<a>y)|c)", 16),
        ("(?<a>x)|(?<a>y)(?<a>z)", 15),
        (@"(?<a>x)|(?<\u0061>y)(?<a>z)", 20),
        ("((?<a>x)|(?<a>y))(?<a>z)*", 17),
        ("(?=(?<a>x))(?<a>y)", 11),
        ("(?<a>x)(?:|(?<a>y))", 11),
        ("(?:(?<a>x)|(?<a>y))|(?<a>z)(?<a>w)", 27),
        ("(?:(?<a>x)|(?<a>y))(?:(?<a>z)|w)", 22),
        ("(?<a>x)|(?<b>y)(?<b>z)", 15),
    ];

    [Theory]
    [MemberData(nameof(Modes))]
    public void A_name_repeated_only_across_alternatives_that_cannot_both_participate_is_admitted(string flags)
    {
        foreach (var (pattern, groups) in Admitted)
        {
            var exception = Record.Exception(() => TestRegex.Compile(pattern, flags));
            Assert.True(exception is null, pattern + " /" + flags + ": " + exception?.Message);
            var names = TestRegex.Compile(pattern, flags).GroupNames;
            Assert.Equal(groups, names.GroupsNamed(names.NameAt(0)).ToArray());
            Assert.All(groups, g => Assert.Equal(names.NameAt(0), names.NameOfGroup(g)));
        }
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void A_name_repeated_where_both_groups_might_participate_is_refused_at_the_later_group(string flags)
    {
        foreach (var (pattern, refused) in Refused)
        {
            var exception = Record.Exception(() => TestRegex.Compile(pattern, flags));
            var syntax = Assert.IsType<RegexSyntaxException>(exception);
            Assert.True(refused == syntax.Position, pattern + " /" + flags + ": refused at " + syntax.Position);
            Assert.Equal("(?<", pattern.Substring(refused, 3));
        }
    }

    /// <summary>
    /// The parser's rule against MightBothParticipate as §22.2.1.4 states it, over a seeded population of generated trees:
    /// each pattern is rendered from a tree of Disjunctions, Alternatives and groups of every kind (capturing, named over
    /// three names, non-capturing, the four look-arounds, quantified), and the oracle reads the tree, not the text: two
    /// groups of one name might both participate unless some Disjunction holds them in different Alternatives. A pattern
    /// is refused exactly when some later group meets an earlier one of its name that way, and at the first such group.
    /// </summary>
    [Fact]
    public void The_rule_agrees_with_MightBothParticipate_over_a_seeded_population_of_trees()
    {
        var random = new Random(20261027);
        var admitted = 0;
        var refused = 0;
        for (var i = 0; i < 20_000; i++)
        {
            var tree = new GeneratedPattern(random);
            var exception = Record.Exception(() => TestRegex.Compile(tree.Text));
            if (tree.FirstConflict is { } position)
            {
                var syntax = Assert.IsType<RegexSyntaxException>(exception);
                Assert.True(position == syntax.Position, tree.Text + ": refused at " + syntax.Position + ", expected " + position);
                refused++;
            }
            else
            {
                Assert.True(exception is null, tree.Text + ": " + exception?.Message);
                admitted++;
            }
        }

        // Neither side is vacuous.
        Assert.True(admitted > 2_000 && refused > 2_000, admitted + " admitted, " + refused + " refused");
    }

    /// <summary>A pattern generated as a tree, rendered to text, and the source position of its first refused group by the tree's own reading of MightBothParticipate.</summary>
    private sealed class GeneratedPattern
    {
        private readonly Random _random;
        private readonly System.Text.StringBuilder _text = new();
        private readonly System.Collections.Generic.List<(string Name, int Position, (int Disjunction, int Alternative)[] Path)> _named = [];
        private readonly System.Collections.Generic.List<(int Disjunction, int Alternative)> _path = [];
        private int _disjunctions;

        internal GeneratedPattern(Random random)
        {
            _random = random;
            Disjunction(depth: 0);
            Text = _text.ToString();
            foreach (var (name, position, path) in _named)
            {
                if (_named.Any(e => e.Name == name && e.Position < position && MightBothParticipate(e.Path, path)))
                {
                    FirstConflict = position;
                    break;
                }
            }
        }

        internal string Text { get; }

        internal int? FirstConflict { get; }

        /// <summary>
        /// §22.2.1.4 over the two groups' paths of (Disjunction, Alternative): false when, where the paths part, they are in
        /// two Alternatives of the same Disjunction; true when one path continues the other (one group holds the other) or
        /// they part into different Disjunctions inside one Alternative.
        /// </summary>
        private static bool MightBothParticipate((int Disjunction, int Alternative)[] x, (int Disjunction, int Alternative)[] y)
        {
            for (var k = 0; k < Math.Min(x.Length, y.Length); k++)
            {
                if (x[k] != y[k])
                    return x[k].Disjunction != y[k].Disjunction;
            }

            return true;
        }

        private void Disjunction(int depth)
        {
            var id = _disjunctions++;
            var alternatives = depth > 3 ? 1 : _random.Next(1, 4);
            for (var a = 0; a < alternatives; a++)
            {
                if (a > 0)
                    _text.Append('|');
                _path.Add((id, a));
                var terms = depth > 3 ? _random.Next(0, 2) : _random.Next(0, 4);
                for (var t = 0; t < terms; t++)
                    Term(depth);
                _path.RemoveAt(_path.Count - 1);
            }
        }

        private void Term(int depth)
        {
            var kind = depth > 3 ? 0 : _random.Next(0, 10);
            if (kind <= 1)
            {
                _text.Append('x');
                return;
            }

            var open = _text.Length;
            switch (kind)
            {
                case 2:
                    _text.Append('(');
                    break;
                case 3:
                    _text.Append("(?:");
                    break;
                case 4:
                    _text.Append(_random.Next(0, 4) switch { 0 => "(?=", 1 => "(?!", 2 => "(?<=", _ => "(?<!" });
                    break;
                default:
                    var name = ((char)('a' + _random.Next(0, 3))).ToString();
                    _named.Add((name, open, [.. _path]));
                    _text.Append("(?<").Append(name).Append('>');
                    break;
            }

            Disjunction(depth + 1);
            _text.Append(')');
            if (kind != 4 && _random.Next(0, 4) == 0)
                _text.Append('*');
        }
    }

    [Fact]
    public void A_name_is_compared_as_decoded_so_differently_spelled_names_that_decode_alike_are_one_name()
    {
        var names = TestRegex.Compile(@"(?<\u{1D400}>x)|(?<" + "\U0001D400" + @">y)|(?<\uD835\uDC00>z)").GroupNames;
        Assert.Equal(1, names.Count);
        Assert.Equal([1, 2, 3], names.GroupsNamed("\U0001D400").ToArray());
    }

    /// <summary>The match of <paramref name="pattern"/> on <paramref name="subject"/> in the form <see cref="TestRegex.Render"/> gives.</summary>
    [Theory]
    [InlineData("(?:(?<a>x)|(?<a>y))\\k<a>", "", "yy", "match 0,2 - 0,1")]
    [InlineData("(?:(?<a>x)|(?<a>y))\\k<a>", "", "xx", "match 0,2 0,1 -")]
    [InlineData("(?:(?<a>x)|(?<a>y))\\k<a>", "", "xyyx", "match 1,3 - 1,2")]
    [InlineData("(?:(?<a>x)|(?<a>y))\\k<a>", "", "xyx", "nomatch")]
    [InlineData("(?:(?<a>x)|(?<a>y))\\k<a>", "i", "yY", "match 0,2 - 0,1")]
    [InlineData("(?:(?<a>x)|(?<a>y))\\k<a>", "iu", "Xx", "match 0,2 0,1 -")]
    [InlineData("(?:(?<a>\\u{1F600})|(?<a>y))\\k<a>", "u", "\U0001F600\U0001F600", "match 0,4 0,2 -")]
    [InlineData("(?:(?<a>\\u{1F600})|(?<a>y))\\k<a>", "v", "y\U0001F600y", "nomatch")]
    [InlineData("\\k<a>(?:(?<a>x)|(?<a>y))", "", "y", "match 0,1 - 0,1")]
    [InlineData("(?<=\\k<a>(?:(?<a>x)|(?<a>y)))z", "", "yyz", "match 2,3 - 1,2")]
    [InlineData("(?<=\\k<a>(?:(?<a>x)|(?<a>y)))z", "", "xyz", "nomatch")]
    [InlineData("(?<=\\k<a>(?:(?<a>x)|(?<a>y)))z", "i", "Yyz", "match 2,3 - 1,2")]
    [InlineData("(?:(?:(?<x>a)|(?<x>b))\\k<x>){2}", "", "aabb", "match 0,4 - 2,3")]
    [InlineData("(?:(?:(?<x>a)|(?<x>b))\\k<x>){2}", "", "abab", "nomatch")]
    [InlineData("^(?:(?<a>x)|(?<a>y)|z){2}\\k<a>$", "", "xz", "match 0,2 - -")]
    [InlineData("^(?:(?<a>x)|(?<a>y)|z){2}\\k<a>$", "", "xzx", "nomatch")]
    [InlineData("(?:(?<a>x)|(?<a>y)|z)+\\k<a>", "", "zyy", "match 0,3 - 1,2")]
    [InlineData("(?:(?=(?<a>x))|(?<a>y))\\k<a>x", "", "xx", "match 0,2 0,1 -")]
    public void A_reference_to_a_duplicated_name_matches_the_group_that_participated(string pattern, string flags, string subject, string expected)
    {
        var regex = TestRegex.Compile(pattern, flags);
        Assert.Equal(expected, TestRegex.Answer(regex, subject, 0));

        // The offset entry answers the same, and the name maps to the group whose slots the match wrote.
        var slots = new int[regex.SlotCount];
        var status = regex.Match(subject, 0, false, slots, null, RegexMatchCeilings.Engines, out _);
        Assert.Equal(expected == "nomatch" ? RegexStatus.NoMatch : RegexStatus.Match, status);
        if (status == RegexStatus.Match)
        {
            var name = regex.GroupNames.NameAt(0);
            var participating = regex.GroupNames.ParticipatingGroup(name, slots);
            var written = regex.GroupNames.GroupsNamed(name).ToArray().Where(g => slots[2 * g] >= 0).ToArray();
            Assert.Equal(written.Length == 0 ? 0 : written.Single(), participating);
        }
    }

    [Fact]
    public void A_result_maps_each_name_to_the_group_that_participated_and_lists_the_names_in_source_order()
    {
        // test262's built-ins/RegExp/prototype/exec/duplicate-named-groups-properties.js and the enumeration-order file.
        var regex = TestRegex.Compile("(?:(?<x>a)|(?<y>a)(?<x>b))(?:(?<z>c)|(?<z>d))");
        Assert.Equal(new[] { "x", "y", "z" }, Enumerable.Range(0, regex.GroupNames.Count).Select(regex.GroupNames.NameAt));

        var abc = regex.Match("abc");
        Assert.Equal(new[] { "x", "y", "z" }, abc.NamedGroups.Keys);
        Assert.Equal((3, "b"), (abc.NamedGroups["x"].Number, abc.NamedGroups["x"].Value));
        Assert.Equal((2, "a"), (abc.NamedGroups["y"].Number, abc.NamedGroups["y"].Value));
        Assert.Equal((4, "c"), (abc.NamedGroups["z"].Number, abc.NamedGroups["z"].Value));
        Assert.Same(abc.Groups[3], abc.NamedGroups["x"]);

        var ad = regex.Match("ad");
        Assert.Equal(new[] { "x", "y", "z" }, ad.NamedGroups.Keys);
        Assert.Equal((1, "a"), (ad.NamedGroups["x"].Number, ad.NamedGroups["x"].Value));
        Assert.False(ad.NamedGroups["y"].Success);
        Assert.Equal((5, "d"), (ad.NamedGroups["z"].Number, ad.NamedGroups["z"].Value));

        // A name none of whose groups participated maps to its first group, unmatched: the groups object's undefined.
        var none = TestRegex.Compile("(?:(?:(?<x>a)|(?<x>b)|c)\\k<x>){2}").Match("aac");
        Assert.True(none.Success);
        Assert.Equal((1, false), (none.NamedGroups["x"].Number, none.NamedGroups["x"].Success));
        Assert.All(none.Groups.Skip(1), static g => Assert.False(g.Success));
    }

    [Fact]
    public void The_participating_group_comes_from_the_offset_entry_s_slots()
    {
        // The adversarial corpus's O07 and O08.
        var o07 = TestRegex.Compile("(?<a>x)|(?<a>y)");
        var slots = new int[o07.SlotCount];
        Assert.Equal(RegexStatus.Match, o07.Match("y", 0, false, slots, null, RegexMatchCeilings.Engines, out _));
        Assert.Equal([0, 1, -1, -1, 0, 1], slots);
        Assert.Equal(2, o07.GroupNames.ParticipatingGroup("a", slots));
        Assert.Equal(RegexStatus.Match, o07.Match("x", 0, false, slots, null, RegexMatchCeilings.Engines, out _));
        Assert.Equal(1, o07.GroupNames.ParticipatingGroup("a", slots));

        var o08 = TestRegex.Compile("(?:(?<a>x)|(?<a>y))\\k<a>");
        slots = new int[o08.SlotCount];
        Assert.Equal(RegexStatus.Match, o08.Match("yy", 0, false, slots, null, RegexMatchCeilings.Engines, out _));
        Assert.Equal([0, 2, -1, -1, 0, 1], slots);
        Assert.Equal(2, o08.GroupNames.ParticipatingGroup("a", slots));
    }

    [Fact]
    public void A_reference_to_a_duplicated_name_is_one_instruction_reading_every_group_of_the_name_at_one_unit()
    {
        // Decision D8's unit for a back-reference is unchanged: one for the instruction dispatched and its capture's
        // length when it runs, whatever the number of groups its name has, as a capture reset of any width costs one.
        var program = TestProgram.Compile("(?:(?<a>x)|(?<b>y)|(?<a>z))\\k<a>(?i:\\k<b>)");
        Assert.Equal(["Backreference 0,2", "BackreferenceIgnoringCase 2,1"],
            ProgramCompilerTests.Listing(program).Where(static l => l.StartsWith("Backreference", StringComparison.Ordinal)));
        Assert.Equal([1, 3], program.ReferencedGroups(0, 2).ToArray());
        Assert.Equal([2], program.ReferencedGroups(2, 1).ToArray());
        Assert.All(program.Code.ToArray().Where(static i => i.Op is RegexOp.Backreference or RegexOp.BackreferenceIgnoringCase), static i => Assert.Equal(1, i.Cost));
    }

    [Fact]
    public void Every_group_of_a_name_in_one_thousand_alternatives_is_read_by_its_reference()
    {
        // The last alternative participates: the reference reads past 999 undefined captures to it.
        var pattern = "(?:" + string.Join("|", Enumerable.Range(0, 1000).Select(static k => "(?<n>" + (char)('\u4E00' + k) + ")")) + ")\\k<n>";
        var regex = TestRegex.Compile(pattern);
        Assert.Equal(Enumerable.Range(1, 1000), regex.GroupNames.GroupsNamed("n").ToArray());
        var last = new string((char)('\u4E00' + 999), 2);
        var match = regex.Match(last);
        Assert.True(match.Success);
        Assert.Equal((1000, last[..1]), (match.NamedGroups["n"].Number, match.NamedGroups["n"].Value));
    }
}

/// <summary>
/// Check A-8 for a duplicated name (requirement R8 beside R6): a match through the offset entry of a pattern whose name
/// many groups share, with a reference to it, allocates nothing once the pool is warm, nor does reading the
/// participating group.
/// </summary>
[Collection(AllocationReading.Collection)]
public class DuplicateNamedGroupAllocationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(1000)]
    public void A_match_through_a_duplicated_name_and_its_reading_allocate_nothing(int alternatives)
    {
        var pattern = "(?:" + string.Join("|", Enumerable.Range(0, alternatives).Select(static k => "(?<n>" + (char)('\u4E00' + k) + ")")) + ")\\k<n>";
        var regex = TestRegex.Compile(pattern);
        var subject = new string((char)('\u4E00' + alternatives - 1), 2);
        var slots = new int[regex.SlotCount];
        var status = RegexStatus.NoMatch;
        var participating = 0;
        var bytes = AllocationReading.BytesPerCall(() =>
        {
            status = regex.Match(subject.AsSpan(), 0, false, slots, null, RegexMatchCeilings.Engines, out _);
            participating = regex.GroupNames.ParticipatingGroup("n", slots);
        });
        Assert.Equal((RegexStatus.Match, alternatives), (status, participating));
        Assert.All(bytes, static b => Assert.Equal(0, b));
    }
}
