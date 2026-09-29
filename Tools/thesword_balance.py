"""TheSword 전투 시뮬레이터 + 레벨 곡선.

Unity C# 전투 코드(BattleStepper / CreatureClass 의 ITrait)를 그대로 옮긴 것.
밸런스 수치는 전부 이 시뮬레이터로 검증한다.

원본 대응:
  - 전투 시계      : BattleStepper.Step(Time.fixedDeltaTime) — 한 스텝의 순서는
                     플레이어 공격 -> 플레이어 방어 -> 몬스터 공격 -> 몬스터 방어
  - 공격 주기      : 3f / AttackSpeed
  - 방어 게이지 주기: 3f / DefenceSpeed
  - Critical       : "N회 공격마다 1회" 주기  (확률 아님)
  - 데미지         : max(1, round(ATK * crit) - DEF), 방어 중이면 1 (크리면 25%)

다른 점 하나 — 전투 사이에 이어지는 것:
  게임은 치명 횟수(AttackCount)·방어 게이지(DefenceCoolTime)·방어 상태(IsDefence)를
  전투가 끝나도 들고 다음 전투로 간다(UI_BattlePopup, BattleForecast 가 받는다).
  여기서는 전투마다 0 에서 시작한다. 그러면 치명타가 늘 20번째 공격에야 나오니
  실제보다 늦거나 같다 — 합으로 보면 플레이어에게 불리한 쪽이라 완주 판정이
  보수적이 된다. 그래서 일부러 맞추지 않는다 (한 전투만 보면 순서에 따라 몇 대
  차이가 난다 — 전투 예측이 "다음에 싸우면" 이라고 말하는 이유다).
"""

import bisect
import csv
import os
import struct

# ProjectSettings/TimeManager.asset 의 Fixed Timestep (4699296 / 141120000 = 0.0333).
# 전투는 FixedUpdate 한 번에 Step 한 번이다. 예전에는 0.02 로 셌다 — 공격 한 번이
# 한 스텝 늦거나 빨라지는 만큼 전투마다 몇 %씩 어긋났다.
FIXED_DT = 4699296 / 141120000


def _f32(x):
    """C# float 로 반올림한 값."""
    return struct.unpack("f", struct.pack("f", x))[0]


# BattleStepper 의 타이머는 float 에 dt 를 더해 간다. float 로 k 번 더한 값을 미리
# 재 두고 타이머를 "마지막으로 0 이 된 뒤 몇 스텝" 으로 세면 비교가 비트까지 같다.
# (배정밀도로 더하면 주기가 스텝 경계에 걸린 전투에서 한 대가 어긋난다.)
_DT32 = _f32(FIXED_DT)
_ELAPSED = [0.0]            # _ELAPSED[k] = float 로 dt 를 k 번 더한 값
while _ELAPSED[-1] < 601.0:
    _ELAPSED.append(_f32(_ELAPSED[-1] + _DT32))


def _steps(speed):
    """3f / speed 초가 차는 데 드는 스텝 수. 속도 0 이면 영영 안 찬다."""
    if speed <= 0:
        return len(_ELAPSED)
    return bisect.bisect_left(_ELAPSED, _f32(3.0 / _f32(speed)))

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
        # 치명 배율은 다른 특성처럼 실수로 곱한다 — 예전에는 (int) 로 잘라서
        # 치명공격력 250 이 2배, 199 가 1배였다.
        num = float(int(max(0.0, attacker.atk)))
        if is_crit:
            num *= attacker.crit_atk / 100.0
        damage = int(round(num))
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
        # 포효: 5회 맞을 때마다 때린 쪽에 되받아친다. 5번째 대에 죽어도 포효한다.
        # 이 식이 명세다 — round(0.2 * max(1, ATK - DEF)), 방어 중이면 1 의 20% = 0.
        # (C# 은 공격력의 20% 에서 방어력을 빼서, 층 설계 레벨에서 41마리 전부
        # 포효가 1 이었다. C# 쪽을 이 식에 맞춘다.)
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


def _strike(attacker, target, is_crit):
    """BattleStepper.Strike. 방어 중이던 쪽은 맞으면 방패가 깨진다 — True 를 돌려준다."""
    if attacker.trait == MAGIC:
        is_crit = True                  # 마력: 마법은 100% 치명 공격
    dmg = compute_damage(attacker, target, is_crit)
    was_shielded = target.shield
    apply_hit(attacker, target, dmg, is_crit)
    if was_shielded:                    # OnDefenceAction -> ClearDefence
        target.shield = False
    return was_shielded


def _next_crit(c):
    c.atk_count += 1
    if c.crit_period > 0 and c.atk_count >= c.crit_period:
        c.atk_count = 0
        return True
    return False


def simulate_battle(player, monster, max_seconds=600.0):
    """1:1 전투. (플레이어 생존여부, 소요시간, 플레이어 HP 손실) 반환.

    플레이어 HP는 호출자가 넘긴 player.hp 에서 이어서 깎인다.
    둘 다 쓰러지면(거대의 포효) 진 것이다 — BattleForecast 의 Win 과 같다.
    """
    p_cd, m_cd = _steps(player.aspd), _steps(monster.aspd)
    p_def_cd, m_def_cd = _steps(player.dspd), _steps(monster.dspd)

    # 타이머는 "마지막으로 0 이 된 뒤 지난 스텝 수" 다 (_ELAPSED 참고).
    p_t = m_t = p_def_t = m_def_t = 0
    player.shield = monster.shield = False
    player.atk_count = monster.atk_count = 0
    player.armor = player.max_hp * ARMOR_SHIELD_RATIO
    monster.armor = monster.max_hp * ARMOR_SHIELD_RATIO
    player.hit_count = monster.hit_count = 0
    player.beast_done = monster.beast_done = False
    player.stealth = monster.stealth = True
    start_hp = player.hp

    # 철벽: 전투 시작 시 방어 상태로 시작한다 (GuardianTrait 생성자가 게이지를 채운다).
    if player.trait == GUARDIAN:
        player.shield = True
        p_def_t = p_def_cd
    if monster.trait == GUARDIAN:
        monster.shield = True
        m_def_t = m_def_cd

    # BattleForecast 가 float 로 초를 더해 가며 멈추는 자리와 같다.
    max_steps = bisect.bisect_left(_ELAPSED, max_seconds)
    for step in range(max_steps):
        if p_t >= p_cd:
            p_t = 0
            if _strike(player, monster, _next_crit(player)):
                m_def_t = 0
            if player.hp <= 0:          # 거대의 포효에 되맞아 죽을 수 있다
                return False, step * FIXED_DT, start_hp - player.hp
            if monster.hp <= 0:
                return True, step * FIXED_DT, start_hp - player.hp

        # 플레이어 방패는 몬스터 공격보다 먼저 선다 — 같은 스텝에 차면 그 공격을 막는다.
        if p_def_t >= p_def_cd:
            player.shield = True
            p_def_t = p_def_cd

        if m_t >= m_cd:
            m_t = 0
            if _strike(monster, player, _next_crit(monster)):
                p_def_t = 0
            if player.hp <= 0:
                return False, step * FIXED_DT, start_hp - player.hp
            if monster.hp <= 0:
                return True, step * FIXED_DT, start_hp - player.hp

        if m_def_t >= m_def_cd:
            monster.shield = True
            m_def_t = m_def_cd

        p_t += 1
        m_t += 1
        p_def_t += 1
        m_def_t += 1

    # 시간 초과 = 서로 못 죽임 = 사실상 진행 불가
    return False, max_steps * FIXED_DT, start_hp - player.hp


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

    # 전투 시계는 프로젝트의 고정 스텝을 float 로 센다
    with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..",
                           "ProjectSettings", "TimeManager.asset"), encoding="utf-8") as f:
        text = f.read()
    count = int(text.split("m_Count:")[1].split()[0])
    rate = int(text.split("m_Numerator:")[1].split()[0])
    assert FIXED_DT == count / rate, (FIXED_DT, count, rate)
    assert _steps(1.0) == 91 and _steps(2.2) == 41      # 3초 / 1.36초 주기

    # 한 스텝 안에서는 플레이어 방패가 몬스터 공격보다 먼저 선다
    p = Creature(1000, 1, 0, 0.01, 1.0, 99, 200)          # 3초에 방패, 공격은 없다
    m = Creature(10 ** 6, 100, 0, 1.0, 0.01, 99, 200)     # 3초에 첫 공격
    _, _, lost = simulate_battle(p, m, max_seconds=3.1)
    assert lost == 1, lost                                # 막혔다

    # 야수: 방어 중이면 0, 그리고 10% 이하에서 한 번 회복한다
    a, b = mk(BEAST), mk()
    b.shield = True
    assert compute_damage(a, b, False) == 0
    a = Creature(1000, 100, 0, 1.0, 0.1, 99, 250, BEAST)  # 치명 배율은 자르지 않는다
    assert compute_damage(a, mk(), True) == 250
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
