# mystery — DialoguePanel 제작 결과

현재 실행 중인 Unity의 `Library/EditorInstance.json`, 에디터 로그, Unity Pipeline/MCP 상태에서 실제 프로젝트 경로 **`C:/UnityProjects/DeductionGame/mystery`**를 확인했다. 새 Unity 프로젝트를 생성하거나 에디터를 다시 실행하지 않았다. 현재 열린 **Unity 6000.3.26f1**에 **Unity MCP**로 직접 제작 명령을 보내 저장했다.

## 실제 생성 파일

- `Assets/UI/Prefabs/DialoguePanel.prefab`
- `Assets/Scenes/DialoguePreview.unity`
- `Assets/UI/Scripts/DialoguePanel.cs`
- `Assets/UI/Scripts/DialogueImeInput.cs`
- `Assets/UI/Editor/DialogueAssetAuthoring.cs`
- `Assets/UI/Art/DialogueFrames/Mansion.png`, `Cruise.png`, `MusicBroadcast.png`
- `Assets/UI/Fonts/NotoSansKR-Regular.otf`, `NotoSansKR SDF.asset`, `OFL.txt`
- 위 파일과 폴더의 `.meta`, Unity 제공 `Assets/TextMesh Pro/` 필수 리소스와 `.meta`.

기존 프로젝트 코드, 구형 씬/맵, 사건·대사 JSON은 가져오지 않았다. 기존 `SampleScene.unity`와 프로젝트 패키지/설정도 변경하지 않았다. 미리보기 씬은 새 빈 씬에서 제작했으며 기존 씬과 별도로 저장했다.

## 프리팹 구성

```text
Canvas (1920×1080 / Scale With Screen Size / Expand)
└─ DialoguePanel (아래 중앙 기준 / 투명 루트)
   ├─ Frame (원본 PNG Image / Simple / Preserve Aspect / 흰색 tint)
   │  ├─ NpcName (TextMeshPro)
   │  └─ NpcDialogue (TextMeshPro)
   ├─ QuestionInput (TMP_InputField / 자유 한국어 입력)
   ├─ SendButton (화면에 있는 유일한 Button)
   ├─ Status
   ├─ HistoryTitle
   └─ DialogueHistory (ScrollRect)
      ├─ Viewport / Content / HistoryText
      └─ Scrollbar
```

프레임의 가로세로 비율은 **2048:682**다. 모든 PNG는 GitHub `main`의 지정 경로에서 직접 내려받았고 원본 바이트를 유지한다. 장식/테두리/알파를 수정하거나 별도 배경 Image로 프레임을 불투명하게 만들지 않았다. Sprite (2D and UI), Alpha Is Transparency, Mipmap 꺼짐, 무압축, NPOT 크기 유지, Standalone RGBA32로 저장했다. 대저택 기본값이며 다른 테마도 동일 UI를 공유한다.

## Unity에서 열기

1. 현재 `mystery`의 Project 창에서 `Assets/Scenes/DialoguePreview.unity`를 더블클릭한다. 작업 종료 시 이 씬을 Edit Mode로 열어 두었다.
2. **Game 탭을 Play 없이** 열고 화면 비율을 1920×1080 또는 16:9로 선택하면 정적 미리보기를 볼 수 있다.
3. Hierarchy의 `Canvas > DialoguePanel`을 선택한다.
4. Inspector의 **Dialogue Panel > Inspector presentation**에서 **Theme**을 `MANSION`, `CRUISE`, `MUSIC_BROADCAST` 중 선택한다. 이름표/본문/입력창은 공유하고 프레임 Sprite만 교체한다. 게임 화면에는 테마 버튼을 추가하지 않았다.
5. **Speaker**에서 마도연·마리예·나주은·이복희·진루나·문일혜를 선택한다. **Preview Text**는 정적 미리보기용 안내 문구이며 사건 대사가 아니다.
6. 프리팹 자체를 편집하려면 `Assets/UI/Prefabs/DialoguePanel.prefab`을 더블클릭한다. 프리팹 모드에서도 Inspector로 표시를 조정할 수 있다.

## 연결 범위

- 전송 버튼/Enter 제출 → 질문 원문을 현재 NPC의 세션 기록에 추가 → `questionSubmitted(string)` 이벤트를 호출하는 코드를 작성했다. 고정 질문 선택지는 없다.
- 추후 승인된 대사 어댑터는 현재 NPC의 사건 권한을 검증한 뒤 `ShowNpcReply(string)`로 표시할 수 있다. UI 자체가 NPC 답변이나 사건 사실을 생성하지 않는다. 범인 전용 거짓말 규칙을 판정할 사건/대사 엔진은 이번 UI 범위에 포함하지 않았다.
- `DialogueImeInput`은 현재 프로젝트의 **Input System 전용 설정을 유지**하면서 TMP의 IME 조합 문자열/후보 위치를 Keyboard API로 연결한다. `BaseInputModule.inputOverride`가 직렬화되지 않으므로 활성화 시 다시 바인딩한다.
- 이전 대화는 NPC별 **현재 실행 세션 메모리**에 유지한다. 디스크 저장·실제 사건·AI 검색 연동은 추가하지 않았다.
- 이전 기본 폰트 Noto Sans KR은 기존 에셋으로 보존. **신규 적용 폰트는 MaruBuri(이름/대사) + Pretendard(입력/기록/버튼/상태)**이며 `Assets/UI/Fonts/TYPOGRAPHY.md`의 Edit Mode 자동 적용 스크립트를 통해 생성·프리팹 갱신한다. 설치된 Unity Editor에서 프리팹 저장이 완료되기 전에는 기존 폰트가 보일 수 있다.

## 확인 및 미실시

**정적 확인: OK.** Unity MCP 제작 명령 성공, 새 C# 컴파일 오류 없음, UI 참조 11개 저장, Button 1개/TMP_InputField 1개/ScrollRect 1개, 3테마·6이름 설정 저장, PNG 3종 원본 SHA256 일치. 대저택 1920×1080 정적 렌더에서 한글과 배치·알파를 확인했다. 다른 테마/해상도의 실행 동작을 기능 테스트했다고 보고하지 않는다.

**기능 테스트, Play Mode, EXE 빌드는 실행하지 않았다.** 한국어 입력/IME/전송/스크롤의 런타임 동작은 미검증이다. 저장된 씬에는 `Main Camera`, `Canvas`, `EventSystem`과 Canvas의 `DialoguePanel` 프리팹 인스턴스만 있다. 맵·건물·방·바닥·NPC 모델을 생성하지 않았다.

## GitHub 전달 형식

`game/UnityProject/Assets/`에는 현재 mystery에서 생성한 UI 에셋, 미리보기 씬과 필수 TMP 리소스만 복사해 전달한다. **새 로컬 Unity 프로젝트를 만든 경로가 아니다.** 기존 mystery에 적용하는 에셋 묶음이며 구형 프로젝트 설정/게임 코드는 포함하지 않는다. 의존 패키지는 현재 프로젝트의 UGUI 2.0.0, Input System 1.20.0, URP 17.3.0이다. 새 프로젝트 생성 없이 기존 mystery의 동일 Assets 상대 경로에서 사용한다.

원본 출처/해시는 `DialogueFrameSources.json`, 정적 에디터 보고서는 `DialogueAuthoringReport.json`에 별도로 기록했다.

## 2026-11-09 대화창 폰트 교체

# mystery dialogue typography — MaruBuri + Pretendard

## Roles applied to the existing `Assets/UI/Prefabs/DialoguePanel.prefab`
- `NpcName`: **MaruBuri SemiBold**
- `NpcDialogue`: **MaruBuri Regular**
- `Text` (TMP_InputField), `Placeholder`, `HistoryText`, `Status`: **Pretendard Regular**
- `HistoryTitle` and SendButton's `Label`: **Pretendard SemiBold**

The original themed PNGs, Canvas, positioning, colors, events and Input System settings are unchanged.

## Applying on an already-open Unity project
The GitHub branch contains `Assets/UI/Editor/ApplyDialogueTypography.cs` and the **actual original OTF font bytes for MaruBuri Regular + SemiBold**. After updating the `mystery` project with this branch, Unity's Editor imports the script and **tries the one-time font migration automatically in Edit Mode**.

If network access prevents the migration, run **Mystery > UI > Apply MaruBuri + Pretendard Fonts** once after Internet is available. The script downloads **Pretendard Regular + SemiBold directly from the original author**, pinned to commit `7aeb0698819be2b4097dae8ec8fe6a795e5cf3ae`, verifies their Git blob hashes, creates four dynamic TMP SDF assets under this folder, and saves the existing prefab with all eight text components assigned to their new fonts.

Pretendard OTF files are **not yet GitHub-committed** because GitHub's connected text-file interface does not retrieve original OTF binary files larger than 1 MB; the editor installer downloads them instead. **After Unity runs the installer, commit the generated Pretendard OTFs, TMP SDF assets and their `.meta` files from the local `mystery` project to make the branch entirely self-contained.**

This change was prepared against the GitHub files. It was not executed in the user's local Unity editor from this conversation. No Play Mode, function testing, or executable build was run.

## Sources and licensing
- **MaruBuri**, copyright NAVER Corporation and NAVER Cultural Foundation: https://hangeul.naver.com/font and https://help.naver.com/service/30016/contents/18088?osType=PC
  - The fonts may be embedded and redistributed under NAVER's open font license. The font files must not be sold alone.
  - Source font mirror, file hashes verified against repository blobs: https://github.com/fonts-archive/MaruBuri
- **Pretendard**, copyright Kil Hyung-jin and contributors, SIL Open Font License 1.1: https://github.com/orioncactus/pretendard
  - Exact license text is included as `Pretendard-OFL.txt`.

## Verification boundaries
The GitHub code and font import assets can be reviewed statically. **Actual TMP font generation and prefab reserialization take place inside Unity Editor** when this change is imported; this response does not claim they already ran on your PC.



## UI 마무리 — 이름 변경 및 초상화 슬롯 (2026-11-09)

- 이름: **문이린 → 문일혜**. 나머지 다섯 NPC 이름, 기존 enum 순서, 기존 테마 PNG/알파는 유지.
- `DialoguePanel`의 이름표는 `speaker.ToString()`에서 표시되므로, Inspector Speaker의 여섯 번째 옵션부터 **문일혜**로 표시.
- `NpcPortrait`는 **실제 승인된 Sprite가 있을 때만 보이는** 선택적 이미지 슬롯. 아직 캐릭터 초상화 6개의 원본 PNG가 GitHub에 없으므로 임의 이미지를 생성하거나 연결하지 않음.
- `speakerPortraits` 배열에 NPC별 Sprite를 넣으면 자동 전환. 초상화가 연결되면 원본 프레임 오른쪽에 표시하며, 대사가 겹치지 않도록 본문 폭을 줄임. 초상화가 없으면 기존 대화창 배치 유지.
- 새로운 Editor 코드 `Assets/UI/Editor/FinalizeDialogueUI.cs`는 기존 프리팹을 **Edit Mode에서만** 열고 필요한 Sprite 슬롯 및 6개 바인딩을 추가해 저장. 기존 씬, UI PNG, TMP 입력·대사기록, EventSystem 및 버튼 동작을 변경하지 않음.
- 기존 MaruBuri/Pretendard Edit Mode 폰트 적용 스크립트는 별도로 작동. GitHub 코드 저장은 완료되지만 로컬 Unity에서 실행해야 실제 에셋과 프리팹 직렬화가 완료됨.
- **테스트, Play Mode, 빌드 없음.** 로컬 Unity 조작은 이번 대화에서 불가능하므로 실제 적용 성공을 주장하지 않음.


### 2026-11-09 NPC 이름 최신 변경

NPC 이름은 **마도연 · 마리예 · 나주은 · 이복희 · 진루나 · 문일혜**로 확정. 기존 6인 순서 및 `DialogueSpeaker` enum 직렬화 숫자(0~5)를 유지하고, 초기 대화창 이름표 프리팹 텍스트도 마도연으로 변경. 기존 PNG와 프리팹 구조·캐릭터 선택 인덱스·UI 동작은 변경하지 않았습니다. 로컬 Unity Editor 동작 확인, Play Mode 및 빌드는 미실시.
