"""Execute via Blender MCP with CHARACTER='NAZ' (or another registered code)."""
import importlib.util
from pathlib import Path
import bpy

root = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('character_source_registry', root / 'source_registry.py')
registry = importlib.util.module_from_spec(spec)
spec.loader.exec_module(registry)
registry.verify()
code = globals().get('CHARACTER')
if not code:
    raise RuntimeError('Explicit CHARACTER is required; no character is selected automatically.')
if bpy.data.is_dirty:
    raise RuntimeError('Current Blender scene has unsaved changes; preserve it before switching.')
bpy.ops.wm.open_mainfile(filepath=str(registry.resolve(code)))
