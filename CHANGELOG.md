# Changelog

Every release of the `Broiler.Regex` package has an entry here, newest first. The file was opened on 2026-10-25, at
work package SJL-11.2 of the shared JavaScript libraries programme, whose
[convention 16](docs/conventions.md) asks each governed library for a changelog with "Observable changes" and "Data"
headings. Releases before it, `0.1.0-preview.1` to `0.1.0-preview.4`, are recorded only by their packages on
nuget.org and by [`eng/preview-version-history.json`](eng/preview-version-history.json).

The package follows semantic versioning on its preview stream ([publishing guide](docs/publishing.md)):

- a public API removal or a binary-breaking change is a major version; for a `0.x` preview, a `0.y` minor version
  (decision D12 of Broiler.VM's JSD-0081);
- a new API is a minor version;
- a correction toward the specification is a minor version, with its own entry under **Observable changes**;
- a change of the Unicode data the package reads is a minor version, with its own entry under **Data**.

Each entry has the headings **Added**, **Changed**, **Observable changes** and **Data**, and says "none" under a
heading with nothing to report. A break of the published API that package validation accepts is listed under
**Changed** with the requirement (R1 to R11 of [`docs/roadmap.md`](docs/roadmap.md) section 6) that forces it, and
in [`docs/suppressions.md`](docs/suppressions.md).

## Unreleased

*SJL-11.2's changes accumulate here. Decision D12 of Broiler.VM's JSD-0081 ships the SJL-11 surface as
`0.2.0-preview.1`, published to nuget.org only with the owner's approval (decision D13).*

### Added

- **The programme's process (R10).**
  - [`docs/conventions.md`](docs/conventions.md), the programme's contract conventions, numbered as Broiler.VM's
    `architecture.md` section 4 and Broiler.Unicode's copy number them.
  - `tests/Regex.Architecture.Tests`: the architecture rules SL1 to SL10 and the contract checks C1, C3 and C5, each
    with a register row and a witness, asserting exactly the starting violation list,
    `tests/Regex.Architecture.Tests/starting-violations.tsv`, which SJL-11.2 shrinks to none.
  - `probes/Regex.Probe`, the Native AOT probe, published with Broiler.VM's root settings; CI compares its report
    under Native AOT with its report under the JIT, on ubuntu-latest and windows-latest.
  - `Broiler.Regex/PublicAPI.txt`, the public API baseline rule SL6 compares in both directions, and package
    validation against the published `0.1.0-preview.4`.
  - CI jobs for the architecture tests, the probe and the assurance check (Broiler.Code's
    `assurance check --root Broiler.RegEx`).
  - `.gitattributes`, which stores every text file with LF line endings.

### Changed

- **The assembly declares itself trimmable and AOT-compatible.** `IsAotCompatible` and `IsTrimmable` are set, with the
  trim, AOT and single-file analyzers reporting as errors; they report nothing. The assembly now carries the
  `IsTrimmable` and `IsAotCompatible` assembly metadata, so a consumer's trimmer treats it as trimmable. No method's
  IL changes.
- No public API changes; package validation against `0.1.0-preview.4` passes with no suppression.

### Observable changes

None. No matching, parsing or flag answer of the package changes.

### Data

None. The package reads the same Unicode data, from the same packages, as `0.1.0-preview.4`.
