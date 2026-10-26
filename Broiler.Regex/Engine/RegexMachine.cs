// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   14
// Annotated:        14/14
// Exempt:           10
// Human-reviewed:   0/14
// IP risk:          None
// Security risk:    High
// Criteria:         7/3
// Resource impact:  8/10 max
// Unverified:       14
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

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
/// choice was made; a write that does not change its register records nothing. When an attempt at one start position
/// fails, every write of the attempt has been undone, so the next position starts from registers that all read -1, with
/// no reset of its own.
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

    private readonly RegexProgram _program;
    private readonly ReadOnlySpan<RegexInstruction> _code;
    private readonly ReadOnlySpan<char> _subject;
    private readonly bool _unicode;

    /// <summary>The registers: the capture registers, then the quantifiers' loop registers.</summary>
    private readonly int[] _registers;

    /// <summary>The backtrack frames, <see cref="FrameSize"/> integers each, and how many are held.</summary>
    private int[] _frames;

    private int _frameTop;

    /// <summary>The undo trail, <see cref="CellSize"/> integers a cell, and how many cells are held.</summary>
    private int[] _trail;

    private int _trailTop;

    /// <summary>Where the attempt that matched ended.</summary>
    private int _end;

    /// <summary>A machine for one call of <paramref name="program"/> over <paramref name="subject"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=3; Fingerprint=EE306C
    // Broiler-Human:        PENDING
    internal RegexMachine(RegexProgram program, ReadOnlySpan<char> subject)
    {
        _program = program;
        _code = program.Code;
        _subject = subject;
        _unicode = program.Unicode;
        _registers = new int[program.RegisterCount];
        _registers.AsSpan().Fill(-1);
        _frames = new int[16 * FrameSize];
        _trail = new int[16 * CellSize];
    }

    /// <summary>
    /// Runs the program from <paramref name="start"/>, or at it alone when <paramref name="anchored"/> is set, and writes
    /// the first match's offsets into <paramref name="slots"/>.
    /// </summary>
    /// <remarks>
    /// The search is RegExpBuiltinExec's loop (ECMA-262 §22.2.7.2): an attempt at each position from the start, the next
    /// position by AdvanceStringIndex, so that under <c>u</c> and <c>v</c> a search does not start inside a surrogate
    /// pair it stepped over; the position after the last character is tried too.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2; IP=None; Security=High; Resources=6; Fingerprint=6D1A62
    // Broiler-Falsified-If: an anchored call tries a position after its start, a search skips the position after the last character, or under u a search starts inside a surrogate pair it stepped over
    // Broiler-Human:        PENDING
    internal RegexStatus Run(int start, bool anchored, Span<int> slots)
    {
        var at = start;
        while (true)
        {
            if (Attempt(at))
            {
                WriteSlots(at, slots);
                return RegexStatus.Match;
            }

            if (anchored || at >= _subject.Length)
                return RegexStatus.NoMatch;

            at = Advance(at);
        }
    }

    /// <summary>
    /// One attempt from <paramref name="origin"/>: the loop that dispatches instructions until the program accepts or
    /// every frame of the attempt is spent.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=8; Fingerprint=4DA762
    // Broiler-Falsified-If: an instruction that fails resumes anywhere but at the newest frame, or a frame resumes with a register write made after it still in force
    // Broiler-Human:        PENDING
    private bool Attempt(int origin)
    {
        var pc = 0;
        var position = origin;
        while (true)
        {
            var instruction = _code[pc];
            switch (instruction.Op)
            {
                case RegexOp.Char:
                    if (position < _subject.Length && Read(position, out var width) == instruction.A)
                    {
                        position += width;
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.Class:
                    if (position < _subject.Length && InClass(instruction.A, Read(position, out width)))
                    {
                        position += width;
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.AnyButLineTerminator:
                    if (position < _subject.Length && !IsLineTerminator(Read(position, out width)))
                    {
                        position += width;
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.Any:
                    if (position < _subject.Length)
                    {
                        Read(position, out width);
                        position += width;
                        pc++;
                        continue;
                    }

                    break;
                case RegexOp.Split:
                    PushFrame(instruction.B, position);
                    pc = instruction.A;
                    continue;
                case RegexOp.Jump:
                    pc = instruction.A;
                    continue;
                case RegexOp.SavePosition:
                    Write(instruction.A, position);
                    pc++;
                    continue;
                case RegexOp.ClearCaptures:
                    for (var register = instruction.A; register <= instruction.B; register++)
                        Write(register, -1);
                    pc++;
                    continue;
                case RegexOp.SetCounter:
                    Write(instruction.A, 0);
                    pc++;
                    continue;
                case RegexOp.IncrementCounter:
                    if (_registers[instruction.A] < int.MaxValue)
                        Write(instruction.A, _registers[instruction.A] + 1);
                    pc++;
                    continue;
                case RegexOp.JumpIfAtLeast:
                    pc = _registers[instruction.A] >= instruction.B ? instruction.C : pc + 1;
                    continue;
                case RegexOp.JumpIfBelow:
                    pc = _registers[instruction.A] < instruction.B ? instruction.C : pc + 1;
                    continue;
                case RegexOp.EmptyCheck:
                    if (position == _registers[instruction.A]
                        && (instruction.B < 0 || _registers[instruction.B] >= instruction.C))
                        break;
                    pc++;
                    continue;
                case RegexOp.Accept:
                    _end = position;
                    return true;
            }

            // The instruction failed: resume at the newest frame, with every write made since it undone.
            if (_frameTop == 0)
            {
                Unwind(0);
                return false;
            }

            _frameTop--;
            var frame = _frameTop * FrameSize;
            pc = _frames[frame];
            position = _frames[frame + 1];
            Unwind(_frames[frame + 2]);
        }
    }

    /// <summary>The character at <paramref name="position"/>: a code unit, or under <c>u</c> and <c>v</c> a surrogate pair read as one code point.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=B063C8
    // Broiler-Falsified-If: under u a high surrogate at the subject's last index reads past its end, or without u a surrogate pair is read as one code point
    // Broiler-Human:        PENDING
    private readonly int Read(int position, out int width)
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
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.3; IP=None; Security=Low; Resources=0; Fingerprint=C0EF82
    // Broiler-Human:        PENDING
    private readonly int Advance(int position)
    {
        if (position + 1 >= _subject.Length || !_unicode)
            return position + 1;
        Read(position, out var width);
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

    /// <summary>ECMA-262's LineTerminator: LF, CR, U+2028 and U+2029.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12.3; IP=None; Security=Low; Resources=0; Fingerprint=B85D17
    // Broiler-Falsified-If: U+0085 NEXT LINE is reported as a line terminator
    // Broiler-Human:        PENDING
    private static bool IsLineTerminator(int codePoint) => codePoint is 0x0A or 0x0D or 0x2028 or 0x2029;

    /// <summary>Pushes a frame that resumes at <paramref name="resume"/> from <paramref name="position"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=4; Fingerprint=0C4720
    // Broiler-Human:        PENDING
    private void PushFrame(int resume, int position)
    {
        var frame = _frameTop * FrameSize;
        if (frame + FrameSize > _frames.Length)
            Array.Resize(ref _frames, _frames.Length * 2);
        _frames[frame] = resume;
        _frames[frame + 1] = position;
        _frames[frame + 2] = _trailTop;
        _frameTop++;
    }

    /// <summary>Writes <paramref name="value"/> into register <paramref name="register"/>, keeping what it held on the trail.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=4; Fingerprint=14453F
    // Broiler-Falsified-If: a write that changes a register leaves no trail cell, so backtracking keeps the value of a path that failed
    // Broiler-Human:        PENDING
    private void Write(int register, int value)
    {
        var held = _registers[register];
        if (held == value)
            return;

        var cell = _trailTop * CellSize;
        if (cell + CellSize > _trail.Length)
            Array.Resize(ref _trail, _trail.Length * 2);
        _trail[cell] = register;
        _trail[cell + 1] = held;
        _trailTop++;
        _registers[register] = value;
    }

    /// <summary>Undoes every register write above trail height <paramref name="height"/>, newest first.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=C9B65B
    // Broiler-Human:        PENDING
    private void Unwind(int height)
    {
        while (_trailTop > height)
        {
            _trailTop--;
            _registers[_trail[_trailTop * CellSize]] = _trail[(_trailTop * CellSize) + 1];
        }
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
