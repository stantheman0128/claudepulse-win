#!/bin/bash
# ClaudePulse deploy script: build → kill old → copy to Startup → restart

set -e

PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
PUBLISH_DIR="$PROJECT_DIR/publish-lite"
STARTUP_DIR="/c/Users/$USERNAME/AppData/Roaming/Microsoft/Windows/Start Menu/Programs/Startup"

echo "=== ClaudePulse Deploy ==="

# 1. Kill running instance
echo "[1/4] Stopping ClaudePulse..."
taskkill //IM ClaudePulse.exe //F 2>/dev/null || true

# 2. Build
echo "[2/4] Building..."
cd "$PROJECT_DIR/ClaudePulse"
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "$PUBLISH_DIR" -v quiet

# 3. Copy to Startup
echo "[3/4] Deploying to Startup folder..."
cp "$PUBLISH_DIR/ClaudePulse.exe" "$STARTUP_DIR/ClaudePulse.exe"

# 4. Restart
echo "[4/4] Starting ClaudePulse..."
"$STARTUP_DIR/ClaudePulse.exe" &

echo "=== Deploy complete ==="
