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
5. **Speaker**에서 마도아·마리예·나주아·복희·지로아·문이린을 선택한다. **Preview Text**는 정적 미리보기용 안내 문구이며 사건 대사가 아니다.
6. 프리팹 자체를 편집하려면 `Assets/UI/Prefabs/DialoguePanel.prefab`을 더블클릭한다. 프리팹 모드에서도 Inspector로 표시를 조정할 수 있다.

## 연결 범위

- 전송 버튼/Enter 제출 → 질문 원문을 현재 NPC의 세션 기록에 추가 → `questionSubmitted(string)` 이벤트를 호출하는 코드를 작성했다. 고정 질문 선택지는 없다.
- 추후 승인된 대사 어댑터는 현재 NPC의 사건 권한을 검증한 뒤 `ShowNpcReply(string)`로 표시할 수 있다. UI 자체가 NPC 답변이나 사건 사실을 생성하지 않는다. 범인 전용 거짓말 규칙을 판정할 사건/대사 엔진은 이번 UI 범위에 포함하지 않았다.
- `DialogueImeInput`은 현재 프로젝트의 **Input System 전용 설정을 유지**하면서 TMP의 IME 조합 문자열/후보 위치를 Keyboard API로 연결한다. `BaseInputModule.inputOverride`가 직렬화되지 않으므로 활성화 시 다시 바인딩한다.
- 이전 대화는 NPC별 **현재 실행 세션 메모리**에 유지한다. 디스크 저장·실제 사건·AI 검색 연동은 추가하지 않았다.
- 한국어 글꼴은 Noto Sans KR Regular, SIL OFL 1.1이며 원본과 라이선스를 함께 포함했다. TMP 동적 다중 atlas를 사용한다.

## 확인 및 미실시

**정적 확인: OK.** Unity MCP 제작 명령 성공, 새 C# 컴파일 오류 없음, UI 참조 11개 저장, Button 1개/TMP_InputField 1개/ScrollRect 1개, 3테마·6이름 설정 저장, PNG 3종 원본 SHA256 일치. 대저택 1920×1080 정적 렌더에서 한글과 배치·알파를 확인했다. 다른 테마/해상도의 실행 동작을 기능 테스트했다고 보고하지 않는다.

**기능 테스트, Play Mode, EXE 빌드는 실행하지 않았다.** 한국어 입력/IME/전송/스크롤의 런타임 동작은 미검증이다. 저장된 씬에는 `Main Camera`, `Canvas`, `EventSystem`과 Canvas의 `DialoguePanel` 프리팹 인스턴스만 있다. 맵·건물·방·바닥·NPC 모델을 생성하지 않았다.

## GitHub 전달 형식

`game/UnityProject/Assets/`에는 현재 mystery에서 생성한 UI 에셋, 미리보기 씬과 필수 TMP 리소스만 복사해 전달한다. **새 로컬 Unity 프로젝트를 만든 경로가 아니다.** 기존 mystery에 적용하는 에셋 묶음이며 구형 프로젝트 설정/게임 코드는 포함하지 않는다. 의존 패키지는 현재 프로젝트의 UGUI 2.0.0, Input System 1.20.0, URP 17.3.0이다. 새 프로젝트 생성 없이 기존 mystery의 동일 Assets 상대 경로에서 사용한다.

원본 출처/해시는 `DialogueFrameSources.json`, 정적 에디터 보고서는 `DialogueAuthoringReport.json`에 별도로 기록했다.
