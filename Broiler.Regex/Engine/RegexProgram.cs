// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   10
// Annotated:        10/10
// Exempt:           11
// Human-reviewed:   0/10
// IP risk:          None
// Security risk:    High
// Criteria:         4/2
// Resource impact:  8/10 max
// Unverified:       10
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Numerics;
using Broiler.Unicode.Algorithms;

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

    /// <summary>The groups the back-references read, each reference's list one run of entries (<see cref="RegexOp.Backreference"/>).</summary>
    private readonly int[] _referenced;

    /// <summary>The tables of strings the classes of strings read (<see cref="RegexOp.StringClass"/>), each shared by every occurrence of its strings.</summary>
    private readonly RegexStrings[] _strings;

    /// <summary>
    /// A program over <paramref name="code"/> and <paramref name="classes"/>, which the caller hands over and does not
    /// keep.
    /// </summary>
    /// <param name="code">The instructions; the last reachable one of every path is an <see cref="RegexOp.Succeed"/> or a failure.</param>
    /// <param name="classes">Each class's ranges as pairs of inclusive bounds, sorted and disjoint.</param>
    /// <param name="referenced">The group numbers the back-references read, each reference's list one run of entries.</param>
    /// <param name="strings">The tables of strings the classes of strings read.</param>
    /// <param name="captureCount">The pattern's capturing groups, group 0 not counted.</param>
    /// <param name="registerCount">The registers the program uses: the capture registers and the quantifiers' loop registers.</param>
    /// <param name="unicode">Whether the pattern has <c>u</c> or <c>v</c>, so that a surrogate pair is read as one character.</param>
    /// <param name="folding">
    /// The Unicode owner's case folding, which an instruction comparing under <c>i</c> reads for Canonicalize and for a
    /// character's case orbit; null for a program with no such instruction.
    /// </param>
    /// <param name="firstTest">The address of the instruction the start-position scan tests positions with, or -1 for none (<see cref="FirstTest"/>).</param>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=B3022A
    // Broiler-Human:        PENDING
    internal RegexProgram(RegexInstruction[] code, int[][] classes, int[] referenced, RegexStrings[] strings, int captureCount, int registerCount, bool unicode, UnicodeCaseFolding? folding,
        int firstTest = -1)
    {
        _code = code;
        _classes = classes;
        _referenced = referenced;
        _strings = strings;
        FirstTest = firstTest;
        CaptureCount = captureCount;
        RegisterCount = registerCount;
        Unicode = unicode;
        Folding = folding;
        OrbitLength = folding is null ? 0 : unicode ? folding.MaxOrbit : folding.MaxUpperOrbit;
    }

    /// <summary>
    /// The Unicode owner's case folding, for the instructions that compare under <c>i</c>: simple case folding under
    /// <c>u</c> and <c>v</c>, the upper case of one code unit without them (ECMA-262 §22.2.2.7.3, both branches of
    /// Canonicalize), and each one's orbit. Null when no instruction compares under <c>i</c>. The provider's objects are
    /// immutable, so one serves every match of every thread.
    /// </summary>
    internal UnicodeCaseFolding? Folding { get; }

    /// <summary>
    /// The most code points a character's case orbit holds in this program's mode, itself included: the searches a
    /// <see cref="RegexOp.ClassIgnoringCase"/> test makes at most, and the room the machine keeps for an orbit. Zero when
    /// <see cref="Folding"/> is null.
    /// </summary>
    internal int OrbitLength { get; }

    /// <summary>The pattern's capturing groups, group 0 (the whole match) not counted.</summary>
    internal int CaptureCount { get; }

    /// <summary>
    /// The literal-prefix prefilter of task 2.4c (plan row 2.4c): the address of the one-character instruction every
    /// attempt reads first, before any choice, or -1 when attempts read nothing that certain. An attempt at a position
    /// whose character that test refuses fails there, so a search tests each position with it alone and enters the
    /// program only where it holds (<see cref="RegexMachine"/>'s remarks, "The start-position scan").
    /// </summary>
    /// <remarks>
    /// It is the test of the first instruction an attempt dispatches past those that only write a register (a group's
    /// start, a capture reset, a count's start): a character or class instruction that reads left to right, the first atom
    /// of an atom run, or the atom of a run that requires at least one character. Anything else first, a choice, an
    /// assertion, a look-around, a class of strings, a run that may take nothing, a loop's bound check, leaves it -1. The
    /// program compiler sets it only with its fast paths on.
    /// </remarks>
    internal int FirstTest { get; }

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

    /// <summary>The <paramref name="count"/> group numbers a back-reference reads, from entry <paramref name="first"/> of the program's list.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=E32DD4
    // Broiler-Human:        PENDING
    internal ReadOnlySpan<int> ReferencedGroups(int first, int count) => _referenced.AsSpan(first, count);

    /// <summary>The ranges of class <paramref name="index"/>, as pairs of inclusive bounds.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=150121
    // Broiler-Human:        PENDING
    internal ReadOnlySpan<int> ClassRanges(int index) => _classes[index];

    /// <summary>The table of strings <paramref name="index"/>, which a <see cref="RegexOp.StringClass"/> names.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=516A25
    // Broiler-Human:        PENDING
    internal RegexStrings Strings(int index) => _strings[index];

    /// <summary>How many tables of strings the program holds: one for each distinct class of strings, however often it occurs.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=1962ED
    // Broiler-Human:        PENDING
    internal int StringTableCount => _strings.Length;

    /// <summary>
    /// Matches <paramref name="subject"/> from <paramref name="start"/>, or at <paramref name="start"/> alone when
    /// <paramref name="anchored"/> is set, and writes the match's offsets into <paramref name="slots"/>: two per group,
    /// group 0 first, -1 for a group that did not participate (requirements R7 and R8). The work is charged to
    /// <paramref name="meter"/> before it is done, in blocks, and bounded by <paramref name="ceilings"/> (R1 and R3).
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="start">
    /// Where the match starts, or the search begins: a code-unit index from 0 to the subject's length. Under <c>u</c> and
    /// <c>v</c> a start inside a surrogate pair splits the pair for the whole call: a read forward from the start meets the
    /// pair's low half as a lone unit, and a read backward from it, a look-behind's, <c>\b</c>'s or <c>^</c>'s under
    /// <c>m</c>, its high half as another. RegExpBuiltinExec (ECMA-262 §22.2.7.2, step 13.b) starts its matcher from such a
    /// <c>lastIndex</c> at the whole pair, so a caller that answers as RegExpBuiltinExec does steps back to the pair's
    /// first unit before it calls, as Broiler.VM's realm does, and reports the index it decides.
    /// </param>
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
    /// whatever its capture count. Frames and trail past <see cref="RegexMachine.PooledLength"/> integers are the call's
    /// own and go to the collector, so a call that needed many leaves none of them pooled.
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

    /// <summary>
    /// The units a class test costs: one for the dispatch, and the class's weight (<see cref="ClassWeight"/>) for each
    /// code point the test searches the class for, as decision D8 charges a class "its weight per character tested".
    /// </summary>
    /// <param name="rangeCount">The class's ranges.</param>
    /// <param name="searches">
    /// The code points one test searches for at most: one compared exactly or by Canonicalize; under <c>i</c>, the most a
    /// character's case orbit holds (<see cref="OrbitLength"/>), each of which <see cref="RegexOp.ClassIgnoringCase"/>
    /// may search for.
    /// </param>
    /// <remarks>
    /// A class of one range costs one unit however it is tested, as a test of it is one comparison per code point;
    /// <c>\p{L}</c>, 684 ranges, costs ten exactly and thirty-seven under <c>i</c> with <c>u</c>, whose orbits hold at most
    /// four code points at UCD 17.0.0. The lookups into the Unicode owner's folding tables that find a Canonicalize or an
    /// orbit are not weighed: they are bounded, whatever the pattern and the subject, as the dispatch is.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Medium; Resources=0; Fingerprint=DF8A5C
    // Broiler-Falsified-If: a class of 684 ranges costs other than ten units searched once or thirty-seven searched four times, or a class of one range costs other than one unit
    // Broiler-Human:        PENDING
    internal static int ClassCost(int rangeCount, int searches) => 1 + (searches * ClassWeight(rangeCount));
}
