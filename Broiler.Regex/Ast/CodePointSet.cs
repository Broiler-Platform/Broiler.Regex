// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   26
// Annotated:        26/26
// Exempt:           5
// Human-reviewed:   0/26
// IP risk:          Low
// Security risk:    Medium
// Criteria:         11/0
// Resource impact:  6/10 max
// Unverified:       26
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
/// <para>
/// <b>Immutable</b> (the programme's conventions 7 and 10, decision D31 of Broiler.VM's JSD-0081): a set's ranges are
/// sorted and merged before its constructor returns, and nothing changes them after it. Every operation answers a
/// new set, so one set may be read by a compiled pattern's concurrent matches and held in a static field. The
/// library builds a set from ranges in any order through the internal <see cref="Builder"/>, which normalizes once,
/// when the set is made.
/// </para>
/// <para>
/// <b>Charged when a compilation uses it</b> (requirement R2, decision D8; task 2.5 of SJL-11.2). The library's own
/// compilations call the internal overloads that take the compilation's <see cref="CompileCharge"/>, each charging,
/// before it reads anything, one unit per range of each operand it reads (<see cref="CompileSite"/>); the public members
/// charge nothing, as a caller of them does the work in its own name. Each operation is one pass over the operands'
/// sorted ranges, so its work is in proportion to the units it charges.
/// </para>
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
    /// <param name="ranges">The ranges, each inclusive; the list is copied.</param>
    /// <param name="charge">The compilation that builds the set, charged for its normalization (<see cref="CompileSite.Normalize"/>), or null.</param>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=2; Fingerprint=CE4F5B
    // Broiler-Human:        PENDING
    internal static CodePointSet FromRanges(List<(int Lo, int Hi)> ranges, CompileCharge? charge = null)
    {
        foreach (var (lo, hi) in ranges)
        {
            if (lo > hi)
                throw new ArgumentException("A range is out of order.", nameof(ranges));
        }

        var copy = ranges.ToArray();
        return Normalized(copy, charge);
    }

    /// <summary>
    /// The set of <paramref name="ranges"/>, which this call owns and reorders: sorted, then each range merged
    /// into the one before it when the two overlap or touch, so a set built from single code points equals the
    /// same set built from one range.
    /// </summary>
    /// <remarks>
    /// Ranges that already arrive in order of their low ends, as a provider's set and a run of code points mapped in order
    /// do, are merged in one pass with no sort. A compilation is charged one unit per range for the pass and, when the
    /// ranges are out of order, ⌊log₂ n⌋ more per range for the sort, before each (<see cref="CompileSite.Normalize"/>).
    /// </remarks>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=DEA094
    // Broiler-Falsified-If: ranges that overlap or touch, such as (0, 4) and (5, 9), or a range ending at int.MaxValue followed by another, are left as two entries instead of being merged, or ranges out of order are merged without being sorted
    // Broiler-Human:        PENDING
    private static CodePointSet Normalized((int Lo, int Hi)[] ranges, CompileCharge? charge)
    {
        if (ranges.Length == 0)
            return new CodePointSet();

        charge?.Charge(CompileSite.Normalize, ranges.Length);
        var ordered = true;
        for (var i = 1; i < ranges.Length && ordered; i++)
            ordered = ranges[i - 1].Lo <= ranges[i].Lo;
        if (!ordered)
        {
            charge?.Charge(CompileSite.Normalize, (long)ranges.Length * (CompileCharge.SortFactor(ranges.Length) - 1));
            Array.Sort(ranges, static (a, b) => a.Lo != b.Lo ? a.Lo.CompareTo(b.Lo) : a.Hi.CompareTo(b.Hi));
        }

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

    /// <summary>How many code points the set holds, read from its ranges in one pass.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=1E8DEA
    // Broiler-Falsified-If: the count of Of(0, 0x10FFFF) is not 1,114,112, or of the empty set not zero
    // Broiler-Human:        PENDING
    internal long CodePointCount()
    {
        var count = 0L;
        foreach (var (lo, hi) in _ranges)
            count += (long)hi - lo + 1;
        return count;
    }

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

    /// <summary>
    /// <see cref="Complement()"/>, charged to <paramref name="charge"/> one unit per range and one more before it is
    /// taken (<see cref="CompileSite.Complement"/>).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Medium; Resources=1; Fingerprint=5AD5F0
    // Broiler-Human:        PENDING
    internal CodePointSet Complement(CompileCharge charge)
    {
        charge.Charge(CompileSite.Complement, (long)_ranges.Length + 1);
        return Complement();
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
    /// <see cref="Union(CodePointSet)"/>, charged to <paramref name="charge"/> one unit per range of each operand before it
    /// is taken (<see cref="CompileSite.Union"/>).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Medium; Resources=1; Fingerprint=5ED2DC
    // Broiler-Human:        PENDING
    internal CodePointSet Union(CodePointSet other, CompileCharge charge)
    {
        charge.Charge(CompileSite.Union, (long)_ranges.Length + other._ranges.Length);
        return Union(other);
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

    /// <summary>
    /// <see cref="Intersect(CodePointSet)"/>, charged to <paramref name="charge"/> one unit per range of each operand
    /// before it is taken (<see cref="CompileSite.Intersection"/>).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Medium; Resources=1; Fingerprint=0FE6CB
    // Broiler-Human:        PENDING
    internal CodePointSet Intersect(CodePointSet other, CompileCharge charge)
    {
        charge.Charge(CompileSite.Intersection, (long)_ranges.Length + other._ranges.Length);
        return Intersect(other);
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
    /// <see cref="Subtract(CodePointSet)"/>, charged to <paramref name="charge"/> one unit per range of each operand and
    /// one more, for the complement it takes on the way, before it is taken (<see cref="CompileSite.Difference"/>).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Medium; Resources=1; Fingerprint=973189
    // Broiler-Human:        PENDING
    internal CodePointSet Subtract(CodePointSet other, CompileCharge charge)
    {
        charge.Charge(CompileSite.Difference, (long)_ranges.Length + other._ranges.Length + 1);
        return Subtract(other);
    }

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
    /// Members gathered in any order, for one set: a class's members as the parser reads them, or a union of many
    /// operands. Single code points and ranges are appended, and whole sets kept as they are; <see cref="ToSet"/> sorts
    /// and merges the appended ranges once and then unions the sets two by two, a level at a time, so a union of
    /// <c>k</c> sets of <c>n</c> ranges in all reads each range at most <c>⌈log₂ (k + 1)⌉</c> times, where adding the
    /// sets one after another to a growing set would read the first sets' ranges <c>k</c> times. A builder is the
    /// parser's own, one per set it builds, and is never shared.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=8D1EAA
    // Broiler-Falsified-If: a set built from members added out of order, such as 9, 0 and then the range 1..8, is anything other than the single range 0..9, or a builder of k sets reads a range more than ceil(log2(k + 1)) times
    // Broiler-Human:        PENDING
    internal sealed class Builder
    {
        private readonly List<(int Lo, int Hi)> _pending = [];

        /// <summary>The whole sets added, each sorted and merged already.</summary>
        private readonly List<CodePointSet> _sets = [];

        /// <summary>Adds one code point.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=129D7E
        // Broiler-Human:        PENDING
        internal void Add(int codePoint) => _pending.Add((codePoint, codePoint));

        /// <summary>Adds the code points <paramref name="lo"/> to <paramref name="hi"/>; the caller has ordered them.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=BD880A
        // Broiler-Human:        PENDING
        internal void AddRange(int lo, int hi) => _pending.Add((lo, hi));

        /// <summary>Adds every code point of <paramref name="set"/>, which is kept whole until <see cref="ToSet"/> unions it.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=2; Fingerprint=B16B52
        // Broiler-Human:        PENDING
        internal void AddAll(CodePointSet set)
        {
            if (!set.IsEmpty)
                _sets.Add(set);
        }

        /// <summary>
        /// The set of every code point added so far; the builder can go on adding. A compilation is charged for the sort
        /// and merge of the appended ranges (<see cref="CompileSite.Normalize"/>) and for each union
        /// (<see cref="CompileSite.Union"/>), before each.
        /// </summary>
        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=2686FA
        // Broiler-Falsified-If: the set holds a code point no member added holds, or lacks one a member added holds
        // Broiler-Human:        PENDING
        internal CodePointSet ToSet(CompileCharge? charge = null)
        {
            var level = new List<CodePointSet>(_sets.Count + 1);
            if (_pending.Count > 0)
                level.Add(FromRanges(_pending, charge));
            level.AddRange(_sets);
            if (level.Count == 0)
                return new CodePointSet();

            while (level.Count > 1)
            {
                var next = new List<CodePointSet>((level.Count + 1) / 2);
                for (var i = 0; i + 1 < level.Count; i += 2)
                    next.Add(charge is null ? level[i].Union(level[i + 1]) : level[i].Union(level[i + 1], charge));
                if (level.Count % 2 == 1)
                    next.Add(level[^1]);
                level = next;
            }

            return level[0];
        }
    }
}
