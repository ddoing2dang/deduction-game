"""Execute only through Blender MCP, after Entrance/gate.json is OK."""
import bpy,os,json,math,random,bmesh
from mathutils import Vector
OUT=os.path.dirname(__file__)
assert json.load(open(os.path.join(OUT,'Entrance','gate.json'),encoding='utf-8-sig'))['status']=='OK'
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'Mansion_EntranceFixed.blend'))
scene=bpy.context.scene;random.seed(24)
refs={o.name:list(o.matrix_world.translation) for o in scene.objects if o.name.startswith('REF_')}
col=bpy.data.collections.new('Mansion_Environment_Pass02_Art');scene.collection.children.link(col)
palette={
 'Oak':(.20,.105,.055,1),'Walnut':(.105,.055,.035,1),'Brass':(.52,.34,.12,1),
 'Ivory':(.72,.64,.47,1),'Paper':(.62,.55,.39,1),'Velvet':(.20,.065,.075,1),
 'Blue':(.12,.20,.24,1),'Green':(.10,.24,.14,1),'Leaf':(.19,.34,.13,1),
 'Terracotta':(.34,.15,.075,1),'Iron':(.10,.13,.14,1),'Copper':(.38,.19,.10,1),
 'Marble':(.47,.46,.39,1),'Wine':(.22,.045,.065,1),'Glass':(.22,.40,.35,.18)}
mats={}
for key,color in palette.items():
 m=bpy.data.materials.get('P02_'+key) or bpy.data.materials.new('P02_'+key);m.diffuse_color=color;m.use_nodes=True
 node=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');node.inputs['Base Color'].default_value=color;node.inputs['Roughness'].default_value=.72;node.inputs['Alpha'].default_value=color[3]
 mats[key]=m
meshes={};added=[];deleted=[]
def primitive(kind,material):
 key=kind+'_'+material
 if key in meshes:return meshes[key]
 me=bpy.data.meshes.new('P02Shared_'+key)
 if kind=='Box':
  vs=[(-.5,-.5,-.5),(-.5,-.5,.5),(-.5,.5,-.5),(-.5,.5,.5),(.5,-.5,-.5),(.5,-.5,.5),(.5,.5,-.5),(.5,.5,.5)]
  fs=[(0,2,6,4),(1,5,7,3),(0,4,5,1),(2,3,7,6),(0,1,3,2),(4,6,7,5)]
 else:
  N=12 if kind=='Cylinder' else 6
  vs=[(math.cos(n*2*math.pi/N)*r,math.sin(n*2*math.pi/N)*r,z) for z,r in [(-.5,.5),(.5,.5 if kind=='Cylinder' else .05)] for n in range(N)]
  fs=[tuple(reversed(range(N))),tuple(range(N,2*N))]+[(n,(n+1)%N,(n+1)%N+N,n+N) for n in range(N)]
 me.from_pydata(vs,[],fs);me.materials.append(mats[material]);me.update();meshes[key]=me;return me
def part(room,group,label,position,size,material='Oak',kind='Box',collision=True,rotation=(0,0,0)):
 name='R%02d_P02_%s_%s'%(room,group,label);o=bpy.data.objects.new(name,primitive(kind,material));col.objects.link(o)
 o.location=position;o.scale=size;o.rotation_euler=rotation
 for k,v in {'StructureRole':'Room','StructureRoom':room,'StructureDoor':'','StructureSide':'','StructureCollision':collision,'P02Shared':True,'ArtAssembly':'R%02d_P02_%s'%(room,group)}.items():o[k]=v
 added.append(name);return o
def box(r,g,n,p,s,m='Oak',collision=True):return part(r,g,n,p,s,m,collision=collision)
def cyl(r,g,n,p,s,m='Brass',collision=True,rotation=(0,0,0)):return part(r,g,n,p,s,m,'Cylinder',collision,rotation)
def discard(name):
 o=bpy.data.objects.get(name)
 if o:deleted.append(name);bpy.data.objects.remove(o,do_unlink=True)
def table(r,g,x,y,z,width,depth,height=.8,material='Oak'):
 box(r,g,'Top',(x,y,z+height),(width,depth,.12),material)
 for i,(dx,dy) in enumerate([(-1,-1),(-1,1),(1,-1),(1,1)]):box(r,g,'Leg%d'%i,(x+dx*(width/2-.13),y+dy*(depth/2-.13),z+height/2),(.10,.10,height),material)
def chair(r,g,x,y,z,back=1,material='Walnut'):
 box(r,g,'Seat',(x,y,z+.46),(.5,.5,.12),material)
 for i,(dx,dy) in enumerate([(-1,-1),(-1,1),(1,-1),(1,1)]):box(r,g,'Leg%d'%i,(x+dx*.19,y+dy*.19,z+.23),(.065,.065,.46),material)
 box(r,g,'Back',(x,y+back*.21,z+.83),(.5,.08,.65),material)
def rug(r,g,x,y,z,w,d,color):
 box(r,g,'Border',(x,y,z+.008),(w,d,.012),'Brass',False)
 box(r,g,'Field',(x,y,z+.016),(w-.14,d-.14,.009),color,False)
def lamp(r,g,x,y,z):
 cyl(r,g,'Foot',(x,y,z+.025),(.28,.28,.05))
 cyl(r,g,'Stem',(x,y,z+.22),(.05,.05,.4))
 part(r,g,'Shade',(x,y,z+.49),(.42,.42,.33),'Ivory','Cone',False)

# Physical art is now audited as well as walls; rugs and suspended decor stay nonphysical.
for o in scene.objects:
 if o.type=='MESH' and o.get('StructureRole')=='Room':
  o['StructureCollision']=not (o.name.startswith('R01_') or 'Mullion' in o.name or 'WindowRail' in o.name or 'Hood' in o.name or 'Portrait' in o.name)

# Hall: a restrained geometric carpet, perimeter console and wall moulding.
rug(1,'HallRug',0,1.5,0,5.4,5.4,'Velvet')
for x in [-1.7,0,1.7]:box(1,'HallRug','Motif'+str(x),(x,1.5,.025),(.12,3.9,.006),'Ivory',False)
table(1,'Console',4.75,6.6,0,1.5,.65,.82,'Walnut');lamp(1,'ConsoleLamp',4.75,6.6,.90)
for o in list(scene.objects):
 if o.get('StructureRole')!='Wall' or o.get('StructureRoom',0)>6:continue
 if 'Pier' not in o.name:continue
 # Trim follows existing solid wall segments; it never bridges a door opening.
 rid=int(o['StructureRoom']);p=o.location.copy();s=o.dimensions.copy();p.z=.16;s.z=.22
 if s.x<s.y:s.x+=.045
 else:s.y+=.045
 box(rid,'Moulding',o.name,p,s,'Walnut',False)

# Library: books in the existing wall case, writing desk and reading lamp.
for shelf,z in enumerate([.58,1.13,1.68,2.23,2.78]):
 for n in range(10):
  color=['Wine','Blue','Green','Paper'][n%4];h=.28+(n%3)*.035
  box(2,'Bookcase','Book%d_%d'%(shelf,n),(-15.54,4.4+n*.27,z),(.36,.16,h),color)
table(2,'Desk',-11.7,7,0,2.1,.85,.84,'Walnut');chair(2,'DeskChair',-11.7,6.15,0,1)
lamp(2,'ReadingLamp',-12.35,7,.91)
box(2,'Desk','OpenBook',(-11.4,6.95,.923),(.55,.35,.035),'Paper',False)
box(2,'Desk','Drawer',(-11.1,6.65,.66),(.65,.12,.22),'Oak')

# Lounge: wall-side seating with an unobstructed centre and west door approaches.
rug(3,'SeatingRug',14.85,4.9,0,2.6,3.8,'Blue')
box(3,'Sofa','Plinth',(15.48,5.0,.2),(1.05,2.9,.4),'Walnut')
box(3,'Sofa','Back',(15.87,5, .83),(.25,2.9,1.05),'Blue')
for n,y in enumerate([4.1,5,5.9]):box(3,'Sofa','Cushion%d'%n,(15.27,y,.58),(.75,.84,.24),'Blue')
for n,y in enumerate([3.52,6.48]):box(3,'Sofa','Arm%d'%n,(15.4,y,.76),(1.1,.18,.6),'Walnut')
table(3,'CoffeeTable',14.45,5.0,0,.60,1.25,.48,'Walnut')
cyl(3,'CoffeeTable','Tray',(14.45,5,.56),(.40,.40,.035),'Brass',False)
box(3,'Portrait','Inner',(9.2,7.70,2.65),(1.10,.035,1.30),'Wine',False)
for n,(x,z,w,h) in enumerate([(8.56,2.65,.08,1.5),(9.84,2.65,.08,1.5),(9.2,1.92,1.3,.08),(9.2,3.38,1.3,.08)]):box(3,'Portrait','Frame%d'%n,(x,7.66,z),(w,.09,h),'Brass',False)

# Dining: southern banquet grouping; north/west service approaches remain clear.
for name in ['R04_Hero_BanquetSideboard','R04_Hero_SideboardMarble']:bpy.data.objects[name].location.x=-8
table(4,'DiningTable',-10.5,-10.1,0,3.8,.82,.82,'Walnut')
box(4,'DiningTable','Runner',(-10.5,-10.1,.89),(3.45,.37,.025),'Velvet',False)
for n,x in enumerate([-11.8,-10.5,-9.2]):
 for sign in [-1,1]:
  chair(4,'DiningChair%d_%d'%(n,sign),x,-10.1+sign*.72,0,sign)
  cyl(4,'DiningTable','Plate%d_%d'%(n,sign),(x,-10.1+sign*.24,.918),(.27,.27,.025),'Ivory',False)
for n,x in enumerate([-11.25,-9.75]):
 cyl(4,'DiningTable','CandleBase%d'%n,(x,-10.1,.94),(.18,.18,.07),'Brass',False)
 cyl(4,'DiningTable','Candle%d'%n,(x,-10.1,1.13),(.07,.07,.32),'Ivory',False)

# Kitchen: west-wall sink/work surface, cooker fittings and shelves.
box(5,'Sink','Cabinet',(-1.8,-9.55,.48),(.95,2.0,.96),'Ivory')
box(5,'Sink','Counter',(-1.8,-9.55,1.0),(1.08,2.12,.10),'Marble')
box(5,'Sink','Basin',(-1.8,-9.2,1.06),(.68,.65,.025),'Iron',False)
cyl(5,'Sink','Tap',(-2.08,-9.2,1.24),(.07,.07,.35),'Copper',False)
for n,y in enumerate([-10.1,-9.45,-8.95]):box(5,'Sink','Door%d'%n,(-1.30,y,.48),(.035,.48,.74),'Oak')
for n,x in enumerate([.4,1.6]):
 for k,y in enumerate([-11.15,-11.55]):cyl(5,'Cooker','Burner%d_%d'%(n,k),(x,y,1.12),(.35,.35,.035),'Iron',False)
for n,z in enumerate([1.9,2.5]):box(5,'Shelves','Shelf%d'%n,(-2.15,-9.5,z),(.45,1.8,.10),'Oak')
for n,y in enumerate([-10,-9.5,-9]):cyl(5,'Shelves','Jar%d'%n,(-2.15,y,2.1),(.20,.20,.30),'Ivory',False)

# Greenhouse: transparent structural panes and restrained low-poly foliage.
for o in scene.objects:
 if o.get('StructureRole')=='Wall' and o.get('StructureRoom')==6:
  o.data.materials.clear();o.data.materials.append(mats['Glass'])
discard('R06_Hero_Topiary')
for n,(x,y) in enumerate([(-3.8,15.6),(3.8,15.6),(3.85,10.6)]):
 if n:part(6,'Plant%d'%n,'Pot',(x,y,.31),(.74,.74,.62),'Terracotta','Cone')
 for k in range(7):
  angle=k*2.4;dx=math.cos(angle)*.30;dy=math.sin(angle)*.30
  part(6,'Plant%d'%n,'Leaf%d'%k,(x+dx,y+dy,.85+(k%3)*.20),(.36,.26,.62),['Green','Leaf'][k%2],'Cone',False,rotation=(.25*math.cos(angle),.3*math.sin(angle),angle))
table(6,'PottingShelf',3.9,13.2,0,.65,1.7,.65,'Oak')
for n,y in enumerate([12.7,13.3,13.8]):part(6,'PottingShelf','Pot%d'%n,(3.9,y,.88),(.24,.24,.34),'Terracotta','Cone',False)

# Boiler room: cylindrical vessel, bands, flue, connected pipe runs and valve wheels.
discard('R07_Hero_Boiler')
cyl(7,'Boiler','Vessel',(-12.8,-3.75,-2.9),(1.4,1.4,2.2),'Iron')
for n,z in enumerate([-3.8,-2.0]):cyl(7,'Boiler','Band%d'%n,(-12.8,-3.75,z),(1.47,1.47,.10),'Copper')
cyl(7,'Boiler','Flue',(-12.8,-3.75,-1.35),(.40,.40,.9),'Iron')
for n in range(3):
 discard('R07_Hero_Pipe_%d'%n)
 x=-13.6+n*.3;cyl(7,'PipeBank','Pipe%d'%n,(x,-3.1,-2.45),(.14,.14,3.0),'Copper')
 cyl(7,'PipeBank','Branch%d'%n,(x,-3.5,-2.9),(.12,.12,.8),'Copper',rotation=(math.pi/2,0,0))
 cyl(7,'PipeBank','Valve%d'%n,(x,-3.94,-2.9),(.28,.28,.07),'Wine',False,rotation=(math.pi/2,0,0))
box(7,'Boiler','GaugePlate',(-12.8,-4.47,-2.65),(.45,.04,.35),'Brass',False)

# Wine: rack is already moved away from all three entries; bottles identify its purpose.
box(8,'RackBacking','Panel',(1,-3.18,-2.85),(3.1,.10,2.35),'Walnut')
for shelf,z in enumerate([-3.45,-2.85,-2.25]):
 for n in range(6):
  x=-.18+n*.47
  cyl(8,'WineBottles','Body%d_%d'%(shelf,n),(x,-3.48,z),(.19,.19,.38),'Green',False,rotation=(math.pi/2,0,0))
  cyl(8,'WineBottles','Neck%d_%d'%(shelf,n),(x,-3.72,z),(.085,.085,.16),'Green',False,rotation=(math.pi/2,0,0))
  cyl(8,'WineBottles','Label%d_%d'%(shelf,n),(x,-3.49,z),(.198,.198,.15),'Paper',False,rotation=(math.pi/2,0,0))
for n,(x,y) in enumerate([(-2.55,-9.2),(-1.55,-9.9)]):
 cyl(8,'Barrel%d'%n,'Body',(x,y,-3.4),(.78,.78,1.2),'Oak')
 for k,z in enumerate([-3.85,-2.95]):cyl(8,'Barrel%d'%n,'Hoop%d'%k,(x,y,z),(.81,.81,.08),'Iron')
box(8,'Crate','Body',(-2.8,-10.35,-3.67),(.68,.68,.66),'Oak')
for n,z in enumerate([-3.88,-3.5]):box(8,'Crate','Band%d'%n,(-2.8,-10.70,z),(.68,.025,.065),'Walnut',False)

# Servants: bedding, modest bedside storage and a small lamp.
box(9,'Bed','Blanket',(12.7,-6.5,-3.24),(1.16,1.32,.06),'Blue',False)
box(9,'Bed','Pillow',(12.7,-5.53,-3.21),(.78,.40,.16),'Ivory',False)
box(9,'Bedside','Cabinet',(11.65,-7.65,-3.68),(.60,.60,.64),'Oak')
lamp(9,'BedsideLamp',11.65,-7.65,-3.33)
for n,z in enumerate([-3.2,-2.7]):box(9,'Locker','Handle%d'%n,(12.8,-2.70,z),(.25,.06,.05),'Iron',False)

for name,p in refs.items():assert (bpy.data.objects[name].matrix_world.translation-Vector(p)).length<1e-5,name
for me in meshes.values():
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
tri=0
for o in scene.objects:
 if o.type=='MESH':o.data.calc_loop_triangles();tri+=len(o.data.loop_triangles)
json.dump({'added':added,'replaced':deleted,'tris_before':6796,'tris_after':tri,'unique_new_meshes':len(meshes),'shared_instances':len(added),'protected_references':refs},open(os.path.join(OUT,'pass02_manifest.json'),'w'),indent=1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Mansion_Environment_Pass02.blend'))
print('Pass02 art:',len(added),'instances /',len(meshes),'shared meshes; tris 6796 ->',tri)
