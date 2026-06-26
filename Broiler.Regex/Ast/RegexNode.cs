using System.Collections.Generic;

namespace Broiler.Regex.Ast;

/// <summary>
/// Base class for every node of a parsed regular-expression pattern.
/// The tree mirrors the ECMAScript <c>Pattern</c> grammar (ECMA-262 §22.2.1):
/// a <see cref="DisjunctionNode"/> of <see cref="SequenceNode"/>s of terms.
/// </summary>
public abstract class RegexNode
{
}

/// <summary>An alternation: <c>a|b|c</c> (§22.2.1 Disjunction).</summary>
public sealed class DisjunctionNode(IReadOnlyList<RegexNode> alternatives) : RegexNode
{
    public readonly IReadOnlyList<RegexNode> Alternatives = alternatives;
}

/// <summary>A concatenation of terms (§22.2.1 Alternative).</summary>
public sealed class SequenceNode(IReadOnlyList<RegexNode> terms) : RegexNode
{
    public readonly IReadOnlyList<RegexNode> Terms = terms;
}

/// <summary>The empty alternative (matches the empty string).</summary>
public sealed class EmptyNode : RegexNode
{
    public static readonly EmptyNode Instance = new();
    private EmptyNode() { }
}

/// <summary>A single literal code point (§22.2.1 PatternCharacter / CharacterEscape).</summary>
public sealed class CharNode(int codePoint) : RegexNode
{
    /// <summary>The Unicode code point (may be astral, e.g. from <c>\u{1F438}</c>).</summary>
    public readonly int CodePoint = codePoint;
}

/// <summary><c>.</c> — matches any character (any code point under <c>s</c>, else non-line-terminator).</summary>
public sealed class AnyCharNode : RegexNode
{
    public static readonly AnyCharNode Instance = new();
    private AnyCharNode() { }
}

/// <summary>A character class <c>[...]</c> or a standalone class escape such as <c>\d</c>.</summary>
public sealed class CharClassNode(CharSet set) : RegexNode
{
    public readonly CharSet Set = set;
}

/// <summary>A quantified term: <c>X* X+ X? X{n,m}</c> (§22.2.1 Quantifier).</summary>
public sealed class QuantifierNode(RegexNode child, int min, int max, bool greedy) : RegexNode
{
    public readonly RegexNode Child = child;
    public readonly int Min = min;
    /// <summary>Maximum repetitions, or <see cref="Unbounded"/> for no upper limit.</summary>
    public readonly int Max = max;
    public readonly bool Greedy = greedy;

    public const int Unbounded = -1;
}

/// <summary>A group: capturing (numbered, optionally named) or non-capturing (§22.2.1 Atom).</summary>
public sealed class GroupNode(RegexNode child, int captureIndex, string? name) : RegexNode
{
    public readonly RegexNode Child = child;
    /// <summary>1-based capture index, or 0 for a non-capturing group.</summary>
    public readonly int CaptureIndex = captureIndex;
    /// <summary>The <c>(?&lt;name&gt;…)</c> group name, or null.</summary>
    public readonly string? Name = name;

    public bool IsCapturing => CaptureIndex > 0;
}

/// <summary>
/// An inline-modifier group <c>(?ims-ims:…)</c> (ES2025 §22.2.1). Adds/removes
/// the <c>i</c>/<c>m</c>/<c>s</c> flags for the duration of its body.
/// </summary>
public sealed class ModifierGroupNode(RegexNode child, RegexFlags added, RegexFlags removed) : RegexNode
{
    public readonly RegexNode Child = child;
    public readonly RegexFlags Added = added;
    public readonly RegexFlags Removed = removed;
}

/// <summary>A back-reference to an earlier capture by number or name (§22.2.1 AtomEscape).</summary>
public sealed class BackreferenceNode : RegexNode
{
    /// <summary>1-based capture index for a numeric reference, or 0 when <see cref="Name"/> is used.</summary>
    public readonly int Index;
    public readonly string? Name;

    public BackreferenceNode(int index) { Index = index; Name = null; }
    public BackreferenceNode(string name) { Index = 0; Name = name; }
}

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
public sealed class AnchorNode(AnchorKind kind) : RegexNode
{
    public readonly AnchorKind Kind = kind;
}

/// <summary>
/// A look-around assertion: look-ahead <c>(?=…)</c>/<c>(?!…)</c> or look-behind
/// <c>(?&lt;=…)</c>/<c>(?&lt;!…)</c> (§22.2.1 Assertion). A look-behind body is
/// matched in the reverse direction (see the matcher).
/// </summary>
public sealed class LookaroundNode(RegexNode child, bool behind, bool negative) : RegexNode
{
    public readonly RegexNode Child = child;
    public readonly bool Behind = behind;
    public readonly bool Negative = negative;
}
