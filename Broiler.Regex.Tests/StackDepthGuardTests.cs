using System;
using System.Linq;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The string-bearing members over long subjects. Since task 2.4c they run the new backend, which keeps its choices
/// and register writes on the heap, so no subject length reaches the CLR stack (check A-4); what bounds a match is the
/// engines' frame and trail ceilings, decision D10's, which the string-bearing members pass. A quantifier over one
/// character is a run, one frame however long; any other body holds a frame per iteration and writes its group starts
/// and captures to the trail, so a long enough subject reaches a ceiling, and the members answer it as a ceiling,
/// never as no match: <see cref="BroilerRegex.Match(string, int)"/> with <see cref="RegexMatch.Limit"/>,
/// <see cref="BroilerRegex.IsMatch"/> and <see cref="BroilerRegex.Matches"/> with an
/// <see cref="InvalidOperationException"/> (requirement R1).
/// </summary>
public class StackDepthGuardTests
{
    [Fact(Timeout = 600000)]
    public void GreedyStarOverALongSubject_MatchesAsOneRun()
    {
        var re = TestRegex.Compile("a*");
        var subject = new string('a', 5_000_000);
        Assert.Equal(subject.Length, re.Match(subject).Length);
    }

    [Fact(Timeout = 600000)]
    public void PlusClassOverALongSubject_MatchesAsOneRun()
    {
        var re = TestRegex.Compile("[ab]+", "u");
        var subject = new string('b', 500_000);
        Assert.Equal(subject, re.Match(subject).Value);
    }

    [Fact(Timeout = 600000)]
    public void CapturingGroupBodyOverALongSubject_MatchesWithinTheEnginesCeilings()
    {
        // A capturing group's iteration holds a frame and writes five trail cells (its start, its capture reset, the
        // capture's two ends): 400,000 iterations are 2,000,000 cells, under the engines' 2,097,152.
        var re = TestRegex.Compile("(a)*");
        var subject = new string('a', 400_000);
        var m = re.Match(subject);
        Assert.Equal(subject, m.Value);
        Assert.Equal("a", m.Groups[1].Value); // last iteration's capture
    }

    [Fact(Timeout = 600000)]
    public void A_match_past_the_engines_trail_ceiling_answers_the_ceiling_not_no_match()
    {
        // 500,000 iterations write 2,500,000 cells: Match answers a failed match naming the trail ceiling, which no match
        // never does, and IsMatch and Matches, which cannot carry it, throw rather than answer false or end quietly.
        var re = TestRegex.Compile("(a)*");
        var subject = new string('a', 500_000);
        var m = re.Match(subject);
        Assert.False(m.Success);
        Assert.Equal(RegexLimit.Trail, m.Limit);
        Assert.Equal(RegexLimit.None, re.Match("b").Limit);
        Assert.Contains("trail", Assert.Throws<InvalidOperationException>(() => re.IsMatch(subject)).Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => re.Matches(subject).ToList());
    }

    [Fact(Timeout = 600000)]
    public void A_match_past_the_engines_frame_ceiling_answers_the_ceiling_after_the_matches_before_it()
    {
        // An alternation's iteration holds two frames, its loop's and its own: 600,000 iterations pass the engines'
        // 1,048,576 frames. Matches yields the match before the long run, then throws at it.
        var re = TestRegex.Compile("(?:a|b)*c");
        var subject = "abc-" + new string('a', 600_000);
        Assert.Equal(RegexLimit.Frames, re.Match(subject, 4).Limit);
        Assert.Equal("abc", re.Match(subject).Value);
        using var matches = re.Matches(subject).GetEnumerator();
        Assert.True(matches.MoveNext());
        Assert.Equal("abc", matches.Current.Value);
        Assert.Throws<InvalidOperationException>(() => matches.MoveNext());
    }

    [Fact(Timeout = 600000)]
    public void AlternationBodyOverALongSubject_MatchesWithinTheEnginesCeilings()
    {
        // A (non-capturing) alternation of sequences is a general body: 250,000 iterations hold 500,000 frames.
        var re = TestRegex.Compile("(?:ab|a)+");
        var subject = string.Concat(Enumerable.Repeat("ab", 250_000));
        Assert.Equal(subject, re.Match(subject).Value);
    }

    [Fact(Timeout = 600000)]
    public void LazyCapturingGroupOverALongSubject_MatchesToTheAnchor()
    {
        // Lazy repetition of a general body over a long subject, forced to consume it all.
        var re = TestRegex.Compile("(a)*?b");
        var subject = new string('a', 300_000) + "b";
        Assert.Equal(subject, re.Match(subject).Value);
    }

    [Fact(Timeout = 600000)]
    public void ShortSubjects_StillMatch()
    {
        var re = TestRegex.Compile("a*b");
        Assert.Equal("aaab", re.Match("aaab").Value);
        Assert.Equal("b", re.Match("b").Value);

        var captured = TestRegex.Compile("(ab)*");
        var abs = string.Concat(Enumerable.Repeat("ab", 2_000));
        Assert.Equal(abs, captured.Match(abs).Value);
    }
}
