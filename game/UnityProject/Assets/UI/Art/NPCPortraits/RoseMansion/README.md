# 장미대저택 기본 대화 초상화 (2026-11-12)

사용자가 대화에 제공한 **원본 PNG 6장**을 수정하거나 재압축하지 않고 저장했습니다.

| NPC ID | 공식 표시명 | 파일 | 업로드 시 파일명 |
|---|---|---|---|
| `MATTO` | 마도은 | [MATTO.png](MATTO.png) | 마도은 기본 대화 초상화.png |
| `MARIHI` | 마혜인 | [MARIHI.png](MARIHI.png) | 마혜인 기본 대화 초상화.png |
| `NAZ` | 나지수 | [NAZ.png](NAZ.png) | 나지수 기본 대화 초상화.png |
| `BOKI` | 김보희 | [BOKI.png](BOKI.png) | 김보희 기본 대화 초상화.png |
| `JJIRO` | 진초록 | [JJIRO.png](JJIRO.png) | 진초록 기본 대화 초상화.png |
| `MOONIRE` | 문이현 | [MOONIRE.png](MOONIRE.png) | 문이레 기본 대화 초상화.png |

**적용 범위**: 대저택(장미대저택/HOTEL) 테마의 기본 대화 초상화. 다른 테마의 초상화나 기존 글로벌 PNG는 보존합니다.

**참고**: `MOONIRE`의 업로드 원본 파일명은 구 표시명 '문이레'지만, 공식 UI 이름은 **문이현**입니다. NPC의 기술적 ID를 이름으로 교체하지 않습니다.

`portrait_manifest.json`이 캐릭터 ID와 Unity Assets 상대 경로를 정의합니다.

## 2026-11-12 연결 코드 추가

- `Assets/UI/Scripts/DialoguePanel.cs`의 `mansionPortraits`는 **MANSION/HOTEL 테마 전용**입니다. 기존 `speakerPortraits`는 CRUISE/MUSIC_BROADCAST용으로 유지합니다. 다른 테마의 초상화를 대저택 대체 이미지로 사용하지 않습니다.
- `Assets/UI/Editor/BindRoseMansionPortraits.cs`가 Unity **Edit Mode**에서 원본 6장의 Sprite import 상태와 `Assets/UI/Prefabs/DialoguePanel.prefab`의 전용 매핑을 연결하도록 구현됐습니다. Unity 프로젝트를 열었을 때 자동 연결을 시도하며, 필요한 경우 메뉴 `Mystery > UI > Bind Rose Mansion Six Portraits (Edit Mode)`를 사용합니다.
- 스프라이트 6개와 폴더·매니페스트 등의 Unity `.meta` 파일을 커밋해 GUID 재생성을 피합니다.
- `Assets/UI/Scripts/DialogueCinematicView.cs`는 NPC가 같아도 테마가 바뀌면 초상화를 다시 선택하도록 수정됐습니다.
- 기존 `DialogueSpeaker` 정수 ID(0~5)와 기존 사건/저장 호환성은 유지하고 대화창에 최신 표시명(마도은, 마혜인, 나지수, 김보희, 진초록, 문이현)을 씁니다.
- **확인 범위:** GitHub 코드와 Unity 에셋 매핑 파일 저장. 로컬 Unity Editor에서 자동 적용·대화창 실제 화면·빌드 동작은 아직 확인하지 않았습니다. 기능 테스트/Play Mode/빌드는 수행하지 않았습니다.

