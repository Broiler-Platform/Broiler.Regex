using System;
using System.Linq;
using Broiler.Regex.Engine;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// A start inside a surrogate pair under <c>u</c> and <c>v</c>: the rows on which task 2.4a's differential test found
/// the two backends disagreeing (the old backend's defect D1), read against what the specification says of such a
/// start, and the answers of the offset entry and of the string-bearing members, which give the specification's.
/// </summary>
/// <remarks>
/// <para>
/// <b>What the specification says.</b> Under <c>u</c> and <c>v</c> the matchers of ECMA-262 §22.2.2 read a list of code
/// points, so a code-unit index inside a pair is no index of their input. The one meaning the specification gives such
/// an index is RegExpBuiltinExec's <c>lastIndex</c> (§22.2.7.2): step 13.b starts the matcher at "the character that was
/// obtained from element lastIndex of S", the whole pair, and steps 22 and 24 report <c>lastIndex</c> itself as the
/// match's <c>"index"</c> and start (an empty match there is decision D41 of Broiler.VM's JSD-0081, which no row of
/// <see cref="Rows"/> is and <see cref="EmptyRows"/> holds). <see cref="Rows"/> holds RegExpBuiltinExec's answers,
/// <c>lastIndex</c> the start and the <c>g</c> flag set.
/// </para>
/// <para>
/// <b>The entry and its caller.</b> <see cref="RegexProgram.Match"/>, and the public offset entry over it, take a
/// code-unit start and read forward from it: from inside a pair they read the low half as a lone unit. That is the
/// entry's contract, not RegExpBuiltinExec's answer; the step back to the pair is the caller's, as Broiler.VM's realm
/// takes it before calling its own matcher, and with it the entry answers every row as RegExpBuiltinExec does.
/// SJL-11.4's and SJL-11.5's adapters take it for their engines.
/// </para>
/// <para>
/// <b>The string-bearing members</b> take the step themselves since task 2.4c and report a non-empty match found at the
/// pair at the caller's start (row M4, decision D23): every row of <see cref="Rows"/> is pinned on them. The old backend
/// they used before was inconsistent here, its defect D1: its greedy run of one character gave back across the start,
/// reading the high half before it, and its general loop did not, so rows 0 and 1 met RegExpBuiltinExec's answer by the
/// run, while row 2, the same pattern written as a loop, and row 4 did not.
/// </para>
/// </remarks>
public class SurrogateStartTests
{
    /// <summary>The rows: a pattern, its flags, a subject, a <c>lastIndex</c> inside a pair, and RegExpBuiltinExec's answer.</summary>
    internal static readonly (string Pattern, string Flags, string Subject, int LastIndex, string Exec)[] Rows =
    [
        ("[\\s\\S]*\\u{1F600}", "u", "\U0001F600ba", 1, "match 1,2"),
        ("[\\s\\S]?\\u{1F600}", "v", "\U0001F600ba", 1, "match 1,2"),
        ("(?:[\\s\\S])*\\u{1F600}", "u", "\U0001F600ba", 1, "match 1,2"),
        ("[\\s\\S]?[^\\u{1F600}]", "u", "\uD83D\U0001F600", 2, "nomatch"),
        ("(.{2})?\\u{1F600}|()", "u", "\U0001F600bcx", 1, "match 1,2 - -"),
        ("(.)*", "u", "\U0001F600bc", 1, "match 1,4 3,4"),
    ];

    /// <summary>
    /// RegExpBuiltinExec over the new backend's entry from <paramref name="lastIndex"/>: the step back of step 13.b to the
    /// pair's first unit, the entry's search from there, and steps 22 and 24's <c>"index"</c>, <paramref name="lastIndex"/>
    /// itself when the first attempt matched.
    /// </summary>
    internal static string Exec(string pattern, string flags, string subject, int lastIndex)
    {
        var program = TestProgram.Compile(pattern, flags);
        var from = lastIndex;
        if (program.Unicode && from > 0 && from < subject.Length && char.IsLowSurrogate(subject[from]) && char.IsHighSurrogate(subject[from - 1]))
            from--;

        var slots = new int[program.SlotCount];
        var status = program.Match(subject, from, false, slots, null, MachineTests.Ample, out _, out _);
        if (status == RegexStatus.Match && slots[0] == from)
            slots[0] = lastIndex;
        return MachineTests.Render(status, slots);
    }

    [Theory]
    [InlineData(0, "nomatch")]
    [InlineData(1, "nomatch")]
    [InlineData(2, "nomatch")]
    [InlineData(3, "match 2,3")]
    [InlineData(4, "match 1,1 - 1,1")]
    [InlineData(5, "match 1,4 3,4")]
    public void The_entry_reads_a_start_inside_a_pair_forward_from_its_low_half_as_a_lone_unit(int row, string entry)
    {
        // The entry's contract: the pair is split for the call, and none of these patterns reads backward, so neither the
        // match nor a capture begins before the start (a look-behind's reads are MachineLookaroundTests').
        var (pattern, flags, subject, lastIndex, _) = Rows[row];
        Assert.Equal(entry, MachineTests.Run(pattern, flags, subject, lastIndex));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void With_the_caller_s_step_back_the_entry_answers_as_RegExpBuiltinExec_does(int row)
    {
        var (pattern, flags, subject, lastIndex, exec) = Rows[row];
        Assert.Equal(exec, Exec(pattern, flags, subject, lastIndex));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void The_public_members_answer_as_RegExpBuiltinExec_does_from_a_start_inside_a_pair(int row)
    {
        // Defect D1's rows pinned to the specification's answer on the new backend: Match steps back to the pair, and a
        // match found there reports the caller's start as its index and its substring from there; IsMatch agrees.
        var (pattern, flags, subject, lastIndex, exec) = Rows[row];
        var regex = TestRegex.Compile(pattern, flags);
        Assert.Equal(exec, TestRegex.Answer(regex, subject, lastIndex));
        Assert.Equal(exec != "nomatch", regex.IsMatch(subject, lastIndex));
        var match = regex.Match(subject, lastIndex);
        if (match.Success)
            Assert.Equal(subject[match.Index..(match.Index + match.Length)], match.Value);
    }

    /// <summary>
    /// JSD-0081's row M4, the empty match: a pattern, its flags, a subject and a <c>lastIndex</c> inside a pair from
    /// which the step back finds an empty match at the pair's first unit, and the two answers decision D41 admits, the
    /// pair's start (Broiler.VM's realm) and <c>lastIndex</c> (Broiler.JS).
    /// </summary>
    /// <remarks>
    /// RegExpBuiltinExec's record of such a match is ill-formed: its <c>"index"</c> is <c>lastIndex</c> (steps 22 and
    /// 24) and its end the matcher's, the pair's first unit, before it, which GetMatchString's assertion forbids.
    /// Decision D41 classes the case implementation-defined, this case only, until the text is corrected.
    /// </remarks>
    internal static readonly (string Pattern, string Flags, string Subject, int LastIndex, string PairStart, string LastIndexAnswer)[] EmptyRows =
    [
        ("(?:)", "u", "\U0001F600", 1, "match 0,0", "match 1,1"),
        ("a*", "u", "\U0001F600b", 1, "match 0,0", "match 1,1"),
        ("(?=)", "v", "\U0001F600", 1, "match 0,0", "match 1,1"),
        ("()", "u", "\U0001F600b", 1, "match 0,0 0,0", "match 1,1 1,1"),
        ("^", "u", "\U0001F600", 1, "match 0,0", "match 1,1"),
        ("(?:)", "uy", "\U0001F600", 1, "match 0,0", "match 1,1"),
    ];

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void The_public_members_report_an_empty_match_from_a_start_inside_a_pair_at_the_pair_s_start(int row)
    {
        // Decision D41's case: the library's answer is the pair's start, where the match was found, Broiler.VM's realm's
        // answer; Match and IsMatch agree, and the record is well-formed, an empty match that neither ends before it
        // starts nor throws.
        var (pattern, flags, subject, lastIndex, pairStart, lastIndexAnswer) = EmptyRows[row];
        var regex = TestRegex.Compile(pattern, flags);
        Assert.Equal(pairStart, TestRegex.Answer(regex, subject, lastIndex));
        Assert.NotEqual(lastIndexAnswer, pairStart);
        Assert.True(regex.IsMatch(subject, lastIndex));
        Assert.Equal("", regex.Match(subject, lastIndex).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void The_offset_entry_gives_an_engine_what_its_own_answer_to_decision_D41_needs(int row)
    {
        // An engine that steps back itself gets the matcher's raw offsets from the pair and keeps its own lastIndex, so it
        // can give either answer decision D41 admits: the pair's start as the offsets say, or lastIndex.
        var (pattern, flags, subject, lastIndex, pairStart, lastIndexAnswer) = EmptyRows[row];
        var regex = TestRegex.Compile(pattern, flags);
        var slots = new int[regex.SlotCount];
        var anchored = flags.Contains('y');
        Assert.Equal(RegexStatus.Match, regex.Match(subject.AsSpan(), lastIndex - 1, anchored, slots, null, RegexMatchCeilings.Engines, out _));
        Assert.Equal(pairStart, MachineTests.Render(RegexStatus.Match, slots));
        Assert.Equal(lastIndexAnswer, MachineTests.Render(RegexStatus.Match, slots.Select(offset => offset == lastIndex - 1 ? lastIndex : offset).ToArray()));
    }
}
