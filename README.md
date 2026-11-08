# 마또 머더미스터리 — AI 추리형 2.5D 보드게임

## 2026-11-08 최신 확정 구조

- **플레이어:** 탐정 1명.
- **NPC 용의자:** 마또 / 마리히 / 나즈 / 찌로 / 보키 / 문이레 — 6명.
- **범인:** NPC 6명 중 매 판 1명 랜덤.
- **맵:** 공동 로비 1 + NPC 개인실 6 + 피해자가 실제 살해된 사건 현장 1 = **총 8개 방**.
- **수사:** NPC 알리바이 대조 + 현장/개인실 증거 연결 + 논리적으로 변화하는 무작위 사건 + 시간표/동선 사건 재구성 + 증거 기반 추궁.
- **최종 정답:** **범인 + 범행수법 + 흉기**. **범행 장소는 정답 대상에서 제외.**
- **NPC 심문:** **질문 선택지 없이 6명 NPC와 한국어 자유 채팅**. NPC는 사건별 기억·이전 발언·증거 제시·진술 번복 이력을 유지하고 **AI가 작가 검수 대사 ID만 검색·선택**해 답한다. 즉석 대사 생성 없음.
- **랜덤 AI:** 승인된 사건 템플릿 + 시드/제약 생성 + NPC별 지식 + 증거/시간표 그래프 + 정답 유일성 검증.
- **정답 입력:** **한국어 자유 문장 직접 타이핑**. AI는 해석, 별도 정답 엔진은 판정.
- **그래픽:** 새 고품질 8방 쿼터뷰 3D 디오라마 + 고품질 2D NPC 스프라이트. 기존 그레이박스 최종 아트 불채택.

## 현행 최상위 문서
- [GAME_RULES.md](GAME_RULES.md) — 확정 룰
- [DESIGN_BASELINE.md](DESIGN_BASELINE.md) — 고정 기획
- [MAPS.md](MAPS.md) — 총 8개 방
- [CASE_DESIGN.md](CASE_DESIGN.md) — 랜덤 사건·알리바이·증거·재구성
- [NATURAL_LANGUAGE_DEDUCTION.md](NATURAL_LANGUAGE_DEDUCTION.md) — 한국어 타이핑과 AI 채점 분리
- [AI.md](AI.md) — NPC별 지식·거짓 진술·심문
- [NPC_FREE_DIALOGUE.md](NPC_FREE_DIALOGUE.md) — 자유 질문 → 사전 검수 NPC 대사 선택·기억·추궁
- [CHARACTERS_PERSONALITY.md](CHARACTERS_PERSONALITY.md) — 사용자 확정 6명 성격과 말투
- [game/dialogue/npc_dialogue_catalog.v1.json](game/dialogue/npc_dialogue_catalog.v1.json) — **6명 × 38상황 = 기초 대사 228개 저장**
- [game/dialogue/README.md](game/dialogue/README.md) — 대사 ID·상황 해금·AI 검색 연결 가이드
- [QUESTION_TUTORIAL.md](QUESTION_TUTORIAL.md) — **질문 튜토리얼**: 초보자 안내·상황별 질문 가이드
- [game/dialogue/question_tutorial.v1.json](game/dialogue/question_tutorial.v1.json) — 게임 UI용 사전 작성 안내 문구 데이터
- [RANDOM_CASE_AI_DESIGN.md](RANDOM_CASE_AI_DESIGN.md) — 제약 기반 랜덤 사건·증거 검증·난이도 설계
- [NPC_SPRITE_SPEC.md](NPC_SPRITE_SPEC.md) — NPC 6명 전용 최소 스프라이트 제작 명세
- [UI_FLOW.md](UI_FLOW.md) — 자유 타이핑 고발·추리보드 UI
- [PRODUCTION_STANDARD_2_5D.md](PRODUCTION_STANDARD_2_5D.md) — 고품질 아트/Unity 기준
- [ARCHITECTURE.md](ARCHITECTURE.md) — 권장 기술 구조
- [AGENTS.md](AGENTS.md) — Codex 작업 제약
- [LEGACY_IMPLEMENTATION.md](LEGACY_IMPLEMENTATION.md) — 이전 런타임 현황

**종전 전체 스냅샷:** [archive/pre-single-detective-reboot-20261108](https://github.com/ddoing2dang/deduction-game/tree/archive/pre-single-detective-reboot-20261108). 구형 6인 PvP/카드/12R/9방 구조는 폐기.

**구현 상태:** 6인 성격 및 기초 상황별 대사 JSON(228개)을 저장했으며, 새 규칙을 GitHub 문서에 반영했지만 기존 Unity 씬·C# 런타임 자체를 새 게임으로 전환한 것은 아니다. 사용자 별도 요청 없이 테스트·빌드·배포하지 않았다.
