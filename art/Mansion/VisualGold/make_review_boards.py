import bpy,numpy as np
from pathlib import Path
P=Path(__file__).parent
GLYPHS={'0':['111','101','101','101','111'],'1':['010','110','010','010','111'],'2':['111','001','111','100','111'],'3':['111','001','111','001','111'],'4':['101','101','111','001','001'],'5':['111','100','111','001','111'],'6':['111','100','111','101','111'],'7':['111','001','010','010','010'],'8':['111','101','111','101','111'],'9':['111','101','111','001','111']}
def board(files,out,w,h):
 gap=12;header=32;a=np.empty((3*(h+header)+gap*4,3*w+gap*4,4),np.float32);a[:]=(.018,.025,.035,1)
 for i,file in enumerate(files):
  im=bpy.data.images.load(str(file),check_existing=False);im.reload();im.scale(w,h);px=np.array(im.pixels[:],np.float32).reshape(h,w,4);y=gap+(2-i//3)*(h+header+gap);x=gap+(i%3)*(w+gap);a[y:y+h,x:x+w]=px;bpy.data.images.remove(im)
  for j,digit in enumerate('%02d'%(i+1)):
   for row,bits in enumerate(GLYPHS[digit]):
    for col,b in enumerate(bits):
     if b=='1':xx=x+10+j*18+col*4;yy=y+h+6+(4-row)*4;a[yy:yy+4,xx:xx+4]=(1,1,1,1)
 im=bpy.data.images.new('ReviewContact',width=a.shape[1],height=a.shape[0]);im.pixels.foreach_set(a.ravel());im.filepath_raw=str(out);im.file_format='PNG';im.save();bpy.data.images.remove(im)
 for cached in bpy.data.images:
  if cached.filepath and Path(bpy.path.abspath(cached.filepath))==out:cached.reload()
for zoom in [9,19,32,'Side']:
 files=[P/'Verification'/('SideRoom_%02d.png'%i if zoom=='Side'else'Room_%d_Zoom_%d.png'%(i,zoom))for i in range(1,10)]
 board(files,P/'Review'/('Unity_NineRooms_'+str(zoom)+'.png'),720,450)
board([P/'Review'/('Room_%02d_Gold.png'%i)for i in range(1,10)],P/'Review/Blender_NineRooms_Final.png',480,360)
print('Actual frames with room-number headers and gutters; scene pixels are only resized.')
