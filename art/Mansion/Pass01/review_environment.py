"""Contact sheets from unaltered camera captures for visual review."""
from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
root=Path(__file__).parent/'StructureAudit'
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
def sheet(folder,rows,columns,name):
 canvas=Image.new('RGB',(640*len(columns),430*len(rows)),(30,30,30));draw=ImageDraw.Draw(canvas)
 for row,room in enumerate(rows):
  for col,(label,pattern) in enumerate(columns):
   path=root/folder/pattern.format(room=room)
   if not path.exists():continue
   im=Image.open(path).convert('RGB');im.thumbnail((640,400))
   canvas.paste(im,(640*col,430*row+30));draw.text((640*col+8,430*row+5),f'Room {room} / {label}',font=font,fill='white')
 canvas.save(root/name)

sheet('After',list(range(1,10)),[(str(s),'Room_{room}_Zoom_'+str(s)+'.png') for s in [9,19,32]],'Unity-all-zooms.jpg')
sheet('BlenderAfter',list(range(1,10)),[(s,'Room_{room}_'+s+'.png') for s in ['Top','Front','Side','Iso']],'Blender-all-angles.jpg')


for start in [1,4,7]:
 sheet('Visual',list(range(start,start+3)),[(s,'Room_{room}_'+s+'.png') for s in ['Top','Front','Side','Iso']],f'Unity-angles-{start}.jpg')
 sheet('Visual',list(range(start,start+3)),[(str(s),'Room_{room}_Zoom_'+str(s)+'.png') for s in [9,19,32]],f'Unity-zooms-{start}.jpg')


for start in [1,4,7]:
 sheet('BlenderAfter',list(range(start,start+3)),[(s,'Room_{room}_'+s+'.png') for s in ['Top','Front','Side','Iso']],f'Blender-angles-{start}.jpg')

