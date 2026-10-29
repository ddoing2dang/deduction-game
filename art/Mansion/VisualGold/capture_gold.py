import bpy,json,math,traceback
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent;R=P/'Review';R.mkdir(exist_ok=True);s=bpy.context.scene;c=s.camera;states={o:o.hide_render for o in s.objects};baseline=json.loads((P.parent/'Pass02/environment_manifest.json').read_text());partmap={p['name']:p for p in baseline['parts']};parts=[o for o in s.objects if o.type=='MESH'and o.get('StructureRole')]
def run():
 try:
  s.render.engine='BLENDER_EEVEE';s.render.resolution_x=1280;s.render.resolution_y=960;s.render.resolution_percentage=100
  for room in range(1,10):
   center=bpy.data.objects['REF_Room_%02d'%room].matrix_world.translation.copy();center.z+=.45
   for o in parts:
    rid=int(o.get('StructureRoom',0));hidden=states[o]or rid!=room
    if o.get('StructureRole')=='Wall'and o.get('StructureSide')in['South','East']:hidden=True
    parent=o.get('GoldParent','')
    if parent in partmap and partmap[parent]['side']in['South','East']:hidden=True
    o.hide_render=hidden
   # Match the real room origin and an architectural three-quarter camera, not arbitrary isolated furniture.
   c.location=center+Vector((12,-16,16));c.rotation_euler=(center-c.location).to_track_quat('-Z','Y').to_euler();c.data.ortho_scale=15 if room!=1 else 18
   s.render.filepath=str(R/('Room_%02d_Gold.png'%room));bpy.ops.render.render(write_still=True)
  for o,v in states.items():
   if o.name in bpy.data.objects:o.hide_render=v
  c.location=(34,-43,42);c.rotation_euler=(Vector((0,1,.5))-c.location).to_track_quat('-Z','Y').to_euler();c.data.ortho_scale=63;s.render.resolution_x=1500;s.render.resolution_y=1150;s.render.filepath=str(R/'Estate_Refined.png');bpy.ops.render.render(write_still=True)
  (R/'capture-status.txt').write_text('Ten current EEVEE renders captured; direct review required')
 except Exception:(R/'capture-status.txt').write_text(traceback.format_exc())
 finally:
  for o,v in states.items():
   if o.name in bpy.data.objects:o.hide_render=v
 return None
bpy.app.timers.register(run,first_interval=.3)
