# The Native AOT probe

`probes/Regex.Probe` is the shared JavaScript libraries programme's convention 12 for Broiler.Regex
([`docs/conventions.md`](../../docs/conventions.md)): an executable published with the settings of Broiler.VM's
composition roots (Native AOT, full trimming, invariant globalization, system resource keys, every trim warning on
its own, warnings as errors) that runs fixed populations through the library, so that CI can show the library
compiles into a Native AOT image with no warning and answers there as it answers under the JIT, byte for byte. It
is not packed. Since task 2.14 of SJL-11.2 it also runs bundle `sjl-11-001`'s part C, the adversarial corpus
([the rule](../../docs/evidence/sjl-11-001/decision-rule.md)).

*Written 2026-10-27, at task 2.14 of SJL-11.2; before it the probe's modes were described in `Program.cs` alone.*

## Modes

| Command | What it does | Exit |
|---|---|---|
| `Broiler.Regex.Probe [--report path]` | On a thread of 1 MiB, the smallest stack the corpus declares: the smoke population `smoke-1` (`ProbePopulation.cs`), the new backend's cases `engine-1` (`EnginePopulation.cs`) and the text helpers' populations `text-1` (`TextPopulation.cs`); the report to standard output and, with `--report`, to the file | 0; 2 on arguments it does not read |
| `Broiler.Regex.Probe --adversarial corpus.tsv [--report path]` | Part C: every row of the adversarial corpus, each in a child process of the probe, against its declared status (below) | 0 when no row differs and no child ends abnormally; 1 otherwise; 2 on arguments or a corpus it cannot read |
| `Broiler.Regex.Probe --engine-row corpus.tsv id` | Hidden: one row of the corpus in this process, on a thread of the corpus's declared stack; writes `id status` on standard output and `units n` on standard error | 0 when the row ran, whatever it answered; 3 when the corpus cannot be read or has no such row; a crash ends the process |

Under the JIT the probe runs as `dotnet probes/Regex.Probe/bin/Release/net10.0/Broiler.Regex.Probe.dll`; under Native
AOT as the image `dotnet publish probes/Regex.Probe -c Release -r <rid>` writes to
`probes/Regex.Probe/bin/Release/net10.0/<rid>/publish/`.

## The adversarial corpus (`--adversarial`)

The rule's part C asks for `probes/Regex.Probe --adversarial` "built under the JIT and published with Native AOT
under Broiler.VM's root settings, on ubuntu-latest and windows-latest, each row in a child process on a thread of
the smallest stack the corpus's header declares", with "each child's exit and status line retained".

- **The corpus** is read by `AdversarialCorpus.cs`, the corpus's one reader, which the child mode and
  `Broiler.Regex.Tests` read too: the format of [`tests/Adversarial/README.md`](../../tests/Adversarial/README.md)
  section 1, its `corpus-format` (1) and `stack-bytes` headers, the column line and the rows. A corpus in another
  format is refused, and the mode exits 2.
- **The children.** The probe starts itself once per row, one at a time, in ordinal order of the ids, with
  `--engine-row`: as `dotnet` and the probe's assembly when it runs under `dotnet`, and otherwise as its own
  executable, the Native AOT image or the apphost. The child builds the row's pattern and subject from their
  expressions (`EngineRun.Expression`) and runs its operation under its policy through the new backend's entry
  (`EngineRun.Answer`), all on a thread of the header's `stack-bytes`, 1,048,576 bytes. A child that exits other
  than 0, writes anything but one line `id status`, or runs past 120 seconds (it is then stopped) has **ended
  abnormally**. Otherwise its status, with the corpus's `x^k` written out, either **gives the declared status** or
  **differs**.
- **Rows that wait.** A row whose operation is `validate` waits for task 2.8, the validate entry, and is not run,
  while `EngineRun.Answer` answers that operation `unsupported`. It is named on the report's `waiting` line and
  counted. When task 2.8 gives the runner the operation, those rows run with no change to the mode.
- **The report** is deterministic: a header with the corpus's SHA-256, then one line per row with the child's exit
  code, its status line, the units it wrote, and the verdict; then the counts, the rows waiting and the rows
  reported (those that differ or end abnormally). It holds no time, path or machine and no text of a child's
  standard error, which differs between the JIT and Native AOT when a child crashes. So the JIT's report and the
  Native AOT image's compare byte for byte on one platform, which is part C's comparison.

CI's `native-aot-probe` job runs the mode under Native AOT and under the JIT on ubuntu-latest (linux-x64) and
windows-latest (win-x64) and compares the two reports with `cmp`; a row that differs or a child that ends abnormally
fails the step. On 2026-10-27 at linux-x64 locally, the mode took about 2 seconds under Native AOT and 6 under the
JIT, and the two reports were the same file.

## Part C's controls

The rule names two controls, each "a scratch build", each "reverted and the revert checked":

| Build | The line changed | What the report must show |
|---|---|---|
| `dotnet build probes/Regex.Probe -c Release -p:RegexAdversarialControl=steps` | `Engine/RegexProgram.cs`, in `RegexProgram.Match`: after the machine runs, a `LimitExceeded` naming steps is answered `NoMatch` | (1) every K, A, L, W and G row reported, and no other |
| `dotnet build probes/Regex.Probe -c Release -p:RegexAdversarialControl=nesting` | `Parsing/RegexNesting.cs`, in `RegexNesting.Descend`: the check of the caller's limit never refuses | (2) every N row past the limit reported, ending abnormally or differing, and no other |

[`AdversarialControls.targets`](AdversarialControls.targets) makes the build: with the switch set the probe does not
reference the library project, but compiles the library's sources into itself, with the declared file replaced by a
copy under `obj/` in which the declared line is changed; the build fails if the line is not in the file exactly
once. The changed line holds a string of its own (`adversarial control 1 (part C)`, `... 2 ...`), so a search finds
the control where it is. The library's source is never changed, and the library project is not built in either
mode, so `Broiler.Regex.dll` and the package cannot carry a control: the switch is MSBuild's alone, with no
preprocessor directive (convention 17), as the fuzz target's planted defect is. The revert is the next build without
the switch.

[`eng/adversarial-controls.py`](../../eng/adversarial-controls.py) holds a report to its control, reading the corpus
itself: the report's SHA-256 is the corpus's, every row is there once in id order, the rows reported are exactly the
control's (`--control steps` or `nesting`) or none (`--control none`, the revert), every other row gives its declared
status or, for a `validate` row, waits, and the probe's exit code agrees. Its `--holds` and `--lacks` search files
for a text in UTF-16 and UTF-8. CI's `adversarial-controls` job builds each control, runs it and checks its report,
shows the control's text in the control's probe assembly, shows that no control built `Broiler.Regex/bin`, then
builds the probe as usual, checks that report as the revert, and shows that the library's DLL lacks both the
controls' text and the switch's name. About 40 seconds of builds and runs locally on linux-x64.

What the controls reported on 2026-10-27, the same under the JIT and under Native AOT on linux-x64:

- Control (1): the 17 rows of the classes K, A, L, W and G answered `nomatch` where they declare `limit:steps`, and
  were reported as differing; the other 41 rows run gave their declared statuses.
- Control (2): N02, N08 and N14, one level past the limit, compiled (`ok`) and differed; N04, N05, N10, N11, N16 and
  N17, 20,000 and 1,000,000 levels deep, overflowed the 1 MiB stack in the parser and ended abnormally (exit 134
  under the JIT, 139 under Native AOT); N01, N07 and N13, at the limit, gave their declared statuses. The six N rows
  past the limit whose operation is `validate` wait for task 2.8 and are not reached.

## Locally

```sh
dotnet build probes/Regex.Probe -c Release
dotnet probes/Regex.Probe/bin/Release/net10.0/Broiler.Regex.Probe.dll --adversarial tests/Adversarial/corpus.tsv --report adversarial-jit.txt
dotnet publish probes/Regex.Probe -c Release -r linux-x64
probes/Regex.Probe/bin/Release/net10.0/linux-x64/publish/Broiler.Regex.Probe --adversarial tests/Adversarial/corpus.tsv --report adversarial-aot.txt
cmp adversarial-jit.txt adversarial-aot.txt

dotnet build probes/Regex.Probe -c Release -p:RegexAdversarialControl=steps
dotnet probes/Regex.Probe/bin/Release/net10.0/Broiler.Regex.Probe.dll --adversarial tests/Adversarial/corpus.tsv --report control-steps.txt
python3 eng/adversarial-controls.py --control steps --corpus tests/Adversarial/corpus.tsv --report control-steps.txt --exit $?
dotnet build probes/Regex.Probe -c Release   # the revert, before any other use of the probe's build
```

A control build leaves the probe's `bin/` and `obj/` holding the control; build the probe without the switch before
the tests, which run its child mode, or the architecture tests, which read its assembly.
