import bpy,json
from pathlib import Path
P=Path(__file__).parent;A=bpy.app.driver_namespace['GoldArchitecture'];r=json.loads((P/'straight-entrance-authoring.json').read_text());cx=r['doorCenterX'];half=(r['clearWidth']+r['wallThickness'])/2;lo,hi=r['serviceCrossingY'];items=[]
for side,sgn in [('Left',-1),('Right',1)]:
 items=[]
 x=cx+sgn*half
 o=A['box']('Entry_'+side+'ServiceCutawayInfill',(x,(lo+hi)/2,r['serviceClearHeight']/2),(r['wallThickness'],hi-lo,r['serviceClearHeight']),bpy.data.materials['Gold_WarmPlaster'],collision=False)
 for k,v in {'StructureRole':'EntranceScreen','StructureRoom':0,'StructureDoor':'','StructureCollision':False,'GoldParent':'','ArtAssembly':'StraightEntrance','StraightEntrance':True}.items():o[k]=v
 inner=x-sgn*r['wallThickness']/2
 for name,z,h,width,mat in [('Wainscot',.82,1.05,.025,'Gold_PolishedDarkWalnut'),('Dado',1.39,.045,.04,'Gold_AgedBrass'),('Skirting',.10,.20,.07,'Gold_Limestone')]:
  n=A['box']('Entry_'+side+'Infill'+name,(inner-sgn*width/2,(lo+hi)/2,z),(width,hi-lo,h),bpy.data.materials[mat],collision=False)
  n['StructureRole']='EntranceScreen';n['StructureRoom']=0;n['StructureDoor']='';n['StructureCollision']=False;n['ArtAssembly']='StraightEntrance';n['StraightEntrance']=True
  items.append(n)
 items.append(o)
 # One visibility unit includes all profiles at each service crossing.
 group=[n for n in items if n.name.startswith('VG_Entry_'+side)]
 bpy.ops.object.select_all(action='DESELECT')
 for n in group:n.select_set(True)
 bpy.context.view_layer.objects.active=o;bpy.ops.object.join()
m=bpy.data.materials['Gold_WarmPlaster'].copy();m.name='Gold_EntranceCofferIvory'
b=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');b.inputs['Emission Color'].default_value=(.48,.42,.31,1);b.inputs['Emission Strength'].default_value=.35
roof=bpy.data.objects['VG_Entry_ServiceCrossingWallSoffit'];roof.data.materials.clear();roof.data.materials.append(m)
r['serviceInfill']='Two 0.26m-thick render wall units close entrance sightlines; no collider at the protected crossover. Existing gameplay bridge stays unchanged and these units cut away during an actual nearby traversal.';(P/'straight-entrance-authoring.json').write_text(json.dumps(r,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'),compress=True)
