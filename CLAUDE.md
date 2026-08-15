# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

**TheSword** — Unity **6000.0.34f1** (URP) 기반 던전 탐험 게임 소스 프로젝트.
CLI 빌드/테스트 스크립트는 없다. Unity 에디터에서 열어 작업하며, 씬은 `Assets/@Scenes/`에 있다
(진입: `TitleScene` → `IntroScene` → `GameScene` → `EndingScene`).

`@` 접두사 폴더(`@Scripts`, `@Resources`, `@Scenes`)만 이 프로젝트의 자체 콘텐츠다.
`Assets/` 하위의 나머지(BroAudio, Febucci Text Animator, DOTween, Retro Arsenal, UniGLTF 등)는 서드파티 에셋이므로 수정하지 않는다.

커밋 메시지는 한국어 + `[ADD]`/`[FIX]` 태그 컨벤션.

## 아키텍처 (Manager Pattern)

모든 코드는 `Assets/@Scripts/` 하위. 중심은 정적 싱글톤 `Managers` (`Managers.cs`):
`@Managers` GameObject에 붙어 최초 접근 시 자동 생성되고 `DontDestroyOnLoad`로 유지된다.
어디서든 `Managers.Xxx`로 접근한다.

- **Core**: `Data`(테이블/세이브), `Resource`(Addressables 로드+캐시), `UI`(팝업 스택), `Scene`, `Sound`, `Pool`, `Input`, `Cursor`
- **Contents**: `Game`(게임 상태 전반), `Directing`(연출), `Event`, `Object`(플레이어/몬스터 등 오브젝트 스폰·추적)

그 외 폴더: `Controllers/`(플레이어·몬스터·보스 로직), `Item/`(문·레버·기둥 등 상호작용 오브젝트), `Scenes/`(씬별 `BaseScene` 파생), `UI/`, `Utils/`(`Define`, `Util`, `Extension`).

### 리소스 로딩 (Addressables)

직접 `Resources.Load`나 `Addressables`를 호출하지 않는다. 흐름:

1. `UI_TitleScene`이 Addressables 라벨 **"PreLoad"** 전체를 `Managers.Resource.LoadAllAsync`로 선로드 → 내부 딕셔너리 캐시
2. 이후 코드는 동기 API 사용: `Managers.Resource.Load<T>(key)`, `Managers.Resource.Instantiate(key)`
3. 스프라이트 키는 `.sprite` 접미사가 붙는다 (Load가 자동 보정 시도)

**새 프리팹/에셋은 Addressables에 등록하고 "PreLoad" 라벨을 붙여야 런타임에 로드된다.**

### 100층 콘텐츠 생성 (`Tools/`)

던전 100층(20층마다 테마가 바뀌는 챕터 00~04)은 손으로 만들지 않고 **파이썬 생성기**로 만든다.
밸런스는 Unity 전투 공식을 그대로 옮긴 시뮬레이터로 검증한다 — 통과하지 못하면 데이터를 쓰지 않는다.

```bash
cd Tools
python generate_content.py            # dry-run: 밸런스 + 도달 가능성만 검사
python generate_content.py --write    # 실제 데이터/레이아웃 출력
python validate_content.py            # 산출물 검증 (Unity 없이 실행 가능)
```

| 파일 | 역할 |
|------|------|
| `thesword_balance.py` | 전투 시뮬레이터 + 레벨 테이블 (C# 전투 코드 1:1 이식) |
| `generate_content.py` | 층별 몬스터 스탯 역산 → 100층 완주 시뮬레이션 → 데이터 출력 |
| `layout_gen.py` | 미로 레이아웃. 방0-[초록문]-방1-[노랑문]-방2-[빨강문]-방3(계단/보스) |
| `mapdata_gen.py` | CSV → 런타임 `MapData.json` (C# 파서와 카운터 순서가 동일해야 함) |
| `validate_content.py` | 계단 쌍·문/열쇠 수·ID 유효성·프리팹 존재 검사 |

설계 규칙: 층의 몹을 다 잡으면 정확히 1레벨. 각 방에 다음 문을 여는 열쇠를 둬서
**몬스터를 잡는 순서가 강제**된다.

### 1~4층은 손대지 않는다

`Dungeon_00_000` ~ `00_003` 은 **손수 만든 도입부**다 (튜토리얼 → 마검 계약 → 킹슬라임).
`DirectingManager` 가 그 층의 특정 오브젝트를 **이름으로 직접** 찾기 때문에
(`Items/CItem13`, `SpawnKingSlime`, `YellowSlimePos` …), 레이아웃을 새로 생성하면
인트로가 NullReference 로 통째로 깨진다. 생성기는 5층부터만 만든다.

목록이 세 곳에 있고 **전부 같아야 한다**:
`Tools/generate_content.py` 의 `HANDMADE_FLOORS`, `MapBuilder.IsHandAuthored`,
`Tools/validate_content.py` 의 `HAND_AUTHORED`.

5층의 목표 레벨은 고정값이 아니라 1~4층을 원본 데이터로 실제 전투 시뮬레이션해서 얻는다
(`simulate_handmade`). 현재 4층 종료 시 Lv8 → 5층 목표 Lv9 → 100층 Lv105.

### 맵 생성 방식

`GameManager.GenerateMap()` 은 1~4층만 프리팹을 쓰고, 나머지 96개 층은
**CSV(MapData)로 런타임 조립**한다 (`MapBuilder.Build`). 서로 폴백이 걸려 있다.

- 타일 프리팹의 `PortalController` / `Door` 는 **루트가 아니라 자식**에 붙어 있다.
  `GetOrAddComponent` 로 루트에 새로 붙이면 설정 안 된 원본이 남아 계단이 죽는다 →
  `MapBuilder.Components<T>()` 로 **전부** 설정할 것.
- 보스는 `MonsterActiveDic` 을 일반 몬스터와 **공유**한다. 전투 결말이
  `UI_MonsterCard.Dead()` 하나뿐이고 거기서 항상 그 딕셔너리에 쓰기 때문이다.
  보스에게 별도 인덱스를 주면 엉뚱한 층의 몬스터가 죽은 것으로 기록된다.
- 챕터 분위기는 `MapBuilder` 의 타일 틴트 + `DirectionalLight` 색 + BGM 으로 낸다.
  **벽 아트와 BGM 은 챕터 00 세트만 실재한다** — 챕터 1~4 는 틴트/조명으로만 구분된다.
  음악을 추가하면 `StageInfoData` 의 BGM 열(`BGM_100` …)에 맞춰 어드레서블만 등록하면
  `GameManager.PlayChapterBGM` 이 자동으로 집어간다.

### 데이터 파이프라인

- 테이블 원본: `Assets/@Resources/Data/Excel/*.csv` → 변환된 `Assets/@Resources/Data/JsonData/*.json`(Addressable TextAsset)을 `DataManager.Init()`이 Newtonsoft.Json + `ILoader<Key,Value>` 패턴으로 로드. 새 테이블 추가 시 `Data.Contents.cs`에 Data 클래스+Loader 정의 후 `DataManager`에 딕셔너리·로드 라인 추가.
- 던전 맵: `Assets/StreamingAssets/Data/Excel/Dungeon_*.csv` 그리드를 `DataManager.ResetActiveDic()`이 파싱해 `MapData.json` 생성. 셀 코드: `I`=소비 아이템, `E`=장비, `M`=몬스터, `B`=보스, `W`=벽, 숫자 3~8=문, 11=스폰 지점, 12=레버, 13=기둥, 14~16=포탈.
- 세이브: 오브젝트별 활성화 상태 딕셔너리들을 `Application.persistentDataPath/*.json`으로 저장/로드 (`UpdateActiveDic`/`LoadActiveDic`).

### UI 컨벤션

`UI_Base` 파생, 이름은 `UI_*`. 팝업은 `UI_Popup`(UIManager 스택으로 `ShowPopupUI`/`ClosePopupUI`), 씬 UI는 `UI_Scene`(`ShowSceneUI`).

- 자식 위젯은 클래스 내 `enum`(Buttons, Texts, GameObjects…)으로 선언하고 `Init()`에서 `BindButton(typeof(Buttons))` 등으로 바인딩 — **enum 이름이 하이어라키의 자식 GameObject 이름과 정확히 일치해야 한다**
- 접근은 `GetButton((int)Buttons.Xxx)`, 이벤트는 `BindEvent(go, action, type)`
- 텍스트는 TextMeshPro (`TMP_Text`)

### 로컬라이제이션

UI에 노출되는 문자열은 하드코딩하지 않고 `ScriptData` 테이블 ID로 `Managers.GetString(id)` 호출 (Kr/En/Jp/Cn).
데이터 시트 이스케이프: `\n`=줄바꿈, `^`=쉼표.
