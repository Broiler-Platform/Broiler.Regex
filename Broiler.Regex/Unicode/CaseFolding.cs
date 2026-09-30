// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   16
// Annotated:        16/16
// Exempt:           5
// Human-reviewed:   0/16
// IP risk:          Low
// Security risk:    High
// Criteria:         14/1
// Resource impact:  2/10 max
// Unverified:       16
//
// GENERATED - DO NOT EDIT MANUALLY

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
// Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=E899C9
// Broiler-Human:        PENDING
public static class CaseFolding
{
    /// <summary>
    /// Returns the canonical representative of <paramref name="codePoint"/> for the
    /// given mode. Two code points are case-equivalent iff their canonical forms are
    /// equal, and the mapping is idempotent, so a canonical form canonicalizes to
    /// itself.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9.4; IP=Low; Security=Medium; Resources=0; Fingerprint=B81B4B
    // Broiler-Falsified-If: Canonicalize(0x017F, unicode: false) returns 0x0053, folding a non-ASCII code point into ASCII where the non-Unicode branch must return it unchanged
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=2; Fingerprint=139B38
    // Broiler-Falsified-If: Orbit(0x006B, unicode: true) omits U+212A KELVIN SIGN, although its simple case fold is 0x006B
    // Broiler-Human:        PENDING
    public static IReadOnlyList<int> Orbit(int canonical, bool unicode)
        => (unicode ? SimpleTable : UpperTable).Orbit(canonical);

    /// <summary>
    /// The code-point runs that have a simple case fold — every code point <c>c</c> with
    /// <c>scf(c) ≠ c</c>. §22.2.2.9 AllCharacters excludes exactly these from the universe
    /// a <c>v</c>-mode complement is taken against when <c>i</c> is on.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=9BA0D0
    // Broiler-Falsified-If: a yielded run contains a code point whose simple case fold is itself, such as U+0061
    // Broiler-Human:        PENDING
    public static IEnumerable<(int Lo, int Hi)> SimpleFoldSources => SimpleTable.Sources;

    /// <summary>True when the two code points are case-equivalent in the given mode.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=276B89
    // Broiler-Falsified-If: Equal(0x212A, 0x006B, ignoreCase: true, unicode: false) returns true, though only the Unicode branch folds the Kelvin sign to 0x006B
    // Broiler-Human:        PENDING
    public static bool Equal(int a, int b, bool ignoreCase, bool unicode)
    {
        if (a == b)
            return true;
        if (!ignoreCase)
            return false;
        return Canonicalize(a, unicode) == Canonicalize(b, unicode);
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=2; Fingerprint=78EC85
    // Broiler-Falsified-If: SimpleTable.Map(0x1E9E) returns anything other than 0x00DF, the simple fold of LATIN CAPITAL LETTER SHARP S
    // Broiler-Human:        PENDING
    private static readonly FoldTable SimpleTable = new(CaseFoldingData.SimpleFold);
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=2; Fingerprint=E6AC99
    // Broiler-Falsified-If: UpperTable.Map(0x00DF) returns anything other than 0x00DF, although its uppercase is the two code units SS and must be discarded
    // Broiler-Human:        PENDING
    private static readonly FoldTable UpperTable = new(CaseFoldingData.UpperFold);

    /// <summary>
    /// One generated fold table: runs of <c>lo..hi</c> that all shift by the same delta,
    /// searched forward, plus the inverse (orbit) map built on first use.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=F228E1
    // Broiler-Falsified-If: _orbits is assigned anywhere other than inside lock (_gate)
    // Broiler-Human:        PENDING
    private sealed class FoldTable
    {
        private readonly int[] _lo;
        private readonly int[] _hi;
        private readonly int[] _delta;

        private Dictionary<int, int[]>? _orbits;
        private readonly object _gate = new();

        // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=2; Fingerprint=BE47EA
        // Broiler-Falsified-If: a run with a negative delta, such as 178:-79, decodes to U+0178 plus 0x79 instead of U+00FF
        // Broiler-Human:        PENDING
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

        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=F58F34
        // Broiler-Human:        PENDING
        public IEnumerable<(int Lo, int Hi)> Sources
        {
            get
            {
                for (var run = 0; run < _lo.Length; run++)
                    yield return (_lo[run], _hi[run]);
            }
        }

        /// <summary>The canonical form of <paramref name="codePoint"/>, or itself when unmapped.</summary>
        // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=CC6A39
        // Broiler-Falsified-If: Map(0x005B), one past the 41..5A run, returns 0x007B instead of 0x005B
        // Broiler-Human:        PENDING
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

        // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=6ADA75
        // Broiler-Falsified-If: a thread that calls Orbit while another thread is still inside the first BuildOrbits gets back an orbit missing members that a later call returns
        // Broiler-Human:        PENDING
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

        // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=2; Fingerprint=58D056
        // Broiler-Falsified-If: the orbit built for 0x0073 from the simple table does not hold exactly 0x0073, 0x0053 and U+017F
        // Broiler-Human:        PENDING
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

        // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=CEB974
        // Broiler-Falsified-If: CountRuns returns a count different from the number of runs the constructor decodes from SimpleFold or UpperFold
        // Broiler-Human:        PENDING
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

        // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=777DCD
        // Broiler-Falsified-If: ReadHex stops before the last hex digit of a bound, such as reading 1E9E as 0x1E9
        // Broiler-Human:        PENDING
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

        // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=84B6E2
        // Broiler-Falsified-If: the generated table holds a lowercase hex digit, which HexValue reads as -1 and so cuts that value short
        // Broiler-Human:        PENDING
        private static int HexValue(char c) => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => -1,
        };
    }
}
