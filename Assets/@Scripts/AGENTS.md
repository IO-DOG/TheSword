<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-08-15 | Updated: 2026-08-20 -->

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
| `Item/` | 문·레버·기둥·물약·룬·떨군 장비 등 타일 상호작용 오브젝트 (see `Item/AGENTS.md`) |
| `Utils/` | `Define`(enum·상수), `Util`, `Extension`, `ChapterTheme`, `MonsterTint` (see `Utils/AGENTS.md`) |
| `Editor/` | 헤드리스 검증·생성·녹화 도구 (`TheSword/` 메뉴) (see `Editor/AGENTS.md`) |
| `Scenes/` | `BaseScene` 파생 씬 스크립트 (Title/Intro/Game/Ending, `DecoJSJ`는 작업용) |
| `Data/` | `Data.Contents.cs`(테이블 Data 클래스 + `ILoader` 구현), `WallData`(ScriptableObject) |
| `Class/` | `CreatureClass` + 전투 특성 `ITrait` 구현 8종(야수·마법·수호·불사·검사·거대·암살·갑옷) + `SplitTrait`/`PotionTrait`/`DefaultTrait`, `EffectFactory` |
| `Inventory/` | `KeyInventory` — 열쇠 보유 상태 |
| `AutoPlay/` | `AutoPlayer` — 1층부터 자동으로 진행하는 검증용 봇 (정체 감지 워치독 포함) |
| `Objects/` | 연출용 오브젝트 (`BossBoom`, `SlimeBlackHole`, `WhiteFlashEffect`, `Effects_00`) |
| `PlayOneShot/` | 단발 사운드/이펙트 재생 헬퍼 |

## For AI Agents

### Working In This Directory
- 시스템 접근은 항상 `Managers.Xxx` 정적 프로퍼티 경유 — 새 싱글톤/`FindObjectOfType` 남발 금지
- 리소스는 `Managers.Resource.Load/Instantiate(key)` — `Resources.Load`·`Addressables` 직접 호출 금지
- enum·레이어·상수는 `Utils/Define.cs`에 모은다 (`Define.Layer`는 실제 Unity 레이어 번호와 일치해야 함)
- UI 노출 문자열은 하드코딩 대신 `Managers.GetString(scriptId)` (Kr/En/Jp/Cn)
- 파일명 = 클래스명, 네임스페이스 없음(전역), 데이터 클래스만 `Data` 네임스페이스

### 이 프로젝트에서 반복된 두 가지 사고
- **이펙트가 로직을 죽인다.** `Managers.Resource.Instantiate`는 없는 키/데이터의 `-`에 null을 준다.
  그대로 `transform`을 만지면 호출한 쪽이 통째로 죽는다 — 무기 FX가 죽으면 그 전투 내내 공격을
  못 하고, 룬 FX가 죽으면 `PickUp`이 끊겨 아이템이 길만 막는다.
  **파티클/이펙트는 반드시 null을 확인하고 없으면 조용히 건너뛴다.**
- **컴포넌트 조회는 두 갈래다.** 여럿인 것(몬스터·아이템)은 `Util.Find<T>`(좁게),
  타일에 하나뿐인 것(문·레버·포탈)은 `Util.FindInTile<T>`(넓게).
  여럿인 것에 넓은 탐색을 쓰면 형제를 집는다 — 슬라임을 밀었는데 옆 칸 늑대와 전투가 열렸다.

### Testing Requirements
- 유닛 테스트 없음. `Editor/`의 헤드리스 도구(`ContentValidator` → `MapBuilderSmokeTest` →
  `ProgressionTest`)와 Unity 에디터 Play 모드로 확인

### Common Patterns
- 새 데이터 테이블: `Data.Contents.cs`에 Data 클래스 + Loader 정의 → `DataManager`에 Dictionary·로드 라인 추가
- 상호작용 오브젝트는 `Define.Layer`의 전용 레이어 + 트리거 콜라이더로 감지
- 전투 특성 id는 `MonsterClassData` 표의 **행 번호와 같아야** 아이콘·배경·무기 애니메이션이 맞는다

## Dependencies

### External
- DOTween(트윈), TextMeshPro, Newtonsoft.Json, Addressables, Cinemachine

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
