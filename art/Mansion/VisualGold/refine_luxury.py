"""Measured presentation refinement only. Execute in the live Blender through MCP."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix
P=Path(__file__).parent;s=bpy.context.scene
assert Path(bpy.data.filepath)==P/'Mansion_VisualGold.blend'
assert not bpy.data.objects.get('VG_Lux_EntranceRunner')
A=bpy.app.driver_namespace['GoldArchitecture'];N=bpy.app.driver_namespace['GoldAuthoring']
bb=N['bbox'];conflicts=N['conflicts'];B=A['box'];M=lambda n:bpy.data.materials['Gold_'+n]
gold=M('AgedBrass');wood=M('Walnut');stone=M('Limestone');marble=M('HallMarble');iron=M('BlackenedIron')
added=[];changed=[];checks=[];placements=json.loads((P/'placements.json').read_text())
before=json.loads((P/'Backup/LuxuryRefinement/gold-manifest.json').read_text())
refs={o.name:[[float(v)for v in row]for row in o.matrix_world]for o in s.objects if o.name.startswith('REF_')}
def copy_tint(source,name,tint):
 m=M(source).copy();m.name='Gold_'+name
 for n in m.node_tree.nodes:
  if n.type=='MIX_RGB' and n.blend_type=='MULTIPLY':n.inputs[2].default_value=(*tint,1)
 m['GoldTint']=list(tint);return m
emerald=copy_tint('HallMarble','VerdeMarble',(.075,.13,.12))
grain=copy_tint('Walnut','DoorInlaidWalnut',(.34,.24,.16))
wine=copy_tint('CrimsonJacquard','BurgundyVelvet',(.32,.035,.055))
forest=copy_tint('CrimsonJacquard','ForestJacquard',(.055,.15,.095))
def props(o,room=0,parent='',assembly=''):
 for k,v in {'StructureRole':'Trim'if parent else 'Room'if room else 'Exterior','StructureRoom':room,'StructureDoor':'','StructureCollision':False,'StructureSide':'','GoldParent':parent,'ArtAssembly':assembly or o.name,'LuxuryRefinement':True}.items():o[k]=v
 return o
def box(label,loc,size,mat,room=0,parent='',bevel=.01):
 o=B('Lux_'+label,loc,size,mat,room,parent,False,bevel);props(o,room,parent);added.append(o);return o
def tube(label,points,r,mat,room=0,parent='',closed=False):
 cu=bpy.data.curves.new(label,'CURVE');cu.dimensions='3D';cu.resolution_u=1;cu.bevel_depth=r;cu.bevel_resolution=1
 sp=cu.splines.new('POLY');sp.points.add(len(points)-1)
 for p,v in zip(sp.points,points):p.co=(*v,1)
 sp.use_cyclic_u=closed;o=bpy.data.objects.new('VG_Lux_'+label,cu);s.collection.objects.link(o);o.data.materials.append(mat);bpy.context.view_layer.objects.active=o;o.select_set(True)
 with bpy.context.temp_override(selected_objects=[o]):bpy.ops.object.convert(target='MESH')
 props(o,room,parent);added.append(o);return o
def join(obs,name,room=0,parent=''):
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();o=obs[0];o.name=name;props(o,room,parent);return o
def clone(source,name,room,anchor,height=None,rotation=0,parent=''):
 src=bpy.data.objects[source];o=src.copy();o.data=src.data;s.collection.objects.link(o);o.name='VG_Lux_'+name
 o.rotation_euler.z=rotation;o.scale=src.scale.copy()
 if height:o.scale*=height/(bb([src])[1].z-bb([src])[0].z)
 o.location=anchor;o.hide_render=False;o.hide_set(False);bpy.context.view_layer.update();lo,hi=bb([o]);issues=conflicts(lo,hi,room)
 assert not issues,(name,issues)
 props(o,room,parent);o['GoldAsset']=src['GoldAsset'];added.append(o)
 placements.append({'name':o.name,'slug':o['GoldAsset'],'room':room,'location':list(o.location),'scale':list(o.scale),'rotation':rotation,'min':list(lo),'max':list(hi),'wallMounted':bool(parent)})
 checks.append({'object':o.name,'protectedConflicts':issues});return o
# This is the LOCK 4<->5 route, not an unused bridge. Preserve floor, IDs and ALL Unity baseline colliders.
# Replace just the tall visual wooden rails by low stone edging; each original component keeps its name.
for o in list(s.objects):
 if not o.name.startswith('DOOR_04_05_Railing_'):continue
 o.data=o.data.copy();world=o.matrix_world.copy();inv=world.inverted();lo,hi=bb([o])
 for v in o.data.vertices:
  p=world@v.co;p.z=.035+(p.z-lo.z)/max(hi.z-lo.z,1e-6)*(.045 if '_Cap'in o.name else .012);v.co=inv@p
 o.data.materials.clear();o.data.materials.append(stone);o['VisualBoundaryRefined']=True;changed.append(o.name)
bpy.data.objects['DOOR_04_05_Passage_Floor'].data.materials.clear();bpy.data.objects['DOOR_04_05_Passage_Floor'].data.materials.append(marble)
# Rebuild ONLY the noncolliding exterior skin. Basement floor projections must not punch permanent holes into ground level.
ground=bpy.data.objects['VG_EstateGround_WithGameplayVoids'];lo,hi=bb([ground]);holes=[]
for o in s.objects:
 if o.type!='MESH' or o.name.startswith('VG_') or o.get('StructureRole')not in ['Floor','Passage','Stair']:continue
 a,b=bb([o])
 if o.get('StructureRole')!='Stair'and b.z<-.35:continue
 holes.append((a.x-.012,b.x+.012,a.y-.012,b.y+.012))
xs=sorted({lo.x,hi.x,-6.,-2.5,6.5}|{float(x)for x in range(-23,24)}|{v for h in holes for v in h[:2]})
ys=sorted({lo.y,hi.y,-14.2,-12.,-3.5}|{float(y)for y in range(-20,24)}|{v for h in holes for v in h[2:]})
vs=[];fs=[];ids=[]
for x0,x1 in zip(xs,xs[1:]):
 for y0,y1 in zip(ys,ys[1:]):
  x,y=(x0+x1)/2,(y0+y1)/2
  if any(a<x<b and c<y<d for a,b,c,d in holes):continue
  k=len(vs);vs.extend([(x0,y0,-.035),(x1,y0,-.035),(x1,y1,-.035),(x0,y1,-.035)]);fs.append((k,k+1,k+2,k+3))
  front=(-6<=x<=6.5 and -14.2<=y<=-12)or(-6<=x<=-2.5 and -12<=y<=-3.5)
  ids.append((1 if (math.floor(x)+math.floor(y))%2 else 2)if front else 0)
me=bpy.data.meshes.new('LuxuryGround_GroundLevelAperturesOnly');me.from_pydata(vs,[],fs)
for m in [M('GardenEvergreen'),marble,emerald]:me.materials.append(m)
for p,i in zip(me.polygons,ids):p.material_index=i
ground.data=me;A['world_uv'](ground,2.5);changed.append(ground.name)
# Finished rugs: geometry is <=3cm high and never collected by PhysicsColliders for navigation.
def rug(label,cx,cy,z,w,h,room,mat):
 obs=[box(label,(cx,cy,z+.006),(w,h,.012),mat,room,bevel=0)]
 for inset,thick in [(.09,.022),(.17,.012)]:
  for dy in [-1,1]:obs.append(box(label+'Border',(cx,cy+dy*(h/2-inset),z+.015),(w-2*inset,thick,.004),gold,room,bevel=0))
  for dx in [-1,1]:obs.append(box(label+'Border',(cx+dx*(w/2-inset),cy,z+.015),(thick,h-2*inset,.004),gold,room,bevel=0))
 return join(obs,'VG_Lux_'+label,room)
rug('EntranceRunner',-4.2,-5.28,0,1.45,2.35,0,wine)
for room,w,h,mat in [(2,4.6,3.4,forest),(4,4.6,3.0,wine),(6,3.9,3.1,forest),(9,3.6,2.6,forest)]:
 c=bpy.data.objects['REF_Room_%02d'%room].matrix_world.translation;rug('R%02d_CentralRug'%room,c.x,c.y,c.z+.006,w,h,room,mat)
# Existing rugs receive brass stitched borders without moving their footprint.
for name in ['R01_P02_HallRug_Border','R03_P02_SeatingRug_Border']:
 o=bpy.data.objects[name];a,b=bb([o]);room=int(o['StructureRoom']);obs=[]
 for dx in [-1,1]:obs.append(box('RugSideBraid',((a.x+b.x)/2+dx*((b.x-a.x)/2-.10),(a.y+b.y)/2,.035),(.025,b.y-a.y-.2,.005),gold,room,bevel=0))
 for dy in [-1,1]:obs.append(box('RugEndBraid',((a.x+b.x)/2,(a.y+b.y)/2+dy*((b.y-a.y)/2-.10),.035),(b.x-a.x-.2,.025,.005),gold,room,bevel=0))
 join(obs,'VG_Lux_R%02d_RugBraid'%room,room)
# Hall wall console: measured west solid wall below the real library opening.
hallWall=bpy.data.objects['R01_Wall_West_Pier_00'];a,b=bb([hallWall]);cx=b.x+.38;cy=a.y+3.2
console=clone('VG_R01_HallConsole','HallWestConsole',1,(cx,cy,0),rotation=math.pi/2)
top=bb([console])[1].z
clone('VG_R01_HallConsoleBust','HallWestBust',1,(cx,cy+.32,top+.003),rotation=math.pi/2)
clone('VG_R01_HallConsoleLamp','HallWestLamp',1,(cx,cy-.37,top),rotation=math.pi/2)
# Framed oval mirror with bowed gilt profile above the console, on the same cutaway wall.
mirror=A['flat']('AntiqueMirror',(.24,.32,.29),.92,.15)
pts=[(b.x+.075,cy+.48*math.cos(math.tau*i/48),2.15+.68*math.sin(math.tau*i/48))for i in range(48)]
ob=tube('HallMirrorGilt',pts,.042,gold,1,hallWall.name,True)
verts=[(b.x+.066,cy,2.15)]+pts;faces=[(0,i+1,(i+1)%48+1)for i in range(48)]
me=bpy.data.meshes.new('OvalMirrorFace');me.from_pydata(verts,[],faces);me.materials.append(mirror);mirrorob=bpy.data.objects.new('VG_Lux_HallMirrorFace',me);s.collection.objects.link(mirrorob);props(mirrorob,1,hallWall.name);added.append(mirrorob)
join([ob,mirrorob],'VG_Lux_HallOvalMirror',1,hallWall.name)
# Wall-mounted art, paired drapes and restrained upper panel frames on actual solid wall segments.
for room in [2,3,4,5,6,9]:
 walls=[o for o in s.objects if o.get('StructureRole')=='Wall'and int(o.get('StructureRoom',0))==room and o.get('StructureSide')=='North'and bb([o])[0].z<.1 and o.dimensions.z>3]
 wall=max(walls,key=lambda o:o.dimensions.x);a,b=bb([wall]);length=b.x-a.x;x=(a.x+b.x)/2
 # Offset toward a clear end of long walls to avoid the original central sconce.
 if length>6:x=a.x+length*.78
 y=a.y-.10;z=a.z+1.75
 if room in [2,3,4]:
  pic=clone('VG_R03_LoungeGiltFrame','R%02d_WallPortrait'%room,room,(x,y,z),height=1.25,parent=wall.name)
 if length>3.2:
  obs=[];ww=min(2.6,length-.35);zz=a.z+2.35
  for dx in [-1,1]:obs.append(box('UpperPanelStile',(x+dx*ww/2,a.y-.014,zz),(.035,.025,1.68),gold,room,wall.name,0))
  for dz in [-.84,.84]:obs.append(box('UpperPanelRail',(x,a.y-.014,zz+dz),(ww,.025,.035),gold,room,wall.name,0))
  join(obs,'VG_Lux_R%02d_UpperPanel'%room,room,wall.name)
 if room in [2,3]:
  obs=[];width=.40;bottom=1.12;topz=3.56
  for side in [-1,1]:
   xx=x+side*.98;vv=[];ff=[]
   for row,zv in enumerate([bottom,bottom+.22,2.25,topz]):
    for j in range(13):
     u=j/12;xxv=xx+(u-.5)*width*(1.0 if row!=2 else .60);vv.append((xxv,y-.13+.055*math.cos(u*math.tau*3),zv))
   for row in range(3):
    for j in range(12):k=row*13+j;ff.append((k,k+1,k+14,k+13))
   me=bpy.data.meshes.new('PleatedVelvetDrape');me.from_pydata(vv,[],ff);me.materials.append(wine if room==3 else forest);o=bpy.data.objects.new('VG_Lux_Drape',me);s.collection.objects.link(o);A['world_uv'](o,.8);props(o,room,wall.name);added.append(o);obs.append(o)
   obs.append(box('DrapeTie',(xx,y-.19,2.25),(.27,.04,.045),gold,room,wall.name,0))
  obs.append(tube('CurtainPole',[(x-1.30,y-.14,3.60),(x+1.30,y-.14,3.60)],.025,gold,room,wall.name))
  join(obs,'VG_Lux_R%02d_PortraitDrapery'%room,room,wall.name)
# Keep the dining sconce outside the new portrait frame, on its measured solid wall segment.
diningSconce=bpy.data.objects['VG_R04_WallSconce_04_0'];diningSconce.location.x=-6.45
for r in placements:
 if r['name']==diningSconce.name:
  bpy.context.view_layer.update();a,b=bb([diningSconce]);r['location']=list(diningSconce.location);r['min']=list(a);r['max']=list(b)
# Ceiling fixtures remain above the protected character headroom.
for room in [2,3]:
 c=bpy.data.objects['REF_Room_%02d'%room].matrix_world.translation
 clone('VG_R04_DiningChandelier','R%02d_CentreChandelier'%room,room,(c.x,c.y,2.9),height=.70)
hallChandelier=bpy.data.objects['VG_R01_HallChandelier'];hallChandelier.location.y=1.5
for r in placements:
 if r['name']==hallChandelier.name:
  a,b=bb([hallChandelier]);r['location']=list(hallChandelier.location);r['min']=list(a);r['max']=list(b)
for o in s.objects:
 if o.type=='LIGHT'and o.get('GoldRoom'):o.data.color=(1,.90,.78)
# Central hall gallery coffer outlines follow the original uninterrupted high wall. No blocking columns.
for wall in [o for o in s.objects if o.get('StructureRole')=='Wall'and o.get('StructureRoom')==1 and o.get('StructureSide')=='North']:
 a,b=bb([wall]);length=b.x-a.x
 if length<2.5:continue
 obs=[];x=(a.x+b.x)/2;y=a.y-.014
 for dx in [-1,1]:obs.append(box('GalleryCofferStile',(x+dx*(length/2-.28),y,5.0),(.045,.025,2.4),gold,1,wall.name,0))
 for z in [3.8,6.2]:obs.append(box('GalleryCofferRail',(x,y,z),(length-.56,.025,.045),gold,1,wall.name,0))
 join(obs,'VG_Lux_GalleryCoffer_'+wall.name,1,wall.name)
# Reuse existing authored CC0 plants on the measured new exterior landing margins.
for side,x in [(-1,-5.66),(1,-2.74)]:clone('VG_R06_ConservatoryPlant2','EntranceUrn'+str(side),0,(x,-4.85,0),height=.70)
# Fine timber grain for door inset panels and a compact heraldic crest below its existing cornice.
door=bpy.data.objects['VG_DecorativeFrontDoor_ClosedDoubleLeaf']
for i,m in enumerate(door.data.materials):
 if m==M('MahoganyPanelling'):door.data.materials[i]=grain
obs=[]
for side in [-1,1]:
 y=-3.5+side*.40
 # Laurel wreath formed from clean swept curves, no new doorway/opening.
 for lr in [-1,1]:
  pts=[(-4.2+lr*(.13+.28*math.sin(t*math.pi*.8)),y,3.40+.32*t)for t in [i/16 for i in range(17)]]
  obs.append(tube('EntranceLaurelStem',pts,.016,gold,1))
  for j in range(5):
   t=(j+.5)/5;x=-4.2+lr*(.13+.28*math.sin(t*math.pi*.8));z=3.40+.32*t
   obs.append(tube('EntranceLaurelLeaf',[(x,y,z),(x+lr*.10,y,z+.045),(x+lr*.02,y,z+.065)],.014,gold,1,closed=True))
 # Lozenge shield, gold perimeter and dark verde inset.
 outline=[(-4.2,y,3.74),(-3.99,y,3.60),(-4.2,y,3.39),(-4.41,y,3.60)]
 obs.append(tube('EntranceCrestRim',outline,.025,gold,1,closed=True))
 me=bpy.data.meshes.new('VerdeHeraldicCrest');me.from_pydata(outline,[],[(0,1,2,3)]);me.materials.append(emerald);o=bpy.data.objects.new('VG_Lux_CrestFace',me);s.collection.objects.link(o);props(o,1);added.append(o);obs.append(o)
join(obs,'VG_Lux_EntranceHeraldicCrest',1)
bpy.context.view_layer.update()
assert all(max(abs(bpy.data.objects[n].matrix_world[i][j]-m[i][j])for i in range(4)for j in range(4))<1e-6 for n,m in refs.items())
(P/'placements.json').write_text(json.dumps(placements,indent=2))
# Refresh the existing placement list used by the Blender collision audit, without reimporting deleted templates.
N['placements']=placements
report={'bridgeRole':'DOOR_04_05 active dining-kitchen route; physics/entries unchanged','visualRailComponentsRefined':changed[:-1],'groundFix':'Only ground-level floors and actual stairs subtract exterior ground; basement floors no longer cause permanent holes','groundCells':len(fs),'newObjects':[o.name for o in s.objects if o.get('LuxuryRefinement')],'protectedPlacementChecks':checks,'referenceCountUnchanged':len(refs),'cc0Placements':len(placements),'noNewTraversalLinks':True}
(P/'luxury-authoring-report.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'),compress=True)
print(json.dumps({k:v for k,v in report.items()if k not in ['newObjects','visualRailComponentsRefined','protectedPlacementChecks']}))
