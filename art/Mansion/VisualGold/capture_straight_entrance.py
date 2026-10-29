import bpy,json,traceback
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent;stage=bpy.app.driver_namespace.get('EntranceCaptureStage','Before');R=P/'Review'/'StraightEntrance'/stage;R.mkdir(parents=True,exist_ok=True)
s=bpy.context.scene;c=s.camera
st={o:o.hide_render for o in s.objects};cp=c.location.copy();cr=c.rotation_euler.copy();ct=c.data.type;cz=c.data.ortho_scale;cl=c.data.lens
jobs=[('Front',(-4.2,-20,5),(-4.2,-6,1.8),12,'ORTHO'),('Inside',(-4.2,-12.9,1.7),(-4.2,-3.7,1.9),28,'PERSP'),('HalfTop',(-4.2,-18,11),(-4.2,-7.5,1.0),15,'ORTHO'),('LeftWall',(-3.4,-11.7,2.4),(-5.5,-6.7,2),28,'PERSP'),('RightWall',(-5,-11.7,2.4),(-2.9,-6.7,2),28,'PERSP'),('Door',(-4.2,-6,2.0),(-4.2,-3.5,1.9),5.5,'ORTHO')]
index=0
def run():
 global index
 try:
  name,pos,target,size,kind=jobs[index]
  for o,v in st.items():o.hide_render=v
  c.location=pos;c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.type=kind
  if kind=='ORTHO':c.data.ortho_scale=size
  else:c.data.lens=size
  s.render.engine='BLENDER_EEVEE';s.render.resolution_x=1440;s.render.resolution_y=1080;s.render.resolution_percentage=100;s.render.filepath=str(R/(name+'.png'));bpy.ops.render.render(write_still=True)
  index+=1;(R/'status.txt').write_text('%d/%d'%(index,len(jobs)))
  if index<len(jobs):return .4
 except Exception:(R/'status.txt').write_text(traceback.format_exc())
 for o,v in st.items():o.hide_render=v
 c.location=cp;c.rotation_euler=cr;c.data.type=ct;c.data.ortho_scale=cz;c.data.lens=cl
 return None
bpy.app.timers.register(run,first_interval=.5)
