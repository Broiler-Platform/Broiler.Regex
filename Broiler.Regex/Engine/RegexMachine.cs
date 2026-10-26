// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   23
// Annotated:        23/23
// Exempt:           25
// Human-reviewed:   0/23
// IP risk:          None
// Security risk:    High
// Criteria:         14/6
// Resource impact:  8/10 max
// Unverified:       23
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
/// <b>A frame</b> is three integers: the address to resume at, the position to resume from, and the trail's height when
/// the frame was pushed. <b>A trail cell</b> is two: the register a write changed and the value it held. Backtracking
/// pops the newest frame and undoes every write above its trail height, so the registers are what they were when the
/// choice was made; a write that does not change its register records nothing, and one that does records a cell even
/// with no frame pending, where the cell serves only the reset below (so the trail ceiling bounds a loop that holds no
/// choice too, <see cref="RegexMatchCeilings"/>'s remarks). When an attempt at one start position fails, every write of
/// the attempt has been undone, so the next position starts from registers that all read -1, with no reset of its own.
/// </para>
/// <para>
/// <b>Charging.</b> Before it dispatches an instruction the machine adds the instruction's cost to the units spent.
/// When that passes the step ceiling, the instruction is not dispatched and the call answers
/// <see cref="RegexStatus.LimitExceeded"/> naming <see cref="RegexLimit.Steps"/>; when it passes what the meter has
/// been charged, the meter is charged the whole blocks of <see cref="Block"/> units that cover it, and only then is the
/// instruction dispatched. So before the k-th unit of work, at least k units have been charged, and the meter is never
/// more than <see cref="Block"/> - 1 units ahead of the work; the tests of check A-1 read both at every dispatch
/// through <see cref="DispatchObserver"/>, which the library never sets. The units of one call run on across all its
/// start positions, with no reset. Undoing a write costs nothing of its own: the instruction that made the write paid
/// for it.
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
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=8; Fingerprint=7D0010
// Broiler-Falsified-If: a match over a subject of any length, or of a pattern of any nesting within the library's maximum, ends the process with a stack overflow, or a failed attempt leaves a register other than -1 for the next start position
// Broiler-Human:        PENDING
internal ref struct RegexMachine
{
    /// <summary>The integers of one frame: the address to resume at, the position, the trail's height.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=08197A
    // Broiler-Human:        PENDING
    private const int FrameSize = 3;

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
    /// 256 KiB. A longer one, which only a call holding more than 21,845 frames or 32,768 trail cells needs, is allocated
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
    /// dispatched and after its units are charged, with the units of the call through that instruction, so that a test
    /// reads what its own meter holds at that moment (bundle <c>sjl-11-001</c>'s check A-1). It is a
    /// <see cref="RegexWorkCharge"/> only so that the loop invokes no delegate type but the meter's.
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
    /// pair it stepped over; the position after the last character is tried too. The start is the caller's, and its
    /// first attempt reads forward from it: under <c>u</c> and <c>v</c> a start inside a pair reads the pair's low half
    /// as a lone unit. RegExpBuiltinExec's step 13.b starts the matcher at the pair instead, and that step back is the
    /// caller's to take before it calls (<see cref="RegexProgram.Match"/>).
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2; IP=None; Security=High; Resources=6; Fingerprint=71ADFA
    // Broiler-Falsified-If: an anchored call tries a position after its start, a search skips the position after the last character, under u a search starts inside a surrogate pair it stepped over, or a start inside a pair reads the unit before it
    // Broiler-Human:        PENDING
    internal RegexStatus Run(int start, bool anchored, Span<int> slots)
    {
        var at = start;
        while (true)
        {
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
    /// accepts, every frame of the attempt is spent, or a ceiling is reached.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=8; Fingerprint=F06560
    // Broiler-Falsified-If: an instruction is dispatched before its units are charged, an instruction that fails resumes anywhere but at the newest frame, or a frame resumes with a register write made after it still in force
    // Broiler-Human:        PENDING
    private Outcome MatchFrom(int origin)
    {
        var pc = 0;
        var position = origin;
        while (true)
        {
            var instruction = _code[pc];
            _spent += (ulong)instruction.Cost;
            if (_spent > _threshold && !Pay())
            {
                _spent -= (ulong)instruction.Cost;
                return Outcome.Exceeded;
            }

            DispatchObserver?.Invoke(_spent);
            switch (instruction.Op)
            {
                case RegexOp.Literal:
                    if (position < _subject.Length && CharacterAt(position, out var width) == instruction.A)
                    {
                        position += width;
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.Class:
                    if (position < _subject.Length && InClass(instruction.A, CharacterAt(position, out width)))
                    {
                        position += width;
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.AnyButLineTerminator:
                    if (position < _subject.Length && !EndsLine(CharacterAt(position, out width)))
                    {
                        position += width;
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.Any:
                    if (position < _subject.Length)
                    {
                        CharacterAt(position, out width);
                        position += width;
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.LiteralIgnoringCase:
                    if (position < _subject.Length && Canonical(CharacterAt(position, out width)) == instruction.A)
                    {
                        position += width;
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.ClassIgnoringCase:
                    if (position < _subject.Length && InClassIgnoringCase(instruction.A, CharacterAt(position, out width)) != (instruction.B != 0))
                    {
                        position += width;
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.CanonicalClass:
                    if (position < _subject.Length && InClass(instruction.A, Canonical(CharacterAt(position, out width))))
                    {
                        position += width;
                        pc++;
                        continue;
                    }

                    break;
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
                case RegexOp.Succeed:
                    _end = position;
                    return Outcome.Matched;
            }

            // The instruction failed: resume at the newest frame, with every write made since it undone.
            if (_framesHeld == 0)
            {
                UndoAbove(0);
                return Outcome.Failed;
            }

            _framesHeld--;
            var frame = _framesHeld * FrameSize;
            pc = _frames[frame];
            position = _frames[frame + 1];
            UndoAbove(_frames[frame + 2]);
        }
    }

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

    /// <summary>The character at <paramref name="position"/>: a code unit, or under <c>u</c> and <c>v</c> a surrogate pair read as one code point.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=A19440
    // Broiler-Falsified-If: under u a high surrogate at the subject's last index reads past its end, or without u a surrogate pair is read as one code point
    // Broiler-Human:        PENDING
    private readonly int CharacterAt(int position, out int width)
    {
        var unit = _subject[position];
        if (_unicode && char.IsHighSurrogate(unit) && position + 1 < _subject.Length && char.IsLowSurrogate(_subject[position + 1]))
        {
            width = 2;
            return 0x10000 + ((unit - 0xD800) << 10) + (_subject[position + 1] - 0xDC00);
        }

        width = 1;
        return unit;
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
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=4; Fingerprint=E0F13C
    // Broiler-Falsified-If: a call holds one frame more than its frame ceiling, or answers LimitExceeded holding fewer
    // Broiler-Human:        PENDING
    private bool PushChoicePoint(int resume, int position)
    {
        if (_framesHeld == _frameCeiling)
        {
            Exceeded = RegexLimit.Frames;
            return false;
        }

        var frame = _framesHeld * FrameSize;
        if (frame + FrameSize > _frames.Length)
            _frames = Grow(_frames, frame, (int)Math.Min((long)_frames.Length * 2, (long)_frameCeiling * FrameSize));
        _frames[frame] = resume;
        _frames[frame + 1] = position;
        _frames[frame + 2] = _cellsHeld;
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
