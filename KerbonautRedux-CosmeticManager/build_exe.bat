@echo off
REM Build script for KerbonautRedux Mod Manager (Windows)
REM Creates a standalone .exe file

echo ===========================================
echo KerbonautRedux Mod Manager - EXE Builder
echo ===========================================
echo.

REM Check if Python is available
python --version >nul 2>&1
if errorlevel 1 (
    echo Error: Python not found!
    echo Please install Python 3.7 or higher.
    pause
    exit /b 1
)

REM Check if kr_gui.py exists
if not exist "kr_gui.py" (
    echo Error: kr_gui.py not found!
    echo Please run this script from the KerbonautRedux directory.
    pause
    exit /b 1
)

REM Install PyInstaller if needed
echo Checking PyInstaller...
python -c "import PyInstaller" >nul 2>&1
if errorlevel 1 (
    echo Installing PyInstaller...
    python -m pip install pyinstaller
    if errorlevel 1 (
        echo Failed to install PyInstaller.
        pause
        exit /b 1
    )
)
echo PyInstaller is ready.
echo.

REM Clean previous builds
echo Cleaning previous builds...
if exist "build" rmdir /s /q "build"
if exist "dist" rmdir /s /q "dist"
if exist "KerbonautRedux_ModManager.spec" del "KerbonautRedux_ModManager.spec"

REM Build the executable
echo.
echo Building executable...
echo This may take a few minutes...
echo.

python -m PyInstaller ^
    --onefile ^
    --windowed ^
    --name "KerbonautRedux_ModManager" ^
    --add-data "Textures;Textures" ^
    --add-data "Models;Models" ^
    --add-data "packed mods;packed mods" ^
    --clean ^
    --noconfirm ^
    kr_gui.py

if errorlevel 1 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo ===========================================
echo Build successful!
echo ===========================================
echo.
echo Executable location: dist\KerbonautRedux_ModManager.exe
echo.

REM Show file size
for %%I in ("dist\KerbonautRedux_ModManager.exe") do (
    echo File size: %%~zI bytes
)

echo.
echo You can now run: dist\KerbonautRedux_ModManager.exe
echo.
echo Note: When distributing the .exe, also include:
echo   - Textures\ folder
echo   - Models\ folder  
echo   - packed mods\ folder
echo   - KerbonautRedux.json (will be created on first run)
echo.
pause
