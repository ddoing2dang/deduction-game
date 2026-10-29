import bpy,json,math
from pathlib import Path
from mathutils import Vector,Quaternion
P=Path(__file__).parent;stage=globals().get('STAGE','Before');R=P/stage;R.mkdir(exist_ok=True)
s=bpy.context.scene;c=s.camera;rig=bpy.data.objects['Armature'];game=bpy.data.collections['Game_LOD_Meshes']
rig.animation_data_clear();rig.data.pose_position='POSE'
for pb in rig.pose.bones:pb.rotation_mode='QUATERNION';pb.rotation_quaternion=(1,0,0,0);pb.location=(0,0,0);pb.scale=(1,1,1)
for side,sign in [('L',1),('R',-1)]:
 for part,ang in [('UpperArm',1.13),('LowerArm',.08)]:
  pb=rig.pose.bones['J_Bip_'+side+'_'+part];q=pb.bone.matrix_local.to_quaternion();pb.rotation_quaternion=q.inverted()@Quaternion((0,1,0),ang*sign)@q
for o in game.objects:o.hide_render=o.get('LOD')!=0;o.hide_set(o.get('LOD')!=0)
for o in game.objects:
 if o.data.shape_keys:
  for k in o.data.shape_keys.key_blocks:k.value=0
views=[('Face_Front',(0,-4,1.445),(0,0,1.445),.48,1000,1000),('Face_45',(4,-4,1.445),(0,0,1.445),.48,1000,1000),('Face_Side90',(4,0,1.445),(0,0,1.445),.48,1000,1000),('Full_Front',(0,-4,.83),(0,0,.83),1.88,900,1250),('Full_45',(4,-4,.83),(0,0,.83),1.88,900,1250),('Outfit_Close',(2.7,-4,1.10),(0,0,1.10),.83,1100,1100),('Full_Back',(0,4,.83),(0,0,.83),1.88,900,1250),('Upper_Front',(0,-4,1.15),(0,0,1.15),.68,1000,1000),('Upper_45',(4,-4,1.15),(0,0,1.15),.68,1000,1000),('Upper_Side90',(4,0,1.15),(0,0,1.15),.68,1000,1000)]
receipt={'views':views,'lights':{o.name:{'matrix':[list(row)for row in o.matrix_world],'energy':o.data.energy,'color':list(o.data.color),'size':getattr(o.data,'size',0)}for o in s.objects if o.type=='LIGHT'},'world':s.world.name,'worldNodes':[(n.type,[str(i.default_value)for i in n.inputs if hasattr(i,'default_value')])for n in s.world.node_tree.nodes],'pose':{p.name:[list(row)for row in p.matrix_basis]for p in rig.pose.bones},'viewTransform':s.view_settings.view_transform,'exposure':s.view_settings.exposure,'gamma':s.view_settings.gamma}
(R/'capture-settings.json').write_text(json.dumps(receipt,indent=2))
def shot(name,loc,target,size,w,h):
 c.location=loc;c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.ortho_scale=size;s.render.resolution_x=w;s.render.resolution_y=h;s.render.resolution_percentage=100;s.render.filepath=str(R/(name+'.png'));bpy.context.view_layer.update();bpy.ops.render.render(write_still=True)
for args in views:shot(*args)
face=bpy.data.objects['Face_LOD0']
for name in ['Neutral','Blink_L','Blink_R','Smile','Angry','Sad','Surprise','Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O']:
 for k in face.data.shape_keys.key_blocks:k.value=0
 if name!='Neutral':face.data.shape_keys.key_blocks[name].value=1
 shot('Expression_'+name,(0,-4,1.445),(0,0,1.445),.48,700,700)
for k in face.data.shape_keys.key_blocks:k.value=0
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI_FINAL_REVIEW.blend'),compress=True)
print(stage,'actual renders saved; lighting/camera/pose receipt recorded')
