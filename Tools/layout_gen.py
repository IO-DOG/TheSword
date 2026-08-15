"""층 레이아웃(미로) 생성기.

CSV 격자 규약은 DataManager.ResetActiveDic 의 파서와 일치해야 한다:
  "0"/빈칸 = 없음(Void), "1" = 바닥, "W_xx" = 벽,
  "M_xxx" = 몬스터(MonsterData.id), "B_xxx" = 보스, "I_xx" = 소비아이템, "E_xx" = 장비,
  "3".."8" = 문, "11" = 스폰, "12" = 레버, "13" = 기둥, "14".."16" = 포탈
파서는 셀에서 숫자만 뽑아 id 로 쓴다: "W_03"->3, "M_004"->4.

구조 원칙(사용자 지정 "순서가 중요한 미로"):
  방0(스폰) -[초록문]- 방1 -[노랑문]- 방2 -[빨강문]- 방3(계단/보스)
  각 방에 다음 문을 여는 열쇠를 두어, 몬스터를 정해진 순서로 잡아야만 진행된다.
"""

import random

GRID_W, GRID_H = 27, 23
ROOM_W, ROOM_H = 5, 5

VOID, FLOOR = "0", "1"
SPAWN = "11"
# 포탈 셀: 14=위층 계단, 15=아래층 계단(윗층에서 내려온 도착지점), 16=보스방
# PortalController.SearchPortal 은 위로 갈 때 "다음 맵의 DownStairs" 를 찾는다.
# 즉 F+1 층에 15 가 없으면 엔딩 처리가 되므로 1층을 뺀 모든 층에 15 가 필요하다.
STAIRS_UP = "14"
STAIRS_DOWN = "15"
DOOR_BY_ORDER = {0: "3", 1: "4", 2: "5"}      # 문 셀 id (열쇠 0/1/2 에 대응)
KEY_ITEM = {0: "I_00", 1: "I_01", 2: "I_02"}  # 초록/노랑/빨강 열쇠
POTION_30, POTION_50 = "I_04", "I_06"         # 회복 30% / 50%

COARSE = [(cx, cy) for cy in range(3) for cx in range(3)]


def _room_origin(cx, cy):
    x = 1 + cx * ((GRID_W - 2 - ROOM_W) // 2)
    y = 1 + cy * ((GRID_H - 2 - ROOM_H) // 2)
    return x, y


def _room_cells(ox, oy):
    return [(x, y) for y in range(oy, oy + ROOM_H) for x in range(ox, ox + ROOM_W)]


def _chain(rng):
    """인접한 코스 격자 칸 4개를 잇는 경로."""
    for _ in range(200):
        path = [rng.choice(COARSE)]
        while len(path) < 4:
            cx, cy = path[-1]
            nbrs = [(cx + dx, cy + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                    if 0 <= cx + dx < 3 and 0 <= cy + dy < 3
                    and (cx + dx, cy + dy) not in path]
            if not nbrs:
                break
            path.append(rng.choice(nbrs))
        if len(path) == 4:
            return path
    return [(0, 0), (1, 0), (2, 0), (2, 1)]


def _carve_room(grid, ox, oy):
    for y in range(oy, oy + ROOM_H):
        for x in range(ox, ox + ROOM_W):
            grid[y][x] = FLOOR


def _carve_corridor(grid, a, b, rng):
    """L자 통로를 판다. 통로에 새로 판 셀 목록을 돌려준다."""
    (ax, ay), (bx, by) = a, b
    cells = []
    if rng.random() < 0.5:
        cells += [(x, ay) for x in range(min(ax, bx), max(ax, bx) + 1)]
        cells += [(bx, y) for y in range(min(ay, by), max(ay, by) + 1)]
    else:
        cells += [(ax, y) for y in range(min(ay, by), max(ay, by) + 1)]
        cells += [(x, by) for x in range(min(ax, bx), max(ax, bx) + 1)]
    for (x, y) in cells:
        grid[y][x] = FLOOR
    return cells


def _add_walls(grid, wall_tiles, rng):
    """바닥과 맞닿은 빈 칸을 벽으로 채운다."""
    for y in range(GRID_H):
        for x in range(GRID_W):
            if grid[y][x] != VOID:
                continue
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < GRID_W and 0 <= ny < GRID_H and grid[ny][nx] != VOID:
                        grid[y][x] = rng.choice(wall_tiles)
                        break
                if grid[y][x] != VOID:
                    break


def build_floor_layout(mob_id, boss_id, wall_tiles, seed, mobs_in_floor=5,
                       with_down_stairs=True, equip_id=None):
    """한 층의 격자를 만든다. (grid, 방 원점들, 문 좌표들) 반환."""
    rng = random.Random(seed)
    grid = [[VOID] * GRID_W for _ in range(GRID_H)]

    origins = [_room_origin(cx, cy) for cx, cy in _chain(rng)]
    centers = [(ox + ROOM_W // 2, oy + ROOM_H // 2) for ox, oy in origins]
    for ox, oy in origins:
        _carve_room(grid, ox, oy)

    all_room_cells = set()
    for o in origins:
        all_room_cells.update(_room_cells(*o))

    doors = []
    for i in range(3):
        cells = _carve_corridor(grid, centers[i], centers[i + 1], rng)
        outside = [c for c in cells if c not in all_room_cells]
        if not outside:
            return None, None, None
        doors.append(outside[len(outside) // 2])

    # 막다른 길 (미로 느낌)
    for _ in range(3):
        cx, cy = rng.choice(centers)
        dx, dy = rng.choice([(1, 0), (-1, 0), (0, 1), (0, -1)])
        for step in range(1, rng.randint(3, 6)):
            nx, ny = cx + dx * step, cy + dy * step
            if 1 <= nx < GRID_W - 1 and 1 <= ny < GRID_H - 1 and grid[ny][nx] == VOID:
                grid[ny][nx] = FLOOR

    door_set = set(doors)

    def free_slots(idx):
        ox, oy = origins[idx]
        cs = [(x, y) for (x, y) in _room_cells(ox, oy)
              if grid[y][x] == FLOOR and (x, y) not in door_set]
        rng.shuffle(cs)
        return cs

    place = {}
    # 몹 배치: 방0에 1, 방1에 2, 방2에 나머지 (순서대로 강해지는 진행)
    per_room = [1, 2, mobs_in_floor - 3]

    s = free_slots(0)
    place[s.pop()] = SPAWN
    if with_down_stairs:
        place[s.pop()] = STAIRS_DOWN
    for _ in range(per_room[0]):
        place[s.pop()] = f"M_{mob_id:03d}"
    place[s.pop()] = KEY_ITEM[0]

    s = free_slots(1)
    for _ in range(per_room[1]):
        place[s.pop()] = f"M_{mob_id:03d}"
    place[s.pop()] = POTION_30
    place[s.pop()] = KEY_ITEM[1]

    s = free_slots(2)
    for _ in range(per_room[2]):
        place[s.pop()] = f"M_{mob_id:03d}"
    place[s.pop()] = POTION_30
    place[s.pop()] = KEY_ITEM[2]

    s = free_slots(3)
    place[s.pop()] = STAIRS_UP
    if boss_id is not None:
        place[s.pop()] = f"B_{boss_id:03d}"
        place[s.pop()] = POTION_50
        place[s.pop()] = POTION_50
    if equip_id is not None:
        place[s.pop()] = f"E_{equip_id:02d}"

    for (x, y), v in place.items():
        grid[y][x] = v
    for i, (x, y) in enumerate(doors):
        grid[y][x] = DOOR_BY_ORDER[i]

    _add_walls(grid, wall_tiles, rng)
    return grid, origins, doors


def validate_layout(grid, origins, doors):
    """문을 순서대로 열며 스폰 -> 각 방 -> 계단 도달이 가능한지 검사."""
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
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
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

    opened = set()
    for i in range(4):
        reach = flood(opened)
        if not any(c in reach for c in _room_cells(*origins[i])):
            return False, f"방{i} 도달 불가"
        if i < 3:
            if doors[i] not in reach:
                return False, f"문{i} 도달 불가"
            opened.add(doors[i])
    if stairs not in flood(opened):
        return False, "계단 도달 불가"
    return True, None
