// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   12
// Annotated:        12/12
// Exempt:           14
// Human-reviewed:   0/12
// IP risk:          None
// Security risk:    Low
// Criteria:         6/0
// Resource impact:  1/10 max
// Unverified:       12
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Broiler.Regex;

/// <summary>
/// A single captured group within a <see cref="RegexMatch"/>: whether it participated, where, and its text, the shape
/// the JavaScript layer needs (<c>Success</c>, <c>Value</c>, <c>Index</c>, <c>Length</c>) for one element of a match's
/// result (ECMA-262 §22.2.7.2, RegExpBuiltinExec, steps 33 and 34).
/// </summary>
// Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=2F0682
// Broiler-Human:        PENDING
public sealed class RegexGroup
{
    /// <summary>Whether the group participated in the match: false for a group the match did not reach, whose captured value is undefined.</summary>
    public bool Success { get; }

    /// <summary>The code-unit index of the group's first code unit in the subject, or -1 for a group that did not participate.</summary>
    public int Index { get; }

    /// <summary>The group's length in code units, 0 for a group that did not participate.</summary>
    public int Length { get; }

    /// <summary>The text the group captured, the subject's code units from <see cref="Index"/>, or the empty string for a group that did not participate.</summary>
    public string Value { get; }

    /// <summary>The group's name (ECMA-262 §22.2.1, GroupSpecifier), or null for a group the pattern does not name and for group 0.</summary>
    public string? Name { get; }

    /// <summary>
    /// The group's number: 0 for the whole match, and the capturing group's in source order otherwise (ECMA-262
    /// §22.2.1.3, CountLeftCapturingParensBefore). Through <see cref="RegexMatch.GroupNamed"/> it is the number of the
    /// group of a name that participated, where ES2025 lets several groups share the name (requirement R6).
    /// </summary>
    public int Number { get; }

    internal RegexGroup(int number, bool success, int index, int length, string value, string? name)
    {
        Number = number;
        Success = success;
        Index = index;
        Length = length;
        Value = value;
        Name = name;
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=247722
    // Broiler-Falsified-If: an unmatched group reports Success as true, an Index other than -1, a non-empty Value, or a Number other than its own
    // Broiler-Human:        PENDING
    internal static RegexGroup Unmatched(int number, string? name) => new(number, false, -1, 0, "", name);
}

/// <summary>
/// The result of a match attempt, with its substrings made: the string-bearing result decision D12 keeps beside the offset
/// entry (requirement R8). <see cref="Success"/> is false for no match, and for a match that reached a ceiling, which
/// <see cref="Limit"/> then names. <c>Groups[0]</c> is the whole match; <c>Groups[1..]</c> are the capturing groups in
/// ECMAScript (source) order.
/// </summary>
/// <remarks>
/// <b>Changed in <c>0.1.0-preview.6</c></b> (task 2.9 of SJL-11.2; convention 1, check C1): <see cref="Groups"/> is a
/// span of the groups where it was an <c>IReadOnlyList&lt;RegexGroup&gt;</c>, so <c>Groups[i]</c> and <c>foreach</c> read
/// as before and <c>Count</c> is <c>Length</c>; <see cref="GroupNamed"/> answers a name's group where the
/// <c>IReadOnlyDictionary&lt;string, RegexGroup&gt;</c> <c>NamedGroups</c> did; and <see cref="BroilerRegex.NextMatch"/>
/// answers the global iteration's next match where <c>BroilerRegex.Matches</c>'s <c>IEnumerable&lt;RegexMatch&gt;</c> did. Breaks
/// of <c>0.1.0-preview.5</c>, listed with requirement R10 in <c>docs/suppressions.md</c>.
/// </remarks>
// Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=DA2CD8
// Broiler-Human:        PENDING
public sealed class RegexMatch
{
    /// <summary>The groups, group 0 first, none for a failed match: immutable, so nothing reachable from <see cref="Empty"/> can change (convention 7).</summary>
    private readonly ImmutableArray<RegexGroup> _groups;

    /// <summary>The subject the match was found in, which <see cref="BroilerRegex.NextMatch"/> goes on in; null for a failed match.</summary>
    internal string? Input { get; }

    /// <summary>Whether the pattern matched: false for no match and for a match that reached a ceiling (<see cref="Limit"/>).</summary>
    public bool Success { get; }

    /// <summary>The code-unit index of the match in the subject, or -1 for a failed match (ECMA-262 §22.2.7.2, RegExpBuiltinExec, step 22).</summary>
    public int Index { get; }

    /// <summary>The match's length in code units, 0 for a failed match.</summary>
    public int Length { get; }

    /// <summary>The matched text, the subject's code units from <see cref="Index"/>, or the empty string for a failed match.</summary>
    public string Value { get; }

    /// <summary>
    /// The groups, group 0 (the whole match) first and then each capturing group in source order, each participating or
    /// not; none for a failed match. A span of the match's own array, which a caller cannot change.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=4CAEDE
    // Broiler-Falsified-If: a match of (a)(b)? on "a" answers other than two groups, group 0 first, or a caller can write a group through the span
    // Broiler-Human:        PENDING
    public ReadOnlySpan<RegexGroup> Groups => _groups.AsSpan();

    /// <summary>
    /// The ceiling the match reached, <see cref="RegexLimit.Frames"/> or <see cref="RegexLimit.Trail"/>, when it
    /// stopped there with no answer; <see cref="RegexLimit.None"/> for a match and for no match (requirement R1:
    /// exhaustion is its own answer, never no match).
    /// </summary>
    public RegexLimit Limit { get; }

    /// <summary>
    /// The no-match result, which every call that finds no match answers. Nothing reachable from it can change: its
    /// groups are an empty array (convention 7).
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=166586
    // Broiler-Falsified-If: RegexMatch.Empty reports Success as true, a non-empty Groups span or a group for a name
    // Broiler-Human:        PENDING
    public static readonly RegexMatch Empty = new(false, -1, 0, "", Array.Empty<RegexGroup>(), null);

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
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=D49C0B
    // Broiler-Human:        PENDING
    private RegexMatch(RegexLimit limit)
        : this(false, -1, 0, "", Array.Empty<RegexGroup>(), null) => Limit = limit;

    /// <summary>A match, or a failed one, of <paramref name="groups"/>, an array made for it alone and never written after.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=A43A57
    // Broiler-Falsified-If: a match's groups change after it is answered, or its subject is not the one it was found in
    // Broiler-Human:        PENDING
    internal RegexMatch(bool success, int index, int length, string value, RegexGroup[] groups, string? input)
    {
        Success = success;
        Index = index;
        Length = length;
        Value = value;
        // The array is the caller's, made for this match alone and never written after, so it is taken as it is.
        _groups = ImmutableCollectionsMarshal.AsImmutableArray(groups);
        Input = input;
    }

    /// <summary>
    /// The group of <paramref name="name"/>: the one of the name's groups that participated, or, when none of them did, the
    /// first of them, unmatched; null when the pattern declares no group of that name, and for a failed match. ES2025 lets
    /// several groups share a name when no two of them can both participate (requirement R6), and RegExpBuiltinExec then
    /// makes the <c>groups</c> object's property of the name from the one that participated (ECMA-262 §22.2.7.2, step
    /// 34.e); the answer is the same instance as its element of <see cref="Groups"/>, whose
    /// <see cref="RegexGroup.Number"/> says which. Where none participated, ECMA-262 gives <c>groups</c> undefined for the
    /// name, and the answer's <see cref="RegexGroup.Success"/> is false.
    /// </summary>
    /// <param name="name">The group name, decoded as the pattern declares it (<see cref="BroilerRegex.GroupNames"/> lists them).</param>
    /// <returns>The group, or null.</returns>
    /// <remarks>
    /// The groups carry their names (<see cref="RegexGroup.Name"/>), so the name's groups are read from them in number
    /// order, the order <see cref="RegexGroupNames.GroupsNamed"/> lists them in; the work is in proportion to the
    /// pattern's capture count.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2 RegExpBuiltinExec; IP=None; Security=Low; Resources=1; Fingerprint=A8B0C7
    // Broiler-Falsified-If: under (?:(?<n>a)|(?<n>b)) on "b" the name n answers group 1, a name no group of which participated answers null, or an undeclared name answers a group
    // Broiler-Human:        PENDING
    public RegexGroup? GroupNamed(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        RegexGroup? first = null;
        foreach (var group in _groups)
        {
            if (!string.Equals(group.Name, name, StringComparison.Ordinal))
                continue;
            if (group.Success)
                return group;
            first ??= group;
        }

        return first;
    }
}
