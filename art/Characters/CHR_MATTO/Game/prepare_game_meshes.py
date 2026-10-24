import bpy,math,json,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
P=Path(r'C:\UnityProjects\DeductionGame\deduction-game\art\Characters\CHR_MATTO\Game')
scene=bpy.context.scene;rig=bpy.data.objects['Armature.001']
assert Path(bpy.data.filepath)==P/'CHR_MATTO_GAME.blend'
assert not bpy.data.collections.get('Game_LOD_Meshes'),'Game meshes already built; reopen approved source into game copy to rebuild.'
rig.animation_data_clear();rig.data.pose_position='REST'
for p in rig.pose.bones:p.rotation_mode='QUATERNION';p.rotation_quaternion=(1,0,0,0);p.location=(0,0,0);p.scale=(1,1,1)
bpy.context.view_layer.update()
source=[o for o in scene.objects if o.type=='MESH']
game=bpy.data.collections.new('Game_LOD_Meshes');scene.collection.children.link(game)
src=bpy.data.collections.new('Approved_Source_Archive');scene.collection.children.link(src)
# Facial aliases reuse the original per-vertex shapes; Neutral is exactly the approved base.
alias={
'Neutral':{},'Smile':{'Fcl_ALL_Fun':.75},'Happy':{'Fcl_ALL_Joy':.8},
'Suspicious':{'Fcl_EYE_Angry':.3,'Fcl_BRW_Angry':.25,'Fcl_MTH_Small':.15},
'Angry':{'Fcl_ALL_Angry':1},'Surprised':{'Fcl_ALL_Surprised':1},
'Worried':{'Fcl_BRW_Sorrow':.7,'Fcl_MTH_Sorrow':.4},
'Thinking':{'Fcl_EYE_Angry':.2,'Fcl_BRW_Sorrow':.25},'Smirk':{'Fcl_MTH_Fun':.6},'Sad':{'Fcl_ALL_Sorrow':1},
'Blink_L':{'Fcl_EYE_Close_L':1},'Blink_R':{'Fcl_EYE_Close_R':1},
'EyeLook_L':{},'EyeLook_R':{},'EyeLook_Up':{},'EyeLook_Down':{},
'Brow_Up':{'Fcl_BRW_Surprised':1},'Brow_Down':{'Fcl_BRW_Angry':1},
'Mouth_Open':{'Fcl_MTH_A':1},'Surprise':{'Fcl_ALL_Surprised':1},
'Mouth_A':{'Fcl_MTH_A':1},'Mouth_I':{'Fcl_MTH_I':1},'Mouth_U':{'Fcl_MTH_U':1},'Mouth_E':{'Fcl_MTH_E':1},'Mouth_O':{'Fcl_MTH_O':1}}
def group(o):
 n=o.name
 if n=='Face':return 'Face'
 if n=='Body':return 'Body'
 if n=='Hair' or n.startswith('Hair_'):return 'Hair_Main'
 if n.startswith('Beret'):return 'ACC_MATTO_Beret'
 if n.startswith('Outfit_Boot')or n.startswith('Outfit_Ankle_Boot')or n.startswith('Outfit_Sock'):return 'Shoes'
 if any(n.startswith(x) for x in ['Outfit_Heart','Outfit_Thigh','Outfit_Left_Thigh','Outfit_Waist_Buckle','Outfit_Chest_Brooch','Outfit_Blouse_Button']):return 'Accessory'
 return 'Outfit'
def clone_evaluated(o):
 # Armature/rest deformation is kept as weights, not frozen into the mesh.
 states=[(m,m.show_viewport,m.show_render)for m in o.modifiers]
 try:
  for m,_,_ in states:
   if m.type in ['ARMATURE','NODES']:m.show_viewport=False;m.show_render=False
  bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(dg)
  me=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=dg)
 finally:
  for m,a,b in states:m.show_viewport=a;m.show_render=b
  bpy.context.view_layer.update()
 ob=bpy.data.objects.new('GamePart_'+o.name,me);game.objects.link(ob)
 for g in o.vertex_groups:ob.vertex_groups.new(name=g.name)
 me.transform(o.matrix_world)
 if o.parent_type=='BONE':
  g=ob.vertex_groups.get(o.parent_bone) or ob.vertex_groups.new(name=o.parent_bone);g.add(list(range(len(me.vertices))),1,'REPLACE')
 return ob
parts={}
for o in source:
 if o.name=='Face':
  ob=bpy.data.objects.new('GamePart_Face',o.data.copy());game.objects.link(ob)
  for g in o.vertex_groups:ob.vertex_groups.new(name=g.name)
  old=ob.data.shape_keys
  ar=np.empty(len(ob.data.vertices)*3,dtype=np.float32)
  old.key_blocks[0].data.foreach_get('co',ar);base=ar.copy().reshape(-1,3)
  original={}
  for name in {n for v in alias.values()for n in v}:
   assert old.key_blocks.get(name),name
   a=np.empty_like(ar);old.key_blocks[name].data.foreach_get('co',a);original[name]=a.reshape(-1,3)-base
  ob.shape_key_clear();ob.shape_key_add(name='Basis')
  for name,mapping in alias.items():
   key=ob.shape_key_add(name=name)
   a=base.copy()
   for n,w in mapping.items():a+=original[n]*w
   # optional look directions move iris/highlight only, not eyelids or the neutral face.
   if name.startswith('EyeLook_'):
    ids={i for p in ob.data.polygons if p.material_index in [1,2]for i in p.vertices}
    for i in ids:
     if name.endswith('L'):a[i,0]+=.004
     if name.endswith('R'):a[i,0]-=.004
     if name.endswith('Up'):a[i,2]+=.003
     if name.endswith('Down'):a[i,2]-=.003
   key.data.foreach_set('co',a.ravel());key.value=0
  (P/'expression-map.json').write_text(json.dumps(alias,indent=2),encoding='utf-8')
 else:ob=clone_evaluated(o)
 # A shared active UV name is mandatory before joining materials with separate authored maps.
 if ob.data.uv_layers:
  active=ob.data.uv_layers.active
  for layer in list(ob.data.uv_layers):
   if layer!=active:ob.data.uv_layers.remove(layer)
  active.name='UVMap'
 else:ob.data.uv_layers.new(name='UVMap')
 parts.setdefault(group(o),[]).append(ob)
 # Keep editable originals in a hidden archive, never exported.
 for c in list(o.users_collection):c.objects.unlink(o)
 src.objects.link(o)
src.hide_viewport=True;src.hide_render=True
def tris(ob):
 ob.data.calc_loop_triangles();return len(ob.data.loop_triangles)
def apply_decimate(ob,ratio):
 bpy.ops.object.select_all(action='DESELECT');ob.hide_set(False);ob.select_set(True);bpy.context.view_layer.objects.active=ob
 m=ob.modifiers.new('Silhouette-aware reduction','DECIMATE');m.ratio=max(.01,min(1,ratio));m.use_collapse_triangulate=True
 bpy.ops.object.modifier_apply(modifier=m.name)
def join(obs,name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0]
 if len(obs)>1:bpy.ops.object.join()
 ob=obs[0];ob.name=name;ob.data.name=name;ob.parent=rig;ob.matrix_world.identity()
 return ob
target={'Body':3900,'Face':7485,'Hair_Main':7200,'ACC_MATTO_Beret':2200,'Outfit':6400,'Shoes':1900,'Accessory':1300}
baseparts={}
for name,objects in parts.items():
 ob=join(objects,name+'_LOD0')
 if name!='Face':apply_decimate(ob,target[name]/tris(ob))
 baseparts[name]=ob
 # normalize/prune to four influences; discard stale zero groups later.
 for v in ob.data.vertices:
  weights=sorted([(g.group,g.weight)for g in v.groups if g.weight>1e-6],key=lambda a:-a[1])[:4]
  if not weights:weights=[(0,1)];g=ob.vertex_groups.get('J_Bip_C_Hips')or ob.vertex_groups.new(name='J_Bip_C_Hips');weights=[(g.index,1)]
  total=sum(w for _,w in weights)
  for g in list(v.groups):ob.vertex_groups[g.group].remove([v.index])
  for i,w in weights:ob.vertex_groups[i].add([v.index],w/total,'REPLACE')
 # Weight-only skirt fix: lower cloth follows thigh motion while the waist remains on Hips.
 if name=='Outfit':
  hips=ob.vertex_groups.get('J_Bip_C_Hips')or ob.vertex_groups.new(name='J_Bip_C_Hips')
  legs=[ob.vertex_groups.get('J_Bip_'+s+'_UpperLeg')or ob.vertex_groups.new(name='J_Bip_'+s+'_UpperLeg')for s in ['L','R']]
  for v in ob.data.vertices:
   x,y,z=v.co
   if .75<z<1.01 and abs(x)<.25:
    existing=sum(g.weight for g in v.groups if ob.vertex_groups[g.group].name=='J_Bip_C_Hips')
    if existing>.5:
     t=max(0,min(.72,(1.02-z)/.25*.72))
     for g in list(v.groups):ob.vertex_groups[g.group].remove([v.index])
     hips.add([v.index],1-t,'REPLACE')
     side=max(0,min(1,(x+.035)/.07));legs[0].add([v.index],t*side,'REPLACE');legs[1].add([v.index],t*(1-side),'REPLACE')
 m=ob.modifiers.new('Humanoid Skin','ARMATURE');m.object=rig
 ob['LOD']=0
# LOD face key interpolation against the unchanged LOD0 face surface.
face=baseparts['Face'];verts=[v.co.copy()for v in face.data.vertices]
face.data.calc_loop_triangles();tri=[tuple(t.vertices)for t in face.data.loop_triangles]
bv=BVHTree.FromPolygons(verts,tri,all_triangles=True)
keys={k.name:[p.co.copy()for p in k.data]for k in face.data.shape_keys.key_blocks}
def facial_lod(level,ratio):
 me=face.data.copy();ob=bpy.data.objects.new('Face_LOD'+str(level),me);game.objects.link(ob)
 for g in face.vertex_groups:ob.vertex_groups.new(name=g.name)
 ob.shape_key_clear();apply_decimate(ob,ratio)
 maps=[]
 for v in ob.data.vertices:
  near,n,i,d=bv.find_nearest(v.co)
  ids=tri[i];a,b,c=[verts[j]for j in ids]
  ba=b-a;ca=c-a;pa=near-a
  d00=ba.dot(ba);d01=ba.dot(ca);d11=ca.dot(ca);d20=pa.dot(ba);d21=pa.dot(ca)
  denom=d00*d11-d01*d01
  u=(d11*d20-d01*d21)/denom if abs(denom)>1e-16 else 0
  w=(d00*d21-d01*d20)/denom if abs(denom)>1e-16 else 0
  maps.append((ids,(1-u-w,u,w)))
 ob.shape_key_add(name='Basis')
 for name,coords in keys.items():
  if name=='Basis':continue
  k=ob.shape_key_add(name=name);k.value=0
  for i,(ids,ws) in enumerate(maps):
   delta=sum(((coords[j]-verts[j])*w for j,w in zip(ids,ws)),Vector())
   k.data[i].co=ob.data.vertices[i].co+delta
 ob.parent=rig
 m=ob.modifiers.new('Humanoid Skin','ARMATURE');m.object=rig;ob['LOD']=level
 return ob
for level,ratio in [(1,.55),(2,.25)]:
 for name,original in baseparts.items():
  if name=='Face':ob=facial_lod(level,.68 if level==1 else .35)
  else:
   ob=bpy.data.objects.new(name+'_LOD'+str(level),original.data.copy());game.objects.link(ob)
   for g in original.vertex_groups:ob.vertex_groups.new(name=g.name)
   # Keep face budget while the other parts meet the overall LOD percentage.
   apply_decimate(ob,.50 if level==1 else .21)
   ob.parent=rig;m=ob.modifiers.new('Humanoid Skin','ARMATURE');m.object=rig;ob['LOD']=level
  ob.hide_render=True;ob.hide_set(True)
# Remove only genuinely unused leaf/helper branches, keeping all Humanoid/finger and used hair bones.
human=json.loads((P/'vroid-human-map.json').read_text())
needed=set(human.values())|{'Root'}
for o in game.objects:
 for v in o.data.vertices:
  for g in v.groups:
   if g.weight>1e-6:needed.add(o.vertex_groups[g.group].name)
for name in list(needed):
 b=rig.data.bones.get(name)
 while b and b.parent:needed.add(b.parent.name);b=b.parent
removed=[b.name for b in rig.data.bones if b.name not in needed]
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
for b in list(rig.data.edit_bones):
 if b.name not in needed:rig.data.edit_bones.remove(b)
bpy.ops.object.mode_set(mode='OBJECT')
# Seven predictable render parts, no camera/light/helper exported.
meshroot=bpy.data.objects.new('Mesh',None);scene.collection.objects.link(meshroot)
for o in game.objects:o.parent=meshroot
rig.data.pose_position='POSE'
for s,sign in [('L',1),('R',-1)]:
 from mathutils import Quaternion
 p=rig.pose.bones['J_Bip_'+s+'_UpperArm'];q=p.bone.matrix_local.to_quaternion();p.rotation_quaternion=q.inverted()@Quaternion((0,1,0),sign*1.13)@q
 p=rig.pose.bones['J_Bip_'+s+'_LowerArm'];q=p.bone.matrix_local.to_quaternion();p.rotation_quaternion=q.inverted()@Quaternion((0,1,0),sign*.08)@q
bpy.context.view_layer.update()
report={'approved_source_untouched':hashlib.sha256((P.parent/'VRoidVisual/CHR_MATTO_VRoid_Visual.blend').read_bytes()).hexdigest()==(P/'approved-source.sha256').read_text(),'bones':len(rig.data.bones),'removed_unused_bones':removed,'lods':{str(i):{o.name:tris(o)for o in game.objects if o.get('LOD')==i}for i in range(3)}}
(P/'game-mesh-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MATTO_GAME.blend'))
print(json.dumps(report))
