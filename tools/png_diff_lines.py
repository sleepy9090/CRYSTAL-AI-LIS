#!/usr/bin/env python3
"""Per scanline: which x ranges differ between two same-size PNGs, with both colours (session 209).

usage: python3 tools/png_diff_lines.py A.png B.png [--max N]
Prints `y: x0-x1 A=#rrggbb B=#rrggbb ...` (runs of equal colour pairs) for each differing line.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from render_compare import read_png  # noqa: E402


def main(argv):
    mx = 40
    if '--max' in argv:
        i = argv.index('--max')
        mx = int(argv[i + 1])
        del argv[i:i + 2]
    wa, ha, a = read_png(argv[0])
    wb, hb, b = read_png(argv[1])
    if (wa, ha) != (wb, hb):
        sys.exit(f'size differs: {wa}x{ha} vs {wb}x{hb}')
    hex6 = lambda c: '#%02x%02x%02x' % tuple(c[:3])
    total = 0
    for y in range(ha):
        runs, x = [], 0
        while x < wa:
            ca, cb = tuple(a[y * wa + x][:3]), tuple(b[y * wa + x][:3])
            if ca == cb:
                x += 1
                continue
            x0 = x
            while x + 1 < wa and tuple(a[y * wa + x + 1][:3]) == ca and tuple(b[y * wa + x + 1][:3]) == cb:
                x += 1
            runs.append(f'{x0}-{x} A={hex6(ca)} B={hex6(cb)}')
            total += x - x0 + 1
            x += 1
        if runs:
            print(f'{y}: ' + '  '.join(runs[:mx]) + (f' (+{len(runs) - mx})' if len(runs) > mx else ''))
    print(f'total {total} px')


if __name__ == '__main__':
    main(sys.argv[1:])
