import bpy,math,json,ast
from pathlib import Path
from mathutils import Vector,Quaternion
P=Path(r'C:\UnityProjects\DeductionGame\deduction-game\art\Characters\CHR_MATTO\Game')
out=P/'Verification/Blender';out.mkdir(parents=True,exist_ok=True)
pres=P/'Verification/Preservation';pres.mkdir(exist_ok=True)
rig=bpy.data.objects['Armature'];scene=bpy.context.scene;cam=scene.camera;game=bpy.data.collections['Game_LOD_Meshes'];src=bpy.data.collections['Approved_Source_Archive']
saved=(rig.animation_data.action,scene.frame_current,cam.matrix_world.copy(),cam.data.ortho_scale,scene.render.resolution_x,scene.render.resolution_y,scene.render.filepath,src.hide_viewport,src.hide_render,game.hide_render)
tree=ast.parse((P/'verify_blender_weights.py').read_text(encoding='utf-8'))
ns={'bpy':bpy,'math':math,'Vector':Vector,'Quaternion':Quaternion,'rig':rig}
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef)and n.name in ['rot','pose']],type_ignores=[]),'weight_poses','exec'),ns)
def camera(face=False):
 scene.render.resolution_x=1000;scene.render.resolution_y=1000
 if face:cam.location=(0,-5,1.55);target=Vector((0,-.01,1.55));cam.data.ortho_scale=.55
 else:cam.location=(2.8,-5,1.18);target=Vector((0,0,.88));cam.data.ortho_scale=2.05
 cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
def render(path):scene.render.filepath=str(path);bpy.context.view_layer.update();bpy.ops.render.render(write_still=True)
def run():
 try:
  src.hide_viewport=True;src.hide_render=True;game.hide_render=False
  rig.animation_data.action=None;camera()
  for name in ['T_Pose','A_Pose','Idle','Walk','Arms_Up','Arms_Forward','Knee_Bend','Sit']:
   ns['pose'](name);render(out/(name+'.png'))
  for kind,frame in [('Idle',1),('Walk',7),('Investigate',25),('Suspect',25),('Deduction',25)]:
   rig.animation_data.action=bpy.data.actions['MATTO_'+kind];scene.frame_set(frame);camera();render(out/('Action_'+kind+'.png'))
  rig.animation_data.action=None;ns['pose']('A_Pose')
  for level in [0,1,2]:
   for o in game.objects:o.hide_render=o.get('LOD')!=level;o.hide_set(o.get('LOD')!=level)
   camera();render(out/('LOD'+str(level)+'.png'))
  for o in game.objects:o.hide_render=o.get('LOD')!=0;o.hide_set(o.get('LOD')!=0)
  camera(True);face=bpy.data.objects['Face_LOD0']
  for name in ['Neutral','Blink_L','Blink_R','Smile','Angry','Sad','Surprise','Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O']:
   for k in face.data.shape_keys.key_blocks:k.value=0
   if name!='Neutral':face.data.shape_keys.key_blocks[name].value=1
   render(out/('Face_'+name+'.png'))
  for k in face.data.shape_keys.key_blocks:k.value=0
  for level in [1,2]:
   for ob in game.objects:ob.hide_render=ob.get('LOD')!=level;ob.hide_set(ob.get('LOD')!=level)
   face=bpy.data.objects['Face_LOD'+str(level)];camera(True)
   for name in ['Neutral','Blink_L','Blink_R','Mouth_A']:
    for key in face.data.shape_keys.key_blocks:key.value=0
    if name!='Neutral':face.data.shape_keys.key_blocks[name].value=1
    render(out/('FaceLOD'+str(level)+'_'+name+'.png'))
   for key in face.data.shape_keys.key_blocks:key.value=0
  for ob in game.objects:ob.hide_render=ob.get('LOD')!=0;ob.hide_set(ob.get('LOD')!=0)
  for style in ['Approved','Game']:
   src.hide_viewport=style!='Approved';src.hide_render=style!='Approved';game.hide_render=style=='Approved'
   for kind in ['Full','Face']:
    camera(kind=='Face')
    if kind=='Full':
     cam.location=(0,-5,.98);cam.rotation_euler=(Vector((0,0,.87))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=1.94
     scene.render.resolution_x=900;scene.render.resolution_y=1200
    render(pres/(style+'_'+kind+'.png'))
  (out/'final-capture-complete.json').write_text(json.dumps({'poses':8,'actions':5,'lods':3,'faces':12,'preservation_pairs':2}))
 except Exception:
  import traceback
  (out/'final-capture-error.txt').write_text(traceback.format_exc())
 finally:
  src.hide_viewport=saved[7];src.hide_render=saved[8];game.hide_render=saved[9]
  for o in game.objects:o.hide_render=o.get('LOD')!=0;o.hide_set(o.get('LOD')!=0)
  rig.animation_data.action=saved[0];scene.frame_set(saved[1]);cam.matrix_world=saved[2];cam.data.ortho_scale=saved[3]
  scene.render.resolution_x=saved[4];scene.render.resolution_y=saved[5];scene.render.filepath=saved[6]
  bpy.context.view_layer.update()
 return None
bpy.app.timers.register(run,first_interval=.5)
print('Final Blender pose, facial, LOD and approved-source comparison captures scheduled.')
