using System;
using System.IO;
using System.Text;
using Broiler.Regex.Test262Library;
using Broiler.Unicode.Tables;

namespace Broiler.Regex.Probe;

/// <summary>
/// The probe's <c>--test262 dir --unreadable list [--report path]</c> mode (task 2.10 of SJL-11.2; bundle
/// <c>sjl-11-001</c>'s check B-2, "under the JIT and through the Native AOT probe, the two reports byte-identical"): the
/// library runner of <c>Broiler.Regex.Tests/Test262Library/</c>, whose sources this probe compiles in, over the test262
/// checkout in <c>dir</c>, with the unreadable list <c>list</c> (<c>test262/library-unreadable.txt</c>).
/// </summary>
/// <remarks>
/// The report is written to standard output and, with <c>--report</c>, to the file named; CI compares the Native AOT run's
/// with the JIT run's byte for byte. Exit 0 when every file passes and the files the runner cannot read are exactly
/// those the list names; 1 otherwise; 2 on arguments the mode does not read, or a checkout or list it cannot read, with
/// the reason on standard error and no report. The checkout's revision is checked by the caller (CI checks the commit
/// and the tree of <c>test/</c> before the run).
/// </remarks>
internal static class Test262Mode
{
    internal static int Run(ReadOnlySpan<string> args)
    {
        string? directory = null, listPath = null, reportPath = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--report" && i + 1 < args.Length && reportPath is null)
                reportPath = args[++i];
            else if (args[i] == "--unreadable" && i + 1 < args.Length && listPath is null)
                listPath = args[++i];
            else if (directory is null && !args[i].StartsWith("--", StringComparison.Ordinal))
                directory = args[i];
            else
                return Usage();
        }

        if (directory is null || listPath is null)
            return Usage();

        string list;
        string report;
        bool passed;
        try
        {
            list = File.ReadAllText(listPath, new UTF8Encoding(false, true));
            (report, passed) = LibraryRun.RunOnStack(directory, list, UnicodeTables.Instance);
        }
        catch (Exception refusal) when (refusal is FormatException or IOException or UnauthorizedAccessException
            or DecoderFallbackException or InvalidOperationException)
        {
            // InvalidOperationException is the run's thread's failure (LibraryRun.RunOnStack), a checkout without a
            // population's directory among them.
            Console.Error.WriteLine("unreadable: " + refusal.Message);
            return 2;
        }

        if (reportPath is not null)
            File.WriteAllText(reportPath, report);
        Console.Out.Write(report);
        return passed ? 0 : 1;
    }

    private static int Usage()
    {
        Console.Error.WriteLine("usage: Broiler.Regex.Probe --test262 dir --unreadable list [--report path]");
        return 2;
    }
}
