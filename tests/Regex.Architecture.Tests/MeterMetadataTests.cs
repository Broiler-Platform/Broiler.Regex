using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;

namespace Broiler.Regex.Architecture.Tests;

/// <summary>
/// Bundle <c>sjl-11-001</c>'s check A-1, its metadata half (requirement R1; decision D20 of Broiler.VM's JSD-0081): the
/// old backend's step budget is gone from the library's built assembly. No type is named <c>Budget</c>,
/// <c>StepLimit</c> or <c>RegexOverflowException</c>, and no method, field or property is named <c>Budget</c>,
/// <c>StepLimit</c> or <c>Reset</c>, the per-start reset that gave each start position a fresh allowance
/// (<c>Budget.Reset</c>), in the assembly's definitions or in its references to other assemblies. A member's name is its
/// metadata name, so the <c>System.Collections.IEnumerator.Reset</c> the compiler writes for an iterator is not the reset.
/// Task 2.4c deleted them with <c>Matching/</c>; the charging half of A-1 is the library tests' (<c>ChargingTests</c>).
/// </summary>
public class MeterMetadataTests
{
    private static readonly string[] DeletedTypes = ["Budget", "StepLimit", "RegexOverflowException"];

    private static readonly string[] DeletedMembers = ["Budget", "StepLimit", "Reset"];

    [Fact]
    public void A1_Budget_StepLimit_the_per_start_reset_and_RegexOverflowException_are_absent_from_the_metadata()
    {
        var read = Names(File.ReadAllBytes(SharedTree.AssemblyPath("Broiler.Regex")));
        Assert.Empty(Deleted(read));
        var names = read.Select(Show).ToHashSet(StringComparer.Ordinal);

        // Not vacuous: the reading finds the library's types, nested ones, methods, fields and properties, and its
        // references to other assemblies' types.
        Assert.Contains("type Broiler.Regex.Engine.RegexMachine", names);
        Assert.Contains("method Broiler.Regex.Engine.RegexMachine.Afford", names);
        Assert.Contains("field Broiler.Regex.Engine.RegexMachine._spent", names);
        Assert.Contains("property Broiler.Regex.RegexMatch.Limit", names);
        Assert.Contains("reference System.Buffers.ArrayPool`1", names);

        // And it refuses each name, planted in a copy of the library's sources as the old backend had them.
        var planted = Names(MatcherRecursionTests.Compile([.. MatcherRecursionTests.LibrarySources(), ("Broiler.Regex/Matching/Planted.cs", Planted)]));
        Assert.Equal(
            [
                "field Broiler.Regex.Matching.PlantedMatcher.StepLimit",
                "method Broiler.Regex.Matching.Budget.Reset",
                "property Broiler.Regex.Matching.MatchState.Budget",
                "type Broiler.Regex.Matching.Budget",
                "type Broiler.Regex.RegexOverflowException",
            ],
            Deleted(planted).Order(StringComparer.Ordinal));
    }

    /// <summary>The names <paramref name="names"/> holds that the deletion removed.</summary>
    private static IEnumerable<string> Deleted(IEnumerable<(string Kind, string Owner, string Name)> names) =>
        names.Where(static name => name.Kind is "type" or "reference"
                ? DeletedTypes.Contains(name.Name, StringComparer.Ordinal)
                : DeletedMembers.Contains(name.Name, StringComparer.Ordinal))
            .Select(Show);

    private static string Show((string Kind, string Owner, string Name) name) =>
        name.Kind + " " + (name.Owner.Length == 0 ? "" : name.Owner + ".") + name.Name;

    /// <summary>
    /// Every type, method, field and property an assembly defines, by kind and qualified name, and every type it
    /// references in another assembly.
    /// </summary>
    private static List<(string Kind, string Owner, string Name)> Names(byte[] image)
    {
        using var pe = new PEReader(new MemoryStream(image));
        var metadata = pe.GetMetadataReader();
        var names = new List<(string Kind, string Owner, string Name)>();
        foreach (var handle in metadata.TypeDefinitions)
        {
            var type = metadata.GetTypeDefinition(handle);
            var qualified = SignatureNames.Qualified(metadata, handle);
            var outer = qualified[..Math.Max(0, Math.Max(qualified.LastIndexOf('.'), qualified.LastIndexOf('+')))];
            names.Add(("type", outer, metadata.GetString(type.Name)));
            foreach (var method in type.GetMethods())
                names.Add(("method", qualified, metadata.GetString(metadata.GetMethodDefinition(method).Name)));
            foreach (var field in type.GetFields())
                names.Add(("field", qualified, metadata.GetString(metadata.GetFieldDefinition(field).Name)));
            foreach (var property in type.GetProperties())
                names.Add(("property", qualified, metadata.GetString(metadata.GetPropertyDefinition(property).Name)));
        }

        foreach (var handle in metadata.TypeReferences)
        {
            var reference = metadata.GetTypeReference(handle);
            var qualified = SignatureNames.Qualified(metadata, handle);
            names.Add(("reference", qualified[..Math.Max(0, qualified.Length - metadata.GetString(reference.Name).Length - 1)], metadata.GetString(reference.Name)));
        }

        return names;
    }

    /// <summary>The old backend's names, as <c>Matching/</c> and the exception's file declared them, in miniature.</summary>
    private const string Planted = """
        namespace Broiler.Regex.Matching
        {
            internal sealed class Budget
            {
                internal void Reset()
                {
                }
            }

            internal readonly struct MatchState(Budget budget)
            {
                internal Budget Budget { get; } = budget;
            }

            internal static class PlantedMatcher
            {
                private const long StepLimit = 10_000_000;
            }
        }

        namespace Broiler.Regex
        {
            public sealed class RegexOverflowException : System.Exception
            {
            }
        }
        """;
}
