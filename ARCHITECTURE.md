# Unity 구조
책임 분리: GameState, TurnManager, BoardManager, PlayerState, InvestigationManager, DeductionManager, AbilityManager, EventManager, AIController, UIManager.
진행: MatchSetup→역할/정답/손패/능력→R1(3P)→R2~5(2P)→R6 추리/추가이벤트 구간→R7 재조사→R11까지 범인 예약/추가이벤트 가능→R12 강제추리→정답자 즉시 단독승리 또는 범인승리.
룰/콘텐츠 수치는 가능한 ScriptableObject/설정 데이터로 분리.
개발 순서: 싱글 상태/카드→맵/이동→수사→추리/승패/관전→능력→이벤트→AI→12R 시뮬레이션/밸런스→UI/연출→싱글 전체검증→멀티/재접속/서버권한.