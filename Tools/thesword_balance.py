"""TheSword 전투 시뮬레이터 + 레벨 곡선.

Unity C# 전투 코드(UI_BaseCard / UI_MonsterCard / CreatureClass.DefaultTrait)를
그대로 옮긴 것. 밸런스 수치는 전부 이 시뮬레이터로 검증한다.

원본 대응:
  - 공격 주기      : 3f / AttackSpeed        (UI_MonsterCard.CoDelayAttack)
  - 방어 게이지 주기: 3f / DefenceSpeed       (UI_MonsterCard.CoDelayDefence)
  - Critical       : "N회 공격마다 1회" 주기  (확률 아님)
  - 데미지         : max(1, round(ATK * crit) - DEF), 방어 중이면 1 (크리면 25%)
"""

import csv
import os

FIXED_DT = 0.02  # WaitForFixedUpdate 기본값

# ---------------------------------------------------------------- 플레이어 테이블


class Creature:
    __slots__ = ("hp", "max_hp", "atk", "dfn", "aspd", "dspd", "crit_period",
                 "crit_atk", "shield", "atk_count")

    def __init__(self, hp, atk, dfn, aspd, dspd, crit_period, crit_atk):
        self.max_hp = self.hp = float(hp)
        self.atk = float(atk)
        self.dfn = float(dfn)
        self.aspd = float(aspd)
        self.dspd = float(dspd)
        self.crit_period = int(crit_period)
        self.crit_atk = float(crit_atk)
        self.shield = False
        self.atk_count = 0


def compute_damage(attacker, target, is_crit):
    """CreatureClass.DefaultTrait.ExecuteAttack 이식."""
    num = float(int(max(0.0, attacker.atk)))
    if is_crit:
        num = num * (attacker.crit_atk / 100.0)
    damage = int(round(num))
    damage -= int(target.dfn)
    damage = int(max(1, damage))
    if target.shield and is_crit:
        damage = int(damage * 0.25)
    elif target.shield:
        damage = 1
    return damage


def simulate_battle(player, monster, max_seconds=600.0):
    """1:1 전투. (플레이어 생존여부, 소요시간, 플레이어 HP 손실) 반환.

    플레이어 HP는 호출자가 넘긴 player.hp 에서 이어서 깎인다.
    """
    p_cd = 3.0 / player.aspd
    m_cd = 3.0 / monster.aspd
    p_def_cd = 3.0 / player.dspd
    m_def_cd = 3.0 / monster.dspd

    p_t = m_t = p_def_t = m_def_t = 0.0
    player.shield = monster.shield = False
    player.atk_count = monster.atk_count = 0
    start_hp = player.hp
    t = 0.0

    while t < max_seconds:
        # --- 공격 판정 (원본: 쿨 도달 시 공격 후 0으로 리셋)
        if p_t >= p_cd:
            p_t = 0.0
            player.atk_count += 1
            is_crit = False
            if player.crit_period > 0 and player.atk_count >= player.crit_period:
                is_crit = True
                player.atk_count = 0
            dmg = compute_damage(player, monster, is_crit)
            was_shielded = monster.shield
            monster.hp -= dmg
            if was_shielded:  # OnDefenceAction -> ClearDefence
                monster.shield = False
                m_def_t = 0.0
            if monster.hp <= 0:
                return True, t, start_hp - player.hp

        if m_t >= m_cd:
            m_t = 0.0
            monster.atk_count += 1
            is_crit = False
            if monster.crit_period > 0 and monster.atk_count >= monster.crit_period:
                is_crit = True
                monster.atk_count = 0
            dmg = compute_damage(monster, player, is_crit)
            was_shielded = player.shield
            player.hp -= dmg
            if was_shielded:
                player.shield = False
                p_def_t = 0.0
            if player.hp <= 0:
                return False, t, start_hp - player.hp

        # --- 방어 게이지
        if p_def_t >= p_def_cd:
            player.shield = True
            p_def_t = p_def_cd
        if m_def_t >= m_def_cd:
            monster.shield = True
            m_def_t = m_def_cd

        p_t += FIXED_DT
        m_t += FIXED_DT
        p_def_t += FIXED_DT
        m_def_t += FIXED_DT
        t += FIXED_DT

    # 시간 초과 = 서로 못 죽임 = 사실상 진행 불가
    return False, t, start_hp - player.hp


# ---------------------------------------------------------------- 레벨 테이블

def load_player_table(csv_path):
    """PlayerData.csv -> {level: {stat: delta}}  (레벨 1행은 기본값, 2행부터 증가치)"""
    rows = {}
    with open(csv_path, "r", encoding="utf-8-sig") as f:
        reader = csv.reader(f)
        next(reader)  # header
        for r in reader:
            if not r or not r[0].strip():
                continue
            lv = int(r[0])
            rows[lv] = dict(
                need_exp=float(r[1]),
                total_exp=float(r[2]),
                atk=float(r[3]),
                dfn=float(r[4]),
                hp=float(r[5]),
                aspd=float(r[6]),
                dspd=float(r[7]),
                crit=float(r[8]),
                crit_atk=float(r[9]),
                mspd=float(r[10]),
            )
    return rows


def extend_player_table(rows, max_level):
    """레벨 99 이후를 같은 패턴으로 연장.

    CurExp 세터가 PlayerDic[Level+1] 을 무조건 읽으므로, 도달 가능한 최대 레벨보다
    최소 1 이상 더 있어야 KeyNotFoundException 이 안 난다.
    """
    top = max(rows)
    growth = rows[top]["need_exp"] / rows[top - 1]["need_exp"]
    need = rows[top]["need_exp"]
    total = rows[top]["total_exp"]
    for lv in range(top + 1, max_level + 1):
        need = round(need * growth)
        total += need
        rows[lv] = dict(
            need_exp=float(need),
            total_exp=float(total),
            atk=5.0,
            dfn=5.0,
            hp=25.0,
            aspd=0.0,
            dspd=0.0,
            crit=0.0,
            crit_atk=5.0 if lv % 5 == 0 else 0.0,
            mspd=0.0,
        )
    return rows


def player_stats_at(rows, level):
    """레벨 L 시점의 누적 스탯 (장비 미착용 기준)."""
    s = dict(rows[1])
    for lv in range(2, level + 1):
        r = rows[lv]
        s["atk"] += r["atk"]
        s["dfn"] += r["dfn"]
        s["hp"] += r["hp"]
        s["aspd"] += r["aspd"]
        s["dspd"] += r["dspd"]
        s["crit"] += r["crit"]
        s["crit_atk"] += r["crit_atk"]
    return s


def make_player(rows, level, cur_hp=None):
    s = player_stats_at(rows, level)
    c = Creature(s["hp"], s["atk"], s["dfn"], s["aspd"], s["dspd"], s["crit"], s["crit_atk"])
    if cur_hp is not None:
        c.hp = cur_hp
    return c


def exp_to_next(rows, level):
    return rows[level + 1]["need_exp"]


if __name__ == "__main__":
    here = os.path.dirname(os.path.abspath(__file__))
    tbl = load_player_table(os.path.join(
        here, "..", "Assets", "@Resources", "Data", "Excel", "PlayerData.csv"))
    tbl = extend_player_table(tbl, 110)
    for lv in (1, 10, 25, 50, 75, 99, 105):
        s = player_stats_at(tbl, lv)
        print(f"Lv{lv:>3}  HP {s['hp']:>7.0f}  ATK {s['atk']:>6.0f}  DEF {s['dfn']:>6.0f}  "
              f"CRIATK {s['crit_atk']:>5.0f}  다음레벨필요EXP {exp_to_next(tbl, lv):>10.0f}")
