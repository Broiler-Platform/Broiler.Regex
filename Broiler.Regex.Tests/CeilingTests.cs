using System;
using System.Linq;
using Broiler.Regex.Engine;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The caller's ceilings (requirements R1 and R3; decisions D8 and D10 of Broiler.VM's JSD-0081; bundle
/// <c>sjl-11-001</c>'s checks A-1 and A-3): steps, frames and trail, each reached and answered
/// <see cref="RegexStatus.LimitExceeded"/> naming it, never <see cref="RegexStatus.NoMatch"/> or a match; and the
/// argument rules.
/// </summary>
public class CeilingTests
{
    private static (RegexStatus Status, RegexLimit Limit, string Answer) Run(string pattern, string subject, ulong? steps = null,
        int frames = RegexMatchCeilings.MaximumFrames, int trail = RegexMatchCeilings.MaximumTrail)
    {
        var program = TestProgram.Compile(pattern);
        var slots = new int[program.SlotCount];
        var status = program.Match(subject, 0, false, slots, null, new RegexMatchCeilings(steps, frames, trail), out var limit, out _);
        return (status, limit, MachineTests.Render(status, slots));
    }

    [Fact]
    public void The_frame_ceiling_is_reached_one_frame_past_what_the_match_holds_at_once()
    {
        // a* over "aaaa" holds five frames at its deepest: one per iteration, and the fifth iteration's, whose body fails.
        Assert.Equal("match 0,4", Run("a*", "aaaa", frames: 5).Answer);
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Frames), Run("a*", "aaaa", frames: 4).Head());
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Frames), Run("a*", "aaaa", frames: 1).Head());

        // The adversarial corpus's M01: each iteration leaves the star's "stop here" pending until the match ends.
        var m01 = Run("(?:a|ab)*c", string.Concat(Enumerable.Repeat("ab", 10_000)) + "c", frames: 100);
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Frames), m01.Head());
        Assert.Equal("match 0,20001", Run("(?:a|ab)*c", string.Concat(Enumerable.Repeat("ab", 10_000)) + "c").Answer);
    }

    [Fact]
    public void The_trail_ceiling_is_reached_one_cell_past_what_the_match_holds_ready_to_undo()
    {
        // (a)* over "aa": each iteration writes its mark, clears group 1 (from the second on) and writes group 1's start
        // before its character: the first iteration three cells with its end, the second five, and the third four before
        // its character fails at the end of the subject. Nothing is undone before that failure, so the call holds twelve.
        Assert.Equal("match 0,2 1,2", Run("(a)*", "aa", trail: 12).Answer);
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Trail), Run("(a)*", "aa", trail: 11).Head());

        // The adversarial corpus's M02: each iteration writes capture 1 while its "stop here" alternative is pending.
        var m02 = Run("(?:(a)|b)*c", new string('a', 10_000) + "c", trail: 100);
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Trail), m02.Head());
        Assert.Equal("match 0,10001 9999,10000", Run("(?:(a)|b)*c", new string('a', 10_000) + "c").Answer);
    }

    [Fact]
    public void The_step_ceiling_is_reached_at_the_unit_past_it()
    {
        // a on "a": Char and Accept, two units.
        Assert.Equal("match 0,1", Run("a", "a", steps: 2).Answer);
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Steps), Run("a", "a", steps: 1).Head());
    }

    /// <summary>The catastrophic rows of the adversarial corpus within task 2.4a's subset (K, A and W, without anchors).</summary>
    public static TheoryData<string, string> CatastrophicRows => new()
    {
        { "(a+)+b", "b" + new string('a', 30) },
        { "(a*)*b", new string('a', 30) + "c" },
        { "(a|aa)+b", "b" + new string('a', 40) },
        { "(?:a+)+b", "b" + new string('a', 30) },
        { string.Concat(Enumerable.Repeat("(?:a|aa)", 30)) + "b", "b" + new string('a', 60) },
        { "(?:(a+)+b|a*c)", new string('a', 30) + "c" },
    };

    [Theory]
    [MemberData(nameof(CatastrophicRows))]
    public void A_catastrophic_case_with_a_step_ceiling_answers_LimitExceeded_naming_steps_and_never_a_match_or_no_match(string pattern, string subject)
    {
        // The corpus's own policy: a step ceiling of 1,000,000 and ordinary frame and trail ceilings.
        var run = Run(pattern, subject, steps: 1_000_000, frames: 4_194_304, trail: 8_388_608);
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Steps), run.Head());
    }

    [Fact]
    public void A_ceiling_the_call_never_reaches_costs_nothing()
    {
        Assert.Equal("match 0,1", Run("a", "a", steps: 2, frames: 1, trail: 1).Answer);
        Assert.Equal("match 0,3", Run("abc", "abc", frames: 1, trail: 1).Answer);
    }

    [Fact]
    public void A_ceiling_below_one_or_a_frame_or_trail_ceiling_above_its_maximum_is_a_broken_precondition()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegexMatchCeilings(0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegexMatchCeilings(null, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegexMatchCeilings(null, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegexMatchCeilings(null, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegexMatchCeilings(null, RegexMatchCeilings.MaximumFrames + 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegexMatchCeilings(null, 1, RegexMatchCeilings.MaximumTrail + 1));

        var largest = new RegexMatchCeilings(ulong.MaxValue, RegexMatchCeilings.MaximumFrames, RegexMatchCeilings.MaximumTrail);
        Assert.Equal((ulong.MaxValue, 1 << 24, 1 << 25), (largest.Steps!.Value, largest.Frames, largest.Trail));
        Assert.False(default(RegexMatchCeilings).IsSet);
    }

    [Fact]
    public void The_engines_defaults_are_decision_D10_s()
    {
        // Broiler.VM's MaximumFrames and MaximumTrail, which both engines pass, with no step ceiling.
        var engines = RegexMatchCeilings.Engines;
        Assert.Equal(((ulong?)null, 1 << 20, 1 << 21), (engines.Steps, engines.Frames, engines.Trail));
        Assert.True(engines.Frames < 4_194_304 && engines.Trail < 8_388_608);
    }
}

/// <summary>Takes the first two of a run's three answers, so a test compares the status and the limit together.</summary>
internal static class RunTuple
{
    internal static (RegexStatus, RegexLimit) Head(this (RegexStatus Status, RegexLimit Limit, string Answer) run) => (run.Status, run.Limit);
}
