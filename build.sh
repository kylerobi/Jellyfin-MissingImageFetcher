#!/usr/bin/env bash
# Builds the plugin for each supported Jellyfin line and regenerates manifest.json
# (the Jellyfin plugin repository file).
#
#   ./build.sh
#
# Bump the versions below for a new release and update CHANGELOG.
#
# Plugin versions follow the Jellyfin line they target (10.11.0.x, 12.0.0.x). Jellyfin only
# offers a build whose targetAbi is <= the server version and picks the highest version, so a
# 12.x server gets the 12.0.0.x build and a 10.11 server never sees it. For a bug-fix release,
# bump the last number (e.g. 12.0.0.1).
set -euo pipefail
cd "$(dirname "$0")"

# label | jellyfin package to compile against | plugin version | targetAbi (minimum server version)
# The v12 build compiles against 12.0.0 so it loads on every 12.x server.
BUILDS=(
  "12|12.0.0|12.0.0.0|12.0.0.0"
  "10.11|10.11.0|10.11.0.0|10.11.0.0"
)
CHANGELOG="Initial release"
NAME="Missing Primary Images"
GUID="8f3c2a71-5d4e-4b9a-9c61-2e7f0a4d6b15"
# Where Jellyfin downloads the zips from. Override for testing.
BASE_URL="${BASE_URL:-https://raw.githubusercontent.com/kylerobi/jellyfin-plugin-missing-primary-images/main/dist}"

TS="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
mkdir -p dist
VERSIONS_JSON=""

for b in "${BUILDS[@]}"; do
  IFS='|' read -r LABEL JF VER ABI <<<"$b"
  ZIP="MissingPrimaryImages_${VER}_jellyfin-${LABEL}.zip"
  STAGE="$(mktemp -d)"
  rm -rf src/bin src/obj
  dotnet build src -c Release -p:JellyfinVersion="$JF" -p:PluginVersion="$VER" -o "$STAGE/out" >/dev/null
  mkdir "$STAGE/pkg"
  cp "$STAGE/out/Jellyfin.Plugin.MissingPrimaryImages.dll" "$STAGE/pkg/"
  cat > "$STAGE/pkg/meta.json" <<JSON
{
  "category": "Metadata",
  "changelog": "$CHANGELOG",
  "description": "Scans your libraries for items without a Primary image and automatically fetches one.",
  "guid": "$GUID",
  "name": "$NAME",
  "overview": "Automatically fetch missing Primary images",
  "owner": "jellyfin-plugin-missing-primary-images",
  "targetAbi": "$ABI",
  "timestamp": "$TS",
  "version": "$VER",
  "status": "Active",
  "autoUpdate": true,
  "assemblies": ["Jellyfin.Plugin.MissingPrimaryImages.dll"]
}
JSON
  # Files must sit at the root of the zip: Jellyfin extracts it straight into the plugin folder.
  rm -f "dist/$ZIP"
  (cd "$STAGE/pkg" && TZ=UTC touch -d "2000-01-01" ./* && zip -qX "$OLDPWD/dist/$ZIP" ./*)
  MD5="$(md5sum "dist/$ZIP" | cut -d' ' -f1)"
  rm -rf "$STAGE"
  VERSIONS_JSON+="${VERSIONS_JSON:+,}
      {
        \"version\": \"$VER\",
        \"changelog\": \"$CHANGELOG\",
        \"targetAbi\": \"$ABI\",
        \"sourceUrl\": \"$BASE_URL/$ZIP\",
        \"checksum\": \"$MD5\",
        \"timestamp\": \"$TS\"
      }"
  echo "Built dist/$ZIP  (Jellyfin >= $ABI, md5 $MD5)"
done
rm -rf src/bin src/obj

cat > manifest.json <<JSON
[
  {
    "guid": "$GUID",
    "name": "$NAME",
    "description": "Scans your libraries for items that have no Primary image (poster, cover, thumbnail) and asks your enabled image providers to fetch one. Never replaces existing images or metadata.",
    "overview": "Automatically fetch missing Primary images",
    "owner": "jellyfin-plugin-missing-primary-images",
    "category": "Metadata",
    "versions": [$VERSIONS_JSON
    ]
  }
]
JSON
python3 -c "import json;json.load(open('manifest.json'))" && echo "manifest.json written"
