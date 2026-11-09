# mystery 대화창 작업 보고서

2026-11-09 · Unity 6000.3.26f1 · Windows PC · Edit Mode

- 기존 DialoguePanel 프리팹에 MaruBuri와 Pretendard의 원본 OTF로 생성한 동적 TMP SDF 네 개를 연결했다. 이름, 대사, 질문 입력, 자리표시자, 기록 본문, 안내, 기록 제목과 전송 라벨의 직렬화된 폰트와 머티리얼을 확인했다. 두 폰트의 라이선스를 포함했다.
- 기존 DialoguePreview 씬에서 1920×1080 정적 렌더로 Mansion, Cruise, MusicBroadcast의 한글 표시와 배치를 확인했다. 세 프레임의 원본 PNG 해시가 작업 시작 시 파일과 일치한다. 기존 질문 입력창, 전송 버튼, 기록 ScrollRect 및 IME 코드를 유지했다.
- 마도연, 마루희, 나주은, 이복희, 진루나, 문일혜를 Edit Mode에서 순서대로 선택해 이름표를 확인했다. 기존 enum 인덱스 0~5를 유지했다.

## 남은 작업

초상화 원본 ZIP 및 PNG 6장이 작업 브랜치, GitHub 이력 또는 릴리스에 없었으므로 초상화 연결은 0/6이다. Madoyeon.png, Maruhee.png, Najueun.png, Ibokhee.png, Jinruna.png, Munilhye.png와 각각의 .meta가 필요하다. 원본이 제공되면 NPCPortraitMapping.json의 SHA-256과 대조하고 기존 연결 도구로 프리팹에 할당한 뒤 세 테마에서 배치를 다시 확인해야 한다.

Play Mode, 기능 테스트, 실행 파일 빌드는 실시하지 않았다.
