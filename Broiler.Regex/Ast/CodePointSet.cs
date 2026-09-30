// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   17
// Annotated:        17/17
// Exempt:           4
// Human-reviewed:   0/17
// IP risk:          Low
// Security risk:    Medium
// Criteria:         12/0
// Resource impact:  6/10 max
// Unverified:       17
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.Regex.Ast;

/// <summary>
/// A set of Unicode code points held as sorted, non-overlapping, non-adjacent
/// ranges. This is the value type of every character class: the built-in escapes
/// (<c>\d \w \s</c>), Unicode property escapes, and the <c>v</c>-mode class-set
/// expressions all reduce to one of these, so union, intersection, difference and
/// complement (ECMA-262 §22.2.1 <c>ClassSetExpression</c>) are plain set algebra
/// rather than a special case per operand kind.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=6; Fingerprint=5EBADC
// Broiler-Falsified-If: after AddRange calls of (5, 9), (0, 4) and (7, 12), Ranges returns entries that overlap, touch or are out of order instead of the single range 0..12
// Broiler-Human:        PENDING
public sealed class CodePointSet
{
    /// <summary>The highest Unicode code point; the universe complements are taken against.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=F4973F
    // Broiler-Human:        PENDING
    public const int MaxCodePoint = 0x10FFFF;

    private readonly List<(int Lo, int Hi)> _ranges;
    private bool _normalized;

    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=1; Fingerprint=EC905E
    // Broiler-Human:        PENDING
    public CodePointSet() => _ranges = [];

    private CodePointSet(List<(int Lo, int Hi)> ranges, bool normalized)
    {
        _ranges = ranges;
        _normalized = normalized;
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=1; Fingerprint=C7C053
    // Broiler-Human:        PENDING
    public static CodePointSet Empty() => new();

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=66D49E
    // Broiler-Human:        PENDING
    public static CodePointSet Of(int lo, int hi)
    {
        var set = new CodePointSet();
        set.AddRange(lo, hi);
        return set;
    }

    /// <summary>The normalized ranges. Enumerating normalizes the set in place.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=0AF3ED
    // Broiler-Falsified-If: a thread enumerating Ranges of a shared set while another thread makes that set's first Contains call throws InvalidOperationException, because Ranges hands out the list the other thread is still compacting
    // Broiler-Human:        PENDING
    public IReadOnlyList<(int Lo, int Hi)> Ranges
    {
        get
        {
            Normalize();
            return _ranges;
        }
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=4; Fingerprint=23B869
    // Broiler-Human:        PENDING
    public bool IsEmpty
    {
        get
        {
            Normalize();
            return _ranges.Count == 0;
        }
    }

    public void AddCodePoint(int codePoint) => AddRange(codePoint, codePoint);

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=2AC4F4
    // Broiler-Falsified-If: AddRange accepts a bound outside 0..0x10FFFF such as int.MaxValue, after which adding (5, 6) leaves Ranges holding two overlapping entries
    // Broiler-Human:        PENDING
    public void AddRange(int lo, int hi)
    {
        if (lo > hi)
            throw new ArgumentException($"Range out of order: U+{lo:X}-U+{hi:X}");
        _ranges.Add((lo, hi));
        _normalized = false;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=38B83D
    // Broiler-Falsified-If: calling AddAll with the set itself as the argument throws InvalidOperationException, because it enumerates the list it is appending to
    // Broiler-Human:        PENDING
    public void AddAll(CodePointSet other)
    {
        foreach (var (lo, hi) in other.Ranges)
            AddRange(lo, hi);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=BEA7CA
    // Broiler-Falsified-If: AddRange on a clone changes the ranges of the set it was cloned from
    // Broiler-Human:        PENDING
    public CodePointSet Clone()
    {
        Normalize();
        return new CodePointSet([.. _ranges], normalized: true);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=50549A
    // Broiler-Falsified-If: a code point equal to a range's Lo or Hi, such as 0x10FFFF tested against Of(0, 0x10FFFF), is reported absent
    // Broiler-Human:        PENDING
    public bool Contains(int codePoint)
    {
        Normalize();
        var lo = 0;
        var hi = _ranges.Count - 1;
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
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=3B25DD
    // Broiler-Falsified-If: complementing a set such as {0x41..0x5A, 0x10FFFF} twice does not give back exactly its original ranges
    // Broiler-Human:        PENDING
    public CodePointSet Complement()
    {
        Normalize();
        var result = new CodePointSet();
        var next = 0;
        foreach (var (lo, hi) in _ranges)
        {
            if (lo > next)
                result.AddRange(next, lo - 1);
            next = hi + 1;
            if (next > MaxCodePoint)
                break;
        }
        if (next <= MaxCodePoint)
            result.AddRange(next, MaxCodePoint);
        result._normalized = true;
        return result;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=A72232
    // Broiler-Falsified-If: after Union the receiver itself holds the other set's ranges, instead of the result being a new set and the receiver unchanged
    // Broiler-Human:        PENDING
    public CodePointSet Union(CodePointSet other)
    {
        var result = Clone();
        result.AddAll(other);
        result.Normalize();
        return result;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=7CB34B
    // Broiler-Falsified-If: intersecting {0..10, 20..30} with {5..25} gives anything other than the two ranges 5..10 and 20..25
    // Broiler-Human:        PENDING
    public CodePointSet Intersect(CodePointSet other)
    {
        Normalize();
        other.Normalize();

        var result = new CodePointSet();
        int i = 0, j = 0;
        while (i < _ranges.Count && j < other._ranges.Count)
        {
            var a = _ranges[i];
            var b = other._ranges[j];
            var lo = Math.Max(a.Lo, b.Lo);
            var hi = Math.Min(a.Hi, b.Hi);
            if (lo <= hi)
                result._ranges.Add((lo, hi));

            if (a.Hi < b.Hi)
                i++;
            else
                j++;
        }
        result._normalized = true;
        return result;
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=4; Fingerprint=23BD23
    // Broiler-Falsified-If: subtracting {0x4D} from {0x41..0x5A} gives anything other than the two ranges 0x41..0x4C and 0x4E..0x5A
    // Broiler-Human:        PENDING
    public CodePointSet Subtract(CodePointSet other) => Intersect(other.Complement());

    /// <summary>
    /// Maps every member through <paramref name="fold"/>, used for the <c>v</c>-mode
    /// <c>MaybeSimpleCaseFolding</c> of a class-set operand (ECMA-262 §22.2.1). Folding
    /// a set before complementing it is what makes <c>[^\P{Lowercase_Letter}]</c> under
    /// <c>vi</c> mean something other than <c>\p{Lowercase_Letter}</c>.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=6; Fingerprint=5A1017
    // Broiler-Falsified-If: mapping Of(0x41, 0x5A) through simple case folding returns anything other than the single range 0x61..0x7A
    // Broiler-Human:        PENDING
    public CodePointSet Map(Func<int, int> fold)
    {
        Normalize();
        var result = new CodePointSet();
        foreach (var (lo, hi) in _ranges)
        {
            for (var cp = lo; cp <= hi; cp++)
                result.AddCodePoint(fold(cp));
        }
        result.Normalize();
        return result;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=7734CD
    // Broiler-Falsified-If: two threads making the first read of one shared un-normalized set at the same time, such as the static folded word set that \b consults under the i and u flags, both sort and compact its list, and one throws from RemoveRange or loses a range
    // Broiler-Human:        PENDING
    private void Normalize()
    {
        if (_normalized)
            return;

        _ranges.Sort(static (a, b) => a.Lo != b.Lo ? a.Lo.CompareTo(b.Lo) : a.Hi.CompareTo(b.Hi));

        var write = 0;
        for (var read = 0; read < _ranges.Count; read++)
        {
            var current = _ranges[read];
            if (write > 0)
            {
                var previous = _ranges[write - 1];
                // Merge overlapping AND adjacent ranges, so a set built from single code
                // points compares equal to the same set built from one range.
                if (current.Lo <= previous.Hi + 1)
                {
                    if (current.Hi > previous.Hi)
                        _ranges[write - 1] = (previous.Lo, current.Hi);
                    continue;
                }
            }
            _ranges[write++] = current;
        }
        _ranges.RemoveRange(write, _ranges.Count - write);
        _normalized = true;
    }
}
