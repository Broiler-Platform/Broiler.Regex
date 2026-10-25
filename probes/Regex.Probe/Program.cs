using System;
using System.IO;

namespace Broiler.Regex.Probe;

/// <summary>
/// The probe's entry point: <c>Broiler.Regex.Probe [--report path]</c>. It compiles every case of
/// <see cref="ProbePopulation"/> once and matches it, writes the report to standard output and, with
/// <c>--report</c>, to the file named, and exits 0. It exits 2 on arguments it does not read. What it proves is
/// in the publish and in the comparison of its Native AOT report with its JIT report, which CI makes.
/// </summary>
internal static class Program
{
    internal static int Main(string[] args)
    {
        string? reportPath = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--report" && i + 1 < args.Length)
            {
                reportPath = args[++i];
            }
            else
            {
                Console.Error.WriteLine("usage: Broiler.Regex.Probe [--report path]");
                return 2;
            }
        }

        var report = ProbePopulation.Report(ProbePopulation.Compile(ProbePopulation.Cases()));
        if (reportPath is not null)
            File.WriteAllText(reportPath, report);

        Console.Out.Write(report);
        return 0;
    }
}
