import bpy,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent;PACK=P/'Sources/FullPack/Mansion_CC0_FullPack';scene=bpy.context.scene
col=bpy.data.collections.new('VisualGold_Architecture');scene.collection.children.link(col)
materials={};additions=[]
def flat(name,color,metal=0,rough=.55,emission=0):
 m=bpy.data.materials.get('Gold_'+name)or bpy.data.materials.new('Gold_'+name);m.use_nodes=True;b=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=rough
 if emission:b.inputs['Emission Color'].default_value=(*color,1);b.inputs['Emission Strength'].default_value=emission
 m.diffuse_color=(*color,1);return m
def pbr(slug,name,tint=(1,1,1),rough=.62):
 m=bpy.data.materials.get('Gold_'+name)or bpy.data.materials.new('Gold_'+name);m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;b=next(q for q in n if q.type=='BSDF_PRINCIPLED');folder=PACK/'Textures'/slug
 for channel,pattern,socket in [('diff','*_diff_*','Base Color'),('rough','*_rough_*','Roughness'),('normal','*_nor_gl_*','Normal')]:
  paths=sorted(folder.glob(pattern));
  if not paths:continue
  im=bpy.data.images.load(str(paths[0]),check_existing=True);tx=n.new('ShaderNodeTexImage');tx.image=im
  if channel!='diff':im.colorspace_settings.name='Non-Color'
  if channel=='normal':norm=n.new('ShaderNodeNormalMap');norm.inputs['Strength'].default_value=.32;l.new(tx.outputs['Color'],norm.inputs['Color']);l.new(norm.outputs['Normal'],b.inputs[socket])
  elif channel=='diff':
   mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(*tint,1);l.new(tx.outputs['Color'],mix.inputs[1]);l.new(mix.outputs[0],b.inputs[socket]);m['GoldTint']=list(tint)
  else:l.new(tx.outputs['Color'],b.inputs[socket])
 b.inputs['Roughness'].default_value=rough;m['GoldTextureSource']=slug;m.diffuse_color=(*tint,1);return m
ivory=pbr('plastered_wall_04','WarmPlaster',(.78,.72,.60));wood=pbr('dark_paneled_wood','MahoganyPanelling',(.54,.40,.30));walnut=pbr('black_walnut_veneer_03','Walnut',(.70,.58,.43));gold=flat('AgedBrass',(.48,.29,.075),.72,.32);dark=flat('BlackenedIron',(.025,.032,.040),.65,.4);stone=pbr('monastery_stone_floor','Limestone',(.68,.66,.57));linen=flat('Linen',(.56,.51,.39),0,.85)
marble=pbr('marble_01','HallMarble',(.84,.82,.72),.34);parquet=pbr('diagonal_parquet','Parquet',(.56,.41,.29),.50);raw=pbr('raw_wooden_floor_01','ServiceTimber',(.52,.44,.32));cellar=pbr('old_stone_wall_02','CellarStone',(.53,.48,.39));rug=pbr('floral_jacquard','CrimsonJacquard',(.24,.042,.052),.88);kitchen=pbr('marble_01','KitchenStone',(.65,.67,.62));brick=pbr('castle_brick_02_red','EstateBrick',(.56,.43,.34));cobbles=pbr('cobblestone_floor_01','DrivewayStone',(.55,.52,.45));glass=flat('EstateGlass',(.07,.17,.20),.1,.18);bulb=flat('WarmLuminousGlass',(1,.64,.26),0,.32,3)
def world_uv(o,tile=2.0):
 o.data=o.data.copy();me=o.data
 if not me.uv_layers:me.uv_layers.new(name='UVMap')
 uv=me.uv_layers.active.data
 for p in me.polygons:
  coords=[o.matrix_world@me.vertices[i].co for i in p.vertices];normal=(coords[1]-coords[0]).cross(coords[2]-coords[0]).normalized()if len(coords)>=3 else Vector((0,0,1));axis=max(range(3),key=lambda i:abs(normal[i]));axes=[i for i in range(3)if i!=axis]
  for li in p.loop_indices:
   v=o.matrix_world@me.vertices[me.loops[li].vertex_index].co;uv[li].uv=(v[axes[0]]/tile,v[axes[1]]/tile)
 return o
def assign(o,mat,tile=2):world_uv(o,tile);o.data.materials.clear();o.data.materials.append(mat)
def box(name,loc,size,mat,room=0,parent='',collision=False,bevel=.015):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name='VG_'+name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o);o.data.materials.append(mat);world_uv(o,2)
 if bevel and min(size)>.01:
  m=o.modifiers.new('Finished edge profile','BEVEL');m.width=min(bevel,min(size)*.23);m.segments=2;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
 for k,v in {'StructureRole':'Trim'if parent else'Room'if room else'Exterior','StructureRoom':room,'StructureDoor':'','StructureSide':'','StructureCollision':collision,'GoldParent':parent,'ArtAssembly':o.name}.items():o[k]=v
 additions.append(o.name);return o
# Use only the real source floor/solid wall/door dimensions. No room footprint or opening is changed.
for item in scene.objects:
 if item.get('StructureRole')=='Wall':item.hide_set(False)
bpy.context.view_layer.update()
original=list(scene.objects)
for o in original:
 if o.type!='MESH'or not o.get('StructureRole')or o.get('GoldAsset'):continue
 role=o['StructureRole'];room=int(o.get('StructureRoom',0));name=o.name
 if role=='Floor':assign(o,marble if room in [1,6]else parquet if room in [2,3,4]else kitchen if room==5 else raw if room==9 else stone,2.8)
 elif role=='Wall':assign(o,ivory if room<7 else cellar if room in [7,8]else ivory,3.5)
 elif role in ['Door','Railing','Gallery','Support']:assign(o,walnut if role!='Support'else gold,1.8)
 elif role=='Stair':assign(o,walnut if room<7 else stone,1.4)
 elif role=='Passage':assign(o,parquet,2.8)
 elif role=='Room':
  if 'Rug'in name or 'Runner'in name:assign(o,rug,1.7)
  elif any(t in name for t in ['Boiler','Pipe','Valve','Flue','Burner']):assign(o,dark if 'Boiler'in name else gold)
  elif any(t in name for t in ['Marble','Counter','Sink','Hood','Range']):assign(o,kitchen if 'Marble'in name or 'Counter'in name else dark)
  elif any(t in name for t in ['Book','Bottle','Crate','Planter','Glass','Mullion']):pass
  else:assign(o,walnut)
for o in original:
 if o.type!='MESH'or o.get('StructureRole')!='Wall':continue
 room=int(o['StructureRoom']);p=o.matrix_world.translation;d=o.dimensions;bottom=p.z-d.z/2
 # Lintels are overhead; never lower a panel across an actual door opening.
 if bottom>.35 or d.z<2.6:continue
 axis=0 if d.x>d.y else 1;other=1-axis;length=d[axis];center=bpy.data.objects['REF_Room_%02d'%room].matrix_world.translation;sgn=1 if center[other]>p[other]else-1
 for label,z,height,material,offset in [('Skirting',bottom+.12,.18,walnut,.025),('Dado',bottom+1.08,.085,gold,.035),('Crown',bottom+d.z-.16,.12,walnut,.045)]:
  q=p.copy();q[other]+=sgn*(d[other]/2+offset);q.z=z;sz=Vector((.05,.05,height));sz[axis]=length
  box('R%02d_%s_%s'%(room,label,o.name),q,sz,material,room,o.name)
 if length>1.2:
  n=max(1,int(length/1.05));w=length/n-.16
  for j in range(n):
   q=p.copy();q[axis]+=-length/2+(j+.5)*length/n;q[other]+=sgn*(d[other]/2+.015);q.z=bottom+.61;sz=Vector((.028,.028,.79));sz[axis]=w
   box('R%02d_Panel_%s_%d'%(room,o.name,j),q,sz,wood,room,o.name)
   # Real thin framing, not a large decorative block.
   for dx in [-1,1]:
    t=q.copy();t[axis]+=dx*w/2;t[other]+=sgn*.018;ts=Vector((.04,.04,.79));box('R%02d_PanelStile_%s_%d_%d'%(room,o.name,j,dx),t,ts,walnut,room,o.name,.0)
# Finished kitchen fronts follow the existing cabinet's real footprint.
for o in list(original):
 if o.type!='MESH'or int(o.get('StructureRoom',0))!=5:continue
 if any(t in o.name for t in ['Cabinet','Worktop','Counter']):
  assign(o,walnut if 'Cabinet'in o.name else kitchen,1.5)
  if max(o.dimensions)>.5:
   m=o.modifiers.new('Cabinet edge rounding','BEVEL');m.width=.015;m.segments=3;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
# Record exact additions; source collider geometry and references are not replaced by these nonphysical profiles.
(P/'architecture-additions.json').write_text(json.dumps(additions,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'));print('Architecture profiles',len(additions),'materials',len([m for m in bpy.data.materials if m.name.startswith('Gold_')]))
