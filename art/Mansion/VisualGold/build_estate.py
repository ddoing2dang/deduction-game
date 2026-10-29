import bpy,math,json,random
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent;A=bpy.app.driver_namespace['GoldArchitecture'];N=bpy.app.driver_namespace['GoldAuthoring'];bb=N['bbox'];B=A['box'];scene=bpy.context.scene
random.seed(41);col=bpy.data.collections.new('VisualGold_Estate');scene.collection.children.link(col)
solid=[o for o in scene.objects if o.type=='MESH'and o.get('StructureRole')in['Floor','Stair','Passage']and not o.name.startswith('VG_')];lo,hi=bb(solid);outerlo=lo-Vector((7,7,0));outerhi=hi+Vector((7,7,0));ground=A['flat']('GardenEvergreen',(.035,.058,.040),0,.90)
holes=[]
for o in solid:
 a,b=bb([o]);holes.append((a.x-.03,b.x+.03,a.y-.03,b.y+.03))
xs=sorted({outerlo.x,outerhi.x}|{v for h in holes for v in h[:2]});ys=sorted({outerlo.y,outerhi.y}|{v for h in holes for v in h[2:]});vs=[];fs=[]
for x0,x1 in zip(xs,xs[1:]):
 for y0,y1 in zip(ys,ys[1:]):
  cx=(x0+x1)/2;cy=(y0+y1)/2
  if any(a<cx<b and c<cy<d for a,b,c,d in holes):continue
  k=len(vs);vs.extend([(x0,y0,-.035),(x1,y0,-.035),(x1,y1,-.035),(x0,y1,-.035)]);fs.append((k,k+1,k+2,k+3))
me=bpy.data.meshes.new('EstateGround_RoomFootprintsCutOut');me.from_pydata(vs,[],fs);me.materials.append(ground);ob=bpy.data.objects.new('VG_EstateGround_WithGameplayVoids',me);col.objects.link(ob)
for k,v in {'StructureRole':'Exterior','StructureRoom':0,'StructureCollision':False,'StructureDoor':'','StructureSide':'','ArtAssembly':'EstateGround'}.items():ob[k]=v
A['world_uv'](ob,3)
gateY=outerlo.y+.25;innerY=lo.y-.30;midY=(gateY+innerY)/2
B('EstateDriveway',(0,midY,-.018),(4.4,innerY-gateY,.024),A['cobbles'],bevel=0)
for x in [-2.3,2.3]:B('DrivewayStoneEdging',(x,midY,.025),(.14,innerY-gateY,.10),A['stone'],bevel=.025)
# Shared lathed posts/rails create an actual wrought-iron estate fence, not opaque cubes.
cache={}
def rod(name,p,q,r,mat):
 length=(Vector(q)-Vector(p)).length;key=(round(r,4),round(length,4),mat.name)
 if key not in cache:
  vv=[(r*math.cos(math.tau*k/8),r*math.sin(math.tau*k/8),z)for z in [-length/2,length/2]for k in range(8)];ff=[tuple(reversed(range(8))),tuple(range(8,16))]+[(k,(k+1)%8,(k+1)%8+8,k+8)for k in range(8)];mm=bpy.data.meshes.new('EstateSharedIron');mm.from_pydata(vv,[],ff);mm.materials.append(mat);cache[key]=mm
 o=bpy.data.objects.new('VG_Estate_'+name,cache[key]);col.objects.link(o);o.location=(Vector(p)+Vector(q))/2;o.rotation_euler=(Vector(q)-Vector(p)).to_track_quat('Z','Y').to_euler()
 for k,v in {'StructureRole':'Exterior','StructureRoom':0,'StructureDoor':'','StructureSide':'','StructureCollision':False,'ArtAssembly':'EstateFence'}.items():o[k]=v
 return o
segments=[((outerlo.x,gateY),( -3.2,gateY)),((3.2,gateY),(outerhi.x,gateY)),((outerlo.x,gateY),(outerlo.x,outerhi.y)),((outerhi.x,gateY),(outerhi.x,outerhi.y)),((outerlo.x,outerhi.y),(outerhi.x,outerhi.y))]
for si,(aa,bbx)in enumerate(segments):
 a=Vector((*aa,0));b=Vector((*bbx,0));n=max(1,int((b-a).length/.55))
 for j in range(n+1):
  p=a.lerp(b,j/n);rod('Picket_%d_%d'%(si,j),p+Vector((0,0,.12)),p+Vector((0,0,1.26 if j%4 else 1.45)),.018 if j%4 else .030,A['dark'])
 for h in [.38,1.05]:rod('Rail_%d_%s'%(si,h),a+Vector((0,0,h)),b+Vector((0,0,h)),.026,A['dark'])
for side in [-1,1]:
 x=side*3.10;B('GateStonePier_'+str(side),(x,gateY,.95),(.65,.65,1.90),A['stone'],bevel=.06);B('GatePierCap_'+str(side),(x,gateY,1.96),(.82,.82,.17),A['stone'],bevel=.04)
 N['safe']('lantern_chandelier_01',0,(x,gateY,2.08),'GateLantern'+str(side),height=.55,collision=False)
 # Garden seats sit in measured side clearances outside the mansion, facing the driveway.
 y=midY+.4;N['safe']('painted_wooden_bench',0,(side*(hi.x+3.1),y,0),'GardenBench'+str(side),height=.9,rotation=-side*math.pi/2)
 for n in [0,1]:N['safe']('potted_plant_02',0,(side*5.4,gateY+1.8+n*3.0,0),'ForecourtPot_%s_%s'%(side,n),height=.85,collision=False)
# Organic clipped topiary clusters use shared low-poly foliage and raised stone borders.
for side in [-1,1]:
 for j in range(3):
  x=side*(hi.x+3.1);y=lo.y+3+j*5.2
  B('GardenBed_%d_%d'%(side,j),(x,y,.16),(2.5,2.8,.30),A['stone'],bevel=.11)
  for k in range(3):
   bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=(x+(k-1)*.55,y,.67));o=bpy.context.object;o.name='VG_ClippedLaurel_%d_%d_%d'%(side,j,k);o.scale=(.72,.95,.53)
   for c in list(o.users_collection):c.objects.unlink(o)
   col.objects.link(o);o.data.materials.append(ground)
   for key,v in {'StructureRole':'Exterior','StructureRoom':0,'StructureCollision':False,'StructureDoor':'','StructureSide':'','ArtAssembly':'GardenTopiary'}.items():o[key]=v
# Studio-ready, legible mystery lighting. Fixture meshes carry emission; only key lights cast shadows.
lights=bpy.data.collections.new('VisualGold_Lighting');scene.collection.children.link(lights)
for room in range(1,10):
 root=bpy.data.objects['REF_Room_%02d'%room].matrix_world.translation
 data=bpy.data.lights.new('Gold_Room_%02d'%room,'AREA');data.energy=600 if room<=6 else 400;data.color=(1,.83,.63);data.shape='DISK';data.size=4.5;o=bpy.data.objects.new(data.name,data);lights.objects.link(o);o.location=root+Vector((0,0,3.8 if room<7 else 2.8));o['GoldRoom']=room
for name,loc,power,size,color in [('MoonFill',(8,-10,24),2200,22,(.63,.74,1)),('EstateWarm',(-14,-18,15),1800,16,(1,.78,.52))]:
 d=bpy.data.lights.new('Gold_'+name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color;o=bpy.data.objects.new(d.name,d);lights.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,1,0))-o.location).to_track_quat('-Z','Y').to_euler()
scene.world=scene.world or bpy.data.worlds.new('GoldEstateWorld');scene.world.use_nodes=True;bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.10,.14,.20,1);bg.inputs['Strength'].default_value=.35
scene.render.engine='BLENDER_EEVEE';scene.view_settings.view_transform='AgX';scene.render.resolution_percentage=100
N['lib'].hide_render=True;N['lib'].hide_viewport=True
(P/'estate-layout.json').write_text(json.dumps({'actual_building_min':list(lo),'actual_building_max':list(hi),'estate_min':list(outerlo),'estate_max':list(outerhi),'ground_cutouts':len(holes),'ground_vertices':len(vs),'shared_fence_meshes':len(cache)},indent=2))
for name,obj in [('asset-processing.json',N['asset_report']),('placements.json',N['placements']),('retired-placeholders.json',N['retired'])]:(P/name).write_text(json.dumps(obj,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'));print('Exterior estate built from measured full structural footprint',list(lo),list(hi))
