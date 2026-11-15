using System;
using System.Collections.Generic;
using System.Text;

namespace Broiler.Regex.Test262Library;

/// <summary>A regular-expression literal of a source: its body and its flags, as written.</summary>
internal readonly record struct Literal(string Body, string Flags);

/// <summary>What one call of test262's <c>harness/regExpUtils.js</c> asks, read from a generated file.</summary>
internal abstract record HelperCall(Literal RegExp, string Expression);

/// <summary>
/// <c>testPropertyEscapes(regExp, string, expression)</c>: <c>regExp.test(string)</c>, or else <c>regExp.test(symbol)</c>
/// for every code point of the string, each a symbol as <c>for (let symbol of string)</c> reads it.
/// </summary>
internal sealed record PropertyEscapesCall(Literal RegExp, string Expression, string Subject) : HelperCall(RegExp, Expression);

/// <summary>
/// <c>testPropertyOfStrings(args)</c>, which <c>testExtendedCharacterClass</c> names too: <c>regExp.test</c> of the match
/// strings joined, or else of each; and, when the non-match strings are given and their join matches, no non-match
/// string matching.
/// </summary>
internal sealed record PropertyOfStringsCall(Literal RegExp, string Expression, IReadOnlyList<string> MatchStrings,
    IReadOnlyList<string>? NonMatchStrings) : HelperCall(RegExp, Expression);

/// <summary><c>assert(!regExp.test(subject), message)</c>: the literal does not match the subject.</summary>
internal sealed record NoMatchAssertion(Literal RegExp, string Expression, string Subject) : HelperCall(RegExp, Expression);

/// <summary>A file of test262's <c>property-escapes/generated</c> or <c>unicodeSets/generated</c>, read.</summary>
/// <param name="Calls">The helper calls, in order; empty for a negative file.</param>
/// <param name="Literals">The literals of a negative file, after <c>$DONOTEVALUATE()</c>; empty otherwise.</param>
internal sealed record GeneratedFile(IReadOnlyList<HelperCall> Calls, IReadOnlyList<Literal> Literals);

/// <summary>
/// The runner's structured reader of the generated files (bundle <c>sjl-11-001</c>'s check B-2, "through its structured
/// helpers"). A file is read token by token (<see cref="JavaScriptTokens"/>) as a sequence of these statements and
/// nothing else, each with an optional <c>;</c>:
/// <list type="bullet">
/// <item><c>const name = buildString({ loneCodePoints: [n, ...], ranges: [[n, n], ...] })</c>, the string of
/// <c>harness/regExpUtils.js</c>'s <c>buildString</c>: <c>String.fromCodePoint</c> of the lone code points, then of every
/// code point of each range in order, concatenated as UTF-16;</item>
/// <item><c>testPropertyEscapes(/literal/, name, "expression")</c>;</item>
/// <item><c>testPropertyOfStrings({...})</c> or <c>testExtendedCharacterClass({...})</c>, whose object's properties are
/// <c>regExp</c> (a literal), <c>expression</c> (a string), <c>matchStrings</c> and <c>nonMatchStrings</c> (arrays of
/// strings), in any order;</item>
/// <item><c>assert(!/literal/.test("string"), "message")</c>;</item>
/// <item><c>$DONOTEVALUATE()</c> followed by literals, each a statement: a negative file.</item>
/// </list>
/// Anything else makes the file unreadable, with the reason (<see cref="FormatException"/>).
/// </summary>
internal sealed class GeneratedReader
{
    private readonly List<Token> _tokens;
    private readonly Dictionary<string, string> _strings = new(StringComparer.Ordinal);
    private int _at;

    private GeneratedReader(List<Token> tokens) => _tokens = tokens;

    /// <summary>Reads a generated file's code (the text after its front matter), or throws <see cref="FormatException"/>.</summary>
    internal static GeneratedFile Read(string code)
    {
        var reader = new GeneratedReader(JavaScriptTokens.Scan(code));
        return reader.ReadFile();
    }

    private GeneratedFile ReadFile()
    {
        var calls = new List<HelperCall>();
        var literals = new List<Literal>();
        var negative = false;
        while (_at < _tokens.Count)
        {
            var token = _tokens[_at];
            if (negative)
            {
                literals.Add(LiteralToken());
            }
            else if (token.Kind == TokenKind.Name && token.Text == "$DONOTEVALUATE" && calls.Count == 0 && _strings.Count == 0)
            {
                _at++;
                Expect("(");
                Expect(")");
                negative = true;
            }
            else if (token.Kind == TokenKind.Name && token.Text == "const")
            {
                _at++;
                var name = NameToken();
                Expect("=");
                if (NameToken() != "buildString")
                    throw Unreadable("a const that is not a buildString call");
                Expect("(");
                _strings[name] = BuildString();
                Expect(")");
            }
            else if (token.Kind == TokenKind.Name && token.Text == "testPropertyEscapes")
            {
                _at++;
                Expect("(");
                var literal = LiteralToken();
                Expect(",");
                var name = NameToken();
                if (!_strings.TryGetValue(name, out var subject))
                    throw Unreadable("testPropertyEscapes reads " + name + ", which no buildString wrote");
                Expect(",");
                var expression = StringToken();
                Expect(")");
                calls.Add(new PropertyEscapesCall(literal, expression, subject));
            }
            else if (token.Kind == TokenKind.Name && token.Text is "testPropertyOfStrings" or "testExtendedCharacterClass")
            {
                _at++;
                Expect("(");
                calls.Add(PropertyOfStrings());
                Expect(")");
            }
            else if (token.Kind == TokenKind.Name && token.Text == "assert")
            {
                _at++;
                Expect("(");
                Expect("!");
                var literal = LiteralToken();
                Expect(".");
                if (NameToken() != "test")
                    throw Unreadable("an assertion that does not call test");
                Expect("(");
                var subject = StringToken();
                Expect(")");
                Expect(",");
                var message = StringToken();
                Expect(")");
                calls.Add(new NoMatchAssertion(literal, message, subject));
            }
            else
            {
                throw Unreadable("a statement the structured reader does not know, at " + Describe(token));
            }

            if (_at < _tokens.Count && IsPunctuator(_tokens[_at], ";"))
                _at++;
        }

        if (negative && literals.Count == 0)
            throw Unreadable("$DONOTEVALUATE() with no literal after it");
        if (!negative && calls.Count == 0)
            throw Unreadable("no helper call");
        return new GeneratedFile(calls, literals);
    }

    private string BuildString()
    {
        Expect("{");
        var text = new StringBuilder();
        List<int>? lone = null;
        List<(int, int)>? ranges = null;
        while (!IsPunctuator(Peek(), "}"))
        {
            var key = NameToken();
            Expect(":");
            Expect("[");
            if (key == "loneCodePoints" && lone is null)
            {
                lone = [];
                while (Peek().Kind != TokenKind.Close)
                {
                    lone.Add(NumberToken());
                    if (!IsPunctuator(Peek(), ","))
                        break;
                    _at++;
                }
            }
            else if (key == "ranges" && ranges is null)
            {
                ranges = [];
                while (Peek().Kind != TokenKind.Close)
                {
                    Expect("[");
                    var start = NumberToken();
                    Expect(",");
                    var end = NumberToken();
                    ExpectClose("]");
                    ranges.Add((start, end));
                    if (!IsPunctuator(Peek(), ","))
                        break;
                    _at++;
                }
            }
            else
            {
                throw Unreadable("buildString's argument has the property " + key + " twice or one it does not know");
            }

            ExpectClose("]");
            if (!IsPunctuator(Peek(), ","))
                break;
            _at++;
        }

        Expect("}");
        if (lone is null || ranges is null)
            throw Unreadable("buildString's argument lacks loneCodePoints or ranges");
        foreach (var codePoint in lone)
            JavaScriptTokens.AppendCodePoint(text, CodePoint(codePoint));
        foreach (var (start, end) in ranges)
        {
            for (var codePoint = CodePoint(start); codePoint <= CodePoint(end); codePoint++)
                JavaScriptTokens.AppendCodePoint(text, codePoint);
        }

        return text.ToString();
    }

    private PropertyOfStringsCall PropertyOfStrings()
    {
        Expect("{");
        Literal? literal = null;
        string? expression = null;
        List<string>? matches = null, nonMatches = null;
        while (!IsPunctuator(Peek(), "}"))
        {
            var key = NameToken();
            Expect(":");
            switch (key)
            {
                case "regExp" when literal is null:
                    literal = LiteralToken();
                    break;
                case "expression" when expression is null:
                    expression = StringToken();
                    break;
                case "matchStrings" when matches is null:
                    matches = Strings();
                    break;
                case "nonMatchStrings" when nonMatches is null:
                    nonMatches = Strings();
                    break;
                default:
                    throw Unreadable("the helper's argument has the property " + key + " twice or one it does not know");
            }

            if (!IsPunctuator(Peek(), ","))
                break;
            _at++;
        }

        Expect("}");
        if (literal is null || expression is null || matches is null)
            throw Unreadable("the helper's argument lacks regExp, expression or matchStrings");
        return new PropertyOfStringsCall(literal.Value, expression, matches, nonMatches);
    }

    private List<string> Strings()
    {
        Expect("[");
        var strings = new List<string>();
        while (Peek().Kind != TokenKind.Close)
        {
            strings.Add(StringToken());
            if (!IsPunctuator(Peek(), ","))
                break;
            _at++;
        }

        ExpectClose("]");
        return strings;
    }

    private static int CodePoint(int value) =>
        value is >= 0 and <= 0x10FFFF ? value : throw Unreadable("a code point past U+10FFFF");

    private Token Peek() => _at < _tokens.Count ? _tokens[_at] : throw Unreadable("the file ends inside a statement");

    private Token Next()
    {
        var token = Peek();
        _at++;
        return token;
    }

    private void Expect(string punctuator)
    {
        var token = Next();
        if (!IsPunctuator(token, punctuator))
            throw Unreadable("'" + punctuator + "' expected at " + Describe(token));
    }

    private void ExpectClose(string close)
    {
        var token = Next();
        if (token.Kind != TokenKind.Close || token.Text != close)
            throw Unreadable("'" + close + "' expected at " + Describe(token));
    }

    private string NameToken()
    {
        var token = Next();
        return token.Kind == TokenKind.Name ? token.Text : throw Unreadable("a name expected at " + Describe(token));
    }

    private int NumberToken()
    {
        var token = Next();
        return token.Kind == TokenKind.Name && JavaScriptTokens.TryNumber(token.Text, out var value)
            ? value
            : throw Unreadable("a number expected at " + Describe(token));
    }

    private string StringToken()
    {
        var token = Next();
        return token.Kind == TokenKind.String ? token.Text : throw Unreadable("a string expected at " + Describe(token));
    }

    private Literal LiteralToken()
    {
        var token = Next();
        return token.Kind == TokenKind.RegularExpression
            ? new Literal(token.Text, token.Flags)
            : throw Unreadable("a regular-expression literal expected at " + Describe(token));
    }

    private static bool IsPunctuator(Token token, string text) =>
        token.Kind is TokenKind.Punctuator or TokenKind.Close && token.Text == text;

    private static string Describe(Token token) =>
        "offset " + token.Offset.ToString(System.Globalization.NumberFormatInfo.InvariantInfo) + " (" + token.Kind switch
        {
            TokenKind.String => "a string",
            TokenKind.RegularExpression => "a literal",
            TokenKind.Template => "a template",
            _ => token.Text,
        } + ")";

    private static FormatException Unreadable(string why) => new(why);
}
