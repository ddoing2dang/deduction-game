# 2026-11-10 진행도 복구·구형 데이터 분리·연결 확인

> GitHub 원격 저장소의 `main`을 기준으로 실제 파일 구조를 검사한 기록. **사용자 Windows의 로컬 Unity 에디터/작업 폴더에 접근하거나 삭제한 기록이 아님**.

## 1. 사용자 요청 해석 및 기준
- 2026-11-07 **이전 구형 게임 시스템의 활성 소스**는 제거/재도입 금지.
- 새 시스템에서 재사용하는 기존 제작 자산은 생성일이 이전이라도 **보존**: 캐릭터 초상/액세서리, 테마별 대화창·카드 프레임, 재사용 CC0 가구와 건축 자산.
- 백업 브랜치/과거 Git 커밋은 삭제·force-push하지 않는다. 기존 정본을 잃으면 새 시스템 관련 자료까지 소실될 수 있으므로 **활성 브랜치와 백업 이력을 구분**한다.
- 사용자가 제공한 시네마틱 참고 이미지 자체는 저작권상 GitHub에 재업로드하지 않고, 해당 UX만 독자 스타일의 화면 계약으로 기록.

## 2. 실제 GitHub main 조사 결과(작업 전)
- 기본 브랜치 `main`은 Unity 초기화 후 `README.md`, `art/`, `.gitignore`만 포함함.
- 즉 구형 6인 PvP, 카드, 턴, 웹/모바일 빌드 및 `game/UnityProject/` 자체가 **이미 main에 없음**. 새롭게 제거할 활성 구형 Unity 코드가 발견되지 않음.
- 메인에 남은 `art/AccessoryLibrary`, `art/NPC/LegacyPortraits`, `art/UI`, `art/Environment`는 새 게임 제작/참고용으로 보존. 원본 자산 직접 수정 금지.
- 환경 모델 206종, 24 공간별 차별화 설계와 테마별 대화창 원본 3종은 GitHub에서 파일 존재 확인. **Unity import/실행 상태는 확인 불가**.

## 3. 진행도 복구 대상
- 백업 기준: `archive/pre-clean-slate-latest-20261109` (커밋 `5326b2088aff441ebac3528ca9c44e92188f4e35`).
- 신규 **1인 탐정 + NPC 6명 + 3테마/8방 + 12종/18실물 + 범인만 거짓말 + 자유 한국어 심문 + 3축 고발** 관련 문서·JSON 총 **43개**를 main에 비파괴적으로 복구하는 작업.
- 포함: `GAME_RULES.md`, `UI_FLOW.md`, `ARCHITECTURE.md`, 3테마 레이아웃과 캐릭터/자연어/랜덤 사건/증거/흉기 문서; `game/cases/`, `game/dialogue/`, `game/maps/`, `game/characters/`, 새 Unity 재시작 가이드, NPC 연결 참조.
- 제외: `game.js`, `index.html`, `style.css`, `web/`, `web/mobile/`, `MULTIPLAYER.md`, `LEGACY_IMPLEMENTATION.md`, 이전 `.github/workflows/`의 모바일/구형 빌드 관련 작업, 구형 Unity 씬·C# 자동 생성기.
- 228개 기본 대사 + 486개 조건부 초안의 원본, 480줄 선별본, 세 테마의 구체적 시범 사건 자료는 백업 블롭을 **바이트 변경 없이** 복구. 검수/승인 전 플레이 진술 적용 금지.
- 새 `UI_CINEMATIC_PRESENTATION_20261110.md` 및 `game/ui/scene_presentation.v1.json`으로 장면 전환·연출·권한을 데이터 계약으로 정의한다.

## 4. 연결 상태: 확인 범위 분리
| 항목 | 상태 | 근거 |
|---|---|---|
| GitHub 저장소 읽기/쓰기 | 연결됨 | 사용자의 GitHub 권한으로 main 파일 구조와 백업 검사 |
| 신규 사건·NPC·맵·대사 데이터 참조 | GitHub 파일로 존재 | 복구 대상의 경로 및 Blob SHA 확인 |
| 3테마 대화창·6인 레거시 초상·3D 환경 파일 | GitHub 보관 | 현재 main 자산 트리 확인 |
| Unity Hub / 로컬 Windows 에디터 | 연결 확인 불가 | 현재 세션에서 PC 로컬 Unity 파일 또는 에디터에 직접 접근하는 연결 제공되지 않음 |
| 새 Unity 프로젝트 / 씬·프리팹 / 패키지 설정 | main에 없음 | Unity 초기화 README 및 현재 트리 |
| NPC AI 검색기·사건 생성기·시네마틱 씬의 런타임 연결 | 미완료 | 설계·데이터 상태와 실제 Unity 구현을 구분 |
| 기능 테스트, Play Mode, EXE 빌드 | 미실시 | 사용자 추가 요청 없으므로 실행 안 함 |

## 5. 다음 연결 지점
- `game/UnityProject`의 새 Unity 6 URP 프로젝트가 실제로 생성되고 GitHub에 포함되어야 로컬/Unity 씬 연결 상태를 검증할 수 있음.
- 우선 `HOTEL` 테마의 대화 장면 한 개를 새 프로젝트에 연결하고, 세 개 대화창 이미지/6 NPC 이름/승인된 대사 선택을 점진적으로 결합.
- 심문 데이터는 `culprit_only_deception_policy` 및 `evidence_driven_dialogue_cases`의 승인을 거쳐 적용. AI가 즉흥적으로 사건 사실을 만들게 하지 않음.
- 사용자 명시 요청 이전에는 Play Mode/테스트/새 EXE 배포 금지.


## 2026-11-10 추가 — Unity 고품질 맵 도구/연결 작업
- GitHub `main`에 `tools/windows/Connect-Case464Unity.ps1` 추가: 현재 Unity 프로젝트를 새 GitHub 작업 폴더로 **비파괴·기본 dry-run** 복사하도록 설계. 실제 PC에서 실행 안 함.
- `tools/windows/Check-Case464Readiness.ps1` 추가: Git origin/로컬 Unity 설정/패키지/씬·메타/아트 준비 상태 확인용 **읽기 전용 도구**. 실행 안 함.
- `tools/unity/Case464MapWorkshop.cs` 추가: 24개 미술 작업용 blueprint 자산 생성, 승인 프리팹 프리플라이트, 새 additive 리뷰 씬 생성용 C# Editor 도구. **현재 Unity에서 컴파일/실행하지 않아 미검증**.
- `UNITY_MAP_MASTERPLAN_20261110.md` 추가: CoplayDev Unity MCP 설치, GitHub 연결, glTFast/ProBuilder/Blender, 고품질 쿼터뷰 PBR·조명·24방 제작 기준.
- 사용자 PC의 **실제 열려 있는 Unity 프로젝트 루트 경로/Unity MCP 활성 연결/패키지/씬 화면**은 확인하지 못함. 준비물 확보·로컬 읽기 검사 완료 전에는 연결 성공 주장 금지.
- 206개 기존 저폴리 CC0 에셋은 원본 그대로 보존. 고품질 완성 맵·24개 실제 Unity 씬은 아직 없음.
- 반복 검증 실패 2회 시 중단·오류 기록·부족한 준비물 요청·복구 경로를 작성. Play Mode/EXE 빌드는 수행하지 않음.


## 2026-11-10 이미지 참고 기반 컷어웨이 맵 방향 적용
- 사용자 제공 TV 세트 참고 이미지 4장의 **고각 지붕 제거, 실내 방 구조, 검은 바깥 배경, 층별 전시형 구도**를 독자 아트 기준으로 정리: `art/Environment/CUTAWAY_DIORAMA_MAP_REFERENCE_20261110.md`.
- `game/maps/cutaway_camera_presentation.v1.json` 저장: 전체맵/층별/방별/조사/대화·추궁 상태, 기본 고도각 60° 및 테마별 8방 수 제약.
- `tools/unity/Case464MapWorkshop.cs`의 승인 프리팹 리뷰 카메라를 60° 고각·고정 검은 외부 배경 및 조정 가능한 시점 파라미터로 변경. **사용자의 로컬 Unity에서 컴파일/실행·시각 확인 미실시**.
- 맵 시각 참고용 스틸은 GitHub에 재업로드하지 않았음. 공식 캐릭터·장면·방/사건 데이터는 유지.
- 실제 Unity 프로젝트 및 MCP 연결은 아직 확인되지 않아 프리팹/씬/게임 진행도에는 영향 없음.


## 2026-11-10 Codex Desktop Unity MCP tool visibility blocker
- GitHub `main` includes local user's Unity project under `game/UnityProject/`, uploaded by user at commit `483a29c6dbc3887aed6b377c3ce857eeb6748c66` (remote verified at the time).
- User's Unity MCP for Unity v10.3.0 showed `Session Active (UnityProject)` and `Codex: Configured`, server URL `http://127.0.0.1:8080/mcp`.
- On Windows `codex mcp list` (via `codex.cmd`) confirmed `unityMCP` registered, status `enabled`, auth `Unsupported` (Auth display does NOT imply connection failure).
- In Codex Desktop, native Unity MCP tools count: **0**. Resource lookup returned `unknown MCP server 'unityMCP'`. User stopped after repeated inability to discover tools, per 2-fail stop policy.
- This is **not proof of a Unity project or scene error**. Actual remote-editor path, active scene and Console remain unverified. No Play Mode, build, scene modification or functional tests performed.
- Independent public issue reports describe Codex Desktop HTTP MCP failing to expose Unity tools, and Codex Desktop dropping tools that CLI can discover: https://github.com/CoplayDev/unity-mcp/issues/1193 ; https://github.com/openai/codex/issues/51920. **Root cause for this user's exact configuration is not proven.**
- Next permitted step: **switch MCP client environment, not retry the same Desktop session**. Prefer an existing standalone Codex CLI using `codex.cmd` and the same registered `unityMCP` server; check only the tool list/Unity project path. If native tools still unavailable, STOP and prepare a supported alternate client or vendor fix. No ad-hoc PowerShell HTTP or unapproved public tunnel.
- Last verified project checkpoint before this documentation entry: `483a29c6dbc3887aed6b377c3ce857eeb6748c66`. On resumption, review current branch tip before any project changes.


## 2026-11-10 오후 — Unity MCP CLI 직접 조회 성공 (사용자 제공 Codex CLI 결과)
- 이전 Codex Desktop 도구 0개 문제는 **Codex CLI에서 별도로 연결 성공**. 같은 Desktop 오류를 다시 해결 대상으로 삼지 않는다.
- Codex CLI가 `unityMCP`로 실제 읽기 전용 조회 수행. **프로젝트 경로 일치**: `C:\UnityProjects\Case464-GitHub\game\UnityProject`.
- 사용자 제공 결과: Unity **6000.3.26f1**, 활성 씬은 **이름 없는 새 씬(변경 표시 없음)**, hierarchy에 `Main Camera`, `Directional Light`, Console 오류 **0개**, 프로젝트 에셋 약 **133개**.
- 저장된 씬: `SampleScene`, `DialoguePreview`; 프리팹: `DialoguePanel` 1개. **H00 제작 씬은 없음**.
- 제작 기반 존재: URP 설정, `Case464MapWorkshop.cs`, `room_profiles_24.v1.json`. 하지만 호텔 건축/가구 3D 프리팹, 프리미엄 PBR 재질의 **Unity 프로젝트 임포트/품질 승인은 미확인**.
- GitHub에 206개 CC0 모델이 있어도 대부분 저폴리 **소스 재료**이며 Unity 에셋 임포트/완성맵 준비와 동일하지 않음.
- 사용자의 Codex CLI 결과를 진행도 근거로 기록한 것이며, 이 ChatGPT 세션에서 로컬 Unity를 직접 호출/검증하지 않았음.
- 다음 작업은 **H00 품질용 소스 확보/검토 → 블렌더/Unity 건축 제작 → 별도 신규 H00 작업 씬 → 시각 검수**. 기존 `SampleScene`/`DialoguePreview`/프리팹 덮어쓰기 금지.
- **반복 기술 실패 2회면 즉시 중단**하고 마지막 성공 단계·필요한 도구/에셋과 재개 지점 보고. Unity Play Mode/기능 테스트/빌드 사용자 별도 요청 전 금지.
