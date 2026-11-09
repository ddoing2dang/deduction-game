# mystery 동기화 검증 · 2026-11-10

기준 커밋 `127c5bb5cd181e75a559c863ce3c2c5211e57de2`. 변경 전 프리팹과 C#을 `mystery/Logs/sync-backup-20261110-010342/`에 백업했다. Interrogation 전체와 폴더 .meta, DialoguePanel 프리팹과 스크립트를 동기화했다.

- Unity Edit Mode에서 새로 임포트하고 C# 컴파일 완료를 확인했다. 오류 0개, 컴파일 실패 플래그 false. 프리팹과 열린 DialoguePreview 씬의 Missing Script는 각각 0개다. 씬의 저장되지 않은 편집 내용은 보존했다.
- 프리팹의 심문 어댑터, 수사 수첩, 현장 조사 컴포넌트 및 사건 JSON 3개·조사 JSON 3개 참조를 확인했다. 사건 JSON 3개 모두 `approved: false`다.
- NPC 이름·초상화 6쌍과 TMP 폰트 8개 연결을 확인했다. 원본 초상화와 .meta, 프레임, 씬은 덮어쓰지 않았다.
- 원격 추적 Assets 183개를 바이트 단위로 비교했다. 181개 일치, 누락 0개다. 차이는 의도적으로 보존한 `Assets/UI/Fonts/NotoSansKR SDF.asset` 및 `Assets/UI/Fonts/Pretendard-Regular SDF.asset` 두 개다.

## 남은 경고

`Assets/Interrogation/Scripts/InvestigationNotebookView.cs:113` 및 `Assets/Interrogation/Scripts/InvestigationSceneView.cs:157`에서 CS0618 두 건이 발생한다. `TMP_Text.enableWordWrapping` 대신 기존 줄바꿈 동작과 같은 `textWrappingMode`를 지정하면 된다. 이번 동기화에서는 원격 코드를 그대로 반영했다.

Play Mode, 기능 테스트, EXE 빌드는 실행하지 않았다.
