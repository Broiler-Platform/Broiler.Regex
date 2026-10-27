// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           3
// Human-reviewed:   0/7
// IP risk:          None
// Security risk:    High
// Criteria:         5/1
// Resource impact:  3/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.Regex.Ast;

/// <summary>
/// The strings of a <c>v</c>-mode class-set operand while the parser builds it (ECMA-262 §22.2.1 ClassSetExpression,
/// §22.2.2.9 CompileToCharSet): an insertion-ordered set, a list for the order the strings were met in and a hash set,
/// compared by code unit, for membership.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why it exists (task 2.5 of SJL-11.2, plan row 2.5).</b> The parser kept these strings in a <c>List</c> and asked
/// <c>List.Contains</c> before each addition, so a union of operands holding n strings in all made O(n²) comparisons, an
/// intersection or a difference of operands of a and b strings O(a·b), and a <c>\q{…}</c> of n alternatives O(n²):
/// fifty <c>\p{RGI_Emoji}</c> in one class, 138,000 strings, made about 187 million comparisons that the compile meter
/// could not see. Here adding a string or looking it up is one hash of its code units and, when the hash meets another,
/// one comparison, so a union, an intersection or a difference reads each string of its operands once: its work is in
/// proportion to the strings it touches, each by its length, which is what it charges (<see cref="CompileSite.Strings"/>).
/// The hash set's own order is never read (convention 6): the list gives the order, and the class sorts its strings
/// longest first, by code unit, when it is made, so the order a string was met in never reaches a match.
/// </para>
/// <para>
/// <b>One parse's.</b> An instance belongs to one operand of one parse, on its thread, and each operation answers a new
/// one or adds to its own, which no other value holds.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=High; Resources=3; Fingerprint=BAF455
// Broiler-Falsified-If: a union of two operands keeps a string twice or loses one, an intersection keeps a string one operand lacks, a difference keeps a string of its second operand, or adding or finding a string costs comparisons that grow with the strings already held
// Broiler-Human:        PENDING
internal sealed class ClassStrings
{
    private readonly List<string> _order = [];
    private readonly HashSet<string> _members = new(StringComparer.Ordinal);

    /// <summary>How many strings are held.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=1630F6
    // Broiler-Human:        PENDING
    internal int Count => _order.Count;

    /// <summary>The strings, in the order they were first added. The list is the instance's own; its reader does not change it.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=B08DFC
    // Broiler-Human:        PENDING
    internal IReadOnlyList<string> InOrder => _order;

    /// <summary>
    /// Adds <paramref name="text"/> unless it is held already, charging <paramref name="charge"/> the string's code units,
    /// at least one, before it is hashed.
    /// </summary>
    /// <returns>Whether it was added.</returns>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Medium; Resources=1; Fingerprint=9B842B
    // Broiler-Falsified-If: a string already held is added again, or one not held is not added
    // Broiler-Human:        PENDING
    internal bool Add(string text, CompileCharge charge)
    {
        charge.Charge(CompileSite.Strings, CompileCharge.StringUnits(text));
        if (!_members.Add(text))
            return false;
        _order.Add(text);
        return true;
    }

    /// <summary>Whether <paramref name="text"/> is held, charging <paramref name="charge"/> the string's code units, at least one, before it is hashed.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Medium; Resources=1; Fingerprint=6D5B97
    // Broiler-Human:        PENDING
    internal bool Contains(string text, CompileCharge charge)
    {
        charge.Charge(CompileSite.Strings, CompileCharge.StringUnits(text));
        return _members.Contains(text);
    }

    /// <summary>Adds every string of <paramref name="other"/> this set does not hold, in <paramref name="other"/>'s order: the strings of a union.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=None; Security=Medium; Resources=2; Fingerprint=775DFF
    // Broiler-Falsified-If: the union of {ab, cd} and {cd, ef} is anything but ab, cd, ef in that order
    // Broiler-Human:        PENDING
    internal void UnionWith(ClassStrings other, CompileCharge charge)
    {
        foreach (var text in other._order)
            Add(text, charge);
    }

    /// <summary>The strings of this set that <paramref name="other"/> holds, in this set's order: the strings of an intersection.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=None; Security=Medium; Resources=2; Fingerprint=FB2F29
    // Broiler-Falsified-If: the intersection of {ab, cd} and {cd, ef} is anything but cd
    // Broiler-Human:        PENDING
    internal ClassStrings Intersect(ClassStrings other, CompileCharge charge)
    {
        var result = new ClassStrings();
        foreach (var text in _order)
        {
            if (other.Contains(text, charge))
                result.Add(text, charge);
        }

        return result;
    }

    /// <summary>The strings of this set that <paramref name="other"/> does not hold, in this set's order: the strings of a difference.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=None; Security=Medium; Resources=2; Fingerprint=C7E9CE
    // Broiler-Falsified-If: the difference of {ab, cd} and {cd, ef} is anything but ab
    // Broiler-Human:        PENDING
    internal ClassStrings Except(ClassStrings other, CompileCharge charge)
    {
        var result = new ClassStrings();
        foreach (var text in _order)
        {
            if (!other.Contains(text, charge))
                result.Add(text, charge);
        }

        return result;
    }
}
