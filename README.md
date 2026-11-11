# 사건파일 No.464 — 1인 탐정·시네마틱 추리 게임

**현재 개발 기준: Windows PC x64 · Unity 6 URP 신규 프로젝트 예정**. 2026-11-10 현재 GitHub `main`에 **새 Unity 프로젝트/씬/프리팹/런타임 코드는 없음**. 실제 에디터 연결·실행·빌드가 됐다고 주장하지 않는다.

## 확정된 게임 구조
- 탐정 플레이어 1명, NPC 6명(마도은·마루희·나지수·백보미·진서아·문이현), 매 사건 범인 1명 랜덤.
- HOTEL·CRUISE·MUSIC_BROADCAST 테마마다 공동 로비 1 + 개인실 6 + 살해 현장 1 = 8 조사 공간.
- 범행수법은 4대 분류, 흉기 후보 12종/각 테마 NPC 개인실에 실물 18개.
- 자유 한국어 질문 → **AI는 사전 검수된 대사만 선택**, 거짓 정보는 해당 판 범인만 진술 가능.
- 최종 고발은 **범인·범행수법·흉기**를 한국어 문장으로 직접 제출. 범행 장소는 채점하지 않음.

## 현재 Unity 고품질 맵 작업
- [Unity·GitHub·MCP 연결 + 프리미엄 3D 쿼터뷰 제작 전체 가이드](UNITY_MAP_MASTERPLAN_20261110.md)
- [Windows Unity 비파괴 GitHub 연결 스크립트](tools/windows/Connect-Case464Unity.ps1)
- [Windows Unity/GitHub 로컬 준비 상태 읽기 전용 검사](tools/windows/Check-Case464Readiness.ps1)
- [Unity 에디터 고품질 맵 워크숍 소스](tools/unity/Case464MapWorkshop.cs)
- **현재 GitHub에서 도구 소스와 가이드만 저장. 사용자 PC에서 열린 Unity에 MCP 연결 또는 해당 스크립트 적용은 아직 확인되지 않음.**
- 동일 오류 2회 반복 시 중단·준비물 요청·마지막 성공 지점 기록을 최우선으로 함.

## 현재 주요 문서
- [게임 최상위 확정 규칙](GAME_RULES.md)
- [시네마틱 대화·조사·의심·추궁 장면 설계 (2026-11-10)](UI_CINEMATIC_PRESENTATION_20261110.md)
- [장면 전환/표시 데이터 계약](game/ui/scene_presentation.v1.json)
- [진행도 복구·구형 데이터 분리·연결 현황](game/RESTORE_STATUS_20261110.md)
- [NPC 자유 심문 계약](NPC_FREE_DIALOGUE.md)
- [사건 랜덤 생성/검증 계약](RANDOM_CASE_AI_DESIGN.md)
- [최종 고발 해석/판정](NATURAL_LANGUAGE_DEDUCTION.md)
- [3테마 8방](MAPS.md)
- [3테마 원본 대화창](art/UI/DialogueFrames/ThemeDialogue_v1/README.md)
- [24개 공간 시각 차별화 자료](art/Environment/ROOM_DISTINCTION_24.md)
- [2026-11-10 사용자 참고 이미지 반영: 지붕 없는 3D 컷어웨이 디오라마 맵](art/Environment/CUTAWAY_DIORAMA_MAP_REFERENCE_20261110.md)
- [고각 쿼터뷰 카메라·검은 스테이지·층별/방별 화면 설정](game/maps/cutaway_camera_presentation.v1.json)
- [HOTEL 중앙 로비 첫 고품질 제작 브리프](art/Environment/HOTEL_H00_PREMIUM_BRIEF.md)
- [Unity 신규 프로젝트 재시작 안내](game/UNITY_RESTART_GUIDE.md)

## 이전 데이터와 백업
2026-11-07 이전 6인 PvP/카드·턴·웹/모바일 게임 코드는 `main`에 복원하지 않았다. 오래전에 만들어졌더라도 **새 게임에 필요한 NPC/대화창/3D 환경 에셋은 보존**한다. 이전 Unity 대화 장면 프로토타입은 `archive/dialogue-prototype-before-full-reset-796cbd5-20261109`에 남아 있으며 새 프로젝트로 자동 복원하지 않는다.

**검증 전 사건과 대사는 게임에 바로 적용하지 않는다.** 새 Unity 프로젝트와 로컬 PC 작업 폴더는 현재 연결 불가. 사용자 명시 요청 전 기능 테스트/Play Mode/EXE 빌드를 수행하지 않는다.
