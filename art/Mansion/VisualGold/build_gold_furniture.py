import math,json,bpy
from mathutils import Vector
attempts=[]
def safe(slug,room,p,label,height=None,rotation=0,fit=None,wall=False,collision=True):
 p=Vector(p)
 for dx,dy in [(0,0),(.25,0),(-.25,0),(0,.25),(0,-.25),(.45,0),(-.45,0),(0,.45),(0,-.45),(.45,.25),(-.45,-.25),(.60,0),(-.60,0)]:
  try:return place(slug,room,p+Vector((dx,dy,0)),label,height,rotation,fit,wall,collision)
  except ValueError as e:attempts.append(str(e))
 raise ValueError('No safe replacement near '+label+'; '+attempts[-1])
def replacement(slug,group,label,height=None,rotation=0,fit=None,wall=False):
 p,d=anchor(group);room=int(group[1:3]);o=safe(slug,room,p,label,height,rotation,fit,wall,not wall);retiregroup(group);return o
replacement('ClassicConsole_01','R01_P02_Console','HallConsole',height=.89)
retiregroup('R01_P02_ConsoleLamp');safe('vintage_oil_lamp',1,(4.95,6.60,.89),'HallConsoleLamp',height=.65,collision=False)
p=bpy.data.objects['R01_Hero_Chandelier'].matrix_world.translation.copy();p.z=5.15
safe('Chandelier_01',1,p,'HallChandelier',height=1.7,collision=False)
retire([o.name for o in scene.objects if o.name.startswith('R01_Chandelier_')or o.name=='R01_Hero_Chandelier'])
replacement('ClassicConsole_01','R02_P02_Desk','LibraryWritingDesk',height=.86)
replacement('ArmChair_01','R02_P02_DeskChair','LibraryArmchair',height=.95)
replacement('vintage_oil_lamp','R02_P02_ReadingLamp','LibraryReadingLamp',height=.62)
p,d=anchor('R02_P02_Bookcase');p.z=0
safe('GothicCabinet_01',2,p,'LibraryBookCabinet',height=2.55,rotation=math.pi/2)
retiregroup('R02_P02_Bookcase');retire([o.name for o in scene.objects if o.name.startswith(('R02_Hero_Bookcase','R02_Hero_Bookshelf'))])
replacement('painted_wooden_sofa','R03_P02_Sofa','LoungeSofa',height=1.15,rotation=math.pi/2)
replacement('gothic_coffee_table','R03_P02_CoffeeTable','LoungeCarvedTable',fit=(.66,1.05,.54))
replacement('fancy_picture_frame_02','R03_P02_Portrait','LoungeGiltFrame',height=1.35,wall=True);retire(['R03_Hero_Portrait'])
p,d=anchor('R04_P02_DiningTable');table=safe('dining_table',4,p,'DiningBanquetTable',height=.82);retiregroup('R04_P02_DiningTable')
for i in range(3):
 g='R04_P02_DiningChair%d_-1'%i;p,d=anchor(g);p.y=max(p.y,floorbounds[4][0].y+.48)
 safe('ArmChair_01',4,p,'DiningChair'+str(i),height=.90);retiregroup(g);retiregroup('R04_P02_DiningChair%d_1'%i)
p=table.location.copy();p.z=2.75;safe('Chandelier_02',4,p,'DiningChandelier',height=.85,collision=False)
for i,slug in enumerate(['potted_plant_01','potted_plant_02','potted_plant_01']):
 g='R06_P02_Plant'+str(i);p,d=anchor(g);p.z=0;safe(slug,6,p,'ConservatoryPlant'+str(i),height=1.45 if i!=1 else 1.05);retiregroup(g)
for i in range(2):replacement('wine_barrel_01','R08_P02_Barrel'+str(i),'CellarOakBarrel'+str(i),height=1.1)
p=bpy.data.objects['R09_Hero_BedFrame'].matrix_world.translation.copy();p.z=-4
safe('GothicBed_01',9,p,'ServantTimberBed',height=1.25);retiregroup('R09_P02_Bed');retire(['R09_Hero_BedFrame','R09_Hero_Mattress'])
replacement('ClassicNightstand_01','R09_P02_Bedside','ServantNightstand',height=.66)
replacement('vintage_oil_lamp','R09_P02_BedsideLamp','ServantOilLamp',height=.55)
lib.hide_viewport=True;lib.hide_render=True
(P/'asset-processing.json').write_text(json.dumps(asset_report,indent=2));(P/'placements.json').write_text(json.dumps(placements,indent=2));(P/'retired-placeholders.json').write_text(json.dumps(retired,indent=2));(P/'placement-rejections.json').write_text(json.dumps(attempts,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'));print('Placed',len(placements),'retired',len(retired),'templates',len(cache))
