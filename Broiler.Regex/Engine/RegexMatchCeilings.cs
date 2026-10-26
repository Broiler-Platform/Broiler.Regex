// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           3
// Human-reviewed:   0/8
// IP risk:          None
// Security risk:    High
// Criteria:         2/2
// Resource impact:  2/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.Regex.Engine;

/// <summary>
/// The caller's ceilings for one match call (requirements R1 and R3; decisions D8 and D10 of Broiler.VM's JSD-0081;
/// convention 5): the units of work it may do, the backtrack frames and the trail cells it may hold at once. Reaching one
/// answers <see cref="RegexStatus.LimitExceeded"/> naming it, never <see cref="RegexStatus.NoMatch"/> and never a match.
/// </summary>
/// <remarks>
/// <para>
/// <b>Steps</b> are optional and counted in the units the meter is charged in (<see cref="RegexMachine"/>'s remarks,
/// "Charging"): a call does at most that many, and an instruction that would take it past them is not dispatched.
/// <b>Frames</b> are the choices a call holds open at once, <b>trail cells</b> the register writes that change a
/// register and that backtracking has not undone, whether or not a choice was open when they were made: the cells of an
/// attempt are kept until it ends, when they reset the registers for the next start. A call that would hold one more
/// stops there. So a loop that holds no choice still fills the trail: a counted quantifier writes its iteration's mark
/// and its count, two cells an iteration that consumes, and under the engines' ceilings <c>a{1048575}</c> matches where
/// <c>a{1048576}</c> answers <see cref="RegexStatus.LimitExceeded"/> naming <see cref="RegexLimit.Trail"/>.
/// Broiler.VM's trail keeps every write as well; the old backend, which the public members use until task 2.4c, matches
/// past it.
/// </para>
/// <para>
/// <b>The argument rules.</b> A ceiling below one is the caller's broken precondition, an
/// <see cref="ArgumentOutOfRangeException"/> (convention 3), and so is a frame or trail ceiling above the library's
/// maximum, <see cref="FrameCeilingMaximum"/> or <see cref="TrailCeilingMaximum"/>. The maxima bound the memory one
/// call can hold, twelve bytes a frame and eight a trail cell: 192 MiB of frames and 256 MiB of trail, and, while the
/// array that reaches either doubles into it, the 128 MiB array it replaces too; at the engines' ceilings, 12 MiB and
/// 16 MiB, with 8 MiB more during the last doubling. They are sixteen times the ceilings both engines pass,
/// <see cref="EngineFrames"/> and <see cref="EngineTrail"/> (decision D10, Broiler.VM's <c>MaximumFrames</c> and
/// <c>MaximumTrail</c>, names this library leaves to those values alone), and four times the adversarial corpus's
/// ordinary ceilings, so no caller the programme has is near them. The step ceiling has no maximum: a step is time, not
/// memory, and the meter is the caller's other bound on it. A ceiling the call never reaches costs nothing: the frames
/// and the trail grow as a match needs them.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decisions D8 and D10; IP=None; Security=High; Resources=2; Fingerprint=25E98E
// Broiler-Falsified-If: a frame or trail ceiling of zero or above its maximum, or a step ceiling of zero, is accepted, or the engines' defaults are other than 1 << 20 frames and 1 << 21 trail cells
// Broiler-Human:        PENDING
internal readonly struct RegexMatchCeilings
{
    /// <summary>The largest frame ceiling a caller may pass: 16,777,216 frames, 192 MiB, and 128 MiB more while they grow to it.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=93BE69
    // Broiler-Human:        PENDING
    internal const int FrameCeilingMaximum = 1 << 24;

    /// <summary>The largest trail ceiling a caller may pass: 33,554,432 cells, 256 MiB, and 128 MiB more while they grow to it.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=BD311A
    // Broiler-Human:        PENDING
    internal const int TrailCeilingMaximum = 1 << 25;

    /// <summary>The frame ceiling both engines pass (decision D10): Broiler.VM's <c>MaximumFrames</c>, 1,048,576.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D10; IP=None; Security=Low; Resources=0; Fingerprint=19A109
    // Broiler-Human:        PENDING
    internal const int EngineFrames = 1 << 20;

    /// <summary>The trail ceiling both engines pass (decision D10): Broiler.VM's <c>MaximumTrail</c>, 2,097,152.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D10; IP=None; Security=Low; Resources=0; Fingerprint=139B81
    // Broiler-Human:        PENDING
    internal const int EngineTrail = 1 << 21;

    /// <summary>Ceilings of <paramref name="steps"/> units, or none, <paramref name="frames"/> frames and <paramref name="trail"/> trail cells.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="steps"/> is zero, or <paramref name="frames"/> or <paramref name="trail"/> is below one or above its maximum.
    /// </exception>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=0; Fingerprint=D25D08
    // Broiler-Falsified-If: new RegexMatchCeilings(0, 1, 1), new RegexMatchCeilings(null, 0, 1) or new RegexMatchCeilings(null, 1, TrailCeilingMaximum + 1) does not throw
    // Broiler-Human:        PENDING
    internal RegexMatchCeilings(ulong? steps, int frames, int trail)
    {
        if (steps == 0)
            throw new ArgumentOutOfRangeException(nameof(steps), "A step ceiling, when there is one, is at least one unit.");
        ArgumentOutOfRangeException.ThrowIfLessThan(frames, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(frames, FrameCeilingMaximum);
        ArgumentOutOfRangeException.ThrowIfLessThan(trail, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trail, TrailCeilingMaximum);
        Steps = steps;
        Frames = frames;
        Trail = trail;
    }

    /// <summary>The ceilings both engines pass (decision D10): no step ceiling, <see cref="EngineFrames"/> and <see cref="EngineTrail"/>.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D10; IP=None; Security=Low; Resources=0; Fingerprint=7F531A
    // Broiler-Human:        PENDING
    internal static RegexMatchCeilings Engines => new(null, EngineFrames, EngineTrail);

    /// <summary>The units a call may do, or null for no step ceiling.</summary>
    internal ulong? Steps { get; }

    /// <summary>The backtrack frames a call may hold at once.</summary>
    internal int Frames { get; }

    /// <summary>The trail cells a call may hold at once.</summary>
    internal int Trail { get; }

    /// <summary>Whether the ceilings were made by the constructor: the default value, whose frame ceiling is zero, was not.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=530DC2
    // Broiler-Human:        PENDING
    internal bool IsSet => Frames >= 1;
}
