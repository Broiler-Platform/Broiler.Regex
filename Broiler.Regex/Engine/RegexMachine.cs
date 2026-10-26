// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   41
// Annotated:        41/41
// Exempt:           26
// Human-reviewed:   0/41
// IP risk:          None
// Security risk:    High
// Criteria:         29/16
// Resource impact:  8/10 max
// Unverified:       41
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Buffers;
using Broiler.Unicode.Algorithms;

namespace Broiler.Regex.Engine;

/// <summary>
/// The backtracking machine of decision D5 of Broiler.VM's JSD-0081: one loop that runs a <see cref="RegexProgram"/>,
/// with an explicit stack of backtrack frames and an undo trail, the defunctionalised form of ECMA-262's continuations
/// (§22.2.2). One machine serves one match call and is not shared; the program it runs is.
/// </summary>
/// <remarks>
/// <para>
/// <b>No recursion.</b> Where the specification calls a continuation, the machine continues at an address; where it would
/// try a choice later, the machine pushes a frame and pops it when the path in hand fails. No method of this namespace
/// reaches itself, directly or through another, in the IL call graph (bundle <c>sjl-11-001</c>'s check A-4, the
/// matcher's half of requirement R4), so the CLR stack a match uses does not grow with the pattern or the subject.
/// </para>
/// <para>
/// <b>A frame</b> is four integers: the address to resume at, the position to resume from, the trail's height when the
/// frame was pushed, and a mark. <b>A trail cell</b> is two: the register a write changed and the value it held.
/// Backtracking pops the newest frame and undoes every write above its trail height, so the registers are what they were
/// when the choice was made; a write that does not change its register records nothing, and one that does records a cell
/// even with no frame pending, where the cell serves only the reset below (so the trail ceiling bounds a loop that holds
/// no choice too, <see cref="RegexMatchCeilings"/>'s remarks). When an attempt at one start position fails, every write of
/// the attempt has been undone, so the next position starts from registers that all read -1, with no reset of its own.
/// </para>
/// <para>
/// <b>A marked frame</b> is the one backtrack frame of a fast path that has more than one way left (task 2.4c): its
/// address is its instruction's, not a place to resume, and its mark, never negative, says which way comes next. A greedy
/// run's mark is the position its required characters end at, which it gives characters back down to; a lazy run's the
/// characters it may still take; a class of strings' the next string, or its single characters, or its empty string, to
/// try. Popping a marked frame takes that way, and the frame stays, with its position or its mark moved on, while another
/// is left; it is the same frame, at the same trail height, so a run of any length holds one frame and writes no trail
/// cell. Every other frame's mark is <see cref="Unmarked"/>.
/// </para>
/// <para>
/// <b>A look-around</b> runs its body on the same stack, with no second loop and no call: it pushes a <i>barrier</i>, a
/// frame whose address field is negative, and keeps the barrier's place in a register of its own. When the body matches,
/// the barrier and every frame above it are dropped at once, so nothing after the look-around backtracks into the body,
/// and the index goes back to the barrier's; when every way through the body fails, backtracking reaches the barrier,
/// which fails in its turn for a positive look-around and, for a negative one, resumes after it with the body's captures
/// undone. A barrier is the only frame popped without resuming, and only once, so backtracking past it costs no
/// unit of its own: the instruction that pushed it paid for it. A look-around nested in another's body pushes its
/// barrier above the outer one's and drops or pops it before the outer one is reached.
/// </para>
/// <para>
/// <b>Charging.</b> Before it dispatches an instruction the machine adds the instruction's cost to the units spent.
/// When that passes the step ceiling, the instruction is not dispatched and the call answers
/// <see cref="RegexStatus.LimitExceeded"/> naming <see cref="RegexLimit.Steps"/>; when it passes what the meter has
/// been charged, the meter is charged the whole blocks of <see cref="Block"/> units that cover it, and only then is the
/// instruction dispatched. A fast path charges each character it examines the same way, before it reads it: a run's
/// character and a folded atom cost what the atom's dispatch would, and a string's character one unit (task 2.4c); the
/// start-position scan charges each position it skips before it moves past it. So before the k-th unit of work, at least k units have been charged, and the meter is never more than
/// <see cref="Block"/> - 1 units ahead of the work; the tests of check A-1 read both at every unit charged through
/// <see cref="DispatchObserver"/>, which the library never sets. The units of one call run on across all its start
/// positions, with no reset. Undoing a write costs nothing of its own: the instruction that made the write paid for it;
/// nor does giving back a run's character, which the instructions that run after it pay for.
/// </para>
/// <para>
/// <b>Memory.</b> The registers, the frames and the trail are arrays rented from the shared array pool and returned
/// when the call ends (<see cref="Release"/>), its exceptions included, so after the pool holds arrays of the sizes a
/// pattern needs, a call allocates nothing at all, whatever its capture count (requirement R8, bundle
/// <c>sjl-11-001</c>'s check A-8). The frames and the trail start small and double as a match needs them, never past
/// their ceilings; past <see cref="PooledLength"/> integers they are allocated for the call alone and left to the
/// collector, so a call that held many frames or cells leaves none of them pooled (a pooled array stays with the thread
/// that returned it, or in the pool's per-processor stores, until the pool trims it). While an array doubles, the one
/// it replaces is held too. An array holds whatever its last user left in it; the machine reads only the registers it
/// fills when it starts and the frames and cells it has written.
/// </para>
/// <para>
/// <b>The fast paths read what the instructions they replace would read</b>: a run's atom, a folded atom and a class of
/// strings' single characters are tested by <see cref="AtomHolds"/>, the test a dispatched character or class instruction
/// makes, and a string's characters through <see cref="NextCharacter"/> as a <see cref="RegexOp.Literal"/> reads them, so a
/// surrogate pair, the split pair of a start inside one and the direction are read alike.
/// </para>
/// <para>
/// <b>The start-position scan</b> is the literal-prefix prefilter of task 2.4c. When every attempt of a program reads one
/// character first, before any choice (<see cref="RegexProgram.FirstTest"/>), a search that is not anchored tests each
/// position with that instruction's test alone and starts an attempt only where it holds, the next position by
/// AdvanceStringIndex as the search's own. A position skipped costs the test's dispatch, one unit and the class's weight,
/// through the same charging as a dispatch; the position admitted costs nothing in the scan, as the attempt's first
/// dispatch makes the same test and pays for it; the subject's end, which holds no character to admit, ends the search
/// with no attempt and no unit. The answer is the search's without the scan, since an attempt at a position the test
/// refuses fails at its first test with nothing to undo but the group starts before it.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=8; Fingerprint=7D0010
// Broiler-Falsified-If: a match over a subject of any length, or of a pattern of any nesting within the library's maximum, ends the process with a stack overflow, or a failed attempt leaves a register other than -1 for the next start position
// Broiler-Human:        PENDING
internal ref struct RegexMachine
{
    /// <summary>The integers of one frame: the address to resume at, the position, the trail's height, the mark.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=9E9389
    // Broiler-Human:        PENDING
    private const int FrameSize = 4;

    /// <summary>The mark of a frame that is no fast path's: a choice's, or a look-around's barrier.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=4295E0
    // Broiler-Human:        PENDING
    private const int Unmarked = -1;

    /// <summary>The integers of one trail cell: the register and the value it held.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=3A53FC
    // Broiler-Human:        PENDING
    private const int CellSize = 2;

    /// <summary>The units the meter is charged in, whole blocks before the work they pay for (decision D8).</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Medium; Resources=0; Fingerprint=5491AD
    // Broiler-Human:        PENDING
    internal const ulong Block = 512;

    /// <summary>
    /// The longest frame or trail array a call takes from the shared array pool and gives back to it: 65,536 integers,
    /// 256 KiB. A longer one, which only a call holding more than 16,384 frames or 32,768 trail cells needs, is allocated
    /// for the call alone and left to the collector when it ends, so that one call's peak does not stay pooled after it.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=2; Fingerprint=357418
    // Broiler-Human:        PENDING
    internal const int PooledLength = 1 << 16;

    private readonly RegexProgram _program;
    private readonly ReadOnlySpan<RegexInstruction> _code;
    private readonly ReadOnlySpan<char> _subject;
    private readonly bool _unicode;
    private readonly RegexWorkCharge? _meter;

    /// <summary>The Unicode owner's case folding, for the instructions that compare under <c>i</c>; null when none does.</summary>
    private readonly UnicodeCaseFolding? _folding;

    /// <summary>Room for one character's case orbit, rented when the program tests a class under <c>i</c>, and empty otherwise.</summary>
    private int[] _orbit;

    /// <summary>The step ceiling in units, <see cref="ulong.MaxValue"/> for none.</summary>
    private readonly ulong _stepCeiling;

    private readonly int _frameCeiling;
    private readonly int _trailCeiling;

    /// <summary>The registers: the capture registers, then the quantifiers' loop registers, in a rented array.</summary>
    private int[] _registers;

    /// <summary>The backtrack frames, <see cref="FrameSize"/> integers each, and how many are held.</summary>
    private int[] _frames;

    private int _framesHeld;

    /// <summary>The undo trail, <see cref="CellSize"/> integers a cell, and how many cells are held.</summary>
    private int[] _trail;

    private int _cellsHeld;

    /// <summary>The units of work the call has dispatched, the instruction being dispatched included.</summary>
    private ulong _spent;

    /// <summary>The units the meter has been charged, <see cref="ulong.MaxValue"/> when there is no meter.</summary>
    private ulong _paid;

    /// <summary>The smaller of <see cref="_paid"/> and <see cref="_stepCeiling"/>: the units the loop may spend before it stops to charge or to refuse.</summary>
    private ulong _threshold;

    /// <summary>Where the attempt that matched ended.</summary>
    private int _end;

    /// <summary>
    /// The call's start when, under <c>u</c> and <c>v</c>, it falls inside a surrogate pair, and -1 otherwise: every read
    /// of the call then takes the pair's two halves as lone units, so that the characters a read meets are the same
    /// whichever way it reads and wherever it starts (<see cref="Run"/>'s remarks).
    /// </summary>
    private int _splitAt;

    /// <summary>A machine for one call of <paramref name="program"/> over <paramref name="subject"/>, charging <paramref name="meter"/> under <paramref name="ceilings"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=3; Fingerprint=5CA4C9
    // Broiler-Human:        PENDING
    internal RegexMachine(RegexProgram program, ReadOnlySpan<char> subject, RegexWorkCharge? meter, RegexMatchCeilings ceilings)
    {
        _program = program;
        _code = program.Code;
        _subject = subject;
        _unicode = program.Unicode;
        _meter = meter;
        _stepCeiling = ceilings.Steps ?? ulong.MaxValue;
        _frameCeiling = ceilings.Frames;
        _trailCeiling = ceilings.Trail;
        _paid = meter is null ? ulong.MaxValue : 0;
        _threshold = Math.Min(_paid, _stepCeiling);
        _folding = program.Folding;
        _registers = ArrayPool<int>.Shared.Rent(program.RegisterCount);
        _registers.AsSpan(0, program.RegisterCount).Fill(-1);
        _frames = ArrayPool<int>.Shared.Rent(16 * FrameSize);
        _trail = ArrayPool<int>.Shared.Rent(16 * CellSize);
        _orbit = program.OrbitLength > 0 ? ArrayPool<int>.Shared.Rent(program.OrbitLength) : [];
    }

    /// <summary>Returns the rented arrays to the pool; the machine is not used after.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=FBCED8
    // Broiler-Falsified-If: an array is returned to the pool twice, or a call's arrays stay out of the pool after it ends
    // Broiler-Human:        PENDING
    internal void Release()
    {
        ArrayPool<int>.Shared.Return(_registers);
        ReturnToPool(_frames);
        ReturnToPool(_trail);
        if (_orbit.Length > 0)
            ArrayPool<int>.Shared.Return(_orbit);
        _registers = _frames = _trail = _orbit = [];
    }

    /// <summary>The ceiling the call reached, when it answered <see cref="RegexStatus.LimitExceeded"/>; otherwise <see cref="RegexLimit.None"/>.</summary>
    internal RegexLimit Exceeded { get; private set; }

    /// <summary>
    /// A test's observer of the loop, or null, as the library always leaves it: called before each instruction is
    /// dispatched and after its units are charged, and likewise before each character a fast path examines, each
    /// back-reference's comparison and each position the start-position scan skips, with the units of the call through
    /// that work, so that a test reads what its own
    /// meter holds at that moment (bundle <c>sjl-11-001</c>'s check A-1). It
    /// is a <see cref="RegexWorkCharge"/> only so that the loop invokes no delegate type but the meter's.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Low; Resources=0; Fingerprint=62AB7A
    // Broiler-Human:        PENDING
    internal RegexWorkCharge? DispatchObserver { private get; init; }

    /// <summary>
    /// The units of work the call did: every instruction it dispatched, the one that reached a frame or trail ceiling
    /// included, and not the one the step ceiling refused.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=036C68
    // Broiler-Human:        PENDING
    internal readonly ulong Spent => _spent;

    /// <summary>
    /// Runs the program from <paramref name="start"/>, or at it alone when <paramref name="anchored"/> is set, and writes
    /// the first match's offsets into <paramref name="slots"/>.
    /// </summary>
    /// <remarks>
    /// The search is RegExpBuiltinExec's loop (ECMA-262 §22.2.7.2): an attempt at each position from the start, the next
    /// position by AdvanceStringIndex, so that under <c>u</c> and <c>v</c> a search does not start inside a surrogate
    /// pair it stepped over; the position after the last character is tried too. The start is the caller's: under
    /// <c>u</c> and <c>v</c> a start inside a pair splits it for the whole call, so a read forward from the start meets
    /// the pair's low half as a lone unit, and a read backward from it (a look-behind, <c>\b</c>, <c>^</c> under
    /// <c>m</c>) its high half as another. RegExpBuiltinExec's step 13.b starts the matcher at the pair instead, and that
    /// step back is the caller's to take before it calls (<see cref="RegexProgram.Match"/>). A program with a first test
    /// skips, before each attempt, the positions that test refuses (the remarks' start-position scan).
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2; IP=None; Security=High; Resources=6; Fingerprint=0C72DD
    // Broiler-Falsified-If: an anchored call tries a position after its start, a search skips the position after the last character when the program has no first test, under u a search starts inside a surrogate pair it stepped over, a read of a call that starts inside a pair takes the pair as one character, or an anchored call scans
    // Broiler-Human:        PENDING
    internal RegexStatus Run(int start, bool anchored, Span<int> slots)
    {
        _splitAt = _unicode && start > 0 && start < _subject.Length
            && char.IsHighSurrogate(_subject[start - 1]) && char.IsLowSurrogate(_subject[start]) ? start : -1;
        var at = start;
        var first = anchored ? -1 : _program.FirstTest;
        while (true)
        {
            if (first >= 0)
            {
                switch (Scan(first, ref at))
                {
                    case Outcome.Failed:
                        return RegexStatus.NoMatch;
                    case Outcome.Exceeded:
                        return RegexStatus.LimitExceeded;
                }
            }

            switch (MatchFrom(at))
            {
                case Outcome.Matched:
                    WriteSlots(at, slots);
                    return RegexStatus.Match;
                case Outcome.Exceeded:
                    return RegexStatus.LimitExceeded;
            }

            if (anchored || at >= _subject.Length)
                return RegexStatus.NoMatch;

            at = NextStart(at);
        }
    }

    /// <summary>
    /// The start-position scan of the literal-prefix prefilter: from <paramref name="at"/>, the first position whose
    /// character the program's first test, the instruction at <paramref name="test"/>, admits. A position the test
    /// refuses is skipped at that instruction's cost, one unit and the class's weight, charged before the scan moves past
    /// it; a position it admits costs nothing here, as the attempt's first dispatch makes the same test and pays for it;
    /// and the position after the last character, which holds no character to admit, ends the scan with no attempt and
    /// no unit. An attempt anywhere the test refuses would fail at its first test, so the positions skipped are those
    /// RegExpBuiltinExec's loop would try and fail at, and the answer is the loop's.
    /// </summary>
    /// <returns>
    /// <see cref="Outcome.Matched"/> with <paramref name="at"/> the position admitted; <see cref="Outcome.Failed"/> when
    /// no position before the subject's end is; <see cref="Outcome.Exceeded"/> at the step ceiling.
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2; IP=None; Security=High; Resources=2; Fingerprint=0D35D4
    // Broiler-Falsified-If: a position is skipped without its units charged, a position whose character the first test admits is skipped, the scan passes the step ceiling, or under u it starts a test inside a surrogate pair it stepped over
    // Broiler-Human:        PENDING
    private Outcome Scan(int test, ref int at)
    {
        var instruction = _code[test];
        while (at < _subject.Length)
        {
            var probe = at;
            if (AtomHolds(instruction, ref probe))
                return Outcome.Matched;
            if (!Afford(instruction.Cost))
                return Outcome.Exceeded;
            at = NextStart(at);
        }

        return Outcome.Failed;
    }

    /// <summary>How one attempt ended.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=4C1744
    // Broiler-Human:        PENDING
    private enum Outcome
    {
        /// <summary>The program accepted.</summary>
        Matched,

        /// <summary>Every frame of the attempt is spent.</summary>
        Failed,

        /// <summary>A ceiling was reached: <see cref="Exceeded"/> names it.</summary>
        Exceeded,
    }

    /// <summary>
    /// One attempt from <paramref name="origin"/>: the loop that charges and dispatches instructions until the program
    /// accepts, every frame of the attempt is spent, or a ceiling is reached. A failed instruction resumes at the newest
    /// frame; a positive look-around's barrier, reached so, fails in its turn and the frame below it is resumed; a marked
    /// frame takes its fast path's next way, or, with none left, fails in its turn too.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=8; Fingerprint=09E9F3
    // Broiler-Falsified-If: an instruction is dispatched before its units are charged, an instruction that fails resumes anywhere but at the newest frame that is not a positive look-around's barrier, a frame resumes with a register write made after it still in force, a frame a look-around's body pushed is resumed after the look-around's close, or a marked frame resumes at its own instruction rather than after the instructions it owns
    // Broiler-Human:        PENDING
    private Outcome MatchFrom(int origin)
    {
        var pc = 0;
        var position = origin;
        while (true)
        {
            var instruction = _code[pc];
            if (!Afford(instruction.Cost))
                return Outcome.Exceeded;

            switch (instruction.Op)
            {
                case RegexOp.Literal:
                case RegexOp.Class:
                case RegexOp.AnyButLineTerminator:
                case RegexOp.Any:
                case RegexOp.LiteralIgnoringCase:
                case RegexOp.ClassIgnoringCase:
                case RegexOp.CanonicalClass:
                    if (AtomHolds(instruction, ref position))
                    {
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.LineStart:
                    if (position == 0 || (instruction.A != 0 && EndsLine(_subject[position - 1])))
                    {
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.LineEnd:
                    if (position == _subject.Length || (instruction.A != 0 && EndsLine(_subject[position])))
                    {
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.WordEdge:
                    if (AtWordEdge(instruction.A, position))
                    {
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.NotWordEdge:
                    if (!AtWordEdge(instruction.A, position))
                    {
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.Backreference:
                case RegexOp.BackreferenceIgnoringCase:
                    switch (MatchReference(instruction, ref position))
                    {
                        case Outcome.Matched:
                            pc++;
                            continue;
                        case Outcome.Exceeded:
                            return Outcome.Exceeded;
                    }

                    break;
                case RegexOp.GreedyRun:
                case RegexOp.LazyRun:
                    switch (EnterRun(instruction, pc, ref position))
                    {
                        case Outcome.Matched:
                            pc += 2;
                            continue;
                        case Outcome.Exceeded:
                            return Outcome.Exceeded;
                    }

                    break;
                case RegexOp.AtomRun:
                    switch (Atoms(instruction, pc, ref position))
                    {
                        case Outcome.Matched:
                            pc += 1 + instruction.A;
                            continue;
                        case Outcome.Exceeded:
                            return Outcome.Exceeded;
                    }

                    break;
                case RegexOp.StringClass:
                {
                    var table = _program.Strings(instruction.A);
                    var member = table.Count;
                    if (NextCharacter(position, table.RightToLeft, out var key, out _))
                        member = table.FirstMember(table.Folded ? Canonical(key) : key);
                    switch (NextWayOfStrings(pc, position, member, out var end))
                    {
                        case Outcome.Matched:
                            position = end;
                            pc += 2;
                            continue;
                        case Outcome.Exceeded:
                            return Outcome.Exceeded;
                    }

                    break;
                }

                case RegexOp.Choice:
                    if (!PushChoicePoint(instruction.B, position))
                        return Outcome.Exceeded;
                    pc = instruction.A;
                    continue;
                case RegexOp.Goto:
                    pc = instruction.A;
                    continue;
                case RegexOp.SavePosition:
                    if (!SetRegister(instruction.A, position))
                        return Outcome.Exceeded;
                    pc++;
                    continue;
                case RegexOp.ClearCaptures:
                    for (var register = instruction.A; register <= instruction.B; register++)
                    {
                        if (!SetRegister(register, -1))
                            return Outcome.Exceeded;
                    }

                    pc++;
                    continue;
                case RegexOp.SetCounter:
                    if (!SetRegister(instruction.A, 0))
                        return Outcome.Exceeded;
                    pc++;
                    continue;
                case RegexOp.IncrementCounter:
                    if (_registers[instruction.A] < int.MaxValue && !SetRegister(instruction.A, _registers[instruction.A] + 1))
                        return Outcome.Exceeded;
                    pc++;
                    continue;
                case RegexOp.ExitAtMaximum:
                    pc = _registers[instruction.A] >= instruction.B ? instruction.C : pc + 1;
                    continue;
                case RegexOp.EnterBelowMinimum:
                    pc = _registers[instruction.A] < instruction.B ? instruction.C : pc + 1;
                    continue;
                case RegexOp.RejectEmptyIteration:
                    if (position == _registers[instruction.A]
                        && (instruction.B < 0 || _registers[instruction.B] >= instruction.C))
                        break;
                    pc++;
                    continue;
                case RegexOp.OpenLookaround:
                    if (!PushChoicePoint(instruction.B != 0 ? NegativeBarrier(instruction.C) : PositiveBarrier, position)
                        || !SetRegister(instruction.A, _framesHeld - 1))
                        return Outcome.Exceeded;
                    pc++;
                    continue;
                case RegexOp.CloseLookaround:
                    var barrier = _registers[instruction.A];
                    _framesHeld = barrier;
                    if (instruction.B != 0)
                        break;
                    position = _frames[(barrier * FrameSize) + 1];
                    pc++;
                    continue;
                case RegexOp.Succeed:
                    _end = position;
                    return Outcome.Matched;
            }

            // The instruction failed: resume at the newest frame, with every write made since it undone. A positive
            // look-around's barrier fails in its turn, as its body found no match; a negative one's resumes after the
            // look-around, which holds. A marked frame takes its fast path's next way, from where its own instructions
            // end, and fails in its turn when it has none.
            while (true)
            {
                if (_framesHeld == 0)
                {
                    UndoAbove(0);
                    return Outcome.Failed;
                }

                _framesHeld--;
                var frame = _framesHeld * FrameSize;
                UndoAbove(_frames[frame + 2]);
                var resume = _frames[frame];
                if (_frames[frame + 3] != Unmarked)
                {
                    var next = NextWay(frame, out var at);
                    if (next == Outcome.Exceeded)
                        return Outcome.Exceeded;
                    if (next == Outcome.Failed)
                        continue;
                    pc = resume + 2;
                    position = at;
                    break;
                }

                if (resume == PositiveBarrier)
                    continue;
                pc = resume >= 0 ? resume : NegativeBarrier(resume);
                position = _frames[frame + 1];
                break;
            }
        }
    }

    /// <summary>
    /// Charges <paramref name="cost"/> units for the work about to be done, as <see cref="MatchFrom"/> charges a dispatch:
    /// the units are spent, and when they pass what the meter has been paid, it is paid, before the work.
    /// </summary>
    /// <returns>False, with nothing spent and <see cref="Exceeded"/> naming the steps, when the step ceiling refuses them; the meter's exception, if it throws, propagates.</returns>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=High; Resources=1; Fingerprint=417A4A
    // Broiler-Falsified-If: the work is done before its units are charged, a refused cost stays spent, or a cost the meter has already been paid for charges it again
    // Broiler-Human:        PENDING
    private bool Afford(int cost)
    {
        _spent += (ulong)cost;
        if (_spent > _threshold && !Pay())
        {
            _spent -= (ulong)cost;
            return false;
        }

        DispatchObserver?.Invoke(_spent);
        return true;
    }

    /// <summary>
    /// Whether the one-character instruction <paramref name="atom"/> holds at <paramref name="position"/>: the test a
    /// <see cref="RegexOp.Literal"/>, <see cref="RegexOp.Class"/>, <see cref="RegexOp.AnyButLineTerminator"/>,
    /// <see cref="RegexOp.Any"/>, <see cref="RegexOp.LiteralIgnoringCase"/>, <see cref="RegexOp.ClassIgnoringCase"/> or
    /// <see cref="RegexOp.CanonicalClass"/> makes of the character it reads next in its direction (CharacterSetMatcher,
    /// ECMA-262 §22.2.2.7.1), whether dispatched or read by a fast path; when it holds, <paramref name="position"/> moves
    /// past the character.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.1; IP=None; Security=High; Resources=1; Fingerprint=83A9CE
    // Broiler-Falsified-If: a character instruction read by a run, an atom run or a class of strings answers other than the same instruction dispatched, or the position moves when the test fails
    // Broiler-Human:        PENDING
    private readonly bool AtomHolds(in RegexInstruction atom, ref int position)
    {
        if (!NextCharacter(position, atom.RightToLeft, out var character, out var step))
            return false;

        var holds = atom.Op switch
        {
            RegexOp.Literal => character == atom.A,
            RegexOp.Class => InClass(atom.A, character),
            RegexOp.AnyButLineTerminator => !EndsLine(character),
            RegexOp.Any => true,
            RegexOp.LiteralIgnoringCase => Canonical(character) == atom.A,
            RegexOp.ClassIgnoringCase => InClassIgnoringCase(atom.A, character) != (atom.B != 0),
            RegexOp.CanonicalClass => InClass(atom.A, Canonical(character)),
            _ => false,
        };
        if (holds)
            position += step;
        return holds;
    }

    /// <summary>
    /// A run's dispatch (<see cref="RegexOp.GreedyRun"/>, <see cref="RegexOp.LazyRun"/>) at <paramref name="address"/>:
    /// its required characters, then, greedy, as many more as its atom admits up to its maximum, or, lazy, none; and its
    /// one marked frame when it has another way: a greedy run that took more than it required, or a lazy one that may take
    /// more. Each character the run examines is charged the atom's cost before it is read.
    /// </summary>
    /// <returns>
    /// <see cref="Outcome.Matched"/>, with <paramref name="position"/> after the characters taken; <see cref="Outcome.Failed"/>
    /// when a required character is missing; <see cref="Outcome.Exceeded"/> at the step or frame ceiling.
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.3.1; IP=None; Security=High; Resources=3; Fingerprint=654F0D
    // Broiler-Falsified-If: a greedy run that reaches the step ceiling while it takes its extra characters answers a match of fewer, a run examines a character it has not charged, a greedy run's frame gives a character back below its required ones, or a lazy run takes a character before it tries its continuation
    // Broiler-Human:        PENDING
    private Outcome EnterRun(in RegexInstruction run, int address, ref int position)
    {
        var atom = _code[address + 1];
        var count = 0;
        for (; count < run.A; count++)
        {
            if (!Afford(atom.Cost))
                return Outcome.Exceeded;
            if (!AtomHolds(atom, ref position))
                return Outcome.Failed;
        }

        if (run.Op == RegexOp.LazyRun)
        {
            if (run.B > run.A && !PushMarkedFrame(address, position, run.B == int.MaxValue ? int.MaxValue : run.B - run.A))
                return Outcome.Exceeded;
            return Outcome.Matched;
        }

        var least = position;
        for (; count < run.B; count++)
        {
            if (!Afford(atom.Cost))
                return Outcome.Exceeded;
            if (!AtomHolds(atom, ref position))
                break;
        }

        return position == least || PushMarkedFrame(address, position, least) ? Outcome.Matched : Outcome.Exceeded;
    }

    /// <summary>
    /// An atom run's dispatch (<see cref="RegexOp.AtomRun"/>) at <paramref name="address"/>: its atoms in order, each in
    /// its own direction, the first paid for by the dispatch and each later one charged its cost before it is read.
    /// </summary>
    /// <returns><see cref="Outcome.Matched"/> past the last atom, <see cref="Outcome.Failed"/> at the first that fails, or <see cref="Outcome.Exceeded"/>.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.3; IP=None; Security=High; Resources=2; Fingerprint=2F9D85
    // Broiler-Falsified-If: an atom run charges units other than its atoms dispatched one by one would, reads an atom after one has failed, or reads a later atom before charging it
    // Broiler-Human:        PENDING
    private Outcome Atoms(in RegexInstruction run, int address, ref int position)
    {
        for (var index = 1; index <= run.A; index++)
        {
            var atom = _code[address + index];
            if (index > 1 && !Afford(atom.Cost))
                return Outcome.Exceeded;
            if (!AtomHolds(atom, ref position))
                return Outcome.Failed;
        }

        return Outcome.Matched;
    }

    /// <summary>
    /// The way a class of strings (<see cref="RegexOp.StringClass"/>) at <paramref name="address"/>, starting at
    /// <paramref name="from"/>, takes next, trying from <paramref name="member"/>: a string of the members kept under the
    /// character there, in their order; at <see cref="RegexStrings.Count"/>, the class test of its single characters; one
    /// past it, the empty string, when the class holds it. The first way that matches is taken, and a marked frame keeps
    /// the way after it when one is left.
    /// </summary>
    /// <returns>
    /// <see cref="Outcome.Matched"/>, with <paramref name="end"/> where the way taken ends; <see cref="Outcome.Failed"/>
    /// when no way is left; <see cref="Outcome.Exceeded"/> at the step or frame ceiling.
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7; IP=None; Security=High; Resources=3; Fingerprint=A53F2C
    // Broiler-Falsified-If: the class tries its single characters before a string that matches, the empty string before its single characters, a string kept under another character than the one at the index, or leaves no frame for a way after the one it takes
    // Broiler-Human:        PENDING
    private Outcome NextWayOfStrings(int address, int from, int member, out int end)
    {
        var table = _program.Strings(_code[address].A);
        var single = table.Count;
        end = from;
        while (member < single)
        {
            var text = table.Text(member);
            member = table.NextMember(member);
            switch (StringHolds(table, text, from, out end))
            {
                case Outcome.Matched:
                    return PushMarkedFrame(address, from, member) ? Outcome.Matched : Outcome.Exceeded;
                case Outcome.Exceeded:
                    return Outcome.Exceeded;
            }
        }

        var empty = _code[address].B != 0;
        if (member == single)
        {
            var characters = _code[address + 1];
            if (!Afford(characters.Cost))
                return Outcome.Exceeded;
            end = from;
            if (AtomHolds(characters, ref end))
                return !empty || PushMarkedFrame(address, from, single + 1) ? Outcome.Matched : Outcome.Exceeded;
        }

        end = from;
        return empty ? Outcome.Matched : Outcome.Failed;
    }

    /// <summary>
    /// Whether <paramref name="text"/>, a string of <paramref name="table"/>, comes next from <paramref name="from"/> in the
    /// table's direction: each of its characters compared with the one the subject holds there, exactly or, folded, by
    /// Canonicalize, as the alternation's <see cref="RegexOp.Literal"/> and <see cref="RegexOp.LiteralIgnoringCase"/> would
    /// compare them; right to left, from its last character. Each character compared costs one unit, charged before it
    /// is read.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7; IP=None; Security=High; Resources=2; Fingerprint=4030D9
    // Broiler-Falsified-If: right to left a string is compared from its first character, a character is read before it is charged, or under u a string's code point is compared with one unit of a surrogate pair
    // Broiler-Human:        PENDING
    private Outcome StringHolds(RegexStrings table, ReadOnlySpan<int> text, int from, out int end)
    {
        end = from;
        for (var index = 0; index < text.Length; index++)
        {
            if (!Afford(1))
                return Outcome.Exceeded;
            var wanted = table.RightToLeft ? text[text.Length - 1 - index] : text[index];
            if (!NextCharacter(end, table.RightToLeft, out var found, out var step) || (table.Folded ? Canonical(found) : found) != wanted)
                return Outcome.Failed;
            end += step;
        }

        return Outcome.Matched;
    }

    /// <summary>
    /// The next way of the marked frame at <paramref name="frame"/>, just popped, with every write since it was pushed
    /// undone: a greedy run's end one character shorter, a lazy run's one character longer, a class of strings' next way.
    /// The frame is kept, at its place, with its position or its mark moved on, while its fast path has a way after this
    /// one.
    /// </summary>
    /// <returns>
    /// <see cref="Outcome.Matched"/>, with <paramref name="position"/> where matching goes on, after the frame's own
    /// instructions; <see cref="Outcome.Failed"/> when it has no way left, which fails in its turn; or
    /// <see cref="Outcome.Exceeded"/>.
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.3.1; IP=None; Security=High; Resources=3; Fingerprint=B7EB27
    // Broiler-Falsified-If: a greedy run gives back two characters at one resumption, or one below its required characters, a lazy run takes more characters than its maximum, or a frame kept for a later way holds the position of the way taken
    // Broiler-Human:        PENDING
    private Outcome NextWay(int frame, out int position)
    {
        var address = _frames[frame];
        var at = _frames[frame + 1];
        var mark = _frames[frame + 3];
        var op = _code[address].Op;
        if (op == RegexOp.StringClass)
            return NextWayOfStrings(address, at, mark, out position);

        var atom = _code[address + 1];
        if (op == RegexOp.GreedyRun)
        {
            position = Shorter(at, mark, atom.RightToLeft);
            if (position != mark)
                KeepFrame(frame, position, mark);
            return Outcome.Matched;
        }

        position = at;
        if (!Afford(atom.Cost))
            return Outcome.Exceeded;
        if (!AtomHolds(atom, ref position))
            return Outcome.Failed;
        if (mark != int.MaxValue)
            mark--;
        if (mark > 0)
            KeepFrame(frame, position, mark);
        return Outcome.Matched;
    }

    /// <summary>
    /// A greedy run's end, <paramref name="end"/>, less the character it took last: the one that ends there, or, right to
    /// left, the one that starts there, read as the run read it, and never past <paramref name="least"/>, where its required
    /// characters end.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.3.1; IP=None; Security=Medium; Resources=0; Fingerprint=4EAF6D
    // Broiler-Falsified-If: under u a run of surrogate pairs gives back one unit of a pair, or a run that began inside a pair the call's start splits gives back past that start
    // Broiler-Human:        PENDING
    private readonly int Shorter(int end, int least, bool rightToLeft)
    {
        if (rightToLeft)
        {
            CharacterAt(end, out var width);
            return Math.Min(end + width, least);
        }

        CharacterBefore(end, out var before);
        return Math.Max(end - before, least);
    }

    /// <summary>Puts the marked frame at <paramref name="frame"/>, just popped, back at its place, from <paramref name="position"/> with <paramref name="mark"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=AED4CB
    // Broiler-Human:        PENDING
    private void KeepFrame(int frame, int position, int mark)
    {
        _frames[frame + 1] = position;
        _frames[frame + 3] = mark;
        _framesHeld++;
    }

    /// <summary>
    /// The resume address of a frame that is a positive look-around's barrier: no address, as a positive look-around
    /// whose body has no way left to match fails.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.6; IP=None; Security=Medium; Resources=0; Fingerprint=8D01FB
    // Broiler-Human:        PENDING
    private const int PositiveBarrier = -1;

    /// <summary>
    /// The resume field of a negative look-around's barrier that resumes at <paramref name="address"/>, -2 - address,
    /// below <see cref="PositiveBarrier"/> and every address; the same map takes the field back to the address.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.6; IP=None; Security=Medium; Resources=0; Fingerprint=E4197B
    // Broiler-Falsified-If: the field of a barrier resuming at address 0 is -1, the positive barrier's, or reads back as another address
    // Broiler-Human:        PENDING
    private static int NegativeBarrier(int address) => -2 - address;

    /// <summary>
    /// Settles the units spent past <see cref="_threshold"/>: refuses them when they pass the step ceiling, and otherwise
    /// charges the meter the whole blocks that cover them, before the instruction they pay for is dispatched.
    /// </summary>
    /// <returns>False when the step ceiling refuses them; the meter's exception, if it throws, propagates.</returns>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=High; Resources=1; Fingerprint=05547C
    // Broiler-Falsified-If: the meter is charged anything but whole blocks, is charged after the work it pays for, or is charged for an instruction the step ceiling refused
    // Broiler-Human:        PENDING
    private bool Pay()
    {
        if (_spent > _stepCeiling)
        {
            Exceeded = RegexLimit.Steps;
            return false;
        }

        var blocks = ((_spent - _paid) + Block - 1) / Block;
        _meter!(blocks * Block);
        _paid += blocks * Block;
        _threshold = Math.Min(_paid, _stepCeiling);
        return true;
    }

    /// <summary>
    /// BackreferenceMatcher (ECMA-262 §22.2.2.7.2) at <paramref name="position"/>: the characters of the first group of
    /// the instruction's list whose capture is defined, compared one by one with the input's, exactly or by Canonicalize;
    /// the empty string when no group of the list has a capture. Right to left, the input's characters are those that end
    /// at the position, compared from the capture's last character to its first, which pairs the same characters as
    /// the specification's comparison of Input[f] to Input[e - 1] with the capture, f being e - len. Before the characters
    /// are compared, the meter is charged the capture's length in code units, the most characters the comparison reads
    /// (decision D8's "a back-reference's length").
    /// </summary>
    /// <returns>
    /// <see cref="Outcome.Matched"/>, with <paramref name="position"/> past the characters matched, in the instruction's direction;
    /// <see cref="Outcome.Failed"/>; or <see cref="Outcome.Exceeded"/>, when the length passes the step ceiling.
    /// </returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.2; IP=None; Security=High; Resources=2; Fingerprint=6AA99F
    // Broiler-Falsified-If: a reference to a group that did not participate fails, a capture's characters are compared before its length is charged, under u a captured lone high surrogate matches the pair it would begin, or right to left the reference moves the index other than to where the characters it matched start
    // Broiler-Human:        PENDING
    private Outcome MatchReference(in RegexInstruction instruction, ref int position)
    {
        int from = -1, to = -1;
        foreach (var group in _program.ReferencedGroups(instruction.A, instruction.B))
        {
            if (_registers[2 * group] >= 0 && _registers[(2 * group) + 1] >= 0)
            {
                (from, to) = (_registers[2 * group], _registers[(2 * group) + 1]);
                break;
            }
        }

        if (to <= from)
            return Outcome.Matched;

        if (!Afford(to - from))
            return Outcome.Exceeded;

        var ignoreCase = instruction.Op == RegexOp.BackreferenceIgnoringCase;
        var rightToLeft = instruction.RightToLeft;
        var at = position;
        for (var read = rightToLeft ? to : from; rightToLeft ? read > from : read < to;)
        {
            if (!NextCharacter(at, rightToLeft, out var found, out var foundStep))
                return Outcome.Failed;
            NextCharacter(read, rightToLeft, out var wanted, out var wantedStep);
            if (wanted != found && !(ignoreCase && Canonical(wanted) == Canonical(found)))
                return Outcome.Failed;
            read += wantedStep;
            at += foundStep;
        }

        position = at;
        return Outcome.Matched;
    }

    /// <summary>
    /// Reads the character an instruction meets next from <paramref name="position"/>: the one that starts there, or,
    /// <paramref name="rightToLeft"/>, the one that ends there; <paramref name="step"/> is what the index moves by to pass
    /// it, its width, negated right to left.
    /// </summary>
    /// <returns>False at the subject's end, or at its start right to left, where there is no character to read.</returns>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.1; IP=None; Security=Medium; Resources=0; Fingerprint=E19704
    // Broiler-Falsified-If: a read right to left from index 0, or left to right from the subject's length, answers a character, or under u a read right to left from after a surrogate pair moves the index by one
    // Broiler-Human:        PENDING
    private readonly bool NextCharacter(int position, bool rightToLeft, out int character, out int step)
    {
        if (rightToLeft ? position == 0 : position == _subject.Length)
        {
            character = step = 0;
            return false;
        }

        if (rightToLeft)
        {
            character = CharacterBefore(position, out var width);
            step = -width;
        }
        else
        {
            character = CharacterAt(position, out step);
        }

        return true;
    }

    /// <summary>
    /// The character at <paramref name="position"/>: a code unit, or under <c>u</c> and <c>v</c> a surrogate pair read as
    /// one code point, unless the call's start splits it (<see cref="_splitAt"/>).
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=D08EE3
    // Broiler-Falsified-If: under u a high surrogate at the subject's last index reads past its end, without u a surrogate pair is read as one code point, or a pair the start splits is read as one
    // Broiler-Human:        PENDING
    private readonly int CharacterAt(int position, out int width)
    {
        var unit = _subject[position];
        if (_unicode && char.IsHighSurrogate(unit) && position + 1 < _subject.Length && char.IsLowSurrogate(_subject[position + 1])
            && position + 1 != _splitAt)
        {
            width = 2;
            return 0x10000 + ((unit - 0xD800) << 10) + (_subject[position + 1] - 0xDC00);
        }

        width = 1;
        return unit;
    }

    /// <summary>
    /// The character that ends at <paramref name="position"/>, the one a read leftwards from it meets: a code unit, or
    /// under <c>u</c> and <c>v</c> a surrogate pair read as one code point, unless the call's start splits it
    /// (<see cref="_splitAt"/>).
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=138731
    // Broiler-Falsified-If: under u a low surrogate at index 0 reads before the subject, without u a surrogate pair ending at the position is read as one code point, or a pair the start splits is read as one
    // Broiler-Human:        PENDING
    private readonly int CharacterBefore(int position, out int width)
    {
        var unit = _subject[position - 1];
        if (_unicode && char.IsLowSurrogate(unit) && position >= 2 && char.IsHighSurrogate(_subject[position - 2])
            && position - 1 != _splitAt)
        {
            width = 2;
            return 0x10000 + ((_subject[position - 2] - 0xD800) << 10) + (unit - 0xDC00);
        }

        width = 1;
        return unit;
    }

    /// <summary>
    /// Whether <paramref name="position"/> is a word boundary (ECMA-262 §22.2.2.6, Assertion :: <c>\b</c>): IsWordChar of
    /// the character before it and of the character at it differ (§22.2.2.6.1), the word characters being class
    /// <paramref name="words"/>.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.6; IP=None; Security=Medium; Resources=1; Fingerprint=8F2123
    // Broiler-Falsified-If: the empty subject's index 0 is reported a boundary, or the index between a and _ is
    // Broiler-Human:        PENDING
    private readonly bool AtWordEdge(int words, int position)
    {
        var before = position > 0 && InClass(words, CharacterBefore(position, out _));
        var after = position < _subject.Length && InClass(words, CharacterAt(position, out _));
        return before != after;
    }

    /// <summary>AdvanceStringIndex (ECMA-262 §22.2.7.3): the next start position, past a whole surrogate pair under <c>u</c> and <c>v</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.3; IP=None; Security=Low; Resources=0; Fingerprint=F83BCE
    // Broiler-Human:        PENDING
    private readonly int NextStart(int position)
    {
        if (position + 1 >= _subject.Length || !_unicode)
            return position + 1;
        CharacterAt(position, out var width);
        return position + width;
    }

    /// <summary>Whether class <paramref name="index"/> holds <paramref name="codePoint"/>: a binary search over its ranges.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=DF3A25
    // Broiler-Falsified-If: a code point equal to a range's low or high bound is reported absent, or the search reads more ranges than its class's weight plus one
    // Broiler-Human:        PENDING
    private readonly bool InClass(int index, int codePoint)
    {
        var ranges = _program.ClassRanges(index);
        var low = 0;
        var high = (ranges.Length / 2) - 1;
        while (low <= high)
        {
            var middle = (low + high) >> 1;
            if (codePoint < ranges[2 * middle])
                high = middle - 1;
            else if (codePoint > ranges[(2 * middle) + 1])
                low = middle + 1;
            else
                return true;
        }

        return false;
    }

    /// <summary>
    /// Canonicalize (ECMA-262 §22.2.2.7.3) of <paramref name="character"/>, read from the Unicode owner's case folding:
    /// its simple case folding under <c>u</c> and <c>v</c>, and without them the upper case of one code unit, kept when
    /// it is not one code unit or would move a character outside ASCII into it.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.3; IP=None; Security=Medium; Resources=0; Fingerprint=AB7FCA
    // Broiler-Falsified-If: without u, U+017F LATIN SMALL LETTER LONG S canonicalizes to S, or under u it does not canonicalize to s
    // Broiler-Human:        PENDING
    private readonly int Canonical(int character) => _unicode ? _folding!.SimpleFold(character) : _folding!.SimpleUpper(character);

    /// <summary>
    /// Whether class <paramref name="index"/> holds a member whose Canonicalize is that of <paramref name="character"/>:
    /// CharacterSetMatcher's test under <c>i</c> (ECMA-262 §22.2.2.7.1), made by testing each code point of the
    /// character's case orbit, which the Unicode owner writes, every code point sharing its Canonicalize, itself included.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.1; IP=None; Security=Medium; Resources=1; Fingerprint=C0C750
    // Broiler-Falsified-If: under u a class holding only U+212A KELVIN SIGN is reported not to hold k, or without u a class holding only s is reported to hold U+017F
    // Broiler-Human:        PENDING
    private readonly bool InClassIgnoringCase(int index, int character)
    {
        var count = _unicode ? _folding!.Orbit(character, _orbit) : _folding!.UpperOrbit(character, _orbit);
        for (var member = 0; member < count; member++)
        {
            if (InClass(index, _orbit[member]))
                return true;
        }

        return false;
    }

    /// <summary>ECMA-262's LineTerminator: LF, CR, U+2028 and U+2029.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12.3; IP=None; Security=Low; Resources=0; Fingerprint=D4877B
    // Broiler-Falsified-If: U+0085 NEXT LINE is reported as a line terminator
    // Broiler-Human:        PENDING
    private static bool EndsLine(int codePoint) => codePoint is 0x0A or 0x0D or 0x2028 or 0x2029;

    /// <summary>Pushes a frame that resumes at <paramref name="resume"/> from <paramref name="position"/>, unless the frame ceiling is reached.</summary>
    /// <returns>False, with <see cref="Exceeded"/> set, when the call already holds as many frames as its ceiling admits.</returns>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=4; Fingerprint=7464A8
    // Broiler-Falsified-If: a call holds one frame more than its frame ceiling, or answers LimitExceeded holding fewer
    // Broiler-Human:        PENDING
    private bool PushChoicePoint(int resume, int position) => PushMarkedFrame(resume, position, Unmarked);

    /// <summary>
    /// Pushes a frame for <paramref name="address"/> from <paramref name="position"/> with <paramref name="mark"/>: a
    /// fast path's marked frame, or, with <see cref="Unmarked"/>, a choice's or a barrier's, unless the frame ceiling is
    /// reached.
    /// </summary>
    /// <returns>False, with <see cref="Exceeded"/> set, when the call already holds as many frames as its ceiling admits.</returns>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=4; Fingerprint=55BDD9
    // Broiler-Falsified-If: a call holds one frame more than its frame ceiling, answers LimitExceeded holding fewer, or a choice's frame is pushed with a mark
    // Broiler-Human:        PENDING
    private bool PushMarkedFrame(int address, int position, int mark)
    {
        if (_framesHeld == _frameCeiling)
        {
            Exceeded = RegexLimit.Frames;
            return false;
        }

        var frame = _framesHeld * FrameSize;
        if (frame + FrameSize > _frames.Length)
            _frames = Grow(_frames, frame, (int)Math.Min((long)_frames.Length * 2, (long)_frameCeiling * FrameSize));
        _frames[frame] = address;
        _frames[frame + 1] = position;
        _frames[frame + 2] = _cellsHeld;
        _frames[frame + 3] = mark;
        _framesHeld++;
        return true;
    }

    /// <summary>Writes <paramref name="value"/> into register <paramref name="register"/>, keeping what it held on the trail, unless the trail ceiling is reached.</summary>
    /// <returns>False, with <see cref="Exceeded"/> set, when the write changes the register and the trail already holds as many cells as its ceiling admits.</returns>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=4; Fingerprint=46CC5B
    // Broiler-Falsified-If: a write that changes a register leaves no trail cell, so backtracking keeps the value of a path that failed, or the trail holds one cell more than its ceiling
    // Broiler-Human:        PENDING
    private bool SetRegister(int register, int value)
    {
        var held = _registers[register];
        if (held == value)
            return true;
        if (_cellsHeld == _trailCeiling)
        {
            Exceeded = RegexLimit.Trail;
            return false;
        }

        var cell = _cellsHeld * CellSize;
        if (cell + CellSize > _trail.Length)
            _trail = Grow(_trail, cell, (int)Math.Min((long)_trail.Length * 2, (long)_trailCeiling * CellSize));
        _trail[cell] = register;
        _trail[cell + 1] = held;
        _cellsHeld++;
        _registers[register] = value;
        return true;
    }

    /// <summary>An array of at least <paramref name="length"/> holding the first <paramref name="used"/> integers of <paramref name="array"/>, which is given back.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=3; Fingerprint=7F7A40
    // Broiler-Falsified-If: a frame or a trail cell held before the growth reads back differently after it
    // Broiler-Human:        PENDING
    private static int[] Grow(int[] array, int used, int length)
    {
        var grown = length <= PooledLength ? ArrayPool<int>.Shared.Rent(length) : GC.AllocateUninitializedArray<int>(length);
        array.AsSpan(0, used).CopyTo(grown);
        ReturnToPool(array);
        return grown;
    }

    /// <summary>Gives a frame or trail array back to the shared pool, unless it is longer than <see cref="PooledLength"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=2; Fingerprint=C6B65F
    // Broiler-Falsified-If: an array longer than PooledLength is returned to the shared pool, or one of the pool's own is not
    // Broiler-Human:        PENDING
    private static void ReturnToPool(int[] array)
    {
        if (array.Length <= PooledLength)
            ArrayPool<int>.Shared.Return(array);
    }

    /// <summary>Undoes every register write above trail height <paramref name="height"/>, newest first.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=688A23
    // Broiler-Human:        PENDING
    private void UndoAbove(int height)
    {
        for (var cell = (_cellsHeld - 1) * CellSize; cell >= height * CellSize; cell -= CellSize)
            _registers[_trail[cell]] = _trail[cell + 1];
        _cellsHeld = Math.Min(_cellsHeld, height);
    }

    /// <summary>Writes the match's offsets: group 0 from <paramref name="origin"/> to the end, then each group's registers.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=FE7591
    // Broiler-Human:        PENDING
    private readonly void WriteSlots(int origin, Span<int> slots)
    {
        slots[0] = origin;
        slots[1] = _end;
        _registers.AsSpan(2, _program.SlotCount - 2).CopyTo(slots[2..]);
    }
}
