#!/usr/bin/env python3
"""Decode the per-area record table read by AreaLoad (31:E14A).

usage: area_dump.py [list|chr|spawns] [area ...]
  list    (default) one line per area: ptr, $07E6/$07E7, R4/R5, spawn count
  chr     group areas by (R4,R5) sprite CHR pair (for meta_dump.py --chr)
  spawns  full decode of each area's spawn entries

Format (session 7, see REVERSE.md "Area records"):
  AreaPtrTable 12:9201 ptr16 indexed by $6C (area id 0-255; banks 12/13 via
  SwitchBank16(6)). Ptr hi bit7 clear -> no record.
  record: 00 e6 e7 R4 R5 {entries} FF
     byte0 != 0 -> record ignored
     entry (4 bytes) per object slot, slots start at X=$0D:
       FE xx xx xx  -> slot skipped (4-byte entry)
       FF           -> end
       b0 = Y cell  : Y = b0*16 + 12
       b1 = X cell  : bit7 -> slot skipped; X = (b1&$7F)*16 + 8 (+8 if b2 b6)
       b2 = flags   : b0-2 handler (SpawnHandlerTable 31:E218), b0-1 also ->$0540,
                      b6 x+8, b7 -> $0380 b5 (tile+$40)
       b3 = param   ($2F) -> $0560
"""
import sys
from nesrom import load_rom

_, PRG, _ = load_rom()
TABLE = 0x9201
BANK = 12

HANDLERS = {
    0: 'type=p+$50',
    1: 'via 4:$80F0 rec',
    2: 'type $0D/$0E (p bit7)',
    3: 'type=p+$D0',
    4: 'type=p (p&F0=40: flag item)',
}


def rd(addr):
    b = BANK + (1 if addr >= 0xA000 else 0)
    return PRG[b * 0x2000 + (addr & 0x1FFF)]


def ptr(area):
    a = TABLE + area * 2
    return rd(a) | rd(a + 1) << 8


def table_len():
    """Table ends where the lowest valid record begins."""
    lo = min(ptr(i) for i in range(256) if ptr(i) & 0x8000 and ptr(i) > TABLE)
    return min(256, (lo - TABLE) // 2)


def decode(area):
    p = ptr(area)
    if not p & 0x8000:
        return None
    if rd(p) != 0:
        return {'ptr': p, 'ignored': rd(p)}
    e6, e7, r4, r5 = (rd(p + i) for i in range(1, 5))
    ents = []
    y = p + 5
    x = 0x0D
    while y < 0xC000:
        b0 = rd(y)
        if b0 == 0xFF:
            break
        if b0 == 0xFE:
            ents.append((x, None))
        else:
            b1, b2, b3 = rd(y + 1), rd(y + 2), rd(y + 3)
            if b1 & 0x80:
                ents.append((x, 'skip'))
            else:
                ents.append((x, (b0 * 16 + 12, (b1 & 0x7F) * 16 + 8 + (8 if b2 & 0x40 else 0),
                                 b2 & 7, b3, b2)))
        y += 4
        x = (x + 1) & 0xFF
    return {'ptr': p, 'e6': e6, 'e7': e7, 'r4': r4, 'r5': r5, 'ents': ents, 'end': y}


def main(argv):
    mode = argv[0] if argv and argv[0] in ('list', 'chr', 'spawns') else 'list'
    rest = [a for a in argv if a not in ('list', 'chr', 'spawns')]
    n = table_len()
    areas = [int(a, 0) for a in rest] or list(range(n))
    print(f'; AreaPtrTable 12:{TABLE:04X}, {n} entries (table end ${TABLE + 2 * n:04X})')
    groups = {}
    for a in areas:
        d = decode(a)
        if d is None:
            if mode == 'list':
                print(f'{a:3d} ${a:02X}  ----  (null ${ptr(a):04X})')
            continue
        if 'ignored' in d:
            print(f'{a:3d} ${a:02X}  {d["ptr"]:04X} byte0=${d["ignored"]:02X} (ignored)')
            continue
        nsp = sum(1 for _, e in d['ents'] if isinstance(e, tuple))
        groups.setdefault((d['r4'], d['r5']), []).append(a)
        if mode in ('list', 'spawns'):
            print(f'{a:3d} ${a:02X}  {d["ptr"]:04X} e6={d["e6"]:02X} e7={d["e7"]:02X} '
                  f'R4={d["r4"]:02X} R5={d["r5"]:02X} spawns={nsp} slots={len(d["ents"])}')
        if mode == 'spawns':
            for x, e in d['ents']:
                if e is None:
                    print(f'      slot {x:02X}: FE (skip)')
                elif e == 'skip':
                    print(f'      slot {x:02X}: b1 bit7 (skip)')
                else:
                    yy, xx, h, prm, b2 = e
                    print(f'      slot {x:02X}: x={xx:4d} y={yy:4d} h{h} p=${prm:02X} '
                          f'b2=${b2:02X}  {HANDLERS.get(h, "INVALID")}')
    if mode == 'chr':
        for (r4, r5), al in sorted(groups.items()):
            print(f'R4={r4:02X} R5={r5:02X}  {len(al):3d} areas: '
                  + ' '.join(f'{a:02X}' for a in al))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
