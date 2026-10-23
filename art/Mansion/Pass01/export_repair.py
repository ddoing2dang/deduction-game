import bpy,os,json,bmesh
from mathutils import Vector
OUT=r"C:\UnityProjects\DeductionGame\deduction-game\art\Mansion\Pass01"
def affected(o):
 return o.type=='MESH' and (o.name.startswith(('DOOR_03_05_','DOOR_04_07_','DOOR_05_08_','R01_Hero_GrandStair','R01_Hero_Gallery','R01_Gallery_','R04_North_','R04_West_','R05_North_','R05_East_','R07_South_','R07_West_','R08_South_','R08_East_')) or o.name in ['R04_Floor','R05_Floor'])
data=[]
for o in bpy.context.scene.objects:
 if not affected(o) or len(o.data.vertices)==0:continue
 vs=[o.matrix_world@v.co for v in o.data.vertices];o.data.calc_loop_triangles()
 data.append({'name':o.name,'vertices':[[v.x,v.z,v.y] for v in vs],'triangles':[idx for tri in o.data.loop_triangles for idx in tri.vertices],'decorative':o.name.startswith(('R01_','DOOR_03_05_Support','DOOR_03_05_UnderBeam')) or '_Foundation' in o.name})
open(OUT+r"\structural_patch.json",'w').write(json.dumps({'meshes':data},indent=1))
export=bpy.data.collections['Mansion_Environment'];states={c:c.hide_viewport for c in bpy.data.collections}
for c in export.children_recursive:c.hide_viewport=False
bpy.context.view_layer.update()
for o in bpy.context.scene.objects:o.select_set(False)
for o in export.all_objects:o.select_set(True)
with bpy.context.temp_override(selected_objects=list(export.all_objects)):
 bpy.ops.export_scene.fbx(filepath=OUT+r"\Mansion_Environment_Pass01.fbx",use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
for o in bpy.context.scene.objects:o.select_set(False)
for c,v in states.items():c.hide_viewport=v
import shutil
unity=os.path.abspath(os.path.join(OUT,'../../../game/UnityProject/Assets/_Game'))
shutil.copyfile(os.path.join(OUT,'Mansion_Environment_Pass01.fbx'),os.path.join(unity,'Art/Mansion/Mansion_Environment_Pass01.fbx'))
shutil.copyfile(os.path.join(OUT,'structural_patch.json'),os.path.join(unity,'Art/Mansion/structural_patch.json'))
os.makedirs(os.path.join(unity,'Resources'),exist_ok=True)
shutil.copyfile(os.path.join(OUT,'stair_architecture_layout.json'),os.path.join(unity,'Resources/MansionStairArchitecture.json'))
bpy.ops.wm.save_as_mainfile(filepath=OUT+r"\Mansion_Environment_Pass01.blend")
print("FBX and manifest updated",len(data))
