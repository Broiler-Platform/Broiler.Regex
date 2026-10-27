using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Broiler.Regex.Ast;
using Broiler.Regex.Compiling;
using Broiler.Regex.Engine;
using Broiler.Regex.Parsing;
using Broiler.Regex.Unicode;
using Broiler.Unicode.Algorithms;
using Broiler.Unicode.Tables;
using Xunit;
using CodePointSet = Broiler.Regex.Ast.CodePointSet;

namespace Broiler.Regex.Tests;

/// <summary>
/// Compilation charged to the caller's meter and bounded by the caller's compile budget (requirement R2; decisions D8 and
/// D9 of Broiler.VM's JSD-0081; bundle <c>sjl-11-001</c>'s check A-2, "compile charges the same meter in proportion to
/// the work, at least one unit per range and per string an expansion touches, for a declared set of v-mode and
/// case-insensitive expansions"): <see cref="BroilerRegex.Compile"/>'s budget and meter, the units each place of the
/// parser, the set algebra and the program compiler charges, and the blocks the meter is charged in.
/// </summary>
public class CompileChargingTests
{
    private static readonly UnicodeProperties Properties = new(UnicodeTables.Instance);
    private static readonly UnicodeStringProperties StringProperties = new(UnicodeTables.Instance);

    /// <summary>A meter that records every charge, and throws <see cref="ChargingTests.MeterSpent"/> on its <paramref name="failAt"/>-th call.</summary>
    private sealed class CountingMeter(int failAt = int.MaxValue)
    {
        internal List<ulong> Charges { get; } = [];

        internal ulong Charged { get; private set; }

        internal void Charge(ulong units)
        {
            if (Charges.Count + 1 == failAt)
                throw new ChargingTests.MeterSpent();
            Charges.Add(units);
            Charged += units;
        }
    }

    /// <summary>The units of each place a compilation of <paramref name="pattern"/> charged, and the units in all, with no meter and no budget.</summary>
    private static (Dictionary<CompileSite, ulong> BySite, ulong Total) Charged(string pattern, string flags, bool fastPaths = true)
    {
        var bySite = new Dictionary<CompileSite, ulong>();
        var charge = new CompileCharge(null, null) { Observer = (site, units, _) => bySite[site] = bySite.GetValueOrDefault(site) + units };
        Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out _, fastPaths, charge));
        return (bySite, charge.Spent);
    }

    private static int Ranges(string name)
    {
        Assert.True(Properties.TryResolve(name, out var set), name);
        return set.RangeCount;
    }

    private static int Ranges(string name, string value)
    {
        Assert.True(Properties.TryResolve(name, value, out var set), name + "=" + value);
        return set.RangeCount;
    }

    private static long CodePoints(string name)
    {
        Assert.True(Properties.TryResolve(name, out var set), name);
        var count = 0L;
        for (var range = 0; range < set.RangeCount; range++)
            count += set.Last(range) - set.First(range) + 1;
        return count;
    }

    /// <summary>
    /// The ranges of single code points, the sequences and the sequences' UTF-16 code units of a property of strings,
    /// <c>RGI_Emoji</c> the sum of the others'.
    /// </summary>
    private static (int Ranges, int Sequences, long CodeUnits) Strings(string name)
    {
        Assert.True(StringProperties.TryResolve(name, out var property), name);
        if (!property.IsUnion)
            return (property.CodePointRangeCount, property.SequenceCount, CodeUnits(property));
        var (ranges, sequences, codeUnits) = (0, 0, 0L);
        for (var id = 0; id < StringProperties.PropertyCount; id++)
        {
            var member = StringProperties.Property(id);
            if (!member.IsUnion)
                (ranges, sequences, codeUnits) = (ranges + member.CodePointRangeCount, sequences + member.SequenceCount, codeUnits + CodeUnits(member));
        }

        return (ranges, sequences, codeUnits);
    }

    private static long CodeUnits(StringProperty property)
    {
        var sequences = property.GetSequences();
        Span<int> sequence = stackalloc int[StringPropertySequences.MaxSequenceLength];
        var units = 0L;
        while (sequences.TryRead(sequence, out var length))
        {
            for (var i = 0; i < length; i++)
                units += sequence[i] > 0xFFFF ? 2 : 1;
        }

        return units;
    }

    /// <summary>The code points two properties share, from the provider's sets.</summary>
    private static long CodePointsInBoth(string first, string second)
    {
        CodePointSet Set(string name)
        {
            Assert.True(Properties.TryResolve(name, out var provided), name);
            var ranges = new List<(int Lo, int Hi)>();
            for (var range = 0; range < provided.RangeCount; range++)
                ranges.Add((provided.First(range), provided.Last(range)));
            return CodePointSet.FromRanges(ranges);
        }

        return Set(first).Intersect(Set(second)).CodePointCount();
    }

    // ----- Compile's budget and meter --------------------------------------------------------------------------------

    public static TheoryData<string, string> Patterns => new()
    {
        { "abc", "" },
        { "(a|b)*c\\d+", "u" },
        { "\\p{L}", "u" },
        { "[\\p{L}\\p{N}_]+", "u" },
        { "[\\p{L}--\\p{Lu}]", "v" },
        { "[\\p{L}&&[^\\p{Lu}]]", "vi" },
        { "\\p{RGI_Emoji}", "v" },
        { "[\\p{RGI_Emoji}\\q{abc|de}]x\\p{RGI_Emoji}", "vi" },
        { "\\w\\b\\s\\S\\W", "iu" },
        { "[" + string.Concat(Enumerable.Range(0, 300).Reverse().Select(static k => (char)(0x4E00 + 3 * k))) + "]", "" },
    };

    [Theory]
    [MemberData(nameof(Patterns))]
    public void A_compile_budget_admits_the_work_it_covers_and_refuses_one_unit_less(string pattern, string flags)
    {
        // The units a compilation charges in all, T: a budget of T compiles the pattern, one of T - 1 answers LimitExceeded
        // naming the compile budget, with no instance, never an exception and never a partial instance.
        var (_, total) = Charged(pattern, flags);
        Assert.True(total >= (ulong)pattern.Length);
        var parsed = RegexFlagsParser.Parse(flags);

        Assert.Equal(RegexStatus.Success, BroilerRegex.Compile(pattern, parsed, UnicodeTables.Instance, 512, total, null, out var regex, out var limit));
        Assert.NotNull(regex);
        Assert.Equal(RegexLimit.None, limit);

        Assert.Equal(RegexStatus.LimitExceeded, BroilerRegex.Compile(pattern, parsed, UnicodeTables.Instance, 512, total - 1, null, out regex, out limit));
        Assert.Null(regex);
        Assert.Equal(RegexLimit.Compile, limit);
    }

    [Theory]
    [MemberData(nameof(Patterns))]
    public void The_meter_holds_whole_blocks_covering_every_unit_charged_and_never_a_block_more(string pattern, string flags)
    {
        // Check A-2's prepaid half, read at every charge: the observer is called after a charge is admitted and paid for,
        // with the units spent through it; the recording meter's total is then at least that, and less than a block more.
        // The meter receives whole blocks only, and the public member charges what the internal one does. This test reads
        // the account, not the work: that a charge comes before the work it pays for is read where the work can be seen,
        // the provider's ranges (Each_range_copied_from_the_provider_is_charged_before_it_is_read) and the comparisons of a
        // class's strings (A_union_an_intersection_and_a_difference_of_n_strings_make_comparisons_in_proportion_to_n).
        var meter = new CountingMeter();
        var faults = new List<string>();
        var charge = new CompileCharge(meter.Charge, null)
        {
            Observer = (site, units, spent) =>
            {
                if (faults.Count < 5 && (meter.Charged < spent || meter.Charged - spent >= CompileCharge.Block))
                    faults.Add(site + " charged " + units + ": " + meter.Charged + " charged through unit " + spent);
            },
        };
        Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance, 512, out _, charge: charge));

        Assert.Empty(faults);
        Assert.All(meter.Charges, static c => Assert.True(c > 0 && c % CompileCharge.Block == 0, "a charge of " + c));
        Assert.True(meter.Charged >= charge.Spent && meter.Charged - charge.Spent < CompileCharge.Block, meter.Charged + " charged for " + charge.Spent);

        var publicMeter = new CountingMeter();
        Assert.Equal(RegexStatus.Success, BroilerRegex.Compile(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance, 512, null, publicMeter.Charge, out _, out _));
        Assert.Equal(meter.Charges, publicMeter.Charges);
    }

    [Fact]
    public void A_meter_that_throws_ends_the_compilation_with_its_own_exception_and_changes_nothing()
    {
        // Decision D9: the meter's exception reaches the caller unchanged, whichever of its calls throws, and the library
        // keeps no state of the compilation it ended: the same pattern compiles afterwards, with or without a meter, to an
        // instance that answers as one compiled before any meter threw, and charges the same blocks.
        const string Pattern = "[\\p{L}--\\p{Lu}]([\\p{RGI_Emoji}\\q{ab|cd}])+\\b[^\\p{N}]";
        const RegexFlags Flags = RegexFlags.UnicodeSets | RegexFlags.IgnoreCase;
        Assert.Equal(RegexStatus.Success, BroilerRegex.Compile(Pattern, Flags, UnicodeTables.Instance, 512, null, null, out var before, out _));
        var calls = new CountingMeter();
        Assert.Equal(RegexStatus.Success, BroilerRegex.Compile(Pattern, Flags, UnicodeTables.Instance, 512, null, calls.Charge, out _, out _));
        Assert.True(calls.Charges.Count >= 3, calls.Charges.Count + " charges");

        string[] subjects = ["xAB\U0001F600!", "bcd x", "b\U0001F44D\U0001F3FDab?"];
        for (var failAt = 1; failAt <= calls.Charges.Count; failAt++)
        {
            var meter = new CountingMeter(failAt);
            Assert.Throws<ChargingTests.MeterSpent>(() => BroilerRegex.Compile(Pattern, Flags, UnicodeTables.Instance, 512, null, meter.Charge, out _, out _));
            Assert.Equal(failAt - 1, meter.Charges.Count);

            var again = new CountingMeter();
            Assert.Equal(RegexStatus.Success, BroilerRegex.Compile(Pattern, Flags, UnicodeTables.Instance, 512, null, again.Charge, out var after, out _));
            Assert.Equal(calls.Charges, again.Charges);
            foreach (var subject in subjects)
                Assert.Equal(TestRegex.Answer(before!, subject, 0), TestRegex.Answer(after!, subject, 0));
        }
    }

    [Fact]
    public void The_budget_and_the_nesting_limit_each_answer_their_own_dimension_in_the_order_the_work_meets_them()
    {
        var nested = string.Concat(Enumerable.Repeat("(", 600)) + "a" + string.Concat(Enumerable.Repeat(")", 600));

        // The pattern's units are charged before the parse reads it: a budget below its length refuses it first.
        Assert.Equal(RegexStatus.LimitExceeded, BroilerRegex.Compile(nested, RegexFlags.None, UnicodeTables.Instance, 512, 1_000, null, out _, out var limit));
        Assert.Equal(RegexLimit.Compile, limit);

        // With room for its units, the parse meets the nesting limit at the 512th group.
        Assert.Equal(RegexStatus.LimitExceeded, BroilerRegex.Compile(nested, RegexFlags.None, UnicodeTables.Instance, 512, 1_201, null, out _, out limit));
        Assert.Equal(RegexLimit.Nesting, limit);

        // A syntax error the parse meets before the budget is spent is the parser's, as before.
        Assert.Throws<RegexSyntaxException>(() => BroilerRegex.Compile("[\\p{L}--", RegexFlags.UnicodeSets, UnicodeTables.Instance, 512, 1_000_000, null, out _, out _));
    }

    [Fact]
    public void A_compile_budget_of_zero_is_a_broken_precondition_and_the_constructors_take_neither_budget_nor_meter()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BroilerRegex.Compile("a", RegexFlags.None, UnicodeTables.Instance, 512, 0UL, null, out _, out _));
        Assert.Equal(RegexStatus.Success, BroilerRegex.Compile("a", RegexFlags.None, UnicodeTables.Instance, 512, 1UL, null, out _, out _));
        Assert.Equal(5, (int)RegexLimit.Compile);

        // The members that take no budget compile what Compile refuses at the corpus's budgets (decision D10's policy for
        // an engine that passes no meter).
        var v01 = "[" + string.Concat(Enumerable.Repeat("\\p{L}", 50)) + "]";
        Assert.Equal(RegexStatus.LimitExceeded, BroilerRegex.Compile(v01, RegexFlags.UnicodeSets, UnicodeTables.Instance, 512, 300UL, null, out _, out var limit));
        Assert.Equal(RegexLimit.Compile, limit);
        Assert.True(new BroilerRegex(v01, "v", UnicodeTables.Instance).IsMatch("é"));
    }

    [Fact]
    public void Concurrent_compilations_with_meters_and_budgets_answer_as_one_alone_does()
    {
        // Each compilation's account is its own (decision D9, check A-11): compilations of the same patterns on sixteen
        // threads at once charge their meters the same blocks and answer the same statuses as one thread alone.
        (string Pattern, RegexFlags Flags, ulong? Budget)[] cases =
        [
            ("[\\p{L}\\p{N}]+", RegexFlags.UnicodeSets | RegexFlags.IgnoreCase, null),
            ("[\\p{RGI_Emoji}--\\q{\U0001F600}]", RegexFlags.UnicodeSets, null),
            ("[" + string.Concat(Enumerable.Repeat("\\p{L}", 50)) + "]", RegexFlags.UnicodeSets, 300UL),
            ("\\w+\\b", RegexFlags.Unicode | RegexFlags.IgnoreCase, 5_000UL),
        ];

        string Run((string Pattern, RegexFlags Flags, ulong? Budget) item)
        {
            var meter = new CountingMeter();
            var status = BroilerRegex.Compile(item.Pattern, item.Flags, UnicodeTables.Instance, 512, item.Budget, meter.Charge, out _, out var limit);
            return status + " " + limit + " " + string.Join(',', meter.Charges);
        }

        var alone = cases.Select(Run).ToArray();
        var answers = new string[16][];
        var threads = Enumerable.Range(0, 16).Select(t => new Thread(() => answers[t] = cases.Select(Run).ToArray())).ToArray();
        foreach (var thread in threads)
            thread.Start();
        foreach (var thread in threads)
            thread.Join();
        Assert.All(answers, a => Assert.Equal(alone, a));
    }

    // ----- Check A-2's per-unit test: each place charges at least its declared units -----------------------------------

    /// <summary>
    /// The declared set of check A-2: <c>v</c>-mode and case-insensitive expansions, and the places that charge them, each
    /// with the units it must charge at least, read from the provider's own data by these tests rather than from the
    /// library: one per range and per string the expansion touches, and where a place's unit says more (a sort's factor,
    /// a string's code units, each time a string is touched), that unit (<see cref="CompileSite"/>). Each charge the
    /// library makes is needed by at least one row: removing any one of them fails a row (the roadmap's record of task 2.5
    /// lists the control, a run with each charge removed in turn).
    /// </summary>
    public static TheoryData<string, string, string, long> DeclaredExpansions
    {
        get
        {
            var emoji = Strings("RGI_Emoji");
            return new TheoryData<string, string, string, long>
            {
                // The pattern itself, a unit per code unit.
                { "abc", "", nameof(CompileSite.Pattern), 3 },
                { "[\\p{L}]", "v", nameof(CompileSite.Pattern), 7 },

                // A property escape through the provider: a unit per range, and per sequence of a property of strings.
                { "\\p{L}", "u", nameof(CompileSite.Property), Ranges("L") },
                { "\\p{Script=Greek}", "u", nameof(CompileSite.Property), Ranges("Script", "Greek") },
                { "[\\P{Lu}]", "v", nameof(CompileSite.Property), Ranges("Lu") },
                { "\\p{RGI_Emoji}", "v", nameof(CompileSite.Property), emoji.Ranges + emoji.Sequences + emoji.CodeUnits },
                { "\\p{RGI_Emoji_Flag_Sequence}", "v", nameof(CompileSite.Property), Strings("RGI_Emoji_Flag_Sequence").Sequences },

                // The set algebra: a unit per range of each operand.
                { "[\\p{L}\\p{N}]", "u", nameof(CompileSite.Union), Ranges("L") + Ranges("N") },
                { "[\\p{L}\\p{N}]", "v", nameof(CompileSite.Union), Ranges("L") + Ranges("N") },
                { "[\\p{L}\\p{N}\\p{M}\\p{P}]", "iv", nameof(CompileSite.Union), Ranges("L") + Ranges("N") + Ranges("M") + Ranges("P") },
                { "[\\p{L}&&\\p{Lu}]", "v", nameof(CompileSite.Intersection), Ranges("L") + Ranges("Lu") },
                { "[\\p{L}--\\p{Lu}]", "v", nameof(CompileSite.Difference), Ranges("L") + Ranges("Lu") },
                { "[^\\p{L}]", "v", nameof(CompileSite.Difference), Ranges("L") + 1 },
                { "\\P{L}", "u", nameof(CompileSite.Complement), Ranges("L") + 1 },
                { "[\\P{L}a]", "u", nameof(CompileSite.Complement), Ranges("L") + 1 },
                { "\\D\\W\\S", "", nameof(CompileSite.Complement), 3 },

                // Ranges gathered out of order: a unit per range for the merge, and the sort's ⌊log₂ 26⌋ = 4 more per range.
                { "[zyxwvutsrqponmlkjihgfedcba]", "", nameof(CompileSite.Normalize), 26 * 5 },
                { "[\\p{L}]", "u", nameof(CompileSite.Normalize), Ranges("L") },

                // MaybeSimpleCaseFolding under vi: a unit per range of each operand folded, per code point mapped one at a time
                // (of \p{L}, those it shares with Cased), and a string's code units.
                { "\\p{L}", "vi", nameof(CompileSite.CaseFold), Ranges("L") + CodePointsInBoth("L", "Cased") },
                { "[\\p{Lu}\\p{Ll}]", "vi", nameof(CompileSite.CaseFold), Ranges("Lu") + Ranges("Ll") },
                { "\\p{RGI_Emoji}", "vi", nameof(CompileSite.CaseFold), emoji.Sequences },
                { "[\\q{AB|cD|Ef}]", "vi", nameof(CompileSite.CaseFold), 6 },

                // A class's strings: a string's code units each time a disjunction, a union, an intersection or a difference
                // adds it or looks it up, and when the class takes it: four strings of two, added and taken; and ab, cd, then
                // cd, ef added, ab and cd looked up, cd kept and taken.
                { "[\\q{ab|cd|ef|gh}]", "v", nameof(CompileSite.Strings), 16 },
                { "[\\q{ab|cd}&&\\q{cd|ef}]", "v", nameof(CompileSite.Strings), 16 },
                { "[\\p{RGI_Emoji}\\q{ab}]", "v", nameof(CompileSite.Strings), emoji.Sequences + 1 },
                { "[\\p{RGI_Emoji}&&\\p{RGI_Emoji}]", "v", nameof(CompileSite.Strings), 2L * emoji.Sequences },
                { "[\\p{RGI_Emoji}--\\q{ab}]", "v", nameof(CompileSite.Strings), emoji.Sequences + 1 },
                { "\\p{RGI_Emoji}", "v", nameof(CompileSite.StringOrder), emoji.Sequences },

                // The sets built from the provider for the pattern: AllCharacters under vi, \s, the folded \w.
                { "[^a]", "vi", nameof(CompileSite.Universe), CodePoints("Cased") },
                { "\\s", "", nameof(CompileSite.Universe), Properties.SpaceSeparator.RangeCount + 3 },
                { "\\w", "iu", nameof(CompileSite.Universe), 63 },

                // The program compiler: a unit per range a class copies, and for a class of strings its code units, and one,
                // to read them, its code units and 1 + ⌊log₂ n⌋ per string for a new table, and its code units, and one, for
                // each earlier table of the same hash compared: abc and de read (6), a new table (5 + 2 × 2) and x's range
                // (1); then the same class again, read (6), compared with the first table (6), and x's range (1).
                { "\\p{L}", "u", nameof(CompileSite.Program), Ranges("L") },
                { "\\p{RGI_Emoji}", "v", nameof(CompileSite.Program), emoji.Sequences },
                { "[\\q{abc|de}x]", "v", nameof(CompileSite.Program), 16 },
                { "[\\q{abc|de}x][\\q{abc|de}x]", "v", nameof(CompileSite.Program), 29 },
            };
        }
    }

    [Theory]
    [MemberData(nameof(DeclaredExpansions))]
    public void Each_place_charges_at_least_one_unit_per_range_and_per_string_its_expansion_touches(string pattern, string flags, string place, long declared)
    {
        var site = Enum.Parse<CompileSite>(place);
        var (bySite, _) = Charged(pattern, flags);
        var charged = bySite.GetValueOrDefault(site);
        Assert.True(charged >= (ulong)declared, "/" + pattern + "/" + flags + ": " + site + " charged " + charged + ", fewer than the " + declared + " declared");
    }

    /// <summary>The caller's tables, reporting each read of a table of ranges to <see cref="Read"/> while it is set.</summary>
    private sealed class WatchedRanges(IUnicodeTables inner) : IUnicodeTables
    {
        internal Action? Read { get; set; }

        public string UnicodeVersion => inner.UnicodeVersion;

        public ReadOnlySpan<byte> Table(UnicodeTable table)
        {
            if (table is UnicodeTable.PropertyRanges or UnicodeTable.StringPropertyRanges)
                Read?.Invoke();
            return inner.Table(table);
        }
    }

    [Theory]
    [InlineData("\\p{L}", "u")]
    [InlineData("[\\p{L}\\p{N}\\p{M}]", "v")]
    [InlineData("\\p{Script=Greek}\\P{Lu}", "u")]
    [InlineData("[\\p{L}--\\p{Lu}]", "vi")]
    [InlineData("[^a]", "vi")]
    [InlineData("\\s\\S", "")]
    [InlineData("\\p{Basic_Emoji}", "v")]
    [InlineData("[\\p{Basic_Emoji}--\\p{Emoji_Keycap_Sequence}]\\p{RGI_Emoji_Flag_Sequence}", "v")]
    public void Each_range_copied_from_the_provider_is_charged_before_it_is_read(string pattern, string flags)
    {
        // Decision D8's order, read on the work itself: the provider's sets are read through the caller's tables, and a
        // copy reads each range's two bounds from the table of ranges, one read each. A property escape and the sets a
        // compilation builds from the provider charge a unit per range before the copy reads it (CompileSite.Property and
        // Universe), so at each read of a table of ranges the units charged at those places are at least half the reads
        // made so far. A charge made after its copy, a charge check A-2's per-unit test reads the same either way, fails
        // here at the copy's first read. The pattern charges its own units first, so the comparison counts only the two
        // places' units, and each pattern reads a table of ranges at least once. The provider's constructors, which read
        // each table of ranges once to check its size before the compilation starts, are made before the watch begins.
        var tables = new WatchedRanges(UnicodeTables.Instance);
        var (reads, charged) = (0L, 0UL);
        var faults = new List<string>();
        var charge = new CompileCharge(null, null)
        {
            Observer = (site, units, _) =>
            {
                if (site is CompileSite.Property or CompileSite.Universe)
                    charged += units;
            },
        };
        var parsed = RegexFlagsParser.Parse(flags);
        var nesting = new RegexNesting(BroilerRegex.MaximumNestingLimit);
        var data = new RegexUnicode(tables, charge);
        tables.Read = () =>
        {
            reads++;
            if (faults.Count < 5 && (ulong)reads > 2 * charged)
                faults.Add("read " + reads + " of a table of ranges with " + charged + " units charged for ranges");
        };

        try
        {
            var tree = RegexParser.Parse(pattern, parsed, data, nesting, out var captureCount, out var groupNames);
            Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(tree, captureCount, groupNames, parsed, data, nesting, out _));
        }
        finally
        {
            tables.Read = null;
        }

        Assert.True(reads > 0, "/" + pattern + "/" + flags + " read no table of ranges");
        Assert.Empty(faults);
    }

    [Fact]
    public void A_class_of_strings_lowered_with_the_fast_paths_off_charges_its_strings_too()
    {
        // The general lowering of task 2.4a, an alternation of a class's strings: a unit per code unit of each string.
        var (bySite, _) = Charged("[\\q{abc|de}x]", "v", fastPaths: false);
        Assert.True(bySite.GetValueOrDefault(CompileSite.Program) >= 5);
    }

    [Fact]
    public void Folding_a_large_set_under_vi_is_charged_in_proportion_to_its_ranges_not_its_code_points()
    {
        // \p{L} holds 145,672 code points in 684 ranges at UCD 17.0.0. Task 2.5 folds a set of more than 4,096 code points
        // through Cased, so its fold costs a few thousand units, where a unit per code point mapped would be 145,672.
        Assert.True(CodePoints("L") > 100_000);
        var (bySite, total) = Charged("\\p{L}", "vi");
        Assert.True(bySite[CompileSite.CaseFold] < 10_000, bySite[CompileSite.CaseFold] + " units to fold \\p{L}");
        Assert.True(total < 30_000, total + " units to compile /\\p{L}/vi");
    }

    // ----- MaybeSimpleCaseFolding in proportion to ranges answers as a fold of every code point -----------------------

    public static IEnumerable<object[]> FoldedSets()
    {
        string[] lone = ["L", "Lu", "Ll", "Lt", "Lm", "Lo", "M", "N", "P", "S", "Z", "C", "Cased", "Any", "ASCII", "Assigned", "Alphabetic", "Changes_When_Casefolded"];
        foreach (var name in lone)
            yield return [name, ""];
        foreach (var script in (string[])["Latin", "Greek", "Cyrillic", "Armenian", "Georgian", "Cherokee", "Deseret", "Adlam", "Glagolitic", "Han"])
            yield return ["Script", script];
    }

    [Theory]
    [MemberData(nameof(FoldedSets))]
    public void Folding_through_Cased_answers_the_fold_of_every_code_point(string name, string value)
    {
        Assert.True(value.Length == 0 ? Properties.TryResolve(name, out var provided) : Properties.TryResolve(name, value, out provided));
        var ranges = new List<(int Lo, int Hi)>();
        for (var range = 0; range < provided.RangeCount; range++)
            ranges.Add((provided.First(range), provided.Last(range)));
        var set = CodePointSet.FromRanges(ranges);

        var data = new RegexUnicode(UnicodeTables.Instance);
        var expected = set.Map(cp => data.Canonicalize(cp, unicode: true));
        Assert.Equal(expected.Ranges, data.SimpleCaseFold(set).Ranges);
        var complement = set.Complement();
        Assert.Equal(complement.Map(cp => data.Canonicalize(cp, unicode: true)).Ranges, data.SimpleCaseFold(complement).Ranges);
    }

    [Fact]
    public void Folding_answers_the_fold_of_every_code_point_on_either_side_of_the_one_by_one_limit()
    {
        // Sets drawn with a fixed seed around RegexUnicode.FoldedOneByOne, so that both ways of folding meet each shape:
        // runs across case pairs, single code points, the astral cased scripts, and the whole code space.
        var data = new RegexUnicode(UnicodeTables.Instance);
        var random = new Random(0x2_5);
        for (var round = 0; round < 200; round++)
        {
            var ranges = new List<(int Lo, int Hi)>();
            var target = round % 2 == 0 ? RegexUnicode.FoldedOneByOne - 64 + random.Next(128) : random.Next(1, 40_000);
            var count = 0L;
            while (count < target)
            {
                var lo = random.Next(4) switch { 0 => random.Next(0x80, 0x600), 1 => random.Next(0x1E00, 0x2200), 2 => random.Next(0x10400, 0x1E950), _ => random.Next(0, 0x110000) };
                var hi = Math.Min(0x10FFFF, lo + random.Next(0, 300));
                ranges.Add((lo, hi));
                count += hi - lo + 1;
            }

            var set = CodePointSet.FromRanges(ranges);
            Assert.Equal(set.Map(cp => data.Canonicalize(cp, unicode: true)).Ranges, data.SimpleCaseFold(set).Ranges);
        }

        var all = CodePointSet.Of(0, CodePointSet.MaxCodePoint);
        Assert.Equal(all.Map(cp => data.Canonicalize(cp, unicode: true)).Ranges, data.SimpleCaseFold(all).Ranges);
    }

    // ----- The union structure: each string touched once ---------------------------------------------------------------

    /// <summary>n distinct strings of three letters, the k-th as written in a pattern.</summary>
    private static string[] ThreeLetterStrings(int n) =>
        [.. Enumerable.Range(0, n).Select(static k => new string([(char)('a' + (k / 26 % 26)), (char)('a' + (k % 26)), (char)('A' + (k / 676 % 26))]))];

    [Theory]
    [InlineData(500)]
    [InlineData(4_000)]
    public void A_union_an_intersection_and_a_difference_of_strings_touch_each_string_a_bounded_number_of_times(int n)
    {
        // Plan row 2.5's O(n²) List.Contains, read on the account: a union of operands holding n strings in all, or an
        // intersection or a difference of two such operands, charges each string of its operands a bounded number of times,
        // so its units are in proportion to the strings' code units. A list charged per comparison would charge them n²/2
        // times; one that charges per lookup charges the same either way, so the work itself is counted by
        // A_union_an_intersection_and_a_difference_of_n_strings_make_comparisons_in_proportion_to_n.
        var strings = ThreeLetterStrings(n);
        var units = 3L * n;
        var oneDisjunction = "[\\q{" + string.Join('|', strings) + "}]";
        var manyOperands = "[" + string.Concat(strings.Select(static s => "\\q{" + s + "}")) + "]";
        var repeated = "[" + string.Concat(Enumerable.Repeat("\\q{" + string.Join('|', strings) + "}", 4)) + "]";
        var intersection = "[\\q{" + string.Join('|', strings) + "}&&\\q{" + string.Join('|', strings.Reverse()) + "}]";
        var difference = "[\\q{" + string.Join('|', strings) + "}--\\q{" + string.Join('|', strings.Reverse()) + "}]";
        foreach (var (pattern, operands) in (ValueTuple<string, int>[])[(oneDisjunction, 1), (manyOperands, 1), (repeated, 4), (intersection, 2), (difference, 2)])
        {
            var (bySite, _) = Charged(pattern, "v");
            var touched = bySite.GetValueOrDefault(CompileSite.Strings);
            Assert.True(touched <= (ulong)(8 * operands * units), pattern[..20] + "... touched " + touched + " units of strings for " + operands * units);
        }
    }

    /// <summary>A comparer of strings that counts every call made of it, equality and hashing alike.</summary>
    private sealed class CountingComparer(IEqualityComparer<string> inner) : IEqualityComparer<string>
    {
        internal long Calls { get; set; }

        public bool Equals(string? x, string? y)
        {
            Calls++;
            return inner.Equals(x, y);
        }

        public int GetHashCode(string obj)
        {
            Calls++;
            return inner.GetHashCode(obj);
        }
    }

    private static ClassStrings StringsOf(IEqualityComparer<string> comparer, IEnumerable<string> strings)
    {
        var set = new ClassStrings(comparer);
        var charge = new CompileCharge(null, null);
        foreach (var text in strings)
            set.Add(text, charge);
        return set;
    }

    [Fact]
    public void A_class_s_strings_are_members_by_the_comparer_they_are_made_with_and_by_no_other()
    {
        // The counting test below counts the comparer's calls; it counts the set's work only if the comparer is all the set
        // compares strings with. A comparer that equates strings of different case decides here what the parser's
        // ordinal comparer never would, so a membership test made by any other means (a search of the list in order,
        // say) answers differently. Intersections and differences answer sets of the same comparer.
        var charge = new CompileCharge(null, null);
        var set = StringsOf(StringComparer.OrdinalIgnoreCase, ["ab", "cd"]);
        Assert.False(set.Add("AB", charge));
        Assert.True(set.Contains("Cd", charge));
        Assert.False(set.Contains("ef", charge));
        set.UnionWith(StringsOf(StringComparer.Ordinal, ["CD", "ef"]), charge);
        Assert.Equal(new[] { "ab", "cd", "ef" }, set.InOrder);

        var other = StringsOf(StringComparer.OrdinalIgnoreCase, ["AB", "EF"]);
        var meet = set.Intersect(other, charge);
        Assert.Equal(new[] { "ab", "ef" }, meet.InOrder);
        Assert.True(meet.Contains("EF", charge));
        var rest = set.Except(other, charge);
        Assert.Equal(new[] { "cd" }, rest.InOrder);
        Assert.False(rest.Add("CD", charge));

        // The parser's sets compare by code unit.
        var ordinal = new ClassStrings();
        Assert.True(ordinal.Add("ab", charge));
        Assert.True(ordinal.Add("AB", charge));
        Assert.False(ordinal.Contains("Ab", charge));
    }

    [Theory]
    [InlineData(500)]
    [InlineData(4_000)]
    public void A_union_an_intersection_and_a_difference_of_n_strings_make_comparisons_in_proportion_to_n(int n)
    {
        // Plan row 2.5's O(n²) List.Contains, read on the work: the comparer's calls, hashing and equality alike, while a
        // union adds n strings to n others of which half are new, an intersection keeps the half of n strings the other
        // operand holds and a difference the other half, and a \q{…} disjunction's n alternatives, a quarter repeated, are
        // added in turn. A hashed set makes a few calls per string read; a list searched before each addition makes about
        // n² for the union, some 250,000 at n = 500, where at most 8n, 4,000, are admitted.
        var strings = ThreeLetterStrings(2 * n);
        var first = strings[..n];
        var second = strings[(n / 2)..(n + (n / 2))];
        var charge = new CompileCharge(null, null);
        var comparer = new CountingComparer(StringComparer.Ordinal);

        var union = StringsOf(comparer, first);
        comparer.Calls = 0;
        union.UnionWith(StringsOf(StringComparer.Ordinal, second), charge);
        Assert.Equal(n + (n / 2), union.Count);
        Assert.True(comparer.Calls <= 8L * n, "a union of " + n + " strings into " + n + " made " + comparer.Calls + " comparisons");

        var left = StringsOf(comparer, first);
        var right = StringsOf(comparer, second);
        comparer.Calls = 0;
        var meet = left.Intersect(right, charge);
        Assert.Equal(n - (n / 2), meet.Count);
        Assert.True(comparer.Calls <= 8L * n, "an intersection of " + n + " strings with " + n + " made " + comparer.Calls + " comparisons");

        comparer.Calls = 0;
        var rest = left.Except(right, charge);
        Assert.Equal(n / 2, rest.Count);
        Assert.True(comparer.Calls <= 8L * n, "a difference of " + n + " strings and " + n + " made " + comparer.Calls + " comparisons");

        var alternatives = new ClassStrings(comparer);
        comparer.Calls = 0;
        foreach (var text in first.Concat(first[..(n / 4)]))
            alternatives.Add(text, charge);
        Assert.Equal(n, alternatives.Count);
        Assert.True(comparer.Calls <= 8L * n, "a disjunction of " + (n + (n / 4)) + " alternatives made " + comparer.Calls + " comparisons");
    }

    [Fact]
    public void Each_occurrence_of_a_class_of_strings_finds_its_table_by_a_hash_of_its_strings()
    {
        // Two hundred classes of the same 500 strings but the last, each its own table: an occurrence that compared its
        // strings with every earlier table's, charging each comparison, would read about 200²/2 × 500 strings; found by a
        // hash, each occurrence reads its own strings a bounded number of times. This reads the account; a lookup that
        // compares with every earlier table but charges only its own strings passes it, so the work itself is counted by
        // An_occurrence_of_a_class_of_strings_compares_its_strings_with_the_tables_of_its_hash_alone.
        var strings = ThreeLetterStrings(500);
        var classes = Enumerable.Range(0, 200).Select(k => "[\\q{" + string.Join('|', strings) + "|z" + k.ToString(System.Globalization.CultureInfo.InvariantCulture) + "q}]").ToArray();
        var (bySite, _) = Charged(string.Concat(classes), "v");
        var perClass = (3L * strings.Length) + 6;
        Assert.True(bySite[CompileSite.Program] <= (ulong)(200 * 16 * perClass), bySite[CompileSite.Program] + " units of the program compiler for " + 200 * perClass);

        // The same class twice shares one table, compared string by string once.
        Assert.Equal(1, TestProgram.Compile(classes[0] + classes[0], "v").StringTableCount);
        Assert.Equal(2, TestProgram.Compile(classes[0] + classes[1], "v").StringTableCount);
    }

    /// <summary>The program of <paramref name="pattern"/> under <c>v</c>, whose classes of strings find their earlier tables through <paramref name="comparer"/>.</summary>
    private static RegexProgram CompiledFindingTablesBy(string pattern, IEqualityComparer<string> comparer)
    {
        Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(pattern, RegexFlagsParser.Parse("v"), UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out var program,
            stringComparer: comparer));
        return program!;
    }

    [Fact]
    public void An_occurrence_of_a_class_of_strings_compares_its_strings_with_the_tables_of_its_hash_alone()
    {
        // The table lookup read on the work, not on the account: the comparer the program compiler finds an earlier table
        // by counts its calls, hashing and equality alike. Two hundred classes of the same 500 three-letter strings and a
        // two-letter string of each class's own, which the class's order, longest first, puts last, so that comparing two
        // of the tables string by string reads all 501 strings before they differ. Found by a hash, each occurrence
        // hashes its 501 strings once and compares them with no earlier table, some 100,000 calls in all, where at most
        // four times that are admitted; compared with every earlier table, as task 2.4c's lookup did, its charges
        // unchanged or not, the occurrences make 200 × 199 / 2 × 501 comparisons more, some ten million.
        var strings = ThreeLetterStrings(500);
        var classes = Enumerable.Range(0, 200).Select(k => "[\\q{" + string.Join('|', strings) + "|" + (char)('a' + (k / 26)) + (char)('a' + (k % 26)) + "}]").ToArray();
        var read = 200L * 501;
        var comparer = new CountingComparer(StringComparer.Ordinal);
        Assert.Equal(200, CompiledFindingTablesBy(string.Concat(classes), comparer).StringTableCount);
        Assert.True(comparer.Calls <= 4 * read, comparer.Calls + " comparisons to find the tables of " + read + " strings in 200 classes");

        // An occurrence of a class met before compares its strings with that one table, once.
        comparer.Calls = 0;
        Assert.Equal(1, CompiledFindingTablesBy(classes[0] + classes[0], comparer).StringTableCount);
        Assert.True(comparer.Calls <= 4 * 2 * 501, comparer.Calls + " comparisons to find the tables of one class of 501 strings met twice");

        // The comparer alone decides which table an occurrence shares, so its calls are every comparison the lookup makes:
        // one that equates strings of different case gives two classes of the same letters in different case one table,
        // which the ordinal comparer, the library's, never does.
        Assert.Equal(1, CompiledFindingTablesBy("[\\q{ab|cd}][\\q{AB|CD}]", StringComparer.OrdinalIgnoreCase).StringTableCount);
        Assert.Equal(2, CompiledFindingTablesBy("[\\q{ab|cd}][\\q{AB|CD}]", StringComparer.Ordinal).StringTableCount);
    }
}
