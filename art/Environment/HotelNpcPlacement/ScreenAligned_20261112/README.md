# 사건파일 No.464 — NPC 화면 정렬형 2.5D 표시

원인: 세계 수직을 유지하려던 45도 경사 제한이 수직 탑다운 카메라에서 스프라이트 세로 길이를 약 0.707배로 압축했습니다. PNG 전체 크기 기준 배율도 불투명 캐릭터 크기를 정확히 반영하지 않았습니다.

수정: SpriteRenderer의 전역 회전을 리뷰 카메라 회전과 일치시키고 로컬 +X/+Y를 화면 오른쪽/위쪽으로 정렬했습니다. 방향별 원본 PNG의 알파 >8 영역 및 발 아래쪽 8행을 측정해 불투명 몸체 높이 1.65 Unity 단위로 균등 배율을 설정하고 실제 발 픽셀을 기존 월드 발 기준점에 등록합니다. 원본 PNG·Pivot·PPU·.meta는 변경하지 않습니다.

원본 48장 모두 512x512, PPU=256, Pivot=(256,32) pixels였습니다. 정면 불투명 높이는 459~460px이며 가로 폭은 175~246px입니다. 좌우 배율을 따로 조절하지 않아 원본 몸체 비율을 유지합니다.

NPC 전용 URP Unlit 투명 합성 재질은 ZWrite Off/ZTest Always를 사용합니다. 몸체가 러그나 가구의 깊이 버퍼에 잘리지 않도록 보장하는 2.5D 표시 방식이며, 완전한 3D 인물의 앞뒤 가림을 시뮬레이션하는 방식은 아닙니다. 원본 발 기준점과 월드 좌표는 그대로입니다. 카메라를 향한 화면 정렬은 LateUpdate와 해당 카메라 렌더 직전에 갱신됩니다.

실제 Unity Edit Mode에서 Before 9장과 After 9장을 같은 전체·층별·객실 보기로 렌더했습니다. 모든 객실에서 머리~발끝을 확인했고 화면 상향/우향 정렬 및 원본 비율을 수치로 대조했습니다. 다른 객실 보기의 NPC는 의도대로 숨겨지고 해당 객실 NPC만 표시됩니다.

NPC_BeforeAfter_6.png는 실제 캡처의 동일 발 주변을 최근접 2배 확대한 비교표입니다. B=수정 전, A=수정 후. 행별 101/102, 103/201, 202/203입니다. 원본 전체 캡처도 모두 보존했습니다.

기존 자동화 도구 tools/unity/refresh_editor_assets.py와 Assets/Editor/Case464AssetRefresh.cs로 코드 임포트·컴파일·Domain Reload·참조 확인·최종 저장을 완료했습니다. 사용자 수동 Refresh/재시작은 요청하지 않았습니다. 중간 미저장 보호가 한 번 작동했으며 Editor의 지연 Undo/레이아웃 기록을 확정·저장한 후 깨끗한 상태를 확인하고 진행했습니다. 최종 결과는 Complete/saved=true입니다.

원본 씬 직렬화 오브젝트 23,217개 유지. NPC 6명 삭제/재생성/중복/객실 이동 없음. 맵·가구·조명·카메라·대화 연결·사건 데이터 보존. 숨겨진 기존 DialoguePanel의 레이아웃 값은 Unity 저장 과정에서 재계산될 수 있습니다.

재사용 Editor 메뉴: Case464 > Hotel NPC > Repair Screen-Aligned Presentation (Existing 6 Only). 새 NPC 배치 메뉴를 다시 실행하지 않습니다.

저장 씬: Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Final_Map_RoomSigns_WideAisle.unity

Play Mode·기능 테스트·게임 빌드는 실행하지 않았습니다. 런타임 상호작용과 실제 실행 중 모드 전환은 미검증입니다.
