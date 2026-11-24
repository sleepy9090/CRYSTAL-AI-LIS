"""Bounding box of the pixels of one colour in a PNG (to locate a sprite for crops).

usage: python3 tools/png_find.py IN.png RRGGBB   -> prints "x0 y0 x1 y1 count" (inclusive), or "none"
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pngio import png_decode  # noqa: E402


def main(argv):
    if len(argv) != 2:
        print(__doc__); return 2
    _, _, rows = png_decode(open(argv[0], 'rb').read())
    c = tuple(int(argv[1][k:k + 2], 16) for k in (0, 2, 4))
    pts = [(x, y) for y, row in enumerate(rows) for x, p in enumerate(row) if p == c]
    if not pts:
        print('none'); return 1
    xs, ys = [p[0] for p in pts], [p[1] for p in pts]
    print(min(xs), min(ys), max(xs), max(ys), len(pts))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
