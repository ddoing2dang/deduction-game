import bpy,json,os
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=os.path.dirname(__file__);AUDIT=os.path.join(OUT,'StructureAudit')
manifest=json.load(open(os.path.join(AUDIT,'environment_manifest.json')));changes=json.load(open(os.path.join(AUDIT,'cleanup_changes.json')))
def tree(o):return BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons])
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.vertices)>0]
trees={o.name:tree(o) for o in meshes}
result={'unknown':[],'unmapped_doors':[],'wall_stair_intersections':[],'headroom':[],'protected_references':[],'stand_interference':[],'floor_count':{},'door_assemblies':{},'role_counts':{}}
keys={r['door'] for r in manifest['layout']['routes']}|{'DOOR_09_06_SECRET'}
for o in meshes:
 r=o.get('StructureRole');result['role_counts'][r]=result['role_counts'].get(r,0)+1
 if not r:result['unknown'].append(o.name)
 if r=='Door':
  key=o.get('StructureDoor');result['door_assemblies'].setdefault(key,[]).append(o.name)
  if key not in keys:result['unmapped_doors'].append(o.name)
for i in range(1,10):result['floor_count'][i]=sum(o.get('StructureRole')=='Floor' and o.get('StructureRoom')==i for o in meshes)
for route in manifest['layout']['routes']:
 if route['door'] not in ['DOOR_04_07','DOOR_05_08']:continue
 key=route['door'];pts=[Vector((p['x'],p['z'],p['y'])) for p in route['points']];start,end=pts[2:4];side=Vector((1,0,0));ramp=bpy.data.objects[key+'_A_COLLISION_Flight']
 for wall in meshes:
  if wall.get('StructureRole')=='Wall' and trees[ramp.name].overlap(trees[wall.name]):result['wall_stair_intersections'].append([ramp.name,wall.name])
 for n in range(1,100):
  for dx in [-.6,0,.6]:
   p=start.lerp(end,n/100)+Vector((dx,0,.03))
   for o in meshes:
    if o.get('StructureRole') in ['Stair','Railing'] or o.name==ramp.name:continue
    hit=trees[o.name].ray_cast(p,Vector((0,0,1)),2.02)
    if hit[0] is not None:result['headroom'].append([key,n,dx,o.name,round(hit[3],3)])
for name,p in changes['references_before'].items():
 if '_Entry_To_' not in name and (bpy.data.objects[name].matrix_world.translation-Vector(p)).length>1e-5:result['protected_references'].append(name)
 if '_Stand_' not in name:continue
 p=Vector(p)-Vector((0,0,.55))
 for o in meshes:
  if o.get('StructureRole') not in ['Wall','Door','Gallery','Railing','Room']:continue
  # Triangle ray bundle at capsule footprint, from feet through the actual actor's head.
  for dx,dy in [(0,0),(.27,0),(-.27,0),(0,.27),(0,-.27)]:
   hit=trees[o.name].ray_cast(p+Vector((dx,dy,.05)),Vector((0,0,1)),1.1)
   if hit[0] is not None:result['stand_interference'].append([name,o.name]);break
result['LOCK_routes']=len(manifest['layout']['routes']);result['secret_panels']=sum(o.get('StructureDoor')=='DOOR_09_06_SECRET' for o in meshes)
result['service_flights']=sum(o.get('StructureRole')=='Stair' and '_COLLISION_' in o.name for o in meshes)
result['decorative_gallery']=True
result['status']='OK' if all(not result[k] for k in ['unknown','unmapped_doors','wall_stair_intersections','headroom','protected_references','stand_interference']) and all(v==1 for v in result['floor_count'].values()) and result['LOCK_routes']==11 and result['service_flights']==2 else 'FAILED'
json.dump(result,open(os.path.join(AUDIT,'blender-verification.json'),'w'),indent=2)
print(json.dumps({k:v for k,v in result.items() if k!='door_assemblies'}))
