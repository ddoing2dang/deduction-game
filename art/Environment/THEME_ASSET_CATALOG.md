# 마또 — 테마별 무료 3D 환경 에셋 통합 카탈로그 (2026-11-09)

**총 206개 CC0 환경 모델** (초기 19 + 이전 추가 103 + 이번 추가 84), 별도 원래 액세서리 GLB 34개 유지. **Windows x64 전용**, Unity 맵·씬·게임 스크립트는 변경하지 않음.

## 3테마 × 8공간 전용 차별화 자료

- [24개 공간별 고유 가구·장식·조명 설계](ROOM_DISTINCTION_24.md)
- [전체 24공간 상세 JSON](room_profiles_24.v1.json) — **24개 서로 다른 대표 모델**, 공간별 소재·구조·조명·보조 모델 경로.
- [실제 전체 206개 모델 목록 및 원본 SHA](ASSET_CATALOG.json)

| 출처·팩 | 모델 | 테마 활용 | GitHub 폴더 |
|---|---:|---|---|
| KayKit Furniture Bits (CC0) | 53 OBJ | 침실·소파·식물·책장·카펫·액자 | [모델](ThirdParty/KayKitFurnitureBits/Models/) |
| KayKit Restaurant Bits (CC0) | 36 OBJ | 출입문·실내 벽·부엌·식당 | [모델](ThirdParty/KayKitRestaurantBits/Models/) |
| Kenney Castle Kit (CC0) | 15 OBJ | 대저택 아치·문·계단·장식 기둥 | [모델](ThirdParty/KenneyCastleKit/Models/) |
| Kenney Furniture Kit (CC0) | 78 GLB | 화장대·오디오 장비·침실·서재·소파·욕실 | [모델](ThirdParty/KenneyFurnitureKit/Models/) |
| Kenney Pirate Kit (CC0) | 6 GLB | 크루즈 선내 창고·해양 소품(양식에 맞춰 제한적 사용) | [모델](ThirdParty/KenneyPirateKit/Models/) |
| Kenney Food Kit (CC0) | 18 GLB | 호텔 룸서비스·크루즈 식당·방송 대기실 간식 | [모델](ThirdParty/KenneyFoodKit/Models/) |

## CC0 사용권 및 원본 출처

각 팩 폴더에 `LICENSE.txt` 원본을 포함. **모두 상업 게임에서 무료 사용 가능, 저작자 표시 의무 없음** (원저작자 표시는 권장).

- KayKit (Kay Lousberg): [Furniture Bits](https://github.com/KayKit-Game-Assets/KayKit-Furniture-Bits-1.0), [Restaurant Bits](https://github.com/KayKit-Game-Assets/KayKit-Restaurant-Bits-1.0).
- Kenney: [Castle Kit](https://kenney.nl/assets/castle-kit), [Furniture Kit](https://kenney.nl/assets/furniture-kit), [Pirate Kit](https://kenney.nl/assets/pirate-kit), [Food Kit](https://kenney.nl/assets/food-kit). 마지막 두 GLB의 공개 저장소 미러는 [Hidencod/tge-assets](https://github.com/Hidencod/tge-assets)이며 저장소의 CC0 LICENSE에 Kenney 모델/변환 GLB가 포함됨.

## Unity 사용

1. 새로운 Windows Unity 6 URP 프로젝트의 `Assets/Art/Environment/`로 필요한 `Models` 폴더를 개별 복사. 현재 GitHub에는 해당 Unity 프로젝트가 생성되어 있지 않다.
2. KayKit/Castle OBJ는 MTL과 해당 원본 PNG 텍스처를 같은 폴더 구조로 유지하며 URP/Lit 머티리얼을 검수. KayKit Furniture의 기존 19 OBJ는 임시 TGA용 MTL을 참조하므로 원본 `furniturebits_texture.png`를 쓰려면 재질 수동 교체.
3. Kenney GLB는 Unity 패키지 관리자에서 [glTFast](https://github.com/Unity-Technologies/com.unity.cloud.gltfast) 설치 후 임포트. 저장된 GLB들은 실제 Unity에 임포트·화면 확인하지 않았음.
4. 24공간은 같은 프리팹을 색만 바꾸지 말 것. 고유 대표 소품 + 천장·벽·바닥 + 조명 + 주요 가구 실루엣을 달리 지정해야 한다.
5. 모델 대부분은 스타일라이즈드 저폴리이므로 **고품질 완성 디오라마는 아님**. 호텔 고급 패널·크루즈 전용 선실/창·방송 화장대 전구/마이크/악기 같은 전용 에셋과 아트 마감 작업이 여전히 필요.

**기존 에셋 보존 / Unity 씬, 맵 파일, C# 런타임 변경 없음 / 기능 테스트 및 EXE 빌드 미실시**.
