#!/usr/bin/env python3
"""For each scanline of B, find the scanline of A it equals (session 115: split-timing checks).

usage: line_match.py PAIR.png            (tools/web_compare.py pair: A = x 0-255, B = x 260-515)
       line_match.py A.png B.png
Prints runs `B lines lo-hi = A lines lo'-hi'` (offset), `no match` for lines that equal no A line within
--range (default 24) of the same y. Exact RGB match.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import render_compare  # noqa: E402


def rows(img, x0):
    w, h, px = img
    return [tuple(px[y * w + x0 + x] for x in range(256)) for y in range(h)]


def main(argv):
    rng = 24
    if '--range' in argv:
        i = argv.index('--range'); rng = int(argv[i + 1]); del argv[i:i + 2]
    if len(argv) == 1:
        img = render_compare.read_png(argv[0])
        a, b = rows(img, 0), rows(img, 260)
    else:
        a, b = rows(render_compare.read_png(argv[0]), 0), rows(render_compare.read_png(argv[1]), 0)
    res = []
    for y, line in enumerate(b):
        off = None
        for d in sorted(range(-rng, rng + 1), key=abs):
            if 0 <= y + d < len(a) and a[y + d] == line:
                off = d
                break
        res.append(off)
    start = 0
    for y in range(1, len(res) + 1):
        if y == len(res) or res[y] != res[start]:
            o = res[start]
            desc = 'no match' if o is None else f'A {start + o}-{y - 1 + o} (offset {o:+d})'
            print(f'B {start:3d}-{y - 1:3d}: {desc}')
            start = y


if __name__ == '__main__':
    main(sys.argv[1:])
