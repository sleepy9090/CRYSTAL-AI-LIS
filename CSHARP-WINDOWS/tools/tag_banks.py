"""Which tiles / 1 KB CHR banks a --dump-tags frame uses in a screen rectangle (background pixels only).

usage: python3 tools/tag_banks.py DUMP.bin X Y W H
"""
import sys
import os

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sprite_pack import load_dump, VALID  # noqa: E402


def main(argv):
    if len(argv) != 5:
        print(__doc__); return 1
    tags, fb, pal = load_dump(argv[0])
    x0, y0, w, h = (int(v) for v in argv[1:])
    banks, tiles = {}, {}
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            t = tags[y * 256 + x]
            if not t & VALID or (t >> 19 & 7) >= 4:
                continue
            banks[(t & 0x1FFF) >> 6] = banks.get((t & 0x1FFF) >> 6, 0) + 1
            tiles[t & 0x1FFF] = t >> 19 & 7
    print('banks: ' + ', '.join(f'{b:02X} x{n}' for b, n in sorted(banks.items())))
    print('tiles: ' + ' '.join(f'{t:04X}/{g}' for t, g in sorted(tiles.items())))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
