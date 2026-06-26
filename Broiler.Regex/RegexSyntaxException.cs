using System;

namespace Broiler.Regex;

/// <summary>
/// Thrown when a pattern or flags string is not a valid ECMAScript regular
/// expression. The JavaScript layer maps this to a <c>SyntaxError</c>.
/// </summary>
public sealed class RegexSyntaxException(string message, int position = -1) : Exception(message)
{
    /// <summary>Zero-based offset into the pattern where the error was detected, or -1.</summary>
    public int Position { get; } = position;
}
