#!/bin/bash
set -e

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$PROJECT_DIR/build"
UNITY_PATH="${UNITY_PATH:-$HOME/.local/share/Unity/6000.3.6f1/Editor/Unity}"

echo "=== Building StandaloneFileBrowser plugin ==="
cd "$PROJECT_DIR/Plugins/StandaloneFileBrowser"
make -j"$(nproc)"
mkdir -p "$PROJECT_DIR/Assets/MATE ENGINE - Packages/StandaloneFileBrowser/Plugins/Linux/x86_64"
cp build/libStandaloneFileBrowser.so \
   "$PROJECT_DIR/Assets/MATE ENGINE - Packages/StandaloneFileBrowser/Plugins/Linux/x86_64/"

echo "=== Building Unity project ==="
mkdir -p "$BUILD_DIR"
"$UNITY_PATH" \
    -projectPath "$PROJECT_DIR" \
    -executeMethod CliBuilder.Build \
    -quit -batchmode -nographics \
    -logFile -

echo "=== Copying launch script ==="
cp "$PROJECT_DIR/launch.sh" "$BUILD_DIR/"
chmod +x "$BUILD_DIR/launch.sh" "$BUILD_DIR/MateEngineX.x86_64"

echo "=== Build complete ==="
echo "Run with: $BUILD_DIR/launch.sh"
