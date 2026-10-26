using Broiler.Unicode.Tables;

namespace Broiler.Regex.Tests;

/// <summary>
/// Compiles a pattern for a test against the Unicode owner's tables, which the library reads through the provider
/// and never carries itself (requirement R11; <c>Broiler.Unicode.Tables</c> is test-only data, decision D11).
/// </summary>
internal static class TestRegex
{
    internal static BroilerRegex Compile(string pattern, string? flags = null) => new(pattern, flags, UnicodeTables.Instance);

    internal static BroilerRegex Compile(string pattern, RegexFlags flags) => new(pattern, flags, UnicodeTables.Instance);
}
