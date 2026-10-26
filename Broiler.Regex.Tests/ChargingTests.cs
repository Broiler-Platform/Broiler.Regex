using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.Regex.Engine;
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
    public void A_call_s_units_are_one_per_instruction_with_a_class_s_weight_on_top(string pattern, string subject, string flags, ulong units)
    {
        // a on "bbb": a failed Literal at 0, 1 and 2, and at 3, the end. [a-cx-z]: two ranges, weight one. \p{L}: 684
        // ranges, weight nine, so ten for the test and one for Succeed. (?:(a)(b))* on "": Choice, the mark, the reset of
        // both groups (one dispatch, one unit, as decision D8 counts it), group 1's start, the failed Literal, then Succeed
        // from the frame; four groups cost the same. a* on "aa": two iterations of Choice, mark, Literal,
        // RejectEmptyIteration and Goto, then Choice, mark and the failed Literal, then Succeed from the frame.
        var program = TestProgram.Compile(pattern, flags);
        Assert.Equal(units, Run(program, subject, null).Units);
    }

    /// <summary>
    /// The declared set of inputs of check A-1's recording meter: catastrophic and ordinary patterns, a class of weight nine,
    /// captures reset at each iteration, and a search that fails at every position.
    /// </summary>
    public static TheoryData<string, string> DeclaredInputs => new()
    {
        { "(a+)+b", "aaaaaaaaac" },
        { "\\p{L}+\\d", "abcdefghijklmnopqrstuvwxyz!" },
        { "(?:(a)|b)*c", "abababababababababababababababx" },
        { "x", new string('y', 600) },
        { "(?:a|aa)(?:a|aa)(?:a|aa)(?:a|aa)(?:a|aa)(?:a|aa)b", "aaaaaaaaaaaa" },
    };

    [Theory]
    [MemberData(nameof(DeclaredInputs))]
    public void Before_the_kth_unit_of_work_at_least_k_units_are_charged_and_never_a_block_more_than_the_work(string pattern, string subject)
    {
        var program = TestProgram.Compile(pattern, "u");
        var full = Run(program, subject, null);
        var total = full.Units;
        Assert.InRange(total, 200UL, 30_000UL);
        var dearest = (ulong)program.Code.ToArray().Max(static i => i.Cost);

        // A step ceiling of k stops the call before the instruction that would take it past k units, so the meter's total
        // at that stop is what was charged before every instruction the call dispatched ran. Over every k, that covers each
        // instruction: charged before it ran, at least the units up to and including its own.
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
            Assert.All(meter.Charges, static c => Assert.True(c > 0 && c % RegexMachine.Block == 0));
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
