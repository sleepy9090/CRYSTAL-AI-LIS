"""Crop the same rectangle out of several PNGs, zoom it, and put the crops side by side (for before / after views).

usage: python3 tools/png_crop.py OUT.png X Y W H ZOOM IN.png [IN.png ..]
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pngio import encode, png_decode  # noqa: E402


def main(argv):
    if len(argv) < 7 or argv[0] in ('-h', '--help'):
        print(__doc__); return 0
    out = argv[0]
    x0, y0, w, h, z = (int(v) for v in argv[1:6])
    gap = 8
    crops = []
    for p in argv[6:]:
        _, _, rows = png_decode(open(p, 'rb').read())
        crops.append([[rows[y][x] for x in range(x0, x0 + w) for _ in range(z)] for y in range(y0, y0 + h) for _ in range(z)])
    W, H = len(crops) * (w * z + gap) - gap, h * z
    img = [[(40, 40, 48)] * W for _ in range(H)]
    for k, c in enumerate(crops):
        for y, row in enumerate(c):
            img[y][k * (w * z + gap):k * (w * z + gap) + w * z] = row
    open(out, 'wb').write(encode(W, H, img))
    print(f'{out}: {W}x{H}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
