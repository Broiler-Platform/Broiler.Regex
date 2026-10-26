using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The new backend's look-arounds against ECMA-262's Pattern semantics: Assertion :: <c>(?=</c>, <c>(?!</c>,
/// <c>(?&lt;=</c> and <c>(?&lt;!</c> (§22.2.2.6), each body matched once and never backtracked into, and the backward
/// direction a look-behind's body is compiled in (§22.2.2.3 to §22.2.2.7 with direction backward): its terms matched
/// right to left, each character read before the index, a group's range taken from where it ends to where it starts, and a
/// back-reference compared from the capture's end. Each test262 row's answer is test262's own (the file is named); each
/// other row's was worked from the sections named and agrees with V8's (Node 22). Answers are in the form
/// <see cref="MachineTests.Render"/> writes.
/// </summary>
public class MachineLookaroundTests
{
    [Theory]
    [InlineData("(?=a)a", "", "ba", "match 1,2")]
    [InlineData("a(?=b)", "", "acab", "match 2,3")]
    [InlineData("(?=a)", "", "", "nomatch")]
    [InlineData("(?!a)b", "", "ab", "match 1,2")]
    [InlineData("(?!a)", "", "aaa", "match 3,3")]
    [InlineData("a(?!b).", "", "abac", "match 2,4")]
    [InlineData("(?=A)a", "i", "a", "match 0,1")]
    [InlineData("(?=(?i:A))a", "", "a", "match 0,1")]
    [InlineData("(?=.$)", "u", "\U0001F600", "match 0,0")]
    [InlineData("(?=.$)", "", "\U0001F600", "match 1,1")]
    [InlineData("a(?=\\b)", "", "ab a", "match 3,4")]
    [InlineData("(?=^)b", "m", "a\nb", "match 2,3")]
    public void A_look_ahead_tests_what_follows_the_index_and_leaves_the_index_where_it_was(string pattern, string flags, string subject, string expected)
    {
        // §22.2.2.6, Assertion :: (?= Disjunction ): step 2.g, "Let z be the MatchState (input, xe, cap)": the body's end is
        // dropped and the continuation runs from the index the look-ahead started at.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?=(a+))", "", "baaabac", "match 1,1 1,4")]
    [InlineData("(?=(a+))a*b\\1", "", "baaabac", "match 3,6 3,4")]
    [InlineData("(.*?)a(?!(a+)b\\2c)\\2(.*)", "", "baaabaac", "match 0,8 0,2 - 3,8")]
    public void The_specification_s_own_look_ahead_examples_answer_as_its_notes_say(string pattern, string flags, string subject, string expected)
    {
        // The notes of §22.2.2.6: /(?=(a+))/ on "baaabac" is ["", "aaa"]; /(?=(a+))a*b\1/ is ["aba", "a"], since the
        // look-ahead's choice is not backtracked into; /(.*?)a(?!(a+)b\2c)\2(.*)/ on "baaabaac" is
        // ["baaabaac", "ba", undefined, "abaac"], since a negative look-ahead's captures are always undefined after it.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?=(a|ab))\\1c", "", "abc", "nomatch")]
    [InlineData("(?:(?=(a|ab))\\1|ab)c", "", "abc", "match 0,3 -")]
    [InlineData("(?<=([abc]+)).\\1", "", "abcdbc", "nomatch")]
    public void A_look_around_s_body_is_matched_once_and_never_backtracked_into(string pattern, string flags, string subject, string expected)
    {
        // §22.2.2.6, step 2.b: "Let r be m(x, d)", with d the continuation that always succeeds; what follows the look-around
        // is not part of m's continuation, so when it fails no other way through the body is tried (test262
        // lookBehind/do-not-backtrack.js for the last row).
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?!(a)b)a\\1", "", "aa", "match 0,1 -")]
    [InlineData("(?!(a)c)a\\1", "", "ab", "match 0,1 -")]
    [InlineData("(?!(b))(a)\\1", "", "a", "match 0,1 - 0,1")]
    [InlineData("(?!(a))\\1b", "", "b", "match 0,1 -")]
    [InlineData("(a)(?!(b))\\2c", "", "ac", "match 0,2 0,1 -")]
    [InlineData("(?!(?!(a)))a\\1", "", "aa", "match 0,1 -")]
    [InlineData("(?=(?=(a))(?!(b)))a\\1", "", "aa", "match 0,2 0,1 -")]
    [InlineData("(?<!(^|[ab]))\\w{2}", "", "abcdef", "match 3,5 -")]
    [InlineData("(?<!(a))b\\1", "", "xb", "match 1,2 -")]
    [InlineData("(?<!(a)c)b\\1", "", "acb", "nomatch")]
    [InlineData("(?<=x(?!(a)b)..)c\\1", "", "xacc", "match 3,4 -")]
    public void A_negative_look_around_leaves_every_capture_as_it_found_it(string pattern, string flags, string subject, string expected)
    {
        // §22.2.2.6, Assertion :: (?! Disjunction ), step 2.d: "Return c(x)", the state from before the body, so a group
        // the body set on a path that failed, or on the path that made the assertion fail, is undefined after it (the
        // second row's group was set before its body's c failed). test262 lookBehind/captures-negative.js for the eighth.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?=(a))?a", "", "a", "match 0,1 -")]
    [InlineData("(?=(a))*a", "", "a", "match 0,1 -")]
    [InlineData("(?=(a)){2}a", "", "a", "match 0,1 0,1")]
    [InlineData("(?=(a)){0}a", "", "a", "match 0,1 -")]
    [InlineData("(?=a){0}b", "", "b", "match 0,1")]
    [InlineData("(?!a)+b", "", "b", "match 0,1")]
    [InlineData("(?:(?=(\\w))\\1)+", "", "ab", "match 0,2 1,2")]
    public void A_quantified_look_ahead_is_an_iteration_that_consumes_nothing(string pattern, string flags, string subject, string expected)
    {
        // Annex B's QuantifiableAssertion (§B.1.2): outside u and v a look-ahead may be quantified. RepeatMatcher
        // (§22.2.2.3.1) then sees an iteration that ends where it began: an optional one fails, taking its captures back
        // with it (step 2.b), and a required one stands.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?<=(c))def", "", "abcdef", "match 3,6 2,3")]
    [InlineData("(?<=(\\w{2}))def", "", "abcdef", "match 3,6 1,3")]
    [InlineData("(?<=(\\w(\\w)))def", "", "abcdef", "match 3,6 1,3 2,3")]
    [InlineData("(?<=(\\w){3})def", "", "abcdef", "match 3,6 0,1")]
    [InlineData("(?<=(bc)|(cd)).", "", "abcdef", "match 3,4 1,3 -")]
    [InlineData("(?<=([ab]{1,2})\\D|(abc))\\w", "", "abcdef", "match 2,3 0,1 -")]
    [InlineData("\\D(?<=([ab]+))(\\w)", "", "abcdef", "match 0,2 0,1 1,2")]
    [InlineData(".*(?<=(..|...|....))(.*)", "", "xabcd", "match 0,5 3,5 5,5")]
    [InlineData(".*(?<=(xx|...|....))(.*)", "", "xabcd", "match 0,5 2,5 5,5")]
    [InlineData(".*(?<=(xx|...))(.*)", "", "xxabcd", "match 0,6 3,6 6,6")]
    [InlineData(".*(?<=(xx|xxx))(.*)", "", "xxabcd", "match 0,6 0,2 2,6")]
    [InlineData("(?<=(b+))c", "", "abbbbbbc", "match 7,8 1,7")]
    [InlineData("(?<=(b\\d+))c", "", "ab1234c", "match 6,7 1,6")]
    [InlineData("(?<=((?:b\\d{2})+))c", "", "ab12b23b34c", "match 10,11 1,10")]
    [InlineData("(?<=[a|b|c]*)[^a|b|c]{3}", "", "abcdef", "match 3,6")]
    [InlineData("(?<=\\w*)[^a|b|c]{3}", "", "abcdef", "match 3,6")]
    public void A_look_behind_matches_its_body_backward_and_reports_its_captures_in_source_order(string pattern, string flags, string subject, string expected)
    {
        // test262 lookBehind/captures.js, alternations.js, greedy-loop.js and variable-length.js. §22.2.2.3 with
        // direction backward matches a sequence's right term first; §22.2.2.7's group step takes CaptureRange(ye, xe),
        // its end where the body stopped reading and its start where the body began, so a range reads left to right.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?<=(b+?))c", "", "abbbc", "match 4,5 3,4")]
    [InlineData("(?<=(b*?)(b*))c", "", "abbbc", "match 4,5 1,1 1,4")]
    [InlineData("(?<=(a|ab)(c|bcd))d", "", "abcd", "match 3,4 0,2 2,3")]
    [InlineData("(?<=(?:a|b)*)c", "", "abc", "match 2,3")]
    [InlineData("(?<=(a)+)b", "", "aab", "match 2,3 0,1")]
    [InlineData("(?<=(?:(a)|b)+)c", "", "abc", "match 2,3 0,1")]
    [InlineData("(?<=(?:(a)|(b))+)c", "", "abc", "match 2,3 0,1 -")]
    [InlineData("(?<=(a){2,3})b", "", "aaaab", "match 4,5 1,2")]
    [InlineData("(?<=(a){2,3}?)b", "", "aaaab", "match 4,5 2,3")]
    [InlineData("(?<=(?:)*)a", "", "a", "match 0,1")]
    [InlineData("(?<=(a*)*)b", "", "aab", "match 2,3 0,2")]
    [InlineData("(?<=^(a?b??)*)$", "", "ab", "match 2,2 0,2")]
    [InlineData("(?<=^(a?b??)*)$", "", "ba", "match 2,2 0,1")]
    public void Backward_a_quantifier_and_an_alternation_keep_their_order_and_their_rules(string pattern, string flags, string subject, string expected)
    {
        // RepeatMatcher and MatchTwoAlternatives do not read the direction (§22.2.2.3.1, §22.2.2.3.3): greedy is still the
        // most iterations first, lazy the fewest, the left alternative first, an optional iteration that consumed nothing
        // still fails and each iteration still clears the captures of its atom; only where each iteration reads moves.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(.)(?<=(\\1\\1))", "", "abb", "match 2,3 2,3 1,3")]
    [InlineData("(.)(?<=(\\1\\1))", "i", "abB", "match 2,3 2,3 1,3")]
    [InlineData("((\\w)\\w)(?<=\\1\\2\\1)", "i", "aabAaBa", "match 4,6 4,6 4,5")]
    [InlineData("(\\w(\\w))(?<=\\1\\2\\1)", "i", "aabAaBa", "match 5,7 5,7 6,7")]
    [InlineData("(?=(\\w))(?<=(\\1)).", "i", "abaBbAa", "match 4,5 4,5 3,4")]
    [InlineData("(?<=(.))(\\w+)(?=\\1)", "", "  'foo'  ", "match 3,6 2,3 3,6")]
    [InlineData("(?<=(.))(\\w+)(?=\\1)", "", "  .foo\"  ", "nomatch")]
    [InlineData("(.)(?<=\\1\\1\\1)", "", "abbb", "match 3,4 3,4")]
    [InlineData("(.)(?<=\\1\\1\\1)", "", "abb", "nomatch")]
    [InlineData("(..)(?<=\\1\\1\\1)", "", "fababab", "match 5,7 5,7")]
    [InlineData("(..)(?<=\\1\\1\\1)", "", "faxabab", "nomatch")]
    [InlineData("(?=(abcdefg))(?<=\\1)", "", "bcdefgabcdefg", "nomatch")]
    [InlineData("^(f)oo(?<=^\\1o+)$", "i", "foo", "match 0,3 0,1")]
    [InlineData("^(f)oo(?<=^\\1o+).$", "i", "fooሴ", "match 0,4 0,1")]
    [InlineData("(?<=\\k<n>(?<n>.))x", "u", "aax", "match 2,3 1,2")]
    [InlineData("(?<=(?<n>.)\\k<n>)x", "", "aax", "match 2,3 1,2")]
    public void Backward_a_back_reference_compares_the_characters_that_end_at_the_index(string pattern, string flags, string subject, string expected)
    {
        // BackreferenceMatcher with direction backward (§22.2.2.7.2): f is e - len, the characters compared are
        // Input[f] to Input[e - 1], and the index moves to f. test262 lookBehind/back-references.js, sliced-strings.js
        // and start-of-line.js. In the last row the group is matched after the reference, which reads it undefined and
        // matches the empty string.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?<=\\1(\\w))d", "i", "abcCd", "match 4,5 3,4")]
    [InlineData("(?<=\\1([abx]))d", "", "abxxd", "match 4,5 3,4")]
    [InlineData("(?<=\\1(\\w+))c", "", "ababc", "match 4,5 2,4")]
    [InlineData("(?<=\\1(\\w+))c", "", "ababbc", "match 5,6 4,5")]
    [InlineData("(?<=\\1(\\w+))c", "", "ababdc", "nomatch")]
    [InlineData("(?<=(\\w+)\\1)c", "", "ababc", "match 4,5 0,4")]
    [InlineData("(?<=a(.\\2)b(\\1)).{4}", "", "aabcacbc", "match 3,7 1,2 3,3")]
    [InlineData("(?<=a(\\2)b(..\\1))b", "", "aacbacb", "match 6,7 1,3 4,6")]
    [InlineData("(?<=(?:\\1b)(aa)).", "", "aabaax", "match 5,6 3,5")]
    [InlineData("(?<=(?:\\1|b)(aa)).", "", "aaaax", "match 4,5 2,4")]
    public void Inside_a_look_behind_a_back_reference_reads_a_group_captured_later_in_source_order(string pattern, string flags, string subject, string expected)
    {
        // Issue #923, rows 3 and 4 of the README's table: test262 lookBehind/back-references-to-captures.js and
        // mutual-recursive.js. Backward, a group to the right of its reference is matched first, so the reference reads
        // what it captured; one to its left is still undefined, and the reference matches the empty string.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?<=$abc)def", "", "abcdef", "nomatch")]
    [InlineData("^f.o(?<=foo)$", "", "fno", "nomatch")]
    [InlineData("^foo(?<!foo)$", "", "foo", "nomatch")]
    [InlineData("^f.o(?<=foo)$", "", "foo", "match 0,3")]
    [InlineData("^f.o(?<!foo)$", "", "fno", "match 0,3")]
    [InlineData("^foooo(?<=fo*)$", "", "foooo", "match 0,5")]
    [InlineData("(?<!abc)\\w\\w\\w", "", "abcdef", "match 0,3")]
    [InlineData("(?<!a[a-z]{2})\\w\\w\\w", "", "abcdef", "match 0,3")]
    [InlineData("(?<!abc)def", "", "abcdef", "nomatch")]
    [InlineData("(?<!a{1}[a-z]{2})def", "", "abcdef", "nomatch")]
    [InlineData("^.(?<=a)", "", "b", "nomatch")]
    [InlineData("^f\\w\\w(?<=\\woo)", "", "foo2", "match 0,3")]
    [InlineData("(?<=a[a-z])\\w\\w\\w", "", "abcdef", "match 2,5")]
    [InlineData("(?<=a{1}b{1})\\w\\w\\w", "", "abcdef", "match 2,5")]
    [InlineData("(?<=^[^a-c]{3})def", "", "abcdef", "nomatch")]
    [InlineData("(?<=^abc)def", "", "abcdef", "match 3,6")]
    [InlineData("(?<=^[a-c]{3})def", "m", "xyz\nabcdef", "match 7,10")]
    [InlineData("(?<=\\b)[d-f]{3}", "", "abc def", "match 4,7")]
    [InlineData("(?<=\\B)\\w", "", "abc def", "match 1,2")]
    [InlineData("(?<!\\b)[d-f]", "", "abc def", "match 5,6")]
    public void A_look_behind_s_assertions_read_the_index_they_are_reached_at(string pattern, string flags, string subject, string expected)
    {
        // test262 lookBehind/misc.js, negative.js, simple-fixed-length.js and start-of-line.js: ^, $, \b and \B do not
        // read the direction (§22.2.2.6), only the index the backward body has reached.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?<=^)\\w+", "m", "ab\ncd\nefg", 2, "match 3,5")]
    [InlineData("\\w+(?<=$)", "m", "ab\ncd\nefg", 2, "match 3,5")]
    [InlineData("(?<=^\\w+)def", "", "abcdefdef", 4, "match 6,9")]
    public void A_look_behind_reads_before_the_start_of_the_search(string pattern, string flags, string subject, int start, string expected)
    {
        // test262 lookBehind/start-of-line.js and sticky.js, from the lastIndex a global match leaves: the input before
        // the start is the input's, read as any other.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject, start));
    }

    [Theory]
    [InlineData("(?<=ab(?=c)\\wd)\\w\\w", "", "abcdef", "match 4,6")]
    [InlineData("(?<=a(?=([^a]{2})d)\\w{3})\\w\\w", "", "abcdef", "match 4,6 1,3")]
    [InlineData("(?<=a(?=([bc]{2}(?<!a{2}))d)\\w{3})\\w\\w", "", "abcdef", "match 4,6 1,3")]
    [InlineData("^faaao?(?<=^f[oa]+(?=o))", "", "faaao", "match 0,4")]
    [InlineData("(?<=a(?=([bc]{2}(?<!a*))d)\\w{3})\\w\\w", "", "abcdef", "nomatch")]
    [InlineData("(?<=a(?=b))b", "", "ab", "match 1,2")]
    [InlineData("(?<=(?<=a)b)c", "", "abc", "match 2,3")]
    [InlineData("(?<=(?<!a)b)c", "", "abc", "nomatch")]
    [InlineData("(?<=(?<!a)b)c", "", "xbc", "match 2,3")]
    [InlineData("(?<=(a)(?=(b)))b", "", "ab", "match 1,2 0,1 1,2")]
    public void A_nested_look_around_reads_in_its_own_direction(string pattern, string flags, string subject, string expected)
    {
        // A look-ahead's body is compiled forward and a look-behind's backward wherever they stand (§22.2.2.6, step 1 of
        // each): test262 lookBehind/nested-lookaround.js and the rows after it.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?<=AB)c", "i", "abc", "match 2,3")]
    [InlineData("(?<=[^a])c", "i", "Ac", "nomatch")]
    [InlineData("(?<=[^a])c", "i", "bc", "match 1,2")]
    [InlineData("(?<=ſ)x", "i", "Sx", "nomatch")]
    [InlineData("(?<=ſ)x", "iu", "Sx", "match 1,2")]
    [InlineData("(?<=\\1(.))x", "i", "ſSx", "nomatch")]
    [InlineData("(?<=\\1(.))x", "iu", "ſSx", "match 2,3 1,2")]
    [InlineData("(?<=\\1(.))x", "iu", "kKx", "match 2,3 1,2")]
    [InlineData("(?<=(?i:a))b", "", "Ab", "match 1,2")]
    [InlineData("(?<=(?-i:a))b", "i", "AB", "nomatch")]
    [InlineData("(?<=(?m:^))b", "", "a\nb", "match 2,3")]
    [InlineData("(?<=(?s:.))b", "", "\nb", "match 1,2")]
    [InlineData("(?<=.)b", "", "\nb", "nomatch")]
    public void Backward_a_character_compares_under_the_flags_in_force_where_it_stands(string pattern, string flags, string subject, string expected)
    {
        // Canonicalize (§22.2.2.7.3) and the modifiers (§22.2.2.7) do not read the direction: ſ is not S without u, and
        // is s's under u with i, read backward as forward.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("(?<=\\u{1F438})a", "u", "\U0001F438a", "match 2,3")]
    [InlineData("(?<=^.)a", "u", "\U0001F600a", "match 2,3")]
    [InlineData("(?<=^.)a", "", "\U0001F600a", "nomatch")]
    [InlineData("(?<=^..)a", "", "\U0001F600a", "match 2,3")]
    [InlineData("(?<=(.))a", "u", "\U0001F600a", "match 2,3 0,2")]
    [InlineData("(?<=(.))a", "", "\U0001F600a", "match 2,3 1,2")]
    [InlineData("(?<=[\\u{1F600}-\\u{1F64F}])a", "u", "\U0001F610a", "match 2,3")]
    [InlineData("(?<=\\p{Emoji_Presentation})a", "u", "\U0001F610a", "match 2,3")]
    [InlineData("(?<=(.)\\1)x", "u", "\U0001F600\U0001F600x", "match 4,5 2,4")]
    [InlineData("(?<=\\1(.))x", "u", "\U0001F600\U0001F600x", "match 4,5 2,4")]
    [InlineData("(?<=\\1(.))x", "", "\U0001F600\U0001F600x", "nomatch")]
    [InlineData("(?<=\\1(..))x", "", "\U0001F600\U0001F600x", "match 4,5 2,4")]
    [InlineData("(?<=\\1(.))x", "u", "\U00010400\U00010428x", "nomatch")]
    [InlineData("(?<=\\1(.))x", "iu", "\U00010400\U00010428x", "match 4,5 2,4")]
    [InlineData("(?<=\\uD83D)x", "u", "\U0001F600x", "nomatch")]
    [InlineData("(?<=\\uDE00)x", "u", "\U0001F600x", "nomatch")]
    [InlineData("(?<=\\uDE00)x", "", "\U0001F600x", "match 2,3")]
    public void Backward_under_u_a_character_is_the_code_point_that_ends_at_the_index(string pattern, string flags, string subject, string expected)
    {
        // Under u and v, Input is the subject's code points (§22.2.2, "the Input is a List of the characters"), so a
        // backward read takes a whole surrogate pair, and neither of its halves alone.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Theory]
    [InlineData("foo(.+)bar\\1", "u", "fooAbarAA", "match 0,8 3,4")]
    [InlineData("foo(.+)bar\\1", "u", "fooAbarA", "match 0,8 3,4")]
    [InlineData("(?<=\\1bar(.+))foo", "u", "AbarAfoo", "match 5,8 4,5")]
    [InlineData("(?<=\\1bar(.+))foo", "u", "BbarAfoo", "nomatch")]
    [InlineData("\\u{1F438}", "u", "\U0001F438", "match 0,2")]
    [InlineData("\\u{1F438}?", "u", "", "match 0,0")]
    [InlineData("\\u{1F438}+", "u", "\U0001F438\U0001F438", "match 0,4")]
    [InlineData("\\u{1F438}*", "u", "\U0001F438\U0001F438", "match 0,4")]
    [InlineData("\\u{1F438}{2}", "u", "\U0001F438\U0001F438", "match 0,4")]
    [InlineData("(?<=\\u{1F438}+)x", "u", "\U0001F438\U0001F438x", "match 4,5")]
    [InlineData("(?<=(\\u{1F438}+))x", "u", "\U0001F438\U0001F438x", "match 4,5 0,4")]
    [InlineData("(?<=^\\u{1F438}{2})x", "u", "\U0001F438\U0001F438x", "match 4,5")]
    [InlineData("(?<=^\\u{1F438}{3})x", "u", "\U0001F438\U0001F438x", "nomatch")]
    [InlineData("(a?b??)*", "", "ab", "match 0,2 1,2")]
    public void The_issue_923_rows_6_7_and_8_answer_as_the_specification_says_both_ways(string pattern, string flags, string subject, string expected)
    {
        // Issue #923, rows 6 to 8 of the README's table: sm/RegExp/unicode-back-reference.js (a back-reference compares
        // code points), sm/RegExp/unicode-braced.js (\u{…} is one code point, quantified as one) and
        // nullable-quantifier.js (an optional iteration that matched the empty string is abandoned), each with a
        // backward form; the backward form of row 8 is a row of the quantifier theory above.
        Assert.Equal(expected, MachineTests.Run(pattern, flags, subject));
    }

    [Fact]
    public void The_issue_923_row_6_cases_with_lone_surrogates_answer_as_SpiderMonkey_s_test_says()
    {
        // sm/RegExp/unicode-back-reference.js: under u a captured lone surrogate is not the pair it would begin or end,
        // and a captured pair is not its lone halves. Theory data would carry a lone surrogate through xunit's
        // serialization, which replaces it, so the subjects are here; the backward rows are this suite's own.
        Assert.Equal("match 0,8 3,4", MachineTests.Run("foo(.+)bar\\1", "u", "fooAbarA\uDC00"));
        Assert.Equal("match 0,8 3,4", MachineTests.Run("foo(.+)bar\\1", "u", "fooAbarA\uD834"));
        Assert.Equal("nomatch", MachineTests.Run("foo(.+)bar\\1", "u", "foo\uD834bar𝀀"));
        Assert.Equal("match 0,8 3,4", MachineTests.Run("foo(.+)bar\\1", "u", "foo\uD834bar\uD834\uD834"));
        Assert.Equal("match 0,8 3,4", MachineTests.Run("foo(.+)bar\\1", "u", "foo\uD834bar\uD834"));
        Assert.Equal("match 0,8 3,4", MachineTests.Run("foo(.+)bar\\1", "u", "foo\uDC00bar\uDC00\uDC00"));
        Assert.Equal("match 0,8 3,4", MachineTests.Run("foo(.+)bar\\1", "u", "foo\uDC00bar\uDC00"));
        Assert.Equal("nomatch", MachineTests.Run("^(.+)\\1$", "u", "\uDC00foobar𝀀foobar\uD834"));
        Assert.Equal("nomatch", MachineTests.Run("(?<=\\1bar(.+))foo", "u", "𝀀bar\uDC00foo"));
        Assert.Equal("match 5,8 4,5", MachineTests.Run("(?<=\\1bar(.+))foo", "u", "\uDC00bar\uDC00foo"));
        Assert.Equal("nomatch", MachineTests.Run("(?<=\\1bar(.+))foo", "u", "\uD834bar𝀀foo"));
        Assert.Equal("nomatch", MachineTests.Run("(?<=\\1(.))x", "u", "😀\uDE00x"));
        Assert.Equal("match 3,4 2,3", MachineTests.Run("(?<=\\1(.))x", "", "😀\uDE00x"));
        Assert.Equal("nomatch", MachineTests.Run("(?<=\\1(.))x", "u", "\uD83D😀x"));
        Assert.Equal("match 2,3", MachineTests.Run("(?<=\\uDE00)x", "u", "\uDE00\uDE00x"));
        Assert.Equal("match 1,2", MachineTests.Run("(?<=^.)x", "u", "\uDE00x"));
        Assert.Equal("match 1,2", MachineTests.Run("(?<=^.)x", "u", "\uD83Dx"));
        Assert.Equal("match 0,0", MachineTests.Run("\\u{1F438}?", "u", "\uD83D"));
        Assert.Equal("match 0,2", MachineTests.Run("\\u{1F438}*", "u", "🐸\uDC38"));
        Assert.Equal("match 0,2", MachineTests.Run("\\u{1F438}+", "u", "🐸\uDC38"));
        Assert.Equal("match 2,3", MachineTests.Run("(?<=\\u{1F438}+)\\uDC38", "u", "🐸\uDC38"));
    }

    [Fact]
    public void A_start_inside_a_pair_splits_it_for_every_read_of_the_call_backward_reads_included()
    {
        // The entry's contract for a start inside a surrogate pair under u (RegexProgram.Match): the pair is read as two
        // lone units by every read of the call, so a look-behind from the start meets the high half alone, and a
        // capture that holds it does not compare as the pair it began.
        const string pair = "😀";
        Assert.Equal("match 1,1", MachineTests.Run("(?<=\\uD83D)", "u", pair, 1, anchored: true));
        Assert.Equal("match 1,1 0,1", MachineTests.Run("(?<=(.))", "u", pair, 1, anchored: true));
        Assert.Equal("match 1,2 0,1", MachineTests.Run("(?<=(.)).", "u", pair + "x", 1));
        Assert.Equal("nomatch", MachineTests.Run("(?<=(.)).\\1", "u", pair + pair, 1));
        Assert.Equal("match 1,1", MachineTests.Run("\\B", "u", pair, 1, anchored: true));
        Assert.Equal("match 2,3 1,2", MachineTests.Run("(?<=(.))x", "u", pair + "x", 1));
    }
}
