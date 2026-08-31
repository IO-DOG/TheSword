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


# 특성 (Define.Trait 과 같은 순서여야 한다)
NONE, BEAST, MAGIC, GUARDIAN, IMMORTAL, KNIGHT, TITAN, ASSASSIN, ARMOR, KINGSLIME = range(10)

TRAIT_NAME = {
    NONE: "없음", BEAST: "야수", MAGIC: "마법", GUARDIAN: "수호", IMMORTAL: "불사",
    KNIGHT: "검사", TITAN: "거대", ASSASSIN: "암살", ARMOR: "갑옷", KINGSLIME: "분열",
}

ARMOR_SHIELD_RATIO = 0.3   # ArmorTrait.SHIELD_RATIO — 껍질 게이지는 체력에 비례
TITAN_ROAR = 0.2           # TitanTrait.Roar — 포효는 공격력의 20%


class Creature:
    __slots__ = ("hp", "max_hp", "atk", "dfn", "aspd", "dspd", "crit_period",
                 "crit_atk", "shield", "atk_count", "trait", "armor", "hit_count",
                 "beast_done", "stealth")

    def __init__(self, hp, atk, dfn, aspd, dspd, crit_period, crit_atk, trait=NONE):
        self.max_hp = self.hp = float(hp)
        self.atk = float(atk)
        self.dfn = float(dfn)
        self.aspd = float(aspd)
        self.dspd = float(dspd)
        self.crit_period = int(crit_period)
        self.crit_atk = float(crit_atk)
        self.shield = False
        self.atk_count = 0
        self.trait = int(trait)
        self.armor = self.max_hp * ARMOR_SHIELD_RATIO
        self.hit_count = 0
        self.beast_done = False
        self.stealth = True


def _standard_damage(attacker, target, is_crit):
    """대부분의 특성이 공유하는 공격식 (DefaultTrait.ExecuteAttack)."""
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


def compute_damage(attacker, target, is_crit):
    """공격자의 특성으로 피해를 구한다 (ITrait.ExecuteAttack).

    ※ 여기 있는 것은 CreatureClass 에서 실제로 도는 코드다. 기획서와 코드가
    어긋난 곳(암살의 은신 해제, 포효의 20%, 껍질 게이지)은 코드를 기획서에
    맞춰 고쳐 놓고 그 결과를 옮겼다. 검사만 예외로, "50% 2회" 를 코드가 아니라
    데이터(공속 2배)로 낸다 — 총량이 사실상 같고 공격 흐름을 건드리지 않는다.
    """
    if attacker.trait == BEAST:
        # 야수만 식이 다르다: 1 로 바닥을 받치지 않고, 방어 중이면 0 이다.
        damage = int(max(0.0, attacker.atk))
        if is_crit:
            damage *= int(attacker.crit_atk / 100.0)
        damage -= int(target.dfn)
        if target.shield and is_crit:
            damage = int(damage * 0.25)
        elif target.shield:
            damage = 0
        return damage

    if attacker.trait == MAGIC:
        # 마력: 마법은 100% 치명 공격.
        return _standard_damage(attacker, target, True)

    return _standard_damage(attacker, target, is_crit)


def apply_hit(attacker, target, damage, is_crit):
    """맞는 쪽의 특성으로 피해를 적용한다 (ITrait.ExcuteOnHit).

    돌려주는 값은 공격자가 되받은 피해(거대의 포효)다. 없으면 0.
    """
    back = 0

    if target.trait in (NONE, BEAST, KINGSLIME):
        damage = max(0, damage)
    elif target.trait == IMMORTAL:
        # 면역: 일반 공격은 20% 만, 치명 공격은 그대로.
        if not is_crit:
            damage = int(damage * 0.2)
    elif target.trait == ASSASSIN:
        # 은신: 일반 공격을 회피하고, 치명 공격을 맞으면 은신이 풀린다.
        if target.stealth and not is_crit:
            damage = 0
        elif is_crit:
            target.stealth = False
    elif target.trait == ARMOR:
        # 껍질: 방어 게이지가 모든 공격을 흡수하고, 다 깎이면 넘친 만큼만 들어간다.
        target.armor -= damage
        if target.armor <= 0:
            damage = -target.armor
            target.armor = 0
        else:
            damage = 0

    target.hp -= damage

    if target.trait == BEAST and not target.beast_done:
        # 광폭: HP 10% 이하가 되면 한 번, 최대 체력의 40% 를 즉시 회복.
        if target.hp > 0 and target.hp / target.max_hp <= 0.1:
            target.beast_done = True
            target.hp += target.max_hp * 0.4

    if target.trait == TITAN:
        # 포효: 5회 맞을 때마다 때린 쪽에 되받아친다.
        target.hit_count += 1
        if target.hit_count >= 5:
            target.hit_count = 0
            back = int(round(_standard_damage(target, attacker, False) * TITAN_ROAR))
            attacker.hp -= max(0, back)

    return back


# ---------------------------------------------------------------- 액티브 스킬
# BattleSkills 와 같은 값이어야 한다. 스킬은 플레이어만 쓰고 봇은 쓰지 않으므로
# 완주 보장 계산에는 넣지 않는다 — 스킬은 게임을 쉽게만 만들 수 있으니,
# 스킬 없이 잰 "실수 허용치" 는 그대로 하한으로 유효하다.
SMASH_RATIO = 2.5
DRAIN_RATIO = 1.5


def skill_damage(player, monster, ratio):
    """강타/흡혈이 넣는 피해. 상대 특성을 거친 실제 감소량을 돌려준다."""
    dmg = int(round(max(1.0, player.atk * ratio - monster.dfn)))
    before = monster.hp
    apply_hit(player, monster, dmg, False)
    return max(0.0, before - monster.hp)


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
    player.armor = player.max_hp * ARMOR_SHIELD_RATIO
    monster.armor = monster.max_hp * ARMOR_SHIELD_RATIO
    player.hit_count = monster.hit_count = 0
    player.beast_done = monster.beast_done = False
    player.stealth = monster.stealth = True
    start_hp = player.hp
    t = 0.0

    # 철벽: 전투 시작 시 방어 상태로 시작한다 (GuardianTrait 생성자가 게이지를 채운다).
    if player.trait == GUARDIAN:
        player.shield = True
        p_def_t = p_def_cd
    if monster.trait == GUARDIAN:
        monster.shield = True
        m_def_t = m_def_cd

    while t < max_seconds:
        # --- 공격 판정 (원본: 쿨 도달 시 공격 후 0으로 리셋)
        if p_t >= p_cd:
            p_t = 0.0
            player.atk_count += 1
            is_crit = False
            if player.crit_period > 0 and player.atk_count >= player.crit_period:
                is_crit = True
                player.atk_count = 0
            if player.trait == MAGIC:
                is_crit = True          # 마력: 마법은 100% 치명 공격
            dmg = compute_damage(player, monster, is_crit)
            was_shielded = monster.shield
            apply_hit(player, monster, dmg, is_crit)
            if was_shielded:  # OnDefenceAction -> ClearDefence
                monster.shield = False
                m_def_t = 0.0
            if player.hp <= 0:          # 거대의 포효에 되맞아 죽을 수 있다
                return False, t, start_hp - player.hp
            if monster.hp <= 0:
                return True, t, start_hp - player.hp

        if m_t >= m_cd:
            m_t = 0.0
            monster.atk_count += 1
            is_crit = False
            if monster.crit_period > 0 and monster.atk_count >= monster.crit_period:
                is_crit = True
                monster.atk_count = 0
            if monster.trait == MAGIC:
                is_crit = True
            dmg = compute_damage(monster, player, is_crit)
            was_shielded = player.shield
            apply_hit(monster, player, dmg, is_crit)
            if was_shielded:
                player.shield = False
                p_def_t = 0.0
            if monster.hp <= 0:         # 플레이어가 거대라면 포효로 몬스터가 죽을 수 있다
                return True, t, start_hp - player.hp
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


def _self_check():
    """특성이 기획서대로 도는지 최소 확인. 이식이 조용히 어긋나는 것을 막는다."""
    def mk(trait=NONE, hp=1000, atk=100, dfn=0, crit=99):
        return Creature(hp, atk, dfn, 1.0, 0.1, crit, 200, trait)

    # 없음: 방어 중이면 1 로 막힌다
    a, b = mk(), mk()
    b.shield = True
    assert compute_damage(a, b, False) == 1

    # 야수: 방어 중이면 0, 그리고 10% 이하에서 한 번 회복한다
    a, b = mk(BEAST), mk()
    b.shield = True
    assert compute_damage(a, b, False) == 0
    beast = mk(BEAST, hp=1000)
    beast.hp = 60
    apply_hit(mk(), beast, 20, False)
    assert beast.hp > 400, beast.hp          # 40% 회복이 들어갔다
    before = beast.hp
    apply_hit(mk(), beast, 10, False)
    assert beast.hp == before - 10           # 두 번은 없다

    # 마법: 일반 공격이어도 치명 배율이 붙는다
    assert compute_damage(mk(MAGIC), mk(), False) == compute_damage(mk(), mk(), True)

    # 불사: 일반 공격은 20% 만, 치명은 그대로
    t = mk(IMMORTAL); apply_hit(mk(), t, 100, False); assert t.max_hp - t.hp == 20
    t = mk(IMMORTAL); apply_hit(mk(), t, 100, True);  assert t.max_hp - t.hp == 100

    # 암살: 일반은 회피, 치명을 맞으면 은신이 풀리고 그 뒤로는 일반도 들어간다
    t = mk(ASSASSIN)
    apply_hit(mk(), t, 100, False); assert t.hp == t.max_hp
    apply_hit(mk(), t, 100, True);  assert t.max_hp - t.hp == 100
    apply_hit(mk(), t, 100, False); assert t.max_hp - t.hp == 200

    # 갑옷: 체력의 30% 만큼 흡수하고 넘친 만큼만 들어간다
    t = mk(ARMOR, hp=100)          # 껍질 30
    apply_hit(mk(), t, 20, False); assert t.hp == 100
    apply_hit(mk(), t, 25, False); assert t.hp == 85, t.hp   # 15 만 관통
    apply_hit(mk(), t, 10, False); assert t.hp == 75

    # 거대: 5회째 피격에 공격력 20% 로 되받아친다
    atk = mk(); t = mk(TITAN, atk=100)
    for _ in range(4):
        assert apply_hit(atk, t, 10, False) == 0
    back = apply_hit(atk, t, 10, False)
    assert back == 20, back

    # 수호: 전투 시작부터 방어 상태
    p = mk(); g = mk(GUARDIAN, hp=10 ** 6)
    simulate_battle(p, g, max_seconds=0.1)
    assert g.shield

    # 스킬은 상대 특성을 거쳐 들어가야 한다. 껍질이나 면역을 통째로 무시하면
    # 챕터마다 다른 특성을 공략한다는 설계가 무너진다.
    plain = mk(hp=1000, atk=100)
    target = mk(hp=1000)
    assert skill_damage(plain, target, SMASH_RATIO) == 250     # 100*2.5 - 0

    armored = mk(ARMOR, hp=1000)                                # 껍질 300
    dealt = skill_damage(mk(atk=100), armored, SMASH_RATIO)
    assert dealt == 0, dealt                                    # 껍질이 통째로 흡수

    immortal = mk(IMMORTAL, hp=1000)
    dealt = skill_damage(mk(atk=100), immortal, SMASH_RATIO)
    assert dealt == 50, dealt                                   # 일반 공격이라 20% 만

    # <b>전투에서 잃는 HP 는 시작 HP 와 무관하다.</b> 플레이어의 공격력·공속이
    # HP 에 안 걸리고 몬스터 HP 도 고정이라 전투 길이가 고정이기 때문이다.
    # generate_content 의 "물약을 늦게 마셔도 이득이 없다" 는 결론이 이 성질
    # 하나에 걸려 있다 — 여기가 깨지면 그 결론부터 다시 재야 한다.
    def loss_from(hp0):
        p = mk(hp=1000, atk=50, crit=5)
        p.hp = hp0
        won, _, lost = simulate_battle(p, mk(hp=400, atk=30, crit=7))
        assert won
        return lost
    assert loss_from(1000) == loss_from(600) == loss_from(400)

    print("특성 자체 점검 통과")


if __name__ == "__main__":
    _self_check()

    here = os.path.dirname(os.path.abspath(__file__))
    tbl = load_player_table(os.path.join(
        here, "..", "Assets", "@Resources", "Data", "Excel", "PlayerData.csv"))
    tbl = extend_player_table(tbl, 110)
    for lv in (1, 10, 25, 50, 75, 99, 105):
        s = player_stats_at(tbl, lv)
        print(f"Lv{lv:>3}  HP {s['hp']:>7.0f}  ATK {s['atk']:>6.0f}  DEF {s['dfn']:>6.0f}  "
              f"CRIATK {s['crit_atk']:>5.0f}  다음레벨필요EXP {exp_to_next(tbl, lv):>10.0f}")
