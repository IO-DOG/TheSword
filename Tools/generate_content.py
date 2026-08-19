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
    NONE, BEAST, MAGIC, GUARDIAN, IMMORTAL, KNIGHT, TITAN, ASSASSIN, ARMOR,
    TRAIT_NAME,
)
from layout_gen import build_floor_layout, validate_layout

# ConsumableItemData 의 회복% 와 짝. 여기 값을 바꾸면 그 표도 같이 봐야 한다.
POTION_BY_HEAL = {0.15: "I_03", 0.20: "I_03", 0.30: "I_04", 0.50: "I_06"}
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
NEW_MONSTER_ID_BASE = 100      # (예전 대역. 아래 표로 대체됨)
# 층마다 몹이 5종이라 대역을 넉넉히 잡는다. 층 F 의 k 번째 몹 = BASE + F*8 + k.
MOB_ID_BASE = 1000             # 1040 ~ 1804
BOSS_ID_BASE = 900             # 900 ~ 904
MOB_NAME_BASE = 11000
MOB_DESC_BASE = 21000
BOSS_NAME_BASE = 10900
BOSS_DESC_BASE = 20900
SCRIPT_STAGE_NAME_BASE = 5100  # 기존 5000~5004 뒤
SCRIPT_MON_NAME_BASE = 10100   # 기존 10000~10008 뒤
SCRIPT_MON_DESC_BASE = 20100   # 기존 20000~20008 뒤

# 층당 전투 목표치 (플레이어 최대 HP 대비 손실 비율, 전투 지속 시간 초)
# 몹 5마리 * 최대 9.5% = 약 47%, 여기에 포션 2개(각 30%)로 층당 순회복이 되게 잡는다.
# 층의 몹 5마리는 서로 다른 종이고, 뒤로 갈수록 아프다.
# 미로가 나무 구조라 경로가 유일하고 문·열쇠가 구역을 자르므로,
# 이 순서대로 만날 수밖에 없다 — 그게 이 층의 "정답 경로"다.
MOB_LOSS_RAMP = [0.72, 0.88, 1.00, 1.16, 1.34]

# 특성 8종 + 룬 + 도입부 경험치 정정까지 반영해 다시 조율한 값.
# 도입부에서 실제로 얻는 경험치(스테이지 배율 포함)를 세기 시작하자 5층 진입이
# Lv9 -> Lv15 로 올라가 그만큼 헐렁해졌다. 0.048 이 설계 목표(실수 9회)에 맞는다.
MOB_HP_LOSS = 0.048
MOB_DURATION = 16.0
BOSS_HP_LOSS = 0.28
BOSS_DURATION = 45.0

# 층에 배치되는 포션 (ConsumableItemData 의 회복 % 와 대응)
# 구역별 회복 아이템. 미로의 막다른 길에 하나씩 둔다.
#   구역0 없음 / 구역1 20% / 구역2 30% / (보스층) 구역3 50%
# 넘치게 마시면 그만큼 버리는 것이라, 언제 들르느냐가 곧 실력이다.
# 한 층에서 얻는 회복은 그 층에서 잃는 양과 거의 같게 잡는다.
# 남아돌면 물약을 아무 때나 마셔도 되니 판단이 사라지고,
# 모자라면 정답 경로로도 못 간다. 넘치게 마셔 버린 몫이 그대로 빚이 되게 한다.
FLOOR_POTIONS = [0.15, 0.20]   # 구역1 / 구역2
EXIT_POTION = 0.15             # 구역3(계단 앞) — 다음 층으로 들고 가는 몫
BOSS_FLOOR_POTIONS = [0.50]    # 보스층은 계단 앞 대신 이걸 둔다
POTION_USE_THRESHOLD = 0.55  # 이 비율 밑으로 떨어지면 마신다 (실제 플레이 행동)

# 실재하는 몬스터 아트만 사용한다.
#
# Boss_C0_I000~003 은 킹 슬라임과 분열 3종 전용이다. 그 연출에서만 쓰고
# 다른 던전에는 내보내지 않는다 — 도입부의 상징이라 아무 층에서나 나오면
# 그 장면이 싱거워진다.
# 그래서 생성 층이 쓸 수 있는 것은 Mob_C0_I000~I007 여덟 종뿐이고,
# 여기에 챕터별 색 변형(MonsterTint)이 곱해져 실제로 보이는 것은 8 x 5 다.
# 정예와 보스는 그림이 아니라 크기와 색의 진하기로 구분한다.
# 0~7 은 원래 쓰던 여덟 종. 8~9 는 Assets 에 있으면서 클립이 없어 못 쓰던 시트를
# MonsterArtSetup 으로 살린 것이다 (Boss_C1_I000, 예전 Monster_Idle).
# 8~9 는 공격 시트가 없어서 공격에도 대기 클립을 쓴다 — 없는 상태를 가리키면
# 애니메이터가 "State could not be found" 만 찍고 아무것도 재생하지 않는다.
MOB_ART = ([(f"Mob_C0_I{i:03d}", f"Mob_C0_A{i:03d}") for i in range(8)]
           + [("Mob_C0_I008", "Mob_C0_I008"), ("Mob_C0_I009", "Mob_C0_I009")])

# 챕터 보스가 입는 그림. 새로 살린 두 종을 우두머리 자리에 먼저 세운다 —
# 덩치가 크고(48x36, 86x68) 색이 달라서 일반 몹과 확실히 구분된다.
BOSS_ART_INDEX = [8, 9, 6, 4, 7]

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
# 이름은 반드시 실제로 쓰는 그림을 따라간다. 예전에는 아무 접미어나 붙여서
# "이끼 골렘" 이 숲의 정령 그림으로 나오는 식이었다.
# MOB_ART 의 순서(Mob_C0_I000~007)와 한 줄씩 짝이다.
# 이름은 반드시 실제 그림을 따라간다.
# 8~9 는 원본 기획 이름이 없는 시트라, 재어 본 색과 덩치로 이름을 붙였다
# (8 = 회색조 48x36, 9 = 짙은 청록 86x68 로 가장 큼).
MOB_SPECIES = ["슬라임", "슬라임", "크로우", "정령",
               "늑대", "고블린 창병", "해골 전사", "고블린 방패병",
               "잿빛 파수꾼", "심연의 거수"]

# ---------------------------------------------------------------- 특성 (기획서 13·53·71쪽)
#
# 기획서 13쪽: "몬스터 단위 공략이 아닌, 챕터 별 유리한 특성과 공략 방법 고민."
# 그래서 특성을 무작위로 흩지 않고 챕터마다 성격을 준다. 층 안에서는 서열이
# 올라갈수록 까다로운 특성이 나온다(MOB_LOSS_RAMP 와 같은 순서).
#
#   챕터 0  도입      — 특성 없는 상대로 기본을 익힌다
#   챕터 1  방어       — 수호·갑옷. 방어를 어떻게 뚫을지가 문제가 된다
#   챕터 2  화력       — 마법·거대. 오래 끌면 죽는다, 빨리 끝내야 한다
#   챕터 3  치명타     — 불사·암살. 치명 주기를 언제 쓸지가 문제가 된다
#   챕터 4  총복습     — 앞의 것이 전부 섞여 나온다
CHAPTER_TRAITS = [
    [NONE,     NONE,     BEAST,    NONE,     GUARDIAN],
    [NONE,     GUARDIAN, ARMOR,    GUARDIAN, KNIGHT],
    [KNIGHT,   MAGIC,    TITAN,    MAGIC,    TITAN],
    [IMMORTAL, ASSASSIN, IMMORTAL, ASSASSIN, BEAST],
    [ARMOR,    MAGIC,    IMMORTAL, TITAN,    ASSASSIN],
]
# 챕터 보스의 특성. 챕터의 성격을 보스가 대표한다.
BOSS_TRAITS = [BEAST, GUARDIAN, TITAN, IMMORTAL, MAGIC]

# 챕터 보스가 떨구는 장비 (EquipData.csv 의 ID).
# 1~4 = 부츠, 5~8 = 목걸이. 둘 다 능력치가 0 이고 유틸만 해금한다.
BOSS_REWARD = [1, 5, 2, 6, 3]

# 특성별 스탯 성격 (기획서 53쪽의 "능력치 특징"을 배수로 옮긴 것).
# 절대값이 아니라 배수다 — 실제 수치는 특성을 켠 시뮬레이터로 역산하므로,
# 여기서는 "어느 쪽으로 치우친 상대인가" 만 정한다.
TRAIT_FLAVOR = {
    NONE:     dict(aspd=1.00, dfn=1.0, dspd=1.00, dur=1.0),
    BEAST:    dict(aspd=0.90, dfn=1.5, dspd=1.00, dur=1.2),  # 체력·방어 높음
    MAGIC:    dict(aspd=0.70, dfn=0.6, dspd=1.00, dur=0.8),  # 낮은 체력, 느린 마법
    GUARDIAN: dict(aspd=0.90, dfn=1.5, dspd=2.00, dur=1.0),  # 높은 방어, 빠른 방어속도
    IMMORTAL: dict(aspd=0.60, dfn=0.6, dspd=0.60, dur=1.0),  # 낮은 스탯, 매우 느림
    KNIGHT:   dict(aspd=2.00, dfn=0.8, dspd=1.00, dur=0.9),  # 쾌속 = 50%x2회를 공속으로
    TITAN:    dict(aspd=0.80, dfn=1.0, dspd=0.80, dur=1.3),  # 많은 체력, 높은 공격
    ASSASSIN: dict(aspd=1.30, dfn=0.5, dspd=0.50, dur=1.0),  # 은신, 빠른 공격속도
    ARMOR:    dict(aspd=0.90, dfn=0.0, dspd=0.01, dur=1.0),  # 방어 불가, 방어력 0
}


def trait_of(chapter, order):
    return CHAPTER_TRAITS[chapter % len(CHAPTER_TRAITS)][order % MOBS_PER_FLOOR]


# ---------------------------------------------------------------- 룬 (기획서 65·81쪽)
#
# "능력치 증가 아이템 / 획득 시, 공방체 능력치 증가", "획득과 동시에 사용되며
# 공방체 스텟을 영구히 증가시켜준다".
#
# 값은 새로 만들지 않고 ConsumableItemData 에 이미 있는 9/10/11 을 쓴다.
# 공격력 +1 은 총량으로 보면 작아 보이지만, 플레이어가 한 레벨에 얻는 것이 +5 다.
# 즉 룬 하나가 반 레벨의 5분의 1 — 세 층에 하나씩 같은 종류가 돌아오므로
# 자연 성장의 약 13% 를 룬이 맡는다. 층이 올라가도 이 비율은 그대로다
# (레벨당 증가치가 일정하므로 챕터별로 등급을 나눌 필요가 없다).
RUNE_CYCLE = ["I_09", "I_10", "I_11"]
RUNE_GAIN = {"I_09": ("atk", 1.0), "I_10": ("dfn", 1.0), "I_11": ("hp", 5.0)}


def rune_of(floor):
    return RUNE_CYCLE[(floor - HANDMADE_FLOORS - 1) % len(RUNE_CYCLE)]


def rune_bonus(floor):
    """그 층에서 싸울 때 이미 들고 있는 룬의 합.

    룬은 층의 마지막 구역(계단 앞)에 있다. 그래서 F층의 룬은 F층 전투가 끝난
    뒤에 얻고, 효과는 F+1층부터다. 이 한 칸 차이를 맞춰야 역산과 실제가 어긋나지 않는다.
    """
    bonus = {"atk": 0.0, "dfn": 0.0, "hp": 0.0}
    for f in range(HANDMADE_FLOORS + 1, floor):
        stat, amount = RUNE_GAIN[rune_of(f)]
        bonus[stat] += amount
    return bonus


def stats_with_runes(ptable, level, floor):
    """레벨 스탯 + 그 층까지 모은 룬."""
    s = dict(player_stats_at(ptable, level))
    for stat, amount in rune_bonus(floor).items():
        s[stat] += amount
    return s


def player_with_runes(ptable, level, floor, cur_hp=None):
    s = stats_with_runes(ptable, level, floor)
    c = Creature(s["hp"], s["atk"], s["dfn"], s["aspd"], s["dspd"],
                 s["crit"], s["crit_atk"])
    if cur_hp is not None:
        c.hp = cur_hp
    return c

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
# 경험치는 원본의 두 배다. 원본이 100 인데 200 을 주는 이유:
# UI_MonsterCard.Dead 가 몬스터마다 두 번 돌고 있어서, 실제 게임은 늘 경험치를
# 두 배로 주고 있었다. 도입부의 난이도가 그 두 배에 맞춰 손수 조정돼 있어서,
# 중복 호출을 막자마자 1층에서 죽는다(단일 경험치로는 4층 종료 시 Lv8 HP21 —
# 칼끝이다). 버그를 되돌리는 대신 같은 경험치를 데이터로 정직하게 준다.
# 5층 목표 레벨은 이 값으로 다시 시뮬레이션해서 얻는다.
ORIGINAL_STAGES = [
    dict(id=0, DungeonID="00_000", Type=0, UpStage="00_001", DownStage="-",
         BossRoom="-", ATK=1, DEF=1, EXP=200, BGM="BGM_000",
         DungeonNameScriptID=5000),
    dict(id=1, DungeonID="00_001", Type=0, UpStage="00_002", DownStage="00_000",
         BossRoom="00_003", ATK=1, DEF=1, EXP=200, BGM="BGM_000",
         DungeonNameScriptID=5001),
    dict(id=2, DungeonID="00_002", Type=0, UpStage="-", DownStage="00_001",
         BossRoom="-", ATK=4, DEF=4, EXP=200, BGM="BGM_001",
         DungeonNameScriptID=5002),
    dict(id=3, DungeonID="00_003", Type=2, UpStage="00_004", DownStage="-",
         BossRoom="-", ATK=1, DEF=1, EXP=400, BGM="BGM_002",
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

    # 게임은 경험치에 스테이지 배율을 곱한다 (StageInfoData 의 EXP / 100).
    # 그걸 빼고 몬스터 원값만 더하고 있어서, 도입부에서 실제로 얻는 경험치보다
    # 적게 셌다 — 5층의 기준 레벨이 그만큼 낮게 잡혀 있었다.
    exp_scale = {st["DungeonID"]: st["EXP"] / 100.0 for st in ORIGINAL_STAGES}

    for did, boss_id in HANDMADE_RUN:
        scale = exp_scale.get(did, 1.0)
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
                         md["CriticalAttack"] or 200, md.get("Ability", 0))

            won, _, _ = simulate_battle(p, m)
            cur_hp = p.hp
            if not won:
                return None, (f"손수 만든 {did} 층 {i + 1}번째 전투에서 사망 "
                              f"(Lv{level}, 몬스터 {md['Name']})")

            exp += md["RewardExp"] * scale
            while level + 1 in ptable and exp >= ptable[level + 1]["need_exp"]:
                exp -= ptable[level + 1]["need_exp"]
                level += 1
                cur_hp += ptable[level]["hp"]

    return (level, exp, cur_hp), None


# ------------------------------------------------------------------ 밸런싱

def solve_monster(ptable, level, hp_loss_target, duration_target, aspd, trait=NONE,
                  floor=None):
    """플레이어 레벨에 맞춰 몬스터 스탯을 역산한다.

    HP  -> 전투 지속시간이 목표가 되도록 (플레이어 DPS 기준)
    ATK -> 플레이어 HP 손실이 목표 비율이 되도록
    둘 다 시뮬레이터를 기준으로 이분 탐색한다.

    특성을 켠 채로 역산한다. 이게 중요하다 — 암살은 치명타가 아닌 공격을 전부
    회피하고 불사는 80% 를 흘리므로, 특성을 끄고 뽑은 수치는 실제와 몇 배씩
    어긋난다. 이분 탐색이 특성까지 포함해서 답을 찾게 둔다.
    """
    ps = player_stats_at(ptable, level) if floor is None else stats_with_runes(ptable, level, floor)
    flavor = TRAIT_FLAVOR[trait]
    aspd = round(aspd * flavor["aspd"], 2)
    dspd = round(max(0.01, 0.1 * flavor["dspd"]), 3)
    duration_target *= flavor["dur"]
    dfn_m = max(0, int(ps["atk"] * 0.30 * flavor["dfn"]))

    def build(hp, atk):
        return Creature(hp, atk, dfn_m, aspd, dspd, 99, 200, trait)

    # 1) HP 이분 탐색: 지속시간 목표
    lo, hi = 1.0, max(50.0, ps["atk"] * duration_target)
    for _ in range(40):
        mid = (lo + hi) / 2
        p = make_player(ptable, level) if floor is None else player_with_runes(ptable, level, floor)
        won, dur, _ = simulate_battle(p, build(mid, 1))
        if not won or dur < duration_target:
            lo = mid
        else:
            hi = mid
    hp_m = max(1.0, round(hi))

    # 2) ATK 이분 탐색: HP 손실 목표
    target_loss = ps["hp"] * hp_loss_target
    # 하한은 0 이어야 한다. 예전에는 "공격력이 방어력보다 작으면 피해가 없다" 는
    # 셈으로 DEF 에서 시작했는데, 마법은 공격력에 치명 배율을 먼저 곱하므로
    # 하한에서 이미 목표를 넘어 이분 탐색이 한 칸도 움직이지 못했다.
    # 그 결과 마법 몬스터만 목표의 10배를 때렸다.
    lo, hi = 0.0, float(int(ps["dfn"])) + max(20.0, ps["hp"])
    for _ in range(40):
        mid = (lo + hi) / 2
        p = make_player(ptable, level) if floor is None else player_with_runes(ptable, level, floor)
        won, _, loss = simulate_battle(p, build(hp_m, mid))
        if not won or loss > target_loss:
            hi = mid
        else:
            lo = mid
    atk_m = max(1.0, round(lo))

    # 특성이 속도까지 바꾸므로 실제로 쓴 값을 돌려준다. 데이터에 들어가는 값과
    # 검증에 쓰인 값이 달라지면 검증이 의미가 없다.
    return hp_m, atk_m, dfn_m, aspd, dspd


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

        theme = CHAPTER_THEMES[ch]
        # 층 몹 전부를 잡으면 정확히 1레벨
        reward = round(exp_to_next(ptable, level) / MOBS_PER_FLOOR)

        for k, ramp_k in enumerate(MOB_LOSS_RAMP[:MOBS_PER_FLOOR]):
            trait = trait_of(ch, k)
            hp_k, atk_k, dfn_k, aspd_k, dspd_k = solve_monster(
                ptable, level, MOB_HP_LOSS * ramp * ramp_k,
                MOB_DURATION * ramp, aspd, trait, floor)
            # 층마다 서로 다른 놈이 서게 고른다. 가장 센 놈(정예)은 그림이 아니라
            # 색이 진하고 몸집이 커서 눈에 띈다 (MonsterTint / MapBuilder.SetupLook).
            art_idx = (idx + k) % len(MOB_ART)
            art = MOB_ART[art_idx]
            monsters.append(dict(
                id=MOB_ID_BASE + floor * 8 + k, Chapter=ch, Ability=trait,
                Name=f"{theme[1]} {MOB_SPECIES[art_idx]}",
                Attack=float(atk_k), Defence=float(dfn_k), MaxHP=float(hp_k),
                AttackSpeed=float(aspd_k), DefenceSpeed=float(dspd_k),
                Critical=99.0, CriticalAttack=200.0,
                RewardExp=float(reward), RewardItem=-1,
                IdleAnimStr=art[0], AttackAnimStr=art[1],
                BattleParticleAttack="FX_WeaponSlash_00",
                BattleParticleHit="FX_WeaponHit_14",
                Shadow="Mob_Shadow_000",
                MonsterNameId=MOB_NAME_BASE + floor * 8 + k,
                MonsterDescId=MOB_DESC_BASE + floor * 8 + k,
                _floor=floor, _boss=False, _order=k,
            ))

        if boss:
            btrait = BOSS_TRAITS[ch % len(BOSS_TRAITS)]
            bhp, batk, bdfn, baspd, bdspd = solve_monster(
                ptable, level, BOSS_HP_LOSS, BOSS_DURATION, 1.1, btrait, floor)
            bart_idx = BOSS_ART_INDEX[ch % len(BOSS_ART_INDEX)]
            bart = MOB_ART[bart_idx]
            monsters.append(dict(
                id=BOSS_ID_BASE + ch, Chapter=ch, Ability=btrait,
                Name=f"{theme[0]}의 {MOB_SPECIES[bart_idx]} 우두머리",
                Attack=float(batk), Defence=float(bdfn), MaxHP=float(bhp),
                AttackSpeed=float(baspd), DefenceSpeed=float(max(0.15, bdspd)),
                Critical=20.0, CriticalAttack=200.0,
                RewardExp=float(round(exp_to_next(ptable, level) * 0.6)),
                # 기획서 107쪽 — 보스를 잡으면 보상 아이템을 떨군다.
                # 능력치가 0 인 부츠·목걸이만 준다(기획서 34쪽의 "유틸 기능 해금").
                # 공격력이 붙은 무기를 주면 그 뒤 층의 밸런스가 통째로 어긋난다.
                RewardItem=BOSS_REWARD[ch % len(BOSS_REWARD)],
                IdleAnimStr=bart[0], AttackAnimStr=bart[1],
                BattleParticleAttack="FX_WeaponSlash_00",
                BattleParticleHit="FX_WeaponHit_18",
                Shadow="Mob_Shadow_000",
                MonsterNameId=BOSS_NAME_BASE + ch,
                MonsterDescId=BOSS_DESC_BASE + ch,
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
        mobs = sorted((m for m in floor_mons if not m["_boss"]),
                      key=lambda m: m["_order"])
        boss = next((m for m in floor_mons if m["_boss"]), None)

        entry_level, entry_hp = level, cur_hp
        # 미로가 강제하는 순서 그대로. 약한 놈부터, 마지막에 보스.
        fights = list(mobs) + ([boss] if boss else [])

        # 구역별 포션. (회복비율, 쓸 수 있게 되는 전투 인덱스)
        # 구역1 포션은 두 번째 전투부터, 구역2 포션은 네 번째 전투부터 닿는다.
        potions = [(FLOOR_POTIONS[0], 1), (FLOOR_POTIONS[1], 3)]
        # 보스층은 보스 직전에 쓸 큰 물약과, 올라가기 전에 채울 물약을 따로 둔다.
        # 큰 것 하나만 두면 보스를 잡고 빈사로 다음 층에 올라가 그대로 죽는다.
        if boss:
            potions.append((BOSS_FLOOR_POTIONS[0], len(fights) - 1))
        potions.append((EXIT_POTION, len(fights)))

        for i, md in enumerate(fights):
            stats = stats_with_runes(ptable, level, floor)
            m = Creature(md["MaxHP"], md["Attack"], md["Defence"],
                         md["AttackSpeed"], md["DefenceSpeed"],
                         md["Critical"], md["CriticalAttack"], md["Ability"])

            # 포션은 주우면 즉시 회복이고 최대치에서 잘린다(ConsumableItem.PickUp).
            # 그래서 넘치게 마시면 그만큼 버리는 것이고, 정답 경로는 "죽지 않을
            # 만큼만, 가장 늦게" 든다. 이 전투를 그냥 치러 보고 죽을 때만 마신다.
            while True:
                probe = Creature(stats["hp"], stats["atk"], stats["dfn"],
                                 stats["aspd"], stats["dspd"], stats["crit"],
                                 stats["crit_atk"])
                probe.hp = min(cur_hp, stats["hp"])
                probe_m = Creature(md["MaxHP"], md["Attack"], md["Defence"],
                                   md["AttackSpeed"], md["DefenceSpeed"],
                                   md["Critical"], md["CriticalAttack"], md["Ability"])
                survives, _, _ = simulate_battle(probe, probe_m)
                if survives:
                    break
                usable = [t for t in potions if t[1] <= i]
                if not usable:
                    break
                heal, _ = usable[0]
                potions.remove(usable[0])
                cur_hp = min(stats["hp"], cur_hp + stats["hp"] * heal)

            p = Creature(stats["hp"], stats["atk"], stats["dfn"],
                         stats["aspd"], stats["dspd"], stats["crit"],
                         stats["crit_atk"])
            p.hp = min(cur_hp, stats["hp"])

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

        # 계단 앞 회복은 올라가기 직전에 든다. 이미 가득하면 그만큼 버린다.
        # 이 층의 룬도 계단 앞에 있으니 여기서 최대 체력이 올라간다.
        stats_end = stats_with_runes(ptable, level, floor + 1)
        for heal, avail_at in list(potions):
            if avail_at >= len(fights):
                cur_hp = min(stats_end["hp"], cur_hp + stats_end["hp"] * heal)
                potions.remove((heal, avail_at))

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
        mobs = sorted((m for m in by_floor[floor] if not m["_boss"]),
                      key=lambda m: m["_order"])
        boss = next((m for m in by_floor[floor] if m["_boss"]), None)
        walls = CHAPTER_THEMES[ch][3]

        # 구역별 회복 아이템 (없는 구역은 None)
        region3 = [POTION_BY_HEAL[EXIT_POTION]]
        if boss:
            region3.insert(0, POTION_BY_HEAL[BOSS_FLOOR_POTIONS[0]])
        pots = [[],
                [POTION_BY_HEAL[FLOOR_POTIONS[0]]],
                [POTION_BY_HEAL[FLOOR_POTIONS[1]]],
                region3]

        grid = None
        for attempt in range(50):
            g, origins, doors = build_floor_layout(
                [m["id"] for m in mobs], boss["id"] if boss else None, walls,
                seed=floor * 1000 + attempt, mobs_in_floor=MOBS_PER_FLOOR,
                equip_id=CHAPTER_EQUIP_REWARD.get(floor), potions=pots,
                rune=rune_of(floor))
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
