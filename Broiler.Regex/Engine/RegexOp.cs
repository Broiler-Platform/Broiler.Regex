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
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=Medium; Resources=0; Fingerprint=4CC3A6
// Broiler-Human:        PENDING
internal enum RegexOp : byte
{
    /// <summary>
    /// Consume one character equal to the code point <c>A</c> (§22.2.2.7.1 CharacterSetMatcher over a one-member set,
    /// without case folding): a code unit outside <c>u</c> and <c>v</c>, and a code point, a surrogate pair read as one,
    /// under them.
    /// </summary>
    Char = 0,

    /// <summary>Consume one character the class numbered <c>A</c> holds (§22.2.2.7.1 CharacterSetMatcher).</summary>
    Class = 1,

    /// <summary>Consume one character that is not a line terminator: <c>.</c> without <c>s</c> (§22.2.2.7, Atom :: <c>.</c>).</summary>
    AnyButLineTerminator = 2,

    /// <summary>Consume one character, whatever it is: <c>.</c> under <c>s</c>.</summary>
    Any = 3,

    /// <summary>
    /// Continue at <c>A</c>, and leave a backtrack point that resumes at <c>B</c> from the same position and the same
    /// registers: the order in which §22.2.2.3 tries an alternation's alternatives and a quantifier's two choices.
    /// </summary>
    Split = 4,

    /// <summary>Continue at <c>A</c>.</summary>
    Jump = 5,

    /// <summary>Write the current position into register <c>A</c>: a capture's start or end, or a quantifier iteration's start.</summary>
    SavePosition = 6,

    /// <summary>
    /// Write "did not participate" (-1) into registers <c>A</c> to <c>B</c>: the captures of a quantified atom, cleared at
    /// the start of each iteration (§22.2.2.3.1 RepeatMatcher, steps 3 and 4).
    /// </summary>
    ClearCaptures = 7,

    /// <summary>Write zero into register <c>A</c>: a counted quantifier's iteration count.</summary>
    SetCounter = 8,

    /// <summary>Add one to register <c>A</c>, stopping at <see cref="int.MaxValue"/>.</summary>
    IncrementCounter = 9,

    /// <summary>Continue at <c>C</c> when register <c>A</c> is at least <c>B</c>, and at the next instruction otherwise: the quantifier's maximum.</summary>
    JumpIfAtLeast = 10,

    /// <summary>Continue at <c>C</c> when register <c>A</c> is below <c>B</c>, and at the next instruction otherwise: the quantifier's minimum.</summary>
    JumpIfBelow = 11,

    /// <summary>
    /// Fail when the iteration that is ending consumed nothing and was not a required one (§22.2.2.3.1 RepeatMatcher, the
    /// continuation's step a): the position equals register <c>A</c>, the iteration's start, and either <c>B</c> is
    /// negative (every iteration of <c>?</c> and <c>*</c> is optional) or register <c>B</c>, the iterations completed before
    /// this one, is at least <c>C</c>, the minimum.
    /// </summary>
    EmptyCheck = 12,

    /// <summary>The pattern has matched, from the attempt's start to the current position.</summary>
    Accept = 13,
}
