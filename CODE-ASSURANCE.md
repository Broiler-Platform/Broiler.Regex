# Broiler.RegEx Code Assurance

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.RegEx`, which rewrites this file,
`HUMAN_REVIEW.units.md`, `assurance.manifest.json` and every generated source header from the
product tree.

**No code unit in this component carries a decision on its human line yet.** This report
records that absence precisely. It is not a claim that the code is reviewed, assured or safe,
and the figures below are the measurement of how far from that claim the per-unit record is.

`HUMAN_REVIEW.md` is a separate, hand-written review record of this component. This report
neither reads it nor summarizes it, and nothing in it is counted here.

## Summary

| Metric | Value |
|---|---:|
| Files scanned | 13 |
| Files not covered | 1 |
| Files carrying an annotation | 13 |
| Code units | 340 |
| Relevant | 222 |
| Exempt by predicate | 118 |
| Annotated | 222 of 222 (100%) |
| Human reviewed | 0 of 222 (0%) |
| Unverified | 222 |

## Review states

| State | Count |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 222 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 118 |

## IP risk

| Value | Units |
|---|---:|
| None | 70 |
| Low | 152 |
| Medium | 0 |
| High | 0 |
| Unknown | 0 |
| *not annotated* | 0 |

## Security risk

| Value | Units |
|---|---:|
| None | 17 |
| Low | 67 |
| Medium | 66 |
| High | 72 |
| Critical | 0 |
| *not annotated* | 0 |

## Resource impact

| Metric | Value |
|---|---:|
| Maximum | 8 / 10 |
| Average over annotated units | 2.3 / 10 |
| Units scored | 222 |

## High-security review areas

- `Broiler.Regex.BroilerRegex` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.BroilerRegex(string, string?)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.BroilerRegex(string, RegexFlags)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.Match(string, int)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.IsMatch(string, int)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.Matches(string)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher.Matcher(RegexNode, int, RegexFlags, IReadOnlyDictionary<string, int>)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher.Run(string, int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher.Compile(RegexNode, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher.CompileSequence(IReadOnlyList<RegexNode>, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher.CompileDisjunction(IReadOnlyList<RegexNode>, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher.CompileGroup(GroupNode, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher.CompileQuantifier(QuantifierNode, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher.CompileGeneralQuantifier(CompiledMatcher, int[], int, int, bool)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher.TryMatchBodyNth(CompiledMatcher, MatchState, int, out MatchState)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher.CompileSingleCharQuantifier(CharPredicate, int, int, bool, Direction)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher.CompileLookaround(LookaroundNode, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Matching.Matcher.CompileCharClassWithStrings(CharSet, Direction, bool, bool)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.Parse(string, RegexFlags, out int, out IReadOnlyDictionary<string, int>)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParsePattern()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.PreScan()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseDisjunction()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseAlternative()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseTerm()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.TryParseAssertion(out bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.HasQuantifier()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.TryApplyQuantifier(RegexNode)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.TryParseBraceQuantifier(out int, out int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseAtom()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseGroup()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseModifierGroup()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadModifierFlags()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseAtomEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseCharacterClass()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadClassAtom(CharSet, out bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseClassSet()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadPropertyOperand(bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseNestedClass()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseClassSetExpression()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseClassIntersection(ClassSetValue, int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseClassSubtraction(ClassSetValue, int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.RejectMixedOperator(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseClassSetRangeOrOperand()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseClassSetOperand(out int?)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ParseClassStringDisjunction()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.IsClassSetSyntaxCharacter(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.IsClassSetReservedPunctuator(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.IsClassSetReservedDoublePunctuator(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.TryReadClassEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadCharacterEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadLegacyOctalOrIdentity()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadControlEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadHexEscape(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.HasHexDigits(int, int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadUnicodeEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.AddPropertyEscape(CharSet, bool, bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadGroupName()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.AtEnd` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.HasAt(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.Peek()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.PeekAt(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.Expect(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadSourceCodePoint()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.TryReadDecimal(out int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.HexValue(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.IsSyntaxChar(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.RegexFlagsParser.Parse(string?)` in `Broiler.Regex/RegexFlags.cs` - Security=High, human line PENDING
- `Broiler.Regex.Unicode.CaseFolding.FoldTable.Orbit(int)` in `Broiler.Regex/Unicode/CaseFolding.cs` - Security=High, human line PENDING
- `Broiler.Regex.Unicode.UnicodeCharSets.ResolveProperty(string, string?)` in `Broiler.Regex/Unicode/UnicodeCharSets.cs` - Security=High, human line PENDING
- `Broiler.Regex.Unicode.UnicodeCharSets.ResolveStringProperty(EmojiSequenceProperties)` in `Broiler.Regex/Unicode/UnicodeCharSets.cs` - Security=High, human line PENDING

## Falsification criteria

| Metric | Value |
|---|---:|
| Units carrying a criterion | 167 |
| Units required to carry one | 72 |
| Required and missing | 0 |

A `Broiler-Falsified-If:` line states, at the declaration, the observation that would make
the unit wrong. `Security=High` says a unit is risky, which is a set and not a test; the
criterion is the test. It is required where `Security` is `High` or `Critical`, permitted
elsewhere, and `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.RegEx` names every unit that owes one and carries none.

The line is a comment, so it is outside every fingerprint by construction: rewording a
criterion moves no recorded value here, in a file header or in
`assurance.manifest.json`, and invalidates nothing. That is the intended reading - a
criterion is an instruction to whoever reads the unit, not part of what a review is bound to.

## Exemption

Exemption is decided by one predicate in `CSharpAssuranceScanner`, not per unit, so
that the rule is reviewable in one place rather than in several hundred.

| Case | Units |
|---|---:|
| TrivialPropertyOrAccessor | 22 |
| ParameterAssigningConstructor | 6 |
| TrivialExpressionBodiedMember | 1 |
| CompilerSuppliedRecordOrEnumMember | 0 |
| DelegatingOverrideOrOperator | 0 |
| InsideAssemblyMarker | 0 |
| FieldDeclaringStorage | 68 |
| EnumMemberOfADeclaredVocabulary | 21 |
| DeclaredInSource | 0 |

## Per-unit exemptions

| Metric | Value |
|---|---:|
| Per-unit exemptions | 0 |

A per-unit `EXEMPT=<reason>` line exempts one unit by a reason a human wrote, for what the
predicate cannot see. Nothing mechanical checks that the reason is true, that it describes
the unit it sits on, or that it says anything at all, so every use is counted and named
here.

No unit in this component states a per-unit exemption.

## Files not covered

These files are under a covered project, or compiled into one, and are left out of the
record. They carry no generated header, no unit of theirs is in `assurance.manifest.json`, and
no figure in this report counts them. A path ending in `/` is a directory the tool does not
enter, and none of its files is covered.

| File | Reason |
|---|---|
| `Broiler.Regex/Unicode/Generated/CaseFoldingData.g.cs` | generated from the Unicode Character Database by Broiler.Regex/Unicode/tools/generate-case-folding.py, which rewrites the whole file |

## Change detection

`assurance.manifest.json` lists **every** code unit in the covered assembly -
340 of them, exempt and relevant alike - with the fingerprint of its declaration.
This manifest is a change-detection record, not a review. A unit listed there is watched, not reviewed:
the entry records what the declaration's tokens hashed to when the generator last ran, and
nothing else. What the manifest adds is that a unit the exemption predicate treats as
trivial is no longer invisible: a semantic change to one moves a value in a generated file
the check compares byte for byte. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.RegEx` holds the manifest to the tree.

Beside the units it lists **every covered file** - 13 of them - with a
fingerprint over the complete token stream of its compilation unit. A unit entry exists only
for a declaration kind the scanner enumerates, and an enumeration is a whitelist: an
`[assembly: ...]` attribute is a member of nothing and can be in no unit at all.
Nothing in a covered file can change without something moving here, whatever kind of declaration it is. Comments are outside the stream, because a token's
text is its own characters, so the generated header above and the annotation lines below move
no file fingerprint - which is what lets one generation be a fixed point.

## Verification

The generator and the check are one computation: `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.RegEx` works out what the
generator would write and compares it with the tree byte for byte, so a record edited by
hand, or left behind by code that moved, is reported rather than trusted.

| Mode | Command | Effect |
|---|---|---|
| Generate | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.RegEx` | Fills every `Fingerprint=TBF`, refreshes a decision the code has outrun into `STALE; Previous=...`, rewrites the generated headers, `HUMAN_REVIEW.units.md`, `assurance.manifest.json` and this file. |
| Check | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.RegEx` | Reports every generated artefact that is not byte-identical to what the generator would produce, every relevant unit with no annotation, every annotation this system cannot read, every fingerprint out of date and every unit at the top of the security vocabulary without a criterion. |
| Release | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.RegEx --release` | The check, and additionally every relevant unit left in a state that blocks a release. |

The fingerprint is six hex characters - 24 bits - of SHA-256 over the declaration's token
texts, joined by single spaces. Trivia is excluded because a token's text is its own
characters and never the comments or whitespace around it, so `dotnet format` moves no
fingerprint and an annotation is never part of what it describes. The value answers whether a
unit changed since it was reviewed. It is not a collision-free identifier across units and it
is not a cryptographic commitment.
