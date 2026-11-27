"""Contact sheet of every talking NPC (spawn handler 1, type $30) by NPC id p, drawn with its first area's sprite CHR and
palettes (reuses the read-only ../../tools meta_dump / area_dump renderers). For classifying NPC voices (Dialogue.cs).

usage: python3 tools/npc_sheet.py OUT_DIR [--scale N] [--per-sheet N] [--dir D (meta id + D: another facing)]
Writes OUT_DIR/npcs_K.png (cells labelled by p in npcs.txt: row-major, cell index -> p, area, meta, tile add).
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', '..', 'tools'))
import area_dump  # noqa: E402
import meta_dump  # noqa: E402
import palette_dump  # noqa: E402
from nesrom import load_rom  # noqa: E402


def main(argv):
    if not argv:
        print(__doc__); return 2
    out = argv[0]
    scale = int(argv[argv.index('--scale') + 1]) if '--scale' in argv else 3
    per = int(argv[argv.index('--per-sheet') + 1]) if '--per-sheet' in argv else 48
    dadd = int(argv[argv.index('--dir') + 1]) if '--dir' in argv else 0   # added to the meta id (a facing)
    os.makedirs(out, exist_ok=True)
    _, prg, chr_rom = load_rom()
    npcs = {}
    for area in range(256):
        s = meta_dump.area_sprite_set(prg, area)
        if not s:
            continue
        for o in s['objects']:
            if o['handler'] == 1 and o['type'] == 0x30 and o['meta'] is not None and o['param'] not in npcs:
                npcs[o['param']] = (area, o['meta'], o['tile_add'], s)
    keys = sorted(npcs)
    cell, cols = 32, 8
    lines = []
    for k0 in range(0, len(keys), per):
        chunk = keys[k0:k0 + per]
        rows = (len(chunk) + cols - 1) // cols
        W, H = cols * cell, rows * cell
        px = [meta_dump.BG] * (W * H)
        for i, p in enumerate(chunk):
            area, mid, tadd, s = npcs[p]
            rgbpals = [[palette_dump.rgb(c) for c in pl] for pl in s['pals']]
            fr = meta_dump.frames(prg, (mid + dadd) & 0xFF)
            cx, cy = (i % cols) * cell, (i // cols) * cell
            for x in range(cell):
                px[cy * W + cx + x] = meta_dump.GRID
            for y in range(cell):
                px[(cy + y) * W + cx] = meta_dump.GRID
            if fr:
                meta_dump.draw_meta(px, W, (cx, cell // 2), (cy, cell * 3 // 4), fr[0], chr_rom, s['chr'], rgbpals, tadd, cell)
            lines.append(f"sheet {k0 // per} cell {i:2d} (row {i // cols}, col {i % cols}): p=${p:02X} area ${area:02X} meta ${mid:02X} tile+{tadd:02X}")
        big = [px[(y // scale) * W + x // scale] for y in range(H * scale) for x in range(W * scale)]
        meta_dump.write_png(os.path.join(out, f'npcs_{k0 // per}.png'), W * scale, H * scale, big)
    with open(os.path.join(out, 'npcs.txt'), 'w') as f:
        f.write('\n'.join(lines) + '\n')
    print(f'{len(keys)} NPCs -> {out}/npcs_*.png, npcs.txt')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
