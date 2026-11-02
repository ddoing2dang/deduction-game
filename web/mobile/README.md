# 검은 장미의 비밀 — 모바일 웹 버전

확정된 6명 캐릭터의 **이미지 그대로** 사용한 독립 2D 싱글플레이 추리 게임. 기존 PC용 Unity 게임/기존 규칙과 코드를 수정하지 않습니다.

## 실행
`index.html` 파일을 웹브라우저로 열거나, `python -m http.server 8080`에서 `/`에 접속합니다. 정적 HTML/JS/CSS라 백엔드, 계정, 설치 없이 실행됩니다. 모바일에서는 세로 화면에 최적화되어 있습니다.

## GitHub Pages
기존 저장소 `ddoing2dang/deduction-game`의 `web/mobile/`에 복사하면 기존 `.github/workflows/pages.yml`이 `web/` 전체를 배포하므로 `https://ddoing2dang.github.io/deduction-game/mobile/`이 경로가 됩니다. 실제 GitHub Actions 배포 여부와 URL의 응답은 별도로 확인해야 합니다.

## 독립 간편 규칙
검은 장미 저택 9개 방 + 고정 캐릭터 6명 + **모바일 전용 예시** 도구 9종. 12회 중 1회 조사를 사용하면 비정답 후보만 제외됩니다. 인물 대화는 분위기용이며 오해를 막기 위해 정답 단서로 사용하지 않습니다. 3회 이상 조사 후 최종 지목 가능; 2번 실패하면 사건 종료. 진행도는 `localStorage`에 자동 저장합니다.

**주의:** 이것은 정식 PC판의 공식 규칙/AI/multiplayer를 구현한 것이 아닌 **독립된 모바일 간편 게임**입니다. 9개 도구 목록은 PC 게임의 미확정 흉기 리스트가 아닙니다.

## 이미지
`assets/characters/CHR_*.webp`은 확정 원본 PNG를 **구성/디자인 변경 없이 웹 전송용으로만 압축한 파생 파일**입니다. 원본 PNG는 별도로 보존하세요.
