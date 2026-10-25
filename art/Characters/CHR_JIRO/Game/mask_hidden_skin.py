import bpy,bmesh,json
from pathlib import Path
P=Path(__file__).parent;report={}
for level in range(3):
 o=bpy.data.objects['Body_LOD'+str(level)];bm=bmesh.new();bm.from_mesh(o.data);delete=[]
 for f in bm.faces:
  points=[v.co for v in f.verts]
  torso=all(.965<v.z<1.22 and abs(v.x)<.158 for v in points)
  sock=all(.145<v.z<(.393 if v.x>0 else .226)and abs(v.x)<.18 for v in points)
  if torso or sock:delete.append(f)
 report[o.name]=len(delete);bmesh.ops.delete(bm,geom=delete,context='FACES');bm.to_mesh(o.data);bm.free();o.data.update()
(P/'hidden-skin-mask.json').write_text(json.dumps({'removed_polygons':report,'scope':'Fully clothed torso and calf interiors only'},indent=2))
