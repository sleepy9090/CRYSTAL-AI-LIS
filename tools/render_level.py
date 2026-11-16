#!/usr/bin/env python3
"""Render whole re-c areas from the k0 screen grid (AreaLoadMap 31:E628 -> $6300) to gfx/areas/.

usage: render_level.py [area_hex ...] [--all] [--visited] [--scale-down N]

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
from render_screen import area_bg, Renderer  # noqa


def render_area(area, visited=False, down=1, outdir=None):
    a = area_bg(area)
    if a is None:
        return None
    W, H = a['W'] * 256, a['H'] * 240
    px = [(0, 0, 0)] * (W * H)
    r = Renderer(a)
    for gy, row in enumerate(a['grid']):
        for gx, s in enumerate(row):
            r.screen(s, visited, px, W, gx * 256, gy * 240)
    if down > 1:
        w2, h2 = W // down, H // down
        px = [px[(y * down) * W + x * down] for y in range(h2) for x in range(w2)]
        W, H = w2, h2
    outdir = outdir or os.path.join(ROOT, 'gfx', 'areas')
    os.makedirs(outdir, exist_ok=True)
    out = os.path.join(outdir, f'area_{area:03d}{"_v" if visited else ""}.png')
    write_png(out, W, H, px)
    return a, out


def main(argv):
    visited = '--visited' in argv
    down = 1
    if '--scale-down' in argv:
        i = argv.index('--scale-down'); down = int(argv[i + 1]); del argv[i:i + 2]
    areas = [int(x, 16) for x in argv if not x.startswith('--')]
    if '--all' in argv:
        areas = range(area_dump.table_len())
    lines, n = [], 0
    for area in areas:
        res = render_area(area, visited, down)
        if res is None:
            lines.append(f'{area:3d} ${area:02X}  null/garbage header (not rendered)')
            continue
        a, out = res
        n += 1
        lines.append(f'{area:3d} ${area:02X}  {a["W"]}x{a["H"]} $67={a["v67"]:02X} R0={a["R0"]:02X} '
                     f'R1={a["R1"]:02X} pal={" ".join("%02X" % p for p in a["pal"])}'
                     f'{" bank10" if a["bank10"] else ""}  grid ' +
                     ' / '.join(' '.join('%02X' % s for s in row) for row in a['grid']))
    if '--all' in argv:
        with open(os.path.join(ROOT, 'gfx', 'areas', 'index.txt'), 'w') as f:
            f.write('\n'.join(lines) + '\n')
    print('\n'.join(lines[-5:]) if '--all' in argv else '\n'.join(lines))
    print(f'rendered {n} areas')


if __name__ == '__main__':
    main(sys.argv[1:])
