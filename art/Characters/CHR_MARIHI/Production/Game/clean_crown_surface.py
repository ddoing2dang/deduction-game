import bpy,math,json,ast
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).parent;s=bpy.context.scene;rig=bpy.data.objects['Armature'];archive=bpy.data.collections['Approved_Source_Archive'];game=bpy.data.collections['Game_LOD_Meshes']
vs=[];fs=[]
for o in [bpy.data.objects['Body_LOD0'],bpy.data.objects['Face_LOD0']]:
 for poly in o.data.polygons:
  if o.name.startswith('Face')and poly.material_index!=0:continue
  if min(o.data.vertices[i].co.z for i in poly.vertices)<1.30:continue
  off=len(vs);vs.extend([o.data.vertices[i].co.copy()for i in poly.vertices]);fs.append(list(range(off,len(vs))))
tree=BVHTree.FromPolygons(vs,fs);origin=Vector((0,.022,1.490))
def surface(d,offset):
 d=d.normalized();h,n,_,_=tree.ray_cast(origin+d*.45,-d,.9)
 if h is None:raise ValueError('Scalp ray missed')
 return h+d*offset
for ob in list(archive.objects):
 if ob.name=='Hair_ScalpCrown'or ob.name.startswith(('Hair_Fringe_','Hair_Sweep_')):bpy.data.objects.remove(ob,do_unlink=True)
mat=bpy.data.materials['MARIHI_ChampagneBlonde']
def source(name,vs,fs,uv):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);archive.objects.link(o);o.parent=bpy.data.objects['Mesh'];me.materials.append(mat);ul=me.uv_layers.new(name='UVMap')
 for p in me.polygons:
  p.use_smooth=True
  for li in p.loop_indices:ul.data[li].uv=uv[me.loops[li].vertex_index]
 o.vertex_groups.new(name='J_Bip_C_Head').add(list(range(len(me.vertices))),1,'REPLACE');m=o.modifiers.new('Humanoid Skin','ARMATURE');m.object=rig;return o
vs=[];fs=[];uv=[];nr=22;nc=64
for j in range(nr+1):
 for k in range(nc):
  th=math.tau*k/nc;back=(1-math.cos(th))*.5;end=math.radians(77+37*back)
  ph=.018+(end-.018)*j/nr;d=Vector((math.sin(th)*math.sin(ph),-math.cos(th)*math.sin(ph),math.cos(ph)))
  vs.append(surface(d,.0032));uv.append((k/nc,j/nr))
for j in range(nr):
 for k in range(nc):a=j*nc+k;b=j*nc+(k+1)%nc;fs.append((a,b,b+nc,a+nc))
source('Hair_ScalpCrown',vs,fs,uv)
spec=[(-.030,-.112,1.461,.026),(-.022,-.079,1.445,.029),(-.010,-.044,1.454,.030),(.003,-.010,1.441,.030),(.014,.024,1.452,.029),(.025,.057,1.466,.027),(.036,.087,1.462,.024),(.044,.114,1.479,.021)]
for i,(xr,xe,ze,width)in enumerate(spec):
 vs=[];fs=[];uv=[];nr=27;nc=7
 for j in range(nr+1):
  t=j/nr;phi=.10+(1.90-.10)*t;blend=t*t*(3-2*t);x=xr+(xe-xr)*blend
  z=1.490+math.cos(phi)*.139;z+=(ze-(1.490+math.cos(1.90)*.139))*t**3
  w=.0015*(1-t)+width*math.sin(math.pi*t)**.72
  for k in range(nc):
   u=k/(nc-1)*2-1;xx=x+u*w;y=.022-.145*math.sin(phi)*math.sqrt(max(.05,1-(xx/.153)**2))-.006-.002*(1-u*u)
   q=Vector((xx,y,z))
   if j<=6:q=surface(q-origin,.0045+(.001*i+.0015)*min(1,j/3))
   elif t<.72:
    h=tree.ray_cast(Vector((xx,-.4,z)),Vector((0,1,0)),.75)[0]
    if h:q.y=min(q.y,h.y-(.009+.001*i)-.002*(1-u*u))
   vs.append(q);uv.append((k/(nc-1),t))
 for j in range(nr):
  for k in range(nc-1):a=j*nc+k;fs.append((a,a+1,a+nc+1,a+nc))
 source('Hair_Fringe_%02d'%i,vs,fs,uv)
# Rebuild from the editable source without projecting overlapping layers onto one surface again.
script=(P/'rebuild_hair_lods.py').read_text();section=script[script.index('verts=[];faces=[];uvs=[];colors=[];weights=[]'):]
ns=globals();ns['src']=archive;ns['parts']=[o for o in archive.objects if o.name.startswith('Hair_')]
exec(compile(section,'rebuild_clean_hair_lods','exec'),ns)
print('Fresh monotonic scalp grid around actual head centre, continuous non-self-intersecting fringe, redundant sweeps removed')
