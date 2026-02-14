#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
KSP_PATH="/home/navajo/m2drive/SteamLibrary/steamapps/common/Kerbal Space Program"
GAMEDATA_PATH="$KSP_PATH/GameData"
MOD_NAME="KerbonautRedux"

echo "==================================="
echo "Installing KerbonautRedux to KSP"
echo "==================================="

if [ ! -d "$SCRIPT_DIR/dist/$MOD_NAME" ]; then
    echo "Error: Distribution folder not found!"
    echo "Please build first with: ./build.sh"
    exit 1
fi

if [ ! -d "$GAMEDATA_PATH" ]; then
    echo "Error: KSP GameData not found at: $GAMEDATA_PATH"
    exit 1
fi

USER_CONFIG=""
if [ -f "$GAMEDATA_PATH/$MOD_NAME/KerbonautRedux.json" ]; then
    echo "Preserving user config..."
    USER_CONFIG=$(cat "$GAMEDATA_PATH/$MOD_NAME/KerbonautRedux.json")
fi

if [ -d "$GAMEDATA_PATH/$MOD_NAME" ]; then
    echo "Removing old installation..."
    rm -rf "$GAMEDATA_PATH/$MOD_NAME"
fi

echo "Copying mod files..."
cp -r "$SCRIPT_DIR/dist/$MOD_NAME" "$GAMEDATA_PATH/"

if [ ! -z "$USER_CONFIG" ]; then
    echo "Restoring user KerbonautRedux.json..."
    echo "$USER_CONFIG" > "$GAMEDATA_PATH/$MOD_NAME/KerbonautRedux.json"
fi

echo ""
echo "Installed files:"
find "$GAMEDATA_PATH/$MOD_NAME" -type f | while read f; do
    size=$(du -h "$f" | cut -f1)
    echo "  $size  ${f#$GAMEDATA_PATH/}"
done

echo ""
echo "==================================="
echo "Installation complete!"
echo "==================================="
echo ""
echo "NEXT STEPS:"
echo "1. Launch KSP"
echo "2. Open the debug console (Alt+F12)"
echo "3. Look for messages starting with [KerbonautRedux]"
echo "4. Spawn or view Valentina"
echo ""
echo "If you don't see hair, check the console for error messages!"
