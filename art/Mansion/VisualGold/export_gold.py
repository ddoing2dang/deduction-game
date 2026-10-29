import bpy,json,re,hashlib,math
import numpy as np
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent;U=P.parents[2]/'game/UnityProject/Assets/_Game/Art/MansionVisualGold';T=U/'Textures';T.mkdir(parents=True,exist_ok=True);scene=bpy.context.scene
objects=[o for o in scene.objects if o.type=='MESH'and o.get('StructureRole')and len(o.data.vertices)]
images={};materials=[];material_names={};image_counter=0
def safe(s):return re.sub('[^A-Za-z0-9_-]','_',s)[:90]
def png(im,linear=False,normal=False,alpha=None):
 if im is None:return None
 key=(bpy.path.abspath(im.filepath),im.name if not im.filepath else '',linear,normal,alpha.name if alpha else '')
 if key in images:return images[key]['file']
 name=safe(Path(im.filepath.replace('\\','/')).stem if im.filepath else im.name);fn='Gold_'+name+('_Linear'if linear else '')+('_N'if normal else '')+('_Alpha'if alpha else '')+'.png'
 if any(d['file']==fn for d in images.values()):fn='Gold_'+str(len(images))+'_'+fn
 w,h=im.size;assert max(w,h)<=2048,(im.name,im.size)
 if alpha:
  a=np.array(im.pixels[:],np.float32).reshape(h,w,4);aa=alpha.copy();aa.scale(w,h);a[:,:,3]=np.array(aa.pixels[:],np.float32).reshape(h,w,4)[:,:,0];bpy.data.images.remove(aa)
  tmp=bpy.data.images.new('ExportAlpha',width=w,height=h,alpha=True);tmp.colorspace_settings.name='Non-Color'if linear else 'sRGB';tmp.pixels.foreach_set(a.ravel());tmp.filepath_raw=str(T/fn);tmp.file_format='PNG';tmp.save();bpy.data.images.remove(tmp)
 else:
  old=im.filepath_raw;fmt=im.file_format;im.filepath_raw=str(T/fn);im.file_format='PNG';im.save();im.filepath_raw=old;im.file_format=fmt
 images[key]={'file':fn,'linear':linear,'normal':normal,'size':[w,h]};return fn
def find_image(socket,seen=None):
 if not socket or not socket.is_linked:return None
 node=socket.links[0].from_node
 if node.type=='TEX_IMAGE':return node.image
 seen=set()if seen is None else seen
 if node in seen:return None
 seen.add(node)
 for i in node.inputs:
  r=find_image(i,seen)
  if r:return r
 return None
used=sorted({m for o in objects for m in o.data.materials if m},key=lambda m:m.name)
for i,m in enumerate(used):
 name='GoldMat_'+safe(m.name);material_names[m]=name;bs=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)if m.use_nodes else None
 d={'name':name,'color':list(bs.inputs['Base Color'].default_value)if bs else list(m.diffuse_color),'albedo':None,'normal':None,'metalGloss':None,'emission':[0,0,0,1],'cutoff':.01,'transparent':False,'normalStrength':.45,'metallic':0,'smoothness':.38}
 if bs:
  al=find_image(bs.inputs['Base Color']);alpha=find_image(bs.inputs['Alpha']);norm=find_image(bs.inputs['Normal']);d['normal']=png(norm,True,True)
  if al:d['color']=list(m.get('GoldTint',[1,1,1]))+[1];d['albedo']=png(al,al.colorspace_settings.name=='Non-Color',False,alpha)
  d['metallic']=float(bs.inputs['Metallic'].default_value);d['smoothness']=1-float(bs.inputs['Roughness'].default_value);d['emission']=list(bs.inputs['Emission Color'].default_value);d['emission']=[v*float(bs.inputs['Emission Strength'].default_value)for v in d['emission'][:3]]+[1]
  d['transparent']=float(bs.inputs['Transmission Weight'].default_value)>.25 or float(bs.inputs['Alpha'].default_value)<.9
  if d['transparent']:d['color'][3]=min(.25,float(bs.inputs['Alpha'].default_value))
  metal=find_image(bs.inputs['Metallic']);rough=find_image(bs.inputs['Roughness'])
  if metal or rough:
   w=h=1024;a=np.ones((h,w,4),np.float32);a[:,:,:3]=d['metallic'];a[:,:,3]=d['smoothness']
   for im,channel in [(metal,0),(rough,3)]:
    if im:
     tmp=im.copy();tmp.scale(w,h);p=np.array(tmp.pixels[:],np.float32).reshape(h,w,4)[:,:,0];bpy.data.images.remove(tmp)
     if channel==3:a[:,:,3]=1-p
     else:a[:,:,:3]=p[:,:,None]
   im=bpy.data.images.new('MG_'+name,width=w,height=h,alpha=True);im.colorspace_settings.name='Non-Color';im.pixels.foreach_set(a.ravel());d['metalGloss']=png(im,True);bpy.data.images.remove(im)
  if alpha:d['cutoff']=.35
 materials.append(d)
# Protect the exact matrices of all authored graph references.
baseline=json.loads((P/'baseline-refs.json').read_text());changed=[]
for name,row in baseline.items():
 if name not in bpy.data.objects or max(abs(bpy.data.objects[name].matrix_world[i][j]-row['matrix'][i][j])for i in range(4)for j in range(4))>1e-6:changed.append(name)
assert not changed,changed
parts=[];meshkeys={};tri=0
for o in objects:
 if o.data not in meshkeys:meshkeys[o.data]='GoldMesh_%04d'%len(meshkeys)
 ps=[o.matrix_world@Vector(v)for v in o.bound_box];lo=[min(v[i]for v in ps)for i in range(3)];hi=[max(v[i]for v in ps)for i in range(3)];o.data.calc_loop_triangles();tri+=len(o.data.loop_triangles)
 parts.append({'name':o.name,'role':o['StructureRole'],'room':int(o.get('StructureRoom',0)),'door':o.get('StructureDoor',''),'side':o.get('StructureSide',''),'parent':o.get('GoldParent',''),'assembly':o.get('ArtAssembly',''),'meshKey':meshkeys[o.data],'materials':[material_names[m]for m in o.data.materials if m],'asset':o.get('GoldAsset',''),'collision':bool(o.get('StructureCollision',False)),'hidden':'_COLLISION_'in o.name,'min':{'x':lo[0],'y':lo[2],'z':lo[1]},'max':{'x':hi[0],'y':hi[2],'z':hi[1]}})
layout=json.loads((P.parent/'Pass02/environment_manifest.json').read_text())['layout']
doc={'version':1,'modelFile':'Mansion_VisualGold.fbx','parts':parts,'materials':materials,'images':list(images.values()),'retired':json.loads((P/'retired-placeholders.json').read_text()),'layout':layout,'sourceRelease':'mansion-cc0-fullpack-v1','sourcePackSHA256':'71720193c0030977b446e520bb5c3a51e1ca644219b5117645f8b7ba971d11ba'}
doc['lights']=[]
for light in scene.objects:
 if light.type=='LIGHT'and'GoldRoom'in light:
  v=light.matrix_world.translation;c=light.data.color;doc['lights'].append({'name':light.name,'room':int(light['GoldRoom']),'position':{'x':v.x,'y':v.z-.3,'z':v.y},'color':{'r':c.r,'g':c.g,'b':c.b,'a':1},'intensity':2.0 if light['GoldRoom']<7 else 1.6,'range':9})
(P/'gold-manifest.json').write_text(json.dumps(doc,indent=1));(U/'gold-manifest.json').write_text(json.dumps(doc,indent=1))
# Selection-only export includes render geometry and retained hidden stair envelopes; references/lights/templates are excluded.
states={c:c.hide_viewport for c in bpy.data.collections}
try:
 for c in states:c.hide_viewport=False
 bpy.context.view_layer.update()
 original_data={o:o.data for o in objects}
 for o,data in original_data.items():o.data=data.copy()
 with bpy.context.temp_override(selected_objects=objects):bpy.ops.export_scene.fbx(filepath=str(U/'Mansion_VisualGold.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False,use_mesh_modifiers=True)
finally:
 if 'original_data'in locals():
  for o,data in original_data.items():
   tmp=o.data;o.data=data;bpy.data.meshes.remove(tmp)
 for c,state in states.items():c.hide_viewport=state
# Authoring textures are stored as relative CC0 PNG files by package_authoring.py.
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'),compress=True)
report={'parts':len(parts),'uniqueMeshes':len(meshkeys),'placedTris':tri,'materials':len(materials),'textures':len(images),'unchangedReferences':len(baseline),'sourcePass02Unchanged':hashlib.sha256((P.parent/'Pass02/Mansion_Environment_Pass02.blend').read_bytes()).hexdigest()==(P/'source-pass02.sha256').read_text()};(P/'export-report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
