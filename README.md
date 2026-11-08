# 추리 게임 — 새 프로젝트 출발점

이 저장소의 기존 게임 제작물은 **2026-11-09에 완전히 초기화**했습니다. 이후 기존 데이터를 복원하지 않고 **새 Windows PC 전용 3테마 지도 제작 스타터**를 추가했습니다.

## Windows PC 새 3테마 지도 제작 스타터 (추가 완료)

- **PC x64만 지원**: Android/iOS/macOS 제외.
- [PC 디오라마 제작 매뉴얼](PC_DIORAMA_MANUAL.md)
- [새 Unity Editor 3테마 레이아웃 생성 도구](tools/unity/ThreeThemeMapDrafts.cs)
- [CC0 3D 가구 OBJ 12개 및 라이선스](art/Environment/ThirdParty/KayKitFurnitureBits/)
- 이전 게임 데이터 복원 안 함. 실제 Unity 프로젝트, 컴파일/테스트, 고품질 완성 맵, EXE는 아직 없음.

## 클린슬레이트 초기화 직후 상태 (변경 기록)
- 구형 Unity 프로젝트, 자동 씬 생성기, 맵 파일·맵 구조 JSON, 3D 가구/환경 에셋, 옛 게임 웹 코드, 사건/대사/캐릭터 설정 JSON 및 이전 게임 기획 문서 **모두 제거**.
- 초기화 당시 보존한 게임 원본 자료는 **사용자가 직접 제공한 투명 UI 이미지 6장**입니다. 재가공하지 않은 원본 PNG로, 대화창 3장과 세로형 이름표/초대장 카드 프레임 3장입니다.
- 신규 Unity 프로젝트 `game/UnityProject`는 아직 **없습니다**. Unity Hub 혹은 Unity MCP로 처음부터 새로 생성해야 합니다.
- 이 저장소의 `main` 브랜치는 새 게임 규칙, 이전 NPC 설정, 이전 사건 데이터, 이전 맵 구조를 **자동 상속하지 않습니다**.

## 남은 원본 UI 이미지 (모두 GitHub)
테마별 대화창:
- `art/UI/DialogueFrames/ThemeDialogue_v1/Mansion.png` — 대저택
- `art/UI/DialogueFrames/ThemeDialogue_v1/Cruise.png` — 크루즈
- `art/UI/DialogueFrames/ThemeDialogue_v1/MusicBroadcast.png` — 음악방송 대기실

세로형 초대장/이름표:
- `art/UI/CardFrames/InvitationPortrait_v2/Mansion.png`
- `art/UI/CardFrames/InvitationPortrait_v2/Cruise.png`
- `art/UI/CardFrames/InvitationPortrait_v2/DressingRoom.png`

## 되돌리기용 전체 보관
삭제 직전 모든 GitHub 파일은 다음 백업 브랜치에 보존되어 있습니다.

[archive/pre-clean-slate-20261109](https://github.com/ddoing2dang/deduction-game/tree/archive/pre-clean-slate-20261109)

백업 브랜치의 내용은 `main`에서 **사용하지 않으며**, 사용자가 명시적으로 특정 자료를 되살리기를 요청할 때만 참고합니다.

## 새로운 프로젝트 만드는 방법 (Unity MCP/Unity Hub)
- GitHub `main` 브랜치를 기준으로 가져옵니다.
- `game/UnityProject`에 **새 Unity 6 URP PC 프로젝트**를 생성합니다.
- 모든 씬, 맵, NPC, 사건, 대사, 스크립트는 새로 설계하고 새로 만듭니다.
- 저장된 UI PNG는 직접 GitHub에서 받아 새 Unity 프로젝트의 `Assets`로 별도로 가져옵니다.
- 사용자의 별도 요청 전에는 Play Mode 기능 테스트나 빌드·배포를 실행하지 않습니다.
- GitHub 변경으로 Windows PC의 오래된 Unity 로컬 폴더를 삭제하거나 Unity Hub 등록을 제거할 수는 없습니다. 로컬 자료는 별도 관리가 필요합니다.
