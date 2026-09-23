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
> escapes (`\p{…}`), `v`-mode class-set expressions and the `Canonicalize` case-fold
> tables are implemented and resolve against the pinned
> [Broiler.UniCode](https://github.com/Broiler-Platform/Broiler.UniCode) data. As of issue #923 the engine is **wired
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

```
pattern string ──▶ RegexParser ──▶ RegexNode AST ──▶ Matcher (CPS backtracker) ──▶ RegexMatch
                   (Parsing/)       (Ast/)            (Matching/)                   (Matching/)
```

* **`Parsing/RegexParser.cs`** — recursive-descent parser for the ECMAScript
  `Pattern` grammar (§22.2.1): disjunction, quantifiers, character classes,
  groups (capturing / non-capturing / named / inline-modifier), assertions
  (anchors, word boundaries, look-around), and escapes (including `\u{…}` and
  back-references). Produces a `RegexNode` tree.
* **`Ast/RegexNode.cs`** — the node hierarchy + `CharSet` (character-class model).
* **`Matching/Matcher.cs`** — compiles the AST to the spec's `Matcher`
  abstraction (`MatchState × Continuation → MatchState?`) and runs it. This is
  where the §22.2.2 algorithm lives, and where the gap cases are handled:
  * `Direction` (+1 / −1) threaded through compilation so a **lookbehind** body
    matches backward while its captures stay in source order (fixes #3, #4).
  * `RepeatMatcher`'s empty-iteration guard (fixes #8).
  * Code-point-aware atom and back-reference matching under `u`/`v`
    (fixes #6, #7).
* **`BroilerRegex.cs`** — the public façade (`Match` / `IsMatch`), mirroring the
  shape `JSRegExp` needs (`Success`, `Index`, `Length`, indexed + named groups).
* **`Ast/CodePointSet.cs`** — sorted code-point ranges with union, intersection,
  difference and complement. Every class member kind reduces to one of these, so
  `v`-mode `&&` / `--` / nesting is plain set algebra rather than a case per operand.
* **`Unicode/UnicodeCharSets.cs`** — the `\d \D \w \W \s \S` sets, `AllCharacters`,
  and `\p{…}` resolution against the generated `Broiler.UniCode` property tables
  (General_Category, binary properties, `Script`, `Script_Extensions`, and the UTS #51
  properties of strings).
* **`Unicode/CaseFolding.cs`** + **`Unicode/Generated/CaseFoldingData.g.cs`** — both
  branches of `Canonicalize`, table-driven from the Unicode Character Database, plus the
  inverse fold orbits the class-membership test walks. Regenerate with
  [`Unicode/tools/generate-case-folding.py`](Unicode/tools/generate-case-folding.py).

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
  and the fold-then-complement ordering that makes `[^\p{Lu}]` mean something
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
  call stack. The remaining native recursion is bounded by the pattern's nesting depth — the
  same bound the recursive-descent parser already imposed to accept the pattern — never by
  the subject length. A `RegexOverflowException` backstop remains for a pathologically nested
  pattern (the JavaScript layer catches it and falls back to .NET), but the subject length no
  longer reaches it. The general driver enumerates each level's body matches by re-running
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
  need the compiled/bytecode path in §4 of the [roadmap](../docs/roadmap.md), so capture-heavy
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
