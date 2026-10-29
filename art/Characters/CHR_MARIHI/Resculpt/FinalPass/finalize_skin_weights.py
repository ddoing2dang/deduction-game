import bpy,json
from pathlib import Path
P=Path(__file__).parent
rig=bpy.data.objects['Armature'];bone_names=set(rig.data.bones.keys());report={}
for o in bpy.data.collections['Game_LOD_Meshes'].objects:
    unused=[g.name for g in o.vertex_groups if g.name not in bone_names]
    for name in unused:o.vertex_groups.remove(o.vertex_groups[name])
    for v in o.data.vertices:
        weights=sorted([(g.group,g.weight)for g in v.groups if g.weight>1e-8],key=lambda p:-p[1])[:4]
        total=sum(w for _,w in weights)
        assert total>0,(o.name,v.index)
        for g in list(v.groups):o.vertex_groups[g.group].remove([v.index])
        for i,w in weights:o.vertex_groups[i].add([v.index],w/total,'REPLACE')
    used={g.group for v in o.data.vertices for g in v.groups if g.weight>1e-8}
    unused=[g.name for g in o.vertex_groups if g.index not in used]
    for name in unused:o.vertex_groups.remove(o.vertex_groups[name])
    report[o.name]={'bone_groups':len(o.vertex_groups),'max_influences':max(len(v.groups)for v in o.data.vertices),'maximum_sum_error':max(abs(sum(g.weight for g in v.groups)-1)for v in o.data.vertices)}
assert all(v['max_influences']<=4 and v['maximum_sum_error']<1e-6 for v in report.values())
(P/'skin-weight-audit.json').write_text(json.dumps(report,indent=2))
(P/'renderer-bone-palettes.json').write_text(json.dumps({n:r['bone_groups']for n,r in report.items()},indent=2))
print('All LOD weights normalized on actual bones, maximum four influences.')
