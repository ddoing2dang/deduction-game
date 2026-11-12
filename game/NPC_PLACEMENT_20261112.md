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
