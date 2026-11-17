#!/usr/bin/env python3
"""Print the RGB colour at (x, y) of one or more PNGs.

usage: python3 tools/pixel_peek.py X Y FILE.png [FILE.png ...] [--ydelta-224]
  --ydelta-224  for 256x224 images (FCEUX, overscan cropped) read row y-8 so Y is in 240-line coords
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from render_compare import read_png  # noqa: E402


def main(argv):
    x, y = int(argv[0]), int(argv[1])
    for p in argv[2:]:
        w, h, px = read_png(p)
        yy = y - 8 if h == 224 else y
        print(f'{p}: {w}x{h} ({x},{y}) = #{"%02x%02x%02x" % tuple(px[yy * w + x])}')


if __name__ == '__main__':
    main(sys.argv[1:])
