import bpy,json,hashlib
from pathlib import Path
from bpy_extras.io_utils import axis_conversion
P=Path(__file__).parent
U=P.parents[4]/'game/UnityProject/Assets/_Game/Characters/CHR_MARIHI'
manifest=json.loads((P/'unity-materials.json').read_text())
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
model=U/'Model/CHR_MARIHI.fbx';model.parent.mkdir(exist_ok=True,parents=True)
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
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI_GAME.blend'))

print('FBX exported',str(model))
