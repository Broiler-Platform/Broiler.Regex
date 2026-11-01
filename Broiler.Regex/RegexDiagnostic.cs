// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   9
// Annotated:        9/9
// Exempt:           54
// Human-reviewed:   0/9
// IP risk:          None
// Security risk:    Low
// Criteria:         5/0
// Resource impact:  0/10 max
// Unverified:       9
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Globalization;

namespace Broiler.Regex;

/// <summary>
/// What a refused pattern or flags string breaks: one code per kind of syntax error and early error the parser knows
/// (ECMA-262 §22.2.1 and §22.2.1.1, Annex B §B.1.2, and the flags of RegExpInitialize, §22.2.3.3, and ParsePattern,
/// §22.2.3.4), and one for the caller's nesting limit. The code of a <see cref="RegexDiagnostic"/>, which the validate entry
/// (<see cref="BroilerRegex.Validate(string, RegexFlags, Broiler.Unicode.Algorithms.IUnicodeTables, int, out RegexDiagnostic, out RegexLimit)"/>)
/// answers, as <see cref="BroilerRegex.Compile"/> and <see cref="Text.RegexFlagsText.Parse"/> do (decision D9 of
/// Broiler.VM's JSD-0081: a syntax status with a code, a message identifier and a position).
/// </summary>
/// <remarks>
/// <para>
/// <b>Stable.</b> A member's number is fixed once it is published and never reused for another condition; a condition
/// added later takes a new number. Each member names one condition, whatever production of the grammar meets it, and has
/// one message identifier (<see cref="RegexDiagnostic.MessageIdOf"/>), so code and identifier name the same thing.
/// </para>
/// <para>
/// <b>The message identifiers</b> are dotted and lower case, of the form of the shared frontend's early-error catalogue
/// (Broiler.JS's <c>Broiler.ECMAScript/docs/frontend/early-errors.md</c>, section 6), whose rows are its early errors
/// (EE), Annex B's (EG) and its limits and grammar (EX). Where the catalogue has a row for the condition, the identifier
/// is the catalogue's and the member's documentation names the row: EE0184 to EE0213, and EX0026, the nesting limit,
/// <c>limit.regexp.too-deep</c>. The library coins an identifier, in the catalogue's <c>regexp</c> namespace, only for a
/// condition the catalogue has no row for, the grammar's own refusals and the flags', which the catalogue folds into
/// EE0019, <c>regexp.literal.invalid</c>; and for the conditions of row EG0002 (<c>annexb.regexp.unicode-mode</c>), Annex
/// B's extensions refused under <c>u</c> or <c>v</c>, which the library tells apart (decision D7 of JSD-0083: the failing
/// condition's identifier "where the validator can tell it"): codes 14, 17, 21, 22, 23, 32 and 80 split EG0002, a split
/// the catalogue is to adopt (the record of task 2.8 in <c>docs/roadmap.md</c>). An engine renders its own text from the
/// identifier and the argument (decision SJL-D-11); <see cref="RegexDiagnostic.Message"/> is a default English rendering
/// and no contract.
/// </para>
/// <para>
/// <b>Positions</b> are code-unit offsets into the pattern, or, for the three codes of the flags, into the flags text.
/// <b>Arguments</b>: a code takes at most one, <see cref="RegexDiagnostic.Argument"/>, as its member says.
/// </para>
/// <para>
/// <b>Not reachable, so not a code:</b> EE0183, a pattern of 2³² − 1 or more capturing groups, which no string the
/// platform can hold reaches.
/// </para>
/// <para><b>New in task 2.8 of SJL-11.2</b>, with the validate entry; fixed for <c>0.1.0-preview.6</c> by task 2.9 of SJL-11.2.</para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D9 and ECMA-262 s22.2.1.1; IP=None; Security=Low; Resources=0; Fingerprint=7C239A
// Broiler-Falsified-If: two members share a number or a message identifier, or a member's number changes once published
// Broiler-Human:        PENDING
public enum RegexDiagnosticCode
{
    /// <summary>No refusal: the default value of a <see cref="RegexDiagnostic"/>.</summary>
    None = 0,

    /// <summary>
    /// <c>regexp.flags.unknown</c>: a flags code unit other than <c>d g i m s u v y</c> (§22.2.3.3 RegExpInitialize). Argument: the
    /// code unit. Position: its index in the flags text.
    /// </summary>
    UnknownFlag = 1,

    /// <summary>
    /// <c>regexp.flags.duplicate</c>: a flag written twice. Argument: the flag. Position: its second occurrence in the flags text.
    /// </summary>
    DuplicateFlag = 2,

    /// <summary>
    /// <c>regexp.flags.unicode-with-unicode-sets</c>: <c>u</c> with <c>v</c> (§22.2.3.4 ParsePattern). Position: the later of the
    /// two in the flags text, or -1 for a <see cref="RegexFlags"/> value that sets both, which has no text (task 2.9 of
    /// SJL-11.2).
    /// </summary>
    UnicodeWithUnicodeSets = 3,

    /// <summary><c>regexp.group.unmatched-close</c>: a <c>)</c> that closes no group. Position: the <c>)</c>.</summary>
    UnmatchedParenthesis = 10,

    /// <summary><c>regexp.group.unterminated</c>: a group the pattern ends inside. Position: the pattern's end.</summary>
    UnterminatedGroup = 11,

    /// <summary>
    /// <c>regexp.group.invalid</c>: <c>(?</c> followed by none of the forms of a group: <c>:</c>, <c>=</c>, <c>!</c>,
    /// <c>&lt;=</c>, <c>&lt;!</c>, a group name, or modifiers and their <c>:</c>. Position: the code unit no form admits.
    /// </summary>
    InvalidGroup = 12,

    /// <summary>
    /// <c>regexp.quantifier.nothing-to-repeat</c>: <c>*</c>, <c>+</c> or <c>?</c> where an atom should be. Argument: the
    /// quantifier's code unit. Position: it.
    /// </summary>
    NothingToRepeat = 13,

    /// <summary>
    /// <c>regexp.quantifier.after-assertion</c>: a quantifier after an assertion no grammar lets one follow: <c>^ $ \b \B</c>,
    /// a look-behind, and under <c>u</c> or <c>v</c> a look-ahead too (Annex B's QuantifiableAssertion; that part is one of the
    /// conditions of catalogue row EG0002, which these codes split). Position: the quantifier.
    /// </summary>
    QuantifiedAssertion = 14,

    /// <summary>
    /// <c>regexp.quantifier.out-of-order</c> (catalogue row EE0185): <c>{n,m}</c> with <c>n</c> above <c>m</c>. Position: the <c>{</c>.
    /// </summary>
    QuantifierOutOfOrder = 15,

    /// <summary>
    /// <c>regexp.quantifier.braced-without-atom</c> (EE0209, Annex B's InvalidBracedQuantifier): a well-formed <c>{n}</c>,
    /// <c>{n,}</c> or <c>{n,m}</c> where an atom should be, outside <c>u</c> and <c>v</c>. Position: the <c>{</c>.
    /// </summary>
    BracedQuantifierWithoutAtom = 16,

    /// <summary>
    /// <c>regexp.pattern.lone-syntax-character</c>: an unescaped <c>]</c>, <c>{</c> or <c>}</c> as a character under <c>u</c>
    /// or <c>v</c>, where Annex B's ExtendedPatternCharacter does not apply (one of the conditions of catalogue row EG0002,
    /// which these codes split). Argument: the code unit. Position: it.
    /// </summary>
    LoneSyntaxCharacter = 17,

    /// <summary><c>regexp.escape.trailing-backslash</c>: a <c>\</c> that ends the pattern. Position: the pattern's end.</summary>
    TrailingBackslash = 20,

    /// <summary>
    /// <c>regexp.escape.invalid-identity</c>: under <c>u</c> or <c>v</c>, an escape of a code unit that is neither a syntax
    /// character nor <c>/</c>, a legacy octal escape among them (one of the conditions of catalogue row EG0002, which these
    /// codes split: Annex B's identity escapes refused there). Argument: the escaped code unit. Position: it.
    /// </summary>
    InvalidIdentityEscape = 21,

    /// <summary>
    /// <c>regexp.escape.invalid-control</c>: under <c>u</c> or <c>v</c>, <c>\c</c> not followed by an ASCII letter (one of the
    /// conditions of catalogue row EG0002, which these codes split). Position: the code unit after the <c>c</c>.
    /// </summary>
    InvalidControlEscape = 22,

    /// <summary>
    /// <c>regexp.escape.invalid-hex</c>: a <c>\x</c> or a four-digit <c>\u</c> escape with a code unit that is not a hexadecimal
    /// digit where one is required (under <c>u</c> or <c>v</c>, where that part is one of the conditions of catalogue row
    /// EG0002, which these codes split; or in a group name). Position: that code unit.
    /// </summary>
    InvalidHexEscape = 23,

    /// <summary>
    /// <c>regexp.escape.invalid-braced</c>: a <c>\u{</c> escape with no digit or with no <c>}</c> after its digits. Position:
    /// the code unit where a digit or the <c>}</c> was required.
    /// </summary>
    InvalidBracedEscape = 24,

    /// <summary>
    /// <c>regexp.escape.code-point-out-of-range</c>: a <c>\u{…}</c> escape above U+10FFFF. Position: the digit that passes it.
    /// </summary>
    CodePointOutOfRange = 25,

    /// <summary>
    /// <c>regexp.backreference.group-number-too-large</c> (EE0192): under <c>u</c> or <c>v</c>, <c>\n</c> for a number above
    /// the pattern's count of capturing groups. Argument: the number's digits. Position: its first digit.
    /// </summary>
    BackreferenceOutOfRange = 30,

    /// <summary>
    /// <c>regexp.backreference.undefined-group-name</c> (EE0191): <c>\k&lt;name&gt;</c> for a name no group of the pattern
    /// declares, before or after it. Argument: the name, decoded. Position: the reference's <c>\</c>.
    /// </summary>
    UndefinedGroupName = 31,

    /// <summary>
    /// <c>regexp.backreference.missing-group-name</c>: <c>\k</c> not followed by <c>&lt;</c> where it must be a reference
    /// (under <c>u</c> or <c>v</c>, where that part is one of the conditions of catalogue row EG0002, which these codes split;
    /// or in a pattern that declares a group name). Position: the code unit after the <c>k</c>.
    /// </summary>
    NamedReferenceWithoutName = 32,

    /// <summary>
    /// <c>regexp.class.named-reference</c>: outside <c>u</c> and <c>v</c>, <c>\k</c> in a class of a pattern that declares a
    /// group name, where Annex B's IdentityEscape excludes it and no class holds a reference. Position: the <c>k</c>.
    /// </summary>
    NamedReferenceInClass = 33,

    /// <summary><c>regexp.group-name.unterminated</c>: a group name the pattern ends inside. Position: the pattern's end.</summary>
    UnterminatedGroupName = 40,

    /// <summary>
    /// <c>regexp.group-name.invalid-escape</c>: a <c>\</c> in a group name not followed by <c>u</c>. Position: the <c>\</c>.
    /// </summary>
    GroupNameInvalidEscape = 41,

    /// <summary>
    /// <c>regexp.group-name.invalid-start</c> (EE0197, EE0198): a group name's first code point, raw, escaped or a surrogate
    /// pair, that is not an IdentifierStartChar. Position: the code point, or its escape's <c>\</c>.
    /// </summary>
    GroupNameInvalidStart = 42,

    /// <summary>
    /// <c>regexp.group-name.invalid-part</c> (EE0199, EE0200): a later code point of a group name that is not an
    /// IdentifierPartChar. Position: the code point, or its escape's <c>\</c>.
    /// </summary>
    GroupNameInvalidPart = 43,

    /// <summary><c>regexp.group-name.empty</c>: <c>&lt;&gt;</c>, a group name of no code point. Position: the <c>&gt;</c>.</summary>
    EmptyGroupName = 44,

    /// <summary>
    /// <c>regexp.group-name.duplicate</c> (EE0184): a name two groups declare where MightBothParticipate holds (§22.2.1.4),
    /// compared decoded. Argument: the name. Position: the later group's <c>(</c>.
    /// </summary>
    DuplicateGroupName = 45,

    /// <summary>
    /// <c>regexp.modifiers.repeated-flag</c> (EE0186, EE0188, EE0189): a modifier list naming a flag twice. Argument: the flag.
    /// Position: its second occurrence.
    /// </summary>
    ModifierRepeated = 50,

    /// <summary>
    /// <c>regexp.modifiers.empty</c> (EE0187): <c>(?-:</c>, a modifier group adding and removing nothing. Position: the code
    /// unit after the modifiers.
    /// </summary>
    ModifiersEmpty = 51,

    /// <summary>
    /// <c>regexp.modifiers.added-and-removed</c> (EE0190): a flag both added and removed by one modifier group. Position: the
    /// code unit after the modifiers.
    /// </summary>
    ModifierAddedAndRemoved = 52,

    /// <summary>
    /// <c>regexp.class.unterminated</c>: a class the pattern ends inside. Position: the pattern's end; under <c>v</c>, the
    /// innermost open class's <c>[</c> when the pattern ends after a complete operand, and the pattern's end when it ends
    /// where an operand is required (after <c>&amp;&amp;</c> or <c>--</c>).
    /// </summary>
    UnterminatedClass = 60,

    /// <summary>
    /// <c>regexp.class.range-out-of-order</c> (EE0194, EE0196, EE0208, EE0211, EE0213): a class range whose first character
    /// is above its last. Argument: the range as written. Position: the range's first character.
    /// </summary>
    ClassRangeOutOfOrder = 61,

    /// <summary>
    /// <c>regexp.class.range-endpoint-is-class</c> (EE0193, EE0195, EE0210, EE0212): under <c>u</c>, a class escape or a
    /// property escape at either end of a class range. Position: the range's <c>-</c>.
    /// </summary>
    ClassRangeEndpointIsClass = 62,

    /// <summary>
    /// <c>regexp.class.negated-may-contain-strings</c> (EE0206, EE0207): under <c>v</c>, a negated class, at the top or
    /// nested, whose contents may contain strings (§22.2.1.8 MayContainStrings). Position: the class's <c>[</c>.
    /// </summary>
    NegatedClassMayContainStrings = 63,

    /// <summary>
    /// <c>regexp.class-set.mixed-operators</c>: under <c>v</c>, a class-set expression mixing union, <c>&amp;&amp;</c> and
    /// <c>--</c> without nesting, or a range or any operand after an intersection's or a subtraction's last operand.
    /// Position: the expression's first operand.
    /// </summary>
    ClassSetMixedOperators = 70,

    /// <summary>
    /// <c>regexp.class-set.ampersand-after-intersection</c>: under <c>v</c>, <c>&amp;&amp;&amp;</c>, a third <c>&amp;</c>
    /// where ClassIntersection's lookahead refuses it. Position: the third <c>&amp;</c>.
    /// </summary>
    ClassSetAmpersandAfterIntersection = 71,

    /// <summary>
    /// <c>regexp.class-set.range-endpoint-not-character</c>: under <c>v</c>, a range whose end is not one ClassSetCharacter
    /// (a class escape, a property escape, <c>\q{…}</c> or a nested class). Position: the range's first operand.
    /// </summary>
    ClassSetRangeEndpointNotCharacter = 72,

    /// <summary>
    /// <c>regexp.class-set.unescaped-syntax-character</c>: under <c>v</c>, an unescaped ClassSetSyntaxCharacter
    /// (<c>( ) [ ] { } / - \ |</c>) where a character should be, in a class or a <c>\q{…}</c>. Argument: the code unit.
    /// Position: it.
    /// </summary>
    ClassSetUnescapedSyntaxCharacter = 73,

    /// <summary>
    /// <c>regexp.class-set.reserved-double-punctuator</c>: under <c>v</c>, a ClassSetReservedDoublePunctuator such as
    /// <c>!!</c> where a character should be. Argument: the two code units. Position: the first.
    /// </summary>
    ClassSetReservedDoublePunctuator = 74,

    /// <summary>
    /// <c>regexp.class-set.string-disjunction-without-brace</c>: under <c>v</c>, <c>\q</c> in a class not followed by
    /// <c>{</c>. Position: the code unit after the <c>q</c>.
    /// </summary>
    ClassStringDisjunctionWithoutBrace = 75,

    /// <summary>
    /// <c>regexp.class-set.string-disjunction-unterminated</c>: under <c>v</c>, a <c>\q{</c> the pattern ends inside.
    /// Position: the <c>q</c>.
    /// </summary>
    UnterminatedClassStringDisjunction = 76,

    /// <summary>
    /// <c>regexp.property.without-brace</c>: under <c>u</c> or <c>v</c>, <c>\p</c> or <c>\P</c> not followed by <c>{</c> (one of
    /// the conditions of catalogue row EG0002, which these codes split). Position: the code unit after the <c>p</c>.
    /// </summary>
    PropertyEscapeWithoutBrace = 80,

    /// <summary><c>regexp.property.unterminated</c>: a <c>\p{</c> the pattern ends inside. Position: the pattern's end.</summary>
    UnterminatedPropertyEscape = 81,

    /// <summary>
    /// <c>regexp.property.unknown-name</c> (EE0201): in <c>\p{name=value}</c>, a name other than General_Category, gc,
    /// Script, sc, Script_Extensions and scx, exactly as written. Argument: the text between the braces. Position: the <c>p</c>.
    /// </summary>
    UnknownPropertyName = 82,

    /// <summary>
    /// <c>regexp.property.unknown-value</c> (EE0202): in <c>\p{name=value}</c>, a value the named property does not have,
    /// exactly as written (PropertyValueAliases.txt). Argument: the text between the braces. Position: the <c>p</c>.
    /// </summary>
    UnknownPropertyValue = 83,

    /// <summary>
    /// <c>regexp.property.unknown-lone-name</c> (EE0203): in <c>\p{name}</c>, a name that is no General_Category value, no
    /// binary property and, under <c>v</c>, no property of strings, exactly as written. Argument: the text between the
    /// braces. Position: the <c>p</c>.
    /// </summary>
    UnknownLonePropertyName = 84,

    /// <summary>
    /// <c>regexp.property.of-strings-outside-v-mode</c> (EE0204): a property of strings, such as <c>RGI_Emoji</c>, under
    /// <c>u</c>. Argument: the text between the braces. Position: the <c>p</c>.
    /// </summary>
    PropertyOfStringsOutsideUnicodeSets = 85,

    /// <summary>
    /// <c>regexp.property.negated-strings</c> (EE0205): <c>\P{…}</c> of a property of strings. Argument: the text between the
    /// braces. Position: the <c>P</c>.
    /// </summary>
    NegatedPropertyOfStrings = 86,

    /// <summary>
    /// <c>limit.regexp.too-deep</c> (catalogue row EX0026, the limit on a pattern's nesting of groups and classes): not a
    /// syntax error but the caller's nesting limit (requirement R4, decision D7), a
    /// <see cref="RegexStatus.LimitExceeded"/> naming <see cref="RegexLimit.Nesting"/>, given a diagnostic so that an engine can
    /// report it as one. Argument: the limit, in decimal. Position: the group or class that opens the level past it.
    /// </summary>
    NestingLimit = 90,
}

/// <summary>
/// One refusal of a pattern or a flags string, as data (decision D9 of Broiler.VM's JSD-0081; decision SJL-D-11): its
/// <see cref="Code"/>, its <see cref="MessageId"/> and <see cref="Argument"/>, its <see cref="Position"/>, and a default
/// English <see cref="Message"/>. The validate entry and <see cref="BroilerRegex.Compile"/> answer it with
/// <see cref="RegexStatus.SyntaxError"/>, or with <see cref="RegexStatus.LimitExceeded"/> for the nesting limit, and
/// <see cref="Text.RegexFlagsText.Parse"/> with <see cref="RegexStatus.SyntaxError"/> for a flags text; the string-bearing
/// constructors throw an <see cref="System.ArgumentException"/> with its <see cref="Message"/>.
/// </summary>
/// <remarks>
/// The default value is no refusal: <see cref="RegexDiagnosticCode.None"/>, an empty identifier and message, no argument
/// and position -1 being its fields' defaults read through these members. New in task 2.8 of SJL-11.2, with the validate
/// entry; fixed for <c>0.1.0-preview.6</c> by task 2.9 of SJL-11.2.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D9; IP=None; Security=Low; Resources=0; Fingerprint=D31024
// Broiler-Falsified-If: a diagnostic's MessageId is not its code's identifier, or the default value reports a code, an identifier or a position other than None, the empty string and -1
// Broiler-Human:        PENDING
public readonly struct RegexDiagnostic
{
    private readonly string? _message;

    /// <summary>Position plus one, so that the default value reads as -1.</summary>
    private readonly int _positionPlusOne;

    /// <summary>A refusal of kind <paramref name="code"/> at <paramref name="position"/>.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=4410FB
    // Broiler-Human:        PENDING
    internal RegexDiagnostic(RegexDiagnosticCode code, int position, string? argument, string message)
    {
        Code = code;
        _positionPlusOne = position + 1;
        Argument = argument;
        _message = message;
    }

    /// <summary>The kind of refusal.</summary>
    public RegexDiagnosticCode Code { get; }

    /// <summary>The code's message identifier (<see cref="MessageIdOf"/>), or the empty string for no refusal.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D9; IP=None; Security=Low; Resources=0; Fingerprint=2AA998
    // Broiler-Human:        PENDING
    public string MessageId => MessageIdOf(Code);

    /// <summary>
    /// The code-unit offset the refusal names: into the pattern, or into the flags text for the codes of the flags; -1 for
    /// no refusal.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=F57EA0
    // Broiler-Falsified-If: the default value answers a position other than -1
    // Broiler-Human:        PENDING
    public int Position => _positionPlusOne - 1;

    /// <summary>The identifier's argument, as the code's documentation names it, or null when the code takes none.</summary>
    public string? Argument { get; }

    /// <summary>A default English rendering, which is no contract (convention 3): an engine renders its own from the identifier.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=626861
    // Broiler-Human:        PENDING
    public string Message => _message ?? "";

    /// <summary>The message identifier of <paramref name="code"/>, the table <see cref="RegexDiagnosticCode"/> documents; the empty string for <see cref="RegexDiagnosticCode.None"/>.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="code"/> is no member of <see cref="RegexDiagnosticCode"/>.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D9; IP=None; Security=Low; Resources=0; Fingerprint=0FCD51
    // Broiler-Falsified-If: a code's identifier differs from the one its documentation names, or two codes share one
    // Broiler-Human:        PENDING
    public static string MessageIdOf(RegexDiagnosticCode code) => code switch
    {
        RegexDiagnosticCode.None => "",
        RegexDiagnosticCode.UnknownFlag => "regexp.flags.unknown",
        RegexDiagnosticCode.DuplicateFlag => "regexp.flags.duplicate",
        RegexDiagnosticCode.UnicodeWithUnicodeSets => "regexp.flags.unicode-with-unicode-sets",
        RegexDiagnosticCode.UnmatchedParenthesis => "regexp.group.unmatched-close",
        RegexDiagnosticCode.UnterminatedGroup => "regexp.group.unterminated",
        RegexDiagnosticCode.InvalidGroup => "regexp.group.invalid",
        RegexDiagnosticCode.NothingToRepeat => "regexp.quantifier.nothing-to-repeat",
        RegexDiagnosticCode.QuantifiedAssertion => "regexp.quantifier.after-assertion",
        RegexDiagnosticCode.QuantifierOutOfOrder => "regexp.quantifier.out-of-order",
        RegexDiagnosticCode.BracedQuantifierWithoutAtom => "regexp.quantifier.braced-without-atom",
        RegexDiagnosticCode.LoneSyntaxCharacter => "regexp.pattern.lone-syntax-character",
        RegexDiagnosticCode.TrailingBackslash => "regexp.escape.trailing-backslash",
        RegexDiagnosticCode.InvalidIdentityEscape => "regexp.escape.invalid-identity",
        RegexDiagnosticCode.InvalidControlEscape => "regexp.escape.invalid-control",
        RegexDiagnosticCode.InvalidHexEscape => "regexp.escape.invalid-hex",
        RegexDiagnosticCode.InvalidBracedEscape => "regexp.escape.invalid-braced",
        RegexDiagnosticCode.CodePointOutOfRange => "regexp.escape.code-point-out-of-range",
        RegexDiagnosticCode.BackreferenceOutOfRange => "regexp.backreference.group-number-too-large",
        RegexDiagnosticCode.UndefinedGroupName => "regexp.backreference.undefined-group-name",
        RegexDiagnosticCode.NamedReferenceWithoutName => "regexp.backreference.missing-group-name",
        RegexDiagnosticCode.NamedReferenceInClass => "regexp.class.named-reference",
        RegexDiagnosticCode.UnterminatedGroupName => "regexp.group-name.unterminated",
        RegexDiagnosticCode.GroupNameInvalidEscape => "regexp.group-name.invalid-escape",
        RegexDiagnosticCode.GroupNameInvalidStart => "regexp.group-name.invalid-start",
        RegexDiagnosticCode.GroupNameInvalidPart => "regexp.group-name.invalid-part",
        RegexDiagnosticCode.EmptyGroupName => "regexp.group-name.empty",
        RegexDiagnosticCode.DuplicateGroupName => "regexp.group-name.duplicate",
        RegexDiagnosticCode.ModifierRepeated => "regexp.modifiers.repeated-flag",
        RegexDiagnosticCode.ModifiersEmpty => "regexp.modifiers.empty",
        RegexDiagnosticCode.ModifierAddedAndRemoved => "regexp.modifiers.added-and-removed",
        RegexDiagnosticCode.UnterminatedClass => "regexp.class.unterminated",
        RegexDiagnosticCode.ClassRangeOutOfOrder => "regexp.class.range-out-of-order",
        RegexDiagnosticCode.ClassRangeEndpointIsClass => "regexp.class.range-endpoint-is-class",
        RegexDiagnosticCode.NegatedClassMayContainStrings => "regexp.class.negated-may-contain-strings",
        RegexDiagnosticCode.ClassSetMixedOperators => "regexp.class-set.mixed-operators",
        RegexDiagnosticCode.ClassSetAmpersandAfterIntersection => "regexp.class-set.ampersand-after-intersection",
        RegexDiagnosticCode.ClassSetRangeEndpointNotCharacter => "regexp.class-set.range-endpoint-not-character",
        RegexDiagnosticCode.ClassSetUnescapedSyntaxCharacter => "regexp.class-set.unescaped-syntax-character",
        RegexDiagnosticCode.ClassSetReservedDoublePunctuator => "regexp.class-set.reserved-double-punctuator",
        RegexDiagnosticCode.ClassStringDisjunctionWithoutBrace => "regexp.class-set.string-disjunction-without-brace",
        RegexDiagnosticCode.UnterminatedClassStringDisjunction => "regexp.class-set.string-disjunction-unterminated",
        RegexDiagnosticCode.PropertyEscapeWithoutBrace => "regexp.property.without-brace",
        RegexDiagnosticCode.UnterminatedPropertyEscape => "regexp.property.unterminated",
        RegexDiagnosticCode.UnknownPropertyName => "regexp.property.unknown-name",
        RegexDiagnosticCode.UnknownPropertyValue => "regexp.property.unknown-value",
        RegexDiagnosticCode.UnknownLonePropertyName => "regexp.property.unknown-lone-name",
        RegexDiagnosticCode.PropertyOfStringsOutsideUnicodeSets => "regexp.property.of-strings-outside-v-mode",
        RegexDiagnosticCode.NegatedPropertyOfStrings => "regexp.property.negated-strings",
        RegexDiagnosticCode.NestingLimit => "limit.regexp.too-deep",
        _ => throw new System.ArgumentOutOfRangeException(nameof(code), "No member of RegexDiagnosticCode has this number."),
    };

    /// <summary>The refusal of the nesting limit <paramref name="limit"/> at the level that opens at <paramref name="position"/>.</summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D7; IP=None; Security=Low; Resources=0; Fingerprint=F8B068
    // Broiler-Human:        PENDING
    internal static RegexDiagnostic Nesting(int position, int limit, string message) =>
        new(RegexDiagnosticCode.NestingLimit, position, limit.ToString(NumberFormatInfo.InvariantInfo), message);

    /// <summary>
    /// The refusal <paramref name="code"/>, one of the three codes of the flags, of the code unit <paramref name="unit"/> at
    /// <paramref name="position"/> of a flags text (<see cref="Text.RegexFlagsText.Parse"/>), with the message
    /// <c>0.1.0-preview.4</c> gave each.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.3.3 RegExpInitialize and s22.2.3.4 ParsePattern; IP=None; Security=Low; Resources=0; Fingerprint=BE9012
    // Broiler-Falsified-If: "gg" gives other than DuplicateFlag at 1 with the argument g, or "uv" other than UnicodeWithUnicodeSets at 1
    // Broiler-Human:        PENDING
    internal static RegexDiagnostic OfFlags(RegexDiagnosticCode code, char unit, int position) => code switch
    {
        RegexDiagnosticCode.UnknownFlag => new(RegexDiagnosticCode.UnknownFlag, position, unit.ToString(),
            "Invalid regular expression flags: '" + unit + "'"),
        RegexDiagnosticCode.DuplicateFlag => new(RegexDiagnosticCode.DuplicateFlag, position, unit.ToString(),
            "Duplicate regular expression flag: '" + unit + "'"),
        _ => new(RegexDiagnosticCode.UnicodeWithUnicodeSets, position, null, "The 'u' and 'v' regular expression flags cannot be combined"),
    };
}
