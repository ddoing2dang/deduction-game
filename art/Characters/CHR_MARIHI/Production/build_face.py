import bpy, math, json, hashlib
import numpy as np
from pathlib import Path
from mathutils import Matrix, Vector
P=Path(__file__).parent
assert not bpy.data.filepath or Path(bpy.data.filepath).parent==P
for o in list(bpy.context.scene.objects):bpy.data.objects.remove(o,do_unlink=True)
source=P.parents[1]/'CHR_MATTO/Game/CHR_MATTO_GAME.blend'
with bpy.data.libraries.load(str(source),link=False)as(a,b):b.objects=['Armature','Body_LOD0','Face_LOD0','Mesh']
for o in b.objects:
 if o.name not in bpy.context.scene.objects:bpy.context.scene.collection.objects.link(o)
for o,name in zip(b.objects,['Armature','Body_LOD0','Face_LOD0','Mesh']):o.name=name
rig=bpy.data.objects['Armature']; rig.animation_data_clear();rig.data.pose_position='REST'
for p in rig.pose.bones:
 p.rotation_mode='QUATERNION';p.rotation_quaternion=(1,0,0,0);p.location=(0,0,0);p.scale=(1,1,1)
rig.data=rig.data.copy();rig.data.transform(Matrix.Scale(.96,4))
for name in ['Body','Face']:
 o=bpy.data.objects[name+'_LOD0'];o.name=name;o.data=o.data.copy();o.data.transform(Matrix.Scale(.96,4),shape_keys=True)
 for mod in list(o.modifiers):
  if mod.type!='ARMATURE':o.modifiers.remove(mod)
 for mod in o.modifiers:mod.object=rig
 o.hide_render=False;o.hide_set(False)
body=bpy.data.objects['Body'];face=bpy.data.objects['Face']
def morph(p):
 x,y,z=p
 # Marihi: tapered lower face, fuller high cheek, slightly hooded upswept eyes.
 jaw=math.exp(-((z-1.344)/.035)**2);cheek=math.exp(-((z-1.408)/.029)**2)
 x*=1-.058*jaw+.018*cheek
 if y<-.045:
  w=math.exp(-((abs(x)-.052)/.043)**4-((z-1.444)/.049)**4)
  z+=w*(.0038*max(-1,min(1,(abs(x)-.045)/.035))-.0015)
  z=1.444+(z-1.444)*(1-.055*w)
  nose=math.exp(-(x/.014)**2-((z-1.398)/.016)**2);y+=.0018*nose
  mouth=math.exp(-(x/.033)**4-((z-1.370)/.010)**2);z+=mouth*.0018*(abs(x)/.027)
 return Vector((x,y,z))
for k in face.data.shape_keys.key_blocks:
 for v in k.data:v.co=morph(v.co)
 k.value=0
for v in face.data.vertices:v.co=morph(v.co)
for v in body.data.vertices:
 if v.co.z>1.30:v.co=morph(v.co)
# Fresh node materials avoid VRM callbacks restoring source material slots/UVs.
T=P.parents[3]/'game/UnityProject/Assets/_Game/Characters/CHR_MATTO/Textures'
atlas=bpy.data.images.load(str(T/'MATTO_FaceAtlas.png'),check_existing=False);atlas.name='MARIHI_FaceAtlas'
a=np.array(atlas.pixels[:],np.float32).reshape(atlas.size[1],atlas.size[0],4)
# UV atlas eye tile (left half, second row from top), preserve pupils and original radial ink.
N=atlas.size[0];ys=slice(N//2,N*3//4);xs=slice(0,N//2)
t=a[ys,xs,:3];r=t[:,:,0].copy();g=t[:,:,1].copy();b=t[:,:,2].copy()
mask=(r>g*1.30)&(r>b*1.30)
t[:,:,0][mask]=np.minimum(1,r[mask]*1.25+.025)
t[:,:,1][mask]=r[mask]*.49+g[mask]*.35
t[:,:,2][mask]=r[mask]*.12+b[mask]*.30
atlas.pixels.foreach_set(a.ravel());atlas.filepath_raw=str(P/'MARIHI_FaceAtlas.png');atlas.save();atlas.pack()
def toon(name,image,tint=(1,1,1,1)):
 m=bpy.data.materials.new(name);m.use_nodes=True;nt=m.node_tree;nt.nodes.clear()
 out=nt.nodes.new('ShaderNodeOutputMaterial');emit=nt.nodes.new('ShaderNodeEmission');tx=nt.nodes.new('ShaderNodeTexImage');tx.image=image
 diff=nt.nodes.new('ShaderNodeBsdfDiffuse');diff.inputs[0].default_value=(.8,.8,.8,1)
 rgb=nt.nodes.new('ShaderNodeShaderToRGB');ramp=nt.nodes.new('ShaderNodeValToRGB');ramp.color_ramp.interpolation='EASE'
 ramp.color_ramp.elements[0].position=.12;ramp.color_ramp.elements[0].color=(.63,.63,.63,1)
 ramp.color_ramp.elements[1].position=.75;ramp.color_ramp.elements[1].color=(1,1,1,1)
 mul=nt.nodes.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1
 color=nt.nodes.new('ShaderNodeMixRGB');color.blend_type='MULTIPLY';color.inputs[0].default_value=1;color.inputs[2].default_value=tint
 nt.links.new(diff.outputs[0],rgb.inputs[0]);nt.links.new(rgb.outputs[0],ramp.inputs[0]);nt.links.new(tx.outputs[0],color.inputs[1]);nt.links.new(color.outputs[0],mul.inputs[1]);nt.links.new(ramp.outputs[0],mul.inputs[2]);nt.links.new(mul.outputs[0],emit.inputs[0]);nt.links.new(emit.outputs[0],out.inputs['Surface'])
 m['tint']=list(tint);m['image']=image.name;m['export_role']='face'if 'Face'in name else'body';return m
cells={0:(1024,0,512,512),1:(0,1024,1024,512),2:(1024,1024,1024,512),3:(0,0,1024,1024),4:(0,1536,1024,512),5:(1024,1536,1024,256),6:(1024,1792,1024,256),7:(1024,512,1024,256)}
indices=[]
if len(face.data.materials)==8:
 uv=face.data.uv_layers.active.data
 for poly in face.data.polygons:
  slot=poly.material_index;x,y,w,h=cells[slot]
  for li in poly.loop_indices:
   u,v=uv[li].uv;uv[li].uv=((x+2+u*(w-4))/2048,(y+2+v*(h-4))/2048)
  indices.append(0 if slot in [0,3]else 2 if slot==4 else 3 if slot==2 else 1)
else:indices=[p.material_index for p in face.data.polygons]
face.data.materials.clear()
for role in ['Warm','Ink','White','Highlight']:face.data.materials.append(toon('MAT_MARIHI_Face'+role,atlas,(.96,.81,.72,1)if role=='Warm'else(1,1,1,1)))
for poly,i in zip(face.data.polygons,indices):poly.material_index=i
bi=bpy.data.images.load(str(T/'MATTO__10.png'),check_existing=False);bi.name='MARIHI_BodyAtlas';bi.pack()
body.data.materials[0]=toon('MAT_MARIHI_Body',bi,(.96,.81,.72,1))
face['Design']='Marihi: tapered jaw, high cheek, hooded upswept amber eyes and confident slight smile; all expressions transformed consistently'
rig['SourceTechnology']='Approved Matto clean topology and VRoid Humanoid; finished hair/outfit not reused'
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI.blend'),compress=True)
print('Marihi private face/body, expression morph and amber iris authored')
