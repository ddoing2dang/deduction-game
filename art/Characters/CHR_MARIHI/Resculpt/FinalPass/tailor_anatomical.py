import bpy,bmesh,math,json,numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).parent;rig=bpy.data.objects['Armature'];game=bpy.data.collections['Game_LOD_Meshes'];archive=bpy.data.collections['Approved_Source_Archive']
rig.data.pose_position='REST'
raw=bpy.data.objects.get('Body.001')
if raw is None:
 with bpy.data.libraries.load(str(P.parents[2]/'CHR_MATTO/VRoidVisual/CHR_MATTO_VRoid_Visual.blend'),link=False) as(a,b):b.objects=['Body']
 raw=b.objects[0]
palette=bpy.data.materials['MAT_MARIHI_Palette'];tex=next(n.image for n in palette.node_tree.nodes if n.type=='TEX_IMAGE');px=np.array(tex.pixels[:]).reshape(128,128,4)
def coloruv(rgb):
 d=np.linalg.norm(px[8::16,8::16,:3]-np.array(rgb),axis=2);y,x=np.unravel_index(np.argmin(d),d.shape);return((x*16+8)/128,(y*16+8)/128)
white=coloruv((.80,.72,.64));black=coloruv((.025,.020,.027));gold=coloruv((.60,.37,.10))
def garment_shape(p):
 x,y,z=p;ax=abs(x);waist=math.exp(-((z-1.04)/.085)**2);chest=math.exp(-((z-1.16)/.065)**2)
 center=max(0,min(1,(.19-ax)/.07))
 x*=1-center*(.10*waist+.04*chest)
 y=.018+(y-.018)*(1-center*(.12*waist+.08*chest))
 return Vector((x,y,z))
# Clean connected shirt topology; retain the original shoulder/arm weights, not rigid torso rings.
top=raw.copy();top.data=raw.data.copy();game.objects.link(top);top.name='Marihi_AnatomicalTailoredTop';top.parent=None;top.matrix_world.identity()
if top.data.shape_keys:top.shape_key_clear()
top.modifiers.clear();top.data.transform(__import__('mathutils').Matrix.Scale(.96,4))
bm=bmesh.new();bm.from_mesh(top.data)
bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index!=3],context='FACES')
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
for co,no in [((0,0,.982),(0,0,-1)),((.245,0,0),(1,0,0)),((-.245,0,0),(-1,0,0))]:
 bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),plane_co=co,plane_no=no,dist=.000001,clear_outer=True,clear_inner=False)
for v in bm.verts:v.co=garment_shape(v.co)
bm.normal_update()
for v in bm.verts:v.co+=v.normal*.003
# Sleeve continuation shares the actual armhole vertices: no separate shoulder cap or gap.
deform=bm.verts.layers.deform.verify();groupnames={g.name:g.index for g in top.vertex_groups}
for side,sign in [('L',1),('R',-1)]:
 edges=[e for e in bm.edges if e.is_boundary and all(abs(v.co.x-sign*.245)<.006 for v in e.verts)]
 assert len(edges)>5,(side,len(edges))
 for j,xx in enumerate([.27,.30,.335,.37,.405,.44,.475,.505,.525,.537]):
  oldverts=set(v for e in edges for v in e.verts);cy=.0247;cz=1.2384
  oldcenter=sum((v.co for v in oldverts),Vector())/len(oldverts)
  oldx=oldcenter.x;oldry=max(abs(v.co.y-oldcenter.y)for v in oldverts);oldrz=max(abs(v.co.z-oldcenter.z)for v in oldverts)
  r=.043+.008*math.sin((xx-.245)/.292*math.pi)**2+.003*math.sin(j*2.0)
  if xx>.50:r=.037-(xx-.50)*.12
  out=bmesh.ops.extrude_edge_only(bm,edges=edges);newverts=[v for v in out['geom']if isinstance(v,bmesh.types.BMVert)]
  for v in newverts:
   dy=(v.co.y-oldcenter.y)/max(.01,oldry);dz=(v.co.z-oldcenter.z)/max(.01,oldrz);angle=math.atan2(dz,dy)
   gather=1+.035*math.cos(angle*6+j*.6)
   v.co=(sign*xx,cy+dy*r*gather,cz+dz*r*.94*gather)
   v[deform].clear();t=max(0,min(1,(xx-.29)/.07))
   for bone,w in [('J_Bip_'+side+'_UpperArm',1-t),('J_Bip_'+side+'_LowerArm',t)]:
    if w>1e-7:v[deform][groupnames[bone]]=w
  ns=set(newverts);edges=[e for e in bm.edges if e.is_boundary and all(v in ns for v in e.verts)]
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(top.data);bm.free();top.data.update()
# Single connected thin garment surface, bounded hems get explicit 1mm thickness.
for layer in list(top.data.uv_layers):top.data.uv_layers.remove(layer)
top.data.materials.clear();top.data.materials.append(palette);top.data.materials.append(bpy.data.materials['MAT_MARIHI_SleevePrint'])
# The sewn black/ivory panels share one continuous surface; a fabric mask avoids overlapping skins.
N=1024;yy,xx=np.mgrid[:N,:N];u=xx/(N-1);vv=yy/(N-1);z=.98+vv*.34;angle=(u-.5)*math.tau;t=np.clip((z-.982)/.205,0,1)
rx=.121+.016*np.sin(t*math.pi*.8);xin=.026+.052*t**1.25
edge=np.arcsin(np.minimum(.90,xin/rx));absangle=np.abs(angle)
darkmask=(z<1.187-.011*np.maximum(0,-np.cos(angle)))&(absangle>edge)
def swatch(uv):return px[int(uv[1]*128),int(uv[0]*128)]
clothpixels=np.empty((N,N,4),np.float32);clothpixels[:]=swatch(white);clothpixels[darkmask]=swatch(black)
binding=(np.abs(absangle-edge)<.009)&(z<1.184);clothpixels[binding]=swatch(gold)
image=bpy.data.images.new('MARIHI_TailoredCloth',width=N,height=N,alpha=True);image.colorspace_settings.name='Non-Color';image.pixels.foreach_set(clothpixels.ravel());image.filepath_raw=str(P/'MARIHI_TailoredCloth.png');image.save();image.pack()
clothmat=palette.copy();clothmat.name='MAT_MARIHI_TailoredCloth';next(n for n in clothmat.node_tree.nodes if n.type=='TEX_IMAGE').image=image;top.data.materials[0]=clothmat
ul=top.data.uv_layers.new(name='UVMap');vc=top.data.color_attributes.get('Color') or top.data.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER');ul=top.data.uv_layers.active
for poly in top.data.polygons:
 c=poly.center;ax=abs(c.x);t=max(0,min(1,(c.z-.982)/.205));inner=.026+.052*t**1.25
 dark=(c.z<1.187 and (ax>inner or c.y>.025))or ax>.165
 poly.material_index=1 if ax>.24 else 0;poly.use_smooth=True
 for li in poly.loop_indices:
  p=top.data.vertices[top.data.loops[li].vertex_index].co
  ul.data[li].uv=((abs(p.x)-.24)/.30,(math.atan2(p.z-1.2384,p.y-.0247)+math.pi)/math.tau)if ax>.24 else ((math.atan2(p.x,-(p.y-.014))+math.pi)/math.tau,(p.z-.98)/.34)
  vc.data[li].color=(1,1,1,1)
# Map only genuine deform groups to the current rig; preserve fingers and unchanged Humanoid structure.
for v in top.data.vertices:
 ws={}
 for g in v.groups:
  n=top.vertex_groups[g.group].name
  if n.startswith('J_Sec_')and 'Bust'in n:n='J_Bip_C_UpperChest'
  if n not in rig.data.bones:continue
  ws[n]=ws.get(n,0)+g.weight
 if not ws:ws={'J_Bip_C_Chest':1}
 for g in list(v.groups):top.vertex_groups[g.group].remove([v.index])
 for n,w in ws.items():
  vg=top.vertex_groups.get(n)or top.vertex_groups.new(name=n);vg.add([v.index],w/sum(ws.values()),'REPLACE')
# Fair torso/sleeve normals and add one controlled subdivision on this small clean piece only.
bpy.ops.object.select_all(action='DESELECT');top.hide_viewport=False;top.hide_set(False);bpy.context.view_layer.update();top.select_set(True);bpy.context.view_layer.objects.active=top
m=top.modifiers.new('Tailored cloth subdivision','SUBSURF');m.levels=1;bpy.ops.object.modifier_apply(modifier=m.name)
m=top.modifiers.new('Curvature preserving garment surface budget','DECIMATE');m.ratio=.22;m.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=m.name)
m=top.modifiers.new('Fabric edge thickness 1mm','SOLIDIFY');m.thickness=.001;m.offset=-1;bpy.ops.object.modifier_apply(modifier=m.name)
top.data.update();top.data.calc_loop_triangles()
tree=BVHTree.FromPolygons([v.co for v in top.data.vertices],[tuple(t.vertices)for t in top.data.loop_triangles],all_triangles=True)
def front(x,z,off=.0015):
 hit=tree.ray_cast(Vector((x,-.5,z)),Vector((0,1,0)),1)[0]
 return Vector((x,(hit.y if hit else -.075)-off,z))
def piece(name,vs,fs,uv,weights):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);game.objects.link(o);me.materials.append(palette);u=me.uv_layers.new(name='UVMap');cc=me.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER');u=me.uv_layers.active
 for i in range(len(me.loops)):u.data[i].uv=uv;cc.data[i].color=(1,1,1,1)
 for p in me.polygons:p.use_smooth=True
 for v in me.vertices:
  for n,w in weights(v.co).items():
   if w>1e-7:g=o.vertex_groups.get(n)or o.vertex_groups.new(name=n);g.add([v.index],w,'REPLACE')
 return o
def torso_weights(p):
 from mathutils.interpolate import poly_3d_calc
 h,n,idx,dist=tree.find_nearest(p);ids=top.data.loop_triangles[idx].vertices
 bary=poly_3d_calc([top.data.vertices[i].co for i in ids],h);ws={}
 for i,a in zip(ids,bary):
  for g in top.data.vertices[i].groups:
   name=top.vertex_groups[g.group].name;ws[name]=ws.get(name,0)+g.weight*a
 return ws
extras=[]
# Black side panels use the same fitted mesh, with sewn binding on its surface; no rigid overlay.
# Folded collar follows curved clavicle surface, not a broad horizontal plate.
for sign in [-1,1]:
 vs=[];fs=[]
 for j in range(5):
  t=j/4
  for k in range(5):
   u=k/4;x=sign*(.009+.038*u*(1-.35*t));z=1.283-.058*t-.012*u;vs.append(front(x,z,.002+.002*math.sin(t*math.pi)))
 for j in range(4):
  for k in range(4):a=j*5+k;fs.append((a,a+1,a+6,a+5))
 extras.append(piece('Tailored_Collar_'+str(sign),vs,fs,white,torso_weights))
 # Bound V-edge is sewn onto the continuous cloth shell.
 points=[]
 for j in range(28):
  t=j/27;z=.988+.194*t;points.append(front(sign*(.026+.052*t**1.25),z,.002))
 vs=[];fs=[]
 for p in points:
  for k in range(5):a=k*math.tau/5;vs.append(p+Vector((.00085*math.cos(a),.00085*math.sin(a),0)))
 for j in range(len(points)-1):
  for k in range(5):a=j*5+k;b=j*5+(k+1)%5;fs.append((a,b,b+5,a+5))
 extras.append(piece('Tailored_GoldBinding_'+str(sign),vs,fs,gold,torso_weights))
# Reuse Marihi's own bow, tie and buttons as design parts, newly fitted to the cloth surface.
for source in archive.objects:
 if not source.name.startswith(('Outfit_NeckBow','Outfit_NeckTie','Accessory_TieGold','Accessory_Button_')):continue
 o=source.copy();o.data=source.data.copy();game.objects.link(o);o.modifiers.clear();o.parent=None;o.matrix_world.identity();o.name='Tailored_'+source.name
 for v in o.data.vertices:
  q=front(v.co.x,v.co.z,.0035);v.co.y=q.y-.002-max(0,max(p.co.y for p in source.data.vertices)-v.co.y)*.55
  for g in list(v.groups):o.vertex_groups[g.group].remove([v.index])
  for n,w in torso_weights(v.co).items():
   if w>1e-7:g=o.vertex_groups.get(n)or o.vertex_groups.new(name=n);g.add([v.index],w,'REPLACE')
 # Original source materials are replaced with exact existing palette swatches.
 for layer in list(o.data.uv_layers):o.data.uv_layers.remove(layer)
 o.data.materials.clear();o.data.materials.append(palette);u=o.data.uv_layers.new(name='UVMap');co=o.data.color_attributes.get('Color')or o.data.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER');u=o.data.uv_layers.active
 for i in range(len(o.data.loops)):u.data[i].uv=gold if source.name.startswith(('Accessory_TieGold','Accessory_Button'))else black;co.data[i].color=(1,1,1,1)
 extras.append(o)
# Preserve a complete editable connected source before making the lower LODs.
bpy.ops.object.select_all(action='DESELECT')
for o in [top]+extras:o.select_set(True)
bpy.context.view_layer.objects.active=top;bpy.ops.object.join()
archivecopy=top.copy();archivecopy.data=top.data.copy();archive.objects.link(archivecopy);archivecopy.name='FinalTailor_ConnectedTop_Source';archivecopy.parent=bpy.data.objects['Mesh'];m=archivecopy.modifiers.new('Humanoid Skin','ARMATURE');m.object=rig
counts={}
for level in range(3):
 base=bpy.data.objects['Outfit_LOD'+str(level)];me=base.data;bm=bmesh.new();bm.from_mesh(me)
 bmesh.ops.delete(bm,geom=[f for f in bm.faces if any(v.co.z>.998 for v in f.verts)],context='FACES');bm.to_mesh(me);bm.free()
 # Remove all discarded upper outfit pieces; skirt/waist geometry retains its authored cloth weights.
 ob=top if level==0 else archivecopy.copy()
 if level>0:ob.data=archivecopy.data.copy();game.objects.link(ob);ob.modifiers.clear();ob.parent=None;ob.matrix_world.identity()
 bpy.ops.object.select_all(action='DESELECT');ob.hide_set(False);ob.select_set(True);bpy.context.view_layer.objects.active=ob
 if level>0:
  m=ob.modifiers.new('Connected garment LOD reduction','DECIMATE');m.ratio=[1,.57,.28][level];m.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=m.name)
 base.hide_set(False);base.select_set(True);bpy.context.view_layer.objects.active=base;bpy.ops.object.join();base.hide_render=level!=0;base.hide_set(level!=0)
 base.data.calc_loop_triangles();counts[base.name]=len(base.data.loop_triangles)
 # Accessory renderer must no longer contain old floating sleeve/neck ornament fragments.
 acc=bpy.data.objects['Accessory_LOD'+str(level)];bm=bmesh.new();bm.from_mesh(acc.data);bmesh.ops.delete(bm,geom=[f for f in bm.faces if any(v.co.z>1.015 for v in f.verts)],context='FACES');bm.to_mesh(acc.data);bm.free()
# The imported technical source remains hidden and is not exported or an extra character.
bpy.data.objects.remove(raw,do_unlink=True)
rig.data.pose_position='POSE';bpy.context.view_layer.update()
(P/'tailor-construction.json').write_text(json.dumps({'construction':'Connected anatomy-following cloth mesh, true inherited shoulder weights, shared armhole/sleeve vertices, curved clavicle collar, 1mm edge thickness, attached gold binding','outfitTris':counts},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI_FINAL_REVIEW.blend'),compress=True)
print(counts)
