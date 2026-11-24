"""Sprite tiles per sprite palette in a --dump-tags frame (which CHR tiles / banks each palette draws, with bounding boxes).

usage: python3 tools/sprite_tiles.py DUMP.bin [DUMP.bin ..]
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sprite_pack import load_dump, VALID  # noqa: E402


def main(argv):
    if not argv:
        print(__doc__); return 1
    for f in argv:
        tags, fb, pal = load_dump(f)
        per = {}
        for p, t in enumerate(tags):
            if not t & VALID or (t >> 19 & 7) < 4:
                continue
            g = (t >> 19 & 7) - 4
            d = per.setdefault(g, {})
            x, y = p % 256, p // 256
            b = d.get(t & 0x1FFF, (x, y, x, y))
            d[t & 0x1FFF] = (min(b[0], x), min(b[1], y), max(b[2], x), max(b[3], y))
        print(os.path.basename(f), 'sprite palettes', ' '.join(f'{pal[0x10 + g * 4 + 1]:02X}{pal[0x10 + g * 4 + 2]:02X}{pal[0x10 + g * 4 + 3]:02X}' for g in range(4)))
        for g in sorted(per):
            print(f'  pal {g}: ' + ' '.join(f'{t:04X}@{b[0]},{b[1]}' for t, b in sorted(per[g].items())))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
