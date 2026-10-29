import bpy,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent;A=bpy.app.driver_namespace['GoldArchitecture'];N=bpy.app.driver_namespace['GoldAuthoring'];B=A['box'];scene=bpy.context.scene;added=[]
# PBR warmth is restrained so the estate does not become a cosy orange dollhouse.
for m in bpy.data.materials:
 if m.name=='Gold_Parquet':
  for n in m.node_tree.nodes:
   if n.type=='MIX_RGB':n.inputs[2].default_value=(.38,.34,.30,1)
  m['GoldTint']=[.38,.34,.30]
 if m.name=='Gold_GardenEvergreen':
  next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Base Color'].default_value=(.07,.095,.055,1)
for name in ['Gold_MoonFill','Gold_EstateWarm']:
 o=bpy.data.objects.get(name)
 if o:o.data.energy=6500
# Exterior-facing solid wall faces use brick; interior plaster is retained. Openings and collision remain unchanged.
for o in list(scene.objects):
 if o.type!='MESH'or o.get('StructureRole')!='Wall':continue
 room=int(o.get('StructureRoom',0));p=o.matrix_world.translation;d=o.dimensions;bottom=p.z-d.z/2
 if room<1:continue
 center=bpy.data.objects['REF_Room_%02d'%room].matrix_world.translation;axis=0 if d.x>d.y else 1;normalaxis=1-axis;inward=1 if center[normalaxis]>p[normalaxis]else-1
 if A['brick']not in list(o.data.materials):o.data.materials.append(A['brick'])
 brickindex=list(o.data.materials).index(A['brick'])
 for face in o.data.polygons:
  normal=o.matrix_world.to_3x3()@face.normal
  if normal[normalaxis]*inward<-.7:face.material_index=brickindex
 if bottom>.35 or d.z<3 or d[axis]<2.2:continue
 # Windows are framed exterior art over an existing solid wall, never a new traversable aperture.
 levels=[bottom+1.65]+([bottom+4.6]if d.z>6 else [])
 count=max(1,int(d[axis]/2.4));width=min(1.1,d[axis]/count-.5)
 for j in range(count):
  for k,z in enumerate(levels):
   q=p.copy();q[axis]+=-d[axis]/2+(j+.5)*d[axis]/count;q[normalaxis]-=inward*(d[normalaxis]/2+.025);q.z=z
   size=Vector((.035,.035,1.6));size[axis]=width
   name='R%02d_ExteriorWindow_%s_%d_%d'%(room,o.name,j,k)
   pane=B(name,q,size,A['glass'],room,o.name);pane['GoldExteriorWindow']=True;added.append(pane.name)
   for pos,width2,height in [(-width/2,.07,1.72),(width/2,.07,1.72),(0,width+.14,.075)]:
    t=q.copy();t[axis]+=pos;t[normalaxis]-=inward*.04
    if pos==0:t.z=z+.84
    ss=Vector((.08,.08,height));ss[axis]=width2;added.append(B(name+'_Frame'+str(pos),t,ss,A['stone'],room,o.name).name)
   for zoff in [-.80,0]:
    t=q.copy();t.z+=zoff;t[normalaxis]-=inward*.04;ss=Vector((.045,.045,.045));ss[axis]=width;added.append(B(name+'_Cross'+str(zoff),t,ss,A['gold'],room,o.name).name)
   t=q.copy();t[normalaxis]-=inward*.04;ss=Vector((.045,.045,1.6));added.append(B(name+'_Centre',t,ss,A['gold'],room,o.name).name)
# A real open fire surround replaces the solid placeholder block at the exact mantel position.
fire=bpy.data.objects['R03_Hero_Fireplace'];lo,hi=N['bbox']([fire]);x=(lo.x+hi.x)/2;y=(lo.y+hi.y)/2
N['retire'](['R03_Hero_Fireplace','R03_Hero_Mantel'])
for side in [-1,1]:B('R03_Fireplace_Pier'+str(side),(x+side*.79,y,.70),(.27,.60,1.4),A['stone'],3,collision=True,bevel=.05)
B('R03_Fireplace_Entablature',(x,y,1.45),(1.96,.65,.22),A['stone'],3,collision=True,bevel=.04)
B('R03_Fireplace_Mantel',(x,y-.04,1.66),(2.3,.83,.14),A['walnut'],3,collision=True,bevel=.04)
B('R03_Firebox_Back',(x,hi.y-.04,.65),(1.30,.045,1.18),A['dark'],3,bevel=.01)
B('R03_Firebox_Hearth',(x,y-.06,.065),(2.30,.90,.13),A['stone'],3,collision=True,bevel=.035)
# Refined kitchen face, doors and brass hardware follow the original cooking-unit bounds.
stove=bpy.data.objects['R05_Hero_Range'];lo,hi=N['bbox']([stove]);cx=(lo.x+hi.x)/2
for j in range(3):
 x=lo.x+.45+j*.80;B('R05_OvenDoor'+str(j),(x,hi.y+.018,.55),(.67,.045,.63),A['dark'],5,bevel=.025);B('R05_OvenHandle'+str(j),(x,hi.y+.075,.87),(.39,.035,.045),A['gold'],5,bevel=.012)
for o in list(scene.objects):
 if o.name.startswith('R05_P02_Shelves_Jar'):N['retire']([o.name])
# The previous header cube becomes a shaped metal canopy and upper duct, in its measured location.
hood=bpy.data.objects['R05_Hero_Hood'];lo,hi=N['bbox']([hood]);cx=(lo.x+hi.x)/2;cy=(lo.y+hi.y)/2;N['retire']([hood.name])
vs=[(cx+sx*w,cy+sy*d,z)for z,w,d in [(2.34,1.3,.425),(2.94,.50,.24)]for sx,sy in [(-1,-1),(1,-1),(1,1),(-1,1)]];fs=[(0,1,2,3),(4,7,6,5)]+[(i,(i+1)%4,(i+1)%4+4,i+4)for i in range(4)]
me=bpy.data.meshes.new('Gold_ShapedRangeCanopy');me.from_pydata(vs,[],fs);me.materials.append(A['dark']);ob=bpy.data.objects.new('VG_R05_ShapedRangeCanopy',me);A['col'].objects.link(ob)
for key,val in {'StructureRole':'Room','StructureRoom':5,'StructureDoor':'','StructureSide':'','StructureCollision':False,'ArtAssembly':'KitchenRangeHood'}.items():ob[key]=val
B('R05_RangeCanopyBand',(cx,cy,2.36),(2.63,.88,.06),A['gold'],5,bevel=.01)
# Catalogue records cover the current measured geometry after the corrections.
for row in N['placements']:
 ob=bpy.data.objects[row['name']];lo,hi=N['bbox']([ob]);row['min']=list(lo);row['max']=list(hi)
(P/'retired-placeholders.json').write_text(json.dumps(N['retired'],indent=2));(P/'placements.json').write_text(json.dumps(N['placements'],indent=2));(P/'exterior-window-parts.json').write_text(json.dumps(added,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'));print('Exterior window/stone profiles',len(added),'fireplace and range refined')
