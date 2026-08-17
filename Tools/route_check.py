"""정답 경로가 하나뿐인지, 그리고 실수를 몇 번까지 봐주는지 잰다.

이 게임에서 "실수" 는 두 가지다.

  1) 물약을 너무 일찍 든다
     물약은 주우면 즉시 회복이고 최대치에서 잘린다(ConsumableItem.PickUp).
     가득한 채로 들르면 넘치는 만큼 그냥 버린다.
  2) 물약을 아예 안 든다
     막다른 길에 있으니 지나칠 수 있다.

몬스터를 잡는 순서는 미로가 강제한다. 미로는 고리가 없는 나무라 경로가
유일하고, 문 세 개가 층을 네 구역으로 자르며, 각 구역의 열쇠는 그 구역
안에서만 얻는다. 그래서 "앞 구역을 비우지 않고 뒤 몬스터를 잡는" 순서는
아예 존재할 수 없다 — 그건 검증이 아니라 구조가 보장한다.
여기서는 그 구조 위에서 물약 판단만 흔들어 본다.

  python route_check.py            정답 경로 + 실수 허용치
  python route_check.py --sweep    난이도별 허용치 표
"""
import os
import sys

import generate_content as G
from thesword_balance import Creature, simulate_battle


def _tables():
    ptable = G.load_player_table(os.path.join(G.EXCEL, "PlayerData.csv"))
    ptable = G.extend_player_table(ptable, G.MAX_LEVEL_TABLE)
    start, err = G.simulate_handmade(ptable)
    if err:
        raise SystemExit(f"손수 만든 구간 실패: {err}")
    monsters = G.build_monsters(ptable, start[0])
    return ptable, monsters, start


def play(ptable, monsters, start, blunder_floors=(), skip_floors=(), waste_floors=()):
    """완주를 시도한다.

    blunder_floors : 그 층에서는 물약을 보이는 대로 바로 든다 (넘쳐도 든다)
    skip_floors    : 그 층에서는 구역 물약을 아예 안 든다
    waste_floors   : 그 층에서 물약 하나를 통째로 헛되이 쓴 셈 친다.
                     "실수 한 번" 의 값을 그대로 재기 위한 것이다.
    """
    by_floor = {}
    for m in monsters:
        by_floor.setdefault(m["_floor"], []).append(m)

    level, exp, cur_hp = start
    blunder = set(blunder_floors)
    skip = set(skip_floors)
    waste = set(waste_floors)

    for floor in range(G.HANDMADE_FLOORS + 1, G.TOTAL_FLOORS + 1):
        mons = sorted((m for m in by_floor[floor] if not m["_boss"]),
                      key=lambda m: m["_order"])
        boss = next((m for m in by_floor[floor] if m["_boss"]), None)
        fights = list(mons) + ([boss] if boss else [])

        pots = [(G.FLOOR_POTIONS[0], 1), (G.FLOOR_POTIONS[1], 3)]
        if boss:
            pots.append((G.BOSS_FLOOR_POTIONS[0], len(fights) - 1))
        pots.append((G.EXIT_POTION, len(fights)))
        if floor in skip:
            pots = [t for t in pots if t[1] >= len(fights)]   # 계단 앞만 남긴다
        if floor in waste:
            # 실수 한 번 = 그 층의 물약 하나를 버린 것과 같다.
            small = min(pots, key=lambda t: t[0], default=None)
            if small is not None:
                pots.remove(small)

        for i, md in enumerate(fights):
            stats = G.stats_with_runes(ptable, level, floor)

            if floor in blunder:
                # 보이면 바로 든다. 넘치면 그만큼 버린다.
                for t in [t for t in pots if t[1] <= i]:
                    cur_hp = min(stats["hp"], cur_hp + stats["hp"] * t[0])
                    pots.remove(t)
            else:
                # 죽을 것 같을 때만, 가장 늦게 든다.
                while True:
                    probe = Creature(stats["hp"], stats["atk"], stats["dfn"],
                                     stats["aspd"], stats["dspd"], stats["crit"],
                                     stats["crit_atk"])
                    probe.hp = min(cur_hp, stats["hp"])
                    pm = Creature(md["MaxHP"], md["Attack"], md["Defence"],
                                  md["AttackSpeed"], md["DefenceSpeed"],
                                  md["Critical"], md["CriticalAttack"],
                                  md["Ability"])
                    if simulate_battle(probe, pm)[0]:
                        break
                    usable = [t for t in pots if t[1] <= i]
                    if not usable:
                        break
                    cur_hp = min(stats["hp"], cur_hp + stats["hp"] * usable[0][0])
                    pots.remove(usable[0])

            p = Creature(stats["hp"], stats["atk"], stats["dfn"],
                         stats["aspd"], stats["dspd"], stats["crit"],
                         stats["crit_atk"])
            p.hp = min(cur_hp, stats["hp"])
            m = Creature(md["MaxHP"], md["Attack"], md["Defence"],
                         md["AttackSpeed"], md["DefenceSpeed"],
                         md["Critical"], md["CriticalAttack"],
                         md["Ability"])

            won, _, _ = simulate_battle(p, m)
            cur_hp = p.hp
            if not won:
                return False, f"{floor}층 {i + 1}번째 전투에서 사망"

            exp += md["RewardExp"]
            while level + 1 in ptable and exp >= ptable[level + 1]["need_exp"]:
                exp -= ptable[level + 1]["need_exp"]
                level += 1
                cur_hp += ptable[level]["hp"]

        # 계단 앞 회복
        stats_end = G.stats_with_runes(ptable, level, floor + 1)
        for t in [t for t in pots if t[1] >= len(fights)]:
            cur_hp = min(stats_end["hp"], cur_hp + stats_end["hp"] * t[0])
            pots.remove(t)

    return True, ""


def tolerance(ptable, monsters, start, limit=20):
    """실수를 몇 번까지 버티는지.

    실수 한 번 = 그 층의 물약 하나를 버린 것. 층을 고르게 흩어 놓고
    하나씩 늘려 가며, 처음으로 완주하지 못하는 지점 직전을 답으로 한다.
    """
    floors = list(range(G.HANDMADE_FLOORS + 1, G.TOTAL_FLOORS + 1))
    for n in range(limit + 1):
        if n == 0:
            picks = []
        else:
            step = len(floors) / n
            picks = [floors[int(i * step)] for i in range(n)]
        ok, _ = play(ptable, monsters, start, waste_floors=picks)
        if not ok:
            return n - 1
    return limit


def main():
    ptable, monsters, start = _tables()

    print(f"난이도  MOB_HP_LOSS = {G.MOB_HP_LOSS:.4f}")
    ok, err = play(ptable, monsters, start)
    print(f"  정답 경로            : {'완주' if ok else '실패 — ' + err}")
    if not ok:
        return 1

    ok_skip, err_skip = play(ptable, monsters, start,
                             skip_floors=range(G.HANDMADE_FLOORS + 1, G.TOTAL_FLOORS + 1))
    print(f"  물약을 전부 지나치면 : {'완주(너무 헐렁하다)' if ok_skip else '실패 — ' + err_skip}")

    ok_all, err_all = play(ptable, monsters, start,
                           blunder_floors=range(G.HANDMADE_FLOORS + 1, G.TOTAL_FLOORS + 1))
    print(f"  매 층 성급히 마시면 : {'완주(너무 헐렁하다)' if ok_all else '실패 — ' + err_all}")

    n = tolerance(ptable, monsters, start)
    print(f"  버티는 실수 횟수      : {n}회  (물약 하나를 버리는 실수 기준)")
    return 0


if __name__ == "__main__":
    if "--sweep" in sys.argv:
        ptable0, _, start0 = _tables()
        print("MOB_HP_LOSS   정답경로   실수허용")
        for loss in [0.030, 0.035, 0.040, 0.042, 0.044, 0.046, 0.047]:
            G.MOB_HP_LOSS = loss
            mons = G.build_monsters(ptable0, start0[0])
            ok, _ = play(ptable0, mons, start0)
            n = tolerance(ptable0, mons, start0) if ok else -1
            print(f"   {loss:.3f}       {'O' if ok else 'X'}        {n}")
        raise SystemExit(0)
    raise SystemExit(main())
