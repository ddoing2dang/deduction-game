# 마또 머더미스터리 — Unity 제작 영역

최신 확정 룰: 플레이어 탐정 1명 / 6명의 고정 NPC 용의자 중 랜덤 범인 1명 / **8개 방**(공동 로비 1, 각 NPC 개인실 6, 피해자 살해 현장 1) / **범인·범행수법·흉기 3항목을 한국어 자유 입력으로 최종 추리**.

- **NPC 질문은 완전 자유 한국어 타이핑**으로 확정. 질문 선택지 없음. NPC 지식 게이트/이전 진술 원문 보존/증거 기반 추궁/LLM 발언 검증.
- 사건 생성은 **검증된 사건 템플릿 + 제약 랜덤 + 진실 동결 + 시드 재현 + 유일해/도달성 검증** 방식.
- 핵심 기능: 알리바이 대조 / 현장·개인실 물증 연결 / 인과적으로 달라지는 랜덤 사건 / 사건 시간표 재구성 / 증거 기반 심문.
- 그래픽: 3D 고품질 쿼터뷰 맵 + 2D 방향별 NPC + 수사 UI/컷신.
- 현재 Unity는 **구형 6인 PvP 카드·턴 기반 LEGACY** 상태. 문서 규칙 확정은 Unity 코드 적용이 아님.

자세한 규칙은 [GAME_RULES.md](../GAME_RULES.md), 맵은 [MAPS.md](../MAPS.md), AI 채점 설계는 [NATURAL_LANGUAGE_DEDUCTION.md](../NATURAL_LANGUAGE_DEDUCTION.md), NPC 자유 채팅은 [NPC_FREE_DIALOGUE.md](../NPC_FREE_DIALOGUE.md), 랜덤 사건은 [RANDOM_CASE_AI_DESIGN.md](../RANDOM_CASE_AI_DESIGN.md), 아트는 [PRODUCTION_STANDARD_2_5D.md](../PRODUCTION_STANDARD_2_5D.md) 참조.

실제 프로젝트는 `UnityProject/`. 원본 씬·에셋 보존, 승인 없는 기능 테스트·빌드·배포 금지.
