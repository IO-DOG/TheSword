"""TheSword 100층 콘텐츠 생성기.

산출물:
  Assets/@Resources/Data/Excel/{PlayerData,MonsterData,StageInfoData,ScriptData}.csv
  Assets/@Resources/Data/JsonData/{...}.json          <- 런타임이 실제로 읽는 파일
  Assets/StreamingAssets/Data/Excel/Dungeon_CC_FFF.csv <- 층 레이아웃 100장

설계 규칙 (사용자 지정):
  - 총 100층, 20층마다 테마(챕터) 변경 -> 챕터 00~04
  - 미로형이지만 "순서"가 핵심. 몬스터를 정해진 순서로 잡아야 레벨/HP가 맞는다.
  - 층의 몹을 다 잡으면 정확히 1레벨 오른다.

안전 규칙:
  - 기존 데이터 행은 절대 덮어쓰지 않는다 (번역 스크립트/챕터0 보스 보존).
    새 항목만 충돌하지 않는 ID 대역에 추가한다.
  - **1~4층은 손수 만든 원본이다.** 튜토리얼 / 마검 계약 / 킹슬라임 연출이
    DirectingManager 에서 그 층의 특정 오브젝트 이름(Items/CItem13, SpawnKingSlime …)을
    직접 찾기 때문에, 레이아웃을 새로 만들면 NullReference 로 인트로가 통째로 깨진다.
    생성은 5층부터. 5층의 목표 레벨은 1~4층을 실제로 시뮬레이션해서 얻는다.
"""

import csv
import json
import os
import random

from thesword_balance import (
    Creature, extend_player_table, exp_to_next, load_player_table,
    make_player, player_stats_at, simulate_battle,
)
from layout_gen import build_floor_layout, validate_layout
from mapdata_gen import emit_mapdata

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
EXCEL = os.path.join(ROOT, "Assets", "@Resources", "Data", "Excel")
JSOND = os.path.join(ROOT, "Assets", "@Resources", "Data", "JsonData")
STREAM = os.path.join(ROOT, "Assets", "StreamingAssets", "Data", "Excel")

TOTAL_FLOORS = 100
FLOORS_PER_CHAPTER = 20
MOBS_PER_FLOOR = 5
MAX_LEVEL_TABLE = 115          # CurExp 세터가 Level+1 을 읽으므로 여유를 둔다
NEW_MONSTER_ID_BASE = 100      # 기존 몬스터 0~16 과 충돌 회피
SCRIPT_STAGE_NAME_BASE = 5100  # 기존 5000~5004 뒤
SCRIPT_MON_NAME_BASE = 10100   # 기존 10000~10008 뒤
SCRIPT_MON_DESC_BASE = 20100   # 기존 20000~20008 뒤

# 층당 전투 목표치 (플레이어 최대 HP 대비 손실 비율, 전투 지속 시간 초)
# 몹 5마리 * 최대 9.5% = 약 47%, 여기에 포션 2개(각 30%)로 층당 순회복이 되게 잡는다.
MOB_HP_LOSS = 0.07
MOB_DURATION = 16.0
BOSS_HP_LOSS = 0.28
BOSS_DURATION = 45.0

# 층에 배치되는 포션 (ConsumableItemData 의 회복 % 와 대응)
FLOOR_POTIONS = [0.30, 0.30]
BOSS_FLOOR_POTIONS = [0.50, 0.50]
POTION_USE_THRESHOLD = 0.55  # 이 비율 밑으로 떨어지면 마신다 (실제 플레이 행동)

# 실재하는 몬스터 아트만 사용한다 (Mob_C0_I000~I007 / Boss_C0_I000~I003)
MOB_ART = [(f"Mob_C0_I{i:03d}", f"Mob_C0_A{i:03d}") for i in range(8)]
BOSS_ART = [(f"Boss_C0_I{i:03d}", f"Boss_C0_A{i:03d}") for i in range(4)]

# 벽 프리팹은 Tilemap_C00_W01 / W02 / W03 만 실재한다 (W00 은 없음).
# 챕터별 분위기는 MapBuilder 의 틴트 + 조명 + BGM 으로 낸다.
CHAPTER_THEMES = [
    # (이름, 몬스터 접두어, BGM, 벽 타일셋)
    ("이끼 낀 지하 묘소", "이끼", "BGM_000", ["W_01", "W_02"]),
    ("무너진 수로", "수렁", "BGM_100", ["W_02", "W_03"]),
    ("잿빛 용광로", "잿불", "BGM_001", ["W_03", "W_01"]),
    ("얼어붙은 심층", "서리", "BGM_101", ["W_01", "W_03"]),
    ("왕좌의 균열", "심연", "BGM_102", ["W_02", "W_01"]),
]
MOB_SUFFIX = ["슬라임", "박쥐", "망령", "골렘", "사냥개", "수호병", "그림자", "파수꾼"]

# 챕터 클리어 보상 장비. EquipItemAnimator 에 실제로 상태가 있는 id(0~4)만 쓴다.
# ponytail: EquipData 의 스탯이 전부 0 이라 지금은 연출용이다.
#           장비로 밸런스를 잡으려면 EquipData 를 채우고 여기 id 를 바꿀 것.
CHAPTER_EQUIP_REWARD = {20: 1, 40: 2, 60: 3, 80: 4}

# ---- 손수 만든 도입부 (건드리지 않는다) -------------------------------------
HANDMADE_FLOORS = 4                      # 1~4층 = Dungeon_00_000 ~ 00_003
# 실제 진행 순서. 00_002(3층)는 마검 이벤트 방이라 몬스터가 없다.
HANDMADE_RUN = [("00_000", None), ("00_001", None), ("00_003", 5)]
HANDMADE_BOSS_ID = 5                     # 킹 슬라임 (프리팹에 구워져 있는 값)
STARTING_SWORD_ATK = 3.0                 # Define.EQUIP_SOWRD_FIRST = 9 (블레이드)

# 원본 StageInfoData 의 1~4층 행. 선형이 아니다:
#   1층 -> 2층 -> 3층(막다른 마검방),  2층 -> [보스방] 4층 -> 5층
ORIGINAL_STAGES = [
    dict(id=0, DungeonID="00_000", Type=0, UpStage="00_001", DownStage="-",
         BossRoom="-", ATK=1, DEF=1, EXP=100, BGM="BGM_000",
         DungeonNameScriptID=5000),
    dict(id=1, DungeonID="00_001", Type=0, UpStage="00_002", DownStage="00_000",
         BossRoom="00_003", ATK=1, DEF=1, EXP=100, BGM="BGM_000",
         DungeonNameScriptID=5001),
    dict(id=2, DungeonID="00_002", Type=0, UpStage="-", DownStage="00_001",
         BossRoom="-", ATK=4, DEF=4, EXP=100, BGM="BGM_001",
         DungeonNameScriptID=5002),
    dict(id=3, DungeonID="00_003", Type=2, UpStage="00_004", DownStage="-",
         BossRoom="-", ATK=1, DEF=1, EXP=200, BGM="BGM_002",
         DungeonNameScriptID=5003),
]


def dungeon_id(floor):
    """1-based 층 번호 -> 'CC_FFF'"""
    ch = (floor - 1) // FLOORS_PER_CHAPTER
    idx = (floor - 1) % FLOORS_PER_CHAPTER
    return f"{ch:02d}_{idx:03d}", ch, idx


def is_boss_floor(floor):
    return floor % FLOORS_PER_CHAPTER == 0


# -------------------------------------------------- 손수 만든 1~4층 시뮬레이션

def _load_original_tables():
    """1~4층에 쓰이는 원본 몬스터(0~16)와 포션 회복률."""
    with open(os.path.join(JSOND, "MonsterData.json"), "r", encoding="utf-8") as f:
        monsters = {m["id"]: m for m in json.load(f)["creatures"]
                    if m["id"] < NEW_MONSTER_ID_BASE}

    potions = {}
    with open(os.path.join(EXCEL, "ConsumableItemData.csv"), "r",
              encoding="utf-8-sig") as f:
        reader = csv.reader(f)
        next(reader)
        for row in reader:
            if row and row[0].strip():
                potions[int(row[0])] = float(row[1])
    return monsters, potions


def _cells(dungeon_id_str, pattern):
    """층 CSV 에서 'M_003' 같은 셀의 숫자만 뽑는다."""
    import re
    path = os.path.join(STREAM, f"Dungeon_{dungeon_id_str}.csv")
    with open(path, "r", encoding="utf-8-sig") as f:
        text = f.read()
    return [int(re.sub(r"[^0-9]", "", c)) for c in re.findall(pattern, text)]


def simulate_handmade(ptable):
    """1~4층을 원본 데이터 그대로 완주시켜 (레벨, 잔여경험치, 현재HP) 를 얻는다.

    이 구간은 우리가 스탯을 정할 수 없으므로, "생성 구간이 어디서 시작하는지"를
    측정하는 것이 목적이다. 약한 몬스터부터 잡는 순서를 가정한다 — 그게 이 게임의
    설계 규칙이고, 파일 순서대로 싸우면 실제로 1층에서 죽는다.
    """
    monsters, potions_by_id = _load_original_tables()

    level, exp = 1, 0.0
    cur_hp = player_stats_at(ptable, level)["hp"]

    for did, boss_id in HANDMADE_RUN:
        mob_ids = [boss_id] if boss_id is not None else _cells(did, r"M_[0-9]+")
        # 순서가 곧 난이도. 약한 놈부터.
        mob_ids.sort(key=lambda i: monsters[i]["MaxHP"] * monsters[i]["Attack"])

        potions = sorted(p for p in (potions_by_id.get(i, 0.0)
                                     for i in _cells(did, r"I_[0-9]+")) if p > 0)

        for i, mid in enumerate(mob_ids):
            stats = player_stats_at(ptable, level)
            while potions and cur_hp < stats["hp"] * POTION_USE_THRESHOLD:
                cur_hp = min(stats["hp"], cur_hp + stats["hp"] * potions.pop(0) / 100.0)

            p = Creature(stats["hp"], stats["atk"] + STARTING_SWORD_ATK, stats["dfn"],
                         stats["aspd"], stats["dspd"], stats["crit"], stats["crit_atk"])
            p.hp = min(cur_hp, stats["hp"])

            md = monsters[mid]
            m = Creature(md["MaxHP"], md["Attack"], md["Defence"], md["AttackSpeed"],
                         md["DefenceSpeed"] or 0.1, md["Critical"] or 99,
                         md["CriticalAttack"] or 200)

            won, _, _ = simulate_battle(p, m)
            cur_hp = p.hp
            if not won:
                return None, (f"손수 만든 {did} 층 {i + 1}번째 전투에서 사망 "
                              f"(Lv{level}, 몬스터 {md['Name']})")

            exp += md["RewardExp"]
            while level + 1 in ptable and exp >= ptable[level + 1]["need_exp"]:
                exp -= ptable[level + 1]["need_exp"]
                level += 1
                cur_hp += ptable[level]["hp"]

    return (level, exp, cur_hp), None


# ------------------------------------------------------------------ 밸런싱

def solve_monster(ptable, level, hp_loss_target, duration_target, aspd):
    """플레이어 레벨에 맞춰 몬스터 스탯을 역산한다.

    HP  -> 전투 지속시간이 목표가 되도록 (플레이어 DPS 기준)
    ATK -> 플레이어 HP 손실이 목표 비율이 되도록
    둘 다 시뮬레이터를 기준으로 이분 탐색한다.
    """
    ps = player_stats_at(ptable, level)
    dfn_m = max(0, int(ps["atk"] * 0.30))

    def build(hp, atk):
        return Creature(hp, atk, dfn_m, aspd, 0.1, 99, 200)

    # 1) HP 이분 탐색: 지속시간 목표
    lo, hi = 1.0, max(50.0, ps["atk"] * duration_target)
    for _ in range(40):
        mid = (lo + hi) / 2
        p = make_player(ptable, level)
        won, dur, _ = simulate_battle(p, build(mid, 1))
        if not won or dur < duration_target:
            lo = mid
        else:
            hi = mid
    hp_m = max(1.0, round(hi))

    # 2) ATK 이분 탐색: HP 손실 목표
    target_loss = ps["hp"] * hp_loss_target
    lo, hi = float(int(ps["dfn"])), float(int(ps["dfn"])) + max(20.0, ps["hp"])
    for _ in range(40):
        mid = (lo + hi) / 2
        p = make_player(ptable, level)
        won, _, loss = simulate_battle(p, build(hp_m, mid))
        if not won or loss > target_loss:
            hi = mid
        else:
            lo = mid
    atk_m = max(1.0, round(lo))

    return hp_m, atk_m, dfn_m


def target_level(floor, start_level):
    """F층 도착 시 목표 레벨.

    1~4층은 손수 만든 구간이라 우리가 정할 수 없다. 거기서 실제로 도달하는 레벨
    (start_level)을 5층의 기준으로 삼고, 그 뒤로는 층당 1레벨씩 올라간다.
    """
    return start_level + (floor - HANDMADE_FLOORS)


def build_monsters(ptable, start_level):
    """층별 몬스터 1종 + 챕터별 보스 1종을 만든다 (5~100층)."""
    monsters = []
    for floor in range(HANDMADE_FLOORS + 1, TOTAL_FLOORS + 1):
        did, ch, idx = dungeon_id(floor)
        level = target_level(floor, start_level)
        boss = is_boss_floor(floor)

        # 챕터 안에서 조금씩 조여든다
        ramp = 1.0 + 0.35 * (idx / (FLOORS_PER_CHAPTER - 1))
        aspd = round(0.9 + 0.4 * (idx / (FLOORS_PER_CHAPTER - 1)), 2)

        hp, atk, dfn = solve_monster(
            ptable, level, MOB_HP_LOSS * ramp, MOB_DURATION * ramp, aspd)

        theme = CHAPTER_THEMES[ch]
        art = MOB_ART[idx % len(MOB_ART)]
        mid = NEW_MONSTER_ID_BASE + floor
        # 층 몹 전부를 잡으면 정확히 1레벨
        reward = round(exp_to_next(ptable, level) / MOBS_PER_FLOOR)

        monsters.append(dict(
            id=mid, Chapter=ch, Ability=0,
            Name=f"{theme[1]} {MOB_SUFFIX[idx % len(MOB_SUFFIX)]}",
            Attack=float(atk), Defence=float(dfn), MaxHP=float(hp),
            AttackSpeed=float(aspd), DefenceSpeed=0.1,
            Critical=99.0, CriticalAttack=200.0,
            RewardExp=float(reward), RewardItem=-1,
            IdleAnimStr=art[0], AttackAnimStr=art[1],
            BattleParticleAttack="FX_WeaponSlash_00",
            BattleParticleHit="FX_WeaponHit_14",
            Shadow="Mob_Shadow_000",
            MonsterNameId=SCRIPT_MON_NAME_BASE + floor,
            MonsterDescId=SCRIPT_MON_DESC_BASE + floor,
            _floor=floor, _boss=False,
        ))

        if boss:
            bhp, batk, bdfn = solve_monster(
                ptable, level, BOSS_HP_LOSS, BOSS_DURATION, 1.1)
            bart = BOSS_ART[ch % len(BOSS_ART)]
            monsters.append(dict(
                id=NEW_MONSTER_ID_BASE + 200 + ch, Chapter=ch, Ability=0,
                Name=f"{theme[0]}의 주인",
                Attack=float(batk), Defence=float(bdfn), MaxHP=float(bhp),
                AttackSpeed=1.1, DefenceSpeed=0.15,
                Critical=20.0, CriticalAttack=200.0,
                RewardExp=float(round(exp_to_next(ptable, level) * 0.6)),
                RewardItem=-1,
                IdleAnimStr=bart[0], AttackAnimStr=bart[1],
                BattleParticleAttack="FX_WeaponSlash_00",
                BattleParticleHit="FX_WeaponHit_18",
                Shadow="Mob_Shadow_000",
                MonsterNameId=SCRIPT_MON_NAME_BASE + 200 + ch,
                MonsterDescId=SCRIPT_MON_DESC_BASE + 200 + ch,
                _floor=floor, _boss=True,
            ))
    return monsters


# ------------------------------------------------------------------ 완주 검증

def simulate_run(ptable, monsters, start_state, verbose=True):
    """5층부터 100층까지 실제 전투 공식으로 완주 시뮬레이션.

    1~4층(손수 만든 구간)은 simulate_handmade 가 이미 돌린 뒤라,
    그 결과 상태(start_state)를 이어받아 시작한다.
    포션은 층마다 배치된 것만 사용한다. 죽으면 즉시 실패로 보고한다.

    ponytail: 장비 보너스는 계산에 넣지 않는다. 실제 플레이어는 마검(+10 ATK)을
              들고 있으므로 여기 결과보다 항상 강하다 — 안전한 방향의 오차다.
    """
    by_floor = {}
    for m in monsters:
        by_floor.setdefault(m["_floor"], []).append(m)

    level, exp, cur_hp = start_state
    log = []

    for floor in range(HANDMADE_FLOORS + 1, TOTAL_FLOORS + 1):
        floor_mons = by_floor[floor]
        mob = next(m for m in floor_mons if not m["_boss"])
        boss = next((m for m in floor_mons if m["_boss"]), None)

        entry_level, entry_hp = level, cur_hp
        fights = [mob] * MOBS_PER_FLOOR + ([boss] if boss else [])

        # 층에 배치된 포션. 실제 플레이처럼 HP 가 낮을 때만 마신다.
        potions = list(FLOOR_POTIONS) + (list(BOSS_FLOOR_POTIONS) if boss else [])

        for i, md in enumerate(fights):
            stats = player_stats_at(ptable, level)
            # 위험하면 포션 사용 (보스 앞에서는 반드시 채우고 들어간다)
            while potions and (cur_hp < stats["hp"] * POTION_USE_THRESHOLD
                               or (md["_boss"] and cur_hp < stats["hp"] * 0.95)):
                cur_hp = min(stats["hp"], cur_hp + stats["hp"] * potions.pop(0))
            p = Creature(stats["hp"], stats["atk"], stats["dfn"],
                         stats["aspd"], stats["dspd"], stats["crit"],
                         stats["crit_atk"])
            p.hp = min(cur_hp, stats["hp"])
            m = Creature(md["MaxHP"], md["Attack"], md["Defence"],
                         md["AttackSpeed"], md["DefenceSpeed"],
                         md["Critical"], md["CriticalAttack"])

            won, dur, loss = simulate_battle(p, m)
            cur_hp = p.hp
            if not won:
                return False, log, (
                    f"{floor}층 {i + 1}번째 전투에서 사망 "
                    f"(Lv{level}, 진입HP {entry_hp:.0f}/{stats['hp']:.0f}, "
                    f"몬스터 {md['Name']} HP{md['MaxHP']:.0f} ATK{md['Attack']:.0f})")

            # 경험치 -> 레벨업 (Unity CurExp 세터와 동일하게 반복 처리)
            exp += md["RewardExp"]
            while level + 1 in ptable and exp >= ptable[level + 1]["need_exp"]:
                exp -= ptable[level + 1]["need_exp"]
                level += 1
                cur_hp += ptable[level]["hp"]  # LevelUp() 은 CurHP 도 같이 올린다

        stats = player_stats_at(ptable, level)
        log.append(dict(floor=floor, entry_level=entry_level, exit_level=level,
                        hp=cur_hp, max_hp=stats["hp"],
                        hp_pct=100.0 * cur_hp / stats["hp"]))
        if verbose and (floor % 10 == 0 or floor == HANDMADE_FLOORS + 1
                        or is_boss_floor(floor)):
            tag = "BOSS" if is_boss_floor(floor) else "    "
            print(f"  {tag} {floor:>3}층  Lv{entry_level:>3}->{level:>3}  "
                  f"HP {cur_hp:>6.0f}/{stats['hp']:>6.0f} ({100.0 * cur_hp / stats['hp']:>5.1f}%)")

    return True, log, None


# ------------------------------------------------------------------ 파일 출력

def write_csv(path, header, rows):
    with open(path, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(header)
        w.writerows(rows)


def write_json(path, root_key, items):
    with open(path, "w", encoding="utf-8") as f:
        json.dump({root_key: items}, f, ensure_ascii=False, indent=2)


def emit_player_data(ptable):
    header = ["Lv", "NeedEXP", "TotalExp", "공격력", "방어력", "체력",
              "공격속도", "방어속도", "치명타", "치명공격력", "이동속도"]
    rows, items = [], []
    for lv in sorted(ptable):
        r = ptable[lv]
        rows.append([lv, int(r["need_exp"]), int(r["total_exp"]), r["atk"], r["dfn"],
                     r["hp"], r["aspd"], r["dspd"], r["crit"], r["crit_atk"], r["mspd"]])
        items.append(dict(id=lv, NeedExp=r["need_exp"], TotalExp=r["total_exp"],
                          Attack=r["atk"], Defence=r["dfn"], MaxHP=r["hp"],
                          AttackSpeed=r["aspd"], DefenceSpeed=r["dspd"],
                          Critical=r["crit"], CriticalAttack=r["crit_atk"],
                          MoveSpeed=r["mspd"]))
    write_csv(os.path.join(EXCEL, "PlayerData.csv"), header, rows)
    write_json(os.path.join(JSOND, "PlayerData.json"), "creatures", items)


def emit_monster_data(monsters):
    """기존 몬스터(0~16)는 보존하고 새 몬스터만 뒤에 붙인다."""
    existing_path = os.path.join(JSOND, "MonsterData.json")
    with open(existing_path, "r", encoding="utf-8") as f:
        existing = json.load(f)["creatures"]
    keep = [m for m in existing if m["id"] < NEW_MONSTER_ID_BASE]

    items = list(keep)
    for m in monsters:
        d = {k: v for k, v in m.items() if not k.startswith("_")}
        items.append(d)
    write_json(existing_path, "creatures", items)

    header = ["ID", "챕터", "특성 ID", "몹이름", "공격력", "방어력", "체력", "공격속도",
              "방어속도", "치명타", "치명공격력", "보상_경험치", "보상 아이템",
              "몬스터_대기", "몬스터_공격", "전투파티클_공격", "전투파티클_피격",
              "몬스터_그림자", "몹 이름 ID", "몹 설명 ID"]
    rows = [[i["id"], i["Chapter"], i["Ability"], i["Name"], i["Attack"], i["Defence"],
             i["MaxHP"], i["AttackSpeed"], i["DefenceSpeed"], i["Critical"],
             i["CriticalAttack"], i["RewardExp"], i["RewardItem"], i["IdleAnimStr"],
             i["AttackAnimStr"], i["BattleParticleAttack"], i["BattleParticleHit"],
             i["Shadow"], i["MonsterNameId"], i["MonsterDescId"]] for i in items]
    write_csv(os.path.join(EXCEL, "MonsterData.csv"), header, rows)


def emit_stage_info():
    """100층 스테이지 그래프.

    1~4층은 원본 그래프를 그대로 쓴다 (선형이 아니고 보스방이 옆으로 물려 있다).
    5층부터는 F -> F+1 한 방향 진행.
    """
    items, rows = list(ORIGINAL_STAGES), []
    for it in ORIGINAL_STAGES:
        rows.append([it["id"], it["DungeonID"],
                     "Boss" if it["Type"] == 2 else "Common",
                     it["UpStage"], it["DownStage"], it["BossRoom"],
                     it["ATK"], it["DEF"], it["EXP"], it["BGM"],
                     it["DungeonNameScriptID"]])

    for floor in range(HANDMADE_FLOORS + 1, TOTAL_FLOORS + 1):
        did, ch, idx = dungeon_id(floor)
        up = dungeon_id(floor + 1)[0] if floor < TOTAL_FLOORS else "-"
        down = dungeon_id(floor - 1)[0] if floor > 1 else "-"
        boss = is_boss_floor(floor)
        theme = CHAPTER_THEMES[ch]
        # Define.DungeonType: Common=0, Special=1, Boss=2
        # ATK/DEF/EXP 는 MonsterController.SetMonster() 에서 "곱셈" 계수로 쓰인다.
        #   Attack = StageInfo.ATK * MonsterData.Attack,  RewardExp = EXP/100 * RewardExp
        # 층별 수치는 이미 MonsterData 에 정확히 넣었으므로 여기서는 1배로 둔다.
        item = dict(id=floor - 1, DungeonID=did, Type=2 if boss else 0,
                    UpStage=up, DownStage=down, BossRoom="-",
                    ATK=1, DEF=1, EXP=100, BGM=theme[2],
                    DungeonNameScriptID=SCRIPT_STAGE_NAME_BASE + floor)
        items.append(item)
        rows.append([item["id"], did, "Boss" if boss else "Common", up, down, "-",
                     1, 1, 100, theme[2], item["DungeonNameScriptID"]])

    header = ["ID", "Dungeon_ID", "Type", "Up_Stairs", "Down_Stairs", "Boss_Room",
              "ATK_보정 (n)", "DEF_보정 (n)", "EXP_보정 (%)", "BGM", "던전 이름_ID"]
    write_csv(os.path.join(EXCEL, "StageInfoData.csv"), header, rows)
    write_json(os.path.join(JSOND, "StageInfoData.json"), "stageInfos", items)
    return items


def emit_scripts(monsters):
    """기존 번역은 그대로 두고 새 ID만 추가한다."""
    path = os.path.join(JSOND, "ScriptData.json")
    with open(path, "r", encoding="utf-8") as f:
        scripts = json.load(f)["scripts"]
    known = {s["id"] for s in scripts}

    def add(sid, kr, en):
        if sid in known:
            return
        scripts.append(dict(id=sid, ScriptKr=kr, ScriptEn=en, ScriptJp=kr, ScriptCn=kr))
        known.add(sid)

    # 1~4층 이름(5000~5003)은 원본 번역이 이미 있다.
    for floor in range(HANDMADE_FLOORS + 1, TOTAL_FLOORS + 1):
        _, ch, _ = dungeon_id(floor)
        theme = CHAPTER_THEMES[ch]
        add(SCRIPT_STAGE_NAME_BASE + floor, f"{theme[0]} {floor}층",
            f"{floor}F")
    for m in monsters:
        add(m["MonsterNameId"], m["Name"], m["Name"])
        add(m["MonsterDescId"], f"{m['Name']}. 깊은 곳에서 올라온 존재.",
            f"{m['Name']}.")

    scripts.sort(key=lambda s: s["id"])
    write_json(path, "scripts", scripts)


# ------------------------------------------------------------------ 층 레이아웃

def emit_layouts(monsters, write=True):
    """100층 레이아웃 CSV 출력. 도달 불가 레이아웃은 시드를 바꿔 재생성한다."""
    by_floor = {}
    for m in monsters:
        by_floor.setdefault(m["_floor"], []).append(m)

    written, failures = 0, []
    for floor in range(HANDMADE_FLOORS + 1, TOTAL_FLOORS + 1):
        did, ch, _ = dungeon_id(floor)
        mob = next(m for m in by_floor[floor] if not m["_boss"])
        boss = next((m for m in by_floor[floor] if m["_boss"]), None)
        walls = CHAPTER_THEMES[ch][3]

        grid = None
        for attempt in range(50):
            g, origins, doors = build_floor_layout(
                mob["id"], boss["id"] if boss else None, walls,
                seed=floor * 1000 + attempt, mobs_in_floor=MOBS_PER_FLOOR,
                equip_id=CHAPTER_EQUIP_REWARD.get(floor))
            if g is None:
                continue
            ok, err = validate_layout(g, origins, doors)
            if ok:
                grid = g
                break
        if grid is None:
            failures.append(f"Dungeon_{did}")
            continue

        if write:
            path = os.path.join(STREAM, f"Dungeon_{did}.csv")
            with open(path, "w", encoding="utf-8", newline="") as f:
                for row in grid:
                    f.write(",".join(row) + "\n")
        written += 1
    return written, failures


# ------------------------------------------------------------------ 진입점

def build_all(dry_run=False):
    ptable = load_player_table(os.path.join(EXCEL, "PlayerData.csv"))
    ptable = extend_player_table(ptable, MAX_LEVEL_TABLE)

    print(f"[1/5] 손수 만든 1~{HANDMADE_FLOORS}층 완주 시뮬레이션")
    start_state, err = simulate_handmade(ptable)
    if start_state is None:
        print(f"  [실패] {err}")
        return False
    start_level, start_exp, start_hp = start_state
    print(f"      {HANDMADE_FLOORS}층 종료 시 Lv{start_level}, HP {start_hp:.0f} "
          f"-> {HANDMADE_FLOORS + 1}층 목표 레벨 {target_level(HANDMADE_FLOORS + 1, start_level)}")

    print(f"[2/5] {HANDMADE_FLOORS + 1}~{TOTAL_FLOORS}층 몬스터 스탯 역산 중...")
    monsters = build_monsters(ptable, start_level)
    print(f"      몬스터 {len(monsters)}종 생성")

    print(f"[3/5] {HANDMADE_FLOORS + 1}~{TOTAL_FLOORS}층 완주 시뮬레이션")
    ok, log, err = simulate_run(ptable, monsters, start_state)
    if not ok:
        print(f"  [실패] {err}")
        return False
    worst = min(log, key=lambda r: r["hp_pct"])
    print(f"      완주 성공. 최저 HP 구간: {worst['floor']}층 {worst['hp_pct']:.1f}%")
    print(f"      최종 레벨: {log[-1]['exit_level']}")

    print("[4/5] 층 레이아웃 생성 + 도달 가능성 검사")
    written, failures = emit_layouts(monsters, write=not dry_run)
    print(f"      {written}/{TOTAL_FLOORS - HANDMADE_FLOORS} 층 생성 "
          f"(1~{HANDMADE_FLOORS}층은 원본 유지)")
    if failures:
        print(f"  [실패] 레이아웃 생성 불가: {', '.join(failures)}")
        return False

    if dry_run:
        print("      dry-run 이므로 파일은 쓰지 않음")
        return True

    print("[5/5] 데이터 파일 출력")
    emit_player_data(ptable)
    emit_monster_data(monsters)
    emit_stage_info()
    emit_scripts(monsters)

    # 레이아웃 CSV 를 쓴 "다음에" 돌려야 한다. MapBuilder 가 읽는 것은 이 JSON 이다.
    print("      MapData.json 생성 (CSV -> 런타임 오브젝트 배치)")
    maps, counts = emit_mapdata()
    print(f"      맵 {len(maps)}장, {counts}")
    print("      완료")
    return True


if __name__ == "__main__":
    import sys
    ok = build_all(dry_run="--write" not in sys.argv)
    sys.exit(0 if ok else 1)
