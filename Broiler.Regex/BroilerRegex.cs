// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   6
// Annotated:        6/6
// Exempt:           5
// Human-reviewed:   0/6
// IP risk:          Low
// Security risk:    High
// Criteria:         6/6
// Resource impact:  8/10 max
// Unverified:       6
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Collections.Generic;
using Broiler.Regex.Matching;
using Broiler.Regex.Parsing;

namespace Broiler.Regex;

/// <summary>
/// A compiled ECMAScript regular expression. The public entry point of
/// Broiler.Regex: parse a pattern + flags once, then match many inputs.
/// </summary>
/// <remarks>
/// Compilation is reusable and the compiled form is immutable, so a single
/// instance may be matched concurrently from multiple threads (each match
/// attempt allocates its own state and step budget).
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=1B83F8
// Broiler-Falsified-If: two threads making their first Match calls at the same time on one instance built from [zab] both normalize the class's out-of-order range list in place, and one throws or misses a match that a single thread finds
// Broiler-Human:        PENDING
public sealed class BroilerRegex
{
    private readonly Matcher _matcher;

    public string Pattern { get; }
    public RegexFlags Flags { get; }

    /// <summary>Number of capturing groups in the pattern.</summary>
    public int CaptureCount { get; }

    /// <summary>Group name → 1-based capture index.</summary>
    public IReadOnlyDictionary<string, int> GroupNames { get; }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=BD448B
    // Broiler-Falsified-If: a flags string with a repeated letter such as "gg" produces an instance instead of throwing RegexSyntaxException
    // Broiler-Human:        PENDING
    public BroilerRegex(string pattern, string? flags = null)
        : this(pattern, RegexFlagsParser.Parse(flags))
    {
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=7; Fingerprint=7C6C9D
    // Broiler-Falsified-If: a pattern of 50,000 nested opening parentheses ends the process with a stack overflow inside RegexParser.Parse instead of raising a catchable exception
    // Broiler-Human:        PENDING
    public BroilerRegex(string pattern, RegexFlags flags)
    {
        Pattern = pattern ?? "";
        Flags = flags;

        var ast = RegexParser.Parse(Pattern, flags, out var captureCount, out var groupNames);
        CaptureCount = captureCount;
        GroupNames = groupNames;
        _matcher = new Matcher(ast, captureCount, flags, groupNames);
    }

    /// <summary>
    /// Finds the first match at or after <paramref name="start"/> (anchored at
    /// exactly <paramref name="start"/> when the <c>y</c> flag is set). Returns
    /// <see cref="RegexMatch.Empty"/> when there is no match.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=EBD5A7
    // Broiler-Falsified-If: Match(input, -1) does not give the result that Match(input, 0) gives, so a negative start is not clamped to 0
    // Broiler-Human:        PENDING
    public RegexMatch Match(string input, int start = 0)
    {
        if (start < 0) start = 0;
        if (start > input.Length)
            return RegexMatch.Empty;
        return _matcher.Run(input, start) ?? RegexMatch.Empty;
    }

    /// <summary>True when the pattern matches anywhere at or after <paramref name="start"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=High; Resources=8; Fingerprint=FC8D95
    // Broiler-Falsified-If: IsMatch(input, start) returns a value other than Match(input, start).Success for the same input and start
    // Broiler-Human:        PENDING
    public bool IsMatch(string input, int start = 0) => Match(input, start).Success;

    /// <summary>
    /// Enumerates all non-overlapping matches, advancing past empty matches by one
    /// code point (the <c>g</c>-flag iteration of §22.2.6.9 / §22.2.6.13).
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=8; Fingerprint=5971A9
    // Broiler-Falsified-If: after an empty match just before a surrogate pair in u or v mode, the next match starts at the pair's low surrogate
    // Broiler-Human:        PENDING
    public IEnumerable<RegexMatch> Matches(string input)
    {
        var pos = 0;
        while (pos <= input.Length)
        {
            var m = _matcher.Run(input, pos);
            if (m == null)
                yield break;

            yield return m;

            var next = m.Index + m.Length;
            if (m.Length == 0)
            {
                next = m.Index + 1;
                if (Flags.IsUnicodeMode() && m.Index < input.Length
                    && char.IsHighSurrogate(input[m.Index])
                    && m.Index + 1 < input.Length && char.IsLowSurrogate(input[m.Index + 1]))
                    next++;
            }
            pos = next;
        }
    }
}
