#!/usr/bin/env python3
"""
KerbalRedux Mesh Converter
==========================

Converts OBJ files to KSP mesh format (.vtx, .tex, .nml, .idx)

Usage:
    python mesh_converter.py <input.obj> [options]
    
Examples:
    # Basic conversion
    python mesh_converter.py ValentinaHair.obj
    
    # With custom output name
    python mesh_converter.py ValentinaHair.obj --name BetterHair
    
    # Auto-install to KSP
    python mesh_converter.py ValentinaHair.obj --install

Interactive Mode (no arguments):
    python mesh_converter.py
"""

import struct
import sys
import argparse
from pathlib import Path


def parse_obj(filepath):
    """Parse OBJ file and return vertices, normals, uvs, and faces."""
    vertices = []
    normals = []
    uvs = []
    faces = []
    
    print(f"📂 Reading {filepath}...")
    
    with open(filepath, 'r') as f:
        for line_num, line in enumerate(f, 1):
            line = line.strip()
            if not line or line.startswith('#'):
                continue
                
            parts = line.split()
            cmd = parts[0]
            
            try:
                if cmd == 'v':
                    vertices.append((float(parts[1]), float(parts[2]), float(parts[3])))
                elif cmd == 'vn':
                    normals.append((float(parts[1]), float(parts[2]), float(parts[3])))
                elif cmd == 'vt':
                    uvs.append((float(parts[1]), float(parts[2])))
                elif cmd == 'f':
                    face = []
                    for part in parts[1:]:
                        idx = int(part.split('/')[0]) - 1
                        face.append(idx)
                    faces.append(face)
            except (ValueError, IndexError) as e:
                print(f"⚠️  Warning: Line {line_num} malformed: {line[:50]}...")
                continue
    
    print(f"   ✅ Loaded {len(vertices)} vertices, {len(normals)} normals, {len(uvs)} UVs, {len(faces)} faces")
    return vertices, normals, uvs, faces


def triangulate_faces(faces):
    """Convert polygons to triangles."""
    triangles = []
    for face in faces:
        if len(face) == 3:
            triangles.extend(face)
        elif len(face) == 4:
            triangles.extend([face[0], face[1], face[2], face[0], face[2], face[3]])
        elif len(face) > 4:
            for i in range(1, len(face) - 1):
                triangles.extend([face[0], face[i], face[i + 1]])
    return triangles


def generate_normals(vertices, triangles):
    """Generate normals from geometry."""
    import math
    normals = [(0.0, 0.0, 0.0) for _ in vertices]
    counts = [0 for _ in vertices]
    
    for i in range(0, len(triangles), 3):
        i0, i1, i2 = triangles[i], triangles[i+1], triangles[i+2]
        v0 = vertices[i0]
        v1 = vertices[i1]
        v2 = vertices[i2]
        
        # Cross product
        ax, ay, az = v1[0]-v0[0], v1[1]-v0[1], v1[2]-v0[2]
        bx, by, bz = v2[0]-v0[0], v2[1]-v0[1], v2[2]-v0[2]
        nx, ny, nz = ay*bz-az*by, az*bx-ax*bz, ax*by-ay*bx
        
        for idx in (i0, i1, i2):
            normals[idx] = (normals[idx][0]+nx, normals[idx][1]+ny, normals[idx][2]+nz)
            counts[idx] += 1
    
    # Average and normalize
    result = []
    for i, (n, c) in enumerate(zip(normals, counts)):
        if c > 0:
            x, y, z = n[0]/c, n[1]/c, n[2]/c
            length = math.sqrt(x*x + y*y + z*z)
            if length > 0:
                result.append((x/length, y/length, z/length))
            else:
                result.append((0, 1, 0))
        else:
            result.append((0, 1, 0))
    return result


def write_vector3_array(data, filepath, name):
    """Write Vector3 array to binary file."""
    with open(filepath, 'wb') as f:
        f.write(struct.pack('I', len(data)))
        for v in data:
            f.write(struct.pack('fff', v[0], v[1], v[2]))
    print(f"   📝 {name}: {len(data)} entries -> {filepath.name} ({Path(filepath).stat().st_size//1024}KB)")


def write_vector2_array(data, filepath, name):
    """Write Vector2 array to binary file."""
    with open(filepath, 'wb') as f:
        f.write(struct.pack('I', len(data)))
        for v in data:
            f.write(struct.pack('ff', v[0], v[1]))
    print(f"   📝 {name}: {len(data)} entries -> {filepath.name} ({Path(filepath).stat().st_size//1024}KB)")


def write_int_array(data, filepath, name):
    """Write int array to binary file."""
    with open(filepath, 'wb') as f:
        f.write(struct.pack('I', len(data)))
        for idx in data:
            f.write(struct.pack('i', idx))
    print(f"   📝 {name}: {len(data)//3} triangles -> {filepath.name} ({Path(filepath).stat().st_size//1024}KB)")


def convert_mesh(input_path, output_name=None, output_dir=None):
    """Convert OBJ to KSP mesh format."""
    input_path = Path(input_path)
    
    if not input_path.exists():
        print(f"❌ Error: File not found: {input_path}")
        return False
    
    # Determine output name
    if output_name is None:
        output_name = input_path.stem
    
    # Determine output directory
    if output_dir is None:
        output_dir = input_path.parent
    else:
        output_dir = Path(output_dir)
        output_dir.mkdir(parents=True, exist_ok=True)
    
    print(f"\n🔄 Converting {input_path.name}...")
    print("=" * 50)
    
    # Parse OBJ
    vertices, normals, uvs, faces = parse_obj(input_path)
    
    if not vertices:
        print("❌ Error: No vertices found!")
        return False
    
    # Triangulate
    triangles = triangulate_faces(faces)
    print(f"   📐 Triangulated to {len(triangles)//3} triangles")
    
    # Generate normals if missing
    if not normals:
        print("   🔄 Generating normals...")
        normals = generate_normals(vertices, triangles)
    
    # Write output files
    base_path = output_dir / output_name
    
    write_vector3_array(vertices, f"{base_path}.vtx", "Vertices")
    
    if uvs:
        write_vector2_array(uvs, f"{base_path}.tex", "UVs")
    else:
        print("   ⚠️  Warning: No UV coordinates")
    
    write_vector3_array(normals, f"{base_path}.nml", "Normals")
    write_int_array(triangles, f"{base_path}.idx", "Indices")
    
    print("=" * 50)
    print(f"✅ Conversion complete! Files written to {output_dir}/")
    print(f"\n   Output files:")
    for ext in ['.vtx', '.tex', '.nml', '.idx']:
        f = base_path.parent / (base_path.name + ext)
        if f.exists():
            size = f.stat().st_size
            print(f"      • {f.name:<20} ({size:>6} bytes)")
    
    return True


def find_ksp_gamedata():
    """Try to find KSP GameData folder."""
    possible_paths = [
        Path.home() / ".steam/steam/steamapps/common/Kerbal Space Program/GameData",
        Path.home() / ".local/share/Steam/steamapps/common/Kerbal Space Program/GameData",
        Path("/home/navajo/m2drive/SteamLibrary/steamapps/common/Kerbal Space Program/GameData"),
    ]
    
    for path in possible_paths:
        if path.exists():
            return path
    return None


def install_to_ksp(mesh_name):
    """Install mesh files to KSP."""
    gamedata = find_ksp_gamedata()
    
    if gamedata is None:
        print("❌ Could not find KSP GameData folder!")
        print("   Please specify path manually or copy files manually.")
        return False
    
    mod_path = gamedata / "KerbalRedux" / "Models"
    mod_path.mkdir(parents=True, exist_ok=True)
    
    print(f"\n📦 Installing to KSP...")
    print(f"   Destination: {mod_path}")
    
    # Copy files
    src_dir = Path(".")
    copied = []
    for ext in ['.vtx', '.tex', '.nml', '.idx']:
        src = src_dir / f"{mesh_name}{ext}"
        if src.exists():
            dst = mod_path / f"{mesh_name}{ext}"
            import shutil
            shutil.copy2(src, dst)
            copied.append(dst.name)
    
    if copied:
        print(f"   ✅ Copied {len(copied)} files: {', '.join(copied)}")
        print(f"\n   🚀 Files installed! Restart KSP to see changes.")
        return True
    else:
        print("   ❌ No files found to copy")
        return False


def interactive_mode():
    """Run in interactive mode."""
    print("\n" + "=" * 60)
    print("   KerbalRedux Mesh Converter")
    print("=" * 60)
    
    # Find OBJ files
    obj_files = list(Path(".").glob("*.obj"))
    
    if obj_files:
        print("\n📂 Found OBJ files:")
        for i, f in enumerate(obj_files, 1):
            print(f"   {i}. {f.name}")
        print("   0. Enter path manually")
    else:
        print("\n📂 No OBJ files found in current directory")
    
    # Get input
    while True:
        choice = input("\nSelect file (number or path): ").strip()
        
        if choice.isdigit():
            idx = int(choice)
            if idx == 0:
                filepath = input("Enter path to OBJ file: ").strip()
            elif 1 <= idx <= len(obj_files):
                filepath = str(obj_files[idx-1])
            else:
                print("Invalid selection")
                continue
        else:
            filepath = choice
        
        if not Path(filepath).exists():
            print(f"❌ File not found: {filepath}")
            continue
        
        break
    
    # Get output name
    default_name = Path(filepath).stem
    name = input(f"Output name [{default_name}]: ").strip()
    if not name:
        name = default_name
    
    # Convert
    if convert_mesh(filepath, name):
        # Ask to install
        install = input("\nInstall to KSP? [y/N]: ").strip().lower()
        if install in ('y', 'yes'):
            install_to_ksp(name)
    
    print("\n✨ Done!")


def main():
    parser = argparse.ArgumentParser(
        description='Convert OBJ files to Kerbal Space Program mesh format',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  %(prog)s ValentinaHair.obj
  %(prog)s hair.obj --name BetterHair --install
  %(prog)s                           # Interactive mode
        """
    )
    
    parser.add_argument('input', nargs='?', help='Input OBJ file')
    parser.add_argument('-n', '--name', help='Output name (default: input filename)')
    parser.add_argument('-o', '--output-dir', help='Output directory')
    parser.add_argument('-i', '--install', action='store_true', help='Install to KSP after conversion')
    
    args = parser.parse_args()
    
    # Interactive mode if no input
    if args.input is None:
        interactive_mode()
        return
    
    # Batch mode
    if convert_mesh(args.input, args.name, args.output_dir):
        output_name = args.name or Path(args.input).stem
        
        if args.install:
            install_to_ksp(output_name)
        else:
            print("\n💡 Tip: Use --install to copy to KSP automatically")


if __name__ == '__main__':
    main()
