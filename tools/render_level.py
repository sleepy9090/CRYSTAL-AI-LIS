#!/usr/bin/env python3
"""Render whole re-c areas from the k0 screen grid (AreaLoadMap 31:E628 -> $6300) to gfx/areas/.

usage: render_level.py [area_hex ...] [--all] [--visited] [--scale-down N] [--coll]

--coll (s77): tint every 16x16 cell by its BG collision class (ObjBgProbe 26:9A30 lookup, see
coll_attr) and also write area_NNN[_v]_coll.txt = one char per cell (legend COLL_LEGEND, the same
as tools/emu/world_probe.py --map, so the web port's collision map can be diffed per area);
prints per-area counts of p/c/D/s/o cells (--all also writes gfx/areas/coll_index.txt).

Each grid cell (W x H screens, k0) is drawn with render_screen.Renderer using the area's
k1 tileset/CHR/palettes. Cells are 256x240; the image is W*256 x H*240 (optionally reduced by
--scale-down N, nearest pixel). Writes gfx/areas/area_NNN.png (NNN = decimal area id) and
gfx/areas/index.txt (area, W, H, tileset, R0/R1, palettes, bank-10 flag, grid).
"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import ROOT  # noqa
from meta_dump import write_png  # noqa
import area_dump  # noqa
from render_screen import area_bg, Renderer, page_bytes, prg_at, subst  # noqa


COLL_LEGEND = ('. 00  D b7 damage floor  p b0 pit  c b5 conveyor  # b2 wall  o b1-only low obstacle  '
               's b6 sink  f b4-only (behind BG)  v b3-only (substitutable)')
COLL_TINT = {'D': (0, 255, 0), 'p': (255, 0, 255), 'c': (0, 255, 255), '#': (255, 0, 0),
             'o': (255, 160, 0), 's': (0, 0, 255), 'f': (160, 160, 160), 'v': (255, 255, 0)}


def coll_char(at):
    """Collision attr byte -> one char (same priority as tools/emu/world_probe.py --map)."""
    return ('D' if at & 0x80 else 'p' if at & 1 else 'c' if at & 0x20 else '#' if at & 4
            else 'o' if at & 2 else 's' if at & 0x40 else 'f' if at & 0x10 else 'v' if at & 8 else '.')


def coll_attr(a, s, r, c, visited=False):
    """ObjBgProbe 26:9A30 for screen s cell (r, c): the page always comes from bank G>>5 (never the
    bank-10 render page), attr = 9:(v69*256 + M); b3 + visited -> M = $BE00 substitute, re-read."""
    m = page_bytes(s, False)[r * 16 + c]
    at = prg_at(9, a['v69'] * 256 + m)
    if visited and at & 8:
        m = subst(a['v67'], m)
        at = prg_at(9, a['v69'] * 256 + m)
    return at


def coll_map(a, visited=False):
    """Area -> list of text rows (one char per 16x16 cell, '|' between screens)."""
    rows = []
    for gy, grow in enumerate(a['grid']):
        for r in range(15):
            rows.append(f'{gy:X}.{r * 16:02X} ' + '|'.join(
                ''.join(coll_char(coll_attr(a, s, r, c, visited)) for c in range(16)) for s in grow))
    return rows


def render_area(area, visited=False, down=1, outdir=None, coll=False):
    a = area_bg(area)
    if a is None:
        return None
    W, H = a['W'] * 256, a['H'] * 240
    px = [(0, 0, 0)] * (W * H)
    r = Renderer(a)
    for gy, row in enumerate(a['grid']):
        for gx, s in enumerate(row):
            r.screen(s, visited, px, W, gx * 256, gy * 240)
    if coll:
        for gy, row in enumerate(a['grid']):
            for gx, s in enumerate(row):
                for cr in range(15):
                    for cc in range(16):
                        t = COLL_TINT.get(coll_char(coll_attr(a, s, cr, cc, visited)))
                        if not t:
                            continue
                        for y in range(gy * 240 + cr * 16, gy * 240 + cr * 16 + 16):
                            for x in range(gx * 256 + cc * 16, gx * 256 + cc * 16 + 16):
                                p = px[y * W + x]
                                px[y * W + x] = tuple((p[i] + t[i]) >> 1 for i in range(3))
    if down > 1:
        w2, h2 = W // down, H // down
        px = [px[(y * down) * W + x * down] for y in range(h2) for x in range(w2)]
        W, H = w2, h2
    outdir = outdir or os.path.join(ROOT, 'gfx', 'areas')
    os.makedirs(outdir, exist_ok=True)
    out = os.path.join(outdir, f'area_{area:03d}{"_v" if visited else ""}{"_coll" if coll else ""}.png')
    write_png(out, W, H, px)
    if coll:
        with open(out[:-4] + '.txt', 'w') as f:
            f.write(f'# area ${area:02X} {a["W"]}x{a["H"]} screens, $69={a["v69"]:02X}; {COLL_LEGEND}\n')
            f.write('\n'.join(coll_map(a, visited)) + '\n')
    return a, out


def main(argv):
    visited = '--visited' in argv
    coll = '--coll' in argv
    down = 1
    if '--scale-down' in argv:
        i = argv.index('--scale-down'); down = int(argv[i + 1]); del argv[i:i + 2]
    areas = [int(x, 16) for x in argv if not x.startswith('--')]
    if '--all' in argv:
        areas = range(area_dump.table_len())
    lines, n = [], 0
    for area in areas:
        res = render_area(area, visited, down, coll=coll)
        if res is None:
            lines.append(f'{area:3d} ${area:02X}  null/garbage header (not rendered)')
            continue
        a, out = res
        n += 1
        if coll:
            cm = ''.join(coll_map(a, visited))
            counts = ' '.join(f'{ch}={cm.count(ch)}' for ch in 'pcDso' if cm.count(ch))
            lines.append(f'{area:3d} ${area:02X}  coll {counts or "-"}')
            continue
        lines.append(f'{area:3d} ${area:02X}  {a["W"]}x{a["H"]} $67={a["v67"]:02X} R0={a["R0"]:02X} '
                     f'R1={a["R1"]:02X} pal={" ".join("%02X" % p for p in a["pal"])}'
                     f'{" bank10" if a["bank10"] else ""}  grid ' +
                     ' / '.join(' '.join('%02X' % s for s in row) for row in a['grid']))
    if '--all' in argv:
        name = 'coll_index.txt' if coll else 'index.txt'
        with open(os.path.join(ROOT, 'gfx', 'areas', name), 'w') as f:
            f.write('\n'.join(lines) + '\n')
    print('\n'.join(lines[-5:]) if '--all' in argv and not coll else '\n'.join(lines))
    print(f'rendered {n} areas')


if __name__ == '__main__':
    main(sys.argv[1:])
