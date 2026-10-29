import bpy,math,json
from mathutils import Vector
# Surface fit fixes use the actual newly imported support height.
lamp=bpy.data.objects['VG_R02_LibraryReadingLamp'];desk=bpy.data.objects['VG_R02_LibraryWritingDesk'];lamp.location.z=bbox([desk])[1].z
# Extend the simple CC0 banquet board between its ends while retaining carved leg width.
table=bpy.data.objects['VG_R04_DiningBanquetTable'];table.data=table.data.copy()
for v in table.data.vertices:
 x=v.co.x;v.co.x=x+math.copysign(.75*min(1,abs(x)/.32),x);v.co.y*=.68
for i in range(3):bpy.data.objects['VG_R04_DiningChair'+str(i)].location.y=floorbounds[4][0].y+.47
# Replace the servants' remaining locker block with a proper timber storage cabinet.
locker=bpy.data.objects['R09_Hero_Locker'];lp=locker.matrix_world.translation.copy();lp.z=-4
safe('GothicCommode_01',9,lp,'ServantDresser',height=.85,rotation=math.pi);retire(['R09_Hero_Locker']);retiregroup('R09_P02_Locker')
# Real ceramic vessels are placed on the measured kitchen shelves, not on movement space.
shelves=oldgroup('R05_P02_Shelves');lo,hi=bbox(shelves)
for i in range(3):
 p=Vector(((lo.x+hi.x)/2,lo.y+(hi.y-lo.y)*(i+1)/4,hi.z-.04))
 try:safe('antique_ceramic_vase_01',5,p,'KitchenCeramicJar'+str(i),height=.23,collision=False)
 except ValueError:pass
# Additional wall art and an oil lamp identify the cellar and boiler without placing props at doors.
for room in [7,8]:
 root=bpy.data.objects['REF_Room_%02d'%room].matrix_world.translation;fl,fh=floorbounds[room]
 try:safe('lantern_chandelier_01',room,(root.x,root.y,fl.z+2.85),'ServiceLantern',height=.65,collision=False)
 except ValueError:pass
lib.hide_render=True;lib.hide_viewport=True
for n,obj in [('asset-processing.json',asset_report),('placements.json',placements),('retired-placeholders.json',retired)]:(P/n).write_text(json.dumps(obj,indent=2))
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(P/'Mansion_VisualGold.blend'))
print('Extra fitted replacements',len(placements),'templates',len(cache))
