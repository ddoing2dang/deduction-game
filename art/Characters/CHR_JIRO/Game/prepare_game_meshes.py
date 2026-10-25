import bpy,math,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).parent;scene=bpy.context.scene;rig=bpy.data.objects['Armature']
rig.animation_data_clear();rig.data.pose_position='REST'
for p in rig.pose.bones:p.rotation_mode='QUATERNION';p.rotation_quaternion=(1,0,0,0);p.location=(0,0,0);p.scale=(1,1,1)
bpy.context.view_layer.update()
source=[o for o in scene.objects if o.type=='MESH']
for oldname in ['Game_LOD_Meshes','Approved_Source_Archive']:
 c=bpy.data.collections.get(oldname)
 if c and len(c.objects)==0:bpy.data.collections.remove(c)
game=bpy.data.collections.new('Game_LOD_Meshes');scene.collection.children.link(game)
src=bpy.data.collections.new('Approved_Source_Archive');scene.collection.children.link(src)
def group(o):
 n=o.name
 if n in ['Face','Body']:return n
 if n.startswith('Hair_'):return 'Hair_Main'
 if n.startswith('ACC_JIRO_'):return 'ACC_JIRO_Flower'
 if n.startswith('Shoes_')or n.startswith('Outfit_Sock'):return 'Shoes'
 if n.startswith('Accessory_'):return 'Accessory'
 return 'Outfit'
parts={}
for o in source:
 ob=bpy.data.objects.new('GamePart_'+o.name,o.data.copy());game.objects.link(ob)
 for g in o.vertex_groups:ob.vertex_groups.new(name=g.name)
 ob.data.transform(o.matrix_world,shape_keys=True)
 if ob.data.uv_layers:
  active=ob.data.uv_layers.active
  for layer in list(ob.data.uv_layers):
   if layer!=active:ob.data.uv_layers.remove(layer)
  active.name='UVMap'
 else:ob.data.uv_layers.new(name='UVMap')
 parts.setdefault(group(o),[]).append(ob)
 for c in list(o.users_collection):c.objects.unlink(o)
 src.objects.link(o)
src.hide_viewport=True;src.hide_render=True
def tris(ob):
 ob.data.calc_loop_triangles();return len(ob.data.loop_triangles)
def apply_decimate(ob,ratio):
 bpy.ops.object.select_all(action='DESELECT');ob.hide_set(False);ob.select_set(True);bpy.context.view_layer.objects.active=ob
 if False:
  protect=ob.vertex_groups.get('ReductionProtection')or ob.vertex_groups.new(name='ReductionProtection')
  for v in ob.data.vertices:
   x,y,z=v.co
   protected=z>1.40 or abs(x)>.17
   protect.add([v.index],1 if protected else .12,'REPLACE')
 m=ob.modifiers.new('Silhouette-aware reduction','DECIMATE');m.ratio=max(.01,min(1,ratio));m.use_collapse_triangulate=True
 bpy.ops.object.modifier_apply(modifier=m.name)
def join(obs,name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0]
 if len(obs)>1:bpy.ops.object.join()
 ob=obs[0];ob.name=name;ob.data.name=name;ob.parent=rig;ob.matrix_world.identity()
 return ob
target={'Hair_Main':10300,'Outfit':8200,'Shoes':1900,'Accessory':550,'ACC_JIRO_Flower':700}
baseparts={}
for name,objects in parts.items():
 ob=join(objects,name+'_LOD0')
 if name not in ['Body','Face']and tris(ob)>target[name]:apply_decimate(ob,target[name]/tris(ob))
 baseparts[name]=ob;m=ob.modifiers.new('Humanoid Skin','ARMATURE');m.object=rig;ob['LOD']=0
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
for level in [1,2]:
 for name,original in baseparts.items():
  if name=='Face':ob=facial_lod(level,.68 if level==1 else .35)
  else:
   ob=bpy.data.objects.new(name+'_LOD'+str(level),original.data.copy());game.objects.link(ob)
   for g in original.vertex_groups:ob.vertex_groups.new(name=g.name)
   ratio=(.50 if level==1 else .20)if name in ['Hair_Main','Outfit']else(.55 if level==1 else .26)if name=='Body'else(.75 if level==1 else .50)if name=='Shoes'else 1
   if ratio<1:apply_decimate(ob,ratio)
   ob.parent=rig;m=ob.modifiers.new('Humanoid Skin','ARMATURE');m.object=rig;ob['LOD']=level
  ob.hide_render=True;ob.hide_set(True)
meshroot=bpy.data.objects['Mesh']
for o in game.objects:o.parent=meshroot
import bpy,math,json
from mathutils import Vector
from pathlib import Path
P=Path(__file__).parent
rig=bpy.data.objects['Armature']
# Four small cloth bones pivot at the garment waist, unlike the thigh pivot below it.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
for end in ['Front','Back']:
 for s,sign in [('L',1),('R',-1)]:
  n='JIRO_Skirt_'+end+'_'+s
  b=rig.data.edit_bones.get(n)or rig.data.edit_bones.new(n)
  b.head=(sign*.065,-.105 if end=='Front' else .09,.985);b.tail=(sign*.065,-.105 if end=='Front' else .09,.745);b.parent=rig.data.edit_bones['J_Bip_C_Hips'];b.use_deform=True
bpy.ops.object.mode_set(mode='OBJECT')
for ob in bpy.data.collections['Game_LOD_Meshes'].objects:
 if not ob.name.startswith(('Outfit_','Accessory_')):continue
 for v in ob.data.vertices:
  x,y,z=v.co
  weights={ob.vertex_groups[g.group].name:g.weight for g in v.groups}
  # Includes the skirt's hems, straps and heart; excludes the actual thigh strap.
  if .71<z<.985 and abs(x)<.26 and (weights.get('J_Bip_C_Hips',0)>.20 or any(n.startswith('JIRO_Skirt_')for n in weights)):
   t=max(0,min(1,(.985-z)/.24))
   side=max(0,min(1,(x+.04)/.08));front=max(0,min(1,(.025-y)/.05))
   ws={'J_Bip_C_Hips':1-t,
       'JIRO_Skirt_Front_L':t*side*front,'JIRO_Skirt_Front_R':t*(1-side)*front,
       'JIRO_Skirt_Back_L':t*side*(1-front),'JIRO_Skirt_Back_R':t*(1-side)*(1-front)}
   ws=dict(sorted([(n,w)for n,w in ws.items()if w>1e-6],key=lambda v:-v[1])[:4]);total=sum(ws.values())
   for g in list(v.groups):ob.vertex_groups[g.group].remove([v.index])
   for n,w in ws.items():
    g=ob.vertex_groups.get(n)or ob.vertex_groups.new(name=n);g.add([v.index],w/total,'REPLACE')
# Store the precise neutral rest pose; appearance meshes were not moved.
(P/'cloth-bones.json').write_text(json.dumps({'bones':['JIRO_Skirt_'+k+'_'+s for k in ['Front','Back']for s in ['L','R']],'waist_pivot_z':.985,'front_rotation_ratio':.88,'back_rotation_ratio':.30}),encoding='utf-8')

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
rig.data.pose_position='POSE'
from mathutils import Quaternion
for s,sign in [('L',1),('R',-1)]:
 for suffix,angle in [('UpperArm',1.20),('LowerArm',.07)]:
  p=rig.pose.bones['J_Bip_'+s+'_'+suffix];q=p.bone.matrix_local.to_quaternion();p.rotation_quaternion=q.inverted()@Quaternion((0,1,0),sign*angle)@q
report={'bones':len(rig.data.bones),'removed_unused_bones':removed,'lods':{str(i):{o.name:tris(o)for o in game.objects if o.get('LOD')==i}for i in range(3)}}
(P/'game-mesh-report.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_JIRO_GAME.blend'));print(json.dumps(report))
