import bpy,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent
face=bpy.data.objects['Face'];body=bpy.data.objects['Body']
def morph(p):
 x,y,z=p;cheek=math.exp(-((z-1.402)/.043)**2)*max(0,1-abs(y+.055)/.15);jaw=math.exp(-((z-1.345)/.029)**2)
 x*=1-.027*cheek-.030*jaw
 # Slightly sharper canthus and less vertically round eye contour; all relative keys follow.
 if y<-.045 and 1.409<z<1.490:
  w=math.exp(-((abs(x)-.050)/.041)**4)
  z=1.444+(z-1.444)*(1-.045*w)+.0012*w*max(-1,min(1,(abs(x)-.050)/.025))
 return Vector((x,y,z))
for key in face.data.shape_keys.key_blocks:
 for v in key.data:v.co=morph(v.co)
for v in face.data.vertices:v.co=morph(v.co)
for v in body.data.vertices:
 if v.co.z>1.30:v.co=morph(v.co)
for ob in list(bpy.context.scene.objects):
 if ob.type!='MESH':continue
 if ob.name.startswith(('Hair_Bang','Hair_Tail_')):
  count=8;rows=len(ob.data.vertices)//count
  for row in range(rows):
   t=row/max(1,rows-1)
   if row==0:continue
   ids=list(range(row*count,(row+1)*count));centre=sum((ob.data.vertices[i].co for i in ids),Vector())/count
   old=centre.copy()
   if ob.name.startswith('Hair_Tail_'):
    fac=math.sin(math.pi*t/2);centre.x*=1-.10*fac;centre.y=.025+(centre.y-.025)*(1-.11*fac);centre.z-=.012*t*t
    width=.78-.12*t;depth=.85
   else:width=1-.33*t*t;depth=1-.16*t
   for i in ids:
    d=ob.data.vertices[i].co-old;d.x*=width;d.y*=depth;ob.data.vertices[i].co=centre+d
 if ob.name.startswith('Outfit_CardiganSleeve_'):
  # Radial shrink around each authored local ring, preserving cuff/shoulder positions.
  nc=28
  for j in range(len(ob.data.vertices)//nc):
   ids=range(j*nc,(j+1)*nc);c=sum((ob.data.vertices[i].co for i in ids),Vector())/nc
   for i in ids:ob.data.vertices[i].co=c+(ob.data.vertices[i].co-c)*.94
 if ob.name=='Outfit_Blouse':
  for v in ob.data.vertices:
   z=v.co.z;w=math.exp(-((z-1.045)/.07)**2);v.co.x*=1-.035*w;v.co.y*=1-.025*w
 if ob.name=='Outfit_PleatedSkirt':
  for p in ob.data.polygons:p.use_smooth=False
for side in ['L','R']:
 ob=bpy.data.objects['Outfit_ShirtPuff_'+side]
 for j in [4,5]:
  ids=range(j*28,(j+1)*28);c=sum((ob.data.vertices[i].co for i in ids),Vector())/28
  for i in ids:ob.data.vertices[i].co=c+(ob.data.vertices[i].co-c)*(.93 if j==4 else .80)
for k in face.data.shape_keys.key_blocks:k.value=0
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_JIRO_GAME.blend'))
print('Sharper cheek/jaw/eyes, tapered bangs/tails, reduced gathered sleeves and defined skirt; original untouched')
