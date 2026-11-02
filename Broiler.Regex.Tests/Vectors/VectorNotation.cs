using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Broiler.Regex.VectorsRunner;

/// <summary>
/// The corpus's notation (SCHEMA.md sections 3 and 4) as the runner needs it: the typed values of a vector's
/// <c>in</c> (<c>str:</c> and <c>int:</c>, the two the regular-expression area writes), an outcome's alternatives, and
/// the rendering of a string and a number, written once so that every op renders as the schema does.
/// </summary>
internal static class VectorNotation
{
    /// <summary>A typed input: a string, or an integer for <c>int:</c>.</summary>
    internal readonly record struct Input(string? Text, long Integer)
    {
        internal string Str => Text ?? throw new FormatException("an input is an int: where the op reads a str:");

        internal int Int => Text is null ? checked((int)Integer) : throw new FormatException("an input is a str: where the op reads an int:");
    }

    /// <summary>Decodes a vector's <c>in</c>: the typed values separated by single spaces.</summary>
    /// <exception cref="FormatException">A value the regular-expression area does not write, or one written otherwise than section 3 says.</exception>
    internal static List<Input> Inputs(string text)
    {
        var inputs = new List<Input>();
        var at = 0;
        while (at < text.Length)
        {
            if (text.AsSpan(at).StartsWith("str:\"", StringComparison.Ordinal))
            {
                at += 4;
                inputs.Add(new Input(ReadString(text, ref at), 0));
            }
            else if (text.AsSpan(at).StartsWith("int:", StringComparison.Ordinal))
            {
                var end = text.IndexOf(' ', at);
                if (end < 0)
                    end = text.Length;
                inputs.Add(new Input(null, long.Parse(text.AsSpan(at + 4, end - at - 4), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)));
                at = end;
            }
            else
            {
                throw new FormatException("an input the runner does not read at " + VectorFile.Decimal(at) + ": " + text);
            }

            if (at < text.Length)
            {
                if (text[at] != ' ')
                    throw new FormatException("inputs not separated by one space: " + text);
                at++;
            }
        }

        return inputs;
    }

    /// <summary>Reads a <c>str:</c> literal from its opening quote at <paramref name="at"/>, leaving <paramref name="at"/> past its closing quote.</summary>
    private static string ReadString(string text, ref int at)
    {
        if (text[at] != '"')
            throw new FormatException("a str: value without its quote");
        at++;
        var value = new StringBuilder();
        while (true)
        {
            if (at >= text.Length)
                throw new FormatException("an unterminated str: value: " + text);
            var c = text[at++];
            if (c == '"')
                return value.ToString();
            if (c != '\\')
            {
                value.Append(c);
                continue;
            }

            var escape = text[at++];
            switch (escape)
            {
                case '"':
                case '\\':
                    value.Append(escape);
                    break;
                case 'u':
                    value.Append((char)int.Parse(text.AsSpan(at, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                    at += 4;
                    break;
                default:
                    throw new FormatException("an escape section 3 does not write: \\" + escape);
            }
        }
    }

    /// <summary>
    /// An outcome's alternatives, separated by <c>" | "</c> (section 4): each <c>value: </c> or <c>throw: </c> and a typed
    /// value, read whole so that a <c>|</c> inside a string is not a separator, or <c>error: </c> or <c>status: </c> and a word.
    /// </summary>
    internal static List<string> Alternatives(string outcome)
    {
        var alternatives = new List<string>();
        var at = 0;
        while (true)
        {
            var start = at;
            if (outcome.AsSpan(at).StartsWith("value: ", StringComparison.Ordinal) || outcome.AsSpan(at).StartsWith("throw: ", StringComparison.Ordinal))
            {
                at += 7;
                if (outcome.AsSpan(at).StartsWith("str:\"", StringComparison.Ordinal))
                {
                    at += 4;
                    ReadString(outcome, ref at);
                }
                else
                {
                    while (at < outcome.Length && outcome[at] != ' ')
                        at++;
                }
            }
            else if (outcome.AsSpan(at).StartsWith("error: ", StringComparison.Ordinal) || outcome.AsSpan(at).StartsWith("status: ", StringComparison.Ordinal))
            {
                while (at < outcome.Length && outcome[at] != ' ')
                    at++;
                at++;
                while (at < outcome.Length && outcome[at] != ' ')
                    at++;
            }
            else
            {
                throw new FormatException("an outcome section 4 does not write: " + outcome);
            }

            alternatives.Add(outcome[start..at]);
            if (at == outcome.Length)
                return alternatives;
            if (!outcome.AsSpan(at).StartsWith(" | ", StringComparison.Ordinal))
                throw new FormatException("alternatives not separated by \" | \": " + outcome);
            at += 3;
        }
    }

    /// <summary>A String in section 3's notation: printable ASCII as itself but <c>"</c> and <c>\</c>, every other code unit <c>\uXXXX</c>.</summary>
    internal static string Str(string value)
    {
        var text = new StringBuilder("str:\"", value.Length + 6);
        foreach (var c in value)
        {
            if (c == '"')
                text.Append("\\\"");
            else if (c == '\\')
                text.Append("\\\\");
            else if (c is >= ' ' and <= '~')
                text.Append(c);
            else
                text.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
        }

        return text.Append('"').ToString();
    }

    /// <summary>A Number by its IEEE 754 bit pattern (section 3), the only way the corpus writes one.</summary>
    internal static string F64(double value) =>
        "f64:0x" + BitConverter.DoubleToInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture);
}
