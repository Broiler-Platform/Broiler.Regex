# Warning suppressions, and accepted breaks of the published API

[← Conventions](conventions.md)

Filed 2026-10-25, at work package SJL-11.2 of the shared JavaScript libraries programme. Two inventories, which
the programme's conventions 12 and 16 ask for.

## Warning suppressions

Convention 12 suppresses no trim, AOT or single-file warning, and convention 17 makes every warning an error. This
inventory, in the shape of Broiler.VM's rule N36, lists by file every warning suppression in a source or project
file of the library (`Broiler.Regex/`) and the Native AOT probe (`probes/Regex.Probe`), and in every build file of
the repository that MSBuild's evaluation of either imports (today `Directory.Build.props`, `Directory.Packages.props`
and `eng/Broiler.Packaging.props`): a `#pragma warning disable`, a `SuppressMessage` or
`UnconditionalSuppressMessage` attribute, a non-empty `NoWarn`, a `WarningsNotAsErrors` or a
`SuppressTrimAnalysisWarnings` element. Rule SL5 of
[`tests/Regex.Architecture.Tests`](../tests/Regex.Architecture.Tests) scans those files and holds the section below
to what it finds: each suppression is listed there, and when the scan finds none the section says None. The build
files are read from the evaluation's import list, not named in the test, so one a later change adds (a
`Directory.Build.targets`, another `eng/*.props`) is scanned the day it is imported.

The probe's project file sets `NoWarn` to `1701;1702`, the compiler's informational codes the SDK puts in every
project. That **replaces** the XML-documentation codes the packaging file appends; it resets the property rather than
suppressing anything, and is not listed. The test projects, whose sources are the rules' own and name every marker
the scan looks for, and the witnesses of the rules, which exist to break them, are not scanned.

### Inventory

- `eng/Broiler.Packaging.props`: `NoWarn` appends `CS1570`, `CS1572`, `CS1573`, `CS1574`, `CS1587`, `CS1591` and
  `CS1734`, the XML-documentation warnings, for every project of the repository. The file is the vendored copy of the
  platform's packaging props and is not edited here. It reaches the library, whose project file does not reset it,
  so a public member without its documentation compiles there today. Rule SL5 refuses the library's evaluated
  `NoWarn` for each of the seven codes, and the starting violation list holds the seven rows until the library resets
  the property and documents every public member (convention 15).

None of these is a trim, AOT or single-file warning.

## Accepted breaks of the published API

Package validation compares every pack of `Broiler.Regex` with the last published package, `0.1.0-preview.4`
(convention 16). A break that the SJL-11 work makes on purpose (decision D12 of Broiler.VM's JSD-0081) is suppressed
in `Broiler.Regex/CompatibilitySuppressions.xml`, listed here with its R item, and recorded in
[`CHANGELOG.md`](../CHANGELOG.md) under **Changed**.

None yet.
