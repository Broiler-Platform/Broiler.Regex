// Arm V of bundle sjl-11-001's reference-arm comparison (docs/evidence/sjl-11-001/reference-arms.md): a test-only copy.
// Source:  Broiler.VM, src/Broiler.VM.Profile.JavaScript.Format/JsUnicodeData.cs at aaa39fb, git blob
//          b4646317dbc07ea7365a91b03ab1cfb3bc20472d, Apache-2.0, Broiler Platform contributors.
// Members: lines 1-21 (the header, the usings and the namespace) and 77-116, JsFormatUnicode, the owner's algorithms
//          the matcher reads; a blank line is put between the two.
// Changes: one, marked ARM CHANGE: the provider is Broiler.Unicode.Tables' UnicodeTables.Instance (test-only data, the
//          same UCD 17.0.0 as the tables Broiler.VM embeds), where Broiler.VM reads its own embedded tables through
//          JsFormatUnicodeTables (lines 61-75), which is not copied, with JsUnicodeData (lines 22-59); and `using
//          Broiler.Unicode.Tables;` is added for it.

// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   11
// Annotated:        11/11
// Exempt:           1
// Human-reviewed:   0/11
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       11
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using Broiler.Unicode.Algorithms;
using Broiler.Unicode.Tables; // ARM CHANGE: the test-only tables

namespace Broiler.VM.Profile.JavaScript.Format;

/// <summary>
/// The owner's algorithms over this assembly's tables, made once: the property lookups and the
/// lexical classes the matcher's property escapes and group names read, case folding for its
/// Canonicalize, and the properties of strings for the <c>v</c> flag.
/// </summary>
/// <remarks>
/// Each instance is immutable and safe to share between threads (the owner's adapter guide), so one
/// serves every engine of the process. None takes a meter: a lookup is a binary search bounded by the
/// tables, and the matcher charges for what it does with the answer exactly as it did when the lookup
/// was its own (JSD-0074, decision 4: the lookups and folding charge nothing).
/// </remarks>
// Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=695E20
// Broiler-Human:        PENDING
internal static class JsFormatUnicode
{
    /// <summary>The provider every instance reads.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=A4A67D
    // Broiler-Human:        PENDING
    // ARM CHANGE: was `private static readonly JsFormatUnicodeTables Tables = new();`
    private static readonly IUnicodeTables Tables = UnicodeTables.Instance;

    /// <summary>The exact property lookups of <c>\p{...}</c>.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=96B680
    // Broiler-Human:        PENDING
    internal static readonly UnicodeProperties Properties = new(Tables);

    /// <summary>IdentifierStartChar, IdentifierPartChar and WhiteSpace.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=D9919C
    // Broiler-Human:        PENDING
    internal static readonly EcmaScriptLexical Lexical = new(Properties);

    /// <summary>Simple case folding and the non-<c>u</c> canonicalisation, with their orbits.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=C9683E
    // Broiler-Human:        PENDING
    internal static readonly UnicodeCaseFolding Folding = new(Tables);

    /// <summary>The properties of strings the <c>v</c> flag admits.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=486F5C
    // Broiler-Human:        PENDING
    internal static readonly UnicodeStringProperties StringProperties = new(Tables);
}
