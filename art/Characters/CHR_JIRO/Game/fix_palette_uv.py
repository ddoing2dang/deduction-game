import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).parent;archive=list(bpy.data.collections['Approved_Source_Archive'].objects);game=bpy.data.collections['Game_LOD_Meshes']
def group(o):
 n=o.name
 if n.startswith('Hair_'):return 'Hair_Main'
 if n.startswith('ACC_JIRO_'):return 'ACC_JIRO_Flower'
 if n.startswith('Shoes_')or n.startswith('Outfit_Sock'):return 'Shoes'
 if n.startswith('Accessory_'):return 'Accessory'
 return 'Outfit'
materials={m for o in archive for m in o.data.materials};flat=[]
for m in materials:
 if m.name.startswith(('MAT_JIRO_','JIRO_Toon_Hair_')):continue
 tex=next((n.image for n in m.node_tree.nodes if n.type=='TEX_IMAGE'and n.image),None)
 if tex is None:flat.append(m)
flat=sorted(flat,key=lambda m:m.name);pos={m:((i%8*16+8)/128,(i//8*16+8)/128)for i,m in enumerate(flat)}
lookup={}
for key in ['Outfit','Shoes','Accessory','ACC_JIRO_Flower']:
 vs=[];fs=[];mats=[]
 for o in archive:
  if o.name in ['Body','Face']or group(o)!=key:continue
  off=len(vs);vs.extend([o.matrix_world@v.co for v in o.data.vertices]);o.data.calc_loop_triangles()
  for t in o.data.loop_triangles:fs.append(tuple(i+off for i in t.vertices));mats.append(o.data.materials[o.data.polygons[t.polygon_index].material_index])
 lookup[key]=(BVHTree.FromPolygons(vs,fs,all_triangles=True),mats)
count=0
for o in game.objects:
 key=o.name.rsplit('_LOD',1)[0]
 if key not in lookup:continue
 tree,mats=lookup[key];uv=o.data.uv_layers.active.data
 for p in o.data.polygons:
  if o.data.materials[p.material_index].name!='MAT_JIRO_ColorPalette':continue
  centre=sum((o.data.vertices[i].co for i in p.vertices),Vector())/len(p.vertices);hit=tree.find_nearest(centre);mat=mats[hit[2]]
  if mat not in pos:
   # At a shared garment boundary use the closest palette-coloured source polygon.
   candidates=tree.find_nearest_range(centre,.015);valid=[h for h in candidates if mats[h[2]]in pos]
   if valid:mat=mats[min(valid,key=lambda h:h[3])[2]]
  assert mat in pos,(o.name,mat.name)
  for li in p.loop_indices:uv[li].uv=pos[mat]
  count+=1
 o.data.update()
(P/'palette-uv-fit.json').write_text(json.dumps({'polygons':count,'palette_materials':[m.name for m in flat],'reason':'UV references are reacquired after adding corner colour attributes in Blender 5'},indent=2))
print('Palette UVs fitted',count)
