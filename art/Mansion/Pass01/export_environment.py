import bpy,os,json,shutil
OUT=os.path.dirname(__file__);AUDIT=os.path.join(OUT,'StructureAudit')
parts=[];objects=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH' or len(o.data.vertices)==0 or 'StructureRole' not in o:continue
 r=o['StructureRole'];room=int(o['StructureRoom']);door=o['StructureDoor'];side=o['StructureSide']
 color=list(o.data.materials[0].diffuse_color) if o.data.materials else [.6,.55,.45,1]
 parts.append({'name':o.name,'role':r,'room':room,'door':door,'side':side,'collision':bool(o['StructureCollision']),'color':{'r':color[0],'g':color[1],'b':color[2],'a':color[3]},'hidden':'_COLLISION_' in o.name})
 objects.append(o)
manifest={'version':1,'parts':parts,'layout':json.load(open(os.path.join(AUDIT,'layout.json')))}
json.dump(manifest,open(os.path.join(AUDIT,'environment_manifest.json'),'w'),indent=1)
states={c:c.hide_viewport for c in bpy.data.collections}
for c in bpy.data.collections:c.hide_viewport=False
bpy.context.view_layer.update()
with bpy.context.temp_override(selected_objects=objects):
 bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'Mansion_Environment_Pass01.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
for c,v in states.items():c.hide_viewport=v
unity=os.path.abspath(os.path.join(OUT,'../../../game/UnityProject/Assets/_Game/Art/Mansion'))
shutil.copyfile(os.path.join(OUT,'Mansion_Environment_Pass01.fbx'),os.path.join(unity,'Mansion_Environment_Pass01.fbx'))
shutil.copyfile(os.path.join(AUDIT,'environment_manifest.json'),os.path.join(unity,'environment_manifest.json'))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Mansion_Environment_Pass01.blend'))
print('Full cleaned environment exported:',len(parts),'classified meshes, 11 routes + mapped secret panels')
