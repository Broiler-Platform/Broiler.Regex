# The reference arms of bundle sjl-11-001

The arms of [`docs/evidence/sjl-11-001/reference-arms.md`](../../docs/evidence/sjl-11-001/reference-arms.md)
(template T4), written at task 2.13 of SJL-11.2, 2026-11-01: Broiler.Regex (arm **S**) against test-only copies of
Broiler.VM's matcher (arm **V**) and of Broiler.JS's translator and routing (arm **J**), over the population
[`scripts/reference-arms/generate_population.py`](../../scripts/reference-arms/README.md) writes. Each arm is its own
executable, built in Release and run under the JIT only, never in the Native AOT probe; none is in
`Broiler.Regex.slnx`, none is referenced by anything, and the package carries none of them. The arms **classify
nothing**: `scripts/reference-arms/compare.py` compares their answers and `classify.py` classes the differences. The
copies retire at SJL-16, or stay as a frozen population by decision.

| Directory | What it is |
|---|---|
| `Common/Harness.cs` | The harness every arm links: the population's reader, the workers, the answers' notation. No engine code |
| `S/` | Arm S: Broiler.Regex at the commit under test, by project reference, through its public API |
| `V/` | Arm V: Broiler.VM's `JsRegExpMatcher` family at `aaa39fb`, the correspondence's revision |
| `J/` | Arm J: Broiler.JS's `CreateRegex`, translator, `GapScan`, `TryRouteToBroiler` and `RunMatch` at `1f91b8f1` |
| `FastPaths/` | Not an arm: the fast-path differential over P4 that tasks 2.4c and 2.11 owe to this task |

## Commands

```sh
dotnet build tests/ReferenceArms/S -c Release      # and V, J, FastPaths
ARM=tests/ReferenceArms/S/bin/Release/net10.0/Regex.ReferenceArms.S.dll
dotnet $ARM --population OUT/population --out OUT/runs [--classes P1,P4] [--threads 3]
dotnet tests/ReferenceArms/J/bin/Release/net10.0/Regex.ReferenceArms.J.dll --population OUT/population --out OUT/runs --culture tr-TR
dotnet tests/ReferenceArms/FastPaths/bin/Release/net10.0/Regex.ReferenceArms.FastPaths.dll OUT/population/P4.tsv.gz OUT/fast-paths
```

Each run writes `<class>.<arm>[.<culture>].tsv.gz`, one line per case in the population's order (`id`, answer,
charge), and `run.<arm>[.<culture>].json`: the culture, the runtime, whether code is JIT-compiled, each package
version and copy the arm calls, its policy, and the negative-control case when a control copy sets one.

**The answers.** `accept`; `refuse:syntax`, `refuse:nesting` (a pattern deeper than the arm's nesting limit),
`refuse:compile`; for an exec, `null`, `match:` then each group's `start,end` from group 0 (`-` for a group that did
not participate) joined by `;`, then, when the pattern names groups, `|` and each name in ordinal order with the number
of its participating group (`-` when none participated), a name's code units outside printable ASCII and `,=|\`
written `\uXXXX`; `limit:steps`, `limit:frames`, `limit:trail` (S), `limit:steps` and `limit:ceiling` (V, whose two
ceilings throw one message), `limit:timeout` (J's .NET engine); `limit:wall-clock` and `crash:exit-N` (below);
`throw:<type>` for an exception an arm does not answer. The charge column is the units the meter received (S, V) or
`-` (J, which has no meter); it is recorded and never compared.

**The exec.** The harness applies RegExpBuiltinExec's steps before the matcher, the same for every arm: lastIndex is
read under `g` or `y` and is 0 otherwise, a lastIndex past the subject is `null`, and `y` anchors. Each arm then calls
its matcher as its engine does, in the copied members below. Argument conversion, lastIndex's writes, the protocols
and error construction stay in the engines and are not compared.

**Workers.** Each class's cases are split into contiguous ranges, one worker process of the arm per range (`--threads`,
3 by default), each case on a thread of 16 MiB, the stack Broiler.JS's shell gives a script (`SH/Program.cs:128`). A
worker that ends while a case runs (Broiler.Regex 0.1.0-preview.4's recursive matcher overflows the stack, row B8) has
that case answered `crash:exit-N`; a case that runs longer than 10 seconds of wall-clock time (1 second once a case of
the same pattern, flags and policy has been stopped in that range) has its worker stopped and is answered
`limit:wall-clock`; a new worker takes the range from the next case. Arm J reaches both: its .NET engine's match timeout
(1 second, below) is not checked in every loop, and the package's matcher recurses. The wall clock is a limit of the
run, not of an arm: on a loaded machine it can stop a case S or V would end under its own bound, so a run to be judged
is taken on an idle machine. *(The unjudged run of 2026-11-01, `docs/roadmap.md`'s record of task 2.13, ran with 2
seconds and 0.5 seconds; the limits were raised after it.)*

## Arm S

`Broiler.Regex/` by project reference, with `Broiler.Unicode.Tables` (the repository's central version) as the
provider's test-only data. A pattern compiles by `BroilerRegex.Compile` with the nesting limit 512 (P3: the row's),
no compile budget and a meter that records every unit; the flags by `RegexFlagsText.Parse`. An exec runs through the
offset entry `BroilerRegex.Match(ReadOnlySpan<char>, int, bool, Span<int>, RegexWorkCharge?, RegexMatchCeilings, out
RegexLimit)` under the default policy's ceilings (10,000,000 units, the engines' frames and trail) or P3's ample one
(no step ceiling, the frame and trail maxima). Around the entry the arm does what `BroilerRegex.Match(string, int)` does
and an engine's adapter must (row M4, decisions D23 and D41 of Broiler.VM's JSD-0081): under `u` or `v` a start
between the halves of a pair runs the matcher from the pair, a non-empty match found there reports the caller's start
as its index, and the empty match is reported where it was found.

## Arm V: Broiler.VM at `aaa39fb`

Each file opens with a header naming its source path, its git blob, the members copied (with lines) and every change;
each change is marked `ARM CHANGE` where it is made.

| File | Source (`src/` of Broiler.VM) | Blob | Members | Changes |
|---|---|---|---|---|
| `V/JsRegExpMatcher.cs` | `Broiler.VM.Profile.JavaScript.Format/JsRegExpMatcher.cs` | `f1253412` | the whole file: `JsRegExpCharge`, the two exceptions, `JsRegExpCase`, `JsRegExpCharSet`, `JsRegExpMatch`, `JsRegExpMatcher` with its Parser, Emitter and Runner | none: every byte after the header's blank line is the blob's |
| `V/JsUnicodeData.cs` | `…Format/JsUnicodeData.cs` | `b4646317` | lines 1-21 and 77-116: `JsFormatUnicode` | the provider is `UnicodeTables.Instance` of `Broiler.Unicode.Tables`, where Broiler.VM reads its own embedded tables of the same UCD 17.0.0 (the rule's "re-pointed") |
| `V/JsRealm.RegExp.cs` | `Broiler.VM.Profile.JavaScript/JsRealm.RegExp.cs` | `6c6e487b` | `RegExpHasFlag`, `RegExpNormalizeFlags`, `RegExpCompile`, the matcher's call in `RegExpMatchOne` (the charge of 16, the step back out of a pair, the anchored call) and `RegExpRun` | the engine is a stand-in that records every charge and stops at an allowance (10,000,000 units under the default policy, none under P3's) as `JsEngine.Charge` stops at the fuel; a refusal is a recorded exception; lastIndex's reading is the harness's and its writes are left out |

Packages: `Broiler.Unicode.Algorithms` and `Broiler.Unicode.Tables` `0.1.0-preview.5`, the Algorithms version Broiler.VM's
`Directory.Packages.props` pins at `aaa39fb`. The matcher's own limits are kept: nesting 512, frames 2^20, trail
2^21; every charge is recorded.

## Arm J: Broiler.JS at `1f91b8f1`

| File | Source (`Broiler.JS/` of Broiler.JS) | Blob | Members | Changes |
|---|---|---|---|---|
| `J/JSRegExp.cs` | `Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.cs` | `4217ce75` | lines 1-17, 20, 141-145, 152-168, 649-4366 and 4369-4965: `BuildFlagsString`, `ParseFlags`, both `CreateRegex` and every static member of the translator, `CaptureGroupMap` | no `JSClassGenerator`, not a `JSObject`; the instance members left out; the .NET `Regex` built with a match timeout of 1 second (a timeout is `limit:timeout`) |
| `J/JSRegExp.Broiler.cs` | `…/RegExp/JSRegExp.Broiler.cs` | in its header | the whole file but `EnumerateMatches` and `IsMatch`: `TryRouteToBroiler`, `GapScan`, `BuildCaptureMapFromBroiler`, `RunMatch`, `FromBroiler`, `FromNet` | those two and their `using` left out |
| `J/JSRegExp.UnicodeSets.cs` | `…/RegExp/JSRegExp.UnicodeSets.cs` | in its header | the whole file | none |
| `J/JSRegExpPrototype.cs` | `…/RegExp/JSRegExpPrototype.cs` and `JSRegExp.cs` | `112362ce`, `4217ce75` | the matcher's call in `ExecMatch` (`RunMatch` from the start, the sticky emulation) and the constructor's assignment from `CreateRegex` | lastIndex's reading and writes, the legacy statics and the result object left out |
| `J/RegExpUnicodeValidator.cs`, `J/RegExpGroupNames.cs`, `J/JSUnicodePropertyNames.cs` | `Broiler.JavaScript.Runtime/…` | in each header | the whole files: the three prechecks | none |
| `J/JSUnicode.cs` | `Broiler.JavaScript.Runtime/Unicode/JSUnicode.cs` | in its header | the whole file | the five reads of `JSUnicodeTables.Instance`, Broiler.JS's embedded tables of the same UCD 17.0.0, read `UnicodeTables.Instance` of `Broiler.Unicode.Tables` |
| `J/StandIns.cs` | not a copy | - | `JSException`, `JSEngine.NewSyntaxError`, `RegexTieringDiagnostics`, `RegexTiering` (off) and the instance fields the copied members read | - |

Every copy is Broiler.JS code, and Broiler.JS is derived in part from Yantra JS (Apache-2.0): each header says so
(`Origin:`), and [`THIRD_PARTY_NOTICES.md`](../../THIRD_PARTY_NOTICES.md) keeps the attribution Broiler.JS's notice
gives that origin.
The two `.cs` files of `BuiltIns/RegExp/` that Broiler.JS keeps with CRLF line endings are LF here (rule SL10).
Packages: `Broiler.Regex` **`0.1.0-preview.4` from nuget.org**, the package whose identity S has, which is why J is
its own executable; `Broiler.Unicode.Algorithms` and `Broiler.Unicode.Tables` `0.1.0-preview.5` (Broiler.JS's pin at
`1f91b8f1`). The arm runs three times: under the invariant culture, `tr-TR` and `lt-LT`, which the .NET engine reads
under `i` (rows M2 and M2a); the project runs on ICU, not in invariant globalization mode.

## The negative controls

The rule asks for a copy of arm V with one result changed for one input of P4, and a copy of arm J with one result
changed for one input of P1, each run once, the comparison reporting each as a difference unclassified, then the
copies removed. A control copy is this directory's `V/` or `J/` copied to a scratch directory, its `Program.cs`'s
`NEGATIVE-CONTROL` line given `Harness.ControlCase = "<case id>";` and nothing else: the harness then changes that
case's answer itself (`Harness.Planted`: a match's first capture ends one unit later, a null becomes the empty match at
0, an acceptance a refusal, anything else a null) and records the case in the run file, which the classifier reads to
list the difference unclassified. The copies are never committed. *(Until the review of 2026-11-02 the harness
appended "`#negative-control` to that case's answer", a marker the classifier's canonical form stripped, so the
planted answer was not a changed result; the classifier no longer strips anything.)*

## The fast-path differential

`FastPaths/` compiles the library's own sources (named by item, not copied) into itself, as the internal compiler has
no public door, and answers every P4 exec with the program compiler's fast paths and without them (its internal
`fastPaths` option), as `Broiler.Regex.Tests/FastPathDifferentialTests.cs` does over the test suite's populations:
the same ceilings (100,000 units, the engines' frames and trail), a case either program stops at the step ceiling run
again under 10,000,000 units, and the status, the ceiling and every slot compared.
