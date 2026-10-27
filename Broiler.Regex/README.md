# Broiler.Regex

A from-scratch, **ECMAScript-oriented** regular-expression engine for the
Broiler.JS runtime.

> **Preview status:** The engine is incomplete, its routing integration is incremental,
> and it must not be described as fully conformant while the limitations below remain.
> Substantial implementation work was AI-assisted. Human-review approval is
> revision-scoped; consult the repository [human review](../HUMAN_REVIEW.md) for the
> reviewed revision and conditions before describing the current checkout as approved.

It exists to close the gap between the ECMAScript regular-expression grammar /
matching semantics (ECMA-262 §22.2) and what `System.Text.RegularExpressions`
can express. The existing engine
([`JSRegExp.cs`](../../Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.cs)) translates a
JS pattern into a .NET `Regex` through ~5000 lines of source-to-source patches.
That strategy has carried us a long way, but a class of failures is **structurally
impossible** to fix by pattern rewriting, because they are differences in the
*matching algorithm*, not the *surface syntax*.

This project is the long-term replacement for that translation layer: a direct
implementation of the spec's backtracking matcher (§22.2.2), so the hard cases
are correct *by construction* rather than by patch.

> **Status: working core, wired into `JSRegExp`.** The parser and the backtracking
> matcher implement the common grammar and the gap cases below. Unicode property
> escapes (`\p{…}`), `v`-mode class-set expressions and both branches of `Canonicalize`
> are implemented and read the Unicode owner's provider,
> [`Broiler.Unicode.Algorithms`](https://github.com/Broiler-Platform/Broiler.Unicode), over
> the tables the caller hands in (requirement R11). As of issue #923 the engine is **wired
> into `JSRegExp`** behind a conservative gap-feature router
> (`JSRegExp.TryBuildBroilerForGaps`): only patterns that hit a documented JS/.NET
> gap are matched here; everything else still uses the .NET translator unchanged —
> see *Integration plan* below.

---

## Why .NET `Regex` cannot be patched into conformance

These are the `RegExp` failures from
[issue #923](https://github.com/MaiRat/Broiler.JS/issues/923) and the reason each
is a *semantic* gap, not a syntax gap:

| # | test262 sample | .NET behaviour | Root cause |
|---|----------------|----------------|------------|
| 3 | `RegExp/lookBehind/mutual-recursive.js` | wrong / null capture | .NET matches lookbehind **right-to-left** and discards/duplicates captures differently from JS |
| 4 | `RegExp/lookBehind/back-references-to-captures.js` | `[c, abab]` vs `[c, ab]` | A back-reference *to a group captured inside a lookbehind* resolves against .NET's reversed capture, not the JS forward capture |
| 6 | `sm/RegExp/unicode-back-reference.js` | match vs `null` | Back-reference compares **code units**, not **code points**, and skips ECMAScript case-fold equivalence |
| 7 | `sm/RegExp/unicode-braced.js` | `🐸` vs `null` | `\u{1F438}` astral atom + quantifier must be **one code point**; .NET treats the surrogate pair as two units |
| 8 | `RegExp/nullable-quantifier.js` | `"a"` vs `"ab"` | ECMAScript `RepeatMatcher` **abandons** an empty-string iteration of a `min=0` quantifier; .NET keeps looping, producing a different (shorter) match |

The common thread: ECMAScript's matcher is a specific **continuation-passing
backtracker** with rules .NET's engine simply does not share — lookbehind that
matches backward *but preserves forward capture order*, an empty-match guard in
the repeat loop, and code-point-level atoms. You cannot rewrite a pattern to make
a different engine adopt these rules; you have to *be* the engine.

(Issue #923 Problems 1, 2, 5, 9 are `eval` / lexical-environment bugs, unrelated
to regex, and are out of scope for this component.)

---

## Architecture

One backend, since SJL-11.2 task 2.4c deleted the continuation-passing matcher of `Matching/`: the
parser builds a tree, the program compiler lowers it to an instruction program, and one loop runs
the program, with no CLR recursion.

```
pattern ──▶ RegexParser ──▶ AST ──▶ ProgramCompiler ──▶ RegexProgram ──▶ RegexMachine ──▶ caller's slots
            (Parsing/)     (Ast/)   (Compiling/)        (Engine/)        (Engine/)            │
                                                                                              ▼
                                                              BroilerRegex: offset entry, or RegexMatch
```

* **`Parsing/RegexParser.cs`** — recursive-descent parser for the ECMAScript
  `Pattern` grammar (§22.2.1): disjunction, quantifiers, character classes,
  groups (capturing / non-capturing / named / inline-modifier), assertions
  (anchors, word boundaries, look-around), and escapes (including `\u{…}` and
  back-references). Produces a `RegexNode` tree, descending only under the
  caller's nesting limit (`Parsing/RegexNesting.cs`).
* **`Ast/RegexNode.cs`** — the node hierarchy + `CharSet` (character-class model).
* **`Compiling/ProgramCompiler.cs`** — lowers the tree to a `RegexProgram` on work lists of the heap,
  with no recursion, checking the caller's nesting limit before it enters each group, modifier group
  and look-around. Under `i` a character is lowered to its Canonicalize and a class to its members as
  written; a modifier group's body under the flags it sets; a look-around to a barrier around its
  body, a look-behind's body right to left (its terms last first, each character and back-reference
  reading backward, a group's end register written first so that captures read in source order). Its
  fast paths (task 2.4c) lower a quantifier over one character or class to a **run**, two or more
  one-character atoms in a row to an **atom run**, and a `v`-mode class of strings to one lookup in a
  **table** of its strings keyed by the character a string begins with, which every occurrence of the
  same strings shares; and it records the one-character test every attempt makes first, the
  **literal-prefix prefilter**. An internal option turns the fast paths off, the general lowering,
  which a differential test compares with them.
* **`Engine/`** — `RegexOp` and `RegexInstruction` are the instruction set; `RegexProgram` is the
  immutable program, shared across threads; `RegexMachine` is the one backtracking loop, with an
  explicit frame stack and an undo trail (defunctionalised continuations) rented from the shared
  array pool, and no CLR recursion, which an architecture test reads from the IL. A look-around is a
  barrier frame on the same stack; a run or a class of strings holds one marked frame that gives back,
  takes or tries its next way in place; a search scans start positions with the program's first test
  before it enters the program. Every dispatched instruction, every character a fast path examines,
  every position the scan skips and a back-reference's length are charged to the caller's meter,
  `RegexWorkCharge`, in blocks of 512 units paid before the work; `RegexMatchCeilings` holds the
  caller's step, frame and trail ceilings, each answered as `RegexStatus.LimitExceeded` naming it.
* **`CompileCharge.cs`** — one compilation's account (task 2.5, requirement R2): the caller's meter,
  charged in blocks of 512 units before the work as a match's is, and the caller's compile budget,
  past which no work is done. Each place whose work the pattern or the provider's data decides
  charges it before doing it, named by `CompileSite`: the pattern's code units; a property escape's
  ranges, and a property of strings' sequences and their code units; each range a union, an
  intersection, a difference or a complement reads, and the sort of ranges gathered out of order; a
  fold's ranges and the code points it maps one at a time; a class string's code units each time it
  is added, looked up or taken, and the class's order; the sets built from the provider (`Cased`,
  `\s`, the folded `\w`, `AllCharacters` under `vi`); and the program compiler's copies of class
  ranges and string tables.
* **`BroilerRegex.cs`** — the public façade. The **offset entry**,
  `Match(ReadOnlySpan<char>, int, bool, Span<int>, RegexWorkCharge?, RegexMatchCeilings, out RegexLimit)`,
  takes a start, an anchored argument independent of `y`, the caller's slots, meter and ceilings, and
  answers a status, allocating nothing once the pool is warm (provisional until task 2.9). The
  string-bearing `Match` / `IsMatch` / `Matches`, which mirror the shape `JSRegExp` needs
  (`Success`, `Index`, `Length`, indexed and named groups), run the same program as RegExpBuiltinExec,
  with no meter and the engines' frame and trail ceilings (decision D10). `Compile(string, RegexFlags,
  IUnicodeTables, int nestingLimit, ulong? compileBudget, RegexWorkCharge?, out BroilerRegex?, out
  RegexLimit)` compiles under the caller's nesting limit, compile budget and meter and answers
  `Success` or `LimitExceeded` naming `Nesting` or `Compile` (provisional until task 2.9); the
  constructors take neither budget nor meter.
* **`Ast/CodePointSet.cs`** — sorted code-point ranges with union, intersection,
  difference and complement, immutable once built, so a set is shared between threads. Every class member kind reduces to one of these, so
  `v`-mode `&&` / `--` / nesting is plain set algebra rather than a case per operand. A compilation
  calls internal overloads that charge its account a unit per range read; the public members charge
  nothing.
* **`Ast/ClassStrings.cs`** — the strings of a `v`-mode class operand while the parser builds it: an
  insertion-ordered hash set (task 2.5), so a union, an intersection, a difference or a `\q{…}` reads
  each string a bounded number of times, where the list it replaces was searched before each
  addition.
* **`Unicode/RegexUnicode.cs`** — every Unicode answer a compilation reads, through the
  provider over the caller's `IUnicodeTables`: `\p{…}` resolution by the exact names
  ECMA-262 admits (General_Category, binary properties, `Script`, `Script_Extensions`,
  and the UTS #51 properties of strings), both branches of `Canonicalize` and the fold
  orbits the class-membership test walks, `\s`, the folded `\w` set and `AllCharacters`, the
  last three built the first time a compilation reads them. MaybeSimpleCaseFolding folds a set of
  more than 4,096 code points through `Cased`, which holds every code point simple case folding
  moves, so its work is in proportion to the set's ranges. The package declares no Unicode version of
  its own.
* **`Unicode/UnicodeCharSets.cs`** — the parts of the class escapes that are not Unicode
  data: `\d`, the basic `\w` set and the code space, and the public members of `0.1` that
  read no table (`IsDigit`, `PositiveEscape`, `IsComplementEscape`, `PropertyMatch`).
* **`Text/`** — the text helpers both engines' `RegExp` objects call (requirement R9, task 2.7; provisional until
  task 2.9 fixes the surface). `RegexFlagsText.Parse` reads a flags string as a `RegexFlagsStatus` with the position of
  the code unit it refuses (an unknown flag, a repeated flag, `u` with `v`) and `RegexFlagsText.Format` writes a flags
  value in the `flags` getter's order, `dgimsuvy`. `RegexEscaper.EscapeString` is `RegExp.escape`'s text, over the
  provider's `EcmaScriptLexical` white space (so U+0085 is not escaped), and `RegexEscaper.EscapePattern` the
  `source` getter's text (EscapeRegExpPattern: `/` outside a class and the line terminators escaped, the empty pattern
  `(?:)`). Both take the caller's maximum length, answered as `LimitExceeded`, and its meter, charged one unit per code
  unit read and written, in blocks of 512 before each pass. An engine keeps its own argument checks, receivers,
  property reads and message text (conventions 2 and 3).

## What works today

- Literals, `.` (with/without `s`), character classes incl. ranges & negation
- Alternation, sequencing, the empty pattern
- Greedy & lazy quantifiers `* + ? {n} {n,} {n,m}` with the spec empty-match guard
- Capturing / non-capturing / **named** groups and named/numeric back-references
- Anchors `^ $` (with/without `m`), word boundaries `\b \B`
- Look-ahead and **look-behind** (positive & negative), backward-matching with
  preserved capture order
- `u`-mode code-point semantics: `\u{…}`, astral atoms as single units,
  code-point back-references
- Flags `g i m s u y d v`; `i` case-folding from the full UCD tables in both the
  Unicode and the non-Unicode mode, including the supplementary-plane scripts
- `\p{…}` / `\P{…}` property escapes: General_Category, binary properties, `Script`,
  `Script_Extensions`, and the UTS #51 properties of strings under `v`
- `v`-mode class-set expressions: nesting, `&&`, `--`, `\q{…}` string alternatives,
  and the fold-then-complement ordering that makes `\P{Lu}` mean something
  different under `vi` than under `ui`
- The text helpers of requirement R9: flags read as a status with a position and written in canonical order,
  `RegExp.escape`'s text and the `source` text (`Text/`)
- The Annex B / Unicode-mode grammar split: `\u{…}`, a bare `]`/`{`/`}`, a class escape
  as a range endpoint, a quantified look-ahead, and `\k<n>` in a pattern that declares no
  group name are each a literal or a legal term in one mode and a syntax error in the
  other

## Known limitations (stubbed / TODO)

- **Nesting is bounded by the caller's limit, not by the stack** (requirement R4). The parser
  checks the limit before it enters each group and, under `v`, each class, and the program compiler,
  which walks the tree on the heap, checks it again; a pattern nested deeper is refused before the
  level past the limit is read. `BroilerRegex.Compile` takes the caller's limit, up to
  `BroilerRegex.MaximumNestingLimit` (512, the limit both engines pass), and answers
  `RegexStatus.LimitExceeded` with `RegexLimit.Nesting`; the constructors and `RegexParser.Parse`
  parse under the maximum and refuse a deeper pattern with a `RegexSyntaxException`. The parser
  still recurses a few frames per level, under that limit: on linux-x64, a pattern of every shape
  measured, nested to the maximum, parses and compiles on a 1 MiB stack under the JIT and Native AOT
  (the measurement and its margins are in the constant's documentation); win-x64 was not measured,
  so the maximum is proven on linux-x64 only.
- **Matching does not recurse** (R4's matcher half, check A-4). Since task 2.4c every member
  matches through the new backend, whose loop keeps its choices on the heap: every shape measured
  matches at the nesting maximum on a 1 MiB thread, under the JIT (`NestingMaximumTests`, all of its
  shapes) and under Native AOT (the probe's N and E cases), and a flat sequence of 100,000 terms
  matches there too. The old matcher's continuations recursed per level and per term, and failed on
  a 1 MiB thread at 321 to 1,053 levels for the shapes the review of task 2.3 measured, ending the
  process or throwing `RegexOverflowException`, and at 5,000 capturing groups in a row; the type
  and the failures are gone.
- **The string-bearing members bound memory, not time** (decision D10). `Match(string, int)`,
  `IsMatch` and `Matches` pass no meter and no step ceiling, as an engine that passes no meter does,
  and the engines' ceilings of 1,048,576 frames and 2,097,152 trail cells. A pattern that backtracks
  catastrophically, such as `(a+)+b` on a long run of `a`, runs to its answer, which can take time
  exponential in the subject's length; the old matcher's per-start step budget answered no match
  instead, which requirement R1 forbids. A caller that matches untrusted patterns uses the offset
  entry with a meter or a step ceiling. A match that reaches a frame or trail ceiling is answered as
  that ceiling, never as no match: `Match` with a failed `RegexMatch` whose `Limit` names it,
  `Matches` with that failed match as its last element, and `IsMatch`, whose `bool` has no room for
  it, with an `InvalidOperationException`, which departs from convention 3 until task 2.9 decides
  the member's form.
- **A loop over a body of more than one character fills the trail** (task 2.4a's bound). The trail
  keeps every register write of an attempt until it ends, as Broiler.VM's does, a loop's included
  when it holds no choice: a counted loop writes two cells an iteration, a capturing group's
  iteration five. A quantifier over one character is a run since task 2.4c, which writes none, so
  `a{1048576}` and `[a]*` over 10,000,001 characters match; but under the engines' ceilings
  `(?:ab){1048576}` over as many repetitions and `(a)*` over 500,000 characters answer the trail
  ceiling, and `(?:){2147483647}` on an empty subject does too, where the specification answers an
  empty match (`CeilingTests` and `StackDepthGuardTests` pin each). The old matcher matched the first
  two and answered no match for the third.
- **A start inside a surrogate pair, under `u` or `v`.** The string-bearing members run the
  matcher from the pair, as RegExpBuiltinExec's step 13.b does, and report a non-empty match found
  there at the caller's start (row M4 of Broiler.VM's correspondence, decision D23). An empty match
  found there ends before that start, a case Broiler.VM's JSD-0081 classes implementation-defined by
  its decision D41: this library reports it at the pair's start, where it was found, as Broiler.VM's
  realm does (Broiler.JS reports the caller's start). The offset entry takes the start as given and
  splits the pair for the call; an engine steps back itself and gives its own D41 answer from the
  offsets written (`SurrogateStartTests`).
- **Compilation is charged through `Compile`, not through the constructors** (task 2.5, R2;
  decision D10). `BroilerRegex.Compile` charges the caller's meter and refuses past the caller's
  compile budget with `RegexLimit.Compile`, in units in proportion to the ranges and strings the
  pattern's expansions touch: `/abc/` costs 3 units, `/\p{L}/u` about 2,100, `/\p{L}/vi` about
  26,000, `/\p{RGI_Emoji}/v` about 338,000 (most of it the bound of ordering its 2,760 strings,
  1 + ⌊log₂ n⌋ units per code unit) and a class of fifty `\p{RGI_Emoji}` under `vi` about 4.9
  million. The constructors take neither meter nor budget, as an engine that passes no meter
  compiles, so their compilation is bounded only by the pattern; a caller that compiles untrusted
  patterns calls `Compile` with a budget. The public `RegexParser` and the public `CodePointSet`
  operations charge nothing either: their work is done in the caller's name (task 2.9 decides their
  surface). The units count work, not memory: a compilation's allocation follows the same
  expansions, about 1.3 MiB for `\p{RGI_Emoji}` at task 2.4c's measurement, of which the parse was
  about 0.95 MiB.
- Two deliberate divergences from V8, both under `vi` and both pinned by a test:
  - `\p{ASCII}` follows §22.2.2.9, which folds a lone binary property's set, so `ſ`
    (which folds to `s`) matches. V8 answers "no match" here while agreeing on
    `[\u0000-\u007F]` and on every other property tested —
    `UnicodeSetsTests.PropertyOfBinaryName_FoldsUnlikeV8`.
  - A one-character `\q{…}` alternative folds like any other class member, so
    `/[\q{A}]/vi` matches `A`. V8 folds the member but not the subject at that length —
    it matches `a` and not `A` — while canonicalizing both for a longer alternative —
    `UnicodeSetsTests.SingleCharacterStringAlternative_FoldsUnlikeV8`.
- Two defects found during SJL-11.2 task 2.2's review, older than the task and still open after task
  2.4c, left for the class-set work of a later SJL-11.2 task (each fails against ES2025's §22.2.1
  grammar). Both are the parser's reading of `\q{…}`, which the backend takes as the parser gives it:
  - `\b` inside `\q{…}` is refused: `/[\q{\b}]/v`, `/[^\q{\b}]/v` and `/[\q{a\bc}]/v` throw
    `RegexSyntaxException`, where `ClassSetCharacter` admits `\b` (U+0008).
  - An escaped lead surrogate followed by an escaped trail surrogate in `\q{…}` is read as
    one code point: `/^[\q{\uD83D\u{DE00}}]$/v` matches "😀", where the two escapes are
    two code points and the alternative is a two-code-point string.
- **Performance.** The backend interprets an instruction program, one dispatch at a time, so it
  stays slower than the compiled `System.Text.RegularExpressions`; no timing benchmark was taken for
  task 2.4c. Its fast paths cut the work it does: a run tests each character it takes with no frame
  or trail cell, an atom run reads its atoms with one dispatch, a class of strings looks up the
  strings that begin with the character at hand, and a search skips the positions its first test
  refuses. Over the test suite's population of patterns the work with them is 18.6 million units
  against 41.4 million without (`FastPathDifferentialTests`). A `v`-mode class under `i` folds an
  operand of more than 4,096 code points through `Cased` since task 2.5: `\p{L}`'s fold maps the
  4,469 code points it shares with `Cased` one at a time, where it mapped all 145,672.

## Integration status

`JSRegExp` currently routes a conservative set of patterns with known .NET
semantic gaps through Broiler.Regex — look-behind captures and back-references,
nullable quantifiers, code-point back-references, astral atoms, and now `v`-mode
class-set expressions and property escapes used as class members.
`RegExpBuiltinExec` consumes common match data from either backend; `Split`,
`Replace`, and `IJSRegExp.Value` still use the .NET backend.

The remaining correctness and adoption gates are tracked in the
[repository roadmap](../docs/roadmap.md).

See [`docs/ecmascript-mapping.md`](docs/ecmascript-mapping.md) for the
node-by-node mapping to ECMA-262 §22.2.2.

## License

Broiler.Regex is licensed under the [Apache License 2.0](../LICENSE). The license
provides the software on an “AS IS” basis without warranties or conditions.
