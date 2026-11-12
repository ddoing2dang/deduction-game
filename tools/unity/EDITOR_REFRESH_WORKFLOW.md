# 사건파일 No.464 — 자동 에셋 갱신

Codex는 파일 수정을 모두 끝낸 뒤 아래 클라이언트를 한 번 실행한다.
사용자에게 Assets > Refresh, 재시작, 반복 클릭을 요청하지 않는다.
PowerShell, Codex CLI, Unity 배치 실행은 사용하지 않는다.

    python tools/unity/refresh_editor_assets.py --scripts-changed

Python은 이미 설치된 런타임을 사용한다. Blender에 포함된 python.exe도
일반 Python으로 사용할 수 있으며 Blender 자체를 실행하는 것이 아니다.
C#을 변경하지 않은 PNG/.meta/씬 작업에서는 --scripts-changed를 생략한다.
다른 씬에서는 --scene Assets/...unity를 지정하고 호텔 NPC가 없는 작업에는
--no-hotel-checks를 지정한다. 이 명령은 Codex가 실행하며 사용자 수동 단계가 아니다.

## 동작

1. 모든 파일 쓰기가 끝난 후 요청 JSON을 원자적으로 게시한다.
2. 이미 로드된 Editor 도구가 Library/Case464AssetRefresh의 큐를 읽는다.
3. AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport)를 한 번 실행한다.
4. isCompiling/isUpdating과 컴파일 오류를 관찰하며, 완료 후 필요한 스크립트
   리로드를 요청한다. SessionState의 작업 단계와 InitializeOnLoadMethod로 이어간다.
5. 지정된 Sprite/SceneAsset과 호텔 NPC 6명×8방향 참조를 확인한다.
6. 발 좌표·스프라이트 로컬 위치·크기·원본 Sprite GUID가 유지된 경우에만
   현재 씬을 백업 후 저장한다. 다른 씬으로 자동 전환하지 않는다.
7. result_<요청 ID>.json의 phase=Complete, saved=true를 확인해야 실제 완료다.
   클라이언트는 터미널 결과까지 기다리고 오류는 자동 재시도하지 않는다.

SessionState가 새 Editor 프로세스에서는 초기화되므로 예전 요청을 재실행하지 않는다.
디스크의 heartbeat는 PID·프로젝트·최근 갱신 시각을 대조한다. 다른 프로젝트와
Play Mode는 거부한다. 컴파일 실패·시간 초과·미저장 변경은 보존한 채 중단한다.
미저장 사용자 변경을 자동으로 버리지 않으며, 이 도구는 UI나 가구를 수정하지 않는다.

## 최초 연결

도구가 아직 로드되지 않았다면, 이미 설치되어 실행 중인 Unity Pipeline의
Library/Pipeline/.unity-pipeline-port를 읽어 로컬 API 핸드셰이크를 확인한다.
이 프로젝트의 Editor인지 확인하고, 고정된 최초 임포트 코드만 요청한다.
최초 임포트는 BootstrapId를 SessionState에 남기며, 새 도구는 이를 이어받아
Refresh를 두 번 실행하지 않고 검증·저장을 완료한다.
토큰은 메모리에서만 사용하고 Git에 저장하거나 출력하지 않는다.

새 Editor가 실행되지 않았거나 Pipeline/큐가 모두 없거나 로컬 API 접근 권한이
없으면 자동 연결은 불가능하다. 이때 실제 원인을 기록하며 수동 Refresh를
요구하는 성공 절차로 대체하지 않는다.

## Editor 내부에서 여러 파일을 만들 때

Case464AssetRefresh.RunAssetBatch(Action)를 사용할 수 있다.
StartAssetEditing은 try/finally의 StopAssetEditing과 짝을 이루며,
컴파일이나 Domain Reload를 기다리는 동안 열린 상태로 유지하지 않는다.
외부 파일 복사는 이 API로 감쌀 필요 없이 쓰기를 끝낸 후 한 번 요청한다.

전역 SaveAssets, 프로젝트 재시작, Play Mode, 기능 테스트, 빌드는 호출하지 않는다.
Library의 요청·백업·토큰·실행 상태는 커밋하지 않는다. 결과 증빙은 토큰과
세션 ID를 제거한 보고서만 저장한다.

Unity API 근거:
- https://docs.unity.com/en-us/engine/7000.0/script-reference/unityeditor/assetdatabase/refresh
- https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/editorutility/requestscriptreload
