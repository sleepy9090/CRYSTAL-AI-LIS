"""Starter graphics pack for the enemies (T12): 16-bit-style 32x32 tiles for ReC.exe's Enhanced graphics, painted from
the sprites as they appear on screen (tools/sprite_capture.py dumps).

usage: python3 tools/sprite_pack.py [--in DIR] [--out DIR] [--preview PNG] [--skip STATE,..] [--max-preview N]
  --in       capture folder (default out/sprites, one sub-folder per state)
  --out      pack folder (default packs/enemies)
  --preview  default <out>/../enemies_preview.png: each newly painted object, NES (4x) next to the pack
  --skip     states to leave out (default group18,death: a town's NPCs and the player alone)

Per dump (Ppu.Tags + Fb + palette of one frame): sprite pixels in sprite palettes 1-3 (palette 0 is the player's,
packs/hero) and not from the player's banks ($40/$41/$42), whose tile + colours show in >= 3 dumps (so fade and hit
flash steps are left to the built-in look), are grouped into objects (8-connected), each object is repainted whole at the NES pixel size and rounded to 4x, and
its pixels are cut back into tiles (tile space, un-flipped) keyed like the exe does: TTTT_AABBCC with the slot 1-3
colours of the sprite palette as seen on that line (same rule as Enhance.cs Render). The first painting of an output
pixel wins; objects whose tiles are all painted already are skipped.

Painting (the hero's rules, made generic -- any NES colour, see tools/hero_pack.py):
  - colours: a 5-tone ramp per NES colour with hue shifting (shadows toward blue-violet, highlights toward warm
    yellow), lighter where the colour ends above / left (light from the top left), darker below / right, the extremes
    only on shapes of >= 6 pixels at the silhouette;
  - dark colours (luma < 0.14) on the silhouette are the outline: near-black tinted toward the colour inside; small
    dark spots inside (eyes, seams) stay near-black; larger dark areas get a cool dark-grey ramp;
  - never outside the NES silhouette (alpha 0 = the built-in Enhanced look).
Exit 1 if nothing was captured.
"""
import colorsys
import json
import os
import struct
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import core_vs_web as cv  # noqa: E402
from pngio import encode  # noqa: E402

DATA = os.path.join(cv.ROOT, '..', 'web', 'data')
S = 4
HERO_BANKS = {0x40, 0x41, 0x42}
VALID = 1 << 24
OUTLINE_OUT = (0x0A, 0x08, 0x10)
DETAIL = (0x10, 0x0C, 0x16)
DARK_RAMP = [(0x10, 0x10, 0x18), (0x1C, 0x1C, 0x28), (0x2A, 0x2A, 0x3A), (0x3C, 0x3C, 0x50), (0x52, 0x52, 0x6A)]


def luma(c):
    return (0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]) / 255


def toward(h, target, amount):
    d = (target - h + 0.5) % 1.0 - 0.5
    return (h + max(-amount, min(amount, d))) % 1.0


def ramp(rgb):
    """5 tones deep .. highlight around one colour (tone 2 = the colour itself)"""
    h, l, s = colorsys.rgb_to_hls(*(v / 255 for v in rgb))
    out = []
    for k, (lf, hue_t, hue_a, sf) in enumerate(((0.50, 0.72, 0.07, 1.05), (0.74, 0.72, 0.035, 1.03), (1, 0, 0, 1),
                                                 (0.30, 0.15, 0.03, 0.95), (0.55, 0.15, 0.05, 0.85))):
        if k == 2:
            out.append(tuple(rgb)); continue
        nl = l * lf if k < 2 else l + (1 - l) * lf
        nh = toward(h, hue_t, hue_a) if s > 0.08 else h
        r, g, b = colorsys.hls_to_rgb(nh, max(0, min(1, nl)), max(0, min(1, s * sf)))
        if s <= 0.08 and k < 2:                     # greys: a touch of blue in the shadows
            b = min(1, b + 0.03)
        out.append((round(r * 255), round(g * 255), round(b * 255)))
    return out


def mix(a, b, t):
    return tuple(round(a[k] + (b[k] - a[k]) * t) for k in range(3))


def scale2x(g):
    h, w = len(g), len(g[0])
    out = [[None] * (2 * w) for _ in range(2 * h)]
    for y in range(h):
        up, row, dn = g[max(y - 1, 0)], g[y], g[min(y + 1, h - 1)]
        for x in range(w):
            e, b, hh = row[x], up[x], dn[x]
            d, f = row[max(x - 1, 0)], row[min(x + 1, w - 1)]
            e0 = e1 = e2 = e3 = e
            if b != hh and d != f:
                e0 = d if d == b else e
                e1 = f if b == f else e
                e2 = d if d == hh else e
                e3 = f if hh == f else e
            out[2 * y][2 * x], out[2 * y][2 * x + 1], out[2 * y + 1][2 * x], out[2 * y + 1][2 * x + 1] = e0, e1, e2, e3
    return out


def regions(grid):
    h, w = len(grid), len(grid[0])
    size = [[0] * w for _ in range(h)]
    seen = [[False] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            if seen[y][x] or grid[y][x] is None:
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


class Painter:
    def __init__(self, nes_rgb):
        self.rgb = [((c >> 16) & 255, (c >> 8) & 255, c & 255) for c in nes_rgb]
        self.ramps = {}

    def ramp_of(self, c):
        if c not in self.ramps:
            self.ramps[c] = ramp(self.rgb[c])
        return self.ramps[c]

    def dark(self, c):
        return luma(self.rgb[c]) < 0.14

    def repaint(self, grid):
        """grid of NES colours (None = not this object) -> grid of RGB at the same size"""
        h, w = len(grid), len(grid[0])
        size = regions(grid)

        def g(x, y):
            return grid[y][x] if 0 <= x < w and 0 <= y < h else None

        out = [[None] * w for _ in range(h)]
        for y in range(h):
            for x in range(w):
                v = grid[y][x]
                if v is None:
                    continue
                if self.dark(v):
                    n8 = [g(x + dx, y + dy) for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dx or dy]
                    if None in n8:
                        inner = [g(x + dx, y + dy) for dx, dy in ((1, 0), (0, 1), (1, 1), (-1, 0), (0, -1))]
                        mat = next((m for m in inner if m is not None and not self.dark(m)), None)
                        out[y][x] = mix(OUTLINE_OUT, self.ramp_of(mat)[0], 0.3) if mat is not None else OUTLINE_OUT
                        continue
                    if size[y][x] <= 3:
                        out[y][x] = DETAIL
                        continue
                    tones = DARK_RAMP
                else:
                    tones = self.ramp_of(v)
                lit = g(x, y - 1) != v or g(x - 1, y) != v
                dk = g(x, y + 1) != v or g(x + 1, y) != v
                big = size[y][x] >= 6
                tone = 2
                if lit and not dk:
                    tone = 4 if big and (g(x, y - 1) is None or self.dark(g(x, y - 1))) and g(x - 1, y - 1) != v else 3
                elif dk and not lit:
                    tone = 0 if big and (g(x, y + 1) is None or self.dark(g(x, y + 1))) and g(x + 1, y + 1) != v else 1
                out[y][x] = tones[tone]
        return out


def load_dump(path):
    b = open(path, 'rb').read()
    n = 256 * 240
    tags = struct.unpack(f'<{n}i', b[:4 * n])
    fb = b[4 * n:5 * n]
    pal = b[5 * n:5 * n + 32]
    return tags, fb, pal


def slot_of(chr_, tag):
    t, col, row = tag & 0x1FFF, tag >> 13 & 7, tag >> 16 & 7
    a = t * 16 + row
    return (chr_[a] >> (7 - col) & 1) | (chr_[a + 8] >> (7 - col) & 1) << 1


MIN_DUMPS = 3


def frame_keys(chr_, tags, fb, pal):
    """per pixel: the pack key (tile, c1, c2, c3) of enemy sprite pixels (else None), and the enemy-pixel mask.
    Enemy = sprite palette 1-3 (palette 0 is the player's, packs/hero) and not a player bank. Colours = the
    renderer's per-line palette (Enhance.cs Render): end-of-frame palette, updated by each line's own pixels."""
    lp = [pal[0x10 + (g - 4) * 4 + s] & 0x3F if s else 0 for g in range(4, 8) for s in range(4)]
    keys = [None] * (256 * 240)
    sprite = bytearray(256 * 240)
    for y in range(240):
        base = y * 256
        for x in range(256):
            t = tags[base + x]
            if not t & VALID or (t >> 19 & 7) < 4:
                continue
            s = slot_of(chr_, t)
            if s:
                lp[((t >> 19 & 7) - 4) * 4 + s] = fb[base + x] & 0x3F
        for x in range(256):
            t = tags[base + x]
            if not t & VALID:
                continue
            g = (t >> 19 & 7) - 4
            if g < 1 or ((t & 0x1FFF) >> 6) in HERO_BANKS:
                continue
            c1, c2, c3 = lp[g * 4 + 1], lp[g * 4 + 2], lp[g * 4 + 3]
            if c1 == c2 == c3:
                continue
            sprite[base + x] = 1
            keys[base + x] = (t & 0x1FFF, c1, c2, c3)
    return keys, sprite


def main(argv):
    src, out_dir, preview, skip, maxprev = os.path.join(cv.ROOT, 'out', 'sprites'), os.path.join(cv.ROOT, 'packs', 'enemies'), None, {'group18', 'death'}, 48
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--in':
            src = argv[i + 1]; i += 1
        elif a == '--out':
            out_dir = argv[i + 1]; i += 1
        elif a == '--preview':
            preview = argv[i + 1]; i += 1
        elif a == '--skip':
            skip = set(argv[i + 1].split(',')); i += 1
        elif a == '--max-preview':
            maxprev = int(argv[i + 1]); i += 1
        elif a in ('-h', '--help'):
            print(__doc__); return 0
        i += 1
    preview = preview or os.path.join(os.path.dirname(os.path.abspath(out_dir)), 'enemies_preview.png')
    chr_ = open(os.path.join(DATA, 'chr.bin'), 'rb').read()
    painter = Painter(json.load(open(os.path.join(DATA, 'palettes.json')))['nesRgb'])
    art = {}                     # key (tile, c1, c2, c3) -> 32x32 RGBA rows
    shots = []
    states = sorted(d for d in os.listdir(src) if os.path.isdir(os.path.join(src, d)) and d not in skip) \
        if os.path.isdir(src) else []
    # pass 1: how many dumps show each tile + colours (fades / hit flashes last a few frames: seen in 1-2 dumps)
    seen_n = {}
    for st in states:
        for fn in sorted(os.listdir(os.path.join(src, st))):
            for k in set(frame_keys(chr_, *load_dump(os.path.join(src, st, fn)))[0]) - {None}:
                seen_n[k] = seen_n.get(k, 0) + 1
    stable = {k for k, n in seen_n.items() if n >= MIN_DUMPS}
    dumps = 0
    per_state = {}
    for st in states:
        for fn in sorted(os.listdir(os.path.join(src, st))):
            tags, fb, pal = load_dump(os.path.join(src, st, fn))
            dumps += 1
            keys, sprite = frame_keys(chr_, tags, fb, pal)
            for p in range(256 * 240):
                if sprite[p] and keys[p] not in stable:
                    sprite[p] = 0
            # objects: 8-connected sprite pixels
            seen = bytearray(256 * 240)
            for start in range(256 * 240):
                if not sprite[start] or seen[start]:
                    continue
                comp, stack = [], [start]
                seen[start] = 1
                while stack:
                    p = stack.pop()
                    comp.append(p)
                    px, py = p % 256, p // 256
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            nx, ny = px + dx, py + dy
                            if 0 <= nx < 256 and 0 <= ny < 240:
                                q = ny * 256 + nx
                                if sprite[q] and not seen[q]:
                                    seen[q] = 1
                                    stack.append(q)
                if len(comp) < 6 or all(keys[p] in art for p in comp):
                    continue
                xs, ys = [p % 256 for p in comp], [p // 256 for p in comp]
                x0, y0, w, h = min(xs), min(ys), max(xs) - min(xs) + 1, max(ys) - min(ys) + 1
                grid = [[None] * w for _ in range(h)]
                for p in comp:
                    grid[p // 256 - y0][p % 256 - x0] = fb[p] & 0x3F
                col = painter.repaint(grid)
                big = scale2x(scale2x([[c if c is not None else (-1, -1, -1) for c in row] for row in col]))
                new = 0
                for p in comp:
                    t, k = tags[p], keys[p]
                    tx, ty = t >> 13 & 7, t >> 16 & 7
                    fh, fv = bool(t & 1 << 22), bool(t & 1 << 23)
                    cell = art.get(k)
                    if cell is None:
                        cell = art[k] = [[(0, 0, 0, 0)] * 32 for _ in range(32)]
                    gx, gy = p % 256 - x0, p // 256 - y0
                    for sy in range(S):
                        for sx in range(S):
                            c = big[gy * S + sy][gx * S + sx]
                            if c[0] < 0:
                                continue
                            ux = tx * S + (S - 1 - sx if fh else sx)
                            uy = ty * S + (S - 1 - sy if fv else sy)
                            if cell[uy][ux][3] == 0:
                                cell[uy][ux] = (*c, 255)
                                new += 1
                if new >= 16 * 20 and per_state.get(st, 0) < 2 and len(shots) < maxprev:
                    per_state[st] = per_state.get(st, 0) + 1
                    orig = [[(*painter.rgb[grid[yy // S][xx // S]], 255) if grid[yy // S][xx // S] is not None
                             else (0, 0, 0, 0) for xx in range(w * S)] for yy in range(h * S)]
                    pnt = [[(*big[yy][xx], 255) if big[yy][xx][0] >= 0 and grid[yy // S][xx // S] is not None
                            else (0, 0, 0, 0) for xx in range(w * S)] for yy in range(h * S)]
                    shots.append((orig, pnt))
    if not dumps:
        print(f'no dumps in {src} (run tools/sprite_capture.py)'); return 1
    os.makedirs(out_dir, exist_ok=True)
    for f in os.listdir(out_dir):
        if f.endswith('.png'):
            os.remove(os.path.join(out_dir, f))
    written = 0
    for (t, c1, c2, c3), cell in sorted(art.items()):
        if not any(p[3] for row in cell for p in row):
            continue
        open(os.path.join(out_dir, f'{t:04X}_{c1:02X}{c2:02X}{c3:02X}.png'), 'wb').write(encode(32, 32, cell, alpha=True))
        written += 1
    # preview
    bg, pad, per = (60, 64, 72), 8, 6
    cw = max(len(o[0]) for o, _ in shots) * 2 + 3 * pad
    chh = max(len(o) for o, _ in shots) + 2 * pad
    W, H = cw * per, chh * ((len(shots) + per - 1) // per)
    img = [[bg] * W for _ in range(H)]
    for n, (o, a) in enumerate(shots):
        ox, oy = (n % per) * cw + pad, (n // per) * chh + pad
        for k, pic in enumerate((o, a)):
            bx = ox + k * (len(o[0]) + pad)
            for y, row in enumerate(pic):
                for x, p in enumerate(row):
                    if p[3]:
                        img[oy + y][bx + x] = p[:3]
    open(preview, 'wb').write(encode(W, H, img))
    tiles = len({k[0] for k in art})
    print(f'{out_dir}: {written} pack tiles ({tiles} distinct tiles) from {dumps} frames of {len(states)} states; '
          f'preview {preview} ({len(shots)} objects)')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
