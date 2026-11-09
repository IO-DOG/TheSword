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
PORTAL_CELLS = frozenset((STAIRS_UP, STAIRS_DOWN, "16"))   # 런타임에 못 지나간다
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
DOOR_CELLS = frozenset("345678")
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

# ---------------------------------------------------------------------------
# 방 배치 — 층마다 틀이 다르다
#
# 예전에는 3x3 고정 격자에 방 아홉을 놓고 방 중심끼리 곧게 이었다. 그래서 96개 층이
# 한 틀이었다 — 바닥 칸 Jaccard 평균 0.64, 0.8 을 넘는 층 쌍이 269개였고, 거울상까지
# 똑같은 쌍도 있었다(11층과 67층). 문은 통로의 <b>아무 칸</b>에 앉아서, 방에서 떨어진
# 긴 통로 한가운데 문짝만 덩그러니 섰다.
#
# 지금은 층마다
#   * 틀(열 2~4 x 행 2~3)과 띠 폭을 굴리고,
#   * 띠 안에서 방의 크기·자리를 굴리고, 일부 방은 모양을 바꾼다(기둥·ㄱ자·칸막이),
#   * 이웃한 두 방을 곧은 통로나 한 번 꺾인 통로로 잇는다.
# 통로는 두 방 사이 틈에만 판다. 다른 통로·방에 맞닿으면 그 씨앗은 버린다 —
# 맞닿는 순간 문을 돌아가는 길이 생겨 "문 셋이 순서를 강제한다" 가 깨진다.
# 문은 <b>문턱</b>(통로가 방에 닿는 첫 칸)에만 세운다. 그래야 방의 벽 줄에 붙어 보인다.
# 남는 빈 땅은 전부 바위(벽)로 채운다 — 비워 두면 그 자리에 바닥 그림(BG)이 깔려
# 걸을 수 있는 맨땅처럼 보였다.

INNER_X, INNER_Y = (1, GRID_W - 2), (1, GRID_H - 2)   # 바깥 한 줄은 테두리 벽

# 층 유형(generate_content.FLOOR_TYPES 의 번호) -> 틀 후보 (열, 행)
#   0 기본 · 4 보물 : 큰길 방 아홉
#   1 인색          : 큰길 방 여섯
#   2 관문          : 작은 방 아홉 — 통로가 길어진다
#   3 넉넉          : 큰길 방 여섯 + 곁방 셋
FRAMES = {
    0: ((3, 3), (4, 3)),
    1: ((2, 3), (3, 2), (4, 2), (3, 3), (4, 3)),
    2: ((3, 3), (4, 3)),
    3: ((3, 3), (4, 3)),
    4: ((3, 3), (4, 3)),
}
PATH_ROOMS = {0: 9, 1: 6, 2: 9, 3: 6, 4: 9}
BRANCH_ROOMS = {3: 3}

# 방 한 변의 범위. 띠가 좁으면 띠에 맞춘다. 3칸보다 좁으면 방으로 안 보인다.
ROOM_W_RANGE, ROOM_H_RANGE = (4, 9), (3, 7)
GAUNTLET_W_RANGE, GAUNTLET_H_RANGE = (3, 5), (3, 3)
BAND_MIN = 5                 # 띠 폭의 하한 — 양쪽을 한 칸씩 비운 안쪽 띠에도 3칸 방이 들어간다
STRAIGHT_LINK = 0.4          # 곧게 이을 수 있을 때 곧게 잇는 몫 — 나머지는 한 번 꺾는다
RESHAPE = 0.55               # 모양을 바꿀 수 있는 방 가운데 실제로 바꾸는 몫


class _Room:
    __slots__ = ("rect", "cells", "anchor")

    def __init__(self, rect):
        self.rect = rect
        x0, y0, x1, y1 = rect
        self.cells = [(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1)]
        self.anchor = ((x0 + x1) // 2, (y0 + y1) // 2)


# 이 층의 방과 통로 입구. build_floor_layout 이 씨앗으로 굴려 채운다. 한 층을 뽑고
# 나면 그 층의 값이 남으므로, 밖에서 room_cells 를 부르려면 그 층을 뽑은 <b>직후</b>여야 한다.
_ROOMS = {}
_MOUTHS = set()      # 통로가 방에 닿는 방 칸. 포탈·골방·모양 바꾸기가 그 둘레를 피한다.


def room_cells(room):
    return list(_ROOMS[room].cells)


def room_center(room):
    return _ROOMS[room].anchor


def _near_mouths():
    return {(mx + dx, my + dy) for mx, my in _MOUTHS for dx in (-1, 0, 1) for dy in (-1, 0, 1)}


def _split(lo, hi, n, rng):
    """lo..hi 칸을 띠 n 개로 나눈다. 띠마다 BAND_MIN 칸 이상, 남는 칸은 무작위로 흩는다."""
    sizes = [BAND_MIN] * n
    spare = (hi - lo + 1) - BAND_MIN * n
    if spare < 0:
        return None
    for _ in range(spare):
        sizes[rng.randrange(n)] += 1
    bands, s = [], lo
    for w in sizes:
        bands.append((s, s + w - 1))
        s += w
    return bands


def _span(band, limits, size_range, rng):
    """띠 안에서 방 한 변의 (시작, 끝).

    다른 띠와 맞댄 쪽은 한 칸 비운다 — 이웃 방과 사이에 두 칸 이상의 틈이 남고, 그 틈이
    통로가 된다. 맵 가장자리 쪽은 테두리 벽이 있으니 비우지 않는다."""
    b0, b1 = band
    m0 = 0 if b0 == limits[0] else 1
    m1 = 0 if b1 == limits[1] else 1
    room_max = (b1 - m1) - (b0 + m0) + 1
    lo, hi = size_range
    hi = min(hi, room_max)
    lo = min(lo, hi)
    if lo < 3:
        return None
    size = rng.randint(lo, hi)
    start = rng.randint(b0 + m0, b1 - m1 - size + 1)
    return start, start + size - 1


def _lattice_path(cols, rows, n, rng):
    """틀의 칸 n 개를 이웃끼리 한 번씩 훑는 길(무작위 깊이 우선 + 되짚기)."""
    cells = [(x, y) for y in range(rows) for x in range(cols)]
    for _ in range(60):
        path = [rng.choice(cells)]
        seen = set(path)

        def grow():
            if len(path) == n:
                return True
            x, y = path[-1]
            nbrs = [(x + dx, y + dy) for dx, dy in NEIGHBORS
                    if 0 <= x + dx < cols and 0 <= y + dy < rows and (x + dx, y + dy) not in seen]
            rng.shuffle(nbrs)
            for c in nbrs:
                path.append(c)
                seen.add(c)
                if grow():
                    return True
                path.pop()
                seen.discard(c)
            return False

        if grow():
            return path
    return None


def _link(a, b, rng):
    """이웃한 두 방 a -> b 를 잇는 통로. (칸 목록 a 쪽부터, a 입구, b 입구) 또는 None.

    통로는 두 방 사이 틈에만 판다. 꺾을 때는 틈 <b>안에서</b> 한 번 꺾고 양 끝을 한 칸
    이상 곧게 둔다 — 그 끝 칸이 문턱이고, 문턱은 양옆이 벽이어야 문이 선다.
    방의 모서리 줄로는 잇지 않는다. 입구 양옆이 방이어야 문턱의 문이 방 벽 줄에 붙는다.
    """
    ra, rb = a.rect, b.rect
    flip = not (ra[2] < rb[0] or rb[2] < ra[0])        # 위아래 이웃이면 축을 바꿔 같은 식으로
    if flip:
        ra = (ra[1], ra[0], ra[3], ra[2])
        rb = (rb[1], rb[0], rb[3], rb[2])
    rev = ra[0] > rb[0]
    if rev:
        ra, rb = rb, ra
    ga, gb = ra[2] + 1, rb[0] - 1                       # 틈의 양 끝 = 두 문턱
    rows_a = list(range(ra[1] + 1, ra[3]))
    rows_b = list(range(rb[1] + 1, rb[3]))
    if gb <= ga or not rows_a or not rows_b:
        return None
    common = [y for y in rows_a if y in rows_b]
    can_bend = gb - ga >= 2
    if common and (not can_bend or rng.random() < STRAIGHT_LINK):
        ya = yb = rng.choice(common)
    elif can_bend:
        ya, yb = rng.choice(rows_a), rng.choice(rows_b)
    else:
        return None
    if ya == yb:
        cells = [(x, ya) for x in range(ga, gb + 1)]
    else:
        gx = rng.randint(ga + 1, gb - 1)
        step = 1 if yb > ya else -1
        cells = [(x, ya) for x in range(ga, gx + 1)]
        cells += [(gx, y) for y in range(ya + step, yb + step, step)]
        cells += [(x, yb) for x in range(gx + 1, gb + 1)]
    mouth_a, mouth_b = (ra[2], ya), (rb[0], yb)
    if rev:
        cells.reverse()
        mouth_a, mouth_b = mouth_b, mouth_a
    if flip:
        cells = [(y, x) for x, y in cells]
        mouth_a, mouth_b = (mouth_a[1], mouth_a[0]), (mouth_b[1], mouth_b[0])
    return cells, mouth_a, mouth_b


def _free_for(grid, cells, ends):
    """새 통로가 빈 땅에만 놓이고, 두 입구 말고는 어떤 바닥에도 맞닿지 않는가."""
    own = set(cells) | set(ends)
    for x, y in cells:
        if grid[y][x] != VOID:
            return False
        for dx, dy in NEIGHBORS:
            n = (x + dx, y + dy)
            if n not in own and _inside(n) and grid[n[1]][n[0]] != VOID:
                return False
    return True


def _connected(cells):
    cells = set(cells)
    if not cells:
        return False
    start = next(iter(cells))
    seen, stack = {start}, [start]
    while stack:
        x, y = stack.pop()
        for dx, dy in NEIGHBORS:
            n = (x + dx, y + dy)
            if n in cells and n not in seen:
                seen.add(n)
                stack.append(n)
    return len(seen) == len(cells)


def _reshape(grid, room, rng):
    """방 모양을 바꾼다 — 기둥 / ㄱ자 / 칸막이. 통로 입구 둘레와 중심은 남긴다.

    네모 방만 이어 붙이면 틀이 달라도 같은 그림으로 보인다. 깎아 낸 칸은 바위가 된다.
    깎은 뒤 방이 둘로 갈리거나 막다른 칸이 생기면 되돌린다.
    """
    x0, y0, x1, y1 = room.rect
    w, h = x1 - x0 + 1, y1 - y0 + 1
    options = []
    if w >= 5 and h >= 5:
        options.append("pillars")
    if w >= 6 and h >= 4:
        options.append("notch")
    if w >= 7 and h >= 5 or w >= 5 and h >= 7:
        options.append("divider")
    if not options or rng.random() >= RESHAPE:
        return
    kind = rng.choice(options)
    if kind == "pillars":
        cut = {(x, y) for y in range(y0 + 1, y1, 2) for x in range(x0 + 1, x1, 2)}
    elif kind == "notch":
        cw, ch = rng.randint(2, w - 4), rng.randint(1, h - 3)
        corner = rng.randrange(4)
        xs = range(x0, x0 + cw) if corner in (0, 2) else range(x1 - cw + 1, x1 + 1)
        ys = range(y0, y0 + ch) if corner in (0, 1) else range(y1 - ch + 1, y1 + 1)
        cut = {(x, y) for x in xs for y in ys}
    elif w >= h:
        cut = {(x, (y0 + y1) // 2) for x in range(x0 + 2, x1 - 1)}
    else:
        cut = {((x0 + x1) // 2, y) for y in range(y0 + 2, y1 - 1)}
    cut -= _near_mouths() | {room.anchor}
    if not cut:
        return
    rest = [c for c in room.cells if c not in cut]
    keep = set(rest)
    if not _connected(rest):
        return
    for x, y in rest:
        if sum((x + dx, y + dy) in keep for dx, dy in NEIGHBORS) < 2:
            return
    room.cells = rest
    for x, y in cut:
        grid[y][x] = VOID


def _fill_rock(grid, wall_tiles, rng):
    """빈 땅을 전부 바위로 채운다. 예전에는 바닥에 닿은 칸만 벽으로 세웠다."""
    for y in range(GRID_H):
        for x in range(GRID_W):
            if grid[y][x] == VOID:
                grid[y][x] = rng.choice(wall_tiles)


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


def _portal_cells(grid, room, used):
    """포탈(계단)을 놓아도 되는 칸.

    포탈은 <b>지나갈 수 없다.</b> 그 칸을 막아도 바닥이 둘로 갈리지 않아야 하고, 통로
    입구 둘레(한 칸)는 비운다 — 입구를 막으면 방 뒤가 통째로 막힌다(예전 아래 계단).
    """
    near = _near_mouths()
    floor = [(x, y) for y in range(GRID_H) for x in range(GRID_W) if grid[y][x] != VOID]
    out = []
    for c in _ROOMS[room].cells:
        if c in used or c in near:
            continue
        rest = [f for f in floor if f != c]
        if _connected(rest):
            out.append(c)
    return out


def _carve_alcove(grid, rooms, rng, avoid=()):
    """방 가장자리에서 밖으로 두 칸을 파 <b>막다른 골방</b>을 만든다.

    avoid 의 방에는 파지 않는다. 후보 목록에서 빼지 않고 <b>건너뛰기만</b> 한다 —
    목록 길이가 같아야 섞는 데 쓰는 씨앗 수가 같다.

    왜 필요한가
    -----------
    "지키는 몬스터 뒤에 덤을 둔다" 를 배치만으로 해 봤다가 실패했다. 열린 방에서는
    어떤 칸도 보물로 가는 길을 끊지 못한다 — 항상 돌아서 접근할 수 있어서, 파수꾼을
    아무리 잘 놓아도 "잡아야 얻는다" 가 성립하지 않았다(150개 층 중 10% 만 지켜졌다).
    골방은 그 판단을 <b>구조로</b> 만든다. 입구가 한 칸뿐이라 그 칸을 막으면 안쪽에
    갈 수 없다. 그래서 입구에 선 몬스터는 진짜 파수꾼이 된다.

    방의 모서리 칸과 통로 입구 둘레에서는 파지 않는다. 골방 입구(금고 문 자리)의
    양옆이 방이어야 문이 방 벽 줄에 붙는다(check_structure).
    네 방향 어디로든 파되, 입구는 방과 안쪽에만, 안쪽은 입구에만 맞닿아야 한다 —
    아니면 뒷문이 생긴다.

    (입구, 안쪽, 붙은 방 칸) 또는 (None, None, None).
    """
    near = _near_mouths()
    cands = []
    for room in rooms:
        cells = set(_ROOMS[room].cells)
        for bx, by in _ROOMS[room].cells:
            if (bx, by) in near:
                continue
            for dx, dy in NEIGHBORS:
                if (bx + dx, by + dy) in cells:
                    continue                       # 밖으로만 판다
                if (bx + dy, by + dx) not in cells or (bx - dy, by - dx) not in cells:
                    continue                       # 모서리 칸
                cands.append(((bx, by), (dx, dy), room))
    rng.shuffle(cands)

    for (bx, by), (dx, dy), room in cands:
        if room in avoid:
            continue
        gate = (bx + dx, by + dy)
        prize = (bx + 2 * dx, by + 2 * dy)
        # 한 칸 안쪽까지만 판다. 테두리까지 파면 그 바깥에 벽을 둘 자리가 없다.
        if not all(1 <= c[0] < GRID_W - 1 and 1 <= c[1] < GRID_H - 1 for c in (gate, prize)):
            continue
        if grid[gate[1]][gate[0]] != VOID or grid[prize[1]][prize[0]] != VOID:
            continue
        if _touches_floor(grid, gate, {(bx, by), prize}):
            continue
        if _touches_floor(grid, prize, {gate}):
            continue
        grid[gate[1]][gate[0]] = FLOOR
        grid[prize[1]][prize[0]] = FLOOR
        return gate, prize, (bx, by)
    return None, None, None


def build_floor_layout(mob_ids, boss_id, wall_tiles, seed, mobs_in_floor=5,
                       with_down_stairs=True, equip_id=None, potions=None, rune=None,
                       alcove=None, layout_kind=0, choice=None):
    """한 층의 격자를 만든다.

    mob_ids : 이 층의 몬스터 id 들. 약한 놈부터 정렬돼 있어야 한다.
    potions : 구역별 회복 아이템 목록. 예) [[], ["I_03"], ["I_04"], ["I_06"]]
    rune    : 이 층에 놓을 룬 셀 코드. 마지막 구역(계단 앞)에 둔다.
    alcove  : 골방의 쓰임새 (종류, 색, 놓을 셀코드). None 이면 파수꾼 + 물약.
              "key"   파수꾼 뒤에 <b>여분 열쇠</b>를 둔다 — 잡아야 얻는다.
              "vault" 골방 입구가 <b>네 번째 문</b>이 되고 안에 보상이 있다.
    layout_kind : 층 유형(FRAMES). 틀·방 수·방 크기가 갈린다.
    choice  : "둘 중 하나" 보상 (셀코드, 셀코드, 놓는 곳). 하나를 주우면 짝이 사라진다.
              "region" 마지막 구역의 곁길 칸 — 보물 층
              "stairs" 계단 방 — 보스를 잡아야 닿는다(40·60·80층, 예전 목걸이 자리)
              둘 다 길을 끊지 않는 칸에만 놓는다. 밟고 지나가며 저절로 줍게 되면 고른 것이 아니다.

    (grid, 구역별 방 목록, 문 좌표들) 반환. 문 좌표는 큰길 문 셋뿐이다 —
    금고 문은 진행을 강제하지 않으므로 도달 가능성 검사에 넣지 않는다.
    """
    rng = random.Random(seed)
    kind = layout_kind if layout_kind in FRAMES else 0

    if isinstance(mob_ids, int):
        mob_ids = [mob_ids] * mobs_in_floor
    mob_ids = list(mob_ids)[:mobs_in_floor]
    while len(mob_ids) < mobs_in_floor:
        mob_ids.append(mob_ids[-1])

    # 틀과 띠. 열·행 수와 띠 폭이 층마다 다르다.
    cols, rows = rng.choice(FRAMES[kind])
    xbands = _split(INNER_X[0], INNER_X[1], cols, rng)
    ybands = _split(INNER_Y[0], INNER_Y[1], rows, rng)
    order = _lattice_path(cols, rows, PATH_ROOMS[kind], rng)
    if xbands is None or ybands is None or order is None:
        return None, None, None

    _ROOMS.clear()
    _MOUTHS.clear()
    wr, hr = (GAUNTLET_W_RANGE, GAUNTLET_H_RANGE) if kind == 2 else (ROOM_W_RANGE, ROOM_H_RANGE)

    grid = [[VOID] * GRID_W for _ in range(GRID_H)]

    def make_room(key):
        sx = _span(xbands[key[0]], INNER_X, wr, rng)
        sy = _span(ybands[key[1]], INNER_Y, hr, rng)
        if sx is None or sy is None:
            return False
        _ROOMS[key] = _Room((sx[0], sy[0], sx[1], sy[1]))
        for x, y in _ROOMS[key].cells:
            grid[y][x] = FLOOR
        return True

    def connect(a, b):
        link = _link(_ROOMS[a], _ROOMS[b], rng)
        if link is None:
            return None
        cells, ma, mb = link
        if not _free_for(grid, cells, (ma, mb)):
            return None
        for x, y in cells:
            grid[y][x] = FLOOR
        _MOUTHS.update((ma, mb))
        return cells

    for key in order:
        if not make_room(key):
            return None, None, None

    # 큰 줄기. necks[i] 는 i 번째 통로의 칸 전부 — 곁길이 없으니 전부 절단점이다.
    # 마지막 통로(계단 방 입구)를 보스와 룬 자리로 쓴다.
    necks = []
    thresholds = []
    for i in range(len(order) - 1):
        cells = connect(order[i], order[i + 1])
        if cells is None:
            return None, None, None
        necks.append(cells)
        # 문은 통로의 두 끝(문턱) 가운데 하나에만. 한가운데 세우면 방에서 떨어진 통로에
        # 문짝만 덩그러니 선다 — "문이 혼자 떨어져 있다".
        thresholds.append(rng.choice((cells[0], cells[-1])))

    cuts = (2, 3, 4) if len(order) == 6 else (3, 5, 7)
    regions = [list(order[:cuts[0]]), list(order[cuts[0]:cuts[1]]),
               list(order[cuts[1]:cuts[2]]), list(order[cuts[2]:])]

    # 곁방(넉넉 층). 틀에 남은 칸을 큰길 방 하나에만 잇는다 — 막다른 방이라 문을
    # 돌아가는 길이 생기지 않는다. 계단 방에는 잇지 않는다(보스 뒤가 된다).
    want = BRANCH_ROOMS.get(kind, 0)
    if want:
        spare = [(x, y) for y in range(rows) for x in range(cols) if (x, y) not in _ROOMS]
        rng.shuffle(spare)
        attached = 0
        for room in spare:
            if attached == want:
                break
            parents = [(i, parent) for i, region in enumerate(regions) for parent in region
                       if parent != order[-1] and
                       abs(parent[0] - room[0]) + abs(parent[1] - room[1]) == 1]
            if not parents:
                continue
            i, parent = rng.choice(parents)
            if not make_room(room) or connect(parent, room) is None:
                return None, None, None
            regions[i].append(room)
            attached += 1
        if attached < want:
            return None, None, None

    # 방 모양. 통로를 다 판 뒤라야 입구 둘레를 지킬 수 있다.
    for key in sorted(_ROOMS):
        _reshape(grid, _ROOMS[key], rng)

    # 막다른 골방. 입구 한 칸에 파수꾼을 세우고 안쪽에 덤을 둔다.
    #
    # 금고 층은 <b>마지막 구역에서만</b> 판다. 골방 입구가 네 번째 문이 되는데,
    # 그 문이 앞 구역에 있으면 <b>이 층의 열쇠로 열린다</b> — 그러면 큰길 문을 열
    # 열쇠가 사라져 층이 통째로 막힌다. check_vault_safe 가 완성된 격자에서 다시 잰다.
    vault_color = alcove[1] if alcove and alcove[0] == "vault" else None
    # 보스층은 계단 방에 골방을 파지 않는다. 보스가 그 방 입구를 막고 서므로 파수꾼이
    # 보스 <b>뒤</b>가 되는데, 완주 계산은 층의 몹 다섯을 보스 앞에서 잡는다(check_behind_boss).
    alcove_gate, alcove_prize, alcove_base = _carve_alcove(
        grid, regions[3] if vault_color is not None else [r for region in regions for r in region], rng,
        avoid=(order[-1],) if boss_id is not None else ())
    # 골방을 못 판 층은 버린다 — 금고 층만이 아니다. 여분 열쇠 층에서 못 파면 열쇠 없이 통과돼
    # 완주 계산(extras_at)이 세는 여분 열쇠가 사라졌다(코드 리뷰가 찾았다, 출고 층에는 없었다).
    if alcove_gate is None:
        return None, None, None

    # 구역 경계에 문을 세운다 — 구역 k 의 마지막 방과 구역 k+1 의 첫 방을 잇는 통로의 문턱.
    doors = [thresholds[c - 1] for c in cuts]
    if len(set(doors)) != 3:
        return None, None, None

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
            pool += [c for c in room_cells(r)
                     if c not in used and grid[c[1]][c[0]] == FLOOR]
        rng.shuffle(pool)
        return pool

    spawn = room_center(order[0])
    place[spawn] = SPAWN
    used.add(spawn)

    if with_down_stairs:
        pool = _portal_cells(grid, order[0], used)
        rng.shuffle(pool)
        if not pool:
            return None, None, None
        place[pool[0]] = STAIRS_DOWN
        used.add(pool[0])

    # 위 계단도 같은 이유로 길을 끊지 않는 칸에. 보스층에서는 보스가 그 앞에 서므로
    # 계단이 마개가 되면 보스에게 갈 수가 없다.
    up_pool = _portal_cells(grid, order[-1], used)
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
    # 예전에는 "계단에서 가장 가까운 빈 칸" 이었는데, 열린 방에서는 어떤 칸도 길을
    # 끊지 못했다 — 챕터 보스 다섯이 전부 그냥 지나칠 수 있는 상대였다(관문인 보스 0/5).
    before_boss = None
    if boss_id is not None:
        if not last_neck:
            return None, None, None
        cell = last_neck.pop(0)
        place[cell] = f"B_{boss_id:03d}"
        used.add(cell)
        # 보스를 잡기 전에 닿는 칸 — 아래 구역3 물약이 보스 앞/뒤를 가를 때 쓴다.
        before_boss = _flood_locked(
            grid, spawn, {cell} | {c for c, v in place.items() if v in PORTAL_CELLS})

    # 룬도 같은 자리에. 룬은 얻을지 말지가 흔들리면 완주 보장을 계산할 수 없다
    # (기획서 65·81쪽, CLAUDE.md "계단 앞 구역에 둔다"). 예전에는 마지막 구역의 아무
    # 빈 칸이어서 96개 층 중 94개에서 그냥 지나칠 수 있었다.
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
    # 자리마다 "막으면 계단까지 못 가는가" 를 직접 계산해서 가른다. 눈대중으로 세운
    # "통로 목" 의 78% 는 그냥 돌아갈 수 있었다.
    free_gates = [g for neck in necks for g in neck if g not in used and g not in doors]
    toll_spots = [g for g in free_gates if _cuts_path(grid, spawn, up, g)]

    # 관문 자리가 모자라면 층 전체에서 찾는다. 강제 전투가 하나도 없으면 "층의 몹을
    # 다 잡으면 1레벨" 이라는 성장 곡선이 무너지고, 봇은 전부 잡으니 검증에서도 안 걸린다.
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

    # 그래도 관문을 못 만들면 이 층은 버린다 — 억지로 배치하느니 다른 씨앗으로
    # 다시 뽑는 편이 낫다(호출부가 다시 시도한다).
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
    if alcove_gate is not None and alcove_gate not in used:
        if vault_color is None:
            open_spots.insert(0, alcove_gate)
        else:
            used.add(alcove_gate)        # 여기엔 파수꾼이 아니라 문이 선다

    # 곁길 — 피해 갈 수 있는 상대.
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
        for k, pot in enumerate(pots or []):
            pool = free_in(regions[r])
            # 보스층 구역3 의 물약은 [보스 앞, 보스 뒤] 순서다 — 큰 물약은 보스 직전에,
            # 계단 앞 물약은 잡고 나서 마신다(simulate_run 이 그렇게 센다).
            if r == 3 and before_boss is not None:
                pool = [c for c in pool if (c in before_boss) == (k == 0)]
                if not pool:
                    return None, None, None
            if not pool:
                continue
            place[pool[0]] = pot
            used.add(pool[0])

    if rune is not None and rune_cell is None:
        # 계단 방 입구를 못 잡은 층의 폴백. 이 층의 룬은 강제가 아니다 —
        # floor_choices 가 "지나칠 수 있는 아이템" 으로 센다.
        pool = free_in(regions[3])
        if pool:
            place[pool[0]] = rune
            used.add(pool[0])

    # 골방 안쪽의 덤. 완주 계산에는 넣지 않는다 — 안 잡고 지나갈 수 있으니
    # 이걸 셈에 넣으면 보장이 거짓말이 된다. 열쇠 경제도 전부 여기 얹힌다.
    if alcove_prize is not None and alcove_prize not in used:
        place[alcove_prize] = alcove[2] if alcove else POTION_20
        used.add(alcove_prize)

    if choice is not None:
        # 완주 계산 밖의 덤이다. 두 칸 다 곁길이어야 한다.
        first, second, where = choice
        if where == "stairs":
            if before_boss is None:
                return None, None, None
            pool = [c for c in free_in([order[-1]]) if c not in before_boss]
        else:
            pool = free_in(regions[3])
        pool = [c for c in pool if not _cuts_path(grid, spawn, up, c)]
        pairs = [(a, b) for a in pool for b in pool if a < b and
                 abs(a[0] - b[0]) + abs(a[1] - b[1]) == 2]
        if not pairs:
            return None, None, None
        a, b = rng.choice(pairs)
        place[a], place[b] = first + "~", second + "~"
        used.update((a, b))

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

    # 네 번째 문 — 금고. 방향 판정은 큰길 문과 <b>같은 줄</b>을 쓴다. 규칙이 두 벌이 되면 어긋난다.
    if vault_color is not None:
        x, y = alcove_gate
        horizontal_corridor = (grid[y][x - 1] != VOID and grid[y][x + 1] != VOID)
        grid[y][x] = (DOOR_V if horizontal_corridor else DOOR_H)[vault_color]

    _fill_rock(grid, wall_tiles, rng)
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
    portals = PORTAL_CELLS

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


def _flood_locked(grid, start, locked):
    """locked 에 든 문을 벽으로 치고 스폰에서 훑는다. 문 칸 자체는 닿는다.

    validate_layout 과 check_vault_safe 가 같은 훑기를 쓴다 — 두 벌이 되면
    "여기까지는 열쇠 없이 갈 수 있다" 의 뜻이 둘로 갈린다.
    """
    seen, stack = {start}, [start]
    while stack:
        x, y = stack.pop()
        for dx, dy in NEIGHBORS:
            n = (x + dx, y + dy)
            if not _inside(n) or n in seen:
                continue
            cell = grid[n[1]][n[0]]
            if cell == VOID or cell.startswith("W"):
                continue
            seen.add(n)
            if n in locked:
                continue                  # 문 앞까지는 가지만 넘어가진 못한다
            stack.append(n)
    return seen


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
    # 테두리의 열린 칸은 바깥에 벽을 세울 자리가 없다 — 걸어서 맵 밖으로 나간다.
    # 좁은 방 층에서 금고가 맨 아래 줄까지 파여 자동 플레이가 그렇게 빠졌다(7층).
    for y in range(GRID_H):
        for x in range(GRID_W):
            if (x in (0, GRID_W - 1) or y in (0, GRID_H - 1)) and \
                    grid[y][x] != VOID and not grid[y][x].startswith("W"):
                return False, f"테두리에 열린 칸 ({x}, {y})"

    door_set = set(doors)

    opened = set()
    for i in range(3):
        reach = _flood_locked(grid, start, door_set - opened)
        key = KEY_ITEM[i]
        # <b>같은 색 열쇠가 둘일 수 있다</b> — 여분 열쇠를 골방에 두는 층이
        # 있어서다. 마지막에 찾은 하나만 보면 엉뚱한 쪽의 도달 여부를 재게
        # 되므로, 그 색 열쇠 <b>아무거나</b> 하나라도 닿으면 된 것으로 친다.
        spots = [(x, y) for y in range(GRID_H) for x in range(GRID_W)
                 if grid[y][x] == key]
        if not spots:
            return False, f"열쇠{i} 없음"
        if not any(c in reach for c in spots):
            return False, f"열쇠{i} 도달 불가"
        if doors[i] not in reach:
            return False, f"문{i} 도달 불가"
        opened.add(doors[i])

    if stairs not in _flood_locked(grid, start, door_set - opened):
        return False, "계단 도달 불가"
    return True, ""


def vault_doors(grid, doors):
    """큰길 문이 아닌 문 = 금고 문. 격자에서 직접 찾는다."""
    main = set(doors or ())
    return [(x, y) for y in range(len(grid)) for x in range(len(grid[y]))
            if grid[y][x].strip() in DOOR_CELLS and (x, y) not in main]


def check_vault_safe(grid, doors):
    """금고 문이 <b>세 문을 다 연 뒤</b>에야 닿는 자리인가.

    아니면 이 층에서 주운 열쇠를 금고에 써 버리고 큰길 문 앞에서 갇힌다.
    되돌릴 수단이 없는 게임이라 그건 버그지 선택이 아니다.
    어긋난 (좌표, 셀) 목록. 빈 목록이면 정상이다.
    """
    spawn, _ = _endpoints(grid)
    extra = vault_doors(grid, doors)
    if spawn is None or not extra:
        return []
    # 마지막 문 하나만 잠가 둔다 = "세 번째 구역까지는 다 열어 본 상태".
    reach = _flood_locked(grid, spawn, {doors[2]})
    return [(c, grid[c[1]][c[0]]) for c in extra if c in reach]


def check_behind_boss(grid, must_reach):
    """보스를 잡기 <b>전에</b> 써야 하는데 보스 뒤에 있는 것. (좌표, 셀) 목록.

    완주 계산(generate_content.simulate_run)은 층의 몹을 다 잡고, 모자라면 큰 물약을
    마신 뒤에 보스와 싸운다. 그런데 보스는 계단 방 입구를 막고 서서 그 방은 보스를
    잡아야 닿는다 — 40·60·80·100층의 큰 물약이 거기 있었고, 40층은 골방 파수꾼까지
    그랬다. 보스와 포탈을 벽으로 치고(문은 연다) 스폰에서 훑는다.
    must_reach 가 층에 아예 없으면 (None, 셀) 이다 — 안 놓인 것도 같은 사고다.
    보스가 없는 층은 빈 목록.
    """
    cells = {(x, y): v.strip() for y, row in enumerate(grid) for x, v in enumerate(row)}
    spawn = next((c for c, v in cells.items() if v == SPAWN), None)
    boss = {c for c, v in cells.items() if v.startswith("B_")}
    if spawn is None or not boss:
        return []
    seen = _flood_locked(grid, spawn, boss | {c for c, v in cells.items() if v in PORTAL_CELLS})
    bad = [(c, v) for c, v in sorted(cells.items())
           if c not in seen and (v.startswith("M_") or v in must_reach)]
    return bad + [(None, code) for code in must_reach if code not in cells.values()]


def key_economy(grid, doors):
    """격자에서 직접 센다. (색깔별 열쇠, 큰길 문, 금고 문, 파수꾼 뒤의 열쇠).

    마지막 값은 <b>막다른 칸에 놓였고 그 유일한 이웃이 몬스터인</b> 열쇠의 수다.
    여분 열쇠는 골방 안쪽에 두므로 여기 잡힌다 — "곁길을 지나치면 여분이 없다"
    는 말이 참인지 배치를 믿지 않고 격자에서 재기 위한 것이다.
    """
    keys, main, vault, guarded = [0, 0, 0], [0, 0, 0], [0, 0, 0], 0
    dset = set(doors or ())
    for y in range(len(grid)):
        for x in range(len(grid[y])):
            cell = grid[y][x].strip()
            if cell in KEY_ITEM.values():
                keys[[k for k in KEY_ITEM if KEY_ITEM[k] == cell][0]] += 1
                nbr = [(x + dx, y + dy) for dx, dy in NEIGHBORS
                       if _inside((x + dx, y + dy)) and _passable(grid, (x + dx, y + dy))]
                if len(nbr) == 1 and grid[nbr[0][1]][nbr[0][0]].startswith("M_"):
                    guarded += 1
            if cell in DOOR_CELLS:
                (main if (x, y) in dset else vault)[(int(cell) - 3) % 3] += 1
    return keys, main, vault, guarded


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
      vaults / vault_items          금고 문 / 그 안에 갇힌 것

    몬스터는 "막으면 경로가 끊기는가"(validate_layout), 아이템은 "계단까지 가는
    모든 길이 이 칸을 지나는가"(_cuts_path)로 잰다. 아이템은 밟으면 바로 줍기
    때문이다. 열쇠는 연결성이 아니라 문이 강제하므로 따로 센다.

    <b>금고 안은 따로 센다.</b> 거기 있는 룬은 "지나칠 수 있는 룬" 이 아니라
    "값을 치르면 얻는 룬" 이다. 섞어 세면 "룬은 전부 강제" 라는 완주 보장이
    거짓으로 보이고, 반대로 강제 쪽에 넣으면 진짜로 거짓이 된다.
    """
    spawn, up = _endpoints(grid)
    out = dict(forced_mobs=0, optional_mobs=0, boss=0, boss_forced=0,
               forced_items=0, optional_items=0, runes=0, forced_runes=0,
               keys=0, dead_ends=0, vaults=0, vault_items=0, choice_rewards=0)
    if spawn is None or up is None:
        return out

    # 금고 뒤에 갇힌 칸 — 문을 벽으로 놓고 훑으면 남는 것들이다.
    extra = vault_doors(grid, doors)
    out["vaults"] = len(extra)
    inside = set()
    if extra:
        reach = _flood_locked(grid, spawn, set(extra))
        inside = {(x, y) for y in range(len(grid)) for x in range(len(grid[y]))
                  if _passable(grid, (x, y)) and (x, y) not in reach}

    for y in range(len(grid)):
        for x in range(len(grid[y])):
            cell = grid[y][x]
            if cell.endswith("~"):
                out["choice_rewards"] += 1
            if (x, y) in inside:
                # 금고 안. 막다른 자리 셈에는 그대로 들어간다 — 잠겼을 뿐
                # 골방은 여전히 골방이다.
                if cell.startswith(("I_", "E_")):
                    out["vault_items"] += 1
            elif cell.startswith("M_"):
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


# ---------------------------------------------------------------------------
# 눈에 이상한 구조를 <b>센다</b>
#
# 문 규칙(check_doors)과 도달 가능성(validate_layout)은 다 통과하는데 화면이 이상한 층이
# 있었다 — 방에서 떨어진 통로 한가운데 문짝만 서 있고, 96개 층이 한 틀이었다.
# 오너가 직접 보고 찾은 것이다. 이제 생성기([4/7])와 validate_content 가 같이 잰다.


def _open(grid, cell):
    return _inside(cell) and _passable(grid, cell)


def check_structure(grid):
    """눈에 이상한 구조. (사유, x, y) 목록 — 빈 목록이면 정상.

      * 문이 통로 한가운데 — 문을 지나가는 방향의 두 이웃 가운데 어느 쪽도 방 칸(양옆이
        열린 칸)이 아니다. 문은 방 입구(문턱)에 서야 벽 줄에 붙어 보인다.
      * 빈 막다른 칸 — 이웃이 하나뿐인데 아무것도 없다. 어디로도 안 가는 꼬리다.
      * 닿지 않는 바닥 — 문을 다 열고 몬스터를 지나가도(포탈은 못 지나간다) 스폰에서 못 간다.
    """
    issues = []
    spawn = None
    for y in range(len(grid)):
        for x in range(len(grid[y])):
            cell = grid[y][x].strip()
            if cell == SPAWN:
                spawn = (x, y)
            if cell in DOOR_CELLS:
                if int(cell) >= 6:                  # 세로문: 좌우로 지나간다
                    ends = [((x - 1, y), ((x - 1, y - 1), (x - 1, y + 1))),
                            ((x + 1, y), ((x + 1, y - 1), (x + 1, y + 1)))]
                else:
                    ends = [((x, y - 1), ((x - 1, y - 1), (x + 1, y - 1))),
                            ((x, y + 1), ((x - 1, y + 1), (x + 1, y + 1)))]
                if not any(_open(grid, e) and all(_open(grid, c) for c in sides)
                           for e, sides in ends):
                    issues.append(("문이 통로 한가운데 서 있다 — 방 입구가 아니다", x, y))
            elif cell == FLOOR:
                if sum(_open(grid, (x + dx, y + dy)) for dx, dy in NEIGHBORS) == 1:
                    issues.append(("아무것도 없는 막다른 칸", x, y))
    if spawn is not None:
        seen, stack = {spawn}, [spawn]
        while stack:
            c = stack.pop()
            if grid[c[1]][c[0]].strip() in PORTAL_CELLS:
                continue                            # 포탈은 밟을 수는 있어도 넘어갈 수 없다
            for dx, dy in NEIGHBORS:
                n = (c[0] + dx, c[1] + dy)
                if n not in seen and _open(grid, n):
                    seen.add(n)
                    stack.append(n)
        for y in range(len(grid)):
            for x in range(len(grid[y])):
                if _open(grid, (x, y)) and (x, y) not in seen:
                    issues.append(("스폰에서 닿지 않는 바닥", x, y))
    return issues


def _floor_set(grid):
    return frozenset((x, y) for y, row in enumerate(grid) for x, c in enumerate(row)
                     if _open(grid, (x, y)))


def floor_similarity(a, b):
    """두 층의 바닥 칸 Jaccard. 좌우·상하 거울까지 맞춰 본 가장 큰 값.
    격자가 정사각형이 아니라 돌려 보지는 않는다."""
    fa, fb = _floor_set(a), _floor_set(b)
    best = 0.0
    for k in range(4):
        t = frozenset(((GRID_W - 1 - x) if k & 1 else x, (GRID_H - 1 - y) if k & 2 else y)
                      for x, y in fb)
        best = max(best, len(fa & t) / max(1, len(fa | t)))
    return best


# 층끼리 이만큼 닮으면 같은 지도로 보인다. 옛 생성기는 평균 0.64, 최대 1.0(거울상)이었다.
SAME_FLOOR_MAX = 0.6
SAME_FLOOR_MEAN = 0.35


def check_variety(grids):
    """{이름: 격자} 가 서로 충분히 다른가. (평균, [(유사도, 이름, 이름)…] 높은 순)."""
    names = sorted(grids)
    pairs = []
    for i, a in enumerate(names):
        for b in names[i + 1:]:
            pairs.append((floor_similarity(grids[a], grids[b]), a, b))
    pairs.sort(reverse=True)
    mean = sum(p[0] for p in pairs) / max(1, len(pairs))
    return mean, pairs


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
    generated = {}
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
        odd = check_structure(grid)
        gen_bad += len(odd)
        if odd and verbose:
            for why, x, y in odd[:5]:
                print("  [구조] %s (행%d, 열%d) — %s" % (did, y, x, why))
        generated[did] = grid

    mean, pairs = check_variety(generated)
    same = [p for p in pairs if p[0] > SAME_FLOOR_MAX]
    gen_bad += len(same) + (1 if mean > SAME_FLOOR_MEAN else 0)
    if verbose:
        print("  생성 층 %d개 / 문 %d개 — 위반 %d건" % (floors, gen_doors, gen_bad))
        if pairs:
            print("  층끼리 닮음(바닥 Jaccard, 거울 포함) 평균 %.3f (문턱 %.2f) · 최대 %.3f %s-%s · %.2f 넘는 쌍 %d"
                  % (mean, SAME_FLOOR_MEAN, pairs[0][0], pairs[0][1], pairs[0][2],
                     SAME_FLOOR_MAX, len(same)))
        print("  (참고) 손수 만든 층 문 %d개 — 위반 %d건" % (hand_doors, hand_bad))
    return gen_bad


def _shape_self_check(seeds=60):
    """새 생성기가 규칙을 지키는지 씨앗 몇 개로 <b>지금</b> 확인한다.

    check_all_floors 는 디스크의 CSV 를 읽는다 — 생성기를 고쳐도 --write 를
    돌리기 전에는 아무것도 안 보인다. 층 유형 다섯(FRAMES)과 골방의 세 쓰임새,
    보스 유무를 돌려 가며 뽑고, generate_content 가 재는 것을 똑같이 잰다.
    돌려주는 값은 (서로 다른 방 배치 수, 닮음 평균, 닮음 최대).
    """
    art = door_prefab_facts()
    layouts, grids = set(), {}
    for seed in range(seeds):
        # 금고 층은 골방을 마지막 구역에서만 파므로 <b>생성이 더 자주 실패한다</b> —
        # 그 실패율을 여기서 먼저 보게 해 둔다(호출부는 씨앗을 다시 뽑는다).
        alcove = [None, ("key", 2, KEY_ITEM[2]), ("vault", 1, RUNE_DEF)][seed % 3]
        kind = seed % 5
        boss = 9 if seed % 4 == 0 else None
        pots = [[], [POTION_20], [POTION_30], [POTION_50, POTION_20] if boss else [POTION_30]]
        grid = None
        for retry in range(100):
            grid, regions, doors = build_floor_layout(
                [1, 2, 3, 4, 5], boss, ["W_01", "W_02"], seed=seed * 100 + retry,
                rune=RUNE_ATK, alcove=alcove, layout_kind=kind, potions=pots)
            # generate_content 처럼 앞서 뽑은 층을 닮으면 다시 뽑는다.
            if grid is not None and not any(floor_similarity(grid, g) > SAME_FLOOR_MAX
                                            for g in grids.values()):
                break
            grid = None
        assert grid is not None, f"씨앗 {seed} 층 생성 실패 (유형 {kind})"
        ok, why = validate_layout(grid, regions, doors)
        assert ok, f"씨앗 {seed} {why}"
        bad = check_doors(grid, art)
        assert not bad, f"씨앗 {seed} 문 위반 {bad}"
        odd = check_structure(grid)
        assert not odd, f"씨앗 {seed} 구조 {odd[:3]}"
        assert not check_sealed_by_portal(grid), f"씨앗 {seed} 포탈이 막았다"
        unsafe = check_vault_safe(grid, doors)
        assert not unsafe, f"씨앗 {seed} 금고가 앞 구역에 있다 {unsafe}"
        if boss:
            behind = check_behind_boss(grid, (POTION_50,))
            assert not behind, f"씨앗 {seed} 보스 뒤 {behind}"
        keys, main, vault, guarded = key_economy(grid, doors)
        assert main == [1, 1, 1], f"씨앗 {seed} 큰길 문 {main}"
        if alcove is None:
            assert (keys, vault) == ([1, 1, 1], [0, 0, 0]), f"씨앗 {seed} {keys} {vault}"
        elif alcove[0] == "key":
            assert keys == [1, 1, 2], f"씨앗 {seed} 여분 열쇠가 없다 {keys}"
            assert guarded == 1, f"씨앗 {seed} 여분 열쇠가 파수꾼 뒤가 아니다"
        else:
            assert vault == [0, 1, 0], f"씨앗 {seed} 금고 문이 없다 {vault}"
        c = floor_choices(grid, doors)
        if alcove and alcove[0] == "vault":
            assert c["vault_items"] == 1 and c["runes"] == 1, f"씨앗 {seed} {c}"
        assert c["forced_mobs"] >= MIN_TOLLS, f"씨앗 {seed} 관문 {c['forced_mobs']}"
        assert c["forced_runes"] == c["runes"], f"씨앗 {seed} 룬을 지나칠 수 있다"
        if boss:
            assert c["boss_forced"], f"씨앗 {seed} 보스를 지나칠 수 있다"
        layouts.add(tuple(sorted((k, r.rect) for k, r in _ROOMS.items())))
        grids[seed] = grid
    assert len(layouts) == seeds, f"방 배치가 {len(layouts)}가지뿐이다"
    mean, pairs = check_variety(grids)
    assert pairs[0][0] <= SAME_FLOOR_MAX, f"너무 닮은 층 {pairs[:3]}"
    return len(layouts), mean, pairs[0][0]


if __name__ == "__main__":
    # 번역표 자기검사. 이게 깨지면 화면의 문이 90° 돌아간다.
    assert [door_art(c) for c in "345678"] == \
        ["Tilemap_3", "Tilemap_5", "Tilemap_7", "Tilemap_4", "Tilemap_6", "Tilemap_8"]
    print("===== 문 배치·구조 검사 =====")
    print("  갓 뽑은 층 60개 — 방 배치 %d가지, 닮음 평균 %.3f · 최대 %.3f, 문·구조 위반 0건"
          % _shape_self_check())
    sys.exit(1 if check_all_floors() else 0)
