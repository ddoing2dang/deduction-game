# Deduction Game Unity Project

실제 Unity 제작 프로젝트. Unity Hub에서는 이 `game/UnityProject` 폴더를 연다.

**최종 시각 제작 기준:** [PRODUCTION_STANDARD_2_5D.md](../../PRODUCTION_STANDARD_2_5D.md)

## 제작/작업 주의
- 기존 Unity 버전과 렌더 파이프라인은 **현재 프로젝트에서 직접 확인**한다. 이전 텍스트만 보고 버전을 추정하지 않는다.
- 기존 3D 쿼터뷰 맵, RoomNode, 플레이어 게임 로직, 네트워크 구조, Collider, 아트 원본, 프리팹을 보존한다.
- 게임 내 캐릭터 외형만 방향별 고품질 2D 스프라이트로 바꾸는 **2.5D 하이브리드** 구조를 우선한다.
- 마또 1명부터 독립 시제품으로 연결하고, 없는 방향/애니메이션 에셋은 만들어졌다고 보고하지 않는다.
- 기존 씬을 덮어쓰거나 자동 재생성하는 작업은 먼저 변경 범위를 검토한다.
- 게임 규칙/AI/이동/상호작용 변경 금지. 사용자가 별도 요청하지 않은 기능 테스트·빌드·배포 금지.

## 게임 규칙 출처
[GAME_RULES.md](../../GAME_RULES.md), [DESIGN_BASELINE.md](../../DESIGN_BASELINE.md), [AI.md](../../AI.md), [ABILITIES.md](../../ABILITIES.md), [MAPS.md](../../MAPS.md)

이 README는 새 시각화 기준을 가리키는 문서일 뿐 Unity 2.5D 구현·실기기 검증 완료 기록이 아니다.
