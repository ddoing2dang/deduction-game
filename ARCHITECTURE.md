# 마또 사건 해결형 머더미스터리 — 신규 소프트웨어 아키텍처 초안

> 상태: 제안 설계. 과거 6인 PvP/카드/12R 구현은 무효이며 새 게임에 맞게 재설계한다.

## 권장 책임 분리
- `CaseSession`: 현재 사건 단계, 플레이어 탐정, 발견한 증거, 추리 상태.
- `CaseTruthGenerator`: 시드로 진실(범인, 장소, 흉기, 사건 시간, 동선)을 구성.
- `CaseValidator`: 사건의 물리적 개연성, 단서 간 일치, 추론 가능성 검증.
- `NpcKnowledgeService`: NPC별 실제 지식·추억·알리바이와 진술, 범인의 의도적 거짓말.
- `EnvironmentInvestigationService`: 장소/오브젝트 조사, 증거 획득, 재방문.
- `DialogueAndConfrontationService`: 심문/질문/증거 제시/모순 지적.
- `EvidenceJournal`: 획득 증거·목격·의심·사건 시간표.
- `DeductionBoard`: 증거·인물·장소·흉기를 플레이어가 수동 연결.
- `AccusationResolver`: 최종 범인/범행 장소/흉기 판정과 엔딩.
- `BoardWorldPresenter`: 3D 쿼터뷰 지도와 고품질 2D NPC 비주얼.
- `SaveLoad`: 사건 시드와 조사 진행 상태 영속화.

## 기본 흐름
사건 생성/검증 → 사건 도입 → 자유 탐색·NPC 대화 → 증거 수집/심문·모순 지적 → 추리 보드 → 최종 고발 → 사건 결말.

구체적 행동 제한, NPC 스케줄, 재도전 규칙과 맵 방 수는 미확정이며 임의로 이전 12라운드 체계를 가져오지 않는다.

## 구현 출발점
기존 Unity `game/UnityProject`는 자산/프로젝트 기반으로 유지하되 과거 Card/TurnManager/AIController 중심 코드는 새 게임에 재사용 가능성을 평가하기 전 **Legacy**로 취급한다. 현 시점 구현/컴파일/Play Mode 검증은 수행하지 않았다.
