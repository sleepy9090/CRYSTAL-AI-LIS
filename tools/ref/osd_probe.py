#!/usr/bin/env python3
"""s101: find FCEUX's on-screen message overlay ("Snapshot Saved.") in the reference screenshots.

usage: python3 tools/ref/osd_probe.py [--ref DIR] [--y0 Y]
For each DIR/frame_*.png and screen_*.png: the colours that occur at or below line Y (default 200)
but nowhere above it, their pixel counts and bounding box.  Colours the NES picture also uses are
listed separately with their count inside the box.
"""
import argparse   # noqa: F401  (stdlib first: tools/ on sys.path shadows dis)
import dis        # noqa: F401
import inspect    # noqa: F401
import glob
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if ROOT not in sys.path:
    sys.path.append(ROOT)

from tools.render_compare import read_png  # noqa: E402


def probe(path, y0):
    w, h, px = read_png(path)
    above, below = {}, {}
    for i, c in enumerate(px):
        c = tuple(c)
        d = above if i // w < y0 else below
        d[c] = d.get(c, 0) + 1
    only = {c: n for c, n in below.items() if c not in above}
    box = None
    if only:
        idx = [i for i, c in enumerate(px) if tuple(c) in only]
        xs = [i % w for i in idx]
        ys = [i // w for i in idx]
        box = (min(xs), min(ys), max(xs), max(ys))
    return w, h, only, below, box


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--ref', default=os.path.join(ROOT, 'ref'))
    ap.add_argument('--y0', type=int, default=200)
    a = ap.parse_args()
    for p in sorted(glob.glob(os.path.join(a.ref, '*.png'))):
        w, h, only, below, box = probe(p, a.y0)
        name = os.path.basename(p)
        if not only:
            print(f'{name} {w}x{h}: no colours unique to lines >= {a.y0}')
            continue
        print(f'{name} {w}x{h}: box x{box[0]}-{box[2]} y{box[1]}-{box[3]}, unique colours ' +
              ' '.join(f'#{r:02X}{g:02X}{b:02X}:{n}' for (r, g, b), n in sorted(only.items(), key=lambda t: -t[1])))


if __name__ == '__main__':
    main()
