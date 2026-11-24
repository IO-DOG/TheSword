"""챕터 기믹 (11-24 오너 "챕터마다 챕터별 기믹 — 제안대로"). 지도 v2 층 위에 얹는다.

수로(챕터 1) — 물살과 수문 레버
  곁가지로 들어가는 통로 한 칸이 물살이다. 화살표 방향으로만 들어가고 거꾸로는 못 나온다.
  그 곁가지 가장 안쪽에 수문 레버가 있고, 당기면 그 층 물살이 멈춘다(되돌릴 수 없다 — 레버는 한 번).
  판단: 앞쪽을 먼저 다 치울까(물약을 두고 가면 못 돌아온다), 물살을 타고 레버까지 갈 수 있나(머리 위 예측).
  완주 계산은 그대로다 — 순서만 바뀐다. 지도에는 레버(셀 12) 한 칸만 더하고, 물살 칸은 GeneratedGimmicks.cs 에 굽는다
  (CSV 셀 코드를 읽는 곳이 수십 곳이라 새 코드를 넣지 않는다).

좌표는 CSV 그대로(x = 열, y = 행). 방향 0 위(y-1) 1 오른쪽 2 아래 3 왼쪽 — C# Currents 와 같다.
"""
import collections

DIRS = ((0, -1), (1, 0), (0, 1), (-1, 0))
REASONS = collections.Counter()        # 0 위 1 오른쪽 2 아래 3 왼쪽
LEVER = "12"
WATER_CHAPTER = 1
KEYS = ("I_00", "I_01", "I_02")


def _cell(grid, p):
    x, y = p
    if 0 <= y < len(grid) and 0 <= x < len(grid[y]):
        return grid[y][x].strip().rstrip("~")
    return None


def _walk(v):
    return v is not None and v != "" and not v.startswith("W") and v not in ("0", "14", "15", "16")


def _spawn(grid):
    return next((x, y) for y, r in enumerate(grid) for x, v in enumerate(r) if v.strip() == "11")


def _flood(grid, start, blocked=frozenset(), edge_ok=None):
    seen = {start}
    q = collections.deque([start])
    while q:
        c = q.popleft()
        for d, (dx, dy) in enumerate(DIRS):
            n = (c[0] + dx, c[1] + dy)
            if n in seen or n in blocked or not _walk(_cell(grid, n)):
                continue
            if edge_ok and not edge_ok(c, n, d):
                continue
            seen.add(n)
            q.append(n)
    return seen


def _open_n(grid, p):
    return [(p[0] + dx, p[1] + dy) for dx, dy in DIRS if _walk(_cell(grid, (p[0] + dx, p[1] + dy)))]


def current_edge(currents):
    """물살 규칙: 물살 칸에는 그 방향으로만 들어가고, 물살 칸에서 거꾸로는 못 나온다."""
    flow = {(x, y): d for x, y, d in currents}

    def ok(a, b, d):
        if b in flow and flow[b] != d:
            return False
        if a in flow and d == (flow[a] + 2) % 4:
            return False
        return True
    return ok


def place_water(grid, floor):
    """곁가지 하나에 물살과 레버를 놓는다. grid 를 고쳐 레버를 넣고 (물살 목록, 레버 칸) 을 돌려준다. 자리가 없으면 None."""
    s = _spawn(grid)
    reach = _flood(grid, s)
    best = None
    for p in sorted(reach):
        if _cell(grid, p) != "1":
            continue
        nb = _open_n(grid, p)
        if len(nb) != 2 or (nb[0][0] != nb[1][0] and nb[0][1] != nb[1][1]):
            continue                                  # 곧은 통로 칸만
        cut = _flood(grid, s, blocked={p})
        behind = reach - cut - {p}
        if not behind or len(behind) > len(reach) * 0.45:
            continue
        vals = [_cell(grid, q) for q in behind]
        # 위층 계단 구역은 된다(되돌아올 수 없는 길 — 앞을 다 치우고 가라는 물음). 아래층 계단·스폰은 안 된다.
        if any(_cell(grid, (q[0] + dx, q[1] + dy)) in ("15", "16") for q in behind for dx, dy in DIRS) or "11" in vals:
            continue
        if any(v.isdigit() and 3 <= int(v) <= 8 for v in vals):
            continue                                  # 문은 건너지 않는다(열쇠 셈·금고)
        # 열쇠가 너머에 있어도 된다 — 레버로 언제든 돌아오니 순서만 바뀐다(check_water 가 잰다).
        mobs = [q for q in behind if _cell(grid, q).startswith("M_")]
        items = [q for q in behind if _cell(grid, q).startswith("I_")]
        if not mobs and not items:
            continue
        nxt = next(q for q in nb if q in behind)
        d = DIRS.index((nxt[0] - p[0], nxt[1] - p[1]))
        # 레버: 물살 너머의 빈 바닥 중 막아도 아무것도 안 끊기는 칸에서 물살에서 가장 먼 곳
        dist = {nxt: 0}
        q = collections.deque([nxt])
        while q:
            c = q.popleft()
            for n in _open_n(grid, c):
                if n in behind and n not in dist:
                    dist[n] = dist[c] + 1
                    q.append(n)
        def keeps(q):                                 # 레버(막힌 칸)를 놓아도 너머가 끊기지 않는다
            rest = behind - {q}
            if not rest:
                return False
            start = next(iter(rest))
            seen = {start}; st = [start]
            while st:
                c = st.pop()
                for n in _open_n(grid, c):
                    if n in rest and n not in seen:
                        seen.add(n); st.append(n)
            return seen == rest
        cand = [q for q in behind if _cell(grid, q) == "1" and q in dist and keeps(q)]
        if not cand:
            continue
        lever = max(cand, key=lambda q: dist[q])
        # 물살에서 레버까지 몬스터를 지나야 판단이 된다(못 이기면 갇힌다 — 예측이 X 면 타지 말 것)
        guarded = lever not in _flood(grid, nxt, blocked=set(mobs) | {p}) if nxt not in mobs else True
        score = (guarded, len(items) + len(mobs), -abs(len(behind) - 12))
        if best is None or score > best[0]:
            best = (score, p, d, lever)
    if best is None:
        return None
    _, p, d, lever = best
    lx, ly = lever
    grid[ly][lx] = LEVER
    currents = [(p[0], p[1], d)]
    issues = check_water(grid, currents, lever)
    if issues:
        grid[ly][lx] = "1"
        return None
    return currents, lever


def add_water(grid, doors, floor, floor_ok, elite_code=None):
    """수로 층에 물살·레버를 얹는다. 물살 너머 레버 길목에 곁길 몬스터 하나를 옮겨 세운다(레버를 몬스터가 지켜야 판단이 된다).
    floor_ok(g) 는 생성기의 층 검사 전부(걸린 까닭 문자열, 통과면 None) — 고친 격자가 하나라도 걸리면 되돌리고 None. 성공하면 grid 를 고치고 (물살, 레버)."""
    from layout_gen import _unavoidable_mob
    trial = [list(r) for r in grid]
    r = place_water(trial, floor)
    if r is None:
        return None
    cur, lever = r
    x, y, d = cur[0]
    nxt = (x + DIRS[d][0], y + DIRS[d][1])
    s = _spawn(trial)
    reach = _flood(trial, s)
    behind = reach - _flood(trial, s, blocked={(x, y)}) - {(x, y)}
    mobs_behind = [q for q in behind if _cell(trial, q).startswith("M_")]
    guarded = lambda g: lever not in _flood(g, nxt, blocked={q for q in behind if _cell(g, q).startswith("M_")} | {(x, y)})
    if not guarded(trial):
        # 길목: 막으면 물살 입구에서 레버가 끊기는 빈 칸
        necks = [c for c in sorted(behind) if _cell(trial, c) == "1" and c != nxt
                 and lever not in _flood(trial, nxt, blocked={c, (x, y)})]
        items_all = {q for q in reach if _cell(trial, q).startswith(("I_", "E_"))}

        def guards_something(q):                      # 막으면 물건에 못 닿는다 = 골방 파수꾼(여분 열쇠·금고 앞) — 옮기지 않는다
            return bool(items_all - _flood(trial, s, blocked={q}))
        movers = [q for q in sorted(reach - behind) if _cell(trial, q).startswith("M_") and _cell(trial, q) != elite_code
                  and not _unavoidable_mob(trial, doors, q) and not guards_something(q)]
        walls = collections.Counter(v for row in trial for v in row if v.startswith("W"))
        rock = walls.most_common(1)[0][0] if walls else "W_01"
        done = False
        for m in movers:
            for c in necks:
                g2 = [list(rr) for rr in trial]
                g2[c[1]][c[0]], g2[m[1]][m[0]] = g2[m[1]][m[0]], "1"
                if len(_open_n(g2, m)) == 1:              # 비운 자리가 빈 막다른 칸이면 바위로 메운다(구조 검사)
                    g2[m[1]][m[0]] = rock
                why = None if guarded(g2) else "not guarded"
                why = why or ("trap" if check_water(g2, cur, lever) else None) or floor_ok(g2)
                REASONS[why or "ok"] += 1
                if why is None:
                    trial, done = g2, True
                    break
            if done:
                break
        if not done:
            return None
    elif check_water(trial, cur, lever) or floor_ok(trial):
        return None
    for yy, row in enumerate(trial):
        grid[yy][:] = row
    return cur, lever


def check_water(grid, currents, lever):
    """물살이 있어도 갇히지 않는가. 빈 목록이면 정상.
      * 물살이 켜진 채로 스폰에서 모든 바닥에 닿는다(문·몬스터는 지나간다고 친다 — 순서 문제만 본다).
      * 물살 너머 어디에서든 레버에 닿는다(물살이 켜진 채로).
      * 레버를 당기면(물살이 멈추면) 어디서든 스폰으로 돌아온다."""
    issues = []
    s = _spawn(grid)
    edge = current_edge(currents)
    all_cells = _flood(grid, s)
    on = _flood(grid, s, edge_ok=edge)
    if all_cells - on:
        issues.append(f"물살이 켜진 채 닿지 않는 칸 {len(all_cells - on)}")
    lever_nb = set(_open_n(grid, lever))
    for c in on:
        back = _flood(grid, c, edge_ok=edge)
        if s not in back and not (back & lever_nb):
            issues.append(f"{c} 에서 스폰에도 레버에도 못 간다(갇힌다)")
            break
    return issues


def emit_cs(path, water):
    """water: {층: (물살 목록, 레버)} -> GeneratedGimmicks.cs"""
    lines = ["// 생성 파일 — Tools/generate_content.py 가 쓴다(Tools/gimmicks.py). 손으로 고치지 않는다.",
             "using System.Collections.Generic;", "",
             "public static class GeneratedGimmicks", "{",
             "    /// <summary>층(1부터) -> 물살 칸 (열, 행, 방향 0위 1오른쪽 2아래 3왼쪽). 그 층 레버를 당기면 멈춘다.</summary>",
             "    public static readonly Dictionary<int, (int x, int y, int dir)[]> Currents = new Dictionary<int, (int x, int y, int dir)[]>",
             "    {"]
    for f in sorted(water):
        cur, _ = water[f]
        lines.append("        { %d, new[] { %s } }," % (f, ", ".join("(%d, %d, %d)" % c for c in cur)))
    lines += ["    };", "}", ""]
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\n".join(lines))


if __name__ == "__main__":
    # 자기검사: 곧은 통로 너머 곁방 — 물살을 타면 레버까지 가야 돌아온다
    g = [list(r) for r in (
        "WWWWWWWWW",
        "W11111WWW",
        "W1M1I1WWW",
        "W11111WWW",
        "WWW1WWWWW",
        "WWW1WWWWW",
        "W1111M1IW",
        "W1W1WWW1W",
        "W1W1W1111",
        "WWWWWWWWW")]
    g = [[c if c not in "1MI" else {"1": "1", "M": "M_1000", "I": "I_03"}[c] for c in r] for r in g]
    g[1][1] = "11"
    g[8][8] = "W"
    g[1][5] = "14"
    r = place_water(g, 25)
    assert r is not None, "자리를 못 찾았다"
    cur, lever = r
    edge = current_edge(cur)
    x, y, d = cur[0]
    assert edge((x - DIRS[d][0], y - DIRS[d][1]), (x, y), d) and not edge((x, y), (x - DIRS[d][0], y - DIRS[d][1]), (d + 2) % 4)
    assert not check_water(g, cur, lever)
    print("gimmicks 자기검사 통과 — 물살", cur, "레버", lever)
