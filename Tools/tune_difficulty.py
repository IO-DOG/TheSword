"""난이도 손잡이를 자동으로 맞춘다.

두 단계로 쓴다.

  1) --strict : 정답 경로 하나로만 겨우 완주하는 지점을 찾는다.
                (여유가 거의 없어서, 한 번만 헛디뎌도 죽는다)
  2) --forgiv : 그 지점에서 조금 풀어, 실수를 몇 번까지 버티는지 재고
                목표치(10회 미만)에 맞춘다.

난이도는 층당 몹 손실(MOB_HP_LOSS) 하나로 조인다. 다른 값은 그대로 두어
"무엇을 만졌는지" 가 분명하게 남게 한다.
"""
import sys
import importlib

import generate_content as G


def run_with(loss, ramp=None, potions=None):
    """주어진 난이도로 표를 다시 만들고 완주를 시도한다."""
    G.MOB_HP_LOSS = loss
    if ramp is not None:
        G.MOB_LOSS_RAMP = list(ramp)
    if potions is not None:
        G.FLOOR_POTIONS = list(potions)

    import os
    ptable = G.load_player_table(os.path.join(G.EXCEL, "PlayerData.csv"))
    ptable = G.extend_player_table(ptable, G.MAX_LEVEL_TABLE)
    start, err = G.simulate_handmade(ptable)
    if err:
        return None, f"손수 만든 구간 실패: {err}"

    # 판정은 route_check 하나로만 한다. 시뮬레이터가 둘이면 서로 다른 답을 내고,
    # 실제로 그것 때문에 "통과한다는 난이도" 가 5층에서 죽었다.
    import route_check
    monsters = G.build_monsters(ptable, start[0])
    ok, err = route_check.play(ptable, monsters, start)
    return (ok, monsters, err), None


def margin_of(log):
    """완주 기록에서 가장 아슬아슬했던 지점(최대 HP 대비 %)."""
    worst = 100.0
    for row in log:
        if isinstance(row, dict) and "hp_pct" in row:
            worst = min(worst, row["hp_pct"])
    return worst


def main():
    mode = sys.argv[1] if len(sys.argv) > 1 else "--strict"

    lo, hi = 0.020, 0.250          # 층당 몹 손실 탐색 범위
    best = None
    for _ in range(24):
        mid = (lo + hi) / 2
        res, err = run_with(mid)
        if err:
            print(err)
            return 1
        ok, monsters, fail = res
        if ok:
            best = (mid, monsters)
            lo = mid               # 더 어렵게 밀어 본다
        else:
            hi = mid
    if best is None:
        print("어떤 난이도에서도 완주하지 못했다")
        return 1

    loss, _mons = best
    print(f"정답 경로로 겨우 완주하는 지점: MOB_HP_LOSS = {loss:.4f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
