using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Broiler.Regex.Test262Library;

/// <summary>The kind of one significant token of <see cref="JavaScriptTokens"/>.</summary>
internal enum TokenKind
{
    /// <summary>An identifier, a keyword or a numeric literal: a run of identifier characters.</summary>
    Name,

    /// <summary>One punctuator character but <c>)</c> and <c>]</c>.</summary>
    Punctuator,

    /// <summary><c>)</c> or <c>]</c>, after which a <c>/</c> divides.</summary>
    Close,

    /// <summary>A string literal; <see cref="Token.Text"/> is its String value.</summary>
    String,

    /// <summary>A template literal, skipped.</summary>
    Template,

    /// <summary>A regular-expression literal; <see cref="Token.Text"/> is its body, <see cref="Token.Flags"/> its flags.</summary>
    RegularExpression,
}

/// <summary>One significant token: its kind, its text (a string literal's value), a literal's flags and its offset.</summary>
internal readonly record struct Token(TokenKind Kind, string Text, string Flags, int Offset);

/// <summary>
/// The runner's declared grammar for test262's sources (task 2.10 of SJL-11.2; bundle <c>sjl-11-001</c>'s check B-2,
/// "whose regular-expression literals the runner's declared grammar extracts"). It is the grammar of
/// <c>scripts/reference-arms/generate_population.py</c>'s <c>scan_javascript</c>, which writes population P2 from the same
/// suite, read over UTF-16 code units, with two changes: a literal's flags are every identifier character and backslash
/// after its closing slash, so that an escape in the flags is part of them and the flag parser refuses it; and a
/// backslash before a line terminator ends the body with no literal (RegularExpressionBackslashSequence), where
/// <c>scan_javascript</c> steps over both.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>White space and line terminators separate tokens; <c>//</c> and <c>/* */</c> comments are skipped.</item>
/// <item>A string literal, single- or double-quoted, ends at its closing quote or at an unescaped line feed or carriage
/// return; its value is read by ECMA-262 12.9.4's escapes (<see cref="StringValue"/>).</item>
/// <item>A template literal is skipped, the text of each substitution scanned as code.</item>
/// <item>A <c>/</c> begins a regular-expression literal when the previous significant token is nothing, a punctuator
/// whose character is one of <c>(,=:[!&amp;|?{};+-*%&lt;&gt;~^</c>, or one of the names <c>return typeof instanceof in of
/// new delete void throw case do else yield await</c>; the body runs to the first <c>/</c> outside a class that no
/// backslash escapes, and never past a line terminator, which no backslash escapes either, or the <c>/</c> is a
/// punctuator.</item>
/// <item>A name is a run of ASCII letters, digits, <c>_</c>, <c>$</c>, backslashes and code units from U+0080; every
/// other character is a punctuator of its own, <c>)</c> and <c>]</c> as <see cref="TokenKind.Close"/>.</item>
/// </list>
/// </remarks>
internal static class JavaScriptTokens
{
    private const string PunctuatorsBeforeLiteral = "(,=:[!&|?{};+-*%<>~^";

    /// <summary>The significant tokens of a source, in order.</summary>
    internal static List<Token> Scan(string source)
    {
        var tokens = new List<Token>();
        var templates = new Stack<int>();
        var braces = 0;
        var i = 0;
        var n = source.Length;
        while (i < n)
        {
            var ch = source[i];
            if (IsSpace(ch))
            {
                i++;
                continue;
            }

            if (ch == '/' && i + 1 < n && source[i + 1] == '/')
            {
                while (i < n && !IsLineTerminator(source[i]))
                    i++;
                continue;
            }

            if (ch == '/' && i + 1 < n && source[i + 1] == '*')
            {
                var end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? n : end + 2;
                continue;
            }

            if (ch is '\'' or '"')
            {
                var j = i + 1;
                while (j < n && source[j] != ch)
                {
                    if (source[j] == '\\')
                        j++;
                    else if (source[j] is '\n' or '\r')
                        break;
                    j++;
                }

                tokens.Add(new Token(TokenKind.String, StringValue(source.AsSpan(i + 1, Math.Min(j, n) - i - 1)), "", i));
                i = j + 1;
                continue;
            }

            if (ch == '`' || (ch == '}' && templates.Count > 0 && templates.Peek() == braces))
            {
                if (ch == '}')
                    templates.Pop();
                var j = i + 1;
                var closed = false;
                while (j < n && source[j] != '`')
                {
                    if (source[j] == '\\')
                    {
                        j += 2;
                        continue;
                    }

                    if (source[j] == '$' && j + 1 < n && source[j + 1] == '{')
                    {
                        templates.Push(braces);
                        j += 2;
                        closed = true;
                        break;
                    }

                    j++;
                }

                if (!closed)
                    j++;
                tokens.Add(new Token(TokenKind.Template, "", "", i));
                i = j;
                continue;
            }

            if (ch == '/')
            {
                if (LiteralMayBegin(tokens) && TryLiteral(source, i, out var body, out var flags, out var next))
                {
                    tokens.Add(new Token(TokenKind.RegularExpression, body, flags, i));
                    i = next;
                    continue;
                }

                tokens.Add(new Token(TokenKind.Punctuator, "/", "", i));
                i++;
                continue;
            }

            if (IsNamePart(ch) || ch == '\\')
            {
                var j = i;
                while (j < n && (IsNamePart(source[j]) || source[j] == '\\'))
                    j++;
                tokens.Add(new Token(TokenKind.Name, source[i..j], "", i));
                i = j;
                continue;
            }

            if (ch == '{')
                braces++;
            else if (ch == '}')
                braces--;
            tokens.Add(new Token(ch is ')' or ']' ? TokenKind.Close : TokenKind.Punctuator, ch.ToString(), "", i));
            i++;
        }

        return tokens;
    }

    private static bool LiteralMayBegin(List<Token> tokens)
    {
        if (tokens.Count == 0)
            return true;
        var previous = tokens[^1];
        return previous.Kind switch
        {
            TokenKind.Punctuator => PunctuatorsBeforeLiteral.Contains(previous.Text[^1], StringComparison.Ordinal),
            TokenKind.Name => previous.Text is "return" or "typeof" or "instanceof" or "in" or "of" or "new" or "delete"
                or "void" or "throw" or "case" or "do" or "else" or "yield" or "await",
            _ => false,
        };
    }

    private static bool TryLiteral(string source, int start, out string body, out string flags, out int next)
    {
        var n = source.Length;
        var j = start + 1;
        var inClass = false;
        while (j < n && !IsLineTerminator(source[j]))
        {
            var c = source[j];
            if (c == '\\')
            {
                // RegularExpressionBackslashSequence: a backslash never escapes a line terminator.
                if (j + 1 < n && IsLineTerminator(source[j + 1]))
                    break;
                j += 2;
                continue;
            }

            if (c == '[')
                inClass = true;
            else if (c == ']')
                inClass = false;
            else if (c == '/' && !inClass)
                break;
            j++;
        }

        if (j >= n || source[j] != '/')
        {
            body = flags = "";
            next = start;
            return false;
        }

        var k = j + 1;
        while (k < n && (IsNamePart(source[k]) || source[k] == '\\'))
            k++;
        body = source[(start + 1)..j];
        flags = source[(j + 1)..k];
        next = k;
        return true;
    }

    /// <summary>
    /// The String value of a string literal's body (ECMA-262 12.9.4): the single-character escapes, <c>\0</c> and the
    /// legacy octal escapes, <c>\x</c>, <c>\u</c> and <c>\u{}</c>, a line continuation, and an identity escape. A
    /// <c>\u{}</c> above U+FFFF is its surrogate pair; a lone surrogate stays one code unit, so that two escapes of a
    /// lead and a trail surrogate make the pair, as the UTF-16 text does.
    /// </summary>
    internal static string StringValue(ReadOnlySpan<char> body)
    {
        var text = new StringBuilder(body.Length);
        var i = 0;
        while (i < body.Length)
        {
            var ch = body[i];
            if (ch != '\\')
            {
                text.Append(ch);
                i++;
                continue;
            }

            i++;
            if (i >= body.Length)
                break;
            ch = body[i];
            switch (ch)
            {
                case 'n': text.Append('\n'); i++; break;
                case 't': text.Append('\t'); i++; break;
                case 'r': text.Append('\r'); i++; break;
                case 'b': text.Append('\b'); i++; break;
                case 'f': text.Append('\f'); i++; break;
                case 'v': text.Append('\v'); i++; break;
                case 'x' when Hex(body, i + 1, 2, out var unit):
                    text.Append((char)unit);
                    i += 3;
                    break;
                case 'u' when i + 1 < body.Length && body[i + 1] == '{':
                {
                    var end = body[i..].IndexOf('}');
                    if (end < 0 || !Hex(body, i + 2, end - 2, out var codePoint) || codePoint > 0x10FFFF)
                        throw new FormatException("a \\u{} escape the string reader cannot read");
                    AppendCodePoint(text, codePoint);
                    i += end + 1;
                    break;
                }
                case 'u' when Hex(body, i + 1, 4, out var unit):
                    text.Append((char)unit);
                    i += 5;
                    break;
                case >= '0' and <= '7':
                {
                    var length = 1;
                    var limit = ch <= '3' ? 3 : 2;
                    while (length < limit && i + length < body.Length && body[i + length] is >= '0' and <= '7')
                        length++;
                    var value = 0;
                    for (var k = 0; k < length; k++)
                        value = value * 8 + (body[i + k] - '0');
                    text.Append((char)value);
                    i += length;
                    break;
                }
                case '\r':
                    i += i + 1 < body.Length && body[i + 1] == '\n' ? 2 : 1;
                    break;
                case '\n' or '\u2028' or '\u2029':
                    i++;
                    break;
                default:
                    text.Append(ch);
                    i++;
                    break;
            }
        }

        return text.ToString();
    }

    /// <summary>A code point as UTF-16: one code unit up to U+FFFF (a lone surrogate among them), a pair above it.</summary>
    internal static void AppendCodePoint(StringBuilder text, int codePoint)
    {
        if (codePoint <= 0xFFFF)
        {
            text.Append((char)codePoint);
            return;
        }

        var offset = codePoint - 0x10000;
        text.Append((char)(0xD800 + (offset >> 10)));
        text.Append((char)(0xDC00 + (offset & 0x3FF)));
    }

    /// <summary>A numeric literal of the generated files: <c>0x</c> and hexadecimal digits, or decimal digits.</summary>
    internal static bool TryNumber(string text, out int value)
    {
        if (text.StartsWith("0x", StringComparison.Ordinal) || text.StartsWith("0X", StringComparison.Ordinal))
            return int.TryParse(text.AsSpan(2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out value);
        return int.TryParse(text, NumberStyles.None, NumberFormatInfo.InvariantInfo, out value);
    }

    private static bool Hex(ReadOnlySpan<char> body, int start, int length, out int value)
    {
        value = 0;
        if (length <= 0 || start + length > body.Length)
            return false;
        foreach (var c in body.Slice(start, length))
        {
            var digit = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'f' => c - 'a' + 10,
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => -1,
            };
            if (digit < 0 || value > 0x10FFFF)
                return false;
            value = value * 16 + digit;
        }

        return true;
    }

    private static bool IsSpace(char ch) =>
        ch is ' ' or '\t' or '\r' or '\n' or '\u2028' or '\u2029' or '\uFEFF' or '\u00A0' or '\v' or '\f';

    private static bool IsLineTerminator(char ch) => ch is '\n' or '\r' or '\u2028' or '\u2029';

    private static bool IsNamePart(char ch) =>
        ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '$' || ch >= '\u0080';
}
