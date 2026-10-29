import bpy, traceback
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent;R=P/'Review'/'Luxury';R.mkdir(exist_ok=True)
s=bpy.context.scene;c=s.camera;states={o:o.hide_render for o in s.objects};cp=c.location.copy();cr=c.rotation_euler.copy();cs=c.data.ortho_scale
jobs=[('Room_%02d'%i,i,(12,-16,16),18 if i==1 else 15,None)for i in range(1,10)]
jobs += [('Exterior_Front',0,(-4.2,-38,17),47,(-4.2,1,1.5)),('Estate',0,(-34,-43,36),63,(0,1,.5)),('Entrance_Close',0,(-4.2,-10.8,4.0),8,(-4.2,-3.7,1.8)),('Entrance_Interior',1,(-.5,2.8,3.3),7,(-4.2,-3.25,1.95)),('Hall_HalfTop',1,(12,-16,16),18,None),('Hall_Side',1,(3.5,-4.8,3.5),17,(-1,1,.9))]
index=0
def run():
 global index
 try:
  name,room,offset,scale,target=jobs[index]
  for o,v in states.items():
   o.hide_render=v
   if room and o.type=='MESH'and o.get('StructureRole'):
    o.hide_render=v or int(o.get('StructureRoom',0))!=room
    p=bpy.data.objects.get(o.get('GoldParent',''));side=p.get('StructureSide','')if p else o.get('StructureSide','')
    if not o.get('DecorativeOnly')and side in (['North','East']if name.endswith('Interior')else['South','East']):o.hide_render=True
  if target:focus=Vector(target);c.location=offset
  else:focus=bpy.data.objects['REF_Room_%02d'%room].matrix_world.translation+Vector((0,0,.45));c.location=focus+Vector(offset)
  c.rotation_euler=(focus-c.location).to_track_quat('-Z','Y').to_euler();c.data.ortho_scale=scale
  s.render.engine='BLENDER_EEVEE';s.render.resolution_x=1440;s.render.resolution_y=1080;s.render.resolution_percentage=100;s.render.filepath=str(R/(name+'.png'));bpy.ops.render.render(write_still=True)
  index+=1;(R/'status.txt').write_text('%d/%d'%(index,len(jobs)))
  if index<len(jobs):return .4
 except Exception:(R/'status.txt').write_text(traceback.format_exc())
 for o,v in states.items():o.hide_render=v
 c.location=cp;c.rotation_euler=cr;c.data.ortho_scale=cs
 return None
bpy.app.timers.register(run,first_interval=.5)
