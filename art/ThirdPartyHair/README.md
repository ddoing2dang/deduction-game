# 무료 상업용 / GitHub 재배포 가능 헤어 소스 (CC0)

이 폴더는 **헤어 후보 에셋의 원본 보관소**입니다. 승인된 모델, 대저택, Unity 프리팹을 수정하지 않습니다.

## GitHub 보관 목록 — 공식 VRoid 구 베타 CC0 샘플

아래 `.vrm` 파일에는 **헤어를 포함한 전체 샘플 아바타**가 들어 있습니다. Blender 5.0.1 + VRM Add-on에서 헤어 메쉬를 추출하여 기존 캐릭터 리그의 Head 본에 맞춰 쓸 수 있습니다. **단독 헤어 모델이 아닙니다.**

| 파일 | 용도 | 원본 Git blob SHA-1 |
|---|---|---|
| `HairSample_Female.vrm` | 여성 헤어 샘플, 긴 머리 변형 | `0e5235f9a7972fbcbceb74960479b208c27a8707` |
| `HairSample_Male.vrm` | 짧은 헤어 후보 | `46e8c583ce5df3b8ee167141b59ba3ad583a2687` |
| `Sendagaya_Shino.vrm` | 여성 롱헤어 후보 | `91f7c9e9ec990efb9de305e4c1d0e32a46498830` |
| `Victoria_Rubin.vrm` | 여성 웨이브 헤어 후보 | `3c59603eee2028756ed41ed4930dca6d99330f1e` |

### 라이선스 출처 (모두 무료 · 상업 사용 가능 · 수정/재배포 가능)

- 공식 VRoid 구 베타 CC0 HairSample 2종: https://vroid.pixiv.help/hc/en-us/articles/4402614652569-Do-VRoid-Studio-s-sample-models-come-with-conditions-of-use
- Sendagaya Shino 공식 개별 CC0: https://vroid.pixiv.help/hc/ja/articles/360013482714
- Victoria Rubin 공식 개별 CC0: https://vroid.pixiv.help/hc/ja/articles/360014900233
- CC0 법적 전문: https://creativecommons.org/publicdomain/zero/1.0/

### 원본 다운로드 (버전 고정)

https://github.com/madjin/vrm-samples/tree/e16eb187100149a315ad92c3c9968f1d5baa6c7d/vroid/beta

`fetch_cc0_hair.py`는 이 커밋의 원본 4개를 내려받고 **파일 크기와 Git blob SHA를 검사**합니다. GitHub Actions를 통해 이 브랜치에 실제 바이너리를 저장합니다. 누락 시 같은 스크립트를 로컬 PC에서 재사용할 수 있습니다. 유료 결제 불필요.

## 추가 CC0 후보 (원본 링크만)

**Quaternius Universal Base Characters**: 20개 교체형 헤어와 모델, 무료 Standard, 상업 사용/재배포 CC0. 다만 **로우폴리 스타일**이라 보키·마리히의 고품질 애니풍 외형과 맞는지 실제 비교가 필요합니다.

- 공식: https://quaternius.com/packs/universalbasecharacters.html
- 무료 Standard: https://quaternius.itch.io/universal-base-characters
- 122 MB Standard ZIP은 여기에는 넣지 않았습니다.

## 실제 캐릭터에 적용할 때

- 보키의 검증된 162본 VRoid 리그·체형·얼굴·허리/치마/신발은 유지합니다.
- 새 헤어를 **작업본에만** 가져와 보키의 버건디 단발·눈썹 근처 앞머리 레퍼런스와 비교해 맞는 후보만 사용합니다.
- **승인된 보키 작업본 자동 교체 금지**; 텍스처·헤어 리깅과 Head 회전 검증을 별도로 시행해야 합니다.
- 상업 사용을 허용해도 **원본 재배포를 금지하는 BOOTH 상품은 이 공개 GitHub에 저장하지 않습니다.**
- 라이선스 확인: 2026-10-31.
