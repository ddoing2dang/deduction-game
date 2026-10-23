"""Run only inside Blender via MCP. Common pilot skeleton, version 1.

This creates technical rig data only; it does not claim to be the finished
Matto character or replace the locked character design reference.
"""
import bpy
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
HERE.mkdir(parents=True, exist_ok=True)
scene = bpy.data.scenes.get('CHR_MATTO_RigDevelopment')
if scene:
    raise RuntimeError('Rig development scene already exists; inspect before rebuilding')
scene = bpy.data.scenes.new('CHR_MATTO_RigDevelopment')
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
scene.render.fps = 30
bpy.context.window.scene = scene
root = bpy.data.objects.new('CHR_MATTO', None)
scene.collection.objects.link(root)
root['spec_version'] = 'Humanoid_v1_draft'
root['body_height_m'] = 1.568
root['head_ratio'] = 5.6
root['status'] = 'Technical rig only; design reference pending'
mesh_root = bpy.data.objects.new('Mesh', None)
scene.collection.objects.link(mesh_root)
mesh_root.parent = root
data = bpy.data.armatures.new('Humanoid_v1')
rig = bpy.data.objects.new('Armature', data)
scene.collection.objects.link(rig)
rig.parent = root
rig.show_in_front = True
rig.display_type = 'WIRE'
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
rows = []
def bone(name, head, tail, parent=None, human=None, deform=True):
    b = data.edit_bones.new(name)
    b.head, b.tail = head, tail
    b.use_deform = deform
    if parent:
        b.parent = data.edit_bones[parent]
    rows.append(dict(name=name, parent=parent, head=list(head), tail=list(tail),
                     human=human, deform=deform))
    return name

bone('Root', (0,0,0), (0,0,.12), deform=False)
bone('Hips', (0,0,.80), (0,0,.90), 'Root', 'Hips')
bone('Spine', (0,0,.90), (0,0,1.04), 'Hips', 'Spine')
bone('Chest', (0,0,1.04), (0,0,1.16), 'Spine', 'Chest')
bone('UpperChest', (0,0,1.16), (0,0,1.225), 'Chest', 'UpperChest')
bone('Neck', (0,0,1.225), (0,0,1.29), 'UpperChest', 'Neck')
bone('Head', (0,0,1.29), (0,0,1.545), 'Neck', 'Head')
for sign, side in [(1,'Left'),(-1,'Right')]:
    bone(side+'Eye', (sign*.049,-.111,1.425), (sign*.049,-.14,1.425), 'Head', side+'Eye')
    bone(side+'Shoulder', (sign*.035,0,1.205), (sign*.155,0,1.205), 'UpperChest', side+'Shoulder')
    bone(side+'UpperArm', (sign*.155,0,1.205), (sign*.405,0,1.205), side+'Shoulder', side+'UpperArm')
    bone(side+'LowerArm', (sign*.405,0,1.205), (sign*.625,0,1.205), side+'UpperArm', side+'LowerArm')
    bone(side+'Hand', (sign*.625,0,1.205), (sign*.697,0,1.205), side+'LowerArm', side+'Hand')
    for finger, y, length in [('Thumb',-.029,.049),('Index',-.021,.063),('Middle',-.005,.069),('Ring',.012,.064),('Little',.027,.05)]:
        start = .659 if finger=='Thumb' else .691
        parent = side+'Hand'
        for index, segment in enumerate(['Proximal','Intermediate','Distal']):
            thumb_step = .012 if finger=='Thumb' else 0
            h = (sign*(start+length*index/3), y-thumb_step*index, 1.205)
            t = (sign*(start+length*(index+1)/3), y-thumb_step*(index+1),1.205)
            name=side+finger+segment
            bone(name,h,t,parent,side+' '+finger+' '+segment)
            parent=name
    bone(side+'UpperLeg', (sign*.079,0,.80), (sign*.079,-.008,.43), 'Hips', side+'UpperLeg')
    bone(side+'LowerLeg', (sign*.079,-.008,.43), (sign*.079,0,.085), side+'UpperLeg', side+'LowerLeg')
    bone(side+'Foot', (sign*.079,0,.085), (sign*.079,-.095,.035), side+'LowerLeg', side+'Foot')
    bone(side+'Toes', (sign*.079,-.095,.035), (sign*.079,-.155,.035), side+'Foot', side+'Toes')
bone('Hair_Back_01',(0,.095,1.46),(0,.12,1.15),'Head')
bone('Hair_Back_02',(0,.12,1.15),(0,.135,.84),'Hair_Back_01')
bpy.ops.object.mode_set(mode='OBJECT')
for obj in scene.objects:
    obj.select_set(False)
rig.select_set(True)
bpy.context.view_layer.objects.active=rig
spec=dict(version='Humanoid_v1_draft', units='meter', forward='-Y', up='+Z',
          height_m=1.568, head_ratio=5.6, bones=rows,
          required_shapes=['Blink_L','Blink_R','Smile','Angry','Sad','Surprise',
                           'Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O'],
          status='Technical skeleton only. Awaiting locked design reference before visual production.')
(HERE/'humanoid_v1.json').write_text(json.dumps(spec,indent=2),encoding='utf-8')
bpy.data.libraries.write(str(HERE/'CHR_MATTO_RigDevelopment.blend'),{scene},fake_user=True)
print(json.dumps(dict(file=str(HERE/'CHR_MATTO_RigDevelopment.blend'),bones=len(rows),
                      mapped_bones=sum(bool(b['human']) for b in rows),status=spec['status'])))
