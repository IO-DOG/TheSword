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
  * 곁길 몬스터를 남긴다 — 지나가는 데는 필요 없고, 덤을 가지려면 잡아야 한다

몬스터는 벽이 아니라 통행료다. 그래서 도달 가능성 검사도 몬스터를 지나갈 수
있는 것으로 보고 한다(validate_layout).
"""

import os
import random
import re
import sys

GRID_W, GRID_H = 27, 23

VOID, FLOOR = "0", "1"
SPAWN = "11"
# 포탈 셀: 14=위층 계단, 15=아래층 계단(윗층에서 내려온 도착지점), 16=보스방
# PortalController.SearchPortal 은 위로 갈 때 "다음 맵의 DownStairs" 를 찾는다.
# 즉 F+1 층에 15 가 없으면 엔딩 처리가 되므로 1층을 뺀 모든 층에 15 가 필요하다.
STAIRS_UP = "14"
STAIRS_DOWN = "15"
# 문 셀 id. 색(열쇠)과 방향 두 가지로 갈린다 — 손수 만든 맵에서 확인한 규칙이다.
#
#   3 / 4 / 5   가로문 : 문의 <b>좌우</b>가 벽이어야 한다. 세로로 지나간다.
#   6 / 7 / 8   세로문 : 문의 <b>위아래</b>가 벽이어야 한다. 가로로 지나간다.
#
# 통로 방향과 문 방향이 어긋나면 벽이 없는 쪽으로 문틀이 떠 있게 되고, 옆으로
# 돌아갈 수 있는 것처럼 보인다. 방향은 놓는 자리가 정한다.
# 층마다 반드시 치러야 하는 통행료의 <b>정확한</b> 수. 다섯 마리 중 셋은 관문,
# 둘은 곁길이다 — 이 갈림이 곧 "강제와 선택" 이고, generate_content 의 나쁜 선택
# 재현("곁길을 전부 건너뛴다")이 이 수를 그대로 쓴다. 한쪽만 바꾸면 재현이 거짓이 된다.
#
# 예전에는 2 였다. 재 보니 96개 층 중 33개가 관문 둘뿐이라 층마다 강제의 양이
# 달랐고, 그러면 "곁길을 건너뛰면 몇 레벨이 모자란가" 를 셀 수가 없다.
MIN_TOLLS = 3

DOOR_H = {0: "3", 1: "4", 2: "5"}   # 좌우가 벽
DOOR_V = {0: "6", 1: "7", 2: "8"}   # 위아래가 벽
KEY_ITEM = {0: "I_00", 1: "I_01", 2: "I_02"}  # 초록/노랑/빨강 열쇠

# 손수 만든 도입부. CSV 는 세이브 인덱스용이고 실물은 프리팹이라, 문도 여기서
# 만든 규칙이 아니라 프리팹에 이미 구워져 있다.
HAND_AUTHORED = ("00_000", "00_001", "00_002", "00_003")

# 회복 아이템. 값은 ConsumableItemData 의 회복%와 짝이어야 한다.
POTION_20, POTION_30, POTION_50 = "I_03", "I_04", "I_06"

# 룬 = 스텟 영구 증가 (기획서 65·81쪽). ConsumableItemData 9/10/11 과 짝:
# 공격력 +1 / 방어력 +1 / 최대 체력 +5. 한 층에 하나씩 돌려가며 놓는다.
RUNE_ATK, RUNE_DEF, RUNE_HP = "I_09", "I_10", "I_11"

NEIGHBORS = ((1, 0), (-1, 0), (0, 1), (0, -1))

# 방 격자 3x3. 방 하나는 최대 7x5 이고, <b>크기는 층마다 다르다.</b>
ROOMS_X, ROOMS_Y = 3, 3
ROOM_W, ROOM_H = 7, 5                       # 가장 큰 방 = 방 사이 간격의 기준
GAP_X = (GRID_W - 2 - ROOMS_X * ROOM_W) // (ROOMS_X - 1)
GAP_Y = (GRID_H - 2 - ROOMS_Y * ROOM_H) // (ROOMS_Y - 1)
STRIDE_X, STRIDE_Y = ROOM_W + GAP_X, ROOM_H + GAP_Y

# 방 반지름 후보. 방 크기는 (2*hw+1) x (2*hh+1) — 5x3 / 5x5 / 7x3 / 7x5 넷이다.
#
# 왜 방 크기가 층마다 달라야 하는가
# ------------------------------
# 방 아홉 개가 고정 좌표에 7x5 로 파이면 격자 621칸 중 515칸이 <b>모든 층에서
# 같은 값</b>이 된다(82.9%). 그래서 두 층의 벽/바닥이 평균 97.7% 같았고, 문은
# 통로 밖 구간의 한가운데에만 앉아 96개 층이 단 12칸을 나눠 썼다.
# 방 크기를 흔들면 그 515칸이 통째로 흔들린다.
#
# <b>줄이는 쪽으로만</b> 흔든다. 방을 키우면 마지막 통로의 방 밖 구간이 짧아져
# last_neck 이 보스에게 한 칸 주고 나면 바닥나고, 룬이 폴백 가지로 떨어져 여덟
# 개 층에서 <b>그냥 지나칠 수 있는 룬</b>이 됐다 — 완주 보장이 거짓이 된다.
# 반대로 hw=1(3칸 방)까지 줄이면 방이 방으로 안 보이고 밟을 칸이 36% 줄어든다.
# 단조로움은 hw 2~3 만으로도 97.6% -> 85.3% 로 떨어진다(hw 1~3 은 83.0%).
HALF_W, HALF_H = (2, 3), (1, 2)

# 이 층의 방 반지름. build_floor_layout 이 씨앗으로 굴려 채운다.
# 방 기하 함수 넷이 층수도 씨앗도 받지 않는 순수 함수라 여기 둔다 — 단조로움의
# 뿌리가 바로 그것이었다. 한 층을 뽑고 나면 그 층의 값이 남으므로, 밖에서
# room_cells 를 부르려면 그 층을 뽑은 <b>직후</b>여야 한다.
_SHAPE = {}


def room_half(rx, ry):
    return _SHAPE.get((rx, ry), (ROOM_W // 2, ROOM_H // 2))


def room_center(rx, ry):
    """방 중심은 <b>격자에 고정</b>이다 — 흔들면 통로가 상대 방에 안 닿는다."""
    return 1 + rx * STRIDE_X + ROOM_W // 2, 1 + ry * STRIDE_Y + ROOM_H // 2


def room_origin(rx, ry):
    cx, cy = room_center(rx, ry)
    hw, hh = room_half(rx, ry)
    return cx - hw, cy - hh


def room_cells(rx, ry):
    ox, oy = room_origin(rx, ry)
    hw, hh = room_half(rx, ry)
    return [(x, y) for y in range(oy, oy + 2 * hh + 1)
            for x in range(ox, ox + 2 * hw + 1)]


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
    """방 크기를 <b>그 방에서</b> 읽는다. 고정값으로 재면 통로의 방 밖 구간이
    잘못 잡혀 문이 방 안에 앉고, check_doors 가 전량 위반으로 뜬다."""
    for ry in range(ROOMS_Y):
        for rx in range(ROOMS_X):
            ox, oy = room_origin(rx, ry)
            hw, hh = room_half(rx, ry)
            if ox <= cell[0] <= ox + 2 * hw and oy <= cell[1] <= oy + 2 * hh:
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



def _off_corridor_cells(room):
    """방 안에서 통로가 지나지 않는 칸.

    통로는 방 중심끼리 곧게 잇는다. 그래서 방의 가운데 가로줄/세로줄이
    그대로 통로다. 포탈처럼 지나갈 수 없는 것을 그 줄에 놓으면 마개가 되어
    방 뒤쪽이 통째로 막힌다(아래 계단이 실제로 그랬다).
    아이템이나 몬스터는 주우거나 부딪혀 지나갈 수 있으니 상관없다.
    """
    cx, cy = room_center(*room)
    return [c for c in room_cells(*room) if c[0] != cx and c[1] != cy]



def _passable(grid, cell):
    c = grid[cell[1]][cell[0]]
    return c != VOID and not c.startswith("W")


def _cuts_path(grid, start, goal, cell):
    """그 칸을 막으면 start 에서 goal 로 갈 수 없게 되는가.

    관문과 곁길을 가르는 유일한 기준이다. 예전에는 "통로 목" 이라는 눈대중으로
    골랐는데, 재 보니 관문의 3분의 1은 그냥 돌아갈 수 있었다 — 통행료가 아니었다.
    """
    if start == goal:
        return False

    seen = {start}
    stack = [start]
    while stack:
        x, y = stack.pop()
        for dx, dy in NEIGHBORS:
            n = (x + dx, y + dy)
            if not (0 <= n[0] < GRID_W and 0 <= n[1] < GRID_H) or n in seen:
                continue
            if not _passable(grid, n):
                continue
            seen.add(n)
            if n == cell:
                continue          # 이 칸은 막힌 것으로 친다
            stack.append(n)
    return goal not in seen



def _inside(cell):
    return 0 <= cell[0] < GRID_W and 0 <= cell[1] < GRID_H


def _touches_floor(grid, cell, allow):
    """그 칸이 허용된 칸 말고 다른 바닥과 맞닿아 있는가."""
    for dx, dy in NEIGHBORS:
        n = (cell[0] + dx, cell[1] + dy)
        if not _inside(n) or n in allow:
            continue
        if grid[n[1]][n[0]] != VOID:
            return True
    return False


def _carve_alcove(grid, rooms, rng):
    """방 가장자리에서 밖으로 두 칸을 파 <b>막다른 골방</b>을 만든다.

    왜 필요한가
    -----------
    "지키는 몬스터 뒤에 덤을 둔다" 를 배치만으로 해 봤다가 실패했다. 우리 방은
    7x5 열린 홀이라 어떤 칸도 보물로 가는 길을 끊지 못한다 — 항상 돌아서 접근할
    수 있어서, 파수꾼을 아무리 잘 놓아도 "잡아야 얻는다" 가 성립하지 않았다
    (150개 층 중 10% 만 실제로 지켜졌다).

    골방은 그 판단을 <b>구조로</b> 만든다. 입구가 한 칸뿐이라 그 칸을 막으면
    안쪽에 갈 수 없다. 그래서 입구에 선 몬스터는 처음으로 진짜 파수꾼이 되고,
    "이놈을 잡고 덤을 가질까, 그냥 지나갈까" 가 생긴다.

    <b>세로로만 판다.</b> 방 크기가 층마다 달라 간격이 2~6칸으로 오락가락하는데,
    가로로 파면 좁은 층에서 안쪽 칸이 옆방과 맞닿아 입구가 둘이 된다. 세로는
    가장 큰 방(7x5)에서도 3칸이 남아 어느 층에서나 두 칸을 팔 수 있다.

    (입구, 안쪽, 붙은 방 칸) 또는 (None, None, None).
    """
    cands = []
    for room in rooms:
        ox, oy = room_origin(*room)
        hw, hh = room_half(*room)
        cx, _ = room_center(*room)
        for x in range(ox, ox + 2 * hw + 1):
            if x == cx:
                continue                       # 통로가 지나는 줄은 피한다
            cands.append(((x, oy), (0, -1)))                 # 위로
            cands.append(((x, oy + 2 * hh), (0, 1)))         # 아래로
    rng.shuffle(cands)

    for (bx, by), (dx, dy) in cands:
        gate = (bx + dx, by + dy)
        prize = (bx + 2 * dx, by + 2 * dy)
        if not _inside(gate) or not _inside(prize):
            continue
        if grid[gate[1]][gate[0]] != VOID or grid[prize[1]][prize[0]] != VOID:
            continue
        # 입구는 방과 안쪽에만, 안쪽은 입구에만 맞닿아야 한다. 아니면 뒷문이 생긴다.
        if _touches_floor(grid, gate, {(bx, by), prize}):
            continue
        if _touches_floor(grid, prize, {gate}):
            continue
        grid[gate[1]][gate[0]] = FLOOR
        grid[prize[1]][prize[0]] = FLOOR
        return gate, prize, (bx, by)
    return None, None, None


def build_floor_layout(mob_ids, boss_id, wall_tiles, seed, mobs_in_floor=5,
                       with_down_stairs=True, equip_id=None, potions=None, rune=None):
    """한 층의 격자를 만든다.

    mob_ids : 이 층의 몬스터 id 들. 약한 놈부터 정렬돼 있어야 한다.
    potions : 구역별 회복 아이템 목록. 예) [[], ["I_03"], ["I_04"], ["I_06"]]
    rune    : 이 층에 놓을 룬 셀 코드. 마지막 구역(계단 앞)에 둔다.

    (grid, 구역별 방 목록, 문 좌표들) 반환.
    """
    rng = random.Random(seed)

    # 이 층의 방 크기. 중심은 고정이므로 통로는 그대로 곧게 남는다.
    _SHAPE.clear()
    for ry in range(ROOMS_Y):
        for rx in range(ROOMS_X):
            _SHAPE[(rx, ry)] = (rng.choice(HALF_W), rng.choice(HALF_H))

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
    # necks[i] 는 그 통로에서 방 밖으로 난 구간 전체 — 곁길이 없다면 이 칸들은
    # 전부 절단점이다. 마지막 통로(계단 방 입구)를 보스와 룬 자리로 쓴다.
    gates = []
    necks = []
    for i in range(len(order) - 1):
        cells = _corridor(grid, centers[i], centers[i + 1])
        outside = [c for c in cells if _outside_room(c)]
        necks.append(outside)
        # 한가운데가 아니라 <b>아무 데나</b>. 한가운데로 고정하면 96개 층의 문이
        # 통로마다 한 칸씩, 다 합쳐 12칸만 쓴다 — 그리고 몬스터의 62%가 그
        # 12칸 위에 선다. 통로 안이면 어디든 좌우(또는 위아래)가 벽이라 문 규칙은
        # 그대로 지켜진다.
        gates.append(rng.choice(outside) if outside else None)

    # <b>곁길 통로는 내지 않는다.</b> 문법을 깨고 있었다.
    #
    # 곁길은 순서상 세 칸 이상 떨어진 방 쌍에만 팠는데, 구역이 3/2/2/2 로 끊겨
    # 있어서 그 조건을 만족하는 쌍은 <b>반드시 구역 경계를 넘는다</b>. 그래서
    # 곁길이 뚫릴 때마다 문을 우회하는 길이 같이 생겼다 — 재 보니 96개 층 중
    # 67개가 문을 하나도 안 열고 뒤 구역 방에 닿았고, 48개 층은 열쇠를 둘 이상,
    # 9개 층은 셋 다 가질 수 있었다. "각 방에 열쇠를 둬서 잡는 순서를 강제한다"
    # 가 절반의 층에서 거짓이었던 것이다. 곁길을 0으로 두면 누수도 0이 된다.
    #
    # 지우고 잃는 것은 거의 없다. 곁길 몬스터는 191 -> 187 마리(층당 0.04마리)뿐
    # 줄었다 — 곁길 몬스터 수를 정하는 것은 통로가 아니라 forced = min(MIN_TOLLS,
    # mobs) = 3 이기 때문이다(다섯 중 셋이 관문, 둘은 곁길). 곁길 통로는 고를
    # 것을 못 늘리면서 문법만 깨고 생성 재시도만 늘리고 있었다(시도 107 -> 96).
    # 골방 파수꾼과 방 안의 남는 한 마리가 "피해 갈 수 있는 상대" 자리를 잇는다.

    # 막다른 골방. 입구 한 칸에 파수꾼을 세우고 안쪽에 덤을 둔다.
    # 통로를 다 판 뒤라야 뒷문이 생기지 않는다.
    alcove_gate, alcove_prize, alcove_base = _carve_alcove(grid, order, rng)

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

    # 골방이 붙은 방 칸은 비워 둔다.
    #
    # 포탈(계단)은 <b>지나갈 수 없다</b>. 그 칸이 골방으로 들어가는 유일한 길이면
    # 골방이 통째로 막혀 안의 덤을 영영 못 먹는다 — 98층이 실제로 그랬고,
    # 자동 플레이 봇은 닿지도 못하는 목표를 잡고 8분을 헤맸다.
    if alcove_base is not None:
        used.add(alcove_base)

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
        # 통로가 지나는 중앙선은 피한다 — 포탈은 지나갈 수 없어서 마개가 된다.
        pool = [c for c in _off_corridor_cells(order[0]) if c not in used]
        rng.shuffle(pool)
        if not pool:
            return None, None, None
        place[pool[0]] = STAIRS_DOWN
        used.add(pool[0])

    # 위 계단도 같은 이유로 중앙선을 피한다. 보스층에서는 보스가 그 뒤에 서므로
    # 계단이 마개가 되면 보스에게 갈 수가 없다.
    up_pool = [c for c in _off_corridor_cells(order[-1]) if c not in used]
    rng.shuffle(up_pool)
    if not up_pool:
        return None, None, None
    up = up_pool[0]
    place[up] = STAIRS_UP
    used.add(up)

    # 계단 방 입구 — 이 층에서 <b>반드시 밟는</b> 자리. 곁길을 내지 않았으므로
    # 여기 놓인 것은 무엇이든 강제가 된다. 보스와 룬이 이 자리를 먼저 가져간다
    # (몬스터 배치보다 먼저 잡아 둬야 통행료 몬스터에게 뺏기지 않는다).
    last_neck = [c for c in necks[-1]
                 if c not in used and _cuts_path(grid, spawn, up, c)]
    last_neck.sort(key=lambda c: abs(c[0] - up[0]) + abs(c[1] - up[1]))

    # 챕터 보스는 계단 방 입구에 세운다.
    # 예전에는 "계단에서 가장 가까운 빈 칸" 이었는데, 방이 7x5 열린 홀이라 어떤
    # 칸도 길을 끊지 못했다 — 재 보니 챕터 보스 다섯이 전부 그냥 지나칠 수 있는
    # 상대였다(관문인 보스 0/5). 보스를 안 잡고 다음 챕터로 올라갈 수 있었다.
    if boss_id is not None:
        if not last_neck:
            return None, None, None
        cell = last_neck.pop(0)
        place[cell] = f"B_{boss_id:03d}"
        used.add(cell)

    # 룬도 같은 자리에. 룬은 얻을지 말지가 흔들리면 완주 보장을 계산할 수 없다
    # (기획서 65·81쪽, CLAUDE.md "계단 앞 구역에 둔다").
    # 예전에는 마지막 구역의 아무 빈 칸이었고, 재 보니 96개 층 중 94개에서 그냥
    # 지나칠 수 있었다 — 그런데 완주 시뮬레이션은 룬을 늘 주운 것으로 셈하고
    # 있었으니 "완주 보장" 이 그만큼 거짓이었다.
    rune_cell = last_neck.pop(0) if last_neck else None
    if rune is not None and rune_cell is not None:
        place[rune_cell] = rune
        used.add(rune_cell)

    # 열쇠는 그 구역의 방 안에. 문을 열려면 그 구역을 훑어야 한다.
    for i in range(3):
        pool = free_in(regions[i])
        if not pool:
            return None, None, None
        place[pool[0]] = KEY_ITEM[i]
        used.add(pool[0])

    # 몬스터는 두 갈래로 나눈다.
    #
    #   관문 : 큰길 목에 선다. 지나가려면 반드시 값을 치른다.
    #   곁길 : 골방 입구와 방 안에 선다. 지나가는 데는 필요 없다 —
    #          잡으면 골방의 덤을 갖고, 안 잡으면 경험치를 버린다.
    #
    # 예전에는 다섯 마리를 전부 "통로 목" 에 세웠다. 그런데 재 보니 그중 78% 는
    # 그냥 돌아갈 수 있었다 — 관문이라 부르면서 실은 관문이 아니었고, 어느 것이
    # 강제인지는 아무도 몰랐다. 눈대중으로는 이 둘을 가를 수 없다.
    # 그래서 자리마다 "막으면 계단까지 못 가는가" 를 직접 계산해서 가른다.
    # 통로 둘이 한 칸을 공유하면 그 칸이 목록에 두 번 들어온다. 그대로 두면
    # 몬스터 둘이 같은 칸에 놓이고 뒤엣것이 앞엣것을 덮어써서, 그 층은 몹이
    # 넷뿐인 층이 된다 — "층의 몹을 다 잡으면 1레벨" 이 그만큼 어긋난다.
    # 96개 층 중 20개가 그랬다.
    def _unique(cells):
        out = []
        for c in cells:
            if c not in out:
                out.append(c)
        return out

    free_gates = _unique([g for g in gates if g and g not in used and g not in doors])

    toll_spots = [g for g in free_gates if _cuts_path(grid, spawn, up, g)]

    # 관문 자리가 모자라면 층 전체에서 찾는다.
    #
    # 통로 목이라고 다 통행료는 아니다. 재 보니 31/200 개 층은 강제 전투가
    # 하나도 없어서, 한 마리도 안 잡고 계단까지 갈 수 있었다. 그러면
    # "층의 몹을 다 잡으면 1레벨" 이라는 성장 곡선이 무너지고, 봇은 전부 잡으니
    # 검증에서도 안 걸린다 — 사람만 조용히 약해진 채 위층에서 죽는다.
    if len(toll_spots) < MIN_TOLLS:
        for r in range(len(regions)):
            for cell in free_in(regions[r]):
                if len(toll_spots) >= MIN_TOLLS:
                    break
                if cell in used or cell in doors or cell in toll_spots:
                    continue
                if _cuts_path(grid, spawn, up, cell):
                    toll_spots.append(cell)
    open_spots = [g for g in free_gates if g not in toll_spots]

    # 그래도 관문을 못 만들면 이 층은 버린다.
    #
    # 곁길 통로가 있던 시절에는 <b>어떤 칸을 막아도 우회로가 있는</b> 층이 있었다.
    # 그런 층은 한 마리도 안 잡고 계단까지 갈 수 있어 통행료라는 구조 자체가 없다.
    # 곁길을 없앤 지금은 96개 층이 전부 첫 시도에 통과해 이 가지가 돌지 않는다.
    # 그래도 남겨 둔다 — 배치 규칙을 손대면 다시 걸릴 수 있고, 억지로 배치하느니
    # 다른 씨앗으로 다시 뽑는 편이 낫다 (호출부가 50번까지 다시 시도한다).
    if len(toll_spots) < MIN_TOLLS:
        return None, None, None

    forced = min(3, mobs_in_floor)
    idx = 0

    # 관문 — 반드시 치러야 하는 통행료. 약한 놈부터 세운다.
    for _ in range(forced):
        if not toll_spots:
            break
        cell = toll_spots.pop(0)
        if cell in used:
            continue
        place[cell] = f"M_{mob_ids[idx]:03d}"
        used.add(cell)
        idx += 1

    # 골방 파수꾼도 곁길이다 — 지나가는 데는 필요 없고, 덤을 가지려면 잡아야 한다.
    # 층의 몹 수는 그대로 둔다 (다섯 마리를 다 잡아야 1레벨이라는 설계).
    if alcove_gate is not None and alcove_gate not in used:
        open_spots.insert(0, alcove_gate)

    # 곁길 — 피해 갈 수 있는 상대. 질러가려면 값을 치르고, 아니면 돌아간다.
    while idx < mobs_in_floor and open_spots:
        cell = open_spots.pop(0)
        if cell in used:
            continue
        place[cell] = f"M_{mob_ids[idx]:03d}"
        used.add(cell)
        idx += 1

    # 그래도 남으면 방 안에 둔다. 층마다 몹 수는 지켜야 레벨 곡선이 맞는다.
    while idx < mobs_in_floor:
        pool = free_in(regions[idx % 3])
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

    if rune is not None and rune_cell is None:
        # 계단 방 입구를 못 잡은 층의 폴백. 마지막 구역 안에는 두되, 이 층의 룬은
        # 강제가 아니다 — floor_choices 가 "지나칠 수 있는 아이템" 으로 센다.
        pool = free_in(regions[3])
        if pool:
            place[pool[0]] = rune
            used.add(pool[0])

    # 골방 안쪽의 덤. 완주 계산에는 넣지 않는다 — 안 잡고 지나갈 수 있으니
    # 이걸 셈에 넣으면 보장이 거짓말이 된다. 계산은 그대로 두고 덤만 얹는다.
    if alcove_prize is not None and alcove_prize not in used:
        place[alcove_prize] = POTION_20
        used.add(alcove_prize)

    if equip_id is not None:
        pool = free_in(regions[3])
        if pool:
            place[pool[0]] = f"E_{equip_id:02d}"
            used.add(pool[0])

    for (x, y), v in place.items():
        grid[y][x] = v
    # 통로가 어느 쪽으로 뻗어 있는지 보고 문 방향을 고른다.
    # 좌우로 뻗은 통로라면 위아래가 벽이 되므로 세로문,
    # 위아래로 뻗은 통로라면 좌우가 벽이 되므로 가로문이다.
    for i, (x, y) in enumerate(doors):
        horizontal_corridor = (grid[y][x - 1] != VOID and grid[y][x + 1] != VOID)
        grid[y][x] = (DOOR_V if horizontal_corridor else DOOR_H)[i]

    _add_walls(grid, wall_tiles, rng)
    return grid, regions, doors


# ---------------------------------------------------------------------------
# 실제로 놓이는 문 그림
#
# 문의 회전은 <b>프리팹에 구워져 있다</b> — MapBuilder.BuildDoor 는 회전을 주지
# 않고 이름으로 프리팹을 고를 뿐이다. 그런데 프리팹 이름은 셀 코드와 순서가
# 다르다. 셀 코드는 방향이 먼저(3/4/5 가로, 6/7/8 세로)인데, 프리팹은
# <b>색이 먼저고 방향이 나중</b>이다:
#
#   Tilemap_3/4 초록, 5/6 노랑, 7/8 빨강.  짝의 뒤쪽(4·6·8)만 Y 90° 로 돌아 있다.
#
# 그래서 셀 코드를 그대로 프리팹 이름으로 쓰면 두 규약이 3 과 8 에서만 겹치고
# 나머지는 어긋난다. 손수 만든 1~4층이 3 과 6 만 쓰는데 6 은 두 규약에서 방향이
# 같아서, 이 어긋남이 96개 생성 층에만 나타나 오래 안 보였다.
# 번역은 여기와 BuildDoor 두 곳뿐이고 서로 같아야 한다.


def check_sealed_by_portal(grid):
    """포탈이 막아서 못 가게 되는 몬스터·아이템을 찾는다.

    데이터에서는 이어져 있어도 <b>런타임에서는 포탈을 통과할 수 없다.</b>
    포탈이 어느 구역의 유일한 입구에 앉으면 그 뒤가 통째로 막힌다 —
    98층에서 골방 입구가 위 계단에 막혀 물약과 파수꾼에 닿을 수 없었고,
    봇은 닿지도 못하는 목표를 잡고 8분을 헤맸다.

    막힌 (좌표, 셀) 목록. 빈 목록이면 정상이다.
    """
    portals = {STAIRS_UP, STAIRS_DOWN, "16"}

    def cell_at(c):
        return grid[c[1]][c[0]].strip()

    start = None
    for y in range(len(grid)):
        for x in range(len(grid[y])):
            if cell_at((x, y)) == SPAWN:
                start = (x, y)
    if start is None:
        return []

    def flood(block_portals):
        seen = {start}
        stack = [start]
        while stack:
            x, y = stack.pop()
            for dx, dy in NEIGHBORS:
                n = (x + dx, y + dy)
                if not _inside(n) or n in seen:
                    continue
                v = cell_at(n)
                if v == VOID or v == "" or v.startswith("W"):
                    continue
                if block_portals and v in portals:
                    continue
                seen.add(n)
                stack.append(n)
        return seen

    reachable = flood(True)
    sealed = []
    for c in flood(False):
        if c in reachable:
            continue
        v = cell_at(c)
        if v[:1] in ("M", "I", "E", "B"):
            sealed.append((c, v))
    return sealed


def door_art(cell):
    """문 셀 코드 -> 런타임에 실제로 놓이는 프리팹 이름."""
    n = int(cell)
    return "Tilemap_%d" % (3 + ((n - 3) % 3) * 2 + (1 if n >= 6 else 0))


def door_prefab_facts():
    """문 프리팹 6종에서 {이름: (색, 세로문인가)} 를 읽는다.

    표를 파이썬에도 적어 두면 두 벌이 되어 같은 방식으로 다시 어긋난다. 그래서
    <b>디스크의 프리팹에서 직접</b> 읽는다 — 색은 소스 모델(Tilemap_Door_G/Y/R),
    방향은 문 노드에 구워진 Y 90° 회전으로 본다.
    프리팹을 못 읽으면 빈 dict 를 주고, 검사는 벽 규칙만 보고 넘어간다.
    """
    root = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
    colors = {}
    for i, c in enumerate("GYR"):
        meta = os.path.join(root, "Assets", "Resources", "Tilemap_Door_%s.obj.meta" % c)
        if not os.path.exists(meta):
            return {}
        with open(meta, encoding="utf-8") as f:
            m = re.search(r"guid: (\w+)", f.read())
        if not m:
            return {}
        colors[m.group(1)] = i

    facts = {}
    for i in range(3, 9):
        path = os.path.join(root, "Assets", "@Resources", "Prefabs", "Map",
                            "Tilemap_%d.prefab" % i)
        if not os.path.exists(path):
            return {}
        with open(path, encoding="utf-8") as f:
            t = f.read()
        m = re.search(r"m_SourcePrefab: \{fileID: \d+, guid: (\w+)", t)
        if not m or m.group(1) not in colors:
            return {}
        vertical = re.search(r"m_LocalEulerAnglesHint\.y\s+value: 90\b", t) is not None
        facts["Tilemap_%d" % i] = (colors[m.group(1)], vertical)
    return facts


def check_doors(grid, art=None):
    """문마다 <b>자리·그림·색</b>이 다 맞는지 본다. 어긋난 것 목록을 돌려준다.

    항목은 (셀코드, x, y, 사유).

    예전에는 셀 코드의 문서상 의미(좌우가 벽인가)만 봤다. 그래서 CSV 는 전부
    규칙에 맞는데 화면의 문은 90° 돌아 있는 상태를 그대로 통과시켰다 — 셀 코드에서
    프리팹 이름으로 가는 번역이 어긋나 있었고, 그 번역은 아무도 안 봤기 때문이다.
    이제 실제로 놓일 프리팹까지 같이 본다.
    """
    if art is None:
        art = door_prefab_facts()

    def is_wall(cx, cy):
        if not (0 <= cy < len(grid) and 0 <= cx < len(grid[cy])):
            return True                    # 격자 밖은 벽으로 친다
        return grid[cy][cx].startswith("W")

    bad = []
    for y in range(len(grid)):
        for x in range(len(grid[y])):
            cell = grid[y][x].strip()
            if cell not in ("3", "4", "5", "6", "7", "8"):
                continue

            vertical = int(cell) >= 6
            if vertical:                   # 세로문 — 위아래가 벽
                ok = is_wall(x, y - 1) and is_wall(x, y + 1)
            else:                          # 가로문 — 좌우가 벽
                ok = is_wall(x - 1, y) and is_wall(x + 1, y)
            if not ok:
                bad.append((cell, x, y, "벽이 없는 쪽으로 문틀이 뜬다"))

            name = door_art(cell)
            if name not in art:
                continue
            color, art_vertical = art[name]
            if art_vertical != vertical:
                bad.append((cell, x, y, "%s 은 %s문 그림이다" %
                            (name, "세로" if art_vertical else "가로")))
            if color != (int(cell) - 3) % 3:
                bad.append((cell, x, y, "%s 색이 열쇠 색(%d)과 다르다" %
                            (name, (int(cell) - 3) % 3)))
    return bad


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


# ---------------------------------------------------------------------------
# 강제와 선택을 <b>센다</b>
#
# "관문" 과 "곁길" 은 배치할 때 이미 갈라 놓지만, 그건 <b>의도</b>다. 완성된
# 격자에서 실제로 그런지는 다시 재야 한다 — 우회로가 하나라도 생기면 관문이
# 조용히 곁길이 되고, 그러면 "다 잡으면 1레벨" 도 "완주 보장" 도 같이 거짓이
# 된다. 곁길 통로가 있던 시절에 챕터 보스 다섯이 그렇게 전부 곁길이 돼 있었다.


def _endpoints(grid):
    spawn = up = None
    for y in range(len(grid)):
        for x in range(len(grid[y])):
            if grid[y][x] == SPAWN:
                spawn = (x, y)
            elif grid[y][x] == STAIRS_UP:
                up = (x, y)
    return spawn, up


def _unavoidable_mob(grid, doors, cell):
    """그 몬스터를 안 잡으면 정답 경로가 끊기는가.

    그 칸을 벽으로 막고 validate_layout 을 <b>그대로</b> 다시 돌린다. 열쇠가
    닿지 않게 되는 것까지 잡히므로, "지나가는 데는 필요 없지만 열쇠를 지키고
    있는" 놈도 관문으로 센다. 눈대중으로는 이 둘을 가를 수 없다.
    """
    blocked = [list(row) for row in grid]
    blocked[cell[1]][cell[0]] = "W_00"
    return not validate_layout(blocked, None, doors)[0]


def floor_choices(grid, doors):
    """한 층의 <b>강제</b>와 <b>선택</b>을 센다.

    돌려주는 dict:
      forced_mobs / optional_mobs   관문 / 곁길 몬스터 (보스 제외)
      boss / boss_forced            보스가 있는가 / 그 보스가 관문인가
      forced_items / optional_items 반드시 밟는 / 지나칠 수 있는 아이템 (열쇠 제외)
      runes / forced_runes          룬 / 그중 반드시 밟는 것
      keys                          열쇠 (문이 강제하므로 늘 필수다)
      dead_ends                     막다른 자리 수 = 골방 덤이 놓일 수 있는 곳

    몬스터는 "막으면 경로가 끊기는가"(validate_layout), 아이템은 "계단까지 가는
    모든 길이 이 칸을 지나는가"(_cuts_path)로 잰다. 아이템은 밟으면 바로 줍기
    때문이다. 열쇠는 연결성이 아니라 문이 강제하므로 따로 센다.
    """
    spawn, up = _endpoints(grid)
    out = dict(forced_mobs=0, optional_mobs=0, boss=0, boss_forced=0,
               forced_items=0, optional_items=0, runes=0, forced_runes=0,
               keys=0, dead_ends=0)
    if spawn is None or up is None:
        return out

    for y in range(len(grid)):
        for x in range(len(grid[y])):
            cell = grid[y][x]
            if cell.startswith("M_"):
                if _unavoidable_mob(grid, doors, (x, y)):
                    out["forced_mobs"] += 1
                else:
                    out["optional_mobs"] += 1
            elif cell.startswith("B_"):
                out["boss"] += 1
                if _unavoidable_mob(grid, doors, (x, y)):
                    out["boss_forced"] += 1
            elif cell in KEY_ITEM.values():
                out["keys"] += 1
            elif cell.startswith(("I_", "E_")):
                forced = _cuts_path(grid, spawn, up, (x, y))
                out["forced_items" if forced else "optional_items"] += 1
                if cell in (RUNE_ATK, RUNE_DEF, RUNE_HP):
                    out["runes"] += 1
                    out["forced_runes"] += 1 if forced else 0
            if cell != VOID and not cell.startswith("W"):
                nbr = sum(1 for dx, dy in NEIGHBORS
                          if _inside((x + dx, y + dy)) and _passable(grid, (x + dx, y + dy)))
                if nbr == 1:
                    out["dead_ends"] += 1
    return out


def check_all_floors(verbose=True):
    """StreamingAssets 의 던전 CSV 를 전부 훑어 문 규칙을 검사한다.

        python layout_gen.py

    생성 층에서 어긋난 문의 수를 돌려준다(0 이면 통과). 손수 만든 1~4층은
    실물이 프리팹이라 따로 세어 참고로만 찍는다.
    """
    root = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
    excel = os.path.join(root, "Assets", "StreamingAssets", "Data", "Excel")
    art = door_prefab_facts()
    if verbose:
        if art:
            for name in sorted(art, key=lambda k: int(k.split("_")[1])):
                color, vertical = art[name]
                print("  프리팹 %-11s %s / %s문" %
                      (name, "초록노랑빨강"[color * 2:color * 2 + 2],
                       "세로" if vertical else "가로"))
        else:
            print("  [경고] 문 프리팹을 못 읽었다 — 벽 규칙만 검사한다")

    gen_doors = gen_bad = hand_doors = hand_bad = 0
    floors = 0
    for fn in sorted(os.listdir(excel)):
        if not fn.startswith("Dungeon_") or not fn.endswith(".csv"):
            continue
        did = fn[len("Dungeon_"):-len(".csv")]
        with open(os.path.join(excel, fn), encoding="utf-8-sig") as f:
            grid = [[c.strip() for c in line.split(",")] for line in f.read().splitlines()]
        doors = sum(1 for row in grid for c in row if c.strip() in ("3", "4", "5", "6", "7", "8"))
        bad = check_doors(grid, art)
        if did in HAND_AUTHORED:
            hand_doors += doors
            hand_bad += len(bad)
            continue
        floors += 1
        gen_doors += doors
        gen_bad += len(bad)
        if bad and verbose:
            for cell, x, y, why in bad:
                print("  [위반] %s 셀 %s (행%d, 열%d) — %s" % (did, cell, y, x, why))

    if verbose:
        print("  생성 층 %d개 / 문 %d개 — 위반 %d건" % (floors, gen_doors, gen_bad))
        print("  (참고) 손수 만든 층 문 %d개 — 위반 %d건" % (hand_doors, hand_bad))
    return gen_bad


def _shape_self_check(seeds=40):
    """방 크기를 흔들어도 규칙이 지켜지는지 씨앗 몇 개로 <b>지금</b> 확인한다.

    check_all_floors 는 디스크의 CSV 를 읽는다 — 생성기를 고쳐도 --write 를
    돌리기 전에는 아무것도 안 보인다. 특히 _outside_room 이 방 크기를 고정값으로
    재면 통로의 방 밖 구간이 잘못 잡혀 <b>문이 방 안에 앉는다.</b>
    돌려주는 값은 씨앗들이 만든 서로 다른 방 모양 조합의 수다.
    """
    art = door_prefab_facts()
    shapes = set()
    for seed in range(seeds):
        grid, regions, doors = build_floor_layout(
            [1, 2, 3, 4, 5], 9, ["W_01", "W_02"], seed=seed, rune=RUNE_ATK)
        assert grid is not None, f"씨앗 {seed} 층 생성 실패"
        ok, why = validate_layout(grid, regions, doors)
        assert ok, f"씨앗 {seed} {why}"
        bad = check_doors(grid, art)
        assert not bad, f"씨앗 {seed} 문 위반 {bad}"
        assert not check_sealed_by_portal(grid), f"씨앗 {seed} 포탈이 막았다"
        shapes.add(tuple(sorted(_SHAPE.items())))
    assert len(shapes) > seeds // 2, f"방 모양이 {len(shapes)}가지뿐이다"
    return len(shapes)


if __name__ == "__main__":
    # 번역표 자기검사. 이게 깨지면 화면의 문이 90° 돌아간다.
    assert [door_art(c) for c in "345678"] == \
        ["Tilemap_3", "Tilemap_5", "Tilemap_7", "Tilemap_4", "Tilemap_6", "Tilemap_8"]
    print("===== 문 배치 검사 =====")
    print("  갓 뽑은 층 40개 — 방 모양 %d가지, 문 위반 0건"
          % _shape_self_check())
    sys.exit(1 if check_all_floors() else 0)
