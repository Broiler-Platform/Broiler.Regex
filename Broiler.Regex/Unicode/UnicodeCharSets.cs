// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   27
// Annotated:        27/27
// Exempt:           2
// Human-reviewed:   0/27
// IP risk:          Low
// Security risk:    High
// Criteria:         24/4
// Resource impact:  3/10 max
// Unverified:       27
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using Broiler.Regex.Ast;
using Broiler.Unicode.Properties;
using UnicodeEmoji.StringProperties;

namespace Broiler.Regex.Unicode;

/// <summary>
/// The code-point sets behind the built-in ECMAScript character-class escapes
/// (<c>\d \D \w \W \s \S</c>) and behind Unicode property escapes
/// (<c>\p{…}</c> / <c>\P{…}</c>), per ECMA-262 §22.2.1 / Table 64.
/// </summary>
/// <remarks>
/// Property data comes from the pinned <c>Broiler.Unicode</c> range tables — the same
/// generated UCD revision the JavaScript layer's translator reads — so both engines
/// answer one set for a given property name.
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=7E3895
// Broiler-Falsified-If: ResolveProperty("General_Category", "Lu") and ResolveProperty("Lu") return code-point sets that differ
// Broiler-Human:        PENDING
public static class UnicodeCharSets
{
    /// <summary>The set matched by one built-in class escape.</summary>
    /// <remarks>
    /// <paramref name="ignoreCase"/> and <paramref name="unicode"/> only matter for
    /// <c>\w</c>/<c>\W</c>: §22.2.2.9.2 WordCharacters widens the basic word set with
    /// every code point that case-folds into it when both flags are on, which is why
    /// <c>/\W/iu</c> must NOT match <c>ſ</c> even though <c>ſ</c> is not a word character.
    /// </remarks>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=66BA6E
    // Broiler-Falsified-If: EscapeSet(NonWord, ignoreCase: true, unicode: true) contains U+017F, so /\W/iu matches LATIN SMALL LETTER LONG S
    // Broiler-Human:        PENDING
    public static CodePointSet EscapeSet(ClassEscape escape, bool ignoreCase, bool unicode) => escape switch
    {
        ClassEscape.Digit => DigitSet.Clone(),
        ClassEscape.NonDigit => DigitSet.Complement(),
        ClassEscape.Word => WordSet(ignoreCase, unicode).Clone(),
        ClassEscape.NonWord => WordSet(ignoreCase, unicode).Complement(),
        ClassEscape.Space => SpaceSet.Clone(),
        ClassEscape.NonSpace => SpaceSet.Complement(),
        _ => CodePointSet.Empty(),
    };

    /// <summary><c>\d</c> — exactly ASCII <c>0-9</c> (§22.2.1).</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=0B019E
    // Broiler-Falsified-If: IsDigit(0x0660) returns true, treating ARABIC-INDIC DIGIT ZERO as a \d member
    // Broiler-Human:        PENDING
    public static bool IsDigit(int cp) => cp >= '0' && cp <= '9';

    /// <summary>
    /// <c>\s</c> — WhiteSpace ∪ LineTerminator (§22.2.1 / Table 36 + §12.2).
    /// This is the full ECMAScript white-space set, broader than .NET's <c>\s</c>.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=492005
    // Broiler-Falsified-If: IsSpace(0x180E) returns true, though MONGOLIAN VOWEL SEPARATOR left the Zs category in Unicode 6.3 and is not ECMAScript white space
    // Broiler-Human:        PENDING
    public static bool IsSpace(int cp) => cp switch
    {
        0x0009 or 0x000A or 0x000B or 0x000C or 0x000D => true, // tab, LF, VT, FF, CR
        0x0020 => true,                                          // space
        0x00A0 => true,                                          // no-break space
        0x1680 => true,                                          // ogham space mark
        >= 0x2000 and <= 0x200A => true,                         // en quad … hair space
        0x2028 or 0x2029 => true,                                // line / paragraph separator
        0x202F => true,                                          // narrow no-break space
        0x205F => true,                                          // medium mathematical space
        0x3000 => true,                                          // ideographic space
        0xFEFF => true,                                          // zero-width no-break space (BOM)
        _ => false,
    };

    /// <summary>§22.2.2.9.2 WordCharacters — <c>\w</c>'s set for the given mode.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9.2; IP=Low; Security=Medium; Resources=0; Fingerprint=1E0F4E
    // Broiler-Falsified-If: WordSet(ignoreCase: true, unicode: false) contains U+017F or U+212A, which only the case-insensitive u/v word set may add
    // Broiler-Human:        PENDING
    public static CodePointSet WordSet(bool ignoreCase, bool unicode)
    {
        if (!ignoreCase || !unicode)
            return BasicWordSet;

        return FoldedWordSet;
    }

    /// <summary>§22.2.2.5 IsWordChar, used by <c>\b</c> / <c>\B</c>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=A55CB2
    // Broiler-Falsified-If: IsWord(0x017F, ignoreCase: true, unicode: true) returns false, so \b under iu treats LATIN SMALL LETTER LONG S as a non-word character
    // Broiler-Human:        PENDING
    public static bool IsWord(int cp, bool ignoreCase, bool unicode)
        => WordSet(ignoreCase, unicode).Contains(cp);

    /// <summary>The positive counterpart of a complementing escape (<c>\D</c> → <c>\d</c>).</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=3982FE
    // Broiler-Human:        PENDING
    public static ClassEscape PositiveEscape(ClassEscape escape) => escape switch
    {
        ClassEscape.NonDigit => ClassEscape.Digit,
        ClassEscape.NonWord => ClassEscape.Word,
        ClassEscape.NonSpace => ClassEscape.Space,
        _ => escape,
    };

    /// <summary>True for <c>\D</c>, <c>\W</c> and <c>\S</c>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=646555
    // Broiler-Human:        PENDING
    public static bool IsComplementEscape(ClassEscape escape)
        => escape is ClassEscape.NonDigit or ClassEscape.NonWord or ClassEscape.NonSpace;

    /// <summary>
    /// §22.2.2.9 AllCharacters — the universe a complement is taken against. Under
    /// <c>v</c> with <c>i</c> that universe holds only the code points that fold to
    /// themselves, which is why <c>[^\p{Lu}]</c> excludes <c>A</c> as well as <c>a</c>:
    /// <c>A</c> is not in the universe at all, having folded to <c>a</c>.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=None; Security=Medium; Resources=0; Fingerprint=84139D
    // Broiler-Falsified-If: AllCharacters(unicodeSets: true, ignoreCase: true) contains 0x0041 or another code point whose simple case fold is not itself
    // Broiler-Human:        PENDING
    public static CodePointSet AllCharacters(bool unicodeSets, bool ignoreCase)
        => unicodeSets && ignoreCase ? CanonicalCharacters : FullRange;

    // ----- Unicode property escapes -------------------------------------------

    /// <summary>The resolution of one <c>\p{…}</c> escape.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=D1335C
    // Broiler-Falsified-If: a caller adds or removes members of a resolved match's CodePoints set or Strings list, changing the cached instance every later resolution of that property of strings returns
    // Broiler-Human:        PENDING
    public sealed class PropertyMatch(CodePointSet codePoints, IReadOnlyList<string> strings)
    {
        /// <summary>The code points the property matches.</summary>
        public readonly CodePointSet CodePoints = codePoints;

        /// <summary>
        /// The multi-code-point sequences a property of strings (UTS #51) matches; empty
        /// for an ordinary property. A property of strings may only be used in a
        /// <c>v</c>-mode class and may never be negated (§22.2.1 early errors).
        /// </summary>
        public readonly IReadOnlyList<string> Strings = strings;

        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=6B9213
        // Broiler-Falsified-If: a property of strings whose resolved data holds no multi-code-point sequence reports false, so negating it or using it outside v mode is accepted
        // Broiler-Human:        PENDING
        public bool IsStringProperty => Strings.Count > 0;
    }

    /// <summary>
    /// Resolves a Unicode property escape such as <c>\p{Letter}</c>,
    /// <c>\p{Script=Greek}</c> or <c>\p{RGI_Emoji}</c>, or returns <c>null</c> when the
    /// name is not one ECMAScript recognises (which the caller reports as a SyntaxError).
    /// </summary>
    /// <remarks>
    /// ECMAScript admits three shapes (§22.2.1 UnicodePropertyValueExpression): the
    /// <c>General_Category=…</c>, <c>Script=…</c> and <c>Script_Extensions=…</c>
    /// name/value forms, a lone General_Category value, and a lone binary property or
    /// property of strings. Names are matched loosely — case-insensitively and ignoring
    /// <c>_</c>, <c>-</c> and spaces — by the Broiler.Unicode tables, so the canonical
    /// names and the ECMAScript aliases both resolve.
    /// </remarks>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=3B515A
    // Broiler-Falsified-If: a loosely spelled key such as \p{GC=Lu} or \p{general_category=Lu} resolves to a set instead of returning null, though ECMAScript accepts only General_Category, gc, Script, sc, Script_Extensions and scx
    // Broiler-Human:        PENDING
    public static PropertyMatch? ResolveProperty(string name, string? value = null)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (value != null)
        {
            var key = Normalize(name);
            var ranges = key switch
            {
                "gc" or "generalcategory" => UnicodeProperties.GetGeneralCategory(value),
                "sc" or "script" => UnicodeProperties.GetScript(value),
                "scx" or "scriptextensions" => UnicodeProperties.GetScriptExtensions(value),
                _ => null,
            };
            return ranges == null ? null : new PropertyMatch(FromRanges(ranges), []);
        }

        // A lone name is a General_Category value, a binary property, or a property of
        // strings — in that order, which is how the spec's tables are laid out.
        var category = UnicodeProperties.GetGeneralCategory(name);
        if (category != null)
            return new PropertyMatch(FromRanges(category), []);

        var binary = UnicodeProperties.GetBinaryProperty(name);
        if (binary != null)
            return new PropertyMatch(FromRanges(binary), []);

        if (StringPropertyNames.TryGetValue(Normalize(name), out var emoji))
            return ResolveStringProperty(emoji);

        return null;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=18D9C8
    // Broiler-Falsified-If: the cached match's CodePoints set is still un-normalized when the lock is released, so two threads first compiling [\p{RGI_Emoji}] under v both run CodePointSet.Normalize on that shared set outside the lock
    // Broiler-Human:        PENDING
    private static PropertyMatch ResolveStringProperty(EmojiSequenceProperties property)
    {
        lock (StringPropertyCache)
        {
            if (StringPropertyCache.TryGetValue(property, out var cached))
                return cached;

            // A property of strings still contributes its single-code-point members to the
            // code-point set; only the longer sequences need string matching.
            var codePoints = new CodePointSet();
            var strings = new List<string>();
            foreach (var sequence in EmojiStringProperties.GetSequences(property))
            {
                if (sequence.Length == 1)
                    codePoints.AddCodePoint(sequence[0]);
                else if (sequence.Length == 2 && char.IsHighSurrogate(sequence[0]) && char.IsLowSurrogate(sequence[1]))
                    codePoints.AddCodePoint(char.ConvertToUtf32(sequence[0], sequence[1]));
                else
                    strings.Add(sequence);
            }

            var match = new PropertyMatch(codePoints, strings);
            StringPropertyCache[property] = match;
            return match;
        }
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=3; Fingerprint=A3E6B3
    // Broiler-Human:        PENDING
    private static CodePointSet FromRanges((int Lo, int Hi)[] ranges)
    {
        var set = new CodePointSet();
        foreach (var (lo, hi) in ranges)
            set.AddRange(lo, hi);
        return set;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=0FBA70
    // Broiler-Falsified-If: with the current culture set to tr-TR, the key "SCRIPT" normalizes to something other than "script"
    // Broiler-Human:        PENDING
    private static string Normalize(string s)
    {
        var buffer = new char[s.Length];
        var n = 0;
        foreach (var ch in s)
        {
            if (ch is '_' or ' ' or '-')
                continue;
            buffer[n++] = char.ToLowerInvariant(ch);
        }
        return new string(buffer, 0, n);
    }

    // ----- Built-in sets ------------------------------------------------------

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=A3AB0F
    // Broiler-Falsified-If: FullRange omits a code point between 0 and 0x10FFFF, such as the lone surrogate 0xD800
    // Broiler-Human:        PENDING
    private static readonly CodePointSet FullRange = CodePointSet.Of(0, CodePointSet.MaxCodePoint);

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=2; Fingerprint=E608CF
    // Broiler-Falsified-If: the field is declared above FullRange, so BuildCanonicalCharacters reads FullRange before it is assigned
    // Broiler-Human:        PENDING
    private static readonly CodePointSet CanonicalCharacters = BuildCanonicalCharacters();

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=E27B08
    // Broiler-Falsified-If: DigitSet is still un-normalized after type initialization, so the first concurrent EscapeSet(Digit) or EscapeSet(NonDigit) calls rewrite its shared range list without a lock
    // Broiler-Human:        PENDING
    private static readonly CodePointSet DigitSet = CodePointSet.Of('0', '9');

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=48E723
    // Broiler-Falsified-If: SpaceSet is still un-normalized after type initialization, so a thread enumerating it in Complement can throw InvalidOperationException while another thread's first Clone normalizes it
    // Broiler-Human:        PENDING
    private static readonly CodePointSet SpaceSet = BuildSpaceSet();

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=0DE240
    // Broiler-Falsified-If: BasicWordSet is declared below FoldedWordSet, so BuildFoldedWordSet clones a null set during type initialization
    // Broiler-Human:        PENDING
    private static readonly CodePointSet BasicWordSet = BuildBasicWordSet();

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=2; Fingerprint=7EC05E
    // Broiler-Falsified-If: FoldedWordSet leaves type initialization with unsorted, un-merged ranges, so two threads whose first \w or \b under iu arrives at once both run CodePointSet.Normalize on the one shared list
    // Broiler-Human:        PENDING
    private static readonly CodePointSet FoldedWordSet = BuildFoldedWordSet();

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=BCE032
    // Broiler-Falsified-If: one of the seven ECMAScript properties of strings, such as RGI_Emoji_Tag_Sequence, has no entry, so \p{RGI_Emoji_Tag_Sequence} under v is rejected
    // Broiler-Human:        PENDING
    private static readonly Dictionary<string, EmojiSequenceProperties> StringPropertyNames = new(StringComparer.Ordinal)
    {
        ["rgiemoji"] = EmojiSequenceProperties.RgiEmoji,
        ["rgiemojiflagsequence"] = EmojiSequenceProperties.RgiEmojiFlagSequence,
        ["rgiemojimodifiersequence"] = EmojiSequenceProperties.RgiEmojiModifierSequence,
        ["rgiemojitagsequence"] = EmojiSequenceProperties.RgiEmojiTagSequence,
        ["rgiemojizwjsequence"] = EmojiSequenceProperties.RgiEmojiZwjSequence,
        ["basicemoji"] = EmojiSequenceProperties.BasicEmoji,
        ["emojikeycapsequence"] = EmojiSequenceProperties.EmojiKeycapSequence,
    };

    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=2; Fingerprint=C1368F
    // Broiler-Falsified-If: StringPropertyCache is read or written anywhere outside a lock taken on itself
    // Broiler-Human:        PENDING
    private static readonly Dictionary<EmojiSequenceProperties, PropertyMatch> StringPropertyCache = [];

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=2; Fingerprint=FBBB48
    // Broiler-Falsified-If: the set it builds contains U+212A KELVIN SIGN, whose simple case fold is 0x006B
    // Broiler-Human:        PENDING
    private static CodePointSet BuildCanonicalCharacters()
    {
        var folding = new CodePointSet();
        foreach (var (lo, hi) in CaseFolding.SimpleFoldSources)
            folding.AddRange(lo, hi);
        return FullRange.Subtract(folding);
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=561BED
    // Broiler-Falsified-If: the set it builds and IsSpace disagree on some code point, such as U+FEFF or U+2028
    // Broiler-Human:        PENDING
    private static CodePointSet BuildSpaceSet()
    {
        var set = new CodePointSet();
        set.AddRange(0x0009, 0x000D); // tab, LF, VT, FF, CR
        set.AddCodePoint(0x0020);     // space
        set.AddCodePoint(0x00A0);     // no-break space
        set.AddCodePoint(0x1680);     // ogham space mark
        set.AddRange(0x2000, 0x200A); // en quad … hair space
        set.AddRange(0x2028, 0x2029); // line / paragraph separator
        set.AddCodePoint(0x202F);     // narrow no-break space
        set.AddCodePoint(0x205F);     // medium mathematical space
        set.AddCodePoint(0x3000);     // ideographic space
        set.AddCodePoint(0xFEFF);     // zero-width no-break space (BOM)
        return set;
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=1DC1FD
    // Broiler-Falsified-If: the set it builds contains a code point outside A-Z, a-z, 0-9 and the low line
    // Broiler-Human:        PENDING
    private static CodePointSet BuildBasicWordSet()
    {
        var set = new CodePointSet();
        set.AddRange('a', 'z');
        set.AddRange('A', 'Z');
        set.AddRange('0', '9');
        set.AddCodePoint('_');
        return set;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9.2; IP=Low; Security=Low; Resources=2; Fingerprint=3437FE
    // Broiler-Falsified-If: the set it builds lacks U+017F or U+212A, the two code points outside A-Z, a-z, 0-9 and the low line whose simple case fold lands inside them
    // Broiler-Human:        PENDING
    private static CodePointSet BuildFoldedWordSet()
    {
        // extraWordChars: every code point outside the basic set whose canonical form is
        // inside it. Reading the fold orbits gives the whole answer from the pinned table
        // rather than the two code points (ſ, K) the ASCII-only special cases named.
        var set = BasicWordSet.Clone();
        foreach (var (lo, hi) in BasicWordSet.Ranges)
        {
            for (var cp = lo; cp <= hi; cp++)
            {
                foreach (var sibling in CaseFolding.Orbit(CaseFolding.Canonicalize(cp, unicode: true), unicode: true))
                    set.AddCodePoint(sibling);
            }
        }
        return set;
    }
}
