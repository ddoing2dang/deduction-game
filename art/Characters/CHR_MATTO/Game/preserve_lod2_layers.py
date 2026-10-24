import bpy,bmesh,json
from pathlib import Path
P=Path(r'C:\UnityProjects\DeductionGame\deduction-game\art\Characters\CHR_MATTO\Game')
game=bpy.data.collections['Game_LOD_Meshes'];target=bpy.data.objects['Outfit_LOD2']
palette=bpy.data.materials['MAT_MATTO_ColorPalette']
if not target.get('ApprovedCorsetShell'):
    pieces=[]
    for sign in [-1,1]:
        src=bpy.data.objects['Outfit_Corset_Side_'+str(sign)]
        ob=bpy.data.objects.new('LOD2_ApprovedCorset_'+str(sign),src.data.copy());game.objects.link(ob);ob.data.transform(src.matrix_world)
        for g in src.vertex_groups:ob.vertex_groups.new(name=g.name)
        ob.data.materials.clear();ob.data.materials.append(palette)
        if not ob.data.uv_layers:ob.data.uv_layers.new(name='UVMap')
        ob.data.uv_layers.active.name='UVMap'
        for loop in ob.data.uv_layers.active.data:loop.uv=(.875,.125)
        pieces.append(ob)
    bpy.ops.object.select_all(action='DESELECT');target.hide_set(False);target.select_set(True)
    for ob in pieces:ob.select_set(True)
    bpy.context.view_layer.objects.active=target;bpy.ops.object.join()
    target['ApprovedCorsetShell']=True;target.hide_set(True);target.hide_render=True
if not target.get('ApprovedBlouseShell'):
    bm=bmesh.new();bm.from_mesh(target.data);uv=bm.loops.layers.uv.active;deform=bm.verts.layers.deform.active
    palette_index=list(target.data.materials).index(palette);remove=[]
    for face in bm.faces:
        if face.material_index!=palette_index:continue
        if not all(abs(loop[uv].uv.x-.125)<1e-5 and abs(loop[uv].uv.y-.125)<1e-5 for loop in face.loops):continue
        names={target.vertex_groups[i].name for v in face.verts for i,w in v[deform].items()if w>1e-6}
        if not names.issubset({'J_Bip_C_Spine','J_Bip_C_Chest'}):continue
        if max(abs(v.co.x)for v in face.verts)>.055 and max(abs(v.co.x)for v in face.verts)<.15 and min(v.co.z for v in face.verts)>1.03 and max(v.co.z for v in face.verts)<1.29:remove.append(face)
    bmesh.ops.delete(bm,geom=remove,context='FACES');bm.to_mesh(target.data);bm.free()
    src=bpy.data.objects['Outfit_Ivory_Tailored_Blouse'];ob=bpy.data.objects.new('LOD2_ApprovedBlouse',src.data.copy());game.objects.link(ob);ob.data.transform(src.matrix_world)
    for g in src.vertex_groups:ob.vertex_groups.new(name=g.name)
    ob.data.materials.clear();ob.data.materials.append(palette)
    if not ob.data.uv_layers:ob.data.uv_layers.new(name='UVMap')
    ob.data.uv_layers.active.name='UVMap'
    for loop in ob.data.uv_layers.active.data:loop.uv=(.125,.125)
    bpy.ops.object.select_all(action='DESELECT');target.hide_set(False);target.select_set(True);ob.select_set(True);bpy.context.view_layer.objects.active=target;bpy.ops.object.join()
    target['ApprovedBlouseShell']=True;target.hide_set(True);target.hide_render=True
p=P/'export_game_fbx.py';exec(compile(p.read_text(encoding='utf-8'),str(p),'exec'),{'__file__':str(p)})
result={}
for level in [0,1,2]:
    n=0
    for ob in game.objects:
        if ob.get('LOD')==level:ob.data.calc_loop_triangles();n+=len(ob.data.loop_triangles)
    result[level]=n
(P/'final-lod-counts.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
p=P/'capture_final_review.py';exec(compile(p.read_text(encoding='utf-8'),str(p),'exec'),{'__file__':str(p)})
