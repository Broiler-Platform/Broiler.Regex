using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Broiler.Regex.Text;
using Broiler.Unicode.Tables;

namespace Broiler.Regex.Probe;

/// <summary>
/// The text helpers in the probe's report (task 2.7 of SJL-11.2, requirement R9): <see cref="RegexFlagsText"/> and
/// <see cref="RegexEscaper"/> over the populations their tests enumerate, so that the Native AOT run answers them as the
/// JIT run does, byte for byte (convention 12; bundle <c>sjl-11-001</c>'s check E-2). Each population is written as its
/// count and the SHA-256 of its answers, one line per input, and a few inputs are written out with their answers.
/// </summary>
internal static class TextPopulation
{
    /// <summary>The population's name, written into its first line.</summary>
    internal const string Name = "text-1";

    private const string Letters = "dgimsuvy";

    /// <summary>The text section of the report: a header line, one line per population, and the inputs written out.</summary>
    internal static string Report()
    {
        var report = new StringBuilder();
        report.Append("Broiler.Regex.Probe text population ").Append(Name).Append('\n');

        // Flags: every ordering of every set of the eight flags, and every code unit alone.
        var flags = new Digest();
        foreach (var text in Arrangements())
            flags.Add(Flags(text));
        for (var unit = 0; unit <= 0xFFFF; unit++)
            flags.Add(Flags(((char)unit).ToString()));
        flags.Write(report, "flags");

        // RegExp.escape's text: every code point alone and after an underscore.
        var escaper = new RegexEscaper(UnicodeTables.Instance);
        var escapes = new Digest();
        for (var codePoint = 0; codePoint <= 0x10FFFF; codePoint++)
        {
            var text = codePoint is >= 0xD800 and <= 0xDFFF ? ((char)codePoint).ToString() : char.ConvertFromUtf32(codePoint);
            escapes.Add(Escaped(escaper, text));
            escapes.Add(Escaped(escaper, "_" + text));
        }

        escapes.Write(report, "escape");

        // The source text: every code unit alone, after a backslash, in a class and after an escaped bracket.
        var sources = new Digest();
        for (var code = 0; code <= 0xFFFF; code++)
        {
            var unit = ((char)code).ToString();
            foreach (var pattern in new[] { unit, "\\" + unit, "[" + unit + "]", "\\[" + unit })
                sources.Add(Source(pattern, int.MaxValue));
        }

        sources.Write(report, "source");

        // Inputs written out: Broiler.VM's correspondence rows F1, T4 and T5, and the meter and the maximum length.
        foreach (var text in new[] { "", "g", "gg", "x", "G", "uv", "vu", "uvx", "dgimsuy", "yg" })
            report.Append("flags ").Append(Show(text)).Append(": ").Append(Flags(text)).Append('\n');
        foreach (var text in new[] { "abc", "1a", "_", "a b", "\u0085", "\u00A0", "\uFEFF", "\u2028", "\u180E", "\uD800", "\uD83D\uDE00", "x\uD83D", ",-=<>#&!%:;@~'`\"", "^$\\.*+?()[]{}|/", "\t\n\u000B\f\r" })
            report.Append("escape ").Append(Show(text)).Append(": ").Append(Escaped(escaper, text)).Append('\n');
        foreach (var pattern in new[] { "", "/", "\\/", "a/b", "[/]", "\n", "\\\n", "\\\u2028", "\u2029", "\uD800", "\u0085", "\\[/\\]" })
            report.Append("source ").Append(Show(pattern)).Append(": ").Append(Source(pattern, int.MaxValue)).Append('\n');
        report.Append("source \"\\u2028\\u2028\" under 11: ").Append(Source("\u2028\u2028", 11)).Append('\n');
        report.Append("escape \".\" * 1000 charges ").Append(Charges(meter => escaper.EscapeString(new string('.', 1000), int.MaxValue, meter, out _))).Append('\n');
        report.Append("source \"/\" * 1000 charges ").Append(Charges(meter => RegexEscaper.EscapePattern(new string('/', 1000), int.MaxValue, meter, out _))).Append('\n');
        return report.ToString();
    }

    private static string Flags(string text)
    {
        var status = RegexFlagsText.Parse(text, out var flags, out var position);
        return status == RegexFlagsStatus.Success
            ? "Success " + Show(RegexFlagsText.Format(flags))
            : status + " at " + position.ToString(CultureInfo.InvariantCulture);
    }

    private static string Escaped(RegexEscaper escaper, string text)
    {
        var status = escaper.EscapeString(text, int.MaxValue, null, out var escaped);
        return status == RegexStatus.Success ? Show(escaped!) : status.ToString();
    }

    private static string Source(string pattern, int maxLength)
    {
        var status = RegexEscaper.EscapePattern(pattern, maxLength, null, out var source);
        return status == RegexStatus.Success ? Show(source!) : status.ToString();
    }

    /// <summary>The units an operation pays its meter, call by call.</summary>
    private static string Charges(Func<RegexWorkCharge, RegexStatus> operation)
    {
        var charges = new List<string>();
        var status = operation(units => charges.Add(units.ToString(CultureInfo.InvariantCulture)));
        return string.Join(" ", charges) + " (" + status + ")";
    }

    /// <summary>Every ordering of every subset of the eight flags.</summary>
    private static IEnumerable<string> Arrangements()
    {
        var stack = new Stack<string>();
        stack.Push("");
        while (stack.Count > 0)
        {
            var prefix = stack.Pop();
            yield return prefix;
            foreach (var letter in Letters)
            {
                if (prefix.IndexOf(letter, StringComparison.Ordinal) < 0)
                    stack.Push(prefix + letter);
            }
        }
    }

    /// <summary>A text, every code unit outside printable ASCII written as <c>\uXXXX</c>, in quotes.</summary>
    private static string Show(string text)
    {
        var shown = new StringBuilder("\"");
        foreach (var unit in text)
        {
            if (unit is >= ' ' and <= '~' && unit != '"' && unit != '\\')
                shown.Append(unit);
            else
                shown.Append("\\u").Append(((int)unit).ToString("X4", CultureInfo.InvariantCulture));
        }

        return shown.Append('"').ToString();
    }

    /// <summary>A population's answers, one line each, counted and hashed.</summary>
    private sealed class Digest
    {
        private readonly StringBuilder _answers = new();
        private int _count;

        internal void Add(string answer)
        {
            _answers.Append(answer).Append('\n');
            _count++;
        }

        internal void Write(StringBuilder report, string name)
        {
            var digest = SHA256.HashData(Encoding.UTF8.GetBytes(_answers.ToString()));
            report.Append(name).Append(": inputs ").Append(_count.ToString(CultureInfo.InvariantCulture)).Append(", answers sha256 ")
                .Append(Convert.ToHexStringLower(digest)).Append('\n');
        }
    }
}
