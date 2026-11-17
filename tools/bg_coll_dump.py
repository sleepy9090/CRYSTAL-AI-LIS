#!/usr/bin/env python3
"""Dump the BG collision attribute tables read by ObjBgProbe 26:9A30.

Model (REVERSE.md "World interaction"):
  page   = screen_grid[$6300 + y_hi*8 + x_hi]; R7 = page>>5 (bank 0-7), hi = $A0 | (page & $1F)
  M      = page byte[(y_lo & $F0) | (x_lo >> 4)]
  attr   = bank 9 byte at ($69 << 8) | M          ($69 = k1 byte 4, R7 = 9)
  if attr b3 and the screen's visited bit ($62F0[y_hi] & bit x_hi):
         M = bank 9 byte $BE00 + ($67 & $3C)*8 + M; attr = bank 9 ($69<<8)|M

usage: bg_coll_dump.py [--out docs/bg_coll.txt]
"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
import render_screen as rs  # noqa
from nesrom import ROOT  # noqa

BITS = {
    0: 'b0 (blocks non-player objects only, mask $27)',
    1: 'b1 low obstacle: blocks player on the ground (mask $32), jumpable',
    2: 'b2 wall: blocks player in the air too (mask $34)',
    3: 'b3 visited-substitutable metatile',
    4: 'b4 -> obj_flags b4 (draw behind BG)',
    5: 'b5 conveyor/current (PlayerConveyor); blocks non-player objects',
    6: 'b6 -> obj_flags b6 (sink, speed-2)',
    7: 'b7 (not tested by the probe callers seen so far)',
}


def table(v69):
    return [rs.prg_at(9, (v69 << 8) | m) for m in range(256)]


def main():
    out = 'docs/bg_coll.txt'
    if '--out' in sys.argv:
        out = sys.argv[sys.argv.index('--out') + 1]
    pages = {}
    for a in range(256):
        bg = rs.area_bg(a)
        if not bg:
            continue
        pages.setdefault((bg['v69'], bg['v67'] & 0x3C), []).append(a)
    lines = ['# BG collision attribute tables (bank 9, page = k1 $69), tools/bg_coll_dump.py', '']
    lines.append('## bit meanings (from ObjBgProbe / 26:9947 / PlayerMove)')
    lines += ['  ' + BITS[b] for b in range(8)]
    lines.append('')
    lines.append('## ($69, $67&$3C) -> areas')
    for (p, t), areas in sorted(pages.items()):
        lines.append('  $69=%02X tileset=%02X  %3d areas: %s' % (p, t, len(areas), ' '.join('%02X' % x for x in areas)))
    lines.append('')
    total = [0] * 8
    for p in sorted({k[0] for k in pages}):
        tb = table(p)
        cnt = [sum(1 for v in tb if v >> b & 1) for b in range(8)]
        for b in range(8):
            total[b] += cnt[b]
        lines.append('## page 9:%02X00  bit counts b0..b7: %s  distinct values: %s' % (
            p, ' '.join(str(c) for c in cnt), ' '.join('%02X' % v for v in sorted(set(tb)))))
        for r in range(16):
            lines.append('  %02X: %s' % (r * 16, ' '.join('%02X' % v for v in tb[r * 16:r * 16 + 16])))
        lines.append('')
    lines.insert(2, '## total bit counts over all pages b0..b7: %s' % ' '.join(str(c) for c in total))
    with open(os.path.join(ROOT, out), 'w') as f:
        f.write('\n'.join(lines) + '\n')
    print('\n'.join(l for l in lines if l.startswith('##') or l.startswith('  $69')))


if __name__ == '__main__':
    main()
