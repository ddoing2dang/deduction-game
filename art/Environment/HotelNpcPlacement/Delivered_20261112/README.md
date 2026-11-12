# 사건파일 No.464 — 원본 NPC 배치 저장

Unity 6000.3.26f1의 실행 중인 Editor와 파일 요청/응답으로 연결해 사용자가 지정한 기존 Apply 메뉴를 실행했습니다. GUI 마우스 제어, Play Mode, 기능 테스트, 빌드는 사용하지 않았습니다.

씬: Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Final_Map_RoomSigns_WideAisle.unity

| 객실 / NPC | 저장된 월드 좌표 X, Y, Z |
|---|---|
| 101 마도은 | 5.071019, 0.027100, 4.151777 |
| 201 마혜인 | 2.352548, 0.027100, -11.252201 |
| 202 나지수 | -2.010304, 0.027100, -8.232847 |
| 103 김보희 | -9.559028, 0.027100, 18.781826 |
| 102 진초록 | -0.387849, 0.027100, 9.610646 |
| 203 문이현 | -12.807827, 0.027100, 1.433303 |

48개 원본 PNG는 ZIP과 SHA-256이 같습니다. 48개 PNG .meta는 Unity가 현재 버전의 임포터 형식으로 갱신했으며 원본 GUID를 모두 보존했습니다. 각 NPC에 8방향 원본을 연결했습니다. 기존 roomId와 DialogueSpeaker 숫자 ID를 유지합니다.

실제 확인: 저장 후 씬 재열기, NPC 6개와 48개 Sprite 참조, 전체/층별/6개 객실 확대의 Edit Mode 렌더 캡처. 러그·가구의 바닥 투영 범위를 피해 NPC 위치만 조정했습니다.

기존 23,124개 직렬화 오브젝트 삭제 없음. 기존 Transform·메시·재질·Light·Camera 블록은 유지했습니다. 카메라 컨트롤러의 마지막 선택 roomId와 SceneRoots 추가만 달라졌습니다. 복도·방·가구 배치는 보존했습니다.

기존 DialoguePanel 프리팹을 숨김 시작 상태로 씬에 연결하고 리뷰 카메라와 입력 EventSystem을 연결했습니다. 클릭 프록시→HotelNpcActor→기존 DialogueCinematicView 경로는 소스/참조로 연결되어 있습니다. 실제 클릭·대화 실행은 Play Mode 금지에 따라 UNVERIFIED입니다. 기존 사건/대사 데이터는 수정하지 않았습니다.

백업: Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Final_Map_RoomSigns_WideAisle_Before_NPC_Placement.unity (로컬 보존; 최신 결과 커밋에 중복 업로드하지 않음).

검토 이미지: NPC_Full.png, NPC_FirstFloor.png, NPC_SecondFloor.png 및 객실 101~203 개별 이미지. 방 확대에서는 기존 실내 조명 설정에 따라 일부 객실이 어둡습니다. 조명은 이번 작업에서 변경하지 않았습니다.
