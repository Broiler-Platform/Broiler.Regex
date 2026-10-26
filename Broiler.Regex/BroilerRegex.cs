// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   12
// Annotated:        12/12
// Exempt:           6
// Human-reviewed:   0/12
// IP risk:          Low
// Security risk:    High
// Criteria:         11/11
// Resource impact:  8/10 max
// Unverified:       12
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using Broiler.Regex.Compiling;
using Broiler.Regex.Engine;
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
/// instance may be matched concurrently from multiple threads (each call
/// rents its own registers, frames and trail).
/// </para>
/// <para>
/// <b>One backend.</b> Since task 2.4c every member matches through the program of the new backend, and the old one,
/// the continuations of <c>Matching/</c> with their per-start step budget, is deleted: the offset entry,
/// <see cref="Match(ReadOnlySpan{char}, int, bool, Span{int}, RegexWorkCharge?, RegexMatchCeilings, out RegexLimit)"/>,
/// with the caller's meter and ceilings, and the string-bearing members, <see cref="Match(string, int)"/>,
/// <see cref="IsMatch"/> and <see cref="Matches"/>, which decision D12 keeps for their existing consumers, under the
/// policy decision D10 sets for an engine that passes no meter: no meter, no step ceiling, and the engines' frame and
/// trail ceilings (<see cref="RegexMatchCeilings.Engines"/>). A pattern that backtracks catastrophically runs to its
/// answer there; a caller that needs a bound on the work passes a meter or a step ceiling to the offset entry.
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
    /// compiler (<c>NestingMaximumTests</c>; the Native AOT probe's N cases). Matching at this maximum is the new
    /// backend's since task 2.4c, which has no CLR recursion (check A-4's matcher half): every shape measured, those of
    /// <c>NestingMaximumTests.MatchedShapes</c> and of <c>UnprovenShapes</c> alike, matches at this maximum on a 1 MiB
    /// thread, whatever its levels hold. Before task 2.4c the old backend's continuations recursed per level and per
    /// term of a sequence, and the second list's shapes could exhaust a 1 MiB stack. win-x64 was not measured; the
    /// maximum is proven on linux-x64 only.
    /// </para>
    /// <para>
    /// <b>Measured, with the margin stated.</b> At the review of task 2.3 of SJL-11.2, on linux-x64, a scratch tool
    /// bisected, for each of 43 shapes, the deepest nesting that parses and compiles, and the deepest that matches a
    /// subject reaching the innermost level, on a thread of a 1 MiB stack in a fresh process. A depth passed only when
    /// every run of it passed: under the JIT in Release three runs with the default tiering, one with optimised code
    /// only and one with none; in Debug two; under Native AOT two. A stack overflow, any exception, a
    /// <c>RegexOverflowException</c> of the old backend included, and a wrong answer each failed it. The shapes were each kind of level
    /// alone (capturing groups with and without <c>u</c>, non-capturing, named, look-ahead, negative look-ahead,
    /// look-behind, modifier and repeated groups, and <c>v</c>-mode classes, nested, complemented, intersected,
    /// subtracted and joined) and mixtures of a capture, an alternation, a sequence, a back-reference, an assertion and
    /// a quantified inner group on each level, forward and inside a look-behind. Parsing and compiling, the deepest
    /// depth that passed for the worst shape was 1,164 under the JIT in Release (modifier groups), 738 in Debug
    /// (complemented property classes under <c>vi</c>) and 2,830 under Native AOT (the same): this maximum is the
    /// largest power of two at most half the first, and the limit both engines pass (decision D7), a margin of 2.27 in
    /// Release, 1.44 in Debug and 5.53 under Native AOT. Matching on the old backend, the worst shape measured, a repeated capture around
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

    /// <summary>The new backend's program, which every member runs since task 2.4c.</summary>
    private readonly RegexProgram _program;

    public string Pattern { get; }
    public RegexFlags Flags { get; }

    /// <summary>Number of capturing groups in the pattern.</summary>
    public int CaptureCount { get; }

    /// <summary>
    /// How many slots the offset entry writes: two for each group, group 0, the whole match, first, so
    /// 2 × (<see cref="CaptureCount"/> + 1). Provisional, with the entry (task 2.9 fixes the public surface).
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=4C9AE6
    // Broiler-Human:        PENDING
    public int SlotCount => _program.SlotCount;

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
    /// Parses and compiles <paramref name="pattern"/>, descending under <paramref name="nesting"/>, to the new backend's
    /// program. A pattern nested deeper is refused with a
    /// <see cref="RegexSyntaxException"/> when <paramref name="refusesAsSyntax"/> is set, and otherwise with the
    /// <see cref="NestingLimitExceeded"/> that <see cref="Compile"/> answers as a status.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=310E62
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

            // The program compiler's walk descends at fewer nodes than the parse did under the same limit, so it is never
            // refused where the parse was not; were it refused, the refusal is the parse's.
            if (ProgramCompiler.Compile(ast, captureCount, groupNames, flags, data, nesting, out var program) != ProgramCompilation.Compiled)
                throw new NestingLimitExceeded(pattern.Length);
            _program = program!;
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
    /// The offset entry (requirements R7 and R8; bundle <c>sjl-11-001</c>'s checks A-7 and A-8): matches
    /// <paramref name="subject"/> from <paramref name="start"/>, or at <paramref name="start"/> alone when
    /// <paramref name="anchored"/> is set, and writes the match's offsets into the caller's <paramref name="slots"/>,
    /// charging the caller's <paramref name="meter"/> before the work and stopping at the caller's
    /// <paramref name="ceilings"/> (requirements R1 and R3). No string is made: the offsets are the answer, and a
    /// substring is the caller's to take.
    /// </summary>
    /// <param name="subject">The subject, any span of UTF-16 code units.</param>
    /// <param name="start">
    /// Where the match starts, or the search begins: a code-unit index from 0 to the subject's length. Under <c>u</c> and
    /// <c>v</c> a start inside a surrogate pair splits the pair for the whole call: a read forward from the start meets the
    /// pair's low half as a lone unit, and a read backward from it (a look-behind's, <c></c>'s, <c>^</c>'s under
    /// <c>m</c>) its high half as another. This entry takes the start as given. RegExpBuiltinExec (ECMA-262 §22.2.7.2,
    /// step 13.b) runs its matcher from the pair instead, and an engine that answers as it does steps back to the pair's
    /// first unit before it calls, then reports from the offsets written and its own <c>lastIndex</c>: a non-empty match
    /// at the pair at that <c>lastIndex</c> (JSD-0081's row M4), and an empty one where its own answer to decision D41's
    /// implementation-defined case puts it. The string-bearing <see cref="Match(string, int)"/> takes that step itself.
    /// </param>
    /// <param name="anchored">
    /// Whether to try <paramref name="start"/> alone (requirement R7). It is independent of the <c>y</c> flag, which this
    /// entry does not read: the flag is for the caller, as it is for <c>RegExp.prototype.exec</c>, to turn into this
    /// argument.
    /// </param>
    /// <param name="slots">
    /// The caller's slots, at least <see cref="SlotCount"/> of them: a match writes the first <see cref="SlotCount"/>, the
    /// start and end of each group, group 0 first, -1 for a group that did not participate; any other answer, and the
    /// meter's exception, leaves each of them -1. Slots past them are not touched.
    /// </param>
    /// <param name="meter">The caller's meter, charged in blocks before the work it pays for, or null for none (convention 4).</param>
    /// <param name="ceilings">The caller's step, frame and trail ceilings, made by their constructor or <see cref="RegexMatchCeilings.Engines"/>.</param>
    /// <param name="limit">The ceiling reached when the answer is <see cref="RegexStatus.LimitExceeded"/>; otherwise <see cref="RegexLimit.None"/>.</param>
    /// <returns><see cref="RegexStatus.Match"/>, <see cref="RegexStatus.NoMatch"/>, or <see cref="RegexStatus.LimitExceeded"/> naming the ceiling in <paramref name="limit"/>.</returns>
    /// <remarks>
    /// <para>
    /// <b>Provisional.</b> This member is new in task 2.4c of SJL-11.2, with <see cref="RegexWorkCharge"/>,
    /// <see cref="RegexMatchCeilings"/> and <see cref="SlotCount"/> made public for it; task 2.9 fixes the public surface
    /// of <c>0.2.0-preview.1</c>, and its shape may change before that version ships.
    /// </para>
    /// <para>
    /// <b>Memory.</b> The call's registers, frames and trail are arrays rented from the shared array pool and given back
    /// before it returns, its exceptions included; once the pool holds arrays of the sizes the pattern needs, a call
    /// allocates nothing, whatever the capture count (check A-8). The compiled pattern is not changed by a call, so
    /// concurrent calls on one instance, and a call the meter makes while it is charged, each answer as alone.
    /// </para>
    /// <para>
    /// <b>Failures.</b> A broken precondition is an exception (convention 3); reaching a ceiling is a status, never
    /// <see cref="RegexStatus.NoMatch"/> and never a match; the meter's exception reaches the caller unchanged, and the
    /// same instance then answers a new call as a new instance would.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is negative or past the subject's end.</exception>
    /// <exception cref="ArgumentException"><paramref name="slots"/> holds fewer than <see cref="SlotCount"/> slots, or <paramref name="ceilings"/> is the default value.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2; IP=None; Security=High; Resources=8; Fingerprint=DF6AB2
    // Broiler-Falsified-If: /b/y unanchored on "ab" answers other than a match at 1, /b/ anchored at 0 of "ab" answers Match, a call that reaches a ceiling answers Match or NoMatch, or a call through a pattern of 1,000 groups allocates once the pool is warm
    // Broiler-Human:        PENDING
    public RegexStatus Match(ReadOnlySpan<char> subject, int start, bool anchored, Span<int> slots, RegexWorkCharge? meter, RegexMatchCeilings ceilings, out RegexLimit limit) =>
        _program.Match(subject, start, anchored, slots, meter, ceilings, out limit, out _);

    /// <summary>
    /// Finds the first match at or after <paramref name="start"/>, or at <paramref name="start"/> alone when the
    /// <c>y</c> flag is set: RegExpBuiltinExec (ECMA-262 §22.2.7.2) from a <c>lastIndex</c> of
    /// <paramref name="start"/>, with the substrings made. Returns <see cref="RegexMatch.Empty"/> when there is no
    /// match, and a failed match whose <see cref="RegexMatch.Limit"/> names the ceiling when the match reached one.
    /// </summary>
    /// <param name="input">The subject.</param>
    /// <param name="start">The <c>lastIndex</c>: a negative one is read as 0, and one past the end answers no match.</param>
    /// <remarks>
    /// <para>
    /// <b>The work.</b> No meter and no step ceiling, and the engines' frame and trail ceilings: decision D10's policy
    /// for an engine that passes no meter (the type's remarks). A match that reaches a frame or trail ceiling answers
    /// a <see cref="RegexMatch"/> whose <see cref="RegexMatch.Success"/> is false and whose
    /// <see cref="RegexMatch.Limit"/> names the ceiling, never no match: exhaustion is its own answer (requirement R1).
    /// </para>
    /// <para>
    /// <b>A start inside a surrogate pair</b> under <c>u</c> and <c>v</c> (JSD-0081's row M4): the matcher runs from the
    /// pair, as step 13.b says, and a match found there that is not empty reports <paramref name="start"/> as its
    /// <see cref="RegexMatch.Index"/> and its substring from there (steps 22 and 24), as decision D23 requires of an
    /// engine's adapter; its captures keep the offsets the matcher wrote. An empty match found at the pair ends before
    /// <paramref name="start"/>, where the text's record of it is ill-formed: decision D41 classes the case
    /// implementation-defined, and this library reports it at the pair's first unit, where it was found, an empty
    /// match whose captures agree with it (Broiler.VM's realm's answer; Broiler.JS answers <paramref name="start"/>).
    /// The offset entry takes the start as given and leaves that step to its caller.
    /// </para>
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2 and JSD-0081 decision D41; IP=Low; Security=High; Resources=8; Fingerprint=CCA438
    // Broiler-Falsified-If: Match(input, -1) does not give the result that Match(input, 0) gives, under u /[\s\S]*\u{1F600}/ from index 1 of "\u{1F600}ba" answers other than a match at 1 ending at 2, /(?:)/u from index 1 of "\u{1F600}" answers other than an empty match at 0, or a match that reaches a ceiling answers RegexMatch.Empty or throws
    // Broiler-Human:        PENDING
    public RegexMatch Match(string input, int start = 0)
    {
        if (start < 0) start = 0;
        if (start > input.Length)
            return RegexMatch.Empty;
        return Run(_program, input, start, out var limit) ?? (limit == RegexLimit.None ? RegexMatch.Empty : RegexMatch.Exceeded(limit));
    }

    /// <summary>True when the pattern matches at or after <paramref name="start"/>, as <see cref="Match(string, int)"/> finds it.</summary>
    /// <remarks>
    /// A <see langword="bool"/> has no room for a third answer, so a match that reaches a frame or trail ceiling, which
    /// <see cref="Match(string, int)"/> answers with <see cref="RegexMatch.Limit"/>, is an
    /// <see cref="InvalidOperationException"/> here rather than a false that would read as no match (requirement R1).
    /// A caller that expects to meet a ceiling calls <see cref="Match(string, int)"/> or the offset entry instead.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The match reached the engines' frame or trail ceiling.</exception>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=8; Fingerprint=494A37
    // Broiler-Falsified-If: IsMatch(input, start) returns a value other than Match(input, start).Success for the same input and start, or returns false for a match that reached a ceiling
    // Broiler-Human:        PENDING
    public bool IsMatch(string input, int start = 0)
    {
        var match = Match(input, start);
        return match.Limit == RegexLimit.None ? match.Success : throw Exhausted(match.Limit);
    }

    /// <summary>
    /// Enumerates all non-overlapping matches, advancing past empty matches by one
    /// code point (the <c>g</c>-flag iteration of §22.2.6.9 / §22.2.6.13).
    /// </summary>
    /// <remarks>
    /// Each match is <see cref="Match(string, int)"/>'s from where the last ended. A match that reaches a frame or trail
    /// ceiling ends the enumeration with an <see cref="InvalidOperationException"/>, after the matches before it, rather
    /// than with a failed match a caller would read as the end (requirement R1).
    /// </remarks>
    /// <exception cref="InvalidOperationException">A match reached the engines' frame or trail ceiling.</exception>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=705430
    // Broiler-Falsified-If: after an empty match just before a surrogate pair in u or v mode, the next match starts at the pair's low surrogate, or the enumeration ends quietly at a match that reached a ceiling
    // Broiler-Human:        PENDING
    public IEnumerable<RegexMatch> Matches(string input)
    {
        var pos = 0;
        while (pos <= input.Length)
        {
            var m = Run(_program, input, pos, out var limit);
            if (m == null)
            {
                if (limit != RegexLimit.None)
                    throw Exhausted(limit);
                yield break;
            }

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

    /// <summary>The exception <see cref="IsMatch"/> and <see cref="Matches"/> throw for a match that reached <paramref name="limit"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=AF14E6
    // Broiler-Human:        PENDING
    private static InvalidOperationException Exhausted(RegexLimit limit) =>
        new("The match reached the engines' " + (limit == RegexLimit.Frames ? "frame" : "trail") + " ceiling (RegexLimit." + limit
            + "); Match(string, int) answers it as RegexMatch.Limit, and the offset entry as a status.");

    /// <summary>
    /// RegExpBuiltinExec (ECMA-262 §22.2.7.2) over the program, with no meter and the engines' ceilings: under <c>u</c>
    /// and <c>v</c> a start inside a surrogate pair steps back to the pair (step 13.b), a non-empty match found there
    /// reports <paramref name="start"/> as its index (row M4), and an empty one the pair's start (decision D41,
    /// <see cref="Match(string, int)"/>'s remarks); null for no match or a ceiling, which <paramref name="limit"/> names.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2 and JSD-0081 decision D41; IP=None; Security=High; Resources=8; Fingerprint=0E6946
    // Broiler-Falsified-If: under u, /[\s\S]*\u{1F600}/ from index 1 of "\u{1F600}ba" answers other than a match at 1 ending at 2, /(?:)/u from index 1 of "\u{1F600}" answers other than an empty match at 0, a sticky instance finds a match after its start, or a ceiling reached answers null with no limit
    // Broiler-Human:        PENDING
    private RegexMatch? Run(RegexProgram program, string input, int start, out RegexLimit limit)
    {
        var from = start;
        if (program.Unicode && from > 0 && from < input.Length && char.IsLowSurrogate(input[from]) && char.IsHighSurrogate(input[from - 1]))
            from--;

        var slots = new int[program.SlotCount];
        var status = program.Match(input, from, (Flags & RegexFlags.Sticky) != 0, slots, null, RegexMatchCeilings.Engines, out limit, out _);
        if (status != RegexStatus.Match)
            return null;

        // A match at the pair the start stepped back to reports the caller's start, unless it ends before it: an empty
        // one there, decision D41's case, is reported where it was found.
        var end = slots[1];
        var index = slots[0] == from && end >= start ? start : slots[0];
        var value = input[index..end];
        var groups = new RegexGroup[CaptureCount + 1];
        groups[0] = new RegexGroup(true, index, end - index, value, null);
        var names = new string?[CaptureCount + 1];
        foreach (var (name, number) in GroupNames)
            names[number] = name;

        var named = new Dictionary<string, RegexGroup>(StringComparer.Ordinal);
        for (var group = 1; group <= CaptureCount; group++)
        {
            var (first, last) = (slots[2 * group], slots[(2 * group) + 1]);
            groups[group] = first >= 0 && last >= first
                ? new RegexGroup(true, first, last - first, input[first..last], names[group])
                : RegexGroup.Unmatched(names[group]);
            if (names[group] is { } name)
                named[name] = groups[group];
        }

        return new RegexMatch(true, index, end - index, value, groups, named);
    }
}
