using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The engines' literal tests, transcribed as data (SJL-11's plan, row 2.8: "the engines' literal tests"; task 2.8 of
/// SJL-11.2): each case a test of Broiler.VM or Broiler.JS that refuses or admits a regular-expression literal, or a
/// <c>RegExp</c> pattern that engine checks with the same grammar, read as the validate entry's input, a pattern and a flags
/// text, with the answer that engine's test expects and the code this library answers with. Broiler.VM's literal check is
/// the binder's <c>CheckRegExp</c> (<c>JsBinder.EarlyErrors.cs</c>), which compiles a literal with the same matcher its
/// <c>RegExp</c> constructor uses, so its constructor tests of pattern refusal are its literal tests too; Broiler.JS's is
/// the scanner's <c>RegExpValidator</c> with the parser's early errors.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sources, at the commits read.</b> Broiler.VM at <c>58b0c84</c>:
/// <c>src/compositions/Broiler.VM.Composition.JavaScript.SliceCompiler/BinderChecks.cs</c> lines 119 to 121 (the three
/// literal rows, each a <c>2101</c> at the literal), <c>src/tests/cli/runs/regexp-pattern-modifiers.js</c> line 20,
/// <c>src/tests/cli/runs/the-v-flag.js</c> lines 11 and 14, and <c>src/tests/differential/the-unicode-property-escapes.js</c>
/// cases 34 to 61 (lines 136 to 166; case 56 a literal through <c>new Function</c>), each a <c>SyntaxError</c> the files'
/// expected outputs name. Broiler.JS at <c>238069ab</c>:
/// <c>Broiler.JS/Broiler.JavaScript.Parser.Tests/RegExpLiteralEarlyErrorTests.cs</c> (every case) and <c>ParserTests.cs</c>'s
/// <c>ParseProgram_RegExpLiteral_Allows_UnicodePropertyEscapes</c> and
/// <c>ParseProgram_RegularExpressionClass_AcceptsAnnexBIdentityEscapes</c> (line 420 and 977).
/// </para>
/// <para>
/// <b>Where an engine's test and this library differ: one case.</b> Broiler.JS's <c>ParserTests.cs:420</c> admits the
/// literal <c>/[\q{/}--[a]]/v</c>, which this library refuses: under <c>v</c> a <c>/</c> is a ClassSetSyntaxCharacter
/// (ECMA-262 §22.2.1), which no ClassSetCharacter of a <c>\q{…}</c> may be unescaped, and V8 (node 22.22.0) refuses it
/// alike ("Invalid character in character class"). Broiler.JS's scanner does not read a <c>v</c>-mode class's grammar,
/// so its parser admits the literal: the defect is that test's expectation, kept apart below
/// (<see cref="Where_an_engine_s_test_and_the_specification_differ_the_library_answers_the_specification"/>) and listed in
/// the record of task 2.8. Every other refusal there is a refusal here and every admission an admission.
/// (Broiler.VM's property-escape probe's own comment says the <c>v</c> flag "stays refused"; its case 60,
/// <c>[\p{L}]</c> under <c>v</c>, is admitted since that realm's phase F2, as its expected output says, and here.)
/// </para>
/// </remarks>
public class EngineLiteralTests
{
    /// <summary>A case: where it comes from, the pattern, the flags text, and the code a refusal answers, or null for an admission.</summary>
    public static TheoryData<string, string, string, RegexDiagnosticCode?> Cases => new()
    {
        // Broiler.VM, BinderChecks.cs:119-121: a literal's pattern and its flags, each refused at the literal (2101@1:1).
        { "vm BinderChecks.cs:119 /(/", "(", "", RegexDiagnosticCode.UnterminatedGroup },
        { "vm BinderChecks.cs:120 /a/gg", "a", "gg", RegexDiagnosticCode.DuplicateFlag },
        { "vm BinderChecks.cs:121 /a/uv", "a", "uv", RegexDiagnosticCode.UnicodeWithUnicodeSets },

        // Broiler.VM, regexp-pattern-modifiers.js:20: five refusals of a modifier group.
        { "vm regexp-pattern-modifiers.js:20 (?ii:a)", "(?ii:a)", "", RegexDiagnosticCode.ModifierRepeated },
        { "vm regexp-pattern-modifiers.js:20 (?i-i:a)", "(?i-i:a)", "", RegexDiagnosticCode.ModifierAddedAndRemoved },
        { "vm regexp-pattern-modifiers.js:20 (?-:a)", "(?-:a)", "", RegexDiagnosticCode.ModifiersEmpty },
        { "vm regexp-pattern-modifiers.js:20 (?x:a)", "(?x:a)", "", RegexDiagnosticCode.InvalidGroup },
        { "vm regexp-pattern-modifiers.js:20 (?i)a", "(?i)a", "", RegexDiagnosticCode.InvalidGroup },

        // Broiler.VM, the-v-flag.js:11 and :14: eight refusals under v, and u with v.
        { "vm the-v-flag.js:11 [a-z&&b]", "[a-z&&b]", "v", RegexDiagnosticCode.ClassSetMixedOperators },
        { "vm the-v-flag.js:11 [a&&&b]", "[a&&&b]", "v", RegexDiagnosticCode.ClassSetAmpersandAfterIntersection },
        { "vm the-v-flag.js:11 [^\\p{RGI_Emoji}]", @"[^\p{RGI_Emoji}]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings },
        { "vm the-v-flag.js:11 \\P{RGI_Emoji}", @"\P{RGI_Emoji}", "v", RegexDiagnosticCode.NegatedPropertyOfStrings },
        { "vm the-v-flag.js:11 [(]", "[(]", "v", RegexDiagnosticCode.ClassSetUnescapedSyntaxCharacter },
        { "vm the-v-flag.js:11 [a!!b]", "[a!!b]", "v", RegexDiagnosticCode.ClassSetReservedDoublePunctuator },
        { "vm the-v-flag.js:11 [a&&b--c]", "[a&&b--c]", "v", RegexDiagnosticCode.ClassSetMixedOperators },
        { "vm the-v-flag.js:11 [\\q{ab}-c]", @"[\q{ab}-c]", "v", RegexDiagnosticCode.ClassSetUnescapedSyntaxCharacter },
        { "vm the-v-flag.js:14 x under uv", "x", "uv", RegexDiagnosticCode.UnicodeWithUnicodeSets },

        // Broiler.VM, the-unicode-property-escapes.js, cases 34-48: misspellings, loose matching, unknown properties.
        { "vm property-escapes 34", @"\p{letter}", "u", RegexDiagnosticCode.UnknownLonePropertyName },
        { "vm property-escapes 35", @"\p{Letter }", "u", RegexDiagnosticCode.UnknownLonePropertyName },
        { "vm property-escapes 36", @"\p{ Letter}", "u", RegexDiagnosticCode.UnknownLonePropertyName },
        { "vm property-escapes 37", @"\p{Script_Extensions=greek}", "u", RegexDiagnosticCode.UnknownPropertyValue },
        { "vm property-escapes 38", @"\p{script_extensions=Greek}", "u", RegexDiagnosticCode.UnknownPropertyName },
        { "vm property-escapes 39", @"\p{Scx=Greek}", "u", RegexDiagnosticCode.UnknownPropertyName },
        { "vm property-escapes 40", @"\p{scx:Greek}", "u", RegexDiagnosticCode.UnknownLonePropertyName },
        { "vm property-escapes 41", @"\p{IsLetter}", "u", RegexDiagnosticCode.UnknownLonePropertyName },
        { "vm property-escapes 42", @"\p{Lowercase_Letter=Y}", "u", RegexDiagnosticCode.UnknownPropertyName },
        { "vm property-escapes 43", @"\p{ASCII=Y}", "u", RegexDiagnosticCode.UnknownPropertyName },
        { "vm property-escapes 44", @"\p{Block=Basic_Latin}", "u", RegexDiagnosticCode.UnknownPropertyName },
        { "vm property-escapes 45", @"\p{Line_Break=Alphabetic}", "u", RegexDiagnosticCode.UnknownPropertyName },
        { "vm property-escapes 46", @"\p{WSpace}", "u", RegexDiagnosticCode.UnknownLonePropertyName },
        { "vm property-escapes 47", @"\p{gc=Greek}", "u", RegexDiagnosticCode.UnknownPropertyValue },
        { "vm property-escapes 48", @"\p{sc=Lu}", "u", RegexDiagnosticCode.UnknownPropertyValue },

        // Cases 49-56: the escape's own grammar; 56 a literal, compiled through new Function.
        { "vm property-escapes 49", @"\pL", "u", RegexDiagnosticCode.PropertyEscapeWithoutBrace },
        { "vm property-escapes 50", @"\p{L", "u", RegexDiagnosticCode.UnterminatedPropertyEscape },
        { "vm property-escapes 51", @"\p{}", "u", RegexDiagnosticCode.UnknownLonePropertyName },
        { "vm property-escapes 52", @"\P", "u", RegexDiagnosticCode.PropertyEscapeWithoutBrace },
        { "vm property-escapes 53", @"[\p{L}-z]", "u", RegexDiagnosticCode.ClassRangeEndpointIsClass },
        { "vm property-escapes 54", @"\\p\{L\}", "u", null },
        { "vm property-escapes 54", @"[\\p]", "u", null },
        { "vm property-escapes 55", @"\\\p{L}", "u", null },
        { "vm property-escapes 56 (a literal)", @"\p{letter}", "u", RegexDiagnosticCode.UnknownLonePropertyName },

        // Cases 57-61: outside u, \p is Annex B's identity escape; properties of strings need v; u with v.
        { "vm property-escapes 57", @"\p{L}", "", null },
        { "vm property-escapes 57", @"\P", "", null },
        { "vm property-escapes 57", @"[\p{L}]", "", null },
        { "vm property-escapes 58", @"\p{RGI_Emoji}", "u", RegexDiagnosticCode.PropertyOfStringsOutsideUnicodeSets },
        { "vm property-escapes 59", @"\p{Basic_Emoji}", "u", RegexDiagnosticCode.PropertyOfStringsOutsideUnicodeSets },
        { "vm property-escapes 60", @"[\p{L}]", "v", null },
        { "vm property-escapes 61", @"\p{L}", "uv", RegexDiagnosticCode.UnicodeWithUnicodeSets },

        // Broiler.JS, RegExpLiteralEarlyErrorTests: a group name that is no RegExpIdentifierName.
        { "js group name /(?<42a>a)/", "(?<42a>a)", "", RegexDiagnosticCode.GroupNameInvalidStart },
        { "js group name /(?<42a>a)/u", "(?<42a>a)", "u", RegexDiagnosticCode.GroupNameInvalidStart },
        { "js group name /(?<:a>a)/", "(?<:a>a)", "", RegexDiagnosticCode.GroupNameInvalidStart },
        { "js group name /(?<a:>a)/u", "(?<a:>a)", "u", RegexDiagnosticCode.GroupNameInvalidPart },
        { "js group name /(?<\u2764>a)/", "(?<\u2764>a)", "", RegexDiagnosticCode.GroupNameInvalidStart },
        { "js group name /(?<U+104A4>a)/u", "(?<\U000104A4>a)", "u", RegexDiagnosticCode.GroupNameInvalidStart },
        { "js group name /(?<$\u275E>a)/", "(?<$\u275E>a)", "", RegexDiagnosticCode.GroupNameInvalidPart },
        { "js group name /(?<a\\uD83D\\uDF12>.)/", @"(?<a\uD83D\uDF12>.)", "", RegexDiagnosticCode.GroupNameInvalidPart },
        { "js group name /(?<a\\uD801>.)/u", @"(?<a\uD801>.)", "u", RegexDiagnosticCode.GroupNameInvalidPart },
        { "js group name /(?<a\\uDCA4>.)/", @"(?<a\uDCA4>.)", "", RegexDiagnosticCode.GroupNameInvalidPart },
        { "js group name /(?<a\\u{1F08B}>.)/", @"(?<a\u{1F08B}>.)", "", RegexDiagnosticCode.GroupNameInvalidPart },
        { "js group name /(?<a\\u{10FFFF}>.)/", @"(?<a\u{10FFFF}>.)", "", RegexDiagnosticCode.GroupNameInvalidPart },
        { "js group name /(?<a\\>.)/", @"(?<a\>.)", "", RegexDiagnosticCode.GroupNameInvalidEscape },
        { "js group name /(?<\\>.)/", @"(?<\>.)", "", RegexDiagnosticCode.GroupNameInvalidEscape },
        { "js group name /(?<>a)/", "(?<>a)", "", RegexDiagnosticCode.EmptyGroupName },

        // A name declared twice in one alternative.
        { "js duplicate /(?<a>a)(?<a>a)/", "(?<a>a)(?<a>a)", "", RegexDiagnosticCode.DuplicateGroupName },
        { "js duplicate /(?<a>a)(?<b>b)(?<a>a)/u", "(?<a>a)(?<b>b)(?<a>a)", "u", RegexDiagnosticCode.DuplicateGroupName },
        { "js duplicate /(?<a>a)((?<a>b))/", "(?<a>a)((?<a>b))", "", RegexDiagnosticCode.DuplicateGroupName },

        // An incomplete named reference, with a group name or under u, in a class too.
        { "js reference /(?<a>.)\\k/", @"(?<a>.)\k", "", RegexDiagnosticCode.NamedReferenceWithoutName },
        { "js reference /\\k(?<a>.)/", @"\k(?<a>.)", "", RegexDiagnosticCode.NamedReferenceWithoutName },
        { "js reference /(?<a>.)\\k<a/", @"(?<a>.)\k<a", "", RegexDiagnosticCode.UnterminatedGroupName },
        { "js reference /(?<a>.)\\k<1>/", @"(?<a>.)\k<1>", "", RegexDiagnosticCode.GroupNameInvalidStart },
        // Broiler.JS's test refuses \k in a class beside a group name; B.1.2 admits it, the letter, as a class reads
        // ~NamedCaptureGroups: decision D-s5 of Broiler.VM's JSD-0081 makes the refusal a correction of Broiler.JS, and
        // this library accepts it since task 2.12 of SJL-11.2, where it answered NamedReferenceInClass.
        { "js reference /(?<a>.)[\\k]/", @"(?<a>.)[\k]", "", null },

        // Valid group names and references.
        { "js valid /(?<a>a)|(?<a>b)/", "(?<a>a)|(?<a>b)", "", null },
        { "js valid /(?:(?<a>a)|(?<a>b))\\k<a>/", @"(?:(?<a>a)|(?<a>b))\k<a>", "", null },
        { "js valid /(?<$>a)(?<_\u200C\u200D>b)/", "(?<$>a)(?<_\u200C\u200D>b)", "", null },
        { "js valid /(?<\\u{1d4d1}>a)/u", @"(?<\u{1d4d1}>a)", "u", null },
        { "js valid /(?<U+1D4D1>a)/", "(?<\U0001D4D1>a)", "", null },
        { "js valid /\\k/", @"\k", "", null },
        { "js valid /\\k<a>/", @"\k<a>", "", null },
        { "js valid /(?<=a)\\k/", @"(?<=a)\k", "", null },
        { "js valid /[(?<a>.)]\\k/", @"[(?<a>.)]\k", "", null },

        // Under v, a negated class that may contain strings.
        { "js negated /[^\\p{RGI_Emoji}]/v", @"[^\p{RGI_Emoji}]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings },
        { "js negated /[^\\p{Basic_Emoji}]/v", @"[^\p{Basic_Emoji}]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings },
        { "js negated /[^\\p{Emoji_Keycap_Sequence}]/v", @"[^\p{Emoji_Keycap_Sequence}]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings },
        { "js negated /[^a\\p{RGI_Emoji_Flag_Sequence}]/v", @"[^a\p{RGI_Emoji_Flag_Sequence}]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings },
        { "js negated /[^\\q{ab}]/v", @"[^\q{ab}]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings },
        { "js negated /[^\\q{}]/v", @"[^\q{}]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings },
        { "js negated /[^\\q{a|bc}]/v", @"[^\q{a|bc}]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings },
        { "js negated /[^[\\p{RGI_Emoji}]]/v", @"[^[\p{RGI_Emoji}]]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings },
        { "js negated /[^\\p{RGI_Emoji}--a]/v", @"[^\p{RGI_Emoji}--a]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings },
        { "js negated /[^\\p{RGI_Emoji}&&\\q{ab}]/v", @"[^\p{RGI_Emoji}&&\q{ab}]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings },
        { "js negated /[a[^\\p{RGI_Emoji}]]/v", @"[a[^\p{RGI_Emoji}]]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings },

        // A negated class that contains no string.
        { "js no string /[^\\p{RGI_Emoji}&&a]/v", @"[^\p{RGI_Emoji}&&a]", "v", null },
        { "js no string /[^a&&\\p{RGI_Emoji}]/v", @"[^a&&\p{RGI_Emoji}]", "v", null },
        { "js no string /[^a--\\p{RGI_Emoji}]/v", @"[^a--\p{RGI_Emoji}]", "v", null },
        { "js no string /[^\\q{a|b}]/v", @"[^\q{a|b}]", "v", null },
        { "js no string /[^\\q{a|U+1F600}]/v", "[^\\q{a|\U0001F600}]", "v", null },
        { "js no string /[^[^\\p{RGI_Emoji}&&a]]/v", @"[^[^\p{RGI_Emoji}&&a]]", "v", null },
        { "js no string /[^[\\p{RGI_Emoji}&&a]]/v", @"[^[\p{RGI_Emoji}&&a]]", "v", null },
        { "js no string /[^\\p{Lu}a-z]/v", @"[^\p{Lu}a-z]", "v", null },
        { "js no string /[\\p{RGI_Emoji}]/v", @"[\p{RGI_Emoji}]", "v", null },
        { "js no string /[^\\p{Script=Latin}]/v", @"[^\p{Script=Latin}]", "v", null },

        // Under u, a class escape at a range's end.
        { "js range /[--\\p{Hex}]/u", @"[--\p{Hex}]", "u", RegexDiagnosticCode.ClassRangeEndpointIsClass },
        { "js range /[\\p{Hex}-U+FFFF]/u", "[\\p{Hex}-\uFFFF]", "u", RegexDiagnosticCode.ClassRangeEndpointIsClass },
        { "js range /[U+FFFF-\\p{Hex}]/u", "[\uFFFF-\\p{Hex}]", "u", RegexDiagnosticCode.ClassRangeEndpointIsClass },
        { "js range /[\\p{Hex}--]/u", @"[\p{Hex}--]", "u", RegexDiagnosticCode.ClassRangeEndpointIsClass },
        { "js range /[a-\\d]/u", @"[a-\d]", "u", RegexDiagnosticCode.ClassRangeEndpointIsClass },
        { "js range /[\\w-a]/u", @"[\w-a]", "u", RegexDiagnosticCode.ClassRangeEndpointIsClass },

        // A class without such a range.
        { "js no range /[\\p{Hex}-]/u", @"[\p{Hex}-]", "u", null },
        { "js no range /[-\\p{Hex}]/u", @"[-\p{Hex}]", "u", null },
        { "js no range /[\\w-]/u", @"[\w-]", "u", null },
        { "js no range /[a\\-\\d]/u", @"[a\-\d]", "u", null },
        { "js no range /[\\--\\-]/u", @"[\--\-]", "u", null },
        { "js no range /[\\p{Hex}\\-a]/u", @"[\p{Hex}\-a]", "u", null },
        { "js no range /[a-z\\d]/u", @"[a-z\d]", "u", null },
        { "js no range /[\\p{Hex}-U+FFFF]/", "[\\p{Hex}-\uFFFF]", "", null },

        // ParserTests.cs:420: property escapes and v-mode classes in a literal.
        { "js ParserTests:420 /^\\p{Any}+$/u", @"^\p{Any}+$", "u", null },
        { "js ParserTests:420 /\\P{ASCII}/u", @"\P{ASCII}", "u", null },
        { "js ParserTests:420 /[\\p{Alphabetic}\\P{ASCII_Hex_Digit}]/v", @"[\p{Alphabetic}\P{ASCII_Hex_Digit}]", "v", null },
        { "js ParserTests:420 /^[[0-9]--\\q{0|2|4|9\\uFE0F\\u20E3}]+$/v", @"^[[0-9]--\q{0|2|4|9\uFE0F\u20E3}]+$", "v", null },

        // ParserTests.cs:977: Annex B identity escapes in a class and out of one, under g.
        { "js ParserTests:977 /[\\_]/g", @"[\_]", "g", null },
        { "js ParserTests:977 /[^0-9a-zA-Z\\-\\_]/g", @"[^0-9a-zA-Z\-\_]", "g", null },
        { "js ParserTests:977 /[\\C\\P]/g", @"[\C\P]", "g", null },
        { "js ParserTests:977 /\\_/g", @"\_", "g", null },
        { "js ParserTests:977 /\\-/g", @"\-", "g", null },
        { "js ParserTests:977 /[a-z]/g", "[a-z]", "g", null },
        { "js ParserTests:977 /[a\\-z]/g", @"[a\-z]", "g", null },
    };

    /// <summary>
    /// Broiler.JS's <c>ParserTests.cs:420</c> admits <c>/[\q{/}--[a]]/v</c>; ECMA-262 and V8 refuse it (the remarks): this
    /// library answers the specification, the <c>/</c> refused where it stands, unescaped in a <c>\q{…}</c>, and admits
    /// the same literal with the <c>/</c> escaped.
    /// </summary>
    [Fact]
    public void Where_an_engine_s_test_and_the_specification_differ_the_library_answers_the_specification()
    {
        Assert.Equal(RegexStatus.SyntaxError, BroilerRegex.Validate(@"[\q{/}--[a]]", "v", UnicodeTables.Instance, 512, out var diagnostic, out _));
        Assert.Equal((RegexDiagnosticCode.ClassSetUnescapedSyntaxCharacter, 4, "/"), (diagnostic.Code, diagnostic.Position, diagnostic.Argument));
        Assert.Equal(RegexStatus.Success, BroilerRegex.Validate(@"[\q{\/}--[a]]", "v", UnicodeTables.Instance, 512, out _, out _));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void An_engine_s_literal_test_is_answered_as_that_engine_s_test_expects(string source, string pattern, string flags, RegexDiagnosticCode? refusal)
    {
        var status = BroilerRegex.Validate(pattern, flags, UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out var diagnostic, out _);
        if (refusal is null)
        {
            Assert.True(status == RegexStatus.Success, source + ": refused with " + diagnostic.MessageId + " at " + diagnostic.Position);
            _ = new BroilerRegex(pattern, flags, UnicodeTables.Instance);
            return;
        }

        Assert.True(status == RegexStatus.SyntaxError, source + ": answered " + status);
        Assert.Equal(refusal, diagnostic.Code);

        // The constructor, the engines' path for a RegExp, refuses with the same diagnostic.
        var thrown = Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(pattern, flags, UnicodeTables.Instance));
        Assert.Equal(diagnostic.Code, thrown.Diagnostic.Code);
        Assert.Equal(diagnostic.Position, thrown.Diagnostic.Position);
    }
}
