#!/usr/bin/env python3
"""Decode the bank-17 ending data (states 4/5, session 83) -> docs/ending.txt.

REVERSE.md "Ending sequence, bank 17". Pair 8 = banks 16/17 at $8000-$BFFF; every pointer here is in $A000-$BFFF.
  EndingPhaseTable 17:A166 (30 x ptr16), EndingScriptBytes 17:A65E (step 19 interpreter), the text crawl list
  ($B854) (records: col, tiles.., 0; a leading 0 ends the list), line-text blocks (AB1D: W H, then H x (W tiles + advance)),
  rectangles (A7A0: W H + W*H tiles), pictures (A81C: W H metatiles, 64 attr, 32 palette, 2 + 4 CHR bank bytes),
  metasprites EndingSprites 17:B45E (attr dx dy tile, $FF end).
Tiles are shown as ASCII when printable (the ending font is ASCII-coded), '.' for $FF, '#' otherwise.
usage: python3 tools/ending_dump.py [--out docs/ending.txt]
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nesrom import load_rom, ROOT

_, PRG, _ = load_rom()


def rd(a):
    bank = 16 if a < 0xA000 else 17
    return PRG[bank * 0x2000 + (a & 0x1FFF)]


def rd16(a):
    return rd(a) | rd(a + 1) << 8


def txt(bs):
    return ''.join(chr(b) if 0x20 <= b < 0x7F else ('.' if b == 0xFF else '#') for b in bs)


def rect(p):
    w, h = rd(p), rd(p + 1)
    rows = [[rd(p + 2 + r * w + c) for c in range(w)] for r in range(h)]
    return w, h, rows, p + 2 + w * h


def lines(p):
    w, h = rd(p), rd(p + 1)
    p += 2
    out = []
    for _ in range(h):
        bs = [rd(p + i) for i in range(w)]
        adv = rd(p + w)
        out.append((bs, adv))
        p += w + 1
    return w, h, out, p


def picture(p):
    w, h = rd(p), rd(p + 1)
    q = p + 2 + w * h
    attr = [rd(q + i) for i in range(64)]
    pal = [rd(q + 64 + i) for i in range(32)]
    chr_ = [rd(q + 96 + i) for i in range(6)]
    return w, h, pal, chr_, q + 102


def main():
    out = sys.argv[sys.argv.index('--out') + 1] if '--out' in sys.argv else os.path.join(ROOT, 'docs', 'ending.txt')
    L = []
    L.append('# Ending data, bank 17 (tools/ending_dump.py, session 83)\n')
    L.append('## EndingPhaseTable 17:A166 (30 entries, obj_phase $0600)')
    for i in range(30):
        L.append(f'  {i:2d}  {rd16(0xA166 + 2 * i):04X}')

    L.append('\n## Text crawl, step 3: list at ($B854) = %04X  (col, tiles.., 0)' % rd16(0xB854))
    p = rd16(0xB854)
    n = 0
    while rd(p) != 0:
        col = rd(p)
        p += 1
        bs = []
        while rd(p) != 0:
            bs.append(rd(p))
            p += 1
        p += 1
        L.append(f'  {n:2d} col {col:2d}  ' + ' '.join('%02X' % b for b in bs))
        n += 1
    L.append(f'  ({n} records, end at {p:04X}; step 3 re-reads the list until 44 lines are queued)')

    p = rd16(0xBE37)
    for blk in ('step 7 (name page)', 'step 11'):
        w, h, ls, p2 = lines(p)
        L.append(f'\n## Line text {blk}: AB1D block at {p:04X}  W={w} H={h}')
        for bs, adv in ls:
            L.append(f'  |{txt(bs)}|  +{adv:02X}')
        p = p2

    L.append('\n## Rectangles (A7A0)')
    for name, ptr in (('STAFF ($A4E3)', 0xA4E3), ('frame ($AB95)', 0xAB95), ('blank ($A57A)', 0xA57A),
                      ('step 22 ($BD78)', 0xBD78)):
        w, h, rows, _ = rect(rd16(ptr))
        L.append(f'  {name} -> {rd16(ptr):04X}  {w}x{h}')
        for r in rows[:16]:
            L.append(f'     |{txt(r)}|')

    L.append('\n## Pictures (A81C)')
    for name, ptr in (('step 0 ($ABD9)', 0xABD9), ('step 25 ($B304)', 0xB304)):
        w, h, pal, chr_, end = picture(rd16(ptr))
        L.append(f'  {name} -> {rd16(ptr):04X}  {w}x{h} metatiles, end {end:04X}, CHR {" ".join("%02X" % c for c in chr_)}')
        L.append('     pal ' + ' '.join('%02X' % c for c in pal))

    L.append('\n## EndingScriptBytes 17:A65E (step 19; stream $88 starts at ($AC83) = %04X)' % rd16(0xAC83))
    y, sp = 0, rd16(0xAC83)
    page = 0
    while True:
        b = rd(0xA65E + y); y += 1
        if b & 0x80:
            L.append(f'  [{y-1:02X}] {b:02X} end -> fade out, step 22')
            break
        if b == 0:
            w, h, pal, chr_, end = picture(sp)
            spr = rd(0xA65E + y); y += 1
            L.append(f'  [{y-2:02X}] 00 picture {sp:04X} {w}x{h} CHR {" ".join("%02X" % c for c in chr_)} + sprite {spr}')
            sp = end
            continue
        rp = rd16(0xB8D2 + 2 * b)
        w, h, rows, _ = rect(rp)
        sub, c5, c4 = rd(0xA65E + y), rd(0xA65E + y + 1), rd(0xA65E + y + 2)
        y += 3
        nxt = rd(0xA65E + y); y += 1
        L.append(f'  [{y-5:02X}] page {page}: rect {b} ({rp:04X} {w}x{h}) at $2250, $060E={sub} wait {c4}/{c5}'
                 f' ({(c4 + 1) + 60 * c5} frames), then $060E={nxt}')
        for r in rows:
            L.append(f'     |{txt(r)}|')
        page += 1

    L.append('\n## EndingSprites 17:B45E (attr dx dy tile)')
    for i in range(10):
        p = rd16(0xB45E + 2 * i)
        recs = []
        while rd(p) != 0xFF and len(recs) < 64:
            recs.append('%02X %02X %02X %02X' % tuple(rd(p + k) for k in range(4)))
            p += 4
        L.append(f'  {i}: {rd16(0xB45E + 2 * i):04X} {len(recs)} sprites  ' + ' | '.join(recs[:4]) + (' ..' if len(recs) > 4 else ''))

    os.makedirs(os.path.dirname(out), exist_ok=True)
    open(out, 'w').write('\n'.join(L) + '\n')
    print('\n'.join(L))


if __name__ == '__main__':
    main()
