using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Broiler.Regex.Fuzz;

/// <summary>
/// Writes one pattern from ECMA-262's Pattern grammar (§22.2.1, with Annex B's additions outside <c>u</c> and <c>v</c>),
/// and beside it a sample: text that the pattern would match, or nearly, from which the case's subject is built so that
/// matches are found as often as refusals and misses.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every construct the parser accepts</b> is a production here: literal characters (astral ones, lone surrogates and
/// characters with case foldings among them), <c>.</c>, every character escape, the class escapes, property escapes
/// with values and properties of strings, classes with ranges and negation, <c>v</c>-mode class sets with nesting,
/// intersection, subtraction and <c>\q{…}</c>, capturing, non-capturing and named groups (names spelled raw, escaped
/// and as surrogate pairs), the four look-arounds, modifier groups, numbered and named back-references, forward and
/// backward, the assertions, and every quantifier form, lazy and greedy, with bounds up to and past
/// <see cref="int.MaxValue"/>. Each production names itself in <see cref="Constructs"/>, which the session counts.
/// </para>
/// <para>
/// <b>The sample is an approximation by design.</b> A class contributes one of its members, an alternation one of its
/// alternatives, a quantifier a few copies; a look-around and an assertion contribute nothing. Its job is to make the
/// subject close to a match, not to be one.
/// </para>
/// </remarks>
internal sealed class PatternGrammar
{
    /// <summary>The longest sample a pattern contributes.</summary>
    private const int SampleCap = 4096;

    private static readonly string[] Characters =
    [
        "a", "b", "c", "x", "y", "z", "A", "B", "Z", "0", "1", "7", "9", " ", "_", ",", "-", "!", "=", ":", "<", ">", "&",
        "#", "%", "~", "@", "'", "\"", "`", ";",
        "\u00E9", "\u00C9", "\u00DF", "\u1E9E", "\u0130", "\u0131", "\u017F", "\u212A", "\u03A3", "\u03C3", "\u03C2", "\u03A9", "\u2126", "\u01C5", "\u01C6", "\u01C4", "\uFB00", "\u0390",
        "\u00A0", "\u3000", "\uFEFF", "\u2028", "\u2029", "\n", "\r", "\t", "\u0000", "\u0085", "\u180E",
        "\u4E2D", "\u0436", "\u03BB", "\u3042", "\u0301", "\u200D",
        "\U0001F600", "\U00010400", "\U00010428", "\U0001D49C", "\U0001F1EB", "\U000E0067",
        "\uD800", "\uDBFF", "\uDC00", "\uDFFF", "\uD83D", "\uDE00",
    ];

    private static readonly string[] SyntaxCharacters = ["^", "$", "\\", ".", "*", "+", "?", "(", ")", "[", "]", "{", "}", "|", "/"];

    /// <summary>Property escapes the specification admits, with a member of each (approximately).</summary>
    private static readonly (string Spec, string Member)[] Properties =
    [
        ("L", "a"), ("Lu", "Q"), ("Ll", "q"), ("Lt", "\u01C5"), ("Letter", "\u03BB"), ("Uppercase_Letter", "\u0416"), ("N", "5"),
        ("Nd", "8"), ("Nl", "\u216B"), ("P", "!"), ("S", "+"), ("Sm", "+"), ("Z", " "), ("Zs", " "), ("Cc", "\u0001"),
        ("Cn", "\u0378"), ("Co", "\uE000"), ("Cs", "\uD800"), ("gc=Lu", "A"), ("General_Category=Decimal_Number", "3"),
        ("Script=Greek", "\u03BB"), ("sc=Grek", "\u03B1"), ("sc=Latn", "b"), ("Script=Cyrillic", "\u0436"),
        ("Script_Extensions=Latin", "c"), ("scx=Hira", "\u3042"), ("Script=Katakana_Or_Hiragana", "\u30FC"), ("sc=Zyyy", "1"),
        ("ASCII", "x"), ("Any", "\U0001F600"), ("Assigned", "a"), ("Alphabetic", "a"), ("White_Space", " "),
        ("Emoji", "\U0001F600"), ("Emoji_Presentation", "\U0001F600"), ("Extended_Pictographic", "\U0001F600"),
        ("Lowercase", "a"), ("Uppercase", "A"), ("ID_Start", "a"), ("ID_Continue", "1"), ("Ideographic", "\u4E2D"),
        ("Hex_Digit", "F"), ("ASCII_Hex_Digit", "f"), ("Changes_When_Casefolded", "A"), ("Cased", "a"), ("Math", "+"),
        ("Dash", "-"), ("Noncharacter_Code_Point", "\uFFFE"), ("Regional_Indicator", "\U0001F1EB"),
        ("Bidi_Mirrored", "("), ("Default_Ignorable_Code_Point", "\u00AD"), ("Variation_Selector", "\uFE0F"),
    ];

    /// <summary>The properties of strings (ECMA-262 table 69), admitted under <c>v</c>, unnegated.</summary>
    private static readonly (string Spec, string Member)[] StringProperties =
    [
        ("RGI_Emoji", "\U0001F468\u200D\U0001F469\u200D\U0001F467"), ("Basic_Emoji", "\u231A"),
        ("Emoji_Keycap_Sequence", "#\uFE0F\u20E3"), ("RGI_Emoji_Flag_Sequence", "\U0001F1EB\U0001F1F7"),
        ("RGI_Emoji_Modifier_Sequence", "\U0001F44D\U0001F3FD"),
        ("RGI_Emoji_Tag_Sequence", "\U0001F3F4\U000E0067\U000E0062\U000E0065\U000E006E\U000E0067\U000E007F"),
        ("RGI_Emoji_ZWJ_Sequence", "\U0001F468\u200D\U0001F469\u200D\U0001F467"),
    ];

    /// <summary>Property escapes one edit away from an admitted one: each is refused (requirement R5's refused set).</summary>
    private static readonly string[] NearMissProperties =
    [
        "l", "lu", "LU", "Is_L", "IsL", "Script=greek", "Script = Greek", "Script_Extensions", "General_Category", "gc",
        "Letter_", "Any=Yes", "ASCII=Y", "sc=Unknown_Script", "Script=Lu", "", "Lu=Lu", "Emoji_Keycap_Sequence=Yes",
        "ScriptExtensions=Latin", "general_category=Lu", "Katakana_Or_Hiragana", "RGIEmoji",
    ];

    /// <summary>Group names: raw, escaped, astral and as surrogate pairs, in every mode.</summary>
    private static readonly string[] Names =
    [
        "a", "b", "n1", "$x", "_y", "\u03C0", "\u540D\u524D", "\\u0061", "\\u{1D49C}", "\U0001D49C", "\\uD835\\uDC9C", "x\\u0031",
        "\u01C4", "ab", "a",
    ];

    private static readonly string[] NearMissNames = ["1a", "", "a-b", "\\uD800", "a b", "\\x41", "\\u{110000}", "\uDC00"];

    private readonly FuzzRandom _random;
    private readonly bool _unicode;
    private readonly bool _sets;
    private readonly bool _ignoreCase;
    private readonly StringBuilder _text = new();
    private readonly List<string> _names = [];
    private readonly Dictionary<int, string> _captured = [];
    private readonly SortedSet<string> _constructs;
    private int _budget;
    private int _groups;

    private PatternGrammar(FuzzRandom random, RegexFlags flags, SortedSet<string> constructs)
    {
        _random = random;
        _unicode = (flags & (RegexFlags.Unicode | RegexFlags.UnicodeSets)) != 0;
        _sets = (flags & RegexFlags.UnicodeSets) != 0;
        _ignoreCase = (flags & RegexFlags.IgnoreCase) != 0;
        _constructs = constructs;
    }

    /// <summary>A pattern under <paramref name="flags"/>, and its sample.</summary>
    internal static (string Pattern, string Sample) Write(FuzzRandom random, RegexFlags flags, SortedSet<string> constructs)
    {
        var grammar = new PatternGrammar(random, flags, constructs) { _budget = random.Between(1, random.Chance(10) ? 60 : 16) };
        var sample = grammar.Disjunction(random.Between(1, random.Chance(10) ? 9 : 5));
        return (grammar._text.ToString(), Cap(sample));
    }

    // ----- Disjunction, Alternative, Term ---------------------------------------------------------------------------

    private string Disjunction(int depth)
    {
        var count = _random.Chance(70) ? 1 : _random.Between(2, 4);
        if (count > 1)
            _constructs.Add("alternation");
        var chosen = _random.Below(count);
        var sample = "";
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
                _text.Append('|');
            var alternative = Alternative(depth);
            if (i == chosen)
                sample = alternative;
        }

        return sample;
    }

    private string Alternative(int depth)
    {
        var terms = _budget <= 0 ? 0 : _random.Chance(8) ? 0 : _random.Between(1, Math.Min(6, _budget));
        if (terms == 0)
            _constructs.Add("empty-alternative");
        var sample = new StringBuilder();
        for (var i = 0; i < terms && _budget > 0; i++)
        {
            _budget--;
            sample.Append(Term(depth));
        }

        return Cap(sample.ToString());
    }

    private string Term(int depth)
    {
        if (_random.Chance(14))
            return Assertion(depth);
        var atom = Atom(depth);
        return _random.Chance(30) ? Quantified(atom) : atom;
    }

    private string Assertion(int depth)
    {
        switch (_random.Below(depth > 0 ? 8 : 4))
        {
            case 0: _text.Append('^'); _constructs.Add("assertion-start"); return "";
            case 1: _text.Append('$'); _constructs.Add("assertion-end"); return "";
            case 2: _text.Append("\\b"); _constructs.Add("word-boundary"); return "";
            case 3: _text.Append("\\B"); _constructs.Add("non-word-boundary"); return "";
            default:
            {
                var kind = _random.Below(4);
                _text.Append(kind switch { 0 => "(?=", 1 => "(?!", 2 => "(?<=", _ => "(?<!" });
                _constructs.Add(kind switch { 0 => "lookahead", 1 => "negative-lookahead", 2 => "lookbehind", _ => "negative-lookbehind" });
                var body = Disjunction(depth - 1);
                _text.Append(')');

                // Annex B's QuantifiableAssertion: outside u and v a look-ahead may carry a quantifier.
                if (kind < 2 && !_unicode && _random.Chance(15))
                {
                    _constructs.Add("quantified-lookahead");
                    Quantifier(out _, out _);
                }

                return kind == 0 && _random.Chance(50) ? body : "";
            }
        }
    }

    // ----- Atoms ----------------------------------------------------------------------------------------------------

    private string Atom(int depth)
    {
        var roll = _random.Below(100);
        if (roll < 30)
            return Literal();
        if (roll < 34)
        {
            _text.Append('.');
            _constructs.Add("dot");
            return _random.Pick(Characters);
        }

        if (roll < 46)
            return Escape();
        if (roll < 52 && _unicode)
            return Property(inClass: false);
        if (roll < 66)
            return _sets ? ClassSet(depth) : Class();
        if (roll < 72)
            return BackReference();
        if (depth > 0)
            return Group(depth);
        return Literal();
    }

    private string Literal()
    {
        var text = _random.Pick(Characters);
        _constructs.Add(Classify(text));

        // Annex B lets ] { } stand for themselves outside u and v; the parser then reads them as characters.
        if (!_unicode && _random.Chance(3))
        {
            var annexB = _random.Pick(["]", "{", "}", "{a}", "{,5}"]);
            _text.Append(annexB);
            _constructs.Add("annex-b-literal-brace");
            return annexB;
        }

        _text.Append(text);
        return Cased(text);
    }

    private string Escape()
    {
        switch (_random.Below(13))
        {
            case 0:
            {
                var escape = _random.Pick(["d", "D", "w", "W", "s", "S"]);
                _text.Append('\\').Append(escape);
                _constructs.Add("class-escape");
                return escape switch
                {
                    "d" => _random.Pick(["0", "5", "9"]),
                    "D" => _random.Pick(["a", "\u00E9", "\U0001F600"]),
                    "w" => _random.Pick(["a", "Z", "_", "4"]),
                    "W" => _random.Pick(["!", "\u00E9", " ", "\u017F"]),
                    "s" => _random.Pick([" ", "\t", "\u00A0", "\uFEFF", "\u2028", "\u3000"]),
                    _ => _random.Pick(["q", "\u180E", "\u0085"]),
                };
            }

            case 1:
            {
                var control = _random.Pick(["f", "n", "r", "t", "v"]);
                _text.Append('\\').Append(control);
                _constructs.Add("control-escape");
                return control switch { "f" => "\f", "n" => "\n", "r" => "\r", "t" => "\t", _ => "\v" };
            }

            case 2:
            {
                var letter = (char)(_random.Chance(50) ? 'A' + _random.Below(26) : 'a' + _random.Below(26));
                _text.Append("\\c").Append(letter);
                _constructs.Add("control-letter");
                return ((char)(letter % 32)).ToString();
            }

            case 3:
                _text.Append("\\0");
                _constructs.Add("nul-escape");
                return "\0";
            case 4:
            {
                var value = _random.Below(256);
                _text.Append("\\x").Append(value.ToString("X2", CultureInfo.InvariantCulture));
                _constructs.Add("hex-escape");
                return ((char)value).ToString();
            }

            case 5:
            {
                var value = _random.Chance(30) ? _random.Between(0xD800, 0xDFFF) : _random.Below(0x10000);
                _text.Append("\\u").Append(value.ToString(_random.Chance(50) ? "X4" : "x4", CultureInfo.InvariantCulture));
                _constructs.Add(value is >= 0xD800 and <= 0xDFFF ? "surrogate-escape" : "unicode-escape");
                return ((char)value).ToString();
            }

            case 6 when _unicode:
            {
                var value = _random.Chance(50) ? _random.Between(0x10000, 0x10FFFF) : _random.Below(0x10000);
                _text.Append("\\u{").Append('0', _random.Below(3)).Append(value.ToString("X", CultureInfo.InvariantCulture)).Append('}');
                _constructs.Add("braced-unicode-escape");
                return value is >= 0xD800 and <= 0xDFFF ? ((char)value).ToString() : char.ConvertFromUtf32(value);
            }

            case 7:
                _text.Append("\\uD83D\\uDE00");
                _constructs.Add("surrogate-pair-escape");
                return "\U0001F600";
            case 8:
            {
                var syntax = _random.Pick(SyntaxCharacters);
                _text.Append('\\').Append(syntax);
                _constructs.Add("identity-escape");
                return syntax;
            }

            case 9 when !_unicode:
            {
                // Annex B: any character but c (and k in a pattern with a group name) escapes itself.
                var identity = _random.Pick(["a", "q", "\u00E9", "-", "8", "9", "k", "z"]);
                _text.Append('\\').Append(identity);
                _constructs.Add("annex-b-identity-escape");
                return identity;
            }

            case 10 when !_unicode:
            {
                var octal = _random.Pick(["01", "012", "377", "400", "7", "08", "1", "0"]);
                _text.Append('\\').Append(octal);
                _constructs.Add("annex-b-octal-escape");
                return "";
            }

            case 11 when !_unicode:
                _text.Append(_random.Pick(["\\c", "\\c1", "\\c_", "\\x4", "\\u12", "\\u{41}"]));
                _constructs.Add("annex-b-incomplete-escape");
                return "";
            default:
                return Literal();
        }
    }

    private string Property(bool inClass)
    {
        if (_sets && _random.Chance(25))
        {
            var (spec, member) = _random.Pick(StringProperties);
            _text.Append("\\p{").Append(spec).Append('}');
            _constructs.Add("property-of-strings");
            return member;
        }

        var (name, sample) = _random.Pick(Properties);
        var negated = _random.Chance(25);
        _text.Append(negated ? "\\P{" : "\\p{").Append(name).Append('}');
        _constructs.Add(negated ? "negated-property" : name.Contains('=', StringComparison.Ordinal) ? "property-value" : "property");
        if (inClass)
            _constructs.Add("property-in-class");
        return negated ? _random.Pick(["!", "\u0378", "a", "\U0001F600"]) : sample;
    }

    private string Class()
    {
        _text.Append('[');
        var negated = _random.Chance(20);
        if (negated)
        {
            _text.Append('^');
            _constructs.Add("negated-class");
        }

        var members = _random.Chance(5) ? 0 : _random.Between(1, 5);
        if (members == 0)
            _constructs.Add(negated ? "class-everything" : "class-nothing");
        var samples = new List<string>();
        for (var i = 0; i < members; i++)
        {
            switch (_random.Below(9))
            {
                case 0:
                case 1:
                {
                    var (low, high) = Range(out var lowText, out var highText);
                    _text.Append(lowText).Append('-').Append(highText);
                    _constructs.Add("class-range");
                    samples.Add(char.ConvertFromUtf32(Math.Min(low, high) is >= 0xD800 and <= 0xDFFF ? 'a' : Math.Min(low, high)));
                    break;
                }

                case 2:
                {
                    var escape = _random.Pick(["\\d", "\\w", "\\s", "\\D", "\\W", "\\S"]);
                    _text.Append(escape);
                    _constructs.Add("class-escape-in-class");
                    samples.Add(escape[1] switch { 'd' => "4", 'w' => "k", 's' => " ", _ => "!" });

                    // Annex B: outside u a class escape may sit beside a dash, which is then a character.
                    if (!_unicode && _random.Chance(20))
                    {
                        _text.Append("-z");
                        _constructs.Add("annex-b-class-escape-range");
                    }

                    break;
                }

                case 3 when _unicode:
                    samples.Add(Property(inClass: true));
                    break;
                case 4:
                {
                    var special = _random.Pick(["\\]", "\\\\", "\\-", "\\^", "\\b", "\\n", "\\x41", "\\u0041", "\\/", "\\["]);
                    _text.Append(special);
                    _constructs.Add("escape-in-class");
                    samples.Add(special[1] switch { 'b' => "\b", 'n' => "\n", 'x' or 'u' => "A", _ => special[1].ToString() });
                    break;
                }

                case 5 when _unicode:
                {
                    _text.Append("\\u{1F600}-\\u{1F64F}");
                    _constructs.Add("astral-class-range");
                    samples.Add("\U0001F610");
                    break;
                }

                case 6 when !_unicode:
                    _text.Append(_random.Pick(["\\c1", "\\c_", "\\k", "\\1", "\\7", "\\8", "\\a", "\\c"]));
                    _constructs.Add("annex-b-class-escape");
                    break;
                default:
                {
                    var text = ClassCharacter();
                    _text.Append(text);
                    samples.Add(text);
                    break;
                }
            }
        }

        _text.Append(']');
        _constructs.Add("class");
        return negated || samples.Count == 0 ? _random.Pick(["!", "\u2603", "Z"]) : Cased(_random.Pick(samples));
    }

    /// <summary>A class member that needs no escape: never <c>]</c>, <c>\</c>, <c>^</c> or <c>-</c>.</summary>
    private string ClassCharacter()
    {
        while (true)
        {
            var text = _random.Pick(Characters);
            if (text is not ("]" or "\\" or "^" or "-"))
                return text;
        }
    }

    /// <summary>Two range ends, written as characters or escapes; out of order now and then, which is refused.</summary>
    private (int Low, int High) Range(out string lowText, out string highText)
    {
        var pairs = new (int, int)[] { ('a', 'z'), ('A', 'Z'), ('0', '9'), ('\u00E0', '\u00FF'), (0x391, 0x3C9), (0x10400, 0x1044F), (0xD800, 0xDBFF), (0x20, 0x7E), ('K', 'k'), (0x1F600, 0x1F64F) };
        var (low, high) = _random.Pick(pairs);
        if (low > 0xFFFF || high > 0xFFFF)
        {
            if (!_unicode)
                (low, high) = ('b', 'y');
        }

        if (_random.Chance(4))
        {
            (low, high) = (high, low);
            _constructs.Add("near-miss-range-out-of-order");
        }

        lowText = RangeEnd(low);
        highText = RangeEnd(high);
        return (low, high);
    }

    private string RangeEnd(int codePoint)
    {
        if (codePoint is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9')
            return ((char)codePoint).ToString();
        if (codePoint > 0xFFFF)
            return _random.Chance(50) ? char.ConvertFromUtf32(codePoint) : "\\u{" + codePoint.ToString("X", CultureInfo.InvariantCulture) + "}";
        return "\\u" + codePoint.ToString("X4", CultureInfo.InvariantCulture);
    }

    // ----- v-mode class sets ----------------------------------------------------------------------------------------

    private string ClassSet(int depth)
    {
        _constructs.Add("class-set");
        _text.Append('[');
        var negated = _random.Chance(15);
        if (negated)
        {
            _text.Append('^');
            _constructs.Add("negated-class-set");
        }

        var strings = !negated || _random.Chance(5);
        string sample;
        switch (_random.Below(10))
        {
            case 0:
            case 1:
            {
                _constructs.Add("class-set-intersection");
                sample = ClassSetOperand(depth, strings);
                var count = _random.Between(1, 3);
                for (var i = 0; i < count; i++)
                {
                    _text.Append("&&");
                    ClassSetOperand(depth, strings);
                }

                break;
            }

            case 2:
            case 3:
            {
                _constructs.Add("class-set-subtraction");
                sample = ClassSetOperand(depth, strings);
                var count = _random.Between(1, 3);
                for (var i = 0; i < count; i++)
                {
                    _text.Append("--");
                    ClassSetOperand(depth, strings);
                }

                break;
            }

            default:
            {
                _constructs.Add("class-set-union");
                var count = _random.Chance(5) ? 0 : _random.Between(1, 4);
                var samples = new List<string>();
                for (var i = 0; i < count; i++)
                {
                    if (_random.Chance(25))
                    {
                        var low = (char)('a' + _random.Below(13));
                        var high = (char)(low + _random.Below(13));
                        _text.Append(low).Append('-').Append(high);
                        _constructs.Add("class-set-range");
                        samples.Add(low.ToString());
                    }
                    else
                    {
                        samples.Add(ClassSetOperand(depth, strings));
                    }
                }

                sample = samples.Count == 0 ? "" : _random.Pick(samples);
                break;
            }
        }

        _text.Append(']');
        return negated ? _random.Pick(["!", "\u2603"]) : Cased(sample);
    }

    private string ClassSetOperand(int depth, bool strings)
    {
        switch (_random.Below(8))
        {
            case 0 when depth > 0:
                _constructs.Add("nested-class-set");
                return ClassSet(depth - 1);
            case 1 when strings:
                return StringDisjunction();
            case 2:
                return Property(inClass: true) is var member && (strings || member.Length <= 2) ? member : "";
            case 3:
            {
                var escape = _random.Pick(["d", "D", "w", "W", "s", "S"]);
                _text.Append('\\').Append(escape);
                _constructs.Add("class-escape-in-class-set");
                return escape == "d" ? "3" : escape == "w" ? "w" : escape == "s" ? " " : "!";
            }

            default:
                return ClassSetCharacter();
        }
    }

    /// <summary>A single character of a class set, escaped where the grammar reserves it.</summary>
    private string ClassSetCharacter()
    {
        var text = _random.Pick(Characters);
        if (text.Length == 1 && (text[0] is '(' or ')' or '[' or ']' or '{' or '}' or '/' or '-' or '\\' or '|' || char.IsAsciiLetterOrDigit(text[0]) is false && text[0] < 0x80 && !char.IsWhiteSpace(text[0]) && text[0] >= 0x20))
        {
            // Every ASCII punctuator is escaped: \ before a syntax character or a reserved punctuator is the character.
            if (text[0] is '"' or '\'' or '_')
            {
                _text.Append(text);
                return text;
            }

            _text.Append('\\').Append(text);
            _constructs.Add("escaped-class-set-character");
            return text;
        }

        _text.Append(text);
        return text;
    }

    private string StringDisjunction()
    {
        _constructs.Add("class-string-disjunction");
        _text.Append("\\q{");
        var count = _random.Between(1, 3);
        var samples = new List<string>();
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
                _text.Append('|');
            var length = _random.Below(4);
            if (length == 0)
                _constructs.Add("empty-class-string");
            var sample = new StringBuilder();
            for (var j = 0; j < length; j++)
            {
                var text = _random.Pick(Characters);
                if (text is "(" or ")" or "[" or "]" or "{" or "}" or "/" or "-" or "\\" or "|" or "&" or "!" or "#" or "%" or "," or ":" or ";" or "<" or "=" or ">" or "@" or "`" or "~")
                    _text.Append('\\');
                _text.Append(text);
                sample.Append(text);
            }

            samples.Add(sample.ToString());
        }

        _text.Append('}');
        return _random.Pick(samples);
    }

    // ----- Groups and references ------------------------------------------------------------------------------------

    private string Group(int depth)
    {
        switch (_random.Below(10))
        {
            case 0:
            case 1:
            case 2:
            case 3:
            {
                var index = ++_groups;
                _text.Append('(');
                _constructs.Add("capturing-group");
                var body = Disjunction(depth - 1);
                _text.Append(')');
                _captured[index] = body;
                return body;
            }

            case 4:
            case 5:
            {
                _text.Append("(?:");
                _constructs.Add("non-capturing-group");
                var body = Disjunction(depth - 1);
                _text.Append(')');
                return body;
            }

            case 6:
            case 7:
            {
                var index = ++_groups;
                var name = _random.Chance(70) || _names.Count == 0 ? _random.Pick(Names) : _random.Pick(_names);
                if (_names.Contains(name))
                    _constructs.Add("repeated-group-name");
                _names.Add(name);
                _text.Append("(?<").Append(name).Append('>');
                _constructs.Add(name.Contains('\\', StringComparison.Ordinal) ? "escaped-group-name" : name.Length > 1 && char.IsSurrogate(name[0]) ? "astral-group-name" : "named-group");
                var body = Disjunction(depth - 1);
                _text.Append(')');
                _captured[index] = body;
                return body;
            }

            default:
            {
                var add = _random.Pick(["i", "m", "s", "im", "is", "ms", "ims", ""]);
                var remove = _random.Pick(["", "", "i", "m", "s", "ms"]);
                foreach (var flag in add)
                    remove = remove.Replace(flag.ToString(), "", StringComparison.Ordinal);
                if (add.Length == 0 && remove.Length == 0)
                    add = "i";
                _text.Append("(?").Append(add);
                if (remove.Length > 0 || _random.Chance(10) && add.Length > 0)
                    _text.Append('-').Append(remove);
                _text.Append(':');
                _constructs.Add("modifier-group");
                var body = Disjunction(depth - 1);
                _text.Append(')');
                return body;
            }
        }
    }

    private string BackReference()
    {
        if (_names.Count > 0 && _random.Chance(50) || _random.Chance(10))
        {
            var name = _names.Count > 0 && _random.Chance(80) ? _random.Pick(_names) : _random.Pick(Names);
            _text.Append("\\k<").Append(name).Append('>');
            _constructs.Add("named-back-reference");
            return "";
        }

        var number = _random.Between(1, _groups + 2);
        _text.Append('\\').Append(number.ToString(CultureInfo.InvariantCulture));
        _constructs.Add(number <= _groups ? "back-reference" : "forward-or-missing-back-reference");
        return _captured.TryGetValue(number, out var sample) ? sample : "";
    }

    // ----- Quantifiers ----------------------------------------------------------------------------------------------

    private string Quantified(string atom)
    {
        Quantifier(out var min, out var max);
        var copies = min > 256 ? 0 : max < 0 ? min + _random.Below(3) : _random.Between(min, Math.Min(max, min + 3));
        var sample = new StringBuilder();
        for (var i = 0; i < copies && sample.Length < SampleCap; i++)
            sample.Append(atom);
        return Cap(sample.ToString());
    }

    private void Quantifier(out int min, out int max)
    {
        switch (_random.Below(7))
        {
            case 0: _text.Append('*'); (min, max) = (0, -1); _constructs.Add("star"); break;
            case 1: _text.Append('+'); (min, max) = (1, -1); _constructs.Add("plus"); break;
            case 2: _text.Append('?'); (min, max) = (0, 1); _constructs.Add("question"); break;
            case 3:
                min = Bound();
                max = min;
                _text.Append('{').Append(min.ToString(CultureInfo.InvariantCulture)).Append('}');
                _constructs.Add("exact-bound");
                break;
            case 4:
                min = Bound();
                max = -1;
                _text.Append('{').Append(min.ToString(CultureInfo.InvariantCulture)).Append(",}");
                _constructs.Add("open-bound");
                break;
            case 5 when _random.Chance(20):
            {
                // A bound past int.MaxValue, which the parser saturates.
                _text.Append(_random.Pick(["{99999999999}", "{0,99999999999}", "{2147483647,}", "{2147483648}"]));
                _constructs.Add("huge-bound");
                (min, max) = (int.MaxValue, int.MaxValue);
                break;
            }

            default:
            {
                min = Bound();
                max = min + _random.Skewed(1 << 16);
                _text.Append('{').Append(min.ToString(CultureInfo.InvariantCulture)).Append(',').Append(max.ToString(CultureInfo.InvariantCulture)).Append('}');
                _constructs.Add("range-bound");
                break;
            }
        }

        if (min >= 1000 || max >= 1000)
            _constructs.Add("large-bound");
        if (_random.Chance(30))
        {
            _text.Append('?');
            _constructs.Add("lazy");
        }
    }

    private int Bound() => _random.Chance(85) ? _random.Below(6) : _random.Skewed(1 << 20);

    // ----- Helpers ---------------------------------------------------------------------------------------------------

    /// <summary>Under <c>i</c>, the sample's ASCII letters in either case.</summary>
    private string Cased(string text)
    {
        if (!_ignoreCase || text.Length == 0 || !_random.Chance(50))
            return text;
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsAsciiLetterLower(chars[i]))
                chars[i] = (char)(chars[i] - 32);
            else if (char.IsAsciiLetterUpper(chars[i]))
                chars[i] = (char)(chars[i] + 32);
        }

        return new string(chars);
    }

    private static string Classify(string text) =>
        text.Length == 2 && char.IsSurrogatePair(text[0], text[1]) ? "astral-literal"
        : text.Length == 1 && char.IsSurrogate(text[0]) ? "lone-surrogate-literal"
        : text.Length == 1 && text[0] >= 0x80 ? "non-ascii-literal"
        : text is "\n" or "\r" or "\u2028" or "\u2029" ? "line-terminator-literal"
        : "literal";

    private static string Cap(string text) => text.Length <= SampleCap ? text : text[..SampleCap];

    /// <summary>A property escape one edit from an admitted one, for the near-miss mutations.</summary>
    internal static string NearMissProperty(FuzzRandom random) => random.Pick(NearMissProperties);

    /// <summary>A group name the grammar refuses, for the near-miss mutations.</summary>
    internal static string NearMissName(FuzzRandom random) => random.Pick(NearMissNames);

    /// <summary>A property of strings, for the shapes and the near-miss mutations.</summary>
    internal static string StringProperty(FuzzRandom random) => random.Pick(StringProperties).Spec;
}
