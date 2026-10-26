using System;
using System.Linq;
using System.Reflection;
using Broiler.Regex.Engine;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The public offset entry of task 2.4c, <c>BroilerRegex.Match</c> over a span (requirements R7 and R8; bundle
/// <c>sjl-11-001</c>'s checks A-7 and A-8): its signature carries no string, its anchored argument is independent of the
/// <c>y</c> flag, it writes the caller's slots, answers statuses with the ceiling reached, charges the caller's meter, and
/// allocates nothing that grows with the capture count, up to the frames and trail cells the shared pool keeps. The
/// allocation readings hold the garbage collector off, so the class runs in their collection
/// (<see cref="AllocationReading"/>).
/// </summary>
[Collection(AllocationReading.Collection)]
public class PublicEntryTests
{
    private static BroilerRegex Regex(string pattern, string flags = "") => new(pattern, flags, UnicodeTables.Instance);

    /// <summary>The entry, found by its parameter types.</summary>
    private static MethodInfo Entry => typeof(BroilerRegex).GetMethod(nameof(BroilerRegex.Match),
        [typeof(ReadOnlySpan<char>), typeof(int), typeof(bool), typeof(Span<int>), typeof(RegexWorkCharge), typeof(RegexMatchCeilings), typeof(RegexLimit).MakeByRefType()])
        ?? throw new InvalidOperationException("no offset entry");

    [Fact]
    public void The_entry_s_signature_carries_no_string_and_answers_a_status()
    {
        // Check A-8's signature half: no parameter or result of the entry, nor of the types it takes, is or holds a string.
        var entry = Entry;
        Assert.True(entry.IsPublic && !entry.IsStatic);
        Assert.Equal(typeof(RegexStatus), entry.ReturnType);
        Assert.DoesNotContain(entry.GetParameters(), static p => p.ParameterType == typeof(string) || p.ParameterType.GetElementType() == typeof(string));
        Assert.True(typeof(RegexWorkCharge).IsPublic && typeof(RegexMatchCeilings).IsPublic && typeof(RegexMatchCeilings).IsValueType);
        Assert.DoesNotContain(typeof(RegexMatchCeilings).GetProperties(), static p => p.PropertyType == typeof(string));
        Assert.Equal(typeof(void), typeof(RegexWorkCharge).GetMethod("Invoke")!.ReturnType);
        Assert.Equal([typeof(ulong)], typeof(RegexWorkCharge).GetMethod("Invoke")!.GetParameters().Select(static p => p.ParameterType));
    }

    [Fact]
    public void A_match_writes_the_caller_s_slots_and_leaves_those_past_them_alone()
    {
        var regex = Regex("(a)(x)?");
        Assert.Equal(4 + 2, regex.SlotCount);
        var slots = Enumerable.Repeat(7, regex.SlotCount + 2).ToArray();
        Assert.Equal(RegexStatus.Match, regex.Match("ba".AsSpan(), 0, false, slots, null, RegexMatchCeilings.Engines, out var limit));
        Assert.Equal(RegexLimit.None, limit);
        Assert.Equal([1, 2, 1, 2, -1, -1, 7, 7], slots);

        Assert.Equal(RegexStatus.NoMatch, regex.Match("bb".AsSpan(), 0, false, slots, null, RegexMatchCeilings.Engines, out limit));
        Assert.Equal(RegexLimit.None, limit);
        Assert.Equal([-1, -1, -1, -1, -1, -1, 7, 7], slots);
    }

    [Fact]
    public void The_anchored_argument_is_independent_of_the_y_flag()
    {
        // Check A-7 on the public entry: /b/ and /b/y answer alike for each value of the argument, a search finding the b
        // at 1 and an anchored call at 0 none.
        foreach (var flags in (string[])["", "y"])
        {
            var regex = Regex("b", flags);
            var slots = new int[regex.SlotCount];
            Assert.Equal(RegexStatus.Match, regex.Match("ab".AsSpan(), 0, false, slots, null, RegexMatchCeilings.Engines, out _));
            Assert.Equal([1, 2], slots);
            Assert.Equal(RegexStatus.NoMatch, regex.Match("ab".AsSpan(), 0, true, slots, null, RegexMatchCeilings.Engines, out _));
            Assert.Equal(RegexStatus.Match, regex.Match("ab".AsSpan(), 1, true, slots, null, RegexMatchCeilings.Engines, out _));
            Assert.Equal([1, 2], slots);
        }
    }

    [Fact]
    public void A_ceiling_is_a_status_naming_it_and_the_meter_s_exception_reaches_the_caller()
    {
        var regex = Regex("(a+)+b");
        var subject = new string('a', 30);
        var slots = Enumerable.Repeat(7, regex.SlotCount).ToArray();
        Assert.Equal(RegexStatus.LimitExceeded, regex.Match(subject.AsSpan(), 0, false, slots, null, new RegexMatchCeilings(10_000, RegexMatchCeilings.EngineFrames, RegexMatchCeilings.EngineTrail), out var limit));
        Assert.Equal(RegexLimit.Steps, limit);
        Assert.Equal([-1, -1, -1, -1], slots);

        // The meter is charged in blocks before the work; one that throws at its allowance stops the call, and the same
        // instance then answers a new call as a new one does.
        var meter = new ChargingTests.RecordingMeter(budget: 1_000);
        Assert.Throws<ChargingTests.MeterSpent>(() => regex.Match(subject.AsSpan(), 0, false, slots, meter.Charge, RegexMatchCeilings.Engines, out _));
        Assert.All(meter.Charges, static c => Assert.Equal(0UL, c % RegexMachine.Block));
        Assert.Equal(RegexStatus.Match, regex.Match("aab".AsSpan(), 0, false, slots, null, RegexMatchCeilings.Engines, out _));
        Assert.Equal([0, 3, 0, 2], slots);
    }

    [Fact]
    public void A_start_outside_the_subject_short_slots_or_unset_ceilings_are_broken_preconditions()
    {
        var regex = Regex("(a)");
        var slots = new int[regex.SlotCount];
        Assert.Throws<ArgumentOutOfRangeException>(() => regex.Match("abc".AsSpan(), -1, false, slots, null, RegexMatchCeilings.Engines, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => regex.Match("abc".AsSpan(), 4, false, slots, null, RegexMatchCeilings.Engines, out _));
        Assert.Throws<ArgumentException>(() => regex.Match("abc".AsSpan(), 0, false, new int[regex.SlotCount - 1], null, RegexMatchCeilings.Engines, out _));
        Assert.Throws<ArgumentException>(() => regex.Match("abc".AsSpan(), 0, false, slots, null, default, out _));
        Assert.Equal(RegexStatus.NoMatch, regex.Match("abc".AsSpan(), 3, false, slots, null, RegexMatchCeilings.Engines, out _));
    }

    [Fact]
    public void A_start_inside_a_surrogate_pair_is_taken_as_given_and_the_step_back_is_the_caller_s()
    {
        // /[\s\S]/u from index 1 of "\u{1F600}": the entry reads the pair's low half as a lone unit and matches it at 1;
        // a caller answering as RegExpBuiltinExec steps back to 0 itself and gets the whole pair, then decides the index
        // it reports from the offsets and its own start (row M4; decision D41 for an empty match).
        var regex = Regex("[\\s\\S]", "u");
        var slots = new int[regex.SlotCount];
        Assert.Equal(RegexStatus.Match, regex.Match("\U0001F600".AsSpan(), 1, false, slots, null, RegexMatchCeilings.Engines, out _));
        Assert.Equal([1, 2], slots);
        Assert.Equal(RegexStatus.Match, regex.Match("\U0001F600".AsSpan(), 0, false, slots, null, RegexMatchCeilings.Engines, out _));
        Assert.Equal([0, 2], slots);
    }

    [Fact]
    public void A_call_allocates_nothing_that_grows_with_the_capture_count()
    {
        // Check A-8 on the public entry: after the shared pool holds the arrays a pattern needs, a call allocates nothing,
        // at 1 group or at 1,000, with no meter or with one: each of twenty calls, with no collection to trim the pool
        // between them (AllocationReading).
        var counts = new[] { 1, 10, 100, 1_000 };
        var measured = new long[counts.Length * 2][];
        RegexWorkCharge meter = static _ => { };
        ProgramCompilerTests.OnASmallStack(() =>
        {
            for (var i = 0; i < counts.Length; i++)
            {
                var regex = Regex(string.Concat(Enumerable.Repeat("(?:(a)|b)", counts[i])));
                var subject = new string('a', counts[i]);
                var slots = new int[regex.SlotCount];
                for (var metered = 0; metered < 2; metered++)
                {
                    var charge = metered == 0 ? null : meter;
                    var status = RegexStatus.NoMatch;
                    measured[(2 * i) + metered] = AllocationReading.BytesPerCall(() => status = regex.Match(subject.AsSpan(), 0, false, slots, charge, RegexMatchCeilings.Engines, out _));
                    Assert.Equal(RegexStatus.Match, status);
                    Assert.Equal(counts[i], slots[^1]);
                }
            }
        });
        Assert.All(measured, static calls => Assert.Equal(new long[calls.Length], calls));
    }

    [Theory]
    [InlineData("(a)", 16_384, 0)]
    [InlineData("(a)", 16_385, 4 << 17)]
    [InlineData("(?:a|bc)", 16_384, 0)]
    [InlineData("(?:a|bc)", 16_385, 4 << 17)]
    public void A_call_allocates_nothing_up_to_the_frames_and_trail_cells_the_pool_keeps_and_past_them_its_own_arrays(
        string group, int count, int least)
    {
        // Check A-8's bound. The frames and the trail come from the shared pool up to RegexMachine.PooledLength integers,
        // 65,536: 16,384 frames of four and 32,768 trail cells of two. A longer array is the call's alone, so that one
        // call's peak does not stay pooled (task 2.4a). (a) repeated writes two trail cells a group and holds no frame,
        // so 16,384 groups that all participate allocate nothing once the pool is warm, and one more allocates, in each
        // call, the trail's next doubling, 131,072 integers; (?:a|bc) repeated holds a frame each and writes no cell,
        // the same bound in frames. Past it, then, a call allocates what grows with the capture count.
        Assert.Equal(1 << 16, RegexMachine.PooledLength);
        var regex = Regex(string.Concat(Enumerable.Repeat(group, count)));
        var subject = new string('a', count);
        var slots = new int[regex.SlotCount];
        var measured = new long[2][];
        RegexWorkCharge meter = static _ => { };
        ProgramCompilerTests.OnASmallStack(() =>
        {
            for (var metered = 0; metered < 2; metered++)
            {
                var charge = metered == 0 ? null : meter;
                var status = RegexStatus.NoMatch;
                measured[metered] = AllocationReading.BytesPerCall(() => status = regex.Match(subject.AsSpan(), 0, false, slots, charge, RegexMatchCeilings.Engines, out _), calls: 10);
                Assert.Equal(RegexStatus.Match, status);
            }
        });
        Assert.All(measured, calls => Assert.All(calls, bytes => Assert.True(least == 0 ? bytes == 0 : bytes >= least, bytes + " bytes in a call")));
    }
}
