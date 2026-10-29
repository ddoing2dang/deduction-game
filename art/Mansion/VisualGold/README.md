# Mansion Visual Gold

2026-10-29. Pass02의 실제 9개 방과 이동망 위에 적용하는 CC0 환경 패스.

## 결과 파일

- 제작 원본: `Mansion_VisualGold.blend` (상대 경로 `AuthoringTextures/` 포함)
- 게임 에셋: `game/UnityProject/Assets/_Game/Art/MansionVisualGold/Mansion_VisualGold.fbx`
- 적용 씬: `game/UnityProject/Assets/_Game/Scenes/Game.unity`
- 라이선스/Release SHA256: `CC0_NOTICES.md`
- 원본 백업: 로컬 `Backup/Mansion_Environment_Pass02.blend`, `Backup/Game_BeforeVisualGold.unity`
- 이전 Pass02 파일은 덮어쓰지 않았다. `export-report.json`에 원본 해시 대조 결과가 있다.

## 방별 구성

| 방 | Visual Gold 구성 |
|---|---|
| 1 중앙홀 | 대리석, 짙은 러그, 조각 콘솔·흉상·오일 램프, 샹들리에, 갤러리 그림, 목재 패널과 황동 몰딩 |
| 2 서재 | 고딕 목재 캐비닛, 조각 책상, 암체어, 독서등, 무광 짙은 파케 바닥 |
| 3 응접실 | 목재 소파, 작은 조각 테이블·티 세트, 석재 벽난로, 금장 그림과 벽등 |
| 4 식당 | 긴 연회 테이블, 실측해서 배치한 의자, 조각 사이드보드, 샹들리에 |
| 5 주방 | 석재 작업대, 마감한 수납장 전면, 황동 손잡이, 형태를 정리한 후드, 선반 도자기 |
| 6 온실 | 실제 CC0 잎/화분, 포팅 선반, 기존 유리/멀리언 유지; 중첩 화분 제거 |
| 7 보일러실 | 기존 보일러·배관·밸브 유지, 석재/흑철/황동 표면, 서비스 랜턴 |
| 8 와인저장고 | 벽면 와인 랙과 병, 실제 CC0 오크 배럴, 석재/목재 표면, 랜턴; 입구 앞 소품 없음 |
| 9 사용인실 | 실제 목재 침대, 협탁·오일 램프, 작은 서랍장, 소박한 목재 바닥 |

외곽은 실제 건물 바닥·계단·통로 투영 범위를 제외한 지면에 석재 진입로, 경계 울타리, 문주·랜턴, 벤치, 흉상, 식재대를 배치한다. 새 플레이 방/이동 연결은 없다. 창은 벽 표면의 시각 요소이며 새로운 출입구가 아니다.

## 배치 기준

- Unity Y-up / Blender Z-up 변환을 명시했다.
- 자산 실제 전체 bounds를 측정하고 바닥 최저점, 기존 가구 슬롯, 실제 벽면을 기준으로 배치했다.
- 공급된 자동 적용 스크립트는 검토만 했다. 좌표 축/원점 문제와 검증 부재 때문에 실행하지 않았다.
- 보호 거리: StandPoint 0.62m, Entry/ActionPoint 0.74m, 실제 LOCK 경로 표본 0.63m.
- 가구끼리 실제 메시 BVH 교차도 검사했다. 식탁을 북쪽으로 0.4m 옮겨 의자 관통을 제거했다.
- 조명·테이블 위 장식은 표면 접촉을 허용한다. 보행 공간의 바닥 가구끼리 교차는 허용하지 않는다.
- 기존 골조/문/계단/게임용 collision mesh는 그대로 쓰며 시각 메시만 교체했다.

## 카메라 / 일러스트 연결

- 일반 플레이: 기존 42도 반 탑뷰와 9 / 19 / 32 줌을 유지한다.
- Gold에서는 카메라의 높이·전후 오프셋과 시선 각도를 맞춰 실제 방을 화면 중심에 둔다.
- 공개 조사/의심/협조 등 행동 및 공개 이벤트: 실제 배우/방을 대상으로 3D 사이드뷰 전환 후 원래 시점 복귀.
- 벽과 벽에 붙은 몰딩/창틀/벽등이 함께 컷어웨이된다. 배우를 덮는 높은 층 바닥, 문틀 및 가구도 가려진다.
- 지하에서는 시선 투영과 실제 두 서비스 계단의 측정된 bounds에 맞춰 외부 지면에 추가 절개를 적용한다. 사이드뷰에서는 다른 방/무관한 연결/외부 지형/다른 방의 배우를 가리고 복귀 시 복원한다. 충돌과 이동 연결은 유지한다.
- `MansionGoldCinematicDirector.IllustrationReady`는 같은 방 ID/카메라 위치/회전/orthographic size/aspect를 2D 연출에 전달한다.
- `Verification/SideRoom_XX_Composition.json`: 실제 Entry/Stand/Action 좌표와 화면 투영값. 후속 일러스트의 공간 구도 기준이다.
- 신규 2D 일러스트는 이번 패스에서 제작하지 않았다. 기존 Catalog의 일러스트 슬롯을 연결하는 인터페이스를 보존한다.

## 에셋 예산과 파이프라인

- 최종 배치 452,884 tris / 778 render mesh parts / 49 materials.
- Unity 텍스처 112개, 모두 최대 2048. Android ASTC 6x6, mipmap 적용. PC 압축도 활성화.
- 원본 모델별 과한 디테일만 제한하고, 실루엣과 UV/재질은 유지했다. 전체 저택 일괄 Decimate를 하지 않았다.
- 모서리/몰딩/외부 창틀은 실제 벽 단위로 합쳐 컷어웨이 단위를 유지했다.
- FBX 재질 순서는 이름으로 대조한다. Unity import 순서를 Blender 슬롯 순서로 추정하지 않는다.
- 맵을 기기에서 실행한 FPS 측정값은 없다. 예산/압축 및 실제 Editor Play 검증 결과를 기록한다.

## 검증 자료

- `blender-final-audit.json`: 94개 REF 불변, 보호 지점/경로 침범과 바닥 가구 교차 0.
- `character-preservation-results.json`: 기존 게임용 캐릭터 파일 179개 SHA256 불변.
- `unity-bounds-errors.txt`, `Verification/runtime-spatial-errors.txt`: 좌표 대조 오류 0.
- `Verification/play-results.txt`: 실제 CharacterActor 양방향 이동 22회, 54 StandPoint, 9 ActionPoint, 비밀통로 양방향, 이벤트/Restart/기존 싱글플레이 회귀.
- `Review/Unity_NineRooms_9.png`, `_19.png`, `_32.png`, `_Side.png`: 실제 Camera.Render 결과를 축소 배열한 검토판. 방 번호와 구분선만 추가하며 장면 재터칭은 없다.
- `Review/Room_XX_Gold.png`, `Review/Estate_Refined.png`: Blender EEVEE 검토 캡처.
- `Unity_Edit_Estate.png`: Unity 적용본 전체 외관.

## 재적용

Unity Editor 전용 `MansionVisualGoldPipeline`이 `mansion-gold.request`를 프로젝트 상위 `C:/UnityProjects/DeductionGame/`에서 읽는다. `Refresh` → 컴파일 완료 후 `Setup` → 완료 후 `Audit` 순서다. OS 입력이나 GUI 자동화 없이 Editor API만 사용한다. `PruneGenerated`는 현재 씬/manifest가 사용하지 않는 이 패스의 생성 에셋만 정리한다.

Blender 제작 스크립트들은 단계별 기록이다. 완료 씬에 모든 빌더를 무작정 재실행하지 않는다. 수정 후 `export_gold.py`, `package_authoring.py`로 내보내고 텍스처 상대 경로를 보존한다. 제작용 외부 텍스처 133개에는 roughness 원본 등이 포함되어 게임용 112개와 수가 다르다.

OS 마우스/키보드/커서/화면 클릭/GUI 자동화는 사용하지 않았다. Blender 변경은 MCP, Unity 변경은 프로젝트 및 Editor API로 수행했다. 기존 캐릭터, 게임 규칙, AI, 멀티플레이 코드는 변경하지 않았다.
