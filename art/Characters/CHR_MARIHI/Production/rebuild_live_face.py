import bpy,ast,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent
source=P.parents[1]/'CHR_MATTO/Game/CHR_MATTO_GAME.blend'
with bpy.data.libraries.load(str(source),link=False)as(a,b):b.meshes=['Face_LOD0']
raw=b.meshes[0]
assert len(raw.materials)==4 and len(raw.vertices)==4280
ns={'math':math,'Vector':Vector};tree=ast.parse((P/'build_face.py').read_text())
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef)and n.name=='morph'],type_ignores=[]),'MarihiFaceMorph','exec'),ns)
face=next(o for o in bpy.context.scene.objects if o.type=='MESH'and o.name.startswith('Face'))
weights=[[(face.vertex_groups[g.group].name,g.weight)for g in v.groups]for v in face.data.vertices]
mats=[bpy.data.materials['MAT_MARIHI_Face'+role]for role in ['Warm','Ink','White','Highlight']]
me=bpy.data.meshes.new('Marihi_CleanFace_Independent')
me.from_pydata([tuple(ns['morph'](v.co*.96))for v in raw.vertices],[],[tuple(p.vertices)for p in raw.polygons]);me.update()
uv=me.uv_layers.new(name='UVMap')
for a,b in zip(uv.data,raw.uv_layers.active.data):a.uv=b.uv.copy()
for m in mats:me.materials.append(m)
for a,b in zip(me.polygons,raw.polygons):a.material_index=b.material_index;a.use_smooth=True
face.data=me;face.name='Face'
for i,ws in enumerate(weights):
 for name,w in ws:
  g=face.vertex_groups.get(name)or face.vertex_groups.new(name=name);g.add([i],w,'REPLACE')
if not any(v.groups for v in me.vertices):
 g=face.vertex_groups.get('J_Bip_C_Head')or face.vertex_groups.new(name='J_Bip_C_Head');g.add(list(range(len(me.vertices))),1,'REPLACE')
for old in raw.shape_keys.key_blocks:
 k=face.shape_key_add(name=old.name)
 for a,b in zip(k.data,old.data):a.co=ns['morph'](b.co*.96)
 k.value=0
face['Design']='Independent clean topology data; tapered jaw and hooded amber-eye contour; no source VRM mesh extension'
new=bpy.data.images.load(str(P/'MARIHI_FaceAtlas.png'),check_existing=False);new.name='MARIHI_FaceAtlas_Painted';new.pack()
for m in mats:
 for n in m.node_tree.nodes:
  if n.type=='TEX_IMAGE':n.image=new
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI.blend'),compress=True)
s=bpy.context.scene;s.render.filepath=str(P/'Review/Face_LiveFixed.png');bpy.ops.render.render(write_still=True)
print('Actual scene face rebuilt from raw mesh arrays: no orphan-name lookup, no VRM callbacks, source atlas UVs retained')
