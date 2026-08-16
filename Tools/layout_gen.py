"""층 레이아웃(미로) 생성기.

CSV 격자 규약은 DataManager.ResetActiveDic 의 파서와 일치해야 한다:
  "0"/빈칸 = 없음(Void), "1" = 바닥, "W_xx" = 벽,
  "M_xxx" = 몬스터(MonsterData.id), "B_xxx" = 보스, "I_xx" = 소비아이템, "E_xx" = 장비,
  "3".."8" = 문, "11" = 스폰, "12" = 레버, "13" = 기둥, "14".."16" = 포탈
파서는 셀에서 숫자만 뽑아 id 로 쓴다: "W_03"->3, "M_004"->4.

구조
----
격자를 재귀 백트래킹(recursive backtracker)으로 판다. 홀수 좌표를 방으로 보고
사이 벽을 뚫는 고전적인 방식이라, 결과는 고리가 없는 나무(spanning tree)다.
나무이므로 두 지점을 잇는 길이 정확히 하나뿐이고 — 그게 이 층의 "정답 경로"다.

그 위에 문 3개를 얹는다. 문은 스폰에서 계단까지 가는 유일한 경로 위,
서로 다른 갈래가 갈라지는 지점에 놓아 층을 네 구역으로 자른다.

  스폰 ─(구역0)─[초록문]─(구역1)─[노랑문]─(구역2)─[빨강문]─(구역3: 계단/보스)

각 구역에는 다음 문을 여는 열쇠가 하나씩 들어간다. 열쇠는 그 구역의
막다른 길 끝에 둔다 — 미로를 실제로 헤매야 찾을 수 있게.

몬스터는 구역 번호 순으로 약한 놈부터 배치한다. 순서를 어기면 레벨이
따라오지 못해 죽는다(그 검증은 route_check.py 가 한다).
"""

import random

GRID_W, GRID_H = 27, 23

VOID, FLOOR = "0", "1"
SPAWN = "11"
# 포탈 셀: 14=위층 계단, 15=아래층 계단(윗층에서 내려온 도착지점), 16=보스방
# PortalController.SearchPortal 은 위로 갈 때 "다음 맵의 DownStairs" 를 찾는다.
# 즉 F+1 층에 15 가 없으면 엔딩 처리가 되므로 1층을 뺀 모든 층에 15 가 필요하다.
STAIRS_UP = "14"
STAIRS_DOWN = "15"
DOOR_BY_ORDER = {0: "3", 1: "4", 2: "5"}      # 문 셀 id (열쇠 0/1/2 에 대응)
KEY_ITEM = {0: "I_00", 1: "I_01", 2: "I_02"}  # 초록/노랑/빨강 열쇠

# 회복 아이템. 값은 ConsumableItemData 의 회복%와 짝이어야 한다.
POTION_20, POTION_30, POTION_50 = "I_03", "I_04", "I_06"

NEIGHBORS = ((1, 0), (-1, 0), (0, 1), (0, -1))

# 미로 칸은 홀수 좌표에만 놓는다. 27x23 격자 -> 13x11 칸.
CELL_COLS = (GRID_W - 1) // 2
CELL_ROWS = (GRID_H - 1) // 2


def _cell_to_grid(cx, cy):
    return 1 + cx * 2, 1 + cy * 2


def carve_maze(rng):
    """재귀 백트래킹으로 미로를 판다. 판 격자와 칸 사이 연결 관계를 돌려준다."""
    grid = [[VOID] * GRID_W for _ in range(GRID_H)]
    linked = {}          # (cx,cy) -> 이웃 칸 집합

    start = (rng.randrange(CELL_COLS), rng.randrange(CELL_ROWS))
    stack = [start]
    seen = {start}
    gx, gy = _cell_to_grid(*start)
    grid[gy][gx] = FLOOR

    while stack:
        cx, cy = stack[-1]
        nbrs = []
        for dx, dy in NEIGHBORS:
            nx, ny = cx + dx, cy + dy
            if 0 <= nx < CELL_COLS and 0 <= ny < CELL_ROWS and (nx, ny) not in seen:
                nbrs.append((nx, ny))
        if not nbrs:
            stack.pop()
            continue

        nxt = rng.choice(nbrs)
        # 두 칸 사이 벽을 뚫는다
        ax, ay = _cell_to_grid(cx, cy)
        bx, by = _cell_to_grid(*nxt)
        grid[(ay + by) // 2][(ax + bx) // 2] = FLOOR
        grid[by][bx] = FLOOR

        linked.setdefault((cx, cy), set()).add(nxt)
        linked.setdefault(nxt, set()).add((cx, cy))
        seen.add(nxt)
        stack.append(nxt)

    return grid, linked


def _tree_path(linked, src, dst):
    """나무에서 두 칸을 잇는 유일한 경로."""
    prev = {src: None}
    stack = [src]
    while stack:
        cur = stack.pop()
        if cur == dst:
            break
        for nb in linked.get(cur, ()):
            if nb not in prev:
                prev[nb] = cur
                stack.append(nb)
    if dst not in prev:
        return None
    path, cur = [], dst
    while cur is not None:
        path.append(cur)
        cur = prev[cur]
    path.reverse()
    return path


def _regions(linked, path, gates):
    """문(gates)으로 잘린 구역들. 구역 i = 문 i 를 열기 전에 갈 수 있는 칸들."""
    cut = set()
    for a, b in gates:
        cut.add((a, b))
        cut.add((b, a))

    regions = []
    blocked = set(cut)
    seen_all = set()
    for i in range(len(gates) + 1):
        src = path[0] if i == 0 else gates[i - 1][1]
        comp, stack = {src}, [src]
        while stack:
            cur = stack.pop()
            for nb in linked.get(cur, ()):
                if (cur, nb) in blocked or nb in comp:
                    continue
                comp.add(nb)
                stack.append(nb)
        regions.append(comp - seen_all)
        seen_all |= comp
    return regions


def _dead_ends(linked, cells):
    """구역 안에서 갈래가 하나뿐인 칸 = 막다른 길."""
    return [c for c in cells if len(linked.get(c, ())) == 1]


def _add_walls(grid, wall_tiles, rng):
    """바닥과 맞닿은 빈 칸을 벽으로 채운다."""
    for y in range(GRID_H):
        for x in range(GRID_W):
            if grid[y][x] != VOID:
                continue
            touching = False
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < GRID_W and 0 <= ny < GRID_H and grid[ny][nx] != VOID:
                        touching = True
                        break
                if touching:
                    break
            if touching:
                grid[y][x] = rng.choice(wall_tiles)


def build_floor_layout(mob_ids, boss_id, wall_tiles, seed, mobs_in_floor=5,
                       with_down_stairs=True, equip_id=None, potions=None):
    """한 층의 격자를 만든다.

    mob_ids : 이 층의 몬스터 id 들. 약한 놈부터 정렬돼 있어야 한다.
              구역 순서대로 배치되므로 이 순서가 곧 "잡아야 하는 순서"다.
    potions : 구역별 회복 아이템 목록. 예) [None, POTION_20, POTION_30, POTION_50]

    (grid, 구역별 몬스터 좌표, 문 좌표들) 반환.
    """
    rng = random.Random(seed)

    if isinstance(mob_ids, int):          # 예전 호출 형태도 받아 준다
        mob_ids = [mob_ids] * mobs_in_floor
    mob_ids = list(mob_ids)[:mobs_in_floor]
    while len(mob_ids) < mobs_in_floor:
        mob_ids.append(mob_ids[-1])

    grid, linked = carve_maze(rng)
    cells = list(linked.keys())

    # 스폰과 계단은 미로에서 가장 멀리 떨어진 두 칸으로 잡는다.
    # 나무에서 지름(diameter)을 구하는 두 번 BFS 방식.
    def farthest(src):
        dist = {src: 0}
        stack = [src]
        while stack:
            cur = stack.pop()
            for nb in linked.get(cur, ()):
                if nb not in dist:
                    dist[nb] = dist[cur] + 1
                    stack.append(nb)
        far = max(dist, key=lambda c: dist[c])
        return far, dist

    a, _ = farthest(cells[0])
    b, _ = farthest(a)
    spawn_cell, stairs_cell = a, b

    path = _tree_path(linked, spawn_cell, stairs_cell)
    if path is None or len(path) < 8:
        return None, None, None

    # 문은 정답 경로를 네 토막으로 자르는 자리에 놓는다.
    steps = [len(path) * (i + 1) // 4 for i in range(3)]
    steps = sorted(set(max(1, min(len(path) - 2, s)) for s in steps))
    if len(steps) < 3:
        return None, None, None
    gates = [(path[s - 1], path[s]) for s in steps]

    regions = _regions(linked, path, gates)
    if any(len(r) == 0 for r in regions):
        return None, None, None

    place = {}

    def put(cell, value):
        gx, gy = _cell_to_grid(*cell)
        place[(gx, gy)] = value

    # 문: 두 칸 사이 벽 자리에 놓는다
    doors = []
    for i, (u, v) in enumerate(gates):
        ux, uy = _cell_to_grid(*u)
        vx, vy = _cell_to_grid(*v)
        dx, dy = (ux + vx) // 2, (uy + vy) // 2
        grid[dy][dx] = DOOR_BY_ORDER[i]
        doors.append((dx, dy))

    put(spawn_cell, SPAWN)
    used = {spawn_cell, stairs_cell}

    if with_down_stairs:
        # 아래 계단은 스폰 구역 안, 스폰 옆 칸에 둔다.
        near = [c for c in regions[0] if c not in used]
        near.sort(key=lambda c: abs(c[0] - spawn_cell[0]) + abs(c[1] - spawn_cell[1]))
        if not near:
            return None, None, None
        put(near[0], STAIRS_DOWN)
        used.add(near[0])

    put(stairs_cell, STAIRS_UP)

    # 열쇠는 그 구역의 막다른 길 끝에. 헤매야 찾도록.
    for i in range(3):
        pool = [c for c in _dead_ends(linked, regions[i]) if c not in used]
        if not pool:
            pool = [c for c in regions[i] if c not in used]
        if not pool:
            return None, None, None
        # 정답 경로에서 먼 막다른 길일수록 좋다
        on_path = set(path)
        pool.sort(key=lambda c: (c in on_path, rng.random()))
        put(pool[0], KEY_ITEM[i])
        used.add(pool[0])

    # 몬스터: 구역 순서대로, 약한 놈부터. 정답 경로 위에 놓아 반드시 마주치게 한다.
    per_region = [1, 2, mobs_in_floor - 3]
    mob_cells = []
    idx = 0
    for r in range(3):
        on_path = [c for c in path if c in regions[r] and c not in used]
        spare = [c for c in regions[r] if c not in used and c not in on_path]
        rng.shuffle(spare)
        slots = on_path + spare
        for _ in range(max(0, per_region[r])):
            if not slots:
                return None, None, None
            cell = slots.pop(0)
            put(cell, f"M_{mob_ids[idx]:03d}")
            used.add(cell)
            mob_cells.append(cell)
            idx += 1

    # 회복 아이템: 구역별로 지정된 것만. 없으면 그 구역엔 회복이 없다.
    # 구역마다 여러 개 놓을 수 있다. 막다른 길 쪽을 먼저 쓴다 —
    # 들르려면 돌아가야 하니, "지금 들를까" 가 판단거리가 된다.
    potions = potions or [[], [POTION_20], [POTION_30], [POTION_30]]
    on_path_set = set(path)
    for r, pots in enumerate(potions[:4]):
        for pot in (pots or []):
            pool = [c for c in regions[r] if c not in used]
            if not pool:
                continue
            pool.sort(key=lambda c: (c in on_path_set, rng.random()))
            put(pool[0], pot)
            used.add(pool[0])

    if boss_id is not None:
        pool = [c for c in regions[3] if c not in used]
        pool.sort(key=lambda c: abs(c[0] - stairs_cell[0]) + abs(c[1] - stairs_cell[1]))
        if not pool:
            return None, None, None
        put(pool[0], f"B_{boss_id:03d}")
        used.add(pool[0])

    if equip_id is not None:
        pool = [c for c in regions[3] if c not in used]
        if pool:
            put(pool[0], f"E_{equip_id:02d}")
            used.add(pool[0])

    for (x, y), v in place.items():
        grid[y][x] = v

    _add_walls(grid, wall_tiles, rng)
    return grid, regions, doors


def validate_layout(grid, regions, doors):
    """문을 순서대로 열며 스폰 -> 각 구역 -> 계단 도달이 가능한지 검사."""
    start = stairs = None
    for y in range(GRID_H):
        for x in range(GRID_W):
            if grid[y][x] == SPAWN:
                start = (x, y)
            elif grid[y][x] == STAIRS_UP:
                stairs = (x, y)
    if start is None:
        return False, "스폰 없음"
    if stairs is None:
        return False, "계단 없음"

    door_set = set(doors)

    def flood(open_doors):
        seen, stack = {start}, [start]
        while stack:
            x, y = stack.pop()
            for dx, dy in NEIGHBORS:
                nx, ny = x + dx, y + dy
                if not (0 <= nx < GRID_W and 0 <= ny < GRID_H) or (nx, ny) in seen:
                    continue
                cell = grid[ny][nx]
                if cell == VOID or cell.startswith("W"):
                    continue
                if (nx, ny) in door_set and (nx, ny) not in open_doors:
                    seen.add((nx, ny))   # 문 앞까지는 갈 수 있다
                    continue
                seen.add((nx, ny))
                stack.append((nx, ny))
        return seen

    # 열쇠를 순서대로 주우며 문을 연다. 각 열쇠는 그 시점에 닿아야 한다.
    opened = set()
    for i in range(3):
        reach = flood(opened)
        key = KEY_ITEM[i]
        spot = None
        for y in range(GRID_H):
            for x in range(GRID_W):
                if grid[y][x] == key:
                    spot = (x, y)
        if spot is None:
            return False, f"열쇠{i} 없음"
        if spot not in reach:
            return False, f"열쇠{i} 도달 불가"
        if doors[i] not in reach:
            return False, f"문{i} 도달 불가"
        opened.add(doors[i])

    if stairs not in flood(opened):
        return False, "계단 도달 불가"
    return True, ""
