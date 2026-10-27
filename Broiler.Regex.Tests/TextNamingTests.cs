using System;
using System.Linq;
using System.Reflection;
using Broiler.Regex.Text;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Decision D20 of Broiler.VM's JSD-0081 moves both engines' flag parsers and escape helpers onto requirement R9's
/// helpers, and decision D5's rule, no code of an engine ported, extends to them: the engines' members are the
/// specification of what each needs, and the helpers are written new, in this library's vocabulary. So
/// <c>Broiler.Regex.Text</c> declares none of those members' names, nor their names spelled <c>Regex</c> for
/// <c>RegExp</c>, as <see cref="EngineNamingTests"/> holds the new backend to Broiler.VM's matcher's names.
/// </summary>
public class TextNamingTests
{
    /// <summary>
    /// Broiler.VM's realm (<c>JsRealm.RegExp.cs</c>) and Broiler.JS's <c>JSRegExp.cs</c>: the flag parsers, the escape
    /// helpers and the <c>source</c> writers decision D20 lists, with the helpers they call.
    /// </summary>
    private static readonly string[] EngineNames =
    [
        "RegExpNormalizeFlags", "RegExpHasFlag", "RegExpFlagProperties", "RegExpEscapeText", "RegExpEscapeCodePoint",
        "RegExpEscapeHex", "RegExpSourceText",
        "ParseFlags", "BuildFlagsString", "Escape", "TryAppendEscape", "AppendHexEscape", "AppendUnicodeEscape",
        "IsAsciiLetterOrDigit", "IsSyntaxCharacter", "IsOtherPunctuator", "EscapePatternSource", "TryAppendLineTerminatorEscape",
    ];

    /// <summary>Every member the text helpers' namespace declares, by name, compiler-generated ones left out, and its types' names.</summary>
    private static string[] DeclaredNames()
    {
        var types = typeof(RegexEscaper).Assembly.GetTypes().Where(static t => t.Namespace == "Broiler.Regex.Text");
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        return types.SelectMany(static t => t.GetMembers(all).Select(static m => m.Name).Append(t.Name))
            .Where(static n => !n.Contains('<', StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    [Fact]
    public void The_text_helpers_declare_none_of_the_engines_names_for_the_members_they_replace()
    {
        var declared = DeclaredNames();
        Assert.Contains(nameof(RegexEscaper.EscapeString), declared);
        Assert.Contains(nameof(RegexFlagsText.Parse), declared);

        var refused = EngineNames.Concat(EngineNames.Select(static n => n.Replace("RegExp", "Regex", StringComparison.Ordinal))).ToArray();
        Assert.Empty(declared.Where(n => refused.Any(r => string.Equals(n.TrimStart('_'), r, StringComparison.OrdinalIgnoreCase))));
    }
}
