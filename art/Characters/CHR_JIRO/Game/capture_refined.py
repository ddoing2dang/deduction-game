
import bpy,json,traceback
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent;R=P/'Comparison';s=bpy.context.scene;c=s.camera
views=[('01_Front',(0,-4,.83),(0,0,.83),1.88,1000,1400),('02_ThreeQuarter',(2.8,-4,.88),(0,0,.83),1.88,1000,1400),('03_Side90',(4,0,.83),(0,0,.83),1.88,1000,1400),('04_Back',(0,4,.83),(0,0,.83),1.88,1000,1400),('05_FaceFront',(0,-4,1.445),(0,0,1.445),.50,1000,1100),('06_FaceThreeQuarter',(2,-4,1.445),(0,0,1.445),.50,1000,1100),('07_TwinRoot_Close',(4,-.4,1.56),(.12,.035,1.535),.31,1100,1000),('08_Head_Side',(4,0,1.50),(0,0,1.50),.40,1100,1100)]
def run():
 try:
  for name,loc,target,size,w,h in views:
   c.location=loc;c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.ortho_scale=size
   s.render.resolution_x=w;s.render.resolution_y=h;s.render.filepath=str(R/(name+'.png'));bpy.ops.render.render(write_still=True)
  c.location=(0,-4,.83);c.rotation_euler=(Vector((0,0,.83))-c.location).to_track_quat('-Z','Y').to_euler();c.data.ortho_scale=1.88;s.render.resolution_x=1000;s.render.resolution_y=1400
  bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_JIRO_GAME.blend'));(R/'capture-status.txt').write_text('CAPTURED; DIRECT REVIEW REQUIRED',encoding='utf-8')
 except Exception:(R/'capture-status.txt').write_text(traceback.format_exc(),encoding='utf-8')
 return None
bpy.app.timers.register(run,first_interval=.3);print('Eight current Jiro captures scheduled')
