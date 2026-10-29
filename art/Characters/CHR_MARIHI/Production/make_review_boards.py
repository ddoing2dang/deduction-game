from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
P=Path(__file__).parent;R=P/'Game/Review';U=P/'Game/Verification';O=P/'Review';O.mkdir(exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
def board(out,items,cols=3,w=500,h=650):
 rows=(len(items)+cols-1)//cols;im=Image.new('RGB',(cols*w,rows*(h+40)),(32,30,31));d=ImageDraw.Draw(im)
 for i,(path,label)in enumerate(items):
  pic=Image.open(path).convert('RGB');pic.thumbnail((w,h));x=i%cols*w+(w-pic.width)//2;y=i//cols*(h+40);im.paste(pic,(x,y));d.text((i%cols*w+12,y+h+5),label,font=font,fill=(235,225,210))
 im.save(O/out)
board('Blender_Turnaround.png',[(R/(n+'.png'),label)for n,label in [('01_Front','Front'),('02_45','45 degrees'),('03_Side90','90 degree side'),('04_Back','Back'),('05_Face','Face'),('06_Face45','Face 45 degrees')]])
names=['Neutral','Blink_L','Blink_R','Smile','Angry','Sad','Surprise','Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O']
board('Blender_Expressions.png',[(R/('Expression_'+n+'.png'),n)for n in names],4,350,350)
board('Blender_Deformation.png',[(R/('Pose_'+n+'.png'),n)for n in ['T_Pose','Idle','Walk','Arms_Up','Arms_Forward','Knee_Bend','Sit']],4,380,480)
board('Blender_LODs.png',[(R/('LOD'+str(n)+'.png'),'LOD '+str(n))for n in range(3)],3,450,600)
if (U/'Face_Neutral.png').exists():
 board('Unity_Expressions.png',[(U/('Face_'+n+'.png'),n)for n in names],4,350,350)
 board('Unity_States.png',[(U/('Action_'+n+'.png'),n)for n in ['Idle','Move','Investigate','Suspect','Cooperate','Deduction','UseAbility','Win','Lose']],3,500,330)
 board('Unity_ThreeZooms.png',[(U/('Room_1_Zoom_'+str(n)+'.png'),'Actual main camera / zoom '+str(n))for n in [9,19,32]],1,1200,750)
 for zoom in [9,19,32]:
  board('Unity_Rooms_'+str(zoom)+'.png',[(U/('Room_'+str(n)+'_Zoom_'+str(zoom)+'.png'),'Room '+str(n)+' / '+str(zoom))for n in range(1,10)],3,600,375)
 board('Blender_Unity_Face.png',[(R/'05_Face.png','Blender candidate'),(U/'Preservation_Face.png','Unity same imported model')],2,650,700)
 board('Reference_Model.png',[(P.parent/'CHR_MARIHI_Reference.png','Authoritative design sheet'),(R/'01_Front.png','Actual Blender game model')],2,900,1200)
print('Contact sheets use only real stored model renders; reference is labelled; no AI concept imagery')
