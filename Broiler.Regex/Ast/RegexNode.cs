// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
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
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=None; Resources=0; Fingerprint=9FE564
// Broiler-Human:        PENDING
public abstract class RegexNode
{
}

/// <summary>An alternation: <c>a|b|c</c> (§22.2.1 Disjunction).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=396456
// Broiler-Human:        PENDING
public sealed class DisjunctionNode(IReadOnlyList<RegexNode> alternatives) : RegexNode
{
    public readonly IReadOnlyList<RegexNode> Alternatives = alternatives;
}

/// <summary>A concatenation of terms (§22.2.1 Alternative).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=61124D
// Broiler-Human:        PENDING
public sealed class SequenceNode(IReadOnlyList<RegexNode> terms) : RegexNode
{
    public readonly IReadOnlyList<RegexNode> Terms = terms;
}

/// <summary>The empty alternative (matches the empty string).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=None; Resources=0; Fingerprint=E81119
// Broiler-Human:        PENDING
public sealed class EmptyNode : RegexNode
{
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=1D00D8
    // Broiler-Human:        PENDING
    public static readonly EmptyNode Instance = new();
    private EmptyNode() { }
}

/// <summary>A single literal code point (§22.2.1 PatternCharacter / CharacterEscape).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=7313B3
// Broiler-Human:        PENDING
public sealed class CharNode(int codePoint) : RegexNode
{
    /// <summary>The Unicode code point (may be astral, e.g. from <c>\u{1F438}</c>).</summary>
    public readonly int CodePoint = codePoint;
}

/// <summary><c>.</c> — matches any character (any code point under <c>s</c>, else non-line-terminator).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=None; Resources=0; Fingerprint=1A69F5
// Broiler-Human:        PENDING
public sealed class AnyCharNode : RegexNode
{
    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=71F067
    // Broiler-Human:        PENDING
    public static readonly AnyCharNode Instance = new();
    private AnyCharNode() { }
}

/// <summary>A character class <c>[...]</c> or a standalone class escape such as <c>\d</c>.</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=ED7BF1
// Broiler-Human:        PENDING
public sealed class CharClassNode(CharSet set) : RegexNode
{
    public readonly CharSet Set = set;
}

/// <summary>A quantified term: <c>X* X+ X? X{n,m}</c> (§22.2.1 Quantifier).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=12605D
// Broiler-Human:        PENDING
public sealed class QuantifierNode(RegexNode child, int min, int max, bool greedy) : RegexNode
{
    public readonly RegexNode Child = child;
    public readonly int Min = min;
    /// <summary>Maximum repetitions, or <see cref="Unbounded"/> for no upper limit.</summary>
    public readonly int Max = max;
    public readonly bool Greedy = greedy;

    // Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=F4CBC5
    // Broiler-Human:        PENDING
    public const int Unbounded = -1;
}

/// <summary>A group: capturing (numbered, optionally named) or non-capturing (§22.2.1 Atom).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=E124F7
// Broiler-Human:        PENDING
public sealed class GroupNode(RegexNode child, int captureIndex, string? name) : RegexNode
{
    public readonly RegexNode Child = child;
    /// <summary>1-based capture index, or 0 for a non-capturing group.</summary>
    public readonly int CaptureIndex = captureIndex;
    /// <summary>The <c>(?&lt;name&gt;…)</c> group name, or null.</summary>
    public readonly string? Name = name;

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=92D60C
    // Broiler-Human:        PENDING
    public bool IsCapturing => CaptureIndex > 0;
}

/// <summary>
/// An inline-modifier group <c>(?ims-ims:…)</c> (ES2025 §22.2.1). Adds/removes
/// the <c>i</c>/<c>m</c>/<c>s</c> flags for the duration of its body.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=B1EE3A
// Broiler-Human:        PENDING
public sealed class ModifierGroupNode(RegexNode child, RegexFlags added, RegexFlags removed) : RegexNode
{
    public readonly RegexNode Child = child;
    public readonly RegexFlags Added = added;
    public readonly RegexFlags Removed = removed;
}

/// <summary>A back-reference to an earlier capture by number or name (§22.2.1 AtomEscape).</summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=EB97EE
// Broiler-Human:        PENDING
public sealed class BackreferenceNode : RegexNode
{
    /// <summary>1-based capture index for a numeric reference, or 0 when <see cref="Name"/> is used.</summary>
    public readonly int Index;
    public readonly string? Name;

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=0DA29F
    // Broiler-Human:        PENDING
    public BackreferenceNode(int index) { Index = index; Name = null; }
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=E96010
    // Broiler-Human:        PENDING
    public BackreferenceNode(string name) { Index = 0; Name = name; }
}

// Broiler-AI:           Origin=AI; IP=None; Security=None; Resources=0; Fingerprint=DF9E67
// Broiler-Human:        PENDING
public enum AnchorKind
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
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=7976EB
// Broiler-Human:        PENDING
public sealed class AnchorNode(AnchorKind kind) : RegexNode
{
    public readonly AnchorKind Kind = kind;
}

/// <summary>
/// A look-around assertion: look-ahead <c>(?=…)</c>/<c>(?!…)</c> or look-behind
/// <c>(?&lt;=…)</c>/<c>(?&lt;!…)</c> (§22.2.1 Assertion). A look-behind body is
/// matched in the reverse direction (see the matcher).
/// </summary>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=None; Security=Low; Resources=0; Fingerprint=26E184
// Broiler-Human:        PENDING
public sealed class LookaroundNode(RegexNode child, bool behind, bool negative) : RegexNode
{
    public readonly RegexNode Child = child;
    public readonly bool Behind = behind;
    public readonly bool Negative = negative;
}
