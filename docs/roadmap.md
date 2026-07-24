# Broiler.Regex roadmap

This file contains only unfinished work. The parser, continuation-based matcher,
gap-feature routing, common `Exec` match data, and their completed issue-specific
milestones are represented by code and tests.

## 1. Complete Unicode semantics

- Resolve `\p{...}` and `\P{...}` from the pinned Broiler.Unicode property data instead
  of throwing `NotSupportedException`.
- Implement `v`-mode class-set intersection, subtraction, nested operands, and
  `\q{...}` string alternatives.
- Implement the full ECMAScript `Canonicalize` case-fold mapping for Unicode and
  UnicodeSets modes.
- Cover lone surrogates, astral code points, complements, aliases, ignore-case
  expansion, and forward/backward matching.

Exit gate: the relevant pinned test262 RegExp and UnicodeSets paths pass without routing
to .NET for unsupported Broiler features.

## 2. Close current RegExp failures

Broiler.JS currently tracks failures involving Annex B leading/trailing escapes,
regular-expression literals, and Unicode ignore-case behavior. For every failure:

1. add the smallest Broiler.Regex or Broiler.JS integration regression;
2. identify whether parsing, matcher semantics, built-in metadata, or host integration
   owns it;
3. rerun the focused upstream path and the full affected shard.

The integration repository's `scripts/compliance/test262-failures.txt` is the exact path
source; do not duplicate its changing list here.

## 3. Replace the translator

Expand routing only as implemented syntax and semantics become test-clean:

- use the shared match-data path for `Exec`, `Split`, and `Replace`;
- preserve global/sticky `lastIndex`, captures, named groups, indices, replacement
  substitutions, and species behavior;
- compare Broiler and .NET results during each routing expansion;
- remove `IJSRegExp.Value` and the source-to-source translator only after no supported
  operation needs them.

Exit gate: all supported patterns use Broiler.Regex, the pinned RegExp corpus is green,
and no production path constructs a .NET `Regex` for ECMAScript matching.

## 4. Performance after correctness

The matcher currently clones capture state on backtracking and has no compiled/DFA
path. Profile representative successful, failing, catastrophic, lookaround,
backreference, and UnicodeSet patterns before changing the model. Any optimization must
retain explicit timeout/backtracking limits and identical capture/ordering semantics.

## 5. Preview gate

- Run unit, Broiler.JS integration, focused test262, fuzz, timeout, and allocation
  suites.
- Verify the nested Broiler.Unicode revision and generated-data provenance.
- Update the ECMAScript mapping when a previously stubbed production is implemented.
- Update `HUMAN_REVIEW.md` for the exact commit and scope.
