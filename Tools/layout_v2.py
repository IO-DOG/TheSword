"""지도 v2 — 한 층의 배치(2026-11-23 오너 결정 "추천대로": 챕터마다 다른 건축 + 갈림길 B).

모양(layout_v2_shapes)을 지은 뒤 예전 생성기(11-09 의 layout_gen.build_floor_layout, 지금은 지웠다)와 같은 계획을 얹는다:
아래 계단, 관문 셋(길 순서대로 약한 놈부터), 열쇠 셋(구역마다), 곁길 둘(금고 층이 아니면 첫째는 골방 파수꾼),
골방(여분 열쇠·금고·물약), 층 유형별 물약, 룬(계단 구역의 길목 — 반드시 밟는다), 보스(계단 앞 길목), 둘 중 하나, 장비.
검사는 layout_gen 의 것을 그대로 쓴다(validate_layout·check_doors·check_structure·check_sealed_by_portal·check_vault_safe·
check_behind_boss·floor_choices).

**갈림길**(fork): 구역 1 과 구역 2 를 잇는 둘째 통로를 파서 노랑 문을 돌아가는 길을 낸다. 그 통로 가운데에 갈림길 정예가 선다.
"열쇠로 낼까, HP 로 낼까" — 노랑 문을 열면 HP 를 아끼고, 정예를 잡으면 열쇠를 남긴다(금고·다음 층).
short 층에는 그 층의 노랑 열쇠가 없다 — 들고 온 노랑 열쇠로 문을 열거나 정예와 싸운다.
원본 시험판: 스크래치 fork/place2.py·place3.py.
"""
import random
import layout_v2_shapes as S
import layout_gen as L

STYLE_OF_CHAPTER = list(S.PALETTE)          # 0 묘소 · 1 수로 · 2 용광로 · 3 심층 · 4 왕좌


def regions_of(g, s, ds, extra=()):
    """구역 — 문 k 이후를 막고 닿는 칸. extra(갈림길 정예 칸)는 구역 2 를 열기 전까지 막는다."""
    zones, prev = [], set()
    for k in range(4):
        block = set(ds[k:]) if k < 3 else set()
        if k < 2:
            block |= set(extra)
        r = set(S.bfs(g, s, block)[0]) - set(ds)
        zones.append(r - prev)
        prev = r
    return zones


def carve_fork(g, z, rng):
    """구역 1 과 구역 2 를 잇는 둘째 통로를 판다. (새 z — 관문 후보를 다시 잰 것, 정예 칸, 통로 칸들) 또는 None."""
    score, ds, tolls, s, u, sizes = z
    zones = regions_of(g, s, ds)
    z1, z2 = zones[1], zones[2]
    e1 = [c for c in z1 if any(n not in z1 and S.inside(*n) and g[n[1]][n[0]] == 0 for n in S.nbrs(*c))]
    e2 = [c for c in z2 if any(n not in z2 and S.inside(*n) and g[n[1]][n[0]] == 0 for n in S.nbrs(*c))]
    if not e1 or not e2:
        return None
    near_door = {n for d in ds for n in S.nbrs(*d)} | set(ds)
    for _ in range(80):
        a = rng.choice(e1)
        b = min(rng.sample(e2, min(12, len(e2))), key=lambda c: abs(c[0] - a[0]) + abs(c[1] - a[1]))
        if abs(a[0] - b[0]) + abs(a[1] - b[1]) < 4:
            continue
        path = S._carve_path(g, a, b, rng, rng.choice((1, 2)), z1 | z2)
        if not path or len(path) < 3 or any(c in near_door for c in path):
            continue
        if any(n in zones[0] or n in zones[3] or n in ds for c in path for n in S.nbrs(*c)):
            continue                                  # 다른 구역·문에 닿으면 문을 돌아가는 길이 또 생긴다
        for x, y in path:
            g[y][x] = 1
        elite = path[len(path) // 2]
        # 둘 다 따로 계단까지: 노랑 문을 막은 채 / 정예 칸을 막은 채
        if u not in S.bfs(g, s, {ds[1]})[0] or u not in S.bfs(g, s, {elite})[0]:
            for x, y in path:
                g[y][x] = 0
            continue
        fl = S.floors(g)
        ap = S.articulation(g, fl)
        dist, prev = S.bfs(g, s)
        pth, c = [], u
        while c is not None:
            pth.append(c)
            c = prev[c]
        new_tolls = [c for c in reversed(pth) if c in ap and c not in ds and c != elite]
        if len(new_tolls) < 4:
            for x, y in path:
                g[y][x] = 0
            continue
        return (score, ds, new_tolls, s, u, sizes), elite, set(path)
    return None


def carve_alcove(g, zone_cells, avoid, ds, rng):
    """layout_gen._carve_alcove 와 같은 규칙 — 방 칸(양옆이 열린 칸)에서 밖으로 두 칸. 입구는 방·안쪽에만, 안쪽은 입구에만 닿는다."""
    near = {n for d in ds for n in S.nbrs(*d)} | set(ds)
    cands = []
    for bx, by in zone_cells:
        if (bx, by) in near or (bx, by) in avoid:
            continue
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if not (S.inside(bx + dy, by + dx) and S.inside(bx - dy, by - dx)):
                continue
            if g[by + dx][bx + dy] != 1 or g[by - dx][bx - dy] != 1:
                continue                                  # 모서리·통로 칸
            gate, prize = (bx + dx, by + dy), (bx + 2 * dx, by + 2 * dy)
            if not all(1 <= c[0] < S.W - 1 and 1 <= c[1] < S.H - 1 for c in (gate, prize)):
                continue
            if g[gate[1]][gate[0]] or g[prize[1]][prize[0]]:
                continue

            def touches(c, allow):
                return any(S.inside(*n) and n not in allow and g[n[1]][n[0]] == 1 for n in S.nbrs(*c))
            if touches(gate, {(bx, by), prize}) or touches(prize, {gate}):
                continue
            cands.append(((bx, by), gate, prize))
    if not cands:
        return None
    base, gate, prize = rng.choice(cands)
    g[gate[1]][gate[0]] = 1
    g[prize[1]][prize[0]] = 1
    return base, gate, prize


def place(g, z, rng, mob_ids, boss_id, walls, potions, rune, alcove, choice, equip_id,
          elite=None, elite_id=None, short=False, corridor=()):
    """모양 g(0/1)·구역 z 위에 그 층의 계획을 얹는다. ((grid, zones, doors), "") 또는 (None, 까닭)."""
    score, ds, tolls, s, u, sizes = z
    boss = boss_id is not None
    vault = alcove[1] if alcove and alcove[0] == "vault" else None
    ex = (elite,) if elite else ()
    zones = regions_of(g, s, ds, ex)
    # 골방 — 금고는 마지막 구역에만(앞 구역이면 이 층 열쇠로 열려 갇힌다). 보스층은 계단 구역에 파지 않는다(파수꾼이 보스 뒤).
    pool = zones[3] if vault is not None else zones[0] | zones[1] | zones[2] | (set() if boss else zones[3])
    al = carve_alcove(g, sorted(pool), {s, u} | set(ex) | set(corridor), ds, rng)
    if al is None:
        return None, "골방 자리 없음"
    base, gate, prize = al
    fl = S.floors(g)
    ap = S.articulation(g, fl)
    zones = regions_of(g, s, ds, ex)
    grid = [[L.VOID] * S.W for _ in range(S.H)]
    for x, y in fl:
        grid[y][x] = L.FLOOR
    # 갈림길 통로는 비워 둔다 — 곁길 몹이 서면 정예 길에 싸움이 하나 더 붙는다(97층에 정예 둘이 줄 섰다, 11-23 리뷰)
    used = set(ds) | {s, u, base, gate, prize} | set(corridor)
    put = {s: L.SPAWN, u: L.STAIRS_UP}
    if elite is not None:
        put[elite] = f"M_{elite_id:03d}"
        used.add(elite)
    near_door = {n for d in ds for n in S.nbrs(*d)}

    def dead(c):
        return g[c[1]][c[0]] == 1 and sum(1 for n in S.nbrs(*c) if S.inside(*n) and g[n[1]][n[0]] == 1) == 1

    def free(zone, cut_ok=False, prefer_dead=False, among=None):
        # 막다른 칸의 아이템은 골방 덤만이다 — fun_sim 이 막다른 칸의 아이템을 골방 덤으로 읽는다.
        cells = [c for c in zone if c not in used and c not in near_door and (cut_ok or c not in ap)
                 and (among is None or c in among) and (prefer_dead or not dead(c))]
        rng.shuffle(cells)
        if prefer_dead:
            cells.sort(key=lambda c: not dead(c))
        return cells

    cells = sorted(free(zones[0]), key=lambda c: -abs(c[0] - s[0]) - abs(c[1] - s[1]))
    if not cells:
        return None, "아래 계단 자리 없음"
    put[cells[0]] = L.STAIRS_DOWN
    used.add(cells[0])
    dist = S.bfs(g, s)[0]
    tolls = [c for c in sorted(tolls, key=lambda c: dist.get(c, 0)) if c not in used]
    if len(tolls) < 3:
        return None, "관문 자리 모자람"
    before_boss = None
    if boss:
        bc = tolls[-1]
        put[bc] = f"B_{boss_id:03d}"
        used.add(bc)
        tolls = tolls[:-1]
        before_boss = set(S.bfs(g, s, {bc} | {u})[0])
    # 룬 — 계단 구역에서 길을 끊는 칸(반드시 밟는다 — 완주 계산이 센다). 보스층은 보스 앞.
    rc = [c for c in tolls if c in zones[3] and u not in S.bfs(g, s, {c})[0]]
    if not rc:
        return None, "룬 자리 없음"
    rune_cell = rc[-1]
    tolls = [c for c in tolls if c != rune_cell]
    put[rune_cell] = rune
    used.add(rune_cell)
    if len(tolls) < 3:
        return None, "관문 자리 모자람"
    picks = [tolls[0], tolls[len(tolls) // 2], tolls[-1]]
    if len(set(picks)) < 3:
        return None, "관문 자리 모자람"
    for i, c in enumerate(picks):                 # 관문 셋은 약한 놈부터 길 순서대로(mob_ids 는 약한 순)
        put[c] = f"M_{mob_ids[i]:03d}"
        used.add(c)
    for i in range(3):
        if i == 1 and short:
            continue                              # 갈림길 short 층 — 이 층엔 노랑 열쇠가 없다
        p = free(zones[i])
        if not p:
            return None, f"열쇠{i} 자리 없음"
        put[p[0]] = L.KEY_ITEM[i]
        used.add(p[0])
    # 곁길 둘 — 금고 층이 아니면 첫째는 골방 입구(파수꾼)
    side = 0
    if vault is None:
        put[gate] = f"M_{mob_ids[3]:03d}"
        side = 1
    for k in range(side, 2):
        p = free(zones[1 + k], prefer_dead=True)
        if not p:
            return None, "곁길 자리 없음"
        put[p[0]] = f"M_{mob_ids[3 + k]:03d}"
        used.add(p[0])
    put[prize] = alcove[2] if alcove else L.POTION_20
    # 물약 — 구역 1·2 는 층 유형 예산, 구역 3 은 [보스 앞 큰 물약,] 계단 앞 물약(emit_layouts 가 그 순서로 준다)
    for r, lst in enumerate(potions):
        for k, pot in enumerate(lst):
            among = None
            if r == 3 and before_boss is not None:
                among = before_boss if k == 0 else set(fl) - before_boss
            p = free(zones[r], among=among) or (free(zones[r], cut_ok=True, among=among) if among is not None else [])
            if not p:
                return None, "보스 앞/뒤 물약 자리 없음" if among is not None else "물약 자리 없음"
            put[p[0]] = pot
            used.add(p[0])
    # 둘 중 하나 — 길을 끊지 않는 두 칸(맨해튼 2). "stairs" 는 보스 뒤
    if choice:
        first, second, where = choice
        if where == "stairs" and before_boss is None:
            return None, "둘 중 하나: 보스 없음"
        among = (set(fl) - before_boss) if where == "stairs" else None
        p = [c for c in free(zones[3], among=among) if c not in ap]
        pairs = [(a, b) for a in p for b in p if a < b and abs(a[0] - b[0]) + abs(a[1] - b[1]) == 2]
        if not pairs:
            return None, "둘 중 하나 자리 없음"
        a, b = rng.choice(pairs)
        put[a], put[b] = first + "~", second + "~"
        used.update((a, b))
    if equip_id is not None:
        p = free(zones[3], among=(set(fl) - before_boss) if before_boss else None)
        if not p:
            return None, "장비 자리 없음"
        put[p[0]] = f"E_{equip_id:02d}"
        used.add(p[0])
    for (x, y), v in put.items():
        grid[y][x] = v
    # 남은 빈 막다른 칸은 메운다(되풀이 — 메우면 그 앞 칸이 새 막다른 칸이 될 수 있다)
    changed = True
    while changed:
        changed = False
        for y in range(S.H):
            for x in range(S.W):
                if grid[y][x] == L.FLOOR and (x, y) not in used and \
                        sum(1 for n in S.nbrs(x, y) if S.inside(*n) and grid[n[1]][n[0]] != L.VOID) == 1:
                    grid[y][x] = L.VOID
                    g[y][x] = 0
                    changed = True
    for i, (x, y) in enumerate(list(ds) + ([gate] if vault is not None else [])):
        horizontal = grid[y][x - 1] != L.VOID and grid[y][x + 1] != L.VOID
        grid[y][x] = (L.DOOR_V if horizontal else L.DOOR_H)[i if i < 3 else vault]
    L._fill_rock(grid, walls, rng)
    return (grid, zones, list(ds)), ""


def build_floor_v2(chapter, seed, mob_ids, boss_id, walls, potions, rune, alcove, choice, equip_id,
                   elite_id=None, short=False):
    """한 번 뽑기. ((grid, zones, doors, elite_cell), "") 또는 (None, 까닭). 씨앗을 바꿔 다시 부르는 것은 emit_layouts 몫이다."""
    rng = random.Random(seed)
    g, z, _, _ = S.make(STYLE_OF_CHAPTER[chapter], seed)
    if g is None:
        return None, "모양"
    elite = None
    if elite_id is not None:
        f = carve_fork(g, z, random.Random(seed * 7 + 1))
        if f is None:
            return None, "갈림길"
        z, elite, corridor = f
    else:
        corridor = set()
    r, why = place(g, z, rng, mob_ids, boss_id, walls, potions, rune, alcove, choice, equip_id,
                   elite, elite_id, short, corridor)
    if r is None:
        return None, why
    grid, zones, ds = r
    return (grid, zones, ds, elite), ""


def check_fork(grid, doors, elite):
    """갈림길 층: 노랑 문을 잠근 채(정예 길)도, 정예 칸을 막은 채(문 길 — 열쇠가 있으면)도 계단에 닿아야 한다."""
    if elite is None:
        return []
    sp, up = L._endpoints(grid)
    errs = []
    if up not in L._flood_locked(grid, sp, {doors[1]}):
        errs.append("정예 길로 계단에 못 간다")
    if up not in L._flood_locked(grid, sp, {elite}):
        errs.append("문 길로 계단에 못 간다")
    return errs


def dead_end_items(grid, elite_code=None):
    """막다른 칸의 아이템(둘 중 하나 제외) — 골방 안쪽 하나뿐이어야 한다(fun_sim 이 그것을 골방 덤으로 읽는다).
    갈림길 정예 칸은 막힌 것으로 센다(fun_sim 은 갈림길을 따로 다룬다)."""
    def opn(x, y):
        return 0 <= y < len(grid) and 0 <= x < len(grid[0]) and grid[y][x] not in (L.VOID, elite_code) \
            and not grid[y][x].startswith("W")
    return [(x, y) for y, row in enumerate(grid) for x, v in enumerate(row)
            if v.startswith("I_") and not v.endswith("~")
            and sum(opn(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))) == 1]
