"""Render actual pilot poses and inspect skin integrity in Blender via MCP."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent;out=HERE/'Verification/Blender';out.mkdir(parents=True,exist_ok=True)
scene=bpy.context.scene;rig=scene.objects['Armature'];cam=scene.camera
scene.render.resolution_x=900;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
parts=[o for o in scene.objects if o.type=='MESH' and o.get('LOD')==0]
for o in scene.objects:
    if o.type=='MESH':o.hide_render=o.get('LOD')!=0;o.hide_set(o.get('LOD')!=0)
def save(name,eye,focus=(0,0,.84),size=1.9):
    cam.location=eye;cam.rotation_euler=(Vector(focus)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=size
    scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
results=[]
rig.animation_data.action=bpy.data.actions['MATTO_Idle'];scene.frame_set(1)
for name,eye in [('Front',(0,-4,1.05)),('Side',(4,0,1.05)),('Back',(0,4,1.05)),('Iso',(2,-4,2.4))]:save(name,eye)
for kind,frames in [('Idle',[1,16,31]),('Walk',[1,7,13,19]),('Investigate',[16]),('Suspect',[16]),('Deduction',[16])]:
    rig.animation_data.action=bpy.data.actions['MATTO_'+kind]
    for frame in frames:
        scene.frame_set(frame);bpy.context.view_layer.update()
        deps=bpy.context.evaluated_depsgraph_get()
        for ob in parts:
            evaluated=ob.evaluated_get(deps);mesh=evaluated.to_mesh()
            points=[ob.matrix_world@v.co for v in mesh.vertices]
            assert all(math.isfinite(x) for p in points for x in p),ob.name+' invalid deformation'
            assert max(p.z for p in points)<1.9,ob.name+' exploding skin'
            evaluated.to_mesh_clear()
        results.append(kind+' frame '+str(frame)+': finite skin, stable silhouette')
    scene.frame_set(7 if kind=='Walk' else 16)
    save('Action_'+kind,(1.5,-4,1.8))
rig.animation_data.action=bpy.data.actions['MATTO_Idle'];scene.frame_set(1)
face=scene.objects['Face_LOD0']
for name in ['Neutral','Blink_L','Blink_R','Smile','Angry','Sad','Surprise','Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O']:
    for key in face.data.shape_keys.key_blocks[1:]:key.value=0
    face.data.shape_keys.key_blocks[name].value=1
    save('Face_'+name,(0,-3,1.43),(0,-.02,1.43),.40)
for key in face.data.shape_keys.key_blocks[1:]:key.value=0
save('Pilot_Front',(0,-4,1.05))
(HERE/'blender-pose-results.json').write_text(json.dumps({'checks':results,'captures':len(list(out.glob('*.png'))),
    'mesh_parts':['Body','Face','Hair_Main','Outfit','ACC_MATTO_Beret','Shoes','Accessory'],
    'note':'Finite skin checks do not replace direct visual intersection review.'},indent=2),encoding='utf-8')
scene.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'CHR_MATTO.blend'))
print('Blender actual renders complete:',str(out))
