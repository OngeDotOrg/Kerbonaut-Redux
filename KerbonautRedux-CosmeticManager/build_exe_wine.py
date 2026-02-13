#!/usr/bin/env python3


import subprocess
import sys
import shutil
import os


def check_wine():
    
    if shutil.which("wine"):
        print("✓ Wine is installed")
        return True
    print("✗ Wine is not installed")
    print("\nTo install Wine:")
    print("  Ubuntu/Debian: sudo apt install wine")
    print("  Fedora:        sudo dnf install wine")
    print("  Arch:          sudo pacman -S wine")
    return False


def setup_wine_prefix():
    
    wine_prefix = os.path.expanduser("~/.wine_kr")
    if not os.path.exists(wine_prefix):
        print("\nInitializing Wine prefix (this may take a moment)...")
        try:
            subprocess.run(["wine", "wineboot", "--init"], 
                         check=True, capture_output=True)
            print("✓ Wine prefix initialized")
        except subprocess.CalledProcessError as e:
            print(f"✗ Failed to initialize Wine: {e}")
            return False
    return True


def install_python_in_wine():
    
    try:
        result = subprocess.run(
            ["wine", "python", "--version"],
            capture_output=True, text=True
        )
        if result.returncode == 0:
            print(f"✓ Python in Wine: {result.stdout.strip()}")
            return True
    except:
        pass
    
    print("\n⚠ Python not found in Wine")
    print("\nYou'll need to install Python for Windows in Wine:")
    print("1. Download Python installer from python.org")
    print("2. Run: wine python-3.x.x-amd64.exe")
    print("3. Make sure to check 'Add Python to PATH'")
    return False


def install_pyinstaller_in_wine():
    
    print("\nChecking PyInstaller in Wine...")
    try:
        result = subprocess.run(
            ["wine", "python", "-c", "import PyInstaller; print('ok')"],
            capture_output=True, text=True
        )
        if result.returncode == 0 and "ok" in result.stdout:
            print("✓ PyInstaller is installed in Wine")
            return True
    except:
        pass
    
    print("Installing PyInstaller in Wine...")
    try:
        subprocess.check_call(
            ["wine", "python", "-m", "pip", "install", "pyinstaller"],
            stdout=subprocess.DEVNULL
        )
        print("✓ PyInstaller installed in Wine")
        return True
    except subprocess.CalledProcessError as e:
        print(f"✗ Failed to install PyInstaller: {e}")
        return False


def get_windows_path(linux_path):
    
    abs_path = os.path.abspath(linux_path)
    return f"Z:{abs_path.replace('/', '\\')}"


def build_exe():
    
    

    for folder in ["build", "dist"]:
        if os.path.exists(folder):
            print(f"Cleaning {folder}/...")
            shutil.rmtree(folder)
    
    for f in ["KerbonautRedux_ModManager.spec"]:
        if os.path.exists(f):
            os.remove(f)
    

    textures_path = get_windows_path("Textures") + ";Textures"
    models_path = get_windows_path("Models") + ";Models"
    packed_mods_path = get_windows_path("packed mods") + ";packed mods"
    script_path = get_windows_path("kr_gui.py")
    icon_path = get_windows_path("icon.ico")
    version_path = get_windows_path("version_info.txt")
    

    cmd = [
        "wine", "python", "-m", "PyInstaller",
        "--onefile",
        "--windowed",
        "--name", "KerbonautRedux_ModManager",
        "--add-data", textures_path,
        "--add-data", models_path,
        "--add-data", packed_mods_path,
    ]
    

    if os.path.exists("icon.ico"):
        cmd.extend(["--icon", icon_path])
        print(f"✓ Using icon: icon.ico")
    

    if os.path.exists("version_info.txt"):
        cmd.extend(["--version-file", version_path])
        print(f"✓ Using version info")
    
    cmd.extend([
        "--clean",
        "--noconfirm",
        script_path
    ])
    
    print("\nBuilding Windows executable via Wine...")
    print("This may take a few minutes...")
    print()
    
    try:
        subprocess.check_call(cmd)
        print("\n" + "="*60)
        print("✓ Build successful!")
        print("="*60)
        
        exe_path = "dist/KerbonautRedux_ModManager.exe"
        
        if os.path.exists(exe_path):
            size_mb = os.path.getsize(exe_path) / (1024 * 1024)
            print(f"\nWindows executable: {exe_path}")
            print(f"Size: {size_mb:.1f} MB")
            print(f"\nThis .exe can be run on Windows computers!")
            print(f"\nSignature: Onge.org")
            print("\nNote: When distributing, also include:")
            print("  - Textures/ folder")
            print("  - Models/ folder")
            print("  - packed mods/ folder")
        
        return True
        
    except subprocess.CalledProcessError as e:
        print(f"\n✗ Build failed: {e}")
        return False


def main():
    print("="*60)
    print("KerbonautRedux Mod Manager - Windows .exe Builder (via Wine)")
    print("="*60)
    print()
    

    if not os.path.exists("kr_gui.py"):
        print("✗ Error: kr_gui.py not found!")
        print("Please run this script from the KerbonautRedux directory.")
        sys.exit(1)
    

    if not check_wine():
        sys.exit(1)
    
    if not setup_wine_prefix():
        sys.exit(1)
    
    if not install_python_in_wine():
        sys.exit(1)
    
    if not install_pyinstaller_in_wine():
        sys.exit(1)
    

    if build_exe():
        print("\nDone!")
    else:
        print("\nBuild failed.")
        sys.exit(1)


if __name__ == "__main__":
    main()
