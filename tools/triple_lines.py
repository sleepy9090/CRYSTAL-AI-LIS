#!/usr/bin/env python3
"""Per-scanline mismatch counts from a tools/web_compare.py triple image.

usage: python3 tools/triple_lines.py gfx/compare/web_2352.png [...]

The triple is emu | web | mask (256 px each, 4 px gaps); this re-diffs the emu and web panels
and prints each scanline with mismatching pixels plus the x range.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from render_compare import read_png  # noqa: E402

W, H, GAP = 256, 240, 4


def lines(path):
    tw, _, px = read_png(path)
    out = {}
    for y in range(H):
        row = y * tw
        for x in range(W):
            a, b = px[row + x], px[row + W + GAP + x]
            if a != b:
                lo, hi, n = out.get(y, (x, x, 0))
                out[y] = (min(lo, x), max(hi, x), n + 1)
    return out


def main(argv):
    for p in argv:
        res = lines(p)
        total = sum(v[2] for v in res.values())
        desc = ' '.join(f'y{y}:{n}px(x{lo}-{hi})' for y, (lo, hi, n) in sorted(res.items()))
        print(f'{os.path.basename(p)}: {total} px  {desc or "identical"}')


if __name__ == '__main__':
    main(sys.argv[1:])
