using System;

namespace Broiler.Regex;

/// <summary>
/// Thrown by the matcher when a match would recurse deeper than the runtime stack can
/// hold. The continuation-passing matcher recurses once per quantifier iteration
/// (§22.2.2.3.1 RepeatMatcher), so a repeat over a long subject can exhaust the stack.
/// Rather than crash the process with an uncatchable <see cref="StackOverflowException"/>,
/// the matcher checks the remaining stack before it recurses and throws this instead, which
/// unwinds the continuation chain cleanly. A host may catch it and fall back to another
/// engine for that subject.
/// </summary>
/// <remarks>
/// This is distinct from the step budget (<see cref="Matching.Budget"/>), which bounds
/// total work against catastrophic backtracking and reports exhaustion as no-match. The
/// stack guard bounds recursion <i>depth</i> — a different resource — and signals rather
/// than silently returning, because the correct answer for a deep-but-valid match still
/// exists and can be produced by an iterative engine.
/// </remarks>
public sealed class RegexOverflowException : Exception
{
    public RegexOverflowException()
        : base("Regular-expression match exceeded the available stack depth.")
    {
    }
}
