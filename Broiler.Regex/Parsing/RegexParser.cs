// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   72
// Annotated:        72/72
// Exempt:           24
// Human-reviewed:   0/72
// IP risk:          Low
// Security risk:    High
// Criteria:         67/56
// Resource impact:  7/10 max
// Unverified:       72
//
// GENERATED - DO NOT EDIT MANUALLY

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Broiler.Regex.Ast;
using Broiler.Regex.Unicode;
using Broiler.Unicode.Algorithms;
using CodePointSet = Broiler.Regex.Ast.CodePointSet;

namespace Broiler.Regex.Parsing;

/// <summary>
/// Recursive-descent parser for the ECMAScript regular-expression
/// <c>Pattern</c> grammar (ECMA-262 §22.2.1). Produces a <see cref="RegexNode"/>
/// tree consumed by the matcher.
/// </summary>
/// <remarks>
/// Internal since task 2.9 of SJL-11.2, with the tree it produces: its work grows with the pattern and it took no meter
/// (convention 4), and it threw for a pattern it refuses (convention 3). <see cref="BroilerRegex.Compile"/> and the
/// validate entry are the public parses, under the caller's nesting limit, each answering a status.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=7; Fingerprint=52F8B3
// Broiler-Falsified-If: a pattern of 50,000 nested '(' characters ends the process with a stack overflow instead of raising a catchable exception
// Broiler-Human:        PENDING
internal sealed class RegexParser
{
    private readonly string _src;
    private readonly bool _unicode;
    private readonly bool _unicodeSets;

    /// <summary>Whether the flags carry <c>u</c> and <c>v</c> both, which ParsePattern refuses before it reads the pattern.</summary>
    private readonly bool _unicodeWithUnicodeSets;
    private readonly RegexUnicode _data;
    private int _pos;
    private int _captureCount;

    /// <summary>The caller's nesting limit, checked before each descent (convention 5, decision D6).</summary>
    private readonly RegexNesting _nesting;

    /// <summary>The depth of the level the cursor is in: the pattern's own Disjunction is 1 (<see cref="RegexNesting"/>).</summary>
    private int _depth = 1;

    /// <summary>
    /// Whether the parse builds the sets its classes match (a compilation's parse) or only reads the pattern and checks its
    /// early errors (the validate entry's, task 2.8 of SJL-11.2; decision D18 of Broiler.VM's JSD-0081: "no expansion"):
    /// validating, no property escape is expanded, nothing is folded, no class's set or strings are built and no set algebra
    /// is done, while every refusal is made at the same place and in the same order as a compilation's parse makes it.
    /// </summary>
    private readonly bool _expand;

    /// <summary>
    /// The compilation's account (requirement R2, task 2.5 of SJL-11.2): the pattern's read and every expansion the parse
    /// makes, a property escape's, a class's set algebra and its strings, are charged there before their work.
    /// </summary>
    private readonly CompileCharge _account;

    /// <summary>
    /// The effective <c>i</c> flag at the cursor. A <c>v</c>-mode class folds its operands
    /// while it is being built (§22.2.2.9.5 MaybeSimpleCaseFolding), and <c>\w</c>'s set widens
    /// under <c>iu</c> (§22.2.2.9.3), so both need the flag as an inline modifier group
    /// leaves it — not only as the pattern's own flags set it.
    /// </summary>
    private readonly Stack<bool> _ignoreCase = new();

    /// <summary>Total number of capturing groups, available after parsing.</summary>
    public int CaptureCount { get; private set; }

    /// <summary>
    /// The pattern's group names, available after parsing: each name with its groups, in the order of its first group
    /// (requirement R6; ECMA-262 §22.2.1.9, GroupSpecifiersThatMatch). Before a parse, and for an instance made by the
    /// public constructor, which does not parse, it holds no name.
    /// </summary>
    /// <remarks>
    /// Made once, when the pattern has been read, and never changed: the compiled pattern that keeps it, and its matches,
    /// share it (convention 10). Task 2.6 of SJL-11.2 reshaped it from a map of each name to one group.
    /// </remarks>
    public RegexGroupNames GroupNames { get; private set; }

    /// <summary>Each name's groups, ascending, as the parse declares them.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=1769F7
    // Broiler-Falsified-If: two group names that differ only in case, such as (?<a>x)(?<A>y), are read as one name
    // Broiler-Human:        PENDING
    private readonly Dictionary<string, List<int>> _groupsByName = new(StringComparer.Ordinal);

    /// <summary>The names, in the order of their first groups.</summary>
    private readonly List<string> _namesInOrder = [];

    /// <summary>The position of the <c>(</c> of each name's latest group, which a later group of the name is checked against.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=9FF007
    // Broiler-Falsified-If: (?<a>x)|(?<A>y) is refused as a duplicate name because the names are compared without regard to case
    // Broiler-Human:        PENDING
    private readonly Dictionary<string, int> _latestGroupOf = new(StringComparer.Ordinal);

    /// <summary>
    /// The Disjunctions the cursor is inside, outermost first: where each began and where its current Alternative began,
    /// both source positions. A group of a name repeated later is in a closed Alternative of one of them exactly when the
    /// two cannot both participate (<see cref="MightBothParticipate"/>).
    /// </summary>
    private readonly List<(int Start, int AlternativeStart)> _disjunctions = [];

    private int _totalCaptureGroups;

    /// <summary>
    /// Whether the pattern holds a GroupSpecifier anywhere: Annex B's ParsePattern then reads it with
    /// <c>[+NamedCaptureGroups]</c>, under which <c>\k</c> is a GroupName reference rather than the letter
    /// (ECMA-262 §B.1.2).
    /// </summary>
    private bool _hasGroupNames;

    /// <summary>
    /// Every <c>\k&lt;name&gt;</c> the parse has read, with the position of its backslash: a reference may
    /// precede its group, so whether the name is declared is decided once the whole pattern is read.
    /// </summary>
    private readonly List<(string Name, int Position)> _namedReferences = [];

    /// <summary>A parser of <paramref name="pattern"/> under <paramref name="flags"/>, reading its Unicode data from <paramref name="tables"/>.</summary>
    /// <param name="pattern">The pattern's source text.</param>
    /// <param name="flags">The pattern's flags.</param>
    /// <param name="tables">The Unicode owner's tables (requirement R11), of the version the provider's algorithms read.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="tables"/> is null.</exception>
    /// <exception cref="System.ArgumentException">The tables are of another Unicode version or lack a table the algorithms read.</exception>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=0F5AC3
    // Broiler-Human:        PENDING
    public RegexParser(string pattern, RegexFlags flags, IUnicodeTables tables)
        : this(pattern, flags, new RegexUnicode(tables), new RegexNesting(BroilerRegex.MaximumNestingLimit))
    {
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=1; Fingerprint=5DCA3B
    // Broiler-Falsified-If: flags holding only v leave the parser outside Unicode mode, so /\u{41}/v is not read as the code point A
    // Broiler-Human:        PENDING
    internal RegexParser(string pattern, RegexFlags flags, RegexUnicode data, RegexNesting nesting, bool expand = true)
    {
        _expand = expand;
        _src = pattern ?? "";
        _unicode = flags.IsUnicodeMode();
        _unicodeSets = (flags & RegexFlags.UnicodeSets) != 0;
        _unicodeWithUnicodeSets = _unicodeSets && (flags & RegexFlags.Unicode) != 0;
        _data = data;
        _account = data.Account;
        _nesting = nesting;
        _ignoreCase.Push((flags & RegexFlags.IgnoreCase) != 0);
        GroupNames = new RegexGroupNames([], [], 0);
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=2052D2
    // Broiler-Falsified-If: inside a (?-i:...) group it reports the pattern's own i flag rather than the innermost modifier's value
    // Broiler-Human:        PENDING
    private bool IgnoreCase => _ignoreCase.Peek();

    /// <summary>
    /// Parses <paramref name="pattern"/> under <paramref name="flags"/>, reading its Unicode data from
    /// <paramref name="tables"/>, and answers its tree and its number of capturing groups.
    /// </summary>
    /// <param name="pattern">The pattern's source text.</param>
    /// <param name="flags">The pattern's flags.</param>
    /// <param name="tables">The Unicode owner's tables (requirement R11), of the version the provider's algorithms read.</param>
    /// <param name="captureCount">The number of capturing groups.</param>
    /// <remarks>
    /// The parse descends under <see cref="BroilerRegex.MaximumNestingLimit"/>, and refuses a pattern nested deeper as
    /// it refuses every pattern it does not read: with a <see cref="RegexSyntaxException"/> at the group or class that
    /// opens the level past it. <see cref="BroilerRegex.Compile"/> takes the caller's limit and answers a status.
    /// </remarks>
    /// <exception cref="RegexSyntaxException">The pattern is not a <c>Pattern</c> under the flags, breaks an early error, or nests deeper than <see cref="BroilerRegex.MaximumNestingLimit"/>.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="tables"/> is null.</exception>
    /// <exception cref="System.ArgumentException">The tables are of another Unicode version or lack a table the algorithms read.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=7; Fingerprint=2D8D8D
    // Broiler-Falsified-If: the capture count it returns for /(a)(?<n>b)/ is not 2
    // Broiler-Human:        PENDING
    public static RegexNode Parse(string pattern, RegexFlags flags, IUnicodeTables tables, out int captureCount)
    {
        var data = new RegexUnicode(tables);
        try
        {
            return Parse(pattern, flags, data, new RegexNesting(BroilerRegex.MaximumNestingLimit), out captureCount, out _);
        }
        catch (NestingLimitExceeded refusal)
        {
            throw RegexNesting.AsSyntaxError(refusal);
        }
    }

    /// <exception cref="NestingLimitExceeded">The pattern nests deeper than <paramref name="nesting"/>.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=7; Fingerprint=9F8916
    // Broiler-Falsified-If: the capture count it returns for /(a)(?<n>b)/ is not 2, or the name map does not send n to 2
    // Broiler-Human:        PENDING
    internal static RegexNode Parse(string pattern, RegexFlags flags, RegexUnicode data, RegexNesting nesting,
        out int captureCount, out RegexGroupNames groupNames)
    {
        var parser = new RegexParser(pattern, flags, data, nesting);
        var node = parser.ParsePattern();
        captureCount = parser.CaptureCount;
        groupNames = parser.GroupNames;
        return node;
    }

    /// <summary>
    /// The validate entry's parse (task 2.8 of SJL-11.2; decision D18 of Broiler.VM's JSD-0081): reads
    /// <paramref name="pattern"/> under <paramref name="flags"/> and checks every early error the parser knows, descending
    /// under <paramref name="nesting"/>, and expands nothing (<see cref="_expand"/>), so its work is the pattern's read and
    /// the lookups its early errors make, of a property's name and of a group name's identifier classes.
    /// </summary>
    /// <exception cref="RegexSyntaxException">The pattern is not a <c>Pattern</c> under the flags, or breaks an early error.</exception>
    /// <exception cref="NestingLimitExceeded">The pattern nests deeper than <paramref name="nesting"/>.</exception>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1 and JSD-0081 decision D18; IP=None; Security=High; Resources=3; Fingerprint=2909F1
    // Broiler-Falsified-If: a pattern a compilation's parse refuses is accepted, one it accepts is refused, a refusal's code or position differs from the compilation's, or validating expands a property escape, folds a set or builds a class's set
    // Broiler-Human:        PENDING
    internal static void Validate(string pattern, RegexFlags flags, RegexUnicode data, RegexNesting nesting)
        => new RegexParser(pattern, flags, data, nesting, expand: false).ParsePattern();

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=7; Fingerprint=DA462E
    // Broiler-Falsified-If: a pattern with unconsumed trailing text such as 'a)' returns a tree instead of throwing RegexSyntaxException
    // Broiler-Human:        PENDING
    private RegexNode ParsePattern()
    {
        // §22.2.3.4 ParsePattern, step 1: "If v is true and u is true", a SyntaxError, before the pattern is read. A flags
        // text refuses the pair before it reaches the parser (RegexFlagsText.Parse); a flags value is refused here, with
        // the same code and message at no place in a flags text.
        if (_unicodeWithUnicodeSets)
            throw new RegexSyntaxException(RegexDiagnostic.OfFlags(RegexDiagnosticCode.UnicodeWithUnicodeSets, 'v', -1));

        // The pattern's units, one per code unit, before the pre-scan reads each of them once and the parse reads them
        // again (requirement R2: the parse's own work, a constant amount per code unit).
        _account.Charge(CompileSite.Pattern, _src.Length);

        // Pre-scan to learn the total capture-group count and the declared group
        // names. ECMAScript needs both up front: in Unicode mode `\1` is always a
        // back-reference (its validity depends on the total count), and a forward
        // `\k<name>` must resolve to a name declared later in the pattern.
        PreScan();

        var node = ParseDisjunction();
        if (_pos != _src.Length)
            throw Refuse(RegexDiagnosticCode.UnmatchedParenthesis, _pos, "Unexpected '" + _src[_pos] + "' in pattern");

        // §22.2.1.1: AtomEscape :: k GroupName is a syntax error when no GroupSpecifier of
        // the pattern declares that name. The parser decides it, so a parse alone refuses
        // \k<b>(?<a>x) as a compilation does.
        foreach (var (name, position) in _namedReferences)
        {
            if (!_groupsByName.ContainsKey(name))
                throw Refuse(RegexDiagnosticCode.UndefinedGroupName, position, "Reference to the undeclared group name '" + name + "'", name);
        }

        CaptureCount = _captureCount;
        var groups = new int[_namesInOrder.Count][];
        for (var at = 0; at < groups.Length; at++)
            groups[at] = [.. _groupsByName[_namesInOrder[at]]];
        GroupNames = new RegexGroupNames([.. _namesInOrder], groups, _captureCount);
        return node;
    }

    /// <summary>
    /// Counts the capturing groups and notes whether any is named, before the parse proper: in Unicode mode
    /// <c>\1</c> is always a back-reference, valid only up to the total count, and whether <c>\k</c> names a group
    /// depends on a GroupSpecifier anywhere in the pattern, later ones included.
    /// </summary>
    /// <remarks>
    /// It reads no name. A name is decoded, validated against the identifier classes and checked for a
    /// duplicate where the parse proper meets it, so a name spelled with an escape is compared decoded.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=3; Fingerprint=83B68B
    // Broiler-Falsified-If: a '(' inside a class, or after a backslash, is counted as a capturing group, so /[(]\1/u is accepted
    // Broiler-Human:        PENDING
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
                if (i + 3 < _src.Length && _src[i + 2] == '<' && _src[i + 3] != '=' && _src[i + 3] != '!')
                {
                    _totalCaptureGroups++;
                    _hasGroupNames = true;
                }
                continue;
            }

            _totalCaptureGroups++;
        }
    }

    // ----- Disjunction / Alternative -----------------------------------------

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=3B256C
    // Broiler-Falsified-If: a pattern of 100,000 nested '(?:' groups exhausts the native stack in this recursion instead of raising a catchable exception
    // Broiler-Human:        PENDING
    private RegexNode ParseDisjunction()
    {
        // Disjunction :: Alternative | Disjunction: each Alternative after a '|' begins a new region of this Disjunction,
        // which a group of a name repeated in it is checked against (MightBothParticipate, §22.2.1.4).
        _disjunctions.Add((_pos, _pos));
        var alternatives = new List<RegexNode> { ParseAlternative() };
        while (Peek() == '|')
        {
            _pos++;
            _disjunctions[^1] = (_disjunctions[^1].Start, _pos);
            alternatives.Add(ParseAlternative());
        }

        _disjunctions.RemoveAt(_disjunctions.Count - 1);
        return alternatives.Count == 1 ? alternatives[0] : new DisjunctionNode(alternatives);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=FBAC09
    // Broiler-Falsified-If: a raw U+0000 character inside the pattern ends the alternative early, so the text after it is lost or rejected
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=36E555
    // Broiler-Falsified-If: a quantifier after a look-behind, ^, $, \b or \B, or after any assertion under u or v, is accepted instead of throwing
    // Broiler-Human:        PENDING
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
                throw Refuse(RegexDiagnosticCode.QuantifiedAssertion, _pos, "Nothing to repeat before quantifier");

            return TryApplyQuantifier(assertion);
        }

        var atom = ParseAtom();
        return TryApplyQuantifier(atom);
    }

    /// <summary>
    /// Parses an assertion, reporting through <paramref name="quantifiable"/> whether
    /// Annex B admits a quantifier after it.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=3E08EC
    // Broiler-Falsified-If: 100,000 nested '(?=' look-aheads exhaust the native stack instead of raising a catchable exception
    // Broiler-Human:        PENDING
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
                var open = _pos;
                _pos += 3;
                Descend(open);
                var body = ParseDisjunction();
                Expect(')', RegexDiagnosticCode.UnterminatedGroup);
                Ascend();
                quantifiable = true;
                return new LookaroundNode(body, behind: false, negative: third == '!');
            }
            if (third == '<' && PeekAt(3) is '=' or '!')
            {
                var negative = PeekAt(3) == '!';
                var open = _pos;
                _pos += 4;
                Descend(open);
                var body = ParseDisjunction();
                Expect(')', RegexDiagnosticCode.UnterminatedGroup);
                Ascend();
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
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=C002FB
    // Broiler-Falsified-If: outside u and v, /^{2}/ is accepted or /^{a}/ is rejected, because the cursor is not restored after the brace probe
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=3; Fingerprint=EAFCF6
    // Broiler-Falsified-If: a '{' that opens no valid quantifier, as in /a{,5}/ outside u, is consumed instead of being left as literal text
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=3; Fingerprint=AB419B
    // Broiler-Falsified-If: {3,2} is accepted, or {2147483648} wraps to a negative count instead of saturating at int.MaxValue
    // Broiler-Human:        PENDING
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
            throw Refuse(RegexDiagnosticCode.QuantifierOutOfOrder, save, "Quantifier {n,m} with m < n");

        return true;
    }

    // ----- Atom ---------------------------------------------------------------

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=ED3398
    // Broiler-Falsified-If: under u or v an unescaped ']', '{' or '}' is returned as a literal character instead of raising a syntax error, or without them /{2}/ is read as the text {2}
    // Broiler-Human:        PENDING
    private RegexNode ParseAtom()
    {
        // The cursor is on a character that is not '|' or ')': ParseAlternative, the only caller of ParseTerm, ends the
        // alternative at the pattern's end and at either of them, and TryParseAssertion moves the cursor only when it
        // answers an assertion. So neither the end nor those two characters reach here (task 2.8 of SJL-11.2 removed the
        // refusals this method had for them, which no pattern could reach and so had no code to give).
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
            case '*':
            case '+':
            case '?':
                throw Refuse(RegexDiagnosticCode.NothingToRepeat, _pos, "Nothing to repeat before '" + c + "'", c.ToString());
            case ']':
            case '{':
            case '}':
                // Annex B lets these stand for themselves; Unicode mode drops that
                // extension, so an unescaped one (including a '{' that did not open a
                // valid quantifier) is a syntax error rather than a literal.
                if (_unicode)
                    throw Refuse(RegexDiagnosticCode.LoneSyntaxCharacter, _pos, "'" + c + "' must be escaped in Unicode mode", c.ToString());
                // Annex B's ExtendedAtom :: InvalidBracedQuantifier (§B.1.2.1): a well-formed
                // {n}, {n,} or {n,m} where an atom should be, as in /{2}/ or /a|{2}/, is a
                // syntax error; only a '{' that opens no quantifier is a character.
                if (c == '{' && HasQuantifier())
                    throw Refuse(RegexDiagnosticCode.BracedQuantifierWithoutAtom, _pos, "Nothing to repeat before '{'");
                break;
        }

        // PatternCharacter — a single source character (or astral code point in u-mode).
        var cp = ReadSourceCodePoint();
        return new CharNode(cp);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=429745
    // Broiler-Falsified-If: a capturing group is numbered after the groups nested inside it, so the outer group of /((a)b)/ is not capture 1
    // Broiler-Human:        PENDING
    private RegexNode ParseGroup()
    {
        _pos++; // consume '('
        if (Peek() == '?')
        {
            var second = PeekAt(1);

            // Non-capturing (?:…)
            if (second == ':')
            {
                var open = _pos - 1;
                _pos += 2;
                Descend(open);
                var body = ParseDisjunction();
                Expect(')', RegexDiagnosticCode.UnterminatedGroup);
                Ascend();
                return new GroupNode(body, captureIndex: 0, name: null);
            }

            // Named capturing (?<name>…)
            if (second == '<' && PeekAt(2) != '=' && PeekAt(2) != '!')
            {
                var open = _pos - 1;
                _pos += 2; // consume '?<'
                var name = ReadGroupName();
                var index = ++_captureCount;

                // §22.2.1.1: two GroupSpecifiers may share a name only when MightBothParticipate is false for them
                // (§22.2.1.4), the names compared as decoded, so (?<a>x)(?<\u0061>y) repeats the name a and is refused,
                // and (?<a>x)|(?<\u0061>y) is admitted (requirement R6).
                if (_latestGroupOf.TryGetValue(name, out var earlier) && MightBothParticipate(earlier))
                    throw Refuse(RegexDiagnosticCode.DuplicateGroupName, open, "Duplicate capture group name '" + name + "'", name);
                _latestGroupOf[name] = open;
                if (!_groupsByName.TryGetValue(name, out var named))
                {
                    _groupsByName.Add(name, named = []);
                    _namesInOrder.Add(name);
                }

                named.Add(index);
                Descend(open);
                var body = ParseDisjunction();
                Expect(')', RegexDiagnosticCode.UnterminatedGroup);
                Ascend();
                return new GroupNode(body, index, name);
            }

            // Inline modifier group (?ims-ims:…)
            if (second is 'i' or 'm' or 's' or '-')
                return ParseModifierGroup();

            throw Refuse(RegexDiagnosticCode.InvalidGroup, _pos, "Invalid group");
        }

        // Plain capturing group
        var captureIndex = ++_captureCount;
        Descend(_pos - 1);
        var child = ParseDisjunction();
        Expect(')', RegexDiagnosticCode.UnterminatedGroup);
        Ascend();
        return new GroupNode(child, captureIndex, null);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=1249C8
    // Broiler-Falsified-If: the i flag added by (?i:...) is still in effect after its ')', so a following v-mode class is case-folded
    // Broiler-Human:        PENDING
    private RegexNode ParseModifierGroup()
    {
        var open = _pos - 1;
        _pos++; // consume '?'
        var added = ReadModifierFlags();
        var removed = RegexFlags.None;
        if (Peek() == '-')
        {
            _pos++;
            removed = ReadModifierFlags();
        }

        if (added == RegexFlags.None && removed == RegexFlags.None)
            throw Refuse(RegexDiagnosticCode.ModifiersEmpty, _pos, "Modifier group must add or remove at least one flag");
        if ((added & removed) != 0)
            throw Refuse(RegexDiagnosticCode.ModifierAddedAndRemoved, _pos, "A modifier flag may not be both added and removed");

        Expect(':', RegexDiagnosticCode.InvalidGroup);
        Descend(open);
        _ignoreCase.Push(
            (IgnoreCase || (added & RegexFlags.IgnoreCase) != 0) && (removed & RegexFlags.IgnoreCase) == 0);
        var body = ParseDisjunction();
        _ignoreCase.Pop();
        Expect(')', RegexDiagnosticCode.UnterminatedGroup);
        Ascend();
        return new ModifierGroupNode(body, added, removed);
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=1; Fingerprint=579B30
    // Broiler-Falsified-If: a repeated modifier flag such as (?ii:a) is accepted instead of throwing
    // Broiler-Human:        PENDING
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
                throw Refuse(RegexDiagnosticCode.ModifierRepeated, _pos, "Repeated flag in modifier group", Peek().ToString());
            flags |= flag;
            _pos++;
        }
        return flags;
    }

    // ----- AtomEscape (outside a character class) -----------------------------

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=0E6882
    // Broiler-Falsified-If: under u a numeric escape above the capture count, such as \2 in /(a)\2/u, is accepted instead of throwing
    // Broiler-Human:        PENDING
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
                throw Refuse(RegexDiagnosticCode.BackreferenceOutOfRange, start, "Back-reference \\" + num.ToString(NumberFormatInfo.InvariantInfo) + " to a non-existent group", _src[start.._pos]);
            _pos = start;
            return new CharNode(ReadLegacyOctalOrIdentity());
        }

        // Named back-reference \k<name>. Outside Unicode mode `\k` only introduces one
        // when the pattern declares a group name somewhere; with none, Annex B reads it
        // as the identity escape `k`, so /\k<n>/ is the literal text "k<n>".
        if (c == 'k' && (_unicode || _hasGroupNames))
        {
            var backslash = _pos - 1;
            _pos++;
            if (Peek() != '<')
                throw Refuse(RegexDiagnosticCode.NamedReferenceWithoutName, _pos, "Expected '<' after \\k");
            _pos++;
            var name = ReadGroupName();
            _namedReferences.Add((name, backslash));
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
            if (_expand)
                set.AddEscape(classEscape.Value, IgnoreCase, _unicode, _data);
            return new CharClassNode(set);
        }

        // Unicode property escape \p{…} / \P{…}
        if (c is 'p' or 'P' && _unicode)
        {
            if (_unicodeSets)
                return new CharClassNode(ToCharSet(ReadPropertyOperand(allowStrings: true)));

            var set = new CharSet();
            set.AddSet(ReadPropertyEscapeSet());
            return new CharClassNode(set);
        }

        // Otherwise a single escaped code point (\n, \xHH, \uHHHH, \u{…}, identity).
        return new CharNode(ReadCharacterEscape());
    }

    // ----- Character class [ ... ] -------------------------------------------

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=59BEF2
    // Broiler-Falsified-If: under u a range with a class-escape endpoint such as [\d-z] is accepted instead of throwing
    // Broiler-Human:        PENDING
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

        // The members are gathered and the class's set is built once, at the ']': adding them to the class one
        // by one would make a new set per member. The class escapes' and property escapes' sets are kept whole and
        // joined two by two (CodePointSet.Builder), each join charged to the compilation.
        var members = new CodePointSet.Builder();
        while (true)
        {
            if (AtEnd)
                throw Refuse(RegexDiagnosticCode.UnterminatedClass, _pos, "Unterminated character class");

            var c = Peek();
            if (c == ']')
            {
                _pos++;
                if (_expand)
                    set.AddSet(members.ToSet(_account));
                return set;
            }

            var atom = _pos;
            var first = ReadClassAtom(set, members, out var firstIsLiteral);

            // A range "a-z": only when '-' is followed by another class atom rather than
            // by the closing ']'.
            if (Peek() == '-' && HasAt(1) && PeekAt(1) != ']')
            {
                var dash = _pos;
                _pos++; // consume '-'
                var second = ReadClassAtom(set, members, out var secondIsLiteral);
                if (firstIsLiteral && secondIsLiteral)
                {
                    if (first > second)
                        throw RangeOutOfOrder(first, second, atom);
                    members.AddRange(first, second);
                }
                else if (_unicode)
                {
                    // §22.2.1 early error: an endpoint that is itself a class (\d, \p{L})
                    // is a syntax error. Only Annex B's non-Unicode grammar rescues it as
                    // a literal '-'.
                    throw Refuse(RegexDiagnosticCode.ClassRangeEndpointIsClass, dash, "A character-class escape may not be an endpoint of a range");
                }
                else
                {
                    if (firstIsLiteral)
                        members.Add(first);
                    members.Add('-');
                    if (secondIsLiteral)
                        members.Add(second);
                }
            }
            else if (firstIsLiteral)
            {
                members.Add(first);
            }
        }
    }

    /// <summary>
    /// Reads one class atom. Adds a class escape (\d…) or a property escape to <paramref name="members"/>, marking the
    /// latter on <paramref name="set"/>, and returns their sentinel as a non-literal; returns a literal code point otherwise.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=4; Fingerprint=415B7D
    // Broiler-Falsified-If: inside a class \b yields anything but U+0008, \- throws under u instead of yielding a hyphen, or without u [\c1] holds anything but U+0011
    // Broiler-Human:        PENDING
    private int ReadClassAtom(CharSet set, CodePointSet.Builder members, out bool isLiteral)
    {
        var c = Peek();
        if (c == '\\')
        {
            _pos++;
            var classEscape = TryReadClassEscape();
            if (classEscape.HasValue)
            {
                if (_expand)
                    members.AddAll(_data.EscapeSet(classEscape.Value, IgnoreCase, _unicode));
                isLiteral = false;
                return -1;
            }
            if (Peek() is 'p' or 'P' && _unicode)
            {
                var property = ReadPropertyEscapeSet();
                if (_expand)
                    members.AddAll(property);
                set.UsesPropertyEscape = true;
                isLiteral = false;
                return -1;
            }
            if (Peek() == 'b') { _pos++; isLiteral = true; return '\b'; } // \b is backspace in a class
            // `ClassEscape :: -` — inside a class `\-` is a literal hyphen even in Unicode
            // mode, where the standalone IdentityEscape would reject it.
            if (Peek() == '-') { _pos++; isLiteral = true; return '-'; }
            // Annex B's ClassEscape[~UnicodeMode] :: c ClassControlLetter (§B.1.2): inside a class
            // a control escape also takes a decimal digit or the low line, so [\c1] is U+0011
            // and [\c_] U+001F.
            if (!_unicode && Peek() == 'c' && PeekAt(1) is (>= '0' and <= '9') or '_')
            {
                var letter = PeekAt(1);
                _pos += 2;
                isLiteral = true;
                return letter % 32;
            }
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
    // `\P{Lu}` under `vi` (matches neither `A` nor `a`) and under `ui` (matches both: the
    // case closure of the complement holds `a`, which `A` folds to). `[^\p{Lu}]` matches
    // neither in both modes, as CharacterSetMatcher's invert (§22.2.2.7.1) refuses every
    // code point that folds like an uppercase letter.

    /// <summary>
    /// One evaluated class-set operand: code points plus string members, and §22.2.1.8 MayContainStrings of the
    /// syntax it was read from, which the early errors of a negated class read (§22.2.1.1). That is not whether
    /// strings are left: <c>\q{ab}--\q{ab}</c> leaves none and may contain strings.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1.8; IP=None; Security=Low; Resources=1; Fingerprint=096551
    // Broiler-Human:        PENDING
    private sealed class ClassSetValue
    {
        public CodePointSet Set = new();
        public ClassStrings Strings = new();
        public bool MayContainStrings;
    }

    private bool _usedSetOperations;
    private bool _usedPropertyEscape;

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=8B4152
    // Broiler-Falsified-If: UsesPropertyEscape stays set from an earlier class, so the second class of /[\p{L}][a]/v is flagged as using a property escape
    // Broiler-Human:        PENDING
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
    /// membership directly rather than walking the subject's fold orbit. Each string is charged its code units as the
    /// class takes it (<see cref="CompileSite.Strings"/>), and the class's order, longest first, as a sort of them
    /// (<see cref="CompileSite.StringOrder"/>), before each.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=4; Fingerprint=40F78F
    // Broiler-Falsified-If: a set built while i is in effect is not marked CaseFolded, so /[\q{ab}]/vi does not match 'AB'
    // Broiler-Human:        PENDING
    private CharSet ToCharSet(ClassSetValue value)
    {
        var set = new CharSet { CaseFolded = IgnoreCase };
        set.AddSet(value.Set);
        var units = 0L;
        foreach (var member in value.Strings.InOrder)
        {
            _account.Charge(CompileSite.Strings, CompileCharge.StringUnits(member));
            set.AddString(member);
            units += CompileCharge.StringUnits(member);
        }

        if (value.Strings.Count > 1)
            _account.Charge(CompileSite.StringOrder, units * CompileCharge.SortFactor(value.Strings.Count));
        set.SortStrings();
        return set;
    }

    /// <summary>
    /// §22.2.2.9 CharacterComplement: the complement is taken against AllCharacters, which
    /// under <c>v</c> with <c>i</c> holds only the code points that fold to themselves.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=Medium; Resources=3; Fingerprint=22D2A9
    // Broiler-Falsified-If: under v with i the complement it returns contains a code point that does not fold to itself, such as 'A'
    // Broiler-Human:        PENDING
    private CodePointSet ComplementOperand(CodePointSet set)
    {
        if (!_expand)
            return set;

        var universe = _unicodeSets && IgnoreCase ? _data.CanonicalCharacters : UnicodeCharSets.FullRange;
        return universe.Subtract(set, _account);
    }

    /// <summary>Reads <c>\d \D \w \W \s \S</c> as a class-set operand: fold, then complement.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=Medium; Resources=3; Fingerprint=D0DA1A
    // Broiler-Falsified-If: under vi the set returned for \W contains a code point that does not fold to itself, such as U+00C9
    // Broiler-Human:        PENDING
    private ClassSetValue ReadClassEscapeOperand(ClassEscape escape)
    {
        if (!_expand)
            return new ClassSetValue();

        var value = new ClassSetValue { Set = _data.EscapeSet(UnicodeCharSets.PositiveEscape(escape), IgnoreCase, _unicode) };
        Fold(value);
        if (UnicodeCharSets.IsComplementEscape(escape))
            value.Set = ComplementOperand(value.Set);
        return value;
    }

    /// <summary>Reads <c>\p{…}</c> / <c>\P{…}</c> as a class-set operand: fold, then complement.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=High; Resources=6; Fingerprint=7EA416
    // Broiler-Falsified-If: under vi \P{Ll} is complemented before its members are folded, so /\P{Ll}/vi matches 'a'
    // Broiler-Human:        PENDING
    private ClassSetValue ReadPropertyOperand(bool allowStrings)
    {
        var resolved = ResolvePropertyEscape(allowStrings, out var negated, out var strings);
        if (resolved is null)
            return new ClassSetValue { MayContainStrings = strings };

        var value = new ClassSetValue { Set = resolved.CodePoints, MayContainStrings = resolved.IsStringProperty };
        foreach (var sequence in resolved.Strings)
            value.Strings.Add(sequence, _account);
        Fold(value);
        if (negated)
            value.Set = ComplementOperand(value.Set);
        return value;
    }

    /// <summary>Parses <c>[ ^? ClassContents ]</c>, the cursor sitting on the <c>'['</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=018294
    // Broiler-Falsified-If: a v-mode class nested 100,000 '[' deep exhausts the native stack instead of raising a catchable exception
    // Broiler-Human:        PENDING
    private ClassSetValue ParseNestedClass()
    {
        var start = _pos;
        Descend(start);
        _pos++; // consume '['

        var negated = false;
        if (Peek() == '^')
        {
            negated = true;
            _pos++;
        }

        // The expression returns at the class's ']' or at the pattern's end, never before another character: a union
        // reads operands up to either, and an intersection or a subtraction refuses whatever else follows its last one.
        var value = ParseClassSetExpression();

        if (AtEnd)
            throw Refuse(RegexDiagnosticCode.UnterminatedClass, start, "Unterminated character class");
        _pos++; // consume ']'
        Ascend();

        if (!negated)
            return value;

        // §22.2.1.1 early error: a complemented class may not contain strings, because the
        // complement of a set of strings is not a set this grammar can describe. It reads
        // MayContainStrings of the contents' syntax (§22.2.1.8), not the strings the set
        // operations leave, so [^\q{ab}--\q{ab}] is refused though no string is left.
        if (value.MayContainStrings)
            throw Refuse(RegexDiagnosticCode.NegatedClassMayContainStrings, start, "A negated 'v'-mode class may not contain strings");

        return new ClassSetValue { Set = ComplementOperand(value.Set) };
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=7; Fingerprint=068B3E
    // Broiler-Falsified-If: a v-mode class repeating \p{RGI_Emoji} a thousand times makes billions of string comparisons, because each union scans the accumulated string list once per member
    // Broiler-Human:        PENDING
    private ClassSetValue ParseClassSetExpression()
    {
        if (AtEnd || Peek() == ']')
            return new ClassSetValue();

        var start = _pos;
        var accumulated = ParseClassSetRangeOrOperand(out var range);

        // A ClassIntersection and a ClassSubtraction begin with a ClassSetOperand (§22.2.1); a ClassSetRange is a
        // ClassUnion's alone. After a range, '--' and '&&' are no operator, and the union's first RejectMixedOperator
        // refuses them, so [a-z--b] and [a-a&&c] are the refusal of a mixed expression, at the range.
        if (!range && Peek() == '&' && PeekAt(1) == '&')
            return ParseClassIntersection(accumulated, start);
        if (!range && Peek() == '-' && PeekAt(1) == '-')
            return ParseClassSubtraction(accumulated, start);

        // A union's operands are gathered and its set is built once, at the end, joined two by two; its strings are
        // added to an insertion-ordered hash set (ClassStrings), one hash per string, where a list searched before each
        // addition made a union of n strings O(n²) comparisons.
        var union = new CodePointSet.Builder();
        union.AddAll(accumulated.Set);
        while (!AtEnd && Peek() != ']')
        {
            RejectMixedOperator(start);
            var operand = ParseClassSetRangeOrOperand(out _);
            accumulated.MayContainStrings |= operand.MayContainStrings;
            if (!_expand)
                continue;
            union.AddAll(operand.Set);
            accumulated.Strings.UnionWith(operand.Strings, _account);
        }

        if (_expand)
            accumulated.Set = union.ToSet(_account);
        return accumulated;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=A442B9
    // Broiler-Falsified-If: a string present in only one operand survives the intersection, so /[\q{ab}&&\q{cd}]/v matches 'ab'
    // Broiler-Human:        PENDING
    private ClassSetValue ParseClassIntersection(ClassSetValue first, int start)
    {
        _usedSetOperations = true;
        var accumulated = first;
        while (Peek() == '&' && PeekAt(1) == '&')
        {
            _pos += 2;
            if (Peek() == '&')
                throw Refuse(RegexDiagnosticCode.ClassSetAmpersandAfterIntersection, _pos, "'&&&' is not a class-set operator");
            var operand = ParseClassSetOperand(out _);
            accumulated = !_expand
                ? new ClassSetValue { MayContainStrings = accumulated.MayContainStrings && operand.MayContainStrings }
                : new ClassSetValue
            {
                Set = accumulated.Set.Intersect(operand.Set, _account),
                Strings = accumulated.Strings.Intersect(operand.Strings, _account),
                // §22.2.1.8: an intersection may contain strings only when every operand may.
                MayContainStrings = accumulated.MayContainStrings && operand.MayContainStrings,
            };
        }

        RejectAfterLastOperand(start);
        return accumulated;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=E01B6A
    // Broiler-Falsified-If: a string of the right operand survives the subtraction, so /[\q{ab|cd}--\q{ab}]/v matches 'ab'
    // Broiler-Human:        PENDING
    private ClassSetValue ParseClassSubtraction(ClassSetValue first, int start)
    {
        _usedSetOperations = true;
        var accumulated = first;
        while (Peek() == '-' && PeekAt(1) == '-')
        {
            _pos += 2;
            var operand = ParseClassSetOperand(out _);
            accumulated = !_expand
                ? new ClassSetValue { MayContainStrings = accumulated.MayContainStrings }
                : new ClassSetValue
            {
                Set = accumulated.Set.Subtract(operand.Set, _account),
                Strings = accumulated.Strings.Except(operand.Strings, _account),
                // §22.2.1.8: a subtraction may contain strings exactly when its first operand may.
                MayContainStrings = accumulated.MayContainStrings,
            };
        }

        RejectAfterLastOperand(start);
        return accumulated;
    }

    /// <summary>
    /// A class-set expression is a union, an intersection or a subtraction — never a
    /// mixture, so an operator met between a union's operands is a syntax error rather
    /// than a silently reassociated expression.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=0; Fingerprint=44118D
    // Broiler-Falsified-If: [ab--c], [ab&&c] or [a-z--b] under v is accepted instead of throwing
    // Broiler-Human:        PENDING
    private void RejectMixedOperator(int start)
    {
        if (AtEnd || Peek() == ']')
            return;
        if ((Peek() == '&' && PeekAt(1) == '&') || (Peek() == '-' && PeekAt(1) == '-'))
            throw Refuse(RegexDiagnosticCode.ClassSetMixedOperators, start, "A class-set expression may not mix union, '&&' and '--' without nesting");
    }

    /// <summary>
    /// A ClassIntersection or a ClassSubtraction ends at its last operand (§22.2.1), which only the class's <c>]</c> may
    /// follow, or the pattern's end, which <see cref="ParseNestedClass"/> then refuses as an unterminated class. An
    /// operator of the other kind, or any operand, would mix the expression with another, so it is refused here, before
    /// the class consumes its <c>]</c>.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=0; Fingerprint=55670B
    // Broiler-Falsified-If: [a--bc or [a&&ac under v compiles, or [a--b&&c], [a&&b--c] or [a--b[c]] is accepted instead of throwing
    // Broiler-Human:        PENDING
    private void RejectAfterLastOperand(int start)
    {
        if (!AtEnd && Peek() != ']')
            throw Refuse(RegexDiagnosticCode.ClassSetMixedOperators, start, "A class-set expression may not mix union, '&&' and '--' without nesting");
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=7B17DF
    // Broiler-Falsified-If: a range endpoint that is a class escape or \q{...}, as in [a-\d] under v, is accepted instead of throwing
    // Broiler-Human:        PENDING
    private ClassSetValue ParseClassSetRangeOrOperand(out bool range)
    {
        // range says whether a ClassSetRange was read, which only a ClassUnion may hold.
        range = false;
        var start = _pos;
        var operand = ParseClassSetOperand(out var single);

        // A range needs a single character on both sides; `a--b` is a subtraction and
        // `a-]` ends the class, so neither starts one.
        if (single == null || Peek() != '-' || PeekAt(1) == '-' || PeekAt(1) == ']' || !HasAt(1))
            return operand;

        _pos++; // consume '-'
        _ = ParseClassSetOperand(out var highSingle);
        if (highSingle == null)
            throw Refuse(RegexDiagnosticCode.ClassSetRangeEndpointNotCharacter, start, "A class-set range needs a single character on both sides");
        if (single.Value > highSingle.Value)
            throw RangeOutOfOrder(single.Value, highSingle.Value, start);

        range = true;
        return _expand ? Fold(new ClassSetValue { Set = CodePointSet.Of(single.Value, highSingle.Value) }) : new ClassSetValue();
    }

    /// <summary>
    /// Parses one <c>ClassSetOperand</c>. <paramref name="single"/> receives the code
    /// point when the operand was a lone character, which is the only shape a range end
    /// may take.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=24DFA0
    // Broiler-Falsified-If: an unescaped '(' or a doubled punctuator such as '!!' is accepted as a literal inside a v-mode class
    // Broiler-Human:        PENDING
    private ClassSetValue ParseClassSetOperand(out int? single)
    {
        single = null;
        if (AtEnd)
            throw Refuse(RegexDiagnosticCode.UnterminatedClass, _pos, "Unterminated character class");

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

            single = ReadClassSetCharacter(escaped: true, "a 'v'-mode class");
            return Fold(SingleCodePoint(single.Value));
        }

        single = ReadClassSetCharacter(escaped: false, "a 'v'-mode class");
        return Fold(SingleCodePoint(single.Value));
    }

    /// <summary>
    /// Reads one ClassSetCharacter (§22.2.1) and answers the code point it denotes, a lone surrogate as well as any other
    /// (§22.2.2.9 CompileToCharSet): a source character that is neither a class-set syntax character nor the first of a
    /// reserved double punctuator, or, when <paramref name="escaped"/> says its <c>\</c> is read, <c>\b</c>, a reserved
    /// punctuator, or a CharacterEscape. A class-set operand and a <c>\q{…}</c> alternative read their characters here
    /// alike; <paramref name="inside"/> names which in a refusal.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=0; Fingerprint=955367
    // Broiler-Falsified-If: \b is refused, a reserved double punctuator such as && is read as two characters, or an unescaped ( or | is read as a character, inside a v-mode class or a \q{...} alternative
    // Broiler-Human:        PENDING
    private int ReadClassSetCharacter(bool escaped, string inside)
    {
        if (escaped)
        {
            if (Peek() == 'b')
            {
                _pos++;
                return '\b';
            }

            // `\` followed by one of the reserved punctuators is that punctuator; every
            // other escape is an ordinary CharacterEscape.
            if (IsClassSetReservedPunctuator(Peek()))
                return _src[_pos++];
            return ReadCharacterEscape();
        }

        var c = Peek();
        if (IsClassSetSyntaxCharacter(c))
            throw Refuse(RegexDiagnosticCode.ClassSetUnescapedSyntaxCharacter, _pos, "'" + c + "' must be escaped inside " + inside, c.ToString());
        if (IsClassSetReservedDoublePunctuator(c) && PeekAt(1) == c)
            throw Refuse(RegexDiagnosticCode.ClassSetReservedDoublePunctuator, _pos, "'" + c + c + "' is reserved inside " + inside, _src.Substring(_pos, 2));
        return ReadSourceCodePoint();
    }

    /// <summary>Parses <c>\q{ ClassString (| ClassString)* }</c>, the cursor on the <c>'q'</c>.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=6; Fingerprint=FADBD6
    // Broiler-Falsified-If: an empty alternative, as in \q{a|}, is dropped instead of making the class match the empty string
    // Broiler-Human:        PENDING
    private ClassSetValue ParseClassStringDisjunction()
    {
        _usedSetOperations = true;
        var start = _pos;
        _pos++; // consume 'q'
        if (Peek() != '{')
            throw Refuse(RegexDiagnosticCode.ClassStringDisjunctionWithoutBrace, _pos, "Expected '{' after \\q");
        _pos++;

        var value = new ClassSetValue();
        var singles = new CodePointSet.Builder();
        var current = new StringBuilder();
        var characters = 0;
        var last = 0;
        var leadThenTrail = false;

        // §22.2.2.10 CompileClassSetString: an alternative is the sequence of the code points its ClassSetCharacters
        // denote, one each, read as code points here and never inferred from the alternative's UTF-16 text.
        void Flush()
        {
            // §22.2.1.8: a ClassString may contain strings unless it is exactly one ClassSetCharacter,
            // counted as written, so an empty one or \uD83D\u{DE00} (a lead escape, then a trail) may.
            if (characters != 1)
                value.MayContainStrings = true;

            // One character is an ordinary member of the code-point set, a lone surrogate as well as any other; a longer
            // (or empty) alternative needs string matching, added once, by its hash, as its UTF-16 text, which reads back
            // as its code points. Except where a lead surrogate is followed by a trail one, as \uD83D\u{DE00} writes them:
            // the text would read as the one astral code point they are not, and the alternative matches nothing, as a
            // subject read by code point (§22.2.7.2) never holds a lead followed by a trail, so it is not kept.
            // Validating, MayContainStrings above is all an early error reads of the alternative, and nothing is kept.
            if (_expand && characters == 1)
                singles.Add(last);
            else if (_expand && !leadThenTrail)
                value.Strings.Add(current.ToString(), _account);

            characters = 0;
            leadThenTrail = false;
            current.Clear();
        }

        while (true)
        {
            if (AtEnd)
                throw Refuse(RegexDiagnosticCode.UnterminatedClassStringDisjunction, start, "Unterminated \\q{…}");

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
                _pos++;
            var codePoint = ReadClassSetCharacter(escaped: c == '\\', "\\q{…}");
            if (characters > 0 && last is >= 0xD800 and <= 0xDBFF && codePoint is >= 0xDC00 and <= 0xDFFF)
                leadThenTrail = true;
            characters++;
            last = codePoint;
            if (_expand)
                current.Append(FromCodePoint(codePoint));
        }

        if (_expand)
            value.Set = singles.ToSet(_account);
        return Fold(value);
    }

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=13D6DB
    // Broiler-Human:        PENDING
    private ClassSetValue SingleCodePoint(int codePoint)
        => _expand ? new() { Set = CodePointSet.Of(codePoint, codePoint) } : new();

    /// <summary>
    /// §22.2.2.9.5 MaybeSimpleCaseFolding — a no-op unless <c>i</c> is in effect. The set is folded by
    /// <see cref="RegexUnicode.SimpleCaseFold"/>, whose work, and charge, is in proportion to its ranges, and each string
    /// is charged its code units before it is folded (<see cref="CompileSite.CaseFold"/>); two strings that fold alike
    /// are kept once.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=Medium; Resources=6; Fingerprint=47A2EB
    // Broiler-Falsified-If: under vi each \p{L} operand is folded one code point at a time, so a class repeating it 10,000 times makes over a billion fold calls during parse
    // Broiler-Human:        PENDING
    private ClassSetValue Fold(ClassSetValue value)
    {
        if (!IgnoreCase || !_expand)
            return value;

        value.Set = _data.SimpleCaseFold(value.Set);
        if (value.Strings.Count > 0)
        {
            var folded = new ClassStrings();
            foreach (var text in value.Strings.InOrder)
            {
                _account.Charge(CompileSite.CaseFold, CompileCharge.StringUnits(text));
                folded.Add(FoldString(text), _account);
            }

            value.Strings = folded;
        }

        return value;
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=Medium; Resources=3; Fingerprint=561D11
    // Broiler-Falsified-If: a surrogate pair is folded as two code units, so /[\q{\u{10400}x}]/vi does not match the text U+10428 followed by x
    // Broiler-Human:        PENDING
    private string FoldString(string value)
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
            sb.Append(FromCodePoint(_data.Canonicalize(cp, unicode: true)));
        }
        return sb.ToString();
    }

    /// <summary>
    /// The UTF-16 text of one code point. Unlike <see cref="char.ConvertFromUtf32"/> this
    /// accepts a lone surrogate, which is a perfectly ordinary member of a class.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=1; Fingerprint=C770F9
    // Broiler-Falsified-If: a lone surrogate code point such as 0xD800 throws instead of returning its single UTF-16 unit
    // Broiler-Human:        PENDING
    private static string FromCodePoint(int codePoint)
        => codePoint <= 0xFFFF ? ((char)codePoint).ToString() : char.ConvertFromUtf32(codePoint);

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=0; Fingerprint=596E48
    // Broiler-Falsified-If: it returns false for one of ( ) [ ] { } / - \ | or true for any other character
    // Broiler-Human:        PENDING
    private static bool IsClassSetSyntaxCharacter(char c)
        => c is '(' or ')' or '[' or ']' or '{' or '}' or '/' or '-' or '\\' or '|';

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=0; Fingerprint=1CB1BF
    // Broiler-Falsified-If: it returns true for '$' or '^', or false for one of & - ! # % , : ; < = > @ ` ~
    // Broiler-Human:        PENDING
    private static bool IsClassSetReservedPunctuator(char c)
        => c is '&' or '-' or '!' or '#' or '%' or ',' or ':' or ';' or '<' or '=' or '>' or '@' or '`' or '~';

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=0; Fingerprint=8B8981
    // Broiler-Falsified-If: it returns true for '-' or '|', or false for one of & ! # $ % * + , . : ; < = > ? @ ^ ` ~
    // Broiler-Human:        PENDING
    private static bool IsClassSetReservedDoublePunctuator(char c)
        => c is '&' or '!' or '#' or '$' or '%' or '*' or '+' or ',' or '.' or ':' or ';'
            or '<' or '=' or '>' or '?' or '@' or '^' or '`' or '~';

    // ----- Shared escape readers ---------------------------------------------

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=0; Fingerprint=4612E7
    // Broiler-Falsified-If: a letter other than d, D, w, W, s or S, such as b, is consumed as a class escape
    // Broiler-Human:        PENDING
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

    /// <summary>Reads a CharacterEscape (the '\' already consumed): \n \r \xHH \uHHHH \u{…} \cX \0, legacy octal, identity.</summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=3; Fingerprint=827CE4
    // Broiler-Falsified-If: under u an identity escape of a non-syntax character such as \a or \- outside a class is accepted instead of throwing
    // Broiler-Human:        PENDING
    private int ReadCharacterEscape()
    {
        // A '\' with nothing after it: \ is never a pattern character of its own
        // (ECMA-262 §22.2.1 — every production that admits it consumes a second
        // character). Reported as a syntax error rather than running off the end of
        // the string in ReadSourceCodePoint below.
        if (AtEnd)
            throw Refuse(RegexDiagnosticCode.TrailingBackslash, _pos, "Trailing '\\' at end of pattern");

        var c = Peek();
        switch (c)
        {
            case 'n': _pos++; return '\n';
            case 'r': _pos++; return '\r';
            case 't': _pos++; return '\t';
            case 'f': _pos++; return '\f';
            case 'v': _pos++; return '\v';
            // CharacterEscape :: 0 [lookahead ∉ DecimalDigit], and DecimalDigit is the ASCII
            // digits alone (§22.2.1): \0 before U+0661 ARABIC-INDIC DIGIT ONE is NUL, then that digit.
            case '0' when !char.IsAsciiDigit(PeekAt(1)): _pos++; return 0;
            case 'x' when _unicode || HasHexDigits(1, 2): return ReadHexEscape(2);
            case 'u' when _unicode || HasHexDigits(1, 4): return ReadUnicodeEscape(_unicode);
            case 'c': return ReadControlEscape();
        }

        // Annex B's LegacyOctalEscapeSequence, outside Unicode mode only (§B.1.2): \0 before an
        // ASCII digit, as in \01 or \08 (NUL, then 8), and in a class \1 to \7. An atom's \1 to
        // \9 never reaches here; ParseAtomEscape reads it as a back-reference or an octal first.
        if (!_unicode && c is >= '0' and <= '7')
            return ReadLegacyOctalOrIdentity();

        // Annex B: outside Unicode mode a `\x`/`\u` that does not introduce a well-formed
        // escape is the identity escape of that letter, so /\u{2}/ is "uu" and
        // /[\u{1F600}-\u{1F64F}]/ is a class over `u{1F60}`… with an out-of-order range.
        if (c is 'x' or 'u')
        {
            _pos++;
            return c;
        }

        // Annex B's IdentityEscape[+NamedCaptureGroups] excludes k (§B.1.2), but only an atom
        // reads [+NamedCaptureGroups]: no production passes the parameter into a CharacterClass,
        // so a class's ClassEscape reads ~NamedCaptureGroups and SourceCharacterIdentityEscape
        // admits k, and [\k<a>](?<a>x) is a class of four letters beside a group, as
        // [\k<a>] alone is (decision D-s5 of Broiler.VM's JSD-0081, 2026-11-01; until task
        // 2.12 of SJL-11.2 this was refused with NamedReferenceInClass). An atom's \k never
        // reaches here in a pattern that declares a group name; ParseAtomEscape reads it first.

        // IdentityEscape: in Unicode mode only syntax characters and '/' may be
        // escaped; outside Unicode mode any character but c may be.
        if (_unicode && !IsSyntaxChar(c) && c != '/')
            throw Refuse(RegexDiagnosticCode.InvalidIdentityEscape, _pos, "Invalid escape \\" + c + " in Unicode mode", c.ToString());

        return ReadSourceCodePoint();
    }

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=F85821
    // Broiler-Falsified-If: an octal escape above 0377, such as \400, is read as one code point greater than 0xFF instead of \40 followed by '0'
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=0; Fingerprint=54B1D6
    // Broiler-Falsified-If: outside u, /\c1/ does not match the text backslash-c-1 because the c is consumed along with the backslash
    // Broiler-Human:        PENDING
    private int ReadControlEscape()
    {
        // The cursor is on the 'c'. CharacterEscape :: c AsciiLetter (§22.2.1).
        var c = PeekAt(1);
        if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
        {
            _pos += 2;
            return c % 32;
        }

        if (_unicode)
            throw Refuse(RegexDiagnosticCode.InvalidControlEscape, _pos + 1, "Invalid \\c escape");

        // Annex B's ExtendedAtom :: \ [lookahead = c] and ClassAtomNoDash :: \ [lookahead = c]
        // (§B.1.2): the backslash alone. The 'c' is not consumed, so it is read next as a
        // character of its own and /\c1/ is the text backslash, c, 1.
        return '\\';
    }

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=1; Fingerprint=0B4138
    // Broiler-Falsified-If: a non-hex character inside the fixed-width run, as in \x4g under u, is accepted instead of throwing
    // Broiler-Human:        PENDING
    private int ReadHexEscape(int digits)
    {
        _pos++; // consume 'x' or 'u'
        var value = 0;
        for (var i = 0; i < digits; i++)
        {
            var d = HexValue(Peek());
            if (d < 0)
                throw Refuse(RegexDiagnosticCode.InvalidHexEscape, _pos, "Invalid hexadecimal escape");
            value = value * 16 + d;
            _pos++;
        }
        return value;
    }

    /// <summary>True when <paramref name="count"/> hexadecimal digits sit at the given offset.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=44C9C9
    // Broiler-Falsified-If: outside u, \x4 at the end of the pattern is decoded as a hex escape instead of the identity escape x followed by 4
    // Broiler-Human:        PENDING
    private bool HasHexDigits(int offset, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (HexValue(PeekAt(offset + i)) < 0)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Reads a <c>RegExpUnicodeEscapeSequence</c>, the cursor on its <c>u</c>, and answers its CharacterValue.
    /// </summary>
    /// <remarks>
    /// Under <paramref name="unicodeMode"/> (a pattern under <c>u</c> or <c>v</c>, and a group name in every mode)
    /// the braced form <c>\u{…}</c> is read, and only two four-digit escapes form a surrogate pair:
    /// <c>u HexLeadSurrogate \u HexTrailSurrogate</c>. A braced escape never joins one, so <c>\uD83D\u{DC38}</c> is
    /// two characters, and a four-digit lead escape followed by any other <c>\u</c> is a lone lead surrogate.
    /// Without it the escape is four hexadecimal digits, one code unit.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=3; Fingerprint=115CDF
    // Broiler-Falsified-If: under u, \uD83D\u{41} is rejected as an invalid hexadecimal escape instead of reading a lone lead surrogate followed by A, or \uD83D\u{DC38} is read as the one code point U+1F438
    // Broiler-Human:        PENDING
    private int ReadUnicodeEscape(bool unicodeMode)
    {
        // Caller is positioned at 'u'. The braced form exists only in Unicode mode; the
        // Annex B grammar reads `\u{…}` as the identity escape `u` followed by a literal
        // brace (or a quantifier), which the caller has already decided.
        if (unicodeMode && PeekAt(1) == '{')
        {
            _pos += 2; // consume 'u{'
            var value = 0;
            var any = false;
            while (HexValue(Peek()) >= 0)
            {
                value = value * 16 + HexValue(Peek());
                if (value > 0x10FFFF)
                    throw Refuse(RegexDiagnosticCode.CodePointOutOfRange, _pos, "Unicode code point out of range");
                _pos++;
                any = true;
            }
            if (!any || Peek() != '}')
                throw Refuse(RegexDiagnosticCode.InvalidBracedEscape, _pos, "Invalid \\u{…} escape");
            _pos++; // consume '}'
            return value;
        }

        var hi = ReadHexEscape(4);

        // In Unicode mode a \uHHHH lead surrogate followed by a \uHHHH trail surrogate — four
        // digits each, never a braced escape — is one astral code point.
        if (unicodeMode && hi is >= 0xD800 and <= 0xDBFF && Peek() == '\\' && PeekAt(1) == 'u' && HasHexDigits(2, 4))
        {
            var lo = 0;
            for (var i = 2; i < 6; i++)
                lo = lo * 16 + HexValue(PeekAt(i));
            if (lo is >= 0xDC00 and <= 0xDFFF)
            {
                _pos += 6; // consume '\uHHHH'
                return char.ConvertToUtf32((char)hi, (char)lo);
            }
        }

        return hi;
    }

    /// <summary>
    /// Reads a <c>\p{…}</c> / <c>\P{…}</c> escape outside a <c>v</c>-mode class-set operand (the <c>'\'</c> already
    /// consumed) and answers the code points it matches, or, when validating, the empty set, having expanded nothing.
    /// </summary>
    /// <remarks>
    /// <c>\P{X}</c> is not "the class is negated": §22.2.2.9 gives it the CharSet that is
    /// the complement of <c>X</c>, and the ordinary case closure then runs over that
    /// complement. Answering the complement as a positive member keeps
    /// <c>[\P{L}\p{Nd}]</c> — where only one member is complemented — expressible. Outside <c>v</c> a property of
    /// strings is refused, so the answer holds no string. The complement is charged to the compilation.
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.2.9; IP=Low; Security=High; Resources=4; Fingerprint=7ED14F
    // Broiler-Falsified-If: \P{L} answers the letters, or \p{RGI_Emoji} under u is accepted instead of throwing
    // Broiler-Human:        PENDING
    private CodePointSet ReadPropertyEscapeSet()
    {
        var resolved = ResolvePropertyEscape(allowStrings: false, out var negated, out _);
        if (resolved is null)
            return CodePointSet.Empty();
        return negated ? resolved.CodePoints.Complement(_account) : resolved.CodePoints;
    }

    /// <summary>
    /// Reads a <c>\p{…}</c> / <c>\P{…}</c> escape (the <c>'\'</c> already consumed), checks its early errors, and answers
    /// what the property matches, uncomplemented, resolved through the provider and charged to the compilation
    /// (<see cref="CompileSite.Property"/>); when validating, null, the property's name having been looked up and nothing
    /// expanded. <paramref name="negated"/> says whether it was written <c>\P</c>, and <paramref name="strings"/> whether
    /// it names a property of strings, which is §22.2.1.8 MayContainStrings of the escape.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The early errors first</b> (§22.2.1.1; catalogue rows EE0201 to EE0205): the name is classified by the provider's
    /// exact lookups (<see cref="RegexUnicode.ClassifyProperty"/>), which copy no set, and a property of strings is refused
    /// where it may not stand before anything is expanded, so a compilation refuses <c>\P{RGI_Emoji}</c> without first
    /// charging its 2,760 strings (task 2.8 of SJL-11.2; before it, the expansion came first).
    /// </para>
    /// <para>
    /// <b>Which name is unknown.</b> In <c>\p{name=value}</c>, a name other than the six of ECMA-262's table of non-binary
    /// property aliases is EE0201's refusal, and an unknown value of one of them EE0202's; a lone name is EE0203's.
    /// </para>
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1.1 and s22.2.2.9; IP=Low; Security=High; Resources=4; Fingerprint=382B2E
    // Broiler-Falsified-If: a negated property of strings such as \P{RGI_Emoji} under v is accepted instead of throwing, \p{RGI_Emoji} reports IsStringProperty false, \p{Scx=Greek} is refused as an unknown value or \p{Script=Grek_} as an unknown name, or validating copies a set
    // Broiler-Human:        PENDING
    private UnicodeCharSets.PropertyMatch? ResolvePropertyEscape(bool allowStrings, out bool negated, out bool strings)
    {
        var start = _pos;
        negated = Peek() == 'P';
        _pos++; // consume 'p'/'P'
        if (Peek() != '{')
            throw Refuse(RegexDiagnosticCode.PropertyEscapeWithoutBrace, _pos, "Expected '{' after \\p");
        _pos++;
        var sb = new StringBuilder();
        while (!AtEnd && Peek() != '}')
            sb.Append(_src[_pos++]);
        if (Peek() != '}')
            throw Refuse(RegexDiagnosticCode.UnterminatedPropertyEscape, _pos, "Unterminated \\p{…}");
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

        var kind = _data.ClassifyProperty(name, value);
        if (kind == PropertyKind.Unknown)
        {
            var code = value is null ? RegexDiagnosticCode.UnknownLonePropertyName
                : name is "General_Category" or "gc" or "Script" or "sc" or "Script_Extensions" or "scx" ? RegexDiagnosticCode.UnknownPropertyValue
                : RegexDiagnosticCode.UnknownPropertyName;
            throw Refuse(code, start, "Unknown Unicode property escape \\p{" + spec + "}", spec);
        }

        strings = kind == PropertyKind.Strings;
        if (strings)
        {
            // A property of strings can only widen a match, so the spec forbids both
            // complementing it and putting it anywhere a complement will be taken.
            if (negated)
                throw Refuse(RegexDiagnosticCode.NegatedPropertyOfStrings, start, "\\P{" + spec + "} may not negate a property of strings", spec);
            if (!allowStrings)
                throw Refuse(RegexDiagnosticCode.PropertyOfStringsOutsideUnicodeSets, start,
                    "\\p{" + spec + "} matches strings, so it needs the 'v' flag and an un-negated class", spec);
        }

        return _expand ? _data.ResolveProperty(name, value)! : null;
    }

    /// <summary>
    /// Reads a GroupName's <c>RegExpIdentifierName</c>, the cursor after its <c>&lt;</c>, through its
    /// <c>&gt;</c>, and answers it decoded (ECMA-262 §22.2.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every mode reads a name the same way: <c>RegExpIdentifierStart</c> and <c>RegExpIdentifierPart</c> take
    /// <c>\ RegExpUnicodeEscapeSequence[+UnicodeMode]</c>, so <c>\u{…}</c> and an escaped surrogate pair are
    /// read without <c>u</c> too, and a raw surrogate pair is one code point without <c>u</c> by the
    /// <c>[~UnicodeMode] UnicodeLeadSurrogate UnicodeTrailSurrogate</c> alternatives.
    /// </para>
    /// <para>
    /// The early errors of §22.2.1.1 hold each code point to the identifier classes: the first must be an
    /// IdentifierStartChar and every later one an IdentifierPartChar, by the provider's
    /// <c>EcmaScriptLexical</c> at its UCD. A lone surrogate is neither, so it is refused as a syntax error.
    /// </para>
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1.1; IP=Low; Security=High; Resources=3; Fingerprint=A4DD96
    // Broiler-Falsified-If: a group name holding a lone surrogate escape, such as (?<\uD800>a), escapes as ArgumentOutOfRangeException instead of RegexSyntaxException, or (?<1>a) or (?<a-b>a) is accepted
    // Broiler-Human:        PENDING
    private string ReadGroupName()
    {
        var name = new StringBuilder();
        var first = true;
        while (true)
        {
            if (AtEnd)
                throw Refuse(RegexDiagnosticCode.UnterminatedGroupName, _pos, "Unterminated group name");
            if (Peek() == '>')
                break;

            var at = _pos;
            int codePoint;
            if (Peek() == '\\')
            {
                _pos++; // consume '\'
                if (Peek() != 'u')
                    throw Refuse(RegexDiagnosticCode.GroupNameInvalidEscape, at, "Only a \\u escape may appear in a group name");
                codePoint = ReadUnicodeEscape(unicodeMode: true);
            }
            else
            {
                var unit = _src[_pos++];
                codePoint = unit;
                if (char.IsHighSurrogate(unit) && !AtEnd && char.IsLowSurrogate(_src[_pos]))
                    codePoint = char.ConvertToUtf32(unit, _src[_pos++]);
            }

            if (first ? !_data.IsIdentifierStart(codePoint) : !_data.IsIdentifierPart(codePoint))
                throw Refuse(first ? RegexDiagnosticCode.GroupNameInvalidStart : RegexDiagnosticCode.GroupNameInvalidPart, at, "Invalid character in a group name");
            name.Append(FromCodePoint(codePoint));
            first = false;
        }

        if (first)
            throw Refuse(RegexDiagnosticCode.EmptyGroupName, _pos, "Empty group name");
        _pos++; // consume '>'
        return name.ToString();
    }

    /// <summary>
    /// MightBothParticipate (ECMA-262 §22.2.1.4) of the group whose <c>(</c> is at <paramref name="earlier"/> and the
    /// group the cursor is declaring: true unless some Disjunction holds the two in different Alternatives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pattern is read left to right, so such a Disjunction is one the cursor is still inside, and the earlier group is
    /// in one of its Alternatives that has closed: between where the Disjunction began and where its current Alternative
    /// began. Those regions of the open Disjunctions are disjoint and in source order (each Disjunction begins inside the
    /// current Alternative of the one around it), so the one to test is that of the innermost open Disjunction that began
    /// at or before the group, found by a binary search over at most the nesting limit's levels.
    /// </para>
    /// <para>
    /// <b>The latest group of a name is enough.</b> The parser checks a group against the latest earlier group of its name
    /// only. If that one cannot participate with it, neither can any group of the name before it: the two earlier groups
    /// were themselves admitted, so a Disjunction holds them in different Alternatives; while it is open the older one is
    /// in a closed Alternative of it too, and once it has closed it lies inside the closed Alternative of an open
    /// Disjunction that holds the latest one, and so does the older one. So every name costs one test per group, a
    /// constant amount of the work the pattern's units pay for (<see cref="CompileSite.Pattern"/>).
    /// </para>
    /// </remarks>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1.4; IP=None; Security=High; Resources=1; Fingerprint=DD9C39
    // Broiler-Falsified-If: (?<a>x)|(?<a>y), (?:(?<a>x)|(?<a>y))|(?<a>z) or ((?<a>x)|(?<a>y))* is refused, or (?<a>x)(?<a>y), (?:(?<a>x)|(?<a>y))(?<a>z), (?<a>x|(?<a>y)) or (?:(?<a>x)|b)(?:(?<a>y)|c) is accepted
    // Broiler-Human:        PENDING
    private bool MightBothParticipate(int earlier)
    {
        int low = 0, high = _disjunctions.Count - 1;
        while (low < high)
        {
            var middle = (low + high + 1) / 2;
            if (_disjunctions[middle].Start <= earlier)
                low = middle;
            else
                high = middle - 1;
        }

        var (start, alternativeStart) = _disjunctions[low];
        return !(start <= earlier && earlier < alternativeStart);
    }

    // ----- Nesting (convention 5, decision D6) ---------------------------------

    /// <summary>
    /// Enters the level a group or a <c>v</c>-mode class opens at <paramref name="open"/>, checking the caller's limit
    /// before the parser descends into it.
    /// </summary>
    /// <exception cref="NestingLimitExceeded">The level would be deeper than the caller's limit.</exception>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D6; IP=None; Security=High; Resources=0; Fingerprint=266817
    // Broiler-Falsified-If: a group opened at the limit's depth is entered instead of refused, so the parser's recursion outgrows the limit
    // Broiler-Human:        PENDING
    private void Descend(int open) => _depth = _nesting.Descend(_depth, open);

    /// <summary>Leaves the level <see cref="Descend"/> entered, once its closing character is read.</summary>
    // Broiler-AI:           Origin=AI; IP=None; Security=Medium; Resources=0; Fingerprint=23E5D5
    // Broiler-Human:        PENDING
    private void Ascend() => _depth--;

    // ----- Low-level cursor helpers ------------------------------------------

    /// <summary>
    /// True once the cursor is past the last character. Every end-of-pattern test
    /// goes through this rather than through <see cref="Peek"/>'s <c>'\0'</c>: U+0000
    /// is a perfectly ordinary pattern character, so comparing against the sentinel
    /// would end the parse in the middle of a pattern that merely contains one.
    /// </summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=3FD96C
    // Broiler-Falsified-If: it reports the end at a U+0000 pattern character rather than only once the cursor is past the last character
    // Broiler-Human:        PENDING
    private bool AtEnd => _pos >= _src.Length;

    /// <summary>True when a character exists <paramref name="offset"/> ahead of the cursor.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=9D370C
    // Broiler-Falsified-If: it returns true for an offset that lands on the source length, so [a- at the end of a pattern reads a range end past the text
    // Broiler-Human:        PENDING
    private bool HasAt(int offset) => _pos + offset < _src.Length;

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=63EC31
    // Broiler-Falsified-If: it throws or reads past the source instead of returning U+0000 once the cursor reaches the source length
    // Broiler-Human:        PENDING
    private char Peek() => _pos < _src.Length ? _src[_pos] : '\0';
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=E7E209
    // Broiler-Falsified-If: an offset past the end throws or reads outside the source instead of returning U+0000
    // Broiler-Human:        PENDING
    private char PeekAt(int offset) => _pos + offset < _src.Length ? _src[_pos + offset] : '\0';

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=16770A
    // Broiler-Falsified-If: the cursor advances when the character does not match, so an unclosed '(' at the end of a pattern does not throw
    // Broiler-Human:        PENDING
    private void Expect(char c, RegexDiagnosticCode code)
    {
        if (Peek() != c)
            throw Refuse(code, _pos, "Expected '" + c + "'");
        _pos++;
    }

    /// <summary>
    /// A refusal of kind <paramref name="code"/> at <paramref name="position"/>, with its default English
    /// <paramref name="message"/> and the <paramref name="argument"/> its code takes (task 2.8 of SJL-11.2).
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=JSD-0081 decision D9; IP=None; Security=Low; Resources=0; Fingerprint=57F7DC
    // Broiler-Human:        PENDING
    private static RegexSyntaxException Refuse(RegexDiagnosticCode code, int position, string message, string? argument = null)
        => new(new RegexDiagnostic(code, position, argument, message));

    /// <summary>
    /// The early error of a class range written high to low (ECMA-262 §22.2.1.1; catalogue rows EE0194, EE0196, EE0208,
    /// EE0211 and EE0213), at the range's first character, whose text from there to the cursor is the argument.
    /// </summary>
    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1.1; IP=None; Security=Low; Resources=0; Fingerprint=9BE9BF
    // Broiler-Falsified-If: [z-a] is refused at a position other than 1, or with an argument other than z-a
    // Broiler-Human:        PENDING
    private RegexSyntaxException RangeOutOfOrder(int low, int high, int start)
        => Refuse(RegexDiagnosticCode.ClassRangeOutOfOrder, start,
            "Range out of order in character class: U+" + low.ToString("X", NumberFormatInfo.InvariantInfo) + "-U+"
            + high.ToString("X", NumberFormatInfo.InvariantInfo),
            _src[start.._pos]);

    /// <summary>Reads one source code point, combining a surrogate pair in Unicode mode.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=855020
    // Broiler-Falsified-If: outside u and v a surrogate pair is read as one code point instead of two code units
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=3; Fingerprint=2D4224
    // Broiler-Falsified-If: a digit run longer than ten digits, such as 99999999999, wraps to a negative or small value instead of saturating at int.MaxValue
    // Broiler-Human:        PENDING
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

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=2D7172
    // Broiler-Falsified-If: it returns a digit value for a character outside 0-9, a-f and A-F; the neighbours are '/', ':', '@', 'G', '`' and 'g'
    // Broiler-Human:        PENDING
    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };

    // Broiler-AI:           Origin=AI; Spec=ECMA-262 s22.2.1; IP=Low; Security=High; Resources=0; Fingerprint=4387A7
    // Broiler-Falsified-If: it returns true for '/', '-' or ',', or false for one of ^ $ \ . * + ? ( ) [ ] { } |
    // Broiler-Human:        PENDING
    private static bool IsSyntaxChar(char c)
        => c is '^' or '$' or '\\' or '.' or '*' or '+' or '?' or '(' or ')'
            or '[' or ']' or '{' or '}' or '|';
}
