import bpy,math,json
import numpy as np
from pathlib import Path
from mathutils import Vector,Quaternion
P=Path(__file__).parent
exec(compile((P/'authoring_tools.py').read_text(),str(P/'authoring_tools.py'),'exec'),globals())
s=bpy.context.scene;rig=bpy.data.objects['Armature'];tree=skin_tree()
for o in list(s.objects):
 if o.name.startswith(('Hair_','ACC_MARIHI_')):bpy.data.objects.remove(o,do_unlink=True)
blonde=material('ChampagneBlonde','E8BA78',.65);light=material('HairLit','F5D29B',.69);shade=material('HairShadow','BD8D53',.73)
black=material('SilkRibbon','28232A',.70);gold=material('RibbonPin','B68B49',.40,.45)
# Compact scalp-fitted crown, with new converging parting strips and no free hemisphere.
vs=[];fs=[];uv=[];nr=19;nc=64;origin=Vector((0,.018,1.43))
for j in range(nr+1):
 for k in range(nc):
  t=j/nr;th=math.tau*k/nc;back=(1-math.cos(th))*.5
  ph=.012+(math.radians(64+45*back-12*abs(math.sin(th))**3)-.012)*t
  d=Vector((math.sin(th)*math.sin(ph),-math.cos(th)*math.sin(ph),math.cos(ph)))
  h,n=scalp(origin+d*.18,tree);vs.append(tuple(h+n*.002));uv.append((k/nc,t))
for j in range(nr):
 for k in range(nc):a=j*nc+k;b=j*nc+(k+1)%nc;fs.append((a,b,b+nc,a+nc))
mesh('Hair_ScalpCrown',vs,fs,blonde,uv)
# Eight offset bangs: diagonal part, convex roots, narrowing uneven points.
bangs=[(-.033,-.111,1.451,.024),(-.024,-.080,1.433,.029),(-.012,-.048,1.450,.035),(.000,-.015,1.434,.034),(.012,.020,1.442,.034),(.024,.051,1.455,.032),(.036,.084,1.449,.028),(.046,.112,1.467,.021)]
for i,(xr,xe,ze,w)in enumerate(bangs):
 ps=[(xr,.009,1.614),(xr*1.1,-.05,1.577),(xe*.58,-.097,1.511),(xe*.91,-.108,1.472),(xe,-.108,ze)]
 leaf('Hair_Fringe_%02d'%i,ps,w,[blonde,light,blonde,blonde,light,blonde,blonde,shade][i],rows=24,thickness=.003,forehead=True)
anchors={}
for side,sign in [('L',1),('R',-1)]:
 h,n=scalp((sign*.127,.073,1.551),tree);anchors[side]=h+n*.0015
 # Cheek framing is substantially longer and slimmer than Jiro's short locks.
 h,n=scalp((sign*.126,-.032,1.538),tree)
 leaf('Hair_FaceFrame_'+side,[h+n*.0015,(sign*.140,-.055,1.487),(sign*.139,-.064,1.394),(sign*.119,-.079,1.292),(sign*.151,-.065,1.266)],.018,light,rows=26,thickness=.004,attach=True,twist=.35*sign)
 # Crown comb lines converge into the real tied root.
 for st in range(3):
  ps=[]
  for p in [(sign*(.010+st*.012),.008,1.616-st*.002),(sign*(.062+st*.012),.047,1.593-st*.010),(sign*.114,.075,1.570-st*.009),anchors[side]]:
   hh,nn=scalp(p,tree);ps.append(hh+nn*.0035)
  leaf('Hair_Sweep_'+side+str(st),ps,.016,[blonde,light,shade][st],rows=16,thickness=.002,attach=True,twist=.4)
# Three helper joints per tail, independent of the copied core and not Jiro helper names.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for side,sign in [('L',1),('R',-1)]:
 parent=rig.data.edit_bones['J_Bip_C_Head']
 for i,p in enumerate([anchors[side],Vector((sign*.223,.135,1.28)),Vector((sign*.245,.13,1.02))],1):
  b=rig.data.edit_bones.get('MARIHI_Tail_'+side+'_%02d'%i)or rig.data.edit_bones.new('MARIHI_Tail_'+side+'_%02d'%i)
  b.head=p;b.tail=p+Vector((0,0,-.19));b.parent=parent;parent=b
bpy.ops.object.mode_set(mode='OBJECT')
# Broad but thin swept clumps: waving depth, staggered lengths, independently curling tapered tips.
for side,sign in [('L',1),('R',-1)]:
 for st in range(6):
  root=anchors[side];h,n=scalp(root+Vector((0,(st-2.5)*.002,0)),tree);root=h+n*.0015
  phase=st*.65;outer=.205+st*.012;end=.79+(st%3)*.033
  ps=[root,root+Vector((sign*.018,.021,-.045)),(sign*(outer-.020),.09+st*.012,1.413),(sign*(outer+.030),.155+st*.008,1.251),(sign*(outer+.009),.122+st*.009,1.075),(sign*(outer+.023),.080+st*.009,.922),(sign*(outer-.002),.065+st*.005,end),(sign*(outer-.038),.080+st*.004,end-.023),(sign*(outer-.053),.097+st*.003,end+.016)]
  def weights(v,side=side):
   q=max(0,min(1.999,(1.49-v.z)/.26));i=int(q);w=q-i
   return {'MARIHI_Tail_'+side+'_%02d'%(i+1):1-w,'MARIHI_Tail_'+side+'_%02d'%(i+2):w}
  ob=leaf('Hair_WaveTail_'+side+'_%02d'%st,ps,[.050,.040,.043,.037,.036,.028][st],[blonde,light,blonde,shade,blonde,light][st],rows=34,thickness=.009 if st%2==0 else .006,weights=weights,attach=True,twist=sign*[.9,-1.2,.7,-1.1,1.3,-.7][st])
  ob['Design']='Champagne blonde layered flat S-wave with tapered curl; independent non-circular clump'
 # Folded black silk bow, mounted at the tie. Deliberately no flowers.
 c=anchors[side]+Vector((sign*.004,-.005,.003));normal=Vector((sign*.55,-.82,.10)).normalized();up=Vector((0,0,1));across=up.cross(normal).normalized()
 for wing in [-1,1]:
  vs=[];fs=[];nr=7;nt=7
  for j in range(nr):
   t=j/(nr-1)
   for k in range(nt):
    u=k/(nt-1)*2-1;p=c+across*(wing*.038*t)+up*(u*.024*(.15+.85*math.sin(t*math.pi*.70)))+normal*(.008*math.sin(t*math.pi)*(1-u*u));vs.append(p)
  for j in range(nr-1):
   for k in range(nt-1):a=j*nt+k;fs.append((a,a+1,a+nt+1,a+nt))
  mesh('ACC_MARIHI_RibbonLoop_'+side+str(wing),vs,fs,black)
  ps=[c+across*wing*.008,c+across*wing*.018+up*(-.04),c+across*wing*.031+up*(-.09),c+across*wing*.017+up*(-.125)]
  leaf('ACC_MARIHI_RibbonTail_'+side+str(wing),ps,.010,black,rows=12,thickness=.0018)
 band('ACC_MARIHI_RibbonKnot_'+side,[c+up*.008,c-up*.008],.006,black,'J_Bip_C_Head')
 # understated gold attachment pin follows scalp, not a floating emblem
 band('ACC_MARIHI_Pin_'+side,[c-up*.003-normal*.001,c+up*.003-normal*.001],.002,gold,'J_Bip_C_Head')
# A single slim cowlick rooted in the crown.
h,n=scalp((-.014,-.006,1.623),tree)
leaf('Hair_Cowlick',[h+n*.0015,(-.011,-.002,1.652),(.016,-.006,1.658),(.039,-.015,1.637)],.0025,light,rows=12,thickness=.001,attach=True)
(P/'hair-anchors.json').write_text(json.dumps({k:list(v)for k,v in anchors.items()},indent=2))
s.render.engine='BLENDER_EEVEE';s.view_settings.view_transform='Standard'
w=bpy.data.worlds.new('Marihi_Studio');w.use_nodes=True;s.world=w
bg=w.node_tree.nodes.get('Background');bg.inputs[0].default_value=(.075,.062,.060,1);bg.inputs[1].default_value=.4
for name,loc,power,size in [('Key',(-2.3,-3.0,3.6),320,3),('Fill',(2,-2,2),145,3),('Rim',(1.4,2.2,3.3),360,2.4)]:
 ld=bpy.data.lights.new('Marihi_'+name,'AREA');ld.energy=power;ld.shape='DISK';ld.size=size;o=bpy.data.objects.new(ld.name,ld);s.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1.2))-o.location).to_track_quat('-Z','Y').to_euler()
c=bpy.data.objects.new('Marihi_Camera',bpy.data.cameras.new('Marihi_Camera'));s.collection.objects.link(c);s.camera=c;c.data.type='ORTHO';c.data.ortho_scale=.52;c.location=(0,-4,1.45);c.rotation_euler=(Vector((0,0,1.45))-c.location).to_track_quat('-Z','Y').to_euler()
s.render.resolution_x=900;s.render.resolution_y=1000;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG'
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI.blend'),compress=True)
s.render.filepath=str(P/'Review/Head_Construction.png');bpy.ops.render.render(write_still=True)
print('New scalp-fitted blonde wave tails, asymmetrical fringe, fitted black bows; no finished character hair reused')
