<!-- Generated: 2026-08-15 | Updated: 2026-08-20 -->

# TheSword

## Purpose
Unity 6000.0.34f1 (URP) 기반 던전 탐험 게임의 소스 프로젝트. 씬 흐름은
`TitleScene → IntroScene → GameScene → EndingScene`. 던전은 100층이고 20층마다 챕터(테마)가 바뀐다.
CLI 빌드 스크립트는 없으며 Unity 에디터에서 열어 작업한다. 검증은 `Assets/@Scripts/Editor/`의
헤드리스 도구와 `Tools/`의 파이썬 시뮬레이터가 맡는다. 아키텍처·데이터 파이프라인 상세는 루트 `CLAUDE.md` 참조.

## Key Files
| File | Description |
|------|-------------|
| `CLAUDE.md` | 아키텍처(Manager Pattern), Addressables 로딩, 전투 특성·룬·챕터 테마, 데이터 파이프라인 정리 |
| `Packages/manifest.json` | Unity 패키지 의존성 (Addressables, Cinemachine, URP, Newtonsoft.Json 등) |
| `ProjectSettings/ProjectVersion.txt` | Unity 에디터 버전 (6000.0.34f1) |
| `TheSword.sln` / `*.csproj` | Unity가 생성 — 손으로 고치지 않는다 |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Assets/` | 게임 콘텐츠 전체. `@` 접두사 폴더만 자체 제작물 (see `Assets/AGENTS.md`) |
| `Tools/` | 5~100층 콘텐츠 생성기 + 밸런스 시뮬레이터 (파이썬, Unity 불필요) (see `Tools/AGENTS.md`) |
| `Packages/` | Unity Package Manager 매니페스트 |
| `ProjectSettings/` | Unity 프로젝트 설정 (에디터에서만 수정) |
| `Documents/` | 기획서 원본 (`(구)기획서.pptx`) |
| `Recordings/` | `PlaythroughRecorder` 산출 영상 — 커밋 대상 아님 |
| `Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/` | Unity 생성물 — 건드리지 않는다 |

## For AI Agents

### Working In This Directory
- 코드 작업은 전부 `Assets/@Scripts/` 하위에서 이루어진다
- 콘텐츠(층 레이아웃·몬스터 스탯·레벨 테이블)는 **손으로 고치지 않고 `Tools/`에서 생성**한다
- `.meta` 파일은 Unity가 생성/관리 — 대응 에셋과 함께 이동/삭제할 것, 단독으로 건드리지 말 것
- 커밋 메시지는 한국어 + `[ADD]`/`[FIX]` 태그 컨벤션

### Testing Requirements
- 자동화 유닛 테스트는 없다. 세 겹으로 확인한다:
  1. `cd Tools && python generate_content.py` — 밸런스·완주 시뮬레이션 (Unity 불필요, 가장 빠름)
  2. `Unity.exe -projectPath . -batchmode -quit -executeMethod ContentValidator.Validate` — 산출물 정합성
  3. Unity 에디터 Play 모드 또는 `PlaythroughRecorder.DryRun` — 실제 진행
- 완주 검증은 세이브를 건드린다. 분리 실행이 안 되고 시간이 오래 걸린다 —
  돌리기 전에 세이브 초기화 여부를 확인할 것

## Dependencies

### External
- Addressables 2.2.2 — 모든 런타임 리소스 로딩
- Newtonsoft.Json — 데이터 테이블/세이브 직렬화
- URP 17.0.3, Cinemachine 2.10.3, DOTween(Assets/Plugins), TextMeshPro
- Unity Recorder — `PlaythroughRecorder`만 사용

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
