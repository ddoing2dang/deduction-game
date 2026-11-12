# 추리 게임 — 새 Unity 프로젝트 시작 (2026-11-09)

**최신 플랫폼 확정(2026-11-09): Windows PC 64비트 전용. Android/iOS/macOS 및 모바일 개발·최적화·빌드·배포 제외. 기존 Unity 파일/에셋은 보존하고 사용자 별도 요청 없이 실행 테스트·Play Mode·EXE 빌드를 하지 않는다.**

[Windows PC 맵 제작 매뉴얼](../PC_MAP_BUILD_MANUAL.md) · [환경 에셋 19종](../art/Environment/)


**이전 `UnityProject/` 프로젝트와 구형 6인 PvP/카드/턴/씬 자동 생성 코드는 `main`에서 초기화되었다.** 삭제 전 전체 상태는 [백업 브랜치](https://github.com/ddoing2dang/deduction-game/tree/archive/pre-fresh-unity-project-20261109)에 있다.

새 프로젝트는 **Unity Hub → New Project → Universal 3D (URP)**, 프로젝트 이름 `UnityProject`, 저장 위치 `<repo>/game`로 생성한다. 버전은 기존 사용하던 Unity **6000.3.26f1** 권장. [자세한 새 프로젝트 생성/이관 절차](UNITY_RESTART_GUIDE.md) 참조.

## 유지되는 현행 게임 기획
- 인간 탐정 1명 + 용의자 NPC 6명(마도은·마연희·나지수·김보희·진소율·문이현); 범인 1명 무작위 선정.
- `HOTEL`, `CRUISE`, `MUSIC_BROADCAST` 세 테마, 각각 8개 조사 공간.
- 최종 정답은 **범인 + 범행수법(4개 대분류 중 1개) + 흉기**, 범행장소는 정답에서 제외.
- 자유 한국어 타이핑 심문. NPC 답변은 사건/지식/증거 상태를 검증하고 **사전 작성·검수된 대사 ID만** 선택.
- **의도적 거짓말은 현재 사건 범인만**, 무고한 NPC는 자신이 실제로 아는 사실만 진술.
- 기본 사전 대사·사건/흉기·맵 설계는 `game/dialogue/`, `game/cases/`, `game/maps/`에 보존됨. 기존 검증 전 시범 사건은 출시 가능한 데이터가 아님.
- 실제 사용자 업로드 원본 대화창 3종은 `art/UI/DialogueFrames/ThemeDialogue_v1/`에 GitHub 저장됨; 프레임 원본과 투명도를 그대로 사용.

## 구현 상태
**기존 Unity 코드 제거 / 새 Unity Hub 프로젝트 미생성 / 신규 씬·프리팹 미구현 / 테스트·빌드 미실시.**

먼저 대저택 대화창 프리팹을 새 Unity 프로젝트의 첫 독립 씬으로 만들고, 이후 다른 두 테마에 재사용한다. 자동 복원되는 구형 `ProjectBootstrap.cs`는 어떤 경우에도 새 프로젝트에 넣지 않는다.
