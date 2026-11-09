# mystery Edit Mode 검증 보고서

검증일 2026-11-10  
대상 브랜치 `codex/mystery-dialogue-panel-20261109`  
검증 기준 커밋 `544b4b10f28154b4647e461a1f814b012b80e886`  
로컬 Unity 프로젝트 `C:\UnityProjects\DeductionGame\mystery`

## 로컬과 GitHub의 차이

원격 `game/UnityProject/Assets/` 추적 파일 183개를 로컬 상대 경로와 바이트 단위로 비교했다. 149개 일치, 32개 누락, 4개 불일치다.

| 위치 | 확인된 문제 | 필요한 조치 |
| --- | --- | --- |
| `Assets/Interrogation/` 및 `Assets/Interrogation.meta` | Cases·Investigations JSON 6개와 Scripts C# 7개, 파일 및 폴더 메타데이터를 포함한 32개 경로가 로컬에 없다. | 원격의 해당 에셋과 .meta를 같은 상대 경로로 동기화한다. |
| `Assets/UI/Prefabs/DialoguePanel.prefab` | 로컬 파일은 이전 커밋 `ae4d24c`와 일치하며, 루트에 DialogueInterrogationAdapter·InvestigationNotebookView·InvestigationSceneView가 없다. | 최신 프리팹으로 동기화한 뒤 Unity에서 세 컴포넌트와 직렬화 참조를 다시 확인한다. |
| `Assets/UI/Scripts/DialoguePanel.cs` | 로컬 파일은 `ae4d24c`와 일치한다. 최신 테마별 대화 상태와 `ShowInterrogationNotice`가 없다. | 프리팹 및 Interrogation 에셋과 함께 최신 스크립트를 동기화한다. |
| `Assets/UI/Fonts/NotoSansKR SDF.asset`, `Assets/UI/Fonts/Pretendard-Regular SDF.asset` | 로컬 TMP 글리프 및 아틀라스 직렬화가 원격과 다르다. 폰트 참조 및 원본 폰트는 유지되어 있다. | 파일을 동기화하기 전에 로컬 SDF 변경을 보존할지 확인한다. |

Unity 6000.3.26f1 Edit Mode에서 현재 로컬 콘솔 오류 0개, 경고 0개, 스크립트 컴파일 실패 플래그 false를 확인했다. 로컬 DialoguePanel 프리팹과 열린 DialoguePreview 씬의 Missing Script는 각각 0개다. 최신 조사 코드가 로컬에 없으므로 이 콘솔 결과는 최신 코드의 Unity 컴파일 결과가 아니다. 위 동기화 후 콘솔과 프리팹을 다시 검사해야 한다.

## 원격 에셋 정적 확인

- 원격 DialoguePanel 프리팹 YAML의 루트에 심문 어댑터, 수사 수첩, 현장 조사 컴포넌트가 모두 등록돼 있다. 세 스크립트 GUID, 사건 JSON 3종, 조사 JSON 3종, 수첩 및 조사 UI의 Pretendard Regular/SemiBold 참조를 해당 .meta와 대조했다.
- 로컬 Unity에서 NPC 이름과 초상화 참조 6쌍, 초상화 비율 유지, 대화창 TMP 폰트 8곳을 확인했다. 초상화 원본 6개의 SHA-256은 매핑과 일치한다. 세 테마 프레임 PNG와 메타데이터는 원격과 바이트가 일치하며, 기본 Mansion 프레임은 Unity에서 로드된다. 원격 프리팹은 이전 초상화·프레임·대화창 텍스트 참조를 유지한다.
- 대저택 `Cases/Mansion.json`과 `Investigations/MansionInvestigation.json`의 사건 ID `mansion_01_stopped_clock_draft`와 테마 값 0이 일치한다. 장소 ID 9개, 조사 지점 ID 15개가 각각 고유하며, 증거 12개가 등록된 사건 증거와 1:1로 대응한다. 일반 관찰 지점은 3개다.
- 선행 증거 참조 3개는 모두 등록돼 있으며, 데이터의 선행 조건 그래프에 막혀 도달할 수 없는 증거는 없다. 이는 정적 데이터 검사다.
- 대저택·크루즈·음악방송 사건 JSON 모두 `approved: false`다. 크루즈·음악방송의 조사 목록은 빈 초안이다.

UI와 사건 데이터는 수정하지 않았다. Play Mode, 기능 테스트, EXE 빌드는 실시하지 않았다.
