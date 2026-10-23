# Windows x64 검증 기록 — 2026-10-23

최종 판정: **미완료**. 후속 자동화 종료 비교 24회는 정상이나 기존 네이티브 충돌 원인은 미확정이다. 사용자 요청에 따라 자동화 없는 사용자 종료 대조군은 이번에 진행하지 않았으며, 전체 종료 안정성을 통과한 것으로 처리하지 않는다. 아래 후속 조사 기록을 참고한다.

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

- 최초 기록은 21:19:56 Mansion, 21:27:35 Museum Player.log였으나, 후속 Windows 이벤트 대조 결과 두 시점의 오류 실행 경로 모두 Mansion이었다. 공유 Player.log 혼선 때문에 두 번째 크래시의 맵을 Museum으로 확정할 수 없다.
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

## 후속 종료 원인 분리 — 2026-10-23 (KST)

### 확인된 사실과 아직 모르는 것

- 기존 Application Error 1000 / WER 1001 기록을 읽어 별도 보존했다. 두 시점 모두 `UnityPlayer.dll` 오프셋 `0x808029`, 예외 `0xc0000005` 및 `0xc000041d`가 기록됐다. 각각 PID `0x36BC`, `0x11A0`의 같은 프로세스에 대해 2개의 예외 이벤트가 있으므로 4회의 독립 게임 충돌로 세지 않는다.
- 두 crash.log 모두 Physics cleanup과 Input System Shutdown 뒤 `UnityPlayer → Windows 메시지/COM → UIAutomationCore.UiaDisconnectAllProviders` 스택이다. 관리 코드의 예외 스택은 없다. UI Automation 종료 상호작용 가능성을 가리키지만 이를 단독 원인으로 확정하지는 못했다.
- 게임 스크립트에는 사용자 정의 `OnApplicationQuit`, `Application.quitting`/`wantsToQuit`, 직접 종료 API, 네이티브 P/Invoke, Task/Thread/async 처리가 없다. 내부 정상 종료 버튼도 없어 새로 추가하지 않았다.
- RuntimeGameUI.OnDestroy는 공개 메시지 구독 해제, RoomNodeView/TokenLabel은 자신의 레이블 정리, MansionEventView는 참조가 남은 표시/재질만 정리한다. 기존 GameOver/Restart/씬 재시작 중복 검사도 유지됐다. 정리 중 관리 코드 오류나 중복 해제가 네이티브 충돌 원인이라는 증거는 발견하지 못했다. 이것이 네이티브 엔진 종료 순서 문제를 배제하는 것은 아니다.
- 테스트 주입 없이 원본 작업 파일의 복사본에서 Release 3개를 새로 빌드했다. 세 맵 Assembly-CSharp.dll에서 PlayerLoopVerification, PlayerUIVerificationBoot, PlayerDisplayVerification, SinglePlayerE2E, UsabilityVerification 타입 문자열이 없음을 확인했다. 불필요한 Release 테스트 코드 때문에 발생했다는 근거가 없어 빌드 분리/게임 코드를 추가 변경하지 않았다.

### 실제 반복 결과

모든 실행은 순차 실행, 1280×720 창 모드, 실행별 고유 Player.log 및 process handle을 확보해 검사했다. 종료 직후 상태 재조회도 수행했다.

| 구분 | X | Alt+F4 | 결과 |
|---|---:|---:|---|
| Mansion / 스크린샷 기반 자동화 | 5 | 5 | 10/10 정상 종료 |
| Mansion / 접근성 텍스트 조회 요청 자동화 | 5 | 5 | 10/10 정상 종료 |
| Cruise / 접근성 텍스트 조회 요청 자동화 | 1 | 1 | 2/2 정상 종료 |
| Museum / 접근성 텍스트 조회 요청 자동화 | 1 | 1 | 2/2 정상 종료 |
| 자동화 없는 실제 사용자 종료 | 0 | 0 | 사용자 선택으로 미실시 |

- 총 24/24: 종료 코드 0, Input System Shutdown 로그 있음, Crash/Unhandled Exception/관리 코드 예외 로그 없음, 새 crash 디렉터리 없음, 해당 실행 중 Windows 오류 이벤트 없음. 마지막 전체 재조회에서도 검사 시작 이후 Windows 충돌 이벤트 0건을 확인했다.
- Mansion의 두 대조군은 같은 빌드 디렉터리/설정으로 비교했다. 전체 Release 파일 해시 목록을 보존했다. Unity 실행기 exe 해시는 맵 간 같을 수 있으며 이것만으로 맵 데이터가 같다는 뜻은 아니다.
- 접근성 조회 14회 중 Mansion `uia-x-02~05`, `uia-altf4-06/08/09/10`, Cruise/Museum Alt+F4에서 실제 창 트리를 확인했다. 다른 4회는 조회 요청의 결과가 null이었으므로 모든 실행에서 동일한 접근성 공급자 연결 상태가 유지됐다고 주장하지 않는다. 스크린샷 대조군도 자동화 입력을 사용하므로 UIAutomationCore가 전혀 개입하지 않았다는 대조군이 아니다.
- 도구 스킬 패키지는 최초 충돌 때 `computer-use/26.1016.51333`, 이번에는 `26.1016.62342`였다. 이 차이가 충돌 해소 원인인지 검증하지 못했다. 구버전 재현이나 회귀 수정으로 간주하지 않는다.
- 첫 collector 호출에서 상위 PowerShell의 이전 LASTEXITCODE가 남아 일부 콘솔에 FAILED가 출력됐지만 result.json의 실제 Player ExitCode는 모두 0이었다. 수집기가 성공 시 명시적으로 exit 0을 반환하도록 수정했고 이후 호출에서 확인했다. 게임 테스트 기준은 변경하지 않았다.

### 회귀와 산출물

- 새 Release 3개 빌드 성공: `C:/UnityProjects/DeductionGame/release-exit-build.log`.
- Editor 전체 6개 검증 suite 재실행 성공: `C:/Users/dm856/AppData/Local/Temp/DeductionGameE2E-1d1627f1890a4154986b0751af96fcc6`. 기존 72판 / 7,450 검증 및 세 맵 GameOver/Restart/UI 회귀 통과.
- 고유 실행 로그, 종료 코드, Windows 이벤트, 빌드 해시: `C:/UnityProjects/DeductionGame/Builds/Windows/ExitVerification`. 각 trial/result.json 및 Player.log, all-results.json, release-manifest.json, prior-windows-events.json.
- 추가한 파일은 `Tests/Windows/WatchReleaseExit.ps1`뿐이며 게임 C#/규칙/LOCK/맵/캐릭터/멀티플레이 코드는 변경하지 않았다. 수집기는 프로세스 실행/로그 수집만 하고 UIAutomation 호출이나 종료 입력을 보내지 않는다. Mode는 실험 조건의 선언이며 무자동화 여부를 자동 증명하는 센서는 아니다.

### 남은 최소 조치와 판정

사용자가 선택한 이번 범위는 자동화 대조군뿐이다. **일반 사용자 종료 최소 10회는 미실시**이므로 일반 종료 안정성과 자동화 의존성을 최종 분리할 수 없다. 다음에는 `WatchReleaseExit.ps1 -Mode HumanOnly`로 고유 로그를 수집하면서 UI 자동화 도구 호출 없이 사람이 X 5회/Alt+F4 5회를 직접 수행해야 한다. 일반 종료에서 1회라도 실패하면 native dump/Windows 이벤트를 기준으로 조사한다. 필요하면 동일 Unity 버전의 최소 재현 비교 및 심볼 분석을 진행한다.

현재 판정은 **검증: 미통과**. 이번에 충돌이 재현되지 않았다는 이유로 기존 충돌 해결 또는 자동화 도구만의 문제라고 확정하지 않는다.
