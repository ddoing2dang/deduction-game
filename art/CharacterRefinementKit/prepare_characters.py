"""Non-destructive Meshy character material/inspection preparation for Blender 4.4+.

Example:
  blender -b BOKI.blend --python prepare_characters.py -- --output ./refined

This does NOT sculpt facial features, perform retopology, or paint a UV texture.
It intentionally does not assign palettes to arbitrary faces of a one-piece mesh.
"""
import bpy
import sys
import os
import re
import json
import hashlib
from pathlib import Path
from collections import Counter

PALETTES = {
    "BOKI": {
        "eye": "#777B84", "skin": "#EBC5B7", "hair": "#65202E", "hair_secondary": "#A3364D",
        "outfit_primary": "#1A1A21", "outfit_secondary": "#BDA78F", "ribbon": "#492A31", "metal": "#BEA17D", "shoes": "#1B161C",
        "design": "Dark burgundy short bob; gray irises; beige oversized sleeves, black top and pleated skirt, thigh strap",
    },
    "MATTO": {
        "eye": "#B42437", "skin": "#F2D5CB", "hair": "#E7E6E9", "hair_secondary": "#BFC2CB",
        "outfit_primary": "#17171C", "outfit_secondary": "#E9E6E7", "ribbon": "#6D172C", "metal": "#B2A1A6", "shoes": "#1C1920",
        "design": "White long hair; red eyes; black beret, large deep-red neck ribbon, black/white/deep-red pleated outfit",
    },
    "JIRO": {
        "eye": "#E7CB3D", "skin": "#F5D2C4", "hair": "#91D35D", "hair_secondary": "#4E975B",
        "outfit_primary": "#16251D", "outfit_secondary": "#EFF0E9", "ribbon": "#3C8E54", "metal": "#B0B2AC", "shoes": "#242327",
        "design": "Green twin tails, white flower ornaments, yellow eyes, white shirt, green ribbon, pleated skirt",
    },
    "NAZ": {
        "eye": "#E78AAE", "skin": "#F0CDBF", "hair": "#715044", "hair_secondary": "#D894AB",
        "outfit_primary": "#1A1B22", "outfit_secondary": "#F2ECE9", "ribbon": "#DD8AA8", "metal": "#B7A9AA", "shoes": "#1C1A1F",
        "design": "Long brown hair with pink tips; pink eyes; paired ribbons; pink argyle and black skirt",
    },
    "MARIHI": {
        "eye": "#3D975D", "skin": "#CDA084", "hair": "#EDCD66", "hair_secondary": "#F7E5A4",
        "outfit_primary": "#17171A", "outfit_secondary": "#F1ECE5", "ribbon": "#161418", "metal": "#C7A45B", "shoes": "#19151B",
        "design": "Warm slightly tanned gyaru skin; green irises; wavy blonde twin tails; black bows; black/white/gold",
    },
    "MUNIRE": {
        "eye": "#8259BD", "skin": "#F3D9D2", "hair": "#8D6CB9", "hair_secondary": "#AF90D5",
        "outfit_primary": "#1F1C29", "outfit_secondary": "#F3EDF2", "ribbon": "#594088", "metal": "#A9A2B1", "shoes": "#1D1B27",
        "design": "Long purple hair and purple eyes; butterfly ornament; white puff sleeves, dark pleated skirt",
    },
}

COLOR_FIELDS = {
    "skin": "FaceSkin", "eye": "EyeIris", "hair": "HairMain", "hair_secondary": "HairAccent",
    "outfit_primary": "OutfitPrimary", "outfit_secondary": "OutfitSecondary",
    "ribbon": "RibbonsAndDetails", "metal": "MetalAndBuckle", "shoes": "Shoes",
}


def linear_hex(hex_color):
    h = hex_color.lstrip('#')
    s = [int(h[i:i+2], 16) / 255.0 for i in (0,2,4)]
    return tuple(v / 12.92 if v <= 0.04045 else ((v+0.055)/1.055)**2.4 for v in s) + (1.0,)


def make_material(name, srgb, kind):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    m.use_fake_user = True
    m.diffuse_color = linear_hex(srgb)
    m['user_color_srgb_hex'] = srgb
    m['locked_color_role'] = kind
    nodes = m.node_tree.nodes
    nodes.clear()
    output = nodes.new('ShaderNodeOutputMaterial')
    shader = nodes.new('ShaderNodeBsdfPrincipled')
    shader.inputs['Base Color'].default_value = linear_hex(srgb)
    shader.inputs['Roughness'].default_value = 0.32 if kind == 'eye' else (0.27 if kind == 'metal' else 0.76 if kind in ['outfit_primary', 'shoes'] else 0.59)
    if kind == 'metal':
        shader.inputs['Metallic'].default_value = 0.78
    elif kind == 'eye':
        shader.inputs['Coat Weight'].default_value = 0.28
        shader.inputs['Coat Roughness'].default_value = 0.13
    m.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
    return m


def infer_character():
    path = os.path.basename(bpy.data.filepath).upper()
    # Do not accidentally confuse WIP names with other characters.
    for key in PALETTES:
        if re.search(r'(?:^|[_\-])'+re.escape(key)+r'(?:[_\-.]|$)', path):
            return key
    return None


def mesh_report(obj):
    mesh = obj.data
    palette_usage = Counter()
    for p in mesh.polygons:
        palette_usage[p.material_index] += 1
    # Non-renderable audit only; original geometry is never changed.
    coords = [obj.matrix_world @ v.co for v in mesh.vertices]
    ext = ({'min': [min(c[i] for c in coords) for i in range(3)],
            'max': [max(c[i] for c in coords) for i in range(3)]} if coords else None)
    return {'name': obj.name, 'vertices': len(mesh.vertices), 'edges': len(mesh.edges),
            'polygons': len(mesh.polygons), 'triangles_expected_at_most': max(0, 2*len(mesh.polygons)),
            'shape_keys': list(mesh.shape_keys.key_blocks.keys()) if mesh.shape_keys else [],
            'vertex_groups': [g.name for g in obj.vertex_groups],
            'uv_maps': [u.name for u in mesh.uv_layers],
            'color_attributes': [c.name for c in mesh.color_attributes],
            'material_slots': [s.material.name if s.material else None for s in obj.material_slots],
            'bounds_world': ext, 'face_count_by_material_slot': dict(palette_usage)}


def prepare():
    args = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    character = None
    output_dir = None
    for i,a in enumerate(args):
        if a == '--character' and i+1 < len(args): character = args[i+1].upper()
        elif a == '--output' and i+1 < len(args): output_dir = args[i+1]
    character = character or infer_character()
    if character not in PALETTES:
        raise RuntimeError('Character code missing. Use -- --character BOKI (or MATTO/JIRO/NAZ/MARIHI/MUNIRE).')
    if output_dir is None:
        output_dir = str(Path(bpy.data.filepath).parent / 'refinement_work')
    output_dir = Path(output_dir).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)
    blend_out = output_dir / ('CHR_'+character+'_REFINEMENT_WIP.blend')
    report_out = output_dir / ('CHR_'+character+'_AUDIT.json')
    if blend_out.exists():
        raise RuntimeError('Refusing to overwrite existing WIP: '+str(blend_out))

    original_objects = [mesh_report(obj) for obj in bpy.data.objects if obj.type=='MESH']
    original_materials = list(bpy.data.materials.keys())
    original_images = list(bpy.data.images.keys())
    for key, display in COLOR_FIELDS.items():
        # Populate Material Library; do NOT assign random mesh faces to it.
        make_material('LOCK_'+character+'_'+display, PALETTES[character][key], key)
    # Non-invasive project metadata. Prevents accidentally treating WIP as finished.
    scene=bpy.context.scene
    scene['character_code']=character
    scene['design_lock']=PALETTES[character]['design']
    scene['refinement_status']='WIP: material palette prepared; no semantic region assignment, face sculpt, UV painting, rigging or validation'
    scene['requires_manually_verified_iris_color']=PALETTES[character]['eye']
    scene['source_file']=os.path.basename(bpy.data.filepath)

    report = {
        'character':character, 'source':bpy.data.filepath, 'output':str(blend_out),
        'blender_version':bpy.app.version_string,
        'source_meshes':original_objects,
        'source_materials':original_materials, 'source_images':original_images,
        'source_armature_count':len([ob for ob in bpy.data.objects if ob.type=='ARMATURE']),
        'palette_hex':{k:PALETTES[character][k] for k in COLOR_FIELDS},
        'real_work_remaining':['facial sculpt and actual facial landmarks','iris region selection, exact LOCK eye material assignment',
                               'anatomy, garment and hair region splitting','UV seams/texture authoring and baking',
                               'topology reconstruction and LOD','rigging, blendshapes and Unity import/play validation'],
        'status':'PREP_ONLY_NOT_FINISHED'
    }
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_out), compress=True)
    report_out.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print('PREPARED',blend_out)
    print('AUDIT',report_out)
    print('NOT_FINISHED: no facial sculpt, no iris recoloring, no paint or rigging done')

if __name__ == '__main__':
    prepare()
