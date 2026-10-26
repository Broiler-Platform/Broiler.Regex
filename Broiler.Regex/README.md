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

Two backends live side by side until SJL-11.2 task 2.4c. The public members match with the
continuation-passing matcher of `Matching/`; task 2.4a's new backend, internal until then, compiles
the same tree to an instruction program that one loop runs. Task 2.4c routes `BroilerRegex` through
the new backend and deletes the old matcher.

```
                                   ┌─▶ Matcher (CPS backtracker) ──▶ RegexMatch       public, until 2.4c
pattern ──▶ RegexParser ──▶ AST ───┤   (Matching/)                   (Matching/)
            (Parsing/)     (Ast/)  └─▶ ProgramCompiler ──▶ RegexProgram + RegexMachine ──▶ caller's slots
                                       (Compiling/)        (Engine/)                        internal, 2.4a
```

* **`Parsing/RegexParser.cs`** — recursive-descent parser for the ECMAScript
  `Pattern` grammar (§22.2.1): disjunction, quantifiers, character classes,
  groups (capturing / non-capturing / named / inline-modifier), assertions
  (anchors, word boundaries, look-around), and escapes (including `\u{…}` and
  back-references). Produces a `RegexNode` tree, descending only under the
  caller's nesting limit (`Parsing/RegexNesting.cs`).
* **`Ast/RegexNode.cs`** — the node hierarchy + `CharSet` (character-class model).
* **`Matching/Matcher.cs`** — compiles the AST to the spec's `Matcher`
  abstraction (`MatchState × Continuation → MatchState?`) and runs it. This is
  where the §22.2.2 algorithm lives, and where the gap cases are handled:
  * `Direction` (+1 / −1) threaded through compilation so a **lookbehind** body
    matches backward while its captures stay in source order (fixes #3, #4).
  * `RepeatMatcher`'s empty-iteration guard (fixes #8).
  * Code-point-aware atom and back-reference matching under `u`/`v`
    (fixes #6, #7).
* **`Compiling/ProgramCompiler.cs`** (task 2.4a) — lowers the tree to a `RegexProgram` on work
  lists of the heap, with no recursion, checking the caller's nesting limit before it enters each
  group. A tree holding what the program cannot hold yet (`^ $ \b \B`, a back-reference, a
  look-around, a modifier group, the `i` flag, a `v`-mode class of strings) is answered "not yet
  supported", a transitional answer task 2.4b removes.
* **`Engine/`** (task 2.4a) — the new backend. `RegexOp` and `RegexInstruction` are its instruction
  set; `RegexProgram` is the immutable program, shared across threads, with the internal match entry
  (a subject span, a start, an anchored argument independent of `y`, offsets written into the
  caller's slots); `RegexMachine` is the one backtracking loop, with an explicit frame stack and an
  undo trail (defunctionalised continuations) rented from the shared array pool, and no CLR
  recursion, which an architecture test reads from the IL; `RegexMatchCeilings` holds the caller's
  step, frame and trail ceilings, each answered as `RegexStatus.LimitExceeded` naming it. Every
  dispatched instruction is charged to the caller's meter, `RegexWorkCharge`, in blocks of 512
  units paid before the work.
* **`BroilerRegex.cs`** — the public façade (`Match` / `IsMatch`), mirroring the
  shape `JSRegExp` needs (`Success`, `Index`, `Length`, indexed + named groups).
* **`Ast/CodePointSet.cs`** — sorted code-point ranges with union, intersection,
  difference and complement, immutable once built, so a set is shared between threads. Every class member kind reduces to one of these, so
  `v`-mode `&&` / `--` / nesting is plain set algebra rather than a case per operand.
* **`Unicode/RegexUnicode.cs`** — every Unicode answer a compilation reads, through the
  provider over the caller's `IUnicodeTables`: `\p{…}` resolution by the exact names
  ECMA-262 admits (General_Category, binary properties, `Script`, `Script_Extensions`,
  and the UTS #51 properties of strings), both branches of `Canonicalize` and the fold
  orbits the class-membership test walks, `\s`, the folded `\w` set and `AllCharacters`.
  The package declares no Unicode version of its own.
* **`Unicode/UnicodeCharSets.cs`** — the parts of the class escapes that are not Unicode
  data: `\d`, the basic `\w` set and the code space, and the public members of `0.1` that
  read no table (`IsDigit`, `PositiveEscape`, `IsComplementEscape`, `PropertyMatch`).

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
- The Annex B / Unicode-mode grammar split: `\u{…}`, a bare `]`/`{`/`}`, a class escape
  as a range endpoint, a quantified look-ahead, and `\k<n>` in a pattern that declares no
  group name are each a literal or a legal term in one mode and a syntax error in the
  other

## Known limitations (stubbed / TODO)

- Quantifier repetition is iterative for every body shape, so a repeat over a long subject
  never overflows: a single-code-point body — a literal, `.`, or a character class — takes a
  linear fast path, and any other body (a capturing group, an alternation of sequences) runs
  through an explicit-stack `RepeatMatcher` that keeps the iteration dimension off the native
  call stack. The remaining native recursion is bounded by the pattern's shape, its nesting
  depth and the length of its sequences, never by the subject length.
- Nesting is bounded by the caller's limit, not by the stack (requirement R4). The parser
  checks the limit before it enters each group and, under `v`, each class, and the closure
  compiler, which walks the tree on the heap, checks it again; a pattern nested deeper is
  refused before the level past the limit is read. `BroilerRegex.Compile` takes the caller's
  limit, up to `BroilerRegex.MaximumNestingLimit` (512, the limit both engines pass), and
  answers `RegexStatus.LimitExceeded` with `RegexLimit.Nesting`; the constructors and
  `RegexParser.Parse` parse under the maximum and refuse a deeper pattern with a
  `RegexSyntaxException`. On linux-x64, a pattern of every shape measured, nested to the
  maximum, parses and compiles on a 1 MiB stack under the JIT and Native AOT (the measurement
  and its margins are in the constant's documentation); win-x64 was not measured, so the
  maximum is proven on linux-x64 only.
- **Matching at the nesting maximum through the public members is proven only for some shapes**,
  until task 2.4c routes them through the new backend of SJL-11.2 task 2.4a (R4's matcher half),
  which matches each shape of the table below that it runs (those without a back-reference, an
  assertion or a look-behind) at the maximum on a 1 MiB thread. The parser still recurses a few
  frames per level, and the public matcher's continuations recurse per level and per term of a
  sequence, so a pattern within
  the maximum can exhaust a small stack when it is matched. `NestingMaximumTests.MatchedShapes`
  lists the shapes that match at the maximum on a 1 MiB thread: each kind of level alone, and an
  alternation, a sequence or an alternation of sequences on each level without a capture. A
  capture with more on each level is not proven, and the review of task 2.3 found these to fail,
  each an open item until task 2.4c and a skipped test in `NestingMaximumTests` (the levels at which
  a match on a 1 MiB thread first failed on linux-x64, in fresh processes; a depth is one more;
  "overflow" is a `RegexOverflowException`, otherwise the process ended with a stack overflow):

  | Shape, written for two levels | JIT Release | JIT Debug | Native AOT |
  |---|---|---|---|
  | `(b\|x\1\B(b\|x\2\Ba\2x)\1x)` | 506 | 328 | 2,506 |
  | `(b\|x\1\B(b\|x\2\Bax)+x)+` | 465, overflow | 321, overflow | 1,463, overflow |
  | `(b\|x\1(b\|x\2ax)+x)+` | 510, overflow | 357, overflow | 1,463, overflow |
  | `(b\|x\1\B(b\|x\2\Ba\Bx)\Bx)`, and inside a look-behind | 531 | 357 | 2,506 |
  | `(b\|x\B(b\|x\Ba\Bx)\Bx)` | 630 | 440 | 2,506 |
  | `(b\|x\1(b\|x\2ax)x)` | 676 | 446 | 2,506 |
  | `(b\|x(b\|xax)+x)+` | 625, overflow | 453, overflow | 1,463, overflow |
  | `(?:b\|x(?:b\|xax)+x)+` | 688, overflow | 501, overflow | 1,463, overflow |
  | `(b\|x?(b\|x?ax?)x?)` | 698 | 535 | 1,810 |
  | `(b\|x(b\|xax)x)`, named, and inside a look-behind | 845 | 584 | 2,506 |
  | the same with capturing, non-capturing, named and modifier groups in turn | 937 | 659 | 2,833 |
  | `(x(xax)x)` | 1,053 | 691 | 3,260 |

  At the maximum, 511 levels, the first two rows fail under the JIT in Release, each in all 20 of
  24 runs that had the default tiering, and the third in 4 of 24; the first eight rows fail under
  the JIT in Debug in every run. A long flat sequence of terms that are not single code points exhausts
  a small stack in the same way and ends the process, as it did in `0.1.0-preview.4`: 5,000
  capturing groups in a row do on a 1 MiB thread, where 2,000 match. The `RegexOverflowException`
  backstop, which the JavaScript layer catches to fall back to .NET, is checked only where a
  general quantifier's driver starts, so it does not reach a crash that happens elsewhere.
- **An alternation charges no step.** The step budget is charged by quantifier iterations and
  scans, not by trying an alternative, so outside a quantifier nothing bounds the paths a
  failing match backtracks through: n copies of `(?:a|a)` followed by `b`, run on n `a`
  characters, explore about 2^n paths, and the time doubles with each copy (on linux-x64 in
  Release, 24 copies take 1.3 s, 26 take 5 s and 28 take 19 s; 40 would take most of a day).
  The new backend's meter, which charges every instruction it dispatches, each tried alternative's
  included (SJL-11.2 task 2.4a, requirement R1), bounds it; the public members reach it at task 2.4c,
  and `Matcher`'s alternation records the defect as its falsifier until then.
- **A start inside a surrogate pair, under `u` or `v` (the old backend's defect D1).** Task 2.4a's
  differential test found that the public matcher's greedy run of one character gives back across the
  start it was called at, joining the pair's low half to the high half before the start:
  `Match("😀ba", 1)` of `/[\s\S]*\u{1F600}/u` answers a match at 1 of the character at 0, and
  a group can be captured before the match. The new backend reads the low half as a character of its
  own and never reads before the start (`MachineTests` pins it); the public members reach it at task
  2.4c. RegExpBuiltinExec's step back from a `lastIndex` inside a pair is the caller's, as Broiler.VM's
  realm takes it.
- **The new backend's subset and its missing fast paths (task 2.4a).** Until task 2.4b, the internal
  backend answers "not yet supported" for `^ $ \b \B`, back-references, look-around, modifier groups,
  the `i` flag and `v`-mode classes of strings. Until task 2.4c it has no fast path: a greedy
  quantifier over one class pushes a frame per character, so `[a]*` over the adversarial corpus's
  10,000,001 characters (row O05) reaches the corpus's frame ceiling rather than matching.
- The general driver enumerates each level's body matches by re-running
  the body per alternative, so a body with heavy internal backtracking is matched more than
  once per iteration; the shared step budget bounds that, and the single-code-point fast path
  avoids it for the common case.
- Two deliberate divergences from V8, both under `vi` and both pinned by a test:
  - `\p{ASCII}` follows §22.2.2.9, which folds a lone binary property's set, so `ſ`
    (which folds to `s`) matches. V8 answers "no match" here while agreeing on
    `[\u0000-\u007F]` and on every other property tested —
    `UnicodeSetsTests.PropertyOfBinaryName_FoldsUnlikeV8`.
  - A one-character `\q{…}` alternative folds like any other class member, so
    `/[\q{A}]/vi` matches `A`. V8 folds the member but not the subject at that length —
    it matches `a` and not `A` — while canonicalizing both for a longer alternative —
    `UnicodeSetsTests.SingleCharacterStringAlternative_FoldsUnlikeV8`.
- Two defects found during SJL-11.2 task 2.2's review, older than the task and left for the
  class-set work of a later SJL-11.2 task (each fails against ES2025's §22.2.1 grammar):
  - `\b` inside `\q{…}` is refused: `/[\q{\b}]/v`, `/[^\q{\b}]/v` and `/[\q{a\bc}]/v` throw
    `RegexSyntaxException`, where `ClassSetCharacter` admits `\b` (U+0008).
  - An escaped lead surrogate followed by an escaped trail surrogate in `\q{…}` is read as
    one code point: `/^[\q{\uD83D\u{DE00}}]$/v` matches "😀", where the two escapes are
    two code points and the alternative is a two-code-point string.
- Performance: the matcher is an interpreter, not a compiled/DFA engine, so it stays slower
  than `System.Text.RegularExpressions`. A first allocation pass has closed much of the gap —
  `MatchState` is a `readonly struct` (deriving a state per code point no longer allocates),
  the single-code-point quantifier fast path scans and backtracks with no per-iteration list,
  one budget and capture array are reused across a run's start positions, and a run of two or
  more single-code-point atoms in a forward sequence is folded into one loop instead of a
  continuation closure per term. On a hot-pattern microbenchmark that leaves simple patterns
  (`\d+`, `[a-z]+`, `\w+`, character-class scans) within ~2–3× of the compiled .NET engine.
  What remains is the per-invocation continuation-closure chain that a sequence with capturing
  groups or nested quantifiers still allocates, and the per-write capture-array clone — both
  need the compiled/bytecode path in §4 of the [roadmap](../docs/roadmap.md), which task 2.4a builds
  and task 2.4c makes the public members' path, so capture-heavy
  patterns are still several times slower. A `v`-mode class under `i` also folds its whole
  operand set at compile time — measurable for an operand the size of `\p{L}`.

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
