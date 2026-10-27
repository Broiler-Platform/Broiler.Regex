using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Broiler.Regex.Fuzz;

/// <summary>
/// The shapes of the adversarial corpus's classes (<c>tests/Adversarial/README.md</c>; part C of bundle
/// <c>sjl-11-001</c>'s rule), drawn with random sizes so that the generator reaches each class part C names: K
/// catastrophic backtracking, A alternation without quantifiers, L look-around and look-behind, G a greedy run longer
/// than the old allowance, W exhaustion inside one alternative, N nesting (at the limit, one past it and far past it,
/// in every kind of group and in <c>v</c>-mode classes), F a flat sequence of many terms, M frames and trail, V large
/// <c>v</c>-mode and case-insensitive expansions, and O ordinary patterns.
/// </summary>
internal static class CorpusShapes
{
    private static readonly string[] Classes = ["K", "A", "L", "G", "W", "N", "F", "M", "V", "O"];

    internal static (string Shape, string Pattern, RegexFlags Flags, string Subject) Write(FuzzRandom random, RegexFlags flags, SortedSet<string> constructs)
    {
        var letter = random.Pick(Classes);
        constructs.Add("class-" + letter);
        var (pattern, newFlags, subject) = letter switch
        {
            "K" => Catastrophic(random, flags),
            "A" => Alternation(random, flags),
            "L" => Lookaround(random, flags),
            "G" => GreedyRun(random, flags),
            "W" => OneAlternative(random, flags),
            "N" => Nesting(random, flags, constructs),
            "F" => Flat(random, flags),
            "M" => FramesAndTrail(random, flags, constructs),
            "V" => Expansion(random, flags),
            _ => Ordinary(random, flags),
        };
        return ("class-" + letter, pattern, newFlags, subject);
    }

    private static string Repeat(string text, int count)
    {
        var builder = new StringBuilder(text.Length * Math.Max(count, 0));
        for (var i = 0; i < count; i++)
            builder.Append(text);
        return builder.ToString();
    }

    private static (string, RegexFlags, string) Catastrophic(FuzzRandom random, RegexFlags flags)
    {
        var atom = random.Pick(["a", "\\w", "[ab]", ".", "(?:a)", "[a]", "a?"]);
        var pattern = random.Below(6) switch
        {
            0 => "(" + atom + "+)+b",
            1 => "(" + atom + "*)*b",
            2 => "(a|aa)+b",
            3 => "(?:" + atom + "+)+b",
            4 => "^(\\w+\\s?)*$",
            _ => "(" + atom + "+)+$",
        };
        var run = Repeat("a", random.Between(8, 40));
        var subject = random.Below(3) switch { 0 => "b" + run, 1 => run + "!", _ => run + "c" };
        return (pattern, flags, subject);
    }

    private static (string, RegexFlags, string) Alternation(FuzzRandom random, RegexFlags flags)
    {
        var count = random.Between(4, 30);
        var alternative = random.Pick(["(?:a|aa)", "(?:a|a)", "(?:ab|a|b)", "(a|aa)"]);
        return (Repeat(alternative, count) + "b", flags, random.Chance(50) ? "b" + Repeat("a", 2 * count) : Repeat("a", 2 * count) + "c");
    }

    private static (string, RegexFlags, string) Lookaround(FuzzRandom random, RegexFlags flags)
    {
        var pattern = random.Pick(["^(?!(?:(a+)+b|a*c))", "^(?=(?:(a+)+b|a*c))", "$(?<=(?:b(a+)+|ca*))", "$(?<!(?:b(a+)+|ca*))", "(?<=(a+)+)b", "(?=(a+)+b)", "(?<!(?:a|aa)+)c"]);
        var run = Repeat("a", random.Between(8, 32));
        return (pattern, flags, random.Chance(50) ? run + "c" : "c" + run);
    }

    private static (string, RegexFlags, string) GreedyRun(FuzzRandom random, RegexFlags flags)
    {
        var pattern = random.Pick(["[a]*", "^[a]*$", "a*", "\\w+", "[^x]*", ".*", "(?:a)*", "[a]*?$", "a+b", "[\\s\\S]*c"]);
        var length = random.Chance(70) ? random.Between(1000, 4096) : random.Between(4097, 200_000);
        return (pattern, flags, Repeat("a", length) + (random.Chance(30) ? "b" : ""));
    }

    private static (string, RegexFlags, string) OneAlternative(FuzzRandom random, RegexFlags flags)
    {
        var pattern = random.Pick(["(?:(a+)+b|a*c)", "(?:x|(a+)+b|y)", "(?:(?:a|aa)+b|c)", "(?:a*c|(a+)+b)"]);
        return (pattern, flags, Repeat("a", random.Between(8, 32)) + random.Pick(["c", "", "d"]));
    }

    private static (string, RegexFlags, string) Nesting(FuzzRandom random, RegexFlags flags, SortedSet<string> constructs)
    {
        var maximum = BroilerRegex.MaximumNestingLimit;
        var depth = random.Below(10) switch
        {
            0 => maximum - 2,
            1 or 2 => maximum - 1,
            3 or 4 => maximum,
            5 => maximum + 1,
            6 => 2 * maximum,
            7 => random.Between(1, maximum),
            8 => 20_000,
            _ => random.Chance(10) ? 1_000_000 : random.Between(maximum, 4 * maximum),
        };
        // depth groups around the atom nest to depth + 1, the pattern's own Disjunction being 1
        // (tests/Adversarial/README.md, section 2).
        constructs.Add(depth == maximum - 1 ? "nesting-at-limit" : depth < maximum - 1 ? "nesting-below-limit" : depth == maximum ? "nesting-one-past" : "nesting-far-past");
        var kind = random.Below(9);
        var open = kind switch
        {
            0 => "(",
            1 => "(?:",
            2 => null,
            3 => "(?=",
            4 => "(?<=",
            5 => "(?i:",
            6 => "[",
            7 => "(?!",
            _ => "(?<!",
        };
        string pattern;
        if (kind == 6)
        {
            flags = (flags & ~RegexFlags.Unicode) | RegexFlags.UnicodeSets;
            pattern = Repeat("[", depth) + "a" + Repeat("]", depth);
        }
        else if (open is null)
        {
            var text = new StringBuilder();
            for (var i = 0; i < depth; i++)
                text.Append("(?<n").Append(i.ToString(CultureInfo.InvariantCulture)).Append('>');
            pattern = text.Append('a').Append(Repeat(")", depth)).ToString();
        }
        else
        {
            pattern = Repeat(open, depth) + "a" + Repeat(")", depth);
        }

        return (pattern, flags, random.Pick(["a", "", "ba"]));
    }

    private static (string, RegexFlags, string) Flat(FuzzRandom random, RegexFlags flags)
    {
        var term = random.Pick(["\\B", "(?:a)", "(?:a|b)", "a", "(a)", "[a]", "\\b", "(?=a)", "a?", "(?<=a)"]);
        var count = random.Chance(80) ? random.Between(100, 4000) : random.Between(4001, 20_000);
        var subject = term is "\\B" or "\\b" or "(?=a)" or "(?<=a)" ? random.Pick(["", "a"]) : Repeat("a", term == "a?" ? random.Below(count) : count);
        return (Repeat(term, count), flags, subject);
    }

    private static (string, RegexFlags, string) FramesAndTrail(FuzzRandom random, RegexFlags flags, SortedSet<string> constructs)
    {
        // Now and then a subject long enough to pass the engines' ceilings, so that the string-bearing members, which
        // pass them, answer a ceiling too.
        var engines = random.Chance(2);
        if (engines)
            constructs.Add("engine-ceiling-subject");
        if (random.Chance(50))
        {
            // Two frames an iteration, a choice and the loop's: 1,048,576 frames are passed near 524,288 characters.
            if (engines)
                return ("(?:a|b)*c", flags & ~RegexFlags.Sticky, Repeat("a", 600_000) + "c");
            return ("(?:a|ab)*c", flags & ~RegexFlags.Sticky, Repeat("ab", random.Between(100, 5000)) + "c");
        }

        var length = engines ? 1_100_000 : random.Between(100, 5000);
        return ("(?:(a)|b)*c", flags & ~RegexFlags.Sticky, Repeat("a", length) + "c");
    }

    private static (string, RegexFlags, string) Expansion(FuzzRandom random, RegexFlags flags)
    {
        var count = random.Between(1, 50);
        switch (random.Below(6))
        {
            case 0:
                return ("[" + Repeat("\\p{L}", count) + "]", random.Pick([RegexFlags.UnicodeSets, RegexFlags.UnicodeSets | RegexFlags.IgnoreCase, RegexFlags.Unicode | RegexFlags.IgnoreCase]), random.Pick(["A", "\u03BB", "1"]));
            case 1:
                return ("[" + Repeat("\\p{RGI_Emoji}", count) + "]", random.Pick([RegexFlags.UnicodeSets, RegexFlags.UnicodeSets | RegexFlags.IgnoreCase]), random.Pick(["\U0001F600", "\U0001F468\u200D\U0001F469\u200D\U0001F467", "a"]));
            case 2:
                return ("[\\p{L}--\\p{Lu}--[a-z]]", RegexFlags.UnicodeSets | RegexFlags.IgnoreCase, random.Pick(["A", "\u00E9", "\u03BB"]));
            case 3:
                return ("[\\p{L}&&\\p{Script=Greek}&&\\P{Lu}]", RegexFlags.UnicodeSets | RegexFlags.IgnoreCase, random.Pick(["\u039B", "\u03BB", "a"]));
            case 4:
                return (Repeat("\\p{L}", count), RegexFlags.Unicode | RegexFlags.IgnoreCase, Repeat("a", count));
            default:
                return ("[^" + Repeat("\\P{L}", Math.Min(count, 8)) + "\\p{N}]", (flags | RegexFlags.Unicode | RegexFlags.IgnoreCase) & ~RegexFlags.UnicodeSets, "k");
        }
    }

    private static (string, RegexFlags, string) Ordinary(FuzzRandom random, RegexFlags flags)
    {
        var n = random.Between(1, 20);
        return random.Below(9) switch
        {
            0 => ("(?:(a+)+b|a*c)", flags, Repeat("a", n) + "c"),
            1 => ("(a+)+b", flags, Repeat("a", n) + "b"),
            2 => ("[0-9]+x", flags, Repeat("1", n * 5) + "x"),
            3 => ("(?:a|ab)*c", flags, Repeat("ab", n) + "c"),
            4 => ("(?<a>x)|(?<a>y)", flags, "y"),
            5 => ("(?:(?<a>x)|(?<a>y))\\k<a>", flags, "yy"),
            6 => ("\\k<b>(?<a>x)", flags | RegexFlags.Unicode, "x"),
            7 => ("(?:(a)|b)*c", flags, Repeat("a", n) + "c"),
            _ => ("b", flags, "ab"),
        };
    }
}

/// <summary>Subjects built around a pattern's sample: as it is, embedded, repeated, edited, in pieces of surrogates, or long.</summary>
internal static class Subjects
{
    private static readonly string[] Noise =
    [
        "a", "b", "x", "A", "Z", "0", "9", " ", "\n", "\r", "\u2028", "_", "-", "!", "\u00E9", "\u00C9", "\u00DF", "\u0130", "\u0131", "\u017F", "\u212A",
        "\u03C3", "\u03C2", "\u03A3", "\u00A0", "\uFEFF", "\u0000", "\U0001F600", "\U00010400", "\U00010428", "\uD800", "\uDBFF",
        "\uDC00", "\uDFFF", "\uD83D", "\uDE00", "ab", "aa", "\t",
    ];

    internal static string Around(FuzzRandom random, string sample, SortedSet<string> constructs)
    {
        var subject = random.Below(100) switch
        {
            < 35 => sample,
            < 50 => Some(random, 6) + sample + Some(random, 6),
            < 60 => Times(sample, random.Between(2, 5)) + (random.Chance(50) ? Some(random, 3) : ""),
            < 70 => Edit(random, sample),
            < 80 => Some(random, random.Skewed(64)),
            < 84 => "",
            < 88 => Times(sample.Length == 0 ? random.Pick(Noise) : sample, random.Between(64, 4096)),
            < 93 => Surrogates(random, sample),
            _ => sample.ToUpperInvariant() + Some(random, 2) + sample.ToLowerInvariant(),
        };

        if (subject.Length > 1 << 16)
            subject = subject[..(1 << 16)];
        Note(subject, constructs);
        return subject;
    }

    /// <summary>The subject constructs the session counts: empty, long, and surrogates alone and in pairs.</summary>
    internal static void Note(string subject, SortedSet<string> constructs)
    {
        if (subject.Length == 0)
            constructs.Add("subject-empty");
        if (subject.Length > 1024)
            constructs.Add("subject-long-run");
        for (var i = 0; i < subject.Length; i++)
        {
            if (char.IsHighSurrogate(subject[i]) && i + 1 < subject.Length && char.IsLowSurrogate(subject[i + 1]))
            {
                constructs.Add("subject-surrogate-pair");
                i++;
            }
            else if (char.IsSurrogate(subject[i]))
            {
                constructs.Add("subject-lone-surrogate");
            }
        }
    }

    private static string Some(FuzzRandom random, int count)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < count; i++)
            builder.Append(random.Pick(Noise));
        return builder.ToString();
    }

    private static string Times(string text, int count)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < count && builder.Length < 1 << 16; i++)
            builder.Append(text);
        return builder.ToString();
    }

    private static string Edit(FuzzRandom random, string sample)
    {
        if (sample.Length == 0)
            return random.Pick(Noise);
        var at = random.Below(sample.Length);
        return random.Below(3) switch
        {
            0 => sample.Remove(at, 1),
            1 => sample.Insert(at, random.Pick(Noise)),
            _ => sample.Remove(at, 1).Insert(at, random.Pick(Noise)),
        };
    }

    private static string Surrogates(FuzzRandom random, string sample)
    {
        var builder = new StringBuilder();
        foreach (var c in sample)
        {
            builder.Append(c);
            if (random.Chance(30))
                builder.Append(random.Pick(["\uD800", "\uDC00", "\U0001F600", "\U0010FFFF", "\uD83D"]));
        }

        return builder.Append(random.Pick(["\uD83D", "\uDE00", ""])).ToString();
    }
}

/// <summary>
/// Near-miss mutations: one edit to a pattern the grammar wrote that takes it, most often, to a refusal the parser must
/// answer as a syntax error or a status, never as an exception it does not declare.
/// </summary>
internal static class NearMiss
{
    private static readonly string[] Operators =
    [
        "swap-bounds", "unclosed-group", "stray-close", "unclosed-class", "duplicate-name", "undeclared-name",
        "property-name", "string-property", "unicode-escape", "quantified-assertion", "class-set-operators",
        "group-name", "modifier", "nothing-to-repeat", "back-reference", "flags-u-and-v", "edit", "truncate",
    ];

    internal static (string Pattern, RegexFlags Flags) Apply(FuzzRandom random, string pattern, RegexFlags flags, SortedSet<string> constructs)
    {
        var name = random.Pick(Operators);
        constructs.Add("near-miss-" + name);
        var unicode = (flags & (RegexFlags.Unicode | RegexFlags.UnicodeSets)) != 0;
        switch (name)
        {
            case "swap-bounds":
            {
                var open = pattern.IndexOf('{', StringComparison.Ordinal);
                var comma = open < 0 ? -1 : pattern.IndexOf(',', open);
                var close = comma < 0 ? -1 : pattern.IndexOf('}', comma);
                if (close > comma + 1 && comma > open + 1)
                    return (pattern[..(open + 1)] + pattern[(comma + 1)..close] + "," + pattern[(open + 1)..comma] + pattern[close..], flags);
                return (Insert(random, pattern, "a{3,2}"), flags);
            }

            case "unclosed-group":
            {
                var close = pattern.LastIndexOf(')');
                return (close >= 0 ? pattern.Remove(close, 1) : Insert(random, pattern, "(a"), flags);
            }

            case "stray-close":
                return (Insert(random, pattern, random.Pick([")", "]", "}", "|)"])), flags);
            case "unclosed-class":
            {
                var close = pattern.LastIndexOf(']');
                return (close >= 0 ? pattern.Remove(close, 1) : Insert(random, pattern, "[a"), flags);
            }

            case "duplicate-name":
                return (Insert(random, pattern, random.Pick(["(?<a>x)(?<a>y)", "(?:(?<a>x)|(?<a>y))", "(?<a>x|(?<a>y))", "(?:(?<a>x)|(?<a>y)|(?<b>z))\\k<a>", "(?<a>x)|(?<\\u0061>y)"])), flags);
            case "undeclared-name":
                return (Insert(random, pattern, "(?<q>a)\\k<zz>"), flags);
            case "property-name":
                return (Insert(random, pattern, "\\p{" + PatternGrammar.NearMissProperty(random) + "}"), random.Chance(70) ? (flags | RegexFlags.Unicode) & ~RegexFlags.UnicodeSets : flags);
            case "string-property":
            {
                var property = PatternGrammar.StringProperty(random);
                return random.Below(3) switch
                {
                    0 => (Insert(random, pattern, "\\P{" + property + "}"), (flags & ~RegexFlags.Unicode) | RegexFlags.UnicodeSets),
                    1 => (Insert(random, pattern, "\\p{" + property + "}"), (flags & ~RegexFlags.UnicodeSets) | RegexFlags.Unicode),
                    _ => (Insert(random, pattern, "[^\\p{" + property + "}]"), (flags & ~RegexFlags.Unicode) | RegexFlags.UnicodeSets),
                };
            }

            case "unicode-escape":
                return (Insert(random, pattern, random.Pick(["\\a", "\\-", "\\c1", "\\x4g", "\\u{110000}", "\\u{}", "\\u{FFFFFFFFF}", "\\uD83D\\u{DE00}", "\\k", "\\8"])), flags | (unicode ? RegexFlags.None : RegexFlags.Unicode));
            case "quantified-assertion":
                return (Insert(random, pattern, random.Pick(["(?<=a)*", "^*", "\\b+", "(?=a){2}", "$?", "(?!a)+", "(?<!a){1,}"])), flags);
            case "class-set-operators":
                return (Insert(random, pattern, random.Pick(["[a--b&&c]", "[a&&&b]", "[!!]", "[(]", "[a-\\d]", "[\\q{a]", "[^\\q{ab}]", "[a&&]", "[--a]", "[\\q{a|bc}--]", "[a-]", "[]a]"])), (flags & ~RegexFlags.Unicode) | RegexFlags.UnicodeSets);
            case "group-name":
                return (Insert(random, pattern, "(?<" + PatternGrammar.NearMissName(random) + ">x)"), flags);
            case "modifier":
                return (Insert(random, pattern, random.Pick(["(?ii:a)", "(?i-i:a)", "(?-:a)", "(?x:a)", "(?i", "(?<", "(?", "(?i-:", "(?-ims:a)", "(?msim:a)"])), flags);
            case "nothing-to-repeat":
                return (random.Pick(["*", "+", "?", "{2}", "{1,2}"]) + pattern, flags);
            case "back-reference":
                return (pattern + "\\" + random.Between(1, 99).ToString(CultureInfo.InvariantCulture), flags | (random.Chance(50) ? RegexFlags.Unicode : RegexFlags.None));
            case "flags-u-and-v":
                return (pattern, flags | RegexFlags.Unicode | RegexFlags.UnicodeSets);
            case "edit":
            {
                var characters = "()[]{}|*+?^$.\\-,0123456789<>=!:&pPqkucx";
                var at = random.Below(pattern.Length + 1);
                var character = characters[random.Below(characters.Length)].ToString();
                return random.Below(3) switch
                {
                    0 => (pattern.Insert(at, character), flags),
                    1 when at < pattern.Length => (pattern.Remove(at, 1), flags),
                    _ when at < pattern.Length => (pattern.Remove(at, 1).Insert(at, character), flags),
                    _ => (pattern + character, flags),
                };
            }

            default:
                return (pattern.Length == 0 ? "(" : pattern[..random.Below(pattern.Length)], flags);
        }
    }

    private static string Insert(FuzzRandom random, string pattern, string text)
    {
        var at = random.Below(pattern.Length + 1);
        return pattern.Insert(at, text);
    }
}

/// <summary>The construct and outcome classes a session must reach before its silence may be read as covering the surface.</summary>
internal static class Coverage
{
    /// <summary>Every construct class the generator can write; a session at the declared seed and iterations reaches each.</summary>
    internal static readonly string[] Constructs =
    [
        "alternation", "empty-alternative", "assertion-start", "assertion-end", "word-boundary", "non-word-boundary",
        "lookahead", "negative-lookahead", "lookbehind", "negative-lookbehind", "quantified-lookahead", "dot",
        "literal", "non-ascii-literal", "astral-literal", "lone-surrogate-literal", "line-terminator-literal",
        "annex-b-literal-brace", "class-escape", "control-escape", "control-letter", "nul-escape", "hex-escape",
        "unicode-escape", "surrogate-escape", "braced-unicode-escape", "surrogate-pair-escape", "identity-escape",
        "annex-b-identity-escape", "annex-b-octal-escape", "annex-b-incomplete-escape", "property", "property-value",
        "negated-property", "property-of-strings", "property-in-class", "class", "negated-class", "class-nothing", "class-everything", "class-range",
        "astral-class-range", "class-escape-in-class", "annex-b-class-escape-range", "escape-in-class",
        "annex-b-class-escape", "class-set", "negated-class-set", "class-set-union", "class-set-intersection",
        "class-set-subtraction", "class-set-range", "nested-class-set", "class-string-disjunction", "empty-class-string",
        "class-escape-in-class-set", "escaped-class-set-character", "capturing-group", "non-capturing-group",
        "named-group", "escaped-group-name", "astral-group-name", "repeated-group-name", "modifier-group",
        "back-reference", "forward-or-missing-back-reference", "named-back-reference", "star", "plus", "question",
        "exact-bound", "open-bound", "range-bound", "large-bound", "huge-bound", "lazy",
        "mode-none", "mode-u", "mode-v", "mode-u-and-v", "flag-i", "flag-m", "flag-s", "flag-y",
        "subject-empty", "subject-long-run", "subject-surrogate-pair", "subject-lone-surrogate",
        "class-K", "class-A", "class-L", "class-G", "class-W", "class-N", "class-F", "class-M", "class-V", "class-O",
        "nesting-below-limit", "nesting-at-limit", "nesting-one-past", "nesting-far-past", "engine-ceiling-subject",
        "near-miss-range-out-of-order", "near-miss-swap-bounds", "near-miss-unclosed-group", "near-miss-stray-close",
        "near-miss-unclosed-class", "near-miss-duplicate-name", "near-miss-undeclared-name", "near-miss-property-name",
        "near-miss-string-property", "near-miss-unicode-escape", "near-miss-quantified-assertion",
        "near-miss-class-set-operators", "near-miss-group-name", "near-miss-modifier", "near-miss-nothing-to-repeat",
        "near-miss-back-reference", "near-miss-flags-u-and-v", "near-miss-edit", "near-miss-truncate",
    ];

    /// <summary>Every outcome a session must have seen: each status of each entry, each limit, and the meter's exception.</summary>
    internal static readonly string[] Outcomes =
    [
        "compile:ok", "compile:syntax", "compile:limit:nesting", "compile:limit:compile", "compile:meter",
        "offset:match", "offset:nomatch", "offset:limit:steps", "offset:limit:frames", "offset:limit:trail", "offset:meter",
        "string:match", "string:nomatch", "string:limit", "is-match:throws", "matches:compared",
        "escape-pattern:ok", "escape-pattern:limit", "escape-string:ok", "escape-string:limit", "probes",
    ];

    internal static IEnumerable<string> Missing(IReadOnlyDictionary<string, long> reached) =>
        Constructs.Select(static c => "construct:" + c).Concat(Outcomes).Where(key => !reached.ContainsKey(key));
}
