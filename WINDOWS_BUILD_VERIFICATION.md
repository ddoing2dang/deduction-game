# Windows x64 검증 기록 — 2026-10-23

최종 판정: **미완료**. 게임/화면 자동 검증은 통과했지만, Windows 창 종료 중 네이티브 충돌을 2회 관찰했다. 종료 안정성 조건을 통과한 것으로 처리하지 않는다.

## 환경과 산출물

- Unity 6000.3.26f1, Windows x86_64, 기존 Mono/PC 설정 유지.
- `C:/UnityProjects/DeductionGame/Builds/Windows/Development/{Mansion,Cruise,Museum}/DeductionGame.exe`
- `C:/UnityProjects/DeductionGame/Builds/Windows/Release/{Mansion,Cruise,Museum}/DeductionGame.exe`
- Development/Release 총 6개 빌드 성공. PE machine 0x8664 확인.
- 기존 개발용 MapTheme 선택을 사용한 맵별 실행 파일이다. 런타임 맵 선택 기능은 추가하지 않았다.

## 최소 수정

- 씬 재로딩 때 DontDestroyOnLoad GameManager만 남고 UI/카메라/캐릭터 표현이 복구되지 않는 문제: 기존 Attach/Ensure 경로를 재사용해 복구하고 새 게임을 시작한다.
- 실제 Player에서 IMGUI 방/캐릭터 이름이 빈 상자/작은 글자로 나타나는 문제: 기존 Canvas와 런타임 폰트를 사용하는 ScreenLabel로 해당 이름 표시만 교체했다. 레이블은 raycast를 받지 않는다.
- 창 크기 변경이 막혀 있던 PC 설정: resizableWindow 활성화.
- 기본 ScrollRect 휠 감도가 너무 낮아 행동 목록 접근이 어려운 문제: 행동/정보 목록 감도 40 적용.
- 게임 규칙, AI, 카드, 이동비용, LOCK 그래프/레이아웃/문서, 멀티플레이 코드는 변경하지 않았다.
- 기존 로컬 AGENTS.md, ProjectVersion 및 Unity 생성 파일은 보존하며 이번 변경과 별도로 둔다.

## 통과한 검증

| 항목 | Mansion | Cruise | Museum |
|---|---|---|---|
| 기존 Editor E2E | 24판 / 2,461 검증 | 24판 / 2,500 검증 | 24판 / 2,489 검증 |
| 실제 Development Player E2E | 동일 24판 통과 | 동일 24판 통과 | 동일 24판 통과 |
| Player CharacterActor/Presentation/Usability | 통과 | 통과 | 통과 |
| Canvas 버튼만으로 초기 배치→GameOver | 통과 | 통과 | 통과 |
| 10회 새 게임 / 안정된 GameObject 수 | 375 | 373 | 381 |
| 씬 재로딩 후 GM 1 / UI 1 / 캐릭터 6 복구 | 통과 | 통과 | 통과 |
| 1920×1080 / 1280×720 / 1024×768 / 800×600 핵심 UI bounds | 통과 | 통과 | 통과 |
| Borderless Fullscreen / 휠 스크롤 거리 | 통과 | 통과 | 통과 |
| Release 시작 화면 | 확인 | 확인 | 확인 |

기존 Editor E2E 합계 72판 / 7,450 검증. 실제 Development Player에서도 별도로 같은 전체 E2E 통과. 기존 테스트 조건/규칙은 낮추지 않았다. 실제 Player UI 검증은 기존 상태/행동/추리/GameOver/Restart 테스트와 Canvas 전체 게임 테스트를 재사용했다.

Windows 입력 도구로 방 클릭 이동, UI 클릭, 초기 배치→R1, 1·2·3/휠 줌, F10, 창 모드/최대화, 포커스 복귀를 확인했다. 최종 Release Mansion에서도 휠로 목록 이동과 초기 배치 완료→R1을 확인했다. 전체 게임 자동 진행은 Development Player에서 실행했다.

자동 Player 9개 실행 로그에는 C# 예외/치명적 오류가 없었다. 워밍업 뒤 120프레임 샘플에 1초 이상 정지 없음. 이는 짧은 기능 검증이며 다른 PC의 성능 보장이나 장시간 부하 시험은 아니다.

## 남은 실패: 네이티브 종료 충돌

- 21:19:56 Mansion Release, 21:27:35 Museum Release 종료 중 Crash!!! 기록.
- 스택: UnityPlayer → Windows 메시지/COM 경로 → UIAutomationCore.UiaDisconnectAllProviders → UnityPlayer.
- 관리 코드 예외 스택이 아니다. Windows 자동화 도구의 종료 직후 창 재조회와의 연관성이 의심되나 원인은 확정하지 못했다.
- 이후 Museum 단독 실행에서 닫기 직후 창 재조회를 하지 않은 경우 정상 shutdown 로그, 추가 crash 기록 없음. 이 1회 성공으로 간헐 실패를 해결했다고 판단하지 않는다.
- 임의의 엔진 교체, 접근성 비활성화, 강제 프로세스 종료를 게임 수정으로 넣지 않았다. 종료 안정성 및 치명적 로그 없음 조건은 미통과다.
- 다음 작업은 단독/복수 실행 및 종료 시 창 조회 유무를 분리한 재현, 필요 시 동일 Unity 버전의 최소 프로젝트 비교와 네이티브 심볼 분석이다. 실제 사용자의 일반 종료도 추가 확인 대상이다.

## 증거

- 최종 Editor 전체 회귀 로그: `C:/Users/dm856/AppData/Local/Temp/DeductionGameE2E-b055cf06c1cd4f5085164878fc8c54da`
- 최종 Development 빌드/Player 9개 로그: `C:/Users/dm856/AppData/Local/Temp/DeductionGameWindows-82742141d98b4edcb2f99f94c34d36e3`
- 최종 Release 빌드 로그: `C:/UnityProjects/DeductionGame/windows-release-final.log`
- 크래시 원본: `C:/Users/dm856/AppData/Local/Temp/ddoing2dang/Deduction Game/Crashes/Crash_2026-10-23_121956870` 및 `Crash_2026-10-23_122735907` (Player.log/crash.dmp).
- 여러 Player의 기본 Player.log 경로가 같으므로 이후 재현은 실행별 고유 로그 경로를 사용해야 한다.

## 재실행

`game/UnityProject/Tests/Windows/RunWindowsVerification.ps1`에 UnityPath/PythonPath를 전달한다. 원본 프로젝트를 보존한 임시 복사본에서만 테스트 코드를 주입하고 3개 Development 빌드→Player loop/ui/display→3개 Release 빌드를 수행한다. 실행 중인 산출물이 있으면 빌드 전에 중단한다.

Player 테스트는 `DEVELOPMENT_BUILD && !UNITY_EDITOR` 전용이며 Release에 포함되지 않는다. 자동 검증의 종료 코드/성공 마커와 실제 수동 종료 안정성은 별도로 확인해야 한다. 다른 DPI/다중 모니터/저사양 PC/장시간 플레이는 이후 사용자 환경 확인 항목이다.
