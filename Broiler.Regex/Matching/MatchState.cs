// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           7
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    Medium
// Criteria:         8/0
// Resource impact:  3/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.Regex.Matching;

/// <summary>
/// A shared, per-run step budget guarding against catastrophic backtracking.
/// Carried by reference through every <see cref="MatchState"/> derived from one
/// match attempt, so the whole search tree shares a single counter (and distinct
/// attempts — including on different threads — get distinct budgets).
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=83AEA8
// Broiler-Falsified-If: two Run calls on one BroilerRegex share a Budget instance, so one attempt's backtracking exhausts the other's allowance
// Broiler-Human:        PENDING
internal sealed class Budget
{
    private long _steps;
    private readonly long _limit;

    public Budget(long limit) => _limit = limit;

    /// <summary>Consumes one step; returns false once the limit is exhausted.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=E67B20
    // Broiler-Falsified-If: new Budget(2) returns true from a third consecutive Step() call
    // Broiler-Human:        PENDING
    public bool Step() => ++_steps <= _limit;

    /// <summary>Resets the counter so one instance can serve every start position of a
    /// single run — each start gets the same fresh allowance a new instance would, with
    /// no per-start allocation.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=369A1D
    // Broiler-Falsified-If: after Reset(), a Budget whose Step() had returned false still returns false, so every later start position of the same Run reports no match
    // Broiler-Human:        PENDING
    public void Reset() => _steps = 0;
}

/// <summary>
/// The ECMAScript matcher "State" (§22.2.2.1): an end index plus the list of
/// capture spans. A <c>readonly struct</c> value type so deriving a state on every
/// atom (<see cref="WithPosition"/> runs once per matched code point) copies four
/// fields by value instead of allocating — the capture array is still shared by
/// reference and copied on write, so backtracking simply discards derived states.
/// The whole-match span is captures[0].
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.1; IP=Low; Security=Medium; Resources=3; Fingerprint=CBA457
// Broiler-Falsified-If: a capture set on a derived state is visible through the state it was derived from, so a branch that backtracks leaves its capture behind
// Broiler-Human:        PENDING
internal readonly struct MatchState(string input, int position, int[] captures, Budget budget)
{
    public readonly string Input = input;
    public readonly int Position = position;

    /// <summary>
    /// Capture spans, length <c>2 * (captureCount + 1)</c>. For group <c>i</c>,
    /// <c>[2i]</c> is the start and <c>[2i+1]</c> the end; <c>-1</c> means unset.
    /// </summary>
    public readonly int[] Captures = captures;

    public readonly Budget Budget = budget;

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=61D89C
    // Broiler-Falsified-If: the returned state carries a different Captures array or Budget instance than the receiver
    // Broiler-Human:        PENDING
    public MatchState WithPosition(int position)
        => new(Input, position, Captures, Budget);

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=B996C1
    // Broiler-Falsified-If: after WithCapture(1, 0, 2), the receiver's Captures[2] and Captures[3] no longer hold their previous values
    // Broiler-Human:        PENDING
    public MatchState WithCapture(int index, int start, int end)
    {
        var captures = (int[])Captures.Clone();
        captures[2 * index] = start;
        captures[2 * index + 1] = end;
        return new MatchState(Input, Position, captures, Budget);
    }

    /// <summary>Returns a state with the given capture indices reset to unset.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=CDB8A5
    // Broiler-Falsified-If: a capture index absent from indices is set to -1 in the returned state, or the receiver's Captures array is modified
    // Broiler-Human:        PENDING
    public MatchState WithResetCaptures(int[] indices)
    {
        if (indices.Length == 0)
            return this;
        var captures = (int[])Captures.Clone();
        foreach (var i in indices)
        {
            captures[2 * i] = -1;
            captures[2 * i + 1] = -1;
        }
        return new MatchState(Input, Position, captures, Budget);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=E300CD
    // Broiler-Falsified-If: a group whose start is set but whose end is still -1 is reported as captured
    // Broiler-Human:        PENDING
    public bool TryGetCapture(int index, out int start, out int end)
    {
        start = Captures[2 * index];
        end = Captures[2 * index + 1];
        return start >= 0 && end >= 0;
    }
}
