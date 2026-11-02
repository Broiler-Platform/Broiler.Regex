// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

// Arm S: Broiler.Regex at the commit under test, through the public API the engines call (tests/ReferenceArms/README.md,
// "Arm S"). It compiles with a policy and the provider and matches through the offset entry, with a meter that records
// every unit charged; what an engine's adapter does around the entry (the start inside a surrogate pair, row M4 and
// decisions D23 and D41 of Broiler.VM's JSD-0081) is written here as the library's string-bearing Match does it.

using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.Regex;
using Broiler.Regex.Text;
using Broiler.Unicode.Tables;

namespace Broiler.Regex.ReferenceArms;

internal static class Program
{
    private static int Main(string[] args) => Harness.Run(args, new ArmS());
}

internal sealed class ArmS : IArm
{
    /// <summary>The population's default policy, step ceiling: arm V's allowance, in the same units (JSD-0081 D8).</summary>
    internal const ulong DefaultSteps = Limits.DefaultSteps;

    public string Name => "S";

    public IReadOnlyDictionary<string, string> Facts { get; } = new SortedDictionary<string, string>(StringComparer.Ordinal)
    {
        ["Broiler.Regex"] = typeof(BroilerRegex).Assembly.GetName().Version + " " + InformationalVersion(typeof(BroilerRegex)),
        ["Broiler.Unicode.Tables"] = InformationalVersion(typeof(UnicodeTables)),
        ["Broiler.Unicode.Algorithms"] = InformationalVersion(typeof(Broiler.Unicode.Algorithms.IUnicodeTables)),
        ["default-policy"] = "meter recording, steps 10000000, frames 1048576, trail 2097152, nesting 512, compile none",
        ["ample-policy"] = "meter recording, steps none, frames 16777216, trail 33554432, nesting the case's, compile none",
    };

    private static string InformationalVersion(Type type) =>
        (Attribute.GetCustomAttribute(type.Assembly, typeof(System.Reflection.AssemblyInformationalVersionAttribute))
            as System.Reflection.AssemblyInformationalVersionAttribute)?.InformationalVersion ?? "?";

    public (Answer Answer, object? Compiled) Compile(Case c)
    {
        if (RegexFlagsText.Parse(c.Flags, out var flags, out _) != RegexStatus.Success)
            return (Answer.Of("refuse:syntax"), null);
        ulong units = 0;
        RegexWorkCharge meter = u => units += u;
        var nesting = c.AmpleNesting ?? BroilerRegex.MaximumNestingLimit;
        var status = BroilerRegex.Compile(c.Pattern, flags, UnicodeTables.Instance, nesting, null, meter,
            out var regex, out var limit, out var diagnostic);
        var charge = units.ToString(CultureInfo.InvariantCulture);
        return status switch
        {
            RegexStatus.Success => (new Answer("accept", charge), new Compiled(regex!, flags)),
            RegexStatus.SyntaxError when diagnostic.Code == RegexDiagnosticCode.NestingLimit => (new Answer("refuse:nesting", charge), null),
            RegexStatus.SyntaxError => (new Answer("refuse:syntax", charge), null),
            RegexStatus.LimitExceeded => (new Answer("refuse:" + Limit(limit), charge), null),
            _ => (new Answer("status:" + status, charge), null),
        };
    }

    public Answer Exec(object compiled, Case c, int start, bool sticky)
    {
        var (regex, flags) = (Compiled)compiled;
        var ceilings = c.AmpleNesting is null
            ? new RegexMatchCeilings(DefaultSteps, RegexMatchCeilings.EngineFrames, RegexMatchCeilings.EngineTrail)
            : new RegexMatchCeilings(null, RegexMatchCeilings.FrameCeilingMaximum, RegexMatchCeilings.TrailCeilingMaximum);
        var input = c.Subject;

        // RegExpBuiltinExec runs the matcher from the character that holds element lastIndex (row M4): under u or v a
        // start between the halves of a pair runs from the pair, as BroilerRegex.Match(string, int) does.
        var from = start;
        if (RegexFlagsParser.IsUnicodeMode(flags) && from > 0 && from < input.Length && char.IsLowSurrogate(input[from]) && char.IsHighSurrogate(input[from - 1]))
            from--;
        ulong units = 0;
        RegexWorkCharge meter = u => units += u;
        var slots = new int[regex.SlotCount];
        var status = regex.Match(input, from, sticky, slots, meter, ceilings, out var limit);
        var charge = units.ToString(CultureInfo.InvariantCulture);
        if (status == RegexStatus.NoMatch)
            return new Answer("null", charge);
        if (status == RegexStatus.LimitExceeded)
            return new Answer("limit:" + Limit(limit), charge);
        if (status != RegexStatus.Match)
            return new Answer("status:" + status, charge);

        // A non-empty match found from the pair reports the caller's start as its index (decision D23); the empty match
        // there, decision D41's implementation-defined case, is reported where it was found, as Match(string, int) does.
        var groups = slots.Length / 2;
        var starts = new int[groups];
        var ends = new int[groups];
        for (var g = 0; g < groups; g++)
        {
            starts[g] = slots[2 * g];
            ends[g] = slots[(2 * g) + 1];
        }

        if (starts[0] == from && ends[0] >= start)
            starts[0] = start;
        return new Answer(Harness.Render(starts, ends, Names(regex)), charge);
    }

    private static IEnumerable<(string, IReadOnlyList<int>)> Names(BroilerRegex regex)
    {
        var names = regex.GroupNames;
        for (var k = 0; k < names.Count; k++)
        {
            var name = names.NameAt(k);
            yield return (name, names.GroupsNamed(name).ToArray());
        }
    }

    private static string Limit(RegexLimit limit) => limit switch
    {
        RegexLimit.Steps => "steps",
        RegexLimit.Frames => "frames",
        RegexLimit.Trail => "trail",
        RegexLimit.Nesting => "nesting",
        RegexLimit.Compile => "compile",
        _ => limit.ToString(),
    };

    private sealed record Compiled(BroilerRegex Regex, RegexFlags Flags);
}
