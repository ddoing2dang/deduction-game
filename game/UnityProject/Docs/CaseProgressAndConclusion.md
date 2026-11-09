# 사건 진행 저장·불러오기 및 결말 화면

2026-11-10 · `mystery` Windows PC · **GitHub 소스/프리팹 작성만 수행**

## 저장·불러오기 단계

- `CaseProgressSaveController`가 기존 대화창 오른쪽 위에 **진행 저장** 탭을 만들고, **지금 저장 / 불러오기** 버튼을 제공한다.
- 인가된 증거 발견, 진술 기록, 범인 지목 성공·실패가 발생하면 자동 저장한다. 게임 시작/테마 전환 시 **그 사건의 기존 저장 데이터가 있으면 1회 자동 불러온다**.
- 파일은 Unity Windows용 `Application.persistentDataPath/mystery-case-saves/case_0.json` (대저택), `case_1.json` (크루즈), `case_2.json` (음악방송)으로 분리한다.
- 저장 내용: `schemaVersion=1`, `theme`, `caseId`, **실제 획득한 증거 ID**, **오답 시도 횟수**, **해결 여부**, **NPC 6명의 사건별 대화 기록·마지막 답변**.
- 기존 파일 덮어쓰기는 임시 파일을 먼저 기록한 뒤 Windows `File.Replace`를 이용하고, 이전 버전은 `.bak`로 남긴다. 첫 저장은 원본 파일이 없을 때 이동한다.
- **저장 파일을 무조건 신뢰하지 않는다.** 스키마, 사건 ID·테마, 증거 ID 전체, 중복, 오답 횟수, 필수 증거 소지 여부, 대화 기록 인덱스·길이 등을 검증한다. 오류 데이터는 복원을 거부한다.
- 기존 사건 데이터가 바뀌면 저장 파일 혼동을 피하기 위해 **사건 `caseId`도 새 버전으로 갱신**하는 것이 좋다.

## 사건 해결 결과 단계

- `CaseConclusionView`는 **승인된 사건에서 충분한 증거를 가진 올바른 용의자 지목이 성공할 때만** 결말 UI를 연다.
- 결말 문구는 사건의 `verdict.successText`를 사용한다. 미승인·미해결 사건은 결말 버튼을 비활성화해 범인/진실을 노출하지 않는다.
- 성공한 사건을 불러오면 **사건 결말** 탭에서 결말을 다시 열 수 있다.
- 기존 **현장 조사 / 수사 수첩 / 진행 저장 / 사건 결말**의 팝업은 한 번에 하나만 열리도록 연결한다.
- NPC 초상화 6개, 원본 프레임과 대사 영역, 기존 텍스트·이름·사건 JSON은 **변경하지 않는다**.

## 파일

- `Assets/Interrogation/Scripts/CaseProgressSave.cs` — JSON 직렬화에 사용하는 사건별 상태 DTO
- `Assets/Interrogation/Scripts/CaseProgressSaveController.cs` — Windows 저장·복원, 자동 저장, 런타임 메뉴
- `Assets/Interrogation/Scripts/CaseConclusionView.cs` — 결말 안내/재열람 런타임 UI
- `Assets/Interrogation/Scripts/DialogueInterrogationAdapter.cs` — 상태 추출·검증·적용, 사건 상태 이벤트
- `Assets/UI/Scripts/DialoguePanel.cs` — 테마/NPC별 대화 기록 직렬화·복원
- `Assets/UI/Prefabs/DialoguePanel.prefab` — 두 컴포넌트와 Pretendard 폰트 참조 직렬화

## 중요한 제한

- 현재 세 사건은 `approved: false`. **이 상태에서는 저장/복원·증거 수집·지목·결말도 잠긴다.** 초안의 잠금 설정을 자동으로 해제하거나 범인을 확정하지 않았다.
- 플레이어가 사용할 사건을 확정하고 `approved: true`로 수정하려면 별도 사건 내용 승인과 사건 데이터 검수가 필요하다.
- **로컬 Windows Unity 폴더에 자동 반영되지 않는다.** GitHub 브랜치 변경만 수행했으며 사용자가 로컬 프로젝트에 동기화해야 한다. 기존 두 로컬 SDF 폰트는 계속 보존한다.
- Unity 컴파일 검증, Play Mode, 기능 테스트, EXE 빌드는 실행하지 않았다. **코드 저장만으로 작동 검증 완료라고 보고하지 않는다.**
