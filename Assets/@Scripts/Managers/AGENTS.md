<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-08-15 | Updated: 2026-08-20 -->

# Managers

## Purpose
게임 전역 시스템. `Managers.cs`가 `@Managers` GameObject에 붙는 정적 싱글톤 허브로,
최초 접근 시 자동 생성되고 `DontDestroyOnLoad`로 유지된다. `Core/`는 인프라, `Contents/`는 게임 로직.

## Key Files
| File | Description |
|------|-------------|
| `Managers.cs` | 싱글톤 허브. 모든 매니저의 정적 프로퍼티 + 로컬라이즈 문자열 `GetString(id)` |
| `Core/DataManager.cs` | JSON 테이블 로드(`ILoader` 패턴), 던전 CSV→MapData 변환(`ResetActiveDic`), persistentDataPath 세이브 |
| `Core/ResourceManager.cs` | Addressables 로드+캐시. "PreLoad" 라벨 일괄 선로드(`LoadAllAsync`) 후 동기 `Load`/`Instantiate` |
| `Core/UIManager.cs` | 팝업 스택(`ShowPopupUI`/`ClosePopupUI`)과 씬 UI(`ShowSceneUI`) 관리 |
| `Core/SceneManagerEx.cs` | `Define.Scene` 기반 씬 전환 |
| `Core/SoundManager.cs` | BGM/SubBgm/Effect 채널 재생 |
| `Core/PoolManager.cs` | 프리팹 오브젝트 풀 |
| `Core/InputManager.cs` | `Managers.Update()`에서 폴링되는 입력 처리 |
| `Core/CursorManager.cs` | `@Cursor` GameObject의 커스텀 커서 (MonoBehaviour, `Managers.Cursor` 정적 필드) |
| `Contents/GameManager.cs` | 게임 상태 중심부: 스테이지 진행, 페이드 액션, 언어 설정, 세이브 리셋 등 |
| `Contents/ObjectManager.cs` | MapData 기반 플레이어/몬스터/아이템 스폰·추적 |
| `Contents/DirectingManager.cs` | 컷신·연출 시퀀스 (내부에 `Events` MonoBehaviour 포함) |
| `Contents/EventManager.cs` | `EventData` 테이블 기반 게임 이벤트 트리거 |
| `Contents/CoroutineManager.cs` | 비-MonoBehaviour에서 코루틴 실행 대행 |
| `Contents/MapBuilder.cs` | MapData(CSV 파생)로 던전 한 층을 런타임 조립. 96개 층이 이 경로 (1~4층만 프리팹) |

## For AI Agents

### Working In This Directory
- 새 매니저 추가 시: 클래스 작성 → `Managers.cs`에 필드+정적 프로퍼티 추가 (Core/Contents region 구분 유지)
- 매니저 대부분은 순수 C# 클래스 — Unity 생명주기가 필요하면 `CoroutineManager`나 `Managers.Update()` 경유
- 씬 전환 시 `Managers.Clear()`가 Sound/Scene/UI/Pool을 정리한다 — 상태를 들고 있는 매니저는 Clear 누락 여부 확인

### Common Patterns
- 테이블 접근: `Managers.Data.XxxDic[id]` — 로드는 타이틀 씬 PreLoad 완료 후 `Data.Init()`에서만
- 세이브 갱신: 활성화 딕셔너리 수정 후 `Managers.Data.UpdateActiveDic()` 호출

### 주의
- **매니저 클래스는 MonoBehaviour를 상속하면 안 된다.** `Managers`가 `new`로 생성하는데
  Unity는 MonoBehaviour의 `new`를 거부해 인스턴스가 null이 된다 (과거 `EventManager`가 이 버그였다).
  Unity 기능이 필요하면 `@Managers` GameObject에 컴포넌트로 붙이고 지연 조회할 것 —
  `DirectingManager.Events`가 그 방식이다.

### MapBuilder 주의
- 타일 프리팹의 `PortalController`/`Door`는 **루트가 아니라 자식**에 붙어 있다.
  `GetOrAddComponent`로 루트에 새로 붙이면 설정 안 된 원본이 남아 계단이 죽는다 →
  `MapBuilder.Components<T>()`로 **전부** 설정할 것
- 보스는 `MonsterActiveDic`을 일반 몬스터와 **공유**한다. 전투 결말이 `UI_MonsterCard.Dead()`
  하나뿐이고 거기서 항상 그 딕셔너리에 쓰기 때문이다. 별도 인덱스를 주면 엉뚱한 층의
  몬스터가 죽은 것으로 기록된다
- 1~4층은 손수 만든 도입부라 `MapBuilder.IsHandAuthored`가 건너뛴다.
  `DirectingManager`가 그 층의 오브젝트를 **이름으로 직접** 찾으므로(`Items/CItem13`,
  `SpawnKingSlime`, `YellowSlimePos` …) 레이아웃을 새로 생성하면 인트로가 통째로 깨진다
- 우두머리는 그림이 아니라 **덩치**(`MonsterBulk`: 정예 1.2, 보스 1.45)와 색으로 구분한다.
  `Boss_C0_*` 애니메이션은 킹슬라임/분열 전용 — 생성 층에 내보내지 않는다
- 챕터 분위기는 타일 틴트 + `DirectionalLight` 색 + BGM. **벽 아트와 BGM은 챕터 00 세트만 실재**한다.
  음악을 추가하면 `StageInfoData`의 BGM 열에 맞춰 어드레서블만 등록하면 `GameManager.PlayChapterBGM`이 집어간다

## Dependencies

### Internal
- `Utils/Define.cs` — Scene/Sound/Layer 등 모든 enum
- `Data/Data.Contents.cs` — 테이블 Data 클래스·Loader

### External
- Addressables, Newtonsoft.Json

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
