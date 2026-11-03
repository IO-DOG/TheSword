"""voxel_kit — TheSword 챕터 복셀 키트 (MagicaVoxel 식 OBJ 를 읽고, 절차적으로 짓고, 내보내고, 미리 본다).

다섯 챕터(5~100층)가 색 필터가 아니라 다섯 곳으로 보이게, 게임의 복셀 화풍(0.1 복셀, 256x1 팔레트) 그대로
벽 4종과 소품을 굽는다. 기획 문서는 Documents/TheSword_Meshy_Prompts.md(챕터 분위기·벽·소품·지킬 것).
들여오기는 Assets/@Scripts/Editor/MeshyArtImport.cs 가 한다(같은 이름의 Meshy 그림이 있으면 그쪽이 이긴다).

    python Tools/voxel_kit.py build [--only 0,2] [--out DIR] [--preview DIR] [--no-sheets]
        에셋(OBJ+MTL+PNG, 빛나는 칸이 있으면 _Emit.png) -> Assets/@Resources/Art/ChapterKit/
        미리보기·대조표 -> --preview (기본: 임시 폴더/voxel_kit_preview) — Assets 아래에 두지 않는다
    python Tools/voxel_kit.py check      짓기만 해서 경계·자리 상자·삼각형 예산·OBJ 왕복을 잰다
    python Tools/voxel_kit.py inspect PATH.obj [--preview DIR]
        기존 MagicaVoxel OBJ 를 복셀로 읽어 게임 카메라·3/4 시점으로 그린다

numpy 와 Pillow 만 쓴다. 같은 입력이면 같은 파일이 나온다(씨앗 고정).

좌표는 전부 Unity 공간(x 오른쪽, y 위, z 북쪽 — 카메라는 남쪽에서 60도로 내려다본다)이고,
OBJ 를 쓸 때만 x 를 뒤집는다(Unity 가 OBJ 를 들일 때 x 를 뒤집는다).

자리 규칙(MeshyArtImport.Rule 과 같다 — 넘으면 들일 때 줄어 복셀 크기가 벽과 달라진다):
    벽        x/z -1.6..1.6 (몸통 -1.5..1.5 + 윗면 덮개 0.1), y 2.0..5.3 — 칸에 꼭 맞고 대칭
    큰 소품    벽 한 칸(32 x 30 x 32 복셀) 안, 바닥 가운데 — 벽 그림을 끈 칸에 선다
    거는 것    20 x 20 복셀 안(불은 12 x 20), 등 = 북쪽(max z) — 남쪽 벽면 가운데에 붙는다
    바닥 것    19 x 8 x 19 복셀 안(발목 높이) — 걷는 칸에 선다
"""
import json
import math
import os
import sys
import tempfile
import time
import types

import numpy as np
from PIL import Image, ImageDraw


def find_root(start=None):
    """Assets 와 ProjectSettings 가 있는 Unity 프로젝트 뿌리. THESWORD_ROOT 가 있으면 그것."""
    env = os.environ.get('THESWORD_ROOT')
    if env:
        return env
    d = os.path.abspath(start or os.path.dirname(os.path.abspath(__file__)))
    for _ in range(6):
        if os.path.isdir(os.path.join(d, 'Assets')) and os.path.isdir(os.path.join(d, 'ProjectSettings')):
            return d
        d = os.path.dirname(d)
    return None


ROOT = find_root()


# ================================================================ core: 팔레트·격자·OBJ 읽기/쓰기·그리기
# vkcore — MagicaVoxel 식 복셀 OBJ 를 읽고, 고치고, 내보내고, 미리 본다 (numpy + Pillow 만).
#
# 좌표는 전부 **Unity 공간**(x 오른쪽, y 위, z 북쪽 = 카메라 반대쪽, 남쪽 -z 면이 카메라를 본다)이다.
# OBJ 는 오른손 좌표라 Unity 가 들여올 때 x 를 뒤집는다 — 읽을 때 x 를 뒤집고 쓸 때 다시 뒤집는다.
# 그래서 여기서 오른쪽에 그린 것은 게임에서도 오른쪽에 보인다.
#
# 복셀 하나 = 모델 0.1 유닛(프리팹 축척 0.1 이면 월드 0.01 = 픽셀 퍼펙트 카메라의 한 점).
# 팔레트는 MagicaVoxel 과 같다: 색 번호 i(1~255) = 256x1 PNG 의 i-1 번째 칸, u = (i-1+0.5)/256.
VOX = 0.1


# ---------------------------------------------------------------- palette
def hex2rgb(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


class Palette:
    """256 칸 팔레트. 0 번은 빈칸이라 쓰지 않는다. ramp() 가 이름 붙인 색 묶음을 차례로 올린다."""

    def __init__(self):
        self.rgba = np.zeros((256, 4), np.uint8)
        self.rgba[:, 3] = 255
        self.emit = np.zeros(256, bool)
        self.n = 1
        self.names = {}
        self.ramps = {}

    def ramp(self, name, hexes, emit=False):
        idx = []
        for h in hexes:
            if self.n > 255:
                raise ValueError('palette full')
            self.rgba[self.n, :3] = hex2rgb(h)
            self.emit[self.n] = emit
            self.names[self.n] = name
            idx.append(self.n)
            self.n += 1
        self.ramps[name] = idx
        return idx

    def __getitem__(self, name):
        return self.ramps[name]

    def png(self):
        img = np.zeros((1, 256, 4), np.uint8)
        img[0, :255] = self.rgba[1:256]
        img[0, 255] = (0, 0, 0, 255)
        return Image.fromarray(img, 'RGBA')

    def emit_png(self):
        """빛나는 칸만 제 색, 나머지는 검정 — Unity 에서 _EmissionMap 으로 쓰면 그 칸만 빛난다."""
        img = np.zeros((1, 256, 4), np.uint8)
        img[0, :, 3] = 255
        for i in range(1, 256):
            if self.emit[i]:
                img[0, i - 1, :3] = self.rgba[i, :3]
        return Image.fromarray(img, 'RGBA')


# ---------------------------------------------------------------- grid
class Grid:
    """복셀 격자. v[i, j, k] = 색 번호(0 = 빈칸). origin = (0,0,0) 칸의 최소 모서리(모델 유닛, Unity 공간)."""

    def __init__(self, shape, origin):
        self.v = np.zeros(shape, np.uint8)
        self.origin = np.asarray(origin, float)

    @property
    def shape(self):
        return self.v.shape

    def copy(self):
        g = Grid(self.v.shape, self.origin)
        g.v = self.v.copy()
        return g

    def index_of(self, x, y, z):
        """모델 좌표(칸 최소 모서리) -> 칸 번호."""
        return tuple(int(round((c - o) / VOX)) for c, o in zip((x, y, z), self.origin))

    def bounds(self):
        """채워진 칸의 모델 좌표 범위 (min xyz, max xyz)."""
        nz = np.argwhere(self.v > 0)
        if len(nz) == 0:
            return None
        lo = self.origin + nz.min(0) * VOX
        hi = self.origin + (nz.max(0) + 1) * VOX
        return lo, hi

    def crop(self):
        """빈 가장자리를 잘라 낸다(원점은 그대로 맞춘다)."""
        nz = np.argwhere(self.v > 0)
        lo, hi = nz.min(0), nz.max(0) + 1
        g = Grid(tuple(hi - lo), self.origin + lo * VOX)
        g.v = self.v[lo[0]:hi[0], lo[1]:hi[1], lo[2]:hi[2]].copy()
        return g


def flood_outside(solid):
    """격자 밖에서 빈칸을 따라 닿는 곳 = 바깥. 닫힌 속은 False 로 남는다 (6방향)."""
    pad = np.pad(solid, 1, constant_values=False)
    out = np.zeros_like(pad)
    out[0, :, :] = out[-1, :, :] = True
    out[:, 0, :] = out[:, -1, :] = True
    out[:, :, 0] = out[:, :, -1] = True
    out &= ~pad
    free = ~pad
    while True:
        grow = out.copy()
        grow[1:] |= out[:-1]
        grow[:-1] |= out[1:]
        grow[:, 1:] |= out[:, :-1]
        grow[:, :-1] |= out[:, 1:]
        grow[:, :, 1:] |= out[:, :, :-1]
        grow[:, :, :-1] |= out[:, :, 1:]
        grow &= free
        if (grow == out).all():
            break
        out = grow
    return out[1:-1, 1:-1, 1:-1]


def dilate_colors(v, target):
    """target(빈 속) 칸을 이웃 색으로 채운다 — 속을 파낼 때 드러날 색."""
    v = v.copy()
    todo = target & (v == 0)
    while todo.any():
        before = todo.sum()
        for ax in range(3):
            for s in (1, -1):
                src = np.roll(v, s, axis=ax)
                # roll 이 맞은편을 끌어오지 않게 가장자리를 막는다
                sl = [slice(None)] * 3
                sl[ax] = 0 if s == 1 else -1
                src[tuple(sl)] = 0
                take = todo & (src > 0)
                v[take] = src[take]
                todo &= ~take
        if todo.sum() == before:
            break
    return v


# ---------------------------------------------------------------- MagicaVoxel OBJ import
def read_obj(path, fill=True):
    """MagicaVoxel OBJ -> (Grid, 팔레트 RGBA 256x4). 면 중심에서 법선 반대로 반 칸 들어간 곳이 그 면의 복셀이다."""
    vs, vts, vns, faces = [], [], [], []
    for line in open(path, encoding='utf-8', errors='replace'):
        a = line.split()
        if not a:
            continue
        if a[0] == 'v':
            vs.append([-float(a[1]), float(a[2]), float(a[3])])   # OBJ -> Unity: x 를 뒤집는다
        elif a[0] == 'vt':
            vts.append(float(a[1]))
        elif a[0] == 'vn':
            vns.append([-float(a[1]), float(a[2]), float(a[3])])
        elif a[0] == 'f':
            refs = [p.split('/') for p in a[1:]]
            faces.append([(int(r[0]) - 1, int(r[1]) - 1 if len(r) > 1 and r[1] else -1,
                           int(r[2]) - 1 if len(r) > 2 and r[2] else -1) for r in refs])
    V = np.array(vs)
    # 격자는 모델의 최소 모서리에서 시작한다 — 홀수 칸 모델은 MagicaVoxel 도 반 칸 어긋나게 가운데를 잡는다
    lo = np.round(V.min(0), 4)
    shape = tuple(int(round(s)) for s in (V.max(0) - lo) / VOX)
    g = Grid(shape, lo)
    for f in faces:
        P = V[[r[0] for r in f]]
        if f[0][2] >= 0:
            n = np.array(vns[f[0][2]])
        else:
            n = np.cross(P[1] - P[0], P[2] - P[0])
            n = -n  # x 를 뒤집어 감김이 바뀌었다
        ax = int(np.argmax(np.abs(n)))
        sgn = 1 if n[ax] > 0 else -1
        texel = int(math.floor(vts[f[0][1]] * 256)) if f[0][1] >= 0 else 0
        col = texel + 1
        b, c = [i for i in range(3) if i != ax]
        plane = P[0][ax]
        # 삼각형(또는 다각형)을 칸 중심으로 훑는다
        tris = [(P[0], P[i], P[i + 1]) for i in range(1, len(P) - 1)]
        for A, B, C in tris:
            bmin, bmax = min(A[b], B[b], C[b]), max(A[b], B[b], C[b])
            cmin, cmax = min(A[c], B[c], C[c]), max(A[c], B[c], C[c])
            ib0 = int(math.floor((bmin - lo[b]) / VOX + 1e-6))
            ib1 = int(math.ceil((bmax - lo[b]) / VOX - 1e-6))
            ic0 = int(math.floor((cmin - lo[c]) / VOX + 1e-6))
            ic1 = int(math.ceil((cmax - lo[c]) / VOX - 1e-6))
            for ib in range(ib0, ib1):
                pb = lo[b] + (ib + 0.5) * VOX
                for ic in range(ic0, ic1):
                    pc = lo[c] + (ic + 0.5) * VOX
                    if _in_tri(pb, pc, (A[b], A[c]), (B[b], B[c]), (C[b], C[c])):
                        ia = int(math.floor((plane - sgn * VOX * 0.5 - lo[ax]) / VOX))
                        idx = [0, 0, 0]
                        idx[ax], idx[b], idx[c] = ia, ib, ic
                        if all(0 <= idx[i] < shape[i] for i in range(3)):
                            g.v[tuple(idx)] = col
    if fill:
        solid = g.v > 0
        outside = flood_outside(solid)
        inner = ~outside & ~solid
        if inner.any():
            g.v = dilate_colors(g.v, inner)
    pal = None
    png = os.path.splitext(path)[0] + '.png'
    if os.path.exists(png):
        im = np.array(Image.open(png).convert('RGBA'))
        if im.shape[0] == 1 and im.shape[1] == 256:
            pal = np.zeros((256, 4), np.uint8)
            pal[1:256] = im[0, :255]
    return g, pal


def _in_tri(px, py, a, b, c):
    def s(p1, p2, p3):
        return (p1[0] - p3[0]) * (p2[1] - p3[1]) - (p2[0] - p3[0]) * (p1[1] - p3[1])
    p = (px, py)
    d1, d2, d3 = s(p, a, b), s(p, b, c), s(p, c, a)
    neg = (d1 < -1e-9) or (d2 < -1e-9) or (d3 < -1e-9)
    pos = (d1 > 1e-9) or (d2 > 1e-9) or (d3 > 1e-9)
    return not (neg and pos)


# ---------------------------------------------------------------- meshing
# MagicaVoxel 의 법선 순서 (OBJ 공간)
_MV_NORMALS = [(-1, 0, 0), (1, 0, 0), (0, 0, 1), (0, 0, -1), (0, -1, 0), (0, 1, 0)]


def quads(grid, greedy=True, cull_bottom=True, keep=None):
    """보이는 면을 사각형으로 모은다. 반환: [(corners 4x3 Unity 모델 좌표, normal(Unity), color)].
    cull_bottom: 맨 아래층의 아랫면(-y)은 바닥에 붙어 아무도 못 보니 뺀다."""
    v = grid.v
    nx, ny, nz = v.shape
    out = []
    solid = v > 0
    for ax in range(3):
        for s in (-1, 1):
            nb = np.zeros_like(solid)
            if s == 1:
                sl_src = [slice(None)] * 3
                sl_src[ax] = slice(1, None)
                sl_dst = [slice(None)] * 3
                sl_dst[ax] = slice(None, -1)
            else:
                sl_src = [slice(None)] * 3
                sl_src[ax] = slice(None, -1)
                sl_dst = [slice(None)] * 3
                sl_dst[ax] = slice(1, None)
            nb[tuple(sl_dst)] = solid[tuple(sl_src)]
            face = solid & ~nb
            if cull_bottom and ax == 1 and s == -1:
                # 아랫면(-y)은 위에서 내려다보는 카메라에도, 위에서 오는 해에도 등을 돌린다 — 전부 뺀다
                face[:] = False
            b, c = [i for i in range(3) if i != ax]
            n = [0, 0, 0]
            n[ax] = s
            for t in range(v.shape[ax]):
                sl = [slice(None)] * 3
                sl[ax] = t
                m = np.where(face[tuple(sl)], v[tuple(sl)], 0).astype(np.int32)
                if not m.any():
                    continue
                nb_, nc_ = m.shape
                if not greedy:
                    for ib, ic in np.argwhere(m > 0):
                        out.append(_quad(grid, ax, s, t, b, c, ib, ic, 1, 1, int(m[ib, ic]), n))
                    continue
                for ic in range(nc_):
                    ib = 0
                    while ib < nb_:
                        col = m[ib, ic]
                        if col == 0:
                            ib += 1
                            continue
                        w = 1
                        while ib + w < nb_ and m[ib + w, ic] == col:
                            w += 1
                        h = 1
                        while ic + h < nc_ and (m[ib:ib + w, ic + h] == col).all():
                            h += 1
                        m[ib:ib + w, ic:ic + h] = 0
                        out.append(_quad(grid, ax, s, t, b, c, ib, ic, w, h, int(col), n))
                        ib += w
    return out


def _quad(grid, ax, s, t, b, c, ib, ic, w, h, col, n):
    plane = t + (1 if s > 0 else 0)
    pts = []
    for db, dc in ((0, 0), (w, 0), (w, h), (0, h)):
        idx = [0, 0, 0]
        idx[ax] = plane
        idx[b] = ib + db
        idx[c] = ic + dc
        pts.append(grid.origin + np.array(idx, float) * VOX)
    return np.array(pts), np.array(n, float), col


# ---------------------------------------------------------------- export
def write_asset(base_path, grid, pal, title=None, greedy=True, cull_bottom=True):
    """base_path(확장자 없이) 에 .obj/.mtl/.png 를 쓴다. 빛나는 칸이 있으면 _Emit.png 도. 삼각형 수를 돌려준다."""
    name = os.path.basename(base_path)
    qs = quads(grid, greedy=greedy, cull_bottom=cull_bottom)
    verts = {}
    vlist = []
    used_tex = {}
    tlist = []
    flines = []
    nidx = {n: i + 1 for i, n in enumerate(_MV_NORMALS)}

    def vid(p):
        key = (round(-p[0], 4) + 0.0, round(p[1], 4) + 0.0, round(p[2], 4) + 0.0)   # Unity -> OBJ: x 를 뒤집는다
        if key not in verts:
            verts[key] = len(vlist) + 1
            vlist.append(key)
        return verts[key]

    def tid(col):
        if col not in used_tex:
            used_tex[col] = len(tlist) + 1
            tlist.append((col - 1 + 0.5) / 256.0)
        return used_tex[col]

    for P, n, col in qs:
        n_obj = (-int(n[0]) + 0, int(n[1]), int(n[2]))
        Po = P.copy()
        Po[:, 0] = -Po[:, 0]
        cr = np.cross(Po[1] - Po[0], Po[2] - Po[0])
        order = [0, 1, 2, 3] if np.dot(cr, n_obj) > 0 else [0, 3, 2, 1]
        ids = [vid(P[i]) for i in order]
        t = tid(col)
        nn = nidx[n_obj]
        flines.append('f %d/%d/%d %d/%d/%d %d/%d/%d' % (ids[0], t, nn, ids[1], t, nn, ids[2], t, nn))
        flines.append('f %d/%d/%d %d/%d/%d %d/%d/%d' % (ids[0], t, nn, ids[2], t, nn, ids[3], t, nn))

    with open(base_path + '.obj', 'w', encoding='ascii', newline='\n') as f:
        f.write('# voxel_kit (MagicaVoxel-style export) : %s\n\n' % (title or name))
        f.write('# group\no %s\n\n' % name)
        f.write('# material\nmtllib %s.mtl\nusemtl %s\n\n' % (name, name))
        f.write('# normals\n')
        for n in _MV_NORMALS:
            f.write('vn %d %d %d\n' % n)
        f.write('\n# texcoords\n')
        for u in tlist:
            f.write('vt %.6f 0.5\n' % u)
        f.write('\n# verts\n')
        for p in vlist:
            f.write('v %s %s %s\n' % tuple(_fmt(x) for x in p))
        f.write('\n# faces\n')
        f.write('\n'.join(flines))
        f.write('\n')
    with open(base_path + '.mtl', 'w', encoding='ascii', newline='\n') as f:
        # 재질 이름은 파일마다 다르다 — 모두 "palette" 이면 재질을 밖으로 빼는 임포트 설정에서 서로 덮어쓴다
        f.write('# voxel_kit\n\nnewmtl %s\nillum 1\nKa 0.000 0.000 0.000\n'
                'Kd 1.000 1.000 1.000\nKs 0.000 0.000 0.000\nmap_Kd %s.png\n' % (name, name))
    pal.png().save(base_path + '.png')
    used = set(np.unique(grid.v)) - {0}
    emits = any(pal.emit[i] for i in used)
    if emits:
        pal.emit_png().save(base_path + '_Emit.png')
    return len(flines), emits


def _fmt(x):
    s = ('%.4f' % x).rstrip('0').rstrip('.')
    return '0' if s in ('-0', '') else s


# ---------------------------------------------------------------- render
def srgb2lin(c):
    c = np.asarray(c, float)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def lin2srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def cam_basis(pitch, yaw):
    p, y = math.radians(pitch), math.radians(yaw)
    f = np.array([math.sin(y) * math.cos(p), -math.sin(p), math.cos(y) * math.cos(p)])
    r = np.array([math.cos(y), 0.0, -math.sin(y)])
    u = np.cross(f, r)
    return r, u, f


class Light:
    """ChapterTheme 의 해·그늘·색 필터. euler = Unity 의 해 회전(x 높이, y 방위)."""

    def __init__(self, color=(1, 1, 1), intensity=1.0, euler=(50, -30), ambient=(0.4, 0.4, 0.4),
                 filt=(1, 1, 1), shadows=True):
        self.color = np.array(color, float)
        self.intensity = intensity
        x, y = math.radians(euler[0]), math.radians(euler[1])
        fwd = np.array([math.sin(y) * math.cos(x), -math.sin(x), math.cos(y) * math.cos(x)])
        self.fwd = fwd / np.linalg.norm(fwd)
        self.L = -self.fwd
        self.ambient = np.array(ambient, float)
        self.filt = np.array(filt, float)
        self.shadows = shadows


def _raster(faces, r, u, f, scale, x0, y0, W, H, want_id=True):
    """정사영 래스터. faces: list of (P 4x3, n). 반환 zbuf, id."""
    zb = np.full((H, W), np.inf)
    fid = np.full((H, W), -1, np.int32)
    for k, (P, n) in enumerate(faces):
        nf = float(np.dot(n, f))
        if nf >= -1e-9:
            continue    # 등을 돌렸거나 옆으로 선 면
        sx = (P @ r - x0) * scale
        sy = (y0 - P @ u) * scale
        ix0 = max(int(math.floor(sx.min())), 0)
        ix1 = min(int(math.ceil(sx.max())), W)
        iy0 = max(int(math.floor(sy.min())), 0)
        iy1 = min(int(math.ceil(sy.max())), H)
        if ix0 >= ix1 or iy0 >= iy1:
            continue
        xs = np.arange(ix0, ix1) + 0.5
        ys = np.arange(iy0, iy1) + 0.5
        X, Y = np.meshgrid(xs, ys)
        inside = np.ones(X.shape, bool)
        area = 0.0
        for i in range(4):
            j = (i + 1) % 4
            area += sx[i] * sy[j] - sx[j] * sy[i]
        sg = 1.0 if area > 0 else -1.0
        for i in range(4):
            j = (i + 1) % 4
            e = (sx[j] - sx[i]) * (Y - sy[i]) - (sy[j] - sy[i]) * (X - sx[i])
            inside &= (e * sg) >= -1e-7
        if not inside.any():
            continue
        # 깊이: 평면 n.p = c, p = wx r + wy u + d f
        c = float(np.dot(n, P[0]))
        wx = X / scale + x0
        wy = y0 - Y / scale
        d = (c - wx * np.dot(n, r) - wy * np.dot(n, u)) / nf
        sub = zb[iy0:iy1, ix0:ix1]
        win = inside & (d < sub)
        sub[win] = d[win]
        if want_id:
            fid[iy0:iy1, ix0:ix1][win] = k
    return zb, fid


def render(items, light=None, pitch=50.0, yaw=0.0, px_per_unit=60.0, floor=None, floor_y=2.0,
           margin=0.3, bg=(24, 22, 28), bounds=None, floor_tint=(1, 1, 1)):
    """items: [(grid, offset(모델 좌표), Palette)]. floor: (H,W,3) 바닥 타일(0~255) 또는 None.
    한 칸 = 3.2 모델 유닛, 바닥 타일 32px = 한 칸."""
    light = light or Light()
    r, u, f = cam_basis(pitch, yaw)
    faces, cols, emis = [], [], []
    for grid, off, pal in items:
        off = np.asarray(off, float)
        for P, n, col in quads(grid, greedy=True, cull_bottom=False):
            faces.append((P + off, n))
            cols.append(pal.rgba[col, :3] / 255.0)
            emis.append(bool(pal.emit[col]))
    if not faces:
        raise ValueError('empty scene')
    allP = np.concatenate([P for P, _ in faces])
    if bounds is None:
        lo, hi = allP.min(0), allP.max(0)
        if floor is not None:
            lo[1] = min(lo[1], floor_y)
    else:
        lo, hi = np.asarray(bounds[0], float), np.asarray(bounds[1], float)
    corners = np.array([[x, y, z] for x in (lo[0], hi[0]) for y in (lo[1], hi[1]) for z in (lo[2], hi[2])])
    sxs, sys_ = corners @ r, corners @ u
    x0, x1 = sxs.min() - margin, sxs.max() + margin
    y1, y0 = sys_.min() - margin, sys_.max() + margin
    W = int(math.ceil((x1 - x0) * px_per_unit))
    H = int(math.ceil((y0 - y1) * px_per_unit))
    zb, fid = _raster(faces, r, u, f, px_per_unit, x0, y0, W, H)

    # 화소의 월드 위치
    X, Y = np.meshgrid(np.arange(W) + 0.5, np.arange(H) + 0.5)
    wx = X / px_per_unit + x0
    wy = y0 - Y / px_per_unit
    N = np.zeros((H, W, 3))
    A = np.zeros((H, W, 3))
    E = np.zeros((H, W), bool)
    hit = fid >= 0
    nrm = np.array([n for _, n in faces])
    colarr = np.array(cols)
    emarr = np.array(emis)
    N[hit] = nrm[fid[hit]]
    A[hit] = colarr[fid[hit]]
    E[hit] = emarr[fid[hit]]
    depth = zb.copy()
    # 바닥 평면
    if floor is not None:
        t = (floor_y - wx * r[1] - wy * u[1]) / f[1]
        P = wx[..., None] * r + wy[..., None] * u + t[..., None] * f
        fl = t < depth
        th, tw = floor.shape[:2]
        tx = np.floor((P[..., 0] / 3.2 + 0.5) * tw).astype(int) % tw   # 칸 가운데가 원점
        tz = np.floor((-P[..., 2] / 3.2 + 0.5) * th).astype(int) % th
        tex = floor[tz, tx] / 255.0 * np.asarray(floor_tint)
        A[fl] = tex[fl]
        N[fl] = (0, 1, 0)
        E[fl] = False
        depth = np.where(fl, t, depth)
        hit = hit | fl
    depth = np.where(hit, depth, 0.0)
    Pw = wx[..., None] * r + wy[..., None] * u + depth[..., None] * f

    # 그림자 지도
    shadow = np.zeros((H, W), bool)
    if light.shadows:
        lf = light.fwd
        up = np.array([0, 1, 0.0]) if abs(lf[1]) < 0.99 else np.array([0, 0, 1.0])
        lr = np.cross(up, lf)
        lr /= np.linalg.norm(lr)
        lu = np.cross(lf, lr)
        sp = px_per_unit * 1.5
        pr, pu = allP @ lr, allP @ lu
        if floor is not None:
            pr = np.concatenate([pr, (Pw[hit] @ lr)])
            pu = np.concatenate([pu, (Pw[hit] @ lu)])
        lx0, lx1 = pr.min() - 0.2, pr.max() + 0.2
        ly1, ly0 = pu.min() - 0.2, pu.max() + 0.2
        LW = int(math.ceil((lx1 - lx0) * sp))
        LH = int(math.ceil((ly0 - ly1) * sp))
        if LW * LH < 40_000_000:
            szb, _ = _raster(faces, lr, lu, lf, sp, lx0, ly0, LW, LH, want_id=False)
            qx = ((Pw @ lr - lx0) * sp).astype(int)
            qy = ((ly0 - Pw @ lu) * sp).astype(int)
            ok = hit & (qx >= 0) & (qx < LW) & (qy >= 0) & (qy < LH)
            dl = Pw @ lf
            bias = 0.06
            sd = np.full((H, W), np.inf)
            sd[ok] = szb[qy[ok], qx[ok]]
            shadow = ok & (dl > sd + bias)
    ndl = np.clip((N * light.L).sum(-1), 0, None)
    ndl[shadow] = 0
    alb = srgb2lin(A)
    lit = alb * (srgb2lin(light.ambient) + srgb2lin(light.color) * light.intensity * ndl[..., None])
    lit = np.where(E[..., None], np.maximum(lit, alb * 1.0), lit)
    lit *= srgb2lin(light.filt)
    img = (lin2srgb(lit) * 255).astype(np.uint8)
    out = np.empty((H, W, 3), np.uint8)
    out[:] = bg
    out[hit] = img[hit]
    return Image.fromarray(out, 'RGB')


# ================================================================ lib: 벽·소품을 짓는 도구
# kitlib — 벽·소품을 짓는 공용 도구 (복셀 칸 번호 공간).
#
# 벽 격자: (32, 34, 32), 원점 (-1.6, 2.0, -1.6).
#   몸통 x/z 1..30 (모델 -1.5..1.5), y 0..29 (모델 2.0..5.0). y 30..32 = 위로 0.3 까지의 장식.
#   x/z 0 과 31 은 발자국 밖 0.1 — 윗면 덮개만 거기까지 나온다(몸통 사이 0.2 틈 위로 이웃 덮개와 맞닿는다).
#   남쪽(-z, 카메라를 보는 면) = z 1, 북쪽 = z 30, 왼쪽(-x) = x 1, 오른쪽 = x 30.
GX, GY, GZ = 32, 34, 32
WALL_ORIGIN = (-1.6, 2.0, -1.6)
B0, B1 = 1, 31          # 몸통 x/z 범위 (slice)
H = 30                  # 몸통 높이 (y 0..29)
PERIM = 120


def wall_grid():
    return Grid((GX, GY, GZ), WALL_ORIGIN)


# 소품 자리 (MeshyArtImport.Rule 이 재는 상자, 복셀 0.1 그대로 들어가는 크기)
FLOOR_MAX = (19, 8, 19)     # 걷는 칸: 화로 발자국 x 발목 높이 x 화로 발자국
HANG_MAX = (20, 20)         # 벽에 거는 것: 옛 사슬의 긴 변
LIGHT_MAX = (12, 20)        # 벽에 거는 불: 옛 횃불(불꽃 그림 포함) 안
BIG_MAX = (32, 30, 32)      # 큰 소품: 벽 한 칸


def prop_grid(w, h, d, kind):
    """소품 격자. kind: 'floor'·'big' = 바닥(y 0)에 서고 x/z 가운데,
    'hang' = 벽에 건다: x 가운데, 아래끝 y 2.0(옛 횃불처럼), 등(벽에 붙는 면)이 z 0 — 남쪽(-z)으로 나온다."""
    if kind in ('floor', 'big'):
        return Grid((w, h, d), (-w * VOX / 2, 0.0, -d * VOX / 2))
    return Grid((w, h, d), (-w * VOX / 2, 2.0, -d * VOX))


def check_fit(g, kind, name=''):
    """자리 상자 안에 드는지 — 넘으면 들일 때 줄어 복셀 크기가 벽과 달라진다."""
    lo, hi = g.bounds()
    size = np.round((hi - lo) / VOX).astype(int)
    lim = {'floor': FLOOR_MAX, 'big': BIG_MAX}.get(kind)
    if kind == 'hang':
        lim = (HANG_MAX[0], HANG_MAX[1], 99)
    if kind == 'light':
        lim = (LIGHT_MAX[0], LIGHT_MAX[1], 99)
    bad = [a for a in range(3) if size[a] > lim[a]]
    if bad:
        raise ValueError('%s: %s 가 %s 상자 %s 를 넘는다' % (name, tuple(size), kind, lim))
    return tuple(size)


def R(seed):
    return np.random.default_rng(seed)


# ---------------------------------------------------------------- noise
def noise2(shape, cell, rng, octaves=2, persist=0.5):
    """부드러운 값 잡음 [0,1]. cell = 격자 한 칸의 크기(복셀)."""
    out = np.zeros(shape)
    amp, tot = 1.0, 0.0
    for o in range(octaves):
        c = max(1.0, cell / (2 ** o))
        gw = int(math.ceil(shape[0] / c)) + 2
        gh = int(math.ceil(shape[1] / c)) + 2
        lat = rng.random((gw, gh))
        xs = np.arange(shape[0]) / c
        ys = np.arange(shape[1]) / c
        x0 = np.floor(xs).astype(int)
        y0 = np.floor(ys).astype(int)
        fx = xs - x0
        fy = ys - y0
        fx = fx * fx * (3 - 2 * fx)
        fy = fy * fy * (3 - 2 * fy)
        a = lat[x0][:, y0]
        b = lat[x0 + 1][:, y0]
        c_ = lat[x0][:, y0 + 1]
        d = lat[x0 + 1][:, y0 + 1]
        top = a + (b - a) * fx[:, None]
        bot = c_ + (d - c_) * fx[:, None]
        out += amp * (top + (bot - top) * fy[None, :])
        tot += amp
        amp *= persist
    return out / tot


def pnoise2(size, cell, rng, octaves=2, persist=0.5):
    """주기 size 로 반복되는 값 잡음 (size x size) — 벽 윗면이 이웃 벽과 이음매 없이 이어지게."""
    out = np.zeros((size, size))
    amp, tot = 1.0, 0.0
    for o in range(octaves):
        n = max(1, int(round(size / max(1.0, cell / (2 ** o)))))
        lat = rng.random((n, n))
        c = size / n
        xs = np.arange(size) / c
        x0 = np.floor(xs).astype(int)
        fx = xs - x0
        fx = fx * fx * (3 - 2 * fx)
        a = lat[x0 % n][:, x0 % n]
        b = lat[(x0 + 1) % n][:, x0 % n]
        c_ = lat[x0 % n][:, (x0 + 1) % n]
        d = lat[(x0 + 1) % n][:, (x0 + 1) % n]
        top = a + (b - a) * fx[:, None]
        bot = c_ + (d - c_) * fx[:, None]
        out += amp * (top + (bot - top) * fx[None, :])
        tot += amp
        amp *= persist
    return out / tot


TOPW = 32   # 윗면 덮개는 발자국 밖 한 칸씩(0..31) — 이웃 벽 덮개와 맞닿아 이음매가 없다


def edge_weight(size=TOPW, inner=2, outer=8):
    """가장자리 띠는 1(모든 변형이 같은 무늬), 안쪽은 0(변형마다 다른 무늬)."""
    i = np.arange(size)
    d1 = np.minimum(i, size - 1 - i)
    d = np.minimum(d1[:, None], d1[None, :]).astype(float)
    return np.clip((outer - d) / (outer - inner), 0, 1)


def top_field(shared_seed, own_seed, cell, octaves=2):
    """이웃과 맞물리는 윗면 잡음: 가장자리는 공용 무늬, 안쪽은 변형마다 제 무늬."""
    a = pnoise2(TOPW, cell, R(shared_seed), octaves)
    if own_seed is None or own_seed == shared_seed:
        return a
    b = pnoise2(TOPW, cell, R(own_seed), octaves)
    w = edge_weight()
    return a * w + b * (1 - w)


# ---------------------------------------------------------------- shell coordinates
def shell_coords():
    """몸통 칸마다 (d, face, s): d = 가장 가까운 옆면까지의 거리(0 = 겉칸),
    face 0 남 1 동 2 북 3 서, s = 둘레를 따라 잰 위치(0..119, 남쪽 왼끝에서 시계 반대 방향… 정확히는
    남쪽 왼→오, 동쪽 남→북, 북쪽 오→왼, 서쪽 북→남)."""
    x = np.arange(GX)[:, None] * np.ones((1, GZ), int)
    z = np.arange(GZ)[None, :] * np.ones((GX, 1), int)
    dS, dE, dN, dW = z - 1, 30 - x, 30 - z, x - 1
    D = np.stack([dS, dE, dN, dW])
    face = np.argmin(D, axis=0)
    d = D.min(0)
    s = np.where(face == 0, x - 1,
        np.where(face == 1, 30 + (z - 1),
        np.where(face == 2, 60 + (30 - x), 90 + (30 - z))))
    return d, face, s


SHELL_D, SHELL_FACE, SHELL_S = shell_coords()
IN_BODY = np.zeros((GX, GZ), bool)
IN_BODY[B0:B1, B0:B1] = True


def face_cells(face, depth=0):
    """그 옆면의 (x, z) 칸들 — 모서리 포함, 지정 깊이."""
    if face == 0:
        return [(x, B0 + depth) for x in range(B0 + depth, B1 - depth)]
    if face == 2:
        return [(x, B1 - 1 - depth) for x in range(B0 + depth, B1 - depth)]
    if face == 1:
        return [(B1 - 1 - depth, z) for z in range(B0 + depth, B1 - depth)]
    return [(B0 + depth, z) for z in range(B0 + depth, B1 - depth)]


def outward(face):
    return {0: (0, -1), 1: (1, 0), 2: (0, 1), 3: (-1, 0)}[face]


# ---------------------------------------------------------------- masonry
def courses_from(heights):
    ys, y = [], 0
    for h in heights:
        ys.append((y, y + h))
        y += h
    assert y == H, (heights, y)
    return ys


def lay_bricks(g, rng, courses, lengths, tones, mortar, recess=1, sunk=0.05, tone_w=None, faces=(0, 1, 2, 3)):
    """몸통을 벽돌로 채운다(속까지 채워진 덩어리). 줄눈은 겉에서 recess 칸 들어간다.
    courses: [(y0, y1)], lengths: 벽돌 길이 후보(줄눈 1 포함), tones: 벽돌 색 후보, mortar: 줄눈 색.
    faces: 줄눈을 파는 옆면 — 카메라는 남쪽 면만 본다(나머지는 칠만 해서 삼각형을 아낀다).
    반환: brick_id 배열 (GX, GY, GZ) — 칸마다 (줄*1000 + 번호), 줄눈은 -1, 몸통 밖은 -2."""
    v = g.v
    bid = np.full((GX, GY, GZ), -2, np.int32)
    tone_w = tone_w if tone_w is not None else [1.0] * len(tones)
    tone_p = np.array(tone_w, float) / sum(tone_w)
    for ci, (y0, y1) in enumerate(courses):
        cuts = []
        pos = rng.integers(0, max(lengths))
        while pos < PERIM + max(lengths):
            cuts.append(pos % PERIM)
            pos += int(rng.choice(lengths))
        cuts = sorted(set(cuts))
        # s -> 벽돌 번호
        idx = np.zeros(PERIM, int)
        joint = np.zeros(PERIM, bool)
        joint[cuts] = True
        k = 0
        start = cuts[0]
        for t in range(PERIM):
            sp = (start + t) % PERIM
            if joint[sp] and t > 0:
                k += 1
            idx[sp] = k
        colors = rng.choice(len(tones), size=k + 2, p=tone_p)
        for yy in range(y0, y1):
            is_hjoint = (yy == y1 - 1) and ci < len(courses) - 1
            for x in range(B0, B1):
                for z in range(B0, B1):
                    s = SHELL_S[x, z]
                    if is_hjoint or joint[s]:
                        bid[x, yy, z] = -1
                        v[x, yy, z] = mortar
                    else:
                        bid[x, yy, z] = ci * 1000 + idx[s]
                        v[x, yy, z] = tones[colors[idx[s]]]
    # 줄눈을 겉에서 파낸다
    if recess:
        for x in range(B0, B1):
            for z in range(B0, B1):
                d = SHELL_D[x, z]
                if d < recess and SHELL_FACE[x, z] in faces:
                    col = bid[x, 0:H, z] == -1
                    v[x, 0:H, z][col] = 0
    # 몇 장은 한 칸 들어가 있다(닳은 벽)
    if sunk > 0:
        ids = np.unique(bid[bid >= 0])
        pick = ids[rng.random(len(ids)) < sunk]
        for b in pick:
            m = (bid == b)
            m &= (SHELL_D[:, None, :] == 0)
            v[m] = 0
    return bid


def bevel_blocks(g, bid, ramp, faces=(0,), up=1, down=-1):
    """다듬은 돌의 모서리: 겉면에서 벽돌의 맨 윗줄은 한 단 밝게, 맨 아랫줄은 한 단 어둡게."""
    rmap = {c: i for i, c in enumerate(ramp)}
    for x in range(B0, B1):
        for z in range(B0, B1):
            if SHELL_D[x, z] != 0 or SHELL_FACE[x, z] not in faces:
                continue
            for y in range(H):
                b = bid[x, y, z]
                c = g.v[x, y, z]
                if b < 0 or c not in rmap:
                    continue
                above = bid[x, y + 1, z] if y + 1 < GY else -9
                below = bid[x, y - 1, z] if y > 0 else -9
                i = rmap[c]
                if above != b:
                    g.v[x, y, z] = ramp[min(len(ramp) - 1, max(0, i + up))]
                elif below != b and y > 0:
                    g.v[x, y, z] = ramp[min(len(ramp) - 1, max(0, i + down))]


def top_y(g, x, z):
    col = np.nonzero(g.v[x, :, z])[0]
    return int(col.max()) if len(col) else -1


def heightmap(g):
    hm = np.full((GX, GZ), -1, int)
    solid = g.v > 0
    any_ = solid.any(axis=1)
    rev = solid[:, ::-1, :]
    top = GY - 1 - np.argmax(rev, axis=1)
    hm[any_] = top[any_]
    return hm


def surface_paint_down(g, x, z, face, y_from, length, color_fn):
    """옆면을 따라 위에서 아래로 칠한다(이끼·물 얼룩·재 흘러내림). 빈칸(줄눈 홈)이면 한 칸 안쪽을 칠한다."""
    ox, oz = outward(face)
    painted = 0
    y = y_from
    while painted < length and y >= 0:
        cx, cz = x, z
        hit = False
        for k in range(3):
            if 0 <= cx < GX and 0 <= cz < GZ and g.v[cx, y, cz]:
                g.v[cx, y, cz] = color_fn(cx, y, cz, painted)
                hit = True
                break
            cx -= ox
            cz -= oz
        if not hit:
            break
        painted += 1
        y -= 1
    return painted


def blob_mask(shape, rng, cover=0.5, cell=7, octaves=2):
    n = noise2(shape, cell, rng, octaves)
    thr = np.quantile(n, 1 - cover)
    return n >= thr, n


def box(g, x0, x1, y0, y1, z0, z1, col):
    """[x0,x1) 등 반열린 범위를 채운다. col=0 이면 비운다."""
    g.v[max(x0, 0):max(x1, 0), max(y0, 0):max(y1, 0), max(z0, 0):max(z1, 0)] = col


def put(g, x, y, z, col):
    if 0 <= x < g.v.shape[0] and 0 <= y < g.v.shape[1] and 0 <= z < g.v.shape[2]:
        g.v[x, y, z] = col


def sphere_mask(shape, c, r):
    X, Y, Z = np.ogrid[:shape[0], :shape[1], :shape[2]]
    return ((X - c[0]) ** 2 + (Y - c[1]) ** 2 + (Z - c[2]) ** 2) <= r * r


def cap_slab(g, y0, y1, colfn, overhang=1):
    """덮개(y0..y1-1)를 발자국 밖 overhang 칸까지 깐다. colfn(x, z, y) -> 색 (x/z 는 0..31)."""
    lo, hi = B0 - overhang, B1 + overhang
    for x in range(lo, hi):
        for z in range(lo, hi):
            for y in range(y0, y1):
                g.v[x, y, z] = colfn(x, z, y)


def streaks(g, rng, face, count, length, width, colfn, y_from=H - 1, xs=None):
    """옆면 위쪽 끝에서 아래로 흘러내린 줄무늬(물 얼룩·재·녹). colfn(i, n) i = 위에서 몇 칸째, n = 그 줄의 길이."""
    cells = face_cells(face)
    picks = xs if xs is not None else sorted(rng.choice(len(cells), size=count, replace=False))
    for k in picks:
        w = int(rng.integers(width[0], width[1] + 1))
        n = int(rng.integers(length[0], length[1] + 1))
        for j in range(w):
            if k + j >= len(cells):
                break
            x, z = cells[k + j]
            nj = max(2, n - abs(j - (w - 1) / 2.0) * 3 - int(rng.integers(0, 3)))
            surface_paint_down(g, x, z, face, y_from, int(nj), lambda X, Y, Z, i, nj=nj: colfn(i, nj))


def crumble(g, bid, courses, steps, above=True):
    """귀퉁이를 계단식으로 무너뜨린다. steps: [(줄 번호, inside(x, z) -> bool)].
    겉칸(둘레 두 겹)은 벽돌 단위로 지운다 — 벽돌의 겉칸 중심이 영역 안이면 그 벽돌째. 속은 영역 그대로.
    above: 그 줄보다 위(장식층 포함)도 같은 영역으로 지운다."""
    X, Z = np.meshgrid(np.arange(GX), np.arange(GZ), indexing='ij')
    for ci, inside in steps:
        y0, y1 = courses[ci]
        ytop = GY if above and ci == len(courses) - 1 else y1
        mask2 = np.vectorize(inside)(X, Z)
        # 속
        core = (SHELL_D >= 2) & mask2
        for y in range(y0, ytop):
            g.v[:, y, :][core] = 0
        # 겉: 벽돌째
        shell = (SHELL_D < 2) & IN_BODY
        ids = np.unique(bid[:, y0:y1, :][shell[:, None, :].repeat(y1 - y0, 1)])
        for b in ids:
            if b < 0:
                continue
            m = (bid == b) & (SHELL_D < 2)[:, None, :]
            cells = np.argwhere(m)
            if len(cells) == 0:
                continue
            cx, cz = cells[:, 0].mean(), cells[:, 2].mean()
            if inside(cx, cz):
                g.v[m] = 0
        # 줄눈 칸(벽돌 사이)도 영역 안이면
        for y in range(y0, y1):
            jm = (bid[:, y, :] == -1) & mask2 & (SHELL_D < 2)
            g.v[:, y, :][jm] = 0
        if ytop > y1:
            for y in range(y1, ytop):
                g.v[:, y, :][mask2] = 0
        # 발자국 밖 덮개 턱(0 과 31)도 같은 영역이면 지운다 — 남겨 두면 허공에 테두리만 뜬다
        rim = mask2 & ~IN_BODY
        for y in range(y0, GY if above else y1):
            g.v[:, y, :][rim] = 0
    return g


def fill_break(g, top, colors, slope=0.6, below=2):
    """무너진 자리를 윗면 재료(눈·재)로 메운다: 둘레 윗면(top)보다 낮게, 앞(남쪽)으로 갈수록 더 낮게 비탈진다.
    위에서 보면 윗면이 앞 가장자리에서 꺼진 것으로만 읽힌다 — 깨진 돌·벽돌이 위로 드러나면
    엔진 미리보기에서 눈밭의 비석·재 위의 검은 ㄷ 자처럼 읽혔다. colors = (속, 겉)."""
    hm = heightmap(g)
    for x in range(GX):
        for z in range(GZ):
            y = hm[x, z]
            if y < 0 or y >= top - 1:
                continue
            fill = min(top - below, y + 1 + int(2 + max(0, z - B0) * slope))
            for yy in range(y + 1, fill + 1):
                g.v[x, yy, z] = colors[1] if yy == fill else colors[0]


def grounded(g, keep=None):
    """y=0 바닥에 6방향으로 이어진 칸만 True. keep(마스크)은 떠 있어도 남긴다."""
    s = g.v > 0
    reach = np.zeros_like(s)
    reach[:, 0, :] = s[:, 0, :]
    while True:
        p = np.pad(reach, 1)
        grow = (p[2:, 1:-1, 1:-1] | p[:-2, 1:-1, 1:-1] | p[1:-1, 2:, 1:-1] | p[1:-1, :-2, 1:-1]
                | p[1:-1, 1:-1, 2:] | p[1:-1, 1:-1, :-2] | reach) & s
        if (grow == reach).all():
            break
        reach = grow
    if keep is not None:
        reach |= keep & s
    return reach


def drop_floating(g, keep=None):
    g.v[~grounded(g, keep)] = 0
    return g


def exposed(g):
    """겉으로 드러난 칸(6방향 중 하나라도 비어 있음)."""
    s = g.v > 0
    p = np.pad(s, 1)
    nb = (p[2:, 1:-1, 1:-1] & p[:-2, 1:-1, 1:-1] & p[1:-1, 2:, 1:-1] & p[1:-1, :-2, 1:-1]
          & p[1:-1, 1:-1, 2:] & p[1:-1, 1:-1, :-2])
    return s & ~nb


# ================================================================ view: 챕터 조명·미리보기
# kitview — 미리보기 도우미: 챕터 조명, 바닥 타일, 한 장짜리 시트.
FLOOR_PNG = os.path.join(ROOT or '.', 'Assets', 'Resources', 'Sprites', '00', 'FloorTile_99_999.png')


# ChapterTheme.cs 와 MapBuilder.ChapterTint 의 값 그대로
THEMES = {
    0: dict(color=(1.00, 0.96, 0.88), intensity=1.10, euler=(50, -30), ambient=(0.42, 0.44, 0.40),
            filt=(1.00, 0.91, 0.81), tint=(1.00, 1.00, 1.00)),
    1: dict(color=(0.62, 0.80, 1.00), intensity=0.85, euler=(38, 20), ambient=(0.30, 0.38, 0.46),
            filt=(0.78, 0.90, 1.00), tint=(0.72, 0.86, 1.00)),
    2: dict(color=(1.00, 0.68, 0.45), intensity=1.05, euler=(62, -10), ambient=(0.44, 0.28, 0.20),
            filt=(1.00, 0.80, 0.62), tint=(1.00, 0.72, 0.55)),
    3: dict(color=(0.75, 0.92, 1.00), intensity=0.80, euler=(22, 40), ambient=(0.32, 0.40, 0.48),
            filt=(0.86, 0.94, 1.00), tint=(0.78, 0.94, 1.00)),
    4: dict(color=(0.62, 0.50, 0.88), intensity=0.70, euler=(14, 70), ambient=(0.26, 0.22, 0.34),
            filt=(0.82, 0.72, 1.00), tint=(0.70, 0.62, 0.90)),
}

GAME_PITCH = 60.0   # 스크린샷에서 잰 값: 칸 세로 간격/가로 간격 = 0.86


def light_for(ch, shadows=True):
    t = THEMES[ch]
    return Light(t['color'], t['intensity'], t['euler'], t['ambient'], t['filt'], shadows)


def floor_tex():
    return np.array(Image.open(FLOOR_PNG).convert('RGB')).astype(float)


def label(img, text, color=(230, 230, 230)):
    d = ImageDraw.Draw(img)
    d.text((4, 2), text, fill=color)
    return img


def hstack(ims, gap=8, bg=(24, 22, 28)):
    w = sum(i.width for i in ims) + gap * (len(ims) - 1)
    h = max(i.height for i in ims)
    out = Image.new('RGB', (w, h), bg)
    x = 0
    for i in ims:
        out.paste(i, (x, 0))
        x += i.width + gap
    return out


def vstack(ims, gap=8, bg=(24, 22, 28)):
    w = max(i.width for i in ims)
    h = sum(i.height for i in ims) + gap * (len(ims) - 1)
    out = Image.new('RGB', (w, h), bg)
    y = 0
    for i in ims:
        out.paste(i, (0, y))
        y += i.height + gap
    return out


def asset_views(grid, pal, ch, ppu=80, floor_y=2.0, with_floor=True):
    """게임 카메라(60도 정면) + 3/4 시점(옆면 확인) + 뒤쪽 3/4."""
    L = light_for(ch)
    fl = floor_tex() if with_floor else None
    tint = THEMES[ch]['tint']
    a = render([(grid, (0, 0, 0), pal)], light=L, pitch=GAME_PITCH, yaw=0, px_per_unit=ppu,
               floor=fl, floor_y=floor_y, floor_tint=tint)
    b = render([(grid, (0, 0, 0), pal)], light=L, pitch=35, yaw=35, px_per_unit=ppu,
               floor=fl, floor_y=floor_y, floor_tint=tint)
    c = render([(grid, (0, 0, 0), pal)], light=L, pitch=35, yaw=-145, px_per_unit=ppu,
               floor=fl, floor_y=floor_y, floor_tint=tint)
    return hstack([a, b, c])


CELL = 3.2


def scene(placements, ch, ppu=30, pitch=GAME_PITCH, yaw=0.0, cells=None, shadows=True):
    """placements: [(grid, pal, (col, row), extra_offset)] — 칸 (col, row) 은 CSV 처럼 오른쪽·아래로 는다.
    벽은 칸 가운데, 모델 좌표 그대로(바닥 = 모델 y 2.0). cells = ((c0, r0), (c1, r1)) 로 화면 범위를 잡는다."""
    items = []
    for grid, pal, (c, r), extra in placements:
        off = np.array([c * CELL, 0.0, -r * CELL]) + np.asarray(extra, float)
        items.append((grid, off, pal))
    bounds = None
    if cells is not None:
        (c0, r0), (c1, r1) = cells
        bounds = ((c0 * CELL - CELL / 2, 2.0, -r1 * CELL - CELL / 2),
                  (c1 * CELL + CELL / 2, 5.4, -r0 * CELL + CELL / 2))
    return render(items, light=light_for(ch, shadows), pitch=pitch, yaw=yaw, px_per_unit=ppu,
                  floor=floor_tex(), floor_y=2.0, floor_tint=THEMES[ch]['tint'], bounds=bounds, margin=0.0)


def tri_count(grid):
    return 2 * len(quads(grid, greedy=True, cull_bottom=True))


# ================================================================ chapter 0
def _chapter0():
    """챕터 0 — 이끼 낀 지하 묘소. 한낮의 무덤: 따뜻한 회갈색 벽돌, 위에서 흘러내리는 이끼."""
    def palette():
        p = Palette()
        p.ramp('brick', ['#4b4038', '#5a4d43', '#695a4e', '#786859', '#867664', '#958571'])
        p.ramp('mortar', ['#2c2622', '#38312b'])
        p.ramp('cap', ['#5f584d', '#6b6357', '#786f62', '#867c6e'])
        p.ramp('moss', ['#283f1d', '#355226', '#43662e', '#557a37', '#6a8f42', '#82a650'])
        p.ramp('wax', ['#8f8466', '#b3a684', '#d3c8a6', '#ebe2c4'])
        p.ramp('flame', ['#ffb43c', '#ffe08a'], emit=True)
        p.ramp('iron', ['#2a292c', '#3a383c', '#4c494d', '#5f5b5e', '#77726f'])
        p.ramp('rust', ['#5a3020', '#7a4126', '#9a542d', '#b8693a'])
        p.ramp('earth', ['#3a2c20', '#4a3929', '#5a4632', '#6b553d'])
        p.ramp('steel', ['#5d646b', '#7b838a', '#9aa2a8', '#bcc3c7', '#dfe4e6'])
        p.ramp('leather', ['#2e1f17', '#432c1f', '#5a3b28'])
        p.ramp('soot', ['#1c1917', '#262220'])
        return p


    P = palette()
    COURSES = courses_from([8, 7, 8, 7])


    TOP_SEED = 1000


    def _base(seed, moss_cover=0.64):
        rng = R(seed)
        g = wall_grid()
        br = P['brick']
        bid = lay_bricks(g, rng, COURSES, lengths=[9, 11, 12, 13], tones=br[1:5], tone_w=[2, 3, 3, 2],
                         mortar=P['mortar'][0], recess=1, sunk=0.06, faces=(0,))
        cap_top(g, seed)
        moss_top(g, seed, cover=moss_cover)
        moss_drips(g, rng)
        return g, rng, bid


    def cap_top(g, seed):
        """덮개돌 한 겹(y 29), 발자국 밖 한 칸까지 덮는다 — 이웃 벽 덮개와 맞닿아 줄이 이어진다."""
        cap = P['cap']
        n = top_field(TOP_SEED + 1, seed + 1, 7)
        for x in range(TOPW):
            for z in range(TOPW):
                g.v[x, H - 1, z] = cap[2] if n[x, z] > 0.56 else cap[1] if n[x, z] > 0.3 else cap[0]


    MOSS_EDGE = 0.64     # 가장자리 띠의 덮임 — 모든 변형이 같아야 이웃과 이음매가 맞는다


    def moss_top(g, seed, cover=MOSS_EDGE, cell=3, mask=None):
        """윗면 이끼 융단 한 겹. 주기 32 의 잡음이라 줄지어 선 벽 위에서 이음매 없이 이어진다.
        한 칸마다 같은 무늬가 반복되므로 큰 덩어리 대신 잘고 고른 얼룩으로 깐다."""
        n = top_field(TOP_SEED, seed, cell, octaves=3)
        shared = pnoise2(TOPW, cell, R(TOP_SEED), 3)
        w = edge_weight()
        thr_map = np.quantile(shared, 1 - MOSS_EDGE) * w + np.quantile(n, 1 - cover) * (1 - w)
        m = n >= thr_map
        if mask is not None:
            m &= mask
        hm = heightmap(g)
        ms = P['moss']
        tone = top_field(TOP_SEED + 7, seed + 7, 3, octaves=1)
        for x, z in zip(*np.nonzero(m)):
            if hm[x, z] < H - 1:
                continue          # 무너진 자리 등에는 따로 얹는다
            y = hm[x, z] + 1
            if y >= GY:
                continue
            edge = n[x, z] < thr_map[x, z] + 0.035
            t = tone[x, z]
            g.v[x, y, z] = ms[1] if edge or t < 0.3 else ms[3] if t > 0.78 else ms[2]
        return m


    def moss_drips(g, rng, prob=0.6, maxlen=12, faces=(0,)):
        """윗면 가장자리 이끼에서 덮개 턱을 넘어 옆면으로 흘러내린다. 남쪽 면이 가장 잘 보인다."""
        ms = P['moss']
        for f in faces:
            cells = face_cells(f)
            ox, oz = outward(f)
            k = 0
            while k < len(cells):
                x, z = cells[k]
                ytop = top_y(g, x, z)
                if ytop >= H and g.v[x, ytop, z] in ms and rng.random() < prob:
                    w = int(rng.integers(1, 4))
                    ln = int(rng.integers(3, maxlen))
                    for j in range(w):
                        if k + j >= len(cells):
                            break
                        cx, cz = cells[k + j]
                        # 턱(발자국 밖 한 칸)부터 덮는다
                        if g.v[cx + ox, H - 1, cz + oz]:
                            g.v[cx + ox, H - 1, cz + oz] = ms[2]
                        l2 = max(2, ln - abs(j - w // 2) * 4 - int(rng.integers(0, 3)))
                        surface_paint_down(g, cx, cz, f, H - 2, l2,
                                           lambda X, Y, Z, i, l2=l2: ms[1] if i >= l2 - 2 else ms[2] if i > 1 else ms[3])
                    k += w + int(rng.integers(1, 4))
                else:
                    k += 1


    def wall_base():
        g, _, _ = _base(1001)
        return g


    def wall_cracked():
        """금 간 벽: 남쪽 면을 가로지르는 굵은 금 하나와 잔금, 깨진 귀퉁이 둘, 금 사이 이끼. 윗면에도 금."""
        g, rng, bid = _base(1002, moss_cover=0.5)
        ms = P['moss']
        # 남쪽 면의 큰 금: 위에서 아래로 갈지자, 두 칸 폭, 깊이 2
        path = [(20, H - 1)]
        x, y = 20, H - 1
        while y > 2:
            y -= 1
            if rng.random() < 0.45:
                x += int(rng.choice([-1, -1, 1]))
            path.append((x, y))
        for i, (x, y) in enumerate(path):
            for dx in (0, 1) if i < len(path) * 0.6 else (0,):
                for dz in (0, 1):
                    if g.v[x + dx, y, B0 + dz]:
                        g.v[x + dx, y, B0 + dz] = 0
                if g.v[x + dx, y, B0 + 2]:
                    g.v[x + dx, y, B0 + 2] = ms[1] if (y % 3 == 0) else P['mortar'][0]
        # 금 위쪽 이끼가 금을 타고 흘러든다
        for (x, y) in path[:9]:
            if g.v[x - 1, y, B0]:
                g.v[x - 1, y, B0] = ms[2]
        # 윗면 금: 남쪽 가장자리에서 북쪽으로
        x, z = 21, B0
        hm = heightmap(g)
        for i in range(18):
            for yy in (H - 1, H):
                if g.v[x, yy, z] and yy >= H - 1:
                    g.v[x, yy, z] = 0
            put(g, x, H - 2, z, ms[1])
            z += 1
            x += int(rng.choice([0, 0, 1, -1]))
        # 깨진 귀퉁이: 남서 위, 남동 아래
        for (cx, cy, cz, r) in ((B0, H - 1, B0, 3.6), (B1 - 1, 6, B0, 3.0)):
            g.v[sphere_mask(g.v.shape, (cx, cy, cz), r)] = 0
        # 귀퉁이에서 떨어진 벽돌 한 조각이 발치에
        box(g, B1 - 6, B1 - 3, 0, 2, 0, 2, P['brick'][3])
        return g


    def wall_niche():
        """벽감 벽: 남쪽 면에 아치 벽감, 그을린 안쪽, 녹아내린 초 한 토막. 윗면은 이끼."""
        g, rng, _ = _base(1003)
        cx, w = 15, 5          # 폭 10
        y0, y1 = 5, 15         # 사각 부분, 그 위 반원
        depth = 8
        def inside(x, y):
            dx = x + 0.5 - cx
            if y0 <= y < y1:
                return abs(dx) < w
            if y >= y1:
                return dx * dx + (y - y1 + 0.5) ** 2 <= w * w
            return False
        def ring(x, y):
            dx = x + 0.5 - cx
            if y0 - 1 <= y < y1:
                return abs(dx) < w + 1.5
            if y >= y1:
                return dx * dx + (y - y1 + 0.5) ** 2 <= (w + 1.6) ** 2
            return False
        for x in range(B0, B1):
            for y in range(0, H):
                if inside(x, y):
                    g.v[x, y, B0:B0 + depth] = 0
                    g.v[x, y, B0 + depth] = P['soot'][1] if (x + 2 * y) % 5 else P['soot'][0]
                elif ring(x, y) and g.v[x, y, B0]:
                    g.v[x, y, B0] = P['cap'][3] if (x * 3 + y) % 4 else P['cap'][2]
                    g.v[x, y, B0 + 1] = P['cap'][1]
        # 그을음이 아치 위로 번진다
        for x in range(cx - 2, cx + 2):
            for y in range(y1 + w + 1, min(H - 1, y1 + w + 4)):
                if g.v[x, y, B0] and (x + y) % 3:
                    g.v[x, y, B0] = P['brick'][0]
        # 바닥 턱(밝은 돌) + 안쪽 바닥
        for x in range(cx - w, cx + w):
            g.v[x, y0 - 1, B0:B0 + depth] = P['cap'][2]
        # 녹은 초: 굵은 토막 3x3, 높이 4, 흘러내린 촛농, 작은 불꽃
        wax = P['wax']
        bx, bz = cx - 1, B0 + 4
        for x in range(bx - 1, bx + 2):
            for z in range(bz - 1, bz + 2):
                for y in range(y0, y0 + 4 - (1 if (x, z) == (bx + 1, bz + 1) else 0)):
                    g.v[x, y, z] = wax[3] if z == bz - 1 else wax[2]
        for (x, z) in ((bx - 2, bz), (bx + 2, bz - 1), (bx, bz - 2), (bx + 1, bz - 2), (bx - 2, bz - 1)):
            g.v[x, y0, z] = wax[1]
        put(g, bx + 2, y0 + 1, bz - 1, wax[2])
        put(g, bx - 1, y0 + 3, bz - 2, wax[2])   # 흘러내린 촛농 줄
        put(g, bx - 1, y0 + 2, bz - 2, wax[2])
        put(g, bx, y0 + 4, bz, P['iron'][0])          # 심지
        put(g, bx, y0 + 5, bz, P['flame'][0])
        put(g, bx, y0 + 6, bz, P['flame'][1])
        return g


    def wall_crumbled():
        """무너진 벽: 남동쪽 위 귀퉁이가 벽돌째 떨어져 나가 계단이 됐다. 헐거운 벽돌, 깨진 턱에 이끼."""
        g, rng, bid = _base(1004, moss_cover=0.6)
        ms, br = P['moss'], P['brick']
        crumble(g, bid, COURSES, [
            (3, lambda x, z: x >= 15 and z <= 18 - (x - 15) * 0.15),
            (2, lambda x, z: x >= 22 and z <= 10),
            (1, lambda x, z: x >= 27 and z <= 4),
        ])
        drop_floating(g)
        # 깨진 턱: 밝은 속살 + 군데군데 이끼
        hm = heightmap(g)
        clump, cn = blob_mask((GX, GZ), rng, cover=0.4, cell=4, octaves=1)
        for x in range(B0, B1):
            for z in range(B0, B1):
                y = hm[x, z]
                if 0 <= y < H - 2:
                    g.v[x, y, z] = br[5] if (x * 3 + z) % 5 == 0 else br[4]
                    if clump[x, z]:
                        put(g, x, y + 1, z, ms[3] if cn[x, z] > 0.8 else ms[2])
        # 이끼가 깨진 가장자리를 타고 흘러내린다(남쪽 면)
        for x in range(15, 22):
            surface_paint_down(g, x, B0, 0, hm[x, B0], int(rng.integers(2, 6)),
                               lambda X, Y, Z, i: ms[2] if i < 2 else ms[1])
        # 헐거운 벽돌: 계단 위에 하나는 비스듬히, 하나는 반쯤 걸쳐서
        hm = heightmap(g)
        def lay(x0, x1, z0, z1, h, col):
            top = max(hm[x, z] for x in range(x0, x1) for z in range(z0, z1))
            box(g, x0, x1, top + 1, top + 1 + h, z0, z1, col)
        lay(17, 22, 4, 7, 3, br[3])
        lay(23, 28, 6, 9, 2, br[2])
        lay(26, 29, 2, 5, 2, br[4])
        return g


    WALLS = {'Base': wall_base, 'A': wall_cracked, 'B': wall_niche, 'C': wall_crumbled}


    # ================================================================ 소품
    def prop_light():
        """빛 · 벽 촛대: 무쇠 벽 촛대에 굵은 밀랍 초 셋(녹아 흘러내림), 단순한 불꽃. 벽에 건다."""
        g = prop_grid(12, 17, 5, 'hang')
        ir, wax, fl = P['iron'], P['wax'], P['flame']
        # 등판(벽에 붙는 판): 가운데 4칸 폭, 위는 둥글게
        box(g, 4, 8, 1, 13, 4, 5, ir[2])
        box(g, 5, 7, 13, 14, 4, 5, ir[2])
        box(g, 4, 8, 1, 2, 4, 5, ir[1])
        put(g, 5, 11, 4, ir[4]); put(g, 6, 11, 4, ir[4])          # 못
        # 팔과 받침: 받침은 앞으로 4칸, 테두리 턱
        box(g, 5, 7, 3, 4, 2, 4, ir[1])
        box(g, 0, 12, 4, 5, 0, 4, ir[2])
        for x in range(12):
            put(g, x, 5, 0, ir[3])
        for z in range(0, 4):
            put(g, 0, 5, z, ir[3]); put(g, 11, 5, z, ir[3])
        # 받침 밑 장식 고리
        box(g, 5, 7, 1, 3, 3, 4, ir[1])
        put(g, 5, 0, 3, ir[0]); put(g, 6, 0, 3, ir[0])
        # 초 셋: (x0, 높이) — 굵기 3x3
        for (x0, h, melt) in ((1, 6, (0, 0)), (4, 8, (2, 0)), (8, 4, (0, 2))):
            for x in range(x0, x0 + 3):
                for z in range(0, 3):
                    top = 5 + h
                    if (x - x0, z) == melt:
                        top -= 1
                    for y in range(5, top):
                        lit = (z == 0)
                        g.v[x, y, z] = wax[3] if (lit and x == x0) else wax[2] if lit else wax[1]
                    g.v[x, top - 1, z] = wax[3]
            # 흘러내린 촛농: 앞면에 한 줄, 받침 위에 고인 것
            put(g, x0, 5 + h - 2, 0, wax[3])
            put(g, x0 + 2, 4, 0, wax[2])
            cx, top = x0 + 1, 5 + h
            put(g, cx, top, 1, ir[0])              # 심지
            put(g, cx, top + 1, 1, fl[0])
            put(g, cx, top + 2, 1, fl[1])
            if h >= 6:
                put(g, cx, top + 3, 1, fl[1])
        # 받침 밑으로 늘어진 촛농 고드름
        put(g, 2, 3, 0, wax[2]); put(g, 2, 2, 0, wax[1])
        put(g, 9, 3, 0, wax[2])
        return g


    def _link_face(g, x0, y0, z, col, col2):
        """정면으로 보이는 고리(가로 3, 세로 4, 가운데 비었다)."""
        for x in range(x0, x0 + 3):
            for y in range(y0, y0 + 4):
                if x == x0 + 1 and y0 < y < y0 + 3:
                    continue
                put(g, x, y, z, col if (x + y) % 3 else col2)


    def _link_edge(g, x, y0, z0, col, col2):
        """옆으로 선 고리(정면에선 한 칸 기둥, 깊이 3)."""
        for y in range(y0, y0 + 4):
            for z in range(z0, z0 + 3):
                if z == z0 + 1 and y0 < y < y0 + 3:
                    continue
                put(g, x, y, z, col if (y + z) % 3 else col2)


    def prop_wall():
        """벽 소품 · 이끼 낀 사슬: 벽 고리에 걸린 녹슨 쇠사슬과 벌어진 족쇄, 고리마다 이끼."""
        g = prop_grid(9, 20, 4, 'hang')
        ir, ru, ms = P['iron'], P['rust'], P['moss']
        # 벽 고리: 박힌 쇠(뒤) + 둥근 고리
        box(g, 3, 6, 17, 20, 3, 4, ir[1])
        for (x, y) in ((3, 14), (4, 14), (5, 14), (2, 15), (6, 15), (2, 16), (6, 16), (3, 17), (5, 17)):
            put(g, x, y, 2, ru[3] if (x + y) % 2 else ir[4])
        # 사슬: 위에서 아래로 엇갈린 고리
        _link_edge(g, 4, 11, 1, ru[2], ir[3])
        _link_face(g, 3, 8, 2, ru[3], ru[2])
        _link_edge(g, 4, 5, 1, ru[2], ir[3])
        # 벌어진 족쇄(U, 오른쪽 위가 열렸다)
        for (x, y) in ((2, 4), (6, 4), (1, 3), (7, 3), (1, 2), (7, 2), (1, 1), (2, 0), (3, 0), (4, 0), (5, 0), (6, 0), (7, 1)):
            put(g, x, y, 2, ir[4] if y > 1 else ru[3])
        put(g, 8, 4, 2, ir[2])                      # 열린 쪽 걸쇠
        put(g, 0, 3, 2, ir[2])                      # 경첩
        # 이끼
        for (x, y, z) in ((3, 14, 2), (4, 14, 2), (2, 15, 2), (3, 8, 2), (4, 10, 1), (1, 1, 2), (2, 0, 2), (4, 7, 1)):
            put(g, x, y, z, ms[3] if y > 9 else ms[2])
        put(g, 3, 13, 2, ms[2])
        return g


    def prop_floor():
        """바닥 · 칼이 꽂힌 무덤 표지: 흙과 돌 무더기에 거꾸로 꽂힌 평범한 쇠칼. 장식도 눈도 없다. 발목 높이."""
        g = prop_grid(17, 8, 15, 'floor')
        ea, st, ir, lt, ms = P['earth'], P['steel'], P['iron'], P['leather'], P['moss']
        cx, cz = 8.5, 7.5
        for x in range(17):
            for z in range(15):
                d = ((x + 0.5 - cx) / 7.6) ** 2 + ((z + 0.5 - cz) / 6.6) ** 2
                if d <= 1.0:
                    g.v[x, 0, z] = ea[1] if d > 0.6 else ea[2]
                if d <= 0.45:
                    g.v[x, 1, z] = ea[3] if (x + z) % 4 else ea[2]
        # 돌 몇 개
        for (x, z, c) in ((3, 4, P['cap'][1]), (12, 3, P['cap'][2]), (14, 9, P['cap'][1]), (4, 11, P['cap'][2]), (9, 12, P['cap'][0])):
            box(g, x, x + 2, 0, 2 if c != P['cap'][0] else 1, z, z + 2, c)
        # 이끼 조금
        for (x, z) in ((6, 9), (7, 9), (11, 6), (10, 10)):
            put(g, x, 2, z, ms[2])
        # 칼: 날 2칸 폭(드러난 날 2칸), 코등이 6칸, 손잡이 2칸, 머리 1칸
        bx = 8
        for y in (2, 3):
            put(g, bx, y, 7, st[3]); put(g, bx + 1, y, 7, st[2])
            put(g, bx, y, 8, st[2]); put(g, bx + 1, y, 8, st[1])
        for x in range(6, 12):
            put(g, x, 4, 7, P['steel'][3] if x in (6, 11) else P['steel'][2])
            put(g, x, 4, 8, P['steel'][1])
        for y in (5, 6):
            put(g, bx, y, 7, lt[2]); put(g, bx + 1, y, 7, lt[1])
            put(g, bx, y, 8, lt[1]); put(g, bx + 1, y, 8, lt[0])
        put(g, bx, 7, 7, P['steel'][3]); put(g, bx + 1, 7, 7, P['steel'][2])
        put(g, bx, 7, 8, P['steel'][2]); put(g, bx + 1, 7, 8, P['steel'][1])
        return g


    def prop_big():
        """바닥(벽 옆 빈칸) · 닫힌 석관: 평평한 뚜껑, 닳은 회갈색 돌, 가장자리를 따라 이끼."""
        g = prop_grid(30, 13, 18, 'big')
        br, cp, ms = P['brick'], P['cap'], P['moss']
        rng = R(1101)
        box(g, 0, 30, 0, 2, 0, 18, br[2])                    # 받침
        box(g, 1, 29, 2, 10, 1, 17, br[3])                   # 몸
        # 앞면 테두리 판(한 칸 들어간 판)
        for x in range(4, 26):
            for y in range(4, 8):
                g.v[x, y, 1] = 0
                g.v[x, y, 2] = br[2]
        box(g, 0, 30, 10, 12, 0, 18, cp[1])                  # 뚜껑
        box(g, 1, 29, 12, 13, 1, 17, cp[2])                  # 뚜껑 윗면(한 칸 안쪽)
        for x in range(1, 29):
            g.v[x, 12, 1] = cp[3]                            # 앞 모서리 밝게
        # 닳음: 모서리 몇 칸 떨어짐, 금 한 줄
        for (x, y, z) in ((0, 11, 0), (29, 10, 0), (29, 11, 17), (0, 1, 0), (1, 9, 1), (28, 8, 1)):
            g.v[x, y, z] = 0
        for y in range(2, 10):
            x = 17 + (y % 3 == 0)
            g.v[x, y, 1] = br[0]
        # 이끼: 뚜껑 가장자리와 받침 위를 따라
        for x in range(0, 30):
            for z in range(0, 18):
                edge = x in (0, 1, 28, 29) or z in (0, 1, 16, 17)
                if edge and rng.random() < 0.45:
                    y = 12 if 1 <= x <= 28 and 1 <= z <= 16 else 11
                    put(g, x, y + 1 if y == 12 else 12 if g.v[x, 12, z] == 0 else 13, z, ms[2] if rng.random() < 0.6 else ms[3])
        for x in range(0, 30):
            if rng.random() < 0.5:
                g.v[x, 1, 0] = ms[2]
                if rng.random() < 0.4:
                    g.v[x, 2, 0] = ms[1] if g.v[x, 2, 0] else 0
        # 뚜껑 앞 모서리에서 흘러내린 이끼
        for x in (3, 4, 11, 22, 23, 24):
            for k in range(1, 3 + (x % 2)):
                if g.v[x, 11 - k + 1, 1]:
                    g.v[x, 11 - k + 1, 1] = ms[2]
            g.v[x, 11, 0] = ms[3]
        return g


    PROPS = {'Light': (prop_light, 'light'), 'Wall': (prop_wall, 'hang'),
             'Floor': (prop_floor, 'floor'), 'Big': (prop_big, 'big')}

    return types.SimpleNamespace(P=P, WALLS=WALLS, PROPS=PROPS)


# ================================================================ chapter 1
def _chapter1():
    """챕터 1 — 무너진 수로. 흐린 오후의 물길: 젖은 청회색 돌, 위에서 흘러내린 물 얼룩, 녹.
    윗면은 번들거리지 않는 밝은 돌색 — 어두운 바닥과 갈라져 보여야 한다."""
    def palette():
        p = Palette()
        p.ramp('stone', ['#38443f', '#43504a', '#4f5c55', '#5b6861', '#68746d', '#77827a'])
        p.ramp('wet', ['#232c2a', '#2a3532', '#313f3a', '#3a4a3c'])
        p.ramp('mortar', ['#20272c', '#2a3238'])
        p.ramp('cope', ['#8a8478', '#989286', '#a6a094', '#b4aea1', '#c2bcae'])
        p.ramp('iron', ['#202326', '#2c3034', '#3a3e43', '#4a4f54'])
        p.ramp('rust', ['#55301f', '#723f26', '#8f522e', '#ab6838'])
        p.ramp('dark', ['#10161b', '#18222a', '#203039'])
        p.ramp('algae', ['#2f4a3a', '#3b5c45', '#4b6e50'])
        p.ramp('wood', ['#3e3026', '#4e3d30', '#5f4b3a', '#715a45', '#8a6f55'])
        p.ramp('rope', ['#7a5f38', '#957548', '#ae8c5a', '#c6a46e'])
        p.ramp('glass', ['#5d8fb5', '#86b8da', '#b4dcf2'], emit=True)
        p.ramp('water', ['#16222b', '#1d2d38', '#26404d'])
        return p


    P = palette()
    COURSES = courses_from([10, 10, 10])
    TOP_SEED = 2000


    def _base(seed, stains=7):
        rng = R(seed)
        g = wall_grid()
        st = P['stone']
        bid = lay_bricks(g, rng, COURSES, lengths=[13, 15, 16, 17], tones=st[1:5], tone_w=[2, 3, 3, 2],
                         mortar=P['mortar'][0], recess=1, sunk=0.0, faces=(0,))
        bevel_blocks(g, bid, st)
        coping(g, seed)
        wet_base(g, rng)
        water_stains(g, rng, stains)
        return g, rng, bid


    def coping(g, seed):
        """덮개돌 두 겹(y 28·29), 발자국 밖 한 칸까지. 밝고 무광, 젖은 얼룩은 한 단 어둡게.
        덮개돌 이음매는 칸 경계(x·z = 31) 한 줄 — 수로 덮개는 한 장씩 얹는다."""
        cp = P['cope']
        damp = top_field(TOP_SEED, seed, 9, octaves=2)
        thr = np.quantile(pnoise2(TOPW, 9, R(TOP_SEED), 2), 0.62)

        wetf = top_field(TOP_SEED + 2, seed + 2, 4, octaves=2)
        thr2 = np.quantile(pnoise2(TOPW, 4, R(TOP_SEED + 2), 2), 0.86)

        def col(x, z, y):
            if y == H - 1:
                if x == TOPW - 1 or z == TOPW - 1:
                    return cp[1]                      # 덮개돌 이음매
                if damp[x, z] > thr:
                    return cp[2]                      # 젖어 한 단 어두운 자리
                return cp[3] if (x * 5 + z * 3) % 17 else cp[4]
            return cp[1]          # 아래 겹: 덮개 옆면(턱)은 한 단 어둡다
        cap_slab(g, H - 2, H, col)
        # 남쪽 턱의 윗줄은 밝게 — 바닥과 갈라지는 선
        for x in range(TOPW):
            if g.v[x, H - 1, 0] not in (cp[1], cp[2]):
                g.v[x, H - 1, 0] = cp[4]


    def wet_base(g, rng):
        """발치는 늘 젖어 있다: 맨 아래 2~4칸이 한 단 어둡다(남쪽 면)."""
        st = P['stone']
        rmap = {c: i for i, c in enumerate(st)}
        hgt = noise2((GX, 1), 5, rng, 1)[:, 0]
        for x in range(B0, B1):
            top = 2 + int(hgt[x] * 3)
            for y in range(0, top):
                for z in (B0, B0 + 1):
                    c = g.v[x, y, z]
                    if c in rmap:
                        g.v[x, y, z] = st[max(0, rmap[c] - 2)]


    def water_stains(g, rng, count=7, xs=None, length=(6, 22)):
        """덮개 턱에서 아래로 흘러내린 어두운 물 얼룩. 위쪽은 이끼빛이 살짝 돈다."""
        wet, al = P['wet'], P['algae']

        def col(i, n):
            if i < 2:
                return al[0]
            return wet[2] if i < n * 0.35 else wet[1] if i < n * 0.8 else wet[2]
        streaks(g, rng, 0, count, length, (1, 3), col, y_from=H - 3, xs=xs)
        # 덮개 턱에도 같은 자리에 얼룩
        cells = face_cells(0)
        for x in range(TOPW):
            below = g.v[x, H - 3, B0] if B0 <= x < B1 else 0
            if below in (al[0],):
                g.v[x, H - 2, 0] = P['cope'][0]


    def rust_streaks(g, rng, count=3):
        """쇠붙이(꺾쇠)에서 흘러내린 녹물 — 짧고 주황빛. 수로의 녹, 심층의 서리와 갈라지는 색."""
        ru = P['rust']
        cells = face_cells(0)
        for k in rng.choice(len(cells) - 2, size=count, replace=False):
            x, z = cells[k]
            y0 = int(rng.integers(12, H - 4))
            g.v[x, y0, B0] = P['iron'][2]                 # 박힌 꺾쇠 머리
            n = int(rng.integers(4, 9))
            for i in range(1, n):
                if y0 - i < 0:
                    break
                c = ru[2] if i < 3 else ru[1] if i < n - 1 else ru[0]
                if g.v[x, y0 - i, B0]:
                    g.v[x, y0 - i, B0] = c
                elif g.v[x, y0 - i, B0 + 1]:
                    g.v[x, y0 - i, B0 + 1] = c


    def wall_base():
        g, rng, _ = _base(2001)
        rust_streaks(g, R(2091), count=2)
        return g


    def wall_drain():
        """배수구 벽: 남쪽 면 아래쪽에 녹슨 쇠 창살 배수구, 그 밑으로 긴 물 얼룩과 녹물."""
        g, rng, bid = _base(2002, stains=4)
        x0, x1, y0, y1, depth = 10, 20, 6, 13, 7
        # 구멍
        for x in range(x0, x1):
            for y in range(y0, y1):
                g.v[x, y, B0:B0 + depth] = 0
                g.v[x, y, B0 + depth] = P['water'][0]
        # 바닥에 고인 물
        for x in range(x0, x1):
            g.v[x, y0 - 1, B0 + 1:B0 + depth] = P['water'][2]
        # 테두리 돌(덮개돌 색)
        for x in range(x0 - 1, x1 + 1):
            for y in range(y0 - 1, y1 + 1):
                if not (x0 <= x < x1 and y0 <= y < y1):
                    g.v[x, y, B0] = P['cope'][1] if y == y1 else P['cope'][0]
        # 창살: 세로 넷 + 가로 하나, 녹
        ir, ru = P['iron'], P['rust']
        for x in (x0 + 1, x0 + 4, x0 + 6, x0 + 8):
            for y in range(y0, y1):
                g.v[x, y, B0 + 1] = ru[1] if (y + x) % 3 else ru[2]
        for x in range(x0, x1):
            g.v[x, y0 + 3, B0 + 1] = ir[2] if x % 2 else ru[1]
        # 아래로 흘러내린 물·녹 얼룩 (넓게 퍼진다)
        for x in range(x0, x1):
            n = 6 - abs(x - (x0 + x1) // 2) // 2 + int(rng.integers(0, 2))
            for y in range(y0 - 1, max(-1, y0 - 1 - n), -1):
                d = y0 - 1 - y
                c = ru[1] if (x % 3 == 0 and d < 4) else P['wet'][1] if d < n - 1 else P['wet'][2]
                if g.v[x, y, B0]:
                    g.v[x, y, B0] = c
                elif g.v[x, y, B0 + 1]:
                    g.v[x, y, B0 + 1] = c
        # 구멍 위 이끼
        for x in range(x0 + 2, x1 - 2):
            if rng.random() < 0.6:
                g.v[x, y1, B0] = P['algae'][1]
        return g


    def wall_barred():
        """창살 벽: 남쪽 면 위쪽에 작은 창, 녹슨 쇠창살, 밝은 창턱, 녹물 얼룩."""
        g, rng, bid = _base(2003, stains=5)
        x0, x1, y0, y1, depth = 10, 21, 13, 24, 9
        for x in range(x0, x1):
            for y in range(y0, y1):
                g.v[x, y, B0:B0 + depth] = 0
                g.v[x, y, B0 + depth] = P['dark'][0]
        for x in range(x0, x1):
            for z in range(B0 + 1, B0 + depth):
                g.v[x, y0 - 1, z] = P['dark'][2]
        # 창틀: 위 상인방은 밝은 돌, 옆은 다듬은 돌
        cp, ir, ru = P['cope'], P['iron'], P['rust']
        for x in range(x0 - 2, x1 + 2):
            g.v[x, y1, B0] = cp[2]
            g.v[x, y1 + 1, B0] = cp[3] if x0 - 2 < x < x1 + 1 else cp[1]
        for y in range(y0 - 1, y1):
            for x in (x0 - 1, x1):
                g.v[x, y, B0] = cp[1]
        # 창턱: 한 칸 내민다(발자국 밖 0.1 안쪽)
        for x in range(x0 - 2, x1 + 2):
            g.v[x, y0 - 1, B0] = cp[3]
            g.v[x, y0 - 1, B0 - 1] = cp[2]
            g.v[x, y0 - 2, B0] = cp[0]
        # 쇠창살: 세로 셋, 가로 하나
        for x in (x0 + 2, x0 + 5, x0 + 8):
            for y in range(y0, y1):
                g.v[x, y, B0 + 1] = ru[2] if (y * 7 + x) % 4 else ru[1]
        for x in range(x0, x1):
            g.v[x, y0 + 5, B0 + 1] = ir[2] if x % 3 else ru[2]
        # 창턱 밑으로 녹물
        for x in (x0 + 2, x0 + 5, x0 + 8):
            n = int(rng.integers(4, 9))
            for i in range(n):
                y = y0 - 3 - i
                if y >= 0 and g.v[x, y, B0]:
                    g.v[x, y, B0] = ru[1] if i < n - 2 else ru[0]
        return g


    def wall_collapsed():
        """무너진 벽: 오른쪽 절반이 한 단 무너져 내려앉았고, 그 턱에 부러진 아치 돌(홍예석 셋이 붙은 채)이 누워 있다."""
        g, rng, bid = _base(2004, stains=4)
        crumble(g, bid, COURSES, [
            (2, lambda x, z: x >= 15 + (2 if z > 20 else 0)),
            (1, lambda x, z: x >= 25 and z <= 9),
        ])
        drop_floating(g)
        st, cp = P['stone'], P['cope']
        hm = heightmap(g)
        for x in range(B0, B1):
            for z in range(B0, B1):
                y = hm[x, z]
                if 0 <= y < H - 3:
                    g.v[x, y, z] = st[5] if (x + 2 * z) % 6 else st[4]
        # 누운 아치 조각: 중심 (33, 26), 반지름 9..13, 두께 3칸, 70도 — 홍예석 셋, 사이는 줄눈
        cx, cz = 34.0, 27.0
        base_y = int(hm[22, 10]) + 1
        for x in range(10, GX):
            for z in range(0, GZ):
                dx, dz = x + 0.5 - cx, z + 0.5 - cz
                r = math.hypot(dx, dz)
                if not (8.5 <= r <= 13.0):
                    continue
                ang = math.degrees(math.atan2(-dz, -dx))   # 0 = 서쪽, 90 = 남쪽
                if ang < 8 or ang > 82:
                    continue
                k = (ang - 8) / 74 * 3
                seg = int(k)
                joint = abs(k - round(k)) * 74 / 3 < 1.2 and 0 < round(k) < 3
                y0 = max(base_y, int(hm[x, z]) + 1 if 0 <= x < GX and 0 <= z < GZ else base_y)
                for y in range(y0, min(base_y + 3, 33)):
                    c = cp[0] if joint else (cp[2] if seg == 1 else cp[3])
                    if y == base_y + 2 and not joint:
                        c = cp[4] if seg != 1 else cp[3]
                    g.v[x, y, z] = c
        # 떨어진 작은 돌 하나
        box(g, 25, 28, base_y, base_y + 2, 3, 6, cp[1])
        return g


    WALLS = {'Base': wall_base, 'A': wall_drain, 'B': wall_barred, 'C': wall_collapsed}


    # ================================================================ 소품
    def prop_light():
        """빛 · 푸른 벽등: 옛 무쇠 벽등, 창백한 푸른 유리판. 벽에 건다."""
        g = prop_grid(11, 19, 7, 'hang')
        ir, gl, ru = P['iron'], P['glass'], P['rust']
        # 벽에 박힌 받침판 + 앞으로 나온 팔(ㄱ자)
        box(g, 4, 7, 9, 17, 6, 7, ir[1])
        box(g, 5, 6, 15, 16, 2, 6, ir[2])
        box(g, 5, 6, 13, 15, 3, 4, ir[2])            # 고리
        # 등 몸통: x 2..8, y 3..12, z 0..4
        x0, x1, y0, y1, z0, z1 = 2, 9, 3, 12, 0, 5
        for x in range(x0, x1):
            for y in range(y0, y1):
                for z in range(z0, z1):
                    frame = x in (x0, x1 - 1) or y in (y0, y1 - 1) or z in (z0, z1 - 1)
                    if not frame:
                        continue
                    corner = (x in (x0, x1 - 1)) + (y in (y0, y1 - 1)) + (z in (z0, z1 - 1)) >= 2
                    if corner:
                        g.v[x, y, z] = ir[3] if (y == y1 - 1) else ir[2]
                    else:
                        # 유리판: 가운데 세로살 하나
                        mullion = (z == z0 and x == (x0 + x1) // 2) or (x in (x0, x1 - 1) and z == (z0 + z1) // 2)
                        g.v[x, y, z] = ir[2] if mullion else (gl[2] if y > y0 + 4 else gl[1])
        # 지붕(계단식 뿔) + 꼭지
        box(g, 1, 10, 12, 13, 0, 5, ir[2])
        box(g, 2, 9, 13, 14, 0, 5, ir[3])
        box(g, 3, 8, 14, 15, 1, 4, ir[2])
        box(g, 4, 7, 15, 16, 1, 4, ir[3])
        box(g, 5, 6, 16, 18, 2, 3, ir[2])
        # 바닥판 + 아래 꼭지
        box(g, 1, 10, 2, 3, 0, 5, ir[1])
        box(g, 4, 7, 1, 2, 1, 4, ir[1])
        box(g, 5, 6, 0, 1, 2, 3, ir[0])
        # 녹
        for (x, y, z) in ((1, 12, 0), (9, 12, 0), (2, 3, 0), (8, 3, 0), (4, 13, 0)):
            put(g, x, y, z, ru[2])
        return g


    def prop_wall():
        """벽 소품 · 밧줄과 두레박: 쇠 갈고리에 걸린 감긴 밧줄과 낡은 나무 두레박(젖은 나무, 쇠테)."""
        g = prop_grid(14, 20, 8, 'hang')
        ir, wd, rp, ru = P['iron'], P['wood'], P['rope'], P['rust']
        # 갈고리
        box(g, 6, 8, 17, 20, 7, 8, ir[1])
        box(g, 6, 8, 17, 18, 5, 7, ir[2])
        put(g, 6, 18, 5, ir[3]); put(g, 7, 18, 5, ir[3])
        # 감긴 밧줄: 굵은 고리 한 타래(두 칸 굵기), 갈고리에 걸려 아래로 처진 타원 — 꼬임은 두 톤 사선
        cx, cy, rx, ry = 7.0, 13.2, 5.6, 4.2
        for x in range(14):
            for y in range(6, 18):
                d = ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2
                if 0.45 <= d <= 1.0:
                    for z in (4, 5, 6):
                        if z == 6 and d < 0.6:
                            continue
                        g.v[x, y, z] = rp[3] if (x + y + z) % 3 == 0 else rp[2] if z < 6 else rp[1]
        # 밧줄 끝이 두레박 손잡이로 내려간다
        for y in range(8, 11):
            put(g, 3, y, 4, rp[2])
        # 두레박: 통(가로 8, 높이 7, 깊이 6), 위가 조금 넓다, 쇠테 둘
        bx0, bz0 = 3, 1
        for y in range(0, 8):
            w = 8 if y >= 4 else 7
            xa = bx0 + (0 if y >= 4 else 0)
            for x in range(xa, xa + w):
                for z in range(bz0, bz0 + 6):
                    rim = x in (xa, xa + w - 1) or z in (bz0, bz0 + 5)
                    if not rim and y > 0:
                        continue
                    if y in (1, 6):
                        c = ru[1] if (x + z) % 4 else ir[2]
                    else:
                        c = wd[2] if (x % 2 == 0) else wd[3]
                        if z == bz0 + 5 or x == xa + w - 1:
                            c = wd[1]
                    g.v[x, y, z] = c
        # 안쪽 물(어둡게)
        box(g, bx0 + 1, bx0 + 7, 5, 6, bz0 + 1, bz0 + 5, P['water'][1])
        # 손잡이(밧줄)
        for (x, y) in ((3, 8), (4, 9), (5, 10), (6, 10), (7, 10), (8, 10), (9, 9), (10, 8)):
            put(g, x, y, 3, rp[2])
        return g


    def prop_floor():
        """바닥(얇게) · 녹슨 바닥 창살: 네모 돌판에 박힌 아주 납작한 녹슨 쇠 창살, 아래로 검은 물."""
        g = prop_grid(17, 2, 17, 'floor')
        cp, ru, ir, wa = P['cope'], P['rust'], P['iron'], P['water']
        box(g, 0, 17, 0, 1, 0, 17, cp[1])
        for x in range(17):
            for z in range(17):
                edge = x in (0, 16) or z in (0, 16)
                g.v[x, 1, z] = cp[2] if edge else 0
                if 3 <= x <= 13 and 3 <= z <= 13:
                    g.v[x, 0, z] = wa[0] if (x + z) % 5 else wa[1]
                elif not edge:
                    g.v[x, 1, z] = cp[0] if (x + z) % 3 else cp[1]
        # 창살: 가로 막대 다섯, 세로 테두리
        for x in range(2, 15):
            for z in range(2, 15):
                frame = x in (2, 14) or z in (2, 14)
                bar = (z - 3) % 2 == 1 and 3 <= x <= 13
                if frame:
                    g.v[x, 1, z] = ru[1] if (x * 3 + z) % 5 else ir[2]
                elif bar:
                    g.v[x, 1, z] = ru[2] if (x + z) % 4 else ru[1]
        # 녹물 번진 자국
        for (x, z) in ((1, 5), (1, 6), (15, 10), (6, 15), (7, 15)):
            g.v[x, 1, z] = ru[0]
        return g


    def prop_big():
        """바닥(벽 옆 빈칸) · 멈춘 물레바퀴 조각: 부서진 나무 물레바퀴 반쪽이 벽에 기대 서 있다. 젖은 검은 나무, 녹슨 쇠테."""
        g = prop_grid(30, 20, 9, 'big')
        wd, ru, ir, al = P['wood'], P['rust'], P['iron'], P['algae']
        cx, cy = 15.0, 4.6
        R_out, R_in, R_hub = 14.5, 11.0, 2.6
        z0, z1 = 1, 8
        for x in range(30):
            for y in range(20):
                dx, dy = x + 0.5 - cx, y + 0.5 - cy
                r = math.hypot(dx, dy)
                ang = math.degrees(math.atan2(dy, dx))      # -180..180, 0 = 오른쪽
                if ang < -18 or ang > 172:
                    continue                                 # 부러져 나간 쪽
                broken = ang < -8 or ang > 160
                for z in range(z0, z1):
                    c = 0
                    if R_in <= r <= R_out:
                        if z in (z0, z1 - 1):
                            # 앞뒤 테(널판). 가운데 쇠띠 한 줄
                            c = ru[1] if abs(r - 12.6) < 0.55 else (wd[2] if z == z0 else wd[1])
                        elif (int((ang + 360) % 360) % 18) < 4:
                            c = wd[3]                            # 물받이 판
                    elif R_hub < r < R_in and z in (z0, z0 + 1) and (int((ang + 360 + 8) % 360) % 45) < 7:
                        c = wd[2] if z == z0 else wd[1]          # 바퀴살
                    elif r <= R_hub:
                        c = ir[2] if z in (z0, z1 - 1) else ir[1] # 굴대 통
                    if c:
                        g.v[x, y, z] = wd[4] if broken and c in (wd[1], wd[2], wd[3]) else c
        # 바퀴 굴대(뒤로 벽까지)
        box(g, 14, 16, 4, 6, z1 - 1, 9, ir[1])
        # 물이끼 몇 점(테 위쪽)
        rng = R(2201)
        for x in range(30):
            for y in range(8, 20):
                if g.v[x, y, z0] in (wd[1], wd[2]) and rng.random() < 0.08:
                    g.v[x, y, z0] = al[1]
        return g


    PROPS = {'Light': (prop_light, 'light'), 'Wall': (prop_wall, 'hang'),
             'Floor': (prop_floor, 'floor'), 'Big': (prop_big, 'big')}

    return types.SimpleNamespace(P=P, WALLS=WALLS, PROPS=PROPS)


# ================================================================ chapter 2
def _chapter2():
    """챕터 2 — 잿빛 용광로. 마검이 벼려진 대장간: 그을린 무쇠와 검은 벽돌 위에 흰 재가 두껍게 쌓였다.
    흰 재의 윗면이 주황빛 바닥과 대비를 만든다. 모루는 없다."""
    def palette():
        p = Palette()
        p.ramp('brick', ['#2b221e', '#362b26', '#41342d', '#4c3d35', '#584639'])
        p.ramp('heat', ['#5c3324', '#6e3c29'])           # 아래쪽 열에 그은 벽돌
        p.ramp('mortar', ['#141110', '#1b1715'])
        p.ramp('iron', ['#24262a', '#2f3237', '#3c4046', '#4b5057', '#5d636a'])
        p.ramp('scorch', ['#2f2a33', '#3a3140', '#4a3a3a'])   # 달궈졌던 쇠의 무지갯빛 그을음(어둡게)
        p.ramp('ash', ['#8e8983', '#a6a19a', '#bdb8b0', '#d2cdc4', '#e4e0d7'])
        p.ramp('soot', ['#100e0d', '#171413'])
        p.ramp('slag', ['#221f1f', '#2e2a29', '#3c3634', '#4d4541', '#6a5f58'])
        p.ramp('ember', ['#c2451a', '#ff7a1a', '#ffb347'], emit=True)
        p.ramp('leather', ['#3b2418', '#4d3020', '#603d29', '#734a32'])
        p.ramp('wood', ['#2e2017', '#3d2b1e', '#4d3727', '#5e4430'])
        p.ramp('stone', ['#3c3835', '#4a4541', '#59534e', '#67615b'])
        p.ramp('coal', ['#151312', '#201c1a'])
        return p


    P = palette()
    COURSES = courses_from([7, 8, 7, 8])
    TOP_SEED = 3000


    def _base(seed, ash_cover_inner=1.0, corner_iron=True):
        rng = R(seed)
        g = wall_grid()
        br = P['brick']
        bid = lay_bricks(g, rng, COURSES, lengths=[8, 9, 10, 11], tones=br[1:5], tone_w=[3, 3, 2, 1],
                         mortar=P['mortar'][0], recess=1, sunk=0.04, faces=(0,))
        heat_band(g, rng, bid)
        if corner_iron:
            iron_corners(g)
        iron_band(g, 14)
        ash_top(g, seed)
        ash_spill(g, rng)
        return g, rng, bid


    def heat_band(g, rng, bid):
        """불이 아래에 있다: 맨 아랫줄 벽돌 몇 장은 열에 그어 붉은 기가 돈다."""
        ids = np.unique(bid[:, 0:COURSES[0][1], :][bid[:, 0:COURSES[0][1], :] >= 0])
        for b in ids:
            if rng.random() < 0.45:
                m = (bid == b) & (g.v > 0)
                g.v[m] = P['heat'][int(rng.integers(0, 2))]


    def iron_corners(g):
        """네 세로 모서리에 무쇠 모서리쇠(ㄱ자, 폭 3) — 단조 공방의 벽."""
        ir = P['iron']
        for (x, z) in ((B0, B0), (B1 - 1, B0), (B0, B1 - 1), (B1 - 1, B1 - 1)):
            sx = 1 if x == B0 else -1
            sz = 1 if z == B0 else -1
            for y in range(0, H - 2):
                for k in range(3):
                    g.v[x + sx * k, y, z] = ir[2] if k < 2 else ir[1]
                    g.v[x, y, z + sz * k] = ir[2] if k < 2 else ir[1]
                if y % 6 == 3:      # 리벳
                    g.v[x + sx, y, z] = ir[4]
                    g.v[x, y, z + sz] = ir[4]


    def iron_band(g, y):
        """가운데 높이에 무쇠 띠(두 칸) — 리벳 박힌."""
        ir = P['iron']
        for x in range(B0, B1):
            for z in range(B0, B1):
                if SHELL_D[x, z] == 0:
                    s = SHELL_S[x, z]
                    g.v[x, y, z] = ir[3]
                    g.v[x, y + 1, z] = ir[2]
                    if s % 6 == 2:
                        g.v[x, y, z] = ir[4]


    ASH_Y = H - 2      # 재가 시작하는 높이(y 28) — 아래 한 겹(y 27)은 그을린 덮개


    def ash_top(g, seed, thick_extra=None):
        """두꺼운 재: 그을린 덮개 한 겹(y 27) 위에 재 4칸(y 28..31), 발자국 밖 한 칸까지.
        남쪽에서 보면 벽 꼭대기에 흰 띠로 보이고, 위에서 보면 평평한 재에 옅은 얼룩뿐이다(숯 점은 엔진에서 잡음으로 읽혀 뺐다).
        높이를 물결치게 하면 그 계단의 그림자 선이 위에서 금(글자)처럼 보여서 평평하게 둔다."""
        ash = P['ash']
        g.v[:, ASH_Y:, :] = 0
        cap_slab(g, ASH_Y - 1, ASH_Y, lambda x, z, y: P['soot'][1])
        tone = top_field(TOP_SEED + 5, seed + 5, 5, octaves=2)
        for x in range(TOPW):
            for z in range(TOPW):
                t = 4
                if thick_extra is not None:
                    t += thick_extra(x, z)
                t = max(1, min(t, GY - 1 - ASH_Y))
                for k in range(t):
                    y = ASH_Y + k
                    if k == t - 1:
                        v = tone[x, z]
                        c = ash[4] if v > 0.66 else ash[3] if v > 0.34 else ash[2]
                    elif k == t - 2:
                        c = ash[2]
                    else:
                        c = ash[1]
                    g.v[x, y, z] = c


    def ash_spill(g, rng, count=6):
        """재가 남쪽 턱을 넘어 흘러내린 자국 — 짧고 넓게."""
        ash = P['ash']
        streaks(g, rng, 0, count, (2, 6), (2, 4), lambda i, n: ash[2] if i < 1 else ash[1] if i < n - 1 else ash[0],
                y_from=ASH_Y - 2)


    def wall_base():
        g, _, _ = _base(3001)
        return g


    def wall_riveted():
        """리벳 벽: 그을린 무쇠 판을 리벳으로 이었다. 달궈졌던 자국, 윗면은 흰 재."""
        g, rng, _ = _base(3002, corner_iron=False)
        ir, sc = P['iron'], P['scorch']
        # 네 면을 판으로 덮는다: 판 경계 = s 가 10 의 배수 근처, 높이 0..14, 15..27
        plates_y = [(0, 14), (14, 28)]
        for x in range(B0, B1):
            for z in range(B0, B1):
                if SHELL_D[x, z] != 0:
                    continue
                s = SHELL_S[x, z]
                seam_s = (s % 15 == 0)
                for (y0, y1) in plates_y:
                    for y in range(y0, y1):
                        edge = seam_s or y == y0 or y == y1 - 1
                        c = ir[1] if edge else ir[2]
                        # 리벳: 판 가장자리에서 한 칸 안쪽, 3칸마다
                        rivet = ((s % 15 in (1, 14)) and (y - y0) % 3 == 1) or \
                                ((y == y0 + 1 or y == y1 - 2) and s % 3 == 1 and not seam_s)
                        g.v[x, y, z] = ir[4] if rivet else c
                        if rivet and SHELL_FACE[x, z] == 0:
                            g.v[x, y, z - 1] = ir[3]          # 남쪽 면 리벳은 한 칸 도드라진다
        # 달궈졌던 자국: 남쪽 면 아래쪽에서 위로 번진 얼룩
        n = noise2((GX, GY), 6, rng, 2)
        for x in range(B0, B1):
            for y in range(0, 22):
                v = n[x, y] - y * 0.02
                if v > 0.55 and g.v[x, y, B0] in (ir[2], ir[1]):
                    g.v[x, y, B0] = sc[1] if v > 0.62 else sc[0]
        return g


    def wall_chimney():
        """굴뚝 벽: 윗면에서 부러진 무쇠 굴뚝 토막이 짧게 솟았고, 둘레는 흰 재가 덮었다.
        굴뚝은 작게(바깥 지름 9) — 엔진 미리보기에서 큰 검은 구멍과 둘레 재 구덩이의 고리가 재 위의 잡음으로 읽혔다."""
        g, rng, _ = _base(3003)
        ir, so = P['iron'], P['soot']
        cx, cz = 15.0, 16.0
        r_out, r_in = 4.6, 2.6
        for x in range(GX):
            for z in range(GZ):
                dx, dz = x + 0.5 - cx, z + 0.5 - cz
                d = math.hypot(dx, dz)
                ang = math.atan2(dz, dx)
                if d < r_in:
                    g.v[x, ASH_Y - 1:, z] = 0
                    g.v[x, ASH_Y - 2, z] = so[0]
                elif d < r_out:
                    # 부러진 테두리: 남쪽 절반은 한 칸 낮다
                    top = 32 if math.sin(ang) > -0.25 else 31
                    for y in range(ASH_Y - 2, top + 1):
                        if y >= top - 1:
                            c = ir[4] if y == top else ir[3]
                        else:
                            c = ir[2] if (int((math.degrees(ang) + 360) // 30)) % 2 else ir[1]
                        g.v[x, y, z] = c
                    for y in range(top + 1, GY):
                        g.v[x, y, z] = 0
        return g


    def wall_slag():
        """무너진 벽: 남쪽 위 모서리가 한 덩어리 떨어져 나갔고(재가 흘러 들어 메웠다), 식은 검은 쇠똥(슬래그)이
        남쪽 면을 타고 흘러내려 굳었다. 위에서는 흰 재가 앞 가장자리에서 조금 꺼진 것만 보인다."""
        g, rng, bid = _base(3004)
        crumble(g, bid, COURSES, [
            (3, lambda x, z: 3 <= x <= 13 and z <= 7 - abs(x + 0.5 - 8) * 0.5),
            (2, lambda x, z: 5 <= x <= 10 and z <= 3),
        ])
        drop_floating(g)
        br, ash, sl, em = P['brick'], P['ash'], P['slag'], P['ember']
        hm = heightmap(g)
        for x in range(GX):
            for z in range(GZ):
                if 0 <= hm[x, z] < ASH_Y - 1:
                    g.v[x, hm[x, z], z] = br[4]                     # 깨진 벽돌 속살
        fill_break(g, ASH_Y + 3, (ash[1], ash[3]))
        # 슬래그: 남쪽 면 오른쪽 위에서 흘러내린 굵은 줄기 셋(한 칸 도드라짐), 끝은 둥근 방울
        for (x0, w, n) in ((17, 4, 22), (22, 3, 15), (26, 2, 10)):
            for j in range(w):
                x = x0 + j
                nn = n - abs(j - w // 2) * 3
                for i in range(nn):
                    y = ASH_Y - 1 - i
                    if y < 0:
                        break
                    c = sl[2] if j == 0 else sl[1] if j < w - 1 else sl[0]
                    if i % 7 == 3 and j == w // 2:
                        c = sl[3]
                    g.v[x, y, B0] = c
                    g.v[x, y, B0 - 1] = c if (j > 0 and j < w - 1) or w <= 2 else 0
                # 방울
                yb = ASH_Y - 1 - nn
                if yb >= 1:
                    g.v[x, yb, B0 - 1] = sl[1]
            # 굳다 만 속불: 줄기 가운데 한두 점
            g.v[x0 + w // 2, ASH_Y - 5, B0 - 1] = em[0]
            g.v[x0, ASH_Y - 2, B0 - 1] = sl[4]
        return g


    WALLS = {'Base': wall_base, 'A': wall_riveted, 'B': wall_chimney, 'C': wall_slag}


    # ================================================================ 소품
    def prop_light():
        """빛 · 화로(바닥): 짧은 세 다리 위 둥근 무쇠 화로, 빨갛게 달아오른 숯. 발목 높이."""
        g = prop_grid(15, 8, 15, 'floor')
        ir, em, co = P['iron'], P['ember'], P['coal']
        cx = cz = 7.5
        # 세 다리(높이 2)
        for (x, z) in ((7, 1), (2, 11), (12, 11)):
            box(g, x, x + 2, 0, 2, z, z + 2, ir[1])
        # 그릇: 바닥 y2, 벽 y3..5, 테 y5 — 위가 넓다
        for x in range(15):
            for z in range(15):
                d = math.hypot(x + 0.5 - cx, z + 0.5 - cz)
                if d <= 5.0:
                    g.v[x, 2, z] = ir[1]
                if 5.0 < d <= 6.2:
                    g.v[x, 3, z] = ir[2]
                if 5.6 < d <= 7.2:
                    g.v[x, 4, z] = ir[2]
                    g.v[x, 5, z] = ir[3] if (x + z) % 5 else ir[4]
                if d <= 5.6:
                    g.v[x, 3, z] = co[0]
                if d <= 6.0:
                    g.v[x, 4, z] = co[1] if (x * 3 + z) % 4 else em[0]
        # 숯 더미: 가운데 봉긋하게, 빛나는 알
        rng = R(3101)
        for x in range(15):
            for z in range(15):
                d = math.hypot(x + 0.5 - cx, z + 0.5 - cz)
                if d <= 5.4:
                    h = 5 + (1 if d < 3.6 else 0) + (1 if d < 1.8 else 0)
                    for y in range(5, h + 1):
                        r = rng.random()
                        g.v[x, y, z] = em[2] if r < 0.18 else em[1] if r < 0.55 else co[1] if r < 0.8 else em[0]
        return g


    def prop_wall():
        """벽 소품 · 도르래 사슬과 갈고리: 벽 받침대의 무쇠 도르래, 늘어진 사슬과 무거운 갈고리. 그을음."""
        g = prop_grid(13, 20, 6, 'hang')
        ir, so = P['iron'], P['soot']
        # 벽 받침대: 등판(위쪽) + 앞으로 나온 두 갈래 팔이 도르래 축을 문다
        box(g, 4, 9, 15, 20, 5, 6, ir[1])
        box(g, 4, 5, 15, 17, 1, 5, ir[2])
        box(g, 8, 9, 15, 17, 1, 5, ir[2])
        # 도르래 바퀴(정면 원판, 지름 7): 테는 밝게, 홈은 어둡게, 가운데 축
        cx, cy = 6.5, 14.0
        for x in range(13):
            for y in range(9, 19):
                d = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
                if d <= 3.7:
                    c = ir[4] if d > 3.0 else ir[1] if d > 2.2 else ir[3] if d > 1.0 else ir[0]
                    g.v[x, y, 2] = c
                    g.v[x, y, 3] = ir[2] if d > 3.0 else ir[1]
        # 사슬 두 가닥(바퀴 양끝에서 곧게 늘어진다): 정면 고리(3칸)와 옆 고리(1칸)가 엇갈려 끊김 없이
        def chain(x, y_top, y_bot):
            y = y_top
            k = 0
            while y >= y_bot:
                if k % 2 == 0:      # 정면 고리: 가운데 뚫린 3x3
                    for dx in (-1, 0, 1):
                        for dy in (0, -1, -2):
                            if dx == 0 and dy == -1:
                                continue
                            put(g, x + dx, y + dy, 2, ir[3] if dy == 0 else ir[2])
                else:               # 옆 고리: 한 칸 기둥(앞뒤 두 칸)
                    for dy in (0, -1, -2):
                        put(g, x, y + dy, 2, ir[2])
                        put(g, x, y + dy, 3, ir[1])
                y -= 2
                k += 1
        chain(3, 13, 4)
        chain(10, 13, 9)
        # 무거운 갈고리(왼쪽 가닥 끝): 굵은 J — 고리목, 휜 배, 뾰족한 끝
        hook = [(3, 3), (3, 2), (2, 2), (2, 1), (2, 0), (3, 0), (4, 0), (5, 0), (6, 1), (6, 2), (7, 3), (5, 1)]
        for (x, y) in hook:
            put(g, x, y, 2, ir[4] if y == 0 else ir[3])
            put(g, x, y, 3, ir[1])
        put(g, 7, 4, 2, ir[4])              # 끝
        # 오른쪽 가닥 끝 추
        box(g, 9, 12, 5, 8, 2, 4, ir[2])
        for x in range(9, 12):
            put(g, x, 7, 2, ir[3])
        # 그을음은 받침대 위쪽에만
        for (x, y) in ((4, 19), (8, 19), (5, 18)):
            put(g, x, y, 5, so[1])
        return g


    def prop_floor():
        """바닥 · 슬래그 더미: 낮게 쌓인 검은 쇠똥 덩어리, 희미하게 빛나는 주황 금. 발목 높이."""
        g = prop_grid(17, 6, 15, 'floor')
        sl, em = P['slag'], P['ember']
        rng = R(3201)
        lumps = [(5, 5, 4.2, 4), (11, 6, 3.6, 5), (8, 10, 3.8, 4), (13, 11, 2.6, 3), (3, 10, 2.4, 2), (9, 3, 2.2, 2)]
        for (cx, cz, r, h) in lumps:
            for x in range(17):
                for z in range(15):
                    d = math.hypot(x + 0.5 - cx, z + 0.5 - cz)
                    if d > r:
                        continue
                    top = int(round(h * (1 - (d / r) ** 2) + 0.4))
                    for y in range(0, max(1, top)):
                        if g.v[x, y, z]:
                            continue
                        lit = y == top - 1
                        g.v[x, y, z] = sl[4] if (lit and (x - cx) < -1) else sl[3] if lit else sl[2] if y > 0 else sl[1]
        # 빛나는 금: 덩어리 사이 골
        for (x, z) in ((7, 7), (8, 7), (8, 8), (10, 9), (6, 8), (12, 8), (5, 9)):
            y = int(np.nonzero(g.v[x, :, z])[0].max()) if g.v[x, :, z].any() else 0
            g.v[x, y, z] = em[1] if (x + z) % 2 else em[0]
        return g


    def prop_extra():
        """[선택] 바닥 · 휜 칼 모양 빈 거푸집(복선): 초승달처럼 휜 칼날 모양으로 파인 돌 거푸집, 속은 비었다."""
        g = prop_grid(19, 3, 11, 'floor')
        st, so = P['stone'], P['soot']
        box(g, 0, 19, 0, 3, 0, 11, st[2])
        for x in range(19):
            for z in range(11):
                if x in (0, 18) or z in (0, 10):
                    g.v[x, 2, z] = st[3]
        # 휜 칼날 홈: 초승달(바깥 원 - 안쪽 원), 자루 쪽 홈
        for x in range(1, 18):
            for z in range(1, 10):
                a = ((x + 0.5 - 9.5) / 8.2) ** 2 + ((z + 0.5 - 1.0) / 8.0) ** 2
                b = ((x + 0.5 - 9.5) / 8.6) ** 2 + ((z + 0.5 + 1.8) / 8.0) ** 2
                blade = a <= 1.0 and b > 1.0 and x >= 4
                hilt = 1 <= x <= 4 and 5 <= z <= 6
                if blade or hilt:
                    g.v[x, 2, z] = 0
                    g.v[x, 1, z] = so[1] if blade else so[0]
        # 거푸집 테두리의 그을음
        for (x, z) in ((3, 2), (15, 8), (16, 2)):
            g.v[x, 2, z] = st[1]
        return g


    def prop_big():
        """바닥(벽 옆 빈칸) · 가죽 풀무: 커다란 가죽 풀무, 나무 손잡이, 무쇠 주둥이. 그을음."""
        g = prop_grid(30, 12, 16, 'big')
        lt, wd, ir, so = P['leather'], P['wood'], P['iron'], P['soot']
        # 받침대 둘
        box(g, 5, 8, 0, 3, 3, 13, wd[1])
        box(g, 18, 21, 0, 3, 3, 13, wd[1])

        def half(x):
            t = (x - 2) / 22.0
            return 6.2 * (1 - t ** 1.7) + 1.0

        # 아래 판
        for x in range(2, 25):
            for z in range(1, 15):
                if abs(z + 0.5 - 8) <= half(x):
                    g.v[x, 3, z] = wd[1]
        # 주름 가죽: 층마다 안으로 들어갔다 나온다(지그재그), 앞쪽 가장자리가 밝다
        for y in range(4, 8):
            inset = 0.0 if y % 2 == 0 else 1.0
            for x in range(2, 24):
                for z in range(1, 15):
                    h = half(x) - inset
                    dz = abs(z + 0.5 - 8)
                    if dz <= h:
                        edge = dz > h - 1.0
                        g.v[x, y, z] = (lt[3] if y % 2 == 0 else lt[1]) if edge else lt[0]
        # 위 판(밝은 나무, 결 두 줄)
        for x in range(2, 25):
            for z in range(1, 15):
                if abs(z + 0.5 - 8) <= half(x):
                    g.v[x, 8, z] = wd[2]
                    g.v[x, 9, z] = wd[3] if (z % 5) else wd[2]
        # 주둥이(무쇠 관, 끝은 그을음 구멍)
        box(g, 23, 30, 4, 7, 6, 10, ir[2])
        box(g, 23, 30, 6, 7, 6, 10, ir[3])
        box(g, 29, 30, 5, 6, 7, 9, so[0])
        box(g, 22, 24, 3, 9, 5, 11, ir[3])
        # 손잡이(넓은 쪽 끝, 위·아래 판에서 뻗는다)
        box(g, 0, 3, 9, 11, 6, 10, wd[3])
        box(g, 0, 1, 9, 11, 5, 11, wd[3])
        box(g, 0, 3, 3, 4, 7, 9, wd[2])
        # 위판 가운데 무쇠 바람 구멍 덮개
        box(g, 8, 11, 10, 11, 7, 10, ir[2])
        return g


    PROPS = {'Light': (prop_light, 'floor'), 'Wall': (prop_wall, 'hang'),
             'Floor': (prop_floor, 'floor'), 'Extra': (prop_extra, 'floor'), 'Big': (prop_big, 'big')}

    return types.SimpleNamespace(P=P, WALLS=WALLS, PROPS=PROPS)


# ================================================================ chapter 3
def _chapter3():
    """챕터 3 — 얼어붙은 심층. 탑지기들의 얼어붙은 초소: 서리 낀 돌, 두꺼운 눈 모자, 고드름.
    해가 낮아 그림자가 길다. 따뜻한 빛은 '마지막 불씨' 하나뿐이다."""
    def palette():
        p = Palette()
        p.ramp('stone', ['#2a333d', '#343f4a', '#3f4b57', '#4b5864', '#586571', '#66737f'])
        p.ramp('mortar', ['#1b2229', '#232b33'])
        p.ramp('frost', ['#7f97aa', '#99b2c4', '#b5cbda', '#cfe0ec'])
        p.ramp('snow', ['#b9c8d3', '#d0dce4', '#e4edf2', '#f2f7fa', '#ffffff'])
        p.ramp('ice', ['#7fb0d0', '#a5cde6', '#cae6f5', '#eaf7fd'])
        p.ramp('dark', ['#0d1218', '#141c24'])
        p.ramp('wood', ['#45372b', '#584636', '#6c5743', '#826a52'])
        p.ramp('icewood', ['#5d6f7d', '#71838f', '#8798a2'])
        p.ramp('iron', ['#2e343a', '#3e464e', '#525b64', '#68727c', '#848f99'])
        p.ramp('cold', ['#6cc2ff', '#bfe8ff'], emit=True)          # 차가운 푸른 불
        p.ramp('ember', ['#e0561e', '#ff8a3a', '#ffc070'], emit=True)
        p.ramp('ash', ['#4a4744', '#5c5854', '#706b66'])
        p.ramp('leather', ['#3a2a20', '#4c372a'])
        return p


    P = palette()
    COURSES = courses_from([7, 8, 7, 8])
    TOP_SEED = 4000
    SNOW_Y = H - 4        # 눈 모자가 시작하는 높이(y 26) — 남쪽에서 보면 두꺼운 흰 띠


    def _base(seed, frost=0.45):
        rng = R(seed)
        g = wall_grid()
        st = P['stone']
        bid = lay_bricks(g, rng, COURSES, lengths=[7, 9, 10, 12, 14], tones=st[1:5], tone_w=[2, 3, 3, 2],
                         mortar=P['mortar'][0], recess=1, sunk=0.05, faces=(0,))
        bevel_blocks(g, bid, st, up=1, down=0)
        frost_face(g, rng, frost)
        snow_cap(g, seed)
        return g, rng, bid


    def frost_face(g, rng, amount=0.32):
        """서리: 남쪽 면에 위쪽·모서리일수록 짙게. 줄눈 홈에도 낀다."""
        fr = P['frost']
        n = noise2((GX, GY), 4, rng, 2)
        for x in range(B0, B1):
            edge = 1.0 - min(x - B0, B1 - 1 - x) / 15.0
            for y in range(0, SNOW_Y):
                v = n[x, y] + (y / SNOW_Y) * 0.35 + edge * 0.15 - (1 - amount) * 0.9
                for z in (B0, B0 + 1):
                    c = g.v[x, y, z]
                    if c == 0:
                        continue
                    if v > 0.55:
                        g.v[x, y, z] = fr[1] if v > 0.68 else fr[0]
                    break


    def snow_cap(g, seed, extra=None):
        """눈 모자: y 26 부터 4~6칸, 발자국 밖 한 칸까지(이웃과 이어진다). 남쪽 턱으로 덩어리가 처진다."""
        sn = P['snow']
        g.v[:, SNOW_Y:, :] = 0
        hf = top_field(TOP_SEED, seed, 8, octaves=2)
        tone = top_field(TOP_SEED + 3, seed + 3, 9, octaves=2)
        drift = top_field(TOP_SEED + 6, seed + 6, 16, octaves=1)
        glit = top_field(TOP_SEED + 8, seed + 8, 1.5, octaves=1)
        for x in range(TOPW):
            for z in range(TOPW):
                # 평평하게 — 높낮이 계단은 낮은 해 아래 윤곽선(글자 같은 무늬)이 된다. 눈은 색과 반짝임으로 낸다
                t = 6
                if extra is not None:
                    t += extra(x, z)
                t = max(0, min(t, GY - SNOW_Y))
                for k in range(t):
                    y = SNOW_Y + k
                    if k == t - 1:
                        v = tone[x, z]
                        c = sn[4] if v > 0.6 or glit[x, z] > 0.93 else sn[3]
                    elif k == t - 2:
                        c = sn[2]
                    else:
                        c = sn[1]
                    g.v[x, y, z] = c
        # 남쪽 턱으로 처진 눈 덩어리: 아래로 1~3칸, 둥근 끝
        r = R(seed + 11)
        x = int(r.integers(0, 4))
        while x < TOPW:
            w = int(r.integers(3, 7))
            d = int(r.integers(1, 4))
            for j in range(w):
                xx = x + j
                if xx >= TOPW:
                    break
                dd = d - (1 if j in (0, w - 1) else 0)
                for k in range(1, dd + 1):
                    put(g, xx, SNOW_Y - k, 0, sn[1] if k < dd else sn[0])
            x += w + int(r.integers(2, 6))


    def wall_base():
        g, _, _ = _base(4001)
        return g


    def wall_icicles():
        """고드름 벽: 눈 모자 남쪽 턱에서 고드름이 줄지어 내려온다."""
        g, rng, _ = _base(4002, frost=0.42)
        ice = P['ice']
        x = 1
        while x < TOPW - 1:
            n = int(rng.integers(4, 13))
            wide = n >= 8
            top = SNOW_Y - 1
            # 처진 눈 아래부터
            while top > 0 and g.v[x, top, 0]:
                top -= 1
            for i in range(n):
                y = top - i
                if y < 0:
                    break
                g.v[x, y, 0] = ice[2] if i < n * 0.4 else ice[1] if i < n - 1 else ice[0]
                if wide and i < n * 0.45 and x + 1 < TOPW:
                    g.v[x + 1, y, 0] = ice[3] if i < 2 else ice[2]
            x += (3 if wide else 2) + int(rng.integers(0, 3))
        return g


    def wall_slit():
        """화살 구멍 벽: 남쪽 면에 좁고 긴 화살 구멍(안쪽으로 벌어진다), 서리 낀 테두리돌, 턱에 쌓인 눈."""
        g, rng, _ = _base(4003)
        cx = 15
        y0, y1 = 5, 21
        st, fr, sn = P['stone'], P['frost'], P['snow']
        for y in range(y0, y1):
            for z in range(B0, B0 + 10):
                depth = z - B0
                half = 1 if depth < 3 else 1 + min(3, depth - 2)
                for x in range(cx - half, cx + half):
                    g.v[x, y, z] = 0
            g.v[cx - 4:cx + 4, y, B0 + 10] = P['dark'][0]
        for x in range(cx - 4, cx + 4):
            for z in range(B0 + 3, B0 + 10):
                g.v[x, y0 - 1, z] = P['dark'][1]
        # 테두리돌: 구멍 둘레 두 칸, 밝은 서리
        for y in range(y0 - 2, y1 + 2):
            for x in range(cx - 3, cx + 3):
                if g.v[x, y, B0] and not (cx - 1 <= x < cx + 1 and y0 <= y < y1):
                    g.v[x, y, B0] = fr[2] if (y in (y0 - 2, y1 + 1) or x in (cx - 3, cx + 2)) else fr[1]
        # 아래 턱의 눈
        for x in range(cx - 1, cx + 1):
            g.v[x, y0, B0] = sn[3]
            g.v[x, y0, B0 + 1] = sn[2]
        return g


    def wall_frostbreak():
        """무너진 벽: 서리에 갈라져 남쪽 위 모서리 덩어리가 떨어져 나갔고, 갈라진 자리를 눈이 메웠다.
        깨진 돌은 남쪽 면과 갈라진 옆구리에만 보이고, 위에서는 눈 모자 앞 가장자리가 비탈지게 꺼진 것만 보인다."""
        g, rng, bid = _base(4004)
        crumble(g, bid, COURSES, [
            (3, lambda x, z: 3 <= x <= 17 and z <= 8 - abs(x + 0.5 - 10) * 0.45),
            (2, lambda x, z: 6 <= x <= 13 and z <= 4),
        ])
        drop_floating(g)
        st, sn, fr = P['stone'], P['snow'], P['frost']
        hm = heightmap(g)
        for x in range(GX):
            for z in range(GZ):
                if 0 <= hm[x, z] < SNOW_Y - 1:
                    g.v[x, hm[x, z], z] = st[5] if (x + z) % 3 else fr[1]   # 깨진 속돌
        fill_break(g, SNOW_Y + 5, (sn[1], sn[3]))
        # 서리 금: 남쪽 면, 깨진 자리 오른쪽 끝에서 아래로
        x, y = 17, SNOW_Y - 6
        for i in range(18):
            if y < 1 or x >= B1 - 1:
                break
            if g.v[x, y, B0]:
                g.v[x, y, B0] = 0
                g.v[x, y, B0 + 1] = fr[3]
            y -= 1
            if rng.random() < 0.4:
                x += int(rng.choice([-1, 1]))
        # 떨어진 조각 하나가 발치에 (눈 덮임)
        box(g, 3, 8, 0, 3, 0, 3, st[3])
        box(g, 3, 8, 3, 4, 0, 3, sn[3])
        return g


    WALLS = {'Base': wall_base, 'A': wall_icicles, 'B': wall_slit, 'C': wall_frostbreak}


    # ================================================================ 소품
    def prop_light():
        """빛 · 차가운 푸른 벽등: 서리 낀 무쇠 벽등, 차갑고 창백한 푸른 불꽃, 아래로 작은 고드름. 벽에 건다."""
        g = prop_grid(11, 20, 6, 'hang')
        ir, cold, fr, ice = P['iron'], P['cold'], P['frost'], P['ice']
        # 벽 받침 + 팔
        box(g, 4, 7, 11, 18, 5, 6, ir[1])
        box(g, 5, 6, 16, 17, 1, 5, ir[2])
        # 새장꼴 등: 위아래 판 + 네 귀 기둥 + 가운데 창살(열려 있어 불꽃이 보인다)
        x0, x1, y0, y1, z0, z1 = 2, 9, 6, 14, 0, 5
        box(g, x0 - 1, x1 + 1, y1, y1 + 1, z0, z1, ir[2])           # 지붕판
        box(g, x0, x1, y1 + 1, y1 + 2, z0 + 1, z1 - 1, ir[3])
        box(g, x0 + 2, x1 - 2, y1 + 2, y1 + 3, z0 + 1, z1 - 1, ir[2])
        box(g, x0 - 1, x1 + 1, y0, y0 + 1, z0, z1, ir[2])           # 바닥판
        for (x, z) in ((x0, z0), (x1 - 1, z0), (x0, z1 - 1), (x1 - 1, z1 - 1)):
            box(g, x, x + 1, y0, y1, z, z + 1, ir[2])
        for y in range(y0, y1):
            put(g, (x0 + x1) // 2, y, z0, ir[1])                      # 앞 창살
        # 차가운 푸른 불꽃(심지 위, 위로 갈수록 희다)
        fx, fz = (x0 + x1) // 2, (z0 + z1) // 2
        put(g, fx, y0 + 1, fz, ir[0])
        for (y, c) in ((y0 + 2, cold[0]), (y0 + 3, cold[0]), (y0 + 4, cold[1]), (y0 + 5, cold[1])):
            put(g, fx, y, fz, c)
        put(g, fx - 1, y0 + 2, fz, cold[0])
        put(g, fx + 1, y0 + 2, fz, cold[0])
        put(g, fx, y0 + 2, fz - 1, cold[0])
        # 서리: 지붕 위
        for x in range(x0 - 1, x1 + 1):
            for z in range(z0, z1):
                if (x + z) % 3:
                    g.v[x, y1, z] = fr[2]
        # 고드름: 바닥판 밑으로
        for (x, n) in ((x0, 4), (x0 + 2, 2), (x0 + 4, 5), (x1 - 2, 3), (x1, 2)):
            for i in range(n):
                put(g, x, y0 - 1 - i, z0, ice[2] if i < n - 1 else ice[1])
        return g


    def prop_wall():
        """벽 소품 · 얼어붙은 창 걸이: 나무 걸이에 걸린 낡은 창 둘, 전부 얼음과 서리에 덮였다."""
        g = prop_grid(20, 20, 6, 'hang')
        wd, ir, fr, ice, sn = P['wood'], P['iron'], P['frost'], P['ice'], P['snow']
        # 가로 걸이 둘(위·아래), 창을 받치는 못
        for y in (13, 4):
            box(g, 0, 20, y, y + 2, 3, 6, wd[2])
            box(g, 0, 20, y + 1, y + 2, 3, 6, wd[3])
            for x in (2, 9, 16):
                box(g, x, x + 2, y + 2, y + 3, 2, 4, wd[1])
        # 창 둘: 곧게, 대가 걸이 앞을 지난다
        for sx in (5, 13):
            for y in range(0, 17):
                put(g, sx, y, 1, wd[3] if y % 5 else wd[2])
                put(g, sx, y, 2, wd[2])
            for (dy, w) in ((17, 3), (18, 3), (19, 1)):
                for dx in range(-(w // 2), w // 2 + 1):
                    put(g, sx + dx, dy, 1, ir[4] if dx <= 0 else ir[3])
            for dx in (-1, 0, 1):
                put(g, sx + dx, 16, 1, ir[1])
        # 얼음·서리: 걸이 위를 덮고, 고드름이 걸이에서 내려온다
        for y in (15, 6):
            for x in range(0, 20):
                if (x * 7) % 5 != 0:
                    put(g, x, y, 3, sn[3])
                    put(g, x, y, 4, sn[2])
        for (x, y0, n) in ((1, 12, 3), (8, 12, 2), (11, 12, 4), (18, 12, 2), (3, 3, 2), (15, 3, 3)):
            for i in range(n):
                put(g, x, y0 - i, 3, ice[2] if i < n - 1 else ice[1])
        for (x, y) in ((5, 10), (5, 11), (13, 9), (13, 2), (5, 1)):
            put(g, x, y, 1, fr[2])
        return g


    def prop_floor():
        """바닥 · 얼음에 갇힌 걸상: 맑고 푸른 얼음덩이 속 작은 나무 걸상. 발목 높이.
        복셀은 비치지 않으니 속의 걸상 그림자를 얼음 겉칸에 비쳐 그린다."""
        g = prop_grid(13, 8, 11, 'floor')
        ice, wd, sn = P['ice'], P['wood'], P['snow']
        for x in range(13):
            for y in range(8):
                for z in range(11):
                    cut = (x in (0, 12)) + (y == 7) + (z in (0, 10))
                    if cut >= 2:
                        continue
                    g.v[x, y, z] = ice[1] if y < 7 else ice[2]
        seat_y, legs = 5, ((3, 2), (9, 2), (3, 8), (9, 8))

        def stool(x, y, z):
            if y == seat_y and 2 <= x <= 10 and 2 <= z <= 8:
                return True
            for (lx, lz) in legs:
                if y < seat_y and lx <= x <= lx + 1 and lz <= z <= lz + 1:
                    return True
            if y == 2 and ((2 <= x <= 10 and z in (2, 8)) or (x in (3, 9) and 2 <= z <= 8)):
                return True
            return False
        for x in range(13):
            for y in range(8):
                for z in range(11):
                    if not g.v[x, y, z]:
                        continue
                    iw = P['icewood']
                    if z == 0 or (z == 1 and x in (0, 12)):
                        if any(stool(x, y, zz) for zz in range(2, 9)):
                            g.v[x, y, z] = iw[1] if y == seat_y else iw[0]
                    if y == 7 or (y == 6 and x in (0, 12)):
                        if any(stool(x, yy, z) for yy in range(0, 7)):
                            g.v[x, y, z] = iw[2] if y == 7 else iw[1]
        # 얼음 윗면의 비스듬한 빛줄기(매끈한 얼음)
        for x in range(13):
            for z in range(11):
                if (x - z) % 6 == 0 and g.v[x, 7, z] in (ice[2],):
                    g.v[x, 7, z] = ice[3]
        for (x, z) in ((2, 3), (6, 1), (10, 7), (4, 9)):
            put(g, x, 7, z, ice[3])
        for (x, z) in ((1, 1), (11, 9), (12, 5)):
            put(g, x, 0, z, sn[3])
        return g


    def prop_big():
        """바닥(벽 옆 빈칸) · 갑옷 걸이: 찌그러진 경비 투구와 흉갑을 걸친 빈 갑옷 걸이, 얼고 서리 꼈다."""
        g = prop_grid(20, 28, 14, 'big')
        wd, ir, fr, sn, ice = P['wood'], P['iron'], P['frost'], P['snow'], P['ice']
        box(g, 2, 18, 0, 2, 6, 8, wd[1])                          # 십자 발
        box(g, 9, 11, 0, 2, 1, 13, wd[1])
        box(g, 9, 11, 2, 24, 6, 8, wd[2])                         # 기둥
        box(g, 3, 17, 19, 20, 6, 8, wd[2])                        # 어깨 가로대
        # 흉갑: 가운데가 앞으로 불룩한 판
        for x in range(4, 16):
            for y in range(9, 20):
                dx = (x + 0.5 - 10) / 6.0
                if y < 11 and abs(dx) > 0.75:
                    continue
                front = 3 - int(round(2.2 * (1 - dx * dx)))
                for z in range(front, 9):
                    if z in (front, front + 1) or x in (4, 15) or y in (9, 19):
                        g.v[x, y, z] = ir[3] if z == front else ir[2]
        for y in range(10, 19):
            put(g, 10, y, 1, ir[4])                               # 세로 능선
        for (x, y) in ((7, 14), (7, 13), (8, 13)):                # 찌그러진 자국
            put(g, x, y, 1, ir[1])
            put(g, x, y, 2, ir[1])
        box(g, 2, 6, 18, 21, 4, 10, ir[2])                        # 어깨 판
        box(g, 14, 18, 18, 21, 4, 10, ir[2])
        # 투구: 둥근 사발 + 챙 + 코 가리개
        for x in range(6, 14):
            for y in range(22, 28):
                for z in range(3, 11):
                    d = ((x + 0.5 - 10) / 4.0) ** 2 + ((y + 0.5 - 22.5) / 5.0) ** 2 + ((z + 0.5 - 7) / 4.0) ** 2
                    if d <= 1.0:
                        g.v[x, y, z] = ir[3] if (z < 6 or y > 25) else ir[2]
        box(g, 5, 15, 22, 23, 2, 12, ir[2])
        box(g, 9, 11, 19, 23, 2, 3, ir[3])
        # 눈과 서리: 위를 향한 맨 윗칸
        for x in range(20):
            for z in range(14):
                col = np.nonzero(g.v[x, :, z])[0]
                if len(col):
                    y = col.max()
                    g.v[x, y, z] = sn[3] if y > 18 else fr[2]
        # 고드름: 어깨판·챙 밑
        for (x, y0, n) in ((3, 17, 2), (16, 17, 3), (6, 21, 2), (13, 21, 2)):
            for i in range(n):
                put(g, x, y0 - i, 4, ice[2])
        return g


    def prop_light2():
        """[선택] 빛 2 · 마지막 불씨 화로(바닥): 작은 무쇠 화로, 잿빛 재 속 마지막 불씨 몇 알, 테두리로 서리. 발목 높이."""
        g = prop_grid(13, 7, 13, 'floor')
        ir, ash, em, fr = P['iron'], P['ash'], P['ember'], P['frost']
        cx = cz = 6.5
        for (x, z) in ((6, 1), (2, 9), (10, 9)):
            box(g, x, x + 1, 0, 2, z, z + 1, ir[1])
        for x in range(13):
            for z in range(13):
                d = math.hypot(x + 0.5 - cx, z + 0.5 - cz)
                if d <= 4.4:
                    g.v[x, 2, z] = ir[1]
                if 4.0 < d <= 5.4:
                    g.v[x, 3, z] = ir[2]
                if 4.6 < d <= 6.3:
                    g.v[x, 4, z] = ir[2]
                    g.v[x, 5, z] = fr[2] if (x * 3 + z) % 3 else ir[3]
                if d <= 4.9:
                    g.v[x, 3, z] = ash[0]
                    g.v[x, 4, z] = ash[1] if (x + z) % 3 else ash[2]
        for (x, z, c) in ((6, 6, 2), (7, 6, 1), (6, 7, 1), (5, 5, 0), (8, 8, 0)):
            g.v[x, 4, z] = em[c]
        put(g, 6, 5, 6, em[1])
        for (x, z) in ((0, 6), (12, 7), (6, 0), (7, 12), (1, 3), (11, 2)):
            put(g, x, 0, z, fr[1])
        return g


    PROPS = {'Light': (prop_light, 'light'), 'Wall': (prop_wall, 'hang'),
             'Floor': (prop_floor, 'floor'), 'Big': (prop_big, 'big'), 'Light2': (prop_light2, 'floor')}

    return types.SimpleNamespace(P=P, WALLS=WALLS, PROPS=PROPS)


# ================================================================ chapter 4
def _chapter4():
    """챕터 4 — 왕좌의 균열. 검은 태양이 낸 틈: 금 간 흑요석, 보랏빛 광택. 모든 것이 위로 떠오르려 한다.
    사람이 만든 것처럼 보이지 않게 — 벽돌·줄눈·다듬은 모서리가 없다. 왕좌는 만들지 않는다."""
    def palette():
        p = Palette()
        p.ramp('obs', ['#151120', '#201b2d', '#2a243a', '#352e48', '#413957'])
        p.ramp('sheen', ['#55487a', '#7262ab', '#9583d0', '#b9aaec', '#e2d8fb'])
        p.ramp('glow', ['#7a2cf0', '#b46cff', '#e6c4ff'], emit=True)
        p.ramp('crystal', ['#5b2bb8', '#8a4cf0', '#b98aff', '#e8d6ff'], emit=True)
        p.ramp('rock', ['#1b1820', '#24202b', '#2e2936', '#393342'])
        p.ramp('iron', ['#34323d', '#46444f', '#5b5966', '#76748a'])
        p.ramp('step', ['#36313f', '#453f51', '#575066', '#6b637e'])
        return p


    P = palette()
    TOP_SEED = 5000


    def _voronoi_wrap(size, n, rng):
        """주기 size 의 보로노이 (이웃 벽과 이어지게). 반환: 칸마다 조각 번호."""
        pts = rng.random((n, 2)) * size
        X, Z = np.meshgrid(np.arange(size) + 0.5, np.arange(size) + 0.5, indexing='ij')
        best = np.full((size, size), 1e9)
        lab = np.zeros((size, size), int)
        for i, (px, pz) in enumerate(pts):
            dx = np.abs(X - px)
            dz = np.abs(Z - pz)
            dx = np.minimum(dx, size - dx)
            dz = np.minimum(dz, size - dz)
            d = dx * dx + dz * dz * 1.0
            m = d < best
            best[m] = d[m]
            lab[m] = i
        return lab


    def _voronoi(shape, n, rng, stretch=(1.0, 1.0)):
        pts = rng.random((n, 2)) * np.array(shape)
        X, Y = np.meshgrid(np.arange(shape[0]) + 0.5, np.arange(shape[1]) + 0.5, indexing='ij')
        best = np.full(shape, 1e9)
        lab = np.zeros(shape, int)
        for i, (px, py) in enumerate(pts):
            d = ((X - px) * stretch[0]) ** 2 + ((Y - py) * stretch[1]) ** 2
            m = d < best
            best[m] = d[m]
            lab[m] = i
        return lab


    def _boundary(lab):
        b = np.zeros(lab.shape, bool)
        b[1:, :] |= lab[1:, :] != lab[:-1, :]
        b[:, 1:] |= lab[:, 1:] != lab[:, :-1]
        return b


    def _boundary_wrap(lab):
        return (lab != np.roll(lab, 1, 0)) | (lab != np.roll(lab, 1, 1))


    def _base(seed, top_y=H):
        """흑요석 덩어리: 남쪽 면은 깎인 조각면(보로노이)과 금, 윗면은 이웃과 이어지는 조각면 + 광택."""
        rng = R(seed)
        g = wall_grid()
        ob, sh = P['obs'], P['sheen']
        box(g, B0, B1, 0, top_y - 1, B0, B1, ob[2])
        # 남쪽 면: 세로로 긴 조각면(위로 솟는 결)
        lab = _voronoi((GX, GY), 10, rng, stretch=(1.0, 0.55))
        bnd = _boundary(lab)
        depth = rng.integers(0, 2, size=lab.max() + 1)
        tone = rng.choice([1, 2, 3], size=lab.max() + 1, p=[0.3, 0.45, 0.25])
        for x in range(B0, B1):
            for y in range(0, top_y - 1):
                L = lab[x, y]
                if bnd[x, y]:
                    g.v[x, y, B0] = 0
                    g.v[x, y, B0 + 1] = ob[0]
                    continue
                if depth[L]:
                    g.v[x, y, B0] = 0
                    g.v[x, y, B0 + 1] = ob[tone[L]]
                else:
                    g.v[x, y, B0] = ob[tone[L]]
        # 조각면마다 윗가장자리(하늘을 향한 턱)와 서쪽 가장자리(낮은 해)에만 광택 — 나머지는 검다
        for x in range(B0, B1):
            for y in range(0, top_y - 1):
                if bnd[x, y]:
                    continue
                L = lab[x, y]
                z = B0 if g.v[x, y, B0] else B0 + 1
                up = y + 1 < GY and (lab[x, y + 1] != L or bnd[x, y + 1])
                west = x - 1 >= 0 and (lab[x - 1, y] != L or bnd[x - 1, y])
                if up and west:
                    g.v[x, y, z] = sh[3]
                elif up:
                    g.v[x, y, z] = sh[2]
                elif west:
                    g.v[x, y, z] = sh[1]
        # 세로 모서리 광택(남쪽 면 양 끝)
        for y in range(0, top_y - 1):
            for x in (B0, B1 - 1):
                if g.v[x, y, B0] and y % 4 != 0:
                    g.v[x, y, B0] = sh[0]
        top_slab(g, seed, top_y - 1)
        return g, rng


    def top_slab(g, seed, y, lab_seed=TOP_SEED):
        """윗면 한 겹(발자국 밖 한 칸까지): 이웃과 이어지는 검은 조각면. 조각 사이는 한 칸 파인 금,
        조각마다 해(서쪽 낮은 곳)와 하늘(북쪽)을 향한 가장자리에만 보랏빛 광택 — 매끈한 흑요석의 모서리."""
        ob, sh = P['obs'], P['sheen']
        lab = _voronoi_wrap(TOPW, 13, R(lab_seed))
        bnd = _boundary_wrap(lab)
        tone = R(lab_seed + 1).choice([2, 3, 3, 4], size=lab.max() + 1)
        for x in range(TOPW):
            for z in range(TOPW):
                L = lab[x, z]
                if bnd[x, z]:
                    g.v[x, y, z] = ob[0]          # 금은 칠만 한다 — 파면 옆면 삼각형이 벽 예산을 넘는다
                    continue
                # 서쪽 가장자리에서 몇 칸째인가 — 낮은 해를 받는 쪽만 넓게 빛난다(그물처럼 테를 다 두르지 않는다)
                k = 0
                while k < 3 and lab[(x - 1 - k) % TOPW, z] == L and not bnd[(x - 1 - k) % TOPW, z]:
                    k += 1
                c = (sh[3], sh[2], sh[1])[k] if k < 3 else ob[tone[L]]
                g.v[x, y, z] = c


    def float_bits(g, spots, ytop=32):
        """윗면 위에 뜬 작은 돌 조각: (x, z, w, d, h) — 바닥은 ytop-h+1, 한 칸 띄운다."""
        ob, sh = P['obs'], P['sheen']
        for (x, z, w, d, h) in spots:
            y0 = ytop - h + 1
            for i in range(w):
                for k in range(d):
                    for j in range(h):
                        top = j == h - 1
                        g.v[x + i, y0 + j, z + k] = sh[3] if (top and i == 0) else sh[1] if top else ob[3]


    def wall_base():
        g, rng = _base(5001)
        float_bits(g, [(5, 7, 2, 2, 2), (19, 21, 2, 1, 1), (24, 9, 1, 2, 2), (11, 25, 1, 1, 1)])
        return g


    def wall_glowcrack():
        """빛 균열 벽: 남쪽 면을 타고 위로 갈라져 오르는 가는 보랏빛 균열(빛난다), 윗면까지 이어진다."""
        g, rng = _base(5002)
        gl, sh = P['glow'], P['sheen']

        def crack(x, y, n, drift):
            pts = []
            for i in range(n):
                pts.append((x, y))
                y += 1
                if rng.random() < 0.5:
                    x += drift if rng.random() < 0.7 else -drift
                x = min(max(x, B0 + 1), B1 - 2)
                if y >= H - 1:
                    break
            return pts
        paths = crack(9, 0, 30, 1) + crack(21, 0, 22, -1) + crack(15, 9, 14, 1)
        for i, (x, y) in enumerate(paths):
            # 겉면에서 빛난다(파인 금 속에 두면 60도 카메라에서는 안 보인다): 가운데 밝게, 옆 한 칸은 테두리 빛
            g.v[x, y, B0] = gl[2] if y % 4 else gl[1]
            g.v[x, y, B0 + 1] = gl[1]
            for dx in (-1, 1):
                c = g.v[x + dx, y, B0]
                if c and c not in gl:
                    g.v[x + dx, y, B0] = gl[0] if (y + dx) % 3 == 0 else sh[2]
        # 윗면으로 이어지는 균열 두 줄
        for (x0, x1) in ((9, 14), (21, 17)):
            x = x0
            for z in range(B0 - 1, 17):
                g.v[x, H - 1, z] = gl[2] if z % 3 else gl[1]
                if rng.random() < 0.4:
                    x += 1 if x1 > x0 else -1
        float_bits(g, [(6, 20, 2, 2, 2), (24, 24, 1, 2, 1)])
        return g


    def wall_spikes():
        """가시 벽: 윗부분이 내려앉은 자리에서 날카로운 흑요석 가시가 곧게 솟는다.
        위로 0.3 까지만 허락되므로 덩어리 윗면을 낮추고 가시가 그 높이를 채운다."""
        g, rng = _base(5003, top_y=21)
        ob, sh = P['obs'], P['sheen']
        floor_y = 20
        top_slab(g, 5003, floor_y, lab_seed=TOP_SEED + 50)
        # 꺼진 바닥은 어둡게 — 가시가 그 위에서 도드라진다
        for x in range(GX):
            for z in range(GZ):
                if g.v[x, floor_y, z] in P['sheen']:
                    g.v[x, floor_y, z] = P['obs'][2]
        # (x, z, 밑동, 높이) — 가운데가 가장 높다
        spikes = [(15, 15, 5, 13), (9, 11, 4, 11), (22, 10, 4, 12), (10, 22, 4, 10), (21, 21, 4, 12),
                  (5, 16, 3, 8), (26, 16, 3, 9), (16, 5, 3, 9), (16, 26, 3, 8), (5, 5, 2, 6), (27, 27, 2, 6),
                  (27, 5, 3, 7), (5, 27, 2, 7), (12, 17, 2, 7), (19, 13, 2, 7)]
        for (cx, cz, b, h) in spikes:
            for j in range(h):
                y = floor_y + 1 + j
                if y > 32:
                    break
                w = max(1, int(round(b - (b - 1) * (j / max(1, h - 1)) ** 0.75)))
                x0 = cx - w // 2
                z0 = cz - w // 2
                for x in range(x0, x0 + w):
                    for z in range(z0, z0 + w):
                        lit = (x == x0)
                        front = (z == z0)
                        if lit:
                            c = sh[4] if j > h * 0.6 else sh[3]
                        elif front:
                            c = sh[2] if j > h * 0.4 else sh[1]
                        else:
                            c = ob[4]
                        g.v[x, y, z] = c
            top = min(floor_y + h, 32)
            g.v[cx - (b > 1) * 0, top, cz] = sh[4]
        return g


    def wall_shattered():
        """부서진 벽: 윗부분이 큰 덩어리 넷으로 부서져 위로 떠올랐다 — 덩어리 사이로 어둠이 보인다."""
        g, rng = _base(5004)
        ob, sh = P['obs'], P['sheen']
        break_y = 21
        g.v[:, break_y:, :] = 0
        # 부러진 단면: 한두 칸 들쭉날쭉, 군데군데 광택
        n = noise2((GX, GZ), 5, rng, 1)
        for x in range(B0, B1):
            for z in range(B0, B1):
                h = break_y - 1 - (1 if n[x, z] < 0.35 else 0)
                g.v[x, h + 1:break_y, z] = 0
                g.v[x, h, z] = sh[0] if n[x, z] > 0.62 else ob[2]
        # 덩어리: 남쪽 면에서 이어지던 결 그대로, 각자 다른 높이에
        # 덩어리는 원래 윗면(발자국 밖 한 칸까지 덮던 판)에서 떨어져 나왔다 — 그래서 경계가 -1.6..1.6 그대로다
        chunks = [  # x0, x1, z0, z1, 바닥 y, 두께
            (0, 14, 0, 13, 25, 5),
            (15, 32, 0, 11, 27, 5),
            (0, 16, 15, 32, 26, 6),
            (18, 32, 13, 32, 24, 5),
        ]
        for (x0, x1, z0, z1, y0, t) in chunks:
            sub = Grid((x1 - x0, t, z1 - z0), (0, 0, 0))
            # 모서리를 한두 칸 깎아 덩어리처럼
            for x in range(x0, x1):
                for z in range(z0, z1):
                    cut = 1 if (x in (x0, x1 - 1) or z in (z0, z1 - 1)) else 0
                    for j in range(cut, t):
                        y = y0 + j
                        if y > 32:
                            continue
                        g.v[x, y, z] = sh[0] if j == cut else ob[2] if j < t - 1 else ob[3]
        # 덩어리 윗면에 광택 결(이웃 벽 윗면과 같은 무늬를 높이만 올린 것)
        lab = _voronoi_wrap(TOPW, 9, R(TOP_SEED))
        hm = heightmap(g)
        tmp = wall_grid()
        top_slab(tmp, 0, 10)
        for x in range(GX):
            for z in range(GZ):
                y = hm[x, z]
                if y >= break_y and tmp.v[x, 10, z]:
                    g.v[x, y, z] = tmp.v[x, 10, z]
        return g


    WALLS = {'Base': wall_base, 'A': wall_glowcrack, 'B': wall_spikes, 'C': wall_shattered}


    # ================================================================ 소품
    def prop_light():
        """빛 · 보랏빛 수정 무리(바닥): 검은 바위에서 돋아난 빛나는 보랏빛 수정 다발. 발목 높이."""
        g = prop_grid(15, 8, 13, 'floor')
        rk, cr = P['rock'], P['crystal']
        # 바위: 낮고 넓은 덩어리
        for x in range(15):
            for z in range(13):
                d = ((x + 0.5 - 7.5) / 7.0) ** 2 + ((z + 0.5 - 6.5) / 6.0) ** 2
                if d <= 1.0:
                    g.v[x, 0, z] = rk[1]
                if d <= 0.55:
                    g.v[x, 1, z] = rk[2] if (x + z) % 3 else rk[3]
                if d <= 0.18:
                    g.v[x, 2, z] = rk[2]
        # 수정: (x, z, 높이, 기울기 dx, dz) — 바깥쪽으로 비스듬히 뻗는다
        shards = [(7, 6, 7, 0, 0), (5, 5, 5, -1, -1), (10, 7, 6, 1, 0), (8, 9, 4, 0, 1), (4, 8, 4, -1, 1), (10, 4, 4, 1, -1)]
        for (cx, cz, h, dx, dz) in shards:
            for j in range(h):
                y = 1 + j
                ox = cx + (dx * j) // 3
                oz = cz + (dz * j) // 3
                w = 2 if j < h - 2 else 1
                for x in range(ox, ox + w):
                    for z in range(oz, oz + w):
                        if y < 8:
                            lit = (x == ox)
                            g.v[x, y, z] = cr[3] if j >= h - 1 else cr[2] if lit else cr[1]
            put(g, cx + (dx * h) // 3, min(7, h), cz + (dz * h) // 3, cr[3])
        return g


    def prop_wall():
        """벽 소품 · 위로 뜬 쇠사슬: 벽 고리에 매인 쇠사슬, 풀린 끝이 중력이 거꾸로인 듯 곧게 위로 떠 있다."""
        g = prop_grid(9, 20, 4, 'hang')
        ir, sh = P['iron'], P['sheen']
        # 벽 고리(아래쪽): 박힌 쇠 + 고리
        box(g, 3, 6, 0, 3, 3, 4, ir[1])
        for (x, y) in ((3, 2), (4, 2), (5, 2), (2, 3), (6, 3), (2, 4), (6, 4), (3, 5), (5, 5)):
            put(g, x, y, 2, ir[3])
        # 사슬: 아래에서 위로, 고리 사이가 조금씩 벌어진다(떠오르는 중)
        y = 5
        k = 0
        gaps = [0, 0, 1, 1, 1, 2]
        while y <= 17 and k < len(gaps):
            if k % 2 == 0:
                for (dx, dy) in ((-1, 0), (0, 0), (1, 0), (-1, 1), (1, 1), (-1, 2), (0, 2), (1, 2)):
                    put(g, 4 + dx, y + dy, 2, sh[2] if dx == -1 else sh[1] if dy == 2 else ir[3])
            else:
                for dy in (0, 1, 2):
                    put(g, 4, y + dy, 1, ir[2])
                    put(g, 4, y + dy, 3, ir[1])
                put(g, 4, y, 2, ir[2])
                put(g, 4, y + 2, 2, ir[2])
            y += 2 + gaps[k]
            k += 1
        # 맨 끝 고리: 떨어져 떠 있고, 윗면이 보랏빛을 받는다
        for (dx, dy) in ((-1, 0), (0, 0), (1, 0), (-1, 1), (1, 1), (-1, 2), (0, 2), (1, 2)):
            put(g, 4 + dx, 17 + dy, 2, sh[2] if dy == 2 else ir[3])
        return g


    def prop_floor():
        """바닥 위 · 떠 있는 돌 조각: 아래가 뾰족한 흑요석 덩어리가 바닥 위에 떠 있다. 밑에는 떨어져 나온 부스러기. 발목 높이."""
        g = prop_grid(11, 8, 10, 'floor')
        ob, sh, rk = P['obs'], P['sheen'], P['rock']
        # 바닥 부스러기 — 뾰족한 끝에서 비켜 있다(떠 있음이 보이게)
        for (x, z) in ((1, 6), (2, 6), (2, 7), (3, 8), (8, 2), (9, 2)):
            g.v[x, 0, z] = rk[2] if (x + z) % 2 else rk[3]
        # 떠 있는 덩어리: 위 두 줄은 넓고, 아래로 뒤집힌 피라미드처럼 좁아진다(끝은 y 2, 바닥과 한 칸 띄움)
        rows = {2: (5, 6, 4, 5), 3: (4, 7, 3, 6), 4: (3, 8, 2, 7), 5: (2, 9, 1, 8), 6: (1, 10, 1, 9), 7: (2, 9, 2, 8)}
        for y, (x0, x1, z0, z1) in rows.items():
            for x in range(x0, x1):
                for z in range(z0, z1):
                    g.v[x, y, z] = ob[2] if y < 6 else ob[3]
        # 뒤집힌 비탈의 서쪽 면은 보랏빛을 받는다
        for y in range(2, 6):
            x0 = rows[y][0]
            for z in range(rows[y][2], rows[y][3]):
                g.v[x0, y, z] = sh[1] if y < 5 else sh[2]
        # 윗면: 서쪽이 넓게 빛난다(한 칸 낮은 턱이 하나 — 들쭉날쭉한 윗면)
        for x in range(1, 10):
            for z in range(1, 9):
                y = 7 if g.v[x, 7, z] else 6
                if not g.v[x, y, z]:
                    continue
                t = (x - 1) / 8.0 - (z - 4.5) * 0.04
                g.v[x, y, z] = sh[3] if t < 0.2 else sh[2] if t < 0.42 else sh[1] if t < 0.62 else ob[4]
        for (x, z) in ((7, 6), (8, 6), (7, 7), (8, 7)):
            g.v[x, 7, z] = 0                       # 깨진 귀퉁이
        return g


    def prop_big():
        """바닥(벽 옆 빈칸) · 부서진 계단 조각: 금 간 어두운 돌계단 세 단. 맨 윗단은 떨어져 조금 떠 있다."""
        g = prop_grid(26, 22, 22, 'big')
        stp, sh, ob = P['step'], P['sheen'], P['obs']
        # 세 단: 북쪽으로 오른다(앞이 낮다). (z0, z1, 높이)
        steps = [(0, 22, 6), (7, 22, 12), (14, 22, 17)]
        for i, (z0, z1, h) in enumerate(steps):
            for x in range(1, 25):
                for z in range(z0, z1):
                    for y in range(0, h):
                        g.v[x, y, z] = stp[1]
                    g.v[x, h - 1, z] = sh[2] if z < z0 + 1 else sh[0] if z < z0 + 3 else stp[3]   # 디딤판: 앞 모서리에 보랏빛
        # 맨 윗단은 떨어져 떠 있다: y 17..19 사이를 비우고 위로 2칸 올린다
        top = g.v[:, 12:17, 14:22].copy()
        g.v[:, 12:17, 14:22] = 0
        for x in range(1, 25):
            for z in range(14, 22):
                g.v[x, 12, z] = stp[2] if z < 16 else stp[1]
        g.v[:, 14:19, 14:22] = top
        # 금: 앞면과 디딤판을 가로지르는 균열(위로 갈라진다)
        for (x0, zs) in ((9, range(0, 7)), (17, range(7, 14))):
            x = x0
            for z in zs:
                for y in range(0, 20):
                    if g.v[x, y, z] and (y == 0 or g.v[x, y + 1, z] == 0):
                        g.v[x, y, z] = ob[0]
                x += 1 if z % 2 else 0
        for y in range(0, 6):
            g.v[9 + (y % 2), y, 0] = ob[0]
        # 부서진 끝: 오른쪽 위 귀퉁이를 덩어리째 떼어 낸다
        g.v[19:26, 9:22, 7:22] = 0
        g.v[21:26, 3:6, 0:7] = 0
        # 떨어진 조각 하나가 위에 떠 있다
        box(g, 20, 24, 20, 22, 9, 13, stp[2])
        for x in range(20, 24):
            g.v[x, 21, 9] = sh[1]
        # 디딤판 앞 모서리에 보랏빛 광택
        for x in range(1, 19):
            for (y, z) in ((5, 0), (11, 7)):
                if g.v[x, y, z]:
                    g.v[x, y, z] = sh[1] if x % 4 else sh[2]
        for x in range(1, 19):
            if g.v[x, 18, 14]:
                g.v[x, 18, 14] = sh[2]
        return g


    PROPS = {'Light': (prop_light, 'floor'), 'Wall': (prop_wall, 'hang'),
             'Floor': (prop_floor, 'floor'), 'Big': (prop_big, 'big')}

    return types.SimpleNamespace(P=P, WALLS=WALLS, PROPS=PROPS)


# ================================================================ build: 굽기·미리보기·대조표

OUT = os.path.join(ROOT, 'Assets', '@Resources', 'Art', 'ChapterKit') if ROOT else None
PREV_ROOT = os.path.join(tempfile.gettempdir(), 'voxel_kit_preview')
PREV = os.path.join(PREV_ROOT, 'assets')
SHEETS = os.path.join(PREV_ROOT, 'sheets')
_CHAPTERS = {}


def chapter(ch):
    """챕터 하나의 팔레트·벽·소품(한 번만 짓는다)."""
    if ch not in _CHAPTERS:
        _CHAPTERS[ch] = (_chapter0, _chapter1, _chapter2, _chapter3, _chapter4)[ch]()
    return _CHAPTERS[ch]


CHAPTER_NAMES = {
    0: 'Ch0 Mossy Catacombs (5-20F)',
    1: 'Ch1 Collapsed Aqueduct (21-40F)',
    2: 'Ch2 Ashen Forge (41-60F)',
    3: 'Ch3 Frozen Depths (61-80F)',
    4: 'Ch4 Rift of the Throne (81-100F)',
}

# 문서 항목 (TheSword_Meshy_Prompts) — README 표에 쓴다
DOC = {
    0: {'Base': 'W02 기본형 (이끼 낀 회갈색 묘소 벽돌)', 'A': '금 간 벽', 'B': '벽감 벽 (녹은 초)', 'C': '무너진 벽',
        'Light': '빛 · 벽 촛대', 'Wall': '벽 소품 · 이끼 낀 사슬', 'Floor': '바닥 · 칼이 꽂힌 무덤 표지',
        'Big': '바닥(벽 옆 빈칸) · 닫힌 석관'},
    1: {'Base': 'W02 기본형 (젖은 청회색 돌, 밝은 덮개)', 'A': '배수구 벽', 'B': '창살 벽', 'C': '무너진 벽 (아치 돌)',
        'Light': '빛 · 푸른 벽등', 'Wall': '벽 소품 · 밧줄과 두레박', 'Floor': '바닥(얇게) · 녹슨 바닥 창살',
        'Big': '바닥(벽 옆 빈칸) · 멈춘 물레바퀴 조각'},
    2: {'Base': 'W02 기본형 (그을린 무쇠·벽돌, 흰 재)', 'A': '리벳 벽', 'B': '굴뚝 벽', 'C': '무너진 벽 (식은 슬래그)',
        'Light': '빛 · 화로 (바닥)', 'Wall': '벽 소품 · 도르래 사슬과 갈고리', 'Floor': '바닥 · 슬래그 더미',
        'Extra': '[선택] 바닥 · 휜 칼 모양 빈 거푸집', 'Big': '바닥(벽 옆 빈칸) · 가죽 풀무'},
    3: {'Base': 'W02 기본형 (서리 낀 돌, 눈 모자)', 'A': '고드름 벽', 'B': '화살 구멍 벽', 'C': '무너진 벽 (서리 금, 눈)',
        'Light': '빛 · 차가운 푸른 벽등', 'Wall': '벽 소품 · 얼어붙은 창 걸이', 'Floor': '바닥 · 얼음에 갇힌 걸상',
        'Big': '바닥(벽 옆 빈칸) · 갑옷 걸이', 'Light2': '[선택] 빛 2 · 마지막 불씨 화로 (바닥)'},
    4: {'Base': 'W02 기본형 (금 간 흑요석, 보랏빛 광택, 뜬 조각)', 'A': '빛 균열 벽', 'B': '가시 벽', 'C': '부서진 벽 (떠오른 덩어리)',
        'Light': '빛 · 보랏빛 수정 무리 (바닥)', 'Wall': '벽 소품 · 위로 뜬 쇠사슬', 'Floor': '바닥 위 · 떠 있는 돌 조각',
        'Big': '바닥(벽 옆 빈칸) · 부서진 계단 조각'},
}


def normalize(g, kind):
    """빈 가장자리를 잘라 원점을 약속대로 둔다: 거는 것은 x 가운데·아래끝 y 2.0·등(max z) 0,
    바닥·큰 소품은 x/z 가운데·아래끝 y 0. (들이개는 경계로 붙이므로 게임 모습은 같다 — 파일이 약속과 맞게.)"""
    c = g.crop()
    w, h, d = c.v.shape
    if kind in ('hang', 'light'):
        c.origin = np.array([-w * VOX / 2, 2.0, -d * VOX])
    else:
        c.origin = np.array([-w * VOX / 2, 0.0, -d * VOX / 2])
    return c


def build_assets(ch, write=True):
    """그 챕터의 벽·소품을 지어 검사하고 쓴다. 반환: [(이름, grid, kind, info)]"""
    mod = chapter(ch)
    out = []
    for var, fn in mod.WALLS.items():
        g = fn()
        lo, hi = g.bounds()
        lo, hi = np.round(lo, 3), np.round(hi, 3)
        # 벽은 칸에 꼭 맞아야 한다: 바닥 가운데로 붙이는 들이개가 어긋나지 않게 대칭이다
        assert lo[0] == -1.6 and hi[0] == 1.6 and lo[2] == -1.6 and hi[2] == 1.6, ('wall bounds', ch, var, lo, hi)
        assert lo[1] == 2.0 and hi[1] <= 5.3 + 1e-6, ('wall height', ch, var, lo, hi)
        out.append(('Wall_C%d_%s' % (ch, var), g, 'wall', {'bounds': (lo.tolist(), hi.tolist())}))
    for slot, (fn, kind) in mod.PROPS.items():
        g = normalize(fn(), kind)
        size = check_fit(g, kind, 'C%d %s' % (ch, slot))
        lo, hi = g.bounds()
        out.append(('Prop_C%d_%s' % (ch, slot), g, kind,
                    {'bounds': (np.round(lo, 3).tolist(), np.round(hi, 3).tolist()), 'voxels': size}))
    stats = []
    for name, g, kind, info in out:
        if write:
            os.makedirs(OUT, exist_ok=True)
            tris, emits = write_asset(os.path.join(OUT, name), g, mod.P, title=name)
        else:
            tris, emits = 2 * len(quads(g)), False
        budget = 3000 if kind == 'wall' else 2000
        assert tris <= budget, (name, tris)
        used = sorted(set(int(c) for c in np.unique(g.v)) - {0})
        mats = sorted(set(mod.P.names[c] for c in used))
        info.update(tris=tris, emits=emits, colors=len(used), materials=mats)
        stats.append((name, kind, info))
    return out, stats, mod


# ---------------------------------------------------------------- 미리보기
def place_extra(g, kind):
    """MeshyArtImport.Rule 과 같은 자리: 벽·큰 소품·바닥 소품은 바닥 가운데, 거는 것은 남쪽 벽면 가운데에 등."""
    lo, hi = g.bounds()
    cx, cz = (lo[0] + hi[0]) / 2, (lo[2] + hi[2]) / 2
    if kind in ('hang', 'light'):
        return (-cx, 3.5 - (lo[1] + hi[1]) / 2, -1.5 - hi[2])
    if kind == 'wall':
        return (-cx, 2.0 - lo[1], -cz)
    return (-cx, 2.0 - lo[1], -cz)


LAYOUT = [
    "WWWWWWWWWWW",
    "WLAWHBWGWCW",
    "...........",
    ".f..W..e.l.",
    "WW..W...WWW",
    "....W...W..",
    "WWWWWWWWWWW",
]


def scene_items(ch, assets):
    by = {n: (g, k) for n, g, k, _ in assets}
    mod = chapter(ch)
    pal = mod.P
    W = lambda v: by['Wall_C%d_%s' % (ch, v)]
    prop = lambda s: by.get('Prop_C%d_%s' % (ch, s))
    light = prop('Light')
    light_on_wall = light is not None and light[1] == 'light'
    items = []
    for r, row in enumerate(LAYOUT):
        for c, code in enumerate(row):
            base = np.array([c * CELL, 0.0, -r * CELL])
            def add(gk):
                g, k = gk
                items.append((g, base + np.array(place_extra(g, 'wall' if k == 'wall' else k)), pal))
            if code in 'WLH':
                add(W('Base'))
                hang = None
                if code == 'L' and light_on_wall:
                    hang = light
                if code == 'H':
                    hang = prop('Wall')
                if hang is not None:
                    add(hang)
            elif code in 'ABC':
                add(W(code))
            elif code == 'G':
                if prop('Big') is not None:
                    add(prop('Big'))
            elif code == 'f' and prop('Floor') is not None:
                add(prop('Floor'))
            elif code == 'e':
                if prop('Extra') is not None:
                    add(prop('Extra'))
                elif prop('Light2') is not None:
                    add(prop('Light2'))
            elif code == 'l':
                if light is not None and not light_on_wall:
                    add(light)
    return items


STRIP = [
    "WWWW...",
    ".......",
    "WAWBWCW",
    ".......",
]

NEUTRAL = dict(color=(1.0, 1.0, 1.0), intensity=1.0, euler=(50, -30), ambient=(0.45, 0.45, 0.45),
               filt=(1.0, 1.0, 1.0), tint=(1.0, 1.0, 1.0))


def render_scene(ch, assets, ppu, rows=None, shadows=True, layout=None, neutral=False):
    global LAYOUT
    keep = LAYOUT
    if layout is not None:
        LAYOUT = layout
    try:
        items = scene_items(ch, assets)
        nrow = len(LAYOUT)
        ncol = len(LAYOUT[0])
    finally:
        LAYOUT = keep
    r0, r1 = (0, nrow - 1) if rows is None else rows
    bounds = ((-CELL / 2, 2.0, -r1 * CELL - CELL / 2), ((ncol - 1) * CELL + CELL / 2, 5.4, -r0 * CELL + CELL / 2))
    if neutral:
        from vkcore import Light
        t = NEUTRAL
        L = Light(t['color'], t['intensity'], t['euler'], t['ambient'], t['filt'], shadows)
    else:
        t = THEMES[ch]
        L = light_for(ch, shadows)
    return render(items, light=L, pitch=GAME_PITCH, yaw=0, px_per_unit=ppu,
                  floor=floor_tex(), floor_y=2.0, floor_tint=t['tint'], bounds=bounds, margin=0.0)


def font(size):
    for f in ('C:/Windows/Fonts/malgun.ttf', 'C:/Windows/Fonts/arial.ttf'):
        if os.path.exists(f):
            from PIL import ImageFont
            return ImageFont.truetype(f, size)
    from PIL import ImageFont
    return ImageFont.load_default()


def titled(img, text, size=22, pad=6, bg=(24, 22, 28)):
    f = font(size)
    out = Image.new('RGB', (img.width, img.height + size + pad * 2), bg)
    ImageDraw.Draw(out).text((pad, pad), text, fill=(232, 230, 236), font=f)
    out.paste(img, (0, size + pad * 2))
    return out


def asset_previews(ch, assets):
    os.makedirs(PREV, exist_ok=True)
    mod = chapter(ch)
    tiles = []
    for name, g, kind, info in assets:
        fy = 2.0 if kind in ('wall',) else g.bounds()[0][1]
        ppu = 70 if kind in ('wall', 'big') else 140
        im = asset_views(g, mod.P, ch, ppu=ppu, floor_y=fy)
        im.save(os.path.join(PREV, name + '.png'))
        tiles.append((name, kind, im))
    return tiles


def contact_sheet(ch, assets, stats):
    os.makedirs(SHEETS, exist_ok=True)
    t0 = time.time()
    game = render_scene(ch, assets, ppu=30)              # 3px/복셀 = 1080p 기본 확대
    zoom = render_scene(ch, assets, ppu=64, rows=(0, 3))  # 위쪽 넉 줄을 크게
    small = render_scene(ch, assets, ppu=20)             # 2px/복셀 = 가장 멀리
    strip = hstack([render_scene(ch, assets, ppu=30, layout=STRIP),
                    render_scene(ch, assets, ppu=64, layout=STRIP)], gap=14)
    neutral = render_scene(ch, assets, ppu=30, neutral=True)
    neutral.save(os.path.join(SHEETS, 'scene_ch%d_neutral.png' % ch))
    mod = chapter(ch)
    # 하나씩: 같은 빛, 같은 크기(벽 50px/유닛, 소품 100px/유닛), 3/4 시점
    singles = []
    for name, g, kind, info in assets:
        fy = 2.0 if kind == 'wall' else g.bounds()[0][1]
        L = light_for(ch)
        ppu = 46 if kind in ('wall', 'big') else 92
        im = render([(g, (0, 0, 0), mod.P)], light=L, pitch=35, yaw=35, px_per_unit=ppu,
                    floor=floor_tex(), floor_y=fy, floor_tint=THEMES[ch]['tint'], margin=0.25)
        label = name.split('_', 2)[2] + '  %d tris' % info['tris']
        singles.append(titled(im, label, size=16))
    rows, cur, w = [], [], 0
    for im in singles:
        if cur and w + im.width > 1500:
            rows.append(hstack(cur, gap=10))
            cur, w = [], 0
        cur.append(im)
        w += im.width + 10
    if cur:
        rows.append(hstack(cur, gap=10))
    lineup = vstack(rows, gap=10)
    sheet = vstack([
        titled(game, '%s - game camera 60deg, 3 px/voxel (1080p default zoom), chapter light + shadows' % CHAPTER_NAMES[ch]),
        titled(strip, 'Base x4 in a row / Base-A-Base-B-Base-C-Base (3 px/voxel and 6.4 px/voxel)'),
        titled(zoom, 'closer: top rows (Base run with A/B/C mixed, hang props on south faces, Big in a wall niche)'),
        titled(small, 'farthest zoom: 2 px/voxel'),
        titled(lineup, 'every asset, same light, 3/4 view'),
    ], gap=14)
    path = os.path.join(SHEETS, 'contact_ch%d.png' % ch)
    sheet.save(path)
    game.save(os.path.join(SHEETS, 'scene_ch%d_game.png' % ch))
    return path, time.time() - t0


def all_chapters_sheet():
    paths = []
    for kind, title in (('game', 'chapter light (ChapterTheme + ChapterTint floor)'),
                        ('neutral', 'neutral white light, untinted floor - the materials alone')):
        ims = []
        for ch in range(5):
            p = os.path.join(SHEETS, 'scene_ch%d_%s.png' % (ch, kind))
            if os.path.exists(p):
                im = Image.open(p)
                im = im.resize((im.width * 2 // 3, im.height * 2 // 3), Image.LANCZOS)
                ims.append(titled(im, CHAPTER_NAMES[ch], size=18))
        if not ims:
            continue
        rows = [hstack(ims[i:i + 3], gap=12) for i in range(0, len(ims), 3)]
        out = titled(vstack(rows, gap=12), 'All five chapters - ' + title, size=22)
        path = os.path.join(SHEETS, 'all_chapters.png' if kind == 'game' else 'all_chapters_neutral.png')
        out.save(path)
        paths.append(path)
    return paths


# ---------------------------------------------------------------- 검사·읽기
def check_all():
    """짓기만 해서 잰다: 벽 경계·높이, 소품 자리 상자, 삼각형 예산, OBJ 왕복(다시 읽어 겉칸 색까지 같은가)."""
    tmp = tempfile.mkdtemp(prefix='voxel_kit_check_')
    bad = 0
    for ch in range(5):
        try:
            assets, stats, mod = build_assets(ch, write=False)
        except (AssertionError, ValueError) as e:
            print('C%d FAIL %s' % (ch, e))
            bad += 1
            continue
        for name, g, kind, info in assets:
            base = os.path.join(tmp, name)
            tris, _ = write_asset(base, g, mod.P, title=name)
            r, pal = read_obj(base + '.obj', fill=False)
            off = np.round((r.origin - g.origin) / VOX).astype(int)
            mism = 0
            for (i, j, k) in np.argwhere(r.v > 0):
                gi, gj, gk = i + off[0], j + off[1], k + off[2]
                inside = 0 <= gi < g.v.shape[0] and 0 <= gj < g.v.shape[1] and 0 <= gk < g.v.shape[2]
                if not inside or g.v[gi, gj, gk] != r.v[i, j, k]:
                    mism += 1
            ok = mism == 0 and np.array_equal(pal[1:255, :3], mod.P.rgba[1:255, :3])
            bad += 0 if ok else 1
            print('%-18s %-5s tris %4d  %s' % (name, kind, tris, 'OK' if ok else 'ROUNDTRIP MISMATCH %d' % mism))
    print('check:', 'all OK' if bad == 0 else '%d problem(s)' % bad)
    return bad


def inspect(path, out_dir):
    g, pal_rgba = read_obj(path)
    pal = Palette()
    if pal_rgba is not None:
        pal.rgba = pal_rgba
    os.makedirs(out_dir, exist_ok=True)
    im = hstack([render([(g, (0, 0, 0), pal)], light=light_for(0), pitch=GAME_PITCH, yaw=0, px_per_unit=80),
                 render([(g, (0, 0, 0), pal)], light=light_for(0), pitch=35, yaw=35, px_per_unit=80)])
    dst = os.path.join(out_dir, 'inspect_' + os.path.splitext(os.path.basename(path))[0] + '.png')
    im.save(dst)
    lo, hi = g.bounds()
    print('%s: %s voxels, bounds %s .. %s, %d colours, %d tris after culling -> %s' % (
        os.path.basename(path), g.v.shape, np.round(lo, 2), np.round(hi, 2),
        len(set(np.unique(g.v)) - {0}), 2 * len(quads(g)), dst))


def main(argv):
    global OUT, PREV, SHEETS, PREV_ROOT
    args = argv[1:]
    cmd = args[0] if args else 'build'

    def opt(name, default=None):
        return args[args.index(name) + 1] if name in args else default
    if opt('--out'):
        OUT = opt('--out')
    if opt('--preview'):
        PREV_ROOT = opt('--preview')
        PREV = os.path.join(PREV_ROOT, 'assets')
        SHEETS = os.path.join(PREV_ROOT, 'sheets')
    if cmd == 'check':
        return 1 if check_all() else 0
    if cmd == 'inspect':
        inspect(args[1], PREV_ROOT)
        return 0
    if cmd != 'build':
        print(__doc__)
        return 2
    if not OUT:
        print('Unity 프로젝트를 못 찾았다 — --out 으로 내보낼 폴더를 준다(또는 THESWORD_ROOT)')
        return 2
    if ROOT and not os.path.exists(FLOOR_PNG):
        print('바닥 타일을 못 찾아 미리보기 바닥이 빈다:', FLOOR_PNG)
    only = [int(x) for x in opt('--only', '0,1,2,3,4').split(',')]
    sheets = '--no-sheets' not in args
    report = {}
    for ch in only:
        t = time.time()
        assets, stats, mod = build_assets(ch, write=True)
        report[ch] = stats
        for name, kind, info in stats:
            print('%-18s %-5s tris %4d colours %2d%s' % (name, kind, info['tris'], info['colors'],
                  ' (glow -> _Emit.png)' if info['emits'] else ''))
        if sheets:
            asset_previews(ch, assets)
            path, dt = contact_sheet(ch, assets, stats)
            print('  sheet', path, '%.0fs' % dt)
        print('chapter %d done %.0fs' % (ch, time.time() - t))
    if sheets:
        print('overview', all_chapters_sheet())
    os.makedirs(PREV_ROOT, exist_ok=True)
    with open(os.path.join(PREV_ROOT, 'stats.json'), 'w', encoding='utf-8') as f:
        json.dump({str(k): v for k, v in report.items()}, f, ensure_ascii=False, indent=1, default=str)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
