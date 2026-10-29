import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).parent;s=bpy.context.scene;rig=bpy.data.objects['Armature']
print('head bone z',rig.data.bones['J_Bip_C_Head'].matrix_local.translation.z)
print('body max',max(v.co.z for v in bpy.data.objects['Body_LOD0'].data.vertices))
assert 1.60<max(v.co.z for v in bpy.data.objects['Body_LOD0'].data.vertices)<1.64

body=bpy.data.objects['Body_LOD0'];face=bpy.data.objects['Face_LOD0']
vs=[];fs=[]
for o in [body,face]:
 for p in o.data.polygons:
  if o==face and p.material_index!=0:continue
  if min(o.data.vertices[i].co.z for i in p.vertices)<1.28:continue
  ids=[]
  for i in p.vertices:ids.append(len(vs));vs.append(o.data.vertices[i].co.copy())
  fs.append(ids)
tree=BVHTree.FromPolygons(vs,fs)
origin=Vector((0,.018,1.43))
def fit(q):
 d=(q-origin).normalized();h,n,_,_=tree.ray_cast(origin+d*.40,-d,.75)
 if h is None:h,n,_,_=tree.find_nearest(q)
 if n.dot(d)<0:n=-n
 return h+n*.0032
crown=bpy.data.objects['Hair_ScalpCrown'];old=[v.co.copy()for v in crown.data.vertices]
ct=BVHTree.FromPolygons(old,[tuple(p.vertices)for p in crown.data.polygons])
for level in range(3):
 ob=bpy.data.objects['Hair_Main_LOD'+str(level)];count=0
 for v in ob.data.vertices:
  d=ct.find_nearest(v.co)[3]
  if d<.003 or v.co.z>1.585:v.co=fit(v.co);count+=1
 ob.data.update();print(ob.name,'scalp fitted',count)
for o in bpy.data.collections['Approved_Source_Archive'].objects:

 if o.name.startswith('Hair_'):
  for v in o.data.vertices:
   if o.name=='Hair_ScalpCrown'or v.co.z>1.585:v.co=fit(v.co)
(P/'scalp-fit.json').write_text(json.dumps({'body_scale_correction':1.0,'rig_scale_already_applied':True,'crown_offset_m':.0032,'reason':'Historical technical source head was unscaled; scalp reconstructed exclusively from current matching body and face'},indent=2))
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI_GAME.blend'),compress=True)
