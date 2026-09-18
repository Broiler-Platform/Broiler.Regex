using System;
using System.Collections.Generic;

namespace Broiler.Regex.Unicode;

/// <summary>
/// ECMAScript case-folding for case-insensitive (<c>i</c>) matching
/// (ECMA-262 §22.2.2.9.4 Canonicalize).
/// </summary>
/// <remarks>
/// The spec distinguishes two modes, and both are table-driven here from the
/// Unicode Character Database revision named by
/// <see cref="CaseFoldingData.UnicodeVersion"/>:
/// <list type="bullet">
/// <item><b>non-Unicode</b>: canonicalize via <c>toUppercase</c> of the single code
/// unit, discarding a multi-code-unit uppercasing and a result that would leave a
/// non-ASCII input inside ASCII (so <c>ſ</c> does NOT fold to <c>s</c>).</item>
/// <item><b>Unicode (<c>u</c>/<c>v</c>)</b>: canonicalize via the Unicode
/// <c>CaseFolding.txt</c> "common + simple" mapping (so <c>ſ→s</c>, <c>K→k</c>).</item>
/// </list>
/// The generated tables replace the earlier <c>char</c>/<see cref="System.Text.Rune"/>
/// casing round trip, which had no mapping at all for supplementary code points and
/// disagreed with <c>scf</c> wherever a script's uppercase and folding directions
/// differ (Cherokee, Deseret, Adlam, Vithkuqi, Garay, Old Hungarian, Warang Citi).
/// </remarks>
public static class CaseFolding
{
    /// <summary>
    /// Returns the canonical representative of <paramref name="codePoint"/> for the
    /// given mode. Two code points are case-equivalent iff their canonical forms are
    /// equal, and the mapping is idempotent, so a canonical form canonicalizes to
    /// itself.
    /// </summary>
    public static int Canonicalize(int codePoint, bool unicode)
    {
        if (!unicode)
        {
            // The non-Unicode branch is defined on a single UTF-16 code unit; a
            // supplementary code point can only reach here through a `u`-less pattern
            // that already matches per code unit, so it is left alone.
            if (codePoint is < 0 or > 0xFFFF)
                return codePoint;
            return UpperTable.Map(codePoint);
        }

        return codePoint < 0 ? codePoint : SimpleTable.Map(codePoint);
    }

    /// <summary>
    /// Returns every code point whose canonical form is <paramref name="canonical"/> —
    /// the fold orbit of a canonical representative, always including
    /// <paramref name="canonical"/> itself.
    /// </summary>
    /// <remarks>
    /// §22.2.2.10 CharacterSetMatcher asks whether the class holds <i>any</i> member
    /// that canonicalizes to the input's canonical form. Canonicalizing every member of
    /// a class is impossible for a set the size of <c>\p{L}</c>, so the test runs the
    /// other way: the orbit is small (rarely more than four code points) and membership
    /// of each orbit element is a plain range lookup.
    /// </remarks>
    public static IReadOnlyList<int> Orbit(int canonical, bool unicode)
        => (unicode ? SimpleTable : UpperTable).Orbit(canonical);

    /// <summary>
    /// The code-point runs that have a simple case fold — every code point <c>c</c> with
    /// <c>scf(c) ≠ c</c>. §22.2.2.9 AllCharacters excludes exactly these from the universe
    /// a <c>v</c>-mode complement is taken against when <c>i</c> is on.
    /// </summary>
    public static IEnumerable<(int Lo, int Hi)> SimpleFoldSources => SimpleTable.Sources;

    /// <summary>True when the two code points are case-equivalent in the given mode.</summary>
    public static bool Equal(int a, int b, bool ignoreCase, bool unicode)
    {
        if (a == b)
            return true;
        if (!ignoreCase)
            return false;
        return Canonicalize(a, unicode) == Canonicalize(b, unicode);
    }

    private static readonly FoldTable SimpleTable = new(CaseFoldingData.SimpleFold);
    private static readonly FoldTable UpperTable = new(CaseFoldingData.UpperFold);

    /// <summary>
    /// One generated fold table: runs of <c>lo..hi</c> that all shift by the same delta,
    /// searched forward, plus the inverse (orbit) map built on first use.
    /// </summary>
    private sealed class FoldTable
    {
        private readonly int[] _lo;
        private readonly int[] _hi;
        private readonly int[] _delta;

        private Dictionary<int, int[]>? _orbits;
        private readonly object _gate = new();

        public FoldTable(string encoded)
        {
            var runs = CountRuns(encoded);
            _lo = new int[runs];
            _hi = new int[runs];
            _delta = new int[runs];

            var index = 0;
            var pos = 0;
            while (pos < encoded.Length)
            {
                var lo = ReadHex(encoded, ref pos);
                var hi = lo;
                if (pos < encoded.Length && encoded[pos] == '.')
                {
                    pos++;
                    hi = ReadHex(encoded, ref pos);
                }
                pos++; // ':'
                var negative = encoded[pos] == '-';
                if (negative)
                    pos++;
                var delta = ReadHex(encoded, ref pos);
                if (pos < encoded.Length)
                    pos++; // ','

                _lo[index] = lo;
                _hi[index] = hi;
                _delta[index] = negative ? -delta : delta;
                index++;
            }
        }

        public IEnumerable<(int Lo, int Hi)> Sources
        {
            get
            {
                for (var run = 0; run < _lo.Length; run++)
                    yield return (_lo[run], _hi[run]);
            }
        }

        /// <summary>The canonical form of <paramref name="codePoint"/>, or itself when unmapped.</summary>
        public int Map(int codePoint)
        {
            var lo = 0;
            var hi = _lo.Length - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                if (codePoint < _lo[mid])
                    hi = mid - 1;
                else if (codePoint > _hi[mid])
                    lo = mid + 1;
                else
                    return codePoint + _delta[mid];
            }
            return codePoint;
        }

        public IReadOnlyList<int> Orbit(int canonical)
        {
            var orbits = _orbits;
            if (orbits == null)
            {
                lock (_gate)
                    _orbits = orbits = BuildOrbits();
            }

            return orbits.TryGetValue(canonical, out var members) ? members : [canonical];
        }

        private Dictionary<int, int[]> BuildOrbits()
        {
            var pending = new Dictionary<int, List<int>>();
            for (var run = 0; run < _lo.Length; run++)
            {
                for (var cp = _lo[run]; cp <= _hi[run]; cp++)
                {
                    var target = cp + _delta[run];
                    if (!pending.TryGetValue(target, out var members))
                        pending[target] = members = [target];
                    members.Add(cp);
                }
            }

            var result = new Dictionary<int, int[]>(pending.Count);
            foreach (var (target, members) in pending)
                result[target] = [.. members];
            return result;
        }

        private static int CountRuns(string encoded)
        {
            if (encoded.Length == 0)
                return 0;
            var count = 1;
            foreach (var ch in encoded)
            {
                if (ch == ',')
                    count++;
            }
            return count;
        }

        private static int ReadHex(string encoded, ref int pos)
        {
            var value = 0;
            while (pos < encoded.Length)
            {
                var digit = HexValue(encoded[pos]);
                if (digit < 0)
                    break;
                value = value * 16 + digit;
                pos++;
            }
            return value;
        }

        private static int HexValue(char c) => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => -1,
        };
    }
}
