import bpy,json,math,re,numpy as np
from pathlib import Path
P=Path(__file__).parent;U=P.parents[4]/'game/UnityProject/Assets/_Game/Characters/CHR_MARIHI';T=U/'Textures';T.mkdir(parents=True,exist_ok=True)
game=bpy.data.collections['Game_LOD_Meshes'];materials={m for o in game.objects for m in o.data.materials};images={};descs=[];replace={}
def export(im):
 if im.name in images:return images[im.name]['file']
 assert max(im.size)<=2048
 name='MARIHI_'+re.sub('[^A-Za-z0-9_-]','_',im.name)+'.png';old=im.filepath_raw;im.filepath_raw=str(T/name);im.save();im.pack();im.filepath_raw=old
 images[im.name]={'name':im.name,'file':name,'size':list(im.size)};return name
def dbase(name):return dict(name=name,baseColor=[1,1,1,1],shadeColor=[.67,.63,.62,1],texture=None,normal=None,emission=None,matcap=None,emissionColor=[0,0,0,1],matcapColor=[0,0,0,1],cutoff=0,metallic=0,linear=False,vertexColor=False)
def blender(name,im=None,color=(1,1,1,1),vertex=False):
 old=bpy.data.materials.get(name)
 if old and old.users==0:bpy.data.materials.remove(old)
 m=bpy.data.materials.new(name);m.use_nodes=True;nt=m.node_tree;nt.nodes.clear();out=nt.nodes.new('ShaderNodeOutputMaterial');em=nt.nodes.new('ShaderNodeEmission');diff=nt.nodes.new('ShaderNodeBsdfDiffuse');rgb=nt.nodes.new('ShaderNodeShaderToRGB');r=nt.nodes.new('ShaderNodeValToRGB')
 r.color_ramp.elements[0].position=.1;r.color_ramp.elements[0].color=(.67,.67,.67,1);r.color_ramp.elements[1].position=.8;r.color_ramp.elements[1].color=(1,1,1,1)
 mul=nt.nodes.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1;mul.inputs[1].default_value=color
 if im:
  tx=nt.nodes.new('ShaderNodeTexImage');tx.image=im;nt.links.new(tx.outputs[0],mul.inputs[1])
 if vertex:
  vc=nt.nodes.new('ShaderNodeVertexColor');vc.layer_name='Color';tint=nt.nodes.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1
  nt.links.new(tx.outputs[0],tint.inputs[1]);nt.links.new(vc.outputs[0],tint.inputs[2]);nt.links.new(tint.outputs[0],mul.inputs[1])
 nt.links.new(diff.outputs[0],rgb.inputs[0]);nt.links.new(rgb.outputs[0],r.inputs[0]);nt.links.new(r.outputs[0],mul.inputs[2]);nt.links.new(mul.outputs[0],em.inputs[0]);nt.links.new(em.outputs[0],out.inputs['Surface']);return m
hairmats=[m for m in materials if 'ChampagneBlonde'in m.name or 'HairLit'in m.name or 'HairShadow'in m.name]
hi=bpy.data.images['MARIHI_HairStroke'];hm=blender('MAT_MARIHI_Hair',hi,vertex=True);hd=dbase(hm.name);hd.update(texture=export(hi),linear=True,vertexColor=True,shadeColor=[.70,.61,.47,1]);descs.append(hd)
for m in hairmats:replace[m]=hm
flat=[]
for m in sorted(materials,key=lambda m:m.name):
 if m in hairmats:continue
 if m.name.startswith('MAT_MARIHI_Face'):
  tx=next(n.image for n in m.node_tree.nodes if n.type=='TEX_IMAGE');d=dbase(m.name);d.update(texture=export(tx),baseColor=list(m.get('tint',[1,1,1,1])),cutoff=float(m.get('cutoff',.35)));descs.append(d);continue
 if m in list(bpy.data.objects['Body_LOD0'].data.materials):
  bi=bpy.data.images.load(str(P.parents[4]/'game/UnityProject/Assets/_Game/Characters/CHR_MATTO/Textures/MATTO__10.png'),check_existing=False);bi.name='MARIHI_BodySkin';bm=blender('MAT_MARIHI_Body',bi,color=(.96,.81,.72,1));replace[m]=bm
  d=dbase(bm.name);d.update(texture=export(bi),baseColor=[.96,.81,.72,1]);descs.append(d);continue
 tex=next((n.image for n in m.node_tree.nodes if n.type=='TEX_IMAGE'and n.image),None)
 if tex:
  nm=blender('MAT_MARIHI_'+('SleevePrint'if'Sleeve'in m.name else'SkirtPrint'),tex);replace[m]=nm;d=dbase(nm.name);d.update(texture=export(tex),linear=tex.colorspace_settings.name=='Non-Color');descs.append(d)
 else:flat.append(m)
pixels=np.ones((128,128,4),np.float32);pos={}
for i,m in enumerate(sorted(flat,key=lambda m:m.name)):
 bs=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
 c=list(m.get('flat_color',list(bs.inputs['Base Color'].default_value)if bs else [1,1,1,1]))
 x=i%8*16;y=i//8*16;pixels[y:y+16,x:x+16]=c;pos[m]=((x+8)/128,(y+8)/128)
pi=bpy.data.images.new('MARIHI_LinearPalette',width=128,height=128,alpha=True);pi.colorspace_settings.name='Non-Color';pi.pixels.foreach_set(pixels.ravel());pm=blender('MAT_MARIHI_Palette',pi);d=dbase(pm.name);d.update(texture=export(pi),linear=True);descs.append(d)
for ob in game.objects:
 old=list(ob.data.materials);new=[];ix=[]
 vc=ob.data.color_attributes.get('Color')or ob.data.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER')
 # Reacquire UV after allocating colour attributes (Blender 5 can relocate the buffer).
 uv=ob.data.uv_layers.active.data
 for poly in ob.data.polygons:
  m=old[poly.material_index];tint=list(m.get('flat_color',[1,1,1,1]))if m in hairmats else[1,1,1,1]
  for li in poly.loop_indices:vc.data[li].color=tint
  if m in pos:
   for li in poly.loop_indices:uv[li].uv=pos[m]
   nm=pm
  else:nm=replace.get(m,m)
  if nm not in new:new.append(nm)
  ix.append(new.index(nm))
 ob.data.materials.clear()
 for m in new:ob.data.materials.append(m)
 for poly,i in zip(ob.data.polygons,ix):poly.material_index=i
 ob.data.update()
(P/'unity-materials.json').write_text(json.dumps({'materials':descs,'images':list(images.values())},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI_GAME.blend'),compress=True)
print('Actual materials',len({m for o in game.objects for m in o.data.materials}),'textures',len(images))
