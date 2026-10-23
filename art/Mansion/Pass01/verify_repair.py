import bpy,json,os
from mathutils import Vector
from mathutils.bvhtree import BVHTree
OUT=r"C:\UnityProjects\DeductionGame\deduction-game\art\Mansion\Pass01"
def tree(o):return BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons])
scene=bpy.context.scene
result={'passage_intersections':[],'headroom':[],'stand_issues':[], 'stair_wall_intersections':[], 'collision_envelope_headroom_obstructions':[]}
bridge=[o for o in bpy.data.collections['DOOR_03_05'].objects if o.type=='MESH']
for o in bridge:
 for wn in ['R01_East_Pier_00','R01_South_Pier_05']:
  if tree(o).overlap(tree(bpy.data.objects[wn])):result['passage_intersections'].append([o.name,wn])
for a,b in [(4,7),(5,8)]:
 col=bpy.data.collections['DOOR_%02d_%02d'%(a,b)]
 obstacles=[o for o in scene.objects if o.type=='MESH' and (o.name.endswith('_Floor') or o.name.startswith(('R%02d_'%a,'R%02d_'%b)) and ('Pier' in o.name or 'Lintel' in o.name) or o in list(col.objects) and 'Landing' in o.name)]
 trees=[(o.name,tree(o)) for o in obstacles];blocked=[]
 for step in col.objects:
  if '_Step_' not in step.name:continue
  # Centre and 0.6m either side; clearance exceeds current CharacterActor 1.1m.
  axis=step.matrix_world.to_3x3()@Vector((0,1,0))
  for lateral in [-.6,0,.6]:
   origin=step.matrix_world.translation+axis*lateral;origin.z+=.09+.015
   for name,t in trees:
    hit=t.ray_cast(origin,Vector((0,0,1)),1.3)
    if hit[0] is not None:blocked.append([step.name,name,round(hit[3],3),lateral])
 result['headroom'].append({'door':[a,b],'obstructions':blocked})
 for envelope in col.objects:
  if '_COLLISION_Flight_' not in envelope.name:continue
  et=tree(envelope)
  for wall in scene.objects:
   if wall.type=='MESH' and wall.name.startswith(('R%02d_'%a,'R%02d_'%b)) and ('Pier' in wall.name or 'Lintel' in wall.name):
    if et.overlap(tree(wall)):result['stair_wall_intersections'].append([envelope.name,wall.name])
  vs=[envelope.matrix_world@v.co for v in envelope.data.vertices]
  start=(vs[4]+vs[5])/2;end=(vs[6]+vs[7])/2;side=(vs[5]-vs[4]).normalized()
  for t in [.1,.3,.5,.7,.9]:
   for lateral in [-.6,0,.6]:
    origin=start.lerp(end,t)+side*lateral+Vector((0,0,.02))
    for name,obstacle in trees:
     hit=obstacle.ray_cast(origin,Vector((0,0,1)),1.3)
     if hit[0] is not None:result['collision_envelope_headroom_obstructions'].append([envelope.name,name,t,lateral,hit[3]])
 floor=tree(bpy.data.objects['R%02d_Floor'%a])
 for slot in range(1,7):
  p=bpy.data.objects['REF_R%02d_Stand_%02d'%(a,slot)].location
  for dx,dy in [(0,0),(.3,0),(-.3,0),(0,.3),(0,-.3)]:
   if floor.ray_cast(p+Vector((dx,dy,0)),Vector((0,0,-1)),2)[0] is None:result['stand_issues'].append([a,slot,'floor missing'])
original=json.load(open(OUT+r"\repair_reference_snapshot.json"))
result['references_preserved']=all(n in bpy.data.objects and (bpy.data.objects[n].matrix_world.translation-Vector(p)).length<1e-5 for n,p in original.items())
result['gallery_step_difference']=round((bpy.data.objects['R01_Hero_Gallery'].location.z+.11)-2.16,5)
result['status']='OK' if not result['passage_intersections'] and not result['stand_issues'] and not result['stair_wall_intersections'] and not result['collision_envelope_headroom_obstructions'] and all(not r['obstructions'] for r in result['headroom']) and result['references_preserved'] else 'FAILED'
open(OUT+r"\repair_blender_verification.json",'w').write(json.dumps(result,indent=2))
print(json.dumps(result))
