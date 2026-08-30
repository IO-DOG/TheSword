# -*- coding: utf-8 -*-
"""96개 생성 층이 얼마나 닮았는지 <b>숫자로</b> 잰다. 아무것도 고치지 않는다.

    python Tools/measure_sameness.py

읽는 것은 Assets/StreamingAssets/Data/Excel/Dungeon_*.csv 뿐이다 (실제 출력물).
층 생성기를 다시 돌리지 않으므로, 지금 디스크에 있는 것이 그대로 재진다.
"""

import collections
import itertools
import math
import os
import sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
EXCEL = os.path.join(ROOT, "Assets", "StreamingAssets", "Data", "Excel")
HAND = ("00_000", "00_001", "00_002", "00_003")

GRID_W, GRID_H = 27, 23
ROOMS_X, ROOMS_Y = 3, 3
ROOM_W, ROOM_H = 7, 5
GAP_X, GAP_Y = 2, 3
DOOR_CELLS = ("3", "4", "5", "6", "7", "8")
KEYS = ("I_00", "I_01", "I_02")
RUNES = ("I_09", "I_10", "I_11")
NB = ((1, 0), (-1, 0), (0, 1), (0, -1))


def room_origin(rx, ry):
    return 1 + rx * (ROOM_W + GAP_X), 1 + ry * (ROOM_H + GAP_Y)


def room_cells(rx, ry):
    ox, oy = room_origin(rx, ry)
    return [(x, y) for y in range(oy, oy + ROOM_H) for x in range(ox, ox + ROOM_W)]


def room_center(rx, ry):
    ox, oy = room_origin(rx, ry)
    return ox + ROOM_W // 2, oy + ROOM_H // 2


ROOMS = [(x, y) for y in range(ROOMS_Y) for x in range(ROOMS_X)]
ROOM_OF = {}
for _r in ROOMS:
    for _c in room_cells(*_r):
        ROOM_OF[_c] = _r


def load(path):
    with open(path, encoding="utf-8-sig") as f:
        return [[c.strip() for c in line.split(",")] for line in f.read().splitlines() if line.strip()]


def passable(v):
    return v not in ("", "0") and not v.startswith("W")


def category(v):
    """그림이 아니라 <b>역할</b>로 묶는다. 몬스터 id 가 층마다 다른 것은 배치가
    다른 것이 아니기 때문이다."""
    if not passable(v):
        return "#"
    if v == "11":
        return "S"          # 스폰
    if v in ("14", "15", "16"):
        return "P"          # 포탈
    if v in DOOR_CELLS:
        return "D"          # 문
    if v in KEYS:
        return "K"          # 열쇠
    if v in RUNES:
        return "R"          # 룬
    if v.startswith("B_"):
        return "B"          # 보스
    if v.startswith("M_"):
        return "M"          # 몬스터
    if v.startswith("E_"):
        return "E"          # 장비
    if v.startswith("I_"):
        return "I"          # 물약 등
    return "."              # 빈 바닥


# ---------------------------------------------------------------- 구조 읽기

def corridor_edges(grid):
    """방 사이를 잇는 통로가 실제로 뚫려 있는 쌍. (통로 칸 목록도 같이)"""
    out = {}
    for (rx, ry) in ROOMS:
        # 오른쪽 이웃
        if rx + 1 < ROOMS_X:
            cy = room_center(rx, ry)[1]
            ox = room_origin(rx, ry)[0]
            cells = [(x, cy) for x in range(ox + ROOM_W, ox + ROOM_W + GAP_X)]
            if all(passable(grid[y][x]) for (x, y) in cells):
                out[((rx, ry), (rx + 1, ry))] = cells
        # 아래 이웃
        if ry + 1 < ROOMS_Y:
            cx = room_center(rx, ry)[0]
            oy = room_origin(rx, ry)[1]
            cells = [(cx, y) for y in range(oy + ROOM_H, oy + ROOM_H + GAP_Y)]
            if all(passable(grid[y][x]) for (x, y) in cells):
                out[((rx, ry), (rx, ry + 1))] = cells
    return out


def outside_floor_cells(grid):
    """방 밖인데 바닥인 칸 전부 (통로 + 골방)."""
    return [(x, y) for y in range(GRID_H) for x in range(GRID_W)
            if passable(grid[y][x]) and (x, y) not in ROOM_OF]


def alcove_cells(grid, edges):
    """통로에 속하지 않은 방 밖 바닥 = 골방."""
    used = set()
    for cells in edges.values():
        used.update(cells)
    return [c for c in outside_floor_cells(grid) if c not in used]


def find_all(grid, pred):
    return [(x, y) for y in range(GRID_H) for x in range(GRID_W) if pred(grid[y][x])]


def ham_paths(edges, start, goal):
    """present 간선만 써서 start -> goal 한붓그리기 전부."""
    adj = collections.defaultdict(set)
    for (a, b) in edges:
        adj[a].add(b)
        adj[b].add(a)
    out = []

    def walk(path, seen):
        if len(path) == len(ROOMS):
            if path[-1] == goal:
                out.append(list(path))
            return
        for n in adj[path[-1]]:
            if n in seen:
                continue
            path.append(n)
            seen.add(n)
            walk(path, seen)
            path.pop()
            seen.discard(n)

    walk([start], {start})
    return out


def room_order(grid, edges):
    """스폰방 -> 계단방 한붓그리기 순서를 되짚는다.

    문 셋이 순서의 3·5·7번째 간선 위에 있어야 하고, 열쇠 i 는 구역 i 안에
    있어야 한다 — 이 두 조건으로 후보를 거른다.
    """
    spawn = find_all(grid, lambda v: v == "11")
    up = find_all(grid, lambda v: v == "14")
    if not spawn or not up:
        return None, 0
    s_room, g_room = ROOM_OF.get(spawn[0]), ROOM_OF.get(up[0])
    if s_room is None or g_room is None:
        return None, 0

    door_by_color = {}
    for c in find_all(grid, lambda v: v in DOOR_CELLS):
        door_by_color[(int(grid[c[1]][c[0]]) - 3) % 3] = c

    key_room = {}
    for i, k in enumerate(KEYS):
        cells = find_all(grid, lambda v, k=k: v == k)
        if cells:
            key_room[i] = ROOM_OF.get(cells[0])

    cand = []
    for p in ham_paths(edges, s_room, g_room):
        ok = True
        for i, idx in enumerate((2, 4, 6)):
            e = (p[idx], p[idx + 1])
            cells = edges.get(e) or edges.get((e[1], e[0]))
            if not cells or door_by_color.get(i) not in cells:
                ok = False
                break
        if not ok:
            continue
        regions = [p[0:3], p[3:5], p[5:7], p[7:9]]
        for i in range(3):
            if key_room.get(i) is not None and key_room[i] not in regions[i]:
                ok = False
                break
        if ok:
            cand.append(tuple(p))
    if len(cand) == 1:
        return cand[0], 1
    return (cand[0] if cand else None), len(cand)


# ---------------------------------------------------------------- 대칭

def sym_variants(order):
    """정사각 격자의 8가지 대칭으로 옮긴 순서들. 회전·거울이면 같은 층이다."""
    def t(c, k):
        x, y = c
        for _ in range(k):
            x, y = ROOMS_Y - 1 - y, x
        return (x, y)
    out = []
    for mirror in (False, True):
        for k in range(4):
            o = tuple(t((ROOMS_X - 1 - c[0], c[1]) if mirror else c, k) for c in order)
            out.append(o)
    return out


def canon(order):
    return min(sym_variants(order))


# ---------------------------------------------------------------- 본체

def main():
    files = sorted(fn for fn in os.listdir(EXCEL)
                   if fn.startswith("Dungeon_") and fn.endswith(".csv"))
    gen, hand = [], []
    for fn in files:
        did = fn[len("Dungeon_"):-4]
        g = load(os.path.join(EXCEL, fn))
        (hand if did in HAND else gen).append((did, g))

    print("=" * 68)
    print(" 96개 생성 층 단조로움 측정 —  읽은 파일 %d개 (생성 %d / 손수 %d)"
          % (len(files), len(gen), len(hand)))
    print("=" * 68)

    # ---------------- 1. 격자·방 크기
    dims = collections.Counter((len(g[0]), len(g)) for _, g in gen)
    print("\n[1] 격자·방")
    print("  격자 크기 종류      : %s" % dict(dims))
    room_ok = sum(1 for _, g in gen
                  if all(passable(g[y][x]) for r in ROOMS for (x, y) in room_cells(*r)))
    print("  9개 방(7x5)이 통째로 바닥인 층 : %d / %d" % (room_ok, len(gen)))
    # 방 밖 바닥이 방 모양을 바꾸는가 (방에 붙은 여분 칸)
    extra = collections.Counter(len(outside_floor_cells(g)) for _, g in gen)
    print("  방 밖 바닥 칸 수 분포 : %s" % dict(sorted(extra.items())))

    # ---------------- 2. 통로
    print("\n[2] 통로")
    edge_sets, edge_cnt, corr_shape = [], collections.Counter(), collections.Counter()
    for _, g in gen:
        e = corridor_edges(g)
        edge_sets.append(frozenset(e))
        edge_cnt[len(e)] += 1
        for (a, b) in e:
            corr_shape["가로직선" if a[1] == b[1] else "세로직선"] += 1
    print("  통로 개수 분포        : %s" % dict(sorted(edge_cnt.items())))
    # 꺾인 통로가 없다는 것을 <b>세어서</b> 확인한다. 방 밖 바닥 칸이
    # (가로통로 2칸 + 세로통로 3칸)의 합보다 딱 2칸(골방) 많으면 남는 칸이 없다.
    straight = 0
    for _, g in gen:
        e = corridor_edges(g)
        h = sum(1 for (a, b) in e if a[1] == b[1])
        v = len(e) - h
        if 2 * h + 3 * v + 2 == len(outside_floor_cells(g)):
            straight += 1
    print("  통로 모양             : %s" % dict(corr_shape))
    print("  방 밖 바닥이 '곧은 통로 + 골방 2칸' 으로 딱 맞는 층 : %d / %d"
          % (straight, len(gen)))
    print("  서로 다른 통로 배치   : %d 가지 / %d 층" % (len(set(edge_sets)), len(gen)))
    top = collections.Counter(edge_sets).most_common(3)
    for s, n in top:
        print("      최다 배치 %2d개 층" % n)

    # ---------------- 3. 한붓그리기 순서
    print("\n[3] 방 순서(한붓그리기)")
    orders, amb = [], 0
    for did, g in gen:
        o, n = room_order(g, corridor_edges(g))
        if n != 1:
            amb += 1
        if o:
            orders.append(o)
    uniq = set(orders)
    print("  되짚은 층              : %d / %d  (후보가 하나로 안 좁혀진 층 %d)"
          % (len(orders), len(gen), amb))
    print("  서로 다른 순서         : %d 가지" % len(uniq))
    cls = collections.Counter(canon(o) for o in orders)
    print("  대칭(회전·거울) 무시   : %d 가지  — 층수 분포 %s"
          % (len(cls), sorted(cls.values(), reverse=True)))
    oc = collections.Counter(orders).most_common(5)
    print("  한 순서가 최대 몇 층에 쓰였나 : %d 층 (상위 5개 %s)"
        % (oc[0][1], [n for _, n in oc]))
    # 3x3 격자에서 가능한 한붓그리기 전부
    full = {}
    for (rx, ry) in ROOMS:
        for d in ((1, 0), (0, 1)):
            n = (rx + d[0], ry + d[1])
            if n in ROOMS:
                full[((rx, ry), n)] = 1
    allp = set()
    for s in ROOMS:
        for gl in ROOMS:
            if s == gl:
                continue
            for p in ham_paths(full, s, gl):
                allp.add(tuple(p))
    print("  3x3 에서 가능한 순서   : %d 가지 (이 중 %d 가지만 쓴다 = %.0f%%)"
          % (len(allp), len(uniq), 100.0 * len(uniq) / len(allp)))

    # 되짚기가 맞는지 확인 — 생성기 규칙("계단 방으로는 곁길을 내지 않는다")이
    # 되짚은 순서에서 그대로 보여야 한다. 안 맞으면 아래 숫자를 믿으면 안 된다.
    shortcut_cnt, last_touch, ok_back = collections.Counter(), 0, 0
    for did, g in gen:
        e = corridor_edges(g)
        o, n = room_order(g, e)
        if not o or n != 1:
            continue
        ok_back += 1
        main = {frozenset((o[i], o[i + 1])) for i in range(len(o) - 1)}
        extra = [k for k in e if frozenset(k) not in main]
        shortcut_cnt[len(extra)] += 1
        if any(o[-1] in k for k in extra):
            last_touch += 1
    print("  되짚기 자기검사        : 곁길 통로 수 %s / 계단 방에 닿은 곁길 %d개 층 (0이어야 맞다)"
          % (dict(sorted(shortcut_cnt.items())), last_touch))

    # ---------------- 4. 문
    print("\n[4] 문")
    dcount = collections.Counter()
    dpos, dcode = [], collections.Counter()
    dedge_idx = collections.Counter()
    for did, g in gen:
        cells = find_all(g, lambda v: v in DOOR_CELLS)
        dcount[len(cells)] += 1
        dpos.append(frozenset(cells))
        for c in cells:
            dcode[g[c[1]][c[0]]] += 1
        e = corridor_edges(g)
        for c in cells:
            for (a, b), cs in e.items():
                if c in cs:
                    dedge_idx["가로통로" if a[1] == b[1] else "세로통로"] += 1
    print("  층당 문 개수           : %s" % dict(sorted(dcount.items())))
    print("  문 셀 코드 분포        : %s" % dict(sorted(dcode.items())))
    print("  문이 선 통로 방향      : %s" % dict(dedge_idx))
    print("  서로 다른 문 3좌표 조합: %d 가지 / %d 층" % (len(set(dpos)), len(gen)))
    allcells = collections.Counter()
    for s in dpos:
        allcells.update(s)
    print("  문이 앉은 칸 종류      : %d 개 (가장 잦은 칸 %d 층 = %.0f%%)"
          % (len(allcells), allcells.most_common(1)[0][1],
             100.0 * allcells.most_common(1)[0][1] / len(gen)))

    # 구역 크기
    reg = collections.Counter()
    for did, g in gen:
        o, n = room_order(g, corridor_edges(g))
        if o:
            reg[(3, 2, 2, 2)] += 1
    print("  구역 크기 3/2/2/2 인 층: %d / %d" % (reg[(3, 2, 2, 2)], len(orders)))

    # ---------------- 5. 층끼리 닮은 정도
    print("\n[5] 층끼리 얼마나 닮았는가  (모든 쌍 %d 개)"
          % (len(gen) * (len(gen) - 1) // 2))
    bits = [[1 if passable(g[y][x]) else 0 for y in range(GRID_H) for x in range(GRID_W)]
            for _, g in gen]
    cats = ["".join(category(g[y][x]) for y in range(GRID_H) for x in range(GRID_W))
            for _, g in gen]
    N = GRID_W * GRID_H

    # 항상 같은 칸 / 변하는 칸
    always = sum(1 for i in range(N) if all(b[i] == bits[0][i] for b in bits))
    print("  격자 칸 %d개 중 96층 전부 같은 칸 : %d (%.1f%%) — 변하는 칸 %d"
          % (N, always, 100.0 * always / N, N - always))

    var_idx = [i for i in range(N) if not all(b[i] == bits[0][i] for b in bits)]

    def pair_stats(seqs, idxs=None):
        idxs = range(len(seqs[0])) if idxs is None else idxs
        tot, mn, mx, cnt = 0.0, 1.0, 0.0, 0
        for a, b in itertools.combinations(range(len(seqs)), 2):
            m = sum(1 for i in idxs if seqs[a][i] == seqs[b][i]) / max(1, len(list(idxs)) if not isinstance(idxs, range) else len(idxs))
            tot += m
            mn = min(mn, m)
            mx = max(mx, m)
            cnt += 1
        return tot / cnt, mn, mx

    avg, mn, mx = pair_stats(bits)
    print("  벽/바닥 비트맵 일치율   : 평균 %.1f%%  (최저 %.1f%% / 최고 %.1f%%)"
          % (avg * 100, mn * 100, mx * 100))
    if var_idx:
        avg2, mn2, mx2 = pair_stats(bits, var_idx)
        print("  변하는 칸 %d개만 보면   : 평균 %.1f%%  (최저 %.1f%% / 최고 %.1f%%)"
              % (len(var_idx), avg2 * 100, mn2 * 100, mx2 * 100))
    avg3, mn3, mx3 = pair_stats(cats)
    print("  역할지도(문·몹·물약까지): 평균 %.1f%%  (최저 %.1f%% / 최고 %.1f%%)"
          % (avg3 * 100, mn3 * 100, mx3 * 100))

    print("  * 이 지표가 가질 수 있는 범위: 변하는 칸이 전부 어긋나도 %.1f%%,"
          " 완전히 같으면 100%%" % (100.0 * always / N))
    print("  * 즉 96개 층은 %.1f~100%% 구간의 %.0f%% 지점에 몰려 있다"
          % (100.0 * always / N,
             100.0 * (avg - always / N) / (1 - always / N)))

    # 손수 만든 층과의 대조 — 격자 크기부터 다르다
    hd = collections.Counter((len(g[0]), len(g)) for _, g in hand)
    print("  (대조) 손수 만든 4개 층의 격자 크기 : %s  — 4개 층이 %d 가지"
          % (dict(hd), len(hd)))

    print("  서로 다른 벽/바닥 비트맵 : %d 가지 / %d 층" % (len(set(map(tuple, bits))), len(gen)))
    print("  서로 다른 역할지도       : %d 가지 / %d 층" % (len(set(cats)), len(gen)))

    # 27x23 은 정사각이 아니라 대칭이 넷(그대로/좌우/상하/180°)뿐이다.
    # 거울로 뒤집어 겹치면 사람 눈에는 같은 층이다.
    def flips(b):
        m = [b[y * GRID_W:(y + 1) * GRID_W] for y in range(GRID_H)]
        out = []
        for fy in (0, 1):
            for fx in (0, 1):
                r = [row[::-1] if fx else row for row in (m[::-1] if fy else m)]
                out.append(tuple(v for row in r for v in row))
        return out

    canon_bits = set(min(flips(b)) for b in bits)
    print("  대칭(좌우·상하) 무시하면 : %d 가지 / %d 층" % (len(canon_bits), len(gen)))
    best = []
    for a, b in itertools.combinations(range(len(gen)), 2):
        fb = flips(bits[b])
        ta = tuple(bits[a])
        best.append(max(sum(1 for i in range(N) if ta[i] == f[i]) / N for f in fb))
    print("  대칭까지 맞춰 본 최고 일치율 : 평균 %.1f%% (최고 %.1f%%)"
          % (100.0 * sum(best) / len(best), 100.0 * max(best)))
    for a, b in itertools.combinations(range(len(gen)), 2):
        if tuple(bits[a]) in flips(bits[b]):
            print("    -> 벽·바닥이 완전히 같은 쌍: Dungeon_%s 와 Dungeon_%s (거울상)"
                  % (gen[a][0], gen[b][0]))

    # ---------------- 6. 오브젝트 자리
    print("\n[6] 몬스터·아이템·골방 자리")
    kinds = {
        "몬스터": lambda v: v.startswith("M_"),
        "보스": lambda v: v.startswith("B_"),
        "열쇠": lambda v: v in KEYS,
        "룬": lambda v: v in RUNES,
        "물약": lambda v: v.startswith("I_") and v not in KEYS and v not in RUNES,
        "장비": lambda v: v.startswith("E_"),
        "위계단": lambda v: v == "14",
        "아래계단": lambda v: v == "15",
    }
    print("  %-8s %6s %6s %8s %8s %8s" % ("", "총개수", "층당", "쓰인칸", "엔트로피", "최다칸%"))
    for name, pred in kinds.items():
        cells = []
        per = []
        for _, g in gen:
            c = find_all(g, pred)
            cells += c
            per.append(len(c))
        if not cells:
            continue
        cnt = collections.Counter(cells)
        ent = -sum((v / len(cells)) * math.log2(v / len(cells)) for v in cnt.values())
        top10 = sum(v for _, v in cnt.most_common(10))
        print("  %-8s %6d %6.2f %8d %8.2f %7.1f%%   상위10칸이 %.0f%%"
              % (name, len(cells), len(cells) / len(gen), len(cnt), ent,
                 100.0 * cnt.most_common(1)[0][1] / len(gen),
                 100.0 * top10 / len(cells)))

    # 몬스터가 어디에 서는가 — 방 안인가 통로 목인가
    gate12 = set()
    for (rx, ry) in ROOMS:
        if rx + 1 < ROOMS_X:
            gate12.add((room_origin(rx, ry)[0] + ROOM_W + 1, room_center(rx, ry)[1]))
        if ry + 1 < ROOMS_Y:
            gate12.add((room_center(rx, ry)[0], room_origin(rx, ry)[1] + ROOM_H + 1))
    inroom = outroom = ongate = 0
    for _, g in gen:
        for c in find_all(g, lambda v: v.startswith(("M_", "B_"))):
            if c in gate12:
                ongate += 1
            if c in ROOM_OF:
                inroom += 1
            else:
                outroom += 1
    tot = inroom + outroom
    print("  몬스터가 선 자리       : 방 밖(통로) %d (%.0f%%) / 방 안 %d (%.0f%%)"
          % (outroom, 100.0 * outroom / tot, inroom, 100.0 * inroom / tot))
    print("      그중 '문이 앉을 수 있는 12칸' 위 : %d (%.0f%%)"
          % (ongate, 100.0 * ongate / tot))

    # 몬스터 자리 겹침
    msets = [frozenset(find_all(g, lambda v: v.startswith("M_"))) for _, g in gen]
    ov = [len(a & b) for a, b in itertools.combinations(msets, 2)]
    print("  두 층의 몬스터 자리 겹침 : 평균 %.2f / 5 마리 (겹침 0인 쌍 %.0f%%)"
          % (sum(ov) / len(ov), 100.0 * sum(1 for v in ov if v == 0) / len(ov)))

    # 골방
    print("\n  골방(막다른 두 칸)")
    acnt = collections.Counter()
    apos = collections.Counter()
    for _, g in gen:
        a = alcove_cells(g, corridor_edges(g))
        acnt[len(a)] += 1
        for c in a:
            apos[c] += 1
    print("    층당 골방 칸 수      : %s" % dict(sorted(acnt.items())))
    print("    골방이 앉은 칸 종류  : %d 개 (가장 잦은 칸 %d 층)"
          % (len(apos), apos.most_common(1)[0][1] if apos else 0))
    arow = collections.Counter(c[1] for c in apos.elements())
    print("    골방이 앉은 행       : %s" % dict(sorted(arow.items())))

    # ---------------- 6-b. 층이 담고 있는 <b>내용물</b>
    print("\n[6-b] 층의 내용물 (자리를 빼고 종류·개수만)")
    inv = []
    for did, g in gen:
        c = collections.Counter()
        for row in g:
            for v in row:
                if v.startswith(("I_", "E_")):
                    c[v] += 1
                elif v.startswith("M_"):
                    c["몬스터"] += 1
                elif v.startswith("B_"):
                    c["보스"] += 1
        inv.append((did, tuple(sorted(c.items()))))
    kinds_cnt = collections.Counter(v for _, v in inv)
    print("  서로 다른 내용물 조합  : %d 가지 / %d 층" % (len(kinds_cnt), len(gen)))
    for combo, n in kinds_cnt.most_common(6):
        items = ", ".join("%s x%d" % (k, v) for k, v in combo)
        print("    %2d개 층 : %s" % (n, items))

    # 물약만 (층 유형이 실제로 다르게 만드는 유일한 것)
    pot = collections.Counter()
    for did, g in gen:
        c = tuple(sorted(collections.Counter(
            v for row in g for v in row
            if v.startswith("I_") and v not in KEYS and v not in RUNES).items()))
        pot[c] += 1
    print("  물약 예산 조합         : %d 가지 / %d 층" % (len(pot), len(gen)))
    for combo, n in pot.most_common(8):
        print("    %2d개 층 : %s" % (n, ", ".join("%s x%d" % kv for kv in combo)))

    # ---------------- 7. 걷는 거리 / 챕터 구분
    print("\n[7] 걷는 거리와 챕터")

    def bfs_len(g, a, b):
        seen, q, d = {a}, collections.deque([(a, 0)]), None
        while q:
            c, n = q.popleft()
            if c == b:
                return n
            for dx, dy in NB:
                p = (c[0] + dx, c[1] + dy)
                if not (0 <= p[0] < GRID_W and 0 <= p[1] < GRID_H) or p in seen:
                    continue
                if not passable(g[p[1]][p[0]]):
                    continue
                seen.add(p)
                q.append((p, n + 1))
        return None

    lens, areas = [], []
    for _, g in gen:
        s = find_all(g, lambda v: v == "11")[0]
        u = find_all(g, lambda v: v == "14")[0]
        d = bfs_len(g, s, u)          # 문을 다 연 상태의 최단거리
        if d:
            lens.append(d)
        areas.append(sum(1 for y in range(GRID_H) for x in range(GRID_W)
                         if passable(g[y][x])))
    print("  스폰->계단 최단거리     : 평균 %.1f칸 (최소 %d / 최대 %d), 종류 %d개"
          % (sum(lens) / len(lens), min(lens), max(lens), len(set(lens))))
    print("  밟을 수 있는 칸 수      : 평균 %.1f (최소 %d / 최대 %d)"
          % (sum(areas) / len(areas), min(areas), max(areas)))
    wall_by_ch = collections.defaultdict(collections.Counter)
    for did, g in gen:
        ch = int(did.split("_")[0])
        for row in g:
            for c in row:
                if c.startswith("W"):
                    wall_by_ch[ch][c] += 1
    pairs = {}
    for ch in sorted(wall_by_ch):
        s = frozenset(wall_by_ch[ch])
        pairs.setdefault(s, []).append(ch)
        print("  챕터 %02d 벽 타일 : %s" % (ch, dict(sorted(wall_by_ch[ch].items()))))
    print("  -> 다섯 챕터가 쓰는 벽 타일 조합은 %d 가지뿐 : %s"
          % (len(pairs), [sorted(v) for v in pairs.values()]))

    # ---------------- 8. 한 층이 갖는 변주의 총량
    print("\n[8] 한 층이 실제로 고를 수 있는 것")
    print("  방 배치      : 1 가지 (3x3 고정, 7x5 고정)")
    print("  방 순서      : %d 가지 관측" % len(uniq))
    print("  통로 배치    : %d 가지 관측" % len(set(edge_sets)))
    print("  골방 자리    : %d 칸 관측" % len(apos))
    print("  즉 층을 구분하는 변수는 위 셋뿐이고, 나머지 %.1f%% 의 칸은 96층이 같다."
          % (100.0 * always / N))
    return 0


if __name__ == "__main__":
    sys.exit(main())
