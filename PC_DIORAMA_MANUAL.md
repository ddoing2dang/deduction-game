# 마또 3테마 Windows PC 디오라마 제작 매뉴얼
2026-11-09 / 신규 시작용 / 실제 실행 및 품질 검증 전

## 1. 확정 범위와 현재 상태
- 지원 대상: Windows 64비트 PC 전용. Android, iOS, macOS 지원 및 배포는 제외한다.
- 배경 제작: Unity 6 Universal 3D(URP) + 편집 가능한 3D 모듈형 맵 + 테마별 재질/조명.
- 사용자의 목표는 고품질 쿼터뷰 디오라마 맵 3개(대저택, 크루즈, 음악방송 대기실)이며 Blender에서 모든 아트를 처음부터 모델링하는 방식을 요구하지 않는다.
- **현재 GitHub main은 2026-11-09에 클린슬레이트 초기화된 저장소다.** 기존 게임 규칙, NPC, 사건 시스템, 과거 UnityProject, 과거 맵 구조는 자동 복원하지 않는다. 원본 UI PNG 6개는 보존한다.
- 이번에 새로 작성한 3테마 레이아웃 스크립트는 **공간 시안용**이다. 8개의 편집 가능한 공간을 생성하지만, 그 구성은 새 게임의 확정 룰이 아니다.

## 2. 실제로 추가한 파일
- tools/unity/ThreeThemeMapDrafts.cs : Unity Editor 메뉴 2개를 제공하는 C# 맵 레이아웃 생성기.
- art/Environment/ThirdParty/KayKitFurnitureBits/Models/ : 신규 취득 CC0 Wavefront OBJ 3D 가구 모델 **12개**.
- art/Environment/ThirdParty/KayKitFurnitureBits/ASSET_MANIFEST.json : 각 모델의 출처 Git blob SHA 목록.
- art/Environment/ThirdParty/KayKitFurnitureBits/LICENSE.txt : 원본 KayKit CC0 라이선스.

### 소품 저작권
- 제작자: **Kay Lousberg / KayKit Furniture Bits 1.0**
- 출처: https://github.com/KayKit-Game-Assets/KayKit-Furniture-Bits-1.0
- 사용 라이선스: CC0 1.0. 상업용 사용/수정/재배포 가능.
- 원본 OBJ 메시와 UV 데이터는 유지했고 mtllib/usemtl 참조만 제거했다. 원본 컬러 텍스처는 포함하지 않았으므로 Unity 재질을 새로 지정해야 한다.
- 이 모델은 단순한 구조 배치용으로, **고품질 3D 최종 환경이 아님**. 외부 유료 에셋 구매가 필요하다면 별도 라이선스를 확인하고 결제·다운로드는 승인된 경로로 진행한다.

## 3. 새 Unity URP 프로젝트 만들기
1. Unity Hub → New project → **Universal 3D** 템플릿 → Unity 6.3 계열 → 프로젝트 이름 UnityProject.
2. 저장 경로를 GitHub 로컬 클론의 game/UnityProject 로 지정한다. 현재 원격 저장소에는 해당 프로젝트가 존재하지 않으며, 여기서 Unity Hub를 실행하거나 실제 프로젝트를 생성하지 않았다.
3. Unity 6.3의 URP는 17.3 계열이다. Unity 템플릿에서 기본으로 생성하는 렌더 파이프라인 에셋을 유지한다. URP 패키지만 수동 설치하는 것으로 URP 프로젝트가 완성되는 것은 아니다.
4. 새 Unity 프로젝트 내부 Assets/Editor 폴더에 **tools/unity/ThreeThemeMapDrafts.cs**를 복사한다.
5. Unity Editor 상단 메뉴 Tools → Matto PC → 1 Import CC0 OBJ props 를 눌러 저장소의 OBJ 파일을 Assets/MattoPC/ImportedModels 아래로 가져온다. **기존 파일 덮어쓰기는 하지 않는다.**
6. Tools → Matto PC → 2 Create three editable layout scenes 를 눌러 Assets/MattoPC/Scenes 에 세 시안 씬을 만든다. 같은 파일명이 이미 존재할 경우 기존 씬 보존을 위해 건너뛴다.
7. 이 스크립트가 만드는 큐브 벽과 바닥은 최종 아트가 아닌 배치용 블록아웃이며, Unity 실제 컴파일/생성 확인은 아직 이루어지지 않았다.

## 4. 테마별 공간 시안
| 시안 | 시각 방향 | 생성 구조 | 교체해야 할 고품질 아트 |
| --- | --- | --- | --- |
| Mansion | 블랙 / 버건디 / 황동, 클래식 저택 | 1층 4공간 + 2층 4공간 | 목재 몰딩, 계단, 벽지, 양탄자, 고급 로비 가구 |
| Cruise | 블루 / 아이보리 / 황동, 선박 내부 | 3데크 5공간 + 4데크 3공간 | 선체 내부 벽 곡률, 선실 문, 난간, 갑판 구조 |
| MusicBroadcast | 로즈 / 골드 / 크림, 방송 대기실 | 단층 U자형 8공간 | 화장대/거울, 방송 조명, 사물함, VIP 대기실 |

**중요:** 이전 프로젝트의 NPC 실명/방 번호/게임 판정/사건 생성 구조는 여기서 복구하거나 자동 적용하지 않았다. 현 단계의 공간 명칭은 편집용 자리표시자다.

## 5. Blender 없이 품질 끌어올리는 최선의 방식
- 먼저 한 공간(대저택 로비)의 조명과 고급 건축 재질을 완성해 3테마 공통 아트 품질 기준으로 삼는다.
- 문·기둥·바닥·벽·난간을 별도 모듈로 관리하고, UV가 정돈된 고품질 건축 에셋을 통일된 URP/Lit 머티리얼로 조합한다.
- 반복 가구는 프리팹화하고 포인트 소품만 테마별로 교체한다.
- 실제 조명은 광원 성능, 그림자 품질, 리플렉션 프로브, 포스트 프로세싱 후보를 PC 실측 후 정한다.
- 객실이나 갑판 밖에 있는 불필요한 모델은 카메라 가시성에 따라 숨기고, 각 층을 독립 계층으로 관리한다.
- 마지막으로 NPC, 증거 조사, 키보드 입력, 컷신과 조화를 확인한다. 이는 별도 게임 기능 구현 작업이다.

## 6. 콘텐츠 보존 및 작업 구분
- GitHub main의 사용자 원본 UI: art/UI/DialogueFrames/ThemeDialogue_v1 와 art/UI/CardFrames/InvitationPortrait_v2.
- **저장 완료:** CC0 가구 OBJ 12개, 라이선스/매니페스트, 편집기 스크립트, 본 매뉴얼.
- **미실행:** 새 Unity 프로젝트 설치/생성, OBJ 임포트, 레이아웃 씬 저장 버튼 실행, Unity Play Mode, 코드 컴파일, 기능 테스트, EXE 빌드/배포.
- **미완성:** 실제 고품질 디오라마 맵, 테마별 완성 조명, 이동 동선, NPC/사건 시스템.
- 별도 요청 전 기능 테스트나 재빌드는 하지 않는다.

## 7. 공식 참조
- Unity 6.3 URP: https://docs.unity.com/en-us/engine/6000.3/manual/render-pipelines/universal-render-pipeline/requirements
- Unity 패키지 관리: https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/managing-packages-manifest/upm-manifest-prj
- 추가 CC0 소재/에셋 공급처: https://polyhaven.com/
