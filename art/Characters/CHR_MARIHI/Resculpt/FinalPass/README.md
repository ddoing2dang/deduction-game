# 마리히 상체·얼굴 최종 보정 검토본

사용자 최종 외형 승인 전의 작업 브랜치 결과물이다. 이 문서는 사용자 디자인 승인을 대신하지 않는다. main 병합 없음.

## 파일과 보존

- 편집용: `CHR_MARIHI_FINAL_REVIEW.blend`
- 동일 게임 메시와 리그를 담은 내보내기 작업본: `CHR_MARIHI_GAME.blend`
- FBX: `game/UnityProject/Assets/_Game/Characters/CHR_MARIHI/Model/CHR_MARIHI.fbx`
- `../CHR_MARIHI_RESCULPT.blend`와 `../../Production/CHR_MARIHI.blend`는 덮어쓰지 않았다. 로컬 `../Backup/CHR_MARIHI_PreFinalTailor.blend`와 `../Backup/CHR_MARIHI_c11daf6.blend`의 SHA256 일치를 확인했다.
- 이전 FBX도 `../Backup/CHR_MARIHI_PreFinalTailor.fbx`에 보존했다. 로컬 백업과 Blender 자동 백업은 Git에서 제외한다.

## 실제 제작 변경

1. 과도한 타원 단면, 겹친 보디스 판, 분리된 어깨 덮개, 기존 상체 의상 메시를 제거했다.
2. 검증된 VRoid 기술 베이스의 연결된 상의 토폴로지를 재단용으로 읽어와 마리히 몸통 곡면에 맞게 버텍스를 편집했다. 마또의 완성 의상·얼굴·헤어를 가져오지 않았으며 마또 파일을 수정하지 않았다.
3. 허리와 흉부를 서로 다른 범위에서 정리하고, 소매를 실제 어깨 웨이트와 공유 버텍스에서 연장했다. 소매의 비균일 단면과 주름을 줄이고 옷 가장자리에 1mm 두께를 만들었다. 어깨 관절을 움직일 때 별도 판 사이가 벌어지던 구조를 없앴다.
4. 검정 보디스 영역은 같은 몸통 곡면에서 이어지며, 블라우스를 뚫거나 떠 있는 중복 판이 없다. 새 1K 원단 마스크와 표면 바인딩으로 검정·아이보리·금색 경계를 정리했다. 이것은 메시 재제작 이후의 표면 처리이며, 조명/색상만 바꾼 수정이 아니다.
5. 옷깃은 쇄골 곡면을 따라 접힌 면으로 만들고 마리히의 기존 리본·타이·단추를 새 표면과 웨이트에 맞췄다.
6. 마리히 얼굴 Basis의 볼·턱·눈 주변·코·입 주변을 추가 조형했다. LOD0 얼굴 버텍스 3,686개 이동, 이번 추가 최대 변위 약 5.11mm. 모든 얼굴 표정과 세 얼굴 LOD에 같은 형태 변환을 적용했다. 머리카락은 금발·검정 리본·웨이브 트윈테일을 유지하면서 앞머리 끝과 트윈테일 끝 단면을 수정했다. 루트는 이동시키지 않았다.
7. 가까이 강제 표시한 원거리 LOD에서 치마 밑단이 흔들리는 결함을 발견해 LOD1/2의 주름·밑단 링을 유지하는 격자로 교체했다. LOD2의 작은 액세서리 세부만 추가로 줄였다. 얼굴과 손을 일괄 Decimate하지 않았다.

## 비교 이미지

모든 이미지는 실제 Blender 또는 Unity 모델 렌더다. AI 콘셉트 이미지를 사용하지 않았다.

- 얼굴 전후: `Comparison/Face_Front.png`, `Face_45.png`, `Face_Side90.png`
- 상반신 전후: `Comparison/Upper_Front.png`, `Upper_45.png`, `Upper_Side90.png`
- 전신 전후: `Comparison/Full_Front.png`, `Full_45.png`
- 팔 올린 어깨: `After/Raised_Shoulder.png`
- 동작: `Comparison/Blender_Poses.png`
- 표정: `Comparison/Expressions_After.png`, `Unity_Expressions.png`
- 같은 게임 메시의 Blender/Unity 비교: `Comparison/Blender_Unity_Full.png`, `Blender_Unity_Face.png`
- Unity 강제 LOD: `Comparison/Unity_LODs.png`
- 실제 플레이 카메라: `Comparison/Unity_Rooms_9.png`, `Unity_Rooms_19.png`, `Unity_Rooms_32.png`
- 실제 행동 카메라: `Verification/Actual_ActionCamera.png`
- 세 캐릭터 비교: `Verification/Trio_MATTO_JIRO_MARIHI.png`

전후 비교의 카메라·조명·포즈·배율·색 관리 설정은 `Before/capture-settings.json`과 `After/capture-settings.json`의 완전 일치로 확인한다. Blender/Unity 사이에는 각 엔진의 실제 조명과 Idle 포즈 차이가 있으며 동일 조명 비교라고 주장하지 않는다.

## 최종 규격

| 항목 | Blender | Unity |
|---|---:|---:|
| LOD0 tris | 31,733 | 31,725 |
| LOD1 tris | 18,721 | 18,713 |
| LOD2 tris | 9,513 | 9,505 |
| 본 | 69 | 69 / Humanoid 매핑 54 |
| 표정 | 필수 11개 포함 기존 인터페이스 유지 | 모든 Face LOD에서 비영 변형 확인 |
| 최대 웨이트 영향 | 4 | 4 |
| Renderer | LOD별 7개 | 21개 저장 / LOD별 7개 선택 |
| Material / Texture | 10 / 7 | 10 / 7, 최대 2K |

Unity는 퇴화 삼각형 8개를 각 LOD에서 제거한다. 얼굴과 손의 LOD0 토폴로지는 유지했다. Android 실제 기기 프레임 성능은 측정하지 않았다.

## 검증 범위

Blender에서 정면·45도·정확한 측면, 전신, 팔 위/앞, 걷기, 앉기, 표정과 세 LOD를 렌더링해 직접 확인한다. 상체 측면의 과도한 흉부 돌출, 어깨의 분리된 판/구멍, 보디스의 흰 면 관통을 수정했다.

Unity는 프로젝트 내부 Editor API로 FBX import, Humanoid/Animator/LOD 구성, 실제 CharacterActor 슬롯 2 검증을 수행한다. `Verification/play-results.txt`에 현재 모델의 실행 결과를 저장한다. 11개 연결 양방향 22회, 54 StandPoint, 9 ActionPoint, 9 상태, 9개 방 3단 줌, 지하·계단·컷어웨이, 실제 SourceId 2 행동 사이드뷰 및 복귀, Restart와 기존 싱글플레이 2,461 assertions가 대상이다. 기술 검사 후 실제 캡처를 별도로 검토한다.

마또·찌로·나즈 슬롯 참조, 대저택, 씬, 게임 규칙, AI, UI, 멀티플레이 코드는 수정 대상이 아니다. 기존 로컬 변경은 보존하고 커밋에서 제외한다. Unity 검토 자료는 이전 Production 자료와 분리되도록 Editor API의 임시 아티팩트 경로를 사용한다.

Blender 조작은 MCP만 사용했다. Unity는 프로젝트 파일과 Editor API만 사용했다. OS 입력·커서 제어·GUI 자동화는 사용하지 않았다.

제작 스크립트는 순서가 있는 작업 체크포인트이며 현재 완성 파일 위에서 임의로 재실행하지 않는다. 내보내기와 검증 스크립트는 현재 게임 메시를 대상으로 한다.

최종 모델로 이동 22회·54 StandPoint·9 ActionPoint·9 상태·회귀 2,461 assertions를 다시 통과했다. `visual-review.json`에 실제 직접 검토한 이미지와 판정 범위를 기록했다. 제작 결함 수정과 기록된 기능/시각 검증: OK. 사용자 최종 외형 승인: 대기.
