# Publishing a preview to nuget.org

The repository restores dependencies and publishes packages using nuget.org only.
GitHub Actions still runs CI and publishing; GitHub Packages is no longer used.
The shipping package is `Broiler.Regex`, targeting .NET 10.

## Prerequisites

- Add a repository Actions secret named `NUGET_API_KEY` with a nuget.org API key
  authorized to create and push `Broiler.Regex` versions.
- Dependencies `Broiler.UniCode.Properties` and
  `Broiler.UniCode.Emoji.StringProperties` at `0.1.0-preview.2` must remain
  available on nuget.org. They are already published. NuGet package IDs are
  case-insensitive; the existing assembly namespaces have not changed.
- Review the current revision and its documented limitations before publishing.
  `HUMAN_REVIEW.md` records an older, revision-scoped human attestation; automated
  checks do not renew that attestation.

## Choose the version

`node eng/resolve-preview-version.mjs` evaluates the shipping projects and selects
the next preview for their configured `VersionPrefix`. The configured
`VersionSuffix` is a minimum, not a fixed published version.

The resolver combines all shipping packages' listed and unlisted nuget.org
versions with `eng/preview-version-history.json`. That file preserves versions
from the retired feed without contacting it or requiring credentials. It is
seeded with `Broiler.Regex 0.1.0-preview.1`, confirmed by successful publish run
[35323188753](https://github.com/Broiler-Platform/Broiler.RegEx/actions/runs/35323188753).
If versions were published outside that workflow, add those versions (or the
highest preview for each release line) to the history before the first release.
Do not remove historical entries when moving feeds.

For example, history containing `0.1.0-preview.3` and nuget.org containing
`0.1.0-preview.2` resolves to `0.1.0-preview.4`. Higher nuget.org previews also
advance the sequence. Other release lines do not affect it. Lookup failures stop
the release instead of treating the feed as empty.

The optional workflow `version-suffix` input must be `preview.N` and at least the
automatically selected next version. A pushed tag such as `v0.1.0-preview.4` must
meet the same rule; the workflow publishes exactly that tagged version or fails.
A dry run does not reserve a version, so a later run resolves it again.

## Validate and publish

1. Run the **Publish** workflow with `dry-run: true` (the default), leaving
   `version-suffix` empty for automatic numbering. This needs no publishing key.
2. Inspect the `nuget-packages` artifact. CI builds and tests on Windows and Linux,
   checks the `.nupkg` and `.snupkg`, and runs a separate consumer using the local
   package plus dependencies from nuget.org with an isolated package cache.
3. Run **Publish** with `dry-run: false`, or push the desired `vX.Y.Z-preview.N`
   tag. The workflow validates again and pushes the package and symbols to
   nuget.org. Publishing runs are serialized to avoid competing version choices.

For local validation, use the version printed by the resolver:

```powershell
node --test eng/resolve-preview-version.test.mjs
node eng/resolve-preview-version.mjs
dotnet build Broiler.Regex.slnx -c Release
dotnet test Broiler.Regex.slnx -c Release --no-build
./eng/pack.ps1 -Version 0.1.0-preview.2 -Output artifacts/preview-check
./eng/verify-feed.ps1 -Packages artifacts/preview-check
```

Use an empty output directory for each pack. Packages contain the README, icon,
license metadata, third-party notices, assembly, and XML API documentation;
symbols are provided in a matching `.snupkg` with SDK Source Link information.
The consumer smoke test exercises both Unicode character properties and emoji
string properties, checking that the renamed dependencies work after packaging.
