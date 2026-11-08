# 마또 — PC 전용 3테마 CC0 가구·장식 에셋 카탈로그 (2026-11-09)

**에셋만 추가 저장했습니다.** 기존 Unity 맵·씬·게임 로직을 복구하거나 수정하지 않았으며, 기능 테스트나 빌드는 하지 않았습니다.

## 현재 등록된 3D 모델
| 팩 | 현재 모델 | 형식 | 용도 | 출처 |
|---|---:|---|---|---|
| KayKit Furniture Bits 1.0 | **53** (기존 19 + 신규 34) | OBJ | 가구, 침대, 소파, 책장, 액자, 카펫, 화분 | [원본](https://github.com/KayKit-Game-Assets/KayKit-Furniture-Bits-1.0) |
| KayKit Restaurant Bits 1.0 | **36** 신규 | OBJ | 문, 벽, 창, 기둥, 주방, 식탁, 식기 | [원본](https://github.com/KayKit-Game-Assets/KayKit-Restaurant-Bits-1.0) |
| Kenney Castle Kit 2.0 | **15** 신규 | OBJ | 고전풍 문, 계단, 아치, 벽 장식 | [원본](https://kenney.nl/assets/castle-kit) |
| Kenney Furniture Kit 2.0 | **18** 신규 | GLB | 거울, 화장대용 수납장, 코트걸이, 방송실 가구, 모니터·라디오 | [원본](https://kenney.nl/assets/furniture-kit) |

**합계: 122개 환경 모델(이 중 신규 103개) + 별도 보존된 기존 3D 액세서리 GLB 34개.**

모든 모델 팩은 **CC0 1.0**으로 개인·상업 게임 무료 사용 가능하며 저작자 표시 의무는 없습니다. 각 팩 폴더에 원본 LICENSE.txt를 보존했습니다. 원본 무료 배포 팩에서만 선택했고 유료 EXTRA/SOURCE 에셋은 포함하지 않았습니다. 다른 모델 판매 플랫폼의 에셋이나 비상업 전용 라이선스 자산은 넣지 않았습니다.

## 테마별 구성 추천

### 1. HOTEL · 대저택 (블랙/레드)
- 고전풍 구조: Kenney Castle `door`, `wall-pillar`, `stairs-stone`, `tower-square-arch`
- 가구: KayKit `armchair_pillows`, `cabinet_medium_decorated`, `shelf_B_large_decorated`, `bed_double_B`
- 소품: 액자 `pictureframe_large_B`, 카펫 `rug_rectangle_stripes_A`, `lamp_standing`

### 2. CRUISE · 크루즈 (블루/화이트)
- 실내 구획: Restaurant Bits `door_A`, `wall_doorway`, `wall_window_closed`, `pillar_A`
- 라운지/식당: Furniture Bits `chair_B_wood`, `table_medium_long`; Restaurant Bits `table_round_A`, `kitchencounter_sink`
- 소품: `plate`, `bowl`, `menu`, `jar_B_medium`, Kenney `lampRoundTable`

### 3. MUSIC_BROADCAST · 음악방송 대기실 (골드/핑크)
- 분장/대기: Kenney `bathroomMirror`, `bathroomCabinetDrawer`, `desk`, `coatRackStanding`
- 방송실 소품: `computerScreen`, `computerKeyboard`, `laptop`, `radio`, `lampWall`
- 라운지: `loungeDesignSofa`, `lampRoundFloor`, `pottedPlant`

**위 테마별 추천 목록은 선택 가이드이며, 모델을 해당 테마에만 사용할 수 있다는 제한은 아닙니다.** 전체 정확한 파일별 경로·출처 SHA와 추천 조합은 [ASSET_CATALOG.json](ASSET_CATALOG.json)에 기록했습니다.

## Unity 6 URP에 가져오기
1. **새 Windows x64 Unity 프로젝트** 생성 후 필요한 팩의 `Models/` 전체를 `Assets/Art/Environment/<PackName>/` 아래로 복사합니다. **OBJ와 해당 MTL·PNG를 동일한 폴더 구조로 유지**하십시오.
2. **KayKit Furniture Bits 기존 19개**는 예전 `kaykit_furniture.mtl`과 임시 TGA 팔레트를 가리킵니다. 원본 색상 재현은 새로 저장한 `furniturebits_texture.png`를 사용한 `kaykit_furniture_original.mtl`을 기반으로 Unity Material을 수동 지정하면 됩니다. 새 34개는 이미 원본 텍스처용 MTL을 참조합니다.
3. **KayKit Restaurant Bits / Kenney Castle Kit OBJ**는 원본 PNG와 공통 MTL을 함께 넣었습니다. URP/Lit Material로 가져온 텍스처를 수동 검수하고 재질이 분홍색으로 변하면 URP 파이프라인 및 재질 Shader 설정을 확인합니다.
4. **Kenney Furniture GLB**는 별도 glTF 2.0 에디터 임포터가 필요합니다. Unity Package Manager에서 `com.unity.cloud.gltfast` (Unity glTFast) 패키지를 추가한 뒤 파일을 Assets 안으로 가져옵니다. [Unity glTFast 문서](https://github.com/Unity-Technologies/com.unity.cloud.gltfast/blob/main/Packages/com.unity.cloud.gltfast/Documentation~/ImportEditor.md).
5. 스케일, 축, 피벗, 텍스처/머티리얼과 저폴리 스타일 일치 여부를 Unity Editor에서 직접 점검해야 합니다. 이 단계는 아직 **실행하지 않았습니다**.

## 원본 보존 및 주의
- *이 작업은 아트 에셋 스테이징입니다.* 기존 맵이나 게임 코드를 복원하지 않습니다.
- 이 모델들은 스타일라이즈드 로우폴리 소품이므로 **완성된 고품질 디오라마 맵이라고 주장하지 않습니다.** 최종 화면에는 테마 재질, 공간 배치, 조명, 카메라, 추가 수작업 디테일이 필요합니다.
- 원본 PNG 컬러 아틀라스 3개는 출처 원본 Git blob SHA와 GitHub에 저장된 blob SHA가 동일함을 확인했습니다.
- 라이선스 원본과 출처는 각 ThirdParty 하위 팩 폴더에 영구 보관합니다.
- `game/UnityProject`를 생성하거나 Play Mode/기능 테스트/EXE 빌드를 수행하지 않았습니다.
