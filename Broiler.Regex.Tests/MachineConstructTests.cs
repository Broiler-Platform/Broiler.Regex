using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The new backend's constructs of task 2.4b against ECMA-262's Pattern semantics (§22.2.2): characters and classes
/// compared under <c>i</c> by both branches of Canonicalize (§22.2.2.7.3), the assertions <c>^ $ \b \B</c>
/// (§22.2.2.6), the modifier groups <c>(?ims-ims:…)</c>, whose flags are in force for their body alone, numbered and
/// named back-references (§22.2.2.7.2), and the <c>v</c>-mode classes that hold strings (§22.2.2.7). Every expected answer is the specification's, read from the section each theory names, in the form
/// <see cref="MachineTests.Render"/> writes.
/// </summary>
public class MachineConstructTests
{
    [Theory]
    [InlineData("a", "i", "A", "match 0,1")]
    [InlineData("A", "i", "xa", "match 1,2")]
    [InlineData("a", "", "A", "nomatch")]
    [InlineData("é", "i", "É", "match 0,1")]
    [InlineData("é", "iu", "É", "match 0,1")]
    [InlineData("\\u{10400}", "iu", "\U00010428", "match 0,2")]
    [InlineData("\\u{10428}", "iv", "\U00010400", "match 0,2")]
    [InlineData("𐐀", "i", "\U00010428", "nomatch")]
    [InlineData("[\\u{10400}]", "iu", "\U00010428", "match 0,2")]
    [InlineData("θ", "iu", "ϑ", "match 0,1")]
    [InlineData("θ", "i", "ϑ", "match 0,1")]
    [InlineData("ẞ", "iu", "ß", "match 0,1")]
    [InlineData("ẞ", "i", "ß", "nomatch")]
    public void A_character_under_i_matches_every_character_of_the_same_Canonicalize(string pattern, string flags, string subject, string expected)
    {
        // §22.2.2.7.3: under u and v, simple case folding (CaseFolding.txt, status C and S); without them, the upper case
        // of one code unit. θ (U+03B8) and ϑ (U+03D1) share both; ẞ (U+1E9E) folds to ß under u, but ß's upper case is
        // two code points, so without u it canonicalizes to itself and ẞ to itself.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("\\u017F", "i", "s", "nomatch")]
    [InlineData("\\u017F", "i", "S", "nomatch")]
    [InlineData("s", "i", "ſ", "nomatch")]
    [InlineData("\\u017F", "iu", "s", "match 0,1")]
    [InlineData("s", "iu", "ſ", "match 0,1")]
    [InlineData("S", "iv", "ſ", "match 0,1")]
    [InlineData("k", "i", "K", "nomatch")]
    [InlineData("\\u212A", "i", "K", "nomatch")]
    [InlineData("k", "iu", "K", "match 0,1")]
    [InlineData("\\u212A", "iu", "K", "match 0,1")]
    [InlineData("[s]", "i", "ſ", "nomatch")]
    [InlineData("[a-z]", "i", "K", "nomatch")]
    [InlineData("[a-z]", "iu", "K", "match 0,1")]
    public void Without_u_a_character_outside_ASCII_whose_upper_case_is_ASCII_does_not_match_it(string pattern, string flags, string subject, string expected)
    {
        // The guard of Canonicalize's non-u branch (§22.2.2.7.3, step 7): "If the numeric value of ch ≥ 128 and the
        // numeric value of cu < 128, return ch." ſ (U+017F) upper-cases to S and the Kelvin sign (U+212A) is its own upper
        // case, so neither meets s, S, k or K without u; under u simple case folding joins them.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("[a-z]+", "i", "1aBcD2", "match 1,5")]
    [InlineData("[^k]", "i", "K", "nomatch")]
    [InlineData("[^k]", "i", "Kx", "match 1,2")]
    [InlineData("[^a-z]", "iu", "K", "nomatch")]
    [InlineData("\\w", "i", "ſ", "nomatch")]
    [InlineData("\\w", "iu", "ſ", "match 0,1")]
    [InlineData("\\W", "iu", "ſ", "nomatch")]
    [InlineData("\\W", "iu", "S", "nomatch")]
    [InlineData("\\W", "i", "ſ", "match 0,1")]
    [InlineData("[\\W]", "iu", "K", "nomatch")]
    [InlineData("\\p{Lu}", "iu", "a", "match 0,1")]
    [InlineData("\\P{Lu}", "iu", "A", "match 0,1")]
    [InlineData("[^\\p{Lu}]", "iu", "a", "nomatch")]
    [InlineData("[^\\p{Lu}]", "iu", "1", "match 0,1")]
    [InlineData("\\d", "i", "1", "match 0,1")]
    public void A_class_under_i_holds_a_character_when_a_member_shares_its_Canonicalize(string pattern, string flags, string subject, string expected)
    {
        // CharacterSetMatcher (§22.2.2.7.1): "there exists a member a of A such that Canonicalize(rer, a) is cc", and an
        // inverted class fails exactly when one does; \w under u with i is WordCharacters (§22.2.2.9.3), which holds ſ and
        // the Kelvin sign, so \W holds neither.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("[a-z]", "iv", "K", "match 0,1")]
    [InlineData("[^a-z]", "iv", "K", "nomatch")]
    [InlineData("\\P{Lu}", "iv", "A", "nomatch")]
    [InlineData("\\P{Lu}", "iv", "a", "nomatch")]
    [InlineData("\\P{Lu}", "iv", "1", "match 0,1")]
    [InlineData("[^\\P{Lu}]", "iv", "a", "match 0,1")]
    [InlineData("[\\p{Lu}--[A-Z]]", "iv", "a", "nomatch")]
    [InlineData("[\\p{Lu}--[A-Z]]", "iv", "é", "match 0,1")]
    [InlineData("[[a-z]&&[^aeiou]]+", "iv", "XyZA", "match 0,3")]
    public void A_v_mode_class_under_i_holds_the_Canonicalize_of_a_character_its_operands_were_folded_to(string pattern, string flags, string subject, string expected)
    {
        // Under v each operand is folded by MaybeSimpleCaseFolding (§22.2.2.9.5) before a complement is taken within
        // AllCharacters, the code points that fold to themselves (§22.2.2.9.4): \P{Lu} under vi holds neither A nor a.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("^a", "", "ba", 0, "nomatch")]
    [InlineData("^a", "", "aa", 1, "nomatch")]
    [InlineData("a$", "", "ab", 0, "nomatch")]
    [InlineData("^a$", "", "a", 0, "match 0,1")]
    [InlineData("^$", "", "", 0, "match 0,0")]
    [InlineData("$", "", "ab", 0, "match 2,2")]
    [InlineData("^b", "", "a\nb", 0, "nomatch")]
    [InlineData("^b", "m", "a\nb", 0, "match 2,3")]
    [InlineData("^b", "m", "a\rb", 0, "match 2,3")]
    [InlineData("^b", "m", "a\u2028b", 0, "match 2,3")]
    [InlineData("^b", "m", "a\u2029b", 0, "match 2,3")]
    [InlineData("^b", "m", "a\u0085b", 0, "nomatch")]
    [InlineData("a$", "m", "a\nb", 0, "match 0,1")]
    [InlineData("a$", "m", "a\u0085", 0, "nomatch")]
    [InlineData("^$", "m", "a\n\nb", 0, "match 2,2")]
    [InlineData("^", "m", "a\n", 1, "match 2,2")]
    [InlineData("(?:^a)+", "m", "a\na", 0, "match 0,1")]
    [InlineData("$\\n^", "m", "a\nb", 0, "match 1,2")]
    public void Caret_and_dollar_hold_at_the_input_s_ends_and_under_m_beside_a_line_terminator(string pattern, string flags, string subject, int start, string expected)
    {
        // Assertion :: ^ and $ (§22.2.2.6): at the input's first and last index, and with rer.[[Multiline]] also after and
        // before a LineTerminator, LF, CR, U+2028 or U+2029, never U+0085.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject, start));
    }

    [Theory]
    [InlineData("\\bb", "", "ab b", "match 3,4")]
    [InlineData("a\\b", "", "ab a", "match 3,4")]
    [InlineData("\\B", "", "", "match 0,0")]
    [InlineData("\\b", "", "", "nomatch")]
    [InlineData("\\b", "", " a", "match 1,1")]
    [InlineData("\\B.", "", "ab", "match 1,2")]
    [InlineData("\\B.", "", "a  b", "match 2,3")]
    [InlineData("\\b_", "", "-_", "match 1,2")]
    [InlineData("\\b", "", "é", "nomatch")]
    [InlineData("\\b", "u", "\U0001F600", "nomatch")]
    [InlineData("\\b", "i", "ſ", "nomatch")]
    [InlineData("\\b", "iu", "ſ", "match 0,0")]
    [InlineData("\\b", "iv", "K", "match 0,0")]
    [InlineData("\\b", "v", "K", "nomatch")]
    [InlineData("\\b", "u", "ſ", "nomatch")]
    [InlineData("a\\b", "i", "aſ", "match 0,1")]
    [InlineData("a\\b", "iu", "aſ", "nomatch")]
    [InlineData("a\\B", "iu", "aK", "match 0,1")]
    [InlineData("\\B", "iv", "Kx", "match 1,1")]
    public void A_word_boundary_reads_the_word_characters_the_flags_give(string pattern, string flags, string subject, string expected)
    {
        // \b and \B (§22.2.2.6) read IsWordChar on each side (§22.2.2.6.1), which reads WordCharacters (§22.2.2.9.3):
        // the basic word characters, and under u or v with i every code point whose simple case folding is one, U+017F
        // and the Kelvin sign U+212A. Without i, or without u and v, neither is a word character.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?i:a)b", "", "AB", "nomatch")]
    [InlineData("(?i:a)b", "", "Ab", "match 0,2")]
    [InlineData("a(?i:b)c", "", "aBc", "match 0,3")]
    [InlineData("a(?i:b)c", "", "aBC", "nomatch")]
    [InlineData("(?i:a)a", "", "aA", "nomatch")]
    [InlineData("(?-i:a)b", "i", "aB", "match 0,2")]
    [InlineData("(?-i:a)b", "i", "AB", "nomatch")]
    [InlineData("(?i:a(?-i:b)c)", "", "AbC", "match 0,3")]
    [InlineData("(?i:a(?-i:b)c)", "", "ABC", "nomatch")]
    [InlineData("(?i:(?-i:(?i:a)))", "", "A", "match 0,1")]
    [InlineData("(?i:a)+", "", "aAaB", "match 0,3")]
    [InlineData("(?i:[^k])", "", "K", "nomatch")]
    [InlineData("(?i:[a-z])", "v", "\u212A", "match 0,1")]
    [InlineData("(?-i:[a-z])", "iv", "\u212A", "nomatch")]
    [InlineData("(?i:\\u017F)", "u", "S", "match 0,1")]
    [InlineData("(?i:\\b)", "u", "\u017F", "match 0,0")]
    [InlineData("(?-i:\\b)", "iu", "\u017F", "nomatch")]
    [InlineData("(?m:^b)", "", "a\nb", "match 2,3")]
    [InlineData("^b(?-m:$)", "m", "b\nb", "match 2,3")]
    [InlineData("(?-m:^)b", "m", "a\nb", "nomatch")]
    [InlineData("(?s:.).", "", "\n\n", "nomatch")]
    [InlineData("(?s:.).", "", "\na", "match 0,2")]
    [InlineData("(?-s:.)", "s", "\n", "nomatch")]
    [InlineData("(?ims:^.a$)", "", "x\n\nA\n", "match 2,4")]
    [InlineData("(?i-ms:^.a$)", "ms", "\n\nA", "nomatch")]
    public void A_modifier_group_s_flags_are_in_force_for_its_body_alone(string pattern, string flags, string subject, string expected)
    {
        // ES2025's modifiers (§22.2.2.7, Atom :: ( ? RegularExpressionModifiers : Disjunction )): the body is compiled
        // with rer's IgnoreCase, Multiline and DotAll updated by the added and removed flags, and what follows the group
        // with rer as it was; a v-mode class folds by the flag in force where it stands.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(a)\\1", "", "aa", "match 0,2 0,1")]
    [InlineData("(a)\\1", "", "ab", "nomatch")]
    [InlineData("\\1(a)", "", "a", "match 0,1 0,1")]
    [InlineData("(a)|\\1b", "", "b", "match 0,1 -")]
    [InlineData("(a)?\\1b", "", "b", "match 0,1 -")]
    [InlineData("(a*)\\1", "", "aaaa", "match 0,4 0,2")]
    [InlineData("(a*)b\\1+", "", "baaa", "match 0,1 0,0")]
    [InlineData("(a)\\1{3}", "", "aaaa", "match 0,4 0,1")]
    [InlineData("(?:(a)|b\\1)+", "", "ab", "match 0,2 -")]
    [InlineData("(a(?:\\1x)?)b", "", "axb", "match 0,3 0,2")]
    [InlineData("(a(?:\\1x)?)b", "", "ab", "match 0,2 0,1")]
    [InlineData("(?<n>ab)\\k<n>", "", "abab", "match 0,4 0,2")]
    [InlineData("\\k<n>(?<n>a)", "", "a", "match 0,1 0,1")]
    [InlineData("(?<n>a)(?<m>b)\\k<m>\\k<n>", "u", "abba", "match 0,4 0,1 1,2")]
    [InlineData("(.)\\1", "u", "\U0001F600\U0001F600", "match 0,4 0,2")]
    [InlineData("(.)\\1", "", "\U0001F600\U0001F600", "nomatch")]
    [InlineData("(..)\\1", "", "\U0001F600\U0001F600", "match 0,4 0,2")]
    public void A_back_reference_matches_what_its_group_captured_and_the_empty_string_when_it_did_not_participate(string pattern, string flags, string subject, string expected)
    {
        // BackreferenceMatcher (§22.2.2.7.2): the characters the group captured, compared one by one with the input's;
        // a group whose capture is undefined, because it has not participated or a quantifier's iteration cleared it,
        // makes the reference match the empty string. Under u and v a character is a code point.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(a)\\1", "i", "aA", "match 0,2 0,1")]
    [InlineData("(\\u017F)\\1", "i", "\u017Fs", "nomatch")]
    [InlineData("(\\u017F)\\1", "iu", "\u017Fs", "match 0,2 0,1")]
    [InlineData("(\\u212A)\\1", "iu", "\u212Ak", "match 0,2 0,1")]
    [InlineData("(\\u212A)\\1", "i", "\u212Ak", "nomatch")]
    [InlineData("([a-z]+)\\1", "i", "aBcAbC", "match 0,6 0,3")]
    [InlineData("(\\u{10400})\\1", "iu", "\U00010400\U00010428", "match 0,4 0,2")]
    [InlineData("(a)(?i:\\1)", "", "aA", "match 0,2 0,1")]
    [InlineData("(?i:(a))(?-i:\\1)", "", "Aa", "nomatch")]
    public void Under_i_a_back_reference_compares_by_Canonicalize(string pattern, string flags, string subject, string expected)
    {
        // §22.2.2.7.2, step c: "If there exists an integer i ... such that Canonicalize(rer, s[i]) is not
        // Canonicalize(rer, Input[g + i]), return failure", with rer's IgnoreCase where the reference stands.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("[\\q{abc|ab}]", "v", "abc", "match 0,3")]
    [InlineData("[\\q{ab|abc}]", "v", "abc", "match 0,3")]
    [InlineData("[\\q{abc|ab}]c", "v", "abc", "match 0,3")]
    [InlineData("[\\q{abc|ab}]c", "v", "abcc", "match 0,4")]
    [InlineData("[a\\q{abc}]", "v", "abc", "match 0,3")]
    [InlineData("[a\\q{abc}]", "v", "abd", "match 0,1")]
    [InlineData("[a\\q{abc}]d", "v", "abcd", "match 0,4")]
    [InlineData("[\\q{}a]", "v", "a", "match 0,1")]
    [InlineData("[\\q{}a]", "v", "b", "match 0,0")]
    [InlineData("[\\q{}]", "v", "x", "match 0,0")]
    [InlineData("[\\q{}]+", "v", "x", "match 0,0")]
    [InlineData("[\\q{}a]*b", "v", "aab", "match 0,3")]
    [InlineData("^[\\q{a|}]$", "v", "", "match 0,0")]
    [InlineData("[\\q{ab}\\q{a}]b", "v", "ab", "match 0,2")]
    [InlineData("([\\q{ab|a}])+c", "v", "abac", "match 0,4 2,3")]
    [InlineData("([\\q{ab|a}])+?b", "v", "abab", "match 0,4 2,3")]
    [InlineData("[\\q{abc}--\\q{abc}]", "v", "abc", "nomatch")]
    [InlineData("[[a-z]--\\q{b}]", "v", "b", "nomatch")]
    [InlineData("[\\q{abc}&&\\q{abc|x}]", "v", "abc", "match 0,3")]
    [InlineData("[\\q{\\u{1F600}a|b}]", "v", "\U0001F600a", "match 0,3")]
    [InlineData("[\\q{a\\u{1F600}}]", "v", "a\U0001F600", "match 0,3")]
    [InlineData("\\p{RGI_Emoji}", "v", "\U0001F468\u200D\U0001F469\u200D\U0001F467", "match 0,8")]
    [InlineData("\\p{RGI_Emoji}", "v", "\U0001F1EF\U0001F1F5x", "match 0,4")]
    [InlineData("^\\p{RGI_Emoji_Flag_Sequence}$", "v", "\U0001F1EF\U0001F1F5", "match 0,4")]
    [InlineData("[\\p{RGI_Emoji_Flag_Sequence}x]+", "v", "\U0001F1EF\U0001F1F5x\U0001F1FA\U0001F1F8", "match 0,9")]
    [InlineData("\\p{Emoji_Keycap_Sequence}", "v", "1\uFE0F\u20E3", "match 0,3")]
    public void A_v_mode_class_with_strings_tries_them_longest_first_then_its_characters_then_the_empty_string(string pattern, string flags, string subject, string expected)
    {
        // CompileAtom for a v-mode CharacterClass whose set holds strings (§22.2.2.7, Atom :: CharacterClass, steps 3 to
        // 8): an alternation of each string of more than one character, in descending order of length, then a
        // CharacterSetMatcher of the single characters, then, when the set holds the empty string, the EmptyMatcher. Each
        // alternative is a backtracking point, so [\q{abc|ab}]c falls back to ab when abc leaves no c. Expected answers
        // agree with V8's (Node 22).
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("[\\q{ABC}]", "vi", "abc", "match 0,3")]
    [InlineData("[\\q{abc}]", "vi", "ABC", "match 0,3")]
    [InlineData("[\\q{\u017Fk}]", "vi", "S\u212A", "match 0,2")]
    [InlineData("(?i:[\\q{abc}])", "v", "ABC", "match 0,3")]
    [InlineData("(?-i:[\\q{abc}])", "vi", "ABC", "nomatch")]
    [InlineData("(?<=[\\q{abc|b}])d", "v", "abcd", "match 3,4")]
    [InlineData("(?<=[\\q{abc|b}])d", "v", "xbd", "match 2,3")]
    [InlineData("(?<=([\\q{bc|abc}]))d", "v", "abcd", "match 3,4 0,3")]
    [InlineData("(?<=^[\\q{ab|a}]+)$", "v", "aab", "match 3,3")]
    [InlineData("(?<=[\\q{}a])b", "v", "b", "match 0,1")]
    [InlineData("(?<=([\\q{ab|b}]))c", "vi", "ABc", "match 2,3 0,2")]
    [InlineData("(?<=[\\q{\\u{1F600}a}])x", "v", "\U0001F600ax", "match 3,4")]
    public void A_class_s_strings_compare_under_the_i_in_force_and_read_backward_in_a_look_behind(string pattern, string flags, string subject, string expected)
    {
        // Under v with i the parser folded each string by MaybeSimpleCaseFolding (§22.2.2.9.5), and each character of it
        // is matched by CharacterSetMatcher with rer's IgnoreCase; in a look-behind each string is MatchSequence with
        // direction backward (§22.2.2.7, step 5.c), its last character read first, and the order of the alternatives does
        // not change.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Fact]
    public void Under_u_a_lone_surrogate_captured_is_not_the_pair_it_would_begin()
    {
        // Theory data would carry a lone surrogate through xunit's serialization, which replaces it, so the subjects are here.
        Assert.Equal("nomatch", MachineTests.Run("(\\uD83D)\\1", "u", "\uD83D\uD83D\uDE00"));
        Assert.Equal("match 0,2 0,1", MachineTests.Run("(\\uD83D)\\1", "", "\uD83D\uD83D\uDE00"));
        Assert.Equal("match 1,3 1,2", MachineTests.Run("(.)\\1", "u", "x\uDE00\uDE00"));
    }
}
