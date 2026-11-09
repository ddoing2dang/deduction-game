# 마또 — 테마 3종 × 8공간 = 24공간 시각 차별화 설계

**Windows PC 전용 · 2026-11-09 · 아트 구성안만 저장 · Unity 맵/씬 변경 없음**

각 공간마다 **고유 대표 3D 오브젝트 1개**를 반드시 부여하고, 벽·바닥 소재/가구 군집/조명/색/시선을 달리한다. 같은 방 프리팹을 복제한 뒤 색만 바꾸는 방식은 사용하지 않는다.

| 테마 | 공간 | 공간 성격 | 고유 대표 오브젝트 | 핵심 시각 차별화 |
|---|---|---|---|---|
| HOTEL | H00 | 대저택 중앙 로비 | `stairs-stone-square.obj` | 중앙 계단과 긴 대칭축, 양측 대기 소파 / #8F343F |
| HOTEL | H01 | 고서재형 개인실 | `bookcaseClosedDoors.glb` | 한 면 전체 책장, 반대쪽 단독 집필 코너 / #765A36 |
| HOTEL | H02 | 골동품 수집실 | `cabinet_medium_decorated.obj` | 방 둘레 진열장과 중간 둥근 러그 / #A88452 |
| HOTEL | H03 | 레트로 객실 | `televisionVintage.glb` | 침대가 벽 뒤쪽, 입구 옆 빈티지 TV / #B06762 |
| HOTEL | H04 | 식물 애호가 스위트 | `pottedPlant.glb` | 창가에 다층 식물, 창을 향한 독서 의자 / #6A8D69 |
| HOTEL | H05 | 욕실 포함 특실 | `bathtub.glb` | 욕실과 수면 공간을 낮은 파티션으로 구획 / #72A5A6 |
| HOTEL | H06 | 절제된 방문객 객실 | `bed_double_B.obj` | 기능적인 침대와 수납장, 벽면 TV / #667F9E |
| HOTEL | H07 | 아치형 사건 현장 | `tower-square-arch.obj` | 높은 아치로 공간을 나누고 사선 가구 배치 / #5B505D |
| CRUISE | C00 | 아트리움 라운지 | `loungeSofaCorner.glb` | 긴 전망창을 향한 L자 라운지 / #397CA4 |
| CRUISE | C01 | 오션뷰 승객실 | `loungeChairRelax.glb` | 창가 리클라이너, 낮은 침대 / #74B9D6 |
| CRUISE | C02 | 선내 다이닝 객실 | `kitchenBar.glb` | 전면 음식 바, 좌우 원형 테이블 / #D4B079 |
| CRUISE | C03 | VIP 스위트 객실 | `tableCoffeeGlass.glb` | 긴 소파와 유리 가구로 넓어 보이는 배치 / #C7AC86 |
| CRUISE | C04 | 선내 팬트리 객실 | `kitchenFridgeLarge.glb` | 서빙 동선 우선, 카운터 일렬 배치 / #7E9FA6 |
| CRUISE | C05 | 선원 공용 침실 | `bedBunk.glb` | 2층 침대 중심, 좁은 통로, 작은 생활 테이블 / #728896 |
| CRUISE | C06 | 항해 도구 보관 선실 | `mast-ropes.glb` | 벽 한쪽에 항해용 밧줄, 작은 작업 테이블 / #9B805F |
| CRUISE | C07 | 적재물 보관 사건 현장 | `chest.glb` | 높은 적재 선반 + 통로 중앙 빈 공간 / #526E82 |
| MUSIC_BROADCAST | M00 | 공동 대기 홀 | `loungeDesignSofa.glb` | 로비 중앙 소파 군집, 공용 안내 화면 / #E7A9B7 |
| MUSIC_BROADCAST | M01 | 메이크업 준비실 | `bathroomMirror.glb` | 거울 벽을 중심으로 개별 좌석 2개 / #F6B4C3 |
| MUSIC_BROADCAST | M02 | 헤어 스타일링실 | `coatRackStanding.glb` | 의자 열과 세면대가 중심인 작업 동선 / #C1A3CD |
| MUSIC_BROADCAST | M03 | 무대 의상 준비실 | `cabinetTelevisionDoors.glb` | 행거·수납장 벽, 중앙 피팅 공간 / #B47E96 |
| MUSIC_BROADCAST | M04 | 음향 모니터링실 | `speaker.glb` | 듀얼 스피커를 향한 책상과 모니터 / #6265A6 |
| MUSIC_BROADCAST | M05 | 휴대 장비 리허설실 | `radio.glb` | 중앙 휴식 의자, 모서리 미디어 장비 / #F1C18E |
| MUSIC_BROADCAST | M06 | 간식·휴게실 | `kitchenCoffeeMachine.glb` | 사각 간식대와 벽면 개수대 / #87C4AA |
| MUSIC_BROADCAST | M07 | VIP 별실 사건 현장 | `desk.glb` | 작은 별실 안 고급 책상·소파 코너 / #C78B65 |

## 제작 규칙

- 로비 1 + 개인실 6 + 사건 현장 1을 각 테마별로 시각적으로 구분하는 **편집용 구성 시안**이다. 실제 방 연결/NPC 배치/사건 진실은 확정하거나 복원하지 않는다.
- 고유 대표 모델은 24공간 전체에서 한 번만 지정한다. 다른 방에서 일반 보조 모델을 재사용하는 것은 가능하지만 대표 구조와 조명/바닥/벽/기물 군집은 중복 금지.
- **에셋 충분성은 숫자가 아니라 공간에 맞는 구별 가능한 형태·질감·장식으로 판단**한다. 음악방송 전용 화장대 전구·고급 무대 마이크/악기, 크루즈 선실 창/선체 모듈, 호텔 고급 실내 기둥/패널 재질은 추가 제작이 필요한 부분이다. 기존 소품을 무리하게 그 물품이라고 주장하지 않는다.
- 각 방의 보조 모델·구조 모델·재질·조명 온도·기물 테마는 [JSON 구성표](room_profiles_24.v1.json)의 파일 경로와 일치하게 작업한다.
- JSON에 나온 모든 3D 파일이 현재 GitHub main에 있는지 검사했고, 대표 모델 중복을 검사했다. **실제 Unity 화면 품질은 테스트/검증하지 않았다.**
