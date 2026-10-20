# 개발 불변 원칙
- Unity PC + Mobile. 항상 6인: 싱글 1인+AI5, 멀티 2~6인+빈자리 AI.
- 싱글플레이를 완성/검증한 뒤 방 코드 멀티플레이를 구현한다.
- 최신 확정 규칙만 구현하고 폐기 규칙을 되살리지 않는다. 변경 전 문서 전체의 충돌을 검사한다.
- 시스템의 확정 정보는 거짓일 수 없다. 행동은 모두가 보고 진짜 비밀정보만 개인 화면에 표시한다.
- 캐릭터 카드는 손패로 배분하지 않는다. 주사위 없음. 최대 12R.
- 서버 권한형 멀티 구조를 사용하고 비밀정보를 클라이언트에 미리 보내지 않는다.
- GameState/TurnManager/BoardManager/PlayerState/InvestigationManager/DeductionManager/AbilityManager/EventManager/AIController/UIManager로 책임을 분리한다.