import json,hashlib,shutil
from pathlib import Path
P=Path(__file__).resolve().parent
CODES=('NAZ','JIRO','MARIHI','MATTO','BOKI','MUNIRE')
EYES=dict(zip(CODES,('pink','yellow','green','red','gray','purple')))
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def configure():
    records=[]
    for code in CODES:
        source=P/'sources'/f'{code}.blend'
        work=P/'work'/f'CHR_{code}'/f'CHR_{code}_REFINEMENT.blend'
        assert source.is_file(),source
        work.parent.mkdir(parents=True,exist_ok=True)
        if not work.exists():shutil.copy2(source,work)
        assert source.resolve()!=work.resolve()
        records.append(dict(character='CHR_'+code,source=source.relative_to(P).as_posix(),source_sha256=digest(source),work=work.relative_to(P).as_posix(),initial_work_sha256=digest(work),eye_lock=EYES[code],status='raw_refinement_base',unity_replacement_allowed=False,required_before_game_export=['semantic_parts','UV','materials','Humanoid_rig','skinning','expressions','LOD','visual_approval','game_validation']))
    registry=dict(schema=1,design_rules='../../ART_GUIDE.md',active_authoring_stage='refinement',unity_export_enabled=False,characters=records)
    (P/'active_sources.json').write_text(json.dumps(registry,indent=2),encoding='utf-8')
    verify()
def resolve(code):
    registry=json.loads((P/'active_sources.json').read_text(encoding='utf-8'))
    entry=next(e for e in registry['characters'] if e['character']=='CHR_'+code.removeprefix('CHR_').upper())
    return P/entry['work']
def verify():
    data=json.loads((P/'active_sources.json').read_text(encoding='utf-8'))
    assert len(data['characters'])==6 and not data['unity_export_enabled']
    for e in data['characters']:
        assert digest(P/e['source'])==e['source_sha256']
        assert (P/e['work']).is_file() and not e['unity_replacement_allowed']
    (P/'source-binding-verification.json').write_text(json.dumps(dict(result='OK',sources=6,work_copies=6,original_hashes_preserved=True,unity_export_enabled=False),indent=2))
if __name__=='__main__':configure()
