#!/usr/bin/env bash
# Run after dotnet build Broiler.Regex.slnx using the same configuration.
set -euo pipefail

configuration="${1:-${CONFIGURATION:-Release}}"
case "$configuration" in
  Debug|Release) ;;
  *) echo "Unknown configuration '$configuration'; use Debug or Release." >&2; exit 2 ;;
esac

dotnet test Broiler.Regex.slnx -c "$configuration" --no-build