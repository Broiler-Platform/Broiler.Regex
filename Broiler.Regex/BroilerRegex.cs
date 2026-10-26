// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           5
// Human-reviewed:   0/9
// IP risk:          Low
// Security risk:    High
// Criteria:         9/9
// Resource impact:  8/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using Broiler.Regex.Matching;
using Broiler.Regex.Parsing;
using Broiler.Regex.Unicode;
using Broiler.Unicode.Algorithms;

namespace Broiler.Regex;

/// <summary>
/// A compiled ECMAScript regular expression. The public entry point of
/// Broiler.Regex: parse a pattern + flags once, then match many inputs.
/// </summary>
/// <remarks>
/// <para>
/// Compilation is reusable and the compiled form is immutable, so a single
/// instance may be matched concurrently from multiple threads (each match
/// attempt allocates its own state and step budget).
/// </para>
/// <para>
/// <b>Unicode data is the caller's</b> (requirement R11, decision D11 of Broiler.VM's JSD-0081): compilation
/// takes the Unicode owner's tables as an <see cref="IUnicodeTables"/> and reads property escapes, case folding,
/// the properties of strings, white space and the identifier classes of group names through the provider's
/// algorithms, <c>Broiler.Unicode.Algorithms</c>. The package carries no table of its own; an engine hands in the
/// tables it embeds, and a test the owner's <c>Broiler.Unicode.Tables</c>.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=1B83F8
// Broiler-Falsified-If: two threads making their first Match calls at the same time on one instance built from [zab] both normalize the class's out-of-order range list in place, and one throws or misses a match that a single thread finds
// Broiler-Human:        PENDING
public sealed class BroilerRegex
{
    /// <summary>
    /// The largest nesting limit a caller may pass to <see cref="Compile"/>, and the limit the members that take none
    /// parse under (convention 5; requirement R4 as decision D6 of Broiler.VM's JSD-0081 reads it).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A limit above what the stack survives is not a limit.</b> The parser recurses, a few frames for each level of
    /// nesting; a limit is only a refusal if the deepest pattern it admits parses and compiles on the smallest stack an
    /// engine compiles on. The adversarial corpus declares that stack: 1 MiB (<c>tests/Adversarial/corpus.tsv</c>,
    /// <c>stack-bytes</c>). The compiler walks the tree on the heap and limits no shape.
    /// </para>
    /// <para>
    /// <b>What is proven at this maximum, on linux-x64.</b> A pattern of every shape measured, nested to this maximum,
    /// parses and compiles on a 1 MiB thread under the JIT, in Release and in Debug, and under Native AOT, and one level
    /// past it is refused with the nesting status, never a stack overflow: decision D6's bound on the parser and the
    /// compiler (<c>NestingMaximumTests</c>; the Native AOT probe's N cases). Matching at this maximum is proven only for
    /// the shapes <c>NestingMaximumTests.MatchedShapes</c> lists: each kind of level alone, and an alternation, a sequence
    /// or an alternation of sequences on each level without a capture. This type's matcher, the continuations of
    /// <c>Matching/</c>, recurses per level and per term of a sequence until task 2.4c routes matching through the new
    /// backend of task 2.4a, which has no CLR recursion (check A-4's matcher half), so a pattern within this maximum
    /// whose levels each hold a capture with a sequence, an alternation, a back-reference, an assertion or a quantifier
    /// can exhaust a 1 MiB stack here, ending the process or throwing <see cref="RegexOverflowException"/>; the README
    /// lists them, and the new backend matches each it runs. win-x64 was not measured; the maximum is proven on linux-x64 only.
    /// </para>
    /// <para>
    /// <b>Measured, with the margin stated.</b> At the review of task 2.3 of SJL-11.2, on linux-x64, a scratch tool
    /// bisected, for each of 43 shapes, the deepest nesting that parses and compiles, and the deepest that matches a
    /// subject reaching the innermost level, on a thread of a 1 MiB stack in a fresh process. A depth passed only when
    /// every run of it passed: under the JIT in Release three runs with the default tiering, one with optimised code
    /// only and one with none; in Debug two; under Native AOT two. A stack overflow, any exception, a
    /// <see cref="RegexOverflowException"/> included, and a wrong answer each failed it. The shapes were each kind of level
    /// alone (capturing groups with and without <c>u</c>, non-capturing, named, look-ahead, negative look-ahead,
    /// look-behind, modifier and repeated groups, and <c>v</c>-mode classes, nested, complemented, intersected,
    /// subtracted and joined) and mixtures of a capture, an alternation, a sequence, a back-reference, an assertion and
    /// a quantified inner group on each level, forward and inside a look-behind. Parsing and compiling, the deepest
    /// depth that passed for the worst shape was 1,164 under the JIT in Release (modifier groups), 738 in Debug
    /// (complemented property classes under <c>vi</c>) and 2,830 under Native AOT (the same): this maximum is the
    /// largest power of two at most half the first, and the limit both engines pass (decision D7), a margin of 2.27 in
    /// Release, 1.44 in Debug and 5.53 under Native AOT. Matching, the worst shape measured, a repeated capture around
    /// an alternation of sequences with a back-reference and an assertion on each level, first failed at 465 levels in
    /// Release, 321 in Debug and 1,463 under Native AOT: below this maximum under the JIT. Every shape was then run at
    /// this maximum, 24 times in Release, 6 in Debug and twice under Native AOT: every one parsed and compiled every
    /// time, and every shape of <c>MatchedShapes</c> matched.
    /// </para>
    /// <para>
    /// <b>A constant, fixed below.</b> A caller compiles the value in. Decision D7 makes 512 the limit both engines pass,
    /// so this maximum is never lowered below 512 in the <c>0.x</c> line: a platform whose stack does not hold it is a
    /// defect of the parser's or the matcher's stack use, not a reason for a lower maximum. A later version may raise it,
    /// which leaves a caller built against this one passing a limit that is still valid.
    /// </para>
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D6; IP=None; Security=High; Resources=0; Fingerprint=953223
    // Broiler-Falsified-If: on linux-x64, a pattern nested to this depth, parsed and compiled on a thread of a 1 MiB stack under the JIT or Native AOT, ends the process with a stack overflow, or a pattern of a shape NestingMaximumTests.MatchedShapes lists, at this depth, does so when it is matched
    // Broiler-Human:        PENDING
    public const int MaximumNestingLimit = 512;

    private readonly Matcher _matcher;

    public string Pattern { get; }
    public RegexFlags Flags { get; }

    /// <summary>Number of capturing groups in the pattern.</summary>
    public int CaptureCount { get; }

    /// <summary>Group name → 1-based capture index.</summary>
    /// <remarks>
    /// Read-only all the way down: the map the matcher reads on every match, behind a view that admits no write through
    /// a cast to <c>IDictionary</c>, so the compiled pattern cannot be changed through it (convention 10).
    /// </remarks>
    public IReadOnlyDictionary<string, int> GroupNames { get; }

    /// <summary>Compiles <paramref name="pattern"/> under the flags string <paramref name="flags"/>.</summary>
    /// <param name="pattern">The pattern's source text.</param>
    /// <param name="flags">The flags, as a <c>RegExp</c> receives them: a string of <c>d g i m s u v y</c>, or null for none.</param>
    /// <param name="tables">The Unicode owner's tables, of the version <c>Broiler.Unicode.Algorithms</c> reads.</param>
    /// <exception cref="RegexSyntaxException">The flags or the pattern are not valid (ECMA-262 §22.2.3.1), or the pattern nests deeper than <see cref="MaximumNestingLimit"/>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="tables"/> is null.</exception>
    /// <exception cref="System.ArgumentException">The tables are of another Unicode version or lack a table the algorithms read.</exception>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=14510D
    // Broiler-Falsified-If: a flags string with a repeated letter such as "gg" produces an instance instead of throwing RegexSyntaxException
    // Broiler-Human:        PENDING
    public BroilerRegex(string pattern, string? flags, IUnicodeTables tables)
        : this(pattern, RegexFlagsParser.Parse(flags), tables)
    {
    }

    /// <summary>Compiles <paramref name="pattern"/> under <paramref name="flags"/>.</summary>
    /// <param name="pattern">The pattern's source text.</param>
    /// <param name="flags">The flags.</param>
    /// <param name="tables">The Unicode owner's tables, of the version <c>Broiler.Unicode.Algorithms</c> reads.</param>
    /// <remarks>
    /// The pattern is parsed and compiled under <see cref="MaximumNestingLimit"/>. A pattern nested deeper is refused as
    /// every pattern this constructor does not compile is, with a <see cref="RegexSyntaxException"/> at the group or
    /// class that opens the level past it; <see cref="Compile"/> takes the caller's own limit and answers a status.
    /// </remarks>
    /// <exception cref="RegexSyntaxException">The pattern is not valid under the flags (ECMA-262 §22.2.1), or nests deeper than <see cref="MaximumNestingLimit"/>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="tables"/> is null.</exception>
    /// <exception cref="System.ArgumentException">The tables are of another Unicode version or lack a table the algorithms read.</exception>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=DFF8D4
    // Broiler-Falsified-If: a pattern of 1,000,000 nested opening parentheses ends the process with a stack overflow instead of throwing RegexSyntaxException
    // Broiler-Human:        PENDING
    public BroilerRegex(string pattern, RegexFlags flags, IUnicodeTables tables)
        : this(pattern ?? "", flags, new RegexUnicode(tables), new RegexNesting(MaximumNestingLimit), refusesAsSyntax: true)
    {
    }

    /// <summary>
    /// Parses and compiles <paramref name="pattern"/>, descending under <paramref name="nesting"/>. A pattern nested
    /// deeper is refused with a <see cref="RegexSyntaxException"/> when <paramref name="refusesAsSyntax"/> is set, and
    /// otherwise with the <see cref="NestingLimitExceeded"/> that <see cref="Compile"/> answers as a status.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=93E41A
    // Broiler-Falsified-If: a pattern the parser accepts under a limit is compiled under another, so the compiler's recursion is bounded by neither
    // Broiler-Human:        PENDING
    private BroilerRegex(string pattern, RegexFlags flags, RegexUnicode data, RegexNesting nesting, bool refusesAsSyntax)
    {
        Pattern = pattern;
        Flags = flags;

        // One view of the provider serves the parse and every later match of this instance.
        try
        {
            var ast = RegexParser.Parse(Pattern, flags, data, nesting, out var captureCount, out var groupNames);
            CaptureCount = captureCount;
            GroupNames = groupNames;
            _matcher = new Matcher(ast, captureCount, flags, groupNames, data, nesting);
        }
        catch (NestingLimitExceeded refusal) when (refusesAsSyntax)
        {
            throw RegexNesting.AsSyntaxError(refusal);
        }
    }

    /// <summary>
    /// Compiles <paramref name="pattern"/> under <paramref name="flags"/>, refusing a pattern nested deeper than
    /// <paramref name="nestingLimit"/> with a status rather than an exception (requirement R4, decisions D6 and D9 of
    /// Broiler.VM's JSD-0081; conventions 3 and 5).
    /// </summary>
    /// <param name="pattern">The pattern's source text.</param>
    /// <param name="flags">The flags.</param>
    /// <param name="tables">The Unicode owner's tables, of the version <c>Broiler.Unicode.Algorithms</c> reads.</param>
    /// <param name="nestingLimit">
    /// The deepest nesting the caller admits, from 1 to <see cref="MaximumNestingLimit"/>, counted as Broiler.VM's
    /// matcher counts it: the pattern's own Disjunction is depth 1; each group of any kind (capturing,
    /// non-capturing, named, look-around, modifier) adds 1; under <c>v</c> each class adds 1, the outermost included.
    /// With 512, the limit both engines pass (decision D7), 511 nested groups compile and 512 are refused.
    /// </param>
    /// <param name="regex">The compiled pattern, or null when the status is not <see cref="RegexStatus.Success"/>.</param>
    /// <param name="limit">
    /// The limit reached when the status is <see cref="RegexStatus.LimitExceeded"/>: <see cref="RegexLimit.Nesting"/>;
    /// otherwise <see cref="RegexLimit.None"/>.
    /// </param>
    /// <returns>
    /// <see cref="RegexStatus.Success"/>, or <see cref="RegexStatus.LimitExceeded"/> when the pattern nests deeper than
    /// <paramref name="nestingLimit"/>. The limit is checked before each descent of the parser and of the compiler, in
    /// the order the parser reads the pattern, so a syntax error the parser meets first is reported first, and a level
    /// past the limit is refused before anything after it is read.
    /// </returns>
    /// <remarks>
    /// <b>A provisional signature.</b> No published version carries this member, and it is replaced, not overloaded,
    /// before <c>0.2.0-preview.1</c> ships: task 2.5 adds the caller's compile meter, a nullable <c>RegexWorkCharge</c>,
    /// as a parameter (convention 4), and decision D9's syntax status carries a code, a message identifier and a
    /// position, for which two out parameters have no room. The plan gives that syntax status to the validate entry
    /// (task 2.8) and names no task that moves this member's syntax error onto it; task 2.9, which fixes the public
    /// surface, holds that open. The shape follows Broiler.Unicode's operations: a status, the result in an out
    /// parameter, the meter a parameter.
    /// </remarks>
    /// <exception cref="RegexSyntaxException">The pattern is not valid under the flags (ECMA-262 §22.2.1): still an exception here, although convention 3 makes it an expected failure (see the remarks).</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="pattern"/> or <paramref name="tables"/> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="nestingLimit"/> is less than 1 or greater than <see cref="MaximumNestingLimit"/>.</exception>
    /// <exception cref="System.ArgumentException">The tables are of another Unicode version or lack a table the algorithms read.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decisions D6, D7 and D9; IP=Low; Security=High; Resources=7; Fingerprint=C53672
    // Broiler-Falsified-If: with a nesting limit of 512, a pattern of 512 nested groups or 512 nested v-mode classes answers Success, one of 511 answers LimitExceeded, a limit above MaximumNestingLimit is accepted, or a pattern of 1,000,000 nested groups ends the process
    // Broiler-Human:        PENDING
    public static RegexStatus Compile(string pattern, RegexFlags flags, IUnicodeTables tables, int nestingLimit,
        out BroilerRegex? regex, out RegexLimit limit)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentOutOfRangeException.ThrowIfLessThan(nestingLimit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nestingLimit, MaximumNestingLimit);

        var data = new RegexUnicode(tables);
        try
        {
            regex = new BroilerRegex(pattern, flags, data, new RegexNesting(nestingLimit), refusesAsSyntax: false);
            limit = RegexLimit.None;
            return RegexStatus.Success;
        }
        catch (NestingLimitExceeded)
        {
            regex = null;
            limit = RegexLimit.Nesting;
            return RegexStatus.LimitExceeded;
        }
    }

    /// <summary>
    /// Finds the first match at or after <paramref name="start"/> (anchored at
    /// exactly <paramref name="start"/> when the <c>y</c> flag is set). Returns
    /// <see cref="RegexMatch.Empty"/> when there is no match.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=EBD5A7
    // Broiler-Falsified-If: Match(input, -1) does not give the result that Match(input, 0) gives, so a negative start is not clamped to 0
    // Broiler-Human:        PENDING
    public RegexMatch Match(string input, int start = 0)
    {
        if (start < 0) start = 0;
        if (start > input.Length)
            return RegexMatch.Empty;
        return _matcher.Run(input, start) ?? RegexMatch.Empty;
    }

    /// <summary>True when the pattern matches anywhere at or after <paramref name="start"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=8; Fingerprint=FC8D95
    // Broiler-Falsified-If: IsMatch(input, start) returns a value other than Match(input, start).Success for the same input and start
    // Broiler-Human:        PENDING
    public bool IsMatch(string input, int start = 0) => Match(input, start).Success;

    /// <summary>
    /// Enumerates all non-overlapping matches, advancing past empty matches by one
    /// code point (the <c>g</c>-flag iteration of §22.2.6.9 / §22.2.6.13).
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=5971A9
    // Broiler-Falsified-If: after an empty match just before a surrogate pair in u or v mode, the next match starts at the pair's low surrogate
    // Broiler-Human:        PENDING
    public IEnumerable<RegexMatch> Matches(string input)
    {
        var pos = 0;
        while (pos <= input.Length)
        {
            var m = _matcher.Run(input, pos);
            if (m == null)
                yield break;

            yield return m;

            var next = m.Index + m.Length;
            if (m.Length == 0)
            {
                next = m.Index + 1;
                if (Flags.IsUnicodeMode() && m.Index < input.Length
                    && char.IsHighSurrogate(input[m.Index])
                    && m.Index + 1 < input.Length && char.IsLowSurrogate(input[m.Index + 1]))
                    next++;
            }
            pos = next;
        }
    }
}
