// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   20
// Annotated:        20/20
// Exempt:           26
// Human-reviewed:   0/20
// IP risk:          None
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       20
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Collections.Generic;

namespace Broiler.Regex.Ast;

/// <summary>
/// Base class for every node of a parsed regular-expression pattern.
/// The tree mirrors the ECMAScript <c>Pattern</c> grammar (ECMA-262 §22.2.1):
/// a <see cref="DisjunctionNode"/> of <see cref="SequenceNode"/>s of terms.
/// </summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=None; Resources=0; Fingerprint=E77094
// Hairs-Human:        PENDING
internal abstract class RegexNode
{
}

/// <summary>An alternation: <c>a|b|c</c> (§22.2.1 Disjunction).</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=1BEBB3
// Hairs-Human:        PENDING
internal sealed class DisjunctionNode(IReadOnlyList<RegexNode> alternatives) : RegexNode
{
    public readonly IReadOnlyList<RegexNode> Alternatives = alternatives;
}

/// <summary>A concatenation of terms (§22.2.1 Alternative).</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=157A70
// Hairs-Human:        PENDING
internal sealed class SequenceNode(IReadOnlyList<RegexNode> terms) : RegexNode
{
    public readonly IReadOnlyList<RegexNode> Terms = terms;
}

/// <summary>The empty alternative (matches the empty string).</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=None; Resources=0; Fingerprint=7DD9C3
// Hairs-Human:        PENDING
internal sealed class EmptyNode : RegexNode
{
    // Hairs-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=1D00D8
    // Hairs-Human:        PENDING
    public static readonly EmptyNode Instance = new();
    private EmptyNode() { }
}

/// <summary>A single literal code point (§22.2.1 PatternCharacter / CharacterEscape).</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=B0296C
// Hairs-Human:        PENDING
internal sealed class CharNode(int codePoint) : RegexNode
{
    /// <summary>The Unicode code point (may be astral, e.g. from <c>\u{1F438}</c>).</summary>
    public readonly int CodePoint = codePoint;
}

/// <summary><c>.</c> — matches any character (any code point under <c>s</c>, else non-line-terminator).</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=None; Resources=0; Fingerprint=43D5A0
// Hairs-Human:        PENDING
internal sealed class AnyCharNode : RegexNode
{
    // Hairs-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=71F067
    // Hairs-Human:        PENDING
    public static readonly AnyCharNode Instance = new();
    private AnyCharNode() { }
}

/// <summary>A character class <c>[...]</c> or a standalone class escape such as <c>\d</c>.</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=2A55B9
// Hairs-Human:        PENDING
internal sealed class CharClassNode(CharSet set) : RegexNode
{
    public readonly CharSet Set = set;
}

/// <summary>A quantified term: <c>X* X+ X? X{n,m}</c> (§22.2.1 Quantifier).</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=84EAB5
// Hairs-Human:        PENDING
internal sealed class QuantifierNode(RegexNode child, int min, int max, bool greedy) : RegexNode
{
    public readonly RegexNode Child = child;
    public readonly int Min = min;
    /// <summary>Maximum repetitions, or <see cref="Unbounded"/> for no upper limit.</summary>
    public readonly int Max = max;
    public readonly bool Greedy = greedy;

    // Hairs-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=F4CBC5
    // Hairs-Human:        PENDING
    public const int Unbounded = -1;
}

/// <summary>A group: capturing (numbered, optionally named) or non-capturing (§22.2.1 Atom).</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=C513B3
// Hairs-Human:        PENDING
internal sealed class GroupNode(RegexNode child, int captureIndex, string? name) : RegexNode
{
    public readonly RegexNode Child = child;
    /// <summary>1-based capture index, or 0 for a non-capturing group.</summary>
    public readonly int CaptureIndex = captureIndex;
    /// <summary>The <c>(?&lt;name&gt;…)</c> group name, or null.</summary>
    public readonly string? Name = name;

    // Hairs-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=92D60C
    // Hairs-Human:        PENDING
    public bool IsCapturing => CaptureIndex > 0;
}

/// <summary>
/// An inline-modifier group <c>(?ims-ims:…)</c> (ES2025 §22.2.1). Adds/removes
/// the <c>i</c>/<c>m</c>/<c>s</c> flags for the duration of its body.
/// </summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=B3DD70
// Hairs-Human:        PENDING
internal sealed class ModifierGroupNode(RegexNode child, RegexFlags added, RegexFlags removed) : RegexNode
{
    public readonly RegexNode Child = child;
    public readonly RegexFlags Added = added;
    public readonly RegexFlags Removed = removed;
}

/// <summary>A back-reference to an earlier capture by number or name (§22.2.1 AtomEscape).</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=C382DA
// Hairs-Human:        PENDING
internal sealed class BackreferenceNode : RegexNode
{
    /// <summary>1-based capture index for a numeric reference, or 0 when <see cref="Name"/> is used.</summary>
    public readonly int Index;
    public readonly string? Name;

    // Hairs-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=0DA29F
    // Hairs-Human:        PENDING
    public BackreferenceNode(int index) { Index = index; Name = null; }
    // Hairs-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=E96010
    // Hairs-Human:        PENDING
    public BackreferenceNode(string name) { Index = 0; Name = name; }
}

// Hairs-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=D701D8
// Hairs-Human:        PENDING
internal enum AnchorKind
{
    /// <summary><c>^</c></summary>
    StartOfInput,
    /// <summary><c>$</c></summary>
    EndOfInput,
    /// <summary><c>\b</c></summary>
    WordBoundary,
    /// <summary><c>\B</c></summary>
    NonWordBoundary,
}

/// <summary>A zero-width assertion at the boundary level (§22.2.1 Assertion).</summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=CE283F
// Hairs-Human:        PENDING
internal sealed class AnchorNode(AnchorKind kind) : RegexNode
{
    public readonly AnchorKind Kind = kind;
}

/// <summary>
/// A look-around assertion: look-ahead <c>(?=…)</c>/<c>(?!…)</c> or look-behind
/// <c>(?&lt;=…)</c>/<c>(?&lt;!…)</c> (§22.2.1 Assertion). A look-behind body is
/// matched in the reverse direction (see the matcher).
/// </summary>
// Hairs-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=F505EE
// Hairs-Human:        PENDING
internal sealed class LookaroundNode(RegexNode child, bool behind, bool negative) : RegexNode
{
    public readonly RegexNode Child = child;
    public readonly bool Behind = behind;
    public readonly bool Negative = negative;
}
