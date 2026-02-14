#!/usr/bin/env python3

import subprocess
import sys
import shutil
import os

def check_pyinstaller():

    try:
        import PyInstaller
        print("✓ PyInstaller is already installed")
        return True
    except ImportError:
        print("PyInstaller not found. Installing...")
        try:
            subprocess.check_call([sys.executable, "-m", "pip", "install", "pyinstaller"])
            print("✓ PyInstaller installed successfully")
            return True
        except subprocess.CalledProcessError as e:
            print(f"✗ Failed to install PyInstaller: {e}")
            return False

def build_exe():

    for folder in ["build", "dist"]:
        if os.path.exists(folder):
            print(f"Cleaning {folder}/...")
            shutil.rmtree(folder)

    if os.path.exists("KerbonautRedux_ModManager.spec"):
        os.remove("KerbonautRedux_ModManager.spec")

    cmd = [
        sys.executable, "-m", "PyInstaller",
        "--onefile",
        "--windowed",
        "--name", "KerbonautRedux_ModManager",
        "--add-data", "Textures;Textures",
        "--add-data", "Models;Models",
        "--add-data", "packed mods;packed mods",
        "--clean",
        "--noconfirm",
        "kr_gui.py"
    ]

    if sys.platform != "win32":
        for i, arg in enumerate(cmd):
            if ";" in arg:
                cmd[i] = arg.replace(";", ":")

    print("\nBuilding executable...")
    print(" ".join(cmd))
    print()

    try:
        subprocess.check_call(cmd)
        print("\n" + "="*60)
        print("✓ Build successful!")
        print("="*60)

        exe_name = "KerbonautRedux_ModManager.exe" if sys.platform == "win32" else "KerbonautRedux_ModManager"
        exe_path = os.path.join("dist", exe_name)

        if os.path.exists(exe_path):
            size_mb = os.path.getsize(exe_path) / (1024 * 1024)
            print(f"\nExecutable: {exe_path}")
            print(f"Size: {size_mb:.1f} MB")
            print(f"\nYou can now run: {exe_path}")
            print("\nNote: When distributing, include:")
            print("  - Textures/ folder")
            print("  - Models/ folder")
            print("  - packed mods/ folder")
            print("  - KerbonautRedux.json (will be created on first run)")

        return True

    except subprocess.CalledProcessError as e:
        print(f"\n✗ Build failed: {e}")
        return False

def main():
    print("="*60)
    print("KerbonautRedux Mod Manager - EXE Builder")
    print("="*60)
    print()

    if not os.path.exists("kr_gui.py"):
        print("✗ Error: kr_gui.py not found!")
        print("Please run this script from the KerbonautRedux directory.")
        sys.exit(1)

    if not check_pyinstaller():
        print("\nPlease install PyInstaller manually:")
        print("  pip install pyinstaller")
        sys.exit(1)

    print()

    if build_exe():
        print("\nDone!")
    else:
        print("\nBuild failed.")
        sys.exit(1)

if __name__ == "__main__":
    main()
