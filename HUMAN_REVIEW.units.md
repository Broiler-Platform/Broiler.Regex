# Human Review: Broiler.RegEx

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.RegEx`, which rewrites this file,
`CODE-ASSURANCE.md`, `assurance.manifest.json` and every generated source header from the
product tree.

> **Status: PENDING.** Human-reviewed: 0 of 305 relevant units. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.RegEx --release`
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

A human may state their own `IP=`, `Security=`, `Resources=`, `CS=` and `CL=` assessment beside their alias,
which is how a reader disagrees with the machine assessment on the line above: an assessment is
a comment and moves no fingerprint, so there is nowhere else to say it.

**No branch, commit or tag is recorded in this file.** Each decision names the fingerprint of
the declaration it was made against, and the state machine compares that value with the
declaration as it now stands. A commit says a tree moved; a fingerprint says whether this unit
did, which is the narrower and the more useful of the two.

## 3. Summary

| Metric | Value |
|---|---:|
| Files scanned | 22 |
| Code units | 547 |
| Relevant | 305 |
| Exempt | 242 |
| Assessed | 305 of 305 (100%) |
| Human reviewed | 0 of 305 (0%) |
| Unverified | 305 |
| Aliases naming a decision | 0 |

## 4. Review States

One row per state of the machine that reads the two lines. The states are computed from the
annotations and the current fingerprints; nothing stores them.

| State | Units |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 305 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 242 |

## 5. Aliases In The Tree

No alias appears on a human line anywhere in the product tree. Nobody has recorded a
decision about any unit of this component.

## 6. Coverage By File

One row per covered file, carrying that file's generated header. `Unverified` counts the
relevant units in a state that blocks a release.

| File | Units | Relevant | Exempt | Unverified | IP risk | Security risk | Criteria |
|---|---:|---:|---:|---:|---|---|---:|
| `Broiler.Regex/Ast/CharSet.cs` | 28 | 15 | 13 | 15 | Low | High | 7/3 |
| `Broiler.Regex/Ast/CodePointSet.cs` | 24 | 20 | 4 | 20 | Low | Medium | 9/0 |
| `Broiler.Regex/Ast/RegexNode.cs` | 46 | 20 | 26 | 20 | None | Low | 0/0 |
| `Broiler.Regex/BroilerRegex.cs` | 19 | 13 | 6 | 13 | Low | High | 13/13 |
| `Broiler.Regex/Compiling/ProgramCompiler.cs` | 50 | 25 | 25 | 25 | None | High | 15/13 |
| `Broiler.Regex/Engine/RegexInstruction.cs` | 8 | 1 | 7 | 1 | None | Medium | 1/0 |
| `Broiler.Regex/Engine/RegexMachine.cs` | 55 | 29 | 26 | 29 | None | High | 19/7 |
| `Broiler.Regex/Engine/RegexMatchCeilings.cs` | 11 | 8 | 3 | 8 | None | High | 2/2 |
| `Broiler.Regex/Engine/RegexOp.cs` | 26 | 1 | 25 | 1 | None | Medium | 0/0 |
| `Broiler.Regex/Engine/RegexProgram.cs` | 17 | 8 | 9 | 8 | None | High | 4/2 |
| `Broiler.Regex/Matching/MatchState.cs` | 15 | 8 | 7 | 8 | Low | Medium | 8/0 |
| `Broiler.Regex/Matching/Matcher.cs` | 70 | 47 | 23 | 47 | Low | High | 36/29 |
| `Broiler.Regex/Matching/RegexMatch.cs` | 17 | 4 | 13 | 4 | None | Low | 2/0 |
| `Broiler.Regex/Parsing/RegexNesting.cs` | 7 | 4 | 3 | 4 | None | High | 2/2 |
| `Broiler.Regex/Parsing/RegexParser.cs` | 85 | 64 | 21 | 64 | Low | High | 60/51 |
| `Broiler.Regex/RegexFlags.cs` | 13 | 4 | 9 | 4 | Low | High | 4/2 |
| `Broiler.Regex/RegexOverflowException.cs` | 2 | 2 | 0 | 2 | Low | Low | 0/0 |
| `Broiler.Regex/RegexStatus.cs` | 11 | 2 | 9 | 2 | None | Low | 0/0 |
| `Broiler.Regex/RegexSyntaxException.cs` | 2 | 1 | 1 | 1 | Low | Low | 0/0 |
| `Broiler.Regex/RegexWorkCharge.cs` | 1 | 1 | 0 | 1 | None | Medium | 0/0 |
| `Broiler.Regex/Unicode/RegexUnicode.cs` | 27 | 19 | 8 | 19 | Low | High | 8/8 |
| `Broiler.Regex/Unicode/UnicodeCharSets.cs` | 13 | 9 | 4 | 9 | Low | Medium | 6/0 |

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
- `Broiler.Regex.Ast.CharSet.AddRange(int, int)` in `Broiler.Regex/Ast/CharSet.cs` - Security=High, Spec=none cited, `11B259`, PENDING
  - Falsified if: a class range written high to low, such as [z-a], is added to the set instead of throwing RegexSyntaxException
- `Broiler.Regex.Ast.CharSet.CheckRange(int, int)` in `Broiler.Regex/Ast/CharSet.cs` - Security=High, Spec=ECMA-262 s22.2.1.1, `64DF54`, PENDING
  - Falsified if: [z-a] is accepted, or [a-a] refused
- `Broiler.Regex.BroilerRegex` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `1B83F8`, PENDING
  - Falsified if: two threads making their first Match calls at the same time on one instance built from [zab] both normalize the class's out-of-order range list in place, and one throws or misses a match that a single thread finds
- `Broiler.Regex.BroilerRegex.MaximumNestingLimit` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=JSD-0081 decision D6, `953223`, PENDING
  - Falsified if: on linux-x64, a pattern nested to this depth, parsed and compiled on a thread of a 1 MiB stack under the JIT or Native AOT, ends the process with a stack overflow, or a pattern of a shape NestingMaximumTests.MatchedShapes lists, at this depth, does so when it is matched
- `Broiler.Regex.BroilerRegex.BroilerRegex(string, string?, IUnicodeTables)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `14510D`, PENDING
  - Falsified if: a flags string with a repeated letter such as "gg" produces an instance instead of throwing RegexSyntaxException
- `Broiler.Regex.BroilerRegex.BroilerRegex(string, RegexFlags, IUnicodeTables)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `807242`, PENDING
  - Falsified if: a pattern of 1,000,000 nested opening parentheses ends the process with a stack overflow instead of throwing RegexSyntaxException
- `Broiler.Regex.BroilerRegex.OnTheNewBackend(string, RegexFlags, IUnicodeTables)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `27F296`, PENDING
  - Falsified if: an instance this answers matches through the old backend's continuations, or refuses a pattern the public constructor accepts
- `Broiler.Regex.BroilerRegex.BroilerRegex(string, RegexFlags, RegexUnicode, RegexNesting, bool, bool)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `7D227B`, PENDING
  - Falsified if: a pattern the parser accepts under a limit is compiled under another, so the compiler's recursion is bounded by neither
- `Broiler.Regex.BroilerRegex.Compile(string, RegexFlags, IUnicodeTables, int, out BroilerRegex?, out RegexLimit)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=JSD-0081 decisions D6, D7 and D9, `3A9104`, PENDING
  - Falsified if: with a nesting limit of 512, a pattern of 512 nested groups or 512 nested v-mode classes answers Success, one of 511 answers LimitExceeded, a limit above MaximumNestingLimit is accepted, or a pattern of 1,000,000 nested groups ends the process
- `Broiler.Regex.BroilerRegex.CompileWithBackend(string, RegexFlags, IUnicodeTables, int, bool, out BroilerRegex?, out RegexLimit)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=JSD-0081 decisions D6, D7 and D9, `B25379`, PENDING
  - Falsified if: with a nesting limit of 512, a pattern of 512 nested groups answers Success on either backend, or one of 511 answers LimitExceeded
- `Broiler.Regex.BroilerRegex.Match(string, int)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `A4E4CD`, PENDING
  - Falsified if: Match(input, -1) does not give the result that Match(input, 0) gives, so a negative start is not clamped to 0
- `Broiler.Regex.BroilerRegex.IsMatch(string, int)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `FC8D95`, PENDING
  - Falsified if: IsMatch(input, start) returns a value other than Match(input, start).Success for the same input and start
- `Broiler.Regex.BroilerRegex.Matches(string)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `9A30B2`, PENDING
  - Falsified if: after an empty match just before a surrogate pair in u or v mode, the next match starts at the pair's low surrogate
- `Broiler.Regex.BroilerRegex.Run(string, int)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=none cited, `081BFB`, PENDING
  - Falsified if: an instance built on the new backend's route matches through the old backend's continuations, or one the public members built through the new backend's program
- `Broiler.Regex.BroilerRegex.RunProgram(RegexProgram, string, int)` in `Broiler.Regex/BroilerRegex.cs` - Security=High, Spec=ECMA-262 s22.2.7.2, `BD98A3`, PENDING
  - Falsified if: under u, /[\s\S]*\u{1F600}/ from index 1 of "\u{1F600}ba" answers other than a match at 1 ending at 2, or a sticky instance finds a match after its start
- `Broiler.Regex.Compiling.ProgramCompiler` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=ECMA-262 s22.2.2, `393186`, PENDING
  - Falsified if: compiling a tree nested to the library's maximum uses more CLR stack than a flat one, a group's, a modifier's or a look-around's body is entered without checking the caller's nesting limit, or a tree the parser built answers anything but a program or the nesting status
- `Broiler.Regex.Compiling.ProgramCompiler.Compile(string, RegexFlags, IUnicodeTables, int, out RegexProgram?)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=none cited, `79AA61`, PENDING
  - Falsified if: a pattern of 512 nested groups under a limit of 512 answers Compiled, or one of 511 answers NestingLimitExceeded
- `Broiler.Regex.Compiling.ProgramCompiler.Compile(RegexNode, int, IReadOnlyDictionary<string, int>, RegexFlags, RegexUnicode, RegexNesting, out RegexProgram?)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=ECMA-262 s22.2.2, `6E37E6`, PENDING
  - Falsified if: a tree the parser built within the nesting limit is answered without a program, a character under the i flag in force is given an instruction that compares it exactly, or one in a look-behind's body an instruction that reads left to right
- `Broiler.Regex.Compiling.ProgramCompiler.Survey(RegexNode, RegexNesting)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=none cited, `DDC27C`, PENDING
  - Falsified if: the groups found for the quantifier of /((a)|(b))*/ are other than 1 to 3, or for the inner one of /(a)(?:(b)(c)+)*/ other than 3 to 3
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=ECMA-262 s22.2.2, `6B9AED`, PENDING
  - Falsified if: a lazy quantifier's choice tries the body before the continuation, or an alternation's choice resumes anywhere but at the next alternative
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.Emit(RegexNode)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=none cited, `7AC8C2`, PENDING
  - Falsified if: an alternation's choice resumes at an address other than the next alternative's first instruction, or a group's end is written before its body
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.Visit(RegexNode, Scope, Stack<Step>)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=ECMA-262 s22.2.2, `4115CF`, PENDING
  - Falsified if: an alternation's alternatives are emitted in an order other than the pattern's, or a sequence's terms in an order other than the pattern's outside a look-behind's body and other than its reverse inside one
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.OpenIteration(QuantifierNode, Scope, Stack<Step>)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=ECMA-262 s22.2.2.3.1, `52344A`, PENDING
  - Falsified if: X{0} emits X, a quantifier with a minimum of zero emits an EnterBelowMinimum, or one with an unbounded maximum an ExitAtMaximum
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.CloseIteration(Iteration)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=ECMA-262 s22.2.2.3.1, `D63006`, PENDING
  - Falsified if: the empty check of a counted quantifier reads the count after the iteration's increment, or a greedy choice's second way is the body
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.AddClass(CharSet, Scope)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=ECMA-262 s22.2.2.7.1, `5A9104`, PENDING
  - Falsified if: the class [^a-c] is given the ranges a to c rather than their complement, [^k] under i is given the complement of k rather than k inverted, or a class's cost is not one plus its weight for each code point its test searches for
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.AddStrings(CharSet, Scope)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=ECMA-262 s22.2.2.7, `71C696`, PENDING
  - Falsified if: [\q{abc|ab}]c fails on abc, [\q{}a] on a matches the empty string, or in a look-behind a string's first character is read first
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.AddAnchor(AnchorKind, Scope)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=ECMA-262 s22.2.2.6, `7D25B8`, PENDING
  - Falsified if: under u with i a word boundary reads a word class without U+017F, or without i or without u one with it
- `Broiler.Regex.Compiling.ProgramCompiler.ProgramWriter.AddReference(BackreferenceNode, Scope)` in `Broiler.Regex/Compiling/ProgramCompiler.cs` - Security=High, Spec=ECMA-262 s22.2.2.7.2, `D6ABF6`, PENDING
  - Falsified if: \k<n> is given another group than the one the parser named n, or a reference under the i flag in force compares exactly
- `Broiler.Regex.Engine.RegexMachine` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, Spec=ECMA-262 s22.2.2, `7D0010`, PENDING
  - Falsified if: a match over a subject of any length, or of a pattern of any nesting within the library's maximum, ends the process with a stack overflow, or a failed attempt leaves a register other than -1 for the next start position
- `Broiler.Regex.Engine.RegexMachine.Run(int, bool, Span<int>)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, Spec=ECMA-262 s22.2.7.2, `016F4F`, PENDING
  - Falsified if: an anchored call tries a position after its start, a search skips the position after the last character, under u a search starts inside a surrogate pair it stepped over, or a read of a call that starts inside a pair takes the pair as one character
- `Broiler.Regex.Engine.RegexMachine.MatchFrom(int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, Spec=ECMA-262 s22.2.2, `5A0B3A`, PENDING
  - Falsified if: an instruction is dispatched before its units are charged, an instruction that fails resumes anywhere but at the newest frame that is not a positive look-around's barrier, a frame resumes with a register write made after it still in force, or a frame a look-around's body pushed is resumed after the look-around's close
- `Broiler.Regex.Engine.RegexMachine.Pay()` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, Spec=JSD-0081 decision D8, `05547C`, PENDING
  - Falsified if: the meter is charged anything but whole blocks, is charged after the work it pays for, or is charged for an instruction the step ceiling refused
- `Broiler.Regex.Engine.RegexMachine.MatchReference(in RegexInstruction, ref int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, Spec=ECMA-262 s22.2.2.7.2, `5AB1B1`, PENDING
  - Falsified if: a reference to a group that did not participate fails, a capture's characters are compared before its length is charged, under u a captured lone high surrogate matches the pair it would begin, or right to left the reference moves the index other than to where the characters it matched start
- `Broiler.Regex.Engine.RegexMachine.PushChoicePoint(int, int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, Spec=none cited, `E0F13C`, PENDING
  - Falsified if: a call holds one frame more than its frame ceiling, or answers LimitExceeded holding fewer
- `Broiler.Regex.Engine.RegexMachine.SetRegister(int, int)` in `Broiler.Regex/Engine/RegexMachine.cs` - Security=High, Spec=none cited, `46CC5B`, PENDING
  - Falsified if: a write that changes a register leaves no trail cell, so backtracking keeps the value of a path that failed, or the trail holds one cell more than its ceiling
- `Broiler.Regex.Engine.RegexMatchCeilings` in `Broiler.Regex/Engine/RegexMatchCeilings.cs` - Security=High, Spec=JSD-0081 decisions D8 and D10, `25E98E`, PENDING
  - Falsified if: a frame or trail ceiling of zero or above its maximum, or a step ceiling of zero, is accepted, or the engines' defaults are other than 1 << 20 frames and 1 << 21 trail cells
- `Broiler.Regex.Engine.RegexMatchCeilings.RegexMatchCeilings(ulong?, int, int)` in `Broiler.Regex/Engine/RegexMatchCeilings.cs` - Security=High, Spec=none cited, `D25D08`, PENDING
  - Falsified if: new RegexMatchCeilings(0, 1, 1), new RegexMatchCeilings(null, 0, 1) or new RegexMatchCeilings(null, 1, TrailCeilingMaximum + 1) does not throw
- `Broiler.Regex.Engine.RegexProgram` in `Broiler.Regex/Engine/RegexProgram.cs` - Security=High, Spec=JSD-0081 decisions D5 and D8, `E22681`, PENDING
  - Falsified if: two threads matching one program at the same time answer differently from one thread matching it twice, or a match call writes a field of the program
- `Broiler.Regex.Engine.RegexProgram.Match(ReadOnlySpan<char>, int, bool, Span<int>, RegexWorkCharge?, RegexMatchCeilings, out RegexLimit, out ulong)` in `Broiler.Regex/Engine/RegexProgram.cs` - Security=High, Spec=ECMA-262 s22.2.7.2, `775D0C`, PENDING
  - Falsified if: /b/ anchored at 0 of "ab" answers Match, /a(b)?/ on "a" writes a slot other than -1 for group 1, a call that reaches a ceiling answers Match or NoMatch, or a call that does not match leaves a slot other than -1
- `Broiler.Regex.Matching.Matcher` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=ECMA-262 s22.2.2, `3E4382`, PENDING
  - Falsified if: /(?<=(\d+)(\d+))$/ matched against '1053' reports groups other than '1' and '053', the result that backward, greedy look-behind matching requires
- `Broiler.Regex.Matching.Matcher.Matcher(RegexNode, int, RegexFlags, IReadOnlyDictionary<string, int>, RegexUnicode, RegexNesting)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `B145AA`, PENDING
  - Falsified if: a pattern compiled with only the v flag matches '.' against one UTF-16 unit of a surrogate pair, because the matcher's Unicode mode stays off
- `Broiler.Regex.Matching.Matcher.Run(string, int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `68BAA0`, PENDING
  - Falsified if: a match found at a later start position reports a group captured during an earlier, failed start position, because the shared initial captures array was written in place
- `Broiler.Regex.Matching.Matcher.Compile(RegexNode, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `7AAEA8`, PENDING
  - Falsified if: compiling a tree nested to the library's maximum uses more CLR stack than a flat one, or a group's, a look-around's or a modifier's body is entered without checking the caller's nesting limit, or /(a)*/ on 'aa' reports group 1 as unmatched because a capturing body took the single-code-point fast path
- `Broiler.Regex.Matching.Matcher.Build(Step, Stack<CompiledMatcher>)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `E47432`, PENDING
  - Falsified if: a sequence's or an alternation's children are taken in reverse order, so /ab/ matches 'ba' or /a|ab/ on 'ab' matches 'ab'
- `Broiler.Regex.Matching.Matcher.Chain(CompiledMatcher[], Direction)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `AAFADF`, PENDING
  - Falsified if: a backward sequence applies its terms left to right, so /(?<=(\d+)(\d+))$/ on '1053' reports groups other than '1' and '053'
- `Broiler.Regex.Matching.Matcher.CompileAtomRun(CharPredicate[])` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `6E2087`, PENDING
  - Falsified if: /abc/ on 'ab' throws IndexOutOfRangeException instead of failing when the subject ends partway through the run
- `Broiler.Regex.Matching.Matcher.Alternation(CompiledMatcher[])` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `73E749`, PENDING
  - Falsified if: an alternation answers a later alternative's match although an earlier one matches, so /a|ab/ on 'ab' matches 'ab'; or, as it does today until task 2.4c routes matching to task 2.4a's meter, a quantifier-free pattern of 40 copies of (?:a|a) followed by b, run on 40 'a' characters, explores about 2^40 paths because no alternative consumes a Budget step
- `Broiler.Regex.Matching.Matcher.CompileGroup(GroupNode, Direction, CompiledMatcher)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `FD28DF`, PENDING
  - Falsified if: /(?<=(ab))c/ on 'abc' reports group 1 with an index other than 0 or a length other than 2
- `Broiler.Regex.Matching.Matcher.CompileGeneralQuantifier(CompiledMatcher, int[], int, int, bool)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=ECMA-262 s22.2.2.3.1, `81AB79`, PENDING
  - Falsified if: the empty alternative of /(?:a|)*b/ on 'c' is accepted as an iteration, so the attempt runs until the 10_000_000-step budget ends it instead of failing after a few steps
- `Broiler.Regex.Matching.Matcher.TryMatchBodyNth(CompiledMatcher, MatchState, int, out MatchState)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `C40A56`, PENDING
  - Falsified if: for the body (?:a|ab) at the start of 'ab', a skip of 1 yields a state at index 1 instead of 2, or a skip of 2 returns true
- `Broiler.Regex.Matching.Matcher.TryGetSingleCharPredicate(RegexNode, Flags, out CharPredicate?)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `B3054A`, PENDING
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
- `Broiler.Regex.Matching.Matcher.CompileLookaround(LookaroundNode, CompiledMatcher)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `A0AE04`, PENDING
  - Falsified if: a positive look-behind leaves the cursor where its body ended, so /(?<=ab)c/ fails on 'abc' or reports a match index other than 2
- `Broiler.Regex.Matching.Matcher.CompileChar(int, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `C1F548`, PENDING
  - Falsified if: under the u flag, /\u{1F600}$/u fails on the surrogate pair 😀 because the character matcher advances by one UTF-16 unit instead of two
- `Broiler.Regex.Matching.Matcher.CompileAnyChar(Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `3AB966`, PENDING
  - Falsified if: without the s flag, /./ matches U+2028 LINE SEPARATOR
- `Broiler.Regex.Matching.Matcher.CompileCharClass(CharSet, Direction, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `0463A9`, PENDING
  - Falsified if: under the u flag, /^[^a]$/u fails on the surrogate pair 😀 because the class consumes only its high half
- `Broiler.Regex.Matching.Matcher.CompileCharClassWithStrings(CharSet, Direction, bool, bool)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `7A1F27`, PENDING
  - Falsified if: /[\q{abc|ab}]c/v fails on 'abc' because the class does not fall back from its longest string member to a shorter one
- `Broiler.Regex.Matching.Matcher.TryMatchLiteral(MatchState, Direction, string, bool, bool, out int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `E0F974`, PENDING
  - Falsified if: in a look-behind, a string member longer than the text before the cursor, such as \q{abc} at index 2, is read from a negative index instead of failing
- `Broiler.Regex.Matching.Matcher.ReadCodePoint(MatchState, Direction, out int, out int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `A65667`, PENDING
  - Falsified if: a forward read at the end of the subject, or a backward read at index 0, reports success
- `Broiler.Regex.Matching.Matcher.CodePointAt(string, int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `0722EA`, PENDING
  - Falsified if: without the u flag, a surrogate pair is read as one code point of width 2
- `Broiler.Regex.Matching.Matcher.CodePointBefore(string, int)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `B85E41`, PENDING
  - Falsified if: under the u flag, the code point before index 2 of 😀 is returned as U+DE00 with width 1 instead of U+1F600 with width 2
- `Broiler.Regex.Matching.Matcher.IsWordBoundary(MatchState, Flags)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `645B40`, PENDING
  - Falsified if: \b reports a boundary at index 0 of an empty subject
- `Broiler.Regex.Matching.Matcher.RegionEquals(string, int, string, int, int, bool, bool)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `1B38A4`, PENDING
  - Falsified if: a region whose last unit is the high half of a surrogate pair compares equal although the pair's low half lies past the region's end
- `Broiler.Regex.Matching.Matcher.ReadAt(string, int, bool)` in `Broiler.Regex/Matching/Matcher.cs` - Security=High, Spec=none cited, `BA00B4`, PENDING
  - Falsified if: under the u flag, a high surrogate at the last index of the subject reads input[pos + 1] and throws IndexOutOfRangeException
- `Broiler.Regex.Parsing.RegexNesting` in `Broiler.Regex/Parsing/RegexNesting.cs` - Security=High, Spec=JSD-0081 decision D6, `E3A9A6`, PENDING
  - Falsified if: with a limit of 512, a pattern of 511 nested groups is refused, or one of 512 nested groups or 512 nested v-mode classes is accepted
- `Broiler.Regex.Parsing.RegexNesting.Descend(int, int)` in `Broiler.Regex/Parsing/RegexNesting.cs` - Security=High, Spec=JSD-0081 decision D6, `6580D7`, PENDING
  - Falsified if: Descend(Limit - 1, p) throws, or Descend(Limit, p) returns instead of throwing
- `Broiler.Regex.Parsing.RegexParser` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `0CA2F0`, PENDING
  - Falsified if: a pattern of 50,000 nested '(' characters ends the process with a stack overflow instead of raising a catchable exception
- `Broiler.Regex.Parsing.RegexParser.Parse(string, RegexFlags, IUnicodeTables, out int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `2D8D8D`, PENDING
  - Falsified if: the capture count it returns for /(a)(?<n>b)/ is not 2
- `Broiler.Regex.Parsing.RegexParser.Parse(string, RegexFlags, RegexUnicode, RegexNesting, out int, out IReadOnlyDictionary<string, int>)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `47A0F9`, PENDING
  - Falsified if: the capture count it returns for /(a)(?<n>b)/ is not 2, or the name map does not send n to 2
- `Broiler.Regex.Parsing.RegexParser.ParsePattern()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `A1C02A`, PENDING
  - Falsified if: a pattern with unconsumed trailing text such as 'a)' returns a tree instead of throwing RegexSyntaxException
- `Broiler.Regex.Parsing.RegexParser.PreScan()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `83B68B`, PENDING
  - Falsified if: a '(' inside a class, or after a backslash, is counted as a capturing group, so /[(]\1/u is accepted
- `Broiler.Regex.Parsing.RegexParser.ParseDisjunction()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `598B1F`, PENDING
  - Falsified if: a pattern of 100,000 nested '(?:' groups exhausts the native stack in this recursion instead of raising a catchable exception
- `Broiler.Regex.Parsing.RegexParser.ParseAlternative()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `FBAC09`, PENDING
  - Falsified if: a raw U+0000 character inside the pattern ends the alternative early, so the text after it is lost or rejected
- `Broiler.Regex.Parsing.RegexParser.ParseTerm()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `3B0D85`, PENDING
  - Falsified if: a quantifier after a look-behind, ^, $, \b or \B, or after any assertion under u or v, is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.TryParseAssertion(out bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `51533B`, PENDING
  - Falsified if: 100,000 nested '(?=' look-aheads exhaust the native stack instead of raising a catchable exception
- `Broiler.Regex.Parsing.RegexParser.HasQuantifier()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `C002FB`, PENDING
  - Falsified if: outside u and v, /^{2}/ is accepted or /^{a}/ is rejected, because the cursor is not restored after the brace probe
- `Broiler.Regex.Parsing.RegexParser.TryApplyQuantifier(RegexNode)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `EAFCF6`, PENDING
  - Falsified if: a '{' that opens no valid quantifier, as in /a{,5}/ outside u, is consumed instead of being left as literal text
- `Broiler.Regex.Parsing.RegexParser.TryParseBraceQuantifier(out int, out int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `BCCEC8`, PENDING
  - Falsified if: {3,2} is accepted, or {2147483648} wraps to a negative count instead of saturating at int.MaxValue
- `Broiler.Regex.Parsing.RegexParser.ParseAtom()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `42FB2F`, PENDING
  - Falsified if: under u or v an unescaped ']', '{' or '}' is returned as a literal character instead of raising a syntax error, or without them /{2}/ is read as the text {2}
- `Broiler.Regex.Parsing.RegexParser.ParseGroup()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `1B81BA`, PENDING
  - Falsified if: a capturing group is numbered after the groups nested inside it, so the outer group of /((a)b)/ is not capture 1
- `Broiler.Regex.Parsing.RegexParser.ParseModifierGroup()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `FE26C3`, PENDING
  - Falsified if: the i flag added by (?i:...) is still in effect after its ')', so a following v-mode class is case-folded
- `Broiler.Regex.Parsing.RegexParser.ReadModifierFlags()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `54D512`, PENDING
  - Falsified if: a repeated modifier flag such as (?ii:a) is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ParseAtomEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `3F4356`, PENDING
  - Falsified if: under u a numeric escape above the capture count, such as \2 in /(a)\2/u, is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ParseCharacterClass()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `91F138`, PENDING
  - Falsified if: under u a range with a class-escape endpoint such as [\d-z] is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ReadClassAtom(CharSet, CodePointSet.Builder, out bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `A95C6E`, PENDING
  - Falsified if: inside a class \b yields anything but U+0008, \- throws under u instead of yielding a hyphen, or without u [\c1] holds anything but U+0011
- `Broiler.Regex.Parsing.RegexParser.ParseClassSet()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `8B4152`, PENDING
  - Falsified if: UsesPropertyEscape stays set from an earlier class, so the second class of /[\p{L}][a]/v is flagged as using a property escape
- `Broiler.Regex.Parsing.RegexParser.ReadPropertyOperand(bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.2.9, `602075`, PENDING
  - Falsified if: under vi \P{Ll} is complemented before its members are folded, so /\P{Ll}/vi matches 'a'
- `Broiler.Regex.Parsing.RegexParser.ParseNestedClass()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `CB6961`, PENDING
  - Falsified if: a v-mode class nested 100,000 '[' deep exhausts the native stack instead of raising a catchable exception
- `Broiler.Regex.Parsing.RegexParser.ParseClassSetExpression()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `D52747`, PENDING
  - Falsified if: a v-mode class repeating \p{RGI_Emoji} a thousand times makes billions of string comparisons, because each union scans the accumulated string list once per member
- `Broiler.Regex.Parsing.RegexParser.ParseClassIntersection(ClassSetValue, int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `EA87D2`, PENDING
  - Falsified if: a string present in only one operand survives the intersection, so /[\q{ab}&&\q{cd}]/v matches 'ab'
- `Broiler.Regex.Parsing.RegexParser.ParseClassSubtraction(ClassSetValue, int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `806DAC`, PENDING
  - Falsified if: a string of the right operand survives the subtraction, so /[\q{ab|cd}--\q{ab}]/v matches 'ab'
- `Broiler.Regex.Parsing.RegexParser.RejectMixedOperator(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `9001F0`, PENDING
  - Falsified if: [a--b&&c] or [a&&b--c] under v is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ParseClassSetRangeOrOperand()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `4A932C`, PENDING
  - Falsified if: a range endpoint that is a class escape or \q{...}, as in [a-\d] under v, is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ParseClassSetOperand(out int?)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `D908B6`, PENDING
  - Falsified if: an unescaped '(' or a doubled punctuator such as '!!' is accepted as a literal inside a v-mode class
- `Broiler.Regex.Parsing.RegexParser.ParseClassStringDisjunction()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `DCF3FD`, PENDING
  - Falsified if: an empty alternative, as in \q{a|}, is dropped instead of making the class match the empty string
- `Broiler.Regex.Parsing.RegexParser.IsClassSetSyntaxCharacter(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `596E48`, PENDING
  - Falsified if: it returns false for one of ( ) [ ] { } / - \ | or true for any other character
- `Broiler.Regex.Parsing.RegexParser.IsClassSetReservedPunctuator(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `1CB1BF`, PENDING
  - Falsified if: it returns true for '$' or '^', or false for one of & - ! # % , : ; < = > @ ` ~
- `Broiler.Regex.Parsing.RegexParser.IsClassSetReservedDoublePunctuator(char)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `8B8981`, PENDING
  - Falsified if: it returns true for '-' or '|', or false for one of & ! # $ % * + , . : ; < = > ? @ ^ ` ~
- `Broiler.Regex.Parsing.RegexParser.TryReadClassEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `4612E7`, PENDING
  - Falsified if: a letter other than d, D, w, W, s or S, such as b, is consumed as a class escape
- `Broiler.Regex.Parsing.RegexParser.ReadCharacterEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `BF6DE9`, PENDING
  - Falsified if: under u an identity escape of a non-syntax character such as \a or \- outside a class is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.ReadLegacyOctalOrIdentity()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `F85821`, PENDING
  - Falsified if: an octal escape above 0377, such as \400, is read as one code point greater than 0xFF instead of \40 followed by '0'
- `Broiler.Regex.Parsing.RegexParser.ReadControlEscape()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `3A9EEB`, PENDING
  - Falsified if: outside u, /\c1/ does not match the text backslash-c-1 because the c is consumed along with the backslash
- `Broiler.Regex.Parsing.RegexParser.ReadHexEscape(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `690F4D`, PENDING
  - Falsified if: a non-hex character inside the fixed-width run, as in \x4g under u, is accepted instead of throwing
- `Broiler.Regex.Parsing.RegexParser.HasHexDigits(int, int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=none cited, `44C9C9`, PENDING
  - Falsified if: outside u, \x4 at the end of the pattern is decoded as a hex escape instead of the identity escape x followed by 4
- `Broiler.Regex.Parsing.RegexParser.ReadUnicodeEscape(bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1, `482AAD`, PENDING
  - Falsified if: under u, \uD83D\u{41} is rejected as an invalid hexadecimal escape instead of reading a lone lead surrogate followed by A, or \uD83D\u{DC38} is read as the one code point U+1F438
- `Broiler.Regex.Parsing.RegexParser.AddPropertyEscape(CharSet, bool, out bool, bool)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.2.9, `769AC3`, PENDING
  - Falsified if: a negated property of strings such as \P{RGI_Emoji} under v is accepted instead of throwing, or \p{RGI_Emoji} reports isStringProperty false
- `Broiler.Regex.Parsing.RegexParser.ReadGroupName()` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=ECMA-262 s22.2.1.1, `E75C85`, PENDING
  - Falsified if: a group name holding a lone surrogate escape, such as (?<\uD800>a), escapes as ArgumentOutOfRangeException instead of RegexSyntaxException, or (?<1>a) or (?<a-b>a) is accepted
- `Broiler.Regex.Parsing.RegexParser.Descend(int)` in `Broiler.Regex/Parsing/RegexParser.cs` - Security=High, Spec=JSD-0081 decision D6, `266817`, PENDING
  - Falsified if: a group opened at the limit's depth is entered instead of refused, so the parser's recursion outgrows the limit
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
- `Broiler.Regex.Unicode.RegexUnicode` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, Spec=ECMA-262 s22.2.2.7.3, `3576CE`, PENDING
  - Falsified if: a compiled pattern reads a Unicode answer that no IUnicodeTables call made during its compilation or matching could have supplied, so a recording provider misses a table the library read
- `Broiler.Regex.Unicode.RegexUnicode.RegexUnicode(IUnicodeTables)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, Spec=none cited, `4DA887`, PENDING
  - Falsified if: tables whose UnicodeVersion is not the provider's are accepted, so the library matches against a data version no caller pinned
- `Broiler.Regex.Unicode.RegexUnicode.OrbitIntersects(CodePointSet, int, bool)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, Spec=ECMA-262 s22.2.2.7.1, `43DE79`, PENDING
  - Falsified if: under iu, a class holding only U+212A KELVIN SIGN is reported not to hold a code point case-equivalent to 'k'
- `Broiler.Regex.Unicode.RegexUnicode.EscapeSet(ClassEscape, bool, bool)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, Spec=ECMA-262 s22.2.2.9, `B86BFA`, PENDING
  - Falsified if: EscapeSet(NonWord, ignoreCase: true, unicode: true) contains U+017F, so /\W/iu matches LATIN SMALL LETTER LONG S
- `Broiler.Regex.Unicode.RegexUnicode.IsWord(int, bool, bool)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, Spec=ECMA-262 s22.2.2.4.1, `03C9D1`, PENDING
  - Falsified if: IsWord(0x017F, ignoreCase: true, unicode: true) returns false, so \b under iu treats LATIN SMALL LETTER LONG S as a non-word character
- `Broiler.Regex.Unicode.RegexUnicode.BuildCanonicalCharacters()` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, Spec=ECMA-262 s22.2.2.9.4, `1593A1`, PENDING
  - Falsified if: the set it builds contains 0x0041 or U+212A KELVIN SIGN, whose simple case folds are not themselves
- `Broiler.Regex.Unicode.RegexUnicode.ResolveProperty(string, string?)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, Spec=ECMA-262 s22.2.2.9.7, `76BE3F`, PENDING
  - Falsified if: a loosely spelled name such as \p{letter}, \p{Script=greek} or \p{ScriptExtensions=Greek} resolves to a set instead of returning null, or \p{Script=Katakana_Or_Hiragana} returns null
- `Broiler.Regex.Unicode.RegexUnicode.ResolveStringProperty(StringProperty)` in `Broiler.Regex/Unicode/RegexUnicode.cs` - Security=High, Spec=ECMA-262 s22.2.2.9, `34C07C`, PENDING
  - Falsified if: \p{RGI_Emoji} under v resolves to fewer code points or sequences than the union of the six properties it is defined as

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

305 of the 305 assessed units declare `Origin=AI`. Reading a declaration is the only thing
that makes it read.
