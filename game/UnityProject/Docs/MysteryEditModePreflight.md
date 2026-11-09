# mystery — Unity Edit Mode 출시 전 정적 점검

> **2026-11-10 · 준비 완료된 검사 도구, 아직 실행하지 않음**
>
> 이 문서는 실제 Unity 검증 완료 보고서가 아니다. 로컬 Unity 프로젝트가 최신 GitHub 커밋을 동기화한 후 검증 메뉴를 실행해야 한다.

## 작업 범위

GitHub 브랜치 `codex/mystery-dialogue-panel-20261109`에 읽기 전용 Edit Mode 점검 메뉴 추가:

`Mystery > Verification > Inspect Current Project (Edit Mode)`

- 새로운 프리팹·씬·맵을 생성하거나 기존 설정을 자동 교체하지 않는다.
- 검사 결과를 Unity Console 한 곳에 요약한다. 성공 항목은 `[OK]`, 문제는 `[FAIL]`, 미승인 사건은 `[INFO]`로 구분한다.
- **플레이 시작, 테스트 실행, EXE 빌드, 기존 파일 쓰기 없음.**

## 점검 항목

1. `Assets/UI/Prefabs/DialoguePanel.prefab`의 루트에 기존 `DialoguePanel` 외에 `DialogueInterrogationAdapter`, `InvestigationNotebookView`, `InvestigationSceneView`, `CaseProgressSaveController`, `CaseConclusionView`가 있는지
2. 프리팹의 Missing Script, 기존 주요 UI 필드 12개, 3테마 원본 프레임, TMP 텍스트 8곳과 폰트 참조
3. 여섯 NPC의 이름·순서·Sprite 연결과 `Docs/NPCPortraitMapping.json`에 기록된 **원본 PNG SHA-256 6개**
4. 사건 `Mansion/Cruise/MusicBroadcast.json`과 조사 `*Investigation.json` 등 **6개 JSON 에셋**의 프리팹 연결과 사건 ID·테마 일치 여부
5. 대저택 사건의 진술 유효성·범인 전용 거짓말 규칙·필수 지목 증거·조사 지점-증거 연결
6. 대저택 **9장소, 15조사 지점, 12증거, 13심문 주제, 78진술** 수량과 증거 선행 조건을 만족하며 모든 증거에 도달 가능한지
7. 세 사건의 `approved: false`를 **문제로 간주하지 않고** 승인 전 게임 잠금이 유지된 상태라고 안내

이 검사는 **정적 구조와 파일 무결성**만 검사한다. 문장에 거짓 정보가 없는지, 게임에서 화면이 겹치는지, 자동 저장이 실제로 복원되는지, IME가 제대로 입력되는지까지 증명할 수 없다.

## 로컬에서 확인하는 순서

1. GitHub의 **최신 브랜치 소스**를 `C:\UnityProjects\DeductionGame\mystery`에 동기화한다. 기존에 로컬 보존한 `NotoSansKR SDF.asset`, `Pretendard-Regular SDF.asset`는 **덮어쓰지 않는다**.
2. 기존 편집 중인 Unity 씬이나 수동 편집 내용을 버리지 않는다.
3. Unity Editor가 C# 재컴파일·에셋 임포트를 마치면 Play 버튼을 누르지 않고 Console에서 오류·경고 개수를 확인한다.
4. 메뉴 `Mystery > Verification > Inspect Current Project (Edit Mode)`를 한 번 실행한다.
5. Console의 `[mystery Edit Mode preflight]` 출력에서 `failed=0`인지, 추가 문제 항목이 있는지 확인한다.
6. 수정 내용 또는 차이점은 **로컬 검증 보고서**에 기록한다. 새 코드는 GitHub만 변경했으므로 현 시점에 위 검사가 실제로 통과했다고 주장하지 않는다.

## 승인 및 후속 단계

- 대저택 사건은 **잠정 범인 진루나**, 사건별 12개 증거, 출입 기록·G3 발급 대장·흙 발자국·회계 사본 4종을 근거로 한 초안이다.
- `approved: false`는 유지한다. 현재 Play Mode를 실행해도 수사 기능은 잠겨 있으며, 미승인 데이터를 임의로 플레이용으로 전환하지 않는다.
- 스토리·알리바이·대사·결말에 대한 **사용자 승인 후에만** `approved: true`로 변경한다.
- 기능 검증/Play Mode와 EXE 빌드는 **별도 명시 요청 전까지 진행하지 않는다**.
- Unity 3D 맵은 사건 데이터 승인과 시스템 검증 이후 제작 순서에 둔다.

## 이번 커밋의 범위

`Assets/Interrogation/Editor/MysteryEditModePreflight.cs`와 `.meta`, `Editor.meta`, 이 문서, README 문구만 추가·갱신했다. 기존 대화창 프리팹·기존 스크립트·원본 PNG·원본 폰트·사건 JSON은 수정하지 않는다.
