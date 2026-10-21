# Deduction Game — Production

실제 PC/Android 게임 구현 영역. 웹 프로토타입과 분리한다.

## 타깃
- Windows
- Android
- Single: 1 human + 5 AI
- Multiplayer: 2–6 humans, 빈 좌석 AI 충원

## 구현 순서
1. Core rules/state machine
2. Pre-round setup: 전원 3P 초기 배치
3. R1~R12: 기본 2P, 이동 + 정보행동
4. 카드/의심/협조/비공개 정보
5. 목격/과학수사/열쇠-상자/이벤트
6. 능력/추리/승패
7. AI
8. 실제 맵/UI/아트
9. 방 코드 멀티플레이

## 핵심 상태 흐름
BOOT -> DEAL -> SETUP_MOVE_3P -> ROUND_START -> PLAYER_TURN -> ROUND_END -> GAME_END

초기 배치가 6명 모두 끝나기 전에는 R1이 시작되지 않는다.
R1부터 모든 플레이어 기본 이동력은 2P다.
