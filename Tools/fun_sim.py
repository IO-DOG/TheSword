"""TheSword 재미 검증 v2 — 사람처럼 노는 수백 판을 실제 콘텐츠 위에서 돌린다.

  python Tools/fun_sim.py --runs 500        # 일반 표 + 탑의 법 표, 끝에 요약 (8코어 20~50초)
  python Tools/fun_sim.py --quick           # 판 수를 줄인 빠른 확인
  python Tools/fun_sim.py --check           # 전투 엔진 대조만 (simulate_battle·BattleStepper 와)
  python Tools/fun_sim.py --mob-loss 0.06,0.06,0.06,0.13,0.13   # 가정: 그 손실로 푼 표를 메모리에서 지어 한 번 더 [9]
  python Tools/fun_sim.py --json out.json   # 판마다의 요약 (보고서 표를 다시 뽑을 때)

찍는 것: [0] 봇 기준점 · [1] 완주·죽음 · [2] 챕터 최저 HP · [3] 띠 치름/기준과 별·판 점수 · [4] 습관별 차이 ·
[5] 지루함·임계·물약·덤 · [6] 판단 하나의 무게 · [7] 별 문턱(띠별 표) · [8] 예전 규칙: 스킬이 전투마다 · [9] 가정: 몹 손실
보고서: Documents/TheSword_재미검증_v2.md

v1(재미검증 보고서 7절)은 봇의 단순 완주 한 판으로 별을 쟀다 — 20띠 중 14띠가 ★★★. 한 판으로는 문턱을
정할 수 없다. 여기서는 물약 습관·곁길·금고·둘 중 하나·제단·스킬을 사람처럼 흔든 판을 수백 번 돌려
띠마다 "치름/기준" 의 분포를 얻고, 그 분포로 문턱을 고른다.

무엇이 실제와 같은가 (전부 import — 새로 셈하지 않는다)
  - 전투: thesword_balance 의 특성 셈(_strike·apply_hit·skill_damage)과 고정 스텝(_steps·_ELAPSED).
    시계만 사건 단위로 건너뛰며 돈다(같은 걸음, 같은 순서). --check 가 simulate_battle 과 모든 몬스터에서,
    BattleStepper.Step 을 줄 그대로 옮긴 float 시계와 무작위 이어 받기 상태에서 비트까지 같은지 본다.
  - 이어 받기: 게임은 치명 횟수(AttackCount)·방어 게이지(DefenceCoolTime)·방패(IsDefence)를 전투 사이에
    넘긴다(UI_BattlePopup.CarryState, BattleForecast). thesword_balance 는 일부러 전투마다 0 에서 센다 —
    기준 값(ParLoss)이 그 셈이다. 장부의 "치름" 은 이어 받은 실제 값이라 여기서는 이어 받는다.
  - 층: StreamingAssets 의 Dungeon CSV 를 그대로 읽는다(관문/곁길·골방 파수꾼·금고·물약 자리·보스 앞뒤).
    generate_content 의 floor_potions·alcove_plan·choice_pair·rune_of 와 어긋나면 멈춘다.
  - 몬스터·기준 값·제단: JsonData 의 MonsterData(_Tower)·StageInfoData 를 그대로 쓴다.
  - 장부·별·점수·제단: SwordLedger.cs 의 식 그대로(정수 비교, 은행가 반올림, 제단 값은 내림, 기준 0 인 싸움은 안 적는다).
  - 물약: ConsumableItem.PickUp 처럼 회복을 반올림하고, 체력 룬은 지금 체력도 올린다.

근사한 것 (보고서에 적는다)
  - 층 안의 동선: 구역(문 셋으로 자른 넷) 단위로 걷는다. 관문 → 그 구역의 곁길 → 다음 구역. 같은 구역 안의
    싸움 순서는 몬스터 id 순(배치가 약한 놈부터다). 봇처럼 예측이 싼 것부터 잡아도 치름은 1% 안쪽으로만 달라졌다.
  - 스킬: 사람이 언제 누르는지는 모른다. 세 가지 습관(안 쓴다 / 시작하자마자 셋 다 / 때를 맞춘다)과 봇의 규칙을
    전투 시계 사이에 끼워 넣는다. 사람이 더 잘 누를 수 있으니 "때를 맞춘다" 도 상한은 아니다.
    스킬은 층마다 한 번씩 찬다(BattleSkills.RefillForFloor — 처음 오는 층에서). [8] 은 같은 사람을 예전 규칙
    (전투마다 다시 찬다)으로 한 번 더 돌린 것이다.
  - 죽으면 층 입구(체크포인트)에서 다시 한다: 물약을 보이는 대로 마시고(생존의 지배 전략) 곁길을 덜 탄다 → 아래층에
    두고 온 물약을 마시고 온다(층에 들어설 때마다 체크포인트를 새로 쓴다) → 스킬을 꺼낸다. 다 안 되면 막힌 판이다.
  - 1~4층은 simulate_handmade 의 결과(5층 입구 Lv16)로 시작한다 — 봇 녹화 로그(F5 Lv17·F7 Lv18·F8 Lv20, 층 중간에
    찍힌다)도 이 설계 궤도다. CLAUDE.md 의 "실측 Lv17" 은 5층 안에서 한 번 오른 뒤의 값이다.
  - 지난 층으로 워프해 곁길을 쓰는 것은 넣지 않는다(장부가 기준을 안 쳐 주므로 점수에 득이 없다). 금고는 챕터 끝에
    워프해 남은 열쇠로 여는 습관만 넣는다(싸움이 없다).
  - 기준점: 봇을 흉내 낸 판(REFERENCE["봇"])은 v1 의 실제 봇 완주보다 8% 덜 치른다(예언 합도 같은 만큼 — 전투 조건이
    조금 다르다는 뜻이고 원인은 못 찾았다). 문턱 추천은 그 비(V1_BOT)로 보정해 찍는다.
"""

import bisect
import functools
import math
import os
import random
import sys

import generate_content as G
import layout_gen as L
from thesword_balance import (
    Creature, simulate_battle, skill_damage, _strike, _next_crit, _steps, _ELAPSED, _f32, FIXED_DT,
    GUARDIAN, ASSASSIN, SMASH_RATIO, DRAIN_RATIO,
)

# ---------------------------------------------------------------- 전투 (BattleStepper + 이어 받기)

PRICE_HP = 1e7                  # BattleForecast.PriceHP — 값을 잴 때 주는 체력
FRESH = (0, 0, False)           # 새 게임의 이어 받기 상태 (AttackCount, 게이지 걸음 수, IsDefence)
MAX_STEPS = bisect.bisect_left(_ELAPSED, 600.0)     # BattleForecast.MAX_SECONDS 에서 멈추는 걸음

steps_of = functools.lru_cache(maxsize=None)(_steps)

# 스킬 습관. 셋 다 층마다 한 번씩 — 처음 오는 층에 들어설 때 다시 찬다(BattleSkills.RefillForFloor).
SKILL_NONE, SKILL_MASH, SKILL_TIMED, SKILL_BOT = "none", "mash", "timed", "bot"
BOT_EMERGENCY_HP = 0.35         # AutoPlayer.SkillEmergencyHp — 봇은 보스가 아니면 이 아래에서만 쓴다
BOT_DRAIN_HP = 0.6              # AutoPlayer.SkillDrainHp
BOT_SMASH_HP = 0.8              # AutoPlayer.SkillSmashHp


def creature_of(md):
    return Creature(md["MaxHP"], md["Attack"], md["Defence"], md["AttackSpeed"], md["DefenceSpeed"],
                    md["Critical"], md["CriticalAttack"], md["Ability"])


def fight(ps, md, carry=FRESH, hp=None, max_hp=None, skill=SKILL_NONE, boss=False, kit=(True, True, True)):
    """BattleStepper 의 시계로 한 판. ps 는 플레이어 능력치(stats 꼴), carry 는 (치명 횟수, 게이지 걸음, 방패).

    hp 가 None 이면 BattleForecast 처럼 체력을 넉넉히 주고 값을 잰다 — 체력은 "쓰러졌나" 에만 쓰이므로
    값은 같다. 이기는가는 호출하는 쪽이 damage < 지금 체력 으로 정한다(Result.Win).
    스킬이 있으면 흡혈의 회복이 최대 체력에서 잘리므로 실제 체력으로 돈다.

    돌려주는 것: (쓰러뜨렸나, 잃은 HP(흡혈로 되찾은 것은 뺀다), 이어 줄 상태, 끝난 걸음, 내가 휘두른 수, 진 싸움인가)
    스킬이 있으면 끝에 하나 더 — 쓴 스킬 (강타, 철벽, 흡혈). kit 은 아직 쓸 수 있는 스킬(층마다 한 번이라는 가정에 쓴다).

    걸음마다 도는 대신 사건(누가 때리는 걸음)만 밟는다. 한 걸음 안의 순서는 Step 그대로다 —
      1 플레이어 공격  2 플레이어 방패가 선다  3 몬스터 공격  4 몬스터 방패가 선다  5 타이머 += dt
    그래서 몬스터 방패는 "선 걸음 < 지금 걸음" 이어야 플레이어 공격을 막고, 플레이어 방패는 "선 걸음 <= 지금" 이면
    몬스터 공격을 막는다. 방패가 깨진 걸음 b 에서 게이지는 0 이 되고 b + 주기 걸음에 다시 선다.
    """
    priced = hp is None
    p = Creature(PRICE_HP if priced else max_hp, ps["atk"], ps["dfn"], ps["aspd"], ps["dspd"],
                 ps["crit"], ps["crit_atk"])
    p.hp = PRICE_HP if priced else hp
    m = creature_of(md)
    hits, k, shield = carry
    p.atk_count = hits
    p_cd, m_cd = steps_of(p.aspd), steps_of(m.aspd)
    pd_cd, md_cd = steps_of(p.dspd), steps_of(m.dspd)
    pd_up = -1 if shield else max(0, pd_cd - k)         # 플레이어 방패가 서는(선) 걸음
    md_up = -1 if m.trait == GUARDIAN else md_cd        # 몬스터 방패 — 수호는 처음부터 서 있다
    pa, ma = p_cd, m_cd
    start = p.hp
    swings = 0

    def finish(next_step, killed):
        """승부가 난 자리에서 이어 줄 상태. next_step 은 아직 2단계(플레이어 방패)를 돌지 않은 걸음이다."""
        tail = () if skill == SKILL_NONE else (used(),)
        if killed:
            up = pd_up < next_step
            return (True, start - p.hp, (p.atk_count, pd_cd if up else next_step - pd_up + pd_cd, up),
                    next_step, swings, False) + tail
        return (False, start - p.hp, None, next_step, swings, True) + tail

    if skill == SKILL_NONE:
        while True:
            s = pa if pa <= ma else ma
            if s >= MAX_STEPS:
                return (False, start - p.hp, None, MAX_STEPS, swings, False)    # 600초 — 못 끝냈다
            if pa == s:
                # 거대의 포효는 때린 쪽(플레이어)의 방패를 본다 — 1단계라 이 걸음의 방패는 아직 안 섰다.
                m.shield, p.shield = md_up < s, pd_up < s
                if _strike(p, m, _next_crit(p)):
                    md_up = s + md_cd
                swings += 1
                if p.hp <= 0:
                    return finish(s, False)
                if m.hp <= 0:
                    return finish(s, True)
                pa += p_cd
            if ma == s:
                p.shield = pd_up <= s
                if _strike(m, p, _next_crit(m)):
                    pd_up = s + pd_cd
                if p.hp <= 0:
                    return finish(s, False)
                if m.hp <= 0:
                    return finish(s, True)
                ma += m_cd

    # ---- 스킬 (BattleSkills.Use) — 걸음과 걸음 사이(Update)에 끼어든다. kit 에 남은 것만(층마다 한 번).
    #   mash  시작하자마자 셋 다. 은신한 암살에게는 헛손질이고, 가득 찬 체력의 흡혈은 회복이 버려진다.
    #   timed 강타는 시작에(암살은 은신이 풀린 뒤), 철벽은 맨몸으로 맞기 직전, 흡혈은 잃은 HP 가 흡혈 값만큼 찼을 때.
    #   bot   AutoPlayer.TickSkills — 보스에서만(일반 전투는 HP 35% 아래부터). 철벽은 맞기 직전,
    #         흡혈은 HP 60% 아래, 강타는 상대 HP 80% 위. 암살에게는 철벽만(밖에서 은신을 못 본다).
    assassin = m.trait == ASSASSIN
    live = skill != SKILL_BOT or boss or p.hp < BOT_EMERGENCY_HP * p.max_hp
    smash, guard, drain = kit

    def used():
        return (kit[0] and not smash, kit[1] and not guard, kit[2] and not drain)

    def hit_with(ratio, heal):
        dealt = skill_damage(p, m, ratio)
        if heal and dealt > 0:
            p.hp = min(p.max_hp, p.hp + round(dealt))

    def between(s_next):
        """걸음 사이(Update) — 지금 쓸 스킬을 쓴다. 승부가 나면 finish 의 결과, 아니면 None."""
        nonlocal smash, drain
        p.shield = pd_up < s_next
        open_ = not assassin or (skill == SKILL_TIMED and not m.stealth)
        if smash and open_ and (skill != SKILL_BOT or m.hp > BOT_SMASH_HP * m.max_hp):
            hit_with(SMASH_RATIO, False)
            smash = False
        if drain and open_:
            want = max(1.0, round(p.atk * DRAIN_RATIO - m.dfn))
            if (skill == SKILL_TIMED and p.max_hp - p.hp >= want) or \
                    (skill == SKILL_BOT and p.hp < BOT_DRAIN_HP * p.max_hp):
                hit_with(DRAIN_RATIO, True)
                drain = False
        if p.hp <= 0:
            return finish(s_next, False)
        if m.hp <= 0:
            return finish(s_next, True)
        return None

    if skill == SKILL_MASH:
        if guard:
            pd_up = -1
            p.shield = True
        if smash:
            hit_with(SMASH_RATIO, False)
        if drain:
            hit_with(DRAIN_RATIO, True)
        smash = drain = guard = False
        if m.hp <= 0:
            return finish(0, True)
    elif live:
        done = between(0)
        if done:
            return done

    while True:
        s = pa if pa <= ma else ma
        if s >= MAX_STEPS:
            return (False, start - p.hp, None, MAX_STEPS, swings, False, used())
        if pa == s:
            m.shield, p.shield = md_up < s, pd_up < s
            if _strike(p, m, _next_crit(p)):
                md_up = s + md_cd
            swings += 1
            if p.hp <= 0:
                return finish(s, False)
            if m.hp <= 0:
                return finish(s, True)
            pa += p_cd
        if ma == s:
            if live and guard:
                # 철벽: 맞기 직전에 게이지를 채운다(Guard(true)). 봇은 게이지만 보고 누르므로 서 있어도 쓴다.
                if pd_up > s:
                    pd_up = s - 1
                    guard = False
                elif skill == SKILL_BOT:
                    guard = False
            p.shield = pd_up <= s
            if _strike(m, p, _next_crit(m)):
                pd_up = s + pd_cd
            if p.hp <= 0:
                return finish(s, False)
            if m.hp <= 0:
                return finish(s, True)
            ma += m_cd
            if not live and p.hp < BOT_EMERGENCY_HP * p.max_hp:
                live = True
        if live and (smash or drain):
            done = between(s + 1)
            if done:
                return done


@functools.lru_cache(maxsize=1 << 18)
def price(stats_key, mkey, carry):
    """스킬 없이 다음에 이놈과 싸우면 — 맵 위 예측(BattleForecast)과 같은 값. 결과를 캐시한다."""
    ps = dict(zip(STAT_KEYS, stats_key))
    return fight(ps, MONSTER_BY_KEY[mkey], carry)


STAT_KEYS = ("atk", "dfn", "aspd", "dspd", "crit", "crit_atk")
MONSTER_BY_KEY = {}             # (표, id) -> 몬스터 줄. World 가 채운다


# ---------------------------------------------------------------- 검증용: BattleStepper.Step 을 줄 그대로

def _fight_stepper(ps, md, carry):
    """C# BattleStepper 를 float 타이머 그대로 옮긴 것(느리다 — --check 전용). 스킬은 없다."""
    dt = _f32(FIXED_DT)

    def period(speed):
        return _f32(3.0 / _f32(speed)) if speed > 0 else math.inf

    p = Creature(PRICE_HP, ps["atk"], ps["dfn"], ps["aspd"], ps["dspd"], ps["crit"], ps["crit_atk"])
    m = creature_of(md)
    hits, k, shield = carry
    p.atk_count = hits
    pp, mp_, pd, md_ = period(p.aspd), period(m.aspd), period(p.dspd), period(m.dspd)
    pat = mat = mdt = 0.0
    pdt = pd if shield else _ELAPSED[k]
    p.shield = shield
    m.shield = False
    if m.trait == GUARDIAN:
        m.shield, mdt = True, md_
    t, step = 0.0, 0
    while t < 600.0:
        if pat >= pp:
            pat = 0.0
            if _strike(p, m, _next_crit(p)):
                mdt = 0.0
            if p.hp <= 0 or m.hp <= 0:
                break
        if pdt >= pd:
            p.shield = True
            pdt = pd
        if mat >= mp_:
            mat = 0.0
            if _strike(m, p, _next_crit(m)):
                pdt = 0.0
            if p.hp <= 0 or m.hp <= 0:
                break
        if mdt >= md_:
            m.shield = True
            mdt = md_
        pat, mat, pdt, mdt = _f32(pat + dt), _f32(mat + dt), _f32(pdt + dt), _f32(mdt + dt)
        t = _f32(t + dt)
        step += 1
    killed = m.hp <= 0 and p.hp > 0
    if not killed:
        return False, PRICE_HP - p.hp, None, step
    return True, PRICE_HP - p.hp, (p.atk_count, pdt, p.shield), step


def check_engine(world, sample=None, rng_seed=7):
    """사건 단위 시계가 (1) simulate_battle 과 모든 몬스터·설계 상태에서, (2) BattleStepper 의 float 시계와
    무작위 이어 받기 상태에서 같은지 본다. 어긋나면 AssertionError."""
    rng = random.Random(rng_seed)
    rows = [md for md in MONSTER_BY_KEY.values() if md.get("_floor")]
    if sample:
        rows = rng.sample(rows, min(sample, len(rows)))
    n1 = n2 = 0
    for md in rows:
        lv = G.target_level(md["_floor"], world.start[0]) + (1 if md["_boss"] else 0)
        ps = G.stats_with_runes(world.ptable, lv, md["_floor"])
        won, dur, loss = simulate_battle(G.player_with_runes(world.ptable, lv, md["_floor"]), creature_of(md))
        k, dmg, _, steps, _, _ = fight(ps, md)
        assert k == won and dmg == loss and abs(steps * FIXED_DT - dur) < 1e-9, (md["id"], won, dur, loss, k, dmg, steps)
        assert round(loss) == round(md["ParLoss"]), (md["id"], loss, md["ParLoss"])
        n1 += 1
        for _ in range(3):
            pd_cd = steps_of(ps["dspd"])
            carry = (rng.randrange(0, 20), rng.randrange(0, pd_cd + 1), rng.random() < 0.3)
            if carry[2]:
                carry = (carry[0], pd_cd, True)
            q = dict(ps, atk=ps["atk"] + rng.randrange(-6, 10), dfn=ps["dfn"] + rng.randrange(-6, 6))
            a = fight(q, md, carry)
            b = _fight_stepper(q, md, carry)
            assert a[0] == b[0] and a[1] == b[1], (md["id"], carry, a, b)
            # 스킬 쪽 시계도 같은 시계다 — 쓸 스킬이 하나도 없으면 결과가 같아야 한다.
            c = fight(q, md, carry, hp=PRICE_HP, max_hp=PRICE_HP, skill=SKILL_TIMED, kit=(False, False, False))
            assert c[:6] == a, (md["id"], carry, a, c)
            if a[0]:
                h, kk, up = a[2]
                assert h == b[2][0] and up == b[2][2], (md["id"], carry, a, b)
                assert up or _ELAPSED[kk] == b[2][1], (md["id"], carry, a, b)
            n2 += 1
    return n1, n2


# ---------------------------------------------------------------- 세계 (표 + 실제 층 격자)

TABLES = {"normal": "MonsterData", "tower": G.TOWER_TABLE}
MAIN_KEY = {"I_00": 0, "I_01": 1, "I_02": 2}


class World:
    """한 번 읽어 두는 것들. 작업 프로세스마다 하나씩 짓는다.
    mob_loss 를 주면 같은 지도에 그 손실로 푼 몬스터 표를 하나 더 지어 "whatif" 로 둔다(generate_content.build_monsters
    를 메모리에서만 돌린다 — 파일은 안 쓴다). 탑의 법 표가 같은 길로 나온다."""

    def __init__(self, mob_loss=None):
        self.ptable = G.extend_player_table(
            G.load_player_table(os.path.join(G.EXCEL, "PlayerData.csv")), G.MAX_LEVEL_TABLE)
        self.start, err = G.simulate_handmade(self.ptable)
        assert err is None, err
        self.items = {r["id"]: r for r in _json("ConsumableItemData")["consumableItems"]}
        self.stages = {r["id"]: r for r in _json("StageInfoData")["stageInfos"]}
        self.tables = {}
        for name, fn in TABLES.items():
            rows = {}
            for md in _json(fn)["creatures"]:
                md = dict(md)
                if md["id"] >= G.MOB_ID_BASE:
                    md["_floor"], md["_boss"] = (md["id"] - G.MOB_ID_BASE) // 8, False
                elif G.BOSS_ID_BASE <= md["id"] < G.BOSS_ID_BASE + 5:
                    md["_floor"], md["_boss"] = (md["id"] - G.BOSS_ID_BASE + 1) * G.FLOORS_PER_CHAPTER, True
                md["_key"] = (name, md["id"])
                md["_par"] = round(md.get("ParLoss", 0.0))     # Mathf.RoundToInt — 은행가 반올림
                rows[md["id"]] = md
                MONSTER_BY_KEY[md["_key"]] = md
            self.tables[name] = rows
        if mob_loss:
            rows = {}
            for m in G.build_monsters(self.ptable, self.start[0], mob_loss=list(mob_loss)):
                md = {k: v for k, v in m.items() if not k.startswith("_")}
                md.update(_floor=m["_floor"], _boss=m["_boss"], _key=("whatif", m["id"]), _par=round(m["ParLoss"]))
                rows[md["id"]] = md
                MONSTER_BY_KEY[md["_key"]] = md
            self.tables["whatif"] = rows
        self.specs = {f: floor_spec(self, f) for f in range(G.HANDMADE_FLOORS + 1, G.TOTAL_FLOORS + 1)}

    def gain(self, code):
        """셀 코드 'I_14' -> ('atk'|'dfn'|'hp', 양) 룬, ('heal', 비율) 물약, ('key', 색) 열쇠."""
        i = int(code[2:4])
        r = self.items[i]
        if i < 3:
            return ("key", i)
        if r["Heal"] > 0:
            return ("heal", r["Heal"] / 100.0)
        for stat, col in (("atk", "AttackUp"), ("dfn", "DefenceUp"), ("hp", "HPUp")):
            if r[col] > 0:
                return (stat, r[col])
        raise ValueError(code)


def _json(name):
    import json
    with open(os.path.join(G.JSOND, name + ".json"), encoding="utf-8") as f:
        return json.load(f)


def _grid(floor):
    did = G.dungeon_id(floor)[0]
    with open(os.path.join(G.STREAM, f"Dungeon_{did}.csv"), encoding="utf-8-sig") as f:
        return [[c.strip() for c in line.split(",")] for line in f.read().splitlines()]


def floor_spec(world, floor):
    """그 층의 격자에서 놀이에 필요한 것만 뽑는다.

      regions[r]  forced  반드시 치르는 몬스터 id (막으면 열쇠·문·계단에 못 닿는다 — layout_gen._unavoidable_mob)
                  side    [(id, 덤)] 곁길 몬스터. 덤은 그놈이 지키는 골방 안쪽(('key', 색) / ('heal', 비율)) 또는 None
                  pots    [(회복, 밟고 지나가야 하나)] 그 구역에서 지킴 없이 닿는 물약 (보스층 구역3 은 보스 앞 것만)
      after_boss  보스를 잡아야 닿는 물약
      rune        계단 방 입구의 룬 (반드시 밟는다 — 보스층은 보스 앞이다)
      vault       (색, 룬) — 골방 입구가 네 번째 문
      choice      (덤, 덤, 'region'|'stairs') 둘 중 하나. 'stairs' 는 보스 뒤
    """
    g = _grid(floor)
    spawn, up = L._endpoints(g)
    cells = {(x, y): c for y, row in enumerate(g) for x, c in enumerate(row)}
    doors = [c for c, v in cells.items() if v in L.DOOR_CELLS]
    main = sorted((d for d in doors if L._cuts_path(g, spawn, up, d)), key=lambda d: (int(cells[d]) - 3) % 3)
    assert len(main) == 3, (floor, main)
    vault_doors = [d for d in doors if d not in main]
    region = {}
    for r in range(4):
        for c in L._flood_locked(g, spawn, set(main[r:])):
            region.setdefault(c, r)
    boss_cell = next((c for c, v in cells.items() if v.startswith("B_")), None)
    portals = {c for c, v in cells.items() if v in L.PORTAL_CELLS}
    before_boss = L._flood_locked(g, spawn, {boss_cell} | portals) if boss_cell else None

    def passable_nbrs(c):
        return [(c[0] + dx, c[1] + dy) for dx, dy in L.NEIGHBORS
                if L._inside((c[0] + dx, c[1] + dy)) and L._passable(g, (c[0] + dx, c[1] + dy))]

    # 골방 안쪽(막다른 칸의 아이템)과 그 입구(파수꾼 또는 금고 문)
    prize_of = {}
    for c, v in cells.items():
        if v.startswith("I_") and not v.endswith("~"):
            nb = passable_nbrs(c)
            if len(nb) == 1:
                prize_of[nb[0]] = c
    taken = set(prize_of.values())

    spec = dict(floor=floor, band=G.band_of(floor), chapter=(floor - 1) // G.FLOORS_PER_CHAPTER,
                regions=[dict(forced=[], side=[], pots=[]) for _ in range(4)],
                boss=None, after_boss=[], rune=None, vault=None, choice=None)
    for c, v in cells.items():
        if v.startswith("M_"):
            mid = int(v[2:])
            r = spec["regions"][region[c]]
            if L._unavoidable_mob(g, main, c):
                r["forced"].append(mid)
            else:
                r["side"].append((mid, world.gain(cells[prize_of[c]]) if c in prize_of else None))
        elif v.startswith("B_"):
            spec["boss"] = int(v[2:])
        elif v.startswith("I_") and c not in taken and v not in MAIN_KEY and not v.endswith("~"):
            kind = world.gain(v)
            if kind[0] == "heal":
                if before_boss is not None and c not in before_boss:
                    spec["after_boss"].append(kind[1])
                else:
                    spec["regions"][region[c]]["pots"].append((kind[1], L._cuts_path(g, spawn, up, c)))
            elif v in ("I_09", "I_10", "I_11"):
                assert L._cuts_path(g, spawn, up, c), (floor, v, "룬을 지나칠 수 있다")
                spec["rune"] = kind
            else:
                raise AssertionError((floor, v, "예상 못 한 아이템"))
    for r in spec["regions"]:
        r["forced"].sort()
        r["side"].sort()
        r["pots"].sort()
    for d in vault_doors:
        inner = [n for n in passable_nbrs(d) if n in taken]
        assert len(inner) == 1, (floor, d)
        spec["vault"] = ((int(cells[d]) - 3) % 3, world.gain(cells[inner[0]]))
    pair = [(c, v) for c, v in cells.items() if v.endswith("~")]
    if pair:
        behind = boss_cell is not None and all(c not in before_boss for c, _ in pair)
        a, b = sorted(v[:-1] for _, v in pair)
        spec["choice"] = (world.gain(a), world.gain(b), "stairs" if behind else "region")
    _check_spec(world, spec)
    return spec


def _check_spec(world, spec):
    """격자에서 읽은 것이 생성기의 계획(floor_potions·alcove_plan·choice_pair·rune_of)과 같은가.
    어긋나면 CSV 가 옛것이거나 생성기가 바뀐 것이다 — 그대로 돌리면 엉뚱한 탑을 잰다."""
    f = spec["floor"]
    pots = sorted([h for r in spec["regions"] for h, _ in r["pots"]] + spec["after_boss"])
    want = sorted(G.floor_potions(f) + [G.EXIT_POTION] + (G.BOSS_FLOOR_POTIONS if G.is_boss_floor(f) else []))
    assert pots == want, (f, pots, want)
    assert spec["rune"] == world.gain(G.rune_of(f)), (f, spec["rune"])
    plan = G.alcove_plan(f)
    loot = [l for r in spec["regions"] for _, l in r["side"] if l]
    if plan is None:
        assert all(l[0] == "heal" for l in loot) and spec["vault"] is None, (f, loot)
    elif plan[0] == "key":
        assert loot == [("key", plan[1])], (f, loot, plan)
    else:
        assert spec["vault"] == (plan[1], world.gain(plan[2])), (f, spec["vault"], plan)
    cp = G.choice_pair(f)
    if cp:
        got = spec["choice"]
        assert got and {got[0], got[1]} == {world.gain(cp[0]), world.gain(cp[1])} and got[2] == cp[2], (f, got, cp)
    else:
        assert spec["choice"] is None, f
    n = sum(len(r["forced"]) + len(r["side"]) for r in spec["regions"])
    assert n == G.MOBS_PER_FLOOR, (f, n)
    assert (spec["boss"] is not None) == G.is_boss_floor(f), f


# ---------------------------------------------------------------- 사람 (습관)

class Persona:
    """한 판의 습관. 판마다 무작위로 뽑는다(sample_persona).

    판단 하나하나의 주사위는 (씨앗, 층, 무엇) 으로 정해진다(roll). 그래서 판단 하나만 바꿔 다시 돌려도
    (선택의 무게 — counterfactual) 다른 판단은 그대로다.

      potion    greedy 보이는 대로 / threshold HP 가 pot_t 아래면 / needed 예측상 싸운 뒤 pot_t 아래로 떨어지면
                (맵 위 예측을 보는 사람) / topup 봇(AutoPlayer: 70% 아래면 먼저, 90% 아래면 싸운 뒤에). 넷 다 예측이
                ✖(지는 싸움)면 마신다. sweep 이면 층을 떠나기 전에 남은 것을 다 마신다(다음 층으로 못 가져간다).
      lazy      막다른 길의 물약을 안 들르는 몫
      side      all / none / random(side_p) / cheap(예측이 최대 HP 의 side_cap 이하만) / loot(골방을 지키는 놈만 + 가끔)
      vault     open 열쇠가 있으면 연다 / pick vault_stats 의 룬만 / never. backfill 이면 챕터 끝에 워프해 남은 열쇠로 연다
      treasure  보물 층 둘 중 하나: rune / potion / hp(체력이 treasure_t 아래면 물약)
      bosspick  40·60·80층 보스 뒤 둘 중 하나: atk / dfn / random
      altar     never / atk / dfn / alt(번갈아) / rich(사고도 altar_t 이상 남으면, altar_stat) / random
      skill     none / mash / timed / bot (fight 의 머리말)
    """

    def __init__(self, seed, name="", **kw):
        self.seed, self.name = seed, name
        self.potion, self.pot_t, self.sweep, self.lazy = "greedy", 0.5, True, 0.0
        self.side, self.side_p, self.side_cap = "all", 1.0, 0.08
        self.vault, self.vault_stats, self.backfill = "open", ("atk", "dfn", "hp"), False
        self.treasure, self.treasure_t, self.bosspick = "rune", 0.5, "atk"
        self.altar, self.altar_t, self.altar_stat = "never", 0.6, "atk"
        self.skill = SKILL_NONE
        for k, v in kw.items():
            assert hasattr(self, k), k
            setattr(self, k, v)

    def roll(self, *key):
        return random.Random(f"{self.seed}:{key}").random()

    def but(self, **changes):
        kw = {k: v for k, v in self.__dict__.items() if k not in ("seed", "name")}
        kw.update(changes)
        return Persona(self.seed, self.name, **kw)

    def label(self):
        pot = self.potion + (f"{self.pot_t:.2f}" if self.potion in ("threshold", "needed") else "")
        return (f"물약 {pot}{'' if self.sweep else '·안쓸어'}{f'·게으름{self.lazy:.1f}' if self.lazy else ''} | "
                f"곁길 {self.side} | 금고 {self.vault}{'+워프' if self.backfill else ''} | 보물 {self.treasure} | "
                f"보스룬 {self.bosspick} | 제단 {self.altar} | 스킬 {self.skill}")


def sample_persona(seed):
    """그럴듯한 사람 하나. 가중치는 짐작이다 — 보고서는 습관별로 갈라서도 보여 준다."""
    r = random.Random(seed)

    def pick(*table):
        x = r.random() * sum(w for _, w in table)
        for v, w in table:
            x -= w
            if x < 0:
                return v
        return table[-1][0]
    P = Persona(seed)
    P.potion = pick(("greedy", .30), ("threshold", .30), ("needed", .25), ("topup", .15))
    P.pot_t = {"threshold": r.uniform(.4, .7), "needed": r.uniform(.0, .15)}.get(P.potion, .7)
    P.sweep = P.potion == "greedy" or r.random() < .85
    P.lazy = 0.0 if r.random() < .75 else r.uniform(.2, .6)
    P.side = pick(("all", .45), ("cheap", .25), ("loot", .12), ("random", .13), ("none", .05))
    P.side_p, P.side_cap = r.uniform(.3, .9), r.uniform(.03, .10)
    P.vault = pick(("open", .55), ("pick", .25), ("never", .20))
    P.vault_stats = tuple(s for s in ("atk", "dfn", "hp") if r.random() < .5) or (r.choice(("atk", "dfn", "hp")),)
    P.backfill = P.vault != "never" and r.random() < .35
    P.treasure = pick(("rune", .5), ("potion", .15), ("hp", .35))
    P.bosspick = pick(("atk", .45), ("dfn", .45), ("random", .10))
    P.altar = pick(("never", .25), ("atk", .2), ("dfn", .2), ("alt", .1), ("rich", .2), ("random", .05))
    P.altar_t, P.altar_stat = r.uniform(.55, .8), r.choice(("atk", "dfn"))
    P.skill = pick((SKILL_NONE, .35), (SKILL_MASH, .25), (SKILL_TIMED, .25), (SKILL_BOT, .15))
    return P


# 고정된 사람들 — 무작위 판과 따로 한 번씩 돈다.
REFERENCE = {
    # AutoPlayer 그대로: 다 잡고, 70% 아래면 물약 먼저·다 비우면 남은 것을 마시고, 열쇠가 있으면 금고를 열고
    # (PathMover.UseKeys), 스킬은 보스(와 HP 35% 아래)에서만, 제단은 늘 지나간다. 보물 층의 둘 중 하나는 물약이다 —
    # 층을 비우면 남은 물약을 룬보다 먼저 줍고(PriTopUp < PriRune), 그 물약이 짝인 룬을 지운다.
    # v1 의 봇 완주(치름 27,654 · 기준 46,693 · 별 22223322333333333333)와 견줄 기준이다.
    "봇": dict(potion="topup", pot_t=.7, side="all", vault="open", treasure="potion",
              bosspick="random", altar="never", skill=SKILL_BOT),
    # 생성기의 정답 경로(simulate_run 기본): 다 잡고, 구역 물약은 죽을 때만 마시고 남으면 두고 간다(계단 쪽 물약은
    # 마신다). 덤(금고·둘 중 하나·제단)은 안 줍는다.
    "정답 경로": dict(potion="needed", pot_t=0.0, sweep=False, side="all", vault="never", treasure="none",
                  bosspick="none", altar="never", skill=SKILL_NONE),
    # 덤까지 챙기는 정답 경로: 보이는 대로 마시고, 다 잡고, 열쇠가 있으면 금고를 열고, 둘 중 하나는 룬. 제단·스킬은 없다.
    # 정답 경로보다 늘 세다 — 완주 보장을 이 사람으로 잡으면 후반 값을 올릴 자리가 생긴다([9]).
    "덤 챙김": dict(potion="greedy", side="all", vault="open", treasure="rune", bosspick="dfn", altar="never"),
    # 점수를 노리는 사람(스킬 없이): 다 잡고, 공격에 다 걸고, 예측을 보며 마신다.
    "점수꾼": dict(potion="needed", pot_t=.05, side="all", vault="open", backfill=True, treasure="rune",
                bosspick="atk", altar="atk", skill=SKILL_NONE),
    "점수꾼+스킬": dict(potion="needed", pot_t=.05, side="all", vault="open", backfill=True, treasure="rune",
                    bosspick="atk", altar="atk", skill=SKILL_TIMED),
    # 대충 하는 사람: 보이는 대로 마시고 곁길은 반쯤, 덤은 안 챙긴다.
    "대충": dict(potion="greedy", side="random", side_p=.5, vault="never", treasure="potion",
               bosspick="random", altar="never", skill=SKILL_NONE),
}


# ---------------------------------------------------------------- 한 판

@functools.lru_cache(maxsize=None)
def _level_stats(world_id, level):
    """레벨 능력치 + 에고소드(3층 계약부터 끝까지). 5층부터라 검은 늘 에고소드다."""
    return G.with_equip(G.player_stats_at(_WORLDS[world_id].ptable, level), G.sword_on(G.HANDMADE_FLOORS + 1))


_WORLDS = {}


class Died(Exception):
    pass


class Run:
    """한 판. 상태는 전부 이 객체에 있고 층 입구마다 통째로 떠 둔다(체크포인트)."""

    LIST_FIELDS = ("keys", "skipped", "stars", "bands", "fights", "lows", "floor_log", "deaths",
                   "decisions", "potlog", "left", "extras", "skipped_mobs")

    def __init__(self, world, table, persona, record_decisions=False, overrides=None, density=False,
                 skill_scope="floor"):
        _WORLDS[id(world)] = world
        self.W, self.wid, self.table, self.P = world, id(world), table, persona
        self.mons = world.tables[table]
        self.level, self.exp, self.hp = world.start         # 5층 입구 (simulate_handmade)
        self.bonus = {"atk": 0.0, "dfn": 0.0, "hp": 0.0}
        self.extra_atk = 0.0            # 크기 룬·제단으로 얹은 공격 (계단 룬 밖의 것) — 임계 측정
        self.carry = FRESH
        self.keys = [0, 0, 0]           # 여분 열쇠 (큰길 열쇠는 그 층 문에 쓴다)
        self.skipped = []               # 못/안 연 금고 (층, 색, 룬)
        self.altar_n = 0
        self.paid = self.par = self.foretold = self.altar_paid = 0
        self.band_paid = self.band_par = 0
        self.stars, self.bands = [], []                 # 띠마다 별, (띠, 치름, 기준)
        self.fights = []                # (층, 보스, 잃은 HP / 최대 HP, 휘두름, 임계를 넘겼나(-1 모름/0/1), +3 이면 덜 휘두르나)
        self.lows = []                  # (층, 전투 직후 HP / 최대 HP)
        self.floor_log = []             # (층, 층 끝 HP, 최대 HP, 치름+제단 누계)
        self.deaths = []                # 죽은 층
        self.decisions = []             # (층, 키, 고른 것, 고를 수 있던 것) — record_decisions 일 때
        self.potlog = []                # (층, 회복량, 넘친 양, 쓰지 않고 둔 양)
        self.left = []                  # 지나온 층에 두고 온 물약 (층, 회복) — 막히면 내려가서 마신다
        self.retreats = 0               # 그렇게 내려간 횟수
        self.skipped_mobs = []          # 지나친 곁길 몬스터 (층, id, 덤) — 그 층에 남아 있다. 막히면 내려가서 잡는다
        self.grinds = 0                 # 그렇게 내려가 잡은 횟수
        self.extras = []                # 챕터 끝마다 (층, 공격, 방어, 체력) — 계단 룬 밖으로 얹은 것
        self.stuck = None
        self.rescues = 0                # 두 번 죽은 층에서 스킬을 꺼내 든 횟수 (스킬 안 쓰는 사람)
        self.overrides = overrides or {}
        self.record_decisions = record_decisions
        self.density = density          # +3 공격 임계 측정(전투마다 한 번 더 잰다)
        self.mode = "normal"
        self.snaps = {}
        # 스킬이 언제 다시 차나 — "floor" 처음 오는 층마다(지금 게임), "battle" 전투마다(예전 규칙, [8] 에서 견준다).
        self.skill_scope = skill_scope
        self.kit = (True, True, True)
        self.kit_target = None          # 층마다 한 번이면 때를 맞추는 사람은 그 층에서 가장 비싼 싸움에 아낀다

    # ---- 상태
    def snapshot(self):
        d = {k: v for k, v in self.__dict__.items() if k not in ("snaps",)}
        d["bonus"] = dict(self.bonus)
        for k in self.LIST_FIELDS:
            d[k] = list(getattr(self, k))
        return d

    def restore(self, snap, keep_logs=("deaths",)):
        kept = {k: getattr(self, k) for k in keep_logs}
        for k, v in snap.items():
            setattr(self, k, list(v) if k in self.LIST_FIELDS else dict(v) if k == "bonus" else v)
        for k, v in kept.items():
            setattr(self, k, v)

    def stats(self):
        s = dict(_level_stats(self.wid, self.level))
        for k, v in self.bonus.items():
            s[k] += v
        return s

    def decide(self, floor, key, default, options):
        k = (floor,) + key
        choice = self.overrides.get(k, default)
        if self.record_decisions and self.mode == "normal":
            self.decisions.append((floor, key, choice, tuple(options)))
        return choice

    # ---- 물약
    def drink(self, heal, floor, log=True):
        mx = self.stats()["hp"]
        amount = round(heal * 100.0 * mx / 100.0)      # ConsumableItem.PickUp: Mathf.Round(Heal * MaxHP / 100)
        before = self.hp
        self.hp = min(mx, self.hp + amount)
        if log:
            self.potlog.append((floor, self.hp - before, amount - (self.hp - before), 0.0))

    def leave(self, pots, floor):
        """층을 떠난다 — 안 마신 물약은 그 층에 남는다(다음 층으로 못 가져간다). pots 는 (회복, 구역).
        쓸어 담는 사람은 다 마시고, 아니면 계단 쪽(구역 3·보스 뒤)에 놓인 것만 마신다 — 정답 경로(simulate_run)도
        계단 앞 물약은 늘 마신다."""
        if not pots:
            return
        sweep = self.P.sweep or self.mode != "normal"
        mx = self.stats()["hp"]
        for h, region in sorted(pots):
            if sweep or region >= 3:
                self.drink(h, floor)
            else:
                self.potlog.append((floor, 0.0, 0.0, round(h * mx)))
                self.left.append((floor, h))
        pots.clear()

    def pre_fight(self, pots, pr, floor):
        """싸우기 전 — 습관대로 마신다. pr 은 스킬 없는 예측(맵 위 숫자)."""
        if not pots:
            return
        mx = self.stats()["hp"]
        kind, t = self.P.potion, self.P.pot_t
        if self.mode != "normal":
            kind = "greedy"
        while pots:
            lethal = not pr[0] or pr[1] >= self.hp
            if kind == "greedy":
                want = True
            elif kind == "threshold":
                want = lethal or self.hp < t * mx
            elif kind == "needed":
                want = lethal or self.hp - pr[1] < t * mx
            else:                                   # topup — AutoPlayer: 70% 아래면 몬스터보다 물약이 먼저
                want = lethal or self.hp < t * mx or self.hp - pr[1] < 0.35 * mx
            if not want:
                return
            pots.sort()
            self.drink(pots.pop(0)[0], floor)

    # ---- 싸움
    def price(self, md, st=None, datk=0.0):
        st = st or self.stats()
        key = (st["atk"] + datk, st["dfn"], st["aspd"], st["dspd"], st["crit"], st["crit_atk"])
        return price(key, md["_key"], self.carry)

    def battle(self, md, floor, pots, boss=False, past=False):
        """한 판 싸운다. 지면 Died. past 면 지난 층에 내려가 싸운 것 — 장부에는 치른 값만 들고(기준·띠 합 밖),
        스킬은 안 쓴다(이 층 몫을 아낀다)."""
        st = self.stats()
        pr = self.price(md, st)
        self.pre_fight(pots, pr, floor)
        mx = st["hp"]
        hp0 = self.hp
        skill = SKILL_NONE if past else self.P.skill
        kit = (True, True, True)
        if self.skill_scope == "floor":
            kit = self.kit
            if not any(kit) or (skill == SKILL_TIMED and md["id"] != self.kit_target):
                skill = SKILL_NONE
        if skill == SKILL_NONE or (skill == SKILL_BOT and not boss and pr[0] and hp0 - pr[1] >= BOT_EMERGENCY_HP * mx):
            res, won = pr, pr[0] and pr[1] < hp0
        else:
            res = fight(st, md, self.carry, hp=hp0, max_hp=mx, skill=skill, boss=boss, kit=kit)
            won = res[0]
            if self.skill_scope == "floor":
                self.kit = tuple(k and not u for k, u in zip(kit, res[6]))
        if not won:
            raise Died(floor)
        loss = res[1]
        self.hp = hp0 - loss
        self.carry = res[2]
        par = md["_par"]
        if par > 0:                     # SwordLedger.OnBattleEnded — 기준이 없는 싸움은 안 적는다
            paid = max(0, round(loss))
            self.paid += paid
            self.foretold += max(0, round(pr[1]))
        if par > 0 and not past:
            self.par += par
            self.band_paid += paid
            self.band_par += par
        # 임계: 크기 룬·제단으로 얹은 공격이 실제로 한 대를 덜게 했나 (스킬 없는 예측끼리 견준다)
        bp = -1
        if self.extra_atk > 0:
            lower = self.price(md, st, -self.extra_atk)
            bp = int(lower[0] and lower[4] > pr[4])
        near = -1
        if self.density:
            up3 = self.price(md, st, 3.0)
            near = int(up3[4] < pr[4])
        self.fights.append((floor, boss, loss / mx, pr[4], bp, near))
        self.lows.append((floor, self.hp / mx))
        self.exp += md["RewardExp"]     # StageInfoData.EXP 는 생성 층 전부 100 — 배율 1
        pt = self.W.ptable
        while self.level + 1 in pt and self.exp >= pt[self.level + 1]["need_exp"]:
            self.exp -= pt[self.level + 1]["need_exp"]
            self.level += 1
            self.hp += pt[self.level]["hp"]             # GameManager.LevelUp — 지금 체력도 같이

    def take(self, gain):
        """룬(계단·크기·제단)을 얹는다. 체력 룬은 지금 체력도 올린다(ConsumableItem.PickUp)."""
        stat, amount = gain
        self.bonus[stat] += amount
        if stat == "hp":
            self.hp += amount

    # ---- 판단
    def want_side(self, floor, i, md, loot, pr):
        P = self.P
        mx = self.stats()["hp"]
        lethal = not pr[0] or pr[1] >= self.hp
        if self.mode != "normal":
            # 다시 하는 판 — 물약을 지키는 놈만, 그 물약보다 싸면.
            default = bool(loot and loot[0] == "heal" and pr[1] < loot[1] * mx and not lethal)
        elif lethal:
            default = False
        elif P.side == "all":
            default = True
        elif P.side == "none":
            default = False
        elif P.side == "random":
            default = P.roll(floor, "side", i) < P.side_p
        elif P.side == "cheap":
            default = pr[1] <= P.side_cap * mx
        else:                                           # loot
            default = loot is not None or P.roll(floor, "side", i) < 0.3
        return self.decide(floor, ("side", i), default, (True, False))

    def want_vault(self, floor, color, gain):
        P = self.P
        if self.keys[color] <= 0:
            return False
        default = P.vault == "open" or (P.vault == "pick" and gain[0] in P.vault_stats)
        return self.decide(floor, ("vault",), default, (True, False))

    def choose_pair(self, floor, a, b, where):
        P = self.P
        if where == "region":
            pot, rune = (a, b) if a[0] == "heal" else (b, a)
            mx = self.stats()["hp"]
            default = {"rune": rune, "potion": pot, "hp": pot if self.hp < P.treasure_t * mx else rune,
                       "none": None}[P.treasure]
            opts = (rune, pot)
        else:
            atk, dfn = (a, b) if a[0] == "atk" else (b, a)
            default = {"atk": atk, "dfn": dfn, "none": None,
                       "random": atk if P.roll(floor, "boss") < 0.5 else dfn}[P.bosspick]
            opts = (atk, dfn)
        return self.decide(floor, ("pair",), default, opts)

    def altar(self, floor):
        """띠의 마지막 층 계단 — SwordLedger.Altar / BuyAltar 그대로."""
        info = self.W.stages.get(floor - 1)
        if not info or info["AltarPrice"] <= 0 or (info["AltarAtk"] <= 0 and info["AltarDef"] <= 0):
            return
        mx = self.stats()["hp"]
        n = self.altar_n + 1
        pct = info["AltarPrice"] + info["AltarPriceStep"] * n * (n - 1)
        reserve = info["AltarReserveTower"] if self.table == "tower" else info["AltarReserve"]
        price_hp = mx * pct / 100.0
        if not (self.hp - price_hp >= mx * reserve / 100.0):
            return
        cost = math.floor(price_hp)
        P = self.P
        default = None
        if P.altar in ("atk", "dfn"):
            default = P.altar
        elif P.altar == "alt":
            default = ("atk", "dfn")[self.altar_n % 2]
        elif P.altar == "rich":
            default = P.altar_stat if (self.hp - cost) >= P.altar_t * mx else None
        elif P.altar == "random":
            default = ("atk", "dfn")[P.roll(floor, "altar", 1) < .5] if P.roll(floor, "altar") < .5 else None
        choice = self.decide(floor, ("altar",), default, ("atk", "dfn", None))
        if self.mode != "normal" or choice is None:
            return
        self.hp -= cost
        gain = info["AltarAtk"] if choice == "atk" else info["AltarDef"]
        self.bonus[choice] += gain
        if choice == "atk":
            self.extra_atk += gain
        self.altar_n += 1
        self.paid += cost
        self.altar_paid += cost

    def backfill(self, floor):
        """워프석 반지(20층 보스)를 얻은 뒤 챕터 끝마다 — 지나친 금고를 남은 열쇠로 연다. 싸움은 없다."""
        if not self.P.backfill or floor < G.FLOORS_PER_CHAPTER:
            return
        for item in list(self.skipped):
            f, color, gain = item
            if self.keys[color] > 0 and (self.P.vault == "open" or gain[0] in self.P.vault_stats):
                self.keys[color] -= 1
                self.take(gain)
                if gain[0] == "atk":
                    self.extra_atk += gain[1]
                self.skipped.remove(item)

    def retreat(self, floor):
        """막혔다 — 아래층으로 내려가(20층 뒤로는 워프석 반지로) 두고 온 물약을 마시고 다시 올라온다. 층에 들어설 때마다
        체크포인트를 새로 쓰므로 그 체력으로 다시 시작한다. 가까운 층의 것부터, 가득 차면 그만.
        물약은 그때의 최대 HP 에 비례해 채운다(ConsumableItem.PickUp) — 늦게 마시는 쪽이 더 채운다."""
        mx = self.stats()["hp"]
        if not self.left or self.hp >= mx:
            return False
        self.retreats += 1
        for item in sorted(self.left, reverse=True):
            if self.hp >= mx:
                break
            f, h = item
            self.left.remove(item)
            before = self.hp
            self.drink(h, f, log=False)
            gained = self.hp - before
            self.potlog.append((f, gained, round(h * mx) - gained, -round(h * mx)))
        return True

    def grind(self, floor):
        """막혔다 — 내려가서(20층 뒤로는 워프) 지나쳤던 곁길 몬스터를 잡는다. 그 층에 그대로 남아 있다.
        경험치·레벨·덤을 얻고 장부에는 치른 값만 든다. 가까운 층부터, 두고 온 물약으로 채워 가며,
        예측상 이기고도 최대 HP 의 25% 가 남는 것만 잡는다(더 빠듯하게 싸우면 다시 할 층에 쓸 HP 를 써 버려 더 막혔다)."""
        fought = False
        for item in sorted(self.skipped_mobs, reverse=True):
            f, mid, loot = item
            md = self.mons[mid]
            self.retreat(floor)
            pr = self.price(md)
            if not pr[0] or self.hp - pr[1] < 0.25 * self.stats()["hp"]:
                continue
            self.skipped_mobs.remove(item)
            self.battle(md, f, [], past=True)
            fought = True
            if loot and loot[0] == "key":
                self.keys[loot[1]] += 1
            elif loot:
                self.drink(loot[1], f)
        self.grinds += fought
        return fought

    # ---- 층
    def play_floor(self, spec):
        f = spec["floor"]
        P = self.P
        pots = []
        if self.skill_scope == "floor":
            self.kit = (True, True, True)
            ids = [m for reg in spec["regions"] for m in reg["forced"] + [s for s, _ in reg["side"]]]
            self.kit_target = spec["boss"] or max(ids, key=lambda m: self.price(self.mons[m])[1])
        for r, reg in enumerate(spec["regions"]):
            for j, (h, forced) in enumerate(reg["pots"]):
                if forced:
                    self.drink(h, f)                    # 길목에 놓였다 — 밟고 지나간다
                elif self.mode != "normal" or self.decide(
                        f, ("pot", r, j), not (P.lazy and P.roll(f, "lazy", r, j) < P.lazy), (True, False)):
                    pots.append((h, r))
                else:
                    self.potlog.append((f, 0.0, 0.0, round(h * self.stats()["hp"])))
                    self.left.append((f, h))
            if P.potion == "greedy" or self.mode != "normal":
                for h, _ in sorted(pots):
                    self.drink(h, f)
                pots.clear()
            for mid in reg["forced"]:
                self.battle(self.mons[mid], f, pots)
            for i, (mid, loot) in enumerate(reg["side"]):
                md = self.mons[mid]
                if not self.want_side(f, (r, i), md, loot, self.price(md)):
                    self.skipped_mobs.append((f, mid, loot))
                    continue
                self.battle(md, f, pots)
                if loot and loot[0] == "key":
                    self.keys[loot[1]] += 1
                elif loot:
                    pots.append((loot[1], r))
                    if P.potion == "greedy" or self.mode != "normal":
                        self.drink(pots.pop()[0], f)
            if P.potion == "topup" and self.mode == "normal":
                # AutoPlayer: 몬스터를 다 잡고 나서(PriMonster < PriTopUp) 90% 아래면 작은 것부터 채운다.
                while pots and self.hp < 0.9 * self.stats()["hp"]:
                    pots.sort()
                    self.drink(pots.pop(0)[0], f)
            if r < 3:
                continue
            if spec["vault"]:
                color, gain = spec["vault"]
                if self.want_vault(f, color, gain):
                    self.keys[color] -= 1
                    self.take(gain)
                    if gain[0] == "atk":
                        self.extra_atk += gain[1]
                else:
                    self.skipped.append((f, color, gain))
            if spec["choice"] and spec["choice"][2] == "region":
                got = self.choose_pair(f, *spec["choice"])
                if got and got[0] == "heal":
                    pots.append((got[1], 3))
                    if P.potion == "greedy" or self.mode != "normal":
                        self.drink(pots.pop()[0], f)
                elif got:
                    self.take(got)
                    if got[0] == "atk":
                        self.extra_atk += got[1]
            if spec["boss"]:
                self.take(spec["rune"])                 # 계단 방 입구의 룬은 보스 앞이다
                self.battle(self.mons[spec["boss"]], f, pots, boss=True)
                if f == G.TOTAL_FLOORS:
                    return                              # 100층 보스 — 계단 없이 결말(StoryDirector.CoEnding)
                pots.extend((h, 4) for h in spec["after_boss"])
                if spec["choice"] and spec["choice"][2] == "stairs":
                    got = self.choose_pair(f, *spec["choice"])
                    if got:
                        self.take(got)
                        if got[0] == "atk":
                            self.extra_atk += got[1]
            else:
                self.take(spec["rune"])
        self.leave(pots, f)

    def close_band(self, floor):
        """SwordLedger.CoCloseBand — 띠의 마지막 층 위 계단을 처음 오를 때. 문턱은 그 층의 StageInfoData 열."""
        if floor % G.BAND_FLOORS:
            return
        row = self.W.stages.get(floor - 1, {})
        col = "Tower" if self.table == "tower" else ""
        self.stars.append(G_stars(self.band_paid, self.band_par,
                                  int(row.get("StarGreat" + col, 0)), int(row.get("StarGood" + col, 0))))
        self.bands.append((G.band_of(floor), self.band_paid, self.band_par))
        self.band_paid = self.band_par = 0

    def play(self, first=G.HANDMADE_FLOORS + 1, last=G.TOTAL_FLOORS):
        """first 층 입구에서 last 층 끝까지. 죽으면 층 입구로 돌아가 다시 한다(체크포인트)."""
        for f in range(first, last + 1):
            spec = self.W.specs[f]
            snap = self.snapshot()
            self.snaps[f] = snap
            # 죽으면 층 입구(체크포인트)에서 다시: 조심해서(물약은 보이는 대로·곁길은 물약 지키는 놈만) → 내려가서 두고 온
            # 물약을 마시고 → 그래도 안 되면 스킬을 꺼낸다(스킬 안 쓰는 사람). 다 안 되면 그 판은 거기서 막힌다.
            for attempt in ("normal", "careful", "retreat", "grind", "careful+"):
                self.mode = attempt
                try:
                    if attempt in ("retreat", "grind", "careful+") and not self.retreat(f) and attempt == "retreat":
                        continue
                    if attempt in ("grind", "careful+") and not self.grind(f) and attempt == "grind":
                        continue
                    if attempt == "careful+":
                        if self.P.skill != SKILL_NONE:
                            continue
                        self.P = self.P.but(skill=SKILL_TIMED)     # 막힌 사람은 스킬을 찾아낸다
                        self.rescues += 1
                    self.play_floor(spec)
                    break
                except Died:
                    self.restore(snap)
                    self.deaths.append(f)
            else:
                self.stuck = f
                self.mode = "normal"
                return False
            if attempt == "careful+":
                self.P = snap["P"]
            self.mode = "normal"
            self.close_band(f)
            if f < G.TOTAL_FLOORS:
                self.altar(f)
            if f % G.FLOORS_PER_CHAPTER == 0:
                self.backfill(f)
                # 계단 룬(완주 계산에 든 것) 밖으로 얹은 것 — 크기 룬·제단·둘 중 하나
                base = G.rune_bonus(f + 1)
                self.extras.append((f, *(self.bonus[k] - base[k] for k in ("atk", "dfn", "hp"))))
            mx = self.stats()["hp"]
            self.floor_log.append((f, self.hp, mx, self.paid))
        return True

    def score(self):
        """SwordLedger.Score — round(1000·치름/기준), 낮을수록 좋다."""
        return round(1000.0 * self.paid / self.par) if self.par > 0 else 0


def G_stars(paid, par, great=0, good=0):
    """SwordLedger.Stars — ★ 통과, ★★ 기준의 good% 이하, ★★★ 기준의 great% 이하(띠마다 StageInfoData 의 문턱).
    문턱이 없는 데이터면 예전 규칙(70%·100%). 정수 셈 그대로."""
    if great <= 0 or good <= 0:
        great, good = 70, 100
    return 0 if par <= 0 else 3 if paid * 100 <= par * great else 2 if paid * 100 <= par * good else 1


# ---------------------------------------------------------------- 선택의 무게 (판단 하나만 바꿔 다시 돌린다)

HORIZON = 5                     # 판단한 층부터 이만큼 위 층의 끝까지 견준다 (띠 하나)
WEIGHT_STEPS = (0.02, 0.05)     # "값이 걸린 판단" 의 문턱 — 최대 HP 대비


def weigh_decisions(world, table, persona, base):
    """base 판의 판단마다 다른 쪽을 골랐다면 — 그 층 입구(체크포인트)에서 다시 돌려 HORIZON 층 뒤의
    체력과 장부(치름+제단)를 견준다. [(층, 종류, 차이(최대 HP 대비), 죽었나)].
    주사위가 (씨앗, 층, 무엇) 으로 정해지므로 바꾼 판단 말고는 같은 사람이 같은 판단을 한다."""
    end_of = {f: (hp, mx, paid) for f, hp, mx, paid in base.floor_log}
    out = []
    for floor, key, chose, options in base.decisions:
        last = min(G.TOTAL_FLOORS, floor + HORIZON)
        if last not in end_of:
            continue
        b_hp, b_mx, b_paid = end_of[last]
        diff, fatal = 0.0, False
        for alt in options:
            if alt == chose:
                continue
            r = Run(world, table, persona)
            r.restore(base.snaps[floor], keep_logs=())
            r.overrides, r.record_decisions, r.density, r.deaths = {(floor,) + key: alt}, False, False, []
            if not r.play(floor, last) or r.deaths:
                fatal = True
            if r.floor_log and r.floor_log[-1][0] == last:
                _, hp, _, paid = r.floor_log[-1]
                diff = max(diff, abs(hp - b_hp) / b_mx, abs(paid - b_paid) / b_mx)
        out.append((floor, key[0], diff, fatal))
    return out


# ---------------------------------------------------------------- 한 판의 요약 (작업 프로세스 -> 집계)

def summarize(r, table, ref):
    P = r.P
    chap = lambda f: (f - 1) // G.FLOORS_PER_CHAPTER        # noqa: E731
    low, low_at = [1.0] * 5, [0] * 5
    for f, x in r.lows:
        if x < low[chap(f)]:
            low[chap(f)], low_at[chap(f)] = x, f
    keys = ("mobs", "lt1", "lt2", "lt5", "bp_n", "bp_hit", "near_n", "near_hit")
    fights = {k: [0] * 5 for k in keys}
    costs = [[] for _ in range(5)]
    for f, boss, cost, _swings, bp, near in r.fights:
        c = chap(f)
        if not boss:
            costs[c].append(cost)
            fights["mobs"][c] += 1
            fights["lt1"][c] += cost < 0.01
            fights["lt2"][c] += cost < 0.02
            fights["lt5"][c] += cost < 0.05
        if bp >= 0:
            fights["bp_n"][c] += 1
            fights["bp_hit"][c] += bp
        if near >= 0:
            fights["near_n"][c] += 1
            fights["near_hit"][c] += near
    fights["cost_med"] = [q(x, .5) for x in costs]
    pots = {k: [0.0] * 5 for k in ("served", "spill", "unused")}
    for f, served, spill, unused in r.potlog:
        pots["served"][chap(f)] += served
        pots["spill"][chap(f)] += spill
        pots["unused"][chap(f)] += unused
    return dict(
        table=table, seed=P.seed, ref=ref, label=P.label(),
        potion=P.potion, side=P.side, vault=P.vault, altar=P.altar, skill=P.skill, treasure=P.treasure,
        ok=r.stuck is None, stuck=r.stuck, deaths=list(r.deaths), rescues=r.rescues, retreats=r.retreats,
        grinds=r.grinds, level=r.level,
        paid=r.paid, par=r.par, altar_paid=r.altar_paid, altar_n=r.altar_n, foretold=r.foretold,
        score=r.score(), stars=list(r.stars), bands=list(r.bands), low=low, low_at=low_at, fights=fights, pots=pots,
        extras=list(r.extras))


_W = None


def _init_worker(mob_loss=None):
    global _W
    _W = World(mob_loss)


def _job(args):
    table, seed, ref, weigh, scope = args
    P = Persona(seed, ref, **REFERENCE[ref]) if ref else sample_persona(seed)
    r = Run(_W, table, P, record_decisions=weigh, density=weigh, skill_scope=scope)
    r.play()
    out = summarize(r, table, ref)
    out["scope"] = scope
    if weigh and r.stuck is None:
        out["weights"] = weigh_decisions(_W, table, P, r)
    return out


# ---------------------------------------------------------------- 집계와 찍기

V1_BOT = dict(paid=27654, foretold=28309, par=46693, stars="22223322333333333333")   # 재미검증 보고서 7절
# 시뮬의 봇이 같은 데이터(기준 합 46,693)에서 치른 값 25,449 — 실제/시뮬 = 1.087. 데이터를 다시 뽑으면 실제 봇과
# 견줄 판이 없으니 이 비를 그대로 쓴다(봇 녹화를 새로 뜨면 V1_BOT 와 함께 갈 것).
CAL = 27654 / 25449


def q(xs, p):
    """분위수 (선형 보간). 빈 목록이면 nan."""
    xs = sorted(xs)
    if not xs:
        return float("nan")
    k = (len(xs) - 1) * p
    lo = int(k)
    hi = min(lo + 1, len(xs) - 1)
    return xs[lo] + (xs[hi] - xs[lo]) * (k - lo)


def pct(n, d):
    return 100.0 * n / d if d else float("nan")


def band_ratios(runs):
    """띠 -> [치름/기준]. 막힌 판도 닫은 띠까지는 센다."""
    out = {b: [] for b in range(20)}
    for r in runs:
        for b, paid, par in r["bands"]:
            if par > 0:
                out[b].append(paid / par)
    return out


def thresholds(ratios, aim3=0.25, aim2=0.50):
    """띠마다 aim 분위수를 재고, 띠들의 가운데 값을 문턱으로 고른다 — 한 숫자로 20띠에 같이 건다."""
    x3 = statistics_median([q(v, aim3) for v in ratios.values() if v])
    x2 = statistics_median([q(v, aim2) for v in ratios.values() if v])
    return x3, x2


def statistics_median(xs):
    return q(xs, 0.5)


def star_share(ratios, x3, x2):
    """문턱 (x3, x2) 로 띠마다 ★★★·★★ 를 받는 몫. 정수 셈(퍼센트 정수)으로 반올림한 문턱을 쓴다."""
    a3, a2 = round(x3 * 100), round(x2 * 100)
    share3, share2 = [], []
    for v in ratios.values():
        if not v:
            continue
        share3.append(pct(sum(1 for x in v if x * 100 <= a3 + 1e-9), len(v)))
        share2.append(pct(sum(1 for x in v if a3 < x * 100 <= a2 + 1e-9), len(v)))
    return share3, share2


def _by_skill(runs):
    return (("스킬 안 씀", [r for r in runs if r["skill"] == SKILL_NONE]),
            ("스킬 씀", [r for r in runs if r["skill"] != SKILL_NONE]))


def report(results, seconds, mob_loss=None, stages=None):
    say = print
    whatif = [r for r in results if r.get("scope") == "battle"]
    late = [r for r in results if r["table"] == "whatif"]
    results = [r for r in results if r.get("scope") != "battle" and r["table"] != "whatif"]
    refs = [r for r in results if r["ref"]]
    pop = {t: [r for r in results if not r["ref"] and r["table"] == t] for t in TABLES}
    say("")
    say("=" * 100)
    say(f"재미 검증 v2 — 무작위 사람 {sum(len(v) for v in pop.values())}판 (일반 {len(pop['normal'])} · 탑의 법 "
        f"{len(pop['tower'])}) + 고정 {len(refs)}판, {seconds / 60:.1f}분")
    say("=" * 100)

    # ---- 0. 기준점: 봇
    say("\n[0] 기준점 — 봇을 흉내 낸 판 vs v1 의 실제 봇 완주")
    cal = CAL
    for r in refs:
        if r["table"] == "normal" and r["ref"] == "봇":
            say(f"    시뮬 봇  치름 {r['paid']:,} · 예언 {r['foretold']:,} · 기준 {r['par']:,} · 점수 {r['score']} · "
                f"별 {''.join(map(str, r['stars']))}")
            say(f"    실제 봇  치름 {V1_BOT['paid']:,} · 예언 {V1_BOT['foretold']:,} · 기준 {V1_BOT['par']:,} · 점수 "
                f"{round(1000 * V1_BOT['paid'] / V1_BOT['par'])} · 별 {V1_BOT['stars']}")
            if r["par"] == V1_BOT["par"]:
                say(f"    -> 시뮬이 실제보다 {100 * (1 - r['paid'] / V1_BOT['paid']):.0f}% 싸게 싸운다(예언 합도 같은 만큼).")
            else:
                say("    -> 기준 합이 다르다 — 데이터가 v1 뒤로 바뀌었다. 견줄 실제 판이 없다.")
            say(f"    문턱은 v1 데이터에서 잰 비 x{cal:.3f} 로 보정한다(CAL).")
    say("    고정 판:")
    for r in refs:
        status = "완주" if r["ok"] else f"{r['stuck']}층 막힘"
        say(f"      {r['table']:6} {r['ref']:8} {status:6} 죽음 {len(r['deaths']):2} · 점수 {r['score']:4} · "
            f"별 {''.join(map(str, r['stars']))} · 챕터 최저 HP {' '.join(f'{100 * x:.0f}' for x in r['low'])}")

    for t, runs in pop.items():
        if not runs:
            continue
        name = "일반" if t == "normal" else "탑의 법"
        say(f"\n{'#' * 30} {name} 표 — {len(runs)}판 {'#' * 30}")
        say("\n[1] 완주 (죽으면 층 입구에서 다시 — 물약을 보이는 대로 → 두고 온 물약 → 지나친 곁길을 잡으러 → 스킬)")
        for label, group in (("전체", runs),) + _by_skill(runs):
            n = len(group)
            if not n:
                continue
            clean = sum(1 for r in group if r["ok"] and not r["deaths"])
            done = sum(1 for r in group if r["ok"])
            rescued = sum(1 for r in group if r["rescues"])
            say(f"    {label:8} {n:3}판 · 안 죽고 완주 {pct(clean, n):3.0f}% · 다시 해서라도 완주 {pct(done, n):3.0f}% · "
                f"판당 죽음 {sum(len(r['deaths']) for r in group) / n:.2f} · 내려가 두고 온 물약을 마신 판 "
                f"{pct(sum(1 for r in group if r['retreats']), n):.0f}% · 지나친 곁길을 잡으러 간 판 "
                f"{pct(sum(1 for r in group if r['grinds']), n):.0f}% · 스킬을 꺼내서야 넘은 판 {pct(rescued, n):.0f}%"
                f" (그 층 {sum(r['rescues'] for r in group)}개)")
        firsts = [r["deaths"][0] for r in runs if r["deaths"]]
        if firsts:
            hist = [sum(1 for f in firsts if (f - 1) // 20 == c) for c in range(5)]
            common = sorted(set(firsts), key=lambda f: -firsts.count(f))[:6]
            say("    첫 죽음 층 — 챕터별 판 수 " + " · ".join(f"ch{c} {h}" for c, h in enumerate(hist))
                + " · 잦은 층 " + ", ".join(f"{f}층 {firsts.count(f)}" for f in common))
        stuck = sorted(r["stuck"] for r in runs if not r["ok"])
        if stuck:
            say(f"    막힌 층 {stuck[:20]}{'…' if len(stuck) > 20 else ''}")
        plain = [r for r in runs if r["skill"] == SKILL_NONE]
        say("    스킬 안 쓰는 사람의 습관별 완주율(판당 죽음):")
        for dim in ("side", "potion", "altar", "vault"):
            vals = sorted({r[dim] for r in plain})
            say(f"      {dim:7} " + " · ".join(
                f"{v} {pct(sum(1 for r in plain if r[dim] == v and r['ok']), sum(1 for r in plain if r[dim] == v)):.0f}%"
                f"({sum(len(r['deaths']) for r in plain if r[dim] == v) / max(1, sum(1 for r in plain if r[dim] == v)):.1f})"
                for v in vals))

        say("\n[2] 긴장 — 챕터별 가장 낮았던 HP(전투 직후, 최대 HP 대비) p10 / p50 / p90, 완주한 판")
        for label, group in _by_skill(runs):
            ok = [r for r in group if r["ok"]]
            if not ok:
                continue
            say(f"    {label}")
            for c in range(5):
                xs = [100 * r["low"][c] for r in ok]
                at = [r["low_at"][c] for r in ok if r["low"][c] < 0.5]
                where = sorted(set(at), key=lambda f: -at.count(f))[:3]
                say(f"      ch{c}  {q(xs, .1):4.0f} {q(xs, .5):4.0f} {q(xs, .9):4.0f}   50% 아래 {pct(sum(1 for x in xs if x < 50), len(xs)):3.0f}%"
                    f" · 25% 아래 {pct(sum(1 for x in xs if x < 25), len(xs)):3.0f}% · 10% 아래 {pct(sum(1 for x in xs if x < 10), len(xs)):3.0f}%"
                    + (" · 50% 아래로 떨어진 층 " + ", ".join(f"{f}층 {at.count(f)}" for f in where) if where else ""))

        col = "Tower" if t == "tower" else ""
        rows = [(stages or {}).get(5 * b + 4, {}) for b in range(20)]
        limits = [(int(r.get("StarGreat" + col, 0)) or 70, int(r.get("StarGood" + col, 0)) or 100) for r in rows]
        say(f"\n[3] 띠마다 치름/기준 (p25 / p50 / p75, 시뮬 그대로) · 데이터의 띠별 별 문턱으로 본 ★★★/★★/★ 몫"
            f" (치름에 봇 보정 x{cal:.3f})")
        say("    띠  층        스킬 안 씀            ★★★ ★★  ★     스킬 씀               ★★★ ★★  ★")
        groups = [band_ratios(g) for _, g in _by_skill(runs)]
        for b in range(20):
            cells = []
            g3, g2 = limits[b]
            for rr in groups:
                v = rr[b]
                if not v:
                    cells.append(" " * 38)
                    continue
                s3 = pct(sum(1 for x in v if x * cal * 100 <= g3 + 1e-9), len(v))
                s2 = pct(sum(1 for x in v if g3 + 1e-9 < x * cal * 100 <= g2 + 1e-9), len(v))
                cells.append(f"{q(v, .25):.2f} {q(v, .5):.2f} {q(v, .75):.2f}     {s3:3.0f} {s2:3.0f} {100 - s3 - s2:3.0f}")
            say(f"    {b:2}  {b * 5 + 1:3}~{b * 5 + 5:<4} {cells[0]}    {cells[1]}")
        say("    판 점수(완주한 판, 낮을수록 좋다) p10 / p25 / p50 / p75 / p90")
        for sk in (SKILL_NONE, SKILL_BOT, SKILL_TIMED, SKILL_MASH):
            xs = [r["score"] for r in runs if r["ok"] and r["skill"] == sk]
            if xs:
                say(f"      스킬 {sk:5} {q(xs, .1):5.0f} {q(xs, .25):5.0f} {q(xs, .5):5.0f} {q(xs, .75):5.0f} {q(xs, .9):5.0f}  ({len(xs)}판)")

        say("\n[4] 습관이 가르는 값 — 띠 치름/기준 가운데 값의 가운데(20띠), 스킬 안 쓰는 사람 안에서 (괄호는 판 수)")
        for dim in ("side", "altar", "vault", "treasure", "potion"):
            vals = sorted({r[dim] for r in plain})
            meds = {}
            for v in vals:
                rr = band_ratios([r for r in plain if r[dim] == v])
                meds[v] = statistics_median([q(x, .5) for x in rr.values() if x])
            spread = max(meds.values()) - min(meds.values())
            say(f"    {dim:8} " + " · ".join(f"{v} {m:.2f}({sum(1 for r in plain if r[dim] == v)})" for v, m in meds.items())
                + f"   폭 {spread:.2f}")
        meds = {}
        for sk in (SKILL_NONE, SKILL_BOT, SKILL_TIMED, SKILL_MASH):
            rr = band_ratios([r for r in runs if r["skill"] == sk])
            if any(rr.values()):
                meds[sk] = statistics_median([q(x, .5) for x in rr.values() if x])
        say("    skill    " + " · ".join(f"{k} {v:.2f}" for k, v in meds.items()) + "   (전체 판)")
        rr = band_ratios(plain)
        say("    스킬 안 씀, 띠별 p10~p90 (잘한 사람과 못한 사람의 사이): "
            + " ".join(f"{b}:{q(v, .1):.2f}~{q(v, .9):.2f}" for b, v in rr.items() if v))

        say("\n[5] 지루함 · 임계 · 물약 (챕터별, 완주한 판)")
        ok = [r for r in plain if r["ok"]]
        if ok:
            say("    스킬 안 쓰는 사람이 챕터 끝에 계단 룬 밖으로 더 얹은 것 (가운데 값: 공격 / 방어 / 체력):  " + " · ".join(
                f"{f}층 {q([e[1] for r in ok for e in r['extras'] if e[0] == f], .5):+.0f}/"
                f"{q([e[2] for r in ok for e in r['extras'] if e[0] == f], .5):+.0f}/"
                f"{q([e[3] for r in ok for e in r['extras'] if e[0] == f], .5):+.0f}" for f in (20, 40, 60, 80, 100)))
        for label, group in _by_skill(runs):
            ok = [r for r in group if r["ok"]]
            if not ok:
                continue
            say(f"    {label}")
            for c in range(5):
                mobs = sum(r["fights"]["mobs"][c] for r in ok)
                agg = {k: sum(r["fights"][k][c] for r in ok) for k in ("lt1", "lt2", "lt5", "bp_n", "bp_hit", "near_n", "near_hit")}
                pots = [sum(r["pots"][k][c] for r in ok) for k in ("served", "spill", "unused")]
                tot = sum(pots) or 1
                say(f"      ch{c}  몹 한 판 가운데 {100 * q([r['fights']['cost_med'][c] for r in ok], .5):4.1f}% · "
                    f"1%/2%/5% 미만 {pct(agg['lt1'], mobs):3.0f}/{pct(agg['lt2'], mobs):3.0f}/{pct(agg['lt5'], mobs):3.0f}% · "
                    f"얹은 공격이 한 대 덜게 {pct(agg['bp_hit'], agg['bp_n']):3.0f}%"
                    + (f" · +3 이면 한 대 덜 {pct(agg['near_hit'], agg['near_n']):3.0f}%" if agg["near_n"] else "")
                    + f" · 물약 들어감/넘침/버림 {100 * pots[0] / tot:3.0f}/{100 * pots[1] / tot:3.0f}/{100 * pots[2] / tot:3.0f}%")

        weighed = [r for r in runs if "weights" in r]
        if weighed:
            say(f"\n[6] 판단 밀도 — 판단 하나만 바꿔 {HORIZON}층 뒤의 HP·장부(치름+제단)를 견준다")
            for label, group in _by_skill(weighed):
                if not group:
                    continue
                ws = [w for r in group for w in r["weights"]]
                say(f"    {label} ({len(group)}판)")
                for kind in ("side", "pot", "vault", "pair", "altar"):
                    xs = [w for w in ws if w[1] == kind]
                    if xs:
                        say(f"      {kind:6} 판당 {len(xs) / len(group):5.1f}개 · 차이 p50 {100 * q([w[2] for w in xs], .5):4.1f}% "
                            f"p90 {100 * q([w[2] for w in xs], .9):4.1f}% · "
                            + " · ".join(f"{100 * s:.0f}% 넘음 {pct(sum(1 for w in xs if w[2] >= s or w[3]), len(xs)):3.0f}%"
                                         for s in WEIGHT_STEPS)
                            + f" · 죽음으로 갈림 {pct(sum(1 for w in xs if w[3]), len(xs)):3.0f}%")
                line = []
                for c in range(5):
                    floors = len(group) * sum(1 for f in range(c * 20 + 1, c * 20 + 21) if f > G.HANDMADE_FLOORS)
                    a, b = (sum(1 for w in ws if (w[0] - 1) // 20 == c and (w[2] >= s or w[3])) / floors for s in WEIGHT_STEPS)
                    line.append(f"ch{c} {a:.2f}/{b:.2f}")
                say("      층마다 값이 걸린 판단 (2%/5% 넘음): " + " · ".join(line))

    # ---- 7. 문턱
    say("\n[7] 별 문턱 — 무작위 사람 전부(스킬은 층마다 한 번). ★★★ = 띠마다 상위 4분의 1(p25), ★★ = 가운데(p50)")
    say(f"    시뮬의 치름에 봇 보정 x{cal:.3f} 를 곱해 실제 게임의 치름으로 읽는다. 기준(ParLoss)은 그대로 둔다.")
    for t, runs in pop.items():
        real = {b: [x * cal for x in v] for b, v in band_ratios(runs).items()}
        if not any(real.values()):
            continue
        name = "일반" if t == "normal" else "탑의 법"
        great = [round(100 * q(real[b], .25)) if real[b] else 0 for b in range(20)]
        good = [round(100 * q(real[b], .50)) if real[b] else 0 for b in range(20)]
        say(f"    {name} — 이 판들로 뽑은 띠별 문턱 (기준 대비 %, 띠 0~19, generate_content 의 STAR_* 에 붙인다)")
        say(f"      ★★★ {great}")
        say(f"      ★★  {good}")
        rows = {b: (stages or {}).get(5 * b + 4, {}) for b in range(20)}
        col = "Tower" if t == "tower" else ""
        have = [(int(rows[b].get("StarGreat" + col, 0)), int(rows[b].get("StarGood" + col, 0))) for b in range(20)]
        rules = [("뽑은 표", list(zip(great, good)))]
        if all(g > 0 and d > 0 for g, d in have):
            rules.append(("데이터의 표", have))
        rules.append(("예전 규칙 70/100", [(70, 100)] * 20))
        for label, table in rules:
            for skill_label, group in (("전체", runs),) + _by_skill(runs):
                rr = {b: [x * cal for x in v] for b, v in band_ratios(group).items()}
                s3 = [pct(sum(1 for x in rr[b] if x * 100 <= table[b][0] + 1e-9), len(rr[b])) for b in range(1, 20) if rr[b]]
                s2 = [pct(sum(1 for x in rr[b] if table[b][0] < x * 100 <= table[b][1] + 1e-9), len(rr[b]))
                      for b in range(1, 20) if rr[b]]
                if s3:
                    say(f"      {label:12} {skill_label:6} 띠 1~19 ★★★ {min(s3):3.0f}~{max(s3):3.0f}% (평균 {sum(s3) / len(s3):3.0f}) · "
                        f"★★ 평균 {sum(s2) / len(s2):3.0f}%")

    if whatif:
        say("\n[8] 예전 규칙과 견줌 — 스킬이 전투마다 다시 찼을 때 → 지금(층마다 한 번) (같은 사람·같은 씨앗, 스킬 쓰는 판만)")
        say("    지금 규칙에서 때를 맞추는 사람은 그 층에서 가장 비싼 싸움(보스층은 보스)에 아끼고, 누르는 사람은 첫 싸움에 다 쓴다.")
        for t in TABLES:
            for sk in (SKILL_MASH, SKILL_TIMED, SKILL_BOT):
                now = [r for r in whatif if r["table"] == t and r["skill"] == sk]      # 예전(전투마다)
                then = [r for r in pop[t] if r["skill"] == sk]                         # 지금(층마다)
                if not now or not then:
                    continue
                a = statistics_median([q(x, .5) for x in band_ratios(now).values() if x])
                b = statistics_median([q(x, .5) for x in band_ratios(then).values() if x])
                sa = q([r["score"] for r in now if r["ok"]], .5)
                sb = q([r["score"] for r in then if r["ok"]], .5)
                la = q([min(r["low"]) for r in now if r["ok"]], .5)
                lb = q([min(r["low"]) for r in then if r["ok"]], .5)
                say(f"    {'일반' if t == 'normal' else '탑의 법':5} {sk:5} 띠 치름/기준 가운데 {a:.2f} → {b:.2f} · 판 점수 가운데 {sa:.0f} → {sb:.0f}"
                    f" · 판 최저 HP 가운데 {100 * la:.0f}% → {100 * lb:.0f}% · 완주 {pct(sum(1 for r in then if r['ok']), len(then)):.0f}%")
        plain = [r for r in pop["normal"] if r["skill"] == SKILL_NONE]
        if plain:
            say(f"    (견줄 것: 일반 표 스킬 안 씀 — 띠 치름/기준 가운데 "
                f"{statistics_median([q(x, .5) for x in band_ratios(plain).values() if x]):.2f} · 판 점수 가운데 "
                f"{q([r['score'] for r in plain if r['ok']], .5):.0f})")

    if late:
        now_loss = G.MOB_HP_LOSS if isinstance(G.MOB_HP_LOSS, (list, tuple)) else (G.MOB_HP_LOSS,) * 5
        say(f"\n[9] 가정 — 몹 손실을 챕터별 {'/'.join(f'{x:g}' for x in mob_loss)} 로 푼 표(지도·물약 그대로, 지금은 "
            f"{'/'.join(f'{x:g}' for x in now_loss)}) — 일반 표의 같은 사람을 한 번 더")
        for skill_label, keep in (("스킬 안 씀", lambda r: r["skill"] == SKILL_NONE), ("스킬 씀", lambda r: r["skill"] != SKILL_NONE)):
            now = [r for r in pop["normal"] if keep(r)]
            then = [r for r in late if not r["ref"] and keep(r)]
            for label, group in (("지금", now), ("가정", then)):
                ok = [r for r in group if r["ok"]]
                if not ok:
                    continue
                cost = [statistics_median([r["fights"]["cost_med"][c] for r in ok]) for c in range(5)]
                say(f"    {skill_label:6} {label}  안 죽고 완주 {pct(sum(1 for r in group if r['ok'] and not r['deaths']), len(group)):3.0f}% · "
                    f"다시 해서라도 {pct(len(ok), len(group)):3.0f}% · 곁길 잡으러 {pct(sum(1 for r in group if r['grinds']), len(group)):3.0f}% · "
                    f"스킬로 {pct(sum(1 for r in group if r['rescues']), len(group)):3.0f}% · 챕터 최저 HP 가운데 "
                    + "/".join(f"{100 * q([r['low'][c] for r in ok], .5):.0f}" for c in range(5))
                    + " · 몹 한 판 가운데 " + "/".join(f"{100 * x:.1f}" for x in cost) + "%")
        for r in sorted(late, key=lambda r: r["ref"] or ""):
            if r["ref"] in ("정답 경로", "덤 챙김", "봇"):
                base = next((x for x in refs if x["ref"] == r["ref"] and x["table"] == "normal"), None)
                was = f"(지금 죽음 {len(base['deaths'])}·최저 {' '.join(f'{100 * x:.0f}' for x in base['low'])})" if base else ""
                say(f"    {r['ref']:6} {'완주' if r['ok'] else str(r['stuck']) + '층 막힘'} · 죽음 {len(r['deaths'])} · 챕터 최저 HP "
                    f"{' '.join(f'{100 * x:.0f}' for x in r['low'])}  {was}")


def main(argv=None):
    import argparse
    import multiprocessing
    import time

    ap = argparse.ArgumentParser(description="TheSword 재미 검증 v2 — 습관을 흔든 수백 판")
    ap.add_argument("--runs", type=int, default=500, help="무작위 사람 판 수 (일반 70%% · 탑의 법 30%%)")
    ap.add_argument("--weigh", type=int, default=32, help="판단 하나하나의 무게를 잴 일반 표 판 수 (스킬 안 씀/씀 반씩)")
    ap.add_argument("--tower-share", type=float, default=0.3)
    ap.add_argument("--quick", action="store_true", help="80판 · 무게 6판")
    ap.add_argument("--check", action="store_true", help="전투 엔진 대조만")
    ap.add_argument("--seed", type=int, default=20261103)
    ap.add_argument("--workers", type=int, default=os.cpu_count() or 4)
    ap.add_argument("--json", help="판마다의 요약을 이 파일에 쓴다")
    ap.add_argument("--mob-loss", help="가정: 챕터별 몹 손실 다섯 개(예 0.06,0.06,0.06,0.13,0.13)로 푼 표에서 "
                                       "스킬 안 쓰는 사람과 고정 판을 한 번 더 돌린다 [9]")
    args = ap.parse_args(argv)
    if args.quick:
        args.runs, args.weigh = 80, 6

    t0 = time.time()
    if args.check:
        W = World()
        n1, n2 = check_engine(W)
        print(f"전투 엔진 대조 통과 — simulate_battle 과 {n1}마리(설계 상태·기준 값까지), "
              f"BattleStepper float 시계와 이어 받기 무작위 {n2}판 ({time.time() - t0:.1f}초)")
        return 0

    mob_loss = tuple(float(x) for x in args.mob_loss.split(",")) if args.mob_loss else None
    assert mob_loss is None or len(mob_loss) == len(G.CHAPTER_THEMES), "--mob-loss 는 챕터 다섯 개"
    W = World()
    n1, n2 = check_engine(W, sample=60)          # 매번 짧게 — 엔진이 어긋나면 아무것도 믿을 수 없다
    print(f"층 {len(W.specs)}개 읽음(생성기 계획과 대조 통과) · 전투 엔진 대조 {n1}+{n2}판 통과")
    jobs = [(t, 1, ref, False, "floor") for t in TABLES for ref in REFERENCE]
    tower_every = round(1 / args.tower_share) if args.tower_share > 0 else 0
    weighed = {True: 0, False: 0}           # 스킬 안 쓰는 사람 / 쓰는 사람 반씩 무게를 잰다
    for i in range(args.runs):
        t = "tower" if tower_every and i % tower_every == tower_every - 1 else "normal"
        plain = sample_persona(args.seed + i).skill == SKILL_NONE
        w = t == "normal" and weighed[plain] < (args.weigh + plain) // 2
        weighed[plain] += w
        jobs.append((t, args.seed + i, None, w, "floor"))
        if not plain:
            jobs.append((t, args.seed + i, None, False, "battle"))    # 예전 규칙: 스킬이 전투마다 찼다면 [8]
        if mob_loss and t == "normal":
            jobs.append(("whatif", args.seed + i, None, False, "floor"))      # 가정: 몹 손실을 바꾸면 [9]
    if mob_loss:
        jobs += [("whatif", 1, ref, False, "floor") for ref in REFERENCE]
    # 무게를 재는 판이 가장 오래 걸린다 — 먼저 던진다.
    jobs.sort(key=lambda j: not j[3])
    results = []
    with multiprocessing.Pool(args.workers, initializer=_init_worker, initargs=(mob_loss,)) as pool:
        for k, res in enumerate(pool.imap_unordered(_job, jobs), 1):
            results.append(res)
            if k % 50 == 0 or k == len(jobs):
                print(f"  {k}/{len(jobs)} 판 ({time.time() - t0:.0f}초)", flush=True)
    results.sort(key=lambda r: (r["ref"] or "", r["table"], r["seed"]))
    report(results, time.time() - t0, mob_loss, W.stages)
    if args.json:
        import json
        with open(args.json, "w", encoding="utf-8") as f:
            json.dump(results, f, ensure_ascii=False)
    return 0


if __name__ == "__main__":
    sys.exit(main())
