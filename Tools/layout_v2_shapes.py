"""지도 v2 — 층의 모양(2026-11-23 오너 결정 "추천대로": 챕터마다 다른 건축 + 갈림길).

격자 27 x 23(layout_gen.GRID_W/H, 바깥 한 줄은 테두리). 0 = 바위, 1 = 바닥.
챕터마다 부품(방 모양)이 다르고(PALETTE), 큰 줄기 4~6개를 하나씩 놓아 앞 부품에 잇는다 — 붙이거나(벽 한 줄 + 문간) 통로로.
얼마간은 챕터의 대표 평면(SIGNATURE — 대칭 성·물길·굴 …)을 통째로 쓰고, 상하좌우로 뒤집는다.
모양을 먼저 짓고, 스폰 -> 계단 최단 길 위의 단절점 가운데 문을 세울 수 있는 칸(양옆 벽, 앞뒤 바닥, 문턱)으로 문 셋을 고른다(zone).
고리는 단절점이 아니므로 문을 돌아가는 길이 저절로 생기지 않는다. 배치는 layout_v2.py.
원본 시험판: 스크래치 fork/shapes.py·shapes2.py(11-23 10~16시 회차, `.omc/plans/fork-proto.md`).
"""
import random
from collections import deque

W, H = 27, 23
X0, X1, Y0, Y1 = 1, W - 2, 1, H - 2


def blank():
    return [[0] * W for _ in range(H)]


def inside(x, y):
    return X0 <= x <= X1 and Y0 <= y <= Y1


def rect(g, x0, y0, x1, y1, v=1):
    for y in range(max(Y0, y0), min(Y1, y1) + 1):
        for x in range(max(X0, x0), min(X1, x1) + 1):
            g[y][x] = v


def line(g, a, b, rng, v=1):
    """한 번 꺾이는 1칸 통로."""
    (x0, y0), (x1, y1) = a, b
    if rng.random() < 0.5:
        rect(g, min(x0, x1), y0, max(x0, x1), y0, v); rect(g, x1, min(y0, y1), x1, max(y0, y1), v)
    else:
        rect(g, x0, min(y0, y1), x0, max(y0, y1), v); rect(g, min(x0, x1), y1, max(x0, x1), y1, v)


def octagon(g, cx, cy, r, v=1):
    for y in range(cy - r, cy + r + 1):
        for x in range(cx - r, cx + r + 1):
            if abs(x - cx) + abs(y - cy) <= r + r // 2 and inside(x, y):
                g[y][x] = v


def floors(g):
    return [(x, y) for y in range(H) for x in range(W) if g[y][x] == 1]


def nbrs(x, y):
    return ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1))


def bfs(g, s, block=()):
    dist = {s: 0}; prev = {s: None}; q = deque([s]); block = set(block)
    while q:
        c = q.popleft()
        for n in nbrs(*c):
            if n not in dist and n not in block and inside(*n) and g[n[1]][n[0]] == 1:
                dist[n] = dist[c] + 1; prev[n] = c; q.append(n)
    return dist, prev


def keep_largest(g):
    seen, best = set(), []
    for c in floors(g):
        if c in seen:
            continue
        d, _ = bfs(g, c); seen |= set(d)
        if len(d) > len(best):
            best = list(d)
    keep = set(best)
    for x, y in floors(g):
        if (x, y) not in keep:
            g[y][x] = 0


def doorable(g, c):
    x, y = c
    f = lambda a, b: inside(a, b) and g[b][a] == 1
    return (f(x - 1, y) and f(x + 1, y) and not f(x, y - 1) and not f(x, y + 1)) or \
           (f(x, y - 1) and f(x, y + 1) and not f(x - 1, y) and not f(x + 1, y))


# ---------------------------------------------------------------- 건축 다섯

def catacombs(rng):
    """묘소 — 구불구불한 좁은 회랑, 벽마다 한 칸짜리 매장 감실, 십자형 납골실."""
    g = blank()
    x, y = X0 + 1, rng.randint(Y0 + 2, Y1 - 2)
    pts = [(x, y)]
    while x < X1 - 2:
        nx = min(X1 - 1, x + rng.randint(3, 6)); ny = max(Y0 + 1, min(Y1 - 1, y + rng.randint(-7, 7)))
        line(g, (x, y), (nx, ny), rng); x, y = nx, ny; pts.append((x, y))
    for (cx, cy) in rng.sample(pts[1:-1], min(3, len(pts) - 2)):          # 십자 납골실
        a = rng.randint(1, 2)
        rect(g, cx - 3, cy - a, cx + 3, cy + a); rect(g, cx - a, cy - 3, cx + a, cy + 3)
    for _ in range(4):                                                    # 곁 회랑(막다른)
        sx, sy = rng.choice(floors(g))
        tx, ty = max(X0 + 1, min(X1 - 1, sx + rng.randint(-6, 6))), max(Y0 + 1, min(Y1 - 1, sy + rng.randint(-6, 6)))
        line(g, (sx, sy), (tx, ty), rng)
    for c in rng.sample(floors(g), len(floors(g)) // 3):                  # 감실
        x, y = c
        for nx, ny in nbrs(x, y):
            if inside(nx, ny) and g[ny][nx] == 0 and rng.random() < 0.12 and \
                    sum(1 for m in nbrs(nx, ny) if inside(*m) and g[m[1]][m[0]] == 1) == 1:
                g[ny][nx] = 1
    return g, (pts[0], pts[-1])


def aqueduct(rng):
    """수로 — 가로로 긴 물길 홀 셋이 뱀처럼 접히고, 물길 가운데 기둥(다리 사이), 끝에 기둥 선 저수조."""
    g = blank()
    n = 4
    while True:
        rows = sorted(rng.sample(range(Y0, Y1 - 1), n))
        if all(b - a >= 5 for a, b in zip(rows, rows[1:])):
            break
    left = rng.random() < 0.5
    ends = []
    for i, ry in enumerate(rows):
        h = rng.choice((2, 3)); x0, x1 = X0 + rng.randint(1, 4), X1 - rng.randint(1, 4)
        rect(g, x0, ry, x1, ry + h - 1)
        for px in range(x0 + 2, x1 - 1, rng.choice((3, 4))):              # 물길의 기둥
            if h == 3:
                g[ry + 1][px] = 0
        ends.append((x0, x1, ry, h))
    for i in range(n - 1):                                                # 번갈아 끝에서 잇는 수직 수로
        x0, x1, ry, h = ends[i]; _, _, ry2, _ = ends[i + 1]
        sx = x0 if (i % 2 == 0) == left else x1
        rect(g, sx, ry + h, sx, ry2 - 1)
    for i in (0, n - 1):                                                  # 저수조 — 긴 수문 통로 끝의 기둥 선 방
        x0, x1, ry, h = ends[i]
        side = x1 if (i == 0) == left else x0
        cx = max(X0 + 3, min(X1 - 3, side + (3 if side == x1 else -3)))
        cy = max(Y0 + 2, ry - 2) if i == 0 else min(Y1 - 2, ry + h + 1)
        cx = max(X0 + 2, min(X1 - 2, cx)); cy = max(Y0 + 2, min(Y1 - 2, cy))
        rect(g, cx - 2, cy - 2, cx + 2, cy + 2)
        g[cy - 1][cx - 1] = g[cy - 1][cx + 1] = g[cy + 1][cx - 1] = g[cy + 1][cx + 1] = 0
    keep_largest(g)
    fl = floors(g)
    return g, (min(fl, key=lambda c: c[1] * 100 + c[0]), max(fl, key=lambda c: c[1] * 100 - c[0]))


def forge(rng):
    """용광로 — 벽을 맞댄 큰 작업장들(BSP). 방 사이는 문간 한 칸, 방 안에 쇠틀(막힌 덩어리)."""
    g = blank()
    leaves = []

    def split(x0, y0, x1, y1, depth):
        w, h = x1 - x0 + 1, y1 - y0 + 1
        if depth == 0 or (w < 11 and h < 9):
            leaves.append((x0, y0, x1, y1)); return
        if w >= h and w >= 11:
            s = rng.randint(x0 + 4, x1 - 4); split(x0, y0, s - 1, y1, depth - 1); split(s + 1, y0, x1, y1, depth - 1)
        elif h >= 9:
            s = rng.randint(y0 + 3, y1 - 3); split(x0, y0, x1, s - 1, depth - 1); split(x0, s + 1, x1, y1, depth - 1)
        else:
            leaves.append((x0, y0, x1, y1))
    split(X0, Y0, X1, Y1, 4)
    rooms = []
    for (x0, y0, x1, y1) in leaves:
        if rng.random() < 0.15 and len(leaves) > 6:
            continue                                                      # 무너진 칸(바위)
        rect(g, x0, y0, x1, y1); rooms.append((x0, y0, x1, y1))
        if x1 - x0 >= 6 and y1 - y0 >= 4 and rng.random() < 0.7:           # 쇠틀
            bx, by = rng.randint(x0 + 2, x1 - 3), rng.randint(y0 + 2, y1 - 2)
            rect(g, bx, by, bx + rng.randint(0, 1), by, 0)
    # 이웃 방 사이 벽에 문간 — 신장 트리로만(고리 없음), 가끔 한 군데 더(구역 안 고리).
    import itertools
    def touch(a, b):
        ax0, ay0, ax1, ay1 = a; bx0, by0, bx1, by1 = b
        if ax1 + 2 == bx0 or bx1 + 2 == ax0:
            lo, hi = max(ay0, by0), min(ay1, by1)
            if hi - lo >= 0:
                x = ax1 + 1 if ax1 + 2 == bx0 else bx1 + 1
                return [(x, y) for y in range(lo, hi + 1)]
        if ay1 + 2 == by0 or by1 + 2 == ay0:
            lo, hi = max(ax0, bx0), min(ax1, bx1)
            if hi - lo >= 0:
                y = ay1 + 1 if ay1 + 2 == by0 else by1 + 1
                return [(x, y) for x in range(lo, hi + 1)]
        return None
    edges = [(a, b, touch(rooms[a], rooms[b])) for a, b in itertools.combinations(range(len(rooms)), 2)]
    edges = [e for e in edges if e[2]]
    rng.shuffle(edges)
    parent = list(range(len(rooms)))
    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]; i = parent[i]
        return i
    for a, b, cells in edges:
        if find(a) != find(b):
            parent[find(a)] = find(b)
            x, y = rng.choice(cells[1:-1] or cells); g[y][x] = 1
    keep_largest(g)
    fl = floors(g)
    return g, (min(fl, key=lambda c: c[0] + c[1]), max(fl, key=lambda c: c[0] + c[1]))


def caverns(rng):
    """심층 — 얼음 동굴. 둥근 굴 여럿을 구불구불한 좁은 굴로 잇는다(세포 자동자)."""
    g = blank()
    cx, cy = X0 + 3, rng.randint(Y0 + 4, Y1 - 4)
    centers = [(cx, cy)]
    for _ in range(rng.randint(4, 6)):
        cx = min(X1 - 3, cx + rng.randint(3, 6)); cy = max(Y0 + 3, min(Y1 - 3, cy + rng.randint(-8, 8)))
        centers.append((cx, cy))
    for (cx, cy) in centers:
        r = rng.randint(2, 4)
        for y in range(cy - r - 1, cy + r + 2):
            for x in range(cx - r - 1, cx + r + 2):
                if inside(x, y) and (x - cx) ** 2 + (y - cy) ** 2 <= r * r + rng.randint(-2, 2):
                    g[y][x] = 1
    for _ in range(2):                                                    # 다듬기
        n = [[sum(1 for dy in (-1, 0, 1) for dx in (-1, 0, 1) if inside(x + dx, y + dy) and g[y + dy][x + dx])
              for x in range(W)] for y in range(H)]
        for y in range(Y0, Y1 + 1):
            for x in range(X0, X1 + 1):
                if g[y][x] and n[y][x] <= 3: g[y][x] = 0
                elif not g[y][x] and n[y][x] >= 6: g[y][x] = 1
    for a, b in zip(centers, centers[1:]):                                 # 구불구불한 굴
        x, y = a
        while (x, y) != b:
            if rng.random() < 0.65:
                x += (b[0] > x) - (b[0] < x) if x != b[0] else 0
                if x == b[0]: y += (b[1] > y) - (b[1] < y)
            else:
                y += (b[1] > y) - (b[1] < y) if y != b[1] else 0
            if inside(x, y): g[y][x] = 1
    keep_largest(g)
    return g, (centers[0], centers[-1])


def keep(rng):
    """왕좌 — 좌우 대칭의 성. 바깥뜰 -> 성문 -> 안뜰(가운데 우물을 도는 회랑) -> 대전(기둥 줄, 양옆 탑) -> 왕좌(양옆 예배실)."""
    g = blank()
    mx = W // 2
    y = Y1
    def sym_rect(hw, y0, y1, v=1):
        rect(g, mx - hw, y0, mx + hw, y1, v)
    bw = rng.randint(5, 9); sym_rect(bw, y - 1, y); y -= 2                  # 바깥뜰
    for side in (-1, 1):                                                   # 바깥뜰 끝의 망루
        if rng.random() < 0.6:
            octagon(g, mx + side * bw, y - 1, 1)
    sym_rect(0, y - 1, y); y -= 2                                          # 성문
    ch = 5; cw = rng.randint(3, 6)
    sym_rect(cw, y - ch + 1, y); sym_rect(cw - 2, y - ch + 3, y - 2, 0); y -= ch  # 안뜰과 우물
    sym_rect(0, y - 1, y); y -= 2                                          # 안뜰 -> 대전
    hh = 4; hw = rng.randint(5, 9)
    sym_rect(hw, y - hh + 1, y)
    for px in range(2, hw, rng.choice((2, 3))):                            # 기둥 줄
        g[y - 1][mx - px] = g[y - 1][mx + px] = 0
        g[y - hh + 2][mx - px] = g[y - hh + 2][mx + px] = 0
    tr = 1
    for side in (-1, 1):                                                   # 양옆 탑(막다른 둥근 방)
        tx = mx + side * (hw + 3)
        if X0 + 2 <= tx <= X1 - 2 and rng.random() < 0.8:
            octagon(g, tx, y - 1, tr); rect(g, min(mx + side * hw, tx), y - 1, max(mx + side * hw, tx), y - 1)
    y -= hh
    sym_rect(0, y - 1, y); y -= 2                                          # 대전 -> 왕좌
    rh = max(2, min(y - Y0 + 1, 4)); rw = rng.randint(2, 4)
    sym_rect(rw, y - rh + 1, y)
    for side in (-1, 1):                                                   # 왕좌 양옆 예배실
        if rng.random() < 0.7:
            cx = mx + side * (rw + 4); rect(g, min(mx + side * rw, cx), y, max(mx + side * rw, cx), y)
            rect(g, cx - 1, y - 2, cx + 1, y)
    keep_largest(g)
    return g, ((mx, Y1), (mx, max(Y0, y - rh + 1)))


STYLES = {"묘소": catacombs, "수로": aqueduct, "용광로": forge, "심층": caverns, "왕좌": keep}


# ---------------------------------------------------------------- 길목 찾기 -> 문 셋·관문

def articulation(g, cells):
    """격자 그래프의 단절점(반복 Tarjan)."""
    idx = {c: i for i, c in enumerate(cells)}
    disc, low, ap = {}, {}, set()
    t = 0
    for root in cells:
        if root in disc:
            continue
        stack = [(root, None, iter([n for n in nbrs(*root) if n in idx]))]
        disc[root] = low[root] = t; t += 1; kids = 0
        while stack:
            c, p, it = stack[-1]
            nxt = next(it, None)
            if nxt is None:
                stack.pop()
                if stack:
                    pc = stack[-1][0]
                    low[pc] = min(low[pc], low[c])
                    if stack[-1][1] is not None and low[c] >= disc[pc]:
                        ap.add(pc)
                continue
            if nxt == p:
                continue
            if nxt in disc:
                low[c] = min(low[c], disc[nxt])
            else:
                disc[nxt] = low[nxt] = t; t += 1
                if c == root: kids += 1
                stack.append((nxt, c, iter([n for n in nbrs(*nxt) if n in idx])))
        if kids > 1:
            ap.add(root)
    return ap


def zone(g, ends, rng):
    """문 셋과 관문 셋을 고른다. 못 고르면 None."""
    fl = floors(g)
    s = min(fl, key=lambda c: abs(c[0] - ends[0][0]) + abs(c[1] - ends[0][1]))
    dist, prev = bfs(g, s)
    u = max(dist, key=dist.get)
    path = []
    c = u
    while c is not None:
        path.append(c); c = prev[c]
    path.reverse()
    ap = articulation(g, fl)
    cuts = [c for c in path[2:-2] if c in ap]
    def threshold(c):
        """layout_gen.check_structure 와 같은 정의 — 지나가는 방향의 이웃 하나가 그 방향과 수직인 양옆이 열린 칸."""
        x, y = c
        f = lambda a, b: inside(a, b) and g[b][a] == 1
        if f(x - 1, y) and f(x + 1, y):            # 좌우로 지나간다
            return any(f(ex, y) and f(ex, y - 1) and f(ex, y + 1) for ex in (x - 1, x + 1))
        return any(f(x, ey) and f(x - 1, ey) and f(x + 1, ey) for ey in (y - 1, y + 1))
    door_ok = [c for c in cuts if doorable(g, c) and threshold(c)]
    total = len(fl)
    best = None
    for _ in range(300):
        if len(door_ok) < 3:
            break
        ds = sorted(rng.sample(door_ok, 3), key=path.index)
        sizes = []
        reach_prev = set()
        for k in range(4):
            block = set(ds[k:]) if k < 3 else set()
            r = set(bfs(g, s, block)[0]) - set(ds)
            sizes.append(len(r - reach_prev)); reach_prev = r
        if min(sizes) < total * 0.08:
            continue
        tolls = [c for c in cuts if c not in ds]
        score = min(sizes)
        if len(tolls) >= 4 and (best is None or score > best[0]):
            best = (score, ds, tolls, s, u, sizes)
    return best


AREA = (140, 320)          # 바닥 칸 수 범위 — 지금 지도(층당 ~200)와 같은 품
PULL = 10                  # 얼음굴 비틀걸음이 목표 쪽으로 기우는 무게. 3 이면 통로에 이빨(홈)이 층당 8.1, 10 이면 6.7(11-23 16시)



# ---------------------------------------------------------------- 부품

def m_rect(rng, w=None, h=None):
    w = w or rng.randint(4, 8); h = h or rng.randint(3, 6)
    return {(x, y) for x in range(w) for y in range(h)}


def m_pillared(rng):
    w, h = rng.randint(6, 10), rng.choice((3, 5))
    c = m_rect(rng, w, h); step = rng.choice((2, 3))
    for x in range(1, w - 1, step):
        for y in range(1, h - 1, 2):
            c.discard((x, y))
    return c


def m_cross(rng):
    a, r = rng.randint(1, 2), rng.randint(3, 4)
    return {(x, y) for x in range(2 * r + 1) for y in range(2 * r + 1)
            if abs(x - r) <= a or abs(y - r) <= a}


def m_octagon(rng):
    r = rng.randint(2, 3)
    return {(x, y) for x in range(2 * r + 1) for y in range(2 * r + 1)
            if abs(x - r) + abs(y - r) <= r + r // 2}


def m_cloister(rng):
    """안뜰 회랑 — 가운데 막힌 덩어리(우물·정원)를 한 바퀴 도는 방."""
    w, h = rng.randint(7, 10), rng.randint(5, 7)
    c = m_rect(rng, w, h); t = rng.choice((1, 2)) if h >= 6 else 1
    for x in range(t, w - t):
        for y in range(t, h - t):
            c.discard((x, y))
    return c


def m_gallery(rng):
    """긴 회랑 — 벽에 한 칸 감실이 박혀 있다."""
    w = rng.randint(7, 12); c = {(x, 1) for x in range(w)}
    for x in range(1, w - 1, rng.choice((2, 3))):
        if rng.random() < 0.6: c.add((x, 0))
        if rng.random() < 0.6: c.add((x, 2))
    return c


def m_channel(rng):
    """물길 — 가운데 줄에 기둥(다리 사이)이 선 긴 홀."""
    w = rng.randint(8, 13); c = {(x, y) for x in range(w) for y in range(3)}
    for x in range(2, w - 1, rng.choice((3, 4))):
        c.discard((x, 1))
    return c


def m_forge(rng):
    """작업장 — 큰 방 안의 쇠틀(막힌 덩어리) 한두 개."""
    w, h = rng.randint(6, 9), rng.randint(4, 6); c = m_rect(rng, w, h)
    for _ in range(rng.randint(1, 2)):
        bx, by = rng.randint(1, w - 3), rng.randint(1, h - 2)
        for x in range(bx, bx + rng.randint(1, 2)):
            c.discard((x, by))
    return c


def m_blob(rng):
    """얼음굴 — 둥글고 울퉁불퉁한 굴."""
    r = rng.choice((2, 2, 3, 3, 4)); c = set()
    for x in range(-r - 1, r + 2):
        for y in range(-r - 1, r + 2):
            if x * x + y * y <= r * r + rng.randint(-2, 2):
                c.add((x + r + 1, y + r + 1))
    return c


def m_apse(rng):
    """반원 끝방(예배실·왕좌) — 네모 방 한쪽 끝이 둥글다."""
    w, h = rng.randint(5, 7), rng.randint(3, 4); c = m_rect(rng, w, h)
    r = w // 2
    for x in range(w):
        for y in range(h, h + r):
            if (x - r) ** 2 + (y - h + 1) ** 2 <= r * r:
                c.add((x, y))
    return c


def m_lshape(rng):
    w, h = rng.randint(5, 8), rng.randint(4, 6); c = m_rect(rng, w, h)
    cx, cy = rng.randint(2, w - 2), rng.randint(2, h - 2)
    for x in range(cx, w):
        for y in range(cy, h):
            c.discard((x, y))
    return c


PALETTE = {
    "묘소": [m_cross, m_cross, m_gallery, m_gallery, m_octagon, m_rect],
    "수로": [m_channel, m_channel, m_pillared, m_rect, m_lshape],
    "용광로": [m_forge, m_forge, m_octagon, m_lshape, m_pillared],
    "심층": [m_blob, m_blob, m_blob, m_lshape],
    "왕좌": [m_cloister, m_pillared, m_octagon, m_apse, m_cross],
}
WINDING = {"묘소": 2, "수로": 1, "용광로": 1, "심층": 2, "왕좌": 1}
MAIN = {"묘소": (4, 6), "수로": (4, 6), "용광로": (4, 6), "심층": (4, 6), "왕좌": (4, 5)}
GAP = {"묘소": 2, "수로": 2, "용광로": 1, "심층": 1, "왕좌": 1}
ATTACH = {"묘소": 0.0, "수로": 0.2, "용광로": 0.8, "심층": 0.0, "왕좌": 0.75}


# ---------------------------------------------------------------- 놓기·잇기

def _edge(cells):
    return [c for c in cells if any(n not in cells for n in nbrs(*c))]


def _drunk_path(g, a, b, rng, own):
    """얼음굴 — 목표 쪽으로 기울어 비틀거리며 판다(곧은 줄이 아니다)."""
    x, y = a; cells = []
    for _ in range(400):
        if (x, y) == b:
            break
        dx, dy = (b[0] > x) - (b[0] < x), (b[1] > y) - (b[1] < y)
        opts = []
        if dx: opts += [(x + dx, y)] * PULL
        if dy: opts += [(x, y + dy)] * PULL
        opts += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
        x, y = rng.choice(opts)
        if (x, y) not in cells and (x, y) not in own:
            cells.append((x, y))
    if (x, y) != b:
        return None
    for c in cells:
        if not inside(*c):
            return None
        for n in nbrs(*c):
            if inside(*n) and g[n[1]][n[0]] == 1 and n not in own and n not in cells:
                return None
    return cells


def _carve_path(g, a, b, rng, bends, own):
    """a -> b 를 bends 번 꺾어 판다. 지나는 칸이 own(두 부품) 밖의 바닥에 닿으면 실패."""
    pts = [a]
    for _ in range(bends - 1):
        pts.append((rng.randint(min(a[0], b[0]), max(a[0], b[0])), rng.randint(min(a[1], b[1]), max(a[1], b[1]))))
    pts.append(b)
    cells = []
    for p, q in zip(pts, pts[1:]):
        x, y = p
        horiz_first = rng.random() < 0.5
        while (x, y) != q:
            if (horiz_first and x != q[0]) or y == q[1]:
                x += 1 if q[0] > x else -1
            else:
                y += 1 if q[1] > y else -1
            cells.append((x, y))
    new = [c for c in cells if c not in own]
    for c in new:
        if not inside(*c):
            return None
        for n in nbrs(*c):
            if inside(*n) and g[n[1]][n[0]] == 1 and n not in own and n not in new:
                return None
    return new


def compose(style, rng):
    g = blank()
    placed = []          # 부품마다 칸 집합(절대 좌표)
    def place(shape):
        cells = shape(rng); w = max(x for x, _ in cells) + 1; h = max(y for _, y in cells) + 1
        for _ in range(80):
            ox, oy = rng.randint(X0, X1 - w + 1), rng.randint(Y0, Y1 - h + 1)
            absc = {(x + ox, y + oy) for x, y in cells}
            if all(inside(*c) for c in absc) and \
                    not any(g[ny][nx] for (x, y) in absc for nx in range(x - GAP[style], x + GAP[style] + 1) for ny in range(y - GAP[style], y + GAP[style] + 1)
                            if inside(nx, ny)):
                return absc
        return None
    def attach(prev, shape):
        """앞 부품 옆에 벽 한 줄을 두고 붙이고, 그 벽에 문간 한 칸을 뚫는다(성 안처럼 방이 벽을 맞댄다)."""
        cells = shape(rng); w = max(x for x, _ in cells) + 1; h = max(y for _, y in cells) + 1
        px0 = min(x for x, _ in prev); px1 = max(x for x, _ in prev); py0 = min(y for _, y in prev); py1 = max(y for _, y in prev)
        for _ in range(60):
            side = rng.choice("LRUD")
            if side == "R": ox, oy = px1 + 2, rng.randint(py0 - h + 1, py1)
            elif side == "L": ox, oy = px0 - 1 - w, rng.randint(py0 - h + 1, py1)
            elif side == "D": ox, oy = rng.randint(px0 - w + 1, px1), py1 + 2
            else: ox, oy = rng.randint(px0 - w + 1, px1), py0 - 1 - h
            absc = {(x + ox, y + oy) for x, y in cells}
            if not all(inside(*c) for c in absc):
                continue
            if any(inside(nx, ny) and g[ny][nx] for (x, y) in absc for nx in range(x - 1, x + 2) for ny in range(y - 1, y + 2)):
                continue
            gaps = []
            for (x, y) in absc:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    m, f = (x + dx, y + dy), (x + 2 * dx, y + 2 * dy)
                    if f in prev and g[m[1]][m[0]] == 0:
                        gaps.append(m)
            if not gaps:
                continue
            for x, y in absc: g[y][x] = 1
            x, y = rng.choice(gaps); g[y][x] = 1
            return absc
        return None
    def link(a, b):
        ea, eb = _edge(a), _edge(b)
        for _ in range(40):
            p = min(rng.sample(ea, min(6, len(ea))), key=lambda c: min(abs(c[0] - d[0]) + abs(c[1] - d[1]) for d in eb[:40]))
            q = min(eb, key=lambda c: abs(c[0] - p[0]) + abs(c[1] - p[1]))
            path = _drunk_path(g, p, q, rng, a | b) if style == "심층" else _carve_path(g, p, q, rng, rng.choice((1, WINDING[style])), a | b)
            if path is not None:
                for x, y in path: g[y][x] = 1
                return True
        return False
    main = rng.randint(*MAIN[style])
    pal = PALETTE[style]
    for i in range(main):
        if placed and rng.random() < ATTACH[style]:
            absc = attach(placed[-1], rng.choice(pal))
            if absc is not None:
                placed.append(absc); continue
        absc = place(rng.choice(pal))
        if absc is None:
            return None, None
        for x, y in absc: g[y][x] = 1
        if placed:
            # 가까운 부품부터 잇는다(먼 것과 이으려면 통로가 다른 부품을 스치기 쉽다)
            near = sorted(placed, key=lambda p: min(abs(a[0] - b[0]) + abs(a[1] - b[1]) for a in list(p)[:40] for b in list(absc)[:40]))
            if not any(link(p, absc) for p in near[:3]):
                return None, None
        placed.append(absc)
    for _ in range(rng.randint(1, 3)):                                     # 곁 부품
        if rng.random() < ATTACH[style]:
            if attach(rng.choice(placed[:-1]), rng.choice(pal)) is not None:
                continue
        absc = place(rng.choice(pal))
        if absc is None:
            continue
        for x, y in absc: g[y][x] = 1
        if not link(rng.choice(placed[:-1]), absc):
            for x, y in absc: g[y][x] = 0
    # 통로가 방에 닿는 칸 e 의 양옆(통로 방향과 수직)을 연다 — 그 통로 칸이 문턱이 된다(check_structure 의 정의).
    rooms = set().union(*placed)
    for y in range(Y0, Y1 + 1):
        for x in range(X0, X1 + 1):
            if not g[y][x] or (x, y) in rooms:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                e = (x + dx, y + dy)
                if e not in rooms:
                    continue
                if any(g[y + sy][x + sx] for sx, sy in ((dy, dx), (-dy, -dx))):
                    continue        # 이 통로 칸은 양옆이 벽이 아니다 — 문 자리가 아니다
                for sx, sy in ((dy, dx), (-dy, -dx)):
                    c = (e[0] + sx, e[1] + sy)
                    if inside(*c) and not g[c[1]][c[0]]:
                        g[c[1]][c[0]] = 1; rooms.add(c)
    keep_largest(g)
    first, last = sorted(placed[0])[0], sorted(placed[-1])[0]
    return g, (first, last)


# ---------------------------------------------------------------- 대표 평면(1차) + 뒤집기

SIGNATURE = {"수로": aqueduct, "왕좌": keep, "묘소": catacombs, "심층": caverns, "용광로": forge}
SIG_SHARE = {"수로": 0.2, "왕좌": 0.25, "묘소": 0.15, "심층": 0.15, "용광로": 0.3}


def flip(g, ends, fx, fy):
    if fx:
        g = [list(reversed(r)) for r in g]; ends = tuple((W - 1 - x, y) for x, y in ends)
    if fy:
        g = list(reversed(g)); ends = tuple((x, H - 1 - y) for x, y in ends)
    return g, ends


def make(style, seed, tries=80):
    for k in range(tries):
        rng = random.Random(seed * 7919 + k)
        if rng.random() < SIG_SHARE[style]:
            g, ends = SIGNATURE[style](rng); plan = "sig"
        else:
            g, ends = compose(style, rng); plan = "mod"
        if g is None:
            continue
        g, ends = flip(g, ends, rng.random() < 0.5, rng.random() < 0.5)
        n = len(floors(g))
        if not AREA[0] <= n <= AREA[1]:
            continue
        z = zone(g, ends, rng)
        if z:
            return g, z, k + 1, plan
    return None, None, tries, None


def similarity(a, b):
    """바닥 칸 Jaccard — 거울상 넷 가운데 가장 닮은 것(layout_gen.floor_similarity 와 같은 뜻)."""
    fa = set(floors(a)); best = 0.0
    for fx in (0, 1):
        for fy in (0, 1):
            gb, _ = flip(b, ((0, 0),), fx, fy); fb = set(floors(gb))
            best = max(best, len(fa & fb) / max(1, len(fa | fb)))
    return best

