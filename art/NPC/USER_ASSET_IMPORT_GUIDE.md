# 사용자 제공 NPC 스프라이트 + 초대장 카드 프레임 활용 기준 (2026-11-08)

## 현재 첨부 확인
- `NPC_Sprites_6_Characters.zip` — SHA-256 `9815adcb90a5a16c4fa4d3ade36d60a66f1fc337554f8cb0d2aa4f27b3f4b3d1`. 캐릭터 색상 슬롯 6개, 개별 PNG 102개(맵 8방향·심문 표정 6종·정적 반응 3종/인물), 통합 시트 18장. 512×512 RGBA 개별 PNG. **이 ZIP/PNG는 현재 GitHub 저장소에 업로드하지 않았으며 채팅 첨부에서 확인했다.**
- `Portrait_Card_Frames_v2.zip` — SHA-256 `effa3a0a41bcbd43b13312edb0e8b223893966efa18a493030a12d4c64c40430`. `Mansion.png / Cruise.png / DressingRoom.png` 3장, 각각 590×860 RGBA. **현재 GitHub의 기존 `art/UI/CardFrames/InvitationPortrait_v2/` 3개 파일과 Git blob SHA가 완전히 일치함**. 중복 교체/재업로드 금지.
- 이미지 원본의 투명 배경과 색상·형태는 새로 그리거나 변형하지 않는 것을 기본으로 한다.

## 테마 매핑
| 게임 테마 ID | 카드 프레임 | 테마 |
|---|---|---|
| HOTEL | `art/UI/CardFrames/InvitationPortrait_v2/Mansion.png` | 대저택/호텔, 검정·빨강 |
| CRUISE | `art/UI/CardFrames/InvitationPortrait_v2/Cruise.png` | 크루즈, 파랑·흰색 |
| MUSIC_BROADCAST | `art/UI/CardFrames/InvitationPortrait_v2/DressingRoom.png` | 음악방송 대기실, 금색·분홍 |

카드 중앙 투명창 **아래**에 NPC 초상화/사진을 깔고, **위**에 카드 프레임 및 제목·설명 UI 텍스트를 배치한다. 카드 PNG 속 제목칸/설명칸에 문자 래스터화하지 않는다.

## NPC 스프라이트 6개 슬롯 — 사용자 직접 제공한 색상별 정체 + 신규 한국식 이름
| 머리색/소스 폴더 | 내부 npcId | 기존 이름(기술 참조만) | 신규 게임 표시 이름 |
|---|---|---|---|
| 은발 / `01_silver` | `MATTO` | 마또 | **마도아** |
| 금발 / `02_blonde` | `MARIHI` | 마리히 | **마리예** |
| 갈색 / `03_brown` | `NAZ` | 나즈 | **나주아** |
| 빨강 / `04_red` | `BOKI` | 보키 | **복희** |
| 연두 / `05_green` | `JJIRO` | 찌로 | **지로아** |
| 보라 / `06_purple` | `MOONIRE` | 문이레 | **문이린** |

새 이름은 화면 표시용이며 사용자 제공 색상-캐릭터 대응은 확정됨. `game/characters/npc_display_profiles.v1.json`이 단일 표시 이름 기준표이다. 기존 `npcId`, `roomId`, `lineId`, 714개 대사 원문을 삭제·변형하지 말고 UI에서 `npcId → displayName`으로 치환한다. 기술 폴더 기본명은 `MATTO_SeoEunbyeol` 형식을 사용한다.
- `MapIdle_N/NE/E/SE/S/SW/W/NW.png`: 8방향 **정적** 맵 배치. 걷기 애니메이션으로 오인하지 않는다.
- `DialoguePortrait_neutral/smile/surprise/worried/angry/serious.png`: 대사 `emotionTag`와 정확히 매핑해 심문에 사용한다.
- `ReactionGesture_explaining/startled/thoughtful.png`: 상황에 맞는 정적 반응. `animationTag`의 `react` 하나만 보고 포즈를 고정 배정하지 않는다.
- MapIdle/ReactionGesture 발 기준 피벗은 **(0.5, 0.0625)**, 카드 프레임 중앙 피벗은 **(0.5, 0.5)**. Portrait는 UI 하단 중앙 기준 정렬. PPU/표시 배율은 Unity의 실제 카메라와 UI를 확인한 뒤 결정.
- Unity에는 **개별 102장 또는 시트 18장만 선택해서** 사용해야 중복 임포트와 메모리 증가를 막는다. 로컬 가져오기용 묶음은 개별 102장 + 카드 3장으로 구성했다.
- 선택 입모양 오버레이와 Walk/Run은 이 첨부에 없다. 원본에 없는 애니메이션을 자동 완성됐다고 보고하지 않는다.

## 저장된 대사 480개와 연결 규칙
- 공통 `game/dialogue/curated/common_situations.v1.json` 156개 + 호텔·크루즈·방송 `game/dialogue/curated/{hotel,cruise,music_broadcast}.v1.json` 각각 108개.
- 선택된 `lineId` → 승인 조건 검증 → 사용자 확정 대응의 `npcId` → 신규 표시명 + `emotionTag`로 초상화 선택 → 텍스트 표시.
- NPC 지식/실제 증거/사건 사실/작가 승인 없이 사건 조건부 대사 사용 금지. 캐릭터 외형만으로 범인을 고정 암시하지 않는다.
- 다른 서사에서 쓰인 카드 UI는 과거 카드 손패/블러핑 게임 규칙을 복구하는 용도가 아니다.

## 실행 상태
**자산 내용 확인 + 색상·이름 확정 바인딩/새 표시명 문서 저장은 완료, NPC PNG 바이너리 GitHub 적재·Unity 임포트·씬 연결·기능 테스트·빌드·배포는 미실시.** 기존 Unity 게임파일 및 원본 아트는 수정하지 않았다.

상세 규격과 연결 설계는 `art/NPC/user_asset_bindings.v1.json`, `NPC_SPRITE_SPEC.md`, `game/dialogue/curated/README.md`를 참조.
