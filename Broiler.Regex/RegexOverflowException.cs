// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           0
// Human-reviewed:   0/2
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

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
// Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=C1DF8E
// Broiler-Human:        PENDING
public sealed class RegexOverflowException : Exception
{
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=A639B9
    // Broiler-Human:        PENDING
    public RegexOverflowException()
        : base("Regular-expression match exceeded the available stack depth.")
    {
    }
}
