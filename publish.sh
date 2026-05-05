#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUBLISH_DIR="$ROOT_DIR/publish"
SERVICE_DIR="$PUBLISH_DIR/service"
DESKTOP_DIR="$PUBLISH_DIR/desktop"
SETUP_DIR="$PUBLISH_DIR/setup"
RESOURCES_DIR="$ROOT_DIR/Resources"
LOGO_SCRIPT="$ROOT_DIR/create-logo.py"

printf '\n========================================\n'
printf 'SAMER Hub - Release Build Baslatiliyor\n'
printf '========================================\n\n'

mkdir -p "$RESOURCES_DIR"
mkdir -p "$PUBLISH_DIR"

if [[ -f "$LOGO_SCRIPT" ]]; then
  printf '[0/3] Logo dosyalari guncelleniyor...\n'
  python3 "$LOGO_SCRIPT"
fi

printf 'Eski publish klasorleri temizleniyor...\n'
rm -rf "$SERVICE_DIR" "$DESKTOP_DIR" "$SETUP_DIR"
mkdir -p "$SERVICE_DIR" "$DESKTOP_DIR" "$SETUP_DIR"

printf '[1/3] Service projesi self-contained olarak yayimlaniyor...\n'
dotnet publish "$ROOT_DIR/src/SamerHub.Service/SamerHub.Service.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:UseAppHost=true \
  -o "$SERVICE_DIR"

printf '[2/3] Desktop projesi self-contained olarak yayimlaniyor...\n'
dotnet publish "$ROOT_DIR/src/SamerHub.Desktop.Avalonia/SamerHub.Desktop.Avalonia.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:UseAppHost=true \
  -o "$DESKTOP_DIR"

if [[ -f "$RESOURCES_DIR/logo.ico" ]]; then
  cp "$RESOURCES_DIR/logo.ico" "$DESKTOP_DIR/logo.ico"
fi

if [[ -f "$RESOURCES_DIR/logo.png" ]]; then
  cp "$RESOURCES_DIR/logo.png" "$DESKTOP_DIR/logo.png"
fi

printf '[3/3] Kurulum uygulamasi self-contained olarak yayimlaniyor...\n'
dotnet publish "$ROOT_DIR/installer/Setup/Setup.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:UseAppHost=true \
  -o "$SETUP_DIR"

[[ -f "$SERVICE_DIR/SamerHub.Service.exe" ]] || { echo "Service exe olusmadi"; exit 1; }
[[ -f "$DESKTOP_DIR/SamerHub.Desktop.Avalonia.exe" ]] || { echo "Desktop exe olusmadi"; exit 1; }
[[ -f "$SETUP_DIR/Setup.exe" ]] || { echo "Setup exe olusmadi"; exit 1; }

printf '\nYayin basariyla tamamlandi.\n'
printf 'Service EXE : %s\n' "$SERVICE_DIR/SamerHub.Service.exe"
printf 'Desktop EXE : %s\n' "$DESKTOP_DIR/SamerHub.Desktop.Avalonia.exe"
printf 'Setup EXE   : %s\n' "$SETUP_DIR/Setup.exe"
printf '\nWindows kurulumu icin su dosyayi calistir:\n'
printf '  %s\n' "$SETUP_DIR/Setup.exe"
