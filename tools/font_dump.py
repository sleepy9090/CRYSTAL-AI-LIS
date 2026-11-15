#!/usr/bin/env python3
"""Render the BG pattern table used by the text box (ShowMessage 30:D354 sets R0=$3C, R1=$38).

usage: font_dump.py [R0 R1] [--out gfx/font.png] [--scale N]
Tiles $00-$7F come from R0 (1KB banks R0, R0+1), $80-$FF from R1 (R1, R1+1).
Draws a 16x16 grid of tiles (tile id = row*16+col), grayscale, scale N (default 3).
"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from nesrom import load_rom, ROOT  # noqa
from meta_dump import tile_pixels, write_png  # noqa

SHADES = [(0, 0, 0), (85, 85, 85), (170, 170, 170), (255, 255, 255)]


def main(argv):
    out = os.path.join(ROOT, 'gfx', 'font.png'); scale = 3
    if '--out' in argv:
        i = argv.index('--out'); out = argv[i + 1]; del argv[i:i + 2]
    if '--scale' in argv:
        i = argv.index('--scale'); scale = int(argv[i + 1]); del argv[i:i + 2]
    r0, r1 = (int(argv[0], 0), int(argv[1], 0)) if len(argv) >= 2 else (0x3C, 0x38)
    chr_rom = load_rom()[2]
    regs = [r0, r0 + 1, r1, r1 + 1]  # 4 x 64 tiles
    W = H = 16 * 8 * scale
    px = [(0, 0, 0)] * (W * H)
    for t in range(256):
        tp = tile_pixels(chr_rom, regs, t)
        ox, oy = (t & 15) * 8 * scale, (t >> 4) * 8 * scale
        for y in range(8):
            for x in range(8):
                c = SHADES[tp[y][x]]
                for sy in range(scale):
                    base = (oy + y * scale + sy) * W + ox + x * scale
                    for sx in range(scale):
                        px[base + sx] = c
    write_png(out, W, H, px)
    print(f'wrote {out} (R0=${r0:02X} R1=${r1:02X})')


if __name__ == '__main__':
    main(sys.argv[1:])
