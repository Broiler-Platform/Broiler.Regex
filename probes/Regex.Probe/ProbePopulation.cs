using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Broiler.Regex.Matching;

namespace Broiler.Regex.Probe;

/// <summary>
/// One case of the probe's population: a pattern and its flags, a subject, and the index the match starts at;
/// with <see cref="All"/>, every match the global iteration finds from the start of the subject.
/// </summary>
internal sealed record ProbeCase(string Id, string Pattern, string Flags, string Subject, int Start, bool All = false);

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
/// the Unicode data the package carries, the global iteration, and refusals. It holds no declared answer: the
/// report is what the package answers, and the Native AOT run is held to the JIT run, byte for byte. The vectors
/// and the adversarial corpus, which do declare answers, are added to the probe by SJL-11.2's later steps.
/// </para>
/// <para>
/// The report names each case with its pattern, flags, start and subject, escaped to ASCII, and the answer; it
/// records no time, machine, path or message text, so two runs on one platform compare byte for byte.
/// </para>
/// </remarks>
internal static class ProbePopulation
{
    /// <summary>The population's name, written into the report's first line.</summary>
    internal const string Name = "smoke-1";

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
    ];

    /// <summary>Compiles every case once, as an engine compiles a pattern once and matches it many times.</summary>
    internal static CompiledCase[] Compile(IReadOnlyList<ProbeCase> cases)
    {
        var compiled = new CompiledCase[cases.Count];
        for (var i = 0; i < cases.Count; i++)
        {
            var probeCase = cases[i];
            try
            {
                compiled[i] = new CompiledCase(probeCase, new BroilerRegex(probeCase.Pattern, probeCase.Flags), null);
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
                .Append(" /").Append(Escape(probeCase.Pattern)).Append('/').Append(probeCase.Flags)
                .Append(probeCase.All ? " all" : " @" + Decimal(probeCase.Start))
                .Append(" \"").Append(Escape(probeCase.Subject)).Append("\": ")
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
        if (!match.Success)
            return "nomatch";

        var text = new StringBuilder("match ").Append(Span(match.Index, match.Length));
        for (var group = 1; group < match.Groups.Count; group++)
        {
            var captured = match.Groups[group];
            text.Append(' ').Append(captured.Success ? Span(captured.Index, captured.Length) : "-");
        }

        var names = new List<string>();
        foreach (var (name, index) in regex.GroupNames)
            names.Add(name + "=" + Decimal(index));
        names.Sort(StringComparer.Ordinal);
        if (names.Count > 0)
            text.Append(" names ").Append(string.Join(",", names));
        return text.ToString();
    }

    private static string Span(int index, int length) => Decimal(index) + "," + Decimal(index + length);

    /// <summary>
    /// An exception as the probe reports it: the library's own two by name, and with the syntax error's
    /// position; any other by its type, which no message or resource text enters.
    /// </summary>
    private static string Thrown(Exception exception) => exception switch
    {
        RegexSyntaxException syntax => "syntax @" + Decimal(syntax.Position),
        RegexOverflowException => "overflow",
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
