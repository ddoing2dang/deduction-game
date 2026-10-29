from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib,re
P=Path(__file__).parent;U=P/'Verification';O=P/'Comparison';font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
def board(name,items,cols=3,w=600,h=375):
 rows=(len(items)+cols-1)//cols;im=Image.new('RGB',(cols*w,rows*(h+38)),(28,26,26));d=ImageDraw.Draw(im)
 for i,(p,label)in enumerate(items):
  pic=Image.open(p).convert('RGB');pic.thumbnail((w,h));x=i%cols*w+(w-pic.width)//2;y=i//cols*(h+38);im.paste(pic,(x,y));d.text((i%cols*w+8,y+h+5),label,font=font,fill='white')
 im.save(O/name)
for zoom in [9,19,32]:board('Unity_Rooms_'+str(zoom)+'.png',[(U/f'Room_{n}_Zoom_{zoom}.png',f'Room {n} / actual camera zoom {zoom}')for n in range(1,10)])
board('Unity_States.png',[(U/f'Action_{n}.png',n)for n in ['Idle','Move','Investigate','Suspect','Cooperate','Deduction','UseAbility','Win','Lose']])
board('Blender_Unity_Full.png',[(P/'After/Full_Front.png','Blender actual mesh'),(U/'Preservation_LOD0.png','Unity same FBX / game lighting')],2,650,850)
board('Blender_Unity_Face.png',[(P/'After/Face_Front.png','Blender actual face'),(U/'Preservation_Face.png','Unity same face / game lighting')],2,650,650)
board('Unity_LODs.png',[(U/f'Preservation_LOD{i}.png',f'Unity forced LOD{i}')for i in range(3)],3,450,600)
board('Blender_Poses.png',[(P/f'After/Pose_{n}.png',n)for n in ['T_Pose','Walk','Arms_Up','Arms_Forward','Sit']],3,420,550)
board('Unity_Expressions.png',[(U/f'Face_{n}.png',n)for n in ['Neutral','Blink_L','Blink_R','Smile','Angry','Sad','Surprise','Mouth_A','Mouth_I','Mouth_U','Mouth_E','Mouth_O']],4,350,350)
board('Unity_Stairs.png',[(U/f'Stair_{door}_Direction_{direction}_Zoom_9.png',f'{door} / direction {direction}')for door in ['DOOR_04_07','DOOR_05_08']for direction in [0,1]],2,850,530)
log=(U/'play-results.txt').read_text();assert 'AUTOMATED CHECKS PASSED'in log and 'FAILED'not in log
assert log.count('actual Marihi bidirectional traversal')==22
assert log.count('actual Marihi mesh clearance at StandPoint')==54
assert log.count('actual Marihi mesh clearance at ActionPoint')==9
assert 'regression assertions=2461'in log
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
root=P.parents[4];fbx=root/'game/UnityProject/Assets/_Game/Characters/CHR_MARIHI/Model/CHR_MARIHI.fbx'
original=P.parents[1]/'Production/CHR_MARIHI.blend';backup=P.parent/'Backup/CHR_MARIHI_c11daf6.blend'
assert sha(original)==sha(backup)
resculpt=P.parent/'CHR_MARIHI_RESCULPT.blend';pre=P.parent/'Backup/CHR_MARIHI_PreFinalTailor.blend';assert sha(resculpt)==sha(pre)
catalog=(root/'game/UnityProject/Assets/_Game/Resources/PresentationCatalog.asset').read_text()
slots=[x.strip()for x in catalog.split('  CharacterPrefabs:\n')[1].split('  CharacterPortraits:')[0].splitlines()if x.strip()]
previous=json.loads((P.parents[1]/'Production/catalog-preservation.json').read_text())['after'];assert slots==[x.strip()for x in previous]
report={'sameBlenderCaptureConditions':json.loads((P/'Before/capture-settings.json').read_text())==json.loads((P/'After/capture-settings.json').read_text()),'Blender':json.loads((P/'blender-verification.json').read_text()),'UnityHumanoid':'Valid human Avatar; 54 mappings','traversals':22,'StandPoints':54,'ActionPoints':9,'AnimatorStates':9,'singlePlayerAssertions':2461,'actionCamera':'Actual SourceId 2, side view and return checked','files':{str(p.relative_to(root)):sha(p)for p in [original,resculpt,P/'CHR_MARIHI_FINAL_REVIEW.blend',P/'CHR_MARIHI_GAME.blend',fbx]},'originalProductionAndPreviousResculptPreserved':True,'userAppearanceApproval':'Pending; no main merge','platformLimit':'No Android device performance benchmark'}
(P/'delivery-verification.json').write_text(json.dumps(report,indent=2))
(P/'catalog-preservation.json').write_text(json.dumps({'allSixSlotReferencesUnchangedFromPreResculpt':True,'slots':slots},indent=2))
print('Captured model boards and checks:',report['traversals'],report['StandPoints'],report['ActionPoints'])
