// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   33
// Annotated:        33/33
// Exempt:           29
// Human-reviewed:   0/33
// IP risk:          None
// Security risk:    High
// Criteria:         21/18
// Resource impact:  5/10 max
// Unverified:       33
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
// Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=E12D19
// Broiler-Human:        PENDING
internal enum ProgramCompilation
{
    /// <summary>The program is built.</summary>
    Compiled = 0,

    /// <summary>The pattern nests deeper than the caller's nesting limit (requirement R4, decisions D6 and D7).</summary>
    NestingLimitExceeded = 1,

    /// <summary>Compiling the pattern would do more work than the caller's compile budget admits (requirement R2, task 2.5).</summary>
    CompileBudgetExceeded = 2,
}

/// <summary>
/// The compiler from the parser's tree to the new backend's <see cref="RegexProgram"/> (decision D5 of Broiler.VM's
/// JSD-0081): task 2.4a's lowering of literal characters, <c>.</c>, classes, sequence, alternation, groups and
/// quantifiers, and task 2.4b's of comparison under <c>i</c>, of the assertions <c>^ $ \b \B</c>, of modifier groups,
/// of back-references, of the four look-arounds, with a look-behind's body lowered right to left, and of the
/// <c>v</c>-mode classes that hold strings. Every tree the parser builds compiles to a program, unless it nests past the
/// caller's limit. It lives outside the machine's namespace, <c>Broiler.Regex.Engine</c>, whose methods the
/// architecture tests hold to having no recursion (bundle <c>sjl-11-001</c>'s check A-4).
/// </summary>
/// <remarks>
/// <para>
/// <b>No recursion, and the nesting limit still checked.</b> The tree is walked twice on work lists of the heap, as the
/// closure compiler of <c>Matching/</c>, deleted at task 2.4c, walked it, so compiling uses the same CLR stack at every
/// depth; decision D6 would admit recursion under the caller's nesting limit, and the walks do not need it. The first walk, which finds each
/// quantifier's groups, calls <see cref="RegexNesting.Descend"/> before it enters a group's, a modifier's or a
/// look-around's body, as the parser did, so a tree the parser did not build cannot be deeper than the limit either;
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
/// <item>a <c>v</c>-mode class holding strings <c>s₁ … sₙ</c>, longest first: the alternation <c>s₁|…|sₙ|C</c>, each
/// string a sequence of its characters, <c>C</c> the class of its single characters, followed by an empty last
/// alternative when the class holds the empty string (§22.2.2.7, Atom :: CharacterClass, steps 3 to 8);</item>
/// <item><c>^</c> and <c>$</c>: <c>LineStart m</c> and <c>LineEnd m</c>, <c>m</c> one when the flag is in force; <c>\b</c>
/// and <c>\B</c>: <c>WordEdge w</c> and <c>NotWordEdge w</c>, <c>w</c> the class of the word characters the
/// flags give; a back-reference: <c>Backreference r, 1</c>, or <c>BackreferenceIgnoringCase r, 1</c> under <c>i</c>,
/// <c>r</c> the entry of the program's list of referenced groups that holds its group;</item>
/// <item><c>X₁|X₂|…|Xₙ</c>: <c>Choice +1, next</c> before each alternative but the last, and a <c>Goto L</c> after it;</item>
/// <item>group <c>k</c>: <c>SavePosition 2k; X; SavePosition 2k+1</c>; a non-capturing group: <c>X</c>; a modifier group:
/// <c>X</c>, lowered under the flags it sets, which every instruction reading <c>i</c>, <c>m</c> or <c>s</c> takes from where
/// it stands;</item>
/// <item>a look-around: <c>OpenLookaround r, negative, L; X; CloseLookaround r, negative</c>, <c>r</c> a register of its
/// own; a look-ahead's <c>X</c> is lowered left to right and a look-behind's right to left, wherever they stand
/// (§22.2.2.6, step 1 of each). Right to left, every character, class and back-reference instruction has
/// <see cref="RegexInstruction.RightToLeft"/> set, a sequence's terms are emitted last first, and group <c>k</c> is
/// <c>SavePosition 2k+1; X; SavePosition 2k</c>, so that its range still reads from its start to its end;</item>
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
/// <para>
/// <b>The fast paths (task 2.4c).</b> With its fast paths on, as the library always compiles, three shapes lower to
/// instructions that charge each character they examine (<see cref="RegexOp"/>'s remarks), in place of the forms above:
/// </para>
/// <list type="bullet">
/// <item>a quantifier over one character, <c>.</c> or a class without strings, <c>X{min,max}</c> greedy or lazy, is
/// <c>GreedyRun min, max</c> or <c>LazyRun min, max</c> followed by <c>X</c>'s own instruction, <c>max</c>
/// <see cref="int.MaxValue"/> when unbounded;</item>
/// <item>two or more such atoms in a row of one sequence, in the order the sequence is matched, are <c>AtomRun k</c>
/// followed by their <c>k</c> instructions, costing the first's cost;</item>
/// <item>a <c>v</c>-mode class holding strings is <c>StringClass t, empty</c> followed by the class of its single
/// characters, <c>t</c> the program's table of its strings, which every occurrence of the same strings read in the same
/// direction shares, so a class of thousands of strings is two instructions and one table however often it occurs.</item>
/// </list>
/// <para>
/// With them off (<c>fastPaths: false</c>, for the differential that holds them to the forms above and for the tests that
/// read those forms), a program holds none of the three, and answers every match the same.
/// </para>
/// <para>
/// <b>Charged (requirement R2, task 2.5).</b> The walks cost a constant amount per node of the tree, which the pattern's
/// units pay for (<see cref="CompileSite.Pattern"/>); what the expansions decide is charged to the compilation's account
/// before it is done (<see cref="CompileSite.Program"/>): each range a class instruction copies, and for a class of strings
/// each string's code units to read it, again for each earlier table it is compared with and for its text, and the
/// table's order; or, with the fast paths off, each string's code units for its lowering.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=5; Fingerprint=393186
// Broiler-Falsified-If: compiling a tree nested to the library's maximum uses more CLR stack than a flat one, a group's, a modifier's or a look-around's body is entered without checking the caller's nesting limit, or a tree the parser built answers anything but a program or the nesting status
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
    /// <param name="program">The program, or null when the nesting limit or the compile budget refused the pattern.</param>
    /// <param name="fastPaths">Whether the program takes the fast paths (the remarks), as every program the library builds does.</param>
    /// <param name="charge">The compilation's account, with the caller's meter and compile budget (task 2.5), or null for none.</param>
    /// <exception cref="RegexSyntaxException">The pattern is not valid under the flags, as the parser refuses it.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> or <paramref name="tables"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nestingLimit"/> is less than 1 or greater than <see cref="BroilerRegex.MaximumNestingLimit"/>.</exception>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=5; Fingerprint=382899
    // Broiler-Falsified-If: a pattern of 512 nested groups under a limit of 512 answers Compiled, one of 511 answers NestingLimitExceeded, or a compilation past its budget answers Compiled
    // Broiler-Human:        PENDING
    internal static ProgramCompilation Compile(string pattern, RegexFlags flags, IUnicodeTables tables, int nestingLimit, out RegexProgram? program, bool fastPaths = true,
        CompileCharge? charge = null)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentOutOfRangeException.ThrowIfLessThan(nestingLimit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nestingLimit, BroilerRegex.MaximumNestingLimit);

        var nesting = new RegexNesting(nestingLimit);
        var data = new RegexUnicode(tables, charge);
        RegexNode tree;
        int captureCount;
        IReadOnlyDictionary<string, int> groupNames;
        try
        {
            tree = RegexParser.Parse(pattern, flags, data, nesting, out captureCount, out groupNames);
            return Compile(tree, captureCount, groupNames, flags, data, nesting, out program, fastPaths);
        }
        catch (NestingLimitExceeded)
        {
            program = null;
            return ProgramCompilation.NestingLimitExceeded;
        }
        catch (CompileBudgetSpent)
        {
            program = null;
            return ProgramCompilation.CompileBudgetExceeded;
        }
    }

    /// <summary>Compiles a tree the parser built under <paramref name="flags"/> to a program.</summary>
    /// <param name="tree">The parser's tree.</param>
    /// <param name="captureCount">The pattern's capturing groups, as the parser counted them.</param>
    /// <param name="groupNames">The parser's map of group names to group numbers, which a named back-reference reads.</param>
    /// <param name="flags">The flags the tree was parsed under.</param>
    /// <param name="data">The Unicode answers of the compilation the parser read, whose case folding the program keeps and whose account the lowering charges.</param>
    /// <param name="nesting">The caller's nesting limit, checked again, by the first walk, before each group's body is entered.</param>
    /// <param name="program">The program, or null when the nesting limit refused the tree.</param>
    /// <param name="fastPaths">Whether the program takes the fast paths (the remarks), as every program the library builds does.</param>
    /// <exception cref="ArgumentException"><paramref name="tree"/> holds a node of a type the parser does not build.</exception>
    /// <exception cref="CompileBudgetSpent">The lowering would take the compilation past the caller's compile budget.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=5; Fingerprint=DD0361
    // Broiler-Falsified-If: a tree the parser built within the nesting limit is answered without a program, a character under the i flag in force is given an instruction that compares it exactly, or one in a look-behind's body an instruction that reads left to right
    // Broiler-Human:        PENDING
    internal static ProgramCompilation Compile(RegexNode tree, int captureCount, IReadOnlyDictionary<string, int> groupNames,
        RegexFlags flags, RegexUnicode data, RegexNesting nesting, out RegexProgram? program, bool fastPaths = true)
    {
        program = null;
        try
        {
            var groups = Survey(tree, nesting);
            program = new ProgramWriter(captureCount, groupNames, flags, data, groups, fastPaths).Emit(tree);
            return ProgramCompilation.Compiled;
        }
        catch (NestingLimitExceeded)
        {
            program = null;
            return ProgramCompilation.NestingLimitExceeded;
        }
    }

    /// <summary>
    /// Walks the tree once before anything is emitted, and finds the capture groups of each quantifier's body, the
    /// registers its iterations reset.
    /// </summary>
    /// <exception cref="NestingLimitExceeded">The tree nests deeper than <paramref name="nesting"/>.</exception>
    /// <exception cref="ArgumentException">The tree holds a node of a type the parser does not build.</exception>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=4; Fingerprint=DDC27C
    // Broiler-Falsified-If: the groups found for the quantifier of /((a)|(b))*/ are other than 1 to 3, or for the inner one of /(a)(?:(b)(c)+)*/ other than 3 to 3
    // Broiler-Human:        PENDING
    private static Dictionary<QuantifierNode, (int First, int Last)> Survey(RegexNode tree, RegexNesting nesting)
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
                case EmptyNode or CharNode or AnyCharNode or AnchorNode or BackreferenceNode or CharClassNode:
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
                case ModifierGroupNode modifier:
                    pending.Push((modifier.Child, nesting.Descend(depth, -1), false));
                    break;
                case LookaroundNode look:
                    pending.Push((look.Child, nesting.Descend(depth, -1), false));
                    break;
                case QuantifierNode quantifier:
                    open.Add((0, -1));
                    pending.Push((quantifier, depth, true));
                    pending.Push((quantifier.Child, depth, false));
                    break;
                default:
                    throw new ArgumentException("The tree holds a node of a type the parser does not build.", nameof(tree));
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
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=EC1FF0
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

        /// <summary>End a look-around's body: its close, and its start's continuation pointed past it.</summary>
        CloseLookaround,

        /// <summary>Fold a sequence's atoms, <see cref="Step.Count"/> of them from the <see cref="Step.Index"/>-th in the order it is matched, into an atom run.</summary>
        FoldAtoms,
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

    /// <summary>
    /// The flags in force where a node stands: the pattern's own, changed by every modifier group around it (ES2025's
    /// <c>(?ims-ims:…)</c>, whose body is compiled with rer's IgnoreCase, Multiline and DotAll updated, §22.2.2.7); and the
    /// direction it is compiled in, right to left in a look-behind's body and left to right elsewhere, a look-ahead's body
    /// in a look-behind's included.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7; IP=None; Security=Medium; Resources=0; Fingerprint=03367C
    // Broiler-Falsified-If: a flag a modifier group removes stays in force in its body, one it adds stays in force after it, or a modifier group changes the direction
    // Broiler-Human:        PENDING
    private readonly record struct Scope(bool IgnoreCase, bool Multiline, bool DotAll, bool RightToLeft)
    {
        /// <summary>The flags in force in the body of a modifier group that adds <paramref name="added"/> and removes <paramref name="removed"/>.</summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7; IP=None; Security=Medium; Resources=0; Fingerprint=5E8FDF
        // Broiler-Falsified-If: With(None, Multiline) leaves Multiline set, or With(IgnoreCase, None) leaves IgnoreCase clear
        // Broiler-Human:        PENDING
        internal Scope With(RegexFlags added, RegexFlags removed) => new(
            Updated(IgnoreCase, RegexFlags.IgnoreCase, added, removed),
            Updated(Multiline, RegexFlags.Multiline, added, removed),
            Updated(DotAll, RegexFlags.DotAll, added, removed),
            RightToLeft);

        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=4CB217
        // Broiler-Human:        PENDING
        private static bool Updated(bool held, RegexFlags flag, RegexFlags added, RegexFlags removed) =>
            (held || (added & flag) != 0) && (removed & flag) == 0;
    }

    /// <summary>One work-list entry: what to do, to which node under which flags, with which pending lowering.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=FE0920
    // Broiler-Human:        PENDING
    private readonly record struct Step(Work Work, RegexNode? Node, Scope Scope = default, int Index = 0, Alternation? Alternation = null, Iteration? Iteration = null, int Count = 0);

    /// <summary>A table of strings the writer built, with the strings, the direction and the folding it was built for.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=D758C3
    // Broiler-Human:        PENDING
    private readonly record struct StringTable(IReadOnlyList<string> Strings, bool RightToLeft, bool Folded, RegexStrings Table);

    /// <summary>The lowering of one tree into one program.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=5; Fingerprint=06D39B
    // Broiler-Falsified-If: a lazy quantifier's choice tries the body before the continuation, or an alternation's choice resumes anywhere but at the next alternative
    // Broiler-Human:        PENDING
    private sealed class ProgramWriter(int captureCount, IReadOnlyDictionary<string, int> groupNames, RegexFlags flags, RegexUnicode data,
        Dictionary<QuantifierNode, (int First, int Last)> groups, bool fastPaths)
    {
        private readonly List<RegexInstruction> _code = [];
        private readonly List<int[]> _classes = [];

        /// <summary>The groups the back-references read, each reference's list one run of entries.</summary>
        private readonly List<int> _referenced = [];

        /// <summary>The tables of strings built so far, each shared by every occurrence of its strings in its direction and folding.</summary>
        private readonly List<StringTable> _strings = [];

        /// <summary>The compilation's account, which the lowering charges before what the expansions decide (task 2.5).</summary>
        private readonly CompileCharge _charge = data.Charge;
        private readonly bool _unicode = flags.IsUnicodeMode();

        /// <summary>The class of the word characters compared exactly, then under <c>i</c>, once it is added; -1 before.</summary>
        private readonly int[] _wordClasses = [-1, -1];

        /// <summary>Whether an instruction compares under <c>i</c>, so that the program keeps the provider's case folding.</summary>
        private bool _folds;

        /// <summary>The next free register: the capture registers come first, two per group, group 0's included.</summary>
        private int _registers = 2 * (captureCount + 1);

        /// <summary>The program of <paramref name="tree"/>, which the first walk read, ending in <see cref="RegexOp.Succeed"/>.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=5; Fingerprint=06C840
        // Broiler-Falsified-If: an alternation's choice resumes at an address other than the next alternative's first instruction, or a group's end is written before its body
        // Broiler-Human:        PENDING
        internal RegexProgram Emit(RegexNode tree)
        {
            var work = new Stack<Step>();
            var scope = new Scope((flags & RegexFlags.IgnoreCase) != 0, (flags & RegexFlags.Multiline) != 0, (flags & RegexFlags.DotAll) != 0, false);
            work.Push(new Step(Work.Visit, tree, scope));
            while (work.Count > 0)
            {
                var step = work.Pop();
                switch (step.Work)
                {
                    case Work.Visit:
                        Visit(step.Node!, step.Scope, work);
                        break;
                    case Work.CloseGroup:
                        Add(RegexOp.SavePosition, step.Scope.RightToLeft ? 2 * step.Index : (2 * step.Index) + 1);
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
                    case Work.CloseLookaround:
                        var open = _code[step.Index];
                        Add(RegexOp.CloseLookaround, open.A, open.B);
                        Patch(step.Index, c: _code.Count);
                        break;
                    case Work.FoldAtoms:
                        AddAtomRun((SequenceNode)step.Node!, step.Index, step.Count, step.Scope);
                        break;
                }
            }

            Add(RegexOp.Succeed);

            var strings = new RegexStrings[_strings.Count];
            for (var i = 0; i < strings.Length; i++)
                strings[i] = _strings[i].Table;
            return new RegexProgram(_code.ToArray(), _classes.ToArray(), _referenced.ToArray(), strings, captureCount, _registers, _unicode, _folds ? data.Folding : null,
                fastPaths ? FirstTest() : -1);
        }

        /// <summary>
        /// The literal-prefix prefilter (<see cref="RegexProgram.FirstTest"/>): past the instructions at the start that only
        /// write a register, the one-character test every attempt makes first, read left to right: a character or class
        /// instruction, an atom run's first atom, or the atom of a run that requires a character; -1 for anything else.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2; IP=None; Security=High; Resources=1; Fingerprint=F1B6B9
        // Broiler-Falsified-If: a pattern that can match without reading a character, such as a*b, a|b, ^a or (?=a)a, is given a first test, or one that reads past a choice is
        // Broiler-Human:        PENDING
        private int FirstTest()
        {
            var at = 0;
            while (_code[at].Op is RegexOp.SavePosition or RegexOp.ClearCaptures or RegexOp.SetCounter)
                at++;

            var first = _code[at];
            if (first.RightToLeft)
                return -1;
            return first.Op switch
            {
                RegexOp.Literal or RegexOp.Class or RegexOp.AnyButLineTerminator or RegexOp.Any or RegexOp.LiteralIgnoringCase
                    or RegexOp.ClassIgnoringCase or RegexOp.CanonicalClass => at,
                RegexOp.AtomRun => at + 1,
                RegexOp.GreedyRun or RegexOp.LazyRun when first.A >= 1 => at + 1,
                _ => -1,
            };
        }

        /// <summary>Lowers what can be lowered of <paramref name="node"/> under <paramref name="scope"/> now, and puts the rest on the work list.</summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=High; Resources=4; Fingerprint=0ADCCD
        // Broiler-Falsified-If: an alternation's alternatives are emitted in an order other than the pattern's, or a sequence's terms in an order other than the pattern's outside a look-behind's body and other than its reverse inside one
        // Broiler-Human:        PENDING
        private void Visit(RegexNode node, Scope scope, Stack<Step> work)
        {
            switch (node)
            {
                case EmptyNode:
                    break;
                case CharClassNode cls when cls.Set.HasStrings:
                    AddStrings(cls.Set, scope);
                    break;
                case CharNode or AnyCharNode or CharClassNode:
                    AddAtom(node, scope);
                    break;
                case AnchorNode anchor:
                    AddAnchor(anchor.Kind, scope);
                    break;
                case BackreferenceNode reference:
                    AddReference(reference, scope);
                    break;
                case SequenceNode sequence:
                {
                    // The terms in the order they are matched, the last first right to left (§22.2.2.3, Alternative ::
                    // Alternative Term with direction backward: the term is matched first), pushed so that the first
                    // comes off the work list first. With the fast paths, each row of two or more atoms is one step that
                    // folds them.
                    for (var end = sequence.Terms.Count; end > 0;)
                    {
                        var start = end - 1;
                        if (fastPaths && IsAtom(MatchedTerm(sequence, start, scope)))
                        {
                            while (start > 0 && IsAtom(MatchedTerm(sequence, start - 1, scope)))
                                start--;
                        }

                        work.Push(end - start >= 2
                            ? new Step(Work.FoldAtoms, sequence, scope, Index: start, Count: end - start)
                            : new Step(Work.Visit, MatchedTerm(sequence, start, scope), scope));
                        end = start;
                    }

                    break;
                }
                case DisjunctionNode disjunction:
                {
                    var alternation = new Alternation(disjunction.Alternatives.Count);
                    work.Push(new Step(Work.CloseAlternation, null, Alternation: alternation));
                    for (var i = disjunction.Alternatives.Count - 1; i >= 0; i--)
                    {
                        work.Push(new Step(Work.CloseAlternative, null, Index: i, Alternation: alternation));
                        work.Push(new Step(Work.Visit, disjunction.Alternatives[i], scope));
                        work.Push(new Step(Work.OpenAlternative, null, Index: i, Alternation: alternation));
                    }

                    break;
                }

                case GroupNode group:
                    if (group.IsCapturing)
                    {
                        // Backward, the body is entered where the range ends and leaves where it starts (§22.2.2.7,
                        // CaptureRange(ye, xe)).
                        Add(RegexOp.SavePosition, scope.RightToLeft ? (2 * group.CaptureIndex) + 1 : 2 * group.CaptureIndex);
                        work.Push(new Step(Work.CloseGroup, null, scope, Index: group.CaptureIndex));
                    }

                    work.Push(new Step(Work.Visit, group.Child, scope));
                    break;
                case ModifierGroupNode modifier:
                    work.Push(new Step(Work.Visit, modifier.Child, scope.With(modifier.Added, modifier.Removed)));
                    break;
                case LookaroundNode look:
                {
                    var open = Add(RegexOp.OpenLookaround, _registers++, look.Negative ? 1 : 0, -1);
                    work.Push(new Step(Work.CloseLookaround, null, Index: open));
                    work.Push(new Step(Work.Visit, look.Child, scope with { RightToLeft = look.Behind }));
                    break;
                }

                case QuantifierNode quantifier:
                    OpenIteration(quantifier, scope, work);
                    break;
            }
        }

        /// <summary>
        /// Begins a quantifier's lowering: its registers, the bound checks and the choice, the iteration's start and
        /// capture reset; the body and <see cref="Work.CloseIteration"/> go on the work list.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.3.1; IP=None; Security=High; Resources=3; Fingerprint=F39735
        // Broiler-Falsified-If: X{0} emits X, a quantifier with a minimum of zero emits an EnterBelowMinimum, or one with an unbounded maximum an ExitAtMaximum
        // Broiler-Human:        PENDING
        private void OpenIteration(QuantifierNode quantifier, Scope scope, Stack<Step> work)
        {
            var min = quantifier.Min;
            var max = quantifier.Max;

            // RepeatMatcher with a maximum of zero returns its continuation at once (§22.2.2.3.1, step 1).
            if (max == 0)
                return;

            // A quantifier over one character is a run: one instruction, and its atom after it.
            if (fastPaths && IsAtom(quantifier.Child))
            {
                Add(quantifier.Greedy ? RegexOp.GreedyRun : RegexOp.LazyRun, min, max == QuantifierNode.Unbounded ? int.MaxValue : max, rightToLeft: scope.RightToLeft);
                AddAtom(quantifier.Child, scope);
                return;
            }

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
            work.Push(new Step(Work.Visit, quantifier.Child, scope));
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
        /// Whether <paramref name="node"/> is an atom of one character, the body a run repeats and an atom run folds: a
        /// pattern character, <c>.</c>, or a class without strings, each of which consumes exactly one character when it
        /// matches, at most one way, and holds no group.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7; IP=None; Security=High; Resources=0; Fingerprint=B770CA
        // Broiler-Falsified-If: a class holding strings, a group of one character or an assertion is taken for an atom
        // Broiler-Human:        PENDING
        private static bool IsAtom(RegexNode node) => node is CharNode or AnyCharNode || node is CharClassNode { Set.HasStrings: false };

        /// <summary>
        /// The <paramref name="index"/>-th term of <paramref name="sequence"/> in the order it is matched: the pattern's order,
        /// or right to left its reverse (§22.2.2.3, Alternative :: Alternative Term with direction backward).
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.3; IP=None; Security=Medium; Resources=0; Fingerprint=578969
        // Broiler-Human:        PENDING
        private static RegexNode MatchedTerm(SequenceNode sequence, int index, Scope scope) =>
            sequence.Terms[scope.RightToLeft ? sequence.Terms.Count - 1 - index : index];

        /// <summary>
        /// Adds the instruction of an atom of one character (<see cref="IsAtom"/>): <c>Literal c</c>, or under <c>i</c>
        /// <c>LiteralIgnoringCase</c> with its Canonicalize; <c>AnyButLineTerminator</c>, or <c>Any</c> under <c>s</c>; a class
        /// test (<see cref="AddClass"/>).
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7; IP=None; Security=High; Resources=1; Fingerprint=DDB1B9
        // Broiler-Falsified-If: a character under the i flag in force is given an instruction that compares it exactly, or the dot under s one that refuses a line terminator
        // Broiler-Human:        PENDING
        private void AddAtom(RegexNode node, Scope scope)
        {
            switch (node)
            {
                case CharNode character when scope.IgnoreCase:
                    _folds = true;
                    Add(RegexOp.LiteralIgnoringCase, data.Canonicalize(character.CodePoint, _unicode), rightToLeft: scope.RightToLeft);
                    break;
                case CharNode character:
                    Add(RegexOp.Literal, character.CodePoint, rightToLeft: scope.RightToLeft);
                    break;
                case AnyCharNode:
                    Add(scope.DotAll ? RegexOp.Any : RegexOp.AnyButLineTerminator, rightToLeft: scope.RightToLeft);
                    break;
                case CharClassNode cls:
                    AddClass(cls.Set, scope);
                    break;
            }
        }

        /// <summary>
        /// Adds an atom run: <c>AtomRun count</c>, then the <paramref name="count"/> atoms of <paramref name="sequence"/> from
        /// its <paramref name="first"/>-th term in the order it is matched, the run costing what its first atom costs.
        /// </summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=1; Fingerprint=FFC2CE
        // Broiler-Falsified-If: a run's cost is not its first atom's, or its atoms are emitted in an order other than the sequence's matched order
        // Broiler-Human:        PENDING
        private void AddAtomRun(SequenceNode sequence, int first, int count, Scope scope)
        {
            var run = Add(RegexOp.AtomRun, count, rightToLeft: scope.RightToLeft);
            for (var k = 0; k < count; k++)
                AddAtom(MatchedTerm(sequence, first + k, scope), scope);
            _code[run] = new RegexInstruction(RegexOp.AtomRun, count, 0, 0, _code[run + 1].Cost, scope.RightToLeft);
        }

        /// <summary>
        /// The index of the program's table of the strings of more than one character of <paramref name="set"/>, read
        /// right to left when <paramref name="rightToLeft"/> is set: an earlier occurrence's table when it holds the same
        /// strings in the same order, read the same way, under the same folding, and otherwise a new one. Each string's
        /// code points are its own, combining a surrogate pair into one, and under a fold each one's Canonicalize, as the
        /// alternation's instructions would compare them.
        /// </summary>
        /// <remarks>
        /// The strings are charged their code units before they are read, and again for each earlier table of the same
        /// direction and folding they are compared with, string by string, before the comparison; a new table's text is
        /// charged the strings' code units, and its order, the strings grouped by the character they begin with, a sort's
        /// 1 + ⌊log₂ n⌋ per string, before it is built (<see cref="CompileSite.Program"/>).
        /// </remarks>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7; IP=None; Security=High; Resources=3; Fingerprint=909E2C
        // Broiler-Falsified-If: two occurrences of \p{RGI_Emoji} in one pattern get two tables, two classes of different strings share one, or a folded class's table holds a code point other than its Canonicalize
        // Broiler-Human:        PENDING
        private int StringTableOf(CharSet set, bool rightToLeft)
        {
            var strings = new List<string>();
            var units = 0L;
            foreach (var written in set.Strings)
            {
                if (written.Length > 0)
                {
                    strings.Add(written);
                    units += written.Length;
                }
            }

            _charge.Charge(CompileSite.Program, units + 1);
            for (var i = 0; i < _strings.Count; i++)
            {
                var known = _strings[i];
                if (known.RightToLeft != rightToLeft || known.Folded != set.CaseFolded)
                    continue;

                _charge.Charge(CompileSite.Program, units + 1);
                if (SameStrings(known.Strings, strings))
                    return i;
            }

            _charge.Charge(CompileSite.Program, units + ((long)strings.Count * CompileCharge.SortFactor(strings.Count)));
            if (set.CaseFolded)
                _folds = true;

            // Each string's code points, then the strings grouped by the character a match of one begins with, its first
            // or, right to left, its last, each group in the class's order; the groups in the order of their characters.
            var textStarts = new int[strings.Count + 1];
            var text = new List<int>();
            var groups = new SortedDictionary<int, List<int>>();
            for (var number = 0; number < strings.Count; number++)
            {
                textStarts[number] = text.Count;
                var value = strings[number];
                for (var at = 0; at < value.Length; at++)
                {
                    var codePoint = char.IsSurrogatePair(value, at) ? char.ConvertToUtf32(value[at], value[++at]) : value[at];
                    text.Add(set.CaseFolded ? data.Canonicalize(codePoint, _unicode) : codePoint);
                }

                var key = text[rightToLeft ? text.Count - 1 : textStarts[number]];
                if (!groups.TryGetValue(key, out var group))
                    groups.Add(key, group = []);
                group.Add(number);
            }

            textStarts[strings.Count] = text.Count;
            var keys = new int[groups.Count];
            var groupStarts = new int[groups.Count + 1];
            var members = new int[strings.Count];
            var groupEnds = new int[strings.Count];
            var next = 0;
            var index = 0;
            foreach (var (key, group) in groups)
            {
                keys[index] = key;
                groupStarts[index++] = next;
                foreach (var number in group)
                    members[next++] = number;
                for (var member = groupStarts[index - 1]; member < next; member++)
                    groupEnds[member] = next;
            }

            groupStarts[groups.Count] = next;
            _strings.Add(new StringTable(strings, rightToLeft, set.CaseFolded,
                new RegexStrings(keys, groupStarts, members, groupEnds, textStarts, text.ToArray(), rightToLeft, set.CaseFolded)));
            return _strings.Count - 1;
        }

        /// <summary>Whether two lists hold the same strings in the same order, compared by code unit.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=6AAC00
        // Broiler-Human:        PENDING
        private static bool SameStrings(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            if (a.Count != b.Count)
                return false;
            for (var i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Adds a class test. Compared exactly, the class is its code points with its negation applied; under <c>i</c> it
        /// is its members as written, tested against a character's case orbit and inverted after, as CharacterSetMatcher
        /// inverts (§22.2.2.7.1); a <c>v</c>-mode class the parser folded is its canonical members, tested against a
        /// character's Canonicalize.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.1; IP=None; Security=High; Resources=3; Fingerprint=3DE6EB
        // Broiler-Falsified-If: the class [^a-c] is given the ranges a to c rather than their complement, [^k] under i is given the complement of k rather than k inverted, or a class's cost is not one plus its weight for each code point its test searches for
        // Broiler-Human:        PENDING
        private void AddClass(CharSet set, Scope scope)
        {
            if (set.CaseFolded)
            {
                _folds = true;
                var canonical = AddRanges(set.CodePoints);
                Add(RegexOp.CanonicalClass, _classes.Count - 1, 0, 0, RegexProgram.ClassCost(canonical, 1), scope.RightToLeft);
            }
            else if (scope.IgnoreCase)
            {
                _folds = true;
                var members = AddRanges(set.CodePoints);
                var searches = _unicode ? data.Folding.MaxOrbit : data.Folding.MaxUpperOrbit;
                Add(RegexOp.ClassIgnoringCase, _classes.Count - 1, set.Negated ? 1 : 0, 0, RegexProgram.ClassCost(members, searches), scope.RightToLeft);
            }
            else
            {
                var ranges = AddRanges(set.Negated ? set.CodePoints.Complement(_charge) : set.CodePoints);
                Add(RegexOp.Class, _classes.Count - 1, 0, 0, RegexProgram.ClassCost(ranges, 1), scope.RightToLeft);
            }
        }

        /// <summary>
        /// Adds a <c>v</c>-mode class holding strings (§22.2.2.7, Atom :: CharacterClass, steps 3 to 8): an alternation of
        /// each string of more than one character, longest first as the parser sorted them, each a sequence of its
        /// characters compared as a one-member class is, then the class of the single characters, then, when the class
        /// holds the empty string, an empty alternative. In a look-behind's body each string's characters are emitted
        /// last first, as MatchSequence with direction backward matches them. With the fast paths it is instead
        /// <c>StringClass</c> over the program's table of the strings (<see cref="StringTableOf"/>), then the class of the
        /// single characters: the same ways in the same order, with the strings that cannot match left untried.
        /// </summary>
        /// <remarks>
        /// The parser sorts the strings by their length in code units; the specification orders them by their length in
        /// characters, and leaves strings of one length in any order. The two orders differ only between strings neither
        /// of which begins the other, and of two such strings at most one can match at an index, so the alternation answers
        /// the same. Under <c>i</c> the strings were folded when parsed, so each character's Canonicalize is itself. The
        /// table's dispatch costs one and its weight, the floor of the base-2 logarithm of the number of characters strings
        /// begin with, as a class of that many ranges is weighed (<see cref="RegexProgram.ClassWeight"/>).
        /// </remarks>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7; IP=None; Security=High; Resources=2; Fingerprint=C7894A
        // Broiler-Falsified-If: [\q{abc|ab}]c fails on abc, [\q{}a] on a matches the empty string, or in a look-behind a string's first character is read first
        // Broiler-Human:        PENDING
        private void AddStrings(CharSet set, Scope scope)
        {
            if (fastPaths)
            {
                var index = StringTableOf(set, scope.RightToLeft);
                var cost = 1 + RegexProgram.ClassWeight(_strings[index].Table.KeyCount);
                Add(RegexOp.StringClass, index, set.MatchesEmptyString ? 1 : 0, 0, cost, scope.RightToLeft);
                AddClass(set, scope);
                return;
            }

            var exits = new List<int>();
            var characters = new List<int>();
            foreach (var text in set.Strings)
            {
                if (text.Length == 0)
                    continue;

                _charge.Charge(CompileSite.Program, text.Length);
                var choice = Add(RegexOp.Choice, _code.Count + 1, -1);
                characters.Clear();
                for (var at = 0; at < text.Length; at++)
                {
                    if (char.IsSurrogatePair(text, at))
                        characters.Add(char.ConvertToUtf32(text[at], text[++at]));
                    else
                        characters.Add(text[at]);
                }

                if (scope.RightToLeft)
                    characters.Reverse();
                foreach (var character in characters)
                {
                    if (set.CaseFolded)
                    {
                        _folds = true;
                        Add(RegexOp.LiteralIgnoringCase, data.Canonicalize(character, _unicode), rightToLeft: scope.RightToLeft);
                    }
                    else
                    {
                        Add(RegexOp.Literal, character, rightToLeft: scope.RightToLeft);
                    }
                }

                exits.Add(Add(RegexOp.Goto, -1));
                Patch(choice, b: _code.Count);
            }

            if (set.MatchesEmptyString)
            {
                var choice = Add(RegexOp.Choice, _code.Count + 1, -1);
                AddClass(set, scope);
                exits.Add(Add(RegexOp.Goto, -1));
                Patch(choice, b: _code.Count);
            }
            else
            {
                AddClass(set, scope);
            }

            foreach (var exit in exits)
                Patch(exit, a: _code.Count);
        }

        /// <summary>
        /// Adds an assertion (§22.2.2.6): <c>^</c> and <c>$</c> with the <c>m</c> flag in force as their operand, and
        /// <c>\b</c> and <c>\B</c> with the class of the word characters the flags give (WordCharacters, §22.2.2.9.3),
        /// which costs one unit and the class's weight for each of the two characters tested.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.6; IP=None; Security=High; Resources=1; Fingerprint=7D25B8
        // Broiler-Falsified-If: under u with i a word boundary reads a word class without U+017F, or without i or without u one with it
        // Broiler-Human:        PENDING
        private void AddAnchor(AnchorKind kind, Scope scope)
        {
            switch (kind)
            {
                case AnchorKind.StartOfInput:
                    Add(RegexOp.LineStart, scope.Multiline ? 1 : 0);
                    return;
                case AnchorKind.EndOfInput:
                    Add(RegexOp.LineEnd, scope.Multiline ? 1 : 0);
                    return;
            }

            var folded = scope.IgnoreCase && _unicode ? 1 : 0;
            if (_wordClasses[folded] < 0)
            {
                AddRanges(data.EscapeSet(ClassEscape.Word, scope.IgnoreCase, _unicode));
                _wordClasses[folded] = _classes.Count - 1;
            }

            var words = _wordClasses[folded];
            var cost = 1 + (2 * RegexProgram.ClassWeight(_classes[words].Length / 2));
            Add(kind == AnchorKind.WordBoundary ? RegexOp.WordEdge : RegexOp.NotWordEdge, words, 0, 0, cost);
        }

        /// <summary>
        /// Adds a back-reference (§22.2.2.7.2): its group, by number or by name, as a list of one entry in the program's
        /// list of referenced groups, compared exactly or, with <c>i</c> in force, by Canonicalize. A number past the
        /// pattern's groups, or a name no group declares, which the parser never answers with, matches the empty string,
        /// as a reference to a group that has not participated does.
        /// </summary>
        // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.2; IP=None; Security=High; Resources=1; Fingerprint=D6ABF6
        // Broiler-Falsified-If: \k<n> is given another group than the one the parser named n, or a reference under the i flag in force compares exactly
        // Broiler-Human:        PENDING
        private void AddReference(BackreferenceNode reference, Scope scope)
        {
            var group = reference.Name is null ? reference.Index : groupNames.TryGetValue(reference.Name, out var named) ? named : 0;
            if (group < 1 || group > captureCount)
                return;

            _referenced.Add(group);
            if (scope.IgnoreCase)
                _folds = true;
            Add(scope.IgnoreCase ? RegexOp.BackreferenceIgnoringCase : RegexOp.Backreference, _referenced.Count - 1, 1, rightToLeft: scope.RightToLeft);
        }

        /// <summary>
        /// Adds the ranges of <paramref name="codePoints"/> to the program's classes, flattened into pairs, and answers how
        /// many there are; charged a unit per range before they are copied (<see cref="CompileSite.Program"/>).
        /// </summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=2; Fingerprint=7D3E6A
        // Broiler-Falsified-If: a range is copied before a unit is charged for it
        // Broiler-Human:        PENDING
        private int AddRanges(CodePointSet codePoints)
        {
            _charge.Charge(CompileSite.Program, codePoints.RangeCount);
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

        /// <summary>Appends an instruction, reading right to left when <paramref name="rightToLeft"/> is set, and answers its address.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=4759F7
        // Broiler-Human:        PENDING
        private int Add(RegexOp op, int a = 0, int b = 0, int c = 0, int cost = 1, bool rightToLeft = false)
        {
            _code.Add(new RegexInstruction(op, a, b, c, cost, rightToLeft));
            return _code.Count - 1;
        }

        /// <summary>Rewrites the operands given of the instruction at <paramref name="address"/>.</summary>
        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=E5AB5B
        // Broiler-Human:        PENDING
        private void Patch(int address, int? a = null, int? b = null, int? c = null)
        {
            var old = _code[address];
            _code[address] = new RegexInstruction(old.Op, a ?? old.A, b ?? old.B, c ?? old.C, old.Cost, old.RightToLeft);
        }
    }
}
