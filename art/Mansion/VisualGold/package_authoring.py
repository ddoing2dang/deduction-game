import bpy,json,re
from pathlib import Path
P=Path(__file__).parent;D=P/'AuthoringTextures';D.mkdir(exist_ok=True)
for o in list(bpy.data.objects):
 if o.name.startswith('CC0_Source_'):bpy.data.objects.remove(o,do_unlink=True)
bpy.data.orphans_purge(do_recursive=True)
used={m for o in bpy.context.scene.objects if o.type=='MESH'and o.get('StructureRole')for m in o.data.materials if m}
images=sorted({n.image for m in used if m.use_nodes for n in m.node_tree.nodes if n.type=='TEX_IMAGE'and n.image},key=lambda im:im.name)
report=[]
for i,im in enumerate(images):
 assert max(im.size)<=2048,(im.name,list(im.size))
 original=im.filepath;filename='%03d_%s.png'%(i,re.sub('[^A-Za-z0-9_-]','_',Path(im.name).stem)[:95])
 if im.packed_file:im.unpack(method='REMOVE')
 im.filepath_raw=str(D/filename);im.file_format='PNG';im.save();im.filepath='//AuthoringTextures/'+filename
 report.append({'image':im.name,'original':original,'file':filename,'size':list(im.size),'colorspace':im.colorspace_settings.name})
(P/'authoring-textures.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'),compress=True)
print('Saved compressed game authoring file with',len(report),'relative CC0 texture files')
