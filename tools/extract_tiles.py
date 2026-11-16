#!/usr/bin/env python3
"""Dump all 128 CHR 1KB banks of re-c to PNG.

usage: extract_tiles.py [--scale N] [--outdir gfx]
  gfx/chr/chr_NNN.png : one 1KB bank = 64 tiles as 16x4 tiles, grayscale, scale N (default 4)
  gfx/chr_all.png     : all 128 banks stacked, 8 banks per row (128x32 px each at scale 1),
                        drawn at scale 2 with a 1px separator grid between banks.
Grayscale: colour index 0..3 -> 0, 85, 170, 255.
"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import load_rom, ROOT  # noqa
from meta_dump import write_png  # noqa

SHADES = [(0, 0, 0), (85, 85, 85), (170, 170, 170), (255, 255, 255)]
SEP = (96, 0, 96)


def bank_pixels(chr_rom, bank):
    """-> 32 rows x 128 cols of colour indices (16x4 tiles)."""
    rows = [[0] * 128 for _ in range(32)]
    base = bank * 0x400
    for t in range(64):
        d = chr_rom[base + t * 16: base + t * 16 + 16]
        ox, oy = (t & 15) * 8, (t >> 4) * 8
        for y in range(8):
            lo, hi = d[y], d[y + 8]
            for x in range(8):
                rows[oy + y][ox + x] = ((lo >> (7 - x)) & 1) | (((hi >> (7 - x)) & 1) << 1)
    return rows


def scaled(rows, scale):
    h, w = len(rows), len(rows[0])
    W, H = w * scale, h * scale
    px = [None] * (W * H)
    for y in range(H):
        r = rows[y // scale]
        for x in range(W):
            px[y * W + x] = SHADES[r[x // scale]]
    return W, H, px


def main(argv):
    scale = 4; outdir = os.path.join(ROOT, 'gfx')
    if '--scale' in argv:
        i = argv.index('--scale'); scale = int(argv[i + 1]); del argv[i:i + 2]
    if '--outdir' in argv:
        i = argv.index('--outdir'); outdir = argv[i + 1]; del argv[i:i + 2]
    chr_rom = load_rom()[2]
    nb = len(chr_rom) // 0x400
    os.makedirs(os.path.join(outdir, 'chr'), exist_ok=True)
    allrows = []
    blank = 0
    for b in range(nb):
        rows = bank_pixels(chr_rom, b)
        allrows.append(rows)
        if not any(chr_rom[b * 0x400:(b + 1) * 0x400]):
            blank += 1
        W, H, px = scaled(rows, scale)
        write_png(os.path.join(outdir, 'chr', f'chr_{b:03d}.png'), W, H, px)
    # sheet: 8 banks per row, scale 2, 1px separators
    s2, per = 2, 8
    bw, bh = 128 * s2, 32 * s2
    W = per * (bw + 1) + 1
    H = (nb // per) * (bh + 1) + 1
    px = [SEP] * (W * H)
    for b, rows in enumerate(allrows):
        ox = 1 + (b % per) * (bw + 1); oy = 1 + (b // per) * (bh + 1)
        for y in range(bh):
            r = rows[y // s2]
            base = (oy + y) * W + ox
            for x in range(bw):
                px[base + x] = SHADES[r[x // s2]]
    write_png(os.path.join(outdir, 'chr_all.png'), W, H, px)
    print(f'wrote {nb} banks to {outdir}/chr/chr_NNN.png (scale {scale}) + {outdir}/chr_all.png '
          f'({W}x{H}); all-zero banks: {blank}')


if __name__ == '__main__':
    main(sys.argv[1:])
