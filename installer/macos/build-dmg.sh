#!/usr/bin/env bash
# Assembles the Claude Agent Dashboard.app bundle from a `dotnet publish` output
# directory and wraps it in a .dmg. Run on macOS (uses hdiutil).
#
# Usage: build-dmg.sh <version> <rid> <publish-dir> <output-dir>
#   version:     e.g. 1.0.0
#   rid:         osx-x64 or osx-arm64 (only used for the output filename)
#   publish-dir: dotnet publish output for that RID
#   output-dir:  where the .dmg is written
set -euo pipefail

VERSION="$1"
RID="$2"
PUBLISH_DIR="$3"
OUTPUT_DIR="$4"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_NAME="Claude Agent Dashboard"
EXE_NAME="ClaudeAgentDashboard.Presentation"

STAGING_DIR="$OUTPUT_DIR/dmg-staging-$RID"
BUNDLE_DIR="$STAGING_DIR/$APP_NAME.app"

rm -rf "$STAGING_DIR"
mkdir -p "$BUNDLE_DIR/Contents/MacOS"
mkdir -p "$BUNDLE_DIR/Contents/Resources"

cp -R "$PUBLISH_DIR/." "$BUNDLE_DIR/Contents/MacOS/"
chmod +x "$BUNDLE_DIR/Contents/MacOS/$EXE_NAME"

sed "s/__VERSION__/$VERSION/g" "$SCRIPT_DIR/Info.plist.template" > "$BUNDLE_DIR/Contents/Info.plist"

# A symlink to /Applications alongside the .app is the standard macOS "drag to install" UX.
ln -s /Applications "$STAGING_DIR/Applications"

mkdir -p "$OUTPUT_DIR"
DMG_PATH="$OUTPUT_DIR/ClaudeAgentDashboard-$VERSION-$RID.dmg"
rm -f "$DMG_PATH"
hdiutil create -volname "$APP_NAME" -srcfolder "$STAGING_DIR" -ov -format UDZO "$DMG_PATH"

echo "Built $DMG_PATH"
