"""Blender MCP only. Render scene cameras; never operates desktop input."""
import bpy,os,math,json
from mathutils import Vector
OUT=os.path.join(os.path.dirname(__file__),'StructureAudit','BlenderAfter')
os.makedirs(OUT,exist_ok=True)
scene=bpy.context.scene
prior_camera=scene.camera;prior_engine=scene.render.engine
prior=(scene.render.resolution_x,scene.render.resolution_y,scene.render.resolution_percentage,scene.render.filepath)
object_visibility={o:o.hide_render for o in scene.objects}
try:
 # Match the working cutaway viewport so foreground walls do not hide diagnostic angles.
 for o in scene.objects:
  if o.hide_get() or any(c.hide_viewport for c in o.users_collection):o.hide_render=True
 try:scene.render.engine='BLENDER_WORKBENCH'
 except TypeError as e:raise RuntimeError('Workbench unavailable: '+str(e))
 scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL'
 scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
 scene.render.resolution_x=1000;scene.render.resolution_y=700;scene.render.resolution_percentage=100
 data=bpy.data.cameras.new('ArchitectureEvidence');cam=bpy.data.objects.new('ArchitectureEvidence',data);scene.collection.objects.link(cam);scene.camera=cam
 data.type='ORTHO';data.clip_end=200
 centers={1:Vector((0,2,1)),2:Vector((-12,4,1)),3:Vector((12,4,1)),4:Vector((-11,-7,-1)),5:Vector((2,-8,-1)),6:Vector((0,13,1)),7:Vector((-10,-6,-3)),8:Vector((1,-7,-3)),9:Vector((10,-5,-3))}
 for room,center in centers.items():
  # Isolate the audited room and its actual connections so upper floors and
  # adjacent rooms cannot conceal basement walls in architectural evidence.
  routes=json.load(open(os.path.join(os.path.dirname(__file__),'StructureAudit','layout.json')))['routes']
  connected={r['door'] for r in routes if room in [r['a'],r['b']]}|{'DOOR_09_06_SECRET'}
  for o in scene.objects:
   hidden=object_visibility.get(o,False) or o.hide_get() or any(c.hide_viewport for c in o.users_collection)
   if o.type=='MESH':
    rid=int(o.get('StructureRoom',0));door=o.get('StructureDoor','')
    if rid and rid!=room:hidden=True
    if door and door not in connected:hidden=True
   o.hide_render=hidden
  for name,offset in [('Top',(0,0,25)),('Front',(0,-25,1)),('Side',(25,0,1)),('Iso',(20,-24,20))]:
   cam.location=center+Vector(offset);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();data.ortho_scale=14
   scene.render.filepath=os.path.join(OUT,'Room_%d_%s.png'%(room,name));bpy.ops.render.render(write_still=True)
  # Same viewing direction and offset as Unity's existing fixed play camera.
  focus=center.copy();focus.z=0 if room<=6 else -4
  cam.location=focus+Vector((0,-24,28));cam.rotation_euler=Vector((0,math.cos(math.radians(42)),-math.sin(math.radians(42)))).to_track_quat('-Z','Y').to_euler()
  for label,zoom in [('Near',9),('Default',19),('Full',32)]:
   data.ortho_scale=zoom*2*1000/700
   scene.render.filepath=os.path.join(OUT,'Room_%d_Play_%s.png'%(room,label));bpy.ops.render.render(write_still=True)
finally:
 for o,state in object_visibility.items():o.hide_render=state
 scene.camera=prior_camera;scene.render.engine=prior_engine
 scene.render.resolution_x,scene.render.resolution_y,scene.render.resolution_percentage,scene.render.filepath=prior
 if 'cam' in locals():bpy.data.objects.remove(cam,do_unlink=True);bpy.data.cameras.remove(data)
print('63 isolated-room Blender architectural camera/zoom captures saved to '+OUT)

