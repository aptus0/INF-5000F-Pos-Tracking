#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DIST_DIR="$ROOT_DIR/dist"
PACKAGE_DIR="$DIST_DIR/SAMERHub-Windows-Package"
ZIP_PATH="$DIST_DIR/SAMERHub-Windows-Package.zip"

mkdir -p "$DIST_DIR"
rm -rf "$PACKAGE_DIR" "$ZIP_PATH"
mkdir -p "$PACKAGE_DIR"

cp -R "$ROOT_DIR/publish/service" "$PACKAGE_DIR/service"
cp -R "$ROOT_DIR/publish/desktop" "$PACKAGE_DIR/desktop"
cp -R "$ROOT_DIR/publish/setup" "$PACKAGE_DIR/setup"

cd "$DIST_DIR"
/usr/bin/zip -qry "$ZIP_PATH" "SAMERHub-Windows-Package"

printf 'Windows paketi hazir:\n%s\n' "$ZIP_PATH"
