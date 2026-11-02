// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

// Every bound of the reference arms' runs, in one place (tests/ReferenceArms/README.md, "Limits"): the population's
// default policy, which S's step ceiling and V's allowance read, and the bounds of the harness itself, which no engine
// under comparison has. Each run file records them (run.<arm>.json, "limits"), so the comparison's summary.json does.
// Written 2026-11-02, at the second review of task 2.13 of SJL-11.2, before any judged run: the time limits were
// scattered over the harness and arm J (a wall clock of 10 seconds, 1 second after a stop, and a .NET match timeout of
// 1 second), and which of them fired first decided a case's answer, so a run was not reproducible byte for byte.
//
// Arm J has no bound of its own on some inputs (its .NET engine backtracks without one), and any limit of time stops
// some case near it in one run and not in the next. So time decides nothing in a run: a calibration run (--calibrate)
// takes the time limits below and writes the cases they stopped to the stop list, tests/ReferenceArms/stops.tsv, which
// the repository keeps; every other run answers a listed case as stopped without running it, and runs every other case
// to its end under backstops far above the calibration's limits. The listings then follow from the commit, which holds
// the list, and the population.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Broiler.Regex.ReferenceArms;

internal static class Limits
{
    /// <summary>
    /// The population's default policy, step ceiling of S and allowance of V, in the same units (JSD-0081 D8). P3's ample
    /// policy has neither.
    /// </summary>
    internal const ulong DefaultSteps = 10_000_000;

    /// <summary>
    /// The worker processes per class file, each answering one contiguous range of its cases: fixed, not read from the
    /// machine, since in a calibration the ranges decide which cases follow a stop in the same worker
    /// (<see cref="CalibrationCaseTimeAfterAStop"/>).
    /// </summary>
    internal const int Workers = 6;

    /// <summary>The stack of every case's thread: 16 MiB, the stack Broiler.JS's shell runs a script on (SH/Program.cs:128).</summary>
    internal const int StackBytes = 16 << 20;

    /// <summary>
    /// A calibration's limit: the processor time one case may take, read from its worker process's user and system time,
    /// before the worker is stopped and the case listed. Processor time, not wall-clock time, so that a loaded machine
    /// lists about the cases an idle one would.
    /// </summary>
    internal static readonly TimeSpan CalibrationCaseTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// <see cref="CalibrationCaseTime"/> for a case under P3's ample policy, whose inputs run to 10,000,001 code units:
    /// arm J takes about 4.5 seconds of processor time to answer P3-F03, `(?:a)` 100,000 times on 100,000 `a`.
    /// </summary>
    internal static readonly TimeSpan CalibrationAmpleCaseTime = TimeSpan.FromSeconds(20);

    /// <summary>
    /// A calibration's limit for a case whose pattern, flags and policy a case stopped earlier in the same range had: a
    /// pattern no arm's own bound stops costs a range seconds, not hours.
    /// </summary>
    internal static readonly TimeSpan CalibrationCaseTimeAfterAStop = TimeSpan.FromSeconds(2);

    /// <summary>
    /// A run's bound on a case the stop list does not hold, in processor time: twelve times the calibration's limit, so
    /// that a case the calibration saw end ends here too. A case that reaches it is answered <see cref="HarnessStop"/>
    /// and recorded as a stop the list does not hold: the run is then not reproducible, and run.sh says so.
    /// </summary>
    internal static readonly TimeSpan RunBackstop = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The wall-clock time one case may take whatever its processor time, in a calibration and in a run: a backstop for
    /// a worker that does not run, answered as a stop too.
    /// </summary>
    internal static readonly TimeSpan WallClockBackstop = TimeSpan.FromSeconds(900);

    /// <summary>
    /// A case that ends after at least this much wall-clock time is listed in the run's timing file, beside the stops:
    /// the cases nearest a limit.
    /// </summary>
    internal static readonly TimeSpan SlowCase = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The one answer of a case the harness stopped, lost or skipped: in a calibration, past its limit or its worker ended
    /// (a crash, such as the stack overflow of Broiler.Regex 0.1.0-preview.4's recursive matcher, correspondence row B8);
    /// in a run, a case the stop list holds, or one that reached a backstop or crashed. One kind, so that which of them it
    /// was, which can depend on the machine's load, never changes the answer; which one it was is in the timing file,
    /// outside the answers the comparison reads.
    /// </summary>
    internal const string HarnessStop = "limit:harness";

    /// <summary>The limits as a run file records them.</summary>
    internal static IReadOnlyDictionary<string, string> Recorded { get; } = new SortedDictionary<string, string>(StringComparer.Ordinal)
    {
        ["default-steps"] = DefaultSteps.ToString(CultureInfo.InvariantCulture),
        ["workers"] = Workers.ToString(CultureInfo.InvariantCulture),
        ["stack-bytes"] = StackBytes.ToString(CultureInfo.InvariantCulture),
        ["calibration-case-processor-seconds"] = CalibrationCaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture),
        ["calibration-case-processor-seconds-ample-policy"] = CalibrationAmpleCaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture),
        ["calibration-case-processor-seconds-after-a-stop"] = CalibrationCaseTimeAfterAStop.TotalSeconds.ToString(CultureInfo.InvariantCulture),
        ["run-backstop-processor-seconds"] = RunBackstop.TotalSeconds.ToString(CultureInfo.InvariantCulture),
        ["wall-clock-backstop-seconds"] = WallClockBackstop.TotalSeconds.ToString(CultureInfo.InvariantCulture),
        ["slow-case-seconds"] = SlowCase.TotalSeconds.ToString(CultureInfo.InvariantCulture),
        ["harness-stop"] = HarnessStop,
    };
}
