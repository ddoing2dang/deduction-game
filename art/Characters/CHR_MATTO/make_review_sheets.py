"""Contact sheets of real Blender/Unity renders; no synthetic replacement images."""
from pathlib import Path
from PIL import Image, ImageDraw
HERE=Path(__file__).resolve().parent
ROOT=HERE/'Verification';OUT=ROOT/'Review';OUT.mkdir(parents=True,exist_ok=True)
def sheet(name,files,columns=3,width=460,height=300):
    rows=(len(files)+columns-1)//columns
    canvas=Image.new('RGB',(columns*width,rows*(height+30)),(30,30,36));draw=ImageDraw.Draw(canvas)
    for n,p in enumerate(files):
        image=Image.open(p).convert('RGB');image.thumbnail((width,height))
        x=n%columns*width;y=n//columns*(height+30)
        canvas.paste(image,(x+(width-image.width)//2,y+(height-image.height)//2))
        draw.text((x+8,y+height+7),p.stem,fill=(240,240,240))
    canvas.save(OUT/(name+'.jpg'),quality=92)
sheet('Blender_Turnaround',[ROOT/'Blender'/f'{n}.png' for n in ['Tpose','Front','Side','Back','Iso']],5,300,420)
sheet('Blender_Actions',sorted((ROOT/'Blender').glob('Action_*.png')),5,280,400)
sheet('Blender_Faces',sorted((ROOT/'Blender').glob('Face_*.png')),4,300,330)
sheet('Unity_Actions',sorted(ROOT.glob('Action_*.png')),3,600,375)
sheet('Unity_Faces',sorted(ROOT.glob('Face_*.png')),4,420,263)
for zoom in [9,19,32]:
    sheet(f'Unity_Zoom_{zoom}',[ROOT/f'Room_{r}_Zoom_{zoom}.png' for r in range(1,10)],3,600,375)
stairs=sorted(ROOT.glob('Stair_*_Zoom_9.png'))
if stairs:sheet('Unity_ServiceStairs',stairs,2,720,450)
print(OUT)
