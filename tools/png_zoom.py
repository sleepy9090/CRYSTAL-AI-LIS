#!/usr/bin/env python3
"""Crop + nearest-neighbour zoom of one or more PNGs, stacked vertically (session 108).

usage: png_zoom.py OUT.png X Y W H SCALE IN.png [IN2.png ...]
Use it to look at a few tiles of two screenshots (tools.emu vs web port) side by side.
"""
import os
import sys

# tools/dis.py shadows the stdlib `dis` that PIL -> inspect needs: drop the script dir from the path first
_here = os.path.dirname(os.path.abspath(__file__))
sys.path[:] = [p for p in sys.path if os.path.abspath(p or '.') != _here]
from PIL import Image  # noqa: E402


def main(argv):
    if len(argv) < 7:
        print(__doc__)
        return 1
    out, x, y, w, h, s = argv[0], *map(int, argv[1:6])
    ims = [Image.open(p).convert('RGB').crop((x, y, x + w, y + h)).resize((w * s, h * s), Image.NEAREST)
           for p in argv[6:]]
    res = Image.new('RGB', (w * s, (h * s + 4) * len(ims)), (255, 0, 255))
    for i, im in enumerate(ims):
        res.paste(im, (0, i * (h * s + 4)))
    res.save(out)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
