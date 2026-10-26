// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   19
// Annotated:        19/19
// Exempt:           25
// Human-reviewed:   0/19
// IP risk:          None
// Security risk:    High
// Criteria:         10/10
// Resource impact:  5/10 max
// Unverified:       19
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using Broiler.Regex.Ast;
using Broiler.Regex.Engine;
using Broiler.Regex.Parsing;
using Broiler.Regex.Unicode;
using Broiler.Unicode.Algorithms;
using CodePointSet = Broiler.Regex.Ast.CodePointSet;

namespace Broiler.Regex.Compiling;

/// <summary>How a compilation to the new backend's program ended.</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=C24ED5
// Broiler-Human:        PENDING
internal enum ProgramCompilation
{
    /// <summary>The program is built.</summary>
    Compiled = 0,

    /// <summary>The pattern nests deeper than the caller's nesting limit (requirement R4, decisions D6 and D7).</summary>
    NestingLimitExceeded = 1,

    /// <summary>
    /// The pattern holds a construct whose instructions task 2.4b has still to add: an assertion (<c>^ $ \b \B</c>), a
    /// back-reference, a look-around, a modifier group, or a <c>v</c>-mode class holding strings. A transitional answer
    /// of task 2.4a, which task 2.4b removes; no program is built, nothing is thrown.
    /// </summary>
    NotYetSupported = 2,
}

/// <summary>
/// The compiler from the parser's tree to the new backend's <see cref="RegexProgram"/> (decision D5 of Broiler.VM's
/// JSD-0081): task 2.4a's lowering of literal characters, <c>.</c>, classes, sequence, alternation, groups and
/// quantifiers, and task 2.4b's of comparison under <c>i</c>. It lives outside the machine's namespace, <c>Broiler.Regex.Engine</c>, whose methods the architecture
/// tests hold to having no recursion (bundle <c>sjl-11-001</c>'s check A-4).
/// </summary>
/// <remarks>
/// <para>
/// <b>No recursion, and the nesting limit still checked.</b> The tree is walked twice on work lists of the heap, as the
/// closure compiler of <c>Matching/</c> walks it, so compiling uses the same CLR stack at every depth; decision D6 would
/// admit recursion under the caller's nesting limit, and the walks do not need it. The first walk, which refuses what
/// the program cannot hold yet and finds each quantifier's groups, calls <see cref="RegexNesting.Descend"/> before it
/// enters a group's body, as the parser did, so a tree the parser did not build cannot be deeper than the limit either;
/// the second, which emits the instructions, walks the same tree after it.
/// </para>
/// <para>
/// <b>The lowering</b>, for a body <c>X</c> whose capture groups are <c>first</c> to <c>last</c>, with <c>L</c> the
/// address after the construct:
/// </para>
/// <list type="bullet">
/// <item>a character: <c>Literal c</c>, or under <c>i</c> <c>LiteralIgnoringCase</c> with its Canonicalize; <c>.</c>:
/// <c>AnyButLineTerminator</c>, or <c>Any</c> under <c>s</c>; a class: <c>Class k</c>, its code points with its negation
/// applied, or under <c>i</c> <c>ClassIgnoringCase k, inverted</c>, its members as written, or <c>CanonicalClass k</c> for a
/// <c>v</c>-mode class the parser folded;</item>
/// <item><c>X₁|X₂|…|Xₙ</c>: <c>Choice +1, next</c> before each alternative but the last, and a <c>Goto L</c> after it;</item>
/// <item>group <c>k</c>: <c>SavePosition 2k; X; SavePosition 2k+1</c>; a non-capturing group: <c>X</c>;</item>
/// <item><c>X?</c>: <c>Choice body, L; body: SavePosition m; ClearCaptures; X; RejectEmptyIteration m</c> (the choice's
/// ways swapped when lazy);</item>
/// <item><c>X*</c>: <c>head: Choice body, L; body: SavePosition m; ClearCaptures; X; RejectEmptyIteration m; Goto
/// head</c>;</item>
/// <item><c>X{min,max}</c>, any other bounds: <c>SetCounter n; loop: ExitAtMaximum n, max, L; EnterBelowMinimum n, min,
/// body; Choice body, L; body: SavePosition m; ClearCaptures; X; RejectEmptyIteration m, n, min; IncrementCounter n; Goto
/// loop</c>, the exit left out when <c>max</c> is unbounded and the entry when <c>min</c> is zero;</item>
/// <item><c>X{0}</c>: nothing, as RepeatMatcher returns its continuation at once when <c>max</c> is zero; the whole
/// pattern is followed by <c>Succeed</c>.</item>
/// </list>
/// <para>
/// <c>m</c> and <c>n</c> are registers of the quantifier's own, above the capture registers, and <c>ClearCaptures</c> is
/// left out when <c>X</c> holds no group. The reset is inside the iteration, after the choice, so that an iteration that
/// fails takes its reset back with it; the empty check reads the count before the iteration's increment, so a count
/// that stops at <see cref="int.MaxValue"/> still tells a required iteration from an optional one.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=5; Fingerprint=393186
// Broiler-Falsified-If: compiling a tree nested to the library's maximum uses more CLR stack than a flat one, a group's body is entered without checking the caller's nesting limit, or a pattern holding an assertion, a back-reference, a look-around, a modifier or a string class is given a program
// Broiler-Human:        PENDING
internal static class ProgramCompiler
{
    /// <summary>
    /// Parses <paramref name="pattern"/> under <paramref name="flags"/> and the caller's nesting limit, and compiles the
    /// tree to a program.
    /// </summary>
    /// <param name="pattern">The pattern's source text.</param>
    /// <param name="flags">The flags; <c>d</c>, <c>g</c>, <c>m</c> and <c>y</c> change nothing a program does.</param>
    /// <param name="tables">The Unicode owner's tables, which the parser reads.</param>
    /// <param name="nestingLimit">The deepest nesting admitted, from 1 to <see cref="BroilerRegex.MaximumNestingLimit"/>, counted as <see cref="BroilerRegex.Compile"/> counts.</param>
    /// <param name="program">The program, or null when the answer is not <see cref="ProgramCompilation.Compiled"/>.</param>
    /// <exception cref="RegexSyntaxException">The pattern is not valid under the flags, as the parser refuses it.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> or <paramref name="tables"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nestingLimit"/> is less than 1 or greater than <see cref="BroilerRegex.MaximumNestingLimit"/>.</exception>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=5; Fingerprint=79AA61
    // Broiler-Falsified-If: a pattern of 512 nested groups under a limit of 512 answers Compiled, or one of 511 answers NestingLimitExceeded
    // Broiler-Human:        PENDING
    internal static ProgramCompilation Compile(string pattern, RegexFlags flags, IUnicodeTables tables, int nestingLimit, out RegexProgram? program)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentOutOfRangeException.ThrowIfLessThan(nestingLimit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nestingLimit, BroilerRegex.MaximumNestingLimit);

        var nesting = new RegexNesting(nestingLimit);
        var data = new RegexUnicode(tables);
        RegexNode tree;
        int captureCount;
        IReadOnlyDictionary<string, int> groupNames;
        try
        {
            tree = RegexParser.Parse(pattern, flags, data, nesting, out captureCount, out groupNames);
        }
        catch (NestingLimitExceeded)
        {
            program = null;
            return ProgramCompilation.NestingLimitExceeded;
        }

        return Compile(tree, captureCount, groupNames, flags, data, nesting, out program);
    }

    /// <summary>Compiles a tree the parser built under <paramref name="flags"/> to a program.</summary>
    /// <param name="tree">The parser's tree.</param>
    /// <param name="captureCount">The pattern's capturing groups, as the parser counted them.</param>
    /// <param name="groupNames">The parser's map of group names to group numbers, which a named back-reference reads.</param>
    /// <param name="flags">The flags the tree was parsed under.</param>
    /// <param name="data">The Unicode answers of the compilation the parser read, whose case folding the program keeps.</param>
    /// <param name="nesting">The caller's nesting limit, checked again, by the first walk, before each group's body is entered.</param>
    /// <param name="program">The program, or null when the answer is not <see cref="ProgramCompilation.Compiled"/>.</param>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=5; Fingerprint=EA9B84
    // Broiler-Falsified-If: a tree holding an AnchorNode, a BackreferenceNode, a LookaroundNode, a ModifierGroupNode or a class with strings is answered with a program, or a character under i is given an instruction that compares it exactly
    // Broiler-Human:        PENDING
    internal static ProgramCompilation Compile(RegexNode tree, int captureCount, IReadOnlyDictionary<string, int> groupNames,
        RegexFlags flags, RegexUnicode data, RegexNesting nesting, out RegexProgram? program)
    {
        program = null;
        try
        {
            var groups = Survey(tree, nesting);
            if (groups is null)
                return ProgramCompilation.NotYetSupported;

            program = new ProgramWriter(captureCount, flags, data, groups).Emit(tree);
            return ProgramCompilation.Compiled;
        }
        catch (NestingLimitExceeded)
        {
            program = null;
            return ProgramCompilation.NestingLimitExceeded;
        }
    }

    /// <summary>
    /// Walks the tree once before anything is emitted: refuses a construct the program cannot hold yet, by answering
    /// null, and finds the capture groups of each quantifier's body, the registers its iterations reset.
    /// </summary>
    /// <exception cref="NestingLimitExceeded">The tree nests deeper than <paramref name="nesting"/>.</exception>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=4; Fingerprint=1E2380
    // Broiler-Falsified-If: the groups found for the quantifier of /((a)|(b))*/ are other than 1 to 3, or for the inner one of /(a)(?:(b)(c)+)*/ other than 3 to 3
    // Broiler-Human:        PENDING
    private static Dictionary<QuantifierNode, (int First, int Last)>? Survey(RegexNode tree, RegexNesting nesting)
    {
        var groups = new Dictionary<QuantifierNode, (int First, int Last)>();

        // A pending entry is a node to visit at its depth, or, with Exit set, the end of a quantifier's body: the groups
        // seen since the quantifier's entry, which its own entry on the open list collects, are then its body's.
        var pending = new Stack<(RegexNode Node, int Depth, bool Exit)>();
        var open = new List<(int First, int Last)>();
        pending.Push((tree, 1, false));
        while (pending.Count > 0)
        {
            var (node, depth, exit) = pending.Pop();
            if (exit)
            {
                var body = open[^1];
                open.RemoveAt(open.Count - 1);
                groups[(QuantifierNode)node] = body;
                if (open.Count > 0 && body.Last >= body.First)
                    open[^1] = Widen(open[^1], body.First, body.Last);
                continue;
            }

            switch (node)
            {
                case EmptyNode or CharNode or AnyCharNode:
                    break;
                case CharClassNode cls when !cls.Set.HasStrings:
                    break;
                case SequenceNode sequence:
                    for (var i = sequence.Terms.Count - 1; i >= 0; i--)
                        pending.Push((sequence.Terms[i], depth, false));
                    break;
                case DisjunctionNode disjunction:
                    for (var i = disjunction.Alternatives.Count - 1; i >= 0; i--)
                        pending.Push((disjunction.Alternatives[i], depth, false));
                    break;
                case GroupNode group:
                    if (group.IsCapturing && open.Count > 0)
                        open[^1] = Widen(open[^1], group.CaptureIndex, group.CaptureIndex);
                    pending.Push((group.Child, nesting.Descend(depth, -1), false));
                    break;
                case QuantifierNode quantifier:
                    open.Add((0, -1));
                    pending.Push((quantifier, depth, true));
                    pending.Push((quantifier.Child, depth, false));
                    break;
                default:
                    // An assertion, a back-reference, a look-around, a modifier group or a class holding strings: the
                    // instructions task 2.4b has still to add.
                    return null;
            }
        }

        return groups;
    }

    /// <summary>The span of groups <paramref name="span"/> widened to hold <paramref name="first"/> to <paramref name="last"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=22985D
    // Broiler-Human:        PENDING
    private static (int First, int Last) Widen((int First, int Last) span, int first, int last) =>
        span.Last < span.First ? (first, last) : (Math.Min(span.First, first), Math.Max(span.Last, last));

    /// <summary>What one entry of the emitter's work list does.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=F1254C
    // Broiler-Human:        PENDING
    private enum Work
    {
        /// <summary>Lower a node.</summary>
        Visit,

        /// <summary>Write a capture group's end.</summary>
        CloseGroup,

        /// <summary>Begin an alternative: the choice that leads to the next one, unless it is the last.</summary>
        OpenAlternative,

        /// <summary>End an alternative: its jump past the alternation, unless it is the last.</summary>
        CloseAlternative,

        /// <summary>Point every alternative's jump past the alternation.</summary>
        CloseAlternation,

        /// <summary>End a quantifier's iteration: the empty check, the count and the jump back, and the exits.</summary>
        CloseIteration,
    }

    /// <summary>The addresses an alternation's lowering patches once its alternatives are emitted.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=44F968
    // Broiler-Human:        PENDING
    private sealed class Alternation(int count)
    {
        /// <summary>How many alternatives there are.</summary>
        internal readonly int Count = count;

        /// <summary>The choice before the alternative being emitted.</summary>
        internal int Choice;

        /// <summary>Every alternative's jump past the alternation.</summary>
        internal readonly List<int> Exits = [];
    }

    /// <summary>One quantifier's lowering in progress: its form, its registers and the addresses it patches.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=BFC3B2
    // Broiler-Human:        PENDING
    private sealed class Iteration(QuantifierNode quantifier, int mark, int counter)
    {
        /// <summary>The quantifier.</summary>
        internal readonly QuantifierNode Quantifier = quantifier;

        /// <summary>The register an iteration's start is written to, for the empty check.</summary>
        internal readonly int Mark = mark;

        /// <summary>The iteration count's register, or -1 for <c>?</c> and <c>*</c>, which need none.</summary>
        internal readonly int Counter = counter;

        /// <summary>Where the loop goes back to: the choice of <c>*</c>, or the bound checks of a counted form; -1 for <c>?</c>.</summary>
        internal int Loop = -1;

        /// <summary>The counted form's check of its maximum, whose target is the exit; -1 when there is none.</summary>
        internal int AtMaximum = -1;

        /// <summary>The choice between another iteration and the continuation.</summary>
        internal int Choice;
    }

    /// <summary>One work-list entry: what to do, to which node, with which pending lowering.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=A765DF
    // Broiler-Human:        PENDING
    private readonly record struct Step(Work Work, RegexNode? Node, int Index = 0, Alternation? Alternation = null, Iteration? Iteration = null);

    /// <summary>The lowering of one tree into one program.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=5; Fingerprint=DDB228
    // Broiler-Falsified-If: a lazy quantifier's choice tries the body before the continuation, or an alternation's choice resumes anywhere but at the next alternative
    // Broiler-Human:        PENDING
    private sealed class ProgramWriter(int captureCount, RegexFlags flags, RegexUnicode data, Dictionary<QuantifierNode, (int First, int Last)> groups)
    {
        private readonly List<RegexInstruction> _code = [];
        private readonly List<int[]> _classes = [];
        private readonly bool _dotAll = (flags & RegexFlags.DotAll) != 0;
        private readonly bool _ignoreCase = (flags & RegexFlags.IgnoreCase) != 0;
        private readonly bool _unicode = flags.IsUnicodeMode();

        /// <summary>Whether an instruction compares under <c>i</c>, so that the program keeps the provider's case folding.</summary>
        private bool _folds;

        /// <summary>The next free register: the capture registers come first, two per group, group 0's included.</summary>
        private int _registers = 2 * (captureCount + 1);

        /// <summary>The program of <paramref name="tree"/>, which the first walk read, ending in <see cref="RegexOp.Succeed"/>.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=5; Fingerprint=1D4C75
        // Broiler-Falsified-If: an alternation's choice resumes at an address other than the next alternative's first instruction, or a group's end is written before its body
        // Broiler-Human:        PENDING
        internal RegexProgram Emit(RegexNode tree)
        {
            var work = new Stack<Step>();
            work.Push(new Step(Work.Visit, tree));
            while (work.Count > 0)
            {
                var step = work.Pop();
                switch (step.Work)
                {
                    case Work.Visit:
                        Visit(step.Node!, work);
                        break;
                    case Work.CloseGroup:
                        Add(RegexOp.SavePosition, (2 * step.Index) + 1);
                        break;
                    case Work.OpenAlternative:
                        if (step.Index < step.Alternation!.Count - 1)
                            step.Alternation.Choice = Add(RegexOp.Choice, _code.Count + 1, -1);
                        break;
                    case Work.CloseAlternative:
                        if (step.Index < step.Alternation!.Count - 1)
                        {
                            step.Alternation.Exits.Add(Add(RegexOp.Goto, -1));
                            Patch(step.Alternation.Choice, b: _code.Count);
                        }

                        break;
                    case Work.CloseAlternation:
                        foreach (var exit in step.Alternation!.Exits)
                            Patch(exit, a: _code.Count);
                        break;
                    case Work.CloseIteration:
                        CloseIteration(step.Iteration!);
                        break;
                }
            }

            Add(RegexOp.Succeed);

            return new RegexProgram(_code.ToArray(), _classes.ToArray(), captureCount, _registers, _unicode, _folds ? data.Folding : null);
        }

        /// <summary>Lowers what can be lowered of <paramref name="node"/> now, and puts the rest on the work list.</summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=4; Fingerprint=4D3C6D
        // Broiler-Falsified-If: an alternation's alternatives, or a sequence's terms, are emitted in an order other than the pattern's
        // Broiler-Human:        PENDING
        private void Visit(RegexNode node, Stack<Step> work)
        {
            switch (node)
            {
                case EmptyNode:
                    break;
                case CharNode character when _ignoreCase:
                    _folds = true;
                    Add(RegexOp.LiteralIgnoringCase, data.Canonicalize(character.CodePoint, _unicode));
                    break;
                case CharNode character:
                    Add(RegexOp.Literal, character.CodePoint);
                    break;
                case AnyCharNode:
                    Add(_dotAll ? RegexOp.Any : RegexOp.AnyButLineTerminator);
                    break;
                case CharClassNode cls:
                    AddClass(cls.Set);
                    break;
                case SequenceNode sequence:
                    for (var i = sequence.Terms.Count - 1; i >= 0; i--)
                        work.Push(new Step(Work.Visit, sequence.Terms[i]));
                    break;
                case DisjunctionNode disjunction:
                {
                    var alternation = new Alternation(disjunction.Alternatives.Count);
                    work.Push(new Step(Work.CloseAlternation, null, Alternation: alternation));
                    for (var i = disjunction.Alternatives.Count - 1; i >= 0; i--)
                    {
                        work.Push(new Step(Work.CloseAlternative, null, i, alternation));
                        work.Push(new Step(Work.Visit, disjunction.Alternatives[i]));
                        work.Push(new Step(Work.OpenAlternative, null, i, alternation));
                    }

                    break;
                }

                case GroupNode group:
                    if (group.IsCapturing)
                    {
                        Add(RegexOp.SavePosition, 2 * group.CaptureIndex);
                        work.Push(new Step(Work.CloseGroup, null, group.CaptureIndex));
                    }

                    work.Push(new Step(Work.Visit, group.Child));
                    break;
                case QuantifierNode quantifier:
                    OpenIteration(quantifier, work);
                    break;
            }
        }

        /// <summary>
        /// Begins a quantifier's lowering: its registers, the bound checks and the choice, the iteration's start and
        /// capture reset; the body and <see cref="Work.CloseIteration"/> go on the work list.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.3.1; IP=None; Security=High; Resources=3; Fingerprint=624325
        // Broiler-Falsified-If: X{0} emits X, a quantifier with a minimum of zero emits an EnterBelowMinimum, or one with an unbounded maximum an ExitAtMaximum
        // Broiler-Human:        PENDING
        private void OpenIteration(QuantifierNode quantifier, Stack<Step> work)
        {
            var min = quantifier.Min;
            var max = quantifier.Max;

            // RepeatMatcher with a maximum of zero returns its continuation at once (§22.2.2.3.1, step 1).
            if (max == 0)
                return;

            var optional = min == 0 && (max == 1 || max == QuantifierNode.Unbounded);
            var iteration = new Iteration(quantifier, _registers++, optional ? -1 : _registers++);
            if (optional)
            {
                if (max == QuantifierNode.Unbounded)
                    iteration.Loop = _code.Count;
            }
            else
            {
                Add(RegexOp.SetCounter, iteration.Counter);
                iteration.Loop = _code.Count;
                if (max != QuantifierNode.Unbounded)
                    iteration.AtMaximum = Add(RegexOp.ExitAtMaximum, iteration.Counter, max, -1);
                if (min > 0)
                    Add(RegexOp.EnterBelowMinimum, iteration.Counter, min, _code.Count + 2);
            }

            // The choice: greedy takes another iteration first, lazy the continuation first; the exit is patched when
            // the iteration closes.
            iteration.Choice = Add(RegexOp.Choice, -1, -1);
            Add(RegexOp.SavePosition, iteration.Mark);
            var (first, last) = groups[quantifier];
            if (last >= first)
                Add(RegexOp.ClearCaptures, 2 * first, (2 * last) + 1);

            work.Push(new Step(Work.CloseIteration, null, Iteration: iteration));
            work.Push(new Step(Work.Visit, quantifier.Child));
        }

        /// <summary>Ends a quantifier's lowering: the empty check, the count and the jump back, then the exits.</summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.3.1; IP=None; Security=High; Resources=3; Fingerprint=D63006
        // Broiler-Falsified-If: the empty check of a counted quantifier reads the count after the iteration's increment, or a greedy choice's second way is the body
        // Broiler-Human:        PENDING
        private void CloseIteration(Iteration iteration)
        {
            var quantifier = iteration.Quantifier;
            Add(RegexOp.RejectEmptyIteration, iteration.Mark, iteration.Counter, quantifier.Min);
            if (iteration.Counter >= 0)
                Add(RegexOp.IncrementCounter, iteration.Counter);
            if (iteration.Loop >= 0)
                Add(RegexOp.Goto, iteration.Loop);

            var body = iteration.Choice + 1;
            var exit = _code.Count;
            if (quantifier.Greedy)
                Patch(iteration.Choice, a: body, b: exit);
            else
                Patch(iteration.Choice, a: exit, b: body);
            if (iteration.AtMaximum >= 0)
                Patch(iteration.AtMaximum, c: exit);
        }

        /// <summary>
        /// Adds a class test. Compared exactly, the class is its code points with its negation applied; under <c>i</c> it
        /// is its members as written, tested against a character's case orbit and inverted after, as CharacterSetMatcher
        /// inverts (§22.2.2.7.1); a <c>v</c>-mode class the parser folded is its canonical members, tested against a
        /// character's Canonicalize.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.1; IP=None; Security=High; Resources=3; Fingerprint=7ACC59
        // Broiler-Falsified-If: the class [^a-c] is given the ranges a to c rather than their complement, [^k] under i is given the complement of k rather than k inverted, or a class's cost is not one plus its weight for each code point its test searches for
        // Broiler-Human:        PENDING
        private void AddClass(CharSet set)
        {
            if (set.CaseFolded)
            {
                _folds = true;
                var canonical = AddRanges(set.CodePoints);
                Add(RegexOp.CanonicalClass, _classes.Count - 1, 0, 0, RegexProgram.ClassCost(canonical, 1));
            }
            else if (_ignoreCase)
            {
                _folds = true;
                var members = AddRanges(set.CodePoints);
                var searches = _unicode ? data.Folding.MaxOrbit : data.Folding.MaxUpperOrbit;
                Add(RegexOp.ClassIgnoringCase, _classes.Count - 1, set.Negated ? 1 : 0, 0, RegexProgram.ClassCost(members, searches));
            }
            else
            {
                var ranges = AddRanges(set.Negated ? set.CodePoints.Complement() : set.CodePoints);
                Add(RegexOp.Class, _classes.Count - 1, 0, 0, RegexProgram.ClassCost(ranges, 1));
            }
        }

        /// <summary>Adds the ranges of <paramref name="codePoints"/> to the program's classes, flattened into pairs, and answers how many there are.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=2; Fingerprint=F61797
        // Broiler-Human:        PENDING
        private int AddRanges(CodePointSet codePoints)
        {
            var ranges = codePoints.Ranges;
            var pairs = new int[2 * ranges.Count];
            for (var i = 0; i < ranges.Count; i++)
            {
                pairs[2 * i] = ranges[i].Lo;
                pairs[(2 * i) + 1] = ranges[i].Hi;
            }

            _classes.Add(pairs);
            return ranges.Count;
        }

        /// <summary>Appends an instruction and answers its address.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=CCA3BE
        // Broiler-Human:        PENDING
        private int Add(RegexOp op, int a = 0, int b = 0, int c = 0, int cost = 1)
        {
            _code.Add(new RegexInstruction(op, a, b, c, cost));
            return _code.Count - 1;
        }

        /// <summary>Rewrites the operands given of the instruction at <paramref name="address"/>.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=7E76E4
        // Broiler-Human:        PENDING
        private void Patch(int address, int? a = null, int? b = null, int? c = null)
        {
            var old = _code[address];
            _code[address] = new RegexInstruction(old.Op, a ?? old.A, b ?? old.B, c ?? old.C, old.Cost);
        }
    }
}
