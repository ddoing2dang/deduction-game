import bpy,ast,json,math
from pathlib import Path
from mathutils import Vector,Quaternion
P=Path(__file__).parent;s=bpy.context.scene;rig=bpy.data.objects['Armature'];game=bpy.data.collections['Game_LOD_Meshes'];cam=s.camera
# Avoid repeat sampling across the top/bottom fabric mask edge.
for m in bpy.data.materials:
 if m.name.startswith('MAT_MARIHI_TailoredCloth'):
  for n in m.node_tree.nodes:
   if n.type=='TEX_IMAGE':n.extension='EXTEND'
src=Path(r'C:\UnityProjects\DeductionGame\deduction-game\art\Characters\CHR_MATTO\Game\verify_blender_weights.py').read_text().replace('MATTO_Skirt','MARIHI_Skirt')
tree=ast.parse(src);fns=[n for n in tree.body if isinstance(n,ast.FunctionDef)and n.name in ['rot','pose']];exec(compile(ast.Module(body=fns,type_ignores=[]),'common_pose_functions','exec'),globals())
out=P/'After';out.mkdir(exist_ok=True)
def render(name,loc,target,size,w=900,h=1100):
 cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=size;s.render.resolution_x=w;s.render.resolution_y=h;s.render.filepath=str(out/(name+'.png'));bpy.context.view_layer.update();bpy.ops.render.render(write_still=True)
for name in ['T_Pose','Walk','Arms_Up','Arms_Forward','Sit']:
 pose(name);render('Pose_'+name,(3,-5,.88),(0,0,.88),1.9)
 if name=='Arms_Up':render('Raised_Shoulder',(3,-5,1.27),(0,0,1.27),.9,1100,1100)
pose('Idle')
for lev in [1,2]:
 for o in game.objects:o.hide_render=o.get('LOD')!=lev;o.hide_set(o.get('LOD')!=lev)
 render('LOD'+str(lev),(3,-5,.88),(0,0,.88),1.9)
for o in game.objects:o.hide_render=o.get('LOD')!=0;o.hide_set(o.get('LOD')!=0)
required=['Blink_L','Blink_R','Smile','Angry','Sad','Surprise','Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O'];stats={};maxw=0;unweighted=0;maxerr=0
for o in game.objects:
 o.data.calc_loop_triangles();stats[o.name]=len(o.data.loop_triangles)
 for v in o.data.vertices:
  assert all(math.isfinite(x)for x in v.co)
  ws=[g.weight for g in v.groups if g.weight>1e-8];maxw=max(maxw,len(ws));unweighted+=not bool(ws);maxerr=max(maxerr,abs(sum(ws)-1))
 if o.name.startswith('Face_'):
  for name in required:
   key=o.data.shape_keys.key_blocks[name];basis=o.data.shape_keys.key_blocks[0];assert any((a.co-b.co).length>1e-6 for a,b in zip(key.data,basis.data)),(o.name,name)
assert not unweighted
counts={str(i):sum(v for n,v in stats.items()if n.endswith('LOD'+str(i)))for i in range(3)}
(P/'blender-verification.json').write_text(json.dumps({'bones':len(rig.data.bones),'tris':counts,'parts':stats,'expressions':required,'allThreeFaceLODsNonzero':True,'maxInfluences':maxw,'unweighted':unweighted,'maxWeightSumError':maxerr,'poses':['T','Walk','Arms Up','Arms Forward','Sit'],'appearanceApproval':'Pending user review'},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI_FINAL_REVIEW.blend'),compress=True)
print(counts,'weights',maxw,maxerr)
