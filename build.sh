#!/usr/bin/env bash
# Builds install-ready plugin folders + zips for both Jellyfin lines.
#   ./build.sh            -> dist/ for 10.11.x and 12.x
#   ./build.sh 10.11.11   -> just that version
set -euo pipefail
cd "$(dirname "$0")"
VERSION=1.0.0.0
TARGETS=("${@:-10.11.11 12.4.0}")
[ $# -eq 0 ] && TARGETS=(10.11.11 12.4.0)
GUID=8f3c2a71-5d4e-4b9a-9c61-2e7f0a4d6b15
rm -rf dist && mkdir -p dist

for JF in "${TARGETS[@]}"; do
  MAJOR_MINOR=$(echo "$JF" | cut -d. -f1-2)
  OUT="dist/jellyfin-$MAJOR_MINOR/MissingPrimaryImages_$VERSION"
  rm -rf src/bin src/obj
  dotnet build src -c Release -p:JellyfinVersion="$JF" -o "build-tmp" >/dev/null
  mkdir -p "$OUT"
  cp build-tmp/Jellyfin.Plugin.MissingPrimaryImages.dll "$OUT/"
  cat > "$OUT/meta.json" <<JSON
{
  "category": "Metadata",
  "changelog": "Initial release",
  "description": "Scans your libraries for items without a Primary image and automatically fetches one.",
  "guid": "$GUID",
  "name": "Missing Primary Images",
  "overview": "Automatically fetch missing Primary images",
  "owner": "local",
  "targetAbi": "$MAJOR_MINOR.0.0",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "version": "$VERSION",
  "status": "Active",
  "autoUpdate": false,
  "assemblies": ["Jellyfin.Plugin.MissingPrimaryImages.dll"]
}
JSON
  (cd "dist/jellyfin-$MAJOR_MINOR" && zip -qr "../MissingPrimaryImages_${VERSION}_jellyfin-$MAJOR_MINOR.zip" "MissingPrimaryImages_$VERSION")
  rm -rf build-tmp
  echo "Built $OUT"
done
rm -rf src/bin src/obj
