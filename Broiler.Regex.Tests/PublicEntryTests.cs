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
/// allocates nothing that grows with the capture count.
/// </summary>
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
        // at 1 group or at 1,000, with no meter or with one; the least of five calls is read, as a collection between two
        // calls may trim the pool.
        var counts = new[] { 1, 10, 100, 1_000 };
        var measured = new long[counts.Length * 2];
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
                    for (var warm = 0; warm < 3; warm++)
                        Assert.Equal(RegexStatus.Match, regex.Match(subject.AsSpan(), 0, false, slots, charge, RegexMatchCeilings.Engines, out _));

                    measured[(2 * i) + metered] = long.MaxValue;
                    for (var call = 0; call < 5; call++)
                    {
                        var before = GC.GetAllocatedBytesForCurrentThread();
                        regex.Match(subject.AsSpan(), 0, false, slots, charge, RegexMatchCeilings.Engines, out _);
                        measured[(2 * i) + metered] = Math.Min(measured[(2 * i) + metered], GC.GetAllocatedBytesForCurrentThread() - before);
                    }

                    Assert.Equal(counts[i], slots[^1]);
                }
            }
        });
        Assert.Equal(new long[counts.Length * 2], measured);
    }
}
