import bpy,math,json
from mathutils import Vector
from pathlib import Path
P=Path(r'C:\UnityProjects\DeductionGame\deduction-game\art\Characters\CHR_MATTO\Game')
rig=bpy.data.objects['Armature.001']
# Four small cloth bones pivot at the garment waist, unlike the thigh pivot below it.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
for end in ['Front','Back']:
 for s,sign in [('L',1),('R',-1)]:
  n='MATTO_Skirt_'+end+'_'+s
  b=rig.data.edit_bones.get(n)or rig.data.edit_bones.new(n)
  b.head=(sign*.065,-.105 if end=='Front' else .09,1.068);b.tail=(sign*.065,-.105 if end=='Front' else .09,.79);b.parent=rig.data.edit_bones['J_Bip_C_Hips'];b.use_deform=True
bpy.ops.object.mode_set(mode='OBJECT')
for ob in bpy.data.collections['Game_LOD_Meshes'].objects:
 if not ob.name.startswith(('Outfit_','Accessory_')):continue
 for v in ob.data.vertices:
  x,y,z=v.co
  weights={ob.vertex_groups[g.group].name:g.weight for g in v.groups}
  # Includes the skirt's hems, straps and heart; excludes the actual thigh strap.
  if .74<z<1.04 and abs(x)<.26 and (weights.get('J_Bip_C_Hips',0)>.20 or any(n.startswith('MATTO_Skirt_')for n in weights)):
   t=max(0,min(1,(1.055-z)/.22))
   side=max(0,min(1,(x+.04)/.08));front=max(0,min(1,(.025-y)/.05))
   ws={'J_Bip_C_Hips':1-t,
       'MATTO_Skirt_Front_L':t*side*front,'MATTO_Skirt_Front_R':t*(1-side)*front,
       'MATTO_Skirt_Back_L':t*side*(1-front),'MATTO_Skirt_Back_R':t*(1-side)*(1-front)}
   ws=dict(sorted([(n,w)for n,w in ws.items()if w>1e-6],key=lambda v:-v[1])[:4]);total=sum(ws.values())
   for g in list(v.groups):ob.vertex_groups[g.group].remove([v.index])
   for n,w in ws.items():
    g=ob.vertex_groups.get(n)or ob.vertex_groups.new(name=n);g.add([v.index],w/total,'REPLACE')
# Store the precise neutral rest pose; appearance meshes were not moved.
(P/'cloth-bones.json').write_text(json.dumps({'bones':['MATTO_Skirt_'+k+'_'+s for k in ['Front','Back']for s in ['L','R']],'waist_pivot_z':1.068,'front_rotation_ratio':.88,'back_rotation_ratio':.30}),encoding='utf-8')
p=P/'verify_blender_weights.py';s=p.read_text(encoding='utf-8')
s=s.replace("bpy.context.view_layer.update()\ndef run():","""# Garment bone follow is mirrored by the runtime visual component.
 for side in ['L','R']:
  angle=(1.15 if kind=='Sit' else .65 if kind=='Knee_Bend' else .38 if kind=='Walk' and side=='L' else -.32 if kind=='Walk' else 0)
  rot('MATTO_Skirt_Front_'+side,(1,0,0),-max(-.25,angle)*.88)
  rot('MATTO_Skirt_Back_'+side,(1,0,0),-max(-.25,angle)*.30)
 bpy.context.view_layer.update()
def run():""")
p.write_text(s,encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MATTO_GAME.blend'))
exec(compile(s,str(p),'exec'),{'__file__':str(p)})
print('Four waist-pivot cloth bones and weight-only correction added; approved neutral coordinates unchanged.')
