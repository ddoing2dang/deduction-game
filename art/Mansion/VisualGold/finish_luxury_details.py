import bpy,json
from pathlib import Path
P=Path(__file__).parent;A=bpy.app.driver_namespace['GoldArchitecture'];bb=bpy.app.driver_namespace['GoldAuthoring']['bbox']
assert not bpy.data.objects.get('VG_Lux_HallStoneTreads')
dark=bpy.data.materials['Gold_Walnut'].copy();dark.name='Gold_PolishedDarkWalnut';dark['GoldTint']=[.25,.16,.105]
for n in dark.node_tree.nodes:
 if n.type=='MIX_RGB'and n.blend_type=='MULTIPLY':n.inputs[2].default_value=(.25,.16,.105,1)
obs=[]
for o in list(bpy.context.scene.objects):
 if o.get('StructureRoom')==1 and o.get('StructureRole')in ['Stair','Gallery','Railing']:
  o.data.materials.clear();o.data.materials.append(dark)
 if not o.name.startswith('R01_Hero_GrandStair_'):continue
 a,b=bb([o]);cx=(a.x+b.x)/2;cy=(a.y+b.y)/2
 for label,x,sx,z,sz,mat in [('StoneTread',cx,b.x-a.x-.018,b.z+.004,.008,'Gold_Limestone'),('BrassNosing',b.x-.022,.023,b.z+.010,.004,'Gold_AgedBrass')]:
  n=A['box']('Lux_'+label,(x,cy,z),(sx,b.y-a.y-.025,sz),bpy.data.materials[mat],1,collision=False,bevel=0);n['LuxuryRefinement']=True;obs.append(n)
bpy.ops.object.select_all(action='DESELECT')
for o in obs:o.select_set(True)
bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();obs[0].name='VG_Lux_HallStoneTreads';obs[0]['ArtAssembly']='HallStoneTreads'
r=json.loads((P/'luxury-authoring-report.json').read_text());r['hallStairFinish']='Original stair mesh and physics retained; polished dark walnut, thin stone treads and brass nosings';(P/'luxury-authoring-report.json').write_text(json.dumps(r,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'),compress=True)
