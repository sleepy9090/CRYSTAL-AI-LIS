#!/usr/bin/env python3
"""List mismatch pixels (pure red #ff2020) of a gfx/compare pair image's mask panel.

usage: python3 tools/mask_peek.py PAIR.png [--panel N] [--top T]
  panel N (default 2 = third panel), panels are W wide with a 4px gap (W = 256);
  --top T adds T to y (8 for the 224-line reference pairs -> 240-line screen coordinates).
Prints the bounding box and the pixels grouped by row.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from render_compare import read_png  # noqa: E402


def main(argv):
    path, panel, top = argv[0], 2, 0
    if '--panel' in argv:
        panel = int(argv[argv.index('--panel') + 1])
    if '--top' in argv:
        top = int(argv[argv.index('--top') + 1])
    w, h, px = read_png(path)
    x0 = panel * 260
    rows = {}
    for y in range(h):
        for x in range(256):
            if tuple(px[y * w + x0 + x]) == (0xFF, 0x20, 0x20):
                rows.setdefault(y + top, []).append(x)
    if not rows:
        print('no mismatch pixels')
        return
    xs = [x for v in rows.values() for x in v]
    print(f'{sum(map(len, rows.values()))} px, x {min(xs)}-{max(xs)}, y {min(rows)}-{max(rows)}')
    for y, v in sorted(rows.items()):
        print(f'  y {y}: x ' + ' '.join(map(str, v)))


if __name__ == '__main__':
    main(sys.argv[1:])
