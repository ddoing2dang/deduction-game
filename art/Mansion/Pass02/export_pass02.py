"""Run through Blender MCP only."""
import bpy,os,json,shutil
OUT=os.path.dirname(__file__);parts=[];objects=[]
for o in bpy.context.scene.objects:
 if o.type!='MESH' or not len(o.data.vertices) or 'StructureRole' not in o:continue
 color=list(o.data.materials[0].diffuse_color) if o.data.materials else [.5,.5,.5,1]
 parts.append({'name':o.name,'role':o['StructureRole'],'room':int(o['StructureRoom']),'door':o['StructureDoor'],'side':o['StructureSide'],
 'collision':bool(o['StructureCollision']),'hidden':'_COLLISION_' in o.name,'color':dict(zip(['r','g','b','a'],color)),
 'shared':bool(o.get('P02Shared',False)),'meshKey':o.data.name if o.get('P02Shared') else '', 'assembly':o.get('ArtAssembly','')})
 objects.append(o)
layout=json.load(open(os.path.join(OUT,'../Pass01/StructureAudit/layout.json')))
manifest={'version':2,'modelFile':'Mansion_Environment_Pass02.fbx','parts':parts,'layout':layout}
json.dump(manifest,open(os.path.join(OUT,'environment_manifest.json'),'w'),indent=1)
states={c:c.hide_viewport for c in bpy.data.collections}
try:
 for c in states:c.hide_viewport=False
 bpy.context.view_layer.update()
 with bpy.context.temp_override(selected_objects=objects):
  bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'Mansion_Environment_Pass02.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
finally:
 for c,v in states.items():c.hide_viewport=v
unity=os.path.abspath(os.path.join(OUT,'../../../game/UnityProject/Assets/_Game/Art/Mansion'))
for name in ['Mansion_Environment_Pass02.fbx','environment_manifest.json']:shutil.copyfile(os.path.join(OUT,name),os.path.join(unity,name))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Mansion_Environment_Pass02.blend'))
print('Exported Pass02 full environment:',len(parts),'objects; shared instances',sum(p['shared'] for p in parts))
