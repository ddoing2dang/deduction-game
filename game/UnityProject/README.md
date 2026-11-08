# DialoguePrototype — 신규 PC Unity 프로젝트

- Editor: **6000.3.26f1**, Universal 3D 템플릿 기반 **URP 17.3.0**, Windows 64-bit 제작 대상.
- `Assets/Scenes/DialoguePrototype.unity`: 신규 씬. 기존 Game 씬 및 구형 게임 코드는 사용하지 않는다.
- `Assets/Prefabs/DialoguePanel.prefab`: 원본 프레임 Image, TMP 이름표/대사, TMP 자유 질문 입력창, 전송 버튼, 스크롤 기록, NPC 6명/테마 3종 전환 버튼.
- `Assets/Scripts/Dialogue/DialoguePrototypeView.cs`: 질문 제출, NPC/테마 전환, 사건·NPC별 대화 기록. 질문·응답·시스템 안내를 구분한다. 원문/대사 ID/시각을 보존하고 번복 시에도 과거 발언을 지우지 않는다.
- 기록 파일: `Application.persistentDataPath/dialogue-journal.v1.json`. 저장 실패 시 세션 기록을 유지하며 UI에 안내한다. 프로토타입 기록과 추후 실제 사건 기록은 caseId로 분리한다.
- `Assets/Scripts/Dialogue/DialoguePolicy.cs`: 승인된 사건 대사·지식·사실·제시 증거·이전 발언 게이트. 범인 1명만 승인된 거짓 진술 가능. 실제 허위 발언이 있었던 범인만 철회 가능. 미승인/출처 불명 대사는 차단한다.
- `Assets/Editor/CreateDialoguePrototype.cs`: Edit Mode 제작용 생성기. 기존 결과물이 있으면 덮어쓰지 않는다. 테스트/Play Mode/빌드 코드는 없다.

## 원본 보존

`Assets/Art/DialogueFrames/`의 PNG는 저장소 `art/UI/DialogueFrames/ThemeDialogue_v1/`와 바이트가 같다. Simple Image, 종횡비 유지, 무압축 Sprite, 알파 유지. 프레임 재가공·9-slicing 없음. 이름/본문은 별도 TMP 레이어다.

`Assets/Data/Preserved/`는 현행 `game/characters`, `game/dialogue`, `game/cases`, `game/maps`의 보존용 사본이다. 원본 JSON을 수정하지 않았다. NPC 이름은 `npc_display_profiles.v1.json`에서 읽는다. 사건·대사 초안은 Resources/StreamingAssets 또는 런타임 자동 대사 선택기로 연결하지 않았다. 원본을 추후 갱신하면 사본도 명시적으로 동기화해야 한다.

한국어 글꼴은 Google Fonts Noto Sans KR (SIL OFL 1.1), 라이선스는 `Assets/Art/Fonts/OFL.txt`. Dynamic SDF로 자유 한국어 입력을 지원하는 구조다. IME 조합 중 Enter 제출을 막으며 Input System UI 모듈과 TMP의 IME 처리를 위해 Active Input Handling은 Both로 설정한다.

## 현재 경계와 미완료

- **Unity MCP 연결 없음.** 플러그인 검색에서도 Unity MCP를 확보하지 못해 설치된 Unity Editor의 batch Edit Mode로 에셋을 제작했다. Unity Hub GUI로 생성한 것은 아니다.
- 승인된 실제 사건/증언팩과 AI 의도 검색 어댑터(`IApprovedDialogueSource`)는 미연결이다. 질문은 기록하지만 NPC 답변을 임의 생성하지 않으며 시스템 안내만 표시한다. 대사 본문은 승인된 답변 전에는 비어 있다.
- 테마 버튼은 프레임만 바꾼다. 사건 진실/맵/기록을 테마 버튼으로 바꾸지 않는다. 실제 사건 변경은 검증된 authority 바인딩을 통해서만 한다.
- NPC 스프라이트/초상화, 3D 맵, 사건 생성기, 증거 UI, 최종 고발은 이번 제작 범위 밖이다.
- **기능 테스트, Play Mode, EXE/APK 빌드를 실행하지 않았다.** 입력·IME·저장 복원·스크롤·테마 교체의 런타임 동작 및 실제 해상도 외관은 미검증이다.

## 기존 프로젝트 안전성

기존 로컬 `C:/UnityProjects/DeductionGame/deduction-game`는 수정·삭제·이동하지 않았다. 새 제작 저장소는 `C:/UnityProjects/DeductionGame/fresh-dialogue`, 프로젝트 상대 경로는 `game/UnityProject`다. GitHub 백업 `archive/pre-fresh-unity-project-20261109`가 `e42612484014d8d0d8a2c948e7471e6b0a7375e9`를 가리키는 것을 확인했다. 이 원격 백업이 기존 로컬 미커밋 변경까지 보관한다는 뜻은 아니다. 기존 로컬 폴더는 그대로 남아 있다.
