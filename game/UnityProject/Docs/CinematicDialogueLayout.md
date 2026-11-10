# 시네마틱 대화 화면 — 맵 흐림, 좌/우 일러스트, 반응형 글꼴

2026-11-10 · `mystery` Windows PC · GitHub 소스/프리팹 변경

## 화면 의도

- **기존 맵**은 유지한다. 대화 시작 시 `Camera.main` 또는 Inspector 지정 `worldCamera`에서 **현재 보이는 3D 장면을 저해상도 RenderTexture로 한 장 캡처**한다.
- `Mystery/DialogueMapBlur` 셰이더가 캡처에 가로·세로 블러를 적용해 전체 화면의 뒤에 둔다. 블러 캡처 불가능/카메라 없음 시 기존 월드 위에 반투명 어둡게 처리만 하고 경고를 기록한다.
- **왼쪽:** 플레이어의 전용 일러스트. 현재 저장소에는 플레이어 아트가 없으므로 `DialogueCinematicView.playerIllustration`은 비어 있다. **NPC 그림을 플레이어로 재사용하지 않는다.** `SetPlayerIllustration(Sprite)`로 나중에 연결할 수 있다.
- **오른쪽:** 기존 `DialoguePanel.speakerPortraits`의 **원본 NPC 일러스트 6종** 가운데 현재 대화 상대. 이전 대화창 안 작은 원본 이미지 컴포넌트는 삭제하지 않고 시네마틱 모드에서만 숨긴다.
- **아래:** 기존 `Frame`의 테마별 PNG 3종을 **원본 비율 그대로 유지**한 채 아래로 내린다. NPC 이름·대사는 그림 아래에 배치하고 질문 입력창 및 전송 버튼을 하단에 정렬한다.
- **상단 중앙:** NPC 대화 기록을 약 760px 폭으로 줄여 양쪽 일러스트와 겹치지 않게 한다. 기록 길이에 따라 스크롤 콘텐츠 높이를 늘린다.
- **글꼴:** 기존 MaruBuri + Pretendard 연결을 보존하며 프리팹의 TMP 텍스트 8곳을 **자동 크기 조절과 줄바꿈**으로 설정한다. 1920x1080 기준 Canvas 스케일을 존중해 1280x720 등에서도 크기를 유동적으로 조절한다.
- **기타 기능:** 심문, 수사 수첩, 현장 조사, 저장, 사건 결말 기존 데이터 및 팝업 이벤트 보존.

## 주요 변경 파일

| 파일 | 변경 |
| --- | --- |
| `Assets/UI/Scripts/DialogueCinematicView.cs` | 맵 카메라 캡처/블러, 좌우 일러스트 표시, 레이아웃/폰트 반응형 처리, 대화 시작/닫기 API |
| `Assets/UI/Art/DialogueMapBlur.shader` | 2패스 가우시안 블러 가로/세로 |
| `Assets/UI/Scripts/DialoguePanel.cs` | NPC 스프라이트 조회·시네마틱 전환 및 대화 기록의 콘텐츠 높이 자동 확장 |
| `Assets/UI/Prefabs/DialoguePanel.prefab` | `DialogueCinematicView` 직렬화 연결, 기존 대화 프레임·입력·기록 위치 정돈, TMP 8곳 자동 크기 조절 |
| 원본 NPC PNG·프레임·폰트 | **미변경** |

## 실제 맵에서 호출 방법

- 새 대화를 시작할 때: `GetComponent<DialogueCinematicView>().OpenWithNpc((DialogueSpeaker)npcIndex)`
- 종료할 때: `GetComponent<DialogueCinematicView>().CloseConversation()`
- 실제 플레이어 일러스트 연결: Inspector의 `Player Illustration` 필드 또는 `SetPlayerIllustration(sprite)`
- 메인 카메라가 `MainCamera`로 태깅되지 않았다면 Inspector `World Camera`에 현재 맵 카메라 연결
- `visibleOnStart`은 지금 **DialoguePreview 미리보기를 위해 true**. 실제 맵에 프리팹을 배치할 때는 false로 지정하고 NPC 상호작용에서 `OpenWithNpc`를 호출한다.
- 카메라 캡처는 **대화 시작 순간 장면 한 장**을 쓰는 설계다. 대화 중 3D 맵이 실시간으로 움직이는 영상 블러는 아직 지원하지 않는다.

## 미완료와 검증 경계

1. **플레이어 전용 PNG가 없다.** 현재 슬롯은 만들어졌으나 그림이 지정되기 전까지 왼쪽은 비어 있다.
2. **저장소에는 실제 월드 맵 씬이 없다.** `DialoguePreview.unity`의 카메라로는 맵이 아니라 미리보기 배경이 캡처된다. 로컬 프로젝트의 실제 맵에서 `worldCamera` 연결을 확인해야 한다.
3. Camera.Render 기반 캡처는 실행 중 렌더 파이프라인 환경에 따라 달라질 수 있다. 미지원 환경이면 자동으로 흐림 대신 반투명 배경이 표시된다. 실제 장면에서 한 번 확인이 필요하다.
4. **로컬 Unity Editor/Play Mode/컴파일 검증을 이번 채팅에서 실행하지 못했다.** 원격 GitHub 코드와 YAML 정적 확인만 가능하다. 따라서 실제 1280x720 화면 스크린샷도 아직 없다.
5. 사건 승인 상태는 `approved: false`로 유지. 대화 화면 개편은 사건 내용을 승인하거나 진술을 해제하지 않는다.
6. EXE 빌드는 요청이 없으므로 수행하지 않는다.

## 1회 실제 화면 확인 가이드

1. 로컬 프로젝트에 최신 GitHub 커밋 반영. **로컬 변경 TMP 폰트 2개와 미저장 씬 수동 편집 보존.**
2. Unity Edit Mode 컴파일 오류와 프리팹 Missing Script 검사.
3. 실제 맵 씬에서 `DialoguePanel`을 표시할 때 `World Camera`를 연결하고, 테스트할 대화 상대를 마도연으로 선택.
4. **Play Mode 1회** 진입해 화면에 현재 맵의 흐린 배경, 우측 NPC, (지정 시) 좌측 플레이어 이미지, 하단 대화창, 글꼴 크기를 확인.
5. 반드시 실제 **Unity Game 탭의 대화 상태**를 캡처하고 사진을 전달. 이미지가 기본 화면만 저장됐다면 **대화 배치 검증은 실패/미검증**으로 보고하고 임의 합성 화면을 제출하지 않는다.
6. Play Mode 종료 후 오류/경고와 실제 스크린샷을 보고. 기능 테스트, 빌드는 추가로 실행하지 않는다.
