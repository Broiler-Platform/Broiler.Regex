using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The ClassSetCharacters of a <c>v</c>-mode <c>\q{…}</c> (ECMA-262 §22.2.1 ClassString): each denotes one code point,
/// a lone surrogate as well as any other, and an alternative is the sequence of them (§22.2.2.10
/// CompileClassSetString), matched code point by code point against a subject read by code point (§22.2.2.7
/// CompileAtom, §22.2.7.2 RegExpBuiltinExec).
/// </summary>
/// <remarks>
/// Until the integration of tasks 2.6 and 2.11 the parser inferred an alternative's code points from its UTF-16 text.
/// A lone surrogate written alone reached <c>char.ConvertToUtf32</c>, whose <c>ArgumentException</c> escaped
/// <c>Compile</c> and the constructor: the fuzz target's finding at seed 20261027, iteration 280, <c>/[\q{\uD800}]/v</c>
/// and <c>/[\q{a|\uDE00}]/v</c>. A lead surrogate written before a trail one, as in <c>\uD83D\u{DE00}</c>, two
/// ClassSetCharacters, read as the one code point U+1F600 they are not, so the class matched U+1F600 and set operations
/// took the alternative for that code point. And the alternative's characters were read by a grammar of its own, which
/// took a reserved double punctuator, as in <c>\q{a&amp;&amp;b}</c>, and refused <c>\b</c>. The alternatives are now read
/// by the class-set operand's own reading of a ClassSetCharacter, as code points.
/// </remarks>
public class ClassStringCodePointTests
{
    /// <summary>The string-bearing answer through the constructor, after <c>Compile</c> has answered <c>Success</c> for the same pattern.</summary>
    private static string Answer(string pattern, string flags, string subject)
    {
        var parsed = RegexFlagsFromText(flags);
        Assert.Equal(RegexStatus.Success, TestRegex.TryCompile(pattern, parsed, UnicodeTables.Instance, 512, out var compiled, out _));
        var answer = TestRegex.Answer(TestRegex.Compile(pattern, flags), subject, 0);
        Assert.Equal(answer, TestRegex.Answer(compiled!, subject, 0));
        return answer;
    }

    private static RegexFlags RegexFlagsFromText(string flags) =>
        RegexFlags.UnicodeSets | (flags.Contains('i') ? RegexFlags.IgnoreCase : RegexFlags.None);

    private static void AssertAll((string Pattern, string Flags, string Subject, string Expected)[] cases)
    {
        foreach (var (pattern, flags, subject, expected) in cases)
        {
            var answer = Answer(pattern, flags, subject);
            Assert.True(expected == answer, $"/{pattern}/{flags} against {Escaped(subject)}: expected {expected}, got {answer}");
        }
    }

    private static string Escaped(string text)
    {
        var escaped = new System.Text.StringBuilder();
        foreach (var unit in text)
            escaped.Append(unit is >= ' ' and <= '~' ? unit.ToString() : "\\u" + ((int)unit).ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
        return escaped.ToString();
    }

    [Fact]
    public void A_lone_surrogate_alone_in_an_alternative_is_that_code_point()
        => AssertAll(
        [
            (@"[\q{\uD800}]", "v", "\uD800", "match 0,1"),
            (@"[\q{\uD800}]", "v", "a\uD800b", "match 1,2"),
            (@"[\q{\uD800}]", "v", "𐀀", "nomatch"),       // the subject's pair is one code point, U+10000
            (@"[\q{a|\uDE00}]", "v", "\uDE00", "match 0,1"),
            (@"[\q{a|\uDE00}]", "v", "😀", "nomatch"),
            (@"[\q{\u{DBFF}}]", "v", "\uDBFF", "match 0,1"),         // a braced escape
            ("[\\q{\uD800}]", "v", "\uD800", "match 0,1"),           // written raw
            (@"[\q{\uD800}]+", "v", "\uD800\uD800", "match 0,2"),
            (@"(?<=[\q{\uD800}])a", "v", "\uD800a", "match 1,2"),    // read right to left
            (@"[^\q{\uD800}]", "v", "\uD800", "nomatch"),            // one ClassSetCharacter may be complemented
            (@"[^\q{\uD800}]", "v", "a", "match 0,1"),
        ]);

    /// <summary>
    /// U+10000 and U+1F600, each written as one RegExpUnicodeEscapeSequence of a lead and a trail surrogate (§22.2.1),
    /// the escape that denotes the pair's one code point. Each is written in two halves, so that the source holds the
    /// escape's ASCII text: an editing tool that joins an escaped pair into the character it denotes once turned the
    /// cases that use them into cases of a raw pair.
    /// </summary>
    private const string EscapedU10000 = @"\uD800" + @"\uDC00";

    private const string EscapedU1F600 = @"\uD83D" + @"\uDE00";

    [Fact]
    public void A_lone_surrogate_beside_a_pair_is_its_own_code_point()
        => AssertAll(
        [
            (@"[\q{\uD800|" + EscapedU10000 + "}]", "v", "\uD800", "match 0,1"),
            (@"[\q{\uD800|" + EscapedU10000 + "}]", "v", "\U00010000", "match 0,2"),  // one escape of a lead and a trail: U+10000
            ("[\\q{\\uD800|\U00010000}]", "v", "\uD800", "match 0,1"),                // the pair written raw
            ("[\\q{\\uD800|\U00010000}]", "v", "\U00010000", "match 0,2"),
            (@"[\q{\uD800x|\u{10000}}]", "v", "\uD800x", "match 0,2"),
            (@"[\q{\uDC00\uD800}]", "v", "\uDC00\uD800", "match 0,2"),       // a trail, then a lead: two code points in the subject too
            (@"[\q{\uDC00\uD800}]", "v", "𐀀", "nomatch"),
            (@"[\q{a\uD800}]", "v", "a\uD800", "match 0,2"),
            (@"[\q{a\uD800}]", "v", "a𐀀", "nomatch"),             // the subject's lead is half of a pair
            (@"[\q{" + EscapedU1F600 + "}]", "v", "\U0001F600", "match 0,2"),        // one escape: U+1F600
            (@"[\q{x" + EscapedU1F600 + "|y}]", "v", "x\U0001F600", "match 0,3"),
            (@"[^\q{" + EscapedU1F600 + "}]", "v", "\U0001F600", "nomatch"),         // one ClassSetCharacter may be complemented
            (@"[^\q{" + EscapedU1F600 + "}]", "v", "a", "match 0,1"),
            ("[\\q{\U0001F600}]", "v", "\U0001F600", "match 0,2"),                     // the pair written raw
        ]);

    [Fact]
    public void A_lead_written_before_a_trail_is_two_code_points_which_no_subject_holds_in_a_row()
        => AssertAll(
        [
            // Two ClassSetCharacters, U+D83D then U+DE00. A subject read by code point holds U+1F600 where its text holds
            // that pair, never U+D83D followed by U+DE00, so the alternative matches nothing. V8 matches U+1F600 here.
            (@"[\q{\uD83D\u{DE00}}]", "v", "😀", "nomatch"),
            (@"[\q{\u{D83D}\uDE00}]", "v", "😀", "nomatch"),
            ("[\\q{\uD83D\\uDE00}]", "v", "😀", "nomatch"),       // a raw lead, then an escaped trail
            (@"[\q{\uD83D\u{DE00}|x}]", "v", "😀x", "match 2,3"),
            (@"[\q{x\uD83D\u{DE00}}]", "v", "x😀", "nomatch"),
            (@"[\q{\uD83D\u{DE00}}]", "vi", "😀", "nomatch"),
        ]);

    [Fact]
    public void Set_operations_compare_an_alternative_s_code_points_not_its_text()
        => AssertAll(
        [
            (@"[\q{\uD800}--\uD800]", "v", "\uD800", "nomatch"),
            (@"[\q{\uD800}--\q{\uD800}]", "v", "\uD800", "nomatch"),
            (@"[\q{\uD800|ab}&&\uD800]", "v", "\uD800", "match 0,1"),
            (@"[\q{\uDE00}&&[\uDC00-\uDFFF]]", "v", "\uDE00", "match 0,1"),
            (@"[[\uDC00-\uDFFF]--\q{\uDE00}]", "v", "\uDE00", "nomatch"),
            (@"[[\uDC00-\uDFFF]--\q{\uDE00}]", "v", "\uDE01", "match 0,1"),
            // U+D83D then U+DE00 is not U+1F600: an intersection with it is empty, and subtracting it removes nothing.
            (@"[\q{\uD83D\u{DE00}}&&\q{\u{1F600}}]", "v", "😀", "nomatch"),
            (@"[\q{\u{1F600}}--\q{\uD83D\u{DE00}}]", "v", "😀", "match 0,2"),
            (@"[\q{x\uD83D\u{DE00}}&&\q{x\u{1F600}}]", "v", "x😀", "nomatch"),
            (@"[\q{x\u{1F600}}--\q{x\uD83D\u{DE00}}]", "v", "x😀", "match 0,3"),
            // One escape of a lead and a trail is the one code point U+1F600, as \u{1F600} is.
            (@"[\q{" + EscapedU1F600 + @"}&&\q{\u{1F600}}]", "v", "\U0001F600", "match 0,2"),
            (@"[\q{" + EscapedU1F600 + @"}--\q{\u{1F600}}]", "v", "\U0001F600", "nomatch"),
            (@"[\q{x" + EscapedU1F600 + @"}&&\q{x\u{1F600}}]", "v", "x\U0001F600", "match 0,3"),
            (@"[\q{x" + EscapedU1F600 + @"}--\q{x\u{1F600}}]", "v", "x\U0001F600", "nomatch"),
        ]);

    [Fact]
    public void A_lone_surrogate_in_an_alternative_folds_to_itself_under_vi()
        => AssertAll(
        [
            (@"[\q{\uD800}]", "vi", "\uD800", "match 0,1"),
            (@"[\q{\uDE00}]", "vi", "\uDE00", "match 0,1"),
            (@"[\q{A\uD800}]", "vi", "a\uD800", "match 0,2"),
            (@"[\q{a\uD800}]", "vi", "A𐀀", "nomatch"),
            (@"[\q{\uD800}&&\uD800]", "vi", "\uD800", "match 0,1"),
            (@"[\q{\uD800|K}--k]", "vi", "\uD800", "match 0,1"),
            (@"[\q{" + EscapedU1F600 + "}]", "vi", "\U0001F600", "match 0,2"),
        ]);

    [Fact]
    public void An_alternative_reads_each_ClassSetCharacter_as_a_class_set_operand_does()
    {
        AssertAll(
        [
            (@"[\q{\b}]", "v", "\b", "match 0,1"),                    // \b is U+0008 in a ClassString as in any class
            (@"[\q{a\b}]", "v", "a\b", "match 0,2"),
            (@"[\q{a&b}]", "v", "a&b", "match 0,3"),                  // a lone '&' is a ClassSetCharacter
            (@"[\q{\&\&}]", "v", "&&", "match 0,2"),                  // an escaped reserved punctuator is itself
        ]);

        // ClassSetCharacter :: [lookahead ∉ ClassSetReservedDoublePunctuator] SourceCharacter but not ClassSetSyntaxCharacter.
        foreach (var pattern in new[] { @"[\q{a&&b}]", @"[\q{!!}]", @"[\q{a^^}]", @"[\q{a~~}]", @"[\q{$$|a}]" })
        {
            Assert.Throws<RegexSyntaxException>(() => TestRegex.Compile(pattern, "v"));
            Assert.Throws<RegexSyntaxException>(() => TestRegex.TryCompile(pattern, RegexFlags.UnicodeSets, UnicodeTables.Instance, 512, out _, out _));
        }
    }
}
