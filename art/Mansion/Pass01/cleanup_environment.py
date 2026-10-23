"""Whole Mansion structure pass. Execute only inside Blender MCP."""
import bpy,bmesh,math,json,os,ast
from mathutils import Vector
OUT=os.path.dirname(__file__);AUDIT=os.path.join(OUT,'StructureAudit');os.makedirs(AUDIT,exist_ok=True)
baseline=os.path.join(OUT,'Mansion_BeforeEnvironmentCleanup.blend')
if os.path.exists(baseline):bpy.ops.wm.open_mainfile(filepath=baseline)
else:bpy.ops.wm.save_as_mainfile(filepath=baseline,copy=True)
scene=bpy.context.scene
visibility={c.name:c.hide_viewport for c in bpy.data.collections}
for c in bpy.data.collections:c.hide_viewport=False
refs={o.name:list(o.matrix_world.translation) for o in scene.objects if o.name.startswith('REF_')}
wood=bpy.data.materials['Walnut'];stone=bpy.data.materials['BasementStone'];plaster=bpy.data.materials['Plaster_WarmIvory'];iron=bpy.data.materials['Iron'];gold=bpy.data.materials['AntiqueBrass']
# Reuse mesh primitives without executing the previous stair revision.
tree=ast.parse(open(os.path.join(OUT,'redesign_stairs.py'),encoding='utf-8-sig').read())
for node in tree.body:
 if isinstance(node,ast.FunctionDef):exec(compile(ast.Module(body=[node],type_ignores=[]),'<primitives>','exec'))
wood=bpy.data.materials['Walnut'];stone=bpy.data.materials['BasementStone'];plaster=bpy.data.materials['Plaster_WarmIvory'];iron=bpy.data.materials['Iron'];gold=bpy.data.materials['AntiqueBrass']
rooms={1:(0,2,0,12,11),2:(-12,4,0,9,8),3:(12,4,0,9,8),4:(-11,-7,0,10,8),5:(2,-8,0,9,8),6:(0,13,0,10,8),7:(-10,-6,-4,8,7),8:(1,-7,-4,9,8),9:(10,-5,-4,8,7)}
deleted=[];changed=[]
def discard(o,reason):deleted.append({'name':o.name,'reason':reason});remove(o)
def role(o,r,room=0,door='',side='',collision=True):
 o['StructureRole']=r;o['StructureRoom']=room;o['StructureDoor']=door;o['StructureSide']=side;o['StructureCollision']=collision
 return o
def emit(name,c,size,mat,col,r,room=0,door='',side='',collision=True):return role(box(name,c,size,mat,col),r,room,door,side,collision)
def getcol(name):return bpy.data.collections[name]
# Door assemblies and oversized bridges are replaced as complete logical connections.
for c in bpy.data.collections:
 if c.name.startswith('DOOR_'):
  key=c.name
  if key in ['DOOR_04_07','DOOR_05_08']:continue
  for o in list(c.objects):
   if o.type=='MESH':discard(o,'replace duplicate end frames/rails and oversized connection geometry with one mapped portal and compact passage')
for o in list(scene.objects):
 if o.type!='MESH':continue
 if any(c.name.startswith('R') and '_Wall_' in c.name for c in o.users_collection):discard(o,'reconcile all wall openings with current LOCK portals')
 elif 'FloorBorder' in o.name:discard(o,'thin nonfunctional floor strip')
 elif 'Pilaster' in o.name:discard(o,'unnecessary corner column')
 elif o.name=='Cube':discard(o,'default scene test cube')
 elif o.name.startswith('HiddenPanel_'):discard(o,'replace unaligned secret panels with wall-aligned mapped panels')

# Explicit cardinal portals; room footprints and centres are unchanged.
# (room A, room B, opening A, opening B, clear width, optional corner)
links=[(1,2,(-6,3,0),(-7.5,3,0),1.4,None),(1,3,(6,3,0),(7.5,3,0),1.8,None),
 (1,5,(1.3,-3.5,0),(1.3,-4,0),1.3,None),(1,6,(0,7.5,0),(0,9,0),2.2,None),
 (2,4,(-11.5,0,0),(-11.5,-3,0),2,None),(3,5,(9.5,0,0),(6.5,-4.9,0),1.6,(9.5,-4.9,0)),
 (4,5,(-6,-7.5,0),(-2.5,-7.5,0),1.6,None),(7,8,(-6,-6.6,-4),(-3.5,-6.6,-4),2,None),
 (8,9,(5.5,-4.4,-4),(6,-4.4,-4),1.6,None)]
openings={i:{s:[] for s in ['North','South','East','West']} for i in rooms}
def register(r,p,width,key):
 x,y,z,w,d=rooms[r];px,py,pz=p
 side='East' if abs(px-(x+w/2))<.02 else 'West' if abs(px-(x-w/2))<.02 else 'North' if abs(py-(y+d/2))<.02 else 'South'
 openings[r][side].append({'at':py if side in ['East','West'] else px,'width':width,'door':key})
 return side
routes=[]
for a,b,p,q,width,corner in links:
 key='DOOR_%02d_%02d'%(a,b);col=getcol(key);root=bpy.data.objects[key]
 sa=register(a,p,width+.12,key);register(b,q,width+.12,key)
 points=[Vector(p)]+([Vector(corner)] if corner else [])+[Vector(q)]
 for k,(u,v) in enumerate(zip(points,points[1:])):
  obj=beam(key+'_Passage_%d'%k,u-Vector((0,0,.16)),v-Vector((0,0,.16)),max(width,1.6),.32,stone if a>=7 else wood,col)
  role(obj,'Passage',door=key)
 # L corner is a continuous landing; remove former wide platform and floating frame.
 if corner:emit(key+'_Passage_Corner',Vector(corner)-Vector((0,0,.16)),(max(width,1.6),max(width,1.6),.32),wood,col,'Passage',door=key)
 # Union L legs and corner into one slab, removing coplanar duplicate floors.
 floors=[o for o in col.objects if o.type=='MESH' and o.get('StructureRole')=='Passage']
 base=floors[0]
 for other in floors[1:]:
  mod=base.modifiers.new('Continuous passage slab','BOOLEAN');mod.operation='UNION';mod.object=other
  bpy.context.view_layer.update();result=bpy.data.meshes.new_from_object(base.evaluated_get(bpy.context.evaluated_depsgraph_get()))
  base.modifiers.clear();base.data=result;remove(other)
 base.name=key+'_Passage_Floor'
 # One actual door assembly per ground/underground corridor, on room A's wall.
 along_y=sa in ['North','South'];sidevec=Vector((1,0,0)) if along_y else Vector((0,1,0));p=Vector(p)
 for sign in [-1,1]:emit(key+'_Door_Jamb_%d'%sign,p+sidevec*sign*(width/2+.07)+Vector((0,0,1.3)),(.14,.14,2.6),wood,col,'Door',door=key)
 emit(key+'_Door_Header',p+Vector((0,0,2.67)),(width+.28,.18,.14) if along_y else (.18,width+.28,.14),wood,col,'Door',door=key)
 # Short side guards only on exposed passage spans, no redundant portal platforms.
 for k,(u,v) in enumerate(zip(points,points[1:])):
  direction=(v-u).normalized();perp=Vector((-direction.y,direction.x,0));start=u+direction*.18;end=v-direction*.18
  if (end-start).length<.25:continue
  for sign in [-1,1]:
   aa=start;bb=end
   if corner and sign==-1:
    if k==0:bb=v-direction*(max(width,1.6)/2+.35)
    else:aa=u+direction*(max(width,1.6)/2+.35)
   rail(key+'_Railing_%d_%d'%(k,sign),aa+perp*sign*(max(width,1.6)/2+.06),bb+perp*sign*(max(width,1.6)/2+.06),col)
 for o in col.objects:
  if o.type=='MESH':
   o.parent=root
   if 'StructureRole' not in o:role(o,'Railing',door=key)
 root['EndpointA']=list(points[0]);root['EndpointB']=list(points[-1]);root['ArchitecturePath']=json.dumps([list(p) for p in points])
 routes.append({'door':key,'a':a,'b':b,'points':[{'x':p.x,'y':p.z,'z':p.y} for p in points]})

# Preserve the two functional service flights, remove only exposed obsolete stringers.
service=json.load(open(os.path.join(OUT,'stair_architecture_layout.json')))
for route in service['routes']:
 key=route['door'];a,b=(4,7) if '04_07' in key else (5,8)
 pts=[Vector((p['x'],p['z'],p['y'])) for p in route['points']]
 register(b,tuple(pts[6]),1.9,key)
 col=getcol(key)
 for o in list(col.objects):
  if '_TopLanding' in o.name:discard(o,'duplicate coplanar slab over the existing room floor; original floor is the upper landing');continue
  if '_Stringer_' in o.name:discard(o,'redundant exposed stair underside strip; solid stair collision slab retained');continue
  if o.type!='MESH':continue
  r='Stair' if '_Step_' in o.name or '_COLLISION_' in o.name else 'Railing' if 'Rail' in o.name or 'Guard' in o.name else 'Floor'
  role(o,r,door=key,collision='_Step_' not in o.name)
  if o.name==key+'_BottomVestibule':
   # End at the basement wall and start after the turn landing, avoiding doubled slabs.
   lo=pts[3].y;hi=pts[6].y;cx=pts[5].x
   o.location=(cx,(lo+hi)/2,-4.14)
   o.data.clear_geometry()
   temp=box('__clean_vestibule',(0,0,0),(1.6,hi-lo,.28),stone,col);o.data=temp.data.copy();remove(temp)
 routes.append({'door':key,'a':a,'b':b,'points':route['points']})

# Secret panels are wall-aligned transitions, not a long hovering tunnel.
secretports=[(9,(7.4,-1.5,-4)),(6,(-5,13,0))]
for rid,p in secretports:register(rid,p,1.4,'DOOR_09_06_SECRET')

# Rebuild perimeter walls with exactly the mapped openings, merging adjoining apertures.
for rid,(x,y,z,w,d) in rooms.items():
 h=8 if rid==1 else 3.5 if rid>=7 else 4
 for side in ['North','South','East','West']:
  col=getcol('R%02d_Wall_%s_Cutaway'%(rid,side));axis=0 if side in ['East','West'] else 1
  fixed=x+(w/2 if side=='East' else -w/2) if axis==0 else y+(d/2 if side=='North' else -d/2)
  low,high=(y-d/2,y+d/2) if axis==0 else (x-w/2,x+w/2)
  cuts=sorted([(max(low,o['at']-o['width']/2),min(high,o['at']+o['width']/2)) for o in openings[rid][side]])
  # Wine east wall needs the stair shaft to remain open below the descending flight.
  if rid==8 and side=='East':cuts.append((-11,-5.65));cuts.sort()
  if rid==8 and side=='South':cuts.append((3.75,5.5));cuts.sort()
  merged=[]
  for lo,hi in cuts:
   if merged and lo<=merged[-1][1]+.02:merged[-1]=(merged[-1][0],max(hi,merged[-1][1]))
   else:merged.append((lo,hi))
  def wall(lo,hi,base,height,label):
   if hi-lo<.04 or height<.04:return
   p=(fixed,(lo+hi)/2,z+base+height/2) if axis==0 else ((lo+hi)/2,fixed,z+base+height/2)
   size=(.22,hi-lo,height) if axis==0 else (hi-lo,.22,height)
   emit('R%02d_Wall_%s_%s_%02d'%(rid,side,label,len(col.objects)),p,size,stone if rid>=7 else plaster,col,'Wall',room=rid,side=side)
  cursor=low
  for lo,hi in merged:
   wall(cursor,lo,0,h,'Pier')
   shaft=(rid==8 and side=='East' and lo<-5.65) or (rid==8 and side=='South')
   if not shaft:wall(lo,hi,2.65,h-2.65,'Lintel')
   cursor=hi
  wall(cursor,high,0,h,'Pier')

# Preserve actual room art and the existing decorative Gallery; classify every mesh.
bpy.data.objects['R05_Hero_Hood'].location.x=bpy.data.objects['R05_Hero_Range'].location.x
bpy.data.objects['R05_Hero_Hood'].location.y=-11.65
for o in list(scene.objects):
 if o.type!='MESH' or 'StructureRole' in o:continue
 if o.name.startswith('R') and o.name[1:3].isdigit():
  rid=int(o.name[1:3]);r='Floor' if o.name=='R%02d_Floor'%rid else 'Gallery' if rid==1 and any(k in o.name for k in ['Gallery','GrandStair']) else 'Room'
  if o.name.startswith('R01_Gallery_Pier'):r='Support'
  role(o,r,room=rid,collision=r=='Floor')
 else:discard(o,'unclassified test mesh outside the architectural environment')
secretcol=getcol('SECRET_09_06_Closed')
for rid,p in secretports:
 side='North' if rid==9 else 'West';size=(1.36,.18,2.6) if rid==9 else (.18,1.36,2.6)
 emit('R%02d_Door_09_06_SECRET_Panel'%rid,Vector(p)+Vector((0,0,1.3)),size,wood,secretcol,'Door',room=rid,door='DOOR_09_06_SECRET',side=side,collision=True)

entries={name:p for name,p in refs.items() if '_Entry_To_' in name}
entries.update({'REF_R03_Entry_To_05':[9.5,1.1,.05],'REF_R05_Entry_To_03':[5.25,-4.9,.05],
 'REF_R08_Entry_To_09':[4.35,-4.4,-3.95],'REF_R09_Entry_To_08':[7.15,-4.4,-3.95]})
for name,p in entries.items():bpy.data.objects[name].location=p
for name,p in refs.items():
 if '_Entry_To_' not in name:assert (bpy.data.objects[name].matrix_world.translation-Vector(p)).length<1e-5,name
for c in bpy.data.collections:
 if c.name in visibility:c.hide_viewport=visibility[c.name]
for o in scene.objects:
 if o.type=='MESH':
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
layout={'routes':routes,'entries':[{'room':int(n[5:7]),'to':int(n[-2:]),'position':{'x':p[0],'y':p[2],'z':p[1]}} for n,p in entries.items()],
 'secret':[{'room':9,'position':{'x':7.4,'y':-3.95,'z':-2.8}},{'room':6,'position':{'x':-3.7,'y':.05,'z':13}}]}
json.dump(layout,open(os.path.join(AUDIT,'layout.json'),'w'),indent=2)
json.dump({'deleted':deleted,'references_before':refs,'openings':openings},open(os.path.join(AUDIT,'cleanup_changes.json'),'w'),indent=2)
scene['EnvironmentCleanup']='All walls reconciled to 11 LOCK links + secret; compact mapped passages; functional stairs retained; no unassigned structures'
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Mansion_Environment_Pass01.blend'))
print('Deleted/replaced',len(deleted),'pieces; current meshes',sum(o.type=='MESH' for o in scene.objects));print('LOCK routes',len(routes))


