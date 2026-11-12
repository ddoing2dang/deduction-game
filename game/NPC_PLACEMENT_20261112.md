# 사건파일 No.464 — 장미대저택 6명 캐릭터 배치 단계 (2026-11-12)

## 실제 저장 상태

- **GitHub 코드 저장:** `Assets/Case464/HotelNpcActor.cs`, `HotelNpcClickProxy.cs`, `Assets/Editor/HotelNpcPlacementBuilder.cs`, 기존 `HotelMapCameraController.cs`의 읽기 전용 현재 방/지역 접근자.
- **사용자 원본 확보:** `NPC_Sprites_6_Characters(1).zip` (별도 ChatGPT Library). 원본에서 `MapIdle_N/NE/E/SE/S/SW/W/NW.png` 8개씩 × 6명 = **48장**을 동일 바이트로 추출한 **Unity 배치용 ZIP** 생성. Unity import 메타 포함.
- **중요한 구분:** 48장의 이미지 바이너리 및 생성된 NPC 배치 씬은 **아직 GitHub에 업로드되지 않았음**. GitHub에는 씬 생성에 필요한 코드만 저장되어 있다. 사용자 로컬 Unity에서 아래 절차로 생성·저장해야 한다.
- **실행/검증:** Play Mode, 테스트, 빌드 및 실제 에디터 씬 생성은 수행하지 않았다.

## NPC 지정 (확정 roomId 유지)

| 층 | 객실 | npcId | 표시 이름 | 대화 번호 |
|---|---|---|---|---|
| 1F | 101 | MATTO | 마도은 | 0 |
| 1F | 102 | JJIRO | 진초록 | 4 |
| 1F | 103 | BOKI | 김보희 | 3 |
| 2F | 201 | MARIHI | 마혜인 | 1 |
| 2F | 202 | NAZ | 나지수 | 2 |
| 2F | 203 | MOONIRE | 문이현 | 5 |

`LOBBY`는 탐정 시작점, `CRIME_SCENE`(204)는 사건 현장으로 유지. NPC 배치 단계에서 7번째/8번째 캐릭터를 만들지 않는다.

## 최신 맵에 **직접** 적용하는 명령 (2026-11-12 추가)

1. 최신 GitHub `main`을 받는다.
2. 필요하면 `장미대저택_6인_맵스프라이트_배치용.zip`의 `Assets/`를 로컬 `game/UnityProject/Assets/`로 병합한다.
3. Unity **Edit Mode**에서 실제 사용 중인 **최신 대저택 .unity 씬**을 열고 `Ctrl+S`로 저장한다. (반드시 해당 씬이 활성 씬이어야 한다.)
4. 메뉴 `Case464 > Hotel NPC > Apply 6 NPCs to OPEN Latest Map (Edit Mode)` 실행.
5. 이 메뉴는 **현재 열려 있는 씬**의 `HotelFinalPolishLayout`, `HotelMapCameraController`, 6개 객실 `HotelOverviewRoomArea`를 읽고 원본 48장의 8방향 스프라이트로 NPC들을 배치한다.
6. **현재 씬 이름 옆에 `_Before_NPC_Placement.unity` 백업**을 자동 저장한 후, 현재 열린 .unity 씬 자체에 NPC 6명을 추가해 저장한다. 씬에 이미 `HotelNpcActor`가 있으면 중복 추가하지 않고 중단한다.
7. 맵이 바뀌어 필수 객실/층 정보가 맞지 않거나 스프라이트가 누락된 경우 임의 좌표로 배치하지 않고 중단한다. 에디터의 Console에 구체적인 오류가 표시된다.

**주의:** 명령을 만든 코드는 GitHub에 업로드되었으나 **사용자 로컬 Unity에서 실제로 실행되지는 않았다**. 위치·클릭·빌드 기능 검증은 별도 요청 전 수행하지 않는다. 기존 `Build 6 NPCs in Separate Scene` 메뉴는 이전 기능이고, 최신 맵 직접 적용은 위의 `Apply ... OPEN Latest Map` 메뉴를 써야 한다.

## Unity 적용 — 원본 맵 보호

1. 준비된 **장미대저택_6인_맵스프라이트_배치용.zip**의 `Assets/`를 로컬 `game/UnityProject/`에 병합한다. 최종 에셋은 `Assets/Case464/NPC/MapIdle/{npcId}/MapIdle_{direction}.png`.
2. GitHub 최신 `main`으로 스크립트를 동기화한다.
3. Unity Editor Edit Mode에서 다른 씬의 저장하지 않은 변경이 없는지 확인한다.
4. 메뉴 **Case464 → Hotel NPC → Build 6 NPCs in Separate Scene (Edit Mode)**.
5. 이 메뉴는 승인된 `Hotel_RoseBlack_Final_Map.unity`를 연 다음 `HotelFinalPolishLayout.rooms` 및 `HotelOverviewRoomArea`의 확정 룸 정보로 6명의 발 위치를 구한다. 공간 매칭에 실패하면 임의 위치로 배치하지 않고 중단한다.
6. 원본 맵을 건드리지 않고 **`Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Characters.unity`** 이름으로 새 씬을 저장한다. 기존에 동일 결과 씬이 있으면 **덮어쓰지 않는다**.
7. 새 씬을 원하는 게임 시작 씬으로 연결하는 작업/실행 확인은 별도 단계다. 만들어진 씬과 48개 원본 PNG·메타를 함께 GitHub에 커밋해야 다른 환경에서도 재현된다.

## 적용 기능 범위

- 6명 모두 8방향 **정지형 SpriteRenderer** 사용. PPU 256, 발 중심 피벗 `(0.5, 0.0625)`.
- 개인실 내부의 정해진 입구·바닥 `floorBounds` 기준으로 배치. 별도 초기 수동 배치 좌표를 하드코딩하지 않는다.
- 고정된 NPC 소속 방/층을 유지하고 화면 모드(전체 분리 보기·층별 보기·방별 보기)에 맞춰 실제/디스플레이 스프라이트를 전환.
- NPC 클릭 시 해당 용의자의 `DialogueCinematicView`를 여는 **후크만** 추가. 실제 게임 UI가 맵 씬에 로드되어야 동작하며 심문 대사 승인·사건 판정을 우회하지 않는다.
- 걷기/달리기, 실제 이동 AI, 사건마다 NPC 이동, 조사용 UI 전환 및 플레이 확인은 **미구현**.
- `Assets/UI/Art/NPCPortraits/RoseMansion`에 저장한 별도의 6명 기본 대화 초상화는 변경하지 않는다.

## 다음 연결

새 씬을 Unity 에디터에서 제작·검토한 다음, 탐정 시작 위치 → NPC 상호작용 → 실제 심문 UI → 증거 획득/사건 상태 연결 순으로 진행한다. 기능 테스트·빌드는 명시 요청 전 수행하지 않는다.

## NPC가 바닥에 누워 보이는 문제 수정 (2026-11-12)

- 원인: `HotelNpcActor.SyncView()`가 `Quaternion.LookRotation(-camera.forward, camera.up)` 전체 빌보딩을 사용해, 거의 수직인 호텔 카메라에서 PNG의 XY 평면이 바닥과 거의 평행해짐. 좌표 자체가 바닥 아래로 잘못 간 문제가 아님.
- 수정: `HotelNpcActor.StandingBillboard()`로 화면 위쪽 방향을 따르되 세계 Y 상향 성분을 보존하고, 카메라 쪽 기울어짐을 최대 45도로 제한. 수직 카메라에서는 완전 직립 2D PNG가 모서리로만 보여 사라지므로 약간 비스듬한 2.5D 자세를 사용.
- 기존 6명 좌표, 객실, 원본 48개 PNG 및 .meta, 배치된 씬, 기존 가구·조명·카메라 구조를 변경하지 않음.
- **적용 시 재배치 메뉴를 실행하지 말 것**: 새 코드를 GitHub에서 pull한 뒤 최신 `Hotel_RoseBlack_Final_Map_RoomSigns_WideAisle.unity` 씬을 Unity Edit Mode에서 열어 스크립트를 리로드하고 새 스프라이트 회전을 확인·저장.
- 이 패치는 **소스 코드 변경만 저장**. Unity 에디터 렌더 확인 및 저장은 로컬 실행 필요. Play Mode/기능 테스트/빌드 미실행.
