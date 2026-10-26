using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.Regex.Compiling;
using Broiler.Regex.Engine;
using Broiler.Unicode.Algorithms;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The new backend's charging (requirement R1; decisions D8 and D9 of Broiler.VM's JSD-0081; bundle <c>sjl-11-001</c>'s
/// check A-1): one unit per instruction dispatched and a class test's weight on top, charged to
/// the caller's meter in whole blocks of 512 before the work, across every start position of a call with no reset.
/// </summary>
public class ChargingTests
{
    /// <summary>A meter that records every charge, and throws <see cref="MeterSpent"/> once more than its budget is charged.</summary>
    internal sealed class RecordingMeter(ulong budget = ulong.MaxValue)
    {
        internal List<ulong> Charges { get; } = [];

        internal ulong Charged { get; private set; }

        internal void Charge(ulong units)
        {
            Charges.Add(units);
            Charged += units;
            if (Charged > budget)
                throw new MeterSpent();
        }
    }

    /// <summary>The recording meter's own exception: the caller's budget is spent.</summary>
    internal sealed class MeterSpent : Exception;

    private static (RegexStatus Status, RegexLimit Limit, ulong Units, int[] Slots) Run(
        RegexProgram program, string subject, RegexWorkCharge? meter, ulong? steps = null, int start = 0, bool anchored = false)
    {
        var slots = new int[program.SlotCount];
        var status = program.Match(subject, start, anchored, slots, meter, new RegexMatchCeilings(steps, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum), out var limit, out var units);
        return (status, limit, units, slots);
    }

    [Theory]
    [InlineData("a", "a", "", 2)]
    [InlineData("a", "ba", "", 3)]
    [InlineData("a", "bbb", "", 4)]
    [InlineData("(?:a|b)", "b", "", 4)]
    [InlineData("[a]", "a", "", 2)]
    [InlineData("[a-cx-z]", "y", "", 3)]
    [InlineData("\\p{L}", "a", "u", 11)]
    [InlineData("(?:(a)(b))*", "", "", 6)]
    [InlineData("(?:(a)(b)(c)(d))*", "", "", 6)]
    [InlineData("a*", "aa", "", 14)]
    [InlineData("a", "A", "i", 2)]
    [InlineData("[a-cx-z]", "Y", "i", 6)]
    [InlineData("\\p{L}", "a", "iu", 38)]
    [InlineData("[^a-cx-z]", "Y", "i", 10)]
    [InlineData("^a", "ba", "", 4)]
    [InlineData("\\bb", "ab b", "", 24)]
    [InlineData("(a)\\1", "aa", "", 6)]
    [InlineData("(a+)\\1", "aaaa", "", 49)]
    [InlineData("\\1(a)", "a", "", 5)]
    [InlineData("(?=a)a", "a", "", 5)]
    [InlineData("(?!a)b", "ab", "", 7)]
    [InlineData("(?<=a)b", "ab", "", 7)]
    [InlineData("(?<=\\1(a))b", "aab", "", 18)]
    [InlineData("[\\q{ab}c]", "c", "v", 4)]
    public void A_call_s_units_are_one_per_instruction_with_a_class_s_weight_on_top(string pattern, string subject, string flags, ulong units)
    {
        // a on "bbb": a failed Literal at 0, 1 and 2, and at 3, the end. [a-cx-z]: two ranges, weight one. \p{L}: 684
        // ranges, weight nine, so ten for the test and one for Succeed. (?:(a)(b))* on "": Choice, the mark, the reset of
        // both groups (one dispatch, one unit, as decision D8 counts it), group 1's start, the failed Literal, then Succeed
        // from the frame; four groups cost the same. a* on "aa": two iterations of Choice, mark, Literal,
        // RejectEmptyIteration and Goto, then Choice, mark and the failed Literal, then Succeed from the frame. Under i
        // (task 2.4b) a character costs one as before, and a class one plus its weight for each code point of a
        // character's case orbit, four at most: [a-cx-z] under i costs 1 + 4 x 1, and \p{L} under iu 1 + 4 x 9. The
        // inverted [^a-cx-z] fails at Y and at the end of the subject, five units each. ^a on "ba": the assertion and the
        // failed Literal at 0, the assertion alone at 1 and 2. \bb on "ab b": a boundary costs 1 + 2 x 2, the word
        // characters' weight for each side; it holds at 0, 2 and 3 and fails at 1, and the Literal fails twice first. A
        // back-reference costs one for its dispatch and the captured length before it compares: (a)\1 on "aa" is the two
        // saves, the Literal, the reference (1 + 1) and Succeed; \1(a) on "a" reads a group with no capture, one unit.
        // (a+)\1 on "aaaa": the loop's 33 units take all four a's, and the reference is then tried with captures of
        // four, three and two characters, costing 5, 4 and 3, the last matching, with a save before each and Succeed. A
        // look-around's start and close cost one each, and popping its barrier nothing: (?=a)a on "a" is the start, the
        // Literal, the close, the Literal and Succeed. (?!a)b on "ab": at 0 the start, the Literal and the close, which
        // fails; at 1 the start, the failed Literal, the barrier's resumption (free), the Literal and Succeed. (?<=a)b on
        // "ab": at 0 the start and the failed backward Literal; at 1 five. (?<=\1(a))b on "aab": at 0 the start, the
        // group's end and the failed Literal; at 1 the same three with the Literal matching, the group's start and the
        // reference to one character (1 + 1), which finds no character before index 0; at 2 those six, the close, the
        // Literal and Succeed. [\q{ab}c] on "c": the choice, the string's failed first Literal, the class and Succeed.
        // The general lowering, whose loops these units count; the fast paths' are the theory after this one.
        var program = TestProgram.General(pattern, flags);
        Assert.Equal(units, Run(program, subject, null).Units);
    }

    [Theory]
    [InlineData("a*", "aa", "", 5)]
    [InlineData("a*", "", "", 3)]
    [InlineData("[a-cx-z]*", "ay", "", 8)]
    [InlineData("a*?b", "aab", "", 7)]
    [InlineData("a{2}b", "aab", "", 5)]
    [InlineData("abc", "abc", "", 4)]
    [InlineData("abc", "abd", "", 6)]
    [InlineData("[\\q{ab}c]", "c", "v", 3)]
    [InlineData("[\\q{ab}c]", "ab", "v", 4)]
    [InlineData("[\\q{abc|ab}]x", "abx", "v", 8)]
    [InlineData("(?<=a+)b", "aab", "", 17)]
    public void A_fast_path_charges_its_dispatch_and_each_character_it_examines(string pattern, string subject, string flags, ulong units)
    {
        // Task 2.4c. a* on "aa": the run's dispatch, the two a's and the read that finds the end, each the Literal's one
        // unit, then Succeed: five, where the loop of the theory above costs fourteen. [a-cx-z]* reads three times at the
        // class's cost, two. a*?b on "aab": the lazy run takes nothing, b fails at 0, the run's frame takes an a, b fails at
        // 1, the frame takes the second a, b matches, Succeed. a{2}b: two required a's, no frame. An atom run costs what
        // its atoms dispatched one by one cost: abc on "abc" four and on "abd" six, as in the general lowering. [\q{ab}c]
        // on "c": the table's dispatch, one unit (one key, weight none), finds no string under c and the class test, one,
        // matches; on "ab" the string's two characters, a unit each. [\q{abc|ab}]x on "abx": abc fails at its third
        // character, ab matches, then x and Succeed. (?<=a+)b on "aab": at 0 the look-behind's start, the run's dispatch
        // and its first read, which finds nothing before index 0; at 1 those with one a read, the read that finds the start,
        // the close and the failed b; at 2 two a's and the start, the close, b and Succeed.
        Assert.Equal(units, Run(TestProgram.Compile(pattern, flags), subject, null).Units);
    }

    [Fact]
    public void An_atom_run_moves_no_unit()
    {
        // Folding replaces dispatches with reads of the same cost: over every subject and start, a pattern whose only fast
        // path is an atom run charges what its general lowering does.
        foreach (var (pattern, flags) in (ValueTuple<string, string>[])[("abc", ""), ("a\\p{L}.", "u"), ("(?<=ab)c", ""), ("x[a-c]y|z", "i")])
        {
            var folded = TestProgram.Compile(pattern, flags);
            var general = TestProgram.General(pattern, flags);
            Assert.Contains(folded.Code.ToArray(), static i => i.Op == RegexOp.AtomRun);
            foreach (var subject in (string[])["", "abc", "xabcx", "zzab", "aBcXbY", "\U0001F600abc"])
            {
                for (var start = 0; start <= subject.Length; start++)
                    Assert.Equal(Run(general, subject, null, start: start).Units, Run(folded, subject, null, start: start).Units);
            }
        }
    }

    [Fact]
    public void A_class_of_strings_costs_its_lookup_and_the_strings_under_the_character_at_the_index()
    {
        // \p{RGI_Emoji} at a position no emoji begins at: the general lowering tries each of its 2,760 strings, about two
        // units each, where the table's dispatch looks the character up and tries none.
        const string Emoji = "\\p{RGI_Emoji}";
        var general = Run(TestProgram.General(Emoji, "v"), "x", null).Units;
        var fast = Run(TestProgram.Compile(Emoji, "v"), "x", null).Units;
        Assert.True(general > 5_000, general + " units in the general lowering");
        Assert.True(fast < 60, fast + " units with the table");
    }

    /// <summary>
    /// The declared set of inputs of check A-1's recording meter: catastrophic and ordinary patterns, classes of weight
    /// nine and ten, captures reset at each iteration, a quantifier bounded above, a reset of a thousand groups, starts
    /// after the first position and inside a surrogate pair, an anchored start, two step ceilings, and searches that
    /// fail at every position; and task 2.4b's instructions: characters and classes under i, with and without u, a
    /// v-mode class folded when parsed, and the assertions ^ $ \b \B, with m and under iu, modifier groups nested and
    /// undone, back-references, under i and under a step ceiling, whose length is charged after the dispatch and before
    /// the comparison (an order the read at each dispatch cannot see, which
    /// <see cref="A_back_reference_s_length_is_charged_before_any_of_its_characters_is_compared"/> reads), and
    /// look-arounds of the four kinds, a look-behind's back-reference read right to left, one started inside a
    /// surrogate pair, a negative look-behind whose body backtracks to a step ceiling, look-behinds whose bodies read
    /// every kind of character, class and back-reference right to left, and v-mode classes holding strings, the empty
    /// one among them.
    /// </summary>
    public static TheoryData<string, string, string, int, bool, ulong?> DeclaredInputs => new()
    {
        { "(a+)+b", "", "aaaaaaaaac", 0, false, null },
        { "\\p{L}+\\d", "u", "abcdefghijklmnopqrstuvwxyz!", 0, false, null },
        { "[\\p{Ll}\\p{Lu}\\p{Lt}\\p{Lm}\\p{Lo}\\p{Mn}\\p{Mc}\\p{Nd}]+!", "u", new string('a', 300) + "?", 0, false, null },
        { "(?:(a)|b)*c", "", "abababababababababababababababx", 0, false, null },
        { "(?:(a)|b)*c", "", "ababababababababababababababx", 3, true, null },
        { "(?:" + string.Concat(Enumerable.Repeat("(a)", 1000)) + ")*b", "", new string('a', 3000) + "c", 0, false, 20_000UL },
        { "x", "", new string('y', 600), 0, false, null },
        { "(?:x|y)z", "u", new string('a', 5000), 17, false, null },
        { "[\\s\\S]*\\u{1F600}", "u", "\U0001F600ba" + new string('x', 200), 1, false, null },
        { "(a+)+b", "", "aaaaaaaaaaaaaac", 0, false, 7_777UL },
        { "(?:a|aa)(?:a|aa)(?:a|aa)(?:a|aa)(?:a|aa)(?:a|aa)b", "", "aaaaaaaaaaaa", 0, false, null },
        { "(?:[\\p{L}\\p{Nd}]|K)+\\u212A", "iu", string.Concat(Enumerable.Repeat("aB1\u017Fk", 120)) + "!", 0, false, null },
        { "[^\\W\\d]+[^k]x", "i", string.Concat(Enumerable.Repeat("Kk", 300)) + "K", 0, false, null },
        { "[[\\p{Lu}--[A-F]]\\d]+\\P{Ll}", "iv", string.Concat(Enumerable.Repeat("gH7", 200)) + "a", 0, false, null },
        { "^(?:\\b\\w+\\B\\w\\b\\W?)+$", "m", string.Concat(Enumerable.Repeat("ab cd\nef gh ", 80)) + "!", 0, false, null },
        { "\\b\\w+\\b", "iu", string.Concat(Enumerable.Repeat("\u017F\u212A- ", 150)), 2, false, null },
        { "(?:(?i:a(?-i:b))(?m:$\\n^)(?s:.))+x", "", string.Concat(Enumerable.Repeat("Ab\n\nAB\n\n", 100)), 0, false, null },
        { "(\\w+)\\s\\1(?:\\s\\1)*$", "i", string.Concat(Enumerable.Repeat("Word wORD ", 40)) + "word", 0, false, null },
        { "(?<w>a+)\\k<w>b", "u", new string('a', 200) + "b", 0, false, 2_000UL },
        { "(?<=\\b(\\w+)\\s\\1)\\b", "i", string.Concat(Enumerable.Repeat("Word wORD ", 40)), 0, false, null },
        { "(?:(?=(\\w))\\1(?!\\d)|(?<=\\d)\\d)+!", "u", string.Concat(Enumerable.Repeat("ab12", 150)) + "?", 0, false, null },
        { "(?<=(.)).\\1", "u", string.Concat(Enumerable.Repeat("\U0001F600", 100)), 1, false, null },
        { "(?<!b(a+)+)c", "", new string('a', 24) + "c", 0, false, 5_000UL },
        { "[\\q{abc|ab|}x]+y", "vi", string.Concat(Enumerable.Repeat("ABcXab", 100)) + "z", 0, false, null },
        { "(?<=[\\q{ab|b}])\\p{RGI_Emoji_Flag_Sequence}", "v", string.Concat(Enumerable.Repeat("ab\U0001F1EF", 60)), 0, false, null },
        { "(?:(a)|b){2,4}c", "", string.Concat(Enumerable.Repeat("abab", 150)) + "x", 0, false, null },
        { "(?<=(?s:.)\\1(?i:[a-c]k)(a)[^\\d])x", "", string.Concat(Enumerable.Repeat("\naBKazx1", 80)), 0, false, null },
        { "(?<=[\\p{Ll}--[q]]{2})\\d", "iv", string.Concat(Enumerable.Repeat("aB1Q2", 120)) + "!", 0, false, null },
        { "[a-z]+?\\d", "", string.Concat(Enumerable.Repeat("abcdefghij", 60)) + "!", 0, false, null },
        { "(?<=[\\d]{2,}?x|ab)\\w*cd", "", string.Concat(Enumerable.Repeat("12xyzcdabq", 60)), 0, false, null },
        { "\\w*?[\\q{xyz|xy|x}]{2}", "v", string.Concat(Enumerable.Repeat("axbxyc", 90)), 0, false, 3_000UL },
    };

    [Theory]
    [MemberData(nameof(DeclaredInputs))]
    public void Before_the_kth_unit_of_work_at_least_k_units_are_charged_and_never_a_block_more_than_the_work(
        string pattern, string flags, string subject, int start, bool anchored, ulong? steps)
    {
        // Check A-1, read at every dispatch: the loop's observer is called after an instruction's units are charged and
        // before it runs, with the units through it, k; the recording meter's total at that moment is what was charged
        // before the k-th unit of work. It is at least k, and less than a block more.
        var program = TestProgram.Compile(pattern, flags);
        var meter = new RecordingMeter();
        var through = new List<ulong>();
        var faults = new List<string>();
        void Observe(ulong k)
        {
            if (faults.Count < 5 && (through.Count > 0 && k <= through[^1] || meter.Charged < k || meter.Charged - k >= RegexMachine.Block))
                faults.Add("dispatch " + (through.Count + 1) + ": " + meter.Charged + " charged before unit " + k);
            through.Add(k);
        }

        var slots = new int[program.SlotCount];
        var machine = new RegexMachine(program, subject, meter.Charge, new RegexMatchCeilings(steps, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum))
        {
            DispatchObserver = Observe,
        };
        RegexStatus status;
        ulong units;
        try
        {
            status = machine.Run(start, anchored, slots);
            units = machine.Spent;
        }
        finally
        {
            machine.Release();
        }

        Assert.Empty(faults);
        Assert.Equal(steps is null, status != RegexStatus.LimitExceeded);
        Assert.Equal(units, through[^1]);
        Assert.True(meter.Charged >= units && meter.Charged - units < RegexMachine.Block, meter.Charged + " charged for " + units + " units");
        Assert.All(meter.Charges, static c => Assert.True(c > 0 && c % RegexMachine.Block == 0, "a charge of " + c));
        Assert.True(units <= RegexMachine.Block || meter.Charges.Count > 1, units + " units were charged in one block");
    }

    /// <summary>The caller's tables, reporting each read to <see cref="Read"/> while it is set.</summary>
    private sealed class WatchedTables(IUnicodeTables inner) : IUnicodeTables
    {
        internal Action? Read { get; set; }

        public string UnicodeVersion => inner.UnicodeVersion;

        public ReadOnlySpan<byte> Table(UnicodeTable table)
        {
            Read?.Invoke();
            return inner.Table(table);
        }
    }

    [Theory]
    [InlineData("^(.{600})\\1$", "is", false)]
    [InlineData("^(.{600})\\1$", "ius", false)]
    [InlineData("(?<=^\\1(.{600}))", "is", true)]
    public void A_back_reference_s_length_is_charged_before_any_of_its_characters_is_compared(string pattern, string flags, bool fromTheEnd)
    {
        // Decision D8 inside one dispatch: a back-reference charges its capture's length, then compares. Check A-1's
        // read at each dispatch cannot see that order, as the units, the charges and the answer are the same either way;
        // the caller's tables can, since a comparison under i reads them through the case folding. Only the reference
        // folds here (the dot under s reads no table, nor do the assertions and the group), so the first read of the
        // tables during the call is the reference's first comparison. By then the observer has seen the length charged,
        // one step of 600 units, more than a block, and the meter holds every unit through it.
        const int length = 600;
        var tables = new WatchedTables(UnicodeTables.Instance);
        Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(pattern, RegexFlagsParser.Parse(flags), tables, BroilerRegex.MaximumNestingLimit, out var program));
        var subject = new string('a', length) + new string('A', length);
        var meter = new RecordingMeter();
        var through = new List<ulong>();
        (ulong Charged, int Observed)? firstRead = null;
        tables.Read = () => firstRead ??= (meter.Charged, through.Count);

        var slots = new int[program!.SlotCount];
        var machine = new RegexMachine(program, subject, meter.Charge, MachineTests.Ample) { DispatchObserver = through.Add };
        RegexStatus status;
        try
        {
            status = machine.Run(fromTheEnd ? subject.Length : 0, anchored: true, slots);
        }
        finally
        {
            machine.Release();
            tables.Read = null;
        }

        Assert.Equal(RegexStatus.Match, status);
        Assert.NotNull(firstRead);
        var (charged, observed) = firstRead.Value;
        Assert.True(observed >= 2, "the tables were read before the reference was dispatched");
        Assert.Equal((ulong)length, through[observed - 1] - through[observed - 2]);
        Assert.True(charged >= through[observed - 1], charged + " charged when the reference's first character was compared, through unit " + through[observed - 1]);
    }

    [Fact]
    public void The_declared_inputs_reach_every_instruction_in_both_directions()
    {
        // Check A-1 covers each instruction the compiler emits: every operation of RegexOp appears in the program of at
        // least one declared input, and every operation that reads the subject, each character, class and back-reference
        // test, appears read right to left in a look-behind's body; no other operation is given a direction.
        var code = DeclaredInputs.Select(static row => TestProgram.Compile((string)row[0], (string)row[1]))
            .SelectMany(static program => program.Code.ToArray()).ToList();
        var forward = code.Select(static i => i.Op).ToHashSet();
        Assert.Empty(Enum.GetValues<RegexOp>().Where(op => !forward.Contains(op)));
        var backward = code.Where(static i => i.RightToLeft).Select(static i => i.Op).ToHashSet();
        RegexOp[] read =
        [
            RegexOp.Literal, RegexOp.LiteralIgnoringCase, RegexOp.Class, RegexOp.ClassIgnoringCase, RegexOp.CanonicalClass,
            RegexOp.Any, RegexOp.AnyButLineTerminator, RegexOp.Backreference, RegexOp.BackreferenceIgnoringCase,
            RegexOp.GreedyRun, RegexOp.LazyRun, RegexOp.AtomRun, RegexOp.StringClass,
        ];
        Assert.Empty(read.Where(op => !backward.Contains(op)));
        Assert.Empty(backward.Where(op => !read.Contains(op)));
    }

    /// <summary>Inputs whose every step ceiling, from one unit to their whole cost, is tried.</summary>
    public static TheoryData<string, string> StepCeilingInputs => new()
    {
        { "(a+)+b", "aaaaaaaaac" },
        { "\\p{L}+\\d", "abcdefghijklmnopqrstuvwxyz!" },
        { "(?:(a)|b)*c", "abababababababababababababababx" },
        { "x", new string('y', 600) },
        { "(?:a|aa)(?:a|aa)(?:a|aa)(?:a|aa)(?:a|aa)(?:a|aa)b", "aaaaaaaaaaaa" },
    };

    [Theory]
    [MemberData(nameof(StepCeilingInputs))]
    public void A_step_ceiling_of_k_units_stops_the_call_before_the_instruction_that_would_pass_it(string pattern, string subject)
    {
        var program = TestProgram.Compile(pattern, "u");
        var full = Run(program, subject, null);
        var total = full.Units;
        Assert.InRange(total, 200UL, 30_000UL);
        var dearest = (ulong)program.Code.ToArray().Max(static i => i.Cost);

        // Every ceiling below the call's whole cost answers LimitExceeded naming steps, within the dearest instruction's
        // cost of the ceiling; the whole cost and more answer as the call without a ceiling does. The meter is never
        // charged less than the work, nor a block more.
        for (ulong k = 1; k <= total + 1; k++)
        {
            var meter = new RecordingMeter();
            var run = Run(program, subject, meter.Charge, steps: k);
            if (k < total)
            {
                Assert.Equal(RegexStatus.LimitExceeded, run.Status);
                Assert.Equal(RegexLimit.Steps, run.Limit);
                Assert.InRange(run.Units, k + 1 > dearest ? k + 1 - dearest : 0, k);
            }
            else
            {
                Assert.Equal(full.Status, run.Status);
                Assert.Equal(total, run.Units);
            }

            Assert.True(meter.Charged >= run.Units, "k = " + k + ": " + meter.Charged + " charged for " + run.Units + " units");
            Assert.True(meter.Charged - run.Units < RegexMachine.Block, "k = " + k + ": over-charged by a block or more");
        }
    }

    [Fact]
    public void The_first_charge_comes_before_the_first_instruction()
    {
        var meter = new RecordingMeter();
        var run = Run(TestProgram.Compile("a"), "a", meter.Charge, steps: 1);
        Assert.Equal((RegexStatus.LimitExceeded, 1UL), (run.Status, run.Units));
        Assert.Equal([RegexMachine.Block], meter.Charges);
    }

    [Fact]
    public void A_call_without_a_meter_does_the_same_work()
    {
        var program = TestProgram.Compile("(?:(a)|b)*c");
        var meter = new RecordingMeter();
        Assert.Equal(Run(program, "ababx", null).Units, Run(program, "ababx", meter.Charge).Units);
    }

    [Fact]
    public void A_non_matching_subject_of_n_positions_charges_at_least_n_units()
    {
        const int n = 10_000;
        var meter = new RecordingMeter();
        var run = Run(TestProgram.Compile("b"), new string('a', n), meter.Charge);
        Assert.Equal(RegexStatus.NoMatch, run.Status);
        Assert.True(run.Units >= n + 1);
        Assert.True(meter.Charged >= run.Units);
    }

    [Fact]
    public void One_call_charges_across_all_its_start_positions_with_no_reset()
    {
        // Each start position's attempt alone fits under the ceiling; the search's attempts together do not, so a ceiling
        // reset at each start position would have answered NoMatch.
        var program = TestProgram.Compile("(a+)+b");
        var subject = new string('a', 12);
        var perStart = Enumerable.Range(0, subject.Length + 1).Select(s => Run(program, subject, null, start: s, anchored: true)).ToList();
        Assert.All(perStart, static r => Assert.Equal(RegexStatus.NoMatch, r.Status));
        var dearest = perStart.Max(static r => r.Units);
        var sum = (ulong)perStart.Sum(static r => (long)r.Units);
        Assert.Equal(sum, Run(program, subject, null).Units);

        var meter = new RecordingMeter();
        var search = Run(program, subject, meter.Charge, steps: dearest + 1);
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Steps), (search.Status, search.Limit));
        Assert.True(meter.Charged >= search.Units);
    }

    [Fact]
    public void A_meter_that_throws_at_its_budget_propagates_and_the_program_answers_a_fresh_call_as_a_new_one_does()
    {
        var program = TestProgram.Compile("(a+)+b");
        var subject = new string('a', 22) + "b";
        var meter = new RecordingMeter(budget: 10_000);
        Assert.Throws<MeterSpent>(() => Run(program, new string('a', 22), meter.Charge));
        Assert.True(meter.Charged > 10_000);
        Assert.True(meter.Charged <= 10_000 + RegexMachine.Block);

        var fresh = TestProgram.Compile("(a+)+b");
        foreach (var text in (string[])[subject, "xaab", "aaaa"])
        {
            var again = Run(program, text, null);
            var anew = Run(fresh, text, null);
            Assert.Equal((anew.Status, anew.Units), (again.Status, again.Units));
            Assert.Equal(anew.Slots, again.Slots);
        }
    }

    [Fact]
    public void A_meter_may_match_the_same_program_while_it_is_being_charged()
    {
        // A meter is the caller's code and may run a match of its own; each call's state is its own.
        var program = TestProgram.Compile("(a|b)*c");
        var inner = new List<string>();
        void Meter(ulong units)
        {
            var slots = new int[program.SlotCount];
            inner.Add(MachineTests.Render(program.Match("abc", 0, false, slots, null, MachineTests.Ample, out _, out _), slots));
        }

        var outer = new int[program.SlotCount];
        Assert.Equal(RegexStatus.Match, program.Match("ababababc", 0, false, outer, Meter, MachineTests.Ample, out _, out _));
        Assert.Equal("match 0,9 7,8", MachineTests.Render(RegexStatus.Match, outer));
        Assert.All(inner, static r => Assert.Equal("match 0,3 1,2", r));
        Assert.NotEmpty(inner);
    }

    [Fact]
    public void Two_calls_of_the_same_inputs_charge_the_same()
    {
        var program = TestProgram.Compile("(?:a|aa)*b");
        var first = new RecordingMeter();
        var second = new RecordingMeter();
        Run(program, new string('a', 18), first.Charge);
        Run(program, new string('a', 18), second.Charge);
        Assert.Equal(first.Charges, second.Charges);
    }
}
