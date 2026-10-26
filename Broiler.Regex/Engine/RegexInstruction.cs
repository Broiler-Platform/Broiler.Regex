// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           6
// Human-reviewed:   0/1
// IP risk:          None
// Security risk:    Medium
// Criteria:         1/0
// Resource impact:  0/10 max
// Unverified:       1
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.Regex.Engine;

/// <summary>
/// One instruction of a <see cref="RegexProgram"/>: an operation, its three operands and its cost in work units
/// (decision D8 of Broiler.VM's JSD-0081). It is written once, by the compiler, and never changed.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Medium; Resources=0; Fingerprint=4FB6AB
// Broiler-Falsified-If: an instruction's operation, an operand or its cost reads back other than the value it was made with
// Broiler-Human:        PENDING
internal readonly struct RegexInstruction
{
    /// <summary>An instruction of <paramref name="op"/> with its operands and its cost.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=867A65
    // Broiler-Human:        PENDING
    internal RegexInstruction(RegexOp op, int a, int b, int c, int cost)
    {
        Op = op;
        A = a;
        B = b;
        C = c;
        Cost = cost;
    }

    /// <summary>What the instruction does.</summary>
    internal readonly RegexOp Op;

    /// <summary>The first operand, as <see cref="RegexOp"/>'s member names it.</summary>
    internal readonly int A;

    /// <summary>The second operand.</summary>
    internal readonly int B;

    /// <summary>The third operand.</summary>
    internal readonly int C;

    /// <summary>
    /// The units the machine charges before it dispatches the instruction: one for the dispatch, plus a class test's
    /// weight for <see cref="RegexOp.Class"/>, plus one for every capture group past the first that
    /// <see cref="RegexOp.ClearCaptures"/> resets (<see cref="RegexProgram"/>'s remarks, "Units"). Always at least one.
    /// </summary>
    internal readonly int Cost;
}
