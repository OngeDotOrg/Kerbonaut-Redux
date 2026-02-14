bl_info = {
    "name": "Kerbal Redux Exporter",
    "author": "Your Name",
    "version": (1, 0, 0),
    "blender": (3, 0, 0),
    "location": "File > Export > Kerbal Redux Mesh (.vtx/.tex/.nml/.idx)",
    "description": "Export meshes for Kerbal Redux mod directly to KSP format",
    "category": "Import-Export",
    "support": "COMMUNITY",
    "doc_url": "",
    "tracker_url": "",
}

import bpy
import struct
import os
from bpy.props import StringProperty, BoolProperty, EnumProperty
from bpy.types import Operator, Panel
from bpy_extras.io_utils import ExportHelper


class KSPMeshExporter:
    """Handles the actual mesh conversion."""
    
    @staticmethod
    def write_vector3_array(data, filepath):
        """Write Vector3 array to binary file."""
        with open(filepath, 'wb') as f:
            f.write(struct.pack('I', len(data)))
            for v in data:
                f.write(struct.pack('fff', v[0], v[1], v[2]))
    
    @staticmethod
    def write_vector2_array(data, filepath):
        """Write Vector2 array to binary file."""
        with open(filepath, 'wb') as f:
            f.write(struct.pack('I', len(data)))
            for v in data:
                f.write(struct.pack('ff', v[0], v[1]))
    
    @staticmethod
    def write_int_array(data, filepath):
        """Write int array to binary file."""
        with open(filepath, 'wb') as f:
            f.write(struct.pack('I', len(data)))
            for idx in data:
                f.write(struct.pack('i', idx))
    
    @staticmethod
    def triangulate_mesh(mesh):
        """Triangulate mesh and return data."""
        import bmesh
        
        # Create BMesh from mesh
        bm = bmesh.new()
        bm.from_mesh(mesh)
        
        # Triangulate
        bmesh.ops.triangulate(bm, faces=bm.faces[:])
        
        # Ensure face indices
        bm.faces.ensure_lookup_table()
        
        # Extract data
        vertices = []
        normals = []
        uvs = []
        indices = []
        
        # Create a dictionary to track unique vertex/uv/normal combinations
        vertex_map = {}
        unique_vertices = []
        
        uv_layer = bm.loops.layers.uv.active
        
        for face in bm.faces:
            for loop in face.loops:
                vert = loop.vert
                
                # COORDINATE SYSTEM CONVERSION
                # Blender: Z-up, -Y-forward (right-handed)
                # Unity/KSP: Y-up, Z-forward (left-handed)
                # 
                # Position: (Y, Z, -X) - tested and correct
                # Normal: (Y, Z, -X) with inversion to face outward
                # Smooth shading: use vertex normal instead of loop normal
                
                pos = (vert.co.y, vert.co.z, -vert.co.x)
                
                # Get smooth vertex normal (not flat face normal)
                # Invert to face outward
                normal = (vert.normal.y, vert.normal.z, -vert.normal.x)
                
                # Get UV
                if uv_layer:
                    uv = (loop[uv_layer].uv.x, loop[uv_layer].uv.y)
                else:
                    uv = (0.0, 0.0)
                
                # Create unique key
                key = (pos, normal, uv)
                
                if key not in vertex_map:
                    vertex_map[key] = len(unique_vertices)
                    unique_vertices.append({
                        'pos': pos,
                        'normal': normal,
                        'uv': uv
                    })
                
                indices.append(vertex_map[key])
        
        # Build final arrays
        for v in unique_vertices:
            vertices.append(v['pos'])
            normals.append(v['normal'])
            uvs.append(v['uv'])
        
        reversed_indices = []
        for i in range(0, len(indices), 3):
            reversed_indices.append(indices[i])
            reversed_indices.append(indices[i+2])  # Swap these two
            reversed_indices.append(indices[i+1])  # to reverse winding
        indices = reversed_indices

        bm.free()
        
        return vertices, normals, uvs, indices
    
    @classmethod
    def export_mesh(cls, obj, filepath_base):
        """Export mesh to KSP format."""
        # Get mesh with modifiers applied
        depsgraph = bpy.context.evaluated_depsgraph_get()
        obj_eval = obj.evaluated_get(depsgraph)
        mesh = obj_eval.to_mesh()
        
        try:
            # Triangulate and get data (with coordinate conversion)
            vertices, normals, uvs, indices = cls.triangulate_mesh(mesh)
            
            if not vertices:
                return (False, "Mesh has no vertices!")
            
            # Write files
            cls.write_vector3_array(vertices, filepath_base + ".vtx")
            cls.write_vector2_array(uvs, filepath_base + ".tex")
            cls.write_vector3_array(normals, filepath_base + ".nml")
            cls.write_int_array(indices, filepath_base + ".idx")
            
            return (True, f"Exported {len(vertices)} vertices, {len(indices)//3} triangles")
            
        finally:
            obj_eval.to_mesh_clear()


class EXPORT_OT_kerbal_redux(Operator, ExportHelper):
    """Export selected mesh to Kerbal Redux format"""
    bl_idname = "export.kerbal_redux"
    bl_label = "Export Kerbal Redux Mesh"
    bl_options = {'PRESET'}
    
    filename_ext = ""
    use_filter_folder = True
    
    filter_glob: StringProperty(
        default="",
        options={'HIDDEN'},
        maxlen=255,
    )
    
    mesh_name: StringProperty(
        name="Mesh Name",
        description="Name for the mesh files (e.g., 'ValentinaHair')",
        default="MyMesh",
    )
    
    export_selected: BoolProperty(
        name="Export Selected Only",
        description="Export only the selected object",
        default=True,
    )
    
    apply_transforms: BoolProperty(
        name="Apply Transforms",
        description="Apply object transforms (Ctrl+A in Blender). If this fails, apply manually before export.",
        default=False,
    )
    
    open_folder: BoolProperty(
        name="Open Folder After Export",
        description="Open the destination folder after export",
        default=False,
    )
    
    def draw(self, context):
        layout = self.layout
        layout.prop(self, "mesh_name")
        layout.prop(self, "export_selected")
        layout.prop(self, "apply_transforms")
        layout.prop(self, "open_folder")
        
        # Show info
        layout.separator()
        box = layout.box()
        box.label(text="Output files:", icon='FILE')
        box.label(text=f"  {self.mesh_name}.vtx - Vertices")
        box.label(text=f"  {self.mesh_name}.tex - UV coordinates")
        box.label(text=f"  {self.mesh_name}.nml - Normals")
        box.label(text=f"  {self.mesh_name}.idx - Triangle indices")
    
    def execute(self, context):
        # Get object to export
        if self.export_selected:
            obj = context.active_object
            if not obj:
                self.report({'ERROR'}, "No active object selected!")
                return {'CANCELLED'}
        else:
            mesh_objects = [o for o in context.scene.objects if o.type == 'MESH']
            if not mesh_objects:
                self.report({'ERROR'}, "No mesh objects found!")
                return {'CANCELLED'}
            obj = mesh_objects[0]
        
        if obj.type != 'MESH':
            self.report({'ERROR'}, f"Selected object '{obj.name}' is not a mesh!")
            return {'CANCELLED'}
        
        # Apply transforms if requested
        if self.apply_transforms:
            try:
                # Store current mode
                original_mode = obj.mode
                
                # Switch to object mode if needed
                if original_mode != 'OBJECT':
                    bpy.ops.object.mode_set(mode='OBJECT')
                
                # Apply transforms
                bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
                
                # Restore original mode if needed
                if original_mode == 'EDIT':
                    bpy.ops.object.mode_set(mode='EDIT')
                    
            except RuntimeError as e:
                self.report({'WARNING'}, f"Could not auto-apply transforms: {str(e)}")
                self.report({'INFO'}, "Please apply manually: Ctrl+A → All Transforms")
        
        # Export
        filepath_base = os.path.join(os.path.dirname(self.filepath), self.mesh_name)
        
        try:
            success, message = KSPMeshExporter.export_mesh(obj, filepath_base)
            
            if success:
                self.report({'INFO'}, f"Exported {self.mesh_name}: {message}")
                
                if self.open_folder:
                    import subprocess
                    folder = os.path.dirname(filepath_base)
                    if os.name == 'nt':
                        subprocess.Popen(f'explorer "{folder}"')
                    else:
                        subprocess.Popen(['xdg-open', folder])
                
                return {'FINISHED'}
            else:
                self.report({'ERROR'}, message)
                return {'CANCELLED'}
                
        except Exception as e:
            self.report({'ERROR'}, f"Export failed: {str(e)}")
            return {'CANCELLED'}


class KERBAL_REDUX_PT_panel(Panel):
    """Kerbal Redux panel in the sidebar"""
    bl_label = "Kerbal Redux"
    bl_idname = "KERBAL_REDUX_PT_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'Kerbal Redux'
    
    def draw(self, context):
        layout = self.layout
        
        # Export button
        layout.operator("export.kerbal_redux", icon='EXPORT')
        
        layout.separator()
        
        # Quick export section
        box = layout.box()
        box.label(text="Quick Export Settings:", icon='SETTINGS')
        
        # Show current object info
        obj = context.active_object
        if obj and obj.type == 'MESH':
            box.label(text=f"Selected: {obj.name}", icon='OBJECT_DATA')
            
            # Mesh stats
            mesh = obj.data
            box.label(text=f"Verts: {len(mesh.vertices)} | Faces: {len(mesh.polygons)}")
        else:
            box.label(text="Select a mesh object", icon='ERROR')
        
        layout.separator()
        
        # Instructions
        box = layout.box()
        box.label(text="Instructions:", icon='INFO')
        box.label(text="1. Position mesh on kerbal")
        box.label(text="2. UV unwrap the mesh")
        box.label(text="3. Click Export above")
        box.label(text="4. Copy files to KSP")


class KERBAL_REDUX_OT_quick_export(Operator):
    """Quick export to default location"""
    bl_idname = "kerbal_redux.quick_export"
    bl_label = "Quick Export"
    bl_description = "Export to the last used location"
    
    def execute(self, context):
        # Get settings from scene
        settings = context.scene.kerbal_redux_settings
        
        if not settings.output_path:
            self.report({'ERROR'}, "No output path set! Use Export dialog first.")
            return {'CANCELLED'}
        
        obj = context.active_object
        if not obj or obj.type != 'MESH':
            self.report({'ERROR'}, "No mesh selected!")
            return {'CANCELLED'}
        
        filepath_base = os.path.join(settings.output_path, settings.mesh_name)
        
        try:
            success, message = KSPMeshExporter.export_mesh(obj, filepath_base)
            
            if success:
                self.report({'INFO'}, f"Exported: {message}")
                return {'FINISHED'}
            else:
                self.report({'ERROR'}, message)
                return {'CANCELLED'}
                
        except Exception as e:
            self.report({'ERROR'}, f"Export failed: {str(e)}")
            return {'CANCELLED'}


class KerbalReduxSettings(bpy.types.PropertyGroup):
    """Settings for the addon"""
    output_path: StringProperty(
        name="Output Path",
        description="Folder where mesh files will be saved",
        default="",
        subtype='DIR_PATH',
    )
    
    mesh_name: StringProperty(
        name="Mesh Name",
        description="Name for the exported mesh files",
        default="MyMesh",
    )
    
    ksp_path: StringProperty(
        name="KSP GameData Path",
        description="Path to KSP's GameData folder (optional, for auto-install)",
        default="",
        subtype='DIR_PATH',
    )


classes = (
    KerbalReduxSettings,
    EXPORT_OT_kerbal_redux,
    KERBAL_REDUX_PT_panel,
    KERBAL_REDUX_OT_quick_export,
)


def menu_func_export(self, context):
    self.layout.operator(EXPORT_OT_kerbal_redux.bl_idname, text="Kerbal Redux Mesh (.vtx/.tex/.nml/.idx)")


def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    
    bpy.types.Scene.kerbal_redux_settings = bpy.props.PointerProperty(type=KerbalReduxSettings)
    bpy.types.TOPBAR_MT_file_export.append(menu_func_export)


def unregister():
    bpy.types.TOPBAR_MT_file_export.remove(menu_func_export)
    
    for cls in reversed(classes):
        bpy.utils.unregister_class(cls)
    
    del bpy.types.Scene.kerbal_redux_settings


if __name__ == "__main__":
    register()
