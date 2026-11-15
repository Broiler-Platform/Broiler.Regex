using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Broiler.Regex.Text;
using Broiler.Unicode.Algorithms;

namespace Broiler.Regex.Test262Library;

/// <summary>Which of check B-2's three populations a file is in.</summary>
internal enum Population
{
    /// <summary>(i) <c>test/language/literals/regexp</c> and <c>test/annexB/language/literals/regexp</c>.</summary>
    Literals,

    /// <summary>(ii) <c>test/built-ins/RegExp/property-escapes/generated</c>.</summary>
    PropertyEscapes,

    /// <summary>(iii) <c>test/built-ins/RegExp/unicodeSets/generated</c>.</summary>
    UnicodeSets,
}

/// <summary>
/// The library job of task 2.10 of SJL-11.2 (bundle <c>sjl-11-001</c>'s check B-2; decision D14 of Broiler.VM's JSD-0081):
/// test262's literal syntax tests, <c>property-escapes/generated</c> and <c>unicodeSets/generated</c> run through the
/// library alone, with no host. Its sources are compiled into the Native AOT probe too (<c>--test262</c>), so that CI
/// runs it under the JIT and Native AOT and compares the two reports byte for byte.
/// </summary>
/// <remarks>
/// <para><b>The files.</b> Every <c>.js</c> file but a <c>_FIXTURE</c> under the three populations' directories of a
/// test262 checkout, in ordinal order of their paths, each read as UTF-8; its front matter (<c>/*---</c> to
/// <c>---*/</c>) says whether it is negative (<c>negative:</c> with <c>phase: parse</c> and <c>type:
/// SyntaxError</c>).</para>
/// <para><b>(i) A literal file.</b> Its regular-expression literals are those the declared grammar extracts
/// (<see cref="JavaScriptTokens"/>). Each is read as <c>IsValidRegularExpressionLiteral</c> reads it, twice: by the
/// validate entry over the flags' text, and by <see cref="RegexFlagsText.Parse"/> then <see cref="BroilerRegex.Compile"/>;
/// both at the nesting limit <see cref="BroilerRegex.MaximumNestingLimit"/>, with no compile budget and no meter. The two
/// must agree. A positive file passes when every literal is accepted; a negative file when one is refused with the
/// syntax status, as the file's SyntaxError then comes from it.</para>
/// <para><b>(ii) and (iii) A generated file</b>, read by <see cref="GeneratedReader"/>: each helper call is run as
/// <c>harness/regExpUtils.js</c> runs it, <c>regExp.test</c> being <see cref="BroilerRegex.Compile"/> as above and the
/// offset entry from index 0, unanchored (no file's literal has <c>g</c> or <c>y</c>), under no step ceiling and the
/// frame and trail maxima. A file passes when no assertion of its calls fails; a negative one (<c>$DONOTEVALUATE()</c>)
/// as a negative literal file does.</para>
/// <para><b>Unreadable files.</b> A file with no literal the grammar extracts, a generated file the structured reader
/// does not read, or a negative file whose SyntaxError is not a literal's (the grammar extracts only accepted literals
/// from it), cannot be judged by this runner. The list <c>test262/library-unreadable.txt</c> names each such file with
/// why. A listed file is run and reported with the runner's own reading beside the list's why, and is not judged; a
/// file the runner cannot read that the list does not name, a listed file the runner reads and passes, and a listed
/// path the populations do not hold each fail the run, so that the list stays exact.</para>
/// <para><b>The report</b> holds a line per file and the counts, and no time, absolute path or machine, so that the
/// JIT's and Native AOT's compare byte for byte on one platform.</para>
/// </remarks>
internal sealed class LibraryRun
{
    /// <summary>The stack of the run's thread: 16 MiB, Broiler.JS's shell's for a script.</summary>
    internal const int StackBytes = 16 * 1024 * 1024;

    private const int Nesting = BroilerRegex.MaximumNestingLimit;

    private readonly IUnicodeTables _tables;
    private readonly RegexMatchCeilings _ceilings = new(null, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum);

    internal LibraryRun(IUnicodeTables tables) => _tables = tables;

    /// <summary>The directory of each population under a test262 checkout, in the report's order.</summary>
    internal static IEnumerable<(Population Population, string Directory)> Directories()
    {
        yield return (Population.Literals, "test/annexB/language/literals/regexp");
        yield return (Population.Literals, "test/language/literals/regexp");
        yield return (Population.PropertyEscapes, "test/built-ins/RegExp/property-escapes/generated");
        yield return (Population.UnicodeSets, "test/built-ins/RegExp/unicodeSets/generated");
    }

    /// <summary>
    /// Runs every file of the checkout at <paramref name="test262"/> on a thread of <see cref="StackBytes"/> against the
    /// unreadable list's text, and gives the report and whether the run passed.
    /// </summary>
    internal static (string Report, bool Passed) RunOnStack(string test262, string unreadableList, IUnicodeTables tables)
    {
        (string, bool) result = ("", false);
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = new LibraryRun(tables).Run(test262, unreadableList);
            }
            catch (Exception caught)
            {
                failure = caught;
            }
        }, StackBytes);
        thread.Start();
        thread.Join();
        if (failure is not null)
            throw new InvalidOperationException("the test262 library run failed: " + failure.Message, failure);
        return result;
    }

    /// <summary>The unreadable list's entries: each line <c>path</c>, a tab and why; <c>#</c> lines are comments.</summary>
    internal static Dictionary<string, string> ReadUnreadableList(string text)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#')
                continue;
            var tab = line.IndexOf('\t', StringComparison.Ordinal);
            if (tab <= 0 || tab == line.Length - 1)
                throw new FormatException("an entry of the unreadable list is not a path, a tab and why: " + line);
            if (!entries.TryAdd(line[..tab], line[(tab + 1)..]))
                throw new FormatException("the unreadable list names a file twice: " + line[..tab]);
        }

        return entries;
    }

    /// <summary>Every file of the populations under <paramref name="test262"/>, by path relative to it with <c>/</c>, in ordinal order.</summary>
    internal static List<(Population Population, string Path)> Files(string test262)
    {
        var files = new List<(Population, string)>();
        foreach (var (population, directory) in Directories())
        {
            var root = Path.Combine(test262, directory);
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException("test262 has no directory " + directory);
            foreach (var file in Directory.EnumerateFiles(root, "*.js", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(test262, file).Replace('\\', '/');
                if (!relative.Contains("_FIXTURE", StringComparison.Ordinal))
                    files.Add((population, relative));
            }
        }

        files.Sort(static (a, b) => string.CompareOrdinal(a.Item2, b.Item2));
        return files;
    }

    /// <summary>Runs every file and writes the report.</summary>
    internal (string Report, bool Passed) Run(string test262, string unreadableList)
    {
        var listed = ReadUnreadableList(unreadableList);
        var files = Files(test262);
        var report = new StringBuilder();
        report.Append("# Broiler.Regex test262 library runner, format 1 (bundle sjl-11-001, check B-2)\n");
        report.Append("# unreadable list SHA-256 ").Append(Sha256(unreadableList)).Append('\n');
        report.Append("# policy: nesting 512, no compile budget, no meter; test from index 0, no step ceiling, frames ")
            .Append(Invariant(RegexMatchCeilings.FrameCeilingMaximum)).Append(", trail ")
            .Append(Invariant(RegexMatchCeilings.TrailCeilingMaximum)).Append('\n');
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var failed = 0;
        foreach (var (population, path) in files)
        {
            seen.Add(path);
            string source;
            try
            {
                source = File.ReadAllText(Path.Combine(test262, path), new UTF8Encoding(false, true));
            }
            catch (DecoderFallbackException)
            {
                source = "";
            }

            var (verdict, detail) = Judge(population, source);
            var name = Label(population);
            if (listed.TryGetValue(path, out var why))
            {
                if (verdict != "pass")
                {
                    report.Append("unreadable ").Append(name).Append(' ').Append(path).Append(": ").Append(verdict)
                        .Append(", ").Append(detail).Append(" | listed: ").Append(why).Append('\n');
                    Count(counts, name + " unreadable");
                    continue;
                }

                verdict = "FAIL";
                detail = "listed as unreadable, but it passes: " + detail;
            }
            else if (verdict == "unreadable")
            {
                verdict = "FAIL";
                detail = "unreadable and not listed: " + detail;
            }

            if (verdict == "FAIL")
                failed++;
            Count(counts, name + " " + verdict);
            report.Append(verdict).Append(' ').Append(name).Append(' ').Append(path).Append(": ").Append(detail).Append('\n');
        }

        foreach (var path in listed.Keys)
        {
            if (!seen.Contains(path))
            {
                failed++;
                report.Append("FAIL listed ").Append(path).Append(": the unreadable list names a file the populations do not hold\n");
            }
        }

        report.Append("# files: ").Append(Invariant(files.Count)).Append('\n');
        var keys = new List<string>(counts.Keys);
        keys.Sort(StringComparer.Ordinal);
        foreach (var key in keys)
            report.Append("# ").Append(key).Append(": ").Append(Invariant(counts[key])).Append('\n');
        report.Append("# failed: ").Append(Invariant(failed)).Append('\n');
        return (report.ToString(), failed == 0);
    }

    /// <summary>One file's verdict, <c>pass</c>, <c>FAIL</c> or <c>unreadable</c>, and what decided it.</summary>
    internal (string Verdict, string Detail) Judge(Population population, string source)
    {
        if (source.Length == 0)
            return ("unreadable", "not UTF-8, or empty");
        var negative = IsNegativeParse(source, out var code);
        if (population == Population.Literals)
        {
            var literals = new List<Literal>();
            foreach (var token in JavaScriptTokens.Scan(code))
            {
                if (token.Kind == TokenKind.RegularExpression)
                    literals.Add(new Literal(token.Text, token.Flags));
            }

            if (literals.Count == 0)
                return ("unreadable", "the declared grammar extracts no regular-expression literal");
            return JudgeLiterals(negative, literals);
        }

        GeneratedFile file;
        try
        {
            file = GeneratedReader.Read(code);
        }
        catch (FormatException unreadable)
        {
            return ("unreadable", "the structured reader: " + unreadable.Message);
        }

        if (file.Literals.Count > 0)
        {
            return negative
                ? JudgeLiterals(true, file.Literals)
                : ("unreadable", "$DONOTEVALUATE() in a file whose front matter is not negative at parse");
        }

        if (negative)
            return ("unreadable", "a negative file with helper calls");
        var failures = new List<string>();
        var assertions = 0;
        for (var k = 0; k < file.Calls.Count; k++)
        {
            var failure = RunCall(file.Calls[k], ref assertions);
            if (failure is not null)
                failures.Add("call " + Invariant(k) + " " + file.Calls[k].Expression + ": " + failure);
        }

        var summary = Invariant(file.Calls.Count) + " calls, " + Invariant(assertions) + " assertions";
        return failures.Count == 0 ? ("pass", summary) : ("FAIL", summary + "; " + string.Join("; ", failures));
    }

    private (string, string) JudgeLiterals(bool negative, IReadOnlyList<Literal> literals)
    {
        var refused = 0;
        var problems = new List<string>();
        for (var k = 0; k < literals.Count; k++)
        {
            var (validated, compiled) = Read(literals[k]);
            if (validated != compiled)
                problems.Add("literal " + Invariant(k) + " " + Render(literals[k]) + ": validate " + validated + ", compile " + compiled);
            else if (validated == "refused")
                refused++;
            else if (validated != "accepted")
                problems.Add("literal " + Invariant(k) + " " + Render(literals[k]) + ": " + validated);
        }

        var summary = (negative ? "negative, " : "positive, ") + Invariant(literals.Count) + " literals, " + Invariant(refused) + " refused";
        if (problems.Count > 0)
            return ("FAIL", summary + "; " + string.Join("; ", problems));
        if (negative)
            return refused > 0 ? ("pass", summary) : ("FAIL", summary + "; no literal refused");
        if (refused == 0)
            return ("pass", summary);
        var refusedLiterals = new List<string>();
        for (var k = 0; k < literals.Count; k++)
        {
            if (Read(literals[k]).Validated == "refused")
                refusedLiterals.Add(Render(literals[k]));
        }

        return ("FAIL", summary + "; refused " + string.Join(", ", refusedLiterals));
    }

    /// <summary>A literal read by the validate entry and by flag parsing and compile: <c>accepted</c>, <c>refused</c> or another status.</summary>
    internal (string Validated, string Compiled) Read(Literal literal)
    {
        var validated = BroilerRegex.Validate(literal.Body, literal.Flags, _tables, Nesting, out _, out var validateLimit) switch
        {
            RegexStatus.Success => "accepted",
            RegexStatus.SyntaxError => "refused",
            RegexStatus.LimitExceeded => "limit:" + validateLimit,
            var other => "status:" + other,
        };
        string compiled;
        if (RegexFlagsText.Parse(literal.Flags, out var flags, out _) != RegexStatus.Success)
        {
            compiled = "refused";
        }
        else
        {
            compiled = BroilerRegex.Compile(literal.Body, flags, _tables, Nesting, null, null, out _, out var limit, out _) switch
            {
                RegexStatus.Success => "accepted",
                RegexStatus.SyntaxError => "refused",
                RegexStatus.LimitExceeded => "limit:" + limit,
                var other => "status:" + other,
            };
        }

        return (validated, compiled);
    }

    private string? RunCall(HelperCall call, ref int assertions)
    {
        var regex = Compile(call.RegExp, out var refusal);
        if (regex is null)
            return refusal;
        switch (call)
        {
            case PropertyEscapesCall escapes:
            {
                assertions++;
                var whole = Test(regex, escapes.Subject);
                if (whole == "true")
                    return null;
                var failing = 0;
                string? first = null;
                foreach (var symbol in Symbols(escapes.Subject))
                {
                    assertions++;
                    var answer = Test(regex, symbol);
                    if (answer != "true")
                    {
                        failing++;
                        first ??= CodePoints(symbol) + " " + answer;
                    }
                }

                return failing == 0 ? null : Invariant(failing) + " symbols do not match, the first " + first;
            }

            case PropertyOfStringsCall strings:
            {
                var problems = new List<string>();
                assertions++;
                if (Test(regex, string.Concat(strings.MatchStrings)) != "true")
                {
                    foreach (var s in strings.MatchStrings)
                    {
                        assertions++;
                        var answer = Test(regex, s);
                        if (answer != "true")
                            problems.Add("should match " + CodePoints(s) + " (" + answer + ")");
                    }
                }

                if (strings.NonMatchStrings is { } nonMatches)
                {
                    assertions++;
                    var joined = Test(regex, string.Concat(nonMatches));
                    if (joined != "false")
                    {
                        foreach (var s in nonMatches)
                        {
                            assertions++;
                            var answer = Test(regex, s);
                            if (answer != "false")
                                problems.Add("should not match " + CodePoints(s) + " (" + answer + ")");
                        }
                    }
                }

                return problems.Count == 0 ? null : string.Join(", ", problems);
            }

            case NoMatchAssertion assertion:
            {
                assertions++;
                var answer = Test(regex, assertion.Subject);
                return answer == "false" ? null : "should not match " + CodePoints(assertion.Subject) + " (" + answer + ")";
            }

            default:
                return "a call the runner does not run";
        }
    }

    private BroilerRegex? Compile(Literal literal, out string refusal)
    {
        refusal = "";
        if (RegexFlagsText.Parse(literal.Flags, out var flags, out _) != RegexStatus.Success)
        {
            refusal = "the flags of " + Render(literal) + " are refused";
            return null;
        }

        if ((flags & (RegexFlags.Global | RegexFlags.Sticky)) != 0)
        {
            refusal = Render(literal) + " has g or y, whose lastIndex the runner does not keep";
            return null;
        }

        var status = BroilerRegex.Compile(literal.Body, flags, _tables, Nesting, null, null, out var regex, out var limit, out _);
        if (status == RegexStatus.Success && regex is not null)
            return regex;
        refusal = Render(literal) + " is refused: " + status + (status == RegexStatus.LimitExceeded ? " " + limit : "");
        return null;
    }

    /// <summary><c>regExp.test(subject)</c> from lastIndex 0: <c>true</c>, <c>false</c>, or <c>limit:</c> and the ceiling met.</summary>
    private string Test(BroilerRegex regex, string subject)
    {
        var slots = new int[regex.SlotCount];
        var status = regex.Match(subject.AsSpan(), 0, false, slots, null, _ceilings, out var limit);
        return status switch
        {
            RegexStatus.Match => "true",
            RegexStatus.NoMatch => "false",
            RegexStatus.LimitExceeded => "limit:" + limit,
            var other => "status:" + other,
        };
    }

    /// <summary>The symbols of a string as <c>for (let symbol of string)</c> reads them: a pair together, a lone surrogate alone.</summary>
    internal static IEnumerable<string> Symbols(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                yield return text.Substring(i, 2);
                i++;
            }
            else
            {
                yield return text.Substring(i, 1);
            }
        }
    }

    /// <summary>Whether the front matter says <c>negative:</c>, <c>phase: parse</c>, <c>type: SyntaxError</c>; and the code after it.</summary>
    internal static bool IsNegativeParse(string source, out string code)
    {
        var start = source.IndexOf("/*---", StringComparison.Ordinal);
        var end = start < 0 ? -1 : source.IndexOf("---*/", start, StringComparison.Ordinal);
        if (end < 0)
        {
            code = source;
            return false;
        }

        code = new string(' ', end + 5) + source[(end + 5)..];
        var phase = false;
        var type = false;
        var inNegative = false;
        foreach (var raw in source[start..end].Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith("negative:", StringComparison.Ordinal))
            {
                inNegative = true;
                continue;
            }

            if (inNegative && line.StartsWith("  ", StringComparison.Ordinal))
            {
                var item = line.Trim();
                phase |= item == "phase: parse";
                type |= item == "type: SyntaxError";
            }
            else
            {
                inNegative = false;
            }
        }

        return phase && type;
    }

    private static string Label(Population population) => population switch
    {
        Population.Literals => "literals",
        Population.PropertyEscapes => "property-escapes",
        _ => "unicode-sets",
    };

    private static string Render(Literal literal) => "/" + Escape(literal.Body) + "/" + Escape(literal.Flags);

    private static string Escape(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c is >= ' ' and <= '~' && c != '\\')
                builder.Append(c);
            else
                builder.Append("\\u").Append(((int)c).ToString("X4", NumberFormatInfo.InvariantInfo));
        }

        return builder.ToString();
    }

    private static string CodePoints(string text)
    {
        var parts = new List<string>();
        for (var i = 0; i < text.Length; i++)
        {
            int codePoint = text[i];
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                i++;
            }

            parts.Add("U+" + codePoint.ToString("X6", NumberFormatInfo.InvariantInfo));
        }

        return parts.Count == 0 ? "the empty string" : string.Join(' ', parts);
    }

    private static void Count(Dictionary<string, int> counts, string key) =>
        counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;

    private static string Invariant(int value) => value.ToString(NumberFormatInfo.InvariantInfo);

    private static string Sha256(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
