using Broiler.Regex.Engine;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// A start inside a surrogate pair under <c>u</c> and <c>v</c>: the rows on which task 2.4a's differential test found
/// the two backends disagreeing (defect D1), read against what the specification says of such a start.
/// </summary>
/// <remarks>
/// <para>
/// <b>What the specification says.</b> Under <c>u</c> and <c>v</c> the matchers of ECMA-262 §22.2.2 read a list of code
/// points, so a code-unit index inside a pair is no index of their input. The one meaning the specification gives such
/// an index is RegExpBuiltinExec's <c>lastIndex</c> (§22.2.7.2): step 13.b starts the matcher at "the character that was
/// obtained from element lastIndex of S", the whole pair, and steps 22 and 24 report <c>lastIndex</c> itself as the
/// match's <c>"index"</c> and start (an empty match there is decision D41 of Broiler.VM's JSD-0081, which neither row
/// below is). <see cref="Rows"/> holds RegExpBuiltinExec's answers, <c>lastIndex</c> the start and the <c>g</c> flag set.
/// </para>
/// <para>
/// <b>The new backend's entry and its caller.</b> <see cref="RegexProgram.Match"/> takes a code-unit start and reads
/// forward from it: from inside a pair it reads the low half as a lone unit and never reads before the start. That is
/// the entry's contract, not RegExpBuiltinExec's answer; the step back to the pair is the caller's, as Broiler.VM's realm
/// takes it before calling its own matcher, and with it the entry answers every row as RegExpBuiltinExec does. Task 2.4c,
/// which routes <see cref="BroilerRegex"/> through the entry, takes that step and decides the index the public members
/// report; SJL-11.4's and SJL-11.5's adapters take it for their engines.
/// </para>
/// <para>
/// <b>The old backend, which the public members still use.</b> Its greedy run of one character gives back across the
/// start, reading the high half before it, and its general loop does not: rows 0 and 1 meet RegExpBuiltinExec's answer
/// by the run, while row 2, the same pattern written as a loop, and row 4 do not. That inconsistency is defect D1.
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
    [InlineData(2, Skip = "The old backend's defect D1: its loop does not step back across a start inside a pair, and answers no match. Task 2.4c's route through the new backend takes the step back.")]
    [InlineData(3)]
    [InlineData(4, Skip = "The old backend's defect D1: it reads the low half as a lone unit and answers match 1,1 - 1,1. Task 2.4c's route through the new backend takes the step back.")]
    [InlineData(5)]
    public void The_public_members_answer_as_RegExpBuiltinExec_does_from_a_start_inside_a_pair(int row)
    {
        var (pattern, flags, subject, lastIndex, exec) = Rows[row];
        Assert.Equal(exec, DifferentialTests.Old(TestRegex.Compile(pattern, flags), subject, lastIndex));
    }
}
