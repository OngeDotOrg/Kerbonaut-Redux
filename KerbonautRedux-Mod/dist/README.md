# Kerbal Redux - Complete Package

A KSP mod for attaching custom meshes to kerbals, with full Blender integration and runtime customization.

## What's New

### Version 2.0 Features:
- **Custom Shader Selection**: Choose from KSP/Specular, KSP/Bumped Specular, KSP/Alpha/Translucent, and more!
- **Bump/Normal Map Support**: Add detail to your cosmetics with normal maps
- **Kerbal Body Texture Override**: Change the kerbal's suit/body texture
- **Runtime Modification API**: Modify position, rotation, scale, color, and shaders at runtime
- **Multiple Hair Pieces**: Attach multiple meshes per kerbal

## What's Included

### 1. KSP Mod (`KerbalRedux/`)
The actual mod that runs in Kerbal Space Program.

**Installation:**
1. Copy `KerbalRedux/` folder to `KSP/GameData/`
2. Launch KSP

**Files:**
- `KerbonautRedux.dll` - The mod code
- `KerbonautRedux.json` - Configuration file (edit this!)
- `Models/` - Mesh files (.vtx, .tex, .nml, .idx)
- `Textures/` - Texture files (.png)

## Configuration

Edit `KerbonautRedux.json` to add/change attachments:

### Full Example Config:

```json
{
  "configs": [
    {
      "kerbalName": "Valentina Kerman",
      "hideHead": false,
      "hidePonytail": false,
      "hideEyes": false,
      "hideTeeth": false,
      "hideTongue": false,
      
      "kerbalTexture": "custom_suit.png",
      "kerbalNormalMap": "custom_suit_normal.png",
      
      "hairPieces": [
        {
          "meshName": "ValentinaHair",
          "meshTexture": "valentina_hair.png",
          "bumpTexture": "valentina_hair_normal.png",
          "boneName": "bn_upperJaw01",
          "shader": "KSP/Bumped Specular",
          "posX": 0.0,
          "posY": 0.05,
          "posZ": 0.02,
          "rotX": 0.0,
          "rotY": 0.0,
          "rotZ": 0.0,
          "scale": 1.0,
          "hairColorR": 0.2,
          "hairColorG": 0.15,
          "hairColorB": 0.1
        }
      ]
    }
  ]
}
```

### Configuration Fields:

**Kerbal Settings:**
- `kerbalName`: Name of the kerbal (try "Valentina" or "Valentina Kerman")
- `hideHead`: Hide the kerbal's head mesh
- `hidePonytail`: Hide the ponytail
- `hideEyes`: Hide the eyes
- `hideTeeth`: Hide the teeth
- `hideTongue`: Hide the tongue
- `kerbalTexture`: Path to custom body/suit texture (optional)
- `kerbalNormalMap`: Path to custom body/suit normal map (optional)

**Hair Piece Settings:**
- `meshName`: Name of mesh files in Models/ folder (without extension)
- `meshTexture`: Name of texture file in Textures/ folder
- `bumpTexture`: Name of normal map texture (optional)
- `boneName`: Bone to attach to (usually "bn_upperJaw01" for head)
- `shader`: Shader to use (see Available Shaders below)
- `posX`, `posY`, `posZ`: Position offset
- `rotX`, `rotY`, `rotZ`: Rotation offset (Euler angles)
- `scale`: Uniform scale
- `hairColorR`, `hairColorG`, `hairColorB`: Color tint (0-1 range)

### Available Shaders:

- `KSP/Specular` (default) - Basic specular shader
- `KSP/Bumped` - Normal mapped shader
- `KSP/Bumped Specular` - Normal mapped with specular highlights
- `KSP/Bumped Specular (Mapped)` - Advanced normal mapping
- `KSP/Alpha/Cutoff` - Cutout transparency
- `KSP/Alpha/Cutoff Bumped` - Cutout with normal map
- `KSP/Alpha/Translucent` - Full transparency (glass, etc)
- `KSP/Alpha/Translucent Specular` - Transparent with specular
- `KSP/Alpha/Unlit Transparent` - Unlit transparent

## Runtime Modification API

You can modify cosmetics at runtime using the KerbonautReduxAddon API:

```csharp
// Get a specific kerbal's hair module
var hairModule = KerbonautReduxAddon.FindHairModule("Valentina Kerman");

// Modify transform
hairModule.SetHairPiecePosition(0, new Vector3(0, 0.06f, 0.02f));
hairModule.SetHairPieceRotation(0, new Vector3(0, 45f, 0));
hairModule.SetHairPieceScale(0, 1.2f);

// Modify material
hairModule.SetHairPieceColor(0, Color.red);
hairModule.SetHairPieceShader(0, "KSP/Alpha/Translucent");

// Show/hide
hairModule.SetHairPieceVisible(0, false);
```

### Available Runtime Methods:

**Transform:**
- `SetHairPiecePosition(index, position)` - Update local position
- `SetHairPieceRotation(index, rotation)` - Update local rotation (Euler)
- `SetHairPieceScale(index, scale)` - Update uniform scale
- `GetHairPieceTransform(index, out position, out rotation, out scale)` - Get current values

**Material:**
- `SetHairPieceColor(index, color)` - Change tint color
- `SetHairPieceTexture(index, texture)` - Change diffuse texture
- `SetHairPieceNormalMap(index, normalMap)` - Change normal map
- `SetHairPieceShader(index, shaderName)` - Change shader
- `SetHairPieceVisible(index, visible)` - Show/hide piece

**Utility:**
- `GetHairPieceCount()` - Get number of hair pieces
- `GetKerbalName()` - Get the kerbal's name

### Static Access Methods:

```csharp
// Find any kerbal's module
var ivaModule = KerbonautReduxAddon.FindHairModule("Valentina Kerman");
var evaModule = KerbonautReduxAddon.FindEvaHairModule("Valentina Kerman");

// Get all modules
var allIva = KerbonautReduxAddon.GetAllHairModules();
var allEva = KerbonautReduxAddon.GetAllEvaHairModules();

// Direct modification
KerbonautReduxAddon.SetKerbalHairPosition("Valentina", 0, new Vector3(0, 0.1f, 0));
KerbonautReduxAddon.SetKerbalHairRotation("Valentina", 0, new Vector3(0, 90f, 0));
KerbonautReduxAddon.SetKerbalHairScale("Valentina", 0, 1.5f);
KerbonautReduxAddon.SetKerbalHairShader("Valentina", 0, "KSP/Alpha/Translucent");
```

### 2. Blender Addon (`kerbal_redux_export.py`)
Export meshes directly from Blender to KSP format!

**Installation:**
1. Open Blender
2. Edit → Preferences → Add-ons
3. Click Install...
4. Select `kerbal_redux_export.py`
5. Enable the addon (check the box)

Or run: `./install_blender_addon.sh` (Linux/macOS)

**Usage:**
1. Model your mesh in Blender
2. Position it on the kerbal head
3. UV unwrap
4. File → Export → Kerbal Redux Mesh (.vtx/.tex/.nml/.idx)
5. Choose output folder and mesh name
6. Click Export

**Features:**
- Exports directly to KSP binary format
- Auto-triangulates faces
- Handles UVs and normals
- Shows mesh stats in sidebar

### 3. Python Converter (`mesh_converter.py`)
Command-line alternative to the Blender addon.

**Usage:**
```bash
python3 mesh_converter.py MyMesh.obj --install
```

## Complete Workflow

### For Mod Users (Playing with the mod):

1. Install mod: Copy `KerbalRedux/` to `KSP/GameData/`
2. Edit `KerbonautRedux.json` to configure attachments
3. Launch KSP and enjoy!

### For Mod Developers (Creating new attachments):

1. **Install the Blender addon**
   - Follow installation steps above

2. **Create your mesh**
   - Model in Blender
   - Position on kerbal head (use reference if needed)
   - UV unwrap

3. **Export from Blender**
   - File → Export → Kerbal Redux Mesh
   - Name it (e.g., "CoolHat")

4. **Copy to KSP**
   - Copy `.vtx`, `.tex`, `.nml`, `.idx` to `KSP/GameData/KerbalRedux/Models/`
   - Copy texture `.png` to `KSP/GameData/KerbalRedux/Textures/`

5. **Edit config**
   - Open `KSP/GameData/KerbalRedux/KerbonautRedux.json`
   - Add entry for your kerbal:
   ```json
   {
     "kerbalName": "Valentina",
     "meshName": "CoolHat",
     "meshTexture": "cool_hat_texture.png",
     "bumpTexture": "cool_hat_normal.png",
     "shader": "KSP/Bumped Specular",
     "boneName": "bn_upperJaw01"
   }
   ```

6. **Test**
   - Launch KSP
   - Quickload (F9) to reload config
   - Check if attachment appears

7. **Iterate**
   - Adjust mesh in Blender
   - Re-export
   - Quickload in KSP
   - Repeat until perfect!

## Tips

- **Position/Rotation/Scale**: Set these in Blender before exporting. The mod attaches the mesh as-is.
- **Shader Selection**: Use `KSP/Alpha/Translucent` for glasses, visors, or ghostly effects.
- **Normal Maps**: Add `_bumpTexture` to make your cosmetics look more detailed.
- **Bone names**: Most kerbal heads use `bn_upperJaw01`. Check the reference head if unsure.
- **Quickloading**: Press F9 in KSP to reload the JSON config without restarting.
- **Runtime API**: Use the API to create interactive UIs for customizing cosmetics in-game!

## Troubleshooting

**Mesh not showing?**
- Check KSP console (Alt+F12) for errors
- Verify mesh files are in Models/ folder
- Check JSON syntax (commas, brackets)
- Try both "Valentina" and "Valentina Kerman" for kerbalName

**Wrong position?**
- Adjust in Blender
- Re-export
- Quickload in KSP
- Or use the runtime API to adjust position live!

**Texture not showing?**
- Verify texture is in Textures/ folder
- Check texture name matches JSON exactly
- Try a simple test texture first
- Check that the shader supports transparency if using alpha

**Shader not working?**
- Check the exact shader name from Available Shaders list
- Some shaders require normal maps to look right

## License

Free to use, modify, and distribute!
