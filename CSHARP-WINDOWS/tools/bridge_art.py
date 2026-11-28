"""Painted HD art for the wooden bridges (playtest 13: "modernize the bridges"): tiles $000F (the planks) and $0051 (the
rope rail beside them), key 27070F (area $03's chasm bridge and the other wooden bridges), as 32x32 pack tiles.

usage: python3 tools/bridge_art.py [OUT_DIR=packs/backgrounds] [--preview PNG]

Planks: one board per 8 NES lines (the NES tile: a dark gap at rows 0 and 6, the board between), warm weathered wood
with fine grain, knots, darker worn ends, a lit top edge and a shadowed bottom, two iron nails each side, dark gaps
showing the chasm. Rail: a twisted rope (two strands, highlights on each twist) beside a dark wooden post edge.
Seamless vertically (8-line period) and horizontally (the board spans the tile). 4 x 4 supersampling per pixel.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from spike_art import png, h2, SIZE, SUB  # noqa: E402

KEY = '27070F'


def plank(x, y):
    """colour at NES coordinates (0..8) of the plank tile"""
    if y < 0.9 or y > 6.1:
        return (14, 9, 8)                                       # the gap: the dark chasm below
    t = (y - 0.9) / 5.2                                         # 0 top .. 1 bottom of the board
    grain = math.sin(y * 9.0 + math.sin(x * 1.3) * 1.8 + h2(int(x * 3), int(y * 4)) * 0.6) * 0.5 + 0.5
    base = (176, 112, 58)
    v = 0.78 + 0.16 * grain + 0.08 * h2(int(x * 4), int(y * 4))
    v *= 0.94 + 0.06 * math.sin(x * 0.785 + 1.0)                   # gentle tone change along the board (8 px period: seamless)
    kx, ky = 5.3, 3.4                                           # a knot
    kd = math.hypot((x - kx) * 1.6, y - ky)
    if kd < 0.55:
        v *= 0.55 + 0.6 * kd
    if t < 0.12:
        v *= 1.25                                               # lit top edge
    elif t > 0.85:
        v *= 0.62                                               # shadowed bottom edge
    c = [b * v for b in base]
    for nx in (0.9,):                                           # iron nails at the board joint
        for ny in (2.4, 4.6):
            d = math.hypot(x - nx, y - ny)
            if d < 0.32:
                c = [70 + 90 * max(0.0, 0.3 - d), 72 + 90 * max(0.0, 0.3 - d), 78 + 95 * max(0.0, 0.3 - d)]
    return tuple(min(255.0, max(0.0, k)) for k in c)


def rail(x, y):
    """colour of the rope rail tile: rope over x 0.6..4.2, a post edge 5..8"""
    if x >= 5.0:
        g = 0.7 + 0.2 * math.sin(y * 7 + x) + 0.1 * h2(int(x * 4), int(y * 4))
        lit = 1.15 if x < 5.6 else 0.85
        return tuple(min(255.0, b * g * lit) for b in (96, 58, 30))
    if 0.6 <= x <= 4.2:
        u = (x - 0.6) / 3.6
        ph = (y * 0.95 + u * 1.6) % 1.0                         # two twisted strands
        s = 0.5 - 0.5 * math.cos(ph * 2 * math.pi)
        shade = 0.55 + 0.55 * s * (1.0 - abs(u - 0.4))
        return tuple(min(255.0, b * shade) for b in (205, 168, 110))
    return (14, 9, 8)


def tile(fn):
    out = []
    for oy in range(SIZE):
        row = []
        for ox in range(SIZE):
            acc = [0.0, 0.0, 0.0]
            for sy in range(SUB):
                for sx in range(SUB):
                    c = fn((ox + (sx + 0.5) / SUB) * 8 / SIZE, (oy + (sy + 0.5) / SUB) * 8 / SIZE)
                    for k in range(3):
                        acc[k] += c[k]
            row.append(tuple(int(round(a / (SUB * SUB))) for a in acc))
        out.append(row)
    return out


def main(argv):
    preview = None
    if '--preview' in argv:
        i = argv.index('--preview'); preview = argv[i + 1]; argv = argv[:i] + argv[i + 2:]
    out = argv[0] if argv else os.path.join(os.path.dirname(HERE), 'packs', 'backgrounds')
    os.makedirs(out, exist_ok=True)
    tp, tr = tile(plank), tile(rail)
    png(tp, os.path.join(out, f'000F_{KEY}.png'))
    png(tr, os.path.join(out, f'0051_{KEY}.png'))
    print(f'2 bridge tiles -> {out}')
    if preview:                                                 # rail, 4 planks, mirrored rail; 6 rows
        rows = []
        for _ in range(6):
            for y in range(SIZE):
                rows.append(tr[y] + tp[y] * 4 + tr[y][::-1])
        png(rows, preview)
        print('preview ->', preview)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
