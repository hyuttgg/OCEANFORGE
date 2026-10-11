#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:?Usage: package-deb.sh <version> <publish-dir> <output-dir> [commit-sha]}"
PUBLISH_DIR="${2:?Missing publish-dir}"
OUTPUT_DIR="${3:?Missing output-dir}"
COMMIT_SHA="${4:-local}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STAGE_DIR="$(mktemp -d)"
trap 'rm -rf "$STAGE_DIR"' EXIT

mkdir -p "$OUTPUT_DIR"
find "$PUBLISH_DIR" -name '*.pdb' -delete

# Write build-info.json
cat > "$PUBLISH_DIR/build-info.json" <<EOF
{
  "product": "OceanForge Desktop",
  "semVer": "$VERSION",
  "gitCommitSha": "$COMMIT_SHA",
  "targetRid": "linux-x64",
  "publishMode": "self-contained",
  "buildTimestampUtc": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
}
EOF

# 1. Portable tar.gz
TAR_PATH="$OUTPUT_DIR/OceanForge-${VERSION}-linux-x64-portable.tar.gz"
tar -czf "$TAR_PATH" -C "$PUBLISH_DIR" .

# 2. Debian .deb package
cp -r "$SCRIPT_DIR/deb-root/"* "$STAGE_DIR/"
sed -i "s/__VERSION__/${VERSION}/g" "$STAGE_DIR/DEBIAN/control"
mkdir -p "$STAGE_DIR/usr/lib/oceanforge"
cp -r "$PUBLISH_DIR/"* "$STAGE_DIR/usr/lib/oceanforge/"
chmod 0755 "$STAGE_DIR/DEBIAN/control" "$STAGE_DIR/usr/bin/oceanforge" "$STAGE_DIR/usr/lib/oceanforge/OceanForge.Desktop"

DEB_PATH="$OUTPUT_DIR/OceanForge-${VERSION}-linux-x64.deb"
dpkg-deb --build --root-owner-group "$STAGE_DIR" "$DEB_PATH"
dpkg-deb --info "$DEB_PATH"
dpkg-deb --contents "$DEB_PATH" | head -n 25
