#!/usr/bin/env bash
set -euo pipefail

PROJ="MPowerKit.VirtualizeListView/MPowerKit.VirtualizeListView.csproj"
OUT="nupkg"
read -rp "Feed name: " FEED
read -rp "NuGet.Config path: " NUGET_CONFIG
NUGET_CONFIG="${NUGET_CONFIG}"

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
