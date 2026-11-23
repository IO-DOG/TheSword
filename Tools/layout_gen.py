"""층 레이아웃의 규약과 검사 — 매직 타워(魔塔) 방식. 층을 짓는 것은 layout_v2.py(지도 v2) 다.

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

  * 방(챕터마다 다른 건축 부품)을 짧은 통로로 잇는다 — 길은 보인다
  * 몬스터는 통로 목에 세운다 — 지나가려면 반드시 값을 치른다
  * 열쇠/물약/장비는 방 안에 둔다 — 들를지 말지가 판단거리다
  * 문 세 개가 층을 네 구역으로 잘라 큰 순서를 정한다
  * 곁길 몬스터를 남긴다 — 지나가는 데는 필요 없고, 덤을 가지려면 잡아야 한다

몬스터는 벽이 아니라 통행료다. 그래서 도달 가능성 검사도 몬스터를 지나갈 수
있는 것으로 보고 한다(validate_layout).
"""

import os
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
# 층을 짓는 것은 layout_v2(지도 v2, 2026-11-23)다. 예전 생성기(3x3 → 틀 굴리기, build_floor_layout)는 지웠다 —
# 그 기록은 git 에 있다(82dfa340 이 11-09 판). 여기에는 v2 와 검사가 같이 쓰는 도우미와 검사만 남는다.

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
            # 갈림길 short 층(layout_v2) — 그 층엔 이 색 열쇠가 없다. 그 문을 잠가도 계단에 닿는(정예 길이 도는) 문일 때만
            # 닫힌 채로 두고 뒤를 잰다. 큰길 문이면 예전처럼 "열쇠 없음" 이다.
            if stairs in _flood_locked(grid, start, {doors[i]}):
                continue
            return False, f"열쇠{i} 없음"
        if not any(c in reach for c in spots):
            return False, f"열쇠{i} 도달 불가"
        if doors[i] not in reach:
            return False, f"문{i} 도달 불가"
        opened.add(doors[i])

    if stairs not in _flood_locked(grid, start, door_set - opened):
        return False, "계단 도달 불가"
    return True, ""


def door_roles(grid):
    """문을 셋으로 가른다 — (큰길, 금고, 갈림길). 큰길은 막으면 계단이 끊기는 문, 금고는 막으면 그 뒤 골방이 끊기는 문,
    갈림길(layout_v2 의 노랑 문)은 막아도 아무 칸도 안 끊기는 문이다 — 정예가 선 둘째 통로가 돈다(몬스터는 지나갈 수 있는 칸)."""
    spawn, up = _endpoints(grid)
    doors = [(x, y) for y in range(len(grid)) for x in range(len(grid[y])) if grid[y][x].strip() in DOOR_CELLS]
    full = len(_flood_locked(grid, spawn, set()))
    main, vault, fork = [], [], []
    for d in doors:
        r = _flood_locked(grid, spawn, {d})
        if up not in r:
            main.append(d)
        elif len(r) < full:              # 잠긴 문 칸 자체는 닿는 것으로 센다(_flood_locked)
            vault.append(d)
        else:
            fork.append(d)
    return main, vault, fork


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
    """지도 v2 생성기(layout_v2)가 규칙을 지키는지 씨앗 몇 개로 <b>지금</b> 확인한다.

    check_all_floors 는 디스크의 CSV 를 읽는다 — 생성기를 고쳐도 --write 를 돌리기 전에는 아무것도 안 보인다.
    챕터 다섯(건축), 골방의 세 쓰임새, 보스 유무, 갈림길(노랑 열쇠가 있는 층·없는 층)을 돌려 가며 뽑고,
    generate_content.emit_layouts 가 재는 것을 똑같이 잰다. 돌려주는 값은 (서로 다른 층 수, 닮음 평균, 닮음 최대).
    """
    import layout_v2                          # layout_v2 가 이 모듈을 부르므로 여기서 부른다
    art = door_prefab_facts()
    grids = {}
    for seed in range(seeds):
        chapter = seed % 5
        alcove = [None, ("key", 2, KEY_ITEM[2]), ("vault", 1, RUNE_DEF)][seed % 3]
        boss = 900 + chapter if seed % 4 == 0 else None
        fork = None if boss else [None, "key", "short"][seed % 3]
        pots = [[], [POTION_20], [POTION_30], [POTION_50, POTION_20] if boss else [POTION_30]]
        mobs = [1000 + seed * 8 + k for k in range(5)]
        elite_id = 1000 + seed * 8 + 5 if fork else None
        grid = None
        for retry in range(300):
            r, _ = layout_v2.build_floor_v2(chapter, seed * 1000 + retry, mobs, boss, ["W_01", "W_02"], pots, RUNE_ATK,
                                            alcove, None, None, elite_id=elite_id, short=fork == "short")
            if r is None:
                continue
            g, regions, doors, ecell = r
            elite_code = f"M_{elite_id:03d}" if elite_id else None
            if not validate_layout(g, regions, doors)[0] or check_structure(g) or layout_v2.check_fork(g, doors, ecell)                     or check_doors(g, art) or check_sealed_by_portal(g) or check_vault_safe(g, doors)                     or (boss and check_behind_boss(g, (POTION_50,))) or len(layout_v2.dead_end_items(g, elite_code)) != 1:
                continue
            c = floor_choices(g, doors)
            if c["forced_mobs"] < MIN_TOLLS or c["forced_runes"] < c["runes"] or (boss and not c["boss_forced"]):
                continue
            if any(floor_similarity(g, other) > SAME_FLOOR_MAX for other in grids.values()):
                continue
            grid = g
            break
        assert grid is not None, f"씨앗 {seed} 층 생성 실패 (챕터 {chapter}, 골방 {alcove and alcove[0]}, 갈림길 {fork})"
        keys, main, vault, guarded = key_economy(grid, doors)
        assert main == [1, 1, 1], f"씨앗 {seed} 큰길 문 {main}"
        want = [1, 0 if fork == "short" else 1, 1]
        if alcove is None:
            assert (keys, vault) == (want, [0, 0, 0]), f"씨앗 {seed} {keys} {vault}"
        elif alcove[0] == "key":
            assert keys == [want[0], want[1], want[2] + 1], f"씨앗 {seed} 여분 열쇠가 없다 {keys}"
            assert guarded == 1, f"씨앗 {seed} 여분 열쇠가 파수꾼 뒤가 아니다"
        else:
            assert vault == [0, 1, 0], f"씨앗 {seed} 금고 문이 없다 {vault}"
        _, vaults, forks = door_roles(grid)
        assert len(forks) == (1 if fork else 0), f"씨앗 {seed} 갈림길 문 {forks}"
        grids[seed] = grid
    distinct = len({tuple(sorted(_floor_set(g))) for g in grids.values()})
    assert distinct == seeds, f"서로 다른 층이 {distinct}개뿐이다"
    mean, pairs = check_variety(grids)
    assert pairs[0][0] <= SAME_FLOOR_MAX, f"너무 닮은 층 {pairs[:3]}"
    return distinct, mean, pairs[0][0]


if __name__ == "__main__":
    # 번역표 자기검사. 이게 깨지면 화면의 문이 90° 돌아간다.
    assert [door_art(c) for c in "345678"] == \
        ["Tilemap_3", "Tilemap_5", "Tilemap_7", "Tilemap_4", "Tilemap_6", "Tilemap_8"]
    print("===== 문 배치·구조 검사 =====")
    print("  갓 뽑은 v2 층 60개 — 서로 다른 층 %d, 닮음 평균 %.3f · 최대 %.3f, 문·구조·갈림길 위반 0건"
          % _shape_self_check())
    sys.exit(1 if check_all_floors() else 0)
