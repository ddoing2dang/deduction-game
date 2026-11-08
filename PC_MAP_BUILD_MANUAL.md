# 마또 머더미스터리 — PC 전용 3테마 Unity 맵 제작 매뉴얼
최신 2026-11-09 · **USER LOCK: Windows 64-bit PC 전용**, Android/iOS/macOS 제외

## 작업 상태
- 이번 저장은 **제작 매뉴얼 + 재사용 CC0 3D OBJ 모델 19개 + 텍스처 팔레트 4종 + 라이선스**를 GitHub에 보관하는 단계입니다.
- 게임용 완성 맵, 씬 구성, 모델의 Unity 임포트, 조명, 상호작용, 기능 테스트, EXE 빌드는 **아직 하지 않았습니다**.
- **현재 GitHub main에는 `game/UnityProject/`가 아직 없고**, 별도 작업으로 이전 구형 Unity 프로젝트가 초기화된 상태입니다. 삭제 전 버전은 `archive/pre-fresh-unity-project-20261109` 브랜치에 보존돼 있습니다. 사용자 PC의 로컬 프로젝트에 원격 삭제를 수행하지 않았습니다. 승인된 NPC·대화창 데이터는 보존하고, 별도 요청 없이 Play Mode/기능 테스트/빌드/배포 금지.

## 왜 Unity 조립 방식인가
Blender에서 모든 환경을 직접 모델링할 필요 없이 **Unity에서 모듈형 방/벽/천장/가구를 조립**합니다. 처음에는 ProBuilder를 동선과 비율 확인에만 사용하고, 최종 아트는 고품질 건축 에셋/재질/조명으로 교체합니다. 기본 큐브 그레이박스나 저폴리 소품만으로 완성 아트라고 주장하지 않습니다.

## 프로젝트 구조
- **새로 생성할 Unity 경로:** `game/UnityProject` (현재 main에는 없음; [Unity 재시작 가이드](game/UNITY_RESTART_GUIDE.md) 절차로 Unity Hub에서 생성)
- 이번 추가 소품: `art/Environment/ThirdParty/KayKitFurnitureBits/Models/`
- 라이선스: `art/Environment/ThirdParty/KayKitFurnitureBits/LICENSE.txt`
- 전체 모델 목록/원본 SHA: `art/Environment/ThirdParty/KayKitFurnitureBits/ASSET_MANIFEST.json`
- 별도 3종 UI: `art/UI/DialogueFrames/Translucent/`

**현재 새 Unity 프로젝트가 없으므로 3D 소품은 Unity Assets에 들어가지 않은 GitHub 스테이징 파일**입니다. Unity 적용을 승인할 때 OBJ, MTL, TGA를 함께 `game/UnityProject/Assets/_Game/Art/Environment/ThirdParty/KayKitFurnitureBits/`로 복사하여 임포트합니다.

## 실제 등록한 외부 자산과 라이선스
출처: **KayKit Furniture Bits 1.0 / Kay Lousberg / CC0 1.0**:
https://github.com/KayKit-Game-Assets/KayKit-Furniture-Bits-1.0
19개 모델: armchair, bed_double_A, bed_single_A, book_set, cabinet_medium, cabinet_small, chair_A_wood, chair_stool, couch, couch_pillows, lamp_standing, lamp_table, pictureframe_large_A, pictureframe_standing_A, rug_oval_A, rug_rectangle_A, shelf_A_big, table_medium, table_medium_long.

원본 **OBJ 메시 데이터는 보존**, 각 OBJ의 `mtllib` 한 줄만 공통 MTL 이름으로 조정했습니다. 원본의 PNG 색상 아틀라스는 이번에 전송하지 않았습니다. 동봉된 256×256 TGA는 조립 확인용 **임시 대체 팔레트**로, 원본과 UV 기반 색상 배치가 다를 수 있습니다. 테마별 팔레트는 자동으로 바뀌지 않으므로 Unity에서 URP/Lit Material을 따로 지정해야 합니다. 작업용 시작 자산이지 고품질 씬의 최종 질감을 보장하지 않습니다.

## 3개 테마별 8공간 고정
| 테마 | 공간 수 | 구조 | 분위기 / 추천 소품 |
|---|---:|---|---|
| HOTEL 대저택·호텔 | 8 | 1F 로비+101 마또+102 찌로+103 보키 / 2F 201 마리히+202 나즈+203 문이레+204 사건 현장 | 블랙·레드, 우드·클래식 브라스 / armchair, bed_double_A, cabinet_medium, lamp_standing |
| CRUISE 크루즈 | 8 | 3데크 5공간, 4데크 3공간. 좌현/우현, 앞뒤 계단 2개 | 블루·화이트, 금속·목재 / table_medium_long, chair_A_wood, shelf_A_big |
| MUSIC_BROADCAST 음악방송 대기실 | 8 | 단층 U자형, 중앙 공동 대기홀·좌3(A01~A03)·우3(B01~B03)·뒤 VIP-01 사건 현장 | 골드·핑크 / couch_pillows, chair_stool, lamp_table |

공간 이름/번호와 논리 roomId의 실제 대응은 `MAPS.md` 및 `game/maps/theme_room_layouts.v1.json`이 우선합니다. **복도·계단은 추가 조사 방이 아닙니다.**

## PC만의 장점 활용
- Windows x64 `BuildTarget.StandaloneWindows64`를 추후 빌드 대상 기준으로 삼음. 실제 Build Profiles 변경/빌드는 이번에 하지 않음.
- PC 키보드/마우스 기반 NPC 심문·증거 조사, 고정 쿼터뷰 카메라와 충분한 텍스트 영역.
- 로비에서 방으로 이동할 때 해당 층/실만 보이도록 벽 가림/컷어웨이 설계. 호텔 2층, 크루즈 데크 이동, 음악방송 단층은 별도 동선.
- 반복 가구는 prefab/공유 재질로 관리. 카메라 각도는 30°~45° 내려보는 쿼터뷰를 시작점으로 **시안 비교 후 결정**, 확정 고정 수치가 아님.
- PC라고 무제한 폴리곤을 쓰지 않고 조명·재질·오클루전/LOD는 사용 중 실측하여 튜닝.

## Unity에서 만드는 순서 (실행 전 지침)
1. [Unity 재시작 가이드](game/UNITY_RESTART_GUIDE.md)를 따라 Windows에서 새 Unity URP 프로젝트를 생성한 후 전용 맵 씬을 준비. 구형 Legacy는 main에 없음.
2. ProBuilder로 **동선 설계용** 8개 공간의 크기/문/계단/복도 확보.
3. OBJ+공용 MTL+색상 TGA를 Assets 하위에 복사한 뒤 모델 임포트. 방향·단위·피벗·노멀 체크.
4. 호텔 로비 **한 공간의 높은 시각 품질**을 먼저 승인받고 나머지 객실로 확장.
5. 창문·계단·문틀·벽·천장 등 **건축 에셋은 아직 미포함**. 개별 에셋을 라이선스 확인 후 추가해야 함.
6. URP/Lit 머티리얼과 색상/광택/조명을 맞추고 유사 세트간 스타일을 통일.
7. NPC 2D 스프라이트·증거 조사·입력 UI·충돌/이동 시스템을 분리하여 연결.
8. 사용자 요청 이후에만 Windows 전용 품질/성능 테스트, Play Mode, EXE 빌드 진행.

## 관련 공식 자료
- Unity Windows 빌드 설정: https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/windows/standalone-binaries
- Unity BuildTarget: https://docs.unity.com/en-us/engine/6000.6/script-reference/unityeditor/buildtarget
- 무료 고품질 CC0 모델/재질 추가 후보: https://polyhaven.com/
- KayKit 원본: https://github.com/KayKit-Game-Assets/KayKit-Furniture-Bits-1.0
