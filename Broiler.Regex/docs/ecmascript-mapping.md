# ECMA-262 §22.2 → Broiler.Regex mapping

How each piece of the ECMAScript regular-expression specification maps onto the
code in this project. Section numbers refer to ECMA-262 (2024+).

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
| `CharacterEscape` `\n \xHH \uHHHH \u{…} \cX` | `ReadCharacterEscape` | `CharNode` |

## §22.2.2 Pattern semantics (the matcher) → `Matching/Matcher.cs`

The spec defines a `Matcher` as a function of a **State** and a **Continuation**.
Broiler.Regex models these directly:

| Spec concept | Code |
|--------------|------|
| State (§22.2.2.1) | `MatchState` (end index + capture spans) |
| Continuation | `Continuation` delegate |
| Matcher | `CompiledMatcher` delegate |
| Direction (forward / backward) | `Matcher.Direction` |
| §22.2.2.3.1 RepeatMatcher | `CompileQuantifier` (incl. empty-match guard) |
| §22.2.2.3.4 capturing group | `CompileGroup` |
| §22.2.2.4 Assertions / look-around | `CompileAnchor`, `CompileLookaround` |
| §22.2.2.7 AtomEscape (character) | `CompileChar` |
| §22.2.2.9 BackreferenceMatcher | `CompileBackreference` |
| §22.2.2.9 CompileToCharSet | `RegexParser` class parsing → `CharSet` |
| §22.2.2.9.2 WordCharacters | `UnicodeCharSets.WordSet` |
| §22.2.2.9.4 AllCharacters | `UnicodeCharSets.AllCharacters` |
| §22.2.2.9.5 MaybeSimpleCaseFolding | `RegexParser.Fold` / `CodePointSet.Map` |
| §22.2.2.9.6 CharacterComplement | `RegexParser.ComplementOperand` |
| §22.2.2.9.4 Canonicalize | `Unicode/CaseFolding.cs` |
| §22.2.2.10 CharacterSetMatcher | `CompileCharClass` / `CharSet.Contains` |

### Why direction matters (look-behind)

§22.2.2.4 specifies that the body of a look-behind is evaluated with
`direction = −1`, and §22.2.2.3 composes the terms of an `Alternative` in
*reverse* under that direction. `CompileSequence` reverses its matcher list when
`dir == Backward`, and `ReadCodePoint` walks backward. Capturing groups still
store `[min, max]` (`CompileGroup`), so captures read left-to-right regardless of
match direction. This is the structural reason Broiler.Regex gets
look-behind + back-reference cases right where a reversed-capture engine cannot.

### Why the empty-match guard matters (nullable quantifiers)

§22.2.2.3.1 step 2.b returns failure when a `min = 0` repetition's body matched
without advancing (`y.endIndex == x.endIndex`). `CompileQuantifier`'s `d`
continuation implements exactly this check, so a nullable quantifier neither
loops forever nor terminates one iteration too early.

## §22.2.6 Flags → `RegexFlags.cs`

`d g i m s u v y` parse to `RegexFlags`; `i`/`m`/`s` are the only flags an inline
modifier group may toggle (`ModifierGroupNode`).

### Why the fold order matters (`v` mode)

§22.2.2.9.5 folds each class-set operand through `scf` **before** §22.2.2.9.6 complements
it, and §22.2.2.9.4 makes that complement's universe the code points that fold to
themselves. `RegexParser.Fold` and `ComplementOperand` keep exactly that order, which is
the whole reason `[^\p{Lu}]` matches neither `A` nor `a` under `vi` while it matches `a`
under `ui`. Because every operand is folded, `CharSet.CaseFolded` lets the matcher
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
