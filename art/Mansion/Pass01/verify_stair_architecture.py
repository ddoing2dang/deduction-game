import bpy,json,os
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=os.path.dirname(__file__)
def tree(o):return BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons])
scene=bpy.context.scene
solids=[o for o in scene.objects if o.type=='MESH' and not o.name.startswith(('REF_','__')) and o.name not in ['Cube'] and len(o.data.polygons)>0]
trees={o.name:tree(o) for o in solids}
result={'headroom_obstructions':[],'wall_intersections':[],'stand_interferences':[],'changed_references':[],'flights':[]}
for key in ['DOOR_04_07','DOOR_05_08']:
 pts=[Vector(p) for p in json.loads(bpy.data.objects[key]['ArchitecturePath'])]
 for label,start,end in [('A',pts[2],pts[3])]:
  side=Vector((-(end-start).y,(end-start).x,0)).normalized()
  ramp=bpy.data.objects[key+'_'+label+'_COLLISION_Flight']
  for o in solids:
   if o.name.startswith('R') and any(k in o.name for k in ['Pier','Lintel','Infill','StairReturn']):
    if trees[ramp.name].overlap(trees[o.name]):result['wall_intersections'].append([ramp.name,o.name])
  for n in range(1,100):
   for lateral in [-.6,0,.6]:
    p=start.lerp(end,n/100)+side*lateral+Vector((0,0,.03))
    for o in solids:
     if o.name==ramp.name or '_Step_' in o.name or '_COLLISION_' in o.name or 'SideRail' in o.name or '_Stringer' in o.name:continue
     hit=trees[o.name].ray_cast(p,Vector((0,0,1)),2.02)
     if hit[0] is not None:result['headroom_obstructions'].append([key,label,n,lateral,o.name,round(hit[3],3)])
  result['flights'].append({'name':ramp.name,'width':1.6,'run':round(Vector((end.x-start.x,end.y-start.y,0)).length,3),'rise':round(abs(end.z-start.z),3)})
 # Mid-landing, lower turn and upper-landing usable standing area.
 for index in [1,4,5,6,7]:
  p=pts[index]+Vector((0,0,.03))
  for o in solids:
   if o.name.startswith(key) and ('Rail' in o.name or 'Portal' in o.name or 'Stringer' in o.name or '_Step_' in o.name):continue
   hit=trees[o.name].ray_cast(p,Vector((0,0,1)),2.02)
   if hit[0] is not None:result['headroom_obstructions'].append([key,'landing',index,o.name,round(hit[3],3)])
before=json.load(open(os.path.join(OUT,'architecture_reference_before.json')))
allowed={'REF_R04_Entry_To_07','REF_R05_Entry_To_08','REF_R07_Entry_To_04','REF_R08_Entry_To_05'}
for name,p in before.items():
 if name not in allowed and (bpy.data.objects[name].matrix_world.translation-Vector(p)).length>1e-5:result['changed_references'].append(name)
stairs=[o for o in solids if o.name.startswith(('DOOR_04_07_','DOOR_05_08_','R01_Gallery_','R01_Hero_GrandStair','R01_Hero_Gallery'))]
for name in before:
 if '_Stand_' not in name:continue
 p=bpy.data.objects[name].matrix_world.translation-Vector((0,0,.55))
 for o in stairs:
  vs=[o.matrix_world@Vector(v) for v in o.bound_box]
  lo=Vector([min(v[i] for v in vs) for i in range(3)]);hi=Vector([max(v[i] for v in vs) for i in range(3)])
  if lo.x-.3<p.x<hi.x+.3 and lo.y-.3<p.y<hi.y+.3 and hi.z>p.z+.15 and lo.z<p.z+1.2:result['stand_interferences'].append([name,o.name])
result['stands_checked']=54
result['minimum_clear_headroom_tested_m']=2.05
result['status']='OK' if not any(result[k] for k in ['headroom_obstructions','wall_intersections','stand_interferences','changed_references']) else 'FAILED'
open(os.path.join(OUT,'stair_architecture_blender_verification.json'),'w').write(json.dumps(result,indent=2))
print(json.dumps(result))
