"""Run only through Blender MCP. Local architectural revision, not a mansion rebuild."""
import bpy, bmesh, math, json, os
from mathutils import Vector
OUT=os.path.dirname(__file__)
backup=os.path.join(OUT,'Mansion_BeforeStairArchitecture.blend')
if not os.path.exists(backup): bpy.ops.wm.save_as_mainfile(filepath=backup,copy=True)
else: bpy.ops.wm.open_mainfile(filepath=backup)
scene=bpy.context.scene
collection_visibility={c.name:c.hide_viewport for c in bpy.data.collections}
for c in bpy.data.collections:c.hide_viewport=False
bpy.context.view_layer.update()
refs={o.name:list(o.matrix_world.translation) for o in scene.objects if o.type=='EMPTY'}
wood=bpy.data.materials['Walnut'];stone=bpy.data.materials['BasementStone']
plaster=bpy.data.materials['Plaster_WarmIvory'];iron=bpy.data.materials['Iron'];gold=bpy.data.materials['AntiqueBrass']
def remove(o): bpy.data.objects.remove(o,do_unlink=True)
def box(name,center,size,mat,col):
 x,y,z=[v/2 for v in size]
 verts=[(-x,-y,-z),(-x,-y,z),(-x,y,-z),(-x,y,z),(x,-y,-z),(x,-y,z),(x,y,-z),(x,y,z)]
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],[tuple(reversed(f)) for f in [(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)]]);mesh.update()
 o=bpy.data.objects.new(name,mesh);col.objects.link(o);o.location=center
 if mat:o.data.materials.append(mat)
 return o
def beam(name,a,b,width,depth,mat,col):
 a,b=Vector(a),Vector(b);o=box(name,(a+b)/2,((b-a).length,width,depth),mat,col)
 o.rotation_euler=(b-a).to_track_quat('X','Z').to_euler();return o
def rail(name,a,b,col):
 a,b=Vector(a),Vector(b)
 beam(name+'_Cap',a+Vector((0,0,1.02)),b+Vector((0,0,1.02)),.09,.09,wood,col)
 for n in range(math.ceil((b-a).length/.45)+1):
  p=a.lerp(b,n/math.ceil((b-a).length/.45))
  box(name+'_Post_%02d'%n,p+Vector((0,0,.5)),(.065,.065,1),iron,col)
def slab(name,x0,x1,y0,y1,z,col,mat=stone):return box(name,((x0+x1)/2,(y0+y1)/2,z-.14),(x1-x0,y1-y0,.28),mat,col)
def cut(o,lo,hi):
 c=box('__cut',(Vector(lo)+Vector(hi))/2,Vector(hi)-Vector(lo),None,o.users_collection[0])
 mod=o.modifiers.new('Local architectural opening','BOOLEAN');mod.operation='DIFFERENCE';mod.object=c
 bpy.context.view_layer.update()
 evaluated=o.evaluated_get(bpy.context.evaluated_depsgraph_get())
 result=bpy.data.meshes.new_from_object(evaluated)
 o.modifiers.clear();o.data=result;remove(c)
def floor(id,c,size):
 old=bpy.data.objects['R%02d_Floor'%id];mat=old.data.materials[0];col=old.users_collection[0];remove(old)
 return box('R%02d_Floor'%id,(c[0],c[1],-.15),(size[0],size[1],.3),mat,col)
def flight(prefix,a,b,col):
 a,b=Vector(a),Vector(b);d=b-a;side=Vector((-d.y,d.x,0)).normalized();N=math.ceil(abs(d.z)/.175)
 # 4m rise / 23 risers = 174mm; 6.6m run / 23 treads = 287mm.
 for n in range(N):
  p=a.lerp(b,(n+.5)/N);p.z=a.z+d.z*(n+1)/N-.09
  o=box(prefix+'_Step_%02d'%n,p,((Vector((d.x,d.y,0))).length/N+.015,1.6,.18),stone,col);o.rotation_euler.z=math.atan2(d.y,d.x)
 # Smooth solid collision slab; render treads carry the actual silhouette.
 vs=[]
 for dz in [-.24,0]:
  for p in [a,b]:
   for s in [-1,1]:vs.append(tuple(p+side*s*.8+Vector((0,0,dz))))
 me=bpy.data.meshes.new(prefix+'_Ramp');me.from_pydata(vs,[],[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)]);me.update()
 o=bpy.data.objects.new(prefix+'_COLLISION_Flight',me);col.objects.link(o);o.hide_render=True;o.hide_set(True)
 for s in [-1,1]:
  rail(prefix+'_SideRail_%d'%s,a+side*s*.86,b+side*s*.86,col)
  beam(prefix+'_Stringer_%d'%s,a+side*s*.76+Vector((0,0,-.3)),b+side*s*.76+Vector((0,0,-.3)),.12,.24,wood,col)
def portal(prefix,c,axis,col):
 c=Vector(c);side=Vector((1,0,0)) if axis=='Y' else Vector((0,1,0))
 for s in [-1,1]:box(prefix+'_Jamb_%d'%s,c+side*s*1.02+Vector((0,0,1.3)),(.14,.14,2.6),wood,col)
 size=(2.18,.18,.16) if axis=='Y' else (.18,2.18,.16)
 box(prefix+'_Header',c+Vector((0,0,2.68)),size,wood,col)

# Clear old stair flights, landings, frames and railings, keeping DoorID roots.
for key in ['DOOR_04_07','DOOR_05_08']:
 for o in list(bpy.data.collections[key].objects):
  if o.type=='MESH':remove(o)
for o in list(bpy.data.collections['Room_01_CentralHall'].objects):
 if o.name.startswith(('R01_Hero_GrandStair','R01_Hero_Gallery','R01_Gallery_')):remove(o)

# Hall: stair runs parallel to rear wall into a wall-supported L gallery.
gc=bpy.data.collections['Room_01_CentralHall']
for n in range(12):
 x=-1.5-(n+.5)*.25;h=(n+1)*.18
 box('R01_Hero_GrandStair_%02d'%n,(x,5.5,h/2),(.26,1.6,h),plaster,gc)
slab('R01_Hero_Gallery',-5.8,-4.5,3.9,7.3,2.16,gc,wood)
slab('R01_Gallery_RearWing',-4.5,-1.5,6.3,7.3,2.16,gc,wood)
for k,(a,b) in enumerate([
 ((-1.5,4.66,0),(-4.5,4.66,2.16)),((-1.5,6.34,0),(-4.5,6.34,2.16)),
 ((-4.5,3.9,2.16),(-4.5,4.66,2.16)),((-5.8,3.9,2.16),(-4.5,3.9,2.16)),
 ((-4.5,6.34,2.16),(-1.5,6.34,2.16)),((-1.5,6.34,2.16),(-1.5,7.3,2.16))]):
 # Keep the upper stair landing open; rear wing front rail starts beyond the stair.
 rail('R01_Gallery_Balustrade_%d'%k,a,b,gc)
for y in [4.1,6.9]:box('R01_Gallery_Pier_'+str(y),(-5.47,y,1.0),(.5,.4,2),wood,gc)
beam('R01_Gallery_RearFascia',(-4.5,6.28,1.94),(-1.5,6.28,1.94),.16,.28,wood,gc)

# Restore old holes, then cut only new side-service stairwells.
f4=floor(4,(-11,-7),(10,8));cut(f4,(-15.96,-11.1,-.6),(-14.24,-5.2,.3))
f5=floor(5,(2,-8),(9,8));cut(f5,(4.09,-12.1,-.6),(5.81,-5.7,.3))
# Seal obsolete ground-floor stair portals (the connections now enter internally).
for id,x,y in [(4,-7,-3),(5,-2,-4)]:
 col=bpy.data.collections['R%02d_Wall_North_Cutaway'%id]
 box('R%02d_North_FormerStairInfill'%id,(x,y,1.3),(2.02,.22,2.6),plaster,col)

routes={
 'DOOR_04_07':[(-13.75,-4.9,0),(-15.06,-4.4,0),(-15.06,-5.2,0),(-15.06,-11.8,-4),(-15.06,-12.6,-4),(-12.7,-12.6,-4),(-12.7,-9.5,-4),(-12.7,-8.6,-4)],
 'DOOR_05_08':[(3.6,-5,0),(4.95,-4.9,0),(4.95,-5.7,0),(4.95,-12.3,-4),(4.95,-13.1,-4),(2.8,-13.1,-4),(2.8,-11,-4),(2.8,-10,-4)]
}
for key,pts in routes.items():
 col=bpy.data.collections[key];root=bpy.data.objects[key]
 x=pts[2][0];yt=pts[2][1];yb=pts[4][1];exitx=pts[5][0]
 flight(key+'_A',pts[2],pts[3],col)
 slab(key+'_TopLanding',x-.8,x+.8,yt,yt+1.6,0,col)
 slab(key+'_BottomLanding',min(x,exitx)-.8,max(x,exitx)+.8,yb-.8,pts[3][1],-4,col)
 slab(key+'_BottomVestibule',exitx-.8,exitx+.8,yb,pts[6][1]+.5,-4,col)
 inner=x+.91 if key=='DOOR_04_07' else x-.91
 floorend=-11 if key=='DOOR_04_07' else -12
 box(key+'_GuardWall',(inner,(yt+floorend)/2,.4),(.12,yt-floorend,.8),plaster,col)
 beam(key+'_GuardCap',(inner,yt,.85),(inner,floorend,.85),.18,.1,wood,col)
 rail(key+'_BottomEndRail',(min(x,exitx)-.8,yb-.8,-4),(max(x,exitx)+.8,yb-.8,-4),col)
 for side in [-1,1]:
  rail(key+'_BottomVestibuleRail_%d'%side,(exitx+side*.86,yb+.9,-4),(exitx+side*.86,pts[6][1]-.2,-4),col)
 rail(key+'_BottomReturnRail',(min(x,exitx)+.8,pts[3][1],-4),(max(x,exitx)-.8,pts[3][1],-4),col)
 # No freestanding doorway frames: only the actual basement wall has an opening.
 root['ArchitecturePath']=json.dumps(pts);root['RepairPath']=json.dumps(pts)
 for o in col.objects:
  if o.type=='MESH':o.parent=root

# Remove only wall portions traversed by the new wells; retain the rest of each wall.
for o in list(scene.objects):
 if o.type!='MESH':continue
 if o.name.startswith('R08_East_'):cut(o,(4.0,-11.2,-4.3),(6.7,-5.65,.2))
 # Keep the existing kitchen service passage, trimming only the sliver over the first tread.
 if o.name.startswith('DOOR_03_05_'):
  cut(o,(4.0,-6.3,-.8),(5.9,-5.5,2.0))
for id,cx,width,y,doorx in [(7,-10,8,-9.5,-12.7),(8,1,9,-11,2.8)]:
 col=bpy.data.collections['R%02d_Wall_South_Cutaway'%id]
 for o in list(col.objects):
  if o.type=='MESH':remove(o)
 lo=cx-width/2;hi=cx+width/2
 for suffix,a,b in [('West',lo,doorx-.95),('East',doorx+.95,hi)]:
  if id==8 and suffix=='East':continue # open service bay beside the descending flight
  box('R%02d_South_StairReturn_%s'%(id,suffix),((a+b)/2,y,-2.25),(b-a,.22,3.5),plaster,col)
 box('R%02d_South_StairLintel'%id,(doorx,y,-.95),(1.9,.22,.9),plaster,col)
# Shift only the two wall-side props obstructing the relocated well.
for name in ['R04_Hero_BanquetSideboard','R04_Hero_SideboardMarble']:bpy.data.objects[name].location.x+=3.5
bpy.data.objects['R05_Hero_Range'].location.x=1
# Old floor-border strips must not bridge across new openings.
for o in list(scene.objects):
 if o.name=='R04_FloorBorderY_-1':remove(o)
 elif o.name=='R05_FloorBorderY_1':remove(o)

entries={'REF_R04_Entry_To_07':(-13.75,-4.9,.05),'REF_R07_Entry_To_04':(-12.7,-8.6,-3.95),'REF_R05_Entry_To_08':(3.6,-5,.05),'REF_R08_Entry_To_05':(2.8,-10,-3.95)}
for name,p in entries.items():bpy.data.objects[name].location=p
for name,p in refs.items():
 if name not in entries:assert (bpy.data.objects[name].matrix_world.translation-Vector(p)).length<1e-5,name
layout={'entries':[{ 'room':int(n[5:7]),'to':int(n[-2:]),'position':{'x':p[0],'y':p[2],'z':p[1]}} for n,p in entries.items()],
 'routes':[{'door':k,'points':[{'x':p[0],'y':p[2],'z':p[1]} for p in v]} for k,v in routes.items()]}
open(os.path.join(OUT,'stair_architecture_layout.json'),'w').write(json.dumps(layout,indent=2))
open(os.path.join(OUT,'architecture_reference_before.json'),'w').write(json.dumps(refs,indent=2))
for name,state in collection_visibility.items():bpy.data.collections[name].hide_viewport=state
bpy.data.collections['Collection'].hide_viewport=True
for o in scene.objects:
 if o.type=='MESH' and (o.name.startswith(('DOOR_04_07_','DOOR_05_08_','R01_Gallery_','R01_Hero_GrandStair','R01_Hero_Gallery')) or o.name in ['R04_Floor','R05_Floor']):
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
scene['StairArchitecture']='Wall-parallel hall gallery; two simple wall-side straight descending stairs; no freestanding doors; 1.6m flights'
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Mansion_Environment_Pass01.blend'))
print('Architectural stairs saved. Four stair Entries aligned; 54 StandPoints/ActionPoints preserved.')
