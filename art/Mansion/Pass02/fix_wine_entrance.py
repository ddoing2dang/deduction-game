"""Reproduce stage 1 in Blender MCP only; do not run after Pass02 art is built."""
import bpy,os,math,json
OUT=os.path.dirname(__file__)
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'Mansion_BeforePass02.blend'))
moved=[]
for o in bpy.context.scene.objects:
 if not o.name.startswith(('R08_Hero_WineRack_','R08_Hero_RackDivider_')):continue
 old=o.location.copy();dx=old.x+2.9;dy=old.y+5.4
 o.location=(1-dy,-3.45+dx,old.z);o.rotation_euler.z+=math.pi/2;o['StructureCollision']=True
 moved.append({'name':o.name,'before':list(old),'after':list(o.location)})
json.dump({'moved':moved,'deleted':[],'entriesChanged':False},open(os.path.join(OUT,'wine-fix.json'),'w'),indent=1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Mansion_EntranceFixed.blend'))
print('Moved',len(moved),'rack pieces from the west doorway to the north wall; no rail or stair change')
