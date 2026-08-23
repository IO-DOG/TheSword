<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-08-20 | Updated: 2026-08-20 -->

# Tools

## Purpose
던전 5~100층의 **콘텐츠 생성기**(파이썬). 층 레이아웃·몬스터 스탯·레벨 테이블·스테이지 정보를
손으로 만들지 않고 여기서 뽑는다. 밸런스는 Unity 전투 공식을 1:1로 옮긴 시뮬레이터로 검증하며,
**검증을 통과하지 못하면 데이터를 쓰지 않는다**. Unity 없이 전부 실행된다.

```bash
cd Tools
python generate_content.py            # dry-run: 밸런스 + 도달 가능성만 검사
python generate_content.py --write    # 실제 데이터/레이아웃 출력
python validate_content.py            # 산출물 검증
python route_check.py                 # 정답 경로 유일성 + 허용 실수 횟수
python tune_difficulty.py --strict    # 난이도 손잡이 자동 조정
```

## Key Files
| File | Description |
|------|-------------|
| `thesword_balance.py` | 전투 시뮬레이터 + 레벨 곡선. `UI_BaseCard`/`UI_MonsterCard`/`CreatureClass` 이식. `_self_check()`가 특성 8종 동작을 확인 |
| `generate_content.py` | 층별 몬스터 스탯 역산 → 100층 완주 시뮬레이션 → CSV/JSON 출력. `CHAPTER_TRAITS`·`HANDMADE_FLOORS`가 여기 있다 |
| `layout_gen.py` | 층 레이아웃(매직 타워식). 방0-[초록문]-방1-[노랑문]-방2-[빨강문]-방3(계단/보스) |
| `mapdata_gen.py` | `Dungeon_*.csv` 100장 → 런타임 `MapData.json`. 파싱 순서가 `DataManager.ResetActiveDic()`과 같아야 한다 |
| `route_check.py` | 정답 경로가 하나뿐인지, 물약 실수를 몇 번까지 봐주는지 측정 |
| `tune_difficulty.py` | 층당 몹 손실(`MOB_HP_LOSS`) 하나만 움직여 난이도를 맞춘다 |
| `validate_content.py` | 계단 쌍·문/열쇠 수·ID 유효성·프리팹 존재 검사 (`Editor/ContentValidator.cs`의 파이썬 판) |

## 산출물 (덮어쓰는 파일)
| 경로 | 내용 |
|------|------|
| `Assets/@Resources/Data/Excel/*.csv` | 테이블 원본 (Player/Monster/StageInfo/Script) |
| `Assets/@Resources/Data/JsonData/*.json` | **런타임이 실제로 읽는 파일** |
| `Assets/StreamingAssets/Data/Excel/Dungeon_CC_FFF.csv` | 층 레이아웃 100장 |

## For AI Agents

### Working In This Directory
- **밸런스를 건드릴 때는 특성을 켠 채로 역산한다.** 암살은 치명타가 아닌 공격을 전부 회피하고
  불사는 80%를 흘린다 — 특성을 끄고 뽑은 수치는 실제와 몇 배씩 어긋난다
- 특성 배분은 `generate_content.py`의 `CHAPTER_TRAITS` **한 곳에서만** 정한다
- **1~4층은 생성하지 않는다.** 손수 만든 도입부라 `DirectingManager`가 오브젝트를 이름으로 직접 찾는다.
  목록이 세 곳에 있고 전부 같아야 한다: `generate_content.py`의 `HANDMADE_FLOORS`,
  `MapBuilder.IsHandAuthored`, `validate_content.py`의 `HAND_AUTHORED`
- 5층 목표 레벨은 고정값이 아니라 1~4층을 원본 데이터로 시뮬레이션해 얻는다(`simulate_handmade`)
- C# 전투 코드를 고치면 `thesword_balance.py`도 같이 고쳐야 한다 — 두 곳이 어긋나면 검증이 거짓말을 한다

### Testing Requirements
- `python generate_content.py` (dry-run)가 통과해야 `--write` 한다
- `--write` 후 `python validate_content.py`로 산출물 재검증

## Dependencies

### Internal
- `Assets/@Scripts/Managers/Core/DataManager.cs` — CSV 셀 코드 파서 (규약의 원본)
- `Assets/@Scripts/Editor/ContentValidator.cs` — 같은 검사의 Unity 판

### External
- 표준 라이브러리만 사용 (외부 패키지 없음)

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
