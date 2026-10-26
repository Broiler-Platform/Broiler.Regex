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
        int frames = RegexMatchCeilings.FrameCeilingMaximum, int trail = RegexMatchCeilings.TrailCeilingMaximum, bool fastPaths = true)
    {
        var program = fastPaths ? TestProgram.Compile(pattern) : TestProgram.General(pattern);
        var slots = new int[program.SlotCount];
        var status = program.Match(subject, 0, false, slots, null, new RegexMatchCeilings(steps, frames, trail), out var limit, out _);
        return (status, limit, MachineTests.Render(status, slots));
    }

    [Fact]
    public void The_frame_ceiling_is_reached_one_frame_past_what_the_match_holds_at_once()
    {
        // a* over "aaaa" as a loop (the general lowering) holds five frames at its deepest: one per iteration, and the
        // fifth iteration's, whose body fails.
        Assert.Equal("match 0,4", Run("a*", "aaaa", frames: 5, fastPaths: false).Answer);
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Frames), Run("a*", "aaaa", frames: 4, fastPaths: false).Head());
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Frames), Run("a*", "aaaa", frames: 1, fastPaths: false).Head());

        // As a run it holds one frame, whatever its length. In a loop each iteration holds the loop's choice and, when it
        // took a character, its run's frame: (?:a*b)* over "aabaabb" holds four choices and two runs' frames at the end.
        Assert.Equal("match 0,4", Run("a*", "aaaa", frames: 1).Answer);
        Assert.Equal("match 0,7", Run("(?:a*b)*", "aabaabb", frames: 6).Answer);
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Frames), Run("(?:a*b)*", "aabaabb", frames: 5).Head());

        // The adversarial corpus's M01: each iteration leaves the star's "stop here" pending until the match ends.
        var m01 = Run("(?:a|ab)*c", string.Concat(Enumerable.Repeat("ab", 10_000)) + "c", frames: 100);
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Frames), m01.Head());
        Assert.Equal("match 0,20001", Run("(?:a|ab)*c", string.Concat(Enumerable.Repeat("ab", 10_000)) + "c").Answer);
    }

    [Fact]
    public void The_trail_ceiling_is_reached_one_cell_past_the_writes_the_match_has_not_undone()
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
    public void A_counted_loop_that_holds_no_choice_still_fills_the_trail_two_cells_an_iteration()
    {
        // The trail keeps every write that changes a register until the attempt ends, whether or not a choice is open: a
        // loop of a{n} (the general lowering) pushes no frame, but each iteration writes its mark and its count, and the
        // count's start one more. Under the engines' ceilings of decision D10 that bounds a counted loop to 1,048,575
        // iterations; the old backend, the public members' until task 2.4c, matched past it.
        const int most = (RegexMatchCeilings.EngineTrail - 1) / 2;
        Assert.Equal(1_048_575, most);
        var subject = new string('a', most + 1);
        var program = TestProgram.General("(?:a){" + most + "}");
        var slots = new int[program.SlotCount];
        Assert.Equal(RegexStatus.Match, program.Match(subject, 0, true, slots, null, RegexMatchCeilings.Engines, out _, out _));
        Assert.Equal([0, most], slots);

        var past = TestProgram.General("a{" + (most + 1) + "}");
        Assert.Equal(RegexStatus.LimitExceeded, past.Match(subject, 0, true, new int[past.SlotCount], null, RegexMatchCeilings.Engines, out var limit, out _));
        Assert.Equal(RegexLimit.Trail, limit);
        Assert.True(TestRegex.Compile("a{" + (most + 1) + "}").Match(subject).Success);

        // A body of more than one character is still a loop with the fast paths, and keeps the bound.
        var pair = TestProgram.Compile("(?:ab){" + (most + 1) + "}");
        Assert.Equal(RegexStatus.LimitExceeded, pair.Match(string.Concat(Enumerable.Repeat("ab", most + 1)), 0, true, new int[pair.SlotCount], null, RegexMatchCeilings.Engines, out limit, out _));
        Assert.Equal(RegexLimit.Trail, limit);

        // The string-bearing members, which pass the engines' ceilings since task 2.4c, answer the same as the ceiling:
        // (?:ab){1048576} over as many repetitions, and (?:){2147483647} on the empty subject, where the specification
        // answers an empty match and the old backend answered no match.
        Assert.Equal(RegexLimit.Trail, TestRegex.Compile("(?:ab){" + (most + 1) + "}").Match(string.Concat(Enumerable.Repeat("ab", most + 1))).Limit);
        Assert.Equal(RegexLimit.Trail, TestRegex.Compile("(?:){" + int.MaxValue + "}").Match("").Limit);
    }

    [Fact]
    public void A_run_holds_one_frame_and_writes_no_trail_cell_whatever_its_length()
    {
        // Task 2.4c's run of one character (the adversarial corpus's presumption 3): a{n}, [a]*, [a]*? and a run read
        // right to left, past the loop's bound, match under the engines' ceilings, and under a trail ceiling of one and a
        // frame ceiling of one, two for the run and the look-behind's barrier both held.
        const int length = (RegexMatchCeilings.EngineTrail / 2) + 1;
        var subject = new string('a', length);
        foreach (var (pattern, frames) in (ValueTuple<string, int>[])[("a{" + length + "}", 1), ("[a]*", 1), ("[a]*?$", 1), ("a+(?<=a{" + length + "})", 2)])
        {
            var program = TestProgram.Compile(pattern);
            var slots = new int[program.SlotCount];
            Assert.Equal(RegexStatus.Match, program.Match(subject, 0, true, slots, null, RegexMatchCeilings.Engines, out _, out _));
            Assert.Equal([0, length], slots);
            Assert.Equal(RegexStatus.Match, program.Match(subject, 0, true, slots, null, new RegexMatchCeilings(null, frames, 1), out _, out _));
        }
    }

    [Fact]
    public void The_step_ceiling_is_reached_at_the_unit_past_it()
    {
        // a on "a": Literal and Succeed, two units.
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

    /// <summary>
    /// Task 2.4b's constructs on the ceilings' paths: look-arounds of the four kinds whose bodies backtrack, a negative
    /// one among them whose body would reach a ceiling, and back-references forward, under i and in a look-behind.
    /// </summary>
    public static TheoryData<string, string, string> ThroughLookaroundsAndReferences => new()
    {
        { "^(?!(?:(a+)+b|a*c))", "u", new string('a', 7) + "c" },
        { "^(?=(?:(a+)+b|a*c))", "", new string('a', 7) + "c" },
        { "$(?<=(?:b(a+)+|ca*))", "", "c" + new string('a', 7) },
        { "$(?<!(?:b(a+)+|ca*))", "u", "c" + new string('a', 7) },
        { "(?!(?:a|ab)*d)a", "", string.Concat(Enumerable.Repeat("ab", 12)) + "c" },
        { "(?=(?:(a)|b)*c)", "", string.Concat(Enumerable.Repeat("ab", 12)) + "c" },
        { "(?:(a)|b)*(?!\\1)c", "", string.Concat(Enumerable.Repeat("ab", 10)) + "c" },
        { "(a+)+\\1b", "", new string('a', 7) },
        { "(?<=\\1(a+))b", "i", new string('a', 9) + "b" },
        { "(\\w+)\\s\\1\\b", "i", "Word wORD wordy word WORD" },
    };

    [Theory]
    [MemberData(nameof(ThroughLookaroundsAndReferences))]
    public void A_ceiling_reached_in_a_look_around_or_a_back_reference_names_itself_and_is_never_taken_for_the_body_s_failure(
        string pattern, string flags, string subject)
    {
        // Every step ceiling below the call's cost, every frame ceiling below what it holds at once and every trail
        // ceiling below the writes it keeps answers LimitExceeded naming that ceiling; at and above, the full answer. A
        // negative look-around whose body stopped at a ceiling would otherwise read the stop as the body's failure, and
        // hold.
        // The general lowering reaches each ceiling through the look-around or the reference; the fast paths' program, whose
        // runs hold fewer frames and cells, answers each ceiling it reaches the same way (the theory after this one).
        var program = TestProgram.General(pattern, flags);
        var full = Run(program, subject, null, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum);
        Assert.NotEqual(RegexStatus.LimitExceeded, full.Status);
        for (ulong k = 1; k <= full.Units; k++)
        {
            var run = Run(program, subject, k, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum);
            if (k < full.Units)
                Assert.True((run.Status, run.Limit) == (RegexStatus.LimitExceeded, RegexLimit.Steps) && run.Units <= k, "steps " + k + ": " + run.Answer + " " + run.Limit);
            else
                Assert.Equal((full.Answer, full.Units), (run.Answer, run.Units));
        }

        Assert.True(Needed(program, subject, RegexLimit.Frames, full) > 1, "no frame ceiling was reached");
        Assert.True(Needed(program, subject, RegexLimit.Trail, full) > 1, "no trail ceiling was reached");
    }

    [Theory]
    [MemberData(nameof(ThroughLookaroundsAndReferences))]
    public void With_the_fast_paths_every_ceiling_below_what_a_call_needs_names_itself(string pattern, string flags, string subject)
    {
        // The same patterns with their runs, atom runs and classes of strings: every step ceiling below the call's cost
        // answers steps, and every frame and trail ceiling below what it holds answers that ceiling, a run's or a class
        // of strings' marked frame among those held; at and above, the general lowering's answer.
        var program = TestProgram.Compile(pattern, flags);
        var full = Run(program, subject, null, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum);
        Assert.Equal(Run(TestProgram.General(pattern, flags), subject, null, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum).Answer, full.Answer);
        for (ulong k = 1; k <= full.Units; k++)
        {
            var run = Run(program, subject, k, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum);
            if (k < full.Units)
                Assert.True((run.Status, run.Limit) == (RegexStatus.LimitExceeded, RegexLimit.Steps) && run.Units <= k, "steps " + k + ": " + run.Answer + " " + run.Limit);
            else
                Assert.Equal((full.Answer, full.Units), (run.Answer, run.Units));
        }

        Needed(program, subject, RegexLimit.Frames, full);
        Needed(program, subject, RegexLimit.Trail, full);
    }

    /// <summary>
    /// The least frame or trail ceiling under which <paramref name="program"/> gives its full answer, every lower one
    /// having answered LimitExceeded naming <paramref name="ceiling"/>.
    /// </summary>
    private static int Needed(RegexProgram program, string subject, RegexLimit ceiling, (RegexStatus Status, RegexLimit Limit, ulong Units, string Answer) full)
    {
        for (var c = 1; ; c++)
        {
            var run = ceiling == RegexLimit.Frames
                ? Run(program, subject, null, c, RegexMatchCeilings.TrailCeilingMaximum)
                : Run(program, subject, null, RegexMatchCeilings.FrameCeilingMaximum, c);
            if (run.Status != RegexStatus.LimitExceeded)
            {
                Assert.Equal((full.Answer, full.Units), (run.Answer, run.Units));
                return c;
            }

            Assert.True(run.Limit == ceiling, ceiling + " " + c + ": " + run.Limit);
        }
    }

    private static (RegexStatus Status, RegexLimit Limit, ulong Units, string Answer) Run(RegexProgram program, string subject, ulong? steps, int frames, int trail)
    {
        var slots = new int[program.SlotCount];
        var status = program.Match(subject, 0, false, slots, null, new RegexMatchCeilings(steps, frames, trail), out var limit, out var units);
        return (status, limit, units, MachineTests.Render(status, slots));
    }

    [Fact]
    public void A_negative_look_around_whose_body_reaches_a_ceiling_answers_LimitExceeded_not_a_match()
    {
        // The body of (?!…) backtracks catastrophically before it fails on its own; stopped at a ceiling first, the
        // call answers the ceiling, where reading the stop as the body's failure would let the look-around hold.
        var program = TestProgram.Compile("^(?!(?:(a+)+b|a*c))");
        var subject = new string('a', 7) + "c";
        Assert.Equal("nomatch", Run(program, subject, null, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum).Answer);
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Steps), Head(Run(program, subject, 100, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum)));
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Frames), Head(Run(program, subject, null, 3, RegexMatchCeilings.TrailCeilingMaximum)));
        Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Trail), Head(Run(program, subject, null, RegexMatchCeilings.FrameCeilingMaximum, 5)));
    }

    [Fact]
    public void A_back_reference_whose_length_passes_the_step_ceiling_stops_before_it_compares()
    {
        // (.{n})\1 over 2n characters: the reference costs one and its length, n; a ceiling anywhere in it answers
        // steps, its units never above the ceiling, as the length is refused before a character is compared.
        const int n = 50_000;
        var program = TestProgram.Compile("(.{" + n + "})\\1", "s");
        var subject = new string('a', 2 * n);
        var full = Run(program, subject, null, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum);
        Assert.Equal(RegexStatus.Match, full.Status);
        foreach (var k in new[] { full.Units - 1, full.Units - 2, full.Units - (n / 2), full.Units - n, full.Units - n - 1 })
        {
            var run = Run(program, subject, k, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum);
            Assert.True((run.Status, run.Limit) == (RegexStatus.LimitExceeded, RegexLimit.Steps) && run.Units <= k, "steps " + k + ": " + run.Answer + " " + run.Units);
        }
    }

    private static (RegexStatus, RegexLimit) Head((RegexStatus Status, RegexLimit Limit, ulong Units, string Answer) run) => (run.Status, run.Limit);

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
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegexMatchCeilings(null, RegexMatchCeilings.FrameCeilingMaximum + 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegexMatchCeilings(null, 1, RegexMatchCeilings.TrailCeilingMaximum + 1));

        var largest = new RegexMatchCeilings(ulong.MaxValue, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum);
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
