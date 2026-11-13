# 장미대저택 핵심 시스템 — 중단 상태

객실 선택 UI 작성 모드 미리보기 오류가 2회 발생하여 사용자 규칙에 따라 중단했습니다.

- 컴파일/Domain Reload: Complete
- 신규 씬: `Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Gameplay_Work.unity` (저장됨)
- 오류: `Sequence contains no matching element` — `RoseProgress.Room → RoseMansionSession.RoomStatus → RoseMansionView.LobbyScreen`
- 원인: 작성 모드 UI가 초기화되지 않은 객실 진행 상태를 참조합니다. Domain Reload의 기본 객체 복원 여부는 가설이며 추가 확인이 필요합니다.
- 기존 NPC 없는 원본 씬과 소스 백업은 보존했습니다.
- 미확정 콘텐츠는 42개 조사 슬롯·6개 사건 정답 분기 등이며 임의 증거/정답을 넣지 않았습니다.
- Play Mode/기능 테스트/빌드 미실시. 실제 게임 진행·저장 복원은 미검증입니다.
- Git 커밋/푸시 미실시. 오류가 남은 상태를 완성본으로 게시하지 않았습니다.

재개 시 UI preview의 State 접근을 실제 진행과 분리하고, 초기화 여부 및 Domain Reload 뒤 RNG 복원을 보완한 뒤 `WORK_STATUS.json`의 자동 Refresh 명령으로 이어갑니다. 기존 씬·부분 생성 파일은 자동 삭제하지 않습니다.
