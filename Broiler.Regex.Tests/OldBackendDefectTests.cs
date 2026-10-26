using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The old backend's defects the differential comparison of task 2.4b found (<see cref="DifferentialTests.Defects"/>),
/// each pinned to the specification's answer: the new backend gives it, and the public members, which run the old
/// backend until task 2.4c routes them through the new one, are shown to give another. The roadmap's record of task 2.4b
/// names each. Expected answers agree with V8's (Node 22).
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
    }

    [Theory]
    [MemberData(nameof(D2Rows))]
    public void D2_the_old_backend_answers_with_the_empty_string_first(string pattern, string flags, string subject, int start, string specification, string old)
    {
        // The defect as it stands, so that the record is of an answer the old backend gives; task 2.4c deletes it.
        Assert.NotEqual(specification, old);
        Assert.Equal(old, DifferentialTests.Old(TestRegex.Compile(pattern, flags), subject, start));
    }
}
