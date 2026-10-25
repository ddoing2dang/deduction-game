import bpy,json,re
import numpy as np
from pathlib import Path
P=Path(__file__).parent;U=P.parents[3]/'game/UnityProject/Assets/_Game/Characters/CHR_JIRO';T=U/'Textures';T.mkdir(parents=True,exist_ok=True)
game=bpy.data.collections['Game_LOD_Meshes'];materials={m for o in game.objects for m in o.data.materials};images={};repl={};flat=[];desc=[]
def export(im):
 if im is None or 'NoneNormal'in im.name or 'NoneBlack'in im.name:return None
 if im.name in images:return images[im.name]['file']
 fn='JIRO_'+re.sub('[^A-Za-z0-9_.-]','_',im.name)+'.png';old=im.filepath_raw;im.filepath_raw=str(T/fn);im.save();im.pack();im.filepath_raw=old
 images[im.name]={'name':im.name,'file':fn,'size':list(im.size)};assert max(im.size)<=2048
 return fn
def dbase(name):return {'name':name,'baseColor':[1,1,1,1],'shadeColor':[.61,.61,.61,1],'texture':None,'normal':None,'emission':None,'matcap':None,'emissionColor':[0,0,0,1],'matcapColor':[0,0,0,1],'cutoff':0,'metallic':0,'linear':False,'vertexColor':False}
hairmats=[m for m in materials if m.name.startswith('JIRO_Toon_Hair_')];hair=hairmats[0].copy();hair.name='MAT_JIRO_Hair';hd=dbase(hair.name);hd['texture']=export(bpy.data.images['JIRO_HairPaint']);hd['linear']=True;hd['shadeColor']=[.55,.59,.49,1];hd['vertexColor']=True;desc.append(hd)
for m in materials:
 if m in hairmats:repl[m]=hair;continue
 e=m.vrm_addon_extension.mtoon1
 if m.name.startswith('MAT_JIRO_'):
  d=dbase(m.name);d['baseColor']=list(e.pbr_metallic_roughness.base_color_factor);d['shadeColor']=list(e.extensions.vrmc_materials_mtoon.shade_color_factor)+[1];d['texture']=export(e.pbr_metallic_roughness.base_color_texture.index.source);d['normal']=export(e.normal_texture.index.source);d['cutoff']=e.alpha_cutoff if e.alpha_mode=='MASK'else .01;desc.append(d);continue
 tex=next((n.image for n in m.node_tree.nodes if n.type=='TEX_IMAGE'and n.image),None)if m.use_nodes else None
 if tex:
  new=m.copy();new.name='MAT_JIRO_Check';repl[m]=new;d=dbase(new.name);d['texture']=export(tex);d['linear']=tex.colorspace_settings.name=='Non-Color';desc.append(d)
 else:flat.append(m)
# One tiny linear palette retains each authored colour without one draw material per ornament.
a=np.ones((128,128,4),np.float32);pos={}
for i,m in enumerate(sorted(flat,key=lambda m:m.name)):
 bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');color=list(bs.inputs['Base Color'].default_value)
 if m.name=='JIRO_Blouse_SoftShaded':color[:3]=[min(1,c*1.70)for c in color[:3]]
 x=i%8*16;y=i//8*16;a[y:y+16,x:x+16]=color;pos[m]=((x+8)/128,(y+8)/128)
pi=bpy.data.images.new('JIRO_LinearPalette',width=128,height=128,alpha=True);pi.colorspace_settings.name='Non-Color';pi.pixels.foreach_set(a.ravel());pm=flat[0].copy();pm.name='MAT_JIRO_ColorPalette';bs=next(n for n in pm.node_tree.nodes if n.type=='BSDF_PRINCIPLED');tx=pm.node_tree.nodes.new('ShaderNodeTexImage');tx.image=pi;pm.node_tree.links.new(tx.outputs[0],bs.inputs['Base Color']);d=dbase(pm.name);d['linear']=True;d['texture']=export(pi);desc.append(d)
for o in game.objects:
 old=list(o.data.materials);new=[];indices={};colors=o.data.color_attributes.get('Color')or o.data.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER');uv=o.data.uv_layers.active.data
 for p in o.data.polygons:
  m=old[p.material_index];tint=[1,1,1,1]
  if m in hairmats:
   tint=list(m.vrm_addon_extension.mtoon1.pbr_metallic_roughness.base_color_factor)
  if m in pos:
   for li in p.loop_indices:uv[li].uv=pos[m]
   n=pm
  else:n=repl.get(m,m)
  for li in p.loop_indices:colors.data[li].color=tint
  if n.name not in indices:indices[n.name]=len(new);new.append(n)
  p.material_index=indices[n.name]
 ix=[p.material_index for p in o.data.polygons];o.data.materials.clear()
 for n in new:o.data.materials.append(n)
 for p,i in zip(o.data.polygons,ix):p.material_index=i
(P/'unity-materials.json').write_text(json.dumps({'materials':desc,'images':list(images.values())},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_JIRO_GAME.blend'));print({'materials':len(desc),'textures':len(images),'files':list(images)})
