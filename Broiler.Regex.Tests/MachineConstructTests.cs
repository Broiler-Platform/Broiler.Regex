using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The new backend's constructs of task 2.4b against ECMA-262's Pattern semantics (§22.2.2): characters and classes
/// compared under <c>i</c> by both branches of Canonicalize (§22.2.2.7.3), and the assertions <c>^ $ \b \B</c>
/// (§22.2.2.6). Every expected answer is the specification's, read from the section each theory names, in the form
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
}
