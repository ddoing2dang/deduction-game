"""Create reusable pilot actions and export through Blender MCP only."""
import bpy, math, json
from bpy_extras.io_utils import axis_conversion
from mathutils import Quaternion,Vector,Matrix
from pathlib import Path
from math import sin,cos,pi
HERE=Path(__file__).resolve().parent;PROJECT=HERE.parents[2]
UNITY=PROJECT/'game/UnityProject/Assets/_Game/Characters/CHR_MATTO'
scene=bpy.context.scene;rig=scene.objects['Armature'];root=scene.objects['CHR_MATTO']
scene.render.fps=30
rig.animation_data_clear()
for a in list(bpy.data.actions):
    if a.name.startswith('MATTO_'):bpy.data.actions.remove(a)
def turn(name,axes):
    pb=rig.pose.bones[name];rest=pb.bone.matrix_local.to_quaternion();q=Quaternion((1,0,0,0))
    for axis,angle in axes:q=Quaternion(axis,angle)@q
    pb.rotation_mode='QUATERNION';pb.rotation_quaternion=rest.inverted()@q@rest
def reach(side,target):
    bpy.context.view_layer.update()
    upper=rig.pose.bones[side+'UpperArm'];lower=rig.pose.bones[side+'LowerArm'];start=upper.head.copy();target=Vector(target)
    delta=target-start;distance=min(delta.length,upper.length+lower.length-.005);direction=delta.normalized()
    along=(upper.length**2-lower.length**2+distance**2)/(2*distance)
    outward=Vector((1 if side=='Left' else -1,0,-.7));bend=(outward-direction*outward.dot(direction)).normalized()
    elbow=start+direction*along+bend*max(0,upper.length**2-along**2)**.5
    for pb,a,b in [(upper,start,elbow),(lower,elbow,target)]:
        original=pb.bone.tail_local-pb.bone.head_local
        rotation=original.rotation_difference((b-a).normalized())@pb.bone.matrix_local.to_quaternion()
        pb.matrix=Matrix.Translation(a)@rotation.to_matrix().to_4x4()
        bpy.context.view_layer.update()
def pose(kind,t):
    for pb in rig.pose.bones:pb.location=(0,0,0);pb.rotation_mode='QUATERNION';pb.rotation_quaternion=(1,0,0,0);pb.scale=(1,1,1)
    phase=2*pi*t
    turn('Chest',[((1,0,0),.012*sin(phase))])
    for sign,side in [(1,'Left'),(-1,'Right')]:
        turn(side+'UpperArm',[((0,1,0),sign*1.26),((1,0,0),.018*sin(phase))])
        turn(side+'LowerArm',[((0,1,0),sign*.10)])
        for finger in ['Thumb','Index','Middle','Ring','Little']:
            for seg in ['Proximal','Intermediate','Distal']:
                turn(side+finger+seg,[((0,1,0),sign*(.16 if finger=='Thumb' else .24))])
    if kind=='Walk':
        for sign,side in [(1,'Left'),(-1,'Right')]:
            wave=sin(phase+(0 if sign==1 else pi))
            turn(side+'UpperLeg',[((1,0,0),-.42*wave)])
            turn(side+'LowerLeg',[((1,0,0),.65*max(0,wave))])
            turn(side+'Foot',[((1,0,0),-.16*wave)])
            turn(side+'UpperArm',[((0,1,0),sign*1.26),((1,0,0),.22*wave)])
        # A small stance-height adjustment, no horizontal/root displacement.
        rig.pose.bones['Hips'].location=(0,-.023,0)
        turn('Chest',[((0,0,1),.035*sin(phase))])
        turn('Hair_Back_01',[((1,0,0),.015*sin(phase+.3))])
    elif kind=='Investigate':
        turn('Head',[((1,0,0),.06+.025*sin(phase))])
        reach('Left',(.025,-.22,1.14+.005*sin(phase)))
    elif kind=='Suspect':
        reach('Right',(-.35,-.34,1.15+.005*sin(phase)))
        for seg in ['Proximal','Intermediate','Distal']:turn('RightIndex'+seg,[])
        turn('Head',[((0,0,1),-.06)])
    elif kind=='Deduction':
        reach('Right',(-.025,-.17,1.265+.003*sin(phase)))
        turn('Head',[((0,0,1),.06)])

actions=[]
for kind,frames in [('Idle',60),('Walk',24),('Investigate',60),('Suspect',60),('Deduction',60)]:
    action=bpy.data.actions.new('MATTO_'+kind);rig.animation_data_create();rig.animation_data.action=action
    action.use_fake_user=True
    for frame in range(1,frames+2,3):
        pose(kind,(frame-1)/frames)
        for pb in rig.pose.bones:
            pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=pb.name)
            if pb.name=='Hips':pb.keyframe_insert(data_path='location',frame=frame,group=pb.name)
    action['pilot_role']=kind;actions.append(action)

def select_export(meshes):
    for ob in scene.objects:
        # FBX filename supplies CHR_MATTO in Unity. Exporting the equally named
        # Blender container too creates duplicate HumanDescription node names.
        ob.hide_set(False);ob.select_set(ob==rig or ob.name=='Mesh' or (meshes and ob.type=='MESH'))
    bpy.context.view_layer.objects.active=rig
common=dict(use_selection=True,global_scale=1.,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
            axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=False,
            armature_nodetype='NULL',path_mode='RELATIVE',use_mesh_modifiers=False)
model=UNITY/'Model/CHR_MATTO.fbx';model.parent.mkdir(parents=True,exist_ok=True)
anim=UNITY/'Animations/MATTO_PilotActions.fbx';anim.parent.mkdir(parents=True,exist_ok=True)
meshroot=scene.objects['Mesh'];original_rig=rig.data
original_meshes={ob:ob.data for ob in scene.objects if ob.type=='MESH'}
original_roots={ob:(ob.parent,ob.matrix_world.copy()) for ob in [rig,meshroot]}
conversion=axis_conversion(to_forward='-Z',to_up='Y').to_4x4()
try:
    # Export-only copies, Y-up data and cancelling object transforms. This
    # produces identity Unity roots while keeping the editable .blend Z-up.
    rig.data=original_rig.copy();rig.data.transform(conversion)
    for ob,data in original_meshes.items():
        ob.data=data.copy();ob.data.transform(conversion,shape_keys=True)
    for ob in [rig,meshroot]:ob.parent=None;ob.matrix_world=conversion.inverted()
    rig.animation_data.action=None
    for pb in rig.pose.bones:pb.rotation_quaternion=(1,0,0,0);pb.location=(0,0,0)
    rig.data.pose_position='REST';select_export(True);bpy.context.view_layer.update()
    bpy.ops.export_scene.fbx(filepath=str(model),object_types={'EMPTY','ARMATURE','MESH'},bake_anim=False,**common)
    rig.data.pose_position='POSE';rig.animation_data.action=actions[0];select_export(False)
    bpy.ops.export_scene.fbx(filepath=str(anim),object_types={'EMPTY','ARMATURE'},bake_anim=True,
                           bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
                           bake_anim_simplify_factor=0,bake_anim_force_startend_keying=True,**common)
finally:
    temporary=rig.data;rig.data=original_rig;bpy.data.armatures.remove(temporary)
    for ob,data in original_meshes.items():
        temporary=ob.data;ob.data=data;bpy.data.meshes.remove(temporary)
    for ob,(parent,matrix) in original_roots.items():ob.parent=parent;ob.matrix_world=matrix
rig.animation_data.action=actions[0];scene.frame_set(1)
for ob in scene.objects:
    if ob.type=='MESH':ob.hide_set(ob.get('LOD')!=0);ob.hide_render=ob.get('LOD')!=0
scene.frame_start=1;scene.frame_end=60
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'CHR_MATTO.blend'))
(HERE/'export-settings.json').write_text(json.dumps(dict(model=str(model),animations=str(anim),settings=common,
     animation_actions=[a.name for a in actions],model_animation_import=False,forward='-Z',up='Y'),indent=2),encoding='utf-8')
print('Exported model and five reusable action takes:',str(model),str(anim))
