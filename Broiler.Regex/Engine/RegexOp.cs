// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           14
// Human-reviewed:   0/1
// IP risk:          None
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       1
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.Regex.Engine;

/// <summary>
/// What one instruction of a <see cref="RegexProgram"/> does: the instruction set of the backtracking machine that runs
/// ECMA-262's Pattern semantics (§22.2.2) as a program rather than as a chain of continuations (requirement R4's matcher
/// half, decision D5 of Broiler.VM's JSD-0081).
/// </summary>
/// <remarks>
/// <para>
/// <b>Operands.</b> Every instruction has three integer operands, <c>A</c>, <c>B</c> and <c>C</c>, which its member below
/// names, and a cost in work units (<see cref="RegexInstruction.Cost"/>). A register is an integer cell of the match: the
/// two capture registers of group <c>k</c> are <c>2k</c> and <c>2k + 1</c>, and the registers above them hold the loop
/// state of the program's quantifiers. Every register write goes through the undo trail, so that backtracking restores
/// the registers as the specification's continuation would have seen them.
/// </para>
/// <para>
/// <b>What task 2.4a holds, and what is still to come.</b> Literal characters compared without case folding,
/// <c>.</c> with and without <c>s</c>, character classes, sequence, alternation, greedy and lazy quantifiers with counted
/// bounds and the specification's empty-iteration rule, and capturing and non-capturing groups with a quantified atom's
/// captures cleared at each iteration. The assertions, back-references, look-around in both directions, modifiers,
/// case-insensitive comparison and the strings of a <c>v</c>-mode class are task 2.4b's instructions; the greedy run of a
/// single code point, atom-run folding and the literal-prefix prefilter are task 2.4c's fast paths. The numbers below are
/// not a contract: a program is built and run by the same version of the library.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=Medium; Resources=0; Fingerprint=BEC728
// Broiler-Human:        PENDING
internal enum RegexOp : byte
{
    /// <summary>
    /// The matcher of a pattern character (§22.2.2.7, Atom :: PatternCharacter): CharacterSetMatcher over the one-member
    /// set of code point <c>A</c>, with no case folding. The input's next character must be that code point; under
    /// <c>u</c> and <c>v</c> a character is a code point, a surrogate pair read as one, and otherwise a code unit.
    /// </summary>
    Literal = 0,

    /// <summary>CharacterSetMatcher (§22.2.2.7.1) over class number <c>A</c>: the next character must be one of its code points.</summary>
    Class = 1,

    /// <summary>
    /// The matcher of Atom :: <c>.</c> without <c>s</c> (§22.2.2.7): the next character must be other than a
    /// LineTerminator.
    /// </summary>
    AnyButLineTerminator = 2,

    /// <summary>The matcher of Atom :: <c>.</c> under <c>s</c>: any next character.</summary>
    Any = 3,

    /// <summary>
    /// Two ways on, tried in order, as MatchTwoAlternatives tries an alternation's two sides (§22.2.2.3.3) and
    /// RepeatMatcher its iteration and its continuation (§22.2.2.3.1, steps 9 to 12): matching goes on at <c>A</c>, and only
    /// if every path from there fails does it go on at <c>B</c>, from the same index with the same captures.
    /// </summary>
    Choice = 4,

    /// <summary>Matching goes on at <c>A</c>: the continuation a construct hands to the one after it.</summary>
    Goto = 5,

    /// <summary>
    /// Register <c>A</c> takes the current index: one end of a capture group's range (§22.2.2.7, the continuation of
    /// Atom :: <c>(</c> GroupSpecifier Disjunction <c>)</c>), or the index an iteration of a quantifier begins at.
    /// </summary>
    SavePosition = 6,

    /// <summary>
    /// RepeatMatcher's steps 3 and 4 (§22.2.2.3.1): every capture inside the quantified atom, registers <c>A</c> to
    /// <c>B</c>, is set to undefined (-1) before the atom is matched again.
    /// </summary>
    ClearCaptures = 7,

    /// <summary>A counted quantifier starts: its iteration count, register <c>A</c>, becomes zero.</summary>
    SetCounter = 8,

    /// <summary>
    /// An iteration of a counted quantifier is complete: its count, register <c>A</c>, grows by one, and stays at
    /// <see cref="int.MaxValue"/> once there.
    /// </summary>
    IncrementCounter = 9,

    /// <summary>
    /// RepeatMatcher's step 1 for a quantifier whose remaining maximum has reached zero: once the count in register
    /// <c>A</c> equals the maximum, <c>B</c>, no iteration is tried and matching goes on at <c>C</c>, the continuation;
    /// below it, at the next instruction.
    /// </summary>
    ExitAtMaximum = 10,

    /// <summary>
    /// RepeatMatcher's step 8: while the count in register <c>A</c> is below the minimum, <c>B</c>, the next iteration is
    /// required and is entered at <c>C</c> with no choice left open; from the minimum on, matching goes on at the next
    /// instruction, the choice.
    /// </summary>
    EnterBelowMinimum = 11,

    /// <summary>
    /// The continuation RepeatMatcher builds at step 2, its step b: an iteration that is ending fails when it was optional
    /// and ended at the index it began at. It began at the index in register <c>A</c>; it was optional when <c>B</c> is
    /// negative (every iteration of <c>?</c> and <c>*</c>), or when register <c>B</c>, the iterations completed before it,
    /// is at least <c>C</c>, the minimum.
    /// </summary>
    RejectEmptyIteration = 12,

    /// <summary>The pattern's last continuation: the match succeeds, from the attempt's start index to the current one.</summary>
    Succeed = 13,
}
