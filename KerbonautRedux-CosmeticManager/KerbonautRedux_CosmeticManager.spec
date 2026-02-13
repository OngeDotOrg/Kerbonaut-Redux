# -*- mode: python ; coding: utf-8 -*-


a = Analysis(
    ['Z:/home/navajo/m2drive/SteamLibrary/steamapps/common/Kerbal Space Program/GameData/KerbonautRedux/kr_gui.py'],
    pathex=[],
    binaries=[],
    datas=[('Z:/home/navajo/m2drive/SteamLibrary/steamapps/common/Kerbal Space Program/GameData/KerbonautRedux/Textures', 'Textures'), ('Z:/home/navajo/m2drive/SteamLibrary/steamapps/common/Kerbal Space Program/GameData/KerbonautRedux/Models', 'Models'), ('Z:/home/navajo/m2drive/SteamLibrary/steamapps/common/Kerbal Space Program/GameData/KerbonautRedux/packed mods', 'packed mods')],
    hiddenimports=[],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='KerbonautRedux_CosmeticManager',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    version='Z:\\home\\navajo\\m2drive\\SteamLibrary\\steamapps\\common\\Kerbal Space Program\\GameData\\KerbonautRedux\\version_info.txt',
    icon=['Z:\\home\\navajo\\m2drive\\SteamLibrary\\steamapps\\common\\Kerbal Space Program\\GameData\\KerbonautRedux\\icon.ico'],
)
