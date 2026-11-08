# NPC 프리셋 대사 카탈로그 — 실행 연동 가이드 (2026-11-08)

## 현재 보관 파일
- `npc_dialogue_catalog.v1.json`: **6명 × 상황 38종 = 228개**의 작가 작성 한국어 기초 대사. 캐릭터 성격은 사용자 지정 원문을 `CHARACTERS_PERSONALITY.md`에 보존.
- 파일은 기존 게임 코드에 자동 연결되지 않았다. **원문/스키마 초안 저장 및 기본 데이터 무결성 검토 완료**, 실제 사건별 대사·게임플레이/AI 선택 엔진 검증은 미완료.

## NPC 구성
| npcId | 이름 | 사용자 고정 성격 |
|---|---|---|
| MATTO | 마또 | 엉뚱, 여린 속마음 감춤, 외향적 |
| JJIRO | 찌로 | 털털, 사려 깊음, 긍정적 |
| MARIHI | 마리히 | 문학소녀, 오타쿠, 긍정적, 상상력 풍부 |
| NAZ | 나즈 | 포근함, 누나미, 줏대, 안정형 |
| BOKI | 보키 | 약한 사투리, 미소녀, 괴식 선호, 일반인 느낌 |
| MOONIRE | 문이레 | 조용함, 나긋함, 소심, 고집 강함 |

## 대사 레코드 필드
- `lineId`: 완전 고유 문자열, 저장 시 삭제/재사용 금지. 예 `MATTO_CONFRONT_VALID_01`.
- `npcId`: 위 6인 중 1명.
- `situation`: 38개 대화 상황의 정규화 코드.
- `intentTags`: 자연어 입력을 AI 분류기가 매칭할 의도/주제 분류.
- `requiredState`: **실제 게임 규칙이 계산한 대사 허용 조건**. AI가 이 조건을 임의로 true로 만들어선 안 된다.
- `responseText`: 화면에 출력할 작가 작성 원문. AI가 문구를 새로 생성·다듬지 않는다.
- `emotionTag`: `neutral, smile, surprise, worried, angry, serious` 중 하나.
- `animationTag`: `idle, talk, gesture, react` 중 하나.
- `templateSlots`: 단서/진술이 제공되어야만 치환 가능한 작가-승인 슬롯. `approvedCaseStatement`는 **사건별로 작성된 진술 내용**만 참조; LLM 생성 문장 금지.
- `status: authored_base`: 캐릭터별 베이스 대사 초안. 실제 사건 대사와 정합성 검수 후 사용 범위를 승인.

## 38개 상황
`INTRO_FIRST`, `INTRO_RETURN`, `SMALLTALK`, `CRIME_REACTION`, `VICTIM_QUESTION`, `ASK_ALIBI`, `ALIBI_FACT`, `ASK_TIME`, `ASK_WITNESS`, `WITNESS_FACT`, `ASK_RELATION`, `ASK_OTHER_NPC`, `ASK_ROOM`, `ASK_ITEM`, `ASK_WEAPON`, `ASK_METHOD`, `ASK_RECORD`, `EVIDENCE_SHOWN`, `EVIDENCE_UNKNOWN`, `CONFRONT_NO_PROOF`, `CONFRONT_VALID`, `RETRACT_VALID`, `MISREMEMBER_VALID`, `PRESSURE_NO_PROOF`, `ACCUSED_NO_PROOF`, `ACCUSED_WITH_EVIDENCE`, `TRUST_GAIN`, `TRUST_LOSS`, `REPEAT`, `REFER_PREVIOUS`, `UNCLEAR`, `UNKNOWN`, `DECLINE_PRIVATE`, `NEED_BREAK`, `FAREWELL`, `DILEMMA_OPINION`, `GUARDED_ANSWER`, `COMFORT_PLAYER`.

## 게임 엔진에서 지켜야 할 선택 절차
1. 플레이어 자유 질문에서 의도/대상/언급 증거/기존 발언을 파악.
2. 실제 NPC ID/사건 시드/지식/이전 대화/플레이어 발견 증거를 읽는다.
3. **먼저** `requiredState` 조건으로 선택 가능한 대사만 필터링.
4. AI는 필터된 후보 **ID 중에서만** 의미상 최적 항목을 검색·랭킹해 `lineId` 반환. 자유 텍스트 응답 생성 금지.
5. 현재 이벤트와 `lineId`를 다시 대조해 서버/게임 규칙에서 최종 허용 검사.
6. `responseText`는 있는 그대로 출력. `ALIBI_FACT`/`WITNESS_FACT` 등 조건부 템플릿은 **작가가 실제 사건별로 작성한 `approvedCaseStatement` 존재 시에만 치환**. 문장 연결/조사 처리를 미리 검수.
7. `lineId`, 사용자 질문, 대사 원문, 사건 진술 ID, 증거 제시 ID를 저장. 정정되더라도 예전 발화 원문은 남긴다.

## 가장 위험한 오용 방지
- **`ALIBI_FACT`/`WITNESS_FACT`는 사건 진술 ID가 준비되지 않았다면 선택 절대 금지**. 6×2 = 12개는 동적 사건 데이터 없이는 실사용 대사가 아니다.
- `UNKNOWN`은 `NO_APPROVED_ANSWER_AVAILABLE`일 때의 안전한 상황 대사. **NPC가 아는 확정 사실을 무조건 모른다고 말하게 하면 안 된다**.
- `CONFRONT_VALID`, `RETRACT_VALID`, `MISREMEMBER_VALID`은 진술 ID/증거 ID와 이벤트 허용 검증 이후만 사용. AI가 감으로 추궁 성공을 선언하지 않는다.
- NPC가 범인이라는 이유만으로 특정 불안 표정·경고 대사가 선택되면 범인 스포일러가 되므로 이 베이스 카탈로그의 `requiredState`는 범인 여부가 아닌 **대화·증거·심리 상태**를 기준으로 한다.
- 이벤트 조건만으로 데이터 유효성을 자동 보장하지 않는다. **사건별 팩**은 원문/연결 증거/해금 조건/의심 해소 경로를 별도 검수해야 한다.

## 대사와 스프라이트 연동
`emotionTag` → `NPC_SPRITE_SPEC.md`의 6개 대화 표정 중 해당 얼굴을 선택. `animationTag` → 준비된 입/반응 레이어만 선택. **상황 대사가 228개라도 신규 이미지 228장이 필요한 건 아니다.**

## 추가 대사 제작이 필요한 내용
- 각 사건 변형에서 실제 NPC가 있었던 시간/방, 목격 대상, 범행 과정/흉기/관계·비밀 관련 구체 진술.
- 범인 6명 변경으로 바뀌는 참·거짓 알리바이와 증거 기반 고백/번복.
- 캐릭터가 실제로 확정받지 않은 직업/연령/개인 역사/상호관계.
- 특수 상황: 단서가 특정 NPC에게 없거나, 침묵·의심·재심문이 연속될 때 필요한 추가 반응.

게임룸 총 8개, 최종 정답은 **범인·범행수법·흉기**, 범행 장소는 정답이 아니라 고정 수사 장소. Unity 로딩/AI 연동/테스트는 별도 요청과 구현 단계에서 수행.
