// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   19
// Annotated:        19/19
// Exempt:           8
// Human-reviewed:   0/19
// IP risk:          Low
// Security risk:    High
// Criteria:         8/8
// Resource impact:  3/10 max
// Unverified:       19
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Text;
using Broiler.Regex.Ast;
using Broiler.Unicode.Algorithms;
using CodePointSet = Broiler.Regex.Ast.CodePointSet;
using ProviderSet = Broiler.Unicode.Algorithms.CodePointSet;

namespace Broiler.Regex.Unicode;

/// <summary>
/// Every Unicode answer one compilation reads, and the matches of the pattern it compiles: each is the Unicode
/// owner's, through the provider the caller hands in (requirement R11 of <c>docs/roadmap.md</c> section 6,
/// decision D11 of Broiler.VM's JSD-0081).
/// </summary>
/// <remarks>
/// <para>
/// <b>What it reads.</b> Property escapes by the exact names ECMA-262's tables admit, through
/// <see cref="UnicodeProperties.TryResolve(ReadOnlySpan{char}, out ProviderSet)"/> and its <c>name=value</c>
/// overload; the properties of strings through <see cref="UnicodeStringProperties"/>; both branches of
/// Canonicalize (ECMA-262 §22.2.2.7.3) through <see cref="UnicodeCaseFolding"/>; the identifier classes a group
/// name is held to (§22.2.1.1) through <see cref="EcmaScriptLexical"/>; and General_Category=Space_Separator, the
/// part of <c>\s</c> that is Unicode data. The library declares no data version of its own: the provider's
/// constructors refuse tables of a version other than the one <see cref="UnicodeAlgorithms.UnicodeVersion"/>
/// names.
/// </para>
/// <para>
/// <b>Immutable.</b> Everything it holds is built in the constructor and never changed: the provider's objects
/// are immutable, and the two sets it builds are normalized before the constructor returns. A compiled pattern
/// keeps one instance for its matches, which may run on several threads at once.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.3; IP=Low; Security=High; Resources=2; Fingerprint=3576CE
// Broiler-Falsified-If: a compiled pattern reads a Unicode answer that no IUnicodeTables call made during its compilation or matching could have supplied, so a recording provider misses a table the library read
// Broiler-Human:        PENDING
internal sealed class RegexUnicode
{
    /// <summary>The largest orbit span this class allocates on the stack; a provider stating a larger one gets an array.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=4FBBFC
    // Broiler-Human:        PENDING
    private const int StackOrbit = 16;

    private readonly UnicodeProperties _properties;
    private readonly UnicodeStringProperties _stringProperties;
    private readonly UnicodeCaseFolding _folding;
    private readonly EcmaScriptLexical _lexical;
    private readonly int _maxOrbit;
    private readonly CodePointSet _spaceSet;
    private readonly CodePointSet _foldedWordSet;

    /// <summary>The answers of <paramref name="tables"/>.</summary>
    /// <param name="tables">The Unicode owner's tables, of the version the provider's algorithms read.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tables"/> is null.</exception>
    /// <exception cref="ArgumentException">The tables are of another Unicode version or lack a table the algorithms read.</exception>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=4DA887
    // Broiler-Falsified-If: tables whose UnicodeVersion is not the provider's are accepted, so the library matches against a data version no caller pinned
    // Broiler-Human:        PENDING
    internal RegexUnicode(IUnicodeTables tables)
    {
        ArgumentNullException.ThrowIfNull(tables);
        _properties = new UnicodeProperties(tables);
        _stringProperties = new UnicodeStringProperties(tables);
        _folding = new UnicodeCaseFolding(tables);
        _lexical = new EcmaScriptLexical(_properties);
        _maxOrbit = Math.Max(_folding.MaxOrbit, _folding.MaxUpperOrbit);
        _spaceSet = BuildSpaceSet();
        _foldedWordSet = BuildFoldedWordSet();
    }

    // ----- Canonicalize (§22.2.2.7.3) -----------------------------------------

    /// <summary>
    /// ECMA-262's Canonicalize of <paramref name="codePoint"/> under <c>i</c>: simple case folding
    /// (<c>CaseFolding.txt</c> status C and S) under <c>u</c> or <c>v</c>, and the upper-casing of one code unit
    /// that keeps a code unit outside ASCII outside it without them.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.3; IP=Low; Security=Medium; Resources=0; Fingerprint=D6AB18
    // Broiler-Human:        PENDING
    internal int Canonicalize(int codePoint, bool unicode)
    {
        if (codePoint < 0)
            return codePoint;
        if (unicode)
            return _folding.SimpleFold(codePoint);

        // The non-Unicode branch is defined on one UTF-16 code unit; a code point above it is left alone.
        return codePoint > 0xFFFF ? codePoint : _folding.SimpleUpper(codePoint);
    }

    /// <summary>
    /// The provider's case folding itself, which a compiled program of the new backend keeps for the Canonicalize and the
    /// case orbits its instructions read at match time (task 2.4b of SJL-11.2). It is immutable, as every object of the
    /// provider is.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.3; IP=None; Security=Low; Resources=0; Fingerprint=A617CF
    // Broiler-Human:        PENDING
    internal UnicodeCaseFolding Folding => _folding;

    /// <summary>True when the two code points are equal, or case-equivalent under <paramref name="ignoreCase"/>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.3; IP=Low; Security=Medium; Resources=0; Fingerprint=E06948
    // Broiler-Human:        PENDING
    internal bool Equal(int a, int b, bool ignoreCase, bool unicode)
    {
        if (a == b)
            return true;
        if (!ignoreCase)
            return false;
        return Canonicalize(a, unicode) == Canonicalize(b, unicode);
    }

    /// <summary>
    /// Whether <paramref name="set"/> holds a code point whose canonical form is that of
    /// <paramref name="codePoint"/>: the question §22.2.2.7.1 CharacterSetMatcher asks of a class under <c>i</c>.
    /// </summary>
    /// <remarks>
    /// Canonicalizing every member of a class is impossible for a set the size of <c>\p{L}</c>, so the test runs
    /// the other way: the provider writes the code point's orbit (every code point sharing its canonical form,
    /// rarely more than four) and each is a range lookup.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.7.1; IP=Low; Security=High; Resources=1; Fingerprint=43DE79
    // Broiler-Falsified-If: under iu, a class holding only U+212A KELVIN SIGN is reported not to hold a code point case-equivalent to 'k'
    // Broiler-Human:        PENDING
    internal bool OrbitIntersects(CodePointSet set, int codePoint, bool unicode)
    {
        if (codePoint < 0 || (!unicode && codePoint > 0xFFFF))
            return set.Contains(codePoint);

        Span<int> orbit = _maxOrbit <= StackOrbit ? stackalloc int[StackOrbit] : new int[_maxOrbit];
        var count = unicode ? _folding.Orbit(codePoint, orbit) : _folding.UpperOrbit(codePoint, orbit);
        for (var i = 0; i < count; i++)
        {
            if (set.Contains(orbit[i]))
                return true;
        }

        return false;
    }

    // ----- Identifiers (§22.2.1.1 group names) ---------------------------------

    /// <summary>Whether <paramref name="codePoint"/> may start a RegExpIdentifierName: IdentifierStartChar.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Medium; Resources=0; Fingerprint=6AC628
    // Broiler-Human:        PENDING
    internal bool IsIdentifierStart(int codePoint) => _lexical.IsIdentifierStart(codePoint);

    /// <summary>Whether <paramref name="codePoint"/> may continue a RegExpIdentifierName: IdentifierPartChar.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Medium; Resources=0; Fingerprint=6AF8F2
    // Broiler-Human:        PENDING
    internal bool IsIdentifierPart(int codePoint) => _lexical.IsIdentifierPart(codePoint);

    // ----- Class escapes (§22.2.2.9) ---------------------------------------------

    /// <summary>The set matched by one built-in class escape; a set is immutable, so one may be shared.</summary>
    /// <remarks>
    /// <paramref name="ignoreCase"/> and <paramref name="unicode"/> only matter for <c>\w</c> and <c>\W</c>:
    /// §22.2.2.9.3 WordCharacters widens the basic word set with every code point that case-folds into it when
    /// both flags are on, which is why <c>/\W/iu</c> must not match <c>ſ</c>.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=High; Resources=1; Fingerprint=B86BFA
    // Broiler-Falsified-If: EscapeSet(NonWord, ignoreCase: true, unicode: true) contains U+017F, so /\W/iu matches LATIN SMALL LETTER LONG S
    // Broiler-Human:        PENDING
    internal CodePointSet EscapeSet(ClassEscape escape, bool ignoreCase, bool unicode) => escape switch
    {
        ClassEscape.Digit => UnicodeCharSets.DigitSet,
        ClassEscape.NonDigit => UnicodeCharSets.DigitSet.Complement(),
        ClassEscape.Word => WordSet(ignoreCase, unicode),
        ClassEscape.NonWord => WordSet(ignoreCase, unicode).Complement(),
        ClassEscape.Space => _spaceSet,
        ClassEscape.NonSpace => _spaceSet.Complement(),
        _ => CodePointSet.Empty(),
    };

    /// <summary>§22.2.2.9.3 WordCharacters, <c>\w</c>'s set for the given mode.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9.3; IP=Low; Security=Medium; Resources=0; Fingerprint=2B40F7
    // Broiler-Human:        PENDING
    private CodePointSet WordSet(bool ignoreCase, bool unicode)
        => ignoreCase && unicode ? _foldedWordSet : UnicodeCharSets.BasicWordSet;

    /// <summary>§22.2.2.4.1 IsWordChar, which <c>\b</c> and <c>\B</c> read.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.4.1; IP=None; Security=High; Resources=0; Fingerprint=03C9D1
    // Broiler-Falsified-If: IsWord(0x017F, ignoreCase: true, unicode: true) returns false, so \b under iu treats LATIN SMALL LETTER LONG S as a non-word character
    // Broiler-Human:        PENDING
    internal bool IsWord(int codePoint, bool ignoreCase, bool unicode)
        => WordSet(ignoreCase, unicode).Contains(codePoint);

    /// <summary>
    /// §22.2.2.9.4 AllCharacters under <c>v</c> with <c>i</c>: every code point that folds to itself, the universe
    /// a complement is taken against there. A new set, built on each call; the parser keeps the one it builds.
    /// </summary>
    /// <remarks>
    /// The provider answers simple case folding one code point at a time and states no enumeration of the code
    /// points it moves. Every such code point is Cased (<c>DerivedCoreProperties.txt</c>; the library's tests
    /// check this over the whole code space at the version the provider pins), so the scan asks
    /// <see cref="UnicodeCaseFolding.SimpleFold"/> of Cased's members only, a few thousand rather than the code
    /// space's 1,114,112. Were a future version to break that, a code point that does not fold to itself would
    /// stay in the universe; under <c>vi</c> every lookup canonicalizes the subject first, so no match would
    /// change, and the test would report it.
    /// <para>
    /// <b>Cost.</b> <c>0.1.0-preview.4</c> built this set once per process, from its own table, in a static. It is
    /// now built for each compilation that complements a class under <c>vi</c>, about 0.85 ms at 17.0.0 against
    /// about 0.05 ms for a compilation that does not (measured locally at task 2.2's review), as are the folded
    /// word set and <c>\s</c>'s set for every compilation in the constructor. Nothing is kept across compilations:
    /// a cache shared by them would be the mutable static that convention 10 and decision D31 refuse. The work is
    /// compile-time work of the kind requirement R2 charges, and task 2.5, which threads the meter through the set
    /// algebra, charges it.
    /// </para>
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9.4; IP=Low; Security=High; Resources=2; Fingerprint=1593A1
    // Broiler-Falsified-If: the set it builds contains 0x0041 or U+212A KELVIN SIGN, whose simple case folds are not themselves
    // Broiler-Human:        PENDING
    internal CodePointSet BuildCanonicalCharacters()
    {
        _properties.TryResolve("Cased", out var cased);
        var moved = new List<(int Lo, int Hi)>();
        for (var range = 0; range < cased.RangeCount; range++)
        {
            for (var codePoint = cased.First(range); codePoint <= cased.Last(range); codePoint++)
            {
                if (_folding.SimpleFold(codePoint) != codePoint)
                    moved.Add((codePoint, codePoint));
            }
        }

        return UnicodeCharSets.FullRange.Subtract(CodePointSet.FromRanges(moved));
    }

    /// <summary><c>\s</c>, WhiteSpace and LineTerminator (ECMA-262 §12.2 and §12.3), its Space_Separator part the provider's.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s12.2; IP=Low; Security=Low; Resources=1; Fingerprint=3E7D6C
    // Broiler-Human:        PENDING
    private CodePointSet BuildSpaceSet()
    {
        var ranges = new List<(int Lo, int Hi)>
        {
            (0x0009, 0x000D), // TAB, LF, VT, FF, CR
            (0x2028, 0x2029), // LINE SEPARATOR, PARAGRAPH SEPARATOR
            (0xFEFF, 0xFEFF), // ZWNBSP
        };
        AddRanges(ranges, _properties.SpaceSeparator);
        return CodePointSet.FromRanges(ranges);
    }

    /// <summary>
    /// §22.2.2.9.3 WordCharacters under <c>i</c> with <c>u</c> or <c>v</c>: the basic word characters and every
    /// code point whose simple case folding is one of them, from the provider's orbits.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9.3; IP=Low; Security=Low; Resources=1; Fingerprint=E762B0
    // Broiler-Human:        PENDING
    private CodePointSet BuildFoldedWordSet()
    {
        var ranges = new List<(int Lo, int Hi)>();
        Span<int> orbit = _maxOrbit <= StackOrbit ? stackalloc int[StackOrbit] : new int[_maxOrbit];
        foreach (var (lo, hi) in UnicodeCharSets.BasicWordSet.Ranges)
        {
            ranges.Add((lo, hi));
            for (var codePoint = lo; codePoint <= hi; codePoint++)
            {
                var count = _folding.Orbit(codePoint, orbit);
                for (var i = 0; i < count; i++)
                    ranges.Add((orbit[i], orbit[i]));
            }
        }

        return CodePointSet.FromRanges(ranges);
    }

    // ----- Property escapes (§22.2.2.9.7 UnicodeMatchProperty) -------------------

    /// <summary>
    /// Resolves the property escape <c>\p{name}</c> or <c>\p{name=value}</c> by the exact names ECMA-262 admits,
    /// or answers null for any other spelling, which the caller reports as a syntax error.
    /// </summary>
    /// <remarks>
    /// The lone form is a General_Category value or group, a binary property, or a property of strings, in that
    /// order, which is how the specification's tables are laid out; the <c>name=value</c> form is
    /// General_Category, Script or Script_Extensions with one of their values. A name is matched as written: no
    /// case folding, no ignoring of <c>_</c>, <c>-</c> or spaces, no <c>Is</c> prefix (§22.2.2.9.7
    /// UnicodeMatchProperty and §22.2.2.9.8 UnicodeMatchPropertyValue).
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9.7; IP=Low; Security=High; Resources=3; Fingerprint=76BE3F
    // Broiler-Falsified-If: a loosely spelled name such as \p{letter}, \p{Script=greek} or \p{ScriptExtensions=Greek} resolves to a set instead of returning null, or \p{Script=Katakana_Or_Hiragana} returns null
    // Broiler-Human:        PENDING
    internal UnicodeCharSets.PropertyMatch? ResolveProperty(string name, string? value)
    {
        if (value != null)
        {
            return _properties.TryResolve(name, value, out var pair)
                ? new UnicodeCharSets.PropertyMatch(ToCodePointSet(pair), [], isStringProperty: false)
                : null;
        }

        if (_properties.TryResolve(name, out var lone))
            return new UnicodeCharSets.PropertyMatch(ToCodePointSet(lone), [], isStringProperty: false);

        if (_stringProperties.TryResolve(name, out var property))
            return ResolveStringProperty(property);

        return null;
    }

    /// <summary>
    /// A property of strings: its single code points as a set and its sequences of two or more as strings.
    /// <c>RGI_Emoji</c> is the union of the others, which the provider holds no data for of its own.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=High; Resources=3; Fingerprint=34C07C
    // Broiler-Falsified-If: \p{RGI_Emoji} under v resolves to fewer code points or sequences than the union of the six properties it is defined as
    // Broiler-Human:        PENDING
    private UnicodeCharSets.PropertyMatch ResolveStringProperty(StringProperty property)
    {
        var ranges = new List<(int Lo, int Hi)>();
        var strings = new List<string>();
        if (property.IsUnion)
        {
            for (var id = 0; id < _stringProperties.PropertyCount; id++)
            {
                var member = _stringProperties.Property(id);
                if (!member.IsUnion)
                    AddStringProperty(member, ranges, strings);
            }
        }
        else
        {
            AddStringProperty(property, ranges, strings);
        }

        return new UnicodeCharSets.PropertyMatch(CodePointSet.FromRanges(ranges), [.. strings], isStringProperty: true);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=Medium; Resources=2; Fingerprint=33F660
    // Broiler-Human:        PENDING
    private static void AddStringProperty(StringProperty property, List<(int Lo, int Hi)> ranges, List<string> strings)
    {
        for (var range = 0; range < property.CodePointRangeCount; range++)
            ranges.Add((property.FirstCodePoint(range), property.LastCodePoint(range)));

        var sequences = property.GetSequences();
        Span<int> sequence = stackalloc int[StringPropertySequences.MaxSequenceLength];
        var text = new StringBuilder();
        while (sequences.TryRead(sequence, out var length))
        {
            text.Clear();
            for (var i = 0; i < length; i++)
                text.Append(char.ConvertFromUtf32(sequence[i]));
            strings.Add(text.ToString());
        }
    }

    /// <summary>The conversion of a set the provider answers into this library's <see cref="CodePointSet"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=2; Fingerprint=15F8D4
    // Broiler-Human:        PENDING
    internal static CodePointSet ToCodePointSet(ProviderSet set)
    {
        var ranges = new List<(int Lo, int Hi)>(set.RangeCount);
        AddRanges(ranges, set);
        return CodePointSet.FromRanges(ranges);
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=0797FD
    // Broiler-Human:        PENDING
    private static void AddRanges(List<(int Lo, int Hi)> ranges, ProviderSet set)
    {
        for (var range = 0; range < set.RangeCount; range++)
            ranges.Add((set.First(range), set.Last(range)));
    }
}
