# GitHub 작업 브랜치 정리 기록 — 2026-11-09

**원인:** 새 대화 씬에는 맵이 없지만 기존 작업 브랜치 `e1b1e6f`에 구형 전체 저장소와 `Assets/Data/Preserved/maps`의 사본이 남아 있었음.

**GitHub 수정:** `main`의 깨끗한 8파일(UI 원본 6 PNG + 기본 파일)과 새 Unity 프로젝트의 파일만 재구성. 이전 맵/사건/대사/웹 코드/모델 에셋/문서 및 `Assets/Data/Preserved`를 제외. Unity 원본 씬·프리팹·스크립트·PNG·TMP 폰트·ProjectSettings 유지. 최소 NPC 이름 JSON 6개를 새 위치로 이전, 동일 GUID 메타 유지, 에디터 스크립트 경로 갱신.

**검증 범위:** GitHub 트리와 씬 YAML을 통해 구형 맵 파일 및 씬 루트 여부 확인. 실제 로컬 Unity Editor 화면, Play Mode, 빌드/기능 테스트는 실시하지 않음.

**사용자 로컬 적용:** 기존 Unity 프로젝트 또는 예전 Scene이 열려 있을 수 있으므로 프로젝트 경로와 `DialoguePrototype.unity` 명시적으로 확인 필요.
