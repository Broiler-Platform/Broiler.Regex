namespace Broiler.Regex.Tests;

/// <summary>
/// <c>v</c>-mode (UnicodeSets) class-set expressions: nesting, <c>&amp;&amp;</c>
/// intersection, <c>--</c> subtraction, <c>\q{…}</c> string alternatives and properties
/// of strings. These were previously parsed and then ignored — the operators were skipped
/// and the class matched the union of its operands — so every <c>v</c> pattern using one
/// had to be refused by the router (Broiler.JS gaps roadmap, track 2 action 2).
/// </summary>
public class UnicodeSetsTests
{
    private static string? Match(string pattern, string flags, string input)
    {
        var m = new BroilerRegex(pattern, flags).Match(input);
        return m.Success ? m.Value : null;
    }

    [Theory]
    // Subtraction.
    [InlineData(@"[\p{L}--[a-z]]", "v", "A", "A")]
    [InlineData(@"[\p{L}--[a-z]]", "v", "a", null)]
    [InlineData(@"[\d--[0-5]]", "v", "7", "7")]
    [InlineData(@"[\d--[0-5]]", "v", "3", null)]
    [InlineData(@"[\w--\d]", "v", "a", "a")]
    [InlineData(@"[[\w]--[\d]]", "v", "a", "a")]
    // Intersection.
    [InlineData(@"[\w&&\d]", "v", "5", "5")]
    [InlineData(@"[\w&&\d]", "v", "a", null)]
    [InlineData(@"[a&&b]", "v", "a", null)]
    [InlineData(@"[\p{L}&&\p{ASCII}]", "v", "a", "a")]
    [InlineData(@"[\p{L}&&\p{ASCII}]", "v", "α", null)]
    // Nesting and plain union.
    [InlineData(@"[[a-c][x-z]]", "v", "y", "y")]
    [InlineData(@"[[a][b]]", "v", "b", "b")]
    [InlineData(@"[a-c]", "v", "b", "b")]
    [InlineData(@"[^\d]", "v", "a", "a")]
    public void SetOperators_AreEvaluated(string pattern, string flags, string input, string? expected)
        => Assert.Equal(expected, Match(pattern, flags, input));

    [Theory]
    [InlineData(@"[\q{abc|d}]", "abc", "abc")]
    [InlineData(@"[\q{abc|d}]", "d", "d")]
    [InlineData(@"[\q{abc|d}]", "ab", null)]
    [InlineData(@"[\q{a}]", "a", "a")]
    [InlineData(@"[\d\q{ab}]", "ab", "ab")]
    // A string alternative and a quantifier: the class is an alternation, so the repeat
    // consumes whole alternatives rather than single code points.
    [InlineData(@"[\q{abc|d}]+", "abcd", "abcd")]
    public void StringDisjunction_MatchesWholeAlternatives(string pattern, string input, string? expected)
        => Assert.Equal(expected, Match(pattern, "v", input));

    [Fact(Timeout = 600000)]
    public void StringDisjunction_MayContainTheEmptyString()
    {
        Assert.Equal("", Match(@"[\q{}]", "v", ""));
        Assert.Equal("a", Match(@"[\q{}]a", "v", "a"));
        Assert.Equal("ab", Match(@"[\q{|ab}]", "v", "ab"));
    }

    [Theory]
    // Set operations apply to the string members too.
    [InlineData(@"[\q{a|bc}&&\q{bc|d}]", "bc", "bc")]
    [InlineData(@"[\q{a|bc}&&\q{bc|d}]", "a", null)]
    [InlineData(@"[\q{ab}--\q{ab}]", "ab", null)]
    public void SetOperators_ApplyToStringMembers(string pattern, string input, string? expected)
        => Assert.Equal(expected, Match(pattern, "v", input));

    [Theory]
    // A property of strings expands to its sequences, longest first.
    [InlineData(@"[\p{RGI_Emoji}]", "\U0001F468‍\U0001F469‍\U0001F467")]
    [InlineData(@"[\p{RGI_Emoji}]", "\U0001F600")]
    [InlineData(@"[\p{Basic_Emoji}]", "❤️")]
    [InlineData(@"\p{RGI_Emoji}", "\U0001F600")]
    public void PropertyOfStrings_MatchesWholeSequences(string pattern, string input)
        => Assert.Equal(input, Match(pattern, "v", input));

    [Theory]
    // §22.2.1 MaybeSimpleCaseFolding folds each operand BEFORE the complement is taken,
    // and §22.2.2.9 AllCharacters restricts that complement to the code points which fold
    // to themselves. Together they are why `v` and `u` disagree here: /[^\p{Lu}]/ui
    // matches nothing an uppercase letter folds from, while /[^\p{Lu}]/vi additionally
    // rejects the uppercase letters themselves.
    [InlineData(@"[^\p{Lu}]", "A", null)]
    [InlineData(@"[^\p{Lu}]", "a", null)]
    [InlineData(@"[^\p{Lu}]", "1", "1")]
    [InlineData(@"[^\P{Lu}]", "A", "A")]
    [InlineData(@"[^\P{Lu}]", "a", "a")]
    [InlineData(@"[^\P{Lu}]", "1", null)]
    [InlineData(@"[\p{Lu}]", "a", "a")]
    [InlineData(@"[\p{Lu}]", "A", "A")]
    [InlineData(@"[\P{Lu}]", "A", null)]
    [InlineData(@"[\P{Lu}]", "1", "1")]
    [InlineData(@"[^a-z]", "A", null)]
    [InlineData(@"[^a]", "A", null)]
    [InlineData(@"[\w&&\p{L}]", "A", "A")]
    [InlineData(@"[\p{Lu}--\p{Ll}]", "A", null)]
    [InlineData(@"[\W]", "A", null)]
    [InlineData(@"[\W]", "!", "!")]
    [InlineData(@"[^\W]", "A", "A")]
    [InlineData(@"[^\d]", "5", null)]
    [InlineData(@"[^\D]", "5", "5")]
    [InlineData(@"[\q{ab}]", "AB", "AB")]
    [InlineData(@"[\q{ab}]", "aB", "aB")]
    public void IgnoreCase_FoldsOperandsBeforeComplementing(string pattern, string input, string? expected)
        => Assert.Equal(expected, Match(pattern, "vi", input));

    [Theory]
    // The same rule reaches a standalone escape, which under `v` is a class-set operand
    // of its own: /\P{Lu}/vi rejects both cases where /\P{Lu}/ui accepts the lowercase.
    [InlineData(@"\P{Lu}", "a", null)]
    [InlineData(@"\P{Lu}", "A", null)]
    [InlineData(@"\P{Lu}", "1", "1")]
    [InlineData(@"\p{Lu}", "a", "a")]
    [InlineData(@"\W", "A", null)]
    [InlineData(@"\W", "!", "!")]
    [InlineData(@"\D", "a", "a")]
    [InlineData(@"\S", "a", "a")]
    public void IgnoreCase_ReachesStandaloneEscapes(string pattern, string input, string? expected)
        => Assert.Equal(expected, Match(pattern, "vi", input));

    [Fact(Timeout = 600000)]
    public void KelvinSign_FoldsWithTheAsciiLetter()
    {
        // The class folds to {k}, so both spellings match it and neither matches the
        // complement — the uppercase K is not even in the universe the complement is
        // taken against.
        Assert.Equal("k", Match(@"[K]", "vi", "k"));
        Assert.Equal("K", Match(@"[K]", "vi", "K"));
        Assert.Null(Match(@"[^K]", "vi", "k"));
        Assert.Null(Match(@"[^K]", "vi", "K"));
    }

    [Theory]
    [InlineData("a", "A")]
    [InlineData("A", "a")]
    [InlineData("k", "K")]
    public void SingleCharacterStringAlternative_FoldsUnlikeV8(string member, string input)
    {
        // A second DELIBERATE divergence from V8, pinned for the same reason.
        // §22.2.2.9 folds a ClassStringDisjunction operand and its one-character elements
        // stay ordinary single-character CharSetElements, so §22.2.2.10 canonicalizes the
        // subject against them like any other member.
        //
        // V8 folds the member but not the subject when the alternative is one character:
        // `/[\q{A}]/vi` matches `a` (the member folded) yet not `A` (the subject did not),
        // while a two-character alternative canonicalizes both — `/[\q{ab}]/vi` matches
        // `aB`. The two cannot both be right, and a plain member folds either way.
        Assert.Equal(input, Match("[\\q{" + member + "}]", "vi", input));
        Assert.Equal(member, Match("[\\q{" + member + "}]", "vi", member));
        // Where V8 agrees: a longer alternative, and a plain class member.
        Assert.Equal("aB", Match(@"[\q{ab}]", "vi", "aB"));
        Assert.Equal("A", Match("[" + member.ToLowerInvariant() + "A]", "vi", "A"));
    }

    [Theory]
    [InlineData("ſ")]
    [InlineData("K")]
    public void PropertyOfBinaryName_FoldsUnlikeV8(string input)
    {
        // A DELIBERATE divergence from V8, pinned here so it cannot drift silently.
        // §22.2.2.9 returns MaybeSimpleCaseFolding of a lone BINARY property's set (only a
        // lone General_Category value is exempt), so `\p{ASCII}` under `vi` holds `s`, and
        // §22.2.2.10 then matches ſ against it because ſ canonicalizes to `s`.
        //
        // V8 answers "no match" for `\p{ASCII}` while agreeing on the equivalent literal
        // range `[\u0000-\u007F]` and on every other property tested, which reads as an
        // unfolded fast path for ASCII rather than a different reading of the spec.
        Assert.Equal(input, Match(@"\p{ASCII}", "vi", input));
        Assert.Equal(input, Match(@"[\u0000-\u007F]", "vi", input));
        // Without `v` the two agree: case closure runs over the unfolded set.
        Assert.Equal(input, Match(@"\p{ASCII}", "ui", input));
    }

    [Theory]
    // §22.2.1 early errors and reserved syntax.
    [InlineData(@"[a~~b]")]          // reserved double punctuator
    [InlineData(@"[a##b]")]
    [InlineData(@"[\w&&\d--a]")]     // an expression may not mix operators
    [InlineData(@"[a--b&&c]")]
    [InlineData(@"[(]")]             // a syntax character must be escaped
    [InlineData(@"[)]")]
    [InlineData(@"[{]")]
    [InlineData(@"[^\q{ab}]")]       // a negated class may not contain strings
    [InlineData(@"[^\p{RGI_Emoji}]")]
    [InlineData(@"[\P{RGI_Emoji}]")] // nor may a property of strings be complemented
    [InlineData(@"[\q{ab}")]         // unterminated
    [InlineData(@"[[a]")]
    public void InvalidClassSet_IsASyntaxError(string pattern)
        => Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(pattern, "v"));

    [Theory]
    // An escaped syntax character or reserved punctuator is itself.
    [InlineData(@"[\(]", "(")]
    [InlineData(@"[\-]", "-")]
    [InlineData(@"[\&]", "&")]
    [InlineData(@"[\|]", "|")]
    [InlineData(@"[\]]", "]")]
    public void EscapedPunctuator_IsALiteralMember(string pattern, string input)
        => Assert.Equal(input, Match(pattern, "v", input));

    [Fact(Timeout = 600000)]
    public void NestedClassDoesNotEndTheGroupCount()
    {
        // The pre-scan counts capturing groups before parsing; a `v`-mode class nests, so
        // the ']' of an inner class must not put the scan back outside the class.
        var re = new BroilerRegex(@"([[a-c][x-z]])", "v");
        Assert.Equal(1, re.CaptureCount);
        Assert.Equal("y", re.Match("y").Groups[1].Value);
    }
}
