// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   7
// Annotated:        7/7
// Exempt:           5
// Human-reviewed:   0/7
// IP risk:          None
// Security risk:    Medium
// Criteria:         6/0
// Resource impact:  1/10 max
// Unverified:       7
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;

namespace Broiler.Regex;

/// <summary>
/// The group names of a compiled pattern (ECMA-262 §22.2.1, GroupSpecifier; requirement R6): each name with the groups
/// that carry it, each group with its name, and, for a match the offset entry wrote, the group of a name that
/// participated. ES2025 lets one name be declared by several groups when no two of them can both participate
/// (§22.2.1.1, MightBothParticipate §22.2.1.4), so a name maps to a list of groups, and a match to the one of them that
/// participated, at most one (§22.2.2.7.2 BackreferenceMatcher's assertion; §22.2.7.2 RegExpBuiltinExec, step 34.e).
/// </summary>
/// <remarks>
/// <para>
/// <b>Order.</b> The names are in the order their first group appears in the pattern, which is the order
/// RegExpBuiltinExec creates the <c>groups</c> object's properties in (its step 34.e creates a name's property at its
/// first group and writes it again at each later one up to the one that participated), so <see cref="NameAt"/> from 0
/// to <see cref="Count"/> - 1 is the property order an engine gives <c>groups</c> and <c>indices.groups</c>. A name's
/// groups are in ascending order.
/// </para>
/// <para>
/// <b>Immutable.</b> Made once by the parser for the pattern and never changed, so a compiled pattern and its concurrent
/// matches share it (convention 10). Its members allocate nothing: a list of groups is a read-only span of its own, and
/// <see cref="ParticipatingGroup"/> reads the caller's slots (convention 11).
/// </para>
/// <para>
/// <b>Provisional.</b> New in task 2.6 of SJL-11.2, where requirement R6 reshapes <c>GroupNames</c>, which was a map of
/// each name to one group; task 2.9 fixes the public surface of <c>0.2.0-preview.1</c>.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1.4 and s22.2.7.2; IP=None; Security=Medium; Resources=1; Fingerprint=A28383
// Broiler-Falsified-If: for /(?<y>a)(?<x>a)|(?<x>b)(?<y>b)/ the names are not y then x, x's groups are not 2 and 3, group 4's name is not y, or the slots of a match of bb send x to other than group 3
// Broiler-Human:        PENDING
public sealed class RegexGroupNames
{
    /// <summary>The names, in the order of their first groups.</summary>
    private readonly string[] _nameList;

    /// <summary>Each name's groups, ascending, at the name's position in <see cref="_nameList"/>.</summary>
    private readonly int[][] _groupLists;

    /// <summary>Each name's position in <see cref="_nameList"/>, compared by code unit; written once, by the constructor.</summary>
    private readonly Dictionary<string, int> _positionOf;

    /// <summary>Each group's name, by group number, or null for a group without one; index 0, the whole match, null.</summary>
    private readonly string?[] _nameOfGroup;

    /// <summary>
    /// The names of a pattern of <paramref name="captureCount"/> capturing groups: <paramref name="nameList"/> in the order of
    /// their first groups, and at the same positions in <paramref name="groupLists"/> the groups of each, ascending. The
    /// arrays become the instance's own.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=31A196
    // Broiler-Falsified-If: a group of a name is given no name, or another name's, or a group beyond the capture count is accepted
    // Broiler-Human:        PENDING
    internal RegexGroupNames(string[] nameList, int[][] groupLists, int captureCount)
    {
        _nameList = nameList;
        _groupLists = groupLists;
        _positionOf = new Dictionary<string, int>(nameList.Length, StringComparer.Ordinal);
        _nameOfGroup = new string?[captureCount + 1];
        for (var at = 0; at < nameList.Length; at++)
        {
            _positionOf.Add(nameList[at], at);
            foreach (var group in groupLists[at])
                _nameOfGroup[group] = nameList[at];
        }

        CaptureCount = captureCount;
    }

    /// <summary>The number of capturing groups of the pattern, whose slots <see cref="ParticipatingGroup"/> reads.</summary>
    internal int CaptureCount { get; }

    /// <summary>How many distinct names the pattern declares; zero when it declares none.</summary>
    /// <remarks>Provisional (task 2.9 fixes the public surface).</remarks>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=E964CF
    // Broiler-Human:        PENDING
    public int Count => _nameList.Length;

    /// <summary>
    /// The name at <paramref name="index"/>, from 0 to <see cref="Count"/> - 1, in the order the names' first groups appear
    /// in the pattern: the order of the <c>groups</c> object's properties (§22.2.7.2, step 34.e).
    /// </summary>
    /// <param name="index">The name's position, from 0 to <see cref="Count"/> - 1.</param>
    /// <returns>The name, decoded: the code points its GroupSpecifier spells, escapes read (§22.2.1.10 CapturingGroupName).</returns>
    /// <remarks>Provisional (task 2.9 fixes the public surface).</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative, or <see cref="Count"/> or more.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2; IP=None; Security=Low; Resources=0; Fingerprint=521344
    // Broiler-Falsified-If: for /(?<y>a)(?<x>a)|(?<x>b)(?<y>b)/ position 0 answers x, or a position of Count answers a name
    // Broiler-Human:        PENDING
    public string NameAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _nameList.Length);
        return _nameList[index];
    }

    /// <summary>
    /// The groups <paramref name="name"/> names, in ascending order: one for a name declared once, several for a name
    /// ES2025 lets alternatives share (§22.2.1.9 GroupSpecifiersThatMatch), none for a name the pattern does not declare.
    /// </summary>
    /// <param name="name">A name, decoded, compared by code unit.</param>
    /// <returns>The group numbers, a read-only view of the instance's own, which nothing changes.</returns>
    /// <remarks>Provisional (task 2.9 fixes the public surface).</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1.9; IP=None; Security=Low; Resources=0; Fingerprint=DB3D86
    // Broiler-Falsified-If: for /(?<x>a)|(?<x>b)/ the groups of x are not 1 and 2, or a name the pattern does not declare answers a group
    // Broiler-Human:        PENDING
    public ReadOnlySpan<int> GroupsNamed(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _positionOf.TryGetValue(name, out var at) ? _groupLists[at] : default;
    }

    /// <summary>The name of group <paramref name="group"/>, or null when it has none, as group 0, the whole match, has none.</summary>
    /// <param name="group">A group number, from 0 to the pattern's capture count.</param>
    /// <remarks>Provisional (task 2.9 fixes the public surface).</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="group"/> is negative or past the pattern's capture count.</exception>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=219B4C
    // Broiler-Falsified-If: for /(?<x>a)|(b)|(?<x>c)/ group 3's name is not x, group 2 has one, or group 4 answers instead of throwing
    // Broiler-Human:        PENDING
    public string? NameOfGroup(int group)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(group);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(group, CaptureCount);
        return _nameOfGroup[group];
    }

    /// <summary>
    /// The group of <paramref name="name"/> that participated in the match whose offsets the offset entry wrote into
    /// <paramref name="slots"/>: the one whose start and end are set, at most one of the name's groups (§22.2.2.7.2,
    /// BackreferenceMatcher's assertion); 0 when none did, or when the pattern declares no such name. RegExpBuiltinExec
    /// gives the name's property of <c>groups</c> that group's substring, and <c>indices.groups</c> its offsets, or
    /// undefined for 0 (§22.2.7.2, steps 34.e and 35).
    /// </summary>
    /// <param name="name">A name, decoded, compared by code unit.</param>
    /// <param name="slots">The slots of a match, as the offset entry writes them: two for each group, group 0 first, -1 for a group that did not participate.</param>
    /// <returns>The participating group's number, or 0.</returns>
    /// <remarks>
    /// Reads the slots of the name's groups alone, and allocates nothing. Provisional (task 2.9 fixes the public surface).
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="slots"/> holds fewer slots than a match of the pattern writes, two for each group and two for the whole match.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.7.2; IP=None; Security=Medium; Resources=1; Fingerprint=FFB2A0
    // Broiler-Falsified-If: for /(?<x>a)|(?<x>b)/ the slots of a match of b send x to other than group 2, the slots of no participating group to other than 0, or a call allocates
    // Broiler-Human:        PENDING
    public int ParticipatingGroup(string name, ReadOnlySpan<int> slots)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (slots.Length < 2 * (CaptureCount + 1))
            throw new ArgumentException("The slots hold fewer than a match of the pattern writes: two for each group and two for the whole match.", nameof(slots));

        foreach (var group in GroupsNamed(name))
        {
            if (slots[2 * group] >= 0 && slots[(2 * group) + 1] >= 0)
                return group;
        }

        return 0;
    }
}
