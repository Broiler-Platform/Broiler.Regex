# Conventions of the shared JavaScript libraries, in Broiler.RegEx

[← Roadmap](roadmap.md) · [Rules register](../tests/Regex.Architecture.Tests/rules.register.json) ·
[Starting violation list](../tests/Regex.Architecture.Tests/starting-violations.tsv) ·
[Suppressions](suppressions.md)

**This document is normative.** It makes the contract conventions of the shared JavaScript libraries
programme binding on the projects of this repository. Those conventions are
[section 4 of `architecture.md`](https://github.com/Broiler-Platform/Broiler.VM/blob/main/docs/javascript-shared-libraries/architecture.md#4-contract-conventions)
in the Broiler.VM repository. Broiler.JS states them for its own shared libraries in
`Broiler.ECMAScript/docs/conventions.md`, and Broiler.Unicode in its `docs/conventions.md`. **This is the
programme's copy, with the same numbering**: convention *n* here is convention *n* there and in
Broiler.Unicode's copy. A change that breaks one of them is a defect, whoever makes it. Where this document
and `architecture.md` section 4 differ, that section wins and this document is corrected.

Filed 2026-10-25, at work package SJL-11.2 of milestone SJL-11, under the owner condition R10 that this
repository adopted at SJL-11.1 ([roadmap](roadmap.md) section 6, "Record of 2026-10-20"). Decisions cited
by number (D5, D6, D8, D9, D11, D12, D31) are those of Broiler.VM's JSD-0081, the area verdict, which that
record carries.

## Where Broiler.Regex stands

The library does not meet these conventions yet. Broiler.VM's JSD-0081 (section "X3") lists the ones it
breaches at `c1422f4d`, the code `0.1.0-preview.4` was packed from, and requirements R1 to R11 close each
of them during SJL-11.2. The architecture tests in [`tests/Regex.Architecture.Tests`](../tests/Regex.Architecture.Tests)
assert **the exact list of today's violations**,
[`starting-violations.tsv`](../tests/Regex.Architecture.Tests/starting-violations.tsv), each with the rule
that finds it and the places in the source where it is. A test fails when that list changes in either
direction: a new violation is refused, and a violation that a change removes must leave the list in the
same change. Where a violation can be at more than one place, the list is checked place by place: the rules
over member and type references (SL1's assembly half, SL3, SL4 and C5) read each place from the IL and the
PDB's sequence points, and SL5 reads each suppressed code's places from the project and build files. A new
place of a listed violation is refused, and so is a citation of a place that no longer holds it, or of a
violation the rule still finds but at no place. A place has two limits, which the tests' `AssemblyFacts`
states. It is a statement, the lines of one sequence point, so a second use of a listed member inside a
statement that already holds one (`char.IsDigit(a) || char.IsDigit(b)`) is not a new place. And it is where
an instruction names the member, its declaring type or a type operand, so a new use that names a type or an
assembly only in a signature, a field or local type or a generic argument (a parameter of type `MemberInfo`,
a new `List<EmojiSequenceProperties>`) is no place at all. Review sees those uses. Every other row's
violation names its one place: a static field, a public member or type, a file, a package or a setting. Each step of SJL-11.2 shrinks the list, and it is empty when SJL-11.2 ends (bundle `sjl-11-001`'s
rule, check E-3).

## Scope

- **The library**, `Broiler.Regex/` (the package `Broiler.Regex`), is held to every convention.
- **The Native AOT probe**, `probes/Regex.Probe`, is an executable. It writes its report to a file, so
  convention 8's I/O clause does not hold for it; every other clause of convention 8 does, and so do
  conventions 12 and 17.
- **The test projects**, `Broiler.Regex.Tests` and `tests/Regex.Architecture.Tests`, are held to
  convention 17, and to nothing that would stop xunit discovering them. They are not Native AOT code and
  declare neither flag of convention 12.
- **The scripts** under `eng/` and the documents are held to convention 17's line endings.

## The conventions

Each convention is either decided by a test or a review obligation. The tests are the rules `SL1` to
`SL10` of the programme's governance (section 3.2), and three checks of the contract conventions that
JSD-0081's section X3 names and that metadata can decide, `C1`, `C3` and `C5`. Each has a register row, a
test and a witness in [`rules.register.json`](../tests/Regex.Architecture.Tests/rules.register.json).

| # | Convention | Decided by |
|---|---|---|
| 1 | **Neutral types only.** Public signatures use base class library types (`string`, `ReadOnlySpan<char>`, `Span<int>`, the integer types, `bool`) and types the library defines (structs, record structs, enums, the status types and the `RegexWorkCharge` meter of decision D9). Decision D11 reads this convention as admitting the Unicode owner's provider contract, `IUnicodeTables`, in a public signature. No public signature names an engine type, a collection type of the base class library, `object`, `dynamic`, `Type`, or a delegate that can run guest code. | Check C1; SL1 keeps engine types out of reach; review |
| 2 | **No coercion, no property access, no guest callbacks.** Inputs arrive converted: a pattern, flags, a subject, an index. An engine's `RegExp` object, its `lastIndex`, its protocols and its error construction are the adapter's. | Review |
| 3 | **Failure is data.** An expected failure is a status (decision D9): a match, no match, a limit exceeded naming its dimension (steps, frames, trail, nesting or compile), or a syntax status with a code, a message identifier and a position. A library throws only when its caller breaks a documented precondition. A meter that throws propagates its exception. Message text is not contract. | Check C3; SL6's baseline shows every signature; review |
| 4 | **Work is charged by the caller's meter.** An operation whose work grows with its input takes a nullable `RegexWorkCharge` and charges before the work it pays for, in the blocks decision D8 documents. The meter may throw, and the library then leaves no shared state changed: the same instance answers a new call as a new instance would. The library never reads a clock and never polls cancellation. | Bundle `sjl-11-001`, part A's checks A-1 and A-2; SL3 and SL4 refuse the clock; review |
| 5 | **Limits are explicit.** Steps, frames, trail, nesting depth and the compile budget come from the caller's policy. Exceeding one is its own status, never a crash, a silent "no match" or a shortened match. **A limit is never the stack**: the matcher does not recurse on the CLR stack, and the parser and compiler recurse only under the caller's nesting limit, checked before each descent, below a documented maximum (decision D6). | Check C5; part A's checks A-3 and A-4; review |
| 6 | **Determinism.** A result depends on its inputs, the provider's tables and the library version only: no culture, time zone or host Unicode, no randomness, environment or process history, no hash-table iteration order. | SL4, SL9 |
| 7 | **No ambient or mutable static state.** No mutable static field, settable static property, `[ThreadStatic]`, `AsyncLocal` or `ThreadLocal`, and no static field holding an object whose state can change after construction. Static data is immutable: constant spans, constant strings, or lazily built immutable values whose construction is idempotent and thread-safe. | SL2 |
| 8 | **No dynamic code and no hidden machinery.** Nothing on Broiler.VM's B5, B5b and B5c lists; no generic instantiation over a run-time `Type`, no `Enum.GetValues(Type)`, no `dynamic`, no reflection-based serialization, no file or network I/O, no environment read. The provider and the data are handed in by the caller. | SL3 |
| 9 | **Platform independence.** No `System.Text.RegularExpressions`, no `string.Normalize` or `IsNormalized`, no `CultureInfo`, `TextInfo`, `CompareInfo`, `TimeZoneInfo` or `DateTime.Now`, no `CharUnicodeInfo`, `char.Is*`, `char.To*` or `Rune.Is*` beyond ASCII, no text formatted or parsed by the current culture. Every Unicode answer comes from the provider. The surrogate tests `char.IsHighSurrogate` and `char.IsLowSurrogate` are code-unit arithmetic, not Unicode data, and are admitted. | SL4 |
| 10 | **Thread safety.** Every public member is safe to call concurrently. A compiled pattern and every `CodePointSet` are immutable after construction (decision D31). | SL2, SL9; part A's check A-11. SL2 reads static state only, and SL9's threads run after its sequential runs through the same instances, so a `CodePointSet` that a compiled pattern holds and normalises in place on its first match is check A-11's alone (task 2.3 makes every set immutable after construction). |
| 11 | **Allocation discipline.** Results are offsets into the caller's slots (R8); strings are the caller's choice. No cache grows without bound. | Part A's check A-8; review |
| 12 | **Native AOT and trimming.** `IsAotCompatible` and `IsTrimmable` for the library, with the trim, AOT and single-file analyzers reporting nothing, as errors. No such warning is suppressed. The probe publishes with Broiler.VM's root settings and runs the whole vector corpus in CI, and CI compares its report with the JIT's byte for byte. Until SJL-11.2 gives it its populations it runs the smoke population `smoke-1` of `probes/Regex.Probe/ProbePopulation.cs`; it runs bundle `sjl-11-001`'s part D, the vectors at library level, from task 2.12 on, and part C, the adversarial corpus, from task 2.14 on (check E-2). | SL5; the `native-aot-probe` CI job |
| 13 | **Data versions are public.** The library declares no data version of its own: it reads the provider's (decision D11). It holds no generated data, because every table it reads is the Unicode owner's. | SL7 |
| 14 | **Names that do not collide with engine scans.** No public member named `Normalize`, `IsNormalized` or `NormalizationForm` (Broiler.VM's rule N23), and no `System.Text.RegularExpressions` type and no `new Regex(` (rule N18). | SL4; SL6's baseline lists every name; review |
| 15 | **Every public member cites what it implements**: the ECMA-262 clause, at the edition the library pins, in its XML documentation. A missing documentation comment is a build error once the library resets the packaging props' `NoWarn`, which SL5 asks of it. | The build (CS1591 as an error); review for the citation |
| 16 | **Versioning and compatibility.** Semantic versioning on this repository's preview stream; the API baseline, `Broiler.Regex/PublicAPI.txt`, compared in both directions; package validation against the last published version, `0.1.0-preview.4`, with every accepted break suppressed, listed in [`suppressions.md`](suppressions.md) and in the changelog under **Changed** with its R item; a changelog entry for every observable behaviour change and every data change. For a `0.x` preview, "inside a major version" is read as inside a `0.y` minor version (decision D12). | SL6; [`CHANGELOG.md`](../CHANGELOG.md) |
| 17 | **Frameworks and code style.** `net10.0`; nullable enabled; implicit usings disabled; warnings as errors; no preprocessor directives; LF line endings under `.gitattributes`. | The build (`Directory.Build.props`); SL5; SL10 |
| 18 | **Assurance.** Every relevant declaration carries the Broiler Code Assurance annotation, the generated artefacts at the repository root are current, and CI fails on drift (decision SJL-D-5 (a)). | SL8; the `assurance` CI job |
| 19 | **Licensing and provenance.** Apache-2.0. Ported code records its origin (`Origin=Ported`); the new backend ports nothing (decision D5). No share-alike or unclear-licence code enters. Data notices travel with data: [`THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md) is packed with the library. | Review |

## How a convention is changed

The conventions are the programme's, not this repository's. A change is made first in Broiler.VM's
`architecture.md` section 4, under the programme's governance, then in the copies, here in place, with a
dated revision quoting the text it replaces. A rule that decides a convention changes in the same commit
as the convention, and its witness is watched failing and passing again.

## Every governed library also carries

Section 10 of the programme's `governance.md` in the Broiler.VM repository asks each governed library for
the following:

- a README with its scope, pins, conventions and consumers (this repository's [README](../README.md) and
  the [engine README](../Broiler.Regex/README.md));
- a changelog with "Observable changes" and "Data" headings ([`CHANGELOG.md`](../CHANGELOG.md));
- XML documentation citing the clause of every public member (convention 15; not yet complete);
- a provenance file for every data set: none after R11, since the library then carries no data;
- an adapter guide for every operation an engine calls: none yet. The operations are SJL-11.2's, and the
  guide comes with the public surface they settle.
