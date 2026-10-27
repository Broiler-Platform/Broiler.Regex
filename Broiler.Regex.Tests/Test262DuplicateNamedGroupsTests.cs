using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.Regex.Engine;
using Broiler.Regex.Text;
using Xunit;

namespace Broiler.Regex.Tests;

// Ported as data from test262, the ECMAScript conformance suite (https://github.com/tc39/test262), at commit
// ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the programme's pin: every file whose front matter's features list
// regexp-duplicate-named-groups, each assertion transcribed to the library's API where the library alone answers it,
// and the syntax tests of test/language/literals/regexp/named-groups/ and test/built-ins/RegExp/named-groups/, each
// literal or pattern with whether it is refused. test262 is made available under the BSD License (3-clause), whose text,
// copyright notice and disclaimer are kept beside this file in test262-LICENSE.txt, as its condition 1 requires. Each
// file ported carries its own notice, kept here as the file states it:
//   test/annexB/built-ins/RegExp/prototype/compile/duplicate-named-capturing-groups-syntax.js: Copyright 2022 Igalia, S.L. All rights reserved.
//   test/built-ins/RegExp/duplicate-named-capturing-groups-syntax.js: Copyright 2022 Igalia, S.L. All rights reserved.
//   test/built-ins/RegExp/named-groups/duplicate-names-exec.js: Copyright 2022 Igalia S.L. All rights reserved.
//   test/built-ins/RegExp/named-groups/duplicate-names-group-property-enumeration-order.js: Copyright 2022 Kevin Gibbons. All rights reserved.
//   test/built-ins/RegExp/named-groups/duplicate-names-match-indices.js: Copyright 2022 Kevin Gibbons. All rights reserved.
//   test/built-ins/RegExp/named-groups/duplicate-names-match.js: Copyright 2022 Kevin Gibbons. All rights reserved.
//   test/built-ins/RegExp/named-groups/duplicate-names-matchall.js: Copyright 2022 Igalia S.L. All rights reserved.
//   test/built-ins/RegExp/named-groups/duplicate-names-replace.js: Copyright 2022 Kevin Gibbons. All rights reserved.
//   test/built-ins/RegExp/named-groups/duplicate-names-replaceall.js: Copyright 2022 Igalia S.L. All rights reserved.
//   test/built-ins/RegExp/named-groups/duplicate-names-search.js: Copyright 2022 Igalia S.L. All rights reserved.
//   test/built-ins/RegExp/named-groups/duplicate-names-split.js: Copyright 2022 Igalia S.L. All rights reserved.
//   test/built-ins/RegExp/named-groups/duplicate-names-test.js: Copyright 2022 Igalia S.L. All rights reserved.
//   test/built-ins/RegExp/named-groups/non-unicode-property-names-invalid.js: Copyright (C) 2020 Apple Inc. All rights reserved.
//   test/built-ins/RegExp/named-groups/unicode-property-names-invalid.js: Copyright (C) 2020 Apple Inc. All rights reserved.
//   test/built-ins/RegExp/prototype/exec/duplicate-named-groups-properties.js: Copyright 2022 Igalia, S.L. All rights reserved.
//   test/built-ins/RegExp/prototype/exec/duplicate-named-indices-groups-properties.js: Copyright 2022 Igalia, S.L. All rights reserved.
//   test/built-ins/String/prototype/match/duplicate-named-groups-properties.js: Copyright 2022 Igalia, S.L. All rights reserved.
//   test/built-ins/String/prototype/match/duplicate-named-indices-groups-properties.js: Copyright 2022 Igalia, S.L. All rights reserved.
//   test/staging/built-ins/RegExp/named-groups/duplicate-named-groups-replace.js: Copyright (C) 2024 the V8 project authors. All rights reserved.
//   test/staging/built-ins/RegExp/named-groups/duplicate-named-groups-search.js: Copyright (C) 2024 the V8 project authors. All rights reserved.
//   test/staging/built-ins/RegExp/named-groups/duplicate-named-groups.js: Copyright (C) 2024 the V8 project authors. All rights reserved.
//   test/language/literals/regexp/named-groups/*.js (56 files): Copyright 2017 the V8 project authors. All rights reserved.
// THIS SOFTWARE IS PROVIDED BY THE ECMA INTERNATIONAL "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES ARE
// DISCLAIMED (the license's disclaimer, in full in test262-LICENSE.txt). The data is test-only: it is compiled into
// the test assembly and into no package.

/// <summary>
/// test262's duplicate named groups (requirement R6, decision D14's reading of R6's exit criterion) judged by the library
/// alone, with no host: the plan's row 2.6 checks that "test262's files tagged regexp-duplicate-named-groups pass in
/// 2.10's host job"; until that job runs, this test runs every assertion of those files that the library answers, and
/// names the files whose assertions need the host (the comment above). The answers are the library's terms for what a
/// host builds: <c>exec</c>'s array is the match and each group's substring or undefined; <c>groups.x</c> is
/// <see cref="RegexMatch.NamedGroups"/>' entry for x, its substring when it participated and undefined when not;
/// <c>Object.keys(groups)</c> is the order of <see cref="RegexGroupNames"/>' names; <c>indices.groups.x</c> the
/// participating group's offsets; <c>test</c> whether there is a match; <c>search</c> the first match's index;
/// <c>matchAll</c> the global iteration, <see cref="BroilerRegex.Matches"/>; <c>source</c> the helper's EscapePattern.
/// </summary>
public class Test262DuplicateNamedGroupsTests
{
    /// <summary>How a file is judged: by this test alone, or by 2.10's host job, this test running the matches its assertions read.</summary>
    internal enum Judged
    {
        /// <summary>Every assertion of the file is one the library answers, and this test makes it.</summary>
        Library,

        /// <summary>
        /// The file's assertions read a host's algorithm over the matches (GetSubstitution for <c>replace</c> and
        /// <c>replaceAll</c>, the sticky splitter of <c>split</c>): this test checks each match those assertions read, and
        /// the file waits for task 2.10's host job, which runs it whole.
        /// </summary>
        Host,
    }

    /// <summary>
    /// Every file of test262 at the pin whose front matter's features list regexp-duplicate-named-groups (19 files, found
    /// by its front matter at ccaac100), and how it is judged here.
    /// </summary>
    internal static readonly (string File, Judged Judged)[] Tagged =
    [
        ("test/annexB/built-ins/RegExp/prototype/compile/duplicate-named-capturing-groups-syntax.js", Judged.Library),
        ("test/built-ins/RegExp/duplicate-named-capturing-groups-syntax.js", Judged.Library),
        ("test/built-ins/RegExp/named-groups/duplicate-names-exec.js", Judged.Library),
        ("test/built-ins/RegExp/named-groups/duplicate-names-group-property-enumeration-order.js", Judged.Library),
        ("test/built-ins/RegExp/named-groups/duplicate-names-match-indices.js", Judged.Library),
        ("test/built-ins/RegExp/named-groups/duplicate-names-match.js", Judged.Library),
        ("test/built-ins/RegExp/named-groups/duplicate-names-matchall.js", Judged.Library),
        ("test/built-ins/RegExp/named-groups/duplicate-names-replace.js", Judged.Host),
        ("test/built-ins/RegExp/named-groups/duplicate-names-replaceall.js", Judged.Host),
        ("test/built-ins/RegExp/named-groups/duplicate-names-search.js", Judged.Library),
        ("test/built-ins/RegExp/named-groups/duplicate-names-split.js", Judged.Host),
        ("test/built-ins/RegExp/named-groups/duplicate-names-test.js", Judged.Library),
        ("test/built-ins/RegExp/prototype/exec/duplicate-named-groups-properties.js", Judged.Library),
        ("test/built-ins/RegExp/prototype/exec/duplicate-named-indices-groups-properties.js", Judged.Library),
        ("test/built-ins/String/prototype/match/duplicate-named-groups-properties.js", Judged.Library),
        ("test/built-ins/String/prototype/match/duplicate-named-indices-groups-properties.js", Judged.Library),
        ("test/staging/built-ins/RegExp/named-groups/duplicate-named-groups-replace.js", Judged.Host),
        ("test/staging/built-ins/RegExp/named-groups/duplicate-named-groups-search.js", Judged.Library),
        ("test/staging/built-ins/RegExp/named-groups/duplicate-named-groups.js", Judged.Library),
    ];

    /// <summary>
    /// The literals of test/language/literals/regexp/named-groups/, each negative syntax test's literal (its body and
    /// flags) refused, and the file's name; the two positive files are their own facts below.
    /// </summary>
    internal static readonly (string File, string Pattern, string Flags, bool Accepted)[] Literals =
    [
        ("invalid-dangling-groupname-2-u.js", @"(?<a>a)\k<ab>", "u", false),
        ("invalid-dangling-groupname-2.js", @"(?<a>a)\k<ab>", "", false),
        ("invalid-dangling-groupname-3-u.js", @"(?<ab>a)\k<a>", "u", false),
        ("invalid-dangling-groupname-3.js", @"(?<ab>a)\k<a>", "", false),
        ("invalid-dangling-groupname-4-u.js", @"\k<a>(?<ab>a)", "u", false),
        ("invalid-dangling-groupname-4.js", @"\k<a>(?<ab>a)", "", false),
        ("invalid-dangling-groupname-5.js", @"\k<a>(?<b>x)", "", false),
        ("invalid-dangling-groupname-u.js", @"(?<a>.)\k<b>", "u", false),
        ("invalid-dangling-groupname-without-group-u.js", @"\k<a>", "u", false),
        ("invalid-dangling-groupname.js", @"(?<a>.)\k<b>", "", false),
        ("invalid-duplicate-groupspecifier-2-u.js", @"(?<a>a)(?<b>b)(?<a>a)", "u", false),
        ("invalid-duplicate-groupspecifier-2.js", @"(?<a>a)(?<b>b)(?<a>a)", "", false),
        ("invalid-duplicate-groupspecifier-u.js", @"(?<a>a)(?<a>a)", "u", false),
        ("invalid-duplicate-groupspecifier.js", @"(?<a>a)(?<a>a)", "", false),
        ("invalid-empty-groupspecifier-u.js", @"(?<>a)", "u", false),
        ("invalid-empty-groupspecifier.js", @"(?<>a)", "", false),
        ("invalid-identity-escape-in-capture-u.js", @"(?<a>\a)", "u", false),
        ("invalid-incomplete-groupname-2-u.js", @"(?<a>.)\k<a", "u", false),
        ("invalid-incomplete-groupname-2.js", @"(?<a>.)\k<a", "", false),
        ("invalid-incomplete-groupname-3-u.js", @"(?<a>.)\k<>", "u", false),
        ("invalid-incomplete-groupname-3.js", @"(?<a>.)\k<>", "", false),
        ("invalid-incomplete-groupname-4.js", @"\k<a(?<a>a)", "", false),
        ("invalid-incomplete-groupname-5.js", @"\k<a(?<a>.)", "", false),
        ("invalid-incomplete-groupname-6.js", @"\k(?<a>.)", "", false),
        ("invalid-incomplete-groupname-u.js", @"(?<a>.)\k", "u", false),
        ("invalid-incomplete-groupname-without-group-2-u.js", @"\k<>", "u", false),
        ("invalid-incomplete-groupname-without-group-3-u.js", @"\k", "u", false),
        ("invalid-incomplete-groupname-without-group-u.js", @"\k<a", "u", false),
        ("invalid-incomplete-groupname.js", @"(?<a>.)\k", "", false),
        ("invalid-non-id-continue-groupspecifier-4-u.js", @"(?<a\>.)", "u", false),
        ("invalid-non-id-continue-groupspecifier-4.js", @"(?<a\>.)", "", false),
        ("invalid-non-id-continue-groupspecifier.js", @"(?<$❞>a)", "", false),
        ("invalid-non-id-start-groupspecifier-2-u.js", @"(?<𐒤>a)", "u", false),
        ("invalid-non-id-start-groupspecifier-2.js", @"(?<𐒤>a)", "", false),
        ("invalid-non-id-start-groupspecifier-3.js", @"(?<a\uD83D\uDF12>.)", "", false),
        ("invalid-non-id-start-groupspecifier-4-u.js", @"(?<a\uD801>.)", "u", false),
        ("invalid-non-id-start-groupspecifier-4.js", @"(?<a\uD801>.)", "", false),
        ("invalid-non-id-start-groupspecifier-5-u.js", @"(?<a\uDCA4>.)", "u", false),
        ("invalid-non-id-start-groupspecifier-5.js", @"(?<a\uDCA4>.)", "", false),
        ("invalid-non-id-start-groupspecifier-6.js", @"(?<a\u{1F08B}>.)", "", false),
        ("invalid-non-id-start-groupspecifier-7.js", @"(?<a\u{10FFFF}>.)", "", false),
        ("invalid-non-id-start-groupspecifier-8-u.js", @"(?<\>.)", "u", false),
        ("invalid-non-id-start-groupspecifier-8.js", @"(?<\>.)", "", false),
        ("invalid-non-id-start-groupspecifier-9-u.js", @"(?<a\u{110000}>.)", "u", false),
        ("invalid-non-id-start-groupspecifier-u.js", @"(?<❤>a)", "u", false),
        ("invalid-non-id-start-groupspecifier.js", @"(?<❤>a)", "", false),
        ("invalid-numeric-groupspecifier-u.js", @"(?<42a>a)", "u", false),
        ("invalid-numeric-groupspecifier.js", @"(?<42a>a)", "", false),
        ("invalid-punctuator-starting-groupspecifier-u.js", @"(?<:a>a)", "u", false),
        ("invalid-punctuator-starting-groupspecifier.js", @"(?<:a>a)", "", false),
        ("invalid-punctuator-within-groupspecifier-u.js", @"(?<a:>a)", "u", false),
        ("invalid-punctuator-within-groupspecifier.js", @"(?<a:>a)", "", false),
        ("invalid-unterminated-groupspecifier-u.js", @"(?<aa)", "u", false),
        ("invalid-unterminated-groupspecifier.js", @"(?<aa)", "", false),
    ];

    /// <summary>
    /// The exec arrays the files assert (exec's, and String.prototype.match's without g, which is exec's), each with the
    /// file it is from: the pattern, its flags, the subject, and the array, null for no match and each undefined element
    /// null.
    /// </summary>
    internal static readonly (string File, string Pattern, string Subject, string?[]? Array)[] Arrays =
    [
        ("duplicate-names-exec.js", "(?<x>a)|(?<x>b)", "bab", ["b", null, "b"]),
        ("duplicate-names-exec.js", "(?<x>b)|(?<x>a)", "bab", ["b", "b", null]),
        ("duplicate-names-exec.js", @"(?:(?<x>a)|(?<x>b))\k<x>", "aa", ["aa", "a", null]),
        ("duplicate-names-exec.js", @"(?:(?<x>a)|(?<x>b))\k<x>", "bb", ["bb", null, "b"]),
        ("duplicate-names-exec.js", @"(?:(?:(?<x>a)|(?<x>b))\k<x>){2}", "aabb", ["aabb", null, "b"]),
        ("duplicate-names-exec.js", @"(?:(?:(?<x>a)|(?<x>b))\k<x>){2}", "abab", null),
        ("duplicate-names-exec.js", @"(?:(?<x>a)|(?<x>b))\k<x>", "abab", null),
        ("duplicate-names-exec.js", @"(?:(?<x>a)|(?<x>b))\k<x>", "cdef", null),
        ("duplicate-names-exec.js", @"^(?:(?<a>x)|(?<a>y)|z)\k<a>$", "xx", ["xx", "x", null]),
        ("duplicate-names-exec.js", @"^(?:(?<a>x)|(?<a>y)|z)\k<a>$", "z", ["z", null, null]),
        ("duplicate-names-exec.js", @"^(?:(?<a>x)|(?<a>y)|z)\k<a>$", "zz", null),
        ("duplicate-names-exec.js", @"(?<a>x)|(?:zy\k<a>)", "zy", ["zy", null]),
        ("duplicate-names-exec.js", @"^(?:(?<a>x)|(?<a>y)|z){2}\k<a>$", "xz", ["xz", null, null]),
        ("duplicate-names-exec.js", @"^(?:(?<a>x)|(?<a>y)|z){2}\k<a>$", "yz", ["yz", null, null]),
        ("duplicate-names-exec.js", @"^(?:(?<a>x)|(?<a>y)|z){2}\k<a>$", "xzx", null),
        ("duplicate-names-exec.js", @"^(?:(?<a>x)|(?<a>y)|z){2}\k<a>$", "yzy", null),
        ("duplicate-names-match.js", "(?<x>a)|(?<x>b)", "bab", ["b", null, "b"]),
        ("duplicate-names-match.js", "(?<x>b)|(?<x>a)", "bab", ["b", "b", null]),
        ("duplicate-names-match.js", @"(?:(?<x>a)|(?<x>b))\k<x>", "aa", ["aa", "a", null]),
        ("duplicate-names-match.js", @"(?:(?<x>a)|(?<x>b))\k<x>", "bb", ["bb", null, "b"]),
        ("duplicate-names-match.js", @"(?:(?:(?<x>a)|(?<x>b))\k<x>){2}", "aabb", ["aabb", null, "b"]),
        ("duplicate-names-match.js", @"(?:(?:(?<x>a)|(?<x>b))\k<x>){2}", "abab", null),
        ("duplicate-names-match.js", @"(?:(?<x>a)|(?<x>b))\k<x>", "abab", null),
        ("duplicate-names-match.js", @"(?:(?<x>a)|(?<x>b))\k<x>", "cdef", null),
        ("duplicate-names-match.js", @"^(?:(?<a>x)|(?<a>y)|z)\k<a>$", "xx", ["xx", "x", null]),
        ("duplicate-names-match.js", @"^(?:(?<a>x)|(?<a>y)|z)\k<a>$", "z", ["z", null, null]),
        ("duplicate-names-match.js", @"^(?:(?<a>x)|(?<a>y)|z)\k<a>$", "zz", null),
        ("duplicate-names-match.js", @"(?<a>x)|(?:zy\k<a>)", "zy", ["zy", null]),
        ("duplicate-names-match.js", @"^(?:(?<a>x)|(?<a>y)|z){2}\k<a>$", "xz", ["xz", null, null]),
        ("duplicate-names-match.js", @"^(?:(?<a>x)|(?<a>y)|z){2}\k<a>$", "yz", ["yz", null, null]),
        ("duplicate-names-match.js", @"^(?:(?<a>x)|(?<a>y)|z){2}\k<a>$", "xzx", null),
        ("duplicate-names-match.js", @"^(?:(?<a>x)|(?<a>y)|z){2}\k<a>$", "yzy", null),
        ("duplicate-named-groups.js", @"(?:(?:(?<a>x)|(?<a>y))\k<a>){2}", "xxyy", ["xxyy", null, "y"]),
        ("duplicate-named-groups.js", @"(?:(?:(?<a>x)|(?<a>y)|(a)|(?<b>b)|(?<a>z))\k<a>){3}", "xzzyyxxy", ["zzyyxx", "x", null, null, null, null]),
        ("duplicate-named-groups.js", @"(?:(?:(?<a>x)|(?<a>y))\k<a>){2}", "xxyy", ["xxyy", null, "y"]),
    ];

    /// <summary>The exec array of <paramref name="regex"/> on <paramref name="subject"/> from 0: the match and each group's substring or null, or null for no match.</summary>
    private static string?[]? Exec(BroilerRegex regex, string subject)
    {
        var match = regex.Match(subject);
        Assert.Equal(RegexLimit.None, match.Limit);
        return match.Success ? [.. match.Groups.Select(static g => g.Success ? g.Value : null)] : null;
    }

    /// <summary>The groups object of a match: each name, in its properties' order, with its value or null for undefined.</summary>
    private static (string Name, string? Value)[] Groups(RegexMatch match) =>
        [.. match.NamedGroups.Select(static e => (e.Key, e.Value.Success ? e.Value.Value : null))];

    [Fact]
    public void Every_tagged_file_is_judged_here_or_named_as_waiting_for_the_host_job()
    {
        Assert.Equal(19, Tagged.Length);
        Assert.Equal(Tagged.Length, Tagged.Select(static t => t.File).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(15, Tagged.Count(static t => t.Judged == Judged.Library));
        Assert.Equal(
            ["duplicate-names-replace.js", "duplicate-names-replaceall.js", "duplicate-names-split.js", "duplicate-named-groups-replace.js"],
            Tagged.Where(static t => t.Judged == Judged.Host).Select(static t => t.File[(t.File.LastIndexOf('/') + 1)..]));
        Assert.All(Arrays, static a => Assert.Contains(Tagged, t => t.File.EndsWith("/" + a.File, StringComparison.Ordinal)));
    }

    [Fact]
    public void The_duplicate_named_capturing_groups_syntax_files()
    {
        // test/built-ins/RegExp/duplicate-named-capturing-groups-syntax.js, and its Annex B twin through
        // RegExp.prototype.compile, whose own steps are the host's: the same alternative refused, separate ones parsed,
        // and the source the pattern itself.
        Assert.IsType<RegexSyntaxException>(Record.Exception(() => TestRegex.Compile("(?<x>a)(?<x>b)")));
        const string Source = "(?<x>a)|(?<x>b)";
        Assert.Equal([1, 2], TestRegex.Compile(Source).GroupNames.GroupsNamed("x").ToArray());
        Assert.Equal(RegexStatus.Success, RegexEscaper.EscapePattern(Source, 100, null, out var source));
        Assert.Equal(Source, source);
    }

    [Fact]
    public void The_exec_arrays()
    {
        foreach (var (file, pattern, subject, expected) in Arrays)
            Assert.True(Same(expected, Exec(TestRegex.Compile(pattern), subject)), file + ": /" + pattern + "/ on " + subject);
    }

    private static bool Same(string?[]? expected, string?[]? actual) =>
        expected is null ? actual is null : actual is not null && expected.SequenceEqual(actual);

    [Fact]
    public void The_groups_object_s_values_and_property_order()
    {
        // duplicate-names-exec.js and duplicate-names-match.js: groups.x after an iterated reference.
        Assert.Equal([("x", "b")], Groups(TestRegex.Compile(@"(?:(?:(?<x>a)|(?<x>b))\k<x>){2}").Match("aabb")));

        // duplicate-names-group-property-enumeration-order.js: source order, not match order.
        var order = TestRegex.Compile("(?<y>a)(?<x>a)|(?<x>b)(?<y>b)");
        Assert.Equal(["y", "x"], Enumerable.Range(0, order.GroupNames.Count).Select(order.GroupNames.NameAt));
        Assert.Equal(["y", "x"], order.Match("aa").NamedGroups.Keys);
        Assert.Equal(["y", "x"], order.Match("bb").NamedGroups.Keys);

        // exec/duplicate-named-groups-properties.js and String/prototype/match/duplicate-named-groups-properties.js.
        var matcher = TestRegex.Compile("(?:(?<x>a)|(?<y>a)(?<x>b))(?:(?<z>c)|(?<z>d))");
        Assert.Equal([("x", "b"), ("y", "a"), ("z", "c")], Groups(matcher.Match("abc")));
        Assert.Equal([("x", "a"), ("y", null), ("z", "d")], Groups(matcher.Match("ad")));
        Assert.Equal([("x", (string?)null)], Groups(TestRegex.Compile(@"(?:(?:(?<x>a)|(?<x>b)|c)\k<x>){2}").Match("aac")));
    }

    /// <summary>The indices.groups object of a match: each name with the participating group's offsets, or null.</summary>
    private static (string Name, (int, int)? Pair)[] IndicesGroups(BroilerRegex regex, string subject)
    {
        // The offset entry with the d flag's reading: the slots and the participating group of each name.
        var slots = new int[regex.SlotCount];
        Assert.Equal(RegexStatus.Match, regex.Match(subject, 0, false, slots, null, RegexMatchCeilings.Engines, out _));
        return [.. Enumerable.Range(0, regex.GroupNames.Count).Select(i =>
        {
            var name = regex.GroupNames.NameAt(i);
            var group = regex.GroupNames.ParticipatingGroup(name, slots);
            return (name, group > 0 ? (slots[2 * group], slots[(2 * group) + 1]) : ((int, int)?)null);
        })];
    }

    [Fact]
    public void The_indices_groups_object_s_values_and_property_order()
    {
        // duplicate-names-match-indices.js.
        Assert.Equal([("x", (2, 3))], IndicesGroups(TestRegex.Compile("(?<x>a)|(?<x>b)", "d"), "..ab"));
        Assert.Equal([("x", (2, 3))], IndicesGroups(TestRegex.Compile("(?<x>a)|(?<x>b)", "d"), "..ba"));

        // exec/duplicate-named-indices-groups-properties.js and its String.prototype.match twin.
        var matcher = TestRegex.Compile("(?:(?<x>a)|(?<y>a)(?<x>b))(?:(?<z>c)|(?<z>d))", "d");
        Assert.Equal([("x", (1, 2)), ("y", (0, 1)), ("z", (2, 3))], IndicesGroups(matcher, "abc"));
        Assert.Equal([("x", (0, 1)), ("y", null), ("z", (1, 2))], IndicesGroups(matcher, "ad"));
        Assert.Equal([("x", ((int, int)?)null)], IndicesGroups(TestRegex.Compile(@"(?:(?:(?<x>a)|(?<x>b)|c)\k<x>){2}", "d"), "aac"));
    }

    [Fact]
    public void Test_search_and_matchAll()
    {
        // duplicate-names-test.js.
        Assert.True(TestRegex.Compile("(?<x>a)|(?<x>b)").IsMatch("bab"));
        Assert.True(TestRegex.Compile("(?<x>b)|(?<x>a)").IsMatch("bab"));
        Assert.True(TestRegex.Compile(@"(?:(?<x>a)|(?<x>b))\k<x>").IsMatch("aa"));
        Assert.True(TestRegex.Compile(@"(?:(?<x>a)|(?<x>b))\k<x>").IsMatch("bb"));
        Assert.True(TestRegex.Compile(@"(?:(?:(?<x>a)|(?<x>b))\k<x>){2}").IsMatch("aabb"));
        Assert.False(TestRegex.Compile(@"(?:(?:(?<x>a)|(?<x>b))\k<x>){2}").IsMatch("abab"));

        // duplicate-names-search.js and staging's duplicate-named-groups-search.js: the first match's index.
        Assert.Equal(1, TestRegex.Compile("(?<x>a)|(?<x>b)").Match("xab").Index);
        Assert.Equal(1, TestRegex.Compile("(?<x>a)|(?<x>b)").Match("xba").Index);
        Assert.Equal(3, TestRegex.Compile("(?<a>x)|(?<a>y)").Match("abcxyz").Index);
        Assert.Equal(3, TestRegex.Compile("(?<a>y)|(?<a>x)").Match("abcxyz").Index);
        Assert.Equal(1, TestRegex.Compile("(?<a>x)|(?<a>y)").Match("aybcxyz").Index);
        Assert.Equal(1, TestRegex.Compile("(?<a>y)|(?<a>x)").Match("aybcxyz").Index);

        // duplicate-names-matchall.js: the global iteration's arrays.
        Assert.Equal(new[] { "b|-|b", "a|a|-", "b|-|b" }, All("(?<x>a)|(?<x>b)", "bab"));
        Assert.Equal(new[] { "b|b|-", "a|-|a", "b|b|-" }, All("(?<x>b)|(?<x>a)", "bab"));
    }

    private static string[] All(string pattern, string subject) =>
        [.. TestRegex.Compile(pattern, "g").Matches(subject).Select(static m => string.Join('|', m.Groups.Select(static g => g.Success ? g.Value : "-")))];

    [Fact]
    public void The_matches_the_host_files_read()
    {
        // The files the host job judges whole (Judged.Host): each match their assertions read, as the library answers it,
        // the substitution and the splitter being the host's.
        // duplicate-names-replace.js and duplicate-names-replaceall.js: $<x> is the participating group's value.
        foreach (var (subject, expected) in new[] { ("ab", new[] { "0:a|a|-", "1:b|-|b" }), ("ba", new[] { "0:b|-|b", "1:a|a|-" }), ("abxab", new[] { "0:a|a|-", "1:b|-|b", "3:a|a|-", "4:b|-|b" }), ("baxba", new[] { "0:b|-|b", "1:a|a|-", "3:b|-|b", "4:a|a|-" }) })
            Assert.Equal(expected, TestRegex.Compile("(?<x>a)|(?<x>b)", "g").Matches(subject).Select(static m => m.Index + ":" + m.Value + "|" + string.Join('|', m.Groups.Skip(1).Select(static g => g.Success ? g.Value : "-"))));

        // duplicate-names-split.js: "xab" and "xba" split at the matches at 1 and 2.
        var split = TestRegex.Compile("(?<x>a)|(?<x>b)");
        Assert.Equal(["a", null], Exec1(split, "xab", 1));
        Assert.Equal([null, "b"], Exec1(split, "xab", 2));
        Assert.Equal([null, "b"], Exec1(split, "xba", 1));
        Assert.Equal(["a", null], Exec1(split, "xba", 2));

        // staging's duplicate-named-groups-replace.js: the match each replacement reads, and $<a>.
        var two = TestRegex.Compile(@"(?:(?:(?<a>x)|(?<a>y))\k<a>)");
        Assert.Equal(("xx", "x"), (two.Match("xxyy").Value, two.Match("xxyy").NamedGroups["a"].Value));
        Assert.Equal(new[] { "0:xx:x", "2:yy:y" }, TestRegex.Compile(@"(?:(?:(?<a>x)|(?<a>y))\k<a>)", "g").Matches("xxyy").Select(static m => m.Index + ":" + m.Value + ":" + m.NamedGroups["a"].Value));
        var five = TestRegex.Compile(@"(?:(?:(?<a>x)|(?<a>y)|(a)|(?<b>b)|(?<a>z))\k<a>)");
        var first = five.Match("xzzyyxxy");
        Assert.Equal((1, "zz", "z"), (first.Index, first.Value, first.NamedGroups["a"].Value));
        Assert.Equal([null, null, null, null, "z"], first.Groups.Skip(1).Select(static g => g.Success ? g.Value : null));
        Assert.Equal(new[] { "1:zz:z", "3:yy:y", "5:xx:x" }, TestRegex.Compile(five.Pattern, "g").Matches("xzzyyxxy").Select(static m => m.Index + ":" + m.Value + ":" + m.NamedGroups["a"].Value));
    }

    /// <summary>The captures of a sticky match at <paramref name="at"/>, as split's splitter reads them, each null for undefined.</summary>
    private static string?[] Exec1(BroilerRegex regex, string subject, int at)
    {
        var sticky = TestRegex.Compile(regex.Pattern, "y");
        var match = sticky.Match(subject, at);
        Assert.True(match.Success);
        return [.. match.Groups.Skip(1).Select(static g => g.Success ? g.Value : null)];
    }

    [Fact]
    public void The_literal_syntax_tests_of_the_named_groups_directory()
    {
        foreach (var (file, pattern, flags, accepted) in Literals)
        {
            var exception = Record.Exception(() => TestRegex.Compile(pattern, flags));
            Assert.True(accepted ? exception is null : exception is RegexSyntaxException, file + ": " + (exception?.GetType().Name ?? "accepted"));
        }

        // forward-reference.js: a reference before its group is accepted and matches.
        Assert.True(TestRegex.Compile(@"\k<a>(?<a>x)").IsMatch("x"));

        // invalid-lone-surrogate-groupname.js: a raw lone surrogate in a name, with and without u (a table, not theory
        // data, as a lone surrogate does not survive xunit's serialization of an argument).
        foreach (var (pattern, flags) in new[] { ("(?<a\uD801>.)", ""), ("(?<a\uDCA4>.)", ""), ("(?<a\uD801>.)", "u"), ("(?<a\uDCA4>.)", "u") })
            Assert.IsType<RegexSyntaxException>(Record.Exception(() => TestRegex.Compile(pattern, flags)));
    }

    [Fact]
    public void The_property_names_invalid_files_of_the_built_ins_directory()
    {
        // non-unicode-property-names-invalid.js and unicode-property-names-invalid.js: each pattern a RegExp is given,
        // its JavaScript string escapes read, refused without u and with it.
        foreach (var flags in new[] { "", "u" })
        {
            foreach (var pattern in new[] { "(?<\U0001F98A>fox)", "(?<\U0001F98A>fox)", "(?<🦊>fox)", "(?<\U0001F415>dog)", "(?<\U0001F415>dog)", "(?<\uD83D \uDC15>dog)", "(?<\U0001D7DAthe>the)" })
                Assert.IsType<RegexSyntaxException>(Record.Exception(() => TestRegex.Compile(pattern, flags)));
        }
    }
}
