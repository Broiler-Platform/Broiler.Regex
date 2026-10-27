# ECMA-262 §22.2 → Broiler.Regex mapping

How each piece of the ECMAScript regular-expression specification maps onto the
code in this project. Section numbers are those of ECMA-262 2025 (ES2025).

## §22.2.1 Patterns (grammar) → `Parsing/RegexParser.cs` + `Ast/`

| Grammar production | Parser method | AST node |
|--------------------|---------------|----------|
| `Disjunction` `A│B` | `ParseDisjunction` | `DisjunctionNode` |
| `Alternative` (concatenation) | `ParseAlternative` | `SequenceNode` / `EmptyNode` |
| `Term` | `ParseTerm` | — |
| `Assertion` `^ $ \b \B` | `TryParseAssertion` | `AnchorNode` |
| `Assertion` look-around | `TryParseAssertion` | `LookaroundNode` |
| `Quantifier` `* + ? {n,m}` | `TryApplyQuantifier` | `QuantifierNode` |
| `Atom` PatternCharacter | `ParseAtom` | `CharNode` |
| `Atom` `.` | `ParseAtom` | `AnyCharNode` |
| `Atom` `( … )` / `(?: … )` / `(?<name> … )` | `ParseGroup` | `GroupNode` |
| `Atom` `(?ims-ims: … )` | `ParseModifierGroup` | `ModifierGroupNode` |
| `CharacterClass` `[ … ]` | `ParseCharacterClass` | `CharClassNode` + `CharSet` |
| `AtomEscape` back-reference | `ParseAtomEscape` | `BackreferenceNode` |
| `CharacterClassEscape` `\d \w \s …` | `TryReadClassEscape` | `CharClassNode` / `CharSet` |
| `CharacterClassEscape` `\p{…}` / `\P{…}` | `AddPropertyEscape` | `CharClassNode` / `CharSet` |
| `ClassSetExpression` (`v` mode) | `ParseClassSetExpression` | `CharClassNode` / `CharSet` |
| `ClassSetOperand`, `NestedClass` | `ParseClassSetOperand`, `ParseNestedClass` | `CodePointSet` |
| `ClassStringDisjunction` `\q{…}` | `ParseClassStringDisjunction` | `CharSet.Strings` |
| `CharacterEscape` `\n \xHH \uHHHH \u{…} \cX \0` | `ReadCharacterEscape` | `CharNode` |
| Annex B `LegacyOctalEscapeSequence` (without `u` and `v`) | `ReadLegacyOctalOrIdentity` | `CharNode` |
| `GroupName` / `RegExpIdentifierName` | `ReadGroupName`, over the provider's `EcmaScriptLexical` (`RegexUnicode.IsIdentifierStart` / `IsIdentifierPart`) | `GroupNode` |
| `AtomEscape` `\k GroupName` | `ParseAtomEscape`, checked against the declared names once the pattern is read | `BackreferenceNode` |

## §22.2.2 Pattern semantics (the matcher) → `Compiling/ProgramCompiler.cs` + `Engine/`

The spec defines a `Matcher` as a function of a **State** and a **Continuation**. Broiler.Regex
defunctionalises them (decision D5 of Broiler.VM's JSD-0081; the continuation-passing matcher of
`Matching/` did so directly until SJL-11.2 task 2.4c deleted it): the program compiler lowers each
construct to instructions, a continuation is the address after them, and a choice the spec would try
later is a frame on the machine's explicit stack.

| Spec concept | Code |
|--------------|------|
| State (§22.2.2.1) | `RegexMachine`'s position and registers (capture ends, an iteration's start, a loop's count) |
| Continuation | the next instruction's address; a pending choice is a frame (address, position, trail height, mark) |
| Matcher | the instructions `ProgramCompiler` lowers a construct to |
| Direction (forward / backward) | `RegexInstruction.RightToLeft`, on each reading instruction of a look-behind's body |
| §22.2.2.3.1 RepeatMatcher | `Choice`, `SavePosition`, `ClearCaptures` and `RejectEmptyIteration` (the empty-match guard), with `SetCounter`, `ExitAtMaximum`, `EnterBelowMinimum` and `IncrementCounter` for counted bounds; `GreedyRun` and `LazyRun` for a quantifier over one character |
| §22.2.2.7 CompileAtom, a capturing group | `SavePosition` of its two registers, the end first when backward |
| §22.2.2.4 Assertions / look-around | `LineStart`, `LineEnd`, `WordEdge`, `NotWordEdge`; `OpenLookaround` and `CloseLookaround` around a barrier frame |
| §22.2.2.7 AtomEscape (character) | `Literal`, `LiteralIgnoringCase`; `AtomRun` for a row of them |
| §22.2.2.7.2 BackreferenceMatcher | `Backreference`, `BackreferenceIgnoringCase` |
| §22.2.2.9 CompileToCharSet | `RegexParser` class parsing → `CharSet` |
| §22.2.2.9.3 WordCharacters | `RegexUnicode.EscapeSet` / `RegexUnicode.IsWord` |
| §22.2.2.9.4 AllCharacters | `RegexParser.ComplementOperand` / `RegexUnicode.BuildCanonicalCharacters` |
| §22.2.2.9.5 MaybeSimpleCaseFolding | `RegexParser.Fold` / `CodePointSet.Map` |
| §22.2.2.9.6 CharacterComplement | `RegexParser.ComplementOperand` |
| §22.2.2.7.3 Canonicalize | `RegexUnicode.Canonicalize`, over the provider's `UnicodeCaseFolding`, read at match time |
| §22.2.2.9.7 UnicodeMatchProperty / §22.2.2.9.8 UnicodeMatchPropertyValue | `RegexUnicode.ResolveProperty`, over the provider's `UnicodeProperties` and `UnicodeStringProperties` |
| §22.2.2.7.1 CharacterSetMatcher | `Class`, `ClassIgnoringCase`, `CanonicalClass`, `Any`, `AnyButLineTerminator`; a class of strings is `StringClass`, its strings longest first, then its single characters, then the empty string |
| §22.2.7.2 RegExpBuiltinExec's loop | `RegexMachine.Run`, a start at a time by AdvanceStringIndex, skipping the positions the program's first test refuses; the string-bearing `BroilerRegex.Match` takes step 13.b's step back from a start inside a pair |

### Why direction matters (look-behind)

§22.2.2.4 specifies that the body of a look-behind is evaluated with
`direction = −1`, and §22.2.2.3 composes the terms of an `Alternative` in
*reverse* under that direction. The program compiler emits a look-behind's terms
last first and marks each reading instruction `RightToLeft`, which reads the
character that ends at the index and moves the index to its start. A capturing
group's end register is written first, so captures read left-to-right regardless
of match direction. This is the structural reason Broiler.Regex gets
look-behind + back-reference cases right where a reversed-capture engine cannot.

### Why the empty-match guard matters (nullable quantifiers)

§22.2.2.3.1 step 2.b returns failure when a `min = 0` repetition's body matched
without advancing (`y.endIndex == x.endIndex`). `RejectEmptyIteration` compares
the index with the iteration's start, which `SavePosition` wrote, so a nullable
quantifier neither loops forever nor terminates one iteration too early. A run
reads one character an iteration, so it never meets the case.

## §22.2.6 Flags → `RegexFlags.cs`

`d g i m s u v y` parse to `RegexFlags`; `i`/`m`/`s` are the only flags an inline
modifier group may toggle (`ModifierGroupNode`).

Since task 2.7 of SJL-11.2 the grammar of a flags string is `Text/RegexFlagsText.cs`:
RegExpInitialize's checks (§22.2.3.3) and ParsePattern's refusal of `u` with `v` (§22.2.3.4) answered as a
`RegexFlagsStatus` with a position, and the `flags` getter's order (§22.2.6.4) by `RegexFlagsText.Format`.
`RegexFlagsParser.Parse` reads the same grammar and throws.

## §22.2.5.1 RegExp.escape and §22.2.6.13.1 EscapeRegExpPattern → `Text/RegexEscaper.cs`

| Spec concept | Code |
|--------------|------|
| §22.2.5.1 RegExp.escape(S), the loop and the leading-character rule | `RegexEscaper.EscapeString` |
| §22.2.5.1.1 EncodeForRegExpEscape(cp) | `RegexEscaper.EncodeCodePoint` and the writer's `Hex` |
| §12.2 WhiteSpace, §12.3 LineTerminator | the provider's `EcmaScriptLexical.IsWhiteSpace` and `IsLineTerminator` |
| §22.2.6.13.1 EscapeRegExpPattern(P, F) | `RegexEscaper.EscapePattern`, a class read as §12.9.5 RegularExpressionClass reads it |

### Why the fold order matters (`v` mode)

§22.2.2.9.5 folds each class-set operand through `scf` **before** §22.2.2.9.6 complements
it, and §22.2.2.9.4 makes that complement's universe the code points that fold to
themselves. `RegexParser.Fold` and `ComplementOperand` keep exactly that order, which is
the whole reason `\P{Lu}` matches neither `A` nor `a` under `vi` while it matches both
under `ui`, whose case closure runs over the complement (`[^\p{Lu}]` matches neither in both
modes). Because every operand is folded, `CharSet.CaseFolded` lets the matcher
canonicalize the subject and test membership directly instead of walking its fold orbit —
the two are equivalent once the set holds only canonical members.

## Not yet mapped

- §22.2.6.11 `RegExp.prototype[@@replace]` and §22.2.6.14 `[@@split]` on the JavaScript
  side still build a .NET `Regex`; only `RegExpBuiltinExec` reads Broiler match data.
- Two deliberate divergences from V8 under `vi` — a lone binary property's set and a
  one-character `\q{…}` alternative — recorded in
  [the engine README](../README.md#known-limitations-stubbed--todo).

These implementation gates are tracked in the
[repository roadmap](../../docs/roadmap.md).
