# Kerbal Redux Blender Addon

Export meshes directly to Kerbal Redux format from Blender!

## Installation

1. Open Blender
2. Go to **Edit > Preferences > Add-ons**
3. Click **Install...**
4. Select `kerbal_redux_export.py`
5. Enable the addon by checking the box

## Usage

### Method 1: Export Dialog

1. **Position your mesh** on the kerbal head (use the reference head if needed)
2. **UV unwrap** the mesh
3. Go to **File > Export > Kerbal Redux Mesh (.vtx/.tex/.nml/.idx)**
4. Choose output folder and set **Mesh Name** (e.g., "ValentinaHair")
5. Click **Export Kerbal Redux Mesh**

Files will be created:
- `ValentinaHair.vtx` - Vertices
- `ValentinaHair.tex` - UV coordinates  
- `ValentinaHair.nml` - Normals
- `ValentinaHair.idx` - Triangle indices

### Method 2: Sidebar Panel

1. Select your mesh object
2. Open the **Kerbal Redux** panel in the 3D View sidebar (press N)
3. Click **Export Kerbal Redux Mesh**

## Workflow

### For Mod Developers:

1. **Model** your mesh in Blender, position it correctly on the reference head
2. **UV unwrap** and create texture
3. **Export** using the addon
4. **Copy** the generated `.vtx`, `.tex`, `.nml`, `.idx` files to:
   ```
   KSP/GameData/KerbalRedux/Models/
   ```
5. **Edit** `KerbalRedux.json` to add your kerbal:
   ```json
   {
     "kerbalName": "YourKerbal",
     "meshName": "YourMesh",
     "meshTexture": "your_texture.png",
     "boneName": "bn_upperJaw01"
   }
   ```

### Tips

- **Apply transforms** before export (Ctrl+A > All Transforms)
- The mesh origin should be at the bone attachment point
- Keep mesh reasonable size (under 5000 vertices)
- Test in KSP with quickload (F9) to iterate

## Features

- ✅ Exports directly to KSP binary format
- ✅ Triangulates quads and n-gons automatically
- ✅ Handles UV coordinates
- ✅ Calculates normals
- ✅ Shows mesh stats in sidebar

## Troubleshooting

**"No active object selected!"**
→ Select your mesh object first

**"Mesh has no vertices!"**
→ Check that your mesh has geometry

**Texture not showing in KSP**
→ Make sure texture is in `GameData/KerbalRedux/Textures/`
→ Check that texture name matches the JSON config exactly
