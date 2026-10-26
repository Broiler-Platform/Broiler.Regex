// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   5
// Annotated:        5/5
// Exempt:           7
// Human-reviewed:   0/5
// IP risk:          None
// Security risk:    High
// Criteria:         3/2
// Resource impact:  8/10 max
// Unverified:       5
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;

namespace Broiler.Regex.Engine;

/// <summary>
/// A compiled pattern of the new backend: an instruction program for the backtracking machine of decision D5 of
/// Broiler.VM's JSD-0081, with the character classes it tests and the registers it needs. It is built once by the
/// program compiler (<c>Broiler.Regex.Compiling.ProgramCompiler</c>) and never changed, so one program is shared by
/// every match of every thread.
/// </summary>
/// <remarks>
/// <para>
/// <b>The specification it runs.</b> ECMA-262's Pattern semantics (§22.2.2) build a matcher out of continuations, and
/// Broiler.VM's matcher (<c>JsRegExpMatcher</c>, decision D5's specification for the design and the units; none of its
/// code is ported) lowers the same semantics to an instruction array run by one loop. A program here is that lowering's
/// counterpart, written for this library: each continuation the specification would build is an address in the program,
/// and each choice the specification would try later is a backtrack frame on a stack of the machine's own.
/// </para>
/// <para>
/// <b>Classes.</b> A class is the sorted, merged ranges of the class's code points with its negation applied, flattened
/// into pairs <c>[low₀, high₀, low₁, high₁, …]</c>, so that one binary search tests a character.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decisions D5 and D8; IP=None; Security=High; Resources=6; Fingerprint=E22681
// Broiler-Falsified-If: two threads matching one program at the same time answer differently from one thread matching it twice, or a match call writes a field of the program
// Broiler-Human:        PENDING
internal sealed class RegexProgram
{
    /// <summary>The instructions, the first at address zero.</summary>
    private readonly RegexInstruction[] _code;

    /// <summary>Each class's ranges, flattened into pairs of inclusive bounds.</summary>
    private readonly int[][] _classes;

    /// <summary>
    /// A program over <paramref name="code"/> and <paramref name="classes"/>, which the caller hands over and does not
    /// keep.
    /// </summary>
    /// <param name="code">The instructions; the last reachable one of every path is an <see cref="RegexOp.Succeed"/> or a failure.</param>
    /// <param name="classes">Each class's ranges as pairs of inclusive bounds, sorted and disjoint.</param>
    /// <param name="captureCount">The pattern's capturing groups, group 0 not counted.</param>
    /// <param name="registerCount">The registers the program uses: the capture registers and the quantifiers' loop registers.</param>
    /// <param name="unicode">Whether the pattern has <c>u</c> or <c>v</c>, so that a surrogate pair is read as one character.</param>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=911D08
    // Broiler-Human:        PENDING
    internal RegexProgram(RegexInstruction[] code, int[][] classes, int captureCount, int registerCount, bool unicode)
    {
        _code = code;
        _classes = classes;
        CaptureCount = captureCount;
        RegisterCount = registerCount;
        Unicode = unicode;
    }

    /// <summary>The pattern's capturing groups, group 0 (the whole match) not counted.</summary>
    internal int CaptureCount { get; }

    /// <summary>How many slots a match writes: two per group, group 0 included.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=577663
    // Broiler-Human:        PENDING
    internal int SlotCount => 2 * (CaptureCount + 1);

    /// <summary>The registers a match uses: the capture registers, then the quantifiers' loop registers.</summary>
    internal int RegisterCount { get; }

    /// <summary>Whether a surrogate pair is one character: the pattern has <c>u</c> or <c>v</c>.</summary>
    internal bool Unicode { get; }

    /// <summary>The instructions, for the machine and for a test that reads the lowering.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=4B63A2
    // Broiler-Human:        PENDING
    internal ReadOnlySpan<RegexInstruction> Code => _code;

    /// <summary>The ranges of class <paramref name="index"/>, as pairs of inclusive bounds.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=150121
    // Broiler-Human:        PENDING
    internal ReadOnlySpan<int> ClassRanges(int index) => _classes[index];

    /// <summary>
    /// Matches <paramref name="subject"/> from <paramref name="start"/>, or at <paramref name="start"/> alone when
    /// <paramref name="anchored"/> is set, and writes the match's offsets into <paramref name="slots"/>: two per group,
    /// group 0 first, -1 for a group that did not participate (requirements R7 and R8). The work is charged to
    /// <paramref name="meter"/> before it is done, in blocks, and bounded by <paramref name="ceilings"/> (R1 and R3).
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="start">Where the match starts, or the search begins: from 0 to the subject's length.</param>
    /// <param name="anchored">Whether to try <paramref name="start"/> alone, whatever the pattern's <c>y</c> flag.</param>
    /// <param name="slots">
    /// The caller's slots, at least <see cref="SlotCount"/> of them. A match writes the first <see cref="SlotCount"/>; any
    /// other answer, and a meter's exception, leaves each of them -1. The slots past them are not touched.
    /// </param>
    /// <param name="meter">The caller's meter, or null for none.</param>
    /// <param name="ceilings">The caller's step, frame and trail ceilings, made by their constructor.</param>
    /// <param name="limit">The ceiling reached when the answer is <see cref="RegexStatus.LimitExceeded"/>; otherwise <see cref="RegexLimit.None"/>.</param>
    /// <param name="units">The units of work the call did, which the meter was charged for before they were done.</param>
    /// <returns><see cref="RegexStatus.Match"/>, <see cref="RegexStatus.NoMatch"/> or <see cref="RegexStatus.LimitExceeded"/>.</returns>
    /// <remarks>
    /// <b>The entry of R7 and R8, internal until task 2.4c</b> routes <see cref="BroilerRegex"/> through it. A broken
    /// precondition, the caller's (convention 3), is an exception; every other answer is a status. The program is not
    /// changed by a call, so concurrent calls on one program, and a call the meter makes while it is charged, each answer
    /// as alone; the call's registers, frames and trail are arrays rented from the shared array pool and given back
    /// before it returns, so after the pool holds arrays of the sizes the program needs, a call allocates nothing,
    /// whatever its capture count.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is negative or past the subject's end.</exception>
    /// <exception cref="ArgumentException"><paramref name="slots"/> holds fewer than <see cref="SlotCount"/> slots, or <paramref name="ceilings"/> is the default value.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2; IP=None; Security=High; Resources=8; Fingerprint=775D0C
    // Broiler-Falsified-If: /b/ anchored at 0 of "ab" answers Match, /a(b)?/ on "a" writes a slot other than -1 for group 1, a call that reaches a ceiling answers Match or NoMatch, or a call that does not match leaves a slot other than -1
    // Broiler-Human:        PENDING
    internal RegexStatus Match(ReadOnlySpan<char> subject, int start, bool anchored, Span<int> slots,
        RegexWorkCharge? meter, RegexMatchCeilings ceilings, out RegexLimit limit, out ulong units)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, subject.Length);
        if (slots.Length < SlotCount)
            throw new ArgumentException("The slots hold fewer than two per capture group, group 0 included.", nameof(slots));
        if (!ceilings.IsSet)
            throw new ArgumentException("The ceilings are the default value; make them with their constructor.", nameof(ceilings));

        var mine = slots[..SlotCount];
        var status = RegexStatus.NoMatch;
        var machine = new RegexMachine(this, subject, meter, ceilings);
        try
        {
            status = machine.Run(start, anchored, mine);
            limit = machine.Exceeded;
            units = machine.Spent;
            return status;
        }
        finally
        {
            machine.Release();
            if (status != RegexStatus.Match)
                mine.Fill(-1);
        }
    }

    /// <summary>
    /// The weight of a class test over <paramref name="rangeCount"/> ranges: the comparisons its binary search can make
    /// beyond the first, ⌊log₂ r⌋ for r ranges and none for an empty class, which the machine charges on top of the
    /// dispatch's unit, so that a class test costs ⌊log₂ r⌋ + 1 units, the most ranges its search reads.
    /// </summary>
    /// <remarks>
    /// This is how this library reads decision D8's "a property class's weight per character tested". Broiler.VM's
    /// class keeps each property escape as a table of its own and charges one more step for every table a test
    /// searches; this library flattens a class, property escapes included, into one sorted table when it is compiled, so
    /// a test is one binary search whose cost grows with the logarithm of the class's ranges. A class of one range costs
    /// what any instruction costs, one unit; <c>\p{L}</c>, 684 ranges at UCD 17.0.0, costs ten.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Medium; Resources=0; Fingerprint=680B3B
    // Broiler-Falsified-If: the weight of a class of one range is not zero, of two or three ranges not one, or of 684 ranges not nine
    // Broiler-Human:        PENDING
    internal static int ClassWeight(int rangeCount) => rangeCount <= 1 ? 0 : BitOperations.Log2((uint)rangeCount);
}
