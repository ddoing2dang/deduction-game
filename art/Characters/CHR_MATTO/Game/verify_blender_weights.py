import bpy,math,json
from pathlib import Path
from mathutils import Vector,Quaternion
P=Path(r'C:\UnityProjects\DeductionGame\deduction-game\art\Characters\CHR_MATTO\Game')
out=P/'Verification/Blender';out.mkdir(parents=True,exist_ok=True)
scene=bpy.context.scene;rig=bpy.data.objects['Armature'];cam=scene.camera
saved={p.name:(p.rotation_quaternion.copy(),p.location.copy())for p in rig.pose.bones}
settings=(cam.matrix_world.copy(),cam.data.ortho_scale,scene.render.resolution_x,scene.render.resolution_y,scene.render.filepath)
def rot(bone,axis,angle):
 pb=rig.pose.bones[bone];q=pb.bone.matrix_local.to_quaternion();pb.rotation_quaternion=q.inverted()@Quaternion(axis,angle)@q
def pose(kind):
 for p in rig.pose.bones:p.rotation_quaternion=(1,0,0,0);p.location=(0,0,0);p.scale=(1,1,1)
 if kind=='T_Pose':return
 for s,sign in [('L',1),('R',-1)]:
  rot('J_Bip_'+s+'_UpperArm',(0,1,0),sign*1.13)
  rot('J_Bip_'+s+'_LowerArm',(0,1,0),sign*.08)
 if kind=='Walk':
  rot('J_Bip_L_UpperLeg',(1,0,0),-.38);rot('J_Bip_L_LowerLeg',(1,0,0),.5)
  rot('J_Bip_R_UpperLeg',(1,0,0),.32)
 if kind=='Arms_Up':
  for s,sign in [('L',1),('R',-1)]:rot('J_Bip_'+s+'_UpperArm',(0,1,0),-sign*1.35)
 if kind=='Arms_Forward':
  for s,sign in [('L',1),('R',-1)]:
   rot('J_Bip_'+s+'_UpperArm',(0,0,1),-sign*math.pi/2)
   rot('J_Bip_'+s+'_LowerArm',(1,0,0),-.25)
 if kind in ['Knee_Bend','Sit']:
  for s in ['L','R']:
   rot('J_Bip_'+s+'_UpperLeg',(1,0,0),-1.15 if kind=='Sit' else -.65)
   rot('J_Bip_'+s+'_LowerLeg',(1,0,0),1.35 if kind=='Sit' else 1.1)
 # Garment bone follow is mirrored by the runtime visual component.
 for side in ['L','R']:
  angle=(1.15 if kind=='Sit' else .65 if kind=='Knee_Bend' else .38 if kind=='Walk' and side=='L' else -.32 if kind=='Walk' else 0)
  rot('MATTO_Skirt_Front_'+side,(1,0,0),-max(-.25,angle)*.88)
  rot('MATTO_Skirt_Back_'+side,(1,0,0),-max(-.25,angle)*.30)
 bpy.context.view_layer.update()
def run():
 try:
  scene.render.resolution_x=1000;scene.render.resolution_y=1000
  cam.data.ortho_scale=2.05;cam.location=(2.8,-5,1.18);cam.rotation_euler=(Vector((0,0,.88))-cam.location).to_track_quat('-Z','Y').to_euler()
  for name in ['T_Pose','A_Pose','Idle','Walk','Arms_Up','Arms_Forward','Knee_Bend','Sit']:
   pose(name);scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
  pose('Idle')
  cam.data.ortho_scale=.58;cam.location=(0,-4,1.56);cam.rotation_euler=(Vector((0,-.01,1.55))-cam.location).to_track_quat('-Z','Y').to_euler()
  face=bpy.data.objects['Face_LOD0']
  for name in ['Neutral','Blink_L','Blink_R','Smile','Angry','Sad','Surprise','Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O']:
   for k in face.data.shape_keys.key_blocks:k.value=0
   if name!='Neutral':face.data.shape_keys.key_blocks[name].value=1
   bpy.context.view_layer.update();scene.render.filepath=str(out/('Face_'+name+'.png'));bpy.ops.render.render(write_still=True)
  for k in face.data.shape_keys.key_blocks:k.value=0
  (out/'complete.json').write_text(json.dumps({'poses':8,'faces':12}))
 except Exception:
  import traceback
  (out/'error.txt').write_text(traceback.format_exc())
 finally:
  for p in rig.pose.bones:p.rotation_quaternion=saved[p.name][0];p.location=saved[p.name][1]
  cam.matrix_world=settings[0];cam.data.ortho_scale=settings[1];scene.render.resolution_x=settings[2];scene.render.resolution_y=settings[3];scene.render.filepath=settings[4]
  bpy.context.view_layer.update()
 return None
bpy.app.timers.register(run,first_interval=.5)
print('Game-only weight and facial renders scheduled.')
