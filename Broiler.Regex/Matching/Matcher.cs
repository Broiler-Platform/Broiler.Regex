using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Broiler.Regex.Ast;
using Broiler.Regex.Unicode;

namespace Broiler.Regex.Matching;

/// <summary>
/// Compiles a <see cref="RegexNode"/> tree to the ECMAScript matcher abstraction
/// (ECMA-262 §22.2.2) and runs it. A <see cref="Matcher"/> is a
/// <c>(State, Continuation) → State?</c> function; backtracking is expressed by
/// trying a continuation and, on failure (<c>null</c>), trying the next branch.
///
/// This is where the JS/.NET semantic gaps are resolved by construction:
/// <list type="bullet">
/// <item>look-behind compiles its body with <see cref="Direction"/> = −1, matching
/// backward while capturing in source order;</item>
/// <item><see cref="CompileQuantifier"/> implements RepeatMatcher's empty-iteration
/// guard;</item>
/// <item>atoms and back-references are code-point aware under <c>u</c>/<c>v</c>.</item>
/// </list>
/// </summary>
internal sealed class Matcher
{
    private delegate MatchState? Continuation(MatchState state);
    private static readonly Continuation IdentityContinuation = static s => s;
    private delegate MatchState? CompiledMatcher(MatchState state, Continuation cont);

    /// <summary>Matching direction: +1 forward, −1 backward (inside a look-behind).</summary>
    private enum Direction { Forward = 1, Backward = -1 }

    private readonly CompiledMatcher _root;
    private readonly int _captureCount;
    private readonly bool _unicode;
    private readonly bool _sticky;
    private readonly IReadOnlyDictionary<string, int> _groupNames;
    private const long StepLimit = 10_000_000;

    /// <summary>Effective i/m/s flags at a point in compilation (mutated by modifier groups).</summary>
    private readonly struct Flags(bool ignoreCase, bool multiline, bool dotAll)
    {
        public readonly bool IgnoreCase = ignoreCase;
        public readonly bool Multiline = multiline;
        public readonly bool DotAll = dotAll;

        public Flags With(RegexFlags add, RegexFlags remove) => new(
            (IgnoreCase || (add & RegexFlags.IgnoreCase) != 0) && (remove & RegexFlags.IgnoreCase) == 0,
            (Multiline || (add & RegexFlags.Multiline) != 0) && (remove & RegexFlags.Multiline) == 0,
            (DotAll || (add & RegexFlags.DotAll) != 0) && (remove & RegexFlags.DotAll) == 0);
    }

    public Matcher(RegexNode root, int captureCount, RegexFlags flags,
        IReadOnlyDictionary<string, int> groupNames)
    {
        _captureCount = captureCount;
        _unicode = flags.IsUnicodeMode();
        _sticky = (flags & RegexFlags.Sticky) != 0;
        _groupNames = groupNames;

        var initial = new Flags(
            (flags & RegexFlags.IgnoreCase) != 0,
            (flags & RegexFlags.Multiline) != 0,
            (flags & RegexFlags.DotAll) != 0);
        _root = Compile(root, Direction.Forward, initial);
    }

    /// <summary>
    /// Attempts a match in <paramref name="input"/> at or after
    /// <paramref name="start"/>. Returns null on no match. (Sticky anchors at
    /// exactly <paramref name="start"/>.)
    /// </summary>
    public RegexMatch? Run(string input, int start)
    {
        // One budget and one initial-captures array serve every start position. Matching
        // never writes the initial array in place (WithCapture/WithResetCaptures copy on
        // write), so it only has to be re-filled with "unset" before each attempt, and the
        // budget is reset to a fresh per-start allowance — identical semantics to a new
        // instance each time, but without the per-start allocation a pattern that fails near
        // the start would otherwise pay while retrying at 1, 2, 3, ….
        var budget = new Budget(StepLimit);
        var captures = NewCaptures();
        for (var at = start; at <= input.Length; at++)
        {
            budget.Reset();
            Array.Fill(captures, -1);
            var state = new MatchState(input, at, captures, budget);
            var result = _root(state, IdentityContinuation);
            if (result != null)
                return BuildMatch(input, at, result.Value);

            if (_sticky)
                break;

            // Advance by a whole code point in Unicode mode so the next start does
            // not land between the halves of a surrogate pair.
            if (_unicode && at < input.Length && char.IsHighSurrogate(input[at])
                && at + 1 < input.Length && char.IsLowSurrogate(input[at + 1]))
                at++;
        }

        return null;
    }

    private int[] NewCaptures()
    {
        var captures = new int[2 * (_captureCount + 1)];
        Array.Fill(captures, -1);
        return captures;
    }

    private RegexMatch BuildMatch(string input, int start, MatchState end)
    {
        var matchEnd = end.Position;
        var value = input[start..matchEnd];

        var groups = new RegexGroup[_captureCount + 1];
        groups[0] = new RegexGroup(true, start, matchEnd - start, value, null);

        var indexToName = new Dictionary<int, string>();
        foreach (var (name, index) in _groupNames)
            indexToName[index] = name;

        var named = new Dictionary<string, RegexGroup>(StringComparer.Ordinal);
        for (var i = 1; i <= _captureCount; i++)
        {
            indexToName.TryGetValue(i, out var name);
            if (end.TryGetCapture(i, out var s, out var e) && e >= s)
            {
                var g = new RegexGroup(true, s, e - s, input[s..e], name);
                groups[i] = g;
                if (name != null) named[name] = g;
            }
            else
            {
                groups[i] = RegexGroup.Unmatched(name);
                if (name != null) named[name] = groups[i];
            }
        }

        return new RegexMatch(true, start, matchEnd - start, value, groups, named);
    }

    // ----- Compilation --------------------------------------------------------

    private CompiledMatcher Compile(RegexNode node, Direction dir, Flags flags) => node switch
    {
        EmptyNode => (s, c) => c(s),
        CharNode ch => CompileChar(ch.CodePoint, dir, flags),
        AnyCharNode => CompileAnyChar(dir, flags),
        CharClassNode cls => CompileCharClass(cls.Set, dir, flags),
        SequenceNode seq => CompileSequence(seq.Terms, dir, flags),
        DisjunctionNode dis => CompileDisjunction(dis.Alternatives, dir, flags),
        GroupNode grp => CompileGroup(grp, dir, flags),
        ModifierGroupNode mod => Compile(mod.Child, dir, flags.With(mod.Added, mod.Removed)),
        QuantifierNode q => CompileQuantifier(q, dir, flags),
        BackreferenceNode br => CompileBackreference(br, dir, flags),
        AnchorNode a => CompileAnchor(a.Kind, flags),
        LookaroundNode la => CompileLookaround(la, flags),
        _ => throw new InvalidOperationException($"Unhandled node {node.GetType().Name}"),
    };

    private CompiledMatcher CompileSequence(IReadOnlyList<RegexNode> terms, Direction dir, Flags flags)
    {
        // Fold a maximal run of two or more single-code-point atoms (a literal substring,
        // an atom run like `\d\d/`) into one deterministic matcher that checks them in a
        // loop — no per-term continuation closure and no per-character state, where the CPS
        // chain would allocate one closure per term on every invocation. Forward only:
        // look-behind reverses term order and is rare, so it keeps the simple chain.
        var built = new List<CompiledMatcher>(terms.Count);
        var t = 0;
        while (t < terms.Count)
        {
            if (dir == Direction.Forward && TryGetSingleCharPredicate(terms[t], flags, out var p0))
            {
                var run = new List<CharPredicate> { p0 };
                var j = t + 1;
                while (j < terms.Count && TryGetSingleCharPredicate(terms[j], flags, out var pj))
                {
                    run.Add(pj);
                    j++;
                }
                if (run.Count == 1)
                    built.Add(Compile(terms[t], dir, flags));
                else
                    built.Add(CompileAtomRun([.. run]));
                t = j;
            }
            else
            {
                built.Add(Compile(terms[t], dir, flags));
                t++;
            }
        }

        var matchers = built.ToArray();

        // Forward: apply left-to-right. Backward (look-behind): apply right-to-left
        // — the spec composes Alternative terms in reverse under direction −1.
        var order = dir == Direction.Forward
            ? matchers
            : Reverse(matchers);

        return (s, c) =>
        {
            Continuation k = c;
            for (var i = order.Length - 1; i >= 0; i--)
            {
                var m = order[i];
                var next = k;
                k = state => m(state, next);
            }
            return k(s);
        };
    }

    /// <summary>
    /// A forward run of single-code-point atoms matched in one loop: each consumes exactly
    /// one code point deterministically, so the whole run either advances the cursor past
    /// all of them and hands off to the continuation once, or fails — with no per-atom
    /// closure and no intermediate <see cref="MatchState"/>.
    /// </summary>
    private CompiledMatcher CompileAtomRun(CharPredicate[] predicates)
    {
        return (s, c) =>
        {
            var input = s.Input;
            var pos = s.Position;
            foreach (var predicate in predicates)
            {
                if (pos >= input.Length)
                    return null;
                var (cp, width) = CodePointAt(input, pos);
                if (!predicate(cp))
                    return null;
                pos += width;
            }
            return c(s.WithPosition(pos));
        };
    }

    private static CompiledMatcher[] Reverse(CompiledMatcher[] source)
    {
        var copy = new CompiledMatcher[source.Length];
        for (var i = 0; i < source.Length; i++)
            copy[i] = source[source.Length - 1 - i];
        return copy;
    }

    private CompiledMatcher CompileDisjunction(IReadOnlyList<RegexNode> alts, Direction dir, Flags flags)
    {
        var matchers = new CompiledMatcher[alts.Count];
        for (var i = 0; i < alts.Count; i++)
            matchers[i] = Compile(alts[i], dir, flags);

        return (s, c) =>
        {
            foreach (var m in matchers)
            {
                var r = m(s, c);
                if (r != null)
                    return r;
            }
            return null;
        };
    }

    private CompiledMatcher CompileGroup(GroupNode group, Direction dir, Flags flags)
    {
        var inner = Compile(group.Child, dir, flags);
        if (!group.IsCapturing)
            return inner;

        var index = group.CaptureIndex;
        return (s, c) =>
        {
            var entry = s.Position;
            return inner(s, y =>
            {
                // Capture span is [min(entry,exit), max(entry,exit)] regardless of
                // direction, so look-behind captures read left-to-right (fixes #3/#4).
                var exit = y.Position;
                var lo = dir == Direction.Forward ? entry : exit;
                var hi = dir == Direction.Forward ? exit : entry;
                return c(y.WithCapture(index, lo, hi));
            });
        };
    }

    private CompiledMatcher CompileQuantifier(QuantifierNode q, Direction dir, Flags flags)
    {
        var min = q.Min;
        var max = q.Max;
        var greedy = q.Greedy;

        // Iterative fast path: a body that consumes exactly one code point, sets no
        // captures, and matches at most one way at each position — a literal char, `.`,
        // or a character class — repeats deterministically, so its repetition is a linear
        // scan with an index-based backtrack. This is the common quantifier (`.*`, `\d+`,
        // `[^"]*`); it avoids the general driver's per-match re-execution entirely.
        if (TryGetSingleCharPredicate(q.Child, flags, out var predicate))
            return CompileSingleCharQuantifier(predicate, min, max, greedy, dir);

        var inner = Compile(q.Child, dir, flags);
        var capIndices = CollectCaptureIndices(q.Child);
        return CompileGeneralQuantifier(inner, capIndices, min, max, greedy);
    }

    /// <summary>
    /// One active iteration level of the iterative RepeatMatcher: the state at entry, the
    /// remaining min/max, and how far its body-match enumeration and its "stop" branch have
    /// been explored. Held on an explicit heap stack so the iteration dimension — the one
    /// that grows with the subject length — no longer consumes a native stack frame apiece.
    /// </summary>
    private sealed class RepeatFrame
    {
        public int RemMin;
        public int RemMax;
        public MatchState X;      // state at this iteration level
        public MatchState Xr;     // X with the quantifier's captures reset (body start)
        public bool XrReady;
        public int Skip;          // index of the next body match to fetch
        public bool BodyExhausted;
        public bool StopTried;    // whether the c(X) "stop" branch has been taken
    }

    /// <summary>
    /// RepeatMatcher (§22.2.2.3.1) for an arbitrary body, driven by an explicit heap stack
    /// instead of recursion so a repeat over a long subject cannot overflow the native
    /// stack — the payoff being that any quantifier, not just a single-code-point one,
    /// matches a subject of any length natively. Body matches at each level are enumerated
    /// lazily in the body's own backtracking order (see <see cref="TryMatchBodyNth"/>);
    /// greedy tries every body-match subtree before the "stop" branch, lazy the reverse,
    /// and the empty-iteration guard abandons a min=0 iteration that consumed nothing (the
    /// nullable-quantifier fix, #8). This is exactly what the recursive form computed; only
    /// the iteration dimension moved off the call stack.
    /// </summary>
    private CompiledMatcher CompileGeneralQuantifier(CompiledMatcher inner, int[] capIndices, int min, int max, bool greedy)
    {
        return (s, c) =>
        {
            // The one place native recursion can still deepen is a body (`inner`) or a
            // continuation (`c`) reached through nested pattern structure — bounded by the
            // pattern's nesting depth, exactly like the recursive-descent parser that
            // already accepted this pattern, never by the subject length. Check once per
            // driver entry so a pathologically nested pattern degrades to a signal (which
            // the caller turns into a .NET fallback) rather than crashing the process.
            if (!System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack())
                throw new RegexOverflowException();

            var stack = new Stack<RepeatFrame>();
            stack.Push(new RepeatFrame { RemMin = min, RemMax = max, X = s });

            while (stack.Count > 0)
            {
                var f = stack.Peek();

                // Lazy: the "stop" branch (match zero more, hand off to c) comes first.
                if (!greedy && !f.StopTried)
                {
                    f.StopTried = true;
                    if (f.RemMin == 0)
                    {
                        if (!s.Budget.Step())
                            return null;
                        var r = c(f.X);
                        if (r != null)
                            return r;
                    }
                }

                // Body branch: try to match the body once more and descend a level.
                if (f.RemMax != 0 && !f.BodyExhausted)
                {
                    if (!f.XrReady)
                    {
                        f.Xr = f.X.WithResetCaptures(capIndices);
                        f.XrReady = true;
                    }
                    if (!s.Budget.Step())
                        return null;
                    if (TryMatchBodyNth(inner, f.Xr, f.Skip, out var y))
                    {
                        f.Skip++;
                        // Empty-iteration guard: abandon a min=0 iteration that consumed
                        // nothing and try the next body match instead (as `d` returning
                        // failure would make the recursive form backtrack).
                        if (f.RemMin == 0 && y.Position == f.X.Position)
                            continue;
                        var nextMin = f.RemMin == 0 ? 0 : f.RemMin - 1;
                        var nextMax = f.RemMax == QuantifierNode.Unbounded ? QuantifierNode.Unbounded : f.RemMax - 1;
                        stack.Push(new RepeatFrame { RemMin = nextMin, RemMax = nextMax, X = y });
                        continue;
                    }
                    f.BodyExhausted = true;
                }

                // Greedy: the "stop" branch comes after every body-match subtree failed.
                if (greedy && !f.StopTried)
                {
                    f.StopTried = true;
                    if (f.RemMin == 0)
                    {
                        if (!s.Budget.Step())
                            return null;
                        var r = c(f.X);
                        if (r != null)
                            return r;
                    }
                }

                // This level is exhausted; drop it and let the parent try its next body match.
                stack.Pop();
            }

            return null;
        };
    }

    /// <summary>
    /// Yields the <paramref name="skip"/>-th body match (0-based) that <paramref name="inner"/>
    /// produces starting at <paramref name="xr"/>, in the body's natural backtracking order,
    /// or returns false when the body has no more matches. Works by running the body with a
    /// continuation that fails the first <paramref name="skip"/> times — forcing the
    /// backtracker on to its next alternative — and succeeds on the next, capturing that
    /// state. The body's own matching still recurses, but only to the (pattern-bounded) depth
    /// of the body's structure, never to the subject length.
    /// </summary>
    private static bool TryMatchBodyNth(CompiledMatcher inner, MatchState xr, int skip, out MatchState result)
    {
        MatchState? captured = null;
        var seen = 0;
        MatchState? Counting(MatchState y)
        {
            if (seen == skip)
            {
                captured = y;
                return y; // stop backtracking: this is the match we want
            }
            seen++;
            return null; // fail so the body backtracks to its next match
        }

        var r = inner(xr, Counting);
        result = captured ?? default;
        return r != null;
    }

    /// <summary>A single-code-point membership test with the atom's flags baked in.</summary>
    private delegate bool CharPredicate(int codePoint);

    /// <summary>
    /// Recognises a quantifier body that consumes exactly one code point, sets no
    /// captures, and matches at most one way per position — a literal char, <c>.</c>,
    /// or a character class without <c>\q{…}</c> string members — and, when so, hands
    /// back a predicate identical to the atom's own matcher. Such a body cannot match
    /// the empty string and has no internal backtracking, so <see cref="CompileSingleCharQuantifier"/>
    /// can iterate it instead of recursing. A class carrying string members is not
    /// eligible: it is an alternation, not a single-character matcher.
    /// </summary>
    private bool TryGetSingleCharPredicate(RegexNode child, Flags flags, [NotNullWhen(true)] out CharPredicate? predicate)
    {
        var ignoreCase = flags.IgnoreCase;
        var unicode = _unicode;
        switch (child)
        {
            case CharNode ch:
                var target = ch.CodePoint;
                predicate = cp => CaseFolding.Equal(cp, target, ignoreCase, unicode);
                return true;
            case AnyCharNode:
                var dotAll = flags.DotAll;
                predicate = cp => dotAll || !IsLineTerminator(cp);
                return true;
            case CharClassNode cls when !cls.Set.HasStrings:
                var set = cls.Set;
                predicate = cp => set.Contains(cp, ignoreCase, unicode);
                return true;
            default:
                predicate = null;
                return false;
        }
    }

    /// <summary>
    /// Iterative, allocation-free RepeatMatcher for a single-code-point body. A greedy
    /// quantifier scans as far as the body matches, then hands cursors to the continuation
    /// furthest-first, recomputing each earlier cursor by stepping back exactly one code
    /// point (the body consumed one per iteration); a lazy one tries the continuation with
    /// the fewest iterations first, matching one more code point each time it fails. This
    /// is what the recursive <c>Repeat</c> computed for such a body, but with no per-iteration
    /// heap — neither a stack frame nor a positions list — so a repeat over a long subject
    /// stays flat and cheap.
    /// </summary>
    private CompiledMatcher CompileSingleCharQuantifier(CharPredicate predicate, int min, int max, bool greedy, Direction dir)
    {
        if (greedy)
        {
            return (s, c) =>
            {
                var input = s.Input;
                var pos = s.Position;
                var count = 0;
                while (max == QuantifierNode.Unbounded || count < max)
                {
                    // Keep the shared catastrophic-backtracking budget honest: each
                    // iteration is one step, as it is in the recursive Repeat.
                    if (!s.Budget.Step())
                        break;
                    if (!TryReadMatching(input, dir, pos, predicate, out var nextPos))
                        break;
                    pos = nextPos;
                    count++;
                }

                if (count < min)
                    return null;

                for (var k = count; ; k--)
                {
                    var r = c(s.WithPosition(pos));
                    if (r != null)
                        return r;
                    if (k == min)
                        return null;
                    pos = StepBack(input, dir, pos);
                }
            };
        }

        return (s, c) =>
        {
            var input = s.Input;
            var pos = s.Position;
            var count = 0;

            // The mandatory iterations must all match, or the quantifier fails outright.
            while (count < min)
            {
                if (!s.Budget.Step())
                    return null;
                if (!TryReadMatching(input, dir, pos, predicate, out var nextPos))
                    return null;
                pos = nextPos;
                count++;
            }

            while (true)
            {
                var r = c(s.WithPosition(pos));
                if (r != null)
                    return r;
                if (max != QuantifierNode.Unbounded && count == max)
                    return null;
                if (!s.Budget.Step())
                    return null;
                if (!TryReadMatching(input, dir, pos, predicate, out var nextPos))
                    return null;
                pos = nextPos;
                count++;
            }
        };
    }

    /// <summary>Reverses one single-code-point iteration: the cursor before the code point
    /// the body consumed at <paramref name="pos"/> in <paramref name="dir"/>.</summary>
    private int StepBack(string input, Direction dir, int pos)
        => dir == Direction.Forward
            ? pos - CodePointBefore(input, pos).width
            : pos + CodePointAt(input, pos).width;

    /// <summary>
    /// Reads one code point at <paramref name="pos"/> in <paramref name="dir"/> and, if it
    /// satisfies <paramref name="predicate"/>, yields the cursor past it. Mirrors
    /// <see cref="ReadCodePoint"/> but works from an explicit position so the iterative
    /// quantifier need not thread a <see cref="MatchState"/> through the scan.
    /// </summary>
    private bool TryReadMatching(string input, Direction dir, int pos, CharPredicate predicate, out int nextPos)
    {
        if (dir == Direction.Forward)
        {
            if (pos >= input.Length)
            {
                nextPos = pos;
                return false;
            }
            var (cp, width) = CodePointAt(input, pos);
            if (!predicate(cp))
            {
                nextPos = pos;
                return false;
            }
            nextPos = pos + width;
            return true;
        }
        else
        {
            if (pos <= 0)
            {
                nextPos = pos;
                return false;
            }
            var (cp, width) = CodePointBefore(input, pos);
            if (!predicate(cp))
            {
                nextPos = pos;
                return false;
            }
            nextPos = pos - width;
            return true;
        }
    }

    private CompiledMatcher CompileBackreference(BackreferenceNode br, Direction dir, Flags flags)
    {
        var index = br.Name != null
            ? (_groupNames.TryGetValue(br.Name, out var i) ? i : 0)
            : br.Index;

        if (index <= 0 || index > _captureCount)
        {
            if (br.Name != null)
                throw new RegexSyntaxException($"Reference to non-existent group '{br.Name}'");
            // A numeric reference past the group count matches the empty string.
            return (s, c) => c(s);
        }

        var ignoreCase = flags.IgnoreCase;
        var unicode = _unicode;

        return (s, c) =>
        {
            if (!s.TryGetCapture(index, out var cs, out var ce))
                return c(s); // an unmatched group's back-reference matches empty
            var len = ce - cs;
            if (len == 0)
                return c(s);

            int from, to;
            if (dir == Direction.Forward)
            {
                from = s.Position;
                to = from + len;
                if (to > s.Input.Length)
                    return null;
            }
            else
            {
                to = s.Position;
                from = to - len;
                if (from < 0)
                    return null;
            }

            if (!RegionEquals(s.Input, from, s.Input, cs, len, ignoreCase, unicode))
                return null;

            return c(s.WithPosition(dir == Direction.Forward ? to : from));
        };
    }

    private CompiledMatcher CompileAnchor(AnchorKind kind, Flags flags)
    {
        var multiline = flags.Multiline;
        return kind switch
        {
            AnchorKind.StartOfInput => (s, c) =>
                (s.Position == 0 || (multiline && IsLineTerminator(CodePointBefore(s.Input, s.Position).cp)))
                    ? c(s) : null,
            AnchorKind.EndOfInput => (s, c) =>
                (s.Position == s.Input.Length || (multiline && IsLineTerminator(CodePointAt(s.Input, s.Position).cp)))
                    ? c(s) : null,
            AnchorKind.WordBoundary => (s, c) => IsWordBoundary(s, flags) ? c(s) : null,
            AnchorKind.NonWordBoundary => (s, c) => !IsWordBoundary(s, flags) ? c(s) : null,
            _ => throw new InvalidOperationException(),
        };
    }

    private CompiledMatcher CompileLookaround(LookaroundNode la, Flags flags)
    {
        var dir = la.Behind ? Direction.Backward : Direction.Forward;
        var inner = Compile(la.Child, dir, flags);
        var negative = la.Negative;

        return (s, c) =>
        {
            var matched = inner(s, y => y);
            if (negative)
                return matched != null ? null : c(s);
            if (matched == null)
                return null;
            // Positive look-around keeps the captures it set, but restores position.
            return c(new MatchState(s.Input, s.Position, matched.Value.Captures, s.Budget));
        };
    }

    // ----- Atom matchers ------------------------------------------------------

    private CompiledMatcher CompileChar(int codePoint, Direction dir, Flags flags)
    {
        var ignoreCase = flags.IgnoreCase;
        var unicode = _unicode;
        return (s, c) =>
        {
            if (!ReadCodePoint(s, dir, out var actual, out var nextPos))
                return null;
            return CaseFolding.Equal(actual, codePoint, ignoreCase, unicode)
                ? c(s.WithPosition(nextPos)) : null;
        };
    }

    private CompiledMatcher CompileAnyChar(Direction dir, Flags flags)
    {
        var dotAll = flags.DotAll;
        return (s, c) =>
        {
            if (!ReadCodePoint(s, dir, out var actual, out var nextPos))
                return null;
            if (!dotAll && IsLineTerminator(actual))
                return null;
            return c(s.WithPosition(nextPos));
        };
    }

    private CompiledMatcher CompileCharClass(CharSet set, Direction dir, Flags flags)
    {
        var ignoreCase = flags.IgnoreCase;
        var unicode = _unicode;

        if (set.HasStrings)
            return CompileCharClassWithStrings(set, dir, ignoreCase, unicode);

        return (s, c) =>
        {
            if (!ReadCodePoint(s, dir, out var actual, out var nextPos))
                return null;
            return set.Contains(actual, ignoreCase, unicode)
                ? c(s.WithPosition(nextPos)) : null;
        };
    }

    /// <summary>
    /// A <c>v</c>-mode class holding multi-code-point members (<c>\q{…}</c> or a property
    /// of strings) is the one CharSet that is not a single-character matcher: §22.2.2.9
    /// compiles it to the alternation of its strings followed by its code points, longest
    /// alternative first, and each alternative is a backtracking point of its own.
    /// </summary>
    private CompiledMatcher CompileCharClassWithStrings(CharSet set, Direction dir, bool ignoreCase, bool unicode)
    {
        var strings = new string[set.Strings.Count];
        for (var i = 0; i < strings.Length; i++)
            strings[i] = set.Strings[i];

        return (s, c) =>
        {
            foreach (var candidate in strings)
            {
                if (!TryMatchLiteral(s, dir, candidate, ignoreCase, unicode, out var after))
                    continue;
                var result = c(s.WithPosition(after));
                if (result != null)
                    return result;
            }

            if (!ReadCodePoint(s, dir, out var actual, out var nextPos))
                return null;
            return set.Contains(actual, ignoreCase, unicode)
                ? c(s.WithPosition(nextPos)) : null;
        };
    }

    /// <summary>Matches a literal string at the cursor in <paramref name="dir"/>.</summary>
    private static bool TryMatchLiteral(MatchState s, Direction dir, string literal,
        bool ignoreCase, bool unicode, out int after)
    {
        int from;
        if (dir == Direction.Forward)
        {
            from = s.Position;
            after = from + literal.Length;
            if (after > s.Input.Length)
                return false;
        }
        else
        {
            after = s.Position - literal.Length;
            from = after;
            if (from < 0)
                return false;
        }

        // A `v`-mode class folded its string members at parse time and the fold is
        // idempotent, so canonicalizing both sides here still compares the subject
        // against the folded member.
        return RegionEquals(s.Input, from, literal, 0, literal.Length, ignoreCase, unicode);
    }

    /// <summary>Reads the code point in <paramref name="dir"/>, yielding the next position.</summary>
    private bool ReadCodePoint(MatchState s, Direction dir, out int codePoint, out int nextPos)
    {
        if (dir == Direction.Forward)
        {
            if (s.Position >= s.Input.Length)
            {
                codePoint = -1; nextPos = s.Position; return false;
            }
            var (cp, width) = CodePointAt(s.Input, s.Position);
            codePoint = cp;
            nextPos = s.Position + width;
            return true;
        }
        else
        {
            if (s.Position <= 0)
            {
                codePoint = -1; nextPos = s.Position; return false;
            }
            var (cp, width) = CodePointBefore(s.Input, s.Position);
            codePoint = cp;
            nextPos = s.Position - width;
            return true;
        }
    }

    // ----- Code-point / character helpers ------------------------------------

    private (int cp, int width) CodePointAt(string input, int pos)
        => ReadAt(input, pos, _unicode);

    private (int cp, int width) CodePointBefore(string input, int pos)
    {
        if (pos <= 0)
            return (-1, 0);
        var c = input[pos - 1];
        if (_unicode && char.IsLowSurrogate(c) && pos - 2 >= 0 && char.IsHighSurrogate(input[pos - 2]))
            return (char.ConvertToUtf32(input[pos - 2], c), 2);
        return (c, 1);
    }

    private static bool IsLineTerminator(int cp)
        => cp is 0x000A or 0x000D or 0x2028 or 0x2029;

    private bool IsWordBoundary(MatchState s, Flags flags)
    {
        var before = s.Position > 0 && IsWordChar(CodePointBefore(s.Input, s.Position).cp, flags);
        var after = s.Position < s.Input.Length && IsWordChar(CodePointAt(s.Input, s.Position).cp, flags);
        return before != after;
    }

    /// <summary>
    /// §22.2.2.5 IsWordChar reads WordCharacters, which under <c>iu</c> also holds every
    /// code point that folds into the basic set — so <c>ſ</c> and <c>K</c> are word
    /// characters for <c>\b</c> exactly when they are members of <c>\w</c>.
    /// </summary>
    private bool IsWordChar(int cp, Flags flags) => UnicodeCharSets.IsWord(cp, flags.IgnoreCase, _unicode);

    /// <summary>Code-point-aware, case-fold-aware comparison of two equal-length regions.</summary>
    private static bool RegionEquals(string a, int aStart, string b, int bStart, int len,
        bool ignoreCase, bool unicode)
    {
        int ai = aStart, bi = bStart;
        var aEnd = aStart + len;
        while (ai < aEnd)
        {
            var (acp, aw) = ReadAt(a, ai, unicode);
            var (bcp, bw) = ReadAt(b, bi, unicode);
            if (!CaseFolding.Equal(acp, bcp, ignoreCase, unicode))
                return false;
            ai += aw;
            bi += bw;
        }
        return ai == aEnd;
    }

    private static (int cp, int width) ReadAt(string input, int pos, bool unicode)
    {
        var c = input[pos];
        if (unicode && char.IsHighSurrogate(c) && pos + 1 < input.Length && char.IsLowSurrogate(input[pos + 1]))
            return (char.ConvertToUtf32(c, input[pos + 1]), 2);
        return (c, 1);
    }

    /// <summary>Collects the 1-based capture indices contained in a subtree.</summary>
    private static int[] CollectCaptureIndices(RegexNode node)
    {
        var list = new List<int>();
        void Walk(RegexNode n)
        {
            switch (n)
            {
                case GroupNode g:
                    if (g.IsCapturing) list.Add(g.CaptureIndex);
                    Walk(g.Child);
                    break;
                case ModifierGroupNode m: Walk(m.Child); break;
                case QuantifierNode q: Walk(q.Child); break;
                case LookaroundNode la: Walk(la.Child); break;
                case SequenceNode seq:
                    foreach (var t in seq.Terms) Walk(t);
                    break;
                case DisjunctionNode d:
                    foreach (var a in d.Alternatives) Walk(a);
                    break;
            }
        }
        Walk(node);
        return [.. list];
    }
}
