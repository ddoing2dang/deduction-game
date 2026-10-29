import bpy,math,numpy as np
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent
exec(compile((P/'authoring_tools.py').read_text(),str(P/'authoring_tools.py'),'exec'),globals())
s=bpy.context.scene;tree=skin_tree()
# Force a new GPU image datablock after painting. Packed-image GPU caches can retain source pixels.
old=next(n.image for n in bpy.data.objects['Face'].data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE')
new=bpy.data.images.load(str(P/'MARIHI_FaceAtlas.png'),check_existing=False);new.name='MARIHI_AmberFace_Final';new.pack()
for m in bpy.data.objects['Face'].data.materials:
 for n in m.node_tree.nodes:
  if n.type=='TEX_IMAGE':n.image=new
for o in list(s.objects):
 if o.name.startswith('Hair_Fringe_'):bpy.data.objects.remove(o,do_unlink=True)
colors=[bpy.data.materials.get('MARIHI_ChampagneBlonde'),bpy.data.materials.get('MARIHI_HairLit')]
spec=[(-.036,-.114,1.456,.024),(-.022,-.080,1.443,.028),(-.008,-.046,1.452,.031),(.006,-.013,1.437,.030),(.018,.022,1.448,.030),(.030,.057,1.460,.027),(.040,.088,1.458,.024),(.048,.116,1.477,.019)]
for i,(xr,xe,ze,w)in enumerate(spec):
 vs=[];fs=[];uv=[];rows=25;cols=7
 for j in range(rows+1):
  t=j/rows;z=1.614+(ze-1.614)*t;x=xr+(xe-xr)*(t*t*(3-2*t))
  width=(.005*(1-t)+w*math.sin(math.pi*t)**.65)*max(.01,1-.45*t**4)
  for k in range(cols):
   u=k/(cols-1)*2-1;xx=x+u*width
   h=tree.ray_cast(Vector((xx,-.4,z)),Vector((0,1,0)),.75)[0]
   # Eye sockets are not scalp: below forehead use a continuing swept outer contour.
   y=h.y-.0038 if h is not None else -.11
   if z<1.515:y=min(y,-.105-.012*max(0,min(1,(1.515-z)/.07)))
   y-=.003*(1-u*u)*math.sin(math.pi*t)
   vs.append((xx,y,z));uv.append((k/(cols-1),t))
 for j in range(rows):
  for k in range(cols-1):a=j*cols+k;fs.append((a,a+1,a+cols+1,a+cols))
 ob=mesh('Hair_Fringe_%02d'%i,vs,fs,colors[1 if i in [1,5]else 0],uv)
 ob['RootIndices']=list(range(cols));ob['Construction']='Scalp projected convex ribbon with socket-safe continuous forehead contour'
# Restrained toon hair prevents bright studio lights bleaching blonde to white.
for m in colors+[bpy.data.materials.get('MARIHI_HairShadow')]:
 bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');color=tuple(bs.inputs['Base Color'].default_value)
 nt=m.node_tree;nt.nodes.clear();out=nt.nodes.new('ShaderNodeOutputMaterial');em=nt.nodes.new('ShaderNodeEmission')
 diff=nt.nodes.new('ShaderNodeBsdfDiffuse');rgb=nt.nodes.new('ShaderNodeShaderToRGB');r=nt.nodes.new('ShaderNodeValToRGB')
 r.color_ramp.elements[0].position=.1;r.color_ramp.elements[0].color=tuple(v*.70 for v in color[:3])+(1,)
 r.color_ramp.elements[1].position=.8;r.color_ramp.elements[1].color=tuple(min(1,v*1.14)for v in color[:3])+(1,)
 nt.links.new(diff.outputs[0],rgb.inputs[0]);nt.links.new(rgb.outputs[0],r.inputs[0]);nt.links.new(r.outputs[0],em.inputs[0]);nt.links.new(em.outputs[0],out.inputs['Surface']);m['flat_color']=list(color)
bpy.context.view_layer.update();s.render.filepath=str(P/'Review/Head_Refined.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI.blend'),compress=True)
