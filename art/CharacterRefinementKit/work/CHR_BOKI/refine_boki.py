import bpy,bmesh,numpy as np,json,hashlib,shutil
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent;K=P.parents[1];work=P/'CHR_BOKI_REFINEMENT.blend'
backup=P/'Backup';backup.mkdir(exist_ok=True)
if not (backup/work.name).exists():shutil.copy2(work,backup/work.name)
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
# Preserve dense source and smooth only the identified front facial surface, not the bob or costume.
bm=bmesh.new();bm.from_mesh(obj.data)
faceverts=[v for v in bm.verts if .56<v.co.z<.77 and abs(v.co.x)<.098 and v.co.y<-.04]
before={v.index:v.co.copy()for v in faceverts}
for _ in range(3):bmesh.ops.smooth_vert(bm,verts=faceverts,factor=.28,use_axis_x=True,use_axis_y=True,use_axis_z=True)
moved=sum((v.co-before[v.index]).length>1e-7 for v in faceverts)
bm.to_mesh(obj.data);bm.free();obj.data.update()
im=bpy.data.images.load(str(K/'sources/CHR_BOKI_front.png'),check_existing=False);a=np.array(im.pixels[:],np.float32).reshape(im.size[1],im.size[0],4);H,W=a.shape[:2]
# Retain the reference pigment detail while repainting only the two actual iris patches.
yy,xx=np.mgrid[:H,:W];yp=H-1-yy
eyearea=(((xx-355)/25)**2+((yp-236)/19)**2<1)|(((xx-432)/25)**2+((yp-236)/19)**2<1)
red=(a[:,:,0]>a[:,:,1]*1.17)&(a[:,:,0]>a[:,:,2]*1.12)
mask=eyearea&red;lum=a[:,:,:3].max(axis=2);a[mask,:3]=lum[mask,None]*np.array((.70,.73,.79))
im.name='BOKI_ReferenceProjection_GRAY_LOCK';im.pixels.foreach_set(a.ravel());im.filepath_raw=str(P/'BOKI_FrontProjection_Gray.png');im.file_format='PNG';im.save();im.pack()
palette={'Face':(.80,.57,.48,1),'Hair':(.15,.018,.034,1),'Sleeves':(.50,.35,.24,1),'Outfit':(.025,.020,.026,1),'Body':(.80,.57,.48,1),'Shoes':(.018,.014,.019,1),'Accessory':(.36,.23,.12,1)}
for name,color in palette.items():
 m=bpy.data.materials.new('BOKI_'+name+'_ProjectionWIP');m.diffuse_color=color;m.use_nodes=True;n=m.node_tree.nodes;p=next(n for n in n if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=color;p.inputs['Roughness'].default_value=.78
 t=n.new('ShaderNodeTexImage');t.image=im;t.extension='EXTEND';m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color']);obj.data.materials.append(m)
uv=obj.data.uv_layers.new(name='FrontReferenceProjection_WIP')
names=list(palette);counts={n:0 for n in names}
for f in obj.data.polygons:
 x,y,z=f.center;px=int(np.clip(384+x*748,0,W-1));py=int(np.clip(65+(.947-z)*744,0,H-1));rgb=a[H-1-py,px,:3]
 if z>.57:part='Face'if abs(x)<.102 and z<.785 and y<-.045 and rgb[0]>rgb[1]*1.05 and rgb[1]>.15 else 'Hair'
 elif z<-.68:part='Shoes'
 elif -.65<z<-.12 and abs(x)<.18:part='Body'if rgb[0]>.35 and rgb[1]>.18 else 'Outfit'
 elif abs(x)>.155 and -.28<z<.37:part='Sleeves'if rgb[0]>.18 and rgb[1]>.10 else 'Accessory'
 elif z>.32 and rgb[0]>.35 and rgb[1]>.18:part='Body'
 else:part='Accessory'if rgb[0]>.22 and rgb[1]>.12 and rgb[1]<rgb[0]*.85 else 'Outfit'
 f.material_index=names.index(part);counts[part]+=1;f.use_smooth=True
 for li in f.loop_indices:
  v=obj.data.vertices[obj.data.loops[li].vertex_index].co;uv.data[li].uv=((384+v.x*748)/W,1-(65+(.947-v.z)*744)/H)
# Separate semantic selections, keeping their original surface positions and topology.
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='MATERIAL');bpy.ops.object.mode_set(mode='OBJECT')
for ob in bpy.context.scene.objects:
 if ob.type!='MESH':continue
 slot=next((ob.data.materials[p.material_index]for p in ob.data.polygons),None)
 if slot:ob.name=slot.name.replace('_ProjectionWIP','').replace('BOKI_','CHR_BOKI_')
# Front projection is intentionally restricted to the front; back surfaces use neutral part colours.
for ob in [o for o in bpy.context.scene.objects if o.type=='MESH']:
 base=ob.data.materials[0];flat=base.copy();flat.name=base.name+'_BackWIP'
 p=next(n for n in flat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 for l in list(p.inputs['Base Color'].links):flat.node_tree.links.remove(l)
 ob.data.materials.append(flat);idx=len(ob.data.materials)-1
 for f in ob.data.polygons:
  if f.normal.y>.25:f.material_index=idx
s=bpy.context.scene;s.render.engine='BLENDER_EEVEE';s.world.color=(.15,.15,.15);s.render.image_settings.file_format='PNG';s.render.resolution_percentage=100
camdata=bpy.data.cameras.new('Boki_Review');camdata.type='ORTHO';cam=bpy.data.objects.new('Boki_Review',camdata);s.collection.objects.link(cam);s.camera=cam
for name,loc,power,size in [('Key',(-3,-4,4),450,4),('Fill',(3,-2,2),220,3)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;ob=bpy.data.objects.new(name,data);s.collection.objects.link(ob);ob.location=loc;ob.rotation_euler=(Vector((0,0,.3))-ob.location).to_track_quat('-Z','Y').to_euler()
R=P/'Review';R.mkdir(exist_ok=True)
for name,loc,target,size in [('Front',(0,-4,0),(0,0,0),2.2),('Side',(4,0,0),(0,0,0),2.2),('Face',(0,-4,.70),(0,0,.70),.62),('ThreeQuarter',(3,-4,.05),(0,0,.05),2.2)]:
 cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();camdata.ortho_scale=size;s.render.resolution_x=700;s.render.resolution_y=900;s.render.filepath=str(R/(name+'.png'));bpy.ops.render.render(write_still=True)
s['refinement_status']='WIP actual surface smoothing, initial semantic separation and front projection UV; not final retopology/rig/game asset'
bpy.ops.wm.save_as_mainfile(filepath=str(work),compress=True)
(P/'refinement-report.json').write_text(json.dumps({'faceVerticesSmoothed':moved,'regionFaces':counts,'irisPixelsRepaintedGray':int(mask.sum()),'UV':'front-reference projection, side/back authoring incomplete','remaining':['semantic boundary cleanup','dedicated eye/eyelid topology','back and side UV painting','production retopology','rig and expressions'],'status':'WIP_NOT_COMPLETE'},indent=2))
print('Saved actual BOKI refinement WIP',counts,'face verts',moved)
