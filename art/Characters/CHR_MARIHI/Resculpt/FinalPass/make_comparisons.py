from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
P=Path(__file__).parent;O=P/'Comparison';O.mkdir(exist_ok=True)
assert json.loads((P/'Before/capture-settings.json').read_text())==json.loads((P/'After/capture-settings.json').read_text()),'Camera/light/pose mismatch'
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',26)
for name in ['Face_Front','Face_45','Face_Side90','Full_Front','Full_45','Outfit_Close','Full_Back','Upper_Front','Upper_45','Upper_Side90']:
 a=Image.open(P/'Before'/(name+'.png')).convert('RGB');b=Image.open(P/'After'/(name+'.png')).convert('RGB');assert a.size==b.size
 out=Image.new('RGB',(a.width*2,a.height+46),(28,26,26));out.paste(a,(0,46));out.paste(b,(a.width,46));d=ImageDraw.Draw(out);d.text((15,8),'BEFORE',font=font,fill='white');d.text((a.width+15,8),'AFTER - USER REVIEW',font=font,fill='white');out.save(O/(name+'.png'))
names=['Neutral','Blink_L','Blink_R','Smile','Angry','Sad','Surprise','Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O']
for stage in ['Before','After']:
 out=Image.new('RGB',(1400,1170),(28,26,26));d=ImageDraw.Draw(out)
 for i,name in enumerate(names):
  im=Image.open(P/stage/('Expression_'+name+'.png'));im=im.resize((350,350));x=i%4*350;y=i//4*390;out.paste(im,(x,y));d.text((x+8,y+355),name,font=font,fill='white')
 out.save(O/('Expressions_'+stage+'.png'))
print('Seven before/after pairs; same cameras, lights, pose, scale verified')
