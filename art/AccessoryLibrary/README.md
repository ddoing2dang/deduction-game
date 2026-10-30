# 캐릭터 의상 장신구 에셋 라이브러리 — 무료 / 상업용

**제작:** 이 프로젝트용으로 새로 생성한 오리지널 절차적 3D 장신구입니다. 타인의 기존 3D 에셋을 재배포한 것이 아닙니다.

- 파일 형식: **.glb** (Blender 5.0.1과 Unity에서 가져오기 가능)
- 구성: **34종** 장신구, 모두 각각 독립 파일
- 크기: 실제 미터 단위의 작은 의상 장식(보통 수 cm). Blender/Unity 장면에서 필요시 Scale 조정
- 축: 오브젝트 중심=장착 위치, **+Z = 앞면**, +Y = 위
- 재질: GLB 안에 PBR 기본 색상·Metallic·Roughness 포함 (외부 텍스처 불필요)
- **라이선스:** 프로젝트를 위해 새로 생성한 결과물로, **개인·상업적 게임 이용 / 변형 / 복제 / 프로젝트 배포** 가능합니다. 별도 결제·출처 표기 요구 없음. 타사 에셋 또는 상표·캐릭터 자료는 포함하지 않습니다.
- 게임용 최적화: 소형 부착용. 묶음 렌더링 / 머티리얼 아틀라스 / 필요 없는 장식 삭제로 LOD 조정

## 분류 / 적용 예시

- **벨트/가터/끈:** \`ACC_Buckle_Gold\`, \`ACC_Buckle_Silver\`, \`ACC_Buckle_Black\`, \`ACC_Buckle_Oval\`, \`ACC_Double_Clasp\`
- **리본:** \`ACC_Bow_Burgundy\`, \`ACC_Bow_Black\`, \`ACC_Bow_Ivory\`, \`ACC_Bow_Purple\`, \`ACC_Ribbon_DoubleTail\`
- **하트·참:** \`ACC_Heart_Pendant\`, \`ACC_Heart_RedGem\`, \`ACC_Cross_Charm\`, \`ACC_Crescent_Moon\`, \`ACC_Key_Charm\`, \`ACC_Lock_Charm\`
- **브로치:** \`ACC_Star_Brooch\`, \`ACC_Butterfly_Pin\`, \`ACC_Flower_White\`, \`ACC_Rose_Brooch\`, \`ACC_Gem_Brooch_Violet\`, \`ACC_Gem_Brooch_Emerald\`, \`ACC_Crest_Badge\`, \`ACC_Flame_Brooch\`
- **장식 체인:** \`ACC_Chain_Short\`, \`ACC_Chain_Drape\`, \`ACC_Pearl_Strand\`, \`ACC_Heart_Chain\`
- **기타:** \`ACC_Button_Gold\`, \`ACC_Button_Black\`, \`ACC_Zipper_Pull\`, \`ACC_Eyelet_Pair\`, \`ACC_Stud_Triple\`, \`ACC_Lace_Ruffle\`

전체 파일·삼각형 수·용도는 자동 생성된 \`index.json\`을 참조하세요.

## Blender 5.0.1 가져오기

**GUI:** File → Import → glTF 2.0 (.glb/.gltf) → \`glb/ACC_*.glb\` 선택.

**Codex/Blender Python:**
\`\`\`python
import sys
sys.path.insert(0, "C:/UnityProjects/DeductionGame/deduction-game/art/AccessoryLibrary")
from import_accessory_blender import import_accessory
objects = import_accessory("ACC_Heart_Pendant")
# 필요한 위치·크기·회전은 현재 캐릭터 의상 기준으로 별도 피팅.
# 몸·리그 웨이트를 자동 변경하지 않습니다.
\`\`\`

Blender 임포트 API 사용: \`bpy.ops.import_scene.gltf(filepath=...)\`.

## 사용 예시

- 보키: 검정/금 버클, 버건디 리본, 체인, 가터 장식
- 마리히: 금색 하트 펜던트, 보석 브로치, 골드 버클
- 찌로: 흰 꽃·아이보리 리본·초록 보석
- 문이레: 나비 핀·보라 리본·십자가
- 마또: 버건디 리본·붉은 하트·골드 버클
- 나즈: 핑크 계열 장식(리본의 재질색 변경)·골드 버튼

**현재 승인된 보키 모델과 162본 리그, 대저택, Unity 프리팹은 수정하지 않았습니다.**
장신구를 실제 적용할 때는 **별도 Blender 작업본**에 가져와 위치·크기·리그 연결을 검증한 후 채택하세요. GLB 단독으로 자동 웨이트나 캐릭터별 배치가 설정된 것은 아닙니다.

## 재생성

Python 3.11+ 와 \`trimesh\`, \`numpy\` 필요.
\`\`\`sh
python -m pip install trimesh numpy
python art/AccessoryLibrary/build_accessories.py
\`\`\`
이 명령으로 \`glb/*.glb\` 파일과 \`index.json\`을 다시 만들 수 있습니다. GitHub Actions에서 자동 생성·보관합니다.

## 추가 외부 에셋 소스 (다운로드·복제는 별도 라이선스 확인)

- Kenney: https://kenney.nl/assets — 개별 팩 CC0 명시 확인 후 사용
- Poly Haven: https://polyhaven.com/models — 전체 모델 CC0

외부 기성 에셋의 이용 가능성은 실제 각 라이선스를 확인해야 하며 **상업 이용만 가능하고 원본 재배포가 제한된 상품을 이 공개 GitHub에 무단 업로드하지 않습니다.**
