import bpy,numpy as np,json
from pathlib import Path
P=Path(__file__).parent/'Review'/'StraightEntrance';files=[]
for name in ['Front','Inside','HalfTop','LeftWall','RightWall','Door']:
 w,h,gap=960,720,16;a=np.empty((h,w*2+gap,4),np.float32);a[:]=(.02,.025,.035,1)
 for i,stage in enumerate(['Before','After']):
  im=bpy.data.images.load(str(P/stage/(name+'.png')),check_existing=False);im.reload();im.scale(w,h);a[:,i*(w+gap):i*(w+gap)+w]=np.array(im.pixels[:],np.float32).reshape(h,w,4);bpy.data.images.remove(im)
 out=P/('Compare_'+name+'.png');im=bpy.data.images.new('BeforeAfter',width=w*2+gap,height=h);im.pixels.foreach_set(a.ravel());im.filepath_raw=str(out);im.file_format='PNG';im.save();bpy.data.images.remove(im);files.append(str(out))
(P/'comparison-legend.json').write_text(json.dumps({'left':'Before, base ee4064f','right':'After, same camera and render dimensions; images resized only','images':files},indent=2));print('Six real-render comparisons, before left / after right')
