// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   21
// Annotated:        21/21
// Exempt:           4
// Human-reviewed:   0/21
// IP risk:          Low
// Security risk:    Medium
// Criteria:         9/0
// Resource impact:  6/10 max
// Unverified:       21
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Broiler.Regex.Ast;

/// <summary>
/// A set of Unicode code points held as sorted, non-overlapping, non-adjacent
/// ranges. This is the value type of every character class: the built-in escapes
/// (<c>\d \w \s</c>), Unicode property escapes, and the <c>v</c>-mode class-set
/// expressions all reduce to one of these, so union, intersection, difference and
/// complement (ECMA-262 §22.2.1 <c>ClassSetExpression</c>) are plain set algebra
/// rather than a special case per operand kind.
/// </summary>
/// <remarks>
/// <b>Immutable</b> (the programme's conventions 7 and 10, decision D31 of Broiler.VM's JSD-0081): a set's ranges are
/// sorted and merged before its constructor returns, and nothing changes them after it. Every operation answers a
/// new set, so one set may be read by a compiled pattern's concurrent matches and held in a static field. The
/// library builds a set from ranges in any order through the internal <see cref="Builder"/>, which normalizes once,
/// when the set is made.
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=6; Fingerprint=5EBADC
// Broiler-Falsified-If: a set built from the ranges (5, 9), (0, 4) and (7, 12), in that order, reports Ranges other than the single range 0..12, or a read of a set writes a field of it
// Broiler-Human:        PENDING
public sealed class CodePointSet
{
    /// <summary>The highest Unicode code point; the universe complements are taken against.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=F4973F
    // Broiler-Human:        PENDING
    public const int MaxCodePoint = 0x10FFFF;

    /// <summary>The ranges, sorted by their low ends, disjoint and never adjacent.</summary>
    private readonly ImmutableArray<(int Lo, int Hi)> _ranges;

    /// <summary>The empty set.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=1; Fingerprint=EC905E
    // Broiler-Human:        PENDING
    public CodePointSet() => _ranges = [];

    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=1A62E0
    // Broiler-Human:        PENDING
    private CodePointSet(ImmutableArray<(int Lo, int Hi)> normalized) => _ranges = normalized;

    /// <summary>The empty set.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=1; Fingerprint=C7C053
    // Broiler-Human:        PENDING
    public static CodePointSet Empty() => new();

    /// <summary>The code points <paramref name="lo"/> to <paramref name="hi"/>, both included.</summary>
    /// <exception cref="ArgumentException"><paramref name="lo"/> is greater than <paramref name="hi"/>.</exception>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=106FB1
    // Broiler-Human:        PENDING
    public static CodePointSet Of(int lo, int hi)
    {
        if (lo > hi)
            throw new ArgumentException("A range is out of order.", nameof(hi));
        return new CodePointSet([(lo, hi)]);
    }

    /// <summary>
    /// A set of the given inclusive ranges, in any order, normalized before it is returned: the conversion of a
    /// set the Unicode owner's provider answers, whose ranges arrive one by one, into this library's own type.
    /// </summary>
    /// <remarks>
    /// The list is copied, not changed, and the set is normalized here, never on a read, so a set built from the
    /// provider can be shared by a compiled pattern's concurrent matches.
    /// </remarks>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=2; Fingerprint=4D779A
    // Broiler-Human:        PENDING
    internal static CodePointSet FromRanges(List<(int Lo, int Hi)> ranges)
    {
        foreach (var (lo, hi) in ranges)
        {
            if (lo > hi)
                throw new ArgumentException("A range is out of order.", nameof(ranges));
        }

        var copy = ranges.ToArray();
        return Normalized(copy);
    }

    /// <summary>
    /// The set of <paramref name="ranges"/>, which this call owns and reorders: sorted, then each range merged
    /// into the one before it when the two overlap or touch, so a set built from single code points equals the
    /// same set built from one range.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=A2E2E2
    // Broiler-Falsified-If: ranges that overlap or touch, such as (0, 4) and (5, 9), or a range ending at int.MaxValue followed by another, are left as two entries instead of being merged
    // Broiler-Human:        PENDING
    private static CodePointSet Normalized((int Lo, int Hi)[] ranges)
    {
        if (ranges.Length == 0)
            return new CodePointSet();

        Array.Sort(ranges, static (a, b) => a.Lo != b.Lo ? a.Lo.CompareTo(b.Lo) : a.Hi.CompareTo(b.Hi));

        var write = 0;
        for (var read = 0; read < ranges.Length; read++)
        {
            var current = ranges[read];
            if (write > 0)
            {
                var previous = ranges[write - 1];
                if ((long)current.Lo <= (long)previous.Hi + 1)
                {
                    if (current.Hi > previous.Hi)
                        ranges[write - 1] = (previous.Lo, current.Hi);
                    continue;
                }
            }
            ranges[write++] = current;
        }

        return new CodePointSet(ImmutableArray.Create(ranges, 0, write));
    }

    /// <summary>The ranges, sorted, disjoint and never adjacent. The list cannot be changed.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=2982CA
    // Broiler-Human:        PENDING
    public IReadOnlyList<(int Lo, int Hi)> Ranges => _ranges;

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=96A816
    // Broiler-Human:        PENDING
    public bool IsEmpty => _ranges.IsEmpty;

    /// <summary>How many ranges the set holds: the units an operation of a compilation charges for reading it.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=995345
    // Broiler-Human:        PENDING
    internal int RangeCount => _ranges.Length;

    /// <summary>This set itself: a set never changes, so it is its own copy.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=ADF644
    // Broiler-Human:        PENDING
    public CodePointSet Clone() => this;

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=3F5B91
    // Broiler-Falsified-If: a code point equal to a range's Lo or Hi, such as 0x10FFFF tested against Of(0, 0x10FFFF), is reported absent
    // Broiler-Human:        PENDING
    public bool Contains(int codePoint)
    {
        var lo = 0;
        var hi = _ranges.Length - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            var range = _ranges[mid];
            if (codePoint < range.Lo)
                hi = mid - 1;
            else if (codePoint > range.Hi)
                lo = mid + 1;
            else
                return true;
        }
        return false;
    }

    /// <summary>Every code point in <c>0..<see cref="MaxCodePoint"/></c> that is not in this set.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=D4EA19
    // Broiler-Falsified-If: complementing a set such as {0x41..0x5A, 0x10FFFF} twice does not give back exactly its original ranges
    // Broiler-Human:        PENDING
    public CodePointSet Complement()
    {
        var result = ImmutableArray.CreateBuilder<(int Lo, int Hi)>(_ranges.Length + 1);
        var next = 0;
        foreach (var (lo, hi) in _ranges)
        {
            if (lo > next)
                result.Add((next, lo - 1));
            if (hi >= MaxCodePoint)
            {
                next = MaxCodePoint + 1;
                break;
            }
            next = Math.Max(next, hi + 1);
        }
        if (next <= MaxCodePoint)
            result.Add((next, MaxCodePoint));
        return new CodePointSet(result.DrainToImmutable());
    }

    /// <summary>
    /// Every code point of this set or <paramref name="other"/>: one pass over the two sorted range lists, so adding
    /// a range to a set of <c>n</c> ranges costs <c>O(n)</c>.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=C29A01
    // Broiler-Falsified-If: Union of {0..4} and {5..9} gives two ranges instead of 0..9, or Union of {0..2, 8..9} and {3..7} gives anything other than 0..9
    // Broiler-Human:        PENDING
    public CodePointSet Union(CodePointSet other)
    {
        if (other._ranges.IsEmpty)
            return this;
        if (_ranges.IsEmpty)
            return other;

        var result = ImmutableArray.CreateBuilder<(int Lo, int Hi)>(_ranges.Length + other._ranges.Length);
        int i = 0, j = 0;
        while (i < _ranges.Length || j < other._ranges.Length)
        {
            // The next range by its low end, from whichever list has it; merged into the last range written when
            // the two overlap or touch, as Normalized merges.
            (int Lo, int Hi) next;
            if (j >= other._ranges.Length || (i < _ranges.Length && _ranges[i].Lo <= other._ranges[j].Lo))
                next = _ranges[i++];
            else
                next = other._ranges[j++];

            if (result.Count > 0 && (long)next.Lo <= (long)result[^1].Hi + 1)
            {
                if (next.Hi > result[^1].Hi)
                    result[^1] = (result[^1].Lo, next.Hi);
            }
            else
            {
                result.Add(next);
            }
        }
        return new CodePointSet(result.DrainToImmutable());
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=AAC14E
    // Broiler-Falsified-If: intersecting {0..10, 20..30} with {5..25} gives anything other than the two ranges 5..10 and 20..25
    // Broiler-Human:        PENDING
    public CodePointSet Intersect(CodePointSet other)
    {
        var result = ImmutableArray.CreateBuilder<(int Lo, int Hi)>();
        int i = 0, j = 0;
        while (i < _ranges.Length && j < other._ranges.Length)
        {
            var a = _ranges[i];
            var b = other._ranges[j];
            var lo = Math.Max(a.Lo, b.Lo);
            var hi = Math.Min(a.Hi, b.Hi);
            if (lo <= hi)
                result.Add((lo, hi));

            if (a.Hi < b.Hi)
                i++;
            else
                j++;
        }
        return new CodePointSet(result.DrainToImmutable());
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=4; Fingerprint=23BD23
    // Broiler-Falsified-If: subtracting {0x4D} from {0x41..0x5A} gives anything other than the two ranges 0x41..0x4C and 0x4E..0x5A
    // Broiler-Human:        PENDING
    public CodePointSet Subtract(CodePointSet other) => Intersect(other.Complement());

    /// <summary>
    /// Maps every member through <paramref name="fold"/>, used for the <c>v</c>-mode
    /// <c>MaybeSimpleCaseFolding</c> of a class-set operand (ECMA-262 §22.2.2.9.5). Folding
    /// a set before complementing it makes <c>[^\P{Lowercase_Letter}]</c> under <c>vi</c> match
    /// what <c>\p{Lowercase_Letter}</c> matches; under <c>ui</c>, whose case closure runs after the
    /// complement, it matches only the lowercase letters with no case partner, such as U+0138.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=6; Fingerprint=00D103
    // Broiler-Falsified-If: mapping Of(0x41, 0x5A) through simple case folding returns anything other than the single range 0x61..0x7A
    // Broiler-Human:        PENDING
    public CodePointSet Map(Func<int, int> fold)
    {
        var result = new Builder();
        foreach (var (lo, hi) in _ranges)
        {
            for (var cp = lo; cp <= hi; cp++)
                result.Add(fold(cp));
        }
        return result.ToSet();
    }

    /// <summary>
    /// Ranges gathered in any order, for one set: a class's members as the parser reads them, or a union of many
    /// operands. Adding is appending; <see cref="ToSet"/> sorts and merges once, so a set of <c>n</c> ranges costs
    /// <c>O(n log n)</c> however its members arrive. A builder is the parser's own, one per set it builds, and is
    /// never shared.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=8D1EAA
    // Broiler-Falsified-If: a set built from members added out of order, such as 9, 0 and then the range 1..8, is anything other than the single range 0..9
    // Broiler-Human:        PENDING
    internal sealed class Builder
    {
        private readonly List<(int Lo, int Hi)> _pending = [];

        /// <summary>Adds one code point.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=129D7E
        // Broiler-Human:        PENDING
        internal void Add(int codePoint) => _pending.Add((codePoint, codePoint));

        /// <summary>Adds the code points <paramref name="lo"/> to <paramref name="hi"/>; the caller has ordered them.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=BD880A
        // Broiler-Human:        PENDING
        internal void AddRange(int lo, int hi) => _pending.Add((lo, hi));

        /// <summary>Adds every code point of <paramref name="set"/>.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=2; Fingerprint=F1C2C3
        // Broiler-Human:        PENDING
        internal void AddAll(CodePointSet set) => _pending.AddRange(set._ranges);

        /// <summary>The set of every code point added so far; the builder can go on adding.</summary>
        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=04EB2F
        // Broiler-Human:        PENDING
        internal CodePointSet ToSet() => FromRanges(_pending);
    }
}
