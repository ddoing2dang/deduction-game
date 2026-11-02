# 검은 장미의 비밀 — 모바일 Godot 독립판
PC Unity 게임의 3D 모델, 규칙, 코드, 에셋을 **절대 수정하지 않는** 모바일 전용 별도 프로젝트입니다.

## 구현
확정 캐릭터 여섯 명(마리히, 찌로, 나즈, 마또, 보키, 문이레)과 검은 장미 저택 9개 공간을 활용한 싱글플레이 간편 추리 게임. 각 사건은 범인/장소/도구 정답을 무작위로 생성합니다. 12번까지 인물·장소·도구를 조사해 비정답 후보를 제거하고 수첩을 확인한 뒤 최종 지목으로 해결합니다. 최소 3번 조사 후 지목 가능하며 2번 틀리면 패배. 현지 저장 기능 지원.

## 실행
Godot 4.3 이상에서 `mobile-godot/project.godot`을 프로젝트로 열고 F5 실행. Android Export Templates/JDK/Android SDK가 준비되면 Android Export.

## 원본 캐릭터 PNG
`assets/characters/CHR_MARIHI.png`, `CHR_JIRO.png`, `CHR_NAZ.png`, `CHR_MATTO.png`, `CHR_BOKI.png`, `CHR_MUNIRE.png` 필요. GitHub 브랜치에는 바이너리 원본이 아직 포함되지 않았으므로 대화에서 제공된 완전한 Godot ZIP에 들어 있는 원본 6장을 같은 경로에 복사하세요. 이미지가 없어도 임시 이름 카드로 구동됩니다.

본 프로토타입의 도구 목록은 모바일 전용 예시이며 PC판의 미확정 흉기 명단과 무관합니다. Godot 실제 실행/Android 설치 검증은 아직 완료되지 않았습니다.
