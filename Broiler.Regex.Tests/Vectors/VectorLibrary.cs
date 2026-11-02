using System;
using System.Collections.Generic;
using System.Text;
using Broiler.Regex.Text;
using Broiler.Unicode.Algorithms;

namespace Broiler.Regex.VectorsRunner;

/// <summary>
/// The op mapping of bundle <c>sjl-11-001</c>'s part D: each op of Broiler.JS's <c>conformance/vectors/regexp/</c> that the
/// library implements, called through the package's public surface on the vector's <c>in</c>, its answer rendered in the
/// corpus's outcome notation (SCHEMA.md section 4); and each op it does not implement, the object model's, by name with
/// the reason.
/// </summary>
/// <remarks>
/// <para>
/// <b>The policy an engine passes</b> (decisions D7 and D10 of Broiler.VM's JSD-0081): every pattern is compiled through
/// <see cref="BroilerRegex.Compile"/> under the nesting limit both engines pass, <see cref="BroilerRegex.MaximumNestingLimit"/>
/// (512), with no compile budget and no meter, and its flags read by <see cref="RegexFlagsText.Parse"/>, as
/// RegExpInitialize reads a flags string; every match runs under the engines' ceilings, but a budget vector's, which runs
/// the offset entry under the vector's step budget.
/// </para>
/// <para>
/// <b>The library's own answers.</b> A refusal the specification makes is <c>error: SyntaxError</c>, as RegExpInitialize
/// throws it; a limit, which the specification does not have, is a status: <c>status: exhausted</c> for
/// <see cref="RegexStatus.LimitExceeded"/> naming <see cref="RegexLimit.Steps"/> (the corpus's word, which the budget
/// vectors give), and <c>status: limit-nesting</c>, <c>-frames</c>, <c>-trail</c> or <c>-compile</c> for the others.
/// An engine renders the nesting status as a SyntaxError (decision D7), the library does not: it is reported as it is.
/// </para>
/// </remarks>
internal sealed class VectorLibrary
{
    /// <summary>The rendering of an op the library does not implement, as Broiler.ECMAScript's library runner writes it.</summary>
    internal const string NotImplemented = "status: not-implemented";

    /// <summary>
    /// The ops of the object model, which the library does not implement, each with why (the rule's "every op the library
    /// does not implement (the object model's) is listed by name"). An op neither mapped nor listed here renders a fault.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> NotImplementedOps = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["RegExp.prototype [ %Symbol.split% ]"] =
            "22.2.6.14's loop: a species constructor, a splitter with y added and its lastIndex stepped by AdvanceStringIndex; no member of the library splits",
        ["String.prototype.search"] =
            "22.1.3.23 with RegExp.prototype[%Symbol.search%] deleted or replaced: property lookups of the object model",
        ["get RegExp.prototype.flags, ordinary object"] =
            "22.2.6.4 over an ordinary object: ToBoolean of each property, with no regular expression",
        ["get RegExp.prototype.flags, read order"] =
            "22.2.6.4's order of property reads, observed by getters",
        ["RegExp.prototype accessors"] =
            "the accessors and toString on RegExp.prototype itself, which has no [[OriginalFlags]]",
        ["RegularExpressionLiteral, eval code"] =
            "12.9.5's lexical grammar of a literal in eval code, a tokenizer's: the library reads a pattern, not a source text",
    };

    private const int Nesting = BroilerRegex.MaximumNestingLimit;

    private readonly IUnicodeTables _tables;
    private readonly RegexEscaper _escaper;

    internal VectorLibrary(IUnicodeTables tables)
    {
        _tables = tables;
        _escaper = new RegexEscaper(tables);
    }

    /// <summary>
    /// The op each mapped op is, written for the report: the library's members it calls, in a phrase.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> Mapping = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["RegExpInitialize"] = "RegexFlagsText.Parse, then BroilerRegex.Compile at nesting 512",
        ["RegExpInitialize, nested pattern"] = "the same, over the opener count times, the middle and the closer count times",
        ["IsValidRegularExpressionLiteral"] = "BroilerRegex.Validate over the flags text, at nesting 512",
        ["RegExpBuiltinExec"] = "Compile, then Match(string, int) from lastIndex (0 without g and y), lastIndex written under g or y",
        ["RegExpBuiltinExec, repeated inputs"] = "the same, the lengths serialised",
        ["RegExpBuiltinExec, step budget"] = "Compile, then the offset entry with a step ceiling of the budget and a counting meter",
        ["RegExp.prototype.test"] = "Compile, then Match(string, 0).Success",
        ["RegExp.prototype [ %Symbol.match% ]"] = "Compile, then Match(string, 0), and NextMatch until it fails under g",
        ["RegExp.prototype [ %Symbol.search% ]"] = "Compile, then Match(string, 0).Index, or -1",
        ["RegExp.escape"] = "RegexEscaper.EscapeString",
        ["RegExp.escape, round trip"] = "RegexEscaper.EscapeString, then Compile and Match(string, 0).Success on the string",
        ["get RegExp.prototype.flags"] = "Compile of \"a\", then RegexFlagsText.Format of its Flags",
        ["EscapeRegExpPattern, one P and F"] = "Compile, then RegexEscaper.EscapePattern of the literal's body and of the compiled Pattern, compared",
        ["EscapeRegExpPattern, a literal of S"] =
            "Compile, then S = RegexEscaper.EscapePattern(Pattern): no line terminator, a RegularExpressionBody, valid, and matching the subject as the pattern does",
    };

    /// <summary>Whether <paramref name="op"/> is one this runner calls the library for.</summary>
    internal static bool Implements(string op) => Mapping.ContainsKey(op);

    /// <summary>Calls the vector's op, and renders the answer; <see cref="NotImplemented"/> for an op of the object model.</summary>
    internal string Call(VectorRecord vector)
    {
        var op = vector.Op;
        if (NotImplementedOps.ContainsKey(op))
            return NotImplemented;
        var a = VectorNotation.Inputs(vector.In);
        return op switch
        {
            "RegExpInitialize" => Construct(a[0].Str, a[1].Str),
            "RegExpInitialize, nested pattern" => Construct(Repeat(a[0].Str, a[1].Int) + a[2].Str + Repeat(a[3].Str, a[1].Int), a[4].Str),
            "IsValidRegularExpressionLiteral" => Literal(a[0].Str, a[1].Str),
            "RegExpBuiltinExec" => Exec(a[0].Str, a[1].Str, a[2].Str, a[3].Int, lengths: false),
            "RegExpBuiltinExec, repeated inputs" =>
                Exec(Repeat(a[0].Str, a[1].Int) + a[2].Str, a[3].Str, Repeat(a[4].Str, a[5].Int) + a[6].Str, a[7].Int, lengths: true),
            "RegExpBuiltinExec, step budget" =>
                Budget(a[0].Str, a[1].Str, Repeat(a[2].Str, a[3].Int) + a[4].Str, a[5].Int, checked((ulong)a[6].Integer)),
            "RegExp.prototype.test" => Test(a[0].Str, a[1].Str, a[2].Str),
            "RegExp.prototype [ %Symbol.match% ]" => SymbolMatch(a[0].Str, a[1].Str, a[2].Str),
            "RegExp.prototype [ %Symbol.search% ]" => Search(a[0].Str, a[1].Str, a[2].Str),
            "RegExp.escape" => Escape(a[0].Str),
            "RegExp.escape, round trip" => RoundTrip(a[0].Str, a[1].Str),
            "get RegExp.prototype.flags" => FlagsGetter(a[0].Str),
            "EscapeRegExpPattern, one P and F" => OnePattern(a[0].Str, a[1].Str),
            "EscapeRegExpPattern, a literal of S" => LiteralOfSource(a[0].Str, a[1].Str, a[2].Str),
            _ => "fault: an op the runner neither maps nor lists as not implemented: " + op,
        };
    }

    /// <summary>
    /// RegExpInitialize's reading of the flags and the pattern: the compiled pattern, or the outcome that ends the op (a
    /// SyntaxError, or the library's limit status).
    /// </summary>
    private string? Compile(string pattern, string flagsText, out BroilerRegex? regex)
    {
        regex = null;
        if (RegexFlagsText.Parse(flagsText, out var flags, out _) != RegexStatus.Success)
            return "error: SyntaxError";
        return BroilerRegex.Compile(pattern, flags, _tables, Nesting, null, null, out regex, out var limit, out _) switch
        {
            RegexStatus.Success => null,
            RegexStatus.SyntaxError => "error: SyntaxError",
            RegexStatus.LimitExceeded => Limit(limit),
            var other => "fault: Compile answered " + other,
        };
    }

    private string Construct(string pattern, string flags) => Compile(pattern, flags, out _) ?? "value: bool:true";

    private string Literal(string body, string flags) =>
        BroilerRegex.Validate(body, flags, _tables, Nesting, out _, out var limit) switch
        {
            RegexStatus.Success => "value: bool:true",
            RegexStatus.SyntaxError => "error: SyntaxError",
            RegexStatus.LimitExceeded => Limit(limit),
            var other => "fault: Validate answered " + other,
        };

    /// <summary>
    /// RegExpBuiltinExec (ECMA-262 §22.2.7.2) from a <c>lastIndex</c> of <paramref name="lastIndex"/>, through the
    /// string-bearing <see cref="BroilerRegex.Match(string, int)"/>, which runs the matcher from it (a surrogate pair's
    /// start under <c>u</c> and <c>v</c>, decision D41's case at the pair's first unit) and tries it alone under
    /// <c>y</c>; the runner takes the steps around it: <c>lastIndex</c> read as 0 without <c>g</c> and <c>y</c>, a
    /// <c>lastIndex</c> past the end answering null, and the property written under <c>g</c> or <c>y</c>, the match's
    /// end or 0.
    /// </summary>
    private string Exec(string pattern, string flagsText, string subject, int lastIndex, bool lengths)
    {
        if (Compile(pattern, flagsText, out var regex) is { } ended)
            return ended;
        var flags = regex!.Flags;
        var writes = (flags & (RegexFlags.Global | RegexFlags.Sticky)) != 0;
        var from = writes ? lastIndex : 0;
        if (from > subject.Length)
            return Serialised(writes ? 0 : lastIndex, regex, null, subject, lengths);
        var match = regex.Match(subject, from);
        if (match.Limit != RegexLimit.None)
            return Limit(match.Limit);
        if (!match.Success)
            return Serialised(writes ? 0 : lastIndex, regex, null, subject, lengths);
        return Serialised(writes ? match.Index + match.Length : lastIndex, regex, Spans(regex, match), subject, lengths);
    }

    /// <summary>
    /// A budget vector: RegExpBuiltinExec's steps over the offset entry, with a step ceiling of <paramref name="budget"/>
    /// units and the engines' frame and trail ceilings, and a meter that counts what it is charged; a start inside a
    /// surrogate pair under <c>u</c> or <c>v</c> steps back to the pair, as the string-bearing member does.
    /// </summary>
    private string Budget(string pattern, string flagsText, string subject, int lastIndex, ulong budget)
    {
        if (Compile(pattern, flagsText, out var regex) is { } ended)
            return ended;
        var flags = regex!.Flags;
        var writes = (flags & (RegexFlags.Global | RegexFlags.Sticky)) != 0;
        var start = writes ? lastIndex : 0;
        if (start > subject.Length)
            return Serialised(writes ? 0 : lastIndex, regex, null, subject, lengths: false);
        var from = start;
        if (RegexFlagsParser.IsUnicodeMode(flags) && from > 0 && from < subject.Length && char.IsLowSurrogate(subject[from]) && char.IsHighSurrogate(subject[from - 1]))
            from--;
        var charged = 0UL;
        var slots = new int[regex.SlotCount];
        var status = regex.Match(subject, from, (flags & RegexFlags.Sticky) != 0, slots, units => charged += units,
            new RegexMatchCeilings(budget, RegexMatchCeilings.EngineFrames, RegexMatchCeilings.EngineTrail), out var limit);
        if (status == RegexStatus.LimitExceeded)
            return Limit(limit);
        if (status != RegexStatus.Match)
            return Serialised(writes ? 0 : lastIndex, regex, null, subject, lengths: false);
        var spans = new (int Start, int End)[regex.CaptureCount + 1];
        for (var group = 0; group < spans.Length; group++)
            spans[group] = (slots[2 * group], slots[(2 * group) + 1]);
        if (spans[0].Start == from && spans[0].End >= start)
            spans[0].Start = start;
        return Serialised(writes ? spans[0].End : lastIndex, regex, spans, subject, lengths: false);
    }

    private string Test(string pattern, string flags, string subject)
    {
        if (Compile(pattern, flags, out var regex) is { } ended)
            return ended;
        var match = regex!.Match(subject, 0);
        return match.Limit != RegexLimit.None ? Limit(match.Limit) : match.Success ? "value: bool:true" : "value: bool:false";
    }

    /// <summary>
    /// RegExp.prototype[%Symbol.match%] (§22.2.6.8) on a new instance: without <c>g</c> the exec's array, with it every
    /// match's text, through <see cref="BroilerRegex.NextMatch"/>; rendered as the vectors' <c>$a</c> writes an array.
    /// </summary>
    private string SymbolMatch(string pattern, string flags, string subject)
    {
        if (Compile(pattern, flags, out var regex) is { } ended)
            return ended;
        var match = regex!.Match(subject, 0);
        var items = new List<string?>();
        if ((regex.Flags & RegexFlags.Global) == 0)
        {
            if (match.Limit != RegexLimit.None)
                return Limit(match.Limit);
            if (!match.Success)
                return "value: " + VectorNotation.Str("null");
            foreach (var group in match.Groups)
                items.Add(group.Success ? group.Value : null);
        }
        else
        {
            while (match.Success)
            {
                items.Add(match.Value);
                match = regex.NextMatch(match);
            }

            if (match.Limit != RegexLimit.None)
                return Limit(match.Limit);
            if (items.Count == 0)
                return "value: " + VectorNotation.Str("null");
        }

        var text = new StringBuilder(VectorFile.Decimal(items.Count));
        foreach (var item in items)
            text.Append(';').Append(item is null ? "u" : VectorFile.Decimal(item.Length) + ":" + item);
        return "value: " + VectorNotation.Str(text.ToString());
    }

    private string Search(string pattern, string flags, string subject)
    {
        if (Compile(pattern, flags, out var regex) is { } ended)
            return ended;
        var match = regex!.Match(subject, 0);
        if (match.Limit != RegexLimit.None)
            return Limit(match.Limit);
        return "value: " + VectorNotation.F64(match.Success ? match.Index : -1);
    }

    private string Escape(string text) =>
        _escaper.EscapeString(text, int.MaxValue, null, out var escaped) == RegexStatus.Success
            ? "value: " + VectorNotation.Str(escaped!)
            : "status: limit-length";

    private string RoundTrip(string text, string flags)
    {
        if (_escaper.EscapeString(text, int.MaxValue, null, out var escaped) != RegexStatus.Success)
            return "status: limit-length";
        return Test(escaped!, flags, text);
    }

    private string FlagsGetter(string flags)
    {
        if (Compile("a", flags, out var regex) is { } ended)
            return ended;
        return "value: " + VectorNotation.Str(RegexFlagsText.Format(regex!.Flags));
    }

    /// <summary>
    /// A literal <c>/P/F</c> and its constructor twin give one <c>source</c>: the literal's P is its body text, the
    /// constructor's the pattern the instance keeps, and EscapeRegExpPattern of each is compared.
    /// </summary>
    private string OnePattern(string body, string flags)
    {
        if (Compile(body, flags, out var regex) is { } ended)
            return ended;
        if (RegexEscaper.EscapePattern(body, int.MaxValue, null, out var fromLiteral) != RegexStatus.Success
            || RegexEscaper.EscapePattern(regex!.Pattern, int.MaxValue, null, out var fromConstructor) != RegexStatus.Success)
            return "status: limit-length";
        return string.Equals(fromLiteral, fromConstructor, StringComparison.Ordinal) ? "value: bool:true" : "value: bool:false";
    }

    /// <summary>
    /// EscapeRegExpPattern's invariants (§22.2.6.13.1): S holds no line terminator, <c>/S/F</c> is a
    /// RegularExpressionLiteral whose body is S (§12.9.5), valid under F (§13.2.7.2, the validate entry), and matches the
    /// subject as the pattern does. The vector's script holds the reference's answer for the match; the runner compares
    /// S's answer with the pattern's own, whose answers the exec vectors judge.
    /// </summary>
    private string LiteralOfSource(string pattern, string flags, string subject)
    {
        if (Compile(pattern, flags, out var regex) is { } ended)
            return ended;
        if (RegexEscaper.EscapePattern(regex!.Pattern, int.MaxValue, null, out var escaped) != RegexStatus.Success || escaped is not { } source)
            return "status: limit-length";
        if (source.AsSpan().IndexOfAny("\n\r\u2028\u2029") >= 0 || !IsRegularExpressionBody(source))
            return "value: bool:false";
        if (BroilerRegex.Validate(source, flags, _tables, Nesting, out _, out _) != RegexStatus.Success)
            return "value: bool:false";
        var asSource = Test(source, flags, subject);
        var asPattern = Test(pattern, flags, subject);
        return asSource == asPattern ? "value: bool:true" : "value: bool:false";
    }

    /// <summary>
    /// Whether <paramref name="text"/> is a RegularExpressionBody (§12.9.5): a first character that is not <c>*</c>, and
    /// characters up to its end with no line terminator, no <c>/</c> outside a class, a backslash always followed by a
    /// character, and every class closed, so that <c>/text/</c> ends at its last <c>/</c>.
    /// </summary>
    internal static bool IsRegularExpressionBody(string text)
    {
        if (text.Length == 0 || text[0] == '*')
            return false;
        var inClass = false;
        for (var at = 0; at < text.Length; at++)
        {
            var c = text[at];
            if (c is '\n' or '\r' or '\u2028' or '\u2029')
                return false;
            if (c == '\\')
            {
                if (++at >= text.Length || text[at] is '\n' or '\r' or '\u2028' or '\u2029')
                    return false;
            }
            else if (inClass)
            {
                inClass = c != ']';
            }
            else if (c == '[')
            {
                inClass = true;
            }
            else if (c == '/')
            {
                return false;
            }
        }

        return !inClass;
    }

    /// <summary>A group's span: the string-bearing match's groups, group 0 from its <see cref="RegexMatch.Index"/>.</summary>
    private static (int Start, int End)[] Spans(BroilerRegex regex, RegexMatch match)
    {
        var spans = new (int Start, int End)[regex.CaptureCount + 1];
        var groups = match.Groups;
        for (var group = 0; group < spans.Length; group++)
            spans[group] = group < groups.Length && groups[group].Success ? (groups[group].Index, groups[group].Index + groups[group].Length) : (-1, -1);
        spans[0] = (match.Index, match.Index + match.Length);
        return spans;
    }

    /// <summary>
    /// The vectors' canonical serialisation of an exec's result (the generator's CANONICAL_JS <c>$x</c>), or with
    /// <paramref name="lengths"/> its lengths alone (<c>$n</c>): <c>lastIndex=</c> the property after the call, then
    /// <c>null</c> or the index, the array's length, each element as its length and text or <c>u</c>, the
    /// <c>groups</c> object in the order of the names' first groups, and the <c>d</c> flag's indices.
    /// </summary>
    private static string Serialised(int lastIndex, BroilerRegex regex, (int Start, int End)[]? spans, string subject, bool lengths)
    {
        var text = new StringBuilder("lastIndex=").Append(VectorFile.Decimal(lastIndex));
        if (spans is null)
            return "value: " + VectorNotation.Str(text.Append(";null").ToString());
        text.Append(";index=").Append(VectorFile.Decimal(spans[0].Start)).Append(";length=").Append(VectorFile.Decimal(spans.Length));
        for (var group = 0; group < spans.Length; group++)
        {
            text.Append(';').Append(VectorFile.Decimal(group)).Append('=');
            if (spans[group].Start < 0)
                text.Append('u');
            else if (lengths)
                text.Append(VectorFile.Decimal(spans[group].End - spans[group].Start));
            else
                text.Append(VectorFile.Decimal(spans[group].End - spans[group].Start)).Append(':').Append(subject, spans[group].Start, spans[group].End - spans[group].Start);
        }

        if (lengths)
            return "value: " + VectorNotation.Str(text.ToString());

        var names = regex.GroupNames;
        text.Append(";groups=");
        AppendGroups(text, names, spans, group => spans[group].Start < 0 ? "u" : VectorFile.Decimal(spans[group].End - spans[group].Start) + ":" + subject[spans[group].Start..spans[group].End]);
        if ((regex.Flags & RegexFlags.HasIndices) == 0)
            return "value: " + VectorNotation.Str(text.Append(";indices=u").ToString());
        text.Append(";indices=").Append(VectorFile.Decimal(spans.Length));
        for (var group = 0; group < spans.Length; group++)
            text.Append(';').Append(VectorFile.Decimal(group)).Append('=').Append(Pair(spans[group]));
        text.Append(";igroups=");
        AppendGroups(text, names, spans, group => Pair(spans[group]));
        return "value: " + VectorNotation.Str(text.ToString());
    }

    private static string Pair((int Start, int End) span) =>
        span.Start < 0 ? "u" : VectorFile.Decimal(span.Start) + "-" + VectorFile.Decimal(span.End);

    /// <summary>
    /// The <c>groups</c> object (§22.2.7.2, step 34): <c>u</c> without names, else their count and each name with the
    /// value of its group that participated, or of its first group, unmatched, when none did.
    /// </summary>
    private static void AppendGroups(StringBuilder text, RegexGroupNames names, (int Start, int End)[] spans, Func<int, string> value)
    {
        if (names.Count == 0)
        {
            text.Append('u');
            return;
        }

        text.Append(VectorFile.Decimal(names.Count));
        for (var at = 0; at < names.Count; at++)
        {
            var name = names.NameAt(at);
            var groups = names.GroupsNamed(name);
            var chosen = groups[0];
            foreach (var group in groups)
            {
                if (spans[group].Start >= 0)
                {
                    chosen = group;
                    break;
                }
            }

            text.Append(";<").Append(name).Append(">=").Append(value(chosen));
        }
    }

    /// <summary>A limit as the library answers it: the corpus's <c>exhausted</c> for steps, a word of its own for the others.</summary>
    internal static string Limit(RegexLimit limit) => limit switch
    {
        RegexLimit.Steps => "status: exhausted",
        RegexLimit.Nesting => "status: limit-nesting",
        RegexLimit.Frames => "status: limit-frames",
        RegexLimit.Trail => "status: limit-trail",
        RegexLimit.Compile => "status: limit-compile",
        _ => "fault: a limit the library does not name: " + limit,
    };

    private static string Repeat(string piece, int count)
    {
        var text = new StringBuilder(piece.Length * count);
        for (var i = 0; i < count; i++)
            text.Append(piece);
        return text.ToString();
    }
}
