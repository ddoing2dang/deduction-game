# mystery 대화창 작업 보고서

2026-11-09 · Unity 6000.3.26f1 · Windows PC · Edit Mode

- 기존 DialoguePanel 프리팹에 MaruBuri와 Pretendard의 원본 OTF로 생성한 동적 TMP SDF 네 개를 연결했다. 이름, 대사, 질문 입력, 자리표시자, 기록 본문, 안내, 기록 제목과 전송 라벨의 직렬화된 폰트와 머티리얼을 확인했다. 두 폰트의 라이선스를 포함했다.
- 기존 DialoguePreview 씬에서 1920×1080 정적 렌더로 Mansion, Cruise, MusicBroadcast의 한글 표시와 배치를 확인했다. 세 프레임의 원본 PNG 해시가 작업 시작 시 파일과 일치한다. 기존 질문 입력창, 전송 버튼, 기록 ScrollRect 및 IME 코드를 유지했다.
- 마도연, 마루희, 나주은, 이복희, 진루나, 문일혜를 Edit Mode에서 순서대로 선택해 이름표를 확인했다. 기존 enum 인덱스 0~5를 유지했다.

## NPC 초상화 6종 연결 완료 (2026-11-09)

- 이전 보고 시점의 `0/6` 미완료 상태를 최신 작업 커밋 `ae4d24c0aafa7ec6767da12b1f4300f7d4b54c85` 기준으로 갱신.
- PNG 6개 + 각 `.png.meta` 6개가 `game/UnityProject/Assets/UI/Art/NPCPortraits/` 경로에 **원격 GitHub 저장 완료**.
- 마도연(`Madoyeon.png`), 마루희(`Maruhee.png`), 나주은(`Najueun.png`), 이복희(`Ibokhee.png`), 진루나(`Jinruna.png`), 문일혜(`Munilhye.png`) 순서로 기존 0~5 enum 유지.
- `DialoguePanel.prefab`의 `speakerPortraits` 6개 GUID와 각 `.png.meta` GUID가 모두 정확히 일치함을 GitHub 원격 파일에서 확인.
- Unity Edit Mode에서 이름·초상화 매핑, 투명도, 비율, 대사 영역과 겹치지 않는 배치를 확인했다는 작업 완료 보고를 받음. 이 문서 갱신에서 Unity를 추가 실행하지 않음.
- 원본 PNG와 프리팹 바이너리·직렬화 데이터는 이 **문서 갱신 커밋에서 수정하지 않음**.

Play Mode, 기능 테스트, 실행 파일 빌드는 실시하지 않았다.
