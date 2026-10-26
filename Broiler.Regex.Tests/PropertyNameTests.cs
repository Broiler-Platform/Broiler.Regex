using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Broiler.Unicode.Algorithms;
using Broiler.Unicode.Tables;
using Xunit;

namespace Broiler.Regex.Tests;

/// <summary>
/// Requirement R5's property names (bundle sjl-11-001's check A-5), in the shape of Broiler.Unicode's population U2:
/// every name and value ECMA-262's tables and <c>PropertyValueAliases.txt</c> admit is accepted under <c>u</c> and
/// <c>v</c>, and every spelling they do not admit is refused (§22.2.2.9, UnicodeMatchProperty and
/// UnicodeMatchPropertyValue, which match exactly).
/// </summary>
/// <remarks>
/// The admitted names are read from the Unicode owner's own name tables, as <see cref="IUnicodeTables"/> documents
/// their layout, not from a list written here: the lone names (General_Category values and groups, and the binary
/// properties), the three property names of the <c>name=value</c> form with their aliases, the Script values, and
/// the properties of strings. The refused set is each admitted name with its letter case changed, an underscore
/// removed, a space or a hyphen inserted, or an <c>Is</c> prefix, and each value given to a property it does not
/// belong to; a mutation that is itself an admitted name is not in it.
/// </remarks>
public class PropertyNameTests
{
    private sealed record Names(
        IReadOnlyList<string> Lone,
        IReadOnlyList<string> CategoryValues,
        IReadOnlyList<string> BinaryProperties,
        IReadOnlyList<(string Name, int Kind)> PropertyNames,
        IReadOnlyList<string> Scripts,
        IReadOnlyList<string> StringProperties);

    private static readonly Lazy<Names> Admitted = new(ReadNames);

    /// <summary>A name table as <see cref="UnicodeTable"/> documents it: entries of a length byte, ASCII characters and a two-byte value, found through an index of two-byte offsets.</summary>
    private static List<(string Name, int Value)> NameTable(UnicodeTable data, UnicodeTable index)
    {
        var bytes = UnicodeTables.Instance.Table(data);
        var offsets = UnicodeTables.Instance.Table(index);
        var names = new List<(string, int)>();
        for (var at = 0; at < offsets.Length; at += 2)
        {
            var offset = offsets[at] | offsets[at + 1] << 8;
            var length = bytes[offset];
            names.Add((Encoding.ASCII.GetString(bytes.Slice(offset + 1, length)), bytes[offset + 1 + length] | bytes[offset + 2 + length] << 8));
        }

        return names;
    }

    private static Names ReadNames()
    {
        var layout = UnicodeTables.Instance.Table(UnicodeTable.PropertyLayout);
        var categorySets = layout[0] | layout[1] << 8 | layout[2] << 16;
        var lone = NameTable(UnicodeTable.PropertyLoneNames, UnicodeTable.PropertyLoneNameIndex);
        return new Names(
            [.. lone.Select(static n => n.Name)],
            [.. lone.Where(n => n.Value < categorySets).Select(static n => n.Name)],
            [.. lone.Where(n => n.Value >= categorySets).Select(static n => n.Name)],
            NameTable(UnicodeTable.PropertyNames, UnicodeTable.PropertyNameIndex),
            [.. NameTable(UnicodeTable.ScriptNames, UnicodeTable.ScriptNameIndex).Select(static n => n.Name)],
            [.. NameTable(UnicodeTable.StringPropertyNames, UnicodeTable.StringPropertyNameIndex).Select(static n => n.Name)]);
    }

    /// <summary>Every <c>name=value</c> pair the tables admit: General_Category's values, and Script's and Script_Extensions'.</summary>
    private static IEnumerable<(string Property, string Value)> AdmittedPairs(Names names)
    {
        foreach (var (property, kind) in names.PropertyNames)
        {
            foreach (var value in kind == 0 ? names.CategoryValues : names.Scripts)
                yield return (property, value);
        }
    }

    private static bool Compiles(string pattern, string flags)
    {
        try
        {
            TestRegex.Compile(pattern, flags);
            return true;
        }
        catch (RegexSyntaxException)
        {
            return false;
        }
    }

    /// <summary>The refused spellings of one admitted name: case changed, an underscore removed, a space or hyphen inserted, an Is prefix.</summary>
    private static IEnumerable<string> Mutations(string name)
    {
        var swapped = new string([.. name.Select(static c => char.IsAsciiLetterUpper(c) ? char.ToLowerInvariant(c) : char.IsAsciiLetterLower(c) ? char.ToUpperInvariant(c) : c)]);
        yield return swapped;
        yield return name.ToLowerInvariant();
        yield return name.ToUpperInvariant();
        var underscore = name.IndexOf('_', StringComparison.Ordinal);
        if (underscore >= 0)
            yield return name.Remove(underscore, 1);
        if (name.Length > 1)
        {
            yield return name[..1] + " " + name[1..];
            yield return name[..1] + "-" + name[1..];
        }

        yield return " " + name;
        yield return "Is" + name;
        yield return "Is_" + name;
    }

    [Fact]
    public void The_name_tables_are_read()
    {
        var names = Admitted.Value;
        Assert.Contains("Lu", names.CategoryValues);
        Assert.Contains("Uppercase_Letter", names.CategoryValues);
        Assert.Contains("ASCII", names.BinaryProperties);
        Assert.Contains("Any", names.BinaryProperties);
        Assert.Contains(("Script_Extensions", 2), names.PropertyNames);
        Assert.Contains("Katakana_Or_Hiragana", names.Scripts);
        Assert.Contains("Hrkt", names.Scripts);
        Assert.Contains("RGI_Emoji", names.StringProperties);
        Assert.Equal(6, names.PropertyNames.Count);
        Assert.Equal(7, names.StringProperties.Count);
    }

    [Theory]
    [InlineData("u")]
    [InlineData("v")]
    public void Every_lone_name_the_tables_admit_is_accepted(string flags)
    {
        var refused = Admitted.Value.Lone.Where(name => !Compiles(@"\p{" + name + "}", flags) || !Compiles(@"\P{" + name + "}", flags)).ToList();
        Assert.Empty(refused);
    }

    [Theory]
    [InlineData("u")]
    [InlineData("v")]
    public void Every_name_and_value_the_tables_admit_is_accepted(string flags)
    {
        var refused = AdmittedPairs(Admitted.Value)
            .Select(static pair => pair.Property + "=" + pair.Value)
            .Where(spec => !Compiles(@"\p{" + spec + "}", flags) || !Compiles(@"\P{" + spec + "}", flags))
            .ToList();
        Assert.Empty(refused);
    }

    [Fact]
    public void Every_property_of_strings_is_accepted_under_v_alone_and_never_negated()
    {
        foreach (var name in Admitted.Value.StringProperties)
        {
            Assert.True(Compiles(@"\p{" + name + "}", "v"), name);
            Assert.True(Compiles(@"[\p{" + name + "}--a]", "v"), name);
            Assert.False(Compiles(@"\p{" + name + "}", "u"), name);
            Assert.False(Compiles(@"\P{" + name + "}", "v"), name);
            Assert.False(Compiles(@"[^\p{" + name + "}]", "v"), name);
        }
    }

    [Theory]
    [InlineData("u")]
    [InlineData("v")]
    public void A_lone_name_spelled_otherwise_is_refused(string flags)
    {
        var names = Admitted.Value;
        var admitted = new HashSet<string>(names.Lone.Concat(names.StringProperties), StringComparer.Ordinal);
        var accepted = names.Lone.Concat(names.StringProperties)
            .SelectMany(Mutations)
            .Distinct(StringComparer.Ordinal)
            .Where(mutation => !admitted.Contains(mutation))
            .Where(mutation => Compiles(@"\p{" + mutation + "}", flags))
            .ToList();
        Assert.Empty(accepted);
    }

    [Theory]
    [InlineData("u")]
    [InlineData("v")]
    public void A_name_or_value_spelled_otherwise_is_refused(string flags)
    {
        var names = Admitted.Value;
        var admitted = new HashSet<string>(AdmittedPairs(names).Select(static p => p.Property + "=" + p.Value), StringComparer.Ordinal);
        var refusals = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (property, value) in AdmittedPairs(names))
        {
            foreach (var mutation in Mutations(property))
                refusals.Add(mutation + "=" + value);
            foreach (var mutation in Mutations(value))
                refusals.Add(property + "=" + mutation);
        }

        refusals.ExceptWith(admitted);
        var accepted = refusals.Where(spec => Compiles(@"\p{" + spec + "}", flags)).ToList();
        Assert.Empty(accepted);
    }

    [Theory]
    [InlineData("u")]
    [InlineData("v")]
    public void A_value_of_the_wrong_property_is_refused(string flags)
    {
        var names = Admitted.Value;
        var admittedPairs = new HashSet<string>(AdmittedPairs(names).Select(static p => p.Property + "=" + p.Value), StringComparer.Ordinal);
        var admittedLone = new HashSet<string>(names.Lone, StringComparer.Ordinal);
        var wrong = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (property, kind) in names.PropertyNames)
        {
            // A General_Category value under Script or Script_Extensions, a Script value under General_Category,
            // and a binary property or a property of strings as any property's value.
            foreach (var value in kind == 0 ? names.Scripts : names.CategoryValues)
                wrong.Add(property + "=" + value);
            foreach (var value in names.BinaryProperties.Concat(names.StringProperties))
                wrong.Add(property + "=" + value);
        }

        // A binary property is not the name of the name=value form, and a Script value is not a lone name.
        foreach (var binary in names.BinaryProperties)
        {
            wrong.Add(binary + "=Y");
            wrong.Add(binary + "=Yes");
            wrong.Add(binary + "=True");
        }

        var loneScripts = names.Scripts.Where(s => !admittedLone.Contains(s)).ToList();
        wrong.ExceptWith(admittedPairs);
        var accepted = wrong.Where(spec => Compiles(@"\p{" + spec + "}", flags))
            .Concat(loneScripts.Where(script => Compiles(@"\p{" + script + "}", flags)))
            .ToList();
        Assert.Empty(accepted);
        Assert.NotEmpty(loneScripts);
    }

    /// <summary>The survey's and the critic's property-name probe cases (the plan's section 1), each with the specification's answer.</summary>
    [Theory]
    [InlineData(@"\p{ASCII}", "u", true)]
    [InlineData(@"\p{ascii}", "u", false)]
    [InlineData(@"\p{Ascii}", "u", false)]
    [InlineData(@"\p{A S C I I}", "u", false)]
    [InlineData(@"\p{L}", "u", true)]
    [InlineData(@"\p{letter}", "u", false)]
    [InlineData(@"\p{Letter}", "u", true)]
    [InlineData(@"\p{Lu}", "u", true)]
    [InlineData(@"\p{lu}", "u", false)]
    [InlineData(@"\p{Uppercase_Letter}", "u", true)]
    [InlineData(@"\p{uppercaseletter}", "u", false)]
    [InlineData(@"\p{uppercase-letter}", "u", false)]
    [InlineData(@"\p{General_Category=Lu}", "u", true)]
    [InlineData(@"\p{general_category=Lu}", "u", false)]
    [InlineData(@"\p{gc=Lu}", "u", true)]
    [InlineData(@"\p{GC=Lu}", "u", false)]
    [InlineData(@"\p{gc=lu}", "u", false)]
    [InlineData(@"\p{Script=Greek}", "u", true)]
    [InlineData(@"\p{Script=greek}", "u", false)]
    [InlineData(@"\p{Script=Grek}", "u", true)]
    [InlineData(@"\p{sc=Grek}", "u", true)]
    [InlineData(@"\p{script=Greek}", "u", false)]
    [InlineData(@"\p{Script = Greek}", "u", false)]
    [InlineData(@"\p{Script_Extensions=Greek}", "u", true)]
    [InlineData(@"\p{scx=Grek}", "u", true)]
    [InlineData(@"\p{ScriptExtensions=Greek}", "u", false)]
    [InlineData(@"\p{Script=Katakana_Or_Hiragana}", "u", true)]
    [InlineData(@"\p{sc=Hrkt}", "u", true)]
    [InlineData(@"\p{scx=Hrkt}", "u", true)]
    [InlineData(@"\p{Script_Extensions=Katakana_Or_Hiragana}", "v", true)]
    [InlineData(@"\p{Hrkt}", "u", false)]
    [InlineData(@"\p{Any}", "u", true)]
    [InlineData(@"\p{Assigned}", "u", true)]
    [InlineData(@"\p{ASCII_Hex_Digit}", "u", true)]
    [InlineData(@"\p{AHex}", "u", true)]
    [InlineData(@"\p{Lowercase}", "u", true)]
    [InlineData(@"\p{Lower}", "u", true)]
    [InlineData(@"\p{Block=Basic_Latin}", "u", false)]
    [InlineData(@"\p{InBasicLatin}", "u", false)]
    [InlineData(@"\p{Is_Lowercase}", "u", false)]
    [InlineData(@"\p{IsGreek}", "u", false)]
    [InlineData(@"\p{General_Category}", "u", false)]
    [InlineData(@"\p{Script}", "u", false)]
    [InlineData(@"\p{Lu=Lu}", "u", false)]
    [InlineData(@"\p{Alphabetic=Yes}", "u", false)]
    [InlineData(@"\p{}", "u", false)]
    [InlineData(@"\p{RGI_Emoji}", "u", false)]
    [InlineData(@"\p{RGI_Emoji}", "v", true)]
    [InlineData(@"\p{rgi_emoji}", "v", false)]
    [InlineData(@"\p{rgiemoji}", "v", false)]
    [InlineData(@"\p{Basic_Emoji}", "v", true)]
    [InlineData(@"\p{Emoji_Keycap_Sequence}", "v", true)]
    public void The_probe_cases_get_the_specification_answer(string pattern, string flags, bool accepted)
        => Assert.Equal(accepted, Compiles(pattern, flags));

    [Theory]
    [InlineData(@"^\p{Script=Katakana_Or_Hiragana}$", "u")]
    [InlineData(@"^\p{sc=Hrkt}$", "u")]
    [InlineData(@"^\p{scx=Hrkt}$", "v")]
    public void Katakana_Or_Hiragana_is_a_value_no_code_point_has(string pattern, string flags)
    {
        var regex = TestRegex.Compile(pattern, flags);
        foreach (var subject in (string[])["あ", "ア", "ｱ", "ー", "a"])
            Assert.False(regex.IsMatch(subject), subject);
    }
}
