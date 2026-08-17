"""층 레이아웃 생성기 — 매직 타워(魔塔) 방식.

CSV 격자 규약은 DataManager.ResetActiveDic 의 파서와 일치해야 한다:
  "0"/빈칸 = 없음(Void), "1" = 바닥, "W_xx" = 벽,
  "M_xxx" = 몬스터(MonsterData.id), "B_xxx" = 보스, "I_xx" = 소비아이템, "E_xx" = 장비,
  "3".."8" = 문, "11" = 스폰, "12" = 레버, "13" = 기둥, "14".."16" = 포탈
파서는 셀에서 숫자만 뽑아 id 로 쓴다: "W_03"->3, "M_004"->4.

왜 미로가 아니라 방인가
----------------------
매직 타워는 길 찾기 게임이 아니라 자원 배분 게임이다. 길은 한눈에 보이고,
문제는 "무엇을 어떤 순서로 살 것인가" 다. 몬스터는 지나가려면 HP 를 내야 하는
관문이고, 열쇠와 물약은 한정된 예산이다. 그래서 구조를 이렇게 잡는다.

  * 방을 격자로 늘어놓고 짧은 통로로 잇는다 — 길은 보인다
  * 몬스터는 통로 목에 세운다 — 지나가려면 반드시 값을 치른다
  * 열쇠/물약/장비는 방 안에 둔다 — 들를지 말지가 판단거리다
  * 문 세 개가 층을 네 구역으로 잘라 큰 순서를 정한다
  * 곁길을 한둘 남긴다 — 지금 이 몬스터를 잡을지 돌아갈지 고르게

몬스터는 벽이 아니라 통행료다. 그래서 도달 가능성 검사도 몬스터를 지나갈 수
있는 것으로 보고 한다(validate_layout).
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

# 방 격자 3x3, 방 하나는 7x5.
ROOMS_X, ROOMS_Y = 3, 3
ROOM_W, ROOM_H = 7, 5
GAP_X = (GRID_W - 2 - ROOMS_X * ROOM_W) // (ROOMS_X - 1)
GAP_Y = (GRID_H - 2 - ROOMS_Y * ROOM_H) // (ROOMS_Y - 1)


def room_origin(rx, ry):
    return 1 + rx * (ROOM_W + GAP_X), 1 + ry * (ROOM_H + GAP_Y)


def room_cells(rx, ry):
    ox, oy = room_origin(rx, ry)
    return [(x, y) for y in range(oy, oy + ROOM_H) for x in range(ox, ox + ROOM_W)]


def room_center(rx, ry):
    ox, oy = room_origin(rx, ry)
    return ox + ROOM_W // 2, oy + ROOM_H // 2


def _carve_rooms(grid):
    for ry in range(ROOMS_Y):
        for rx in range(ROOMS_X):
            for (x, y) in room_cells(rx, ry):
                grid[y][x] = FLOOR


def _corridor(grid, a, b):
    """두 방 중심을 잇는 곧은 통로. 판 칸 목록을 돌려준다."""
    (ax, ay), (bx, by) = a, b
    cells = []
    if ay == by:
        for x in range(min(ax, bx), max(ax, bx) + 1):
            cells.append((x, ay))
    else:
        for y in range(min(ay, by), max(ay, by) + 1):
            cells.append((ax, y))
    for (x, y) in cells:
        grid[y][x] = FLOOR
    return cells


def _outside_room(cell):
    for ry in range(ROOMS_Y):
        for rx in range(ROOMS_X):
            ox, oy = room_origin(rx, ry)
            if ox <= cell[0] < ox + ROOM_W and oy <= cell[1] < oy + ROOM_H:
                return False
    return True


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


def _room_order(rng):
    """방 아홉 개를 인접하게 훑는 한붓그리기 순서."""
    all_rooms = [(x, y) for y in range(ROOMS_Y) for x in range(ROOMS_X)]
    for _ in range(400):
        order = [rng.choice(all_rooms)]
        seen = set(order)
        while len(order) < len(all_rooms):
            cx, cy = order[-1]
            nbrs = [(cx + dx, cy + dy) for dx, dy in NEIGHBORS
                    if 0 <= cx + dx < ROOMS_X and 0 <= cy + dy < ROOMS_Y
                    and (cx + dx, cy + dy) not in seen]
            if not nbrs:
                break
            nxt = rng.choice(nbrs)
            order.append(nxt)
            seen.add(nxt)
        if len(order) == len(all_rooms):
            return order
    return all_rooms


def build_floor_layout(mob_ids, boss_id, wall_tiles, seed, mobs_in_floor=5,
                       with_down_stairs=True, equip_id=None, potions=None):
    """한 층의 격자를 만든다.

    mob_ids : 이 층의 몬스터 id 들. 약한 놈부터 정렬돼 있어야 한다.
    potions : 구역별 회복 아이템 목록. 예) [[], ["I_03"], ["I_04"], ["I_06"]]

    (grid, 구역별 방 목록, 문 좌표들) 반환.
    """
    rng = random.Random(seed)

    if isinstance(mob_ids, int):
        mob_ids = [mob_ids] * mobs_in_floor
    mob_ids = list(mob_ids)[:mobs_in_floor]
    while len(mob_ids) < mobs_in_floor:
        mob_ids.append(mob_ids[-1])

    grid = [[VOID] * GRID_W for _ in range(GRID_H)]
    _carve_rooms(grid)

    order = _room_order(rng)
    centers = [room_center(*r) for r in order]

    # 큰 줄기. 통로마다 방 밖 구간의 한가운데가 관문 자리다.
    gates = []
    for i in range(len(order) - 1):
        cells = _corridor(grid, centers[i], centers[i + 1])
        outside = [c for c in cells if _outside_room(c)]
        gates.append(outside[len(outside) // 2] if outside else None)

    # 곁길. 길이 한 줄이면 고를 것이 없다.
    extra = 0
    for _ in range(20):
        if extra >= 2:
            break
        a, b = rng.sample(range(len(order)), 2)
        if abs(a - b) < 3:
            continue
        (ax, ay), (bx, by) = order[a], order[b]
        if abs(ax - bx) + abs(ay - by) != 1:
            continue
        _corridor(grid, centers[a], centers[b])
        extra += 1

    # 구역: 방 아홉 개를 3/2/2/2 로 끊고 경계에 문을 세운다.
    doors = []
    for c in (3, 5, 7):
        g = gates[c - 1]
        if g is None:
            return None, None, None
        doors.append(g)
    if len(set(doors)) != 3:
        return None, None, None

    regions = [order[0:3], order[3:5], order[5:7], order[7:9]]

    place = {}
    used = set()

    def free_in(rooms):
        pool = []
        for r in rooms:
            pool += [c for c in room_cells(*r)
                     if c not in used and grid[c[1]][c[0]] == FLOOR]
        rng.shuffle(pool)
        return pool

    spawn = room_center(*order[0])
    place[spawn] = SPAWN
    used.add(spawn)

    if with_down_stairs:
        pool = free_in([order[0]])
        if not pool:
            return None, None, None
        place[pool[0]] = STAIRS_DOWN
        used.add(pool[0])

    up = room_center(*order[-1])
    if up in used:
        return None, None, None
    place[up] = STAIRS_UP
    used.add(up)

    # 열쇠는 그 구역의 방 안에. 문을 열려면 그 구역을 훑어야 한다.
    for i in range(3):
        pool = free_in(regions[i])
        if not pool:
            return None, None, None
        place[pool[0]] = KEY_ITEM[i]
        used.add(pool[0])

    # 몬스터는 통로 목에. 지나가려면 값을 치른다. 약한 놈부터.
    per_region = [1, 2, mobs_in_floor - 3]
    gates_by_region = [gates[0:2], gates[3:5], gates[5:7]]
    idx = 0
    for r in range(3):
        spots = [g for g in gates_by_region[r]
                 if g and g not in used and g not in doors]
        for _ in range(max(0, per_region[r])):
            if spots:
                cell = spots.pop(0)
            else:
                pool = free_in(regions[r])
                if not pool:
                    return None, None, None
                cell = pool[0]
            place[cell] = f"M_{mob_ids[idx]:03d}"
            used.add(cell)
            idx += 1

    potions = potions or [[], [POTION_20], [POTION_30], [POTION_30]]
    for r, pots in enumerate(potions[:4]):
        for pot in (pots or []):
            pool = free_in(regions[r])
            if not pool:
                continue
            place[pool[0]] = pot
            used.add(pool[0])

    if boss_id is not None:
        pool = free_in([order[-1]])
        pool.sort(key=lambda c: abs(c[0] - up[0]) + abs(c[1] - up[1]))
        if not pool:
            return None, None, None
        place[pool[0]] = f"B_{boss_id:03d}"
        used.add(pool[0])

    if equip_id is not None:
        pool = free_in(regions[3])
        if pool:
            place[pool[0]] = f"E_{equip_id:02d}"
            used.add(pool[0])

    for (x, y), v in place.items():
        grid[y][x] = v
    for i, (x, y) in enumerate(doors):
        grid[y][x] = DOOR_BY_ORDER[i]

    _add_walls(grid, wall_tiles, rng)
    return grid, regions, doors


def validate_layout(grid, regions, doors):
    """문을 순서대로 열며 스폰 -> 열쇠 -> 계단 도달이 가능한지 검사.

    몬스터가 선 칸도 지나갈 수 있는 것으로 본다 — 매직 타워에서 몬스터는
    벽이 아니라 통행료다. 문만 열쇠를 얻기 전까지 벽이다.
    """
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
                    seen.add((nx, ny))
                    continue
                seen.add((nx, ny))
                stack.append((nx, ny))
        return seen

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
