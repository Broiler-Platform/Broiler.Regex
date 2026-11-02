// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

// Arm J: the copy of Broiler.JS's CreateRegex, translator, GapScan, TryRouteToBroiler and RunMatch at 1f91b8f1, over
// Broiler.Regex 0.1.0-preview.4 from nuget.org and .NET Regex, called as Broiler.JS's RegExp constructor and ExecMatch
// call them (tests/ReferenceArms/README.md, "Arm J"). It is run three times: under the invariant culture, tr-TR and lt-LT.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Broiler.JavaScript.BuiltIns.RegExp;
using Broiler.JavaScript.Runtime;

namespace Broiler.Regex.ReferenceArms;

internal static class Program
{
    private static int Main(string[] args)
    {
        // NEGATIVE-CONTROL: a control copy of this arm sets Harness.ControlCase here, and nothing else changes.
        return Harness.Run(args, new ArmJ());
    }
}

internal sealed class ArmJ : IArm
{
    public string Name => "J";

    public IReadOnlyDictionary<string, string> Facts { get; } = new SortedDictionary<string, string>(StringComparer.Ordinal)
    {
        ["copy"] = "Broiler.JS 1f91b8f1: JSRegExp.cs, JSRegExp.Broiler.cs, JSRegExp.UnicodeSets.cs, JSRegExpPrototype.cs, RegExpUnicodeValidator.cs, RegExpGroupNames.cs, JSUnicodePropertyNames.cs, JSUnicode.cs (blobs in each file's header)",
        ["Broiler.Regex"] = InformationalVersion(typeof(Broiler.Regex.BroilerRegex)),
        ["Broiler.Unicode.Algorithms"] = InformationalVersion(typeof(Broiler.Unicode.Algorithms.IUnicodeTables)),
        ["Broiler.Unicode.Tables"] = InformationalVersion(typeof(Broiler.Unicode.Tables.UnicodeTables)),
        ["policy"] = "Broiler.JS's own: no meter; Broiler.Regex 0.1.0-preview.4's internal step limit; .NET Regex with no match timeout",
    };

    private static string InformationalVersion(Type type) =>
        (Attribute.GetCustomAttribute(type.Assembly, typeof(System.Reflection.AssemblyInformationalVersionAttribute))
            as System.Reflection.AssemblyInformationalVersionAttribute)?.InformationalVersion ?? "?";

    public (Answer Answer, object? Compiled) Compile(Case c)
    {
        try
        {
            return (Answer.Of("accept"), JSRegExp.Create(c.Pattern, c.Flags));
        }
        catch (JSException)
        {
            // Broiler.JS refuses every pattern it refuses with a SyntaxError, a nesting depth among none of them.
            return (Answer.Of("refuse:syntax"), null);
        }
    }

    public Answer Exec(object compiled, Case c, int start, bool sticky)
    {
        var regexp = (JSRegExp)compiled;
        // The .NET Regex has no match timeout, as Broiler.JS builds it: a match that does not end is stopped by the
        // harness's processor-time limit (Common/Limits.cs) and answered as the harness's stop.
        var match = regexp.ExecMatchAt(c.Subject, start);
        if (!match.Success)
            return Answer.Of("null");
        var groups = match.Groups;
        var starts = new int[groups.Length];
        var ends = new int[groups.Length];
        for (var g = 0; g < groups.Length; g++)
        {
            starts[g] = groups[g].Success ? groups[g].Index : -1;
            ends[g] = groups[g].Success ? groups[g].Index + groups[g].Length : -1;
        }

        // Group 0 is the match, which RunMatch reports in Index and Length.
        starts[0] = match.Index;
        ends[0] = match.Index + match.Length;
        IEnumerable<(string, IReadOnlyList<int>)> names = regexp.captureMap is { } map
            ? map.NamedGroups.Select(static n => (n.Name, (IReadOnlyList<int>)n.Indices))
            : [];
        return Answer.Of(Harness.Render(starts, ends, names));
    }
}
