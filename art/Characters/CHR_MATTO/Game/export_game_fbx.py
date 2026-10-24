import bpy,json,hashlib
import numpy as np
from pathlib import Path
from bpy_extras.io_utils import axis_conversion
P=Path(r'C:\UnityProjects\DeductionGame\deduction-game\art\Characters\CHR_MATTO\Game')
U=Path(r'C:\UnityProjects\DeductionGame\deduction-game\game\UnityProject\Assets\_Game\Characters\CHR_MATTO')
T=U/'Textures';manifest=json.loads((P/'unity-materials.json').read_text())
# Normals need the same atlas transform as albedo; preserve each original face patch.
cells={0:(1024,0,512,512),1:(0,1024,1024,512),2:(1024,1024,1024,512),3:(0,0,1024,1024),4:(0,1536,1024,512),5:(1024,1536,1024,256),6:(1024,1792,1024,256),7:(1024,512,1024,256)}
original=bpy.data.objects['Face']
a=np.empty((2048,2048,4),dtype=np.float32);a[:]=(.5,.5,1,1)
for slot,(x,y,w,h)in cells.items():
 im=original.data.materials[slot].vrm_addon_extension.mtoon1.normal_texture.index.source
 if im is None or im.name=='Shader_NoneNormal':continue
 src=np.array(im.pixels[:],dtype=np.float32).reshape(im.size[1],im.size[0],4)
 yi=np.linspace(0,src.shape[0]-1,h-4).astype(int);xi=np.linspace(0,src.shape[1]-1,w-4).astype(int)
 a[y:y+h,x:x+w]=np.pad(src[yi[:,None],xi[None,:]],((2,2),(2,2),(0,0)),mode='edge')
im=bpy.data.images.get('MATTO_Face_NormalAtlas')or bpy.data.images.new('MATTO_Face_NormalAtlas',width=2048,height=2048,alpha=True)
im.colorspace_settings.name='Non-Color';im.pixels.foreach_set(a.ravel());im.filepath_raw=str(T/'MATTO_FaceNormalAtlas.png');im.save();im.pack()
for d in manifest['materials']:
 if d['name'].startswith('MAT_MATTO_Face'):
  d['normal']='MATTO_FaceNormalAtlas.png'
  bpy.data.materials[d['name']].vrm_addon_extension.mtoon1.normal_texture.index.source=im
 if d.get('normal')=='MATTO_Shader_NoneNormal.png':d['normal']=None
 for field in ['texture','normal','emission','matcap']:
  if d.get(field)=='MATTO_Shader_NoneBlack.png':d[field]=None
 if d.get('emission')=='MATTO_Shader_NoneBlack.png':d['emission']=None
 if not any(abs(c)>1e-6 for c in d['matcapColor'][:3]):d['matcap']=None
 if not any(abs(c)>1e-6 for c in d['emissionColor'][:3]):d['emission']=None
used={d[k]for d in manifest['materials']for k in ['texture','normal','emission','matcap']if d.get(k)}
manifest['images']=[i for i in manifest['images']if i['file']in used]
manifest['images'].append({'name':im.name,'file':'MATTO_FaceNormalAtlas.png','size':[2048,2048]})
manifest['images']=list({i['file']:i for i in manifest['images']}.values())
(P/'unity-materials.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
rig=bpy.data.objects.get('Armature') or bpy.data.objects['Armature.001'];rig.name='Armature'
game=bpy.data.collections['Game_LOD_Meshes'];meshroot=bpy.data.objects['Mesh'];scene=bpy.context.scene
# Model file contains no obsolete pilot actions or editor helpers.
exec(compile((P/'finalize_skin_weights.py').read_text(encoding='utf-8'),str(P/'finalize_skin_weights.py'),'exec'),{'__file__':str(P/'finalize_skin_weights.py')})
for ob in game.objects:
 if ob.data.shape_keys:
  for key in ob.data.shape_keys.key_blocks:key.value=0
original_rig=rig.data;original_meshes={o:o.data for o in game.objects}
original_roots={o:(o.parent,o.matrix_world.copy())for o in [rig,meshroot]}
conv=axis_conversion(to_forward='-Z',to_up='Y').to_4x4()
model=U/'Model/CHR_MATTO.fbx';model.parent.mkdir(exist_ok=True,parents=True)
common=dict(use_selection=True,global_scale=1.,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=False,armature_nodetype='NULL',path_mode='RELATIVE',use_mesh_modifiers=False)
saved_action=rig.animation_data.action if rig.animation_data else None
if rig.animation_data:rig.animation_data.action=None
saved_pose={p.name:(p.rotation_quaternion.copy(),p.location.copy())for p in rig.pose.bones}
for pb in rig.pose.bones:pb.rotation_quaternion=(1,0,0,0);pb.location=(0,0,0)
try:
 rig.data=original_rig.copy();rig.data.transform(conv)
 for ob,data in original_meshes.items():ob.data=data.copy();ob.data.transform(conv,shape_keys=True)
 for ob in [rig,meshroot]:ob.parent=None;ob.matrix_world=conv.inverted()
 rig.data.pose_position='REST'
 for o in scene.objects:
  o.select_set(False)
 for o in [rig,meshroot]+list(game.objects):o.hide_set(False);o.hide_render=False;o.select_set(True)
 bpy.context.view_layer.objects.active=rig;bpy.context.view_layer.update()
 bpy.ops.export_scene.fbx(filepath=str(model),object_types={'EMPTY','ARMATURE','MESH'},bake_anim=False,**common)
finally:
 tmp=rig.data;rig.data=original_rig;bpy.data.armatures.remove(tmp)
 for o,data in original_meshes.items():tmp=o.data;o.data=data;bpy.data.meshes.remove(tmp)
 for o,(parent,matrix)in original_roots.items():o.parent=parent;o.matrix_world=matrix
 rig.data.pose_position='POSE'
 if rig.animation_data:rig.animation_data.action=saved_action
 for pb in rig.pose.bones:pb.rotation_quaternion=saved_pose[pb.name][0];pb.location=saved_pose[pb.name][1]
 for o in game.objects:o.hide_set(o.get('LOD')!=0);o.hide_render=o.get('LOD')!=0
 bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MATTO_GAME.blend'))
report={'file':str(model),'bones':len(rig.data.bones),'materials':len(manifest['materials']),'textures':len(used),'approved_hash_unchanged':hashlib.sha256((P.parent/'VRoidVisual/CHR_MATTO_VRoid_Visual.blend').read_bytes()).hexdigest()==(P/'approved-source.sha256').read_text()}
(P/'export-report.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
