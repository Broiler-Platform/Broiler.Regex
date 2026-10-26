// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           14
// Human-reviewed:   0/9
// IP risk:          None
// Security risk:    Low
// Criteria:         3/0
// Resource impact:  1/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Broiler.Regex.Matching;

/// <summary>
/// A single captured group within a <see cref="RegexMatch"/>. Mirrors the shape
/// the JavaScript layer needs (<c>Success</c>, <c>Value</c>, <c>Index</c>,
/// <c>Length</c>), so it can stand in for a <c>System.Text.RegularExpressions.Group</c>.
/// </summary>
// Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=2F0682
// Broiler-Human:        PENDING
public sealed class RegexGroup
{
    public bool Success { get; }
    public int Index { get; }
    public int Length { get; }
    public string Value { get; }
    public string? Name { get; }

    internal RegexGroup(bool success, int index, int length, string value, string? name)
    {
        Success = success;
        Index = index;
        Length = length;
        Value = value;
        Name = name;
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=7EA521
    // Broiler-Falsified-If: an unmatched group reports Success as true, an Index other than -1, or a non-empty Value
    // Broiler-Human:        PENDING
    internal static RegexGroup Unmatched(string? name) => new(false, -1, 0, "", name);
}

/// <summary>
/// The result of a match attempt. <see cref="Success"/> is false for no match, and for a match that reached a ceiling,
/// which <see cref="Limit"/> then names. <c>Groups[0]</c> is the whole match; <c>Groups[1..]</c> are the capturing
/// groups in ECMAScript (source) order.
/// </summary>
// Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=DA2CD8
// Broiler-Human:        PENDING
public sealed class RegexMatch
{
    public bool Success { get; }
    public int Index { get; }
    public int Length { get; }
    public string Value { get; }
    public IReadOnlyList<RegexGroup> Groups { get; }
    public IReadOnlyDictionary<string, RegexGroup> NamedGroups { get; }

    /// <summary>
    /// The ceiling the match reached, <see cref="RegexLimit.Frames"/> or <see cref="RegexLimit.Trail"/>, when it
    /// stopped there with no answer; <see cref="RegexLimit.None"/> for a match and for no match (requirement R1:
    /// exhaustion is its own answer, never no match).
    /// </summary>
    /// <remarks>
    /// <b>Provisional</b>, new in task 2.4c of SJL-11.2 with the string-bearing members' move to the new backend; task
    /// 2.9 fixes the public surface of <c>0.2.0-preview.1</c>.
    /// </remarks>
    public RegexLimit Limit { get; }

    /// <summary>
    /// The no-match result, which every call that finds no match answers. Nothing reachable from it can change: its
    /// groups are an empty array and its named groups an empty read-only dictionary (convention 7).
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=E09821
    // Broiler-Falsified-If: RegexMatch.Empty reports Success as true or a non-empty Groups list, or a cast of its NamedGroups to IDictionary admits a write
    // Broiler-Human:        PENDING
    public static readonly RegexMatch Empty =
        new(false, -1, 0, "", Array.Empty<RegexGroup>(), ReadOnlyDictionary<string, RegexGroup>.Empty);

    /// <summary>The failed match of a call that reached <paramref name="limit"/>, one shared instance for each ceiling.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=771ACF
    // Broiler-Falsified-If: the match answered for a ceiling reports Success as true, a Limit of None or another ceiling's, or a non-empty Groups list
    // Broiler-Human:        PENDING
    internal static RegexMatch Exceeded(RegexLimit limit) => limit switch
    {
        RegexLimit.Frames => FramesExceeded,
        RegexLimit.Trail => TrailExceeded,
        RegexLimit.Steps => StepsExceeded,
        _ => throw new ArgumentOutOfRangeException(nameof(limit), limit, "A match stops only at a step, frame or trail ceiling."),
    };

    /// <summary>The failed match of a call that reached the frame ceiling.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=A6FDF3
    // Broiler-Human:        PENDING
    private static readonly RegexMatch FramesExceeded = new(RegexLimit.Frames);

    /// <summary>The failed match of a call that reached the trail ceiling.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=B18928
    // Broiler-Human:        PENDING
    private static readonly RegexMatch TrailExceeded = new(RegexLimit.Trail);

    /// <summary>The failed match of a call that reached a step ceiling, which the string-bearing members do not set.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=83D4EE
    // Broiler-Human:        PENDING
    private static readonly RegexMatch StepsExceeded = new(RegexLimit.Steps);

    /// <summary>A failed match naming <paramref name="limit"/>, with no groups, as <see cref="Empty"/> has none.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=881BBA
    // Broiler-Human:        PENDING
    private RegexMatch(RegexLimit limit)
        : this(false, -1, 0, "", Array.Empty<RegexGroup>(), ReadOnlyDictionary<string, RegexGroup>.Empty) => Limit = limit;

    internal RegexMatch(bool success, int index, int length, string value,
        IReadOnlyList<RegexGroup> groups, IReadOnlyDictionary<string, RegexGroup> namedGroups)
    {
        Success = success;
        Index = index;
        Length = length;
        Value = value;
        Groups = groups;
        NamedGroups = namedGroups;
    }
}
