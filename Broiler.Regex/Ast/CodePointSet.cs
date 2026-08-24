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
public sealed class CodePointSet
{
    /// <summary>The highest Unicode code point; the universe complements are taken against.</summary>
    public const int MaxCodePoint = 0x10FFFF;

    private readonly List<(int Lo, int Hi)> _ranges;
    private bool _normalized;

    public CodePointSet() => _ranges = [];

    private CodePointSet(List<(int Lo, int Hi)> ranges, bool normalized)
    {
        _ranges = ranges;
        _normalized = normalized;
    }

    public static CodePointSet Empty() => new();

    public static CodePointSet Of(int lo, int hi)
    {
        var set = new CodePointSet();
        set.AddRange(lo, hi);
        return set;
    }

    /// <summary>The normalized ranges. Enumerating normalizes the set in place.</summary>
    public IReadOnlyList<(int Lo, int Hi)> Ranges
    {
        get
        {
            Normalize();
            return _ranges;
        }
    }

    public bool IsEmpty
    {
        get
        {
            Normalize();
            return _ranges.Count == 0;
        }
    }

    public void AddCodePoint(int codePoint) => AddRange(codePoint, codePoint);

    public void AddRange(int lo, int hi)
    {
        if (lo > hi)
            throw new ArgumentException($"Range out of order: U+{lo:X}-U+{hi:X}");
        _ranges.Add((lo, hi));
        _normalized = false;
    }

    public void AddAll(CodePointSet other)
    {
        foreach (var (lo, hi) in other.Ranges)
            AddRange(lo, hi);
    }

    public CodePointSet Clone()
    {
        Normalize();
        return new CodePointSet([.. _ranges], normalized: true);
    }

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

    public CodePointSet Union(CodePointSet other)
    {
        var result = Clone();
        result.AddAll(other);
        result.Normalize();
        return result;
    }

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

    public CodePointSet Subtract(CodePointSet other) => Intersect(other.Complement());

    /// <summary>
    /// Maps every member through <paramref name="fold"/>, used for the <c>v</c>-mode
    /// <c>MaybeSimpleCaseFolding</c> of a class-set operand (ECMA-262 §22.2.1). Folding
    /// a set before complementing it is what makes <c>[^\P{Lowercase_Letter}]</c> under
    /// <c>vi</c> mean something other than <c>\p{Lowercase_Letter}</c>.
    /// </summary>
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
