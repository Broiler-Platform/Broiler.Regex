using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Broiler.Regex.Compiling;
using Broiler.Regex.Parsing;
using Broiler.Regex.Unicode;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// The validate entry (task 2.8 of SJL-11.2; decisions D9 and D18 of Broiler.VM's JSD-0081; bundle <c>sjl-11-001</c>'s
/// checks A-5 and A-11): parse and early errors only, under the caller's nesting limit, with no expansion and no meter,
/// answering a status with a <see cref="RegexDiagnostic"/>, a code, a message identifier and its argument, and a position;
/// the code table it answers from; and its agreement with <see cref="BroilerRegex.Compile"/>, whose
/// <see cref="RegexSyntaxException"/> carries the same diagnostic.
/// </summary>
public class ValidateEntryTests
{
    /// <summary>
    /// One refusal of each code: a pattern, its flags text, and the code, position and argument the entry answers, with
    /// the condition each code documents. A position of a flags code is in the flags text.
    /// </summary>
    public static TheoryData<string, string, RegexDiagnosticCode, int, string?> Refusals => new()
    {
        { "a", "gx", RegexDiagnosticCode.UnknownFlag, 1, "x" },
        { "a", "uvx", RegexDiagnosticCode.UnknownFlag, 2, "x" },
        { "a", "gg", RegexDiagnosticCode.DuplicateFlag, 1, "g" },
        { "a", "uv", RegexDiagnosticCode.UnicodeWithUnicodeSets, 1, null },
        { "a)", "", RegexDiagnosticCode.UnmatchedParenthesis, 1, null },
        { "(a", "", RegexDiagnosticCode.UnterminatedGroup, 2, null },
        { "(?x)", "", RegexDiagnosticCode.InvalidGroup, 1, null },
        { "(?i)a", "", RegexDiagnosticCode.InvalidGroup, 3, null },
        { "*a", "", RegexDiagnosticCode.NothingToRepeat, 0, "*" },
        { "a|+", "", RegexDiagnosticCode.NothingToRepeat, 2, "+" },
        { "^*", "", RegexDiagnosticCode.QuantifiedAssertion, 1, null },
        { "(?<=a)?", "", RegexDiagnosticCode.QuantifiedAssertion, 6, null },
        { "(?=a)*", "u", RegexDiagnosticCode.QuantifiedAssertion, 5, null },
        { "a{2,1}", "", RegexDiagnosticCode.QuantifierOutOfOrder, 1, null },
        { "{2}", "", RegexDiagnosticCode.BracedQuantifierWithoutAtom, 0, null },
        { "a|{1,}", "", RegexDiagnosticCode.BracedQuantifierWithoutAtom, 2, null },
        { "]", "u", RegexDiagnosticCode.LoneSyntaxCharacter, 0, "]" },
        { "a{", "v", RegexDiagnosticCode.LoneSyntaxCharacter, 1, "{" },
        { "a\\", "", RegexDiagnosticCode.TrailingBackslash, 2, null },
        { "[a\\", "u", RegexDiagnosticCode.TrailingBackslash, 3, null },
        { "\\a", "u", RegexDiagnosticCode.InvalidIdentityEscape, 1, "a" },
        { "\\c1", "u", RegexDiagnosticCode.InvalidControlEscape, 2, null },
        { "\\x4g", "u", RegexDiagnosticCode.InvalidHexEscape, 3, null },
        { "(?<\\u00G1>a)", "", RegexDiagnosticCode.InvalidHexEscape, 7, null },
        { "\\u{}", "u", RegexDiagnosticCode.InvalidBracedEscape, 3, null },
        { "\\u{41", "u", RegexDiagnosticCode.InvalidBracedEscape, 5, null },
        { "\\u{110000}", "u", RegexDiagnosticCode.CodePointOutOfRange, 8, null },
        { "(a)\\2", "u", RegexDiagnosticCode.BackreferenceOutOfRange, 4, "2" },
        { "\\k<b>(?<a>x)", "u", RegexDiagnosticCode.UndefinedGroupName, 0, "b" },
        { "(?<a>x)\\k<\\u0062>", "", RegexDiagnosticCode.UndefinedGroupName, 7, "b" },
        { "(?<a>x)\\k", "", RegexDiagnosticCode.NamedReferenceWithoutName, 9, null },
        { "\\k", "u", RegexDiagnosticCode.NamedReferenceWithoutName, 2, null },
        { "(?<a>x)[\\k<a>]", "", RegexDiagnosticCode.NamedReferenceInClass, 9, null },
        { "(?<a", "", RegexDiagnosticCode.UnterminatedGroupName, 4, null },
        { "(?<a\\x41>.)", "", RegexDiagnosticCode.GroupNameInvalidEscape, 4, null },
        { "(?<1>.)", "", RegexDiagnosticCode.GroupNameInvalidStart, 3, null },
        { "(?<\\u0031>.)", "u", RegexDiagnosticCode.GroupNameInvalidStart, 3, null },
        { "(?<a-b>.)", "", RegexDiagnosticCode.GroupNameInvalidPart, 4, null },
        { "(?<a\\uD800>.)", "", RegexDiagnosticCode.GroupNameInvalidPart, 4, null },
        { "(?<>.)", "", RegexDiagnosticCode.EmptyGroupName, 3, null },
        { "(?<a>x)(?<a>y)", "", RegexDiagnosticCode.DuplicateGroupName, 7, "a" },
        { "(?<a>x)(?<\\u0061>y)", "u", RegexDiagnosticCode.DuplicateGroupName, 7, "a" },
        { "(?:(?<a>x)|(?<a>y))(?<a>z)", "", RegexDiagnosticCode.DuplicateGroupName, 19, "a" },
        { "(?ii:a)", "", RegexDiagnosticCode.ModifierRepeated, 3, "i" },
        { "(?i-ss:a)", "", RegexDiagnosticCode.ModifierRepeated, 5, "s" },
        { "(?-:a)", "", RegexDiagnosticCode.ModifiersEmpty, 3, null },
        { "(?i-i:a)", "", RegexDiagnosticCode.ModifierAddedAndRemoved, 5, null },
        { "[a", "", RegexDiagnosticCode.UnterminatedClass, 2, null },
        { "[a", "v", RegexDiagnosticCode.UnterminatedClass, 0, null },
        { "[[a]", "v", RegexDiagnosticCode.UnterminatedClass, 0, null },
        { "[a&&", "v", RegexDiagnosticCode.UnterminatedClass, 4, null },
        { "[z-a]", "", RegexDiagnosticCode.ClassRangeOutOfOrder, 1, "z-a" },
        { "x[\\u0062-\\u0061]", "u", RegexDiagnosticCode.ClassRangeOutOfOrder, 2, "\\u0062-\\u0061" },
        { "[a[z-a]]", "v", RegexDiagnosticCode.ClassRangeOutOfOrder, 3, "z-a" },
        { "[\\d-z]", "u", RegexDiagnosticCode.ClassRangeEndpointIsClass, 3, null },
        { "[a-\\p{L}]", "u", RegexDiagnosticCode.ClassRangeEndpointIsClass, 2, null },
        { "[^\\q{ab}]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings, 0, null },
        { "[a[^\\q{ab}]]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings, 2, null },
        { "[^\\q{ab}--\\q{ab}]", "v", RegexDiagnosticCode.NegatedClassMayContainStrings, 0, null },
        { "[a-z&&b]", "v", RegexDiagnosticCode.ClassSetMixedOperators, 1, null },
        { "[a&&b--c]", "v", RegexDiagnosticCode.ClassSetMixedOperators, 1, null },
        { "[a--bc]", "v", RegexDiagnosticCode.ClassSetMixedOperators, 1, null },
        { "[a&&&b]", "v", RegexDiagnosticCode.ClassSetAmpersandAfterIntersection, 4, null },
        { "[a-\\d]", "v", RegexDiagnosticCode.ClassSetRangeEndpointNotCharacter, 1, null },
        { "[(]", "v", RegexDiagnosticCode.ClassSetUnescapedSyntaxCharacter, 1, "(" },
        { "[\\q{a|}}]", "v", RegexDiagnosticCode.ClassSetUnescapedSyntaxCharacter, 7, "}" },
        { "[a!!b]", "v", RegexDiagnosticCode.ClassSetReservedDoublePunctuator, 2, "!!" },
        { "[\\qa]", "v", RegexDiagnosticCode.ClassStringDisjunctionWithoutBrace, 3, null },
        { "[\\q{a", "v", RegexDiagnosticCode.UnterminatedClassStringDisjunction, 2, null },
        { "\\pL", "u", RegexDiagnosticCode.PropertyEscapeWithoutBrace, 2, null },
        { "\\p{L", "u", RegexDiagnosticCode.UnterminatedPropertyEscape, 4, null },
        { "\\p{script=Greek}", "u", RegexDiagnosticCode.UnknownPropertyName, 1, "script=Greek" },
        { "\\p{ASCII=Y}", "u", RegexDiagnosticCode.UnknownPropertyName, 1, "ASCII=Y" },
        { "\\p{Script=greek}", "u", RegexDiagnosticCode.UnknownPropertyValue, 1, "Script=greek" },
        { "\\p{gc=Greek}", "v", RegexDiagnosticCode.UnknownPropertyValue, 1, "gc=Greek" },
        { "\\p{letter}", "u", RegexDiagnosticCode.UnknownLonePropertyName, 1, "letter" },
        { "\\P{}", "v", RegexDiagnosticCode.UnknownLonePropertyName, 1, "" },
        { "\\p{RGI_Emoji}", "u", RegexDiagnosticCode.PropertyOfStringsOutsideUnicodeSets, 1, "RGI_Emoji" },
        { "[\\p{Basic_Emoji}]", "u", RegexDiagnosticCode.PropertyOfStringsOutsideUnicodeSets, 2, "Basic_Emoji" },
        { "\\P{RGI_Emoji}", "v", RegexDiagnosticCode.NegatedPropertyOfStrings, 1, "RGI_Emoji" },
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public void Every_refusal_is_answered_with_its_code_identifier_argument_and_position(
        string pattern, string flags, RegexDiagnosticCode code, int position, string? argument)
    {
        var status = BroilerRegex.Validate(pattern, flags, UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit, out var diagnostic, out var limit);
        Assert.Equal(RegexStatus.SyntaxError, status);
        Assert.Equal(RegexLimit.None, limit);
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal(RegexDiagnostic.MessageIdOf(code), diagnostic.MessageId);
        Assert.Equal(position, diagnostic.Position);
        Assert.Equal(argument, diagnostic.Argument);
        Assert.NotEqual("", diagnostic.Message);

        // The constructor over the flags text, and Compile over parsed flags, refuse with the same diagnostic.
        var thrown = Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(pattern, flags, UnicodeTables.Instance));
        AssertSame(diagnostic, thrown.Diagnostic);
        Assert.Equal(diagnostic.Message, thrown.Message);
        Assert.Equal(IsOfFlags(code) ? -1 : position, thrown.Position);
        if (!IsOfFlags(code))
        {
            var compiled = Assert.Throws<RegexSyntaxException>(() => BroilerRegex.Compile(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance,
                BroilerRegex.MaximumNestingLimit, null, null, out _, out _));
            AssertSame(diagnostic, compiled.Diagnostic);
        }
    }

    [Fact]
    public void Every_code_is_refused_by_a_case_of_the_table()
    {
        var reached = Refusals.Select(static row => (RegexDiagnosticCode)row[2]).Append(RegexDiagnosticCode.NestingLimit).ToHashSet();
        var missing = Enum.GetValues<RegexDiagnosticCode>().Where(code => code != RegexDiagnosticCode.None && !reached.Contains(code));
        Assert.Empty(missing);
    }

    /// <summary>
    /// The code table, pinned: each code's number and message identifier. A code's number and identifier never change once
    /// published (its documentation), so a change here is a change of the contract, made with a record.
    /// </summary>
    [Fact]
    public void The_code_table_is_pinned()
    {
        (RegexDiagnosticCode Code, int Number, string Id)[] table =
        [
            (RegexDiagnosticCode.None, 0, ""),
            (RegexDiagnosticCode.UnknownFlag, 1, "regexp.flags.unknown"),
            (RegexDiagnosticCode.DuplicateFlag, 2, "regexp.flags.duplicate"),
            (RegexDiagnosticCode.UnicodeWithUnicodeSets, 3, "regexp.flags.unicode-with-unicode-sets"),
            (RegexDiagnosticCode.UnmatchedParenthesis, 10, "regexp.group.unmatched-close"),
            (RegexDiagnosticCode.UnterminatedGroup, 11, "regexp.group.unterminated"),
            (RegexDiagnosticCode.InvalidGroup, 12, "regexp.group.invalid"),
            (RegexDiagnosticCode.NothingToRepeat, 13, "regexp.quantifier.nothing-to-repeat"),
            (RegexDiagnosticCode.QuantifiedAssertion, 14, "regexp.quantifier.after-assertion"),
            (RegexDiagnosticCode.QuantifierOutOfOrder, 15, "regexp.quantifier.out-of-order"),
            (RegexDiagnosticCode.BracedQuantifierWithoutAtom, 16, "regexp.quantifier.braced-without-atom"),
            (RegexDiagnosticCode.LoneSyntaxCharacter, 17, "regexp.pattern.lone-syntax-character"),
            (RegexDiagnosticCode.TrailingBackslash, 20, "regexp.escape.trailing-backslash"),
            (RegexDiagnosticCode.InvalidIdentityEscape, 21, "regexp.escape.invalid-identity"),
            (RegexDiagnosticCode.InvalidControlEscape, 22, "regexp.escape.invalid-control"),
            (RegexDiagnosticCode.InvalidHexEscape, 23, "regexp.escape.invalid-hex"),
            (RegexDiagnosticCode.InvalidBracedEscape, 24, "regexp.escape.invalid-braced"),
            (RegexDiagnosticCode.CodePointOutOfRange, 25, "regexp.escape.code-point-out-of-range"),
            (RegexDiagnosticCode.BackreferenceOutOfRange, 30, "regexp.backreference.group-number-too-large"),
            (RegexDiagnosticCode.UndefinedGroupName, 31, "regexp.backreference.undefined-group-name"),
            (RegexDiagnosticCode.NamedReferenceWithoutName, 32, "regexp.backreference.missing-group-name"),
            (RegexDiagnosticCode.NamedReferenceInClass, 33, "regexp.class.named-reference"),
            (RegexDiagnosticCode.UnterminatedGroupName, 40, "regexp.group-name.unterminated"),
            (RegexDiagnosticCode.GroupNameInvalidEscape, 41, "regexp.group-name.invalid-escape"),
            (RegexDiagnosticCode.GroupNameInvalidStart, 42, "regexp.group-name.invalid-start"),
            (RegexDiagnosticCode.GroupNameInvalidPart, 43, "regexp.group-name.invalid-part"),
            (RegexDiagnosticCode.EmptyGroupName, 44, "regexp.group-name.empty"),
            (RegexDiagnosticCode.DuplicateGroupName, 45, "regexp.group-name.duplicate"),
            (RegexDiagnosticCode.ModifierRepeated, 50, "regexp.modifiers.repeated-flag"),
            (RegexDiagnosticCode.ModifiersEmpty, 51, "regexp.modifiers.empty"),
            (RegexDiagnosticCode.ModifierAddedAndRemoved, 52, "regexp.modifiers.added-and-removed"),
            (RegexDiagnosticCode.UnterminatedClass, 60, "regexp.class.unterminated"),
            (RegexDiagnosticCode.ClassRangeOutOfOrder, 61, "regexp.class.range-out-of-order"),
            (RegexDiagnosticCode.ClassRangeEndpointIsClass, 62, "regexp.class.range-endpoint-is-class"),
            (RegexDiagnosticCode.NegatedClassMayContainStrings, 63, "regexp.class.negated-may-contain-strings"),
            (RegexDiagnosticCode.ClassSetMixedOperators, 70, "regexp.class-set.mixed-operators"),
            (RegexDiagnosticCode.ClassSetAmpersandAfterIntersection, 71, "regexp.class-set.ampersand-after-intersection"),
            (RegexDiagnosticCode.ClassSetRangeEndpointNotCharacter, 72, "regexp.class-set.range-endpoint-not-character"),
            (RegexDiagnosticCode.ClassSetUnescapedSyntaxCharacter, 73, "regexp.class-set.unescaped-syntax-character"),
            (RegexDiagnosticCode.ClassSetReservedDoublePunctuator, 74, "regexp.class-set.reserved-double-punctuator"),
            (RegexDiagnosticCode.ClassStringDisjunctionWithoutBrace, 75, "regexp.class-set.string-disjunction-without-brace"),
            (RegexDiagnosticCode.UnterminatedClassStringDisjunction, 76, "regexp.class-set.string-disjunction-unterminated"),
            (RegexDiagnosticCode.PropertyEscapeWithoutBrace, 80, "regexp.property.without-brace"),
            (RegexDiagnosticCode.UnterminatedPropertyEscape, 81, "regexp.property.unterminated"),
            (RegexDiagnosticCode.UnknownPropertyName, 82, "regexp.property.unknown-name"),
            (RegexDiagnosticCode.UnknownPropertyValue, 83, "regexp.property.unknown-value"),
            (RegexDiagnosticCode.UnknownLonePropertyName, 84, "regexp.property.unknown-lone-name"),
            (RegexDiagnosticCode.PropertyOfStringsOutsideUnicodeSets, 85, "regexp.property.of-strings-outside-v-mode"),
            (RegexDiagnosticCode.NegatedPropertyOfStrings, 86, "regexp.property.negated-strings"),
            (RegexDiagnosticCode.NestingLimit, 90, "limit.regexp.too-deep"),
        ];

        Assert.Equal(Enum.GetValues<RegexDiagnosticCode>().Order(), table.Select(static row => row.Code).Order());
        foreach (var (code, number, id) in table)
        {
            Assert.Equal(number, (int)code);
            Assert.Equal(id, RegexDiagnostic.MessageIdOf(code));
        }

        // One identifier per code, each of the form the shared catalogue gives its identifiers (early-errors.md, section 6):
        // two to four lower-case dotted segments, in the regexp namespace, but for the nesting limit's, which is EX0026's
        // in EX's limit namespace.
        var ids = table.Skip(1).Select(static row => row.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ids, static id => Assert.Matches("^[a-z][a-z0-9]*(-[a-z0-9]+)*(\\.[a-z][a-z0-9]*(-[a-z0-9]+)*){1,3}$", id));
        Assert.All(table.Skip(1).Where(static row => row.Code != RegexDiagnosticCode.NestingLimit), static row => Assert.StartsWith("regexp.", row.Id, StringComparison.Ordinal));
        Assert.Throws<ArgumentOutOfRangeException>(() => RegexDiagnostic.MessageIdOf((RegexDiagnosticCode)4));
    }

    /// <summary>
    /// Where the shared frontend's early-error catalogue has a row for a condition, the code's identifier is the
    /// catalogue's: its rows EE0184 to EE0213 and EX0026 with their <c>message_id</c> column, transcribed from Broiler.JS's
    /// <c>Broiler.ECMAScript/docs/frontend/early-errors.tsv</c> at <c>238069ab</c> (the provider's answer names "the failing
    /// condition's message identifier", JSD-0083's decision D7). EE0183 has no code: no string reaches it. EX0026 is the
    /// catalogue's limit on a pattern's nesting of groups and classes, which the caller's nesting limit is (decision D7 of
    /// JSD-0081 makes Broiler.VM's bound of 512 the caller's limit).
    /// </summary>
    [Theory]
    [InlineData("EE0184", "regexp.group-name.duplicate", RegexDiagnosticCode.DuplicateGroupName)]
    [InlineData("EE0185", "regexp.quantifier.out-of-order", RegexDiagnosticCode.QuantifierOutOfOrder)]
    [InlineData("EE0186", "regexp.modifiers.repeated-flag", RegexDiagnosticCode.ModifierRepeated)]
    [InlineData("EE0187", "regexp.modifiers.empty", RegexDiagnosticCode.ModifiersEmpty)]
    [InlineData("EE0188", "regexp.modifiers.repeated-flag", RegexDiagnosticCode.ModifierRepeated)]
    [InlineData("EE0189", "regexp.modifiers.repeated-flag", RegexDiagnosticCode.ModifierRepeated)]
    [InlineData("EE0190", "regexp.modifiers.added-and-removed", RegexDiagnosticCode.ModifierAddedAndRemoved)]
    [InlineData("EE0191", "regexp.backreference.undefined-group-name", RegexDiagnosticCode.UndefinedGroupName)]
    [InlineData("EE0192", "regexp.backreference.group-number-too-large", RegexDiagnosticCode.BackreferenceOutOfRange)]
    [InlineData("EE0193", "regexp.class.range-endpoint-is-class", RegexDiagnosticCode.ClassRangeEndpointIsClass)]
    [InlineData("EE0194", "regexp.class.range-out-of-order", RegexDiagnosticCode.ClassRangeOutOfOrder)]
    [InlineData("EE0195", "regexp.class.range-endpoint-is-class", RegexDiagnosticCode.ClassRangeEndpointIsClass)]
    [InlineData("EE0196", "regexp.class.range-out-of-order", RegexDiagnosticCode.ClassRangeOutOfOrder)]
    [InlineData("EE0197", "regexp.group-name.invalid-start", RegexDiagnosticCode.GroupNameInvalidStart)]
    [InlineData("EE0198", "regexp.group-name.invalid-start", RegexDiagnosticCode.GroupNameInvalidStart)]
    [InlineData("EE0199", "regexp.group-name.invalid-part", RegexDiagnosticCode.GroupNameInvalidPart)]
    [InlineData("EE0200", "regexp.group-name.invalid-part", RegexDiagnosticCode.GroupNameInvalidPart)]
    [InlineData("EE0201", "regexp.property.unknown-name", RegexDiagnosticCode.UnknownPropertyName)]
    [InlineData("EE0202", "regexp.property.unknown-value", RegexDiagnosticCode.UnknownPropertyValue)]
    [InlineData("EE0203", "regexp.property.unknown-lone-name", RegexDiagnosticCode.UnknownLonePropertyName)]
    [InlineData("EE0204", "regexp.property.of-strings-outside-v-mode", RegexDiagnosticCode.PropertyOfStringsOutsideUnicodeSets)]
    [InlineData("EE0205", "regexp.property.negated-strings", RegexDiagnosticCode.NegatedPropertyOfStrings)]
    [InlineData("EE0206", "regexp.class.negated-may-contain-strings", RegexDiagnosticCode.NegatedClassMayContainStrings)]
    [InlineData("EE0207", "regexp.class.negated-may-contain-strings", RegexDiagnosticCode.NegatedClassMayContainStrings)]
    [InlineData("EE0208", "regexp.class.range-out-of-order", RegexDiagnosticCode.ClassRangeOutOfOrder)]
    [InlineData("EE0209", "regexp.quantifier.braced-without-atom", RegexDiagnosticCode.BracedQuantifierWithoutAtom)]
    [InlineData("EE0210", "regexp.class.range-endpoint-is-class", RegexDiagnosticCode.ClassRangeEndpointIsClass)]
    [InlineData("EE0211", "regexp.class.range-out-of-order", RegexDiagnosticCode.ClassRangeOutOfOrder)]
    [InlineData("EE0212", "regexp.class.range-endpoint-is-class", RegexDiagnosticCode.ClassRangeEndpointIsClass)]
    [InlineData("EE0213", "regexp.class.range-out-of-order", RegexDiagnosticCode.ClassRangeOutOfOrder)]
    [InlineData("EX0026", "limit.regexp.too-deep", RegexDiagnosticCode.NestingLimit)]
    public void A_condition_the_catalogue_names_has_the_catalogue_s_identifier(string row, string catalogueId, RegexDiagnosticCode code)
    {
        Assert.Matches("^E[EX][0-9]{4}$", row);
        Assert.Equal(catalogueId, RegexDiagnostic.MessageIdOf(code));
    }

    /// <summary>
    /// Catalogue row EG0002 (<c>annexb.regexp.unicode-mode</c>, JSE8107) is one condition for every Annex B extension of
    /// the pattern grammar refused under <c>u</c> or <c>v</c>. The validator tells them apart (JSD-0083's decision D7, "the
    /// failing condition's message identifier where the validator can tell it"), so the codes split EG0002: wholly
    /// <see cref="RegexDiagnosticCode.LoneSyntaxCharacter"/>, <see cref="RegexDiagnosticCode.InvalidIdentityEscape"/> (with
    /// a legacy octal escape and <c>\8</c> in a class) and <see cref="RegexDiagnosticCode.InvalidControlEscape"/>, and in
    /// part <see cref="RegexDiagnosticCode.QuantifiedAssertion"/> (a quantified look-ahead),
    /// <see cref="RegexDiagnosticCode.InvalidHexEscape"/> (<c>\x</c> or <c>\u</c> without its digits),
    /// <see cref="RegexDiagnosticCode.NamedReferenceWithoutName"/> (<c>\k</c> without <c>&lt;</c>) and
    /// <see cref="RegexDiagnosticCode.PropertyEscapeWithoutBrace"/> (<c>\p</c> without <c>{</c>). The row's other examples,
    /// <c>\8</c> without an eighth group and a class range ending in a class escape, are rows EE0192, EE0193 and EE0195 and
    /// take theirs. The split is a difference the catalogue is to adopt (the record of task 2.8 in <c>docs/roadmap.md</c>):
    /// each pattern here is admitted by Annex B without <c>u</c> or <c>v</c> and refused with its code under both.
    /// </summary>
    [Theory]
    [InlineData("{", RegexDiagnosticCode.LoneSyntaxCharacter, 0)]
    [InlineData("a]", RegexDiagnosticCode.LoneSyntaxCharacter, 1)]
    [InlineData("}", RegexDiagnosticCode.LoneSyntaxCharacter, 0)]
    [InlineData("\\a", RegexDiagnosticCode.InvalidIdentityEscape, 1)]
    [InlineData("\\01", RegexDiagnosticCode.InvalidIdentityEscape, 1)]
    [InlineData("[\\8]", RegexDiagnosticCode.InvalidIdentityEscape, 2)]
    [InlineData("\\c1", RegexDiagnosticCode.InvalidControlEscape, 2)]
    [InlineData("[\\c]", RegexDiagnosticCode.InvalidControlEscape, 3)]
    [InlineData("(?=a)*", RegexDiagnosticCode.QuantifiedAssertion, 5)]
    [InlineData("(?!a){2}", RegexDiagnosticCode.QuantifiedAssertion, 5)]
    [InlineData("\\xg", RegexDiagnosticCode.InvalidHexEscape, 2)]
    [InlineData("\\u12", RegexDiagnosticCode.InvalidHexEscape, 4)]
    [InlineData("\\ka", RegexDiagnosticCode.NamedReferenceWithoutName, 2)]
    [InlineData("\\p", RegexDiagnosticCode.PropertyEscapeWithoutBrace, 2)]
    public void An_Annex_B_extension_refused_under_u_or_v_has_its_own_code_of_row_EG0002(string pattern, RegexDiagnosticCode code, int position)
    {
        Assert.Equal(RegexStatus.Success, BroilerRegex.Validate(pattern, RegexFlags.None, UnicodeTables.Instance, 512, out _, out _));
        foreach (var flags in new[] { RegexFlags.Unicode, RegexFlags.UnicodeSets })
        {
            Assert.Equal(RegexStatus.SyntaxError, BroilerRegex.Validate(pattern, flags, UnicodeTables.Instance, 512, out var diagnostic, out _));
            Assert.Equal(code, diagnostic.Code);
            Assert.Equal(position, diagnostic.Position);
            Assert.NotEqual("annexb.regexp.unicode-mode", diagnostic.MessageId);
        }
    }

    /// <summary>
    /// The critic's <c>\k</c> case (SJL-11's plan, row 2.8; the critic's probe of <c>0.1.0-preview.4</c>, where
    /// <c>RegexParser.Parse(\k&lt;b&gt;(?&lt;a&gt;x), u)</c> was accepted by the parse alone): a reference before any group
    /// to a name no group declares is refused by the validate entry, as by compilation (check A-5, "\k&lt;name&gt; to an
    /// undeclared name refused by the parser and by the validate entry"), under every mode where <c>\k</c> is a reference.
    /// </summary>
    [Theory]
    [InlineData("u")]
    [InlineData("v")]
    [InlineData("")]
    [InlineData("i")]
    public void The_critic_s_k_case_is_refused_by_the_validate_entry(string flags)
    {
        Assert.Equal(RegexStatus.SyntaxError, BroilerRegex.Validate("\\k<b>(?<a>x)", flags, UnicodeTables.Instance, 512, out var diagnostic, out _));
        Assert.Equal(RegexDiagnosticCode.UndefinedGroupName, diagnostic.Code);
        Assert.Equal("regexp.backreference.undefined-group-name", diagnostic.MessageId);
        Assert.Equal(0, diagnostic.Position);
        Assert.Equal("b", diagnostic.Argument);

        // Its controls: the name declared later is admitted; with no group name and no u or v, \k is Annex B's letter.
        Assert.Equal(RegexStatus.Success, BroilerRegex.Validate("\\k<a>(?<a>x)", flags, UnicodeTables.Instance, 512, out _, out _));
        if (flags is "" or "i")
            Assert.Equal(RegexStatus.Success, BroilerRegex.Validate("\\k<b>", flags, UnicodeTables.Instance, 512, out _, out _));
    }

    [Fact]
    public void The_nesting_limit_is_answered_as_a_limit_with_its_diagnostic()
    {
        var at = "(" + new string('(', 510) + "a" + new string(')', 511);
        Assert.Equal(RegexStatus.Success, BroilerRegex.Validate(at, RegexFlags.None, UnicodeTables.Instance, 512, out var none, out var noLimit));
        Assert.Equal(RegexDiagnosticCode.None, none.Code);
        Assert.Equal(RegexLimit.None, noLimit);

        var past = new string('(', 512) + "a" + new string(')', 512);
        Assert.Equal(RegexStatus.LimitExceeded, BroilerRegex.Validate(past, RegexFlags.None, UnicodeTables.Instance, 512, out var diagnostic, out var limit));
        Assert.Equal(RegexLimit.Nesting, limit);
        Assert.Equal(RegexDiagnosticCode.NestingLimit, diagnostic.Code);
        Assert.Equal("limit.regexp.too-deep", diagnostic.MessageId);
        Assert.Equal(511, diagnostic.Position);
        Assert.Equal("512", diagnostic.Argument);

        // Compile answers the same status; the constructors, under the maximum, throw the same diagnostic.
        Assert.Equal(RegexStatus.LimitExceeded, BroilerRegex.Compile(past, RegexFlags.None, UnicodeTables.Instance, 512, null, null, out _, out var compiled));
        Assert.Equal(RegexLimit.Nesting, compiled);
        var thrown = Assert.Throws<RegexSyntaxException>(() => new BroilerRegex(past, RegexFlags.None, UnicodeTables.Instance));
        Assert.Equal(RegexDiagnosticCode.NestingLimit, thrown.Diagnostic.Code);
        Assert.Equal(511, thrown.Diagnostic.Position);

        // The caller's own limit, under v a class counted as a level; a syntax error the parser meets first is answered first.
        Assert.Equal(RegexStatus.LimitExceeded, BroilerRegex.Validate("[[a]]", RegexFlags.UnicodeSets, UnicodeTables.Instance, 2, out var classes, out _));
        Assert.Equal((RegexDiagnosticCode.NestingLimit, 1, "2"), (classes.Code, classes.Position, classes.Argument));
        Assert.Equal(RegexStatus.SyntaxError, BroilerRegex.Validate("*((a))", RegexFlags.None, UnicodeTables.Instance, 1, out _, out _));
        Assert.Equal(RegexStatus.LimitExceeded, BroilerRegex.Validate("((a))*", RegexFlags.None, UnicodeTables.Instance, 1, out _, out _));
    }

    [Fact]
    public void A_pattern_nested_far_past_the_limit_is_refused_on_the_smallest_declared_stack()
    {
        // The corpus's 1 MiB stack (tests/Adversarial/corpus.tsv, stack-bytes): N06's, N12's and N18's million levels are
        // refused before the parser descends past the limit, so no frame per level is taken (requirement R4, decision D6).
        var answers = new List<(RegexStatus, RegexLimit)>();
        var thread = new Thread(() =>
        {
            foreach (var (open, close, flags) in new[] { ("(", ")", RegexFlags.None), ("(?:", ")", RegexFlags.None), ("[", "]", RegexFlags.UnicodeSets) })
            {
                var pattern = string.Concat(Enumerable.Repeat(open, 1_000_000)) + "a" + string.Concat(Enumerable.Repeat(close, 1_000_000));
                var status = BroilerRegex.Validate(pattern, flags, UnicodeTables.Instance, 512, out _, out var limit);
                answers.Add((status, limit));
            }
        }, 1 << 20);
        thread.Start();
        thread.Join();
        Assert.Equal(3, answers.Count);
        Assert.All(answers, static answer => Assert.Equal((RegexStatus.LimitExceeded, RegexLimit.Nesting), answer));
    }

    [Fact]
    public void The_entry_checks_its_arguments_and_answers_no_exception_for_a_refusal()
    {
        Assert.Throws<ArgumentNullException>(() => BroilerRegex.Validate(null!, RegexFlags.None, UnicodeTables.Instance, 512, out _, out _));
        Assert.Throws<ArgumentNullException>(() => BroilerRegex.Validate("a", RegexFlags.None, null!, 512, out _, out _));
        Assert.Throws<ArgumentNullException>(() => BroilerRegex.Validate("a", "g", null!, 512, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => BroilerRegex.Validate("a", RegexFlags.None, UnicodeTables.Instance, 0, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => BroilerRegex.Validate("a", "", UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit + 1, out _, out _));

        // The flags text is read first, as IsValidRegularExpressionLiteral reads it, and null or empty is no flag.
        Assert.Equal(RegexStatus.SyntaxError, BroilerRegex.Validate("(", "gg", UnicodeTables.Instance, 512, out var flags, out _));
        Assert.Equal(RegexDiagnosticCode.DuplicateFlag, flags.Code);
        Assert.Equal(RegexStatus.Success, BroilerRegex.Validate("a", (string?)null, UnicodeTables.Instance, 512, out _, out _));
        Assert.Equal(RegexStatus.Success, BroilerRegex.Validate("", "dgimsvy", UnicodeTables.Instance, 512, out _, out _));
    }

    [Fact]
    public void The_default_diagnostic_is_no_refusal()
    {
        var none = default(RegexDiagnostic);
        Assert.Equal(RegexDiagnosticCode.None, none.Code);
        Assert.Equal("", none.MessageId);
        Assert.Equal(-1, none.Position);
        Assert.Null(none.Argument);
        Assert.Equal("", none.Message);

        // An exception made through the public constructor carries no diagnostic.
        Assert.Equal(RegexDiagnosticCode.None, new RegexSyntaxException("made by a caller", 3).Diagnostic.Code);
    }

    /// <summary>
    /// "No set expansion" (decision D18): a validation charges its account the pattern's read alone, where a compilation of
    /// the same pattern charges the expansions it makes, so no property escape is expanded, no set folded, complemented or
    /// joined, no class's strings gathered and no universe built. Each pattern holds the work an early error does not need.
    /// </summary>
    [Theory]
    [InlineData("\\p{L}", "u")]
    [InlineData("[\\p{L}\\p{N}]", "iu")]
    [InlineData("\\P{Ll}", "iu")]
    [InlineData("[^\\p{Lu}]", "iv")]
    [InlineData("[[\\p{L}--\\p{Lu}]&&x]", "v")]
    [InlineData("[\\p{L}--[a-z]]", "iv")]
    [InlineData("[\\p{RGI_Emoji}\\q{ab|c}]", "v")]
    [InlineData("\\p{RGI_Emoji}", "iv")]
    [InlineData("[\\w\\s\\D]\\W", "iu")]
    [InlineData("[\\S]", "v")]
    [InlineData("(?i:[\\p{Lu}a-z])\\k<n>(?<n>b)", "v")]
    public void A_validation_expands_nothing(string pattern, string flags)
    {
        var parsed = RegexFlagsParser.Parse(flags);
        var sites = new List<CompileSite>();
        var account = new CompileCharge(null, null) { Observer = (site, _, _) => sites.Add(site) };
        Assert.Equal(RegexStatus.Success, BroilerRegex.Validate(pattern, parsed, new RegexUnicode(UnicodeTables.Instance, account), new RegexNesting(512), out _, out _));
        Assert.Equal([CompileSite.Pattern], sites);
        Assert.Equal((ulong)pattern.Length, account.Spent);

        // The control: the compilation of the same pattern charges expansions.
        var compiled = new List<CompileSite>();
        var compilation = new CompileCharge(null, null) { Observer = (site, _, _) => compiled.Add(site) };
        Assert.Equal(ProgramCompilation.Compiled, ProgramCompiler.Compile(pattern, parsed, UnicodeTables.Instance, 512, out _, charge: compilation));
        Assert.Contains(compiled, static site => site != CompileSite.Pattern);
        Assert.True(compilation.Spent > account.Spent);
    }

    [Fact]
    public void A_class_V_pattern_validates_at_its_read_where_compilation_is_refused_at_its_budget()
    {
        // The corpus's V01 and V05: fifty \p{L} under v, fifty \p{RGI_Emoji} under vi, which a compile budget of 300 and 1,000
        // units refuses (requirement R2). The validate entry reads each pattern once: its work is the pattern's length.
        foreach (var (pattern, flags, budget) in new[]
        {
            ("[" + string.Concat(Enumerable.Repeat("\\p{L}", 50)) + "]", RegexFlags.UnicodeSets, 300UL),
            ("[" + string.Concat(Enumerable.Repeat("\\p{RGI_Emoji}", 50)) + "]", RegexFlags.UnicodeSets | RegexFlags.IgnoreCase, 1000UL),
        })
        {
            var account = new CompileCharge(null, null);
            Assert.Equal(RegexStatus.Success, BroilerRegex.Validate(pattern, flags, new RegexUnicode(UnicodeTables.Instance, account), new RegexNesting(512), out _, out _));
            Assert.Equal((ulong)pattern.Length, account.Spent);
            Assert.Equal(RegexStatus.LimitExceeded, BroilerRegex.Compile(pattern, flags, UnicodeTables.Instance, 512, budget, null, out _, out var limit));
            Assert.Equal(RegexLimit.Compile, limit);
        }
    }

    /// <summary>
    /// A property escape refused where it stands, a property of strings negated or outside <c>v</c>, is refused before it
    /// is expanded, by a compilation as by the validate entry: the early error reads the property's kind from the
    /// provider's lookups, so the compilation charges the pattern's read alone, where it expanded the property first
    /// before task 2.8.
    /// </summary>
    [Theory]
    [InlineData("\\P{RGI_Emoji}", "v", RegexDiagnosticCode.NegatedPropertyOfStrings)]
    [InlineData("[\\P{RGI_Emoji}]", "v", RegexDiagnosticCode.NegatedPropertyOfStrings)]
    [InlineData("\\p{RGI_Emoji}", "u", RegexDiagnosticCode.PropertyOfStringsOutsideUnicodeSets)]
    [InlineData("[\\p{Emoji_Keycap_Sequence}]", "iu", RegexDiagnosticCode.PropertyOfStringsOutsideUnicodeSets)]
    public void A_property_escape_its_mode_refuses_is_refused_before_it_is_expanded(string pattern, string flags, RegexDiagnosticCode code)
    {
        var sites = new List<CompileSite>();
        var compilation = new CompileCharge(null, null) { Observer = (site, _, _) => sites.Add(site) };
        var refusal = Assert.Throws<RegexSyntaxException>(() => ProgramCompiler.Compile(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance, 512, out _, charge: compilation));
        Assert.Equal(code, refusal.Diagnostic.Code);
        Assert.Equal([CompileSite.Pattern], sites);
        Assert.Equal((ulong)pattern.Length, compilation.Spent);
    }

    /// <summary>
    /// Every syntax error <see cref="BroilerRegex.Compile"/> gives, the validate entry gives with the same diagnostic, and
    /// it accepts exactly what <see cref="BroilerRegex.Compile"/> compiles, with no budget, under the maximum and under a
    /// small limit: over the suite population (every string literal of this test project under the flags near it and nine
    /// base flag strings) and task 2.4b's seeded population. The fuzz target's grammar is judged alike in
    /// <c>tests/Regex.Fuzz</c> (its oracle, and <c>FuzzTargetTests</c>).
    /// </summary>
    [Fact]
    public void The_validate_entry_answers_as_Compile_over_the_suite_and_seeded_populations()
    {
        var seen = new HashSet<(string, string)>();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in TestPopulations.SuitePopulation(TestPopulations.TestSources()).Concat(TestPopulations.SeededPopulation()))
        {
            if (!seen.Add((row.Pattern, row.Flags)))
                continue;
            var flags = RegexFlagsParser.Parse(row.Flags);
            foreach (var nesting in (ReadOnlySpan<int>)[BroilerRegex.MaximumNestingLimit, 2])
            {
                var answer = Agreement(row.Pattern, flags, nesting);
                counts[answer] = counts.GetValueOrDefault(answer) + 1;
            }
        }

        // Not vacuous: each answer is reached, refusals by the thousand among them.
        Assert.True(counts.GetValueOrDefault("compiled") > 10_000, string.Join(", ", counts));
        Assert.True(counts.GetValueOrDefault("syntax") > 1_000, string.Join(", ", counts));
        Assert.True(counts.GetValueOrDefault("nesting") > 100, string.Join(", ", counts));
    }

    /// <summary>
    /// Compiles and validates <paramref name="pattern"/> under <paramref name="nesting"/>, fails on any disagreement, and
    /// answers which of the three the two agreed on.
    /// </summary>
    internal static string Agreement(string pattern, RegexFlags flags, int nesting)
    {
        var validated = BroilerRegex.Validate(pattern, flags, UnicodeTables.Instance, nesting, out var diagnostic, out var limit);
        try
        {
            var compiled = BroilerRegex.Compile(pattern, flags, UnicodeTables.Instance, nesting, null, null, out var regex, out var compiledLimit);
            if (compiled == RegexStatus.Success)
            {
                Assert.True(validated == RegexStatus.Success, Show(pattern, flags) + " compiles and is answered " + validated + " " + diagnostic.MessageId);
                return "compiled";
            }

            Assert.Equal(RegexLimit.Nesting, compiledLimit);
            Assert.True(validated == RegexStatus.LimitExceeded && limit == RegexLimit.Nesting && diagnostic.Code == RegexDiagnosticCode.NestingLimit,
                Show(pattern, flags) + " meets the nesting limit in Compile and is answered " + validated + " " + diagnostic.MessageId);
            return "nesting";
        }
        catch (RegexSyntaxException refusal)
        {
            Assert.True(validated == RegexStatus.SyntaxError, Show(pattern, flags) + " is refused by Compile (" + refusal.Message + ") and answered " + validated);
            AssertSame(refusal.Diagnostic, diagnostic);
            Assert.Equal(refusal.Position, diagnostic.Position);
            return "syntax";
        }
    }

    private static string Show(string pattern, RegexFlags flags) => "/" + TestPopulations.Escape(pattern) + "/ under " + flags;

    private static void AssertSame(RegexDiagnostic expected, RegexDiagnostic actual)
    {
        Assert.Equal(expected.Code, actual.Code);
        Assert.Equal(expected.MessageId, actual.MessageId);
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.Argument, actual.Argument);
        Assert.Equal(expected.Message, actual.Message);
    }

    private static bool IsOfFlags(RegexDiagnosticCode code) =>
        code is RegexDiagnosticCode.UnknownFlag or RegexDiagnosticCode.DuplicateFlag or RegexDiagnosticCode.UnicodeWithUnicodeSets;
}
