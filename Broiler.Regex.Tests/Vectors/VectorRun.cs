using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Broiler.Unicode.Algorithms;

namespace Broiler.Regex.VectorsRunner;

/// <summary>One vector's result: what the vector expects, what the library answered, and the verdict.</summary>
internal sealed record VectorResult(VectorRecord Vector, string Rendered, string Verdict)
{
    /// <summary>The verdict of a vector whose class holds the library to its <c>out</c> (normative and divergence) and whose answer is one of its alternatives.</summary>
    internal const string Match = "match";

    /// <summary>The verdict of such a vector whose answer is none of them.</summary>
    internal const string Difference = "difference";

    /// <summary>The verdict of a vector whose op is the object model's.</summary>
    internal const string NotImplemented = "not-implemented";

    /// <summary>The verdict of an impl-defined vector, which the rule has reported with the library's answer, followed by what that answer meets.</summary>
    internal const string Reported = "reported";

    /// <summary>Whether the vector's <c>out</c> is a status: a library-level vector, which engine-level runners skip.</summary>
    internal bool IsStatusVector => Vector.Out.StartsWith("status: ", StringComparison.Ordinal);
}

/// <summary>
/// The library-level runner of bundle <c>sjl-11-001</c>'s part D: every vector of the pinned copy, its op called through
/// <see cref="VectorLibrary"/>, judged by its class, and the report, deterministic (ordered by id, no time, no path).
/// </summary>
/// <remarks>
/// <para>
/// <b>The verdicts.</b> A <c>normative</c> or <c>divergence</c> vector holds the library to its <c>out</c>, which the
/// specification determines (SCHEMA.md section 5: a divergence field classifies an engine's difference and names no
/// library): <c>match</c> when the answer is one of <c>out</c>'s alternatives, else <c>difference</c>. A status vector,
/// one whose <c>out</c> is a status, is judged so too. An <c>impl-defined</c> vector is <c>reported</c> with the
/// library's answer and what it meets: <c>out</c>, either engine's <c>per-engine</c> expectation, or none; where the
/// library answers the nesting status and an engine's expectation is decision D7's SyntaxError, which that engine renders
/// from the same status, the engine is named with <c>(D7)</c>. A vector whose op is the object model's is
/// <c>not-implemented</c>, and its op is listed by name with the reason (<see cref="VectorLibrary.NotImplementedOps"/>).
/// </para>
/// <para>
/// <b>The stack.</b> The run is made on a thread of <see cref="StackBytes"/>, the smallest stack the adversarial corpus
/// declares, as the probe's populations are, so that the nesting vectors at the limit run where both engines compile.
/// </para>
/// </remarks>
internal static class VectorRun
{
    /// <summary>The stack the run is made on: <c>tests/Adversarial/corpus.tsv</c>'s <c>stack-bytes</c>, 1 MiB.</summary>
    internal const int StackBytes = 1 << 20;

    /// <summary>Runs every vector of <paramref name="corpus"/> on the calling thread, in the corpus's order.</summary>
    internal static List<VectorResult> Run(VectorCorpus corpus, IUnicodeTables tables)
    {
        var library = new VectorLibrary(tables);
        var results = new List<VectorResult>(corpus.Records.Count);
        foreach (var vector in corpus.Records)
            results.Add(Judge(vector, library.Call(vector)));
        return results;
    }

    /// <summary>Runs <see cref="Run"/> and writes <see cref="Report"/> on a thread of <see cref="StackBytes"/>.</summary>
    internal static string RunOnSmallestStack(VectorCorpus corpus, IUnicodeTables tables)
    {
        var report = "";
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                report = Report(corpus, Run(corpus, tables));
            }
            catch (Exception caught)
            {
                failure = caught;
            }
        }, StackBytes);
        thread.Start();
        thread.Join();
        if (failure is not null)
            throw new InvalidOperationException("the vectors' run failed: " + failure.Message, failure);
        return report;
    }

    /// <summary>The verdict of one answer, by the vector's class.</summary>
    internal static VectorResult Judge(VectorRecord vector, string rendered)
    {
        if (rendered == VectorLibrary.NotImplemented)
            return new VectorResult(vector, rendered, VectorResult.NotImplemented);
        if (vector.Class == "impl-defined")
            return new VectorResult(vector, rendered, VectorResult.Reported + "; meets " + Meets(vector, rendered));
        return new VectorResult(vector, rendered, VectorNotation.Alternatives(vector.Out).Contains(rendered) ? VectorResult.Match : VectorResult.Difference);
    }

    /// <summary>What an impl-defined vector's answer meets: <c>out</c>, each engine's expectation, or none.</summary>
    private static string Meets(VectorRecord vector, string rendered)
    {
        var met = new List<string>();
        if (VectorNotation.Alternatives(vector.Out).Contains(rendered))
            met.Add("out");
        foreach (var engine in (ReadOnlySpan<string>)["broiler-js", "broiler-vm"])
        {
            if (vector["per-engine." + engine] is not { } expected)
                continue;
            if (VectorNotation.Alternatives(expected).Contains(rendered))
                met.Add("per-engine." + engine);
            else if (rendered == "status: limit-nesting" && expected == "error: SyntaxError")
                met.Add("per-engine." + engine + " (D7)");
        }

        return met.Count == 0 ? "none" : string.Join(", ", met);
    }

    /// <summary>
    /// The report: the pin and its check, the counts by class and verdict, every op not implemented with its count and
    /// reason, and one tab-separated line per vector, ordered by id (ordinal); LF line endings, no time, no path.
    /// </summary>
    internal static string Report(VectorCorpus corpus, IReadOnlyList<VectorResult> results)
    {
        var sorted = new List<VectorResult>(results);
        sorted.Sort(static (x, y) => string.CompareOrdinal(x.Vector.Id, y.Vector.Id));
        var text = new StringBuilder();
        var pin = corpus.Pin;
        text.Append("# Broiler.Regex: the vectors at library level (bundle sjl-11-001, part D)\n");
        text.Append("# pin: Broiler.JS ").Append(pin.Commit).Append(", corpus revision ").Append(VectorFile.Decimal(pin.Revision))
            .Append(", ").Append(pin.Source).Append(", ").Append(VectorFile.Decimal(pin.Files.Count)).Append(" files, regexp.pin SHA-256 ")
            .Append(pin.Sha256).Append('\n');
        text.Append("# pin check, before the run: every file's length and SHA-256 as the pin gives them, and no file it does not name\n");
        text.Append("# vectors: ").Append(VectorFile.Decimal(sorted.Count)).Append('\n');

        foreach (var cls in (ReadOnlySpan<string>)["normative", "divergence", "impl-defined"])
        {
            int all = 0, match = 0, difference = 0, notImplemented = 0, reported = 0;
            foreach (var result in sorted)
            {
                if (result.Vector.Class != cls || result.IsStatusVector)
                    continue;
                all++;
                switch (result.Verdict)
                {
                    case VectorResult.Match: match++; break;
                    case VectorResult.Difference: difference++; break;
                    case VectorResult.NotImplemented: notImplemented++; break;
                    default: reported++; break;
                }
            }

            text.Append("# ").Append(cls).Append(": ").Append(VectorFile.Decimal(all));
            if (cls == "impl-defined")
                text.Append(", reported ").Append(VectorFile.Decimal(reported));
            else
                text.Append(", match ").Append(VectorFile.Decimal(match)).Append(", difference ").Append(VectorFile.Decimal(difference));
            text.Append(", not implemented ").Append(VectorFile.Decimal(notImplemented)).Append('\n');
        }

        int statusAll = 0, statusMatch = 0;
        foreach (var result in sorted)
        {
            if (!result.IsStatusVector)
                continue;
            statusAll++;
            if (result.Verdict == VectorResult.Match)
                statusMatch++;
        }

        text.Append("# status vectors: ").Append(VectorFile.Decimal(statusAll)).Append(", match ").Append(VectorFile.Decimal(statusMatch))
            .Append(", difference ").Append(VectorFile.Decimal(statusAll - statusMatch)).Append('\n');

        var meets = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var result in sorted)
        {
            if (result.Verdict.StartsWith(VectorResult.Reported, StringComparison.Ordinal))
                meets[result.Verdict] = meets.GetValueOrDefault(result.Verdict) + 1;
        }

        foreach (var (verdict, count) in meets)
            text.Append("# impl-defined ").Append(verdict).Append(": ").Append(VectorFile.Decimal(count)).Append('\n');

        var ops = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var result in sorted)
        {
            if (result.Verdict == VectorResult.NotImplemented)
                ops[result.Vector.Op] = ops.GetValueOrDefault(result.Vector.Op) + 1;
        }

        foreach (var (op, count) in ops)
            text.Append("# not implemented (the object model's): ").Append(op).Append(": ").Append(VectorFile.Decimal(count))
                .Append(" vectors; ").Append(VectorLibrary.NotImplementedOps[op]).Append('\n');

        var differences = 0;
        foreach (var result in sorted)
        {
            if (result.Verdict == VectorResult.Difference)
                differences++;
        }

        text.Append("# differences: ").Append(VectorFile.Decimal(differences)).Append('\n');
        text.Append("id\tclass\top\texpected\trendered\tverdict\n");
        foreach (var result in sorted)
        {
            text.Append(result.Vector.Id).Append('\t').Append(result.Vector.Class).Append('\t').Append(result.Vector.Op).Append('\t')
                .Append(result.Vector.Out).Append('\t').Append(result.Rendered).Append('\t').Append(result.Verdict).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>The ids of every vector whose verdict is a difference, in id order.</summary>
    internal static List<string> Differences(IReadOnlyList<VectorResult> results)
    {
        var ids = new List<string>();
        foreach (var result in results)
        {
            if (result.Verdict == VectorResult.Difference)
                ids.Add(result.Vector.Id);
        }

        ids.Sort(StringComparer.Ordinal);
        return ids;
    }
}
