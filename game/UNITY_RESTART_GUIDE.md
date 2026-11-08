# 새 Unity 프로젝트 시작 가이드 (기존 구현 전체 초기화)

**현재 상태 (2026-11-09):** 기존 `game/UnityProject/` 구형 프로젝트 파일 **18개를 GitHub main에서 제거**함. Unity 씬·C# 자동 생성/카드/턴/구형 맵·프로젝트 설정은 재사용하지 않는다.

## 완전한 되돌리기 지점
- 기존 코드의 삭제 전 상태는 GitHub 브랜치 [`archive/pre-fresh-unity-project-20261109`](https://github.com/ddoing2dang/deduction-game/tree/archive/pre-fresh-unity-project-20261109)에 **전체 저장**되어 있다.
- GitHub 기록에서 과거 파일 조회/복구 가능. **사용자 Windows PC에 존재하는 로컬 폴더나 Unity Hub 등록 내용은 원격 GitHub 작업으로 삭제되지 않는다.**
- 새 프로젝트를 만들 때 이전 `ProjectBootstrap.cs`, `GameManager.cs`, `Game.unity`, 카드/게임턴 로직을 가져오지 않는다.

## Unity Hub에서 새 프로젝트 생성 (Windows)
1. 기존 프로젝트가 실행 중이면 저장 후 Unity Editor를 닫는다. 로컬에 별도로 필요한 것이 있으면 백업한다.
2. GitHub 저장소의 변경 사항을 받는다. 로컬에 옛 `game/UnityProject` 폴더가 남아 있으면 삭제하거나 저장소 밖으로 옮긴다. 확실히 분리하고 싶다면 저장소를 새 디렉터리에 다시 clone해도 된다.
3. Unity Hub → **New Project** → **Universal 3D (URP)** 템플릿 → Unity Editor **6000.3.26f1** (로컬 설치 버전/호환성에 따라 새 버전 선택 가능).
4. **Project Name: `UnityProject`**, **Location: `<repository-root>/game`**. 결과 경로가 `<repository-root>/game/UnityProject`인지 확인한다. Unity Hub가 프로젝트를 만들기 전 해당 대상 폴더가 비어 있어야 한다.
5. 생성 직후 구형 Game 씬을 불러오지 않는다. 신규 씬 `Assets/Scenes/DialoguePrototype.unity`를 처음부터 작성한다.
6. Unity가 생성한 신규 `Assets`, `Packages`, `ProjectSettings`를 Git에 추가한다. `Library`, `Temp`, `Obj`, `Logs`, `Builds`, `UserSettings` 등 생성 캐시/빌드는 Git에 올리지 않는다.

## 현재 확정한 데이터·아트 (지우면 안 됨)
- 원본 대화창 3개: `art/UI/DialogueFrames/ThemeDialogue_v1/Mansion.png`, `Cruise.png`, `MusicBroadcast.png` (2048×682 투명 PNG).
- NPC 표시 이름: `game/characters/npc_display_profiles.v1.json`; 은발 마도아 / 금발 마리예 / 갈색 나주아 / 빨강 복희 / 연두 지로아 / 보라 문이린.
- 테마별 공간: `game/maps/theme_room_layouts.v1.json`; 테마마다 공동 로비, NPC 개인실 6, 사건현장 1 = **총 8 조사 공간**.
- 흉기/사건 초안: `game/cases/`, `game/dialogue/`; 기존 원본 대사 714개와 선별 480개는 보존, 시범 사건은 실제 게임 검증·승인 전에는 사용 불가.
- 범인만 의도적 허위 진술: `game/cases/culprit_only_deception_policy.v1.json`.
- NPC 원본 스프라이트 ZIP은 예전 채팅 첨부에서 확인·작업 설계 저장한 상태이며, **NPC 개별 PNG 102개는 아직 GitHub 저장소 안에 업로드되지 않음**. 원본 ZIP/임포트 패키지가 별도로 필요하다.

## 다음 작업 순서
1. 신규 Unity 프로젝트를 Unity Hub로 만든다.
2. **독립 대저택 대화창 프리팹**을 신규 씬에 제작: 원본 투명 PNG + 이름표 TextMeshPro + 대사 TextMeshPro + 자유 질문 TMP_InputField + 이전 대화 스크롤.
3. NPC 이름 6개와 테마 3개 프레임 교체 로직을 연결한다. 원본 대화창의 테두리·투명도 재가공/변형 금지.
4. 실제 게임용으로 검증된 사건 진실·증거 게이트·NPC 지식별 대사 선택을 연결한다. 범인 1명만 거짓말, 무고한 5명은 진실만 말함. AI는 승인된 대사 ID만 선택한다.
5. 맵 8개/3테마 2.5D 디오라마, 인물 및 수사 시스템을 순차 구현한다.

**Unity Play Mode·기능 테스트·EXE/APK 빌드/배포는 별도 사용자 요청 전까지 실행하지 않는다.** 본 문서는 신규 프로젝트의 작업 기준이고 Unity Hub 프로젝트 생성을 원격 GitHub에서 대신 실행한 것은 아니다.
