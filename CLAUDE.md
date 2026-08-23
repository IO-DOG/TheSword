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

### 전투 특성 (기획서 13·53·71쪽)

몬스터는 특성 8종(야수·마법·수호·불사·검사·거대·암살·갑옷) 중 하나를 갖는다.
패시브는 `CreatureClass` 의 `ITrait` 구현이고, 전투창의 아이콘·배경·무기 애니메이션은
`MonsterClassData` 가 특성 id 로 물어온다 — **특성 id 는 그 표의 행 번호와 같아야 한다.**

챕터마다 성격이 다르다 (도입 / 방어 / 화력 / 치명타 / 총복습).
"몬스터 하나하나를 외우는 게 아니라 챕터마다 통하는 수를 찾는다" 는 기획 의도라,
특성 배분은 `generate_content.py` 의 `CHAPTER_TRAITS` 한 곳에서만 정한다.

밸런스를 건드릴 때 **특성을 켠 채로 역산해야 한다**. 암살은 치명타가 아닌 공격을
전부 회피하고 불사는 80% 를 흘리므로, 특성을 끄고 뽑은 수치는 실제와 몇 배씩 어긋난다.
`solve_monster` 가 특성을 받아 이분 탐색하고, `thesword_balance.py` 의 `_self_check()`
가 여덟 특성이 기획서대로 도는지 확인한다.

### 룬 (기획서 65·81쪽)

층마다 하나씩, 공격력/방어력/체력을 돌려가며 놓는다(`ConsumableItemData` 9·10·11).
**계단 앞 구역에 둔다** — 반드시 지나는 자리라 성장이 결정적이고, 그래야 완주 보장을
계산할 수 있다. F층의 룬은 계단 앞에 있으니 효과는 F+1층부터다(`rune_bonus`).
판단거리는 물약 타이밍이 맡는다.

### 몬스터 그림

쓸 수 있는 애니메이션은 `Mob_C0_I000~I009` (대기)와 `Mob_C0_A000~A007`, `A009` (공격).
**`Boss_C0_*` 는 킹 슬라임과 분열 3종 연출 전용이다 — 생성 층에 내보내지 않는다.**
그래서 우두머리는 그림이 아니라 **덩치**(`MapBuilder.MonsterBulk`: 정예 1.2, 보스 1.45)와
색으로 구분한다.

같은 그림을 색으로 갈라 쓴다(`MonsterTint`): 색조=챕터, 진하기=층 안 서열,
그리고 **다섯 층마다 색 갈래**가 바뀐다(한 챕터 20층이 같은 색이면 같은 놈으로 보인다).

시트를 추가하면 클립을 손으로 만들지 말고:

```bash
Unity.exe -projectPath . -executeMethod MonsterArtSetup.Build
```

`MonsterArtSetup.Wanted()` 에 한 줄 넣으면 클립 생성 → 두 컨트롤러(맵/전투창) 등록까지
한다. 잘려 있지 않은 시트는 `NeedSlicing` 에 넣으면 가로로 균등 분할해 준다.
떨군 장비가 재생하는 `EquipItem_{id}` 상태도 같은 도구가 채운다 —
**없는 상태를 재생하면 경고만 찍히고 그림이 안 나온다. 즉 떨어진 장비가 보이지 않는다.**

### 에디터가 바로 안 켜질 때

두 가지 창이 뜬다. 둘 다 **지난번에 에디터가 제대로 안 닫혔다는 표시**지 고장이 아니다.

| 창 | 원인 | 대처 |
|---|---|---|
| `Recovering Scene Backups` | `Temp/__Backupscenes` 에 씬 백업이 남았다 | 그 폴더를 지우면 안 뜬다 |
| `Enter Safe Mode?` | 스크립트 컴파일 실패, 또는 `Temp` 가 반쯤 쓰인 채 남았다 | 컴파일 에러부터 확인, 없으면 `Temp` 통째로 삭제 |

```bash
python Tools/clean_editor_state.py    # 에디터가 꺼져 있을 때만 동작한다
```

복구 창에서 **Yes 를 누를 때마다 `Assets/_Recovery` 에 복사본이 하나씩 쌓인다.**
놀이에도 빌드에도 쓰이지 않는 파일이라 `.gitignore` 에 넣어 두었다.

자동 검증(`PlaythroughRecorder`)은 나가기 전에 씬 백업을 스스로 치운다. 그런데도
창이 뜬다면 그 실행이 **중간에 강제 종료됐다는 뜻**이다 — 로그의 마지막 층수를 보면
게임이 죽은 것인지 실행이 잘린 것인지 갈린다.

**런타임 코드에 `using UnityEditor;` 를 남기지 말 것.** 에디터에서는 컴파일되지만
빌드에서 깨지고, 에디터 참조 없이 컴파일되는 상황에서 Safe Mode 로 떨어진다.

### 문 배치 규칙

문은 가로문과 세로문 두 가지고, **놓을 수 있는 자리가 정해져 있다.**

| 셀 코드 | 종류 | 조건 | 지나가는 방향 |
|---|---|---|---|
| 3 / 4 / 5 | 가로문 | 문의 **좌우가 벽** | 세로로 통과 |
| 6 / 7 / 8 | 세로문 | 문의 **위아래가 벽** | 가로로 통과 |

색은 3 으로 나눈 나머지다 — 3·6 초록, 4·7 노랑, 5·8 빨강.
(`MapBuilder.BuildDoor` 가 `(id - 3) % 3` 으로 열쇠 색을 정한다. 예전에는
`Clamp(id-3)` 이라 6·7·8 이 전부 빨강을 달라고 했다.)

방향이 자리와 어긋나면 벽이 없는 쪽으로 문틀이 떠 있게 되고, 옆으로 돌아갈 수
있는 것처럼 보인다. 그래서 **통로 방향이 문 방향을 정한다** — 좌우로 뻗은 통로에는
세로문, 위아래로 뻗은 통로에는 가로문.

`layout_gen.check_doors()` 가 모든 문을 훑어 규칙 위반을 잡는다. 레이아웃을
바꿨으면 이 검사가 0 인지 확인할 것.

### 챕터 테마 (기획서 12·118~123쪽)

"현재 시스템을 유지 ▶ 환경을 변화". 20층마다 시간대(해의 각도·세기·색)와 안개가 바뀐다
(`ChapterTheme.Apply`). 벽 아트가 챕터 00 세트뿐이라 색만 바꾸면 필터를 씌운 것으로
보이는데, 그림자 방향과 안개가 같이 바뀌면 다른 장소로 읽힌다.

### 장식은 0.1 로 놓는다

층 장식은 맵 데이터가 아니라 층마다 짝지은 별도 프리팹이다 (`Deco_CC_FFF`).
미술을 바꿔도 밸런스·경로 검증을 다시 돌릴 필요가 없고, 손수 만든 1~4층이 이미
그 규칙을 쓰고 있다.

```bash
Unity.exe -projectPath . -executeMethod MapDecoSetup.Build
```

**타일 아트는 한 칸을 3.2 유닛으로 그려 놓았고, 맵은 0.32 간격으로 조립한다.**
그래서 벽 프리팹(`Tilemap_C00_W02`)이 0.1 로 놓여 있다 — 같은 팩에서 나온 장식도
같은 값이어야 한 칸에 맞는다. 1 로 두면 이렇게 된다.

| | 1.0 (틀림) | 0.1 (맞음) |
|---|---|---|
| 사슬 | 2.2 x 6.2 칸 | 0.2 x 0.6 칸 |
| 화로 | 5.9 x 5.9 칸 | 0.6 x 0.6 칸 |

높이도 같이 맞는다. 벽은 y 0.2~0.5 를 차지하고, 이 팩의 장식은 y 2.0 부터
그려져 있어서 0.1 을 곱하면 벽면 아래쪽에 정확히 걸린다. 0.3 으로 두면 벽 위로 뜬다.

**불은 층당 넷다섯 개다.** 횃불마다 점광원을 달았더니 층마다 서른 개가 됐고,
같은 100층 녹화가 31분에서 57분으로 늘었다 (URP 포워드 렌더링에서 점광원은 화면에
몇 개가 겹치느냐로 값을 치른다). 절반으로 줄여 열다섯 개일 때도 여전히 느렸고,
무엇보다 그렇게 많으면 불이 장식이 아니라 배경이 된다. 사슬 같은 소품은 광원이
없어 싸므로 스무 개쯤 걸어도 된다.

### 이펙트가 없어도 게임은 굴러가야 한다

이 프로젝트에서 같은 사고가 세 번 났다. `Managers.Resource.Instantiate` 가 null 을
주는데(어드레서블에 없는 키, 데이터의 `-`) 그대로 `transform` 을 만져 **호출한 쪽이
통째로 죽는** 것이다.

- 무기 FX → 공격 코루틴이 죽어 그 전투 내내 한 번도 공격하지 못한다
- 룬 FX → `PickUp` 이 끊겨 아이템이 안 꺼지고, 줍지도 못하는데 길만 막는다

**파티클/이펙트를 만들면 반드시 null 을 확인하고, 없으면 조용히 건너뛴다.**

### 상호작용 컴포넌트 조회

콜라이더가 붙은 오브젝트에 스크립트가 없는 경우가 많다. 두 가지를 구분해서 쓴다.

| | 범위 | 대상 |
|---|---|---|
| `Util.Find<T>` | 자신 → 자식 → 조상 자신 | 몬스터·아이템처럼 **여럿인 것** |
| `Util.FindInTile<T>` | 부모로 올라가며 그 아래 전체 | 문·레버·포탈처럼 **타일에 하나뿐인 것** |

여럿인 것에 넓은 탐색을 쓰면 형제를 집는다 — 실제로 슬라임을 밀었는데 옆 칸 늑대와
전투가 열렸다.

**콜라이더는 루트가 아니라 자식에 붙어 있다.** 그래서 `GetComponent<BoxCollider>()`
는 조용히 null 을 주고, `Destroy(null)` 은 아무 일도 하지 않는다. 죽는 연출용
복사본(`UI_MonsterCard.CoDead` 가 몬스터를 통째로 복사한다)의 콜라이더를 그렇게
지우려다 실패해서, 복사본이 1초 동안 살아 있는 몬스터로 남았고 그 칸을 다시 밟으면
**같은 보스와 두 번 싸웠다**. 콜라이더를 없앨 때는 `GetComponentsInChildren<Collider>`
로 훑는다.

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

**막다른 골방** — 방 가장자리에서 밖으로 두 칸을 판다(`_carve_alcove`). 입구가 한
칸뿐이라 그 칸을 막으면 안쪽에 갈 수 없고, 그래서 입구에 선 몬스터가 진짜 파수꾼이
된다. 열린 방에서는 아무리 잘 놓아도 돌아서 접근할 수 있어 "잡아야 얻는다" 가
성립하지 않았다. **세로로만 판다** — 가로 간격이 2칸이라 안쪽이 옆방과 맞닿는다.

골방 안의 덤은 **완주 계산에 넣지 않는다.** 안 잡고 지나갈 수 있는 것을 셈에 넣으면
완주 보장이 거짓말이 된다. 파수꾼도 그 층 다섯 마리 중 하나를 옮겨 세운 것이지
새로 늘린 것이 아니다.

### 1~4층은 손대지 않는다

`Dungeon_00_000` ~ `00_003` 은 **손수 만든 도입부**다 (튜토리얼 → 마검 계약 → 킹슬라임).
`DirectingManager` 가 그 층의 특정 오브젝트를 **이름으로 직접** 찾기 때문에
(`Items/CItem13`, `SpawnKingSlime`, `YellowSlimePos` …), 레이아웃을 새로 생성하면
인트로가 NullReference 로 통째로 깨진다. 생성기는 5층부터만 만든다.

목록이 세 곳에 있고 **전부 같아야 한다**:
`Tools/generate_content.py` 의 `HANDMADE_FLOORS`, `MapBuilder.IsHandAuthored`,
`Tools/validate_content.py` 의 `HAND_AUTHORED`.

5층의 목표 레벨은 고정값이 아니라 1~4층을 원본 데이터로 실제 전투 시뮬레이션해서 얻는다
(`simulate_handmade`). 현재 4층 종료 시 Lv14 → 5층 목표 Lv15 → 100층 Lv105.

이 값은 **아래로 치우친 추정**이다. 시뮬레이터는 "약한 놈부터 필요한 만큼"만 잡는데
실제 플레이는 그 층의 몬스터를 대체로 다 잡기 때문에, 자동 검증에서는 5층 진입이
Lv17 로 관측됐다 (예측보다 3 높다). 완주 판정이 그만큼 보수적이라는 뜻이라 위험하지는
않지만, **도입부 난이도를 손볼 때는 예측이 아니라 실측 레벨을 봐야 한다.**

### 맵 생성 방식

`GameManager.GenerateMap()` 은 1~4층만 프리팹을 쓰고, 나머지 96개 층은
**CSV(MapData)로 런타임 조립**한다 (`MapBuilder.Build`). 서로 폴백이 걸려 있다.

- 타일 프리팹의 `PortalController` / `Door` 는 **루트가 아니라 자식**에 붙어 있다.
  `GetOrAddComponent` 로 루트에 새로 붙이면 설정 안 된 원본이 남아 계단이 죽는다 →
  `MapBuilder.Components<T>()` 로 **전부** 설정할 것.
- 보스는 `MonsterActiveDic` 을 일반 몬스터와 **공유**한다. 전투 결말이
  `UI_MonsterCard.Dead()` 하나뿐이고 거기서 항상 그 딕셔너리에 쓰기 때문이다.
  보스에게 별도 인덱스를 주면 엉뚱한 층의 몬스터가 죽은 것으로 기록된다.
- **방금 잡은 놈과 다시 싸우지 않게 표시한다.** 보통 몬스터는 죽으면 곧바로 꺼져서
  "꺼진 것과는 싸우지 않는다" 로 충분하지만, 보스는 죽고 나서도 연출(폭발 → 하얗게 →
  빛)이 끝날 때까지 몇 초 켜져 있다. 그 사이에 다시 부딪히면 **같은 보스와 두 번
  싸우고 경험치와 보상을 두 번 받는다** — 60층·80층에서 실제로 났다.
  `UI_MonsterCard.Dead()` 가 `MonsterController.MarkDead()` 로 그 자리를 표시하고,
  `SetMonster` 가 그것을 먼저 본다.
  **`MonsterActiveDic` 으로 검사하면 안 된다** — 1~4층의 프리팹 몬스터는 그 인덱스를
  나눠 쓰기 때문에(`MapBuilder` 만 인덱스를 부여한다), 하나가 죽으면 킹 슬라임까지
  막혀 4층에서 게임이 멈춘다. 실제로 그렇게 한 번 막았다.
- 챕터 분위기는 `MapBuilder` 의 타일 틴트 + `DirectionalLight` 색 + BGM 으로 낸다.
  **벽 아트와 BGM 은 챕터 00 세트만 실재한다** — 챕터 1~4 는 틴트/조명으로만 구분된다.
  음악을 추가하면 `StageInfoData` 의 BGM 열(`BGM_100` …)에 맞춰 어드레서블만 등록하면
  `GameManager.PlayChapterBGM` 이 자동으로 집어간다.

### 데이터 파이프라인

- **이름은 `ScriptData` 가 이긴다.** `MonsterData.Name` 은 화면에 안 쓰인다 —
  UI 는 `MonsterNameId` 로 `ScriptData` 를 찾는다. 생성기의 `emit_scripts` 는 손으로 쓴
  번역을 지키려고 **이미 있는 ID 를 건너뛰었는데**, 그 바람에 생성한 몬스터의 이름이
  옛 문자열로 남았다: 데이터는 "…잿빛 파수꾼 우두머리" 인데 화면은 "…킹 슬라임" 이었고,
  일반 몹도 슬라임이 늑대로 불렸다. 그림에 맞춰 이름을 고쳐도 화면은 안 바뀐다는 뜻이다.
  지금은 **생성 구간(5100~, 10900~, 11000~, 20900~, 21000~)만 덮어쓴다.**
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
