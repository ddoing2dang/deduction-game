import bpy,math,json,ast,traceback
from pathlib import Path
from mathutils import Vector,Quaternion
P=Path(__file__).parent;R=P/'Review';R.mkdir(exist_ok=True);s=bpy.context.scene;c=s.camera;rig=bpy.data.objects['Armature'];game=bpy.data.collections['Game_LOD_Meshes']
t=ast.parse((P.parents[2]/'CHR_MATTO/Game/verify_blender_weights.py').read_text())
code=ast.unparse(ast.Module(body=[n for n in t.body if isinstance(n,ast.FunctionDef)and n.name in ['rot','pose']],type_ignores=[])).replace('MATTO_Skirt','MARIHI_Skirt')
exec(compile(code,'shared_pose_tests','exec'),globals())
def view(name,loc,target,size,w=900,h=1200):
 c.location=loc;c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.ortho_scale=size;s.render.resolution_x=w;s.render.resolution_y=h;s.render.filepath=str(R/(name+'.png'));bpy.context.view_layer.update();bpy.ops.render.render(write_still=True)
def run():
 try:
  for ob in game.objects:ob.hide_render=ob.get('LOD')!=0;ob.hide_set(ob.get('LOD')!=0)
  pose('Idle')
  for name,loc,target,size in [('01_Front',(0,-4,.83),(0,0,.83),1.88),('02_45',(4,-4,.83),(0,0,.83),1.88),('03_Side90',(4,0,.83),(0,0,.83),1.88),('04_Back',(0,4,.83),(0,0,.83),1.88),('05_Face',(0,-4,1.445),(0,0,1.445),.48),('06_Face45',(4,-4,1.445),(0,0,1.445),.48),('07_Root',(4,-.3,1.54),(.12,.035,1.54),.32)]:view(name,loc,target,size)
  face=bpy.data.objects['Face_LOD0']
  for name in ['Neutral','Blink_L','Blink_R','Smile','Angry','Sad','Surprise','Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O']:
   for k in face.data.shape_keys.key_blocks:k.value=0
   if name!='Neutral':face.data.shape_keys.key_blocks[name].value=1
   view('Expression_'+name,(0,-4,1.445),(0,0,1.445),.48,800,800)
  for k in face.data.shape_keys.key_blocks:k.value=0
  for name in ['T_Pose','Idle','Walk','Arms_Up','Arms_Forward','Knee_Bend','Sit']:
   pose(name);view('Pose_'+name,(3,-4,1.0),(0,0,.86),1.95,900,1100)
  pose('Idle')
  for level in [0,1,2]:
   for ob in game.objects:ob.hide_render=ob.get('LOD')!=level;ob.hide_set(ob.get('LOD')!=level)
   view('LOD'+str(level),(2,-4,.90),(0,0,.85),1.9,900,1200)
  (R/'capture-status.txt').write_text('CAPTURED; DIRECT VISUAL REVIEW REQUIRED')
 except Exception:(R/'capture-status.txt').write_text(traceback.format_exc())
 finally:
  for ob in game.objects:ob.hide_render=ob.get('LOD')!=0;ob.hide_set(ob.get('LOD')!=0)
  pose('Idle');bpy.context.view_layer.update()
 return None
bpy.app.timers.register(run,first_interval=.3);print('Actual game-model view, expression, LOD and deformation captures scheduled')
