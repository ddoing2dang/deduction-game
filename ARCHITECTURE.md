# Unity 구조
책임 분리: GameState, TurnManager, BoardManager, PlayerState, InvestigationManager, DeductionManager, AbilityManager, EventManager, AIController, UIManager.
진행: MatchSetup→역할/정답/손패/능력→R1 이전 전원 초기 배치(3P)→R1~R5(각 턴 기본 2P, 공식 최종 추리 금지)→R6~R11(각 턴 기본 2P, 공식 최종 추리 가능, R7 과학수사 공개 안내)→R12(각 턴 기본 2P, 활성 수사자의 최종 추리 1회 강제)→라운드 종료 시 공식 정답자 수로 해당 테마 점수 정산→3개 테마 모두 진행 후 누적점수로 우승 결정.
공식 최종 추리는 수사자별 테마당 최대 2회·같은 라운드 최대 1회다. 예: R6 첫 추리 실패 시 R6 재추리는 불가하고 R7에 두 번째 추리는 가능하다(추가 쿨타임 없음). 2회 실패하면 해당 테마에서 관전 전환. 같은 라운드의 수사자가 모두 행동하기 전에는 첫 정답 제출만으로 테마를 끝내지 않는다. 범인 가짜추리 1회 의무와 범인 0/1/2점 생존 득점은 GAME_RULES.md 최신 LOCK을 따른다.
룰/콘텐츠 수치는 가능한 ScriptableObject/설정 데이터로 분리.
개발 순서: 싱글 상태/카드→맵/이동→수사→추리/승패/관전→능력→이벤트→AI→12R 시뮬레이션/밸런스→UI/연출→싱글 전체검증→멀티/재접속/서버권한.