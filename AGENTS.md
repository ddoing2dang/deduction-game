# 개발 불변 원칙
- Unity PC + Mobile. 항상 6인: 싱글 1인+AI5, 멀티 2~6인+빈자리 AI.
- 싱글플레이를 완성/검증한 뒤 방 코드 멀티플레이를 구현한다.
- 최신 확정 규칙만 구현하고 폐기 규칙을 되살리지 않는다. 변경 전 문서 전체의 충돌을 검사한다.
- 시스템의 확정 정보는 거짓일 수 없다. 행동은 모두가 보고 진짜 비밀정보만 개인 화면에 표시한다.
- 캐릭터 카드는 손패로 배분하지 않는다. 주사위 없음. 최대 12R.
- 서버 권한형 멀티 구조를 사용하고 비밀정보를 클라이언트에 미리 보내지 않는다.
- GameState/TurnManager/BoardManager/PlayerState/InvestigationManager/DeductionManager/AbilityManager/EventManager/AIController/UIManager로 책임을 분리한다.

# Codex 작업 기준 (2026-10-23)
- 실제 제작 저장소는 `ddoing2dang/deduction-game`, 기본 브랜치는 `main`.
- Unity 프로젝트 루트는 `game/UnityProject`.
- 현재 Unity 3D 제작은 기존 규칙 프로토타입을 유지하면서 최종 3D 구조로 확장한다. 이전 단계로 되돌리거나 새 프로젝트를 만들지 않는다.
- 맵/캐릭터/카메라/아트 규격은 `ART_GUIDE.md`, 게임 연결망은 `MAPS.md`, 게임 규칙은 `GAME_RULES.md`를 우선한다. LOCK 항목을 임의 변경하지 않는다.
- 현재 대저택 레이아웃은 production-scale 좌표와 6인 StandPoint를 사용한다.
- `ProjectBootstrap`의 SceneLayoutVersion으로 기존 Game.unity를 자동 업그레이드한다. 씬 구조 변경 시 버전을 올려 기존 로컬 씬에도 적용되게 한다.
- 현재까지 11개 확정 연결부와 DoorID 계층, 지상/지하 서비스 연결, 비밀통로 오브젝트 기반을 추가했다.
- 다음 Unity 3D 제작 순서: 대저택 실제 벽/문/복도/계단 구조 → RoomNode/Entry/StandPoint/ActionPoint 실체화 → NavMesh 이동 → 문/경로 이벤트 상태 연동 → 카메라 벽 컷어웨이 → 플레이 검증.
- Blender 최종 에셋 제작으로 넘어가기 전에 위 Unity 3D 시스템과 대저택 플레이 동선을 검증한다.
- 기존 게임 규칙/AI/추리 로직을 맵 시각화 작업 때문에 임의 삭제하거나 단순화하지 않는다.
- 사용자가 요청하지 않은 대규모 재작성, 새 프로젝트 생성, 브랜치 갈아엎기, 규칙 재설계는 하지 않는다.
- 변경 후 컴파일 오류 가능성을 확인하고, 서로 연관된 파일은 함께 검토한다.
