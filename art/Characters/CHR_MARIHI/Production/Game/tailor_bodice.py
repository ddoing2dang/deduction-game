import bpy,math,numpy as np
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent;rig=bpy.data.objects['Armature'];game=bpy.data.collections['Game_LOD_Meshes'];archive=bpy.data.collections['Approved_Source_Archive'];saved=rig.data.pose_position;rig.data.pose_position='REST'
palette=bpy.data.materials['MAT_MARIHI_Palette'];im=next(n.image for n in palette.node_tree.nodes if n.type=='TEX_IMAGE');a=np.array(im.pixels[:]).reshape(128,128,4)
def uvcolor(rgb):
 d=np.linalg.norm(a[8::16,8::16,:3]-np.array(rgb)[None,None,:3],axis=2);y,x=np.unravel_index(np.argmin(d),d.shape);return((x*16+8)/128,(y*16+8)/128)
black=uvcolor(list(bpy.data.materials['MARIHI_Cardigan']['flat_color'])[:3]);goldmat=bpy.data.materials['MARIHI_Brass'];bs=next(n for n in goldmat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');gold=uvcolor(bs.inputs['Base Color'].default_value[:3])
def build(name,vs,fs,uv):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();ob=bpy.data.objects.new(name,me);game.objects.link(ob);me.materials.append(palette);ul=me.uv_layers.new(name='UVMap');vc=me.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER');ul=me.uv_layers.active
 for i in range(len(me.loops)):ul.data[i].uv=uv;vc.data[i].color=(1,1,1,1)
 for p in me.polygons:p.use_smooth=True
 for v in me.vertices:
  t=max(0,min(1,(v.co.z-.99)/.18))
  for name,w in [('J_Bip_C_Spine',1-t),('J_Bip_C_Chest',t)]:
   if w>1e-6:g=ob.vertex_groups.get(name)or ob.vertex_groups.new(name=name);g.add([v.index],w,'REPLACE')
 return ob
rings=[(1.022,.135,.126),(1.058,.138,.129),(1.124,.154,.150),(1.201,.171,.158),(1.275,.172,.136)]
def front(x,z):
 for (a,ax,ay),(b,bx,by)in zip(rings,rings[1:]):
  if a<=z<=b:
   t=(z-a)/(b-a);rx=ax+(bx-ax)*t;ry=ay+(by-ay)*t;return .014-ry*max(.02,1-(x/rx)**2)**.5
 return -.12
for level,nr,nc in [(0,14,7),(1,9,5),(2,6,4)]:
 pieces=[]
 for sign in [-1,1]:
  vs=[];fs=[];edge=[]
  for j in range(nr):
   t=j/(nr-1);z=1.034+.196*t;inner=.037+.065*t**1.5;outer=.132+.032*math.sin(t*math.pi*.7)
   for k in range(nc):
    u=k/(nc-1);x=sign*(inner+(outer-inner)*u);y=front(x,z)-.004-.003*math.sin(math.pi*u);vs.append((x*.96,y*.96,z*.96))
   edge.append(Vector((sign*inner*.96,(front(sign*inner,z)-.006)*.96,z*.96)))
  for j in range(nr-1):
   for k in range(nc-1):a=j*nc+k;fs.append((a,a+1,a+nc+1,a+nc))
  pieces.append(build('Outfit_MarihiTailoredSide_'+str(sign),vs,fs,black))
  vs=[];fs=[];N=5
  for j,c in enumerate(edge):
   for k in range(N):aa=math.tau*k/N;vs.append(c+Vector((.0012*math.cos(aa),.0012*math.sin(aa),0)))
  for j in range(len(edge)-1):
   for k in range(N):a=j*N+k;b=j*N+(k+1)%N;fs.append((a,b,b+N,a+N))
  pieces.append(build('Outfit_MarihiBodicePiping_'+str(sign),vs,fs,gold))
  if level==0:
   for ob in pieces[-2:]:
    copy=ob.copy();copy.data=ob.data.copy();archive.objects.link(copy);copy.parent=bpy.data.objects['Mesh'];m=copy.modifiers.new('Humanoid Skin','ARMATURE');m.object=rig
 base=bpy.data.objects['Outfit_LOD'+str(level)];base.hide_set(False);bpy.ops.object.select_all(action='DESELECT')
 for o in [base]+pieces:o.select_set(True)
 bpy.context.view_layer.objects.active=base;bpy.ops.object.join();base.hide_set(level!=0);base.hide_render=level!=0
rig.data.pose_position=saved;bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI_GAME.blend'),compress=True)
print('New fitted black waistcoat side panels and narrow gold piping, distinct from Jiro full ivory front')
