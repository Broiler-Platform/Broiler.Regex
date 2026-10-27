// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   10
// Annotated:        10/10
// Exempt:           19
// Human-reviewed:   0/10
// IP risk:          None
// Security risk:    High
// Criteria:         2/2
// Resource impact:  1/10 max
// Unverified:       10
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.Regex;

/// <summary>
/// The places a compilation charges its work (requirement R2; decision D8 of Broiler.VM's JSD-0081, "compilation charged
/// in proportion to the expansion"; bundle <c>sjl-11-001</c>'s check A-2), each with its unit. The meter is charged the
/// same units whatever the place; the place is named for the tests of check A-2, which read what each place charged
/// through <see cref="CompileCharge.Observer"/>.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Low; Resources=0; Fingerprint=6F7846
// Broiler-Human:        PENDING
internal enum CompileSite
{
    /// <summary>The parser's read of the pattern: one unit per code unit, which its pre-scan reads once before the parse proper.</summary>
    Pattern = 0,

    /// <summary>
    /// A property escape expanded through the provider: one unit per range its set of code points holds, and for a
    /// property of strings one per sequence before the sequences are read and each sequence's code units before its
    /// string is built.
    /// </summary>
    Property = 1,

    /// <summary>Ranges gathered in any order made into a set: one unit per range, and ⌊log₂ n⌋ more per range for the sort when they arrive out of order.</summary>
    Normalize = 2,

    /// <summary>A union of two sets: one unit per range of each.</summary>
    Union = 3,

    /// <summary>An intersection of two sets: one unit per range of each.</summary>
    Intersection = 4,

    /// <summary>A difference of two sets: one unit per range of each, and one more.</summary>
    Difference = 5,

    /// <summary>A complement: one unit per range, and one more.</summary>
    Complement = 6,

    /// <summary>
    /// MaybeSimpleCaseFolding of a <c>v</c>-mode operand under <c>i</c> (ECMA-262 §22.2.2.9.5): one unit per range of
    /// the set read, one per code point mapped one at a time, and a string's code units, at least one, for each string
    /// folded.
    /// </summary>
    CaseFold = 7,

    /// <summary>
    /// A class's strings: a string's code units, at least one, each time a union, an intersection, a difference or a
    /// <c>\q{…}</c> disjunction adds it to a class's strings or looks it up there.
    /// </summary>
    Strings = 8,

    /// <summary>A class's strings ordered longest first: each string's code units, at least one, 1 + ⌊log₂ n⌋ times for n strings.</summary>
    StringOrder = 9,

    /// <summary>
    /// The sets a compilation builds from the provider rather than from the pattern, each once and only when the pattern
    /// needs it: <c>Cased</c> and <c>\s</c>'s Space_Separator (one unit per range copied), the folded <c>\w</c> (one per
    /// code point whose orbit it reads), and <c>AllCharacters</c> under <c>v</c> with <c>i</c> (one per <c>Cased</c> code
    /// point it folds).
    /// </summary>
    Universe = 10,

    /// <summary>
    /// The program compiler: one unit per range a class instruction copies, and for a class of strings each string's code
    /// units, at least one, for its table's key and its text, and 1 + ⌊log₂ n⌋ per string for the table's order, or for
    /// its lowering to an alternation when the fast paths are off.
    /// </summary>
    Program = 11,
}

/// <summary>A test's observer of a compilation's charges: the place, the units it charged, and the units spent through them.</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=DBBE2E
// Broiler-Human:        PENDING
internal delegate void CompileChargeObserver(CompileSite site, ulong units, ulong spent);

/// <summary>
/// One compilation's account of its work (requirement R2; decisions D8 and D9 of Broiler.VM's JSD-0081; conventions 4 and
/// 5): the caller's nullable meter, charged in whole blocks of <see cref="Block"/> units before the work they pay for, and
/// the caller's optional compile budget, past which no work is done.
/// </summary>
/// <remarks>
/// <para>
/// <b>Prepaid, as a match is.</b> Every place that does work whose size the pattern or the provider's data decides charges
/// its units here before it does that work (<see cref="CompileSite"/> names each place and its unit). When the units spent
/// pass what the meter has been charged, the meter is charged the whole blocks that cover them, and only then does the
/// work go on: before the k-th unit of a compilation's work at least k units have been charged, and the meter is never
/// more than a block less one ahead of the work, as the new backend's machine charges a match (task 2.4a). Work bounded
/// whatever the pattern - one character of the parse, one node of the tree the program compiler walks, the provider's
/// objects built once per compilation - is not charged on its own: the pattern's units pay for the first two, a constant
/// number per code unit.
/// </para>
/// <para>
/// <b>The budget.</b> A charge that would take the units spent past the caller's budget is refused before its work, and
/// the compilation ends with <see cref="CompileBudgetSpent"/>, which never leaves the library:
/// <see cref="BroilerRegex.Compile"/> answers it as <see cref="RegexStatus.LimitExceeded"/> naming
/// <see cref="RegexLimit.Compile"/>. The members that take no budget never meet it.
/// </para>
/// <para>
/// <b>One compilation's.</b> An instance is made for one compilation, runs on its thread and is kept by nothing the
/// compilation answers, so a meter that throws leaves no state of the library changed: the exception ends the compilation
/// and reaches the caller as it was thrown, and the next compilation starts from a new instance (decision D9).
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decisions D8 and D9; IP=None; Security=High; Resources=1; Fingerprint=F31C83
// Broiler-Falsified-If: a compilation's meter is charged anything but whole blocks of 512 units, is charged after the work a charge pays for, or a budget of n units admits a compilation whose charges total more than n
// Broiler-Human:        PENDING
internal sealed class CompileCharge
{
    /// <summary>The units the meter is charged in, whole blocks before the work they pay for, as a match's are (decision D8).</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=Low; Resources=0; Fingerprint=5491AD
    // Broiler-Human:        PENDING
    internal const ulong Block = 512;

    private readonly RegexWorkCharge? _meter;

    /// <summary>The caller's compile budget in units, <see cref="ulong.MaxValue"/> for none.</summary>
    private readonly ulong _budget;

    /// <summary>The units the compilation has spent, the charge being made included once it is admitted.</summary>
    private ulong _spent;

    /// <summary>The units the meter has been charged, <see cref="ulong.MaxValue"/> when there is no meter.</summary>
    private ulong _paid;

    /// <summary>The smaller of <see cref="_paid"/> and <see cref="_budget"/>: the units that may be spent before a charge stops to pay or to refuse.</summary>
    private ulong _threshold;

    /// <summary>An account charging <paramref name="meter"/>, or none, under <paramref name="budget"/>, or none.</summary>
    /// <param name="meter">The caller's meter, or null.</param>
    /// <param name="budget">The caller's compile budget in units, at least one, or null for none; the caller has checked it.</param>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=2B81C4
    // Broiler-Human:        PENDING
    internal CompileCharge(RegexWorkCharge? meter, ulong? budget)
    {
        _meter = meter;
        _budget = budget ?? ulong.MaxValue;
        _paid = meter is null ? ulong.MaxValue : 0;
        _threshold = Math.Min(_paid, _budget);
    }

    /// <summary>The units the compilation has spent so far, every charge admitted included.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=24A8BF
    // Broiler-Human:        PENDING
    internal ulong Spent => _spent;

    /// <summary>
    /// A test's observer, or null, as the library always leaves it: called after each charge is admitted, and paid for when
    /// there is a meter, and before the work it pays for, with the place, its units and the units spent through them, so
    /// that a test reads what its own meter holds at that moment (bundle <c>sjl-11-001</c>'s check A-2).
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=9995D4
    // Broiler-Human:        PENDING
    internal CompileChargeObserver? Observer { private get; init; }

    /// <summary>
    /// Charges <paramref name="units"/> of work at <paramref name="site"/>, before the work is done: the units are spent,
    /// and when they pass what the meter has been charged, the meter is charged the whole blocks that cover them.
    /// </summary>
    /// <exception cref="CompileBudgetSpent">The units would take the compilation past the caller's budget; nothing is spent or charged.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D8; IP=None; Security=High; Resources=1; Fingerprint=6F285C
    // Broiler-Falsified-If: a charge that takes the units spent past the budget is admitted, a refused charge stays spent, the meter is charged a part of a block, or the observer sees a charge before the meter has been charged for it
    // Broiler-Human:        PENDING
    internal void Charge(CompileSite site, ulong units)
    {
        if (units == 0)
            return;

        var spent = units > ulong.MaxValue - _spent ? ulong.MaxValue : _spent + units;
        if (spent > _threshold)
        {
            if (spent > _budget)
                throw new CompileBudgetSpent();

            var blocks = ((spent - _paid) + Block - 1) / Block;
            _meter!(blocks * Block);
            _paid += blocks * Block;
            _threshold = Math.Min(_paid, _budget);
        }

        _spent = spent;
        Observer?.Invoke(site, units, spent);
    }

    /// <summary>Charges <paramref name="units"/> of work at <paramref name="site"/>, as <see cref="Charge(CompileSite, ulong)"/> does; a count never negative.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=0DFF28
    // Broiler-Human:        PENDING
    internal void Charge(CompileSite site, long units) => Charge(site, (ulong)Math.Max(0, units));

    /// <summary>The units of a string a compilation reads: its length in code units, at least one, so that the empty string costs a unit too.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=8DF064
    // Broiler-Human:        PENDING
    internal static long StringUnits(string text) => Math.Max(1, text.Length);

    /// <summary>The comparisons a sort of <paramref name="count"/> items makes per item at most: 1 + ⌊log₂ n⌋, and none for an empty list.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=C311D8
    // Broiler-Human:        PENDING
    internal static long SortFactor(int count) => count <= 1 ? 1 : 1 + System.Numerics.BitOperations.Log2((uint)count);
}

/// <summary>
/// A charge past the caller's compile budget, thrown by <see cref="CompileCharge.Charge(CompileSite, ulong)"/> to unwind
/// a compilation. It is the library's own and never leaves it: <see cref="BroilerRegex.Compile"/> answers it as a status.
/// </summary>
// Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=A2DD93
// Broiler-Human:        PENDING
internal sealed class CompileBudgetSpent() : Exception("The compilation would do more work than the caller's compile budget admits.");
