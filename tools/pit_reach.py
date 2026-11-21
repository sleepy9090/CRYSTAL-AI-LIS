#!/usr/bin/env python3
"""Can the player walk from an area's entry points to its pit (b0) cells? (session 129)

Cell-level flood fill over the ObjBgProbe 26:9A30 collision attrs (render_level.coll_attr):
a 16x16 cell is walkable when attr & $06 == 0 (no b1 low obstacle, no b2 wall; both block the
player on the ground and in a normal jump, see bg_coll_dump.BITS). b0 cells are pits: reaching
one starts the fall, so the fill stops there. 4-connected, across screen borders, inside the
W x H screen grid. This is an approximation of the real body probes (no sub-cell offsets, no
objects/doors, no flight), good enough to tell "walled off" from "on the walking path".

Start points: the k2 entry records (area_dump sub) given by index, or explicit world x,y.
  usage: pit_reach.py AREA [--entries 0,1,..] [--at X,Y ...] [--visited] [--fly] [--path]
  --fly: only b2 blocks (flight / warp-exit mask $34 crosses b1); --path: BFS path to the first pit
         (x,y hex world coords; y = y_hi*256 + y_lo, y_lo 0..$EF)
"""
import os
import sys
sys.path.insert(0, os.path.dirname(__file__))
import area_dump  # noqa
from render_screen import area_bg  # noqa
from render_level import coll_attr  # noqa


def k2_entry(area, e):
    _, ps = area_dump.sub_header(area)
    rd, p = area_dump.rd10, ps[2] + 4 * e
    return rd(p) | rd(p + 1) << 8, rd(p + 2) | rd(p + 3) << 8


def cell_of(x, y):
    return (y >> 8, (y & 0xFF) >> 4, x >> 8, (x & 0xFF) >> 4)  # gy, row, gx, col


def build(a, visited):
    att = {}
    for gy, row in enumerate(a['grid']):
        for gx, s in enumerate(row):
            for r in range(15):
                for c in range(16):
                    att[(gy, r, gx, c)] = coll_attr(a, s, r, c, visited)
    return att


def neighbours(k, a):
    gy, r, gx, c = k
    for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        nr, nc, ny, nx = r + dr, c + dc, gy, gx
        if nr < 0:
            nr, ny = 14, gy - 1
        elif nr > 14:
            nr, ny = 0, gy + 1
        if nc < 0:
            nc, nx = 15, gx - 1
        elif nc > 15:
            nc, nx = 0, gx + 1
        if 0 <= ny < a['H'] and 0 <= nx < a['W']:
            yield (ny, nr, nx, nc)


BLOCK = [6]  # --fly: 4
EXITS = set()  # k3 exit cells (gy, row, gx, col): the fill does not walk past them


def k3_cells(area):
    """k3 records (x cell = x>>4, y cell = y_hi*16 + row) -> cell keys."""
    _, ps = area_dump.sub_header(area)
    rd, p, out = area_dump.rd10, ps[3], set()
    while rd(p) != 0xFF and len(out) < 64:
        xc, yc = rd(p), rd(p + 1)
        out.add((yc >> 4, yc & 15, xc >> 4, xc & 15))
        p += 4
    return out


def fill(a, att, start):
    """BFS; returns (cells, pit cells, start blocked); fill.prev = parent links for --path."""
    seen, todo, pits, prev = {start}, [start], set(), {start: None}
    fill.prev = prev
    if att.get(start, 4) & BLOCK[0]:
        return seen, pits, True
    while todo:
        k = todo.pop(0)
        if att[k] & 1:
            pits.add(k)
            continue
        if k in EXITS and k != start:  # ExitLookup 31:EB09 would leave the area here
            continue
        for n in neighbours(k, a):
            if n not in seen and not att[n] & BLOCK[0]:
                seen.add(n)
                prev[n] = k
                todo.append(n)
    return seen, pits, False


def fmt(k):
    gy, r, gx, c = k
    return f'(${gx * 256 + c * 16:03X},${gy * 256 + r * 16:03X})'


def main(argv):
    area = int(argv[0], 16)
    visited = '--visited' in argv
    if '--fly' in argv:
        BLOCK[0] = 4
    a = area_bg(area)
    att = build(a, visited)
    if '--no-exits' not in argv:
        EXITS.update(k3_cells(area))
    allpits = sorted(k for k, v in att.items() if v & 1)
    starts = []
    if '--entries' in argv:
        for e in argv[argv.index('--entries') + 1].split(','):
            x, y = k2_entry(area, int(e, 16))
            starts.append((f'k2 entry {e}', x, y))
    i = 0
    while '--at' in argv[i:]:
        i = argv.index('--at', i) + 1
        x, y = (int(v, 16) for v in argv[i].split(','))
        starts.append(('point', x, y))
    print(f'area ${area:02X} W={a["W"]} H={a["H"]} visited={visited}: {len(allpits)} pit cells')
    for name, x, y in starts:
        seen, pits, blocked = fill(a, att, cell_of(x, y))
        state = 'START CELL BLOCKED (attr ${:02X})'.format(att.get(cell_of(x, y), 0)) if blocked else ''
        print(f'  {name} x=${x:04X} y=${y:04X} {fmt(cell_of(x, y))}: {len(seen)} cells reached, '
              f'{len(pits)} pit cells reached {state}')
        for k in sorted(pits)[:8]:
            print(f'    pit {fmt(k)} attr ${att[k]:02X}')
        if pits and '--path' in argv:
            k, path = sorted(pits)[0], []
            while k is not None:
                path.append(fmt(k))
                k = fill.prev[k]
            print('    path: ' + ' '.join(reversed(path)))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
