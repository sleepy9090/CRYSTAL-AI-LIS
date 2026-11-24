"""CHR tiles -> a labelled PNG sheet (for planning graphics-pack art).

usage: python3 tools/chr_sheet.py OUT.png PALETTE TILE[-TILE] [TILE..] [--scale N] [--cols N]
  PALETTE = 3 NES colours hex for slots 1-3, e.g. 36140F; TILE = tile number hex (CHR byte address / 16), ranges ok.
  Slot 0 is drawn as grey checkers. Each cell has the tile number written under it (3x5 digits).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pngio import encode  # noqa: E402
import core_vs_web as cv  # noqa: E402

DATA = os.path.join(cv.ROOT, '..', 'web', 'data')
DIGITS = {c: rows.replace('|', '') for c, rows in zip('0123456789ABCDEF', [
    '###|# #|# #|# #|###', ' # |## | # | # |###', '###|  #|###|#  |###', '###|  #|###|  #|###', '# #|# #|###|  #|  #',
    '###|#  |###|  #|###', '###|#  |###|# #|###', '###|  #|  #|  #|  #', '###|# #|###|# #|###', '###|# #|###|  #|###',
    ' # |# #|###|# #|# #', '## |# #|## |# #|## ', '###|#  |#  |#  |###', '## |# #|# #|# #|## ', '###|#  |## |#  |###',
    '###|#  |## |#  |#  '])}


def nes_rgb():
    import json
    return json.load(open(os.path.join(DATA, 'palettes.json')))['nesRgb']


def tile_slots(chr_, t):
    return [[(chr_[t * 16 + y] >> (7 - x) & 1) | (chr_[t * 16 + y + 8] >> (7 - x) & 1) << 1 for x in range(8)]
            for y in range(8)]


def main(argv):
    if len(argv) < 3 or argv[0] in ('-h', '--help'):
        print(__doc__); return 0
    out, palhex, rest = argv[0], argv[1], argv[2:]
    scale, cols, tiles = 4, 16, []
    i = 0
    while i < len(rest):
        a = rest[i]
        if a == '--scale':
            scale = int(rest[i + 1]); i += 1
        elif a == '--cols':
            cols = int(rest[i + 1]); i += 1
        elif '-' in a:
            lo, hi = (int(x, 16) for x in a.split('-'))
            tiles += range(lo, hi + 1)
        else:
            tiles.append(int(a, 16))
        i += 1
    chr_ = open(os.path.join(DATA, 'chr.bin'), 'rb').read()
    rgb = nes_rgb()
    cols3 = [None] + [rgb[int(palhex[k:k + 2], 16)] for k in (0, 2, 4)]
    cw, chh = 8 * scale + 4, 8 * scale + 12
    rows_n = (len(tiles) + cols - 1) // cols
    W, H = cols * cw, rows_n * chh
    img = [[(40, 40, 48)] * W for _ in range(H)]
    for n, t in enumerate(tiles):
        ox, oy = (n % cols) * cw + 2, (n // cols) * chh + 2
        s = tile_slots(chr_, t)
        for y in range(8 * scale):
            for x in range(8 * scale):
                v = s[y // scale][x // scale]
                c = cols3[v]
                img[oy + y][ox + x] = (c >> 16, c >> 8 & 255, c & 255) if c is not None else \
                    ((90, 90, 90) if (x // 4 + y // 4) % 2 else (120, 120, 120))
        for k, ch in enumerate('%04X' % t):
            pat = DIGITS[ch]
            for py in range(5):
                for px in range(3):
                    if pat[py * 3 + px] != ' ':
                        img[oy + 8 * scale + 3 + py][ox + k * 4 + px] = (230, 230, 120)
    open(out, "wb").write(encode(W, H, img))
    print(f'{out}: {len(tiles)} tiles, {W}x{H}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
