"""Decorative closed front door on a measured, unchanged solid hall wall. Run via Blender MCP."""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent
s=bpy.context.scene
assert Path(bpy.data.filepath)==P/'Mansion_VisualGold.blend'
wall=bpy.data.objects['R01_Wall_South_Pier_00']
assert not any(o.name.startswith('VG_DecorativeFrontDoor') for o in s.objects)
baseline={o.name:[[float(v) for v in row] for row in o.matrix_world] for o in s.objects if o.name.startswith('REF_')}
parts=[]
def mat(n):return bpy.data.materials['Gold_'+n]
wood=mat('MahoganyPanelling');walnut=mat('Walnut');brass=mat('AgedBrass');stone=mat('Limestone');iron=mat('BlackenedIron')
glow=bpy.data.materials.new('Gold_FrontDoorLampGlass');glow.use_nodes=True
bs=next(n for n in glow.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(1,.65,.28,1);bs.inputs['Emission Color'].default_value=(1,.58,.20,1);bs.inputs['Emission Strength'].default_value=2;bs.inputs['Roughness'].default_value=.35
def finish(o,name,m):
 o.name='FrontDoor_'+name;o.data.materials.append(m);parts.append(o)
 if not o.data.uv_layers:o.data.uv_layers.new(name='UVMap')
 for p in o.data.polygons:
  axes=[0,2] if abs(p.normal.y)>.5 else [1,2] if abs(p.normal.x)>.5 else [0,1]
  for li in p.loop_indices:
   v=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co;o.data.uv_layers.active.data[li].uv=(v[axes[0]]*.6,v[axes[1]]*.6)
 return o
def box(name,loc,size,m,bev=.015):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bev:
  b=o.modifiers.new('Crafted edge','BEVEL');b.width=min(bev,min(size)*.23);b.segments=2;bpy.ops.object.modifier_apply(modifier=b.name)
 return finish(o,name,m)
def rod(name,a,b,r,m):
 a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=(b-a).length,location=(a+b)/2);o=bpy.context.object;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return finish(o,name,m)
cx=-4.20
for side in [-1,1]:
 # Exterior negative Y / hall interior positive Y; overlay existing solid wall, never cut it.
 y=-3.5+side*.255
 def face(name,x,z,w,h,m,depth=.045,offset=0):return box(name,(x,y+side*offset,z),(w,depth,h),m)
 for leaf in [-1,1]:
  x=cx+leaf*.559
  face('SolidWalnutLeaf',x,1.67,1.10,3.24,walnut,.065)
  for z,h in [(.55,.72),(1.72,1.20),(2.84,.58)]:
   face('RecessedWoodPanel',x,z,.82,h,wood,.025,.045)
   for dx in [-.445,.445]:face('PanelStile',x+dx,z,.048,h+.09,walnut,.035,.072)
   for dz in [-h/2-.025,h/2+.025]:face('PanelRail',x,z+dz,.93,.048,walnut,.035,.072)
   for dx in [-.382,.382]:face('BrassInlay',x+dx,z,.009,h-.06,brass,.012,.078)
  # Long brass pulls stand off the door on two short mounting brackets.
  hx=cx+leaf*.115
  face('PullBackplate',hx,1.57,.11,.53,brass,.018,.095)
  for z in [1.39,1.75]:rod('HandleMount',(hx,y+side*.1,z),(hx,y+side*.18,z),.021,brass)
  rod('BrassPull',(hx,y+side*.18,1.39),(hx,y+side*.18,1.75),.025,brass)
  for z in [.48,1.6,2.77]:face('Hinge',cx+leaf*1.075,z,.045,.15,brass,.035,.087)
 face('MeetingStile',cx,1.67,.032,3.24,brass,.024,.09)
 for dx in [-1.23,1.23]:
  face('CarvedJamb',cx+dx,1.8,.22,3.6,walnut,.16,.02)
  face('JambFillet',cx+dx,1.84,.055,3.38,brass,.03,.13)
  face('PlinthBlock',cx+dx,.18,.32,.34,stone,.23,.04)
  face('Capital',cx+dx,3.53,.35,.18,stone,.22,.05)
 for z,w,h,m in [(3.35,2.34,.13,walnut),(3.70,2.85,.18,stone),(3.85,3.02,.11,walnut),(3.92,3.13,.06,brass)]:face('Entablature',cx,z,w,h,m,.19,.04)
 face('Frieze',cx,3.53,2.2,.24,wood,.10,.03)
 for dx in [-.82,-.41,0,.41,.82]:face('FriezeBrassRosette',cx+dx,3.54,.045,.075,brass,.025,.10)
 # Shallow threshold is trim only. No floor, collider, entry, trigger or nav link is introduced.
 face('Threshold',cx,.045,2.27,.07,stone,.24,.02)
 for dx in [-1.53,1.53]:
  x=cx+dx
  face('SconceBackplate',x,2.48,.18,.58,brass,.06,.035)
  rod('SconceArm',(x,y+side*.06,2.28),(x,y+side*.26,2.28),.026,brass)
  face('SconceLantern',x,2.54,.16,.33,glow,.14,.25)
  for z in [2.35,2.73]:face('LanternCap',x,z,.23,.07,iron,.20,.25)
  for dd in [-.09,.09]:face('LanternMullion',x+dd,2.54,.018,.34,brass,.17,.25)
# Exterior doorstep supports the decorative threshold over the existing estate ground cutout.
# Shallow facade trim only: no collider, opening or new traversable connection.
box('ExteriorStoneApron',(cx,-4.00,-.055),(2.75,1.03,.11),stone,.025)
box('ApronNosing',(cx,-4.49,-.035),(2.84,.11,.07),stone,.015)
# Join decorative assembly: a single non-gameplay architectural assembly, not a gameplay door.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name='VG_DecorativeFrontDoor_ClosedDoubleLeaf'
for k,v in {'StructureRole':'Trim','StructureRoom':1,'StructureDoor':'','StructureSide':'South','StructureCollision':False,'GoldParent':'','DecorativeWall':wall.name,'ArtAssembly':'DecorativeFrontDoor','DecorativeOnly':True}.items():o[k]=v
bpy.context.view_layer.update()
assert all(max(abs(bpy.data.objects[n].matrix_world[i][j]-m[i][j]) for i in range(4)for j in range(4))<1e-6 for n,m in baseline.items())
placements=json.loads((P/'placements.json').read_text())
o.data.calc_loop_triangles()
report={'wall':wall.name,'centerBlender': [cx,-3.5,0],'leafWidth':2.22,'height':3.95,'decorativeOnly':True,'newDoorIDs':0,'newColliders':0,'newPaths':0,'referenceCountUnchanged':len(baseline),'cc0Types':len({p['slug']for p in placements}),'cc0Placements':len(placements),'addedTris':len(o.data.loop_triangles),'backup':'Backup/FrontDoorFollowup','parts':[o.name]}
(P/'front-door-report.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'),compress=True)
print(json.dumps(report))
