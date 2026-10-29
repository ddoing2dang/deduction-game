# CHR_MARIHI 제작·게임 적용 검증

**제작 및 Unity 통합 검증: OK. 사용자 외형 최종 승인: 대기. main 병합 없음.**

기준 대저택 커밋: `0425d680bbb8802e7e46598900f19c69340e3b57`.
작업 브랜치: `codex/chr-marihi-production`.

마리히 원본 참조 시트를 직접 확인하고, 마또의 clean body/face topology와 공용 Humanoid 기술만 재사용했다. 얼굴 윤곽·눈매·금빛 홍채·피부톤·금발 헤어·검정 리본·의상은 마리히용으로 제작했다. 찌로의 꽃 장식과 기존 헤어/의상 완성 오브젝트는 가져오지 않았다. 가구·맵·게임 규칙·AI·UI·다른 캐릭터 파일은 수정하지 않았다.

실제 제작 중 발견한 붉은 눈 잔상, 얼굴 비표시 데이터의 이름 충돌, 앞머리 이마 교차, 정수리 돌출, 헤어 레이어 겹침, 넥타이 관통을 수정했다. 최종 렌더에서 이 결함이 재현되지 않는 것을 확인했다. 검정 보디스·금색 파이핑·자카드 소매·하트 장식으로 의상을 구분했다.

## 실제 결과물

- 편집 가능한 원본: [CHR_MARIHI.blend](CHR_MARIHI.blend)
- 게임 작업본: [CHR_MARIHI_GAME.blend](Game/CHR_MARIHI_GAME.blend)
- FBX: 저장소 기준 `game/UnityProject/Assets/_Game/Characters/CHR_MARIHI/Model/CHR_MARIHI.fbx`
- Unity 슬롯: **2만 변경**. 기존 로컬 나즈 슬롯과 마또·찌로 슬롯 유지.
- 보존 비교: [catalog-preservation.json](catalog-preservation.json), [protected-files-check.json](protected-files-check.json). 기존 파일 **3,758개 변경 없음**. Game.unity도 기준 커밋과 동일.

## 제출 이미지 — 동일한 최종 게임 모델

1. [정면 전신](Game/Review/01_Front.png)
2. [45도 전신](Game/Review/02_45.png)
3. [정확한 90도 측면](Game/Review/03_Side90.png)
4. [후면](Game/Review/04_Back.png)
5. [얼굴 근접](Game/Review/05_Face.png), [얼굴 45도](Game/Review/06_Face45.png)
6. [Blender 표정 비교](Review/Blender_Expressions.png), [Unity 표정 비교](Review/Unity_Expressions.png)
7. [대저택 실제 게임 화면](Game/Verification/Room_1_Zoom_9.png), [실제 마리히 행동 사이드뷰](Game/Verification/Actual_ActionCamera.png)
8. [마또·찌로·마리히 3인 실제 Unity 비교](Game/Verification/Trio_MATTO_JIRO_MARIHI.png)

[전신·얼굴 시트](Review/Blender_Turnaround.png), [레퍼런스와 실제 모델 비교](Review/Reference_Model.png), [Blender/Unity 얼굴 비교](Review/Blender_Unity_Face.png).
비교 시트는 실제 저장된 렌더를 크기 조정·배열한 것이다. AI 콘셉트를 모델 렌더로 사용하지 않았다.

## 규격 및 직접 확인

- Blender LOD0/1/2: **30,703 / 18,118 / 9,116 tris**.
- Unity LOD0/1/2: **30,679 / 18,094 / 9,092 tris**. Import 시 각 LOD의 degenerate triangle 24개 제거.
- 69 bones, 54 Humanoid 매핑, Avatar valid/human. 최대 4개 정규화 웨이트.
- SkinnedMeshRenderer 21개 저장, LOD별 7개 활성. Body GPU palette 최대 56, Face 1, Hair 7.
- 9 Material, 6 Texture, 최대 2K. 1 BU=1m, 실제 rest geometry 높이 약 **1.647m**.
- 25개 표정 별칭과 Basis, 필수 11개 표정/입 모양이 모든 LOD에서 존재하고 변형량이 0이 아님.
- 기존 Idle/Walk/Investigate/Suspect/Deduction 5개 공용 클립을 리타깃. 나머지 상태는 기존 대표 클립/표정 조합. 최종 18개 신규 클립을 제작했다는 의미는 아님.
- [Blender 변형 검사](Review/Blender_Deformation.png): T/Idle/Walk, 팔 위·앞, 무릎·앉는 굽힘. 어깨·소매·스커트·스트랩·헤어에서 심각한 파손/관통 없음.
- [LOD 비교](Review/Blender_LODs.png): 얼굴·금발 트윈테일·검정 리본·의상 실루엣 유지. 멀리서 작은 주름/장식/헤어 세부는 단순화됨.
- [Unity 9개 상태](Review/Unity_States.png): 상태 전환·스키닝·표정 확인.
- 9개 방 × 3단 줌 직접 확인: [근접](Review/Unity_Rooms_9.png), [기본](Review/Unity_Rooms_19.png), [전체](Review/Unity_Rooms_32.png). 지하·컷어웨이 포함.
- 실제 마리히 CharacterActor로 11개 기본 연결 양방향 **22회**, 54 StandPoint, 9 ActionPoint 통과. 발 접지·경로·회전·벽/문/캐릭터 메시 간섭 검사 통과. 서비스 계단 실제 이동 캡처 직접 확인.
- 실제 행동 카메라 호출의 SourceId가 **2**, Marihi Animator가 **Deduction**임을 별도 검사. Visual Gold 사이드뷰와 기본 줌 복귀 통과.
- 싱글플레이 회귀 **2,461 assertions**, Restart 후 6명/마리히 1명/UI 준비 상태 유지.

[전체 이동·상태 로그](Game/Verification/play-results.txt), [슬롯 2 카메라 보완 검사](Game/Verification/camera-results.txt), [기계 검증/파일 해시](final-verification.json).

## 남은 검토 범위

검사한 포즈와 실제 카메라에서 진행을 막는 메시 결함은 발견하지 못했다. 원거리 LOD의 작은 치마 주름·장식·헤어 디테일 간소화와 Blender/Unity 조명 차이는 남아 있다. Android 실제 기기 FPS는 측정하지 않았다. **레퍼런스와의 최종 외형 적합성은 사용자의 승인 대상이며, 이 보고서가 그 승인을 대신하지 않는다.**

Blender 연결: OK
Unity Humanoid: OK
검증: OK
