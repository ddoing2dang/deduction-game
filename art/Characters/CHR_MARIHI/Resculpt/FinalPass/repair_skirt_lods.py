import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.interpolate import poly_3d_calc
P=Path(__file__).parent;game=bpy.data.collections['Game_LOD_Meshes'];rig=bpy.data.objects['Armature'];rig.data.pose_position='REST'
source=bpy.data.objects['Outfit_PleatedSkirt'];assert len(source.data.vertices)==576
reference=bpy.data.objects['Outfit_LOD0'];reference.data.calc_loop_triangles();tri=[tuple(t.vertices)for t in reference.data.loop_triangles];tree=BVHTree.FromPolygons([v.co for v in reference.data.vertices],tri,all_triangles=True)
mat=bpy.data.materials['MAT_MARIHI_SkirtPrint'];reports={}
for level,N,rows in [(1,32,list(range(6))),(2,32,[0,2,4,5])]:
 vs=[];uvs=[];fs=[];weights=[]
 for j in rows:
  for k in range(N):
   vi=j*96+k*3;p=source.data.vertices[vi].co.copy();vs.append(p);uvs.append((k/N,j/5))
   h,n,idx,d=tree.find_nearest(p);ids=tri[idx];f=poly_3d_calc([reference.data.vertices[i].co for i in ids],h);ws={}
   for i,a in zip(ids,f):
    for g in reference.data.vertices[i].groups:
     name=reference.vertex_groups[g.group].name;ws[name]=ws.get(name,0)+g.weight*a
   weights.append(ws)
 for j in range(len(rows)-1):
  for k in range(N):a=j*N+k;b=j*N+(k+1)%N;fs.append((a,b,b+N,a+N))
 me=bpy.data.meshes.new('HemRingPreserved_LOD'+str(level));me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(me.name,me);game.objects.link(o);me.materials.append(mat);ul=me.uv_layers.new(name='UVMap');cc=me.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER');ul=me.uv_layers.active
 for p in me.polygons:
  p.use_smooth=True;seam=any(uvs[i][0]>.9 for i in p.vertices)and any(uvs[i][0]<.1 for i in p.vertices)
  for li in p.loop_indices:
   i=me.loops[li].vertex_index;u,v=uvs[i];ul.data[li].uv=(u+1 if seam and u<.1 else u,v);cc.data[li].color=(1,1,1,1)
 for i,ws in enumerate(weights):
  for n,w in ws.items():
   if w>1e-7:g=o.vertex_groups.get(n)or o.vertex_groups.new(name=n);g.add([i],w,'REPLACE')
 base=bpy.data.objects['Outfit_LOD'+str(level)];bm=bmesh.new();bm.from_mesh(base.data);idx=[i for i,m in enumerate(base.data.materials)if m==mat];bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index in idx],context='FACES');bm.to_mesh(base.data);bm.free()
 base.hide_set(False);bpy.ops.object.select_all(action='DESELECT');o.select_set(True);base.select_set(True);bpy.context.view_layer.objects.active=base;bpy.ops.object.join();base.hide_set(True);base.hide_render=True
 base.data.calc_loop_triangles();reports[base.name]=len(base.data.loop_triangles)
# Keep the far LOD under 30% without touching face/hands/hair: trim small accessory detail only.
ob=bpy.data.objects['Accessory_LOD2'];bpy.ops.object.select_all(action='DESELECT');ob.hide_set(False);ob.select_set(True);bpy.context.view_layer.objects.active=ob
m=ob.modifiers.new('Far accessory hidden detail budget','DECIMATE');m.ratio=.94;m.use_collapse_triangulate=True
while ob.modifiers.find(m.name)>0:bpy.ops.object.modifier_move_up(modifier=m.name)
bpy.ops.object.modifier_apply(modifier=m.name);ob.hide_set(True);ob.hide_render=True
rig.data.pose_position='POSE';bpy.context.view_layer.update();(P/'skirt-lod-repair.json').write_text(json.dumps(reports,indent=2));print(reports)
