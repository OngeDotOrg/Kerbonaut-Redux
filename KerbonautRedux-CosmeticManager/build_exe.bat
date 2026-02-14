@echo off

echo ===========================================
echo KerbonautRedux Mod Manager - EXE Builder
echo ===========================================
echo.

python --version >nul 2>&1
if errorlevel 1 (
    echo Error: Python not found!
    echo Please install Python 3.7 or higher.
    pause
    exit /b 1
)

if not exist "kr_gui.py" (
    echo Error: kr_gui.py not found!
    echo Please run this script from the KerbonautRedux directory.
    pause
    exit /b 1
)

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

echo Cleaning previous builds...
if exist "build" rmdir /s /q "build"
if exist "dist" rmdir /s /q "dist"
if exist "KerbonautRedux_ModManager.spec" del "KerbonautRedux_ModManager.spec"

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
