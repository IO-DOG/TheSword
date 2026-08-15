<!-- Generated: 2026-08-15 | Updated: 2026-08-15 -->

# TheSword

## Purpose
Unity 6000.0.34f1 (URP) 기반 던전 탐험 게임의 소스 프로젝트. 씬 흐름은
`TitleScene → IntroScene → GameScene → EndingScene`. CLI 빌드/테스트 스크립트는 없으며
Unity 에디터에서 열어 작업한다. 아키텍처·데이터 파이프라인 상세는 루트 `CLAUDE.md` 참조.

## Key Files
| File | Description |
|------|-------------|
| `CLAUDE.md` | 아키텍처(Manager Pattern), Addressables 로딩, 데이터 파이프라인, UI 컨벤션 정리 |
| `Packages/manifest.json` | Unity 패키지 의존성 (Addressables, Cinemachine, URP, Newtonsoft.Json 등) |
| `ProjectSettings/ProjectVersion.txt` | Unity 에디터 버전 (6000.0.34f1) |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Assets/` | 게임 콘텐츠 전체. `@` 접두사 폴더만 자체 제작물 (see `Assets/AGENTS.md`) |
| `Packages/` | Unity Package Manager 매니페스트 |
| `ProjectSettings/` | Unity 프로젝트 설정 (에디터에서만 수정) |

## For AI Agents

### Working In This Directory
- 코드 작업은 전부 `Assets/@Scripts/` 하위에서 이루어진다
- `.meta` 파일은 Unity가 생성/관리 — 대응 에셋과 함께 이동/삭제할 것, 단독으로 건드리지 말 것
- 커밋 메시지는 한국어 + `[ADD]`/`[FIX]` 태그 컨벤션

### Testing Requirements
- 자동화 테스트 없음. Unity 에디터에서 Play 모드로 확인

## Dependencies

### External
- Addressables 2.2.2 — 모든 런타임 리소스 로딩
- Newtonsoft.Json — 데이터 테이블/세이브 직렬화
- URP 17.0.3, Cinemachine 2.10.3, DOTween(Assets/Plugins), TextMeshPro

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
