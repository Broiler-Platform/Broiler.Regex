using System;
using System.IO;
using Broiler.Regex.VectorsRunner;
using Broiler.Unicode.Tables;

namespace Broiler.Regex.Probe;

/// <summary>
/// The probe's <c>--vectors dir [--report path]</c> mode (task 2.12 of SJL-11.2; bundle <c>sjl-11-001</c>'s part D, "under
/// the JIT and through the Native AOT probe"): the library runner of <c>Broiler.Regex.Tests/Vectors/</c>, whose sources
/// this probe compiles in, over the pinned copy in <c>dir</c> (<c>tests/Vectors</c>: <c>regexp.pin</c> and
/// <c>regexp/</c>). The pin is checked first; a copy it refuses is not run.
/// </summary>
/// <remarks>
/// The report is written to standard output and, with <c>--report</c>, to the file named; CI compares the Native AOT
/// run's with the JIT run's byte for byte. Exit 0 when every vector the rule holds to its <c>out</c> (normative,
/// divergence and status vectors whose op the library implements) gives it; 1 when one does not; 2 on arguments the mode
/// does not read, a pin it refuses or a file it cannot read, with the reason on standard error and no report.
/// </remarks>
internal static class VectorsMode
{
    internal static int Run(ReadOnlySpan<string> args)
    {
        string? directory = null, reportPath = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--report" && i + 1 < args.Length && reportPath is null)
                reportPath = args[++i];
            else if (directory is null && !args[i].StartsWith("--", StringComparison.Ordinal))
                directory = args[i];
            else
                return Usage();
        }

        if (directory is null)
            return Usage();

        VectorCorpus corpus;
        try
        {
            corpus = VectorCorpus.Load(directory);
        }
        catch (VectorPinException refusal)
        {
            Console.Error.WriteLine("refused: " + refusal.Message);
            return 2;
        }
        catch (Exception refusal) when (refusal is FormatException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("unreadable: " + refusal.Message);
            return 2;
        }

        var report = VectorRun.RunOnSmallestStack(corpus, UnicodeTables.Instance);
        if (reportPath is not null)
            File.WriteAllText(reportPath, report);
        Console.Out.Write(report);
        return report.Contains("\n# differences: 0\n", StringComparison.Ordinal) ? 0 : 1;
    }

    private static int Usage()
    {
        Console.Error.WriteLine("usage: Broiler.Regex.Probe --vectors dir [--report path]");
        return 2;
    }
}
