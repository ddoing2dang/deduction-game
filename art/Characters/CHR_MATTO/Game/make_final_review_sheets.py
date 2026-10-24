import bpy,numpy as np,json
from pathlib import Path
P=Path(r'C:\UnityProjects\DeductionGame\deduction-game\art\Characters\CHR_MATTO\Game')
V=P/'Verification';B=V/'Blender';R=V/'Review';R.mkdir(exist_ok=True)
def read(path,w,h,crop=None):
    im=bpy.data.images.load(str(path),check_existing=False)
    if crop:
        W,H=im.size;a=np.array(im.pixels[:],dtype=np.float32).reshape(H,W,4)
        x0,y0,x1,y1=crop;a=a[H-y1:H-y0,x0:x1]
        tmp=bpy.data.images.new('ReviewCropTemp',width=a.shape[1],height=a.shape[0],alpha=True);tmp.pixels.foreach_set(a.ravel());bpy.data.images.remove(im);im=tmp
    im.scale(w,h);a=np.array(im.pixels[:],dtype=np.float32).reshape(h,w,4);bpy.data.images.remove(im);return a
def write(name,a):
    old=bpy.data.images.get(name)
    if old and old.users==0:bpy.data.images.remove(old)
    im=bpy.data.images.new(name,width=a.shape[1],height=a.shape[0],alpha=True);im.pixels.foreach_set(a.ravel());im.file_format='PNG';im.filepath_raw=str(R/(name+'.png'));im.save();bpy.data.images.remove(im)
def sheet(name,paths,cols,w,h):
    rows=[]
    for i in range(0,len(paths),cols):
        row=[read(p,w,h)for p in paths[i:i+cols]]
        while len(row)<cols:row.append(np.full((h,w,4),(.13,.12,.14,1),dtype=np.float32))
        rows.append(np.concatenate(row,axis=1))
    write(name,np.concatenate(rows[::-1],axis=0))
poses=['T_Pose','A_Pose','Idle','Walk','Arms_Up','Arms_Forward','Knee_Bend','Sit']
faces=['Neutral','Blink_L','Blink_R','Smile','Angry','Sad','Surprise','Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O']
sheet('Blender_Weights',[B/(n+'.png')for n in poses],4,420,420)
sheet('Blender_Actions',[B/('Action_'+n+'.png')for n in ['Idle','Walk','Investigate','Suspect','Deduction']],3,450,450)
sheet('Blender_Faces',[B/('Face_'+n+'.png')for n in faces],4,360,360)
sheet('Blender_LODs',[B/('LOD'+str(i)+'.png')for i in range(3)],3,450,450)
rows=[]
for level in [0,1,2]:
 row=[]
 for name in ['Neutral','Blink_L','Blink_R','Mouth_A']:
  path=B/('Face_'+name+'.png')if level==0 else B/('FaceLOD'+str(level)+'_'+name+'.png')
  row.append(read(path,360,360))
 rows.append(np.concatenate(row,axis=1))
write('Blender_Faces_All_LODs',np.concatenate(rows[::-1],axis=0))
result=(V/'play-results.txt').read_text(encoding='utf-8')if(V/'play-results.txt').exists()else''
ready='AUTOMATED CHECKS PASSED'in result and result.count('actual Matto mesh clearance at StandPoint ')==54
if ready:
    for zoom in [9,19,32]:sheet('Unity_Zoom_'+str(zoom),[V/('Room_'+str(i)+'_Zoom_'+str(zoom)+'.png')for i in range(1,10)],3,600,375)
    sheet('Unity_Actions',[V/('Action_'+n+'.png')for n in ['Idle','Move','Investigate','Suspect','Deduction','Cooperate','UseAbility','Win','Lose']],3,600,375)
    sheet('Unity_Faces',[V/('Face_'+n+'.png')for n in faces],4,480,300)
    sheet('Unity_LODs',[V/('Preservation_LOD'+str(i)+'.png')for i in range(3)],3,450,600)
    stairs=[V/('Stair_'+door+'_Direction_'+str(direction)+'_Zoom_9.png')for door in ['DOOR_04_07','DOOR_05_08']for direction in [0,1]]
    sheet('Unity_Service_Stairs',stairs,2,720,450)
    sheet('Unity_ActionPoints',[V/('ActionPoint_'+str(i)+'.png')for i in range(1,10)],3,600,375)
    sheet('Unity_ActionCamera',[V/'Actual_ActionCamera.png',V/'Actual_ActionCamera_Return.png'],2,720,450)
    pre=V/'Preservation'
    a=[read(pre/'Approved_Face.png',600,600),read(pre/'Game_Face.png',600,600),read(V/'Preservation_Face.png',600,600)]
    write('Approved_Blender_Unity_Face',np.concatenate(a,axis=1))
    a=[read(pre/'Approved_Full.png',450,600),read(pre/'Game_Full.png',450,600),read(V/'Preservation_LOD0.png',450,600)]
    write('Approved_Blender_Unity_Full',np.concatenate(a,axis=1))
    (R/'review-inventory.json').write_text(json.dumps({'poses':poses,'faces':faces,'unity_zoom_rooms':9,'zoom_levels':[9,19,32],'standpoints':54,'bidirectional_routes':22,'status':'READY FOR DIRECT VISUAL REVIEW'},indent=2))
print(json.dumps({'source_sheets_saved':4,'unity_sheets_ready':ready,'review_folder':str(R)}))
