# 사건파일 No.464 — 자동 갱신 실제 적용 기록

2026-11-12 Unity 6000.3.26f1의 실행 중인 동일 프로젝트 Editor에서 자동화 연결·임포트·컴파일/Domain Reload 대기·참조 확인·씬 저장을 수행했습니다. 사용자 Refresh 클릭과 GUI 제어, Unity 재시작은 사용하지 않았습니다.

- 최초 설치: ForceSynchronousImport 1회, Domain Reload 1회, 원본 Sprite 48장 참조 및 NPC 발 기준점 보존 확인, 최신 씬 저장.
- 상주 큐로 최종 C# 변경 반영: ForceSynchronousImport 1회, Domain Reload 1회 후 SessionState 단계 복구, Complete/saved=true.
- 기본 Node 도구의 루프백 통신은 OS EACCES로 차단되어 같은 경로는 중단했습니다. 권한이 있는 로컬 실행 도구로 설치된 Pipeline API에 연결했습니다.
- 백그라운드 Editor에서 delayCall 예약이 실행되지 않는 원인을 확인했고, 최초 연결은 API 내 직접 Refresh와 compilationFinished의 명시적 리로드로 수정했습니다. 반복 수동 클릭을 요구하지 않습니다.
- 직렬화 오브젝트 23217개 유지. 기존 NPC Transform, 맵·가구·카메라·Light 블록 변경 없음. Unity 저장 과정에서 숨겨진 기존 DialoguePanel 프리팹의 레이아웃 값 7개만 재계산됐습니다.
- Bootstrap과 상주 큐 결과 JSON은 세션 키와 프로세스 ID를 제거했습니다. 로컬 API 토큰·Library 작업 큐·임시 백업은 GitHub에 올리지 않습니다.
- Play Mode·기능 테스트·빌드는 실행하지 않았습니다. 실제 화면 재검수는 이번 자동 갱신 작업 범위에서 수행하지 않았습니다.

재사용: tools/unity/EDITOR_REFRESH_WORKFLOW.md
실제 저장 씬: Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Final_Map_RoomSigns_WideAisle.unity
