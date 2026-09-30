# Human Review: Broiler.RegEx

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.RegEx`, which rewrites this file,
`CODE-ASSURANCE.md`, `assurance.manifest.json` and every generated source header from the
product tree.

> **Status: PENDING.** Human-reviewed: 0 of 222 relevant units. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.RegEx --release`
> fails while any relevant unit is without a decision bound to its current fingerprint.

## 1. How To Use This File

Read it; do not edit it. A decision about a code unit is the `// Broiler-Human:` line on that
unit's declaration, and every table below is read out of those lines. There is nothing here
to fill in and nothing here to leave blank.

## 2. How A Review Is Recorded

In one place: the `// Broiler-Human:` line of the assurance annotation that sits on the
declaration being read. Nothing in this file is edited by hand, no second document carries a
per-item checklist, and no list of permitted aliases exists to be added to.

```csharp
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=4A3BFD
// Broiler-Falsified-If: a negative value reaches the running total
// Broiler-Human:        PENDING
```

The last line has four shapes. A human writes three of them; the generator writes the fourth
and may never invent an alias, which the check asserts in both directions.

| Line | Meaning |
|---|---|
| `PENDING` | Nobody has recorded a decision for this unit. The generator leaves it exactly as it stands. |
| `<alias>` | A human states their own alias and leaves the machine field to the generator, which fills it with the declaration's fingerprint at the next run. |
| `<alias>; Fingerprint=<six hex>` | A decision bound to one exact version of one declaration. |
| `STALE; Previous=<alias>@<fingerprint>` | Written by the generator when the code moved after a decision. Only a human clears it, by stating their alias again. |

A human may state their own `IP=`, `Security=` and `Resources=` assessment beside their alias,
which is how a reader disagrees with the machine assessment on the line above: an assessment is
a comment and moves no fingerprint, so there is nowhere else to say it.

**No branch, commit or tag is recorded in this file.** Each decision names the fingerprint of
the declaration it was made against, and the state machine compares that value with the
declaration as it now stands. A commit says a tree moved; a fingerprint says whether this unit
did, which is the narrower and the more useful of the two.

## 3. Summary

| Metric | Value |
|---|---:|
| Files scanned | 13 |
| Code units | 340 |
| Relevant | 222 |
| Exempt | 118 |
| Assessed | 222 of 222 (100%) |
| Human reviewed | 0 of 222 (0%) |
| Unverified | 222 |
| Aliases naming a decision | 0 |

## 4. Review States

One row per state of the machine that reads the two lines. The states are computed from the
annotations and the current fingerprints; nothing stores them.

| State | Units |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 222 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 118 |

## 5. Aliases In The Tree

No alias appears on a human line anywhere in the product tree. Nobody has recorded a
decision about any unit of this component.

## 6. Coverage By File

One row per covered file, carrying that file's generated header. `Unverified` counts the
relevant units in a state that blocks a release. The files the configuration leaves out
are listed in `CODE-ASSURANCE.md`.

| File | Units | Relevant | Exempt | Unverified | IP risk | Security risk | Criteria |
|---|---:|---:|---:|---:|---|---|---:|
| `Broiler.Regex/Ast/CharSet.cs` | 27 | 14 | 13 | 14 | Low | High | 8/2 |
| `Broiler.Regex/Ast/CodePointSet.cs` | 21 | 17 | 4 | 17 | Low | Medium | 12/0 |
| `Broiler.Regex/Ast/RegexNode.cs` | 46 | 20 | 26 | 20 | None | Low | 0/0 |
| `Broiler.Regex/BroilerRegex.cs` | 11 | 6 | 5 | 6 | Low | High | 6/6 |
| `Broiler.Regex/Matching/MatchState.cs` | 15 | 8 | 7 | 8 | Low | Medium | 8/0 |
| `Broiler.Regex/Matching/Matcher.cs` | 62 | 44 | 18 | 44 | Low | High | 35/29 |
| `Broiler.Regex/Matching/RegexMatch.cs` | 17 | 4 | 13 | 4 | None | Low | 2/0 |
| `Broiler.Regex/Parsing/RegexParser.cs` | 74 | 59 | 15 | 59 | Low | High | 57/49 |
| `Broiler.Regex/RegexFlags.cs` | 13 | 4 | 9 | 4 | Low | High | 4/2 |
| `Broiler.Regex/RegexOverflowException.cs` | 2 | 2 | 0 | 2 | Low | Low | 0/0 |
| `Broiler.Regex/RegexSyntaxException.cs` | 2 | 1 | 1 | 1 | Low | Low | 0/0 |
| `Broiler.Regex/Unicode/CaseFolding.cs` | 21 | 16 | 5 | 16 | Low | High | 15/4 |
| `Broiler.Regex/Unicode/UnicodeCharSets.cs` | 29 | 27 | 2 | 27 | Low | High | 24/4 |

## 7. Decisions Recorded

No unit in this component carries a decision on its human line. Every one of them reads
`PENDING`.

## 8. Decisions The Code Has Outrun

No unit carries a decision that the code has since moved past.

## 9. Where A Decision Is Required First

The units at the top of the security vocabulary, with the observation that would show each
one wrong and the human line it carries. The set is read from the assessments rather than
written out, so a unit that becomes `High` joins it at the next generation.

- `Broiler.Regex.Ast.CharSet` in `Broiler.Regex/Ast/CharSet.cs` - Security=High, Spec=none cited, `BD9DC9`, PENDING
  - Falsified if: a v-mode class written [\q{ab|ab|c}] ends up with a duplicate entry or a one-code-point member in Strings
- `Broiler.Regex.Ast.CharSet.AddRange(int, int)` in `Broiler.Regex/Ast/CharSet.cs` - Security=High, Spec=none cited, `13A512`, PENDING
  - Falsified if: a class range written high to low, such as [z-a], is added to the set instead of throwing RegexSyntaxException
- `Broiler.Regex.BroilerRegex` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `1B83F8`, PENDING
  - Falsified if: two threads making their first Match calls at the same time on one instance built from [zab] both normalize the class's out-of-order range list in place, and one throws or misses a match that a single thread finds
- `Broiler.Regex.BroilerRegex.BroilerRegex(string, string?)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `BD448B`, PENDING
  - Falsified if: a flags string with a repeated letter such as "gg" produces an instance instead of throwing RegexSyntaxException
- `Broiler.Regex.BroilerRegex.BroilerRegex(string, RegexFlags)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `7C6C9D`, PENDING
  - Falsified if: a pattern of 50,000 nested opening parentheses ends the process with a stack overflow inside RegexParser.Parse instead of raising a catchable exception
- `Broiler.Regex.BroilerRegex.Match(string, int)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `EBD5A7`, PENDING
  - Falsified if: Match(input, -1) does not give the result that Match(input, 0) gives, so a negative start is not clamped to 0
- `Broiler.Regex.BroilerRegex.IsMatch(string, int)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `FC8D95`, PENDING
  - Falsified if: IsMatch(input, start) returns a value other than Match(input, start).Success for the same input and start
- `Broiler.Regex.BroilerRegex.Matches(string)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `5971A9`, PENDING
  - Falsified if: after an empty match just before a surrogate pair in u or v mode, the next match starts at the pair's low surrogate
- `Broiler.Regex.Matching.Matcher` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=ECMA-262 s22.2.2, `3E4382`, PENDING
  - Falsified if: /(?<=(\d+)(\d+))$/ matched against '1053' reports groups other than '1' and '053', the result that backward, greedy look-behind matching requires
- `Broiler.Regex.Matching.Matcher.Matcher(RegexNode, int, RegexFlags, IReadOnlyDictionary<string, int>)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `D05D03`, PENDING
  - Falsified if: a pattern compiled with only the v flag matches '.' against one UTF-16 unit of a surrogate pair, because the matcher's Unicode mode stays off
- `Broiler.Regex.Matching.Matcher.Run(string, int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `68BAA0`, PENDING
  - Falsified if: a match found at a later start position reports a group captured during an earlier, failed start position, because the shared initial captures array was written in place
- `Broiler.Regex.Matching.Matcher.Compile(RegexNode, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `7DD78C`, PENDING
  - Falsified if: a group nesting depth that the parser accepts overflows the native stack inside Compile, which has no TryEnsureSufficientExecutionStack check, instead of raising RegexOverflowException
- `Broiler.Regex.Matching.Matcher.CompileSequence(IReadOnlyList<RegexNode>, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `3A255D`, PENDING
  - Falsified if: a pattern of 100_000 consecutive \b assertions recurses once per term through the continuation chain and ends the process with a stack overflow instead of raising RegexOverflowException
- `Broiler.Regex.Matching.Matcher.CompileAtomRun(CharPredicate[])` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `6E2087`, PENDING
  - Falsified if: /abc/ on 'ab' throws IndexOutOfRangeException instead of failing when the subject ends partway through the run
- `Broiler.Regex.Matching.Matcher.CompileDisjunction(IReadOnlyList<RegexNode>, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `3D9C19`, PENDING
  - Falsified if: a quantifier-free pattern of 40 copies of (?:a|a) followed by b, run on 40 'a' characters, explores about 2^40 paths because no alternative consumes a Budget step
- `Broiler.Regex.Matching.Matcher.CompileGroup(GroupNode, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `C2F071`, PENDING
  - Falsified if: /(?<=(ab))c/ on 'abc' reports group 1 with an index other than 0 or a length other than 2
- `Broiler.Regex.Matching.Matcher.CompileQuantifier(QuantifierNode, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `5A1D52`, PENDING
  - Falsified if: /(a)*/ on 'aa' reports group 1 as unmatched because a capturing body was routed to the single-code-point fast path
- `Broiler.Regex.Matching.Matcher.CompileGeneralQuantifier(CompiledMatcher, int[], int, int, bool)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=ECMA-262 s22.2.2.3.1, `81AB79`, PENDING
  - Falsified if: the empty alternative of /(?:a|)*b/ on 'c' is accepted as an iteration, so the attempt runs until the 10_000_000-step budget ends it instead of failing after a few steps
- `Broiler.Regex.Matching.Matcher.TryMatchBodyNth(CompiledMatcher, MatchState, int, out MatchState)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `C40A56`, PENDING
  - Falsified if: for the body (?:a|ab) at the start of 'ab', a skip of 1 yields a state at index 1 instead of 2, or a skip of 2 returns true
- `Broiler.Regex.Matching.Matcher.TryGetSingleCharPredicate(RegexNode, Flags, out CharPredicate?)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `39E79B`, PENDING
  - Falsified if: /[\q{abc}]*/v on 'abc' matches only the empty string because a class with string members was treated as a single-code-point body
- `Broiler.Regex.Matching.Matcher.CompileSingleCharQuantifier(CharPredicate, int, int, bool, Direction)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `CC68C7`, PENDING
  - Falsified if: a greedy /a*/ on a subject of 10_000_001 'a' characters reports a match of length 10_000_000, because budget exhaustion ends the scan early instead of failing the attempt
- `Broiler.Regex.Matching.Matcher.StepBack(string, Direction, int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `8ECA52`, PENDING
  - Falsified if: under the u flag, with a match started on the low half of a surrogate pair, stepping back from index 2 of 😀 returns 0, before the repeat's own start at 1
- `Broiler.Regex.Matching.Matcher.TryReadMatching(string, Direction, int, CharPredicate, out int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `5EC77A`, PENDING
  - Falsified if: under the u flag, a backward read from index 2 of 😀 moves the cursor to 1 instead of 0
- `Broiler.Regex.Matching.Matcher.CompileBackreference(BackreferenceNode, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `66411A`, PENDING
  - Falsified if: /\1(a)/ on 'a' fails, although a back-reference to a group that has not captured yet matches the empty string
- `Broiler.Regex.Matching.Matcher.CompileAnchor(AnchorKind, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `2187F5`, PENDING
  - Falsified if: without the m flag, /^b/ matches 'a\nb' at index 2
- `Broiler.Regex.Matching.Matcher.CompileLookaround(LookaroundNode, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `60FAD8`, PENDING
  - Falsified if: a positive look-behind leaves the cursor where its body ended, so /(?<=ab)c/ fails on 'abc' or reports a match index other than 2
- `Broiler.Regex.Matching.Matcher.CompileChar(int, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `43E20F`, PENDING
  - Falsified if: under the u flag, /\u{1F600}$/u fails on the surrogate pair 😀 because the character matcher advances by one UTF-16 unit instead of two
- `Broiler.Regex.Matching.Matcher.CompileAnyChar(Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `3AB966`, PENDING
  - Falsified if: without the s flag, /./ matches U+2028 LINE SEPARATOR
- `Broiler.Regex.Matching.Matcher.CompileCharClass(CharSet, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `A79481`, PENDING
  - Falsified if: under the u flag, /^[^a]$/u fails on the surrogate pair 😀 because the class consumes only its high half
- `Broiler.Regex.Matching.Matcher.CompileCharClassWithStrings(CharSet, Direction, bool, bool)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `6F1EA6`, PENDING
  - Falsified if: /[\q{abc|ab}]c/v fails on 'abc' because the class does not fall back from its longest string member to a shorter one
- `Broiler.Regex.Matching.Matcher.TryMatchLiteral(MatchState, Direction, string, bool, bool, out int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `3693E8`, PENDING
  - Falsified if: in a look-behind, a string member longer than the text before the cursor, such as \q{abc} at index 2, is read from a negative index instead of failing
- `Broiler.Regex.Matching.Matcher.ReadCodePoint(MatchState, Direction, out int, out int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `A65667`, PENDING
  - Falsified if: a forward read at the end of the subject, or a backward read at index 0, reports success
- `Broiler.Regex.Matching.Matcher.CodePointAt(string, int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `0722EA`, PENDING
  - Falsified if: without the u flag, a surrogate pair is read as one code point of width 2
- `Broiler.Regex.Matching.Matcher.CodePointBefore(string, int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `B85E41`, PENDING
  - Falsified if: under the u flag, the code point before index 2 of 😀 is returned as U+DE00 with width 1 instead of U+1F600 with width 2
- `Broiler.Regex.Matching.Matcher.IsWordBoundary(MatchState, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `645B40`, PENDING
  - Falsified if: \b reports a boundary at index 0 of an empty subject
- `Broiler.Regex.Matching.Matcher.RegionEquals(string, int, string, int, int, bool, bool)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `662E7A`, PENDING
  - Falsified if: a region whose last unit is the high half of a surrogate pair compares equal although the pair's low half lies past the region's end
- `Broiler.Regex.Matching.Matcher.ReadAt(string, int, bool)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `BA00B4`, PENDING
  - Falsified if: under the u flag, a high surrogate at the last index of the subject reads input[pos + 1] and throws IndexOutOfRangeException
- `Broiler.Regex.Parsing.RegexParser` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `0CA2F0`, PENDING
  - Falsified if: a pattern of 50,000 nested '(' characters ends the process with a stack overflow instead of raising a catchable exception
- `Broiler.Regex.Parsing.RegexParser.Parse(string, RegexFlags, out int, out IReadOnlyDictionary<string, int>)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `3E8A2B`, PENDING
  - Falsified if: the capture count it returns for /(a)(?<n>b)/ is not 2, or the name map does not send n to 2
- `Broiler.Regex.Parsing.RegexParser.ParsePattern()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `4FD255`, PENDING
  - Falsified if: a pattern with unconsumed trailing text such as 'a)' returns a tree instead of throwing RegexSyntaxException
- `Broiler.Regex.Parsing.RegexParser.PreScan()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `FAEF9E`, PENDING
  - Falsified if: one '>' after thousands of unterminated '(?<a' openers makes it keep a separate name substring per opener, so retained memory grows with the square of the pattern length
- `Broiler.Regex.Parsing.RegexParser.ParseDisjunction()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `598B1F`, PENDING
  - Falsified if: a pattern of 100,000 nested '(?:' groups exhausts the native stack in this recursion instead of raising a catchable exception
- `Broiler.Regex.Parsing.RegexParser.ParseAlternative()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `FBAC09`, PENDING
  - Falsified if: a raw U+0000 character inside the pattern ends the alternative early, so the text after it is lost or rejected
- `Broiler.Regex.Parsing.RegexParser.ParseTerm()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `3B0D85`, PENDING
  - Falsified if: a quantifier after a look-behind, ^, $, \b or \B, or after any assertion under u or v, is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.TryParseAssertion(out bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `20003E`, PENDING
  - Falsified if: 100,000 nested '(?=' look-aheads exhaust the native stack instead of raising a catchable exception
- `Broiler.Regex.Parsing.RegexParser.HasQuantifier()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `C002FB`, PENDING
  - Falsified if: outside u and v, /^{2}/ is accepted or /^{a}/ is rejected, because the cursor is not restored after the brace probe
- `Broiler.Regex.Parsing.RegexParser.TryApplyQuantifier(RegexNode)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `EAFCF6`, PENDING
  - Falsified if: a '{' that opens no valid quantifier, as in /a{,5}/ outside u, is consumed instead of being left as literal text
- `Broiler.Regex.Parsing.RegexParser.TryParseBraceQuantifier(out int, out int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `BCCEC8`, PENDING
  - Falsified if: {3,2} is accepted, or {2147483648} wraps to a negative count instead of saturating at int.MaxValue
- `Broiler.Regex.Parsing.RegexParser.ParseAtom()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `810E98`, PENDING
  - Falsified if: under u or v an unescaped ']', '{' or '}' is returned as a literal character instead of raising a syntax error
- `Broiler.Regex.Parsing.RegexParser.ParseGroup()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `84B390`, PENDING
  - Falsified if: a capturing group is numbered after the groups nested inside it, so the outer group of /((a)b)/ is not capture 1
- `Broiler.Regex.Parsing.RegexParser.ParseModifierGroup()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `9481DC`, PENDING
  - Falsified if: the i flag added by (?i:...) is still in effect after its ')', so a following v-mode class is case-folded
- `Broiler.Regex.Parsing.RegexParser.ReadModifierFlags()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `54D512`, PENDING
  - Falsified if: a repeated modifier flag such as (?ii:a) is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ParseAtomEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `049542`, PENDING
  - Falsified if: under u a numeric escape above the capture count, such as \2 in /(a)\2/u, is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ParseCharacterClass()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `8CA155`, PENDING
  - Falsified if: under u a range with a class-escape endpoint such as [\d-z] is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ReadClassAtom(CharSet, out bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `3D570C`, PENDING
  - Falsified if: inside a class \b yields anything but U+0008, or \- throws under u instead of yielding a hyphen
- `Broiler.Regex.Parsing.RegexParser.ParseClassSet()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `8B4152`, PENDING
  - Falsified if: UsesPropertyEscape stays set from an earlier class, so the second class of /[\p{L}][a]/v is flagged as using a property escape
- `Broiler.Regex.Parsing.RegexParser.ReadPropertyOperand(bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.2.9, `D189F6`, PENDING
  - Falsified if: under vi \P{Ll} is complemented before its members are folded, so /\P{Ll}/vi matches 'a'
- `Broiler.Regex.Parsing.RegexParser.ParseNestedClass()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `45D9C5`, PENDING
  - Falsified if: a v-mode class nested 100,000 '[' deep exhausts the native stack instead of raising a catchable exception
- `Broiler.Regex.Parsing.RegexParser.ParseClassSetExpression()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `1E86C7`, PENDING
  - Falsified if: a v-mode class repeating \p{RGI_Emoji} a thousand times makes billions of string comparisons, because each union scans the accumulated string list once per member
- `Broiler.Regex.Parsing.RegexParser.ParseClassIntersection(ClassSetValue, int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `2FC989`, PENDING
  - Falsified if: a string present in only one operand survives the intersection, so /[\q{ab}&&\q{cd}]/v matches 'ab'
- `Broiler.Regex.Parsing.RegexParser.ParseClassSubtraction(ClassSetValue, int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `A3FDF3`, PENDING
  - Falsified if: a string of the right operand survives the subtraction, so /[\q{ab|cd}--\q{ab}]/v matches 'ab'
- `Broiler.Regex.Parsing.RegexParser.RejectMixedOperator(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `9001F0`, PENDING
  - Falsified if: [a--b&&c] or [a&&b--c] under v is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ParseClassSetRangeOrOperand()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `4A932C`, PENDING
  - Falsified if: a range endpoint that is a class escape or \q{...}, as in [a-\d] under v, is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ParseClassSetOperand(out int?)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `D908B6`, PENDING
  - Falsified if: an unescaped '(' or a doubled punctuator such as '!!' is accepted as a literal inside a v-mode class
- `Broiler.Regex.Parsing.RegexParser.ParseClassStringDisjunction()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `1ACD24`, PENDING
  - Falsified if: an empty alternative, as in \q{a|}, is dropped instead of making the class match the empty string
- `Broiler.Regex.Parsing.RegexParser.IsClassSetSyntaxCharacter(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `596E48`, PENDING
  - Falsified if: it returns false for one of ( ) [ ] { } / - \ | or true for any other character
- `Broiler.Regex.Parsing.RegexParser.IsClassSetReservedPunctuator(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `1CB1BF`, PENDING
  - Falsified if: it returns true for '$' or '^', or false for one of & - ! # % , : ; < = > @ ` ~
- `Broiler.Regex.Parsing.RegexParser.IsClassSetReservedDoublePunctuator(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `8B8981`, PENDING
  - Falsified if: it returns true for '-' or '|', or false for one of & ! # $ % * + , . : ; < = > ? @ ^ ` ~
- `Broiler.Regex.Parsing.RegexParser.TryReadClassEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `4612E7`, PENDING
  - Falsified if: a letter other than d, D, w, W, s or S, such as b, is consumed as a class escape
- `Broiler.Regex.Parsing.RegexParser.ReadCharacterEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `63802E`, PENDING
  - Falsified if: under u an identity escape of a non-syntax character such as \a or \- outside a class is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ReadLegacyOctalOrIdentity()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `F85821`, PENDING
  - Falsified if: an octal escape above 0377, such as \400, is read as one code point greater than 0xFF instead of \40 followed by '0'
- `Broiler.Regex.Parsing.RegexParser.ReadControlEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `FC80D8`, PENDING
  - Falsified if: outside u, /\c1/ does not match the text backslash-c-1 because the c is consumed along with the backslash
- `Broiler.Regex.Parsing.RegexParser.ReadHexEscape(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `690F4D`, PENDING
  - Falsified if: a non-hex character inside the fixed-width run, as in \x4g under u, is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.HasHexDigits(int, int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `44C9C9`, PENDING
  - Falsified if: outside u, \x4 at the end of the pattern is decoded as a hex escape instead of the identity escape x followed by 4
- `Broiler.Regex.Parsing.RegexParser.ReadUnicodeEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `46F15D`, PENDING
  - Falsified if: under u, \uD83D\u{41} is rejected as an invalid hexadecimal escape instead of reading a lone lead surrogate followed by A
- `Broiler.Regex.Parsing.RegexParser.AddPropertyEscape(CharSet, bool, bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.2.9, `3AFA86`, PENDING
  - Falsified if: a negated property of strings such as \P{RGI_Emoji} under v is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ReadGroupName()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `8F93F4`, PENDING
  - Falsified if: a group name holding a lone surrogate escape, such as (?<\uD800>a), escapes as ArgumentOutOfRangeException instead of RegexSyntaxException
- `Broiler.Regex.Parsing.RegexParser.AtEnd` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `3FD96C`, PENDING
  - Falsified if: it reports the end at a U+0000 pattern character rather than only once the cursor is past the last character
- `Broiler.Regex.Parsing.RegexParser.HasAt(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `9D370C`, PENDING
  - Falsified if: it returns true for an offset that lands on the source length, so [a- at the end of a pattern reads a range end past the text
- `Broiler.Regex.Parsing.RegexParser.Peek()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `63EC31`, PENDING
  - Falsified if: it throws or reads past the source instead of returning U+0000 once the cursor reaches the source length
- `Broiler.Regex.Parsing.RegexParser.PeekAt(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `E7E209`, PENDING
  - Falsified if: an offset past the end throws or reads outside the source instead of returning U+0000
- `Broiler.Regex.Parsing.RegexParser.Expect(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `0472DB`, PENDING
  - Falsified if: the cursor advances when the character does not match, so an unclosed '(' at the end of a pattern does not throw
- `Broiler.Regex.Parsing.RegexParser.ReadSourceCodePoint()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `855020`, PENDING
  - Falsified if: outside u and v a surrogate pair is read as one code point instead of two code units
- `Broiler.Regex.Parsing.RegexParser.TryReadDecimal(out int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `2D4224`, PENDING
  - Falsified if: a digit run longer than ten digits, such as 99999999999, wraps to a negative or small value instead of saturating at int.MaxValue
- `Broiler.Regex.Parsing.RegexParser.HexValue(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `2D7172`, PENDING
  - Falsified if: it returns a digit value for a character outside 0-9, a-f and A-F; the neighbours are '/', ':', '@', 'G', '`' and 'g'
- `Broiler.Regex.Parsing.RegexParser.IsSyntaxChar(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `4387A7`, PENDING
  - Falsified if: it returns true for '/', '-' or ',', or false for one of ^ $ \ . * + ? ( ) [ ] { } |
- `Broiler.Regex.RegexFlagsParser` in `Broiler.Regex/RegexFlags.cs` - Security=High, Spec=none cited, `8E3CEF`, PENDING
  - Falsified if: a flags string holding a letter outside d, g, i, m, s, u, v and y, such as "x", is accepted instead of throwing RegexSyntaxException
- `Broiler.Regex.RegexFlagsParser.Parse(string?)` in `Broiler.Regex/RegexFlags.cs` - Security=High, Spec=ECMA-262 s22.2.3.1, `A19067`, PENDING
  - Falsified if: the flags string "uv" (or "vu") returns Unicode | UnicodeSets instead of throwing RegexSyntaxException
- `Broiler.Regex.Unicode.CaseFolding` in `Broiler.Regex/Unicode/CaseFolding.cs` - Security=High, Spec=none cited, `E899C9`, PENDING
  - Falsified if: two threads calling Orbit(0x0073, unicode: true) at the same moment, before the simple table's orbit map is built, get lists that differ, one of them lacking U+017F
- `Broiler.Regex.Unicode.CaseFolding.Orbit(int, bool)` in `Broiler.Regex/Unicode/CaseFolding.cs` - Security=High, Spec=none cited, `139B38`, PENDING
  - Falsified if: Orbit(0x006B, unicode: true) omits U+212A KELVIN SIGN, although its simple case fold is 0x006B
- `Broiler.Regex.Unicode.CaseFolding.FoldTable` in `Broiler.Regex/Unicode/CaseFolding.cs` - Security=High, Spec=none cited, `F228E1`, PENDING
  - Falsified if: _orbits is assigned anywhere other than inside lock (_gate)
- `Broiler.Regex.Unicode.CaseFolding.FoldTable.Orbit(int)` in `Broiler.Regex/Unicode/CaseFolding.cs` - Security=High, Spec=none cited, `6ADA75`, PENDING
  - Falsified if: a thread that calls Orbit while another thread is still inside the first BuildOrbits gets back an orbit missing members that a later call returns
- `Broiler.Regex.Unicode.UnicodeCharSets` in `Broiler.Regex/Unicode/UnicodeCharSets.cs` - Security=High, Spec=none cited, `7E3895`, PENDING
  - Falsified if: ResolveProperty("General_Category", "Lu") and ResolveProperty("Lu") return code-point sets that differ
- `Broiler.Regex.Unicode.UnicodeCharSets.ResolveProperty(string, string?)` in `Broiler.Regex/Unicode/UnicodeCharSets.cs` - Security=High, Spec=none cited, `3B515A`, PENDING
  - Falsified if: a loosely spelled key such as \p{GC=Lu} or \p{general_category=Lu} resolves to a set instead of returning null, though ECMAScript accepts only General_Category, gc, Script, sc, Script_Extensions and scx
- `Broiler.Regex.Unicode.UnicodeCharSets.ResolveStringProperty(EmojiSequenceProperties)` in `Broiler.Regex/Unicode/UnicodeCharSets.cs` - Security=High, Spec=none cited, `18D9C8`, PENDING
  - Falsified if: the cached match's CodePoints set is still un-normalized when the lock is released, so two threads first compiling [\p{RGI_Emoji}] under v both run CodePointSet.Normalize on that shared set outside the lock
- `Broiler.Regex.Unicode.UnicodeCharSets.Normalize(string)` in `Broiler.Regex/Unicode/UnicodeCharSets.cs` - Security=High, Spec=none cited, `0FBA70`, PENDING
  - Falsified if: with the current culture set to tr-TR, the key "SCRIPT" normalizes to something other than "script"

## 10. What This Record Does Not Say

It is not an approval of the component, and a full table above would not be one either. It
records which declarations somebody stated a decision about, and against which version of
each. It does not record what they read, how long they spent, or whether they were right.

A fingerprint is six hex characters of SHA-256 over a declaration's token texts. It answers
whether a unit changed since a decision was recorded against it. It is not a collision-free
identifier across units and it is not a cryptographic commitment, so it detects a change and
does not resist a forger with commit access.

An assessment is a comment, so changing one moves no fingerprint anywhere, and nothing
mechanical checks that it is right; the check holds its values to their vocabularies and no
further.

222 of the 222 assessed units declare `Origin=AI`. Reading a declaration is the only thing
that makes it read.
