# Character visual inspection (Blender 4.4+/5.0 compatible)
# Reads character mesh, writes non-destructive orthographic view renders + island audit.
# No geometrical/material edits and DOES NOT save the open .blend.
import bpy
import sys
import os
import json
from pathlib import Path
from mathutils import Vector


def arg_value(name, default=None):
    args = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    return args[args.index(name)+1] if name in args and args.index(name)+1 < len(args) else default


def island_report(mesh):
    n = len(mesh.vertices)
    parent = list(range(n))
    sizes = [1] * n
    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    for edge in mesh.edges:
        a, b = edge.vertices
        ra, rb = find(a), find(b)
        if ra != rb:
            if sizes[ra] < sizes[rb]:
                ra, rb = rb, ra
            parent[rb] = ra
            sizes[ra] += sizes[rb]
    counts = {}
    for i in range(n):
        root = find(i)
        counts[root] = counts.get(root, 0) + 1
    return {'island_count': len(counts),
            'largest_islands_vertices': sorted(counts.values(), reverse=True)[:25],
            'single_vertex_islands': sum(1 for c in counts.values() if c == 1)}


def run():
    code = (arg_value('--character') or Path(bpy.data.filepath).stem.replace('CHR_','').split('_')[0]).upper()
    out = Path(arg_value('--output', str(Path(bpy.data.filepath).parent / 'visual_audit'))).resolve() / code
    out.mkdir(parents=True, exist_ok=True)
    meshes = [o for o in bpy.context.scene.objects if o.type=='MESH']
    if not meshes:
        raise RuntimeError('No mesh objects in file')
    corners=[]
    for obj in meshes:
        # Calculate all object bound box corners, not full vertex lists
        corners.extend([obj.matrix_world @ Vector(pt) for pt in obj.bound_box])
    lo=[min(v[i] for v in corners) for i in range(3)]
    hi=[max(v[i] for v in corners) for i in range(3)]
    mid=Vector([(lo[i]+hi[i])/2 for i in range(3)])
    height=hi[2]-lo[2]
    if height <= 0:
        raise RuntimeError('Invalid mesh height')

    report={'character':code, 'file':Path(bpy.data.filepath).name, 'blender':bpy.app.version_string,
            'mesh_count':len(meshes), 'bounds':{'min':lo,'max':hi},
            'sources':[],
            'NOTE':'Visual inspection only; no sculpt, UV, materials, rigging or original files were changed.'}
    for obj in meshes:
        m=obj.data
        # In case a mesh is very large, this still works with a few hundred thousand vertices.
        report['sources'].append({'name':obj.name,'vertices':len(m.vertices),
                                  'faces':len(m.polygons),'triangles':sum(max(0,len(face.vertices)-2) for face in m.polygons),
                                  'uv_layers':len(m.uv_layers), 'material_slots':len(obj.material_slots),
                                  'shape_keys':list(m.shape_keys.key_blocks.keys()) if m.shape_keys else [],
                                  'vertex_groups':len(obj.vertex_groups), **island_report(m)})

    scene=bpy.context.scene
    scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x=600
    scene.render.resolution_y=900
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.image_settings.color_mode='RGBA'
    scene.render.film_transparent=False
    scene.camera=None
    # Workbench uses studio lighting independently of nonexistent source texture/material.
    shade=scene.display.shading
    shade.light='STUDIO'
    shade.color_type='SINGLE'
    shade.single_color=(0.65,0.66,0.70)
    for attr, val in (('show_cavity', True), ('cavity_type', 'BOTH'),
                      ('curvature_ridge_factor',1.3),('curvature_valley_factor',1.0),
                      ('show_shadows',True),('show_specular_highlight',True)):
        try: setattr(shade,attr,val)
        except (AttributeError,TypeError): pass
    scene.render.filepath=str(out / 'unwritten.png')
    camera_data=bpy.data.cameras.new('REVIEW_ONLY_Ortho')
    camera_data.type='ORTHO'
    camera_data.clip_start=0.01
    camera_data.clip_end=100.0
    cam=bpy.data.objects.new('REVIEW_ONLY_Camera', camera_data)
    scene.collection.objects.link(cam)
    scene.camera=cam

    body_center=mid.copy()
    face_center=mid.copy()
    face_center.z=lo[2] + 0.85*height
    views=[
        ('01_Y_MINUS_FULL', (0,-1,0), body_center, 1.13*height),
        ('02_Y_PLUS_FULL', (0,1,0), body_center, 1.13*height),
        ('03_X_PLUS_SIDE', (1,0,0), body_center, 1.13*height),
        ('04_Y_MINUS_FACE', (0,-1,0), face_center, .37*height),
        ('05_Y_PLUS_FACE', (0,1,0), face_center, .37*height),
    ]
    for label,vec,center,ortho in views:
        cam.location=center + Vector(vec)*4.0
        cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
        camera_data.ortho_scale=ortho
        scene.render.filepath=str(out / (label+'.png'))
        bpy.ops.render.render(write_still=True)
        print('VIEW_SAVED', scene.render.filepath)
    (out / 'mesh_islands.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print('COMPLETED_VISUAL_AUDIT', code, out)
    print('NOT_FINISHED: Source .blend was not saved or modified')


if __name__=='__main__':
    run()
