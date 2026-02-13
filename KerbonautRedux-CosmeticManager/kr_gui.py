#!/usr/bin/env python3


import os
import json
import shutil
import zipfile
import tkinter as tk
from tkinter import ttk, messagebox, filedialog, simpledialog
from pathlib import Path
from dataclasses import dataclass, field
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


class KerbonautManager:

    BONES = [

        "bn_upperJaw01",
        "bn_lowerJaw01", 
        "bn_neck01",
        "bn_headPivot_a01",
        "bn_headPivot_b01",
        "bn_helmet01",
        

        "bn_upperTeet01",
        "bn_lowerTeeth01",
        "bn_l_mouthCorner01",
        "bn_r_mouthCorner01",
        "bn_l_mouthUpMid_a01",
        "bn_l_mouthUp_b01",
        "bn_l_mouthUp_c01",
        "bn_l_mouthUp_d01",
        "bn_r_mouthUp_b01",
        "bn_r_mouthUp_c01",
        "bn_r_mouthUp_d01",
        "bn_l_mouthLowMid_a01",
        "bn_l_mouthLow_b01",
        "bn_l_mouthLow_c01",
        "bn_l_mouthLow_d01",
        "bn_r_mouthLow_b01",
        "bn_r_mouthLow_c01",
        "bn_r_mouthLow_d01",
        "bn_tongue_A01",
        "bn_tongue_B01",
        "bn_tongue_C01",
        "bn_tongue_D01",
        

        "bn_spA01",
        "bn_spB01",
        "bn_spc01",
        "bn_spD01",
        

        "bn_l_shld01",
        "bn_l_arm01",
        "bn_l_elbow_a01",
        "bn_l_elbow_b01",
        "bn_l_wrist01",
        "bn_l_armEnd01",
        "bn_l_handle01",
        

        "bn_l_thumb_a01",
        "bn_l_thumb_b01",
        "bn_l_thumb_c01",
        "bn_l_index_a01",
        "bn_l_index_b01",
        "bn_l_mid_a01",
        "bn_l_mid_b01",
        "bn_l_pinky_a01",
        "bn_l_pinky_b01",
        

        "bn_r_shld01",
        "bn_r_arm01",
        "bn_r_elbow_a01",
        "bn_r_elbow_b01",
        "bn_r_wrist01",
        "bn_r_armEnd01",
        "bn_r_handle01",
        

        "bn_r_thumb_a01",
        "bn_r_thumb_b01",
        "bn_r_thumb_c01",
        "bn_r_index_a01",
        "bn_r_index_b01",
        "bn_r_mid_a01",
        "bn_r_mid_b01",
        "bn_r_pinky_a01",
        "bn_r_pinky_b01",
        

        "bn_l_hip01",
        "bn_l_knee_a01",
        "bn_l_knee_b01",
        "bn_l_foot01",
        "bn_l_ball01",
        "bn_r_hip01",
        "bn_r_knee_a01",
        "bn_r_knee_b01",
        "bn_r_foot01",
        "bn_r_ball01",
    ]

    def __init__(self, base_path: str = "."):
        self.base_path = Path(base_path).resolve()
        self.textures_path = self.base_path / "Textures"
        self.models_path = self.base_path / "Models"
        self.config_path = self.base_path / "KerbonautRedux.json"
        self.packed_mods_path = self.base_path / "packed mods"
        self.installed_mods_path = self.base_path / ".installed_mods.json"
        self.icon_path = self.base_path / "icon.ico"
        self.png_icon_path = self.base_path / "icon.png"
        
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

    def scan_available_mods(self) -> List[dict]:
        if not self.packed_mods_path.exists():
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
                                "file": str(file),
                                "filename": file.name,
                                "name": pack_data.get("name", "Unknown"),
                                "version": pack_data.get("version", "1.0"),
                                "description": pack_data.get("description", ""),
                                "author": pack_data.get("author", "Unknown"),
                            })
                except Exception:
                    pass
        return available

    def get_available_items(self) -> List[dict]:
        
        items = []
        installed = self.load_installed_mods()
        
        for mod_id, mod_data in installed.items():
            for cfg in mod_data.get("configs", []):
                for hp in cfg.get("hairPieces", []):
                    items.append({
                        "mesh": hp.get("meshName", ""),
                        "texture": hp.get("meshTexture", ""),
                        "bone": hp.get("boneName", "bn_upperJaw01"),
                        "mod": mod_data.get("name", "Unknown"),
                    })
        return items

    def get_available_textures(self) -> List[str]:
        
        if not self.textures_path.exists():
            return []
        return sorted([f.name for f in self.textures_path.iterdir() if f.suffix == '.png'])

    def get_textures_for_item(self, mesh_name: str) -> List[str]:
        
        if not mesh_name:
            return []
        
        all_textures = self.get_available_textures()
        mesh_prefix = mesh_name.lower()
        

        variants = []
        for tex in all_textures:
            tex_lower = tex.lower()

            if tex_lower.startswith(mesh_prefix):
                variants.append(tex)
        

        return variants

    def get_textures_by_mod(self) -> Dict[str, List[str]]:
        
        result = {"(Unknown)": []}
        installed = self.load_installed_mods()
        

        texture_to_mod = {}
        for mod_id, mod_data in installed.items():
            mod_name = mod_data.get("name", "Unknown")
            for tex in mod_data.get("textures", []):
                texture_to_mod[tex] = mod_name
        

        all_textures = self.get_available_textures()
        for tex in all_textures:
            mod = texture_to_mod.get(tex, "(Other)")
            if mod not in result:
                result[mod] = []
            result[mod].append(tex)
        
        return result

    def get_mod_for_item(self, mesh_name: str) -> str:
        
        installed = self.load_installed_mods()
        for mod_id, mod_data in installed.items():
            for cfg in mod_data.get("configs", []):
                for hp in cfg.get("hairPieces", []):
                    if hp.get("meshName") == mesh_name:
                        return mod_data.get("name", "Unknown")
        return "(Unknown)"

    def get_available_meshes(self) -> List[str]:
        
        if not self.models_path.exists():
            return []
        meshes = set()
        for f in self.models_path.iterdir():
            if f.suffix.lower() == '.idx':
                meshes.add(f.stem)
        return sorted(list(meshes))

    def install_mod(self, zip_path: str) -> tuple:
        
        zip_path = Path(zip_path)
        if not zip_path.exists():
            return False, "File not found"

        temp_extract = self.base_path / ".temp_mod_extract"
        if temp_extract.exists():
            shutil.rmtree(temp_extract)
        
        try:
            with zipfile.ZipFile(zip_path, 'r') as zf:
                zf.extractall(temp_extract)
        except zipfile.BadZipFile:
            return False, "Invalid zip file"

        pack_json_files = list(temp_extract.rglob("pack.json"))
        if not pack_json_files:
            shutil.rmtree(temp_extract)
            return False, "No pack.json found in archive"

        pack_json_path = pack_json_files[0]
        mod_folder = pack_json_path.parent
        
        with open(pack_json_path, 'r') as f:
            pack_data = json.load(f)
        
        mod_name = pack_data.get("name", "Unknown")
        mod_version = pack_data.get("version", "1.0")
        mod_id = f"{mod_name}_{mod_version}"
        
        installed_mods = self.load_installed_mods()
        if mod_id in installed_mods:
            shutil.rmtree(temp_extract)
            return False, f"'{mod_name}' v{mod_version} is already installed"


        model_exts = {'.idx', '.vtx', '.nml', '.tex'}
        copied_models = []
        for file in mod_folder.iterdir():
            if file.suffix.lower() in model_exts:
                dest = self.models_path / file.name
                if not dest.exists():
                    shutil.copy2(file, dest)
                copied_models.append(file.name)


        copied_textures = []
        for file in mod_folder.iterdir():
            if file.suffix.lower() == '.png':
                dest = self.textures_path / file.name
                if not dest.exists():
                    shutil.copy2(file, dest)
                copied_textures.append(file.name)


        installed_mods[mod_id] = {
            "name": mod_name,
            "version": mod_version,
            "author": pack_data.get("author", "Unknown"),
            "description": pack_data.get("description", ""),
            "models": copied_models,
            "textures": copied_textures,
            "configs": pack_data.get("configs", []),
        }
        self.save_installed_mods(installed_mods)

        shutil.rmtree(temp_extract)
        return True, f"Installed '{mod_name}' v{mod_version}"

    def uninstall_mod(self, mod_name: str) -> tuple:
        
        installed_mods = self.load_installed_mods()
        
        mod_id = None
        mod_data = None
        for mid, data in installed_mods.items():
            if data["name"].lower() == mod_name.lower():
                mod_id = mid
                mod_data = data
                break
        
        if mod_id is None:
            return False, f"Mod '{mod_name}' not found"


        configs = self.load_main_config()
        used_meshes = set()
        used_textures = set()
        for cfg in configs:
            for hp in cfg.hairPieces:
                used_meshes.add(hp.meshName)
                used_textures.add(hp.meshTexture)


        mod_meshes = set()
        for cfg in mod_data.get("configs", []):
            for hp in cfg.get("hairPieces", []):
                mod_meshes.add(hp.get("meshName"))


        for model in mod_data.get("models", []):
            model_path = self.models_path / model
            if model_path.exists():
                base_name = model_path.stem

                if base_name not in used_meshes or base_name in mod_meshes:
                    try:
                        model_path.unlink()
                    except:
                        pass


        for texture in mod_data.get("textures", []):
            tex_path = self.textures_path / texture
            if tex_path.exists():
                if texture not in used_textures:
                    try:
                        tex_path.unlink()
                    except:
                        pass


        del installed_mods[mod_id]
        self.save_installed_mods(installed_mods)

        return True, f"Uninstalled '{mod_data['name']}'"


class KerbonautGUI:
    def __init__(self, root):
        self.root = root
        self.root.title("KerbonautRedux Cosmetic Manager")
        self.root.geometry("900x650")
        self.root.minsize(800, 600)
        
        self.manager = KerbonautManager()
        

        self.set_icon()
        

        self.style = ttk.Style()
        self.style.theme_use('clam')
        self.style.configure('Title.TLabel', font=('Helvetica', 14, 'bold'))
        self.style.configure('Header.TLabel', font=('Helvetica', 11, 'bold'))
        
        self.create_widgets()
        self.refresh_all()
    
    def set_icon(self):
        
        try:

            if self.manager.icon_path.exists():
                self.root.iconbitmap(str(self.manager.icon_path))

            elif self.manager.png_icon_path.exists():
                from PIL import Image, ImageTk
                img = Image.open(self.manager.png_icon_path)
                photo = ImageTk.PhotoImage(img)
                self.root.iconphoto(True, photo)
        except Exception:
            pass
    
    def open_website(self, event=None):
        
        import webbrowser
        webbrowser.open("https://onge.org")
    
    def create_widgets(self):

        self.main_frame = ttk.Frame(self.root, padding="10")
        self.main_frame.grid(row=0, column=0, sticky="nsew")
        
        self.root.columnconfigure(0, weight=1)
        self.root.rowconfigure(0, weight=1)
        self.main_frame.columnconfigure(0, weight=1)
        self.main_frame.rowconfigure(1, weight=1)
        

        title = ttk.Label(self.main_frame, text="🚀 KerbonautRedux Cosmetic Manager", 
                         style='Title.TLabel')
        title.grid(row=0, column=0, sticky="w", pady=(0, 10))
        

        self.notebook = ttk.Notebook(self.main_frame)
        self.notebook.grid(row=1, column=0, sticky="nsew")
        

        self.mods_tab = ttk.Frame(self.notebook, padding="10")
        self.notebook.add(self.mods_tab, text="📦 Mods")
        self.setup_mods_tab()
        

        self.kerbal_tab = ttk.Frame(self.notebook, padding="10")
        self.notebook.add(self.kerbal_tab, text="👨‍🚀 Kerbals")
        self.setup_kerbal_tab()
        

        bottom_frame = ttk.Frame(self.main_frame)
        bottom_frame.grid(row=2, column=0, sticky="ew", pady=(10, 0))
        bottom_frame.columnconfigure(0, weight=1)
        

        self.status_var = tk.StringVar(value="Ready")
        status_bar = ttk.Label(bottom_frame, textvariable=self.status_var, 
                              relief=tk.SUNKEN, anchor=tk.W)
        status_bar.grid(row=0, column=0, sticky="ew")
        

        link_label = tk.Label(bottom_frame, text="created by onge.org", 
                             fg="blue", cursor="hand2", font=('Segoe UI', 8, 'underline'))
        link_label.grid(row=0, column=1, sticky="e", padx=(10, 0))
        link_label.bind("<Button-1>", self.open_website)
        link_label.bind("<Enter>", lambda e: link_label.config(fg="purple"))
        link_label.bind("<Leave>", lambda e: link_label.config(fg="blue"))
    
    def setup_mods_tab(self):
        self.mods_tab.columnconfigure(0, weight=1)
        self.mods_tab.rowconfigure(0, weight=1)
        

        paned = ttk.PanedWindow(self.mods_tab, orient=tk.HORIZONTAL)
        paned.grid(row=0, column=0, sticky="nsew")
        

        available_frame = ttk.LabelFrame(paned, text="Available Mods", padding="5")
        paned.add(available_frame, weight=1)
        
        available_frame.columnconfigure(0, weight=1)
        available_frame.rowconfigure(0, weight=1)
        

        columns = ("name", "version", "author")
        self.available_tree = ttk.Treeview(available_frame, columns=columns, 
                                          show="headings", height=10)
        self.available_tree.heading("name", text="Mod Name")
        self.available_tree.heading("version", text="Version")
        self.available_tree.heading("author", text="Author")
        self.available_tree.column("name", width=150)
        self.available_tree.column("version", width=60)
        self.available_tree.column("author", width=100)
        
        scrollbar = ttk.Scrollbar(available_frame, orient=tk.VERTICAL, 
                                 command=self.available_tree.yview)
        self.available_tree.configure(yscrollcommand=scrollbar.set)
        
        self.available_tree.grid(row=0, column=0, sticky="nsew")
        scrollbar.grid(row=0, column=1, sticky="ns")
        

        btn_frame = ttk.Frame(available_frame)
        btn_frame.grid(row=1, column=0, columnspan=2, pady=(5, 0))
        ttk.Button(btn_frame, text="🔄 Refresh", 
                  command=self.refresh_available).pack(side=tk.LEFT, padx=2)
        ttk.Button(btn_frame, text="📂 Install from File...", 
                  command=self.install_from_file).pack(side=tk.LEFT, padx=2)
        ttk.Button(btn_frame, text="⬇️ Install Selected", 
                  command=self.install_selected).pack(side=tk.LEFT, padx=2)
        

        installed_frame = ttk.LabelFrame(paned, text="Installed Mods", padding="5")
        paned.add(installed_frame, weight=1)
        
        installed_frame.columnconfigure(0, weight=1)
        installed_frame.rowconfigure(0, weight=1)
        

        columns = ("name", "version")
        self.installed_tree = ttk.Treeview(installed_frame, columns=columns, 
                                          show="headings", height=10)
        self.installed_tree.heading("name", text="Mod Name")
        self.installed_tree.heading("version", text="Version")
        self.installed_tree.column("name", width=200)
        self.installed_tree.column("version", width=60)
        
        scrollbar2 = ttk.Scrollbar(installed_frame, orient=tk.VERTICAL, 
                                  command=self.installed_tree.yview)
        self.installed_tree.configure(yscrollcommand=scrollbar2.set)
        
        self.installed_tree.grid(row=0, column=0, sticky="nsew")
        scrollbar2.grid(row=0, column=1, sticky="ns")
        

        btn_frame2 = ttk.Frame(installed_frame)
        btn_frame2.grid(row=1, column=0, columnspan=2, pady=(5, 0))
        ttk.Button(btn_frame2, text="🔄 Refresh", 
                  command=self.refresh_installed).pack(side=tk.LEFT, padx=2)
        ttk.Button(btn_frame2, text="🗑️ Uninstall Selected", 
                  command=self.uninstall_selected).pack(side=tk.LEFT, padx=2)
    
    def setup_kerbal_tab(self):
        self.kerbal_tab.columnconfigure(0, weight=1)
        self.kerbal_tab.rowconfigure(0, weight=1)
        

        paned = ttk.PanedWindow(self.kerbal_tab, orient=tk.HORIZONTAL)
        paned.grid(row=0, column=0, sticky="nsew")
        

        list_frame = ttk.LabelFrame(paned, text="Kerbals", padding="5")
        paned.add(list_frame, weight=1)
        
        self.kerbal_listbox = tk.Listbox(list_frame, height=20, font=('Segoe UI', 11))
        self.kerbal_listbox.pack(fill=tk.BOTH, expand=True)
        self.kerbal_listbox.bind('<<ListboxSelect>>', self.on_kerbal_select)
        
        scrollbar = ttk.Scrollbar(self.kerbal_listbox, command=self.kerbal_listbox.yview)
        self.kerbal_listbox.config(yscrollcommand=scrollbar.set)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        

        btn_frame = ttk.Frame(list_frame)
        btn_frame.pack(fill=tk.X, pady=(5, 0))
        ttk.Button(btn_frame, text="➕ Add", 
                  command=self.add_kerbal).pack(side=tk.LEFT, padx=2)
        ttk.Button(btn_frame, text="✏️ Rename", 
                  command=self.rename_kerbal).pack(side=tk.LEFT, padx=2)
        ttk.Button(btn_frame, text="🗑️ Delete", 
                  command=self.delete_kerbal).pack(side=tk.LEFT, padx=2)
        

        details_frame = ttk.LabelFrame(paned, text="Kerbal Configuration", padding="10")
        paned.add(details_frame, weight=2)
        
        details_frame.columnconfigure(0, weight=1)
        details_frame.rowconfigure(2, weight=1)
        

        toggles_frame = ttk.LabelFrame(details_frame, text="Visibility Options", padding="10")
        toggles_frame.grid(row=0, column=0, sticky="ew", pady=(0, 10))
        
        self.hide_vars = {}
        for i, (key, label) in enumerate([
            ("hideHead", "Hide Head"),
            ("hidePonytail", "Hide Ponytail"),
            ("hideEyes", "Hide Eyes"),
            ("hideTeeth", "Hide Teeth"),
        ]):
            var = tk.BooleanVar(value=False)
            self.hide_vars[key] = var
            cb = ttk.Checkbutton(toggles_frame, text=label, variable=var,
                                command=self.on_hide_toggle)
            cb.grid(row=i//2, column=i%2, sticky="w", padx=10)
        

        equip_frame = ttk.LabelFrame(details_frame, text="Equipped Items", padding="5")
        equip_frame.grid(row=1, column=0, sticky="nsew", pady=(0, 10))
        equip_frame.columnconfigure(0, weight=1)
        equip_frame.rowconfigure(0, weight=1)
        
        self.equip_tree = ttk.Treeview(equip_frame, columns=("mesh", "texture", "bone"),
                                       show="headings", height=8)
        self.equip_tree.heading("mesh", text="Mesh (Item)")
        self.equip_tree.heading("texture", text="Texture")
        self.equip_tree.heading("bone", text="Bone")
        self.equip_tree.column("mesh", width=150)
        self.equip_tree.column("texture", width=180)
        self.equip_tree.column("bone", width=120)
        
        self.equip_tree.grid(row=0, column=0, sticky="nsew")
        
        equip_scroll = ttk.Scrollbar(equip_frame, orient=tk.VERTICAL, 
                                    command=self.equip_tree.yview)
        self.equip_tree.configure(yscrollcommand=equip_scroll.set)
        equip_scroll.grid(row=0, column=1, sticky="ns")
        

        equip_btns = ttk.Frame(details_frame)
        equip_btns.grid(row=2, column=0, sticky="ew")
        
        ttk.Button(equip_btns, text="➕ Add Item from Mods", 
                  command=self.add_item_dialog).pack(side=tk.LEFT, padx=2)
        ttk.Button(equip_btns, text="🗑️ Remove Item", 
                  command=self.remove_item).pack(side=tk.LEFT, padx=2)
        

        self.equip_tree.bind('<Double-1>', self.on_equip_edit)
        

        hint = ttk.Label(details_frame, text="💡 Double-click Texture or Bone to edit", 
                        foreground="gray", font=('Segoe UI', 8))
        hint.grid(row=3, column=0, sticky="w", pady=(5, 0))
        
        self.current_kerbal = None
        self.kerbal_configs = {}
        self._edit_widget = None
    
    def refresh_all(self):
        self.refresh_available()
        self.refresh_installed()
        self.refresh_kerbals()
    
    def refresh_available(self):
        for item in self.available_tree.get_children():
            self.available_tree.delete(item)
        
        mods = self.manager.scan_available_mods()
        for mod in mods:
            self.available_tree.insert("", tk.END, values=(
                mod["name"],
                mod["version"],
                mod["author"],
            ), tags=(mod["file"],))
        
        self.status_var.set(f"Found {len(mods)} available mods")
    
    def refresh_installed(self):
        for item in self.installed_tree.get_children():
            self.installed_tree.delete(item)
        
        mods = self.manager.load_installed_mods()
        for mod_id, data in mods.items():
            self.installed_tree.insert("", tk.END, values=(
                data["name"],
                data["version"],
            ), tags=(data["name"],))
        
    def refresh_kerbals(self):
        self.kerbal_listbox.delete(0, tk.END)
        self.kerbal_configs = {}
        
        configs = self.manager.load_main_config()
        for cfg in configs:
            self.kerbal_listbox.insert(tk.END, cfg.kerbalName)
            self.kerbal_configs[cfg.kerbalName] = cfg
        
        self.clear_kerbal_details()
    
    def clear_kerbal_details(self):
        self.current_kerbal = None
        for var in self.hide_vars.values():
            var.set(False)
        for item in self.equip_tree.get_children():
            self.equip_tree.delete(item)
    
    def on_kerbal_select(self, event):
        selection = self.kerbal_listbox.curselection()
        if not selection:
            return
        
        name = self.kerbal_listbox.get(selection[0])
        self.current_kerbal = name
        cfg = self.kerbal_configs.get(name)
        
        if cfg:
            self.hide_vars["hideHead"].set(cfg.hideHead)
            self.hide_vars["hidePonytail"].set(cfg.hidePonytail)
            self.hide_vars["hideEyes"].set(cfg.hideEyes)
            self.hide_vars["hideTeeth"].set(cfg.hideTeeth)
            
            for item in self.equip_tree.get_children():
                self.equip_tree.delete(item)
            
            for hp in cfg.hairPieces:
                self.equip_tree.insert("", tk.END, values=(
                    hp.meshName, hp.meshTexture, hp.boneName
                ))
    
    def on_hide_toggle(self):
        self.save_kerbal_changes()
    
    def save_kerbal_changes(self):
        if not self.current_kerbal:
            return
        
        cfg = self.kerbal_configs.get(self.current_kerbal)
        if cfg:
            cfg.hideHead = self.hide_vars["hideHead"].get()
            cfg.hidePonytail = self.hide_vars["hidePonytail"].get()
            cfg.hideEyes = self.hide_vars["hideEyes"].get()
            cfg.hideTeeth = self.hide_vars["hideTeeth"].get()
            
            all_configs = list(self.kerbal_configs.values())
            self.manager.save_main_config(all_configs)
    
    def add_kerbal(self):
        name = simpledialog.askstring("Add Kerbal", "Enter kerbal name:")
        if name:
            name = name.strip()
            if not name:
                messagebox.showwarning("Invalid Name", "Name cannot be empty")
                return
            if name in self.kerbal_configs:
                messagebox.showwarning("Duplicate", f"'{name}' already exists")
                return
            
            new_cfg = KerbalConfig(kerbalName=name)
            self.kerbal_configs[name] = new_cfg
            self.kerbal_listbox.insert(tk.END, name)
            
            all_configs = list(self.kerbal_configs.values())
            self.manager.save_main_config(all_configs)
            
            self.status_var.set(f"Added {name}")
    
    def rename_kerbal(self):
        if not self.current_kerbal:
            messagebox.showwarning("No Selection", "Select a kerbal to rename")
            return
        
        old_name = self.current_kerbal
        new_name = simpledialog.askstring("Rename Kerbal", 
                                          f"Rename '{old_name}' to:",
                                          initialvalue=old_name)
        if new_name:
            new_name = new_name.strip()
            if not new_name or new_name == old_name or new_name in self.kerbal_configs:
                return
            
            cfg = self.kerbal_configs[old_name]
            cfg.kerbalName = new_name
            del self.kerbal_configs[old_name]
            self.kerbal_configs[new_name] = cfg
            
            selection = self.kerbal_listbox.curselection()
            if selection:
                self.kerbal_listbox.delete(selection[0])
                self.kerbal_listbox.insert(selection[0], new_name)
            
            self.current_kerbal = new_name
            all_configs = list(self.kerbal_configs.values())
            self.manager.save_main_config(all_configs)
            self.status_var.set(f"Renamed to '{new_name}'")
    
    def delete_kerbal(self):
        if not self.current_kerbal:
            messagebox.showwarning("No Selection", "Select a kerbal to delete")
            return
        
        name = self.current_kerbal
        if messagebox.askyesno("Confirm", f"Delete '{name}'?"):
            del self.kerbal_configs[name]
            
            selection = self.kerbal_listbox.curselection()
            if selection:
                self.kerbal_listbox.delete(selection[0])
            
            self.clear_kerbal_details()
            all_configs = list(self.kerbal_configs.values())
            self.manager.save_main_config(all_configs)
            self.status_var.set(f"Deleted '{name}'")
    
    def add_item_dialog(self):
        
        if not self.current_kerbal:
            messagebox.showwarning("No Selection", "Select a kerbal first")
            return
        

        items = self.manager.get_available_items()
        if not items:
            messagebox.showinfo("No Items", "No items available. Install some mods first!")
            return
        
        dialog = tk.Toplevel(self.root)
        dialog.title(f"Add Item to {self.current_kerbal}")
        dialog.geometry("500x400")
        dialog.transient(self.root)
        dialog.grab_set()
        
        ttk.Label(dialog, text="Select an item from installed mods:").pack(pady=5)
        

        columns = ("mesh", "texture", "mod")
        tree = ttk.Treeview(dialog, columns=columns, show="headings", height=12)
        tree.heading("mesh", text="Item (Mesh)")
        tree.heading("texture", text="Texture")
        tree.heading("mod", text="From Mod")
        tree.column("mesh", width=150)
        tree.column("texture", width=150)
        tree.column("mod", width=120)
        

        scrollbar = ttk.Scrollbar(dialog, orient=tk.VERTICAL, command=tree.yview)
        tree.configure(yscrollcommand=scrollbar.set)
        
        tree.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=(10, 0), pady=5)
        scrollbar.pack(side=tk.LEFT, fill=tk.Y, pady=5)
        

        for item in items:
            tree.insert("", tk.END, values=(
                item["mesh"],
                item["texture"],
                item["mod"]
            ))
        

        ttk.Label(dialog, text="Attach to bone:").pack(pady=(10, 0))
        bone_var = tk.StringVar(value="bn_upperJaw01")
        bone_combo = ttk.Combobox(dialog, textvariable=bone_var, 
                                  values=self.manager.BONES, width=30)
        bone_combo.pack(pady=5)
        
        def do_add():
            selection = tree.selection()
            if not selection:
                messagebox.showwarning("No Selection", "Select an item")
                return
            
            values = tree.item(selection[0], "values")
            mesh, texture = values[0], values[1]
            bone = bone_var.get()
            
            cfg = self.kerbal_configs.get(self.current_kerbal)
            if cfg:

                for hp in cfg.hairPieces:
                    if hp.meshName == mesh:
                        messagebox.showwarning("Duplicate", 
                            f"{self.current_kerbal} already has '{mesh}' equipped")
                        return
                
                cfg.hairPieces.append(HairPiece(mesh, texture, bone))
                self.equip_tree.insert("", tk.END, values=(mesh, texture, bone))
                
                all_configs = list(self.kerbal_configs.values())
                self.manager.save_main_config(all_configs)
                
                self.status_var.set(f"Added {mesh} to {self.current_kerbal}")
                dialog.destroy()
        
        ttk.Button(dialog, text="Add Item", command=do_add).pack(pady=10)
    
    def change_texture(self):
        
        selection = self.equip_tree.selection()
        if not selection or not self.current_kerbal:
            return
        
        idx = self.equip_tree.index(selection[0])
        cfg = self.kerbal_configs.get(self.current_kerbal)
        
        if not cfg or idx >= len(cfg.hairPieces):
            return
        
        piece = cfg.hairPieces[idx]
        

        variants = self.manager.get_textures_for_item(piece.meshName)
        

        source_mod = self.manager.get_mod_for_item(piece.meshName)
        
        dialog = tk.Toplevel(self.root)
        dialog.title(f"Change Texture - {piece.meshName}")
        dialog.geometry("350x350")
        dialog.transient(self.root)
        dialog.grab_set()
        
        ttk.Label(dialog, text=f"Item: {piece.meshName}", font=('Helvetica', 10, 'bold')).pack(pady=5)
        ttk.Label(dialog, text=f"From mod: {source_mod}", fg="gray").pack(pady=(0, 5))
        

        current_frame = ttk.Frame(dialog)
        current_frame.pack(fill=tk.X, padx=10, pady=5)
        ttk.Label(current_frame, text="Current:", font=('Helvetica', 9)).pack(side=tk.LEFT)
        ttk.Label(current_frame, text=piece.meshTexture, font=('Helvetica', 9, 'bold')).pack(side=tk.LEFT, padx=(5, 0))
        
        ttk.Label(dialog, text="Available variants:").pack(pady=(10, 5))
        

        frame = ttk.Frame(dialog)
        frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)
        
        if not variants:

            ttk.Label(frame, text="No texture variants found for this item.", 
                     foreground="gray").pack(pady=20)
            ttk.Label(frame, text=f"Current: {piece.meshTexture}").pack()
            ttk.Button(dialog, text="Close", command=dialog.destroy).pack(pady=10)
            return
        
        scrollbar = ttk.Scrollbar(frame)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        
        listbox = tk.Listbox(frame, yscrollcommand=scrollbar.set, width=40, height=10)
        listbox.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar.config(command=listbox.yview)
        

        for tex in variants:
            listbox.insert(tk.END, tex)
            if tex == piece.meshTexture:
                listbox.selection_set(tk.END)
                listbox.see(tk.END)
        
        def do_change():
            sel = listbox.curselection()
            if not sel:
                return
            
            new_tex = listbox.get(sel[0])
            piece.meshTexture = new_tex
            self.equip_tree.item(selection[0], values=(
                piece.meshName, piece.meshTexture, piece.boneName
            ))
            all_configs = list(self.kerbal_configs.values())
            self.manager.save_main_config(all_configs)
            self.status_var.set(f"Changed texture to {new_tex}")
            dialog.destroy()
        
        ttk.Button(dialog, text="Change Texture", command=do_change).pack(pady=10)
    
    def on_equip_edit(self, event):
        

        if self._edit_widget:
            self._edit_widget.destroy()
            self._edit_widget = None
        
        if not self.current_kerbal:
            return
        

        row_id = self.equip_tree.identify_row(event.y)
        column = self.equip_tree.identify_column(event.x)
        
        if not row_id:
            return
        

        idx = self.equip_tree.index(row_id)
        cfg = self.kerbal_configs.get(self.current_kerbal)
        
        if not cfg or idx >= len(cfg.hairPieces):
            return
        
        piece = cfg.hairPieces[idx]
        

        if column == "

            self._show_inline_editor(row_id, 1, piece, "texture")
        elif column == "

            self._show_inline_editor(row_id, 2, piece, "bone")
    
    def _show_inline_editor(self, row_id, col_idx, piece, field):
        

        x, y, width, height = self.equip_tree.bbox(row_id, f"
        

        values = self.equip_tree.item(row_id, "values")
        current_val = values[col_idx]
        

        var = tk.StringVar(value=current_val)
        
        if field == "texture":

            options = self.manager.get_textures_for_item(piece.meshName)
            if not options:
                options = [current_val]
        else:
            options = self.manager.BONES
        
        combo = ttk.Combobox(self.equip_tree, textvariable=var, values=options, width=20)
        combo.place(x=x, y=y, width=width, height=height)
        combo.focus_set()
        
        self._edit_widget = combo
        
        def on_select(event=None):
            new_val = var.get()
            

            if field == "texture":
                piece.meshTexture = new_val
            else:
                piece.boneName = new_val
            

            new_values = list(values)
            new_values[col_idx] = new_val
            self.equip_tree.item(row_id, values=tuple(new_values))
            

            all_configs = list(self.kerbal_configs.values())
            self.manager.save_main_config(all_configs)
            
            self.status_var.set(f"Changed {field} to {new_val}")
            

            combo.destroy()
            self._edit_widget = None
        
        def on_cancel(event):
            combo.destroy()
            self._edit_widget = None
        
        combo.bind('<<ComboboxSelected>>', on_select)
        combo.bind('<Return>', on_select)
        combo.bind('<Escape>', on_cancel)
        combo.bind('<FocusOut>', lambda e: self.root.after(200, on_cancel if self._edit_widget == combo else lambda: None))
        

        combo.event_generate('<Down>')
    
    def remove_item(self):
        
        selection = self.equip_tree.selection()
        if not selection or not self.current_kerbal:
            return
        
        idx = self.equip_tree.index(selection[0])
        cfg = self.kerbal_configs.get(self.current_kerbal)
        
        if cfg and idx < len(cfg.hairPieces):
            cfg.hairPieces.pop(idx)
            self.equip_tree.delete(selection[0])
            
            all_configs = list(self.kerbal_configs.values())
            self.manager.save_main_config(all_configs)
            
            self.status_var.set(f"Removed item from {self.current_kerbal}")
    
    def install_selected(self):
        
        selection = self.available_tree.selection()
        if not selection:
            messagebox.showwarning("No Selection", "Select a mod to install")
            return
        
        filepath = self.available_tree.item(selection[0], "tags")[0]
        success, message = self.manager.install_mod(filepath)
        
        if success:
            messagebox.showinfo("Success", message)
            self.refresh_all()
        else:
            messagebox.showerror("Error", message)
    
    def install_from_file(self):
        
        filepath = filedialog.askopenfilename(
            title="Select Mod Zip",
            filetypes=[("Zip files", "*.zip"), ("All files", "*.*")]
        )
        if filepath:
            success, message = self.manager.install_mod(filepath)
            if success:
                messagebox.showinfo("Success", message)
                self.refresh_all()
            else:
                messagebox.showerror("Error", message)
    
    def uninstall_selected(self):
        
        selection = self.installed_tree.selection()
        if not selection:
            messagebox.showwarning("No Selection", "Select a mod to uninstall")
            return
        
        mod_name = self.installed_tree.item(selection[0], "tags")[0]
        

        installed = self.manager.load_installed_mods()
        mod_data = None
        for mid, data in installed.items():
            if data["name"] == mod_name:
                mod_data = data
                break
        
        if mod_data:
            mod_items = set()
            for cfg in mod_data.get("configs", []):
                for hp in cfg.get("hairPieces", []):
                    mod_items.add(hp.get("meshName"))
            

            users = []
            for kerbal_name, cfg in self.kerbal_configs.items():
                for hp in cfg.hairPieces:
                    if hp.meshName in mod_items:
                        users.append(kerbal_name)
                        break
            
            if users:
                msg = f"'{mod_name}' is used by: {', '.join(users)}\n\n"
                msg += "Uninstalling will NOT remove equipped items.\n"
                msg += "Continue?"
                if not messagebox.askyesno("In Use", msg):
                    return
        
        if messagebox.askyesno("Confirm", f"Uninstall '{mod_name}'?"):
            success, message = self.manager.uninstall_mod(mod_name)
            if success:
                messagebox.showinfo("Success", message)
                self.refresh_all()
            else:
                messagebox.showerror("Error", message)


def main():
    root = tk.Tk()
    app = KerbonautGUI(root)
    root.mainloop()


if __name__ == "__main__":
    main()
