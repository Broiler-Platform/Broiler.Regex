// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   4
// Annotated:        4/4
// Exempt:           13
// Human-reviewed:   0/4
// IP risk:          None
// Security risk:    Low
// Criteria:         2/0
// Resource impact:  1/10 max
// Unverified:       4
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

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
/// The result of a match attempt. <see cref="Success"/> is false for no match.
/// <c>Groups[0]</c> is the whole match; <c>Groups[1..]</c> are the capturing
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

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=C3E4DA
    // Broiler-Falsified-If: RegexMatch.Empty reports Success as true or a non-empty Groups list
    // Broiler-Human:        PENDING
    public static readonly RegexMatch Empty =
        new(false, -1, 0, "", Array.Empty<RegexGroup>(), new Dictionary<string, RegexGroup>());

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
