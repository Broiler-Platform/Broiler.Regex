using System.Collections.Generic;

namespace Broiler.Regex.Fuzz;

/// <summary>
/// The session's only source of choice: SplitMix64 (Steele, Lea and Flood, "Fast splittable pseudorandom number
/// generators", 2014), seeded per iteration from the session's seed and the iteration's index.
/// </summary>
/// <remarks>
/// <b>An iteration is a function of its seed and its index alone.</b> Each iteration draws from a generator of its own,
/// so iteration <c>i</c> of seed <c>s</c> is the same case however the session's range is split among workers and
/// whatever ran before it, and <c>--replay</c> regenerates it without running the iterations before it.
/// </remarks>
internal sealed class FuzzRandom
{
    private const ulong Gamma = 0x9E3779B97F4A7C15UL;

    private ulong _state;

    private FuzzRandom(ulong state) => _state = state;

    /// <summary>The generator of iteration <paramref name="iteration"/> of the session seeded <paramref name="seed"/>.</summary>
    internal static FuzzRandom ForIteration(ulong seed, long iteration) =>
        new(Finish(seed ^ Finish(((ulong)iteration * Gamma) + 0x6A09E667F3BCC909UL)));

    /// <summary>The next 64 bits.</summary>
    internal ulong NextBits()
    {
        _state += Gamma;
        return Finish(_state);
    }

    /// <summary>A number from 0 to <paramref name="count"/> − 1; <paramref name="count"/> is at least 1.</summary>
    internal int Below(int count) => (int)(NextBits() % (ulong)count);

    /// <summary>A number from <paramref name="low"/> to <paramref name="high"/>, both included.</summary>
    internal int Between(int low, int high) => low + Below(high - low + 1);

    /// <summary>A number from <paramref name="low"/> to <paramref name="high"/>, both included, as an unsigned long.</summary>
    internal ulong Between(ulong low, ulong high) => high - low == ulong.MaxValue ? NextBits() : low + (NextBits() % (high - low + 1));

    /// <summary>True with a chance of <paramref name="percent"/> in a hundred.</summary>
    internal bool Chance(int percent) => Below(100) < percent;

    /// <summary>One of <paramref name="items"/>.</summary>
    internal T Pick<T>(IReadOnlyList<T> items) => items[Below(items.Count)];

    /// <summary>
    /// A small number more often than a large one: 0 to <paramref name="high"/>, each halving of the range half as likely,
    /// so that most quantifier bounds and repetitions are small and a few are large.
    /// </summary>
    internal int Skewed(int high)
    {
        var width = Below(31);
        var limit = width >= 30 ? high : System.Math.Min(high, (1 << width) - 1);
        return Below(limit + 1);
    }

    private static ulong Finish(ulong z)
    {
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
