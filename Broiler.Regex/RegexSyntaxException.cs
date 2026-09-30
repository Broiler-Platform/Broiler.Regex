// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           1
// Human-reviewed:   0/1
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       1
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.Regex;

/// <summary>
/// Thrown when a pattern or flags string is not a valid ECMAScript regular
/// expression. The JavaScript layer maps this to a <c>SyntaxError</c>.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=0; Fingerprint=B41B30
// Broiler-Human:        PENDING
public sealed class RegexSyntaxException(string message, int position = -1) : Exception(message)
{
    /// <summary>Zero-based offset into the pattern where the error was detected, or -1.</summary>
    public int Position { get; } = position;
}
