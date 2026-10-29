# Visual Gold 최종 검토 — 2026-10-29

최종 판정: OK. 자동 이동 성공과 별도로 Blender/Unity의 저장된 실제 렌더 캡처를 직접 검토했다.

- CC0 Release ZIP SHA256 대조 완료. 실제 모델 21종, 배치 50개. 출처는 CC0_NOTICES.md.
- 중앙홀/서재/응접실/식당/주방/온실/보일러실/와인저장고/사용인실의 가구, 바닥, 벽 패널, 몰딩, 조명과 기능별 인상을 확인했다.
- 기존 방과 문/계단 연결, DoorID 및 protected point를 유지했다. Blender REF 94개와 Unity 지점 85개 불변.
- 중복 몰딩, 임시 사이드보드·침대·캐비닛·화분 등의 교체와 정리를 확인했다. 바닥 가구끼리 메시 교차 및 보호 동선 침범 0.
- FBX의 재질 이름 매핑, 물리용 원본 메시 유지, 시각 메시 bounds 대조 오류 0.
- Unity 9개 방의 9/19/32 줌, 일반 시점, 지하, 컷어웨이, 실제 행동/이벤트 사이드뷰를 확인했다.
- 벽에 붙은 창틀/몰딩/벽등이 함께 사라지고 복원된다. 가려졌던 문틀도 카메라가 이동하면 복원된다.
- 지하는 방 영역뿐 아니라 실제 서비스 계단의 측정된 3D bounds에 대한 시선 교차로 외부 지면을 절개한다. 두 계단을 오르내리는 배우와 발판이 연속해서 보이는 캡처를 확인했다. 원형 스포트라이트식 지면 구멍은 사용하지 않는다.
- 캐릭터 중심뿐 아니라 얼굴 주변을 표본 검사해 얇은 난간/문틀이 얼굴을 지속적으로 가리지 않도록 했다.
- 실제 CharacterActor 기본 연결 양방향 22회, 54 StandPoint와 9 ActionPoint clearance, 9 Animator 상태가 통과했다.
- 비밀통로 9↔6 양방향의 실제 배우 이동과 1P 비용, 패널 collider 개방, 공개 이벤트 표시, Restart를 확인했다.
- 기존 싱글플레이 회귀 2,461개 통과. 기존 게임용 캐릭터 파일 179개 SHA256 불변.
- 2D 연결은 실제 방 ID/카메라 pose/ortho size/aspect와 Entry/Stand/Action 화면 좌표를 전달한다. 신규 2D 일러스트를 제작한 것으로 보고하지 않는다.
- 452,884 tris / 778 mesh parts / 49 materials / Unity textures 112개, 모두 최대 2K. Android ASTC 6x6. 실제 PC/Android 기기 FPS 벤치마크는 수행하지 않았다.

검증 도중 기록 파일 잠금과 에디터 Debug 모드의 검사 타이밍 문제를 수정했다. 결과 파일은 atomic replacement로 저장하고, 공개 CharacterActor cue를 다시 발행한 직후 Animator를 일정 시간 평가해 에디터 FPS에 의존하지 않도록 했다. 전체 검증을 다시 실행했으며 현재 play-results.txt에는 FAILED가 없다. 에디터는 원래 Release 최적화 모드로 복원했다.

Blender 원본은 보존됐고, 변경은 별도 Mansion_VisualGold.blend에 있다. OS 입력/커서/화면 클릭/GUI 자동화를 사용하지 않았다. Blender는 MCP, Unity는 프로젝트 파일 및 Editor API로 작업했다.

원시 검증: Verification/play-results.txt
요약/기록 해시: final-verification.json
최종 검토판: Review/Unity_NineRooms_9.png, _19.png, _32.png, _Side.png
# Straight entrance enclosure — 2026-10-30

Latest saved model, before/after comparisons and full fresh Play validation are recorded in `STRAIGHT_ENTRANCE_REVIEW.md` and `straight-entrance-final-verification.json`. Thick side walls and a continuous entrance floor close the static entrance sightlines. The actual 4-to-5 crossing remains physically open behind dedicated cutaway infill units; it is not deleted or converted to a new connection. Earlier-stage records follow.

# Entrance repair and luxury refinement — 2026-10-29

Latest saved source and fresh Play Mode verification are documented in `LUXURY_REVIEW.md` and `luxury-final-verification.json`. The active 4-to-5 bridge is preserved, its tall visual rails become low stone edging, and the exterior ground no longer has basement-projection holes. Current totals: 550,557 tris / 809 render parts / 56 materials / 117 game textures. The final run passes 22 traversals, secret passage both ways and 2,461 single-player assertions after direct render review. Earlier-stage records follow.

# Decorative front door follow-up — 2026-10-29

Final saved Blender/FBX and full fresh Play Mode audit now include the decorative double front door. See `FRONT_DOOR_REVIEW.md` and `front-door-final-verification.json` for current totals, exact file hashes, direct render review and preservation checks. The latest environment is 471,948 tris / 779 render parts / 50 materials / 112 game textures. Existing 778 parts and gameplay layout remain unchanged.
