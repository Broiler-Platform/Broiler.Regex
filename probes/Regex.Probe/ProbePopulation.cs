using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Broiler.Unicode.Tables;

namespace Broiler.Regex.Probe;

/// <summary>
/// One case of the probe's population: a pattern and its flags, a subject, and the index the match starts at;
/// with <see cref="All"/>, every match the global iteration finds from the start of the subject. With
/// <see cref="NestingLimit"/>, the pattern is compiled by <see cref="BroilerRegex.Compile"/> under that limit, and
/// otherwise by the constructor; <see cref="Shown"/> and <see cref="SubjectShown"/> are how the report writes a pattern
/// and a subject too long to write out.
/// </summary>
internal sealed record ProbeCase(string Id, string Pattern, string Flags, string Subject, int Start, bool All = false,
    int? NestingLimit = null, string? Shown = null, string? SubjectShown = null);

/// <summary>A case compiled once: the instance every run shares, or the answer compilation gave instead.</summary>
internal sealed record CompiledCase(ProbeCase Case, BroilerRegex? Regex, string? Refusal);

/// <summary>
/// The probe's population and its rendering, shared by the probe, which runs it under the JIT and under Native
/// AOT (convention 12), and by the architecture tests' rule SL9, which runs it twice and on two threads through
/// the same compiled instances (conventions 6 and 10).
/// </summary>
/// <remarks>
/// <para>
/// The population is a smoke population of the public API as it stands at SJL-11.2's first step: the common
/// grammar, Annex B, flags, look-around in both directions, back-references, named groups, modifiers, case
/// folding in both modes, astral code points, Unicode property escapes and <c>v</c>-mode set operations over
/// the Unicode owner's tables (<c>Broiler.Unicode.Tables</c>, which the probe hands to every compilation as the
/// library's provider), the global iteration, refusals, and, from task 2.3 of SJL-11.2, nesting at the library's
/// maximum, one level past it and far past it, compiled under a nesting limit and run on a thread of
/// <see cref="StackBytes"/>, so that the Native AOT run proves the maximum as the JIT run does (requirement R4): a
/// pattern at the maximum parses and compiles, one of the shapes the maximum proves matching for matches a subject that
/// reaches its innermost level, and one level past is refused. It holds no declared answer: the
/// report is what the package answers, and the Native AOT run is held to the JIT run, byte for byte. The vectors
/// and the adversarial corpus, which do declare answers, are added to the probe by SJL-11.2's later steps.
/// </para>
/// <para>
/// The report names each case with its pattern, flags, start and subject, escaped to ASCII, and the answer, whose
/// group names are escaped the same way; it records no time, machine, path or message text, so two runs on one
/// platform compare byte for byte.
/// </para>
/// </remarks>
internal static class ProbePopulation
{
    /// <summary>The population's name, written into the report's first line.</summary>
    internal const string Name = "smoke-1";

    /// <summary>
    /// The stack the probe runs the population on: 1 MiB, the smallest stack the adversarial corpus declares
    /// (<c>tests/Adversarial/corpus.tsv</c>, <c>stack-bytes</c>), on which the library's nesting maximum is proven
    /// (requirement R4, decision D6 of Broiler.VM's JSD-0081).
    /// </summary>
    internal const int StackBytes = 1 << 20;

    internal static ProbeCase[] Cases() =>
    [
        // The common grammar.
        new("C01", "a", "", "bab", 0),
        new("C02", "abc", "", "xxabcxx", 0),
        new("C03", "^abc$", "m", "x\nabc\ny", 0),
        new("C04", "a.c", "", "a\nc abc", 0),
        new("C05", "a.c", "s", "a\nc", 0),
        new("C06", "\\d+", "", "ab123cd", 0),
        new("C07", "\\w+\\s\\w+", "", "hello world", 0),
        new("C08", "[a-z]+", "i", "ABC def", 0),
        new("C09", "colou?r", "", "color colour", 0, All: true),
        new("C10", "a{2,3}", "", "aaaa", 0),
        new("C11", "a+?", "", "aaa", 0),
        new("C12", "(a)|(b)", "", "b", 0),
        new("C13", "(a*)*b", "", "aaab", 0),
        new("C14", "(z)((a+)?(b+)?(c))*", "", "zaacbbbcac", 0),
        new("C15", "\\bfoo\\b", "", "a foo b", 0),
        new("C16", "[^]", "", "x", 0),
        new("C17", "\\cJ", "", "\n", 0),
        new("C18", "a", "", "aaa", 5),

        // Groups, references and assertions.
        new("G01", "(?<year>\\d{4})-(?<month>\\d{2})", "", "on 2026-10", 0),
        new("G02", "(a)\\1", "", "aa", 0),
        new("G03", "(?<x>a)\\k<x>", "", "aa", 0),
        new("G04", "(?=a)a", "", "ba", 0),
        new("G05", "(?!a)\\w", "", "ab", 0),
        new("G06", "(?<=\\$)\\d+", "", "cost $42", 0),
        new("G07", "(?<!\\$)\\b\\d+", "", "$42 17", 0),
        new("G08", "(?<=(\\d+)(\\d+))$", "", "1053", 0),
        new("G09", "(?i:a)b", "", "Ab aB", 0, All: true),
        new("G10", "(?<a>.)(?<b>.)\\k<b>\\k<a>", "u", "xabba", 0),

        // Flags.
        new("F01", "a", "y", "ba", 0),
        new("F02", "a", "y", "ba", 1),
        new("F03", "", "", "abc", 0, All: true),
        new("F04", "", "u", "😀", 0, All: true),
        new("F05", "", "", "😀", 0, All: true),

        // Annex B.
        new("B01", "\\0", "", "\u0000", 0),
        new("B02", "\\8", "", "8", 0),
        new("B03", "[\\d-x]", "", "-", 0),
        new("B04", "a{", "", "a{", 0),
        new("B05", "\\101", "", "A", 0),

        // Case folding and code points.
        new("U01", "ſ", "i", "s", 0),
        new("U02", "ſ", "iu", "S", 0),
        new("U03", "\\u{1F600}", "u", "x😀", 0),
        new("U04", "^.$", "u", "😀", 0),
        new("U05", "^.$", "", "😀", 0),
        new("U06", "\\u{10400}", "iu", "𐐨", 0),
        new("U07", "\\w", "iu", "K", 0),
        new("U08", "\\W", "iu", "ſ", 0),

        // Unicode property escapes and v-mode set operations, over the package's Unicode data.
        new("P01", "\\p{Script=Greek}+", "u", "123αβ", 0),
        new("P02", "\\p{L}+", "u", "123ñü", 0),
        new("P03", "\\P{L}+", "u", "ab12cd", 0),
        new("P04", "\\p{Lu}", "iu", "a", 0),
        new("P05", "[\\p{L}--[a-z]]+", "v", "abcDEF", 0),
        new("P06", "[\\p{Lu}&&[A-C]]+", "v", "ABCDE", 0),
        new("P07", "[\\q{abc|d}]", "v", "xabc", 0),
        new("P08", "\\p{RGI_Emoji}", "v", "a👍🏽b", 0),
        new("P09", "[^\\P{Lowercase_Letter}]", "iv", "A", 0),
        new("P10", "\\p{Script_Extensions=Latin}", "u", "ͣ", 0),

        // Refusals.
        new("R01", "(", "", "", 0),
        new("R02", "a{2,1}", "", "", 0),
        new("R03", "\\p{Foo}", "u", "", 0),
        new("R04", "a", "gg", "", 0),
        new("R05", "a", "uv", "", 0),
        new("R06", "(?<a>x)(?<a>y)", "", "", 0),
        new("R07", "[z-a]", "", "", 0),
        new("R08", "\\k<a>", "u", "", 0),
        new("R09", "(?<\\u{1D400}>x)", "", "x", 0),
        new("R10", "a**", "", "", 0),
        new("R11", "(?<\\uD835\\uDC00>x)", "", "x", 0),

        // Nesting (R4, decision D6): at BroilerRegex.MaximumNestingLimit, one level past it and far past it, compiled
        // under a nesting limit and matched, on a thread of StackBytes. Depth counts as Broiler.VM's does: the pattern
        // is depth 1, each group adds 1, and under v each class adds 1; so 511 levels are the maximum, 512. Every
        // subject of a case at the maximum reaches the innermost level. Before task 2.4c routed the public members through
        // task 2.4a's backend, the shapes whose match at the maximum the old matcher did not prove (N10, N17) were matched
        // against a subject that failed at the outermost level; they now reach the innermost too.
        Nested("N01", "(", "a", ")", 511, "", "a"),
        Nested("N02", "(", "a", ")", 512, "", "a"),
        Nested("N03", "(?:", "a", ")", 511, "", "a"),
        Nested("N04", "(?:", "a", ")", 512, "", "a"),
        Nested("N05", "[", "a", "]", 511, "v", "a"),
        Nested("N06", "[", "a", "]", 512, "v", "a"),
        Nested("N07", "(?=", "a", ")", 511, "", "a"),
        Nested("N08", "(?<=", "a", ")", 511, "", "a", prefix: "a"),
        Nested("N09", "(?i:", "a", ")", 511, "", "A"),

        // A repeated alternation of sequences at the maximum, matched to the innermost level.
        Nested("N10", "(?:b|x", "a", "x)+", 511, "", "a", before: "x", after: "x"),
        Nested("N11", "(?:x?", "a", "x?)", 511, "", "a"),
        Nested("N12", "(", "a", ")", 1_000_000, "", "a"),
        Nested("N13", "[", "a", "]", 1_000_000, "v", "a"),
        Nested("N14", "(", "a", ")", 512, "", "a", limit: null),

        // The general quantifier's driver at every level, to the innermost: a repeated capturing group.
        Nested("N15", "(", "a", ")+", 511, "", "a"),

        // The shape that costs the matcher's continuations the most stack among those the maximum proves matching for:
        // an alternation of sequences, on a subject that reaches the innermost level.
        Nested("N16", "(?:b|x", "a", "x)", 511, "", "a", before: "x", after: "x"),

        // The review's witness of a match at the maximum that exhausted a 1 MiB stack under the JIT on the old matcher: a
        // capture around an alternation of sequences with back-references and assertions. Matched to the innermost level
        // at the maximum, and refused one level past it.
        NumberedBackReferences("N17", 511),
        NumberedBackReferences("N18", 512),
    ];

    /// <summary>
    /// A case of <paramref name="levels"/> nested <paramref name="open"/> ... <paramref name="close"/> around
    /// <paramref name="inner"/>, compiled under <paramref name="limit"/> (the library's maximum unless given; null
    /// for the constructor, which parses under that maximum). The subject is <paramref name="subject"/> with
    /// <paramref name="before"/> and <paramref name="after"/> once a level around it.
    /// </summary>
    private static ProbeCase Nested(string id, string open, string inner, string close, int levels, string flags, string subject,
        string prefix = "", int? limit = BroilerRegex.MaximumNestingLimit, string before = "", string after = "")
    {
        var pattern = new StringBuilder(prefix);
        for (var i = 0; i < levels; i++)
            pattern.Append(open);
        pattern.Append(inner);
        for (var i = 0; i < levels; i++)
            pattern.Append(close);

        // Written as the adversarial corpus writes a repeated text: 'x'^k is k copies of x.
        var shown = (prefix.Length > 0 ? "'" + Escape(prefix) + "' + " : "") + "'" + Escape(open) + "'^" + Decimal(levels)
            + " + '" + Escape(inner) + "' + '" + Escape(close) + "'^" + Decimal(levels);
        if (before.Length == 0 && after.Length == 0)
            return new ProbeCase(id, pattern.ToString(), flags, subject, 0, NestingLimit: limit, Shown: shown);

        var text = new StringBuilder();
        for (var i = 0; i < levels; i++)
            text.Append(before);
        text.Append(subject);
        for (var i = 0; i < levels; i++)
            text.Append(after);
        var subjectShown = "'" + Escape(before) + "'^" + Decimal(levels) + " + '" + Escape(subject) + "' + '" + Escape(after) + "'^" + Decimal(levels);
        return new ProbeCase(id, pattern.ToString(), flags, text.ToString(), 0, NestingLimit: limit, Shown: shown, SubjectShown: subjectShown);
    }

    /// <summary>
    /// A case of <paramref name="levels"/> nested capturing groups, the level <c>k</c> (1 the outermost) an alternation of
    /// <c>b</c> and a sequence that opens with <c>x\k\B</c> and closes with <c>\kx</c>, around <c>a</c>, compiled under the
    /// library's maximum and matched against <c>x</c> once a level, <c>a</c> and <c>x</c> once a level, which reaches the
    /// innermost level: each reference names its own group, not yet closed, and matches the empty string.
    /// </summary>
    private static ProbeCase NumberedBackReferences(string id, int levels)
    {
        var pattern = new StringBuilder();
        for (var k = 1; k <= levels; k++)
            pattern.Append("(b|x\\").Append(Decimal(k)).Append("\\B");
        pattern.Append('a');
        for (var k = levels; k >= 1; k--)
            pattern.Append("\\").Append(Decimal(k)).Append("x)");

        var shown = "'(b|x" + Escape("\\") + "k" + Escape("\\") + "B' for k = 1 to " + Decimal(levels) + " + 'a' + '"
            + Escape("\\") + "kx)' for k = " + Decimal(levels) + " to 1";
        var subject = new string('x', levels) + "a" + new string('x', levels);
        return new ProbeCase(id, pattern.ToString(), "", subject, 0, NestingLimit: BroilerRegex.MaximumNestingLimit, Shown: shown,
            SubjectShown: "'x'^" + Decimal(levels) + " + 'a' + 'x'^" + Decimal(levels));
    }

    /// <summary>Compiles every case once, as an engine compiles a pattern once and matches it many times.</summary>
    internal static CompiledCase[] Compile(IReadOnlyList<ProbeCase> cases)
    {
        var compiled = new CompiledCase[cases.Count];
        for (var i = 0; i < cases.Count; i++)
        {
            var probeCase = cases[i];
            try
            {
                if (probeCase.NestingLimit is { } limit)
                {
                    var status = BroilerRegex.Compile(probeCase.Pattern, RegexFlagsParser.Parse(probeCase.Flags), UnicodeTables.Instance, limit, null, null, out var regex, out var exceeded);
                    compiled[i] = status == RegexStatus.Success
                        ? new CompiledCase(probeCase, regex, null)
                        : new CompiledCase(probeCase, null, exceeded == RegexLimit.Nesting ? "limit:nesting" : "limit:another");
                    continue;
                }

                compiled[i] = new CompiledCase(probeCase, new BroilerRegex(probeCase.Pattern, probeCase.Flags, UnicodeTables.Instance), null);
            }
            catch (Exception exception)
            {
                compiled[i] = new CompiledCase(probeCase, null, Thrown(exception));
            }
        }

        return compiled;
    }

    /// <summary>The report of one run over compiled cases: a header line, one line per case, and a digest line.</summary>
    internal static string Report(IReadOnlyList<CompiledCase> compiled)
    {
        var report = new StringBuilder();
        report.Append("Broiler.Regex.Probe population ").Append(Name).Append('\n');
        var answers = new StringBuilder();
        foreach (var item in compiled)
        {
            var answer = Answer(item);
            answers.Append(answer).Append('\n');
            var probeCase = item.Case;
            report.Append(probeCase.Id)
                .Append(" /").Append(probeCase.Shown ?? Escape(probeCase.Pattern)).Append('/').Append(probeCase.Flags)
                .Append(probeCase.NestingLimit is { } limit ? " nesting " + Decimal(limit) : "")
                .Append(probeCase.All ? " all" : " @" + Decimal(probeCase.Start))
                .Append(probeCase.SubjectShown is { } subjectShown ? " " + subjectShown + ": " : " \"" + Escape(probeCase.Subject) + "\": ")
                .Append(answer).Append('\n');
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(answers.ToString()));
        report.Append("cases ").Append(Decimal(compiled.Count)).Append(", answers sha256 ").Append(Convert.ToHexStringLower(digest)).Append('\n');
        return report.ToString();
    }

    /// <summary>One case's answer: its refusal, no match, a match with every group, or every match of the iteration.</summary>
    internal static string Answer(CompiledCase item)
    {
        if (item.Regex is null)
            return item.Refusal!;

        try
        {
            if (!item.Case.All)
                return Render(item.Regex, item.Regex.Match(item.Case.Subject, item.Case.Start));

            var matches = new List<string>();
            foreach (var match in item.Regex.Matches(item.Case.Subject))
            {
                matches.Add(Render(item.Regex, match));
                if (matches.Count == 16)
                    break;
            }

            return matches.Count == 0 ? "nomatch" : string.Join(" | ", matches);
        }
        catch (Exception exception)
        {
            return Thrown(exception);
        }
    }

    private static string Render(BroilerRegex regex, RegexMatch match)
    {
        if (match.Limit != RegexLimit.None)
            return "limit:" + (match.Limit == RegexLimit.Frames ? "frames" : match.Limit == RegexLimit.Trail ? "trail" : "steps");
        if (!match.Success)
            return "nomatch";

        // A run of eight or more equal groups is written once with its count, as x^k (the adversarial corpus's notation).
        var text = new StringBuilder("match ").Append(Span(match.Index, match.Length));
        for (var group = 1; group < match.Groups.Count;)
        {
            var captured = match.Groups[group];
            var shown = captured.Success ? Span(captured.Index, captured.Length) : "-";
            var run = 1;
            while (group + run < match.Groups.Count && Shown(match.Groups[group + run]) == shown)
                run++;
            text.Append(' ').Append(shown);
            if (run >= 8)
                text.Append('^').Append(Decimal(run));
            else
            {
                for (var i = 1; i < run; i++)
                    text.Append(' ').Append(shown);
            }
            group += run;
        }

        var names = new List<string>();
        foreach (var (name, index) in regex.GroupNames)
            names.Add(Escape(name) + "=" + Decimal(index));
        names.Sort(StringComparer.Ordinal);
        if (names.Count > 0)
            text.Append(" names ").Append(string.Join(",", names));
        return text.ToString();
    }

    private static string Span(int index, int length) => Decimal(index) + "," + Decimal(index + length);

    private static string Shown(RegexGroup group) => group.Success ? Span(group.Index, group.Length) : "-";

    /// <summary>
    /// An exception as the probe reports it: the library's own, the syntax error, by name and with its
    /// position; any other by its type, which no message or resource text enters.
    /// </summary>
    private static string Thrown(Exception exception) => exception switch
    {
        RegexSyntaxException syntax => "syntax @" + Decimal(syntax.Position),
        ArgumentOutOfRangeException => "threw ArgumentOutOfRangeException",
        ArgumentException => "threw ArgumentException",
        InvalidOperationException => "threw InvalidOperationException",
        IndexOutOfRangeException => "threw IndexOutOfRangeException",
        _ => "threw another exception",
    };

    private static string Escape(string text)
    {
        var escaped = new StringBuilder();
        foreach (var unit in text)
        {
            if (unit is >= ' ' and <= '~' && unit is not '\\' and not '"')
                escaped.Append(unit);
            else
                escaped.Append("\\u").Append(((int)unit).ToString("X4", NumberFormatInfo.InvariantInfo));
        }

        return escaped.ToString();
    }

    private static string Decimal(int value) => value.ToString(NumberFormatInfo.InvariantInfo);
}
