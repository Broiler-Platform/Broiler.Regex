using System.Threading;
using Broiler.Unicode.Algorithms;
using Broiler.Unicode.Tables;

namespace Broiler.Regex.Tests;

/// <summary>
/// Compiles a pattern for a test against the Unicode owner's tables, which the library reads through the provider
/// and never carries itself (requirement R11; <c>Broiler.Unicode.Tables</c> is test-only data, decision D11).
/// </summary>
/// <remarks>
/// <b>The backend switch.</b> Every test that matches through the public members builds its instance here, so that
/// <see cref="NewBackendSuiteTests"/> can run the same tests again on the new backend: while
/// <see cref="OnTheNewBackend"/> holds a count in the current execution context, which the threads and tasks a test
/// starts inherit, each instance is built on the library's internal route through the new backend
/// (<c>BroilerRegex.OnTheNewBackend</c>), and the count records how many were. Outside it, the old backend's public
/// constructors and <c>Compile</c> build it, as a caller's would.
/// </remarks>
internal static class TestRegex
{
    /// <summary>The instances the current case built on the new backend, or null when it runs on the old one.</summary>
    internal static readonly AsyncLocal<StrongBox?> OnTheNewBackend = new();

    /// <summary>A count that the threads and tasks of one case share.</summary>
    internal sealed class StrongBox
    {
        private int _value;

        internal int Value => Volatile.Read(ref _value);

        internal void Increment() => Interlocked.Increment(ref _value);
    }

    internal static BroilerRegex Compile(string pattern, string? flags = null) => New(pattern, RegexFlagsParser.Parse(flags), UnicodeTables.Instance);

    internal static BroilerRegex Compile(string pattern, RegexFlags flags) => New(pattern, flags, UnicodeTables.Instance);

    /// <summary>The public constructor's instance, or the new backend's when the switch is on.</summary>
    internal static BroilerRegex New(string pattern, RegexFlags flags, IUnicodeTables tables)
    {
        if (OnTheNewBackend.Value is not { } count)
            return new BroilerRegex(pattern, flags, tables);

        count.Increment();
        return BroilerRegex.OnTheNewBackend(pattern, flags, tables);
    }

    /// <summary>The public constructor taking a flags string: its own flags check, then <see cref="New(string, RegexFlags, IUnicodeTables)"/>.</summary>
    internal static BroilerRegex New(string pattern, string? flags, IUnicodeTables tables) => New(pattern, RegexFlagsParser.Parse(flags), tables);

    /// <summary><see cref="BroilerRegex.Compile"/>, or its route through the new backend when the switch is on.</summary>
    internal static RegexStatus TryCompile(string pattern, RegexFlags flags, IUnicodeTables tables, int nestingLimit, out BroilerRegex? regex, out RegexLimit limit)
    {
        if (OnTheNewBackend.Value is not { } count)
            return BroilerRegex.Compile(pattern, flags, tables, nestingLimit, out regex, out limit);

        count.Increment();
        return BroilerRegex.CompileWithBackend(pattern, flags, tables, nestingLimit, onTheNewBackend: true, out regex, out limit);
    }
}
