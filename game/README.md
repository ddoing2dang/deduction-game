# 새 Unity 대화창 UI 프로젝트 — 기존 맵 완전 배제

## 현재 GitHub 브랜치 구성
- 기존 맵 스크립트, 맵 오브젝트, 맵 JSON, 환경/가구 에셋, 오래된 웹 게임 및 옛 문서 전부 제거.
- 새 Unity 씬 `Assets/Scenes/DialoguePrototype.unity`의 루트는 **Main Camera / DialogueCanvas / EventSystem**뿐입니다. `DialogueCanvas`에 `DialoguePanel.prefab`가 연결되어 있습니다.
- 기존 `Assets/Data/Preserved/`(맵·사건·대사·구형 프로필 사본) 전체 제거.
- 프리팹에 필요한 **이름 목록 6개만** 새로 `Assets/Data/UI/NpcNames.json`으로 작성. 옮긴 TextAsset의 Unity 메타 GUID를 유지하여 씬/프리팹 직렬화 참조를 유지합니다.
- 대저택·크루즈·음악방송 대화창 PNG는 **원본 그대로**. 한국어 TMP 폰트·입력·대화 기록·NPC 전용 거짓말 게이트는 유지하며 실제 사건/AI 어댑터는 미연결.
- 새 Unity 프로젝트에는 기존 그레이박스, 방, 맵 데이터가 없습니다.

## 로컬 Unity가 아직 옛 맵을 보여줄 때
1. Unity Editor 창에서 프로젝트 경로를 확인하세요. **새 프로젝트:** `C:/UnityProjects/DeductionGame/fresh-dialogue/game/UnityProject`.
2. 먼저 기존 로컬 미커밋 작업이 있다면 백업하고, GitHub 작업 브랜치 `codex/fresh-unity-dialogue-prototype-20261109`의 최신 변경을 받으세요. `main`과 작업 브랜치를 섞지 마세요.
3. Unity Project 패널에서 **`Assets/Scenes/DialoguePrototype.unity` 더블클릭**. Hierarchy 루트가 `Main Camera`, `DialogueCanvas`, `EventSystem`인지 확인하세요.
4. 여전히 `Mansion_Graybox`, `Room_01` 등 구형 오브젝트가 있으면 다른 씬이나 다른 Unity 프로젝트가 열려 있는 것입니다.
5. Scene 창의 바닥 **격자선은 실제 맵이 아닙니다**. 기본 Scene 뷰 편집용 안내선입니다.

과거 PC 로컬 프로젝트를 원격 작업으로 삭제하거나 변경하지 않았습니다. 기능 테스트/Play Mode/EXE 빌드를 진행하지 않았습니다.
