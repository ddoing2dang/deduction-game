# Deduction Game — Unity 게임

- 대상: Windows PC / Android
- 동시에 6명: 싱글 1인 + AI 5인, 멀티 2~6인 + 빈자리 AI (멀티 구현은 싱글 안정화 및 사용자 요청 이후)
- 최종 시각 제작 기준: [PRODUCTION_STANDARD_2_5D.md](../PRODUCTION_STANDARD_2_5D.md)
- 게임 규칙: [GAME_RULES.md](../GAME_RULES.md) 및 [DESIGN_BASELINE.md](../DESIGN_BASELINE.md)
- 실제 Unity 프로젝트: `UnityProject/`

## 최신 시각 제작 방향
- 기존 3D 쿼터뷰 디오라마와 9방/RoomNode 이동 그래프, 게임 판정은 유지한다.
- 캐릭터 외형은 완전 3D 리깅 모델 대신 고품질 방향별 2D 스프라이트를 사용한다.
- PlayerRoot의 이동/충돌/AI/규칙을 보존하고, VisualRoot의 2.5D 표현을 독립 구현한다.
- 마또 1명 정적 시제품부터 시작해 방향별 애니메이션, 가림, 그림자를 검증한 후 다른 5명에게 확장한다.
- 기존 모델/씬/프리팹 삭제, 임의 재빌드 및 배포를 하지 않는다.

## 핵심 게임 상태 흐름 (규칙 보존)
BOOT → DEAL → SETUP_MOVE_3P → ROUND_START → PLAYER_TURN → ROUND_END → GAME_END

초기 배치가 6명 모두 끝나기 전에는 R1이 시작되지 않는다. R1부터 모든 플레이어 기본 이동력은 2P다. 자세한 판정과 비밀정보는 게임 규칙 문서를 따른다.
