# 신규 대화 프로토타입 제작 기록 — 2026-11-09

## 실제 생성

- `Assets/Scenes/DialoguePrototype.unity`: 신규 카메라, Canvas, EventSystem, 프리팹 인스턴스.
- `Assets/Prefabs/DialoguePanel.prefab`: 원본 대저택 프레임 기본값, 이름/본문 TMP, 자유 질문 TMP_InputField, 전송 버튼, 이전 대화 ScrollRect, 6 NPC/3테마 버튼, 직렬화된 참조.
- `Assets/Scripts/Dialogue/DialoguePrototypeView.cs`: NPC 이름 JSON 연결, 테마 프레임 변경, 질문/대사/시스템 안내 구분, 사건·NPC별 로컬 대화 기록 저장 코드.
- `Assets/Scripts/Dialogue/DialoguePolicy.cs`: 승인 대사와 범인 전용 허위 진술 게이트 및 후속 사건 어댑터 인터페이스.
- `Assets/Editor/CreateDialoguePrototype.cs`: Edit Mode 제작 명령. 이미 생성된 씬/프리팹은 덮어쓰지 않는다.
- `Assets/Art/DialogueFrames/{Mansion,Cruise,MusicBroadcast}.png`: 원본 바이트 복사와 Sprite 설정.
- `Assets/Art/Fonts/`: Noto Sans KR 원본/OFL 라이선스/동적 TMP 폰트. `Assets/TextMesh Pro/`: Unity 제공 TMP 필수 리소스.
- `Assets/Data/Preserved/`: 기존 사건·대사·캐릭터·맵 자료 17파일과 Unity 메타데이터.
- `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/`: 신규 Universal 3D 템플릿에서 생성하고 에디터 6000.3.26f1에 맞춰 임포트한 설정. PC URP 파이프라인 사용, Win64 제작 대상. 구형 프로젝트 설정 복사 없음.

## 문서 반영

프로젝트 README와 `game/UNITY_RESTART_GUIDE.md`에 실제 경로, 생성 방법, 범위, 미완료 상태를 기록했다. 원본 기획/사건/대사/맵 및 원본 PNG를 변경하지 않았다.

## 수행한 확인

**검증: OK — 정적 파일·직렬화 참조·에디터 자동 컴파일에 한정.**

- Unity 제작 명령이 씬·프리팹을 저장하고 `DIALOGUE_ASSETS_CREATED`를 기록, 종료 코드 0.
- 최종 소스 수정 후 별도 에디터 임포트/자동 컴파일 종료 코드 0. 컴파일 오류 없음.
- PNG 3개 모두 원본/사본/manifest SHA256 일치.
- 보존 자료 17파일 모두 원본과 사본 해시 일치.
- 프리팹 내부 fileID 참조 연결 확인, UI 필드 및 테마별 Sprite/이름 JSON 참조 저장 확인.
- 기존 로컬 프로젝트 수정·삭제·이동 없음. 원격 백업 브랜치 존재 확인. 로컬 미커밋 자료는 기존 폴더에 그대로 유지.

## 미완료 및 미실시

- GitHub 저장 직전 원격 main이 `d9757f0`에서 `bd66e55`로 변경되었고, 사건·대사·맵 원본과 가이드 삭제가 포함되어 있었다. 이번 요청의 자료 보존 조건과 충돌하므로 main을 강제 덮어쓰거나 삭제 변경을 채택하지 않았다. 결과는 `codex/fresh-unity-dialogue-prototype-20261109` 브랜치에 저장한다. main 통합은 미완료이며, 보존 범위 충돌을 먼저 정리해야 한다.

- Unity MCP 미연결. 대신 설치된 Unity Editor의 batch Edit Mode와 정식 Universal 3D 템플릿 사용. Unity Hub GUI 생성은 미실시.
- 승인된 실제 사건/대사 선택 어댑터와 AI 의도 분류는 미연결. 미검증 기존 대사를 임의 승인/출력하지 않는다. 현재는 질문 저장과 응답 미연결 시스템 안내를 제공하는 코드이며 NPC 본문은 승인된 응답 전까지 빈칸이다.
- NPC 스프라이트와 맵/실제 사건 플레이는 미구현.
- **기능 테스트·Play Mode·EXE/APK 빌드 미실시.** 런타임 한국어 IME, 전송, 기록 복원, 스크롤, 테마 전환 동작 및 화면 외관은 미검증. 정적 확인을 기능 검증으로 간주하지 않는다.
