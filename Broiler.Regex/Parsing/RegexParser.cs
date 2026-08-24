using System.Collections.Generic;
using System.Text;
using Broiler.Regex.Ast;
using Broiler.Regex.Unicode;

namespace Broiler.Regex.Parsing;

/// <summary>
/// Recursive-descent parser for the ECMAScript regular-expression
/// <c>Pattern</c> grammar (ECMA-262 §22.2.1). Produces a <see cref="RegexNode"/>
/// tree consumed by the matcher.
/// </summary>
public sealed class RegexParser
{
    private readonly string _src;
    private readonly bool _unicode;
    private readonly bool _unicodeSets;
    private int _pos;
    private int _captureCount;

    /// <summary>
    /// The effective <c>i</c> flag at the cursor. A <c>v</c>-mode class folds its operands
    /// while it is being built (§22.2.1 MaybeSimpleCaseFolding), and <c>\w</c>'s set widens
    /// under <c>iu</c> (§22.2.2.9.2), so both need the flag as an inline modifier group
    /// leaves it — not only as the pattern's own flags set it.
    /// </summary>
    private readonly Stack<bool> _ignoreCase = new();

    /// <summary>Total number of capturing groups, available after parsing.</summary>
    public int CaptureCount { get; private set; }

    /// <summary>Map of group name → 1-based capture index, available after parsing.</summary>
    public IReadOnlyDictionary<string, int> GroupNames => _groupNames;

    private readonly Dictionary<string, int> _groupNames = [];
    private readonly Dictionary<string, int> _declaredNames = [];
    private int _totalCaptureGroups;

    public RegexParser(string pattern, RegexFlags flags)
    {
        _src = pattern ?? "";
        _unicode = flags.IsUnicodeMode();
        _unicodeSets = (flags & RegexFlags.UnicodeSets) != 0;
        _ignoreCase.Push((flags & RegexFlags.IgnoreCase) != 0);
    }

    private bool IgnoreCase => _ignoreCase.Peek();

    public static RegexNode Parse(string pattern, RegexFlags flags, out int captureCount,
        out IReadOnlyDictionary<string, int> groupNames)
    {
        var parser = new RegexParser(pattern, flags);
        var node = parser.ParsePattern();
        captureCount = parser.CaptureCount;
        groupNames = parser.GroupNames;
        return node;
    }

    private RegexNode ParsePattern()
    {
        // Pre-scan to learn the total capture-group count and the declared group
        // names. ECMAScript needs both up front: in Unicode mode `\1` is always a
        // back-reference (its validity depends on the total count), and a forward
        // `\k<name>` must resolve to a name declared later in the pattern.
        PreScan();

        var node = ParseDisjunction();
        if (_pos != _src.Length)
            throw new RegexSyntaxException($"Unexpected '{_src[_pos]}' in pattern", _pos);

        CaptureCount = _captureCount;
        return node;
    }

    private void PreScan()
    {
        // A `v`-mode class nests, so the depth — not a single in-class flag — is what says
        // whether a '(' is a group or a literal inside a class.
        var depth = 0;
        for (var i = 0; i < _src.Length; i++)
        {
            var c = _src[i];
            if (c == '\\')
            {
                i++; // skip escaped char
                continue;
            }
            if (c == '[') { if (_unicodeSets || depth == 0) depth++; continue; }
            if (c == ']') { if (depth > 0) depth--; continue; }
            if (depth > 0 || c != '(')
                continue;

            // '(' — capturing unless it begins a (?: (?= (?! (?<= (?<! (?flags …) group.
            if (i + 1 < _src.Length && _src[i + 1] == '?')
            {
                // (?<name>…) is capturing+named; (?<=…) / (?<!…) are not.
                if (i + 2 < _src.Length && _src[i + 2] == '<'
                    && i + 3 < _src.Length && _src[i + 3] != '=' && _src[i + 3] != '!')
                {
                    var end = _src.IndexOf('>', i + 3);
                    if (end < 0)
                        throw new RegexSyntaxException("Unterminated group name", i);
                    var name = _src.Substring(i + 3, end - (i + 3));
                    _totalCaptureGroups++;
                    if (!_declaredNames.TryAdd(name, _totalCaptureGroups))
                        throw new RegexSyntaxException($"Duplicate capture group name '{name}'", i);
                }
                continue;
            }

            _totalCaptureGroups++;
        }
    }

    // ----- Disjunction / Alternative -----------------------------------------

    private RegexNode ParseDisjunction()
    {
        var alternatives = new List<RegexNode> { ParseAlternative() };
        while (Peek() == '|')
        {
            _pos++;
            alternatives.Add(ParseAlternative());
        }

        return alternatives.Count == 1 ? alternatives[0] : new DisjunctionNode(alternatives);
    }

    private RegexNode ParseAlternative()
    {
        var terms = new List<RegexNode>();
        while (true)
        {
            if (AtEnd || Peek() is '|' or ')')
                break;
            terms.Add(ParseTerm());
        }

        return terms.Count switch
        {
            0 => EmptyNode.Instance,
            1 => terms[0],
            _ => new SequenceNode(terms),
        };
    }

    // ----- Term (Atom + optional Quantifier, or an Assertion) ----------------

    private RegexNode ParseTerm()
    {
        var assertion = TryParseAssertion(out var quantifiable);
        if (assertion != null)
        {
            if (!HasQuantifier())
                return assertion;

            // Annex B `ExtendedTerm :: QuantifiableAssertion Quantifier`: outside Unicode
            // mode a look-AHEAD may carry a quantifier. Every other assertion — a
            // look-behind, `^`, `$`, `\b`, `\B` — may not, and in Unicode mode none may.
            if (!quantifiable || _unicode)
                throw new RegexSyntaxException("Nothing to repeat before quantifier", _pos);

            return TryApplyQuantifier(assertion);
        }

        var atom = ParseAtom();
        return TryApplyQuantifier(atom);
    }

    /// <summary>
    /// Parses an assertion, reporting through <paramref name="quantifiable"/> whether
    /// Annex B admits a quantifier after it.
    /// </summary>
    private RegexNode? TryParseAssertion(out bool quantifiable)
    {
        quantifiable = false;
        var c = Peek();
        switch (c)
        {
            case '^': _pos++; return new AnchorNode(AnchorKind.StartOfInput);
            case '$': _pos++; return new AnchorNode(AnchorKind.EndOfInput);
        }

        if (c == '\\' && PeekAt(1) is 'b' or 'B')
        {
            var kind = PeekAt(1) == 'b' ? AnchorKind.WordBoundary : AnchorKind.NonWordBoundary;
            _pos += 2;
            return new AnchorNode(kind);
        }

        // Look-around groups (?= (?! (?<= (?<!  — these are assertions, not atoms.
        if (c == '(' && PeekAt(1) == '?')
        {
            var third = PeekAt(2);
            if (third is '=' or '!')
            {
                _pos += 3;
                var body = ParseDisjunction();
                Expect(')');
                quantifiable = true;
                return new LookaroundNode(body, behind: false, negative: third == '!');
            }
            if (third == '<' && PeekAt(3) is '=' or '!')
            {
                var negative = PeekAt(3) == '!';
                _pos += 4;
                var body = ParseDisjunction();
                Expect(')');
                return new LookaroundNode(body, behind: true, negative: negative);
            }
        }

        return null;
    }

    /// <summary>
    /// True when a well-formed quantifier starts at the cursor. A `{` that does not open
    /// one is an ordinary character, so `/^{a}/` is the anchor followed by literal text
    /// while `/^{2}/` is a quantified assertion — a syntax error.
    /// </summary>
    private bool HasQuantifier()
    {
        if (Peek() is '*' or '+' or '?')
            return true;
        if (Peek() != '{')
            return false;

        var save = _pos;
        var found = TryParseBraceQuantifier(out _, out _);
        _pos = save;
        return found;
    }

    private RegexNode TryApplyQuantifier(RegexNode atom)
    {
        var c = Peek();
        int min, max;
        switch (c)
        {
            case '*': min = 0; max = QuantifierNode.Unbounded; _pos++; break;
            case '+': min = 1; max = QuantifierNode.Unbounded; _pos++; break;
            case '?': min = 0; max = 1; _pos++; break;
            case '{':
                if (!TryParseBraceQuantifier(out min, out max))
                    return atom; // a literal '{' that is not a valid quantifier
                break;
            default:
                return atom;
        }

        var greedy = true;
        if (Peek() == '?')
        {
            greedy = false;
            _pos++;
        }

        return new QuantifierNode(atom, min, max, greedy);
    }

    private bool TryParseBraceQuantifier(out int min, out int max)
    {
        min = max = 0;
        var save = _pos;
        _pos++; // consume '{'

        if (!TryReadDecimal(out min))
        {
            _pos = save;
            return false;
        }

        max = min;
        if (Peek() == ',')
        {
            _pos++;
            if (Peek() == '}')
            {
                max = QuantifierNode.Unbounded;
            }
            else if (!TryReadDecimal(out max))
            {
                _pos = save;
                return false;
            }
        }

        if (Peek() != '}')
        {
            _pos = save;
            return false;
        }
        _pos++; // consume '}'

        if (max != QuantifierNode.Unbounded && max < min)
            throw new RegexSyntaxException("Quantifier {n,m} with m < n", save);

        return true;
    }

    // ----- Atom ---------------------------------------------------------------

    private RegexNode ParseAtom()
    {
        if (AtEnd)
            throw new RegexSyntaxException("Unexpected end of atom", _pos);

        var c = Peek();
        switch (c)
        {
            case '.':
                _pos++;
                return AnyCharNode.Instance;
            case '(':
                return ParseGroup();
            case '[':
                return new CharClassNode(ParseCharacterClass());
            case '\\':
                return ParseAtomEscape();
            case ')':
            case '|':
                throw new RegexSyntaxException("Unexpected end of atom", _pos);
            case '*':
            case '+':
            case '?':
                throw new RegexSyntaxException($"Nothing to repeat before '{c}'", _pos);
            case ']':
            case '{':
            case '}':
                // Annex B lets these stand for themselves; Unicode mode drops that
                // extension, so an unescaped one (including a '{' that did not open a
                // valid quantifier) is a syntax error rather than a literal.
                if (_unicode)
                    throw new RegexSyntaxException($"'{c}' must be escaped in Unicode mode", _pos);
                break;
        }

        // PatternCharacter — a single source character (or astral code point in u-mode).
        var cp = ReadSourceCodePoint();
        return new CharNode(cp);
    }

    private RegexNode ParseGroup()
    {
        _pos++; // consume '('
        if (Peek() == '?')
        {
            var second = PeekAt(1);

            // Non-capturing (?:…)
            if (second == ':')
            {
                _pos += 2;
                var body = ParseDisjunction();
                Expect(')');
                return new GroupNode(body, captureIndex: 0, name: null);
            }

            // Named capturing (?<name>…)
            if (second == '<' && PeekAt(2) != '=' && PeekAt(2) != '!')
            {
                _pos += 2; // consume '?<'
                var name = ReadGroupName();
                var index = ++_captureCount;
                _groupNames[name] = index;
                var body = ParseDisjunction();
                Expect(')');
                return new GroupNode(body, index, name);
            }

            // Inline modifier group (?ims-ims:…)
            if (second is 'i' or 'm' or 's' or '-')
                return ParseModifierGroup();

            throw new RegexSyntaxException("Invalid group", _pos);
        }

        // Plain capturing group
        var captureIndex = ++_captureCount;
        var child = ParseDisjunction();
        Expect(')');
        return new GroupNode(child, captureIndex, null);
    }

    private RegexNode ParseModifierGroup()
    {
        _pos++; // consume '?'
        var added = ReadModifierFlags();
        var removed = RegexFlags.None;
        if (Peek() == '-')
        {
            _pos++;
            removed = ReadModifierFlags();
        }

        if (added == RegexFlags.None && removed == RegexFlags.None)
            throw new RegexSyntaxException("Modifier group must add or remove at least one flag", _pos);
        if ((added & removed) != 0)
            throw new RegexSyntaxException("A modifier flag may not be both added and removed", _pos);

        Expect(':');
        _ignoreCase.Push(
            (IgnoreCase || (added & RegexFlags.IgnoreCase) != 0) && (removed & RegexFlags.IgnoreCase) == 0);
        var body = ParseDisjunction();
        _ignoreCase.Pop();
        Expect(')');
        return new ModifierGroupNode(body, added, removed);
    }

    private RegexFlags ReadModifierFlags()
    {
        var flags = RegexFlags.None;
        while (true)
        {
            var flag = Peek() switch
            {
                'i' => RegexFlags.IgnoreCase,
                'm' => RegexFlags.Multiline,
                's' => RegexFlags.DotAll,
                _ => RegexFlags.None,
            };
            if (flag == RegexFlags.None)
                break;
            if ((flags & flag) != 0)
                throw new RegexSyntaxException("Repeated flag in modifier group", _pos);
            flags |= flag;
            _pos++;
        }
        return flags;
    }

    // ----- AtomEscape (outside a character class) -----------------------------

    private RegexNode ParseAtomEscape()
    {
        _pos++; // consume '\'
        var c = Peek();

        // Decimal back-reference \1 .. \n
        if (c is >= '1' and <= '9')
        {
            var start = _pos;
            TryReadDecimal(out var num);
            if (num <= _totalCaptureGroups)
                return new BackreferenceNode(num);
            // In Unicode mode an over-large numeric escape is a syntax error;
            // in non-Unicode it is a legacy octal/identity escape.
            if (_unicode)
                throw new RegexSyntaxException($"Back-reference \\{num} to a non-existent group", start);
            _pos = start;
            return new CharNode(ReadLegacyOctalOrIdentity());
        }

        // Named back-reference \k<name>. Outside Unicode mode `\k` only introduces one
        // when the pattern declares a group name somewhere; with none, Annex B reads it
        // as the identity escape `k`, so /\k<n>/ is the literal text "k<n>".
        if (c == 'k' && (_unicode || _declaredNames.Count > 0))
        {
            _pos++;
            if (Peek() != '<')
                throw new RegexSyntaxException("Expected '<' after \\k", _pos);
            _pos++;
            var name = ReadGroupName();
            return new BackreferenceNode(name);
        }

        // Class escapes that are valid as standalone atoms.
        var classEscape = TryReadClassEscape();
        if (classEscape.HasValue)
        {
            // Under `v` even a standalone escape is a class-set operand: `\W` is the
            // complement of the FOLDED word set within the canonical universe, which is
            // why /\W/vi rejects `A` where /\W/ui accepts it.
            if (_unicodeSets)
                return new CharClassNode(ToCharSet(ReadClassEscapeOperand(classEscape.Value)));

            var set = new CharSet();
            set.AddEscape(classEscape.Value, IgnoreCase, _unicode);
            return new CharClassNode(set);
        }

        // Unicode property escape \p{…} / \P{…}
        if (c is 'p' or 'P' && _unicode)
        {
            if (_unicodeSets)
                return new CharClassNode(ToCharSet(ReadPropertyOperand(allowStrings: true)));

            var set = new CharSet();
            AddPropertyEscape(set, allowStrings: false);
            return new CharClassNode(set);
        }

        // Otherwise a single escaped code point (\n, \xHH, \uHHHH, \u{…}, identity).
        return new CharNode(ReadCharacterEscape());
    }

    // ----- Character class [ ... ] -------------------------------------------

    private CharSet ParseCharacterClass()
    {
        if (_unicodeSets)
            return ParseClassSet();

        _pos++; // consume '['
        var set = new CharSet();
        if (Peek() == '^')
        {
            set.Negated = true;
            _pos++;
        }

        while (true)
        {
            if (AtEnd)
                throw new RegexSyntaxException("Unterminated character class", _pos);

            var c = Peek();
            if (c == ']')
            {
                _pos++;
                return set;
            }

            var first = ReadClassAtom(set, out var firstIsLiteral);

            // A range "a-z": only when '-' is followed by another class atom rather than
            // by the closing ']'.
            if (Peek() == '-' && HasAt(1) && PeekAt(1) != ']')
            {
                var dash = _pos;
                _pos++; // consume '-'
                var second = ReadClassAtom(set, out var secondIsLiteral);
                if (firstIsLiteral && secondIsLiteral)
                {
                    set.AddRange(first, second);
                }
                else if (_unicode)
                {
                    // §22.2.1 early error: an endpoint that is itself a class (\d, \p{L})
                    // is a syntax error. Only Annex B's non-Unicode grammar rescues it as
                    // a literal '-'.
                    throw new RegexSyntaxException(
                        "A character-class escape may not be an endpoint of a range", dash);
                }
                else
                {
                    if (firstIsLiteral)
                        set.AddCodePoint(first);
                    set.AddCodePoint('-');
                    if (secondIsLiteral)
                        set.AddCodePoint(second);
                }
            }
            else if (firstIsLiteral)
            {
                set.AddCodePoint(first);
            }
        }
    }

    /// <summary>
    /// Reads one class atom. Adds class escapes (\d…) directly to <paramref name="set"/>
    /// and returns their sentinel as a non-literal; returns a literal code point otherwise.
    /// </summary>
    private int ReadClassAtom(CharSet set, out bool isLiteral)
    {
        var c = Peek();
        if (c == '\\')
        {
            _pos++;
            var classEscape = TryReadClassEscape();
            if (classEscape.HasValue)
            {
                set.AddEscape(classEscape.Value, IgnoreCase, _unicode);
                isLiteral = false;
                return -1;
            }
            if (Peek() is 'p' or 'P' && _unicode)
            {
                AddPropertyEscape(set, allowStrings: false);
                set.UsesPropertyEscape = true;
                isLiteral = false;
                return -1;
            }
            if (Peek() == 'b') { _pos++; isLiteral = true; return '\b'; } // \b is backspace in a class
            // `ClassEscape :: -` — inside a class `\-` is a literal hyphen even in Unicode
            // mode, where the standalone IdentityEscape would reject it.
            if (Peek() == '-') { _pos++; isLiteral = true; return '-'; }
            isLiteral = true;
            return ReadCharacterEscape();
        }

        isLiteral = true;
        return ReadSourceCodePoint();
    }

    // ----- v-mode class set expressions --------------------------------------
    //
    // Under the `v` flag a class is a ClassSetExpression (ECMA-262 §22.2.1): operands
    // that are nested classes, `\q{…}` string disjunctions, property escapes or single
    // characters, combined by union, `&&` intersection or `--` subtraction. Every
    // operand reduces to a CodePointSet plus a set of multi-code-point strings, so the
    // operators are the plain set algebra of CodePointSet.
    //
    // With `i`, MaybeSimpleCaseFolding folds each operand as it is produced — before any
    // complement is taken. That ordering is the whole observable difference between
    // `[^\p{Lu}]` under `vi` (matches neither `A` nor `a`) and under `ui` (matches `a`).

    /// <summary>One evaluated class-set operand: code points plus string members.</summary>
    private sealed class ClassSetValue
    {
        public CodePointSet Set = new();
        public List<string> Strings = [];
    }

    private bool _usedSetOperations;
    private bool _usedPropertyEscape;

    private CharSet ParseClassSet()
    {
        _usedSetOperations = false;
        _usedPropertyEscape = false;
        var set = ToCharSet(ParseNestedClass());
        set.UsesSetOperations = _usedSetOperations;
        set.UsesPropertyEscape = _usedPropertyEscape;
        return set;
    }

    /// <summary>
    /// Wraps an evaluated class-set value as a matchable <see cref="CharSet"/>. Under
    /// <c>i</c> the members are already folded, so the matcher folds the subject and tests
    /// membership directly rather than walking the subject's fold orbit.
    /// </summary>
    private CharSet ToCharSet(ClassSetValue value)
    {
        var set = new CharSet { CaseFolded = IgnoreCase };
        set.AddSet(value.Set);
        foreach (var member in value.Strings)
            set.AddString(member);
        set.SortStrings();
        return set;
    }

    /// <summary>
    /// §22.2.2.9 CharacterComplement: the complement is taken against AllCharacters, which
    /// under <c>v</c> with <c>i</c> holds only the code points that fold to themselves.
    /// </summary>
    private CodePointSet ComplementOperand(CodePointSet set)
        => UnicodeCharSets.AllCharacters(_unicodeSets, IgnoreCase).Subtract(set);

    /// <summary>Reads <c>\d \D \w \W \s \S</c> as a class-set operand: fold, then complement.</summary>
    private ClassSetValue ReadClassEscapeOperand(ClassEscape escape)
    {
        var value = new ClassSetValue();
        value.Set.AddAll(UnicodeCharSets.EscapeSet(UnicodeCharSets.PositiveEscape(escape), IgnoreCase, _unicode));
        Fold(value);
        if (UnicodeCharSets.IsComplementEscape(escape))
            value.Set = ComplementOperand(value.Set);
        return value;
    }

    /// <summary>Reads <c>\p{…}</c> / <c>\P{…}</c> as a class-set operand: fold, then complement.</summary>
    private ClassSetValue ReadPropertyOperand(bool allowStrings)
    {
        var property = new CharSet();
        var negated = AddPropertyEscape(property, allowStrings, complement: false);
        var value = new ClassSetValue { Set = property.CodePoints, Strings = [.. property.Strings] };
        Fold(value);
        if (negated)
            value.Set = ComplementOperand(value.Set);
        return value;
    }

    /// <summary>Parses <c>[ ^? ClassContents ]</c>, the cursor sitting on the <c>'['</c>.</summary>
    private ClassSetValue ParseNestedClass()
    {
        var start = _pos;
        _pos++; // consume '['

        var negated = false;
        if (Peek() == '^')
        {
            negated = true;
            _pos++;
        }

        var value = ParseClassSetExpression();

        if (AtEnd)
            throw new RegexSyntaxException("Unterminated character class", start);
        _pos++; // consume ']'

        if (!negated)
            return value;

        // §22.2.1 early error: a complemented class may not contain strings, because the
        // complement of a set of strings is not a set this grammar can describe.
        if (value.Strings.Count > 0)
            throw new RegexSyntaxException("A negated 'v'-mode class may not contain strings", start);

        return new ClassSetValue { Set = ComplementOperand(value.Set) };
    }

    private ClassSetValue ParseClassSetExpression()
    {
        if (AtEnd || Peek() == ']')
            return new ClassSetValue();

        var start = _pos;
        var accumulated = ParseClassSetRangeOrOperand();

        if (Peek() == '&' && PeekAt(1) == '&')
            return ParseClassIntersection(accumulated, start);
        if (Peek() == '-' && PeekAt(1) == '-')
            return ParseClassSubtraction(accumulated, start);

        while (!AtEnd && Peek() != ']')
        {
            RejectMixedOperator(start);
            var operand = ParseClassSetRangeOrOperand();
            accumulated.Set.AddAll(operand.Set);
            foreach (var member in operand.Strings)
            {
                if (!accumulated.Strings.Contains(member))
                    accumulated.Strings.Add(member);
            }
        }

        return accumulated;
    }

    private ClassSetValue ParseClassIntersection(ClassSetValue first, int start)
    {
        _usedSetOperations = true;
        var accumulated = first;
        while (Peek() == '&' && PeekAt(1) == '&')
        {
            _pos += 2;
            if (Peek() == '&')
                throw new RegexSyntaxException("'&&&' is not a class-set operator", _pos);
            var operand = ParseClassSetOperand(out _);
            accumulated = new ClassSetValue
            {
                Set = accumulated.Set.Intersect(operand.Set),
                Strings = [.. accumulated.Strings.FindAll(operand.Strings.Contains)],
            };
        }

        RejectMixedOperator(start);
        return accumulated;
    }

    private ClassSetValue ParseClassSubtraction(ClassSetValue first, int start)
    {
        _usedSetOperations = true;
        var accumulated = first;
        while (Peek() == '-' && PeekAt(1) == '-')
        {
            _pos += 2;
            var operand = ParseClassSetOperand(out _);
            accumulated = new ClassSetValue
            {
                Set = accumulated.Set.Subtract(operand.Set),
                Strings = [.. accumulated.Strings.FindAll(member => !operand.Strings.Contains(member))],
            };
        }

        RejectMixedOperator(start);
        return accumulated;
    }

    /// <summary>
    /// A class-set expression is a union, an intersection or a subtraction — never a
    /// mixture, so an operator (or trailing operand) left over after one of them is a
    /// syntax error rather than a silently reassociated expression.
    /// </summary>
    private void RejectMixedOperator(int start)
    {
        if (AtEnd || Peek() == ']')
            return;
        if ((Peek() == '&' && PeekAt(1) == '&') || (Peek() == '-' && PeekAt(1) == '-'))
            throw new RegexSyntaxException(
                "A class-set expression may not mix union, '&&' and '--' without nesting", start);
    }

    private ClassSetValue ParseClassSetRangeOrOperand()
    {
        var start = _pos;
        var operand = ParseClassSetOperand(out var single);

        // A range needs a single character on both sides; `a--b` is a subtraction and
        // `a-]` ends the class, so neither starts one.
        if (single == null || Peek() != '-' || PeekAt(1) == '-' || PeekAt(1) == ']' || !HasAt(1))
            return operand;

        _pos++; // consume '-'
        _ = ParseClassSetOperand(out var highSingle);
        if (highSingle == null)
            throw new RegexSyntaxException("A class-set range needs a single character on both sides", start);
        if (single.Value > highSingle.Value)
            throw new RegexSyntaxException(
                $"Range out of order in character class: U+{single.Value:X}-U+{highSingle.Value:X}", start);

        return Fold(new ClassSetValue { Set = CodePointSet.Of(single.Value, highSingle.Value) });
    }

    /// <summary>
    /// Parses one <c>ClassSetOperand</c>. <paramref name="single"/> receives the code
    /// point when the operand was a lone character, which is the only shape a range end
    /// may take.
    /// </summary>
    private ClassSetValue ParseClassSetOperand(out int? single)
    {
        single = null;
        if (AtEnd)
            throw new RegexSyntaxException("Unterminated character class", _pos);

        var c = Peek();
        if (c == '[')
        {
            _usedSetOperations = true;
            return ParseNestedClass();
        }

        if (c == '\\')
        {
            _pos++;
            if (Peek() == 'q')
                return ParseClassStringDisjunction();

            var classEscape = TryReadClassEscape();
            if (classEscape.HasValue)
                return ReadClassEscapeOperand(classEscape.Value);

            if (Peek() is 'p' or 'P')
            {
                _usedPropertyEscape = true;
                return ReadPropertyOperand(allowStrings: true);
            }

            if (Peek() == 'b')
            {
                _pos++;
                single = '\b';
                return Fold(SingleCodePoint('\b'));
            }

            // `\` followed by one of the reserved punctuators is that punctuator; every
            // other escape is an ordinary CharacterEscape.
            if (IsClassSetReservedPunctuator(Peek()))
            {
                single = _src[_pos++];
                return Fold(SingleCodePoint(single.Value));
            }

            single = ReadCharacterEscape();
            return Fold(SingleCodePoint(single.Value));
        }

        // ClassSetCharacter: a source character that is neither a syntax character nor
        // half of a reserved double punctuator.
        if (IsClassSetSyntaxCharacter(c))
            throw new RegexSyntaxException($"'{c}' must be escaped inside a 'v'-mode class", _pos);
        if (IsClassSetReservedDoublePunctuator(c) && PeekAt(1) == c)
            throw new RegexSyntaxException($"'{c}{c}' is reserved inside a 'v'-mode class", _pos);

        single = ReadSourceCodePoint();
        return Fold(SingleCodePoint(single.Value));
    }

    /// <summary>Parses <c>\q{ ClassString (| ClassString)* }</c>, the cursor on the <c>'q'</c>.</summary>
    private ClassSetValue ParseClassStringDisjunction()
    {
        _usedSetOperations = true;
        var start = _pos;
        _pos++; // consume 'q'
        if (Peek() != '{')
            throw new RegexSyntaxException("Expected '{' after \\q", _pos);
        _pos++;

        var value = new ClassSetValue();
        var current = new StringBuilder();

        void Flush()
        {
            var text = current.ToString();
            current.Clear();
            // A one-character alternative is an ordinary member of the code-point set;
            // only longer (and empty) alternatives need string matching.
            if (text.Length == 1 || (text.Length == 2 && char.IsSurrogatePair(text, 0)))
                value.Set.AddCodePoint(char.ConvertToUtf32(text, 0));
            else if (!value.Strings.Contains(text))
                value.Strings.Add(text);
        }

        while (true)
        {
            if (AtEnd)
                throw new RegexSyntaxException("Unterminated \\q{…}", start);

            var c = Peek();
            if (c == '}')
            {
                _pos++;
                Flush();
                break;
            }
            if (c == '|')
            {
                _pos++;
                Flush();
                continue;
            }

            if (c == '\\')
            {
                _pos++;
                if (IsClassSetReservedPunctuator(Peek()))
                {
                    current.Append(_src[_pos++]);
                    continue;
                }
                current.Append(FromCodePoint(ReadCharacterEscape()));
                continue;
            }

            if (IsClassSetSyntaxCharacter(c))
                throw new RegexSyntaxException($"'{c}' must be escaped inside \\q{{…}}", _pos);

            current.Append(FromCodePoint(ReadSourceCodePoint()));
        }

        return Fold(value);
    }

    private static ClassSetValue SingleCodePoint(int codePoint)
    {
        var value = new ClassSetValue();
        value.Set.AddCodePoint(codePoint);
        return value;
    }

    /// <summary>§22.2.1 MaybeSimpleCaseFolding — a no-op unless <c>i</c> is in effect.</summary>
    private ClassSetValue Fold(ClassSetValue value)
    {
        if (!IgnoreCase)
            return value;

        value.Set = value.Set.Map(static cp => CaseFolding.Canonicalize(cp, unicode: true));
        for (var i = 0; i < value.Strings.Count; i++)
            value.Strings[i] = FoldString(value.Strings[i]);
        return value;
    }

    private static string FoldString(string value)
    {
        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var cp = (int)value[i];
            if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
            {
                cp = char.ConvertToUtf32(value[i], value[i + 1]);
                i++;
            }
            sb.Append(FromCodePoint(CaseFolding.Canonicalize(cp, unicode: true)));
        }
        return sb.ToString();
    }

    /// <summary>
    /// The UTF-16 text of one code point. Unlike <see cref="char.ConvertFromUtf32"/> this
    /// accepts a lone surrogate, which is a perfectly ordinary member of a class.
    /// </summary>
    private static string FromCodePoint(int codePoint)
        => codePoint <= 0xFFFF ? ((char)codePoint).ToString() : char.ConvertFromUtf32(codePoint);

    private static bool IsClassSetSyntaxCharacter(char c)
        => c is '(' or ')' or '[' or ']' or '{' or '}' or '/' or '-' or '\\' or '|';

    private static bool IsClassSetReservedPunctuator(char c)
        => c is '&' or '-' or '!' or '#' or '%' or ',' or ':' or ';' or '<' or '=' or '>' or '@' or '`' or '~';

    private static bool IsClassSetReservedDoublePunctuator(char c)
        => c is '&' or '!' or '#' or '$' or '%' or '*' or '+' or ',' or '.' or ':' or ';'
            or '<' or '=' or '>' or '?' or '@' or '^' or '`' or '~';

    // ----- Shared escape readers ---------------------------------------------

    private ClassEscape? TryReadClassEscape()
    {
        switch (Peek())
        {
            case 'd': _pos++; return ClassEscape.Digit;
            case 'D': _pos++; return ClassEscape.NonDigit;
            case 'w': _pos++; return ClassEscape.Word;
            case 'W': _pos++; return ClassEscape.NonWord;
            case 's': _pos++; return ClassEscape.Space;
            case 'S': _pos++; return ClassEscape.NonSpace;
            default: return null;
        }
    }

    /// <summary>Reads a CharacterEscape (the '\' already consumed): \n \r \xHH \uHHHH \u{…} \cX \0 identity.</summary>
    private int ReadCharacterEscape()
    {
        // A '\' with nothing after it: \ is never a pattern character of its own
        // (ECMA-262 §22.2.1 — every production that admits it consumes a second
        // character). Reported as a syntax error rather than running off the end of
        // the string in ReadSourceCodePoint below.
        if (AtEnd)
            throw new RegexSyntaxException("Trailing '\\' at end of pattern", _pos);

        var c = Peek();
        switch (c)
        {
            case 'n': _pos++; return '\n';
            case 'r': _pos++; return '\r';
            case 't': _pos++; return '\t';
            case 'f': _pos++; return '\f';
            case 'v': _pos++; return '\v';
            case '0' when !char.IsDigit(PeekAt(1)): _pos++; return 0;
            case 'x' when _unicode || HasHexDigits(1, 2): return ReadHexEscape(2);
            case 'u' when _unicode || HasHexDigits(1, 4): return ReadUnicodeEscape();
            case 'c': return ReadControlEscape();
        }

        // Annex B: outside Unicode mode a `\x`/`\u` that does not introduce a well-formed
        // escape is the identity escape of that letter, so /\u{2}/ is "uu" and
        // /[\u{1F600}-\u{1F64F}]/ is a class over `u{1F60}`… with an out-of-order range.
        if (c is 'x' or 'u')
        {
            _pos++;
            return c;
        }

        // IdentityEscape: in Unicode mode only syntax characters and '/' may be
        // escaped; outside Unicode mode any non-IdentifierPart char is allowed.
        if (_unicode && !IsSyntaxChar(c) && c != '/')
            throw new RegexSyntaxException($"Invalid escape \\{c} in Unicode mode", _pos);

        return ReadSourceCodePoint();
    }

    private int ReadLegacyOctalOrIdentity()
    {
        // Legacy octal escape \ooo (non-Unicode only); fall back to identity.
        var c = Peek();
        if (c is >= '0' and <= '7')
        {
            var value = 0;
            var count = 0;
            while (count < 3 && Peek() is >= '0' and <= '7')
            {
                var next = value * 8 + (Peek() - '0');
                if (next > 0xFF) break;
                value = next;
                _pos++;
                count++;
            }
            return value;
        }
        return ReadSourceCodePoint();
    }

    private int ReadControlEscape()
    {
        _pos++; // consume 'c'
        var c = Peek();
        if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
        {
            _pos++;
            return c % 32;
        }
        // Not a valid control letter: treat '\c' as a literal backslash-c in legacy mode.
        if (_unicode)
            throw new RegexSyntaxException("Invalid \\c escape", _pos);
        return '\\';
    }

    private int ReadHexEscape(int digits)
    {
        _pos++; // consume 'x' or 'u'
        var value = 0;
        for (var i = 0; i < digits; i++)
        {
            var d = HexValue(Peek());
            if (d < 0)
                throw new RegexSyntaxException("Invalid hexadecimal escape", _pos);
            value = value * 16 + d;
            _pos++;
        }
        return value;
    }

    /// <summary>True when <paramref name="count"/> hexadecimal digits sit at the given offset.</summary>
    private bool HasHexDigits(int offset, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (HexValue(PeekAt(offset + i)) < 0)
                return false;
        }
        return true;
    }

    private int ReadUnicodeEscape()
    {
        // Caller is positioned at 'u'. The braced form exists only in Unicode mode; the
        // Annex B grammar reads `\u{…}` as the identity escape `u` followed by a literal
        // brace (or a quantifier), which the caller has already decided.
        if (_unicode && PeekAt(1) == '{')
        {
            _pos += 2; // consume 'u{'
            var value = 0;
            var any = false;
            while (HexValue(Peek()) >= 0)
            {
                value = value * 16 + HexValue(Peek());
                if (value > 0x10FFFF)
                    throw new RegexSyntaxException("Unicode code point out of range", _pos);
                _pos++;
                any = true;
            }
            if (!any || Peek() != '}')
                throw new RegexSyntaxException("Invalid \\u{…} escape", _pos);
            _pos++; // consume '}'
            return value;
        }

        var hi = ReadHexEscape(4);

        // In Unicode mode, a \uHHHH high surrogate followed by a \uHHHH low
        // surrogate is a single astral code point.
        if (_unicode && hi >= 0xD800 && hi <= 0xDBFF && Peek() == '\\' && PeekAt(1) == 'u')
        {
            var save = _pos;
            _pos++; // consume '\'
            var lo = ReadHexEscape(4);
            if (lo >= 0xDC00 && lo <= 0xDFFF)
                return char.ConvertToUtf32((char)hi, (char)lo);
            _pos = save; // not a low surrogate; leave it for the next atom
        }

        return hi;
    }

    /// <summary>
    /// Reads a <c>\p{…}</c> / <c>\P{…}</c> escape (the <c>'\'</c> already consumed) and
    /// adds what it matches to <paramref name="set"/>.
    /// </summary>
    /// <remarks>
    /// <c>\P{X}</c> is not "the class is negated": §22.2.2.9 gives it the CharSet that is
    /// the complement of <c>X</c>, and the ordinary case closure then runs over that
    /// complement. Adding the complement as a positive member keeps
    /// <c>[\P{L}\p{Nd}]</c> — where only one member is complemented — expressible.
    /// </remarks>
    private bool AddPropertyEscape(CharSet set, bool allowStrings, bool complement = true)
    {
        var start = _pos;
        var negated = Peek() == 'P';
        _pos++; // consume 'p'/'P'
        if (Peek() != '{')
            throw new RegexSyntaxException("Expected '{' after \\p", _pos);
        _pos++;
        var sb = new StringBuilder();
        while (!AtEnd && Peek() != '}')
            sb.Append(_src[_pos++]);
        if (Peek() != '}')
            throw new RegexSyntaxException("Unterminated \\p{…}", _pos);
        _pos++;

        var spec = sb.ToString();
        string name;
        string? value = null;
        var eq = spec.IndexOf('=');
        if (eq >= 0)
        {
            name = spec[..eq];
            value = spec[(eq + 1)..];
        }
        else
        {
            name = spec;
        }

        var resolved = UnicodeCharSets.ResolveProperty(name, value)
            ?? throw new RegexSyntaxException($"Unknown Unicode property escape \\p{{{spec}}}", start);

        if (resolved.IsStringProperty)
        {
            // A property of strings can only widen a match, so the spec forbids both
            // complementing it and putting it anywhere a complement will be taken.
            if (negated)
                throw new RegexSyntaxException($"\\P{{{spec}}} may not negate a property of strings", start);
            if (!allowStrings)
                throw new RegexSyntaxException(
                    $"\\p{{{spec}}} matches strings, so it needs the 'v' flag and an un-negated class", start);
            foreach (var sequence in resolved.Strings)
                set.AddString(sequence);
        }

        set.AddSet(negated && complement ? resolved.CodePoints.Complement() : resolved.CodePoints);
        return negated;
    }

    private string ReadGroupName()
    {
        var sb = new StringBuilder();
        while (!AtEnd && Peek() != '>')
        {
            // RegExpIdentifierName allows \u escapes; decode them so the stored
            // name matches a JS string key.
            if (Peek() == '\\' && PeekAt(1) == 'u')
            {
                _pos++; // consume '\'
                sb.Append(char.ConvertFromUtf32(ReadUnicodeEscape()));
                continue;
            }
            sb.Append(_src[_pos++]);
        }
        if (Peek() != '>')
            throw new RegexSyntaxException("Unterminated group name", _pos);
        _pos++; // consume '>'
        var name = sb.ToString();
        if (name.Length == 0)
            throw new RegexSyntaxException("Empty group name", _pos);
        return name;
    }

    // ----- Low-level cursor helpers ------------------------------------------

    /// <summary>
    /// True once the cursor is past the last character. Every end-of-pattern test
    /// goes through this rather than through <see cref="Peek"/>'s <c>'\0'</c>: U+0000
    /// is a perfectly ordinary pattern character, so comparing against the sentinel
    /// would end the parse in the middle of a pattern that merely contains one.
    /// </summary>
    private bool AtEnd => _pos >= _src.Length;

    /// <summary>True when a character exists <paramref name="offset"/> ahead of the cursor.</summary>
    private bool HasAt(int offset) => _pos + offset < _src.Length;

    private char Peek() => _pos < _src.Length ? _src[_pos] : '\0';
    private char PeekAt(int offset) => _pos + offset < _src.Length ? _src[_pos + offset] : '\0';

    private void Expect(char c)
    {
        if (Peek() != c)
            throw new RegexSyntaxException($"Expected '{c}'", _pos);
        _pos++;
    }

    /// <summary>Reads one source code point, combining a surrogate pair in Unicode mode.</summary>
    private int ReadSourceCodePoint()
    {
        var c = _src[_pos++];
        if (_unicode && char.IsHighSurrogate(c) && _pos < _src.Length && char.IsLowSurrogate(_src[_pos]))
        {
            var lo = _src[_pos++];
            return char.ConvertToUtf32(c, lo);
        }
        return c;
    }

    private bool TryReadDecimal(out int value)
    {
        value = 0;
        if (!char.IsAsciiDigit(Peek()))
            return false;
        long acc = 0;
        while (char.IsAsciiDigit(Peek()))
        {
            acc = acc * 10 + (Peek() - '0');
            if (acc > int.MaxValue)
                acc = int.MaxValue;
            _pos++;
        }
        value = (int)acc;
        return true;
    }

    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };

    private static bool IsSyntaxChar(char c)
        => c is '^' or '$' or '\\' or '.' or '*' or '+' or '?' or '(' or ')'
            or '[' or ']' or '{' or '}' or '|';
}
