"""Matto pilot authoring. Execute only in live Blender through MCP."""
import bpy, bmesh, math, json
from mathutils import Vector
from pathlib import Path
from math import sin, cos, pi
HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[2]
UNITY=PROJECT/'game/UnityProject/Assets/_Game/Characters/CHR_MATTO'
scene=bpy.context.scene
assert scene.objects.get('Armature'), 'Open the existing Matto rig project first'
rig=scene.objects['Armature'];root=scene.objects['CHR_MATTO'];meshroot=scene.objects['Mesh']
for obj in list(scene.objects):
    if obj.type in {'MESH','LIGHT','CAMERA'}:
        bpy.data.objects.remove(obj,do_unlink=True)
scene.name='CHR_MATTO'
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
root['source_reference']='Matto_Turnaround.png; upper turnaround, not SD strip'
root['body_height_m']=1.568;root['status']='Pilot in production; visual acceptance pending'

# One small shared colour/paint atlas. No external image-generation service.
COLORS=[(.96,.82,.78),(.985,.965,.96),(.73,.68,.73),(.045,.036,.052),
        (.15,.12,.16),(.34,.017,.045),(.6,.035,.075),(.64,.39,.19),
        (.98,.98,1),(.82,.10,.15),(.20,.015,.035),(.035,.012,.025),
        (.98,.67,.67),(.22,.18,.22),(.76,.7,.68),(.10,.07,.10)]
texdir=UNITY/'Textures';texdir.mkdir(parents=True,exist_ok=True)
image=bpy.data.images.get('MATTO_Atlas') or bpy.data.images.new('MATTO_Atlas',width=1024,height=1024)
import numpy as np
pixels=np.zeros((1024,1024,4),dtype=np.float32);pixels[:,:,3]=1
for k,col in enumerate(COLORS):
    x,y=k%4,k//4
    # Restrained painted gradient within each region, with safe padding.
    grad=np.linspace(.94,1.025,256)[:,None,None]
    pixels[y*256:(y+1)*256,x*256:(x+1)*256,:3]=np.minimum(1,np.array(col)[None,None,:]*grad)
image.pixels.foreach_set(pixels.ravel());image.filepath_raw=str(texdir/'MATTO_Atlas.png');image.file_format='PNG';image.save()
image.pack()
mat=bpy.data.materials.get('MAT_MATTO_Atlas') or bpy.data.materials.new('MAT_MATTO_Atlas')
mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear()
out=nodes.new('ShaderNodeOutputMaterial');bsdf=nodes.new('ShaderNodeBsdfPrincipled');tex=nodes.new('ShaderNodeTexImage');tex.image=image
bsdf.inputs['Roughness'].default_value=.9
mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color']);mat.node_tree.links.new(bsdf.outputs['BSDF'],out.inputs['Surface'])
mat.diffuse_color=(.8,.75,.76,1)

def mix(a,b,t):
    t=max(0,min(1,t));return {a:1-t,b:t} if a!=b else {a:1}
def torso(p):
    z=p[2]
    if z<.91:return {'Hips':1}
    if z<1.04:return mix('Spine','Chest',(z-.93)/.14)
    return mix('Chest','UpperChest',(z-1.07)/.14)
def arm(p,side):
    x=abs(p[0])
    if x<.36:return {side+'UpperArm':1}
    if x<.455:return mix(side+'UpperArm',side+'LowerArm',(x-.36)/.095)
    if x<.595:return {side+'LowerArm':1}
    return mix(side+'LowerArm',side+'Hand',(x-.595)/.05)
def leg(p,side):
    z=p[2]
    if z>.49:return {side+'UpperLeg':1}
    if z>.37:return mix(side+'LowerLeg',side+'UpperLeg',(z-.37)/.12)
    return {side+'LowerLeg':1}
def hairweight(p):
    z=p[2]
    if z>1.3:return {'Head':1}
    if z>1.12:return mix('Hair_Back_01','Head',(z-1.12)/.18)
    return mix('Hair_Back_02','Hair_Back_01',(z-.9)/.22)

class Geometry:
    def __init__(self,name,lod,density):
        self.name=name;self.lod=lod;self.d=density;self.v=[];self.f=[];self.uv=[];self.w=[];self.tags=[]
    def n(self,n,minimum=3):return max(minimum,round(n*self.d))
    def vertex(self,p,color=0,uv=(.5,.5),weight=None,tag=''):
        i=len(self.v);self.v.append(tuple(p));self.uv.append(((color%4+.08+.84*uv[0])/4,(color//4+.08+.84*uv[1])/4))
        self.w.append(weight(p) if callable(weight) else weight or {'Head':1});self.tags.append(tag);return i
    def quad(self,a,b,c,d):self.f.append((a,b,c,d))
    def rings(self,rings,segments=32,color=0,weight=None,tag='',axis='Z',pleat=0):
        n=self.n(segments,8);ids=[]
        for j,(center,rx,ry) in enumerate(rings):
            row=[]
            for k in range(n):
                t=2*pi*k/n;factor=1+pleat*cos(t*24)
                if axis=='X':p=(center[0],center[1]+rx*cos(t)*factor,center[2]+ry*sin(t)*factor)
                else:p=(center[0]+rx*cos(t)*factor,center[1]+ry*sin(t)*factor,center[2])
                row.append(self.vertex(p,color,(k/n,j/max(1,len(rings)-1)),weight,tag))
            ids.append(row)
        for j in range(len(ids)-1):
            for k in range(n):self.quad(ids[j][k],ids[j][(k+1)%n],ids[j+1][(k+1)%n],ids[j+1][k])
        self.f.extend([tuple(reversed(ids[0])),tuple(ids[-1])]);return ids
    def ellipsoid(self,center,scale,color=0,weight=None,segments=32,rings=16,tag=''):
        rs=[]
        nr=self.n(rings,6)
        for j in range(nr+1):
            t=pi*j/nr
            rs.append(((center[0],center[1],center[2]-scale[2]*cos(t)),max(.0001,scale[0]*sin(t)),max(.0001,scale[1]*sin(t))))
        return self.rings(rs,segments,color,weight,tag)
    def ribbon(self,points,width,color=7,weight=None,tag=''):
        # Smooth round cord / sculpted trim, deliberately few radial sides.
        if self.d<1 and len(points)>8:
            source=points;steps=max(3,round((len(source)-1)*self.d));points=[]
            for a in range(steps+1):
                t=a/steps*(len(source)-1);k=min(len(source)-2,int(t))
                points.append(tuple(Vector(source[k]).lerp(Vector(source[k+1]),t-k)))
        count=self.n(8,4);rows=[]
        for j,p in enumerate(points):
            tangent=Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])
            tangent.normalize();axis=tangent.cross(Vector((0,1,0)))
            if axis.length<.1:axis=tangent.cross(Vector((1,0,0)))
            axis.normalize();other=tangent.cross(axis).normalized();row=[]
            for k in range(count):
                q=Vector(p)+width*(cos(2*pi*k/count)*axis+sin(2*pi*k/count)*other)
                row.append(self.vertex(q,color,(k/count,j/max(1,len(points)-1)),weight,tag))
            rows.append(row)
        for j in range(len(rows)-1):
            for k in range(count):self.quad(rows[j][k],rows[j][(k+1)%count],rows[j+1][(k+1)%count],rows[j+1][k])
    def patch(self,points,color,weight,tag=''):
        if len(points)>3 and (Vector(points[0])-Vector(points[-1])).length<1e-8:points=points[:-1]
        minx=min(p[0] for p in points);minz=min(p[2] for p in points)
        sx=max(.0001,max(p[0] for p in points)-minx);sz=max(.0001,max(p[2] for p in points)-minz)
        ids=[self.vertex(p,color,((p[0]-minx)/sx,(p[2]-minz)/sz),weight,tag) for p in points]
        self.f.append(tuple(ids));return ids
    def finish(self):
        mesh=bpy.data.meshes.new(self.name+f'_LOD{self.lod}');mesh.from_pydata(self.v,[],self.f);mesh.update()
        bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.triangulate(bm,faces=list(bm.faces))
        bad=[f for f in bm.faces if f.calc_area()<1e-13]
        if bad:bmesh.ops.delete(bm,geom=bad,context='FACES_ONLY')
        bm.to_mesh(mesh);bm.free();mesh.update()
        ob=bpy.data.objects.new(self.name+f'_LOD{self.lod}',mesh);scene.collection.objects.link(ob);ob.parent=meshroot;ob.data.materials.append(mat)
        uv=mesh.uv_layers.new(name='UVMap')
        for loop in mesh.loops:uv.data[loop.index].uv=self.uv[loop.vertex_index]
        for p in mesh.polygons:p.use_smooth=True
        groups={name:ob.vertex_groups.new(name=name) for name in sorted({b for w in self.w for b in w})}
        for i,w in enumerate(self.w):
            for name,value in w.items():
                if value>1e-5:groups[name].add([i],value,'REPLACE')
        mod=ob.modifiers.new('Humanoid skin','ARMATURE');mod.object=rig
        ob['part']=self.name;ob['LOD']=self.lod
        if self.name=='Face':self.shapes(ob)
        return ob
    def shapes(self,ob):
        ob.shape_key_add(name='Basis')
        names=['Neutral','Smile','Happy','Suspicious','Angry','Surprised','Worried','Thinking','Smirk','Sad','Blink_L','Blink_R','EyeLook_L','EyeLook_R','EyeLook_Up','EyeLook_Down','Brow_Up','Brow_Down','Mouth_Open','Surprise','Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O']
        for name in names:
            key=ob.shape_key_add(name=name)
            key.value=0
            for i,(p,tag) in enumerate(zip(self.v,self.tags)):
                x,y,z=p;dx=dy=dz=0
                eye=tag.startswith('eye');brow=tag.startswith('brow');mouth=tag=='mouth';side='L' if x>0 else 'R'
                if eye and name=='Blink_'+side:dz=(1.43-z)*.97
                if eye and name.startswith('EyeLook') and 'iris' in tag:
                    dx={'EyeLook_L':.005,'EyeLook_R':-.005}.get(name,0);dz={'EyeLook_Up':.004,'EyeLook_Down':-.004}.get(name,0)
                if brow and name in ['Angry','Suspicious','Brow_Down']:dz=(abs(x)-.052)*.35-.003
                if brow and name in ['Sad','Worried']:dz=-(abs(x)-.052)*.35+.002
                if brow and name in ['Surprise','Surprised','Happy','Brow_Up']:dz=.008
                if mouth:
                    corner=min(1,abs(x)/.016)
                    if name in ['Smile','Happy','Smirk']:dz=.006*corner;dx=x*.18
                    if name in ['Sad','Worried']:dz=-.004*corner
                    if name in ['Mouth_Open','Mouth_A','Surprise','Surprised']:dz=(z-1.346)*4-.003
                    if name=='Mouth_I':dx=x*.4;dz=(z-1.346)*.4
                    if name in ['Mouth_U','Mouth_O']:dx=-x*.35;dz=(z-1.346)*2-.002
                    if name=='Mouth_E':dx=x*.25;dz=(z-1.346)*1.4
                    if name=='Thinking':dx=.004;dz=-.001
                    if name=='Angry':dz=-.002*corner
                key.data[i].co=(x+dx,y+dy,z+dz)

def interp_profile(profile,count):
    rows=[]
    for j in range(count+1):
        t=j/count*(len(profile)-1);k=min(len(profile)-2,int(t));a=t-k
        rows.append(tuple(profile[k][i]*(1-a)+profile[k+1][i]*a for i in range(len(profile[0]))))
    return rows

all_objects=[]
for lod,density in enumerate([.62,.395,.21]):
    G={name:Geometry(name,lod,density) for name in ['Body','Face','Hair_Main','Outfit','ACC_MATTO_Beret','Shoes','Accessory']}
    body,face,hair,outfit,beret,shoes,acc=G.values()
    # Slim limbs with blended knee/elbow rings, separated hidden torso surfaces removed.
    body.rings([((0,0,z),r,r*.82) for z,r in [(1.19,.045),(1.225,.039),(1.27,.035),(1.30,.042)]],32,0,{'Neck':1})
    for sign,side in [(1,'Left'),(-1,'Right')]:
        prof=interp_profile([(.175,.034),(.25,.039),(.4,.032),(.51,.027),(.625,.021)],body.n(20,10))
        body.rings([((sign*x,0,1.205),r,r) for x,r in prof],32,0,lambda p,s=side:arm(p,s),axis='X')
        body.ellipsoid((sign*.662,0,1.205),(.049,.033,.018),0,{side+'Hand':1},32,12)
        for finger,y,length in [('Thumb',-.029,.049),('Index',-.021,.063),('Middle',-.005,.069),('Ring',.012,.064),('Little',.027,.05)]:
            start=.659 if finger=='Thumb' else .691
            count=body.n(9,6);rows=[]
            for k in range(count+1):
                t=k/count;r=(.007 if finger!='Little' else .006)*(1-.7*t**4)
                rows.append(((sign*(start+length*t),y-(.036*t if finger=='Thumb' else 0),1.205),r,r))
            def finger_weight(p,s=side,f=finger,start=start,length=length):
                t=(abs(p[0])-start)/length
                a,b=('Proximal','Intermediate') if t<.66 else ('Intermediate','Distal')
                return mix(s+f+a,s+f+b,(t-(.15 if t<.66 else .66))/.3)
            body.rings(rows,12,0,finger_weight,axis='X')
        prof=interp_profile([(.17,.032),(.25,.041),(.37,.042),(.43,.037),(.55,.051),(.68,.061),(.8,.067)],body.n(26,12))
        body.rings([((sign*.079,-.008*max(0,1-abs(z-.43)/.18),z),r,r*.91) for z,r in prof],32,0,lambda p,s=side:leg(p,s))
        # Detached, gathered sleeves preserve bare shoulder silhouette.
        prof=interp_profile([(.225,.045),(.25,.063),(.30,.075),(.39,.061),(.46,.078),(.54,.084),(.59,.044),(.615,.038)],outfit.n(24,12))
        outfit.rings([((sign*x,0,1.205),r,r*.94) for x,r in prof],32,3,lambda p,s=side:arm(p,s),axis='X')
        for x,r in [(.24,.062),(.60,.041)]:
            acc.rings([((sign*(x+a),0,1.205),r,r) for a in [-.009,.009]],24,7,lambda p,s=side:arm(p,s),axis='X')
        # Ankle socks / platform ankle boots, asymmetry matches turnaround.
        shoes.rings([((sign*.079,0,z),r,r*.9) for z,r in [(.155,.036),(.175,.038),(.215,.035),(.235,.034)]],32,1 if sign==1 else 3,{side+'LowerLeg':1})
        shoes.rings([((sign*.079,-.042,z),rx,ry) for z,rx,ry in [(.006,.053,.116),(.025,.056,.119),(.045,.055,.117),(.065,.053,.11),(.095,.049,.091),(.125,.036,.048),(.17,.038,.044)]],40,3,{side+'Foot':1})
        for z in [.028,.047]:
            acc.ribbon([(sign*.079+.055*cos(t),-.042+.118*sin(t),z) for t in np.linspace(0,2*pi,41)],.0018,7,{side+'Foot':1})
        for z,y in [(.09,-.105),(.14,-.035)]:
            yy=-.085 if z<.1 else -.044;curve=.055 if z<.1 else .047;zz=.098 if z<.1 else .154
            acc.ribbon([(sign*.079+xx,yy-curve*(1-(xx/.05)**2),zz+.009*(1-(xx/.05)**2)) for xx in [-.045,-.025,0,.025,.045]],.006,4,{side+'Foot':1})
            acc.ribbon([(sign*.079+xx,yy-curve-.004,zz+.009+dz) for xx,dz in [(-.014,-.009),(.014,-.009),(.014,.009),(-.014,.009),(-.014,-.009)]],.0024,7,{side+'Foot':1})
        acc.patch([(sign*.126,.007,.155),(sign*.15,.005,.181),(sign*.153,.002,.14),(sign*.126,-.001,.155)],5,{side+'Foot':1})
        acc.patch([(sign*.127,0,.154),(sign*.146,-.002,.142),(sign*.142,-.004,.087),(sign*.13,-.003,.095)],5,{side+'Foot':1})
    # Head with broad anime facial plane, sculpted jaw and restrained nose.
    nr=face.n(32,16);ns=face.n(64,24);ids=[]
    for j in range(nr+1):
        z=1.288+.28*j/nr;t=j/nr
        rx=.132*(sin(pi*t)**.42)*(.9+.1*min(1,t*3));ry=.108*(sin(pi*t)**.65)
        row=[]
        for k in range(ns):
            ph=2*pi*k/ns;x=rx*cos(ph);y=ry*sin(ph)
            if y<0:y=-ry*abs(sin(ph))**.30-.004*math.exp(-(x/.016)**2-((z-1.384)/.018)**2)
            row.append(face.vertex((x,y,z),0,(k/ns,t),{'Head':1},'head'))
        ids.append(row)
    for j in range(nr):
        for k in range(ns):face.quad(ids[j][k],ids[j][(k+1)%ns],ids[j+1][(k+1)%ns],ids[j+1][k])
    for sign in [-1,1]:
        face.ellipsoid((sign*.122,-.001,1.405),(.017,.018,.032),0,{'Head':1},20,12)
        # Curved almond eye shell. Slight convexity provides real 3D eye highlights.
        cx=sign*.052;cz=1.432;steps=face.n(32,16);rows=[]
        for j in range(5):
            v=j/4;row=[]
            for k in range(steps+1):
                u=-1+2*k/steps;h=.018*(max(0,1-u*u)**.62)
                z=cz+((v-.5)*2)*h*(.72 if v>.5 else .85)+sign*u*.003
                row.append(face.vertex((cx+u*.036,-.109-.009*(1-u*u)*sin(pi*v),z),8,(k/steps,v),{'Head':1},'eye_white'))
            rows.append(row)
        for j in range(4):
            for k in range(steps):face.quad(rows[j][k],rows[j][k+1],rows[j+1][k+1],rows[j+1][k])
        # Iris rings with red gradient, dark pupil and two painted-style highlights.
        for radius,color,depth in [(1,10,-.120),(.84,9,-.121),(.38,11,-.122)]:
            iris=face.ellipsoid((cx,depth,cz),(.017*radius,.001,.016*radius),color,{'Head':1},24,8,'eye_iris')
            for row in iris:
                for n in row:
                    x,y,z=face.v[n];u=(x-cx)/.036;h=.018*max(0,1-u*u)**.62
                    face.v[n]=(x,y,min(cz+h*.72+sign*u*.003,max(cz-h*.85+sign*u*.003,z)))
        face.ellipsoid((cx-.004,-.124,cz+.008),(.004,.001,.0045),8,{'Head':1},16,8,'eye_iris')
        face.ellipsoid((cx+.005,-.124,cz-.007),(.0016,.001,.002),8,{'Head':1},12,6,'eye_iris')
        for upper in [True,False]:
            points=[]
            for u in np.linspace(-1,1,steps+1):
                h=.018*max(0,1-u*u)**.62;points.append((cx+u*.036,-.120,cz+(h*.72 if upper else -h*.85)+sign*u*.003))
            face.ribbon(points,.0023 if upper else .00045,11,{'Head':1},'eye_lash')
        face.ribbon([(cx+u*.032,-.109,1.467+.006*(1-u*u)) for u in np.linspace(-1,1,13)],.0018,2,{'Head':1},'brow')
        face.ellipsoid((sign*.075,-.108,1.39),(.014,.0006,.004),12,{'Head':1},20,8,'cheek')
    face.ellipsoid((0,-.084,1.346),(.013,.001,.0015),10,{'Head':1},24,6,'mouth')
    # Blouse/corset and collar. Body under opaque clothing is intentionally omitted.
    profile=interp_profile([(.88,.112,.075),(.94,.111,.074),(1.04,.13,.082),(1.12,.145,.09),(1.18,.139,.081),(1.205,.085,.054)],outfit.n(20,10))
    outfit.rings([((0,0,z),x,y) for z,x,y in profile],48,1,torso)
    outfit.rings([((0,0,z),.043,.037) for z in [1.222,1.24,1.265]],32,1,{'Neck':1})
    # Black fitted side panels; front white bib remains readable beneath bow.
    for sign in [-1,1]:
        outfit.patch([(sign*.055,-.078,.92),(sign*.12,-.045,.92),(sign*.137,-.04,1.12),(sign*.075,-.08,1.14)],3,torso)
        outfit.patch([(sign*.012,-.049,1.261),(sign*.052,-.06,1.22),(sign*.035,-.092,1.177),(sign*.005,-.084,1.225)],1,{'UpperChest':1})
    for z in [.97,1.02,1.07]:acc.ellipsoid((0,-.086,z),(.004,.003,.004),7,torso,12,6)
    # Short pleated skirt, actual fold silhouette and lined hem.
    rings=[]
    count=outfit.n(12,7)
    for j in range(count+1):
        t=j/count;z=.925-.255*t;rings.append(((0,0,z),.114+.104*t,.08+.065*t))
    outfit.rings(rings,96,3,{'Hips':1},pleat=.033)
    outfit.rings([((0,0,z),.115,.082) for z in [.918,.944]],48,4,{'Hips':1})
    for sign in [-1,1]:
        for x in [.115,.145]:
            outfit.patch([(sign*x,-.125,.685),(sign*(x+.019),-.12,.685),(sign*(x*.65+.012),-.078,.9),(sign*x*.65,-.08,.9)],5,{'Hips':1})
        acc.ribbon([(sign*(.03+.16*t),-.085-.06*t,.913-.23*t) for t in np.linspace(0,1,15)],.0016,7,{'Hips':1})
    for z in [.683,.698]:
        acc.ribbon([(.217*cos(t),.146*sin(t),z) for t in np.linspace(0,2*pi,97)],.002,7,{'Hips':1})
    # Large neck bow: folded, rounded lobes and long tails, not a flat card.
    for sign in [-1,1]:
        pts=[(sign*.009,-.101,1.19),(sign*.065,-.098,1.229),(sign*.085,-.10,1.208),(sign*.061,-.119,1.17),(sign*.009,-.113,1.19)]
        outfit.patch(pts,5,{'UpperChest':1});outfit.ribbon(pts,.002,6,{'UpperChest':1})
        outfit.patch([(sign*.008,-.104,1.19),(sign*.043,-.108,1.16),(sign*.06,-.104,1.075),(sign*.03,-.11,1.09)],5,{'UpperChest':1})
        acc.ribbon([(sign*.032,-.112,1.14),(sign*.044,-.109,1.09)],.0012,7,{'UpperChest':1})
    acc.ellipsoid((0,-.119,1.193),(.014,.006,.018),7,{'UpperChest':1},24,12)
    acc.ellipsoid((0,-.125,1.193),(.007,.003,.01),5,{'UpperChest':1},20,10)
    # Reference's thigh harness and heart pendant.
    acc.rings([((.079,0,z),.064,.06) for z in [.625,.647]],32,3,{'LeftUpperLeg':1})
    acc.ribbon([(.122,-.041,.623),(.153,-.041,.623),(.153,-.041,.649),(.122,-.041,.649),(.122,-.041,.623)],.0026,7,{'LeftUpperLeg':1})
    heart=[]
    for t in np.linspace(0,2*pi,49):
        heart.append((.125+.0014*16*sin(t)**3,-.139,.79+.0014*(13*cos(t)-5*cos(2*t)-2*cos(3*t)-cos(4*t))))
    acc.ribbon(heart,.0022,7,{'Hips':1});acc.ribbon([(.125,-.14,.81),(.12,-.11,.9)],.0015,7,{'Hips':1})
    # Hair cap covers only crown/back. Facial opening remains clear.
    nr=hair.n(18,9);ns=hair.n(64,24);ids=[]
    for j in range(nr+1):
        row=[]
        for k in range(ns):
            ph=2*pi*k/ns;front=max(0,-sin(ph));bottom=1.29+.20*front
            z=bottom+(1.584-bottom)*j/nr;t=(z-1.295)/.29;r=max(.003,.14*(max(0,sin(pi*min(.997,t)))**.5))
            row.append(hair.vertex((r*cos(ph),r*.87*sin(ph)+.013,z),1,(k/ns,j/nr),{'Head':1}))
        ids.append(row)
    for j in range(nr):
        for k in range(ns):hair.quad(ids[j][k],ids[j][(k+1)%ns],ids[j+1][(k+1)%ns],ids[j+1][k])
    def lock(points,width,depth,weight,color=1):
        # Broad sculpted locks: bounded number, taper and wave at the ends.
        rows=[];n=hair.n(18,8)
        for j in range(n+1):
            t=j/n;q=t*(len(points)-1);k=min(len(points)-2,int(q));a=q-k
            p0=Vector(points[max(0,k-1)]);p1=Vector(points[k]);p2=Vector(points[k+1]);p3=Vector(points[min(len(points)-1,k+2)])
            p=.5*((2*p1)+(-p0+p2)*a+(2*p0-5*p1+4*p2-p3)*a*a+(-p0+3*p1-3*p2+p3)*a*a*a)
            taper=max(.025,(sin(pi*(.12+.88*t)))**.62)
            rows.append((tuple(p),max(.0005,width*taper),max(.0004,depth*taper)))
        hair.rings(rows,16,color,weight)
    back_count=11 if lod<2 else 7
    for i in range(back_count):
        t=-1+2*i/(back_count-1);x=t*.125;y=.065+.055*(1-t*t)
        lock([(x*.8,y*.75,1.55),(x*1.10,y,1.34),(x*1.23,y+.035,1.12),(x*1.5,y+.008,.94),(x*1.12,y-.018,.84+.035*(i%3))],.032 if lod<2 else .045,.017,hairweight,1)
    for sign in [-1,1]:
        for i in range(3):
            x=sign*(.11+i*.011);y=-.055+i*.02
            lock([(sign*.10,-.035,1.53),(x,-.08,1.37),(x*1.26,-.08,1.19),(x*1.42,-.055,1.05),(x*1.10,-.07,.94+.03*i)],.026,.010,{'Head':1})
    for i in range(7):
        x=(i-3)*.032;endz=1.445+.025*(i%3==0)+.014*abs(i-3)/3
        lock([(x*.7,-.06,1.568),(x,-.11,1.53),(x+.01,-.12,endz+.023),(x+.018,-.12,endz)],.025,.005,{'Head':1})
    # Tilted soft beret with band, crown and circular brass insignia.
    nr=beret.n(14,8);rows=[]
    for j in range(nr+1):
        t=j/nr;z=1.555+.098*t;rad=.145*(sin(pi*(.12+.88*t)))**.45
        rows.append(((.016-.025*t,.015,z),max(.002,rad*1.12),max(.002,rad*.87)))
    beret.rings(rows,64,3,{'Head':1})
    beret.rings([((0,.012,z),.133,.111) for z in [1.549,1.57]],64,4,{'Head':1})
    def crest(x,z):
        t=max(0,min(1,(z-1.555)/.098));rad=.145*sin(pi*(.12+.88*t))**.45
        front=.015-rad*.87*max(0,1-((x-(.016-.025*t))/(rad*1.12))**2)**.5
        return (x,front-.004,z)
    acc.ribbon([crest(.1+.014*cos(t),1.582+.014*sin(t)) for t in np.linspace(0,2*pi,33)],.002,7,{'Head':1})
    acc.ribbon([crest(.1+.01*sin(t),1.582+.01*cos(t)) for t in np.linspace(0,4*pi,17)],.001,7,{'Head':1})
    # Sleeve ties and beret ribbons; weighted to the relevant parent bone.
    for sign,side in [(1,'Left'),(-1,'Right')]:
        for yy in [-1,1]:
            outfit.patch([(sign*.285,-.069,1.205),(sign*(.27+yy*.025),-.078,1.245),(sign*(.30+yy*.025),-.08,1.24)],5,{side+'UpperArm':1})
        outfit.patch([(sign*.12,.072,1.56),(sign*.147,.07,1.53),(sign*.153,.065,1.44),(sign*.132,.063,1.457)],3,{'Head':1})
    objects=[g.finish() for g in G.values()];all_objects+=objects
    if lod==2:
        for ob in objects:
            if ob.get('part')=='Face':continue
            bpy.context.view_layer.objects.active=ob;ob.select_set(True)
            ob.modifiers.clear()
            simplify=ob.modifiers.new('Distant silhouette reduction','DECIMATE');simplify.ratio=.4;simplify.use_collapse_triangulate=True
            bpy.ops.object.modifier_apply(modifier=simplify.name)
            skin=ob.modifiers.new('Humanoid skin','ARMATURE');skin.object=rig
            ob.select_set(False)
    for ob in objects:ob.hide_set(lod!=0);ob.hide_render=lod!=0

# Authoring lights/camera are excluded from FBX.
world=scene.world or bpy.data.worlds.new('Matto Studio');scene.world=world;world.use_nodes=True
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.35,.35,.38,1)
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[1].default_value=.65
for name,loc,power,size in [('Key',(-2,-4,4),350,4),('Fill',(3,-2,2),180,3),('Rim',(0,2,3),250,3)]:
    light=bpy.data.lights.new(name,'AREA');light.energy=power;light.shape='DISK';light.size=size
    ob=bpy.data.objects.new(name,light);scene.collection.objects.link(ob);ob.location=loc;ob.rotation_euler=(Vector((0,0,1))-ob.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('PilotReviewCamera');cam=bpy.data.objects.new('PilotReviewCamera',camdata);scene.collection.objects.link(cam);scene.camera=cam
cam.location=(2,-5,2.1);cam.rotation_euler=(Vector((0,0,.84))-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=1.95
try:scene.render.engine='BLENDER_EEVEE'
except TypeError:pass
scene.render.resolution_x=1100;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.view_settings.view_transform='Standard'
for o in scene.objects:o.select_set(False)
root.select_set(True);bpy.context.view_layer.objects.active=root
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'CHR_MATTO.blend'))
stats=[]
for lod in range(3):
    obs=[o for o in all_objects if o['LOD']==lod]
    stats.append(dict(lod=lod,tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in obs),vertices=sum(len(o.data.vertices) for o in obs),renderers=len(obs)))
(HERE/'model-stats.json').write_text(json.dumps(dict(lods=stats,bones=len(rig.data.bones),materials=1,textures=[dict(name='MATTO_Atlas.png',width=1024,height=1024)],shapes=[k.name for k in scene.objects['Face_LOD0'].data.shape_keys.key_blocks][1:]),indent=2),encoding='utf-8')
print(json.dumps(stats))
