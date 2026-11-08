# 마또 머더미스터리 — 신규 기술 구조 (2026-11-08)

> 구형 6인 PvP/카드/12라운드 구현은 LEGACY. 목표: **탐정 1명 + NPC 용의자 6명 + 고정된 8개 방 + 자유 타이핑 최종 고발**.

## 주요 시스템 책임
- `CaseSession`: 현재 사건 및 플레이어 탐정의 조사/추리 진행 상태.
- `ConstrainedCaseGenerator`: 검수된 사건 템플릿+시드+제약 기반으로 동선·수법·흉기 호환 사건 조합을 생성.
- `CaseTruthGenerator`: 시드별 범인 1명, **methodId**, weaponId, 시간·동선·물증·NPC 진술 구성. `crimeSceneRoomId = CRIME_SCENE` 고정.
- `CaseValidator`: 시간/동선·사건 인과관계/물증·증언 출처·증거 도달성·`범인+수법+흉기` 정답 유일성/시드 재현성 검증. 세 정답(범인/수법/흉기)의 인과적 증명을 검사.
- `BoardMapService`: **LOBBY + NPC 개인실 6 + CRIME_SCENE = 총 8개 방**. 연결·문·상호작용 데이터는 후속 확정.
- `NpcKnowledgeService`: 각 NPC의 사건 당시 제한된 지식, 목격·알리바이 및 의도적 거짓 진술.
- `NpcQuestionInterpreter`: 한국어 자유 질문의 의도·대명사·과거 발언/증거 참조 인식. 질문 버튼/정해진 질문 선택지 없음.
- `EvidenceReferenceResolver`: 질문 속 물건·진술·기록을 실제 보유·발견한 증거/진술 ID에 연결. 미발견 증거 자동 획득 금지.
- `NpcKnowledgeGate`: 현재 NPC가 공개 가능한 사실/진술/회피/정정 범위만 권한 검사하여 모델에 제공.
- `ConfrontationRules`: `statementId + evidenceId + 조건`으로 추궁 효력을 판정하고 해금할 새 진술을 명시.
- `NpcDialogueLLM`: 허용된 사실과 인물 성격·감정 상태로 자유 질문에 자연어 답변 작성. 게임의 확정 사실 편집 권한 없음.
- `DialogueOutputValidator`: 출력 스키마·인용 지식 ID·새 진술 ID·사실 근거 검증, 오류 시 재시도/안전 회피 대사.
- `NpcConversationMemory`: 각 NPC의 질문/원본 발언/제시받은 증거/철회·정정/심리 상태를 기록, 저장/로드.
- `EnvironmentInvestigationService`: 8개 공간의 상호작용 물증·오브젝트 조사·획득 이력.
- `DialogueAndConfrontationService`: NPC 면담, 증거로 진술 모순 지적, 추가 진술.
- `EvidenceJournal`: 증거 출처/관찰 사실/추론 가설 분리.
- `TimelineReconstruction`: 사건의 인물·방·시간·물증을 연결하는 재구성 기능.
- `NaturalLanguageDeductionParser`: 플레이어 **한국어 자유 입력**에서 범인/수법/흉기와 증거 구절 추출. LLM 단계는 진실 수정 권한 없음.
- `DeductionNormalization`: 이름/동의어/오타와 method signature 의미를 공인된 ID/불명확 상태로 정규화.
- `AccusationResolver`: 구조화된 `culpritNpcId, methodId, weaponId`를 사건 진실과 결정론적으로 비교, 고발 기회/피드백 정책은 미정.
- `BoardWorldPresenter`: 고품질 3D 쿼터뷰 디오라마 + NPC 2D 8방향 비주얼/컷신.
- `SaveLoad`: 정답 유출 없이 사건 시드와 조사 진행 저장.

## 전체 흐름
템플릿 제약 사건 생성/검증/진실 동결 → 로비에서 시작 → 8개 방 조사 및 NPC 자유 한국어 대화와 알리바이 대조 → 진술 원본기록·물증 연결/모순 지적 → 사건 시간표 재구성 → **한국어 자유 서술 최종 고발 → 해석 확인 → 구조화 판정 → 엔딩**.

## 구현/운영 제약
- LLM은 언어 인터페이스이며 진실 데이터 관리와 판정기는 격리한다.
- NPC 대화에 제공되는 내용은 해당 캐릭터가 알고 있는 진실/거짓 주장 범위로 제한한다. NPC 자유 입력은 질문 선택지로 축소하지 않는다.
- 사건 사실 데이터·LLM 생성 발화·자연어 최종 추리 입력을 서로 격리한다. 대화 결과는 검증된 상태 변경만 적용.
- `NPC_FREE_DIALOGUE.md`와 `RANDOM_CASE_AI_DESIGN.md`를 구현 계약으로 사용하고, LLM 연결 시 JSON 스키마/권한 검증/프롬프트 주입 차단/비밀키 서버 보관/지연·장애 복구를 설계한다.
- 예전 GameState/TurnManager/Card/AIController/AbilityManager/장소 정답 체크 코드는 새 설계에 자동 합류하지 않는다.
- 구형 `game/UnityProject`는 원본 보존. 새 코드 구현과 Unity Play Mode/모바일 성능 테스트는 아직 수행하지 않았다.
