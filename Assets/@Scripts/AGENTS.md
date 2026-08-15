<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-08-15 | Updated: 2026-08-15 -->

# @Scripts

## Purpose
게임 코드 전체. 정적 싱글톤 `Managers`를 통해 모든 시스템에 접근하는 Manager Pattern
(Rookiss 스타일). 진입점은 각 씬의 `BaseScene` 파생 클래스 `Awake()`.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Managers/` | 싱글톤 허브 + Core/Contents 매니저 (see `Managers/AGENTS.md`) |
| `Controllers/` | 플레이어·몬스터·보스·카메라·포탈 로직 (see `Controllers/AGENTS.md`) |
| `UI/` | UI_Base 바인딩 기반 팝업/씬 UI (see `UI/AGENTS.md`) |
| `Scenes/` | `BaseScene` 파생 씬 스크립트 (Title/Intro/Game/Ending, `DecoJSJ`는 작업용) |
| `Item/` | 맵 상호작용 오브젝트: `Door`, `BossDoor`, `Lever`, `Pillar`, `ConsumableItem`, `Equip`, `PostPointLight` |
| `Data/` | `Data.Contents.cs`(테이블 Data 클래스 + `ILoader` 구현), `WallData`(ScriptableObject) |
| `Class/` | `CreatureClass` — 크리처 공통 스탯/클래스 정의 |
| `Inventory/` | `KeyInventory` — 열쇠 보유 상태 |
| `Objects/` | 연출용 오브젝트 (`BossBoom`, `SlimeBlackHole`, `WhiteFlashEffect`, `Effects_00`) |
| `PlayOneShot/` | 단발 사운드/이펙트 재생 헬퍼 |
| `Utils/` | `Define`(enum·상수), `Util`(FindChild 등), `Extension` |
| `Editor/` | 비어 있음 (meta만 존재) |

## For AI Agents

### Working In This Directory
- 시스템 접근은 항상 `Managers.Xxx` 정적 프로퍼티 경유 — 새 싱글톤/`FindObjectOfType` 남발 금지
- 리소스는 `Managers.Resource.Load/Instantiate(key)` — `Resources.Load`·`Addressables` 직접 호출 금지
- enum·레이어·상수는 `Utils/Define.cs`에 모은다 (`Define.Layer`는 실제 Unity 레이어 번호와 일치해야 함)
- UI 노출 문자열은 하드코딩 대신 `Managers.GetString(scriptId)` (Kr/En/Jp/Cn)
- 파일명 = 클래스명, 네임스페이스 없음(전역), 데이터 클래스만 `Data` 네임스페이스

### Testing Requirements
- 자동화 테스트 없음. Unity 에디터 Play 모드로 확인

### Common Patterns
- 새 데이터 테이블: `Data.Contents.cs`에 Data 클래스 + Loader 정의 → `DataManager`에 Dictionary·로드 라인 추가
- 상호작용 오브젝트는 `Define.Layer`의 전용 레이어 + 트리거 콜라이더로 감지

## Dependencies

### External
- DOTween(트윈), TextMeshPro, Newtonsoft.Json, Addressables, Cinemachine

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
