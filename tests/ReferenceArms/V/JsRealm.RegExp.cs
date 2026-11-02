// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0

// Arm V of bundle sjl-11-001's reference-arm comparison (docs/evidence/sjl-11-001/reference-arms.md): a test-only copy.
// Source:  Broiler.VM, src/Broiler.VM.Profile.JavaScript/JsRealm.RegExp.cs at aaa39fb, git blob
//          6c6e487ba1d7130e945c0bf9de7a2ec9609a5a0e, Apache-2.0, Broiler Platform contributors.
// Members: how the realm calls the matcher, and nothing of the protocols: RegExpHasFlag (:1603-1614),
//          RegExpNormalizeFlags (:1738-1786), RegExpCompile (:1791-1809), the matcher's
//          start in RegExpMatchOne (:1900-1964: the charge of 16 at :1904, the flags' reading of lastIndex at
//          :1910-1916, the step back out of a surrogate pair at :1926-1932, the call at :1934-1936) and RegExpRun
//          (:1969-1980).
// Changes: each marked ARM CHANGE. The engine is RealmStandIn, which records every charge in place of JsEngine.Charge
//          and turns a refusal into a recorded answer in place of a SyntaxError object; lastIndex's ToLength and the
//          flags' reading are the harness's (the rule leaves lastIndex handling in the engines, uncompared), so the
//          method takes the start the harness read; the writes of lastIndex (:1938-1962) are not copied.

namespace Broiler.VM.Profile.JavaScript.Format;

/// <summary>The engine the copied realm members call: the charge delegate's owner and the error constructor.</summary>
internal sealed class RealmStandIn
{
    /// <summary>The units charged so far, as JsEngine.Charge would take them from the allowance.</summary>
    internal ulong Charged { get; private set; }

    /// <summary>The allowance: a charge past it ends the run as JsEngine.Charge's abort does.</summary>
    internal ulong Allowance { get; init; } = ulong.MaxValue;

    /// <summary>JsEngine.Charge: takes the units and aborts when the allowance is spent.</summary>
    internal void Charge(ulong units)
    {
        Charged += units;
        if (Charged > Allowance)
        {
            throw new AllowanceExhausted();
        }
    }
}

/// <summary>JsAbort(JsAbortKind.Exhausted), as the stand-in raises it; the arm answers it as a limit.</summary>
internal sealed class AllowanceExhausted : System.Exception
{
}

/// <summary>The matcher's refusal, as RegExpCompile turns it into a SyntaxError; the arm answers it by its kind.</summary>
internal sealed class RealmSyntaxError(string message) : System.Exception(message)
{
}

/// <summary>The copied members.</summary>
internal static class JsRealmRegExp
{
    /// <summary>Compiles a pattern with this profile's own matcher.</summary>
    internal static JsRegExpMatcher RegExpCompile(RealmStandIn engine, string source, string flags)
    {
        _ = engine; // ARM CHANGE: the engine is only the error's constructor here
        try
        {
            return JsRegExpMatcher.Compile(
                source,
                RegExpHasFlag(flags, 'i'),
                RegExpHasFlag(flags, 'm'),
                RegExpHasFlag(flags, 's'),
                RegExpHasFlag(flags, 'u'),
                RegExpHasFlag(flags, 'v'));
        }
        catch (JsRegExpSyntaxError failure)
        {
            // ARM CHANGE: was `throw engine.Error("SyntaxError", "Invalid regular expression: /" + source + "/: " + failure.Message);`
            throw new RealmSyntaxError(failure.Message);
        }
    }

    /// <summary>RegExpHasFlag (:1603-1614).</summary>
    private static bool RegExpHasFlag(string flags, char flag)
    {
        foreach (var candidate in flags)
        {
            if (candidate == flag)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>RegExpNormalizeFlags (:1738-1786): the flags' refusals and their order.</summary>
    internal static string RegExpNormalizeFlags(RealmStandIn engine, string flags)
    {
        var seen = 0;

        foreach (var flag in flags)
        {
            var bit = flag switch
            {
                'd' => 1,
                'g' => 2,
                'i' => 4,
                'm' => 8,
                's' => 16,
                'u' => 32,
                'v' => 64,
                'y' => 128,
                _ => 0,
            };

            if (bit == 0 || (seen & bit) != 0)
            {
                throw new RealmSyntaxError("Invalid regular expression flags: " + flags); // ARM CHANGE: was `throw engine.Error("SyntaxError", "Invalid regular expression flags: " + flags);`
            }

            seen |= bit;
        }

        // `u` AND `v` ARE TWO UNICODE MODES AND A PATTERN HAS ONE: together they are a SyntaxError
        // (phase F2, which admitted `v`).
        if ((seen & 96) == 96)
        {
            throw new RealmSyntaxError("Invalid regular expression flags: " + flags); // ARM CHANGE: was `throw engine.Error("SyntaxError", "Invalid regular expression flags: " + flags);`
        }

        // THE ORDER IS THE SPECIFICATION'S AND NOT THE ORDER THEY WERE WRITTEN IN, which is what
        // makes `new RegExp("x", "yg").flags` answer "gy" and what `toString` prints.
        var builder = new System.Text.StringBuilder(8);
        var order = "dgimsuvy";

        for (var at = 0; at < order.Length; at++)
        {
            if ((seen & (1 << at)) != 0)
            {
                builder.Append(order[at]);
            }
        }

        return builder.ToString();
    }

    /// <summary>The matcher's call in RegExpMatchOne, from the start the harness read from lastIndex.</summary>
    internal static JsRegExpMatch? RegExpMatchOne(RealmStandIn engine, JsRegExpMatcher matcher, string input, int lastIndex, bool sticky)
    {
        engine.Charge(16);

        // ARM CHANGE: `var start = (double)IntegerConversions.ToLength(engine.ToNumber(target.LastIndex));` and the
        // flags' test (`if (!tracks) start = 0;`) are the harness's, which hands the start in.
        double start = lastIndex;

        if (matcher.Unicode &&
            start > 0 && start < input.Length &&
            char.IsLowSurrogate(input[(int)start]) &&
            char.IsHighSurrogate(input[(int)start - 1]))
        {
            start--;
        }

        var match = start > input.Length
            ? null
            : RegExpRun(engine, matcher, input, (int)start, sticky);

        // ARM CHANGE: the lastIndex writes (:1938-1962) are not copied.
        return match;
    }

    private static JsRegExpMatch? RegExpRun(
        RealmStandIn engine, JsRegExpMatcher matcher, string input, int start, bool anchored)
    {
        try
        {
            // ARM CHANGE: was `engine.Realm.RegExpMeter(engine)`, which is `owner.Charge` (:1985).
            return matcher.Match(input, start, anchored, engine.Charge);
        }
        catch (JsRegExpOverflowError)
        {
            // ARM CHANGE: was `throw new JsAbort(JsAbortKind.Exhausted, failure.Message);`
            throw;
        }
    }
}
