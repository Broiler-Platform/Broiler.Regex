using System.Collections.Generic;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Where a <c>v</c>-mode class-set expression ends (ECMA-262 §22.2.1): a ClassIntersection or a ClassSubtraction ends at
/// its last ClassSetOperand, which only another operator of the same kind or the class's <c>]</c> may follow, so an
/// operand after it is a syntax error, and a class the pattern ends inside is an unterminated one.
/// </summary>
/// <remarks>
/// Until the integration of tasks 2.6 and 2.11 the parser refused only an operator of the other kind there and then took
/// whatever character followed for the closing <c>]</c> without reading it: <c>/[a--bc/v</c> and <c>/[a&amp;&amp;ac/v</c>
/// compiled and matched <c>a</c>, as <c>/[a--b]/v</c> and <c>/[a&amp;&amp;a]/v</c> do, and <c>/[a--bc]/v</c> was refused
/// for the stray <c>]</c> the swallowed <c>c</c> left. The fuzz target's long run found it (iterations 215,039 and
/// 814,524 of seed 20261027).
/// </remarks>
public class ClassSetExpressionEndTests
{
    private const string Mixed = "A class-set expression may not mix union, '&&' and '--' without nesting";

    /// <summary>The refusal through both entries: the constructor's exception and <c>Compile</c>'s, the same message at the same position.</summary>
    private static RegexSyntaxException Refusal(string pattern, RegexFlags flags)
    {
        var thrown = Assert.Throws<RegexSyntaxException>(() => TestRegex.Compile(pattern, flags));
        var compiled = Assert.Throws<RegexSyntaxException>(() => TestRegex.TryCompile(pattern, flags, UnicodeTables.Instance, 512, out _, out _));
        Assert.Equal(thrown.Message, compiled.Message);
        Assert.Equal(thrown.Position, compiled.Position);
        return thrown;
    }

    /// <summary>
    /// Every operator, every operand that can follow its last one (a character, an escape, a class escape, a property, a
    /// nested class, a <c>\q{…}</c>, a lone <c>-</c> or <c>&amp;</c>), at nesting depths 0 to 3, closed and unclosed, with
    /// and without <c>^</c> and <c>i</c>.
    /// </summary>
    public static IEnumerable<object[]> TrailingOperands()
    {
        string[] operators = ["--", "&&"];
        string[] trailing = ["c", @"\x63", @"\d", @"\p{L}", "[c]", @"\q{c}", "-c", "&c", @"\&", "\U0001F600"];
        foreach (var op in operators)
        {
            foreach (var tail in trailing)
            {
                for (var depth = 0; depth <= 3; depth++)
                {
                    foreach (var closed in new[] { true, false })
                    {
                        foreach (var negated in new[] { false, true })
                        {
                            var open = new string('[', depth) + (negated ? "[^" : "[");
                            var inner = "a" + op + "b" + tail;
                            var pattern = open + inner + (closed ? new string(']', depth + 1) : "");
                            yield return [pattern, open.Length];
                        }
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(TrailingOperands))]
    public void An_operand_after_the_last_operand_of_an_intersection_or_a_subtraction_is_a_syntax_error(string pattern, int expressionStart)
    {
        foreach (var flags in new[] { RegexFlags.UnicodeSets, RegexFlags.UnicodeSets | RegexFlags.IgnoreCase })
        {
            var refusal = Refusal(pattern, flags);
            Assert.Equal(Mixed, refusal.Message);
            Assert.Equal(expressionStart, refusal.Position);
        }
    }

    [Theory]
    // The fuzz target's reductions and the brief's, and the other operators' forms.
    [InlineData("[a--bc", 1)]
    [InlineData("[a&&ac", 1)]
    [InlineData("[a--bc]", 1)]
    [InlineData("[a&&ac]", 1)]
    [InlineData("[[a]--B\r", 1)]                       // the long run's reduction: a raw carriage return after the last operand
    [InlineData(@"[[a]--B\r", 1)]                      // and its EscapePattern text, whose backslash was taken for the ']'
    [InlineData("[[a]--[b]c]", 1)]
    [InlineData(@"[\q{a}--bc", 1)]
    [InlineData("[a--b--cd]", 1)]
    [InlineData("[a&&b&&cd]", 1)]
    [InlineData("[a--b-c]", 1)]
    [InlineData("[a&&b&c]", 1)]
    [InlineData("[x[a--bc]]", 3)]
    public void A_trailing_operand_is_refused_where_the_expression_begins(string pattern, int position)
    {
        var refusal = Refusal(pattern, RegexFlags.UnicodeSets);
        Assert.Equal(Mixed, refusal.Message);
        Assert.Equal(position, refusal.Position);
    }

    [Theory]
    // A class the pattern ends inside, after an operator's last operand, is unterminated, at its own '['.
    [InlineData("[a--b", 0)]
    [InlineData("[a&&a", 0)]
    [InlineData("[[a]--[b]", 0)]
    [InlineData("[a&&[b]", 0)]
    [InlineData(@"[\q{a}--b", 0)]
    [InlineData("[[a--b]", 0)]
    [InlineData("x[[a--b]]y[a&&b", 10)]
    public void A_class_left_open_after_an_operator_s_last_operand_is_unterminated(string pattern, int position)
    {
        var refusal = Refusal(pattern, RegexFlags.UnicodeSets);
        Assert.Equal("Unterminated character class", refusal.Message);
        Assert.Equal(position, refusal.Position);
    }

    [Theory]
    // The same operators, closed where ECMA-262 closes them: nothing here changes.
    [InlineData("[a--b]", "a", "match 0,1")]
    [InlineData("[a&&a]", "a", "match 0,1")]
    [InlineData("[[a--b]c]", "c", "match 0,1")]
    [InlineData("[a--[bc]]", "a", "match 0,1")]
    [InlineData("[[a&&a][b--c]]", "b", "match 0,1")]
    [InlineData(@"[\q{ab|c}--c]", "ab", "match 0,2")]
    [InlineData("[[[a--b]]]", "a", "match 0,1")]
    public void An_expression_closed_by_its_bracket_is_unchanged(string pattern, string subject, string expected)
        => Assert.Equal(expected, TestRegex.Answer(TestRegex.Compile(pattern, "v"), subject, 0));
}
