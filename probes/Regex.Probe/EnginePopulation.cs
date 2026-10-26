using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Broiler.Regex.Probe;

/// <summary>
/// The new backend's cases in the probe's report (task 2.4a of SJL-11.2): its internal entry, run on the probe's thread
/// of <see cref="ProbePopulation.StackBytes"/>, so that the Native AOT run proves under its own stack what the JIT run
/// proves: the flat sequences of 100,000 terms (the adversarial corpus's F03 and F04) and the nesting cases at the maximum
/// (N01, N07, N13) complete through the new entry, and the new backend answers its ceilings, its meter, its anchored
/// argument, task 2.4b's constructs and task 2.4c's fast paths as the JIT does. Each case is written in the corpus's terms
/// (<see cref="EngineRun"/>), and the report gives each its answer and the units the match did.
/// </summary>
internal static class EnginePopulation
{
    /// <summary>The population's name, written into its first line.</summary>
    internal const string Name = "engine-1";

    private const string Ordinary = "meter=none steps=100000000 frames=4194304 trail=8388608 nesting=512 compile=none";

    /// <summary>The cases: an identifier, the corpus row it repeats or "-", the operation, the pattern, the flags, the subject and the policy.</summary>
    internal static (string Id, string Row, string Op, string Pattern, string Flags, string Subject, string Policy)[] Cases() =>
    [
        // The critic's crash cases (plan row 2.4a): a flat sequence of 100,000 terms and nesting at the maximum.
        ("E01", "F03", "match@0", "\"(?:a)\" * 100000", "-", "\"a\" * 100000", Ordinary),
        ("E02", "F04", "match@0", "\"(?:a|b)\" * 100000", "-", "\"a\" * 100000", Ordinary),
        ("E03", "N01", "match@0", "\"(\" * 511 + \"a\" + \")\" * 511", "-", "\"a\"", Ordinary),
        ("E04", "N07", "match@0", "\"(?:\" * 511 + \"a\" + \")\" * 511", "-", "\"a\"", Ordinary),
        ("E05", "N13", "match@0", "\"[\" * 511 + \"a\" + \"]\" * 511", "v", "\"a\"", Ordinary),
        ("E06", "N02", "compile", "\"(\" * 512 + \"a\" + \")\" * 512", "-", "-", Ordinary),
        ("E07", "N11", "compile", "\"(?:\" * 1000000 + \"a\" + \")\" * 1000000", "-", "-", Ordinary),
        ("E08", "-", "match@0", "\"(\" * 511 + \"a\" + \")+\" * 511", "-", "\"a\"", Ordinary),

        // Shapes whose match at the maximum the old matcher did not prove before task 2.4c (NestingMaximumTests'
        // UnprovenShapes within the subset): a capture around an alternation of sequences on each level, plain and repeated.
        ("E09", "-", "match@0", "\"(b|x\" * 511 + \"a\" + \"x)\" * 511", "-", "\"x\" * 511 + \"a\" + \"x\" * 511", Ordinary),
        ("E10", "-", "match@0", "\"(b|x\" * 511 + \"a\" + \"x)+\" * 511", "-", "\"x\" * 511 + \"a\" + \"x\" * 511", Ordinary),

        // The ceilings, each named, and the meter.
        ("E11", "K01", "match@0", "\"(a+)+b\"", "-", "\"b\" + \"a\" * 30", "meter=none steps=1000000 frames=4194304 trail=8388608 nesting=512 compile=none"),
        ("E12", "M01", "match@0", "\"(?:a|ab)*c\"", "-", "\"ab\" * 10000 + \"c\"", "meter=none steps=none frames=100 trail=8388608 nesting=512 compile=none"),
        ("E13", "M02", "match@0", "\"(?:(a)|b)*c\"", "-", "\"a\" * 10000 + \"c\"", "meter=none steps=none frames=4194304 trail=100 nesting=512 compile=none"),
        ("E14", "O12", "match@0", "\"[0-9]+x\"", "-", "\"x\" + \"1\" * 1000000", "meter=10000 steps=none frames=4194304 trail=8388608 nesting=512 compile=none"),
        ("E15", "O13", "match@0", "\"[0-9]+x\"", "-", "\"1\" * 100 + \"x\"", "meter=1000000 steps=none frames=4194304 trail=8388608 nesting=512 compile=none"),

        // Ordinary answers: captures, the empty check, lazy order, the anchored argument, code points.
        ("E16", "O15", "match@0", "\"(?:(a)|b)*c\"", "-", "\"a\" * 100 + \"c\"", Ordinary),
        ("E17", "-", "match@0", "\"(a?)*?b|(?:(x)|y)+\"", "-", "\"yxy\"", Ordinary),
        ("E18", "O09", "anchored@0", "\"b\"", "-", "\"ab\"", Ordinary),
        ("E19", "O10", "anchored@1", "\"b\"", "-", "\"ab\"", Ordinary),
        ("E20", "-", "match@1", "\"[^a]\\\\u{1F600}.\"", "u", "\"a\\ud83d\\ude00\\ud83d\\ude00b\"", Ordinary),
        ("E21", "-", "match@0", "\"(?<x>a{2,3}?)(?<y>[\\\\s\\\\S]{1,2})\"", "s", "\"aaaa\\n\"", Ordinary),

        // A look-behind's back-reference to a group it captures later in source order (issue #923's row 4, which was
        // the transitional refusal "^a" until task 2.4b), and a syntax error.
        ("E22", "-", "match@0", "\"(?<=\\\\1(\\\\w+))c\"", "-", "\"ababc\"", Ordinary),
        ("E23", "-", "compile", "\"a{2,1}\"", "-", "-", Ordinary),

        // The corpus's rows task 2.4b brings to the new backend: the anchors, the look-arounds, \B, case-insensitive
        // classes and the v-mode class of strings, each as the row declares it.
        ("E24", "K05", "match@0", "\"^(\\\\w+\\\\s?)*$\"", "-", "\"a\" * 30 + \"!\"", "meter=none steps=1000000 frames=4194304 trail=8388608 nesting=512 compile=none"),
        ("E25", "G03", "match@0", "\"^[a]*$\"", "u", "\"a\" * 10000001", "meter=none steps=50000 frames=4194304 trail=8388608 nesting=512 compile=none"),
        ("E26", "L01", "match@0", "\"^(?!(?:(a+)+b|a*c))\"", "-", "\"a\" * 30 + \"c\"", "meter=none steps=1000000 frames=4194304 trail=8388608 nesting=512 compile=none"),
        ("E27", "L02", "match@0", "\"^(?!(?:(a+)+b|a*c))\"", "u", "\"a\" * 30 + \"c\"", "meter=none steps=1000000 frames=4194304 trail=8388608 nesting=512 compile=none"),
        ("E28", "L03", "match@0", "\"^(?=(?:(a+)+b|a*c))\"", "-", "\"a\" * 30 + \"c\"", "meter=none steps=1000000 frames=4194304 trail=8388608 nesting=512 compile=none"),
        ("E29", "L04", "match@0", "\"$(?<=(?:b(a+)+|ca*))\"", "-", "\"c\" + \"a\" * 30", "meter=none steps=1000000 frames=4194304 trail=8388608 nesting=512 compile=none"),
        ("E30", "L05", "match@0", "\"$(?<!(?:b(a+)+|ca*))\"", "u", "\"c\" + \"a\" * 30", "meter=none steps=1000000 frames=4194304 trail=8388608 nesting=512 compile=none"),
        ("E31", "F01", "match@0", "\"\\\\B\" * 100000", "-", "\"\"", Ordinary),
        ("E32", "F02", "match@0", "\"\\\\B\" * 100000", "u", "\"\"", Ordinary),
        ("E33", "O02", "match@0", "\"^(?!(?:(a+)+b|a*c))\"", "u", "\"a\" * 12 + \"c\"", Ordinary),
        ("E34", "O03", "match@0", "\"$(?<=(?:b(a+)+|ca*))\"", "-", "\"c\" + \"a\" * 12", Ordinary),
        ("E35", "O16", "match@0", "\"[\" + \"\\\\p{L}\" * 50 + \"]\"", "iv", "\"A\"", Ordinary),
        ("E36", "O17", "match@0", "\"[\" + \"\\\\p{RGI_Emoji}\" * 50 + \"]\"", "v", "\"\\ud83d\\ude00\"", Ordinary),

        // NestingMaximumTests' look-behind shape at the maximum, matched right to left with no CLR recursion: a capture
        // around an alternation of sequences on each of 510 levels inside a look-behind.
        ("E37", "-", "match@0", "\"[\\\\s\\\\S]*(?<=\" + \"(b|x\" * 510 + \"a\" + \"x)\" * 510 + \")\"", "-", "\"x\" * 510 + \"a\" + \"x\" * 510", Ordinary),

        // The corpus's rows task 2.4c brings to the new backend: the greedy run of [a] over 10,000,001 characters, which
        // the run instruction matches with one frame; and its fast paths in both directions and under a fold: a lazy
        // run, a run and an atom run read right to left, classes of strings under i forward and in a look-behind, and
        // \p{RGI_Emoji}'s table.
        ("E38", "O05", "match@0", "\"[a]*\"", "-", "\"a\" * 10000001", "meter=none steps=2000000000 frames=4194304 trail=8388608 nesting=512 compile=none"),
        ("E39", "O06", "match@0", "\"^[a]*$\"", "u", "\"a\" * 10000001", "meter=none steps=2000000000 frames=4194304 trail=8388608 nesting=512 compile=none"),
        ("E40", "-", "match@0", "\"(?<=[\\\\d]{2,}?x|ab)\\\\w*?cd\"", "-", "\"12xyzcdabq\" * 40 + \"12xcd\"", Ordinary),
        ("E41", "-", "match@0", "\"(?:[\\\\q{abc|ab|}x]|(?<=[\\\\q{\\\\u{1F600}a|b}]))+y\"", "vi", "\"ABcXab\" * 50 + \"\\ud83d\\ude00Ay\"", Ordinary),
        ("E42", "-", "match@3", "\"\\\\p{RGI_Emoji}+\"", "v", "\"abc\\ud83d\\udc4d\\ud83c\\udffd\\ud83d\\ude00!\"", Ordinary),
    ];

    /// <summary>The engine section of the report: a header line, one line per case with its answer and units, and a digest line.</summary>
    internal static string Report()
    {
        var report = new StringBuilder();
        report.Append("Broiler.Regex.Probe engine population ").Append(Name).Append('\n');
        var answers = new StringBuilder();
        var cases = Cases();
        foreach (var (id, row, op, pattern, flags, subject, policy) in cases)
        {
            string answer;
            ulong units;
            try
            {
                answer = EngineRun.Answer(op, EngineRun.Expression(pattern), flags, EngineRun.Expression(subject), EngineRun.ParsePolicy(policy), out units);
            }
            catch (Exception exception)
            {
                answer = "threw " + (exception is ArgumentException ? "ArgumentException" : "another exception");
                units = 0;
            }

            var line = answer + " units " + units.ToString(CultureInfo.InvariantCulture);
            answers.Append(line).Append('\n');
            report.Append(id).Append(' ').Append(row).Append(' ').Append(op).Append(" /").Append(pattern).Append('/').Append(flags)
                .Append(' ').Append(subject).Append(' ').Append(policy).Append(": ").Append(line).Append('\n');
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(answers.ToString()));
        report.Append("cases ").Append(cases.Length.ToString(CultureInfo.InvariantCulture)).Append(", answers sha256 ")
            .Append(Convert.ToHexStringLower(digest)).Append('\n');
        return report.ToString();
    }
}
