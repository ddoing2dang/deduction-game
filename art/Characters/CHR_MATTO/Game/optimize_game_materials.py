import bpy,json,math
import numpy as np
from pathlib import Path
P=Path(r'C:\UnityProjects\DeductionGame\deduction-game\art\Characters\CHR_MATTO\Game')
U=P.parents[3]/'game/UnityProject/Assets/_Game/Characters/CHR_MATTO'
# P = repo/art/Characters/CHR_MATTO/Game
U=Path(r'C:\UnityProjects\DeductionGame\deduction-game\game\UnityProject\Assets\_Game\Characters\CHR_MATTO')
T=U/'Textures';T.mkdir(parents=True,exist_ok=True)
game=bpy.data.collections['Game_LOD_Meshes']
assert not bpy.data.materials.get('MAT_MATTO_FaceWarm'),'Already converted.'
# Pack the eight unchanged face images into one 2K sheet.
cells={0:(1024,0,512,512),1:(0,1024,1024,512),2:(1024,1024,1024,512),3:(0,0,1024,1024),4:(0,1536,1024,512),5:(1024,1536,1024,256),6:(1024,1792,1024,256),7:(1024,512,1024,256)}
face=bpy.data.objects['Face_LOD0'];oldface=list(face.data.materials)
atlas=np.zeros((2048,2048,4),dtype=np.float32)
for slot,(x,y,w,h)in cells.items():
 im=oldface[slot].vrm_addon_extension.mtoon1.pbr_metallic_roughness.base_color_texture.index.source
 a=np.array(im.pixels[:],dtype=np.float32).reshape(im.size[1],im.size[0],4)
 # Two-pixel gutters prevent neighbouring cells bleeding into facial masks.
 yi=np.linspace(0,a.shape[0]-1,h-4).astype(int);xi=np.linspace(0,a.shape[1]-1,w-4).astype(int)
 patch=a[yi[:,None],xi[None,:],:]
 patch=np.pad(patch,((2,2),(2,2),(0,0)),mode='edge')
 atlas[y:y+h,x:x+w]=patch
im=bpy.data.images.new('MATTO_FaceAtlas_2K',width=2048,height=2048,alpha=True)
im.pixels.foreach_set(atlas.ravel());im.filepath_raw=str(T/'MATTO_FaceAtlas.png');im.save();im.pack()
fm={}
for name,slot in [('FaceWarm',3),('FaceInk',1),('FaceWhite',4),('FaceHighlight',2)]:
 m=oldface[slot].copy();m.name='MAT_MATTO_'+name
 e=m.vrm_addon_extension.mtoon1;e.pbr_metallic_roughness.base_color_texture.index.source=im;e.extensions.vrmc_materials_mtoon.shade_multiply_texture.index.source=im
 fm[name]=m
for o in game.objects:
 if not o.name.startswith('Face_'):continue
 uv=o.data.uv_layers.active.data
 for p in o.data.polygons:
  slot=p.material_index;x,y,w,h=cells[slot]
  for li in p.loop_indices:
   u,v=uv[li].uv;uv[li].uv=((x+2+u*(w-4))/2048,(y+2+v*(h-4))/2048)
  p.material_index={'FaceWarm':0,'FaceInk':1,'FaceWhite':2,'FaceHighlight':3}['FaceWarm'if slot in [0,3]else'FaceWhite'if slot==4 else'FaceHighlight'if slot==2 else'FaceInk']
 indices_after=[p.material_index for p in o.data.polygons]
 o.data.materials.clear()
 for m in fm.values():o.data.materials.append(m)
 for poly,idx in zip(o.data.polygons,indices_after):poly.material_index=idx
# A single non-colour palette replaces seven flat-colour draw materials without changing their linear colours.
flatnames=['MATTO_Ivory_Blouse','MATTO_Pearl_Hair','MATTO_Charcoal_Fabric','MATTO_Black_Leather','MATTO_Deep_Crimson','MATTO_Ribbon_Light','MATTO_Pleat_Shadow']
palette=np.ones((128,128,4),dtype=np.float32)
positions={}
for i,n in enumerate(flatnames):
 m=bpy.data.materials[n];ramp=next(q for q in m.node_tree.nodes if q.type=='VALTORGB')
 color=list(ramp.color_ramp.elements[-1].color)
 x=(i%4)*32;y=(i//4)*32;palette[y:y+32,x:x+32]=color;positions[n]=((x+16)/128,(y+16)/128)
pi=bpy.data.images.new('MATTO_Linear_Palette',width=128,height=128,alpha=True);pi.colorspace_settings.name='Non-Color';pi.pixels.foreach_set(palette.ravel());pi.filepath_raw=str(T/'MATTO_LinearPalette.png');pi.save();pi.pack()
pm=bpy.data.materials['MATTO_Charcoal_Fabric'].copy();pm.name='MAT_MATTO_ColorPalette'
n=pm.node_tree.nodes;l=pm.node_tree.links
ramp=next(q for q in n if q.type=='VALTORGB')
ramp.color_ramp.elements[0].color=(.61,.61,.61,1);ramp.color_ramp.elements[1].color=(1,1,1,1)
em=next(q for q in n if q.type=='EMISSION')
tx=n.new('ShaderNodeTexImage');tx.image=pi
mul=n.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1
l.new(ramp.outputs[0],mul.inputs[1]);l.new(tx.outputs[0],mul.inputs[2]);l.new(mul.outputs[0],em.inputs[0])
repl={}
for old,new in [('N00_000_00_Body_00_SKIN (Instance)','MAT_MATTO_Body'),('N00_000_Hair_00_HAIR (Instance)','MAT_MATTO_Hair'),('MATTO_Antique_Brass','MAT_MATTO_Brass'),('MATTO_Skirt_Tartan','MAT_MATTO_Tartan')]:
 m=bpy.data.materials[old].copy();m.name=new;repl[old]=m
for o in game.objects:
 if o.name.startswith('Face_'):continue
 old=list(o.data.materials);new=[];indices={}
 uv=o.data.uv_layers.active.data
 for p in o.data.polygons:
  m=old[p.material_index]
  if m.name in flatnames:
   for li in p.loop_indices:uv[li].uv=positions[m.name]
   m=pm
  elif m.name in repl:m=repl[m.name]
  if m.name not in indices:indices[m.name]=len(new);new.append(m)
  p.material_index=indices[m.name]
 indices_after=[p.material_index for p in o.data.polygons]
 o.data.materials.clear()
 for m in new:o.data.materials.append(m)
 for poly,idx in zip(o.data.polygons,indices_after):poly.material_index=idx
# Material descriptors are consumed by Unity; original data-driven colours remain explicit.
images={}
def exportImage(im,label=None):
 if im is None:return None
 if im.name in images:return images[im.name]
 filename=label or ('MATTO_'+im.name.replace(' ','_').replace('(','').replace(')','')+'.png')
 if im==bpy.data.images.get('MATTO_FaceAtlas_2K'):filename='MATTO_FaceAtlas.png'
 if im==pi:filename='MATTO_LinearPalette.png'
 old=im.filepath_raw;im.filepath_raw=str(T/filename);im.save();im.pack();im.filepath_raw=old
 images[im.name]=filename;return filename
descs=[]
for m in sorted({m for o in game.objects for m in o.data.materials},key=lambda m:m.name):
 e=m.vrm_addon_extension.mtoon1
 d={'name':m.name,'baseColor':[1,1,1,1],'shadeColor':[.61,.61,.61,1],'texture':None,'normal':None,'emission':None,'matcap':None,'emissionColor':[0,0,0,1],'matcapColor':[0,0,0,1],'cutoff':0,'metallic':0,'linear':False}
 if e.enabled:
  d['baseColor']=list(e.pbr_metallic_roughness.base_color_factor)
  d['shadeColor']=list(e.extensions.vrmc_materials_mtoon.shade_color_factor)+[1]
  d['texture']=exportImage(e.pbr_metallic_roughness.base_color_texture.index.source)
  d['cutoff']=e.alpha_cutoff if e.alpha_mode=='MASK' else 0
  d['normal']=exportImage(e.normal_texture.index.source)
  d['emission']=exportImage(e.emissive_texture.index.source)
  d['emissionColor']=list(e.emissive_factor)+[1]
  d['matcap']=exportImage(e.extensions.vrmc_materials_mtoon.matcap_texture.index.source)
  d['matcapColor']=list(e.extensions.vrmc_materials_mtoon.matcap_factor)+[1]
 elif m==pm:
  d['texture']=exportImage(pi);d['linear']=True
 elif m.name=='MAT_MATTO_Brass':
  bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');d['baseColor']=list(bs.inputs['Base Color'].default_value);d['metallic']=bs.inputs['Metallic'].default_value
 elif m.name=='MAT_MATTO_Tartan':
  d['baseColor']=[.037,.032,.043,1];d['texture']=exportImage(bpy.data.images['MATTO_Subtle_Tartan'])
 descs.append(d)
(P/'unity-materials.json').write_text(json.dumps({'materials':descs,'images':[{'name':k,'file':v,'size':list(bpy.data.images[k].size)}for k,v in images.items()]},indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MATTO_GAME.blend'))
print(json.dumps({'materials':len(descs),'textures':len(images),'files':list(images.values())}))
