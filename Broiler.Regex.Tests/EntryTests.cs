using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Broiler.Regex.Engine;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The new backend's match entry (requirements R7 and R8; bundle <c>sjl-11-001</c>'s checks A-7, A-8 and A-11):
/// its argument rules, the caller's slots, a call that allocates nothing that grows with the capture count, and one
/// program shared by concurrent calls.
/// </summary>
public class EntryTests
{
    [Fact]
    public void A_start_outside_the_subject_short_slots_or_unset_ceilings_are_broken_preconditions()
    {
        var program = TestProgram.Compile("(a)");
        var slots = new int[program.SlotCount];
        Assert.Throws<ArgumentOutOfRangeException>(() => program.Match("abc", -1, false, slots, null, RegexMatchCeilings.Engines, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => program.Match("abc", 4, false, slots, null, RegexMatchCeilings.Engines, out _, out _));
        Assert.Throws<ArgumentException>(() => program.Match("abc", 0, false, new int[program.SlotCount - 1], null, RegexMatchCeilings.Engines, out _, out _));
        Assert.Throws<ArgumentException>(() => program.Match("abc", 0, false, slots, null, default, out _, out _));

        // The start may be the subject's length.
        Assert.Equal(RegexStatus.NoMatch, program.Match("abc", 3, false, slots, null, RegexMatchCeilings.Engines, out _, out _));
    }

    [Fact]
    public void A_match_writes_its_slots_and_leaves_the_caller_s_slots_past_them_alone()
    {
        var program = TestProgram.Compile("(a)(x)?");
        var slots = Enumerable.Repeat(7, program.SlotCount + 3).ToArray();
        Assert.Equal(RegexStatus.Match, program.Match("ba", 0, false, slots, null, RegexMatchCeilings.Engines, out var limit, out _));
        Assert.Equal(RegexLimit.None, limit);
        Assert.Equal([1, 2, 1, 2, -1, -1, 7, 7, 7], slots);
    }

    [Fact]
    public void Any_other_answer_leaves_every_slot_minus_one()
    {
        var program = TestProgram.Compile("(a+)+b");
        var slots = Enumerable.Repeat(7, program.SlotCount).ToArray();
        Assert.Equal(RegexStatus.NoMatch, program.Match("aaa", 0, false, slots, null, RegexMatchCeilings.Engines, out _, out _));
        Assert.Equal([-1, -1, -1, -1], slots);

        slots = Enumerable.Repeat(7, program.SlotCount).ToArray();
        Assert.Equal(RegexStatus.LimitExceeded, program.Match(new string('a', 30), 0, false, slots, null, new RegexMatchCeilings(10_000, RegexMatchCeilings.EngineFrames, RegexMatchCeilings.EngineTrail), out var limit, out _));
        Assert.Equal(RegexLimit.Steps, limit);
        Assert.Equal([-1, -1, -1, -1], slots);

        slots = Enumerable.Repeat(7, program.SlotCount).ToArray();
        var meter = new ChargingTests.RecordingMeter(budget: 1_000);
        Assert.Throws<ChargingTests.MeterSpent>(() => program.Match(new string('a', 30), 0, false, slots, meter.Charge, RegexMatchCeilings.Engines, out _, out _));
        Assert.Equal([-1, -1, -1, -1], slots);
    }

    /// <summary>
    /// A pattern of <paramref name="groups"/> capture groups, each an alternation that leaves a frame pending, so that the
    /// registers, the frames and the trail all grow with the capture count.
    /// </summary>
    private static (RegexProgram Program, string Subject) Captures(int groups) =>
        (TestProgram.Compile(string.Concat(Enumerable.Repeat("(?:(a)|b)", groups))), new string('a', groups));

    [Fact]
    public void A_call_allocates_nothing_that_grows_with_the_capture_count()
    {
        // Bundle sjl-11-001's check A-8: after the shared pool holds the arrays a program needs, a call allocates nothing,
        // whatever its capture count; the bytes are those the measuring thread allocates.
        var measured = new long[4];
        var counts = new[] { 1, 10, 100, 1_000 };
        ProgramCompilerTests.OnASmallStack(() =>
        {
            for (var i = 0; i < counts.Length; i++)
            {
                var (program, subject) = Captures(counts[i]);
                var slots = new int[program.SlotCount];
                for (var warm = 0; warm < 3; warm++)
                    Assert.Equal(RegexStatus.Match, program.Match(subject, 0, false, slots, null, RegexMatchCeilings.Engines, out _, out _));

                var before = GC.GetAllocatedBytesForCurrentThread();
                program.Match(subject, 0, false, slots, null, RegexMatchCeilings.Engines, out _, out _);
                measured[i] = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.Equal(counts[i], slots[^1]);
            }
        });
        Assert.Equal([0L, 0L, 0L, 0L], measured);
    }

    [Fact]
    public void A_call_that_ends_with_the_meter_s_exception_gives_its_arrays_back()
    {
        var (program, subject) = Captures(200);
        var measured = 0L;
        ProgramCompilerTests.OnASmallStack(() =>
        {
            var slots = new int[program.SlotCount];
            for (var warm = 0; warm < 3; warm++)
                program.Match(subject, 0, false, slots, null, RegexMatchCeilings.Engines, out _, out _);

            // The meter throws at its first block; the arrays the call rented go back to the pool all the same, so the
            // next call allocates nothing.
            Assert.Throws<ChargingTests.MeterSpent>(() => program.Match(subject, 0, false, slots, static _ => throw new ChargingTests.MeterSpent(), RegexMatchCeilings.Engines, out _, out _));
            var before = GC.GetAllocatedBytesForCurrentThread();
            var status = program.Match(subject, 0, false, slots, null, RegexMatchCeilings.Engines, out _, out _);
            measured = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(RegexStatus.Match, status);
        });
        Assert.Equal(0L, measured);
    }

    [Fact]
    public void A_call_s_frames_and_trail_past_the_pooled_length_go_to_the_collector_not_the_shared_pool()
    {
        // a* over 100,000 characters holds 100,001 frames (300,003 integers) and 100,000 trail cells (200,000 integers)
        // at its deepest. Arrays that long are the call's alone: once it ends, renting their lengths from the shared
        // pool on the same thread finds nothing the call left there and allocates them anew.
        var program = TestProgram.Compile("a*");
        var subject = new string('a', 100_000);
        long frames = 0, trail = 0;
        ProgramCompilerTests.OnASmallStack(() =>
        {
            var slots = new int[program.SlotCount];
            Assert.Equal(RegexStatus.Match, program.Match(subject, 0, false, slots, null, MachineTests.Ample, out _, out _));
            var before = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(System.Buffers.ArrayPool<int>.Shared.Rent(300_003));
            frames = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(System.Buffers.ArrayPool<int>.Shared.Rent(200_000));
            trail = GC.GetAllocatedBytesForCurrentThread() - before;
        });
        Assert.True(frames >= 4L * 300_003, frames + " bytes allocated to rent the frames' length");
        Assert.True(trail >= 4L * 200_000, trail + " bytes allocated to rent the trail's length");
    }

    [Fact]
    public void One_program_shared_by_concurrent_calls_answers_each_as_it_answers_alone()
    {
        // Bundle sjl-11-001's check A-11 for the new backend: the program is immutable and every call's state is its own.
        var program = TestProgram.Compile("(?:(a)|(b))+(c+)?d");
        var subjects = Enumerable.Range(0, 64).Select(static i =>
        {
            var text = new StringBuilder();
            for (var j = 0; j < i % 17 + 1; j++)
                text.Append((i + j) % 3 == 0 ? 'b' : 'a');
            if (i % 2 == 0)
                text.Append('c', i % 5);
            if (i % 7 != 0)
                text.Append('d');
            return text.ToString();
        }).ToArray();

        string Answer(string subject)
        {
            var slots = new int[program.SlotCount];
            var status = program.Match(subject, 0, false, slots, null, RegexMatchCeilings.Engines, out _, out var units);
            return MachineTests.Render(status, slots) + " units " + units;
        }

        var alone = subjects.Select(Answer).ToArray();
        var together = new string[subjects.Length * 16];
        Parallel.For(0, together.Length, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i => together[i] = Answer(subjects[i % subjects.Length]));
        for (var i = 0; i < together.Length; i++)
            Assert.Equal(alone[i % subjects.Length], together[i]);
    }
}
