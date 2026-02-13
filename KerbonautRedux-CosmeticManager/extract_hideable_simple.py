#!/usr/bin/env python3


import UnityPy
import os

ksp_path = "/home/navajo/m2drive/SteamLibrary/steamapps/common/Kerbal Space Program/KSP_x64_Data"

print("Extracting hideable parts from KSP...")
print()

parts_found = {
    'head': [],
    'helmet': [],
    'visor': [],
    'hair': [],
    'ponytail': [],
    'eyes': [],
    'teeth': [],
    'mouth': [],
    'ears': [],
}

for file in sorted(os.listdir(ksp_path)):
    if not file.startswith("sharedassets"):
        continue
        
    filepath = os.path.join(ksp_path, file)
    
    try:
        env = UnityPy.load(filepath)
        
        for obj in env.objects:
            if obj.type.name in ["SkinnedMeshRenderer", "MeshRenderer"]:
                try:
                    data = obj.read()
                    go = data.m_GameObject.read()
                    name = go.m_Name
                    name_lower = name.lower()
                    

                    if not any(x in name_lower for x in ['kerbal', 'eva', 'astronaut', 'girl']):
                        continue
                    

                    if 'ponytail' in name_lower:
                        parts_found['ponytail'].append((name, file))
                    elif 'hair' in name_lower:
                        parts_found['hair'].append((name, file))
                    elif 'helmet' in name_lower:
                        parts_found['helmet'].append((name, file))
                    elif 'visor' in name_lower:
                        parts_found['visor'].append((name, file))
                    elif 'pupil' in name_lower or ('eyeball' in name_lower):
                        parts_found['eyes'].append((name, file))
                    elif 'teeth' in name_lower:
                        parts_found['teeth'].append((name, file))
                    elif 'head' in name_lower and 'helmet' not in name_lower:
                        parts_found['head'].append((name, file))
                    elif 'ear' in name_lower:
                        parts_found['ears'].append((name, file))
                        
                except:
                    pass
                    
    except:
        pass


for category, items in parts_found.items():
    if items:
        print(f"\n{category.upper()}:")
        for name, src_file in sorted(set(items)):
            print(f"  - {name}")

print("\n" + "="*50)
print("SUGGESTED HIDE OPTIONS FOR CONFIG:")
print("="*50)
print()
