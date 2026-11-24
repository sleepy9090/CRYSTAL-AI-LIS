"""Starter graphics pack for the player character (T11): 16-bit-style 32x32 tiles for ReC.exe's Enhanced graphics.

usage: python3 tools/hero_pack.py [OUT_DIR] [--preview PNG]
  OUT_DIR   default packs/hero (copy its PNGs into %APPDATA%\\re-c\\pack, or run ReC.exe --pack OUT_DIR)
  --preview default OUT_DIR/../hero_preview.png: every painted frame, NES original (4x) next to the pack art
  --zoom N  scale the preview N times; --first N  only the first N frames

What it paints: the player's metasprite records (web/data/metasprites.json) -- walk 00-03, attack 04-07, the small
parts 09/0B/0D/0F and the poses BC/BD/CC/CD -- with the body bank in CHR R2 = $40 (no shield) and $41 (shield) and R3 =
$42 (sword), in the player's full-brightness sprite palette $36 $14 $0F (skin, magenta hair / clothes, black).
Each frame is composed whole (so light and outlines run across tile edges), drawn at 4x, then cut back into tiles named
TTTT_36140F.png (tile = CHR byte address / 16, in tile space: flipped placements are un-flipped). A tile used in
several frames keeps the first frame's painting (records in the order above). Fades / hit flashes use other colours, so
they keep the built-in Enhanced look.

Painting (per frame): the SNES has the same resolution as the NES, so "16-bit" here means more colours per pixel,
not thinner lines. Every NES pixel gets one new colour, then the frame is rounded with Scale2x twice to 4x:
  - colour: 5-tone ramps per material (skin, magenta hair / clothes, dark cloth); a pixel whose material ends above /
    left of it (toward the top-left light) gets a lighter tone, one whose material ends below / right a darker tone;
    the lightest / darkest tones only on shapes of >= 6 pixels, at the silhouette;
  - outline: black pixels on the silhouette stay near-black, tinted toward the material inside them (selective
    outline); small black spots inside a shape (eyes, seams) stay near-black; larger black areas are dark cloth;
  - hair shine: the NES art uses the skin colour for 1-4 pixel spots on the hair (3 colours per sprite); those become
    pink hair highlights;
  - the result never leaves the NES silhouette (the pack only repaints pixels the sprite drew; rounded-off corners are
    transparent = the built-in look shows there).
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import core_vs_web as cv  # noqa: E402
from pngio import encode  # noqa: E402

DATA = os.path.join(cv.ROOT, '..', 'web', 'data')
S = 4
RECORDS = ['00', '01', '02', '03', '04', '05', '06', '07', '09', '0B', '0D', '0F', 'BC', 'BD', 'CC', 'CD']
R2_BANKS = (0x40, 0x41)
R3_BANK = 0x42
PAL = (0x36, 0x14, 0x0F)

# 5-tone ramps, deep shadow .. highlight (tone 2 is close to the NES colour)
RAMPS = {
    1: [(0x8C, 0x5A, 0x48), (0xC9, 0x8E, 0x72), (0xF0, 0xC8, 0xA6), (0xFA, 0xDC, 0xBE), (0xFF, 0xEC, 0xD8)],   # skin
    2: [(0x52, 0x08, 0x5A), (0x8E, 0x12, 0x92), (0xC8, 0x2C, 0xC4), (0xE2, 0x5C, 0xDA), (0xF2, 0x8C, 0xEA)],   # magenta
    3: [(0x12, 0x0C, 0x1A), (0x1E, 0x16, 0x29), (0x2C, 0x22, 0x40), (0x40, 0x34, 0x58), (0x5A, 0x4C, 0x78)],   # dark cloth
}
OUTLINE_OUT, OUTLINE_IN = (0x0A, 0x06, 0x10), (0x2A, 0x16, 0x34)
DETAIL = (0x10, 0x08, 0x18)          # small black details inside a shape (eyes, mouth, seams): kept near-black


def load():
    chr_ = open(os.path.join(DATA, 'chr.bin'), 'rb').read()
    meta = json.load(open(os.path.join(DATA, 'metasprites.json')))
    return chr_, meta


def slots(chr_, t):
    return [[(chr_[t * 16 + y] >> (7 - x) & 1) | (chr_[t * 16 + y + 8] >> (7 - x) & 1) << 1 for x in range(8)]
            for y in range(8)]


def chr_tile(idx, r2):
    """metasprite tile index -> CHR tile number (sprites at $1000: idx 0-63 = R2, 64-127 = R3)"""
    return r2 * 64 + idx if idx < 64 else R3_BANK * 64 + (idx - 64) if idx < 128 else None


def compose(chr_, frame, r2):
    """-> (w, h, slot grid, owner grid of (tile, tx, ty, hflip, vflip)); OAM order: the first entry is on top"""
    xs = [e[0] for e in frame]
    ys = [e[1] for e in frame]
    x0, y0 = min(xs), min(ys)
    w, h = max(xs) - x0 + 8, max(ys) - y0 + 8
    grid = [[0] * w for _ in range(h)]
    own = [[None] * w for _ in range(h)]
    for dx, dy, attr, idx in reversed(frame):
        t = chr_tile(idx, r2)
        if t is None:
            continue
        s = slots(chr_, t)
        fh, fv = bool(attr & 0x40), bool(attr & 0x80)
        for ty in range(8):
            for tx in range(8):
                v = s[ty][tx]
                if not v:
                    continue
                x = dx - x0 + (7 - tx if fh else tx)
                y = dy - y0 + (7 - ty if fv else ty)
                grid[y][x] = v
                own[y][x] = (t, tx, ty, fh, fv)
    return w, h, grid, own


def scale2x(g):
    h, w = len(g), len(g[0])
    out = [[0] * (2 * w) for _ in range(2 * h)]
    for y in range(h):
        for x in range(w):
            e = g[y][x]
            b, hh = g[max(y - 1, 0)][x], g[min(y + 1, h - 1)][x]
            d, f = g[y][max(x - 1, 0)], g[y][min(x + 1, w - 1)]
            e0 = e1 = e2 = e3 = e
            if b != hh and d != f:
                e0 = d if d == b else e
                e1 = f if b == f else e
                e2 = d if d == hh else e
                e3 = f if hh == f else e
            out[2 * y][2 * x], out[2 * y][2 * x + 1], out[2 * y + 1][2 * x], out[2 * y + 1][2 * x + 1] = e0, e1, e2, e3
    return out


def regions(grid):
    """size of each pixel's 4-connected same-slot region"""
    h, w = len(grid), len(grid[0])
    size = [[0] * w for _ in range(h)]
    seen = [[False] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            if seen[y][x] or not grid[y][x]:
                continue
            v, stack, comp = grid[y][x], [(x, y)], []
            seen[y][x] = True
            while stack:
                cx, cy = stack.pop()
                comp.append((cx, cy))
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= nx < w and 0 <= ny < h and not seen[ny][nx] and grid[ny][nx] == v:
                        seen[ny][nx] = True
                        stack.append((nx, ny))
            for cx, cy in comp:
                size[cy][cx] = len(comp)
    return size


def mix(a, b, t):
    return tuple(round(a[k] + (b[k] - a[k]) * t) for k in range(3))


def repaint(grid):
    """slot grid -> grid of RGB (None = transparent) at the same resolution: what a 16-bit artist would do with the
    same pixels -- material ramps lit from the top left, selective outline, near-black details, hair shine"""
    h, w = len(grid), len(grid[0])
    size = regions(grid)

    def g(x, y):
        return grid[y][x] if 0 <= x < w and 0 <= y < h else 0

    out = [[None] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            v = grid[y][x]
            if not v:
                continue
            n8 = [g(x + dx, y + dy) for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dx or dy]
            if v == 3:
                if 0 in n8:
                    # selective outline: next to a lit material (above / left of it) the line takes a dark tint of it
                    inner = [g(x + dx, y + dy) for dx, dy in ((1, 0), (0, 1), (1, 1))]
                    mat = next((m for m in inner if m in (1, 2)), None)
                    out[y][x] = mix(OUTLINE_OUT, RAMPS[mat][0], 0.3) if mat else OUTLINE_OUT
                elif size[y][x] <= 3:
                    out[y][x] = DETAIL
                else:
                    v = 3                                # dark cloth: shaded below like the other materials
                if out[y][x] is not None:
                    continue
            # hair shine: 1-4 pixel skin spots touching hair and no larger skin area
            if v == 1 and size[y][x] <= 4 and 2 in [g(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))] \
                    and not any(g(x + dx, y + dy) == 1 and size[y + dy][x + dx] > 4
                                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)) if 0 <= x + dx < w and 0 <= y + dy < h):
                out[y][x] = RAMPS[2][4]
                continue
            lit = g(x, y - 1) != v or g(x - 1, y) != v
            dark = g(x, y + 1) != v or g(x + 1, y) != v
            big = size[y][x] >= 6
            tone = 2
            if lit and not dark:
                tone = 4 if big and g(x, y - 1) in (0, 3) and g(x - 1, y - 1) != v else 3
            elif dark and not lit:
                tone = 0 if big and g(x, y + 1) in (0, 3) and g(x + 1, y + 1) != v else 1
            out[y][x] = RAMPS[v][tone]
    return out


def paint(grid):
    """slot grid (1x) -> 4x rows of RGBA: the repainted pixels, corners rounded with Scale2x twice (same smoothing
    as the built-in Enhanced look), never outside the NES silhouette (alpha 0 there = keep the built-in look)"""
    h, w = len(grid), len(grid[0])
    col = repaint(grid)
    keyed = [[col[y][x] if col[y][x] is not None else (-1, -1, -1) for x in range(w)] for y in range(h)]
    big = scale2x(scale2x(keyed))
    rows = []
    for y in range(h * S):
        row = []
        for x in range(w * S):
            c = big[y][x]
            row.append((0, 0, 0, 0) if c[0] < 0 or not grid[y // S][x // S] else (*c, 255))
        rows.append(row)
    return rows


def main(argv):
    out_dir = os.path.join(cv.ROOT, 'packs', 'hero')
    preview = None
    zoom, first = 1, 0
    i = 0
    while i < len(argv):
        if argv[i] == '--preview':
            preview = argv[i + 1]; i += 1
        elif argv[i] == '--zoom':
            zoom = int(argv[i + 1]); i += 1
        elif argv[i] == '--first':
            first = int(argv[i + 1]); i += 1
        elif argv[i] == '--ascii':
            pass
        elif argv[i] in ('-h', '--help'):
            print(__doc__); return 0
        else:
            out_dir = argv[i]
        i += 1
    preview = preview or os.path.join(os.path.dirname(os.path.abspath(out_dir)), 'hero_preview.png')
    chr_, meta = load()
    if '--ascii' in argv:                       # debug: record 00 frame 0, slots and the repainted colours
        _, _, grid, _ = compose(chr_, meta['00']['frames'][0], R2_BANKS[0])
        col = repaint(grid)
        names = {OUTLINE_OUT: 'O', DETAIL: 'd'}
        for ramp, ch in ((RAMPS[1], 'skin'), (RAMPS[2], 'mag'), (RAMPS[3], 'clo')):
            for k, c in enumerate(ramp):
                names[c] = 'smc'[('skin', 'mag', 'clo').index(ch)] if k == 2 else str(k)
        for y, row in enumerate(grid):
            print(''.join('.123'[v] for v in row), ' ', ''.join(
                '.' if c is None else names.get(c, 'o') for c in col[y]))
        return 0
    os.makedirs(out_dir, exist_ok=True)
    tiles = {}                                  # tile -> 32x32 RGBA rows (first painting wins, per output pixel)
    shots = []                                  # preview: (original rows, painted rows)
    for r2 in R2_BANKS:
        for rec in RECORDS:
            for frame in meta[rec]['frames']:
                if not frame:
                    continue
                w, h, grid, own = compose(chr_, frame, r2)
                art = paint(grid)
                if r2 == R2_BANKS[0]:
                    orig = [[((0xF0, 0xD0, 0xB0), (0xD8, 0x00, 0xCC), (0, 0, 0))[grid[y // S][x // S] - 1] + (255,)
                             if grid[y // S][x // S] else (0, 0, 0, 0) for x in range(w * S)] for y in range(h * S)]
                    shots.append((orig, art))
                for y in range(h * S):
                    for x in range(w * S):
                        o = own[y // S][x // S]
                        if o is None or art[y][x][3] == 0:
                            continue
                        t, tx, ty, fh, fv = o
                        sx, sy = x % S, y % S
                        ux = tx * S + (S - 1 - sx if fh else sx)
                        uy = ty * S + (S - 1 - sy if fv else sy)
                        cell = tiles.setdefault(t, [[(0, 0, 0, 0)] * 32 for _ in range(32)])
                        if cell[uy][ux][3] == 0:
                            cell[uy][ux] = art[y][x]
    for f in os.listdir(out_dir):
        if f.endswith('.png'):
            os.remove(os.path.join(out_dir, f))
    tag = ''.join(f'{c:02X}' for c in PAL)
    for t, cell in sorted(tiles.items()):
        open(os.path.join(out_dir, f'{t:04X}_{tag}.png'), 'wb').write(encode(32, 32, cell, alpha=True))
    # preview: pairs on a grey background, 2 pairs per row band
    bg = (60, 64, 72)
    pad = 8
    def z(pic):
        return [[p for p in row for _ in range(zoom)] for row in pic for _ in range(zoom)]
    cells = [(z(o), z(a)) for o, a in (shots[:first] if first else shots)]
    cw = max(len(o[0]) for o, _ in cells) * 2 + 3 * pad
    chh = max(len(o) for o, _ in cells) + 2 * pad
    per = 6 if zoom == 1 else 4
    W, H = cw * per, chh * ((len(cells) + per - 1) // per)
    img = [[bg] * W for _ in range(H)]
    for n, (o, a) in enumerate(cells):
        ox, oy = (n % per) * cw + pad, (n // per) * chh + pad
        for k, pic in enumerate((o, a)):
            bx = ox + k * (len(o[0]) + pad)
            for y, row in enumerate(pic):
                for x, p in enumerate(row):
                    if p[3]:
                        img[oy + y][bx + x] = p[:3]
    open(preview, 'wb').write(encode(W, H, img))
    print(f'{out_dir}: {len(tiles)} tiles ({tag}); preview {preview} ({len(cells)} frames)')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
