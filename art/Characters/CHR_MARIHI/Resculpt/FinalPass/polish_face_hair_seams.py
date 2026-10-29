import bpy,math,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
P=Path(__file__).parent;rig=bpy.data.objects['Armature'];game=bpy.data.collections['Game_LOD_Meshes'];archive=bpy.data.collections['Approved_Source_Archive'];rig.data.pose_position='REST'
# Put the sleeve material seam on a complete shared ring, not the jagged old shoulder triangulation.
for o in list(game.objects)+[bpy.data.objects['FinalTailor_ConnectedTop_Source']]:
 if o.type!='MESH' or not o.name.startswith(('Outfit_LOD','FinalTailor_')):continue
 names=[m.name for m in o.data.materials]
 if 'MAT_MARIHI_TailoredCloth'not in names:continue
 a=names.index('MAT_MARIHI_TailoredCloth');b=names.index('MAT_MARIHI_SleevePrint');uv=o.data.uv_layers.active.data
 for p in o.data.polygons:
  if p.material_index not in [a,b]:continue
  sleeve=abs(p.center.x)>.26;p.material_index=b if sleeve else a
  for li in p.loop_indices:
   q=o.data.vertices[o.data.loops[li].vertex_index].co
   uv[li].uv=((abs(q.x)-.24)/.30,(math.atan2(q.z-1.2384,q.y-.0247)+math.pi)/math.tau)if sleeve else((math.atan2(q.x,-(q.y-.014))+math.pi)/math.tau,1 if abs(p.center.x)>.18 else(q.z-.98)/.34)
def g(a,c,w):return math.exp(-((a-c)/w)**2)
def faceform(p):
 x,y,z=p;ax=abs(x);f=max(0,min(1,(-y+.025)/.07));sign=1 if x>=0 else-1
 dx=(-sign*.0038*g(ax,.098,.036)*g(z,1.408,.035)-sign*.0030*g(ax,.079,.038)*g(z,1.357,.021)+sign*.0028*g(ax,.024,.025)*g(z,1.327,.018))*f
 dy=(-.0042*g(ax,.075,.039)*g(z,1.403,.025)-.0027*g(x,0,.021)*g(z,1.406,.016)+.0045*g(x,0,.013)*g(z,1.384,.010)-.0036*g(x,0,.035)*g(z,1.357,.010)-.0020*g(x,0,.028)*g(z,1.334,.014))*f
 dz=(.0028*g(ax,.092,.028)*g(z,1.452,.025)-.0014*g(ax,.035,.023)*g(z,1.444,.022)+.0018*g(x,0,.026)*g(z,1.323,.012))*f
 return p+Vector((dx,dy,dz))
counts={}
for o in list(game.objects)+[bpy.data.objects['Face'],bpy.data.objects['Body']]:
 if not o.name.startswith(('Face','Body')):continue
 before=[v.co.copy()for v in o.data.vertices]
 if o.data.shape_keys:
  for key in o.data.shape_keys.key_blocks:
   for v in key.data:v.co=faceform(v.co)
  for v in o.data.vertices:v.co=o.data.shape_keys.key_blocks[0].data[v.index].co
 else:
  for v in o.data.vertices:
   if v.co.z>1.31:v.co=faceform(v.co)
 o.data.update();counts[o.name]={'moved':sum((v.co-p).length>1e-7 for v,p in zip(o.data.vertices,before)),'maxDelta':max((v.co-p).length for v,p in zip(o.data.vertices,before))}
# Edit the actual Marihi clumps and transfer displacement to each game hair LOD.
old=[];new=[];tri=[]
for o in archive.objects:
 if not o.name.startswith(('Hair_Fringe_','Hair_WaveTail_')):continue
 before=[v.co.copy()for v in o.data.vertices];after=[p.copy()for p in before];nc=7 if o.name.startswith('Hair_Fringe')else 8;nr=len(before)//nc
 for j in range(nr):
  t=j/(nr-1);ids=list(range(j*nc,(j+1)*nc));c=sum((before[i]for i in ids),Vector())/nc
  for k,i in enumerate(ids):
   if o.name.startswith('Hair_Fringe'):
    u=k/(nc-1)*2-1;w=max(0,(t-.72)/.28)
    # A rounded cloth-like clump end, not a sharp triangular spike; roots stay untouched.
    after[i].z+=.0045*w*w*u*u;after[i].x+=(before[i].x-c.x)*.10*w
    after[i].y-=.0015*w*(1-u*u)
   else:
    w=max(0,(t-.70)/.30);d=before[i]-c;after[i]=c+Vector((d.x*(1-.20*w),d.y*(1-.12*w),d.z))
    after[i].x+=(1 if c.x>0 else-1)*.005*math.sin(w*math.pi)*w;after[i].y+=.005*w*w
 for v,q in zip(o.data.vertices,after):v.co=q
 off=len(old);old.extend(before);new.extend(after);o.data.calc_loop_triangles();tri.extend(tuple(i+off for i in t.vertices)for t in o.data.loop_triangles)
tree=BVHTree.FromPolygons(old,tri,all_triangles=True)
for o in game.objects:
 if not o.name.startswith('Hair_Main'):continue
 for v in o.data.vertices:
  h,n,i,d=tree.find_nearest(v.co)
  if d>.008:continue
  ids=tri[i];q=barycentric_transform(h,*[old[i]for i in ids],*[new[i]for i in ids]);v.co+=q-h
 o.data.update()
rig.data.pose_position='POSE';bpy.context.view_layer.update();(P/'face-hair-edit.json').write_text(json.dumps(counts,indent=2));bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI_FINAL_REVIEW.blend'),compress=True)
print(counts)
