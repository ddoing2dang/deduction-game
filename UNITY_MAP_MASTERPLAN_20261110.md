# 사건파일 No.464 — Unity·GitHub·MCP 연결 및 프리미엄 쿼터뷰 3D 맵 제작 기준

2026-11-10 · Windows PC x64 · **설치/제작 절차 및 편집 도구 저장 단계**. 실제 사용자의 실행 중인 Unity Editor나 로컬 파일에 접근한 것은 아님. Unity에서 씬 컴파일·실행·조명 베이크·빌드하지 않음.

## 절대 우선
1. 사용자 PC에서 열린 Unity 프로젝트를 **원본 무손상** 상태로 GitHub 작업 복제본에 연결. 원본을 삭제·초기화하거나 Unity 버전을 무단으로 업그레이드하지 않음.
2. 개발 기준 소스: `https://github.com/ddoing2dang/deduction-game`의 main. Unity 프로젝트는 복제 작업 폴더 `game/UnityProject/` 아래에 위치.
3. 구형 PvP/카드/모바일 게임 소스는 복원하지 않음. 보존 대상: 신규 탐정 게임의 확정 룰·3테마 8방·NPC 데이터·대사·기존 206 CC0 환경모델·UI PNG/캐릭터 원본.
4. **저폴리 모델/기본 큐브를 최종 아트라고 보고 금지**. 렌더 품질과 전체 구성의 수동 승인이 없으면 빈 씬으로 남기고 필요한 자산을 보고.
5. 오류/검증 실패가 동일 원인으로 2번 반복되면 즉시 중단 → 메시지·시도 내역·마지막 성공 커밋·준비물·재개 명령을 작업 로그에 기록. 우회 실패 은폐·무한 반복 금지.
6. 사용자 별도 지시 없이는 Unity Play Mode, 기능 테스트, EXE 빌드/배포 금지. 디자인/스크립트 정적 검토와 실제 Unity 검증을 반드시 구분.

## A. 현재 열려 있는 Unity 프로젝트를 GitHub와 연결

### 필요 준비물
- 현재 열려 있는 Unity Editor의 실제 프로젝트 루트 폴더(그 안에 `Assets/`, `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`가 있어야 함).
- Git for Windows 설치 및 GitHub 인증 준비. GitHub Desktop으로 이후 커밋/푸시해도 무방.
- Windows PowerShell. 로컬 작업을 위한 충분한 저장공간. Unity 프로젝트 원본을 **새 저장소 복제본으로 복사**하므로 크기 확인 필요.
- Unity의 현재 씬 저장. 복사 적용 단계는 Unity Editor **닫은 뒤** 실행.

### 권장 연결: 안전 복제(기존 Unity 폴더에 git init + 강제 병합하지 않음)
1. GitHub에서 `tools/windows/Connect-Case464Unity.ps1` 내려받기. 스크립트 내용을 검토.
2. Unity가 열린 상태에서 **읽기 전용 준비 검사**:
   `powershell -File .\Connect-Case464Unity.ps1 -UnityProjectPath "D:\\경로\\내Unity프로젝트" -WorkspacePath "D:\\Case464-GitHub"`
3. 확인 후 Unity에서 씬/에셋 저장, **현재 Editor 닫기**.
4. 사용자 명시 실행: `powershell -File .\Connect-Case464Unity.ps1 -UnityProjectPath "D:\\경로\\내Unity프로젝트" -WorkspacePath "D:\\Case464-GitHub" -Apply`
5. 이 스크립트는 원본의 `Assets/`, `Packages/`, `ProjectSettings/`를 복제 저장소 `game/UnityProject/`로 복사. 캐시 `Library/`, `Temp/`, `Logs/`, `obj/` 등은 복사하지 않음. 원본은 변경하지 않고 자동 commit/push 없음.
6. CC0 원본 모델도 함께 준비하려면 **최초 적용 명령에만** `-Apply -StageArt`를 같이 사용. GLB를 쓰기 전 glTFast가 필요하며 복사 자체는 품질 승인 아님. 이미 연결한 프로젝트에 `-Apply`를 반복하지 말 것.
7. Unity Hub에서 **복사본** `D:\\Case464-GitHub\\game\\UnityProject`를 프로젝트로 열기. 프로젝트 이름과 경로를 확인해 원본/복제본을 혼동하지 않기.
8. Unity `Edit > Project Settings > Editor > Asset Serialization`을 **Force Text**로, `Version Control`을 **Visible Meta Files**로 설정. `Assets/*.meta`를 누락하지 말고 커밋. `Library`, `Temp`, `Logs`는 .gitignore대로 제외.
9. GitHub Desktop 또는 Git CLI로 작업된 파일 목록을 **리뷰**한 뒤 승인된 변경만 stage/commit/push. 일단 로컬 복사만으로 **GitHub 원격 반영 완료라고 주장하지 않음**.
10. 스크립트가 기존 작업 트리/경로 충돌/설치 누락을 발견하면 작업 중지. 원본 Unity에 강제 git init/풀/리셋 금지.

### GitHub 수동 명령(복사 완료 뒤)
```powershell
git -C "D:\Case464-GitHub" remote -v
git -C "D:\Case464-GitHub" status --short
git -C "D:\Case464-GitHub" diff --stat
# 확인 후, 의도한 경로만 선택적으로 stage
git -C "D:\Case464-GitHub" add game/UnityProject/Assets game/UnityProject/Packages game/UnityProject/ProjectSettings
git -C "D:\Case464-GitHub" status --short
# 변경 검토를 마친 뒤에만 commit과 push
```
이미 `.git` 아래에 프로젝트가 있고 origin이 동일 저장소라면 새 클론 대신 원래 브랜치/경로/상태를 우선 검토.

## B. Unity MCP 연결(실제 로컬 편집은 연결 후 가능)

공식 오픈소스: https://github.com/CoplayDev/unity-mcp  · 설치: https://coplaydev.github.io/unity-mcp/getting-started/install

1. 복제한 Unity 프로젝트를 Editor에서 엶.
2. `Window > Package Manager > + > Add package from git URL`:
   `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
3. 필요한 Git, Python 3.10+, `uv`는 MCP for Unity 설치 마법사의 지시에 따라 준비.
4. `Window > MCP for Unity`에서 **현재 Unity 프로젝트와 에디터 인스턴스**를 확인. 로컬 MCP 서버 활성화.
5. 사용 중인 MCP 클라이언트(Codex / VS Code / Cursor 등)를 선택하고 `Configure Selected`. 지원되는 클라이언트에 한해 자동 등록, 서버에 Connected가 보여야 함.
6. 자동 연결 불가 시 로컬 HTTP 엔드포인트 예: `http://localhost:8080/mcp`. 서버 포트·전송방식을 실제 Unity MCP 창에서 확인한 뒤 클라이언트에 연결. 원격 인터넷에 로컬 MCP 포트를 노출하지 말 것.
7. 클라이언트가 에디터/활성 씬/패키지/에셋 목록을 **읽을 수 있는 상태**인지 먼저 확인. 다른 Unity 창 선택 또는 연결 지연 시 편집 금지.
8. ChatGPT 이 대화에 Unity MCP 도구가 자동으로 생긴다고 가정 금지. AI가 실제로 Unity Editor를 조작하려면 **해당 MCP 클라이언트가 연결되고 Unity 에디터 도구를 제공해야** 함.
9. 맵 제작에 필요한 tool group(scene, asset, material, object, prefab, lighting, screenshot 등)을 활성화. 빌드·테스트 도구는 사용자 허가 없이 실행하지 말 것.

**주의**: 연결 상태 표시는 로컬 클라이언트에서 확인해야 한다. GitHub MCP는 **코드/문서/에셋 파일**을 다룰 수 있지만, 그 사실만으로 로컬 Unity 메모리의 씬을 조작하지 못한다.

### ChatGPT 자체 연결을 원하는 경우
- 일반 ChatGPT 웹 대화가 `http://localhost:8080/mcp` 같은 로컬 주소를 곧바로 호출하는 방식은 지원되지 않는다. 현재 ChatGPT는 승인된 원격 MCP 서버 또는 해당 제품에서 제공하는 **Secure MCP Tunnel** 방식으로 사설 MCP에 접근할 수 있다.
- ChatGPT 맞춤형 MCP 앱의 쓰기/수정 기능은 제품/요금제/권한에 따라 지원 범위가 다르다. **현재 대화에는 Unity Editor MCP 도구가 연결되어 있지 않으므로 Unity 장면을 직접 고칠 수 없다.** 지원되지 않는 요금제나 환경에서 억지 우회하지 말 것.
- 실용적인 1차 제작 경로는 **로컬 Unity Editor + CoplayDev Unity MCP + Codex/VS Code 등의 지원 MCP 클라이언트**이고, 지금 ChatGPT GitHub 연결로 코드를 버전 관리한다.
- OpenAI 공식 연결 가이드: https://help.openai.com/en/articles/12584461-connecting-custom-mcp-servers-in-chatgpt
- 로컬/사설 MCP 보안: https://developers.openai.com/api/docs/guides/tools-connectors-mcp
- 로컬 Unity MCP 서버를 인증 없이 외부에 개방하거나 무분별한 터널로 퍼블릭 인터넷에 노출하지 않는다.

## C. Unity 내 제작 패키지/공식 소스

| 제작 작업 | 추천 도구 | 비고 |
|---|---|---|
| 건축 벽체/계단/문틀 UV 모델링 | Unity ProBuilder (`com.unity.probuilder`) | 고품질 마감 전 건축 비례/충돌/동선 편집 |
| 고급 GLB 임포트 | Unity glTFast (`com.unity.cloud.gltfast`) | 설치 후 에셋 임포트 색/노멀/재질 검수 |
| 곡면·고급 건축 트림/소품 | Blender | 베벨/Weighted Normal/UV/PBR 재질 작업 후 FBX 또는 검증된 glTF |
| 고품질 텍스처 | Poly Haven, ambientCG 등 실제 사용권 확인 | 주표면 PBR 베이스컬러/노멀/러프니스/메탈/AO, 필요 시 높이 |
| 마감/사용 흔적 | URP Decal Projector, Shader Graph | 벽 마모, 카펫 테두리, 작은 흔적, 발자국 등의 사건별 비노출 단서 |
| 조명 | Baked GI, Reflection Probe, URP Adaptive Probe Volumes | 조명 데이터 생성은 사용자 테스트/베이크 승인 시점 |
| 쿼터뷰 연출 | Orthographic Camera, Cinemachine 선택 | 고정/추적 카메라·대화 시 컷 전환은 사건 UI와 충돌 금지 |
| 화면 마감 | URP Volume: 톤매핑/화이트밸런스/적정 Bloom/AO | 과도한 블러/필터로 디테일 숨기지 않음 |

Unity ProBuilder: https://docs.unity3d.com/kr/current/Manual/com.unity.probuilder.html
Unity glTFast: https://github.com/Unity-Technologies/com.unity.cloud.gltfast
Unity URP 데칼: https://docs.unity3d.com/kr/current/Manual/urp/renderer-feature-decal-landing.html
Unity 조명: https://docs.unity3d.com/kr/6000.0/Manual/urp/probevolumes-use.html
Poly Haven CC0: https://polyhaven.com/license

**외부 모델의 라이선스는 다운로드별로 확인.** 현재 저장된 206 모델의 CC0는 '재사용 가능'을 의미할 뿐 프리미엄 품질을 보장하지 않는다. 새 유료·제한 라이선스 자산의 무단 GitHub 업로드 금지.

## D. 3D 쿼터뷰 아트 룩

**최우선 참고 규격:** [컷어웨이 디오라마 스타일 가이드](art/Environment/CUTAWAY_DIORAMA_MAP_REFERENCE_20261110.md) · [맵 카메라/화면 상태 JSON](game/maps/cutaway_camera_presentation.v1.json). 각 층을 나란히 보여주는 화면 연출도 허용하되 실제 동선·방 개수는 바꾸지 않는다. 원본 TV 스틸을 게임에 복제하지 않는다.


### 필수 카메라
- **2026-11-10 사용자 참고 이미지 확정 방향:** 전체 맵은 지붕 없는 고각 컷어웨이 3D 디오라마. 검은 외부 공간, 실제 두께의 뒷벽·측벽·문틀, 카메라 앞쪽의 가림 벽만 선택적으로 숨긴다. Orthographic 카메라를 기본으로 **수평면 기준 60° 하향 고도각**에서 시작해 55°/65°를 비교한다. 개별 방의 가독성을 우선해 각도는 조정할 수 있다. 전체맵·층별·방별·물체 클로즈업의 장면 전환은 `art/Environment/CUTAWAY_DIORAMA_MAP_REFERENCE_20261110.md`가 기준이다.
- 건물 지붕/높은 벽은 카메라 방향에 따라 cutaway, 유리화, 계층 숨김 등으로 시야 확보. 범인의 정답을 가리는 게 아니라 플레이어 조사의 가독성을 개선.
- 방 크기보다 **방 정체성, 인테리어 구조, 소재의 물성, 가구 사이의 관계, 소품의 생활감**을 우선.

### 방당 미술 7층
1. 구조: 실제 두께를 가진 벽·문·복도·천장·계단·기둥, 신뢰 가능한 건축 비례.
2. 대형 형태: 24개 서로 다른 대표 오브젝트 + 방 고유 배치/실루엣.
3. 고유한 재료: 벽/바닥/금속/천/목재의 PBR 속성, 필요 시 2K/4K 원천 텍스처.
4. 중형 소품: 책장 내부, 침구 접힘, 패널, 트림, 문 손잡이, 전등 반사, 커튼 두께.
5. 미세 디테일: 이음새, 모서리 베벨, 긁힘/얼룩/이용 흔적, 고유 물건과 시선 유도.
6. 조명: 테마별 명암 관계, 창문 역광·실내 실용광·미세 바운스, 그림자 접촉.
7. 플레이 역할: 사건마다 연결되는 조사 가능 지점, 문 출입 경로, NPC 방 소지품. **흉기 실물은 NPC 개인실에만** (규칙상 6방×3개/테마).

### 3개 테마
- **HOTEL**: 중앙 계단 로비의 대칭·아치·월넛·황동·대리석/카펫의 재질 차, 각 객실의 가구 차별화. 고급 호텔 객실 문틀·천장 코브몰딩·벽 패널이 저폴리 평면처럼 보이지 않아야 함.
- **CRUISE**: 길고 좁은 좌현/우현 동선, 3데크 5방+4데크 3방, 앞뒤 계단 2개, 실제 선체 창/난간/곡면 벽·금속 트림. 403호 현장은 앞계단 근처.
- **MUSIC_BROADCAST**: 단층 U자 복도, 공동 대기홀 뒤 VIP-01, 조명 거울·의상 레일·스피커·케이블·방송 장비·화려한 라운지. 개별 방송 장비를 단순 상자로 대체하지 않음.
- **테마별 8개 조사 공간 고정**, 복도/계단/통로는 이동용 비조사 공간. 실제 방 배정은 `game/maps/theme_room_layouts.v1.json`을 사용하고, 아트 리스트 `art/Environment/room_profiles_24.v1.json`의 H00/C00/M00 등은 별도 디자인 ID로만 사용한다.

### 시각 승인/재작업 기준(프로젝트 목표치)
- 대표 오브젝트 24개는 디자인 ID별로 **중복하지 않음**.
- 중·근거리에서 모서리가 깨지는 대형 실루엣, 노멀 반전, 재질 누락, 텍스처 깨짐, 부동 소품, 동일 구조의 반복은 **승인 불가**.
- 미세 소품을 마구 늘려 성능이 떨어지는 대신, 가까운 것에는 세부 지오메트리/데칼, 먼 곳에는 단순화·메시 결합/LOD 사용.
- 각 방의 개성은 **배치·재질·조명·대표 가구**가 모두 달라야 함. 같은 기본 프리팹 색만 바꾸기 금지.
- 게임 플레이 시 증거 핫스팟이 시각적으로 식별되어야 하고, 중요한 물체가 UI/인물에 가려지지 않아야 함.
- 고품질 마감은 스크린샷 기준의 실제 육안 검수로만 확정. 자동 검사만 통과해서는 완성품이라 할 수 없음.

## E. 준비된 Unity 에디터 도구로 시작하기

소스: `tools/unity/Case464MapWorkshop.cs`. Windows 복제 스크립트 적용 시 복제된 프로젝트의 `Assets/Editor`에 설치.

1. 디자인 원본 `art/Environment/room_profiles_24.v1.json`을 복제 프로젝트 `Assets/Case464/Data/room_profiles_24.v1.json`에 복사.
2. Unity Project 창에서 빈 폴더 `Assets/Case464/MapBlueprints`, `Assets/Case464/Scenes/Workshop`을 생성.
3. `Case464 > Map Workshop > Create Blueprint Assets From Plan` 메뉴로 24개 **빈 아트 블루프린트**(기존 자산 덮어쓰기 없이) 생성.
4. Unity Inspector에서 첫 `HOTEL_H00` 블루프린트에 실제 게임 `gameplayRoomId=LOBBY` 지정, 프리미엄 건축 prefab/대표 prefab/디테일 prefab 배치·위치·스케일 설정.
5. 레퍼런스/아트 디렉터 확인 후 `artDirectorApproved`와 `artApprovalReference` 채우기. 자동 승인 불가.
6. `Case464 > Map Workshop > Open`에서 해당 블루프린트 선택, `Check selected blueprint`으로 **필수 자산 연결·빈 재질·고유 소재 수·공간 종류 등 정적 검사**.
7. 모든 프리플라이트가 충족되면 `Create review scene additively`로 `Assets/Case464/Scenes/Workshop/HOTEL_H00_Review.unity` 생성. 현재 열려 있는 다른 씬은 그대로 유지, **기존 리뷰 씬 덮어쓰기 금지**.
8. 생성 씬은 **프리팹 조립 + 기본 검토 카메라만** 포함. 조명 베이크/환경 최종 재질/증거 인터랙션은 후속 제작 및 실제 시각 검수 대상. **이 단계만으로 고품질 맵 완성 주장 금지**.
9. 호텔 로비 1개가 최종 품질 승인을 받기 전 나머지 23방을 무분별하게 일괄 배치하지 말 것.

## F. 우선 준비물 / 실패 시 중단 기준

| 단계 | 연결 또는 준비물이 없으면 |
|---|---|
| 현재 Unity 프로젝트 실제 경로 | 자동 추측·삭제 금지, 사용자가 경로 확인 후 재개 |
| GitHub 연결·Git 인증 | 강제 push 금지, 로컬 읽기/상태 확인부터 |
| MCP Unity Editor 인스턴스 | 에디터가 연결됐다는 주장을 중지, 설치 창/클라이언트 설정 안내 |
| URP ProBuilder glTFast | 누락 시 해당 기능 정지, 패키지 설치·색상/셰이더 호환 확인 |
| 고품질 건축 소스·PBR 재질 | 저폴리/큐브로 대체 완성 금지, 필요한 모델·재질 목록 제시 |
| 승인되지 않은 NPC 증언·사건 정보 | 가짜 물증/범인 암시 생성 금지 |
| 동일 오류 2회 | 멈춤, 마지막 성공 SHA·원문 로그·선행조건·재개 지점 기록 |

**최종 산출물 목표**: 최초 고품질 HOTEL 로비 1개 승인 → 호텔 8방 완성 → 크루즈 8방 → 음악방송 8방 → 사건 데이터/심문 연출과 연결. 실제 Unity 연결과 실행 상태는 사용자 PC에서 확인 후 단계별로 기록한다.
