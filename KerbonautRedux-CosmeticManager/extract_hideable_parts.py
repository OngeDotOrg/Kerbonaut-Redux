#!/usr/bin/env python3


import UnityPy
import os

ksp_path = "/home/navajo/m2drive/SteamLibrary/steamapps/common/Kerbal Space Program/KSP_x64_Data"


HIDEABLE_KEYWORDS = ['head', 'hair', 'ponytail', 'eye', 'teeth', 'mouth', 'visor', 'helmet', 'ear']

print("Scanning for hideable mesh parts in KSP...")
print("="*60)

found_parts = {}

for file in sorted(os.listdir(ksp_path)):
    if not file.startswith("sharedassets"):
        continue
        
    filepath = os.path.join(ksp_path, file)
    
    try:
        env = UnityPy.load(filepath)
        
        for obj in env.objects:
            if obj.type.name == "SkinnedMeshRenderer":
                try:
                    data = obj.read()
                    mesh_name = data.m_GameObject.read().m_Name
                    

                    mesh_lower = mesh_name.lower()
                    is_kerbal = any(x in mesh_lower for x in ['kerbal', 'eva', 'astronaut', 'girl'])
                    
                    if not is_kerbal:
                        continue
                    

                    is_hideable = any(kw in mesh_lower for kw in HIDEABLE_KEYWORDS)
                    

                    enabled = getattr(data, 'm_Enabled', True)
                    
                    if is_hideable or 'head' in mesh_lower or 'hair' in mesh_lower:
                        if mesh_name not in found_parts:
                            found_parts[mesh_name] = {
                                'file': file,
                                'enabled': enabled,
                                'bone_count': len(data.m_Bones),
                                'keywords_found': [kw for kw in HIDEABLE_KEYWORDS if kw in mesh_lower]
                            }
                        
                except Exception as e:
                    pass
                    
    except Exception as e:
        print(f"Error loading {file}: {e}")


print("\n" + "="*60)
print("POTENTIALLY HIDEABLE PARTS:")
print("="*60)

categories = {
    'HEAD': ['head', 'helmet'],
    'HAIR': ['hair', 'ponytail'],
    'EYES': ['eye', 'pupil'],
    'MOUTH': ['teeth', 'mouth', 'tongue'],
    'FACE': ['face', 'jaw', 'ear'],
    'OTHER': []
}

for cat_name, keywords in categories.items():
    print(f"\n
    found_in_cat = []
    
    for mesh_name, info in sorted(found_parts.items()):
        mesh_lower = mesh_name.lower()
        matches = any(kw in mesh_lower for kw in keywords)
        
        if cat_name == 'OTHER':

            all_keywords = [k for sublist in categories.values() for k in sublist if sublist != keywords]
            matches = not any(kw in mesh_lower for kw in all_keywords) and info['keywords_found']
        
        if matches or (cat_name == 'OTHER' and info['keywords_found']):
            found_in_cat.append((mesh_name, info))
    
    if found_in_cat:
        for mesh_name, info in found_in_cat:
            status = "✓" if info['enabled'] else "✗"
            print(f"  {status} {mesh_name}")
            print(f"     Bones: {info['bone_count']} | File: {info['file']}")
    else:
        print("  (none found)")

print("\n" + "="*60)
print("ALL KERBAL MESHES (for reference):")
print("="*60)
for mesh_name in sorted(found_parts.keys()):
    info = found_parts[mesh_name]
    status = "✓ enabled" if info['enabled'] else "✗ disabled"
    print(f"  {mesh_name} ({status}, {info['bone_count']} bones)")


print("\n" + "="*60)
print("Generating hideable_parts.json...")
import json

hideable_config = {
    "hideable_parts": {
        "head": [m for m in found_parts if 'head' in m.lower() and 'helmet' not in m.lower()],
        "helmet": [m for m in found_parts if 'helmet' in m.lower()],
        "ponytail": [m for m in found_parts if 'ponytail' in m.lower() or 'hair' in m.lower()],
        "eyes": [m for m in found_parts if 'eye' in m.lower() or 'pupil' in m.lower()],
        "teeth": [m for m in found_parts if 'teeth' in m.lower()],
        "visor": [m for m in found_parts if 'visor' in m.lower()],
    },
    "all_meshes": list(found_parts.keys())
}

with open('hideable_parts.json', 'w') as f:
    json.dump(hideable_config, f, indent=2)

print("Saved to hideable_parts.json!")
