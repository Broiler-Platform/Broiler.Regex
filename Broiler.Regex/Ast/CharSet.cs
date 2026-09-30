// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   14
// Annotated:        14/14
// Exempt:           13
// Human-reviewed:   0/14
// IP risk:          Low
// Security risk:    Medium
// Criteria:         7/0
// Resource impact:  4/10 max
// Unverified:       14
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using Broiler.Regex.Unicode;

namespace Broiler.Regex.Ast;

/// <summary>
/// The model of a character class (<c>[...]</c>) or a standalone class escape
/// (<c>\d</c>, <c>\p{L}</c>, …). Every member kind — literal, range, built-in
/// escape, Unicode property, or the result of a <c>v</c>-mode set operation —
/// reduces to one <see cref="CodePointSet"/>, optionally negated as a whole
/// (ECMA-262 §22.2.1 CharacterClass / ClassSetExpression).
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=BD9DC9
// Broiler-Falsified-If: a v-mode class written [\q{ab|ab|c}] ends up with a duplicate entry or a one-code-point member in Strings
// Broiler-Human:        PENDING
public sealed class CharSet
{
    private readonly CodePointSet _set = new();
    private List<string>? _strings;
    private HashSet<string>? _members;

    /// <summary>True for a negated class <c>[^…]</c>.</summary>
    public bool Negated { get; set; }

    /// <summary>
    /// Set when the class used a <c>v</c>-mode set operation (<c>&amp;&amp;</c>,
    /// <c>--</c>), a nested class, or a <c>\q{…}</c> string disjunction. The set is
    /// fully evaluated either way; this only records how it was written, for the
    /// integration layer's routing decision.
    /// </summary>
    public bool UsesSetOperations { get; set; }

    /// <summary>
    /// Set when a <c>\p{…}</c> / <c>\P{…}</c> escape appeared as a member of a bracketed
    /// class rather than standing alone. Also for the integration layer's routing
    /// decision: a class is the shape the JavaScript layer's .NET translator cannot
    /// express, since it can neither nest a complemented fragment nor put a
    /// supplementary-plane range inside <c>[…]</c>.
    /// </summary>
    public bool UsesPropertyEscape { get; set; }

    /// <summary>
    /// True when the members were already case-folded at parse time — the <c>v</c>-mode
    /// <c>MaybeSimpleCaseFolding</c>, which folds each operand <i>before</i> complementing
    /// it. Matching then folds the subject and tests membership directly instead of
    /// walking the subject's fold orbit.
    /// </summary>
    public bool CaseFolded { get; set; }

    /// <summary>The code points this class accepts, before <see cref="Negated"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=280671
    // Broiler-Human:        PENDING
    public CodePointSet CodePoints => _set;

    /// <summary>
    /// Multi-code-point members contributed by <c>\q{…}</c> or by a property of strings
    /// (<c>\p{RGI_Emoji}</c>), longest first. Empty for every non-<c>v</c> class.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=E7AA94
    // Broiler-Human:        PENDING
    public IReadOnlyList<string> Strings => _strings ?? (IReadOnlyList<string>)Array.Empty<string>();

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=A23CDD
    // Broiler-Human:        PENDING
    public bool HasStrings => _strings is { Count: > 0 };

    /// <summary>
    /// True when the class holds the empty string — only <c>\q{}</c> can put it there.
    /// It is the one character class that matches without consuming a code point, which
    /// the quantifier's empty-iteration guard has to know about.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=032173
    // Broiler-Human:        PENDING
    public bool MatchesEmptyString => _members?.Contains("") == true;

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=547200
    // Broiler-Human:        PENDING
    public void AddCodePoint(int codePoint) => _set.AddCodePoint(codePoint);

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=13A512
    // Broiler-Falsified-If: a class range written high to low, such as [z-a], is added to the set instead of throwing RegexSyntaxException
    // Broiler-Human:        PENDING
    public void AddRange(int lo, int hi)
    {
        if (lo > hi)
            throw new RegexSyntaxException($"Range out of order in character class: U+{lo:X}-U+{hi:X}");
        _set.AddRange(lo, hi);
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=2; Fingerprint=BAD743
    // Broiler-Falsified-If: two threads first parsing \w under the i and u flags at the same moment both sort the shared static folded word set in place inside Clone, and one receives a copy with a lost or duplicated range
    // Broiler-Human:        PENDING
    public void AddEscape(ClassEscape escape, bool ignoreCase, bool unicode)
        => _set.AddAll(UnicodeCharSets.EscapeSet(escape, ignoreCase, unicode));

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=4; Fingerprint=597BF4
    // Broiler-Human:        PENDING
    public void AddSet(CodePointSet set) => _set.AddAll(set);

    /// <summary>
    /// Adds a multi-code-point member. A one-code-point string is an ordinary member of
    /// the code-point set, per <c>ClassStringDisjunction</c>'s handling of a single
    /// <c>ClassSetCharacter</c>.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=2AADD1
    // Broiler-Falsified-If: a string holding exactly one surrogate pair, such as U+1F600, lands in Strings instead of in the code-point set
    // Broiler-Human:        PENDING
    public void AddString(string value)
    {
        if (CodePointLength(value) == 1)
        {
            AddCodePoint(value.Length == 1 ? value[0] : char.ConvertToUtf32(value, 0));
            return;
        }

        _strings ??= [];
        _members ??= new HashSet<string>(StringComparer.Ordinal);
        if (_members.Add(value))
            _strings.Add(value);
    }

    /// <summary>Orders the string members longest first, as leftmost-longest matching needs.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=328D46
    // Broiler-Falsified-If: for \q{ab|abc} the sorted Strings list puts "ab" ahead of "abc", so the shorter alternative is tried first
    // Broiler-Human:        PENDING
    public void SortStrings()
        => _strings?.Sort(static (a, b) =>
        {
            var byLength = b.Length.CompareTo(a.Length);
            return byLength != 0 ? byLength : string.CompareOrdinal(a, b);
        });

    /// <summary>
    /// Tests whether <paramref name="codePoint"/> is a member of this class
    /// (§22.2.2.10 CharacterSetMatcher).
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=091D6A
    // Broiler-Falsified-If: a CharSet with both CaseFolded and Negated set answers as if it were not negated, because the CaseFolded branch returns before Negated is applied
    // Broiler-Human:        PENDING
    public bool Contains(int codePoint, bool ignoreCase, bool unicode)
    {
        // A `v`-mode class folded its operands during construction and complemented the
        // folded set, so the subject only has to be folded the same way.
        if (CaseFolded)
            return _set.Contains(CaseFolding.Canonicalize(codePoint, unicode));

        var member = _set.Contains(codePoint);

        // Otherwise the spec asks whether the class holds ANY member that canonicalizes
        // to the subject's canonical form. Testing every member is impossible for a set
        // the size of `\p{L}`, so the orbit of the subject's canonical form — never more
        // than a handful of code points — is tested against the class instead.
        if (!member && ignoreCase)
        {
            var canonical = CaseFolding.Canonicalize(codePoint, unicode);
            foreach (var sibling in CaseFolding.Orbit(canonical, unicode))
            {
                if (_set.Contains(sibling))
                {
                    member = true;
                    break;
                }
            }
        }

        return Negated ? !member : member;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=3; Fingerprint=6C9E67
    // Broiler-Falsified-If: an unpaired high surrogate followed by an ASCII letter is counted as one code point instead of two
    // Broiler-Human:        PENDING
    private static int CodePointLength(string value)
    {
        var count = 0;
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                i++;
            count++;
        }
        return count;
    }
}

/// <summary>A built-in character-class escape usable inside or outside a class.</summary>
// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=C35DA6
// Broiler-Human:        PENDING
public enum ClassEscape
{
    /// <summary><c>\d</c></summary>
    Digit,
    /// <summary><c>\D</c></summary>
    NonDigit,
    /// <summary><c>\w</c></summary>
    Word,
    /// <summary><c>\W</c></summary>
    NonWord,
    /// <summary><c>\s</c></summary>
    Space,
    /// <summary><c>\S</c></summary>
    NonSpace,
}
