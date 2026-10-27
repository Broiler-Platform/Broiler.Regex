using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Broiler.Regex.Compiling;
using Broiler.Unicode.Algorithms;
using Broiler.Unicode.Tables;
using Xunit;

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

    /// <summary>The ranges of single code points and the sequences of a property of strings, <c>RGI_Emoji</c> the sum of the others'.</summary>
    private static (int Ranges, int Sequences) Strings(string name)
    {
        Assert.True(StringProperties.TryResolve(name, out var property), name);
        if (!property.IsUnion)
            return (property.CodePointRangeCount, property.SequenceCount);
        var (ranges, sequences) = (0, 0);
        for (var id = 0; id < StringProperties.PropertyCount; id++)
        {
            var member = StringProperties.Property(id);
            if (!member.IsUnion)
                (ranges, sequences) = (ranges + member.CodePointRangeCount, sequences + member.SequenceCount);
        }

        return (ranges, sequences);
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
    public void The_meter_is_charged_whole_blocks_before_the_work_and_never_a_block_more(string pattern, string flags)
    {
        // Check A-2's prepaid half, read at every charge: the observer is called after a charge is admitted and paid for,
        // before its work, with the units spent through it; the recording meter's total is then at least that, and less
        // than a block more. The meter receives whole blocks only, and the public member charges what the internal one does.
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
    /// library: one per range and per string the expansion touches (the place's unit, <see cref="CompileSite"/>).
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
                { "\\p{RGI_Emoji}", "v", nameof(CompileSite.Property), emoji.Ranges + emoji.Sequences },
                { "\\p{RGI_Emoji_Flag_Sequence}", "v", nameof(CompileSite.Property), Strings("RGI_Emoji_Flag_Sequence").Sequences },

                // The program compiler: a unit per range a class copies, and per string a table holds.
                { "\\p{L}", "u", nameof(CompileSite.Program), Ranges("L") },
                { "\\p{RGI_Emoji}", "v", nameof(CompileSite.Program), emoji.Sequences },
                { "[\\q{abc|ab}x]", "v", nameof(CompileSite.Program), 2 },
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

    [Fact]
    public void A_class_of_strings_lowered_with_the_fast_paths_off_charges_its_strings_too()
    {
        // The general lowering of task 2.4a, an alternation of a class's strings: a unit per code unit of each string.
        var (bySite, _) = Charged("[\\q{abc|de}x]", "v", fastPaths: false);
        Assert.True(bySite.GetValueOrDefault(CompileSite.Program) >= 5);
    }
}
