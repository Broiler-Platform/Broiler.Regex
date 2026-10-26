using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The old backend's defects the differential comparison of task 2.4b found (<see cref="DifferentialTests.Defects"/>), D2
/// and JSD-0081's row M7, each pinned to the specification's answer: the new backend gives it, through the offset entry
/// and, since task 2.4c, through the public string-bearing members, and the old backend, kept on an internal route until
/// task 2.4c deletes it, is shown to give another. The roadmap's record of task 2.4b names each. Expected answers agree
/// with V8's (Node 22). Defect D1's rows are <see cref="SurrogateStartTests"/>'.
/// </summary>
public class OldBackendDefectTests
{
    /// <summary>The rows of defect D2: a <c>v</c>-mode class holding the empty string and single characters.</summary>
    public static TheoryData<string, string, string, int, string, string> D2Rows => new()
    {
        { "[\\q{}a]", "v", "a", 0, "match 0,1", "match 0,0" },
        { "[\\q{ab|}c]", "v", "c", 0, "match 0,1", "match 0,0" },
        { "([\\q{}a])", "vi", "A", 0, "match 0,1 0,1", "match 0,0 0,0" },
        { "(?<=([\\q{}a]))", "v", "a", 1, "match 1,1 0,1", "match 1,1 1,1" },
        { "[\\q{}\\p{L}]", "v", "x", 0, "match 0,1", "match 0,0" },
    };

    [Theory]
    [MemberData(nameof(D2Rows))]
    public void D2_a_class_s_empty_string_is_tried_after_its_single_characters(string pattern, string flags, string subject, int start, string specification, string old)
    {
        // CompileAtom for a v-mode class with strings (ECMA-262 §22.2.2.7, Atom :: CharacterClass): the strings of more
        // than one character, longest first, then the CharacterSetMatcher of the single characters, then the EmptyMatcher
        // when the class holds the empty string. The old backend's class tries its strings, the empty one among them, before
        // its single characters, so the empty string wins where a character would.
        _ = old;
        Assert.Equal(specification, MachineTests.Run(pattern, flags, subject, start));
        Assert.Equal(specification, TestRegex.Answer(TestRegex.Compile(pattern, flags), subject, start));
    }

    [Fact]
    public void M7_a_backward_back_reference_reads_whole_code_points_under_u()
    {
        // JSD-0081's row M7, Broiler.Regex 0.1.0-preview.4's defect that decision D36 corrects in SJL-11.2: under u a
        // back-reference in a look-behind compares the code points that end at the index (§22.2.2.7.2, f = e - len over
        // Input, a List of code points), so it never begins inside a surrogate pair. The old backend compares the code units
        // that end there, from e minus the capture's length in units, and so matches a lone trail surrogate captured on
        // its own against the trail half of a pair. Lone surrogates do not survive xunit's theory data, so the rows are here.
        Assert.Equal("nomatch", MachineTests.Run("(?<=\\1(.))x", "u", "\uD83D\uDE00\uDE00x"));
        Assert.Equal("nomatch", MachineTests.Run("(?<=\\1bar(.+))foo", "u", "\uD834\uDC00bar\uDC00foo"));
        Assert.Equal("match 3,4 2,3", MachineTests.Run("(?<=\\1(.))x", "", "\uD83D\uDE00\uDE00x"));
        Assert.Equal("nomatch", TestRegex.Answer(TestRegex.Compile("(?<=\\1(.))x", "u"), "\uD83D\uDE00\uDE00x", 0));
        Assert.Equal("nomatch", TestRegex.Answer(TestRegex.Compile("(?<=\\1bar(.+))foo", "u"), "\uD834\uDC00bar\uDC00foo", 0));
        Assert.Equal("match 3,4 2,3", TestRegex.Answer(TestRegex.Compile("(?<=\\1(.))x"), "\uD83D\uDE00\uDE00x", 0));
    }

    [Fact]
    public void M7_the_old_backend_compares_the_code_units_that_end_at_the_index()
    {
        // The defect as it stands, so that the record is of an answer the old backend gives; task 2.4c deletes it.
        Assert.Equal("match 3,4 2,3", DifferentialTests.Old(DifferentialTests.OldBackend("(?<=\\1(.))x", "u"), "\uD83D\uDE00\uDE00x", 0));
        Assert.Equal("match 6,9 5,6", DifferentialTests.Old(DifferentialTests.OldBackend("(?<=\\1bar(.+))foo", "u"), "\uD834\uDC00bar\uDC00foo", 0));
    }

    [Theory]
    [MemberData(nameof(D2Rows))]
    public void D2_the_old_backend_answers_with_the_empty_string_first(string pattern, string flags, string subject, int start, string specification, string old)
    {
        // The defect as it stands, so that the record is of an answer the old backend gives; task 2.4c deletes it.
        Assert.NotEqual(specification, old);
        Assert.Equal(old, DifferentialTests.Old(DifferentialTests.OldBackend(pattern, flags), subject, start));
    }
}
