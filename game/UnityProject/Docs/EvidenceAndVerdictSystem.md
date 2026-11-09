# 증거 수첩 · 범인 지목 시스템 v1

2026-11-10 · `mystery` PC / Unity 6 / **소스 저장 및 직렬화 확인 범위만 작업**

## 플레이어 흐름

1. 게임의 실제 단서 버튼이나 훗날 3D 오브젝트 상호작용에서 `EvidencePickup.Collect()`를 호출한다.
2. `EvidencePickup`이 등록된 `evidenceId`를 `DialogueInterrogationAdapter.UnlockEvidence(id)`에 전달한다. **게임 내 질문 입력, 수첩 목록 자체로는 증거를 획득할 수 없다.**
3. 기존 `DialoguePanel.prefab`의 우상단에 **수사 수첩** 버튼이 런타임 생성된다. 수첩에는 획득한 증거 제목만 노출되며 미발견 증거의 이름과 ID는 숨긴다.
4. 플레이어가 수첩에서 증거를 선택하고 NPC 6명 중 한 명을 선택해 **증거와 함께 지목하기**를 누른다.
5. 판정 엔진은 사건 승인 → 현 사건에 존재하는 단서인지 → 실제 획득한 단서인지 → 선택한 근거가 필수 증거를 충족하는지 → 지목한 NPC가 범인인지 순서로 확인한다.
6. **근거가 부족할 때는 오답 횟수를 소모하지 않는다.** 충분한 근거로 오답을 제출했을 때만 지목 기회가 감소한다. 대저택 초안은 2회로 설정했다.
7. 오답일 때 범인의 이름이나 아직 얻지 못한 필수 증거를 노출하지 않는다. 정답일 때만 사건 완결 문구를 표시한다.

## 대저택 제1사건: 잠정 지목 조건

| 필수 증거 ID | 정황 |
|---|---|
| `m01_lock` | 21:08·21:11 G3 서비스 문 출입 기록 |
| `m01_key` | G3 카드 지급 대장 |
| `m01_mud` | 서비스 문 앞 온실 흙 작업화 흔적 |
| `m01_ledger` | 후원금 전용 회계 사본 |

네 가지를 **실제로 획득하고 수첩에서 모두 제출**해야 지목 판정으로 넘어간다. 잠정 범인은 진루나(인덱스 4). 사용자 사건 승인 전이므로 `approved: false`를 그대로 유지했다.

## 구현 위치

- `Assets/Interrogation/Scripts/AccusationVerdictRules.cs`: 단서 진위/소유 여부와 범인 지목의 순수 판정 로직
- `Assets/Interrogation/Scripts/EvidencePickup.cs`: 나중에 배치할 증거 상호작용의 연결 컴포넌트. 새 맵이나 가짜 증거 위치는 만들지 않음
- `Assets/Interrogation/Scripts/InvestigationNotebookView.cs`: 런타임에서 생성되는 수첩·증거 선택·6인 지목 UI. 프리팹에 컴포넌트와 Pretendard 폰트 참조 저장
- `Assets/Interrogation/Scripts/DialogueInterrogationAdapter.cs`: 사건별 인벤토리, 단서 획득 이벤트, 획득한 단서만 열람, 범인 지목 시도·결과 관리
- `Assets/Interrogation/Scripts/InterrogationCaseData.cs`: 사건별 판정 설정 `CaseVerdict`
- `Assets/Interrogation/Cases/Mansion.json`: 필수 증거 4종, 오답 허용 2회, 결말 문구. 사건 자체는 미승인

## 한계 및 안전 경계

- **사건 승인 전:** `approved: false`인 대저택·크루즈·음악방송 세 사건에서는 실제 증거 획득 및 지목이 잠김. 기존 NPC 대사도 여전히 미승인 상태.
- **맵/증거 포인트 미배치:** UI와 상호작용 API를 준비한 것이지, 증거를 실제로 찾을 수 있는 스테이지를 만든 것은 아님. 추후 단서 발견 화면 또는 3D 오브젝트와 `EvidencePickup`을 연결해야 한다.
- **저장:** 증거 인벤토리, 지목 실패 횟수, 사건 해결 여부는 현재 실행 세션 메모리에만 유지. 세이브/로드 미구현.
- **기능 확인:** 로컬 Unity Editor, Play Mode, 컴파일, 기능 테스트, EXE 빌드 **실행하지 않음**. GitHub 정적 파일/참조 확인만 가능.
- **기존 리소스:** NPC 초상화 6장·원본 대화창 PNG 3종·폰트 4종·기존 장면과 질문 입력 UI를 보존.
