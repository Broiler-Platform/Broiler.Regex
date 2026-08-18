using System.Collections.Generic;
using System.Text;

namespace Broiler.Regex.Unicode;

/// <summary>
/// ECMAScript case-folding for case-insensitive (<c>i</c>) matching
/// (ECMA-262 §22.2.2.9.4 Canonicalize).
/// </summary>
/// <remarks>
/// The spec distinguishes two modes:
/// <list type="bullet">
/// <item><b>non-Unicode</b>: canonicalize via <c>toUppercase</c>, but a multi-char
/// uppercasing or a result that leaves the ASCII range when the input was ASCII is
/// rejected (so <c>ſ</c> does NOT fold to <c>s</c>).</item>
/// <item><b>Unicode (<c>u</c>/<c>v</c>)</b>: canonicalize via the Unicode
/// <c>CaseFolding.txt</c> "common + simple" mapping (so <c>ſ→s</c>, <c>K→k</c>).</item>
/// </list>
/// This implementation covers ASCII fully plus a documented subset of the
/// non-ASCII simple folds that test262 exercises (the Greek/Latin special cases
/// the .NET translator also special-cased). TODO: replace with the full
/// <c>Broiler.Unicode</c> simple-fold table for complete conformance.
/// </remarks>
public static class CaseFolding
{
    /// <summary>
    /// Returns the canonical representative of <paramref name="codePoint"/> for the
    /// given mode. Two code points are case-equivalent iff their canonical forms are
    /// equal.
    /// </summary>
    public static int Canonicalize(int codePoint, bool unicode)
    {
        if (unicode)
            return SimpleFold(codePoint);

        // Non-Unicode Canonicalize: ToUppercase of the single code unit, with the
        // ASCII guard (a non-ASCII char must not fold into the ASCII range).
        if (codePoint > 0xFFFF)
            return codePoint;

        var ch = (char)codePoint;
        var upper = char.ToUpperInvariant(ch);
        if (upper == ch)
            return codePoint;

        // Reject a fold that crosses into ASCII from a non-ASCII source.
        if (codePoint >= 0x80 && upper < 0x80)
            return codePoint;

        return upper;
    }

    /// <summary>
    /// Unicode "simple" case fold: lower-cases the code point, with a few
    /// special-cased folds that <c>ToLowerInvariant</c> alone gets wrong for
    /// regex equivalence.
    /// </summary>
    private static int SimpleFold(int codePoint)
    {
        switch (codePoint)
        {
            case 0x017F: return 's';    // ſ LATIN SMALL LETTER LONG S → s
            case 0x212A: return 'k';    // K KELVIN SIGN → k
            case 0x212B: return 0x00E5; // Å ANGSTROM SIGN → å
            case 0x2126: return 0x03C9; // Ω OHM SIGN → ω
            case 0x1E9E: return 0x00DF; // ẞ LATIN CAPITAL SHARP S → ß
        }

        if (codePoint > 0xFFFF)
            return AstralFold(codePoint);

        var lower = char.ToLowerInvariant((char)codePoint);
        if (lower != (char)codePoint)
            return lower;

        // Some letters only have an upper form whose lower we want as the rep.
        var upper = char.ToUpperInvariant((char)codePoint);
        if (upper != (char)codePoint)
        {
            var upperLower = char.ToLowerInvariant(upper);
            if (upperLower != (char)codePoint)
                return upperLower;
        }

        return codePoint;
    }

    /// <summary>
    /// Simple fold of a supplementary-plane code point. <see cref="char"/> casing is
    /// defined on single UTF-16 code units and leaves every astral code point alone, so
    /// the bicameral supplementary scripts — Deseret, Osage, Warang Citi, Medefaidrin,
    /// Adlam, Vithkuqi, Old Hungarian — folded to themselves and <c>/𐐀/iu</c> matched
    /// only the capital. <see cref="Rune"/> casing is defined on code points and has the
    /// mappings; the upper-then-lower round trip is the same representative the JS layer's
    /// astral fold table uses, so both engines answer one equivalence class.
    /// </summary>
    private static int AstralFold(int codePoint)
    {
        // Garay (Unicode 16.0) is assigned but carries no simple case mapping in .NET's
        // tables yet, so the round trip below leaves each letter in a class of its own.
        // Fold its capitals (U+10D50..U+10D65) onto their small letters (U+10D70..U+10D85)
        // explicitly; the day the tables catch up this agrees with them.
        if (codePoint >= GarayCapitalFirst && codePoint <= GarayCapitalLast)
            return codePoint + GarayCapitalToSmall;

        var rune = new Rune(codePoint);
        return Rune.ToLowerInvariant(Rune.ToUpperInvariant(rune)).Value;
    }

    private const int GarayCapitalFirst = 0x10D50;
    private const int GarayCapitalLast = 0x10D65;
    private const int GaraySmallFirst = 0x10D70;
    private const int GarayCapitalToSmall = GaraySmallFirst - GarayCapitalFirst;

    /// <summary>
    /// Yields the small set of code points known to be case-fold siblings of
    /// <paramref name="codePoint"/> (used to test class membership in both
    /// directions). This is the inverse of the special-case table above.
    /// </summary>
    public static IEnumerable<int> SimpleSiblings(int codePoint, bool unicode)
    {
        if (!unicode)
        {
            if (codePoint <= 0xFFFF)
            {
                var ch = (char)codePoint;
                var upper = char.ToUpperInvariant(ch);
                if (upper != ch && !(codePoint < 0x80 && upper >= 0x80))
                    yield return upper;
                var lower = char.ToLowerInvariant(ch);
                if (lower != ch && !(codePoint < 0x80 && lower >= 0x80))
                    yield return lower;
            }
            yield break;
        }

        switch (codePoint)
        {
            case 's': yield return 0x017F; break;
            case 'k': yield return 0x212A; break;
            case 0x00E5: yield return 0x212B; break;
            case 0x03C9: yield return 0x2126; break;
            case 0x00DF: yield return 0x1E9E; break;
        }

        if (codePoint > 0xFFFF)
        {
            // Same reason AstralFold exists: char casing has no mapping for a
            // supplementary code point, so the siblings have to come from Rune casing
            // (plus Garay, which .NET has no mapping for either).
            if (codePoint >= GarayCapitalFirst && codePoint <= GarayCapitalLast)
                yield return codePoint + GarayCapitalToSmall;
            else if (codePoint >= GaraySmallFirst && codePoint <= GaraySmallFirst + (GarayCapitalLast - GarayCapitalFirst))
                yield return codePoint - GarayCapitalToSmall;

            var rune = new Rune(codePoint);
            var upperRune = Rune.ToUpperInvariant(rune).Value;
            if (upperRune != codePoint) yield return upperRune;
            var lowerRune = Rune.ToLowerInvariant(rune).Value;
            if (lowerRune != codePoint) yield return lowerRune;
            yield break;
        }

        var bmp = (char)codePoint;
        var bmpUpper = char.ToUpperInvariant(bmp);
        if (bmpUpper != bmp) yield return bmpUpper;
        var bmpLower = char.ToLowerInvariant(bmp);
        if (bmpLower != bmp) yield return bmpLower;
    }

    /// <summary>True when the two code points are case-equivalent in the given mode.</summary>
    public static bool Equal(int a, int b, bool ignoreCase, bool unicode)
    {
        if (a == b)
            return true;
        if (!ignoreCase)
            return false;
        return Canonicalize(a, unicode) == Canonicalize(b, unicode);
    }
}
