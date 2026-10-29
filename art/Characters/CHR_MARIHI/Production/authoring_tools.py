
import bpy,bmesh,math,json,numpy as np
from pathlib import Path
from mathutils import Vector,Quaternion
from mathutils.bvhtree import BVHTree
P=Path(__file__).parent
def linear(h):
 c=[int(h[i:i+2],16)/255 for i in (0,2,4)]
 return tuple(v/12.92 if v<=.04045 else((v+.055)/1.055)**2.4 for v in c)
def material(name,color,rough=.75,metal=0):
 m=bpy.data.materials.new('MARIHI_'+name);m.use_nodes=True;n=m.node_tree.nodes;p=next(q for q in n if q.type=='BSDF_PRINCIPLED')
 p.inputs['Base Color'].default_value=(*linear(color),1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal;p.inputs['Specular IOR Level'].default_value=.19
 p.inputs['Emission Color'].default_value=(*linear(color),1);p.inputs['Emission Strength'].default_value=.04
 m.diffuse_color=(*linear(color),1);return m
def mesh(name,vs,fs,mat,uv=None,bone='J_Bip_C_Head',weights=None):
 s=bpy.context.scene;rig=bpy.data.objects['Armature'];root=bpy.data.objects['Mesh'];me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
 ob=bpy.data.objects.new(name,me);s.collection.objects.link(ob);me.materials.append(mat);ob.parent=root
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 for f in me.polygons:f.use_smooth=True
 if uv:
  ul=me.uv_layers.new(name='UVMap')
  for f in me.polygons:
   for li in f.loop_indices:ul.data[li].uv=uv[me.loops[li].vertex_index]
 groups={}
 for v in me.vertices:
  ws=weights(v.co)if weights else{bone:1}
  for n,w in ws.items():
   if w<1e-7:continue
   if n not in groups:groups[n]=ob.vertex_groups.new(name=n)
   groups[n].add([v.index],w,'REPLACE')
 ar=ob.modifiers.new('Humanoid Skin','ARMATURE');ar.object=rig
 return ob
def cat(ps,t):
 t=max(0,min(.999999,t))*(len(ps)-1);i=int(t);u=t-i;a=ps[max(0,i-1)];b=ps[i];c=ps[i+1];d=ps[min(len(ps)-1,i+2)]
 return .5*(2*b+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u)
def skin_tree():
 vs=[];fs=[]
 # Current approved game body/face plus source skin-only head surface for smooth fitting.
 for o in [bpy.data.objects['Face'],bpy.data.objects['Body']]:
  for p in o.data.polygons:
   if o.name=='Face'and p.material_index!=0:continue
   if min(o.data.vertices[i].co.z for i in p.vertices)<1.27:continue
   ids=[]
   for i in p.vertices:ids.append(len(vs));vs.append(o.data.vertices[i].co.copy())
   fs.append(ids)
 raw=json.loads((P/'technical_source_head.json').read_text())
 for p in raw['faces']:
  ids=[]
  for i in p:ids.append(len(vs));vs.append(Vector(raw['vertices'][i]))
  fs.append(ids)
 return BVHTree.FromPolygons(vs,fs)
def scalp(p,tree=None):
 tree=tree or skin_tree();origin=Vector((0,.018,1.43));d=(Vector(p)-origin).normalized();h,n,_,_=tree.ray_cast(origin+d*.45,-d,.8)
 if h is None:h,n,_,_=tree.find_nearest(Vector(p))
 if n.dot(d)<0:n=-n
 return h,n
# A beveled leaf cross-section, not an elliptical tube.
PROFILE=[(-1,0),(-.60,.50),(0,1),(.60,.50),(1,0),(.65,-.22),(0,-.30),(-.65,-.22)]
def leaf(name,points,width,mat,rows=28,thickness=.012,weights=None,attach=False,forehead=False,twist=0):
 ps=[Vector(v)for v in points];tree=skin_tree();vs=[];fs=[];uv=[];rootids=[]
 for j in range(rows+1):
  t=j/rows;p=cat(ps,t);tg=(cat(ps,min(1,t+.005))-cat(ps,max(0,t-.005))).normalized()
  # Broad visible faces and a thin ridge give different front and side silhouettes.
  front=Vector((math.sin(t*twist),-math.cos(t*twist),.08)).normalized()
  across=tg.cross(front).normalized();normal=across.cross(tg).normalized()
  grow=min(1,t/.24);grow=grow*grow*(3-2*grow)
  w=(.002*(1-grow)+width*grow)*max(.014,(1-t)**.70)
  dep=thickness*max(.012,(1-t)**.75)
  if forehead:
   w=(.0018*(1-grow)+width*grow)*max(.018,(1-t)**.42)
  for k,(u,v)in enumerate(PROFILE):
   q=p+across*(u*w)+normal*(v*dep)
   if forehead:
    q=p+Vector((u*w,0,0));hit=tree.ray_cast(Vector((q.x,-.4,q.z)),Vector((0,1,0)),.8)
    if hit[0]is None:h,n=scalp(q,tree)
    else:h,n=hit[:2];n=-n if n.y>0 else n
    blend=min(1,t/.20);blend=blend*blend*(3-2*blend)
    offset=.0013+(.0035+v*.002)*blend
    q=h+n*offset
   elif attach and j==0:
    # A genuine two-dimensional scalp-tangent root cap.
    h,n=scalp(p,tree);uaxis=Vector((0,0,1)).cross(n).normalized();vaxis=n.cross(uaxis).normalized()
    q=h+uaxis*(u*.0022)+vaxis*(v*.0012);hh,nn=scalp(q,tree);q=hh+nn*.0015
   vs.append(tuple(q));uv.append((k/(len(PROFILE)-1),t))
   if j==0:rootids.append(len(vs)-1)
 for j in range(rows):
  for k in range(len(PROFILE)):a=j*len(PROFILE)+k;b=j*len(PROFILE)+(k+1)%len(PROFILE);fs.append((a,b,b+len(PROFILE),a+len(PROFILE)))
 fs.extend([tuple(reversed(range(len(PROFILE)))),tuple(rows*len(PROFILE)+k for k in range(len(PROFILE)))])
 ob=mesh(name,vs,fs,mat,uv,weights=weights)
 ob['Construction']='Thin beveled anime leaf, changing width/curvature; no circular tube';ob['RootIndices']=rootids
 return ob
def band(name,points,radius,mat,bone):
 # Curves are limited to narrow garment edging, never the large hair silhouette.
 cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=1;cu.bevel_depth=radius;cu.bevel_resolution=0
 sp=cu.splines.new('POLY');sp.points.add(len(points)-1)
 for p,q in zip(sp.points,points):p.co=(*q,1)
 ob=bpy.data.objects.new(name,cu);bpy.context.scene.collection.objects.link(ob);cu.materials.append(mat)
 bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob;bpy.ops.object.convert(target='MESH')
 # bake object coordinates, bind the resulting editable mesh
 for v in ob.data.vertices:v.co=ob.matrix_world@v.co
 ob.matrix_world.identity();ob.parent=bpy.data.objects['Mesh'];ob.vertex_groups.new(name=bone).add(list(range(len(ob.data.vertices))),1,'REPLACE')
 m=ob.modifiers.new('Humanoid Skin','ARMATURE');m.object=bpy.data.objects['Armature'];return ob
