using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Broiler.Regex.VectorsRunner;

/// <summary>
/// One vector of the shared conformance corpus (Broiler.JS's <c>conformance/vectors/SCHEMA.md</c>, schema 1): its
/// fields as the file gives them, and the file it is in.
/// </summary>
internal sealed class VectorRecord
{
    private readonly Dictionary<string, string> _fields;

    internal VectorRecord(string file, Dictionary<string, string> fields)
    {
        File = file;
        _fields = fields;
    }

    /// <summary>The name of the file the vector is in.</summary>
    internal string File { get; }

    /// <summary>A field's value, or null when the record has no such field.</summary>
    internal string? this[string key] => _fields.TryGetValue(key, out var value) ? value : null;

    internal string Id => _fields["id"];

    internal string Op => _fields["op"];

    internal string In => _fields["in"];

    internal string Out => _fields["out"];

    internal string Class => _fields["class"];
}

/// <summary>
/// The reader of a vector file, as SCHEMA.md section 1 states the format: UTF-8 with LF line endings; a line whose
/// first character is <c>#</c> is a comment; the first other line reads exactly <c>schema: 1</c>; records follow,
/// separated by blank lines, each line <c>key: value</c> split at the first <c>": "</c>; a key at most once in a record
/// and none the schema does not name. A file that breaks any of these is refused with a <see cref="FormatException"/>.
/// </summary>
internal static class VectorFile
{
    /// <summary>The keys of section 2, but the per-engine and divergence fields, which <see cref="IsKnownKey"/> reads by prefix.</summary>
    private static readonly HashSet<string> Keys = new(StringComparer.Ordinal)
    {
        "id", "area", "op", "in", "out", "class", "prov", "script", "note", "since",
    };

    private static readonly string[] Required = ["id", "area", "op", "in", "out", "class", "prov", "script", "since"];

    private static bool IsKnownKey(string key) =>
        Keys.Contains(key)
        || key is "per-engine.broiler-js" or "per-engine.broiler-vm" or "divergence.broiler-js" or "divergence.broiler-vm";

    /// <summary>Reads the records of <paramref name="text"/>, the file named <paramref name="file"/>, in file order.</summary>
    internal static List<VectorRecord> Read(string file, string text)
    {
        if (text.Contains('\r', StringComparison.Ordinal))
            throw new FormatException(file + ": a CR; the schema has LF line endings");
        var lines = text.Split('\n');
        var records = new List<VectorRecord>();
        var sawSchema = false;
        Dictionary<string, string>? current = null;
        for (var number = 0; number < lines.Length; number++)
        {
            var line = lines[number];
            if (line.StartsWith('#'))
                continue;
            if (!sawSchema)
            {
                if (line.Length == 0)
                    continue;
                if (line != "schema: 1")
                    throw new FormatException(file + ":" + Decimal(number + 1) + ": the first line that is not a comment is not \"schema: 1\"");
                sawSchema = true;
                continue;
            }

            if (line.Length == 0)
            {
                if (current is not null)
                    records.Add(Finish(file, number, current));
                current = null;
                continue;
            }

            var colon = line.IndexOf(": ", StringComparison.Ordinal);
            if (colon <= 0)
                throw new FormatException(file + ":" + Decimal(number + 1) + ": not a key: value line");
            var key = line[..colon];
            if (!IsKnownKey(key))
                throw new FormatException(file + ":" + Decimal(number + 1) + ": the key " + key + " is not the schema's");
            current ??= new Dictionary<string, string>(StringComparer.Ordinal);
            if (!current.TryAdd(key, line[(colon + 2)..]))
                throw new FormatException(file + ":" + Decimal(number + 1) + ": the key " + key + " twice in one record");
        }

        if (!sawSchema)
            throw new FormatException(file + ": no \"schema: 1\" line");
        if (current is not null)
            records.Add(Finish(file, lines.Length, current));
        return records;
    }

    private static VectorRecord Finish(string file, int number, Dictionary<string, string> fields)
    {
        foreach (var key in Required)
        {
            if (!fields.ContainsKey(key))
                throw new FormatException(file + ":" + Decimal(number) + ": a record without " + key);
        }

        return new VectorRecord(file, fields);
    }

    internal static string Decimal(long value) => value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// The pin file of the vectors' copy (<c>tests/Vectors/regexp.pin</c>; bundle <c>sjl-11-001</c>'s part D: "a pin file
/// naming Broiler.JS's commit and each file's SHA-256, checked before the run"): Broiler.JS's commit, the corpus
/// revision, the directory of Broiler.JS the files are copied from, and each file's name, length, SHA-256 and git blob.
/// </summary>
internal sealed class VectorPin
{
    private VectorPin(string commit, int revision, string source, List<(string Name, long Length, string Sha256, string Blob)> files, string text)
    {
        Commit = commit;
        Revision = revision;
        Source = source;
        Files = files;
        Sha256 = Hex(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    /// <summary>Broiler.JS's commit, forty lower-case hexadecimal digits.</summary>
    internal string Commit { get; }

    /// <summary>The corpus revision at that commit.</summary>
    internal int Revision { get; }

    /// <summary>The directory of Broiler.JS the files are copied from.</summary>
    internal string Source { get; }

    /// <summary>Each file the pin names, in the pin's order.</summary>
    internal IReadOnlyList<(string Name, long Length, string Sha256, string Blob)> Files { get; }

    /// <summary>The pin file's own SHA-256, which the report prints.</summary>
    internal string Sha256 { get; }

    /// <summary>Reads the pin file's text; a line the format does not have is refused with a <see cref="FormatException"/>.</summary>
    internal static VectorPin Parse(string text)
    {
        string? commit = null, source = null;
        int? revision = null;
        var files = new List<(string, long, string, string)>();
        foreach (var line in text.Split('\n'))
        {
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            if (line.StartsWith("broiler-js-commit: ", StringComparison.Ordinal))
                commit = line["broiler-js-commit: ".Length..];
            else if (line.StartsWith("corpus-revision: ", StringComparison.Ordinal))
                revision = int.Parse(line["corpus-revision: ".Length..], NumberStyles.None, CultureInfo.InvariantCulture);
            else if (line.StartsWith("source: ", StringComparison.Ordinal))
                source = line["source: ".Length..];
            else if (line.StartsWith("file: ", StringComparison.Ordinal))
            {
                var parts = line["file: ".Length..].Split(' ');
                if (parts.Length != 4 || !IsHex(parts[2], 64) || !IsHex(parts[3], 40) || !parts[0].EndsWith(".vectors", StringComparison.Ordinal))
                    throw new FormatException("the pin's line is not \"file: name length sha256 blob\": " + line);
                files.Add((parts[0], long.Parse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture), parts[2], parts[3]));
            }
            else
            {
                throw new FormatException("the pin has a line it does not know: " + line);
            }
        }

        if (commit is null || !IsHex(commit, 40) || revision is null || source is null || files.Count == 0)
            throw new FormatException("the pin names no commit of forty hexadecimal digits, no revision, no source or no file");
        return new VectorPin(commit, revision.Value, source, files, text);
    }

    /// <summary>
    /// The check that comes before every run: every file the pin names is in <paramref name="directory"/> with the length
    /// and SHA-256 the pin gives, and the directory holds no <c>.vectors</c> file the pin does not name. The answer is
    /// each problem found, sorted; none means the copy is the pinned one.
    /// </summary>
    internal List<string> Check(string directory)
    {
        var problems = new List<string>();
        var named = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (name, length, sha256, _) in Files)
        {
            named.Add(name);
            var path = Path.Combine(directory, name);
            if (!System.IO.File.Exists(path))
            {
                problems.Add(name + ": missing");
                continue;
            }

            var bytes = System.IO.File.ReadAllBytes(path);
            if (bytes.LongLength != length)
                problems.Add(name + ": " + VectorFile.Decimal(bytes.LongLength) + " bytes, where the pin gives " + VectorFile.Decimal(length));
            var actual = Hex(SHA256.HashData(bytes));
            if (actual != sha256)
                problems.Add(name + ": SHA-256 " + actual + ", where the pin gives " + sha256);
        }

        if (Directory.Exists(directory))
        {
            foreach (var path in Directory.GetFiles(directory, "*.vectors"))
            {
                var name = Path.GetFileName(path);
                if (!named.Contains(name))
                    problems.Add(name + ": a file the pin does not name");
            }
        }
        else
        {
            problems.Add(directory + ": no such directory");
        }

        problems.Sort(StringComparer.Ordinal);
        return problems;
    }

    internal static string Hex(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(bytes);

    private static bool IsHex(string text, int length)
    {
        if (text.Length != length)
            return false;
        foreach (var c in text)
        {
            if (c is not ((>= '0' and <= '9') or (>= 'a' and <= 'f')))
                return false;
        }

        return true;
    }
}

/// <summary>
/// The pinned copy as a run reads it: the pin, checked before anything is read, and every record of every file the pin
/// names, in file order. <see cref="Load"/> refuses a copy the check finds a problem in with a
/// <see cref="VectorPinException"/>, so no run reads a vector of a copy that is not the pinned one.
/// </summary>
internal sealed class VectorCorpus
{
    private VectorCorpus(VectorPin pin, List<VectorRecord> records)
    {
        Pin = pin;
        Records = records;
    }

    internal VectorPin Pin { get; }

    internal IReadOnlyList<VectorRecord> Records { get; }

    /// <summary>
    /// Reads <paramref name="root"/>'s <c>regexp.pin</c>, checks the directory <c>regexp/</c> beside it against it, and
    /// only then reads the files.
    /// </summary>
    /// <exception cref="VectorPinException">The pin is missing or unreadable, or the check finds a problem.</exception>
    /// <exception cref="FormatException">A pinned file does not read as the schema says.</exception>
    internal static VectorCorpus Load(string root)
    {
        var pinPath = Path.Combine(root, "regexp.pin");
        if (!System.IO.File.Exists(pinPath))
            throw new VectorPinException([pinPath + ": no pin file"]);
        VectorPin pin;
        try
        {
            pin = VectorPin.Parse(System.IO.File.ReadAllText(pinPath, new UTF8Encoding(false, true)));
        }
        catch (FormatException refusal)
        {
            throw new VectorPinException([pinPath + ": " + refusal.Message]);
        }

        var directory = Path.Combine(root, "regexp");
        var problems = pin.Check(directory);
        if (problems.Count > 0)
            throw new VectorPinException(problems);

        var records = new List<VectorRecord>();
        foreach (var (name, _, _, _) in pin.Files)
            records.AddRange(VectorFile.Read(name, System.IO.File.ReadAllText(Path.Combine(directory, name), new UTF8Encoding(false, true))));
        return new VectorCorpus(pin, records);
    }
}

/// <summary>The pin check's refusal: the copy is not the pinned one, and nothing is run.</summary>
internal sealed class VectorPinException : Exception
{
    internal VectorPinException(List<string> problems)
        : base("the vectors' copy is not the pinned one: " + string.Join("; ", problems))
    {
        Problems = problems;
    }

    internal IReadOnlyList<string> Problems { get; }
}
