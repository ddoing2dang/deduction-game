import bpy,math,json,numpy as np
from pathlib import Path
from mathutils import Vector,Quaternion
P=Path(__file__).parent
exec(compile((P/'authoring_tools.py').read_text(),str(P/'authoring_tools.py'),'exec'),globals())
s=bpy.context.scene;rig=bpy.data.objects['Armature'];rig.data.pose_position='REST';tree=skin_tree()
for o in list(s.objects):
 if o.name.startswith(('Hair_Fringe_','Accessory_WaistBow','Outfit_Placket','Outfit_NeckRibbon','Outfit_Ribbon_Edge')):bpy.data.objects.remove(o,do_unlink=True)
colors=[bpy.data.materials['MARIHI_ChampagneBlonde'],bpy.data.materials['MARIHI_HairLit'],bpy.data.materials['MARIHI_HairShadow']]
spec=[(-.035,-.114,1.457,.025),(-.022,-.080,1.442,.029),(-.008,-.046,1.451,.031),(.006,-.013,1.438,.030),(.018,.022,1.449,.030),(.030,.057,1.461,.027),(.040,.088,1.459,.024),(.048,.116,1.477,.019)]
for i,(xr,xe,ze,w)in enumerate(spec):
 ps=[Vector((xr,.006,1.614)),Vector((xr,-.061,1.578)),Vector((xe*.55,-.105,1.524)),Vector((xe*.91,-.120,1.479)),Vector((xe,-.124,ze))]
 vs=[];fs=[];uv=[];rows=25;nc=7
 for j in range(rows+1):
  t=j/rows;c=cat(ps,t);ww=(.002*(1-t)+w*math.sin(math.pi*t)**.65)
  for k in range(nc):
   u=k/(nc-1)*2-1;q=c+Vector((ww*u,-.002*(1-u*u)*math.sin(math.pi*t),0))
   if j==0:
    h,n=scalp(q,tree);q=h+n*.0023
   elif t<.78:
    h=tree.ray_cast(Vector((q.x,-.4,q.z)),Vector((0,1,0)),.7)[0]
    if h is not None:q.y=min(q.y,h.y-.003)
   vs.append(q);uv.append((k/(nc-1),t))
 for j in range(rows):
  for k in range(nc-1):a=j*nc+k;fs.append((a,a+1,a+nc+1,a+nc))
 ob=mesh('Hair_Fringe_%02d'%i,vs,fs,colors[0],uv);ob['RootIndices']=list(range(nc))
# Make lower tails visibly separate and undulating, not six coincident banana outlines.
for o in s.objects:
 if not o.name.startswith('Hair_WaveTail_'):continue
 st=int(o.name[-2:]);sign=1 if '_L_'in o.name else -1;nc=8;rows=len(o.data.vertices)//nc
 for j in range(rows):
  t=j/(rows-1)
  if t<.15:continue
  ids=range(j*nc,(j+1)*nc);c=sum((o.data.vertices[i].co for i in ids),Vector())/nc
  dx=sign*(.019*math.sin(t*math.tau*1.7+st*.6)+.009*(st-2.5))*math.sin(math.pi*t/2)**2
  dy=.016*math.sin(t*math.tau*1.4+st*.8)*t
  for i in ids:
   d=o.data.vertices[i].co-c;d.x*=.82;d.y*=1.12
   o.data.vertices[i].co=c+d+Vector((dx,dy,-.008*(st%3)*t*t))
# Bow loops are moved forward on their mount so the black silhouette is readable.
for o in s.objects:
 if o.name.startswith('ACC_MARIHI_'):
  for v in o.data.vertices:v.co.y-=.030
# Surface-following tie, placket and shirt collar thickness.
shirt=bpy.data.objects['Outfit_Blouse'];st=BVHTree.FromPolygons([v.co.copy()for v in shirt.data.vertices],[tuple(p.vertices)for p in shirt.data.polygons])
ivory=shirt.data.materials[0];black=bpy.data.materials['MARIHI_Forest_Ribbon'];gold=bpy.data.materials['MARIHI_Brass'];S=.96
for sign in [-1,1]:
 vs=[];fs=[];uv=[];nr=15;nc=5
 for j in range(nr):
  t=j/(nr-1);z=(1.276-.143*t)*S;cx=sign*(.009+.019*t);w=.017*(1-.2*t)
  for k in range(nc):
   x=(cx+(k/(nc-1)-.5)*w)*S;h=st.ray_cast(Vector((x,-.5,z)),Vector((0,1,0)),.9)[0]
   vs.append((x,h.y-.005 if h else -.15*S,z));uv.append((k/(nc-1),t))
 for j in range(nr-1):
  for k in range(nc-1):a=j*nc+k;fs.append((a,a+1,a+nc+1,a+nc))
 mesh('Outfit_NeckTie_'+str(sign),vs,fs,black,uv,bone='J_Bip_C_UpperChest')
 band('Accessory_TieGold_'+str(sign),[tuple(v+Vector((0,-.0007,0)))for v in map(Vector,vs[::nc])],.0007,gold,'J_Bip_C_UpperChest')
for ob in s.objects:
 if ob.name.startswith('Outfit_Collar_'):
  for v in ob.data.vertices:
   h=st.ray_cast(Vector((v.co.x,-.5,v.co.z)),Vector((0,1,0)),.9)[0]
   if h:v.co.y=h.y-.004
 if ob.name.startswith('Outfit_CardiganSleeve_'):
  for j in range(len(ob.data.vertices)//28):
   ids=range(j*28,(j+1)*28);c=sum((ob.data.vertices[i].co for i in ids),Vector())/28
   for i in ids:ob.data.vertices[i].co=c+(ob.data.vertices[i].co-c)*.90
# Private garment texture: warm black tartan and narrow double gold hem.
im=next(n.image for n in bpy.data.objects['Outfit_PleatedSkirt'].data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE')
N=1024;yy,xx=np.mgrid[:N,:N];u=xx/(N-1);v=yy/(N-1);a=np.ones((N,N,4),np.float32);a[:,:,:3]=linear('30272E')
a[(np.minimum((u*16)%1,1-(u*16)%1)<.012),:3]=linear('665346')
for lo,hi,col in [(.84,.846,'A98752'),(.90,.914,'C9A467'),(.95,.96,'B48F51')]:a[(v>lo)&(v<hi),:3]=linear(col)
im.pixels.foreach_set(a.ravel());im.update();im.filepath_raw=str(P/'MARIHI_Skirt_Check.png');im.save();im.pack()
# Belt buckle and a real thin heart charm, not a front bow copied from Jiro.
def strap(name,ps,rad,bone):return band(name,[tuple(Vector(p)*S)for p in ps],rad*S,gold,bone)
strap('Accessory_BeltBuckle',[(-.016,-.129,1.022),(.016,-.129,1.022),(.016,-.129,1.041),(-.016,-.129,1.041),(-.016,-.129,1.022)],.0018,'J_Bip_C_Hips')
heart=[]
for t in np.linspace(0,math.tau,49):
 x=16*math.sin(t)**3;z=13*math.cos(t)-5*math.cos(2*t)-2*math.cos(3*t)-math.cos(4*t)
 heart.append((.140+x*.0007,-.123,.932+z*.0007))
strap('Accessory_HeartPendant',heart,.0012,'J_Bip_C_Hips')
strap('Accessory_HeartChain',[(.142,-.116,.991),(.142,-.121,.951)],.0008,'J_Bip_C_Hips')
# Gold ribbon/buckle accents on dropped sleeves, aligned with each arm in rest pose.
for side,sign in [('L',1),('R',-1)]:
 for z in [1.244,1.270]:
  strap('Accessory_SleeveGold_'+side+str(z),[(sign*.300,-.050,z),(sign*.336,-.053,z-.005)],.0010,'J_Bip_'+side+'_UpperArm')
# Use controlled cloth shading; studio lamps must not erase tailoring to pure white.
for m in {m for o in s.objects if o.type=='MESH'and o.name.startswith(('Outfit_','Shoes_','Accessory_'))for m in o.data.materials}:
 if not m.use_nodes:continue
 nt=m.node_tree;bs=next((n for n in nt.nodes if n.type=='BSDF_PRINCIPLED'),None)
 if bs is None:continue
 col=tuple(bs.inputs['Base Color'].default_value);metal=bs.inputs['Metallic'].default_value
 if metal>.1:continue
 tx=next((n for n in nt.nodes if n.type=='TEX_IMAGE'and n.image),None);tex=tx.image if tx else None
 nt.nodes.clear();out=nt.nodes.new('ShaderNodeOutputMaterial');em=nt.nodes.new('ShaderNodeEmission');diff=nt.nodes.new('ShaderNodeBsdfDiffuse');rgb=nt.nodes.new('ShaderNodeShaderToRGB');ramp=nt.nodes.new('ShaderNodeValToRGB')
 ramp.color_ramp.elements[0].position=.10;ramp.color_ramp.elements[0].color=(.60,.60,.60,1)
 ramp.color_ramp.elements[1].position=.8;ramp.color_ramp.elements[1].color=(.96,.96,.96,1)
 mix=nt.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[1].default_value=col
 if tex:tx=nt.nodes.new('ShaderNodeTexImage');tx.image=tex;nt.links.new(tx.outputs[0],mix.inputs[1])
 nt.links.new(diff.outputs[0],rgb.inputs[0]);nt.links.new(rgb.outputs[0],ramp.inputs[0]);nt.links.new(ramp.outputs[0],mix.inputs[2]);nt.links.new(mix.outputs[0],em.inputs[0]);nt.links.new(em.outputs[0],out.inputs['Surface']);m['flat_color']=list(col)
rig.data.pose_position='POSE'
for side,sign in [('L',1),('R',-1)]:
 for part,ang in [('UpperArm',1.20),('LowerArm',.07)]:
  pb=rig.pose.bones['J_Bip_'+side+'_'+part];q=pb.bone.matrix_local.to_quaternion();pb.rotation_quaternion=q.inverted()@Quaternion((0,1,0),ang*sign)@q
bpy.context.view_layer.update();c=s.camera;c.location=(0,-4,.83);c.rotation_euler=(Vector((0,0,.83))-c.location).to_track_quat('-Z','Y').to_euler();c.data.ortho_scale=1.88
s.render.filepath=str(P/'Review/Full_Refined.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI.blend'),compress=True)
