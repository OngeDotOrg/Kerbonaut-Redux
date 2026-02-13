# Kerbonaut Redux Cosmetic Manager

A GUI tool for managing Kerbonaut Redux configs. editing JSON by hand is annoying.

## What It Does

- Visual editor for kerbal configs
- Import/manage custom meshes
- Set bone attachments and offsets
- Configure which parts to hide
- Export everything to JSON

## Files

```
kr_gui.py                      # The GUI (tkinter)
kr_manager.py                  # Core logic
extract_bones.py               # Get bone names from models
extract_hideable_parts.py      # Figure out what can be hidden
build_exe.py                   # Build Windows executable
launch_gui.sh                  # Quick launch on Linux
```

## Running

From source:
```bash
python kr_gui.py
```

Build exe:
```bash
python build_exe.py
```

## Requirements

- Python 3.8+
- tkinter (usually comes with Python)
- PyInstaller (for building exe)

## Contributing

See CONTRIBUTING.md

## License

Free tool by Onge.org. See LICENSE file.
