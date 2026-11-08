# 마또 머더미스터리 — Unity 프로젝트

Unity Hub에서 실제 `game/UnityProject` 폴더를 사용한다. **현재 Unity 게임플레이는 구형 6인 PvP 카드 기반 LEGACY 구현**이다. 새 게임은 기획 문서에 확정되었으나 아직 Unity 코드/씬/프리팹으로 전환되지 않았다.

## 신규 대상
- 플레이어: 탐정 1명.
- NPC: 마또/마리히/나즈/찌로/보키/문이레, 범인 1명 랜덤.
- 맵: 공동 로비 1 + NPC 개인실 6 + 피해자 살해 현장 1 = 8개 방.
- 수사: 알리바이 대조/증거 연결/랜덤 사건/시간표 재구성/증거 추궁.
- **NPC 심문**: 6명 전원 질문 선택지 없이 한국어 자유 타이핑/실시간 AI 대화. NPC별 지식·대화 기억·진술 원문·모순 증거로 **사전 검수 대사 ID 선택**. AI가 대사를 새로 생성하지 않음.
- **랜덤 사건**: 검증된 템플릿 + 제약/시드 생성, 진실 동결, 증거 도달성/유일 정답 검증.
- 최종 고발: **범인 + 범행수법 + 흉기**를 한국어 문장으로 자유 타이핑. AI 파서 + 별도 사건 진실 판정.
- 아트: 고품질 쿼터뷰 2.5D 맵/8방향 NPC 스프라이트.

## 확인할 문서
- [GAME_RULES.md](../../GAME_RULES.md)
- [MAPS.md](../../MAPS.md)
- [CASE_DESIGN.md](../../CASE_DESIGN.md)
- [NATURAL_LANGUAGE_DEDUCTION.md](../../NATURAL_LANGUAGE_DEDUCTION.md)
- [NPC_FREE_DIALOGUE.md](../../NPC_FREE_DIALOGUE.md)
- [NPC_SPRITE_SPEC.md](../../NPC_SPRITE_SPEC.md)
- [RANDOM_CASE_AI_DESIGN.md](../../RANDOM_CASE_AI_DESIGN.md)
- [PRODUCTION_STANDARD_2_5D.md](../../PRODUCTION_STANDARD_2_5D.md)
- [AGENTS.md](../../AGENTS.md)

원본 3D 맵·Unity 씬·에셋을 사용자 승인 없이 삭제/덮어쓰기하지 않는다. 사용자가 요청하지 않은 기능 테스트, Play Mode, EXE/APK 재빌드·배포 금지. 실제 새 구현과 검증을 마친 것으로 보고하지 않는다.
