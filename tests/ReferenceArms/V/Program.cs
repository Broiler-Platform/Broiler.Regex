// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

// Arm V: the copy of Broiler.VM's matcher at aaa39fb (JsRegExpMatcher.cs), called as Broiler.VM's realm calls it
// (JsRealm.RegExp.cs, the copied members), with the charge delegate kept: every unit it charges is recorded, and an
// allowance stands for the fuel Broiler.VM's engine would grant (tests/ReferenceArms/README.md, "Arm V").

using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.VM.Profile.JavaScript.Format;

namespace Broiler.Regex.ReferenceArms;

internal static class Program
{
    private static int Main(string[] args)
    {
        // NEGATIVE-CONTROL: a control copy of this arm sets Harness.ControlCase here, and nothing else changes.
        return Harness.Run(args, new ArmV());
    }
}

internal sealed class ArmV : IArm
{
    /// <summary>The allowance of one exec under the population's default policy: S's step ceiling, in the same units.</summary>
    internal const ulong DefaultAllowance = Limits.DefaultSteps;

    public string Name => "V";

    public IReadOnlyDictionary<string, string> Facts { get; } = new SortedDictionary<string, string>(StringComparer.Ordinal)
    {
        ["copy"] = "Broiler.VM aaa39fb: JsRegExpMatcher.cs f125341255294259e3acecdad74d2310a8260d51, JsUnicodeData.cs b4646317dbc07ea7365a91b03ab1cfb3bc20472d, JsRealm.RegExp.cs 6c6e487ba1d7130e945c0bf9de7a2ec9609a5a0e",
        ["Broiler.Unicode.Algorithms"] = InformationalVersion(typeof(Broiler.Unicode.Algorithms.IUnicodeTables)),
        ["Broiler.Unicode.Tables"] = InformationalVersion(typeof(Broiler.Unicode.Tables.UnicodeTables)),
        ["default-policy"] = "allowance 10000000 units per exec; the matcher's own ceilings (frames 1048576, trail 2097152, nesting 512)",
        ["ample-policy"] = "no allowance; the matcher's own ceilings",
    };

    private static string InformationalVersion(Type type) =>
        (Attribute.GetCustomAttribute(type.Assembly, typeof(System.Reflection.AssemblyInformationalVersionAttribute))
            as System.Reflection.AssemblyInformationalVersionAttribute)?.InformationalVersion ?? "?";

    public (Answer Answer, object? Compiled) Compile(Case c)
    {
        var engine = new RealmStandIn();
        try
        {
            JsRealmRegExp.RegExpNormalizeFlags(engine, c.Flags);
            var matcher = JsRealmRegExp.RegExpCompile(engine, c.Pattern, c.Flags);
            return (new Answer("accept", "0"), matcher);
        }
        catch (RealmSyntaxError refusal)
        {
            // The matcher refuses a pattern nested deeper than its limit with a message of its own (:1818, :2617).
            var kind = refusal.Message == "Regular expression is nested too deeply" ? "nesting" : "syntax";
            return (new Answer("refuse:" + kind, "0"), null);
        }
    }

    public Answer Exec(object compiled, Case c, int start, bool sticky)
    {
        var matcher = (JsRegExpMatcher)compiled;
        var engine = new RealmStandIn { Allowance = c.AmpleNesting is null ? DefaultAllowance : ulong.MaxValue };
        JsRegExpMatch? match;
        try
        {
            match = JsRealmRegExp.RegExpMatchOne(engine, matcher, c.Subject, start, sticky);
        }
        catch (AllowanceExhausted)
        {
            return new Answer("limit:steps", Charged(engine));
        }
        catch (JsRegExpOverflowError)
        {
            // Both ceilings throw the same message (:4343, :4381), so the arm cannot say which was reached.
            return new Answer("limit:ceiling", Charged(engine));
        }

        if (match is null)
            return new Answer("null", Charged(engine));
        var groups = match.CaptureCount + 1;
        var starts = new int[groups];
        var ends = new int[groups];
        var names = new List<(string, IReadOnlyList<int>)>();
        for (var g = 0; g < groups; g++)
        {
            var participated = g == 0 || match.Participated(g);
            starts[g] = participated ? match.StartOf(g) : -1;
            ends[g] = participated ? match.EndOf(g) : -1;
            if (g > 0 && matcher.NameOf(g) is { } name)
                names.Add((name, new[] { g }));
        }

        return new Answer(Harness.Render(starts, ends, names), Charged(engine));
    }

    private static string Charged(RealmStandIn engine) => engine.Charged.ToString(CultureInfo.InvariantCulture);
}
