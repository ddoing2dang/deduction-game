# 현재 캐릭터 제작 입력

`active_sources.json`을 6명 보강 제작의 입력 레지스트리로 사용한다.
`sources/*.blend`는 보존 원본이며 `work/CHR_*/CHR_*_REFINEMENT.blend`만 편집한다.
기존 승인본, Game 작업본, 이전 refinement_work 파일은 보존한다.

Blender MCP에서 `open_active_character.py`를 실행할 때 `CHARACTER`를 명시한다.
파일 경로는 `source_registry.resolve()`로 결정한다. 작업 중 변경된 파일을
`configure()`로 다시 복사하거나 덮어쓰지 않는다. 검증은 `verify()`로 실행한다.

기존 prepare_characters.py의 재질 팔레트와 render_character_views.py의
진단 렌더를 이 작업본에 사용한다. prepare 단계는 이미 존재하는 작업본을
덮어쓰지 않으므로 필요한 경우 별도 출력 디렉터리를 지정한다.
OS 입력이나 BAT/외부 GUI 실행 대신 Blender MCP에서 제작 스크립트를 실행한다.

ART_GUIDE.md 디자인/눈색 LOCK 우선. 원본에 UV·재질·리그가 없으므로
현재 등록은 외형 승인 또는 게임 적용 완료를 뜻하지 않는다.
기존 캐릭터별 Game/export_game_fbx.py 및 Unity Setup은 이 초벌에 실행하지 않는다.
UV, 재질, Humanoid 리그, 스키닝, 표정, LOD, 시각 승인 및 게임 검증 전까지
Unity 슬롯·프리팹·FBX를 교체하지 않는다.
