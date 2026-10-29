import bpy,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent;A=bpy.app.driver_namespace['GoldArchitecture'];r=json.loads((P/'straight-entrance-authoring.json').read_text());cx=r['doorCenterX'];front=r['exteriorPortalY'];rear=r['doorWallY']+.02;w=r['clearWidth'];roof=bpy.data.objects['VG_Entry_ServiceCrossingWallSoffit']
roof.data.materials.clear();roof.data.materials.append(bpy.data.materials['Gold_WarmPlaster'])
for o in bpy.context.scene.objects:
 if o.name.startswith('DOOR_04_05_Railing_'):o['GoldRenderHidden']=True;o.hide_render=True
# Flared rear floor remains a single continuous surface, all the way to the original broad door surround.
door=bpy.data.objects['VG_DecorativeFrontDoor_ClosedDoubleLeaf'];a,b=bpy.app.driver_namespace['GoldAuthoring']['bbox']([door])
outline=[(cx-w/2,front-.28),(cx+w/2,front-.28),(cx+w/2,r['doorWallY']-1.30),(b.x+.03,rear),(a.x-.03,rear),(cx-w/2,r['doorWallY']-1.30)]
floor=bpy.data.objects['VG_Entry_Floor'];inv=floor.matrix_world.inverted();verts=[inv@Vector((x,y,z))for z in [0,.012]for x,y in outline];n=len(outline);faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n)for i in range(n)]
me=bpy.data.meshes.new('ContinuousEntranceFloorWithRearSplay');me.from_pydata(verts,[],faces);me.materials.append(bpy.data.materials['Gold_HallMarble']);floor.data=me;A['world_uv'](floor,2.0)
# Thin gilt ceiling coffers finish the crossover canopy on both faces, rather than a dark slab.
y0,y1=r['serviceCrossingY'];items=[]
for z in [3.352,3.608]:
 for x in [cx-w/2+.09,cx+w/2-.09]:items.append(A['box']('Entry_CeilingCoffer',(x,(y0+y1)/2,z),(.025,y1-y0-.18,.009),bpy.data.materials['Gold_AgedBrass'],bevel=0))
 for y in [y0+.09,y1-.09]:items.append(A['box']('Entry_CeilingCoffer',(cx,y,z),(w-.18,.025,.009),bpy.data.materials['Gold_AgedBrass'],bevel=0))
for o in items:
 o['StructureRole']='Exterior';o['StructureRoom']=0;o['StructureCollision']=False;o['StructureDoor']='';o['ArtAssembly']='StraightEntrance';o['StraightEntrance']=True
bpy.ops.object.select_all(action='DESELECT')
for o in items:o.select_set(True)
bpy.context.view_layer.objects.active=items[0];bpy.ops.object.join();items[0].name='VG_Entry_CrossoverCeilingCoffers'
r['hiddenLegacyRails']=[o.name for o in bpy.context.scene.objects if o.name.startswith('DOOR_04_05_Railing_')];r['floorRearSplay']=True;(P/'straight-entrance-authoring.json').write_text(json.dumps(r,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'),compress=True)
