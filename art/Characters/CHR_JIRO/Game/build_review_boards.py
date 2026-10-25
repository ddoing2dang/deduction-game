import bpy,numpy as np,json
from pathlib import Path
P=Path(__file__).parent;R=P/'Verification';C=P/'Comparison';s=bpy.context.scene
B=R/'Boards';B.mkdir(exist_ok=True)
def board(files,path,cols,cw,ch):
 rows=(len(files)+cols-1)//cols;canvas=np.ones((rows*ch,cols*cw,4),np.float32);canvas[:,:,:3]=.035
 for i,f in enumerate(files):
  assert f.exists(),str(f)
  im=bpy.data.images.load(str(f),check_existing=False);w,h=im.size;scale=min(cw/w,ch/h);nw,nh=int(w*scale),int(h*scale);im.scale(nw,nh)
  a=np.asarray(im.pixels[:],dtype=np.float32).reshape(nh,nw,4);x=i%cols*cw+(cw-nw)//2;y=(rows-1-i//cols)*ch+(ch-nh)//2
  canvas[y:y+nh,x:x+nw]=a;bpy.data.images.remove(im)
 im=bpy.data.images.new('ActualCaptureComparison',width=cols*cw,height=rows*ch,alpha=True);im.pixels.foreach_set(canvas.ravel());im.save_render(str(path),scene=s);bpy.data.images.remove(im)
sets={}
sets['Before_Refined_Unity_LODs']=[C/'Before_01_Front.png',C/'01_Front.png',R/'Preservation_LOD0.png',R/'Preservation_LOD2.png']
sets['Face_Before_Refined_Unity']=[C/'Before_05_FaceFront.png',C/'05_FaceFront.png',R/'Preservation_Face.png']
sets['Side_Before_After']=[C/'Before_03_Side90.png',C/'03_Side90.png',C/'Before_02_ThreeQuarter.png',C/'02_ThreeQuarter.png']
for k in ['Before_Refined_Unity_LODs','Side_Before_After']:board(sets[k],B/(k+'.png'),4,500,700)
board(sets['Face_Before_Refined_Unity'],B/'Face_Before_Refined_Unity.png',3,500,550)
for zoom in [9,19,32]:
 sets['Rooms_Zoom_'+str(zoom)]=[R/('Room_'+str(i)+'_Zoom_'+str(zoom)+'.png')for i in range(1,10)]
 board(sets['Rooms_Zoom_'+str(zoom)],B/('Rooms_Zoom_'+str(zoom)+'.png'),3,600,375)
 sets['Stairs_Zoom_'+str(zoom)]=[R/('Stair_'+door+'_Direction_'+str(d)+'_Zoom_'+str(zoom)+'.png')for door in ['DOOR_04_07','DOOR_05_08']for d in [0,1]]
 board(sets['Stairs_Zoom_'+str(zoom)],B/('Stairs_Zoom_'+str(zoom)+'.png'),2,800,500)
sets['Nine_States']=[R/('Action_'+n+'.png')for n in ['Idle','Move','Investigate','Suspect','Cooperate','Deduction','UseAbility','Win','Lose']]
board(sets['Nine_States'],B/'Nine_States.png',3,600,375)
sets['Expressions']=[R/('Face_'+n+'.png')for n in ['Neutral','Blink_L','Blink_R','Smile','Angry','Sad','Surprise','Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O']]
board(sets['Expressions'],B/'Expressions.png',4,480,300)
sets['ActionCamera']=[R/'Actual_ActionCamera.png',R/'Actual_ActionCamera_Return.png'];board(sets['ActionCamera'],B/'ActionCamera.png',2,800,500)
(P/'review-board-index.json').write_text(json.dumps({k:[str(f)for f in fs]for k,fs in sets.items()},indent=2))
print('Direct capture boards written',len(sets))
