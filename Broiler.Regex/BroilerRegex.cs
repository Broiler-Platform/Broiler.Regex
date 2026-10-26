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
    /// nesting, and so do the matcher's continuations until task 2.4a replaces them; a limit is only a refusal if the
    /// deepest pattern it admits completes on the smallest stack an engine compiles and matches on. The adversarial
    /// corpus declares that stack: 1 MiB (<c>tests/Adversarial/corpus.tsv</c>, <c>stack-bytes</c>).
    /// </para>
    /// <para>
    /// <b>Measured, with the margin stated.</b> At task 2.3 of SJL-11.2, on linux-x64, a scratch tool compiled one
    /// shape of nesting on a thread of a 1 MiB stack, in a child process, matched it against a subject that reaches its
    /// innermost level, and bisected the depth at which the process ended with a stack overflow. The shapes were each
    /// kind of level alone (capturing, non-capturing, named, look-ahead, look-behind, modifier groups and <c>v</c>-mode
    /// classes, nested, intersected, subtracted and complemented) and the shapes that cost the most stack for one
    /// level: an alternation of sequences, a quantified one, and a sequence in a modifier. The deepest depth that
    /// completed for the worst shape was 1,053 under the JIT in Release (an alternation of sequences, in the
    /// matcher's continuations), 740 under the JIT in Debug (a <c>v</c>-mode class, in the parser) and 2,833 under
    /// Native AOT (a <c>v</c>-mode class). This maximum is the largest power of two at most half the first, and the
    /// limit both engines pass (decision D7): a margin of 2.0 under the JIT in Release, 1.4 in Debug and 5.5 under
    /// Native AOT. Compilation walks the tree on the heap and limits no shape. Frames are larger on win-x64, which was
    /// not measured here.
    /// </para>
    /// <para>
    /// <b>Proven where it is used.</b> A pattern at this maximum compiles and matches on a 1 MiB thread under the JIT
    /// (the test <c>NestingMaximumTests</c> starts its own thread) and in the Native AOT probe, whose nesting cases run
    /// on a thread of that stack; one level past it is refused with the nesting status, never a stack overflow.
    /// </para>
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D6; IP=None; Security=High; Resources=0; Fingerprint=953223
    // Broiler-Falsified-If: a pattern nested to this depth, compiled and matched on a thread of a 1 MiB stack under the JIT or Native AOT, ends the process with a stack overflow
    // Broiler-Human:        PENDING
    public const int MaximumNestingLimit = 512;

    private readonly Matcher _matcher;

    public string Pattern { get; }
    public RegexFlags Flags { get; }

    /// <summary>Number of capturing groups in the pattern.</summary>
    public int CaptureCount { get; }

    /// <summary>Group name → 1-based capture index.</summary>
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
    /// <param name="regex">The compiled pattern, or null when the status is not <see cref="RegexStatus.Ok"/>.</param>
    /// <param name="limit">
    /// The limit reached when the status is <see cref="RegexStatus.LimitExceeded"/>: <see cref="RegexLimit.Nesting"/>;
    /// otherwise <see cref="RegexLimit.None"/>.
    /// </param>
    /// <returns>
    /// <see cref="RegexStatus.Ok"/>, or <see cref="RegexStatus.LimitExceeded"/> when the pattern nests deeper than
    /// <paramref name="nestingLimit"/>. The limit is checked before each descent of the parser and of the compiler, in
    /// the order the parser reads the pattern, so a syntax error the parser meets first is reported first, and a level
    /// past the limit is refused before anything after it is read.
    /// </returns>
    /// <exception cref="RegexSyntaxException">The pattern is not valid under the flags (ECMA-262 §22.2.1); task 2.8 makes this a status.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="pattern"/> or <paramref name="tables"/> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="nestingLimit"/> is less than 1 or greater than <see cref="MaximumNestingLimit"/>.</exception>
    /// <exception cref="System.ArgumentException">The tables are of another Unicode version or lack a table the algorithms read.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decisions D6, D7 and D9; IP=Low; Security=High; Resources=7; Fingerprint=43CEE0
    // Broiler-Falsified-If: with a nesting limit of 512, a pattern of 512 nested groups or 512 nested v-mode classes answers Ok, one of 511 answers LimitExceeded, a limit above MaximumNestingLimit is accepted, or a pattern of 1,000,000 nested groups ends the process
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
            return RegexStatus.Ok;
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
