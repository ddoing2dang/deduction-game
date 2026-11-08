"""Import an ACC_*.glb accessory in Blender 5.x without modifying a character rig.

In Blender Python:
  from import_accessory_blender import import_accessory
  obj_list = import_accessory("ACC_Bow_Burgundy")
Each item can then be manually fitted or precisely placed by a modeling script.
"""
from pathlib import Path
import bpy

ROOT=Path(__file__).resolve().parent

def import_accessory(asset_name: str):
    if not asset_name.startswith("ACC_") or "/" in asset_name or "\\" in asset_name or "." in asset_name:
        raise ValueError("Expected a simple ACC_* asset name")
    path=ROOT/"glb"/(asset_name+".glb")
    if not path.is_file():
        raise FileNotFoundError(path)
    existing=set(bpy.context.scene.objects)
    result=bpy.ops.import_scene.gltf(filepath=str(path))
    if "FINISHED" not in result:
        raise RuntimeError(f"Blender import failed: {asset_name}")
    imported=[ob for ob in bpy.context.scene.objects if ob not in existing]
    if not imported:
        raise RuntimeError(f"No objects imported for {asset_name}")
    collection=bpy.data.collections.get("AccessoryLibrary_Imported")
    if collection is None:
        collection=bpy.data.collections.new("AccessoryLibrary_Imported")
        bpy.context.scene.collection.children.link(collection)
    for ob in imported:
        if ob.type!="MESH":
            continue
        if collection not in list(ob.users_collection):
            collection.objects.link(ob)
        ob["accessory_asset"]=asset_name
        ob["source_glb"]=str(path)
        # Imported items are not auto-parented or auto-weighted.
    return imported
