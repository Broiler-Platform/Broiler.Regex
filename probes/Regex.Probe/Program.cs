using System;
using System.IO;
using System.Threading;

namespace Broiler.Regex.Probe;

/// <summary>
/// The probe's entry point: <c>Broiler.Regex.Probe [--report path]</c>. On a thread of
/// <see cref="ProbePopulation.StackBytes"/>, it compiles every case of <see cref="ProbePopulation"/> once and matches
/// it, runs every case of <see cref="EnginePopulation"/> through the new backend's entry and the text helpers over
/// <see cref="TextPopulation"/>, writes the report to standard output and, with <c>--report</c>, to the file named, and
/// exits 0. It exits 2 on arguments it does not read. What it
/// proves is in the publish and in the comparison of its Native AOT report with its JIT report, which CI makes.
/// </summary>
/// <remarks>
/// The hidden argument <c>--engine-row corpus id</c> runs one row of the adversarial corpus through the new backend's
/// entry in this process alone (<see cref="EngineRun.Row"/>), so that a test can run each row in a child process and
/// observe a crash rather than suffer it.
/// </remarks>
internal static class Program
{
    internal static int Main(string[] args)
    {
        if (args.Length == 3 && args[0] == "--engine-row")
            return EngineRun.Row(args[1], args[2]);

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

        // The population runs on a thread of the smallest stack the adversarial corpus declares, so that its nesting
        // cases prove the library's maximum there, under Native AOT as under the JIT.
        var report = "";
        var thread = new Thread(() => report = ProbePopulation.Report(ProbePopulation.Compile(ProbePopulation.Cases())) + EnginePopulation.Report() + TextPopulation.Report(), ProbePopulation.StackBytes);
        thread.Start();
        thread.Join();
        if (reportPath is not null)
            File.WriteAllText(reportPath, report);

        Console.Out.Write(report);
        return 0;
    }
}
