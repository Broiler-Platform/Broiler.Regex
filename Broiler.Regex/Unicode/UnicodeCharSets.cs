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
public static class UnicodeCharSets
{
    /// <summary>The set matched by one built-in class escape.</summary>
    /// <remarks>
    /// <paramref name="ignoreCase"/> and <paramref name="unicode"/> only matter for
    /// <c>\w</c>/<c>\W</c>: §22.2.2.9.2 WordCharacters widens the basic word set with
    /// every code point that case-folds into it when both flags are on, which is why
    /// <c>/\W/iu</c> must NOT match <c>ſ</c> even though <c>ſ</c> is not a word character.
    /// </remarks>
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
    public static bool IsDigit(int cp) => cp >= '0' && cp <= '9';

    /// <summary>
    /// <c>\s</c> — WhiteSpace ∪ LineTerminator (§22.2.1 / Table 36 + §12.2).
    /// This is the full ECMAScript white-space set, broader than .NET's <c>\s</c>.
    /// </summary>
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
    public static CodePointSet WordSet(bool ignoreCase, bool unicode)
    {
        if (!ignoreCase || !unicode)
            return BasicWordSet;

        return FoldedWordSet;
    }

    /// <summary>§22.2.2.5 IsWordChar, used by <c>\b</c> / <c>\B</c>.</summary>
    public static bool IsWord(int cp, bool ignoreCase, bool unicode)
        => WordSet(ignoreCase, unicode).Contains(cp);

    /// <summary>The positive counterpart of a complementing escape (<c>\D</c> → <c>\d</c>).</summary>
    public static ClassEscape PositiveEscape(ClassEscape escape) => escape switch
    {
        ClassEscape.NonDigit => ClassEscape.Digit,
        ClassEscape.NonWord => ClassEscape.Word,
        ClassEscape.NonSpace => ClassEscape.Space,
        _ => escape,
    };

    /// <summary>True for <c>\D</c>, <c>\W</c> and <c>\S</c>.</summary>
    public static bool IsComplementEscape(ClassEscape escape)
        => escape is ClassEscape.NonDigit or ClassEscape.NonWord or ClassEscape.NonSpace;

    /// <summary>
    /// §22.2.2.9 AllCharacters — the universe a complement is taken against. Under
    /// <c>v</c> with <c>i</c> that universe holds only the code points that fold to
    /// themselves, which is why <c>[^\p{Lu}]</c> excludes <c>A</c> as well as <c>a</c>:
    /// <c>A</c> is not in the universe at all, having folded to <c>a</c>.
    /// </summary>
    public static CodePointSet AllCharacters(bool unicodeSets, bool ignoreCase)
        => unicodeSets && ignoreCase ? CanonicalCharacters : FullRange;

    // ----- Unicode property escapes -------------------------------------------

    /// <summary>The resolution of one <c>\p{…}</c> escape.</summary>
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
    public static PropertyMatch ResolveProperty(string name, string value)
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

    private static CodePointSet FromRanges((int Lo, int Hi)[] ranges)
    {
        var set = new CodePointSet();
        foreach (var (lo, hi) in ranges)
            set.AddRange(lo, hi);
        return set;
    }

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

    private static readonly CodePointSet FullRange = CodePointSet.Of(0, CodePointSet.MaxCodePoint);

    private static readonly CodePointSet CanonicalCharacters = BuildCanonicalCharacters();

    private static readonly CodePointSet DigitSet = CodePointSet.Of('0', '9');

    private static readonly CodePointSet SpaceSet = BuildSpaceSet();

    private static readonly CodePointSet BasicWordSet = BuildBasicWordSet();

    private static readonly CodePointSet FoldedWordSet = BuildFoldedWordSet();

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

    private static readonly Dictionary<EmojiSequenceProperties, PropertyMatch> StringPropertyCache = [];

    private static CodePointSet BuildCanonicalCharacters()
    {
        var folding = new CodePointSet();
        foreach (var (lo, hi) in CaseFolding.SimpleFoldSources)
            folding.AddRange(lo, hi);
        return FullRange.Subtract(folding);
    }

    private static CodePointSet BuildSpaceSet()
    {
        var set = new CodePointSet();
        for (var cp = 0; cp <= 0xFEFF; cp++)
        {
            if (IsSpace(cp))
                set.AddCodePoint(cp);
        }
        return set;
    }

    private static CodePointSet BuildBasicWordSet()
    {
        var set = new CodePointSet();
        set.AddRange('a', 'z');
        set.AddRange('A', 'Z');
        set.AddRange('0', '9');
        set.AddCodePoint('_');
        return set;
    }

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
