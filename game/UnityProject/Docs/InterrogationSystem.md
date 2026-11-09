# NPC 자유 심문 — 규칙 기반 v1

2026-11-09 · `mystery` Windows PC · GitHub 소스 변경

## 구현

- 기존 `DialoguePanel.prefab`에 `DialogueInterrogationAdapter` 컴포넌트를 추가하고 질문 전송 이벤트와 답변 표시 함수에 연결. **기존 프레임 3종, NPC 초상화 6장, 폰트, 입력창은 그대로 유지**.
- `Assets/Interrogation/Cases/{Mansion,Cruise,MusicBroadcast}.json`으로 사건별 독립적인 데이터 구성.
- 한국어 자유 질문에서 미리 정의한 주제 `aliases`를 정규화해 결정적으로 찾음. 자유 입력은 되지만 자연어 생성 AI가 아니므로 등록하지 않은 질문에는 자동 사실 생성 대신 **안내 기록**을 남김.
- 각 진술은 `truthfulText`를 기본값으로 사용하고, **범인으로 설정한 유일한 NPC만** `deceptiveText` 분기를 사용할 수 있음. 비범인에게 거짓 진술을 설정한 파일은 승인 상태에서 검증 거부.
- 단서 목록 `evidence`, 주제별 공개 조건 `requiredEvidenceIds`, 범인의 거짓 진술을 사실로 되돌리는 `deceptionBrokenByEvidenceId` 제공.
- 증거 획득은 **플레이어 질문 텍스트와 분리한** `DialogueInterrogationAdapter.UnlockEvidence(id)` 메서드로만 가능. 이후 사건 진행·증거 수집 시스템에서 호출.
- 대화 기록과 마지막 진술은 **테마 + NPC** 단위로 분리해 서로 다른 사건 기록이 섞이지 않도록 변경. 현재는 실행 중 메모리에만 보관함.

## NPC 순서 (수정 금지)

| index | 이름 | 이미지 |
|---:|---|---|
| 0 | 마도연 | Madoyeon.png |
| 1 | 마루희 | Maruhee.png |
| 2 | 나주은 | Najueun.png |
| 3 | 이복희 | Ibokhee.png |
| 4 | 진루나 | Jinruna.png |
| 5 | 문일혜 | Munilhye.png |

## 사건 작성 지침

현재 세 JSON은 모두 `"approved": false`, `"culpritSpeaker": -1`이다. 실제 사건과 범인/알리바이/증거를 아직 확정하지 않았으므로 **출시 가능한 NPC 대사는 일부러 넣지 않음**.

1. `profiles`의 6명에게 `personalityNotes`, `alibiNotes`, `knowledgeNotes`를 사건별로 기입한다. 이 필드는 작가용이며 자동으로 출력되지 않는다.
2. 사건의 유일한 범인을 `culpritSpeaker`에 0~5로 지정한다.
3. `evidence` 배열에 `{"id":"clue01","title":"확정된 증거 이름"}`처럼 실제 증거를 등록한다.
4. `topics`의 `aliases`는 질문 표현이다. `statements`를 아래 형태로 등록한다.

```json
{
  "id": "alibi",
  "aliases": ["알리바이", "어디 있었", "어디있었"],
  "requiredEvidenceIds": [],
  "statements": [
    {
      "speaker": 0,
      "truthfulText": "(사건 설정에 맞춰 작가가 검증한 진실)",
      "deceptiveText": "",
      "deceptionBrokenByEvidenceId": ""
    }
  ]
}
```

5. 범인에게만 `deceptiveText`를 채운다. `deceptionBrokenByEvidenceId`를 지정하면 해당 단서 획득 후 진실을 말하게 된다. `truthfulText`는 범인에게도 실제 진실이어야 한다.
6. 작가가 사건 모순, 알리바이, NPC 지식 범위, 문장 진위까지 확인한 뒤 `"approved": true`로 변경한다. **코드는 자연어 문장의 진실을 검증하지 못하므로 작가 승인이 필수**.
7. 증거 획득 UI, 사건 선택 UI, 세이브/로드, AI 대화 생성은 향후 별도 작업 범위이다.

## 작업 상태

**코드·프리팹·사건 초안 파일은 GitHub 작업 브랜치에 저장.** 이번 단계에서 로컬 Unity Editor 실행, 컴파일 확인, Play Mode, 기능 테스트, EXE 빌드는 하지 않았다.
