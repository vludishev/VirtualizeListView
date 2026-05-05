#!/usr/bin/env bash
set -euo pipefail

PROJ="MPowerKit.VirtualizeListView/MPowerKit.VirtualizeListView.csproj"
OUT="nupkg"
read -rp "Feed name: " FEED
read -rp "NuGet.Config path: " NUGET_CONFIG

if [[ -z "$FEED" ]]; then
  echo "Error: Feed name must not be empty." >&2
  exit 1
fi

if [[ -z "$NUGET_CONFIG" ]]; then
  echo "Error: NuGet.Config path must not be empty." >&2
  exit 1
fi

if [[ ! -f "$NUGET_CONFIG" ]]; then
  echo "Error: NuGet.Config file does not exist: $NUGET_CONFIG" >&2
  exit 1
fi

echo "==> Building Release..."
dotnet build "$PROJ" -c Release /p:WarningLevel=0

echo "==> Packing..."
dotnet pack "$PROJ" -c Release --no-build /p:WarningLevel=0 -o "$OUT"

PACKAGE=$(ls "$OUT"/*.nupkg | sort -V | tail -1)
echo "==> Pushing $PACKAGE..."
dotnet nuget push "$PACKAGE" \
  --source "$FEED" \
  --api-key az \
  --configfile "$NUGET_CONFIG"

echo "==> Done: $PACKAGE pushed to $FEED"
