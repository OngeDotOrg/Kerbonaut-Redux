#!/usr/bin/env python3


import os
import sys
import json
import shutil
import zipfile
import argparse
from pathlib import Path
from dataclasses import dataclass, asdict, field
from typing import List, Dict, Optional, Set


@dataclass
class HairPiece:
    meshName: str
    meshTexture: str
    boneName: str

    def to_dict(self):
        return {
            "meshName": self.meshName,
            "meshTexture": self.meshTexture,
            "boneName": self.boneName,
        }
    
    def __hash__(self):
        return hash((self.meshName, self.meshTexture, self.boneName))


@dataclass
class KerbalConfig:
    kerbalName: str
    hideHead: bool = False
    hidePonytail: bool = False
    hideEyes: bool = False
    hideTeeth: bool = False
    hairPieces: List[HairPiece] = field(default_factory=list)

    def to_dict(self):
        return {
            "kerbalName": self.kerbalName,
            "hideHead": self.hideHead,
            "hidePonytail": self.hidePonytail,
            "hideEyes": self.hideEyes,
            "hideTeeth": self.hideTeeth,
            "hairPieces": [hp.to_dict() for hp in self.hairPieces],
        }


@dataclass
class ModInfo:
    name: str
    author: str
    version: str
    description: str
    configs: List[dict]
    installed_path: Optional[str] = None


class KerbonautReduxManager:
    BONES = [
        "bn_upperJaw01",
        "bn_lowerJaw01", 
        "bn_head01",
        "bn_neck01",
        "bn_spine01",
        "bn_spine02",
        "bn_spine03",
        "bn_l_clavicle01",
        "bn_r_clavicle01",
        "bn_l_upperArm01",
        "bn_r_upperArm01",
    ]

    def __init__(self, base_path: str = "."):
        self.base_path = Path(base_path).resolve()
        self.textures_path = self.base_path / "Textures"
        self.models_path = self.base_path / "Models"
        self.config_path = self.base_path / "KerbonautRedux.json"
        self.packed_mods_path = self.base_path / "packed mods"
        self.installed_mods_path = self.base_path / ".installed_mods.json"
        

        self.textures_path.mkdir(exist_ok=True)
        self.models_path.mkdir(exist_ok=True)
        self.packed_mods_path.mkdir(exist_ok=True)

    def load_main_config(self) -> List[KerbalConfig]:
        
        if not self.config_path.exists():
            return []
        
        with open(self.config_path, 'r') as f:
            data = json.load(f)
        
        configs = []
        for cfg in data.get("configs", []):
            hair_pieces = [
                HairPiece(
                    meshName=hp.get("meshName", ""),
                    meshTexture=hp.get("meshTexture", ""),
                    boneName=hp.get("boneName", "bn_upperJaw01"),
                )
                for hp in cfg.get("hairPieces", [])
            ]
            configs.append(KerbalConfig(
                kerbalName=cfg.get("kerbalName", ""),
                hideHead=cfg.get("hideHead", False),
                hidePonytail=cfg.get("hidePonytail", False),
                hideEyes=cfg.get("hideEyes", False),
                hideTeeth=cfg.get("hideTeeth", False),
                hairPieces=hair_pieces,
            ))
        return configs

    def save_main_config(self, configs: List[KerbalConfig]):
        
        data = {"configs": [cfg.to_dict() for cfg in configs]}
        with open(self.config_path, 'w') as f:
            json.dump(data, f, indent=2)

    def load_installed_mods(self) -> Dict[str, dict]:
        
        if not self.installed_mods_path.exists():
            return {}
        with open(self.installed_mods_path, 'r') as f:
            return json.load(f)

    def save_installed_mods(self, mods: Dict[str, dict]):
        
        with open(self.installed_mods_path, 'w') as f:
            json.dump(mods, f, indent=2)

    def get_used_assets(self) -> Dict[str, Set[str]]:
        
        configs = self.load_main_config()
        meshes = set()
        textures = set()
        
        for cfg in configs:
            for hp in cfg.hairPieces:
                meshes.add(hp.meshName)
                textures.add(hp.meshTexture)
        
        return {"meshes": meshes, "textures": textures}

    def check_conflicts(self, mod_info: ModInfo, target_kerbal: Optional[str] = None) -> List[str]:
        
        conflicts = []
        current_configs = self.load_main_config()
        
        for cfg in mod_info.configs:
            kerbal_name = target_kerbal if target_kerbal else cfg.get("kerbalName", "*")
            

            existing = next((c for c in current_configs if c.kerbalName == kerbal_name), None)
            if existing:

                for hp in cfg.get("hairPieces", []):
                    for existing_hp in existing.hairPieces:
                        if existing_hp.meshName == hp.get("meshName"):
                            conflicts.append(f"{kerbal_name} already has {hp.get('meshName')}")
        
        return conflicts

    def parse_pack_json(self, pack_json_path: Path) -> ModInfo:
        
        with open(pack_json_path, 'r') as f:
            data = json.load(f)
        
        return ModInfo(
            name=data.get("name", "Unknown"),
            author=data.get("author", "Unknown"),
            version=data.get("version", "1.0"),
            description=data.get("description", ""),
            configs=data.get("configs", []),
        )

    def install_mod(self, zip_path: str, target_kerbal: Optional[str] = None, 
                    force: bool = False) -> bool:
        
        zip_path = Path(zip_path)
        if not zip_path.exists():
            print(f"❌ Error: File not found: {zip_path}")
            return False


        temp_extract = self.base_path / ".temp_mod_extract"
        if temp_extract.exists():
            shutil.rmtree(temp_extract)
        
        try:
            with zipfile.ZipFile(zip_path, 'r') as zf:
                zf.extractall(temp_extract)
        except zipfile.BadZipFile:
            print(f"❌ Error: Invalid zip file: {zip_path}")
            return False


        pack_json_files = list(temp_extract.rglob("pack.json"))
        if not pack_json_files:
            print("❌ Error: No pack.json found in the archive")
            shutil.rmtree(temp_extract)
            return False

        pack_json_path = pack_json_files[0]
        mod_folder = pack_json_path.parent
        mod_info = self.parse_pack_json(pack_json_path)


        installed_mods = self.load_installed_mods()
        mod_id = f"{mod_info.name}_{mod_info.version}"
        
        if mod_id in installed_mods:
            print(f"⚠️  Mod '{mod_info.name}' v{mod_info.version} is already installed.")
            response = input("   Reinstall? [y/N]: ").strip().lower()
            if response != 'y':
                shutil.rmtree(temp_extract)
                return False

            self.uninstall_mod(mod_info.name, skip_confirm=True)


        conflicts = self.check_conflicts(mod_info, target_kerbal)
        if conflicts and not force:
            print("⚠️  Potential conflicts detected:")
            for c in conflicts:
                print(f"   - {c}")
            response = input("   Install anyway? [y/N]: ").strip().lower()
            if response != 'y':
                shutil.rmtree(temp_extract)
                return False

        print(f"📦 Installing: {mod_info.name} v{mod_info.version}")
        print(f"   Author: {mod_info.author}")
        print(f"   {mod_info.description}")


        model_exts = {'.idx', '.vtx', '.nml', '.tex'}
        copied_models = []
        for file in mod_folder.iterdir():
            if file.suffix.lower() in model_exts:
                dest = self.models_path / file.name

                if not dest.exists():
                    shutil.copy2(file, dest)
                    copied_models.append(file.name)
                    print(f"   📄 Model: {file.name}")
                else:
                    print(f"   📄 Model: {file.name} (already exists, skipping)")
                    copied_models.append(file.name)


        copied_textures = []
        for file in mod_folder.iterdir():
            if file.suffix.lower() == '.png':
                dest = self.textures_path / file.name
                if not dest.exists():
                    shutil.copy2(file, dest)
                    copied_textures.append(file.name)
                    print(f"   🎨 Texture: {file.name}")
                else:
                    print(f"   🎨 Texture: {file.name} (already exists, skipping)")
                    copied_textures.append(file.name)


        current_configs = self.load_main_config()
        
        for cfg in mod_info.configs:
            kerbal_name = target_kerbal if target_kerbal else cfg.get("kerbalName", "*")
            

            existing = next((c for c in current_configs if c.kerbalName == kerbal_name), None)
            if existing is None:
                existing = KerbalConfig(kerbalName=kerbal_name)
                current_configs.append(existing)
            

            if "hideHead" in cfg:
                existing.hideHead = cfg["hideHead"]
            if "hidePonytail" in cfg:
                existing.hidePonytail = cfg["hidePonytail"]
            if "hideEyes" in cfg:
                existing.hideEyes = cfg["hideEyes"]
            if "hideTeeth" in cfg:
                existing.hideTeeth = cfg["hideTeeth"]
            

            for hp in cfg.get("hairPieces", []):
                piece = HairPiece(
                    meshName=hp.get("meshName", ""),
                    meshTexture=hp.get("meshTexture", ""),
                    boneName=hp.get("boneName", "bn_upperJaw01"),
                )
                existing.hairPieces.append(piece)
                print(f"   ➕ Added: {piece.meshName} to {kerbal_name}")

        self.save_main_config(current_configs)


        installed_mods[mod_id] = {
            "name": mod_info.name,
            "version": mod_info.version,
            "author": mod_info.author,
            "description": mod_info.description,
            "models": copied_models,
            "textures": copied_textures,
            "configs": mod_info.configs,
            "target_kerbal": target_kerbal,
        }
        self.save_installed_mods(installed_mods)


        shutil.rmtree(temp_extract)
        
        print(f"✅ Successfully installed '{mod_info.name}'")
        return True

    def uninstall_mod(self, mod_name: str, skip_confirm: bool = False,
                      keep_shared: bool = True) -> bool:
        
        installed_mods = self.load_installed_mods()
        

        mod_id = None
        mod_data = None
        for mid, data in installed_mods.items():
            if data["name"].lower() == mod_name.lower():
                mod_id = mid
                mod_data = data
                break
        
        if mod_id is None:
            print(f"❌ Mod '{mod_name}' not found in installed mods")
            return False

        if not skip_confirm:
            print(f"🗑️  Uninstalling: {mod_data['name']} v{mod_data['version']}")
            if keep_shared:
                print("   (Shared assets will be preserved if still in use)")
            response = input("   Are you sure? [y/N]: ").strip().lower()
            if response != 'y':
                return False


        used_assets = self.get_used_assets() if keep_shared else {"meshes": set(), "textures": set()}
        

        meshes_to_remove = set()
        for cfg in mod_data.get("configs", []):
            for hp in cfg.get("hairPieces", []):
                meshes_to_remove.add(hp.get("meshName"))


        for model in mod_data.get("models", []):
            model_path = self.models_path / model
            if model_path.exists():

                model_base = model.replace('.idx', '').replace('.vtx', '').replace('.nml', '').replace('.tex', '')
                if model_base in used_assets["meshes"] and model_base in meshes_to_remove:
                    print(f"   💾 Keeping model (still in use): {model}")
                else:
                    model_path.unlink()
                    print(f"   🗑️  Removed model: {model}")


        for texture in mod_data.get("textures", []):
            tex_path = self.textures_path / texture
            if tex_path.exists():
                if texture in used_assets["textures"]:
                    print(f"   💾 Keeping texture (still in use): {texture}")
                else:
                    tex_path.unlink()
                    print(f"   🗑️  Removed texture: {texture}")


        current_configs = self.load_main_config()
        target_kerbal = mod_data.get("target_kerbal")
        
        for cfg in mod_data.get("configs", []):
            kerbal_name = target_kerbal if target_kerbal else cfg.get("kerbalName", "*")
            mesh_names = [hp.get("meshName") for hp in cfg.get("hairPieces", [])]
            
            for existing in current_configs:
                if existing.kerbalName == kerbal_name or kerbal_name == "*":

                    original_count = len(existing.hairPieces)
                    existing.hairPieces = [
                        hp for hp in existing.hairPieces 
                        if hp.meshName not in mesh_names
                    ]
                    removed_count = original_count - len(existing.hairPieces)
                    if removed_count > 0:
                        print(f"   🗑️  Removed {removed_count} item(s) from {existing.kerbalName}")


        current_configs = [c for c in current_configs if c.hairPieces]
        self.save_main_config(current_configs)


        del installed_mods[mod_id]
        self.save_installed_mods(installed_mods)

        print(f"✅ Successfully uninstalled '{mod_data['name']}'")
        return True

    def list_mods(self):
        
        installed_mods = self.load_installed_mods()
        
        if not installed_mods:
            print("📭 No mods installed")
            return

        print("📦 Installed Mods:")
        print("-" * 60)
        for mod_id, data in installed_mods.items():
            print(f"  • {data['name']} v{data['version']}")
            print(f"    Author: {data['author']}")
            print(f"    {data['description']}")
            print(f"    Models: {len(data.get('models', []))}")
            print(f"    Textures: {len(data.get('textures', []))}")
            if data.get("target_kerbal"):
                print(f"    Installed for: {data['target_kerbal']} only")
            print()

    def list_kerbals(self):
        
        configs = self.load_main_config()
        
        if not configs:
            print("📭 No kerbal configurations found")
            return

        print("👨‍🚀 Kerbal Configurations:")
        print("-" * 60)
        for cfg in configs:
            print(f"  • {cfg.kerbalName}")
            if cfg.hideHead:
                print("    [Hide Head]")
            if cfg.hidePonytail:
                print("    [Hide Ponytail]")
            if cfg.hideEyes:
                print("    [Hide Eyes]")
            if cfg.hideTeeth:
                print("    [Hide Teeth]")
            
            if cfg.hairPieces:
                print("    Equipped items:")
                for hp in cfg.hairPieces:
                    print(f"      - {hp.meshName} ({hp.meshTexture}) on {hp.boneName}")
            else:
                print("    No items equipped")
            print()

    def edit_kerbal(self, kerbal_name: str):
        
        configs = self.load_main_config()
        

        cfg = next((c for c in configs if c.kerbalName == kerbal_name), None)
        if cfg is None:
            cfg = KerbalConfig(kerbalName=kerbal_name)
            configs.append(cfg)
            print(f"🆕 Creating new configuration for {kerbal_name}")
        else:
            print(f"✏️  Editing {kerbal_name}")

        print("\nCurrent settings:")
        print(f"  1. Hide Head: {cfg.hideHead}")
        print(f"  2. Hide Ponytail: {cfg.hidePonytail}")
        print(f"  3. Hide Eyes: {cfg.hideEyes}")
        print(f"  4. Hide Teeth: {cfg.hideTeeth}")
        print(f"  5. View equipped items")
        print(f"  6. Add item")
        print(f"  7. Remove item")
        print(f"  8. Save and exit")
        print(f"  9. Cancel")

        while True:
            choice = input("\nSelect option: ").strip()
            
            if choice == "1":
                cfg.hideHead = not cfg.hideHead
                print(f"   Hide Head: {cfg.hideHead}")
            elif choice == "2":
                cfg.hidePonytail = not cfg.hidePonytail
                print(f"   Hide Ponytail: {cfg.hidePonytail}")
            elif choice == "3":
                cfg.hideEyes = not cfg.hideEyes
                print(f"   Hide Eyes: {cfg.hideEyes}")
            elif choice == "4":
                cfg.hideTeeth = not cfg.hideTeeth
                print(f"   Hide Teeth: {cfg.hideTeeth}")
            elif choice == "5":
                if cfg.hairPieces:
                    for i, hp in enumerate(cfg.hairPieces):
                        print(f"   {i}: {hp.meshName} ({hp.meshTexture}) on {hp.boneName}")
                else:
                    print("   No items equipped")
            elif choice == "6":
                mesh = input("   Mesh name (without extension): ").strip()

                print("   Available textures:")
                textures = sorted([f.name for f in self.textures_path.iterdir() if f.suffix == '.png'])
                for i, tex in enumerate(textures[:10]):
                    print(f"      {tex}")
                if len(textures) > 10:
                    print(f"      ... and {len(textures) - 10} more")
                
                texture = input("   Texture filename: ").strip()
                print("   Common bones:")
                for i, bone in enumerate(self.BONES[:5]):
                    print(f"      {bone}")
                bone = input("   Bone name [bn_upperJaw01]: ").strip() or "bn_upperJaw01"
                cfg.hairPieces.append(HairPiece(meshName=mesh, meshTexture=texture, boneName=bone))
                print(f"   Added {mesh}")
            elif choice == "7":
                if cfg.hairPieces:
                    for i, hp in enumerate(cfg.hairPieces):
                        print(f"   {i}: {hp.meshName}")
                    idx = input("   Index to remove: ").strip()
                    try:
                        removed = cfg.hairPieces.pop(int(idx))
                        print(f"   Removed {removed.meshName}")
                    except (ValueError, IndexError):
                        print("   Invalid index")
                else:
                    print("   No items to remove")
            elif choice == "8":
                self.save_main_config(configs)
                print("💾 Saved!")
                break
            elif choice == "9":
                print("❌ Cancelled")
                break

    def scan_available_mods(self):
        
        if not self.packed_mods_path.exists():
            print("📭 No packed mods folder found")
            return []

        available = []
        for file in self.packed_mods_path.iterdir():
            if file.suffix.lower() == '.zip':
                try:
                    with zipfile.ZipFile(file, 'r') as zf:

                        pack_files = [n for n in zf.namelist() if n.endswith('pack.json')]
                        if pack_files:
                            pack_data = json.loads(zf.read(pack_files[0]))
                            available.append({
                                "file": file.name,
                                "name": pack_data.get("name", "Unknown"),
                                "version": pack_data.get("version", "1.0"),
                                "description": pack_data.get("description", ""),
                                "author": pack_data.get("author", "Unknown"),
                            })
                except Exception as e:
                    print(f"⚠️  Error reading {file.name}: {e}")
        
        return available

    def list_available(self):
        
        available = self.scan_available_mods()
        
        if not available:
            print("📭 No packed mods found in 'packed mods/' folder")
            return

        print("📦 Available Mods:")
        print("-" * 60)
        for mod in available:
            print(f"  • {mod['name']} v{mod['version']}")
            print(f"    File: {mod['file']}")
            print(f"    Author: {mod['author']}")
            print(f"    {mod['description']}")
            print()

    def create_mod_template(self, name: str, output_dir: str = "packed mods"):
        
        template = {
            "name": name,
            "author": "Your Name",
            "version": "1.0",
            "description": "Description of your mod",
            "configs": [
                {
                    "kerbalName": "*",
                    "hideHead": False,
                    "hidePonytail": False,
                    "hideEyes": False,
                    "hideTeeth": False,
                    "hairPieces": [
                        {
                            "meshName": "YourMeshName",
                            "meshTexture": "your_texture.png",
                            "boneName": "bn_upperJaw01"
                        }
                    ]
                }
            ]
        }
        
        output_path = self.base_path / output_dir / f"{name.replace(' ', '_')}_template"
        output_path.mkdir(exist_ok=True)
        
        pack_json_path = output_path / "pack.json"
        with open(pack_json_path, 'w') as f:
            json.dump(template, f, indent=2)
        

        readme_path = output_path / "README.txt"
        readme_content = f
        for bone in self.BONES:
            readme_content += f"  - {bone}\n"
        
        with open(readme_path, 'w') as f:
            f.write(readme_content)
        
        print(f"📝 Created mod template at: {output_path}")
        print(f"   Edit pack.json and add your model/texture files, then zip and install!")


def main():
    parser = argparse.ArgumentParser(
        description="KerbonautRedux Mod Manager - Manage KSP kerbal accessories",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=
    )
    
    subparsers = parser.add_subparsers(dest="command", help="Commands")


    subparsers.add_parser("list", help="List installed mods")


    subparsers.add_parser("available", help="List available mods in packed mods folder")


    install_parser = subparsers.add_parser("install", help="Install a mod from zip file")
    install_parser.add_argument("zipfile", help="Path to the mod zip file")
    install_parser.add_argument("--kerbal", help="Install only for specific kerbal")
    install_parser.add_argument("--force", action="store_true", help="Force install even with conflicts")


    uninstall_parser = subparsers.add_parser("uninstall", help="Uninstall a mod")
    uninstall_parser.add_argument("name", help="Name of the mod to uninstall")


    subparsers.add_parser("kerbals", help="List all kerbal configurations")


    edit_parser = subparsers.add_parser("edit", help="Edit a kerbal's configuration")
    edit_parser.add_argument("name", help="Kerbal name")


    create_parser = subparsers.add_parser("create", help="Create a new mod template")
    create_parser.add_argument("name", help="Name for the new mod")

    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        return

    manager = KerbonautReduxManager()

    if args.command == "list":
        manager.list_mods()
    elif args.command == "available":
        manager.list_available()
    elif args.command == "install":
        manager.install_mod(args.zipfile, args.kerbal, args.force)
    elif args.command == "uninstall":
        manager.uninstall_mod(args.name)
    elif args.command == "kerbals":
        manager.list_kerbals()
    elif args.command == "edit":
        manager.edit_kerbal(args.name)
    elif args.command == "create":
        manager.create_mod_template(args.name)


if __name__ == "__main__":
    main()
