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
public sealed class CharSet
{
    private readonly CodePointSet _set = new();
    private List<string> _strings;
    private HashSet<string> _members;

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
    public CodePointSet CodePoints => _set;

    /// <summary>
    /// Multi-code-point members contributed by <c>\q{…}</c> or by a property of strings
    /// (<c>\p{RGI_Emoji}</c>), longest first. Empty for every non-<c>v</c> class.
    /// </summary>
    public IReadOnlyList<string> Strings => _strings ?? (IReadOnlyList<string>)Array.Empty<string>();

    public bool HasStrings => _strings is { Count: > 0 };

    /// <summary>
    /// True when the class holds the empty string — only <c>\q{}</c> can put it there.
    /// It is the one character class that matches without consuming a code point, which
    /// the quantifier's empty-iteration guard has to know about.
    /// </summary>
    public bool MatchesEmptyString => _members?.Contains("") == true;

    public void AddCodePoint(int codePoint) => _set.AddCodePoint(codePoint);

    public void AddRange(int lo, int hi)
    {
        if (lo > hi)
            throw new RegexSyntaxException($"Range out of order in character class: U+{lo:X}-U+{hi:X}");
        _set.AddRange(lo, hi);
    }

    public void AddEscape(ClassEscape escape, bool ignoreCase, bool unicode)
        => _set.AddAll(UnicodeCharSets.EscapeSet(escape, ignoreCase, unicode));

    public void AddSet(CodePointSet set) => _set.AddAll(set);

    /// <summary>
    /// Adds a multi-code-point member. A one-code-point string is an ordinary member of
    /// the code-point set, per <c>ClassStringDisjunction</c>'s handling of a single
    /// <c>ClassSetCharacter</c>.
    /// </summary>
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
