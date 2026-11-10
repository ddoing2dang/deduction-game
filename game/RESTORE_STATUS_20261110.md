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
