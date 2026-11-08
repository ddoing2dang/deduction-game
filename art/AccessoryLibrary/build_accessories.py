#!/usr/bin/env python3
"""Build original low-poly anime clothing accessory GLBs. No third-party mesh data."""
from pathlib import Path
import json, math
import numpy as np
import trimesh

BASE=Path(__file__).parent
OUT=BASE/"glb"
OUT.mkdir(exist_ok=True)
P={
"G":([209,163,87,255],.80,.27),"g":([240,205,134,255],.68,.27),
"S":([166,176,194,255],.84,.27),"K":([35,35,44,255],.13,.48),
"R":([116,32,52,255],.05,.55),"I":([236,226,208,255],.03,.63),
"V":([125,87,177,255],.05,.50),"E":([64,154,132,255],.12,.27),
"B":([57,108,168,255],.12,.29)
}
INDEX=[]
class A:
 def __init__(self,name,category,desc):
  self.name=name;self.category=category;self.desc=desc
  self.scene=trimesh.Scene();self.n=0
 def put(self, mesh, code):
  rgba,m,r=P[code]
  mesh.visual=trimesh.visual.TextureVisuals(material=trimesh.visual.material.PBRMaterial(baseColorFactor=rgba,metallicFactor=m,roughnessFactor=r,doubleSided=True))
  self.n+=1
  self.scene.add_geometry(mesh,geom_name=f"{self.name}_{self.n}",node_name=f"{self.name}_{self.n}")
 def box(self,x,y,z,w,h,t,c):
  m=trimesh.creation.box(extents=(w,h,t))
  m.apply_translation((x,y,z));self.put(m,c)
 def disk(self,x,y,z,r,t,c):
  m=trimesh.creation.cylinder(radius=r,height=t,sections=24)
  m.apply_translation((x,y,z));self.put(m,c)
 def line(self,p,q,rad,c):
  d=np.array(q)-np.array(p)
  m=trimesh.creation.cylinder(radius=rad,height=float(np.linalg.norm(d)),sections=10)
  m.apply_transform(trimesh.geometry.align_vectors((0,0,1),d))
  m.apply_translation((np.array(p)+np.array(q))/2);self.put(m,c)
 def loop(self,path,rad,c,closed=True):
  a=np.array(path,dtype=float)
  verts=[];faces=[];n=len(a);s=8
  for i,p in enumerate(a):
   bef=a[(i-1)%n] if closed or i>0 else a[0]-(a[1]-a[0])
   aft=a[(i+1)%n] if closed or i<n-1 else a[-1]+a[-1]-a[-2]
   t=(aft-bef);t=t/np.linalg.norm(t)
   tmp=np.array([0.,0.,1.]) if abs(t[2])<.8 else np.array([1.,0.,0.])
   u=np.cross(t,tmp);u/=np.linalg.norm(u);v=np.cross(t,u)
   for k in range(s):verts.append(p+rad*(math.cos(k*2*math.pi/s)*u+math.sin(k*2*math.pi/s)*v))
  for i in range(n if closed else n-1):
   j=(i+1)%n
   for k in range(s):
    l=(k+1)%s
    faces.extend([[i*s+k,j*s+k,j*s+l],[i*s+k,j*s+l,i*s+l]])
  if not closed:
   start=len(verts);verts.append(a[0]);end=len(verts);verts.append(a[-1])
   for k in range(s):
    l=(k+1)%s;faces.extend([[start,l,k],[end,(n-1)*s+k,(n-1)*s+l]])
  self.put(trimesh.Trimesh(vertices=verts,faces=faces,process=True),c)
 def ring(self,x,y,rx,ry,r,c,z=.004):
  p=[(x+rx*math.cos(i*math.pi*2/32),y+ry*math.sin(i*math.pi*2/32),z) for i in range(32)]
  self.loop(p,r,c)
 def shape(self,p,c,z=.004,th=.004):
  points=np.array(p,float)
  center=points.mean(axis=0);n=len(points)
  vv=[(center[0],center[1],z-th/2),(center[0],center[1],z+th/2)]
  vv += [(x,y,z-th/2) for x,y in points]
  vv += [(x,y,z+th/2) for x,y in points]
  ff=[]
  for i in range(n):
   j=(i+1)%n
   ff += [[0,2+j,2+i],[1,2+n+i,2+n+j],[2+i,2+j,2+n+j],[2+i,2+n+j,2+n+i]]
  self.put(trimesh.Trimesh(vertices=vv,faces=ff,process=True),c)
 def finish(self):
  file=OUT/(self.name+".glb")
  self.scene.export(file)
  check=trimesh.load(file,force="scene")
  tris=sum(len(g.faces) for g in check.geometry.values())
  if tris<10:raise ValueError(self.name)
  INDEX.append({"name":self.name,"category":self.category,"description_ko":self.desc,"file":str(file.relative_to(BASE)).replace("\\","/"),"triangles":tris})
def obj(name,cat,desc):return A("ACC_"+name,cat,desc)
for metal,code in [("Gold","G"),("Silver","S"),("Black","K")]:
 a=obj("Buckle_"+metal,"belt","벨트/가터의 사각 버클")
 for y in [-.016,.016]:a.box(0,y,.003,.050,.005,.006,code)
 for x in [-.022,.022]:a.box(x,0,.003,.005,.034,.006,code)
 a.line((.021,0,.007),(-.006,0,.007),.0016,code);a.finish()
a=obj("Buckle_Oval","belt","타원 버클")
a.ring(0,0,.024,.017,.0025,"G");a.line((.020,0,.004),(-.005,0,.004),.002,"G");a.finish()
a=obj("Double_Clasp","belt","더블 스트랩 체결 장치")
for x in [-.016,.016]:a.ring(x,0,.012,.014,.0026,"S")
a.box(0,0,.002,.04,.004,.005,"K");a.disk(0,0,.006,.005,.003,"G");a.finish()
for name,color in [("Burgundy","R"),("Black","K"),("Ivory","I"),("Purple","V")]:
 a=obj("Bow_"+name,"ribbon","블라우스/치마/헤어 리본")
 a.shape([(-.004,0),(-.055,.024),(-.043,0),(-.055,-.023)],color)
 a.shape([(.004,0),(.055,.024),(.043,0),(.055,-.023)],color)
 a.shape([(-.007,-.003),(-.021,-.064),(-.001,-.050),(.006,-.003)],color)
 a.shape([(.007,-.003),(.021,-.064),(.033,-.050),(-.006,-.003)],color)
 a.box(0,0,.007,.017,.022,.009,"G" if color=="K" else color);a.finish()
a=obj("Ribbon_DoubleTail","ribbon","리본 긴 꼬리")
for s in [-1,1]:a.shape([(s*.006,.04),(s*.016,-.06),(s*.028,-.044),(s*.045,-.065),(s*.036,.041)],"R")
a.box(0,.038,.007,.05,.013,.006,"K");a.finish()
def heart(scale=1,shift_y=0):
 return [(.028*scale*16/17*(math.sin(2*math.pi*i/64)**3),shift_y+.028*scale*(13*math.cos(2*math.pi*i/64)-5*math.cos(4*math.pi*i/64)-2*math.cos(6*math.pi*i/64)-math.cos(8*math.pi*i/64))/17,.004) for i in range(64)]
a=obj("Heart_Pendant","pendant","골드 하트 참")
a.loop(heart(),.0024,"G");a.ring(0,.035,.005,.006,.0016,"G");a.finish()
a=obj("Heart_RedGem","pendant","붉은 하트 보석")
h=heart();a.loop(h,.0027,"G");a.shape([(x*.75,y*.75) for x,y,z in h],"R");a.finish()
a=obj("Star_Brooch","brooch","별 배지")
p=[]
for i in range(10):
 ang=math.pi/2+i*math.pi/5
 radius=.03 if i%2==0 else .013
 p.append((radius*math.cos(ang),radius*math.sin(ang)))
a.shape(p,"g");a.disk(0,0,.01,.005,.005,"R");a.finish()
a=obj("Cross_Charm","pendant","십자가 장식")
a.box(0,0,.004,.01,.058,.008,"S");a.box(0,.012,.004,.039,.009,.008,"S");a.disk(0,.012,.01,.004,.004,"V");a.finish()
a=obj("Crescent_Moon","pendant","초승달 장식")
points=[(.010*math.cos(-2+i*4/30)-.006,.019*math.sin(-2+i*4/30),.003) for i in range(31)]
a.loop(points,.0026,"g",False);a.finish()
a=obj("Butterfly_Pin","brooch","나비 핀 장식")
for s in [-1,1]:
 a.shape([(s*.003,.002),(s*.012,.030),(s*.039,.022),(s*.028,-.006)],"V")
 a.shape([(s*.005,-.002),(s*.024,-.010),(s*.027,-.030),(s*.008,-.019)],"V")
 a.disk(s*.024,.019,.009,.004,.003,"G")
a.box(0,0,.008,.006,.049,.006,"K");a.line((0,.021,.01),(-.011,.036,.01),.001,"G");a.line((0,.021,.01),(.011,.036,.01),.001,"G");a.finish()
a=obj("Flower_White","brooch","흰 꽃 장식")
for i in range(5):
 ang=2*math.pi*i/5
 a.disk(.021*math.sin(ang),.021*math.cos(ang),.003,.014,.004,"I")
a.disk(0,0,.009,.009,.006,"g");a.finish()
a=obj("Rose_Brooch","brooch","장미 핀")
for tier in range(3):
 for i in range(6-tier):
  ang=i*2*math.pi/(6-tier)
  rad=.021-tier*.006
  a.disk(rad*math.sin(ang),rad*math.cos(ang),tier*.003,.012-tier*.002,.003,"R")
a.disk(0,0,.010,.006,.005,"G");a.finish()
for name,c in [("Violet","V"),("Emerald","E")]:
 a=obj("Gem_Brooch_"+name,"brooch","보석 브로치")
 a.ring(0,0,.027,.028,.003,"G")
 a.shape([(0,.028),(.023,.005),(.017,-.019),(0,-.03),(-.017,-.019),(-.023,.005)],c,z=.009,th=.008);a.finish()
for name,c in [("Gold","G"),("Black","K")]:
 a=obj("Button_"+name,"button","버튼/소매 장식")
 a.disk(0,0,.002,.013,.004,c);a.ring(0,0,.012,.012,.0013,"g" if c=="K" else "S",z=.005)
 for x in [-.004,.004]:
  for y in [-.004,.004]:a.disk(x,y,.006,.0014,.001,"K")
 a.finish()
a=obj("Chain_Short","chain","연결 체인")
for i in range(9):a.ring((i-4)*.019,-.012*math.sin(i*math.pi/8),.011,.006,.0015,"G")
a.finish()
a=obj("Chain_Drape","chain","U형 드레이프 체인")
p=[((i/32-.5)*.18,-.055*(1-(2*i/32-1)**2),.004) for i in range(33)]
a.loop(p,.0017,"S",False)
for i in range(0,len(p),4):x,y,z=p[i];a.disk(x,y,z,.003,.004,"S")
a.finish()
a=obj("Pearl_Strand","chain","진주 스트랜드")
for i in range(11):
 t=i/10
 a.disk((t-.5)*.17,-.022*math.sin(math.pi*t),.003,.006,.009,"I")
a.finish()
a=obj("Key_Charm","pendant","열쇠 참")
a.ring(0,.019,.012,.012,.0028,"G")
a.box(0,-.021,.004,.006,.058,.007,"G")
a.box(.009,-.042,.004,.02,.006,.007,"G");a.box(.007,-.032,.004,.017,.005,.007,"G");a.finish()
a=obj("Lock_Charm","pendant","자물쇠 참")
a.box(0,-.01,.005,.039,.039,.009,"G");a.ring(0,.012,.014,.023,.0035,"S");a.disk(0,-.008,.011,.004,.003,"K");a.finish()
a=obj("Zipper_Pull","fastener","지퍼 손잡이")
a.box(0,.025,.003,.008,.019,.006,"K");a.ring(0,.012,.008,.008,.002,"S");a.shape([(-.011,.008),(.011,.008),(.012,-.031),(-.012,-.031)],"S");a.finish()
a=obj("Eyelet_Pair","fastener","작은 아일렛 쌍")
for x in [-.016,.016]:a.ring(x,0,.009,.009,.0025,"S")
a.finish()
a=obj("Stud_Triple","fastener","장식 스터드")
for x in [-.02,0,.02]:a.box(x,0,.003,.014,.014,.006,"S");a.disk(x,0,.007,.003,.003,"K")
a.finish()
a=obj("Crest_Badge","brooch","가슴 문장 배지")
a.shape([(-.025,.024),(.025,.024),(.023,-.008),(0,-.037),(-.023,-.008)],"K")
a.shape([(-.018,.018),(.018,.018),(.016,-.005),(0,-.027),(-.016,-.005)],"G",z=.010)
a.box(0,.003,.014,.008,.024,.003,"R");a.box(0,.005,.014,.027,.007,.003,"R");a.finish()
a=obj("Flame_Brooch","brooch","불꽃 펜던트")
a.shape([(0,.036),(.017,.008),(.013,-.013),(0,-.030),(-.017,-.011),(-.013,.011)],"G")
a.shape([(0,.019),(.008,0),(0,-.018),(-.008,0)],"R",z=.010);a.finish()
a=obj("Lace_Ruffle","trim","장식 레이스 띠")
p=[(-.085+i*.17/32,.008*math.sin(i*math.pi/2),.003) for i in range(33)]
a.loop(p,.0027,"I",False);a.box(0,.012,0,.18,.007,.004,"I");a.finish()
a=obj("Heart_Chain","chain","하트 참 달린 체인")
a.loop([(-.085,0,.004),(-.048,-.025,.004),(0,-.036,.004),(.048,-.025,.004),(.085,0,.004)],.0019,"G",False)
a.loop([(x*.55,y*.55-.037,z) for x,y,z in heart()],.0016,"g");a.finish()
index={"version":1,"units":"meters","orientation":"Y up and +Z forward; origin at accessory mounting point","count":len(INDEX),"assets":INDEX}
(BASE/"index.json").write_text(json.dumps(index,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("created",len(INDEX),"assets, total",sum(x.stat().st_size for x in OUT.glob("*.glb")),"bytes")
