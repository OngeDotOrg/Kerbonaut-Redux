# Kerbonaut Redux Mod

The KSP mod part of Kerbonaut Redux. Lets you attach custom meshes (hair, accessories, whatever) to your kerbals.

## What It Does

- Stick custom hair on kerbals (Finally ksp2 hair)
- Hide original parts if needed
- Works in IVA and EVA
- JSON configs so you don't need to recompile for changes

## Project Structure

```
src/
├── KerbonautReduxAddon.cs    # Entry point
├── HairModule.cs              # Makes the hair stick to heads
├── HairConfigs.cs             # Loads your JSON configs
├── HairVisibilityChecker.cs   # Hides hair when helmet is on
├── AssemblyInfo.cs            # Metadata stuff
└── KerbalRedux.csproj         # Project file

install_to_ksp.sh              # Helper script to install
kerbal_redux_export.py         # Blender export script
```

## Building

```bash
dotnet build src/KerbalRedux.csproj
```

Or open in Visual Studio/Rider and hit build. Then copy the DLL to GameData.

## Requirements

- KSP 1.12.x
- .NET Framework 4.7.2 or compatible

## Wanna Contribute?

PRs welcome! See CONTRIBUTING.md

## License

Free KSP mod by Onge.org. See LICENSE file.
