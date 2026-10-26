using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Broiler.Regex.Ast;
using Broiler.Regex.Parsing;
using Broiler.Unicode.Algorithms;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Requirement R11 (decision D11 of Broiler.VM's JSD-0081; bundle sjl-11-001's check A-10): the library reads its
/// Unicode data through the Unicode owner's provider, <c>Broiler.Unicode.Algorithms</c>, over the tables the caller
/// hands in, and carries none of its own.
/// </summary>
public class ProviderDataTests
{
    private static readonly Assembly Library = typeof(BroilerRegex).Assembly;

    /// <summary>A provider that answers the owner's tables and records every table it is asked for.</summary>
    private sealed class RecordingTables(IUnicodeTables inner, string? version = null) : IUnicodeTables
    {
        public HashSet<UnicodeTable> Read { get; } = [];

        public string UnicodeVersion => version ?? inner.UnicodeVersion;

        public ReadOnlySpan<byte> Table(UnicodeTable table)
        {
            lock (Read)
                Read.Add(table);
            return inner.Table(table);
        }
    }

    /// <summary>Patterns that reach every kind of Unicode answer the library reads, with a subject and the expected first match.</summary>
    public static readonly TheoryData<string, string, string, int> Reaches = new()
    {
        { @"\p{Script=Greek}+", "u", "123αβ", 3 },                // a name=value property
        { @"\p{Lu}", "u", "aB", 1 },                              // a lone General_Category value
        { @"\p{ASCII_Hex_Digit}+", "u", "xyzF0", 3 },             // a binary property
        { @"\p{RGI_Emoji}", "v", "a👍🏽b", 1 },                     // a property of strings
        { "ſ", "iu", "S", 0 },                                    // simple case folding
        { "k", "i", "K", 0 },                                     // the upper-casing branch
        { @"\w", "iu", "\u212A", 0 },                        // the folded word set
        { @"\s", "", "a\u3000", 1 },                         // Space_Separator
        { @"[^a]", "vi", "Aab", 2 },                              // AllCharacters under v with i
    };

    [Theory]
    [MemberData(nameof(Reaches))]
    public void Every_answer_is_read_through_the_tables_the_caller_hands_in(string pattern, string flags, string subject, int index)
    {
        var recording = new RecordingTables(UnicodeTables.Instance);
        var match = new BroilerRegex(pattern, flags, recording).Match(subject);

        Assert.True(match.Success);
        Assert.Equal(index, match.Index);
        Assert.Equal(TestRegex.Compile(pattern, flags).Match(subject).Index, match.Index);

        // The four groups of tables the provider's algorithms read: properties, case folding, the properties of
        // strings and the lexical classes. Each is read through the caller's provider, whatever the pattern.
        Assert.Contains(UnicodeTable.PropertyRanges, recording.Read);
        Assert.Contains(UnicodeTable.FoldData, recording.Read);
        Assert.Contains(UnicodeTable.UpperData, recording.Read);
        Assert.Contains(UnicodeTable.StringPropertyData, recording.Read);
        Assert.Contains(UnicodeTable.LexicalTrie, recording.Read);
    }

    [Fact]
    public void A_provider_of_another_Unicode_version_is_refused_when_a_pattern_is_compiled()
    {
        var older = new RecordingTables(UnicodeTables.Instance, "16.0.0");
        Assert.Throws<ArgumentException>(() => new BroilerRegex("a", "u", older));
        Assert.Throws<ArgumentException>(() => RegexParser.Parse("a", RegexFlags.None, older, out _));
        Assert.Throws<ArgumentNullException>(() => new BroilerRegex("a", RegexFlags.None, null!));
    }

    [Fact]
    public void The_library_references_the_provider_alone_and_carries_no_table_or_data_version()
    {
        var references = Library.GetReferencedAssemblies().Select(static a => a.Name!).ToList();
        Assert.Contains("Broiler.Unicode.Algorithms", references);
        Assert.All(
            references.Where(static name => name != "Broiler.Unicode.Algorithms"),
            static name => Assert.True(name is "netstandard" || name.StartsWith("System", StringComparison.Ordinal), name));

        var types = Library.GetTypes();
        Assert.DoesNotContain(types, static t => t.Name is "CaseFolding" or "CaseFoldingData");
        Assert.DoesNotContain(
            types.SelectMany(static t => t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)),
            static m => m.Name.Contains("UnicodeVersion", StringComparison.Ordinal));
    }

    /// <summary>
    /// The invariant <c>RegexUnicode.BuildCanonicalCharacters</c> rests on: every code point the provider's simple
    /// case folding moves is Cased, at the version the provider pins, so a scan of Cased's members finds them all.
    /// </summary>
    [Fact]
    public void Every_code_point_simple_folding_moves_is_Cased()
    {
        var folding = new UnicodeCaseFolding(UnicodeTables.Instance);
        var properties = new UnicodeProperties(UnicodeTables.Instance);
        Assert.True(properties.TryResolve("Cased", out var cased));

        var moved = 0;
        for (var codePoint = 0; codePoint <= UnicodeAlgorithms.MaxCodePoint; codePoint++)
        {
            if (folding.SimpleFold(codePoint) == codePoint)
                continue;
            moved++;
            Assert.True(cased.Contains(codePoint), $"U+{codePoint:X4} folds and is not Cased");
        }

        Assert.True(moved > 1000);
    }

    /// <summary>
    /// §22.2.2.9 CharacterComplement under <c>v</c> with <c>i</c>: the universe is the code points that fold to
    /// themselves, so the class's set holds none that does not.
    /// </summary>
    [Fact]
    public void A_complement_under_vi_holds_only_code_points_that_fold_to_themselves()
    {
        var node = (CharClassNode)RegexParser.Parse("[^a]", RegexFlags.UnicodeSets | RegexFlags.IgnoreCase, UnicodeTables.Instance, out _);
        var set = node.Set.CodePoints;
        Assert.False(set.Contains('a'));
        Assert.False(set.Contains('A'));
        Assert.False(set.Contains(0x212A)); // KELVIN SIGN, which folds to k
        Assert.False(set.Contains(0x017F)); // LATIN SMALL LETTER LONG S, which folds to s
        Assert.True(set.Contains('k'));
        Assert.True(set.Contains('s'));
        Assert.True(set.Contains(0x10FFFF));
    }
}
