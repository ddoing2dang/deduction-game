# 시네마틱 대화화면 Unity 컴파일 오류 재조정

**원인:** `6d1c929`에서 `DialogueCinematicView`는 `DialoguePanel`의 8개 읽기 전용 UI 참조 및 `UseExternalPortraits` 상태를 사용하지만, `DialoguePanel.cs`에 해당 API가 반영되지 않아 16개의 `CS1061`류 오류가 보고됨. 해당 저장소 커밋 파일에도 해당 멤버가 실제로 없어 **GitHub 소스 불일치**로 확인.

**수정:** 필요한 API를 `DialoguePanel`에 추가하고, 시네마틱 모드에서 원래 작은 NPC 초상화가 재노출되지 않도록 `RefreshPortrait`의 크기 변경 코드를 조건부로 분리. `DialogueCinematicView`의 OnEnable/OnDisable에서 일러스트 모드 전환 시 `ApplyPresentation()` 호출.

**씬 Missing Script:** `DialoguePreview.unity` YAML은 현재 유지하고 `MysteryEditModePreflight`에서 열려 있는 씬의 스크립트 누락 오브젝트 이름과 계층 경로를 출력하도록 추가. 컴파일이 통과한 뒤 확인하며 원인을 확인하기 전 기존 컴포넌트를 삭제하지 않는다.

**보존:** 현존하는 NPC PNG 6장, 대화 프레임 3장, TMP 폰트, 프리팹 직렬화 참조, 기존 씬, 사건 JSON 승인 잠금. 실제 Unity Play Mode/빌드/스크린샷은 수행하지 않았다.

## 로컬 확인

1. 동기화 대상: `Assets/UI/Scripts/DialoguePanel.cs`, `Assets/UI/Scripts/DialogueCinematicView.cs`, `Assets/Interrogation/Editor/MysteryEditModePreflight.cs` (세 C# 파일).
2. 로컬 SDF 폰트 두 파일, 씬의 미저장 편집, 기존 이미지 및 프리팹은 보존.
3. Unity Edit Mode 컴파일 결과, `Missing Script` 개수 확인.
4. 메뉴 `Mystery > Verification > Inspect Current Project (Edit Mode)`에서 누락 컴포넌트가 있으면 정확한 계층 경로 보고.
5. 컴파일 통과 후에만 Play Mode 진입 여부 결정. 맵이 없다면 실제 맵 블러 테스트는 여전히 미검증으로 표시.
