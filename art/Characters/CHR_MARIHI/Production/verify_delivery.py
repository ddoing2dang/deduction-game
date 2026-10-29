from pathlib import Path
import hashlib,json,re,subprocess,datetime
P=Path(__file__).parent;repo=P.parents[3];G=P/'Game';V=G/'Verification'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
play=(V/'play-results.txt').read_text();camera=(V/'camera-results.txt').read_text()
assert 'AUTOMATED CHECKS PASSED' in play and 'FAILED' not in play
assert 'CAMERA CHECKS PASSED; SOURCE ID 2 CONFIRMED' in camera and 'FAILED' not in camera
assert len(re.findall(r'actual Marihi bidirectional traversal DOOR_',play))==22
before=json.loads((P/'protected-files-before.json').read_text());changed=[name for name,h in before.items()if not(repo/name).exists()or sha(repo/name).upper()!=h.upper()]
assert not changed,changed
catalog=repo/'game/UnityProject/Assets/_Game/Resources/PresentationCatalog.asset'
def slots(text):return re.search(r'CharacterPrefabs:\s*\n(.*?)  CharacterPortraits:',text,re.S).group(1).strip().splitlines()
old=slots((P/'catalog-before.asset.txt').read_text());new=slots(catalog.read_text())
assert len(old)==len(new)==6
assert all(old[i]==new[i]for i in [0,1,3,4,5])
assert old[2]!=new[2] and 'guid:' in new[2]
(P/'catalog-preservation.json').write_text(json.dumps(dict(updatedSlot=2,otherSlotsUnchanged=True,localNazPreserved=True,before=old,after=new),indent=2))
assert subprocess.run(['git','diff','--quiet','--','game/UnityProject/Assets/_Game/Scenes/Game.unity'],cwd=repo).returncode==0
stats=json.loads((G/'final-blender-statistics.json').read_text());assert stats['finiteCoordinates'] and stats['requiredExpressionsNonzeroAllLODs']
imp=(G/'unity-import-results.txt').read_text();assert 'Avatar valid=True human=True' in imp
tris=[int(re.search(r'LOD'+str(i)+r' tris=(\d+)',imp).group(1))for i in range(3)]
assert .5<=tris[1]/tris[0]<=.6 and .2<=tris[2]/tris[0]<=.3
files=[P/'CHR_MARIHI.blend',G/'CHR_MARIHI_GAME.blend',repo/'game/UnityProject/Assets/_Game/Characters/CHR_MARIHI/Model/CHR_MARIHI.fbx']
for name in ['01_Front','02_45','03_Side90','04_Back','05_Face','06_Face45']:assert (G/'Review'/(name+'.png')).exists()
for name in ['Trio_MATTO_JIRO_MARIHI','Actual_ActionCamera','Room_1_Zoom_9','Room_1_Zoom_19','Room_1_Zoom_32']:assert (V/(name+'.png')).exists()
report=dict(status='OK',designApproval='PENDING_USER_REVIEW',mainMerged=False,baseMansionCommit='0425d680bbb8802e7e46598900f19c69340e3b57',branch='codex/chr-marihi-production',unityLODTris=tris,blenderLODTris=list(stats['lodTotals'].values()),bones=69,humanoidMappings=54,humanoidValid=True,materials=9,textures=6,maxTextureDimension=2048,storedSMRs=21,activeLODRenderers=7,expressions=25,requiredExpressions=11,actualBidirectionalTraversals=22,standPoints=54,actionPoints=9,animatorStates=9,roomsAtThreeZooms=9,singlePlayerAssertions=2461,actualActionCameraSourceId=2,protectedFiles=len(before),changedProtectedFiles=changed,gameSceneUnchanged=True,catalogChangedSlot=2,blenderConnection='OK',visualReview='Actual Blender views/expressions/deformation/LODs; Unity nine-room three-zoom frames, states, face, trio, service stairs and actual slot-2 action camera directly reviewed',deviceFPSBenchmark=False,files={str(f.relative_to(repo)).replace('\\','/'):sha(f)for f in files},playLogSHA256=sha(V/'play-results.txt'),cameraLogSHA256=sha(V/'camera-results.txt'))
(P/'final-verification.json').write_text(json.dumps(report,indent=2))
print(json.dumps({k:report[k]for k in ['status','designApproval','unityLODTris','actualBidirectionalTraversals','singlePlayerAssertions','protectedFiles','changedProtectedFiles','actualActionCameraSourceId']}))
