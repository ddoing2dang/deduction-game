# HOTEL H00 PBR 원본과 가공 기록

아래 다운로드 원본은 CC0 1.0이다. 라이선스와 개별 에셋 페이지를 함께 보존한다. [CC0 1.0 법률문](https://creativecommons.org/publicdomain/zero/1.0/legalcode) · [Poly Haven 라이선스](https://polyhaven.com/license) · [ambientCG 라이선스](https://docs.ambientcg.com/license/)

| 원본 | H00 사용처 |
| --- | --- |
| [Walnut Veneer · Poly Haven](https://polyhaven.com/a/walnut_veneer) | 2K 월넛과 짙은 월넛. 슬롯별 검붉은 틴트 |
| [Velour Velvet · Poly Haven](https://polyhaven.com/a/velour_velvet) | 2K 버건디 벨벳과 솔기·러그용 직물 |
| [White Plaster 02 · Poly Haven](https://polyhaven.com/a/white_plaster_02) | 2K 석고와 몰딩 |
| [Marble 012 · ambientCG](https://ambientcg.com/view?id=Marble012) | 2K 아이보리·적갈 대리석. 적갈은 동일 원본에 별도 틴트와 UV 오프셋 적용 |
| [Marble 006 · ambientCG](https://ambientcg.com/view?id=Marble006) | 2K 암색 대리석 |
| [Marble 018 · ambientCG](https://ambientcg.com/view?id=Marble018) | 2K 연마 석재. 노멀 강도와 틴트 조정 |
| [Metal Plates 001 · ambientCG](https://ambientcg.com/view?id=MetalPlates001) | 2K 브러시 강판 원본의 결·스크래치와 금속도 맵을 황동색으로 보정 |
| [Studio Small 03 · Poly Haven](https://polyhaven.com/a/studio_small_03) | 1K EXR, 로비 안의 반사 프로브 전용 |

`_Base.jpg`는 원본 Diffuse 또는 Color, `_Normal.jpg`는 원본 Normal GL이다. `_MetalSmooth.png`의 R은 금속도, A는 선형 `1 − Roughness`다. 비금속의 R은 0이다. 황동의 Roughness는 원본 변화를 0.34~0.58 범위로 압축했다. Unity에서는 Base Color만 sRGB로 읽고 Normal GL은 Normal map으로 임포트한다.

## Unity 편집 모드

- **Case464 → Hotel H00 → Apply CC0 PBR Materials**로 기존 H00 전용 URP Lit 10개를 갱신한다. 기존 머티리얼 GUID를 유지한다. 창 유리·식물·유백색 발광 슬롯 3개는 기존 설정을 사용한다.
- **Case464 → Hotel H00 → Open PBR Lighting Overlay**로 승인된 `H00_Lobby_Work`와 별도 조명 씬을 함께 연다. 첫 실행에서 `H00_Lighting_Review.unity`를 저장한다. 샹들리에와 벽등의 따뜻한 빛, 창 방향의 차가운 스폿과 그림자, 반사 프로브를 이 씬에 보관한다. 승인 씬을 단독으로 열면 기존 조명만 표시된다.
