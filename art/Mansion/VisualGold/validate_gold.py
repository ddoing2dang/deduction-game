import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).parent;ns=bpy.app.driver_namespace['GoldAuthoring'];s=bpy.context.scene
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();rows=[];bad=[];floorprops=[]
for o in s.objects:
 if not o.get('GoldAsset')or not o.get('StructureRoom'):continue
 lo,hi=ns['bbox']([o]);room=int(o['StructureRoom']);issues=ns['conflicts'](lo,hi,room)
 row={'name':o.name,'room':room,'source':o['GoldAsset'],'min':list(lo),'max':list(hi),'protectedConflicts':issues};rows.append(row)
 if issues:bad.append(row)
 floor=ns['floorbounds'][room][1].z
 if lo.z<=floor+.06 and o.get('StructureCollision'):floorprops.append(o)
intersections=[]
for i,a in enumerate(floorprops):
 av=[a.matrix_world@v.co for v in a.data.vertices];at=BVHTree.FromPolygons(av,[list(p.vertices)for p in a.data.polygons])
 for b in floorprops[i+1:]:
  if b['StructureRoom']!=a['StructureRoom']:continue
  bv=[b.matrix_world@v.co for v in b.data.vertices];bt=BVHTree.FromPolygons(bv,[list(p.vertices)for p in b.data.polygons]);hits=at.overlap(bt)
  if hits:intersections.append({'a':a.name,'b':b.name,'triangleIntersections':len(hits)})
base=json.loads((P/'baseline-refs.json').read_text());changed=[n for n,v in base.items()if max(abs(bpy.data.objects[n].matrix_world[i][j]-v['matrix'][i][j])for i in range(4)for j in range(4))>1e-6]
r={'furniture':rows,'protectedConflicts':bad,'furnitureIntersections':intersections,'unchangedRefs':len(base)-len(changed),'changedRefs':changed,'sourcePreserved':hashlib.sha256((P.parent/'Pass02/Mansion_Environment_Pass02.blend').read_bytes()).hexdigest()==(P/'source-pass02.sha256').read_text()}
(P/'blender-final-audit.json').write_text(json.dumps(r,indent=2));print(json.dumps({k:v for k,v in r.items()if k!='furniture'}))
