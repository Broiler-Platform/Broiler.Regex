// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           7
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
/// One instruction of a <see cref="RegexProgram"/>: an operation, its three operands, the direction it reads in and its
/// cost in work units (decision D8 of Broiler.VM's JSD-0081). It is written once, by the compiler, and never changed.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Medium; Resources=0; Fingerprint=4FB6AB
// Broiler-Falsified-If: an instruction's operation, an operand, its cost or its direction reads back other than the value it was made with
// Broiler-Human:        PENDING
internal readonly struct RegexInstruction
{
    /// <summary>An instruction of <paramref name="op"/> with its operands, its cost and its direction.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=CC5D98
    // Broiler-Human:        PENDING
    internal RegexInstruction(RegexOp op, int a, int b, int c, int cost, bool rightToLeft)
    {
        Op = op;
        A = a;
        B = b;
        C = c;
        Cost = cost;
        RightToLeft = rightToLeft;
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
    /// The units the machine charges before it dispatches the instruction: one for the dispatch, as decision D8 counts
    /// it, plus, for a class test, the class's weight (<see cref="RegexProgram.ClassWeight"/>) for each code point the
    /// test searches it for (<see cref="RegexProgram.ClassCost"/>), and for <see cref="RegexOp.WordEdge"/> and
    /// <see cref="RegexOp.NotWordEdge"/> the word class's weight for each of the two characters they test. Every
    /// other instruction, a <see cref="RegexOp.ClearCaptures"/> of any width and a look-around's two included, costs one;
    /// a back-reference charges its capture's length in code units on top when it runs, as that length is not known
    /// before. How the units are charged is <see cref="RegexMachine"/>'s remarks, "Charging".
    /// </summary>
    internal readonly int Cost;

    /// <summary>
    /// Whether the instruction reads right to left: it stands in a look-behind's body, which ECMA-262 compiles with
    /// direction backward (§22.2.2.6, Assertion :: <c>(?&lt;=</c> Disjunction <c>)</c>, step 1). A character or class
    /// instruction then reads the character that ends at the index and moves the index to where it starts, and a
    /// back-reference compares its capture with the characters that end at the index. Every other instruction reads no
    /// direction.
    /// </summary>
    internal readonly bool RightToLeft;
}
