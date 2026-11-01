using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Broiler.Regex.Probe;

/// <summary>
/// One row of the adversarial corpus (<c>tests/Adversarial/corpus.tsv</c>), its fields in the order of
/// <see cref="AdversarialCorpus.Columns"/>.
/// </summary>
internal sealed class AdversarialRow(string[] fields)
{
    /// <summary>Every field of the row, in the corpus's column order.</summary>
    internal string[] Fields { get; } = fields;

    internal string Id => Fields[0];

    internal string Class => Fields[1];

    /// <summary><c>compile</c>, <c>validate</c>, <c>match@k</c> or <c>anchored@k</c>.</summary>
    internal string Op => Fields[2];

    /// <summary>The pattern, an expression (<see cref="EngineRun.Expression"/>).</summary>
    internal string Pattern => Fields[3];

    internal string Flags => Fields[4];

    /// <summary>The subject, an expression, or <c>-</c>.</summary>
    internal string Subject => Fields[5];

    /// <summary>The policy's six keys (<see cref="EngineRun.ParsePolicy"/>).</summary>
    internal string Policy => Fields[6];

    /// <summary>The declared status, in the corpus's vocabulary.</summary>
    internal string Status => Fields[7];
}

/// <summary>
/// The adversarial corpus as its README's section 1 defines it: the header lines the probe reads, the column line, and
/// one row per case. Shared by the probe's child mode <c>--engine-row</c> (<see cref="EngineRun.Row"/>), its
/// <c>--adversarial</c> mode (<c>AdversarialMode</c>) and the test project's <c>AdversarialCorpusTests</c>, which
/// links this file, so that the corpus has one reader.
/// </summary>
/// <remarks>
/// The reader refuses, with a <see cref="FormatException"/> naming the line, a corpus whose format is not 1, which
/// declares no stack, whose column line is not the README's, or whose row has another number of fields, an identifier
/// that is not its class's letter and two digits, or an identifier another row has. It does not read the expressions,
/// the policies or the statuses: the run reads each where it uses it.
/// </remarks>
internal sealed class AdversarialCorpus
{
    /// <summary>The columns of the corpus, in order (its README, section 1).</summary>
    internal static readonly string[] Columns =
    [
        "id", "class", "op", "pattern", "flags", "subject", "policy", "status",
        "vm_fuel", "vm_subject", "vm_outcome", "js_subject", "js_outcome",
    ];

    private const string Classes = "KALGWNFMVO";

    private const string FormatHeader = "# corpus-format: ";

    private const string StackHeader = "# stack-bytes: ";

    private AdversarialCorpus(int stackBytes, AdversarialRow[] rows)
    {
        StackBytes = stackBytes;
        Rows = rows;
    }

    /// <summary>The smallest stack a child process runs a row on: the header's <c>stack-bytes</c>.</summary>
    internal int StackBytes { get; }

    /// <summary>The rows, in the order the file holds them.</summary>
    internal IReadOnlyList<AdversarialRow> Rows { get; }

    /// <summary>Reads the corpus file at <paramref name="path"/>.</summary>
    internal static AdversarialCorpus Read(string path) => Read(path, out _);

    /// <summary>Reads the corpus file at <paramref name="path"/>, whose bytes, as read, are <paramref name="bytes"/>.</summary>
    /// <exception cref="FormatException">The file is not UTF-8, or not the README's format.</exception>
    internal static AdversarialCorpus Read(string path, out byte[] bytes)
    {
        bytes = File.ReadAllBytes(path);
        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw new FormatException("The adversarial corpus is not UTF-8.");
        }

        return Parse(text);
    }

    /// <summary>Reads a corpus from its text.</summary>
    internal static AdversarialCorpus Parse(string text)
    {
        int? format = null;
        var stackBytes = 0;
        var columns = false;
        var rows = new List<AdversarialRow>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var number = 0;
        foreach (var raw in text.Split('\n'))
        {
            number++;
            var line = raw.EndsWith('\r') ? raw[..^1] : raw;
            if (line.Length == 0)
                continue;
            if (line.StartsWith('#'))
            {
                if (columns)
                    throw Refused(number, "a header line after the column line");
                if (line.StartsWith(FormatHeader, StringComparison.Ordinal))
                    format = Whole(line[FormatHeader.Length..], number);
                else if (line.StartsWith(StackHeader, StringComparison.Ordinal))
                    stackBytes = Whole(line[StackHeader.Length..], number);
                continue;
            }

            var fields = line.Split('\t');
            if (!columns)
            {
                if (!fields.SequenceEqual(Columns, StringComparer.Ordinal))
                    throw Refused(number, "the column line is not the corpus README's");
                columns = true;
                continue;
            }

            if (fields.Length != Columns.Length)
                throw Refused(number, "a row of " + Decimal(fields.Length) + " fields");
            var id = fields[0];
            if (id.Length != 3 || !Classes.Contains(id[0], StringComparison.Ordinal) || id[1] is < '0' or > '9' || id[2] is < '0' or > '9'
                || fields[1] != id[..1])
                throw Refused(number, "the identifier " + id + " is not its class's letter and two digits");
            if (!ids.Add(id))
                throw Refused(number, "a second row " + id);
            rows.Add(new AdversarialRow(fields));
        }

        if (format != 1)
            throw Refused(0, "no '" + FormatHeader + "1' header");
        if (stackBytes <= 0)
            throw Refused(0, "no '" + StackHeader + "' header");
        if (!columns)
            throw Refused(0, "no column line");
        return new AdversarialCorpus(stackBytes, [.. rows]);
    }

    /// <summary>The row <paramref name="id"/>, or null.</summary>
    internal AdversarialRow? Find(string id) => Rows.FirstOrDefault(row => row.Id == id);

    /// <summary>A status with every <c>x^k</c> written out as <c>k</c> copies of <c>x</c> (the README's shorthand).</summary>
    internal static string Expand(string status) =>
        string.Join(' ', status.Split(' ').SelectMany(static token =>
        {
            var caret = token.LastIndexOf('^');
            return caret > 0 && int.TryParse(token.AsSpan(caret + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var count)
                ? Enumerable.Repeat(token[..caret], count)
                : [token];
        }));

    private static int Whole(string text, int line) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : throw Refused(line, "'" + text + "' is not a whole number");

    private static FormatException Refused(int line, string why) =>
        new("The adversarial corpus " + (line > 0 ? "at line " + Decimal(line) + " has " : "has ") + why + ".");

    private static string Decimal(int value) => value.ToString(NumberFormatInfo.InvariantInfo);
}
