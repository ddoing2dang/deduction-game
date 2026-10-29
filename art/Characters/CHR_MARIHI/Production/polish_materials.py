import bpy,math,numpy as np,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent
exec(compile((P/'authoring_tools.py').read_text(),str(P/'authoring_tools.py'),'exec'),globals())
s=bpy.context.scene
# Authored paint on the real meshes: fine longitudinal separation and broad anime highlights.
N=512;yy,xx=np.mgrid[:1024,:N];u=xx/(N-1);v=yy/1023
tone=.82+.18*np.sin(math.pi*u)**.7+.11*np.exp(-((v-.34)/.11)**2)
tone-=.045*(.5+.5*np.cos(u*math.tau*7))**16
a=np.ones((1024,N,4),np.float32);a[:,:,:3]=tone[:,:,None]
im=bpy.data.images.new('MARIHI_HairStroke',width=N,height=1024,alpha=True);im.colorspace_settings.name='Non-Color';im.pixels.foreach_set(a.ravel());im.filepath_raw=str(P/'MARIHI_HairStroke.png');im.save();im.pack()
for m in [bpy.data.materials['MARIHI_'+n]for n in ['ChampagneBlonde','HairLit','HairShadow']]:
 nt=m.node_tree;em=next(n for n in nt.nodes if n.type=='EMISSION');old=em.inputs[0].links[0].from_socket
 tx=nt.nodes.new('ShaderNodeTexImage');tx.image=im;mix=nt.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
 nt.links.new(old,mix.inputs[1]);nt.links.new(tx.outputs[0],mix.inputs[2]);nt.links.new(mix.outputs[0],em.inputs[0]);m['export_texture']='MARIHI_HairStroke.png'
gold=bpy.data.materials['MARIHI_Brass'];ivory=bpy.data.materials['MARIHI_Blouse'];dark=bpy.data.materials['MARIHI_Cardigan']
# Small open silk bows on sleeves, shaped in rest coordinates then skinned.
rig=bpy.data.objects['Armature'];saved=rig.data.pose_position;rig.data.pose_position='REST'
for side,sign in [('L',1),('R',-1)]:
 c=Vector((sign*.303,-.054,1.203))
 for wing in [-1,1]:
  vs=[];fs=[];nr=6;nc=5
  for j in range(nr):
   t=j/(nr-1)
   for k in range(nc):
    q=k/(nc-1)*2-1;vs.append(c+Vector((wing*.022*t,-.004*math.sin(math.pi*t)*(1-q*q),q*.012*(.2+.8*math.sin(math.pi*t*.7)))))
  for j in range(nr-1):
   for k in range(nc-1):a=j*nc+k;fs.append((a,a+1,a+nc+1,a+nc))
  mesh('Accessory_SleeveRibbon_'+side+str(wing),vs,fs,gold,bone='J_Bip_'+side+'_UpperArm')
# Subtle gold-on-charcoal sleeve print uses shared UV image, no little floating meshes.
N=512;yy,xx=np.mgrid[:N,:N];u=xx/(N-1);v=yy/(N-1);a=np.ones((N,N,4),np.float32);a[:,:,:3]=linear('30272E')
du=(u*8)%1-.5;dv=(v*6)%1-.5;motif=(abs(du)+abs(dv)<.105)&(abs(du)+abs(dv)>.070)
a[motif,:3]=linear('A78859');a[(abs(du)<.015)&(abs(dv)<.14),:3]=linear('7E6649')
pattern=bpy.data.images.new('MARIHI_SleeveJacquard',width=N,height=N,alpha=True);pattern.colorspace_settings.name='Non-Color';pattern.pixels.foreach_set(a.ravel());pattern.filepath_raw=str(P/'MARIHI_SleeveJacquard.png');pattern.save();pattern.pack()
printed=dark.copy();printed.name='MARIHI_SleeveJacquard';nt=printed.node_tree;mix=next(n for n in nt.nodes if n.type=='MIX_RGB');tx=nt.nodes.new('ShaderNodeTexImage');tx.image=pattern;nt.links.new(tx.outputs[0],mix.inputs[1])
for ob in s.objects:
 if ob.name.startswith('Outfit_CardiganSleeve_'):ob.data.materials[0]=printed
 if ob.name.startswith('Outfit_PleatedSkirt'):
  for f in ob.data.polygons:f.use_smooth=False
# Give the toe/sole a readable material separation and leather sheen.
for n,hexcolor,rough in [('Loafers','302A2E',.36),('Skirt','262229',.70)]:
 m=bpy.data.materials['MARIHI_'+n];nt=m.node_tree;nt.nodes.clear();o=nt.nodes.new('ShaderNodeOutputMaterial');bs=nt.nodes.new('ShaderNodeBsdfPrincipled');bs.inputs['Base Color'].default_value=(*linear(hexcolor),1);bs.inputs['Roughness'].default_value=rough;bs.inputs['Specular IOR Level'].default_value=.28;nt.links.new(bs.outputs[0],o.inputs['Surface']);m['flat_color']=list(bs.inputs['Base Color'].default_value)
rig.data.pose_position=saved
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI.blend'),compress=True)
print('Hair stroke paint, sleeve jacquard, gold silk accents and shoe material separation authored on actual model')
