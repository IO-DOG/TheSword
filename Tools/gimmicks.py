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


# ------------------------------------------------------------------ 왕좌(챕터 4) — 짝 균열
RIFT_CHAPTER = 4


def _room_cells(grid):
    out = set()
    for y in range(len(grid)):
        for x in range(len(grid[y])):
            for ox in (0, -1):
                for oy in (0, -1):
                    if all(_walk(_cell(grid, (x + ox + i, y + oy + j))) for i in (0, 1) for j in (0, 1)):
                        out.add((x, y))
    return out


def place_rift(grid, elite_code=None):
    """같은 구역 안의 관문 몬스터 하나를 건너뛰는 짝 균열. (a, b) 또는 None. 지도는 안 고친다(균열 칸은 빈 바닥 그대로).
    문은 건너뛰지 않는다(열쇠 셈) — 문을 벽으로 친 그래프에서 관문 T 하나만 사이에 둔 두 방 칸을 잇는다."""
    s = _spawn(grid)
    is_door = lambda v: v is not None and v.isdigit() and 3 <= int(v) <= 8

    def zone(start, blocked):
        seen = {start}; q = collections.deque([start])
        while q:
            c = q.popleft()
            for dx, dy in DIRS:
                n = (c[0] + dx, c[1] + dy)
                v = _cell(grid, n)
                if n in seen or n in blocked or not _walk(v) or is_door(v):
                    continue
                seen.add(n); q.append(n)
        return seen

    rooms = _room_cells(grid)
    reach = _flood(grid, s)
    best = None
    for t in sorted(reach):
        v = _cell(grid, t)
        if not v.startswith("M_") or v == elite_code:
            continue
        front_all = _flood(grid, s, blocked={t})
        nbs = [n for n in _open_n(grid, t) if not is_door(_cell(grid, n))]
        fr = [n for n in nbs if n in front_all]
        bk = [n for n in nbs if n not in front_all]
        if not fr or not bk:
            continue                                   # 관문이 아니다(돌아갈 수 있다)
        F = zone(fr[0], {t})
        R = zone(bk[0], {t})
        if F & R:
            continue

        def dist_from_t(region):
            d = {t: 0}; q = collections.deque([t])
            while q:
                c = q.popleft()
                for n in _open_n(grid, c):
                    if n in region and n not in d:
                        d[n] = d[c] + 1; q.append(n)
            return d
        df, dr = dist_from_t(F), dist_from_t(R)

        def ok_end(c, d):
            if _cell(grid, c) != "1" or not (2 <= d.get(c, 0) <= 12):
                return False
            if any(_cell(grid, (c[0] + dx, c[1] + dy)) in ("11", "14", "15", "16") for dx, dy in DIRS):
                return False
            return s in _flood(grid, s, blocked={c}) and _flood(grid, s, blocked={c}) | {c} == reach
        A = [c for c in sorted(F) if ok_end(c, df)]
        B = [c for c in sorted(R) if ok_end(c, dr)]
        if not A or not B:
            continue
        a = max(A, key=lambda c: df[c]); b = max(B, key=lambda c: dr[c])
        score = (len(R), df[a] + dr[b])
        if best is None or score > best[0]:
            best = (score, a, b)
    return None if best is None else (best[1], best[2])


# ------------------------------------------------------------------ 용광로(챕터 2) — 풀무 화로와 밸브
FORGE_CHAPTER = 2


def add_furnace(grid, doors, floor, floor_ok, elite_code=None):
    """몬스터가 둘 이상 선 방 하나에 화로를 놓고, 그 방 몬스터를 지나지 않고 닿는 곳에 밸브(레버, 셀 12)를 둔다.
    밸브 길목은 곁길 몬스터 하나가 지킨다(물살과 같은 길 — 관문·골방 파수꾼이 아닌 놈을 옮긴다).
    성공하면 grid 를 고치고 (화로 칸, 화로 방 몬스터 칸들, 밸브, 길목 몬스터 칸) 을 돌려준다. 완주 보장은 꺼진 화로로 잰다
    — 밸브가 화로 방보다 먼저 닿으니 다 잡는 정답 경로는 밸브를 먼저 잠근다."""
    from layout_gen import _unavoidable_mob
    s = _spawn(grid)
    reach = _flood(grid, s)
    rooms = _room_cells(grid)
    comps = []
    left = set(rooms)
    while left:
        st = left.pop(); comp = {st}; q = [st]
        while q:
            c = q.pop()
            for n in _open_n(grid, c):
                if n in left:
                    left.remove(n); comp.add(n); q.append(n)
        comps.append(comp)
    best = None
    for comp in comps:
        mobs = sorted(q for q in comp if _cell(grid, q).startswith("M_") and _cell(grid, q) != elite_code)
        plain = [q for q in comp if _cell(grid, q) == "1"]
        if len(mobs) < 2 or not plain:
            continue
        if best is None or len(mobs) > len(best[1]):
            best = (comp, mobs, plain)
    if best is None:
        return None
    comp, mobs, plain = best
    cx = sum(q[0] for q in comp) / len(comp); cy = sum(q[1] for q in comp) / len(comp)
    furnace = min(plain, key=lambda q: ((q[0] - cx) ** 2 + (q[1] - cy) ** 2, q))
    before = _flood(grid, s, blocked=set(mobs))                # 화로 방 몬스터를 안 지나고 닿는 곳

    def keeps(g, q):
        return _flood(g, s, blocked={q}) | {q} == _flood(g, s)
    items_all = {q for q in reach if _cell(grid, q).startswith(("I_", "E_"))}

    def guards_something(g, q):
        return bool(items_all - _flood(g, s, blocked={q}))
    walls = collections.Counter(v for row in grid for v in row if v.startswith("W"))
    rock = walls.most_common(1)[0][0] if walls else "W_01"
    cands = sorted((q for q in before if _cell(grid, q) == "1" and q not in comp and q != furnace
                    and min(abs(q[0] - m[0]) + abs(q[1] - m[1]) for m in mobs) >= 3 and keeps(grid, q)),
                   key=lambda q: -min(abs(q[0] - s[0]) + abs(q[1] - s[1]), 30))
    movers = [q for q in sorted(reach) if _cell(grid, q).startswith("M_") and q not in mobs and _cell(grid, q) != elite_code
              and not _unavoidable_mob(grid, doors, q) and not guards_something(grid, q)]
    for lever in cands[:25]:
        g = [list(r) for r in grid]
        g[lever[1]][lever[0]] = LEVER
        # 이미 몬스터가 지키는가(그 몬스터를 막으면 밸브에 못 간다)
        guard = next((m for m in sorted(reach) if _cell(g, m).startswith("M_") and m not in mobs
                      and lever not in _flood(g, s, blocked={m} | set(mobs))), None)
        if guard is None:
            path_cells = [c for c in sorted(before) if _cell(g, c) == "1" and c != lever and c != furnace and c not in comp
                          and lever not in _flood(g, s, blocked={c} | set(mobs))]
            done = None
            for m in movers:
                for c in path_cells:
                    g2 = [list(r) for r in g]
                    g2[c[1]][c[0]], g2[m[1]][m[0]] = g2[m[1]][m[0]], "1"
                    if len(_open_n(g2, m)) == 1:
                        g2[m[1]][m[0]] = rock
                    if lever in _flood(g2, s, blocked={c} | set(mobs)):
                        continue
                    if s not in _flood(g2, s) or floor_ok(g2):
                        continue
                    done = (g2, c)
                    break
                if done:
                    break
            if not done:
                continue
            g, guard = done
        elif floor_ok(g):
            continue
        for yy, row in enumerate(g):
            grid[yy][:] = row
        return furnace, mobs, lever, guard
    return None


# ------------------------------------------------------------------ 심층(챕터 3) — 얼음 미끄럼
ICE_CHAPTER = 3


def slide_moves(grid, ice, c, alive):
    """c 에서 네 방향으로 한 번 움직였을 때 서는 칸들. 얼음 칸에 들어서면 막힐 때까지 미끄러진다 —
    벽·몬스터(alive) 앞에서 서고, 물건 칸에 닿으면 주우며 그 칸에 선다, 얼음이 아닌 바닥(열린 문 칸 포함)에 닿으면 거기 선다."""
    out = []
    for dx, dy in DIRS:
        n = (c[0] + dx, c[1] + dy)
        v = _cell(grid, n)
        if not _walk(v) or n in alive:
            continue
        if n not in ice or v.startswith(("I_", "E_")):
            out.append(n)
            continue
        pos = n
        while True:
            m = (pos[0] + dx, pos[1] + dy)
            w = _cell(grid, m)
            # 문은 연 것으로 친다 — 열린 문 칸은 얼음이 아닌 바닥이라 거기 선다(77층: 닫힌 문 앞에서 서던 자리가 문을 열면 사라졌다)
            if not _walk(w) or m in alive:
                break
            if w.startswith(("I_", "E_")) or m not in ice:
                pos = m
                break
            pos = m
        out.append(pos)
    return out


def slide_reach(grid, ice, start, alive):
    seen = {start}; q = collections.deque([start])
    while q:
        c = q.popleft()
        for n in slide_moves(grid, ice, c, alive):
            if n not in seen:
                seen.add(n); q.append(n)
    return seen


def check_ice(grid, ice):
    """얼음이 있어도 다 잡고 다 줍고 계단에 닿는가, 갇히는 칸은 없는가(몬스터는 닿는 대로 잡아 가며 잰다). 빈 목록이면 정상."""
    s = _spawn(grid)
    mobs = {q for y, r in enumerate(grid) for x, v in enumerate(r) for q in [(x, y)] if v.startswith(("M_", "B_"))}
    alive = set(mobs)
    seen_all = set()
    while True:
        reach = slide_reach(grid, ice, s, alive)
        seen_all |= reach
        killable = {m for m in alive if any((m[0] + dx, m[1] + dy) in reach for dx, dy in DIRS)}
        if not killable:
            break
        alive -= killable
    issues = []
    if alive:
        issues.append(f"못 잡는 몬스터 {len(alive)}")
    need = {q for y, r in enumerate(grid) for x, v in enumerate(r) for q in [(x, y)]
            if v.startswith(("I_", "E_")) or v == "12" or (v.isdigit() and 3 <= int(v) <= 8)}
    lost = need - seen_all
    if lost:
        issues.append(f"못 닿는 물건·문·레버 {len(lost)}")
    if not any(_cell(grid, (q[0] + dx, q[1] + dy)) == "14" for q in seen_all for dx, dy in DIRS):
        issues.append("계단에 못 닿는다")
    final = slide_reach(grid, ice, s, set())
    # 어떤 순서로 잡아도 잃는 것이 없어야 한다 — 멈춤돌이 다 사라진 최악(전부 잡음)에서도 물건·문·레버에 닿고,
    # 몬스터마다 다른 놈이 다 없어도 옆에 설 수 있어야 한다(77층: 멈춤돌을 먼저 잡자 초록 열쇠가 영영 닿지 않았다)
    # 물건도 멈춤돌이다(주우면 사라진다) — 최악은 "다른 물건은 다 줍고 몬스터는 다 잡은" 상태
    items = [q for q in need if _cell(grid, q).startswith(("I_", "E_"))]
    bare = [list(r) for r in grid]
    for q in items:
        bare[q[1]][q[0]] = "1"
    for i in items:
        g1 = [list(r) for r in bare]
        g1[i[1]][i[0]] = grid[i[1]][i[0]]
        if i not in slide_reach(g1, ice, s, set()):
            issues.append(f"{i} 물건은 멈춤돌(다른 몬스터·물건)이 없으면 못 닿는다")
            break
    rest = need - set(items)
    if rest - slide_reach(bare, ice, s, set()):
        issues.append(f"멈춤돌이 다 사라지면 못 닿는 문·레버 {len(rest - slide_reach(bare, ice, s, set()))}")
    for m in mobs:
        alone = slide_reach(bare, ice, s, {m})
        if not any((m[0] + dx, m[1] + dy) in alone for dx, dy in DIRS):
            issues.append(f"{m} 는 다른 멈춤돌이 없으면 못 잡는다")
            break
    for c in final:
        if s not in slide_reach(grid, ice, c, set()):
            issues.append(f"{c} 에서 돌아오지 못한다(갇힌다)")
            break
    return issues


def place_ice(grid, elite_code=None):
    """몬스터·물건이 있는 방 하나를 얼음으로 깐다(빈 바닥 + 물건 칸). 지도는 안 고친다. 얼음 칸 목록 또는 None."""
    s = _spawn(grid)
    rooms = _room_cells(grid)
    comps, left = [], set(rooms)
    while left:
        st = left.pop(); comp = {st}; q = [st]
        while q:
            c = q.pop()
            for n in _open_n(grid, c):
                if n in left:
                    left.remove(n); comp.add(n); q.append(n)
        comps.append(comp)
    best = None
    for comp in comps:
        ice = {q for q in comp if _cell(grid, q) == "1" or _cell(grid, q).startswith(("I_", "E_"))}
        ice -= {q for q in ice if any(_cell(grid, (q[0] + dx, q[1] + dy)) in ("11", "14", "15", "16") for dx, dy in DIRS)}
        if len(ice) < 10:
            continue
        stop = sum(1 for q in comp if _cell(grid, q).startswith("M_"))
        items = sum(1 for q in ice if _cell(grid, q).startswith("I_"))
        if check_ice(grid, ice):
            continue
        score = (stop > 0, items, len(ice))
        if best is None or score > best[0]:
            best = (score, ice)
    return None if best is None else sorted(best[1])


def emit_cs(path, water, rifts=None, furnaces=None, ice=None):
    """water: {층: (물살 목록, 레버)}, rifts: {층: (a, b)}, furnaces: {층: (화로 칸, 배율, 몬스터 id 목록)} -> GeneratedGimmicks.cs"""
    lines = ["// 생성 파일 — Tools/generate_content.py 가 쓴다(Tools/gimmicks.py). 손으로 고치지 않는다.",
             "using System.Collections.Generic;", "",
             "public static class GeneratedGimmicks", "{",
             "    /// <summary>층(1부터) -> 물살 칸 (열, 행, 방향 0위 1오른쪽 2아래 3왼쪽). 그 층 레버를 당기면 멈춘다.</summary>",
             "    public static readonly Dictionary<int, (int x, int y, int dir)[]> Currents = new Dictionary<int, (int x, int y, int dir)[]>",
             "    {"]
    for f in sorted(water):
        cur, _ = water[f]
        lines.append("        { %d, new[] { %s } }," % (f, ", ".join("(%d, %d, %d)" % c for c in cur)))
    lines += ["    };", "",
              "    /// <summary>층(1부터) -> 짝 균열 두 칸 (열, 행). 한쪽에 들어서면 다른 쪽으로 옮겨진다(같은 구역의 관문 하나를 건너뛴다).</summary>",
              "    public static readonly Dictionary<int, (int ax, int ay, int bx, int by)> Rifts = new Dictionary<int, (int ax, int ay, int bx, int by)>",
              "    {"]
    for f in sorted(rifts or {}):
        (ax, ay), (bx, by) = rifts[f]
        lines.append("        { %d, (%d, %d, %d, %d) }," % (f, ax, ay, bx, by))
    lines += ["    };", "",
              "    /// <summary>층(1부터) -> 풀무 화로 (칸, 공격 배율, 화로 방 몬스터 id). 그 층 밸브(레버)를 잠그면 꺼진다.</summary>",
              "    public static readonly Dictionary<int, (int x, int y, float mul, int[] ids)> Furnaces = new Dictionary<int, (int x, int y, float mul, int[] ids)>",
              "    {"]
    for f in sorted(furnaces or {}):
        (fx, fy), mul, ids = furnaces[f]
        lines.append("        { %d, (%d, %d, %.4ff, new[] { %s }) }," % (f, fx, fy, mul, ", ".join(map(str, ids))))
    lines += ["    };", "",
              "    /// <summary>층(1부터) -> 얼음 칸 (열, 행). 들어서면 막힐 때까지 미끄러진다(물건 칸에서는 주우며 선다).</summary>",
              "    public static readonly Dictionary<int, (int x, int y)[]> Ice = new Dictionary<int, (int x, int y)[]>",
              "    {"]
    for f in sorted(ice or {}):
        lines.append("        { %d, new[] { %s } }," % (f, ", ".join("(%d, %d)" % c for c in ice[f])))
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
