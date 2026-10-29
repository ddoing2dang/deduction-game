"""Measured decorative entrance with separately tagged physical side boundaries. Blender MCP only."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent;s=bpy.context.scene;N=bpy.app.driver_namespace['GoldAuthoring'];A=bpy.app.driver_namespace['GoldArchitecture'];bb=N['bbox'];B=A['box']
assert not bpy.data.objects.get('VG_Entry_Floor')
door=bpy.data.objects['VG_DecorativeFrontDoor_ClosedDoubleLeaf'];dl,dh=bb([door]);cx=(dl.x+dh.x)/2
wall=bpy.data.objects[door.get('DecorativeWall','R01_Wall_South_Pier_00')];wl,wh=bb([wall]);doorY=(wl.y+wh.y)/2
fl,fh=bb([bpy.data.objects['R05_Floor']]);frontY=fl.y-.30
bridge=bpy.data.objects['DOOR_04_05_Passage_Floor'];bl,bh=bb([bridge]);bridgeY=(bl.y+bh.y)/2
crossLo=bl.y-.30;crossHi=bh.y+.30
# Actual exterior wall faces constrain width; leave a centimetre of construction clearance.
west=[o for o in s.objects if o.get('StructureRole')=='Wall'and o.get('StructureRoom')==4 and o.get('StructureSide')=='East']
east=[o for o in s.objects if o.get('StructureRole')=='Wall'and o.get('StructureRoom')==5 and o.get('StructureSide')=='West']
leftFace=max(bb([o])[1].x for o in west);rightFace=min(bb([o])[0].x for o in east)
thick=.26;half=min(cx-leftFace,rightFace-cx)-thick/2-.01;left=cx-half;right=cx+half
clear=2*half-thick;height=4.35;spring=3.10;crossClear=3.30
gold=bpy.data.materials['Gold_AgedBrass'];stone=bpy.data.materials['Gold_Limestone'];plaster=bpy.data.materials['Gold_WarmPlaster'];marble=bpy.data.materials['Gold_HallMarble'];green=bpy.data.materials['Gold_VerdeMarble'];wood=bpy.data.materials['Gold_PolishedDarkWalnut'];velvet=bpy.data.materials['Gold_BurgundyVelvet'];glow=bpy.data.materials['Gold_FrontDoorLampGlass'];iron=bpy.data.materials['Gold_BlackenedIron']
created=[];boundaries=[]
def tag(o,role='Exterior',side='',boundary=False):
 for k,v in {'StructureRole':'EntranceBoundary'if boundary else role,'StructureRoom':0,'StructureDoor':'','StructureSide':side,'StructureCollision':boundary,'GoldParent':'','ArtAssembly':'StraightEntrance','EntranceSideBoundary':boundary,'StraightEntrance':True}.items():o[k]=v
 created.append(o)
 if boundary:boundaries.append(o)
 return o
def box(name,loc,size,mat,side='',boundary=False,bevel=.015):
 o=B('Entry_'+name,loc,size,mat,collision=False,bevel=bevel);return tag(o,side=side,boundary=boundary)
def join(obs,name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();o=obs[0];o.name=name;return o
def segment(side,x,a,b):
 # Each solid portion is a separate collider; the real service opening is never bridged by a box collider.
 box(side+'Wall_'+str(len(boundaries)),(x,(a+b)/2,height/2),(thick,b-a,height),plaster,side,True)
 inner=x+(.5*thick if side=='Left'else-.5*thick);sgn=1 if side=='Left'else-1;profiles=[]
 for z,h,w,mat in [(.10,.20,.07,stone),(.82,1.05,.025,wood),(1.39,.045,.04,gold),(height-.17,.14,.09,stone),(height-.055,.035,.105,gold)]:
  profiles.append(box(side+'Profile',(inner+sgn*w/2,(a+b)/2,z),(w,b-a,h),mat,side))
 for y in [a+.16,b-.16]:profiles.append(box(side+'Pilaster',(inner+sgn*.035,y,2.0),(.07,.16,3.7),stone,side))
 join(profiles,'VG_Entry_'+side+'_Profiles_'+str(len(boundaries)))
for side,x in [('Left',left),('Right',right)]:
 segment(side,x,frontY-.30,crossLo)
 segment(side,x,crossHi,doorY-1.30)
 box(side+'CrossoverSpandrel',(x,bridgeY,(height+crossClear)/2),(thick,crossHi-crossLo,height-crossClear),plaster,side,True)
 # Deep service reveals/returns screen oblique views into adjacent empty volumes, outside the protected 4<->5 band.
 outward=-1 if side=='Left'else 1
 box(side+'ServiceReturn',(x+outward*.46,crossHi+.18,1.68),(1.05,.26,3.36),plaster,side,True)
 # Flare into the existing broad door surround rather than burying its mouldings.
 rear=dl.x-.16 if side=='Left'else dh.x+.16
 y0=doorY-1.30;y1=doorY+.02;dx=rear-x;dy=y1-y0;length=math.hypot(dx,dy)
 o=box(side+'SplayedRearWall',((rear+x)/2,(y0+y1)/2,height/2),(thick,length,height),plaster,side,True);o.rotation_euler.z=-math.atan2(dx,dy)
 # Corner pilasters frame the external mouth; no leaf, ID or interaction is created here.
 for z,w,d,h,mat in [(.15,.43,.65,.30,stone),(1.75,.30,.48,3.25,stone),(3.36,.45,.60,.18,gold)]:box(side+'PortalPier',(x,frontY,z),(w,d,h),mat,side)
 # Sconces and moulded inset panels repeat symmetrically along the real solid spans.
 for y in [frontY+1.05,crossLo-.85,(crossHi+doorY-1.30)/2]:
  inner=x+outward*(-thick/2-.06);parts=[]
  parts.append(box(side+'SconcePlate',(inner,y,2.45),(.055,.18,.56),gold,side))
  parts.append(box(side+'SconceGlass',(inner-outward*.10,y,2.48),(.14,.16,.30),glow,side))
  for z in [2.29,2.67]:parts.append(box(side+'SconceCap',(inner-outward*.10,y,z),(.20,.24,.07),iron,side))
  join(parts,'VG_Entry_'+side+'_Sconce_'+str(round(y,2)))
  # Framing stays on the wall, not across the service opening.
  for z in [1.75,3.30]:box(side+'InsetRail',(inner+outward*.025,y,z),(.025,.62,.025),gold,side,bevel=0)
# A shallow covered crossing screens the bridge from half-top views. Clear height is greater than the existing 2.65m doors.
box('ServiceCrossingWallSoffit',(cx,bridgeY,3.48),(clear+2*thick,crossHi-crossLo,.24),stone,boundary=True)
# Actual thick entrance arch, open down its axis. Elliptical voussoir ring is extruded in Y.
vs=[];fs=[];steps=40;rx=clear/2;rz=1.12
for y in [frontY-.28,frontY+.28]:
 for outer in [0,1]:
  for i in range(steps+1):
   t=math.pi*i/steps;vs.append((cx+(rx+outer*.19)*math.cos(t),y,spring+(rz+outer*.17)*math.sin(t)))
n=steps+1
for i in range(steps):
 for base in [0,2*n]:fs.append((base+i,base+i+1,base+n+i+1,base+n+i))
 fs.append((i,i+1,2*n+i+1,2*n+i));fs.append((n+i,n+i+1,3*n+i+1,3*n+i))
for i in [0,steps]:fs.append((i,n+i,3*n+i,2*n+i))
me=bpy.data.meshes.new('MeasuredEllipticalEntranceArch');me.from_pydata(vs,[],fs);me.materials.append(stone);o=bpy.data.objects.new('VG_Entry_OuterArch',me);s.collection.objects.link(o);tag(o);A['world_uv'](o,2)
box('PortalCrown',(cx,frontY,height+.09),(2*half+.65,.74,.16),stone)
box('PortalCrownBrass',(cx,frontY,height+.19),(2*half+.72,.77,.035),gold)
# One continuous floor strip; it is render-only and cannot add a walkable NavMesh surface.
floor=box('Floor',(cx,(frontY-.28+doorY+.02)/2,.006),(clear,doorY+.02-(frontY-.28),.012),marble,bevel=0)
run=box('Runner',(cx,(frontY+.08+doorY-.18)/2,.016),(1.18,doorY-.18-(frontY+.08),.008),velvet,bevel=0)
for x in [cx-.54,cx+.54]:box('RunnerBraid',(x,(frontY+.16+doorY-.28)/2,.022),(.018,doorY-.28-(frontY+.16),.003),gold,bevel=0)
for x in [left+thick/2+.08,right-thick/2-.08]:box('FloorBorder',(x,(frontY+doorY)/2,.013),(.10,doorY-frontY,.004),green,bevel=0)
if bpy.data.objects.get('VG_Lux_EntranceRunner'):bpy.data.objects.remove(bpy.data.objects['VG_Lux_EntranceRunner'],do_unlink=True)
# Reposition the existing external urns beside the new portal, away from the wall thickness.
rows=json.loads((P/'placements.json').read_text())
for side in [-1,1]:
 name='VG_Lux_EntranceUrn'+str(side);o=bpy.data.objects[name];o.location=(cx+side*(half+.18),frontY-.68,0);bpy.context.view_layer.update()
 for row in rows:
  if row['name']==name:a,b=bb([o]);row['location']=list(o.location);row['min']=list(a);row['max']=list(b)
# Keep the exterior door sconces inside the rear splay; the approved door itself stays in place.
inv=door.matrix_world.inverted()
for v in door.data.vertices:
 p=door.matrix_world@v.co
 if p.y<doorY-.12 and 2.15<p.z<2.85:
  if p.x<cx-1.36:p.x+=.24
  elif p.x>cx+1.36:p.x-=.24
  v.co=inv@p
# Two modest fills; fixtures remain emissive and no extra gameplay component is introduced.
for j,y in enumerate([frontY+2.0,doorY-1.65]):
 d=bpy.data.lights.new('EntryWarmFill'+str(j),'AREA');d.energy=110;d.color=(1,.86,.69);d.shape='DISK';d.size=2.0;o=bpy.data.objects.new(d.name,d);s.collection.objects.link(o);o.location=(cx,y,3.30);o['GoldRoom']=0;o['GoldIntensity']=.65;o['GoldRange']=4.5
bpy.context.view_layer.update();N['placements']=rows;(P/'placements.json').write_text(json.dumps(rows,indent=2))
report={'referenceCommit':'ee4064fffbb25d33143f6074811de597bc911f12','doorCenterX':cx,'doorWallY':doorY,'exteriorPortalY':frontY,'clearWidth':clear,'wallThickness':thick,'wallHeight':height,'serviceCrossingY':[crossLo,crossHi],'serviceClearHeight':crossClear,'boundaryParts':[o.name for o in boundaries],'floor':'VG_Entry_Floor','floorAddsNavigation':False,'newDoorIDs':0,'newRoomTransitions':0,'routePreserved':'DOOR_04_05 remains at original elevation with concealed side crossover apertures and separate existing collision boundaries'}
(P/'straight-entrance-authoring.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'),compress=True);print(json.dumps(report))
