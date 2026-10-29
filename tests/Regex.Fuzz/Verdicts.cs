using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Broiler.Regex.Engine;
using Broiler.Regex.Text;
using Broiler.Unicode.Tables;

namespace Broiler.Regex.Fuzz;

/// <summary>
/// One finding: what kind of failure, where (the entry and its arguments), and what was seen. A finding is never a
/// status, a syntax refusal, the meter's own exception or an argument exception a member documents for an argument the
/// session passed on purpose: those are answers.
/// </summary>
/// <param name="Kind">
/// <c>exception</c>, an exception the entry does not document; <c>disagreement</c>, two entries (or two policies of one
/// entry) answering one question differently; <c>contract</c>, an answer the member's documentation rules out;
/// <c>precondition</c>, a documented argument exception missing or of another type; <c>process-end</c> and <c>hang</c>,
/// found by the supervisor; <c>fuzzer</c>, a defect of this session's own code.
/// </param>
internal sealed record Finding(string Kind, string Site, string Detail)
{
    /// <summary>What a reduction must keep: the kind, the entry without its start, and the exception's type.</summary>
    internal string Signature
    {
        get
        {
            var site = Site.Split('@')[0];
            var type = Kind == "exception" ? Detail.Split(':')[0] : "";
            return Kind + "|" + site + "|" + type;
        }
    }

    internal string Line => Kind + "\t" + Site + "\t" + Detail.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
}

/// <summary>What one iteration did: its transcript record, its findings, and the outcomes and constructs it reached.</summary>
internal sealed class Outcome
{
    internal StringBuilder Record { get; } = new();

    internal List<Finding> Findings { get; } = [];

    internal SortedDictionary<string, long> Reached { get; } = new(StringComparer.Ordinal);

    internal void Reach(string key) => Reached[key] = Reached.TryGetValue(key, out var count) ? count + 1 : 1;

    internal void Find(string kind, string site, string detail) => Findings.Add(new Finding(kind, site, detail));

    internal void Note(string field, string value) => Record.Append(field).Append('=').Append(value).Append(';');
}

/// <summary>The session's own exception for a meter whose allowance is spent: an answer, never a finding.</summary>
internal sealed class AllowanceSpent : Exception
{
    internal AllowanceSpent()
        : base("The fuzz session's meter allowance is spent.")
    {
    }
}

/// <summary>A meter that counts what it is charged and, with an allowance, throws <see cref="AllowanceSpent"/> once more is charged.</summary>
internal sealed class FuzzMeter
{
    /// <summary>
    /// An allowance no case is generated with, which makes the meter throw an exception that is not the session's own at
    /// its first charge: the tests' control that the oracle reports every exception but the declared ones.
    /// </summary>
    internal const ulong ForeignFault = ulong.MaxValue;

    private readonly ulong? _allowance;

    internal FuzzMeter(ulong? allowance)
    {
        _allowance = allowance;
        Charge = Add;
    }

    internal ulong Charged { get; private set; }

    internal RegexWorkCharge Charge { get; }

    private void Add(ulong units)
    {
        if (_allowance == ForeignFault)
            throw new InvalidOperationException("a meter's exception that is not the session's own");
        Charged += units;
        if (_allowance is { } allowance && Charged > allowance)
            throw new AllowanceSpent();
    }
}

/// <summary>
/// The oracle: runs one case through every public entry and judges each answer by the entry's documentation, and each
/// pair of entries that answer the same question by each other.
/// </summary>
/// <remarks>
/// <para>
/// <b>Compilation</b>: <see cref="BroilerRegex.Compile"/> under the case's full policy and under its tight one, both
/// with a nesting limit, a compile budget and a meter; each answers <c>Success</c>, <c>LimitExceeded</c> naming nesting or
/// compile, a <see cref="RegexSyntaxException"/> or the meter's exception. Limits are monotone: a tight compilation that
/// succeeds or refuses a syntax error answers as the full one does. The constructor, which takes no limit, answers as
/// the full compilation at <see cref="BroilerRegex.MaximumNestingLimit"/> does.
/// </para>
/// <para>
/// <b>Matching</b>: the offset entry from every start of the case, anchored and not, under the wide ceilings (a step
/// ceiling and the engines' frames and trail) and once under narrow ones; its slots are held to their contract, and a
/// narrow call that completes answers as the wide one does. The string-bearing <see cref="BroilerRegex.Match(string, int)"/>,
/// <see cref="BroilerRegex.IsMatch"/> and <see cref="BroilerRegex.Matches"/> are compared with the offset entry's
/// answers through the mapping their documentation states (RegExpBuiltinExec's step back into a surrogate pair, the
/// <c>y</c> flag as the anchored argument, the reported index of decision D41, the iteration of §22.2.6.9), each
/// <c>Value</c> they report read against the subject at the offsets they report, wherever the wide call completed or
/// met the engines' frame or trail ceiling, which the string-bearing members also pass.
/// </para>
/// <para>
/// <b>The text helpers</b> (R9): <see cref="RegexEscaper.EscapePattern"/>'s text compiles as the pattern does and
/// matches the subject as it does; <see cref="RegexEscaper.EscapeString"/>'s text, compiled under any flags, matches the
/// subject whole, anchored at 0.
/// </para>
/// </remarks>
internal static class Verdicts
{
    private const int Untouched = 0x5EED5EED;

    /// <summary>The units the emulation of <see cref="BroilerRegex.Matches"/> may spend before the comparison is skipped.</summary>
    private const ulong MatchesAllowance = 1UL << 20;

    private readonly record struct Compiled(string Answer, BroilerRegex? Regex, int Position, string Message, ulong Units)
    {
        internal string Text => Regex is { } regex
            ? string.Create(CultureInfo.InvariantCulture, $"ok:{regex.CaptureCount}:{regex.SlotCount}:u{Units}")
            : Answer == "syntax" ? string.Create(CultureInfo.InvariantCulture, $"syntax@{Position}:{Hash(Message)}:u{Units}")
            : string.Create(CultureInfo.InvariantCulture, $"{Answer}:u{Units}");
    }

    private readonly record struct Matched(string Answer, int[] Slots, ulong Units)
    {
        internal bool Completed => Answer is "match" or "nomatch";

        internal bool Equivalent(Matched other) => Answer == other.Answer && Slots.AsSpan().SequenceEqual(other.Slots);

        internal string Text => Answer == "match"
            ? string.Create(CultureInfo.InvariantCulture, $"m({SlotsText(Slots)}):u{Units}")
            : string.Create(CultureInfo.InvariantCulture, $"{Answer}:u{Units}");
    }

    /// <summary>Runs <paramref name="c"/> and judges every answer.</summary>
    internal static Outcome Run(FuzzCase c)
    {
        var outcome = new Outcome();
        foreach (var construct in c.Constructs)
            outcome.Reach("construct:" + construct);
        outcome.Note("case", Hash(c.Pattern + "\u0001" + FuzzCase.FlagsText(c.Flags) + "\u0001" + c.Subject));
        try
        {
            Judge(c, outcome);
        }
        catch (Exception failure)
        {
            // Every library call is made inside its own catch; an exception here is the session's own defect.
            outcome.Find("fuzzer", "oracle", Describe(failure));
        }

        return outcome;
    }

    private static void Judge(FuzzCase c, Outcome outcome)
    {
        var full = Compile(c.Pattern, c.Flags, c.Full, "compile/full", outcome);
        outcome.Note("full", full.Text);
        outcome.Reach("compile:" + full.Answer);
        var tight = Compile(c.Pattern, c.Flags, c.Tight, "compile/tight", outcome);
        outcome.Note("tight", tight.Text);
        outcome.Reach("compile:" + tight.Answer);
        CompareCompilations(full, tight, outcome);
        CompareConstructor(c, full, outcome);

        if (full.Regex is { } regex)
        {
            if (regex.SlotCount != 2 * (regex.CaptureCount + 1))
                outcome.Find("contract", "compile/full", Invariant($"SlotCount {regex.SlotCount} is not 2 \u00D7 (CaptureCount {regex.CaptureCount} + 1)"));
            var wide = new Dictionary<(int, bool), Matched>();
            foreach (var start in c.Starts)
            {
                foreach (var anchored in (ReadOnlySpan<bool>)[false, true])
                {
                    var answer = Offset(regex, c.Subject, start, anchored, c.Wide, Site("offset/wide", start, anchored), outcome);
                    wide[(start, anchored)] = answer;
                    outcome.Note(Site("w", start, anchored), answer.Text);
                    outcome.Reach("offset:" + answer.Answer);
                }
            }

            Narrow(c, regex, wide, outcome);
            StringMembers(c, regex, wide, outcome);
            if (c.Probes)
                Probes(c, regex, outcome);
        }

        if (c.Escapes)
            Escapes(c, full, outcome);
    }

    // ----- Compilation ----------------------------------------------------------------------------------------------

    private static Compiled Compile(string pattern, RegexFlags flags, CompilePolicy policy, string site, Outcome outcome)
    {
        var meter = new FuzzMeter(policy.Meter);
        try
        {
            var status = BroilerRegex.Compile(pattern, flags, UnicodeTables.Instance, policy.Nesting, policy.Budget, meter.Charge, out var regex, out var limit);
            if (status == RegexStatus.Success && regex is not null && limit == RegexLimit.None)
                return new("ok", regex, -1, "", meter.Charged);
            if (status == RegexStatus.LimitExceeded && regex is null && limit is RegexLimit.Nesting or RegexLimit.Compile)
                return new(limit == RegexLimit.Nesting ? "limit:nesting" : "limit:compile", null, -1, "", meter.Charged);
            outcome.Find("contract", site, Invariant($"Compile answered {status} with limit {limit} and {(regex is null ? "no" : "a")} compiled pattern"));
            return new("contract", null, -1, "", meter.Charged);
        }
        catch (RegexSyntaxException refusal)
        {
            return new("syntax", null, refusal.Position, refusal.Message, meter.Charged);
        }
        catch (AllowanceSpent)
        {
            return new("meter", null, -1, "", meter.Charged);
        }
        catch (Exception failure)
        {
            outcome.Find("exception", site, Describe(failure));
            return new("escaped", null, -1, "", meter.Charged);
        }
    }

    /// <summary>A tight compilation that completes, or meets a syntax error, met no limit first: the full one answers alike.</summary>
    private static void CompareCompilations(Compiled full, Compiled tight, Outcome outcome)
    {
        if (tight.Regex is { } compiled && (full.Regex is not { } wide || wide.CaptureCount != compiled.CaptureCount || wide.SlotCount != compiled.SlotCount))
            outcome.Find("disagreement", "compile/tight", "the tight policy compiled the pattern " + tight.Text + " where the full policy answered " + full.Text);
        if (tight.Answer == "syntax" && (full.Answer != "syntax" || full.Position != tight.Position || full.Message != tight.Message))
            outcome.Find("disagreement", "compile/tight", Invariant($"the tight policy refused a syntax error at {tight.Position} ({tight.Message}) where the full policy answered {full.Text}"));
    }

    /// <summary>The constructor takes no limit and parses under the maximum: it answers as the full compilation does.</summary>
    private static void CompareConstructor(FuzzCase c, Compiled full, Outcome outcome)
    {
        if (full.Answer is not ("ok" or "syntax" or "limit:nesting"))
            return;
        try
        {
            var built = new BroilerRegex(c.Pattern, c.Flags, UnicodeTables.Instance);
            outcome.Note("ctor", "ok");
            if (full.Regex is not { } regex || regex.CaptureCount != built.CaptureCount || regex.SlotCount != built.SlotCount)
                outcome.Find("disagreement", "constructor", "the constructor compiled a pattern Compile answered " + full.Text);
        }
        catch (RegexSyntaxException refusal)
        {
            outcome.Note("ctor", Invariant($"syntax@{refusal.Position}"));
            if (full.Answer == "ok" || full.Answer == "syntax" && (refusal.Position != full.Position || refusal.Message != full.Message))
                outcome.Find("disagreement", "constructor", Invariant($"the constructor refused a syntax error at {refusal.Position} ({refusal.Message}) where Compile answered {full.Text} {full.Message}"));
        }
        catch (Exception failure)
        {
            outcome.Find("exception", "constructor", Describe(failure));
        }
    }

    // ----- The offset entry -----------------------------------------------------------------------------------------

    private static Matched Offset(BroilerRegex regex, string subject, int start, bool anchored, MatchPolicy policy, string site, Outcome outcome)
    {
        var count = regex.SlotCount;
        var slots = new int[count + 2];
        Array.Fill(slots, Untouched);
        var meter = new FuzzMeter(policy.Meter);
        string answer;
        try
        {
            var status = regex.Match(subject.AsSpan(), start, anchored, slots, meter.Charge, policy.Ceilings, out var limit);
            answer = (status, limit) switch
            {
                (RegexStatus.Match, RegexLimit.None) => "match",
                (RegexStatus.NoMatch, RegexLimit.None) => "nomatch",
                (RegexStatus.LimitExceeded, RegexLimit.Steps) when policy.Steps is not null => "limit:steps",
                (RegexStatus.LimitExceeded, RegexLimit.Frames) => "limit:frames",
                (RegexStatus.LimitExceeded, RegexLimit.Trail) => "limit:trail",
                _ => "",
            };
            if (answer.Length == 0)
            {
                outcome.Find("contract", site, Invariant($"the offset entry answered {status} with limit {limit}"));
                answer = "contract";
            }
        }
        catch (AllowanceSpent)
        {
            answer = "meter";
        }
        catch (Exception failure)
        {
            outcome.Find("exception", site, Describe(failure));
            return new("escaped", slots[..count], meter.Charged);
        }

        if (slots[count] != Untouched || slots[count + 1] != Untouched)
            outcome.Find("contract", site, Invariant($"the offset entry wrote past its {count} slots"));
        if (answer == "match")
            CheckMatchSlots(slots.AsSpan(0, count), subject.Length, start, anchored, site, outcome);
        else if (slots.AsSpan(0, count).ContainsAnyExcept(-1))
            outcome.Find("contract", site, "the offset entry answered " + answer + " and left a slot other than -1: " + SlotsText(slots.AsSpan(0, count)));
        return new(answer, slots[..count], meter.Charged);
    }

    private static void CheckMatchSlots(ReadOnlySpan<int> slots, int length, int start, bool anchored, string site, Outcome outcome)
    {
        var (first, end) = (slots[0], slots[1]);
        if (first < 0 || first > end || end > length || first < start || anchored && first != start)
        {
            outcome.Find("contract", site, Invariant($"the match's offsets {first},{end} are not a match from {start}{(anchored ? " anchored" : "")} in a subject of {length}"));
            return;
        }

        for (var group = 1; group < slots.Length / 2; group++)
        {
            var (from, to) = (slots[2 * group], slots[(2 * group) + 1]);
            if (from == -1 && to == -1)
                continue;
            if (from < 0 || from > to || to > length)
                outcome.Find("contract", site, Invariant($"group {group}'s offsets {from},{to} are neither -1,-1 nor a span of the subject of {length}"));
        }
    }

    /// <summary>
    /// One call under the narrow policy. If it completes, the wide call from the same start answers alike; if its meter
    /// throws, the same instance then answers the wide call's question as before (convention 4).
    /// </summary>
    private static void Narrow(FuzzCase c, BroilerRegex regex, Dictionary<(int, bool), Matched> wide, Outcome outcome)
    {
        var site = Site("offset/narrow", c.NarrowStart, c.NarrowAnchored);
        var narrow = Offset(regex, c.Subject, c.NarrowStart, c.NarrowAnchored, c.Narrow, site, outcome);
        outcome.Note("narrow", narrow.Text);
        outcome.Reach("offset:" + narrow.Answer);
        var reference = wide[(c.NarrowStart, c.NarrowAnchored)];
        if (narrow.Completed && (c.Narrow.Steps is not null || reference.Completed) && !narrow.Equivalent(reference))
            outcome.Find("disagreement", site, "narrow ceilings answered " + narrow.Text + " where the wide ones answered " + reference.Text);
        if (narrow.Answer == "meter")
        {
            var again = Offset(regex, c.Subject, c.NarrowStart, c.NarrowAnchored, c.Wide, site + "/after-meter", outcome);
            if (!again.Equivalent(reference) || again.Units != reference.Units)
                outcome.Find("disagreement", site + "/after-meter", "after the meter's exception the same instance answered " + again.Text + " where it answered " + reference.Text);
        }
    }

    // ----- The string-bearing members -------------------------------------------------------------------------------

    private static void StringMembers(FuzzCase c, BroilerRegex regex, Dictionary<(int, bool), Matched> wide, Outcome outcome)
    {
        var subject = c.Subject;
        var sticky = (c.Flags & RegexFlags.Sticky) != 0;
        var starts = c.Probes ? c.Starts.Append(-1).Append(subject.Length + 1).ToArray() : c.Starts.ToArray();
        foreach (var start in starts)
        {
            var site = Invariant($"string-match@{start}");
            if (start > subject.Length)
            {
                var past = Call(() => regex.Match(subject, start), site, outcome);
                if (past is not null && (past.Success || past.Limit != RegexLimit.None || past.Index != -1))
                    outcome.Find("contract", site, "a start past the end answered other than no match");
                continue;
            }

            var from = StepBack(regex.Flags, subject, Math.Max(start, 0));
            var reference = Wide(c, regex, wide, from, sticky, outcome);
            if (reference.Answer is not ("match" or "nomatch" or "limit:frames" or "limit:trail"))
            {
                outcome.Note(site, "skipped");
                continue;
            }

            var match = Call(() => regex.Match(subject, start), site, outcome);
            if (match is not null)
            {
                outcome.Note(site, match.Success ? Invariant($"m{match.Index},{match.Length}") : match.Limit.ToString());
                outcome.Reach(match.Success ? "string:match" : match.Limit == RegexLimit.None ? "string:nomatch" : "string:limit");
                var expected = Expected(subject, Math.Max(start, 0), from, reference);
                var difference = Compare(match, expected, subject, regex);
                if (difference is not null)
                    outcome.Find("disagreement", site, difference + "; the offset entry from " + from.ToString(CultureInfo.InvariantCulture) + (sticky ? " anchored" : "") + " answered " + reference.Text);
            }

            IsMatch(regex, subject, start, reference, outcome);
        }

        Matches(c, regex, wide, outcome);
    }

    /// <summary>RegExpBuiltinExec's step 13.b: under <c>u</c> or <c>v</c> a start inside a surrogate pair runs from the pair.</summary>
    private static int StepBack(RegexFlags flags, string subject, int start) =>
        (flags & (RegexFlags.Unicode | RegexFlags.UnicodeSets)) != 0 && start > 0 && start < subject.Length
            && char.IsLowSurrogate(subject[start]) && char.IsHighSurrogate(subject[start - 1]) ? start - 1 : start;

    private static Matched Wide(FuzzCase c, BroilerRegex regex, Dictionary<(int, bool), Matched> wide, int from, bool anchored, Outcome outcome)
    {
        if (!wide.TryGetValue((from, anchored), out var answer))
        {
            answer = Offset(regex, c.Subject, from, anchored, c.Wide, Site("offset/wide", from, anchored), outcome);
            wide[(from, anchored)] = answer;
        }

        return answer;
    }

    /// <summary>What a string-bearing match from <paramref name="start"/> answers, by its documentation, given the offset entry's answer from <paramref name="from"/>.</summary>
    private static (bool Success, int Index, int Length, RegexLimit Limit, int[] Slots) Expected(string subject, int start, int from, Matched reference)
    {
        if (reference.Answer != "match")
            return (false, -1, 0, reference.Answer switch { "limit:frames" => RegexLimit.Frames, "limit:trail" => RegexLimit.Trail, _ => RegexLimit.None }, []);
        var end = reference.Slots[1];
        var index = reference.Slots[0] == from && end >= start ? start : reference.Slots[0];
        return (true, index, end - index, RegexLimit.None, reference.Slots);
    }

    /// <summary>
    /// A string-bearing match against what the offset entry's answer makes of it: its status and limit; for a failed
    /// match the shape <see cref="RegexMatch.Empty"/> and the ceilings' matches document (no index, no text, no groups);
    /// for a match its index, length and <see cref="RegexMatch.Value"/>, the subject's text there; group 0 as the whole
    /// match; each group's offsets and <see cref="RegexGroup.Value"/> from the slots, the empty text and index -1 for a
    /// group that did not participate, and each group's <see cref="RegexGroup.Number"/> its own; and each
    /// <see cref="RegexMatch.NamedGroups"/> entry as the participating group of its name where one participated, its
    /// <see cref="RegexGroup.Number"/> that group's, whose slots it reports, and <see cref="RegexGroupNames.ParticipatingGroup"/>
    /// reading the same group from the slots; or, where none did, as no match, numbered as the name's first group.
    /// </summary>
    private static string? Compare(RegexMatch match, (bool Success, int Index, int Length, RegexLimit Limit, int[] Slots) expected, string subject, BroilerRegex regex)
    {
        var captureCount = regex.CaptureCount;
        if (match.Success != expected.Success || match.Limit != expected.Limit)
            return Invariant($"the string-bearing match answered success {match.Success}, limit {match.Limit}; expected success {expected.Success}, limit {expected.Limit}");
        if (!expected.Success)
        {
            if (match.Index != -1 || match.Length != 0 || match.Value.Length != 0 || match.Groups.Count != 0 || match.NamedGroups.Count != 0)
                return Invariant($"the failed string-bearing match has index {match.Index}, length {match.Length}, value {FuzzCase.Quote(match.Value)}, {match.Groups.Count} groups and {match.NamedGroups.Count} names; a failed match has index -1, length 0, the empty value and none of the others");
            return null;
        }

        if (match.Index != expected.Index || match.Length != expected.Length)
            return Invariant($"the string-bearing match is {match.Index},{match.Length}; expected {expected.Index},{expected.Length}");
        // Offsets outside the subject are the offset entry's contract finding already (CheckMatchSlots); none is read here.
        if (!Within(subject, expected.Index, expected.Index + expected.Length))
            return null;
        var value = subject.Substring(expected.Index, expected.Length);
        if (match.Value != value)
            return Invariant($"the string-bearing match {expected.Index},{expected.Length} has the value {FuzzCase.Quote(match.Value)}; the subject there is {FuzzCase.Quote(value)}");
        if (match.Groups.Count != captureCount + 1)
            return Invariant($"the string-bearing match has {match.Groups.Count} groups for {captureCount} captures");
        var whole = match.Groups[0];
        if (!whole.Success || whole.Index != expected.Index || whole.Length != expected.Length || whole.Value != value || whole.Name is not null || whole.Number != 0)
            return Invariant($"group 0 is {whole.Success} {whole.Index},{whole.Length} {FuzzCase.Quote(whole.Value)}{(whole.Name is null ? "" : " named " + whole.Name)}, number {whole.Number}; the match is {expected.Index},{expected.Length} {FuzzCase.Quote(value)}, group 0");
        // Each name's groups by number, ascending: the first of them, and those the slots say participated.
        var names = new SortedDictionary<string, (int First, List<int> Participating)>(StringComparer.Ordinal);
        for (var group = 1; group <= captureCount; group++)
        {
            var (from, to) = (expected.Slots[2 * group], expected.Slots[(2 * group) + 1]);
            var actual = match.Groups[group];
            var took = from >= 0;
            if (took && !Within(subject, from, to))
                return null;
            var text = took ? subject[from..to] : "";
            if (actual.Success != took || actual.Index != (took ? from : -1) || actual.Length != (took ? to - from : 0) || actual.Value != text)
                return Invariant($"group {group} is {actual.Success} {actual.Index},{actual.Length} {FuzzCase.Quote(actual.Value)}; the slots say {from},{to}{(took ? " " + FuzzCase.Quote(text) : "")}");
            if (actual.Number != group)
                return Invariant($"group {group} has the number {actual.Number}");
            if (actual.Name is { } name)
            {
                if (!names.TryGetValue(name, out var groups))
                    names[name] = groups = (group, []);
                if (took)
                    groups.Participating.Add(group);
            }
        }

        foreach (var (name, (first, participating)) in names)
        {
            if (!match.NamedGroups.TryGetValue(name, out var named))
                return "the named group " + name + " is not in NamedGroups";
            var shown = Invariant($"{named.Success} {named.Index},{named.Length} {FuzzCase.Quote(named.Value)}, number {named.Number}");
            if (named.Name != name)
                return "NamedGroups gives under the name " + name + " a group named " + (named.Name ?? "nothing");
            // The entry's number is the participating group's, and its offsets that group's slots: R6 lets at most one
            // group of a name participate, and RegexGroupNames.ParticipatingGroup reads the same one from the slots.
            if (participating.Count > 0)
            {
                var number = named.Number;
                if (!participating.Contains(number))
                    return "NamedGroups gives the name " + name + " as " + shown + ", which is no participating group of that name (" + string.Join(", ", participating.Select(static n => n.ToString(CultureInfo.InvariantCulture))) + ")";
                var (from, to) = (expected.Slots[2 * number], expected.Slots[(2 * number) + 1]);
                if (!named.Success || named.Index != from || named.Length != to - from || named.Value != subject[from..to])
                    return Invariant($"NamedGroups gives the name {name} as {shown}; the slots of group {number} say {from},{to}");
            }
            else if (named.Success || named.Index != -1 || named.Length != 0 || named.Value.Length != 0 || named.Number != first)
            {
                return Invariant($"NamedGroups gives the name {name} as {shown}, where no group of that name participated; the name's first group is {first}");
            }

            var read = regex.GroupNames.ParticipatingGroup(name, expected.Slots);
            if (read != (participating.Count > 0 ? named.Number : 0))
                return Invariant($"RegexGroupNames.ParticipatingGroup reads group {read} of the name {name} from the slots; the string-bearing match gives {shown}");
        }

        if (match.NamedGroups.Count != names.Count)
            return Invariant($"NamedGroups holds {match.NamedGroups.Count} names; the groups name {names.Count}");
        return null;
    }

    private static bool Within(string subject, int from, int to) => from >= 0 && from <= to && to <= subject.Length;

    private static void IsMatch(BroilerRegex regex, string subject, int start, Matched reference, Outcome outcome)
    {
        var site = Invariant($"is-match@{start}");
        var exhausted = reference.Answer is "limit:frames" or "limit:trail";
        try
        {
            var answer = regex.IsMatch(subject, start);
            if (exhausted || answer != (reference.Answer == "match"))
                outcome.Find("disagreement", site, Invariant($"IsMatch answered {answer}; the offset entry answered {reference.Text}"));
        }
        catch (InvalidOperationException) when (exhausted)
        {
            outcome.Reach("is-match:throws");
        }
        catch (Exception failure)
        {
            outcome.Find("exception", site, Describe(failure));
        }
    }

    /// <summary>
    /// <see cref="BroilerRegex.Matches"/> against its documentation: each element <c>Match(input, k)</c>'s from where the
    /// last ended, an empty match advancing one code point, a ceiling the last element. Emulated through the offset
    /// entry's wide calls. A call the case already made (its starts, anchored as the <c>y</c> flag says, 0 among them) is
    /// always taken, so the first element is compared on every subject, a ceiling there included, which only subjects of
    /// about 500,000 units and more reach; a new call is made only on a subject of at most 4,096 units, while the calls
    /// taken have spent less than <see cref="MatchesAllowance"/> units, for at most 4,096 elements. An emulation that
    /// reaches the enumeration's end (no match, a ceiling, or past the subject's end) is compared element for element and
    /// in its count; one those bounds, or a call that met the step ceiling or the meter, cut short is compared over the
    /// elements it has, as the enumeration's first.
    /// </summary>
    private static void Matches(FuzzCase c, BroilerRegex regex, Dictionary<(int, bool), Matched> wide, Outcome outcome)
    {
        var subject = c.Subject;
        var sticky = (c.Flags & RegexFlags.Sticky) != 0;
        var unicode = (regex.Flags & (RegexFlags.Unicode | RegexFlags.UnicodeSets)) != 0;
        var expected = new List<(bool Success, int Index, int Length, RegexLimit Limit, int[] Slots)>();
        var spent = 0UL;
        var position = 0;
        var whole = true;
        while (position <= subject.Length)
        {
            var from = StepBack(regex.Flags, subject, position);
            if (expected.Count >= 4096 || !wide.ContainsKey((from, sticky)) && (subject.Length > 4096 || spent >= MatchesAllowance))
            {
                whole = false;
                break;
            }

            var reference = Wide(c, regex, wide, from, sticky, outcome);
            spent += reference.Units;
            if (reference.Answer is not ("match" or "nomatch" or "limit:frames" or "limit:trail"))
            {
                whole = false;
                break;
            }

            if (reference.Answer == "nomatch")
                break;
            var element = Expected(subject, position, from, reference);
            expected.Add(element);
            if (!element.Success)
                break;
            var next = element.Index + element.Length;
            if (element.Length == 0)
            {
                next = element.Index + 1;
                if (unicode && element.Index + 1 < subject.Length && char.IsHighSurrogate(subject[element.Index]) && char.IsLowSurrogate(subject[element.Index + 1]))
                    next++;
            }

            position = next;
        }

        if (!whole && expected.Count == 0)
        {
            outcome.Note("matches", "skipped");
            return;
        }

        List<RegexMatch> actual;
        try
        {
            actual = regex.Matches(subject).Take(whole ? expected.Count + 1 : expected.Count).ToList();
        }
        catch (Exception failure)
        {
            outcome.Find("exception", "matches", Describe(failure));
            return;
        }

        outcome.Note("matches", expected.Count.ToString(CultureInfo.InvariantCulture) + (whole ? "" : "+"));
        outcome.Reach(whole ? "matches:compared" : "matches:prefix");
        if (expected.Count > 0 && expected[^1].Limit != RegexLimit.None)
            outcome.Reach("matches:limit");
        if (actual.Count != expected.Count)
        {
            outcome.Find("disagreement", "matches", whole
                ? Invariant($"Matches gave {(actual.Count > expected.Count ? "more than " : "")}{actual.Count} elements; the offset entry's iteration gives {expected.Count}")
                : Invariant($"Matches ended after {actual.Count} elements; the offset entry's iteration gives {expected.Count} before it was cut short"));
            return;
        }

        for (var i = 0; i < expected.Count; i++)
        {
            var difference = Compare(actual[i], expected[i], subject, regex);
            if (difference is not null)
            {
                outcome.Find("disagreement", Invariant($"matches[{i}]"), difference);
                return;
            }
        }
    }

    // ----- The text helpers (R9) ------------------------------------------------------------------------------------

    private static void Escapes(FuzzCase c, Compiled full, Outcome outcome)
    {
        var meter = new FuzzMeter(c.Tight.Meter);
        string? source = null;
        try
        {
            var status = RegexEscaper.EscapePattern(c.Pattern, c.EscapeMaximum, meter.Charge, out source);
            outcome.Note("escape-pattern", status.ToString());
            if (status == RegexStatus.Success && source is not null)
                outcome.Reach("escape-pattern:ok");
            else if (status == RegexStatus.LimitExceeded)
                outcome.Reach("escape-pattern:limit");
            else
                outcome.Find("contract", "escape-pattern", Invariant($"EscapePattern answered {status} with {(source is null ? "no" : "a")} text"));
            if (status != RegexStatus.Success)
                source = null;
        }
        catch (AllowanceSpent)
        {
            outcome.Note("escape-pattern", "meter");
        }
        catch (Exception failure)
        {
            outcome.Find("exception", "escape-pattern", Describe(failure));
        }

        if (source is not null && full.Regex is { } regex)
        {
            var again = Compile(source, c.Flags, c.Full, "escape-pattern/compile", outcome);
            if (again.Regex is not { } recompiled || recompiled.CaptureCount != regex.CaptureCount || recompiled.SlotCount != regex.SlotCount)
            {
                outcome.Find("disagreement", "escape-pattern", "the source text " + FuzzCase.Quote(source) + " compiled to " + again.Text + " where the pattern compiled to " + full.Text);
            }
            else
            {
                var before = Offset(regex, c.Subject, 0, false, c.Wide, "escape-pattern/match-pattern", outcome);
                var after = Offset(recompiled, c.Subject, 0, false, c.Wide, "escape-pattern/match-source", outcome);
                if (before.Completed && after.Completed && !before.Equivalent(after))
                    outcome.Find("disagreement", "escape-pattern", "the source text " + FuzzCase.Quote(source) + " matched " + after.Text + " where the pattern matched " + before.Text);
            }
        }

        string? text = null;
        meter = new FuzzMeter(c.Tight.Meter);
        try
        {
            var status = new RegexEscaper(UnicodeTables.Instance).EscapeString(c.Subject, c.EscapeMaximum, meter.Charge, out text);
            outcome.Note("escape-string", status.ToString());
            if (status == RegexStatus.Success && text is not null)
                outcome.Reach("escape-string:ok");
            else if (status == RegexStatus.LimitExceeded)
                outcome.Reach("escape-string:limit");
            else
                outcome.Find("contract", "escape-string", Invariant($"EscapeString answered {status} with {(text is null ? "no" : "a")} text"));
            if (status != RegexStatus.Success)
                text = null;
        }
        catch (AllowanceSpent)
        {
            outcome.Note("escape-string", "meter");
        }
        catch (Exception failure)
        {
            outcome.Find("exception", "escape-string", Describe(failure));
        }

        if (text is null)
            return;
        var escaped = Compile(text, c.EscapeFlags, c.Full, "escape-string/compile", outcome);
        if (escaped.Regex is not { } literal)
        {
            outcome.Find("disagreement", "escape-string", "RegExp.escape's text " + FuzzCase.Quote(text) + " of the subject compiled to " + escaped.Text + " under " + FuzzCase.FlagsText(c.EscapeFlags));
            return;
        }

        var whole = Offset(literal, c.Subject, 0, true, c.Wide, "escape-string/match", outcome);
        if (whole.Completed && (whole.Answer != "match" || whole.Slots[0] != 0 || whole.Slots[1] != c.Subject.Length))
            outcome.Find("disagreement", "escape-string", "RegExp.escape's text " + FuzzCase.Quote(text) + " under " + FuzzCase.FlagsText(c.EscapeFlags) + " answered " + whole.Text + " on the subject it escapes");
    }

    // ----- Preconditions --------------------------------------------------------------------------------------------

    /// <summary>Each documented argument exception, for an argument broken on purpose.</summary>
    private static void Probes(FuzzCase c, BroilerRegex regex, Outcome outcome)
    {
        var count = regex.SlotCount;
        var slots = new int[count];
        var ceilings = c.Wide.Ceilings;
        var subject = c.Subject;
        Expect<ArgumentOutOfRangeException>("probe/nesting-zero", () => BroilerRegex.Compile(c.Pattern, c.Flags, UnicodeTables.Instance, 0, null, null, out _, out _), outcome);
        Expect<ArgumentOutOfRangeException>("probe/nesting-past-maximum", () => BroilerRegex.Compile(c.Pattern, c.Flags, UnicodeTables.Instance, BroilerRegex.MaximumNestingLimit + 1, null, null, out _, out _), outcome);
        Expect<ArgumentOutOfRangeException>("probe/budget-zero", () => BroilerRegex.Compile(c.Pattern, c.Flags, UnicodeTables.Instance, 1, 0, null, out _, out _), outcome);
        Expect<ArgumentNullException>("probe/pattern-null", () => BroilerRegex.Compile(null!, c.Flags, UnicodeTables.Instance, 1, null, null, out _, out _), outcome);
        Expect<ArgumentOutOfRangeException>("probe/start-negative", () => regex.Match(subject.AsSpan(), -1, false, slots, null, ceilings, out _), outcome);
        Expect<ArgumentOutOfRangeException>("probe/start-past-end", () => regex.Match(subject.AsSpan(), subject.Length + 1, false, slots, null, ceilings, out _), outcome);
        Expect<ArgumentException>("probe/slots-short", () => regex.Match(subject.AsSpan(), 0, false, slots.AsSpan(0, count - 1), null, ceilings, out _), outcome);
        Expect<ArgumentException>("probe/ceilings-default", () => regex.Match(subject.AsSpan(), 0, false, slots, null, default, out _), outcome);
        Expect<ArgumentOutOfRangeException>("probe/steps-zero", () => new RegexMatchCeilings(0, 1, 1), outcome);
        Expect<ArgumentOutOfRangeException>("probe/frames-past-maximum", () => new RegexMatchCeilings(null, RegexMatchCeilings.FrameCeilingMaximum + 1, 1), outcome);
        Expect<ArgumentOutOfRangeException>("probe/trail-zero", () => new RegexMatchCeilings(null, 1, 0), outcome);
        outcome.Reach("probes");
    }

    private static void Expect<T>(string site, Func<object> call, Outcome outcome)
        where T : ArgumentException
    {
        try
        {
            call();
            outcome.Find("precondition", site, "no exception for a broken precondition; " + typeof(T).Name + " is documented");
        }
        catch (T)
        {
            outcome.Note(site, "ok");
        }
        catch (Exception failure)
        {
            outcome.Find("precondition", site, typeof(T).Name + " is documented; " + Describe(failure));
        }
    }

    // ----- Helpers ---------------------------------------------------------------------------------------------------

    private static RegexMatch? Call(Func<RegexMatch> call, string site, Outcome outcome)
    {
        try
        {
            return call();
        }
        catch (Exception failure)
        {
            outcome.Find("exception", site, Describe(failure));
            return null;
        }
    }

    private static string Site(string entry, int start, bool anchored) => Invariant($"{entry}@{start}{(anchored ? "/anchored" : "")}");

    private static string SlotsText(ReadOnlySpan<int> slots)
    {
        if (slots.Length <= 16)
        {
            var text = new StringBuilder();
            for (var i = 0; i < slots.Length; i++)
                text.Append(i == 0 ? "" : i % 2 == 0 ? "|" : ",").Append(slots[i].ToString(CultureInfo.InvariantCulture));
            return text.ToString();
        }

        var bytes = new byte[slots.Length * 4];
        for (var i = 0; i < slots.Length; i++)
            BitConverter.TryWriteBytes(bytes.AsSpan(i * 4), slots[i]);
        return Invariant($"{slots[0]},{slots[1]}+{slots.Length - 2}#{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))[..12]}");
    }

    /// <summary>
    /// An exception as a finding states it: its type, its message, and the library's frames on its stack, innermost
    /// first, as <c>at Type.Method (File.cs:line)</c> with the compiler's names for local functions and lambdas read as
    /// their method's, and no directory, so that a finding reads the same on every machine.
    /// </summary>
    internal static string Describe(Exception failure)
    {
        var frames = Frames(failure).Take(3).ToList();
        return failure.GetType().FullName + ": " + failure.Message.Replace('\n', ' ').Replace('\r', ' ')
            + (frames.Count > 0 ? " at " + string.Join(" < ", frames) : "");
    }

    /// <summary>The library's methods on an exception's stack, innermost first, each with its file and line where the stack has them.</summary>
    internal static IEnumerable<string> Frames(Exception failure)
    {
        foreach (var raw in (failure.StackTrace ?? "").Split('\n'))
        {
            var line = raw.Trim();
            if (!line.StartsWith("at Broiler.Regex.", StringComparison.Ordinal) || line.StartsWith("at Broiler.Regex.Fuzz.", StringComparison.Ordinal))
                continue;
            var call = line[3..];
            var open = call.IndexOf('(', StringComparison.Ordinal);
            var method = KnownFindings.Method(open < 0 ? call : call[..open]);
            var file = call.LastIndexOf(" in ", StringComparison.Ordinal) is var at and >= 0 ? Path.GetFileName(call[(at + 4)..].Replace(":line ", ":", StringComparison.Ordinal)) : "";
            yield return method + (file.Length > 0 ? " (" + file + ")" : "");
        }
    }

    /// <summary>A stable 64-bit FNV-1a of a text, in hexadecimal: the case's and a message's mark in the transcript.</summary>
    internal static string Hash(string text)
    {
        var hash = 0xCBF29CE484222325UL;
        foreach (var c in text)
        {
            hash = (hash ^ (byte)c) * 0x100000001B3UL;
            hash = (hash ^ (byte)(c >> 8)) * 0x100000001B3UL;
        }

        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
