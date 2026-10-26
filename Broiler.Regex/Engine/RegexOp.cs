// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           29
// Human-reviewed:   0/1
// IP risk:          None
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       1
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.Regex.Engine;

/// <summary>
/// What one instruction of a <see cref="RegexProgram"/> does: the instruction set of the backtracking machine that runs
/// ECMA-262's Pattern semantics (§22.2.2) as a program rather than as a chain of continuations (requirement R4's matcher
/// half, decision D5 of Broiler.VM's JSD-0081).
/// </summary>
/// <remarks>
/// <para>
/// <b>Operands.</b> Every instruction has three integer operands, <c>A</c>, <c>B</c> and <c>C</c>, which its member below
/// names, and a cost in work units (<see cref="RegexInstruction.Cost"/>). A register is an integer cell of the match: the
/// two capture registers of group <c>k</c> are <c>2k</c> and <c>2k + 1</c>, and the registers above them hold the loop
/// state of the program's quantifiers. Every register write goes through the undo trail, so that backtracking restores
/// the registers as the specification's continuation would have seen them.
/// </para>
/// <para>
/// <b>What each task added.</b> Task 2.4a's: literal characters, <c>.</c> with and without <c>s</c>, character classes,
/// sequence, alternation, greedy and lazy quantifiers with counted bounds and the specification's empty-iteration rule,
/// and capturing and non-capturing groups with a quantified atom's captures cleared at each iteration. Task 2.4b's:
/// characters and classes compared under <c>i</c>, the assertions <c>^ $ \b \B</c>, back-references and the four
/// look-arounds, with the backward direction a look-behind's body reads in. Task 2.4c's are the fast paths, each an
/// instruction that charges the meter for every character it examines (decision D8 of Broiler.VM's JSD-0081): the greedy
/// and the lazy run of one character or class (<see cref="GreedyRun"/>, <see cref="LazyRun"/>), atom-run folding
/// (<see cref="AtomRun"/>) and a class of strings read through a table its occurrences share (<see cref="StringClass"/>).
/// The program compiler emits each only when its fast paths are on, as they are for every program the library builds; a
/// program compiled with them off holds none, and answers every match the same (the fast paths' differential). The
/// numbers below are not a contract: a program is built and run by the same version of the library.
/// </para>
/// <para>
/// <b>An instruction that owns the ones after it.</b> A run is followed by its atom, the one-character instruction
/// (<see cref="Literal"/>, <see cref="Class"/>, <see cref="AnyButLineTerminator"/>, <see cref="Any"/>,
/// <see cref="LiteralIgnoringCase"/>, <see cref="ClassIgnoringCase"/> or <see cref="CanonicalClass"/>) it repeats; an
/// atom run by the atoms it folds; a class of strings by the class test of its single characters. The owner reads them
/// as data, with their operands, their direction and their cost, and goes on after them: no path dispatches an owned
/// instruction, and no jump lands on one.
/// </para>
/// <para>
/// <b>Direction.</b> An instruction with <see cref="RegexInstruction.RightToLeft"/> set stands in a look-behind's body,
/// which the specification compiles with direction backward: each member below that reads "the next character" then
/// reads the character that ends at the index, a surrogate pair under <c>u</c> and <c>v</c> included, and moves the index
/// to where it starts (§22.2.2.7.1, step 1, with direction backward). The compiler emits a backward sequence's terms
/// right to left and a backward group's end before its start (§22.2.2.3, Alternative :: Alternative Term, and §22.2.2.7,
/// the group's CaptureRange(ye, xe)), so no other instruction reads the direction.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2; IP=None; Security=Medium; Resources=0; Fingerprint=ECD892
// Broiler-Human:        PENDING
internal enum RegexOp : byte
{
    /// <summary>
    /// The matcher of a pattern character (§22.2.2.7, Atom :: PatternCharacter): CharacterSetMatcher over the one-member
    /// set of code point <c>A</c>, with no case folding. The input's next character must be that code point; under
    /// <c>u</c> and <c>v</c> a character is a code point, a surrogate pair read as one, and otherwise a code unit.
    /// </summary>
    Literal = 0,

    /// <summary>CharacterSetMatcher (§22.2.2.7.1) over class number <c>A</c>: the next character must be one of its code points.</summary>
    Class = 1,

    /// <summary>
    /// The matcher of Atom :: <c>.</c> without <c>s</c> (§22.2.2.7): the next character must be other than a
    /// LineTerminator.
    /// </summary>
    AnyButLineTerminator = 2,

    /// <summary>The matcher of Atom :: <c>.</c> under <c>s</c>: any next character.</summary>
    Any = 3,

    /// <summary>
    /// Two ways on, tried in order, as MatchTwoAlternatives tries an alternation's two sides (§22.2.2.3.3) and
    /// RepeatMatcher its iteration and its continuation (§22.2.2.3.1, steps 9 to 12): matching goes on at <c>A</c>, and only
    /// if every path from there fails does it go on at <c>B</c>, from the same index with the same captures.
    /// </summary>
    Choice = 4,

    /// <summary>Matching goes on at <c>A</c>: the continuation a construct hands to the one after it.</summary>
    Goto = 5,

    /// <summary>
    /// Register <c>A</c> takes the current index: one end of a capture group's range (§22.2.2.7, the continuation of
    /// Atom :: <c>(</c> GroupSpecifier Disjunction <c>)</c>), or the index an iteration of a quantifier begins at.
    /// </summary>
    SavePosition = 6,

    /// <summary>
    /// RepeatMatcher's steps 3 and 4 (§22.2.2.3.1): every capture inside the quantified atom, registers <c>A</c> to
    /// <c>B</c>, is set to undefined (-1) before the atom is matched again.
    /// </summary>
    ClearCaptures = 7,

    /// <summary>A counted quantifier starts: its iteration count, register <c>A</c>, becomes zero.</summary>
    SetCounter = 8,

    /// <summary>
    /// An iteration of a counted quantifier is complete: its count, register <c>A</c>, grows by one, and stays at
    /// <see cref="int.MaxValue"/> once there.
    /// </summary>
    IncrementCounter = 9,

    /// <summary>
    /// RepeatMatcher's step 1 for a quantifier whose remaining maximum has reached zero: once the count in register
    /// <c>A</c> equals the maximum, <c>B</c>, no iteration is tried and matching goes on at <c>C</c>, the continuation;
    /// below it, at the next instruction.
    /// </summary>
    ExitAtMaximum = 10,

    /// <summary>
    /// RepeatMatcher's step 8: while the count in register <c>A</c> is below the minimum, <c>B</c>, the next iteration is
    /// required and is entered at <c>C</c> with no choice left open; from the minimum on, matching goes on at the next
    /// instruction, the choice.
    /// </summary>
    EnterBelowMinimum = 11,

    /// <summary>
    /// The continuation RepeatMatcher builds at step 2, its step b: an iteration that is ending fails when it was optional
    /// and ended at the index it began at. It began at the index in register <c>A</c>; it was optional when <c>B</c> is
    /// negative (every iteration of <c>?</c> and <c>*</c>), or when register <c>B</c>, the iterations completed before it,
    /// is at least <c>C</c>, the minimum.
    /// </summary>
    RejectEmptyIteration = 12,

    /// <summary>The pattern's last continuation: the match succeeds, from the attempt's start index to the current one.</summary>
    Succeed = 13,

    /// <summary>
    /// The matcher of a pattern character under <c>i</c> (§22.2.2.7.1 CharacterSetMatcher over a one-member set, with
    /// rer.[[IgnoreCase]] true): the next character's Canonicalize (§22.2.2.7.3) must be <c>A</c>, the pattern character's
    /// own Canonicalize, computed when the program was built.
    /// </summary>
    LiteralIgnoringCase = 14,

    /// <summary>
    /// CharacterSetMatcher under <c>i</c> over class number <c>A</c>, its members as written: the next character
    /// matches when some member shares its Canonicalize, which the machine finds by testing the members of the
    /// character's case orbit, every code point with the same Canonicalize; when <c>B</c> is one the class is inverted
    /// (<c>[^…]</c>) and the character matches when no member shares it.
    /// </summary>
    ClassIgnoringCase = 15,

    /// <summary>
    /// CharacterSetMatcher under <c>v</c> with <c>i</c> over class number <c>A</c>, whose members are already canonical
    /// forms: the parser folded each operand by MaybeSimpleCaseFolding (§22.2.2.9.5) and took every complement within
    /// AllCharacters, so the next character matches when its Canonicalize is a member.
    /// </summary>
    CanonicalClass = 16,

    /// <summary>
    /// Assertion :: <c>^</c> (§22.2.2.6): the index is the input's first, or, when <c>A</c> is one (rer.[[Multiline]], the
    /// <c>m</c> flag in force), the character before it is a LineTerminator.
    /// </summary>
    LineStart = 17,

    /// <summary>
    /// Assertion :: <c>$</c> (§22.2.2.6): the index is the input's end, or, when <c>A</c> is one, the character at it is a
    /// LineTerminator.
    /// </summary>
    LineEnd = 18,

    /// <summary>
    /// Assertion :: <c>\b</c> (§22.2.2.6): IsWordChar of the character before the index and of the character at it
    /// differ, the word characters being class number <c>A</c>, the WordCharacters (§22.2.2.9.3) of the flags in force.
    /// </summary>
    WordEdge = 19,

    /// <summary>Assertion :: <c>\B</c>: IsWordChar of the two characters around the index, read from class <c>A</c>, agree.</summary>
    NotWordEdge = 20,

    /// <summary>
    /// BackreferenceMatcher (§22.2.2.7.2): the characters the group captured must come next, compared exactly. The group is
    /// read from the program's list of referenced groups, <c>B</c> entries from entry <c>A</c>: the first whose capture is
    /// defined is the one compared, and when none is, the reference matches the empty string. Today a list holds one
    /// group; a name ES2025 lets several groups share (task 2.6) becomes a list of them, the one that participated
    /// being compared, with no change to the instruction.
    /// </summary>
    Backreference = 21,

    /// <summary>
    /// BackreferenceMatcher under <c>i</c>: as <see cref="Backreference"/>, each character compared by Canonicalize
    /// (§22.2.2.7.3).
    /// </summary>
    BackreferenceIgnoringCase = 22,

    /// <summary>
    /// The start of a look-around (§22.2.2.6, Assertion :: <c>(?=</c>, <c>(?!</c>, <c>(?&lt;=</c> and <c>(?&lt;!</c>
    /// Disjunction <c>)</c>): a barrier frame is pushed at the index, and register <c>A</c> takes its place on the frame
    /// stack; matching goes on into the body. <c>B</c> is one for a negative look-around, whose barrier, when every way
    /// through the body has failed, resumes at <c>C</c>, the address after the look-around, from the index it was pushed
    /// at and with every capture the body made undone (step 2.d, "Return c(x)"). A positive look-around's barrier, reached
    /// the same way, fails in its turn (step 2.c).
    /// </summary>
    OpenLookaround = 23,

    /// <summary>
    /// The end of a look-around's body, which has matched (§22.2.2.6, step 2.b, the continuation that "always returns its
    /// MatchState argument"): the barrier frame register <c>A</c> names, and every frame above it, are dropped, so nothing
    /// after the look-around backtracks into its body. A positive look-around then goes on at the next instruction, from
    /// the index its barrier holds, with the captures its body made (steps 2.e to 2.h); a negative one, <c>B</c> one,
    /// fails (step 2.c).
    /// </summary>
    CloseLookaround = 24,

    /// <summary>
    /// RepeatMatcher (§22.2.2.3.1) over one character, greedy: the quantifier <c>{A,B}</c> of the atom at the next
    /// address, <c>B</c> <see cref="int.MaxValue"/> when unbounded; matching goes on after the atom. The run takes the
    /// <c>A</c> required characters, failing when one is missing, then as many more as the atom admits, up to <c>B</c>, and
    /// leaves one backtrack frame that gives the extra characters back one at a time, the last taken first, down to where
    /// the required ones end: the order in which the loop it replaces would have tried its continuation, with no frame and no
    /// trail cell per character. A one-character atom consumes a character whenever it matches, so the empty-iteration rule
    /// never applies, and it holds no group, so no capture is reset. The dispatch costs one unit, and every character the run
    /// examines, the one that ends it included, costs the atom's cost, charged before it is read; a character given back
    /// costs nothing of its own, as the instructions that run after it pay.
    /// </summary>
    GreedyRun = 25,

    /// <summary>
    /// RepeatMatcher over one character, lazy: as <see cref="GreedyRun"/>, the continuation tried first after the
    /// <c>A</c> required characters, and one more character taken each time every path from there fails, up to <c>B</c>,
    /// with one backtrack frame for the whole run. Each character the run examines costs the atom's cost, the one a
    /// resumption reads included.
    /// </summary>
    LazyRun = 26,

    /// <summary>
    /// Atom-run folding: the <c>A</c> one-character atoms at the next addresses, two or more terms of one sequence in the
    /// order the sequence is matched (its last term first in a look-behind's body), each read in its own direction; the
    /// first that fails fails the instruction, and matching goes on after the last. Folding moves no unit: the dispatch
    /// costs the first atom's cost and each later atom its own, charged before it is read, as the atoms dispatched one by
    /// one would cost.
    /// </summary>
    AtomRun = 27,

    /// <summary>
    /// A <c>v</c>-mode class holding strings (§22.2.2.7, Atom :: CharacterClass, steps 3 to 8), read from the program's
    /// table of strings <c>A</c>, which every occurrence of the same strings shares: its strings of more than one
    /// character longest first, then the class test of its single characters at the next address, then, when <c>B</c> is
    /// one, the empty string; matching goes on after the class test, and a backtrack frame keeps the next way while one is
    /// left. Two strings can both match at an index only if they begin with the same character, or end with it right to
    /// left, so the table keeps the strings by that character, and the instruction tries only those whose character is the
    /// one at the index, in their order, which is the order of the alternation it replaces with the strings that cannot
    /// match left out. The dispatch costs one unit and the table's weight, the search for the character; each character a
    /// string's comparison reads costs one, and the class test its cost.
    /// </summary>
    StringClass = 28,
}
