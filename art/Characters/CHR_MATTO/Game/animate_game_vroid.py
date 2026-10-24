import bpy,math,json
from pathlib import Path
from mathutils import Quaternion,Vector,Matrix
from bpy_extras.io_utils import axis_conversion
P=Path(r'C:\UnityProjects\DeductionGame\deduction-game\art\Characters\CHR_MATTO\Game')
U=Path(r'C:\UnityProjects\DeductionGame\deduction-game\game\UnityProject\Assets\_Game\Characters\CHR_MATTO')
rig=bpy.data.objects['Armature'];scene=bpy.context.scene
rig.animation_data_clear()
for a in list(bpy.data.actions):
 if a.name.startswith('MATTO_'):bpy.data.actions.remove(a)
def turn(name,axes):
 pb=rig.pose.bones[name];rest=pb.bone.matrix_local.to_quaternion();q=Quaternion((1,0,0,0))
 for axis,angle in axes:q=Quaternion(axis,angle)@q
 pb.rotation_mode='QUATERNION';pb.rotation_quaternion=rest.inverted()@q@rest
def reach(side,target):
 bpy.context.view_layer.update();upper=rig.pose.bones['J_Bip_'+side+'_UpperArm'];lower=rig.pose.bones['J_Bip_'+side+'_LowerArm']
 start=upper.head.copy();target=Vector(target);delta=target-start;distance=min(delta.length,upper.length+lower.length-.005);direction=delta.normalized()
 along=(upper.length**2-lower.length**2+distance**2)/(2*distance)
 outward=Vector((1 if side=='L'else-1,0,-.7));bend=(outward-direction*outward.dot(direction)).normalized()
 elbow=start+direction*along+bend*max(0,upper.length**2-along**2)**.5
 for pb,a,b in [(upper,start,elbow),(lower,elbow,target)]:
  original=pb.bone.tail_local-pb.bone.head_local
  rotation=original.rotation_difference((b-a).normalized())@pb.bone.matrix_local.to_quaternion()
  pb.matrix=Matrix.Translation(a)@rotation.to_matrix().to_4x4();bpy.context.view_layer.update()
def pose(kind,t):
 for pb in rig.pose.bones:pb.rotation_mode='QUATERNION';pb.rotation_quaternion=(1,0,0,0);pb.location=(0,0,0);pb.scale=(1,1,1)
 phase=math.tau*t
 turn('J_Bip_C_Chest',[((1,0,0),.008*math.sin(phase))])
 for sign,side in [(1,'L'),(-1,'R')]:
  turn('J_Bip_'+side+'_UpperArm',[((0,1,0),sign*1.13)])
  turn('J_Bip_'+side+'_LowerArm',[((0,1,0),sign*.08)])
  for finger in ['Index','Middle','Ring','Little']:
   for seg in [1,2,3]:turn('J_Bip_'+side+'_'+finger+str(seg),[((0,1,0),sign*.12)])
 if kind=='Walk':
  for sign,side in [(1,'L'),(-1,'R')]:
   wave=math.sin(phase+(0 if sign==1 else math.pi))
   turn('J_Bip_'+side+'_UpperLeg',[((1,0,0),-.38*wave)])
   turn('J_Bip_'+side+'_LowerLeg',[((1,0,0),.60*max(0,wave))])
   turn('J_Bip_'+side+'_Foot',[((1,0,0),-.12*wave)])
   turn('J_Bip_'+side+'_UpperArm',[((0,1,0),sign*1.13),((1,0,0),.20*wave)])
   for section,ratio in [('Front',.88),('Back',.30)]:turn('MATTO_Skirt_'+section+'_'+side,[((1,0,0),-.38*wave*ratio)])
  rig.pose.bones['J_Bip_C_Hips'].location=(0,-.018,0)
 elif kind=='Investigate':
  turn('J_Bip_C_Head',[((1,0,0),.04+.015*math.sin(phase))]);reach('L',(.025,-.22,1.14))
 elif kind=='Suspect':
  reach('R',(-.35,-.34,1.17));turn('J_Bip_C_Head',[((0,0,1),-.05)])
  for i in [1,2,3]:turn('J_Bip_R_Index'+str(i),[])
 elif kind=='Deduction':
  reach('R',(-.025,-.19,1.295));turn('J_Bip_C_Head',[((0,0,1),.05)])
 return None
actions=[]
for kind,frames in [('Idle',60),('Walk',24),('Investigate',60),('Suspect',60),('Deduction',60)]:
 a=bpy.data.actions.new('MATTO_'+kind);rig.animation_data_create();rig.animation_data.action=a;a.use_fake_user=True
 for frame in range(1,frames+2,3):
  pose(kind,(frame-1)/frames)
  for pb in rig.pose.bones:
   pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=pb.name)
   if pb.name=='J_Bip_C_Hips':pb.keyframe_insert(data_path='location',frame=frame,group=pb.name)
 actions.append(a)
# Export matching skeleton data with the exact same axis convention as the model.
original=rig.data;matrix=rig.matrix_world.copy();parent=rig.parent
conv=axis_conversion(to_forward='-Z',to_up='Y').to_4x4()
try:
 rig.data=original.copy();rig.data.transform(conv);rig.parent=None;rig.matrix_world=conv.inverted()
 rig.data.pose_position='POSE';rig.animation_data.action=actions[0]
 bpy.ops.object.select_all(action='DESELECT');rig.hide_set(False);rig.select_set(True);bpy.context.view_layer.objects.active=rig
 bpy.context.view_layer.update()
 bpy.ops.export_scene.fbx(filepath=str(U/'Animations/MATTO_VRoid_Actions.fbx'),use_selection=True,object_types={'ARMATURE'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=False,armature_nodetype='NULL',use_mesh_modifiers=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,bake_anim_force_startend_keying=True)
finally:
 tmp=rig.data;rig.data=original;bpy.data.armatures.remove(tmp);rig.parent=parent;rig.matrix_world=matrix
rig.animation_data.action=actions[0];scene.frame_set(1);rig.data.pose_position='POSE'
scene.render.fps=30;scene.frame_start=1;scene.frame_end=60
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MATTO_GAME.blend'))
(P/'representative-animation-report.json').write_text(json.dumps({'clips':[a.name for a in actions],'source':'Current approved VRoid skeleton; five representative roles only','new_final_18_clip_library':False},indent=2))
print('Five current-skeleton representative motions exported.')
