"""Read-only Blender MCP validation of visible art, not just collision envelopes."""
import bpy,os,json
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=os.path.dirname(__file__);baseline=json.load(open(os.path.join(OUT,'pass02_manifest.json')))
props=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.get('StructureRole')=='Room']
issues=[];changed=[];portals=[]
for name,p in baseline['protected_references'].items():
 if (bpy.data.objects[name].matrix_world.translation-Vector(p)).length>1e-5:changed.append(name)
for ref in bpy.context.scene.objects:
 if not ref.name.startswith('REF_') or not any(k in ref.name for k in ['_Stand_','_Entry_To_','_Action_Point']):continue
 foot=ref.matrix_world.translation.copy();foot.z-=.55 if '_Stand_' in ref.name else .05
 radius=.4 if '_Stand_' in ref.name else .5
 for o in props:
  if o.get('StructureRoom')!=int(ref.name[5:7]):continue
  vs=[o.matrix_world@v.co for v in o.data.vertices];lo=Vector(tuple(min(v[i] for v in vs) for i in range(3)));hi=Vector(tuple(max(v[i] for v in vs) for i in range(3)))
  if hi.z<foot.z+.08 or lo.z>foot.z+1.3:continue
  dx=max(lo.x-foot.x,0,foot.x-hi.x);dy=max(lo.y-foot.y,0,foot.y-hi.y)
  if dx*dx+dy*dy<radius*radius:issues.append([ref.name,o.name,round((dx*dx+dy*dy)**.5,3)])
# Three waist/head-height bundles span each Wine doorway and its inner approach.
for key,point,direction,width in [('05_08',(2.8,-11,-4),(0,1,0),1.6),('07_08',(-3.5,-6.6,-4),(1,0,0),2),('08_09',(5.5,-4.4,-4),(-1,0,0),1.6)]:
 direction=Vector(direction);side=Vector((-direction.y,direction.x,0))
 for o in props:
  if o.get('StructureRoom')!=8:continue
  tree=BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons])
  for lateral in [-width*.3,0,width*.3]:
   for height in [.35,.8,1.2]:
    p=Vector(point)+side*lateral+Vector((0,0,height))
    if tree.ray_cast(p,direction,1.55)[0] is not None:portals.append([key,o.name,lateral,height])
headroom=[]
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.get('StructureRole') not in ['Stair','Railing']]
trees={o.name:BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons]) for o in meshes}
layout=json.load(open(os.path.join(OUT,'environment_manifest.json')))['layout']
for route in layout['routes']:
 if route['door'] not in ['DOOR_04_07','DOOR_05_08']:continue
 points=[Vector((p['x'],p['z'],p['y'])) for p in route['points']]
 for n in range(1,100):
  for dx in [-.6,0,.6]:
   start=points[2].lerp(points[3],n/100)+Vector((dx,0,.03))
   for o in meshes:
    if trees[o.name].ray_cast(start,Vector((0,0,1)),2.02)[0] is not None:headroom.append([route['door'],n,dx,o.name])
tris=0
for o in bpy.context.scene.objects:
 if o.type=='MESH':o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
result={'protected_reference_changes':changed,'art_footprint_conflicts':issues,'wine_portal_visual_obstructions':portals,'service_stair_headroom_obstructions':headroom,'tris':tris,'status':'OK' if not(changed or issues or portals or headroom) else 'FAILED'}
json.dump(result,open(os.path.join(OUT,'blender-verification.json'),'w'),indent=1);print(json.dumps(result))
assert result['status']=='OK',result
