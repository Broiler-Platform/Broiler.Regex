// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   4
// Annotated:        4/4
// Exempt:           3
// Human-reviewed:   0/4
// IP risk:          None
// Security risk:    High
// Criteria:         2/2
// Resource impact:  0/10 max
// Unverified:       4
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.Regex.Parsing;

/// <summary>
/// The caller's nesting limit, which the parser, and every walk of the tree it builds before matching, checks before
/// each descent: requirement R4 as decision D6 of Broiler.VM's JSD-0081 reads it, "the parser and the compiler may
/// recurse, bounded by the caller's nesting limit checked before each descent" (convention 5).
/// </summary>
/// <remarks>
/// <para>
/// <b>Depth</b> is counted as Broiler.VM's matcher counts it, so that Broiler.VM, passing its own limit of 512, keeps
/// its verdicts (decision D7; <c>tests/Adversarial/README.md</c>, section 2): the pattern's own Disjunction is depth 1;
/// each group of any kind - capturing, non-capturing, named, look-around or modifier - adds 1 for its Disjunction; and
/// under <c>v</c> each class adds 1, the outermost included. A limit of <c>L</c> admits depth <c>L</c> and refuses
/// <c>L + 1</c>, so with 512, 511 nested groups compile and 512 are refused. A class outside <c>v</c>, a quantifier,
/// a sequence and an alternation add no depth: the parser reads each in a loop, and in the tree each sits under one
/// level a bounded number of nodes deep.
/// </para>
/// <para>
/// <b>Checked before the descent</b>, at every recursive production: <see cref="Descend"/> is called before the
/// parser reads a group's Disjunction or a <c>v</c>-mode class's contents, and before a walk of the tree enters a
/// group's, a look-around's or a modifier's body. Past the limit it throws <see cref="NestingLimitExceeded"/>, which
/// unwinds the parse or the walk and never leaves the library: <see cref="BroilerRegex.Compile"/> answers it as
/// <see cref="RegexStatus.LimitExceeded"/> naming <see cref="RegexLimit.Nesting"/>, and the members that take no
/// limit, which parse under <see cref="BroilerRegex.MaximumNestingLimit"/>, as the
/// <see cref="RegexSyntaxException"/> by which they refuse every pattern they do not compile.
/// </para>
/// <para>
/// <b>The seam for the program compiler.</b> The walk after parsing is task 2.4a's compiler from this tree to an
/// instruction program (<c>Compiling/ProgramCompiler.cs</c>), the only one since task 2.4c deleted the old backend's
/// closure compiler. It walks the tree on the heap, with no recursion, and still enters each group's body through
/// <see cref="Descend"/> before it lowers the tree, so a tree built under another limit is held to the caller's.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D6; IP=None; Security=High; Resources=0; Fingerprint=E3A9A6
// Broiler-Falsified-If: with a limit of 512, a pattern of 511 nested groups is refused, or one of 512 nested groups or 512 nested v-mode classes is accepted
// Broiler-Human:        PENDING
internal readonly struct RegexNesting
{
    /// <summary>A limit of <paramref name="limit"/>, which the caller has checked against the documented range.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=9D559A
    // Broiler-Human:        PENDING
    internal RegexNesting(int limit) => Limit = limit;

    /// <summary>The deepest depth admitted.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=9A0DF3
    // Broiler-Human:        PENDING
    internal int Limit { get; }

    /// <summary>
    /// The depth one level below <paramref name="depth"/>, checked before the descent that opens it.
    /// </summary>
    /// <param name="depth">The depth of the level the descent leaves.</param>
    /// <param name="position">Where the level opens in the pattern, or -1 in a walk of the tree.</param>
    /// <exception cref="NestingLimitExceeded">The level would be deeper than <see cref="Limit"/>.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D6; IP=None; Security=High; Resources=0; Fingerprint=6580D7
    // Broiler-Falsified-If: Descend(Limit - 1, p) throws, or Descend(Limit, p) returns instead of throwing
    // Broiler-Human:        PENDING
    internal int Descend(int depth, int position)
    {
        if (depth >= Limit)
            throw new NestingLimitExceeded(position);
        return depth + 1;
    }

    /// <summary>
    /// The refusal the members that take no limit report for a pattern nested deeper than
    /// <see cref="BroilerRegex.MaximumNestingLimit"/>: they refuse every pattern they do not compile with a
    /// <see cref="RegexSyntaxException"/>, as they refuse every pattern they do not compile.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=67B805
    // Broiler-Human:        PENDING
    internal static RegexSyntaxException AsSyntaxError(NestingLimitExceeded refusal)
        => new("The pattern nests deeper than the library's maximum nesting limit", refusal.Position);
}

/// <summary>
/// A descent past the caller's nesting limit, thrown by <see cref="RegexNesting.Descend"/> to unwind a parse or a walk
/// of the tree. It is the library's own and never leaves it: every public member that parses catches it.
/// </summary>
// Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=9B9FA0
// Broiler-Human:        PENDING
internal sealed class NestingLimitExceeded(int position) : Exception("The pattern nests deeper than the caller's nesting limit.")
{
    /// <summary>Where the refused level opens in the pattern, or -1 when a walk of the tree refused it.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=74547A
    // Broiler-Human:        PENDING
    internal int Position { get; } = position;
}
