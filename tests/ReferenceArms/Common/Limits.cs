// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

// Every bound of the reference arms' runs, in one place (tests/ReferenceArms/README.md, "Limits"): the population's
// default policy, which S's step ceiling and V's allowance read, and the bounds of the harness itself, which no engine
// under comparison has. Each run file records them (run.<arm>.json, "limits"), so the comparison's summary.json does.
// Written 2026-11-02, at the second review of task 2.13 of SJL-11.2, before any judged run: the time limits were
// scattered over the harness and arm J (a wall clock of 10 seconds, 1 second after a stop, and a .NET match timeout of
// 1 second), and which of them fired first decided a case's answer, so a run was not reproducible byte for byte.

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
    /// The worker processes per class file, each answering one contiguous range of its cases. Fixed, not read from the
    /// machine: the ranges decide which cases follow a stop in the same worker (<see cref="CaseTimeAfterAStop"/>).
    /// </summary>
    internal const int Workers = 3;

    /// <summary>The stack of every case's thread: 16 MiB, the stack Broiler.JS's shell runs a script on (SH/Program.cs:128).</summary>
    internal const int StackBytes = 16 << 20;

    /// <summary>
    /// The processor time one case may take, read from its worker process's user and system time, before the worker is
    /// stopped and the case answered <see cref="HarnessStop"/>. Processor time, not wall-clock time: a loaded machine
    /// slows a case's wall clock and leaves its processor time nearly as it is.
    /// </summary>
    internal static readonly TimeSpan CaseTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// <see cref="CaseTime"/> for a case under P3's ample policy, whose inputs run to 10,000,001 code units: arm J takes
    /// about 4.5 seconds of processor time to answer P3-F03, `(?:a)` 100,000 times on 100,000 `a`.
    /// </summary>
    internal static readonly TimeSpan AmpleCaseTime = TimeSpan.FromSeconds(20);

    /// <summary>
    /// The processor time of a case whose pattern, flags and policy a case stopped earlier in the same range had: a
    /// pattern no arm's own bound stops costs a range seconds, not hours.
    /// </summary>
    internal static readonly TimeSpan CaseTimeAfterAStop = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The wall-clock time one case may take whatever its processor time: a backstop for a worker that does not run
    /// (a machine so loaded that the processor-time limit is not reached in this time), answered as a stop too.
    /// </summary>
    internal static readonly TimeSpan CaseWallClockBackstop = TimeSpan.FromSeconds(120);

    /// <summary>
    /// A case that ends after at least this much wall-clock time is listed in the run's timing file, beside the stops:
    /// the cases nearest a limit, which a run on a slower machine could stop.
    /// </summary>
    internal static readonly TimeSpan SlowCase = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The one answer of a case the harness stopped or lost: past <see cref="CaseTime"/> (or
    /// <see cref="CaseTimeAfterAStop"/>, or <see cref="CaseWallClockBackstop"/>), or its worker ended (a crash, such as
    /// the stack overflow of Broiler.Regex 0.1.0-preview.4's recursive matcher, correspondence row B8). One kind, so
    /// that which of them came first, which can depend on the machine's load, never changes the answer; which one it was
    /// is in the timing file, outside the answers the comparison reads.
    /// </summary>
    internal const string HarnessStop = "limit:harness";

    /// <summary>The limits as a run file records them.</summary>
    internal static IReadOnlyDictionary<string, string> Recorded { get; } = new SortedDictionary<string, string>(StringComparer.Ordinal)
    {
        ["default-steps"] = DefaultSteps.ToString(CultureInfo.InvariantCulture),
        ["workers"] = Workers.ToString(CultureInfo.InvariantCulture),
        ["stack-bytes"] = StackBytes.ToString(CultureInfo.InvariantCulture),
        ["case-processor-seconds"] = CaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture),
        ["case-processor-seconds-ample-policy"] = AmpleCaseTime.TotalSeconds.ToString(CultureInfo.InvariantCulture),
        ["case-processor-seconds-after-a-stop"] = CaseTimeAfterAStop.TotalSeconds.ToString(CultureInfo.InvariantCulture),
        ["case-wall-clock-backstop-seconds"] = CaseWallClockBackstop.TotalSeconds.ToString(CultureInfo.InvariantCulture),
        ["slow-case-seconds"] = SlowCase.TotalSeconds.ToString(CultureInfo.InvariantCulture),
        ["harness-stop"] = HarnessStop,
    };
}
