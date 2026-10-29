import bpy,math,json,re
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
P=Path(__file__).parent;PACK=P/'Sources/FullPack/Mansion_CC0_FullPack';scene=bpy.context.scene
manifest=json.loads((PACK/'manifest.json').read_text());spec={a['slug']:a for a in manifest['models']}
lib=bpy.data.collections.get('Gold_Asset_Library')or bpy.data.collections.new('Gold_Asset_Library')
if lib.name not in scene.collection.children:scene.collection.children.link(lib)
placed=bpy.data.collections.get('VisualGold_Dressing')or bpy.data.collections.new('VisualGold_Dressing')
if placed.name not in scene.collection.children:scene.collection.children.link(placed)
cache={};asset_report={};placements=[];retired=[]
budgets={'ArmChair_01':4500,'Chandelier_01':10000,'Chandelier_02':8000,'GothicCabinet_01':8000,'GothicCommode_01':3500,'GothicBed_01':12000,'gothic_coffee_table':6500,'ClassicConsole_01':5000,'vintage_grandfather_clock_01':7000,'fancy_picture_frame_02':5000,'wine_barrel_01':6500,'marble_bust_01':5000,'brass_vase_03':1600,'antique_ceramic_vase_01':3500,'tea_set_01':6500,'vintage_oil_lamp':4500,'potted_plant_01':9000,'potted_plant_02':7000,'potted_plant_04':3500,'horse_statue_01':5000,'throw_pillows_01':3000,'lantern_chandelier_01':4500}
def bbox(obs):
 ps=[o.matrix_world@Vector(v)for o in obs for v in o.bound_box]
 return Vector([min(v[i]for v in ps)for i in range(3)]),Vector([max(v[i]for v in ps)for i in range(3)])
def texture_fix(mat,folder):
 if not mat.use_nodes:return
 for n in mat.node_tree.nodes:
  if n.type=='TEX_IMAGE'and n.image:
   filename=Path(n.image.filepath.replace('\\','/')).name
   path=folder/'textures'/filename
   if path.exists():n.image.filepath=str(path);n.image.reload()
 for n in mat.node_tree.nodes:
  if n.type=='BSDF_PRINCIPLED':
   n.inputs['Roughness'].default_value=.52
   # Controlled PBR: no high-frequency displacement/subdivision in a mobile game.
def load(slug):
 if slug in cache:return cache[slug]
 path=PACK/spec[slug]['file'];lib.hide_viewport=False;lib.hide_render=False
 with bpy.data.libraries.load(str(path),link=False)as(src,dst):dst.objects=list(src.objects)
 obs=[o for o in dst.objects if o];meshes=[]
 for o in obs:
  lib.objects.link(o)
  for m in o.modifiers:
   if m.type in ['SUBSURF','NODES']:m.show_viewport=False;m.show_render=False
 bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
 for o in obs:
  if o.type!='MESH':continue
  ev=o.evaluated_get(dg);me=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=dg);me.transform(o.matrix_world)
  n=bpy.data.objects.new('CC0_'+slug+'_Bake',me);lib.objects.link(n);meshes.append(n)
  for mat in me.materials:texture_fix(mat,path.parent)
  if me.uv_layers:
   active=me.uv_layers.active
   for layer in list(me.uv_layers):
    if layer!=active:me.uv_layers.remove(layer)
   active.name='UVMap'
  else:me.uv_layers.new(name='UVMap')
 bpy.ops.object.select_all(action='DESELECT')
 for o in meshes:o.select_set(True)
 bpy.context.view_layer.objects.active=meshes[0]
 if len(meshes)>1:bpy.ops.object.join()
 ob=meshes[0];ob.name='CC0_Source_'+slug
 # Join preserves UVs and multi-material slots. Bake only detail beyond the measured per-asset budget.
 ob.data.calc_loop_triangles();raw=len(ob.data.loop_triangles);target=budgets.get(slug,raw)
 if raw>target:
  m=ob.modifiers.new('Mobile detail budget','DECIMATE');m.ratio=target/raw;m.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=m.name)
 lo,hi=bbox([ob]);center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z));ob.data.transform(Matrix.Translation(-center));ob.data.calc_loop_triangles()
 for i,mat in enumerate(list(ob.data.materials)):
  if not mat:continue
  copy=mat.copy();copy.name='Gold_CC0_'+slug+'_'+str(i);ob.data.materials[i]=copy
 for o in obs:bpy.data.objects.remove(o,do_unlink=True)
 ob.hide_render=True;ob.hide_set(True);cache[slug]=ob
 asset_report[slug]={'source':spec[slug]['source'],'raw_tris':raw,'game_tris':len(ob.data.loop_triangles),'measured_dimensions':list(hi-lo),'source_bottom':list(lo),'template_mesh':ob.data.name}
 return ob
# Protected positions and polyline corridors come from current references and LOCK manifest, never guessed coordinates.
refs=[o for o in scene.objects if o.name.startswith('REF_')];layout=json.loads((P.parent/'Pass02/environment_manifest.json').read_text())['layout']
protected=[]
for o in refs:
 if any(s in o.name for s in ['_Stand_','_Entry_To_','_Action_Point']):
  p=o.matrix_world.translation.copy();p.z-=.55 if '_Stand_'in o.name else .05;protected.append((o.name,p,.62 if '_Stand_'in o.name else .74))
routes=[(r['door'],[Vector((p['x'],p['z'],p['y']))for p in r['points']])for r in layout['routes']]
def distance_segment(p,a,b):
 v=b-a;t=max(0,min(1,(p-a).dot(v)/max(v.length_squared,1e-10)));return(p-(a+v*t)).length
floorbounds={}
for room in range(1,10):
 os=[o for o in scene.objects if o.get('StructureRole')=='Floor'and o.get('StructureRoom')==room];floorbounds[room]=bbox(os)
def conflicts(lo,hi,room):
 issues=[]
 # Hanging fixtures are above head clearance and do not consume walkable floor.
 for name,p,rad in protected:
  if not(hi.z>p.z+.08 and lo.z<p.z+1.85):continue
  if p.z<lo.z-2 or p.z>hi.z+1:continue
  d=Vector((max(lo.x-p.x,0,p.x-hi.x),max(lo.y-p.y,0,p.y-hi.y),0)).length
  if d<rad:issues.append(name)
 for door,ps in routes:
  for a,b in zip(ps,ps[1:]):
   n=max(2,int((b-a).length/.3))
   for j in range(n+1):
    p=a.lerp(b,j/n)
    if hi.z<=p.z+.06 or lo.z>=p.z+2.1:continue
    d=Vector((max(lo.x-p.x,0,p.x-hi.x),max(lo.y-p.y,0,p.y-hi.y),0)).length
    if d<.63:issues.append(door);break
 return sorted(set(issues))
def place(slug,room,anchor,label,height=None,rotation=0,fit=None,wall=False,collision=True):
 src=load(slug);ob=bpy.data.objects.new('VG_R%02d_%s'%(room,label),src.data);placed.objects.link(ob);ob.rotation_euler.z=rotation
 dims=Vector(asset_report[slug]['measured_dimensions']);scale=height/dims.z if height else 1
 if fit:scale=min(fit[0]/max(dims.x,1e-4),fit[1]/max(dims.y,1e-4),fit[2]/max(dims.z,1e-4))
 ob.scale=(scale,)*3;ob.location=anchor;ob.hide_render=False;ob.hide_set(False);bpy.context.view_layer.update();lo,hi=bbox([ob]);bad=conflicts(lo,hi,room)
 if room>0:
  flo,fhi=floorbounds[room]
  if lo.x<flo.x+.10 or hi.x>fhi.x-.10 or lo.y<flo.y+.10 or hi.y>fhi.y-.10:bad.append('ROOM_BOUNDARY')
 if bad:
  bpy.data.objects.remove(ob,do_unlink=True);raise ValueError(label+' intersects '+str(bad))
 for k,v in {'StructureRole':'Room'if room else'Exterior','StructureRoom':room,'StructureDoor':'','StructureSide':'','StructureCollision':collision,'ArtAssembly':ob.name,'GoldAsset':slug,'GoldWallMounted':wall}.items():ob[k]=v
 placements.append({'name':ob.name,'slug':slug,'room':room,'location':list(ob.location),'scale':scale,'rotation':rotation,'min':list(lo),'max':list(hi),'wallMounted':wall})
 return ob
def retire(names):
 for n in names:
  o=bpy.data.objects.get(n)
  if o and o.type=='MESH':retired.append(n);bpy.data.objects.remove(o,do_unlink=True)
def oldgroup(name):return [o for o in list(scene.objects)if o.get('ArtAssembly')==name]
def retiregroup(name):retire([o.name for o in oldgroup(name)])
def anchor(group):
 obs=oldgroup(group);assert obs,group
 lo,hi=bbox(obs);return Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z)),hi-lo
