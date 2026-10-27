using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Broiler.Regex.Compiling;
using Broiler.Regex.Engine;
using Broiler.Unicode.Tables;

namespace Broiler.Regex.Probe;

/// <summary>
/// One case through the new backend's internal entry (task 2.4a of SJL-11.2), written in the adversarial corpus's
/// terms (<c>tests/Adversarial/README.md</c>): an operation, a pattern and a subject as expressions, flags, and a policy
/// of six keys; its answer is a library status in the corpus's vocabulary. Shared by the hidden argument
/// <c>--engine-row</c>, which runs one row of the corpus in a process of its own, and by
/// <see cref="EnginePopulation"/>, which the report carries.
/// </summary>
/// <remarks>
/// The answer beyond the corpus's statuses is <c>unsupported</c>, for an operation other than <c>compile</c>,
/// <c>match@k</c> and <c>anchored@k</c> (the validate entry is task 2.8's); since task 2.4b every pattern the parser
/// accepts compiles to a program, unless it nests past the row's limit, and since task 2.5 the compilation is charged to
/// the row's meter and bounded by its compile budget (requirement R2), answering <c>limit:compile</c> past it. The meter is
/// the same for the compilation and the match, as an engine's is (bundle <c>sjl-11-001</c>'s check A-2: "compile charges
/// the same meter"), each charging it in its own blocks. Task 2.14's <c>--adversarial</c> mode, which runs every row with
/// both controls of the rule's part C, is built on this.
/// </remarks>
internal static class EngineRun
{
    /// <summary>The probe's meter's own exception: more units were charged than the row's meter allows.</summary>
    internal sealed class MeterSpent : Exception
    {
        internal MeterSpent() : base("The meter's allowance is spent.")
        {
        }
    }

    /// <summary>A row's policy: the meter's allowance, the step, frame and trail ceilings, the nesting limit, and the compile budget.</summary>
    internal readonly record struct Policy(ulong? Meter, ulong? Steps, int Frames, int Trail, int Nesting, ulong? Compile);

    /// <summary>
    /// The answer of one case and the units its match did, or, for a <c>compile</c> case, its compilation did; zero for a
    /// case that does not match, and the meter's total when the meter's allowance ends the case.
    /// </summary>
    internal static string Answer(string op, string pattern, string flags, string subject, Policy policy, out ulong units)
    {
        units = 0;
        var charged = 0UL;
        RegexWorkCharge? meter = policy.Meter is { } allowance
            ? spent =>
            {
                charged += spent;
                if (charged > allowance)
                    throw new MeterSpent();
            }
            : null;

        ProgramCompilation compiled;
        RegexProgram? program;
        var compilation = new CompileCharge(meter, policy.Compile);
        try
        {
            compiled = ProgramCompiler.Compile(pattern, flags == "-" ? RegexFlags.None : RegexFlagsParser.Parse(flags), UnicodeTables.Instance, policy.Nesting, out program,
                charge: compilation);
        }
        catch (RegexSyntaxException)
        {
            return "syntax";
        }
        catch (MeterSpent)
        {
            units = charged;
            return "meter-exception";
        }

        if (compiled == ProgramCompilation.NestingLimitExceeded)
            return "limit:nesting";
        if (compiled == ProgramCompilation.CompileBudgetExceeded)
        {
            units = compilation.Spent;
            return "limit:compile";
        }

        if (op == "compile")
        {
            units = compilation.Spent;
            return "ok";
        }

        bool anchored;
        string index;
        if (op.StartsWith("match@", StringComparison.Ordinal))
            (anchored, index) = (false, op["match@".Length..]);
        else if (op.StartsWith("anchored@", StringComparison.Ordinal))
            (anchored, index) = (true, op["anchored@".Length..]);
        else
            return "unsupported";

        var slots = new int[program!.SlotCount];
        try
        {
            var status = program.Match(subject, int.Parse(index, NumberStyles.None, CultureInfo.InvariantCulture), anchored, slots, meter,
                new RegexMatchCeilings(policy.Steps, policy.Frames, policy.Trail), out var limit, out units);
            return status switch
            {
                RegexStatus.Match => Render(slots),
                RegexStatus.NoMatch => "nomatch",
                _ => "limit:" + limit switch
                {
                    RegexLimit.Steps => "steps",
                    RegexLimit.Frames => "frames",
                    RegexLimit.Trail => "trail",
                    RegexLimit.Compile => "compile",
                    _ => "another",
                },
            };
        }
        catch (MeterSpent)
        {
            units = charged;
            return "meter-exception";
        }
    }

    /// <summary>A match as the corpus writes it: <c>match s,e</c> and each group's <c>s,e</c> or <c>-</c>, a run of eight or more equal groups as <c>x^k</c>.</summary>
    internal static string Render(ReadOnlySpan<int> slots)
    {
        var text = new StringBuilder("match ").Append(Span(slots, 0));
        for (var group = 1; group < slots.Length / 2;)
        {
            var shown = Span(slots, group);
            var run = 1;
            while (group + run < slots.Length / 2 && Span(slots, group + run) == shown)
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

        return text.ToString();
    }

    private static string Span(ReadOnlySpan<int> slots, int group) =>
        slots[2 * group] < 0 ? "-" : Decimal(slots[2 * group]) + "," + Decimal(slots[(2 * group) + 1]);

    /// <summary>A policy of the corpus's six keys, in their order.</summary>
    internal static Policy ParsePolicy(string text)
    {
        var keys = text.Split(' ');
        if (keys.Length != 6)
            throw new FormatException("A policy has six keys: " + text);

        string Value(int at, string key) =>
            keys[at].StartsWith(key + "=", StringComparison.Ordinal) ? keys[at][(key.Length + 1)..] : throw new FormatException("Expected " + key + " in " + text);
        ulong? Optional(string value) => value == "none" ? null : ulong.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
        int Whole(string value) => int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);

        return new Policy(Optional(Value(0, "meter")), Optional(Value(1, "steps")), Whole(Value(2, "frames")), Whole(Value(3, "trail")),
            Whole(Value(4, "nesting")), Optional(Value(5, "compile")));
    }

    /// <summary>
    /// An expression of the corpus: JSON string literals, each optionally followed by <c>* n</c>, joined by <c>+</c>;
    /// <c>-</c> is the empty text of a row with no subject.
    /// </summary>
    internal static string Expression(string text)
    {
        if (text == "-")
            return "";

        var built = new StringBuilder();
        var at = 0;
        while (true)
        {
            SkipSpaces(text, ref at);
            var piece = JsonString(text, ref at);
            SkipSpaces(text, ref at);
            var count = 1;
            if (at < text.Length && text[at] == '*')
            {
                at++;
                SkipSpaces(text, ref at);
                var digits = at;
                while (at < text.Length && text[at] is >= '0' and <= '9')
                    at++;
                count = int.Parse(text.AsSpan(digits, at - digits), NumberStyles.None, CultureInfo.InvariantCulture);
                SkipSpaces(text, ref at);
            }

            built.EnsureCapacity(built.Length + (piece.Length * count));
            for (var i = 0; i < count; i++)
                built.Append(piece);

            if (at == text.Length)
                return built.ToString();
            if (text[at] != '+')
                throw new FormatException("Expected + at " + Decimal(at) + " of " + text);
            at++;
        }
    }

    private static void SkipSpaces(string text, ref int at)
    {
        while (at < text.Length && text[at] == ' ')
            at++;
    }

    private static string JsonString(string text, ref int at)
    {
        if (at >= text.Length || text[at] != '"')
            throw new FormatException("Expected a JSON string at " + Decimal(at) + " of " + text);
        at++;
        var value = new StringBuilder();
        while (text[at] != '"')
        {
            var unit = text[at++];
            if (unit != '\\')
            {
                value.Append(unit);
                continue;
            }

            var escape = text[at++];
            switch (escape)
            {
                case 'u':
                    value.Append((char)int.Parse(text.AsSpan(at, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                    at += 4;
                    break;
                case 'b': value.Append('\b'); break;
                case 'f': value.Append('\f'); break;
                case 'n': value.Append('\n'); break;
                case 'r': value.Append('\r'); break;
                case 't': value.Append('\t'); break;
                default: value.Append(escape); break;
            }
        }

        at++;
        return value.ToString();
    }

    /// <summary>
    /// The hidden argument <c>--engine-row corpus id</c>: runs the row <paramref name="id"/> of the corpus file through the
    /// new backend's entry on a thread of the corpus's declared stack and writes <c>id answer</c>. Exits 0 when the row
    /// ran, whatever it answered; 3 when the corpus has no such row. A crash ends the process, which its parent observes.
    /// </summary>
    internal static int Row(string corpusPath, string id)
    {
        var stackBytes = 0;
        string[]? row = null;
        foreach (var line in File.ReadAllLines(corpusPath))
        {
            if (line.StartsWith("# stack-bytes: ", StringComparison.Ordinal))
                stackBytes = int.Parse(line.AsSpan("# stack-bytes: ".Length), NumberStyles.None, CultureInfo.InvariantCulture);
            else if (line.StartsWith(id + "\t", StringComparison.Ordinal))
                row = line.Split('\t');
        }

        if (row is null || stackBytes == 0)
        {
            Console.Error.WriteLine("no row " + id + " or no stack-bytes header in " + corpusPath);
            return 3;
        }

        var answer = "";
        var units = 0UL;
        var thread = new Thread(() => answer = Answer(row[2], Expression(row[3]), row[4], Expression(row[5]), ParsePolicy(row[6]), out units), stackBytes);
        thread.Start();
        thread.Join();
        Console.Out.Write(id + " " + answer + "\n");
        Console.Error.Write("units " + units.ToString(CultureInfo.InvariantCulture) + "\n");
        return 0;
    }

    private static string Decimal(int value) => value.ToString(NumberFormatInfo.InvariantInfo);
}
