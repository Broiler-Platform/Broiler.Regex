namespace Broiler.Regex.Tests;

/// <summary>
/// Quantifier repetition is iterative for every body shape: a single-code-point body (`a*`,
/// `.*`, `[ab]+`) through the linear fast path, and any other body — a capturing group, an
/// alternation of sequences — through the explicit-stack RepeatMatcher. So a repeat over a
/// subject of any length matches natively, without recursing once per iteration. The
/// remaining native recursion is bounded by the pattern's nesting depth (like the parser
/// that accepted the pattern), never by the subject length, so the `RegexOverflowException`
/// backstop is no longer reachable by input size; it stays only to degrade a pathologically
/// nested pattern to a signal rather than a crash. A short subject always matches.
/// </summary>
public class StackDepthGuardTests
{
    [Fact(Timeout = 600000)]
    public void GreedyStarOverALongSubject_MatchesViaTheFastPath()
    {
        var re = new BroilerRegex("a*");
        var subject = new string('a', 500_000);
        Assert.Equal(subject, re.Match(subject).Value);
    }

    [Fact(Timeout = 600000)]
    public void PlusClassOverALongSubject_MatchesViaTheFastPath()
    {
        var re = new BroilerRegex("[ab]+", "u");
        var subject = new string('b', 500_000);
        Assert.Equal(subject, re.Match(subject).Value);
    }

    [Fact(Timeout = 600000)]
    public void CapturingGroupBodyOverALongSubject_MatchesViaTheIterativeDriver()
    {
        // A capturing-group body is not eligible for the fast path, so it exercises the
        // explicit-stack RepeatMatcher. It used to overflow; now it matches natively.
        var re = new BroilerRegex("(a)*");
        var subject = new string('a', 500_000);
        var m = re.Match(subject);
        Assert.Equal(subject, m.Value);
        Assert.Equal("a", m.Groups[1].Value); // last iteration's capture
    }

    [Fact(Timeout = 600000)]
    public void AlternationBodyOverALongSubject_MatchesViaTheIterativeDriver()
    {
        // A (non-capturing) alternation of sequences is also a general body.
        var re = new BroilerRegex("(?:ab|a)+");
        var subject = string.Concat(System.Linq.Enumerable.Repeat("ab", 250_000));
        Assert.Equal(subject, re.Match(subject).Value);
    }

    [Fact(Timeout = 600000)]
    public void LazyCapturingGroupOverALongSubject_MatchesToTheAnchor()
    {
        // Lazy repetition of a general body over a long subject, forced to consume it all.
        var re = new BroilerRegex("(a)*?b");
        var subject = new string('a', 300_000) + "b";
        Assert.Equal(subject, re.Match(subject).Value);
    }

    [Fact(Timeout = 600000)]
    public void ShortSubjects_StillMatch()
    {
        var re = new BroilerRegex("a*b");
        Assert.Equal("aaab", re.Match("aaab").Value);
        Assert.Equal("b", re.Match("b").Value);

        var captured = new BroilerRegex("(ab)*");
        var abs = string.Concat(System.Linq.Enumerable.Repeat("ab", 2_000));
        Assert.Equal(abs, captured.Match(abs).Value);
    }
}
