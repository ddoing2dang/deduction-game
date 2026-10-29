
import bpy,math,json,numpy as np
from pathlib import Path
from mathutils import Vector,Quaternion
from mathutils.bvhtree import BVHTree
P=Path(__file__).parent
exec(compile((P/'authoring_tools.py').read_text(),str(P/'authoring_tools.py'),'exec'),globals())
S=.96;s=bpy.context.scene;rig=bpy.data.objects['Armature']
ivory=material('Blouse','E7DCD1',.88);dark=material('Cardigan','2D272D',.90);rib=material('KnitRib','373940',.90)
forest=material('Forest_Ribbon','28252C',.78);edge=material('Ribbon_Light','C8A46B',.88);gold=material('Brass','D1A35B',.42,.35)
ink=material('Skirt','272D31',.88);leather=material('Loafers','25282D',.52);white=material('IvorySock','E0D8D0',.92);sock=material('CharcoalSock','34303A',.92)
def topweight(v):
 z=v.z/S;t=max(0,min(1,(z-1.07)/.18));return {'J_Bip_C_Spine':1-t,'J_Bip_C_Chest':t}
def M(name,vs,fs,mat,bone='J_Bip_C_Hips',weights=None,uv=None):
 return mesh(name,[tuple(Vector(p)*S)for p in vs],fs,mat,uv,bone,weights)
def edgepath(name,ps,rad,mat,bone):return band(name,[tuple(Vector(p)*S)for p in ps],rad*S,mat,bone)
def shell(name,rings,mat,n=56,theta0=0,theta1=math.tau,folds=False,backdrop=False):
 vs=[];fs=[];uv=[];closed=abs(theta1-theta0-math.tau)<1e-4;N=n if closed else n+1
 for j,(z,rx,ry)in enumerate(rings):
  t=j/(len(rings)-1)
  for k in range(N):
   aa=theta0+(theta1-theta0)*k/n
   pleat=(.002+.009*t)*math.cos(aa*16)if folds else .0012*math.cos(aa*8+.5*t)*max(0,1-t)
   x=(rx+pleat)*math.cos(aa);y=.014+(ry+pleat*.6)*math.sin(aa);zz=z
   if backdrop:zz-=.060*(max(0,math.sin(aa))**2)*(t**2)
   vs.append((x,y,zz));uv.append((k/n,t))
 for j in range(len(rings)-1):
  for k in range(n):a=j*N+k;b=j*N+(k+1)%N;fs.append((a,b,b+N,a+N))
 return M(name,vs,fs,mat,weights=topweight if not folds else None,uv=uv)
def panel(name,ps,mat,bone):
 # Small folded panels get thickness through an explicit shallow back, not a subdivision stack.
 vs=[tuple(p)for p in ps]+[(p[0],p[1]+.0015,p[2])for p in ps];n=len(ps)
 fs=[tuple(range(n)),tuple(reversed(range(n,2*n)))]
 fs +=[(i,(i+1)%n,(i+1)%n+n,i+n)for i in range(n)]
 return M(name,vs,fs,mat,bone=bone)
def bow(name,c,w,h,mat,bone,back=False):
 c=Vector(c);direction=1 if back else -1
 for side in [-1,1]:
  vs=[];fs=[];nr=6;nc=6
  for j in range(nr):
   t=j/(nr-1)
   for k in range(nc):
    u=k/(nc-1)*2-1;hh=h*(.15+.85*math.sin(t*math.pi*.76));p=c+Vector((side*w*t,direction*.008*math.sin(t*math.pi)*(1-u*u),u*hh))
    vs.append(tuple(p))
  for j in range(nr-1):
   for k in range(nc-1):a=j*nc+k;fs.append((a,a+1,a+nc+1,a+nc))
  M(name+'_Loop_'+str(side),vs,fs,mat,bone=bone)
 panel(name+'_Knot',[(c.x-.008,c.y-.004,c.z-.008),(c.x+.008,c.y-.004,c.z-.008),(c.x+.008,c.y-.005,c.z+.008),(c.x-.008,c.y-.005,c.z+.008)],mat,bone)
# Fitted shoulder-to-waist shirt, with gentle gathering rather than a straight block.
shirt=shell('Outfit_Blouse',[(1.022,.135,.126),(1.058,.138,.129),(1.124,.154,.150),(1.201,.171,.158),(1.275,.172,.136),(1.304,.137,.097),(1.334,.055,.047)],ivory,n=64)
panel('Outfit_Placket',[(-.008,-.109,1.036),(.008,-.109,1.036),(.009,-.143,1.243),(-.009,-.143,1.243)],ivory,'J_Bip_C_Chest')
for side,sign in [('L',1),('R',-1)]:
 panel('Outfit_Collar_'+side,[(sign*.008,-.040,1.333),(sign*.058,-.067,1.307),(sign*.043,-.122,1.252),(sign*.006,-.109,1.290)],ivory,'J_Bip_C_UpperChest')
 def armweights(v,side=side):
  t=max(0,min(1,(abs(v.x)/S-.285)/.085));return {'J_Bip_'+side+'_UpperArm':1-t,'J_Bip_'+side+'_LowerArm':t}
 def sleeve(name,profile,mat,n=28):
  vs=[];fs=[];uv=[]
  for j,(x,z,ry,rz)in enumerate(profile):
   for k in range(n):
    aa=math.tau*k/n;wrinkle=1+.035*math.sin(aa*5+j*.60)+.018*math.sin(aa*9-j*.8)
    vs.append((sign*x,.025+ry*math.cos(aa)*wrinkle,z+rz*math.sin(aa)*wrinkle));uv.append((k/n,j/(len(profile)-1)))
  for j in range(len(profile)-1):
   for k in range(n):a=j*n+k;b=j*n+(k+1)%n;fs.append((a,b,b+n,a+n))
  return M(name,vs,fs,mat,weights=armweights,uv=uv)
 sleeve('Outfit_ShirtPuff_'+side,[(.148,1.289,.047,.042),(.171,1.296,.056,.053),(.201,1.298,.065,.061),(.229,1.293,.063,.059),(.258,1.285,.054,.049),(.272,1.282,.048,.044)],ivory)
 # Dropped cuff and bunched sleeves: nonuniform radial and longitudinal folds.
 sleeve('Outfit_CardiganSleeve_'+side,[(.218,1.240,.060,.058),(.243,1.250,.071,.068),(.278,1.250,.080,.073),(.320,1.255,.083,.078),(.356,1.256,.075,.070),(.391,1.260,.091,.084),(.427,1.265,.088,.080),(.455,1.271,.073,.068),(.480,1.276,.085,.077),(.508,1.280,.074,.068),(.536,1.283,.061,.055),(.553,1.284,.054,.049)],dark)
 sleeve('Outfit_Cuff_'+side,[(.534,1.283,.060,.054),(.544,1.284,.058,.052),(.558,1.284,.055,.049),(.565,1.284,.054,.048)],rib,n=28)
 # Shoulder opening edge and limited cuff ribbing make the construction readable.
 for x,z,ry,rz in [(.243,1.25,.071,.068),(.555,1.284,.055,.049)]:
  edgepath('Outfit_RolledEdge_'+side+str(x),[(sign*x,.025+ry*math.cos(a),z+rz*math.sin(a))for a in np.linspace(0,math.tau,33)],.0028,rib,'J_Bip_'+side+('_UpperArm'if x<.3 else'_LowerArm'))
 # A gold strap detail close to the upper sleeve.
 edgepath('Accessory_SleeveChain_'+side,[(sign*.272,.025+.081*math.cos(a),1.250+.074*math.sin(a))for a in np.linspace(math.pi*.6,math.pi*1.55,17)],.0013,gold,'J_Bip_'+side+'_UpperArm')
# Fabric actually wraps behind the body with a curved dropped upper edge.
shell('Outfit_CardiganBack',[(.967,.164,.118),(1.000,.165,.126),(1.078,.173,.137),(1.160,.181,.129),(1.230,.183,.111)],dark,n=48,theta0=-.14,theta1=math.pi+.14,backdrop=True)
for sign in [-1,1]:
 vs=[];fs=[];nr=7;nc=5
 for j in range(nr):
  t=j/(nr-1)
  for k in range(nc):
   u=k/(nc-1);x=sign*(.136+.039*u+.007*math.sin(t*math.pi));z=.982+.210*t
   y=-.083*(1-u)-.014*u-.010*math.sin(t*math.pi)*math.sin(u*math.pi);vs.append((x,y,z))
 for j in range(nr-1):
  for k in range(nc-1):a=j*nc+k;fs.append((a,a+1,a+nc+1,a+nc))
 M('Outfit_CardiganFront_'+str(sign),vs,fs,dark,weights=topweight)
bow('Outfit_NeckBow',(0,-.123,1.286),.045,.020,forest,'J_Bip_C_UpperChest')
for sign in [-1,1]:
 ps=[(sign*.006,-.126,1.277),(sign*.024,-.127,1.275),(sign*.051,-.151,1.156),(sign*.031,-.151,1.143)]
 panel('Outfit_NeckRibbon_'+str(sign),ps,forest,'J_Bip_C_UpperChest')
 edgepath('Outfit_Ribbon_Edge_'+str(sign),[ps[0],ps[-1]],.0011,edge,'J_Bip_C_UpperChest')
# Green hem stripe and fine check grid are painted in UV space, not extruded geometry.
N=1024;yy,xx=np.mgrid[:N,:N];u=xx/(N-1);v=yy/(N-1);a=np.ones((N,N,4),np.float32);a[:,:,:3]=linear('2B3033')
vv=1-v
for lo,hi,c in [(.055,.075,'AC874E'),(.102,.118,'BA9D6D'),(.145,.150,'D9B97A')]:a[(vv>lo)&(vv<hi),:3]=linear(c)
lines=np.minimum((u*16)%1,1-(u*16)%1)<.012;a[lines,:3]=linear('4B4040')
im=bpy.data.images.new('MARIHI_Skirt_Check',width=N,height=N,alpha=True);im.colorspace_settings.name='Non-Color';im.pixels.foreach_set(a.ravel());im.filepath_raw=str(P/'MARIHI_Skirt_Check.png');im.save();im.pack()
sm=ink.copy();sm.name='MARIHI_PleatedCheck';tx=sm.node_tree.nodes.new('ShaderNodeTexImage');tx.image=im;bs=next(n for n in sm.node_tree.nodes if n.type=='BSDF_PRINCIPLED');sm.node_tree.links.new(tx.outputs[0],bs.inputs['Base Color'])
shell('Outfit_PleatedSkirt',[(1.025,.145,.134),(.994,.151,.136),(.950,.163,.138),(.881,.186,.144),(.803,.203,.150),(.777,.205,.150)],sm,n=96,folds=True)
shell('Outfit_Waistband',[(1.011,.146,.136),(1.039,.145,.135)],forest,n=64)
bow('Accessory_WaistBow',(0,-.127,1.032),.048,.018,forest,'J_Bip_C_Hips')
for sign in [-1,1]:
 panel('Accessory_WaistRibbon_'+str(sign),[(sign*.012,-.126,1.021),(sign*.033,-.126,1.021),(sign*.075,-.159,.822),(sign*.054,-.159,.814)],forest,'J_Bip_C_Hips')
 edgepath('Accessory_WaistRibbonTrim_'+str(sign),[(sign*.019,-.128,1.018),(sign*.060,-.162,.823)],.0011,edge,'J_Bip_C_Hips')
 edgepath('Accessory_WaistChain_'+str(sign),[(sign*(.023+.105*t),-.119+.029*t,1.023-.031*math.sin(math.pi*t))for t in np.linspace(0,1,20)],.0010,gold,'J_Bip_C_Hips')
bow('Accessory_BackBow',(0,.151,1.008),.085,.034,dark,'J_Bip_C_Hips',back=True)
for sign in [-1,1]:
 panel('Accessory_BackRibbon_'+str(sign),[(sign*.008,.154,.999),(sign*.026,.154,.997),(sign*.055,.167,.834),(sign*.037,.167,.823)],forest,'J_Bip_C_Hips')

# Front garment surfaces are derived from the shirt pattern; no detached plackets/ties.
def shirt_front(x,z):
 rings=[(1.022,.135,.126),(1.058,.138,.129),(1.124,.154,.150),(1.201,.171,.158),(1.275,.172,.136),(1.304,.137,.097),(1.334,.055,.047)]
 rx,ry=rings[0][1:]
 for (a,ax,ay),(b,bx,by)in zip(rings,rings[1:]):
  if a<=z<=b:
   q=(z-a)/(b-a);rx=ax*(1-q)+bx*q;ry=ay*(1-q)+by*q;break
 if z>rings[-1][0]:rx,ry=rings[-1][1:]
 return .014-ry*max(.05,1-(x/rx)**2)**.5
for ob in s.objects:
 if ob.type!='MESH' or not ob.name.startswith(('Outfit_Placket','Outfit_Collar','Outfit_NeckBow','Outfit_NeckRibbon','Outfit_Ribbon_Edge')):continue
 for vv in ob.data.vertices:
  x,y,z=vv.co/S
  extra=.004
  if 'Loop' in ob.name:extra+=.003*math.sin(min(1,abs(x)/.045)*math.pi)
  vv.co.y=(shirt_front(x,z)-extra)*S

# Small buttons are simple faceted beads, not subdivided boxes.
def bead(name,c,rad,mat,bone):
 vs=[];fs=[];n=10
 for z,rr in [(-.003,rad*.6),(0,rad),(.003,rad*.6)]:
  for k in range(n):aa=math.tau*k/n;vs.append((c[0]+rr*math.cos(aa),c[1]+z,c[2]+rr*math.sin(aa)))
 for j in range(2):
  for k in range(n):a=j*n+k;b=j*n+(k+1)%n;fs.append((a,b,b+n,a+n))
 fs +=[tuple(reversed(range(n))),tuple(2*n+k for k in range(n))]
 return M(name,vs,fs,mat,bone=bone)
for z in [1.112,1.179,1.241]:bead('Accessory_Button_'+str(z),(0,shirt_front(0,z)-.003,z),.0032,gold,'J_Bip_C_Chest')
# Strap fitted to skin. Use anatomical fallback only where the game mask removes hidden skin.
tree=BVHTree.FromObject(bpy.data.objects['Body'],bpy.context.evaluated_depsgraph_get());vs=[];fs=[];n=40
for z in [.705,.730]:
 for aa in np.linspace(0,math.tau,n,endpoint=False):
  d=Vector((math.cos(aa),math.sin(aa),0));h=tree.ray_cast(Vector((.077*S,0,z*S)),d,.16)[0];q=h/S+d*.002 if h is not None else Vector((.077+.062*d.x,.068*d.y,z));vs.append(tuple(q))
for k in range(n):fs.append((k,(k+1)%n,(k+1)%n+n,k+n))
M('Accessory_ThighStrap',vs,fs,leather,bone='J_Bip_L_UpperLeg')
def buckle(name,c,w,h,bone):
 x,y,z=c;edgepath(name,[(x-w/2,y,z-h/2),(x+w/2,y,z-h/2),(x+w/2,y,z+h/2),(x-w/2,y,z+h/2),(x-w/2,y,z-h/2)],.0019,gold,bone)
buckle('Accessory_ThighBuckle',(.090,-.069,.717),.026,.020,'J_Bip_L_UpperLeg')
# Socks follow noncircular calf sections, with a cuff instead of a straight tube.
for side,sign,top,mat in [('L',1,.235,sock),('R',-1,.276,white)]:
 vs=[];fs=[];n=24;rows=9
 for j in range(rows):
  t=j/(rows-1);z=.116+(top-.116)*t;calf=max(0,min(1,(z-.18)/.25));rx=.034+.017*calf;ry=.034+.021*calf
  center=.077+.002*math.sin(t*math.pi)
  for k in range(n):aa=math.tau*k/n;vs.append((sign*center+rx*math.cos(aa),.020+ry*math.sin(aa),z))
 for j in range(rows-1):
  for k in range(n):a=j*n+k;b=j*n+(k+1)%n;fs.append((a,b,b+n,a+n))
 M('Outfit_Sock_'+side,vs,fs,mat,bone='J_Bip_'+side+'_LowerLeg')
 for z in [top-.006,top-.015]:
  calf=max(0,min(1,(z-.18)/.25));edgepath('Outfit_SockCuff_'+side+str(z),[(sign*.077+(.034+.017*calf)*math.cos(a),.020+(.034+.021*calf)*math.sin(a),z)for a in np.linspace(0,math.tau,25)],.0018,mat,'J_Bip_'+side+'_LowerLeg')
 # Explicit loafer loft with a shaped toe, distinct heel and welt.
 vs=[];fs=[];n=20;profiles=[(-.153,.005,.009,.059),(-.142,.037,.022,.064),(-.117,.052,.031,.071),(-.071,.057,.038,.077),(-.020,.055,.041,.080),(.032,.047,.042,.080),(.075,.037,.035,.075),(.085,.004,.010,.065)]
 for y,rx,rz,z in profiles:
  for k in range(n):aa=math.tau*k/n;vs.append((sign*.077+rx*math.cos(aa),y,z+rz*math.sin(aa)))
 for j in range(len(profiles)-1):
  for k in range(n):a=j*n+k;b=j*n+(k+1)%n;fs.append((a,b,b+n,a+n))
 fs +=[tuple(reversed(range(n))),tuple((len(profiles)-1)*n+k for k in range(n))]
 M('Shoes_Loafer_'+side,vs,fs,leather,bone='J_Bip_'+side+'_Foot')
 # Rounded footprint sole, avoiding rectangular platform blocks.
 vs=[];fs=[];n=40
 for z in [.011,.032,.048]:
  for k in range(n):
   aa=math.tau*k/n;x=.063*math.cos(aa);y=-.034+.119*math.sin(aa);vs.append((sign*.077+x,y,z))
 for j in range(2):
  for k in range(n):a=j*n+k;b=j*n+(k+1)%n;fs.append((a,b,b+n,a+n))
 fs +=[tuple(reversed(range(n))),tuple(2*n+k for k in range(n))]
 M('Shoes_Welt_'+side,vs,fs,ink,bone='J_Bip_'+side+'_Foot')
 edgepath('Shoes_Vamp_'+side,[(sign*.077+.051*math.cos(a),-.032,.080+.041*math.sin(a))for a in np.linspace(0,math.pi,19)],.0060,leather,'J_Bip_'+side+'_Foot')
 buckle('Shoes_Buckle_'+side,(sign*.110,-.034,.113),.023,.017,'J_Bip_'+side+'_Foot')
# Comfortable inspection pose; underlying Humanoid T-pose remains in the rest rig.
rig.data.pose_position='POSE'
for side,sign in [('L',1),('R',-1)]:
 for part,ang in [('UpperArm',1.20),('LowerArm',.07)]:
  pb=rig.pose.bones['J_Bip_'+side+'_'+part];q=pb.bone.matrix_local.to_quaternion();pb.rotation_quaternion=q.inverted()@Quaternion((0,1,0),ang*sign)@q
bpy.context.view_layer.update()
c=s.camera;c.location=(0,-4,.83);c.rotation_euler=(Vector((0,0,.83))-c.location).to_track_quat('-Z','Y').to_euler();c.data.ortho_scale=1.88
s.render.resolution_x=900;s.render.resolution_y=1300;s.render.filepath=str(P/'Review/Full_First.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(P/'CHR_MARIHI.blend'))
print('New fitted blouse, draped sleeves, pleats and shaped shoes built; no subdivision modifiers')
