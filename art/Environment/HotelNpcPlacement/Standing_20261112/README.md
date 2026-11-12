# 사건파일 No.464 — NPC StandingBillboard 적용

최신 main의 StandingBillboard 수정 코드를 fast-forward로 동기화한 후 실행 중인 Unity Editor 6000.3.26f1의 Edit Mode에서 기존 6명의 스프라이트 회전만 갱신·저장했습니다. 기존 배치 메뉴는 재실행하지 않았습니다.

- 카메라가 90° 수직이어도 스프라이트의 세계 위쪽 성분을 유지하고 최대 기울기를 45°로 제한합니다.
- 스프라이트 로컬 +X를 화면 오른쪽으로 정렬해 PNG 좌우 반전을 방지했습니다. PNG 자체와 8방향 연결은 변경하지 않았습니다.
- NPC 루트 좌표, 두 스프라이트의 로컬 발 기준점과 Scale 모두 유지했습니다.
- 직렬화 오브젝트 23,217개 유지, 추가/삭제 0개. 기존 스프라이트 자식 Transform 회전 12개와 Actor 기울기 설정 6개만 변경됐습니다. 맵·가구·Light·Camera·카메라 컨트롤러 블록은 수정 전과 같습니다.
- 전체 및 101·102·103·201·202·203 확대 Edit Mode 캡처를 확인했습니다. 수직 뷰에서 45° 경사 평면을 투영하므로 기존 바닥 평행 빌보드보다 화면상 세로 길이는 짧아집니다. 크기를 조작하지 않았습니다.
- Before_*는 이전 납품 커밋 f66ac84663af2d9056c0615b39e27ce2eab236ab의 실제 캡처 원본을 복사한 비교 자료입니다. After_*는 이번 Unity Edit Mode에서 새로 렌더한 캡처입니다.
- 기존 원본 씬의 이번 수정 전 백업은 BeforeScene.local-backup.unity로 로컬에 보존하며 GitHub 업로드에서 제외했습니다.
- Play Mode·기능 테스트·빌드는 실행하지 않았습니다. 런타임 동작은 UNVERIFIED입니다.

실제 씬: Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Final_Map_RoomSigns_WideAisle.unity
