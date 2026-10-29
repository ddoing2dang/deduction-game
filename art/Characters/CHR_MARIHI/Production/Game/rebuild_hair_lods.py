import bpy,math,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).parent;s=bpy.context.scene;rig=bpy.data.objects['Armature'];game=bpy.data.collections['Game_LOD_Meshes'];src=bpy.data.collections['Approved_Source_Archive']
vs=[];fs=[]
for o in [bpy.data.objects['Body_LOD0'],bpy.data.objects['Face_LOD0']]:
 for p in o.data.polygons:
  if o.name.startswith('Face')and p.material_index!=0:continue
  if min(o.data.vertices[i].co.z for i in p.vertices)<1.28:continue
  off=len(vs);vs.extend([o.data.vertices[i].co.copy()for i in p.vertices]);fs.append(list(range(off,len(vs))))
tree=BVHTree.FromPolygons(vs,fs);origin=Vector((0,.018,1.43))
def fit(q,offset):
 d=(q-origin).normalized();h,n,_,_=tree.ray_cast(origin+d*.40,-d,.75)
 if h is None:h,n,_,_=tree.find_nearest(q)
 if n.dot(d)<0:n=-n
 return h+n*offset
parts=[o for o in src.objects if o.name.startswith('Hair_')]
for ob in parts:
 if ob.name=='Hair_ScalpCrown':
  for v in ob.data.vertices:v.co=fit(v.co,.0032)
 elif ob.name.startswith('Hair_Sweep_'):
  for v in ob.data.vertices:v.co=fit(v.co,.0080)
 elif ob.name.startswith('Hair_Fringe_'):
  for v in ob.data.vertices:
   if v.co.z>1.505:v.co=fit(v.co,.011)
   else:v.co.y-=.002
 # Original source root IDs remain useful evidence, unlike simplified vertex indices.
 elif ob.get('RootIndices'):
  for i in ob['RootIndices']:ob.data.vertices[i].co=fit(ob.data.vertices[i].co,.0040)
verts=[];faces=[];uvs=[];colors=[];weights=[]
for ob in parts:
 off=len(verts);verts.extend(v.co.copy()for v in ob.data.vertices)
 weights.extend([{ob.vertex_groups[g.group].name:g.weight for g in v.groups}for v in ob.data.vertices])
 uv=ob.data.uv_layers.active.data if ob.data.uv_layers else None
 for p in ob.data.polygons:
  faces.append(tuple(i+off for i in p.vertices));mat=ob.data.materials[p.material_index];color=list(mat.get('flat_color',[1,1,1,1]))
  for li in p.loop_indices:uvs.append(tuple(uv[li].uv)if uv else(0,0));colors.append(color)
for level in range(3):
 old=bpy.data.objects['Hair_Main_LOD'+str(level)];bpy.data.objects.remove(old,do_unlink=True)
 me=bpy.data.meshes.new('MarihiHairSeparatedSurface_LOD'+str(level));me.from_pydata(verts,[],faces);me.update();ob=bpy.data.objects.new('Hair_Main_LOD'+str(level),me);game.objects.link(ob);ob.parent=bpy.data.objects['Mesh'];ob['LOD']=level
 me.materials.append(bpy.data.materials['MAT_MARIHI_Hair']);uv=me.uv_layers.new(name='UVMap');vc=me.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER');uv=me.uv_layers.active.data
 for i,(a,c)in enumerate(zip(uvs,colors)):uv[i].uv=a;vc.data[i].color=c
 for p in me.polygons:p.use_smooth=True
 for i,ws in enumerate(weights):
  for name,w in ws.items():
   g=ob.vertex_groups.get(name)or ob.vertex_groups.new(name=name);g.add([i],w,'REPLACE')
 me.calc_loop_triangles();target=[9500,4750,1899][level]
 bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
 m=ob.modifiers.new('Part-aware hair surface budget','DECIMATE');m.ratio=min(1,target/len(me.loop_triangles));m.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=m.name)
 ar=ob.modifiers.new('Humanoid Skin','ARMATURE');ar.object=rig;ob.hide_render=level!=0;ob.hide_set(level!=0)
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI_GAME.blend'),compress=True)
print('Separated crown / sweep / bang surface offsets, no coincident scalp layers; rebuilt all hair LODs from editable source')
