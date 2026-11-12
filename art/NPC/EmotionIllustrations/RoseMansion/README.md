# 사건파일 No.464 — 장미대저택 감정 일러스트 36장 (사용자 제공 자료)

> 2026-11-13 기준: 사용자가 `character_emotion_poses_36.zip`으로 제공한 **6인 × 감정 6종 = 총 36장**을 확인하고 기록한 참조 문서입니다. **이 README와 매니페스트가 GitHub에 올라가더라도 원본 PNG는 별도 바이너리 업로드가 완료될 때까지 GitHub에 없는 상태입니다.** 등록 여부는 `manifest.v1.json`의 `gitHubOriginalPngUploadStatus`를 확인하세요.

## 최종 캐릭터와 매핑

| NPC 내부 ID | 객실 | 머리색 | 공식 표시 이름 | 파일 수 |
|---|---|---|---|---|
| `MATTO` | 101 | 은발 | 마도은 | 6 |
| `BOKI` | 103 | 빨강 | 김보희 | 6 |
| `NAZ` | 202 | 갈색 | 나지수 | 6 |
| `JJIRO` | 102 | 초록 | 진소율 | 6 |
| `MARIHI` | 201 | 금발 | 마연희 | 6 |
| `MOONIRE` | 203 | 보라 | 문이현 | 6 |

## 제공된 여섯 감정 구분

| 순번 | 감정 파일 분류 |
|---|---|
| 01 | 당황/놀람 |
| 02 | 짜증/분노 |
| 03 | 피곤/우울 |
| 04 | 확신/깨달음 |
| 05 | 슬픔/체념 |
| 06 | 의문/궁금 |

- 원본: 36개 PNG (각각 1073×1466 픽셀, RGBA 투명 배경). 원본 데이터 변경 없이 보존.
- 원본 ZIP SHA-256: `3a35339d3921d344b7d322ef419dcf25a4d95b4ce556cabde131a0ae7e17f09a`.
- 이미지별 원본 파일명, 크기 및 SHA-256은 [`manifest.v1.json`](manifest.v1.json)에 기록.
- 원본을 저장소에 넣을 경우 제안 경로: `art/NPC/EmotionIllustrations/RoseMansion/{npcId}/{originalFileName}`.
- 원본의 캐릭터 옷/포즈/감정 표현을 변경하지 않는다. 현재는 **기획 참고 자료**이며 컷신 출현 시점과 NPC 대사 슬롯은 아직 지정되지 않았다.
- 204호 피해자 객실과 기존 6 NPC 객실 배치, 5일/10턴 조사 규칙, 범인 랜덤 선정, 6종 재연 컷신 및 18칸 기록도감 등 확정 룰은 유지한다.
- **기존** 기본 대화 초상화 폴더 `game/UnityProject/Assets/UI/Art/NPCPortraits/RoseMansion`과 NPC 맵 스프라이트 48장, GUID, Unity 코드/씬/프리팹은 건드리지 않는다.

## 저장 및 사용 상태

1. **현재 완료:** 사용자가 압축파일로 제공한 이미지 36장 인벤토리·파일명·해시·외형/캐릭터 이름 대응 확인.
2. **GitHub에 저장된 내용:** README 및 JSON 참조 인덱스.
3. **아직 미완료:** GitHub에 PNG 36장의 원본 바이너리 업로드. 로컬 Unity 임포트·실제 화면 확인/테스트/빌드 역시 수행하지 않음.
4. GitHub에 실제 원본 업로드가 완료되면 `gitHubOriginalPngUploadStatus`를 `uploaded`로 바꾸고 GitHub 트리의 36개 원본 해시를 확인한다.
