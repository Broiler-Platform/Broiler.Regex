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
| Files scanned | 25 |
| Files not covered | 0 |
| Files carrying an annotation | 25 |
| Code units | 633 |
| Relevant | 355 |
| Exempt by predicate | 278 |
| Annotated | 355 of 355 (100%) |
| Human reviewed | 0 of 355 (0%) |
| Unverified | 355 |

## Review states

| State | Count |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 355 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 278 |

## IP risk

| Value | Units |
|---|---:|
| None | 268 |
| Low | 104 |
| Medium | 0 |
| High | 0 |
| Unknown | 0 |
| *not annotated* | 0 |

## Security risk

| Value | Units |
|---|---:|
| None | 13 |
| Low | 118 |
| Medium | 105 |
| High | 136 |
| Critical | 0 |
| *not annotated* | 0 |

## Resource impact

| Metric | Value |
|---|---:|
| Maximum | 8 / 10 |
| Average over annotated units | 1.6 / 10 |
| Units scored | 355 |

## High-security review areas

- `Broiler.Regex.Ast.CharSet` in `Broiler.Regex/Ast/CharSet.cs` - Security=High, human line PENDING
- `Broiler.Regex.Ast.CharSet.AddRange(int, int)` in `Broiler.Regex/Ast/CharSet.cs` - Security=High, human line PENDING
- `Broiler.Regex.Ast.CharSet.CheckRange(int, int)` in `Broiler.Regex/Ast/CharSet.cs` - Security=High, human line PENDING
- `Broiler.Regex.Ast.ClassStrings` in `Broiler.Regex/Ast/ClassStrings.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.MaximumNestingLimit` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.BroilerRegex(string, string?, IUnicodeTables)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.BroilerRegex(string, RegexFlags, IUnicodeTables)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.BroilerRegex(string, RegexFlags, RegexUnicode, RegexNesting, bool)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.Compile(string, RegexFlags, IUnicodeTables, int, ulong?, RegexWorkCharge?, out BroilerRegex?, out RegexLimit)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.Match(ReadOnlySpan<char>, int, bool, Span<int>, RegexWorkCharge?, RegexMatchCeilings, out RegexLimit)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.Match(string, int)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.IsMatch(string, int)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.Matches(string)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.BroilerRegex.Run(RegexProgram, string, int, out RegexLimit)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, human line PENDING
- `Broiler.Regex.CompileCharge` in `Broiler.Regex/CompileCharge.cs` - Security=High, human line PENDING
- `Broiler.Regex.CompileCharge.Charge(CompileSite, ulong)` in `Broiler.Regex/CompileCharge.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.Compile(string, RegexFlags, IUnicodeTables, int, out RegexProgram?, bool, CompileCharge?, IEqualityComparer<string>?)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.Compile(RegexNode, int, RegexGroupNames, RegexFlags, RegexUnicode, RegexNesting, out RegexProgram?, bool, IEqualityComparer<string>?)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.Survey(RegexNode, RegexNesting)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.Emit(RegexNode)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.FirstTest()` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.Visit(RegexNode, Scope, Stack<Step>)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.OpenIteration(QuantifierNode, Scope, Stack<Step>)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.CloseIteration(Iteration)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.IsAtom(RegexNode)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.AddAtom(RegexNode, Scope)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.AddAtomRun(SequenceNode, int, int, Scope)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.StringTableOf(CharSet, bool)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.AddClass(CharSet, Scope)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.AddStrings(CharSet, Scope)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.AddAnchor(AnchorKind, Scope)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.AddReference(BackreferenceNode, Scope)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.Run(int, bool, Span<int>)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.Scan(int, ref int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.TakeScanned(int, ref int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.MatchFrom(int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.Afford(int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.AtomHolds(in RegexInstruction, ref int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.EnterRun(in RegexInstruction, int, ref int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.Atoms(in RegexInstruction, int, ref int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.NextWayOfStrings(int, int, int, out int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.StringHolds(RegexStrings, ReadOnlySpan<int>, int, out int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.NextWay(int, out int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.Pay()` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.MatchReference(in RegexInstruction, ref int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.PushChoicePoint(int, int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.PushMarkedFrame(int, int, int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMachine.SetRegister(int, int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMatchCeilings` in `Broiler.Regex/Engine/RegexMatchCeilings.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexMatchCeilings.RegexMatchCeilings(ulong?, int, int)` in `Broiler.Regex/Engine/RegexMatchCeilings.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexProgram` in `Broiler.Regex/Engine/RegexProgram.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexProgram.Match(ReadOnlySpan<char>, int, bool, Span<int>, RegexWorkCharge?, RegexMatchCeilings, out RegexLimit, out ulong)` in `Broiler.Regex/Engine/RegexProgram.cs` - Security=High, human line PENDING
- `Broiler.Regex.Engine.RegexStrings` in `Broiler.Regex/Engine/RegexStrings.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexNesting` in `Broiler.Regex/Parsing/RegexNesting.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexNesting.Descend(int, int)` in `Broiler.Regex/Parsing/RegexNesting.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.Parse(string, RegexFlags, IUnicodeTables, out int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.Parse(string, RegexFlags, RegexUnicode, RegexNesting, out int, out RegexGroupNames)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
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
- `Broiler.Regex.Parsing.RegexParser.ReadClassAtom(CharSet, CodePointSet.Builder, out bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
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
- `Broiler.Regex.Parsing.RegexParser.ReadUnicodeEscape(bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadPropertyEscapeSet()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ResolvePropertyEscape(bool, out bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadGroupName()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.Descend(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.AtEnd` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.HasAt(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.Peek()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.PeekAt(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.Expect(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.ReadSourceCodePoint()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.TryReadDecimal(out int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.HexValue(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.Parsing.RegexParser.IsSyntaxChar(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, human line PENDING
- `Broiler.Regex.RegexFlagsParser` in `Broiler.Regex/RegexFlags.cs` - Security=High, human line PENDING
- `Broiler.Regex.RegexFlagsParser.Parse(string?)` in `Broiler.Regex/RegexFlags.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexEscaper` in `Broiler.Regex/Text/RegexEscaper.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexEscaper.SyntaxCharactersAndSolidus` in `Broiler.Regex/Text/RegexEscaper.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexEscaper.OtherPunctuators` in `Broiler.Regex/Text/RegexEscaper.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexEscaper.EscapeString(string, int, RegexWorkCharge?, out string?)` in `Broiler.Regex/Text/RegexEscaper.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexEscaper.EscapePattern(string, int, RegexWorkCharge?, out string?)` in `Broiler.Regex/Text/RegexEscaper.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexEscaper.EncodeString(string, Span<char>)` in `Broiler.Regex/Text/RegexEscaper.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexEscaper.EncodeCodePoint(ref TextWriter, int)` in `Broiler.Regex/Text/RegexEscaper.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexEscaper.EncodePattern(string, Span<char>, out bool)` in `Broiler.Regex/Text/RegexEscaper.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexEscaper.TextWriter.Hex(int)` in `Broiler.Regex/Text/RegexEscaper.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexEscaper.TextWriter.LineTerminator(char)` in `Broiler.Regex/Text/RegexEscaper.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexEscaper.MeterAccount` in `Broiler.Regex/Text/RegexEscaper.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexEscaper.MeterAccount.Spend(long)` in `Broiler.Regex/Text/RegexEscaper.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexFlagsText` in `Broiler.Regex/Text/RegexFlagsText.cs` - Security=High, human line PENDING
- `Broiler.Regex.Text.RegexFlagsText.Parse(ReadOnlySpan<char>, out RegexFlags, out int)` in `Broiler.Regex/Text/RegexFlagsText.cs` - Security=High, human line PENDING
- `Broiler.Regex.Unicode.RegexUnicode` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, human line PENDING
- `Broiler.Regex.Unicode.RegexUnicode.RegexUnicode(IUnicodeTables, CompileCharge?)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, human line PENDING
- `Broiler.Regex.Unicode.RegexUnicode.OrbitIntersects(CodePointSet, int, bool)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, human line PENDING
- `Broiler.Regex.Unicode.RegexUnicode.EscapeSet(ClassEscape, bool, bool)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, human line PENDING
- `Broiler.Regex.Unicode.RegexUnicode.IsWord(int, bool, bool)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, human line PENDING
- `Broiler.Regex.Unicode.RegexUnicode.BuildCanonicalCharacters()` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, human line PENDING
- `Broiler.Regex.Unicode.RegexUnicode.SimpleCaseFold(CodePointSet)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, human line PENDING
- `Broiler.Regex.Unicode.RegexUnicode.ResolveProperty(string, string?)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, human line PENDING
- `Broiler.Regex.Unicode.RegexUnicode.ResolveStringProperty(StringProperty)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, human line PENDING

## Falsification criteria

| Metric | Value |
|---|---:|
| Units carrying a criterion | 204 |
| Units required to carry one | 136 |
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
| TrivialPropertyOrAccessor | 52 |
| ParameterAssigningConstructor | 7 |
| TrivialExpressionBodiedMember | 0 |
| CompilerSuppliedRecordOrEnumMember | 0 |
| DelegatingOverrideOrOperator | 0 |
| InsideAssemblyMarker | 0 |
| FieldDeclaringStorage | 131 |
| EnumMemberOfADeclaredVocabulary | 88 |
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

No file under a covered project's directory, and no file a covered project compiles in
through a `<Compile Include>` it states, is left out of the record.

## Change detection

`assurance.manifest.json` lists **every** code unit in the covered assembly -
633 of them, exempt and relevant alike - with the fingerprint of its declaration.
This manifest is a change-detection record, not a review. A unit listed there is watched, not reviewed:
the entry records what the declaration's tokens hashed to when the generator last ran, and
nothing else. What the manifest adds is that a unit the exemption predicate treats as
trivial is no longer invisible: a semantic change to one moves a value in a generated file
the check compares byte for byte. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.RegEx` holds the manifest to the tree.

Beside the units it lists **every covered file** - 25 of them - with a
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
