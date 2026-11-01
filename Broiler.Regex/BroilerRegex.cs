// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   15
// Annotated:        15/15
// Exempt:           6
// Human-reviewed:   0/15
// IP risk:          Low
// Security risk:    High
// Criteria:         14/14
// Resource impact:  8/10 max
// Unverified:       15
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
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
/// <see cref="IsMatch"/> and <see cref="NextMatch"/>, which decision D12 keeps for their existing consumers, under the
/// policy decision D10 sets for an engine that passes no meter: no meter, no step ceiling, and the engines' frame and
/// trail ceilings (<see cref="RegexMatchCeilings.Engines"/>). A pattern that backtracks catastrophically runs to its
/// answer there; a caller that needs a bound on the work passes a meter or a step ceiling to the offset entry.
/// </para>
/// <para>
/// <b>Compilation is charged</b> (requirement R2, decision D8 of Broiler.VM's JSD-0081; task 2.5). <see cref="Compile"/>
/// takes the caller's meter and compile budget and charges the work its expansions do, in proportion to the ranges and
/// strings they touch, in blocks before the work, as a match is charged; the constructors, like the string-bearing
/// members, take neither, as an engine that passes no meter does (decision D10).
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
    /// <c>NestingMaximumTests.MatchedShapes</c> and of <c>ShapesTheOldMatcherDidNotProve</c> alike, matches at this maximum on a 1 MiB
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

    /// <summary>The pattern's source text, as the caller gave it (the <c>source</c> before EscapeRegExpPattern, ECMA-262 §22.2.6.13.1).</summary>
    public string Pattern { get; }

    /// <summary>The flags the pattern was compiled under (ECMA-262 §22.2.6.4, the <c>flags</c> getter's flags).</summary>
    public RegexFlags Flags { get; }

    /// <summary>Number of capturing groups in the pattern.</summary>
    public int CaptureCount { get; }

    /// <summary>
    /// How many slots the offset entry writes: two for each group, group 0, the whole match, first, so
    /// 2 × (<see cref="CaptureCount"/> + 1).
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=4C9AE6
    // Broiler-Human:        PENDING
    public int SlotCount => _program.SlotCount;

    /// <summary>
    /// The pattern's group names: each name with the groups that carry it, each group with its name, and the group of a
    /// name that participated in a match the offset entry wrote (requirement R6; ECMA-262 §22.2.1.4, §22.2.7.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// ES2025 lets a name be declared by several groups when no two of them can both participate, so a name maps to a
    /// list of groups (<see cref="RegexGroupNames.GroupsNamed"/>), and a match to the one that participated
    /// (<see cref="RegexGroupNames.ParticipatingGroup"/>, from the offset entry's slots; <see cref="RegexMatch.GroupNamed"/>
    /// for <see cref="Match(string, int)"/>'s result).
    /// </para>
    /// <para>
    /// <b>Reshaped.</b> Until task 2.6 of SJL-11.2 this was an <c>IReadOnlyDictionary&lt;string, int&gt;</c>
    /// of each name to its one group, which a name of several groups does not fit (a break of <c>0.1.0-preview.4</c>'s
    /// surface, listed with requirement R6); fixed for <c>0.1.0-preview.6</c> by task 2.9 of SJL-11.2.
    /// It cannot be changed: the compiled pattern and its concurrent matches share it (convention 10).
    /// </para>
    /// </remarks>
    public RegexGroupNames GroupNames { get; }

    /// <summary>Compiles <paramref name="pattern"/> under the flags string <paramref name="flags"/>.</summary>
    /// <param name="pattern">The pattern's source text.</param>
    /// <param name="flags">The flags, as a <c>RegExp</c> receives them: a string of <c>d g i m s u v y</c>, or null for none.</param>
    /// <param name="tables">The Unicode owner's tables, of the version <c>Broiler.Unicode.Algorithms</c> reads.</param>
    /// <remarks>
    /// A string-bearing convenience (decision D12): its precondition is a flags text and a pattern the specification
    /// accepts, nested no deeper than <see cref="MaximumNestingLimit"/>. A caller that does not know they are reads the
    /// refusal as a status from <see cref="Text.RegexFlagsText.Parse"/>, <see cref="Validate(string, string, IUnicodeTables, int, out RegexDiagnostic, out RegexLimit)"/>
    /// or <see cref="Compile"/>, with its diagnostic.
    /// </remarks>
    /// <exception cref="System.ArgumentException">
    /// The flags or the pattern are not valid (ECMA-262 §22.2.3.1), or the pattern nests deeper than
    /// <see cref="MaximumNestingLimit"/>: the precondition broken, with the refusal's message, which the validate entry's
    /// diagnostic carries for the same flags and pattern (its <c>Message</c>); or the tables are of another Unicode version
    /// or lack a table the algorithms read.
    /// </exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="tables"/> is null.</exception>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=14510D
    // Broiler-Falsified-If: a flags string with a repeated letter such as "gg" produces an instance instead of throwing an ArgumentException with the validate entry's message
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
    /// <para>
    /// A string-bearing convenience (decision D12), compiled as <see cref="Compile"/> compiles under
    /// <see cref="MaximumNestingLimit"/> with no budget and no meter. Its precondition is a pattern the specification
    /// accepts under <paramref name="flags"/>, nested no deeper than that maximum; a pattern that breaks it is refused, as
    /// a broken precondition is (convention 3), with an <see cref="System.ArgumentException"/> whose message is the
    /// refusal's, the validate entry's diagnostic's <c>Message</c> for the same pattern. A caller that does not know its
    /// pattern is valid calls <see cref="Compile"/>, which answers the refusal as the syntax status with its diagnostic and
    /// takes the caller's own nesting limit, compile budget and meter.
    /// </para>
    /// <para>
    /// No meter is charged and no compile budget bounds the work, which is in proportion to the ranges and strings the
    /// pattern's expansions touch (decision D10's policy for an engine that passes no meter).
    /// </para>
    /// <para>
    /// <b>Changed in <c>0.1.0-preview.6</c></b> (task 2.9 of SJL-11.2): the refusal was a public <c>RegexSyntaxException</c>,
    /// the library's own type for a failure its contract admits, which convention 3 refuses; it is an
    /// <see cref="System.ArgumentException"/> now, whose runtime type is internal.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentException">
    /// The pattern is not valid under the flags (ECMA-262 §22.2.1), or nests deeper than <see cref="MaximumNestingLimit"/>:
    /// the precondition broken, with the refusal's message; or the tables are of another Unicode version or lack a table the
    /// algorithms read.
    /// </exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="tables"/> is null.</exception>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=DFF8D4
    // Broiler-Falsified-If: a pattern of 1,000,000 nested opening parentheses ends the process with a stack overflow instead of throwing an ArgumentException
    // Broiler-Human:        PENDING
    public BroilerRegex(string pattern, RegexFlags flags, IUnicodeTables tables)
        : this(pattern ?? "", flags, new RegexUnicode(tables), new RegexNesting(MaximumNestingLimit), refusesAsSyntax: true)
    {
    }

    /// <summary>
    /// Parses and compiles <paramref name="pattern"/>, descending under <paramref name="nesting"/>, to the new backend's
    /// program, charging <paramref name="data"/>'s account. A pattern nested deeper is refused with the internal
    /// <see cref="RegexSyntaxException"/> when <paramref name="refusesAsSyntax"/> is set, and otherwise with the
    /// <see cref="NestingLimitExceeded"/> that <see cref="Compile"/> answers as a status; a compilation past the account's
    /// budget ends with the <see cref="CompileBudgetSpent"/> that <see cref="Compile"/> answers as a status.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=310E62
    // Broiler-Falsified-If: a pattern the parser accepts under a limit is compiled under another, so the compiler's recursion is bounded by neither
    // Broiler-Human:        PENDING
    private BroilerRegex(string pattern, RegexFlags flags, RegexUnicode data, RegexNesting nesting, bool refusesAsSyntax)
    {
        Pattern = pattern;
        Flags = flags;

        // One compilation's view of the provider, charged to its account, serves the parse and the lowering; the program
        // keeps only the provider's case folding from it, for its matches.
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
    /// Compiles <paramref name="pattern"/> under <paramref name="flags"/> (ECMA-262 §22.2.3.4, ParsePattern, and the
    /// early errors of §22.2.1.1), charging the caller's <paramref name="meter"/> for the work and refusing, with a status
    /// rather than an exception, a pattern that is not valid, one nested deeper than <paramref name="nestingLimit"/>, and
    /// one whose compilation would do more work than <paramref name="compileBudget"/> admits (requirements R2 and R4,
    /// decisions D6, D8 and D9 of Broiler.VM's JSD-0081; conventions 3 to 5).
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
    /// <param name="compileBudget">
    /// The units of work the compilation may do, at least one, or null for no budget; counted in the units the meter is
    /// charged in (the remarks). A compilation that would do more stops before the work that would pass it.
    /// </param>
    /// <param name="meter">The caller's meter, charged in blocks before the work it pays for, or null for none (convention 4).</param>
    /// <param name="regex">The compiled pattern, or null when the status is not <see cref="RegexStatus.Success"/>.</param>
    /// <param name="limit">
    /// The limit reached when the status is <see cref="RegexStatus.LimitExceeded"/>: <see cref="RegexLimit.Nesting"/> or
    /// <see cref="RegexLimit.Compile"/>; otherwise <see cref="RegexLimit.None"/>.
    /// </param>
    /// <param name="diagnostic">
    /// The refusal: for <see cref="RegexStatus.SyntaxError"/>, the diagnostic the validate entry answers for the same
    /// pattern, its code, message identifier, argument and position; for <see cref="RegexLimit.Nesting"/>, the
    /// <see cref="RegexDiagnosticCode.NestingLimit"/> diagnostic at the group or class that opens the level past the limit;
    /// otherwise, the compile budget's limit and a success, the default value, whose code is
    /// <see cref="RegexDiagnosticCode.None"/>.
    /// </param>
    /// <returns>
    /// <see cref="RegexStatus.Success"/>; <see cref="RegexStatus.SyntaxError"/> for a pattern the grammar or an early error
    /// refuses under <paramref name="flags"/>; or <see cref="RegexStatus.LimitExceeded"/> when the pattern nests deeper than
    /// <paramref name="nestingLimit"/> or its compilation would pass <paramref name="compileBudget"/>. Each is checked in
    /// the order the parser reads the pattern and then the compiler lowers it, before the work it guards: the nesting
    /// limit before each descent, the budget before each charge, so a syntax error or a limit the parser meets first is
    /// reported first.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>The units</b> (decision D8, "compilation charged in proportion to the expansion"; bundle <c>sjl-11-001</c>'s check
    /// A-2). One unit per code unit of the pattern, which the parse reads; one per range of each code-point set a union,
    /// an intersection, a difference or a complement reads, and per range a property escape expands to; one per string
    /// a class's strings add or look up, by its length in code units and at least one; one per code point a case fold maps
    /// one at a time, which a set of more than 4,096 code points avoids by folding through <c>Cased</c>, in proportion to
    /// its ranges; and the program compiler's copies of each class's ranges and each class of strings' table. Ranges and
    /// strings gathered out of order cost a sort's 1 + ⌊log₂ n⌋ each. Work that is the same for every pattern, a constant
    /// per code unit of the parse and per node of the tree, is in the pattern's units. A meter is charged whole blocks of
    /// 512 units, before the work they pay for, as a match's meter is: before the k-th unit of work, at least k units
    /// have been charged, and never a block more than the work.
    /// </para>
    /// <para>
    /// <b>The budget and the meter are the caller's, and independent.</b> The budget bounds this compilation's units and
    /// answers a status; the meter, the caller's own count across operations, may throw, and its exception reaches the
    /// caller unchanged, ending the compilation with no state of the library changed, so a new call answers as before.
    /// The constructors charge no meter and take no budget, as an engine that passes no meter compiles (decision D10).
    /// </para>
    /// <para>
    /// <b>The signature</b>, fixed by task 2.9 of SJL-11.2 for <c>0.1.0-preview.6</c>. Task 2.3 gave this member the
    /// nesting limit, task 2.5 the compile budget and the meter, and task 2.9 the diagnostic: a syntax error is decision
    /// D9's syntax status, with the validate entry's diagnostic, and never an exception (convention 3), where
    /// <c>0.1.0-preview.5</c> threw it. The shape follows Broiler.Unicode's operations: a status, the limits and then the
    /// meter as parameters, the results in out parameters. The budget stays a parameter beside the nesting limit, not a
    /// member of <see cref="RegexMatchCeilings"/>, whose ceilings are a match's.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="pattern"/> or <paramref name="tables"/> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="nestingLimit"/> is less than 1 or greater than <see cref="MaximumNestingLimit"/>, or <paramref name="compileBudget"/> is zero.</exception>
    /// <exception cref="System.ArgumentException">The tables are of another Unicode version or lack a table the algorithms read.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decisions D6 to D9; IP=Low; Security=High; Resources=7; Fingerprint=B527E7
    // Broiler-Falsified-If: with a nesting limit of 512, a pattern of 512 nested groups or 512 nested v-mode classes answers Success, one of 511 answers LimitExceeded, a limit above MaximumNestingLimit is accepted, a pattern of 1,000,000 nested groups ends the process, a class of fifty \p{L} under v compiles within a budget of 300 units, the meter is charged after the work it pays for, or a syntax error such as "a)" is thrown rather than answered SyntaxError with the validate entry's diagnostic
    // Broiler-Human:        PENDING
    public static RegexStatus Compile(string pattern, RegexFlags flags, IUnicodeTables tables, int nestingLimit, ulong? compileBudget,
        RegexWorkCharge? meter, out BroilerRegex? regex, out RegexLimit limit, out RegexDiagnostic diagnostic)
    {
        diagnostic = default;
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentOutOfRangeException.ThrowIfLessThan(nestingLimit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nestingLimit, MaximumNestingLimit);
        if (compileBudget == 0)
            throw new ArgumentOutOfRangeException(nameof(compileBudget), "A compile budget, when there is one, is at least one unit.");

        var data = new RegexUnicode(tables, new CompileCharge(meter, compileBudget));
        try
        {
            regex = new BroilerRegex(pattern, flags, data, new RegexNesting(nestingLimit), refusesAsSyntax: false);
            limit = RegexLimit.None;
            return RegexStatus.Success;
        }
        catch (RegexSyntaxException refusal)
        {
            regex = null;
            limit = RegexLimit.None;
            diagnostic = refusal.Diagnostic;
            return RegexStatus.SyntaxError;
        }
        catch (NestingLimitExceeded refusal)
        {
            regex = null;
            limit = RegexLimit.Nesting;
            diagnostic = RegexDiagnostic.Nesting(refusal.Position, nestingLimit, RegexNesting.RefusalMessage);
            return RegexStatus.LimitExceeded;
        }
        catch (CompileBudgetSpent)
        {
            regex = null;
            limit = RegexLimit.Compile;
            return RegexStatus.LimitExceeded;
        }
    }

    /// <summary>
    /// The validate entry (task 2.8 of SJL-11.2; decision D18 of Broiler.VM's JSD-0081): answers whether
    /// <paramref name="pattern"/> is a valid ECMAScript pattern under <paramref name="flags"/>, by the grammar and every
    /// early error the parser knows, under the caller's <paramref name="nestingLimit"/>, and when it is not, which refusal,
    /// as data: a status and a <see cref="RegexDiagnostic"/> with a code, a message identifier and its argument, and a
    /// position (decision D9, convention 3). Nothing is compiled, expanded or charged.
    /// </summary>
    /// <param name="pattern">The pattern's source text, as a literal's body or a <c>RegExp</c> constructor's pattern holds it.</param>
    /// <param name="flags">The flags.</param>
    /// <param name="tables">The Unicode owner's tables, of the version <c>Broiler.Unicode.Algorithms</c> reads: property names and group names are read through them.</param>
    /// <param name="nestingLimit">The deepest nesting the caller admits, from 1 to <see cref="MaximumNestingLimit"/>, counted as <see cref="Compile"/> counts it.</param>
    /// <param name="diagnostic">The refusal, when the status is <see cref="RegexStatus.SyntaxError"/> or <see cref="RegexStatus.LimitExceeded"/>; otherwise the default value, whose code is <see cref="RegexDiagnosticCode.None"/>.</param>
    /// <param name="limit"><see cref="RegexLimit.Nesting"/> when the status is <see cref="RegexStatus.LimitExceeded"/>; otherwise <see cref="RegexLimit.None"/>.</param>
    /// <returns>
    /// <see cref="RegexStatus.Success"/> for a valid pattern; <see cref="RegexStatus.SyntaxError"/> for the first refusal the
    /// parser meets, in the order it reads the pattern; or <see cref="RegexStatus.LimitExceeded"/> when the pattern nests
    /// deeper than <paramref name="nestingLimit"/> before any refusal, its diagnostic <see cref="RegexDiagnosticCode.NestingLimit"/>
    /// at the group or class that opens the level past it.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>What it reads, and what it does not do.</b> The parse a compilation makes, with every set expansion left out: no
    /// property escape is expanded (its name is looked up in the provider's tables, which copies nothing), nothing is folded,
    /// no class's code points or strings are gathered and no set algebra is done. Each early error is decided from the
    /// pattern's text and those lookups: a property's name and kind (code points or strings) from the provider; a group
    /// name's code points from the provider's identifier classes (<c>EcmaScriptLexical</c>); MayContainStrings of a negated
    /// <c>v</c>-mode class from its syntax (§22.2.1.8), never from the strings its set operations would leave; a range's
    /// order from its two characters; duplicate names, <c>\k&lt;name&gt;</c> and <c>\n</c> from the names and the count of
    /// groups the pattern declares. Its work is in proportion to the pattern's length, which is why it takes no meter and no
    /// budget (decision D18 as JSD-0081 files it: "no expansion, no meter"; plan row 2.8's "nullable meter" is not taken).
    /// </para>
    /// <para>
    /// <b>As <see cref="Compile"/> answers.</b> Every syntax error <see cref="Compile"/> and the constructors refuse a
    /// pattern with, this entry answers with the same diagnostic: the same code, identifier, argument and position, which
    /// <see cref="Compile"/> answers with its status and whose message the constructors' exception carries; and a pattern it
    /// accepts compiles. The two part only
    /// where <see cref="Compile"/> meets a limit this entry does not have: its compile budget, which may stop a compilation
    /// before it reaches a later syntax error or the end of a valid pattern, and its meter, whose exception does the same.
    /// Both read the caller's nesting limit alike: past it, this entry answers <see cref="RegexStatus.LimitExceeded"/> where
    /// <see cref="Compile"/> does, and the constructors, which parse under <see cref="MaximumNestingLimit"/>, throw with the
    /// same diagnostic's message.
    /// </para>
    /// <para>
    /// New in task 2.8 of SJL-11.2, with <see cref="RegexDiagnostic"/>, <see cref="RegexDiagnosticCode"/>
    /// and <see cref="RegexStatus.SyntaxError"/>; fixed for <c>0.1.0-preview.6</c> by task 2.9 of SJL-11.2.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="pattern"/> or <paramref name="tables"/> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="nestingLimit"/> is less than 1 or greater than <see cref="MaximumNestingLimit"/>.</exception>
    /// <exception cref="System.ArgumentException">The tables are of another Unicode version or lack a table the algorithms read.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decisions D9 and D18; IP=None; Security=High; Resources=3; Fingerprint=384C55
    // Broiler-Falsified-If: under u, \k<b>(?<a>x) answers Success, a pattern Compile refuses with a syntax error is answered otherwise or at another position, one Compile compiles is refused, 512 nested groups under a limit of 512 answer other than LimitExceeded naming Nesting, or validating charges a unit of expansion
    // Broiler-Human:        PENDING
    public static RegexStatus Validate(string pattern, RegexFlags flags, IUnicodeTables tables, int nestingLimit,
        out RegexDiagnostic diagnostic, out RegexLimit limit)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentOutOfRangeException.ThrowIfLessThan(nestingLimit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nestingLimit, MaximumNestingLimit);
        return Validate(pattern, flags, new RegexUnicode(tables), new RegexNesting(nestingLimit), out diagnostic, out limit);
    }

    /// <summary>
    /// The validate entry over a flags text (task 2.8 of SJL-11.2): the flags as a <c>RegExp</c> receives them, then the
    /// pattern under them, as
    /// <see cref="Validate(string, RegexFlags, IUnicodeTables, int, out RegexDiagnostic, out RegexLimit)"/> reads it.
    /// Together they are a literal's IsValidRegularExpressionLiteral (ECMA-262 §13.2.7.2): a flags text it refuses is
    /// answered <see cref="RegexStatus.SyntaxError"/> with <see cref="RegexDiagnosticCode.UnknownFlag"/>,
    /// <see cref="RegexDiagnosticCode.DuplicateFlag"/> or <see cref="RegexDiagnosticCode.UnicodeWithUnicodeSets"/>, at the
    /// code unit <see cref="Text.RegexFlagsText.Parse"/> names, a position in the flags text, before the pattern is read.
    /// </summary>
    /// <param name="pattern">The pattern's source text.</param>
    /// <param name="flags">The flags text: code units of <c>d g i m s u v y</c>, or null or empty for none.</param>
    /// <param name="tables">The Unicode owner's tables, of the version <c>Broiler.Unicode.Algorithms</c> reads.</param>
    /// <param name="nestingLimit">The deepest nesting the caller admits, from 1 to <see cref="MaximumNestingLimit"/>.</param>
    /// <param name="diagnostic">The refusal, or the default value when the status is <see cref="RegexStatus.Success"/>.</param>
    /// <param name="limit"><see cref="RegexLimit.Nesting"/> when the status is <see cref="RegexStatus.LimitExceeded"/>; otherwise <see cref="RegexLimit.None"/>.</param>
    /// <returns><see cref="RegexStatus.Success"/>, <see cref="RegexStatus.SyntaxError"/> or <see cref="RegexStatus.LimitExceeded"/>, as the overload over <see cref="RegexFlags"/> answers.</returns>
    /// <remarks>The constructor over a flags text throws with the same diagnostic's message for the same flags.</remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="pattern"/> or <paramref name="tables"/> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="nestingLimit"/> is less than 1 or greater than <see cref="MaximumNestingLimit"/>.</exception>
    /// <exception cref="System.ArgumentException">The tables are of another Unicode version or lack a table the algorithms read.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s13.2.7.2 IsValidRegularExpressionLiteral; IP=None; Security=High; Resources=3; Fingerprint=702602
    // Broiler-Falsified-If: the flags "gg" answer other than DuplicateFlag at 1, "uv" other than UnicodeWithUnicodeSets at 1, or a flags refusal is answered after a refusal of the pattern
    // Broiler-Human:        PENDING
    public static RegexStatus Validate(string pattern, string? flags, IUnicodeTables tables, int nestingLimit,
        out RegexDiagnostic diagnostic, out RegexLimit limit)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(tables);
        ArgumentOutOfRangeException.ThrowIfLessThan(nestingLimit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nestingLimit, MaximumNestingLimit);
        if (Text.RegexFlagsText.Parse(flags, out var parsed, out diagnostic) != RegexStatus.Success)
        {
            limit = RegexLimit.None;
            return RegexStatus.SyntaxError;
        }

        return Validate(pattern, parsed, new RegexUnicode(tables), new RegexNesting(nestingLimit), out diagnostic, out limit);
    }

    /// <summary>
    /// The validate entry over one compilation's view of the provider, <paramref name="data"/>, whose account a test reads to
    /// see that nothing but the pattern's read is charged (no expansion); the arguments are checked.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D18; IP=None; Security=High; Resources=3; Fingerprint=D3A4DE
    // Broiler-Falsified-If: a refusal escapes as an exception, or the nesting refusal is answered without its diagnostic
    // Broiler-Human:        PENDING
    internal static RegexStatus Validate(string pattern, RegexFlags flags, RegexUnicode data, RegexNesting nesting,
        out RegexDiagnostic diagnostic, out RegexLimit limit)
    {
        try
        {
            RegexParser.Validate(pattern, flags, data, nesting);
            diagnostic = default;
            limit = RegexLimit.None;
            return RegexStatus.Success;
        }
        catch (RegexSyntaxException refusal)
        {
            diagnostic = refusal.Diagnostic;
            limit = RegexLimit.None;
            return RegexStatus.SyntaxError;
        }
        catch (NestingLimitExceeded refusal)
        {
            diagnostic = RegexDiagnostic.Nesting(refusal.Position, nesting.Limit, RegexNesting.RefusalMessage);
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
    /// pair's low half as a lone unit, and a read backward from it (a look-behind's, <c>\b</c>'s, <c>^</c>'s under
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
    /// New in task 2.4c of SJL-11.2, with <see cref="RegexWorkCharge"/>, <see cref="RegexMatchCeilings"/> and
    /// <see cref="SlotCount"/> made public for it; fixed for <c>0.1.0-preview.6</c> by task 2.9 of SJL-11.2, with no change of its signature.
    /// </para>
    /// <para>
    /// <b>Memory.</b> The call's registers, frames and trail are arrays rented from the shared array pool and given back
    /// before it returns, its exceptions included; once the pool holds arrays of the sizes the pattern needs, a call
    /// allocates nothing, whatever the capture count (check A-8), as long as it holds at most 16,384 backtrack frames and
    /// 32,768 trail cells at once. Past them its frames or trail are allocated for the call alone, so that one call's peak
    /// does not stay pooled; a capturing group that participates writes two trail cells, so a pattern of more than about
    /// 16,384 such groups allocates, in every call, a trail that grows with them. The compiled pattern is not changed by
    /// a call, so concurrent calls on one instance, and a call the meter makes while it is charged, each answer as alone.
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
    // Broiler-Falsified-If: /b/y unanchored on "ab" answers other than a match at 1, /b/ anchored at 0 of "ab" answers Match, a call that reaches a ceiling answers Match or NoMatch, or a call through a pattern of 1,000 or 16,384 groups allocates once the pool is warm
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
    /// This is the string-bearing member's one throw for an expected failure, which convention 3 would make a status:
    /// decision D12 keeps the member for its consumers, and the owner admitted the exception for it on 2026-11-01
    /// (task 2.9; <c>docs/conventions.md</c>, "Where Broiler.Regex stands"). The status forms are
    /// <see cref="Match(string, int)"/> and the offset entry.
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
    /// The next match of the global iteration after <paramref name="match"/>, a match of this pattern (ECMA-262 §22.2.6.9,
    /// <c>RegExp.prototype[@@match]</c> with <c>g</c>, and §22.2.6.13): <see cref="Match(string, int)"/> in the same
    /// subject from where <paramref name="match"/> ended, or, after an empty match, from one code point past it, a
    /// surrogate pair whole under <c>u</c> and <c>v</c> (AdvanceStringIndex, §22.2.7.3). A failed match answers itself, so a
    /// loop from <see cref="Match(string, int)"/> at 0 that calls this until <see cref="RegexMatch.Success"/> is false finds
    /// every match, and ends with <see cref="RegexMatch.Empty"/> when there is no more and with a failed match whose
    /// <see cref="RegexMatch.Limit"/> names the ceiling when the next one reached it.
    /// </summary>
    /// <param name="match">A match this pattern's <see cref="Match(string, int)"/> or this member answered.</param>
    /// <returns>The next match, a failed match, or <paramref name="match"/> itself when it failed.</returns>
    /// <remarks>
    /// <b>New in <c>0.1.0-preview.6</c></b> (task 2.9 of SJL-11.2), in place of <c>Matches</c>, whose
    /// <c>IEnumerable&lt;RegexMatch&gt;</c> convention 1 keeps out of a public signature (check C1): the same matches, in
    /// the same order, the one that reached a ceiling last. The work is <see cref="Match(string, int)"/>'s: no meter, no step
    /// ceiling, the engines' frame and trail ceilings. The match keeps its subject for this; the flags are this pattern's,
    /// so a match another pattern found steps over a pair as this one's flags say.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.6.9 and s22.2.7.3 AdvanceStringIndex; IP=None; Security=High; Resources=8; Fingerprint=770A62
    // Broiler-Falsified-If: after an empty match just before a surrogate pair in u or v mode, the next match starts at the pair's low surrogate, a match that reached a ceiling answers other than itself, or the matches it walks differ from the global iteration's
    // Broiler-Human:        PENDING
    public RegexMatch NextMatch(RegexMatch match)
    {
        ArgumentNullException.ThrowIfNull(match);
        if (!match.Success || match.Input is not { } input)
            return match;

        var next = match.Index + match.Length;
        if (match.Length == 0)
        {
            next = match.Index + 1;
            if (Flags.IsUnicodeMode() && match.Index < input.Length
                && char.IsHighSurrogate(input[match.Index])
                && match.Index + 1 < input.Length && char.IsLowSurrogate(input[match.Index + 1]))
                next++;
        }

        return Match(input, next);
    }

    /// <summary>The exception <see cref="IsMatch"/> throws for a match that reached <paramref name="limit"/>.</summary>
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
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2 and JSD-0081 decision D41; IP=None; Security=High; Resources=8; Fingerprint=DE5CAD
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
        groups[0] = new RegexGroup(0, true, index, end - index, value, null);
        for (var group = 1; group <= CaptureCount; group++)
        {
            var (first, last) = (slots[2 * group], slots[(2 * group) + 1]);
            var name = GroupNames.NameOfGroup(group);
            groups[group] = first >= 0 && last >= first
                ? new RegexGroup(group, true, first, last - first, input[first..last], name)
                : RegexGroup.Unmatched(group, name);
        }

        // A name's group, the one that participated or its first (RegExpBuiltinExec, step 34.e), is read from the groups
        // by RegexMatch.GroupNamed; the match keeps its subject for NextMatch.
        return new RegexMatch(true, index, end - index, value, groups, input);
    }
}
