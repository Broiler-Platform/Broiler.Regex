# The vectors at library level: the pinned copy

This directory holds bundle `sjl-11-001`'s part D population
([`docs/evidence/sjl-11-001/decision-rule.md`](../../docs/evidence/sjl-11-001/decision-rule.md), part D): "every vector of
conformance/vectors/regexp/*.vectors at the corpus revision bundle sjl-11-001 names, copied into tests/Vectors/regexp/
with a pin file naming Broiler.JS's commit and each file's SHA-256, checked before the run". Added on 2026-11-02 by task
2.12 of SJL-11.2.

| Path | What it is |
|---|---|
| `regexp/*.vectors` | The eight files of Broiler.JS's `conformance/vectors/regexp/`, copied byte for byte |
| `regexp.pin` | The pin: Broiler.JS's commit, the corpus revision, the directory copied, and each file's name, length, SHA-256 and git blob |

## Where they come from

- **Repository:** [Broiler.JS](https://github.com/Broiler-Platform/Broiler.JS), the home of the shared conformance corpus
  (`conformance/vectors/`, whose format is its `SCHEMA.md`, schema 1).
- **Commit:** `b2c2e2243a086285d806866a64b83297791a8ec5`, Broiler.JS's `main` after Broiler-Platform/Broiler.JS#1089
  (milestone SJL-11.3), whose corpus is **revision 32**: revision 31 added the regular-expression area (1,420 vectors in
  eight files, every expected value but 24 computed by `scripts/conformance/regexpref.py`, `ref:regexpref@1`, ECMA-262
  22.2 and B.1.2 transliterated over UCD 17.0.0), and revision 32 classified both engines' first runs of it. The files
  last changed at `de8d7b54b31a1572dd3eeef674c716df70dcf9c8`; the pin names the merge commit, the one Broiler.JS's
  bundle `sjl-11-001` reads part D at (its decision D-partd: "The corpus revision this part names is 32").
- **Never edited here.** A vector the library fails is a finding against the library, or, when the vector is wrong, a
  finding reported to the corpus's owner; the copy is not changed to make a vector pass (SCHEMA.md section 5: "A
  vector an engine fails is never deleted or reclassified to make it pass").

## The check

`Broiler.Regex.Tests/Vectors/VectorCorpus.cs` reads `regexp.pin` and, before it reads any vector, checks that every
file the pin names is in `regexp/` with the pin's length and SHA-256, and that `regexp/` holds no `.vectors` file the pin
does not name. A problem refuses the run: the tests fail (`VectorsRunTests`) and the probe's `--vectors` mode exits 2
with the problems on standard error and no report. `.gitattributes` keeps these files LF on every platform, so the
check reads the same bytes on Windows.

## How to refresh

1. In a scratch worktree of Broiler.JS at the new commit (never its working tree), copy
   `conformance/vectors/regexp/*.vectors` here whole, replacing every file (and removing one the new revision drops).
2. Rewrite `regexp.pin`: the commit, the revision (`conformance/vectors/REVISIONS.md`), and for each file
   `file: <name> <length> <sha256> <blob>`, the blob from `git rev-parse <commit>:conformance/vectors/regexp/<name>`.
3. Run `Broiler.Regex.Tests` and the probe's `--vectors` mode, and record the new reading as a dated record of
   `docs/roadmap.md` and in the bundle's README, with every vector whose answer changed. An op the runner neither maps
   nor lists as the object model's renders a fault, which is a difference: a new op is mapped or listed before the
   copy is taken.

## Licence

The vectors are Broiler.JS's, Copyright 2026 Broiler Platform contributors, under the Apache License 2.0, the licence
of this repository too ([`LICENSE`](../../LICENSE)). Their expected values for property escapes, case folding and
white space are computed over the Unicode Character Database 17.0.0, which remains subject to the
[Unicode Terms of Use](https://www.unicode.org/terms_of_use.html); their notes cite test262 files by path and carry no
test262 text. [`THIRD_PARTY_NOTICES.md`](../../THIRD_PARTY_NOTICES.md) says so too. The package carries none of it.
